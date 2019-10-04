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
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
	/// <summary>
	/// Utility struct for writing Singles.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
    internal struct SingleUIntUnion
	{
		/// <summary>
		/// Value as a 32-bit float.
		/// </summary>
		[FieldOffset(0)]
		public float SingleValue;

		/// <summary>
		/// Value as an unsigned 32-bit integer.
		/// </summary>
		[FieldOffset(0)]
		public uint UIntValue;
	}

    /// <summary>
	/// Utility struct for writing Doubles.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
    internal struct DoubleULongUnion
    {
        /// <summary>
        /// Value as a 64-bit float.
        /// </summary>
        [FieldOffset(0)]
        public double DoubleValue;

        /// <summary>
        /// Value as an unsigned 64-bit integer.
        /// </summary>
        [FieldOffset(0)]
        public ulong ULongValue;
    }

    public partial class NetBuffer
	{
		/// <summary>
		/// Ensures the buffer can hold this number of bits.
		/// </summary>
		public void EnsureBufferSize(int numberOfBits)
		{
			int byteLen = ((numberOfBits + 7) >> 3);
			if (m_data == null)
			{
				m_data = new byte[byteLen + c_overAllocateAmount];
				return;
			}
			if (m_data.Length < byteLen)
				Array.Resize(ref m_data, byteLen + c_overAllocateAmount);
			return;
		}

		/// <summary>
		/// Ensures the buffer can hold this number of bits.
		/// </summary>
		internal void InternalEnsureBufferSize(int numberOfBits)
		{
			int byteLen = ((numberOfBits + 7) >> 3);
			if (m_data == null)
			{
				m_data = new byte[byteLen];
				return;
			}
			if (m_data.Length < byteLen)
				Array.Resize(ref m_data, byteLen);
			return;
		}

		/// <summary>
		/// Writes a <see cref="bool"/> value using 1 bit.
		/// </summary>
		public void Write(bool value)
		{
			EnsureBufferSize(m_bitLength + 1);
			NetBitWriter.WriteByte((value ? (byte)1 : (byte)0), 1, m_data, m_bitLength);
			m_bitLength += 1;
		}

		/// <summary>
		/// Write a <see cref="byte"/>.
		/// </summary>
		public void Write(byte source)
		{
			EnsureBufferSize(m_bitLength + 8);
			NetBitWriter.WriteByte(source, 8, m_data, m_bitLength);
			m_bitLength += 8;
		}

		/// <summary>
		/// Writes a <see cref="sbyte"/>.
		/// </summary>
		[CLSCompliant(false)]
		public void Write(sbyte source)
		{
			EnsureBufferSize(m_bitLength + 8);
			NetBitWriter.WriteByte((byte)source, 8, m_data, m_bitLength);
			m_bitLength += 8;
		}

		/// <summary>
		/// Writes 1 to 8 bits of a <see cref="byte"/>.
		/// </summary>
		public void Write(byte source, int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 8), "Write(byte, numberOfBits) can only write between 1 and 8 bits");
			EnsureBufferSize(m_bitLength + numberOfBits);
			NetBitWriter.WriteByte(source, numberOfBits, m_data, m_bitLength);
			m_bitLength += numberOfBits;
		}

        /// <summary>
		/// Writes all bytes in a span.
		/// </summary>
		public void Write(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty)
                return;

            int bits = source.Length * 8;
            EnsureBufferSize(m_bitLength + bits);
            NetBitWriter.WriteBytes(source, m_data, m_bitLength);
            m_bitLength += bits;
        }

		/// <summary>
		/// Writes the specified number of bytes from an array.
		/// </summary>
		public void Write(byte[] source, int offsetInBytes, int numberOfBytes)
		{
            Write(source.AsSpan(offsetInBytes, numberOfBytes));
		}

		/// <summary>
		/// Writes an 16-bit <see cref="ushort"/>.
		/// </summary>
		/// <param name="source"></param>
		[CLSCompliant(false)]
		public void Write(ushort source)
		{
			EnsureBufferSize(m_bitLength + 16);
			NetBitWriter.WriteUInt16(source, 16, m_data, m_bitLength);
			m_bitLength += 16;
		}

		/// <summary>
		/// Writes a 16-bit <see cref="uint"/> at a given offset in the buffer.
		/// </summary>
		[CLSCompliant(false)]
		public void WriteAt(int offset, ushort source)
		{
			int newBitLength = Math.Max(m_bitLength, offset + 16);
			EnsureBufferSize(newBitLength);
			NetBitWriter.WriteUInt16(source, 16, m_data, offset);
			m_bitLength = newBitLength;
		}

		/// <summary>
		/// Writes an unsigned <see cref="ushort"/> using 1 to 16 bits.
		/// </summary>
		[CLSCompliant(false)]
		public void Write(ushort source, int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 16), "Write(ushort, numberOfBits) can only write between 1 and 16 bits");
			EnsureBufferSize(m_bitLength + numberOfBits);
			NetBitWriter.WriteUInt16(source, numberOfBits, m_data, m_bitLength);
			m_bitLength += numberOfBits;
		}

		/// <summary>
		/// Writes a 16-bit <see cref="short"/>.
		/// </summary>
		public void Write(short source)
		{
			EnsureBufferSize(m_bitLength + 16);
			NetBitWriter.WriteUInt16((ushort)source, 16, m_data, m_bitLength);
			m_bitLength += 16;
		}

		/// <summary>
		/// Writes a 16-bit <see cref="int"/> at a given offset in the buffer.
		/// </summary>
		public void WriteAt(int offset, short source)
		{
			int newBitLength = Math.Max(m_bitLength, offset + 16);
			EnsureBufferSize(newBitLength);
			NetBitWriter.WriteUInt16((ushort)source, 16, m_data, offset);
			m_bitLength = newBitLength;
		}

#if UNSAFE
		/// <summary>
		/// Writes a 32-bit <see cref="int"/>.
		/// </summary>
		public unsafe void Write(Int32 source)
		{
			EnsureBufferSize(m_bitLength + 32);

			// can write fast?
			if (m_bitLength % 8 == 0)
			{
				fixed (byte* numRef = &Data[m_bitLength / 8])
					*((int*)numRef) = source;
			}
			else
			{
				NetBitWriter.WriteUInt32((UInt32)source, 32, Data, m_bitLength);
			}
			m_bitLength += 32;
		}
#else
        /// <summary>
        /// Writes a 32-bit <see cref="int"/>.
        /// </summary>
        public void Write(int source)
		{
			EnsureBufferSize(m_bitLength + 32);
			NetBitWriter.WriteUInt32((uint)source, 32, m_data, m_bitLength);
			m_bitLength += 32;
		}
#endif

        /// <summary>
        /// Writes a 32-bit <see cref="int"/> at a given offset in the buffer.
        /// </summary>
        public void WriteAt(int offset, int source)
		{
			int newBitLength = Math.Max(m_bitLength, offset + 32);
			EnsureBufferSize(newBitLength);
			NetBitWriter.WriteUInt32((uint)source, 32, m_data, offset);
			m_bitLength = newBitLength;
		}

#if UNSAFE
		/// <summary>
		/// Writes a 32-bit <see cref="uint"/>.
		/// </summary>
		public unsafe void Write(UInt32 source)
		{
			EnsureBufferSize(m_bitLength + 32);

			// can write fast?
			if (m_bitLength % 8 == 0)
			{
				fixed (byte* numRef = &Data[m_bitLength / 8])
				{
					*((uint*)numRef) = source;
				}
			}
			else
			{
				NetBitWriter.WriteUInt32(source, 32, Data, m_bitLength);
			}

			m_bitLength += 32;
		}
#else
        /// <summary>
        /// Writes a 32-bit <see cref="uint"/>.
        /// </summary>
        [CLSCompliant(false)]
		public void Write(uint source)
		{
			EnsureBufferSize(m_bitLength + 32);
			NetBitWriter.WriteUInt32(source, 32, m_data, m_bitLength);
			m_bitLength += 32;
		}
#endif

        /// <summary>
        /// Writes a 32-bit <see cref="uint"/> at a given offset in the buffer.
        /// </summary>
        [CLSCompliant(false)]
		public void WriteAt(int offset, uint source)
		{
			int newBitLength = Math.Max(m_bitLength, offset + 32);
			EnsureBufferSize(newBitLength);
			NetBitWriter.WriteUInt32(source, 32, m_data, offset);
			m_bitLength = newBitLength;
		}

        /// <summary>
        /// Writes a <see cref="uint"/> using 1 to 32 bits.
        /// </summary>
        [CLSCompliant(false)]
		public void Write(uint source, int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "Write(uint, numberOfBits) can only write between 1 and 32 bits");
			EnsureBufferSize(m_bitLength + numberOfBits);
			NetBitWriter.WriteUInt32(source, numberOfBits, m_data, m_bitLength);
			m_bitLength += numberOfBits;
		}

        /// <summary>
        /// Writes a <see cref="int"/> using 1 to 32 bits.
        /// </summary>
        public void Write(int source, int numberOfBits)
		{
			NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "Write(int, numberOfBits) can only write between 1 and 32 bits");
			EnsureBufferSize(m_bitLength + numberOfBits);

			if (numberOfBits != 32)
			{
				// make first bit sign
				int signBit = 1 << (numberOfBits - 1);
				if (source < 0)
					source = (-source - 1) | signBit;
				else
					source &= (~signBit);
			}

			NetBitWriter.WriteUInt32((uint)source, numberOfBits, m_data, m_bitLength);

			m_bitLength += numberOfBits;
		}

		/// <summary>
		/// Writes a 64-bit <see cref="ulong"/>.
		/// </summary>
		[CLSCompliant(false)]
		public void Write(ulong source)
		{
			EnsureBufferSize(m_bitLength + 64);
			NetBitWriter.WriteUInt64(source, 64, m_data, m_bitLength);
			m_bitLength += 64;
		}

        /// <summary>
        /// Writes a 64-bit <see cref="ulong"/> at a given offset in the buffer.
        /// </summary>
        [CLSCompliant(false)]
		public void WriteAt(int offset, ulong source)
		{
			int newBitLength = Math.Max(m_bitLength, offset + 64);
			EnsureBufferSize(newBitLength);
			NetBitWriter.WriteUInt64(source, 64, m_data, offset);
			m_bitLength = newBitLength;
		}

        /// <summary>
        /// Writes an <see cref="ulong"/> using 1 to 64 bits
        /// </summary>
        [CLSCompliant(false)]
		public void Write(ulong source, int numberOfBits)
		{
			EnsureBufferSize(m_bitLength + numberOfBits);
			NetBitWriter.WriteUInt64(source, numberOfBits, m_data, m_bitLength);
			m_bitLength += numberOfBits;
		}

        /// <summary>
        /// Writes a 64-bit <see cref="long"/>.
        /// </summary>
        public void Write(long source)
		{
			EnsureBufferSize(m_bitLength + 64);
			ulong usource = (ulong)source;
			NetBitWriter.WriteUInt64(usource, 64, m_data, m_bitLength);
			m_bitLength += 64;
		}

        /// <summary>
        /// Writes a <see cref="long"/> using 1 to 64 bits.
        /// </summary>
        public void Write(long source, int numberOfBits)
		{
			EnsureBufferSize(m_bitLength + numberOfBits);
			ulong usource = (ulong)source;
			NetBitWriter.WriteUInt64(usource, numberOfBits, m_data, m_bitLength);
			m_bitLength += numberOfBits;
		}

#if UNSAFE
		/// <summary>
		/// Writes a 32-bit <see cref="float"/>.
		/// </summary>
		public unsafe void Write(float source)
		{
			uint val = *((uint*)&source);
#if BIGENDIAN
            val = NetUtility.SwapByteOrder(val);
#endif
			Write(val);
		}
#else
        /// <summary>
        /// Writes a 32-bit <see cref="float"/>.
        /// </summary>
        public void Write(float source)
		{
			// Use union to avoid BitConverter.GetBytes() which allocates memory on the heap
			SingleUIntUnion su;
            su.UIntValue = 0;
			su.SingleValue = source;

#if BIGENDIAN
			// swap byte order
			su.UIntValue = NetUtility.SwapByteOrder(su.UIntValue);
#endif
			Write(su.UIntValue);
		}
#endif

#if UNSAFE
		/// <summary>
		/// Writes a 64-bit <see cref="double"/>.
		/// </summary>
		public unsafe void Write(double source)
		{
			ulong val = *((ulong*)&source);
#if BIGENDIAN
			val = NetUtility.SwapByteOrder(val);
#endif
			Write(val);
		}
#else
        /// <summary>
		/// Writes a 64-bit <see cref="double"/>.
		/// </summary>
		public void Write(double source)
        {
            // Use union to avoid BitConverter.GetBytes() which allocates memory on the heap
            DoubleULongUnion su;
            su.ULongValue = 0;
            su.DoubleValue = source;

#if BIGENDIAN
			// swap byte order
			su.ULongValue = NetUtility.SwapByteOrder(su.ULongValue);
#endif
            Write(su.ULongValue);
        }
#endif

		/// <summary>
		/// Write Base128 encoded variable sized <see cref="uint"/> of up to 32 bits.
		/// </summary>
		/// <returns>number of bytes written</returns>
		[CLSCompliant(false)]
		public int WriteVariableUInt32(uint value)
		{
			int retval = 1;
			uint num1 = (uint)value;
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
        /// Write Base128 encoded variable sized <see cref="int"/> of up to 32 bits.
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableInt32(int value)
		{
			uint zigzag = (uint)(value << 1) ^ (uint)(value >> 31);
			return WriteVariableUInt32(zigzag);
		}

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="long"/> of up to 64 bits.
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableInt64(long value)
		{
			ulong zigzag = (ulong)(value << 1) ^ (ulong)(value >> 63);
			return WriteVariableUInt64(zigzag);
		}

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="ulong"/> of up to 64 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        [CLSCompliant(false)]
		public int WriteVariableUInt64(ulong value)
		{
			int retval = 1;
            ulong num1 = (ulong)value;
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
		/// Compress (lossy) a <see cref="float"/> in the range -1..1 using the specified amount of bits.
		/// </summary>
		public void WriteSignedSingle(float value, int numberOfBits)
		{
			NetException.Assert(((value >= -1.0) && (value <= 1.0)), " WriteSignedSingle() must be passed a float in the range -1 to 1; val is " + value);

			float unit = (value + 1f) * 0.5f;
			int maxVal = (1 << numberOfBits) - 1;
			uint writeVal = (uint)(unit * (float)maxVal);

			Write(writeVal, numberOfBits);
		}

        /// <summary>
        /// Compress (lossy) a <see cref="float"/> in the range 0..1 using the specified amount of bits.
        /// </summary>
        public void WriteUnitSingle(float value, int numberOfBits)
		{
			NetException.Assert(((value >= 0.0) && (value <= 1.0)), " WriteUnitSingle() must be passed a float in the range 0 to 1; val is " + value);

			int maxValue = (1 << numberOfBits) - 1;
			uint writeVal = (uint)(value * (float)maxValue);

			Write(writeVal, numberOfBits);
		}

        /// <summary>
        /// Compress a <see cref="float"/> within a specified range using the specified amount of bits.
        /// </summary>
        public void WriteRangedSingle(float value, float min, float max, int numberOfBits)
		{
			NetException.Assert(((value >= min) && (value <= max)), " WriteRangedSingle() must be passed a float in the range MIN to MAX; val is " + value);

			float range = max - min;
			float unit = ((value - min) / range);
			int maxVal = (1 << numberOfBits) - 1;
			Write((uint)((float)maxVal * unit), numberOfBits);
		}

		/// <summary>
		/// Writes an <see cref="int"/> with the least amount of bits needed for the specified range.
		/// Returns the number of bits written.
		/// </summary>
		public int WriteRangedInteger(int min, int max, int value)
		{
			NetException.Assert(value >= min && value <= max, "Value not within min/max range!");

			uint range = (uint)(max - min);
			int numBits = NetUtility.BitsToHoldUInt(range);

			uint rvalue = (uint)(value - min);
			Write(rvalue, numBits);

			return numBits;
		}

		/// <summary>
		/// Write a <see cref="string"/>.
		/// </summary>
		public void Write(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				WriteVariableUInt32(0);
				return;
			}

			byte[] bytes = Encoding.UTF8.GetBytes(source);
			EnsureBufferSize(m_bitLength + 8 + (bytes.Length * 8));
			WriteVariableUInt32((uint)bytes.Length);
			Write(bytes);
		}

		/// <summary>
		/// Writes an <see cref="IPEndPoint"/> description.
		/// </summary>
		public void Write(IPEndPoint endPoint)
		{
			byte[] bytes = endPoint.Address.GetAddressBytes();
			Write((byte)bytes.Length);
			Write(bytes);
			Write((ushort)endPoint.Port);
		}

		/// <summary>
		/// Writes the current local time to a message; readable (and convertable to local time) by the remote host using ReadTime().
		/// </summary>
		public void WriteTime(bool highPrecision)
		{
			double localTime = NetTime.Now;
			if (highPrecision)
				Write(localTime);
			else
				Write((float)localTime);
		}

		/// <summary>
		/// Writes a local timestamp to a message; readable (and convertable to local time) by the remote host using ReadTime().
		/// </summary>
		public void WriteTime(double localTime, bool highPrecision)
		{
			if (highPrecision)
				Write(localTime);
			else
				Write((float)localTime);
		}

		/// <summary>
		/// Pads data with enough bits to reach a full byte. Decreases CPU usage for subsequent byte writes.
		/// </summary>
		public void WritePadBits()
		{
			m_bitLength = ((m_bitLength + 7) >> 3) * 8;
			EnsureBufferSize(m_bitLength);
		}

		/// <summary>
		/// Pads data with the specified number of bits.
		/// </summary>
		public void WritePadBits(int numberOfBits)
		{
			m_bitLength += numberOfBits;
			EnsureBufferSize(m_bitLength);
		}

		/// <summary>
		/// Append all the bits from a <see cref="NetBuffer"/> to this buffer.
		/// </summary>
		public void Write(NetBuffer buffer)
		{
			EnsureBufferSize(m_bitLength + (buffer.LengthBytes * 8));

			Write(buffer.m_data, 0, buffer.LengthBytes);

			// did we write excessive bits?
			int bitsInLastByte = (buffer.m_bitLength % 8);
			if (bitsInLastByte != 0)
			{
				int excessBits = 8 - bitsInLastByte;
				m_bitLength -= excessBits;
			}
		}
	}
}
