using SocketConnection.Hardware;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketConnection
{
    public partial class Form1 : Form
    {
        private TCPConnection _connection;
        private string IP = "192.168.3.132";
        private int digitalSampleCounter = 0;
        private int serialSampleCounter = 0;
        BlockingCollection<byte[]> digitalDataBuffer;
        BlockingCollection<byte[]> serialDataBuffer;
        Thread readSocket;
        Thread storeDigitalData;
        Thread storeSerialData;

        public Form1()
        {
            InitializeComponent();
            _connection = new TCPConnection(IP, 5000);
            digitalDataBuffer = new BlockingCollection<byte[]>();
            serialDataBuffer = new BlockingCollection<byte[]>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!_connection.IsConnected())
                MessageBox.Show("Connection failed");

            _connection.ReadSocketDataBuffer();
            storeDigitalData = new Thread(ReadDigitalDataBuffer);
            storeDigitalData.Start();

            storeSerialData = new Thread(ReadSerialDataBuffer);
            storeSerialData.Start();
        }

        private void ReadDigitalDataBuffer()
        {
            var task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    byte[] digitalData = _connection.ReadDigitalDataBuffer();
                    if (digitalData != null)
                        digitalDataBuffer.Add(digitalData);
                }
            });
        }

        private void ReadSerialDataBuffer()
        {
            var task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    byte[] serialData = _connection.ReadSerialDataBuffer();
                    if (serialData != null)
                        serialDataBuffer.Add(serialData);
                }
            });
        }

        private void btnWriteDigitalData_Click(object sender, EventArgs e)
        {
            GetDigitalData();
        }

        private void GetDigitalData()
        {
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    digitalDataBuffer.TryTake(out byte[] digitalBuffer);
                    UpdateTextBox(digitalBuffer, digitalSampleCounter);
                }
                catch
                { }
            });
        }

        private void btnWriteSeriallData_Click(object sender, EventArgs e)
        {
            GetSerialData();
        }

        private void GetSerialData()
        {
            try
            {
                serialDataBuffer.TryTake(out byte[] serialBuffer);
                UpdateTextBox(serialBuffer, serialSampleCounter);
            }
            catch
            { }
        }

        private void UpdateTextBox(byte[] digitalBuffer, int counter)
        {
            string text = "";
            if (digitalBuffer != null)
                text = $"Sample {++digitalSampleCounter}: {BitConverter.ToString(digitalBuffer)}\n";

            if (txtDataBuffer.InvokeRequired)
            {
                txtDataBuffer.Invoke(new Action<byte[], int>(UpdateTextBox), new object[] { digitalBuffer, counter });
            }
            else
            {
                if (digitalBuffer != null && digitalBuffer.Length > 0)
                {
                    foreach (byte b in digitalBuffer)
                    {
                        txtDataBuffer.Text += b.ToString("X2") + "    ";
                    }
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtDataBuffer.Clear();
        }

        private string ToString(byte[] byteArray)
        {
            string str = "";

            foreach (byte b in byteArray)
                str += b.ToString();

            return str;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            readSocket.Interrupt();
            storeDigitalData.Interrupt();
            storeSerialData.Interrupt();
        }
    }
}
