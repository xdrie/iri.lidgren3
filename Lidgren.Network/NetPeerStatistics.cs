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
    /// Statistics for a <see cref="NetPeer"/> instance.
    /// </summary>
    public sealed class NetPeerStatistics
    {
        private readonly NetPeer _peer;

        internal int _sentPackets;
        internal int _receivedPackets;

        internal int _sentMessages;
        internal int _receivedMessages;
        internal int _receivedFragments;

        internal int _sentBytes;
        internal int _receivedBytes;

        internal long _totalBytesAllocated;

        internal NetPeerStatistics(NetPeer peer)
        {
            _peer = peer;
        }

        internal void Reset()
        {
            _sentPackets = 0;
            _receivedPackets = 0;

            _sentMessages = 0;
            _receivedMessages = 0;
            _receivedFragments = 0;

            _sentBytes = 0;
            _receivedBytes = 0;

            _totalBytesAllocated = 0;
        }

        /// <summary>
        /// Gets the number of sent packets since the NetPeer was initialized.
        /// </summary>
        public int SentPackets => _sentPackets;

        /// <summary>
        /// Gets the number of received packets since the NetPeer was initialized.
        /// </summary>
        public int ReceivedPackets => _receivedPackets;

        /// <summary>
        /// Gets the number of sent messages since the NetPeer was initialized.
        /// </summary>
        public int SentMessages => _sentMessages;

        /// <summary>
        /// Gets the number of received messages since the NetPeer was initialized.
        /// </summary>
        public int ReceivedMessages => _receivedMessages;

        /// <summary>
        /// Gets the number of sent bytes since the NetPeer was initialized.
        /// </summary>
        public int SentBytes => _sentBytes;

        /// <summary>
        /// Gets the number of received bytes since the NetPeer was initialized.
        /// </summary>
        public int ReceivedBytes => _receivedBytes;

        /// <summary>
        /// Gets the number of bytes allocated (and possibly garbage collected) for message storage.
        /// </summary>
        public long StorageBytesAllocated => _totalBytesAllocated;

        /// <summary>
        /// Gets the number of bytes in the recycled pool.
        /// </summary>
        public int BytesInRecyclePool => _peer._bytesInPool;

        internal void PacketSent(int byteCount, int messageCount)
        {
            _sentPackets++;
            _sentBytes += byteCount;
            _sentMessages += messageCount;
        }

        internal void PacketReceived(int byteCount, int messageCount, int fragmentCount)
        {
            _receivedPackets++;
            _receivedBytes += byteCount;
            _receivedMessages += messageCount;
            _receivedFragments += fragmentCount;
        }

        /// <summary>
        /// Builds and returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormatLine("{0} active connections", _peer.ConnectionCount);

            sb.AppendFormatLine(
                "Sent {0} bytes in {1} messages in {2} packets",
                _sentBytes, _sentMessages, _sentPackets);

            sb.AppendFormatLine(
                "Received {0} bytes in {1} messages ({2} fragments) in {3} packets", 
                _receivedBytes, _receivedMessages, _receivedFragments, _receivedPackets);

            sb.AppendLine();
            sb.AppendFormatLine("Bytes in pool: {0}", BytesInRecyclePool);
            sb.AppendFormatLine("Total bytes allocated: {0} bytes", _totalBytesAllocated);

            return sb.ToString();
        }
    }
}