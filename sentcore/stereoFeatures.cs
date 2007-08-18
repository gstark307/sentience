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

namespace sentience.core
{
    /// <summary>
    /// stores a list of stereo features
    /// </summary>
    public class stereoFeatures
    {
        public int no_of_features;
        public float[] features = null;
        public float[] uncertainties = null;
        public Byte[,] colour = null;

        #region "constructors"

        public stereoFeatures(int no_of_features)
        {
            this.no_of_features = no_of_features;
            features = new float[no_of_features*3];
            uncertainties = new float[no_of_features];
            for (int i = 0; i < no_of_features; i++) uncertainties[i] = 1;
            colour = new Byte[no_of_features, 3];
        }
        
        #endregion

        #region "matching functions"

        /// <summary>
        /// match this feature set with another
        /// </summary>
        /// <param name="other">the other feature set</param>
        /// <param name="match_indexes">array which contains the matching indexes</param>
        /// <param name="img_width">image width</param>
        /// <param name="img_height">image height</param>
        /// <param name="max_displacement">maximum horizontal displacement as a percentage 1-100</param>
        public void match(stereoFeatures other, int[] match_indexes, 
                          int img_width, int img_height,
                          int max_displacement_x, int max_displacement_y,
                          bool overwrite)
        {
            matchTemporal(other.no_of_features, other.features,
                          no_of_features, features,
                          match_indexes, img_width, img_height, 
                          max_displacement_x, max_displacement_y,
                          overwrite);
        }

        /// <summary>
        /// match two disparity feature sets separated in time
        /// </summary>
        /// <param name="prev_no_of_disparities">previous number of disparities</param>
        /// <param name="prev_disparities">previous disparity array</param>
        /// <param name="no_of_disparities">current number of disparities</param>
        /// <param name="disparities">current disparity array</param>
        /// <param name="match_indexes">returned array containing the indexes of the previous disaity array for matching features</param>
        /// <param name="img_width">image width</param>
        /// <param name="img_height">image height</param>
        /// <param name="max_displacement">maximum horizontal displacement as a percentage 1-100</param>
        private void matchTemporal(int prev_no_of_disparities, float[] prev_disparities,
                                  int no_of_disparities, float[] disparities,
                                  int[] match_indexes, int img_width, int img_height,
                                  int max_displacement_x, int max_displacement_y,
                                  bool overwrite)
        {
            int max_disp_x = max_displacement_x * img_width / 100;
            int max_disp_y = max_displacement_y * img_height / 100;

            for (int i = 0; i < no_of_disparities; i++)
            {
                if ((overwrite) || ((!overwrite) && (match_indexes[i] == -1)))
                {
                    int x = (int)disparities[i * 3];
                    int y = (int)disparities[(i * 3) + 1];
                    int min_dist = max_disp_x;
                    int winner = -1;

                    for (int j = 0; j < prev_no_of_disparities; j++)
                    {
                        int y2 = (int)prev_disparities[(j * 3) + 1];
                        int dy = y - y2;
                        if (dy < 0) dy = -dy;
                        if (dy < max_disp_y)
                        {
                            int x2 = (int)prev_disparities[j * 3];
                            int dx = x - x2;
                            if (dx < 0) dx = -dx;
                            if (dx < max_disp_x)
                            {
                                if (dx + dy < min_dist)
                                {
                                    min_dist = dx + dy;
                                    winner = j;
                                }
                            }
                        }
                    }
                    match_indexes[i] = winner;
                }
            }
        }
        
        #endregion

        /*
                private void matchTemporal(int prev_no_of_disparities, float[] prev_disparities,
                                          int no_of_disparities, float[] disparities,
                                          int[] match_indexes, int img_width, int img_height, int max_displacement)
                {
                    int idx = -1;
                    int prev_idx = -1;
                    int prev_idx2 = -1;
                    int py = -1;
                    int py2 = -1;
                    int py3 = -1;
                    int i, xx, xx2, yy, yy2;

                    for (int y = 0; y < img_height; y++)
                    {
                        while ((prev_idx < prev_no_of_disparities - 1) && (py < y - 4))
                        {
                            prev_idx++;
                            py = (int)(prev_disparities[(prev_idx * 3) + 1]);
                        }

                        prev_idx2 = prev_idx;
                        while ((prev_idx2 < prev_no_of_disparities - 1) && (py2 < y + 4))
                        {
                            prev_idx2++;
                            py2 = (int)(prev_disparities[(prev_idx2 * 3) + 1]);
                        }

                        while ((idx < no_of_disparities - 1) && (py3 < y))
                        {
                            idx++;
                            py3 = (int)(disparities[(idx * 3) + 1]);
                        }

                        i = idx;
                        yy = y;
                        int dx, dy, min_dx, winner;
                        while ((i < no_of_disparities - 1) && (yy == y))
                        {
                            match_indexes[i] = -1;
                            yy = (int)(disparities[(i * 3) + 1]);
                            if (yy == y)
                            {
                                xx = (int)(disparities[i * 3]);
                                min_dx = max_displacement * img_width / 100;
                                winner = -1;
                                for (int prev_i = prev_idx; prev_i <= prev_idx2; prev_i++)
                                {
                                    xx2 = (int)(prev_disparities[prev_i * 3]);
                                    yy2 = (int)(prev_disparities[(prev_i * 3) + 1]);
                                    dx = xx2 - xx;
                                    dy = yy2 - yy;
                                    if (dy < 0) dy = -dy;
                                    if (dx < 0) dx = -dx;
                                    if (dx + dy < min_dx)
                                    {
                                        min_dx = dx + dy;
                                        winner = prev_i;
                                    }
                                }

                                match_indexes[i] = winner;
                                i++;
                            }

                        }

                    }
                }
        */


        #region "saving and loading"
        
        /// <summary>
        /// save stereo features to file
        /// </summary>
        /// <param name="binfile">file to save to</param>
        public void save(BinaryWriter binfile)
        {
            binfile.Write(no_of_features);
            for (int i = 0; i < no_of_features*3; i++)
                binfile.Write(features[i]);

            for (int i = 0; i < no_of_features; i++)
                for (int j = 0; j < 3; j++)
                    binfile.Write(colour[i, j]);
        }

        /// <summary>
        /// load stereo features from file
        /// </summary>
        /// <param name="binfile">file to load from</param>
        public void load(BinaryReader binfile)
        {
            try
            {
                no_of_features = binfile.ReadInt32();
                features = new float[no_of_features];
                for (int i = 0; i < no_of_features * 3; i++)
                    features[i] = binfile.ReadSingle();

                colour = new Byte[no_of_features, 3];
                for (int i = 0; i < no_of_features; i++)
                    for (int j = 0; j < 3; j++)
                        colour[i, j] = binfile.ReadByte();
            }
            catch
            {
            }
        }
        
        #endregion

    }
}
