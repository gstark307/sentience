Firstly it should be noted that this is not yet a complete system, since it is so far untested on an actual mobile robot.  However, it is possible to compile and run some programs.

## System Requirements ##

The system has been written with the intention that it should run on any reasonably modern computer hardware - even fairly minimal processors.  Until recently most development has been done on a system having a 1.8GHz single core processor and 512MB RAM, which should probably be regarded as a minimum specification.

Obviously the better the hardware specification the more smoothly everything will run, and multi-threading code is being used to ensure that it should be scalable as more computational power becomes available in the coming years.

On a Microsoft Windows system you'll need to have [Visual C# 2005](http://msdn2.microsoft.com/en-us/express/aa700756.aspx) (or later) installed.  On GNU/Linux systems you'll need to install [Mono](http://www.mono-project.com/Main_Page) and the [MonoDevelop](http://www.monodevelop.com/Main_Page) IDE.

For visualisation of 3D occupancy grids you'll also need to install [IFrIT](http://home.fnal.gov/~gnedin/IFRIT/), which runs on both Windows and GNU/Linux systems.

You'll also need a subversion client of some description in order to be able to check out the code.  I'm presently using [RapidSVN](http://rapidsvn.tigris.org/).


## Checkout ##

Create a directory called _/home/_

&lt;myusername&gt;

/develop/sentience_(or_c:\\develop\sentience_on Microsoft Windows systems), then use your preferred subversion client to check out the source code to that directory._

The location of the project to be checked out is

> http://sentience.googlecode.com/svn/trunk/



## Test Routines ##

Program location:  [sentience/applications/testFunctions](http://sentience.googlecode.com/svn/trunk/applications/testFunctions/)

The test routines are primarily intended for development and debugging purposes.  They allow the user to visualise certain key functions within the Sentience system, such as path planning, motion modeling and probabilistic sensor models.  For the casual user who is perhaps mildly interested in the algorithms but doesn't want to get involved with the messy practicalities of building robots or stereo cameras this is a good place to begin.

Some suggested experiments:

  * Try altering the [noise variables](http://sentience.googlecode.com/svn/trunk/sentcore/motionModel.cs), and observe what effects this has upon the motion model.

  * Change the [stereo model](http://sentience.googlecode.com/svn/trunk/sentcore/stereoModel.cs) parameters, such as the standard deviation (sigma) or the vergence angle, to see what effects these have upon the probability distributions.

_Note that because the sensor models are calculated in detail at high resolution it can take some time for these to be completed.  For actual usage on a mobile robot much lower resolution versions computed and inserted into a lookup table._


## Example Stereo Image Sequences ##

Location:  [sentience/testdata](http://sentience.googlecode.com/svn/trunk/testdata/)

Some example image sequences can be used to help test and develop the system.  These were gathered laboriously by hand from measured known positions.

Create a subdirectory called _seq1_ within the testdata directory, then unzip the contents of _sequence1.zip_ into this directory.  Each sequence contains images from the left and right cameras, together with a stereo camera calibration file, robot design file and simulation file which contains the known positions from which the images were taken.


## Stereo Vision Workbench ##

Program Location:  [sentience/applications/stereocorrespondence](http://sentience.googlecode.com/svn/trunk/applications/stereocorrespondence/)

The stereocorrespondence program can be used to help develop and test new stereo algorithms and view the results together with benchmark timings.

You can view the depth maps corresponding to stereo image sequences by using the stereocorrespondence program.  Make sure that you have some image sequence data unzipped, then run the program and click _next_ or _previous_ to step through the sequence.  On the menu you can select the level of detail you wish the depth maps to be calculated at.  The lowest detail depth maps can be run at 20Hz and would be suitable for real time obstacle avoidance.


## Simulation ##

Program Location:  [sentience/applications/mapping](http://sentience.googlecode.com/svn/trunk/applications/mapping/)

In order to be able to test the performance of the mapping and localisation without having to use an actual robot a simulation environment has been developed.  This is not terribly elaborate, but it does allow viewing of the map as it is created in a 2D overhead view, together with the evolution of the tree-based search strategy.  Benchmark timings are also displayed which give some indication as to whether the overall system would be suitable for real time operations.

Ensure that some stereo image sequences have been unzipped, then from the menu bar choose _File/Open_ and select the _simulation_ xml file.  On the next tab you may then run the simulation and observe the map which is created.  Note that the system keeps track of multiple mapping hypotheses which become increasingly refined over time.

Some suggested experiments:

  * Try altering the noise parameters within the MotionModel section of the robotdesign xml file and observe what effect this has upon the mapping performance.

  * Change the number of poses evaluated within the MotionModel section of the robotdesign xml file to see how this effects both the quality of mapping and the benchmark timings.

  * Alter the CullingThreshold within the MotionModel section of the robotdesign xml file, then view the evolution of the tree search.  Higher values (closer to 100) will mean that lower scoring hypothetical paths are more quickly pruned from the tree.

_Note that you'll need to re-load the simulation file in order for changes to the robotdesign file to take effect._