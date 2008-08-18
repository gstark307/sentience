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
        public int matching_threshold = 90000000;
        
        // radius to use when evaluating match quality
		// this is a percentage of the possible disparity value
		// so that bold claims (big disparities) require bold evidence
        public int compare_radius = 100;
        
        // we don't need to sample every row to get
        // usable range data.  Alternate rows will do.
        protected const int vertical_compression = 2;
        
        // absolute minimum sum of squared differences value, below which
        // we're just seeing noise
        public int minimum_response = 2000;
        
        // inhibition radius for non-maximal supression
        // along each row as a percentage of the image width
        public int inhibition_radius_percent = 3;
        
        protected const int ideal_number_of_features = 1000;
                
        // luminence images at two scales (full size and half size)      
        protected byte[][] left_bmp_mono;
        protected byte[][] right_bmp_mono;
		
		// features detected on each image row
        protected List<float>[] left_row_features;
        protected List<float>[] right_row_features;
		
		// temporary buffers used for sum of squared differences
        protected int[][] row_buffer;
    
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
        protected int minSSD(int index, 
		                     byte[] bmp, int image_width, 
		                     ref int gradient_direction)
		{
			int min = int.MaxValue;
			int pixels = bmp.Length;
			int direction = 0;
			
            if ((index > image_width*4) &&
			    (index < pixels - (image_width*4)))
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
        protected void UpdateSSD(int start_index, byte[] bmp, int image_width,
                                 int[] SSD, int[] gradient_direction)
        {
            // clear the buffer
            for (int x = SSD.Length-1; x >= 0; x--) SSD[x] = 0;
        
            // calculate the sum of squared differences for each pixel
            // along the row
            int diff0, diff1, diff2, diff3;
            int n = start_index + SSD.Length - 2;
            for (int x = SSD.Length - 2; x > 0; x--,n--)
            {
				int dir = 0;
				int v = minSSD(n, bmp, image_width, ref dir);
				dir = 0;
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
        /// <param name="inhibition_radius">radius used for non maximal supression</param>
        /// <param name="minimum_response">minimum SSD value</param>
        /// <param name="row_features">returned feature x coordinates and SSD values</param>
        protected void GetRowFeatures(int start_index, byte[] bmp, 
		                              int start_index2, byte[] bmp2,
                                      int[] SSD, int[] gradient_direction,
		                              int[] SSD2, int[] gradient_direction2,
		                              int[] buffer,
                                      int inhibition_radius,
                                      int minimum_response,
                                      List<float> row_features)
        {
            row_features.Clear();
            
            UpdateSSD(start_index, bmp, image_width, SSD, gradient_direction);
            UpdateSSD(start_index2, bmp2, image_width/2, SSD2, gradient_direction2);

			// combine results on multiple scales
			int x2 = 0;
            for (int x = 0; x < SSD2.Length; x++, x2 += 2)
			{
				int v = SSD2[x];
		        SSD[x2] += v;
				SSD[x2+1] += v;
			}
			
			//Buffer.BlockCopy(SSD, 0, buffer, 0, SSD.Length);
			for (int i = SSD.Length-1; i >= 0; i--)
			    buffer[i] = SSD[i];
			
			NonMaximalSuppression(SSD, inhibition_radius, minimum_response);
			
            // store the features
            for (int x = SSD.Length-1-inhibition_radius; x > 0; x--)
            {
                if (SSD[x] > 0)
                {
                    float weight = 0;
                    float tot = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (buffer[x+dx] > 0)
                        {
                            float w = 1.0f / buffer[x+dx];
                            weight += w;
                            tot += w * (x+dx);
                        }
                    }
                    
                    if (weight > 0)
                    {
                        float interpolated_x = tot / weight; 
                        row_features.Add(interpolated_x);
                        row_features.Add(SSD[x]);
    					row_features.Add(gradient_direction[x]);
					}
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
            int pixels = left_bmp.Length - image_width;
            int horizontal_radius = possible_disparity * compare_radius / 100;
			if (horizontal_radius < 2) horizontal_radius = 2;			

			// calculate an average pixel intensity value
			// for the left and right search areas
			int left_average_intensity = 0;
			int right_average_intensity = 0;				
            for (int x = -horizontal_radius; x <= horizontal_radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 > image_width) && (nn2 > image_width) &&
                    (nn1 < pixels) && (nn2 < pixels))
                {
                    left_average_intensity += left_bmp[nn1];
					right_average_intensity += right_bmp[nn2];  
					hits++;
                }
                else
                {
                    result = int.MaxValue;
                    break;
                }
            }
			
			// compare pixels in the left and right search regions and calculate
			// a sum of squared differences
			// note that we also compensate for difference in average pixel intensity
			if ((hits > 0) && (result < int.MaxValue))
			{
				left_average_intensity /= hits;
				right_average_intensity /= hits;
				int relative_intensity_difference = right_average_intensity - left_average_intensity;
				
			    hits = 0;
			
	            for (int x = -horizontal_radius; x <= horizontal_radius; x++)
	            {
	                int nn1 = n1 + x;
	                int nn2 = n2 + x;
	                if ((nn1 > image_width) && (nn2 > image_width) &&
	                    (nn1 < pixels) && (nn2 < pixels))
	                {
	                    int diff = left_bmp[nn1] - (right_bmp[nn2] - relative_intensity_difference);  
	                    result += diff * diff;
	                    diff = left_bmp[nn1-image_width] - (right_bmp[nn2-image_width] - relative_intensity_difference);  
	                    result += diff * diff;
	                    diff = left_bmp[nn1+image_width] - (right_bmp[nn2+image_width] - relative_intensity_difference);  
	                    result += diff * diff;
						hits++;
	                }
	                else
	                {
	                    result = int.MaxValue;
	                    break;
	                }
	            }
			}
			
			if (hits > 0) result /= hits;
            
            return(result);
        }
        
        protected void MatchFeatures(int y, 
                                     List<float> left_row_features, 
                                     List<float> right_row_features,
                                     float calibration_offset_x,
                                     float calibration_offset_y,
                                     byte[] left_bmp, byte[] right_bmp)
        {
            int max_disparity_pixels = image_width * max_disparity / 100;
            
            List<float> candidate_matches = new List<float>();
            int average_similarity = 0;
            int hits = 0;

            for (int i = 0; i < left_row_features.Count; i += 3)
            {
                float x_left = left_row_features[i];
				int direction_left = (int)left_row_features[i + 2];
                int min_variance = int.MaxValue;
                float best_disparity = 0;
                int best_index = -1;
                for (int j = 0; j < right_row_features.Count; j += 3)
                {
                    float x_right = right_row_features[j];

                    float disparity = x_left - x_right + (int)calibration_offset_x;
                    if ((disparity >= 0) && (disparity < max_disparity_pixels))
                    {
                        int direction_right = (int)right_row_features[j + 2];
						if (direction_left == direction_right)
						{						
	                        int n1 = (y * image_width) + (int)x_left;
	                        int n2 = ((y+(int)calibration_offset_y) * image_width) + (int)x_right;
	                        
	                        int v = similarity(n1, n2, (int)disparity, left_bmp, right_bmp);
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
                        float x_left = candidate_matches[i+1];
                        float disparity = candidate_matches[i+2];
                        features.Add(new StereoFeature(x_left, y, disparity));
                    }
                }
            }
        }

		/// <summary>
		/// downsamples the given image to half its original size
		/// </summary>
		/// <param name="img">mono image data</param>
		/// <param name="img_width">width of the image</param>
		/// <param name="img_height">height of the image</param>
		/// <param name="downsampled">downsampled image</param>
        protected void DownSample(byte[] img, int img_width, int img_height,
		                          ref byte[] downsampled)
		{
			int width = img_width / 2;
			int height = img_height / 2;
			if (downsampled == null) downsampled = new byte[width * height];
			int n1 = 0;
			for (int y = 0; y < img_height; y += 2)
			{
				int n2 = y * img_width;
				for (int x = 0; x < img_width; x += 2)
				{
					float intensity = 
						(img[n2] + img[n2+1] + 
					     img[n2 + img_width] + img[n2 + img_width + 1]) * 0.25f;
					downsampled[n1] = (byte)intensity;
					n1++;
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
			UpdateSimple(left_bmp, right_bmp,
			             image_width, image_height,
			             calibration_offset_x, calibration_offset_y);
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
        protected void UpdateSimple(byte[] left_bmp, byte[] right_bmp,
                                    int image_width, int image_height,
                                    float calibration_offset_x, float calibration_offset_y)
        {
            features.Clear();
        
            this.image_width = image_width;
            this.image_height = image_height;            
            int bytes_per_pixel = left_bmp.Length / image_width * image_height;
            
			// create mono image buffers
            if (left_bmp_mono == null)
			{
				left_bmp_mono = new byte[2][];
				right_bmp_mono = new byte[2][];
		    }			
			
            // convert images to mono
            if (bytes_per_pixel > 1)
            {
                monoImage(left_bmp, image_width, image_height, 1, ref left_bmp_mono[0]);
                monoImage(right_bmp, image_width, image_height, 1, ref right_bmp_mono[0]);
            }
            else
            {
                left_bmp_mono[0] = left_bmp;
                right_bmp_mono[0] = right_bmp;
            }
            
            // create some buffers
            if (left_row_features == null)
            {
				row_buffer = new int[5][];
                row_buffer[0] = new int[image_width];
				row_buffer[1] = new int[image_width];
				row_buffer[2] = new int[image_width/2];
				row_buffer[3] = new int[image_width/2];
				row_buffer[4] = new int[image_width];
				left_bmp_mono[1] = null;
				right_bmp_mono[1] = null;
                left_row_features = new List<float>[image_height / vertical_compression];
                right_row_features = new List<float>[image_height / vertical_compression];
                
                for (int y = 0; y < image_height / vertical_compression; y++)
                {
                    left_row_features[y] = new List<float>();
                    right_row_features[y] = new List<float>();
                }
            }
			
			// downsample the images to half their original size
			DownSample(left_bmp_mono[0], image_width, image_height, ref left_bmp_mono[1]);
			DownSample(right_bmp_mono[0], image_width, image_height, ref right_bmp_mono[1]);
            
            int inhibition_radius = image_width * inhibition_radius_percent / 100;
            int n = 0;
            for (int y = 0; y < image_height; y+=vertical_compression)
            {
                int yy = y / vertical_compression;
                if ((y > 4) && (y < image_height-5))
				{
					int n2 = (y/2) * (image_width/2);
	                GetRowFeatures(n, left_bmp_mono[0], 
					               n2, left_bmp_mono[1], 
					               row_buffer[0], row_buffer[1], 
					               row_buffer[2], row_buffer[3],
					               row_buffer[4],
	                               inhibition_radius,
	                               minimum_response,
	                               left_row_features[yy]);
	                
	                GetRowFeatures(n, right_bmp_mono[0], 
					               n2, right_bmp_mono[1], 
					               row_buffer[0], row_buffer[1],
					               row_buffer[2], row_buffer[3],
					               row_buffer[4],
	                               inhibition_radius,
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
                                  left_bmp_mono[0], right_bmp_mono[0]);
                }
            }
            
            // adaptation of the minimum response to try
            // to maintain a reasonable number of stereo features
            if ((features.Count < ideal_number_of_features * 90 / 100) &&
                (minimum_response > 700))
                minimum_response -= 50;
            if (features.Count > ideal_number_of_features * 110 / 100)
                minimum_response += 10;
            

        }

    }
}
