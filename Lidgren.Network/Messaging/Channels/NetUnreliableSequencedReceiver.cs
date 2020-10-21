
namespace Lidgren.Network
{
	internal sealed class NetUnreliableSequencedReceiver : NetReceiverChannel
	{
		private int _lastReceivedSequenceNumber = -1;

		public NetUnreliableSequencedReceiver(NetConnection connection)
			: base(connection)
		{
		}

		public override void ReceiveMessage(NetIncomingMessage message)
		{
			int nr = message.SequenceNumber;

			// ack no matter what
			Connection.QueueAck(message._baseMessageType, nr);

			int relate = NetUtility.RelativeSequenceNumber(nr, _lastReceivedSequenceNumber + 1);
			if (relate < 0)
				return; // drop if late

			_lastReceivedSequenceNumber = nr;
			Peer.ReleaseMessage(message);
		}
	}
}
