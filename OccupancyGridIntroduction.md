An _occupancy grid_ is a typically a two or three dimensional grid divided up into small square or cubic units known as _cells_ or _voxels_.  Each cell within the grid stores a value which represents the probability of it being occupied (a positive value) or empty (a negative value).  Cells may also contain additional information such as the average observed colour.

3D occupancy grids are the most effective method for representing physical space at a reasonably high resolution.  Even though the sensor readings used to update the grid may be noisy and contain inaccuracies, over time because of the probabilistic nature of the way grid cells are updated the truth accumulates and inconsistencies cancel out.

Early efforts using the [Sentience project](http://sluggish.uni.cc/sentience/sentience.htm) concentrated upon building grids modelling the space within a short range of the robot, in order to facilitate object recognition.  More recent work is aimed at building maps suitable for mobile robot navigation and detection of large objects such as chairs and tables.

The stages involved in creating an occupancy grid are as follows:

  1. Grab images from cameras
  1. Perform [rectification](ImageRectification.md) to remove distortion due to the shape of the camera lenses
  1. Perform dense stereo correspondence to calculate a [depth map](DisparityMap.md)
  1. For each disparity create a ray of light using an appropriate [sensor model](StereoSensorModel.md) and insert it into the grid, taking into account the position and orientation of the robot
  1. Continue updating the grid over time as the robot accumulates experience of its surroundings

The hallway image sequence below, which is actually a sequence of stereo images rather than the monoscopic view shown, is turned into a 3D volumetric model which may then be viewed from different angles.

![http://static.flickr.com/119/316416417_aec21a9bff_o.gif](http://static.flickr.com/119/316416417_aec21a9bff_o.gif)
![http://static.flickr.com/103/316416421_1117860e57.jpg](http://static.flickr.com/103/316416421_1117860e57.jpg)

The blue area in the image above represents unoccupied vacant space at floor level, and the side walls and doorways can be seen.

![http://farm1.static.flickr.com/134/319594099_71623e87ed_o.gif](http://farm1.static.flickr.com/134/319594099_71623e87ed_o.gif)