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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        private readonly struct DelayedPacket
        {
            public byte[] Data { get; }
            public TimeSpan DelayedUntil { get; }
            public IPEndPoint Target { get; }

            public DelayedPacket(byte[] data, TimeSpan delayedUntil, IPEndPoint target)
            {
                Data = data;
                DelayedUntil = delayedUntil;
                Target = target;
            }
        }

        private List<DelayedPacket> DelayedPackets { get; } = new List<DelayedPacket>();

        //Avoids allocation on mapping to IPv6
        private IPEndPoint _targetCopy = new IPEndPoint(IPAddress.Any, 0);

        internal void SendPacket(int byteCount, IPEndPoint target, int numMessages, out bool connectionReset)
        {
            connectionReset = false;

            // simulate loss
            float loss = Configuration._loss;
            if (loss > 0f)
            {
                if (MWCRandom.Global.NextSingle() < loss)
                {
                    LogVerbose("Sending packet " + byteCount + " bytes - SIMULATED LOST!");
                    return; // packet "lost"
                }
            }

            Statistics.PacketSent(byteCount, numMessages);

            // simulate latency
            var m = Configuration._minimumOneWayLatency;
            var r = Configuration._randomOneWayLatency;
            if (m == TimeSpan.Zero && r == TimeSpan.Zero)
            {
                // no latency simulation
                //LogVerbose("Sending packet " + numBytes + " bytes");
                bool wasSent = ActuallySendPacket(_sendBuffer, byteCount, target, out connectionReset);
                // TODO: handle 'wasSent == false' better?

                if ((!wasSent && !connectionReset) ||
                    Configuration._duplicates > 0f && MWCRandom.Global.NextSingle() < Configuration._duplicates)
                {
                    ActuallySendPacket(_sendBuffer, byteCount, target, out connectionReset); // send it again!
                }
                return;
            }

            int num = 1;
            if (Configuration._duplicates > 0f && MWCRandom.Global.NextSingle() < Configuration._duplicates)
                num++;

            var now = NetTime.Now;
            for (int i = 0; i < num; i++)
            {
                var delay = Configuration._minimumOneWayLatency +
                    (MWCRandom.Global.NextSingle() * Configuration._randomOneWayLatency);

                var data = new byte[byteCount]; // TODO: ArrayPool
                Buffer.BlockCopy(_sendBuffer, 0, data, 0, byteCount);

                // Enqueue delayed packet
                var p = new DelayedPacket(data, now + delay, target);
                DelayedPackets.Add(p);
            }

            // LogVerbose("Sending packet " + numBytes + " bytes - delayed " + NetTime.ToReadable(delay));
        }

        private void SendDelayedPackets()
        {
            if (DelayedPackets.Count == 0)
                return;

            var now = NetTime.Now;

            // reverse-for so elements can be removed without breaking loop
            for (int i = DelayedPackets.Count; i-- > 0;)
            {
                var p = DelayedPackets[i];
                if (now > p.DelayedUntil)
                {
                    ActuallySendPacket(p.Data, p.Data.Length, p.Target, out _);
                    DelayedPackets.RemoveAt(i);
                }
            }
        }

        private void FlushDelayedPackets()
        {
            foreach (DelayedPacket p in DelayedPackets)
            {
                try
                {
                    ActuallySendPacket(p.Data, p.Data.Length, p.Target, out bool _);
                }
                catch (Exception ex)
                {
                    LogWarning("Failed to flush delayed packet: " + ex);
                }
            }
            DelayedPackets.Clear();
        }

        // TODO: replace byte[] with Span in the future (held back by Socket.SendTo)
        // https://github.com/dotnet/runtime/issues/33418
        internal bool ActuallySendPacket(byte[] data, int numBytes, IPEndPoint target, out bool connectionReset)
        {
            if (Socket == null)
                throw new InvalidOperationException("No socket bound.");

            connectionReset = false;
            bool broadcasting = false;

            try
            {
                IPAddress? ba = NetUtility.GetBroadcastAddress();

                // TODO: refactor this check outta here
                if (target.Address.Equals(ba))
                {
                    // Some networks do not allow 
                    // a global broadcast so we use the BroadcastAddress from the configuration
                    // this can be resolved to a local broadcast addresss e.g 192.168.x.255                    
                    _targetCopy.Address = Configuration.BroadcastAddress;
                    _targetCopy.Port = target.Port;

                    Socket.EnableBroadcast = true;
                    broadcasting = true;
                }
                else if (
                    Configuration.DualStack &&
                    Configuration.LocalAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Maps to IPv6 for Dual Mode
                    NetUtility.MapToIPv6(target, _targetCopy);
                }
                else
                {
                    _targetCopy.Port = target.Port;
                    _targetCopy.Address = target.Address;
                }

                int bytesSent = Socket.SendTo(data, 0, numBytes, SocketFlags.None, _targetCopy);
                if (numBytes != bytesSent)
                    LogWarning(
                        "Failed to send the full " + numBytes + "; only " + bytesSent + " bytes sent in packet!");

                //LogDebug("Sent " + numBytes + " bytes");
            }
            catch (SocketException sx)
            {
                switch (sx.SocketErrorCode)
                {
                    case SocketError.WouldBlock:
                        // send buffer full?
                        LogWarning(
                            "Socket threw exception; would block - send buffer full? Increase in NetPeerConfiguration");
                        return false;

                    case SocketError.ConnectionReset:
                        // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable" 
                        connectionReset = true;
                        return false;

                    default:
                        LogError("Failed to send packet: " + sx);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to send packet: " + ex);
            }
            finally
            {
                if (broadcasting)
                    Socket.EnableBroadcast = false;
            }
            return true;
        }

        internal bool SendMTUPacket(int numBytes, IPEndPoint target)
        {
            if (Socket == null)
                throw new InvalidOperationException("No socket bound.");

            try
            {
                Socket.DontFragment = true;

                int bytesSent = Socket.SendTo(_sendBuffer, 0, numBytes, SocketFlags.None, target);
                if (numBytes != bytesSent)
                    LogWarning("Failed to send the full " + numBytes + "; only " + bytesSent + " bytes sent in packet!");

                Statistics.PacketSent(numBytes, 1);
            }
            catch (SocketException sx)
            {
                switch (sx.SocketErrorCode)
                {
                    case SocketError.MessageSize:
                        return false;

                    case SocketError.WouldBlock:
                        // send buffer full?
                        LogWarning(
                            "Socket threw exception; would block - send buffer full? Increase in NetPeerConfiguration");
                        return true;

                    case SocketError.ConnectionReset:
                        return true;

                    default:
                        LogError("Failed to send packet: (" + sx.SocketErrorCode + ") " + sx);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to send packet: " + ex);
            }
            finally
            {
                Socket.DontFragment = false;
            }
            return true;
        }
    }
}
