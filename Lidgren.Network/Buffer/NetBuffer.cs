using System;
using System.Text;

namespace Lidgren.Network
{
    public partial class NetBuffer : IBitBuffer
    {
        // TODO: rethink pooling (ArrayPool is probably the best candidate)
        //       implementing IDisposable for recycling/returning objects would also be wise

        /// <summary>
        /// Number of extra bytes to overallocate for message buffers to avoid resizing.
        /// </summary>
        protected const int ExtraGrowAmount = 32; // TODO: move to config

        public static Encoding StringEncoding { get; } = new UTF8Encoding(false, false);

        private int _bitPosition;
        private int _bitLength;
        internal byte[] _data; // TODO: hide this
        private bool _isDisposed;

        public Span<byte> Span => _data.AsSpan();

        public int BitPosition
        {
            get => _bitPosition;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _bitPosition = value;
            }
        }

        public int BytePosition
        {
            get => BitPosition / 8;
            set => BitPosition = value * 8;
        }

        public int BitLength
        {
            get => _bitLength;
            set
            {
                EnsureBitCapacity(value);
                _bitLength = value;
            }
        }

        public int ByteLength
        {
            get => NetBitWriter.BytesForBits(_bitLength);
            set => BitLength = value * 8;
        }

        public int BitCapacity
        {
            get => _data.Length * 8;
            set => ByteCapacity = NetBitWriter.BytesForBits(value);
        }

        public int ByteCapacity
        {
            get => _data.Length;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (_data.Length != value)
                {
                    var newBuffer = new byte[value];
                    _data.AsMemory(0, ByteLength).CopyTo(newBuffer);
                    _data = newBuffer;
                }
            }
        }

        public NetBuffer(byte[]? buffer)
        {
            _data = buffer ?? Array.Empty<byte>();
        }

        public void EnsureBitCapacity(int bitCount)
        {
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            int byteLength = NetBitWriter.BytesForBits(bitCount);
            if (ByteCapacity < byteLength)
                ByteCapacity = byteLength + ExtraGrowAmount;
        }

        public void IncrementBitPosition(int bitCount)
        {
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            _bitPosition += bitCount;
            this.SetLengthByPosition();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
