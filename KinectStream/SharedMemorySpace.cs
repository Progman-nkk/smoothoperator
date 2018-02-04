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
        private Painter _thePainter = null;
        private CameraManager _TheManager = null;
        private SmoothOperator _mainForm = null;
        public PictureBox _imageStreamReference = null;
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
            get { return sentMessage; }
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
            set
            {

                handX = value;
            }
        }
        public double HandY
        {
            get { return handY; }
            set
            {


                handY = value;
            }
        }
        public double HandZ
        {
            get { return handZ; }
            set
            {

                handZ = value;
            }
        }


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
                averageTimePerRun = totalTime / averageCounter;
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
    }
}

