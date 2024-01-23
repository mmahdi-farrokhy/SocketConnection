using SocketConnection.Data;
using System;
using System.Net.Sockets;

namespace SocketConnection.Hardware
{
    using System.Collections.Concurrent;
    using System.Diagnostics;

    public class TCPConnection : SocketConnection
    {
        private TcpClient tcpClient;
        private BlockingCollection<byte[]> socketBuffer;
        private BlockingCollection<byte[]> digitalDataBuffer;
        private BlockingCollection<byte[]> serialDataBuffer;
        private string IP = "192.168.3.xxx";
        private int Port = 5000;
        private const int ATTEMPTS_LIMIT = 5;
        private const int TIMEOUT = 5000;
        private const int SAMPLE_SIZE = 19;
        private const int SAMPLE_PER_PACKET = 70;
        private const int PACKET_SIZE = SAMPLE_SIZE * SAMPLE_PER_PACKET;
        private const byte SAMPLE_INITIALIZER = 0xFF;
        private const byte STIM_COMMAND_MARKER = 0xFA;
        private const byte HEAD_BOX_COMMAND_MARKER = 0xFB;
        private Packet _packet;
        private readonly byte[] START_COMMAND = new byte[] { 0XFF, 0X31 };
        private readonly byte[] STOP_COMMAND = new byte[] { 0XFF, 0X31 };
        private DataProcessingMode _processingMode;
        private Test _currentTes;

        public TCPConnection(string ip, int port, DataProcessingMode processingMode)
        {
            IP = ip;
            Port = port;
            tcpClient = new TcpClient();
            socketBuffer = new BlockingCollection<byte[]>();
            digitalDataBuffer = new BlockingCollection<byte[]>();
            serialDataBuffer = new BlockingCollection<byte[]>();
            StartConnection();
            _processingMode = processingMode;
        }

        public void StartConnection()
        {
            int attemptCounter = 0;

            while (attemptCounter < ATTEMPTS_LIMIT)
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

            if (attemptCounter >= ATTEMPTS_LIMIT)
            {
                throw new TCPConnectionException($"Failed to connect after {ATTEMPTS_LIMIT} attempts.");
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

        public byte[] ReceiveSocketData()
        {
            byte[] socketData = new byte[PACKET_SIZE];
            bool shouldSendFakeCommand = false;
            short attemptsCounter = 0;

            while (!SendCommand(START_COMMAND) && attemptsCounter++ < ATTEMPTS_LIMIT) { }

            if (attemptsCounter >= ATTEMPTS_LIMIT)
            {
                throw new TCPSendMessageException("Could not send start command to the hardware");
            }

            if (shouldSendFakeCommand)
                SendFakeCommand();

            FlushCollection(ref socketBuffer);
            DateTime start = DateTime.UtcNow;
            //while (IsConnected())
            {
                int totalBytesReceived = 0;
                bool recv_error = false;
                //while (totalBytesReceived < PACKET_SIZE)
                {
                    int bytesRead = tcpClient.GetStream().Read(socketData, totalBytesReceived, PACKET_SIZE - totalBytesReceived);
                    if (bytesRead == -1 || bytesRead == 0)
                    {
                        recv_error = true;
                        //break;
                    }

                    totalBytesReceived += bytesRead;
                }

                //if (recv_error) continue;

                if (_processingMode == DataProcessingMode.BufferedProcessing)
                {
                    socketBuffer.Add(socketData);
                }
                else if (_processingMode == DataProcessingMode.ImmediateProcessing)
                {
                    ProcessData(socketData);
                }

                if (_currentTes == Test.NormalNeedle || _currentTes == Test.CascadeNeedle)
                {
                    DateTime stop = DateTime.UtcNow;
                    TimeSpan duration = stop.Subtract(start);
                    if (duration.Seconds > 60)
                        /*break*/;
                }
            }

            attemptsCounter = 0;
            while (!SendCommand(STOP_COMMAND) && attemptsCounter < ATTEMPTS_LIMIT) { }
            if (attemptsCounter >= ATTEMPTS_LIMIT)
                StopProcessingData();

            return socketData;
        }

        private void StopProcessingData()
        {
            throw new NotImplementedException();
        }

        private void ProcessData(byte[] socketData)
        {
            throw new NotImplementedException();
        }

        private void SendFakeCommand()
        {
            int attemptNember = 0;
            int bytesRead;
            const int fakeLen = 13;
            const int fakeCount = 100;
            byte[] fakeCommand = new byte[fakeLen * fakeCount];
            for (int ii = 0; ii < fakeLen * fakeCount; ii += fakeLen)
            {
                fakeCommand[ii + 0] = 0xFF;
                fakeCommand[ii + 1] = HEAD_BOX_COMMAND_MARKER;
                fakeCommand[ii + 2] = fakeLen;

                for (int i = ii + 3; i < ii + fakeLen; i++)
                    fakeCommand[i] = 0;
            }
            while (!SendCommand(fakeCommand) && attemptNember++ < ATTEMPTS_LIMIT)
            {
            }

            bytesRead = tcpClient.GetStream().Read(fakeCommand, 0, fakeLen * fakeCount);
        }

        public void ReadSocketDataBuffer()
        {
            try
            {
                while (IsConnected() && SendCommand(START_COMMAND))
                {
                    byte[] packet = new byte[PACKET_SIZE];
                    int bytesRead = tcpClient.GetStream().Read(packet, 0, PACKET_SIZE);
                    int remainingDigitalDataBytes = 0;
                    int remainingSerialDataBytes = 0;

                    if (bytesRead < PACKET_SIZE)
                    {
                        Array.Resize<byte>(ref packet, bytesRead);
                    }

                    if (bytesRead > 0)
                    {
                        Sample currentSample = ExtractCurrentSample(packet);
                        if (currentSample is SerialData)
                        {
                            serialDataBuffer.Add(currentSample.Body);
                        }
                        else if (currentSample is DigitalData)
                        {
                            digitalDataBuffer.Add(currentSample.Body);
                        }
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
                                currentSample = new SerialData(commandLength);
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

        public bool SendCommand(byte[] command)
        {
            bool isSent = false;
            try
            {
                if (tcpClient.Connected)
                {
                    NetworkStream networkStream = tcpClient.GetStream();
                    networkStream.Write(command, 0, command.Length);
                    Console.WriteLine($"Command sent successfully: {BitConverter.ToString(command)}");
                    isSent = true;
                }
            }
            catch
            {
                isSent = false;
            }

            return isSent;
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

        public void FlushCollection(ref BlockingCollection<byte[]> collection)
        {
            while (collection.TryTake(out _)) { }
        }
    }
}
