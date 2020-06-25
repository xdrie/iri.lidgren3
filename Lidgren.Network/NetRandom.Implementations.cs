using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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

    /// <summary>
    /// <see cref="RNGCryptoServiceProvider"/> based random; very slow but cryptographically safe.
    /// </summary>
    public class CryptoRandom : NetRandom, IDisposable
    {
        /// <summary>
        /// Global instance of <see cref="CryptoRandom"/>.
        /// </summary>
        public static new CryptoRandom Global { get; } = new CryptoRandom();

        private bool _isDisposed;
        private RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        /// <summary>
        /// Seeds in <see cref="CryptoRandom"/> do not create deterministic sequences.
        /// </summary>
        public override void Initialize(int seed)
        {
            Span<byte> tmp = stackalloc byte[(int)((uint)seed % 16)];
            _rng.GetBytes(tmp); // just prime it
        }

        [CLSCompliant(false)]
        public override uint NextUInt32()
        {
            Span<uint> tmp = stackalloc uint[1];
            NextBytes(MemoryMarshal.AsBytes(tmp));
            return tmp[0];
        }

        public override void NextBytes(Span<byte> buffer)
        {
            _rng.GetBytes(buffer);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _rng.Dispose();

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
