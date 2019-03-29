
namespace Lidgren.Network
{
    public partial class NetPeer
    {

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        public NetOutgoingMessage CreateMessage()
        {
            return CreateMessage(m_configuration.m_defaultOutgoingMessageCapacity);
        }

        /// <summary>
        /// Creates a new message for sending and writes the provided string to it
        /// </summary>
        public NetOutgoingMessage CreateMessage(string content)
        {
            var om = CreateMessage(2 + content.Length); // fair guess
            om.Write(content);
            return om;
        }

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        /// <param name="initialCapacity">initial capacity in bytes</param>
        public NetOutgoingMessage CreateMessage(int initialCapacity)
        {
            NetOutgoingMessage retval;
            if (m_outgoingMessagesPool == null || !m_outgoingMessagesPool.TryDequeue(out retval))
                retval = new NetOutgoingMessage();

            if (initialCapacity > 0)
                retval.m_data = GetStorage(initialCapacity);

            return retval;
        }

        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, byte[] useStorageData)
        {
            NetIncomingMessage retval;
            if (m_incomingMessagesPool == null || !m_incomingMessagesPool.TryDequeue(out retval))
                retval = new NetIncomingMessage(tp);
            else
                retval.m_incomingMessageType = tp;
            retval.m_data = useStorageData;
            return retval;
        }

        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, int minimumByteSize)
        {
            NetIncomingMessage retval;
            if (m_incomingMessagesPool == null || !m_incomingMessagesPool.TryDequeue(out retval))
                retval = new NetIncomingMessage(tp);
            else
                retval.m_incomingMessageType = tp;
            retval.m_data = GetStorage(minimumByteSize);
            return retval;
        }

        /// <summary>
        /// Creates an incoming message with the required capacity for releasing to the application
        /// </summary>
        internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, string text)
        {
            NetIncomingMessage retval;
            if (string.IsNullOrEmpty(text))
            {
                retval = CreateIncomingMessage(tp, 1);
                retval.Write(string.Empty);
                return retval;
            }

            int numBytes = System.Text.Encoding.UTF8.GetByteCount(text);
            retval = CreateIncomingMessage(tp, numBytes + (numBytes > 127 ? 2 : 1));
            retval.Write(text);

            return retval;
        }
    }
}
