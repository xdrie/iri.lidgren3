
namespace Lidgren.Network
{
	/// <summary>
	/// Result of a SendMessage call
	/// </summary>
	public enum NetSendResult
	{
		/// <summary>
		/// Message failed to enqueue because there is no connection.
		/// </summary>
		FailedNotConnected = 0,

		/// <summary>
		/// No recipients were specified.
		/// </summary>
		NoRecipients = 1,

		/// <summary>
		/// Message was immediately sent.
		/// </summary>
		Sent = 2,

		/// <summary>
		/// Message was queued for delivery.
		/// </summary>
		Queued = 3,

		/// <summary>
		/// Message was dropped immediately since too many message were queued.
		/// </summary>
		Dropped = 4
	}
}
