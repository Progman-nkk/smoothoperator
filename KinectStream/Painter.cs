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
    public class Painter
    {
        private Form bboxWindow;
        private SharedMemorySpace _globalCoordinates = null;
        Graphics g;
        int x = 10;
        int y = 10;
        int width = 10;
        int height = 10;
        ColorSpacePoint tempPenTip = new ColorSpacePoint();
        Font drawFont = new Font("Consolas", 12);
        Rectangle tempRectange;
        SolidBrush drawBrush;
        System.Drawing.Pen myPen;
        Dictionary<JointType, Joint> _tempSkeleton;
        private Thread maskThread;
        public Painter(ref SharedMemorySpace globalCoordinates)
        {
            _globalCoordinates = globalCoordinates;
            _tempSkeleton = new Dictionary<JointType, Joint>();
            _globalCoordinates._imageStreamReference = _globalCoordinates.MainForm.ImageStream;
            _globalCoordinates.MainForm.ImageStream.Paint += new PaintEventHandler(imageStream_Paint);
            tempRectange = new Rectangle(x, y, width, height);
            drawBrush = new SolidBrush(System.Drawing.Color.Purple);
            myPen = new System.Drawing.Pen(System.Drawing.Color.Red);
            //maskThread = new Thread(maskPainterThread);
            //maskThread.Start();
        }
        private void maskPainterThread()
        {
            while (true)
            {
            if (bboxWindow == null) { createMask(); }
            
            bboxWindow.Left = _globalCoordinates.MaskLeft;
            bboxWindow.Top = _globalCoordinates.MaskTop;
            bboxWindow.Width = _globalCoordinates.MaskWidth;
            bboxWindow.Height = _globalCoordinates.MaskHeight;
            Thread.Sleep(125);
            }
        }
        private void createMask()
        {
                bboxWindow = new Form();
                bboxWindow.TransparencyKey = System.Drawing.Color.Green;
                bboxWindow.BackColor = System.Drawing.Color.LimeGreen;
                
                Size _size = new Size(0, 0);
                bboxWindow.MinimumSize = _size;
                bboxWindow.TopMost = true;
                
                bboxWindow.Show();
        }
        private void imageStream_Paint(object sender, PaintEventArgs e)
        {
            g = e.Graphics;
            tempRectange.X = x;
            tempRectange.Y = y;
            tempRectange.Width = width;
            tempRectange.Height = height;

            lock (_tempSkeleton)
            {
                lock (_globalCoordinates.SkeletonStructure)
                {

                foreach (KeyValuePair<JointType, Joint> pair in _globalCoordinates.SkeletonStructure)
                {
                    if (!_tempSkeleton.ContainsKey(pair.Key))
                    {
                        if (pair.Value.TrackingState == TrackingState.Tracked || pair.Value.TrackingState == TrackingState.Inferred)
                        {
                            _tempSkeleton.Add(pair.Key, pair.Value);
                        }
                    }
                }
            
            foreach (KeyValuePair<JointType, Joint> pair in _tempSkeleton)
            {
                tempPenTip = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(pair.Value.Position);
                x = double.IsInfinity(tempPenTip.X) ? x : Convert.ToInt32(tempPenTip.X);
                y = double.IsInfinity(tempPenTip.Y) ? y : Convert.ToInt32(tempPenTip.Y);
                g.DrawEllipse(myPen, x, y, width, height);
                g.DrawString(pair.Value.Position.Z.ToString() + "m", drawFont, drawBrush, x, y);
            }
            }
          }
            if (_tempSkeleton.Count > 0)
            {
                int[] extremeties;
                calculateBBox(ref _tempSkeleton, out extremeties);
                int bboxWidth = extremeties[1] - extremeties[0];
                int bboxHeight = extremeties[3] - extremeties[2];
                g.DrawString(_globalCoordinates.bodyID, drawFont, drawBrush, extremeties[0], extremeties[2]);
                Point tempPoint = _globalCoordinates.MainForm.PointToScreen(new Point(Convert.ToInt32(extremeties[0] - (bboxWidth / 8)), Convert.ToInt32(extremeties[2] - (bboxHeight / 7))));
                Pen _myPen = new System.Drawing.Pen(System.Drawing.Color.Red, 3);
                g.DrawRectangle(_myPen, Convert.ToInt32(extremeties[0]), Convert.ToInt32(extremeties[2]), Convert.ToInt32(bboxWidth), Convert.ToInt32(bboxHeight));

                _globalCoordinates.MaskLeft = Convert.ToInt32(tempPoint.X-(tempPoint.X*8));
                _globalCoordinates.MaskTop = Convert.ToInt32(tempPoint.Y- (tempPoint.Y * 9));
                _globalCoordinates.MaskWidth = Convert.ToInt32(bboxWidth * (float)1.3);
                _globalCoordinates.MaskHeight = Convert.ToInt32(bboxHeight * (float)1.2);
            }
            _tempSkeleton.Clear();
        }
        private void calculateBBox(ref Dictionary<JointType, Joint> _skeletonStructure, out int[] _extremeties)
        {
            ColorSpacePoint tempPenTip = new ColorSpacePoint();
            _extremeties = new int[4];
            //JointType xLow, xHigh, yLow, yHigh = new JointType();
            List<float> xValue = new List<float>();
            List<float> yValue = new List<float>();
            
            foreach (KeyValuePair<JointType, Joint> pair in _skeletonStructure)
            {
                if(pair.Value.TrackingState == TrackingState.Tracked || pair.Value.TrackingState == TrackingState.Inferred)
                {
                    tempPenTip = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(pair.Value.Position);
                    xValue.Add(tempPenTip.X);
                    yValue.Add(tempPenTip.Y);
                }
            }
            if (xValue.Count > 0 && yValue.Count > 0)
            {
                double minX = xValue.ToArray().Min();
                double maxX = xValue.ToArray().Max();
                double minY = yValue.ToArray().Min();
                double maxY = yValue.ToArray().Max();
                _extremeties[0] = double.IsInfinity(minX) ? _extremeties[0] : Convert.ToInt32(minX);
                _extremeties[1] = double.IsInfinity(maxX) ? _extremeties[1] : Convert.ToInt32(maxX);
                _extremeties[2] = double.IsInfinity(minY) ? _extremeties[2] : Convert.ToInt32(minY);
                _extremeties[3] = double.IsInfinity(maxY) ? _extremeties[3] : Convert.ToInt32(maxY);
            }

        }
    }
}
