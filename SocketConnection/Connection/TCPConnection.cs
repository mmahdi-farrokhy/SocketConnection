using SocketConnection.Data;
using System;
using System.Net.Sockets;

namespace SocketConnection.Hardware
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Windows.Forms;

    public class TCPConnection : SocketConnection
    {
        private TcpClient _tcpClient;
        private BlockingCollection<byte[]> _socketPacketBuffer;
        private BlockingCollection<byte[]> _digitalDataBuffer;
        private BlockingCollection<byte[]> _serialDataBuffer;
        private string IP = "192.168.3.xxx";
        private int Port = 5000;
        private const int ATTEMPTS_LIMIT = 5;
        private const int TIMEOUT = 5000;
        private readonly byte[] START_COMMAND = new byte[] { 0XFF, 0X31 };
        private readonly byte[] STOP_COMMAND = new byte[] { 0XFF, 0X31 };
        private DataProcessingMode _processingMode;
        private Test _currentTes;
        int collectionCapacity = 10 * 1000;
        private bool Helper_DefaultInstance_ApplicationIsRunning = true;

        public TCPConnection(string ip, int port, DataProcessingMode processingMode)
        {
            IP = ip;
            Port = port;
            _tcpClient = new TcpClient();
            _socketPacketBuffer = new BlockingCollection<byte[]>(collectionCapacity);
            _digitalDataBuffer = new BlockingCollection<byte[]>(collectionCapacity);
            _serialDataBuffer = new BlockingCollection<byte[]>(collectionCapacity);
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
                    IAsyncResult result = _tcpClient.BeginConnect(IP, Port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TIMEOUT, true);

                    if (success && _tcpClient.Connected)
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
            _tcpClient.Close();
        }

        public bool IsConnected()
        {
            return _tcpClient.Connected;
        }

        public byte[] ReceiveSocketData()
        {
            byte[] socketDataPacket = new byte[Packet.TotalPacketLength];
            bool shouldSendFakeCommand = false;
            short attemptsCounter = 0;

            while (!SendCommand(START_COMMAND) && attemptsCounter++ < ATTEMPTS_LIMIT) { }

            if (attemptsCounter >= ATTEMPTS_LIMIT)
            {
                throw new TCPSendMessageException("Could not send start command to the hardware");
            }

            if (shouldSendFakeCommand)
                SendFakeCommand();

            FlushCollection(ref _socketPacketBuffer);
            DateTime start = DateTime.UtcNow;
            while (IsConnected())
            {
                int totalBytesReceived = 0;
                bool recv_error = false;
                while (totalBytesReceived < Packet.TotalPacketLength)
                {
                    int bytesRead = _tcpClient.GetStream().Read(socketDataPacket, totalBytesReceived, Packet.TotalPacketLength - totalBytesReceived);
                    if (bytesRead == -1 || bytesRead == 0)
                    {
                        recv_error = true;
                        break;
                    }

                    totalBytesReceived += bytesRead;
                }

                if (recv_error) continue;

                if (_processingMode == DataProcessingMode.BufferedProcessing)
                {
                    _socketPacketBuffer.Add(socketDataPacket);
                }
                else if (_processingMode == DataProcessingMode.ImmediateDrawing)
                {
                    CallWaveInProc(socketDataPacket);
                }

                if (_currentTes == Test.NormalNeedle || _currentTes == Test.CascadeNeedle)
                {
                    DateTime stop = DateTime.UtcNow;
                    TimeSpan duration = stop.Subtract(start);
                    if (duration.Seconds > 60)
                        break;
                }
            }

            attemptsCounter = 0;
            while (!SendCommand(STOP_COMMAND) && attemptsCounter < ATTEMPTS_LIMIT) { }
            if (attemptsCounter >= ATTEMPTS_LIMIT)
                CallOnWaveInStop();

            return socketDataPacket;
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
                fakeCommand[ii + 1] = Packet.HeadBoxCommandMarker;
                fakeCommand[ii + 2] = fakeLen;

                for (int i = ii + 3; i < ii + fakeLen; i++)
                    fakeCommand[i] = 0;
            }
            while (!SendCommand(fakeCommand) && attemptNember++ < ATTEMPTS_LIMIT)
            {
            }

            bytesRead = _tcpClient.GetStream().Read(fakeCommand, 0, fakeLen * fakeCount);
        }

        private void CallWaveInProc(byte[] signal)
        {
            byte[] channel1Data = new byte[Packet.SignalBufferLength];
            byte[] channel2Data = new byte[Packet.SignalBufferLength];
            byte[] channel3Data = new byte[Packet.SignalBufferLength];
            byte[] channel4Data = new byte[Packet.SignalBufferLength];
            int numberOfSamples = Packet.TotalPacketLength / 19;

            for (int sampleCounter = 0; sampleCounter < numberOfSamples; sampleCounter++)
            {
                channel1Data[sampleCounter] = (byte)((signal[19 * sampleCounter + 2] * 256) + signal[19 * sampleCounter + 3]);
                channel2Data[sampleCounter] = (byte)((signal[19 * sampleCounter + 4] * 256) + signal[19 * sampleCounter + 5]);
                channel3Data[sampleCounter] = (byte)((signal[19 * sampleCounter + 6] * 256) + signal[19 * sampleCounter + 7]);
                channel4Data[sampleCounter] = (byte)((signal[19 * sampleCounter + 8] * 256) + signal[19 * sampleCounter + 9]);
            }
        }

        private void CallOnWaveInStop()
        {

        }

        public void ReadSocketDataBuffer()
        {
            try
            {
                byte[] signal = new byte[Packet.SignalBufferLength];
                byte[] command = new byte[Packet.CommandBufferLength];
                int sampleIndexInPacket = 0;
                int signalOverlapBytes = 0;
                int commandOverlapBytes = 0;
                int commandCounter = 0;
                int signalCounter = 0;
                byte[] packetFromBuffer;

                while (IsConnected() && Helper_DefaultInstance_ApplicationIsRunning)
                {
                    bool couldReadData = _socketPacketBuffer.TryTake(out packetFromBuffer, 10);
                    if (!couldReadData)
                        continue;

                    if (signalOverlapBytes > 0)
                    {
                        sampleIndexInPacket = CopyFromPacketBufferTo(ref signal, packetFromBuffer, signalOverlapBytes, signalCounter);
                        if (signalCounter == Packet.SignalBufferLimit)
                        {
                            CallWaveInProc(signal);
                            signalCounter = 0;
                        }
                    }
                    else if (commandOverlapBytes > 0)
                    {
                        sampleIndexInPacket = CopyFromPacketBufferTo(ref command, packetFromBuffer, commandOverlapBytes, commandCounter);
                        if (commandCounter == Packet.CommandBufferLimit)
                        {
                            AnalyzeStimCommand(command, Packet.CommandBufferLength);
                            commandCounter = 0;
                        }
                    }
                    else
                    {
                        sampleIndexInPacket = 0;
                    }

                    if (sampleIndexInPacket < Packet.TotalPacketLength)
                    {
                        DataType currentData = DetectDataType(packetFromBuffer, sampleIndexInPacket);
                        if (currentData == DataType.SerialData)
                        {
                            if (sampleIndexInPacket <= Packet.TotalPacketLength - Packet.SampleLength)
                            {
                                Array.Copy(packetFromBuffer, sampleIndexInPacket, command, Packet.SampleLength * commandCounter, Packet.SampleLength);
                                commandCounter++;
                                if (commandCounter == Packet.CommandBufferLimit)
                                {
                                    AnalyzeStimCommand(command, Packet.CommandBufferLength);
                                    commandCounter = 0;
                                }
                            }
                            else
                            {
                                Array.Copy(packetFromBuffer, sampleIndexInPacket, command, Packet.SampleLength * commandCounter, Packet.TotalPacketLength - sampleIndexInPacket);
                                commandCounter++;
                                commandOverlapBytes = Packet.SampleLength - (Packet.TotalPacketLength - sampleIndexInPacket);
                            }

                            sampleIndexInPacket += Packet.SampleLength;
                        }
                        else if (currentData == DataType.DigitalData)
                        {
                            if (sampleIndexInPacket <= Packet.TotalPacketLength - Packet.SampleLength)
                            {
                                Array.Copy(packetFromBuffer, sampleIndexInPacket, signal, Packet.SampleLength * signalCounter, Packet.SampleLength);
                                signalCounter++;
                                if (signalCounter == Packet.SignalBufferLimit)
                                {
                                    CallWaveInProc(signal);
                                    signalCounter = 0;
                                }
                            }
                            else
                            {
                                Array.Copy(packetFromBuffer, sampleIndexInPacket, signal, Packet.SampleLength * signalCounter, Packet.TotalPacketLength - sampleIndexInPacket);
                                signalCounter++;
                                signalOverlapBytes = Packet.SampleLength - (Packet.TotalPacketLength - sampleIndexInPacket);
                            }

                            sampleIndexInPacket += Packet.SampleLength;
                        }
                        else
                        {
                            sampleIndexInPacket = 0;
                        }
                    }
                }

                PopCollectionElements(_socketPacketBuffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error listening for data: {ex.Message}");
            }
        }

        private static int CopyFromPacketBufferTo(ref byte[] destinationBuffer, byte[] sourcePacket, int overlapBytes, int counter)
        {
            int IndexInPacket = overlapBytes;
            int TargetIndex = Packet.SampleLength * counter - overlapBytes;
            Array.Copy(sourcePacket, 0, destinationBuffer, TargetIndex, overlapBytes);
            return IndexInPacket;
        }

        private void PopCollectionElements(BlockingCollection<byte[]> socketPacketBuffer)
        {
            byte[] temp;
            bool status = socketPacketBuffer.TryTake(out temp, TimeSpan.FromMilliseconds(10));
            while (status)
            {
                status = socketPacketBuffer.TryTake(out temp, TimeSpan.FromMilliseconds(10));
            }
        }

        private DataType DetectDataType(byte[] packet, int sampleStartIndex)
        {
            DataType type = DataType.DigitalData;

            if (packet[sampleStartIndex] == Packet.SampleInitializer)
            {
                if (packet[sampleStartIndex + 1] == Packet.SampleInitializer)
                {
                    type = DataType.DigitalData;

                    if (packet[sampleStartIndex + 2] == Packet.HeadBoxCommandMarker || packet[sampleStartIndex + 2] == Packet.StimCommandMarker)
                    {
                        type = DataType.SerialData;
                    }
                }
            }

            return type;
        }

        private void AnalyzeStimCommand(byte[] command, int commandDecodeLength)
        {

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
                    if (firstSampleByte == Packet.SampleInitializer)
                    {
                        if (secondSampleByte == Packet.SampleInitializer)
                        {
                            if (thirdSampleByte == Packet.HeadBoxCommandMarker || thirdSampleByte == Packet.StimCommandMarker)
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
                if (_tcpClient.Connected)
                {
                    NetworkStream networkStream = _tcpClient.GetStream();
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
            _digitalDataBuffer.TryTake(out byte[] digitialData);
            return digitialData;
        }

        public byte[] ReadSerialDataBuffer()
        {
            _serialDataBuffer.TryTake(out byte[] serialData);
            return serialData;
        }

        public void FlushCollection(ref BlockingCollection<byte[]> collection)
        {
            while (collection.TryTake(out _)) { }
        }
    }
}
