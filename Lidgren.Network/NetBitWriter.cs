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
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Lidgren.Network
{
    // TODO: optimize with Intrinsics (remember to steal/look at old impl)

    /// <summary>
    /// Helper class for <see cref="NetBuffer"/> to write/read bits.
    /// </summary>
    public static class NetBitWriter
    {
        #region CopyBits

        /// <summary>
        /// Copies bits between buffers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void CopyBits(
            ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount,
            Span<byte> destination, int destinationBitOffset)
        {
            if (bitCount == 0)
                return;

            var src = source.Slice(sourceBitOffset / 8);
            var dst = destination.Slice(destinationBitOffset / 8);

            sourceBitOffset %= 8;
            destinationBitOffset %= 8;

            int srcNextRemBits = 8 - sourceBitOffset;
            int dstNextRemBits = 8 - destinationBitOffset;

            #region Bulk-copy most bits

            int i;
            if (sourceBitOffset > 0)
            {
                var byteSrc = src.Slice(0, (bitCount + 7) / 8);
                if (destinationBitOffset > 0)
                {
                    for (i = 0; i < byteSrc.Length - 1; i++)
                    {
                        int value = ReadBitsAtSrc(src, i, sourceBitOffset, srcNextRemBits);
                        WriteBitsAtDst(dst, i, value, destinationBitOffset, dstNextRemBits);
                    }
                }
                else
                {
                    for (i = 0; i < byteSrc.Length - 1; i++)
                    {
                        int value = ReadBitsAtSrc(src, i, sourceBitOffset, srcNextRemBits);
                        dst[i] = (byte)value;
                    }
                }
            }
            else if (destinationBitOffset > 0)
            {
                var byteSrc = src.Slice(0, (bitCount + 7) / 8);
                for (i = 0; i < byteSrc.Length - 1; i++)
                {
                    int value = byteSrc[i];
                    WriteBitsAtDst(dst, i, value, destinationBitOffset, dstNextRemBits);
                }
            }
            else
            {
                var byteSrc = src.Slice(0, bitCount / 8);
                byteSrc.CopyTo(dst);
                i = byteSrc.Length;
            }
            bitCount -= i * 8;

            #endregion

            #region Copy remaining bits

            if (bitCount == 0)
                return;

            // mask away unused bits lower than relevant bits in last byte
            int lastValue = src[i] >> sourceBitOffset;

            int bitsInNextByte = bitCount - srcNextRemBits;
            if (bitsInNextByte < 1)
            {
                // we don't need to read from the next byte, 
                // we need to mask away unused bits higher than relevant bits
                lastValue &= 255 >> (8 - bitCount);
            }
            else
            {
                int nextValue = src[i + 1];

                // mask away unused bits higher than relevant bits in next byte
                nextValue &= 255 >> (8 - bitsInNextByte);

                lastValue |= nextValue << srcNextRemBits;
            }

            int bitsLeftInDst = dstNextRemBits - bitCount;
            if (bitsLeftInDst >= 0)
            {
                // everything fits in the last byte

                int mask = (255 >> dstNextRemBits) | (255 << (8 - bitsLeftInDst));

                dst[i] = (byte)(
                    (dst[i] & mask) | // Mask out lower and upper bits
                    (lastValue << destinationBitOffset)); // Insert new bits
            }
            else
            {
                dst[i] = (byte)(
                    (dst[i] & (255 >> dstNextRemBits)) | // Mask out upper bits
                    (lastValue << destinationBitOffset)); // Write the lower bits to the upper bits of last byte

                dst[i + 1] = (byte)(
                    (dst[i + 1] & (255 << (bitCount - dstNextRemBits))) | // Mask out lower bits
                    (lastValue >> dstNextRemBits)); // Write the upper bits to the lower bits of next byte
            }

            #endregion
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void WriteBitsAtDst(
            Span<byte> dst, int i, int value, int destinationBitOffset, int dstNextRemBits)
        {
            int lastValue = value & (255 >> destinationBitOffset);
            dst[i] |= (byte)(lastValue << destinationBitOffset);

            int nextValue = value & (255 << dstNextRemBits);
            dst[i + 1] |= (byte)(nextValue >> dstNextRemBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static int ReadBitsAtSrc(
            ReadOnlySpan<byte> byteSrc, int i, int sourceBitOffset, int srcNextRemBits)
        {
            int last = byteSrc[i] >> sourceBitOffset;
            int next = (byteSrc[i + 1] << srcNextRemBits) & 255;
            return last | next;
        }

        // old impl is left here as SSE reference code
        /*

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void CopyBits_old(
            ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount,
            Span<byte> destination, int destinationBitOffset)
        {
            int srcNextPart = sourceBitOffset % 8;
            int dstNextPart = destinationBitOffset % 8;

            var src = source.Slice(sourceBitOffset / 8);
            var dst = destination.Slice(destinationBitOffset / 8);

            int i = 0;
            int bitsLeft = bitCount;
            int byteCount = bitsLeft / 8;
            if (byteCount > 0)
            {
                if (srcNextPart == 0 &&
                    dstNextPart == 0)
                {
                    src.Slice(i, byteCount).CopyTo(dst);
                    i += byteCount;
                }
                else
                {
                    if (bitsLeft > dst.Length * 8 - dstNextPart)
                        throw new ArgumentException(
                            "The given offsets and count reach outside the destination buffer.");

                    if (bitsLeft > src.Length * 8 - srcNextPart)
                        throw new ArgumentException(
                            "The given offsets and count reach outside the source buffer.");

                    int srcLastPart = 8 - srcNextPart;
                    int dstLastPart = 8 - dstNextPart;
                    byte srcNextClearMask = (byte)(255 >> srcLastPart);
                    byte dstLastClearMask = (byte)(255 >> dstLastPart);
                    byte dstNextClearMask = (byte)(255 << dstNextPart);

                    //fixed (byte* srcPtr = src)
                    //fixed (byte* dstPtr = dst)
                    {
                        // TODO: fix SSE

                        #region SSE2
                        

                        if (Sse2.IsSupported)
                        {
                            byte bSrcNextPart = (byte)srcNextPart;
                            byte bDstLastPart = (byte)dstLastPart;
                            byte bDstNextPart = (byte)dstNextPart;

                            var vLastClearMask = Vector128.Create(lastClearMask);
                            var vNextClearMask = Vector128.Create(nextClearMask);
                            var castByteMask = Vector128.Create((short)byte.MaxValue);

                            for (; i <= byteCount - Vector128<byte>.Count; i += Vector128<byte>.Count)
                            {
                                var values = Sse2.LoadVector128(srcPtr + i);

                                // we have to widen to int16 as shifts don't operate on int8
                                var lowValues = Sse2.UnpackLow(values, Vector128<byte>.Zero).AsInt16();
                                var highValues = Sse2.UnpackHigh(values, Vector128<byte>.Zero).AsInt16();

                                // process the first values
                                {
                                    var last1 = Sse2.ShiftRightLogical(lowValues, bSrcNextPart);
                                    var last2 = Sse2.ShiftRightLogical(highValues, bSrcNextPart);
                                    last1 = Sse2.ShiftLeftLogical(last1, bDstNextPart);
                                    last2 = Sse2.ShiftLeftLogical(last2, bDstNextPart);

                                    // "cast" from int16 to int8 as we just left-shifted 
                                    last1 = Sse2.And(last1, castByteMask);
                                    last2 = Sse2.And(last2, castByteMask);

                                    // write to destination
                                    var lastPack = Sse2.PackUnsignedSaturate(last1, last2);
                                    var lastResult = Sse2.LoadVector128(dstPtr + i);
                                    lastResult = Sse2.And(lastResult, vLastClearMask); // clear before writing
                                    lastResult = Sse2.Or(lastResult, lastPack); // write last part
                                    Sse2.Store(dstPtr + i, lastResult);
                                }

                                // process the second values
                                {
                                    var next1 = Sse2.ShiftLeftLogical(lowValues, bSrcNextPart);
                                    var next2 = Sse2.ShiftLeftLogical(highValues, bSrcNextPart);

                                    // "cast" from int16 to int8 as we just left-shifted 
                                    next1 = Sse2.And(next1, castByteMask);
                                    next2 = Sse2.And(next2, castByteMask);
                                    next1 = Sse2.ShiftRightLogical(next1, bDstLastPart);
                                    next2 = Sse2.ShiftRightLogical(next2, bDstLastPart);

                                    // write to destination
                                    var nextPack = Sse2.PackUnsignedSaturate(next1, next2);
                                    var nextResult = Sse2.LoadVector128(dstPtr + i + 1);
                                    nextResult = Sse2.And(nextResult, vNextClearMask); // clear before writing
                                    nextResult = Sse2.Or(nextResult, nextPack); // write next part
                                    Sse2.Store(dstPtr + i + 1, nextResult);
                                }
                            }
                        }

                        #endregion

                        for (; i < byteCount; i++)
                        {
                            int value = src[i];

                            if (srcNextPart > 0)
                            {
                                // mask away bits left of relevant bits 
                                int next = src[i + 1] & srcNextClearMask;

                                // shift away bits right of relevant bits
                                value >>= srcNextPart;
                                value |= next << srcLastPart;
                            }

                            dst[i] &= dstLastClearMask; // clear before writing
                            dst[i] |= (byte)(value << dstNextPart); // write last part

                            if (dstNextPart > 0)
                            {
                                dst[i + 1] &= dstNextClearMask; // clear before writing
                                dst[i + 1] |= (byte)(value >> dstLastPart); // write next part
                            }
                        }
                    }
                }
                bitsLeft -= byteCount * 8;
            }

            if (bitsLeft > 0)
            {
                int value = src[i] >> srcNextPart;

                // Mask out all the bits we dont want
                value &= 255 >> (8 - bitsLeft);

                int bitsFree = 8 - dstNextPart;
                int lastBits = bitsFree - bitsLeft;

                // Check if everything fits in the last byte
                if (lastBits >= 0)
                {
                    int mask = (255 >> bitsFree) | (255 << (8 - lastBits));

                    dst[i] = (byte)(
                        (dst[i] & mask) | // Mask out lower and upper bits
                        (value << dstNextPart)); // Insert new bits
                }
                else
                {
                    dst[i] = (byte)(
                        (dst[i] & (255 >> bitsFree)) | // Mask out upper bits
                        (value << dstNextPart)); // Write the lower bits to the upper bits in the first byte

                    dst[i + 1] = (byte)(
                        (dst[i + 1] & (255 << (bitsLeft - bitsFree))) | // Mask out lower bits
                        (value >> bitsFree)); // Write the upper bits to the lower bits of the second byte
                }
            }
        }

        */

        #endregion

        #region ReadByte[Unchecked]

        /// <summary>
        /// Read 1 to 8 bits from a buffer into a byte without validating offsets.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static byte ReadByteUnchecked(ReadOnlySpan<byte> source, int bitOffset, int bitCount)
        {
            int byteOffset = bitOffset / 8;
            int firstByteLength = bitOffset % 8;

            if (bitCount == 8 && firstByteLength == 0)
                return source[byteOffset];

            // mask away unused bits lower than (right of) relevant bits in first byte
            byte first = (byte)(source[byteOffset] >> firstByteLength);

            int bitsInSecondByte = bitCount - (8 - firstByteLength);
            if (bitsInSecondByte < 1)
            {
                // we don't need to read from the second byte, but we DO need
                // to mask away unused bits higher than (left of) relevant bits
                return (byte)(first & (255 >> (8 - bitCount)));
            }

            byte second = source[byteOffset + 1];

            // mask away unused bits higher than (left of) relevant bits in second byte
            second &= (byte)(255 >> (8 - bitsInSecondByte));

            return (byte)(first | (byte)(second << (bitCount - bitsInSecondByte)));
        }

        /// <summary>
        /// Read 1 to 8 bits from a buffer into a byte.
        /// </summary>
        public static byte ReadByte(ReadOnlySpan<byte> source, int bitOffset, int bitCount)
        {
            if (bitCount == 0) return 0;
            if (bitCount < 1) throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > 8) throw new ArgumentOutOfRangeException(nameof(bitCount));

            return ReadByteUnchecked(source, bitOffset, bitCount);
        }

        #endregion

        #region WriteByte[Unchecked]

        /// <summary>
        /// Writes 1 to 8 bits of data to a buffer without validating offsets.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void WriteByteUnchecked(
            int source, int bitCount, Span<byte> destination, int destinationBitOffset)
        {
            // Mask out all the bits we dont want
            source &= 255 >> (8 - bitCount);

            int p = destinationBitOffset / 8;
            int bitsUsed = destinationBitOffset % 8;
            int bitsFree = 8 - bitsUsed;
            int bitsLeft = bitsFree - bitCount;

            // Fast path, everything fits in the first byte
            if (bitsLeft >= 0)
            {
                int mask = (255 >> bitsFree) | (255 << (8 - bitsLeft));

                destination[p] = (byte)(
                    (destination[p] & mask) | // Mask out lower and upper bits
                    (source << bitsUsed)); // Insert new bits

                return;
            }

            destination[p] = (byte)(
                (destination[p] & (255 >> bitsFree)) | // Mask out upper bits
                (source << bitsUsed)); // Write the lower bits to the upper bits in the first byte

            p += 1;

            destination[p] = (byte)(
                (destination[p] & (255 << (bitCount - bitsFree))) | // Mask out lower bits
                (source >> bitsFree)); // Write the upper bits to the lower bits of the second byte
        }

        /// <summary>
        /// Writes a byte of data to a buffer.
        /// </summary>
        public static void WriteByte(
            byte source, Span<byte> destination, int destinationBitOffset)
        {
            WriteByteUnchecked(source, 8, destination, destinationBitOffset);
        }

        /// <summary>
        /// Writes 1 to 8 bits of data to a buffer.
        /// </summary>
        public static void WriteByte(
            byte source, int bitCount, Span<byte> destination, int destinationBitOffset)
        {
            if (bitCount == 0) return;
            if (bitCount < 1) throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > 8) throw new ArgumentOutOfRangeException(nameof(bitCount));

            WriteByteUnchecked(source, bitCount, destination, destinationBitOffset);
        }

        #endregion

        /// <summary>
        /// Write Base128 encoded variable sized <see cref="uint"/>.
        /// </summary>
        /// <returns>Number of bytes written.</returns>
        [CLSCompliant(false)]
        public static int WriteVarUInt32(uint value, Span<byte> destination)
        {
            int offset = 0;
            uint num1 = value;
            while (num1 >= 0x80)
            {
                destination[offset] = (byte)(num1 | 0x80);
                num1 >>= 7;
                offset++;
            }
            destination[offset] = (byte)num1;
            return offset + 1;
        }

        /// <summary>
        /// Reads a <see cref="uint"/> written using <see cref="WriteVarUInt32"/>.
        /// </summary>
        [CLSCompliant(false)]
        public static uint ReadVarUInt32(ReadOnlySpan<byte> buffer, out int bytesRead)
        {
            int num1 = 0;
            int num2 = 0;
            int offset = 0;
            while (true)
            {
                LidgrenException.Assert(num2 != 0x23, "Bad 7-bit encoded integer");

                byte num3 = buffer[offset++];
                num1 |= (num3 & 0x7f) << (num2 & 0x1f);
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    bytesRead = offset;
                    return (uint)num1;
                }
            }
        }
    }
}
