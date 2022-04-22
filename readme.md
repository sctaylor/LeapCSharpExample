# LeapCSharpExample

This repo is an illustration of how I integrated the LeapMotion using C# on Windows using UltraLeap's 
v4 / v5 64bit drivers.  It is the underpinning for the basic motion aspects of my personal project
**MidiPaw** (https://www.midipaw.com) which is currently not open sourced.

A number of folks have inquired re how I  got started integrating with Leap, and perhaps with additional
creative endeavours the LeapMotion universe of fun things to do will grow.

Everything needed is free
* VisualStudio Community Edition
* Leap Motion SDK (to get drivers and LeapC.dll)

The interface code in the LeapMotion folder is provided by Ultraleap under Apache License 2.0
(http://www.apache.org/licenses/LICENSE-2.0).  The other source code supplied by me is offered
without restriction or requirement.  You can use a copy of it or modify it for whatever purpose you wish, and it
is provided as-is without warranty of any kind.

## Overview of Folders/Classes/Files

* Controllers
  * This folder contains two classes to demonstrate Leap integration:
    * The base class **Controller** which is a generalized
    controller class that fires events based on changes in underlying
    state of the controller (i.e. movement happening, or in the case of other
    kinds of controllers, buttons pressing or pressure changing, etc)
    * The class **ConrollerLeap** which is an extension of the Controller class
    to provide the Controller services on a Leap Motion
  
* Helpers
  * These are a couple of simple classes to help the controller classes
    handle the motion-driven data being provided by the Leap Motion.

* LeapMotion
  * This collection of classes is derived from the C# Unity implementation
  that was originally provided with the Unity Modules 4.8 by Ultraleap.
  They have kindly confirmed that these are released under an open
  agreement and it is fine to use them in this way and share them publicly.

* LeapC.dll
  * This project includes the 64-bit LeapMotion v5.2 LeapC.dll.  It is set to copy to the
  output directory (right click, properties to see this).  If you are running the
  v4 Leap drivers, replace this with the 64-bit v4 LeapC.dll that you get from the
  Leap Motion SDK.
    * Alternatively, if you have the LeapC.dll that you wish to use sitting in a
    directory that Windows will use when searching for DLLs, you can omit the
    explicit LeapC.dll from this project and just let Windows find it at runtime
    fro m your registry or path.

* MainWindow.xaml.cs
  * This is the source behind the single window, showing the
  usage of Leap information for control (in this case just
  updating a text value)

### ControllerLeap Usage

As shown in the very basic example of *MainWindow.xaml.cs*, the *ControllerLeap* class operates by just
instantiating an instance (which automatically tries to connect) and then subscribe to
the *MotionEvent* stream (and unsubscribe as the window is destroyed)


        public MainWindow()
        {
            InitializeComponent();

            clMyLeap = new ControllerLeap();

            clMyLeap.MotionEvent += HandleMotionEvent;
        }

        ~MainWindow()
        {
            if (clMyLeap != null)
                clMyLeap.MotionEvent -= HandleMotionEvent;
        }

From there, **ControllerLeap** will fire motion events to the handler that you've provided.  It has a signature like this:

        void HandleMotionEvent(object sender, Controller.MotionEventArgs e)

Inside the handler, you receive a *MotionEventArgs* which contains only a *MotionState* member.
The *MotionState* contains:
* An array of floats *fValue[]*, one per type of defined *Motion*
* A boolean *bLeft* indicating if it is the left hand (this code currently tracks only one hand at a time)
* A float *fFrameRate* giving the current number of event calls per second

The indicies for the *Motion* values that can appear in a *MotionState* are (from *Controller.cs*):

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

Not all of these *Motions* are supported/implemented by *ControllerLeap*.  The implemented list
can be seen in *ControllerLeap.cs*:

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


## Good Luck!

I hope this helps gets a fun project or two started.  Unfortunately I can't provide any 
direct support but hopefully you'll find it easy enough to get going!

  - Steve