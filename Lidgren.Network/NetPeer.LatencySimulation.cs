﻿/* Copyright (c) 2010 Michael Lidgren

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
using System.Diagnostics;
#if !__NOIPENDPOINT__
using NetEndPoint = System.Net.IPEndPoint;
using NetAddress = System.Net.IPAddress;
#endif

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        private readonly List<DelayedPacket> m_delayedPackets = new List<DelayedPacket>();

        //Avoids allocation on mapping to IPv6
        private NetEndPoint _targetCopy = new NetEndPoint(NetAddress.Any, 0);

        private readonly struct DelayedPacket
        {
            public byte[] Data { get; }
            public double DelayedUntil { get; }
            public NetEndPoint Target { get; }

            public DelayedPacket(byte[] data, double delayedUntil, NetEndPoint target)
            {
                Data = data;
                DelayedUntil = delayedUntil;
                Target = target;
            }
        }

        internal void SendPacket(int numBytes, NetEndPoint target, int numMessages, out bool connectionReset)
        {
            connectionReset = false;

            // simulate loss
            float loss = m_configuration.m_loss;
            if (loss > 0f)
            {
                if ((float)MWCRandom.Instance.NextDouble() < loss)
                {
                    LogVerbose("Sending packet " + numBytes + " bytes - SIMULATED LOST!");
                    return; // packet "lost"
                }
            }

            m_statistics.PacketSent(numBytes, numMessages);

            // simulate latency
            float m = m_configuration.m_minimumOneWayLatency;
            float r = m_configuration.m_randomOneWayLatency;
            if (m == 0f && r == 0f)
            {
                // no latency simulation
                // LogVerbose("Sending packet " + numBytes + " bytes");
                _ = ActuallySendPacket(m_sendBuffer, numBytes, target, out connectionReset);
                // TODO: handle wasSent == false?

                if (m_configuration.m_duplicates > 0f && MWCRandom.Instance.NextDouble() < m_configuration.m_duplicates)
                    ActuallySendPacket(m_sendBuffer, numBytes, target, out connectionReset); // send it again!

                return;
            }

            int num = 1;
            if (m_configuration.m_duplicates > 0f && MWCRandom.Instance.NextSingle() < m_configuration.m_duplicates)
                num++;

            for (int i = 0; i < num; i++)
            {
                float delay = m_configuration.m_minimumOneWayLatency +
                    (MWCRandom.Instance.NextSingle() * m_configuration.m_randomOneWayLatency);

                var data = new byte[numBytes];
                Buffer.BlockCopy(m_sendBuffer, 0, data, 0, numBytes);

                // Enqueue delayed packet
                var p = new DelayedPacket(data, NetTime.Now + delay, target);
                m_delayedPackets.Add(p);
            }

            // LogVerbose("Sending packet " + numBytes + " bytes - delayed " + NetTime.ToReadable(delay));
        }

        private void SendDelayedPackets()
        {
            if (m_delayedPackets.Count <= 0)
                return;

            double now = NetTime.Now;

            RestartDelaySending:
            foreach (DelayedPacket p in m_delayedPackets)
            {
                if (now > p.DelayedUntil)
                {
                    ActuallySendPacket(p.Data, p.Data.Length, p.Target, out _);
                    m_delayedPackets.Remove(p);
                    goto RestartDelaySending;
                }
            }
        }

        private void FlushDelayedPackets()
        {
            try
            {
                foreach (DelayedPacket p in m_delayedPackets)
                    ActuallySendPacket(p.Data, p.Data.Length, p.Target, out bool _);
                m_delayedPackets.Clear();
            }
            catch
            {
            }
        }

        internal bool ActuallySendPacket(byte[] data, int numBytes, NetEndPoint target, out bool connectionReset)
        {
            connectionReset = false;
            NetAddress ba = default;

            try
            {
                ba = NetUtility.GetCachedBroadcastAddress();

                // TODO: refactor this check outta here
                if (target.Address.Equals(ba))
                {
                    // Some networks do not allow 
                    // a global broadcast so we use the BroadcastAddress from the configuration
                    // this can be resolved to a local broadcast addresss e.g 192.168.x.255                    
                    _targetCopy.Address = m_configuration.BroadcastAddress;
                    _targetCopy.Port = target.Port;
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                }
                else if (
                    m_configuration.DualStack &&
                    m_configuration.LocalAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    NetUtility.CopyEndpoint(target, _targetCopy); //Maps to IPv6 for Dual Mode
                }
                else
                {
                    _targetCopy.Port = target.Port;
                    _targetCopy.Address = target.Address;
                }

                int bytesSent = Socket.SendTo(data, 0, numBytes, SocketFlags.None, _targetCopy);
                if (numBytes != bytesSent)
                    LogWarning("Failed to send the full " + numBytes + "; only " + bytesSent + " bytes sent in packet!");

                // LogDebug("Sent " + numBytes + " bytes");
            }
            catch (SocketException sx)
            {
                if (sx.SocketErrorCode == SocketError.WouldBlock)
                {
                    // send buffer full?
                    LogWarning("Socket threw exception; would block - send buffer full? Increase in NetPeerConfiguration");
                    return false;
                }
                if (sx.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable" 
                    connectionReset = true;
                    return false;
                }
                LogError("Failed to send packet: " + sx);
            }
            catch (Exception ex)
            {
                LogError("Failed to send packet: " + ex);
            }
            finally
            {
                if (target.Address.Equals(ba))
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
            }
            return true;
        }

        internal bool SendMTUPacket(int numBytes, NetEndPoint target)
        {
            try
            {
                Socket.DontFragment = true;
                int bytesSent = Socket.SendTo(m_sendBuffer, 0, numBytes, SocketFlags.None, target);
                if (numBytes != bytesSent)
                    LogWarning("Failed to send the full " + numBytes + "; only " + bytesSent + " bytes sent in packet!");

                m_statistics.PacketSent(numBytes, 1);
            }
            catch (SocketException sx)
            {
                if (sx.SocketErrorCode == SocketError.MessageSize)
                    return false;
                if (sx.SocketErrorCode == SocketError.WouldBlock)
                {
                    // send buffer full?
                    LogWarning("Socket threw exception; would block - send buffer full? Increase in NetPeerConfiguration");
                    return true;
                }
                if (sx.SocketErrorCode == SocketError.ConnectionReset)
                    return true;
                LogError("Failed to send packet: (" + sx.SocketErrorCode + ") " + sx);
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
