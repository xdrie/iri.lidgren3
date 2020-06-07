﻿#if !__ANDROID__ && !__CONSTRAINED__ && !WINDOWS_RUNTIME && !UNITY_STANDALONE_LINUX
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    public static partial class NetUtility
    {
        private static readonly long _timeInitialized = Stopwatch.GetTimestamp();
        private static readonly double _dInvFreq = 1.0 / Stopwatch.Frequency;
        private static readonly SHA256 _sha256 = SHA256.Create();

        public static double Now => (Stopwatch.GetTimestamp() - _timeInitialized) * _dInvFreq;

        [CLSCompliant(false)]
        public static ulong GetPlatformSeed(int seedInc)
        {
            ulong seed = (ulong)Stopwatch.GetTimestamp();
            return seed ^ ((ulong)Environment.WorkingSet + (ulong)seedInc);
        }

        private static NetworkInterface GetNetworkInterface()
        {
            var computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            if (computerProperties == null)
                return null;

            var nics = NetworkInterface.GetAllNetworkInterfaces();
            if (nics == null || nics.Length < 1)
                return null;

            NetworkInterface best = null;
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Unknown)
                    continue;

                if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
                    continue;

                if (best == null)
                    best = adapter;

                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                // make sure this adapter has any ipv4 addresses
                IPInterfaceProperties properties = adapter.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
                {
                    if (unicastAddress != null &&
                        unicastAddress.Address != null &&
                        unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // Yes it does, return this network interface.
                        return adapter;
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// If available, returns the physical (MAC) address for the first usable network interface.
        /// </summary>
        public static PhysicalAddress GetMacAddress()
        {
            var ni = GetNetworkInterface();
            if (ni == null)
                return null;
            return ni.GetPhysicalAddress();
        }

        public static IPAddress GetBroadcastAddress()
        {
            var ni = GetNetworkInterface();
            if (ni == null)
                return null;

            Span<byte> addressTmp = stackalloc byte[16];
            Span<byte> subnetMaskTmp = stackalloc byte[16];

            var properties = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
            {
                if (unicastAddress != null &&
                    unicastAddress.Address != null &&
                    unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (!unicastAddress.Address.TryWriteBytes(addressTmp, out int addressLength))
                        throw new NotImplementedException("Unknown address length.");

                    if (!unicastAddress.IPv4Mask.TryWriteBytes(subnetMaskTmp, out int subnetMaskLength))
                        throw new NotImplementedException("Unknown subnet mask length.");

                    // subnet mask should realistically always have length 4
                    if (addressLength != subnetMaskLength)
                        throw new Exception("Length of IP address and subnet mask do not match.");

                    Span<byte> broadcast = stackalloc byte[addressLength];
                    for (int i = 0; i < broadcast.Length; i++)
                        broadcast[i] = (byte)(addressTmp[i] | (subnetMaskTmp[i] ^ 255));

                    return new IPAddress(broadcast);
                }
            }
            return IPAddress.Broadcast;
        }

        /// <summary>
        /// Gets my local IPv4 address (not necessarily external) and subnet mask.
        /// </summary>
        public static IPAddress GetMyAddress(out IPAddress mask)
        {
            var ni = GetNetworkInterface();
            if (ni == null)
            {
                mask = null;
                return null;
            }

            IPInterfaceProperties properties = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
            {
                if (unicastAddress != null &&
                    unicastAddress.Address != null &&
                    unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    mask = unicastAddress.IPv4Mask;
                    return unicastAddress.Address;
                }
            }

            mask = null;
            return null;
        }

        public static byte[] ComputeSHA256(byte[] bytes, int offset, int count)
        {
            return _sha256.ComputeHash(bytes, offset, count);
        }
    }
}
#endif