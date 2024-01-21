using SocketConnection.Data;
using System;
using System.Net.Sockets;

namespace SocketConnection.Hardware
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class TCPConnection : SocketConnection
    {
        private TcpClient tcpClient;
        private BlockingCollection<byte[]> digitalDataBuffer;
        private BlockingCollection<byte[]> serialDataBuffer;
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
        private int _numberOfDigitalDataSamples;

        public TCPConnection(string ip, int port)
        {
            IP = ip;
            Port = port;
            tcpClient = new TcpClient();
            digitalDataBuffer = new BlockingCollection<byte[]>();
            serialDataBuffer = new BlockingCollection<byte[]>();
            StartConnection();
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
                        Debug.WriteLine("Connection established.");
                        break;
                    }
                    else
                    {
                        StopConnection();
                        attemptCounter++;
                    }
                }
                catch (NullReferenceException)
                {
                    attemptCounter++;
                }
            }

            if (attemptCounter >= NUMBER_OF_ATTEMPTS)
            {
                throw new TCPConnectionException($"Failed to connect after {NUMBER_OF_ATTEMPTS} attempts.");
            }
        }

        public void StopConnection()
        {
            tcpClient.Close();
        }

        public bool IsConnected()
        {
            return tcpClient.Connected;
        }

        public byte[] ReadPacketFromSocket()
        {
            int adcDigitalDataLength = 1330;
            int uartSerialDataLength = 19;
            int headerLength = 3;
            int bodyLength = 0;
            byte[] header = new byte[headerLength];
            byte[] body = new byte[0];
            byte[] packet = new byte[0];

            tcpClient.GetStream().Read(header, 0, headerLength);

            if (header.SequenceEqual(new byte[] { 0XFF, 0XFF, 0XFA }) || header.SequenceEqual(new byte[] { 0XFF, 0XFF, 0XFB }))
            {
                bodyLength = uartSerialDataLength - 3;
                packet = new byte[uartSerialDataLength];
            }
            else if (header.Take(2).SequenceEqual(new byte[] { 0XFF, 0XFF }))
            {
                bodyLength = adcDigitalDataLength - 3;
                packet = new byte[adcDigitalDataLength];
            }

            body = new byte[bodyLength];
            tcpClient.GetStream().Read(body, 0, bodyLength);
            Buffer.BlockCopy(header, 0, packet, 0, headerLength);
            Buffer.BlockCopy(body, 0, packet, headerLength, bodyLength);
            return packet;
        }

        public void ListenForData()
        {
            try
            {
                var task = Task.Factory.StartNew(() =>
                {
                    while (IsConnected())
                    {
                        byte[] packet = new byte[PACKET_SIZE];
                        int bytesRead = tcpClient.GetStream().Read(packet, 0, PACKET_SIZE);
                        if (bytesRead < PACKET_SIZE)
                        {
                            Array.Resize<byte>(ref packet, bytesRead);
                        }

                        if (bytesRead > 0)
                        {
                            Sample currentSample = ExtractCurrentSample(packet);
                            if (currentSample is HardwareCommand)
                            {
                                HandleCommand(currentSample as HardwareCommand);
                            }
                            else
                            {
                                _numberOfDigitalDataSamples = bytesRead / 19;
                                ProcessDigitalData(packet);
                            }
                        }
                        else
                        {
                            //break;
                        }
                    }
                }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listening for data: {ex.Message}");
            }
        }

        public Sample ExtractCurrentSample(byte[] packet)
        {
            Sample currentSample = null;

            for (int packetByteIndex = 0; packetByteIndex < packet.Length; packetByteIndex++)
            {
                if (packetByteIndex + 3 < packet.Length)
                {
                    byte firstSampleByte = packet[packetByteIndex];
                    byte secondSampleByte = packet[packetByteIndex + 1];
                    byte thirdSampleByte = packet[packetByteIndex + 2];
                    if (firstSampleByte == SAMPLE_INITIALIZER)
                    {
                        if (secondSampleByte == SAMPLE_INITIALIZER)
                        {
                            if (thirdSampleByte == HEAD_BOX_COMMAND_MARKER || thirdSampleByte == STIM_COMMAND_MARKER)
                            { // UART Serial Data
                                int commandLength = packet[packetByteIndex + 3];
                                currentSample = new HardwareCommand(thirdSampleByte, commandLength);
                                currentSample.Header = new byte[] { 0xFF, 0xFF, thirdSampleByte };
                                int commandStartIndex = packetByteIndex + 4;
                                Array.Copy(packet, commandStartIndex, currentSample.Body, 0, currentSample.Length);
                            }
                            else
                            { // ADC Digital Data
                                currentSample = new DigitalData();
                                currentSample.Header = new byte[] { 0XFF, 0XFF };
                                int commandStartIndex = 2;
                                Array.Copy(packet, commandStartIndex, currentSample.Body, 0, currentSample.Length);
                            }

                            break;
                        }
                    }
                }
            }

            return currentSample;
        }

        public void HandleCommand(HardwareCommand hardwareCommand)
        {
            serialDataBuffer.Add(hardwareCommand.Body);
        }

        public void ProcessDigitalData(byte[] digitalDataPacket)
        {
            byte[] digitalData = ExtractDigitalDataFromPacket(digitalDataPacket, _numberOfDigitalDataSamples);
            BufferDigitalData(digitalData);
        }

        public byte[] ExtractDigitalDataFromPacket(byte[] digitalDataPacket, int numberOfSamples)
        {
            byte[] extractedData = new byte[numberOfSamples * 17];

            for (int sampleNumber = 0; sampleNumber < numberOfSamples; sampleNumber++)
            {
                int copyStart = 2 + (sampleNumber * 19);
                int pasteStart = sampleNumber * 17;
                Array.Copy(digitalDataPacket, copyStart, extractedData, pasteStart, 17);
            }

            return extractedData;
        }

        private void BufferDigitalData(byte[] digitalData)
        {
            lock (digitalDataBuffer)
            {
                digitalDataBuffer.Add(digitalData);
            }
        }

        public void SendCommand(byte[] command)
        {
            try
            {
                if (tcpClient == null || !tcpClient.Connected)
                {
                    Console.WriteLine("Error: TcpClient is not connected.");
                    return;
                }

                NetworkStream networkStream = tcpClient.GetStream();
                networkStream.Write(command, 0, command.Length);
                Console.WriteLine($"Command sent successfully: {BitConverter.ToString(command)}");
            }
            catch (Exception ex)
            {

                throw new TCPSendMessageException($"Error sending command: {ex.Message}");
            }
        }

        public byte[] ReadDigitalDataBuffer()
        {
            digitalDataBuffer.TryTake(out byte[] digitialData);
            return digitialData;
        }

        public byte[] ReadSerialDataBuffer()
        {
            serialDataBuffer.TryTake(out byte[] serialData);
            return serialData;
        }
    }
}
