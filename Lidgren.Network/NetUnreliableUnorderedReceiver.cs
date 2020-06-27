
namespace Lidgren.Network
{
	internal sealed class NetUnreliableUnorderedReceiver : NetReceiverChannel
	{
		public NetUnreliableUnorderedReceiver(NetConnection connection)
			: base(connection)
		{
		}

		public override void ReceiveMessage(NetIncomingMessage msg)
		{
			// ack no matter what
			Connection.QueueAck(msg._baseMessageType, msg.SequenceNumber);

			Peer.ReleaseMessage(msg);
		}
	}
}
