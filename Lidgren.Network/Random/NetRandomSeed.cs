using System;
using System.Threading;

namespace Lidgren.Network
{
	/// <summary>
	/// Helper for generating random integer numbers.
	/// </summary>
	public static class NetRandomSeed
	{
		private static int _seedIncrement = -1640531527;

		/// <summary>
		/// Generates a 32-bit random seed.
		/// </summary>
		[CLSCompliant(false)]
		public static uint GetUInt32()
		{
			ulong seed = GetUInt64();
			uint low = (uint)seed;
			uint high = (uint)(seed >> 32);
			return low ^ high;
		}

		/// <summary>
		/// Generates a 64-bit random seed.
		/// </summary>
		[CLSCompliant(false)]
		public static ulong GetUInt64()
		{
			// TODO: optimize

			ulong seed = (ulong)System.Diagnostics.Stopwatch.GetTimestamp();
			seed ^= (ulong)Environment.WorkingSet;
			ulong s2 = (ulong)Interlocked.Increment(ref _seedIncrement);
			s2 |= ((ulong)Guid.NewGuid().GetHashCode()) << 32;
			seed ^= s2;
			return seed;
		}
	}
}
