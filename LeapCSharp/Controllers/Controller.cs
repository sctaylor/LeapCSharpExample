using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LeapCSharp
{
    public class Controller : INotifyPropertyChanged
    {

        public enum ControlType
        {
            tWii,
            tLeap,
            tJoy
        }

        public enum MotionType
        {
            Position,
            Pose,
            Speed,
            Velocity
        }

        public enum Motions
        {
            mLeftRight,             // Left/Right position
            mUpDown,                // Up/Down position
            mForwardBack,           // Near/Far position
            mTiltForwardBack,       // Wiimote or pen
            mTiltLeftRight,         // Wiimote or pen
            mTwistLeftRight,        // Rotation of the hand, like airplane YAW
            mTurnLeftRight,         // Rotation of the hand, like airplane YAW
            mPressure,              // Pen
            mGrab,                  // Fist Motion
            mPinch,                 // Thumb and second finger pinch
            mDistance,              // Like two hand distance apart on a leap motion
            mSpread,                // Degree of spread-out fingers
            mSpeed,                 // Overall velocoity in any direction.
            mSpeedLeftRight,        // Absolute speed 0-1
            mSpeedUpDown,           // Absolute speed 0-1
            mSpeedForwardBack,      // Absolute speed 0-1
            mVelLeftRight,          // Directional speed 0-1, 0.5 is still
            mVelUpDown,             // Directional speed 0-1, 0.5 is still
            mVelForwardBack,        // Directional speed 0-1, 0.5 is still
        }

        public static Dictionary<Motions, MotionType> dictMotionTypes = new Dictionary<Motions, MotionType>()
        {
            { Motions.mLeftRight, MotionType.Position }, 
            { Motions.mUpDown, MotionType.Position }, 
            { Motions.mForwardBack, MotionType.Position }, 
            { Motions.mTiltForwardBack, MotionType.Pose },        
            { Motions.mTiltLeftRight, MotionType.Pose },        
            { Motions.mTwistLeftRight, MotionType.Pose },     
            { Motions.mTurnLeftRight, MotionType.Pose },        
            { Motions.mPressure, MotionType.Pose },     
            { Motions.mGrab, MotionType.Pose },     
            { Motions.mPinch, MotionType.Pose },     
            { Motions.mDistance, MotionType.Position },    
            { Motions.mSpread, MotionType.Position },    
            { Motions.mSpeed, MotionType.Speed },
            { Motions.mSpeedLeftRight, MotionType.Speed },
            { Motions.mSpeedUpDown, MotionType.Speed },
            { Motions.mSpeedForwardBack, MotionType.Speed },
            { Motions.mVelLeftRight, MotionType.Velocity },
            { Motions.mVelUpDown, MotionType.Velocity },
            { Motions.mVelForwardBack, MotionType.Velocity }
        };

        public bool bNoReading { get; set; } = true;
        public MotionState msCurrentState { get; set; } = new MotionState();
        public MotionState msRawCurrentState { get; set; } = new MotionState();

        public MotionRange mrFullRange { get; set; } = new MotionRange();
        public MotionRange mrDefaultRange { get; set; } = new MotionRange();
        public MotionRange mrCurrentRange { get; set; } = new MotionRange();

        public MotionRange mrWorkingSpace { get; set; } = new MotionRange();
        public int iCurrentWorkingSpace = 0;
        public class MotionState
        {
            // Normalized in range 0..1 for all -- negative number means "undefined"

            public float[] fValue = new float[Enum.GetNames(typeof(Motions)).Length];        // Used to raise a motion event back to subscribers like rulespecs
            public bool bIsLeft = true;
            public float fFrameRate = 0;

            public MotionState()
            {
                for ( int i=0; i < fValue.Length; i++)
                {
                    fValue[i] = -1; // Undefined
                }
            }

        }

        public class MotionRange
        {
            // Normalized in range 0..1 for all -- negative number means "undefined"

            public float[] fMin = new float[Enum.GetNames(typeof(Motions)).Length];        
            public float[] fMax = new float[Enum.GetNames(typeof(Motions)).Length];

            public MotionRange()
            {
                for (int i = 0; i < fMin.Length; i++)
                {
                    fMin[i] = 0; // Undefined
                    fMax[i] = 1; // Undefined
                }
            }

            public MotionRange(MotionRange copy)
            {
                for (int i = 0; i < fMin.Length; i++)
                {
                    fMin[i] = copy.fMin[i]; // Undefined
                    fMax[i] = copy.fMax[i]; // Undefined
                }
            }

            public void CopyFrom(MotionRange copy)
            {
                for (int i = 0; i < fMin.Length; i++)
                {
                    fMin[i] = copy.fMin[i]; // Undefined
                    fMax[i] = copy.fMax[i]; // Undefined
                }
            }

        }


        double lMilliSecsLastSend = 0;
        double lMilliSecsBetweenSends = 1000 / 500;

        // Motion Event Handler Stuff
        public event EventHandler<MotionEventArgs> MotionEvent;
        protected virtual void raiseMotionEvent( MotionEventArgs me)
        {
            long lMilliSecsNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if ((lMilliSecsNow - lMilliSecsLastSend) >= lMilliSecsBetweenSends)    
            {

                MotionEvent?.Invoke(this, me);

                // Console.WriteLine("Raising motion event after " + (long)(lMilliSecsNow - lMilliSecsLastSend) + " ms");
                lMilliSecsLastSend = lMilliSecsNow;
            }
            else
            {
                // Console.WriteLine("Not time yet... Now,Last,Between: " + lMilliSecsNow + " " + lMilliSecsLastSend + " " + lMilliSecsBetweenSends);
            }
        }

        // Raw Motion Event Handler Stuff -- meant only for leap calibration, gets FULL/Raw x y and z STABALIZED positional measurements
        public event EventHandler<MotionEventArgs> RawMotionEvent;
        protected virtual void raiseRawMotionEvent(MotionEventArgs me)
        {
                RawMotionEvent?.Invoke(this, me);
        }


        public class MotionEventArgs : EventArgs
        {
            public MotionEventArgs(MotionState ms)
            {
                msState = ms;
            }

            public MotionState msState { get; set; }
        }


        public class Move
        {
            public Motions mMotion { get; set; }
            public string sDesc { get; set; }
            public string sImagePath { get; set; }
            public string sHint { get; set; }

            public bool IsPositionType { get { return (dictMotionTypes[mMotion] == MotionType.Position); } }
            public bool IsPoseType { get { return (dictMotionTypes[mMotion] == MotionType.Pose); } }
            public bool IsSpeedType { get { return (dictMotionTypes[mMotion] == MotionType.Speed); } }
            public bool IsVelocityType { get { return (dictMotionTypes[mMotion] == MotionType.Velocity); } }
        }

        public string sName { get; set; }
        public string sImagePath { get; set; }
        public ControlType tType { get; set; }

        private bool pvtDisconnected = true;
        public bool bDisconnected
        {
            get { return pvtDisconnected; }
            set
            {
                pvtDisconnected = value;
                NotifyPropertyChanged();
            }
        }


        public Move[] mMoves { get; set; }


        public Controller(  )
        {

        }

        public virtual void Disconnect()
        {
            return;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

}
