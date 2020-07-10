using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Lidgren.Network
{
    public partial class NetBuffer : IBitBuffer
    {
        // TODO: rethink pooling (ArrayPool is probably the best candidate)
        //       implementing IDisposable for recycling/returning objects would also be wise

        public static Encoding StringEncoding { get; } = new UTF8Encoding(false, false);

        /// <summary>
        /// Number of extra bytes to overallocate for message buffers to avoid resizing.
        /// </summary>
        protected const int ExtraGrowAmount = 32; // TODO: move to config

        // TODO: optimize reflection

        private static Dictionary<Type, MethodInfo> ReadMethods { get; } = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> WriteMethods { get; } = new Dictionary<Type, MethodInfo>();

        private int _bitPosition;
        private int _bitLength;
        internal byte[] _data; // TODO: hide this

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
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value > BitCapacity)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _bitLength = value;
            }
        }

        public int ByteLength
        {
            get => NetBitWriter.ByteCountForBits(_bitLength);
            set => BitLength = value * 8;
        }

        public int BitCapacity
        {
            get => _data.Length * 8;
            set => ByteCapacity = NetBitWriter.ByteCountForBits(value);
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

        static NetBuffer()
        {
            var inMethods = typeof(NetIncomingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo method in inMethods)
            {
                if (method.GetParameters().Length == 0 &&
                    method.Name.StartsWith("Read", StringComparison.InvariantCulture) &&
                    method.Name.Substring(4) == method.ReturnType.Name)
                    ReadMethods[method.ReturnType] = method;
            }

            var outMethods = typeof(NetOutgoingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo method in outMethods)
            {
                if (method.Name.Equals("Write", StringComparison.InvariantCulture))
                {
                    ParameterInfo[] pis = method.GetParameters();
                    if (pis.Length == 1)
                        WriteMethods[pis[0].ParameterType] = method;
                }
            }
        }

        public NetBuffer(byte[]? buffer)
        {
            _data = buffer ?? Array.Empty<byte>();
        }

        public void EnsureBitCapacity(int bitCount)
        {
            int byteLength = NetBitWriter.ByteCountForBits(bitCount);
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
    }
}
