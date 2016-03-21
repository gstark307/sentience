**_"It is perhaps somewhat unfortunate that the subject <of camera calibration> must continue to be a drain on research efforts during the development of new systems."_** - Bob Fisher

Camera images normally contain some amount of spherical distortion, sometimes known as the _goldfish bowl effect_.  Linear features such as chair legs or door frames may appear to be warped, especially at the periphery of the image.  The job of camera calibration is to discover the nature of this distortion so that the resulting image may subsequently be undistorted or [rectified](ImageRectification.md).

Various methods of calibration exist.  The simplest method assumes a perfectly symmetrical lens around the [centre of distortion](CentreOfDistortion.md), and uses a polynomial equation to represent how light rays are bent radially around this by the curvature of the lens.

The typical method of calibration involves having the camera observe a test pattern, which can consist of spots, lines or squares (or indeed any easily identifiable repeating pattern).  Since the real geometry of the pattern is known the lens distortion can be calculated, then used to rectify the image back into a flat geometry.

Various calibration utilities existed at the time when the Sentience software was being written, but usually they were either closed source, depended upon proprietary software components such as Matlab, or required some amount of manual intervention when detecting the calibration pattern.  So a new stereo vision calibration tool needed to be developed.

### Stages of Calibration ###

**Step 1: Find the edges**

To begin with we calculate a histogram of pixel intensity values within a circular region in the centre of the image. From this histogram we can compute average values for the dark and light pixels corresponding to dots on the calibration pattern and their background. These mean dark and mean light values then become the low and high thresholds of a canny edge filter. The result looks like this:

![http://farm4.static.flickr.com/3255/2560591201_10a825746e.jpg](http://farm4.static.flickr.com/3255/2560591201_10a825746e.jpg)

**Step 2: Locate the dots**

Once edges have been found the dots themselves are quite easy to find. This is done by finding connected line segments with a small grouping radius of a few pixels. Most dots on the pattern are a dark colour, with a red dot marking a known reference position relative to the robot's cameras. The exact centre point of each dot is calculated using a local centre of gravity of the edge pixel positions.

![http://farm3.static.flickr.com/2419/2560591347_e09404657a.jpg](http://farm3.static.flickr.com/2419/2560591347_e09404657a.jpg)

**Step 3: Find the centre square**

By doing a little local search around the red dot we can discover the four adjacent dots and form a square, like this:

![http://farm4.static.flickr.com/3162/2560591401_153f1e3f84.jpg](http://farm4.static.flickr.com/3162/2560591401_153f1e3f84.jpg)

**Step 4: Join the dots**

Probably the neatest part of the calibration algorithm is the way it joins the dots to form a grid. This is done by a recursive search, starting from the centre square and growing outwards. At each dot pre-existing local links are examined and the system makes predictions about where it expects other adjacent dots to be. You can see the prediction search areas marked as yellow circles. If the algorithm finds a dot within the predicted region it links to it, and continues until it can't find any more dots to connect with. Whilst linking the red centre dot is ignored.

Notice here that not all the dots on the pattern were detected and so the grid isn't complete, but this actually doesn't matter too much.

![http://farm4.static.flickr.com/3160/2560591483_4804e99a77.jpg](http://farm4.static.flickr.com/3160/2560591483_4804e99a77.jpg)

**Step 5: The Matrix**

Once a grid has been fitted coordinates may be assigned. Since we know the centre position from the red dot grid points can be assigned relative to that position, like so:

![http://farm4.static.flickr.com/3175/2561413818_1035241393.jpg](http://farm4.static.flickr.com/3175/2561413818_1035241393.jpg)

With coordinates assigned we can then dump the dot positions into a two dimensional array for later lookup.

**Step 6: Lines**

Purely for checking purposes a set of lines are also discovered, aligned with the axes of the pattern. The lines will be used as a method of visually checking that the rectification looks reasonable. Here each line is given a different colour for clarity. These make the warping effect of the camera lens more apparent.

![http://farm4.static.flickr.com/3132/2560591639_2ec0cf677c.jpg](http://farm4.static.flickr.com/3132/2560591639_2ec0cf677c.jpg)

**Step 7: Ideals Vs reality**

Based upon the known position of the centre red dot we can reproject a set of points into the image which are the ideal positions of each dot if the image were to be perfectly rectified.

![http://farm4.static.flickr.com/3265/2578103963_a567e63b13.jpg](http://farm4.static.flickr.com/3265/2578103963_a567e63b13.jpg)

**Step 8: Curves in space**

For every dot we can compute its radial distance from the centre of lens distortion. In an ideal world the centre of lens distortion would be the centre of the image, but this is not actually always the case. We can find the radial distances both for the ideal (reprojected) dot and the actually detected dot, plot these points on a graph then fit a curve to them using linear regression. The curve fitting needs to be quite precise to get a good result, so an annealing-like search over multiple possible centres of lens distortion is performed to find the minimum curve fitting RMS error. A certain amount of random noise is added to the dot positions which is then progressively cooled down over time.

The resulting curve is a model of how the lens distorts light entering the camera.

![http://farm4.static.flickr.com/3250/2618716971_a062c8bd3d.jpg](http://farm4.static.flickr.com/3250/2618716971_a062c8bd3d.jpg)

An alternative way of viewing this curve is as a lens distortion model.  Concentric circles show ten degree intervals, and the shading gives some impression of the lens shape.

![http://farm4.static.flickr.com/3040/2618716909_a2d7afc93c.jpg](http://farm4.static.flickr.com/3040/2618716909_a2d7afc93c.jpg)

**Step 9: Lookup table**

We can use the lens distortion curve and centre of distortion position to directly calculate rectified positions, but in practice this is slow and involves doing a lot of tedious floating point calculations such as square roots. Instead we can use the curve to create a lookup table which maps pixels in the original image into pixels in a rectified image. Rectifying the image then becomes very easy and fast, which is useful for a busy robot on the move.

You can see the rectified lines here, which gives a good indication of whether the algorithm was successful or not.

![http://farm4.static.flickr.com/3138/2578103893_116c18bd3f.jpg](http://farm4.static.flickr.com/3138/2578103893_116c18bd3f.jpg)

and the rectified image now looks like this:

![http://farm4.static.flickr.com/3030/2578104011_2bf86f508a.jpg](http://farm4.static.flickr.com/3030/2578104011_2bf86f508a.jpg)

Notice that everything now looks much straighter. This isn't all there is to the calibration procedure, but it covers the essentials. We can also correct for slight rotation of the camera and include that within the calculation of the lookup table.



The final result for both left and right camera images looks like this:

![http://farm4.static.flickr.com/3030/2641981504_fd93c7d74b_o.jpg](http://farm4.static.flickr.com/3030/2641981504_fd93c7d74b_o.jpg)



### Users guide ###

![http://sentience.googlegroups.com/web/fishfood.jpg](http://sentience.googlegroups.com/web/fishfood.jpg)

Sentience contains an automatic monocular or stereo camera calibration system, called FishFood.  The procedure requires a calibration pattern, but I have tried to keep the system as simple as possible, such that it could be carried out _in the field_ by a non-expert using only easily available materials.

Ingredients required:

  * [Stereo camera](HowToMakeAStereoCamera.md)
  * Large piece of cardboard
  * Black marker pen
  * Tape measure
  * Straight object to use as a drawing guide, such as a ruler or piece of wood

Instructions:

1.  Draw a grid pattern on the cardboard using the pen and some straight object as a guide.  Cardboard is a good material to use, since it is non-reflective and easy to draw on.  You can choose anything you like as the spacing between lines (provided that it's regular).  I've found that 50 millimetres (5cm) seems to be a good spacing value.  Also, draw a small spot about 1.5cm in diameter to the north west of the centre of the pattern.

2.  Lay the calibration pattern down on the floor and have the stereo camera look down towards the centre of the pattern.  Ensure that the centre of the two cameras is aligned with the centre of the pattern.  The cameras would typically be mounted on a pan and tilt mechanism on the head of a robot, but in this case I'm just using the back of a chair as a substitute for a mobile robot.

![http://sentience.googlegroups.com/web/calibration_pattern.jpg](http://sentience.googlegroups.com/web/calibration_pattern.jpg)

3.  Measure the distance along the ground from the stereo camera to the centre of the calibration pattern.  Also, measure the height of the cameras above the floor.  To be as accurate as possible you should be measuring distances from the lenses of the cameras.

4.  Run the calibration program and enter the measurement values, pattern spacing, field of vision and stereo camera baseline distance, then from **video** on the menu bar select **start camera**.  The program has two modes of operation: _alignment mode_ and _calibration mode_, toggled by the button at the top left of the screen.  When video streaming initially commences you will be in alignment mode.  When the cameras are running use the large white crosshairs to align the cameras with the pattern, like this:

![http://sentience.googlegroups.com/web/calibration_roi.jpg](http://sentience.googlegroups.com/web/calibration_roi.jpg)

**Additional note:  The Creative Webcam NX Ultra cameras used in this example already appear to be pre-rectified either in hardware or as part of their software drivers.  This seems to be the exception rather than the norm, with most webcam images containing some amount of distortion**

The first thing to check is that left and right cameras are connected correctly.  Looking from behind the stereo camera the left camera should be on your left.  You can check this simply by covering the lens of one of the cameras.  If the cameras are in the wrong order exit the program, unplug the cameras and plug them back in the other way around.

5.  If the pattern does not fill the entire image you can define a _region of interest_ by clicking on the top left or bottom right areas of the image.  This will instruct the program where to look for edge features.  Regions of interest appear as green boxes.

6.  Click on the button marked **Calibration** to begin the calibration process.

7.  A status message to the left of the screen will show when the automatic calibration is completed.  If the calibration seems to be taking a long time use the drop down list to select edges, corners or lines and check that these features are being detected properly.  Using the drop down list check the rectified images to see that they look reasonable.  A common problem is that the centre spot indicated by a white square within the corners image is not being detected, which may be due to bad lighting.  If the calibration has been successfully completed the view will automatically switch to show the rectified image.  Just for fun, an example of a **bad rectification** is shown below.

![http://sentience.googlegroups.com/web/calibration_bad.jpg](http://sentience.googlegroups.com/web/calibration_bad.jpg)

8.  When you are happy that all is well, select **Save As** from the **File** menu to save the calibration results.  Parameters for stereo camera calibration are stored within an XML format which may then be read by other programs within the _Sentience_ system.  A typical calibration file looks like this:

```

<?xml version="1.0" encoding="ISO-8859-1"?>
<!--Sentience 3D Perception System-->
<Sentience>
  <StereoCamera>
    <!--Name of the WDM software driver for the cameras-->
    <DriverName>Creative WebCam NX Ultra</DriverName>
    <!--Position and orientation of the camera relative to the robots head or body-->
    <PositionOrientation>
      <!--Position in millimetres-->
      <PositionMillimetres>0,0,0</PositionMillimetres>
      <!--Orientation in degrees-->
      <OrientationDegrees>0,0,0</OrientationDegrees>
    </PositionOrientation>
    <!--Focal length in millimetres-->
    <FocalLengthMillimetres>5</FocalLengthMillimetres>
    <!--Camera baseline distance in millimetres-->
    <BaselineMillimetres>100</BaselineMillimetres>
    <!--Calibration Data-->
    <Calibration>
      <!--Image offsets in pixels due to small missalignment from parallel-->
      <Offsets>23.21169,-1</Offsets>
      <Camera>
        <!--Horizontal field of view of the camera in degrees-->
        <FieldOfViewDegrees>78</FieldOfViewDegrees>
        <!--Image dimensions in pixels-->
        <ImageDimensions>320,240</ImageDimensions>
        <!--The centre of distortion in pixels-->
        <CentreOfDistortion>157,112</CentreOfDistortion>
        <!--Polynomial coefficients used to describe the camera lens distortion-->
        <DistortionCoefficients>0,1.231268,0.0002229516</DistortionCoefficients>
        <!--Scaling factor-->
        <Scale>0.8125</Scale>
        <!--Rotation of the image in degrees-->
        <RotationDegrees>-0.3941584</RotationDegrees>
        <!--The minimum RMS error between the distortion curve and plotted points-->
        <RMSerror>3.328376</RMSerror>
      </Camera>
      <Camera>
        <!--Horizontal field of view of the camera in degrees-->
        <FieldOfViewDegrees>78</FieldOfViewDegrees>
        <!--Image dimensions in pixels-->
        <ImageDimensions>320,240</ImageDimensions>
        <!--The centre of distortion in pixels-->
        <CentreOfDistortion>155,113</CentreOfDistortion>
        <!--Polynomial coefficients used to describe the camera lens distortion-->
        <DistortionCoefficients>0,1.15394,0.0006436906</DistortionCoefficients>
        <!--Scaling factor-->
        <Scale>0.8125</Scale>
        <!--Rotation of the image in degrees-->
        <RotationDegrees>-0.6620849</RotationDegrees>
        <!--The minimum RMS error between the distortion curve and plotted points-->
        <RMSerror>3.4475</RMSerror>
      </Camera>
    </Calibration>
  </StereoCamera>
</Sentience>

```


### What to do next? ###

[Design your robot](RobotDesigner.md)

### Further Reading ###

For more information on camera calibration methods a nice summary of the history of this subject can be found in [The Development of Camera Calibration Methods and Models](http://sluggish.uni.cc/sentience/CameraCalibrationMethods.pdf) by T.A. Clarke and J.G. Fryer.

Also see

[Calibration of Stereo Cameras for Mobile Robots](http://www.dis.uniroma1.it/~iocchi/stereo/calib.html) by [Luca Iocchi](http://www.dis.uniroma1.it/~iocchi/).

[Autonomous Cross-Country Navigation Using Stereo Vision](http://sentience.googlegroups.com/web/singh_sanjiv_1999_1.pdf), Sanjiv Singh and Bruce Digney