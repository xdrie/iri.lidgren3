using System;

namespace Lidgren.Network
{
    public class EndOfMessageException : LidgrenException
    {
        private const string DefaultMessage =
            "Attempt to read past the buffer. " +
            "Likely caused by mismatching Write/Reads, different size, or order.";

        public EndOfMessageException() : base(DefaultMessage)
        {
        }

        public EndOfMessageException(string? message) : base(PassMessage(message))
        {
        }

        public EndOfMessageException(string? message, Exception? innerException) : base(PassMessage(message), innerException)
        {
        }

        private static string PassMessage(string? message)
        {
            return string.IsNullOrEmpty(message) ? DefaultMessage : message;
        }
    }
}
