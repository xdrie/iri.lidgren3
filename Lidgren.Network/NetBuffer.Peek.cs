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
	public partial class NetBuffer
	{
        /// <summary>
        /// Gets the underlying data buffer. This array is recycled so do not keep 
        /// references to it after recycling the <see cref="NetBuffer"/>.
        /// </summary>
        public byte[] PeekDataBuffer()
        {
            return m_data;
        }
        
		/// <summary>
		/// Reads a 1-bit <see cref="bool"/> without advancing the read pointer.
		/// </summary>
		public bool PeekBoolean()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 1, c_readOverflowError);
			return NetBitWriter.ReadByte(m_data, 1, m_readPosition) > 0;
		}
        
		/// <summary>
		/// Reads a <see cref="byte"/> without advancing the read pointer.
		/// </summary>
		public byte PeekByte()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 8, c_readOverflowError);
			return NetBitWriter.ReadByte(m_data, 8, m_readPosition);
		}

		/// <summary>
		/// Reads an <see cref="sbyte"/> without advancing the read pointer.
		/// </summary>
		[CLSCompliant(false)]
		public sbyte PeekSByte()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 8, c_readOverflowError);
			return (sbyte)NetBitWriter.ReadByte(m_data, 8, m_readPosition);
		}

		/// <summary>
		/// Reads the specified number of bits into a <see cref="byte"/> without advancing the read pointer.
		/// </summary>
		public byte PeekByte(int numberOfBits)
		{
			return NetBitWriter.ReadByte(m_data, numberOfBits, m_readPosition);
		}

		/// <summary>
		/// Reads the specified number of bytes without advancing the read pointer.
		/// </summary>
		public byte[] PeekBytes(int numberOfBytes)
		{
			NetException.Assert(m_bitLength - m_readPosition >= (numberOfBytes * 8), c_readOverflowError);

			byte[] retval = new byte[numberOfBytes];
			NetBitWriter.ReadBytes(m_data, m_readPosition, retval);
			return retval;
		}

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer.
        /// </summary>
        public void PeekBytes(byte[] buffer, int offset, int count)
        {
            NetException.Assert(offset + count <= buffer.Length);
            PeekBytes(buffer.AsSpan(offset, count));
        }

		/// <summary>
		/// Reads the specified number of bytes without advancing the read pointer.
		/// </summary>
		public void PeekBytes(Span<byte> span)
		{
			NetException.Assert(m_bitLength - m_readPosition >= (span.Length * 8), c_readOverflowError);
			NetBitWriter.ReadBytes(m_data, m_readPosition, span);
		}

        public bool TryPeekBytes(Span<byte> span)
        {
            if (m_bitLength - m_readPosition >= (span.Length * 8))
                return false;

            PeekBytes(span);
            return true;
        }
        
        /// <summary>
        /// Reads an <see cref="short"/> without advancing the read pointer.
        /// </summary>
        public short PeekInt16()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 16, c_readOverflowError);
			return (short)NetBitWriter.ReadUInt16(m_data, 16, m_readPosition);
		}

		/// <summary>
		/// Reads a <see cref="ushort"/> without advancing the read pointer.
		/// </summary>
		[CLSCompliant(false)]
		public ushort PeekUInt16()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 16, c_readOverflowError);
			return NetBitWriter.ReadUInt16(m_data, 16, m_readPosition);
		}
        
		/// <summary>
		/// Reads an <see cref="int"/> without advancing the read pointer.
		/// </summary>
		public int PeekInt32()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);
			return (int)NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
		}

		/// <summary>
		/// Reads the specified number of bits into an <see cref="int"/> without advancing the read pointer.
		/// </summary>
		public int PeekInt32(int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadInt() can only read between 1 and 32 bits");
			NetException.Assert(m_bitLength - m_readPosition >= numberOfBits, c_readOverflowError);

			uint retval = NetBitWriter.ReadUInt32(m_data, numberOfBits, m_readPosition);

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
		/// Reads a <see cref="uint"/> without advancing the read pointer.
		/// </summary>
		[CLSCompliant(false)]
		public uint PeekUInt32()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);
			return NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
		}

		/// <summary>
		/// Reads the specified number of bits into a <see cref="uint"/> without advancing the read pointer.
		/// </summary>
		[CLSCompliant(false)]
		public uint PeekUInt32(int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadUInt() can only read between 1 and 32 bits");
            //NetException.Assert(m_bitLength - m_readBitPtr >= numberOfBits, "tried to read past buffer size");

            return NetBitWriter.ReadUInt32(m_data, numberOfBits, m_readPosition);
		}

		/// <summary>
		/// Reads a <see cref="ulong"/> without advancing the read pointer.
		/// </summary>
		[CLSCompliant(false)]
		public ulong PeekUInt64()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);

			ulong low = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
			ulong high = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition + 32);
            
            return low + (high << 32);
		}

		/// <summary>
		/// Reads an <see cref="long"/> without advancing the read pointer.
		/// </summary>
		public long PeekInt64()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);
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
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 64), "ReadUInt() can only read between 1 and 64 bits");
			NetException.Assert(m_bitLength - m_readPosition >= numberOfBits, c_readOverflowError);
            
			if (numberOfBits <= 32)
			{
				return NetBitWriter.ReadUInt32(m_data, numberOfBits, m_readPosition);
			}
			else
			{
				uint v1 = NetBitWriter.ReadUInt32(m_data, 32, m_readPosition);
                uint v2 = NetBitWriter.ReadUInt32(m_data, numberOfBits - 32, m_readPosition);
                return (ulong)(v1 | ((long)v2 << 32));
			}
		}

		/// <summary>
		/// Reads the specified number of bits into an <see cref="long"/> without advancing the read pointer.
		/// </summary>
		public long PeekInt64(int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0) && (numberOfBits < 65), "ReadInt64(bits) can only read between 1 and 64 bits");
			return (long)PeekUInt64(numberOfBits);
		}
        
		/// <summary>
		/// Reads a 32-bit <see cref="float"/> without advancing the read pointer.
        /// <para></para>
		/// </summary>
		public float PeekFloat()
		{
			return PeekSingle();
		}

		/// <summary>
		/// Reads a 32-bit <see cref="float"/> without advancing the read pointer.
		/// </summary>
		public float PeekSingle()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);

			if ((m_readPosition & 7) == 0) // read directly
				return BitConverter.ToSingle(m_data, m_readPosition >> 3);

			byte[] bytes = PeekBytes(4);
			return BitConverter.ToSingle(bytes, 0);
		}

		/// <summary>
		/// Reads a 64-bit <see cref="double"/> without advancing the read pointer.
		/// </summary>
		public double PeekDouble()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);

			if ((m_readPosition & 7) == 0) // read directly
                return BitConverter.ToDouble(m_data, m_readPosition >> 3);

			byte[] bytes = PeekBytes(8);
			return BitConverter.ToDouble(bytes, 0);
		}

		/// <summary>
		/// Reads a <see cref="string"/> without advancing the read pointer.
		/// </summary>
		public string PeekString()
		{
			int wasReadPosition = m_readPosition;
			string str = ReadString();
			m_readPosition = wasReadPosition;
			return str;
		}
	}
}

