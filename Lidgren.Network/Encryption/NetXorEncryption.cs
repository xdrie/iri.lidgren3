using System;

namespace Lidgren.Network
{
    /// <summary>
    /// Example class of XOR encryption. Not suitable for use due to weakness.
    /// </summary>
    [Obsolete("XOR encryption is very weak and should not be used.")]
    public sealed class NetXorEncryption : NetEncryption
    {
        private byte[] _key = Array.Empty<byte>();

        public NetXorEncryption(NetPeer peer, ReadOnlySpan<byte> key) : base(peer)
        {
            SetKey(key);
        }

        public NetXorEncryption(NetPeer peer, ReadOnlySpan<char> key) : base(peer)
        {
            SetKey(key);
        }

        public override void SetKey(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                throw new ArgumentException("Span may not be empty.", nameof(data));

            _key = data.ToArray();
        }

        public override bool Encrypt(NetOutgoingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var data = message.Data;
            var slice = data.AsSpan(0, message.ByteLength);
            for (int i = 0; i < slice.Length; i++)
            {
                int offset = i % _key.Length;
                slice[i] = (byte)(slice[i] ^ _key[offset]);
            }
            return true;
        }

        public override bool Decrypt(NetIncomingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var data = message.Data;
            var slice = data.AsSpan(0, message.ByteLength);
            for (int i = 0; i < slice.Length; i++)
            {
                int offset = i % _key.Length;
                slice[i] = (byte)(slice[i] ^ _key[offset]);
            }
            return true;
        }
    }
}
