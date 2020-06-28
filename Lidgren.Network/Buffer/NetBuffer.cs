using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lidgren.Network
{
    public partial class NetBuffer
    {
        /// <summary>
        /// Number of extra bytes to overallocate for message buffers to avoid resizing.
        /// </summary>
        protected const int ExtraGrowAmount = 32; // TODO: move to config

        // TODO: optimize reflection

        private static Dictionary<Type, MethodInfo> ReadMethods { get; } = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> WriteMethods { get; } = new Dictionary<Type, MethodInfo>();

        private int _bitPosition;
        private int _bitLength;

        /// <summary>
        /// Gets or sets the internal data buffer.
        /// </summary>
        [SuppressMessage("Performance", "CA1819", Justification = "<Pending>")]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the position within the buffer in bits.
        /// </summary>
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

        /// <summary>
        /// Gets the position within the buffer in bytes.
        /// </summary>
        /// <remarks>
        /// The bits of the first returned byte may already have been read - 
        /// check <see cref="BitPosition"/> to be sure.
        /// </remarks>
        public int BytePosition => BitPosition / 8;

        /// <summary>
        /// Gets whether <see cref="BitPosition"/> is byte-aligned and contains no stray bits.
        /// </summary>
        public bool IsByteAligned => BitPosition % 8 == 0;

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bits.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bytes.
        /// </summary>
        public int ByteLength
        {
            get => NetBitWriter.ByteCountForBits(_bitLength);
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value > ByteCapacity)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _bitLength = value * 8;
            }
        }

        public int BitCapacity
        {
            get => Data.Length * 8;
            set => ByteCapacity = NetBitWriter.ByteCountForBits(value);
        }

        public int ByteCapacity
        {
            get => Data.Length;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (Data.Length != value)
                {
                    var newBuffer = new byte[value];
                    Data.AsMemory(0, ByteLength).CopyTo(newBuffer);
                    Data = newBuffer;
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

        public void IncrementBitPosition(int bitCount)
        {
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            _bitPosition += bitCount;
            SetLengthByPosition();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetLengthByPosition()
        {
            if (_bitLength < BitPosition)
                _bitLength = BitPosition;
        }
    }
}
