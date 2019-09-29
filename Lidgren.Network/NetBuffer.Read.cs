using System;
using System.Net;

namespace Lidgren.Network
{
    /// <summary>
    /// Base class for <see cref="NetIncomingMessage"/> and <see cref="NetOutgoingMessage"/>.
    /// </summary>
    public partial class NetBuffer
	{
		private const string c_readOverflowError = 
            "Trying to read past the buffer size - likely caused by mismatching Write/Reads, different size or order.";

        /// <summary>
        /// Reads one <see cref="byte"/> and casts it to <see cref="NetConnectionStatus"/>.
        /// </summary>
        public NetConnectionStatus ReadStatus()
        {
            return (NetConnectionStatus)ReadByte();
        }

        /// <summary>
        /// Reads a 1-bit <see cref="bool"/> value written by <see cref="Write(bool)"/>.
        /// </summary>
        public bool ReadBoolean()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 1, c_readOverflowError);
			byte retval = NetBitWriter.ReadByte(m_data, 1, m_readPosition);
			m_readPosition += 1;
			return retval > 0;
		}
		
		/// <summary>
		/// Reads a <see cref="byte"/>.
		/// </summary>
		public byte ReadByte()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 8, c_readOverflowError);
			byte retval = NetBitWriter.ReadByte(m_data, 8, m_readPosition);
			m_readPosition += 8;
			return retval;
		}

        /// <summary>
        /// Reads a <see cref="byte"/> and returns whether the read succeeded.
        /// </summary>
        public bool TryReadByte(out byte result)
		{
			if (m_bitLength - m_readPosition < 8)
			{
				result = 0;
				return false;
			}
			result = NetBitWriter.ReadByte(m_data, 8, m_readPosition);
			m_readPosition += 8;
			return true;
		}

		/// <summary>
		/// Reads a <see cref="sbyte"/>.
		/// </summary>
		[CLSCompliant(false)]
		public sbyte ReadSByte()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 8, c_readOverflowError);
			byte retval = NetBitWriter.ReadByte(m_data, 8, m_readPosition);
			m_readPosition += 8;
			return (sbyte)retval;
		}

		/// <summary>
		/// Reads 1 to 8 bits into a <see cref="byte"/>.
		/// </summary>
		public byte ReadByte(int numberOfBits)
		{
			NetException.Assert(numberOfBits > 0 && numberOfBits <= 8, "ReadByte(bits) can only read between 1 and 8 bits");
			byte retval = NetBitWriter.ReadByte(m_data, numberOfBits, m_readPosition);
			m_readPosition += numberOfBits;
			return retval;
		}

        /// <summary>
        /// Reads the specified number of bytes and returns whether the read succeeded.
        /// </summary>
        public bool ReadBytes(int numberOfBytes, out byte[] result)
		{
			if (m_bitLength - m_readPosition + 7 < (numberOfBytes * 8))
			{
				result = null;
				return false;
			}

			result = new byte[numberOfBytes];
			NetBitWriter.ReadBytes(m_data, m_readPosition, result);
			m_readPosition += (8 * numberOfBytes);
			return true;
		}

        /// <summary>
        /// Reads a block of bytes from the stream.
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            Read(bytes);
            return bytes;
        }
        
        /// <summary>
        /// Reads a block of bytes from the stream and writes the data to a given buffer.
        /// </summary>
        /// <param name="span">The destination span.</param>
		public int Read(Span<byte> span)
        {
            NetException.Assert(m_bitLength - m_readPosition + 7 >= (span.Length * 8), c_readOverflowError);

            int bitsToRead = Math.Min((m_bitLength - m_readPosition + 7), span.Length * 8);
            int bytesToRead = bitsToRead / 8;

            NetBitWriter.ReadBytes(m_data, m_readPosition, span.Slice(0, bytesToRead));
            m_readPosition += bitsToRead;
            return bytesToRead;
        }

        /// <summary>
        /// Reads a block of bytes from the stream and writes the data in a given buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset where to start writing in the destination buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
		public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// Reads the specified number of bits into a given buffer.
        /// </summary>
        /// <param name="span">The destination span.</param>
        /// <param name="numberOfBits">The number of bits to read</param>
        public void ReadBits(Span<byte> span, int numberOfBits)
		{
			NetException.Assert(m_bitLength - m_readPosition >= numberOfBits, c_readOverflowError);
			NetException.Assert(NetUtility.BytesNeededToHoldBits(numberOfBits) <= span.Length);

			int numberOfWholeBytes = numberOfBits / 8;
			int extraBits = numberOfBits - (numberOfWholeBytes * 8);

			NetBitWriter.ReadBytes(m_data, m_readPosition, span.Slice(0, numberOfWholeBytes));
			m_readPosition += (8 * numberOfWholeBytes);

			if (extraBits > 0)
                span[numberOfWholeBytes] = ReadByte(extraBits);
		}

		/// <summary>
		/// Reads a 16 bit <see cref="short"/> written by <see cref="Write(short)"/>.
		/// </summary>
		public short ReadInt16()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 16, c_readOverflowError);
			uint retval = NetBitWriter.ReadUInt16(m_data, 16, m_readPosition);
			m_readPosition += 16;
			return (short)retval;
		}

        /// <summary>
        /// Reads a 16 bit <see cref="ushort"/> written by <see cref="Write(ushort)"/>.
        /// </summary>
        [CLSCompliant(false)]
		public ushort ReadUInt16()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 16, c_readOverflowError);
			uint retval = NetBitWriter.ReadUInt16(m_data, 16, m_readPosition);
			m_readPosition += 16;
			return (ushort)retval;
		}

        /// <summary>
        /// Reads a 32 bit <see cref="int"/> written by <see cref="Write(int)"/>.
        /// </summary>
        public int ReadInt32()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);
			uint retval = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
			m_readPosition += 32;
			return (int)retval;
		}

		/// <summary>
        /// Reads a 32 bit <see cref="int"/> written by <see cref="Write(int)"/>.
		/// </summary>
		[CLSCompliant(false)]
		public bool ReadInt32(out int result)
		{
			if (m_bitLength - m_readPosition < 32)
			{
				result = 0;
				return false;
			}
			result = (int)NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
			m_readPosition += 32;
			return true;
		}

		/// <summary>
		/// Reads a <see cref="int"/> stored in 1 to 32 bits, written by <see cref="Write(int, int)"/>.
		/// </summary>
		public int ReadInt32(int numberOfBits)
		{
			NetException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadInt32(bits) can only read between 1 and 32 bits");
			NetException.Assert(m_bitLength - m_readPosition >= numberOfBits, c_readOverflowError);

			uint retval = NetBitWriter.ReadUInt32(m_data, numberOfBits, m_readPosition);
			m_readPosition += numberOfBits;

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
				return -((int)tmp);
			}
		}

        /// <summary>
        /// Reads a <see cref="uint"/> written by <see cref="Write(uint)"/>.
        /// </summary>
        [CLSCompliant(false)]
		public uint ReadUInt32()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);
			uint retval = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
			m_readPosition += 32;
			return retval;
		}

        /// <summary>
        /// Reads a 32 bit <see cref="uint"/> written by <see cref="Write(uint)"/> and returns whether the read succeeded.
        /// </summary>
        [CLSCompliant(false)]
		public bool ReadUInt32(out uint result)
		{
			if (m_bitLength - m_readPosition < 32)
			{
				result = 0;
				return false;
			}
			result = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
			m_readPosition += 32;
			return true;
		}

		/// <summary>
		/// Reads an <see cref="uint"/> stored in 1 to 32 bits, written by <see cref="Write(uint, int)"/>.
		/// </summary>
		[CLSCompliant(false)]
		public uint ReadUInt32(int numberOfBits)
		{
			NetException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadUInt32(bits) can only read between 1 and 32 bits");
            //NetException.Assert(m_bitLength - m_readBitPtr >= numberOfBits, "tried to read past buffer size");

            uint retval = NetBitWriter.ReadUInt32(m_data, numberOfBits, m_readPosition);
			m_readPosition += numberOfBits;
			return retval;
		}

		/// <summary>
		/// Reads a 64 bit <see cref="ulong"/> written by <see cref="Write(ulong)"/>.
		/// </summary>
		[CLSCompliant(false)]
		public ulong ReadUInt64()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);

			ulong low = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
			m_readPosition += 32;
			ulong high = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);

			ulong retval = low + (high << 32);

			m_readPosition += 32;
			return retval;
		}

        /// <summary>
        /// Reads a 64 bit <see cref="long"/> written by <see cref="Write(long)"/>.
        /// </summary>
        public long ReadInt64()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);
			unchecked
			{
				ulong retval = ReadUInt64();
				long longRetval = (long)retval;
				return longRetval;
			}
		}

		/// <summary>
		/// Reads an <see cref="ulong"/> stored in 1 to 64 bits, written by <see cref="Write(ulong, int)"/>.
		/// </summary>
		[CLSCompliant(false)]
		public ulong ReadUInt64(int numberOfBits)
		{
			NetException.Assert(numberOfBits > 0 && numberOfBits <= 64, "ReadUInt64(bits) can only read between 1 and 64 bits");
			NetException.Assert(m_bitLength - m_readPosition >= numberOfBits, c_readOverflowError);

			ulong retval;
			if (numberOfBits <= 32)
			{
				retval = (ulong)NetBitWriter.ReadUInt32(m_data, numberOfBits, m_readPosition);
			}
			else
			{
				retval = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
				retval |= NetBitWriter.ReadUInt32(m_data, numberOfBits - 32, m_readPosition) << 32;
			}
			m_readPosition += numberOfBits;
			return retval;
		}

        /// <summary>
        /// Reads a <see cref="long"/> stored in 1 to 64 bits, written by <see cref="Write(long, int)"/>.
        /// </summary>
        public long ReadInt64(int numberOfBits)
		{
			NetException.Assert(((numberOfBits > 0) && (numberOfBits <= 64)), "ReadInt64(bits) can only read between 1 and 64 bits");
			return (long)ReadUInt64(numberOfBits);
		}

        /// <summary>
        /// Reads a 32 bit <see cref="float"/> written by <see cref="Write(float)"/>.
        /// </summary>
        public float ReadFloat()
		{
			return ReadSingle();
		}

        /// <summary>
        /// Reads a 32 bit <see cref="float"/> written by <see cref="Write(float)"/>.
        /// </summary>
        public float ReadSingle()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);

			if ((m_readPosition & 7) == 0) // read directly
			{
				float retval = BitConverter.ToSingle(m_data, m_readPosition >> 3);
				m_readPosition += 32;
				return retval;
			}

			byte[] bytes = ReadBytes(4);
			return BitConverter.ToSingle(bytes, 0);
		}

        /// <summary>
        /// Reads a 32 bit <see cref="float"/> written by <see cref="Write(float)"/>.
        /// </summary>
        public bool ReadSingle(out float result)
		{
			if (m_bitLength - m_readPosition < 32)
			{
				result = 0.0f;
				return false;
			}

			if ((m_readPosition & 7) == 0) // read directly
			{
				result = BitConverter.ToSingle(m_data, m_readPosition >> 3);
				m_readPosition += 32;
				return true;
			}

			byte[] bytes = ReadBytes(4);
			result = BitConverter.ToSingle(bytes, 0);
			return true;
		}

        /// <summary>
        /// Reads a 64 bit <see cref="double"/> written by <see cref="Write(double)"/>.
        /// </summary>
        public double ReadDouble()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);

			if ((m_readPosition & 7) == 0) // read directly
			{
				// read directly
				double retval = BitConverter.ToDouble(m_data, m_readPosition >> 3);
				m_readPosition += 64;
				return retval;
			}

			byte[] bytes = ReadBytes(8);
			return BitConverter.ToDouble(bytes, 0);
		}

        /// <summary>
        /// Reads a variable sized <see cref="uint"/> written by <see cref="WriteVariableUInt32"/>.
        /// </summary>
        [CLSCompliant(false)]
		public uint ReadVariableUInt32()
		{
			int num1 = 0;
			int num2 = 0;
			while (m_bitLength - m_readPosition >= 8)
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
        /// Reads a variable sized <see cref="uint"/> written by <see cref="WriteVariableUInt32"/>
        /// and returns whether the read succeeded.
		/// </summary>
		[CLSCompliant(false)]
		public bool ReadVariableUInt32(out uint result)
		{
			int num1 = 0;
			int num2 = 0;
			while (m_bitLength - m_readPosition >= 8)
			{
                if (TryReadByte(out byte num3) == false)
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
        /// Reads a variable sized <see cref="int"/> written by <see cref="WriteVariableInt32"/>.
        /// </summary>
        public int ReadVariableInt32()
		{
			uint n = ReadVariableUInt32();
			return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
		}

        /// <summary>
        /// Reads a variable sized <see cref="long"/> written by <see cref="WriteVariableInt64"/>.
        /// </summary>
        public long ReadVariableInt64()
		{
            ulong n = ReadVariableUInt64();
			return (long)(n >> 1) ^ -(long)(n & 1); // decode zigzag
		}

        /// <summary>
        /// Reads a variable sized <see cref="ulong"/> written by <see cref="WriteVariableInt64"/>.
        /// </summary>
        [CLSCompliant(false)]
		public ulong ReadVariableUInt64()
		{
            ulong num1 = 0;
			int num2 = 0;
			while (m_bitLength - m_readPosition >= 8)
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
        /// Reads a 32 bit <see cref="float"/> written by <see cref="WriteSignedSingle"/>.
        /// </summary>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to -1 and smaller or equal to 1</returns>
        public float ReadSignedSingle(int numberOfBits)
		{
			uint encodedVal = ReadUInt32(numberOfBits);
			int maxVal = (1 << numberOfBits) - 1;
			return ((encodedVal + 1) / (float)(maxVal + 1) - 0.5f) * 2.0f;
		}

        /// <summary>
        /// Reads a 32 bit <see cref="float"/> written by <see cref="WriteUnitSingle"/>.
        /// </summary>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to 0 and smaller or equal to 1</returns>
        public float ReadUnitSingle(int numberOfBits)
		{
			uint encodedVal = ReadUInt32(numberOfBits);
			int maxVal = (1 << numberOfBits) - 1;
			return (float)(encodedVal + 1) / (float)(maxVal + 1);
		}

        /// <summary>
        /// Reads a 32 bit <see cref="float"/> written by <see cref="WriteRangedSingle"/>.
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to MIN and smaller or equal to MAX</returns>
        public float ReadRangedSingle(float min, float max, int numberOfBits)
		{
			float range = max - min;
			int maxVal = (1 << numberOfBits) - 1;
			float encodedVal = (float)ReadUInt32(numberOfBits);
			float unit = encodedVal / (float)maxVal;
			return min + (unit * range);
		}

        /// <summary>
        /// Reads a 32 bit <see cref="int"/> written by <see cref="WriteRangedInteger"/>.
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
        public int ReadRangedInteger(int min, int max)
		{
			uint range = (uint)(max - min);
			int numBits = NetUtility.BitsToHoldUInt(range);

			uint rvalue = ReadUInt32(numBits);
			return (int)(min + rvalue);
		}

        /// <summary>
        /// Reads a <see cref="string"/> written by <see cref="Write(string)"/>.
        /// </summary>
        public string ReadString()
		{
			int byteLen = (int)ReadVariableUInt32();
			if (byteLen <= 0)
				return string.Empty;

			if ((ulong)(m_bitLength - m_readPosition) < ((ulong)byteLen * 8))
			{
				// not enough data
#if DEBUG
				
				throw new NetException(c_readOverflowError);
#else
				m_readPosition = m_bitLength;
				return null; // unfortunate; but we need to protect against DDOS
#endif
			}

			if ((m_readPosition & 7) == 0)
			{
				// read directly
				string retval = System.Text.Encoding.UTF8.GetString(m_data, m_readPosition >> 3, byteLen);
				m_readPosition += (8 * byteLen);
				return retval;
			}

            byte[] bytes = ReadBytes(byteLen);
			return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}

        /// <summary>
        /// Reads a <see cref="string"/> written by <see cref="Write(string)"/> 
        /// and returns whether the read succeeded.
        /// </summary>
        public bool ReadString(out string result)
		{
            if (ReadVariableUInt32(out uint byteLen) == false)
            {
                result = string.Empty;
                return false;
            }

            if (byteLen <= 0)
			{
				result = string.Empty;
				return true;
			}

			if (m_bitLength - m_readPosition < (byteLen * 8))
			{
				result = string.Empty;
				return false;
			}

			if ((m_readPosition & 7) == 0)
			{
				// read directly
				result = System.Text.Encoding.UTF8.GetString(m_data, m_readPosition >> 3, (int)byteLen);
				m_readPosition += (8 * (int)byteLen);
				return true;
			}

            if (ReadBytes((int)byteLen, out byte[] bytes) == false)
            {
                result = string.Empty;
                return false;
            }

            result = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			return true;
		}

        /// <summary>
        /// Reads a value, in local time comparable to <see cref="NetTime.Now"/>,
        /// written by <see cref="WriteTime(bool)"/> for the given <see cref="NetConnection"/>.
        /// </summary>
        public double ReadTime(NetConnection connection, bool highPrecision)
		{
			double remoteTime = highPrecision ? ReadDouble() : ReadSingle();

			if (connection == null)
				throw new NetException("Cannot call ReadTime() on message without a connected sender (ie. unconnected messages)");

			// lets bypass NetConnection.GetLocalTime for speed
			return remoteTime - connection.m_remoteTimeOffset;
		}

		/// <summary>
		/// Reads a stored <see cref="IPEndPoint"/> description.
		/// </summary>
		public IPEndPoint ReadIPEndPoint()
		{
            IPAddress address = ReadIPAddress();
			int port = ReadUInt16();
			return new IPEndPoint(address, port);
		}

        /// <summary>
        /// Reads a stored <see cref="IPAddress"/>.
        /// </summary>
        public IPAddress ReadIPAddress()
        {
            byte len = ReadByte();
            byte[] addressBytes = ReadBytes(len);
            return new IPAddress(addressBytes);
        }

		/// <summary>
		/// Pads data with enough bits to reach a full <see cref="byte"/>. Decreases CPU usage for subsequent byte writes.
		/// </summary>
		public void SkipPadBits()
		{
			m_readPosition = ((m_readPosition + 7) >> 3) * 8;
		}

		/// <summary>
		/// Pads data with enough bits to reach a full <see cref="byte"/>. Decreases CPU usage for subsequent byte writes.
		/// </summary>
		public void ReadPadBits()
		{
			m_readPosition = ((m_readPosition + 7) >> 3) * 8;
		}

		/// <summary>
		/// Pads data with the specified number of bits.
		/// </summary>
		public void SkipPadBits(int numberOfBits)
		{
			m_readPosition += numberOfBits;
		}
	}
}
