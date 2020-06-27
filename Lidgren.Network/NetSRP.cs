using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Lidgren.Network
{
    /// <summary>
    /// Helper methods for implementing SRP authentication.
    /// </summary>
    public static class NetSRP
    {
        private static readonly BigInteger N = new BigInteger(
            new byte[]
            {
                227, 236, 162, 178, 181, 125, 248, 101, 201, 86, 237, 231, 169, 232, 169, 165,
                119, 92, 115, 54, 180, 128, 242, 44, 105, 69, 224, 224, 146, 182, 184, 21, 1,
            },
            isUnsigned: true);

        private static readonly BigInteger g = 2;
        private static readonly BigInteger k = ComputeMultiplier();

        private static HashAlgorithm GetHashAlgorithm()
        {
            return SHA256.Create();
        }

        /// <summary>
        /// Compute multiplier (k)
        /// </summary>
        private static BigInteger ComputeMultiplier()
        {
            string one = NetUtility.ToHexString(N.ToByteArray(isUnsigned: true));
            string two = NetUtility.ToHexString(g.ToByteArray(isUnsigned: true));

            string ccstr = one + two.PadLeft(one.Length, '0');
            byte[] cc = NetUtility.FromHexString(ccstr);

            using var algorithm = GetHashAlgorithm();
            var ccHashed = algorithm.ComputeHash(cc);

            Span<char> tmp = stackalloc char[1 + NetUtility.GetHexCharCount(ccHashed.Length)];
            tmp[0] = '0'; // ensure that the value is unsigned
            NetUtility.ToHexString(ccHashed, tmp.Slice(1));

            return BigInteger.Parse(tmp, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Create 16 bytes of random salt.
        /// </summary>
        public static byte[] CreateRandomSalt()
        {
            var value = new byte[16];
            CryptoRandom.Global.NextBytes(value);
            return value;
        }

        /// <summary>
        /// Create a random ephemeral value.
        /// </summary>
        public static BigInteger CreateRandomEphemeral()
        {
            Span<byte> value = stackalloc byte[32];
            CryptoRandom.Global.NextBytes(value);
            return new BigInteger(value, isUnsigned: true);
        }

        /// <summary>
        /// Computer private key (x)
        /// </summary>
        public static BigInteger ComputePrivateKey(string username, string password, ReadOnlySpan<byte> salt)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(username + ":" + password);

            using var algorithm = GetHashAlgorithm();
            Span<byte> innerHash = stackalloc byte[NetBitWriter.ByteCountForBits(algorithm.HashSize)];
            if (!algorithm.TryComputeHash(tmp, innerHash, out _))
                throw new Exception();

            // x   ie. H(salt || H(username || ":" || password))
            int totalLength = innerHash.Length + salt.Length;
            var total = totalLength < 4096 ? stackalloc byte[totalLength] : new byte[totalLength];
            salt.CopyTo(total);
            innerHash.CopyTo(total.Slice(salt.Length));

            Span<byte> totalHash = stackalloc byte[NetBitWriter.ByteCountForBits(algorithm.HashSize)];
            if (!algorithm.TryComputeHash(total, totalHash, out _))
                throw new Exception();

            return new BigInteger(totalHash, isUnsigned: true);
        }

        /// <summary>
        /// Creates a verifier that the server can later use to authenticate users later on (v)
        /// </summary>
        public static BigInteger ComputeServerVerifier(BigInteger privateKey)
        {
            // Verifier (v) = g^x (mod N)
            var x = privateKey;
            return BigInteger.ModPow(g, x, N);
        }

        /// <summary>
        /// Compute client public ephemeral value (A)
        /// </summary>
        public static BigInteger ComputeClientEphemeral(BigInteger clientPrivateEphemeral) // a
        {
            // A = g^a (mod N) 
            var a = clientPrivateEphemeral;
            return BigInteger.ModPow(g, a, N);
        }

        /// <summary>
        /// Compute server ephemeral value (B)
        /// </summary>
        public static BigInteger ComputeServerEphemeral(
            BigInteger serverPrivateEphemeral,
            BigInteger verifier)
        {
            var b = serverPrivateEphemeral;
            var v = verifier;

            // B = kv + g^b (mod N) 
            var bb = BigInteger.ModPow(g, b, N);
            var kv = v * k;
            var B = (kv + bb) % N;

            return B;
        }

        /// <summary>
        /// Compute intermediate value (u)
        /// </summary>
        public static BigInteger ComputeU(
            BigInteger clientPublicEphemeral,
            BigInteger serverPublicEphemeral)
        {
            // u = SHA-1(A || B)

            int byteCount = clientPublicEphemeral.GetByteCount() + serverPublicEphemeral.GetByteCount();
            var buffer = byteCount < 4096 ? stackalloc byte[byteCount] : new byte[byteCount];

            if (!clientPublicEphemeral.TryWriteBytes(buffer, out int clientBytesWritten, isUnsigned: true) ||
                !serverPublicEphemeral.TryWriteBytes(buffer.Slice(clientBytesWritten), out _, isUnsigned: true))
                throw new Exception();

            using var algorithm = GetHashAlgorithm();
            Span<byte> hash = stackalloc byte[NetBitWriter.ByteCountForBits(algorithm.HashSize)];
            if (!algorithm.TryComputeHash(buffer, hash, out _))
                throw new Exception();
            
            return new BigInteger(hash, isUnsigned: true);
        }

        /// <summary>
        /// Computes the server session value
        /// </summary>
        public static BigInteger ComputeServerSessionValue(
            BigInteger clientPublicEphemeral,
            BigInteger verifier,
            BigInteger udata,
            BigInteger serverPrivateEphemeral)
        {
            var A = clientPublicEphemeral;
            var v = verifier;
            var u = udata;
            var b = serverPrivateEphemeral;

            // S = (Av^u) ^ b (mod N)
            return BigInteger.ModPow(BigInteger.ModPow(v, u, N) * A % N, b, N) % N;
        }

        /// <summary>
        /// Computes the client session value
        /// </summary>
        public static BigInteger ComputeClientSessionValue(
            BigInteger serverPublicEphemeral,
            BigInteger xdata,
            BigInteger udata,
            BigInteger clientPrivateEphemeral)
        {
            var B = serverPublicEphemeral;
            var x = xdata;
            var u = udata;
            var a = clientPrivateEphemeral;

            // (B - kg^x) ^ (a + ux)   (mod N)
            var bx = BigInteger.ModPow(g, x, N);
            var btmp = (B + N * k - (bx * k)) % N;
            return BigInteger.ModPow(btmp, x * u + a, N);
        }

        /// <summary>
        /// Create XTEA symmetrical encryption object from sessionValue
        /// </summary>
        public static NetXteaEncryption CreateEncryption(NetPeer peer, ReadOnlySpan<byte> sessionValue)
        {
            using var algorithm = GetHashAlgorithm();
            Span<byte> hash = stackalloc byte[NetBitWriter.ByteCountForBits(algorithm.HashSize)];
            if (!algorithm.TryComputeHash(sessionValue, hash, out _))
                throw new Exception();

            Span<byte> key = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
            {
                key[i] = hash[i];
                for (int j = 1; j < hash.Length / 16; j++)
                    key[i] ^= hash[i + (j * 16)];
            }

            return new NetXteaEncryption(peer, key);
        }

        /// <summary>
        /// Create XTEA symmetrical encryption object from sessionValue
        /// </summary>
        public static NetXteaEncryption CreateEncryption(NetPeer peer, BigInteger sessionValue)
        {
            return CreateEncryption(peer, sessionValue.ToByteArray());
        }
    }
}
