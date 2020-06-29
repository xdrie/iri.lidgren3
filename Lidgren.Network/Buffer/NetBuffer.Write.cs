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
using System.Buffers.Binary;
using System.Text.Unicode;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
    public partial class NetBuffer
    {
        // TODO: pool arrays in certain scenarios dictated by config

        /// <summary>
        /// Ensures the buffer can hold this number of bits with overallocating.
        /// </summary>
        internal void EnsureCapacity(int bitCount, int extraByteGrowSize)
        {
            int byteLength = NetBitWriter.ByteCountForBits(bitCount);
            if (Data == null || Data.Length < byteLength)
                ByteCapacity = byteLength + extraByteGrowSize;
        }

        /// <summary>
        /// Ensures the buffer can hold this number of bits.
        /// </summary>
        public void EnsureCapacity(int bitCount)
        {
            EnsureCapacity(bitCount, ExtraGrowAmount);
        }

        /// <summary>
        /// Ensures the buffer can hold it's current bits and the given amount.
        /// </summary>
        public void EnsureEnoughCapacity(int bitCount)
        {
            EnsureCapacity(BitLength + bitCount);
        }

        public void EnsureEnoughCapacity(int bitCount, int maxBitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            EnsureEnoughCapacity(bitCount);
        }

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        public void Write(ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount)
        {
            if (source.IsEmpty)
                return;

            EnsureEnoughCapacity(bitCount);
            NetBitWriter.CopyBits(source, sourceBitOffset, bitCount, Data, BitPosition);
            IncrementBitPosition(bitCount);
        }

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        public void Write(ReadOnlySpan<byte> source, int bitCount)
        {
            Write(source, 0, bitCount);
        }

        /// <summary>
        /// Writes bytes from a span.
        /// </summary>
        public void Write(ReadOnlySpan<byte> source)
        {
            if (!IsByteAligned)
            {
                Write(source, source.Length * 8);
                return;
            }

            EnsureEnoughCapacity(source.Length * 8);
            source.CopyTo(Data.AsSpan(BytePosition));
            IncrementBitPosition(source.Length * 8);
        }

        #region Bool

        /// <summary>
        /// Writes a <see cref="bool"/> value using 1 bit.
        /// </summary>
        public void Write(bool value)
        {
            EnsureEnoughCapacity(1);
            NetBitWriter.WriteByteUnchecked(value ? 1 : 0, 1, Data, BitPosition);
            IncrementBitPosition(1);
        }

        #endregion

        #region Int8

        /// <summary>
        /// Writes a <see cref="sbyte"/>.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(sbyte value)
        {
            EnsureEnoughCapacity(8);
            NetBitWriter.WriteByteUnchecked((byte)value, 8, Data, BitPosition);
            IncrementBitPosition(8);
        }

        /// <summary>
        /// Write a <see cref="byte"/>.
        /// </summary>
        public void Write(byte value)
        {
            EnsureEnoughCapacity(8);
            NetBitWriter.WriteByteUnchecked(value, 8, Data, BitPosition);
            IncrementBitPosition(8);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> using 1 to 8 bits.
        /// </summary>
        public void Write(byte source, int bitCount)
        {
            EnsureEnoughCapacity(bitCount, 8);
            NetBitWriter.WriteByteUnchecked(source, bitCount, Data, BitPosition);
            IncrementBitPosition(bitCount);
        }

        #endregion

        #region Int16

        /// <summary>
        /// Writes a 16-bit <see cref="short"/>.
        /// </summary>
        public void Write(short value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(tmp, value);
            Write(tmp);
        }

        /// <summary>
        /// Writes an 16-bit <see cref="ushort"/>.
        /// </summary>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public void Write(ushort value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(tmp, value);
            Write(tmp);
        }

        #endregion

        #region Int32

        /// <summary>
        /// Writes a 32-bit <see cref="int"/>.
        /// </summary>
        public void Write(int value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
            Write(tmp);
        }

        /// <summary>
        /// Writes a 32-bit <see cref="uint"/>.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(uint value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(tmp, value);
            Write(tmp);
        }

        #endregion

        #region Int64

        /// <summary>
        /// Writes a 64-bit <see cref="long"/>.
        /// </summary>
        public void Write(long value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(tmp, value);
            Write(tmp);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="ulong"/>.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(ulong value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(tmp, value);
            Write(tmp);
        }

        #endregion

        #region Float

        /// <summary>
        /// Writes a 32-bit <see cref="float"/>.
        /// </summary>
        public void Write(float value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(float)];
            Unsafe.As<byte, float>(ref MemoryMarshal.GetReference(tmp)) = value;
            Write(tmp);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="double"/>.
        /// </summary>
        public void Write(double value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(double)];
            Unsafe.As<byte, double>(ref MemoryMarshal.GetReference(tmp)) = value;
            Write(tmp);
        }

        #endregion

        /// <summary>
        /// Writes an unsigned <see cref="ushort"/> using 1 to 16 bits.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(ushort value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(tmp, value);
            Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes a <see cref="uint"/> using 1 to 32 bits.
        /// </summary>
        [CLSCompliant(false)]
        public void Write(uint value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(tmp, value);
            Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes a <see cref="int"/> using 1 to 32 bits.
        /// </summary>
        public void Write(int value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            if (bitCount != tmp.Length * 8)
            {
                // make first bit sign
                int signBit = 1 << (bitCount - 1);
                if (value < 0)
                    value = (-value - 1) | signBit;
                else
                    value &= ~signBit;
            }

            BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
            Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using 1 to 64 bits
        /// </summary>
        [CLSCompliant(false)]
        public void Write(ulong value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(tmp, value);
            Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes a <see cref="long"/> using 1 to 64 bits.
        /// </summary>
        public void Write(long value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            if (bitCount != tmp.Length * 8)
            {
                // make first bit sign
                long signBit = 1 << (bitCount - 1);
                if (value < 0)
                    value = (-value - 1) | signBit;
                else
                    value &= ~signBit;
            }

            BinaryPrimitives.WriteInt64LittleEndian(tmp, value);
            Write(tmp, bitCount);
        }

        #region VarInt

        /// <summary>
        /// Write variable sized <see cref="ulong"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        [CLSCompliant(false)]
        public int WriteVar(ulong value)
        {
            Span<byte> tmp = stackalloc byte[NetBitWriter.MaxVarInt64Size];

            int offset = 0;
            ulong bits = value;
            while (bits > 0x7Fu)
            {
                tmp[offset++] = (byte)(bits | ~0x7Fu);
                bits >>= 7;
            }

            tmp[offset++] = (byte)bits;

            Write(tmp.Slice(0, offset));
            return offset;
        }

        /// <summary>
        /// Write variable sized <see cref="long"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        public int WriteVar(long value)
        {
            ulong zigzag = (ulong)(value << 1) ^ (ulong)(value >> 63);
            return WriteVar(zigzag);
        }

        /// <summary>
        /// Write variable sized <see cref="uint"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        [CLSCompliant(false)]
        public int WriteVar(uint value)
        {
            Span<byte> tmp = stackalloc byte[NetBitWriter.MaxVarInt32Size];

            int offset = 0;
            uint bits = value;
            while (bits > 0x7Fu)
            {
                tmp[offset++] = (byte)(bits | ~0x7Fu);
                bits >>= 7;
            }

            tmp[offset++] = (byte)bits;

            Write(tmp.Slice(0, offset));
            return offset;
        }

        /// <summary>
        /// Write variable sized <see cref="int"/>.
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
            int numBits = NetBitWriter.BitCountForValue(range);

            uint rvalue = (uint)(value - min);
            Write(rvalue, numBits);

            return numBits;
        }

        /// <summary>
        /// Write characters from a span, readable as a string.
        /// </summary>
        public void Write(ReadOnlySpan<char> source)
        {
            var initialHeader = new NetStringHeader(source.Length, null);
            if (source.IsEmpty)
            {
                Write(NetStringHeader.Empty);
                return;
            }

            int startPosition = BitPosition;
            BitPosition += initialHeader.Size * 8;

            Span<byte> buffer = stackalloc byte[4096];
            int totalBytesWritten = 0;
            var charSource = source;
            while (charSource.Length > 0)
            {
                var status = Utf8.FromUtf16(charSource, buffer, out int charsRead, out int bytesWritten, true, true);
                // TODO: check status

                charSource = charSource.Slice(charsRead);

                totalBytesWritten += bytesWritten;
                Write(buffer.Slice(0, bytesWritten));
            }

            if (charSource.Length > 0)
                throw new Exception();

            int endPosition = BitPosition;
            BitPosition = startPosition;
            Write(new NetStringHeader(source.Length, totalBytesWritten));
            BitPosition = endPosition;
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

            EnsureEnoughCapacity(buffer.BitLength);
            Write(buffer.Data, 0, buffer.BitLength);
        }

        public void Write<TEnum>(TEnum value)
            where TEnum : Enum
        {
            WriteVar(EnumConverter.Convert(value));
        }

        public void Write(NetStringHeader value)
        {
            WriteVar((uint)value.CharCount);

            if (value.CharCount == 0)
                return;

            // Both MaxByteCount and ByteCount should take the same amount
            // of bytes when encoded as variable ints.
            // That is why we need to check if ByteCount has to be "extended" with bytes.
            if (value.ByteCount == null)
            {
                WriteVar((uint)value.MaxByteCount);
            }
            else
            {
                WriteVar((uint)value.ByteCount);

                int byteCountVarSize = value.ByteCountVarSize;
                int sizeDiff = value.MaxByteCountVarSize - byteCountVarSize;
                if (sizeDiff > 0)
                {
                    BitPosition -= 1; // rewind to last byte's high bit
                    Write(true); // set high bit to true

                    // Write empty 7-bit values with high bit set.
                    // This should only write if size diff is more than 2, 
                    // which should realistically never occur.
                    for (int i = 1; i < sizeDiff - 1; i++)
                        Write((byte)0b1000_0000);

                    if (sizeDiff - 1 > 0)
                        Write((byte)0);
                }
            }
        }

        /// <summary>
        /// Byte-aligns the write position, 
        /// decreasing work for subsequent writes if the position was not aligned.
        /// </summary>
        public void WritePadBits()
        {
            BitPosition = NetBitWriter.ByteCountForBits(BitPosition) * 8;
            EnsureCapacity(BitPosition);
            SetLengthByPosition();
        }

        /// <summary>
        /// Pads the write position with the specified number of bits.
        /// </summary>
        public void WritePadBits(int bitCount)
        {
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            BitPosition += bitCount;
            EnsureCapacity(BitPosition);
            SetLengthByPosition();
        }
    }
}
