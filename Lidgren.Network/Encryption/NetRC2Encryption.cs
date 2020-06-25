using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Lidgren.Network
{
	[Obsolete("RC2 encryption is very weak and should not be used.")]
	public sealed class NetRC2Encryption : NetCryptoProviderBase
	{
		[SuppressMessage("Security", "CA5351", Justification = "Encryption is obsoleted.")]
		public NetRC2Encryption(NetPeer peer) : base(peer, RC2.Create())
		{
		}

		public NetRC2Encryption(NetPeer peer, ReadOnlySpan<byte> key) : this(peer)
		{
			SetKey(key);
		}

		public NetRC2Encryption(NetPeer peer, ReadOnlySpan<char> key) : this(peer)
		{
			SetKey(key);
		}
	}
}
