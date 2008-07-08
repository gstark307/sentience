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

namespace sentience.core
{
    /// <summary>
    /// represents a 2D feature being tracked
    /// </summary>
    public class sentienceTrackingFeature
    {
        #region "variables"
        
        public int ID;        // an ID number for the feature
        public float x, y;    // 2D position of the feature within the image
        public float vx, vy;  // the current 2D velocity of the feature
        public float predicted_x, predicted_y;  // predicted 2D position of the feature within the image
        public float average_disparity;  // stereo disparity of the feature averaged over time
        public float total_disparity;    // an accumulated value for stereo disparity
        public int persistence = 0;      // the length of time that this feature has been observed (ticks)
        
        #endregion
        
        #region "constructors"
        
        public sentienceTrackingFeature(int ID, int x, int y, float disp)
        {
            this.ID = ID;
            this.x = x;
            this.y = y;
            vx = 0;
            vy = 0;
            predicted_x = x;
            predicted_y = y;
            average_disparity = disp;
            total_disparity = 0;
        }
        
        #endregion

        #region "update state"
        
        /// <summary>
        /// update the position of the feature
        /// </summary>
        /// <param name="new_x">new 2D x position of the feature within the image</param>
        /// <param name="new_y">new 2D y position of the feature within the image</param>
        public void updatePosition(int new_x, int new_y)
        {
            // calculate the speed of movement of the feature
            vx = new_x - x;
            vy = new_y - y;

            // apply a deadband to allow for small amounts of pixel jitter
            if ((vx > -3) && (vx < 3)) vx = 0;
            if ((vy > -3) && (vy < 3)) vy = 0;

            // update the fature position
            x = new_x;
            y = new_y;

            // preduct the next position of the feature
            predicted_x = x + vx;
            predicted_y = y + vy;
        }
        
        #endregion
    }

    /// <summary>
    /// class used to track stereo features over time
    /// </summary>
    public class sentienceTracking
    {
        const int history_steps = 3;
        int curr_step = 0;
        private int max_ID = 1;
        private ArrayList matched_index_list = new ArrayList();
        private sentienceTrackingFeature[,] IDs = null;
        //private stereoFeatures prev_features = null;
        private ArrayList prev_feature_list = new ArrayList();

        // maximum displacement of features per time step
        // as a percentahe of the image width/height
        public int max_displacement_x = 15;
        public int max_displacement_y = 25;

        // a value used to reduce the uncertainty of tracked feature disparities
        public float uncertainty_gain = 0.1f;

        /// <summary>
        /// reset the tracking
        /// </summary> 
        public void reset()
        {
            matched_index_list = new ArrayList();
        }

        /// <summary>
        /// update stereo feature tracking
        /// </summary>
        /// <param name="features">stereo features</param>
        /// <param name="img_width">image width</param>
        /// <param name="img_height">image height</param>
        public void update(stereoFeatures features, 
                           int img_width, int img_height)
        {
            stereoFeatures prev_features = null;

            if (matched_index_list.Count == 0)
            {
                IDs = new sentienceTrackingFeature[history_steps, 1000];
                for (int h = 0; h < history_steps; h++)
                {
                    matched_index_list.Add(new int[1000]);
                    for (int i = 0; i < 1000; i++)
                        IDs[h, i] = null;
                }
            }

            int prev_step = curr_step - 1;
            if (prev_step < 0) prev_step += history_steps;
            int[] matched_indexes = (int[])matched_index_list[curr_step];
            int[] prev_matched_indexes = (int[])matched_index_list[prev_step];

            if (prev_feature_list.Count > 1)
            {
                int prev_step2 = curr_step - 2;
                if (prev_step2 < 0) prev_step2 += history_steps;

                prev_features = (stereoFeatures)prev_feature_list[prev_step];
                stereoFeatures prev_features2 = (stereoFeatures)prev_feature_list[prev_step2];

                // predict the positions of features using the velocity values
                for (int i = 0; i < prev_features.no_of_features; i++)
                {
                    if (IDs[prev_step, i] != null)
                    {
                        if ((IDs[prev_step, i].vx != 0) || ((IDs[prev_step, i].vy != 0)))
                        {
                            prev_features.features[(i * 3)] = IDs[prev_step, i].predicted_x;
                            prev_features.features[(i * 3) + 1] = IDs[prev_step, i].predicted_y;
                        }
                    }
                }

                // match predicted feature positions with the currently observed ones                
                features.match(prev_features, matched_indexes,
                               img_width, img_height,
                               max_displacement_x, max_displacement_y, true);

                // fill in any gaps
                
                features.match(prev_features2, prev_matched_indexes,
                               img_width, img_height,
                               max_displacement_x, max_displacement_y, true);
                 

                for (int i = 0; i < features.no_of_features; i++)
                {
                    // if the feature has been matched with a previous one update its ID
                    if (matched_indexes[i] > -1)
                        IDs[curr_step, i] = IDs[prev_step, matched_indexes[i]];
                    else
                    {
                        if (prev_matched_indexes[i] > -1)
                            IDs[curr_step, i] = IDs[prev_step2, prev_matched_indexes[i]];
                        else
                            IDs[curr_step, i] = null;
                    }
                }
            }

            // update the persistence and average disparity for observed features
            for (int i = 0; i < features.no_of_features; i++)
            {
                int x = (int)features.features[i * 3];
                int y = (int)features.features[(i * 3) + 1];
                float disp = features.features[(i * 3) + 2];

                if (IDs[curr_step, i] != null)
                {
                    int dx = (int)(x - IDs[curr_step, i].predicted_x);
                    if (dx < 0) dx = -dx;
                    IDs[curr_step, i].updatePosition(x, y);

                    float av_disparity = 0;
                    if (dx < 50)
                    {
                        // if the feature has not moved very much
                        IDs[curr_step, i].persistence++;
                        IDs[curr_step, i].total_disparity += disp;
                        av_disparity = IDs[curr_step, i].total_disparity / IDs[curr_step, i].persistence;
                    }
                    else
                    {
                        av_disparity = (IDs[curr_step, i].average_disparity * 0.9f) +
                                                   (disp * 0.1f);                        
                    }

                    if (av_disparity > 0)
                    {
                        float disp_change = (av_disparity - disp) / av_disparity;
                        float disp_confidence = 1.0f / (1.0f + (disp_change * disp_change));
                        int idx = matched_indexes[i];
                        if (idx > -1)
                        {
                            features.uncertainties[i] = prev_features.uncertainties[idx] -
                                                        ((prev_features.uncertainties[idx] * disp_confidence * uncertainty_gain));
                            if (features.uncertainties[i] < 0.2f) features.uncertainties[i] = 0.2f;
                            features.uncertainties[i] *= 1.02f;
                            if (features.uncertainties[i] > 1) features.uncertainties[i] = 1;
                        }
                    }

                    IDs[curr_step, i].average_disparity = av_disparity;
                    features.features[(i * 3) + 2] = IDs[curr_step, i].average_disparity;
                }

                // create a new tracking feature
                if (IDs[curr_step, i] == null)
                {
                    IDs[curr_step, i] = new sentienceTrackingFeature(max_ID, x, y, disp);
                    max_ID++;
                    if (max_ID > 30000) max_ID = 1;
                }
            }

            curr_step++;
            if (curr_step >= history_steps) curr_step = 0;
            if (prev_feature_list.Count < history_steps)
                prev_feature_list.Add(features);
            else
                prev_feature_list[curr_step] = features;
            //prev_features = features;
        }
    }
}
