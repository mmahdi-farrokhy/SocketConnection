using System.Collections.Generic;
using System.Linq;

namespace SocketConnection.Data
{
    public class SerialData : Sample
    {
        public SerialData(int length)
        {
            Length = length;
            Body = new byte[Length];
        }
            
        public override bool Equals(object obj)
        {
            return obj is SerialData command &&
                   Header.SequenceEqual(command.Header) &&
                   Body.SequenceEqual(command.Body) &&
                   Length == command.Length;
        }

        public override int GetHashCode()
        {
            int hashCode = 420707727;
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Header);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Body);
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }

        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}
