/*
    simple stereo vision based upon luminence only
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
    /// stereo correspondence based upon edge detection
    /// </summary>
    public class StereoVisionEdges : StereoVision
    {
        // we don't need to sample every row to get
        // usable range data.  Alternate rows will do.
        protected const int vertical_compression = 2;
                
        protected const int ideal_number_of_features = 1000;
                		
		// features detected on each image row
        protected List<float>[] left_row_features;
        protected List<float>[] right_row_features;
		
        public StereoVisionEdges()
        {
            algorithm_type = EDGES;
            BroadcastStereoFeatureColours = false;
        }
		
		/// <summary>
		/// returns an edge detection threshold proportional to the image contrast
		/// </summary>
		/// <param name="img_mono">image data</param>
		/// <param name="width">width of the image</param>
		/// <param name="height">height of the image</param>
		/// <returns>threshold value</returns>
		protected int GetEdgeDetectionThreshold(
		    byte[] img_mono,
		    int width,
		    int height)		    
		{
			// make a ballpark estimate of the image contrast
			int tx = width * 25 / 100;
			int ty = height * 25 / 100;
			int bx = width - tx;
			int by = height - ty;
			int contrast = 0;
			int contrast_hits = 0;
		    for (int y = ty; y < by; y += 4)
		    {
		    	int n = (y * width) + tx;
		        for (int x = tx; x < bx; x += 4, n += 4)
		        {
		        	int pixel_contrast0 = img_mono[n] - img_mono[n-2];
		        	if (pixel_contrast0 < 0) pixel_contrast0 = -pixel_contrast0;
		        	int pixel_contrast1 = img_mono[n+2-width] - img_mono[n];
		        	if (pixel_contrast1 < 0) pixel_contrast1 = -pixel_contrast1;
		
		        	contrast += pixel_contrast1 + pixel_contrast0;
		        	contrast_hits += 2;
		        }
		    }
		    if (contrast_hits > 0) contrast /= contrast_hits;
		
		    int auto_threshold = 5 + (contrast * 120 / 20);
		    return(auto_threshold);	
		}
		
        
        
        protected void MatchFeatures(
            List<float> left_row_features, 
            List<float> right_row_features,
            byte[] left_bmp, byte[] right_bmp)
        {
			int max_disparity_pixels = image_width * max_disparity / 100;
			
			int[,] matching_matrix = new int[left_row_features.Count, right_row_features.Count];
			for (int i = 0; i < left_row_features.Count-4; i += 4)
			{
				int x0 = (int)left_row_features[i];
				int y0 = (int)left_row_features[i + 1];
				int rising0 = (int)left_row_features[i + 2];
			    for (int j = 0; j < right_row_features.Count-4; j += 4)
			    {
					int rising1 = (int)right_row_features[i + 2];
					if (rising0 == rising1)
					{
				        int x1 = (int)right_row_features[j];
						int disp = x0 - x1;
						if ((disp >= 0) && (disp < max_disparity_pixels))
						{
				            int y1 = (int)right_row_features[j + 1];
							
							// TODO
						}
					}
				}
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
			int edge_detection_radius = image_width * 24 / 640;
			
			UpdateEdges(
		        left_bmp_colour, right_bmp_colour,
			    left_bmp, right_bmp,
			    image_width, image_height,
			    calibration_offset_x, 
                calibration_offset_y,
			    edge_detection_radius);
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
        protected void UpdateEdges(
            byte[] left_bmp_colour, byte[] right_bmp_colour,
		    byte[] left_bmp, byte[] right_bmp,
            int image_width, int image_height,
            float calibration_offset_x, 
            float calibration_offset_y,
		    int edge_detection_radius)
        {	
			features.Clear();
            this.image_width = image_width;
            this.image_height = image_height;            
						
			// get the edge detection threshold
			int edge_detection_threshold = GetEdgeDetectionThreshold(left_bmp, image_width, image_height);
			
			int[] buffer = new int[image_width];			
            List<float> left_row_features = new List<float>();
            List<float> right_row_features = new List<float>();
			
			int image_width2 = image_width*2;
			int image_width3 = image_width*3;
			int ty = (int)(vertical_compression + calibration_offset_y);
			int by = image_height - (int)(vertical_compression + calibration_offset_y);
			if (ty < 0) ty = 0;
			if (by > image_height) by = image_height;
			for (int y = ty; y < by; y += vertical_compression)
			{				
				left_row_features.Clear();
				right_row_features.Clear();
				
				// edges in the left image
				
				int n_left = y * image_width;
				int sum_left = left_bmp[n_left] + left_bmp[n_left-image_width] + left_bmp[n_left-image_width2] + left_bmp[n_left-image_width3] + left_bmp[n_left+image_width] + left_bmp[n_left+image_width2] + left_bmp[n_left+image_width3];
				buffer[0] = sum_left;
			    for (int x = 1; x < image_width; x++, n_left++)
			    {
			    	sum_left += left_bmp[n_left] + left_bmp[n_left-image_width] + left_bmp[n_left-image_width2] + left_bmp[n_left-image_width3] +
			    	       left_bmp[n_left+image_width] + left_bmp[n_left+image_width2] + left_bmp[n_left+image_width3];
			    	buffer[x] = sum_left;
			    }
		
			    int edge_start = 0;
			    int edge_rising = 0;
		    	bool prev_is_edge = false;
			    for (int x = edge_detection_radius; x < image_width - 1 - edge_detection_radius; x++)
			    {
			    	int rising = 0;
			    	int r = edge_detection_radius;
			    	bool is_edge = true;
			    	int edge_magnitude = 0;
			    	while (r > 1)
			    	{
			    	    int centre = buffer[x];
			    	    int left = centre - buffer[x - r];
			    	    int right = buffer[x + r] - centre;
			    	    int diff = right - left;
			    	    if (diff < 0)
			    	    {
			    	    	diff = -diff;
			    	    	rising = 1;
			    	    }
			    	    if (diff < edge_detection_threshold * r)
			    	    {
			    	    	is_edge = false;
			    	    	break;
			    	    }
			    	    else
			    	    {
			    	    	edge_magnitude += diff;
			    	    }
			    	    r--;
			    	}
			    	if (is_edge)
			    	{
			    		if (!prev_is_edge)
			    		{
			    			edge_start = x;
			    			edge_rising = rising;
			    		}
			    	}
			    	else
			    	{
			    		if (prev_is_edge)
			    		{
			    		    int edge_x = edge_start + (((x-1) - edge_start) / 2);
			    	    	left_row_features.Add(edge_x);
			    	    	left_row_features.Add(y);
			    	    	left_row_features.Add(rising);
			    	    	left_row_features.Add(edge_magnitude);
			    		}
			    	}
			    	prev_is_edge = is_edge;
			    }
								
				if (left_row_features.Count > 0)
				{
				
					// edges in the right image
					int n_right = (int)(y + calibration_offset_y) * image_width;
					int sum_right = right_bmp[n_right] + right_bmp[n_right-image_width] + right_bmp[n_right-image_width2] + right_bmp[n_right-image_width3] + right_bmp[n_right+image_width] + right_bmp[n_right+image_width2] + right_bmp[n_right+image_width3];
					buffer[0] = sum_right;
				    for (int x = 1; x < image_width; x++, n_right++)
				    {
				    	sum_right += right_bmp[n_right] + right_bmp[n_right-image_width] + right_bmp[n_right-image_width2] + right_bmp[n_right-image_width3] +
				    	       right_bmp[n_right+image_width] + right_bmp[n_right+image_width2] + right_bmp[n_right+image_width3];
				    	buffer[x] = sum_right;
				    }
			
				    edge_start = 0;
				    edge_rising = 0;
			    	prev_is_edge = false;
				    for (int x = edge_detection_radius; x < image_width - 1 - edge_detection_radius; x++)
				    {
				    	int rising = 0;
				    	int r = edge_detection_radius;
				    	bool is_edge = true;
				    	int edge_magnitude = 0;
				    	while (r > 1)
				    	{
				    	    int centre = buffer[x];
				    	    int left = centre - buffer[x - r];
				    	    int right = buffer[x + r] - centre;
				    	    int diff = right - left;
				    	    if (diff < 0)
				    	    {
				    	    	diff = -diff;
				    	    	rising = 1;
				    	    }
				    	    if (diff < edge_detection_threshold * r)
				    	    {
				    	    	is_edge = false;
				    	    	break;
				    	    }
				    	    else
				    	    {
				    	    	edge_magnitude += diff;
				    	    }
				    	    r--;
				    	}
				    	if (is_edge)
				    	{
				    		if (!prev_is_edge)
				    		{
				    			edge_start = x;
				    			edge_rising = rising;
				    		}
				    	}
				    	else
				    	{
				    		if (prev_is_edge)
				    		{
				    		    int edge_x = edge_start + (((x-1) - edge_start) / 2) + (int)calibration_offset_x;
				    	    	right_row_features.Add(edge_x);
				    	    	right_row_features.Add(y);
				    	    	right_row_features.Add(rising);
				    	    	right_row_features.Add(edge_magnitude);
				    		}
				    	}
				    	prev_is_edge = is_edge;
				    }
	
					if (right_row_features.Count > 0)
					{
						// match left and right features
		                MatchFeatures(
		                    left_row_features, 
		                    right_row_features,
		                    left_bmp, right_bmp);
					}
				}
			}
			
        }

    }
}
