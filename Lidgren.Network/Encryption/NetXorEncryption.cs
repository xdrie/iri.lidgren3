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

        public override bool SupportsIV => false;

        public NetXorEncryption(NetPeer peer) : base(peer)
        {
        }

        public override void SetKey(byte[] data)
        {
            _key = (byte[])data.Clone();
        }

        public override void SetIV(byte[] iv)
        {
            throw new NotSupportedException();
        }

        public override bool Encrypt(NetOutgoingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var data = message.GetBuffer();
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

            var data = message.GetBuffer();
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
