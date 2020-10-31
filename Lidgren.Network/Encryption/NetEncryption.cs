using System;

namespace Lidgren.Network
{
    /// <summary>
    /// Base class for an encryption algorithm.
    /// </summary>
    public abstract class NetEncryption : IDisposable
    {
        public NetPeer Peer { get; private set; }

        public bool IsDisposed { get; private set; }

        public abstract bool SupportsIV { get; }

        /// <summary>
        /// Constructs the base encryption object.
        /// </summary>
        public NetEncryption(NetPeer peer)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
        }

        public abstract void SetKey(byte[] key);

        public abstract void SetIV(byte[] iv);

        /// <summary>
        /// Encrypt an outgoing message in place.
        /// </summary>
        public abstract bool Encrypt(NetOutgoingMessage message);

        /// <summary>
        /// Decrypt an incoming message in place.
        /// </summary>
        public abstract bool Decrypt(NetIncomingMessage message);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
        }

        ~NetEncryption()
        {
            Dispose(false);
        }
    }
}
