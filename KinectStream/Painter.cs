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
        private SharedMemorySpace _globalCoordinates = null;
        Graphics g;
        int x = 10;
        int y = 10;
        int width = 10;
        int height = 10;
        ColorSpacePoint tempPenTip = new ColorSpacePoint();
        Font drawFont = new Font("Consolas", 10);
        Rectangle tempRectange;
        SolidBrush drawBrush;
        System.Drawing.Pen myPen;
        Dictionary<JointType, Joint> _tempSkeleton;

        public Painter(ref SharedMemorySpace globalCoordinates)
        {
            _globalCoordinates = globalCoordinates;
            _tempSkeleton = new Dictionary<JointType, Joint>();
            _globalCoordinates.MainForm.ImageStream.Paint += new PaintEventHandler(imageStream_Paint);
            tempRectange = new Rectangle(x, y, width, height);
            drawBrush = new SolidBrush(System.Drawing.Color.Purple);
            myPen = new System.Drawing.Pen(System.Drawing.Color.Red);
        }
        private void imageStream_Paint(object sender, PaintEventArgs e)
        {
            g = e.Graphics;
            tempRectange.X = x;
            tempRectange.Y = y;
            tempRectange.Width = width;
            tempRectange.Height = height;

            // Collection was modified - figure out a way to lock joints
            int _bodyCount = 0;
            for(int i = 0; i < _globalCoordinates.TheManager.BodyBag.Length; i++)
            {
                 if(_globalCoordinates.TheManager.BodyBag[i] != null)
                {
                    _bodyCount++;
                IReadOnlyDictionary<JointType, Joint> joints = _globalCoordinates.TheManager.BodyBag[i].Joints;
                foreach (KeyValuePair<JointType, Joint> pair in joints)
                {
                    tempPenTip = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(pair.Value.Position);
                    x = double.IsInfinity(tempPenTip.X) ? x : Convert.ToInt32(tempPenTip.X / 2);
                    y = double.IsInfinity(tempPenTip.Y) ? y : Convert.ToInt32(tempPenTip.Y / 2);
                    g.DrawEllipse(myPen, x, y, width, height);
                    //g.DrawString(pair.Value.Position.Z.ToString() + "m", drawFont, drawBrush, x, y);
                }
                if (joints.Count > 0)
                {
                    int[] extremeties;
                    calculateBBox(ref joints, out extremeties);
                    int bboxWidth = extremeties[1] - extremeties[0];
                    int bboxHeight = extremeties[3] - extremeties[2];
                    g.DrawString(_globalCoordinates.TheManager.BodyBag[i].TrackingId.ToString(), drawFont, drawBrush, extremeties[0] / 2, extremeties[2] / 2);
                    Point tempPoint = _globalCoordinates.MainForm.PointToScreen(new Point(Convert.ToInt32(extremeties[0] - (bboxWidth / 8)), Convert.ToInt32(extremeties[2] - (bboxHeight / 7))));
                    Pen _myPen = new System.Drawing.Pen(System.Drawing.Color.Red, 3);
                    g.DrawRectangle(_myPen, Convert.ToInt32(extremeties[0] / 2), Convert.ToInt32(extremeties[2] / 2), Convert.ToInt32(bboxWidth / 2), Convert.ToInt32(bboxHeight / 2));
                }
                }
            }
            _globalCoordinates.TheManager.BodyCount = _bodyCount;
            
        }
        private void calculateBBox(ref IReadOnlyDictionary<JointType, Joint> _skeletonStructure, out int[] _extremeties)
        {
            ColorSpacePoint tempPenTip = new ColorSpacePoint();
            _extremeties = new int[4];
            //JointType xLow, xHigh, yLow, yHigh = new JointType();
            List<float> xValue = new List<float>();
            List<float> yValue = new List<float>();

            foreach (KeyValuePair<JointType, Joint> pair in _skeletonStructure)
            {
                if (pair.Value.TrackingState == TrackingState.Tracked || pair.Value.TrackingState == TrackingState.Inferred)
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
