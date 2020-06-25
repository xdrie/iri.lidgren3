using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Lidgren.Network
{
    public partial class NetBuffer
    {
        // TODO: move into config
        public static Encoding StringEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Number of extra bytes to overallocate for message buffers to avoid resizing.
        /// </summary>
        // TODO: move into config
        protected const int ExtraGrowAmount = 8;

        private static readonly Dictionary<Type, MethodInfo> _readMethods = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> _writeMethods = new Dictionary<Type, MethodInfo>();

        private int _bitLength;

        /// <summary>
        /// Gets or sets the internal data buffer.
        /// </summary>
        [SuppressMessage("Performance", "CA1819", Justification = "<Pending>")]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the position within the buffer in bits.
        /// </summary>
        public int BitPosition { get; set; }

        /// <summary>
        /// Gets whether <see cref="BitPosition"/> is byte-aligned and contains no stray bits.
        /// </summary>
        public bool IsByteAligned => BitPosition % 8 == 0;

        /// <summary>
        /// Gets the position within the buffer in bytes.
        /// </summary>
        /// <remarks>
        /// The bits of the first returned byte may already have been read - 
        /// check <see cref="BitPosition"/> to be sure.
        /// </remarks>
        public int BytePosition => BitPosition / 8;

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bits.
        /// </summary>
        public int BitLength
        {
            get => _bitLength;
            set
            {
                _bitLength = value;
                EnsureBufferSize(_bitLength, 0);
            }
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bytes.
        /// </summary>
        public int ByteLength
        {
            get => (_bitLength + 7) / 8;
            set
            {
                _bitLength = value * 8;
                EnsureBufferSize(_bitLength, 0);
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
                    _readMethods[method.ReturnType] = method;
            }

            var outMethods = typeof(NetOutgoingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo method in outMethods)
            {
                if (method.Name.Equals("Write", StringComparison.InvariantCulture))
                {
                    ParameterInfo[] pis = method.GetParameters();
                    if (pis.Length == 1)
                        _writeMethods[pis[0].ParameterType] = method;
                }
            }
        }
    }
}
