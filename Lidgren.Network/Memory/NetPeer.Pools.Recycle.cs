
using System.Collections.Generic;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        internal void Recycle(byte[] storage)
        {
            if (m_storagePool == null || storage == null)
                return;

            lock (m_storagePool)
            {
                m_bytesInPool += storage.Length;
                int cnt = m_storagePool.Count;
                for (int i = 0; i < cnt; i++)
                {
                    if (m_storagePool[i] == null)
                    {
                        m_storagePool[i] = storage;
                        return;
                    }
                }
                m_storagePool.Add(storage);
            }
        }

        /// <summary>
        /// Recycles a NetIncomingMessage instance for reuse; taking pressure off the garbage collector
        /// </summary>
        public void Recycle(NetIncomingMessage msg)
        {
            if (m_incomingMessagesPool == null)
                return;

            NetException.Assert(m_incomingMessagesPool.Contains(msg) == false, "Recyling already recycled message! Thread race?");

            byte[] storage = msg.m_data;
            msg.m_data = null;
            Recycle(storage);
            msg.Reset();
            m_incomingMessagesPool.Enqueue(msg);
        }

        /// <summary>
        /// Recycles a list of NetIncomingMessage instances for reuse; taking pressure off the garbage collector
        /// </summary>
        public void Recycle(IEnumerable<NetIncomingMessage> toRecycle)
        {
            if (m_incomingMessagesPool == null)
                return;

            // first recycle the storage of each message
            if (m_storagePool != null)
            {
                lock (m_storagePool)
                {
                    foreach (var msg in toRecycle)
                    {
                        var storage = msg.m_data;
                        msg.m_data = null;
                        m_bytesInPool += storage.Length;
                        int cnt = m_storagePool.Count;
                        for (int i = 0; i < cnt; i++)
                        {
                            if (m_storagePool[i] == null)
                            {
                                m_storagePool[i] = storage;
                                return;
                            }
                        }
                        msg.Reset();
                        m_storagePool.Add(storage);
                    }
                }
            }

            // then recycle the message objects
            m_incomingMessagesPool.Enqueue(toRecycle);
        }

        internal void Recycle(NetOutgoingMessage msg)
        {
            if (m_outgoingMessagesPool == null)
                return;

            NetException.Assert(m_outgoingMessagesPool.Contains(msg) == false, "Recyling already recycled message! Thread race?");

            byte[] storage = msg.m_data;
            msg.m_data = null;

            // message fragments cannot be recycled
            // TODO: find a way to recycle large message after all fragments has been acknowledged; or? possibly better just to garbage collect them
            if (msg.m_fragmentGroup == 0)
                Recycle(storage);

            msg.Reset();
            m_outgoingMessagesPool.Enqueue(msg);
        }
    }
}
