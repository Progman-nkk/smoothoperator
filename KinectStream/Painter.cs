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
    class Painter
    {
        private Form bboxWindow;
        private SharedMemorySpace _globalCoordinates = null;

        public Painter(ref SharedMemorySpace globalCoordinates)
        {

            _globalCoordinates = globalCoordinates;
                _globalCoordinates._imageStreamReference = _globalCoordinates.MainForm.ImageStream;
                _globalCoordinates.MainForm.ImageStream.Paint += new PaintEventHandler(imageStream_Paint);
            
            if (bboxWindow == null)
            {
                bboxWindow = new Form();
                bboxWindow.TransparencyKey = System.Drawing.Color.LimeGreen;
                bboxWindow.BackColor = System.Drawing.Color.LimeGreen;
                bboxWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
                bboxWindow.TopMost = true;
                bboxWindow.TopLevel = true;
                Size _size = new Size(0, 0);
                bboxWindow.MinimumSize = _size;
                bboxWindow.Show(_globalCoordinates.MainForm);
            }

        }
        private void imageStream_Paint(object sender, PaintEventArgs e)
        {

            Graphics g = e.Graphics;
            float x = 0;
            float y = 0;
            float width = 10;
            float height = 10;
            DepthSpacePoint tempPenTip = new DepthSpacePoint();
            Font drawFont = new Font("Consolas", 12);
            SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Purple);
            Rectangle tempRectange = new Rectangle((int)x, (int)y, (int)width, (int)height);
            if (_globalCoordinates.skeletonStructure.ContainsKey(JointType.HandRight) && _globalCoordinates.skeletonStructure.ContainsKey(JointType.HandLeft))
            {
                DepthSpacePoint tempHead = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(_globalCoordinates.skeletonStructure[JointType.HandRight].Position);
                DepthSpacePoint tempSpineBase = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(_globalCoordinates.skeletonStructure[JointType.HandLeft].Position);
                g.DrawLine(System.Drawing.Pens.Yellow, tempHead.X, tempHead.Y, tempSpineBase.X, tempSpineBase.Y);

            }

            foreach (KeyValuePair<JointType, Joint> pair in _globalCoordinates.skeletonStructure)
            {
                tempPenTip = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(pair.Value.Position);
                x = (tempPenTip.X <= -900) ? -900 : (tempPenTip.X >= 900 ? 900 : tempPenTip.X);
                y = (tempPenTip.Y <= -900) ? -900 : (tempPenTip.Y >= 900 ? 900 : tempPenTip.Y);
                g.DrawEllipse(System.Drawing.Pens.Red, x, y, width, height);
                g.DrawString(pair.Value.Position.Z.ToString() + "m", drawFont, drawBrush, x, y);
            }
            if (_globalCoordinates.skeletonStructure.Count > 0)
            {
                float[] extremeties;
                calculateBBox(ref _globalCoordinates.skeletonStructure, out extremeties);
                float bboxWidth = extremeties[1] - extremeties[0];
                float bboxHeight = extremeties[3] - extremeties[2];
                g.DrawRectangle(System.Drawing.Pens.Blue, tempRectange);
                g.DrawRectangle(System.Drawing.Pens.Green, extremeties[0] - (bboxWidth / 8), extremeties[2] - (bboxHeight / 8), bboxWidth * (float)1.3, bboxHeight * (float)1.2);
                g.DrawString("ID#2936", drawFont, drawBrush, extremeties[0] - (bboxWidth / 8), extremeties[2] - (bboxHeight / 7));
                Point tempPoint = _globalCoordinates.MainForm.PointToScreen(new Point((int)(extremeties[0] - (bboxWidth / 8)), (int)(extremeties[2] - (bboxHeight / 5))));

                bboxWindow.Left = tempPoint.X;
                bboxWindow.Top = tempPoint.Y;
                bboxWindow.Width = (int)(bboxWidth * (float)1.3);
                bboxWindow.Height = (int)(bboxHeight * (float)1.2);
                bboxWindow.TopMost = true;
                bboxWindow.TopLevel = true;
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
                tempPenTip = _globalCoordinates.TheManager.KinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(pair.Value.Position);
                xValue.Add(tempPenTip.X);
                yValue.Add(tempPenTip.Y);
            }
            _extremeties[0] = xValue.ToArray().Min();
            _extremeties[1] = xValue.ToArray().Max();
            _extremeties[2] = yValue.ToArray().Min();
            _extremeties[3] = yValue.ToArray().Max();

        }
         
    }
}
