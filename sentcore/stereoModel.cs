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
using System.Xml;
using System.Collections.Generic;
using CenterSpace.Free;
using sluggish.utilities;

namespace sentience.core
{
    /// <summary>
    /// fundamental stereo vision maths and models
    /// </summary>
    public sealed class stereoModel
    {
        // random number generator
        MersenneTwister rnd = new MersenneTwister(100);

        public bool mapping = true;

        // field of vision
        public float FOV_horizontal = 78 * (float)Math.PI / 180.0f;  // horizontal field of vision
        public float FOV_vertical = 78/2 * (float)Math.PI / 180.0f;  // vertical field of vision
        public int image_width = 320;   // horizontal image size
        public int image_height = 240;  // vertical image size
        public float focal_length = 5;  // focal length in millimetres
        public float baseline = 100;    // distance between the centres of the two cameras in millimetres
        public float sigma = 0.04f;     //0.005f;  //angular uncertainty magnitude (standard deviation) pixels per mm of range
        private float max_prob = 0.0f;
        private int max_prob_x, max_prob_y;
        private int starting_y;
        public bool undistort = true;
        
        // vergence angle between the two cameras
        public float vergence_radians = 0.0f * (float)Math.PI / 180.0f;

        private int ray_model_normal_length = 500;
        private int ray_model_max_length = 1500;
        public rayModelLookup ray_model = null;

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


        #region "calculation of the sensor model lookup table"

        /// <summary>
        /// creates a lookup table for sensor models at different visual disparities
        /// </summary>
        public void createLookupTables(stereoHead robot_head, int gridCellSize_mm)
        {
            int width = 100;
            int height = 100;
            Byte[] img_result = new Byte[width * height * 3];

            for (int cam = 0; cam < robot_head.no_of_stereo_cameras; cam++)
            {
                // update parameters based upon the calibration data
                image_width = robot_head.calibration[cam].leftcam.image_width;
                image_height = robot_head.calibration[cam].leftcam.image_height;
                baseline = robot_head.calibration[cam].baseline;
                FOV_horizontal = robot_head.calibration[cam].leftcam.camera_FOV_degrees * (float)Math.PI / 180.0f;
                FOV_vertical = FOV_horizontal * image_height / image_width;

                // create the lookup
                createLookupTable(gridCellSize_mm, img_result, width, height);

                // attach the lookup table to the relevant camera
                robot_head.sensormodel[cam] = ray_model;
            }
        }

        public void createLookupTable(int gridCellSize_mm)
        {
            int width = 320;
            int height = 240;
            Byte[] img_result = new Byte[width * height * 3];
            createLookupTable(gridCellSize_mm, img_result, width, height);
        }

        /// <summary>
        /// creates a lookup table for sensor models at different visual disparities
        /// </summary>
        /// <param name="img_result"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void createLookupTable(int gridCellSize_mm, Byte[] img_result, int width, int height)
        {
            bool mirror = false;
            int divisor = 40;
            int grid_dimension = 20000;
            float[,,] grid_layer = new float[grid_dimension / divisor, grid_dimension, 3];
            updateRayModel(grid_layer, grid_dimension, 
                           img_result, width, height,
                           divisor, false, gridCellSize_mm, mirror);
            ray_model.Save("InverseSensorModels.xml");
        }

        /// <summary>
        /// Ok, so we've calculated the ray models in stupifying detail
        /// now let's compress them down to some more compact and less memory-hogging form
        /// so that each array element corresponds to a single occupancy grid cell, which
        /// makes updating the grid a simple process
        /// </summary>
        /// <param name="gridCellSize_mm">Size of each occupancy grid cell in millimetres</param>
        private void compressRayModels(int gridCellSize_mm)
        {
            if (ray_model != null)
            {
                ray_model_normal_length = ray_model.dimension_probability / gridCellSize_mm;

                rayModelLookup new_ray_model = new rayModelLookup(ray_model.dimension_disparity, ray_model_normal_length);

                for (int d = 1; d < ray_model.dimension_disparity; d++)
                {
                    int prev_index = 0;
                    for (int i = 0; i < ray_model_normal_length; i++)
                    {
                        int next_index = (i + 1) * ray_model.dimension_probability / new_ray_model.dimension_probability;
                        // sum the probabilities
                        float total_probability = 0;
                        for (int j = prev_index; j < next_index; j++)
                        {
                            // is there a DJ in the house ?
                            total_probability += ray_model.probability[d][j];
                        }
                        if (total_probability > 0)
                            new_ray_model.probability[d][i] = total_probability;
                        prev_index = next_index;
                    }
                    new_ray_model.length[d] = ray_model.length[d] * new_ray_model.dimension_probability / ray_model.dimension_probability;

                    // if there's only one grid cell give it the full probability
                    // (it's gotta be in there somewhere!)
                    if (new_ray_model.length[d] == 1) new_ray_model.probability[d][0] = 1.0f;
                }

                // finally swap the arrays
                ray_model = new_ray_model;
                //ray_model_length = new_ray_model_length;
            }
        }

        /// <summary>
        /// creates a lookup table for sensor models
        /// </summary>
        /// <param name="grid_layer">the grid upon which the rays will be drawn</param>
        /// <param name="grid_dimension">size of the grid</param>
        /// <param name="img">image within which to display the results</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="divisor">a dividing factor used to make the width of the grid smaller than its length</param>
        /// <param name="apply_smoothing">try to smooth the data to remove pixelation effects</param>
        /// <param name="gridCellSize_mm">Size of each occupancy grid cell in millimetres</param>
        /// <param name="mirror">apply mirroring</param>
        private void updateRayModel(float[, ,] grid_layer, int grid_dimension, 
                                    Byte[] img, int img_width, int img_height, 
                                    int divisor, bool apply_smoothing,
                                    int gridCellSize_mm, bool mirror)
        {
            // half a pixel of horizontal uncertainty
            sigma = 1.0f / (image_width * 1) * FOV_horizontal;
            //sigma *= image_width / 320;
            this.divisor = divisor;

            float max_disparity = (10 * image_width / 100);

            ray_model_max_length = grid_dimension;
            ray_model = new rayModelLookup((int)(max_disparity) + 1, ray_model_max_length);

            int min_dist = (int)baseline;
            int max_dist = grid_dimension;
            int x = (image_width)*499/1000;

            for (float disparity_pixels = 1; disparity_pixels < max_disparity; disparity_pixels += 0.5f)
            {
                float xx = x + disparity_pixels;

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

                    throwRay(-baseline / 2, xx, vergence_radians, min_dist, max_dist, grid_layer, grid_dimension, 1, mirror);
                    throwRay(baseline / 2, x, -vergence_radians, min_dist, max_dist, grid_layer, grid_dimension, 2, mirror);

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
                        for (int l = 0; l < ray_model_max_length; l++)
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
                    if (total_probability > 0)
                    {
                        if (max_length > 0) scaling_factor = 1.0f / total_probability;

                        // record the length of the ray model
                        ray_model.length[(int)(disparity_pixels * 2)] = max_length+1;

                        float max = 0;
                        int max_index = 0;
                        for (int l = 0; l < ray_model_max_length; l++)
                        {
                            int y2 = ray_model_max_length - 1 - l;
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
                                        ray_model.probability[(int)(disparity_pixels * 2)][l - start] = cellval;
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
                            float[] probvalues = new float[ray_model_max_length];
                            int radius = 20;
                            for (int itt = 0; itt < 10; itt++)
                            {
                                for (int i = 0; i < ray_model_max_length; i++)
                                {
                                    float value = 0;
                                    int hits = 0;
                                    for (int j = i - radius; j < i + radius; j++)
                                    {
                                        if ((j >= 0) && (j < ray_model_max_length))
                                        {
                                            value += ((j - (i - radius)) * ray_model.probability[(int)(disparity_pixels * 2)][j]);
                                            hits++;
                                        }
                                    }
                                    if (hits > 0) value /= hits;
                                    probvalues[i] = value;
                                }
                                for (int i = 0; i < ray_model_max_length; i++)
                                    if (ray_model.probability[(int)(disparity_pixels * 2)][i] > max / 200.0f)
                                        ray_model.probability[(int)(disparity_pixels * 2)][i] = probvalues[i];
                            }
                        }
                    }
                    
                }
            }

            // compress the array down to a more managable size
            compressRayModels(gridCellSize_mm);

            ray_model_to_graph_image(img, img_width, img_height);

            this.divisor = 1;
        }

        #endregion

        /// <summary>
        /// show a single ray model, with both occupancy and vacancy
        /// </summary>
        /// <param name="grid_layer">the grid upon which the rays will be drawn</param>
        /// <param name="grid_dimension">size of the grid</param>
        /// <param name="img">image within which to display the result</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="divisor">a dividing factor used to make the width of the grid smaller than its length</param>
        /// <param name="show_outline">draw a border around the probably occupied area</param>
        /// <param name="mirror">apply mirroring</param>
        public void showSingleRay(float[, ,] grid_layer, int grid_dimension, 
                                  byte[] img, int img_width, int img_height, 
                                  int divisor, bool show_outline, bool mirror)
        {
            // half a pixel of horizontal uncertainty
            sigma = 1.8f / (image_width * 2) * FOV_horizontal;
            sigma *= image_width / 320;
            this.divisor = divisor;

            int min_dist = (int)baseline;
            int max_dist = grid_dimension;
            int x = (image_width) * 499 / 1000;
            
            //x = (image_width) * 550 / 1000;

            float disparity_pixels = 3;

            float xx = x + disparity_pixels;

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

                throwRay(-baseline / 2, xx, vergence_radians, min_dist, max_dist, grid_layer, grid_dimension, 1,mirror);
                throwRay(baseline / 2, x, -vergence_radians, min_dist, max_dist, grid_layer, grid_dimension, 2,mirror);
                grid_layer_to_image(grid_layer, grid_dimension, img, img_width, img_height, true);

                if (show_outline)
                {

                    drawing.drawLine(img, img_width, img_height, (int)(x_left * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                 (int)(x_right * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                 255, 0, 0, 0, false);
                    drawing.drawLine(img, img_width, img_height, (int)(x_left * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                 (int)(x_start * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_start * img_height / grid_dimension),
                                 255, 0, 0, 0, false);
                    drawing.drawLine(img, img_width, img_height, (int)(x_right * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                 (int)(x_start * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_start * img_height / grid_dimension),
                                 255, 0, 0, 0, false);
                    drawing.drawLine(img, img_width, img_height, (int)(x_left * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                 (int)(x_end * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_end * img_height / grid_dimension),
                                 255, 0, 0, 0, false);
                    drawing.drawLine(img, img_width, img_height, (int)(x_right * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                 (int)(x_end * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_end * img_height / grid_dimension),
                                 255, 0, 0, 0, false);
                }

            }
            this.divisor = 1;
            
        }


        /// <summary>
        /// create a list of rays to be stored within poses
        /// </summary>
        /// <param name="head">head configuration</param>
        /// <param name="camera_index">index number for the stereo camera</param>
        /// <returns>list of evidence rays</returns>
        public List<evidenceRay> createObservation(stereoHead head, int camera_index)
        {
            List<evidenceRay> result = new List<evidenceRay>();

            // get essential data for this stereo camera
            baseline = head.calibration[camera_index].baseline;
            image_width = head.calibration[camera_index].leftcam.image_width;
            image_height = head.calibration[camera_index].leftcam.image_height;
            FOV_horizontal = head.calibration[camera_index].leftcam.camera_FOV_degrees * (float)Math.PI / 180.0f;
            FOV_vertical = FOV_horizontal * image_height / image_width;

            // calculate observational uncertainty as a standard deviation
            // of half a pixel
            sigma = 1.0f / (image_width * 1) * FOV_horizontal;

            // some head geometry
            pos3D headOrientation = head.cameraPosition[camera_index];
            pos3D cameraOrientation = new pos3D(0, 0, 0);
            cameraOrientation.pan = headOrientation.pan;
            cameraOrientation.tilt = headOrientation.tilt;
            cameraOrientation.roll = headOrientation.roll;

            if (head.features[camera_index] != null)  // if there are stereo features associated with this camera
            {
                float[] stereo_features = head.features[camera_index].features;
                float[] uncertainties = head.features[camera_index].uncertainties;
                int f2 = 0;
                for (int f = 0; f < stereo_features.Length; f += 3)
                {
                    // get the parameters of the feature
                    float image_x = stereo_features[f];
                    float image_y = stereo_features[f + 1];
                    float disparity = stereo_features[f + 2];

                    // create a ray
                    evidenceRay ray = createRay(image_x, image_y, disparity, uncertainties[f2],
                                                head.features[camera_index].colour[f2, 0],
                                                head.features[camera_index].colour[f2, 1],
                                                head.features[camera_index].colour[f2, 2]);

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

            return (result);
        }


        /// <summary>
        /// shows the gaussian distribution thrown into the grid
        /// this is only used for visualisation/debugging
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        public void showDistribution(byte[] img, int img_width, int img_height)
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
        /// this isn't really used anymore and should probably be removed
        /// </summary>
        /// <param name="ray1">the first ray</param>
        /// <param name="ray2">the second ray</param>
        /// <param name="intersectPos">intersection point of the two rays</param>
        /// <returns>true if the rays overlap</returns>
        /*
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

                    if (geometry.intersection(x0, y0, x1, y1, x2, y2, x3, y3, ref xi, ref yi))
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
                        geometry.intersection(x0, y0, x1, y1, x4, y4, x5, y5, ref xi2, ref zi1);
                        geometry.intersection(x2, y2, x3, y3, x4, y4, x5, y5, ref xi2, ref zi2);

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
        */



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

        /// <summary>
        /// creates a lookup table for the gaussian function
        /// </summary>
        /// <param name="levels">number of levels in the lookup table</param>
        /// <returns>lookup table</returns>
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

        /// <summary>
        /// creates a lookup table for half of the gaussian function
        /// </summary>
        /// <param name="levels">number of levels in the lookup table</param>
        /// <returns>lookup table</returns>
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
        /// <param name="x1">origin x coordinate</param>
        /// <param name="y1">origin y coordinate</param>
        /// <param name="x2">end point x coordinate</param>
        /// <param name="y2">end point y coordinate</param>
        /// <param name="linewidth">width of the line</param>
        /// <param name="additive"></param>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="rayNumber"></param>
        /// <param name="mirror"></param>
        public void addGaussianLine(float x1, float y1, float x2, float y2, 
                                    int linewidth, float additive, 
                                    float[,,] grid_layer, 
                                    int grid_dimension, int rayNumber,
                                    bool mirror)
        {
            float w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
            float cx, cy;
            float m, dist, max_dist, dxx, dyy, incr;
            int symmetry_x = grid_layer.GetLength(0)/2;

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
                                    
                                    if ((mirror) && (xx3 > symmetry_x))
                                        xx3 = symmetry_x - (xx3 - symmetry_x);
                                        
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

                                    if ((mirror) && (xx3 > symmetry_x))
                                        xx3 = symmetry_x - (xx3 - symmetry_x);
                                            
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


        /// <summary>
        /// creates a ray object which may be used to update occupancy grids
        /// </summary>
        /// <param name="x">x position of the feature within the camera image</param>
        /// <param name="y">y position of the feature within the camera image</param>
        /// <param name="disparity">stereo disparity in pixels</param>
        /// <param name="uncertainty">standard deviation</param>
        /// <param name="r">red colour component of the ray</param>
        /// <param name="g">green colour component of the ray</param>
        /// <param name="b">blue colour component of the ray</param>
        /// <returns>evidence ray object</returns>
        public evidenceRay createRay(float x, float y, float disparity, 
                                     float uncertainty, 
                                     byte r, byte g, byte b)
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
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="show_ray_outlines">show lines within which rays are projected</param>
        /// <param name="show_ray_vacancy">show vacancy areas or only areas where rays intersect</param>
        /// <param name="mirror">apply mirroring</param>
        public void showProbabilities(float[, ,] grid_layer, int grid_dimension, 
                                      byte[] img, int img_width, int img_height, 
                                      bool show_ray_outlines,
                                      bool show_vacancy,
                                      bool mirror)
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

                        throwRay(-baseline / 2, xx+offset, vergence_radians, min_dist, max_dist, grid_layer, grid_dimension,1, mirror);
                        throwRay(baseline / 2, x+offset, -vergence_radians, min_dist, max_dist, grid_layer, grid_dimension,2, mirror);
                        

                        if (show_ray_outlines)
                        {

                            //img.drawLine((int)(x_start * img.width / grid_dimension), img.height - 1 - (int)(y_start * img.height / grid_dimension),
                            //             (int)(x_end * img.width / grid_dimension), img.height - 1 - (int)(y_end * img.height / grid_dimension),
                            //             255, 0, 0, 0);
                            drawing.drawLine(img, img_width, img_height, (int)(x_left * img_width / (grid_dimension/divisor)), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                         (int)(x_right * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            drawing.drawLine(img, img_width, img_height, (int)(x_left * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                         (int)(x_start * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_start * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            drawing.drawLine(img, img_width, img_height, (int)(x_right * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                         (int)(x_start * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_start * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            /*
                            drawing.drawLine((int)(max_prob_x * img.width / grid_dimension), (int)(max_prob_y * img.height / grid_dimension),
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
                            drawing.drawLine(img, img_width, img_height, (int)(x_left * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_left * img_height / grid_dimension),
                                         (int)(x_end * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_end * img_height / grid_dimension),
                                         255, 0, 0, 0, false);
                            drawing.drawLine(img, img_width, img_height, (int)(x_right * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_right * img_height / grid_dimension),
                                         (int)(x_end * img_width / (grid_dimension / divisor)), img_height - 1 - (int)(y_end * img_height / grid_dimension),
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
        /// displays ray models for each disparity as a graph
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        public void ray_model_to_graph_image(Byte[] img, int img_width, int img_height)
        {
            float max_value = 0.0f;
            int max_length = 0;

            // clear the image
            for (int i = 0; i < img.Length; i++) img[i] = 255;

            // find the maximum probability
            for (int disparity_pixels = 1; disparity_pixels < ray_model.dimension_disparity; disparity_pixels++)
            {
                for (int i = 0; i < ray_model.length[disparity_pixels]; i++)
                {
                    if (ray_model.probability[disparity_pixels][i] > 0)
                    {
                        if (i > max_length) max_length = i;

                        if (ray_model.probability[disparity_pixels][i] > max_value)
                            max_value = ray_model.probability[disparity_pixels][i];
                    }
                }
            }

            if (max_value > 0)
            {
                max_value *= 1.1f;
                // for each possible diaparity value
                for (int disparity_pixels = 1; disparity_pixels < ray_model.dimension_disparity; disparity_pixels+=2)
                {
                    int prev_i = 0;
                    float prev_value = -1;
                    for (int i = 0; i < ray_model.length[disparity_pixels]; i++)
                    {
                        if (ray_model.probability[disparity_pixels][i] != prev_value)
                        {
                            if (i > 0)
                            {
                                int x = i * img_width / max_length;
                                int y = (int)(ray_model.probability[disparity_pixels][i] * img_height / max_value);
                                if (y >= img_height) y = img_height - 1;
                                y = img_height - 1 - y;
                                int prev_x = prev_i * img_width / max_length;
                                int prev_y = (int)(ray_model.probability[disparity_pixels][prev_i] * img_height / max_value);
                                if (prev_y >= img_height) prev_y = img_height - 1;
                                prev_y = img_height - 1 - prev_y;
                                drawing.drawLine(img, img_width, img_height, prev_x, prev_y, x, y, 0, 0, 0, 0, false);
                            }
                            prev_value = ray_model.probability[disparity_pixels][i];
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
        /// <param name="y_start">bottom intersection (closest to cameras)</param>
        /// <param name="x_end">top intersection (farthest from cameras)</param>
        /// <param name="y_end">top intersection (farthest from cameras)</param>
        /// <param name="x_left">left intersection</param>
        /// <param name="y_left">left intersection</param>
        /// <param name="x_right">right intersection</param>
        /// <param name="y_right">right intersection</param>
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

            // convert the horizontal locations of features in image coordinates
            // into angles
            float angle1_radians = ((x1 - half_width) * FOV_horizontal / image_width);
            float angle2_radians = ((x2 - half_width) * FOV_horizontal / image_width);
            
            // apply vergence
            angle1_radians += vergence_radians;
            angle2_radians -= vergence_radians;

            // offsets of the cameras relative to the centre of the baseline
            float offset_x1 = (grid_dimension / (2*divisor)) - (baseline / 2);
            float offset_x2 = (grid_dimension / (2*divisor)) + (baseline / 2);
            
            const int d = 100;  // some arbitrary number

            float xx1 = (d * (float)Math.Sin(angle1_radians));
            float xx2 = (d * (float)Math.Sin(angle2_radians));
            float yy = d;

            if (ray_uncertainty < 0.5f) ray_uncertainty = 0.5f;
            float uncertainty = sigma * d * ray_uncertainty;
            
            // locate the vertices of the diamond shape formed by
            // the intersection of two rays

            // find the starting point at which the rays first intersect
            geometry.intersection(offset_x1, 0.0f, offset_x1 + xx1 + uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 - uncertainty, yy,
                         ref x_start, ref y_start);
                         
            // find the end point at which the rays stop intersecting
            geometry.intersection(offset_x1, 0.0f, offset_x1 + xx1 - uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 + uncertainty, yy,
                         ref x_end, ref y_end);
                         
            // find the leftmost intersection point
            geometry.intersection(offset_x1, 0.0f, offset_x1 + xx1 - uncertainty, yy,
                         offset_x2, 0.0f, offset_x2 + xx2 - uncertainty, yy,
                         ref x_left, ref y_left);
                         
            // find the rightmost intersection point
            geometry.intersection(offset_x1, 0.0f, offset_x1 + xx1 + uncertainty, yy,
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
        /// <param name="vergence_angle">additional vergence angle in radians</param>
        /// <param name="min_dist">distance to begin throwing at</param>
        /// <param name="max_dist">distance to end throwing at</param>
        /// <param name="grid_layer"></param>
        /// <param name="grid_dimension"></param>
        /// <param name="rayNumber">used to avoid updating grid cells more than needed</param>
        /// <param name="mirror">apply mirroring</param>
        public void throwRay(float offset, float x, float vergence_angle, 
                             int min_dist, int max_dist, 
                             float[, ,] grid_layer, int grid_dimension, int rayNumber,
                             bool mirror)
        {
            // calc uncertainty in angle (+/- half a pixel)
            float angular_uncertainty = FOV_horizontal / (image_width * 2);

            // convert x positions an angle
            int half_width = image_width / 2;
            float angle = ((x - half_width) * FOV_horizontal / (float)image_width);
            angle += vergence_angle;

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
                
                // undistorting here means that features having constant
                // disparity become a line in real world coordinates rather than an arc
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

                addGaussianLine(tx, ty, bx, by, 2, magnitude, grid_layer, grid_dimension, rayNumber, mirror);
            }
        }

    }
}
