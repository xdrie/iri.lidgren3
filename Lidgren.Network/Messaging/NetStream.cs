using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Lidgren.Network
{
    public enum NetStreamMessageType : byte
    {
        Open,
        Data,
        Pause,
        Resume,
        Close,
    }

    public class NetStream : Stream
    {
        public const int MaxDataHeaderSize = 6;

        public delegate void StreamDataRequest(NetStream stream, int amount);
        public delegate long StreamSeekRequest(NetStream stream, long offset, SeekOrigin origin);

        private Queue<byte[]> _readQueue;
        private byte[]? _readBuffer;
        private int _readBufferOffset;
        private bool _closed;
        private AutoResetEvent _readEvent = new AutoResetEvent(false);

        private byte[] _writeBuffer;
        private int _writeBufferOffset;

        public NetMessageScheduler Scheduler { get; }
        public NetConnection Connection { get; }
        public int Channel { get; }

        public bool IsDisposed { get; private set; }

        public NetPeer Peer => Connection.Peer;

        //public StreamDataRequest? ReadRequest { get; }
        //public StreamWriteRequest? WriteCallback { get; }
        //public StreamSeekRequest? SeekCallback { get; }
        //private Action? CloseCallback { get; }
        //
        //public override bool CanRead => ReadRequest != null;
        //public override bool CanWrite => WriteCallback != null;
        //public override bool CanSeek => SeekCallback != null;

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => true;

        public override long Length => throw new InvalidOperationException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new InvalidOperationException();
        }

        public NetStream(
            NetMessageScheduler scheduler,
            NetConnection connection,
            int sequenceChannel
            )//,
             //StreamDataRequest? readRequest,
             //StreamWriteRequest? writeRequest,
             //StreamSeekRequest? seekCallback,
             //Action? closeCallback))
        {
            NetConstants.AssertValidDeliveryChannel(
                NetDeliveryMethod.Stream, sequenceChannel,
                null, nameof(sequenceChannel));

            //if (readRequest == null && writeRequest == null)
            //    throw new ArgumentException("Both read and write callbacks are null.");

            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Channel = sequenceChannel;

            _readQueue = new Queue<byte[]>();

            _writeBuffer = new byte[GetBufferSize(Connection.CurrentMTU)];
            _writeBufferOffset = 0;

            //var createMessage = Peer.CreateMessage();
            //createMessage.Write(CanRead);
            //createMessage.Write(CanWrite);
            //createMessage.Write(CanSeek);
            //createMessage.Write(CanTimeout); // TODO:
            //SendStreamMessage(createMessage);

            var openMessage = Peer.CreateMessage();
            openMessage.Write((byte)NetStreamMessageType.Open);
            var result = SendStreamMessage(openMessage);
            // TODO: check result

        }

        //public NetStream(
        //    NetConnection connection,
        //    int sequenceChannel,
        //    StreamReadRequest? readCallback,
        //    StreamWriteRequest? writeCallback,
        //    StreamSeekRequest? seekCallback,
        //    Action? closeCallback) 
        //    : this(
        //        connection?.Peer.DefaultScheduler!, connection!, sequenceChannel,
        //        readCallback, writeCallback, seekCallback, closeCallback)
        //{
        //}

        public static int GetBufferSize(int mtu)
        {
            return Math.Max(1, mtu - MaxDataHeaderSize - NetConstants.HeaderSize);
        }

        internal void OnDataMessage(IBitBuffer buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            int length = buffer.ReadVarInt32();
            byte[] data = buffer.Read(length);

            lock (_readQueue)
                _readQueue.Enqueue(data);

            _readEvent.Set();
        }

        internal void OnCloseMessage(IBitBuffer buffer)
        {
            _closed = true;
            _readEvent.Set();
        }

        public override int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
                return 0;

            int read = TryRead(buffer);
            if (read != 0)
                return read;

            TryRead:
            if (_closed)
                return TryRead(buffer);

            _readEvent.WaitOne();
            read = TryRead(buffer);
            if (read == 0)
                goto TryRead;
            return read;
        }

        private int TryRead(Span<byte> buffer)
        {
            int read = 0;

            TryRead:
            if (_readBuffer != null)
            {
                var dataSlice = _readBuffer.AsSpan(_readBufferOffset);
                dataSlice = dataSlice.Slice(0, Math.Min(dataSlice.Length, buffer.Length));
                dataSlice.CopyTo(buffer);
                buffer = buffer.Slice(dataSlice.Length);
                read += dataSlice.Length;

                _readBufferOffset += dataSlice.Length;
                if (_readBufferOffset == _readBuffer.Length)
                {
                    _readBuffer = null;
                    _readBufferOffset = 0;
                }
            }

            if (_readBuffer == null)
            {
                lock (_readQueue)
                {
                    if (_readQueue.TryDequeue(out _readBuffer))
                        goto TryRead;
                }
            }
            return read;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                var space = _writeBuffer.AsSpan(_writeBufferOffset);
                if (space.IsEmpty)
                {
                    FlushCore();
                    continue;
                }

                var toCopy = buffer.Slice(0, Math.Min(buffer.Length, space.Length));
                toCopy.CopyTo(space);
                buffer = buffer.Slice(toCopy.Length);
                _writeBufferOffset += toCopy.Length;
            }
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
            if (_writeBufferOffset == 0)
                return;

            FlushCore();
        }

        private void FlushCore()
        {
            int length = _writeBufferOffset;

            var message = Peer.CreateMessage(length + 6);
            message.Write((byte)NetStreamMessageType.Data);
            message.WriteVar(length);
            message.Write(_writeBuffer.AsSpan(0, length));

            var result = SendStreamMessage(message);
            // TODO: check result

            _writeBufferOffset = 0;
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
            var message = Peer.CreateMessage(1);
            message.Write((byte)NetStreamMessageType.Close);
            return SendStreamMessage(message);
        }

        private NetSendResult SendStreamMessage(NetOutgoingMessage message)
        {
            return Connection.SendMessage(message, NetDeliveryMethod.Stream, Channel);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!IsDisposed)
            {
                Flush();

                var result = SendClose();
                // TODO: check result

                IsDisposed = true;
            }
        }
    }
}
