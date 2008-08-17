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
    /// simple stereo correspondence which uses both luminence and colour
    /// </summary>
    public class StereoVisionSimpleColour : StereoVisionSimple
    {
        public int minimum_response_colour = 45;
        
        // inhibition radius for non-maximal supression
        // as a percentage of the image width
        public int inhibition_radius_colour_percent = 2;

        // buffers
        private byte[] left_bmp_colour;
        private byte[] right_bmp_colour;    
    
        public StereoVisionSimpleColour()
        {
            algorithm_type = SIMPLE_COLOUR;
            matching_threshold *= 4;
			convert_to_mono = false;
			similarity_threshold_percent = 70;
        }
        
        
        #region "measures of similarity"
        
        /// <summary>
        /// returns a measure of the similarity of two points
        /// </summary>
        /// <param name="n1">pixel index in the left image</param>
        /// <param name="n2">pixel index in the right image</param>
        /// <returns>
        /// sum of squared differences
        /// </returns>
        private int similarity(int n1, int n2, 
                               byte[] left_bmp, byte[] right_bmp,
                               byte[] left_bmp_colour, byte[] right_bmp_colour)
        {
            int result = 0;            
            int hits = 0;
            int pixels = left_bmp.Length;
            int diff1, diff2, diff3, d0, d1;
            int max = pixels - image_width;
            
            for (int x = -compare_radius; x <= compare_radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 >= image_width) && (nn2 >= image_width) &&
                    (nn1 < max) && (nn2 < max))
                {
                    d0 = 0;
                    d1 = 0;

                    diff1 = left_bmp[nn1] - right_bmp[nn2];  
                    d0 += diff1 * diff1;                    
                    diff2 = left_bmp[nn1-image_width] - right_bmp[nn2 - image_width];  
                    d0 += diff2 * diff2;
                    diff3 = left_bmp[nn1+image_width] - right_bmp[nn2 + image_width];
                    d0 += diff3 * diff3;

                    diff2 = left_bmp_colour[nn1-image_width] - right_bmp_colour[nn2 - image_width];
                    d1 += diff2 * diff2;
                    diff1 = left_bmp_colour[nn1] - right_bmp_colour[nn2];                      
                    d1 += diff1 * diff1;
                    diff3 = left_bmp_colour[nn1+image_width] - right_bmp_colour[nn2 + image_width];
                    d1 += diff3 * diff3;
                    
                    result += (int)((d0 * 3 + d1) * 0.25f);
                }
                else
                {
                    result = int.MaxValue;
                    break;
                }
            }
            
            return(result);
        }

        
        #endregion
        
        #region "matching images"

        /// <summary>
        /// match features along a row between the left and right images
        /// </summary>
        /// <param name="y">current row y coordinate</param>
        /// <param name="left_row_features">features on the left image row</param>
        /// <param name="right_row_features">features on the right image row</param>
        /// <param name="calibration_offset_x">x offset of the right image relative to the left</param>
        /// <param name="calibration_offset_y">y offset of the right image relative to the left</param>
        /// <param name="left_bmp">left image</param>
        /// <param name="right_bmp">right image</param>
        /// <param name="threshold_percent">threshold used to remove probably bad matches, in gthe range 0-100</param>        
        protected void MatchFeatures(int y, 
                                     List<float> left_row_features, 
                                     List<float> right_row_features,
                                     float calibration_offset_x,
                                     float calibration_offset_y,
                                     byte[] left_bmp, byte[] right_bmp,
                                     byte[] left_bmp_colour, byte[] right_bmp_colour,
                                     int threshold_percent)
        {
            int max_disparity_pixels = image_width * max_disparity / 100;
            
            List<float> candidate_matches = new List<float>();
            int average_similarity = 0;
            int hits = 0;

            for (int i = 0; i < left_row_features.Count; i += 2)
            {
                float x_left = left_row_features[i];
                int min_variance = int.MaxValue;
                float best_disparity = 0;
                int best_index = -1;
                for (int j = 0; j < right_row_features.Count; j += 2)
                {
                    float x_right = right_row_features[j];

                    float disparity = x_left - x_right + (int)calibration_offset_x;
                    if ((disparity >= 0) && (disparity < max_disparity_pixels))
                    {
                    
                        int n1 = (y * image_width) + (int)x_left;
                        int n2 = ((y+(int)calibration_offset_y) * image_width) + (int)x_right;
                        
                        int v = similarity(n1, n2, left_bmp, right_bmp,
                                           left_bmp_colour, right_bmp_colour);
                        if ((v < matching_threshold) &&
                            (v < min_variance))
                        {
                            min_variance = v;
                            best_disparity = disparity;
                            best_index = j;
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
                int threshold = average_similarity * threshold_percent / 100;
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
        /// matches the rows of the left and right images
        /// </summary>
        /// <param name="left_row_features">features on the left image row</param>
        /// <param name="right_row_features">features on the right image row</param>
        /// <param name="calibration_offset_x">x offset of the right image relative to the left</param>
        /// <param name="calibration_offset_y">y offset of the right image relative to the left</param>
        /// <param name="left_bmp">left image</param>
        /// <param name="right_bmp">right image</param>
        /// <param name="threshold_percent">threshold used to remove probably bad matches, in gthe range 0-100</param>        
        private void Match(List<float>[] left_row_features,
                           List<float>[] right_row_features,
                           float calibration_offset_x, float calibration_offset_y,
                           byte[] left_bmp, byte[] right_bmp,
                           byte[] left_bmp_colour, byte[] right_bmp_colour,
                           int threshold_percent)
        {
            for (int y_left = 0; y_left < left_row_features.Length; y_left++)
            {
                int y_right = y_left + (int)calibration_offset_y;
                if ((y_right > -1) && (y_right < right_row_features.Length))
                {
                    MatchFeatures(y_left*vertical_compression, left_row_features[y_left], right_row_features[y_right],
                                  calibration_offset_x, calibration_offset_y,
                                  left_bmp, right_bmp,
                                  left_bmp_colour, right_bmp_colour,
                                  threshold_percent);
                }
            }
        }
        
        #endregion
    
        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp">rectified left colour image data</param>
        /// <param name="right_bmp">rectified right colour image_data</param>
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
            
            if (left_bmp.Length == image_width * image_height * 3)
            {            
                // get hue images
                monoImage(left_bmp, image_width, image_height, 2, ref left_bmp_colour);
                monoImage(right_bmp, image_width, image_height, 2, ref right_bmp_colour);

				// create mono image buffers
                if (left_bmp_mono == null)
				{
					left_bmp_mono = new byte[2][];
					right_bmp_mono = new byte[2][];
			    }
				
                // get luminence images
                monoImage(left_bmp, image_width, image_height, 1, ref left_bmp_mono[0]);
                monoImage(right_bmp, image_width, image_height, 1, ref right_bmp_mono[0]);
                
                // create some buffers
                bool create_buffers = false;
                if (left_row_features == null)
                    create_buffers = true;
                else
                {
                    if (left_row_features.Length != image_height / vertical_compression)
                        create_buffers = true;
                }
                if (create_buffers)
                {
				    row_buffer = new int[4][];
                    row_buffer[0] = new int[image_width];
				    row_buffer[1] = new int[image_width];
				    row_buffer[2] = new int[image_width/2];
				    row_buffer[3] = new int[image_width/2];
				    row_buffer[4] = new int[image_width];
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
                int inhibition_radius_colour = image_width * inhibition_radius_colour_percent / 100;

                int n = image_width * vertical_compression;
                for (int y = vertical_compression; y < image_height-vertical_compression; y+=vertical_compression)
                {
					int n2 = (y/2) * (image_width/2);
                    int yy = y / vertical_compression;

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
                                   
                    n += (image_width * vertical_compression);
                }

                Match(left_row_features, right_row_features,
                      calibration_offset_x, calibration_offset_y,
                      left_bmp_mono[0], right_bmp_mono[0],
                      left_bmp_colour, right_bmp_colour,
                      similarity_threshold_percent);

            }
            else Console.WriteLine("You must supply colour images");
        }

    }
}
