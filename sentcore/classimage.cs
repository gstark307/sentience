/*
    Sentience 3D Perception System
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

#define VISUALISATION

using System;
using sluggish.utilities;


namespace sentience.core
{
    public sealed class classimage
    {
        //dimensions of the image
        public int width, height;

        public Byte[] image;
        private int[] Integral;

#if VISUALISATION
        public float[,] blobs;
#endif

        // variables used by the blob detector
        private int tx1, ty1, bx1, by1;
        private int tx2, ty2, bx2, by2;
        private int outer, inner;
        private float outer_pixels, inner_pixels;
        private float inv_inner_pixels, inv_outer_pixels;
        private float centre_surround_diff;
        private float outer_average, inner_average;
        private int leftside;

        /// <summary>
        /// constructor
        /// </summary>
        public classimage()
        {
            init();
        }


        public void init()
        {
            image=null;
            width=0;
            height=0;
        }


        /// <summary>
        /// create a new image
        /// </summary>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        public void createImage(int wdth, int hght)
        {
            width = wdth;
            height = hght;

            image = new Byte [width * height];
            Integral = new int [width * height];
#if VISUALISATION
            blobs = new float[width, height];
#endif
        }


        /// <summary>
        /// clear the image
        /// </summary>
        public void clear()
        {
            for (int x = 0; x < width * height; x++)
                image[x] = 0;
        }


        /// <summary>
        /// update the integral image
        /// </summary>
        public void updateIntegralImage()
        {
            sluggish.utilities.image.updateIntegralImage(image, width, height, Integral);
        }

        /// <summary>
        /// get image from a bitmap
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        /// <param name="bytes_per_pixel"></param>
        public void updateFromBitmap(Byte[] bmp, int wdth, int hght, int bytes_per_pixel)
        {
            if (bytes_per_pixel == 1)
            {
                for (int i = 0; i < wdth * hght; i++)
                    image[i] = bmp[i];
            }
            else
            {
                int n = 0;
                for (int i = 0; i < wdth * hght * bytes_per_pixel; i += bytes_per_pixel)
                {
                    int tot = 0;
                    for (int j = 0; j < bytes_per_pixel; j++) tot += bmp[i + j];
                    image[n] = (Byte)(tot / bytes_per_pixel);
                    n++;
                }
            }
        }

        public void updateFromBitmapVerticalCompression(Byte[] bmp, int wdth, int hght, int compression,
                                                        int offset_x, int offset_y)
        {
            int n;
            int i = 0;
            for (int y = 0; y < hght / compression; y++)
            {
                int yy = (y*compression) + offset_y;                
                for (int x = 0; x < wdth; x++)
                {
                    int xx = x + offset_x;
                    if ((xx > -1) && (xx < wdth) &&
                        (yy > -1) && (yy < hght))
                    {
                        n = (yy * wdth) + xx;
                        image[i] = bmp[n];
                    }
                    i++;
                }
            }

                    
        }


        /// <summary>
        /// detect blobs
        /// </summary>
        /// <param name="blobradius_x">x radius of the blob in pixels</param>
        /// <param name="blobradius_y">y radius of the blob in pixels</param>
        /// <param name="step_size">step size used for speedup (normally = 1)</param>
        /// <param name="points">array to be populated with responses</param>
        /// <returns></returns>
        public int detectBlobs(int scale, int blobradius_x, int blobradius_y,
                               int step_size, float[,] points, byte[,] scales, byte[,] pattern)
        {
            int average_diff = detectBlobs(scale, blobradius_x, blobradius_y, blobradius_x, blobradius_y, width - blobradius_x - 1 - step_size, height - blobradius_y - 1 - step_size, step_size, points, scales, pattern);
            return (average_diff);
        }



        private float detectBlob_centreSurround(int x, int y, int blobradius_x,
                                                ref float diff2)
        {
            // get the total pixel intensity for the surround region
            outer = sluggish.utilities.image.getIntegral(Integral, tx1, ty1, bx1, by1, width);
            
            // get the total pixel intensity for the centre region
            inner = sluggish.utilities.image.getIntegral(Integral, tx2, ty2, bx2, by2, width);
                        
            // average pixel intensity for the surround region
            outer_average = (outer - inner) * inv_outer_pixels;
            
            // average pixel intensity for the centre region
            inner_average = inner * inv_inner_pixels;
            
            // difference between the centre and surround
            centre_surround_diff = outer_average - inner_average;

            // total pixel intensity to the left
            leftside = sluggish.utilities.image.getIntegral(Integral, tx1, ty1, tx1 + blobradius_x, by1, width);
                        
            // difference between left and right 
            diff2 = ((leftside*2) - outer) * 2 * inv_outer_pixels;

            //note: don't bother trying above/below comparisons, it only degrades the results

            return (centre_surround_diff);
        }


        /// <summary>
        /// detect blobs within the given region
        /// </summary>
        /// <param name="blobradius_x"></param>
        /// <param name="blobradius_y"></param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <param name="step_size"></param>
        /// <param name="points"></param>
        /// <param name="scales"></param>
        /// <param name="pattern"></param>
        /// <returns>average blob response (centre/surround magnitude)</returns>
        public int detectBlobs(int scale, int blobradius_x, int blobradius_y,
                               int tx, int ty, int bx, int by,
                               int step_size, float[,] points, byte[,] scales, byte[,] pattern)
        {
            int i, x, y, hits = 0;
            float average_diff = 0;
            float centre_surround_diff = 0;
            float left_right_diff = 0;
            int diameter_x = blobradius_x * 2;
            int diameter_y = blobradius_y * 2;
            int half_radius_x = blobradius_x / 2;
            if (half_radius_x < 1) half_radius_x = 1;
            int half_radius_y = blobradius_y / 2;
            if (half_radius_y < 1) half_radius_y = 1;

            // number of pixels in the surround region
            outer_pixels = (diameter_x * diameter_y) - (blobradius_x * blobradius_y);
            inv_outer_pixels = 1.0f / outer_pixels;

            // number of pixels in the centre region
            inner_pixels = blobradius_x * blobradius_y;
            inv_inner_pixels = 1.0f / inner_pixels;

            // ensure that the region of interest is sensible
            if (tx < blobradius_x) tx = blobradius_x;
            if (ty < blobradius_y) ty = blobradius_y;
            if (bx > width - blobradius_x - 1) ty = width - blobradius_x - 1;
            if (by > height - blobradius_y - 1) by = height - blobradius_y - 1;

#if VISUALISATION
            for (y = ty; y <= by; y++)
                for (x = tx; x <= bx; x += step_size)
                    blobs[x, y] = 0;
#endif

            for (y = ty; y <= by; y++)
            {
                ty1 = y - blobradius_y;
                by1 = ty1 + diameter_y;
                ty2 = y - half_radius_y;
                by2 = ty2 + blobradius_y;

                i = 0;
                for (x = tx; x <= bx; x += step_size)
                {
                    // get the total pixel intensity for the surround region
                    tx1 = x - blobradius_x;
                    bx1 = tx1 + diameter_x;
                    tx2 = x - half_radius_x;
                    bx2 = tx2 + blobradius_x;

                    // get the centre/surround and left/right responses at this location
                    centre_surround_diff = 
                        detectBlob_centreSurround(x, y, blobradius_x, 
                                                  ref left_right_diff);

#if VISUALISATION
                    blobs[x, y] += centre_surround_diff;
#endif

                    // magnitude of the local centre/surround difference
                    float abs_centre_surround_diff = centre_surround_diff;
                    if (abs_centre_surround_diff < 0) abs_centre_surround_diff = -abs_centre_surround_diff;
                    
                    // magnitude of the local left/right difference
                    float abs_left_right_diff = left_right_diff;
                    if (abs_left_right_diff < 0) abs_left_right_diff = -abs_left_right_diff;
                    
                    // get the current highest magnitude response at this location
                    float abs_best_response = points[y, i];
                    if (abs_best_response < 0) abs_best_response = -abs_best_response;

                    // which is magnitude is largest ?
                    // we want to base our stereo correspondences upon the biggest
                    // magnitudes which are likely to be the most confident
                    if (abs_centre_surround_diff > abs_left_right_diff)
                    {
                        // centre/surround measurement gives us the highest magnitude response
                        if ((scale == 0) || 
                            (abs_centre_surround_diff > abs_best_response))
                        {
                            // record the difference value, scale and the type of pattern (centre/surround comparisson)
                            points[y, i] = centre_surround_diff;
                            scales[y, i] = (byte)scale;
                            pattern[y, i] = sentience_stereo_contours.PATTERN_CENTRE_SURROUND;
                        }
                    }
                    else
                    {
                        // left/right measurement gives us the highest magnitude response
                        if ((scale == 0) || 
                            (abs_left_right_diff > abs_best_response))
                        {
                            // record the difference value, scale and the type of pattern (left/right comparisson)
                            points[y, i] = left_right_diff; 
                            scales[y, i] = (byte)scale;
                            pattern[y, i] = sentience_stereo_contours.PATTERN_LEFT_RIGHT;
                        }
                    }
                    i++;

                    //update average centre-surround difference value
                    if (centre_surround_diff >= 0)
                        average_diff += centre_surround_diff;
                    else
                        average_diff -= centre_surround_diff;
                    hits++;
                }
            }

            // return the average centre/surround response, which can be used to adjust gain/exposure
            if (hits > 0) average_diff /= hits;
            return ((int)average_diff);
        }

    }
}
