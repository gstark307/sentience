Multiple methods exist for determining the range to an object.  The most popular for robotics use are _active sensing_ methods such as infrared, ultrasonic and laser ranging devices.  The trouble with all of these methods is that they only give quite low resolution information.  For infrared and ultrasonics the area of space within which objects may be detected is quite wide (several degrees at least), so that the precise position of an object may only be known in very vague terms.

A laser ranger on the other hand has a far more specific narrow beam, but at any point in time only gives a tiny _tunnel vision-like_ amount of information about the depth of the scene.  To overcome this problem in recent years _scanning laser rangefinders_ have become increasingly popular.  By scanning in one or more directions using a spinning mirror to direct a laser beam it's possible to build up [detailed information about the geometric structure of the environment](http://www.realitymapping.co.uk/).  However, this scanning process is relatively slow and relies upon precision engineered mechanical parts.  At the time of writing scanning laser rangefinders are still very expensive devices costing thousands of dollars, and remain only within the realm of high end academic, industrial or military applications.

Cameras can also be used as range sensing devices.  When more than one camera are arranged in some configuration, such that the same features in the environment can be observed from multiple viewpoints, matches made between [corresponding features](StereoCorrespondence.md) may be used to [calculate range values](StereoRanging.md).  Potentially, _every pixel in the camera image_ can be used as a highly focussed ranging device.

A few possibilities for camera based range sensing exist.

  1. A single camera taking pictures as it moves through space, with features being tracked and correlated over time.
  1. Two cameras taking photos near symultaneously, calculating ranges from [stereo correspondences](StereoCorrespondence.md) under the [epipolar constraint](EpipolarConstraint.md).
  1. Three cameras - a _trinocular_ system - working in a similar manner to stereo but with greater possibilities for accuracy and dissambiguation of features.  The only dissadvantage here is the greater computing resources needed to process three images rather than two, and calculate three dimensional light ray intersections.
  1. Use of two or three cameras together with tracking of features over time to produce a _spatio-temporal correlation_ system.  This is the ideal visual perception system, since tracking of features over time facilitates accurate long range measurements to be taken.


Key advantages of camera based systems.

  1. **Safety**.  Laser ranging devices may give good mapping results, but whether or not such devices will be deemed safe to use in a domestic environment containing people and pets remains unknown.  The lower power rated lasers seem to rely upon the human blink response for protection, which seems dubious and may not apply to cats and dogs.
  1. Although a trinocular system might be more accurate, using only two cameras gives a **minimally complex** solution.
  1. **High speed**.  Unlike methods which require mechanical scanning cameras can gather a large amount of range data in a very short space of time.
  1. **Very low cost** of digital imaging devices.  In the last five years digital imagers have become ubiquitous devices, used in digital cameras and mobile phones.  Low cost means that robots using stereo vision easily fall within the means of the robotics hobbyist or even consumer robotics domains.
  1. Cameras are **entirely solid state**, whereas laser scanners have moving parts which could potentially become unreliable during the _rough and tumble_ of robotic excursions.
  1. **Colour** information can be easily acquired at the same time as range data, helping to build realistic full colour 3D models of the environment.
  1. **Energy efficient**.  Active sensors need to pump a lot of energy into the environment, whereas passive sensors such as cameras don't need to do this and so may have a lower energy requirement.