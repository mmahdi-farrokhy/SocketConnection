using System.Collections.Generic;
using System.Linq;

namespace SocketConnection.Data
{
    public class HardwareCommand : Sample
    {
        public HardwareCommandType Type { get; private set; }

        public HardwareCommand(byte commandTypeMarker, int length)
        {
            if (commandTypeMarker == 0xFA)
                Type = HardwareCommandType.UART_STIM;
            else if (commandTypeMarker == 0xFB)
                Type = HardwareCommandType.UART_HEADBOX;

            Length = length;
            Body = new byte[Length];
        }

        public override bool Equals(object obj)
        {
            return obj is HardwareCommand command &&
                   Header.SequenceEqual(command.Header) &&
                   Body.SequenceEqual(command.Body) &&
                   Type == command.Type &&
                   Length == command.Length;
        }

        public override int GetHashCode()
        {
            int hashCode = 420707727;
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Header);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Body);
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }

        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}
