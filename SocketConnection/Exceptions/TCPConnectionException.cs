using System;
using System.Runtime.Serialization;

namespace SocketConnection.Hardware
{
    [Serializable]
    internal class TCPConnectionException : Exception
    {
        public TCPConnectionException()
        {
        }

        public TCPConnectionException(string message) : base(message)
        {
        }

        public TCPConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TCPConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}