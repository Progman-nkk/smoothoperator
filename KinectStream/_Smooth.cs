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
            if (TheManager == null) { TheManager = new CameraManager(); }
            if (globalCoordinates == null) { globalCoordinates = new SharedMemorySpace(mainForm, TheManager); }
            if (ThePainter == null) { ThePainter = new Painter(ref globalCoordinates); }
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
            //if (TheManager.DepthThread.IsAlive)
            //{
            //    MessageBox.Show("depthThread is open, closing...");
            //    TheManager.DepthThread.Abort();
            //}
            //if (TheManager.BodyThread.IsAlive)
            //{
            //    MessageBox.Show("bodyThread is open, closing...");
            //    TheManager.DepthThread.Abort();
            //}
            //if (TheManager.KinectSensor.IsOpen)
            //{
            //    MessageBox.Show("Kinect is open, closing...");
            //    TheManager.KinectSensor.Close();
            //}
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
            ImageStream.Image = TheManager.PixelArray;
            txtCounter.Text = TheManager.CounterBodyReaderLoop.ToString();
            txtLooper.Text = TheManager.CurrentBodyReaderLoop.ToString();
            averageTime.Text = TheManager.AverageBodyReaderLoop.ToString();
            txtMidSpineX.Text = globalCoordinates.JointToTrackX.ToString("#.###");
            txtMidSpineY.Text = globalCoordinates.JointToTrackY.ToString("#.###");
            txtMidSpineZ.Text = globalCoordinates.JointToTrackZ.ToString("#.###");
            txtArmX.Text = globalCoordinates.ArmX.ToString();
            txtArmY.Text = globalCoordinates.ArmY.ToString();
            txtArmZ.Text = globalCoordinates.ArmZ.ToString();
            txtArmA.Text = globalCoordinates.ArmA.ToString();
            txtArmB.Text = globalCoordinates.ArmB.ToString();
            txtArmC.Text = globalCoordinates.ArmC.ToString();
            txtCurrentA1.Text = TheArm.CurrentAxis.a1Value.ToString();
            txtCurrentA2.Text = TheArm.CurrentAxis.a2Value.ToString();
            txtCurrentA3.Text = TheArm.CurrentAxis.a3Value.ToString();
            txtCurrentA4.Text = TheArm.CurrentAxis.a4Value.ToString();
            txtCurrentA5.Text = TheArm.CurrentAxis.a5Value.ToString();
            txtCurrentA6.Text = TheArm.CurrentAxis.a6Value.ToString();
            txtCurrentX.Text = TheArm.CurrentPos.xValue.ToString();
            txtCurrentY.Text = TheArm.CurrentPos.yValue.ToString();
            txtCurrentZ.Text = TheArm.CurrentPos.xValue.ToString();
            txtCurrentA.Text = TheArm.CurrentPos.aValue.ToString();
            txtCurrentB.Text = TheArm.CurrentPos.bValue.ToString();
            txtCurrentC.Text = TheArm.CurrentPos.cValue.ToString();
            rightHandState.Text = globalCoordinates.HandState.ToString();
            txtWriterTimer.Text = TheArm.WriterBackgroundTimer.ToString();
            txtReaderTimer.Text = TheArm.ReaderBackgroundTimer.ToString();
            txtBodyCount.Text = TheManager.BodyCount.ToString();


        }

    }
}

