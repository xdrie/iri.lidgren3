using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lidgren.Network
{
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
