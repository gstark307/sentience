Stereoscopic ranging has a long and honourable history.  Before the technology of [radar](http://en.wikipedia.org/wiki/Radar) came along in the mid part of the 20th century battle ships used mirrors and clockwork mechanisms in order to accurately calculate the range to an enemy ship.  Mirrors on either side of the ship reflected light to a central location, where a human observer would turn a wheel to manually align the two images.  When the images were perfectly aligned the distance to the ship could be read from a clockwork dial.  This same ranging method was later adapted and made smaller so that it could be used on battle tanks.  Up until the mid 1960s when laser technology arrived tanks used manual stereoscopic correspondence to calculate the correct gun elevation for long range targets.

For stereo vision calculating the range to an observed object or feature is fairly straightforward.

> Range = ( Focal length x [Camera baseline](StereoBaseline.md) ) / [Disparity](StereoDisparity.md)

Where the [disparity](StereoDisparity.md) is the horizontal difference in pixels between the position of the feature in left and right images, the focal length is also expressed in pixels and the baseline distance is in millimetres.

![http://sentience.googlegroups.com/web/stereo_vision_geometry1.jpg](http://sentience.googlegroups.com/web/stereo_vision_geometry1.jpg)

To convert disparity in pixels to a distance value some example code is given as follows:

```
        /// <summary>
        /// returns the distance for the given stereo disparity
        /// </summary>
        /// <param name="disparity_pixels">disparity in pixels</param>
        /// <param name="focal_length_mm">focal length in millimetres</param>
        /// <param name="sensor_pixels_per_mm">number of pixels per millimetre on the sensor chip</param>
        /// <param name="baseline_mm">distance between cameras in millimetres</param>
        /// <returns>range in millimetres</returns>        
        public static float DisparityToDistance(float disparity_pixels,
                                                float focal_length_mm,
                                                float sensor_pixels_per_mm,
                                                float baseline_mm)
        {
            float focal_length_pixels = focal_length_mm * sensor_pixels_per_mm;
            float distance_mm = baseline_mm * focal_length_pixels / disparity_pixels;
            return(distance_mm);
        }
```

See [here](SensorPixelDensity.md) for possible ways of finding out the sensor pixel density.

However, this is not the end of the story.  Since there is uncertainty associated with the disparity measurement (which can be up to plus or minus half a pixel) the actual range lies within a [spatial probability distribution](StereoUncertainty.md), which can be represented using a [sensor model](StereoSensorModel.md).

![http://sentience.googlegroups.com/web/stereo_vision_geometry2.jpg](http://sentience.googlegroups.com/web/stereo_vision_geometry2.jpg)