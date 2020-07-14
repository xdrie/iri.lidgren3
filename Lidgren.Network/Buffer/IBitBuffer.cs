using System;

namespace Lidgren.Network
{
    public interface IBitBuffer : IDisposable
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

        /// <summary>
        /// Ensures that the buffer can hold this number of bits.
        /// </summary>
        void EnsureBitCapacity(int bitCount);
    }
}
