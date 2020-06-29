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
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
    // TODO: check NetBuffer.Read
    // TODO: add PeekTimeSpan()

    public partial class NetBuffer
    {
        /// <summary>
        /// Tries to read the specified number of bits without advancing the read position.
        /// </summary>
        public bool TryPeek(Span<byte> destination, int bitCount)
        {
            if (!HasEnough(bitCount))
                return false;

            NetBitWriter.CopyBits(Data, BitPosition, bitCount, destination, 0);
            return true;
        }

        /// <summary>
        /// Tries to read the specified number of bits, 
        /// between one and <paramref name="maxBitCount"/>, 
        /// without advancing the read position.
        /// </summary>
        public bool TryPeek(Span<byte> destination, int bitCount, int maxBitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            return TryPeek(destination, bitCount);
        }

        /// <summary>
        /// Reads the specified number of bits without advancing the read position.
        /// </summary>
        public void Peek(Span<byte> destination, int bitCount)
        {
            if (!TryPeek(destination, bitCount))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Reads the specified number of bits,
        /// between one and <paramref name="maxBitCount"/>, 
        /// without advancing the read position.
        /// </summary>
        public void Peek(Span<byte> destination, int bitCount, int maxBitCount)
        {
            if (!TryPeek(destination, bitCount, maxBitCount))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Tries to read the specified number of bytes without advancing the read position.
        /// </summary>
        public bool TryPeek(Span<byte> destination)
        {
            if (!IsByteAligned)
                return TryPeek(destination, destination.Length * 8);

            if (!HasEnough(destination.Length))
                return false;

            Data.AsSpan(BytePosition, destination.Length).CopyTo(destination);
            return true;
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read position.
        /// </summary>
        public void Peek(Span<byte> destination)
        {
            if (!TryPeek(destination))
                throw new EndOfMessageException();
        }

        /// <summary>
        /// Reads a 1-bit <see cref="bool"/> without advancing the read position.
        /// </summary>
        public bool PeekBool()
        {
            if (!HasEnough(1))
                throw new EndOfMessageException();
            return NetBitWriter.ReadByteUnchecked(Data, BitPosition, 1) > 0;
        }

        /// <summary>
        /// Reads an <see cref="sbyte"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public sbyte PeekSByte()
        {
            if (!HasEnough(8))
                throw new EndOfMessageException();
            return (sbyte)NetBitWriter.ReadByteUnchecked(Data, BitPosition, 8);
        }

        /// <summary>
        /// Reads a <see cref="byte"/> without advancing the read position.
        /// </summary>
        public byte PeekByte()
        {
            if (!HasEnough(8))
                throw new EndOfMessageException();
            return NetBitWriter.ReadByteUnchecked(Data, BitPosition, 8);
        }

        /// <summary>
        /// Reads the specified number of bits into a <see cref="byte"/> without advancing the read position.
        /// </summary>
        public byte PeekByte(int bitCount)
        {
            if (!HasEnough(bitCount))
                throw new EndOfMessageException();
            return NetBitWriter.ReadByteUnchecked(Data, BitPosition, bitCount);
        }

        #region Int16

        /// <summary>
        /// Reads an <see cref="short"/> without advancing the read position.
        /// </summary>
        public short PeekInt16()
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            Peek(tmp);
            return BinaryPrimitives.ReadInt16LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a <see cref="ushort"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public ushort PeekUInt16()
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            Peek(tmp);
            return BinaryPrimitives.ReadUInt16LittleEndian(tmp);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="short"/> without advancing the read position.
        /// </summary>
        public short PeekInt16(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            Peek(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadInt16LittleEndian(tmp);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="ushort"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public ushort PeekUInt16(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            Peek(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadUInt16LittleEndian(tmp);
        }

        #endregion

        #region Int32

        /// <summary>
        /// Reads an <see cref="int"/> without advancing the read position.
        /// </summary>
        public int PeekInt32()
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            Peek(tmp);
            return BinaryPrimitives.ReadInt32LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a <see cref="uint"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public uint PeekUInt32()
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            Peek(tmp);
            return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="int"/> without advancing the read position.
        /// </summary>
        public int PeekInt32(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            Peek(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadInt32LittleEndian(tmp);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="uint"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public uint PeekUInt32(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(uint)];
            Peek(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
        }

        #endregion

        #region Int64

        /// <summary>
        /// Reads an <see cref="long"/> without advancing the read position.
        /// </summary>
        public long PeekInt64()
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            Peek(tmp);
            return BinaryPrimitives.ReadInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads a <see cref="ulong"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public ulong PeekUInt64()
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            Peek(tmp);
            return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="long"/> without advancing the read position.
        /// </summary>
        public long PeekInt64(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            Peek(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadInt64LittleEndian(tmp);
        }

        /// <summary>
        /// Reads the specified number of bits into an <see cref="ulong"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public ulong PeekUInt64(int bitCount)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ulong)];
            Peek(tmp, bitCount, tmp.Length * 8);
            return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
        }

        #endregion

        #region VarInt 

        /// <summary>
        /// Tries to read a variable sized <see cref="uint"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public OperationStatus PeekVarUInt32(out uint result)
        {
            return NetBitWriter.ReadVarUInt32(this, peek: true, out result);
        }

        /// <summary>
        /// Tries to read a variable sized <see cref="ulong"/> without advancing the read position.
        /// </summary>
        [CLSCompliant(false)]
        public OperationStatus PeekVarUInt64(out ulong result)
        {
            return NetBitWriter.ReadVarUInt64(this, peek: true, out result);
        }

        [CLSCompliant(false)]
        public uint PeekVarUInt32()
        {
            var status = PeekVarUInt32(out uint value);
            if (status == OperationStatus.Done)
                return value;

            if (status == OperationStatus.NeedMoreData)
                throw new EndOfMessageException();

            return default;
        }

        [CLSCompliant(false)]
        public ulong PeekVarUInt64()
        {
            var status = PeekVarUInt64(out ulong value);
            if (status == OperationStatus.Done)
                return value;

            if (status == OperationStatus.NeedMoreData)
                throw new EndOfMessageException();

            return default;
        }

        /// <summary>
        /// Reads a variable sized <see cref="int"/> written by <see cref="WriteVar(int)"/>.
        /// </summary>
        public int PeekVarInt32()
        {
            uint n = PeekVarUInt32();
            return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized <see cref="long"/> written by <see cref="WriteVar(long)"/>.
        /// </summary>
        public long PeekVarInt64()
        {
            ulong n = PeekVarUInt64();
            return (long)(n >> 1) ^ -(long)(n & 1); // decode zigzag
        }

        #endregion

        #region Float

        /// <summary>
        /// Reads a 32-bit <see cref="float"/> without advancing the read position.
        /// </summary>
        public float PeekSingle()
        {
            Span<byte> tmp = stackalloc byte[sizeof(float)];
            Peek(tmp);
            return Unsafe.ReadUnaligned<float>(ref MemoryMarshal.GetReference(tmp));
        }

        /// <summary>
        /// Reads a 64-bit <see cref="double"/> without advancing the read position.
        /// </summary>
        public double PeekDouble()
        {
            Span<byte> tmp = stackalloc byte[sizeof(double)];
            Peek(tmp);
            return Unsafe.ReadUnaligned<double>(ref MemoryMarshal.GetReference(tmp));
        }

        #endregion

        public bool PeekStringHeader(out NetStringHeader header)
        {
            int startPosition = BitPosition;
            bool read = ReadStringHeader(out header);
            BitPosition = startPosition;
            return read;
        }

        /// <summary>
        /// Reads a <see cref="string"/> without advancing the read position.
        /// </summary>
        public string PeekString()
        {
            int startPosition = BitPosition;
            string str = ReadString();
            BitPosition = startPosition;
            return str;
        }

        /// <summary>
        /// Reads a <see cref="TimeSpan"/> without advancing the read position.
        /// </summary>
        public TimeSpan PeekTimeSpan()
        {
            return new TimeSpan(PeekVarInt64());
        }

        /// <summary>
        /// Reads an enum of type <typeparamref name="TEnum"/> without advancing the read position.
        /// </summary>
        public TEnum PeekEnum<TEnum>()
            where TEnum : Enum
        {
            long value = PeekVarInt64();
            return EnumConverter.Convert<TEnum>(value);
        }
    }
}

