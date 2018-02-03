using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SmoothStream
{
   
    
    public partial class SmoothOperator : Form
    {
        public SmoothOperator()
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            InitializeComponent();
            initializeGlobals();
            initializeKinect();
            initializeGraphics();
        }

        // Camera
        
        private KinectSensor kinectSensor = null;
        private Thread bodyThread = null;
        private Thread depthThread = null;
        private DepthFrameReader depthFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private Stopwatch bodyReaderTimer = null;
        private void initializeKinect()
        {
            kinectSensor = KinectSensor.GetDefault();
            if(kinectSensor != null) { kinectSensor.Open(); }
            if (bodyReaderTimer == null) { bodyReaderTimer = new Stopwatch(); bodyReaderTimer.Start(); }
            bodyThread = new Thread(bodyTrackerThread);
            bodyThread.Start();
            depthThread = new Thread(depthTrackerThread);
            depthThread.Start();
        }
        private void depthTrackerThread()
        {
            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            if (depthFrameReader != null) { depthFrameReader.FrameArrived += depthReader_FrameArrived; }
        }
        private void bodyTrackerThread()
        {
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            if (bodyFrameReader != null) { bodyFrameReader.FrameArrived += bodyReader_FrameArrived; }
        }
        private void depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    globalCoordinates.DepthPixelArray = ToImageBitmap(frame);
                    imageStream.Image = globalCoordinates.DepthPixelArray;
                }
            }
        }
        private Bitmap ToImageBitmap(DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;
            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * 32 / 8];
            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            int mapDepthToByte = maxDepth / 256;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? (depth/mapDepthToByte) : 0);
                pixelData[colorIndex++] = intensity; // B
                pixelData[colorIndex++] = intensity; // G
                pixelData[colorIndex++] = intensity; // R
                pixelData[colorIndex++] = 255; // Alpha
            }
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }
        private void bodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                int _bodyCount = 0;
                
                for(int i = 0; i< bodies.Length; i++) { 
                
                    if (bodies[i].IsTracked)
                    {
                        _bodyCount++;
                        IReadOnlyDictionary<JointType, Joint> joints = bodies[i].Joints;
                        if(joints != null)
                        {
                            globalCoordinates.skeletonStructure.Clear();
                            foreach(KeyValuePair<JointType, Joint> pair in joints)
                            {
                                globalCoordinates.skeletonStructure.Add(pair.Key, pair.Value);
                            }

                        }

                        Joint midSpine = joints[JointType.Head];
                        
                        globalCoordinates.midSpinePixel = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(midSpine.Position);
                        globalCoordinates.HandX = midSpine.Position.X;
                        globalCoordinates.HandY = midSpine.Position.Y;
                        globalCoordinates.HandZ = midSpine.Position.Z;
                        globalCoordinates.HandState = bodies[i].HandRightState.ToString();
                    }
                }
                globalCoordinates.BodyCount = _bodyCount;
                if(_bodyCount < 1)
                {
                    globalCoordinates.HandState = "Lasso";
                }
            }
            // Timer Stuff
            globalCoordinates.AverageCounter++;
            globalCoordinates.CurrentLoop = bodyReaderTimer.Elapsed.TotalMilliseconds;
            bodyReaderTimer.Restart();
        }

        // Graphics
        private Form bboxWindow;
        public void initializeGraphics()
        {
            imageStream.Paint += new PaintEventHandler(this.imageStream_Paint);
            if(bboxWindow == null)
            {
                bboxWindow = new Form();
                bboxWindow.TransparencyKey = System.Drawing.Color.LimeGreen;
                bboxWindow.BackColor = System.Drawing.Color.LimeGreen;
                bboxWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
                bboxWindow.TopMost = true;
                Size _size = new Size(0, 0);
                bboxWindow.MinimumSize = _size;
                bboxWindow.Show();
            }
        }
        private void imageStream_Paint(object sender, PaintEventArgs e)
        {
           
            Graphics g = e.Graphics;
            float x = 0;
            float y = 0;
            float width = 10;
            float height = 10;
            DepthSpacePoint tempPenTip= new DepthSpacePoint();
            Font drawFont = new Font("Consolas", 12);
            SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Purple);
            Rectangle tempRectange = new Rectangle((int)x, (int)y, (int)width, (int)height);
            if(globalCoordinates.skeletonStructure.ContainsKey(JointType.HandRight) && globalCoordinates.skeletonStructure.ContainsKey(JointType.HandLeft))
            {
                DepthSpacePoint tempHead = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(globalCoordinates.skeletonStructure[JointType.HandRight].Position);
                DepthSpacePoint tempSpineBase = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(globalCoordinates.skeletonStructure[JointType.HandLeft].Position);
                g.DrawLine(System.Drawing.Pens.Yellow, tempHead.X, tempHead.Y, tempSpineBase.X, tempSpineBase.Y);

            }

            foreach (KeyValuePair<JointType, Joint> pair in globalCoordinates.skeletonStructure)
            {
                tempPenTip = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(pair.Value.Position);
                x = (tempPenTip.X <= -900)?-900:(tempPenTip.X>=900?900:tempPenTip.X);
                y = (tempPenTip.Y <= -900) ? -900 : (tempPenTip.Y >= 900 ? 900 : tempPenTip.Y);
                g.DrawEllipse(System.Drawing.Pens.Red, x, y, width, height);
                g.DrawString(pair.Value.Position.Z.ToString() + "m", drawFont, drawBrush, x, y);
            }
            if(globalCoordinates.skeletonStructure.Count > 0)
            {
                float[] extremeties;
                calculateBBox(ref globalCoordinates.skeletonStructure, out extremeties);
                float bboxWidth = extremeties[1] - extremeties[0];
                float bboxHeight = extremeties[3] - extremeties[2];
                g.DrawRectangle(System.Drawing.Pens.Blue, tempRectange);
                g.DrawRectangle(System.Drawing.Pens.Green, extremeties[0]-(bboxWidth/8), extremeties[2] - (bboxHeight / 8), bboxWidth*(float)1.3, bboxHeight*(float)1.2);
                g.DrawString("ID#2936", drawFont, drawBrush, extremeties[0] - (bboxWidth / 8), extremeties[2] - (bboxHeight / 7));
                Point tempPoint = this.PointToScreen(new Point((int)(extremeties[0] - (bboxWidth / 8)), (int)(extremeties[2] - (bboxHeight / 5))));
               
                bboxWindow.Left = tempPoint.X;
                bboxWindow.Top = tempPoint.Y;
                bboxWindow.Width = (int)(bboxWidth * (float)1.3);
                bboxWindow.Height = (int)(bboxHeight * (float)1.2);
            }



        }
        private void calculateBBox(ref Dictionary<JointType, Joint> _skeletonStructure, out float[] _extremeties)
        {
            DepthSpacePoint tempPenTip = new DepthSpacePoint();
            _extremeties = new float[] { 0, 0, 0, 0 };
            //JointType xLow, xHigh, yLow, yHigh = new JointType();
            List<float> xValue = new List<float>(), yValue = new List<float>();
            foreach (KeyValuePair<JointType, Joint> pair in _skeletonStructure)
            {
                tempPenTip = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(pair.Value.Position);
                xValue.Add(tempPenTip.X);
                yValue.Add(tempPenTip.Y);
            }
            _extremeties[0] = xValue.ToArray().Min();
            _extremeties[1] = xValue.ToArray().Max();
            _extremeties[2] = yValue.ToArray().Min();
            _extremeties[3] = yValue.ToArray().Max();

        }

        // Open.Arms
        OpenArms[] clientObjects = new OpenArms[10];
        Stopwatch writerAssistTimer;
        Stopwatch readerAssistTimer;
        BackgroundWorker writerAssistant;
        BackgroundWorker readerAssistant;
        public void initializeBackgroundComs()
        {
            if(writerAssistant == null)
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
        private void startClient_Click(object sender, EventArgs e)
        {
            int clientsConnected = 0;
            for(int i = 0; i < clientObjects.Length; i++)
            {
                if(clientObjects[i] == null)
                {
                    clientObjects[i] = new SmoothStream.OpenArms("172.31.1.147", 7000);
                    if (clientObjects[i].isConnected())
                    {
                        clientsConnected++;
                    }
                }
            }
            if (clientsConnected == clientObjects.Length)
            {
                MessageBox.Show("all connected");
                initializeBackgroundComs();
            }
            else
            {
                for (int i = 0; i < clientObjects.Length; i++)
                {
                    clientObjects[i] = null;
                }
            }

        }
        private void readCurrentAxisThread()
        {
            OpenArms tempClient = getNextClient();
            OpenArms.E6Axis currentAxis = new OpenArms.E6Axis(tempClient.readVariable("$AXIS_ACT_MEAS"));
            tempClient.IsActive = false;
            currentAxis.updateCurrentE6Axis(ref globalCoordinates);
        }
        private void readCurrentPosThread()
        {
            OpenArms.E6Pos currentPos = new OpenArms.E6Pos(clientObjects[2].readVariable("$POS_ACT"));
            currentPos.updateCurrentE6Pos(ref globalCoordinates);
        }
        private void writerAssistant_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //Debug.WriteLine(timerGui.Elapsed.TotalMilliseconds.ToString());
            //timerGui.Restart();

            writerAssistTimer.Restart();
            BackgroundWorker worker = (BackgroundWorker)sender;

            OpenArms.E6Pos tempE6 = new OpenArms.E6Pos();
            tempE6.updateE6Pos(calculateDelta(), globalCoordinates);
            OpenArms tempClient = getNextClient();
            tempClient.writeVariable("MYPOS", tempE6.currentValue);
            tempClient.IsActive = false;
            globalCoordinates.SentMessage = tempE6.currentValue;
        }
        private void writerAssistant_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            while (writerAssistTimer.Elapsed.TotalMilliseconds < 3.5);
            BackgroundWorker worker = (BackgroundWorker)sender;
            globalCoordinates.WriterBackgroundTimer = writerAssistTimer.Elapsed.TotalMilliseconds;
            worker.RunWorkerAsync();
            updateGUI();
            
        }
        private void readerAssistant_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //Debug.WriteLine(timerGui.Elapsed.TotalMilliseconds.ToString());
            //timerGui.Restart();

            readerAssistTimer.Restart();
            BackgroundWorker worker = (BackgroundWorker)sender;

            readCurrentAxisThread();
            readCurrentPosThread();
        }
        private void readerAssistant_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            while (readerAssistTimer.Elapsed.TotalMilliseconds < 7);
            BackgroundWorker worker = (BackgroundWorker)sender;
            globalCoordinates.ReaderBackgroundTimer = readerAssistTimer.Elapsed.TotalMilliseconds;
            worker.RunWorkerAsync();
        }
        private OpenArms getNextClient()
        {
            
            foreach(OpenArms client in clientObjects)
            {
                if (!client.IsActive)
                {
                    client.IsActive = true;
                    return client;
                }
            }
            MessageBox.Show("Clients Busy");
            return clientObjects[11];
            
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
            
            
            if(globalCoordinates.HandState != "Lasso")
            {
                if (globalCoordinates.CurrentA3 >= 120)
                {
                    deltaValues[0] = 0.5;
                }
                else
                {

                    deltaValues[0] = (globalCoordinates.HandZ * 0);
                }
                
                deltaValues[1] = (globalCoordinates.HandX * posAcceleration);
                deltaValues[2] = (globalCoordinates.HandY * posAcceleration);
                deltaValues[3] = (globalCoordinates.HandX * rotAcceleration);
                deltaValues[4] = (globalCoordinates.HandY * rotAcceleration);
                deltaValues[5] = (globalCoordinates.HandZ * rotAcceleration);
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
            globalCoordinates.ArmX = deltaValues[0];
            globalCoordinates.ArmY = deltaValues[1];
            globalCoordinates.ArmZ = deltaValues[2];
            globalCoordinates.ArmA = deltaValues[3];
            globalCoordinates.ArmB = deltaValues[4];
            globalCoordinates.ArmC = deltaValues[5];

            return deltaValues;
        }

        // Global
        SharedMemorySpace globalCoordinates;
        Stopwatch timerGui = new Stopwatch();
        public void initializeGlobals()
        {
            if(globalCoordinates == null) { globalCoordinates = new SharedMemorySpace(); }
        }
        private void updateGUI()
        {
            imageStream.Image = globalCoordinates.DepthPixelArray;
            txtCounter.Text = globalCoordinates.AverageCounter.ToString();
            txtLooper.Text = globalCoordinates.CurrentLoop.ToString();
            averageTime.Text = globalCoordinates.AverageTimePerRun.ToString();
            txtMidSpineX.Text = globalCoordinates.HandX.ToString("#.###");
            txtMidSpineY.Text = globalCoordinates.HandY.ToString("#.###");
            txtMidSpineZ.Text = globalCoordinates.HandZ.ToString("#.###");
            txtArmX.Text = globalCoordinates.ArmX.ToString();
            txtArmY.Text = globalCoordinates.ArmY.ToString();
            txtArmZ.Text = globalCoordinates.ArmZ.ToString();
            txtArmA.Text = globalCoordinates.ArmA.ToString();
            txtArmB.Text = globalCoordinates.ArmB.ToString();
            txtArmC.Text = globalCoordinates.ArmC.ToString();
            txtCurrentA1.Text = globalCoordinates.CurrentA1.ToString();
            txtCurrentA2.Text = globalCoordinates.CurrentA2.ToString();
            txtCurrentA3.Text = globalCoordinates.CurrentA3.ToString();
            txtCurrentA4.Text = globalCoordinates.CurrentA4.ToString();
            txtCurrentA5.Text = globalCoordinates.CurrentA5.ToString();
            txtCurrentA6.Text = globalCoordinates.CurrentA6.ToString();
            txtCurrentX.Text = globalCoordinates.CurrentX.ToString();
            txtCurrentY.Text = globalCoordinates.CurrentY.ToString();
            txtCurrentZ.Text = globalCoordinates.CurrentZ.ToString();
            txtCurrentA.Text = globalCoordinates.CurrentA.ToString();
            txtCurrentB.Text = globalCoordinates.CurrentB.ToString();
            txtCurrentC.Text = globalCoordinates.CurrentC.ToString();
            rightHandState.Text = globalCoordinates.HandState.ToString();
            txtWriterTimer.Text = globalCoordinates.WriterBackgroundTimer.ToString();
            txtReaderTimer.Text = globalCoordinates.ReaderBackgroundTimer.ToString();
            txtBodyCount.Text = globalCoordinates.BodyCount.ToString();
            

        }
        private void KinectInterface_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (depthThread.IsAlive)
            {
                MessageBox.Show("depthThread is open, closing...");
                depthThread.Abort();
            }
            if (bodyThread.IsAlive)
            {
                MessageBox.Show("bodyThread is open, closing...");
                depthThread.Abort();
            }
            if (kinectSensor.IsOpen)
            {
                MessageBox.Show("Kinect is open, closing...");
                kinectSensor.Close();
            }
            Application.Exit();
        }

        public class SharedMemorySpace
        {
            public Dictionary<JointType, Joint> skeletonStructure = new Dictionary<JointType, Joint>();
            public DepthSpacePoint midSpinePixel = new DepthSpacePoint();
            private Bitmap depthPixelArray;
            private double totalTime = 0;
            private double currentLoop = 0;
            private double averageTimePerRun = 0;
            private int averageCounter = 0;

            private int bodyCount = 0;
            public double[] max = new double[6] { 0, 0, 0, 0, 0, 0 };
            private double handX = 0;
            private double handY = 0;
            private double handZ = 0;
            private double armX = 0;
            private double armY = 0;
            private double armZ = 0;
            private double armA = 0;
            private double armB = 0;
            private double armC = 0;
            private double currentA1 = 0;
            private double currentA2 = 0;
            private double currentA3 = 0;
            private double currentA4 = 0;
            private double currentA5 = 0;
            private double currentA6 = 0;
            private double currentX = 0;
            private double currentY = 0;
            private double currentZ = 0;
            private double currentA = 0;
            private double currentB = 0;
            private double currentC = 0;
            private string handState = "None";
            private double writerBackgroundTimer = 0;
            private double readerBackgroundTimer = 0;
            private string sentMessage = "None";

            public string SentMessage
            {
                get { return sentMessage;  }
                set { sentMessage = value; }
            }
            public string HandState
            {
                get { return handState; }
                set { handState = value; }
            }
            public double HandX
            {
                get { return handX; }
                set {
                   
                    handX = value; }
            }
            public double HandY
            {
                get { return handY; }
                set {
                    
                   
                    handY = value; }
            }
            public double HandZ
            {
                get { return handZ; }
                set {
                    
                    handZ = value; }
            }

            
            public double ArmX
            {
                get { return armX; }
                set {
                    if (armX > max[0])
                    {
                        max[0] = armX;
                    }
                    armX = value; }
            }
            public double ArmY
            {
                get { return armY; }
                set {
                    if (armY > max[1])
                    {
                        max[1] = armY;
                    }
                    armY = value; }
            }
            public double ArmZ
            {
                get { return armZ; }
                set {
                    if (armZ > max[2])
                    {
                        max[2] = armZ;
                    }
                    armZ = value; }
            }
            public double ArmA
            {
                get
                {
                    return armA;
                }

                set
                {
                    armA = value;
                }
            }
            public double ArmB
            {
                get
                {
                    return armB;
                }

                set
                {
                    armB = value;
                }
            } 
            public double ArmC
            {
                get
                {
                    return armC;
                }

                set
                {
                    armC = value;
                }
            }

            public double CurrentA1
            {
                get
                {
                    return currentA1;
                }

                set
                {
                    currentA1 = value;
                }
            }
            public double CurrentA2
            {
                get
                {
                    return currentA2;
                }

                set
                {
                    currentA2 = value;
                }
            }
            public double CurrentA3
            {
                get
                {
                    return currentA3;
                }

                set
                {
                    currentA3 = value;
                }
            }
            public double CurrentA4
            {
                get
                {
                    return currentA4;
                }

                set
                {
                    currentA4 = value;
                }
            }
            public double CurrentA5
            {
                get
                {
                    return currentA5;
                }

                set
                {
                    currentA5 = value;
                }
            }
            public double CurrentA6
            {
                get
                {
                    return currentA6;
                }

                set
                {
                    currentA6 = value;
                }
            }

            public double WriterBackgroundTimer
            {
                get { return writerBackgroundTimer; }
                set { writerBackgroundTimer = value; }
            }

            public int BodyCount
            {
                get
                {
                    return bodyCount;
                }

                set
                {
                    bodyCount = value;
                }
            }

            public double CurrentX
            {
                get
                {
                    return currentX;
                }

                set
                {
                    currentX = value;
                }
            }

            public double CurrentY
            {
                get
                {
                    return currentY;
                }

                set
                {
                    currentY = value;
                }
            }

            public double CurrentZ
            {
                get
                {
                    return currentZ;
                }

                set
                {
                    currentZ = value;
                }
            }

            public double CurrentA
            {
                get
                {
                    return currentA;
                }

                set
                {
                    currentA = value;
                }
            }

            public double CurrentB
            {
                get
                {
                    return currentB;
                }

                set
                {
                    currentB = value;
                }
            }

            public double CurrentC
            {
                get
                {
                    return currentC;
                }

                set
                {
                    currentC = value;
                }
            }

            public double AverageTimePerRun
            {
                get
                {
                    return averageTimePerRun;
                }
            }

            public int AverageCounter
            {
                get
                {
                    return averageCounter;
                }

                set
                {
                    averageCounter = value;
                }
            }

            public double CurrentLoop
            {
                get
                {
                    return currentLoop;
                }

                set
                {
                    currentLoop = value;
                    totalTime = totalTime + value;
                    averageTimePerRun = totalTime/averageCounter;
                }
            }

            public Bitmap DepthPixelArray
            {
                get
                {
                    return depthPixelArray;
                }

                set
                {
                    depthPixelArray = value;
                }
            }

            public double ReaderBackgroundTimer
            {
                get
                {
                    return readerBackgroundTimer;
                }

                set
                {
                    readerBackgroundTimer = value;
                }
            }
        }
    }
}
