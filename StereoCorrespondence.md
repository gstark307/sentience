Stereo correspondence is the problem of discovering the closest possible match between two images captured simultaneously from cameras in different spatial locations.  Typically the cameras are alligned in such a way that each [scan line](ScanLine.md) of the [rectified](ImageRectification.md) images corresponds to the same line in the opposite image - something known as the [epipolar constraint](EpipolarConstraint.md).

It should be noted that solving the stereo correspondence problem under the epipolar constraint is a special class of a more general problem domain.  Near identical methods may be used to solve the problem of matching two images taken from the same camera but separated by some small difference in time as the robot moves through space (structure from motion).  It may also be used to _stitch_ or _register_ images together to form larger panoramas, maps, or synchronise deformable models.  The ideal stereo correspondence algorithm therefore would be a specialised subclass of this very general matching solution.

### Sparse or Dense? ###

There are two main approaches which can be taken to finding corresponding points in two or more images.

One way is to pick out [feature points](FeaturePoints.md) of high distinctiveness, such as [SIFT](http://en.wikipedia.org/wiki/Scale-invariant_feature_transform) features or [FAST](http://mi.eng.cam.ac.uk/~er258/work/fast.html) corners.  These features can typically be detected and matched at high speed, making this sort of correspondence a viable solution for real time robotics applications.  Using distinctive features may also be useful for sparse landmark based [SLAM](http://en.wikipedia.org/wiki/Simultaneous_localization_and_mapping).

An example of sparse stereo matching can be seen below.  The green spot features picked out in this case are vertical edges within the image, with larger spots indicating features which are closer to the cameras.

![http://sluggish.uni.cc/sentience/sentdemo.jpg](http://sluggish.uni.cc/sentience/sentdemo.jpg)

### Dense Stereo Correspondence ###

Source code: [sentience\_stereo\_contours.cs](http://sentience.googlecode.com/svn/trunk/sentcore/sentience_stereo_contours.cs)

The other way is to try to match as many pixels as possible, typically known as _dense stereo matching_.  Dense stereo is useful if the system needs to recover the detailed geometry of the scene, in order to facilitate obstacle avoidance or object recognition.  Efficient methods for dense stereo correspondence have been slow to materialise.  There are a few companies selling commercial stereo vision products which utilise dedicated hardware in order perform _brute force_ correspondence searching.  Even with hardware acceleration this is a very unintelligent way to solve the problem, and is particularly prone to matching errors.  This problem can be solved in a more practical and far less expensive way by using _multi-scale receptive field contours_, whereby areas of the image are labelled according to their [centre/surround](CentreSurround.md) frequency and amplitude prior to being matched.  Using an efficient algorithm dense stereo matching becomes a practical proposition for robotics use, even on low end single core processors.

An example of dense stereo can be seen in the images below.  Here an orange juice carton is observed and depth data derrived from correspondence matching is used to update a 3D [occupancy grid](OccupancyGrid.md).

![http://sluggish.uni.cc/sentience/occupancy2.jpg](http://sluggish.uni.cc/sentience/occupancy2.jpg)

### Dense Stereo Algorithm ###

This method, developed after trying numerous alternative algorithms, seems to give good results in all kinds of conditions, is robust to the kind of noise which you typically find in webcam images and can be executed with sufficient speed for use on a moving robot.

The steps involved in producing a dense disparity map are as follows:

1.  Convert the original images into mono (one byte per pixel).  At present colour information is not used.  Although colour certainly provides an additional basis for matching it does slow down the algorithm somewhat.

2.  [Rectify](http://code.google.com/p/sentience/wiki/ImageRectification) the images using the camera calibration settings.  This removes distortions due to the shape of the lens, which may be especially severe for wide angle lenses.

3.  Calculate the centre/surround and left/right responses for each image using two or three scales.

![http://sentience.googlegroups.com/web/stereo_correspondence_patterns.jpg](http://sentience.googlegroups.com/web/stereo_correspondence_patterns.jpg)

Here the responses are calculated as square or rectangular shaped regions for speed, using the [integral image](http://kos.informatik.uni-osnabrueck.de/download/icra2005/node12.html) method.  In theory the centre/surround response should be calculated from circular or [elliptical regions](http://www.pubmedcentral.nih.gov/articlerender.fcgi?artid=1330602), but in this case we trade accuracy for speed.  Experiments using the more accurate rounded receptive fields indicate that the improvement in accuracy is marginal at best.

The functions carrying out these operations can be found within the detectBlobs method in [classimage](http://sentience.googlecode.com/svn/trunk/sentcore/classimage.cs).  Notice that these are both measures of _relative pixel intensity_, so we do not need to carry out any sort of intensity/colour normalisation upon the left and right images prior to matching.  For each pixel we keep track of the pattern (either centre/surround or left/right) and scale which gave us the largest response.  What we're trying to do here is maximise the amount of information which is useful for finding correspondences.  In principle multiple patterns could be tried, but in practice I've found that these two give the best results, with additional patterns not adding much to the quality of the disparity map.

_The use of centre/surround receptive fields in this case is loosely inspired from biology.  However, we can gain efficiencies over biology since we do not need to implement separate [on-centre and off-centre fields](http://en.wikipedia.org/wiki/Receptive_field).  In the biological case separate cell types are needed because it's not possible to produce a negative neuron firing rate._

4.  So for left and right images we now have a set of pattern response magnitudes together with information about the maximally responding pattern and scale.  We can then add further labels to each pixel depending upon whether the change in magnitude (gradient) for each row is ascending or descending, and also include changes in gradient.  What we're trying to do is give each pixel an ID or fingerprint which is as unique as possible, so that when matching the rows in left and right images only a few candidate matches need to be tried, rather than the usual _brute force search_ carried out by dedicated hardware systems.

5.  For additional discriminatory power (and hence matching speed) we can also binarise each row and column and add a flag to each pixel indicating whether it is above or below the average intensity for that row/column.  This is a simple but effective strategy which allows large areas of the search space to be quickly disguarded.

6.  Another trick is to find the single maximally responding vertical edge within each column and compare these between the two images, which also helps to exclude matches which may look similar on an individual row (for example the "picket fence" effect) but have substantially dissimilar vertical contexts.

7.  For each row in the left and right images we then compare the various indicators calculated for each pixel:

**Maximally responding pattern type**
Either centre/surround or left/right

**Maximally responding scale**
The scale which gives us the highest pattern response magnitude

**Pattern response magnitude**
Are the response magnitudes in left and right images similar?
Is the magnitude above or below zero?

**Pattern response gradient**
Are the response gradients in left and right images similar?
Is the gradient above or below zero?

**Vertical context**
Is there some similarity in the properties of image columns?

So after all these checks have been carried out we're only left with a few candidate matches, out of which we can pick the closest match based upon difference in magnitude and gradient.


### Updating the disparity map ###

Once stereo correspondence matches have been found for each row we can use this information to update the disparity map.  When updating the map we add an entry not only for the pixel for which the disparity value was found but also for a few neighbouring pixels, according to a gaussian probability distribution.  This takes into account the fact that the system could have made a single pixel position mistake, and helps to increase the reliability and accuracy of the disparity values, avoiding the usual situation where "snow" appears due to singular bad matches.  It also gives us sub-pixel accuracy based upon multiple neighbouring disparity matches, thereby helping to satisfy the usual continuous disparity change constraint.

Disparity values may subsequently be randomly sampled from the disparity map and turned into [probability density functions](http://code.google.com/p/sentience/wiki/StereoSensorModel) which may be inserted into a 3D occupancy grid.  For speed these density values are stored within pre-calculated lookup tables.