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
using System.Collections;
using sluggish.utilities;
using CenterSpace.Free;

namespace sentience.core
{
    public class sentience_stereo_contours
    {
        MersenneTwister rnd = new MersenneTwister(100);

        public bool useSmoothing = false;

        // roll angle of the camera in radians
        public float roll = 0;

        // max number of features per row
        private const int MAX_POINT_FEATURES = 6000;
        
        // types of pattern
        public const int PATTERN_CENTRE_SURROUND = 0;
        public const int PATTERN_LEFT_RIGHT = 1;

        /// <remarks> 
        /// The number of features required
        /// this can be between 0 and MAX_POINT_FEATURES
        /// The number of features actually returned is given in no_of_selected_features
        /// and may be lower than the required number if there are few features
        /// within the images (eg looking at an area with large blank spaces)
        /// </remarks>
        public int required_features;

        ///a fixed quantity (required_features) of selected point features
        public int no_of_selected_features;
        public float[] selected_features;

        /// <remarks>
        /// max disparity as a percentage of image width
        /// in the range 1-100
        /// </remarks>
        public int max_disparity;

        ///attention map
        public bool[,] attention_map;

        ///disparity map
        public float[,] disparity_map;

        public float[,] disparity_hits;

        public int[,] scale_width;

        // step size used to speed up blob detection
        public int step_size = 1;

        // the number of scales on which stereo matches will be searched for
        const int no_of_scales = 2;

        // to speed things up the original images are sub-sampled, typically
        // by removing alternate rows
        public int vertical_compression = 4;
        public int disparity_map_compression = 3;

        // previous compression values.  We store these so that any changes
        // at runtime can be detected
        private int prev_vertical_compression;
        private int prev_disparity_map_compression;

        // maximum difference below which matching is possible
        public float match_threshold = 10.0f;

        // radius of the surround for blob detection
        public float surround_radius_percent = 1.8f;

        private classimage img_left, img_right;

        // blob feature responses
        private float[,] wavepoints_left = null;
        private float[,] wavepoints_right = null;

        // blob feature scales
        private byte[,] wavepoints_left_scale = null;
        private byte[,] wavepoints_right_scale = null;

        // blob feature patterns
        private byte[,] wavepoints_left_pattern = null;
        private byte[,] wavepoints_right_pattern = null;

        int[,] scalepoints_left;
        int[,] scalepoints_right;
        int[,,] scalepoints_lookup;

        Byte[] left_image = null;

        // gaussian function lookup table
        const int gaussian_lookup_levels = 1000;
        float[] gaussian_lookup;

        #region "constructors"

        /// <summary>
        /// constructor
        /// This sets some initial default values.
        /// </summary>
        public sentience_stereo_contours()
        {
            ///array storing a fixed quantity of selected features
            no_of_selected_features = 0;
            selected_features = new float[MAX_POINT_FEATURES * 3];

            required_features = MAX_POINT_FEATURES; ///return all detected features by default

            /// maximum disparity 10% of image width
            max_disparity = 10;
        }

        #endregion

        #region "initialisation"

        /// <summary>
        /// clear the attention map
        /// </summary>
        /// <param name="wdth">width of the attention map</param>
        /// <param name="hght">height of the attention map</param>
        public void resetAttention(int wdth, int hght)
        {
            for (int xx = 0; xx < wdth; xx++)
                for (int yy = 0; yy < hght; yy++)
                    attention_map[xx, yy] = true;
        }

        /// <summary>
        /// clear the disparity map
        /// </summary>
        /// <param name="map_wdth">width of the disparity map</param>
        /// <param name="map_hght">height of the disparity map</param>
        public void clearDisparityMap(int map_wdth, int map_hght)
        {
            for (int x = 0; x < map_wdth; x++)
                for (int y = 0; y < map_hght; y++)
                {
                    disparity_map[x, y] = -1;
                    disparity_hits[x, y] = 0;
                }
        }

        #endregion

        #region "filtering/smoothing"

        /// <summary>
        /// elmimnate unlikely disparity values
        /// </summary>
        /// <param name="histogram_levels">number of disparity histogram levels</param>
        /// <param name="threshold">threshold in the range 0-100</param>
        private void filterDisparityHistogram(int histogram_levels,
                                              int threshold)
        {
            // get the dimensions of the depth map
            int map_wdth = disparity_map.GetLength(0);
            int map_hght = disparity_map.GetLength(1);

            float[] histogram = new float[histogram_levels];

            int half_levels = histogram_levels / 2;
        
            // filter horizontally
            int[] bucket = new int[map_wdth];
            for (int y = 2; y < map_hght-2; y++)
            {
                // clear histogram values
                float max_response = 0;
                for (int h = 0; h < histogram_levels; h++) histogram[h] = 0;
                
                // for all disparities on this row
                for (int x = 0; x < map_wdth; x++)
                {
                    // which histogram bucket should this go into
                    int b = half_levels + (int)(disparity_map[x, y] * half_levels / max_disparity);
                    if (b >= histogram_levels) b = histogram_levels - 1;
                    if (b < 0) b = 0;                    
                    
                    // update histogram
                    histogram[b]++;
                    if (histogram[b] > max_response) max_response = histogram[b];
                    
                    bucket[x] = b;
                }
                
                // calculate minimum threshold
                float minimum_response = max_response * threshold / 100;
                
                // eliminate disparities below the minimum
                for (int x = 0; x < map_wdth; x++)
                {
                    if (histogram[bucket[x]] < minimum_response)
                        disparity_map[x, y] = 0;
                }
            }

            // filter vertically
            bucket = new int[map_hght];
            for (int x = 2; x < map_wdth-2; x++)
            {
                // clear histogram values
                float max_response = 0;
                for (int h = 0; h < histogram_levels; h++) histogram[h] = 0;
                
                // for all disparities on this column
                for (int y = 0; y < map_hght; y++)
                {
                    // which histogram bucket should this go into
                    int b = half_levels + (int)(disparity_map[x, y] * half_levels / max_disparity);
                    if (b >= histogram_levels) b = histogram_levels - 1;
                    if (b < 0) b = 0;                    
                    
                    // update histogram
                    histogram[b]++;
                    if (histogram[b] > max_response) max_response = histogram[b];
                    
                    bucket[y] = b;
                }
                
                // calculate minimum threshold
                float minimum_response = max_response * threshold / 100;
                
                // eliminate disparities below the minimum
                for (int y = 0; y < map_hght; y++)
                {
                    if (histogram[bucket[y]] < minimum_response)
                        disparity_map[x, y] = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map_wdth">width of the disparity map</param>
        /// <param name="map_hght">height of the disparity map</param>
        /// <param name="threshold"></param>
        private void filterDisparityMap(int map_wdth, int map_hght, int threshold)
        {
            float diff1, diff2;

            for (int y = 2; y < map_hght-1; y++)
            {
                float prev_value = 0;
                float value = disparity_map[0, y] * 255 / max_disparity;
                for (int x = 0; x < map_wdth - 1; x++)
                {
                    float next_value = disparity_map[x + 1, y] * 255 / max_disparity;
                    if ((value > 200) && (x > 0))
                    {
                        diff1 = value - prev_value;
                        if (diff1 > threshold)
                        {
                            diff2 = value - next_value;
                            if (diff2 > threshold)
                            {
                                if ((next_value > 0) && (prev_value > 0))
                                    disparity_map[x, y] = (disparity_map[x + 1, y] + disparity_map[x - 1, y]) / 2;
                                else
                                {
                                    if (next_value > 0)
                                        disparity_map[x, y] = disparity_map[x + 1, y];
                                    else
                                        if (prev_value > 0)
                                            disparity_map[x, y] = disparity_map[x - 1, y];
                                        else
                                            disparity_map[x, y] = 0;
                                }
                            }
                        }

                        float above_value = disparity_map[x, y - 2] * 255 / max_disparity;
                        float curr_value = disparity_map[x, y - 1] * 255 / max_disparity;
                        diff1 = curr_value - above_value;
                        if (diff1 > threshold)
                        {
                            float below_value = disparity_map[x, y] * 255 / max_disparity;
                            diff2 = curr_value - below_value;
                            if (diff2 > threshold)
                            {
                                if ((above_value > 0) && (below_value > 0))
                                    disparity_map[x, y - 1] = (disparity_map[x, y - 2] + disparity_map[x, y]) / 2;
                                else
                                {
                                    if (above_value > 0)
                                        disparity_map[x, y - 1] = disparity_map[x, y - 2];
                                    else
                                        if (below_value > 0)
                                            disparity_map[x, y - 1] = disparity_map[x, y];
                                        else
                                            disparity_map[x, y - 1] = 0;
                                }
                            }
                        }
                    }
                    prev_value = value;
                    value = next_value;
                }
            }
        }

        /// <summary>
        /// perform smoothing on the disparity values when the stereo camera is rolled
        /// at a 45 degree angle
        /// </summary>
        /// <param name="map_wdth">width of the disparity map</param>
        /// <param name="map_hght">height of the disparity map</param>
        /// <param name="slant_direction">is the camera rolled to the left or right</param>
        public void smoothDisparityMapSlanted(int map_wdth, int map_hght, 
                                              int slant_direction)
        {
            float tollerance = 1.0f * step_size;
            //int search_y = (int)(map_hght * (max_disparity - value) / (5 * max_disparity));
            int search_y = map_hght / 5;
            if (search_y < 1) search_y = 1;

            float[,] new_disparity_map = new float[map_wdth, map_hght];
            for (int x = 2; x < map_wdth - 2; x++)
                for (int y = 0; y < map_hght; y++)
                {
                    float value = disparity_map[x, y];
                    if ((value > 0))
                    //&& (value * 255 / max_disparity < 150))
                    {
                        float tot = 0;
                        int hits = 0;
                        for (int xx = x - 2; xx < x + 2; xx++)
                        {
                            for (int yy = y - search_y; yy < y + search_y; yy++)
                            {
                                if ((yy > -1) && (yy < map_hght))
                                {
                                    int xxx = xx + (slant_direction * (yy - y));
                                    if ((xxx > -1) && (xxx < map_wdth))
                                    {
                                        float value2 = disparity_map[xxx, yy];
                                        if (value2 > 0)
                                        {
                                            float diff = value2 - value;
                                            if (diff < 0) diff = -diff;
                                            if (diff < tollerance)
                                            {
                                                tot += value2;
                                                hits++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        new_disparity_map[x, y] = tot / hits;
                    }
                    else new_disparity_map[x, y] = disparity_map[x, y];
                }
            disparity_map = new_disparity_map;
        }


        /// <summary>
        /// smooths the disparity map when the stereo camera is at a normal zero degrees roll angle
        /// </summary>
        /// <param name="map_wdth">width of the disparity map</param>
        /// <param name="map_hght">height of the disparity map</param>
        public void smoothDisparityMap(int map_wdth, int map_hght)
        {
            float tollerance = 1.0f * step_size;
            //int search_y = (int)(map_hght * (max_disparity - value) / (5 * max_disparity));
            int search_y = map_hght / 5;
            if (search_y < 1) search_y = 1;

            float[,] new_disparity_map = new float[map_wdth, map_hght];
            for (int x = 2; x < map_wdth-2; x++)
                for (int y = 0; y < map_hght; y++)
                {
                    float value = disparity_map[x, y];
                    if ((value > 0))                        
                        //&& (value * 255 / max_disparity < 150))
                    {
                        float tot = 0;
                        int hits = 0;
                        for (int xx = x - 2; xx < x + 2; xx++)
                        {
                            for (int yy = y - search_y; yy < y + search_y; yy++)
                            {
                                if ((yy > -1) && (yy < map_hght))
                                {
                                    float value2 = disparity_map[xx, yy];
                                    if (value2 > 0)
                                    {
                                        float diff = value2 - value;
                                        if (diff < 0) diff = -diff;
                                        if (diff < tollerance)
                                        {
                                            tot += value2;
                                            hits++;
                                        }
                                    }
                                }
                            }
                        }
                        new_disparity_map[x, y] = tot / hits;
                    }
                    else new_disparity_map[x, y] = disparity_map[x, y];
                }
            disparity_map = new_disparity_map;
        }

        #endregion

        #region "updating the disparity map"

        // distance lokup table used to avoid doing square roots
        float[,] distance_lookup;

        /// <summary>
        /// update the disparity map at the given point using the given scale
        /// </summary>
        /// <param name="x">x coordinate at which to insert the disparity data</param>
        /// <param name="y">y coordinate at which to insert the disparity data</param>
        /// <param name="map_wdth">width of the disparity map</param>
        /// <param name="map_hght">height of the disparity map</param>
        /// <param name="scale">an index number corresponding to the scale upon which data should be inserted into the disparity map</param>
        /// <param name="disparity_value">the disparity value to be added to the map</param>
        /// <param name="confidence">how confident are we about this disparity value?</param>
        private void updateDisparityMap(int x, int y, 
                                        int map_wdth, int map_hght, 
                                        int scale, 
                                        float disparity_value, 
                                        float confidence)
        {
            const int lookup_table_size = 50;
            const int half_lookup_table_size = lookup_table_size / 2;
            
            // create a lookup table for distances, which
            // will help us to avoid having to do a lot of
            // square root calculations
            if (distance_lookup == null)
            {                
                distance_lookup = new float[lookup_table_size, lookup_table_size];                
                for (int x1 = 0; x1 < distance_lookup.GetLength(0); x1++)
                {
                    int dx = x1 - half_lookup_table_size;
                    for (int y1 = 0; y1 < distance_lookup.GetLength(1); y1++)
                    {
                        int dy = y1 - half_lookup_table_size;
                        distance_lookup[x1, y1] = (float)Math.Sqrt((dx*dx)+(dy*dy));
                    }
                }
            }
        
            // determine the surround area within which disparity data will be inserted
            int surround_pixels_x = scale_width[scale, 0]/2;
            if (surround_pixels_x < 2) surround_pixels_x = 2;
            int surround_pixels_y = scale_width[scale, 1]/2;
            if (surround_pixels_y < 2) surround_pixels_y = 2;

            // create a gaussian lookup table to improve speed if necessary
            if (gaussian_lookup == null)
                gaussian_lookup = probabilities.CreateHalfGaussianLookup(gaussian_lookup_levels);

            int half_surround_pixels_x = surround_pixels_x / 2;
            int half_surround_pixels_y = surround_pixels_y / 2;

            for (int xx = x; xx < x + surround_pixels_x; xx++)
            {
                if ((xx > -1) && (xx < map_wdth))
                {
                    int dx = x - xx + half_surround_pixels_x;
                    for (int yy = y; yy < y + surround_pixels_y; yy++)
                    {
                        if ((yy > -1) && (yy < map_hght))
                        {
                            int dy = y - yy + half_surround_pixels_y;
                            float dist = distance_lookup[half_lookup_table_size + dx, half_lookup_table_size + dy];
                            if (dist < surround_pixels_x)
                            {
                                // get the index within the gaussian lookup table
                                // based upon the fractional distance from the x,y centre point relative to the surround radius
                                int gaussian_index = (int)Math.Round((surround_pixels_x - dist) * (gaussian_lookup_levels-1) / (float)surround_pixels_x);

                                // number of hits, proportional to a gaussian probability distribution
                                float hits = 10 + (gaussian_lookup[gaussian_index] * 100 * confidence);

                                // update the disparity map at this location
                                disparity_map[xx, yy] += (disparity_value * hits);
                                disparity_hits[xx, yy] += hits;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region "main update routine"

        /// <summary>
        /// main update routine for contour based stereo correspondence
        /// </summary>
        /// <param name="left_bmp">left image data</param>
        /// <param name="right_bmp">right image data</param>
        /// <param name="wdth">width of the images</param>
        /// <param name="hght">height of the images</param>
        /// <param name="calibration_offset_x">calibration offset to counter for any small vergence angle between the cameras</param>
        /// <param name="calibration_offset_y">calibration offset to counter for any small vergence angle between the cameras</param>
        /// <param name="reset_attention">reset the attention map</param>
        public void update(Byte[] left_bmp, Byte[] right_bmp,
                           int wdth, int hght,
                           float calibration_offset_x, float calibration_offset_y, 
                           bool reset_attention)
        {
            int scale, idx;
            int x, y, x2;

            if ((wavepoints_left == null) || 
                (vertical_compression != prev_vertical_compression) ||
                (disparity_map_compression != prev_disparity_map_compression))
            {
                // create image objects to store the left and right camera data
                img_left = new classimage();
                img_left.createImage(wdth, hght / vertical_compression);
                img_right = new classimage();
                img_right.createImage(wdth, hght / vertical_compression);

                wavepoints_left = new float[hght / vertical_compression, wdth / step_size];
                wavepoints_left_scale = new byte[hght / vertical_compression, wdth / step_size];
                wavepoints_left_pattern = new byte[hght / vertical_compression, wdth / step_size];
                wavepoints_right = new float[hght / vertical_compression, wdth / step_size];
                wavepoints_right_scale = new byte[hght / vertical_compression, wdth / step_size];
                wavepoints_right_pattern = new byte[hght / vertical_compression, wdth / step_size];
                scalepoints_left = new int[no_of_scales, wdth + 1];
                scalepoints_right = new int[no_of_scales, wdth + 1];
                scalepoints_lookup = new int[no_of_scales, wdth, wdth + 1];

                // create an attention map
                attention_map = new bool[wdth, hght];
                resetAttention(wdth, hght);

                int w = (wdth / (step_size * disparity_map_compression)) + 1;
                int h = (hght / (vertical_compression * disparity_map_compression)) + 1;
                disparity_map = new float[w, h];
                disparity_hits = new float[w, h];
                scale_width = new int[no_of_scales, 2];

                int sc = 2;
                for (int s = 0; s < no_of_scales; s++)
                {
                    scale_width[s, 0] = (int)(wdth * surround_radius_percent * sc / 100);
                    if (scale_width[s, 0] < 2) scale_width[s, 0] = 2;
                    scale_width[s, 1] = (int)((hght / vertical_compression) * surround_radius_percent * sc / 100);
                    if (scale_width[s, 1] < 2) scale_width[s, 1] = 2;
                    sc++;
                }
            }

            if (reset_attention) resetAttention(wdth, hght);

            // store compression values so that changes in these
            // values can be detected
            prev_vertical_compression = vertical_compression;
            prev_disparity_map_compression = disparity_map_compression;

            // set the images
            left_image = left_bmp;
            img_left.updateFromBitmapVerticalCompression(left_bmp, wdth, hght, vertical_compression, 0, 0);
            img_right.updateFromBitmapVerticalCompression(right_bmp, wdth, hght, vertical_compression, (int)calibration_offset_x, (int)calibration_offset_y);

            // update integrals
            img_left.updateIntegralImage();
            img_right.updateIntegralImage();            

            // clear the disparity map
            clearDisparityMap(wdth / (step_size * disparity_map_compression), hght / (vertical_compression * disparity_map_compression));

            // update blobs on multiple scales
            for (scale = 0; scale < no_of_scales; scale++)
            {
                // get x and y radius for this scale
                int surround_pixels_x = scale_width[scale, 0];
                int surround_pixels_y = scale_width[scale, 1];

                // detect blobs at this scale
                img_left.detectBlobs(scale, surround_pixels_x, surround_pixels_y, step_size, wavepoints_left, wavepoints_left_scale, wavepoints_left_pattern);
                img_right.detectBlobs(scale, surround_pixels_x, surround_pixels_y, step_size, wavepoints_right, wavepoints_right_scale, wavepoints_right_pattern);
            }

            // update the scale points for fast searching
            float min_thresh = 5.0f;
            float min_grad = 0.5f;
            float left_diff, right_diff;
            float prev_left_diff = 0, prev_right_diff = 0;
            float prev_left_grad = 0, prev_right_grad = 0;
            float left_grad = 0, right_grad = 0;
            int max_disp = max_disparity * (wdth / step_size) / 100;
            int searchfactor = 4;
            int max_disp2 = max_disp / searchfactor;
            int max_wdth = wdth / searchfactor;

            // for each row of the image
            for (y = 0; y < hght / vertical_compression; y++)
            {
                for (int sign = 0; sign < 8; sign++)
                {
                    // go through each detection pattern
                    // at present there are only two patterns: centre/surround and left/right                    
                    for (int currPattern = PATTERN_CENTRE_SURROUND; currPattern <= PATTERN_LEFT_RIGHT; currPattern++)
                    {
                        // clear the number of points
                        for (scale = 0; scale < no_of_scales; scale++)
                        {
                            scalepoints_left[scale, 0] = 0;
                            scalepoints_right[scale, 0] = 0;
                            for (x = 0; x < max_wdth; x++)
                                scalepoints_lookup[scale, x, 0] = 0;
                        }
                        
                        int ww = wdth / step_size;
                        for (x = 0; x < ww; x++)
                        {
                            int pattern = wavepoints_left_pattern[y, x];
                            if (pattern == currPattern)
                            {
                                // response value
                                left_diff = wavepoints_left[y, x];
                                right_diff = wavepoints_right[y, x];
                                if ((x > 0) && ((left_diff != 0) || (right_diff != 0)))
                                {
                                    // gradient - change in response along the row
                                    left_grad = left_diff - prev_left_diff;
                                    right_grad = right_diff - prev_right_diff;
                                    
                                    float left_grad_change = left_grad - prev_left_grad;
                                    float right_grad_change = right_grad - prev_right_grad;

                                    if ((left_diff != 0) && ((left_grad < -min_grad) || (left_grad > min_grad)))
                                    {                                        
                                        // combiantions of response and gradient directions
                                        if (((sign == 0) && (left_diff > min_thresh) && (left_grad > 0) && (left_grad_change > 0)) ||
                                            ((sign == 1) && (left_diff < -min_thresh) && (left_grad > 0) && (left_grad_change > 0)) ||
                                            ((sign == 2) && (left_diff > min_thresh) && (left_grad <= 0) && (left_grad_change > 0)) ||
                                            ((sign == 3) && (left_diff < -min_thresh) && (left_grad <= 0) && (left_grad_change > 0)) ||
                                            ((sign == 4) && (left_diff > min_thresh) && (left_grad > 0) && (left_grad_change <= 0)) ||
                                            ((sign == 5) && (left_diff < -min_thresh) && (left_grad > 0) && (left_grad_change <= 0)) ||
                                            ((sign == 6) && (left_diff > min_thresh) && (left_grad <= 0) && (left_grad_change <= 0)) ||
                                            ((sign == 7) && (left_diff < -min_thresh) && (left_grad <= 0) && (left_grad_change <= 0))                                            
                                            )
                                        {
                                            // what is the best responding scale ?
                                            scale = wavepoints_left_scale[y, x];
                                            
                                            // get the current index
                                            idx = scalepoints_left[scale, 0] + 1;
                                            
                                            // set the x position
                                            scalepoints_left[scale, idx] = x;
                                            
                                            // increment the index
                                            scalepoints_left[scale, 0]++;
                                        }
                                    }

                                    if ((right_diff != 0) && ((right_grad < -min_grad) || (right_grad > min_grad)))
                                    {
                                        // combiantions of response and gradient directions
                                        if (((sign == 0) && (right_diff > min_thresh) && (right_grad > 0) && (right_grad_change > 0)) ||
                                            ((sign == 1) && (right_diff < -min_thresh) && (right_grad > 0) && (right_grad_change > 0)) ||
                                            ((sign == 2) && (right_diff > min_thresh) && (right_grad <= 0) && (right_grad_change > 0)) ||
                                            ((sign == 3) && (right_diff < -min_thresh) && (right_grad <= 0) && (right_grad_change > 0)) ||
                                            ((sign == 4) && (right_diff > min_thresh) && (right_grad > 0) && (right_grad_change <= 0)) ||
                                            ((sign == 5) && (right_diff < -min_thresh) && (right_grad > 0) && (right_grad_change <= 0)) ||
                                            ((sign == 6) && (right_diff > min_thresh) && (right_grad <= 0) && (right_grad_change <= 0)) ||
                                            ((sign == 7) && (right_diff < -min_thresh) && (right_grad <= 0) && (right_grad_change <= 0))                                            
                                            )
                                        {
                                            scale = wavepoints_right_scale[y, x];
                                            
                                            // get the current index
                                            idx = scalepoints_right[scale, 0] + 1;
                                            
                                            // set the x position
                                            scalepoints_right[scale, idx] = x;
                                            
                                            // increment the index
                                            scalepoints_right[scale, 0]++;

                                            x2 = x / searchfactor;
                                            //for (int xx = x2 - max_disp2; xx < x2 + max_disp2; xx++)                                            
                                            for (int xx = x2; xx < x2 + max_disp2; xx++)
                                            {
                                                if ((xx > -1) && (xx < max_wdth))
                                                {
                                                    int idx2 = scalepoints_lookup[scale, xx, 0] + 1;
                                                    scalepoints_lookup[scale, xx, idx2] = idx;
                                                    scalepoints_lookup[scale, xx, 0]++;
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // record previous responses
                                prev_left_grad = left_grad;
                                prev_right_grad = right_grad;
                                prev_left_diff = left_diff;
                                prev_right_diff = right_diff;
                            }
                        }

                        // stereo match
                        for (scale = 0; scale < no_of_scales; scale++)
                        {
                            int no_of_points_left = scalepoints_left[scale, 0];
                            int no_of_points_right = scalepoints_right[scale, 0];
                            for (int i = 0; i < no_of_points_left; i++)
                            {
                                int disp = -1;

                                // get the position and response magnitude of the left point
                                int x_left = scalepoints_left[scale, i + 1];
                                float diff_left = wavepoints_left[y, x_left];

                                int x_left2 = x_left - 2;
                                if (x_left2 < 0) x_left2 = 0;
                                int x_left3 = x_left + 2;
                                if (x_left3 >= ww) x_left3 = ww-1;
                                int prev_pattern_left = wavepoints_left_pattern[y, x_left2];
                                int next_pattern_left = wavepoints_left_pattern[y, x_left3];
                                float min_response_difference = match_threshold;

                                x2 = x_left / searchfactor;
                                int no_of_candidates = scalepoints_lookup[scale, x2, 0];
                                for (int j = 0; j < no_of_candidates; j++)
                                {
                                    int idx2 = scalepoints_lookup[scale, x2, j + 1];

                                    int x_right = scalepoints_right[scale, idx2];
                                    int dx = x_left - x_right;                                    
                                    if ((dx > -1) && (dx < max_disp))
                                    {
                                        int x_right2 = x_right - 2;
                                        if (x_right2 < 0) x_right2 = 0;
                                        int prev_pattern_right = wavepoints_right_pattern[y, x_right2];
                                        if (prev_pattern_left == prev_pattern_right)
                                        {
                                            int x_right3 = x_right + 2;
                                            if (x_right3 >= ww) x_right3 = ww - 1;
                                            int next_pattern_right = wavepoints_right_pattern[y, x_right3];
                                            if (next_pattern_left == next_pattern_right)
                                            {
                                                float diff_right = wavepoints_right[y, x_right];
                                                float response_difference = diff_right - diff_left;
                                                if (response_difference < 0) response_difference = -response_difference;
                                                if (response_difference < min_response_difference)
                                                {
                                                    disp = dx;
                                                    min_response_difference = response_difference;
                                                }
                                            }
                                        }                                        
                                    }
                                    if (dx > max_disp) j = no_of_candidates;
                                }


                                if (disp > -1)
                                {
                                    float confidence = 1.0f - (min_response_difference / match_threshold);
                                    confidence /= (no_of_scales - scale);
                                    confidence *= confidence;
                                    int mx = (x_left + disp) / disparity_map_compression;
                                    int my = y / disparity_map_compression;
                                    updateDisparityMap(mx, my,
                                                       wdth / (step_size * disparity_map_compression),
                                                       hght / (vertical_compression * disparity_map_compression),
                                                       scale, disp * step_size, confidence);
                                }
                            }

                        }
                    }

                }

            }

            // update disparity map
            int max_w = wdth / (step_size * disparity_map_compression);
            int max_h = hght / (vertical_compression * disparity_map_compression);
            for (y = 0; y < max_h; y++)
            {                
                for (x = 0; x < max_w; x++)
                {
                    float disp = disparity_map[x, y];
                    if (disp < 0)
                    {
                        disp = 0;
                    }
                    else
                    {
                        disp = disp / disparity_hits[x, y];
                        disparity_map[x, y] = disp;
                    }
                }
            }

            
            // remove snow            
            //filterDisparityMap(wdth / (step_size * disparity_map_compression), hght / (vertical_compression * disparity_map_compression), 10);
            //filterDisparityHistogram(20, 10);
            
            // smooth the disparity map
            if (useSmoothing)
            {
                if ((roll > -Math.PI/8) && (roll < Math.PI/8))
                    // conventional horizontal stereo camera mounting
                    smoothDisparityMap(wdth / (step_size * disparity_map_compression), hght / (vertical_compression * disparity_map_compression));
                else
                {
                    // rolled camera mounting
                    if (roll < 0)
                        smoothDisparityMapSlanted(wdth / (step_size * disparity_map_compression), hght / (vertical_compression * disparity_map_compression), 1);
                    else
                        smoothDisparityMapSlanted(wdth / (step_size * disparity_map_compression), hght / (vertical_compression * disparity_map_compression), 0);
                }
            }            
            

            //update the selected features
            getSelectedFeatures(wdth, hght);


        }

        #endregion

        #region "returning results"

        /// <summary>
        /// update the selected features array
        /// </summary>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        private void getSelectedFeatures(int wdth, int hght)
        {
            int tries = 0;
            int x_reduction = step_size * disparity_map_compression;
            int max_x = wdth / x_reduction;
            int y_reduction = vertical_compression * disparity_map_compression;
            int max_y = hght / y_reduction;
            no_of_selected_features = 0;
            selected_features = new float[required_features*3];
            bool[,] touched = new bool[max_x, max_y];
            while ((no_of_selected_features < required_features) && 
                   (tries < 100))
            {
                int x = rnd.Next(max_x - 1);
                int y = rnd.Next(max_y - 1);
                float disp = disparity_map[x, y];
                if (disp > 0)
                {
                    if (!touched[x, y])
                    {
                        touched[x, y] = true;
                        selected_features[no_of_selected_features * 3] = x * x_reduction;
                        selected_features[(no_of_selected_features * 3) + 1] = y * y_reduction;
                        selected_features[(no_of_selected_features * 3) + 2] = disp * step_size;
                        no_of_selected_features++;
                    }
                }
                else tries++;
            }
        }

        /// <summary>
        /// return the disparity value at the given point in te image
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float getDisparityMapPoint(int x, int y)
        {
            // convert to disparity map coordinates
            int xx = x / (step_size * disparity_map_compression);
            int yy = y / (vertical_compression * disparity_map_compression);
            float pointValue = disparity_map[xx, yy];
            if (pointValue < 0) pointValue = 0;

            return (pointValue);
        }

        /// <summary>
        /// returns the disparity map as an image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        public void getDisparityMap(Byte[] img, int wdth, int hght, int threshold)
        {
            if (disparity_map != null)
            {
                int disparity_map_width = disparity_map.GetLength(0);
                int disparity_map_height = disparity_map.GetLength(1);

                int max_disp = max_disparity * (wdth / step_size) / 100;
                int n = 0;
                for (int y = 0; y < hght; y++)
                {
                    int yy = y / (vertical_compression * disparity_map_compression);
                    for (int x = 0; x < wdth; x++)
                    {
                        int xx = x / (step_size * disparity_map_compression);
                        float disp = 0;

                        if ((xx < disparity_map_width) &&
                            (yy < disparity_map_height))
                            disp = disparity_map[xx, yy];

                        if (disp < 0)
                            disp = 0;
                        else
                            disp = disp * 255 / max_disp;


                        //if (average_disparity_hits[xx,yy] > 0)
                        //    disp = average_disparity_map[xx, yy] * 255 / (average_disparity_hits[xx,yy] * max_disp);

                        if (disp < threshold) disp = 0;
                        img[n] = (Byte)disp;
                        img[n + 1] = (Byte)disp;
                        img[n + 2] = (Byte)disp;
                        n += 3;
                    }
                }
            }
        }


        /// <summary>
        /// show any close objects in the given image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        /// <param name="threshold"></param>
        public void getCloseObjects(Byte[] raw_image, Byte[] output_img, Byte[] background_img, int wdth, int hght, int threshold)
        {
            int max_intensity = 255;
            if (disparity_map != null)
            {
                int max_disp = max_disparity * (wdth / step_size) / 100;
                int n = 0;
                for (int y = 0; y < hght; y++)
                {
                    int yy = y / (vertical_compression * disparity_map_compression);
                    for (int x = 0; x < wdth; x++)
                    {
                        int xx = x / (step_size * disparity_map_compression);
                        float disp = disparity_map[xx, yy];
                        if (disp < 0)
                            disp = 0;
                        else
                            disp = disp * 255 / max_disp;

                        int r = 0;
                        int g = 0;
                        int b = 0;
                        int n2 = ((y * wdth) + x) * 3;
                        if (disp > threshold)
                        {
                            r = raw_image[n2 + 2];
                            g = raw_image[n2 + 1];
                            b = raw_image[n2];
                            if ((r + g + b) / 3 > max_intensity)
                            {
                                r = 0;
                                g = 0;
                                b = 0;
                                disp = 0;
                            }
                        }
                        if (disp <= threshold)
                        {
                            if (background_img != null)
                            {
                                r = background_img[n2 + 2];
                                g = background_img[n2 + 1];
                                b = background_img[n2];
                            }
                        }
                        output_img[n] = (Byte)b;
                        output_img[n + 1] = (Byte)g;
                        output_img[n + 2] = (Byte)r;
                        n += 3;
                    }
                }
            }
        }

        #endregion


    }


}
