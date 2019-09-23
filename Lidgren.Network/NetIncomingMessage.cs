/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Net;
using System.Diagnostics;

namespace Lidgren.Network
{
	/// <summary>
	/// Incoming message either sent from a remote peer or generated within the library.
	/// </summary>
	[DebuggerDisplay("Type={MessageType} LengthBits={LengthBits}")]
	public sealed class NetIncomingMessage : NetBuffer
	{
		internal NetIncomingMessageType m_incomingMessageType;
		internal IPEndPoint m_senderEndPoint;
		internal NetConnection m_senderConnection;
		internal int m_sequenceNumber;
		internal NetMessageType m_receivedMessageType;
		internal bool m_isFragment;
		internal double m_receiveTime;

        /// <summary>
        /// Gets the type of this incoming message.
        /// </summary>
        public NetIncomingMessageType MessageType => m_incomingMessageType;

        /// <summary>
        /// Gets the delivery method this message was sent with (if user data).
        /// </summary>
        public NetDeliveryMethod DeliveryMethod => NetUtility.GetDeliveryMethod(m_receivedMessageType);

        /// <summary>
        /// Gets the sequence channel this message was sent with (if user data).
        /// </summary>
        public int SequenceChannel => (int)m_receivedMessageType - (int)NetUtility.GetDeliveryMethod(m_receivedMessageType);

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> of the sender, if any.
        /// </summary>
        public IPEndPoint SenderEndPoint => m_senderEndPoint;

        /// <summary>
        /// Gets the <see cref="NetConnection"/> of the sender, if any.
        /// </summary>
        public NetConnection SenderConnection => m_senderConnection;

        /// <summary>
        /// Gets at what local time the message was received from the network.
        /// </summary>
        public double ReceiveTime => m_receiveTime;

        internal NetIncomingMessage()
		{
		}

		internal NetIncomingMessage(NetIncomingMessageType tp)
		{
			m_incomingMessageType = tp;
		}

		internal void Reset()
		{
			m_incomingMessageType = NetIncomingMessageType.Error;
			m_readPosition = 0;
			m_receivedMessageType = NetMessageType.LibraryError;
			m_senderConnection = null;
			m_bitLength = 0;
			m_isFragment = false;
		}

		/// <summary>
		/// Try to decrypt the message with the specified encryption algorithm.
		/// </summary>
		/// <param name="encryption">The encryption algorithm used to encrypt the message.</param>
		/// <returns>Whether the decryption succeeded.</returns>
		public bool Decrypt(NetEncryption encryption)
		{
			return encryption.Decrypt(this);
		}

        /// <summary>
        /// Reads a value, in local time comparable to <see cref="NetTime.Now"/>,
        /// written by <see cref="NetBuffer.WriteTime(bool)"/>.
        /// This requires a sender connection.
        /// </summary>
        public double ReadTime(bool highPrecision)
		{
			return ReadTime(m_senderConnection, highPrecision);
		}

		/// <summary>
		/// Returns a <see cref="string"/> that represents this object.
		/// </summary>
		public override string ToString()
		{
			return "[NetIncomingMessage #" + m_sequenceNumber + " " + LengthBytes + " bytes]";
		}
	}
}
