![http://sentience.googlegroups.com/web/catseyes.jpg](http://sentience.googlegroups.com/web/catseyes.jpg)

Sentience is a volumetric perception system for mobile robots, written in C#.  It uses webcam-based stereoscopic vision to generate depth maps, and from these create colour 3D voxel models of the environment for obstacle avoidance, navigation and object recognition purposes.

Videos:

  * [Embedded stereo correspondence algorithm running on the Surveyor SRV-1 robot](http://www.youtube.com/watch?v=oGFhc9SnBFg)
  * [Features of interest which can be stereo matched](http://www.youtube.com/watch?v=ErrOYxyuneo)
  * [Disparity histograms](http://www.youtube.com/watch?v=wBRcj3-jzrw)
  * [Stereo camera calibration procedure](http://www.youtube.com/watch?v=Y9Ogsul8_TY)
  * [Rodney humanoid using the Minoru webcam](http://www.youtube.com/watch?v=gNRdcwrTOM0)
  * [Stereo matches with the Minoru webcam](http://www.youtube.com/watch?v=EUcLAarcj7U)
  * [Dense disparity map with the Minoru webcam](http://www.youtube.com/watch?v=HeTpk5U8L-k)

Robots which use Sentience:

  * [Rodney](http://sluggish.uni.cc/rodney/rodney.htm)
  * [Flint](http://sluggish.uni.cc/flint/index.htm)
  * [Surveyor SRV-1 (with stereo vision system)](http://code.google.com/p/surveyor-srv1-firmware/)
  * GROK2

Stereo cameras:
  * [How to make a stereo camera](HowToMakeAStereoCamera.md)
  * [Using multiple cameras on Linux](UsingMultipleCamerasOnLinux.md)
  * [The Surveyor Stereo Vision System](SurveyorSVS.md)
  * [Minoru 3D webcam](MinoruWebcam.md)

Utilities:
  * [stereosensormodel](utilitystereosensormodel.md) - a program for creating stereo vision [sensor models](StereoUncertainty.md) suitable for use with occupancy grids
  * [stereotweaks](utilitystereotweaks.md) - a GUI which can be used to manually set stereo camera calibration offsets, visually check camera calibration and create anaglyph-like animations.
  * [surveyorstereo](SurveyorSVSgui.md) - a graphical user interface for the [Surveyor SVS](http://www.surveyor.com/stereo/SVS_setup.html).

Development:
  * [What development tools do I need to work with the sentience code?](DevelopmentTools.md)
  * [How to install OpenCV](http://dircweb.king.ac.uk/reason/opencv_cvs.php)
  * [How to compile and run the code](GettingStarted.md)

Fundamentals of stereo vision:
  * [Why stereo vision ?](WhyStereoVision.md)
  * [How do I calculate range from stereo disparity?](StereoRanging.md)
  * [Characterising uncertainties in stereoscopic vision](StereoUncertainty.md)
  * [Stereo camera calibration](CameraCalibration.md)
  * [Sensor models](StereoSensorModel.md)

Other topics:

  * [Project roadmap](ProjectRoadmap.md)
  * [How do I find the pixel density of the image sensor, or the focal length?](SensorPixelDensity.md)
  * [Utilities for webcam based stereo vision](WebcamStereoVisionUtilities.md)
  * [A biologically inspired stereo correspondence algorithm](StereoCorrespondence.md)
  * [Moving through space](MotionModels.md)
  * [Autonomous Navigation](AutonomousNavigation.md)
  * [Zen and the art of robot design](RobotDesigner.md)
  * [Occupancy Grids](OccupancyGrid.md)

Background reading:

  * [Articles on occupancy grids and SLAM algorithms](OccupancyGridReferences.md)
  * [The Development of Camera Calibration Methods and Models](http://sluggish.uni.cc/sentience/CameraCalibrationMethods.pdf) by T.A. Clarke and J.G. Fryer.
  * [Probabilistic Robotics](http://www.probabilistic-robotics.org/), by Sebastian Thrun, Wolfram Burgard and Dieter Fox.
  * [Estimating egomotion from stereo vision using the principle of least commitment](http://groups.google.com/group/sentience/web/egomotion.pdf)