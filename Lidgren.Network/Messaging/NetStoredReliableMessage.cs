using System;

namespace Lidgren.Network
{
	internal struct NetStoredReliableMessage
	{
		public int SequenceNumber;
		public int NumSent;
		public TimeSpan LastSent;
		public NetOutgoingMessage? Message;

		public void Reset()
		{
			NumSent = default;
			LastSent = default;
			Message = default;
		}
	}
}
