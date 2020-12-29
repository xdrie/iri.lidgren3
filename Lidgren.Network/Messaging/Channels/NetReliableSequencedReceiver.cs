﻿
namespace Lidgren.Network
{
	internal sealed class NetReliableSequencedReceiver : NetReceiverChannel
	{
		private int _windowStart;
		private int _windowSize;

		public NetReliableSequencedReceiver(NetConnection connection, int windowSize)
			: base(connection)
		{
			_windowSize = windowSize;
		}

		private void AdvanceWindow()
		{
			_windowStart = (_windowStart + 1) % NetConstants.SequenceNumbers;
		}

		public override void ReceiveMessage(NetIncomingMessage message)
		{
			int nr = message.SequenceNumber;

			int relate = NetUtility.RelativeSequenceNumber(nr, _windowStart);

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
			if (relate > _windowSize)
			{
				// too early message!
				Peer.LogDebug("Received " + message + " TOO EARLY! Expected " + _windowStart);
				return;
			}

			// ok
			_windowStart = (_windowStart + relate) % NetConstants.SequenceNumbers;
			Peer.ReleaseMessage(message);
			return;
		}
	}
}
