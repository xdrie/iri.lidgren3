using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace Lidgren.Network
{
    public static class BitBufferWriteExtensions
    {
        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount)
        {
            if (source.IsEmpty)
                return;

            buffer.EnsureEnoughBitCapacity(bitCount);
            NetBitWriter.CopyBits(source, sourceBitOffset, bitCount, buffer.GetBuffer(), buffer.BitPosition);
            buffer.IncrementBitPosition(bitCount);
        }

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IBitBuffer buffer, ReadOnlySpan<byte> source, int bitCount)
        {
            buffer.Write(source, 0, bitCount);
        }

        /// <summary>
        /// Writes bytes from a span.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, ReadOnlySpan<byte> source)
        {
            if (!buffer.IsByteAligned())
            {
                buffer.Write(source, 0, source.Length * 8);
                return;
            }

            buffer.EnsureEnoughBitCapacity(source.Length * 8);
            source.CopyTo(buffer.GetBuffer().AsSpan(buffer.BytePosition));
            buffer.IncrementBitPosition(source.Length * 8);
        }

        #region Bool

        /// <summary>
        /// Writes a <see cref="bool"/> value using 1 bit.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, bool value)
        {
            buffer.EnsureEnoughBitCapacity(1);
            NetBitWriter.WriteByteUnchecked(value ? 1 : 0, 1, buffer.GetBuffer(), buffer.BitPosition);
            buffer.IncrementBitPosition(1);
        }

        #endregion

        #region Int8

        /// <summary>
        /// Writes a <see cref="sbyte"/>.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, sbyte value)
        {
            buffer.EnsureEnoughBitCapacity(8);
            NetBitWriter.WriteByteUnchecked((byte)value, 8, buffer.GetBuffer(), buffer.BitPosition);
            buffer.IncrementBitPosition(8);
        }

        /// <summary>
        /// Write a <see cref="byte"/>.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, byte value)
        {
            buffer.EnsureEnoughBitCapacity(8);
            NetBitWriter.WriteByteUnchecked(value, 8, buffer.GetBuffer(), buffer.BitPosition);
            buffer.IncrementBitPosition(8);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> using 1 to 8 bits.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, byte source, int bitCount)
        {
            buffer.EnsureEnoughBitCapacity(bitCount, maxBitCount: 8);
            NetBitWriter.WriteByteUnchecked(source, bitCount, buffer.GetBuffer(), buffer.BitPosition);
            buffer.IncrementBitPosition(bitCount);
        }

        #endregion

        #region Int16

        /// <summary>
        /// Writes a 16-bit <see cref="short"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, short value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(tmp, value);
            buffer.Write(tmp);
        }

        /// <summary>
        /// Writes an 16-bit <see cref="ushort"/>.
        /// </summary>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, ushort value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(tmp, value);
            buffer.Write(tmp);
        }

        #endregion

        #region Int32

        /// <summary>
        /// Writes a 32-bit <see cref="int"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, int value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
            buffer.Write(tmp);
        }

        /// <summary>
        /// Writes a 32-bit <see cref="uint"/>.
        /// </summary>
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, uint value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(tmp, value);
            buffer.Write(tmp);
        }

        #endregion

        #region Int64

        /// <summary>
        /// Writes a 64-bit <see cref="long"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, long value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(tmp, value);
            buffer.Write(tmp);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="ulong"/>.
        /// </summary>
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, ulong value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(tmp, value);
            buffer.Write(tmp);
        }

        #endregion

        #region Float

        /// <summary>
        /// Writes a 32-bit <see cref="float"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, float value)
        {
            int intValue = BitConverter.SingleToInt32Bits(value);
            buffer.Write(intValue);
        }

        /// <summary>
        /// Writes a 64-bit <see cref="double"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, double value)
        {
            long intValue = BitConverter.DoubleToInt64Bits(value);
            buffer.Write(intValue);
        }

        #endregion

        /// <summary>
        /// Writes an unsigned <see cref="ushort"/> using 1 to 16 bits.
        /// </summary>
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, ushort value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(tmp, value);
            buffer.Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes a <see cref="uint"/> using 1 to 32 bits.
        /// </summary>
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, uint value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(tmp, value);
            buffer.Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes a <see cref="int"/> using 1 to 32 bits.
        /// </summary>
        public static void Write(this IBitBuffer buffer, int value, int bitCount)
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
            buffer.Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using 1 to 64 bits
        /// </summary>
        [CLSCompliant(false)]
        public static void Write(this IBitBuffer buffer, ulong value, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(tmp, value);
            buffer.Write(tmp, bitCount);
        }

        /// <summary>
        /// Writes a <see cref="long"/> using 1 to 64 bits.
        /// </summary>
        public static void Write(this IBitBuffer buffer, long value, int bitCount)
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
            buffer.Write(tmp, bitCount);
        }

        #region VarInt

        /// <summary>
        /// Write variable sized <see cref="ulong"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        [CLSCompliant(false)]
        public static int WriteVar(this IBitBuffer buffer, ulong value)
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

            buffer.Write(tmp.Slice(0, offset));
            return offset;
        }

        /// <summary>
        /// Write variable sized <see cref="long"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        public static int WriteVar(this IBitBuffer buffer, long value)
        {
            ulong zigzag = (ulong)(value << 1) ^ (ulong)(value >> 63);
            return buffer.WriteVar(zigzag);
        }

        /// <summary>
        /// Write variable sized <see cref="uint"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        [CLSCompliant(false)]
        public static int WriteVar(this IBitBuffer buffer, uint value)
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

            buffer.Write(tmp.Slice(0, offset));
            return offset;
        }

        /// <summary>
        /// Write variable sized <see cref="int"/>.
        /// </summary>
        /// <returns>Amount of bytes written.</returns>
        public static int WriteVar(this IBitBuffer buffer, int value)
        {
            uint zigzag = (uint)(value << 1) ^ (uint)(value >> 31);
            return buffer.WriteVar(zigzag);
        }

        #endregion

        /// <summary>
        /// Compress (lossy) a <see cref="float"/> in the range -1..1 using the specified amount of bits.
        /// </summary>
        public static void WriteSigned(this IBitBuffer buffer, float value, int bitCount)
        {
            LidgrenException.Assert(
                (value >= -1.0) && (value <= 1.0), 
                " WriteSignedSingle() must be passed a float in the range -1 to 1; val is " + value);

            float unit = (value + 1f) * 0.5f;
            int maxVal = (1 << bitCount) - 1;
            uint writeVal = (uint)(unit * maxVal);

            buffer.Write(writeVal, bitCount);
        }

        /// <summary>
        /// Compress (lossy) a <see cref="float"/> in the range 0..1 using the specified amount of bits.
        /// </summary>
        public static void WriteUnit(this IBitBuffer buffer, float value, int bitCount)
        {
            LidgrenException.Assert(
                (value >= 0.0) && (value <= 1.0),
                " WriteUnitSingle() must be passed a float in the range 0 to 1; val is " + value);

            int maxValue = (1 << bitCount) - 1;
            uint writeVal = (uint)(value * maxValue);

            buffer.Write(writeVal, bitCount);
        }

        /// <summary>
        /// Compress a <see cref="float"/> within a specified range using the specified amount of bits.
        /// </summary>
        public static void WriteRanged(this IBitBuffer buffer, float value, float min, float max, int bitCount)
        {
            LidgrenException.Assert(
                (value >= min) && (value <= max), 
                " WriteRangedSingle() must be passed a float in the range MIN to MAX; val is " + value);

            float range = max - min;
            float unit = (value - min) / range;
            int maxVal = (1 << bitCount) - 1;

            buffer.Write((uint)(maxVal * unit), bitCount);
        }

        /// <summary>
        /// Writes an <see cref="int"/> with the least amount of bits needed for the specified range.
        /// Returns the number of bits written.
        /// </summary>
        public static int WriteRanged(this IBitBuffer buffer, int min, int max, int value)
        {
            LidgrenException.Assert(value >= min && value <= max, "Value not within min/max range!");

            uint range = (uint)(max - min);
            int numBits = NetBitWriter.BitsForValue(range);

            uint rvalue = (uint)(value - min);
            buffer.Write(rvalue, numBits);

            return numBits;
        }

        /// <summary>
        /// Write characters from a span, readable as a string.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, ReadOnlySpan<char> source)
        {
            if (source.IsEmpty)
            {
                buffer.Write(NetStringHeader.Empty);
                return;
            }

            var initialHeader = new NetStringHeader(source.Length, null);
            int headerBitCount = initialHeader.HeaderSize * 8;
            buffer.EnsureBitCapacity(headerBitCount + initialHeader.ExpectedByteCount * 8);

            int startPosition = buffer.BitPosition;
            // Here we reserve space for the header.
            buffer.BitPosition += headerBitCount;

            Span<byte> writeBuffer = stackalloc byte[4096];
            int byteCount = 0;
            var charSource = source;
            do
            {
                var status = Utf8.FromUtf16(
                    charSource, writeBuffer, out int charsRead, out int bytesWritten, true, true);
                // TODO: check status

                charSource = charSource.Slice(charsRead);

                byteCount += bytesWritten;
                buffer.Write(writeBuffer.Slice(0, bytesWritten));
            }
            while (charSource.Length > 0);

            int endPosition = buffer.BitPosition;

            // Write header with exact values in previously reserved space.
            buffer.BitPosition = startPosition;
            buffer.Write(new NetStringHeader(source.Length, byteCount));

            buffer.BitPosition = endPosition;
        }

        /// <summary>
        /// Writes a <see cref="string"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, string? source)
        {
            buffer.Write(source.AsSpan());
        }

        /// <summary>
        /// Writes an <see cref="IPAddress"/> .
        /// </summary>
        public static void Write(this IBitBuffer buffer, IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            Span<byte> tmp = stackalloc byte[16];
            if (!address.TryWriteBytes(tmp, out int count))
                throw new ArgumentException("Failed to get address bytes.");
            tmp = tmp.Slice(0, count);

            buffer.Write((byte)tmp.Length);
            buffer.Write(tmp);
        }

        /// <summary>
        /// Writes an <see cref="IPEndPoint"/> description.
        /// </summary>
        public static void Write(this IBitBuffer buffer, IPEndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));

            buffer.Write(endPoint.Address);
            buffer.Write((ushort)endPoint.Port);
        }

        /// <summary>
        /// Writes a <see cref="TimeSpan"/>.
        /// </summary>
        public static void Write(this IBitBuffer buffer, TimeSpan time)
        {
            buffer.WriteVar(time.Ticks);
        }

        /// <summary>
        /// Writes the current local time (<see cref="NetTime.Now"/>).
        /// </summary>
        public static void WriteLocalTime(this IBitBuffer buffer)
        {
            buffer.Write(NetTime.Now);
        }

        /// <summary>
        /// Append all the bits from a <see cref="IBitBuffer"/> to this buffer.
        /// </summary>
        public static void Write(this IBitBuffer buffer, IBitBuffer sourceBuffer)
        {
            if (sourceBuffer == null)
                throw new ArgumentNullException(nameof(sourceBuffer));

            buffer.Write(sourceBuffer.GetBuffer(), 0, sourceBuffer.BitLength);
        }

        public static void Write<TEnum>(this IBitBuffer buffer, TEnum value)
            where TEnum : Enum
        {
            buffer.WriteVar(EnumConverter.Convert(value));
        }

        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void Write(this IBitBuffer buffer, NetStringHeader value)
        {
            buffer.WriteVar((uint)value.CharCount);

            if (value.CharCount == 0)
                return;

            // Both MaxByteCount and ByteCount should occupy the same amount
            // of bytes when written as variable ints. That is why we need to 
            // check if the encoded ByteCount has to be padded "empty" bytes.
            if (value.ByteCount == null)
            {
                buffer.WriteVar((uint)value.MaxByteCount);
            }
            else
            {
                buffer.WriteVar((uint)value.ByteCount);

                int sizeDiff = value.MaxByteCountVarSize - value.ExpectedByteCountVarSize;
                if (sizeDiff > 0)
                {
                    buffer.BitPosition -= 1; // rewind to last byte's high bit
                    buffer.Write(true); // set high bit to true

                    // Write empty 7-bit values with high bit set.
                    // This should only write if size diff is more than 1, 
                    // which should practically never occur.
                    for (int i = 0; i < sizeDiff - 1; i++)
                        buffer.Write((byte)0b1000_0000);

                    buffer.Write((byte)0);
                }
            }
        }

        /// <summary>
        /// Byte-aligns the write position, 
        /// decreasing work for subsequent writes if the position was not aligned.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void WritePadBits(this IBitBuffer buffer)
        {
            buffer.BitPosition = NetBitWriter.BytesForBits(buffer.BitPosition) * 8;
            buffer.EnsureBitCapacity(buffer.BitPosition);
            buffer.SetLengthByPosition();
        }

        /// <summary>
        /// Pads the write position with the specified number of bits.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void WritePadBits(this IBitBuffer buffer, int bitCount)
        {
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            buffer.BitPosition += bitCount;
            buffer.EnsureBitCapacity(buffer.BitPosition);
            buffer.SetLengthByPosition();
        }

        /// <summary>
        /// Pads the write position with the specified number of bytes.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void WritePadBytes(this IBitBuffer buffer, int byteCount)
        {
            buffer.WritePadBits(byteCount * 8);
        }
    }
}
