using System;
using System.IO;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    public abstract class NetCryptoProviderBase : NetEncryption
    {
        // TODO: cache ICryptoTransform
        // TODO: optimize by not creating a new MemoryStream for every call (possibly with span)


        [CLSCompliant(false)]
        protected SymmetricAlgorithm Algorithm { get; private set; }

        public NetCryptoProviderBase(NetPeer peer, SymmetricAlgorithm algorithm) : base(peer)
        {
            Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            Algorithm.GenerateKey();
            Algorithm.GenerateIV();
        }

        public override void SetKey(ReadOnlySpan<byte> data)
        {
            var key = new byte[Algorithm.Key.Length];
            for (int i = 0; i < key.Length; i++)
                key[i] = data[i % data.Length];
            Algorithm.Key = key;

            var iv = new byte[Algorithm.IV.Length];
            for (int i = 0; i < iv.Length; i++)
                iv[iv.Length - 1 - i] = data[i % data.Length];
            Algorithm.IV = iv;
        }

        public override bool Encrypt(NetOutgoingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            int unEncLenBits = message.BitLength;

            var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, Algorithm.CreateEncryptor(), CryptoStreamMode.Write, true))
                cs.Write(message.Span.Slice(0, message.ByteLength));

            int length = (int)ms.Length;

            message.BitPosition = 0;
            message.EnsureBitCapacity((length + 4) * 8);
            message.Write((uint)unEncLenBits);
            message.Write(ms.GetBuffer().AsSpan(0, length));
            message.ByteLength = length + 4;

            return true;
        }

        public override unsafe bool Decrypt(NetIncomingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            int unEncLenBits = (int)message.ReadUInt32();
            int byteLen = NetBitWriter.BytesForBits(unEncLenBits);
            var result = Peer.GetStorage(byteLen);

            fixed (byte* msgPtr = message.Span)
            {
                using var ms = new UnmanagedMemoryStream(msgPtr + 4, message.ByteLength - 4);
                using var cs = new CryptoStream(ms, Algorithm.CreateDecryptor(), CryptoStreamMode.Read);
                cs.Read(result, 0, byteLen);
            }

            // TODO: recycle existing msg

            message._data = result; // TODO: make api for this
            message.BitLength = unEncLenBits;
            message.BitPosition = 0;

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                    Algorithm.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}