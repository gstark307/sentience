/*
    simple stereo correspondence based on luminence and colour matching
    The performance of this algorithm is not much better than luminence only
    Copyright (C) 2008 Bob Mottram
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
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// geometric stereo correspondence which combines point disparities
    /// with line features
    /// </summary>
    public class StereoVisionGeometric : StereoVisionSimple
    {
		// dynamic threshold for non-maximal supression
		protected int FAST_feature_threshold = 5;

        // ideal number of corner features		
		public int required_FAST_features = 300;
		
        // minimum line length as a percentage of the image width		
		public int minimum_line_length = 10;

		// absolute threshold for detection of line features
		protected int line_detection_threshold = 50;
		
        // detected line features
		protected List<FASTline> lines;
		
        public StereoVisionGeometric()
        {
            algorithm_type = GEOMETRIC;
			feature_scale = 0.4f;
			BroadcastStereoFeatureColours = true;
        }
		
		
		
		protected void UpdateCornerDisparities(FASTcorner[] corners, 
		                                       int search_radius_percent)
		{
			int[] disparity_histogram = new int[image_width];
			
			int search_pixels = image_width * search_radius_percent / 100;
			for (int i = 0; i < corners.Length; i++)
			{
				int min_dist = search_pixels;
				min_dist *= min_dist;
				int disparity = 0;
				int max = 0;
				
				for (int d = disparity_histogram.Length-1; d >= 0; d--)
					disparity_histogram[d] = 0;
				
				for (int j = 0; j < features.Count; j++)
				{
					int dx = (int)(features[j].x - corners[i].x);
					int dy = (int)(features[j].y - corners[i].y);
					int dist = dx*dx + dy*dy;
					if (dist < min_dist)
					{
						int disp = (int)features[j].disparity;
						disparity_histogram[disp]++;
						if (disparity_histogram[disp] > max)
						{
							max = disparity_histogram[disp];
							disparity = disp;
						}
						//min_dist = dist;
					}
				}
				if (max > 1)
				{
				    corners[i].disparity = disparity;
				}
				else
					corners[i].disparity = -1;
			}
			
			features.Clear();
			for (int i = 0; i < corners.Length; i++)
			{
				if (corners[i].disparity > 0)					
				    features.Add(new StereoFeature(corners[i].x, corners[i].y, corners[i].disparity));
			}
		}

        protected List<FASTline> DetectLines(FASTcorner[] corners,
		                                     byte[] img, int image_width, int image_height)
		{
			List<FASTline> lines = new List<FASTline>();
			int average_response = 0;
			int hits = 0;

			int min_line_length_pixels = image_width * minimum_line_length / 100;
			int min_line_length_pixels_sqr = min_line_length_pixels * min_line_length_pixels; 
			
			for (int i = 0; i < corners.Length-1; i++)
			{
			    for (int j = i + 1; j < corners.Length; j++)
			    {
					int dx = corners[i].x - corners[j].x;
					int dy = corners[i].y - corners[j].y;
					
					int length_sqr = dx*dx + dy*dy;
					if (length_sqr > min_line_length_pixels_sqr)
					{
						// is this a line ?
						int response = 0;
						if (FASTline.isLine(img, image_width, image_height, corners[i], corners[j], line_detection_threshold, ref response))
						{
							FASTline line = new FASTline(corners[i], corners[j], response);
							lines.Add(line);
							
							average_response += response;
							hits++;
						}
					}
				}
			}
			
			if (hits > 0)
			{
				average_response /= hits;
				int threshold = average_response * 150 / 100;
				for (int i = lines.Count-1; i >= 0; i--)
				{
					if (lines[i].response < threshold)
						lines.RemoveAt(i);
				}
				//Console.WriteLine("average: " + average_response.ToString());
			}
			return(lines);
		}
		
        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp_colour">rectified left colour image data</param>
        /// <param name="right_bmp_colour">rectified right colour image_data</param>
        /// <param name="left_bmp">rectified left colour image data</param>
        /// <param name="right_bmp">rectified right colour image_data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>		
        public override void Update(byte[] left_bmp_colour, byte[] right_bmp_colour,
		                            byte[] left_bmp, byte[] right_bmp,
                                    int image_width, int image_height,
                                    float calibration_offset_x, float calibration_offset_y)
        {
			// update sparse stereo features
			UpdateSimple(left_bmp_colour, right_bmp_colour,
			             left_bmp, right_bmp,
			             image_width, image_height,
			             calibration_offset_x, calibration_offset_y);			
			
            // extract FAST corner features from the left image
            FASTcorner[] corners_all = FAST.fast_corner_detect_9(left_bmp_mono[0], image_width, image_height, FAST_feature_threshold);
            FASTcorner[] corners = FAST.fast_nonmax(left_bmp_mono[0], image_width, image_height, corners_all, FAST_feature_threshold * 2, 0, 0);
			
            //features.Clear();			
			
            // adjust the threshold
			int no_of_feats = 0;
			if (corners != null)
			{
	            no_of_feats = corners.Length;
								
				//for (int i = 0; i < corners.Length; i++)
				//	features.Add(new StereoFeature(corners[i].x, corners[i].y, 20));
			}			
            if (no_of_feats < required_FAST_features / 2) FAST_feature_threshold -= 1;
            if (no_of_feats > required_FAST_features) FAST_feature_threshold += 1;
			
			if (Math.Abs(no_of_feats - required_FAST_features) < 100)
                UpdateCornerDisparities(corners, 10);
			else
				features.Clear();
			
			//if (Math.Abs(no_of_feats - required_FAST_features) < 100)
			//    lines = DetectLines(corners, left_bmp_mono[0], image_width, image_height);
			//else 
			//	lines = null;
		    
        }
              

    }
}