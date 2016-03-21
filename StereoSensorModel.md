A sensor model is a set of values, typically held within a lookup table, which governs how the probability of an uncertain measurement taken from a particular type of sensor varies in space.  Some sensors give more certain information than others.  A laser for example gives quite precise values for reflections at close range, such that these may be considered merely to be points in space (a _point cloud_).  Readings from other types of sensor, such as ultrasonic or digital imagers, contain far more uncertainly and so require a model larger than a single point.

To create a sensor model suitable for stereo vision a numerical simulation can be used to exhaustively calculate the [probability of an observed feature existing](StereoUncertainty.md) at every point in space - something known as a _probability density function_.  This can then be used to create an appropriate lookup table for a variety of possible ranges.

A typical sensor model for stereoscopic vision looks something like the following.  This is for quite a large stereo disparity value.

![http://sentience.googlegroups.com/web/stereo_ray_sensor_model2.jpg](http://sentience.googlegroups.com/web/stereo_ray_sensor_model2.jpg)

For a smaller disparity value indicating a feature further away from the camera the "tail" of the sensor model is more dispersed, heading off to infinity.

![http://sentience.googlegroups.com/web/stereo_ray_sensor_model4.jpg](http://sentience.googlegroups.com/web/stereo_ray_sensor_model4.jpg)

The grey scales indicate the probability of the observed feature existing at that location in space.  The left side of the model is closest to the cameras, with the axis of the ray of light connecting the observed feature to the centre of the [camera baseline](StereoBaseline.md) being the vertical centre row.  Since the sensor model is symmetrical about the axis of the ray of light we only need to store the top half of this image as the sensor model.  Sensor model probability distributions vary depending upon the [calculated range](StereoRanging.md) of the feature.  Accurate sensor models are essential for creation of reasonable quality [3D occupancy grids](OccupancyGrid.md).

Probability density functions for 3, 5, 7 and 9 pixels of visual disparity are shown in the graphs below.  The graphs look rather rough because they were produced purely from a numerical simulation rather than being calculated directly from equations.  These show only the probabilities for the _probably occupied_ part of the sensor model.

![http://sentience.googlegroups.com/web/probability_density_functions.jpg](http://sentience.googlegroups.com/web/probability_density_functions.jpg)

### Pretty Vacant ###

In addition to having a sensor model for the existence (occupancy) of a feature we also need a model for the probable empty space (vacancy) between the camera and the feature.  This vacancy model is simpler, and can be used to _carve out_ empty space within an [occupancy grid](OccupancyGrid.md).  In practical navigation and mapping experiments it seems that it's the vacancy model more than any other factor which has a large influence upon the quality of the resulting 3D voxel model.

### The Complete Model ###

An example of a full inverse sensor model for stereo vision can be seen in the image below.  Here the grey area corresponds to probably occupied space, whereas the blue areas correspond to probably vacant space.

![http://sentience.googlegroups.com/web/stereo_vision_sensor_model.jpg](http://sentience.googlegroups.com/web/stereo_vision_sensor_model.jpg)

### Further Reading ###

[Probabilistic Robotics](http://www.probabilistic-robotics.org/), _Occupancy Grid Mapping (Chapter 9)_, Sebastian Thrun, Wolfram Burgard and Dieter Fox.
