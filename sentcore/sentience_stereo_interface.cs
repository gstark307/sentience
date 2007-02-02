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
        FASTlines line_detector = null;

        public sentience_stereo_interface()
        {
            stereovision = new sentience_stereo();
            stereovision_contours = new sentience_stereo_contours();
            stereovision_FAST = new sentience_stereo_FAST();
        }

        /// <summary>
        /// update the disparity values for FAST corners
        /// </summary>
        private void updateFASTCornerDisparities()
        {
            if (line_detector != null)
            {
                if (line_detector.corners != null)
                {
                    for (int i = 0; i < line_detector.corners.Length; i++)
                    {
                        FASTcorner c = line_detector.corners[i];
                        c.disparity = stereovision_contours.getDisparityMapPoint(c.x, c.y);
                    }
                }
            }
        }

        /// <summary>
        /// calculate the disparity values for observed lines
        /// </summary>
        private void updateLineDisparities()
        {
            if (line_detector != null)
            {
                const int linepoints = 8;
                for (int i = 0; i < line_detector.lines.Length; i++)
                {
                    FASTline line = line_detector.lines[i];
                    if (line.Visible)
                    {
                        int dx = line.point2.x - line.point1.x;
                        int dy = line.point2.y - line.point1.y;
                        float tot_lower = 0;
                        int lower_hits = 0;
                        float tot_upper = 0;
                        int upper_hits = 0;
                        for (int j = 0; j < linepoints; j++)
                        {
                            int xx = line.point1.x + (dx * j / linepoints);
                            int yy = line.point1.y + (dy * j / linepoints);
                            float disp = stereovision_contours.getDisparityMapPoint(xx, yy);
                            if (disp > 0)
                            {
                                if (j < linepoints / 2)
                                {
                                    tot_lower += disp;
                                    lower_hits++;
                                }
                                else
                                {
                                    tot_upper += disp;
                                    upper_hits++;
                                }
                            }
                        }
                        if ((lower_hits > 0) && (upper_hits > 0))
                        {
                            float av_lower = tot_lower / lower_hits;
                            float av_upper = tot_upper / upper_hits;
                            line.point1.disparity = av_lower;
                            line.point2.disparity = av_upper;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// load mono image data (single byte per pixel)
        /// Note that this assumes that the the image_data array has already been locked
        /// by the calling program
        /// </summary>
        public void loadImage(Byte[] image_data, int width, int height, bool isLeftImage, int bytesPerPixel)
        {
            if ((isLeftImage) && ((currentAlgorithmType == 2) || (currentAlgorithmType == 3)))
            {
                if (line_detector == null) line_detector = new FASTlines();
                line_detector.drawLines = false;
                line_detector.Update(image_data, width, height);
            }

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
                                                     calibration_offset_x, calibration_offset_y, true);
                        break;
                    }
                case 2:  // FAST corners based stereo
                    {
                        stereovision_contours.vertical_compression = 4;
                        stereovision_contours.disparity_map_compression = 3;
                        stereovision_contours.update(left_image, right_image, image_width, image_height,
                                                     calibration_offset_x, calibration_offset_y, false);
                        updateFASTCornerDisparities();
                        break;
                    }
                case 3:  // stereo lines
                    {
                        stereovision_contours.vertical_compression = 4;
                        stereovision_contours.disparity_map_compression = 3;
                        stereovision_contours.update(left_image, right_image, image_width, image_height,
                                                     calibration_offset_x, calibration_offset_y, false);
                        updateLineDisparities();
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
            if ((currentAlgorithmType == 1) || (currentAlgorithmType == 3))
                stereovision_contours.getDisparityMap(img, width, height, threshold);
        }

        /// <summary>
        /// get the point features returned from stereo matching
        /// </summary>
        /// <returns>The number of features found</returns>
        public int getSelectedPointFeatures(float[] features)
        {
            int no_of_features = 0;
            int max = 0;

            switch(currentAlgorithmType)
            {
                case 0:
                    {
                        // get features from non-maximal suppression
                        max = stereovision.no_of_selected_features * 3;
                        for (int i = 0; i < max; i++)
                        {
                            features[i] = stereovision.selected_features[i];
                        }
                        break;
                    }
                case 1:
                    {
                        // get features from the disparity map
                        max = stereovision_contours.no_of_selected_features * 3;
                        if (max > features.Length) max = features.Length;
                        for (int i = 0; i < max; i++)
                        {
                            features[i] = stereovision_contours.selected_features[i];
                        }
                        break;
                    }
                case 2:
                    {
                        // get FAST corner features from non-maximal suppression
                        if (line_detector != null)
                        {
                            if (line_detector.corners != null)
                            {
                                max = line_detector.corners.Length * 3;
                                if (max > features.Length) max = features.Length;
                                int n = 0;
                                for (int i = 0; i < max; i += 3)
                                {
                                    FASTcorner c = line_detector.corners[n];
                                    features[i] = c.x;
                                    features[i + 1] = c.y;
                                    features[i + 2] = c.disparity;
                                    n++;
                                }
                            }
                        }
                        break;
                    }
                case 3:
                    {
                        // get features from the disparity map
                        max = stereovision_contours.no_of_selected_features * 3;
                        if (max > features.Length) max = features.Length;
                        for (int i = 0; i < max; i++)
                        {
                            features[i] = stereovision_contours.selected_features[i];
                        }
                        break;
                    }
            }
            no_of_features = max / 3;
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
