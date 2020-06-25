using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;

namespace Lidgren.Network
{
    // TODO: move most code to NetBuffer.Peek
    //       and call it from here and advance the read pointer if it succeeds

    /// <summary>
    /// Base class for <see cref="NetIncomingMessage"/> and <see cref="NetOutgoingMessage"/>.
    /// </summary>
    public partial class NetBuffer
    {
        const string ReadOverflowError = "";

        // TODO: make into extension with ReadEnum

        public bool HasEnough(int bitCount)
        {
            return _bitLength - BitPosition >= bitCount;
        }

        /// <summary>
        /// Tries to read a specified number of bits into the given buffer.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="bitCount">The number of bits to read</param>
        public bool TryReadBits(Span<byte> destination, int bitCount)
        {
            if (!HasEnough(bitCount))
                return false;

            NetBitWriter.CopyBits(Data, BitPosition, bitCount, destination, 0);
            BitPosition += bitCount;
            return true;
        }

        /// <summary>
        /// Tries to reads bytes into the given span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        public bool TryRead(Span<byte> destination)
        {
            return TryReadBits(destination, destination.Length * 8);
        }

        /// <summary>
        /// Reads the specified number of bits into the given buffer.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="bitCount">The number of bits to read</param>
        public void ReadBits(Span<byte> destination, int bitCount)
        {
            if (!TryReadBits(destination, bitCount))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Reads the specified number of bits, between one and <paramref name="maxBitCount"/>, into the given buffer.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="bitCount">The number of bits to read</param>
        /// <param name="maxBitCount">The maximum amount of bits to read.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Bit count is less than one or greater than <paramref name="maxBitCount"/>.
        /// </exception>
        public void ReadBits(Span<byte> destination, int bitCount, int maxBitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            ReadBits(destination, bitCount);
        }

        /// <summary>
        /// Reads bytes into the given span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        public void Read(Span<byte> destination)
        {
            ReadBits(destination, destination.Length * 8);
        }

        /// <summary>
        /// Reads a 1-bit <see cref="bool"/> value written by <see cref="Write(bool)"/>.
        /// </summary>
        public bool ReadBoolean()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 1, ReadOverflowError);
            byte retval = NetBitWriter.ReadByteUnchecked(Data, BitPosition, 1);
            BitPosition += 1;
            return retval > 0;
        }

        /// <summary>
        /// Reads a <see cref="byte"/>.
        /// </summary>
        public byte ReadByte()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 8, ReadOverflowError);
            byte retval = NetBitWriter.ReadByteUnchecked(Data, BitPosition, 8);
            BitPosition += 8;
            return retval;
        }

        /// <summary>
        /// Tries to read a <see cref="byte"/>.
        /// </summary>
        /// <returns>Whether the read succeeded.</returns>
        public bool ReadByte(out byte result)
        {
            if (!HasEnough(8))
            {
                result = 0;
                return false;
            }

            result = PeekByte();
            BitPosition += sizeof(byte) * 8;
            return true;
        }

        /// <summary>
        /// Reads a <see cref="sbyte"/>.
        /// </summary>
        [CLSCompliant(false)]
        public sbyte ReadSByte()
        {
            sbyte value = PeekSByte();
            BitPosition += sizeof(sbyte) * 8;
            return (sbyte)value;
        }

        /// <summary>
        /// Reads 1 to 8 bits into a <see cref="byte"/>.
        /// </summary>
        public byte ReadByte(int bitCount)
        {
            byte value = PeekByte(bitCount);
            BitPosition += bitCount;
            return value;
        }

        /// <summary>
        /// Reads a 16-bit <see cref="short"/> written by <see cref="Write(short)"/>.
        /// </summary>
        public short ReadInt16()
        {
            short value = PeekInt16();
            BitPosition += sizeof(short) * 2;
            return value;
        }

        /// <summary>
        /// Reads a 16-bit <see cref="ushort"/> written by <see cref="Write(ushort)"/>.
        /// </summary>
        [CLSCompliant(false)]
        public ushort ReadUInt16()
        {
            ushort value = PeekUInt16();
            BitPosition += sizeof(ushort) * 2;
            return value;
        }

        /// <summary>
        /// Reads a 32-bit <see cref="int"/> written by <see cref="Write(int)"/>.
        /// </summary>
        [CLSCompliant(false)]
        public bool ReadInt32(out int result)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            if (TryRead(tmp))
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
        public int ReadInt32()
        {
            if (!ReadInt32(out int result))
                throw new EndOfMessageException();
            return result;
        }

        /// <summary>
        /// Reads a <see cref="int"/> stored in 1 to 32 bits, written by <see cref="Write(int, int)"/>.
        /// </summary>
        public int ReadInt32(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            ReadBits(tmp, bitCount);
            int value = BinaryPrimitives.ReadInt32LittleEndian(tmp);

            if (bitCount == 32)
                return value;

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
        [CLSCompliant(false)]
        public uint ReadUInt32()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 32, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt32(Data, BitPosition, 32);
            BitPosition += 32;
            return retval;
        }

        /// <summary>
        /// Reads a 32-bit <see cref="uint"/> written by <see cref="Write(uint)"/> and returns whether the read succeeded.
        /// </summary>
        [CLSCompliant(false)]
        public bool ReadUInt32(out uint result)
        {
            if (_bitLength - BitPosition < 32)
            {
                result = 0;
                return false;
            }
            result = NetBitWriter.ReadUInt32(Data, BitPosition, 32);
            BitPosition += 32;
            return true;
        }

        /// <summary>
        /// Reads an <see cref="uint"/> stored in 1 to 32 bits, written by <see cref="Write(uint, int)"/>.
        /// </summary>
        [CLSCompliant(false)]
        public uint ReadUInt32(int bitCount)
        {
            LidgrenException.Assert(bitCount > 0 && bitCount <= 32, "ReadUInt32(bits) can only read between 1 and 32 bits");
            //NetException.Assert(m_bitLength - m_readBitPtr >= bitCount, "tried to read past buffer size");

            uint retval = NetBitWriter.ReadUInt32(Data, BitPosition, bitCount);
            BitPosition += bitCount;
            return retval;
        }

        #region Int64

        /// <summary>
        /// Reads a 64-bit <see cref="long"/> written by <see cref="Write(long)"/>.
        /// </summary>
        public long ReadInt64()
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            Read(tmp);
            return BinaryPrimitives.ReadInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a 64-bit <see cref="ulong"/> written by <see cref="Write(ulong)"/>.
        /// </summary>
        [CLSCompliant(false)]
        public ulong ReadUInt64()
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            Read(tmp);
            return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a <see cref="long"/> stored in 1 to 64 bits, written by <see cref="Write(long, int)"/>.
        /// </summary>
        public long ReadInt64(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            ReadBits(tmp, bitCount, sizeof(ulong) * 8);
            return BinaryPrimitives.ReadInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads an <see cref="ulong"/> stored in 1 to 64 bits, written by <see cref="Write(ulong, int)"/>.
        /// </summary>
        [CLSCompliant(false)]
        public ulong ReadUInt64(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            ReadBits(tmp, bitCount, sizeof(ulong) * 8);
            return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
        }

        #endregion

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="Write(float)"/>.
        /// </summary>
        public float ReadSingle()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 32, ReadOverflowError);

            if ((BitPosition & 7) == 0) // read directly
            {
                float retval = BitConverter.ToSingle(Data, BitPosition >> 3);
                BitPosition += 32;
                return retval;
            }

            Span<byte> bytes = stackalloc byte[sizeof(float)];
            Read(bytes);
            return BitConverter.ToSingle(bytes);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="Write(float)"/>.
        /// </summary>
        public bool ReadSingle(out float result)
        {
            if (_bitLength - BitPosition < 32)
            {
                result = 0f;
                return false;
            }

            if ((BitPosition & 7) == 0) // read directly
            {
                result = BitConverter.ToSingle(Data, BitPosition >> 3);
                BitPosition += 32;
                return true;
            }

            byte[] bytes = new byte[0];
            //ReadBytes(4);
            result = BitConverter.ToSingle(bytes, 0);
            return true;
        }

        /// <summary>
        /// Reads a 64-bit <see cref="double"/> written by <see cref="Write(double)"/>.
        /// </summary>
        public double ReadDouble()
        {
            LidgrenException.Assert(_bitLength - BitPosition >= 64, ReadOverflowError);

            if ((BitPosition & 7) == 0) // read directly
            {
                // read directly
                double retval = BitConverter.ToDouble(Data, BitPosition >> 3);
                BitPosition += 64;
                return retval;
            }

            return BitConverter.Int64BitsToDouble(ReadInt64());
        }

        /// <summary>
        /// Reads a variable sized <see cref="uint"/> written by <see cref="WriteVar"/>.
        /// </summary>
        [CLSCompliant(false)]
        public uint ReadVarUInt32()
        {
            int num1 = 0;
            int num2 = 0;
            while (_bitLength - BitPosition >= 8)
            {
                byte num3 = ReadByte();
                num1 |= (num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return (uint)num1;
            }

            // ouch; failed to find enough bytes; malformed variable length number?
            return (uint)num1;
        }

        /// <summary>
        /// Reads a variable sized <see cref="uint"/> written by <see cref="WriteVar"/>
        /// and returns whether the read succeeded.
        /// </summary>
        [CLSCompliant(false)]
        public bool ReadVarUInt32(out uint result)
        {
            int num1 = 0;
            int num2 = 0;
            while (_bitLength - BitPosition >= 8)
            {
                if (!ReadByte(out byte num3))
                {
                    result = 0;
                    return false;
                }
                num1 |= (num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    result = (uint)num1;
                    return true;
                }
            }
            result = (uint)num1;
            return false;
        }

        /// <summary>
        /// Reads a variable sized <see cref="ulong"/> written by <see cref="WriteVar"/>.
        /// </summary>
        [CLSCompliant(false)]
        public ulong ReadVarUInt64()
        {
            ulong num1 = 0;
            int num2 = 0;
            while (_bitLength - BitPosition >= 8)
            {
                //if (num2 == 0x23)
                //	throw new FormatException("Bad 7-bit encoded integer");

                byte num3 = ReadByte();
                num1 |= ((ulong)num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return num1;
            }

            // ouch; failed to find enough bytes; malformed variable length number?
            return num1;
        }

        /// <summary>
        /// Reads a variable sized <see cref="int"/> written by <see cref="WriteVar"/>.
        /// </summary>
        public int ReadVarInt32()
        {
            uint n = ReadVarUInt32();
            return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized <see cref="long"/> written by <see cref="WriteVar"/>.
        /// </summary>
        public long ReadVarInt64()
        {
            ulong n = ReadVarUInt64();
            return (long)(n >> 1) ^ -(long)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="WriteSigned"/>.
        /// </summary>
        /// <param name="bitCount">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to -1 and smaller or equal to 1</returns>
        public float ReadSignedSingle(int bitCount)
        {
            uint encodedVal = ReadUInt32(bitCount);
            int maxVal = (1 << bitCount) - 1;
            return ((encodedVal + 1) / (float)(maxVal + 1) - 0.5f) * 2.0f;
        }

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> written by <see cref="WriteUnit"/>.
        /// </summary>
        /// <param name="bitCount">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to 0 and smaller or equal to 1</returns>
        public float ReadUnitSingle(int bitCount)
        {
            uint encodedVal = ReadUInt32(bitCount);
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
        public float ReadRangedSingle(float min, float max, int bitCount)
        {
            float range = max - min;
            int maxVal = (1 << bitCount) - 1;
            float encodedVal = ReadUInt32(bitCount);
            float unit = encodedVal / maxVal;
            return min + (unit * range);
        }

        /// <summary>
        /// Reads a 32-bit <see cref="int"/> written by <see cref="WriteRanged"/>.
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
        public int ReadRangedInteger(int min, int max)
        {
            uint range = (uint)(max - min);
            int numBits = NetUtility.BitCountForValue(range);

            uint rvalue = ReadUInt32(numBits);
            return (int)(min + rvalue);
        }

        [CLSCompliant(false)]
        public bool ReadStringLength(out int byteLength, out int charLength)
        {
            byteLength = default;
            charLength = default;

            if (!ReadVarUInt32(out uint uByteLen) || uByteLen > int.MaxValue)
                return false;
            if (uByteLen <= 0)
                return true;

            if (!ReadVarUInt32(out uint uCharLen) || uCharLen > int.MaxValue)
                return false;
            if (uCharLen <= 0)
                return true;

            if (!HasEnough((int)uByteLen * 8))
                return false;

            byteLength = (int)uByteLen;
            charLength = (int)uCharLen;
            return true;
        }

        /// <summary>
        /// Reads chars written by <see cref="Write(ReadOnlySpan{char})"/> or <see cref="Write(string)"/>.
        /// </summary>
        /// <param name="byteLength">The length acquired by <see cref="ReadStringLength"/>.</param>
        /// <param name="destination">The destination for chars.</param>
        public void Read(int byteLength, Span<char> destination)
        {
            if (byteLength < 0)
                throw new ArgumentNullException(nameof(byteLength));

            if (IsByteAligned)
            {
                var source = Data.AsSpan().Slice(BytePosition, byteLength);

                if (StringEncoding.GetChars(source, destination) < destination.Length)
                    throw new InvalidDataException("Failed to read all characters.");

                BitPosition += byteLength * 8;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[Math.Min(byteLength, 4096)];
                while (byteLength > 0)
                {
                    var slice = buffer.Slice(0, Math.Min(buffer.Length, byteLength));
                    Read(slice);

                    int charsRead = StringEncoding.GetChars(slice, destination);
                    if (charsRead == 0)
                        break;

                    destination = destination.Slice(charsRead);
                    byteLength -= slice.Length;
                }

                if (destination.Length > 0)
                    throw new InvalidDataException("Failed to read all characters.");
            }
        }

        /// <summary>
        /// Tries to read a <see cref="string"/> written by 
        /// <see cref="Write(ReadOnlySpan{char})"/> or <see cref="Write(string)"/>.
        /// </summary>
        /// <returns>Whether a string was successfully read.</returns>
        public bool ReadString(out string result)
        {
            if (!ReadStringLength(out int uByteLen, out int uCharLen))
            {
                result = string.Empty;
                return false;
            }

            static void ReadCallback(Span<char> destination, (NetBuffer, int) state)
            {
                var (buffer, byteLen) = state;
                buffer.Read(byteLen, destination);
            }

            result = string.Create(uCharLen, (this, (int)uByteLen), ReadCallback);
            return true;
        }

        /// <summary>
        /// Reads a <see cref="string"/> written by 
        /// <see cref="Write(ReadOnlySpan{char})"/> or <see cref="Write(string)"/>.
        /// </summary>
        public string ReadString()
        {
            if (!ReadString(out string result))
                throw new EndOfMessageException();
            return result;
        }

        /// <summary>
        /// Byte-aligns the read position, 
        /// decreasing work for subsequent reads if the position was not aligned.
        /// </summary>
        public void SkipPadBits()
        {
            BitPosition = (BitPosition + 7) / 8 * 8;
        }

        /// <summary>
        /// Pads the read position with the specified number of bits.
        /// </summary>
        public void SkipBits(int bitCount)
        {
            BitPosition += bitCount;
        }

        #region TODO: turn these into extension methods

        /// <summary>
        /// Reads local time comparable to <see cref="NetTime.Now"/>,
        /// written by <see cref="WriteLocalTime"/> for the given <see cref="NetConnection"/>.
        /// </summary>
        public TimeSpan ReadLocalTime(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var remoteTime = ReadTimeSpan();
            return connection.GetLocalTime(remoteTime);
        }

        /// <summary>
        /// Reads an <see cref="IPAddress"/>.
        /// </summary>
        public IPAddress ReadIPAddress()
        {
            byte length = ReadByte();
            Span<byte> tmp = stackalloc byte[length];
            return new IPAddress(tmp);
        }

        /// <summary>
        /// Reads an <see cref="IPEndPoint"/> description.
        /// </summary>
        public IPEndPoint ReadIPEndPoint()
        {
            var address = ReadIPAddress();
            var port = ReadUInt16();
            return new IPEndPoint(address, port);
        }

        /// <summary>
        /// Reads a <see cref="TimeSpan"/>.
        /// </summary>
        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ReadVarInt64());
        }

        public TEnum ReadEnum<TEnum>()
            where TEnum : Enum
        {
            long value = ReadVarInt64();
            return EnumConverter.Convert<TEnum>(value);
        }

        #endregion
    }
}
