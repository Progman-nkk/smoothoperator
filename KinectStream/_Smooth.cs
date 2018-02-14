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
            //System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            InitializeComponent();
            initializeGlobals(this);
        }
        private CameraManager TheManager = null;
        private Painter ThePainter = null;
        public SharedMemorySpace globalCoordinates = null;
        public void initializeGlobals(SmoothStream.SmoothOperator mainForm)
        {
            if (globalCoordinates == null) { globalCoordinates = new SharedMemorySpace(mainForm); }
            if (ThePainter == null) { ThePainter = new Painter(ref globalCoordinates); }
            if (TheManager == null) { TheManager = new CameraManager(ref globalCoordinates); }
        }
        private void SmoothStream_FormClosing(object sender, FormClosingEventArgs e)
        {
           
            if (TheManager.KinectSensor.IsOpen)
            {
                MessageBox.Show("Kinect is open, closing...");
                TheManager.KinectSensor.Close();
            }
            
            Application.Exit();
        }
        private void SmoothOperator_Load(object sender, EventArgs e)
        {

        }
    }
}

