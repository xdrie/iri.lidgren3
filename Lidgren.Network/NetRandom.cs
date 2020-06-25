using System;
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
    /// <summary>
    /// <see cref="NetRandom"/> base class.
    /// </summary>
    public abstract class NetRandom : Random
    {
        private const double RealUnitInt = 1.0 / (int.MaxValue + 1.0);

        /// <summary>
        /// Get global instance of <see cref="NetRandom "/> (uses <see cref="MWCRandom"/>),
        /// </summary>
        public static NetRandom Global { get; } = new MWCRandom();

        private uint _boolBuffer;
        private int _nextBoolIndex;

        /// <summary>
        /// Constructor with randomized seed.
        /// </summary>
        public NetRandom()
        {
            Initialize((int)NetRandomSeed.GetUInt32());
        }

        /// <summary>
        /// Constructor with provided 32-bit seed.
        /// </summary>
        public NetRandom(int seed)
        {
            Initialize(seed);
        }

        /// <summary>
        /// Initialize this instance with provided 32-bit seed.
        /// </summary>
        public abstract void Initialize(int seed);

        /// <summary>
        /// Generates a random value from <see cref="uint.MinValue"/> 
        /// to <see cref="uint.MaxValue"/>, inclusively.
        /// </summary>
        [CLSCompliant(false)]
        public virtual uint NextUInt32()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates a random value that is greater or equal than 
        /// zero and less than <see cref="int.MaxValue"/>.
        /// </summary>
        public override int Next()
        {
            var retval = (int)(0x7FFFFFFF & NextUInt32());
            if (retval == 0x7FFFFFFF)
                return NextInt32();
            return retval;
        }

        /// <summary>
        /// Generates a random value greater or equal than 
        /// zero and less or equal than <see cref="int.MaxValue"/> (inclusively)-
        /// </summary>
        public int NextInt32()
        {
            return (int)(0x7FFFFFFF & NextUInt32());
        }

        /// <summary>
        /// Returns random value larger or equal to 0.0 and less than 1.0
        /// </summary>
        public override double NextDouble()
        {
            return RealUnitInt * NextInt32();
        }

        /// <summary>
        /// Returns random value is greater or equal than 0.0 and less than 1.0.
        /// </summary>
        protected override double Sample()
        {
            return RealUnitInt * NextInt32();
        }

        /// <summary>
        /// Returns random value is greater or equal than 0f and less than 1f.
        /// </summary>
        public float NextSingle()
        {
            var retval = (float)(RealUnitInt * NextInt32());
            if (retval == 1f)
                return NextSingle();
            return retval;
        }

        /// <summary>
        /// Returns a random value is greater or equal to
        /// 0 and less than <paramref name="maxValue"/>.
        /// </summary>
        public override int Next(int maxValue)
        {
            return (int)(NextDouble() * maxValue);
        }

        /// <summary>
        /// Returns a random value is greater or equal to 
        /// <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
        /// </summary>
        public override int Next(int minValue, int maxValue)
        {
            return minValue + (int)(NextDouble() * (maxValue - minValue));
        }

        /// <summary>
        /// Generates a random value between
        /// <see cref="ulong.MinValue"/> to <see cref="ulong.MaxValue"/>.
        /// </summary>
        [CLSCompliant(false)]
        public ulong NextUInt64()
        {
            ulong retval = NextUInt32();
            retval |= NextUInt32() << 32;
            return retval;
        }

        /// <summary>
        /// Returns <see langword="true"/> or <see langword="false"/> randomly.
        /// </summary>
        public bool NextBool()
        {
            if (_nextBoolIndex >= 32)
            {
                _boolBuffer = NextUInt32();
                _nextBoolIndex = 1;
            }

            bool retval = ((_boolBuffer >> _nextBoolIndex) & 1) == 1;
            _nextBoolIndex++;
            return retval;
        }

        public override void NextBytes(Span<byte> buffer)
        {
            var ints = MemoryMarshal.Cast<byte, uint>(buffer);
            for (int i = 0; i < ints.Length; i++)
                ints[i] = NextUInt32();

            buffer = buffer.Slice(ints.Length * sizeof(uint));

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)NextUInt32();
        }

        public override void NextBytes(byte[] buffer)
        {
            NextBytes(buffer.AsSpan());
        }
    }
}
