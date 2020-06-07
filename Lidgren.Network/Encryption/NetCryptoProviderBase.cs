using System;
using System.IO;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    public abstract class NetCryptoProviderBase : NetEncryption, IDisposable
    {
        // TODO: cache ICryptoTransform

        [CLSCompliant(false)]
        protected SymmetricAlgorithm _algorithm;

        public bool IsDisposed { get; private set; }

        public NetCryptoProviderBase(NetPeer peer, SymmetricAlgorithm algo) : base(peer)
        {
            _algorithm = algo;
            _algorithm.GenerateKey();
            _algorithm.GenerateIV();
        }

        public override void SetKey(byte[] data, int offset, int count)
        {
            int len = _algorithm.Key.Length;
            var key = new byte[len];
            for (int i = 0; i < len; i++)
                key[i] = data[offset + (i % count)];
            _algorithm.Key = key;

            len = _algorithm.IV.Length;
            key = new byte[len];
            for (int i = 0; i < len; i++)
                key[len - 1 - i] = data[offset + (i % count)];
            _algorithm.IV = key;
        }

        public override bool Encrypt(NetOutgoingMessage msg)
        {
            int unEncLenBits = msg.LengthBits;

            var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, _algorithm.CreateEncryptor(), CryptoStreamMode.Write, true))
                cs.Write(msg.m_data, 0, msg.LengthBytes);

            int length = (int)ms.Length;

            msg.EnsureBufferSize((length + 4) * 8);
            msg.LengthBits = 0; // reset write pointer
            msg.Write((uint)unEncLenBits);
            msg.Write(ms.GetBuffer().AsSpan(0, length));
            msg.LengthBits = (length + 4) * 8;

            return true;
        }

        public override bool Decrypt(NetIncomingMessage msg)
        {
            int unEncLenBits = (int)msg.ReadUInt32();
            int byteLen = NetUtility.BytesNeededToHoldBits(unEncLenBits);
            var result = m_peer.GetStorage(byteLen);

            var ms = new MemoryStream(msg.m_data, 4, msg.LengthBytes - 4);
            using (var cs = new CryptoStream(ms, _algorithm.CreateDecryptor(), CryptoStreamMode.Read))
                cs.Read(result, 0, byteLen);

            // TODO: recycle existing msg

            msg.m_data = result;
            msg.m_bitLength = unEncLenBits;
            msg.m_readPosition = 0;

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _algorithm.Dispose();
                    _algorithm = null;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NetCryptoProviderBase()
        {
            Dispose(false);
        }
    }
}