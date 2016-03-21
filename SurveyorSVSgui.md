![http://groups.google.com/group/sentience/web/surveyorstereo2.jpg](http://groups.google.com/group/sentience/web/surveyorstereo2.jpg)

# Installation #

To install the GUI:

```
    sudo dpkg -i surveyorstereo-x.x.deb
```

or alternatively there is a rpm package and also a zipped archive of files which can be run on Windows systems.  If installing from rpm you will need to ensure that the Mono C# core and gtk-sharp packages are installed.

By default the IP address for the SVS is 169.254.0.10 and the ports for the cameras are 10001 and 10002.  At the time of writing these are the factory defaults.  If the SVS is configured in some other way you'll need to edit the startup script.

```
    sudo gedit /usr/bin/surveyorstereo.sh
```

It should look something like this:

```
    mkdir ~/svs
    cd ~/svs
    mono /usr/bin/sentience/surveyorstereo/surveyorstereo.exe -i 169.254.0.10 -leftport 10001 -rightport 10002
```

Here you can edit the IP address and port numbers, the re-save the script.  Note that the script sends you to a subdirectory called _svs_ within your home directory.  This is useful because it means that if images are logged they're always placed in a consistent location.

The GUI can then be run simply by typing:

```
    surveyorstereo
```

or on Windows systems run _surveyorstereo.exe_.

# Source code #

If you're compiling the GUI from source on Linux systems, or if you're running MonoDevelop on Windows, use the _mds_ solution files, which are in MonoDevelop 1.0 format.  Using the older format helps to keep the _Gtk_ and _Windows.Forms_ versions GUI versions separate.  Possibly in future support for _Windows.Forms_ may be dropped.

# Camera Calibration #

A video of the calibration procedure [is shown here](http://www.youtube.com/watch?v=Y9Ogsul8_TY).

  1. Click on _calibrate left camera_.  A calibration pattern will then appear shown within the right image.  Pont the left camera at the pattern, with the red dot towards the centre of the image.  The screen should be more or less perpendicular to the axis of the cameras view, with the pattern filling the image.
  1. After a while you will see that the image has been flattened, and ideally the spacing between dots should be constant across the image.  You may need to experiment with moving the camera bacwards or forwards to get the ideal distance.
  1. Uncheck _calibrate left camera_, then click on _calibrate right camera_.  Perform the same operation for the right camera, then uncheck _calibrate right camera_.
  1. Point the stereo camera at something which is distant - preferably more than five metres away.  Click on _calibrate alignment_.  The stereo camera should now be calibrated.
  1. Within your current directory a calibration xml file will be generated.  Also there will be two files called _svs\_left_ and _svs\_right_.  If you wish to use the embedded stereo vision these files can be uploaded to the blackfin processors, using the _storecalib1_ and _storecalib2_ scripts.
  1. You can manually check the calibration by clicking on the Tweaks button, or selecting it from the Tools menu.  The [tweaks utility](utilitystereotweaks.md) allows you to manually specify offset values, then visualise the effect.  You'll need to have the stereotweaks utility installed for this to work.

It may be necessary to try calibrating a few times before getting good looking images.  Sometimes the system isn't able to build an accurate model of the lens distortion, due to misdetection of the dot centres or errors in dot linking.  Since camera calibration is generally a one-off procedure (unless the positions of the cameras subsequently change) this isn't a major issue.

# Driving #

If the SVS is connected to a robot base the buttons can be used to drive the robot as usual.  In the current version the screen resolution buttons have no effect, with the resolution being fixed at 320 pixels across.

# Logging #

Stereo images and driving commands can be logged by clicking on the _logging_ checkbox, or by selecting it from the tools menu.  When you uncheck logging all files will be compressed into a single archive for convenience, and assigned the specified log name.  This feature is useful if you wish to carry out offline experiments using stereo images, such as visual SLAM or feature detection.  The images are saved in jpeg format, which is the same format in which they're transmitted from the SVS.  Note that in order for the compression to work you'll need to ensure that [GNU tar](http://www.gnu.org/software/tar/) is installed, or on Windows systems use [gzip](http://www.gzip.org/) and ensure that it is included within your [PATH environment variable](http://vlaurie.com/computers2/Articles/environment.htm).

You can also replay any prerecorded logs by entering the desired log name then clicking on the replay button.  This will replay the same driving commands which were used.  This is open loop, so be prepared for accumulating dead reckoning errors.