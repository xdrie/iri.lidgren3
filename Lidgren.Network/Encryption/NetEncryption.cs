using System;
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
    /// <summary>
    /// Base class for an encryption algorithm.
    /// </summary>
    public abstract class NetEncryption : IDisposable
    {
        public NetPeer Peer { get; private set; }

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Constructs the base encryption object.
        /// </summary>
        public NetEncryption(NetPeer peer)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
        }

        public void SetKey(ReadOnlySpan<char> data)
        {
            SetKey(MemoryMarshal.AsBytes(data));
        }

        public abstract void SetKey(ReadOnlySpan<byte> data);

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
