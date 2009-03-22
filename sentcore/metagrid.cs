/*
    Meta level occupancy grid
    Copyright (C) 2009 Bob Mottram
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
using System.Collections.Generic;
using sluggish.utilities;

namespace sentience.core
{
	public class metagrid
	{
        public const int TYPE_SIMPLE = 0;
        public const int TYPE_MULTI_HYPOTHESIS = 1;
        
        // the type of occupancy grid to be used
        public int grid_type;

        // sub grids
        protected occupancygridBase[] grid;  

        // weighting for each sub grid, used to compute localisation matching scores
        protected float[] grid_weight;       

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="no_of_grids">The number of sub grids</param>
        /// <param name="grid_type">the type of sub grids</param>
        /// <param name="dimension_mm">dimension of the smallest sub grid</param>
        /// <param name="dimension_vertical_mm">vertical dimension of the smallest sub grid</param>
        /// <param name="cellSize_mm">cell size of the smallest sub grid</param>
        /// <param name="localisationRadius_mm">localisation radius within the smallest sub grid</param>
        /// <param name="maxMappingRange_mm">maximum mapping radius within the smallest sub grid</param>
        /// <param name="vacancyWeighting">vacancy model weighting, typically between 0.2 and 2</param>
        public metagrid(
            int no_of_grids,
            int grid_type,
		    int dimension_mm, 
            int dimension_vertical_mm, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
        {
            int dimension_cells = dimension_mm / cellSize_mm;
            int dimension_cells_vertical = dimension_vertical_mm / cellSize_mm;
            if (vacancyWeighting < 0.2f) vacancyWeighting = 0.2f;
            this.grid_type = grid_type;
            grid = new occupancygridBase[no_of_grids];

            // values used to weight the matching score for each sub grid
            grid_weight = new float[no_of_grids];
            grid_weight[0] = 1;
            for (int i = 1; i < no_of_grids; i++)
                grid_weight[i] = grid_weight[i] * 0.5f;

            switch (grid_type)
            {
                case TYPE_SIMPLE:
                    {
                        for (int g = 0; g < no_of_grids; g++)
                            grid[g] = new occupancygridSimple(dimension_cells, dimension_cells_vertical, cellSize_mm * (g + 1), localisationRadius_mm * (g + 1), maxMappingRange_mm * (g + 1), vacancyWeighting);

                        break;
                    }
                case TYPE_MULTI_HYPOTHESIS:
                    {
                        for (int g = 0; g < no_of_grids; g++)
                            grid[g] = new occupancygridMultiHypothesis(dimension_cells, dimension_cells_vertical, cellSize_mm * (g + 1), localisationRadius_mm * (g + 1), maxMappingRange_mm * (g + 1), vacancyWeighting);

                        break;
                    }
            }
        }

        #endregion

        #region "clearing the grid"

        public void Clear()
        {
            for (int i = 0; i < grid.Length; i++)
                grid[i].Clear();
        }

        #endregion

        #region "setting the grid position"

        /// <summary>
        /// set the position and orientation of the grid
        /// </summary>
        /// <param name="centre_x_mm">centre x coordinate in millimetres</param>
        /// <param name="centre_y_mm">centre y coordinate in millimetres</param>
        /// <param name="centre_z_mm">centre z coordinate in millimetres</param>
        /// <param name="orientation_radians">grid orientation in radians</param>
        public void SetPosition(
            float centre_x_mm,
            float centre_y_mm,
            float centre_z_mm,
            float orientation_radians)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i].x = centre_x_mm;
                grid[i].y = centre_y_mm;
                grid[i].z = centre_z_mm;
                grid[i].pan = orientation_radians;
            }
        }

        #endregion

        #region "updating the grid"

        /// <summary>
        /// insert a stereo ray into the grid
        /// </summary>
        /// <param name="ray">stereo ray</param>
        /// <param name="origin">pose from which this observation was made</param>
        /// <param name="sensormodel_lookup">sensor model to be used</param>
        /// <param name="left_camera_location">left stereo camera position and orientation</param>
        /// <param name="right_camera_location">right stereo camera position and orientation</param>
        /// <param name="localiseOnly">whether we are mapping or localising</param>
        /// <returns>localisation matching score</returns>
        public float Insert(
            evidenceRay ray,
            particlePose origin,
            rayModelLookup sensormodel_lookup,
            pos3D left_camera_location,
            pos3D right_camera_location,
            bool localiseOnly)
        {
            float matchingScore = 0;

            switch (grid_type)
            {
                case TYPE_MULTI_HYPOTHESIS:
                    {
                        for (int i = 0; i < grid.Length; i++)
                        {
                            occupancygridMultiHypothesis grd = (occupancygridMultiHypothesis)grid[i];
                            matchingScore += grid_weight[i] * grd.Insert(ray, origin, sensormodel_lookup, left_camera_location, right_camera_location, localiseOnly);
                        }
                        break;
                    }
            }
            return (matchingScore);
        }

        /// <summary>
        /// insert a stereo ray into the grid
        /// </summary>
        /// <param name="ray">stereo ray</param>
        /// <param name="sensormodel_lookup">sensor model to be used</param>
        /// <param name="left_camera_location">left stereo camera position and orientation</param>
        /// <param name="right_camera_location">right stereo camera position and orientation</param>
        /// <param name="localiseOnly">whether we are mapping or localising</param>
        /// <returns>localisation matching score</returns>
        public float Insert(
            evidenceRay ray,
            rayModelLookup sensormodel_lookup,
            pos3D left_camera_location,
            pos3D right_camera_location,
            bool localiseOnly)
        {
            float matchingScore = 0;
            switch (grid_type)
            {
                case TYPE_SIMPLE:
                {
                    for (int i = 0; i < grid.Length; i++)
                    {
                        occupancygridSimple grd = (occupancygridSimple)grid[i];
                        matchingScore += grid_weight[i] * grd.Insert(ray, sensormodel_lookup, left_camera_location, right_camera_location, localiseOnly);
                    }
                    break;
                }                
            }
            return (matchingScore);
        }

        #endregion

        #region "localisation"

        static void FindBestPose(
            List<pos3D> poses,
            List<float> scores,
            ref pos3D best_pose,
            float sampling_radius_major_mm,
            byte[] img,
            int img_width,
            int img_height)
        {
            float max_score = float.MinValue;
            for (int i = 0; i < scores.Count; i++)
            {
                if (scores[i] > max_score)
                {
                    max_score = scores[i];
                }
            }

            // create an image showing the results
            if (img != null)
            {
                float max_radius = sampling_radius_major_mm * 0.1f;
                for (int i = img.Length - 1; i >= 0; i--) img[i] = 0;
                int tx = -(int)(sampling_radius_major_mm * 0.5f);
                int ty = -(int)(sampling_radius_major_mm * 0.5f);
                int bx = (int)(sampling_radius_major_mm * 0.5f);
                int by = (int)(sampling_radius_major_mm * 0.5f);

                for (int i = 0; i < poses.Count; i++)
                {
                    pos3D p = poses[i];
                    float score = scores[i];
                    int x = (int)((p.x - tx) * img_width / sampling_radius_major_mm);
                    int y = (int)((p.y - ty) * img_height / sampling_radius_major_mm);
                    float radius = score * max_radius / max_score;
                }
            }
        }

        /// <summary>
        /// Localisation
        /// </summary>
        /// <param name="baseline_mm">baseline distance for each stereo camera in millimetres</param>
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="stereo_features">stereo features (disparities) for each stereo camera</param>
        /// <param name="stereo_features_colour">stereo feature colours for each stereo camera</param>
        /// <param name="stereo_features_uncertainties">stereo feature uncertainties (priors) for each stereo camera</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        /// <param name="left_camera_location">position and orientation of the left camera on each stereo camera</param>
        /// <param name="right_camera_location">position and orientation of the right camera on each stereo camera</param>
        /// <param name="no_of_samples">number of sample poses</param>
        /// <param name="sampling_radius_major_mm">major radius for samples, in the direction of robot movement</param>
        /// <param name="sampling_radius_minor_mm">minor radius for samples, perpendicular to the direction of robot movement</param>
        /// <param name="robot_pose">position and orientation of the robots centre of rotation</param>
        /// <param name="max_orientation_variance">maximum variance in orientation in radians, used to create sample poses</param>
        /// <param name="max_tilt_variance">maximum variance in tilt angle in radians, used to create sample poses</param>
        /// <param name="poses">list of poses tried</param>
        /// <param name="pose_score">list of pose matching scores</param>
        /// <param name="best_robot_pose">returned best pose estimate</param>
        /// <returns>best localisation matching score</returns>
        public float Localise(
            float[] baseline_mm,
            int[] image_width,
            int[] image_height,
            float[] FOV_degrees,
		    float[][] stereo_features,
		    byte[][,] stereo_features_colour,
		    float[][] stereo_features_uncertainties,
            stereoModel[] sensormodel,
            pos3D[] left_camera_location,
            pos3D[] right_camera_location,
            int no_of_samples,
            float sampling_radius_major_mm,
            float sampling_radius_minor_mm,
            pos3D robot_pose,
            float max_orientation_variance,
            float max_tilt_variance,
            List<pos3D> poses,
            List<float> pose_score,
            ref pos3D best_robot_pose)
        {
            float best_matching_score = float.MinValue;

            poses.Clear();
            pose_score.Clear();

            // positions of the left and right camera relative to the robots centre of rotation
            pos3D[] relative_left_cam = new pos3D[left_camera_location.Length];
            pos3D[] relative_right_cam = new pos3D[right_camera_location.Length];
            for (int cam = 0; cam < baseline_mm.Length; cam++)
            {
                relative_left_cam[cam] = left_camera_location[cam].Subtract(robot_pose);
                relative_right_cam[cam] = right_camera_location[cam].Subtract(robot_pose);
            }

            pos3D stereo_camera_centre = new pos3D(0, 0, 0);

            // try a number of random poses
            Random rnd = new Random();  // no fixed seed
            for (int i = 0; i < no_of_samples; i++)
            {
                // create a random sample
                float x_offset = (rnd.Next() - 0.5f) * sampling_radius_major_mm * 2;
                float y_offset = (rnd.Next() - 0.5f) * sampling_radius_minor_mm * 2;

                pos3D sample_pose = new pos3D(x_offset, y_offset, 0);
                sample_pose.pan = robot_pose.pan + ((rnd.Next() - 0.5f) * 2 * max_orientation_variance);
                sample_pose.tilt = robot_pose.tilt + ((rnd.Next() - 0.5f) * 2 * max_tilt_variance);
                
                float matching_score = 0;

                for (int cam = 0; cam < baseline_mm.Length; cam++)
                {
                    pos3D sample_pose_left_cam = relative_left_cam[cam].add(sample_pose);
                    sample_pose_left_cam.pan = 0;
                    sample_pose_left_cam.tilt = 0;
                    sample_pose_left_cam.roll = 0;
                    sample_pose_left_cam = sample_pose_left_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);
                    sample_pose_left_cam.x += robot_pose.x;
                    sample_pose_left_cam.y += robot_pose.y;
                    sample_pose_left_cam.z += robot_pose.z;

                    pos3D sample_pose_right_cam = relative_right_cam[cam].add(sample_pose);
                    sample_pose_right_cam.pan = 0;
                    sample_pose_right_cam.tilt = 0;
                    sample_pose_right_cam.roll = 0;
                    sample_pose_right_cam = sample_pose_right_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);
                    sample_pose_right_cam.x += robot_pose.x;
                    sample_pose_right_cam.y += robot_pose.y;
                    sample_pose_right_cam.z += robot_pose.z;

                    // centre position between the left and right cameras
                    stereo_camera_centre.x = sample_pose_left_cam.x + ((sample_pose_right_cam.x - sample_pose_left_cam.x) * 0.5f);
                    stereo_camera_centre.y = sample_pose_left_cam.y + ((sample_pose_right_cam.y - sample_pose_left_cam.y) * 0.5f);
                    stereo_camera_centre.z = sample_pose_left_cam.z + ((sample_pose_right_cam.z - sample_pose_left_cam.z) * 0.5f);
                    stereo_camera_centre.pan = sample_pose.pan;
                    stereo_camera_centre.tilt = sample_pose.tilt;
                    stereo_camera_centre.roll = sample_pose.roll;

                    // create a set of stereo rays as observed from this pose
                    List<evidenceRay> rays = sensormodel[cam].createObservation(
                        stereo_camera_centre,
                        baseline_mm[cam],
                        image_width[cam],
                        image_height[cam],
                        FOV_degrees[cam],
                        stereo_features[cam],
                        stereo_features_colour[cam],
                        stereo_features_uncertainties[cam],
                        true);

                    for (int r = 0; r < rays.Count; r++)
                    {
                        matching_score += Insert(rays[r], sensormodel[cam].ray_model, sample_pose_left_cam, sample_pose_right_cam, true);
                    }
                }

                // add the pose to the list
                poses.Add(sample_pose);
                pose_score.Add(matching_score);
            }

            FindBestPose(poses, pose_score, ref best_robot_pose, sampling_radius_major_mm, null, 0, 0);

            return (best_matching_score);
        }

        #endregion

    }
}
