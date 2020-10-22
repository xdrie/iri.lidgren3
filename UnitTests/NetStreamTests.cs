using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Lidgren.Network;

namespace UnitTests
{
    public static class NetStreamTests
    {
        public static void Run()
        {
            Console.WriteLine("Testing streams");

            string appId = "NetStreamTest";
            int port = 20001;

            var serverThread = new Thread(() =>
            {
                var config = new NetPeerConfiguration(appId)
                {
                    AcceptIncomingConnections = true,
                    Port = port
                };
                config.DisableMessageType(NetIncomingMessageType.DebugMessage);
                var server = new NetServer(config);
                server.Start();

                while (server.TryReadMessage(5000, out var message))
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.StatusChanged:
                            Console.WriteLine("Server Status: " + message.ReadEnum<NetConnectionStatus>());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Console.WriteLine("Server Debug: " + message.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            Console.WriteLine("Server Warning: " + message.ReadString());
                            break;

                        case NetIncomingMessageType.Data:
                            Console.WriteLine("Server Data: " + message.ReadString());
                            break;

                        default:
                            Console.WriteLine(message.MessageType);
                            break;
                    }
                }
            });

            var clientThread = new Thread(() =>
            {
                var config = new NetPeerConfiguration(appId)
                {
                    AcceptIncomingConnections = false
                };
                config.DisableMessageType(NetIncomingMessageType.DebugMessage);
                var client = new NetClient(config);
                client.Start();

                var connection = client.Connect(new IPEndPoint(IPAddress.Loopback, port));
                while (connection.Status != NetConnectionStatus.Connected)
                {
                    if (connection.Status == NetConnectionStatus.Disconnected)
                        throw new Exception("Failed to connect.");
                    Thread.Sleep(1);
                }

                var msg = client.CreateMessage("hello");
                connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);

                while (client.TryReadMessage(5000, out var message))
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.StatusChanged:
                            Console.WriteLine("Client Status: " + message.ReadEnum<NetConnectionStatus>());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Console.WriteLine("Client Debug: " + message.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            Console.WriteLine("Client Warning: " + message.ReadString());
                            break;

                        case NetIncomingMessageType.Data:
                            Console.WriteLine("Client Data: " + message.ReadString());
                            break;

                        default:
                            Console.WriteLine(message.MessageType);
                            break;
                    }
                }
            });

            serverThread.Start();
            clientThread.Start();

            serverThread.Join();
            clientThread.Join();
        }
    }
}
