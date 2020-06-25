using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Lidgren.Network
{
	[Obsolete("TripleDES encryption is weak and should not be used.")]
	public sealed class NetTripleDESEncryption : NetCryptoProviderBase
	{
		[SuppressMessage("Security", "CA5350", Justification = "Encryption is obsoleted.")]
		public NetTripleDESEncryption(NetPeer peer) : base(peer, TripleDES.Create())
		{
		}

		public NetTripleDESEncryption(NetPeer peer, ReadOnlySpan<byte> key) : this(peer)
		{
			SetKey(key);
		}

		public NetTripleDESEncryption(NetPeer peer, ReadOnlySpan<char> key) : this(peer)
		{
			SetKey(key);
		}
	}
}
