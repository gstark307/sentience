![http://lh6.ggpht.com/_cGREIsCvj4M/S6zMBC7BevI/AAAAAAAAAjU/YZNB1Uu1Zzo/Minoru.jpg](http://lh6.ggpht.com/_cGREIsCvj4M/S6zMBC7BevI/AAAAAAAAAjU/YZNB1Uu1Zzo/Minoru.jpg)

# Introduction #

The Minoru is the first commercially available stereo webcam.  It's primarily intended for entertainment - broadcasting stereo anaglyphs as a novel alternative to the usual webcam based video conferencing or blogging.  However, it also makes a good inexpensive range sensor for robotics use.

# Disassembly #

The Minoru comes in a casing which is attractive, but not very easy to mount onto a flat surface.  The outer casing can be removed as follows:

  1. Remove the camera stand using a hacksaw just beneath the main body of the camera.
  1. The silver band around the centre of the camera can then be removed in two sections.
  1. Using a knife or precision screwdriver carefully separate the two halves of the casing.  This can be quite tricky.
  1. Remove the white plastic lens covers and any surrounding foam.
  1. You may wish to remove the rubber backing, or keep it in place to insulate the electronics from the surface upon which you're mounting the camera.
  1. Finally you should have extracted the camera board, which looks like the picture above.

# Software #

The Minoru is UVC compilant, and therefore very easy to use on a GNU/Linux operating system.  Plug in the camera, then open a command shell and type:

```
ls /dev/video*
```

This should display two extra video devices.  If you are using a kernel with a version earlier than 2.6.30, obtain the latest UVC driver from [here](http://linuxtv.org/hg/~pinchartl/uvcvideo) then install it (possibly you might need to reboot for the new driver to take effect).

You can test out the webcam using a program called **v4l2stereo**, which is part of a project called [libv4l2cam](http://code.google.com/p/libv4l2cam/) which is related to the Sentience project, and is also under the General Public License.  A [deb package](http://code.google.com/p/libv4l2cam/) is available for easy installation on Debian based distros.  Versions 1.043 and above use the OpenCV version 2 packages which are part of Ubuntu 10.04 (or later).  You will need to install [libcvm57](http://code.google.com/p/libv4l2cam/downloads/detail?name=libcvm57.deb) (or later) before installing the v4l2stereo package.

_v4l2stereo_ can be used in various ways, but to check that the Minoru is working you can use the following command.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --features
```

This assumes that the left camera is video device number 1 and the right camera is video device number 0, and should display two images showing the edge features which are the basis for stereo matching, like this:

![http://lh4.ggpht.com/_cGREIsCvj4M/S6zMA5dcFBI/AAAAAAAAAjQ/_yodyK0qInw/edge_features.jpg](http://lh4.ggpht.com/_cGREIsCvj4M/S6zMA5dcFBI/AAAAAAAAAjQ/_yodyK0qInw/edge_features.jpg)

So, once you have established that the cameras are working the first thing to do is calibrate them using the _--calibrate_ option.  This uses the OpenCV stereo camera calibration routines in order to obtain the optical parameters.  First, print out a [calibration pattern](http://code.google.com/p/libv4l2cam/downloads/detail?name=chess9x6_25mm.pdf), which consists of a checkerboard pattern, and mount it on a rigid backing such as cardboard or wood.  Then type:

```
v4l2stereo --dev0 /dev/video1 --dev1 /dev/video0 --calibrate "6 9 24"
```

The first number of the calibrate option is the number of squares across, the second is the number of squares down, and the third is the size of each square in millimetres.  The order of the dimensions should correspond to how the calibration pattern is presented to the cameras.  The video below shows the procedure.

<a href='http://www.youtube.com/watch?feature=player_embedded&v=o9aUhDe_vPQ' target='_blank'><img src='http://img.youtube.com/vi/o9aUhDe_vPQ/0.jpg' width='425' height=344 /></a>

Optionally the number of calibration images which are gathered can be set.  By default this is 20, but higher numbers should give a more accurate result.

```
v4l2stereo --dev0 /dev/video1 --dev1 /dev/video0 --calibrate "6 9 24" --calibrationimages 60
```

Once camera calibration is complete the parameters are automatically saved to a file called _calibration.txt_.  Normally when running _v4l2stereo_ the program will search for this file in the current directory, but optionally you can also specify it as follows:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --calibrationfile calibration.txt
```

To test the image rectification:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0
```

If the rectification is good you should notice that when the left and right images are placed side by side the rows of both images correspond.  If there is any vertical displacement it is possible to manually alter this, either by editing the _vshift_ parameter within _calibration.txt_ or by using the _--offsety_ option.

You can then test stereo correspondence like this:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --disparitymap
```

This currently uses the [ELAS](http://www.rainsoft.de/software/libelas.html) algorithm for dense stereo.  [Histogram equalization](http://en.wikipedia.org/wiki/Histogram_equalization) is also useful to help improve stereo correspondence results.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --disparitymap --equal
```

<a href='http://www.youtube.com/watch?feature=player_embedded&v=blynlw1YUKs' target='_blank'><img src='http://img.youtube.com/vi/blynlw1YUKs/0.jpg' width='425' height=344 /></a>

<a href='http://www.youtube.com/watch?feature=player_embedded&v=Mx_JqLWVwgM' target='_blank'><img src='http://img.youtube.com/vi/Mx_JqLWVwgM/0.jpg' width='425' height=344 /></a>

A threshold can also be applied, so that only nearby objects are visible.  This can be useful for detecting people at close range.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --disparitymap --equal --disparitythreshold 15
```

Using the disparity threshold it's also possible to substitute a background image.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --background mybackground.jpg --equal --disparitythreshold 7
```

The above uses a global disparity threshold, but if your stereo camera is always fixed in place (for example, a fixed security camera) then it's also possible to create a _background model_, which in effect is like having an individual disparity threshold for each pixel.  If you're trying to detect people, make sure that there is nobody in view when making the model.  To create a background model file:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --learnbackground model.dat --equal
```

Then to subsequently use the model:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --disparitymap --equal --backgroundmodel model.dat --disparitythreshold 3
```

This means that only pixels having a disparity of three or greater relative to the background will be shown.  An example situation is shown in the video below.  Here a background model was created for a desk surface, and objects subsequently placed on or above it then show up quite clearly.

<a href='http://www.youtube.com/watch?feature=player_embedded&v=9UmHVALu-E0' target='_blank'><img src='http://img.youtube.com/vi/9UmHVALu-E0/0.jpg' width='425' height=344 /></a>

Another possible example situation in which this might be used is a fixed downward looking stereo camera on a mobile robot, which is then able to detect obstacles (collision detection/[hazcam](http://en.wikipedia.org/wiki/Hazcam)) or objects on the floor.  In an indoor scenario where the floor is always flat this should work quite well.

Similar to the global disparity threshold situation you can also use a background image if you wish.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --background mybackground.jpg --equal --backgroundmodel model.dat --disparitythreshold 3
```

An overhead view of the projected disparity map can also be shown.  This type of projection would be useful for updating occupancy grids.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --overhead
```

<a href='http://www.youtube.com/watch?feature=player_embedded&v=kQPIm2Kw5kc' target='_blank'><img src='http://img.youtube.com/vi/kQPIm2Kw5kc/0.jpg' width='425' height=344 /></a>

The point cloud can also be observed from alternative "imaginary" viewpoints using the _virtual camera_ mode.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --vcamera
```

<a href='http://www.youtube.com/watch?feature=player_embedded&v=gHv2w9w4zZk' target='_blank'><img src='http://img.youtube.com/vi/gHv2w9w4zZk/0.jpg' width='425' height=344 /></a>

To move the virtual camera use the following keys:

```
A - up
Z - down
X - forward
S - Backward
< - left
>  - right
1/2 - Tilt down/up
3/4 - Pan left/right
5/6 - Roll anti-clockwise/clockwise
```

One problem which becomes conspicuous with the overhead and virtual camera views is that in low lighting when exposure time is relatively high depth errors due to lack of camera synchronization can become a problem.  Under these conditions the image may appear to oscillate in depth.

If the camera is looking down at a flat surface, such as a floor or desk, then it's possible to detect obstacles or objects which have significant vertical structure above the ground plane.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --obstacles
```

You can also specify more precisely the downward tilt angle of the camera as follows:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --obstacles --poserotation "30 0 0" --obstaclethreshold 3000 --obstaclecellsize 20
```

Where the first parameter of _poserotation_ specifies the tilt angle in degrees, with positive being downwards.  A threshold value can be used to indicate how much vertical structure is needed in order to qualify as an object above the ground plane (points per grid cell), and the cell size of the 2D ground plane grid used to accumulate points may be given in millimetres.  Unlike with the _background model_ method, this much more reliably removes the background.

An example of an object on a desk surface is shown in the following video.  As with the _virtual camera_ view the keys can be used to observe from different angles.

<a href='http://www.youtube.com/watch?feature=player_embedded&v=GV9ex-xepDc' target='_blank'><img src='http://img.youtube.com/vi/GV9ex-xepDc/0.jpg' width='425' height=344 /></a>

Multiple objects can also be detected with this method.  Here we instead use the _objects_ option, and give a minimum and maximum ground plane area in square millimetres.  Objects are colour coded to indicate that they are discreet entities.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --objects --poserotation "30 0 0" --obstaclethreshold 3000 --obstaclecellsize 20 --minarea 50 --maxarea 500
```

<a href='http://www.youtube.com/watch?feature=player_embedded&v=InDr6Qr5O-I' target='_blank'><img src='http://img.youtube.com/vi/InDr6Qr5O-I/0.jpg' width='425' height=344 /></a>

Mesh models can be saved either in [STL](http://en.wikipedia.org/wiki/STL_%28file_format%29) or [X3D](http://en.wikipedia.org/wiki/X3D) format.  The STL format lacks any colour information, but may be useful for [rapid prototyping](http://en.wikipedia.org/wiki/RepRap_Project) applications.  [X3D](http://en.wikipedia.org/wiki/X3D) produces a colour and theoretically web compatible 3D mesh

To save the entire scene in X3D format:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --vcamera --maxrange 2000 --savex3d myscene.x3d
```

To detect an object with the camera tilted downwards by 30 degrees, then save it in X3D format:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --objects --poserotation "30 0 0" --obstaclethreshold 3000 --obstaclecellsize 20 --minarea 50 --maxarea 500 --savex3d mymodel.x3d
```

<a href='http://www.youtube.com/watch?feature=player_embedded&v=pSVeubPH0xY' target='_blank'><img src='http://img.youtube.com/vi/pSVeubPH0xY/0.jpg' width='425' height=344 /></a>

Or to save the object as STL:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal --objects --poserotation "30 0 0" --obstaclethreshold 3000 --obstaclecellsize 20 --minarea 50 --maxarea 500 --savestl mymodel.stl
```

The effective stereo range of the Minoru, using 320x240 images, is between 35cm and 2 metres.  The range is limited mainly by the baseline distance between the cameras and the image resolution.  For close up tasks such as manipulation or inspection of objects this might be quite a useful off-the-shelf sensor.

A greater effective range of approximately 3-4 metres can be obtained by using a higher resolution.  However, this requires a slower frame rate, and differences in frame capture times due to unsynchronized capture become a far greater issue.  Also you will need to recalibrate using the higher resolution.  Note that camera calibration at 640x480 resolution can be somewhat haphazard, and may take several attempts before a good rectification result is obtained.

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 -w 640 -h 480 --fps 15 --disparitymap --equal
```

# Point Clouds #

It's also possible to export the range data as a point cloud file.  For example:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --equal -vcamera --poserotation "0 0 0" --pointcloud mycloud.dat
```

This runs the cameras, creates a point cloud with the given rotational orientation, then exits.  The _poserotation_ parameters correspond to tilt, pan and roll angles in degrees, where positive tilt is downwards, positive pan is clockwise as viewed from above and positive roll is clockwise as viewed from behind.

If the stereo camera is mounted on a pan and tilt mechanism, or if the robot observes from different orientations the _poserotation_ and _posetranslation_ options can be used.  The _pcloud_ utility may then be run to visualise and save a composite point cloud model constructed from multiple observations.

```
pcloud -f "mycloud0.dat,mycloud1.dat"
```

The file names should be separated by commas.  You can also save to X3D format.

```
pcloud -f "mycloud0.dat,mycloud1.dat" --savex3d test.x3d
```

An example composite point cloud X3D file, visualised using [MeshLab](http://en.wikipedia.org/wiki/MeshLab), is shown below.

<a href='http://www.youtube.com/watch?feature=player_embedded&v=GnIVTpvdYRk' target='_blank'><img src='http://img.youtube.com/vi/GnIVTpvdYRk/0.jpg' width='425' height=344 /></a>

# Network streaming and headless operation #

It's possible to stream video over a network using [gstreamer](http://gstreamer.freedesktop.org/).  This can be useful for remotely operated vehicles or surveillance systems.

On the server:

```
v4l2stereo -0 /dev/video1 -1 /dev/video0 --disparitymap --equal --stream --headless
```

then on the client:

```
gst-launch tcpclientsrc host=[ip] port=5000 ! multipartdemux ! jpegdec ! autovideosink
```

To do a local test you can use _localhost_ as the IP address.

# Development #

**Required OpenCV packages**

If you wish to do development on the v4l2stereo code you will need to ensure that opencv packages are installed.  On Ubuntu 10.04 or later this is easily achieved as follows:

```
    sudo apt-get install libcv2.1 libhighgui2.1 libcvaux2.1 libcv-dev libcvaux-dev libhighgui-dev libgstreamer-plugins-base0.10-dev libgst-dev
```

**Configuring for use with Eclipse**

To use with the Eclipse C++ IDE:

  * Create a new C++ project

  * Import the source from the _Import/File System option_

  * Within the project properties select _C++ Build/Settings_

  * Under _GCC C++ Compiler/Directories/Include paths (-l)_ enter

```
/usr/include/opencv
/usr/include/gstreamer-0.10
```

  * Under _GCC C++ Compiler/Miscellaneous_ enter

```
-c -fopenmp -fmessage-length=0 -lcam -lcv -lcxcore -lcvaux -lhighgui `pkg-config --cflags --libs gstreamer-0.10` -L/usr/lib -lcv -lcxcore -lcvaux -lhighgui `pkg-config --cflags --libs glib-2.0` `pkg-config --cflags --libs gstreamer-plugins-base-0.10` -lgstapp-0.10
```

  * Under _GCC C++ Linker/Libraries/Libraries (-l)_ enter

```
cv
cxcore
gstapp-0.10
highgui
```

  * Under _GCC C++ Linker/Libraries/Library search path (-L)_ enter

```
/usr/lib
/usr/lib/gstreamer-0.10
```

  * Under _GCC C++ Linker/Miscellaneous/Linker flags_ enter

```
/usr/lib/libcxcore.so /usr/lib/libcvaux.so /usr/lib/libcv.so /usr/lib/libhighgui.so
```

After applying those settings you should now be able to compile the project within Eclipse.

# Integration with ROS #

The [source package](http://code.google.com/p/libv4l2cam/) also contains an example of integrating v4l2stereo with [ROS](http://www.ros.org) - the robot operating system produced by [Willow Garage](http://www.willowgarage.com/).  This would make it possible to replace more expensive stereo cameras with something like a Minoru.  The only real advantage that the more expensive stereo cameras have is synchronized frame capture, which is advantageous when the robot is in motion.

**Making the package**

Add the directory where the ros stereocam package is located to _/opt/ros/cturtle/setup.sh_

Reload

```
. ~/.bashrc
```

Then make the package as follows:

```
rosmake --rosdep-install stereocam
```

Edit the _stereocam.launch_ file as appropriate.  If you are only using a single stereo camera then add the desired v4l2stereo command for _glimpsecommand1_, and leave the other glimpse commands as empty strings.  You may also wish to use a RAM disk for _outputdirectory_, in order to minimise hard disk access.

**Running the ROS publisher (broadcasting service)**

Start broadcasting:

```
roslaunch stereocam.launch
```

Then to test that the broadcast can be received:

```
roslaunch stereocam_subscribe.launch
```

_stereocam\_subscribe.launch_ is only intended to be illustrative of how to connect to the stereo camera using ROS and receive point cloud data.  You can use this as a guideline to how to integrate with your own system.

# Other things to try #

One limitation of the Minoru is that the field of view is quite narrow - only about 40 degrees - giving it a tunnel vision which is fairly standard for webcams.  For robotics use a wider field of view would be preferable, and it may be possible to replace the lenses (M12 fittings which screw in).  _v4l2stereo_ includes options which allow you to rectify the images if the camera calibration parameters are known.

After searching around and trying a few different lens types I found that the following specification works well.

```
Angle of view: 180 degrees
Focal length: 1.8mm
Back focal length: 5.44mm
Format: 1/3", 1/4"
Aperture: F2.0
```

The most important figure here is the _back focal length_, which is the distance between the back of the lens and the CCD or CMOS sensor.  If this is longer than about 7mm then the lens will usually not fit into the short lens mountings on the Minoru, which are only about 10mm in height above the circuit board.  In this case the field of view is actually less than the full 180 degrees, because the sensor size is smaller than the quoted format sizes.

With wide angle lenses fitted, the Minoru looks like this:

![http://groups.google.com/group/sentience/web/minoru_wide_angle2.jpg](http://groups.google.com/group/sentience/web/minoru_wide_angle2.jpg)