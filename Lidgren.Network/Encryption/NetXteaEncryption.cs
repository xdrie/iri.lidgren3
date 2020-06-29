/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Buffers.Binary;

namespace Lidgren.Network
{
    /// <summary>
    /// Methods to encrypt and decrypt data using the XTEA algorithm.
    /// </summary>
    public sealed class NetXteaEncryption : NetBlockEncryptionBase
    {
        private const int KeySize = 16;
        //private const int c_delta = unchecked((int)0x9E3779B9);

        private readonly int _rounds;
        private readonly uint[] _sum0;
        private readonly uint[] _sum1;

        /// <summary>
        /// Gets the block size for this cipher.
        /// </summary>
        public override int BlockSize => 8;

        private NetXteaEncryption(NetPeer peer, int rounds) : base(peer)
        {
            _rounds = rounds;
            _sum0 = new uint[_rounds];
            _sum1 = new uint[_rounds];
        }

        public NetXteaEncryption(NetPeer peer, ReadOnlySpan<byte> key, int rounds = 32) : this(peer, rounds)
        {
            SetKey(key);
        }

        public NetXteaEncryption(NetPeer peer, ReadOnlySpan<char> key, int rounds = 32) : this(peer, rounds)
        {
            SetKey(key);
        }

        public override void SetKey(ReadOnlySpan<byte> data)
        {
            Span<byte> hash = stackalloc byte[NetBitWriter.ByteCountForBits(NetUtility.Sha256.HashSize)];
            var key = data.Length > KeySize ? hash : data;

            if (data.Length > KeySize)
                if (!NetUtility.Sha256.TryComputeHash(data, hash, out _))
                    throw new Exception();

            Span<uint> tmp = stackalloc uint[8];
            int i = 0;
            int j = 0;
            while (i < 4)
            {
                tmp[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(j));
                i++;
                j += 4;
            }
            for (i = j = 0; i < 32; i++)
            {
                _sum0[i] = ((uint)j) + tmp[j & 3];
                j += -1640531527;
                _sum1[i] = ((uint)j) + tmp[(j >> 11) & 3];
            }
        }

        protected override void EncryptBlock(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            uint v0 = BinaryPrimitives.ReadUInt32LittleEndian(source);
            uint v1 = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4));

            for (int i = 0; i < _rounds; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ _sum0[i];
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ _sum1[i];
            }

            BinaryPrimitives.WriteUInt32LittleEndian(destination, v0);
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(4), v1);
        }

        protected override void DecryptBlock(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            uint v0 = BinaryPrimitives.ReadUInt32LittleEndian(source);
            uint v1 = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4));

            for (int i = _rounds; i-- > 0;)
            {
                v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ _sum1[i];
                v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ _sum0[i];
            }

            BinaryPrimitives.WriteUInt32LittleEndian(destination, v0);
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(4), v1);
        }
    }
}