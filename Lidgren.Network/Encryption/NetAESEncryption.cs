using System;
using System.Security.Cryptography;

namespace Lidgren.Network
{
	public sealed class NetAESEncryption : NetCryptoProviderBase
	{
		public NetAESEncryption(NetPeer peer) : base(peer, Aes.Create())
		{
		}

		public NetAESEncryption(NetPeer peer, ReadOnlySpan<byte> key) : this(peer)
		{
			SetKey(key);
		}

		public NetAESEncryption(NetPeer peer, ReadOnlySpan<char> key) : this(peer)
		{
			SetKey(key);
		}
	}
}
