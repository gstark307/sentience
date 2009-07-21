/*
    edge based stereo
    Copyright (C) 2009 Bob Mottram
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
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// stereo correspondence based upon edge detection
    /// </summary>
    public class StereoVisionEdges : StereoVision
    {
        private StereoVisionEdgesCam[] camera;

        /* feature detection params */
        int inhibition_radius = 16;
        uint minimum_response = 300;

        /* matching params */
        int ideal_no_of_matches = 200;
        int descriptor_match_threshold = 0;
        
        /* These weights are used during matching of stereo features.
         * You can adjust them if you wish */
        int learnDesc = 18*5;  /* weight associated with feature descriptor match */
        int learnLuma = 7*5;   /* weight associated with luminance match */
        int learnDisp = 1;   /* weight associated with disparity (bias towards smaller disparities) */
        int learnPrior = 4;  /* weight associated with prior disparity */

        public int max_disparity_percent = 40;
        public int use_priors = 1;
        
        public StereoVisionEdges()
        {
            algorithm_type = EDGES;
            camera = new StereoVisionEdgesCam[2];
            for (int cam = 0; cam < 2; cam++)
            {
                camera[cam] = new StereoVisionEdgesCam();
                camera[cam].init(320, 240);
            }
        }

        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp_colour">rectified left image data</param>
        /// <param name="right_bmp_colour">rectified right_image_data</param>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right_image_data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        public override void Update(
            byte[] left_bmp_colour, byte[] right_bmp_colour,
		    byte[] left_bmp, byte[] right_bmp,
            int image_width, int image_height,
            float calibration_offset_x, 
            float calibration_offset_y)
        {
            byte[] rectified_frame_buf;
            StereoVisionEdgesCam stereocam = null;
            
            int calib_offset_x = -(int)calibration_offset_x;
            int calib_offset_y = (int)calibration_offset_y;
            for (int cam = 1; cam >= 0; cam--) 
            {
                int no_of_feats = 0;
                int no_of_feats_horizontal = 0;
                if (cam == 0) 
                {
                    rectified_frame_buf = left_bmp_colour;
                    stereocam = camera[0];
                }
                else 
                {
                    rectified_frame_buf = right_bmp_colour;
                    stereocam = camera[1];
                }

                stereocam.imgWidth = (uint)image_width;
                stereocam.imgHeight = (uint)image_height;
                
                no_of_feats = stereocam.svs_get_features_vertical(
                    rectified_frame_buf,
                    inhibition_radius,
                    minimum_response,
                    calib_offset_x,
                    calib_offset_y);

                if (cam == 0) 
                {
                    no_of_feats_horizontal = stereocam.svs_get_features_horizontal(
                        rectified_frame_buf,
                        inhibition_radius,
                        minimum_response,
                        calib_offset_x,
                        calib_offset_y);
                }

                calib_offset_x = 0;
                calib_offset_y = 0;
            }

            int matches = camera[0].svs_match(
                camera[1],
                ideal_no_of_matches,
                max_disparity_percent,
                descriptor_match_threshold,
                learnDesc,
                learnLuma,
                learnDisp,
                learnPrior,
                use_priors);

            features.Clear();
            for (int i = 0; i < matches; i++)
            {
                features.Add(new StereoFeature(
                    (float)(camera[0].svs_matches[i*4+1]), 
                    (float)(camera[0].svs_matches[i*4+2]), 
                    (float)(camera[0].svs_matches[i*4+3])));
		    }
        }
    }
}
