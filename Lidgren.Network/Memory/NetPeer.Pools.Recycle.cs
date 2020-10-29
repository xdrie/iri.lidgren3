using System;
using System.Buffers;
using System.Collections.Generic;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        // TODO: use ArrayPool instead

        internal void Recycle(byte[] storage)
        {
            if (_storagePool == null || storage == null)
                return;

            //ArrayPool<byte>.Shared.Return(storage);
            //return;

            lock (_storagePool)
            {
                _bytesInPool += storage.Length;
                int count = _storagePool.Count;
                for (int i = 0; i < count; i++)
                {
                    if (_storagePool[i] == null)
                    {
                        _storagePool[i] = storage;
                        return;
                    }
                }
                _storagePool.Add(storage);
            }
        }

        // TODO: add api for accessing message _data

        /// <summary>
        /// Recycles a message for reuse; taking pressure off the garbage collector
        /// </summary>
        public void Recycle(NetIncomingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (_incomingMessagePool == null)
                return;

            LidgrenException.Assert(
                !_incomingMessagePool.Contains(message), "Recyling already recycled message! Thread race?");

            byte[] storage = message._data;
            message._data = Array.Empty<byte>();

            Recycle(storage);
            message.Reset();
            _incomingMessagePool.Enqueue(message);
        }

        /// <summary>
        /// Recycles a list of messages for reuse.
        /// </summary>
        public void Recycle(IEnumerable<NetIncomingMessage> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (_incomingMessagePool == null)
                return;

            // first recycle the storage of each message
            if (_storagePool != null)
            {
                lock (_storagePool)
                {
                    foreach (var message in messages)
                    {
                        byte[] storage = message._data;
                        message._data = Array.Empty<byte>();

                        _bytesInPool += storage.Length;
                        for (int i = 0; i < _storagePool.Count; i++)
                        {
                            if (_storagePool[i] == null)
                            {
                                _storagePool[i] = storage;
                                return;
                            }
                        }
                        message.Reset();
                        _storagePool.Add(storage);
                    }
                }
            }

            // then recycle the message objects
            _incomingMessagePool.Enqueue(messages);
        }

        internal void Recycle(NetOutgoingMessage message)
        {
            if (_outgoingMessagePool == null)
                return;

            LidgrenException.Assert(
                !_outgoingMessagePool.Contains(message), "Recyling already recycled message! Thread race?");

            byte[] storage = message._data;
            message._data = Array.Empty<byte>();

            // message fragments cannot be recycled
            // TODO: find a way to recycle large message after all fragments has been acknowledged;
            //       or? possibly better just to garbage collect them
            if (message._fragmentGroup == 0)
                Recycle(storage);

            message.Reset();
            _outgoingMessagePool.Enqueue(message);
        }
    }
}
