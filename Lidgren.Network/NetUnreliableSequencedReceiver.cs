using System;

namespace Lidgren.Network
{
	internal sealed class NetUnreliableSequencedReceiver : NetReceiverChannel
	{
		private int m_lastReceivedSequenceNumber = -1;

		public NetUnreliableSequencedReceiver(NetConnection connection)
			: base(connection)
		{
		}

		public override void ReceiveMessage(NetIncomingMessage msg)
		{
			int nr = msg.SequenceNumber;

			// ack no matter what
			Connection.QueueAck(msg._baseMessageType, nr);

			int relate = NetUtility.RelativeSequenceNumber(nr, m_lastReceivedSequenceNumber + 1);
			if (relate < 0)
				return; // drop if late

			m_lastReceivedSequenceNumber = nr;
			Peer.ReleaseMessage(msg);
		}
	}
}
