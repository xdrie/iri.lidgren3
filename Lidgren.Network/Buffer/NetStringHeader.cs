using System;

namespace Lidgren.Network
{
    // TODO: possibly add Encoding property so we are not fixed to one encoding

    public readonly struct NetStringHeader : IEquatable<NetStringHeader>
    {
        public static NetStringHeader Empty => default;

        public int CharCount { get; }
        public int? ByteCount { get; }

        public int MaxByteCount => NetBuffer.StringEncoding.GetMaxByteCount(CharCount);
        public int MaxByteCountVarSize => NetBitWriter.GetVarIntSize((uint)MaxByteCount);
        public int CharCountVarSize => NetBitWriter.GetVarIntSize((uint)CharCount);
        public int ByteCountVarSize => NetBitWriter.GetVarIntSize((uint)(ByteCount ?? MaxByteCount));

        /// <summary>
        /// Gets the size of the header in bytes. 
        ///  <see cref="MaxByteCount"/> is used if <see cref="ByteCount"/> is <see langword="null"/>.
        /// </summary>
        public int Size => CharCountVarSize + ByteCountVarSize;

        public NetStringHeader(int charLength, int? byteLength)
        {
            if (charLength < 0)
                throw new ArgumentOutOfRangeException(nameof(charLength));
            if (byteLength < 0)
                throw new ArgumentOutOfRangeException(nameof(byteLength));

            CharCount = charLength;
            ByteCount = byteLength;
        }

        [CLSCompliant(false)]
        public NetStringHeader(uint charLength, uint? byteLength)
        {
            if (charLength > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(charLength));
            if (byteLength > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(byteLength));

            CharCount = (int)charLength;
            ByteCount = (int?)byteLength;
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
