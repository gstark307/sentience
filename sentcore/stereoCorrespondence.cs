/*
    The main stereo correspondence class
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
    /// the main stereo correspondence class
    /// </summary>
    public class stereoCorrespondence
    {
        // an interface to different stereo correspondence algorithms
        sentience_stereo_interface stereointerface = new sentience_stereo_interface();

        public stereoCorrespondence(int no_of_stereo_features)
        {
            stereointerface.setRequiredFeatures(no_of_stereo_features);
            stereointerface.setDifferenceThreshold(100);
            stereointerface.setMaxDisparity(5);
        }

        /// <summary>
        /// returns the left and right rectified images
        /// </summary>
        /// <param name="left_image">left rectified mono image</param>
        /// <param name="right_image">right rectified mono image</param>
        public void getRectifiedImages(ref Byte[] left_image, ref Byte[] right_image)
        {
            left_image = stereointerface.left_image;
            right_image = stereointerface.right_image;
        }
        
        /// <summary>
        /// returns a rectified image from the left or right camera
        /// </summary>
        /// <param name="leftImage"></param>
        /// <returns>rectified bitmap image - one byte per pixel</returns>
        public Byte[] getRectifiedImage(bool leftImage)
        {
            if (leftImage)
                return (stereointerface.left_image);
            else
                return (stereointerface.right_image);
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
        /// set the calibration data
        /// </summary>
        /// <param name="calib"></param>
        public void setCalibration(calibrationStereo calib)
        {
            stereointerface.setCalibration(calib);
        }

        /// <summary>
        /// load calibration data from file
        /// </summary>
        /// <param name="calibrationFilename"></param>
        public void loadCalibration(String calibrationFilename)
        {
            stereointerface.loadCalibration(calibrationFilename);
        }

        /// <summary>
        /// load a pair of rectified images
        /// </summary>
        /// <param name="stereo_cam_index">index of the stereo camera</param>
        /// <param name="fullres_left">left image</param>
        /// <param name="fullres_right">right image</param>
        /// <param name="head">stereo head geometry and features</param>
        /// <param name="no_of_stereo_features"></param>
        /// <param name="bytes_per_pixel"></param>
        /// <param name="algorithm_type"></param>
        /// <returns></returns>
        public float loadRectifiedImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, stereoHead head, int no_of_stereo_features, int bytes_per_pixel,
                                         int algorithm_type)
        {
            setCalibration(null);  // don't use any calibration data
            return(loadImages(stereo_cam_index, fullres_left, fullres_right, head, no_of_stereo_features, bytes_per_pixel, algorithm_type));
        }

        /// <summary>
        /// load a pair of raw (unrectified) images
        /// </summary>
        /// <param name="stereo_cam_index">index of the stereo camera</param>
        /// <param name="fullres_left">left image</param>
        /// <param name="fullres_right">right image</param>
        /// <param name="head">stereo head geometry and features</param>
        /// <param name="no_of_stereo_features"></param>
        /// <param name="bytes_per_pixel"></param>
        /// <param name="algorithm_type"></param>
        /// <returns></returns>
        public float loadRawImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, stereoHead head, int no_of_stereo_features, int bytes_per_pixel,
                                   int algorithm_type)
        {
            setCalibration(head.calibration[stereo_cam_index]);  // load the appropriate calibration settings for this camera
            return(loadImages(stereo_cam_index, fullres_left, fullres_right, head, no_of_stereo_features, bytes_per_pixel, algorithm_type));
        }

        /// <summary>
        /// load a pair of images
        /// </summary>
        private float loadImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, stereoHead head, int no_of_stereo_features, int bytes_per_pixel,
                                 int algorithm_type)
        {
            stereointerface.loadImage(fullres_left, head.calibration[stereo_cam_index].leftcam.image_width, head.calibration[stereo_cam_index].leftcam.image_height, true, bytes_per_pixel);

            stereointerface.loadImage(fullres_right, head.calibration[stereo_cam_index].leftcam.image_width, head.calibration[stereo_cam_index].leftcam.image_height, false, bytes_per_pixel);
            
            // calculate stereo disparity features
            int peaks_per_row = 5;
            stereointerface.stereoMatchRun(0, peaks_per_row, algorithm_type);

            // retrieve the features
            stereoFeatures feat = new stereoFeatures(no_of_stereo_features);
            int no_of_selected_features = 0;
            stereoFeatures features;

            no_of_selected_features = stereointerface.getSelectedPointFeatures(feat.features);
            if (no_of_selected_features > 0)
            {
                
                if (no_of_selected_features == no_of_stereo_features)
                {
                    features = feat;
                }
                else
                {
                 
                    features = new stereoFeatures(no_of_selected_features);
                    for (int f = 0; f < no_of_selected_features * 3; f++)
                        features.features[f] = feat.features[f];
                }

                // update the head with these features
                head.setStereoFeatures(stereo_cam_index, features);

                // update the colours for each feature
                head.updateFeatureColours(stereo_cam_index, fullres_left);
            }

            if (no_of_selected_features > 0) //no_of_stereo_features * 4 / 10)
                return (stereointerface.getAverageMatchingScore());
            else
                return (-1);
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

        public void setSurroundRadius(float value)
        {
            stereointerface.setSurroundRadius(value);
        }

        public void setMatchingThreshold(float value)
        {
            stereointerface.setMatchingThreshold(value);
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
