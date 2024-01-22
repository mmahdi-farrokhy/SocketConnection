namespace SocketConnection
{
    public interface SocketConnection
    {
        void StartConnection();
        void StopConnection();
        bool IsConnected();
        void ReadSocketDataBuffer();
        bool SendCommand(byte[] command);
    }
}
