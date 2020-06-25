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
    // TODO: check NetBuffer.Read
    // TODO: add PeekTimeSpan()

    public partial class NetBuffer
    {
        /// <summary>
        /// Reads a 1-bit <see cref="bool"/> without advancing the read pointer.
        /// </summary>
        public bool PeekBoolean()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 1, ReadOverflowError);
            return NetBitWriter.ReadByteUnchecked(Data, BitPosition, 1) > 0;
        }

        /// <summary>
        /// Reads a <see cref="byte"/> without advancing the read pointer.
        /// </summary>
        public byte PeekByte()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 8, ReadOverflowError);
            return NetBitWriter.ReadByteUnchecked(Data, BitPosition, 8);
        }

        /// <summary>
        /// Reads an <see cref="sbyte"/> without advancing the read pointer.
        /// </summary>
        [CLSCompliant(false)]
        public sbyte PeekSByte()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 8, ReadOverflowError);
            return (sbyte)NetBitWriter.ReadByteUnchecked(Data, BitPosition, 8);
        }

        /// <summary>
        /// Reads the specified number of bits into a <see cref="byte"/> without advancing the read pointer.
        /// </summary>
        public byte PeekByte(int bitCount)
        {
            return NetBitWriter.ReadByteUnchecked(Data, BitPosition, bitCount);
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer.
        /// </summary>
        [Obsolete("This method allocates a new array for each call.")]
        public byte[] PeekBytes(int numberOfBytes)
        {
            LidgrenException.Assert(_bitLength - BitPosition >= (numberOfBytes * 8), ReadOverflowError);

            byte[] retval = new byte[numberOfBytes];
            // TODO NetBitWriter.ReadBytes(m_data, _readPosition, retval);
            throw new NotImplementedException();
            return retval;
        }

        /// <summary>
        /// Tries to read the specified number of bits without advancing the read pointer.
        /// </summary>
        public bool TryPeekBits(Span<byte> span, int bitCount)
        {
            if (_bitLength - BitPosition >= bitCount)
                return false;

            throw new NotImplementedException();
            // TODO NetBitWriter.ReadBytes(m_data, _readPosition, span);
            return true;
        }

        /// <summary>
        /// Reads the specified number of bits without advancing the read pointer.
        /// </summary>
        public void PeekBits(Span<byte> span, int bitCount)
        {
            if (!TryPeekBits(span, bitCount))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Tries to read the specified number of bytes without advancing the read pointer.
        /// </summary>
        public bool TryPeekBytes(Span<byte> span)
        {
            return TryPeekBits(span, span.Length * 8);
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer.
        /// </summary>
        public void PeekBytes(Span<byte> span)
        {
            if (!TryPeekBytes(span))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Reads an <see cref="short"/> without advancing the read pointer.
        /// </summary>
        public short PeekInt16()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 16, ReadOverflowError);
            return (short)NetBitWriter.ReadUInt16(Data, 16, BitPosition);
        }

        /// <summary>
        /// Reads a <see cref="ushort"/> without advancing the read pointer.
        /// </summary>
        [CLSCompliant(false)]
        public ushort PeekUInt16()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 16, ReadOverflowError);
            return NetBitWriter.ReadUInt16(Data, 16, BitPosition);
        }

        /// <summary>
        /// Reads an <see cref="int"/> without advancing the read pointer.
        /// </summary>
        public int PeekInt32()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 32, ReadOverflowError);
            return (int)NetBitWriter.ReadUInt32(Data, BitPosition, 32);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="int"/> without advancing the read pointer.
        /// </summary>
        public int PeekInt32(int numberOfBits)
        {
            LidgrenException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadInt() can only read between 1 and 32 bits");
            LidgrenException.Assert(_bitLength - BitPosition >= numberOfBits, ReadOverflowError);

            uint retval = NetBitWriter.ReadUInt32(Data, BitPosition, numberOfBits);

            if (numberOfBits == 32)
                return (int)retval;

            int signBit = 1 << (numberOfBits - 1);
            if ((retval & signBit) == 0)
                return (int)retval; // positive

            // negative
            unchecked
            {
                uint mask = ((uint)-1) >> (33 - numberOfBits);
                uint tmp = (retval & mask) + 1;
                return -(int)tmp;
            }
        }

        /// <summary>
        /// Reads a <see cref="uint"/> without advancing the read pointer.
        /// </summary>
        [CLSCompliant(false)]
        public uint PeekUInt32()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 32, ReadOverflowError);
            return NetBitWriter.ReadUInt32(Data, BitPosition, 32);
        }

        /// <summary>
        /// Reads the specified number of bits into a <see cref="uint"/> without advancing the read pointer.
        /// </summary>
        [CLSCompliant(false)]
        public uint PeekUInt32(int numberOfBits)
        {
            LidgrenException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadUInt() can only read between 1 and 32 bits");
            //NetException.Assert(m_bitLength - m_readBitPtr >= numberOfBits, "tried to read past buffer size");

            return NetBitWriter.ReadUInt32(Data, BitPosition, numberOfBits);
        }

        /// <summary>
        /// Reads a <see cref="ulong"/> without advancing the read pointer.
        /// </summary>
        [CLSCompliant(false)]
        public ulong PeekUInt64()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 64, ReadOverflowError);

            ulong low = NetBitWriter.ReadUInt32(Data, BitPosition, 32);
            ulong high = NetBitWriter.ReadUInt32(Data, BitPosition + 32, 32);

            return low + (high << 32);
        }

        /// <summary>
        /// Reads an <see cref="long"/> without advancing the read pointer.
        /// </summary>
        public long PeekInt64()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 64, ReadOverflowError);
            unchecked
            {
                return (long)PeekUInt64();
            }
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="ulong"/> without advancing the read pointer.
        /// </summary>
        [CLSCompliant(false)]
        public ulong PeekUInt64(int numberOfBits)
        {
            LidgrenException.Assert(numberOfBits > 0 && numberOfBits <= 64, "ReadUInt() can only read between 1 and 64 bits");
            LidgrenException.Assert(_bitLength - BitPosition >= numberOfBits, ReadOverflowError);

            if (numberOfBits <= 32)
            {
                return NetBitWriter.ReadUInt32(Data, BitPosition, numberOfBits);
            }
            else
            {
                uint v1 = NetBitWriter.ReadUInt32(Data, BitPosition, 32);
                uint v2 = NetBitWriter.ReadUInt32(Data, BitPosition, numberOfBits - 32);
                return (ulong)(v1 | ((long)v2 << 32));
            }
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="long"/> without advancing the read pointer.
        /// </summary>
        public long PeekInt64(int numberOfBits)
        {
            LidgrenException.Assert((numberOfBits > 0) && (numberOfBits < 65), "ReadInt64(bits) can only read between 1 and 64 bits");
            return (long)PeekUInt64(numberOfBits);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> without advancing the read pointer.
        /// </summary>
        public float PeekSingle()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 32, ReadOverflowError);

            if ((BitPosition & 7) == 0) // read directly
                return BitConverter.ToSingle(Data, BitPosition >> 3);

            byte[] bytes = PeekBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads a 64-bit <see cref="double"/> without advancing the read pointer.
        /// </summary>
        public double PeekDouble()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 64, ReadOverflowError);

            if ((BitPosition & 7) == 0) // read directly
                return BitConverter.ToDouble(Data, BitPosition >> 3);

            byte[] bytes = PeekBytes(8);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Reads a <see cref="string"/> without advancing the read pointer.
        /// </summary>
        public string PeekString()
        {
            int wasReadPosition = BitPosition;
            string str = ReadString();
            BitPosition = wasReadPosition;
            return str;
        }

        /// <summary>
        /// Reads a <see cref="TimeSpan"/> without advancing the read pointer.
        /// </summary>
        public TimeSpan PeekTimeSpan()
        {
            return new TimeSpan(PeekVarInt64());
        }

        /// <summary>
        /// Reads an enum of type <typeparamref name="TEnum"/> without advancing the read pointer.
        /// </summary>
        public TEnum PeekEnum<TEnum>()
            where TEnum : Enum
        {
            long value = PeekVarInt64();
            return EnumConverter.Convert<TEnum>(value);
        }
    }
}

