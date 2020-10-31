using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Unicode;

namespace Lidgren.Network
{
    // TODO: move most code to NetBuffer.Peek
    //       and call it from here and advance the read pointer if it succeeds

    public static class BitBufferReadExtensions
    {
        /// <summary>
        /// Tries to read a specified number of bits into the given buffer.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="bitCount">The number of bits to read.</param>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static bool TryReadBits(this IBitBuffer buffer, Span<byte> destination, int bitCount)
        {
            if (!buffer.HasEnough(bitCount))
                return false;

            NetBitWriter.CopyBits(buffer.GetBuffer(), buffer.BitPosition, bitCount, destination, 0);
            buffer.BitPosition += bitCount;
            return true;
        }

        /// <summary>
        /// Reads the specified number of bits into the given buffer.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="bitCount">The number of bits to read.</param>
        /// <exception cref="EndOfMessageException"></exception>
        public static void ReadBits(this IBitBuffer buffer, Span<byte> destination, int bitCount)
        {
            if (!buffer.TryReadBits(destination, bitCount))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Reads the specified number of bits, clamped between one and <paramref name="maxBitCount"/>, into the given buffer.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="bitCount">The number of bits to read.</param>
        /// <param name="maxBitCount">The maximum amount of bits to read.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Bit count is less than one or greater than <paramref name="maxBitCount"/>.
        /// </exception>
        /// <exception cref="EndOfMessageException"></exception>
        public static void ReadBits(this IBitBuffer buffer, Span<byte> destination, int bitCount, int maxBitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            buffer.ReadBits(destination, bitCount);
        }

        /// <summary>
        /// Tries to read bytes into the given span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static bool TryRead(this IBitBuffer buffer, Span<byte> destination)
        {
            if (!buffer.IsByteAligned())
                return buffer.TryReadBits(destination, destination.Length * 8);

            if (!buffer.HasEnough(destination.Length))
                return false;

            buffer.GetBuffer().AsSpan(buffer.BytePosition, destination.Length).CopyTo(destination);
            buffer.BitPosition += destination.Length * 8;
            return true;
        }

        /// <summary>
        /// Reads bytes into the given span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <exception cref="EndOfMessageException"></exception>
        public static void Read(this IBitBuffer buffer, Span<byte> destination)
        {
            if (!buffer.TryRead(destination))
                throw new EndOfMessageException();
        }

        public static bool TryRead(this IBitBuffer buffer, int count, out byte[] data)
        {
            data = new byte[count];
            return buffer.TryRead(data);
        }

        /// <summary>
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static byte[] Read(this IBitBuffer buffer, int count)
        {
            var data = new byte[count];
            buffer.Read(data);
            return data;
        }

        /// <summary>
        /// Reads bytes into the given span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static int StreamRead(this IBitBuffer buffer, Span<byte> destination)
        {
            if (buffer.IsByteAligned())
            {
                int remainingBytes = Math.Min(buffer.ByteLength - buffer.BytePosition, destination.Length);
                buffer.Read(destination.Slice(0, remainingBytes));
                return remainingBytes;
            }
            else
            {
                int remainingBits = Math.Min(buffer.BitLength - buffer.BitPosition, destination.Length * 8);
                buffer.ReadBits(destination, remainingBits);
                return remainingBits / 8;
            }
        }

        #region Bool

        /// <summary>
        /// Reads a 1-bit <see cref="bool"/> value written by <see cref="Write(bool)"/>.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static bool ReadBool(this IBitBuffer buffer)
        {
            if (!buffer.HasEnough(1))
                throw new EndOfMessageException();

            byte value = NetBitWriter.ReadByteUnchecked(buffer.GetBuffer(), buffer.BitPosition, 1);
            buffer.BitPosition += 1;
            return value > 0;
        }

        #endregion

        #region Int8

        /// <summary>
        /// Tries to read a <see cref="byte"/>.
        /// </summary>
        /// <returns>Whether the read succeeded.</returns>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static bool ReadByte(this IBitBuffer buffer, out byte result)
        {
            if (!buffer.HasEnough(8))
            {
                result = default;
                return false;
            }

            result = NetBitWriter.ReadByteUnchecked(buffer.GetBuffer(), buffer.BitPosition, 8);
            buffer.BitPosition += 8;
            return true;
        }

        /// <summary>
        /// Reads a <see cref="byte"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static byte ReadByte(this IBitBuffer buffer)
        {
            if (!buffer.ReadByte(out byte value))
                throw new EndOfMessageException();
            return value;
        }

        /// <summary>
        /// Reads 1 to 8 bits into a <see cref="byte"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static byte ReadByte(this IBitBuffer buffer, int bitCount)
        {
            if (!buffer.HasEnough(bitCount))
                throw new EndOfMessageException();
            
            byte value = NetBitWriter.ReadByte(buffer.GetBuffer(), buffer.BitPosition, bitCount);
            buffer.BitPosition += bitCount;
            return value;
        }

        /// <summary>
        /// Reads a <see cref="sbyte"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [CLSCompliant(false)]
        public static sbyte ReadSByte(this IBitBuffer buffer)
        {
            if (!buffer.ReadByte(out byte value))
                throw new EndOfMessageException();
            return (sbyte)value;
        }

        #endregion

        #region Int16

        /// <summary>
        /// Reads a 16-bit <see cref="short"/> written by <see cref="Write(short)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static short ReadInt16(this IBitBuffer buffer)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            buffer.Read(tmp);
            return BinaryPrimitives.ReadInt16LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a 16-bit <see cref="ushort"/> written by <see cref="Write(ushort)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static ushort ReadUInt16(this IBitBuffer buffer)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            buffer.Read(tmp);
            return BinaryPrimitives.ReadUInt16LittleEndian(tmp);
        }

        #endregion

        #region Int32

        /// <summary>
        /// Reads a 32-bit <see cref="int"/> written by <see cref="Write(int)"/>.
        /// </summary>
        [CLSCompliant(false)]
        public static bool ReadInt32(this IBitBuffer buffer, out int result)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            if (buffer.TryRead(tmp))
            {
                result = BinaryPrimitives.ReadInt32LittleEndian(tmp);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Reads a 32-bit <see cref="int"/> written by <see cref="Write(int)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static int ReadInt32(this IBitBuffer buffer)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            buffer.Read(tmp);
            return BinaryPrimitives.ReadInt32LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a <see cref="int"/> stored in 1 to 32 bits, written by <see cref="Write(int, int)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static int ReadInt32(this IBitBuffer buffer, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            if (bitCount == tmp.Length * 8)
            {
                buffer.Read(tmp);
                return BinaryPrimitives.ReadInt32LittleEndian(tmp);
            }

            buffer.ReadBits(tmp, bitCount, tmp.Length * 8);
            int value = BinaryPrimitives.ReadInt32LittleEndian(tmp);

            int signBit = 1 << (bitCount - 1);
            if ((value & signBit) == 0)
                return value; // positive

            // negative
            unchecked
            {
                uint mask = ((uint)-1) >> (33 - bitCount);
                uint nValue = ((uint)value & mask) + 1;
                return -(int)nValue;
            }
        }

        /// <summary>
        /// Reads a <see cref="uint"/> written by <see cref="Write(uint)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static uint ReadUInt32(this IBitBuffer buffer)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            buffer.Read(tmp);
            return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="uint"/> written by <see cref="Write(uint)"/> and returns whether the read succeeded.
        /// </summary>
        [CLSCompliant(false)]
        public static bool ReadUInt32(this IBitBuffer buffer, out uint result)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            if (buffer.TryRead(tmp))
            {
                result = BinaryPrimitives.ReadUInt32LittleEndian(tmp);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Reads an <see cref="uint"/> stored in 1 to 32 bits, written by <see cref="Write(uint, int)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static uint ReadUInt32(this IBitBuffer buffer, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            buffer.ReadBits(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="int"/> written by <see cref="WriteRanged"/>.
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
        /// <exception cref="EndOfMessageException"></exception>
        public static int ReadRangedInt32(this IBitBuffer buffer, int min, int max)
        {
            uint range = (uint)(max - min);
            int numBits = NetBitWriter.BitsForValue(range);
            uint rvalue = buffer.ReadUInt32(numBits);
            return (int)(min + rvalue);
        }

        #endregion

        #region Int64

        /// <summary>
        /// Reads a 64-bit <see cref="long"/> written by <see cref="Write(long)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static long ReadInt64(this IBitBuffer buffer)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            buffer.Read(tmp);
            return BinaryPrimitives.ReadInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a 64-bit <see cref="ulong"/> written by <see cref="Write(ulong)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static ulong ReadUInt64(this IBitBuffer buffer)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            buffer.Read(tmp);
            return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a <see cref="long"/> stored in 1 to 64 bits, written by <see cref="Write(long, int)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static long ReadInt64(this IBitBuffer buffer, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            if (bitCount == tmp.Length * 8)
            {
                buffer.Read(tmp);
                return BinaryPrimitives.ReadInt64LittleEndian(tmp);
            }

            buffer.ReadBits(tmp, bitCount, tmp.Length * 8);
            long value = BinaryPrimitives.ReadInt64LittleEndian(tmp);

            long signBit = 1 << (bitCount - 1);
            if ((value & signBit) == 0)
                return value; // positive

            // negative
            unchecked
            {
                ulong mask = ((ulong)-1) >> (65 - bitCount);
                ulong nValue = ((ulong)value & mask) + 1;
                return -(long)nValue;
            }
        }

        /// <summary>
        /// Reads an <see cref="ulong"/> stored in 1 to 64 bits, written by <see cref="Write(ulong, int)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static ulong ReadUInt64(this IBitBuffer buffer, int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            buffer.ReadBits(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
        }

        #endregion

        #region VarInt

        /// <summary>
        /// Tries to read a variable sized <see cref="uint"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public static OperationStatus ReadVarUInt32(this IBitBuffer buffer, out uint result)
        {
            return NetBitWriter.ReadVarUInt32(buffer, peek: false, out result);
        }

        /// <summary>
        /// Tries to read a variable sized <see cref="ulong"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public static OperationStatus ReadVarUInt64(this IBitBuffer buffer, out ulong result)
        {
            return NetBitWriter.ReadVarUInt64(buffer, peek: false, out result);
        }

        /// <summary>
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static uint ReadVarUInt32(this IBitBuffer buffer)
        {
            var status = ReadVarUInt32(buffer, out uint value);
            if (status == OperationStatus.Done)
                return value;

            if (status == OperationStatus.NeedMoreData)
                throw new EndOfMessageException();

            return default;
        }

        /// <summary>
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        [CLSCompliant(false)]
        public static ulong ReadVarUInt64(this IBitBuffer buffer)
        {
            var status = buffer.ReadVarUInt64(out ulong value);
            if (status == OperationStatus.Done)
                return value;

            if (status == OperationStatus.NeedMoreData)
                throw new EndOfMessageException();

            return default;
        }

        /// <summary>
        /// Reads a variable sized <see cref="int"/> written by <see cref="WriteVar(int)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static int ReadVarInt32(this IBitBuffer buffer)
        {
            uint n = buffer.ReadVarUInt32();
            return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized <see cref="long"/> written by <see cref="WriteVar(long)"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static long ReadVarInt64(this IBitBuffer buffer)
        {
            ulong n = buffer.ReadVarUInt64();
            return (long)(n >> 1) ^ -(long)(n & 1); // decode zigzag
        }

        #endregion

        #region Float

        /// <summary>
        /// Reads a 32-bit <see cref="float"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static float ReadSingle(this IBitBuffer buffer)
        {
            int intValue = buffer.ReadInt32();
            return BitConverter.Int32BitsToSingle(intValue);
        }

        /// <summary>
        /// Reads a 64-bit <see cref="double"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static double ReadDouble(this IBitBuffer buffer)
        {
            long intValue = buffer.ReadInt64();
            return BitConverter.Int64BitsToDouble(intValue);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="WriteSigned"/>.
        /// </summary>
        /// <param name="bitCount">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to -1 and smaller or equal to 1</returns>
        /// <exception cref="EndOfMessageException"></exception>
        public static float ReadSignedSingle(this IBitBuffer buffer, int bitCount)
        {
            uint encodedVal = buffer.ReadUInt32(bitCount);
            int maxVal = (1 << bitCount) - 1;
            return ((encodedVal + 1) / (float)(maxVal + 1) - 0.5f) * 2.0f;
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="WriteUnit"/>.
        /// </summary>
        /// <param name="bitCount">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to 0 and smaller or equal to 1</returns>
        /// <exception cref="EndOfMessageException"></exception>
        public static float ReadUnitSingle(this IBitBuffer buffer, int bitCount)
        {
            uint encodedVal = buffer.ReadUInt32(bitCount);
            int maxVal = (1 << bitCount) - 1;
            return (encodedVal + 1) / (float)(maxVal + 1);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="WriteRanged"/>.
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <param name="bitCount">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to MIN and smaller or equal to MAX</returns>
        /// <exception cref="EndOfMessageException"></exception>
        public static float ReadRangedSingle(this IBitBuffer buffer, float min, float max, int bitCount)
        {
            float range = max - min;
            int maxVal = (1 << bitCount) - 1;
            float encodedVal = buffer.ReadUInt32(bitCount);
            float unit = encodedVal / maxVal;
            return min + (unit * range);
        }

        #endregion

        #region ReadString

        public static bool ReadStringHeader(this IBitBuffer buffer, out NetStringHeader header)
        {
            header = default;

            if (buffer.ReadVarUInt32(out uint charCount) != OperationStatus.Done || charCount > int.MaxValue)
                return false;
            if (charCount <= 0)
                return true;

            if (buffer.ReadVarUInt32(out uint byteCount) != OperationStatus.Done || byteCount > int.MaxValue)
                return false;
            if (byteCount <= 0)
                return true;

            if (!buffer.HasEnough((int)byteCount * 8))
                return false;

            header = new NetStringHeader(charCount, byteCount);
            return true;
        }

        /// <summary>
        /// Reads chars written by <see cref="Write(ReadOnlySpan{char})"/> or <see cref="Write(string)"/>.
        /// This method does not read <see cref="NetStringHeader"/>.
        /// </summary>
        /// <param name="byteCount">The amount of bytes to read..</param>
        /// <param name="destination">The destination for chars.</param>
        /// <param name="bytesRead">The amount of bytes read.</param>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static OperationStatus Read(
            this IBitBuffer buffer, int byteCount, Span<char> destination, out int bytesRead, out int charsWritten)
        {
            if (byteCount < 0)
                throw new ArgumentNullException(nameof(byteCount));

            if (buffer.IsByteAligned())
            {
                var source = buffer.GetBuffer().AsSpan(buffer.BytePosition, byteCount);
                var status = Utf8.ToUtf16(source, destination, out bytesRead, out charsWritten, false, false);
                buffer.BitPosition += bytesRead * 8;
                return status;
            }
            else
            {
                Span<byte> readBuffer = stackalloc byte[Math.Min(byteCount, 4096)];
                var status = OperationStatus.Done;
                bytesRead = 0;
                charsWritten = 0;

                while (byteCount > 0)
                {
                    var slice = readBuffer.Slice(0, Math.Min(readBuffer.Length, byteCount));
                    buffer.Peek(slice);

                    var lastStatus = status;
                    status = Utf8.ToUtf16(
                        readBuffer, destination, out int sBytesRead, out int sCharsWritten, false, false);

                    bytesRead += sBytesRead;
                    byteCount -= sBytesRead;
                    buffer.BitPosition += sBytesRead * 8;

                    charsWritten += sCharsWritten;
                    destination = destination.Slice(sCharsWritten);

                    if (status != OperationStatus.Done)
                    {
                        if (status == OperationStatus.NeedMoreData &&
                            lastStatus != OperationStatus.NeedMoreData &&
                            byteCount > 0)
                            continue;

                        break;
                    }
                }

                return status;
            }
        }

        private static void CreateStringCallback(
            Span<char> destination, (IBitBuffer, NetStringHeader) state)
        {
            var (buffer, header) = state;
            if (header.ByteCount == null)
                return;

            buffer.Read(header.ByteCount.Value, destination, out _, out _);
        }

        /// <summary>
        /// Tries to read a <see cref="string"/>.
        /// </summary>
        /// <returns>Whether a string was successfully read.</returns>
        public static bool ReadString(this IBitBuffer buffer, out string result)
        {
            if (!buffer.ReadStringHeader(out var header))
            {
                result = string.Empty;
                return false;
            }

            if (header.ByteCount == null)
            {
                result = string.Empty;
                return true;
            }

            result = string.Create(header.CharCount, (buffer, header), CreateStringCallback);
            return true;
        }

        /// <summary>
        /// Reads a <see cref="string"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static string ReadString(this IBitBuffer buffer)
        {
            if (!buffer.ReadString(out string result))
                throw new EndOfMessageException();
            return result;
        }

        #endregion

        /// <summary>
        /// Byte-aligns the position, 
        /// decreasing work for subsequent operations if the position was not aligned.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void SkipPadBits(this IBitBuffer buffer)
        {
            buffer.BitPosition = NetBitWriter.BytesForBits(buffer.BitPosition) * 8;
        }

        /// <summary>
        /// Pads the read position with the specified number of bits.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        public static void SkipBits(this IBitBuffer buffer, int bitCount)
        {
            buffer.BitPosition += bitCount;
        }

        #region TODO: turn these into extension methods

        /// <summary>
        /// Reads local time comparable to <see cref="NetTime.Now"/>,
        /// written by <see cref="WriteLocalTime"/> for the given <see cref="NetConnection"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static TimeSpan ReadLocalTime(this IBitBuffer buffer, NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var remoteTime = buffer.ReadTimeSpan();
            return connection.GetLocalTime(remoteTime);
        }

        /// <summary>
        /// Reads an <see cref="IPAddress"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static IPAddress ReadIPAddress(this IBitBuffer buffer)
        {
            byte length = buffer.ReadByte();
            Span<byte> tmp = stackalloc byte[length];
            return new IPAddress(tmp);
        }

        /// <summary>
        /// Reads an <see cref="IPEndPoint"/> description.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static IPEndPoint ReadIPEndPoint(this IBitBuffer buffer)
        {
            var address = buffer.ReadIPAddress();
            var port = buffer.ReadUInt16();
            return new IPEndPoint(address, port);
        }

        /// <summary>
        /// Reads a <see cref="TimeSpan"/>.
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static TimeSpan ReadTimeSpan(this IBitBuffer buffer)
        {
            return new TimeSpan(buffer.ReadVarInt64());
        }

        /// <summary>
        /// </summary>
        /// <exception cref="EndOfMessageException"></exception>
        public static TEnum ReadEnum<TEnum>(this IBitBuffer buffer)
            where TEnum : Enum
        {
            long value = buffer.ReadVarInt64();
            return EnumConverter.Convert<TEnum>(value);
        }

        #endregion
    }
}
