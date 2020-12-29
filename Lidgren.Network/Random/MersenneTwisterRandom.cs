using System;

namespace Lidgren.Network
{
    /// <summary>
    /// Mersenne Twister based random.
    /// </summary>
    public sealed class MersenneTwisterRandom : NetRandom
    {
        /// <summary>
        /// Get global instance of <see cref="MersenneTwisterRandom"/>.
        /// </summary>
        public static new MersenneTwisterRandom Global { get; } = new MersenneTwisterRandom();

        private const int N = 624;
        private const int M = 397;
        private const uint MATRIX_A = 0x9908b0dfU;
        private const uint UPPER_MASK = 0x80000000U;
        private const uint LOWER_MASK = 0x7fffffffU;
        private const uint TEMPER1 = 0x9d2c5680U;
        private const uint TEMPER2 = 0xefc60000U;
        private const int TEMPER3 = 11;
        private const int TEMPER4 = 7;
        private const int TEMPER5 = 15;
        private const int TEMPER6 = 18;

        private uint[] _mt = new uint[N];
        private uint[] _mag01 = new uint[] { 0x0U, MATRIX_A };
        private int _mti;

        /// <summary>
        /// Constructor with randomized seed.
        /// </summary>
        public MersenneTwisterRandom()
        {
            Initialize((int)NetRandomSeed.GetUInt32());
        }

        /// <summary>
        /// Constructor with provided 32-bit seed.
        /// </summary>
        [CLSCompliant(false)]
        public MersenneTwisterRandom(int seed)
        {
            Initialize(seed);
        }

        [CLSCompliant(false)]
        public override void Initialize(int seed)
        {
            _mti = N + 1;

            _mt[0] = (uint)seed;
            for (int i = 1; i < _mt.Length; i++)
                _mt[i] = (uint)(1812433253 * (_mt[i - 1] ^ (_mt[i - 1] >> 30)) + i);
        }

        [CLSCompliant(false)]
        public override uint NextUInt32()
        {
            uint y;
            if (_mti >= N)
            {
                GenRandAll();
                _mti = 0;
            }
            y = _mt[_mti++];
            y ^= y >> TEMPER3;
            y ^= (y << TEMPER4) & TEMPER1;
            y ^= (y << TEMPER5) & TEMPER2;
            y ^= y >> TEMPER6;
            return y;
        }

        private void GenRandAll()
        {
            int kk = 1;
            uint y;
            uint p;
            y = _mt[0] & UPPER_MASK;
            do
            {
                p = _mt[kk];
                _mt[kk - 1] = _mt[kk + (M - 1)] ^ ((y | (p & LOWER_MASK)) >> 1) ^ _mag01[p & 1];
                y = p & UPPER_MASK;
            } while (++kk < N - M + 1);
            do
            {
                p = _mt[kk];
                _mt[kk - 1] = _mt[kk + (M - N - 1)] ^ ((y | (p & LOWER_MASK)) >> 1) ^ _mag01[p & 1];
                y = p & UPPER_MASK;
            } while (++kk < N);
            p = _mt[0];
            _mt[N - 1] = _mt[M - 1] ^ ((y | (p & LOWER_MASK)) >> 1) ^ _mag01[p & 1];
        }
    }
}
