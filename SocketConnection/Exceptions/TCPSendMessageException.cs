using System;
using System.Runtime.Serialization;

namespace SocketConnection.Hardware
{
    [Serializable]
    public class TCPSendMessageException : Exception
    {
        public TCPSendMessageException()
        {
        }

        public TCPSendMessageException(string message) : base(message)
        {
        }

        public TCPSendMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TCPSendMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}