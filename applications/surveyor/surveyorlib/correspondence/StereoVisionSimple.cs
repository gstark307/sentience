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
    /// simple stereo correspondence (luminence only)
    /// </summary>
    public class StereoVisionSimple : StereoVision
    {
		// matching threshold used to kill off stragglers
        public int similarity_threshold_percent = 50;		
		
        // absolute maximum sum of squared differences threshold
        // used when matching left and right images
        public int matching_threshold = 300000;
        
        // radius in pixels to use when evaluating match quality
        public int compare_radius = 90;
        
        // we don't need to sample every row to get
        // usable range data
        public int vertical_compression = 2;
        
        // absolute minimum sum of squared differences value, below which
        // we're just seeing noise
        public int minimum_response = 2000;
        
        // radius in pixels to use for sum of squared differences
        public int summation_radius = 5;
        
        // inhibition radius for non-maximal supression
        // as a percentage of the image width
        public int inhibition_radius_percent = 5;
                
        // buffers        
        protected byte[] left_bmp_mono;
        protected byte[] right_bmp_mono;
        protected List<int>[] left_row_features;
        protected List<int>[] right_row_features;
        protected int[] row_buffer;
		protected int[] row_buffer2;
    
        public StereoVisionSimple()
        {
            algorithm_type = SIMPLE;
            convert_to_mono = true;
        }

		/// <summary>
		/// returns the minimum sum of squared differences for a 3x3
		/// pixel region, similar to Moravec's operator 
		/// </summary>
		/// <see cref="http://www.cim.mcgill.ca/~dparks/CornerDetector/mainMoravec.htm"/>
		/// <param name="index">centre pixel index</param>
		/// <param name="bmp">mono image data</param>
		/// <param name="image_width">width of the image</param>
		/// <param name="gradient_direction">best responding gradient direction.  This helps to improve matching performance</param>
		/// <returns>minimum sum of squared differences</returns>
        protected int minSSD(int index, byte[] bmp, int image_width, ref int gradient_direction)
		{
			int min = int.MaxValue;
			int pixels = bmp.Length;
			int direction = 0;
			
            if ((index > image_width*3) &&
			    (index < pixels - (image_width*3)))
			{			
				// try each direction
				for (int offset_x = -1; offset_x <= 1; offset_x++)
				{
	  			    for (int offset_y = -1; offset_y <= 1; offset_y++)
				    {
						if (!((offset_x==0) && (offset_y==0)))
						{
							// compare to a 3x3 region
							int ssd = 0;
							for (int x = -1; x <= 1; x++)
							{
								int n1 = index + x;
								int n2 = n1 + offset_x + x;
				   			    for (int y = -1; y <= 1; y++)
							    {
									n1 += image_width * y;
							        n2 += image_width * (y + offset_y);
									int diff = bmp[n1] - bmp[n2];
									ssd += diff*diff;
								}
							}
							
							// less is more!
							if (ssd < min)
							{
								min = ssd;
								gradient_direction = direction;
								if (min == 0)
								{
									// abandon ship
									offset_x = 2; 
									offset_y = 2;
								}
							}
						}
						direction++;
					}
				}
			}
			return(min);
		}
		
        /// <summary>
        /// updates sum of squared difference values, based upon luminence image
        /// </summary>
        /// <param name="start_index">starting pixel index for the row</param>
        /// <param name="bmp">luminence image</param>
        /// <param name="SSD">row buffer in which to store the values</param>
        /// <param name="gradient_direction">local gradient direction at each point along the row</param>
        /// <param name="summation_radius">summing radius</param>
        protected void UpdateSSD(int start_index, byte[] bmp,
                                 int[] SSD, int[] gradient_direction,
                                 int summation_radius)
        {
            // clear the buffer
            for (int x = SSD.Length-1; x >= 0; x--) SSD[x] = 0;
        
            // calculate the sum of squared differences for each pixel
            // along the row
            int diff0, diff1, diff2, diff3;
            int n = start_index + SSD.Length - 2 - summation_radius;
            for (int x = SSD.Length - 2 - summation_radius; x > 0; x--,n--)
            {
				int dir = 0;
				int v = minSSD(n, bmp, image_width, ref dir);
				SSD[x] += v;
				SSD[x+1] += v;
				gradient_direction[x] = dir;
            }
        }
		
		
        /// <summary>
        /// performs non-maximal supression on the given row of SSD values
        /// </summary>
        /// <param name="SSD">squared difference values for the row</param>
        /// <param name="inhibition_radius">radius for local competition</param>
        /// <param name="min_response">minimum response value</param>
        protected void NonMaximalSuppression(int[] SSD, int inhibition_radius,
                                             int min_response)
        {
            // perform non-maximal supression            
            for (int x = 0; x < SSD.Length - inhibition_radius; x++)
            {
                //Console.WriteLine(SSD[x]);
                if (SSD[x] < min_response) SSD[x] = 0;
                int v = SSD[x];                
                if (v > 0)
                {
                    for (int r = 1; r < inhibition_radius; r++)
                    {
                        if (SSD[x+r] < v)
                        {
                            SSD[x+r] = 0;
                        }
                        else
                        {
                            SSD[x] = 0;
                            r = inhibition_radius;
                        }
                    }
                }
            }
        }
                        
        /// <summary>
        /// returns a set of features for the given row
        /// </summary>
        /// <param name="start_index">starting pixel index for the row</param>
        /// <param name="bmp_mono">luminence image data</param>
        /// <param name="bmp_colour">hue image data</param>
        /// <param name="SSD">buffer used to store squared differences values</param>
        /// <param name="gradient_direction">local gradient direction at each point along the row</param>
        /// <param name="summation_radius">radius used for summing differences</param>
        /// <param name="inhibition_radius">radius used for non maximal supression</param>
        /// <param name="minimum_response">minimum SSD value</param>
        /// <param name="row_features">returned feature x coordinates and SSD values</param>
        protected void GetRowFeatures(int start_index, byte[] bmp,
                                      int[] SSD, int[] gradient_direction,
                                      int summation_radius,
                                      int inhibition_radius,
                                      int minimum_response,
                                      List<int> row_features)
        {
            row_features.Clear();
            
            UpdateSSD(start_index, bmp, SSD, gradient_direction, summation_radius);

            NonMaximalSuppression(SSD, inhibition_radius, minimum_response);

            // store the features
            for (int x = SSD.Length-1-inhibition_radius; x >= 0; x--)
            {
                if (SSD[x] > 0)
                {                
                    row_features.Add(x);
                    row_features.Add(SSD[x]);
					row_features.Add(gradient_direction[x]);
                }
            }
        }
        
        /// <summary>
        /// returns a measure of the similarity of two points
        /// </summary>
        /// <param name="n1">pixel index in the left image</param>
        /// <param name="n2">pixel index in the right image</param>
        /// <param name="possible_disparity">hypothetical disparity for this match</param>
        /// <returns>
        /// sum of squared differences
        /// </returns>
        protected int similarity(int n1, int n2, 
		                         int possible_disparity,
                                 byte[] left_bmp, byte[] right_bmp)
        {
            int result = 0;            
            int hits = 0;
            int pixels = left_bmp.Length;
            int radius = possible_disparity * compare_radius / 100;
			if (radius < 2) radius = 2;
			
            for (int x = -radius; x <= radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 > -1) && (nn2 > -1) &&
                    (nn1 < pixels) && (nn2 < pixels))
                {
                    int diff = left_bmp[nn1] - right_bmp[nn2];  
                    result += diff * diff;
					hits++;
                }
                else
                {
                    result = int.MaxValue;
                    break;
                }
            }
			if (hits > 0) result /= hits;
            
            return(result);
        }
        
        protected void MatchFeatures(int y, 
                                     List<int> left_row_features, 
                                     List<int> right_row_features,
                                     float calibration_offset_x,
                                     float calibration_offset_y,
                                     byte[] left_bmp, byte[] right_bmp)
        {
            int max_disparity_pixels = image_width * max_disparity / 100;
            
            List<int> candidate_matches = new List<int>();
            int average_similarity = 0;
            int hits = 0;

            for (int i = 0; i < left_row_features.Count; i += 3)
            {
                int x_left = left_row_features[i];
				int direction_left = left_row_features[i + 2];
                int min_variance = int.MaxValue;
                int best_disparity = 0;
                int best_index = -1;
                for (int j = 0; j < right_row_features.Count; j += 3)
                {
                    int x_right = right_row_features[j];

                    int disparity = x_left - x_right + (int)calibration_offset_x;
                    if ((disparity >= 0) && (disparity < max_disparity_pixels))
                    {
                        int direction_right = right_row_features[j + 2];
						if (direction_left == direction_right)
						{						
	                        int n1 = (y * image_width) + x_left;
	                        int n2 = ((y+(int)calibration_offset_y) * image_width) + x_right;
	                        
	                        int v = similarity(n1, n2, disparity, left_bmp, right_bmp);
	                        if ((v < matching_threshold) &&
	                            (v < min_variance))
	                        {
	                            min_variance = v;
	                            best_disparity = disparity;
	                            best_index = j;
	                        }
						}
                    }
                }
                if (best_disparity > 0)
                {
                    candidate_matches.Add(min_variance);
                    candidate_matches.Add(x_left);
                    candidate_matches.Add(best_disparity);                            

                    average_similarity += min_variance;
                    hits++;

                }
            }
            
            if (hits > 0)
            {            
                average_similarity /= hits;
                int threshold = average_similarity * similarity_threshold_percent / 100;
                for (int i = 0; i < candidate_matches.Count; i += 3)
                {
                    if (candidate_matches[i] < threshold)
                    {
                        int x_left = candidate_matches[i+1];
                        int disparity = candidate_matches[i+2];
                        features.Add(new StereoFeature(x_left, y, disparity));
                    }
                }
            }
        }
    
        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right_image_data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        public override void Update(byte[] left_bmp, byte[] right_bmp,
                                    int image_width, int image_height,
                                    float calibration_offset_x, float calibration_offset_y)
        {
            features.Clear();
        
            this.image_width = image_width;
            this.image_height = image_height;            
            int bytes_per_pixel = left_bmp.Length / image_width * image_height;
            
            // convert images to mono
            if (bytes_per_pixel > 1)
            {
                monoImage(left_bmp, image_width, image_height, 1, ref left_bmp_mono);
                monoImage(right_bmp, image_width, image_height, 1, ref right_bmp_mono);
            }
            else
            {
                left_bmp_mono = left_bmp;
                right_bmp_mono = right_bmp;
            }
            
            // create some buffers
            if (left_row_features == null)
            {
                row_buffer = new int[image_width];
				row_buffer2 = new int[image_width];
                left_row_features = new List<int>[image_height / vertical_compression];
                right_row_features = new List<int>[image_height / vertical_compression];
                
                for (int y = 0; y < image_height / vertical_compression; y++)
                {
                    left_row_features[y] = new List<int>();
                    right_row_features[y] = new List<int>();
                }
            }
            
            int inhibition_radius = image_width * inhibition_radius_percent / 100;
            int n = 0;
            for (int y = 0; y < image_height; y+=vertical_compression)
            {
                int yy = y / vertical_compression;
                if ((y > 4) && (y < image_height-5))
				{
	                GetRowFeatures(n, left_bmp_mono, row_buffer, row_buffer2, 
	                               summation_radius, inhibition_radius,
	                               minimum_response,
	                               left_row_features[yy]);
	                
	                GetRowFeatures(n, right_bmp_mono, row_buffer, row_buffer2,
	                               summation_radius, inhibition_radius,
	                               minimum_response,
	                               right_row_features[yy]);

	                // test                
	                //for (int i = 0; i <  left_row_features[yy].Count; i += 2)
	                //    features.Add(new StereoFeature(left_row_features[yy][i], y, 5));
	                //for (int i = 0; i <  right_row_features[yy].Count; i += 2)
	                //    features.Add(new StereoFeature(right_row_features[yy][i], y, 5));
				}          
                n += (image_width * vertical_compression);
            }

            for (int y_left = 0; y_left < left_row_features.Length; y_left++)
            {
                int y_right = y_left + (int)calibration_offset_y;
                if ((y_right > -1) && (y_right < right_row_features.Length))
                {
                    MatchFeatures(y_left*vertical_compression, left_row_features[y_left], right_row_features[y_right],
                                  calibration_offset_x, calibration_offset_y,
                                  left_bmp_mono, right_bmp_mono);
                }
            }

        }

    }
}
