using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using Helpers;
using Leap;
using LeapCSharp.Properties;

namespace LeapCSharp
{
    public class ControllerLeap : Controller
    {
        private LeapMotion lmc { get; set; } = null;


        public enum Parts
        {
            hPalm,
            hThumb,
            hPointer,
            hMiddle,
            hFourth,
            hPinky
        }

        public int[] iTracking { get; set; } = { 5, 0, 0, 0, 0, 0 };    // Hand and finger weights


        private MovAvg maSpeed = new  MovAvg(Settings.Default.iSpeedSmoother);
        private MovAvg maSpeedx = new MovAvg(Settings.Default.iSpeedSmoother);
        private MovAvg maSpeedy = new MovAvg(Settings.Default.iSpeedSmoother);
        private MovAvg maSpeedz = new MovAvg(Settings.Default.iSpeedSmoother);
        private MovAvg maVelx   = new MovAvg(Settings.Default.iSpeedSmoother);
        private MovAvg maVely   = new MovAvg(Settings.Default.iSpeedSmoother);
        private MovAvg maVelz   = new MovAvg(Settings.Default.iSpeedSmoother);

        private MovAvg maFrameRate = new MovAvg(100);


        private int pvtSpeedSmoother = 5;
        public int iSpeedSmoother
        {
            get
            {
                return pvtSpeedSmoother;
            }
            set
            {
                pvtSpeedSmoother = value;
                maSpeed.ChangeSize(value);
                maSpeedx.ChangeSize(value);
                maSpeedy.ChangeSize(value);
                maSpeedz.ChangeSize(value);
                maVelx.ChangeSize(value);
                maVely.ChangeSize(value);
                maVelz.ChangeSize(value);

                iLookBack = ((int)(value * 0.2)).Clamp<int>(2, 6);
            }
        }

        private int pvtMaxSpeed = 100;
        public int iMaxSpeed
        {
            get
            {
                return pvtMaxSpeed;
            }
            set
            {
                pvtMaxSpeed = value;
                mrCurrentRange.fMin[(int)Motions.mSpeed] = 0f;
                mrCurrentRange.fMax[(int)Motions.mSpeed] = pvtMaxSpeed;
                mrCurrentRange.fMin[(int)Motions.mSpeedLeftRight] = 0f;
                mrCurrentRange.fMax[(int)Motions.mSpeedLeftRight] = pvtMaxSpeed;
                mrCurrentRange.fMin[(int)Motions.mSpeedUpDown] = 0f;
                mrCurrentRange.fMax[(int)Motions.mSpeedUpDown] = pvtMaxSpeed;
                mrCurrentRange.fMin[(int)Motions.mSpeedForwardBack] = 0f;
                mrCurrentRange.fMax[(int)Motions.mSpeedForwardBack] = pvtMaxSpeed;
                mrCurrentRange.fMin[(int)Motions.mVelLeftRight] = -pvtMaxSpeed;
                mrCurrentRange.fMax[(int)Motions.mVelLeftRight] = pvtMaxSpeed;
                mrCurrentRange.fMin[(int)Motions.mVelUpDown] = -pvtMaxSpeed;
                mrCurrentRange.fMax[(int)Motions.mVelUpDown] = pvtMaxSpeed;
                mrCurrentRange.fMin[(int)Motions.mVelForwardBack] = -pvtMaxSpeed;
                mrCurrentRange.fMax[(int)Motions.mVelForwardBack] = pvtMaxSpeed;

            }
        }


        private class HistPos
        {
            public float x = 0;
            public float y = 0;
            public float z = 0;
            public double seconds = -1;
        }
        private HistPos[] History = new HistPos[1000];
        private int iHistLoc = 0;
        private Stopwatch swHist = new Stopwatch();

        public ControllerLeap( )
        {
            for ( int i=0; i < History.Length; i++)
            {
                History[i] = new HistPos();
            }
            swHist.Start();

            mMoves = new Move[] { new Move { mMotion = Motions.mLeftRight, sDesc = "Move Left/Right", sImagePath = "",
                                         sHint = "Hand moving left and right" },
                              new Move { mMotion = Motions.mUpDown, sDesc = "Move Up/Down", sImagePath = "",
                                         sHint = "Hand moving up and down" },
                              new Move { mMotion = Motions.mForwardBack, sDesc = "Move Fore/Back", sImagePath = "",
                                         sHint = "Hand moving forward and back" },
                              new Move { mMotion = Motions.mTwistLeftRight, sDesc = "Twist Left/Right", sImagePath = "",
                                         sHint = "Tilting hand left and right" },
                              new Move { mMotion = Motions.mTiltForwardBack, sDesc = "Tilt Up/Down", sImagePath = "",
                                         sHint = "Tilting hand up (back) or down (forward)" },
                              new Move { mMotion = Motions.mTurnLeftRight, sDesc = "Turn Left/Right", sImagePath = "",
                                         sHint = "Turning or rotating hand left and right (yaw)" },
                              new Move { mMotion = Motions.mGrab , sDesc = "Open/Grab", sImagePath = "",
                                         sHint = "Opening left hand or grabbing, making a fist" },
                              new Move { mMotion = Motions.mPinch , sDesc = "Pinch", sImagePath = "",
                                         sHint = "Pinching with thumb and pointer finger" },
                              new Move { mMotion = Motions.mSpread , sDesc = "Spread", sImagePath = "",
                                         sHint = "Fingers spread or close together" },
                              new Move { mMotion = Motions.mSpeed , sDesc = "Speed", sImagePath = "",
                                         sHint = "Hand speed in any direction" },
                              new Move { mMotion = Motions.mSpeedLeftRight , sDesc = "Speed Left/Right", sImagePath = "",
                                         sHint = "Hand speed moving left or right" },
                              new Move { mMotion = Motions.mSpeedUpDown , sDesc = "Speed Up/Down", sImagePath = "",
                                         sHint = "Hand speed moving up or down" },
                              new Move { mMotion = Motions.mSpeedForwardBack , sDesc = "Speed Fore/Back", sImagePath = "",
                                         sHint = "Hand speed moving forward and back" },
                              new Move { mMotion = Motions.mVelLeftRight , sDesc = "Velocity Left/Right", sImagePath = "",
                                         sHint = "Hand velocity moving left or right, centered when still" },
                              new Move { mMotion = Motions.mVelUpDown , sDesc = "Velocity Up/Down", sImagePath = "",
                                         sHint = "Hand velocity moving up or down, centered when still" },
                              new Move { mMotion = Motions.mVelForwardBack , sDesc = "Velocity Fore/Back", sImagePath = "",
                                         sHint = "Hand velocity moving forward and back, centered when still" }
            };

            sName = "LeapMotion";
            sImagePath = "";
            tType = ControlType.tLeap;
            bDisconnected = true;

            // Calibration
            mrFullRange.fMin[(int)Motions.mLeftRight] = -350f;
            mrFullRange.fMax[(int)Motions.mLeftRight] = 350f;
            mrFullRange.fMin[(int)Motions.mUpDown] = 100f;
            mrFullRange.fMax[(int)Motions.mUpDown] = 700f;
            mrFullRange.fMin[(int)Motions.mForwardBack] = -350f;
            mrFullRange.fMax[(int)Motions.mForwardBack] = 350f;
            mrFullRange.fMin[(int)Motions.mTwistLeftRight] = -1f;
            mrFullRange.fMax[(int)Motions.mTwistLeftRight] = 1f;
            mrFullRange.fMin[(int)Motions.mTiltForwardBack] = -1;
            mrFullRange.fMax[(int)Motions.mTiltForwardBack] = 1f;
            mrFullRange.fMin[(int)Motions.mTurnLeftRight] = -1f;
            mrFullRange.fMax[(int)Motions.mTurnLeftRight] = 1f;
            mrFullRange.fMin[(int)Motions.mGrab] = 0f;
            mrFullRange.fMax[(int)Motions.mGrab] = 3.14159f;
            mrFullRange.fMin[(int)Motions.mPinch] = 20f;
            mrFullRange.fMax[(int)Motions.mPinch] = 75f;
            mrFullRange.fMin[(int)Motions.mSpread] = 70f;
            mrFullRange.fMax[(int)Motions.mSpread] = 270f;
            mrFullRange.fMin[(int)Motions.mSpeed] = 0f;
            mrFullRange.fMax[(int)Motions.mSpeed] = 1200f;
            mrFullRange.fMin[(int)Motions.mSpeedLeftRight] = 0f;
            mrFullRange.fMax[(int)Motions.mSpeedLeftRight] = 1200f;
            mrFullRange.fMin[(int)Motions.mSpeedUpDown] = 0f;
            mrFullRange.fMax[(int)Motions.mSpeedUpDown] = 1200f;
            mrFullRange.fMin[(int)Motions.mSpeedForwardBack] = 0f;
            mrFullRange.fMax[(int)Motions.mSpeedForwardBack] = 1200f;
            mrFullRange.fMin[(int)Motions.mVelLeftRight] = -1200f;
            mrFullRange.fMax[(int)Motions.mVelLeftRight] = 1200f;
            mrFullRange.fMin[(int)Motions.mVelUpDown] = -1200f;
            mrFullRange.fMax[(int)Motions.mVelUpDown] = 1200f;
            mrFullRange.fMin[(int)Motions.mVelForwardBack] = -1200f;
            mrFullRange.fMax[(int)Motions.mVelForwardBack] = 1200f;

            mrDefaultRange.fMin[(int)Motions.mLeftRight] = -130;
            mrDefaultRange.fMax[(int)Motions.mLeftRight] = 130;
            mrDefaultRange.fMin[(int)Motions.mUpDown] = 100;
            mrDefaultRange.fMax[(int)Motions.mUpDown] = 360;
            mrDefaultRange.fMin[(int)Motions.mForwardBack] = -130;
            mrDefaultRange.fMax[(int)Motions.mForwardBack] = 130;
            mrDefaultRange.fMin[(int)Motions.mTwistLeftRight] = -0.95f;
            mrDefaultRange.fMax[(int)Motions.mTwistLeftRight] = 0.95f;
            mrDefaultRange.fMin[(int)Motions.mTiltForwardBack] = -0.95f;
            mrDefaultRange.fMax[(int)Motions.mTiltForwardBack] = 0.95f;
            mrDefaultRange.fMin[(int)Motions.mTurnLeftRight] = -0.7f;
            mrDefaultRange.fMax[(int)Motions.mTurnLeftRight] = 0.7f;
            mrDefaultRange.fMin[(int)Motions.mGrab] = 0f;
            mrDefaultRange.fMax[(int)Motions.mGrab] = 3.14159f;
            mrDefaultRange.fMin[(int)Motions.mPinch] = 20f;
            mrDefaultRange.fMax[(int)Motions.mPinch] = 75f;
            mrDefaultRange.fMin[(int)Motions.mSpread] = 70f;
            mrDefaultRange.fMax[(int)Motions.mSpread] = 270f;
            mrDefaultRange.fMin[(int)Motions.mSpeed] = 0f;
            mrDefaultRange.fMax[(int)Motions.mSpeed] = 600f;
            mrDefaultRange.fMin[(int)Motions.mSpeedLeftRight] = 0f;
            mrDefaultRange.fMax[(int)Motions.mSpeedLeftRight] = 600f;
            mrDefaultRange.fMin[(int)Motions.mSpeedUpDown] = 0f;
            mrDefaultRange.fMax[(int)Motions.mSpeedUpDown] = 600f;
            mrDefaultRange.fMin[(int)Motions.mSpeedForwardBack] = 0f;
            mrDefaultRange.fMax[(int)Motions.mSpeedForwardBack] = 600f;
            mrDefaultRange.fMin[(int)Motions.mVelLeftRight] = -600f;
            mrDefaultRange.fMax[(int)Motions.mVelLeftRight] = 600f;
            mrDefaultRange.fMin[(int)Motions.mVelUpDown] = -600f;
            mrDefaultRange.fMax[(int)Motions.mVelUpDown] = 600f;
            mrDefaultRange.fMin[(int)Motions.mVelForwardBack] = -600f;
            mrDefaultRange.fMax[(int)Motions.mVelForwardBack] = 600f;

            mrCurrentRange.CopyFrom(mrDefaultRange);
            mrCurrentRange.fMin[(int)Motions.mLeftRight] = Settings.Default.rLeapMinX;
            mrCurrentRange.fMax[(int)Motions.mLeftRight] = Settings.Default.rLeapMaxX;
            mrCurrentRange.fMin[(int)Motions.mUpDown] = Settings.Default.rLeapMinY;
            mrCurrentRange.fMax[(int)Motions.mUpDown] = Settings.Default.rLeapMaxY;
            mrCurrentRange.fMin[(int)Motions.mForwardBack] = Settings.Default.rLeapMinZ;
            mrCurrentRange.fMax[(int)Motions.mForwardBack] = Settings.Default.rLeapMaxZ;
            mrCurrentRange.fMin[(int)Motions.mSpeed] = 0f;
            mrCurrentRange.fMax[(int)Motions.mSpeed] = Settings.Default.iMaxSpeed;
            mrCurrentRange.fMin[(int)Motions.mSpeedLeftRight] = 0f;
            mrCurrentRange.fMax[(int)Motions.mSpeedLeftRight] = Settings.Default.iMaxSpeed;
            mrCurrentRange.fMin[(int)Motions.mSpeedUpDown] = 0f;
            mrCurrentRange.fMax[(int)Motions.mSpeedUpDown] = Settings.Default.iMaxSpeed;
            mrCurrentRange.fMin[(int)Motions.mSpeedForwardBack] = 0f;
            mrCurrentRange.fMax[(int)Motions.mSpeedForwardBack] = Settings.Default.iMaxSpeed;
            mrCurrentRange.fMin[(int)Motions.mVelLeftRight] = -Settings.Default.iMaxSpeed;
            mrCurrentRange.fMax[(int)Motions.mVelLeftRight] = Settings.Default.iMaxSpeed;
            mrCurrentRange.fMin[(int)Motions.mVelUpDown] = -Settings.Default.iMaxSpeed;
            mrCurrentRange.fMax[(int)Motions.mVelUpDown] = Settings.Default.iMaxSpeed;
            mrCurrentRange.fMin[(int)Motions.mVelForwardBack] = -Settings.Default.iMaxSpeed;
            mrCurrentRange.fMax[(int)Motions.mVelForwardBack] = Settings.Default.iMaxSpeed;


            iTracking = new int[] {
                Settings.Default.iLeapTrackPalm,
                Settings.Default.iLeapTrackThumb,
                Settings.Default.iLeapTrackPointer,
                Settings.Default.iLeapTrackMiddle,
                Settings.Default.iLeapTrackFourth,
                Settings.Default.iLeapTrackPinky,
             };

            // Now find some leaps!!

            try
            {
                lmc = new LeapMotion();
            }
            catch ( Exception e )
            {
                MessageBox.Show("Error connecting to LeapMotion: " + e.Message, "LeapMotion Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (lmc != null)
            {
                // Ask for frames even in the background - this is important!
                lmc.SetPolicy(LeapMotion.PolicyFlag.POLICY_BACKGROUND_FRAMES);
                lmc.SetPolicy(LeapMotion.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);

                lmc.ClearPolicy(LeapMotion.PolicyFlag.POLICY_OPTIMIZE_HMD); // NOT head mounted
                lmc.ClearPolicy(LeapMotion.PolicyFlag.POLICY_IMAGES);       // NO images, please

                // Subscribe to connected/not messages
                lmc.Connect += Lmc_Connect;
                lmc.Disconnect += Lmc_Disconnect;
                lmc.FrameReady += Lmc_FrameReady;

                if ( lmc.IsConnected )
                {
                    bDisconnected = false;
                }
                else
                {
                    lmc.StartConnection();
                }
            }
        }

        private void Lmc_Disconnect(object sender, ConnectionLostEventArgs e)
        {
            bDisconnected = true;
        }

        private void Lmc_Connect(object sender, ConnectionEventArgs e)
        {
            // Say we are NO LONGER DISCONNECTED woot :)  Double negatives, anyone?
            bDisconnected = false;
        }

        protected override void raiseMotionEvent(MotionEventArgs me)    // Back to the rules engine for translation 
        {
            base.raiseMotionEvent( me);
        }


        // Speed calculation settings
        private float fSpeed = 0;
        private Leap.Vector vSpeed = new Leap.Vector(0, 0, 0);
        private Leap.Vector vVel   = new Leap.Vector(0, 0, 0);
        private int iLookBack = 2;
        private int iFrameRateLookBack = 10;


        // For hand validation
        private bool bWeSeeValidHands = false;
        private double dExpansionFactor = 0.1;
        private int iTrackHand = 0;

        private void Lmc_FrameReady(object sender, FrameEventArgs e)
        {
            if ( bDisconnected == true )
            {
                bDisconnected = false;
            }

            if (e.frame.Hands.Count > 0 )
            {


                // Figure out which hand to look at --> The first in-range one.
                if (iTrackHand >= e.frame.Hands.Count)
                {
                    iTrackHand = e.frame.Hands.Count-1;
                }

                for ( int i = 0; i < e.frame.Hands.Count; i++ )
                {

                    // Expand the current range by x %
                    if (e.frame.Hands[i].PalmPosition.x >= mrCurrentRange.fMin[(int)Motions.mLeftRight] - dExpansionFactor * Math.Abs(mrCurrentRange.fMax[(int)Motions.mLeftRight] - mrCurrentRange.fMin[(int)Motions.mLeftRight]) &&
                            e.frame.Hands[i].PalmPosition.x <= mrCurrentRange.fMax[(int)Motions.mLeftRight] + dExpansionFactor * Math.Abs(mrCurrentRange.fMax[(int)Motions.mLeftRight] - mrCurrentRange.fMin[(int)Motions.mLeftRight]) &&
                            e.frame.Hands[i].PalmPosition.y >= mrCurrentRange.fMin[(int)Motions.mUpDown] - dExpansionFactor * Math.Abs(mrCurrentRange.fMax[(int)Motions.mUpDown] - mrCurrentRange.fMin[(int)Motions.mUpDown]) &&
                            e.frame.Hands[i].PalmPosition.y <= mrCurrentRange.fMax[(int)Motions.mUpDown] + dExpansionFactor * Math.Abs(mrCurrentRange.fMax[(int)Motions.mUpDown] - mrCurrentRange.fMin[(int)Motions.mUpDown]) &&
                            e.frame.Hands[i].PalmPosition.z >= mrCurrentRange.fMin[(int)Motions.mForwardBack] - dExpansionFactor * Math.Abs(mrCurrentRange.fMax[(int)Motions.mForwardBack] - mrCurrentRange.fMin[(int)Motions.mForwardBack]) &&
                            e.frame.Hands[i].PalmPosition.z <= mrCurrentRange.fMax[(int)Motions.mForwardBack] + dExpansionFactor * Math.Abs(mrCurrentRange.fMax[(int)Motions.mForwardBack] - mrCurrentRange.fMin[(int)Motions.mForwardBack]))
                    {
                        // One is in range.  Track it.  Otherwise continue to track "0" but don't 
                        iTrackHand = i;
                        bWeSeeValidHands = true;
                        break;
                    }

                }

                // ---------------  Catch the RAW current measurements of x/y/z
                try
                {
                    // Set the current raw state --- ** This is just the raw x y and z coordinates **

                    msRawCurrentState.fValue[(int)Motions.mLeftRight] = (float)e.frame.Hands[iTrackHand].PalmPosition.x;
                    msRawCurrentState.fValue[(int)Motions.mUpDown] = (float)e.frame.Hands[iTrackHand].PalmPosition.y;
                    msRawCurrentState.fValue[(int)Motions.mForwardBack] = (float)e.frame.Hands[iTrackHand].PalmPosition.z;
                    msRawCurrentState.fFrameRate = (float)maFrameRate.GetAverage();

                    // Now that the motion state has been updated, raise the event
                    raiseRawMotionEvent(new MotionEventArgs(msRawCurrentState));
                }
                catch (Exception f)
                { Console.Out.Write(f.StackTrace); }
            }
            else
            {
                // Hand count is zero.
                bWeSeeValidHands = false;
            }



            // Continue with rule-based motion only on valid hands.

            if ( !bWeSeeValidHands )
            {
                bNoReading = true;
                raiseMotionEvent(new MotionEventArgs(msCurrentState));
                return;
            }

            bNoReading = false;   // We *have* a reading of hands, duh!


            // Send the non-raw motion :)
            try
            {
                float fTotal=0;
                float fPosX=0, fPosY=0, fPosZ=0;

                for (int i=0; i < 6; i++)
                {
                    fTotal += (float)iTracking[i];
                }

                fPosX += e.frame.Hands[iTrackHand].PalmPosition.x * (float)iTracking[(int)Parts.hPalm];
                fPosX += e.frame.Hands[iTrackHand].Fingers[0].TipPosition.x * (float)iTracking[(int)Parts.hThumb];
                fPosX += e.frame.Hands[iTrackHand].Fingers[1].TipPosition.x * (float)iTracking[(int)Parts.hPointer];
                fPosX += e.frame.Hands[iTrackHand].Fingers[2].TipPosition.x * (float)iTracking[(int)Parts.hMiddle];
                fPosX += e.frame.Hands[iTrackHand].Fingers[3].TipPosition.x * (float)iTracking[(int)Parts.hFourth];
                fPosX += e.frame.Hands[iTrackHand].Fingers[4].TipPosition.x * (float)iTracking[(int)Parts.hPinky];
                fPosX /= fTotal;

                fPosY += e.frame.Hands[iTrackHand].PalmPosition.y * (float)iTracking[(int)Parts.hPalm];
                fPosY += e.frame.Hands[iTrackHand].Fingers[0].TipPosition.y * (float)iTracking[(int)Parts.hThumb];
                fPosY += e.frame.Hands[iTrackHand].Fingers[1].TipPosition.y * (float)iTracking[(int)Parts.hPointer];
                fPosY += e.frame.Hands[iTrackHand].Fingers[2].TipPosition.y * (float)iTracking[(int)Parts.hMiddle];
                fPosY += e.frame.Hands[iTrackHand].Fingers[3].TipPosition.y * (float)iTracking[(int)Parts.hFourth];
                fPosY += e.frame.Hands[iTrackHand].Fingers[4].TipPosition.y * (float)iTracking[(int)Parts.hPinky];
                fPosY /= fTotal;

                fPosZ += e.frame.Hands[iTrackHand].PalmPosition.z * (float)iTracking[(int)Parts.hPalm];
                fPosZ += e.frame.Hands[iTrackHand].Fingers[0].TipPosition.z * (float)iTracking[(int)Parts.hThumb];
                fPosZ += e.frame.Hands[iTrackHand].Fingers[1].TipPosition.z * (float)iTracking[(int)Parts.hPointer];
                fPosZ += e.frame.Hands[iTrackHand].Fingers[2].TipPosition.z * (float)iTracking[(int)Parts.hMiddle];
                fPosZ += e.frame.Hands[iTrackHand].Fingers[3].TipPosition.z * (float)iTracking[(int)Parts.hFourth];
                fPosZ += e.frame.Hands[iTrackHand].Fingers[4].TipPosition.z * (float)iTracking[(int)Parts.hPinky];
                fPosZ /= fTotal;

                History[iHistLoc].x = e.frame.Hands[iTrackHand].PalmPosition.x;
                History[iHistLoc].y = e.frame.Hands[iTrackHand].PalmPosition.y;
                History[iHistLoc].z = e.frame.Hands[iTrackHand].PalmPosition.z;
                History[iHistLoc].seconds = swHist.Elapsed.TotalSeconds;

                fSpeed = (float)maSpeed.AddToAvg(e.frame.Hands[iTrackHand].PalmVelocity.Magnitude);

                double dsec = swHist.Elapsed.TotalSeconds;

                // Framerate
                int iFrameRateLook = (iHistLoc - iFrameRateLookBack) % History.Length;
                iFrameRateLook = iFrameRateLook < 0 ? iFrameRateLook + History.Length : iFrameRateLook;

                if (History[iFrameRateLook].seconds >= 0)
                {
                    msCurrentState.fFrameRate = (float)maFrameRate.AddToAvg(iFrameRateLookBack / ( dsec - History[iFrameRateLook].seconds ));
                }

                int iLook = (iHistLoc - iLookBack) % History.Length;
                iLook = iLook < 0 ? iLook + History.Length : iLook;

                if (true && History[iLook].seconds >= 0)
                {

                    vVel.x = (float)maVelx.AddToAvg(((double)(e.frame.Hands[iTrackHand].PalmPosition.x - History[iLook].x)) / (dsec - History[iLook].seconds));
                    vVel.y = (float)maVely.AddToAvg(((double)(e.frame.Hands[iTrackHand].PalmPosition.y - History[iLook].y)) / (dsec - History[iLook].seconds));
                    vVel.z = (float)maVelz.AddToAvg(((double)(e.frame.Hands[iTrackHand].PalmPosition.z - History[iLook].z)) / (dsec - History[iLook].seconds));
                    vSpeed.x = (float)maSpeedx.AddToAvg(Math.Abs(((double)(e.frame.Hands[iTrackHand].PalmPosition.x - History[iLook].x)) / (dsec - History[iLook].seconds)));
                    vSpeed.y = (float)maSpeedy.AddToAvg(Math.Abs(((double)(e.frame.Hands[iTrackHand].PalmPosition.y - History[iLook].y)) / (dsec - History[iLook].seconds)));
                    vSpeed.z = (float)maSpeedz.AddToAvg(Math.Abs(((double)(e.frame.Hands[iTrackHand].PalmPosition.z - History[iLook].z)) / (dsec - History[iLook].seconds)));

                }
                iHistLoc++;
                iHistLoc %= History.Length;

                float fFingerDist = 0f;

                for (int x = 0; x < 4; x++)
                {
                    fFingerDist += e.frame.Hands[iTrackHand].Fingers[x].TipPosition.DistanceTo(e.frame.Hands[iTrackHand].Fingers[x + 1].TipPosition);
                }



                // Weighted Finger Tracking.
                msCurrentState.fValue[(int)Motions.mLeftRight] = (float)(((fPosX) - (mrCurrentRange.fMin[(int)Motions.mLeftRight])) / (mrCurrentRange.fMax[(int)Motions.mLeftRight] - mrCurrentRange.fMin[(int)Motions.mLeftRight])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mUpDown] = (float)(((fPosY) - (mrCurrentRange.fMin[(int)Motions.mUpDown])) / (mrCurrentRange.fMax[(int)Motions.mUpDown] - mrCurrentRange.fMin[(int)Motions.mUpDown])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mForwardBack] = (float)(((fPosZ) - (mrCurrentRange.fMin[(int)Motions.mForwardBack])) / (mrCurrentRange.fMax[(int)Motions.mForwardBack] - mrCurrentRange.fMin[(int)Motions.mForwardBack])).Clamp(0, 1);

                // Twist and Shout
                msCurrentState.fValue[(int)Motions.mTwistLeftRight] = 1.0f - (float)(((e.frame.Hands[iTrackHand].PalmNormal.x) - (mrCurrentRange.fMin[(int)Motions.mTwistLeftRight])) / (mrCurrentRange.fMax[(int)Motions.mTwistLeftRight] - mrCurrentRange.fMin[(int)Motions.mTwistLeftRight])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mTiltForwardBack] = 1.0f - (float)(((e.frame.Hands[iTrackHand].PalmNormal.z) - (mrCurrentRange.fMin[(int)Motions.mTiltForwardBack])) / (mrCurrentRange.fMax[(int)Motions.mTiltForwardBack] - mrCurrentRange.fMin[(int)Motions.mTiltForwardBack])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mTurnLeftRight] = (float)(((e.frame.Hands[iTrackHand].Direction.Yaw) - (mrCurrentRange.fMin[(int)Motions.mTurnLeftRight])) / (mrCurrentRange.fMax[(int)Motions.mTurnLeftRight] - mrCurrentRange.fMin[(int)Motions.mTurnLeftRight])).Clamp(0, 1);

                // Grab and Pinch and Spread (oh my!)
                msCurrentState.fValue[(int)Motions.mGrab] = (float)(((e.frame.Hands[iTrackHand].GrabAngle) - (mrCurrentRange.fMin[(int)Motions.mGrab])) / (mrCurrentRange.fMax[(int)Motions.mGrab] - mrCurrentRange.fMin[(int)Motions.mGrab])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mPinch] = 1.0f - (float)(((e.frame.Hands[iTrackHand].PinchDistance) - (mrCurrentRange.fMin[(int)Motions.mPinch])) / (mrCurrentRange.fMax[(int)Motions.mPinch] - mrCurrentRange.fMin[(int)Motions.mPinch])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpread] = (float)(((fFingerDist) - (mrCurrentRange.fMin[(int)Motions.mSpread])) / (mrCurrentRange.fMax[(int)Motions.mSpread] - mrCurrentRange.fMin[(int)Motions.mSpread])).Clamp(0, 1);

                // Speed (Leap's calcs!)!
/*
                msCurrentState.fValue[(int)Motions.mSpeed] = (float)(((e.frame.Hands[iTrackHand].PalmVelocity.Magnitude) - (mrCurrentRange.fMin[(int)Motions.mSpeed])) / (mrCurrentRange.fMax[(int)Motions.mSpeed] - mrCurrentRange.fMin[(int)Motions.mSpeed])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpeedLeftRight] = (float)((Math.Abs(e.frame.Hands[iTrackHand].PalmVelocity.x) - (mrCurrentRange.fMin[(int)Motions.mSpeedLeftRight])) / (mrCurrentRange.fMax[(int)Motions.mSpeedLeftRight] - mrCurrentRange.fMin[(int)Motions.mSpeedLeftRight])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpeedUpDown] = (float)((Math.Abs(e.frame.Hands[iTrackHand].PalmVelocity.y) - (mrCurrentRange.fMin[(int)Motions.mSpeedUpDown])) / (mrCurrentRange.fMax[(int)Motions.mSpeedUpDown] - mrCurrentRange.fMin[(int)Motions.mSpeedUpDown])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpeedForwardBack] = (float)((Math.Abs(e.frame.Hands[iTrackHand].PalmVelocity.z) - (mrCurrentRange.fMin[(int)Motions.mSpeedForwardBack])) / (mrCurrentRange.fMax[(int)Motions.mSpeedForwardBack] - mrCurrentRange.fMin[(int)Motions.mSpeedForwardBack])).Clamp(0, 1);
*/
                // Speed (my calcs!)!
                msCurrentState.fValue[(int)Motions.mSpeed] = (float)(((fSpeed) - (mrCurrentRange.fMin[(int)Motions.mSpeed])) / (mrCurrentRange.fMax[(int)Motions.mSpeed] - mrCurrentRange.fMin[(int)Motions.mSpeed])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpeedLeftRight] = (float)((Math.Abs(vSpeed.x) - (mrCurrentRange.fMin[(int)Motions.mSpeedLeftRight])) / (mrCurrentRange.fMax[(int)Motions.mSpeedLeftRight] - mrCurrentRange.fMin[(int)Motions.mSpeedLeftRight])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpeedUpDown] = (float)((Math.Abs(vSpeed.y) - (mrCurrentRange.fMin[(int)Motions.mSpeedUpDown])) / (mrCurrentRange.fMax[(int)Motions.mSpeedUpDown] - mrCurrentRange.fMin[(int)Motions.mSpeedUpDown])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mSpeedForwardBack] = (float)((Math.Abs(vSpeed.z) - (mrCurrentRange.fMin[(int)Motions.mSpeedForwardBack])) / (mrCurrentRange.fMax[(int)Motions.mSpeedForwardBack] - mrCurrentRange.fMin[(int)Motions.mSpeedForwardBack])).Clamp(0, 1);

                // Velocity (i.e. with direction)
                msCurrentState.fValue[(int)Motions.mVelLeftRight] = (float)((vVel.x - (mrCurrentRange.fMin[(int)Motions.mVelLeftRight])) / (mrCurrentRange.fMax[(int)Motions.mVelLeftRight] - mrCurrentRange.fMin[(int)Motions.mVelLeftRight])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mVelUpDown] = (float)((vVel.y - (mrCurrentRange.fMin[(int)Motions.mVelUpDown])) / (mrCurrentRange.fMax[(int)Motions.mVelUpDown] - mrCurrentRange.fMin[(int)Motions.mVelUpDown])).Clamp(0, 1);
                msCurrentState.fValue[(int)Motions.mVelForwardBack] = (float)((vVel.z - (mrCurrentRange.fMin[(int)Motions.mVelForwardBack])) / (mrCurrentRange.fMax[(int)Motions.mVelForwardBack] - mrCurrentRange.fMin[(int)Motions.mVelForwardBack])).Clamp(0, 1);

                // Let's be even handed about this!
                msCurrentState.bIsLeft = !(e.frame.Hands[iTrackHand].IsRight);

                // Now that the motion state has been updated, raise the event
                raiseMotionEvent( new MotionEventArgs(msCurrentState));
            }
            catch (Exception f)
                { Console.Out.Write(f.StackTrace); }


        }



        override public void Disconnect()
        {
            if (lmc != null)
            {
                try
                {
                    lmc.Dispose();
                    lmc = null;
                }
                catch (Exception)
                { }
            }
        }

    }

}
