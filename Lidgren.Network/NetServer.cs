using System.Collections.Generic;

namespace Lidgren.Network
{
    /// <summary>
    /// Specialized version of NetPeer used for "server" peers
    /// </summary>
    public class NetServer : NetPeer
	{
		/// <summary>
		/// NetServer constructor
		/// </summary>
		public NetServer(NetPeerConfiguration config) : base(config)
		{
			config.AcceptIncomingConnections = true;
		}

        /// <summary>
        /// Send a message to all connections
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">How to deliver the message</param>
		/// <param name="sequenceChannel">Which sequence channel to use for the message</param>
        public void SendToAll(NetOutgoingMessage msg, NetDeliveryMethod method, int sequenceChannel)
        {
            var all = this.GetConnections();
            if (all.Count > 0)
                SendMessage(msg, all, method, sequenceChannel);
            ConnectionListPool.Return(all);
        }

		/// <summary>
		/// Send a message to all connections except one
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <param name="method">How to deliver the message</param>
		/// <param name="except">Don't send to this particular connection</param>
		/// <param name="sequenceChannel">Which sequence channel to use for the message</param>
		public void SendToAll(NetOutgoingMessage msg, NetConnection except, NetDeliveryMethod method, int sequenceChannel)
		{
			var all = this.GetConnections();
            if (all.Count > 0)
            {
                if (except == null)
                {
                    SendMessage(msg, all, method, sequenceChannel);
                }
                else
                {
                    if (all.Count > 1)
                    {
                        List<NetConnection> Exclude()
                        {
                            var list = ConnectionListPool.Rent();
                            foreach (var conn in all)
                                if (conn != except)
                                    list.Add(conn);
                            return list;
                        }

                        var tmp = Exclude();
                        SendMessage(msg, tmp, method, sequenceChannel);
                        ConnectionListPool.Return(tmp);
                    }
                }
            }
            ConnectionListPool.Return(all);
        }
        
        /// <summary>
        /// Returns a string that represents this object
        /// </summary>
        public override string ToString()
		{
			return "[NetServer " + ConnectionCount + " connections]";
		}
    }
}
