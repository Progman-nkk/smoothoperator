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

        private BackgroundWorker guiWorker;
        private Stopwatch guiStopwatch;
        private CameraManager TheManager = null;
        private Painter ThePainter = null;
        public SharedMemorySpace globalCoordinates = null;
        private OpenArms TheArm = null;

        public void initializeGlobals(SmoothOperator mainForm)
        {
            if (globalCoordinates == null) { globalCoordinates = new SharedMemorySpace(mainForm); }
            if (TheManager == null) { TheManager = new CameraManager(ref globalCoordinates); }
            //if (ThePainter == null) { ThePainter = new Painter(ref globalCoordinates); }
            if (TheArm == null) { TheArm = new OpenArms("172.31.1.147", 7000, ref globalCoordinates); }
            if(guiWorker == null) {
                guiStopwatch = new Stopwatch();
                guiWorker = new BackgroundWorker();
                guiWorker.DoWork += guiWorker_DoWork;
                guiWorker.RunWorkerCompleted += guiWorker_RunWorkerCompleted;
                guiWorker.RunWorkerAsync();
            }
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
        private void startClient_Click(object sender, EventArgs e){ TheArm.initializeClients(); }

        private void guiWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(15);
        }
        private void guiWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            updateGUI();
            guiWorker.RunWorkerAsync();
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

