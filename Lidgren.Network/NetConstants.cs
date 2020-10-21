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

using System;

namespace Lidgren.Network
{
    /// <summary>
    /// All the constants used when internally by the library.
    /// </summary>
    internal static class NetConstants
    {
        public const int UnreliableChannels = 1;
        public const int UnreliableSequencedChannels = 32;
        public const int ReliableUnorderedChannels = 1;
        public const int ReliableSequencedChannels = 32;
        public const int ReliableOrderedChannels = 32;
        public const int StreamChannels = 32;

        public const int TotalChannels =
            UnreliableChannels + UnreliableSequencedChannels +
            ReliableUnorderedChannels + ReliableSequencedChannels + ReliableOrderedChannels +
            StreamChannels;

        public const int SequenceNumbers = 1024;

        public const int HeaderByteSize = 5;

        public const int UnreliableWindowSize = 128;
        public const int DefaultWindowSize = 64;
        public const int ReliableOrderedWindowSize = DefaultWindowSize;
        public const int ReliableSequencedWindowSize = DefaultWindowSize;

        public const int MaxFragmentationGroups = ushort.MaxValue - 1;
        public const int UnfragmentedMessageHeaderSize = 5;

        public static void AssertValidDeliveryChannel(
            NetDeliveryMethod method, int sequenceChannel,
            string? methodParamName, string? channelParamName)
        {
            if (sequenceChannel < 0)
                throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);

            switch (method)
            {
                case NetDeliveryMethod.Unreliable:
                    if (sequenceChannel >= UnreliableChannels)
                        throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);
                    break;

                case NetDeliveryMethod.UnreliableSequenced:
                    if (sequenceChannel >= UnreliableSequencedChannels)
                        throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);
                    break;

                case NetDeliveryMethod.ReliableUnordered:
                    if (sequenceChannel >= ReliableUnorderedChannels)
                        throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);
                    break;

                case NetDeliveryMethod.ReliableSequenced:
                    if (sequenceChannel >= ReliableSequencedChannels)
                        throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);
                    break;

                case NetDeliveryMethod.ReliableOrdered:
                    if (sequenceChannel >= ReliableOrderedChannels)
                        throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);
                    break;

                case NetDeliveryMethod.Stream:
                    if (sequenceChannel >= StreamChannels)
                        throw new ArgumentOutOfRangeException(channelParamName, sequenceChannel, null);
                    break;

                default:
                case NetDeliveryMethod.Unknown:
                    throw new ArgumentOutOfRangeException(methodParamName, method, null);
            }
        }
    }
}
