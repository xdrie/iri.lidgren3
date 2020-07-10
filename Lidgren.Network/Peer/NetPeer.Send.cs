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
        private static void AssertValidRecipients<T>(IReadOnlyCollection<T> recipients, string paramName)
        {
            if (recipients == null)
                throw new ArgumentNullException(paramName);

            if (recipients.Count < 1)
                throw new ArgumentException("The must be at least one recipient.", paramName);
        }

        [DebuggerHidden]
        private void AssertValidUnconnectedLength(NetOutgoingMessage message)
        {
            if (message.ByteLength > Configuration.MaximumTransmissionUnit)
                throw new LidgrenException(
                    "Unconnected message must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " +
                    Configuration.MaximumTransmissionUnit + ").");
        }

        /// <summary>
        /// Send a message to a specific connection.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        public NetSendResult SendMessage(
            NetOutgoingMessage message, NetConnection recipient, NetDeliveryMethod method)
        {
            return SendMessage(message, recipient, method, 0);
        }

        /// <summary>
        /// Send a message to a specific connection.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public NetSendResult SendMessage(
            NetOutgoingMessage message, NetConnection recipient, NetDeliveryMethod method, int sequenceChannel)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (sequenceChannel >= NetConstants.ChannelsPerDeliveryMethod)
                throw new ArgumentOutOfRangeException(nameof(sequenceChannel));

            LidgrenException.Assert(
                (method != NetDeliveryMethod.Unreliable && method != NetDeliveryMethod.ReliableUnordered) ||
                ((method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.ReliableUnordered) && sequenceChannel == 0),
                "Delivery method " + method + " cannot use sequence channels other than 0!");

            LidgrenException.Assert(method != NetDeliveryMethod.Unknown, "Bad delivery method!");

            message.AssertNotSent(nameof(message));
            message._isSent = true;

            bool suppressFragmentation =
                (method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.UnreliableSequenced) &&
                Configuration.UnreliableSizeBehaviour != NetUnreliableSizeBehaviour.NormalFragmentation;

            // headers + length, faster than calling message.GetEncodedSize
            int len = NetConstants.UnfragmentedMessageHeaderSize + message.ByteLength;
            if (len <= recipient.CurrentMTU || suppressFragmentation)
            {
                Interlocked.Increment(ref message._recyclingCount);
                return recipient.EnqueueMessage(message, method, sequenceChannel);
            }
            else
            {
                // message must be fragmented!
                if (recipient._internalStatus != NetConnectionStatus.Connected)
                    return NetSendResult.FailedNotConnected;

                var tmp = NetConnectionListPool.Rent();
                try
                {
                    tmp.Add(recipient);
                    return SendFragmentedMessage(message, tmp, method, sequenceChannel);
                }
                finally
                {
                    NetConnectionListPool.Return(tmp);
                }
            }
        }

        internal static int GetMTU(IReadOnlyCollection<NetConnection> recipients)
        {
            int mtu = NetPeerConfiguration.DefaultMTU;
            if (recipients.Count < 1)
            {
#if DEBUG
                throw new LidgrenException("GetMTU called with no recipients.");
#else
                // we don't have access to the particular peer, so just use default MTU
                return mtu;
#endif
            }

            foreach (var conn in recipients)
            {
                if (conn != null && conn.CurrentMTU < mtu)
                    mtu = conn.CurrentMTU;
            }
            return mtu;
        }

        /// <summary>
        /// Send a message to a list of connections.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="recipients">The list of recipients to send to</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public void SendMessage(
            NetOutgoingMessage message, IReadOnlyCollection<NetConnection> recipients, NetDeliveryMethod method, int sequenceChannel)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            AssertValidRecipients(recipients, nameof(recipients));

            if (method == NetDeliveryMethod.Unreliable || method == NetDeliveryMethod.ReliableUnordered)
                LidgrenException.Assert(sequenceChannel == 0, "Delivery method " + method + " cannot use sequence channels other than 0!");

            message.AssertNotSent(nameof(message));
            message._isSent = true;

            int len = message.GetEncodedSize();
            int mtu = GetMTU(recipients);
            if (len <= mtu)
            {
                Interlocked.Add(ref message._recyclingCount, recipients.Count);
                foreach (var conn in recipients)
                {
                    if (conn == null)
                    {
                        Interlocked.Decrement(ref message._recyclingCount);
                        continue;
                    }

                    NetSendResult res = conn.EnqueueMessage(message, method, sequenceChannel);
                    if (res != NetSendResult.Queued && res != NetSendResult.Sent)
                        Interlocked.Decrement(ref message._recyclingCount);
                }
            }
            else
            {
                // message must be fragmented!
                SendFragmentedMessage(message, recipients, method, sequenceChannel);
            }
        }

        private void SendUnconnectedMessageCore(NetOutgoingMessage message, IPEndPoint recipient)
        {
            message._messageType = NetMessageType.Unconnected;
            message._isSent = true;

            Interlocked.Increment(ref message._recyclingCount);
            UnsentUnconnectedMessages.Enqueue((recipient, message));
        }

        /// <summary>
        /// Send a message to an unconnected host.
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage message, ReadOnlySpan<char> host, int port)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            message.AssertNotSent(nameof(message));
            AssertValidUnconnectedLength(message);

            var address = NetUtility.Resolve(host);
            if (address == null)
                throw new LidgrenException("Failed to resolve " + host.ToString());

            var recipient = new IPEndPoint(address, port);
            SendUnconnectedMessageCore(message, recipient);
        }

        /// <summary>
        /// Send a message to an unconnected host.
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage message, IPEndPoint recipient)
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));
            
            AssertValidUnconnectedLength(message);
            message.AssertNotSent(nameof(message));

            SendUnconnectedMessageCore(message, recipient);
        }

        /// <summary>
        /// Send a message to an unconnected host.
        /// </summary>
        public void SendUnconnectedMessage(NetOutgoingMessage message, IReadOnlyCollection<IPEndPoint> recipients)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            AssertValidRecipients(recipients, nameof(recipients));
            AssertValidUnconnectedLength(message);
            message.AssertNotSent(nameof(message));

            message._messageType = NetMessageType.Unconnected;
            message._isSent = true;

            Interlocked.Add(ref message._recyclingCount, recipients.Count);
            foreach (IPEndPoint endPoint in recipients.AsListEnumerator())
            {
                if (endPoint == null)
                    throw new InvalidOperationException("Null recipient endpoint.");
                UnsentUnconnectedMessages.Enqueue((endPoint, message));
            }
        }

        /// <summary>
        /// Send a message to this exact same netpeer (loopback).
        /// </summary>
        public void SendUnconnectedToSelf(NetOutgoingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            message.AssertNotSent(nameof(message));

            if (Socket == null)
                throw new InvalidOperationException("No socket bound.");

            message._messageType = NetMessageType.Unconnected;
            message._isSent = true;

            if (Configuration.IsMessageTypeEnabled(NetIncomingMessageType.UnconnectedData) == false)
                return; // dropping unconnected message since it's not enabled for receiving

            var om = CreateIncomingMessage(NetIncomingMessageType.UnconnectedData, message.ByteLength);
            om.Write(message);
            om.IsFragment = false;
            om.ReceiveTime = NetTime.Now;
            om.SenderConnection = null;
            om.SenderEndPoint = (IPEndPoint)Socket.LocalEndPoint;
            LidgrenException.Assert(om.BitLength == message.BitLength);

            ReleaseMessage(om);
        }
    }
}