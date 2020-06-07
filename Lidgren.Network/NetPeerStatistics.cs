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
    /// Statistics for a NetPeer instance
    /// </summary>
    public sealed class NetPeerStatistics
    {
        private readonly NetPeer m_peer;

        internal int m_sentPackets;
        internal int m_receivedPackets;

        internal int m_sentMessages;
        internal int m_receivedMessages;
        internal int m_receivedFragments;

        internal int m_sentBytes;
        internal int m_receivedBytes;

        internal long m_totalBytesAllocated;

        internal NetPeerStatistics(NetPeer peer)
        {
            m_peer = peer;
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

            m_totalBytesAllocated = 0;
        }

        /// <summary>
        /// Gets the number of sent packets since the NetPeer was initialized
        /// </summary>
        public int SentPackets => m_sentPackets;

        /// <summary>
        /// Gets the number of received packets since the NetPeer was initialized
        /// </summary>
        public int ReceivedPackets => m_receivedPackets;

        /// <summary>
        /// Gets the number of sent messages since the NetPeer was initialized
        /// </summary>
        public int SentMessages => m_sentMessages;

        /// <summary>
        /// Gets the number of received messages since the NetPeer was initialized
        /// </summary>
        public int ReceivedMessages => m_receivedMessages;

        /// <summary>
        /// Gets the number of sent bytes since the NetPeer was initialized
        /// </summary>
        public int SentBytes => m_sentBytes;

        /// <summary>
        /// Gets the number of received bytes since the NetPeer was initialized
        /// </summary>
        public int ReceivedBytes => m_receivedBytes;

        /// <summary>
        /// Gets the number of bytes allocated (and possibly garbage collected) for message storage
        /// </summary>
        public long StorageBytesAllocated => m_totalBytesAllocated;

        /// <summary>
        /// Gets the number of bytes in the recycled pool
        /// </summary>
        public int BytesInRecyclePool => m_peer.m_bytesInPool;

        internal void PacketSent(int numBytes, int numMessages)
        {
            m_sentPackets++;
            m_sentBytes += numBytes;
            m_sentMessages += numMessages;
        }

        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            m_receivedPackets++;
            m_receivedBytes += numBytes;
            m_receivedMessages += numMessages;
            m_receivedFragments += numFragments;
        }

        /// <summary>
        /// Builds and returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormatLine("{0} active connections", m_peer.ConnectionCount);

            sb.AppendFormatLine(
                "Sent {0} bytes in {1} messages in {2} packets",
                m_sentBytes, m_sentMessages, m_sentPackets);

            sb.AppendFormatLine(
                "Received {0} bytes in {1} messages ({2} fragments) in {3} packets", 
                m_receivedBytes, m_receivedMessages, m_receivedFragments, m_receivedPackets);

            sb.AppendLine();
            sb.AppendFormatLine("Bytes in pool: {0}", BytesInRecyclePool);
            sb.AppendFormatLine("Total bytes allocated: {0} bytes", m_totalBytesAllocated);

            return sb.ToString();
        }
    }
}