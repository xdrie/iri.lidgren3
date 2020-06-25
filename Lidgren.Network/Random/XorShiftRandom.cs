using System;

namespace Lidgren.Network
{
    /// <summary>
    /// Xor Shift based random.
    /// </summary>
    public sealed class XorShiftRandom : NetRandom
    {
        /// <summary>
        /// Get global instance of <see cref="XorShiftRandom"/>.
        /// </summary>
        public static new XorShiftRandom Global { get; } = new XorShiftRandom();

        //private const uint BaseX = 123456789;
        private const uint BaseY = 362436069;
        private const uint BaseZ = 521288629;
        private const uint BaseW = 88675123;

        private uint _x;
        private uint _y;
        private uint _z;
        private uint _w;

        /// <summary>
        /// Constructor with randomized seed.
        /// </summary>
        public XorShiftRandom()
        {
            Initialize(NetRandomSeed.GetUInt64());
        }

        /// <summary>
        /// Constructor with provided 64-bit seed.
        /// </summary>
        [CLSCompliant(false)]
        public XorShiftRandom(ulong seed)
        {
            Initialize(seed);
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public override void Initialize(int seed)
        {
            _x = (uint)seed;
            _y = BaseY;
            _z = BaseZ;
            _w = BaseW;
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public void Initialize(ulong seed)
        {
            _x = (uint)seed;
            _y = BaseY;
            _z = (uint)(seed << 32);
            _w = BaseW;
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public override uint NextUInt32()
        {
            uint t = _x ^ (_x << 11);
            _x = _y; _y = _z; _z = _w;
            return _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
        }
    }
}
