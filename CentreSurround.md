In the initial stages of biological vision the centre/surround arrangement of receptive fields gives rise to some very rubust and invariant image processing properties.

![http://www.brainconnection.com/med/medart/l/anat/990501.jpg](http://www.brainconnection.com/med/medart/l/anat/990501.jpg)

For each point on the retina the neural architecture is arranged in such a fashion so that on-centre selective cells are activated more frequently if light is present in the centre of the receptive field but not present in the area immediately surrounding it.  Off-centre cells perform the reverse operation.

### Biology versus Simulation ###

When simulating this kind of arrangement on a computer we do not actually need to separately model on-centre and off-centre.  A simple subtraction of the centre area from the surround gives a single positive or negative result, analogous to neural firing frequency.  Of course in the biological situation neurons cannot have a _negative firing rate_, so there does need to be a division of architectures.

We can make further computational efficiencies if we are not too concerned about the shape of the receptive field.  If we approximate the field as a square area we can use the well-known _integral image_ method to return the centre/surround result using only a few lookups, without needing to touch individual image pixels multiple times.  Strictly speaking the square recptive field shape is not as effective as a circular one, but the huge computing speed advantage of using squares easily outweighs the very slight degradation in quality.

### Keeping the noise down ###

Whatever the receptive field shape, using the centre/surround arrangement is extremely useful for practical applications since it helps to filter out a large quantity of the noise which is usually present in camera images (especially webcams which may use lossy compression methods).  The traditional way to reduce noise was to pre-process the image using a gaussian filter, or to use expensive high quality imagers together with proprietory hardware.  Although good quality equipment will always help to improve performance it's not actually necessary and in commercial applications simply builds structural cost into the final product.  One consideration which I held to whilst developing the Sentience vision system was that biological vision is also an intrinsically noisy process.  The rod and cone light receptors of the retina are not highly precise measuring devices, so evolution has been forced by necessity to discover some simple but effective ways of reducing noise in the visual system.