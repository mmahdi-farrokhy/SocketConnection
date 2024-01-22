namespace SocketConnection
{
    public class Packet
    {
        public int DeviceActiveChannelsCount { get; set; } = 4;
        public int CommandChannelCount { get; set; }
        public int SampleCount { get; set; }
        public int HeaderLength { get; set; }
        public int BodyLength { get { return 2 * CommandChannelCount + 1; } }
        public int SentPacketStartLength { get; set; }
        public int TotalPacketLength { get { return HeaderLength + SampleCount * BodyLength; } }
        public int DecodedDataLength { get { return DeviceActiveChannelsCount * SampleCount + 1; } }
        public byte HeadBoxCommand { get; set; }
        public byte StimBoxCommand { get; set; }
    }
}
