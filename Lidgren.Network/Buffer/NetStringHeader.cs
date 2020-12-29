using System;

namespace Lidgren.Network
{
    // TODO: possibly add Encoding property so we are not fixed to one encoding

    public readonly struct NetStringHeader : IEquatable<NetStringHeader>
    {
        public static NetStringHeader Empty => default;

        public int CharCount { get; }
        public int? ByteCount { get; }

        /// <summary>
        /// <see cref="MaxByteCount"/> if <see cref="ByteCount"/> is <see langword="null"/>,
        /// otherwise <see cref="ByteCount"/>.
        /// </summary>
        public int ExpectedByteCount => ByteCount ?? MaxByteCount;

        public int MaxByteCount => NetBuffer.StringEncoding.GetMaxByteCount(CharCount);
        public int MaxByteCountVarSize => NetBitWriter.GetVarIntSize((uint)MaxByteCount);
        public int CharCountVarSize => NetBitWriter.GetVarIntSize((uint)CharCount);
        public int ExpectedByteCountVarSize => NetBitWriter.GetVarIntSize((uint)ExpectedByteCount);

        /// <summary>
        /// Gets the size of the header in bytes. 
        /// </summary>
        public int MinimumHeaderSize => CharCountVarSize + ExpectedByteCountVarSize;

        public NetStringHeader(int charCount, int? byteCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount));
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            CharCount = charCount;
            ByteCount = byteCount;
        }

        [CLSCompliant(false)]
        public NetStringHeader(uint charCount, uint? byteCount)
        {
            if (charCount > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(charCount));
            if (byteCount > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            CharCount = (int)charCount;
            ByteCount = (int?)byteCount;
        }

        public bool Equals(NetStringHeader other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is NetStringHeader other && this == other;
        }

        public static bool operator ==(NetStringHeader left, NetStringHeader right)
        {
            return (left.CharCount, left.ByteCount) == (right.CharCount, right.ByteCount);
        }

        public static bool operator !=(NetStringHeader left, NetStringHeader right)
        {
            return (left.CharCount, left.ByteCount) != (right.CharCount, right.ByteCount);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CharCount, ByteCount);
        }
    }
}
