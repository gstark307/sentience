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
    /// simple stereo correspondence
    /// </summary>
    public class StereoVisionSimple2 : StereoVision
    {
        public int max_disparity = 20;
        public int matching_threshold = 50000;
        public int compare_radius = 5;
        
        public int vertical_compression = 2;
        public int minimum_response = 2000;
        public int summation_radius = 3;
        public int inhibition_radius_percent = 5;
        
    
        private List<int>[] left_row_features;
        private List<int>[] right_row_features;
        private int[] row_buffer;
    
        public StereoVisionSimple2()
        {
            algorithm_type = SIMPLE2;
        }
        
        #region "converting to mono"
        
        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="conversion_type">method for converting to mono</param>
        /// <returns></returns>
        private unsafe byte[] monoImage(byte[] img_colour, int img_width, int img_height,
                                        int conversion_type, byte[] output)
        {
            byte[] mono_image = null;
            if (output == null)
            {
                mono_image = new byte[img_width * img_height];
            }
            else
            {
                if (output.Length == img_width * img_height)
                    mono_image = output;
                else
                    mono_image = new byte[img_width * img_height];
            }

            if (img_colour.Length == mono_image.Length)
            {
                for (int i = 0; i < mono_image.Length; i++)
                    mono_image[i] = img_colour[i];
            }
            else
            {
                int n = 0;
                short tot = 0;
                int luminence = 0;

                //fixed (byte* unsafe_mono_image = mono_image)
                {
                    //fixed (byte* unsafe_img_colour = img_colour)
                    {
                        for (int i = 0; i < img_width * img_height * 3; i += 3)
                        {
                            switch (conversion_type)
                            {
                                case 0: // magnitude
                                    {
                                        tot = 0;
                                        for (int col = 0; col < 3; col++)
                                        {
                                            tot += img_colour[i + col];
                                        }
                                        mono_image[n] = (byte)(tot / 3);
                                        break;
                                    }
                                case 1: // luminance
                                    {
                                        luminence = ((img_colour[i + 2] * 299) +
                                                     (img_colour[i + 1] * 587) +
                                                     (img_colour[i] * 114)) / 1000;
                                        //if (luminence > 255) luminence = 255;
                                        mono_image[n] = (byte)luminence;
                                        break;
                                    }
                            }
                            n++;
                        }
                    }
                }
            }
            return (mono_image);
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <returns></returns>
        private byte[] monoImage(byte[] img_colour, int img_width, int img_height)
        {
            return (monoImage(img_colour, img_width, img_height, 0, null));
        }
        
        
        #endregion

        private void GetRowFeatures(int start_index, byte[] bmp,
                                    int[] SSD, 
                                    int summation_radius,
                                    int inhibition_radius,
                                    int minimum_response,
                                    List<int> row_features)
        {
            row_features.Clear();
            
            // clear the buffer
            for (int x = SSD.Length-1; x >= 0; x--) SSD[x] = 0;
        
            // calculate the sum of squared differences for each pixel
            // along the row
            int n = start_index + SSD.Length - 1 - summation_radius;
            for (int x = SSD.Length - 1 - summation_radius; x >= 0; x--,n--)
            {
                for (int r = summation_radius; r > 0; r--)
                {
                    int diff = bmp[n] - bmp[n+r];
                    diff *= diff;
                    SSD[x] += diff;
                    SSD[x+r] += diff;
                }                
            }
            
            // perform non-maximal supression            
            for (int x = 0; x < SSD.Length - inhibition_radius; x++)
            {
                if (SSD[x] < minimum_response) SSD[x] = 0;
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

            for (int x = -compare_radius; x <= compare_radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 > -1) && (nn2 > -1) &&
                    (nn1 < pixels) && (nn2 < pixels))
                {
                    int diff = left_bmp[nn1] - right_bmp[nn2];  
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
        
        private void MatchFeatures(int y, 
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
                int threshold = average_similarity * 80 / 100;
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
                left_bmp = monoImage(left_bmp, image_width, image_height);
                right_bmp = monoImage(right_bmp, image_width, image_height);
            }
            
            // create some buffers
            if (left_row_features == null)
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
            int n = 0;
            for (int y = 0; y < image_height; y+=vertical_compression)
            {
                int yy = y / vertical_compression;
                                
                GetRowFeatures(n, left_bmp, row_buffer, 
                               summation_radius, inhibition_radius,
                               minimum_response,
                               left_row_features[yy]);
                
                GetRowFeatures(n, right_bmp, row_buffer, 
                               summation_radius, inhibition_radius,
                               minimum_response,
                               right_row_features[yy]);

                // test                               
                //for (int i = 0; i <  left_row_features[yy].Count; i += 2)
                //    features.Add(new StereoFeature(left_row_features[yy][i], y, 5));
                //for (int i = 0; i <  right_row_features[yy].Count; i += 2)
                //    features.Add(new StereoFeature(right_row_features[yy][i], y, 5));
                               
                n += (image_width * vertical_compression);
            }

            for (int y_left = 0; y_left < left_row_features.Length; y_left++)
            {
                int y_right = y_left + (int)calibration_offset_y;
                if ((y_right > -1) && (y_right < right_row_features.Length))
                {
                    MatchFeatures(y_left*vertical_compression, left_row_features[y_left], right_row_features[y_right],
                                  calibration_offset_x, calibration_offset_y,
                                  left_bmp, right_bmp);
                }
            }

        }

    }
}
