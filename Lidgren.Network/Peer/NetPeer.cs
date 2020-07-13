using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Lidgren.Network
{
    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers.
    /// </summary>
    public partial class NetPeer : IDisposable
    {
        private static int _initializedPeersCount;

        private static TimeSpan MaxMessageReadWaitSlice { get; } = TimeSpan.FromMilliseconds(100);

        private bool _isDisposed;
        private string? _shutdownReason;

        private object MessageReceivedEventInitMutex { get; } = new object();

        private Dictionary<IPEndPoint, NetConnection> ConnectionLookup { get; }
            = new Dictionary<IPEndPoint, NetConnection>();

        internal List<NetConnection> Connections { get; }
            = new List<NetConnection>();

        /// <summary>
        /// Gets the <see cref="NetPeerStatus"/> of the <see cref="NetPeer"/>.
        /// </summary>
        public NetPeerStatus Status { get; private set; }

        /// <summary>
        /// Gets a unique identifier for this <see cref="NetPeer"/> based on IP/port and MAC address. 
        /// <para>Not available until <see cref="Start"/> has been called.</para>
        /// </summary>
        public long UniqueIdentifier { get; private set; }

        /// <summary>
        /// Gets the port number this <see cref="NetPeer"/> is listening and sending on,
        /// if <see cref="Start"/> has been called.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets a <see cref="NetUPnP"/> helper if enabled in the <see cref="NetPeerConfiguration"/>.
        /// </summary>
        public NetUPnP? UPnP { get; private set; }

        /// <summary>
        /// Gets or sets the application defined object containing data about the peer.
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// Gets the number of active connections.
        /// </summary>
        public int ConnectionCount => Connections.Count;

        /// <summary>
        /// Statistics on this <see cref="NetPeer"/> since it was initialized.
        /// </summary>
        public NetPeerStatistics Statistics { get; }

        /// <summary>
        /// Gets the configuration used to instantiate this <see cref="NetPeer"/>.
        /// </summary>
        public NetPeerConfiguration Configuration { get; }

        /// <summary>
        /// Signalling event which can be waited on to determine when a message may be queued for reading.
        /// </summary>
        /// <remarks>
        /// There is no guarantee that after the event is signaled the blocked thread will 
        /// find the message in the queue. Other user created threads could be preempted and dequeue 
        /// the message before the waiting thread wakes up.
        /// </remarks>
        public AutoResetEvent MessageReceivedEvent
        {
            get
            {
                if (_messageReceivedEvent == null)
                {
                    // make sure we don't create more than one event
                    lock (MessageReceivedEventInitMutex)
                    {
                        if (_messageReceivedEvent == null)
                            _messageReceivedEvent = new AutoResetEvent(false);
                    }
                }
                return _messageReceivedEvent;
            }
        }

        /// <summary>
        /// Constructs the peer with a given configuration.
        /// </summary>
        public NetPeer(NetPeerConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));

            if (Configuration.LocalAddress.AddressFamily == AddressFamily.InterNetworkV6)
                _senderRemote = new IPEndPoint(IPAddress.IPv6Any, 0);
            else
                _senderRemote = new IPEndPoint(IPAddress.Any, 0);

            Statistics = new NetPeerStatistics(this);
            Status = NetPeerStatus.NotRunning;
        }

        /// <summary>
        /// Appends the current connections to a collection.
        /// </summary>
        /// <param name="destination">The collection to which append connections.</param>
        /// <returns>The amount of connections appended.</returns>
        public int GetConnections(ICollection<NetConnection> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            lock (Connections)
            {
                foreach (var conn in Connections)
                    destination.Add(conn);
                return Connections.Count;
            }
        }

        /// <summary>
        /// Binds to socket and spawns the networking thread.
        /// </summary>
        public void Start()
        {
            if (Status != NetPeerStatus.NotRunning)
            {
                // already running! Just ignore...
                LogWarning("Start() called on already running NetPeer - ignoring.");
                return;
            }

            Status = NetPeerStatus.Starting;

            // fix network thread name
            if (Configuration.NetworkThreadName == "Lidgren.Network Thread")
            {
                int pc = Interlocked.Increment(ref _initializedPeersCount);
                Configuration.NetworkThreadName = "Lidgren.Network Thread " + pc.ToString(CultureInfo.InvariantCulture);
            }

            InitializeNetwork();

            // start network thread
            _networkThread = new Thread(new ThreadStart(NetworkLoop));
            _networkThread.Name = Configuration.NetworkThreadName;
            _networkThread.IsBackground = true;
            _networkThread.Start();

            // send upnp discovery
            UPnP?.Discover();
        }

        /// <summary>
        /// Gets the connection for a certain remote endpoint.
        /// </summary>
        public NetConnection? GetConnection(IPEndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));

            // this should not pose a threading problem, _connectionLookup is never added to concurrently
            // and TryGetValue will not throw an exception on fail, only yield null, which is acceptable
            ConnectionLookup.TryGetValue(endPoint, out NetConnection? retval);

            return retval;
        }

        private static void TryApplyConnectionStatus(NetIncomingMessage message)
        {
            if (message.SenderConnection == null)
                return;

            if (message.MessageType == NetIncomingMessageType.StatusChanged)
            {
                var status = message.PeekEnum<NetConnectionStatus>();
                message.SenderConnection.Status = status;
            }
        }

        /// <summary>
        /// Tries to read a pending message from any connection.
        /// </summary>
        /// <returns>Whether a message was successfully read.</returns>
        public bool TryReadMessage([MaybeNullWhen(false)] out NetIncomingMessage message)
        {
            if (ReleasedIncomingMessages.TryDequeue(out message))
            {
                TryApplyConnectionStatus(message);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to read a message from any connection, blocking if needed.
        /// </summary>
        /// <returns>Whether a message was successfully read.</returns>
        public bool TryReadMessage(
            TimeSpan timeout,
            [MaybeNullWhen(false)] out NetIncomingMessage message)
        {
            // check if we already have a message to return
            if (TryReadMessage(out message))
                return true;

            var resetEvent = MessageReceivedEvent;

            TryWait:
            while (timeout > TimeSpan.Zero)
            {
                var toWait = timeout < MaxMessageReadWaitSlice ? timeout : MaxMessageReadWaitSlice;
                if (ReleasedIncomingMessages.Count > 0 || resetEvent.WaitOne(toWait))
                    break;
                timeout -= toWait;
            }

            // Can happen when multiple threads read the same peer.
            // It's probably best to go back and wait again if we have leftover time.
            if (ReleasedIncomingMessages.Count == 0 &&
                timeout > TimeSpan.Zero)
                goto TryWait;

            return TryReadMessage(out message);
        }

        /// <summary>
        /// Tries to read a message from any connection.
        /// </summary>
        /// <returns>Whether a message was successfully read.</returns>
        public bool TryReadMessage(
            int millisecondsTimeout,
            [MaybeNullWhen(false)] out NetIncomingMessage message)
        {
            return TryReadMessage(TimeSpan.FromMilliseconds(millisecondsTimeout), out message);
        }

        /// <summary>
        /// Tries to read pending messages from any connection.
        /// </summary>
        /// <param name="destination">The collection to which append messages.</param>
        /// <returns>The amount of messages read.</returns>
        public int TryReadMessages(ICollection<NetIncomingMessage> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destination.IsReadOnly)
                throw new ArgumentException("The collection is read-only.");

            return ReleasedIncomingMessages.TryDrain(destination, onItem: TryApplyConnectionStatus);
        }

        // send message immediately
        internal void SendLibraryMessage(NetOutgoingMessage message, IPEndPoint recipient)
        {
            AssertIsOnLibraryThread();
            LidgrenException.Assert(!message._isSent);

            int len = message.Encode(_sendBuffer, 0, 0);
            SendPacket(len, recipient, 1, out _);
        }

        /// <summary>
        /// Send raw bytes; only used for debugging. 
        /// </summary>
        public void RawSend(byte[] buffer, int offset, int length, IPEndPoint destination)
        {
            // wrong thread might crash with network thread
            Array.Copy(buffer, offset, _sendBuffer, 0, length);
            SendPacket(length, destination, 1, out _);
        }

        /// <summary>
        /// In DEBUG, throws an exception, in RELEASE logs an error message.
        /// </summary>
        [SuppressMessage("Performance", "CA1822", Justification = "Contains compiler conditionals.")]
        internal void ThrowOrLog(string message)
        {
#if DEBUG
            throw new LidgrenException(message);
#else
            LogError(message);
#endif
        }

        /// <summary>
        /// Disconnects all active connections and closes the socket.
        /// </summary>
        public void Shutdown(string? reason)
        {
            // called on user thread
            if (Socket == null)
                return; // already shut down

            LogDebug("Shutdown requested");

            _shutdownReason = reason;
            Status = NetPeerStatus.ShutdownRequested;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _messageReceivedEvent?.Dispose();
                    _outgoingMessagePool?.Dispose();
                    _incomingMessagePool?.Dispose();
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
