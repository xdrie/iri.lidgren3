
namespace Lidgren.Network
{
	internal sealed class NetReliableSequencedReceiver : NetReceiverChannel
	{
		private int m_windowStart;
		private int m_windowSize;

		public NetReliableSequencedReceiver(NetConnection connection, int windowSize)
			: base(connection)
		{
			m_windowSize = windowSize;
		}

		private void AdvanceWindow()
		{
			m_windowStart = (m_windowStart + 1) % NetConstants.NumSequenceNumbers;
		}

		public override void ReceiveMessage(NetIncomingMessage message)
		{
			int nr = message.SequenceNumber;

			int relate = NetUtility.RelativeSequenceNumber(nr, m_windowStart);

			// ack no matter what
			Connection.QueueAck(message._baseMessageType, nr);

			if (relate == 0)
			{
				// Log("Received message #" + message.SequenceNumber + " right on time");

				//
				// excellent, right on time
				//

				AdvanceWindow();
				Peer.ReleaseMessage(message);
				return;
			}

			if (relate < 0)
			{
				Peer.LogVerbose("Received message #" + message.SequenceNumber + " DROPPING LATE or DUPE");
				return;
			}

			// relate > 0 = early message
			if (relate > m_windowSize)
			{
				// too early message!
				Peer.LogDebug("Received " + message + " TOO EARLY! Expected " + m_windowStart);
				return;
			}

			// ok
			m_windowStart = (m_windowStart + relate) % NetConstants.NumSequenceNumbers;
			Peer.ReleaseMessage(message);
			return;
		}
	}
}
