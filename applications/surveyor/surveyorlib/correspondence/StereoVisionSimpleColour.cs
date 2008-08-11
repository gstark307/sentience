/*
    
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
    public class StereoVisionSimpleColour : StereoVision
    {
        // maximum disparity as a percentage of the image width
        public int max_disparity = 30;
        
        // maximum sum of squared differences threshold
        // used when matching left and right images
        public int matching_threshold = 50000;
        
        // radius in pixels to use when evaluating match quality
        public int compare_radius = 5;
        
        // we don't need to sample every row to get
        // usable range data
        public int vertical_compression = 2;
        
        // minimum sum of squared differences value, below which
        // we're just seeing noise
        public int minimum_response = 2000;
        public int minimum_response_colour = 45;
        
        // radius in pixels to use for sum of squared differences
        public int summation_radius = 5;
        public int summation_radius_colour = 15;
        
        // inhibition radius for non-maximal supression
        // as a percentage of the image width
        public int inhibition_radius_percent = 5;
        public int inhibition_radius_colour_percent = 15;

        // buffers
        private byte[] left_bmp_mono;
        private byte[] right_bmp_mono;
        private byte[] left_bmp_colour;
        private byte[] right_bmp_colour;    
        private List<int>[] left_row_features;
        private List<int>[] right_row_features;
        private int[] row_buffer;
    
        public StereoVisionSimpleColour()
        {
            algorithm_type = SIMPLE_COLOUR;
        }
        
        #region "colour texture images"

        /// <summary>
        /// creates an image where the dominant colour of each pixel
        /// is red green or blue
        /// </summary>
        /// <param name="img_colour">colour image</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="img">returned image</param>
        private void ColourTexture(byte[] img_colour, int img_width, int img_height, ref byte[] img)
        {
            int tollerance = 100;
            
            if (img == null)
            {
                img = new byte[img_width * img_height];
            }
            else
            {
                if (img.Length != img_width * img_height)
                    img = new byte[img_width * img_height];
            }
            
            if (img_colour.Length == img_width * img_height * 3)
            {            
                int n1 = 0;
                int n2 = 0;
                for (int y = 0; y < img_height; y++)
                {
                    for (int x = 0; x < img_width; x++)
                    {
                        int b = img_colour[n2];
                        int g = img_colour[n2 + 1];
                        int r = img_colour[n2 + 2];

/*                        
                        int hue = 0;
                        int min = 255;
                        if ((r < g) && (r < b)) min = r;
                        if ((g < r) && (g < b)) min = g;
                        if ((b < g) && (b < r)) min = b;
                        
                        if ((r > g) && (r > b))
                            hue = (60 * (g - b) / (r - min)) % 360; 
                        if ((b > g) && (b > r))
                            hue = (60 * (b - r) / (b - min)) + 120;
                        if ((g > b) && (g > r))
                            hue = (60 * (r - g) / (g - min)) + 240;
                        hue = hue * 255 / 360;
*/                        
                        if ((Math.Abs(r-g) < 20) &&
                            (Math.Abs(r-b) < 20) &&
                            (Math.Abs(b-g) < 20))
                        {
                            // grey
                            img[n1] = 1;
                        }
                        else
                        {
                            if (r*2 > b+g+tollerance)
                            {
                                // red
                                img[n1] = 3;
                            }
                            else
                            {
                                if (b*2 > r+g+tollerance)
                                {
                                    // blue
                                    img[n1] = 4;
                                }
                                else
                                {
                                    if (g*2 > r+b+tollerance)
                                    {
                                        // green
                                        img[n1] = 5;
                                    }
                                    else
                                    {
                                        // other
                                        img[n1] = 4;
                                    }
                                }
                            }
                        }
                                                
                        n1++;
                        n2 += 3;
                    }
                }
            }
        }
                
        #endregion

        #region "sum of squared differences"

        /// <summary>
        /// updates sum of squared difference values, based upon luminence image
        /// </summary>
        /// <param name="start_index">starting pixel index for the row</param>
        /// <param name="bmp">luminence image</param>
        /// <param name="SSD">row buffer in which to store the values</param>
        /// <param name="summation_radius">summing radius</param>
        private void UpdateSSD(int start_index, byte[] bmp,
                               int[] SSD, 
                               int summation_radius)
        {
            // clear the buffer
            for (int x = SSD.Length-1; x >= 0; x--) SSD[x] = 0;
        
            // calculate the sum of squared differences for each pixel
            // along the row
            int diff1, diff2, diff3;
            int n = start_index + SSD.Length - 1 - summation_radius;
            for (int x = SSD.Length - 1 - summation_radius; x >= 0; x--,n--)
            {
                for (int r = summation_radius; r > 0; r--)
                {
                    diff1 = bmp[n] - bmp[n+r];
                    diff1 *= diff1;                    
                    diff2 = bmp[n] - bmp[n+r-image_width];
                    diff2 *= diff2;
                    diff3 = bmp[n] - bmp[n+r+image_width];
                    diff3 *= diff3;
                    int diff = diff1 + diff2 + diff3;
                    SSD[x] += diff;
                    SSD[x+r] += diff;
                }                
            }
        }

        /// <summary>
        /// updates sum of squared difference values, based upon colour texture
        /// </summary>
        /// <param name="start_index">starting pixel index for the row</param>
        /// <param name="bmp_colour">colour texture image</param>
        /// <param name="SSD">row buffer in which to store the values</param>
        /// <param name="summation_radius">summing radius</param>
        private void UpdateSSDcolour(int start_index, byte[] bmp_colour,
                                     int[] SSD, 
                                     int summation_radius)
        {
            // clear the buffer
            for (int x = SSD.Length-1; x >= 0; x--) SSD[x] = 0;
        
            // calculate the sum of squared differences for each pixel
            // along the row
            int diff;
            int n = start_index + SSD.Length - 1 - summation_radius;
            for (int x = SSD.Length - 1 - summation_radius; x >= 0; x--,n--)
            {
                for (int r = summation_radius; r > 0; r--)
                {
                    diff = 0;
                    byte v = bmp_colour[n];
                    if (v != bmp_colour[n+r]) diff++;
                    if (v != bmp_colour[n+r-image_width]) diff++;
                    if (v != bmp_colour[n+r+image_width]) diff++;
                    SSD[x] += diff;
                    SSD[x+r] += diff;
                }                
            }
        }
        
        #endregion

        #region "finding features of interest on an image row"

        /// <summary>
        /// performs non-maximal supression on the given row of SSD values
        /// </summary>
        /// <param name="SSD">squared difference values for the row</param>
        /// <param name="inhibition_radius">radius for local competition</param>
        private void NonMaximalSuppression(int[] SSD, int inhibition_radius,
                                           int min_response)
        {
            // perform non-maximal supression            
            for (int x = 0; x < SSD.Length - inhibition_radius; x++)
            {
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
        /// <param name="colour">whether to use colour or not</param>
        /// <param name="start_index">starting pixel index for the row</param>
        /// <param name="bmp">image data</param>
        /// <param name="SSD">buffer used to store squared differences values</param>
        /// <param name="summation_radius">radius used for summing differences</param>
        /// <param name="inhibition_radius">radius used for non maximal supression</param>
        /// <param name="minimum_response">minimum SSD value</param>
        /// <param name="row_features">returned feature x coordinates and SSD values</param>
        private void GetRowFeatures(bool colour,
                                    int start_index, byte[] bmp,
                                    int[] SSD, 
                                    int summation_radius,
                                    int inhibition_radius,
                                    int min_response,
                                    List<int> row_features)
        {
            row_features.Clear();
            
            if (!colour)
                UpdateSSD(start_index, bmp, SSD, summation_radius);
            else
                UpdateSSDcolour(start_index, bmp, SSD, summation_radius);
            NonMaximalSuppression(SSD, inhibition_radius, min_response);
            
            // store the features
            for (int x = SSD.Length-1-inhibition_radius; x >= 0; x--)
            {
                if (SSD[x] > 0)
                {
                    row_features.Add(x);
                    row_features.Add(SSD[x]);
                }
            }
        }
        
        #endregion
        
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
                               byte[] left_bmp, byte[] right_bmp)
        {
            int result = 0;            
            int hits = 0;
            int pixels = left_bmp.Length;
            int diff1, diff2, diff3;
            
            for (int x = -compare_radius; x <= compare_radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 > -1) && (nn2 > -1) &&
                    (nn1 < pixels) && (nn2 < pixels))
                {
                    diff1 = left_bmp[nn1] - right_bmp[nn2];  
                    result += diff1 * diff1;
                    diff2 = left_bmp[nn1-image_width] - right_bmp[nn2 - image_width];  
                    result += diff2 * diff2;
                    diff3 = left_bmp[nn1+image_width] - right_bmp[nn2 + image_width];  
                    result += diff3 * diff3;
                }
                else
                {
                    result = int.MaxValue;
                    break;
                }
            }
            
            return(result);
        }

        /// <summary>
        /// returns a measure of the colour similarity of two points
        /// </summary>
        /// <param name="n1">pixel index in the left image</param>
        /// <param name="n2">pixel index in the right image</param>
        /// <returns>
        /// sum of squared differences
        /// </returns>
        private int coloursimilarity(int n1, int n2, 
                                     byte[] left_bmp_colour, byte[] right_bmp_colour)
        {
            int result = 0;            
            int hits = 0;
            int pixels = left_bmp_colour.Length;
            int diff;
            
            for (int x = -compare_radius; x <= compare_radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 > -1) && (nn2 > -1) &&
                    (nn1 < pixels) && (nn2 < pixels))
                {
                    diff = 0;
                    if (left_bmp_colour[nn1] != right_bmp_colour[nn2]) diff++;  
                    if (left_bmp_colour[nn1-image_width] != right_bmp_colour[nn2 - image_width]) diff++;  
                    if (left_bmp_colour[nn1+image_width] != right_bmp_colour[nn2 + image_width]) diff++;
                    result += diff * diff;
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
        private void MatchFeatures(int y, 
                                   List<int> left_row_features, 
                                   List<int> right_row_features,
                                   float calibration_offset_x,
                                   float calibration_offset_y,
                                   byte[] left_bmp, byte[] right_bmp,
                                   int threshold_percent)
        {
            int max_disparity_pixels = image_width * max_disparity / 100;
            
            List<int> candidate_matches = new List<int>();
            int average_similarity = 0;
            int hits = 0;

            for (int i = 0; i < left_row_features.Count; i += 2)
            {
                int x_left = left_row_features[i];
                int min_variance = int.MaxValue;
                int best_disparity = 0;
                int best_index = -1;
                for (int j = 0; j < right_row_features.Count; j += 2)
                {
                    int x_right = right_row_features[j];

                    int disparity = x_left - x_right + (int)calibration_offset_x;
                    if ((disparity >= 0) && (disparity < max_disparity_pixels))
                    {
                    
                        int n1 = (y * image_width) + x_left;
                        int n2 = ((y+(int)calibration_offset_y) * image_width) + x_right;
                        
                        int v = similarity(n1, n2, left_bmp, right_bmp);
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
                        int x_left = candidate_matches[i+1];
                        int disparity = candidate_matches[i+2];
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
        private void Match(List<int>[] left_row_features,
                           List<int>[] right_row_features,
                           float calibration_offset_x, float calibration_offset_y,
                           byte[] left_bmp, byte[] right_bmp,
                           int threshold_percent)
        {
            for (int y_left = 0; y_left < left_row_features.Length; y_left++)
            {
                int y_right = y_left + (int)calibration_offset_y;
                if ((y_right > -1) && (y_right < right_row_features.Length))
                {
                    MatchFeatures(y_left*vertical_compression, left_row_features[y_left], right_row_features[y_right],
                                  calibration_offset_x, calibration_offset_y,
                                  left_bmp, right_bmp, threshold_percent);
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
                // get colour texture images
                ColourTexture(left_bmp, image_width, image_height, ref left_bmp_colour);
                ColourTexture(right_bmp, image_width, image_height, ref right_bmp_colour);

                // get luminence images
                monoImage(left_bmp, image_width, image_height, 1, ref left_bmp_mono);
                monoImage(right_bmp, image_width, image_height, 1, ref right_bmp_mono);
                
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
                    row_buffer = new int[image_width];
                    left_row_features = new List<int>[image_height / vertical_compression];
                    right_row_features = new List<int>[image_height / vertical_compression];
                    
                    for (int y = 0; y < image_height / vertical_compression; y++)
                    {
                        left_row_features[y] = new List<int>();
                        right_row_features[y] = new List<int>();
                    }
                }
                
                int inhibition_radius = image_width * inhibition_radius_percent / 100;
                int inhibition_radius_colour = image_width * inhibition_radius_colour_percent / 100;

                int n = image_width * vertical_compression;
                for (int y = vertical_compression; y < image_height-vertical_compression; y+=vertical_compression)
                {
                    int yy = y / vertical_compression;
                                    
                    GetRowFeatures(false, n, left_bmp_mono, row_buffer, 
                                   summation_radius, inhibition_radius,
                                   minimum_response,
                                   left_row_features[yy]);
                    
                    GetRowFeatures(false, n, right_bmp_mono, row_buffer, 
                                   summation_radius, inhibition_radius,
                                   minimum_response,
                                   right_row_features[yy]);

                    // test                               
                    for (int i = 0; i <  left_row_features[yy].Count; i += 2)
                        features.Add(new StereoFeature(left_row_features[yy][i], y, 5));
                    //for (int i = 0; i <  right_row_features[yy].Count; i += 2)
                    //    features.Add(new StereoFeature(right_row_features[yy][i], y, 5));
                                   
                    n += (image_width * vertical_compression);
                }
/*                
                Match(left_row_features, right_row_features,
                      calibration_offset_x, calibration_offset_y,
                      left_bmp_mono, right_bmp_mono, 40);
*/
                n = image_width * vertical_compression;
                for (int y = vertical_compression; y < image_height-vertical_compression; y+=vertical_compression)
                {
                    int yy = y / vertical_compression;
                                    
                    GetRowFeatures(true, n, left_bmp_colour, row_buffer, 
                                   summation_radius_colour, inhibition_radius_colour,
                                   minimum_response_colour,
                                   left_row_features[yy]);
                    
                    GetRowFeatures(true, n, right_bmp_colour, row_buffer, 
                                   summation_radius_colour, inhibition_radius_colour,
                                   minimum_response_colour,
                                   right_row_features[yy]);

                    // test                        
                    //for (int i = 0; i <  left_row_features[yy].Count; i += 2)
                    //    features.Add(new StereoFeature(left_row_features[yy][i], y, 5));
                    //for (int i = 0; i <  right_row_features[yy].Count; i += 2)
                    //    features.Add(new StereoFeature(right_row_features[yy][i], y, 5));
                                   
                    n += (image_width * vertical_compression);
                }

/*
                Match(left_row_features, right_row_features,
                      calibration_offset_x, calibration_offset_y,
                      left_bmp_colour, right_bmp_colour, 50);
*/
            }
            else Console.WriteLine("You must supply colour images");
        }

    }
}
