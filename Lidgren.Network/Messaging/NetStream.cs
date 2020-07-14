using System;
using System.IO;

namespace Lidgren.Network
{
    public class NetStream : Stream
    {
        private bool _readable;
        private bool _writable;

        public NetConnection Connection { get; }
        public int SequenceChannel { get; }

        public NetPeer Peer => Connection.Peer;

        public override bool CanRead => _readable;
        public override bool CanWrite => _writable;
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public NetStream(NetConnection connection, int sequenceChannel)
        {
            NetConstants.AssertValidSequenceChannel(
                NetDeliveryMethod.Stream, sequenceChannel, nameof(sequenceChannel));

            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            SequenceChannel = sequenceChannel;

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
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
    }
}
