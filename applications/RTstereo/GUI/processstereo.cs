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
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;  //for DLL access
using sentience.core;

namespace WindowsApplication1
{
    public class processstereo
    {
        sentience_stereo_interface stereointerface = new sentience_stereo_interface();
        stereoFeatures features, prev_features=null;
        sentienceTracking tracking;
        sentienceRadar radar;
        stereoHead robot_head;
        stereoModel stereo_model;
        int correspondence_algorithm_type = 0;

        int peaks_per_row = 5;

        // maximum disparity as a percentage of the image width
        public int max_disparity_percent = 20;

        // number of stereo features required
        public int required_features = 100;

        // difference threshold, relative to the background average
        // the lower this threshold the more features are detected, 
        // but the more succeptible to noise the system is
        public int difference_threshold = 100;

        // maximum number of features
        const int MAX_FEATURES = 50000;

        // number of features detected
        public int no_of_disparities = 0;
        public float[] disparities;

        private bool initialised = false;

        public processstereo()
        {
            disparities = new float[MAX_FEATURES * 3];
            tracking = new sentienceTracking();
            radar = new sentienceRadar();
            robot_head = new stereoHead(1);
            stereo_model = new stereoModel();
        }

        public void loadCalibrationData(String filename)
        {
            robot_head.loadCalibrationData(0, filename);
        }

        /// <summary>
        /// returns the WDM driver name for the camibrated camera
        /// </summary>
        /// <returns></returns>
        public String getCameraDriverName()
        {
            String driverName = "";
            driverName = robot_head.calibration[0].DriverName;
            return (driverName);
        }

        public float getObstacleDistance(float focal_length_mm, float camera_baseline_mm,
                                         int img_width, float FOV_radians)
        {
            return(radar.getObstacleDistance(focal_length_mm,camera_baseline_mm,img_width,FOV_radians));
        }

        /// <summary>
        /// set the type of stereo correspondence algorithm
        /// </summary>
        /// <param name="algorithm_type"></param>
        public void setCorrespondenceAlgorithmType(int algorithm_type)
        {
            correspondence_algorithm_type = algorithm_type;
        }



        /// <summary>
        /// return the radar image
        /// </summary>
        /// <returns></returns>
        public Byte[] getRadarImage()
        {
            return (radar.img);
        }


        public Byte[] getRaysImage(int img_width, int img_height)
        {
            Byte[] img_rays = new Byte[img_width * img_height * 3];
            robot_head.setStereoFeatures(0, features.features, features.uncertainties, features.features.Length);
            //viewpoint v = stereo_model.createViewpoint(robot_head, null);
            //v.showAbove(img_rays, img_width, img_height, 2000, 255, 255, 255, true, 50, true);
            return (img_rays);
        }

        /// <summary>
        /// return a disparity map
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void getDisparityMap(Byte[] img, int width, int height, int threshold)
        {
            stereointerface.getDisparityMap(img, width, height, threshold);
        }

        /// <summary>
        /// show nearby objects
        /// </summary>
        /// <param name="raw_image"></param>
        /// <param name="output_img"></param>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        /// <param name="threshold"></param>
        public void getCloseObjects(Byte[] raw_image, Byte[] output_img, Byte[] background_image, int wdth, int hght, int threshold)
        {
            stereointerface.getCloseObjects(raw_image, output_img, background_image, wdth, hght, threshold);
        }


        public void LoadCalibration(String filename)
        {
            stereointerface.loadCalibration(filename);
        }

        public unsafe void stereoMatch(Byte[] left_bmp, Byte[] right_bmp,
                                        int width, int height, bool colourImages)
        {
            if (!initialised)
            {
                initialised = true;
            }

            int BytesPerPixel = 3;
            if (!colourImages) BytesPerPixel = 1;

            stereointerface.loadImage(left_bmp, width, height, true, BytesPerPixel);

            stereointerface.loadImage(right_bmp, width, height, false, BytesPerPixel);

            setMaxDisparity(max_disparity_percent);
            setRequiredFeatures(required_features);

            // do the stereo correspondence            
            stereointerface.stereoMatchRun(0, peaks_per_row, correspondence_algorithm_type);

            // retrieve features
            no_of_disparities = stereointerface.getSelectedPointFeatures(disparities);

            // track the features
            features = new stereoFeatures(no_of_disparities);

            if (correspondence_algorithm_type == 0)
            {
                for (int i = 0; i < no_of_disparities * 3; i++) features.features[i] = disparities[i];
                tracking.update(features, width, height);
                for (int i = 2; i < no_of_disparities; i += 3) disparities[i] = features.features[i];
            }
            //robot_head.features[0] = features;

            // update radar
            radar.update(width, height, features);

            // dynamically adjust the difference threshold to try to
            // maintain a constant number of stereo features
            if (no_of_disparities < required_features*9/10)
            {
                difference_threshold -= 1;
                if (difference_threshold < 10)
                {
                    difference_threshold = 10;
                }
                setDifferenceThreshold(difference_threshold);
                if (no_of_disparities < required_features * 8 / 10)
                    if (peaks_per_row < 10) peaks_per_row++;
            }
            else
            {
                if (no_of_disparities >= required_features*9/10)
                {
                    if (peaks_per_row > 5)
                        peaks_per_row--;
                    else
                    {
                        difference_threshold += 2;
                        if (difference_threshold > 10000) difference_threshold = 10000;
                        setDifferenceThreshold(difference_threshold);
                    }
                }
            }

            prev_features = features;

        }

        public void setRequiredFeatures(int no_of_features)
        {
            stereointerface.setRequiredFeatures(no_of_features);
        }

        public void setContextRadii(int radius1, int radius2)
        {
            stereointerface.setContextRadii(radius1, radius2);
        }

        public void setMaxDisparity(int max_disparity)
        {
            stereointerface.setMaxDisparity(max_disparity);
        }

        public void setLocalAverageRadius(int radius)
        {
            stereointerface.setLocalAverageRadius(radius);
        }

        public void stereoCalibrate(Byte[] bmp_left, Byte[] bmp_right,
                               int bmp_width, int bmp_height, int bytesPerPixel,
                               ref int offset_x, ref int offset_y)
        {
            stereointerface.stereoCalibrate(bmp_left, bmp_right, bmp_width, bmp_height,
                                            bytesPerPixel, ref offset_x, ref offset_y);
        }

        public void setDifferenceThreshold(int diff)
        {
            stereointerface.setDifferenceThreshold(diff);
        }


    }
}
