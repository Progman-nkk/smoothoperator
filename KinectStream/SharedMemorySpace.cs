using System.Collections.Generic;
using System.Drawing;
using Microsoft.Kinect;
using System.Windows.Forms;

namespace SmoothStream
{
    public class SharedMemorySpace
    {
        public SharedMemorySpace(SmoothStream.SmoothOperator mainForm, CameraManager TheManager)
        {
            MainForm = mainForm;
            _theManager = TheManager;
        }

        // Global
        private CameraManager _theManager = null;
        internal CameraManager TheManager
        {
            get
            {
                return _theManager;
            }

            set
            {
                _theManager = value;
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

        private string handState = "None";
        public string HandState
        {
            get { return handState; }
            set { handState = value; }
        }

        //OpenArms

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
        
        //Deltas

        public double[] max = new double[6] { 0, 0, 0, 0, 0, 0 };
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

