namespace SocketConnection.Data
{
    public abstract class Sample
    {
        public byte[] Header { get; set; }
        public byte[] Body { get; set; }
        public int Length { get; set; }

        public abstract void Process();
    }
}
