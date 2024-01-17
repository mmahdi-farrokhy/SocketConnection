using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketConnection.Hardware.Tests
{
    [TestClass()]
    public class TCPConnectionShould
    {
        TCPConnection tcpConnection;

        [TestInitialize()]
        void Init()
        {
            //tcpConnection = new TCPConnection("192.168.3", 5000);
        }

        [TestMethod()]
        public void CheckIfADeviceInsideIPRange192_168_3IsConnected()
        {
            Assert.Fail();
            bool connected = tcpConnection.IsConnected();
            Assert.IsTrue(connected);
        }

        [TestMethod()]
        public void StartConnectionTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ListenForDataTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void HandleCommandTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ProcessDigitalDataTest()
        {
            Assert.Fail();
        }
    }
}