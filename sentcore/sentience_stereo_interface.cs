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
using System.IO;
using System.Collections.Generic;
using System.Text;
using sentience.calibration;

namespace sentience.core
{
    /// <summary>
    /// an interface class which allows multiple types of stereo
    /// correspondence algorithm to be selected
    /// </summary>
    public class sentience_stereo_interface
    {
        // types of correspondence algorithm
        public const int CORRESPONDENCE_SIMPLE = 0;
        public const int CORRESPONDENCE_CONTOURS = 1;
        public const int CORRESPONDENCE_FAST = 2;
        public const int CORRESPONDENCE_LINES = 3;

        // left and right images (mono) and their dimensions
        public int image_width = 0;
        public int image_height = 0;
        private int bytes_per_pixel;
        public Byte[] left_image = null;
        public Byte[] right_image = null;
        
        // objects to handle different types of stereo correspondence
        private sentience_stereo stereovision;
        private sentience_stereo_contours stereovision_contours;
        private sentience_stereo_FAST stereovision_FAST;

        // the current type of algorithm being used
        private int currentAlgorithmType = CORRESPONDENCE_SIMPLE;
        private FASTlines line_detector = null;

        // calibration parameters for this stereo camera
        private calibrationStereo calibration = null;

        public sentience_stereo_interface()
        {
            stereovision = new sentience_stereo();
            stereovision_contours = new sentience_stereo_contours();
            stereovision_FAST = new sentience_stereo_FAST();
        }

        /// <summary>
        /// load stereo calibration parameters from file
        /// </summary>
        /// <param name="calibrationFilename"></param>
        public void loadCalibration(String calibrationFilename)
        {
            if (File.Exists(calibrationFilename))
            {
                calibration = new calibrationStereo();
                calibration.Load(calibrationFilename);
                calibration.updateCalibrationMaps();
            }
            else calibration = null;
        }

        /// <summary>
        /// set the calibration data
        /// </summary>
        /// <param name="calib"></param>
        public void setCalibration(calibrationStereo calib)
        {
            calibration = calib;
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
                if (line_detector.lines != null)
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
        }

        /// <summary>
        /// loads a colour image with the given number of bytes per pixel
        /// and stores it as a mono image, also applying the camera calibration
        /// lookup table
        /// </summary>
        public void loadImage(Byte[] image_data, int width, int height, bool isLeftImage, int bytesPerPixel)
        {
            if ((isLeftImage) && 
                ((currentAlgorithmType == CORRESPONDENCE_FAST) ||
                (currentAlgorithmType == CORRESPONDENCE_LINES)))
            {
                if (line_detector == null) line_detector = new FASTlines();
                line_detector.drawLines = false;
                line_detector.Update(image_data, width, height);
            }

            if ((image_width != width) || (image_height != height))
            {
                left_image = new Byte[width * height];
                right_image = new Byte[width * height];

                image_width = width;
                image_height = height;
                bytes_per_pixel = 1;
            }

            
            int tot, offset;
            for (int j = 0; j < width * height; j++)
            {
                int n = j;
                
                if (calibration != null)
                {
                    if (isLeftImage)
                        n = calibration.leftcam.calibration_map[j];
                    else
                        n = calibration.rightcam.calibration_map[j];
                }                

                // get the total intensity
                offset = n * bytesPerPixel;
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
        public void stereoMatchRun(int image_threshold, int peaks_per_row, int algorithm_type)
        {
            float calibration_offset_x=0, calibration_offset_y=0;
            if (calibration != null)
            {
                calibration_offset_x = calibration.offset_x;
                calibration_offset_y = calibration.offset_y;
            }

            currentAlgorithmType = algorithm_type;
            switch (algorithm_type)
            {
                case CORRESPONDENCE_SIMPLE:  // feature based stereo
                    {
                        stereovision.update(left_image, right_image, image_width, image_height, bytes_per_pixel,
                                            calibration_offset_x, calibration_offset_y, image_threshold, peaks_per_row);
                        break;
                    }
                case CORRESPONDENCE_CONTOURS:  // contour based stereo
                    {
                        stereovision_contours.update(left_image, right_image, image_width, image_height,
                                                     calibration_offset_x, calibration_offset_y, true);
                        break;
                    }
                case CORRESPONDENCE_FAST:  // FAST corners based stereo
                    {
                        stereovision_contours.vertical_compression = 4;
                        stereovision_contours.disparity_map_compression = 3;
                        stereovision_contours.update(left_image, right_image, image_width, image_height,
                                                     calibration_offset_x, calibration_offset_y, false);
                        updateFASTCornerDisparities();
                        break;
                    }
                case CORRESPONDENCE_LINES:  // stereo lines
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
            if (currentAlgorithmType == CORRESPONDENCE_CONTOURS)
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
            if ((currentAlgorithmType == CORRESPONDENCE_CONTOURS) ||
                (currentAlgorithmType == CORRESPONDENCE_LINES))
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
                case CORRESPONDENCE_SIMPLE:
                    {
                        // get features from non-maximal suppression
                        max = stereovision.no_of_selected_features * 3;
                        for (int i = 0; i < max; i++)
                        {
                            features[i] = stereovision.selected_features[i];
                        }
                        break;
                    }
                case CORRESPONDENCE_CONTOURS:
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
                case CORRESPONDENCE_FAST:
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
                case CORRESPONDENCE_LINES:
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

        public void setSurroundRadius(float value)
        {
            stereovision_contours.surround_radius_percent = value;
        }

        public void setMatchingThreshold(float value)
        {
            stereovision_contours.match_threshold = value;
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
