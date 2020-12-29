using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lidgren.Network
{
    public static class BitBufferExtensions
    {
        /// <summary>
        /// Ensures that the buffer can hold this number of bytes.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(this IBitBuffer buffer, int byteCount)
        {
            buffer.EnsureBitCapacity(byteCount * 8);
        }

        /// <summary>
        /// Ensures the buffer can hold it's current bits and the given amount.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureEnoughBitCapacity(this IBitBuffer buffer, int bitCount)
        {
            buffer.EnsureBitCapacity(buffer.BitLength + bitCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureEnoughBitCapacity(this IBitBuffer buffer, int bitCount, int maxBitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            buffer.EnsureEnoughBitCapacity(bitCount);
        }

        /// <summary>
        /// Gets whether <see cref="IBitBuffer.BitPosition"/> is byte-aligned, containing no stray bits.
        /// </summary>
        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsByteAligned(this IBitBuffer buffer)
        {
            return buffer.BitPosition % 8 == 0;
        }

        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasEnough(this IBitBuffer buffer, int bitCount)
        {
            return buffer.BitLength - buffer.BitPosition >= bitCount;
        }

        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementBitPosition(this IBitBuffer buffer, int bitCount)
        {
            buffer.BitPosition += bitCount;
            buffer.SetLengthByPosition();
        }

        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLengthByPosition(this IBitBuffer buffer)
        {
            if (buffer.BitLength < buffer.BitPosition)
                buffer.BitLength = buffer.BitPosition;
        }
    }
}
