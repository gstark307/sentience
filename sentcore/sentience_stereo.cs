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
using System.Text;
using CenterSpace.Free;

namespace sentience.core
{
    /// <summary>    
    ///                              sentience_stereo
    ///
    ///    This is used to calculate depth maps from stereo images, and returns
    ///    a set of point features.  All feature coordinates are from the left image.
    /// 
    ///    The algorithm works basically as follows:
    /// 
    /// 1.  Determine vertical edge intensities by comparing a few pixels to the
    ///     left and right along each row.
    ///
    /// 2.  Perform non maximal suppression along each row to give the 5 strongest
    ///     edges.
    ///
    /// 3.  Perform (1) and (2) on the left and right camera images.
    ///
    /// 4.  For each row compare the edge features in the left and right images.  A
    ///     small neighbourhood is compared, on two or three different scales.  Store
    ///     the comparison results.
    ///
    /// 5.  Sort the results from (4) to give typically the 100 best scoring
    ///     matches.
    ///
    /// This method works reasonably well for low numbers of features.  If you
    /// increase the number of features selected in (5) the results get worse
    /// because lower quality matches are included.
    ///
    ///    version              : 6.04
    ///    begin                : Sun Sep 10 2000
    ///    last modified        : Sun Sep 10 2006
    ///    copyright            : (C) 2000-2006 by Bob Mottram
    ///    email                : fuzzgun@gmail.com
    /// </summary>
    /// <example>
    ///
    ///     stereovision = new sentience_stereo(); 
    ///
    ///     stereovision.standard_width = 640;
    ///     stereovision.standard_height = 480/4;
    ///     stereovision.max_disparity = 5;
    ///     stereovision.required_features = 200;
    ///     stereovision.update(left_image, right_image, 640, 480, 1);
    ///
    ///     float x, y, disparity;
    ///     for (int i = 0; i < stereovision.no_of_selected_features; i++)
    ///     {
    ///         x = selected_features[i*3];              //left image x coordinate
    ///         y = selected_features[(i*3)+1];          //left image y coordinate
    ///         disparity = selected_features[(i*3)+2];  //disparity in pixels (3 decimal places)
    ///     }
    ///
    /// </example>
    /// <history>
    ///     <version>
    ///         <number>6.01</number>
    ///         <changes>
    ///         </changes>
    ///         <bugfixes>
    ///         Set lookup array values initially to zero within updateStandard
    ///         </bugfixes>
    ///     </version>
    ///     <version>
    ///         <number>6.02</number>
    ///         <changes>
    ///         Replaced disparity_map array with disparities, enabling more efficient
    ///         selection of a fixed number of features and elimination of a couple of functions
    ///         </changes>
    ///         <bugfixes>
    ///         </bugfixes>
    ///     </version>
    ///     <version>
    ///         <number>6.03</number>
    ///         <changes>
    ///         Simplified algorithm no longer uses line segments
    ///         </changes>
    ///         <bugfixes>
    ///         </bugfixes>
    ///     </version>
    ///     <version>
    ///         <number>6.04</number>
    ///         <changes>
    ///         added edge blob type
    ///         each feature now has a matching score, so that they can be rated for quality
    ///         </changes>
    ///         <bugfixes>
    ///         </bugfixes>
    ///     </version>
    /// </history>
    public class sentience_stereo
    {
        // max number of features per row
        private const int MAX_POINT_FEATURES = 6000;

        /// <remarks> 
        /// The number of features required
        /// this can be between 0 and MAX_POINT_FEATURES
        /// The number of features actually returned is given in no_of_selected_features
        /// and may be lower than the required number if there are few features
        /// within the images (eg looking at an area with large blank spaces)
        /// </remarks>
        public int required_features;

        /// <remarks> 
        /// this is used to set the minimum centre/surround blob difference
        /// threshold below which matching does not occur.
        /// It determines what the value for average_blob_difference will be.
        /// values are in the range 1-100
        /// </remarks>
        public int difference_threshold;

        ///full resolution image dimensions
        public int fullres_width, fullres_height;

        ///a fixed quantity (required_features) of selected point features
        public int no_of_selected_features;
        public float[] selected_features;

        /// <remarks>
        /// max disparity as a percentage of image width
        /// in the range 1-100
        /// </remarks>
        public int max_disparity;

        /// radii used for context matching
        /// this is as a percentage of the image resolution
        /// in the range 0-100
        public int matchRadius1, matchRadius2;


        ///all image disparities within a big list
        public int no_of_disparities;
        public float[] disparities;

        public float average_matching_score;

        // radius used to calculate local average intensity
        // this is used to normalise peak values
        public int localAverageRadius;


        ///used to indicate whether arrays have been initialised
        private bool initialised;

        private int[] disp_x;
        private int[] disp_y;
        private int[] integral;
        private float[] left_maxima;
        private float[] right_maxima;
        private float[] temp;
        private bool[,] left_feature_properties;
        private bool[,] right_feature_properties;

        /// <summary>
        /// constructor
        /// This sets some initial default values.
        /// </summary>
        public sentience_stereo()
        {
            initialised = false;

            ///array storing a fixed quantity of selected features
            no_of_selected_features = 0;
            selected_features = new float[MAX_POINT_FEATURES * 3];

            no_of_disparities = 0;
            disparities = new float[MAX_POINT_FEATURES * 4];

            required_features = MAX_POINT_FEATURES; ///return all detected features by default
            difference_threshold = 30; //100;

            /// maximum disparity 5% of image width
            max_disparity = 5;

            matchRadius1 = 2;
            matchRadius2 = 4;

            // radius for local intensity averaging
            localAverageRadius = 236;
        }


        /// <summary>
        /// locate edge feature maxima along a single row
        /// </summary>
        /// <param name="y">y coordinate within the image (row number)</param>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="integral">array used to store sliding sums</param>
        /// <param name="maxima">array storing maxima.  Each maxima consists of the x coordinate, magnitude and absolute magnitude value</param>
        /// <param name="temp">temporary array</param>
        /// <param name="radius">centre radius of centre/surround feature detector</param>
        /// <param name="inhibit_radius">surround radius of centre/surround feature detector</param>
        /// <param name="min_intensity">minimum intensity of the centre region of the centre/surround area</param>
        /// <param name="max_intensity">maximum intensity of the centre region of the centre/surround area</param>
        /// <param name="max_maxima">the maximum number of maxima per row</param>
        /// <param name="image_threshold">minimum pixel intensity</param>
        /// <return>number of maxima found</return>
        private int row_maxima(int y, byte[] bmp,
                               int wdth, int hght, int bytes_per_pixel,
                               int[] integral, float[] maxima, float[] temp, int radius,
                               int inhibit_radius, int min_intensity, int max_intensity, 
                               int max_maxima, int image_threshold)
        {
            int x, xx, v, i;
            int startpos = y * wdth * bytes_per_pixel;
            int no_of_maxima = 0;
            int no_of_temp_maxima = 0;
            float prev_mag, mag, prev_x, x_accurate, absdiff;

            // update the integrals for the row
            xx = startpos;
            integral[0] = 0;
            for (int b = 0; b < bytes_per_pixel; b++) integral[0] += bmp[xx + b];
            xx += bytes_per_pixel;

            for (x = 1; x < wdth; x++)
            {
                integral[x] = integral[x - 1];
                if (bmp[xx] > image_threshold)
                    for (int b = 0; b < bytes_per_pixel; b++) integral[x] += bmp[xx + b];

                xx += bytes_per_pixel;
            }

            int radius2 = 3 * localAverageRadius / 100;

            // create edges
            for (x = radius; x < wdth - radius - 1; x++)
            {
                v = integral[x];
                float left = v - integral[x - radius];
                float right = integral[x + radius] - v;
                float tot = left + right;

                if ((tot > min_intensity) && (tot < max_intensity))
                {
                    int x_min = x - radius2;
                    if (x_min < 0) x_min = 0;
                    int x_max = x + radius2;
                    if (x_max >= wdth) x_max = wdth - 1;
                    float tot_wide = (integral[x_max] - integral[x_min]) / (float)(x_max - x_min);
                    float diff = (left - right) * 140 / tot_wide;

                    absdiff = diff;
                    if (absdiff < 0) absdiff = -absdiff;

                    if (absdiff > difference_threshold)
                    {
                        // a simple kind of sub-pixel interpolation
                        x_accurate = (((x - radius) * left) + ((x + radius) * right)) / tot;

                        temp[no_of_temp_maxima * 3] = x_accurate;
                        temp[(no_of_temp_maxima * 3) + 1] = diff;
                        temp[(no_of_temp_maxima * 3) + 2] = absdiff;
                        no_of_temp_maxima++;
                    }
                }
            }

            // compete
            prev_mag = temp[2];
            prev_x = (int)temp[0];
            for (i = 1; i < no_of_temp_maxima; i++)
            {
                mag = temp[(i * 3) + 2];
                x = (int)temp[i * 3];
                float x_diff = x - prev_x;
                if (x_diff <= inhibit_radius)
                {
                    if (prev_mag <= mag) temp[(i - 1) * 3] = -1;
                    if (mag < prev_mag) temp[i * 3] = -1;
                }

                prev_mag = mag;
                prev_x = x;
            }

            // populate maxima array
            for (i = 1; i < no_of_temp_maxima; i++)
            {
                if (temp[i * 3] > -1)
                {
                    for (int p = 0; p < 3; p++)
                        maxima[(no_of_maxima * 3) + p] = temp[(i * 3) + p];

                    no_of_maxima++;
                }
            }

            // sort edges        
            int search_max = no_of_maxima;
            if (search_max > max_maxima) search_max = max_maxima;
            int winner = -1;
            for (i = 0; i < search_max - 1; i++)
            {
                mag = maxima[(i * 3) + 2];
                winner = -1;
                for (int j = i + 1; j < no_of_maxima; j++)
                {
                    if (maxima[(j * 3) + 2] > mag)
                    {
                        winner = j;
                        mag = maxima[(j * 3) + 2];
                    }
                }
                if (winner > -1)
                {
                    // swap
                    for (int p = 0; p < 3; p++)
                    {
                        float temp2 = maxima[(i * 3) + p];
                        maxima[(i * 3) + p] = maxima[(winner * 3) + p];
                        maxima[(winner * 3) + p] = temp2;
                    }
                }
            }
            no_of_maxima = search_max;


            return (no_of_maxima);
        }

        /// <summary>
        /// calculate some local properties for each maxima, to assist
        /// with matching later on
        /// </summary>
        /// <param name="y">y coordinate within the image (row number)</param>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="no_of_maxima">the number of maxima in this row</param>
        /// <param name="feature_properties">properties associated with each maxima</param>
        /// <param name="disp_x"></param>
        /// <param name="disp_y"></param>
        private void update_feature_properties(
                                     int y, byte[] bmp,
                                     int wdth, int hght, int bytes_per_pixel,
                                     int no_of_maxima, float[] maxima,
                                     bool[,] feature_properties,
                                     int[] disp_x, int[] disp_y)
        {
            int xx, yy, startpos;

            startpos = y * wdth;
            for (int f = 0; f < no_of_maxima; f++)
            {
                int x = (int)maxima[f * 3];
                //if ((x > 1) && (x < wdth - 3))
                //{
                    int centre = (integral[x + 1] - integral[x - 1]);

                    int i = 0;
                    while (i < 16)
                    {
                        xx = x + disp_x[i];
                        yy = y + disp_y[i];
                        int offset = yy * wdth;
                        int surround = 0;
                        if ((xx > -1) && (xx < wdth - 3))
                            if ((yy > -1) && (yy < hght - 1))
                            {
                                surround = bmp[(offset + xx) * bytes_per_pixel];
                                surround += bmp[(((yy + 1) * wdth) + xx) * bytes_per_pixel];
                                surround += bmp[(offset + (xx + 1)) * bytes_per_pixel];
                                //surround += bmp[(offset + (xx + 2)) * bytes_per_pixel];
                                //surround += bmp[(offset + (xx - 1)) * bytes_per_pixel];
                            }
                        if (centre > surround)
                            feature_properties[f, i] = true;
                        else
                            feature_properties[f, i] = false;

                        i++;
                    }
                //}
            }

        }


        /// <summary>
        /// match maxima features in the left and right images using local
        /// feature properties
        /// </summary>
        /// <param name="y">y coordinate within the image (row number)</param>
        /// <param name="left_bmp">left image data</param>
        /// <param name="right_bmp">right image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="no_of_left_maxima">number of maxima in the left image</param>
        /// <param name="left_maxima">left maxima for this row</param>
        /// <param name="no_of_right_maxima">number of maxima in the right image</param>
        /// <param name="right_maxima">right maxima for this row</param>
        /// <param name="left_feature_properties">properties for each of the maxima in the left image</param>
        /// <param name="right_feature_properties">properties for each of the maxima in the right image</param>
        /// <param name="max_disparity_pixels">maximum disparity in pixels</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        private void match(int y, byte[] left_bmp, byte[] right_bmp,
                           int wdth, int hght, int bytes_per_pixel,
                           int no_of_left_maxima, float[] left_maxima,
                           int no_of_right_maxima, float[] right_maxima,
                           bool[,] left_feature_properties,
                           bool[,] right_feature_properties,
                           int max_disparity_pixels,
                           float calibration_offset_x, float calibration_offset_y)
        {
            bool positive, positive2;

            for (int f = 0; f < no_of_left_maxima; f++)
            {
                int x = (int)left_maxima[f * 3];
                if (left_maxima[(f * 3) + 1] >= 0)
                    positive = true;
                else
                    positive = false;

                int f2 = 0;
                float min_score = 999999;
                int winner = -1;
                while (f2 < no_of_right_maxima)
                {
                    float x2 = right_maxima[f2 * 3];
                    if (right_maxima[(f2 * 3) + 1] >= 0)
                        positive2 = true;
                    else
                        positive2 = false;
                    if (positive == positive2)
                    {
                        float disp = x - x2 + calibration_offset_x;
                        if ((disp > -1) && (disp < max_disparity_pixels))
                        {
                            int i = 0;
                            bool different = false;
                            while ((i < 16) && (!different))
                            {
                                if (left_feature_properties[f, i] != right_feature_properties[f2, i])
                                    different = true;
                                i++;
                            }
                            if (!different)
                            {
                                float score = disp;
                                if (score < min_score)
                                {
                                    winner = f2;
                                    min_score = score;
                                }
                            }
                        }
                    }
                    f2++;
                }
                if (winner > -1)
                {
                    // screen position
                    disparities[no_of_disparities * 4] = x;
                    disparities[(no_of_disparities * 4) + 1] = y;
                    // disparity value
                    float disp_value = left_maxima[f * 3] - right_maxima[winner * 3] + calibration_offset_x;
                    disparities[(no_of_disparities * 4) + 2] = disp_value;
                    // score for this disparity
                    disparities[(no_of_disparities * 4) + 3] = left_maxima[(f * 3) + 2] + right_maxima[(winner * 3) + 2];
                    no_of_disparities++;
                }
            }
        }

        public void getSelectedFeatures(MersenneTwister rnd)
        {
            int i, n, index;
            float fx, fy, disp;

            ///populate the selected_features array
            no_of_selected_features = 0;
            if (no_of_disparities > 0)
            {
                n = 0;
                for (i = 0; i < required_features; i++)
                {
                    index = (int)((rnd.Next(100000) * (no_of_disparities - 1)) / 100000);

                    fx = disparities[index * 4];
                    fy = disparities[(index * 4) + 1];
                    disp = disparities[(index * 4) + 2];

                    n = no_of_selected_features * 3;
                    selected_features[n] = fx;
                    selected_features[n + 1] = fy;
                    selected_features[n + 2] = disp;
                    no_of_selected_features++;
                }
            }
        }

        /// <summary>
        /// return the maximal features
        /// </summary>
        /// <param name="max_disparity_pixels"></param>
        public void getBestFeatures(int max_disparity_pixels)
        {
            int i, n, max;
            float fx, fy, disp, mag;
            int max_disp_thresh = max_disparity_pixels * 5 / 10;
            int big_disparities = 0;

            ///populate the selected_features array
            no_of_selected_features = 0;
            if (no_of_disparities > 0)
            {
                if (required_features > no_of_disparities)
                    max = no_of_disparities;
                else
                    max = required_features;

                // sort disparities by score
                int winner = -1;
                for (i = 0; i < max - 1; i++)
                {
                    mag = disparities[(i * 4) + 3];
                    winner = -1;
                    for (int j = i + 1; j < no_of_disparities; j++)
                    {
                        if (disparities[(j * 4) + 3] > mag)
                        {
                            winner = j;
                            mag = disparities[(j * 4) + 3];
                        }
                    }
                    if (winner > -1)
                    {
                        // swap
                        for (int p = 0; p < 4; p++)
                        {
                            float temp = disparities[(i * 4) + p];
                            disparities[(i * 4) + p] = disparities[(winner * 4) + p];
                            disparities[(winner * 4) + p] = temp;
                        }
                    }

                    // count the number of large disparities               
                    if (disparities[(i * 4) + 2] > max_disp_thresh) big_disparities++;
                }

                n = 0;
                for (i = 0; i < max - 1; i++)
                {
                    fx = disparities[i * 4];
                    fy = disparities[(i * 4) + 1];
                    disp = disparities[(i * 4) + 2];

                    //fx = 319;
                    //fy = 120;

                    if (!((big_disparities < max / 8) && (disp > max_disp_thresh)))
                    {
                        n = no_of_selected_features * 3;
                        selected_features[n] = fx;
                        selected_features[n + 1] = fy;
                        selected_features[n + 2] = disp;
                        no_of_selected_features++;
                    }
                }
            }
        }



        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp">left image data</param>
        /// <param name="right_bmp">right_image_data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        /// <param name="image_threshold"></param>
        /// <param name="peaks_per_row">the number of maxima per row</param>
        public void update(byte[] left_bmp, byte[] right_bmp,
                           int wdth, int hght, int bytes_per_pixel,
                           float calibration_offset_x, float calibration_offset_y,
                           int image_threshold, int peaks_per_row)
        {
            int y_step = 4;
            int radius = 3;
            int max_maxima = hght * peaks_per_row / y_step;  //max number of peaks per row

            if (!initialised)
            {
                disp_x = new int[24];
                disp_y = new int[24];
                integral = new int[wdth];
                left_maxima = new float[wdth * 3];
                right_maxima = new float[wdth * 3];
                temp = new float[wdth * 3];
                left_feature_properties = new bool[wdth, 16];
                right_feature_properties = new bool[wdth, 16];
                initialised = true;
            }

            int match_radius_x = matchRadius1 * wdth / 100;
            int match_radius_x2 = matchRadius2 * wdth / 100;
            int match_radius_y = matchRadius1 * hght / 100;
            int match_radius_y2 = matchRadius2 * hght / 100;
            disp_x[0] = -match_radius_x;
            disp_y[0] = 0;
            disp_x[1] = -match_radius_x;
            disp_y[1] = -match_radius_y;
            disp_x[2] = 0;
            disp_y[2] = -match_radius_y;
            disp_x[3] = match_radius_x;
            disp_y[3] = -match_radius_y;
            disp_x[4] = match_radius_x;
            disp_y[4] = 0;
            disp_x[5] = match_radius_x;
            disp_y[5] = match_radius_y;
            disp_x[6] = 0;
            disp_y[6] = match_radius_y;
            disp_x[7] = -match_radius_x;
            disp_y[7] = match_radius_y;

            disp_x[8] = -match_radius_x2;
            disp_y[8] = 0;
            disp_x[9] = -match_radius_x2;
            disp_y[9] = -match_radius_y2;
            disp_x[10] = 0;
            disp_y[10] = -match_radius_y2;
            disp_x[11] = match_radius_x2;
            disp_y[11] = -match_radius_y2;
            disp_x[12] = match_radius_x2;
            disp_y[12] = 0;
            disp_x[13] = match_radius_x2;
            disp_y[13] = match_radius_y2;
            disp_x[14] = 0;
            disp_y[14] = match_radius_y2;
            disp_x[15] = -match_radius_x2;
            disp_y[15] = match_radius_y2;

            int max_disparity_pixels = (wdth * max_disparity) / 100;
            int inhibit_radius = max_disparity_pixels / 2;

            int min_intensity = 10 * bytes_per_pixel * radius * 2;
            int max_intensity = 255 * bytes_per_pixel * radius * 2;

            no_of_disparities = 0;
            no_of_selected_features = 0;

            // for each row of the image
            for (int y = 0; y < hght; y += y_step)
            {
                // find peak values for a single row in the left image
                int no_of_left_maxima = row_maxima(y, left_bmp, wdth, hght, bytes_per_pixel, integral,
                                                   left_maxima, temp, radius, inhibit_radius, min_intensity, max_intensity,
                                                   max_maxima, image_threshold);
                // calculate some properties for each peak                                   
                update_feature_properties(y, left_bmp, wdth, hght, bytes_per_pixel,
                                          no_of_left_maxima, left_maxima, left_feature_properties,
                                          disp_x, disp_y);
                // find peak values for a single row in the right image
                int y2 = (int)Math.Round(y + calibration_offset_y);
                if ((y2 > -1) && (y2 < wdth))
                {
                    int no_of_right_maxima = row_maxima(y2, right_bmp, wdth, hght, bytes_per_pixel, integral,
                                                  right_maxima, temp, radius, inhibit_radius, min_intensity, max_intensity,
                                                  max_maxima, image_threshold);
                    // calculate some contextual properties for each peak
                    update_feature_properties(y2, right_bmp, wdth, hght, bytes_per_pixel,
                                              no_of_right_maxima, right_maxima, right_feature_properties,
                                              disp_x, disp_y);
                    // match the peaks!
                    match(y, left_bmp, right_bmp, wdth, hght, bytes_per_pixel,
                          no_of_left_maxima, left_maxima,
                          no_of_right_maxima, right_maxima,
                          left_feature_properties,
                          right_feature_properties,
                          max_disparity_pixels,
                          calibration_offset_x, calibration_offset_y);
                }
            }

            // return a fixed number of features
            //getSelectedFeatures();        
            getBestFeatures(max_disparity_pixels);
        }


    }
}
