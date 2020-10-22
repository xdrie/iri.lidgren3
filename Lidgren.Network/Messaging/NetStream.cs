using System;
using System.Collections.Generic;
using System.IO;

namespace Lidgren.Network
{
    public interface INetMessageProducerConsumer
    {
        NetConnection Connection { get; }

        bool TryReceive(NetIncomingMessage message);
        bool TrySend(out NetOutgoingMessage message);
    }

    public interface INetMessageScheduler
    {
        void RegisterSystem(INetMessageProducerConsumer system);
        void UnregisterSystem(INetMessageProducerConsumer system);
    }

    /// <summary>
    /// Acts as a default implementation of <see cref="INetMessageScheduler"/>.
    /// </summary>
    public class NetMessageScheduler : INetMessageScheduler
    {
        private Dictionary<NetConnection, INetMessageProducerConsumer> _systems =
            new Dictionary<NetConnection, INetMessageProducerConsumer>();

        public void RegisterSystem(INetMessageProducerConsumer system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            _systems.Add(system.Connection, system);
        }

        public void UnregisterSystem(INetMessageProducerConsumer system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (!_systems.Remove(system.Connection))
                throw new KeyNotFoundException();
        }
    }

    public class NetStream : Stream, INetMessageProducerConsumer
    {
        public delegate int StreamReadCallback(Span<byte> buffer);
        public delegate void StreamWriteCallback(ReadOnlySpan<byte> buffer);
        public delegate long StreamSeekCallback(long offset, SeekOrigin origin);

        public NetMessageScheduler Scheduler { get; }
        public NetConnection Connection { get; }
        public int SequenceChannel { get; }

        public StreamReadCallback? ReadCallback { get; }
        public StreamWriteCallback? WriteCallback { get; }
        public StreamSeekCallback? SeekCallback { get; }
        private Action? CloseCallback { get; }

        public bool IsDisposed { get; private set; }

        public NetPeer Peer => Connection.Peer;

        public override bool CanRead => ReadCallback != null;
        public override bool CanWrite => WriteCallback != null;
        public override bool CanSeek => SeekCallback != null;

        public override long Length => throw new InvalidOperationException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new InvalidOperationException();
        }

        public NetStream(
            NetMessageScheduler scheduler,
            NetConnection connection,
            int sequenceChannel,
            StreamReadCallback? readCallback,
            StreamWriteCallback? writeCallback,
            StreamSeekCallback? seekCallback,
            Action? closeCallback)
        {
            NetConstants.AssertValidDeliveryChannel(
                NetDeliveryMethod.Stream, sequenceChannel,
                null, nameof(sequenceChannel));

            if (readCallback == null && writeCallback == null)
                throw new ArgumentException("Both read and write callbacks are null.");

            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            SequenceChannel = sequenceChannel;
            ReadCallback = readCallback;
            WriteCallback = writeCallback;
            SeekCallback = seekCallback;
            CloseCallback = closeCallback;

            Scheduler.RegisterSystem(this);

            var createMessage = Peer.CreateMessage();
            createMessage.Write(CanRead);
            createMessage.Write(CanWrite);
            createMessage.Write(CanSeek);
            createMessage.Write(CanTimeout); // TODO:
            SendStreamMessage(createMessage);
        }

        public NetStream(
            NetConnection connection,
            int sequenceChannel,
            StreamReadCallback? readCallback,
            StreamWriteCallback? writeCallback,
            StreamSeekCallback? seekCallback,
            Action? closeCallback) 
            : this(
                connection?.Peer.DefaultScheduler!, connection!, sequenceChannel,
                readCallback, writeCallback, seekCallback, closeCallback)
        {

        }

        bool INetMessageProducerConsumer.TryReceive(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        bool INetMessageProducerConsumer.TrySend(out NetOutgoingMessage message)
        {
            throw new NotImplementedException();
        }

        private void WriteHeader()
        {

        }

        private void ReadHeader()
        {

        }

        public override int Read(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override int ReadByte()
        {
            Span<byte> tmp = stackalloc byte[1];
            if (Read(tmp) != tmp.Length)
                return -1;
            return tmp[0];
        }

        public override void WriteByte(byte value)
        {
            Write(stackalloc byte[] { value });
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private NetSendResult SendClose()
        {
            var message = Peer.CreateMessage();

            return SendStreamMessage(message);
        }

        private NetSendResult SendStreamMessage(NetOutgoingMessage message)
        {
            return Connection.SendMessage(message, NetDeliveryMethod.Stream, SequenceChannel);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!IsDisposed)
            {
                SendClose();
                Scheduler.UnregisterSystem(this);
                IsDisposed = true;
            }
        }
    }
}
