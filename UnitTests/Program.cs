using System;
using System.Reflection;
using Lidgren.Network;
using System.Net;
using System.Runtime.Intrinsics.X86;

namespace UnitTests
{
    class Program
    {
        public static unsafe string ToHexString(ReadOnlySpan<byte> data)
        {
            // TODO: pass Span directly, not now as ref-structs are not supported by generics

            fixed (byte* dataPtr = data)
            {
                return string.Create(data.Length * 2, (IntPtr)dataPtr, (dst, srcPtr) =>
                {
                    var src = new ReadOnlySpan<byte>((byte*)srcPtr, dst.Length / 2);

                    for (int i = 0; i < dst.Length / 2; i++)
                    {
                        int value = src[i];

                        int a = value >> 4;
                        dst[i * 2 + 0] = (char)(a > 9 ? a + 0x37 : a + 0x30);

                        int b = value & 0xF;
                        dst[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                    }
                });
            }
        }
        static void Main(string[] args)
        {
            //Span<byte> src = new byte[3] { 254, 255, 255 };
            //Span<byte> dst = new byte[3];
            //NetBitWriter.CopyBits(src, 1, 17, dst, 1);

            // TODO: check use of GetAddressBytes and optimize with span

            //Span<byte> src = new byte[] { 255, 255 }; // 0b00110010, 0b00111100 };
            //Span<byte> dst = new byte[3] { 0, 0, 0 };
            //NetBitWriter.CopyBits(src, 0, 16, dst, 9);
            //string r =
            //    Convert.ToString(dst[0], 2).PadLeft(8, '0') + "_" +
            //    Convert.ToString(dst[1], 2).PadLeft(8, '0') + "_" +
            //    Convert.ToString(dst[2], 2).PadLeft(8, '0');
            //Console.WriteLine(r);

            var config = new NetPeerConfiguration("unittests");
            config.EnableUPnP = true;

            var peer = new NetPeer(config);
            peer.Start(); // needed for initialization

            Console.WriteLine("Unique identifier is " + NetUtility.ToHexString(peer.UniqueIdentifier));

            ReadWriteTests.Run(peer);

            NetQueueTests.Run();

            MiscTests.Run(peer);

            BitVectorTests.Run();

            EncryptionTests.Run(peer);

            var om = peer.CreateMessage();
            peer.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, 14242));
            try
            {
                peer.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, 14242));

                Console.WriteLine(nameof(CannotResendException) + " check failed");
            }
            catch (CannotResendException)
            {
                Console.WriteLine(nameof(CannotResendException) + " check OK");
            }

            // read all message
            while (peer.TryReadMessage(10000, out var message))
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        Console.WriteLine("Peer message: " + message.ReadString());
                        break;

                    case NetIncomingMessageType.Error:
                        throw new Exception("Received error message!");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tests finished");
            Console.ReadKey();
        }

        /// <summary>
        /// Helper method
        /// </summary>
        public static NetIncomingMessage CreateIncomingMessage(ReadOnlyMemory<byte> fromData, int bitLength)
        {
            var inc = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
            inc.Data = fromData.ToArray();
            inc.BitLength = bitLength;
            return inc;
        }
    }
}
