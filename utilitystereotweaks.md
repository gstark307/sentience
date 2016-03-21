The automatic stereo camera calibration isn't always completely reliable, so I found it useful to also have a utility which can be used to manually check what a pair of stereo images look like, and alternate between them to visualise the disparity.  If the cameras are well calibrated objects in the distance should appear to move less than objects which are nearer.

The utility can be used to manually tweak the X and Y offset values, and also adjust scale and rotation of one image relative to the other.  You can then animate the images to check that the disparity seems reasonable and that lens distortion has been removed.

First ensure that the core libraries are installed:

```
    sudo dpkg -i sentience-core-x.x.deb
```

Then install the utility:

```
    sudo dpkg -i stereotweaks-x.x.deb
```

Also add the following to _.bashrc_

```
    PATH=$PATH:/usr/bin/sentience/stereotweaks
    export PATH
```

The program can then be run either by calling

```
    stereotweaks.exe
```

or optionally you can specify the left and right image filenames

```
    stereotweaks.exe -left leftimage.jpg -right rightimage.jpg
```

The images don't necessarily need to be in jpeg format, and could be bitmaps or png files.