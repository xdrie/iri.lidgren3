using MonoGame.Utilities.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
        internal List<byte[]> m_storagePool; // sorted smallest to largest
		internal NetQueue<NetOutgoingMessage> m_outgoingMessagesPool;
		internal NetQueue<NetIncomingMessage> m_incomingMessagesPool;

        internal int m_bytesInPool;

		private void InitializePools()
		{
            if (m_configuration.UseMessageRecycling)
			{
				m_storagePool = new List<byte[]>(16);
				m_outgoingMessagesPool = new NetQueue<NetOutgoingMessage>(32);
				m_incomingMessagesPool = new NetQueue<NetIncomingMessage>(32);
			}
			else
			{
				m_storagePool = null;
				m_outgoingMessagesPool = null;
				m_incomingMessagesPool = null;
			}
		}

        internal MemoryStream GetRecyclableMemory()
        {
            return RecyclableMemoryManager.Instance.GetMemoryStream();
        }

		internal byte[] GetStorage(int minimumCapacityInBytes)
		{
			if (m_storagePool == null)
				return new byte[minimumCapacityInBytes];

			lock (m_storagePool)
			{
				for (int i = 0; i < m_storagePool.Count; i++)
				{
					byte[] retval = m_storagePool[i];
					if (retval != null && retval.Length >= minimumCapacityInBytes)
					{
						m_storagePool[i] = null;
						m_bytesInPool -= retval.Length;
						return retval;
					}
				}
			}
			m_statistics.m_totalBytesAllocated += minimumCapacityInBytes;
			return new byte[minimumCapacityInBytes];
		}
	}
}
