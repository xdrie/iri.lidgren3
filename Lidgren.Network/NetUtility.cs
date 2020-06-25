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
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    // TODO: replace IAsyncResult with Task<>

    /// <summary>
    /// Utility methods
    /// </summary>
    public static partial class NetUtility
    {
        internal static SHA256 Sha256 { get; } = SHA256.Create();

        /// <summary>
        /// Resolve endpoint callback
        /// </summary>
        public delegate void ResolveEndPointCallback(IPEndPoint? endPoint);

        /// <summary>
        /// Resolve address callback
        /// </summary>
        public delegate void ResolveAddressCallback(IPAddress? address);

        private static IPAddress? _cachedBroadcastAddress;

        /// <summary>
        /// Get IPv4 endpoint from notation (xxx.xxx.xxx.xxx) or hostname and port number asynchronously.
        /// </summary>
        public static void ResolveAsync(ReadOnlySpan<char> host, int port, ResolveEndPointCallback callback)
        {
            ResolveAsync(host, (resolved) =>
            {
                if (resolved == null)
                    callback(null);
                else
                    callback(new IPEndPoint(resolved, port));
            });
        }

        /// <summary>
        /// Get IPv4 endpoint from notation (xxx.xxx.xxx.xxx) or hostname and port number
        /// </summary>
        public static IPEndPoint? Resolve(ReadOnlySpan<char> host, int port)
        {
            var resolved = Resolve(host);
            return resolved == null ? null : new IPEndPoint(resolved, port);
        }

        public static IPAddress? GetCachedBroadcastAddress()
        {
            if (_cachedBroadcastAddress == null)
                _cachedBroadcastAddress = GetBroadcastAddress();
            return _cachedBroadcastAddress;
        }

        /// <summary>
        /// Get IPv4 address from notation (xxx.xxx.xxx.xxx) or hostname asynchronously.
        /// </summary>
        public static void ResolveAsync(ReadOnlySpan<char> host, ResolveAddressCallback callback)
        {
            if (host.IsEmpty)
                throw new ArgumentException("Supplied string must not be empty.", nameof(host));

            host = host.Trim();

            if (IPAddress.TryParse(host, out IPAddress ipAddress))
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork ||
                    ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    callback?.Invoke(ipAddress);
                    return;
                }

                throw new ArgumentException(
                    "This method will not currently resolve other than IPv4 and IPv6 addresses.",
                    nameof(host));
            }

            // ok must be a host name
            IPHostEntry entry;
            try
            {
                Dns.BeginGetHostEntry(host.ToString(), (result) =>
                {
                    try
                    {
                        entry = Dns.EndGetHostEntry(result);
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.HostNotFound)
                        {
                            //LogWrite(string.Format(CultureInfo.InvariantCulture, "Failed to resolve host '{0}'.", host.ToString()));
                            callback?.Invoke(null);
                            return;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (entry == null)
                    {
                        callback?.Invoke(null);
                        return;
                    }

                    // check each entry for a valid IP address
                    foreach (var ipCurrent in entry.AddressList)
                    {
                        if (ipCurrent.AddressFamily == AddressFamily.InterNetwork ||
                            ipCurrent.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            callback?.Invoke(ipCurrent);
                            return;
                        }
                    }

                    callback?.Invoke(null);

                }, null);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.HostNotFound)
                {
                    //LogWrite(string.Format(CultureInfo.InvariantCulture, "Failed to resolve host '{0}'.", host.ToString()));
                    callback?.Invoke(null);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Get IPv4 address from notation (xxx.xxx.xxx.xxx) or hostname.
        /// </summary>
        public static IPAddress? Resolve(ReadOnlySpan<char> host)
        {
            if (host.IsEmpty)
                throw new ArgumentException("Supplied string must not be empty.", nameof(host));

            host = host.Trim();

            if (IPAddress.TryParse(host, out IPAddress ipAddress))
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork ||
                    ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    return ipAddress;

                throw new ArgumentException(
                    "This method will not currently resolve other than IPv4 or IPv6 addresses.",
                    nameof(host));
            }

            // ok must be a host name
            try
            {
                var addresses = Dns.GetHostAddresses(host.ToString());
                if (addresses == null)
                    return null;

                foreach (var address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork ||
                        address.AddressFamily == AddressFamily.InterNetworkV6)
                        return address;
                }
                return null;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.HostNotFound)
                {
                    //LogWrite(string.Format(CultureInfo.InvariantCulture, "Failed to resolve host '{0}'.", host.ToString()));
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        // TODO: replace hex methods with fast net5 ones

        public static int GetHexCharCount(int byteCount)
        {
            return byteCount * 2;
        }

        public static void ToHexString(ReadOnlySpan<byte> source, Span<char> destination)
        {
            destination = destination.Slice(0, Math.Min(destination.Length, GetHexCharCount(source.Length)));
            source = source.Slice(0, Math.Min(source.Length, destination.Length / 2));

            if (source.IsEmpty || destination.IsEmpty)
                return;

            for (int i = 0; i < destination.Length / 2; i++)
            {
                int value = source[i];

                int a = value >> 4;
                destination[i * 2 + 0] = (char)(a > 9 ? a + 0x37 : a + 0x30);

                int b = value & 0xF;
                destination[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
        }

        /// <summary>
        /// Create a hex string from an array of bytes
        /// </summary>
        public static unsafe string ToHexString(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty)
                return string.Empty;

            // TODO: pass Span directly, not now as ref-structs are not supported by generics

            fixed (byte* srcPtr = source)
            {
                return string.Create(source.Length * 2, (IntPtr)srcPtr, (dst, srcPtr) =>
                {
                    var src = new ReadOnlySpan<byte>((byte*)srcPtr, dst.Length / 2);
                    ToHexString(src, dst);
                });
            }
        }

        /// <summary>
        /// Create a hex string from an <see cref="long"/> value.
        /// </summary>
        public static string ToHexString(long value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(tmp, value);
            return ToHexString(tmp);
        }

        public static int GetHexByteCount(int textLength)
        {
            return (textLength + 1) / 2;
        }

        /// <summary>
        /// Convert a hexadecimal text to into bytes.
        /// </summary>
        public static void FromHexString(ReadOnlySpan<char> source, Span<byte> destination)
        {
            destination = destination.Slice(0, Math.Min(destination.Length, GetHexByteCount(source.Length)));
            source = source.Slice(0, Math.Min(source.Length / 2 * 2, destination.Length / 2));

            if (source.IsEmpty || destination.IsEmpty)
                return;

            int i = 0;
            for (; i < source.Length; i += 2)
                destination[i / 2] = byte.Parse(source[i..(i + 1)], NumberStyles.HexNumber);

            if (source.Length - i > 0)
                destination[i] = byte.Parse(stackalloc char[] { source[i] }, NumberStyles.HexNumber);
        }

        public static byte[] FromHexString(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
                return Array.Empty<byte>();

            byte[] array = new byte[GetHexByteCount(text.Length)];
            FromHexString(text, array);
            return array;
        }

        /// <summary>
        /// Returns true if the supplied endpoint is on the same subnet as this host.
        /// </summary>
        public static bool IsLocal(IPEndPoint endPoint)
        {
            if (endPoint == null)
                return false;

            return IsLocal(endPoint.Address);
        }

        /// <summary>
        /// Returns true if the supplied address is on the same subnet as this host.
        /// </summary>
        public static bool IsLocal(IPAddress remote)
        {
            if (remote == null)
                return false;

            if (!GetLocalAddress(out IPAddress? local, out IPAddress? mask))
                return false;

            uint maskBits = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint remoteBits = BitConverter.ToUInt32(remote.GetAddressBytes(), 0);
            uint localBits = BitConverter.ToUInt32(local.GetAddressBytes(), 0);

            // compare network portions
            return (remoteBits & maskBits) == (localBits & maskBits);
        }

        /// <summary>
        /// Returns how many bits are necessary to hold a certain number
        /// </summary>
        [CLSCompliant(false)]
        public static int BitCountForValue(uint value)
        {
            int bits = 1;
            while ((value >>= 1) != 0)
                bits++;
            return bits;
        }

        /// <summary>
        /// Returns how many bits are necessary to hold a certain number.
        /// </summary>
        [CLSCompliant(false)]
        public static int BitCountForValue(ulong value)
        {
            int bits = 1;
            while ((value >>= 1) != 0)
                bits++;
            return bits;
        }

        /// <summary>
        /// Returns how many bytes are required to hold a certain number of bits.
        /// </summary>
        public static int ByteCountForBits(int bitCount)
        {
            return (bitCount + 7) / 8;
        }

        /// <summary>
        /// Converts a number of bytes to a shorter, more readable string representation
        /// </summary>
        public static string ToHumanReadable(long bytes)
        {
            if (bytes < 4000) // 1-4 kb is printed in bytes
                return bytes + " bytes";
            if (bytes < 1000 * 1000) // 4-999 kb is printed in kb
                return Math.Round(bytes / 1000.0, 2) + " kilobytes";
            return Math.Round(bytes / (1000.0 * 1000.0), 2) + " megabytes"; // else megabytes
        }

        internal static int RelativeSequenceNumber(int number, int expected)
        {
            return
                (number - expected + NetConstants.NumSequenceNumbers + NetConstants.NumSequenceNumbers / 2) %
                NetConstants.NumSequenceNumbers - NetConstants.NumSequenceNumbers / 2;

            // old impl:
            //int retval = ((nr + NetConstants.NumSequenceNumbers) - expected) % NetConstants.NumSequenceNumbers;
            //if (retval > (NetConstants.NumSequenceNumbers / 2))
            //	retval -= NetConstants.NumSequenceNumbers;
            //return retval;
        }

        /// <summary>
        /// Gets the window size used internally in the library for a certain delivery method
        /// </summary>
        public static int GetWindowSize(NetDeliveryMethod method)
        {
            switch (method)
            {
                case NetDeliveryMethod.Unknown:
                    return 0;

                case NetDeliveryMethod.Unreliable:
                case NetDeliveryMethod.UnreliableSequenced:
                    return NetConstants.UnreliableWindowSize;

                case NetDeliveryMethod.ReliableOrdered:
                    return NetConstants.ReliableOrderedWindowSize;

                case NetDeliveryMethod.ReliableSequenced:
                case NetDeliveryMethod.ReliableUnordered:
                default:
                    return NetConstants.DefaultWindowSize;
            }
        }

        // shell sort
        internal static void SortMembersList(System.Reflection.MemberInfo[] list)
        {
            int h;
            int j;
            System.Reflection.MemberInfo tmp;

            h = 1;
            while (h * 3 + 1 <= list.Length)
                h = 3 * h + 1;

            while (h > 0)
            {
                for (int i = h - 1; i < list.Length; i++)
                {
                    tmp = list[i];
                    j = i;
                    while (true)
                    {
                        if (j >= h)
                        {
                            if (string.Compare(list[j - h].Name, tmp.Name, StringComparison.InvariantCulture) > 0)
                            {
                                list[j] = list[j - h];
                                j -= h;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }

                    list[j] = tmp;
                }
                h /= 3;
            }
        }

        internal static NetDeliveryMethod GetDeliveryMethod(NetMessageType mtp)
        {
            if (mtp >= NetMessageType.UserReliableOrdered1)
                return NetDeliveryMethod.ReliableOrdered;
            else if (mtp >= NetMessageType.UserReliableSequenced1)
                return NetDeliveryMethod.ReliableSequenced;
            else if (mtp >= NetMessageType.UserReliableUnordered)
                return NetDeliveryMethod.ReliableUnordered;
            else if (mtp >= NetMessageType.UserSequenced1)
                return NetDeliveryMethod.UnreliableSequenced;
            return NetDeliveryMethod.Unreliable;
        }

        /// <summary>
        /// Copies from <paramref name="src"/> to <paramref name="dst"/>. 
        /// Maps to an IPv6 address.
        /// </summary>
        /// <param name="src">Source.</param>
        /// <param name="dst">Destination.</param>
        internal static void CopyEndpoint(IPEndPoint src, IPEndPoint dst)
        {
            dst.Port = src.Port;
            if (src.AddressFamily == AddressFamily.InterNetwork)
                dst.Address = src.Address.MapToIPv6();
            else
                dst.Address = src.Address;
        }

        /// <summary>
        /// Maps the endpoint to an IPv6 address.
        /// </summary>
        internal static IPEndPoint MapToIPv6(IPEndPoint endPoint)
        {
            if (endPoint.AddressFamily == AddressFamily.InterNetwork)
                return new IPEndPoint(endPoint.Address.MapToIPv6(), endPoint.Port);
            return endPoint;
        }
    }
}
