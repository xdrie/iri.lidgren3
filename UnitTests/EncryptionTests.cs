using System;
using System.Linq;
using System.Threading.Tasks;
using Lidgren.Network;

namespace UnitTests
{
    public static class EncryptionTests
    {
        public static void Run(NetPeer peer)
        {
            //
            // Test encryption
            //

            Console.WriteLine("Testing encryption:");
            TestEncryption(new NetXorEncryption(peer, "TopSecret"));
            TestEncryption(new NetXteaEncryption(peer, "TopSecret"));
            TestEncryption(new NetAESEncryption(peer, "TopSecret"));
            TestEncryption(new NetRC2Encryption(peer, "TopSecret"));
            TestEncryption(new NetDESEncryption(peer, "TopSecret"));
            TestEncryption(new NetTripleDESEncryption(peer, "TopSecret"));

            var srpXteas = TestSRPWithRandomness(peer);
            Console.WriteLine("Testing SRP-based Xtea encryptions...");
            foreach (var algo in srpXteas)
                TestEncryption(algo, false);

            Console.WriteLine("Message encryption OK");
        }

        public static void TestEncryption(NetEncryption algo, bool printName = true)
        {
            NetOutgoingMessage om = algo.Peer.CreateMessage();
            om.Write("Hallon");
            om.Write(42);
            om.Write(5, 5);
            om.Write(true);
            om.Write("kokos");

            int unencLen = om.BitLength;
            if(!om.Encrypt(algo))
                throw new LidgrenException("failed to encrypt");

            // convert to incoming message
            NetIncomingMessage im = Program.CreateIncomingMessage(om.Data, om.BitLength);
            if (im.Data == null || im.Data.Length == 0)
                throw new LidgrenException("bad im!");

            if (!im.Decrypt(algo))
                throw new LidgrenException("failed to decrypt");

            if (im.Data == null || im.Data.Length == 0 || im.BitLength != unencLen)
                throw new LidgrenException("Length fail");

            var str = im.ReadString();
            if (str != "Hallon")
                throw new LidgrenException("fail");
            if (im.ReadInt32() != 42)
                throw new LidgrenException("fail");
            if (im.ReadInt32(5) != 5)
                throw new LidgrenException("fail");
            if (im.ReadBool() != true)
                throw new LidgrenException("fail");
            if (im.ReadString() != "kokos")
                throw new LidgrenException("fail");

            if (printName)
                Console.WriteLine(" - " + algo.GetType().Name + " OK");
        }

        public static NetXteaEncryption[] TestSRPWithRandomness(NetPeer peer)
        {
            int parallelism = (int)Math.Max(1, Environment.ProcessorCount * 0.75);

            var xtea = new NetXteaEncryption[2000 + 1000 * parallelism];

            Console.WriteLine(
                $"Testing SRP helper {xtea.Length} times (degree of parallelism: {parallelism})...");

            Parallel.For(
                0,
                xtea.Length, 
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                (i) =>
            {
                var salt = NetSRP.CreateRandomSalt();
                var x = NetSRP.ComputePrivateKey("user", "password", salt);

                var v = NetSRP.ComputeServerVerifier(x);
                //Console.WriteLine("v = " + NetUtility.ToHexString(v));

                var a = NetSRP.CreateRandomEphemeral(); //  NetUtility.ToByteArray("393ed364924a71ba7258633cc4854d655ca4ec4e8ba833eceaad2511e80db2b5");
                var A = NetSRP.ComputeClientEphemeral(a);
                //Console.WriteLine("A = " + NetUtility.ToHexString(A));

                var b = NetSRP.CreateRandomEphemeral(); // NetUtility.ToByteArray("cc4d87a90db91067d52e2778b802ca6f7d362490c4be294b21b4a57c71cf55a9");
                var B = NetSRP.ComputeServerEphemeral(b, v);
                //Console.WriteLine("B = " + NetUtility.ToHexString(B));

                var u = NetSRP.ComputeU(A, B);
                //Console.WriteLine("u = " + NetUtility.ToHexString(u));

                var Ss = NetSRP.ComputeServerSessionValue(A, v, u, b);
                //Console.WriteLine("Ss = " + NetUtility.ToHexString(Ss));

                var Sc = NetSRP.ComputeClientSessionValue(B, x, u, a);
                //Console.WriteLine("Sc = " + NetUtility.ToHexString(Sc));

                if (Ss != Sc)
                    throw new LidgrenException("SRP non matching session values!");

                xtea[i] = NetSRP.CreateEncryption(peer, Ss);
            });

            return xtea;
        }
    }
}
