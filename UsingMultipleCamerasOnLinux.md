When you only ever have a single camera attached to a computer then life is easy, but if you need to have multiple cameras attached - for example with one or more stereo cameras - then keeping track of the index numbers assigned to each camera can become a nightmare. If cameras are unplugged and then reconnected they can be assigned completely different index numbers.

To have a consistent multi-camera system you need to set up some _udev_ rules. First look at the parameters for each of your video devices. For example, for _/dev/video0_

```
    udevadm info -a -p $(udevadm info -q path -n /dev/video0) 
```

This will provide a list of attributes associated with the device. Create a text file similar to the following:

```
    KERNEL=="video[0-9]*", ATTRS{serial}=="A1CDE628", SYMLINK+="video-front-left"
    KERNEL=="video[0-9]*", ATTRS{serial}=="70CDEF10", SYMLINK+="video-front-right"
    KERNEL=="video[0-9]*", ATTRS{serial}=="208AEF19", SYMLINK+="video-rear-left"
    KERNEL=="video[0-9]*", ATTRS{serial}=="E929E62D", SYMLINK+="video-rear-right" 
```

In this case I've picked out the serial number attribute which is unique to each camera, then assigned a more meaningful name to the device. Save this file with a filename such as _10-video.rules_, copy it to _/etc/udev/rules.d_ then unplug and reconnect your cameras.

Listing the video devices then looks like this:

```
    /dev/video0
    /dev/video1
    /dev/video2
    /dev/video3
    /dev/video4
    /dev/video-front-left
    /dev/video-front-right
    /dev/video-rear-left
    /dev/video-rear-right
```

By referencing devices by their new names, such as _/dev/video-front-left_, you can then achieve a completely consistent interface with your software, no matter what the order in which cameras were originally connected.