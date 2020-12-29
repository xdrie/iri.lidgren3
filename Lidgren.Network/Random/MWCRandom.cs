using System;

namespace Lidgren.Network
{
    /// <summary>
    /// Multiply With Carry based random.
    /// </summary>
    public class MWCRandom : NetRandom
    {
        /// <summary>
        /// Get global instance of <see cref="MWCRandom"/>.
        /// </summary>
        public static new MWCRandom Global { get; } = new MWCRandom();

        private uint _w;
        private uint _z;

        /// <summary>
        /// Constructor with randomized seed.
        /// </summary>
        public MWCRandom()
        {
            Initialize(NetRandomSeed.GetUInt64());
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public override void Initialize(int seed)
        {
            _w = (uint)seed;
            _z = _w * 16777619;
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public void Initialize(ulong seed)
        {
            _w = (uint)seed;
            _z = (uint)(seed >> 32);
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public override uint NextUInt32()
        {
            _z = 36969 * (_z & 65535) + (_z >> 16);
            _w = 18000 * (_w & 65535) + (_w >> 16);
            return (_z << 16) + _w;
        }
    }
}
