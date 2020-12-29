
namespace Lidgren.Network
{
	internal sealed class NetUnreliableUnorderedReceiver : NetReceiverChannel
	{
		public NetUnreliableUnorderedReceiver(NetConnection connection)
			: base(connection)
		{
		}

		public override void ReceiveMessage(NetIncomingMessage message)
		{
			// ack no matter what
			Connection.QueueAck(message._baseMessageType, message.SequenceNumber);

			Peer.ReleaseMessage(message);
		}
	}
}
