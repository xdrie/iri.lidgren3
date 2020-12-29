﻿/* Copyright (c) 2010 Michael Lidgren

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
        private readonly NetConnection _connection;

        internal int _sentPackets;
        internal int _receivedPackets;

        internal int _sentMessages;
        internal int _receivedMessages;

        internal int _receivedFragments;

        internal int _sentBytes;
        internal int _receivedBytes;

        internal int _resentMessagesDueToDelay;
        internal int _resentMessagesDueToHole;

        internal NetConnectionStatistics(NetConnection connection)
        {
            _connection = connection;
            Reset();
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
            _resentMessagesDueToDelay = 0;
            _resentMessagesDueToHole = 0;
        }

        /// <summary>
        /// Gets the number of sent packets for this connection.
        /// </summary>
        public int SentPackets => _sentPackets;

        /// <summary>
        /// Gets the number of received packets for this connection.
        /// </summary>
        public int ReceivedPackets => _receivedPackets;

        /// <summary>
        /// Gets the number of sent bytes for this connection.
        /// </summary>
        public int SentBytes => _sentBytes;

        /// <summary>
        /// Gets the number of received bytes for this connection.
        /// </summary>
        public int ReceivedBytes => _receivedBytes;

        /// <summary>
        /// Gets the number of resent reliable messages for this connection.
        /// </summary>
        public int ResentMessages => _resentMessagesDueToHole + _resentMessagesDueToDelay;

        /// <summary>
        /// Gets the number of unsent messages currently in queue for this connection.
        /// </summary>
        public int QueuedMessages
        {
            get
            {
                int unsent = 0;
                foreach (NetSenderChannel sendChan in _connection._sendChannels)
                {
                    if (sendChan != null)
                        unsent += sendChan.QueuedSends.Count;
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
                foreach (NetSenderChannel sendChan in _connection._sendChannels)
                {
                    if (sendChan is NetReliableSenderChannel relSendChan)
                    {
                        for (int i = 0; i < relSendChan.StoredMessages.Length; i++)
                            if (relSendChan.StoredMessages[i].Message != null)
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
                foreach (NetReceiverChannel recChan in _connection._receiveChannels)
                {
                    if (!(recChan is NetReliableOrderedReceiver relRecChan))
                        continue;

                    foreach (var msg in relRecChan.WithheldMessages)
                        if (msg != null)
                            withheld++;
                }
                return withheld;
            }
        }

        // public double LastSendRespondedTo { get { return m_connection.m_lastSendRespondedTo; } }

        internal void PacketSent(int numBytes, int numMessages)
        {
            LidgrenException.Assert(numBytes > 0 && numMessages > 0);
            _sentPackets++;
            _sentBytes += numBytes;
            _sentMessages += numMessages;
        }

        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            LidgrenException.Assert(numBytes > 0 && numMessages > 0);
            _receivedPackets++;
            _receivedBytes += numBytes;
            _receivedMessages += numMessages;
            _receivedFragments += numFragments;
        }

        internal void MessageResent(MessageResendReason reason)
        {
            if (reason == MessageResendReason.Delay)
                _resentMessagesDueToDelay++;
            else
                _resentMessagesDueToHole++;
        }

        /// <summary>
        /// Returns a string that represents this object
        /// </summary>
        public override string ToString()
        {
            // TODO: add custom format support

            var sb = new StringBuilder();

            sb.AppendFormatLine("Average roundtrip time: {0}", NetTime.ToReadable(_connection.AverageRoundtripTime));
            sb.AppendFormatLine("Current MTU: {0}", _connection.CurrentMTU);

            sb.AppendFormatLine(
                "Sent {0} bytes in {1} messages in {2} packets",
                _sentBytes, _sentMessages, _sentPackets);

            sb.AppendFormatLine(
                "Received {0} bytes in {1} messages ({2} fragments) in {3} packets",
                _receivedBytes, _receivedMessages, _receivedFragments, _receivedPackets);

            sb.AppendLine();
            sb.AppendFormatLine("Queued: {0}", QueuedMessages);
            sb.AppendFormatLine("Stored: {0}", StoredMessages);
            sb.AppendFormatLine("Witheld: {0}", WithheldMessages);
            sb.AppendFormatLine("Resent (by delay): {0}", _resentMessagesDueToDelay);
            sb.AppendFormatLine("Resent (by hole): {0}", _resentMessagesDueToHole);

            return sb.ToString();
        }
    }
}
