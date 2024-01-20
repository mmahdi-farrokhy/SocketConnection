using SocketConnection.Data;
using System;
using System.Net.Sockets;

namespace SocketConnection.Hardware
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class TCPConnection : SocketConnection
    {
        private TcpClient tcpClient;
        private BlockingCollection<byte[]> digitalDataBuffer;
        private BlockingCollection<byte[]> commandBuffer;
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
            this.Port = port;
            tcpClient = new TcpClient();
            digitalDataBuffer = new BlockingCollection<byte[]>();
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
                        attemptCounter++;
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
            int packetBodyLength = 0;
            byte[] packetHeader = new byte[3];
            byte[] packetBody;
            byte[] fullPacket = new byte[0];

            tcpClient.GetStream().Read(packetHeader, 0, packetHeader.Length);

            if (packetHeader.SequenceEqual(new byte[] { 0XFF, 0XFF, 0XFA }) || packetHeader.SequenceEqual(new byte[] { 0XFF, 0XFF, 0XFB }))
            {
                packetBodyLength = uartSerialDataLength - 3;
                fullPacket = new byte[uartSerialDataLength];
            }
            else if (packetHeader.Take(2).SequenceEqual(new byte[] { 0XFF, 0XFF }))
            {
                packetBodyLength = adcDigitalDataLength - 3;
                fullPacket = new byte[adcDigitalDataLength];
            }

            packetBody = new byte[packetBodyLength];
            tcpClient.GetStream().Read(packetBody, 0, packetBody.Length);
            Buffer.BlockCopy(packetHeader, 0, fullPacket, 0, packetHeader.Length);
            Buffer.BlockCopy(packetBody, 0, fullPacket, packetHeader.Length, packetBody.Length);
            return fullPacket;
        }

        public void ListenForData()
        {
            try
            {
                while (IsConnected())
                {
                    byte[] packet = new byte[PACKET_SIZE];
                    int bytesRead = tcpClient.GetStream().Read(packet, 0, packet.Length);
                    if (bytesRead > 0)
                    {
                        Sample currentSample = ExtractCurrentSample(packet);
                        if (currentSample is HardwareCommand)
                        {
                            HandleCommand(currentSample as HardwareCommand);
                        }
                        else
                        {
                            _numberOfDigitalDataSamples = bytesRead / 1330;
                            ProcessDigitalData(packet);
                        }
                    }
                    else
                    {
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
                            {
                                int commandLength = packet[packetByteIndex + 3];
                                currentSample = new HardwareCommand(thirdSampleByte, commandLength);
                                currentSample.Header = new byte[] { 0xFF, 0xFF, thirdSampleByte };
                                int sizeIndex = packetByteIndex + 3;
                                int commandStartIndex = sizeIndex + 1;
                                Array.Copy(packet, commandStartIndex, currentSample.Body, 0, currentSample.Length);
                                packetByteIndex += 19;
                            }
                            else
                            {
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

        public void HandleCommand(HardwareCommand hardwareCommand)
        {
            commandBuffer.Add(hardwareCommand.Body);
        }

        public void ProcessDigitalData(byte[] digitalDataPacket)
        {
            byte[] digitalData = ExtractDigitalDataFromPacket(digitalDataPacket);
            digitalDataBuffer.Add(digitalData);
        }

        private byte[] ExtractDigitalDataFromPacket(byte[] digitalDataPacket)
        {
            byte[] extractedData = null;

            for (int sampleNumber = 0; sampleNumber < _numberOfDigitalDataSamples; sampleNumber++)
            {
                int copyStart = 2 + (sampleNumber * 19);
                int pasteStart = sampleNumber * 17;
                Array.Copy(digitalDataPacket, copyStart, extractedData, pasteStart, 17);
            }

            return extractedData;
        }

        private void EnqueueDigitalData(byte[] digitalData)
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
    }
}
