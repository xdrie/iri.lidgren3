
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

            int strByteCount = NetBuffer.StringEncoding.GetMaxByteCount(content.Length);
            int minSize = NetBitWriter.GetVarIntSize(strByteCount);

            var om = CreateMessage(minSize + strByteCount);
            om.Write(content);
            return om;
        }

        // TODO: create api for accessing message _data

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        /// <param name="minimumByteCapacity">Minimum initial capacity in bytes.</param>
        public NetOutgoingMessage CreateMessage(int minimumByteCapacity)
        {
            if (_outgoingMessagePool == null ||
                !_outgoingMessagePool.TryDequeue(out NetOutgoingMessage? retval))
            {
                retval = new NetOutgoingMessage(null);
            }
            else
            {
                retval.BitPosition = 0;
                retval.BitLength = 0;
            }

            if (minimumByteCapacity > 0)
                retval._data = GetStorage(minimumByteCapacity);

            return retval;
        }

        internal NetIncomingMessage CreateIncomingMessage(byte[] buffer, NetIncomingMessageType type)
        {
            if (_incomingMessagePool == null ||
                !_incomingMessagePool.TryDequeue(out NetIncomingMessage? retval))
            {
                retval = new NetIncomingMessage(buffer, type);
            }
            else
            {
                retval.MessageType = type;

                retval._data = buffer;
                retval.BitPosition = 0;
                retval.BitLength = 0;
            }
            return retval;
        }

        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType type, int minimumByteCapacity)
        {
            if (_incomingMessagePool == null ||
                !_incomingMessagePool.TryDequeue(out NetIncomingMessage? msg))
                msg = new NetIncomingMessage(null, type);
            else
            {
                msg.MessageType = type;
                msg.BitPosition = 0;
                msg.BitLength = 0;
            }

            if (minimumByteCapacity > 0)
                msg._data = GetStorage(minimumByteCapacity);

            return msg;
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
