using SocketConnection.Data;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketConnection.Hardware
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Net.Http;
    using System.Windows.Forms;

    public class TCPConnection : SocketConnection
    {
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private NetworkStream clientStream;
        private Queue<byte[]> packetQueue; // Queue to buffer incoming packets
        private string IP = "192.168.3.xxx";
        private int Port = 5000;
        private const int NUMBER_OF_ATTEMPTS = 5;
        private const int TIMEOUT = 5000;
        private const int SAMPLE_SIZE = 19;
        private const int SAMPLE_PER_PACKET = 70;
        private const int PACKET_SIZE = SAMPLE_SIZE * SAMPLE_PER_PACKET;
        private const byte SAMPLE_INITIALIZER = 0xFF;
        private const byte STIM_COMMAND_MARKER = 0xFA;
        private const byte HEAD_BOX_COMMAND_MARKER = 0xFB;

        public TCPConnection(string ip, int port)
        {
            IP = ip;
            this.Port = port;
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpClient = new TcpClient();
            packetQueue = new Queue<byte[]>();
        }

        public void StartConnection()
        {
            int attemptCounter = 0;

            while (attemptCounter < NUMBER_OF_ATTEMPTS)
            {
                try
                {
                    IAsyncResult result = tcpClient.BeginConnect(IP, Port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TIMEOUT, true);

                    if (success && tcpClient.Connected)
                    {
                        Console.WriteLine("Connection established.");
                        break;
                    }
                    else
                    {
                        tcpClient.Close();
                    }
                }
                catch
                {
                    attemptCounter++;
                }
            }

            if (attemptCounter >= NUMBER_OF_ATTEMPTS)
            {
                throw new TCPConnectionException($"Failed to connect after {NUMBER_OF_ATTEMPTS} attempts.");
            }
        }

        public bool IsConnected()
        {
            return tcpClient.Connected;
        }

        public byte[] ReadPacketFromSocket()
        {
            int adcDigitalDataLength = 1330;
            int uartSerialDataLength = 19;
            int restOfPacketLength;
            byte[] header = new byte[3];
            byte[] packet;
            byte[] fullPacket;

            tcpClient.GetStream().Read(header, 0, header.Length);

            if (header.SequenceEqual(new byte[] { 0XFF, 0XFF, 0XFA }) || header.SequenceEqual(new byte[] { 0XFF, 0XFF, 0XFB }))
            {
                restOfPacketLength = uartSerialDataLength - 3;
                fullPacket = new byte[uartSerialDataLength];
            }
            else
            {
                restOfPacketLength = adcDigitalDataLength - 3;
                fullPacket = new byte[adcDigitalDataLength];
            }

            packet = new byte[restOfPacketLength];
            tcpClient.GetStream().Read(packet, 0, packet.Length);
            Buffer.BlockCopy(header, 0, fullPacket, 0, header.Length);
            Buffer.BlockCopy(packet, 0, fullPacket, header.Length, packet.Length);
            return fullPacket;
        }

        public void ListenForData()
        {
            try
            {
                while (IsConnected())
                {
                    byte[] packet = new byte[PACKET_SIZE];
                    int bytesRead = clientStream.Read(packet, 0, packet.Length);

                    if (bytesRead > 0)
                    {
                        Sample currentSample = ExtractCurrentSample(packet);
                        if (currentSample is HardwareCommand)
                        {
                            HandleCommand(packet);
                        }
                        else
                        {
                            ProcessDigitalData(packet);
                        }
                    }
                    else
                    {
                        // Handle the case when the connection is closed by the server
                        // You may want to reconnect or take appropriate action based on your requirements
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listening for data: {ex.Message}");
            }
        }

        public Sample ExtractCurrentSample(byte[] packet)
        {
            Sample currentSample = new DigitalData();

            for (int packetByteIndex = 0; packetByteIndex < packet.Length; packetByteIndex++)
            {
                if (packetByteIndex + 2 < packet.Length)
                {
                    byte firstSampleByte = packet[packetByteIndex];
                    byte secondSampleByte = packet[packetByteIndex + 1];
                    byte thirdSampleByte = packet[packetByteIndex + 2];
                    if (firstSampleByte == SAMPLE_INITIALIZER)
                    {
                        if (secondSampleByte == SAMPLE_INITIALIZER)
                        {
                            if (thirdSampleByte == HEAD_BOX_COMMAND_MARKER || thirdSampleByte == STIM_COMMAND_MARKER)
                            { // Command
                                int commandLength = packet[packetByteIndex + 3];
                                currentSample = new HardwareCommand(thirdSampleByte, commandLength);
                                currentSample.Header = new byte[] { 0xFF, 0xFF, thirdSampleByte };
                                int sizeIndex = packetByteIndex + 3;
                                int commandStartIndex = sizeIndex + 1;
                                Array.Copy(packet, commandStartIndex, currentSample.Body, 0, currentSample.Length);
                                packetByteIndex += 19;
                            }
                            else
                            { // ADC Data
                                currentSample = new DigitalData();
                                currentSample.Header = new byte[] { 0XFF, 0XFF };
                                int commandStartIndex = 2;
                                Array.Copy(packet, commandStartIndex, currentSample.Body, 0, currentSample.Length);
                                packetByteIndex += 19;
                            }
                        }
                    }
                }
                else
                {
                    continue;
                }
            }

            return currentSample;
        }

        private void EnqueueDigitalData(byte[] digitalData)
        {
            lock (packetQueue)
            {
                packetQueue.Enqueue(digitalData);
            }
        }

        private void ProcessBufferedPackets()
        {
            while (true)
            {
                byte[] bufferedData;

                lock (packetQueue)
                {
                    if (packetQueue.Count > 0)
                    {
                        bufferedData = packetQueue.Dequeue();
                    }
                    else
                    {
                        // No more packets in the queue, exit the loop
                        break;
                    }
                }

                // Process or buffer the digital data
                ProcessDigitalData(bufferedData);
            }
        }

        public void HandleCommand(byte[] commandData)
        {
            // Add logic to handle the command received from the hardware
            // For example, send a response back to the hardware or perform an action
            // You can access this.clientStream to send data back to the hardware

            // After handling the command, process any buffered digital data
            ProcessBufferedPackets();
        }

        public void ProcessDigitalData(byte[] digitalData)
        {
            // Add logic to process or buffer the digital data received from the hardware
        }
    }
}
