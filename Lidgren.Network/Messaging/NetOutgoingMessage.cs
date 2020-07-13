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
using System.Diagnostics;

namespace Lidgren.Network
{
    /// <summary>
    /// Outgoing message used to send data to remote peers.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class NetOutgoingMessage : NetBuffer
    {
        internal NetMessageType _messageType;
        internal bool _isSent; // TODO: create better error/assert for this
        internal int _recyclingCount;

        internal int _fragmentGroup;             // which group of fragments ths belongs to
        internal int _fragmentGroupTotalBits;    // total number of bits in this group
        internal int _fragmentChunkByteSize;	  // size, in bytes, of every chunk but the last one
        internal int _fragmentChunkNumber;       // which number chunk this is, starting with 0

        internal string DebuggerDisplay => $"BitLength = {BitLength}";

        public NetOutgoingMessage(byte[]? buffer) : base(buffer)
        {
        }

        internal void Reset()
        {
            _messageType = NetMessageType.LibraryError;
            _isSent = false;
            _recyclingCount = 0;
            _fragmentGroup = 0;
            BitLength = 0;
        }

        internal int Encode(Span<byte> destination, int offset, int sequenceNumber)
        {
            //  8 bits - NetMessageType
            //  1 bit  - Fragment?
            // 15 bits - Sequence number
            // 16 bits - Payload length in bits

            destination[offset++] = (byte)_messageType;

            int low = (sequenceNumber << 1) | (_fragmentGroup == 0 ? 0 : 1);
            destination[offset++] = (byte)low;
            destination[offset++] = (byte)(sequenceNumber >> 7);

            if (_fragmentGroup == 0)
            {
                destination[offset++] = (byte)BitLength;
                destination[offset++] = (byte)(BitLength >> 8);

                int byteLen = NetBitWriter.ByteCountForBits(BitLength);
                Span.Slice(0, byteLen).CopyTo(destination.Slice(offset));
                offset += byteLen;
            }
            else
            {
                int offsetBase = offset;
                destination[offset++] = (byte)BitLength;
                destination[offset++] = (byte)(BitLength >> 8);

                //
                // write fragmentation header
                //
                offset = NetFragmentationHelper.WriteHeader(
                    destination, offset,
                    _fragmentGroup, _fragmentGroupTotalBits, _fragmentChunkByteSize, _fragmentChunkNumber);
                int hdrLen = offset - offsetBase - 2;

                // update length
                int realBitLength = BitLength + (hdrLen * 8);
                destination[offsetBase] = (byte)realBitLength;
                destination[offsetBase + 1] = (byte)(realBitLength >> 8);

                int byteLen = NetBitWriter.ByteCountForBits(BitLength);
                Span.Slice(_fragmentChunkNumber * _fragmentChunkByteSize, byteLen).CopyTo(destination.Slice(offset));
                offset += byteLen;
            }

            LidgrenException.Assert(offset > 0);
            return offset;
        }

        internal void AssertNotSent(string? paramName = null)
        {
            if (_isSent)
                throw new CannotResendException(paramName);
        }

        internal int GetEncodedSize()
        {
            int size = NetConstants.UnfragmentedMessageHeaderSize; // regular headers
            if (_fragmentGroup != 0)
            {
                size += NetFragmentationHelper.GetFragmentationHeaderSize(
                    _fragmentGroup, _fragmentGroupTotalBits / 8, _fragmentChunkByteSize, _fragmentChunkNumber);
            }
            size += ByteLength;
            return size;
        }

        /// <summary>
        /// Encrypt this message using the provided algorithm.
        /// No more writing can be done before sending it or the message will be corrupt.
        /// </summary>
        public bool Encrypt(NetEncryption encryption)
        {
            if (encryption == null)
                throw new ArgumentNullException(nameof(encryption));

            return encryption.Encrypt(this);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this object.
        /// </summary>
        public override string ToString()
        {
            if (_isSent)
                return "{NetOutgoingMessage: " + _messageType + ", " + ByteLength + " bytes}";

            return "{NetOutgoingMessage: " + ByteLength + " bytes}";
        }
    }
}
