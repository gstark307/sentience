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
            for (int i = 0; i < no_of_grids; i++)
                grid_weight[i] = 1;

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

        /// <summary>
        /// insert an observation
        /// </summary>
        /// <param name="stereo_rays">list of stereo rays for each stereo camera</param>
        /// <param name="sensormodel_lookup">sensor model for each stereo camera</param>
        /// <param name="left_camera_location">position and orientation of the left camera on each stereo camera</param>
        /// <param name="right_camera_location">position and orientation of the right camera on each stereo camera</param>
        /// <param name="no_of_samples">number of sample poses</param>
        /// <param name="sampling_radius_major_mm">major radius for samples, in the direction of robot movement</param>
        /// <param name="sampling_radius_minor_mm">minor radius for samples, perpendicular to the direction of robot movement</param>
        /// <param name="robot_pose">position and orientation of the robots centre of rotation</param>
        /// <param name="max_orientation_variance">maximum variance in orientation in radians, use to create sample poses</param>
        /// <param name="best_robot_pose">returned best pose estimate</param>
        /// <returns>best localisation matching score</returns>
        public float Insert(
            List<evidenceRay>[] stereo_rays,
            rayModelLookup[] sensormodel_lookup,
            pos3D[] left_camera_location,
            pos3D[] right_camera_location,
            int no_of_samples,
            float sampling_radius_major_mm,
            float sampling_radius_minor_mm,
            pos3D robot_pose,
            float max_orientation_variance,
            ref float best_robot_pose)
        {
            float best_matching_score = 0;

            // positions of the left and right camera relative to the robots centre of rotation
            pos3D[] relative_left_cam = new pos3D[left_camera_location.Length];
            pos3D[] relative_right_cam = new pos3D[right_camera_location.Length];
            for (int cam = 0; cam < stereo_rays.Length; cam++)
            {
                relative_left_cam[cam] = left_camera_location[cam].Subtract(robot_pose);
                relative_right_cam[cam] = right_camera_location[cam].Subtract(robot_pose);
            }

            Random rnd = new Random(0);
            for (int i = 0; i < no_of_samples; i++)
            {
                // create a random sample
                float angle = (float)(rnd.Next() * Math.PI * 2);
                float radius = (float)rnd.Next() * sampling_radius_major_mm;
                float x = radius * (float)Math.Sin(angle);
                float y = radius * (float)Math.Cos(angle);
                float sample_orientation = robot_pose.pan + ((rnd.Next() - 0.5f) * 2 * max_orientation_variance);

                // bias in the direction of movement
                x = x * sampling_radius_minor_mm / sampling_radius_major_mm;

                pos3D sample_pose = new pos3D(x, y, 0);
                sample_pose.pan = angle;

                for (int cam = 0; cam < stereo_rays.Length; cam++)
                {
                    pos3D sample_pose_left_cam = relative_left_cam[cam].add(sample_pose);
                    sample_pose_left_cam.pan = relative_left_cam[cam].pan;
                    sample_pose_left_cam.tilt = relative_left_cam[cam].tilt;
                    sample_pose_left_cam.roll = relative_left_cam[cam].roll;
                    sample_pose_left_cam = sample_pose_left_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);

                    pos3D sample_pose_right_cam = relative_right_cam[cam].add(sample_pose);
                    sample_pose_right_cam.pan = relative_right_cam[cam].pan;
                    sample_pose_right_cam.tilt = relative_right_cam[cam].tilt;
                    sample_pose_right_cam.roll = relative_right_cam[cam].roll;
                    sample_pose_right_cam = sample_pose_right_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);
                }
            }

            return (best_matching_score);
        }

        #endregion

    }
}
