using System.Collections.Generic;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        internal List<byte[]?>? _storagePool; // sorted smallest to largest
        internal NetQueue<NetOutgoingMessage>? _outgoingMessagePool;
        internal NetQueue<NetIncomingMessage>? _incomingMessagePool;

        internal int _bytesInPool;

        private void InitializePools()
        {
            if (Configuration.UseMessageRecycling)
            {
                _storagePool = new List<byte[]?>(16);
                _outgoingMessagePool = new NetQueue<NetOutgoingMessage>(32);
                _incomingMessagePool = new NetQueue<NetIncomingMessage>(32);
            }
        }

        internal byte[] GetStorage(int minimumCapacityInBytes)
        {
            if (_storagePool == null)
                return new byte[minimumCapacityInBytes];

            lock (_storagePool)
            {
                for (int i = 0; i < _storagePool.Count; i++)
                {
                    var array = _storagePool[i];
                    if (array != null && array.Length >= minimumCapacityInBytes)
                    {
                        _storagePool[i] = null;
                        _bytesInPool -= array.Length;
                        return array;
                    }
                }
            }
            Statistics._totalBytesAllocated += minimumCapacityInBytes;
            return new byte[minimumCapacityInBytes];
        }
    }
}
