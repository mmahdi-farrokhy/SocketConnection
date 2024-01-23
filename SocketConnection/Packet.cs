namespace SocketConnection
{
    public static class Packet
    {
        private static int _hardwareChannelCount = 4;
        private static int _commandChannelCount = 8;
        private static int _samplePerPacket = 70;
        private static int _headerLength = 2;
        private static int _signalBufferCount = 70;
        private static int _commandBufferCount = 1;
        private static int _bodyLength = 2 * _commandChannelCount + 1;
        private static int _sampleLength = _headerLength + _bodyLength;
        private static byte _sampleInitializer = 0xFF;
        private static int _sampleRate = 128000;

        public static int SampleLength { get { return _sampleLength; } }
        public static int SignalBufferLimit { get { return _signalBufferCount; } }
        public static int CommandBufferLimit { get { return _commandBufferCount; } }
        public static int TotalPacketLength { get { return _sampleLength * _samplePerPacket; } }
        public static int SignalBufferLength { get { return _signalBufferCount * _sampleLength; } }
        public static int CommandBufferLength { get { return _commandBufferCount * _sampleLength; } }
        public static int SampleRate { get { return _sampleRate; } }
        public static byte SampleInitializer { get { return _sampleInitializer; } }
        public static byte StimCommandMarker { get { return 0xFA; } }
        public static byte HeadBoxCommandMarker { get { return 0xFB; } }
    }
}
