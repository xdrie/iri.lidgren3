using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        public void SendToAll(NetOutgoingMessage msg, NetDeliveryMethod method)
        {
            var all = this.Connections;
            if (all.Count > 0)
                SendMessage(msg, all, method, 0);
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
			var all = this.Connections;
			if (all.Count <= 0)
				return;

            if (except == null)
            {
                SendMessage(msg, all, method, sequenceChannel);
            }
            else
            {
                if (all.Count > 1) {
                    IEnumerable<NetConnection> Exclude()
                    {
                        foreach (var conn in all)
                            if (conn != except)
                                yield return conn;
                    }
                    
                    var recipients = new FakeCollection(Exclude(), all.Count - 1);
                    SendMessage(msg, recipients, method, sequenceChannel);
                }
            }
		}
        
        /// <summary>
        /// Returns a string that represents this object
        /// </summary>
        public override string ToString()
		{
			return "[NetServer " + ConnectionsCount + " connections]";
		}
        
        struct FakeCollection : ICollection<NetConnection>
        {
            private IEnumerable<NetConnection> _enumerable;
            private int _count;

            public FakeCollection(IEnumerable<NetConnection> enumerable, int count)
            {
                _enumerable = enumerable;
                _count = count;
            }

            public int Count => _count;
            public bool IsReadOnly => true;

            public void Add(NetConnection item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(NetConnection item) => throw new NotImplementedException();
            public void CopyTo(NetConnection[] array, int arrayIndex) => throw new NotImplementedException();
            public bool Remove(NetConnection item) => throw new NotImplementedException();

            public IEnumerator<NetConnection> GetEnumerator() => _enumerable.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
