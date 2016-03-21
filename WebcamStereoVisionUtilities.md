# fswebcam #

fswebcam is a good utility which works with Video4Linux version 1 and 2 devices.  I've also used it with the so-called "driverless" USB Video class (UVC) devices.  The main site for this utility [is here](http://www.firestorm.cx/fswebcam/).  There is a problem associated with using fswebcam for stereo vision though, in that it's not intended to capture images from more than a single camera device at a time.  As a workaround for this I modified the 20070108 version of fswebcam to be able to grab images from different devices in quick succession.  The modified version, which includes all the original functionality, [can be found here](http://code.google.com/p/sentience/source/browse/#svn/trunk/applications/fswebcam).

To install the modified version on an Ubuntu system first you will need to [download the GD library](http://www.libgd.org) upon which it is based.  Move to the directory where you extracted the code, then go though the usual installation sequence:

```
./configure
make
sudo make install
```

Download the [modified version of fswebcam](http://code.google.com/p/sentience/source/browse/trunk/applications/fswebcam-20080912.tar.gz) and go through the same install procedure.  You will now be able to use the utility with multiple devices, like this:

```
fswebcam -S 2 -d /dev/video0,/dev/video1 -r 320x240 --no-banner -save test.jpg
```

This will save two images called test0.jpg and test1.jpg.  You can even capture from more than two camera devices if you wish, with the main limit being USB bandwidth.


# Stereo vision / calibration GUI #

[Source code](http://code.google.com/p/sentience/source/browse/#svn/trunk/applications/surveyor/webcamstereo)

![http://lh3.ggpht.com/fuzzgun/SM16onu3ALI/AAAAAAAAAHU/w_4htI7jSZk/webcamstereo.jpg](http://lh3.ggpht.com/fuzzgun/SM16onu3ALI/AAAAAAAAAHU/w_4htI7jSZk/webcamstereo.jpg)

Main project file: webcamstereo.mds

This is a Gtk based graphical user interface written in C# which can be used to test stereo vision algorithms and perform camera calibration, which was a spinoff development from the Surveyor stereo vision system.  You can find more detailed instructions on calibration [here](http://code.google.com/p/sentience/wiki/SurveyorSVS).

The GUI may be cusomized by altering the settings within [MainWindow.cs](http://code.google.com/p/sentience/source/browse/trunk/applications/surveyor/webcamstereo/MainWindow.cs).

This program uses the modified version of fswebcam (see above) to grab images from camera devices.  To prevent thrashing the hard disk with a lot of saving and loading of images you may wish to create a RAM disk, like so:

```
mkdir /home/myusername/ramdisk
sudo mount -t tmpfs none /home/myusername/ramdisk -o size=10m
```

A ten megabyte RAM disk should be more than adequate to store the temporary image files.  You should then put the path which you created for the RAM disk into the _temporary\_files\_path_ variable within [MainWindow.cs](http://code.google.com/p/sentience/source/browse/trunk/applications/surveyor/webcamstereo/MainWindow.cs).


# Stereo vision server #

[Server source code](http://code.google.com/p/sentience/source/browse/#svn/trunk/applications/surveyor/stereoserver)

![http://lh4.ggpht.com/fuzzgun/SMWnVVwxO-I/AAAAAAAAAGA/gPS6EYcwsro/stereo_server.jpg](http://lh4.ggpht.com/fuzzgun/SMWnVVwxO-I/AAAAAAAAAGA/gPS6EYcwsro/stereo_server.jpg)

The stereo vision server developed for use with the [Surveyor stereo camera](http://code.google.com/p/sentience/w/edit/SurveyorSVS) can also be used with webcams, using the following syntax.  As previously a RAM disk may be used to store temporary files.

```
stereoserver -leftdevice <left camera device, eg. /dev/video0>
             -rightdevice <right camera device, eg. /dev/video1>
             -broadcastport <port number on which to broadcast stereo feature data>
             -algorithm <"simple" or "dense">
             -calibration <calibration filename>
             -width <width of the image coming from the stereo camera in pixels>
             -height <height of the image coming from the stereo camera in pixels>
             -record <save raw and rectified images to the given path>
             -fps <ideal number of frames per second>
             -ramdisk <ram disk path where temporary files will be stored>
```

The [client software](http://code.google.com/p/sentience/source/browse/#svn/trunk/applications/surveyor/stereoclient) also works identically, since it doesn't care what source the images came from.

```
stereoclient -server <IP address of the computer on which the stereo vision server is running>
             -port <broadcast port number on which the stereo vision server is communicating>
```