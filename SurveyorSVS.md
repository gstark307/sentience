![http://photos-a.ak.facebook.com/photos-ak-sf2p/v337/62/98/502968981/n502968981_1201968_5573.jpg](http://photos-a.ak.facebook.com/photos-ak-sf2p/v337/62/98/502968981/n502968981_1201968_5573.jpg)

# Introduction #

This is a stereo vision system for mobile robots sold by [Surveyor Corporation](http://www.surveyor.com/stereo/stereo_info.html).  Code and algorithms from the main [Sentience project](http://code.google.com/p/sentience) have been used to develop some software for use with this device, giving the ability to range objects within view.

The software is written in C# and is intended to be compiled either using Visual Studio 2005 on a Microsoft Windows system, or using [MonoDevelop](http://www.monodevelop.com) on GNU/Linux systems.  It has been tested on Windows Vista and Ubuntu Hardy, and is licenced under GPL version 3.

# Setup #

General SVS setup information can be [found here](http://www.surveyor.com/stereo/SVS_setup.html).  Also a step by step guide to uploading new firmware versions [is here](SurveyorSVSFirmware.md).

# Configuration #

Main solution files are:

  * surveyorstereo.mds (GNU/Linux version)
  * surveyorstereo.sln (Windows version)

First you need to ensure that the IP address and port numbers for the stereo camera are configured correctly.  These are contained within [MainWindow.cs](http://code.google.com/p/sentience/source/browse/trunk/applications/surveyor/surveyorstereo/MainWindow.cs) (GNU/Linux version which uses a Gtk GUI) or [frmStereo.cs](http://code.google.com/p/sentience/source/browse/trunk/applications/surveyor/surveyorstereo/frmStereo.cs) (Windows version using Windows.Forms).  You should then be able to compile and run the software and receive images from both cameras.

On a new installation of Ubuntu you may need to install gmcs (the .NET 2.0 runtime) in order to compile the software, like this:

```
sudo apt-get install mono-gmcs
```

# Calibration #

A video of the calibration procedure [is shown here](http://www.youtube.com/watch?v=Y9Ogsul8_TY).

Both cameras need to be calibrated before the device can be used for stereo ranging.  Calibration removes the lens distortion effects, ensuring that straight lines in the world appear as straight lines in the images so that the [epipolar constraint](http://en.wikipedia.org/wiki/Epipolar_geometry) applies, and also corrects for any small misalignments from a perfectly parallel geometry.  This is an easy procedure which only takes a few minutes, and only typically needs to be performed once unless the positions of the cameras have changed.

It's a good idea to ensure that both cameras are rigidly mounted in parallel so that they cannot move relative to each other, even despite the knocks and bumps which a mobile robot is bound to encounter.  One way to do this is to bolt a piece of wood or metal to the top mounting holes, as in the above picture.

Run the software, then either click on the "calibrate left camera" checkbox or select it from the menu bar (depending upon whether the Windows or GNU/Linux version is being used).  You should see a pattern of dots appear next to the left camera image, like this:

![http://www.surveyor.com/images/calibration_test.jpg](http://www.surveyor.com/images/calibration_test.jpg)

Point the left camera at the dot pattern on the screen (it is assumed that the screen being used is flat, not curved like an old CRT monitor) so that the pattern fills the field of view of the camera.  You may need to experiment with the distance between the screen and the camera, but once the system acquires a suitable image it will be displayed in the left image area.  After a few updates you will notice that the dot pattern appears to go from being somewhat warped around the periphery to being straight, with regular spacing between dots.  There will also be a beep sound, indicating that the RMS curve fitting error has fallen below an acceptable threshold.  Once this happens you can click on "calibrate right camera" or select it from the menu and perform a similar procedure for the right camera.

![http://lh5.ggpht.com/fuzzgun/SLmBwBTDJLI/AAAAAAAAAFg/Evl22kdbjZw/SVS_calibration3.jpg](http://lh5.ggpht.com/fuzzgun/SLmBwBTDJLI/AAAAAAAAAFg/Evl22kdbjZw/SVS_calibration3.jpg)

You should now be able to see a pair of images where straight lines appear straight.  To complete the calibration process you now need to point the stereo camera at something a significant distance away - preferably more than five metres distant.  The idea here is that rays of light coming from these distant objects can be considered for practical purposes to be effectively parallel.  A good way to do this is to point the stereo camera out of a window at a distant house or trees.  Click on "calibrate alignment" or select it from the menu and the calibration process will now be complete.

You may wish to save the calibration XML file to some suitable location for later use, which can be done either from the file menu or by clicking on a button.  In the GNU/Linux (Gtk) version the calibration file is saved onto the desktop.

By default you will see the simple (sparse) stereo algorithm running, with green dots appearing in the left image indicating the amount of stereo disparity for detected features.  Bigger dots are closer to the camera.  The relationship between disparity and distance is given [in this document](http://code.google.com/p/sentience/source/browse/trunk/docs/stereo_vision_geometry.odg).  If the results look unsatisfactory for any reason try going through the calibration procedure again.  It cannot be overstated that good camera calibration is essential for any reasonable results on stereo correspondence.  Poorly focussed cameras can also cause problems, so ensure that the images look reasonably sharp and if not then manually adjust the focus as needed.

# Calibration using a physical pattern #

It is also possible to save the calibration pattern as a bitmap.  In the GNU/Linux version this is saved to the desktop by clicking a button, and in other versions a dialog from the file menu allows you to save the image to any location.  The image may then be printed out and glued to a cardboard or other rigid backing.  This provides an alternative to calibrating directly from the screen, should that be more convenient.  The audible beep produced as each camera is calibrated means that you don't necessarily need to be looking at a monitor or laptop in order to know that the process has completed.

The calibration image can also be [downloaded here](http://lh3.ggpht.com/fuzzgun/SMWqwVVowLI/AAAAAAAAAGI/UCxY7K4NScw/s720/calibration_pattern.jpg).

http://lh4.ggpht.com/fuzzgun/SMquSVlcgUI/AAAAAAAAAGo/0uqQi3Jb-bw/s512/calibration_pattern_card.JPG

# Stereo vision server #

[Server source code](http://code.google.com/p/sentience/source/browse/#svn/trunk/applications/surveyor/stereoserver)

![http://lh4.ggpht.com/fuzzgun/SMWnVVwxO-I/AAAAAAAAAGA/gPS6EYcwsro/stereo_server.jpg](http://lh4.ggpht.com/fuzzgun/SMWnVVwxO-I/AAAAAAAAAGA/gPS6EYcwsro/stereo_server.jpg)

A graphical user interface is obviously necessary for calibration and testing of the stereo correspondence algorithms.  However, once calibration has been completed it is possible to run the system "headless" as a command line program.  This program computes stereo disparities and broadcasts this data to other connected applications using the TCP protocol.  The syntax is as follows:

```
stereoserver -server <IP address of the stereo camera>
             -leftport <port number of the left camera, usually 10001>
             -rightport <port number of the right camera, usually 10002>
             -broadcastport <port number on which to broadcast stereo feature data>
             -algorithm <"simple" or "dense">
             -calibration <calibration filename>
             -width <width of the image coming from the stereo camera in pixels>
             -height <height of the image coming from the stereo camera in pixels>
             -record <save raw and rectified images to the given path>
             -fps <ideal number of frames per second>
```

What is the format of the stereo feature data being broadcast?  For the simple edge/corner based stereo algorithm the data format is 12 bytes per feature.

```
public struct BroadcastStereoFeature
{
    public float x;         // 4 bytes
    public float y;         // 4 bytes
    public float disparity; // 4 bytes
}
```

For the dense algorithm three bytes indicating the colour of the feature is also transmitted, with 16 bytes per feature.

```
public struct BroadcastStereoFeatureColour
{
    public float x;         // 4 bytes
    public float y;         // 4 bytes
    public float disparity; // 4 bytes
    public byte r, g, b;    // 3 bytes
    public byte pack;       // 1 byte
}
```

The first byte in the transmission contains either a zero value if the subsequent data is in the first format or a one if the subsequent data is in the second format with additional colour information.

To keep bandwidth usage reasonable a maximum of 200 stereo features are broadcast at any point in time.  If more than 200 features were detected a random sample is returned.  This also allows computational economies to be made in the dense stereo algorithm, which only needs to sample a few randomly selected image rows in order to acquire enough data.  For those familiar with [LIDAR](http://en.wikipedia.org/wiki/LIDAR) systems you can think of this as being like a _crazy laser scanner_, where the tilt angle used for each scan line is picked randomly.

An example command line client program which connects to the stereo vision server and receives features [can be found here](http://code.google.com/p/sentience/source/browse/#svn/trunk/applications/surveyor/stereoclient).  The syntax for its use is as follows.

```
stereoclient -server <IP address of the computer on which the stereo vision server is running>
             -port <broadcast port number on which the stereo vision server is communicating>
```

An easy way to write a program which does something with the stereo disparity data would be to create a new class which inherits from [StereoVisionClient.cs](http://code.google.com/p/sentience/source/browse/trunk/applications/surveyor/stereoclient/StereoVisionClient.cs) and overrides the FeaturesArrived method.


# Notes #

The dense stereo algorithm is loosely based upon a paper by Jenny Read and Bruce Cumming, [Sensors for impossible stimuli may solve the stereo correspondence problem](http://www.staff.ncl.ac.uk/j.c.a.read/publications/ReadCumming07.pdf).

Results of stereo correspondence are always noisy in practice, and occasional false matches are an occupational hazard.  However, provided that the signal outweighs the noise good results can be achieved, especially when using probabilistic methods such as occupancy grids.

# Ideas for future work #

It is possible that stereo correspondence could be combined with fast monocular tracking similar to that used by [Andrew Davison](http://www.doc.ic.ac.uk/~ajd/).  Features could be initialized in a very computationally economical manner, perhaps randomly selecting a single image row upon which to find new features, then tracked monocularly at high speed.

# Release 0.3.1 #

This version contains code which has been tested on Windows Vista, both within Visual Studio 2008 and the Windows version of MonoDevelop.

# Release 0.3 #

In the interests of following the mantra _release early and often_ this is a somewhat experimental and possibly unstable release.  There have been significant improvements in the threading used to grab images from both cameras, which make the GUI a lot less flaky.  Buttons have also been added to allow manual teleoperation of the SRV robot, although the screen resolution buttons don't work.  There's also a logging feature which allows images and teleoperation commands to be stored to disk for later analysis or simulation.

For the first time binaries have also been released to allow easy installation.  There are deb and rpm packages for installation on various Linux distros.  The deb package should install necessary dependencies, but if you're installing the rpm you'll need to ensure that the mono-core and gtk-sharp packages are installed.

In this version I expect that there will be broken functionality on Windows based operating systems, because I've payed little attention to the Windows version.  This will be improved in the next release.

Report all bugs using the Issues tab above.

# Release 0.2 #

[Source code](http://sentience.googlecode.com/files/surveyor_svs_v0_2_0.zip)

Stereo correspondence and calibration algorithms are mostly unchanged.  This version mainly contains usability improvements, which include:

  * Slightly altered class heirachy.

  * A command line stereo vision server as an alternative to the GUI based system.  This may be useful on robots running GNU/Linux which are not using X.  The server program only acquires images from the SVS and computes stereo matches if one or more client applications are connected.

  * An example client command line program which connects to the server and receives stereo feature data.

  * An audible beep when calibration of an individual camera is complete.

  * Ability to save the calibration pattern so that it may be printed out.

# Release 0.1 #

[Source code](http://sentience.googlecode.com/files/surveyor_svs_v0_1_0.zip)

The initial release contains algorithms for sparse edge/corner based stereo correspondence and a dense disparity map.  The dense algorithm has not been optimized, so there is presumably scope for improvement in its performance.

Known issues:

  * When selecting to calibrate left or right cameras on the Linux version sometimes you need to click more than once on the check box.

  * Calibration does not include relative rotation (roll angle) of one camera to another.  It is assumed that the relative rotation is zero degrees.

  * There is no way of transmitting the stereo feature data to other applications.  Broadcasting the data via TCP or UDP is anticipated for a later version.