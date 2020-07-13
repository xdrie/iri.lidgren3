using System;

namespace Lidgren.Network
{
    /// <summary>
    /// Specialized version of a peer used for "server" peers.
    /// It accepts incoming connections and maintains <see cref="NetConnection"/>s with clients.
    /// </summary>
    public class NetServer : NetPeer
    {
        /// <summary>
        /// Constructs the server with a given configuration.
        /// </summary>
        public NetServer(NetPeerConfiguration config) : base(config)
        {
            config.AcceptIncomingConnections = true;
        }

        /// <summary>
        /// Send a message to all connections except one
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="except">Don't send to this particular connection</param>
        /// <param name="sequenceChannel">Which sequence channel to use for the message</param>
        public void SendToAll(
            NetOutgoingMessage message, NetConnection? except, NetDeliveryMethod method, int sequenceChannel)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var all = NetConnectionListPool.GetConnections(this);
            if (all == null)
            {
                if (!message._isSent)
                    Recycle(message);
                return;
            }

            try
            {
                if (except != null)
                    all.Remove(except);

                SendMessage(message, all, method, sequenceChannel);
            }
            finally
            {
                NetConnectionListPool.Return(all);
            }
        }

        /// <summary>
        /// Send a message to all connections
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Which sequence channel to use for the message</param>
        public void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method, int sequenceChannel)
        {
            SendToAll(message, except: null, method, sequenceChannel);
        }

        /// <summary>
        /// Returns a string that represents this object
        /// </summary>
        public override string ToString()
        {
            return "{NetServer: " + ConnectionCount + " connections}";
        }
    }
}
