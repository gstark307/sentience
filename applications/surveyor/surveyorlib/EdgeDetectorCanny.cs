/**
 * This software has been released into the public domain.
 * Please read the notes in this source file for additional information.
 * 
 * This class provides a configurable implementation of the Canny edge
 * detection algorithm. This classic algorithm has a number of shortcomings,
 * but remains an effective tool in many scenarios. This class is designed
 * for single threaded use only.
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

namespace sluggish.utilities
{
    public sealed class EdgeDetectorCanny : EdgeDetector
    {
        // statics	
        private static float GAUSSIAN_CUT_OFF = 0.005f;
        private static float MAGNITUDE_SCALE = 100f;
        private static float MAGNITUDE_LIMIT = 1000f;
        private static int MAGNITUDE_MAX = (int)(MAGNITUDE_SCALE * MAGNITUDE_LIMIT);
        private static float MAGNITUDE_LIMIT_SQR = MAGNITUDE_LIMIT * MAGNITUDE_LIMIT;

        // fields	
        private int height;
        private int width;
        private int picsize;
        private int[] data;
        private int[] magnitude;
        private byte[] sourceImage;        

        private float gaussianKernelRadius;
        private float lowThreshold;
        private float highThreshold;
        private int gaussianKernelWidth;

        private float[] xConv;
        private float[] yConv;
        private float[] xGradient;
        private float[] yGradient;
        private int[] edge_pixel_index;
        private float[] edge_magnitude;

        #region "constructors"

        /// <summary>
        /// Constructs a new detector with default parameters. 
        /// </summary>
        public EdgeDetectorCanny()
        {
            lowThreshold = 2.5f;
            highThreshold = 7.5f;
            gaussianKernelRadius = 2f;
            gaussianKernelWidth = 16;
            automatic_thresholds = true;
        }

        /// <summary>
        /// initialise arrays
        /// </summary>
        private void initArrays()
        {
            if (data == null || picsize != data.Length)
            {
                data = new int[picsize];
                magnitude = new int[picsize];

                xConv = new float[picsize];
                yConv = new float[picsize];
                xGradient = new float[picsize];
                yGradient = new float[picsize];

                edge_pixel_index = new int[picsize];
                edge_magnitude = new float[picsize];
            }
        }

        #endregion

        #region "convert RGB to luminence image"

        /// <summary>
        /// RGB to luminence conversion
        /// </summary>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <returns>luninence value</returns>
        private int luminance(int r, int g, int b)
        {
            return (((299 * r) + (587 * g) + (114 * b)) / 1000);
        }

        /// <summary>
        /// gets luminance data from the raw image
        /// </summary>
        private void readLuminance()
        {
            byte[] pixels = sourceImage;
            int offset = pixels.Length - 1;
            if (pixels.Length == width * height * 3)
            {
                int n = (width * height) - 1;
                for (int i = pixels.Length - 2; i >= 0; i -= 3)
                {
                    int r = pixels[offset--] & 0xff;
                    int g = pixels[offset--] & 0xff;
                    int b = pixels[offset--] & 0xff;
                    data[n--] = luminance(r, g, b);
                }
            }
            else
            {
                for (int i = pixels.Length - 1; i >= 0; i--)
                    data[i] = pixels[offset--] & 0xff;
            }
        }

        #endregion

        #region "Automatically find appropriate high and low thresholds"

        /// <summary>
        /// calculate high and low thresholds
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="MeanDark">returned mean dark value</param>
        /// <param name="MeanLight">returned mean light value</param>
        public static unsafe void GetThresholds(float[] histogram,
                                                ref float MeanDark, ref float MeanLight)
        {
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
                for (int grey_level = max_grey_levels - 1; grey_level >= 0; grey_level--)
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
                    for (h = histlength - 1; h >= 0; h--)
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
        }


        /// <summary>
        /// automatically calculate the high and low canny thresholds
        /// based upon the mean light and dark values from a 
        /// histogram taken from the centre region of the image
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="sampling_step_size">step sized used to sample the image in pixels</param>
        private void AutoThreshold(byte[] img,
                                   int sampling_step_size)
        {
            int bytes_per_pixel = img.Length / (width * height);
            int tx = width / 3;
            int ty = height / 3;
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
                        histogram[img[n + 2]]++;
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
            GetThresholds(histogram, ref mean_dark, ref mean_light);
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

        #region "main update"

        /// <summary>
        /// processes an image to find edges
        /// </summary>
        /// <param name="img">colour image data</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        public override byte[] Update(byte[] img, int width, int height)
        {
            this.width = width;
            this.height = height;
            sourceImage = img;

            // adjust thresholds automatically
            if (automatic_thresholds) AutoThreshold(sourceImage, 2);

            picsize = width * height;
            initArrays();
            readLuminance();
            int no_of_edges = computeGradients(gaussianKernelRadius, gaussianKernelWidth);
                        
            int low = (int)Math.Round(lowThreshold * MAGNITUDE_SCALE);
            int high = (int)Math.Round(highThreshold * MAGNITUDE_SCALE);

            performHysteresis(low, high, data, no_of_edges);
            thresholdEdges();
            writeEdges(no_of_edges);
            return (edgesImage);
        }

        #endregion

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
                    kernel[kwidth] = (g1 + g2 + g3) / 3f / (2f * (float)Math.PI * kernelRadius * kernelRadius);
                    diffKernel[kwidth] = g3 - g2;
                }
            }
        }

        #endregion

        #region "gaussian convolution and non-maximal suppression"

        /// <summary>
        /// Compute gradients
        /// </summary>
        /// <param name="kernelRadius"></param>
        /// <param name="kernelWidth"><see cref="System.Int32"/></param>
        private unsafe int computeGradients(float kernelRadius, int kernelWidth)
        {
            // create gaussian convolution masks
            createMasks(kernelRadius, kernelWidth);
            
            int no_of_edges = 0;

            int initX = kwidth - 1;
            int maxX = width - (kwidth - 1);
            int initY = width * (kwidth - 1);
            int maxY = width * (height - (kwidth - 1));

            // perform convolution in x and y directions
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
                    for (xOffset = 1; xOffset < kwidth; xOffset++, index3--, index4++)
                    {
                        k = kernel[xOffset];
                        sumY += k * (data[index1] + data[index2]);
                        sumX += k * (data[index3] + data[index4]);
                        index1 -= width;
                        index2 += width;
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

                    float gradMag = SqrMag(xGrad, yGrad);

                    /*
                    if (xGrad >= 0)
                        xGrad = xGrad * xGrad;
                    else
                        xGrad = -(xGrad * xGrad);

                    if (yGrad >= 0)
                        yGrad = yGrad * yGrad;
                    else
                        yGrad = -(yGrad * yGrad);
                     */

                    //perform non-maximal supression
                    float nMag = SqrMag(xGradient[indexN], yGradient[indexN]);
                    float sMag = SqrMag(xGradient[indexS], yGradient[indexS]);
                    float wMag = SqrMag(xGradient[indexW], yGradient[indexW]);
                    float eMag = SqrMag(xGradient[indexE], yGradient[indexE]);
                    float neMag = SqrMag(xGradient[indexNE], yGradient[indexNE]);
                    float seMag = SqrMag(xGradient[indexSE], yGradient[indexSE]);
                    float swMag = SqrMag(xGradient[indexSW], yGradient[indexSW]);
                    float nwMag = SqrMag(xGradient[indexNW], yGradient[indexNW]);

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
                                             
                    float xGrad_abs = xGrad;
                    if (xGrad_abs < 0) xGrad_abs = -xGrad_abs;

                    float yGrad_abs = yGrad;
                    if (yGrad_abs < 0) yGrad_abs = -yGrad_abs;

                    bool is_edge = false;
                    if (xGrad * yGrad <= 0)
                    {
                        float sumGrad = xGrad + yGrad;
                        if (xGrad_abs >= yGrad_abs)
                        {
                            if ((tmp = xGrad_abs * gradMag) >= MathAbs((yGrad * neMag) - (sumGrad * eMag)) &&
                                (tmp > MathAbs((yGrad * swMag) - (sumGrad * wMag))))
                                is_edge = true;
                        }
                        else
                        {
                            if ((tmp = MathAbs(yGrad * gradMag)) >= MathAbs((xGrad * neMag) - ((yGrad + xGrad) * nMag)) &&
                                (tmp > MathAbs((xGrad * swMag) - ((yGrad + xGrad) * sMag))))
                                is_edge = true;
                        }
                    }
                    else
                    {
                        if (xGrad_abs >= yGrad_abs)
                        {
                            if ((tmp = xGrad_abs * gradMag) >= MathAbs((yGrad * seMag) + ((xGrad - yGrad) * eMag)) &&
                                (tmp > MathAbs((yGrad * nwMag) + ((xGrad - yGrad) * wMag))))
                                is_edge = true;
                        }
                        else
                        {
                            if ((tmp = yGrad_abs * gradMag) >= MathAbs((xGrad * seMag) + ((yGrad - xGrad) * sMag)) &&
                                (tmp > MathAbs((xGrad * nwMag) + ((yGrad - xGrad) * nMag))))
                                is_edge = true;
                        }
                    }

                    if (is_edge)                        
                    {
                        if (gradMag >= MAGNITUDE_LIMIT_SQR)
                            edge_magnitude[index] = -1;
                        else
                            edge_magnitude[index] = gradMag;
                                                    
                        edge_pixel_index[no_of_edges++] = index;
                    }
                }
            }
                        
            // process the detected edges 
            for (int i = no_of_edges - 1; i >= 0; i--)
            {
                int index = edge_pixel_index[i];
                float gradMag = edge_magnitude[index];
                if (gradMag < 0)
                    magnitude[index] = MAGNITUDE_MAX;
                else
                    magnitude[index] = (int)(MAGNITUDE_SCALE * Math.Sqrt(gradMag));
            }

            return(no_of_edges);
        }

        /// <summary>
        /// squared magnitude 
        /// </summary>
        /// <remarks>
        /// NOTE: Math.Abs(x) + Math.Abs(y) can be used as an alternative, but
        /// gives poorer results
        /// </remarks>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>
        /// </returns>        
        private float SqrMag(float x, float y)
        {
            return (x * x + y * y);
        }

        /// <summary>
        /// this method is faster than Math.Abs
        /// </summary>
        /// <param name="x">the value whose absolute we want</param>
        /// <returns>absolute value</returns>
        private float MathAbs(float x)
        {
            if (x == 0f) return (0);
            if (x < 0)
                return (-x);
            else
                return (x);
        }


        /// <summary>
        /// gaussian function
        /// </summary>
        /// <param name="x"></param>
        /// <param name="sigma"></param>
        /// <returns></returns>
        private float gaussian(float x, float sigma)
        {
            return (float)Math.Exp(-(x * x) / (2f * sigma * sigma));
        }

        #endregion

        #region "hysteresis"

        /// <summary>
        /// follow the yellow brick road
        /// </summary>
        /// <param name="low">low threshold</param>
        /// <param name="high">high threshold</param>
        /// <param name="followedEdges">indicates which edges have been followed</param>
        /// <param name="no_of_edges">number of detected edges</param>
        private unsafe void performHysteresis(int low, int high, int[] followedEdges, int no_of_edges)
        {
            // clear array containing followed edges
            for (int i = followedEdges.Length - 1; i >= 0; i--) followedEdges[i] = 0;

            fixed (int* unsafe_followedEdges = followedEdges)
            {
                fixed (int* unsafe_magnitude = magnitude)
                {
                    for (int i = no_of_edges - 1; i >= 0; i--)
                    {
                	    int index = edge_pixel_index[i];
                	    
                	    if (unsafe_followedEdges[index] == 0) 
                	    {
                	        if (unsafe_magnitude[index] >= high)
                    		{
                    			int y = index % width;
                    			int x = index - (y * width);
                    			follow(x, y, index, ref low, unsafe_followedEdges, unsafe_magnitude);
                    		}
                		}
                    }
                }
            }
        }

        /// <summary>
        /// recursive edge following
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="i1"></param>
        /// <param name="threshold"></param>
        /// <param name="followedEdges">indicates which edges have been followed</param>
        private unsafe void follow(int x1, int y1, int i1,
                                   ref int threshold,
                                   int* followedEdges, int* magnitude)
        {
            int x0 = x1 == 0 ? x1 : x1 - 1;
            int x2 = x1 == width - 1 ? x1 : x1 + 1;
            int y0 = y1 == 0 ? y1 : y1 - 1;
            int y2 = y1 == height - 1 ? y1 : y1 + 1;

            // mark this location as having been followed
            followedEdges[i1] = magnitude[i1];

            // look for other pixels in the neighbourhood to follow
            for (int y = y0; y <= y2; y++)
            {
                int i2 = y * width;
                int i3 = i2 + x0;
                for (int x = x0; x <= x2; x++, i3++)
                {                    
                    if (followedEdges[i3] == 0)   // hasn't been followed
                    {
                        if (magnitude[i3] >= threshold) // with sufficient magnitude
                        {
                            follow(x, y, i3, ref threshold, followedEdges, magnitude);
                            return;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// binarise the edges to 0 or 255 values
        /// </summary>
        private void thresholdEdges()
        {
            for (int i = data.Length - 1; i >= 0; i--)
                if (data[i] != 0) data[i] = 0; else data[i] = 255;
        }

        #endregion

        #region "creating an edges image for later display"

        /// <summary>
        /// update the edges image 
        /// </summary>
        /// <remarks>
        /// NOTE: There is currently no mechanism for obtaining the edge data
        /// in any other format other than an INT_ARGB type BufferedImage.
        /// This may be easily remedied by providing alternative accessors.
        /// </remarks>
        /// <param name="no_of_edges">number of edges detected</param>
        private void writeEdges(int no_of_edges)
        {
            if (edgesImage == null)
            {
                edgesImage = new byte[width * height * 3];
                edges = new List<int>();
            }
            else
            {
                // has the size changed ?
                if (edgesImage.Length != width * height * 3)
                    edgesImage = new byte[width * height * 3];

                // clear the image
                for (int i = edgesImage.Length - 1; i >= 0; i--)
                    edgesImage[i] = 255;
            }

            edges.Clear();

            for (int i = no_of_edges - 1; i >= 0; i--)
            {
                int index = edge_pixel_index[i];
                int y = index / width;
                int x = index - (y * width);
                edges.Add(x);
                edges.Add(y);

                index *= 3;
                edgesImage[index++] = 0;
                edgesImage[index++] = 0;
                edgesImage[index++] = 0;
            }
        }

        #endregion
    }

}
