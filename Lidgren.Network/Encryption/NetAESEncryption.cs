using System.Security.Cryptography;

namespace Lidgren.Network
{
	public class NetAESEncryption : NetCryptoProviderBase
	{
		public NetAESEncryption(NetPeer peer)
			: base(peer, Aes.Create())
		{
		}

		public NetAESEncryption(NetPeer peer, string key)
			: base(peer, Aes.Create())
		{
			SetKey(key);
		}

		public NetAESEncryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, Aes.Create())
		{
			SetKey(data, offset, count);
		}
	}
}
