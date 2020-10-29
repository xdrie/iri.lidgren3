using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

                void OnStream(NetStream stream)
                {
                    int transferred = 0;

                    var readTask = Task.Run(() =>
                    {
                        Span<byte> tmp = stackalloc byte[4096];
                        int read;
                        while ((read = stream.Read(tmp)) > 0)
                        {
                            //Console.WriteLine("Server Stream Read: " + tmp[0]);
                            transferred += read;
                        }

                        Console.WriteLine($"Server Stream {stream.Channel} Read Finished: {transferred}");
                    });

                    Task.Run(() =>
                    {
                        while (!readTask.IsCompleted)
                        {
                            Console.WriteLine($"Server Stream {stream.Channel} Read Transferred: " + transferred);
                            Thread.Sleep(500);
                        }
                    });
                }

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

                        case NetIncomingMessageType.StreamData:
                        {
                            var type = (NetStreamMessageType)message.ReadByte();
                            int channel = message.SequenceChannel;

                            if (type != NetStreamMessageType.Data)
                                Console.WriteLine("Server Stream: " + type);

                            var connection = message.SenderConnection;
                            if (connection == null)
                            {
                                // send error message back to sender
                                break;
                            }

                            ref NetStream? stream = ref connection._openStreams[channel];
                            switch (type)
                            {
                                case NetStreamMessageType.Open:
                                    if (stream != null)
                                    {
                                        // send "AlreadyOpen" message
                                        break;
                                    }
                                    stream = new NetStream(server.DefaultScheduler, connection, channel);

                                    OnStream(stream);
                                    stream.Peer.Recycle(message);
                                    break;

                                case NetStreamMessageType.Data:
                                    if (stream == null)
                                    {
                                        // send "NotOpen" message
                                        break;
                                    }
                                    stream.OnDataMessage(message);
                                    break;

                                case NetStreamMessageType.Close:
                                    if (stream == null)
                                    {
                                        // send "NotOpen" message (or maybe not?) 
                                        break;
                                    }
                                    stream.OnCloseMessage(message);
                                    stream = null;
                                    break;
                            }
                            break;
                        }

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

                for (int j = 0; j < 8; j++)
                {
                    int channel = j;
                    Task.Run(() =>
                    {
                        try
                        {
                            var stream = new NetStream(client.DefaultScheduler, connection, channel);
                            Span<byte> span = stackalloc byte[1024 * 4];
                            for (int i = 0; i < 1024 * 1024 * 32; i += span.Length)
                            {
                                stream.Write(span);
                                //Thread.Sleep(1);
                            }
                            stream.Dispose();
                            Console.WriteLine($"Server Stream {channel} Data Written");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
                }

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

                        case NetIncomingMessageType.StreamData:
                        {
                            var type = (NetStreamMessageType)message.ReadByte();
                            int channel = message.SequenceChannel;

                            Console.WriteLine("Client Stream: " + type);

                            client.Recycle(message);
                            break;
                        }

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
