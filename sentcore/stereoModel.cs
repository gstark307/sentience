/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CenterSpace.Free;

namespace sentience.core
{
    public class stereoModel
    {
        Random rnd = new Random();

        public bool mapping = true;

        // field of vision
        public float FOV_horizontal = 78 * (float)Math.PI / 180.0f;
        public float FOV_vertical = 78/2 * (float)Math.PI / 180.0f;
        public int image_width = 320;
        public int image_height = 240;
        public float focal_length = 5; // mm
        public float baseline = 100; // mm
        public float sigma = 0.04f; //0.005f;  //angular uncertainty magnitude (standard deviation) pixels per mm of range
        private float max_prob = 0.0f;
        private int max_prob_x, max_prob_y;
        private int starting_y;
        public bool undistort = true;

        private int ray_model_length = 1500;
        //private int ray_model_width = 200;
        private float[,] ray_model = null;

        // variables used to show the survey scores
        //private float max_trial_pose_score = 1;
        //private float prev_max_trial_pose_score = 1;
        //private float[] TrialPoseScore = new float[2000];
        //private float[] prevTrialPoseScore = new float[2000];

        // adjustment factor for peak probability density
        private float peak_density_adjust = 0;

        // number of features to use when updating or probing the grid
        public int no_of_stereo_features = 100;

        public int divisor = 1;

        // Yes, it's prime time...
        private int[] primes = {    
   1019,   1021,   1031,   1033,   1039,  1049,  1051,   1061,   1063,   1069, 
   1087,   1091,  1093,   1097,   1103,   1109,   1117,   1123,   1129,   1151, 
   1153,   1163,  1171,   1181,   1187,   1193,   1201,   1213,   1217,  1223, 
   1229,   1231,   1237,   1249,   1259,   1277,   1279,   1283,   1289,   1291, 
   1297,   1301,   1303,   1307,   1319,   1321,   1327,   1361,   1367,   1373, 
   1381,   1399,   1409,   1423,   1427,   1429,   1433,   1439,   1447,   1451, 
   1453,   1459,   1471,   1481,   1483,   1487,   1489,   1493,   1499,   1511, 
   1523,   1531,   1543,   1549,   1553,   1559,   1567,   1571,   1579,   1583, 
   1597,   1601,   1607,   1609,   1613,   1619,   1621,   1627,   1637,   1657, 
   1663,   1667,   1669,   1693,   1697,   1699,   1709,   1721,   1723,   1733, 
   1741,   1747,   1753,   1759,   1777,   1783,   1787,   1789,   1801,   1811, 
   1823,   1831,   1847,   1861,   1867,   1871,   1873,   1877,   1879,   1889, 
   1901,   1907,   1913,   1931,   1933,   1949,   1951,   1973,   1979,   1987, 
   1993,   1997,   1999,   2003,   2011,   2017,   2027,   2029,   2039,   2053, 
   2063,   2069,   2081,   2083,   2087,   2089,   2099,   2111,   2113,   2129, 
   2131,   2137,   2141,   2143,   2153,   2161,   2179,   2203,   2207,   2213, 
   2221,   2237,   2239,   2243,   2251,   2267,   2269,   2273,   2281,   2287, 
   2293,   2297,   2309,   2311,   2333,   2339,   2341,   2347,   2351,   2357, 
   2371,   2377,   2381,   2383,   2389,   2393,   2399,   2411,   2417,   2423, 
   2437,   2441,   2447,   2459,   2467,   2473,   2477,   2503,   2521,   2531, 
   2539,   2543,   2549,   2551,   2557,   2579,   2591,   2593,   2609,   2617, 
   2621,   2633,   2647,   2657,   2659,   2663,   2671,   2677,   2683,   2687, 
   2689,   2693,   2699,   2707,   2711,   2713,   2719,   2729,   2731,   2741, 
   2749,   2753,   2767,   2777,   2789,   2791,   2797,   2801,   2803,   2819, 
   2833,   2837,   2843,   2851,   2857,   2861,   2879,   2887,   2897,   2903, 
   2909,   2917,   2927,   2939,   2953,   2957,   2963,   2969,   2971,   2999, 
   3001,   3011,   3019,   3023,   3037,   3041,   3049,   3061,   3067,   3079, 
   3083,   3089,   3109,   3119,   3121,   3137,   3163,   3167,   3169,   3181, 
   3187,   3191,   3203,   3209,   3217,   3221,   3229,   3251,   3253,   3257, 
   3259,   3271,   3299,   3301,   3307,   3313,   3319,   3323,   3329,   3331, 
   3343,   3347,   3359,   3361,   3371,   3373,   3389,   3391,   3407,   3413, 
   3433,   3449,   3457,   3461,   3463,   3467,   3469,   3491,   3499,   3511, 
   3517,   3527,   3529,   3533,   3539,   3541,   3547,   3557,   3559,   3571, 
   3581,   3583,   3593,   3607,   3613,   3617,   3623,   3631,   3637,   3643, 
   3659,   3671,   3673,   3677,   3691,   3697,   3701,   3709,   3719,   3727, 
   3733,   3739,   3761,   3767,   3769,   3779,   3793,   3797,   3803,   3821, 
   3823,   3833,   3847,   3851,   3853,   3863,   3877,   3881,   3889,   3907, 
   3911,   3917,   3919,   3923,   3929,   3931,   3943,   3947,   3967,   3989, 
   4001,   4003,   4007,   4013,   4019,   4021,   4027,   4049,   4051,   4057, 
   4073,   4079,   4091,   4093,   4099,   4111,   4127,   4129,   4133,   4139, 
   4153,   4157,   4159,   4177,   4201,   4211,   4217,   4219,   4229,   4231, 
   4241,   4243,   4253,   4259,   4261,   4271,   4273,   4283,   4289,   4297, 
   4327,   4337,   4339,   4349,   4357,   4363,   4373,   4391,   4397,   4409, 
   4421,   4423,  4441,   4447,   4451,   4457,   4463,   4481,   4483,   4493, 
   4507,   4513,   4517,   4519,   4523,   4547,   4549,   4561,   4567,   4583, 
   4591,   4597,   4603,   4621,   4637,   4639,   4643,   4649,   4651,   4657, 
   4663,   4673,   4679,   4691,   4703,   4721,   4723,   4729,   4733,   4751, 
   4759,   4783,   4787,   4789,   4793,   4799,   4801,   4813,   4817,   4831,
   4861,   4871,   4877,   4889,   4903,   4909,   4919,   4931,   4933,   4937, 
   4943,   4951,   4957,   4967,   4969,   4973,   4987,   4993,   4999,   5003, 
   5009,   5011,   5021,   5023,   5039,   5051,   5059,   5077,  5081,   5087 };

        /// <summary>
        /// creates a ray model
        /// Grid dimension should be 1000
        /// </summary>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="img"></param>
        public void updateRayModel(float[, ,] grid_layer, int grid_dimension, Byte[] img, int img_width, int img_height, int divisor, bool apply_smoothing)
        {
            // half a pixel of horizontal uncertainty
            sigma = 1.8f / (image_width * 2) * FOV_horizontal;
            sigma *= image_width / 320;
            this.divisor = divisor;

            int max_disparity = 10 * image_width / 320;

            ray_model_length = grid_dimension;
            if (ray_model == null)
                ray_model = new float[max_disparity, ray_model_length];

            int min_dist = (int)baseline;
            int max_dist = grid_dimension;
            int x = (image_width)*499/1000;

            for (int disparity_pixels = 3; disparity_pixels < max_disparity; disparity_pixels++)
            {
                int xx = x + disparity_pixels;

                // clear the grid
                max_prob = 0.0f;
                for (int gx = 0; gx < grid_dimension / divisor; gx++)
                    for (int gy = 0; gy < grid_dimension; gy++)
                    {
                        grid_layer[gx, gy, 0] = 0;
                        grid_layer[gx, gy, 1] = 0;
                        grid_layer[gx, gy, 2] = 0;
                    }

                float x_start = 0, y_start = 0;
                float x_end = 0, y_end = 0;
                float x_left = 0, y_left = 0;
                float x_right = 0, y_right = 0;
                float confidence = 1;
                raysIntersection(xx, x, grid_dimension, confidence,
                                 ref x_start, ref y_start, ref x_end, ref y_end,
                                 ref x_left, ref y_left, ref x_right, ref y_right);
                if (x_right > x_left)
                {
                    if (y_start < -1) y_start = -y_start;
                    if (y_end < -1) y_end = -y_end;

                    if (y_start > 0)
                        min_dist = (int)y_start;
                    else
                        min_dist = 100;

                    max_dist = grid_dimension;
                    if ((y_end > 0) && (y_end < grid_dimension))
                        max_dist = (int)y_end;

                    throwRay(-baseline / 2, xx, min_dist, max_dist, grid_layer, grid_dimension, 1);
                    throwRay(baseline / 2, x, min_dist, max_dist, grid_layer, grid_dimension, 2);

                    int start = -1;

                    float min_prob = 0.0f;
                    int max_length = 0;
                    int x2 = (grid_layer.GetLength(0) / 2);
                    int winner = x2;
                    float total_probability = 0;
                    for (int xx2 = x2 - (grid_dimension / (divisor * 4)); xx2 <= x2 + (grid_dimension / (divisor * 4)); xx2++)
                    {
                        int length = 0;
                        float tot = 0;
                        for (int l = 0; l < ray_model_length; l++)
                            if ((grid_layer[xx2, l, 2] == 2) && 
                                (grid_layer[xx2, l, 0] > 1))
                            {
                                float cellval = grid_layer[xx2, l, 1];
                                if (cellval > min_prob)
                                {
                                    length++;
                                    tot += cellval;
                                }
                            }
                        if (length > max_length)
                        {
                            max_length = length;
                            winner = xx2;
                            total_probability = tot;
                        }
                    }
                    float scaling_factor = 1;
                    if (max_length > 0) scaling_factor = 1.0f / total_probability;

                    float max = 0;
                    int max_index = 0;
                    for (int l = 0; l < ray_model_length; l++)
                    {
                        int y2 = ray_model_length-1-l;
                        if ((y2 > -1) && (y2 < grid_dimension))
                        {
                            if ((grid_layer[winner, y2, 2] == 2) && 
                                (grid_layer[winner, y2, 1] != 0) && 
                                (grid_layer[winner, y2, 0] > 1))
                            {
                                float cellval = grid_layer[winner, y2, 1] * scaling_factor;
                                if (cellval > min_prob)
                                {
                                    if (start == -1) start = l;
                                    ray_model[disparity_pixels, l - start] = cellval;
                                    if (cellval > max)
                                    {
                                        max = cellval;
                                        max_index = l - start;
                                    }
                                }
                            }
                        }
                    }

                    
                    if (apply_smoothing)
                    {
                        float[] probvalues = new float[ray_model_length];
                        int radius = 20;
                        for (int itt = 0; itt < 10; itt++)
                        {
                            for (int i = 0; i < ray_model_length; i++)
                            {
                                float value = 0;
                                int hits = 0;
                                for (int j = i - radius; j < i + radius; j++)
                                {
                                    if ((j >= 0) && (j < ray_model_length))
                                    {
                                        value += ((j - (i - radius)) * ray_model[disparity_pixels, j]);
                                        hits++;
                                    }
                                }
                                if (hits > 0) value /= hits;
                                probvalues[i] = value;
                            }
                            for (int i = 0; i < ray_model_length; i++)
                                if (ray_model[disparity_pixels, i] > max/200.0f)
                                    ray_model[disparity_pixels, i] = probvalues[i];
                        }
                    }                    

                    ray_model_to_graph_image(img, img_width, img_height);
                }
            }
            this.divisor = 1;
        }

        /// <summary>
        /// show a single ray model, with both occupancy and vacancy
        /// </summary>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="divisor"></param>
        public void showSingleRay(float[, ,] grid_layer, int grid_dimension, Byte[] img, int img_width, int img_height, int divisor)
        {
            // half a pixel of horizontal uncertainty
            sigma = 1.8f / (image_width * 2) * FOV_horizontal;
            sigma *= image_width / 320;
            this.divisor = divisor;

            int min_dist = (int)baseline;
            int max_dist = grid_dimension;
            int x = (image_width) * 499 / 1000;

            int disparity_pixels = 3;

            int xx = x + disparity_pixels;

            // clear the grid
            max_prob = 0.0f;
            for (int gx = 0; gx < grid_dimension / divisor; gx++)
                for (int gy = 0; gy < grid_dimension; gy++)
                {
                    grid_layer[gx, gy, 0] = 0;
                    grid_layer[gx, gy, 1] = 0;
                    grid_layer[gx, gy, 2] = 0;
                }

            float x_start = 0, y_start = 0;
            float x_end = 0, y_end = 0;
            float x_left = 0, y_left = 0;
            float x_right = 0, y_right = 0;
            float confidence = 1;
            raysIntersection(xx, x, grid_dimension, confidence,
                                 ref x_start, ref y_start, ref x_end, ref y_end,
                                 ref x_left, ref y_left, ref x_right, ref y_right);
            if (x_right > x_left)
            {
                if (y_start < -1) y_start = -y_start;
                if (y_end < -1) y_end = -y_end;

                if (y_start > 0)
                    min_dist = (int)y_start;
                else
                    min_dist = 100;

                max_dist = grid_dimension;
                if ((y_end > 0) && (y_end < grid_dimension))
                    max_dist = (int)y_end;

                min_dist = 0;

                throwRay(-baseline / 2, xx, min_dist, max_dist, grid_layer, grid_dimension, 1);
                throwRay(baseline / 2, x, min_dist, max_dist, grid_layer, grid_dimension, 2);
                grid_layer_to_image(grid_layer, grid_dimension, img, img_width, img_height, true);
            }
            this.divisor = 1;
        }


        #region "old stuff no longer used"

        /*
        /// <summary>
        /// Returns a trial pose using a simplistic gaussian uncertainty
        /// This is only used in situations where no motion model is available
        /// </summary>
        /// <param name="index"></param>
        /// <param name="max_index"></param>
        /// <param name="translation_x_tollerance"></param>
        /// <param name="translation_y_tollerance"></param>
        /// <param name="angular_offset"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void getTrialPose(int index, int max_index,
                                  float translation_x_tollerance, float translation_y_tollerance,
                                  float angular_offset, ref float x, ref float y)
        {
            int whorls = 10 + ((max_index - 200) / 70);
            float max_angle = 2 * 3.1415927f * whorls;
            const float offset = 1.5f;
            float rand_angle = offset + (index * (max_angle - offset) / (float)max_index);
            float ang = rand_angle * 0.85f / max_angle;
            rand_angle = rand_angle * (1.0f + (((float)Math.PI - 1.0f) * 0.5f * (1.0f - ang) * (1.0f - ang)));
            float rand_radius = rand_angle / max_angle;

            rand_angle += angular_offset;
            x = rand_radius * translation_x_tollerance / 2 * (float)Math.Sin(rand_angle);
            y = rand_radius * translation_y_tollerance / 2 * (float)Math.Cos(rand_angle);
        }

        /// <summary>
        /// surveys the given x,y location for optimum pan direction
        /// </summary>
        /// <param name="current_view"></param>
        /// <param name="map_view"></param>
        /// <param name="separation_tollerance">separation tollerance for limiting the search</param>
        /// <param name="ray_thickness">ray thickness</param>
        /// <param name="x">x location at which to perform the survey</param>
        /// <param name="y">y location at which to perform the survey</param>
        /// <returns></returns>
        public float[] surveyPan(viewpoint current_view, occupancygridMultiResolution grid,
                                 float ray_thickness,
                                 float x, float y)
        {
            const int no_of_angles = 50;
            float[] results = new float[no_of_angles];
            float max = 0;
            pos3D centre = new pos3D(0, 0, 0);

            for (int ang = 0; ang < no_of_angles; ang++)
            {
                float pan = ang * ((float)Math.PI / 2.0f) / no_of_angles;
                pan -= (float)Math.PI / 4.0f;

                // generate the trial pose using this pan angle
                viewpoint trialPose = current_view.createTrialPose(pan, x, y);

                // what's the score at this angle ?
                grid.insert(trialPose, false, centre);
                float score = grid.matchedCells();
                if (score > max) max = score;
                results[ang] = score;
            }
            if (max > 0)
            {
                // normalise the results
                for (int ang = 0; ang < no_of_angles; ang++)
                    results[ang] /= max;
            }
            return (results);
        }

        /// <summary>
        /// Monte Carlo Localisation:
        /// Tries to match a viewpoint within a grid and returns a list of scores
        /// for the tried poses
        /// </summary>        
        public ArrayList surveyXYP(viewpoint current_view, occupancygridMultiResolution grid,
                                  float translation_x_tollerance, float translation_y_tollerance,
                                  int no_of_trial_poses,
                                  int ray_thickness, bool pruneSurvey, int randomSeed,
                                  int pruneThreshold, float survey_angular_offset,
                                  pos3D local_odometry_position, float momentum,
                                  ref float max_score)
        {
            // let's twist again...
            MersenneTwister randGen = new MersenneTwister(randomSeed);

            int no_of_orientations = 1000;
            int bucket = 0;
            float score = 0;
            float x = 0, y = 0;
            viewpoint trialPose;
            ArrayList survey_results = new ArrayList();
            float[] orientation_bucket = new float[no_of_orientations];

            float half_x = translation_x_tollerance / 2;
            float half_y = translation_y_tollerance / 2;

            float pan_tollerance = (float)Math.PI / 4.0f;

            // swap arrays for trial pose scores
            prev_max_trial_pose_score = max_trial_pose_score;
            float[] tempScores = prevTrialPoseScore;
            prevTrialPoseScore = TrialPoseScore;
            TrialPoseScore = tempScores;

            // examine the hood
            max_score = 0;
            for (int t = 0; t < no_of_trial_poses; t++)
            {
                // get some parameters which will be used to construct the trial pose
                getTrialPose(t, no_of_trial_poses, translation_x_tollerance,
                         translation_y_tollerance, survey_angular_offset, ref x, ref y);

                // use a pseudorandomly generated pan angle.  
                // The bucketing used here will permit more
                // efficient pruning of results later
                bucket = randGen.Next(no_of_orientations - 1);
                //bucket = (t * ((no_of_orientations - 1) * 1000 * primes[randomSeed % (primes.Length-1)]) / no_of_trial_poses) % (no_of_orientations - 1);
                //if (bucket < 0) bucket = -bucket;
                float pan_angle = (bucket - (no_of_orientations / 2)) *
                                  pan_tollerance / no_of_orientations;

                // voila! the trial pose is generated
                trialPose = current_view.createTrialPose(pan_angle, x, y);

                // insert the trial pose into the grid and record the number
                // of matched cells
                grid.insert(trialPose, false, local_odometry_position);
                score = grid.matchedCells();

                // use history
                score = (score * score * score * score * (1.0f - momentum)) + (prevTrialPoseScore[t] * momentum);

                TrialPoseScore[t] = score;

                // deal or no deal ?
                if (score > 0)
                {
                    orientation_bucket[bucket] += score;

                    // add the result to the survey
                    survey_results.Add(new particlePose(t, x, y, bucket, score));

                    // record the max score
                    if (score > max_score) max_score = score;
                }
            }

            if (pruneSurvey)
            {
                // which bucket has triumphed ?
                int winner = 0;
                float max = 0;
                for (int b = 0; b < no_of_orientations; b++)
                {
                    if (orientation_bucket[b] > max)
                    {
                        max = orientation_bucket[b];
                        winner = b;
                    }
                }

                // banish results for unwanted pan orientations
                int max_diff = (no_of_orientations - 1) * pruneThreshold / 100;
                if (max_diff < 1) max_diff = 1;
                for (int i = survey_results.Count - 1; i >= 0; i--)
                {
                    particlePose result = (particlePose)survey_results[i];
                    if (Math.Abs(result.pan - winner) > max_diff)
                    {
                        TrialPoseScore[result.index] *= 0.8f;
                        //survey_results.RemoveAt(i);                        
                    }
                }
            }

            //store the max score
            max_trial_pose_score = max_score;

            // return the results of the survey.  Make of them what you will!
            return (survey_results);
        }

        /// <summary>
        /// returns the peak of the pan graph
        /// </summary>
        /// <param name="peak"></param>
        /// <returns></returns>
        public float SurveyPeakPan(float[] peak)
        {
            float x = 0;
            float tot_score = 0;
            for (int ang = 0; ang < peak.Length; ang++)
            {
                tot_score += peak[ang];
                x += ang * peak[ang];
            }
            if (tot_score > 0)
            {
                x /= tot_score;
            }
            float pan = (x * ((float)Math.PI / 2.0f) / peak.Length) - ((float)Math.PI / 4.0f);
            return (pan);
        }

        /// <summary>
        /// returns the peak position of the survey
        /// </summary>
        /// <param name="survey_results"></param>
        /// <param name="peak_x"></param>
        /// <param name="peak_y"></param>
        public void SurveyPeak(ArrayList survey_results,
                               ref float peak_x, ref float peak_y)
        {
            // get the max score
            float max_score = 0;
            for (int i = 0; i < survey_results.Count; i++)
            {
                particlePose result = (particlePose)survey_results[i];
                if (result.score > max_score) max_score = result.score;
            }
            float min_score = 0;

            float tot_score = 0;
            float x = 0;
            float y = 0;
            float scoresqr;
            for (int i = 0; i < survey_results.Count; i++)
            {
                particlePose result = (particlePose)survey_results[i];
                if (result.score > min_score)
                {
                    scoresqr = result.score; // *result.score;  // it's hip to be square
                    tot_score += scoresqr;
                    x += (result.x * scoresqr);
                    y += (result.y * scoresqr);
                }
            }
            if (tot_score > 0)
            {
                // gravity always gets you in the end
                x /= tot_score;
                y /= tot_score;
            }
            peak_x = x;
            peak_y = y / 10; //TODO:  doh! why do I have to divide this?
        }

        /// <summary>
        /// used to check the survey distribution
        /// </summary>
        /// <param name="img"></param>
        public void showSurveyDistribution(int no_of_trial_poses, Byte[] img, int img_width, int img_height)
        {
            int i;
            float x = 0, y = 0;
            int radius = img_width * 9 / no_of_trial_poses;
            if (radius < 2) radius = 2;

            for (i = 0; i < img.Length; i++) img[i] = 0;

            int max = no_of_trial_poses;
            for (i = 0; i < max; i++)
            {
                getTrialPose(i, max, (float)img_width, (float)img_height, 0, ref x, ref y);
                x += (img_width / 2);
                y += (img_height / 2);
                if ((x > 0) && (x < img_width - 1) && (y > 0) && (y < img_height - 1))
                {
                    Byte col = (Byte)(50 + rnd.Next(205));
                    for (int xx = (int)x - radius; xx <= x + radius; xx++)
                    {
                        int dx = xx - (int)x;
                        for (int yy = (int)y - radius; yy <= y + radius; yy++)
                        {
                            int dy = yy - (int)y;
                            int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));
                            if (dist <= radius)
                            {
                                if ((xx > 0) && (xx < img_width - 1) && (yy > 0) && (yy < img_height - 1))
                                {
                                    int n = ((img_width * (int)yy) + (int)xx) * 3;
                                    for (int c = 0; c < 3; c++)
                                        img[n + c] = col;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// used to check the survey distribution
        /// </summary>
        /// <param name="img"></param>        
        public void showSurveyScores(Byte[] img, int img_width, int img_height)
        {
            int i;
            float x = 0, y = 0;
            int radius = img_width * 6 / survey_trial_poses;
            if (radius < 2) radius = 2;

            for (i = 0; i < img.Length; i++) img[i] = 0;

            if (max_trial_pose_score > 0)
            {
                int max = survey_trial_poses;
                for (i = 0; i < max; i++)
                {
                    getTrialPose(i, max, (float)img_width, (float)img_height, 0, ref x, ref y);
                    x += (img_width / 2);
                    y += (img_height / 2);
                    if ((x > 0) && (x < img_width - 1) && (y > 0) && (y < img_height - 1))
                    {
                        float score = TrialPoseScore[i] / max_trial_pose_score;
                        score *= 255;
                        if (score > 255) score = 255;
                        Byte col = (Byte)score;
                        for (int xx = (int)x - radius; xx <= x + radius; xx++)
                        {
                            int dx = xx - (int)x;
                            for (int yy = (int)y - radius; yy <= y + radius; yy++)
                            {
                                int dy = yy - (int)y;
                                int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));
                                if (dist <= radius)
                                {
                                    if ((xx > 0) && (xx < img_width - 1) && (yy > 0) && (yy < img_height - 1))
                                    {
                                        int n = ((img_width * (int)yy) + (int)xx) * 3;
                                        img[n] = col;
                                        //for (int c = 0; c < 3; c++)
                                        //  img[n + c] = col;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// show the pan survey
        /// </summary>
        /// <param name="peak"></param>
        /// <param name="img"></param>
        public void showSurveyPan(float[] peak, Byte[] img, int img_width, int img_height, float actual_pan, float pan)
        {
            int x, y;
            int prev_x = 0;
            int prev_y = 0;

            for (int i = 0; i < img.Length; i++) img[i] = 0;

            for (int ang = 0; ang < peak.Length; ang++)
            {
                x = img_width * ang / peak.Length;
                y = img_height - 1 - (int)(img_height * peak[ang]);
                if (ang > 0)
                {
                    util.drawLine(img, img_width, img_height, x, y, prev_x, prev_y, 0, 255, 0, 0, false);
                }
                prev_x = x;
                prev_y = y;
            }

            x = (img_width / 2) + (int)(img_width * actual_pan / ((float)Math.PI / 2.0f));
            util.drawLine(img, img_width, img_height, x, 0, x, img_height - 1, 255, 255, 255, 0, false);

            x = (img_width / 2) + (int)(img_width * pan / ((float)Math.PI / 2.0f));
            util.drawLine(img, img_width, img_height, x, 0, x, img_height - 1, 255, 255, 0, 0, false);
        }

        /// <summary>
        /// returns a histogram of scores for pan angle, typically forming
        /// a peak around the best value
        /// </summary>
        /// <param name="survey_results"></param>
        /// <param name="no_of_buckets"></param>
        /// <param name="normalise"></param>
        /// <returns></returns>
        public float[] getSurveyPeak(ArrayList survey_results, int no_of_buckets, bool normalise)
        {
            int b, pose;
            float[] peak = new float[no_of_buckets];
            int[] hits = new int[no_of_buckets];
            int half_buckets = no_of_buckets / 2;
            float half_PI = (float)Math.PI / 2;

            for (pose = 0; pose < survey_results.Count; pose++)
            {
                particlePose trialPose = (particlePose)survey_results[pose];
                int bucket = half_buckets + (int)(trialPose.pan * half_buckets / half_PI);
                if ((bucket > -1) && (bucket < no_of_buckets))
                {
                    peak[bucket] += trialPose.score;
                    hits[bucket]++;
                }
            }

            //average score per bucket
            for (b = 0; b < no_of_buckets; b++)
                if (hits[b] > 0) peak[b] /= hits[b];

            //normalise the values if necessary
            if (normalise)
            {
                float max = 0;
                for (b = 0; b < no_of_buckets; b++)
                    if (peak[b] > max) max = peak[b];
                if (max > 0)
                {
                    for (b = 0; b < no_of_buckets; b++)
                        peak[b] /= max;
                }
            }

            return (peak);
        }

        /// <summary>
        /// displays the scores for the given survey area
        /// </summary>
        /// <param name="img"></param>
        /// <param name="survey_results"></param>
        public void showSurveyArea(Byte[] img, int img_width, int img_height,
                                   ArrayList survey_results,
                                   float actual_x, float actual_y, float actual_pan, float pan)
        {
            int x, y;
            float min_x, max_x, min_y, max_y, max_score;
            float peak_x = 0;
            float peak_y = 0;

            // find the peak
            SurveyPeak(survey_results, ref peak_x, ref peak_y);

            min_x = 99999;
            max_x = -99999;
            min_y = 99999;
            max_y = -99999;
            max_score = 0;
            for (int pose = 0; pose < survey_results.Count; pose++)
            {
                particlePose trialPose = (particlePose)survey_results[pose];
                if (trialPose.x < min_x) min_x = trialPose.x;
                if (trialPose.y < min_y) min_y = trialPose.y;
                if (trialPose.x > max_x) max_x = trialPose.x;
                if (trialPose.y > max_y) max_y = trialPose.y;
                if (trialPose.score > max_score) max_score = trialPose.score;
            }

            for (int i = 0; i < img.Length; i++) img[i] = 0;

            if (max_score > 0)
            {
                int half_width = img_width / 2;
                int half_height = img_height / 2;
                float scale_x = max_x - min_x;
                float scale_y = max_y - min_y;
                for (int pose = 0; pose < survey_results.Count; pose++)
                {
                    particlePose trialPose = (particlePose)survey_results[pose];
                    x = (int)((trialPose.x - min_x) * (img_width - 1) / scale_x);
                    y = (int)((trialPose.y - min_y) * (img_height - 1) / scale_y);
                    if (x < 1) x = 1;
                    if (y < 1) y = 1;
                    int value = (int)(trialPose.score * 255 * 2 / max_score);
                    if (value > 255) value = 255;
                    Byte intensity = (Byte)value;
                    for (int c = 0; c < 3; c++)
                    {
                        int n = ((img_width * y) + x) * 3;
                        img[n + c] = intensity;
                        n = ((img_width * (y - 1)) + x) * 3;
                        img[n + c] = intensity;
                        n = ((img_width * (y - 1)) + (x - 1)) * 3;
                        img[n + c] = intensity;
                        n = ((img_width * y) + (x - 1)) * 3;
                        img[n + c] = intensity;
                    }
                }

                // draw the peak position
                x = (int)((peak_x - min_x) * (img_width - 1) / scale_x);
                y = (int)((peak_y - min_y) * (img_height - 1) / scale_y);
                int w = img_width / 60;
                int h = img_height / 60;
                int dx = (int)(w * Math.Sin(pan));
                int dy = (int)(w * Math.Cos(pan));
                util.drawLine(img, img_width, img_height, x - dx, y - dy, x + dx, y + dy, 0, 255, 255, 0, false);
                util.drawLine(img, img_width, img_height, x - dy, y + dx, x + dy, y - dx, 0, 255, 255, 0, false);

                // and also the actual position
                x = (int)((actual_x - min_x) * (img_width - 1) / scale_x);
                y = (int)((actual_y - min_y) * (img_height - 1) / scale_y);
                dx = (int)(w * Math.Sin(actual_pan));
                dy = (int)(w * Math.Cos(actual_pan));
                util.drawLine(img, img_width, img_height, x - dx, y - dy, x + dx, y + dy, 0, 255, 0, 0, false);
                util.drawLine(img, img_width, img_height, x - dy, y + dx, x + dy, y - dx, 0, 255, 0, 0, false);
            }
        }
         
        /// <summary>
        /// clear any previous localisation momentum
        /// </summary>

        public void clearMomentum()
        {
            for (int i = 0; i < 2000; i++)
            {
                prevTrialPoseScore[i] = 0;
            }
        }
         
         */

        #endregion


        /// <summary>
        /// create a viewpoint using the given head, which contains the positions
        /// of all cameras and their observed stereo features
        /// </summary>
        /// <param name="head"></param>
        /// <returns></returns>
        public viewpoint createViewpoint(stereoHead head, pos3D robotOrientation)
        {
            baseline = head.baseline_mm;
            image_width = head.image_width;
            image_height = head.image_height;

            // create the viewpoint
            viewpoint view = new viewpoint(head.no_of_cameras);

            for (int cam = 0; cam < head.no_of_cameras; cam++)
            {
                pos3D headOrientation = head.cameraPosition[cam];
                pos3D cameraOrientation = new pos3D(0, 0, 0);
                cameraOrientation.pan = headOrientation.pan;
                if (robotOrientation != null) cameraOrientation.pan += robotOrientation.pan;
                cameraOrientation.tilt = headOrientation.tilt;
                cameraOrientation.roll = headOrientation.roll;

                if (head.features[cam] != null)  // if there are stereo features associated with this camera
                {
                    float[] stereo_features = head.features[cam].features;
                    float[] uncertainties = head.features[cam].uncertainties;
                    int f2 = 0;
                    for (int f = 0; f < stereo_features.Length; f += 3)
                    {
                        // get the parameters of the feature
                        float image_x = stereo_features[f];
                        float image_y = stereo_features[f + 1];
                        float disparity = stereo_features[f + 2];

                        // create a ray
                        evidenceRay ray = createRay(image_x, image_y, disparity, uncertainties[f2], head.features[cam].colour[f2, 0], head.features[cam].colour[f2, 1], head.features[cam].colour[f2, 2]);

                        if (ray != null)
                        {
                            // convert from camera-centric coordinates to real world coordinates
                            ray.translateRotate(cameraOrientation);

                            // add to the viewpoint
                            view.rays[cam].Add(ray);
                        }
                        f2++;
                    }
                }
            }

            return (view);
        }

        /// <summary>
        /// create a list of rays to be stored within poses
        /// </summary>
        /// <param name="head">head configuration</param>
        /// <returns></returns>
        public ArrayList createObservation(stereoHead head)
        {
            ArrayList result = new ArrayList();

            for (int cam = 0; cam < head.no_of_cameras; cam++)
            {
                // get data for this stereo camera
                baseline = head.calibration[cam].baseline;
                image_width = head.calibration[cam].leftcam.image_width;
                image_height = head.calibration[cam].leftcam.image_height;

                // some head geometry
                pos3D headOrientation = head.cameraPosition[cam];
                pos3D cameraOrientation = new pos3D(0, 0, 0);
                cameraOrientation.pan = headOrientation.pan;
                cameraOrientation.tilt = headOrientation.tilt;
                cameraOrientation.roll = headOrientation.roll;

                if (head.features[cam] != null)  // if there are stereo features associated with this camera
                {
                    float[] stereo_features = head.features[cam].features;
                    float[] uncertainties = head.features[cam].uncertainties;
                    int f2 = 0;
                    for (int f = 0; f < stereo_features.Length; f += 3)
                    {
                        // get the parameters of the feature
                        float image_x = stereo_features[f];
                        float image_y = stereo_features[f + 1];
                        float disparity = stereo_features[f + 2];

                        // create a ray
                        evidenceRay ray = createRay(image_x, image_y, disparity, uncertainties[f2], head.features[cam].colour[f2, 0], head.features[cam].colour[f2, 1], head.features[cam].colour[f2, 2]);

                        if (ray != null)
                        {
                            // convert from camera-centric coordinates to head coordinates
                            ray.translateRotate(cameraOrientation);

                            // add to the result
                            result.Add(ray);
                        }
                        f2++;
                    }
                }
            }

            return (result);
        }


        /// <summary>
        /// shows the probability distribution thrown into the grid
        /// </summary>
        /// <param name="img"></param>
        public void showDistribution(Byte[] img, int img_width, int img_height)
        {
            for (int i = 0; i < img.Length; i++) img[i] = 0;

            for (int x = 0; x < img_width; x++)
            {
                float prob = Gaussian((x - (img_width / 2)) / (float)(img_width / 2));
                for (int y = img_height - 1; y > img_height - 1 - (int)(prob * img_height); y--)
                {
                    int n = ((img_width * y) + x) * 3;
                    img[n + 1] = 255;
                }
            }
        }



        /// <summary>
        /// do the two given rays intersect?  If so then return the intersection point
        /// </summary>
        /// <param name="ray1"></param>
        /// <param name="ray2"></param>
        /// <returns></returns>
        public static bool raysOverlap(evidenceRay ray1, evidenceRay ray2,
                                       pos3D intersectPos, float separation_tollerance,
                                       int ray_thickness, ref float separation)
        {
            bool intersect = false;
            float x0, y0, x1, y1, x2, y2, x3, y3, x4, y4, x5, y5;
            float xi = 0, yi = 0;
            float w = (ray1.width + ray2.width) * separation_tollerance;

            //do they intersect in the XY plane?

            x2 = ray2.vertices[0].x;
            y2 = ray2.vertices[0].y;
            x3 = ray2.vertices[1].x;
            y3 = ray2.vertices[1].y;
            
            int incr = (ray_thickness * 2)-1;
            if (incr < 1) incr = 1;
            int xx = -ray_thickness;
            while ((xx <= ray_thickness) && (!intersect))
            {
                int yy = -ray_thickness;
                while ((yy <= ray_thickness) && (!intersect))
                {
                    x0 = ray1.vertices[0].x + xx;
                    y0 = ray1.vertices[0].y + yy;
                    x1 = ray1.vertices[1].x + xx;
                    y1 = ray1.vertices[1].y + yy;

                    if (util.intersection(x0, y0, x1, y1, x2, y2, x3, y3, ref xi, ref yi))
                    {
                        //calculate vertical separation at the intersection point
                        y0 = ray1.vertices[0].z;
                        y1 = ray1.vertices[1].z;
                        y2 = ray2.vertices[0].z;
                        y3 = ray2.vertices[1].z;

                        x4 = xi;
                        y4 = -1000;
                        x5 = xi;
                        y5 = 1000;

                        float xi2 = 0, zi1 = 0, zi2 = 0;
                        util.intersection(x0, y0, x1, y1, x4, y4, x5, y5, ref xi2, ref zi1);
                        util.intersection(x2, y2, x3, y3, x4, y4, x5, y5, ref xi2, ref zi2);

                        float vertical_separation = Math.Abs(zi2 - zi1);
                        if (vertical_separation <= w)
                        {
                            intersect = true;
                            intersectPos.x = xi - xx;
                            intersectPos.y = yi - yy;
                            intersectPos.z = zi2;
                            separation = vertical_separation;
                        }
                    }
                    yy += incr;
                }
                xx += incr;
            }
            return (intersect);
        }




        #region "gaussian functions"

        /// <summary>
        /// gaussian function
        /// </summary>
        /// <param name="fraction"></param>
        /// <returns></returns>
        public static float Gaussian(float fraction)
        {
            fraction *= 3.0f;
            float prob = (float)((1.0f / Math.Sqrt(2.0*Math.PI))*Math.Exp(-0.5*fraction*fraction));

            return (prob*2.5f);
        }

        public static float[] createGaussianLookup(int levels)
        {
            float[] gaussLookup = new float[levels];

            for (int i = 0; i < levels; i++)
            {
                float fract = ((i * 2.0f) / levels) - 1.0f;
                gaussLookup[i] = stereoModel.Gaussian(fract);
            }
            return (gaussLookup);
        }

        public static float[] createHalfGaussianLookup(int levels)
        {
            float[] gaussLookup = new float[levels];

            for (int i = 0; i < levels; i++)
            {
                float fract = (i / (float)levels);
                gaussLookup[i] = stereoModel.Gaussian(fract);
            }
            return (gaussLookup);
        }


        /// <summary>
        /// inserts a line with gaussian probability distribution into the grid
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="linewidth"></param>
        /// <param name="additive"></param>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="rayNumber"></param>
        public void addGaussianLine(float x1, float y1, float x2, float y2, int linewidth, float additive, float[,,] grid_layer, int grid_dimension, int rayNumber)
        {
            float w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
            float cx, cy;
            float m, dist, max_dist, dxx, dyy, incr;

            dx = x2 - x1;
            dy = y2 - y1;
            cx = x1 + (dx / 2);
            cy = y1 + (dy / 2);
            max_dist = (float)Math.Sqrt((dx * dx) + (dy * dy)) / 2;
            w = Math.Abs(dx);
            h = Math.Abs(dy);
            if (x2 >= x1) step_x = 1; else step_x = -1;
            if (y2 >= y1) step_y = 1; else step_y = -1;

            if (w > h)
            {
                if (dx != 0)
                {
                    m = dy / (float)dx;
                    x = x1;
                    int max_steps = Math.Abs((int)x2 - (int)x1);
                    int steps = 0;
                    while ((x != x2 + step_x) && (steps < max_steps))
                    {
                        steps++;
                        y = (m * (x - x1)) + y1;

                        for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                            for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                            {
                                if ((xx2 >= 0) && (xx2 < grid_dimension/divisor) && (yy2 >= 0) && (yy2 < grid_dimension))
                                {
                                    int xx3 = (int)xx2;
                                    int yy3 = (int)yy2;

                                    if (grid_layer[xx3, yy3, 2] < rayNumber)
                                    {
                                        dxx = xx2 - cx;
                                        dyy = yy2 - cy;
                                        if (max_dist > 0.01f)
                                            dist = (float)Math.Sqrt((dxx * dxx) + (dyy * dyy)) / max_dist;
                                        else
                                            dist = 0;
                                        incr = Gaussian(dist) * additive;

                                        grid_layer[xx3, yy3, 0]++;
                                        if (grid_layer[xx3, yy3, 1] == 0)
                                            grid_layer[xx3, yy3, 1] += incr;
                                        else
                                            grid_layer[xx3, yy3, 1] *= incr;
                                        if (grid_layer[xx3, yy3, 1] > max_prob)
                                        {
                                            max_prob = grid_layer[xx3, yy3, 1];
                                            if (yy2 > starting_y)
                                            {
                                                max_prob_x = xx3;
                                                max_prob_y = yy3;
                                            }
                                        }
                                        if ((grid_layer[xx3, yy3, 2] == rayNumber - 1) || (grid_layer[xx3, yy3, 2] == 0))
                                            grid_layer[xx3, yy3, 2] = rayNumber;
                                    }
                                }
                            }

                        x += step_x;
                    }
                }
            }
            else
            {
                if (dy != 0)
                {
                    m = dx / (float)dy;
                    y = y1;
                    int max_steps = Math.Abs((int)y2 - (int)y1);
                    int steps = 0;
                    while ((y != y2 + step_y) && (steps < max_steps))
                    {
                        steps++;
                        x = (m * (y - y1)) + x1;
                        for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                            for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                            {
                                if ((xx2 >= 0) && (xx2 < grid_dimension / divisor) && (yy2 >= 0) && (yy2 < grid_dimension))
                                {
                                    int xx3 = (int)xx2;
                                    int yy3 = (int)yy2;

                                    if (grid_layer[xx3, yy3, 2] < rayNumber)
                                    {
                                        dxx = xx2 - cx;
                                        dyy = yy2 - cy;
                                        if (max_dist > 0.01f)
                                            dist = (float)Math.Sqrt((dxx * dxx) + (dyy * dyy)) / max_dist;
                                        else
                                            dist = 0;
                                        incr = Gaussian(dist) * additive;

                                        grid_layer[xx3, yy3, 0]++;
                                        if (grid_layer[xx3, yy3, 1] == 0)
                                            grid_layer[xx3, yy3, 1] += incr;
                                        else
                                            grid_layer[xx3, yy3, 1] *= incr;
                                        if ((grid_layer[xx3, yy3, 1] > max_prob)) // && (yy2 > starting_y))
                                        {
                                            max_prob = grid_layer[xx3, yy3, 1];
                                            if (yy2 > starting_y)
                                            {
                                                max_prob_x = xx3;
                                                max_prob_y = yy3;
                                            }
                                        }
                                        if ((grid_layer[xx3, yy3, 2] == rayNumber - 1) || (grid_layer[xx3, yy3, 2] == 0))
                                            grid_layer[xx3, yy3, 2] = rayNumber;
                                    }
                                }
                            }

                        y += step_y;
                    }
                }
            }
        }

        #endregion


        public evidenceRay createRay(float x, float y, float disparity, float uncertainty, Byte r, Byte g, Byte b)
        {
            evidenceRay ray = null;
            float x1 = x + disparity;
            float x2 = x;
            float x_start = 0, y_start = 0;
            float x_end = 0, y_end = 0;
            float x_left = 0, y_left = 0;
            float x_right = 0, y_right = 0;
            int grid_dimension = 2000;

            raysIntersection(x1, x2, grid_dimension, uncertainty,
                             ref x_start, ref y_start, ref x_end, ref y_end,
                             ref x_left, ref y_left, ref x_right, ref y_right);
            if (y_start < -1) y_start = -y_start;
            if (y_end < -1) y_end = -y_end;

            if (x_right > x_left)
            {
                // calc uncertainty in angle (+/- half a pixel)
                float angular_uncertainty = FOV_vertical / (image_height * 2);

                // convert y image position to z height
                int half_height = image_height / 2;
                float angle = ((y - half_height) * FOV_vertical / image_height);
                float z = (int)(y_left * Math.Sin(angle));
                float z_start = (int)(y_start * Math.Sin(angle));
                float z_end = (int)(y_end * Math.Sin(angle));

                //use an offset so that the point from which the ray was observed was (0,0,0)
                float x_offset = grid_dimension / 2;
                ray = new evidenceRay();
                ray.vertices[0] = new pos3D(x_start - x_offset, y_start, z_start);
                
                ray.vertices[1] = new pos3D(x_end - x_offset, y_end, z_end);
                ray.fattestPoint = (y_left - y_start) / (y_end - y_start);
                //ray.vertices[2] = new pos3D(x_left - x_offset, y_left, z);
                //ray.vertices[3] = new pos3D(x_right - x_offset, y_right, z);
                //ray.centre = new pos3D(x_left + ((x_right-x_left)/2) - x_offset, y_left, z);
                ray.width = x_right - x_left;

                ray.colour[0] = r;
                ray.colour[1] = g;
                ray.colour[2] = b;

                ray.uncertainty = uncertainty;
                ray.disparity = disparity;
                ray.sigma = sigma;
            }
            return (ray);
        }

        /// <summary>
        /// show stereo ray probabilities
        /// </summary>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="img"></param>
        public void showProbabilities(float[, ,] grid_layer, int grid_dimension, 
                                      Byte[] img, int img_width, int img_height, 
                                      bool show_ray_outlines,
                                      bool show_vacancy)
        {
            int min_dist = (int)baseline;
            int max_dist = grid_dimension;
            int max_disparity = image_width * 35 / 100;
            int interval = image_width / 6;

            for (int i = 0; i < img.Length; i++) img[i] = 0;

            for (int x = 0; x < image_width; x+=interval)
            {
                int disp = 1;
                int xx = x + disp;
                int offset = 0;
                while (xx < x+max_disparity)
                {
                    // clear the grid
                    max_prob = 0.0f;
                    max_prob_x = 0;
                    max_prob_y = 0;
                    for (int gx = 0; gx < grid_dimension; gx++)
                        for (int gy = 0; gy < grid_dimension; gy++)
                        {
                            grid_layer[gx, gy, 0] = 0;
                            grid_layer[gx, gy, 1] = 0;
                            grid_layer[gx, gy, 2] = 0;
                        }
                    
                    float x_start=0, y_start=0;
                    float x_end = 0, y_end = 0;
                    float x_left = 0, y_left = 0;
                    float x_right = 0, y_right = 0;
                    float uncertainty = 1;
                    raysIntersection(xx + offset, x + offset, grid_dimension, uncertainty,
                                     ref x_start, ref y_start, ref x_end, ref y_end,
                                     ref x_left, ref y_left, ref x_right, ref y_right);
                    if (x_right > x_left)
                    {
                        if (y_start < -1) y_start = -y_start;
                        if (y_end < -1) y_end = -y_end;

                        if (y_start > 0)
                            min_dist = (int)y_start;
                        else
                            min_dist = 100;

                        max_dist = grid_dimension;
                        if ((y_end > 0) && (y_end < grid_dimension))
                            max_dist = (int)y_end;

                        starting_y = (int)(y_start + 50);

                        if (show_vacancy) min_dist = 0;

                        throwRay(-baseline / 2, xx+offset, min_dist, max_dist, grid_layer, grid_dimension,1);
                        throwRay(baseline / 2, x+offset, min_dist, max_dist, grid_layer, grid_dimension,2);
                        

                        if (show_ray_outlines)
                        {

                            //img.drawLine((int)(x_start * img.width / grid_dimension), img.height - 1 - (int)(y_start * img.height / grid_dimension),
                            //             (int)(x_end * img.width / grid_dimension), img.height - 1 - (int)(y_end * img.height / grid_dimension),
                            //             255, 0, 0, 0);
                            util.drawLine(img, img_width, img_height, (int)(x_left * img_width / grid_dimension), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                         (int)(x_right * img_width / grid_dimension), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            util.drawLine(img, img_width, img_height, (int)(x_left * img_width / grid_dimension), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                         (int)(x_start * img_width / grid_dimension), img_height - 1 - (int)(y_start * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            util.drawLine(img, img_width, img_height, (int)(x_right * img_width / grid_dimension), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                         (int)(x_start * img_width / grid_dimension), img_height - 1 - (int)(y_start * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            /*
                            img.drawLine((int)(max_prob_x * img.width / grid_dimension), (int)(max_prob_y * img.height / grid_dimension),
                                         (int)((max_prob_x + 5) * img.width / grid_dimension), (int)(max_prob_y * img.height / grid_dimension),
                                         0, 255, 0, 0);
                            */ 


                            /*
                            int dir1, dir2;
                            int cx = (int)(x_left + ((x_right - x_left)/2));
                            if (cx > x_start) dir1 = 1; else dir1 = 0;
                            if (x_end > cx) dir2 = 1; else dir2 = 0;
                            if ((dir1 == dir2) && (y_end > y_left))
                            {
                             */
                            util.drawLine(img, img_width, img_height, (int)(x_left * img_width / grid_dimension), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                         (int)(x_end * img_width / grid_dimension), img_height - 1 - (int)(y_end * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            util.drawLine(img, img_width, img_height, (int)(x_right * img_width / grid_dimension), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                         (int)(x_end * img_width / grid_dimension), img_height - 1 - (int)(y_end * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            //}                        
                        }
                        else grid_layer_to_image(grid_layer, grid_dimension, img, img_width, img_height, show_vacancy);
                        
                    }                    

                    disp *= 2;
                    xx = x + disp;

                    if (offset == 0)
                        offset = interval/2;
                    else
                        offset = 0;
                }
            }
        }

        /// <summary>
        /// show ray models
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void ray_model_to_image(Byte[] img, int img_width, int img_height)
        {
            for (int x = 0; x < img_width; x++)
            {
                int xx = x * ray_model.GetLength(0) / img_width;

                // find the max
                float max_value=0;
                for (int y = 0; y < img_height; y++)
                {
                    int yy = y * ray_model.GetLength(1) / img_height;

                    if (ray_model[xx, yy] > max_value) max_value = ray_model[xx, yy];
                }

                for (int y = 0; y < img_height; y++)
                {
                    int n = (((img_height-1-y) * img_width) + x) * 3;
                    if (n < img.Length)
                    {
                        if (max_value > 0)
                        {
                            int yy = y * ray_model.GetLength(1) / img_height;
                            float value = ray_model[xx, yy] * 255 / max_value;

                            for (int col = 0; col < 3; col++) img[n + col] = (Byte)value;
                        }
                        else for (int col = 0; col < 3; col++) img[n + col] = (Byte)0;
                    }
                }
            }
        }

        /// <summary>
        /// displays ray models for each disparity as a graph
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void ray_model_to_graph_image(Byte[] img, int img_width, int img_height)
        {
            float max_value = 0.0f;
            int max_length = 0;

            // clear the image
            for (int i = 0; i < img.Length; i++) img[i] = 255;

            // find the maximum probability
            for (int disparity_pixels = 3; disparity_pixels < ray_model.GetLength(0); disparity_pixels++)
            {
                for (int i = 0; i < ray_model.GetLength(1); i++)
                {
                    if (ray_model[disparity_pixels, i] > 0)
                    {
                        if (i > max_length) max_length = i;

                        if (ray_model[disparity_pixels, i] > max_value)
                            max_value = ray_model[disparity_pixels, i];
                    }
                }
            }

            if (max_value > 0)
            {
                max_value *= 1.1f;
                // for each possible diaparity value
                for (int disparity_pixels = 3; disparity_pixels < ray_model.GetLength(0); disparity_pixels+=2)
                {
                    int prev_i = 0;
                    float prev_value = -1;
                    for (int i = 0; i < ray_model.GetLength(1); i++)
                    {
                        if (ray_model[disparity_pixels, i] != prev_value)
                        {
                            if (i > 0)
                            {
                                int x = i * img_width / max_length;
                                int y = (int)(ray_model[disparity_pixels, i] * img_height / max_value);
                                if (y >= img_height) y = img_height - 1;
                                y = img_height - 1 - y;
                                int prev_x = prev_i * img_width / max_length;
                                int prev_y = (int)(ray_model[disparity_pixels, prev_i] * img_height / max_value);
                                if (prev_y >= img_height) prev_y = img_height - 1;
                                prev_y = img_height - 1 - prev_y;
                                util.drawLine(img, img_width, img_height, prev_x, prev_y, x, y, 0, 0, 0, 0, false);
                            }
                            prev_value = ray_model[disparity_pixels, i];
                            prev_i = i;
                        }
                    }
                }
            }

        }


        /// <summary>
        /// inserts grid cells into an image object
        /// </summary>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="img"></param>
        public void grid_layer_to_image(float[, ,] grid_layer, int grid_dimension, 
                                        Byte[] img, int img_width, int img_height,
                                        bool show_vacancy)
        {
            float max = max_prob / 70;
            if (max < 0.000000001f) max = 0.000000001f;

            for (int x = 0; x < img_width; x++)
            {
                int xx = x * (grid_dimension / divisor) / img_width;
                if ((xx > -1) && (xx < (grid_dimension / divisor)))
                {
                    for (int y = 0; y < img_height; y++)
                    {
                        int yy = y * grid_dimension / img_height;

                        if ((grid_layer[xx, yy, 2] == 2) &&
                            (grid_layer[xx, yy, 0] > 1))
                        {
                            int cellval = (int)(grid_layer[xx, yy, 1] * 255 / max);
                            if (cellval > 255) cellval = 255;
                            Byte cell_value = (Byte)cellval;
                            int n = ((img_width * y) + x) * 3;
                            if (img[n] < cell_value)
                            {                                
                                img[n] = cell_value;
                                img[n + 1] = cell_value;
                                img[n + 2] = cell_value;
                            }
                        }
                    }
                }
            }

            if (show_vacancy)
            {
                for (int x = 0; x < img_width; x++)
                {
                    int xx = x * (grid_dimension / divisor) / img_width;
                    if ((xx > -1) && (xx < (grid_dimension / divisor)))
                    {
                        for (int y = 0; y < img_height; y++)
                        {
                            int yy = y * grid_dimension / img_height;

                            int n = ((img_width * y) + x) * 3;
                            if ((grid_layer[xx, yy, 1] > 0) && (img[n] == 0))
                            {                                
                                int cellval = (int)(grid_layer[xx, yy, 1] * 255 * 5.0f);
                                if (cellval > 255) cellval = 255;
                                //cellval = 50;
                                Byte cell_value = (Byte)cellval;
                                img[n] = cell_value;
                                img[n + 1] = 0;
                                img[n + 2] = 0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// calculates the region inside which two rays intersect
        /// </summary>
        /// <param name="x1">feature position in the left image (in pixels)</param>
        /// <param name="x2">feature position in the right image (in pixels)</param>
        /// <param name="grid_dimension"></param>
        /// <param name="x_start">bottom intersection (closest to cameras)</param>
        /// <param name="y_start"></param>
        /// <param name="x_end">top intersection (farthest from cameras)</param>
        /// <param name="y_end"></param>
        /// <param name="x_left"></param>
        /// <param name="y_left"></param>
        /// <param name="x_right"></param>
        /// <param name="y_right"></param>
        private void raysIntersection(float x1, float x2, int grid_dimension, float ray_uncertainty,
                                      ref float x_start, ref float y_start,
                                      ref float x_end, ref float y_end,
                                      ref float x_left, ref float y_left,
                                      ref float x_right, ref float y_right)
        {
            // calc uncertainty in angle (+/- half a pixel)
            float angular_uncertainty = FOV_horizontal / (image_width * 2);

            // convert x positions to angles
            int half_width = image_width / 2;
            //float left_angle = ((float)Math.PI / 2) + ((left_x - half_width) * FOV / image_width);
            //float right_angle = ((float)Math.PI / 2) + ((right_x - half_width) * FOV / image_width);
            float angle1 = ((x1 - half_width) * FOV_horizontal / image_width);
            float angle2 = ((x2 - half_width) * FOV_horizontal / image_width);

            float offset_x1 = (grid_dimension / (2*divisor)) - (baseline / 2);
            float offset_x2 = (grid_dimension / (2*divisor)) + (baseline / 2);
            
            int d = 100;

            float xx1 = (d * (float)Math.Sin(angle1));
            float xx2 = (d * (float)Math.Sin(angle2));
            float yy = d;

            if (ray_uncertainty < 0.5f) ray_uncertainty = 0.5f;
            float uncertainty = sigma * d * ray_uncertainty;

            util.intersection(offset_x1, 0.0f, offset_x1 + xx1 + uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 - uncertainty, yy,
                         ref x_start, ref y_start);
            util.intersection(offset_x1, 0.0f, offset_x1 + xx1 - uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 + uncertainty, yy,
                         ref x_end, ref y_end);
            util.intersection(offset_x1, 0.0f, offset_x1 + xx1 - uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 - uncertainty, yy,
                         ref x_left, ref y_left);
            util.intersection(offset_x1, 0.0f, offset_x1 + xx1 + uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 + uncertainty, yy,
                         ref x_right, ref y_right);

            //do some checking
            if (x_right > x_left)
            {
                int dir1, dir2;
                float cx = x_left + ((x_right - x_left) / 2);
                if (cx > x_start) dir1 = 1; else dir1 = 0;
                if (x_end > cx) dir2 = 1; else dir2 = 0;
                if (!((dir1 == dir2) && (y_end > y_left)))
                {
                    float dy = y_left - y_start;
                    float grad = (cx - x_start) / dy;
                    float dist = dy * 5;
                    x_end = cx + (grad * dist);
                    y_end = y_left + dist;
                }
            }

        }

        /// <summary>
        /// throw a ray into the grid, updating probabilities as we go
        /// </summary>
        /// <param name="offset">typically +/- half the camera baseline</param>
        /// <param name="x">x position of the feature in the image (in pixels)</param>
        /// <param name="min_dist">distance to begin throwing at</param>
        /// <param name="max_dist">distance to end throwing at</param>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="rayNumber">used to avoid updating grid cells more than needed</param>
        public void throwRay(float offset, float x, int min_dist, int max_dist, float[, ,] grid_layer, int grid_dimension, int rayNumber)
        {
            // calc uncertainty in angle (+/- half a pixel)
            float angular_uncertainty = FOV_horizontal / (image_width * 2);

            // convert x positions to angles
            int half_width = image_width / 2;
            //float left_angle = ((float)Math.PI / 2) + ((left_x - half_width) * FOV / image_width);
            //float right_angle = ((float)Math.PI / 2) + ((right_x - half_width) * FOV / image_width);
            float angle = ((x - half_width) * FOV_horizontal / (float)image_width);

            int offset_x = (grid_dimension / (2*divisor)) + (int)(offset);

            for (float d = min_dist; d < max_dist; d += 1.0f)
            {
                float xx = d * (float)Math.Sin(angle);
                float yy;
                if (undistort)
                    yy = d;
                else
                    yy = d * (float)Math.Cos(angle);

                float uncertainty = sigma * d;

                float ux0, uy0, ux1, uy1;
                
                if (undistort)
                {
                    ux0 = xx - uncertainty;
                    uy0 = yy;
                    ux1 = xx + uncertainty;
                    uy1 = yy;
                }
                else
                {
                    ux0 = xx - uncertainty * (float)Math.Sin((Math.PI / 2) + angle);
                    uy0 = yy - uncertainty * (float)Math.Cos((Math.PI / 2) + angle);
                    ux1 = xx + uncertainty * (float)Math.Sin((Math.PI / 2) + angle);
                    uy1 = yy + uncertainty * (float)Math.Cos((Math.PI / 2) + angle);
                }

                float tx = offset_x + (int)(ux0);
                float ty = grid_dimension - 1 - (int)(uy0);
                float bx = offset_x + (int)(ux1);
                float by = grid_dimension - 1 - (int)(uy1);

                //check this value, which should be the height of the gaussian
                float magnitude = 1.0f / uncertainty;

                addGaussianLine(tx, ty, bx, by, 2, magnitude, grid_layer, grid_dimension, rayNumber);
            }
        }


        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            if (mapping)
                util.AddComment(doc, parent, "Sensor model used to update grid maps");            
            else
                util.AddComment(doc, parent, "Sensor model used to localise within grids");

            String ModelName = "SensorModelMapping";
            if (!mapping) ModelName = "SensorModelLocalisation";

            XmlElement nodeSensorModel = doc.CreateElement(ModelName);
            parent.AppendChild(nodeSensorModel);

            if (!mapping)
            {
                util.AddComment(doc, nodeSensorModel, "Number of features to use when performing grid based localisation");
                util.AddTextElement(doc, nodeSensorModel, "NoOfFeatures", Convert.ToString(no_of_stereo_features));
            }
            else
            {
                util.AddComment(doc, nodeSensorModel, "Number of features to use when updating the grid");
                util.AddTextElement(doc, nodeSensorModel, "NoOfFeatures", Convert.ToString(no_of_stereo_features));
            }

            util.AddComment(doc, nodeSensorModel, "Observation Error Standard Deviation");
            util.AddTextElement(doc, nodeSensorModel, "ObservationErrorStandardDeviation", Convert.ToString(sigma));

            util.AddComment(doc, nodeSensorModel, "Adjustment factor for probability peak density");
            util.AddTextElement(doc, nodeSensorModel, "PeakDensityAdjust", Convert.ToString(peak_density_adjust));

            
            return (nodeSensorModel);
        }

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeSentience = doc.CreateElement("Sentience");
            doc.AppendChild(nodeSentience);

            nodeSentience.AppendChild(getXml(doc, nodeSentience));

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public bool Load(String filename)
        {
            bool loaded = false;

            if (File.Exists(filename))
            {
                // use an XmlTextReader to open an XML document
                XmlTextReader xtr = new XmlTextReader(filename);
                xtr.WhitespaceHandling = WhitespaceHandling.None;

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                xd.Load(xtr);

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
                LoadFromXml(xnodDE, 0);

                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "ObservationErrorStandardDeviation")
            {
                sigma = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "PeakDensityAdjust")
            {
                peak_density_adjust = Convert.ToSingle(xnod.InnerText);
            }

            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) &&
                ((xnod.Name == "SensorModelMapping") || (xnod.Name == "SensorModelLocalisation")))
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }
}
