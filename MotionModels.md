The world is a very uncertain place.  Both the sensor readings and the actuators of the robot are subject to errors of various kinds.  Images can be distorted by lens shape, blurred by environmental conditions and are always quantised into pixels which loses or obscures some of the small scale details.  The robots gearboxes may contain some amount of mechanical _slop_ and the friction between the wheels and the ground may vary depending upon the type of surface over which it is moving.

In most robotics applications changes in position and orientation are calculated in a completely deterministic way and assume that the amount of error is negligible.  However, this is never truly the case.  To be able to handle uncertainties in its own motion the robot needs a probabilistic _motion model_ which explicitly allows for the possibility of error.  To do this we can use a particle-like method, where each particle represents a hypothesis about the robots position and orientation based upon recent motor commands.  An advantage of using particles is that non-gaussian probability distributions can be modelled, which might be the case if the robot were being driven by mechanically complex articulated legs.

### Open Loop ###

The diagram below shows what happens to the distribution of position hypotheses over time as a robot moves forwards in a straight line without any feedback from the environment (open loop).  Each black dot represents a possible position, sometimes also referred to as a _trial pose_.

![http://sentience.googlegroups.com/web/motion_model_open_loop.jpg](http://sentience.googlegroups.com/web/motion_model_open_loop.jpg)

To begin with the robot is relatively certain about its location, with all the hypotheses being clustered together within a small area.  Over time in the absence of any feedback in order to ascertain which hypotheses are more likely than others the distribution gradually diverges until the robot is completely clueless as to where it really is.

All open loop systems suffer from this kind of dissolution, whether they be robots, human relationships or systems of political governance.  For example in a democracy there is periodic feedback from the population in the form of elections, maintaining a sloppy but consistent system, whereas in a dictatorship there is either no feedback or _inappropriate_ feedback (advisers tell the dictator what they believe he wants to hear rather than what is actually happening).  The dictator usually has to resort to increasingly tyranical methods in order to stay in control of the system.

### Closed Loop ###

If we test each of the possible pose hypotheses against observations of the environment - in this case against an [occupancy grid map](OccupancyGrid.md) - we can assign a score to each of these possibilities depending upon how well the robots observations from that position matched the map.  By applying a selectionist strategy where persistent low scoring poses are removed and replaced by new ones chosen to be closer to higher scoring pose hypotheses we can avoid the runaway uncertainty seen on the open loop situation.  This is _grid based localisation_.

![http://sentience.googlegroups.com/web/motion_model_closed_loop.jpg](http://sentience.googlegroups.com/web/motion_model_closed_loop.jpg)


### Further Reading ###

[Learning Probabilistic Motion Models for Mobile Robots](http://www.aicml.cs.ualberta.ca/_banff04/icml/pages/papers/284.ps) by Austin I. Eliazar and Ronald Parr.

[Probabilistic Robotics](http://www.probabilistic-robotics.org/), Robot Motion (Chapter 5), by Sebastian Thrun, Wolfram Burgard and Dieter Fox.