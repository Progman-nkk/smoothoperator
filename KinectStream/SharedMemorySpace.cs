using System.Collections.Generic;
using System.Drawing;
using Microsoft.Kinect;
using System.Windows.Forms;

namespace SmoothStream
{
    public class SharedMemorySpace
    {
        public SharedMemorySpace(SmoothStream.SmoothOperator mainForm)
        {
            MainForm = mainForm;
        }

        // Global
        private Painter _thePainter = null;
        internal Painter ThePainter
        {
            get
            {
                return _thePainter;
            }

            set
            {
                _thePainter = value;
            }
        }
        private CameraManager _TheManager = null;
        internal CameraManager TheManager
        {
            get
            {
                return _TheManager;
            }

            set
            {
                _TheManager = value;
            }
        }
        private SmoothOperator _mainForm = null;
        public SmoothOperator MainForm
        {
            get
            {
                return _mainForm;
            }

            set
            {
                _mainForm = value;
            }
        }

        // CameraManager - ColorFrames
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

        // CameraManager - BodyFrames
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
        private Dictionary<JointType, Joint> skeletonStructure = new Dictionary<JointType, Joint>();
        public Dictionary<JointType, Joint> SkeletonStructure
        {
            get
            {
                lock (skeletonStructure)
                {
                    return skeletonStructure;
                }
            }

            set
            {
                lock (skeletonStructure)
                {
                    skeletonStructure = value;
                }
            }
        }
        public string bodyID = "none";
        private string handState = "None";
        public string HandState
        {
            get { return handState; }
            set { handState = value; }
        }

        private double totalTime = 0;
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
        private double averageBodyReaderLoop = 0;
        public double AverageBodyReaderLoop
        {
            get
            {
                return averageBodyReaderLoop;
            }
        }

        public double[] max = new double[6] { 0, 0, 0, 0, 0, 0 };
        private double jointToTrackX = 0;
        private double jointToTrackY = 0;
        private double jointToTrackZ = 0;
        public double JointToTrackX
        {
            get { return jointToTrackX; }
            set
            {

                jointToTrackX = value;
            }
        }
        public double JointToTrackY
        {
            get { return jointToTrackY; }
            set
            {


                jointToTrackY = value;
            }
        }
        public double JointToTrackZ
        {
            get { return jointToTrackZ; }
            set
            {

                jointToTrackZ = value;
            }
        }

        //OpenArms
        private double writerBackgroundTimer = 0;
        public double WriterBackgroundTimer
        {
            get { return writerBackgroundTimer; }
            set { writerBackgroundTimer = value; }
        }
        private double readerBackgroundTimer = 0;
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

        private double currentA1 = 0;
        private double currentA2 = 0;
        private double currentA3 = 0;
        private double currentA4 = 0;
        private double currentA5 = 0;
        private double currentA6 = 0;
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

        private double currentX = 0;
        private double currentY = 0;
        private double currentZ = 0;
        private double currentA = 0;
        private double currentB = 0;
        private double currentC = 0;
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
        
        //Deltas
        private double armX = 0;
        private double armY = 0;
        private double armZ = 0;
        private double armA = 0;
        private double armB = 0;
        private double armC = 0;
        public double ArmX
        {
            get { return armX; }
            set
            {
                if (armX > max[0])
                {
                    max[0] = armX;
                }
                armX = value;
            }
        }
        public double ArmY
        {
            get { return armY; }
            set
            {
                if (armY > max[1])
                {
                    max[1] = armY;
                }
                armY = value;
            }
        }
        public double ArmZ
        {
            get { return armZ; }
            set
            {
                if (armZ > max[2])
                {
                    max[2] = armZ;
                }
                armZ = value;
            }
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

        
    }
}

