using System;
using System.Reflection;
using Lidgren.Network;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Buffers.Binary;
using System.Numerics;

namespace UnitTests
{
    class Program
    {
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

            NetQueueTests.Run();

            BitVectorTests.Run();

            var config = new NetPeerConfiguration("unittests");
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            config.EnableUPnP = true;

            var peer = new NetPeer(config);
            peer.Start(); // needed for initialization

            Console.WriteLine("Unique identifier is " + NetUtility.ToHexString(peer.UniqueIdentifier));

            ReadWriteTests.Run(peer);

            MiscTests.Run(peer);

            //EncryptionTests.Run(peer);

            var om = peer.CreateMessage("henlo from myself");
            peer.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, peer.Port));
            try
            {
                peer.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, peer.Port));

                Console.WriteLine(nameof(CannotResendException) + " check failed");
            }
            catch (CannotResendException)
            {
                Console.WriteLine(nameof(CannotResendException) + " check OK");
            }

            // read all message
            while (peer.TryReadMessage(5000, out var message))
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

                    case NetIncomingMessageType.Data:
                        Console.WriteLine("Data: " + message.ReadString());
                        break;

                    case NetIncomingMessageType.UnconnectedData:
                        Console.WriteLine("UnconnectedData: " + message.ReadString());
                        break;

                    default:
                        Console.WriteLine(message.MessageType);
                        break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tests finished");
            Console.ReadKey();
        }

        /// <summary>
        /// Helper method
        /// </summary>
        public static NetIncomingMessage CreateIncomingMessage(ReadOnlySpan<byte> fromData, int bitLength)
        {
            var inc = new NetIncomingMessage(
                fromData.Slice(0, NetBitWriter.BytesForBits(bitLength)).ToArray());
            inc.BitLength = bitLength;
            return inc;
        }
    }
}
