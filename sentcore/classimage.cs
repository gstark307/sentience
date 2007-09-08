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

using System;
using sluggish.utilities;

namespace sentience.core
{
    public class classimage
    {
        //dimensions of the image
        public int width, height;

        public Byte[] image;
        long[,] Integral;

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
            Integral = new long [width, height];
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
                               int step_size, float[,] points, int[,] scales, int[,] pattern,
                               int blob_type)
        {
            int average_diff = detectBlobs(scale, blobradius_x, blobradius_y, blobradius_x, blobradius_y, width - blobradius_x - 1 - step_size, height - blobradius_y - 1 - step_size, step_size, points, scales, pattern, blob_type);
            return (average_diff);
        }

        /// <summary>
        /// detect a blob at the given location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blobradius_x"></param>
        /// <param name="blobradius_y"></param>
        /// <param name="diameter_x"></param>
        /// <param name="diameter_y"></param>
        /// <param name="half_radius_x"></param>
        /// <param name="half_radius_y"></param>
        /// <param name="outer_pixels"></param>
        /// <param name="inner_pixels"></param>
        /// <returns></returns>
        private float detectBlobPoint(int x, int y, int blobradius_x, int blobradius_y, 
                                      int diameter_x, int diameter_y, int half_radius_x, int half_radius_y, 
                                      float outer_pixels, float inner_pixels, int blob_type,
                                      ref float diff2)
        {
            float diff = 0;

            switch (blob_type)
            {
                case 0: // centre/surround arrangement
                {
                    diff = detectBlob_centreSurround(x, y, blobradius_x, blobradius_y, diameter_x, diameter_y,
                                              half_radius_x, half_radius_y, outer_pixels, inner_pixels, ref diff2);
                    break; 
                }
            }

            return (diff);
        }


        private float detectBlob_centreSurround(int x, int y, int blobradius_x, int blobradius_y,
                                                int diameter_x, int diameter_y, int half_radius_x, int half_radius_y,
                                                float outer_pixels, float inner_pixels,
                                                ref float diff2)
        {
            // centre/surround

            int tx1, ty1, bx1, by1;
            int tx2, ty2, bx2, by2;
            long outer, inner;
            float diff;

            tx1 = x - blobradius_x;
            ty1 = y - blobradius_y;
            bx1 = tx1 + diameter_x;
            by1 = ty1 + diameter_y;
            outer = sluggish.utilities.image.getIntegral(Integral, tx1, ty1, bx1, by1);
            tx2 = x - half_radius_x;
            ty2 = y - half_radius_y;
            bx2 = tx2 + blobradius_x;
            by2 = ty2 + blobradius_y;
            inner = sluggish.utilities.image.getIntegral(Integral, tx2, ty2, bx2, by2);
            float outer_average = (outer - inner) / outer_pixels;
            float inner_average = inner / inner_pixels;
            diff = outer_average - inner_average;

            // left/right
            long leftside = sluggish.utilities.image.getIntegral(Integral, tx1, ty1, tx1 + blobradius_x, by1);
            float left_average = leftside * 2 / outer_pixels;
            float right_average = (outer - leftside) * 2 / outer_pixels;
            diff2 = left_average - right_average;

            //note: don't bother trying above/below comparisons, it only degrades the results

            return (diff);
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
        /// <returns></returns>
        public int detectBlobs(int scale, int blobradius_x, int blobradius_y,
                               int tx, int ty, int bx, int by,
                               int step_size, float[,] points, int[,] scales, int[,] pattern,
                               int blob_type)
        {
            int i, x, y, hits = 0, average_diff = 0;
            float outer_pixels, inner_pixels;
            float diff = 0;
            int diameter_x = blobradius_x * 2;
            int diameter_y = blobradius_y * 2;
            int half_radius_x = blobradius_x / 2;
            if (half_radius_x < 1) half_radius_x = 1;
            int half_radius_y = blobradius_y / 2;
            if (half_radius_y < 1) half_radius_y = 1;

            //mono images only use the first channel
            outer_pixels = (diameter_x * diameter_y) - (blobradius_x * blobradius_y);
            inner_pixels = blobradius_x * blobradius_y;

            if (tx < blobradius_x) tx = blobradius_x;
            if (ty < blobradius_y) ty = blobradius_y;
            if (bx > width - blobradius_x - 1) ty = width - blobradius_x - 1;
            if (by > height - blobradius_y - 1) by = height - blobradius_y - 1;

            float diff2 = 0;
            for (y = ty; y <= by; y++)
            {
                i = 0;
                for (x = tx; x <= bx; x += step_size)
                {
                    diff = detectBlobPoint(x, y, blobradius_x, blobradius_y, diameter_x, diameter_y, half_radius_x, half_radius_y, outer_pixels, inner_pixels, blob_type, ref diff2);
                    float absdiff = diff;
                    if (absdiff < 0) absdiff = -absdiff;
                    float absdiff2 = diff2;
                    if (absdiff2 < 0) absdiff2 = -absdiff2;
                    
                    float absdiff4 = points[y, i];
                    if (absdiff4 < 0) absdiff4 = -absdiff4;

                    if (absdiff > absdiff2)
                    {
                        if ((scale == 0) || (absdiff > absdiff4))
                        {
                            points[y, i] = diff; //add the point to the list
                            scales[y, i] = scale;
                            pattern[y, i] = 0;
                        }
                    }
                    else
                    {
                        if ((scale == 0) || (absdiff2 > absdiff4))
                        {
                            points[y, i] = diff2; //add the point to the list
                            scales[y, i] = scale;
                            pattern[y, i] = 1;
                        }
                    }
                    i++;

                    //update average centre-surround difference value
                    if (diff >= 0)
                        average_diff += (int)diff;
                    else
                        average_diff -= (int)diff;
                    hits++;
                }
            }

            if (hits > 0) average_diff /= hits;
            return (average_diff);
        }

    }
}
