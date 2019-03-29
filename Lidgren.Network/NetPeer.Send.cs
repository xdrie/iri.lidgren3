using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        /// <summary>
        /// Send a message to a specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        public NetSendResult SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method)
        {
            return SendMessage(msg, recipient, method, 0);
        }

        /// <summary>
        /// Send a message to a specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public NetSendResult SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method, int sequenceChannel)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));
            if (sequenceChannel >= NetConstants.NetChannelsPerDeliveryMethod)
                throw new ArgumentOutOfRangeException(nameof(sequenceChannel));

            NetException.Assert(
                ((method != NetDeliveryMethod.Unreliable && method != NetDeliveryMethod.ReliableUnordered) ||
                ((method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.ReliableUnordered) && sequenceChannel == 0)),
                "Delivery method " + method + " cannot use sequence channels other than 0!"
            );

            NetException.Assert(method != NetDeliveryMethod.Unknown, "Bad delivery method!");

            if (msg.m_isSent)
                throw new NetException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            msg.m_isSent = true;

            bool suppressFragmentation = (method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.UnreliableSequenced) && m_configuration.UnreliableSizeBehaviour != NetUnreliableSizeBehaviour.NormalFragmentation;

            int len = NetConstants.UnfragmentedMessageHeaderSize + msg.LengthBytes; // headers + length, faster than calling msg.GetEncodedSize
            if (len <= recipient.m_currentMTU || suppressFragmentation)
            {
                Interlocked.Increment(ref msg.m_recyclingCount);
                return recipient.EnqueueMessage(msg, method, sequenceChannel);
            }
            else
            {
                // message must be fragmented!
                if (recipient.m_status != NetConnectionStatus.Connected)
                    return NetSendResult.FailedNotConnected;
                return SendFragmentedMessage(msg, new NetConnection[] { recipient }, method, sequenceChannel);
            }
        }

        internal static int GetMTU(ICollection<NetConnection> recipients)
        {
            int mtu = NetPeerConfiguration.kDefaultMTU;
            if (recipients.Count < 1)
            {
#if DEBUG
				throw new NetException("GetMTU called with no recipients");
#else
                // we don't have access to the particular peer, so just use default MTU
                return mtu;
#endif
            }

            foreach(var conn in recipients)
            {
                if (conn == null)
                    continue;

                if (conn.m_currentMTU < mtu)
                    mtu = conn.m_currentMTU;
            }
            return mtu;
        }

        /// <summary>
        /// Send a message to a list of connections
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipients">The list of recipients to send to</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public void SendMessage(NetOutgoingMessage msg, ICollection<NetConnection> recipients, NetDeliveryMethod method, int sequenceChannel)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (recipients == null)
                throw new ArgumentNullException(nameof(recipients));
            if (recipients.Count < 1)
                throw new NetException("recipients must contain at least one item");

            if (method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.ReliableUnordered)
                NetException.Assert(sequenceChannel == 0, "Delivery method " + method + " cannot use sequence channels other than 0!");
            if (msg.m_isSent)
                throw new NetException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");

            int mtu = GetMTU(recipients);
            msg.m_isSent = true;

            int len = msg.GetEncodedSize();
            if (len <= mtu)
            {
                Interlocked.Add(ref msg.m_recyclingCount, recipients.Count);
                foreach (var conn in recipients)
                {
                    if (conn == null)
                    {
                        Interlocked.Decrement(ref msg.m_recyclingCount);
                        continue;
                    }

                    NetSendResult res = conn.EnqueueMessage(msg, method, sequenceChannel);
                    if (res != NetSendResult.Queued && res != NetSendResult.Sent)
                        Interlocked.Decrement(ref msg.m_recyclingCount);
                }
            }
            else
            {
                // message must be fragmented!
                SendFragmentedMessage(msg, recipients, method, sequenceChannel);
            }
        }

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage msg, string host, int port)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (msg.m_isSent)
                throw new NetException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            if (msg.LengthBytes > m_configuration.MaximumTransmissionUnit)
                throw new NetException("Unconnected messages too long! Must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " + m_configuration.MaximumTransmissionUnit + ")");

            IPAddress adr = NetUtility.Resolve(host);
            if (adr == null)
                throw new NetException("Failed to resolve " + host);

            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            Interlocked.Increment(ref msg.m_recyclingCount);
            m_unsentUnconnectedMessages.Enqueue(new NetTuple<IPEndPoint, NetOutgoingMessage>(new IPEndPoint(adr, port), msg));
        }

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage msg, IPEndPoint recipient)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));
            if (msg.m_isSent)
                throw new NetException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            if (msg.LengthBytes > m_configuration.MaximumTransmissionUnit)
                throw new NetException("Unconnected messages too long! Must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " + m_configuration.MaximumTransmissionUnit + ")");

            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            Interlocked.Increment(ref msg.m_recyclingCount);
            m_unsentUnconnectedMessages.Enqueue(new NetTuple<IPEndPoint, NetOutgoingMessage>(recipient, msg));
        }

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage msg, IList<IPEndPoint> recipients)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (recipients == null)
                throw new ArgumentNullException(nameof(recipients));
            if (recipients.Count < 1)
                throw new NetException("recipients must contain at least one item");
            if (msg.m_isSent)
                throw new NetException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            if (msg.LengthBytes > m_configuration.MaximumTransmissionUnit)
                throw new NetException("Unconnected messages too long! Must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " + m_configuration.MaximumTransmissionUnit + ")");

            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            Interlocked.Add(ref msg.m_recyclingCount, recipients.Count);
            foreach (IPEndPoint ep in recipients)
                m_unsentUnconnectedMessages.Enqueue(new NetTuple<IPEndPoint, NetOutgoingMessage>(ep, msg));
        }

        /// <summary>
        /// Send a message to this exact same netpeer (loopback)
        /// </summary>
        public void SendUnconnectedToSelf(NetOutgoingMessage msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            if (msg.m_isSent)
                throw new NetException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");

            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.UnconnectedData) == false)
                return; // dropping unconnected message since it's not enabled for receiving

            NetIncomingMessage om = CreateIncomingMessage(NetIncomingMessageType.UnconnectedData, msg.LengthBytes);
            om.Write(msg);
            om.m_isFragment = false;
            om.m_receiveTime = NetTime.Now;
            om.m_senderConnection = null;
            om.m_senderEndPoint = m_socket.LocalEndPoint as IPEndPoint;
            NetException.Assert(om.m_bitLength == msg.LengthBits);

            ReleaseMessage(om);
        }
    }
}