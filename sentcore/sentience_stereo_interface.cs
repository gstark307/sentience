/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{

    public class sentience_stereo_interface
    {
        int image_width = 0;
        int image_height = 0;
        int bytes_per_pixel;
        Byte[] left_image = null;
        Byte[] right_image = null;
        sentience_stereo stereovision;
        sentience_stereo_contours stereovision_contours;
        sentience_stereo_FAST stereovision_FAST;
        int currentAlgorithmType = 0;
        int adaptive_threshold = 0;

        public sentience_stereo_interface()
        {
            stereovision = new sentience_stereo();
            stereovision_contours = new sentience_stereo_contours();
            stereovision_FAST = new sentience_stereo_FAST();
        }

        /// <summary>
        /// load mono image data (single byte per pixel)
        /// Note that this assumes that the the image_data array has already been locked
        /// by the calling program
        /// </summary>
        public void loadImage(Byte[] image_data, int width, int height, bool isLeftImage, int bytesPerPixel)
        {
            int j;

            if ((image_width != width) || (image_height != height))
            {
                left_image = new Byte[width * height];
                right_image = new Byte[width * height];

                image_width = width;
                image_height = height;
                bytes_per_pixel = 1;
            }

            
            int tot, offset;
            for (j = 0; j < width * height; j++)
            {
                // get the total intensity
                offset = j * bytesPerPixel;
                tot = 0;
                for (int k = 0; k < bytesPerPixel; k++)
                {
                    tot += image_data[offset + k];
                }
                ///left or right
                if (isLeftImage)
                    left_image[j] = (Byte)(tot / bytesPerPixel);
                else
                    right_image[j] = (Byte)(tot / bytesPerPixel);
            }

        }

        /// <summary>
        /// run the stereo algorithm
        /// </summary>
        public void stereoMatchRun(int calibration_offset_x, int calibration_offset_y, int image_threshold, int peaks_per_row, int algorithm_type)
        {
            currentAlgorithmType = algorithm_type;
            switch (algorithm_type)
            {
                case 0:  // feature based stereo
                    {
                        stereovision.update(left_image, right_image, image_width, image_height, bytes_per_pixel,
                                            calibration_offset_x, calibration_offset_y, image_threshold, peaks_per_row);
                        break;
                    }
                case 1:  // contour based stereo
                    {
                        stereovision_contours.update(left_image, right_image, image_width, image_height,
                                                     calibration_offset_x, calibration_offset_y);
                        break;
                    }
                case 2:  // FAST corners based stereo
                    {
                        stereovision_FAST.update(left_image, right_image, image_width, image_height, bytes_per_pixel,
                                                 calibration_offset_x, calibration_offset_y, ref adaptive_threshold);
                        break;
                    }
            }
        }

        /// <summary>
        /// show nearby objects
        /// </summary>
        /// <param name="raw_image"></param>
        /// <param name="output_img"></param>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        /// <param name="threshold"></param>
        public void getCloseObjects(Byte[] raw_image, Byte[] output_img, Byte[] background_img, int wdth, int hght, int threshold)
        {
            if (currentAlgorithmType == 1)
                stereovision_contours.getCloseObjects(raw_image, output_img, background_img, wdth, hght, threshold);
        }


        /// <summary>
        /// returns a disparity map
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void getDisparityMap(Byte[] img, int width, int height, int threshold)
        {            
            if (currentAlgorithmType == 1)
                stereovision_contours.getDisparityMap(img, width, height, threshold);
        }

        /// <summary>
        /// get the point features returned from stereo matching
        /// </summary>
        /// <returns>The number of features found</returns>
        public int getSelectedPointFeatures(float[] features)
        {
            int no_of_features = 0;
            switch(currentAlgorithmType)
            {
                case 0:
                    {
                        // get features from non-maximal suppression
                        for (int i = 0; i < stereovision.no_of_selected_features * 3; i++)
                        {
                            features[i] = stereovision.selected_features[i];
                        }
                        no_of_features = stereovision.no_of_selected_features;
                        break;
                    }
                case 1:
                    {
                        // get features from the disparity map
                        for (int i = 0; i < stereovision_contours.no_of_selected_features * 3; i++)
                        {
                            features[i] = stereovision_contours.selected_features[i];
                        }
                        no_of_features = stereovision_contours.no_of_selected_features;
                        break;
                    }
                case 2:
                    {
                        // get FAST corner features from non-maximal suppression
                        for (int i = 0; i < stereovision_FAST.no_of_selected_features * 3; i++)
                        {
                            features[i] = stereovision_FAST.selected_features[i];
                        }
                        no_of_features = stereovision_FAST.no_of_selected_features;
                        break;
                    }
            }
            return (no_of_features);
        }



        /// <summary>
        /// Set the difference threshold
        /// </summary>
        public void setDifferenceThreshold(int diff)
        {
            stereovision.difference_threshold = diff;
        }


        /// <summary>
        /// returns the average feature matching score
        /// </summary>
        public float getAverageMatchingScore()
        {
            return (stereovision.average_matching_score);
        }

        /// <summary>
        /// set the number of features required
        /// </summary>
        public void setRequiredFeatures(int no_of_features)
        {
            stereovision.required_features = no_of_features;
            //stereovision_contours.required_features = no_of_features;
            //stereovision_FAST.required_features = no_of_features;
        }

        /// <summary>
        /// sets two radii values as a percentage of the image
        /// resolution, which are used for context matching
        /// features in left and right images
        /// </summary>
        public void setContextRadii(int radius1, int radius2)
        {
            stereovision.matchRadius1 = radius1;
            stereovision.matchRadius2 = radius2;
        }

        /// <summary>
        /// set the radius within which local average
        /// intensity is calculated
        /// </summary>
        public void setLocalAverageRadius(int radius)
        {
            stereovision.localAverageRadius = radius;
        }


        /// <summary>
        /// set the maximum disparity as a percentage of image width
        /// </summary>
        public void setMaxDisparity(int max_disparity)
        {
            stereovision.max_disparity = max_disparity;
            stereovision_contours.max_disparity = max_disparity;
            stereovision_FAST.max_disparity = max_disparity;
        }


        public void stereoCalibrate(Byte[] bmp_left, Byte[] bmp_right,
                               int bmp_width, int bmp_height, int bytesPerPixel,
                               ref int offset_x, ref int offset_y)
        {
            int search_radius_x = bmp_width / 6;
            int search_radius_y = bmp_height / 20;
            int compare_width = bmp_width - (search_radius_x * 2);
            int compare_height = bmp_height - (search_radius_y * 2);

            offset_x = 0;
            offset_y = 0;

            int min_diff = -1;
            for (int x = -search_radius_x; x < search_radius_x; x++)
            {
                for (int y = -search_radius_y; y < search_radius_y; y++)
                {
                    // get the pixel difference at this position
                    int diff = 0;
                    for (int cx = 0; cx < compare_width; cx++)
                    {
                        for (int cy = 0; cy < compare_height; cy++)
                        {
                            int p1 = ((cy + search_radius_y) * bmp_width) + (cx + search_radius_x);
                            int p2 = ((cy + search_radius_y + y) * bmp_width) + (cx + search_radius_x + x);
                            int dp = (int)(bmp_left[p1 * bytesPerPixel]) - (int)(bmp_right[p2 * bytesPerPixel]);
                            if (dp < 0) dp = -dp;
                            diff += dp;
                        }
                    }

                    if ((min_diff == -1) || (diff < min_diff))
                    {
                        min_diff = diff;
                        offset_x = x;
                        offset_y = y;
                    }
                }
            }
        }
    }

}
