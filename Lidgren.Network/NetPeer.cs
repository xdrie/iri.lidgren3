using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;

#if !__NOIPENDPOINT__
using NetEndPoint = System.Net.IPEndPoint;
#endif

namespace Lidgren.Network
{
	/// <summary>
	/// Represents a local peer capable of holding zero, one or more connections to remote peers.
	/// </summary>
	public partial class NetPeer
	{
		private static int s_initializedPeersCount;

		private int m_listenPort;
        private object m_messageReceivedEventCreationLock = new object();

		internal readonly List<NetConnection> m_connections;
		private readonly Dictionary<NetEndPoint, NetConnection> m_connectionLookup;

		private string m_shutdownReason;

        /// <summary>
        /// Gets the <see cref="NetPeerStatus"/> of the <see cref="NetPeer"/>.
        /// </summary>
        public NetPeerStatus Status => m_status;

        /// <summary>
        /// Signalling event which can be waited on to determine when a message is queued for reading.
        /// Note that there is no guarantee that after the event is signaled the blocked thread will 
        /// find the message in the queue. Other user created threads could be preempted and dequeue 
        /// the message before the waiting thread wakes up.
        /// </summary>
        public AutoResetEvent MessageReceivedEvent
		{
			get
			{
				if (m_messageReceivedEvent == null)
				{
					lock (m_messageReceivedEventCreationLock) // make sure we don't create more than one event object
					{
						if (m_messageReceivedEvent == null)
							m_messageReceivedEvent = new AutoResetEvent(false);
					}
				}
				return m_messageReceivedEvent;
			}
		}

        /// <summary>
        /// Gets a unique identifier for this <see cref="NetPeer"/> based on Mac address and IP/port. 
        /// <para>Note: Not available until <see cref="Start"/> has been called.</para>
        /// </summary>
        public long UniqueIdentifier => m_uniqueIdentifier;

        /// <summary>
        /// Gets the port number this <see cref="NetPeer"/> is listening and sending on,
        /// if <see cref="Start"/> has been called.
        /// </summary>
        public int Port => m_listenPort;

        /// <summary>
        /// Returns an <see cref="NetUPnP"/> object if enabled in the <see cref="NetPeerConfiguration"/>.
        /// </summary>
        public NetUPnP UPnP => m_upnp;

        /// <summary>
        /// Gets or sets the application defined object containing data about the peer.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Gets a list of the current connections.
        /// The <see cref="List{T}"/> is rented from the 
        /// <see cref="NetConnectionListPool"/> and recycling it is advised.
        /// </summary>
        public List<NetConnection> GetConnections()
        {
            var list = NetConnectionListPool.Rent();
            lock (m_connections)
            {
                foreach (var conn in m_connections)
                    list.Add(conn);
            }
            return list;
        }

        /// <summary>
        /// Gets the number of active connections.
        /// </summary>
        public int ConnectionCount => m_connections.Count;

        /// <summary>
        /// Statistics on this <see cref="NetPeer"/> since it was initialized.
        /// </summary>
        public NetPeerStatistics Statistics => m_statistics;

        /// <summary>
        /// Gets the configuration used to instantiate this <see cref="NetPeer"/>.
        /// </summary>
        public NetPeerConfiguration Configuration => m_configuration;

        /// <summary>
        /// NetPeer constructor.
        /// </summary>
        public NetPeer(NetPeerConfiguration config)
		{
			m_configuration = config;
			m_statistics = new NetPeerStatistics(this);
			m_releasedIncomingMessages = new NetQueue<NetIncomingMessage>(8);
			m_unsentUnconnectedMessages = new NetQueue<NetTuple<NetEndPoint, NetOutgoingMessage>>(4);
			m_connections = new List<NetConnection>();
			m_connectionLookup = new Dictionary<NetEndPoint, NetConnection>();
			m_handshakes = new Dictionary<NetEndPoint, NetConnection>();
            var address = config.DualStack ? IPAddress.IPv6Any : IPAddress.Any;
            m_senderRemote = new NetEndPoint(address, 0);
			m_status = NetPeerStatus.NotRunning;
			m_receivedFragmentGroups = new Dictionary<NetConnection, Dictionary<int, ReceivedFragmentGroup>>();	
		}

		/// <summary>
		/// Binds to socket and spawns the networking thread.
		/// </summary>
		public void Start()
		{
			if (m_status != NetPeerStatus.NotRunning)
			{
				// already running! Just ignore...
				LogWarning("Start() called on already running NetPeer - ignoring.");
				return;
			}

			m_status = NetPeerStatus.Starting;

			// fix network thread name
			if (m_configuration.NetworkThreadName == "Lidgren network thread")
			{
				int pc = Interlocked.Increment(ref s_initializedPeersCount);
				m_configuration.NetworkThreadName = "Lidgren network thread " + pc.ToString();
			}

			InitializeNetwork();
			
			// start network thread
			m_networkThread = new Thread(new ThreadStart(NetworkLoop));
			m_networkThread.Name = m_configuration.NetworkThreadName;
			m_networkThread.IsBackground = true;
			m_networkThread.Start();

			// send upnp discovery
			if (m_upnp != null)
				m_upnp.Discover(this);

			// allow some time for network thread to start up in case they call Connect() or UPnP calls immediately
			Thread.Sleep(50);
		}

		/// <summary>
		/// Gets the connection for a certain remote endpoint. Can return <see langword="null"/>.
		/// </summary>
		public NetConnection GetConnection(NetEndPoint ep)
		{

            // this should not pose a threading problem, m_connectionLookup is never added to concurrently
            // and TryGetValue will not throw an exception on fail, only yield null, which is acceptable
            m_connectionLookup.TryGetValue(ep, out NetConnection retval);

            return retval;
		}

		/// <summary>
		/// Read a pending message from any connection, 
        /// blocking up to <paramref name="maxMillis"/> if needed.
		/// </summary>
		public NetIncomingMessage WaitMessage(int maxMillis)
		{
			var msg = ReadMessage();
			if (msg != null)
				return msg; // no need to wait; we already have a message to deliver
			var msgEvt = MessageReceivedEvent;
			msgEvt.WaitOne(maxMillis);
			return ReadMessage();
		}

		/// <summary>
		/// Read a pending message from any connection, if any.
		/// </summary>
		public NetIncomingMessage ReadMessage()
		{
            if (m_releasedIncomingMessages.TryDequeue(out NetIncomingMessage retval))
            {
                if (retval.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    NetConnectionStatus status = (NetConnectionStatus)retval.PeekByte();
                    retval.SenderConnection.m_visibleStatus = status;
                }
            }
            return retval;
		}

		/// <summary>
		/// Read a pending message from any connection, if any.
		/// </summary>
		public int ReadMessages(IList<NetIncomingMessage> addTo)
		{
			int added = m_releasedIncomingMessages.TryDrain(addTo);
			if (added > 0)
			{
				for (int i = 0; i < added; i++)
				{
					var index = addTo.Count - added + i;
					var nim = addTo[index];
					if (nim.MessageType == NetIncomingMessageType.StatusChanged)
					{
						NetConnectionStatus status = (NetConnectionStatus)nim.PeekByte();
						nim.SenderConnection.m_visibleStatus = status;
					}
				}
			}
			return added;
		}

		// send message immediately
		internal void SendLibrary(NetOutgoingMessage msg, NetEndPoint recipient)
		{
			VerifyNetworkThread();
			NetException.Assert(msg.m_isSent == false);

            int len = msg.Encode(m_sendBuffer, 0, 0);
            SendPacket(len, recipient, 1, out _);
		}

		/// <summary>
		/// Create a connection to a remote endpoint.
		/// </summary>
		public NetConnection Connect(string host, int port)
		{
			return Connect(new NetEndPoint(NetUtility.Resolve(host), port), null);
		}

		/// <summary>
		/// Create a connection to a remote endpoint.
		/// </summary>
		public NetConnection Connect(string host, int port, NetOutgoingMessage hailMessage)
		{
			return Connect(new NetEndPoint(NetUtility.Resolve(host), port), hailMessage);
		}

		/// <summary>
		/// Create a connection to a remote endpoint
		/// </summary>
		public NetConnection Connect(NetEndPoint remoteEndPoint)
		{
			return Connect(remoteEndPoint, null);
		}

		/// <summary>
		/// Create a connection to a remote endpoint.
		/// </summary>
		public virtual NetConnection Connect(NetEndPoint remoteEndPoint, NetOutgoingMessage hailMessage)
		{
			if (remoteEndPoint == null)
				throw new ArgumentNullException("remoteEndPoint");
            if(m_configuration.DualStack)
                remoteEndPoint = NetUtility.MapToIPv6(remoteEndPoint);

			lock (m_connections)
			{
				if (m_status == NetPeerStatus.NotRunning)
					throw new NetException("Must call Start() first");

				if (m_connectionLookup.ContainsKey(remoteEndPoint))
					throw new NetException("Already connected to that endpoint!");

                if (m_handshakes.TryGetValue(remoteEndPoint, out NetConnection hs))
                {
                    // already trying to connect to that endpoint; make another try
                    switch (hs.m_status)
                    {
                        case NetConnectionStatus.InitiatedConnect:
                            // send another connect
                            hs.m_connectRequested = true;
                            break;
                        case NetConnectionStatus.RespondedConnect:
                            // send another response
                            hs.SendConnectResponse((float)NetTime.Now, false);
                            break;
                        default:
                            // weird
                            LogWarning("Weird situation; Connect() already in progress to remote endpoint; but hs status is " + hs.m_status);
                            break;
                    }
                    return hs;
                }

                NetConnection conn = new NetConnection(this, remoteEndPoint);
				conn.m_status = NetConnectionStatus.InitiatedConnect;
				conn.m_localHailMessage = hailMessage;

				// handle on network thread
				conn.m_connectRequested = true;
				conn.m_connectionInitiator = true;

				m_handshakes.Add(remoteEndPoint, conn);

				return conn;
			}
		}

		/// <summary>
		/// Send raw bytes; only used for debugging.
		/// </summary>
		public void RawSend(byte[] arr, int offset, int length, NetEndPoint destination)
	{
			// wrong thread - this miiiight crash with network thread... but what's a boy to do.
			Array.Copy(arr, offset, m_sendBuffer, 0, length);
            SendPacket(length, destination, 1, out _);
        }

		/// <summary>
		/// In DEBUG, throws an exception, in RELEASE logs an error message
		/// </summary>
		/// <param name="message"></param>
		internal void ThrowOrLog(string message)
		{
#if DEBUG
			throw new NetException(message);
#else
			LogError(message);
#endif
		}

		/// <summary>
		/// Disconnects all active connections and closes the socket.
		/// </summary>
		public void Shutdown(string reason)
		{
			// called on user thread
			if (m_socket == null)
				return; // already shut down

			LogDebug("Shutdown requested");
			m_shutdownReason = reason;
			m_status = NetPeerStatus.ShutdownRequested;
		}
	}
}
