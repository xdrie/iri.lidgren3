using System;
using System.IO;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    public abstract class NetCryptoProviderBase : NetEncryption, IDisposable
    {
        protected SymmetricAlgorithm m_algorithm;
        
        [ThreadStatic]
        private ICryptoTransform __encryptor;
        protected ICryptoTransform Encryptor
        {
            get
            {
                if (__encryptor == null)
                {
                    __encryptor = m_algorithm.CreateEncryptor();
                }
                else if (__encryptor.IsReusable() == false)
                {
                    __encryptor.Dispose();
                    __encryptor = m_algorithm.CreateEncryptor();
                }
                return __encryptor;
            }
        }

        [ThreadStatic]
        private ICryptoTransform __decryptor;
        protected ICryptoTransform Decryptor
        {
            get
            {
                if (__decryptor == null)
                {
                    __decryptor = m_algorithm.CreateDecryptor();
                }
                else if (__decryptor.IsReusable() == false)
                {
                    __decryptor.Dispose();
                    __decryptor = m_algorithm.CreateDecryptor();
                }
                return __decryptor;
            }
        }

        public bool IsDisposed { get; private set; }

        public NetCryptoProviderBase(NetPeer peer, SymmetricAlgorithm algo) : base(peer)
        {
            m_algorithm = algo;
            m_algorithm.GenerateKey();
            m_algorithm.GenerateIV();
        }

        public override void SetKey(byte[] data, int offset, int count)
        {
            int len = m_algorithm.Key.Length;
            byte[] key = new byte[len];
            for (int i = 0; i < len; i++)
                key[i] = data[offset + (i % count)];
            m_algorithm.Key = key;

            len = m_algorithm.IV.Length;
            key = new byte[len];
            for (int i = 0; i < len; i++)
                key[len - 1 - i] = data[offset + (i % count)];
            m_algorithm.IV = key;
        }

        public override bool Encrypt(NetOutgoingMessage msg)
        {
            try
            {
                int sourceBits = msg.LengthBits;

#if !(ANDROID || IOS)
                using (var ms = m_peer.GetRecyclableMemory())
                {
                    using (var cs = new CryptoStream(ms, Encryptor, CryptoStreamMode.Write, true))
                        cs.Write(msg.m_data, 0, msg.LengthBytes);

                    int length = (int)ms.Length;
                    int neededBufferBits = (length + 4) * 8;

                    msg.EnsureBufferSize(neededBufferBits);
                    msg.LengthBits = 0; // reset write pointer
                    msg.Write((uint)sourceBits);
                    msg.Write(ms.GetBuffer(), 0, length);
                    msg.LengthBits = neededBufferBits;
                }
#else
                var ms = new MemoryStream();
                var cs = new CryptoStream(ms, m_algorithm.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(msg.m_data, 0, msg.LengthBytes);
                cs.Close();
                
                var result = ms.ToArray();
                ms.Close();

                msg.EnsureBufferSize((result.Length + 4) * 8);
                msg.LengthBits = 0; // reset write pointer
                msg.Write((uint)sourceBits);
                msg.Write(result);
                msg.LengthBits = (result.Length + 4) * 8;
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Decrypt(NetIncomingMessage msg)
        {
            void Recycle(byte[] buffer)
            {
                if (m_peer.m_configuration.UseMessageRecycling)
                    m_peer.Recycle(buffer);
            }

            bool success = true;
            int decryptedBits = 0;
            byte[] result = null;
            byte[] originalMsgBuffer = msg.m_data;

            try
            {
                decryptedBits = (int)msg.ReadUInt32();
                using (var ms = new MemoryStream(originalMsgBuffer, 4, msg.LengthBytes - 4))
                using (var cs = new CryptoStream(ms, Decryptor, CryptoStreamMode.Read))
                {
                    int decryptedLength = NetUtility.BytesNeededToHoldBits(decryptedBits);
                    result = m_peer.GetStorage(decryptedLength);

                    if (cs.Read(result, 0, decryptedLength) != decryptedLength)
                        success = false;
                }
            }
            catch
            {
                success = false;
            }

            Recycle(originalMsgBuffer);
            if (success == false)
            {
                Recycle(result);
                result = null;
                decryptedBits = 0;
            }

            msg.m_data = result;
            msg.m_bitLength = decryptedBits;
            msg.m_readPosition = 0;
            return success;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed == false)
            {
                if (disposing)
                {
                    __encryptor.Dispose();
                    __decryptor.Dispose();
                    m_algorithm.Dispose();

                    __encryptor = null;
                    __decryptor = null;
                    m_algorithm = null;
                }
                IsDisposed = true;
            }
        }

        ~NetCryptoProviderBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
