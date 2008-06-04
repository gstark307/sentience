/**
 * This software has been released into the public domain.
 * Please read the notes in this source file for additional information.
 * 
 * This class provides a configurable implementation of the Canny edge
 * detection algorithm. This classic algorithm has a number of shortcomings,
 * but remains an effective tool in many scenarios. This class is designed
 * for single threaded use only.
 * 
 * Sample usage:
 * 
 * //create the detector
 * CannyEdgeDetector detector = new CannyEdgeDetector();
 * 
 * //adjust its parameters as desired
 * detector.setLowThreshold(0.5f);
 * detector.setHighThreshold(1f);
 * 
 * //apply it to an image
 * detector.setSourceImage(frame);
 * detector.Update("myimage.bmp", "edges.bmp");
 * 
 * For a more complete understanding of this edge detector's parameters
 * consult an explanation of the algorithm.
 * 
 * Original author (Java): Tom Gibara
 * C# implementation: Bob Mottram
 *
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace sentience.calibration
{
    public class CannyEdgeDetector 
    {
 	    // statics	
	    private static float GAUSSIAN_CUT_OFF = 0.005f;
	    private static float MAGNITUDE_SCALE = 100f;
	    private static float MAGNITUDE_LIMIT = 1000f;
	    private static int MAGNITUDE_MAX = (int) (MAGNITUDE_SCALE * MAGNITUDE_LIMIT);

    	// fields	
    	private int height;
    	private int width;
    	private int picsize;
    	private int[] data;
    	private int[] magnitude;
    	private float[] orientation;
    	private byte[] sourceImage;
    	private byte[] edgesImage;
    	
    	private float gaussianKernelRadius;
    	private float lowThreshold;
    	private float highThreshold;
    	private int gaussianKernelWidth;
    	private bool contrastNormalized;

    	private float[] xConv;
    	private float[] yConv;
    	private float[] xGradient;
    	private float[] yGradient;
        
        // list of edge locations
        public List<int> edges;

        // whether to calculate edge orientations
        public bool enable_edge_orientations;
        
        // control thresholds automatically based upon contrast
        public bool automatic_thresholds;
	
        #region "constructors"
    	
        /// <summary>
        /// Constructs a new detector with default parameters. 
        /// </summary>
    	public CannyEdgeDetector() 
        {
    		lowThreshold = 2.5f;
    		highThreshold = 7.5f;
    		gaussianKernelRadius = 2f;
    		gaussianKernelWidth = 16;
    		contrastNormalized = false;
            automatic_thresholds = true;
    	}
        
        #endregion

        #region "accessors"
    	
        /// <summary>
        /// The image that provides the luminance data used by this detector to
        /// generate edges.
        /// </summary>
        /// <returns>
        /// the source image, or null <see cref="System.Byte"/>
        /// </returns>
    	public byte[] getSourceImage() 
        {
    		return sourceImage;
    	}
    	
        /// <summary>
        /// Specifies the image that will provide the luminance data in which edges
        /// will be detected. A source image must be set before the process method
        /// is called.
        /// </summary>
        /// <param name="image">
        /// a source of luminance data <see cref="System.Byte"/>
        /// </param>
    	public void setSourceImage(byte[] image) 
        {
    		sourceImage = image;
    	}

        /// <summary>
        /// Obtains an image containing the edges detected during the last call to
        /// the process method. The buffered image is an opaque image of type
        /// BufferedImage.TYPE_INT_ARGB in which edge pixels are white and all other
        /// pixels are black.
        /// </summary>
        /// <returns>
        /// an image containing the detected edges, or null if the process <see cref="System.Byte"/>
        /// </returns>
    	public byte[] getEdgesImage() 
        {
    		return edgesImage;
    	}
     
        /// <summary>
        /// Sets the edges image. Calling this method will not change the operation
        /// of the edge detector in any way. It is intended to provide a means by
        /// which the memory referenced by the detector object may be reduced.
        /// </summary>
        /// <param name="edgesImage">
        /// expected (though not required) to be null <see cref="System.Byte"/>
        /// </param>
    	public void setEdgesImage(byte[] edgesImage) 
        {
    		this.edgesImage = edgesImage;
    	}

        /// <summary>
        /// The low threshold for hysteresis. The default value is 2.5.
        /// </summary>
        /// <returns>
        /// the low hysteresis threshold <see cref="System.Single"/>
        /// </returns>
    	public float getLowThreshold() 
        {
    		return lowThreshold;
    	}
    	

        /// <summary>
        /// Sets the low threshold for hysteresis. Suitable values for this parameter
        /// must be determined experimentally for each application. It is nonsensical
        /// (though not prohibited) for this value to exceed the high threshold value.
        /// </summary>
        /// <param name="threshold">
        /// a low hysteresis threshold <see cref="System.Single"/>
        /// </param>
    	public void setLowThreshold(float threshold) 
        {
    		//if (threshold < 0) throw new IllegalArgumentException();
    		lowThreshold = threshold;
    	}
     
        /// <summary>
        /// The high threshold for hysteresis. The default value is 7.5. 
        /// </summary>
        /// <returns>
        /// the high hysteresis threshold <see cref="System.Single"/>
        /// </returns>
    	public float getHighThreshold() 
        {
    		return highThreshold;
    	}
    	
        /// <summary>
        /// Sets the high threshold for hysteresis. Suitable values for this
        /// parameter must be determined experimentally for each application. It is
        /// nonsensical (though not prohibited) for this value to be less than the
        /// low threshold value. 
        /// </summary>
        /// <param name="threshold">
        /// a high hysteresis threshold <see cref="System.Single"/>
        /// </param>
    	public void setHighThreshold(float threshold) 
        {
    		//if (threshold < 0) throw new IllegalArgumentException();
    		highThreshold = threshold;
    	}

        /// <summary>
        /// The number of pixels across which the Gaussian kernel is applied.
        /// The default value is 16.
        /// </summary>
        /// <returns>
        /// the radius of the convolution operation in pixels <see cref="System.Int32"/>
        /// </returns>
    	public int getGaussianKernelWidth() 
        {
    		return gaussianKernelWidth;
    	}
    	
        /// <summary>
        /// The number of pixels across which the Gaussian kernel is applied.
        /// This implementation will reduce the radius if the contribution of pixel
        /// values is deemed negligable, so this is actually a maximum radius.
        /// </summary>
        /// <param name="gaussianKernelWidth">
        /// a radius for the convolution operation in pixels, at least 2 <see cref="System.Int32"/>
        /// </param>
    	public void setGaussianKernelWidth(int gaussianKernelWidth) 
        {
    		if (gaussianKernelWidth < 2) gaussianKernelWidth = 2;
    		this.gaussianKernelWidth = gaussianKernelWidth;
    	}

        /// <summary>
        /// The radius of the Gaussian convolution kernel used to smooth the source
        /// image prior to gradient calculation. The default value is 16.
        /// </summary>
        /// <returns>
        /// the Gaussian kernel radius in pixels <see cref="System.Single"/>
        /// </returns>
    	public float getGaussianKernelRadius() 
        {
    		return gaussianKernelRadius;
    	}
    	
        /// <summary>
        /// Sets the radius of the Gaussian convolution kernel used to smooth the
        /// source image prior to gradient calculation.
        /// </summary>
        /// <param name="gaussianKernelRadius">
        /// a Gaussian kernel radius in pixels, must exceed 0.1f. <see cref="System.Single"/>
        /// </param>
    	public void setGaussianKernelRadius(float gaussianKernelRadius) 
        {
    		if (gaussianKernelRadius < 0.1f) gaussianKernelRadius = 0.1f;
    		this.gaussianKernelRadius = gaussianKernelRadius;
    	}
    	
        /// <summary>
        /// Whether the luminance data extracted from the source image is normalized
        /// by linearizing its histogram prior to edge extraction. The default value
        /// is false.
        /// </summary>
        /// <returns>
        /// whether the contrast is normalized <see cref="System.Boolean"/>
        /// </returns>
    	public bool isContrastNormalized() 
        {
    		return contrastNormalized;
    	}
    	
        /// <summary>
        /// Sets whether the contrast is normalized
        /// </summary>
        /// <param name="contrastNormalized">
        /// true if the contrast should be normalized <see cref="System.Boolean"/>
        /// </param>
    	public void setContrastNormalized(bool contrastNormalized) 
        {
    		this.contrastNormalized = contrastNormalized;
    	}
        
        /// <summary>
        /// calculate a global threshold based upon an intensity histogram
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="MeanDark">returned mean dark value</param>
        /// <param name="MeanLight">returned mean light value</param>
        /// <param name="DarkRatio">ratio of dark pixels to light</param>
        /// <returns>global threshold value</returns>
        public static unsafe float GetGlobalThreshold(float[] histogram,
                                                      ref float MeanDark, ref float MeanLight,
                                                      ref float DarkRatio)
        {
            float global_threshold = 0;

            float Tmin = 0;
            float Tmax = 0;
            float MinVariance = 999999;  // some large figure
            float currMeanDark, currMeanLight, VarianceDark, VarianceLight;

            float DarkHits = 0;
            float LightHits = 0;
            float BestDarkHits = 0;
            float BestLightHits = 0;
            int histlength = histogram.Length;
            MeanDark = 0;
            MeanLight = 0;
            
            // calculate squared magnitudes
            // this avoids unnecessary multiplications later on
            float[] histogram_squared_magnitude = new float[histogram.Length];
            for (int i = 0; i < histogram.Length; i++)
                histogram_squared_magnitude[i] = histogram[i] * histogram[i];

            //const float threshold_increment = 0.25f;
            const int max_grey_levels = 255;
            
            // precompute some values to avoid excessive divisions
            float mult1 = histlength / (float)max_grey_levels;
            float mult2 = 255.0f / max_grey_levels;
            
            int h, bucket;
            float magnitude_sqr, Variance, divisor;

            fixed (float* unsafe_histogram = histogram_squared_magnitude)
            {
                // evaluate all possible thresholds
				for (int grey_level = max_grey_levels-1; grey_level >= 0; grey_level--)
                {
                    // compute mean and variance for this threshold
                    // in a struggle between light and darkness
                    DarkHits = 0;
                    LightHits = 0;
                    currMeanDark = 0;
                    currMeanLight = 0;
                    VarianceDark = 0;
                    VarianceLight = 0;
					
                    bucket = (int)(grey_level * mult1);
                    for (h = histlength-1; h >= 0; h--)
                    {
                        magnitude_sqr = unsafe_histogram[h];
                        if (h < bucket)
                        {
                            currMeanDark += h * magnitude_sqr;
                            VarianceDark += (bucket - h) * magnitude_sqr;
                            DarkHits += magnitude_sqr;
                        }
                        else
                        {
                            currMeanLight += h * magnitude_sqr;
                            VarianceLight += (bucket - h) * magnitude_sqr;
                            LightHits += magnitude_sqr;
                        }
                    }
                    					
                    // compute means
                    if (DarkHits > 0)
                    {
                        // rescale into 0-255 range
						divisor = DarkHits * histlength;
                        currMeanDark = currMeanDark * 255 / divisor;
                        VarianceDark = VarianceDark * 255 / divisor;
                    }
                    if (LightHits > 0)
                    {
                        // rescale into 0-255 range
						divisor = LightHits * histlength;
                        currMeanLight = currMeanLight * 255 / divisor;
                        VarianceLight = VarianceLight * 255 / divisor;
                    }

                    Variance = VarianceDark + VarianceLight;
                    if (Variance < 0) Variance = -Variance;

                    if (Variance < MinVariance)
                    {
                        MinVariance = Variance;
                        Tmin = grey_level * mult2;
                        MeanDark = currMeanDark;
                        MeanLight = currMeanLight;
                        BestDarkHits = DarkHits;
                        BestLightHits = LightHits;
                    }
                    if ((int)(Variance * 1000) == (int)(MinVariance * 1000))
                    {
                        Tmax = grey_level * mult2;
                        MeanLight = currMeanLight;
                        BestLightHits = LightHits;
                    }
                }
            }

            global_threshold = (Tmin + Tmax) / 2;

            if (BestLightHits + BestDarkHits > 0) 
                DarkRatio = BestDarkHits * 100 / (BestLightHits + BestDarkHits);

            return (global_threshold);
        }        
        
        /// <summary>
        /// automatically calculate the high and low canny thresholds
        /// based upon the mean light and dark values from a 
        /// histogram taken from the centre region of the image
        /// </summary>
        /// <param name="img">
        /// image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="sampling_step_size">
        /// step sized used to sample the image in pixels <see cref="System.Int32"/>
        /// </param>
        private void AutoThreshold(byte[] img,
                                   int sampling_step_size)
        {
            int bytes_per_pixel = img.Length / (width*height);
            int tx = width/3;
            int ty = height/3;
            int bx = width - 1 - tx;
            int by = height - 1 - ty;
            float[] histogram = new float[256];
            for (int y = ty; y <= by; y += sampling_step_size)
            {
                int n = ((y * width) + tx) * bytes_per_pixel;
                for (int x = tx; x <= bx; x += sampling_step_size)
                {
                    if (bytes_per_pixel > 1)
                    {
                        histogram[img[n+2]]++;
                    }
                    else
                    {
                        histogram[img[n]]++;
                    }
                    n += bytes_per_pixel;
                }
            }
            float mean_dark = 0;
            float mean_light = 0;
            float dark_ratio = 0;
            GetGlobalThreshold(histogram, ref mean_dark, ref mean_light, ref dark_ratio);
            mean_dark /= 255.0f;
            mean_light /= 255.0f;
            float contrast = mean_light - mean_dark;
            
            float fraction = (contrast - 0.048f) / (0.42f - 0.048f);            
            
            lowThreshold = 1.6f + (fraction * (8.0f - 1.6f));
            highThreshold = 2.0f + (fraction * (10f - 2.0f));
            
            //Console.WriteLine("contrast: " + contrast.ToString());
            //Console.WriteLine("low threshold: " + lowThreshold.ToString());
            //Console.WriteLine("high threshold: " + highThreshold.ToString());
        }
        
        #endregion
    	
        #region "methods"

        /// <summary>
        /// process the given image file
        /// </summary>
        /// <param name="filename">
        /// filename of the image to process <see cref="System.String"/>
        /// </param>
        /// <param name="edges_filename">
        /// filename to save edges image <see cref="System.String"/>
        /// </param>
    	public void Update(string filename, string edges_filename) 
        {            
            if (File.Exists(filename))
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                byte[] img = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, img);
                
                Update(img, bmp.Width, bmp.Height);
                
                Bitmap edges_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(edgesImage, edges_bmp);
                if (edges_filename.ToLower().EndsWith("jpg"))
                    edges_bmp.Save(edges_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                if (edges_filename.ToLower().EndsWith("bmp"))
                    edges_bmp.Save(edges_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }
        
        /// <summary>
        /// processes an image
        /// </summary>
        /// <param name="img">
        /// colour image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="width">
        /// image width <see cref="System.Int32"/>
        /// </param>
        /// <param name="height">
        /// image height <see cref="System.Int32"/>
        /// </param>
    	public byte[] Update(byte[] img, int width, int height) 
        {
    		this.width = width;
    		this.height = height;
            sourceImage = img;
            
            // adjust thresholds automatically
            if (automatic_thresholds) AutoThreshold(sourceImage, 2);
                
    		picsize = width * height;
    		initArrays();
    		readLuminance();
    		if (contrastNormalized) normalizeContrast();
    		computeGradients(gaussianKernelRadius, gaussianKernelWidth);
    		int low = (int)Math.Round(lowThreshold * MAGNITUDE_SCALE);
    		int high = (int)Math.Round( highThreshold * MAGNITUDE_SCALE);
    		performHysteresis(low, high, data);
    		thresholdEdges();
    		writeEdges(data);
            return(edgesImage);
    	}
        
        #endregion
             
        #region "private utility methods"
        
        private byte[] smooth(byte[] img, int radius)
        {
            int bytes_per_pixel = img.Length / (width * height);
            byte[] smoothed = new byte[img.Length];            
            
            for (int i = 0; i < radius; i++)
            {
                for (int y = 0; y < height-1; y++)
                {
                    for (int x = 0; x < width-1; x++)
                    {
                        int n = ((y * width) + x) * bytes_per_pixel;
                        int n1 = ((y * width) + x + 1) * bytes_per_pixel;
                        int n2 = (((y+1) * width) + x + 1) * bytes_per_pixel;
                        int n3 = (((y+1) * width) + x) * bytes_per_pixel;
                        
                        byte smoothed_value = (byte)((img[n] + img[n1] + img[n2] + img[n3]) / 4);
                        for (int col = 0; col < bytes_per_pixel; col++)
                            smoothed[n+col] = smoothed_value;
                    }
                }
                if (i < radius-1) 
                {
                    byte[] temp = img;
                    img = smoothed;
                    smoothed = temp;
                }
            }
            return(smoothed); 
        }
    	
    	private void initArrays() {
    		if (data == null || picsize != data.Length) {
    			data = new int[picsize];
    			magnitude = new int[picsize];
    			orientation = new float[picsize];

    			xConv = new float[picsize];
    			yConv = new float[picsize];
    			xGradient = new float[picsize];
    			yGradient = new float[picsize];
    		}
    	}

        #region "creating convolution masks"
        
        private int kwidth;
        private float[] kernel;
    	private float[] diffKernel;
        
        /// <summary>
        /// generate the gaussian convolution masks
        /// </summary>
        /// <param name="kernelRadius">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="kernelWidth">
        /// A <see cref="System.Int32"/>
        /// </param>
        private void createMasks(float kernelRadius, int kernelWidth)
        {
            bool create = false;
            if (kernel == null)
            {
                create = true;
            }
            else
            {
                if (kernel.Length != kernelWidth)
                    create = true;
            }

            if (create)
            {
                kernel = new float[kernelWidth];
    	        diffKernel = new float[kernelWidth];
                
        		for (kwidth = 0; kwidth < kernelWidth; kwidth++) 
                {
        			float g1 = gaussian(kwidth, kernelRadius);
        			if (g1 <= GAUSSIAN_CUT_OFF && kwidth >= 2) break;
        			float g2 = gaussian(kwidth - 0.5f, kernelRadius);
        			float g3 = gaussian(kwidth + 0.5f, kernelRadius);
        			kernel[kwidth] = (g1 + g2 + g3) / 3f / (2f * (float) Math.PI * kernelRadius * kernelRadius);
        			diffKernel[kwidth] = g3 - g2;
        		}
            }
        }
    	
        #endregion
        
        /// <summary>
        /// Compute gradients
        /// </summary>
        /// <remarks>
        /// NOTE: The elements of the method below (specifically the technique for
        /// non-maximal suppression and the technique for gradient computation)
        /// are derived from an implementation posted in the following forum (with the
        /// clear intent of others using the code):
        ///   http://forum.java.sun.com/thread.jspa?threadID=546211&start=45&tstart=0
        /// My code effectively mimics the algorithm exhibited above.
        /// Since I don't know the providence of the code that was posted it is a
        /// possibility (though I think a very remote one) that this code violates
        /// someone's intellectual property rights. If this concerns you feel free to
        /// contact me for an alternative, though less efficient, implementation.
        /// </remarks>
        /// <param name="kernelRadius">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="kernelWidth">
        /// A <see cref="System.Int32"/>
        /// </param>
    	private void computeGradients(float kernelRadius, int kernelWidth) 
        {
            // create gaussian convolution masks
            createMasks(kernelRadius, kernelWidth);
            
    		int initX = kwidth - 1;
    		int maxX = width - (kwidth - 1);
    		int initY = width * (kwidth - 1);
    		int maxY = width * (height - (kwidth - 1));
    		
    		//perform convolution in x and y directions
			for (int y = initY; y < maxY; y += width) 
            {
       		    for (int x = initX; x < maxX; x++) 
                {
    				int index = x + y;
    				float sumX = data[index] * kernel[0];
    				float sumY = sumX;
    				int xOffset = 1;
    				int yOffset = width;
                    int index1 = index - yOffset;
                    int index2 = index + yOffset;
                    int index3 = index - xOffset;
                    int index4 = index + xOffset;
                    float k;
    				while (xOffset < kwidth) 
                    {
                        k = kernel[xOffset];
    					sumY += k * (data[index1] + data[index2]);
    					sumX += k * (data[index3] + data[index4]);
    					xOffset++;
                        index1 -= width;
                        index2 += width;
                        index3--;
                        index4++;
    				}
    				
    				yConv[index] = sumY;
    				xConv[index] = sumX;
    			}
     
    		}
     
    		for (int x = initX; x < maxX; x++) 
            {
    			for (int y = initY; y < maxY; y += width) 
                {
    				float sum = 0f;
    				int index = x + y;
    				for (int i = 1; i < kwidth; i++)
    					sum += diffKernel[i] * (yConv[index - i] - yConv[index + i]);
     
    				xGradient[index] = sum;
    			}
     
    		}

    		for (int x = kwidth; x < width - kwidth; x++) 
            {
    			for (int y = initY; y < maxY; y += width) 
                {
    				float sum = 0.0f;
    				int index = x + y;
    				int yOffset = width;
    				for (int i = 1; i < kwidth; i++) 
                    {
    					sum += diffKernel[i] * (xConv[index - yOffset] - xConv[index + yOffset]);
    					yOffset += width;
    				}
     
    				yGradient[index] = sum;
    			}     
    		}
     
    		initX = kwidth;
    		maxX = width - kwidth;
    		initY = width * kwidth;
    		maxY = width * (height - kwidth);
            for (int y = initY; y < maxY; y += width)
            {
       		    for (int x = initX; x < maxX; x++)
                {
    				int index = x + y;
                    
    				int indexN = index - width;
    				int indexS = index + width;
    				int indexW = index - 1;
    				int indexE = index + 1;
    				int indexNW = indexN - 1;
    				int indexNE = indexN + 1;
    				int indexSW = indexS - 1;
    				int indexSE = indexS + 1;
                    
    				float xGrad = xGradient[index];
    				float yGrad = yGradient[index];

    				float gradMag = hypot2(xGrad, yGrad);

    				//perform non-maximal supression
    				float nMag = hypot2(xGradient[indexN], yGradient[indexN]);
    				float sMag = hypot2(xGradient[indexS], yGradient[indexS]);
    				float wMag = hypot2(xGradient[indexW], yGradient[indexW]);
    				float eMag = hypot2(xGradient[indexE], yGradient[indexE]);
    				float neMag = hypot2(xGradient[indexNE], yGradient[indexNE]);
    				float seMag = hypot2(xGradient[indexSE], yGradient[indexSE]);
    				float swMag = hypot2(xGradient[indexSW], yGradient[indexSW]);
    				float nwMag = hypot2(xGradient[indexNW], yGradient[indexNW]);

                    float tmp;
    				/*
    				 * An explanation of what's happening here, for those who want
    				 * to understand the source: This performs the "non-maximal
    				 * supression" phase of the Canny edge detection in which we
    				 * need to compare the gradient magnitude to that in the
    				 * direction of the gradient; only if the value is a local
    				 * maximum do we consider the point as an edge candidate.
    				 * 
    				 * We need to break the comparison into a number of different
    				 * cases depending on the gradient direction so that the
    				 * appropriate values can be used. To avoid computing the
    				 * gradient direction, we use two simple comparisons: first we
    				 * check that the partial derivatives have the same sign (1)
    				 * and then we check which is larger (2). As a consequence, we
    				 * have reduced the problem to one of four identical cases that
    				 * each test the central gradient magnitude against the values at
    				 * two points with 'identical support'; what this means is that
    				 * the geometry required to accurately interpolate the magnitude
    				 * of gradient function at those points has an identical
    				 * geometry (upto right-angled-rotation/reflection).
    				 * 
    				 * When comparing the central gradient to the two interpolated
    				 * values, we avoid performing any divisions by multiplying both
    				 * sides of each inequality by the greater of the two partial
    				 * derivatives. The common comparand is stored in a temporary
    				 * variable (3) and reused in the mirror case (4).
    				 * 
    				 */
    				if (xGrad * yGrad <= (float) 0 // (1)
    					? MathAbs(xGrad) >= MathAbs(yGrad) // (2)
    						? (tmp = MathAbs(xGrad * gradMag)) >= MathAbs(yGrad * neMag - (xGrad + yGrad) * eMag) // (3)
    							&& tmp > MathAbs(yGrad * swMag - (xGrad + yGrad) * wMag) // (4)
    						: (tmp = MathAbs(yGrad * gradMag)) >= MathAbs(xGrad * neMag - (yGrad + xGrad) * nMag) // (3)
    							&& tmp > MathAbs(xGrad * swMag - (yGrad + xGrad) * sMag) // (4)
    					: MathAbs(xGrad) >= MathAbs(yGrad) // (2)
    						? (tmp = MathAbs(xGrad * gradMag)) >= MathAbs(yGrad * seMag + (xGrad - yGrad) * eMag) // (3)
    							&& tmp > MathAbs(yGrad * nwMag + (xGrad - yGrad) * wMag) // (4)
    						: (tmp = MathAbs(yGrad * gradMag)) >= MathAbs(xGrad * seMag + (yGrad - xGrad) * sMag) // (3)
    							&& tmp > MathAbs(xGrad * nwMag + (yGrad - xGrad) * nMag) // (4)
    					) 
                    {
                        magnitude[index] = gradMag >= MAGNITUDE_LIMIT ? MAGNITUDE_MAX : (int) (MAGNITUDE_SCALE * gradMag);

                        // orientation of the edge
                        if (enable_edge_orientations)
                            orientation[index] = (float)Math.Atan2(yGrad, xGrad);
    				}
                    else 
                    {
    					magnitude[index] = 0;
                        orientation[index] = 0;
    				}
                    
    			}
    		}
    	}
     
        /// <summary>
        /// hypotenuse 
        /// </summary>
        /// <remarks>
        /// NOTE: Math.Abs(x) + Math.Abs(y) can be used as an alternative, but
        /// gives poorer results
        /// </remarks>
        /// <param name="x">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Single"/>
        /// </returns>        
        private float hypot2(float x, float y) 
        {
    		if (x == 0f) return (MathAbs(y));
    		if (y == 0f) return (MathAbs(x));
    		return (float)Math.Sqrt(x * x + y * y);
    	}

        private float hypot(float x, float y) 
        {
    		if (x == 0f) return y;
    		if (y == 0f) return x;
            if (x < 0) x = -x;
            if (y < 0) y = -y;
    		return (x + y);
    	}

        /// <summary>
        /// this method is faster than Math.Abs
        /// </summary>
        /// <param name="x">
        /// the value whose absolute we want <see cref="System.Single"/>
        /// </param>
        /// <returns>
        /// absolute value <see cref="System.Single"/>
        /// </returns>
        private float MathAbs(float x) 
        {
            if (x == 0f) return(0);
            if (x < 0) 
                return(-x);
            else
                return(x);
    	}

        /// <summary>
        /// gaussian function
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="sigma">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Single"/>
        /// </returns>
    	private float gaussian(float x, float sigma) 
        {
    		return (float)Math.Exp(-(x * x) / (2f * sigma * sigma));
    	}

        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// NOTE: this implementation reuses the data array to store both
        /// luminance data from the image, and edge intensity from the processing.
        /// This is done for memory efficiency, other implementations may wish
        /// to separate these functions.
        /// </remarks>
        /// <param name="low">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="high">
        /// A <see cref="System.Int32"/>
        /// </param>
    	private void performHysteresis(int low, int high, int[] followedEdges) 
        {
            // clear array containing followed edges
            for (int i = followedEdges.Length-1; i >= 0; i--) followedEdges[i] = 0;
     
    		int offset = 0;
            for (int y = 0; y < height; y++)                            		 
            {
    			for (int x = 0; x < width; x++) 
                {
    				if ((followedEdges[offset] == 0) && (magnitude[offset] >= high)) 
    					follow(x, y, offset, low, followedEdges);
    				offset++;
    			}
    		}
     	}

    	private void follow(int x1, int y1, int i1, 
                            int threshold, 
                            int[] followedEdges) 
        {
    		int x0 = x1 == 0 ? x1 : x1 - 1;
    		int x2 = x1 == width - 1 ? x1 : x1 + 1;
    		int y0 = y1 == 0 ? y1 : y1 - 1;
    		int y2 = y1 == height -1 ? y1 : y1 + 1;
    		
            // mark this location as having been followed
    		followedEdges[i1] = magnitude[i1];
            
            // look for other pixels in the neighbourhood to follow
            for (int y = y0; y <= y2; y++)
            {
                int i2 = y * width;
    			for (int x = x0; x <= x2; x++) 
                {    				
    				if (!((y == y1) && (x == x1)))        // not the current pixel
                    {
                        int i3 = i2 + x;
                        if ((followedEdges[i3] == 0) &&   // hasn't been followed
    					    (magnitude[i3] >= threshold)) // with sufficient magnitude
                        {
    					    follow(x, y, i3, threshold, followedEdges);
    					    return;
    				    }
                    }
    			}
    		}
            
    	}

    	private void thresholdEdges() 
        {
    		for (int i = data.Length-1; i >= 0; i--) 
            {
                if (data[i] != 0) data[i] = 0; else data[i] = 255; 
    		}
    	}
    	
    	private int luminance(int r, int g, int b) 
        {
    		return (((299 * r) + (587 * g) + (114 * b)) / 1000);
    	}

        private int luminance2(float r, float g, float b) 
        {
    		return (int)Math.Round(0.299f * r + 0.587f * g + 0.114f * b);
    	}
    	
    	private void readLuminance() 
        {
            byte[] pixels = sourceImage;
            int offset = pixels.Length-1;
            if (pixels.Length == width * height * 3)
            {
                int n = (width * height) - 1;
                for (int i = pixels.Length-2; i >= 0; i -= 3) 
                {
                    int r = pixels[offset--] & 0xff;
                    int g = pixels[offset--] & 0xff;
                    int b = pixels[offset--] & 0xff;
                    data[n--] = luminance(r, g, b);
                }
            }
            else
            {
                for (int i = pixels.Length-1; i >= 0; i--) 
                    data[i] = pixels[offset--] & 0xff;
            }
    	}
     
    	private void normalizeContrast() 
        {
    		int[] histogram = new int[256];
    		for (int i = 0; i < data.Length; i++) {
    			histogram[data[i]]++;
    		}
    		int[] remap = new int[256];
    		int sum = 0;
    		int j = 0;
    		for (int i = 0; i < histogram.Length; i++) {
    			sum += histogram[i];
    			int target = sum*255/picsize;
    			for (int k = j+1; k <=target; k++) {
    				remap[k] = i;
    			}
    			j = target;
    		}
    		
    		for (int i = 0; i < data.Length; i++) {
    			data[i] = remap[data[i]];
    		}
    	}
    	

        /// <summary>
        /// update the edges image 
        /// </summary>
        /// <remarks>
        /// NOTE: There is currently no mechanism for obtaining the edge data
        /// in any other format other than an INT_ARGB type BufferedImage.
        /// This may be easily remedied by providing alternative accessors.
        /// </remarks>
        /// <param name="pixels">
        /// A <see cref="System.Int32"/>
        /// </param>
    	private void writeEdges(int[] pixels) 
        {
    		if (edgesImage == null) {
    			edgesImage = new byte[width * height * 3];
                edges = new List<int>();
    		}
            edges.Clear();
            
            int n = 0;
            int x=0,y=0;
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)pixels[i];
                
                if (v == 0)
                {
                    edges.Add(x);
                    edges.Add(y);
                }
                
                edgesImage[n++] = v;
                edgesImage[n++] = v;
                edgesImage[n++] = v;
                              
                x++;
                if (x >= width)
                {
                    x = 0;
                    y++;
                }
            }
    	}
        
        #endregion
        
        #region "display"
        
        /// <summary>
        /// displays the detected magnitudes
        /// </summary>
        /// <param name="img">
        /// image data <see cref="System.Byte"/>
        /// </param>
        /// <param name="img_width">
        /// image width <see cref="System.Int32"/>
        /// </param>
        /// <param name="img_height">
        /// image_height <see cref="System.Int32"/>
        /// </param>
        public void ShowMagnitudes(byte[] img, int img_width, int img_height)
        {
            for (int y = 0; y < img_height; y++)
            {
                int yy = y * (height-1) / img_height;
                for (int x = 0; x < img_width; x++)
                {
                    int xx = x * (width-1) / img_width;
                    int mag = magnitude[(yy*width)+xx];
                    int mag2 = mag * 255 / (MAGNITUDE_MAX/4);
                    if (mag2 > 255) mag2 = 255;
                    byte v = (byte)(255 - mag2);

                    // test
                    //byte v = (byte)data[(yy*width)+xx];
                    //if ((v == 255) && (magnitude[(yy*width)+xx] < 0)) v = (byte)0;
                    
                    int n = ((y * img_width) + x)*3;
                    img[n] = v;
                    img[n+1] = v;
                    img[n+2] = v;
                    
                    if (v != 255)
                    {
                        if (mag >= highThreshold)
                        {
                            img[n] = 0;
                            img[n+1] = v;
                            img[n+2] = 0;
                        }
                        if (mag < lowThreshold)
                        {
                            img[n] = 0;
                            img[n+1] = 0;
                            img[n+2] = v;
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region "detecting corner points"
        
        public List<int> GetCorners()
        {
            List<int> corners = new List<int>();

            int bytes_per_pixel = edgesImage.Length / (width*height);
            int stride = (width-3) * bytes_per_pixel;
            
            for (int i = 0; i < edges.Count; i += 2)
            {
                int x = edges[i];
                int y = edges[i+1];
                if ((x > 0) && (x < width-1) &&
                    (y > 0) && (y < height-1))
                {
                    int adjacent_edges = 0;
                    int n = (((y-1) * width) + x-1)*bytes_per_pixel;
                    for (int yy = y-1; yy <= y+1; yy++)
                    {
                        for (int xx = x-1; xx <= x+1; xx++)
                        {
                            if (edgesImage[n] == 0)
                            {
                                adjacent_edges++;
                                if (adjacent_edges > 2)
                                {
                                    xx = x+2;
                                    yy = y+2;
                                }
                            }
                            n += 3;
                        }
                        n += stride;
                    }
                    
                    if (adjacent_edges <= 2)
                    {
                        corners.Add(x);
                        corners.Add(y);
                    }
                }
            }
            return(corners);
        }
        
        public void ConnectBrokenEdges(int maximum_separation)
        {
            List<int> corners = GetCorners();
            bool[] connected = new bool[corners.Count];            
            
            for (int i = 0; i < corners.Count-2; i += 2)
            {
                int x0 = corners[i];
                int y0 = corners[i+1];
                for (int j = i + 2; j < corners.Count; j += 2)
                {
                    if (!connected[j])
                    {
                        int x1 = corners[j];
                        int dx = x1 - x0;
                        if (dx < 0) dx = -dx;
                        if (dx <= maximum_separation)
                        {
                            int y1 = corners[j+1];
                            int dy = y1 - y0;
                            if (dy < 0) dy = -dy;
                            if (dy <= maximum_separation)
                            {
                                int dist = (int)Math.Sqrt((dx*dx)+(dy*dy));
                                for (int d = 1; d < dist; d++)
                                {
                                    int ix = x0 + (d * dx / dist);
                                    int iy = y0 + (d * dy / dist);
                                    edges.Add(ix);
                                    edges.Add(iy);
                                }
                                
                                connected[j] = true;
                                j = corners.Count;
                            }
                        }
                    }
                }
            }
        }
        
        #endregion
     
    }
    
}
