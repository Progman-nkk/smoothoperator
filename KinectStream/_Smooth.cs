using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
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
            initializeGlobals(this);
        }

        private CameraManager TheManager = null;
        private Painter ThePainter = null;
        public SharedMemorySpace globalCoordinates;

        public void initializeGlobals(SmoothStream.SmoothOperator mainForm)
        {
            if (globalCoordinates == null) { globalCoordinates = new SharedMemorySpace(mainForm); }
            if (TheManager == null) { TheManager = new CameraManager(ref globalCoordinates); }
            if (ThePainter == null) { ThePainter = new Painter(ref globalCoordinates); }
        }
        private void SmoothStream_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (TheManager.DepthThread.IsAlive)
            {
                MessageBox.Show("depthThread is open, closing...");
                TheManager.DepthThread.Abort();
            }
            if (TheManager.BodyThread.IsAlive)
            {
                MessageBox.Show("bodyThread is open, closing...");
                TheManager.DepthThread.Abort();
            }
            if (TheManager.KinectSensor.IsOpen)
            {
                MessageBox.Show("Kinect is open, closing...");
                TheManager.KinectSensor.Close();
            }
            Application.Exit();
        }

        

        OpenArms[] clientObjects = new OpenArms[10];
        Stopwatch writerAssistTimer;
        Stopwatch readerAssistTimer;
        BackgroundWorker writerAssistant;
        BackgroundWorker readerAssistant;
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
        private void startClient_Click(object sender, EventArgs e)
        {
            int clientsConnected = 0;
            for (int i = 0; i < clientObjects.Length; i++)
            {
                if (clientObjects[i] == null)
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
            while (writerAssistTimer.Elapsed.TotalMilliseconds < 3.5) ;
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
            while (readerAssistTimer.Elapsed.TotalMilliseconds < 7) ;
            BackgroundWorker worker = (BackgroundWorker)sender;
            globalCoordinates.ReaderBackgroundTimer = readerAssistTimer.Elapsed.TotalMilliseconds;
            worker.RunWorkerAsync();
        }
        private OpenArms getNextClient()
        {

            foreach (OpenArms client in clientObjects)
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


            if (globalCoordinates.HandState != "Lasso")
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
        private void updateGUI()
        {
            //ImageStream.Image = globalCoordinates.DepthPixelArray;
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

    }
}

