using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        [DebuggerHidden]
        private static void AssertNotSent(NetOutgoingMessage msg)
        {
            if (msg.m_isSent)
                throw new NetException(
                    "This message has already been sent. Use NetPeer.SendMessage() to send to multiple recipients efficiently.");
        }

        [DebuggerHidden]
        private static void AssertValidRecipients<T>(IList<T> recipients, string paramName)
        {
            if (recipients == null)
                throw new ArgumentNullException(paramName);
            if (recipients.Count < 1)
                throw new ArgumentException("Recipients must contain at least one item.", paramName);
        }

        [DebuggerHidden]
        private void AssertValidUnconnectedLength(NetOutgoingMessage msg)
        {
            if (msg.LengthBytes > m_configuration.MaximumTransmissionUnit)
                throw new NetException(
                    "Unconnected message must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " +
                    m_configuration.MaximumTransmissionUnit + ").");
        }

        /// <summary>
        /// Send a message to a specific connection.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        public NetSendResult SendMessage(
            NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method)
        {
            return SendMessage(msg, recipient, method, 0);
        }

        /// <summary>
        /// Send a message to a specific connection.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public NetSendResult SendMessage(
            NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method, int sequenceChannel)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (sequenceChannel >= NetConstants.NetChannelsPerDeliveryMethod)
                throw new ArgumentOutOfRangeException(nameof(sequenceChannel));

            NetException.Assert(
                ((method != NetDeliveryMethod.Unreliable && method != NetDeliveryMethod.ReliableUnordered) ||
                ((method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.ReliableUnordered) && sequenceChannel == 0)),
                "Delivery method " + method + " cannot use sequence channels other than 0!");

            NetException.Assert(method != NetDeliveryMethod.Unknown, "Bad delivery method!");

            AssertNotSent(msg);
            msg.m_isSent = true;

            bool suppressFragmentation = 
                (method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.UnreliableSequenced) &&
                m_configuration.UnreliableSizeBehaviour != NetUnreliableSizeBehaviour.NormalFragmentation;

            // headers + length, faster than calling msg.GetEncodedSize
            int len = NetConstants.UnfragmentedMessageHeaderSize + msg.LengthBytes;
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

                var tmp = NetConnectionListPool.Rent();
                tmp.Add(recipient);
                var result = SendFragmentedMessage(msg, tmp, method, sequenceChannel);
                NetConnectionListPool.Return(tmp);
                return result;
            }
        }

        internal static int GetMTU(List<NetConnection> recipients)
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
                if (conn != null && conn.m_currentMTU < mtu)
                    mtu = conn.m_currentMTU;
            }
            return mtu;
        }

        /// <summary>
        /// Send a message to a list of connections.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipients">The list of recipients to send to</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public void SendMessage(
            NetOutgoingMessage msg, List<NetConnection> recipients, NetDeliveryMethod method, int sequenceChannel)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            AssertValidRecipients(recipients, nameof(recipients));

            if (method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.ReliableUnordered)
                NetException.Assert(sequenceChannel == 0, "Delivery method " + method + " cannot use sequence channels other than 0!");

            AssertNotSent(msg);
            msg.m_isSent = true;

            int len = msg.GetEncodedSize();
            int mtu = GetMTU(recipients);
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
        /// Send a message to an unconnected host.
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage msg, string host, int port)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (host == null) throw new ArgumentNullException(nameof(host));
            AssertValidUnconnectedLength(msg);
            AssertNotSent(msg);

            IPAddress adr = NetUtility.Resolve(host);
            if (adr == null)
                throw new NetException("Failed to resolve " + host);

            var endpoint = new IPEndPoint(adr, port);
            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            Interlocked.Increment(ref msg.m_recyclingCount);
            m_unsentUnconnectedMessages.Enqueue(new NetTuple<IPEndPoint, NetOutgoingMessage>(endpoint, msg));
        }

        /// <summary>
        /// Send a message to an unconnected host.
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage msg, IPEndPoint recipient)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            AssertValidUnconnectedLength(msg);
            AssertNotSent(msg);

            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            Interlocked.Increment(ref msg.m_recyclingCount);
            m_unsentUnconnectedMessages.Enqueue(new NetTuple<IPEndPoint, NetOutgoingMessage>(recipient, msg));
        }

        /// <summary>
        /// Send a message to an unconnected host.
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage msg, IList<IPEndPoint> recipients)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            AssertValidRecipients(recipients, nameof(recipients));
            AssertValidUnconnectedLength(msg);
            AssertNotSent(msg);

            msg.m_messageType = NetMessageType.Unconnected;
            msg.m_isSent = true;

            Interlocked.Add(ref msg.m_recyclingCount, recipients.Count);
            foreach (IPEndPoint ep in recipients)
                m_unsentUnconnectedMessages.Enqueue(new NetTuple<IPEndPoint, NetOutgoingMessage>(ep, msg));
        }

        /// <summary>
        /// Send a message to this exact same netpeer (loopback).
        /// </summary>
        public void SendUnconnectedToSelf(NetOutgoingMessage msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            AssertNotSent(msg);

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