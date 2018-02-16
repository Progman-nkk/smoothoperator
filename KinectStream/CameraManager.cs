using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;

namespace SmoothStream
{
    public class CameraManager
    {
        private KinectSensor kinectSensor = null;
        public KinectSensor KinectSensor
        {
            get
            {
                return kinectSensor;
            }

            set
            {
                kinectSensor = value;
            }
        }

        private Thread bodyThread = null;
        private Thread depthThread = null;
        private Thread colorThread = null;
        private DepthFrameReader depthFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private ColorFrameReader colorFrameReader = null;
        private Body[] bodies = null;
        private Stopwatch bodyReaderTimer = null;

        private byte[] pixelData = null;
        private int width = 0;
        private int height = 0;
        public Thread DepthThread
        {
            get
            {
                return depthThread;
            }

            set
            {
                depthThread = value;
            }
        }
        public Thread BodyThread
        {
            get
            {
                return bodyThread;
            }

            set
            {
                bodyThread = value;
            }
        }
        public Thread ColorThread
        {
            get
            {
                return colorThread;
            }

            set
            {
                colorThread = value;
            }
        }
        private Bitmap pixelArray;
        public Bitmap PixelArray
        {
            get
            {
                return pixelArray;
            }

            set
            {
                pixelArray = value;
            }
        }
        private int bodyCount = 0;
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
        private Body[] bodyBag = new Body[6];
        public Body[] BodyBag
        {
            get
            {
                return bodyBag;
            }

            set
            {
                bodyBag = value;
            }
        }
        private int counterBodyReaderLoop = 0;
        public int CounterBodyReaderLoop
        {
            get
            {
                return counterBodyReaderLoop;
            }

            set
            {
                counterBodyReaderLoop = value;
            }
        }
        private double currentBodyReaderLoop = 0;
        public double CurrentBodyReaderLoop
        {
            get
            {
                return currentBodyReaderLoop;
            }

            set
            {
                currentBodyReaderLoop = value;
                totalTime = totalTime + value;
                averageBodyReaderLoop = totalTime / counterBodyReaderLoop;
            }
        }
        private double averageBodyReaderLoop = 0;
        public double AverageBodyReaderLoop
        {
            get
            {
                return averageBodyReaderLoop;
            }
        }
        private double totalTime = 0;

        public CameraManager()
        {
            KinectSensor = KinectSensor.GetDefault();
            if (KinectSensor != null) { KinectSensor.Open(); }
            if (bodyReaderTimer == null) { bodyReaderTimer = new Stopwatch(); bodyReaderTimer.Start(); }
            BodyThread = new Thread(bodyTrackerThread);
            BodyThread.Start();
            //DepthThread = new Thread(depthTrackerThread);
            //DepthThread.Start();
            ColorThread = new Thread(colorTrackerThread);
            ColorThread.Start();
        }

        private void depthTrackerThread()
        {
            depthFrameReader = KinectSensor.DepthFrameSource.OpenReader();
            if (depthFrameReader != null) { depthFrameReader.FrameArrived += depthReader_FrameArrived; }
        }
        private void bodyTrackerThread()
        {
            bodyFrameReader = KinectSensor.BodyFrameSource.OpenReader();
            if (bodyFrameReader != null) { bodyFrameReader.FrameArrived += bodyReader_FrameArrived; }
        }
        private void colorTrackerThread()
        {
            colorFrameReader = KinectSensor.ColorFrameSource.OpenReader();
            if (colorFrameReader != null) { colorFrameReader.FrameArrived += colorReader_FrameArrived; }
        }
        private void colorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    width = frame.FrameDescription.Width;
                    height = frame.FrameDescription.Height;
                    if (pixelData == null)
                    {
                        pixelData = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];
                    }

                    Bitmap bitmap = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    frame.CopyConvertedFrameDataToArray(pixelData, ColorImageFormat.Bgra);
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, 1920, 1080), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
                    bitmap.UnlockBits(bitmapData);
                    PixelArray = (Bitmap)bitmap.Clone();
                    //bitmap.Dispose();

                }
            }
        }
        private void depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    PixelArray = ToImageBitmap(frame);
                   
                }
            }
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
                for (int i = 0; i < bodies.Length; i++)
                {

                    if (bodies[i].IsTracked)
                    {
                        bool isInBodyBag = false;
                        for(int t = 0; t < BodyBag.Length; t++)
                        {
                            if (bodies[i].TrackingId == BodyBag[t]?.TrackingId)
                            {
                                isInBodyBag = true;
                                BodyBag[t] = bodies[i];
                            }
                        }
                        if(!isInBodyBag){
                            for (int h = 0; h < BodyBag.Length; h++)
                            {
                                if (BodyBag[h]?.IsTracked != true)
                                {
                                    BodyBag[h] = bodies[i];
                                    
                                    break;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < BodyBag.Length; i++)
                {
                    if(BodyBag[i]?.IsTracked == false) { BodyBag[i] = null; }
                }
                
            }
            // Timer Stuff
            CounterBodyReaderLoop++;
            CurrentBodyReaderLoop = bodyReaderTimer.Elapsed.TotalMilliseconds;
            bodyReaderTimer.Restart();
        }
        private Bitmap ToImageBitmap(ref ColorFrame frame)
        {
            width = frame.FrameDescription.Width;
            height = frame.FrameDescription.Height;
            if (pixelData == null)
            {
                pixelData = new byte[width * height * 4];
            }
            frame.CopyConvertedFrameDataToArray(pixelData, ColorImageFormat.Bgra);
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
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
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / mapDepthToByte) : 0);
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
    }
}
