namespace SocketConnection
{
    public class ConnectionConfig
    {
        public ConnectionConfig(string serverIp, int serverPort)
        {
            IP = serverIp;
            Port = serverPort;
        }

        public string IP { get; set; }
        public int Port { get; set; }
    }
}
