using System;
using System.Runtime.CompilerServices;

namespace Lidgren.Network
{
    public interface IBitBuffer
    {
        int BitCapacity { get; set; }

        int ByteCapacity { get; set; }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bits.
        /// </summary>
        int BitLength { get; set; }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bytes.
        /// </summary>
        int ByteLength { get; set; }

        /// <summary>
        /// Gets or sets the position within the buffer in bits.
        /// </summary>
        int BitPosition { get; set; }

        /// <summary>
        /// Gets or sets the position within the buffer in bytes.
        /// </summary>
        int BytePosition { get; set; }

        Span<byte> Span { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IncrementBitPosition(int bitCount)
        {
            BitPosition += bitCount;
            this.SetLengthByPosition();
        }

        /// <summary>
        /// Ensures that the buffer can hold this number of bits.
        /// </summary>
        void EnsureBitCapacity(int bitCount);

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        void Write(ReadOnlySpan<byte> source, int sourceBitOffset, int bitCount)
        {
            if (source.IsEmpty)
                return;

            this.EnsureEnoughBitCapacity(bitCount);
            NetBitWriter.CopyBits(source, sourceBitOffset, bitCount, Span, BitPosition);
            IncrementBitPosition(bitCount);
        }

        /// <summary>
        /// Writes a certain amount of bits from a span.
        /// </summary>
        void Write(ReadOnlySpan<byte> source, int bitCount)
        {
            Write(source, 0, bitCount);
        }

        /// <summary>
        /// Writes bytes from a span.
        /// </summary>
        void Write(ReadOnlySpan<byte> source)
        {
            if (!this.IsByteAligned())
            {
                Write(source, source.Length * 8);
                return;
            }

            this.EnsureEnoughBitCapacity(source.Length * 8);
            source.CopyTo(Span.Slice(BytePosition));
            IncrementBitPosition(source.Length * 8);
        }
    }
}
