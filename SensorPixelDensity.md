A common problem when calculating ranges from stereo vision is how do I know, or how can I find out the pixel density (pixels per mm) of the sensor chip or the focal length in pixels.  Especially for webcams these figures are usually unknown, and inquiring with the camera manufacturer is usually a waste of time (they either don't know, or won't tell you).

Below is a table of typical sensor sizes for CCD chips.  Most webcams are based upon CMOS, but I assume that the sensors come in the same physical sizes.  If you know the maximum resolution of the camera in pixels, and assume one of the smallest sensor sizes (smaller sensors are cheaper, but noisier), then you can figure out possible values for the number of pixels per millimetre.  By observing objects at known distances you can then make a good estimate of what the sensor pixel density or focal length in pixels is (assuming that your cameras are aligned in parallel with reasonable accuracy).

```
focal_length_pixels = distance_mm * disparity_pixels / baseline_mm;
sensor_pixels_per_mm = focal_length_pixels / focal_length_mm;
```


The other method is to disassemble the camera and physically measure the sensor.  I wouldn't recommend this unless you really know what you're doing, since it could result in the camera becoming unusable.


| Type    | Aspect  | Width        | Height   | Diagonal | Area    | Relative Area |
|:--------|:--------|:-------------|:---------|:---------|:--------|:--------------|
|         | Ratio   | mm           | mm       | mm	      | mm2     |	              |
| 1/6" 	  |  4:3    |	2.300        |	1.730    | 2.878    | 3.979   | 1.000         |
| 1/4" 	  |  4:3    |	3.200        |	2.400    | 4.000    | 7.680   | 1.930         |
| 1/3.6" 	|  4:3    |	4.000        |	3.000    | 5.000    | 12.000  |  3.016         |
| 1/3.2" 	|  4:3    |	4.536        |	3.416    | 5.678    | 15.495  |  3.894         |
| 1/3" 	  |  4:3    |	5.270        |	3.960    | 6.592    | 20.869  | 5.245         |
| 1/2" 	  |  4:3    |	6.400        |	4.800    | 8.000    | 30.720  | 7.721         |
| 1/1.8" 	|  4:3    |	7.176        |	5.319    | 8.932    | 38.169  |  9.593         |
| 2/3" 	  |  4:3    |	8.800        |	6.600    | 11.000   | 58.080  | 14.597        |
| 1" 	    |  4:3    |	12.800       |	9.600    | 16.000   | 122.880 | 30.882        |
|4/3" 	   |  4:3    |	18.000       |	13.500   | 22.500   | 243.000 | 61.070        |