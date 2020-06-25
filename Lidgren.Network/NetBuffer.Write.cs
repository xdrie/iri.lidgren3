//#define UNSAFE
//#define BIGENDIAN
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
using System.Net;
using System.Text;
using System.Buffers.Binary;

namespace Lidgren.Network
{
    public partial class NetBuffer
    {
        // TODO: pool arrays in certain scenarios dictated by config

        /// <summary>
        /// Ensures the buffer can hold this number of bits with overallocating.
        /// </summary>
        internal void EnsureBufferSize(int bitCount, int extraByteGrowSize)
        {
            int byteLength = (bitCount + 7) / 8;

            if (Data == null || Data.Length < byteLength)
            {
                var newBuffer = new byte[byteLength + extraByteGrowSize];
                Data.AsMemory(0, ByteLength).CopyTo(newBuffer);
                Data = newBuffer;
            }
        }

        /// <summary>
        /// Ensures the buffer can hold this number of bits.
        /// </summary>
        public void EnsureBufferSize(int bitCount)
        {
            EnsureBufferSize(bitCount, ExtraGrowAmount);
        }

        internal void ExpandBufferSize(int bitCount, int maxBitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            EnsureBufferSize(_bitLength + bitCount);
        }

        /// <summary>
        /// Writes an amount of bits from a span into a specified part of the buffer.
        /// </summary>
        public void WriteBitsAt(ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount, int destinationBitOffset)
        {
            if (source.IsEmpty)
                return;

            int newBitLength = Math.Max(_bitLength, destinationBitOffset + 16);
            EnsureBufferSize(newBitLength);
            NetBitWriter.CopyBits(source, sourceBitOffset, bitCount, Data, destinationBitOffset);
            _bitLength = newBitLength;
        }

        /// <summary>
        /// Writes an amount of bits from a span into a specified part of the buffer.
        /// </summary>
        public void WriteBitsAt(ReadOnlySpan<byte> source, int bitCount, int destinationBitOffset)
        {
            WriteBitsAt(source, 0, bitCount, destinationBitOffset);
        }

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        public void WriteBits(ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount)
        {
            if (source.IsEmpty)
                return;

            EnsureBufferSize(_bitLength + bitCount);
            NetBitWriter.CopyBits(source, sourceBitOffset, bitCount, Data, _bitLength);
            _bitLength += bitCount;
        }

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        public void WriteBits(ReadOnlySpan<byte> source, int bitCount)
        {
            WriteBits(source, 0, bitCount);
        }

        /// <summary>
        /// Writes bytes from a span.
        /// </summary>
        public void Write(ReadOnlySpan<byte> source)
        {
            WriteBits(source, source.Length * 8);
        }

        #region Write(value)

        /// <summary>
        /// Writes a <see cref="bool"/> value using 1 bit.
        /// </summary>
        public void Write(bool value)
        {
            EnsureBufferSize(_bitLength + 1);
            NetBitWriter.WriteByteUnchecked(value ? 1 : 0, 1, Data, _bitLength);
            _bitLength += 1;
        }

        /// <summary>
        /// Writes a <see cref="sbyte"/>.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(sbyte source)
        {
            EnsureBufferSize(_bitLength + 8);
            NetBitWriter.WriteByteUnchecked((byte)source, 8, Data, _bitLength);
            _bitLength += 8;
        }

        /// <summary>
        /// Write a <see cref="byte"/>.
        /// </summary>
        public void Write(byte source)
        {
            EnsureBufferSize(_bitLength + 8);
            NetBitWriter.WriteByteUnchecked(source, 8, Data, _bitLength);
            _bitLength += 8;
        }

        /// <summary>
        /// Writes a 16-bit <see cref="short"/>.
        /// </summary>
        public void Write(short value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(tmp, value);
            WriteBits(tmp, 0, tmp.Length * 8);
        }

        /// <summary>
        /// Writes an 16-bit <see cref="ushort"/>.
        /// </summary>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public void Write(ushort value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            WriteBits(tmp, 0, tmp.Length * 8);
        }

        /// <summary>
        /// Writes a 32-bit <see cref="int"/>.
        /// </summary>
        public void Write(int value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
            WriteBits(tmp, 0, tmp.Length * 8);
        }

        /// <summary>
        /// Writes a 32-bit <see cref="uint"/>.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(uint value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(tmp, value);
            WriteBits(tmp, 0, tmp.Length * 8);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="long"/>.
        /// </summary>
        public void Write(long value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(tmp, value);
            WriteBits(tmp, 0, tmp.Length * 8);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="ulong"/>.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(ulong value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(tmp, value);
            WriteBits(tmp, 0, tmp.Length * 8);
        }

        /// <summary>
        /// Writes a 32-bit <see cref="float"/>.
        /// </summary>
        public void Write(float value)
        {
            int intValue = BitConverter.SingleToInt32Bits(value);
            Write(intValue);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="double"/>.
        /// </summary>
        public void Write(double value)
        {
            long intValue = BitConverter.DoubleToInt64Bits(value);
            Write(intValue);
        }

        #endregion

        #region WriteAt(bitOffset, value)

        /// <summary>
        /// Writes a 16-bit <see cref="uint"/> at a given offset in the buffer.
        /// </summary>
        [CLSCompliant(false)]
        public void WriteAt(int bitOffset, ushort source)
        {
            int newBitLength = Math.Max(_bitLength, bitOffset + 16);
            EnsureBufferSize(newBitLength);
            NetBitWriter.WriteUInt16(source, 16, Data, bitOffset);
            _bitLength = newBitLength;
        }

        #endregion

        /// <summary>
        /// Writes a <see cref="byte"/> using 1 to 8 bits.
        /// </summary>
        public void Write(byte source, int bitCount)
        {
            ExpandBufferSize(bitCount, 8);
            // call the unchecked write as we check beforehand
            NetBitWriter.WriteByteUnchecked(source, bitCount, Data, _bitLength);
            _bitLength += bitCount;
        }

        /// <summary>
        /// Writes an unsigned <see cref="ushort"/> using 1 to 16 bits.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(ushort source, int bitCount)
        {
            ExpandBufferSize(bitCount, 16);
            NetBitWriter.WriteUInt16(source, bitCount, Data, _bitLength);
            _bitLength += bitCount;
        }

        /// <summary>
        /// Writes a 16-bit <see cref="short"/> at a given bit offset in the buffer.
        /// </summary>
        public void WriteAt(int offset, short value)
        {
            int newBitLength = Math.Max(_bitLength, offset + 16);
            EnsureBufferSize(newBitLength);
            NetBitWriter.WriteUInt16((ushort)value, 16, Data, offset);
            _bitLength = newBitLength;
        }

        /// <summary>
        /// Writes a 32-bit <see cref="int"/> at a given bit offset in the buffer.
        /// </summary>
        public void WriteAt(int bitOffset, int value)
        {
            int newBitLength = Math.Max(_bitLength, bitOffset + 32);
            EnsureBufferSize(newBitLength);
            NetBitWriter.WriteUInt32((uint)value, 32, Data, bitOffset);
            _bitLength = newBitLength;
        }

        /// <summary>
        /// Writes a 32-bit <see cref="uint"/> at a given offset in the buffer.
        /// </summary>
        [CLSCompliant(false)]
        public void WriteAt(int offset, uint source)
        {
            int newBitLength = Math.Max(_bitLength, offset + 32);
            EnsureBufferSize(newBitLength);
            NetBitWriter.WriteUInt32(source, 32, Data, offset);
            _bitLength = newBitLength;
        }

        /// <summary>
        /// Writes a <see cref="uint"/> using 1 to 32 bits.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(uint source, int bitCount)
        {
            ExpandBufferSize(bitCount, 32);
            NetBitWriter.WriteUInt32(source, bitCount, Data, _bitLength);
            _bitLength += bitCount;
        }

        /// <summary>
        /// Writes a <see cref="int"/> using 1 to 32 bits.
        /// </summary>
        public void Write(int source, int bitCount)
        {
            ExpandBufferSize(bitCount, 32);

            if (bitCount != 32)
            {
                // make first bit sign
                int signBit = 1 << (bitCount - 1);
                if (source < 0)
                    source = (-source - 1) | signBit;
                else
                    source &= ~signBit;
            }

            Span<byte> tmp = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(tmp, source);
            WriteBits(tmp, bitCount);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="ulong"/> at a given offset in the buffer.
        /// </summary>
        [CLSCompliant(false)]
        public void WriteAt(int offset, ulong source)
        {
            int newBitLength = Math.Max(_bitLength, offset + 64);
            EnsureBufferSize(newBitLength);
            NetBitWriter.WriteUInt64(source, 64, Data, offset);
            _bitLength = newBitLength;
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using 1 to 64 bits
        /// </summary>
        [CLSCompliant(false)]
        public void Write(ulong source, int bitCount)
        {
            ExpandBufferSize(bitCount, 64);
            NetBitWriter.WriteUInt64(source, bitCount, Data, _bitLength);
            _bitLength += bitCount;
        }

        /// <summary>
        /// Writes a <see cref="long"/> using 1 to 64 bits.
        /// </summary>
        public void Write(long source, int bitCount)
        {
            ExpandBufferSize(bitCount, 64);
            ulong usource = (ulong)source;
            NetBitWriter.WriteUInt64(usource, bitCount, Data, _bitLength);
            _bitLength += bitCount;
        }

        #region WriteVar

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="ulong"/> of up to 64 bits.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        [CLSCompliant(false)]
        public int WriteVar(ulong value)
        {
            int retval = 1;
            ulong num1 = value;
            while (num1 >= 0x80)
            {
                Write((byte)(num1 | 0x80));
                num1 >>= 7;
                retval++;
            }

            Write((byte)num1);
            return retval;
        }

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="long"/> of up to 64 bits.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        public int WriteVar(long value)
        {
            ulong zigzag = (ulong)(value << 1) ^ (ulong)(value >> 63);
            return WriteVar(zigzag);
        }

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="uint"/> of up to 32 bits.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        [CLSCompliant(false)]
        public int WriteVar(uint value)
        {
            return WriteVar((ulong)value);
        }

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="int"/> of up to 32 bits.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        public int WriteVar(int value)
        {
            uint zigzag = (uint)(value << 1) ^ (uint)(value >> 31);
            return WriteVar(zigzag);
        }

        #endregion

        /// <summary>
        /// Compress (lossy) a <see cref="float"/> in the range -1..1 using the specified amount of bits.
        /// </summary>
        public void WriteSigned(float value, int bitCount)
        {
            LidgrenException.Assert(
                (value >= -1.0) && (value <= 1.0), " WriteSignedSingle() must be passed a float in the range -1 to 1; val is " + value);

            float unit = (value + 1f) * 0.5f;
            int maxVal = (1 << bitCount) - 1;
            uint writeVal = (uint)(unit * maxVal);

            Write(writeVal, bitCount);
        }

        /// <summary>
        /// Compress (lossy) a <see cref="float"/> in the range 0..1 using the specified amount of bits.
        /// </summary>
        public void WriteUnit(float value, int bitCount)
        {
            LidgrenException.Assert(
                (value >= 0.0) && (value <= 1.0), " WriteUnitSingle() must be passed a float in the range 0 to 1; val is " + value);

            int maxValue = (1 << bitCount) - 1;
            uint writeVal = (uint)(value * maxValue);

            Write(writeVal, bitCount);
        }

        /// <summary>
        /// Compress a <see cref="float"/> within a specified range using the specified amount of bits.
        /// </summary>
        public void WriteRanged(float value, float min, float max, int bitCount)
        {
            LidgrenException.Assert(
                (value >= min) && (value <= max), " WriteRangedSingle() must be passed a float in the range MIN to MAX; val is " + value);

            float range = max - min;
            float unit = (value - min) / range;
            int maxVal = (1 << bitCount) - 1;

            Write((uint)(maxVal * unit), bitCount);
        }

        /// <summary>
        /// Writes an <see cref="int"/> with the least amount of bits needed for the specified range.
        /// Returns the number of bits written.
        /// </summary>
        public int WriteRanged(int min, int max, int value)
        {
            LidgrenException.Assert(value >= min && value <= max, "Value not within min/max range!");

            uint range = (uint)(max - min);
            int numBits = NetUtility.BitCountForValue(range);

            uint rvalue = (uint)(value - min);
            Write(rvalue, numBits);

            return numBits;
        }

        /// <summary>
        /// Write characters from a span, readable as a string.
        /// </summary>
        public void Write(ReadOnlySpan<char> source)
        {
            if (source.IsEmpty)
            {
                WriteVar((uint)0);
                return;
            }

            // TODO: improve this by not allocating array (use stackalloced buffer and arraypooling)
            int byteCount = StringEncoding.GetByteCount(source);
            var buffer = byteCount < 4096 ? stackalloc byte[byteCount] : new byte[byteCount];

            if (StringEncoding.GetBytes(source, buffer) != byteCount)
                throw new Exception();

            EnsureBufferSize(_bitLength + (4 + byteCount) * 8);
            WriteVar((uint)byteCount);
            WriteVar((uint)source.Length);
            Write(buffer);
        }

        /// <summary>
        /// Writes a <see cref="string"/>.
        /// </summary>
        public void Write(string source)
        {
            Write(source.AsSpan());
        }

        /// <summary>
        /// Writes an <see cref="IPAddress"/> .
        /// </summary>
        public void Write(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            Span<byte> tmp = stackalloc byte[16];
            if (!address.TryWriteBytes(tmp, out int count))
                throw new ArgumentException("Failed to get address bytes.");
            tmp = tmp.Slice(0, count);

            Write((byte)tmp.Length);
            Write(tmp);
        }

        /// <summary>
        /// Writes an <see cref="IPEndPoint"/> description.
        /// </summary>
        public void Write(IPEndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));

            Write(endPoint.Address);
            Write((ushort)endPoint.Port);
        }

        /// <summary>
        /// Writes a <see cref="TimeSpan"/>.
        /// </summary>
        public void Write(TimeSpan time)
        {
            WriteVar(time.Ticks);
        }

        /// <summary>
        /// Writes the current local time to a message; 
        /// readable by the remote host using <see cref="ReadLocalTime"/>.
        /// </summary>
        public void WriteLocalTime()
        {
            Write(NetTime.Now);
        }

        /// <summary>
        /// Append all the bits from a <see cref="NetBuffer"/> to this buffer.
        /// </summary>
        public void Write(NetBuffer buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            EnsureBufferSize(BitLength + buffer.BitLength);
            WriteBits(buffer.Data, BitLength, buffer.BitLength);
        }

        public void Write<TEnum>(TEnum value)
            where TEnum : Enum
        {
            WriteVar(EnumConverter.Convert(value));
        }

        /// <summary>
        /// Byte-aligns the write position, 
        /// decreasing work for subsequent writes if the position was not aligned.
        /// </summary>
        public void WritePadBits()
        {
            _bitLength = (_bitLength + 7) / 8 * 8;
            EnsureBufferSize(_bitLength);
        }

        /// <summary>
        /// Pads the write position with the specified number of bits.
        /// </summary>
        public void WritePadBits(int bitCount)
        {
            _bitLength += bitCount;
            EnsureBufferSize(_bitLength);
        }
    }
}
