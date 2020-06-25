using System;

namespace Lidgren.Network
{
    public class CannotResendException : ArgumentException
    {
        private const string DefaultMessage =
            "The message has already been sent. " +
            "Use other methods to send to multiple recipients efficiently.";

        public CannotResendException() : base(DefaultMessage)
        {
        }

        public CannotResendException(string? paramName) : base(DefaultMessage, paramName)
        {
        }

        public CannotResendException(string? message, string? paramName) : base(PassMessage(message), paramName)
        {
        }

        public CannotResendException(string? message, Exception? innerException) : base(PassMessage(message), innerException)
        {
        }
        
        public CannotResendException(string? message, string? paramName, Exception? innerException) :
            base(PassMessage(message), paramName, innerException)
        {

        }

        private static string PassMessage(string? message)
        {
            return string.IsNullOrEmpty(message) ? DefaultMessage : message;
        }
    }
}
