using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    [Obsolete("DES encryption is very weak and should not be used.")]
    public sealed class NetDESEncryption : NetCryptoProviderBase
    {
        [SuppressMessage("Security", "CA5351", Justification = "Encryption is obsoleted.")]
        public NetDESEncryption(NetPeer peer) : base(peer, DES.Create())
        {
        }

        public NetDESEncryption(NetPeer peer, ReadOnlySpan<byte> key) : this(peer)
        {
            SetKey(key);
        }

        public NetDESEncryption(NetPeer peer, ReadOnlySpan<char> key) : this(peer)
        {
            SetKey(key);
        }
    }
}
