namespace SocketConnection
{
    public interface SocketConnection
    {
        void StartConnection();
        bool IsConnected();
        void ListenForData();
        void HandleCommand(byte[] commandData);
        void ProcessDigitalData(byte[] digitalData);
    }

}
