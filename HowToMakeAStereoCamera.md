You don't need to use prohibitively expensive dedicated hardware to be able to experiment with stereoscopic vision.  Most webcams these days are of a reasonable quality and have a sufficiently high frame rate to be practical on slow moving domestic robots.

Here's how to build a stereo camera in 30 minutes at a cost of under 30 quid (about $60).

### Choose your cameras ###

There are zillions of webcams now on the market, and the models and megapixels are constantly changing.  Firstly I should say that both cameras should be of an identical model.  This might seem obvious, but I have seen folks in the past try to do stereo using two entirely different cameras and they soon get themselves into trouble.  Quite apart from any aesthetic considerations using the same model ensures that the optics of both cameras will be somewhat similar and they will have the same [field of view](FieldOfView.md) and focal lengths.

If possible try to acquire webcams which use a CCD chip, rather than the CMOS type.  CMOS cameras are ok in most situations, but under low illumination conditions such as artificial lighting (especially the low intensity _energy saving_ light bulbs) CCD has superior performance characteristics.  Another alternative is to use a camera which has its own LED illumination, although this could be problematic in borderline situations where the LEDs might switch on and off erratically.  If you can't find any CCD based cameras, or they're too expensive, just stick with good old CMOS.

In the world of digital photography megapixels rule.  However, for a stereo vision system you actually don't need a particularly high resolution.  640x480, or even a mere 320x240 pixels are quite adequate for ranging of distances up to about three metres.  When selecting cameras think _cheap and nasty_ rather than _top of the range_.  Any extraneous features or software (other than the driver) are strictly surplus to requirements.

If possible try to use cameras with a wide field of view.  The standard field of view for webcams (at the time of writing) is 40 degrees horizontal and 20 degrees vertical.  There are a few with a wider view than this.  A wide field of view just means that the robot _can see more of what's ahead_ at one time than would otherwise be the case, so things like obstacle avoidance or feature tracking work better.

For this system I'm going to use the _Creative Webcam NX Ultra_.  It's not the smallest webcam you've ever seen, but it has a wide 78 degree field of view and uses a CCD chip.  Most important of all, they only cost 13 pounds each on eBay.

### Construction ###

You'll need something to mount the cameras onto as a backplate.  For this I used a piece of strip aluminium from a local DIY store.  The metal is light, yet thick enough not to bend easily - a very important property for a mobile robot.

Cut off a strip of metal so that you can mount the cameras onto it with a [baseline separation](StereoBaseline.md) of 100mm between the centres of each lens.  Why 100mm?  Well, there doesn't seem to be any general agreement on an appropriate baseline distance for mobile robot stereo vision.  It's just down to personal preference, and 100mm seems like a round number in the right sort of ballpack.  In the past I have used 70mm and 140mm spacings.

![http://sentience.googlegroups.com/web/make_stereo_camera1.jpg](http://sentience.googlegroups.com/web/make_stereo_camera1.jpg)

The _Creative_ webcams which I'm using are conveniently held together with screws which can be easily removed (it's almost as if they designed them to be dissassembled!).  Remove the top two screws from each camera, then drill holes in the aluminium strip using an appropriately sized bit, like this:

![http://sentience.googlegroups.com/web/make_stereo_camera2.jpg](http://sentience.googlegroups.com/web/make_stereo_camera2.jpg)

### Final assembly ###

Now its time to assemble the whole caboodle.  The stereo camera singularity **_is near_**.  Screw the cameras onto the metal backplate, and make sure that they're secure.  In my case I found that the screws weren't long enough, and instead used some small screws which came with radio control servos as a substitute.  You may also wish to drill a few holes in the centre of the metal strip to allow the system to be securely bolted onto your robot.

![http://sentience.googlegroups.com/web/make_stereo_camera3.jpg](http://sentience.googlegroups.com/web/make_stereo_camera3.jpg)

And that's all there is to it.  The next step is [camera calibration](CameraCalibration.md), and then the system is ready to be used as a fully operational ranging device.