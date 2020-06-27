using System;
using System.Threading;

namespace Lidgren.Network
{
    /// <summary>
    /// Sender part of Selective repeat ARQ for a particular NetChannel
    /// </summary>
    internal sealed class NetUnreliableSenderChannel : NetSenderChannel
    {
        private NetConnection m_connection;
        private int m_windowStart;
        private int m_windowSize;
        private int m_sendStart;

        private NetBitVector m_receivedAcks;

        public override int WindowSize => m_windowSize;

        public NetUnreliableSenderChannel(NetConnection connection, int windowSize)
        {
            m_connection = connection;
            m_windowSize = windowSize;
            m_windowStart = 0;
            m_sendStart = 0;
            m_receivedAcks = new NetBitVector(NetConstants.NumSequenceNumbers);
        }

        public override int GetAllowedSends()
        {
            int retval = m_windowSize - (m_sendStart + NetConstants.NumSequenceNumbers - m_windowStart) % m_windowSize;
            LidgrenException.Assert(retval >= 0 && retval <= m_windowSize);
            return retval;
        }

        public override void Reset()
        {
            m_receivedAcks.Clear();
            QueuedSends.Clear();
            m_windowStart = 0;
            m_sendStart = 0;
        }

        public override NetSendResult Enqueue(NetOutgoingMessage message)
        {
            int queueLen = QueuedSends.Count + 1;
            int left = GetAllowedSends();
            if (queueLen > left ||
                (message.ByteLength > m_connection.CurrentMTU && 
                m_connection._peerConfiguration.UnreliableSizeBehaviour == NetUnreliableSizeBehaviour.DropAboveMTU))
            {
                m_connection.Peer.Recycle(message);
                return NetSendResult.Dropped;
            }

            QueuedSends.Enqueue(message);
            return NetSendResult.Sent;
        }

        // call this regularely
        public override void SendQueuedMessages(TimeSpan now)
        {
            int num = GetAllowedSends();
            if (num < 1)
                return;

            // queued sends
            while (QueuedSends.Count > 0 && num > 0)
            {
                if (QueuedSends.TryDequeue(out NetOutgoingMessage? om))
                    ExecuteSend(om);
                num--;
            }
        }

        private void ExecuteSend(NetOutgoingMessage message)
        {
            m_connection.Peer.AssertIsOnLibraryThread();

            int seqNr = m_sendStart;
            m_sendStart = (m_sendStart + 1) % NetConstants.NumSequenceNumbers;

            m_connection.QueueSendMessage(message, seqNr);

            Interlocked.Decrement(ref message._recyclingCount);
            if (message._recyclingCount <= 0)
                m_connection.Peer.Recycle(message);

            return;
        }

        // remoteWindowStart is remote expected sequence number; everything below this has arrived properly
        // seqNr is the actual nr received
        public override void ReceiveAcknowledge(TimeSpan now, int seqNr)
        {
            // late (dupe), on time or early ack?
            int relate = NetUtility.RelativeSequenceNumber(seqNr, m_windowStart);

            if (relate < 0)
            {
                //m_connection.m_peer.LogDebug("Received late/dupe ack for #" + seqNr);
                return; // late/duplicate ack
            }

            if (relate == 0)
            {
                //m_connection.m_peer.LogDebug("Received right-on-time ack for #" + seqNr);

                // ack arrived right on time
                LidgrenException.Assert(seqNr == m_windowStart);

                m_receivedAcks[m_windowStart] = false;
                m_windowStart = (m_windowStart + 1) % NetConstants.NumSequenceNumbers;

                return;
            }

            // Advance window to this position
            m_receivedAcks[seqNr] = true;

            while (m_windowStart != seqNr)
            {
                m_receivedAcks[m_windowStart] = false;
                m_windowStart = (m_windowStart + 1) % NetConstants.NumSequenceNumbers;
            }
        }
    }
}
