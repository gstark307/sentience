/*
    edge detection using a haar filter
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;

namespace sluggish.utilities
{
    public class EdgeDetectorHaar : EdgeDetector
    {
        public int[] magnitude;
    
        #region "scaling down images"

        /// <summary>
        /// Downsamples the give image by summing the values of groups
        /// if four pixels.  The returned image is half the dimension
        /// of the one given.  This is used in order to apply the Haar 
        /// filter at different spatial scales.
        /// Note that the image is given as an array of integers rather
        /// than bytes, so that overflows do not occur.
        /// </summary>
        /// <param name="img">image to be downsampled</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="buffer">buffer into which to inser the downsampled image data</param>
        /// <returns>downsampled image, half the original size</returns>
        private int[] PyrDown(int[] img, int width, int height, int[] buffer)
        {
            int[] result = buffer;

            int n2 = 0;
            for (int y = 0; y < height - 1; y += 2)
            {
                int n = y * width;
                for (int x = 0; x < width - 1; x += 2, n += 2)
                {
                    int sum = img[n] + img[n + 1] +
                              img[n + width] + img[n + width + 1];
                    result[n2++] = sum;
                }
            }
            return (result);
        }

        #endregion

        #region "hysteresis"

        private const int MAX_RECURSION_DEPTH = 6000;

        /// <summary>
        /// follow the yellow brick road
        /// </summary>
        /// <param name="low">low threshold</param>
        /// <param name="high">high threshold</param>
        /// <param name="magnitude">array containing the haar filter resonses</param>
        /// <param name="followedEdges">indicates which edges have been followed</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        private unsafe void Hysteresis(int low, int high,
                                       int[] magnitude,
                                       int[] followedEdges,
                                       int width, int height)
        {
            // clear array containing followed edges
            for (int i = followedEdges.Length - 1; i >= 0; i--) followedEdges[i] = 0;

            fixed (int* unsafe_followedEdges = followedEdges)
            {
                fixed (int* unsafe_magnitude = magnitude)
                {
                    int y = height - 1;
                    int w = width - 1;
                    int x = w;
                    int depth = 0;
                    for (int i = (width * height) - 1; i >= 0; i--)
                    {
                        if ((unsafe_followedEdges[i] == 0) && (magnitude[i] >= high))
                            follow(x, y, i, low, unsafe_followedEdges, unsafe_magnitude, width, height, depth);

                        x--;
                        if (x < 0)
                        {
                            x = w;
                            y--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// recursive edge following
        /// </summary>
        /// <param name="x1">x coordinate to begin at</param>
        /// <param name="y1">y coordinate to begin at</param>
        /// <param name="i1">pixel index within the image</param>
        /// <param name="threshold">low threshold</param>
        /// <param name="followedEdges">indicates which edges (pixel indexes) have been followed</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        private unsafe void follow(int x1, int y1, int i1,
                                   int threshold,
                                   int* followedEdges,
                                   int* magnitude,
                                   int width, int height,
                                   int depth)
        {
            if (depth < MAX_RECURSION_DEPTH)
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
                    for (int x = x0; x <= x2; x++)
                    {
                        int i3 = i2 + x;
                        if ((followedEdges[i3] == 0) &&   // hasn't been followed
                            (magnitude[i3] >= threshold)) // with sufficient magnitude
                        {
                            follow(x, y, i3, threshold, followedEdges, magnitude, width, height, depth + 1);
                            return;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// binarise the edges to 0 or 255 values
        /// </summary>
        /// <param name="magnitude">response magnitudes</param>
        private void thresholdEdges(int[] magnitude)
        {
            for (int i = magnitude.Length - 1; i >= 0; i--)
            {
                if (magnitude[i] != 0)
                    magnitude[i] = 0;
                else
                    magnitude[i] = 255;
            }
        }

        #endregion

        #region "automatic thresholds"

        /// <summary>
        /// calculate a global threshold based upon an intensity histogram
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="MeanDark">returned mean dark value</param>
        /// <param name="MeanLight">returned mean light value</param>
        /// <returns>global threshold value</returns>
        public unsafe void GetMeanHighLow(int[] histogram,
                                                 ref int MeanLow, ref int MeanHigh)
        {
            int Tmin = 0;
            int Tmax = 0;
            int MinVariance = int.MaxValue;  // some large figure
            int currMeanLow, currMeanHigh, VarianceLow, VarianceHigh;

            int LowHits = 0;
            int HighHits = 0;
            int BestLowHits = 0;
            int BestHighHits = 0;
            int histlength = histogram.Length - 1;
            MeanLow = 0;
            MeanHigh = 0;

            int max_levels = histlength;

            int h, bucket;
            int magnitude, Variance;

            fixed (int* unsafe_histogram = histogram)
            {
                // evaluate all possible thresholds
                for (int level = max_levels - 1; level >= 0; level--)
                {
                    // compute mean and variance for this threshold
                    // in a struggle between light and darkness
                    LowHits = 0;
                    HighHits = 0;
                    currMeanLow = 0;
                    currMeanHigh = 0;
                    VarianceLow = 0;
                    VarianceHigh = 0;

                    bucket = level * histlength / max_levels;
                    for (h = histlength - 1; h >= 0; h--)
                    {
                        magnitude = unsafe_histogram[h];
                        if (h < bucket)
                        {
                            currMeanLow += h * magnitude;
                            VarianceLow += (bucket - h) * magnitude;
                            LowHits += magnitude;
                        }
                        else
                        {
                            currMeanHigh += h * magnitude;
                            VarianceHigh += (bucket - h) * magnitude;
                            HighHits += magnitude;
                        }
                    }

                    // compute means
                    if (LowHits > 0)
                    {
                        // rescale into 0-255 range
                        currMeanLow = currMeanLow / LowHits;
                        VarianceLow = VarianceLow / LowHits;
                    }
                    if (HighHits > 0)
                    {
                        // rescale into 0-255 range
                        currMeanHigh = currMeanHigh / HighHits;
                        VarianceHigh = VarianceHigh / HighHits;
                    }

                    Variance = VarianceLow + VarianceHigh;
                    if (Variance < 0) Variance = -Variance;

                    if (Variance < MinVariance)
                    {
                        MinVariance = Variance;
                        Tmin = level;
                        MeanLow = currMeanLow;
                        MeanHigh = currMeanHigh;
                        BestLowHits = LowHits;
                        BestHighHits = HighHits;
                    }
                    if (Variance == MinVariance)
                    {
                        Tmax = level;
                        MeanHigh = currMeanHigh;
                        BestHighHits = HighHits;
                    }
                }
            }
        }

        private void AutoThreshold(int[] magnitude,
                                   int max_magnitude,
                                   ref int low_threshold,
                                   ref int high_threshold,
                                   int[] histogram)
        {
            int i, bucket;
            int histogram_levels = histogram.Length - 1;

            // clear the histogram
            for (i = histogram_levels; i >= 0; i--) histogram[i] = 0;

            // update the histogram
            for (i = magnitude.Length - 1; i >= 0; i--)
            {
                if (magnitude[i] != 0)
                {
                    int mag = magnitude[i];
                    bucket = mag * histogram_levels / max_magnitude;
                    if (bucket > 0)
                        histogram[bucket]++;
                }
            }

            // get the mean high and low values
            GetMeanHighLow(histogram, ref low_threshold, ref high_threshold);
        }

        private void AutoThreshold(byte[] image,
                                   ref int low_threshold,
                                   ref int high_threshold,
                                   int[] histogram)
        {
            int i;
            int histogram_levels = histogram.Length - 1;

            // clear the histogram
            for (i = histogram_levels; i >= 0; i--) histogram[i] = 0;

            // update the histogram
            for (i = image.Length - 1; i >= 0; i--)
                histogram[image[i]]++;

            // get the mean high and low values
            GetMeanHighLow(histogram, ref low_threshold, ref high_threshold);
        }


        #endregion

        #region "filtering"

        /// <summary>
        /// update edge responses using a Haar filter
        /// </summary>
        /// <param name="image">image to be filtered (1 byte per pixel)</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="scale">spatial scale at which to apply the filter, typically in the range 0-3</param>
        /// <param name="max_magnitude">returned maximum magnitude of the response</param>
        /// <param name="buffers">temporary buffers</param>
        /// <returns>magnitude of responses</returns>
        public int[] HaarFilter(byte[] image,
                                int width, int height,
                                int scale,
                                ref int max_magnitude,
                                int[][] buffers)
        {
            int[] pi1 = { 1, -1, 1, -1 };
            int[] pi2 = { 1, 1, -1, -1 };

            int i;
            int[] image2 = buffers[0];

            // convert the original image as an array of bytes into integers
            for (i = (width * height) - 1; i >= 0; i--) image2[i] = image[i];

            // downsample the image to the required scale, summing pixel values
            int index = 1;
            for (int sc = 0; sc < scale; sc++)
            {
                image2 = PyrDown(image2, width, height, buffers[index]);
                width /= 2;
                height /= 2;
                index = 1 - index;
            }

            // create modulus image
            int[] magnitude = buffers[2];
            max_magnitude = 0;
            int tmp1, tmp2;
            for (i = (width * height) - 2 - width; i >= 0; i--)
            {
                tmp1 = image2[i] * pi1[0] + image2[i + width] * pi1[1] +
                       image2[i + 1] * pi1[2] + image2[i + 1 + width] * pi1[3];
                tmp1 *= tmp1;

                tmp2 = image2[i] * pi2[0] + image2[i + width] * pi2[1] +
                       image2[i + 1] * pi2[2] + image2[i + 1 + width] * pi2[3];
                tmp2 *= tmp2;

                magnitude[i] = tmp1 + tmp2;
                if (magnitude[i] > max_magnitude) max_magnitude = magnitude[i];
            }

            return (magnitude);
        }

        #endregion

        #region "main update"

        /// <summary>
        /// Detect Edges
        /// </summary>
        /// <param name="image">image data (1 byte per pixel)</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="scale">spatial scale at which to apply the filter, typically in the range 0-3</param>
        /// <param name="buffers">temporary buffers</param>
        public void Update(byte[] image,
                           int width, int height,
                           int scale,
                           ref int[][] buffers)
        {
            const int HISTOGRAM_LEVELS = 1000;

            if (edgesImage == null)
                edgesImage = new byte[width * height * 3];

            if (image.Length == width * height)  // sanity check
            {
                // get the dimensions of the scaled down image
                int scaled_width = width;
                int scaled_height = height;
                for (int i = 0; i < scale; i++)
                {
                    scaled_width /= 2;
                    scaled_height /= 2;
                }
                
                if (buffers == null)
                {
                    // create some buffers
                    // buffers consist of:
                    //    2 x full size images
                    //    1 x scaled down image
                    //    1 x 256 level histogram
                    buffers = new int[4][];
                    for (int b = 0; b < buffers.Length; b++)
                    {
                        if (b < 2)
                            buffers[b] = new int[width * height]; // full size images
                        else
                        {
                            if (b == 2)
                                buffers[b] = new int[scaled_width * scaled_height]; // scaled down image
                            else
                                buffers[b] = new int[HISTOGRAM_LEVELS];  // used for histogram
                        }
                    }
                }

                int max_magnitude = 0;
                int[] mag = HaarFilter(image, width, height, scale,
                                       ref max_magnitude, buffers);

                // high and low thresholds
                int low_threshold = max_magnitude * 2 / 100;
                int high_threshold = max_magnitude * 6 / 100;
                int[] followedEdges = buffers[0];

                max_magnitude = (255 * 2 * (scale + 1)) * (255 * 2 * (scale + 1)) * 4;

                AutoThreshold(mag, max_magnitude,
                              ref low_threshold, ref high_threshold,
                              buffers[3]);
                low_threshold = low_threshold * max_magnitude / (HISTOGRAM_LEVELS * 40 / 100);
                high_threshold = high_threshold * max_magnitude / (HISTOGRAM_LEVELS * 120 / 100);

                // perform hysteresis
                Hysteresis(low_threshold, high_threshold,
                           mag, followedEdges,
                           scaled_width, scaled_height);

                thresholdEdges(followedEdges);

                if (edges == null) edges = new List<int>();
                edges.Clear();
                if (edgesImage != null)
                {
                    // update the edges image
                    for (int i = edgesImage.Length - 1; i >= 0; i--) edgesImage[i] = 255;

                    int n = 0;
                    for (int y = 0; y < scaled_height; y++)
                    {
                        for (int x = 0; x < scaled_width; x++)
                        {
                            if (followedEdges[n++] == 0)
                            {
                                int xx = x * (width - 1) / scaled_width;
                                int yy = y * (height - 1) / scaled_height;
                                int n2 = ((yy * width) + xx) * 3;
                                edgesImage[n2] = 0;

                                edges.Add(xx);
                                edges.Add(yy);
                            }
                        }
                    }
                }

                magnitude = new int[width*height];
                int nn = 0;
                for (int y = 0; y < height; y++)
                {
                    int yy = y * scaled_height / height;
                    for (int x = 0; x < width; x++)
                    {
                        int xx = x * scaled_width / width;
                        magnitude[nn] = mag[(yy * scaled_height) + xx];
                        nn++;
                    }
                }
            }
            else
            {
                Console.WriteLine("You must supply a mono image");
            }
        }

        /// <summary>
        /// Detect Edges
        /// </summary>
        /// <param name="image">image data (1 byte per pixel)</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="scale">spatial scale at which to apply the filter, typically in the range 0-3</param>
        public void Update(byte[] image,
                           int width, int height,
                           int scale)
        {
            int[][] buffers = null;
            Update(image, width, height, scale,
                   ref buffers);
        }

        /// <summary>
        /// Detect Edges
        /// </summary>
        /// <param name="image">image data (1 byte per pixel)</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        public override byte[] Update(byte[] image, int width, int height)
        {
            int[][] buffers = null;
            int scale = 1;
            Update(image, width, height, scale, ref buffers);
            return (edgesImage);
        }


        #endregion
    }

}