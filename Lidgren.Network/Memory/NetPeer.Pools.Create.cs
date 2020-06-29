using System.Text;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        public NetOutgoingMessage CreateMessage()
        {
            return CreateMessage(Configuration._defaultOutgoingMessageCapacity);
        }

        /// <summary>
        /// Creates a new message for sending and writes the provided string to it.
        /// </summary>
        public NetOutgoingMessage CreateMessage(string? content)
        {
            if (content == null)
                content = string.Empty;

            int strByteCount = NetBuffer.StringEncoding.GetByteCount(content);
            var om = strByteCount == 0 ? CreateMessage(1) : CreateMessage(2 + strByteCount);
            om.Write(content);
            return om;
        }

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        /// <param name="initialCapacity">initial capacity in bytes</param>
        public NetOutgoingMessage CreateMessage(int initialCapacity)
        {
            if (_outgoingMessagePool == null || 
                !_outgoingMessagePool.TryDequeue(out NetOutgoingMessage? retval))
                retval = new NetOutgoingMessage();

            if (initialCapacity > 0)
                retval.Data = GetStorage(initialCapacity);

            return retval;
        }

        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType type, byte[] buffer)
        {
            if (_incomingMessagePool == null ||
                !_incomingMessagePool.TryDequeue(out NetIncomingMessage? retval))
                retval = new NetIncomingMessage(type);
            else
                retval.MessageType = type;

            retval.Data = buffer;
            return retval;
        }

        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType type, int minimumByteSize)
        {
            if (_incomingMessagePool == null || 
                !_incomingMessagePool.TryDequeue(out NetIncomingMessage? retval))
                retval = new NetIncomingMessage(type);
            else
                retval.MessageType = type;

            retval.Data = GetStorage(minimumByteSize);
            return retval;
        }

        /// <summary>
        /// Creates an incoming message with the required capacity for releasing to the application.
        /// </summary>
        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType type, string text)
        {
            NetIncomingMessage msg;
            if (string.IsNullOrEmpty(text))
            {
                msg = CreateIncomingMessage(type, 1);
                msg.Write(string.Empty);
                msg.BitPosition = 0;
                return msg;
            }

            int strByteCount = NetBuffer.StringEncoding.GetMaxByteCount(text.Length);
            msg = CreateIncomingMessage(type, strByteCount + 10);
            msg.Write(text);
            msg.BitPosition = 0;

            return msg;
        }
    }
}
