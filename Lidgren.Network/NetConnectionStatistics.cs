/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Text;

namespace Lidgren.Network
{
    /// <summary>
    /// Statistics for a <see cref="NetConnection"/> instance.
    /// </summary>
    public sealed class NetConnectionStatistics
    {
        private readonly NetConnection m_connection;

        internal int m_sentPackets;
        internal int m_receivedPackets;

        internal int m_sentMessages;
        internal int m_receivedMessages;

        internal int m_receivedFragments;

        internal int m_sentBytes;
        internal int m_receivedBytes;

        internal int m_resentMessagesDueToDelay;
        internal int m_resentMessagesDueToHole;

        internal NetConnectionStatistics(NetConnection conn)
        {
            m_connection = conn;
            Reset();
        }

        internal void Reset()
        {
            m_sentPackets = 0;
            m_receivedPackets = 0;
            m_sentMessages = 0;
            m_receivedMessages = 0;
            m_receivedFragments = 0;
            m_sentBytes = 0;
            m_receivedBytes = 0;
            m_resentMessagesDueToDelay = 0;
            m_resentMessagesDueToHole = 0;
        }

        /// <summary>
        /// Gets the number of sent packets for this connection.
        /// </summary>
        public int SentPackets => m_sentPackets;

        /// <summary>
        /// Gets the number of received packets for this connection.
        /// </summary>
        public int ReceivedPackets => m_receivedPackets;

        /// <summary>
        /// Gets the number of sent bytes for this connection.
        /// </summary>
        public int SentBytes => m_sentBytes;

        /// <summary>
        /// Gets the number of received bytes for this connection.
        /// </summary>
        public int ReceivedBytes => m_receivedBytes;

        /// <summary>
        /// Gets the number of resent reliable messages for this connection.
        /// </summary>
        public int ResentMessages => m_resentMessagesDueToHole + m_resentMessagesDueToDelay;

        /// <summary>
        /// Gets the number of unsent messages currently in queue for this connection.
        /// </summary>
        public int QueuedMessages
        {
            get
            {
                int unsent = 0;
                foreach (NetSenderChannelBase sendChan in m_connection.m_sendChannels)
                {
                    if (sendChan != null)
                        unsent += sendChan.m_queuedSends.Count;
                }
                return unsent;
            }
        }

        /// <summary>
        /// Gets the number of reliable messages buffered for this connection.
        /// </summary>
        public int StoredMessages
        {
            get
            {
                int stored = 0;
                foreach (NetSenderChannelBase sendChan in m_connection.m_sendChannels)
                {
                    if (sendChan is NetReliableSenderChannel relSendChan)
                    {
                        for (int i = 0; i < relSendChan.m_storedMessages.Length; i++)
                            if (relSendChan.m_storedMessages[i].Message != null)
                                stored++;
                    }
                }
                return stored;
            }
        }

        /// <summary>
        /// Gets the number of received reliable messages that are buffered for this connection.
        /// </summary>
        public int WithheldMessages
        {
            get
            {
                int withheld = 0;
                foreach (NetReceiverChannelBase recChan in m_connection.m_receiveChannels)
                {
                    if (recChan is NetReliableOrderedReceiver relRecChan)
                    {
                        for (int i = 0; i < relRecChan.m_withheldMessages.Length; i++)
                            if (relRecChan.m_withheldMessages[i] != null)
                                withheld++;
                    }
                }
                return withheld;
            }
        }

        // public double LastSendRespondedTo { get { return m_connection.m_lastSendRespondedTo; } }

        internal void PacketSent(int numBytes, int numMessages)
        {
            NetException.Assert(numBytes > 0 && numMessages > 0);
            m_sentPackets++;
            m_sentBytes += numBytes;
            m_sentMessages += numMessages;
        }

        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            NetException.Assert(numBytes > 0 && numMessages > 0);
            m_receivedPackets++;
            m_receivedBytes += numBytes;
            m_receivedMessages += numMessages;
            m_receivedFragments += numFragments;
        }

        internal void MessageResent(MessageResendReason reason)
        {
            if (reason == MessageResendReason.Delay)
                m_resentMessagesDueToDelay++;
            else
                m_resentMessagesDueToHole++;
        }

        /// <summary>
        /// Returns a string that represents this object
        /// </summary>
        public override string ToString()
        {
            // TODO: add custom format support

            var sb = new StringBuilder();

            sb.AppendFormatLine("Average roundtrip time: {0}", NetTime.ToReadable(m_connection.AverageRoundtripTime));
            sb.AppendFormatLine("Current MTU: {0}", m_connection.m_currentMTU);

            sb.AppendFormatLine(
                "Sent {0} bytes in {1} messages in {2} packets", 
                m_sentBytes, m_sentMessages, m_sentPackets);

            sb.AppendFormatLine(
                "Received {0} bytes in {1} messages ({2} fragments) in {3} packets",
                m_receivedBytes, m_receivedMessages, m_receivedFragments, m_receivedPackets);

            sb.AppendLine();
            sb.AppendFormatLine("Queued: {0}", QueuedMessages);
            sb.AppendFormatLine("Stored: {0}", StoredMessages);
            sb.AppendFormatLine("Witheld: {0}", WithheldMessages);
            sb.AppendFormatLine("Resent (by delay): {0}", m_resentMessagesDueToDelay);
            sb.AppendFormatLine("Resent (by hole): {0}", m_resentMessagesDueToHole);

            return sb.ToString();
        }
    }
}
