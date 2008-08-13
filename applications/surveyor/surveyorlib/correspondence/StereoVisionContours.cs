/*
    contour based stereo
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
using System.Drawing;
using System.IO;
using System.Collections;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// contour based stereo correspondence algorithm
    /// </summary>
    public class StereoVisionContours : StereoVision
    {
        Random rnd = new Random(100);

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

        /// <remarks>
        /// max disparity as a percentage of image width
        /// in the range 1-100
        /// </remarks>
        public int max_disparity;

        ///attention map
        public bool[,] attention_map;

        ///disparity map
        public float[][] disparity_map;

        public float[][] disparity_hits;

        public int[][] scale_width;

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
        public float match_threshold = 50;

        // radius of the surround for blob detection
        public float surround_radius_percent = 1.8f;

        private StereoVisionContoursImage img_left, img_right;

        // blob feature responses
        private float[][][] wavepoints_left = null;
        private float[][][] wavepoints_right = null;

        // blob feature scales
        private byte[][] wavepoints_left_scale = null;
        private byte[][] wavepoints_right_scale = null;

        // blob feature patterns
        private byte[][] wavepoints_left_pattern = null;
        private byte[][] wavepoints_right_pattern = null;

        int[][] scalepoints_left;
        int[][] scalepoints_right;
        int[][][] scalepoints_lookup;

        byte[] left_image = null;

        // gaussian function lookup table
        const int gaussian_lookup_levels = 1000;
        float[] gaussian_lookup;

        #region "constructors"

        /// <summary>
        /// constructor
        /// This sets some initial default values.
        /// </summary>
        public StereoVisionContours()
        {
            algorithm_type = CONTOURS;
            convert_to_mono = true;
            
            required_features = MAX_POINT_FEATURES; ///return all detected features by default

            /// maximum disparity 30% of image width
            max_disparity = 30;
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
                    attention_map[xx,yy] = true;
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
                    disparity_map[x][y] = -1;
                    disparity_hits[x][y] = 0;
                }
        }

        #endregion
		
        #region "updating the disparity map"

        // distance lokup table used to avoid doing square roots
        float[][] distance_lookup;

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
                distance_lookup = new float[lookup_table_size][];
                for (int i = 0; i < distance_lookup.Length; i++)
                    distance_lookup[i] = new float[lookup_table_size];

                for (int x1 = 0; x1 < lookup_table_size; x1++)
                {
                    int dx = x1 - half_lookup_table_size;
                    for (int y1 = 0; y1 < lookup_table_size; y1++)
                    {
                        int dy = y1 - half_lookup_table_size;
                        distance_lookup[x1][y1] = (float)Math.Sqrt((dx*dx)+(dy*dy));
                    }
                }
            }
        
            // determine the surround area within which disparity data will be inserted
            int surround_pixels_x = scale_width[scale][0]/2;
            if (surround_pixels_x < 2) surround_pixels_x = 2;
            int surround_pixels_y = scale_width[scale][1]/2;
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
                            float dist = distance_lookup[half_lookup_table_size + dx][half_lookup_table_size + dy];
                            if (dist < surround_pixels_x)
                            {
                                // get the index within the gaussian lookup table
                                // based upon the fractional distance from the x,y centre point relative to the surround radius
                                int gaussian_index = (int)((surround_pixels_x - dist) * (gaussian_lookup_levels-1) / (float)surround_pixels_x);

                                // number of hits, proportional to a gaussian probability distribution
                                float hits = 10 + (gaussian_lookup[gaussian_index] * 100 * confidence);

                                // update the disparity map at this location
                                disparity_map[xx][yy] += (disparity_value * hits);
                                disparity_hits[xx][yy] += hits;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region "main update routine"

        public bool reset_attention;

        /// <summary>
        /// main update routine for contour based stereo correspondence
        /// </summary>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right image data</param>
        /// <param name="wdth">width of the images</param>
        /// <param name="hght">height of the images</param>
        /// <param name="calibration_offset_x">calibration offset to counter for any small vergence angle between the cameras</param>
        /// <param name="calibration_offset_y">calibration offset to counter for any small vergence angle between the cameras</param>
        public override void Update(byte[] left_bmp, byte[] right_bmp,
                                    int wdth, int hght,
                                    float calibration_offset_x, float calibration_offset_y)
        {
            int scale, idx;
            int x, y, x2;

            if ((wavepoints_left == null) || 
                (vertical_compression != prev_vertical_compression) ||
                (disparity_map_compression != prev_disparity_map_compression))
            {
                // create image objects to store the left and right camera data
                img_left = new StereoVisionContoursImage();
                img_left.createImage(wdth, hght / vertical_compression);
                img_right = new StereoVisionContoursImage();
                img_right.createImage(wdth, hght / vertical_compression);

                wavepoints_left = new float[hght / vertical_compression][][];
                wavepoints_right = new float[hght / vertical_compression][][];
                wavepoints_left_scale = new byte[hght / vertical_compression][];
                wavepoints_left_pattern = new byte[hght / vertical_compression][];
                wavepoints_right_scale = new byte[hght / vertical_compression][];
                wavepoints_right_pattern = new byte[hght / vertical_compression][];
                for (int i = 0; i < wavepoints_left.Length; i++)
                {
                    wavepoints_left[i] = new float[wdth / step_size][];
                    wavepoints_right[i] = new float[wdth / step_size][];
                    wavepoints_left_scale[i] = new byte[wdth / step_size];
                    wavepoints_left_pattern[i] = new byte[wdth / step_size];
                    wavepoints_right_scale[i] = new byte[wdth / step_size];
                    wavepoints_right_pattern[i] = new byte[wdth / step_size];
                    for (int j = 0; j < wavepoints_left[i].Length; j++)
                    {
                        wavepoints_left[i][j] = new float[3];
                        wavepoints_right[i][j] = new float[3];
                    }
                }

                scalepoints_left = new int[no_of_scales][];
                scalepoints_right = new int[no_of_scales][];
                scalepoints_lookup = new int[no_of_scales][][];
                for (int i = 0; i < no_of_scales; i++)
                {
                    scalepoints_left[i] = new int[wdth + 1];
                    scalepoints_right[i] = new int[wdth + 1];
                    scalepoints_lookup[i] = new int[wdth][];
                    for (int j = 0; j < scalepoints_lookup[i].Length; j++)
                    {
                        scalepoints_lookup[i][j] = new int[wdth + 1];
                    }
                }

                // create an attention map
                attention_map = new bool[wdth, hght];
                resetAttention(wdth, hght);

                int w = (wdth / (step_size * disparity_map_compression)) + 1;
                int h = (hght / (vertical_compression * disparity_map_compression)) + 1;
                disparity_map = new float[w][];
                disparity_hits = new float[w][];
                for (int i = 0; i < w; i++)
                {
                    disparity_map[i] = new float[h];
                    disparity_hits[i] = new float[h];
                }
                scale_width = new int[no_of_scales][];

                int sc = 2;
                for (int s = 0; s < no_of_scales; s++)
                {
                    scale_width[s] = new int[2];
                    scale_width[s][0] = (int)(wdth * surround_radius_percent * sc / 100);
                    if (scale_width[s][0] < 2) scale_width[s][0] = 2;
                    scale_width[s][1] = (int)((hght / vertical_compression) * surround_radius_percent * sc / 100);
                    if (scale_width[s][1] < 2) scale_width[s][1] = 2;
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

			// update average intensities for each row and column
			img_left.updateAverages();
			img_right.updateAverages();

            // disparity map dimensions
            int compressed_wdth = wdth / (step_size * disparity_map_compression);
            int compressed_hght = hght / (vertical_compression * disparity_map_compression);

            // clear the disparity map
            clearDisparityMap(compressed_wdth, compressed_hght);

            // update blobs on multiple scales
            for (scale = 0; scale < no_of_scales; scale++)
            {
                // get x and y radius for this scale
                int surround_pixels_x = scale_width[scale][0];
                int surround_pixels_y = scale_width[scale][1];

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
			int max_vertical_edge_difference = hght / 4;

            // assorted variables
            int no_of_points_left, no_of_points_right;
            int disp, x_left, vertical_left, x_left2, x_left3, no_of_candidates;
            int prev_pattern_left, next_pattern_left, idx2;
            int x_right, vertical_right, x_right2, x_right3, dx, prev_pattern_right, next_pattern_right;
            float diff_left, diff_row_left, diff_col_left, min_response_difference;
            float confidence, diff_right, response_difference;
                        
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
                            scalepoints_left[scale][0] = 0;
                            scalepoints_right[scale][0] = 0;
                            for (x = 0; x < max_wdth; x++)
                                scalepoints_lookup[scale][x][0] = 0;
                        } 
                        
                        int ww = wdth / step_size;
                        for (x = 0; x < ww; x++)
                        {
                            int pattern = wavepoints_left_pattern[y][x];
                            if (pattern == currPattern)
                            {
                                // response value
                                left_diff = wavepoints_left[y][x][0];
                                right_diff = wavepoints_right[y][x][0];
                                if ((x > 0) && ((left_diff != 0) || (right_diff != 0)))
                                {
                                    float left_row_diff = wavepoints_left[y][x][1];
                                    float right_row_diff = wavepoints_right[y][x][1];

                                    // gradient - change in response along the row
                                    left_grad = left_diff - prev_left_diff;
                                    right_grad = right_diff - prev_right_diff;

                                    if (((left_row_diff > 0) && (right_row_diff > 0)) ||
                                        ((left_row_diff < 0) && (right_row_diff < 0)))
                                    {
                                        float left_col_diff = wavepoints_left[y][x][2];
                                        float right_col_diff = wavepoints_right[y][x][2];
                                        if (((left_col_diff >= 0) && (right_col_diff >= 0)) ||
                                            ((left_col_diff < 0) && (right_col_diff < 0)))
                                        {
                                            float left_horizontal_grad_change = left_grad - prev_left_grad;
                                            float right_horizontal_grad_change = right_grad - prev_right_grad;

                                            if ((left_diff != 0) && ((left_grad < -min_grad) || (left_grad > min_grad)))
                                            {
                                                // combiantions of response and gradient directions
                                                if (((sign == 0) && (left_diff > min_thresh) && (left_grad > 0) && (left_horizontal_grad_change > 0)) ||
                                                    ((sign == 1) && (left_diff < -min_thresh) && (left_grad > 0) && (left_horizontal_grad_change > 0)) ||
                                                    ((sign == 2) && (left_diff > min_thresh) && (left_grad <= 0) && (left_horizontal_grad_change > 0)) ||
                                                    ((sign == 3) && (left_diff < -min_thresh) && (left_grad <= 0) && (left_horizontal_grad_change > 0)) ||
                                                    ((sign == 4) && (left_diff > min_thresh) && (left_grad > 0) && (left_horizontal_grad_change <= 0)) ||
                                                    ((sign == 5) && (left_diff < -min_thresh) && (left_grad > 0) && (left_horizontal_grad_change <= 0)) ||
                                                    ((sign == 6) && (left_diff > min_thresh) && (left_grad <= 0) && (left_horizontal_grad_change <= 0)) ||
                                                    ((sign == 7) && (left_diff < -min_thresh) && (left_grad <= 0) && (left_horizontal_grad_change <= 0))
                                                    )
                                                {
                                                    // what is the best responding scale ?
                                                    scale = wavepoints_left_scale[y][x];

                                                    // get the current index
                                                    idx = scalepoints_left[scale][0] + 1;

                                                    // set the x position
                                                    scalepoints_left[scale][idx] = x;

                                                    // increment the index
                                                    scalepoints_left[scale][0]++;
                                                }
                                            }

                                            if ((right_diff != 0) && ((right_grad < -min_grad) || (right_grad > min_grad)))
                                            {
                                                // combiantions of response and gradient directions
                                                if (((sign == 0) && (right_diff > min_thresh) && (right_grad > 0) && (right_horizontal_grad_change > 0)) ||
                                                    ((sign == 1) && (right_diff < -min_thresh) && (right_grad > 0) && (right_horizontal_grad_change > 0)) ||
                                                    ((sign == 2) && (right_diff > min_thresh) && (right_grad <= 0) && (right_horizontal_grad_change > 0)) ||
                                                    ((sign == 3) && (right_diff < -min_thresh) && (right_grad <= 0) && (right_horizontal_grad_change > 0)) ||
                                                    ((sign == 4) && (right_diff > min_thresh) && (right_grad > 0) && (right_horizontal_grad_change <= 0)) ||
                                                    ((sign == 5) && (right_diff < -min_thresh) && (right_grad > 0) && (right_horizontal_grad_change <= 0)) ||
                                                    ((sign == 6) && (right_diff > min_thresh) && (right_grad <= 0) && (right_horizontal_grad_change <= 0)) ||
                                                    ((sign == 7) && (right_diff < -min_thresh) && (right_grad <= 0) && (right_horizontal_grad_change <= 0))
                                                    )
                                                {
                                                    scale = wavepoints_right_scale[y][x];

                                                    // get the current index
                                                    idx = scalepoints_right[scale][0] + 1;

                                                    // set the x position
                                                    scalepoints_right[scale][idx] = x;

                                                    // increment the index
                                                    scalepoints_right[scale][0]++;

                                                    x2 = x / searchfactor;
                                                    for (int xx = x2; xx < x2 + max_disp2; xx++)
                                                    {
                                                        if ((xx > -1) && (xx < max_wdth))
                                                        {
                                                            idx2 = scalepoints_lookup[scale][xx][0] + 1;
                                                            scalepoints_lookup[scale][xx][idx2] = idx;
                                                            scalepoints_lookup[scale][xx][0]++;
                                                        }
                                                    }
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
                        for (scale = no_of_scales - 1; scale >= 0; scale--)
                        {
                            no_of_points_left = scalepoints_left[scale][0];
                            no_of_points_right = scalepoints_right[scale][0];
							
							//for each possible match in the left image
                            for (int i = no_of_points_left - 1; i >= 0; i--)
                            {
                                disp = -1;

                                // get the position and response magnitude of the left point
                                x_left = scalepoints_left[scale][i + 1];
								vertical_left = img_left.column_maximal_edge[x_left];
                                diff_left = wavepoints_left[y][x_left][0];
                                diff_row_left = wavepoints_left[y][x_left][1];
                                diff_col_left = wavepoints_left[y][x_left][2];

                                x_left2 = x_left - 2;
                                if (x_left2 < 0) x_left2 = 0;
                                x_left3 = x_left + 2;
                                if (x_left3 >= ww) x_left3 = ww - 1;
                                prev_pattern_left = wavepoints_left_pattern[y][x_left2];
                                next_pattern_left = wavepoints_left_pattern[y][x_left3];
                                min_response_difference = match_threshold;

                                x2 = x_left / searchfactor;
                                no_of_candidates = scalepoints_lookup[scale][x2][0];

								// for each possible match in the right image
								// note here that we scan from right to left
                                for (int j = no_of_candidates - 1; j >= 0; j--)
                                {
                                    idx2 = scalepoints_lookup[scale][x2][j + 1];

									// get the horizontal position of the possible match in the right image
                                    x_right = scalepoints_right[scale][idx2];
									
									// what's the disparity ?
                                    dx = x_left - x_right;
									
									// is the disparity in the range we expect ?
                                    if ((dx > -1) && (dx < max_disp))
                                    {
										// vertical context checking
										vertical_right = img_left.column_maximal_edge[x_right];
                                        int dv = vertical_left - vertical_right;
										if (dv < 0) dv = -dv;
										
										// is the vertical context within tollerance ?
										if (dv < max_vertical_edge_difference)
										{										
											// check the ordering of patterns
                                            x_right2 = x_right - 2;
                                            if (x_right2 < 0) x_right2 = 0;
                                            prev_pattern_right = wavepoints_right_pattern[y][x_right2];
                                            if (prev_pattern_left == prev_pattern_right)
                                            {
                                                x_right3 = x_right + 2;
                                                if (x_right3 >= ww) x_right3 = ww - 1;
                                                next_pattern_right = wavepoints_right_pattern[y][x_right3];
                                                if (next_pattern_left == next_pattern_right)
                                                {
													// check the response magnitude difference
                                                    diff_right = wavepoints_right[y][x_right][0];
                                                    response_difference = diff_right - diff_left;
                                                    if (response_difference < 0) response_difference = -response_difference;
                                                    response_difference *= dv;
													
													// is the magnitude difference the best that we've found so far ? ?
												    if (response_difference < min_response_difference)
                                                    {
														// record the disparity and minimum difference
                                                        disp = dx;
                                                        min_response_difference = response_difference;
                                                    }
                                                }
                                            }
										}
                                    }
									
									// if the horizontal difference is too large then we may 
									// as well abandon the search
                                    if (dx > max_disp) break;
                                }

                                if (disp > -1)
                                {
									// how confident are we in this match ?
                                    confidence = 1.0f - (min_response_difference / match_threshold);
                                    confidence /= (no_of_scales - scale);
                                    confidence *= confidence;
									
									// get the position on the disparity map
                                    int mx = (x_left + disp) / disparity_map_compression;
                                    int my = y / disparity_map_compression;
									
									// update the dispalrity map using a gaussian
									// probability distribution
                                    updateDisparityMap(mx, my,
                                                       compressed_wdth, compressed_hght,
                                                       scale, disp * step_size, confidence);
                                }
                            }

                        }
                         
                        
                    }

                }

            }

            // update disparity map
            float disparity_value;
            for (y = compressed_hght; y >= 0; y--)
            {
                for (x = compressed_wdth - 1; x >= 0; x--)
                {
                    disparity_value = disparity_map[x][y];
                    if (disparity_value < 0)
                    {
                        disparity_value = 0;
                    }
                    else
                    {
                        disparity_value /= disparity_hits[x][y];
                        disparity_map[x][y] = disparity_value;
                    }
                }
            }            
            			
            // get a fixed quantity of features which may
			// subsequently be used to create ray models
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
            features.Clear();
            
            int tries = 0;
            int x_reduction = step_size * disparity_map_compression;
            int max_x = disparity_map.Length; //wdth / x_reduction;
            int y_reduction = vertical_compression * disparity_map_compression;
            int max_y = disparity_map[0].Length; //hght / y_reduction;
            bool[,] touched = new bool[max_x, max_y];
            while ((features.Count < required_features) && 
                   (tries < 100))
            {
                int x = rnd.Next(max_x - 1);
                int y = rnd.Next(max_y - 1);
                float disp = disparity_map[x][y];
                if (disp > 0)
                {
                    if (!touched[x, y])
                    {
                        touched[x, y] = true;
                        features.Add(new StereoFeature(x * x_reduction, y * y_reduction, disp * step_size));
                    }
                }
                else tries++;
            }
        }

        /// <summary>
        /// return the disparity value at the given point in the image
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float getDisparityMapPoint(int x, int y)
        {
            // convert to disparity map coordinates
            int xx = x / (step_size * disparity_map_compression);
            int yy = y / (vertical_compression * disparity_map_compression);
            float pointValue = disparity_map[xx][yy];
            if (pointValue < 0) pointValue = 0;

            return (pointValue);
        }

        public override void Show(ref Bitmap output)
        {
            byte[] output_img = null;
            
            if (img[0].Length == image_width * image_height * 3)
            {
                output_img = new byte[image_width*image_height];
            }
            else
            {
                output_img = new byte[image_width*image_height*3];
            }
            
            if (output == null)
                output = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                
            int threshold = 0;
            getDisparityMap(output_img, image_width, image_height, threshold);
                
            BitmapArrayConversions.updatebitmap_unsafe(output_img, output);
        }


        /// <summary>
        /// returns the disparity map as an image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="wdth"></param>
        /// <param name="hght"></param>
        public void getDisparityMap(byte[] img, int wdth, int hght, int threshold)
        {
            if (disparity_map != null)
            {
                int disparity_map_width = disparity_map.Length;
                int disparity_map_height = disparity_map[0].Length;

Console.WriteLine("pixels : " + img.Length.ToString());
Console.WriteLine("pixels2: " + (wdth*hght*3).ToString() + " " + wdth.ToString() + "x" + hght.ToString());


                int max_disp = max_disparity * (wdth / step_size) / 100;
                int n = 0;
                for (int y = 0; y < hght; y++)
                {
                    int yy = y / (vertical_compression * disparity_map_compression);
                    //int yy = y * (disparity_map_height*vertical_compression) / hght;
                    for (int x = 0; x < wdth; x++)
                    {
                        int xx = x / (step_size * disparity_map_compression);
                        //int xx = x * disparity_map_width / wdth;
                        float disp = 0;

                        if ((xx < disparity_map_width) &&
                            (yy < disparity_map_height))
                            disp = disparity_map[xx][yy];

                        if (disp < 0)
                            disp = 0;
                        else
                            disp = disp * 255 / max_disp;


                        //if (average_disparity_hits[xx,yy] > 0)
                        //    disp = average_disparity_map[xx, yy] * 255 / (average_disparity_hits[xx,yy] * max_disp);

                        if (disp < threshold) disp = 0;
                        if (n+2 < img.Length)
                        {
                            img[n] = (byte)disp;
                            img[n + 1] = (byte)disp;
                            img[n + 2] = (byte)disp;
                        }
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
        public void getCloseObjects(byte[] raw_image, byte[] output_img, byte[] background_img, int wdth, int hght, int threshold)
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
                        float disp = disparity_map[xx][yy];
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
                        output_img[n] = (byte)b;
                        output_img[n + 1] = (byte)g;
                        output_img[n + 2] = (byte)r;
                        n += 3;
                    }
                }
            }
        }

        #endregion


    }


}