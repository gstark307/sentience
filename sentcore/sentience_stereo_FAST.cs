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
using System.Collections;
using sluggish.imageprocessing.FASTcorners;

namespace sentience.core
{
    /// <summary>
    /// stereo correspondence based upon FAST corners
    /// </summary>
    public class sentience_stereo_FAST
    {
        public const int MAX_POINT_FEATURES = 6000;

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

        ///all image disparities within a big list
        public int no_of_disparities;
        public float[] disparities;

        /// <summary>
        /// constructor
        /// This sets some initial default values.
        /// </summary>
        public sentience_stereo_FAST()
        {
            required_features = 200;

            ///array storing a fixed quantity of selected features
            no_of_selected_features = 0;
            selected_features = new float[MAX_POINT_FEATURES * 3];

            //no_of_disparities = 0;
            //disparities = new float[MAX_POINT_FEATURES * 4];

            /// maximum disparity 5% of image width
            max_disparity = 5;
        }


        private void subPixelLocation(Byte[] img, int width, int height,
                                      int x, int y, int radius_x, int radius_y,
                                      ref float fx, ref float fy)
        {
            float tot = 0;
            fx = 0;
            fy = 0;
            for (int xx = x - radius_x; xx < x + radius_x; xx++)
            {
                if ((xx > 0) && (xx < width-1))
                {
                    for (int yy = y - radius_y; yy < y + radius_y; yy++)
                    {
                        if ((yy > 0) && (yy < height - 1))
                        {
                            int n = (yy * width) + xx;
                            int value = Math.Abs(img[n] - img[n - 1]) + 
                                        Math.Abs(img[n] - img[n + 1]);
                            value *= value;
                            tot += value;
                            fx += value * xx;
                            fy += value * yy;
                        }
                    }
                }
            }
            if (tot > 0)
            {
                fx /= tot;
                fy /= tot;
            }
        }

        public void update(Byte[] left_bmp, Byte[] right_bmp,
                           int wdth, int hght, int bytes_per_pixel,
                           float calibration_offset_x, float calibration_offset_y,
                           ref int threshold)
        {
            Byte[] left_img;
            Byte[] right_img;
            int max_disparity_pixels = wdth * max_disparity / 100;

            if (bytes_per_pixel == 1)
            {
                left_img = left_bmp;
                right_img = right_bmp;
            }
            else
            {
                left_img = new Byte[wdth * hght];
                right_img = new Byte[wdth * hght];
                int n = 0;
                for (int i = 0; i < wdth * hght * bytes_per_pixel; i += bytes_per_pixel)
                {
                    int left_tot = 0;
                    int right_tot = 0;
                    for (int c = 0; c < bytes_per_pixel; c++)
                    {
                        left_tot += left_bmp[i + c];
                        right_tot += right_bmp[i + c];
                    }
                    left_img[n] = (Byte)(left_tot / bytes_per_pixel);
                    right_img[n] = (Byte)(right_tot / bytes_per_pixel);
                    n++;
                }
            }

            // set a default threshold value if none is specified
            if (threshold == 0) threshold = 50;

            // extract features from the left image
            FASTcorner[] left_corners_all = FAST.fast_corner_detect_10(left_img, wdth, hght, threshold);
            FASTcorner[] left_corners = FAST.fast_nonmax(left_img, wdth, hght, left_corners_all, threshold * 2, 0, 0);

            // only continue if there aren't too many features
            no_of_selected_features = 0;
            if (left_corners != null)
            {
                if (left_corners.Length < required_features * 2)
                {
                    // extract features from the right image
                    FASTcorner[] right_corners_all = FAST.fast_corner_detect_10(right_img, wdth, hght, threshold);
                    FASTcorner[] right_corners = FAST.fast_nonmax(right_img, wdth, hght, right_corners_all, threshold * 2, -calibration_offset_x, -calibration_offset_y);
                    if (right_corners != null)
                    {
                        // update feature properties used for matching
                        FAST.fast_update(left_corners, wdth / 5, hght / 5);
                        FAST.fast_update(right_corners, wdth / 5, hght / 5);

                        // adjust the threshold
                        int no_of_feats = (left_corners.Length + right_corners.Length) / 2;
                        if (no_of_feats < required_features / 2) threshold -= 4;
                        if (no_of_feats > required_features) threshold += 4;

                        // this is a test
                        int n = 0;
                        /*
                        for (int i = 0; i < left_corners.Length; i++)
                        {
                            FASTcorner corner = left_corners[i];
                            selected_features[(n * 3)] = corner.x;
                            selected_features[(n * 3) + 1] = corner.y;
                            selected_features[(n * 3) + 2] = 3;
                            n++;
                        }
                        for (int i = 0; i < right_corners.Length; i++)
                        {
                            FASTcorner corner = right_corners[i];
                            selected_features[(n * 3)] = corner.x;
                            selected_features[(n * 3) + 1] = corner.y;
                            selected_features[(n * 3) + 2] = 3;
                            n++;
                        }
                        */
                        no_of_selected_features = n;

                        // bucket the data into rows
                        // this helps to make matching more efficient
                        ArrayList[] left_row = new ArrayList[hght];
                        ArrayList[] right_row = new ArrayList[hght];
                        for (int i = 0; i < left_corners.Length; i++)
                        {
                            int index = left_corners[i].y;
                            if (left_row[index] == null) left_row[index] = new ArrayList();
                            left_row[index].Add(left_corners[i]);
                        }
                        for (int i = 0; i < right_corners.Length; i++)
                        {
                            int index = right_corners[i].y;
                            if (right_row[index] == null) right_row[index] = new ArrayList();
                            right_row[index].Add(right_corners[i]);
                        }

                        // match rows
                        int vertical_search = 0;
                        for (int y = 0; y < hght; y++)
                        {
                            ArrayList row = left_row[y];
                            if (row != null)
                            {
                                for (int f1 = 0; f1 < row.Count; f1++)
                                {
                                    FASTcorner corner1 = (FASTcorner)row[f1];
                                    int min_score = 60;
                                    float disp = -1;

                                    for (int yy = y - vertical_search; yy <= y + vertical_search; yy++)
                                    {
                                        if ((yy > -1) && (yy < hght))
                                        {
                                            ArrayList row2 = right_row[yy];
                                            if (row2 != null)
                                            {
                                                for (int f2 = 0; f2 < row2.Count; f2++)
                                                {
                                                    FASTcorner corner2 = (FASTcorner)row2[f2];
                                                    int dx = corner1.x - corner2.x;
                                                    if ((dx >= 0) && (dx < max_disparity_pixels))
                                                    {
                                                        int dy = yy - y;
                                                        int score = corner1.matching_score(corner2) * (Math.Abs(dy)+1);
                                                        //int score = Math.Abs(dy) + dx;
                                                        if ((score > -1) && ((min_score == -1) || (score < min_score)))
                                                        {
                                                            min_score = score;
                                                            float left_fx=0, right_fx=0, fy=0;
                                                            subPixelLocation(left_img, wdth, hght, corner1.x, corner1.y, 5, 1, ref left_fx, ref fy);
                                                            subPixelLocation(right_img, wdth, hght, corner2.x, corner2.y, 5, 1, ref right_fx, ref fy);
                                                            disp = left_fx - right_fx;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (disp > -1)
                                    {
                                        if ((corner1.x > -1) && (corner1.x < 320))
                                        {
                                            
                                            selected_features[(no_of_selected_features * 3)] = corner1.x;
                                            selected_features[(no_of_selected_features * 3) + 1] = corner1.y;
                                            selected_features[(no_of_selected_features * 3) + 2] = disp;
                                            no_of_selected_features++;
                                            
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else threshold -= 4;
                }
                else threshold += 4;
            }
            else threshold -= 4;
            if (threshold < 10) threshold = 10;
        }


    }
}
