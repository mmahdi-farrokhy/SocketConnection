using System.Collections.Generic;
using System.Linq;

namespace SocketConnection.Data
{
    public class DigitalData : Sample
    {
        public int NumberOfActiveDataChannels { get; set; }

        public DigitalData()
        {
            Length = 17;
            Body = new byte[Length];
            NumberOfActiveDataChannels = 4;
        }

        public override bool Equals(object obj)
        {
            return obj is DigitalData data &&
                   Header.SequenceEqual(data.Header) &&
                   Body.SequenceEqual(data.Body);
        }

        public override int GetHashCode()
        {
            int hashCode = -306108907;
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Header);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Body);
            return hashCode;
        }

        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}
