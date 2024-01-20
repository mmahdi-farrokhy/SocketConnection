namespace SocketConnection
{
    public interface SocketConnection
    {
        void StartConnection();
        void StopConnection();
        bool IsConnected();
        void ListenForData();
        void SendCommand(byte[] command);
    }
}
