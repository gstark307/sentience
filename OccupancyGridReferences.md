These are what I think are the more instructive articles on occupancy grid mapping.  In the classic method described by Hans Moravec it should be noted that the exact position and orientation of the robot is always known.  Only in the more recent SLAM articles do you have a situation where the robots position uncertainty is taken into account.


_Old skool stuff from the 1980s._

  * [High Resolution Maps from Wide Angle Sonar](http://www.frc.ri.cmu.edu/~hpm/project.archive/robot.papers/1985/al2.html), Hans P. Moravec & Alberto Elfes


_Some sensor modelling, but for sonar rather than vision sensors._

  * [Learning Sensor Models for Evidence Grids](http://www.frc.ri.cmu.edu/~hpm/talks/Sonar.figs/1991.sensor.model.html), Hans Moravec & Mike Blackwell


_The classic 1996 paper.  This was one of the first articles which I read in connection with stereo vision.  Even over a decade later few researchers seem to have done much better than this using vision sensors._

  * [Robot Spatial Perception by Stereoscopic Vision and 3D Evidence Grids](http://www.ri.cmu.edu/pub_files/pub1/moravec_hans_1996_4/moravec_hans_1996_4.pdf), Hans Moravec


_Describes optimisation of sensor models by minimisation of colour variance within the grid.  Colour variance can be used as one way of measuring occupancy grid quality._

  * [Robust Navigation by Probabilistic Volumetric Sensing](http://www.frc.ri.cmu.edu/~hpm/project.archive/robot.papers/2000/ARPA.MARS.reports.00/Report.0006/Report.0006.html), Hans Moravec


_A description of the basic distributed particle SLAM method._

  * [DP-SLAM: Fast, Robust Simultaneous Localization and Mapping Without Predetermined Landmarks](http://www.cs.duke.edu/~eliazar/papers/DP-SLAM.pdf), Austin Eliazar & Ronald Parr


_A more recent paper on DP-SLAM, including some tweaks on the original algorithm._

  * [Hierarchical Linear/Constant Time SLAM Using Particle Filters for Dense Maps](http://www.cs.duke.edu/%7Eparr/nips05.pdf), Austin Eliazar & Ronald Parr

_Inspiration from biology_

  * [Scale-Invariant Memory Representations Emerge from Moiré Interference between Grid Fields That Produce Theta Oscillations: A Computational Model](http://www.jneurosci.org/cgi/content/abstract/27/12/3211), Hugh T. Blair, Adam C. Welday, and Kechen Zhang