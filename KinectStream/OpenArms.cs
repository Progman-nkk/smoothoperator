using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace SmoothStream
{
    class OpenArms
    {
        Stopwatch writerAssistTimer, readerAssistTimer;
        BackgroundWorker writerAssistant, readerAssistant;
        SharedMemorySpace _globalCoordinates;
        string _serverIp = "notset";
        int _serverSocket = 0;

        public OpenArms(string serverIP, int serverSocket, ref SharedMemorySpace globalCoordinates)
        {
            tunnelSystem = new ClientTunnel[10];
            //initializeClients(serverIP, serverSocket);
            _globalCoordinates = globalCoordinates;
            _serverIp = serverIP;
            _serverSocket = serverSocket;
        }

        public void initializeBackgroundComs()
        {
            if (writerAssistant == null)
            {
                writerAssistTimer = new Stopwatch();
                writerAssistant = new BackgroundWorker();
                writerAssistant.DoWork += writerAssistant_DoWork;
                writerAssistant.RunWorkerCompleted += writerAssistant_RunWorkerCompleted;
                writerAssistant.RunWorkerAsync();
            }
            if (readerAssistant == null)
            {
                readerAssistTimer = new Stopwatch();
                readerAssistant = new BackgroundWorker();
                readerAssistant.DoWork += readerAssistant_DoWork;
                readerAssistant.RunWorkerCompleted += readerAssistant_RunWorkerCompleted;
                readerAssistant.RunWorkerAsync();
            }

        }
        private void writerAssistant_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            writerAssistTimer.Restart();
            BackgroundWorker worker = (BackgroundWorker)sender;

            OpenArms.E6Pos tempE6 = new OpenArms.E6Pos();
            tempE6.updateE6Pos(calculateDelta(), _globalCoordinates);
            ClientTunnel tempClient = getNextClient();
            tempClient.writeVariable("MYPOS", tempE6.currentValue);
            tempClient.IsActive = false;
            _globalCoordinates.SentMessage = tempE6.currentValue;
        }
        private void writerAssistant_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            while (writerAssistTimer.Elapsed.TotalMilliseconds < 3.5) ;
            BackgroundWorker worker = (BackgroundWorker)sender;
            _globalCoordinates.WriterBackgroundTimer = writerAssistTimer.Elapsed.TotalMilliseconds;
            worker.RunWorkerAsync();
        }
        private void readerAssistant_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            readerAssistTimer.Restart();
            BackgroundWorker worker = (BackgroundWorker)sender;
            readCurrentAxisThread();
            readCurrentPosThread();
        }
        private void readerAssistant_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            while (readerAssistTimer.Elapsed.TotalMilliseconds < 7) ;
            BackgroundWorker worker = (BackgroundWorker)sender;
            worker.RunWorkerAsync();
        }
        private void readCurrentAxisThread()
        {
            ClientTunnel tempClient = getNextClient();
            E6Axis currentAxis = new E6Axis(tempClient.readVariable("$AXIS_ACT_MEAS"));
            tempClient.IsActive = false;
            currentAxis.updateCurrentE6Axis(ref _globalCoordinates);
        }
        private void readCurrentPosThread()
        {
            ClientTunnel tempClient = getNextClient();
            E6Pos currentPos = new E6Pos(tempClient.readVariable("$POS_ACT"));
            tempClient.IsActive = false;
            currentPos.updateCurrentE6Pos(ref _globalCoordinates);
        }
        private double[] calculateDelta()
        {
            double[] deltaValues = new double[6] { 0, 0, 0, 0, 0, 0 };
            double posAcceleration = 6;
            double rotAcceleration = .6;
            double rotDecceleration = .4;
            double posDecceleration = .8;
            double maxValue = 2;
            double minValue = -2;

            if (_globalCoordinates.HandState != "Lasso")
            {
                if (_globalCoordinates.CurrentA3 >= 120)
                {
                    deltaValues[0] = 0.5;
                }
                else
                {
                    deltaValues[0] = (_globalCoordinates.HandZ * 0);
                }
                deltaValues[1] = (_globalCoordinates.HandX * posAcceleration);
                deltaValues[2] = (_globalCoordinates.HandY * posAcceleration);
                deltaValues[3] = (_globalCoordinates.HandX * rotAcceleration);
                deltaValues[4] = (_globalCoordinates.HandY * rotAcceleration);
                deltaValues[5] = (_globalCoordinates.HandZ * rotAcceleration);
            }
            else
            {
                deltaValues[0] = deltaValues[0] * posDecceleration;
                deltaValues[1] = deltaValues[1] * posDecceleration;
                deltaValues[2] = deltaValues[2] * posDecceleration;
                deltaValues[3] = deltaValues[3] * rotDecceleration;
                deltaValues[4] = deltaValues[4] * rotDecceleration;
                deltaValues[5] = deltaValues[5] * rotDecceleration;
            }
            for (int i = 0; i < deltaValues.Length; i++)
            {
                if (deltaValues[i] > maxValue)
                {
                    deltaValues[i] = maxValue;
                }
                if (deltaValues[i] < minValue)
                {
                    deltaValues[i] = minValue;
                }
            }
            _globalCoordinates.ArmX = deltaValues[0];
            _globalCoordinates.ArmY = deltaValues[1];
            _globalCoordinates.ArmZ = deltaValues[2];
            _globalCoordinates.ArmA = deltaValues[3];
            _globalCoordinates.ArmB = deltaValues[4];
            _globalCoordinates.ArmC = deltaValues[5];

            return deltaValues;
        }

        public class ReceiveMessageFormat
        {
            public ushort _messageId;
            public ushort _reqLength;
            public int _functionType;
            public ushort _varLength;
            public string _varValue;
            public int _temp;
            public ReceiveMessageFormat(byte[] messageResponse)
            {

                _messageId = (ushort)messageResponse[1];
                _reqLength = (ushort)messageResponse[3];
                _functionType = (int)messageResponse[4];
                _varLength = (ushort)messageResponse[6];

                byte[] _varValueBytes = new byte[_varLength];
                Buffer.BlockCopy(messageResponse, 7, _varValueBytes, 0, _varLength);
                _varValue = Encoding.UTF8.GetString(_varValueBytes);
                _temp = _messageId;
            }
        }
        private class SendMessageFormat
        {
            private ushort _reqLength; // In hexadecimal units
            private ushort _messageId; // In hexadecimal units
            public byte _functionType;
            private ushort _varNameLength; // In hexadecimal units
            private string _varName;
            private ushort _varValueLength; // In hexadecimal units
            private string _varValue;
            private string _combined;
            public byte[] messageReady;

            // Read Constructor
            public SendMessageFormat(string messageId, string reqLength, string functionType, string varNameLength, string varName)
            {
                _messageId = Convert.ToUInt16(messageId, 16);
                _reqLength = Convert.ToUInt16(reqLength, 16); // In hexadecimal units
                _functionType = Byte.Parse(functionType);
                _varNameLength = Convert.ToUInt16(varNameLength, 16); // In hexadecimal units
                _varName = varName;
                _combined = messageId + reqLength + functionType + varNameLength + varName;
                formatReadRequest(out messageReady);
            }
            // Write Constructor
            public SendMessageFormat(string messageId, string reqLength, string functionType, string varNameLength, string varName, string varValueLength, string varValue)
            {
                _messageId = Convert.ToUInt16(messageId, 16);
                _reqLength = Convert.ToUInt16(reqLength, 16); // In hexadecimal units
                _functionType = Byte.Parse(functionType);
                _varNameLength = Convert.ToUInt16(varNameLength, 16); // In hexadecimal units
                _varName = varName;
                _varValueLength = Convert.ToUInt16(varValueLength, 16); // In hexadecimal units
                _varValue = varValue;
                _combined = messageId + reqLength + functionType + varNameLength + varName + varValueLength + varValue;
                formatWriteRequest(out messageReady);
            }
            private void formatReadRequest(out byte[] messageReady)
            {
                List<byte[]> byteParts = new List<byte[]>();

                byte[] _messageId_bytes = BitConverter.GetBytes(_messageId);
                Array.Reverse(_messageId_bytes);
                byte[] _reqLength_bytes = BitConverter.GetBytes(_reqLength);
                Array.Reverse(_reqLength_bytes);
                byte[] _functionType_bytes = { _functionType };
                byte[] _varNameLength_bytes = BitConverter.GetBytes(_varNameLength);
                Array.Reverse(_varNameLength_bytes);
                byte[] _varName_bytes = Encoding.UTF8.GetBytes(_varName);
                byteParts.Add(_messageId_bytes);
                byteParts.Add(_reqLength_bytes);
                byteParts.Add(_functionType_bytes);
                byteParts.Add(_varNameLength_bytes);
                byteParts.Add(_varName_bytes);

                int totalArrayLength = _messageId_bytes.Length + _reqLength_bytes.Length +
                                       _functionType_bytes.Length + _varNameLength_bytes.Length +
                                       _varName_bytes.Length;

                messageReady = AppendByteArrays(byteParts, totalArrayLength, _functionType);
            }
            private void formatWriteRequest(out byte[] messageReady)
            {
                List<byte[]> byteParts = new List<byte[]>();

                byte[] _messageId_bytes = BitConverter.GetBytes(_messageId);
                Array.Reverse(_messageId_bytes);
                byte[] _reqLength_bytes = BitConverter.GetBytes(_reqLength);
                Array.Reverse(_reqLength_bytes);
                byte[] _functionType_bytes = { _functionType };
                byte[] _varNameLength_bytes = BitConverter.GetBytes(_varNameLength);
                Array.Reverse(_varNameLength_bytes);
                byte[] _varName_bytes = Encoding.UTF8.GetBytes(_varName);
                byte[] _varValueLength_bytes = BitConverter.GetBytes(_varValueLength);
                Array.Reverse(_varValueLength_bytes);
                byte[] _varValue_bytes = Encoding.UTF8.GetBytes(_varValue);
                byteParts.Add(_messageId_bytes);
                byteParts.Add(_reqLength_bytes);
                byteParts.Add(_functionType_bytes);
                byteParts.Add(_varNameLength_bytes);
                byteParts.Add(_varName_bytes);
                byteParts.Add(_varValueLength_bytes);
                byteParts.Add(_varValue_bytes);

                int totalArrayLength = _messageId_bytes.Length + _reqLength_bytes.Length +
                                       _functionType_bytes.Length + _varNameLength_bytes.Length +
                                       _varName_bytes.Length + _varValueLength_bytes.Length + _varValue_bytes.Length;

                messageReady = AppendByteArrays(byteParts, totalArrayLength, _functionType);
            }
            static byte[] AppendByteArrays(List<byte[]> _byteParts, int _totalArrayLength, int _functionType)
            {
                byte[] outputBytes = new byte[_totalArrayLength];
                Buffer.BlockCopy(_byteParts[0], 0, outputBytes, 0, _byteParts[0].Length);
                Buffer.BlockCopy(_byteParts[1], 0, outputBytes, _byteParts[0].Length, _byteParts[1].Length);
                Buffer.BlockCopy(_byteParts[2], 0, outputBytes, (_byteParts[0].Length + _byteParts[1].Length), _byteParts[2].Length);
                Buffer.BlockCopy(_byteParts[3], 0, outputBytes, (_byteParts[0].Length + _byteParts[1].Length + _byteParts[2].Length), _byteParts[3].Length);
                Buffer.BlockCopy(_byteParts[4], 0, outputBytes, (_byteParts[0].Length + _byteParts[1].Length + _byteParts[2].Length + _byteParts[3].Length), _byteParts[4].Length);
                if (_functionType == 1)
                {
                    Buffer.BlockCopy(_byteParts[5], 0, outputBytes, (_byteParts[0].Length + _byteParts[1].Length + _byteParts[2].Length + _byteParts[3].Length + _byteParts[4].Length), _byteParts[5].Length);
                    Buffer.BlockCopy(_byteParts[6], 0, outputBytes, (_byteParts[0].Length + _byteParts[1].Length + _byteParts[2].Length + _byteParts[3].Length + _byteParts[4].Length + _byteParts[5].Length), _byteParts[6].Length);
                }
                return outputBytes;
            }

        }
        private class E6Pos
        {

            public double xValue;
            public double yValue;
            public double zValue;
            public double aValue;
            public double bValue;
            public double cValue;
            //public double e1Value;
            //public double e2Value;
            //public double e3Value;
            //public double e4Value;
            //public double e5Value;
            //public double e6Value;
            //public int sValue;
            //public int tValue;

            public string currentValue;


            public E6Pos(string encodedE6)
            {
                if(encodedE6.Length>1)
                {
                currentValue = encodedE6;
                Regex _regex = new Regex(@"([+-]?[0-9]+[\.]?[0-9]+[eE]?[+-]?[0-9]+)");
                string[] result = _regex.Split(encodedE6);
                xValue = Convert.ToDouble(result[1]);
                yValue = Convert.ToDouble(result[3]);
                zValue = Convert.ToDouble(result[5]);
                aValue = Convert.ToDouble(result[7]);
                bValue = Convert.ToDouble(result[9]);
                cValue = Convert.ToDouble(result[11]);
                //e1Value = Convert.ToDouble(result[13]);
                //e2Value = Convert.ToDouble(result[15]);
                //e3Value = Convert.ToDouble(result[17]);
                
                }

            }
            public E6Pos()
            {
                xValue = 0;
                yValue = 0;
                zValue = 0;
                aValue = 0;
                bValue = 0;
                cValue = 0;
            }
            //public void updateE6Pos(double[] deltaArray)
            //{
            //    //xValue += deltaArray[0];
            //    xValue = 0;
            //    yValue += deltaArray[1];
            //    zValue += deltaArray[2];
            //    aValue += deltaArray[3];
            //    bValue -= deltaArray[4];
            //    //cValue += deltaArray[5];
            //    //e1Value += deltaArray[6];
            //    //e2Value += deltaArray[7];
            //    //e3Value += deltaArray[8];
            //    currentValue = formatE6Pos(xValue, yValue, zValue, aValue, bValue, cValue);

            //}
            public void updateE6Pos(double[] deltaArray, SharedMemorySpace globalCoordinates)
            {
                xValue += deltaArray[0];
                yValue += deltaArray[1];
                zValue += deltaArray[2];
                aValue += deltaArray[3];
                //bValue -= deltaArray[4];
                //cValue += deltaArray[5];
                //e1Value += deltaArray[6];
                //e2Value += deltaArray[7];
                //e3Value += deltaArray[8];
                currentValue = formatE6Pos(xValue, yValue, zValue, aValue, bValue, cValue);

            }
            private string formatE6Pos(double x, double y, double z, double a, double b, double c)
            {
                string valueString = "{E6POS: X " + x + ", Y " + y + ", Z " + z + ", A " + a + ", B " + b + ", C " + c + ", E1 0.0, E2 0.0, E3 0.0, E4 0.0, E5 0.0, E6 0.0}";
                return valueString;
            }
            public void updateCurrentE6Pos(ref SharedMemorySpace globalCoordinates)
            {

                globalCoordinates.CurrentX = xValue;
                globalCoordinates.CurrentY = yValue;
                globalCoordinates.CurrentZ = zValue;
                globalCoordinates.CurrentA = aValue;
                globalCoordinates.CurrentB = bValue;
                globalCoordinates.CurrentC = cValue;

                currentValue = formatE6Pos(xValue, yValue, zValue, aValue, bValue, cValue);
            }
        }
        private class E6Axis
        {
            public double a1Value;
            public double a2Value;
            public double a3Value;
            public double a4Value;
            public double a5Value;
            public double a6Value;
            public double e1Value;
            public double e2Value;
            public double e3Value;
            public double e4Value;
            public double e5Value;
            public double e6Value;
            public int sValue;
            public int tValue;

            public string currentValue;

            public E6Axis(string encodedE6)
            {
                if (encodedE6.Length > 1)
                {
                    currentValue = encodedE6;
                    Regex _regex = new Regex(@"([+-]?[0-9]+[\.]?[0-9]+[eE]?[+-]?[0-9]+?)|(0\.0)");
                    string[] result = _regex.Split(encodedE6);
                    a1Value = Convert.ToDouble(result[1]);
                    a2Value = Convert.ToDouble(result[3]);
                    a3Value = Convert.ToDouble(result[5]);
                    a4Value = Convert.ToDouble(result[7]);
                    a5Value = Convert.ToDouble(result[9]);
                    a6Value = Convert.ToDouble(result[11]);
                    //e1Value = Convert.ToDouble(result[13]);
                    //e2Value = Convert.ToDouble(result[15]);
                    //e3Value = Convert.ToDouble(result[17]);
                    //e4Value = Convert.ToDouble(result[19]);
                    //e5Value = Convert.ToDouble(result[21]);
                    //e6Value = Convert.ToDouble(result[23]);

                }

            }
            public E6Axis()
            {
                a1Value = 0;
                a2Value = 0;
                a3Value = 0;
                a4Value = 0;
                a5Value = 0;
                a6Value = 0;
            }
            public void updateCurrentE6Axis(ref SharedMemorySpace globalCoordinates)
            {

                globalCoordinates.CurrentA1 = a1Value;
                globalCoordinates.CurrentA2 = a2Value;
                globalCoordinates.CurrentA3 = a3Value;
                globalCoordinates.CurrentA4 = a4Value;
                globalCoordinates.CurrentA5 = a5Value;
                globalCoordinates.CurrentA6 = a6Value;
                
                currentValue = formatE6Axis(a1Value, a2Value, a3Value, a4Value, a5Value, a6Value);

            }
            
            private string formatE6Axis(double a1, double a2, double a3, double a4, double a5, double a6)
            {
                string valueString = "{E6POS: X " + a1 + ", Y " + a2 + ", Z " + a3 + ", A " + a4 + ", B " + a5 + ", C " + a6 + ", E1 0.0, E2 0.0, E3 0.0, E4 0.0, E5 0.0, E6 0.0}";
                return valueString;
            }

        }

        private ClientTunnel getNextClient()
        {
            foreach (ClientTunnel client in tunnelSystem)
            {
                if (!client.IsActive)
                {
                    client.IsActive = true;
                    return client;
                }
            }
            MessageBox.Show("Clients Busy");
            return tunnelSystem[11]; // <-- Fix this 

        }
        private ClientTunnel[] tunnelSystem;
        public void initializeClients()
        {
            int clientsConnected = 0;
            for (int i = 0; i < tunnelSystem.Length; i++)
            {
                if (tunnelSystem[i] == null)
                {
                    tunnelSystem[i] = new ClientTunnel(_serverIp, _serverSocket);
                    if (tunnelSystem[i].isConnected())
                    {
                        clientsConnected++;
                    }
                }
            }
            if (clientsConnected == tunnelSystem.Length)
            {
                MessageBox.Show("all connected");
                initializeBackgroundComs();
            }
            else
            {
                for (int i = 0; i < tunnelSystem.Length; i++)
                {
                    tunnelSystem[i] = null;
                }
            }
        }

        private class ClientTunnel
        {
            private Socket clientSocket;
            public Socket ClientSocket
            {
                get { return clientSocket; }
                set { clientSocket = value; }
            }
            private bool isActive = false;
            public bool IsActive
            {
                get { return isActive; }
                set { isActive = value; }
            }

            public ClientTunnel(string serverIP, int serverSocket)
            {
                startClient(serverIP, serverSocket);
            }

            public bool startClient(string serverIP, int serverSocket)
            {
                ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                System.Net.IPAddress ip = System.Net.IPAddress.Parse(serverIP);
                IPEndPoint clientEndPoint = new IPEndPoint(ip, serverSocket);
                Stopwatch connectionTimeout = new Stopwatch();
                connectionTimeout.Start();
                try
                {
                    IAsyncResult result = ClientSocket.BeginConnect(clientEndPoint, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(5000, true);
                    if (ClientSocket.Connected) { ClientSocket.EndConnect(result); }
                    else
                    {
                        ClientSocket.Close();
                        MessageBox.Show("Connection Timeout!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw new System.Exception("Could not instantiate client");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Trouble Connecting: " + e.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
            public bool isConnected()
            {
                if (ClientSocket.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public string readVariable(string _variableRead)
            {
                string outputString;
                byte[] messageReq = readMessageRequest(_variableRead, out outputString);
                byte[] receivedData = new byte[256];
                int sentBytes = 0;
                int receivedBytes = 0;
                try
                {
                    sentBytes = ClientSocket.Send(messageReq);
                    //MessageBox.Show("Sent:" + sentBytes.ToString() + " bytes as " + outputString, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    receivedBytes = ClientSocket.Receive(receivedData);
                }
                catch (SocketException e)
                {
                    MessageBox.Show("SocketException: " + e.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                ReceiveMessageFormat response = new ReceiveMessageFormat(receivedData);
                //MessageBox.Show("Received:" + receivedBytes.ToString() + " bytes as" + response._varValue, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return response._varValue;
            }
            public string writeVariable(string _variableWrite, string targetE6)
            {
                string outputString;
                byte[] messageReq = writeMessageRequest(_variableWrite, targetE6, out outputString);
                byte[] receivedData = new byte[256];
                int sentBytes = 0;
                int receivedBytes = 0;
                try
                {
                    sentBytes = ClientSocket.Send(messageReq);
                    //MessageBox.Show("Sent:" + sentBytes.ToString() + " bytes as " + outputString, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    receivedBytes = ClientSocket.Receive(receivedData);
                }
                catch (SocketException e)
                {
                    MessageBox.Show("SocketException: " + e.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                ReceiveMessageFormat response = new ReceiveMessageFormat(receivedData);
                //MessageBox.Show("Received:" + receivedBytes.ToString() + " bytes as" + response._varValue, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return response._varValue;
            }

            byte[] readMessageRequest(string varName, out string outputString)
            {
                Random rnd = new Random();
                string messageId = (rnd.Next(0, 99)).ToString("X2");
                string functionType = "0";
                string varNameLength = varName.Length.ToString("X2");
                string reqLength = (varName.Length + 3).ToString("X2");
                outputString = messageId + reqLength + functionType + varNameLength + varName;
                SendMessageFormat readRequest = new SendMessageFormat(messageId, reqLength, functionType, varNameLength, varName);

                return readRequest.messageReady;
            }
            byte[] writeMessageRequest(string varName, string varValue, out string outputString)
            {
                Random rnd = new Random();
                string messageId = (rnd.Next(0, 99)).ToString("X2");
                string reqLength = (varName.Length + varValue.Length + 5).ToString("X2");
                string functionType = "1";
                string varNameLength = varName.Length.ToString("X2");
                string varValueLength = varValue.Length.ToString("X2");


                outputString = messageId + reqLength + functionType + varNameLength + varName + varValueLength + varValue;
                SendMessageFormat readRequest = new SendMessageFormat(messageId, reqLength, functionType, varNameLength, varName, varValueLength, varValue);

                return readRequest.messageReady;
            }
        }
    }
}
