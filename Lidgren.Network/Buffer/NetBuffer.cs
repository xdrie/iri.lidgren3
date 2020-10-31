using System;
using System.Buffers;
using System.Text;

namespace Lidgren.Network
{
    public class NetBuffer : IBitBuffer
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
        private ArrayPool<byte> _storagePool;
        private byte[] _buffer;
        private bool _recycleData;
        private bool _isDisposed;

        public int BitPosition
        {
            get => _bitPosition;
            set => _bitPosition = value;
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
                if (_bitPosition > _bitLength)
                    _bitPosition = _bitLength;
            }
        }

        public int ByteLength
        {
            get => NetBitWriter.BytesForBits(_bitLength);
            set => BitLength = value * 8;
        }

        public int BitCapacity
        {
            get => _buffer.Length * 8;
            set => ByteCapacity = NetBitWriter.BytesForBits(value);
        }

        public int ByteCapacity
        {
            get => _buffer.Length;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value > _buffer.Length)
                {
                    var newBuffer = _storagePool.Rent(value);
                    _buffer.AsMemory(0, ByteLength).CopyTo(newBuffer);
                    SetBuffer(newBuffer);
                }
            }
        }

        public NetBuffer(ArrayPool<byte> storagePool)
        {
            _storagePool = storagePool ?? throw new ArgumentNullException(nameof(storagePool));
            _buffer = Array.Empty<byte>();
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

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        public void SetBuffer(byte[] buffer, bool isRecyclable = true)
        {
            if (_recycleData)
                _storagePool.Return(_buffer);

            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _recycleData = buffer.Length > 0 && isRecyclable;
        }

        public void Trim()
        {
            if (_bitLength == 0)
            {
                if (_recycleData)
                {
                    _storagePool.Return(_buffer);
                    _buffer = Array.Empty<byte>();
                    _recycleData = false;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Trim();
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
