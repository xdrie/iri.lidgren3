using System;
using System.Buffers.Binary;

namespace Lidgren.Network
{
    /// <summary>
    /// Base for a block based encryption class. This class is not thread-safe.
    /// </summary>
    public abstract class NetBlockEncryptionBase : NetEncryption
    {
        private byte[] _buffer;

        /// <summary>
        /// Block size in bytes for this cipher
        /// </summary>
        public abstract int BlockSize { get; }

        public NetBlockEncryptionBase(NetPeer peer) : base(peer)
        {
            _buffer = new byte[BlockSize];
        }

        /// <summary>
        /// Encrypt a block of bytes.
        /// </summary>
        protected abstract void EncryptBlock(ReadOnlySpan<byte> source, Span<byte> destination);

        /// <summary>
        /// Decrypt a block of bytes.
        /// </summary>
        protected abstract void DecryptBlock(ReadOnlySpan<byte> source, Span<byte> destination);

        /// <summary>
        /// Encrypt am outgoing message with this algorithm;
        /// no writing can be done to the message after encryption, 
        /// or message will be corrupted.
        /// </summary>
        public override bool Encrypt(NetOutgoingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            int payloadBitLength = message.BitLength;
            int numBytes = message.ByteLength;
            int blockSize = BlockSize;
            int numBlocks = (int)Math.Ceiling(numBytes / (double)blockSize);
            int dstSize = numBlocks * blockSize;

            message.EnsureCapacity((dstSize + 4) * 8); // add 4 bytes for payload length
            message.BitPosition = 0;

            var buffer = _buffer.AsSpan();
            var messageBuffer = message.Data.AsSpan();
            for (int i = 0; i < numBlocks; i++)
            {
                var messageSlice = messageBuffer.Slice(i * blockSize);
                EncryptBlock(messageSlice, buffer);
                buffer.CopyTo(messageSlice);
            }
            message.ByteLength = dstSize;
            message.BitPosition = message.BitLength;

            // add true payload length last
            message.Write((uint)payloadBitLength);

            return true;
        }

        /// <summary>
        /// Decrypt an incoming message encrypted with corresponding Encrypt.
        /// </summary>
        /// <param name="message">message to decrypt</param>
        /// <returns>true if successful; false if failed</returns>
        public override bool Decrypt(NetIncomingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            int numEncryptedBytes = message.ByteLength - 4; // last 4 bytes is true bit length
            int blockSize = BlockSize;
            int numBlocks = numEncryptedBytes / blockSize;
            if (numBlocks * blockSize != numEncryptedBytes)
                return false;

            var buffer = _buffer.AsSpan();
            var messageBuffer = message.Data.AsSpan();
            for (int i = 0; i < numBlocks; i++)
            {
                var messageSlice = messageBuffer.Slice(i * blockSize);
                DecryptBlock(messageSlice, buffer);
                buffer.CopyTo(messageSlice);
            }

            uint realSize = BinaryPrimitives.ReadUInt32LittleEndian(messageBuffer.Slice(numEncryptedBytes));
            message.BitLength = (int)realSize;

            return true;
        }
    }
}
