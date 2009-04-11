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
        
        // centre position of the grids
        public float x,y,z;

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

        #region "probing the grid"
		
		/// <summary>
		/// probes the grid using the given 3D line and returns the distance to the nearest occupied grid cell
		/// </summary>
		/// <param name="grid_index">index number of the grid to be probed</param>
		/// <param name="x0_mm">start x coordinate</param>
		/// <param name="y0_mm">start y coordinate</param>
		/// <param name="z0_mm">start z coordinate</param>
		/// <param name="x1_mm">end x coordinate</param>
		/// <param name="y1_mm">end y coordinate</param>
		/// <param name="z1_mm">end z coordinate</param>
		/// <returns>range to the nearest occupied grid cell in millimetres</returns>
		public float ProbeRange(
		    int grid_index,
	        float x0_mm,
		    float y0_mm,
		    float z0_mm,
	        float x1_mm,
		    float y1_mm,
		    float z1_mm)
		{
			float range_mm = -1;
			switch(grid_type)
			{
			    case TYPE_SIMPLE:
			    {
				    range_mm = grid[grid_index].ProbeRange(x0_mm, y0_mm, z0_mm, x1_mm, y1_mm, z1_mm);
				    break;
			    }
			    case TYPE_MULTI_HYPOTHESIS:
			    {
				    occupancygridMultiHypothesis grd = (occupancygridMultiHypothesis)grid[grid_index];
				    // TODO get the best pose estimate
				    particlePose pose = null;
				    range_mm = grd.ProbeRange(x0_mm, y0_mm, z0_mm, x1_mm, y1_mm, z1_mm);
				    break;
			    }
			}
			return(range_mm);
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
            x = centre_x_mm;
            y = centre_y_mm;
            z = centre_z_mm;
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

        #region "mapping"

        /// <summary>
        /// Mapping
        /// </summary>
        /// <param name="body_width_mm">width of the robot body in millimetres</param>
        /// <param name="body_length_mm">length of the robot body in millimetres</param>
        /// <param name="body_centre_of_rotation_x">x centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_y">y centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_z">z centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="head_centroid_x">head centroid x position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_y">head centroid y position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_z">head centroid z position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_pan">head pan angle in radians</param>
        /// <param name="head_tilt">head tilt angle in radians</param>
        /// <param name="head_roll">head roll angle in radians</param>
        /// <param name="baseline_mm">stereo camera baseline in millimetres</param>
        /// <param name="stereo_camera_position_x">stereo camera x position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_y">stereo camera y position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_z">stereo camera z position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_pan">stereo camera pan in radians relative to the head</param>
        /// <param name="stereo_camera_tilt">stereo camera tilt in radians relative to the head</param>
        /// <param name="stereo_camera_roll">stereo camera roll in radians relative to the head</param>
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="stereo_features">stereo features (disparities) for each stereo camera</param>
        /// <param name="stereo_features_colour">stereo feature colours for each stereo camera</param>
        /// <param name="stereo_features_uncertainties">stereo feature uncertainties (priors) for each stereo camera</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        /// <param name="left_camera_location">returned position and orientation of the left camera on each stereo camera</param>
        /// <param name="right_camera_location">returned position and orientation of the right camera on each stereo camera</param>
        /// <param name="robot_pose">current estimated position and orientation of the robots centre of rotation</param>
        public void Map(
		    float body_width_mm,
		    float body_length_mm,
		    float body_centre_of_rotation_x,
		    float body_centre_of_rotation_y,
		    float body_centre_of_rotation_z,
		    float head_centroid_x,
		    float head_centroid_y,
		    float head_centroid_z,
		    float head_pan,
		    float head_tilt,
		    float head_roll,
		    float baseline_mm,
		    float stereo_camera_position_x,
		    float stereo_camera_position_y,
		    float stereo_camera_position_z,
		    float stereo_camera_pan,
		    float stereo_camera_tilt,
		    float stereo_camera_roll,
            int image_width,
            int image_height,
            float FOV_degrees,
		    float[] stereo_features,
		    byte[,] stereo_features_colour,
		    float[] stereo_features_uncertainties,
            stereoModel sensormodel,
            ref pos3D left_camera_location,
            ref pos3D right_camera_location,
            pos3D robot_pose)
        {
            // positions of the left and right camera relative to the robots centre of rotation
            pos3D head_location = new pos3D(0,0,0);
            pos3D camera_centre_location = null;
            pos3D relative_left_cam = null;
            pos3D relative_right_cam = null;
			
            occupancygridBase.calculateCameraPositions(
	            body_width_mm,
	            body_length_mm,
	            body_centre_of_rotation_x,
	            body_centre_of_rotation_y,
	            body_centre_of_rotation_z,
	            head_centroid_x,
	            head_centroid_y,
	            head_centroid_z,
	            head_pan,
	            head_tilt,
	            head_roll,
	            baseline_mm,
	            stereo_camera_position_x,
	            stereo_camera_position_y,
	            stereo_camera_position_z,
	            stereo_camera_pan,
	            stereo_camera_tilt,
	            stereo_camera_roll,
                ref head_location,
                ref camera_centre_location,
                ref relative_left_cam,
                ref relative_right_cam);
                                       
            left_camera_location = relative_left_cam.translate(robot_pose.x, robot_pose.y, robot_pose.z);
            right_camera_location = relative_right_cam.translate(robot_pose.x, robot_pose.y, robot_pose.z);

            pos3D stereo_camera_centre = new pos3D(0, 0, 0);

            // update the grid
			
            // centre position between the left and right cameras
            stereo_camera_centre.x = left_camera_location.x + ((right_camera_location.x - left_camera_location.x) * 0.5f);
            stereo_camera_centre.y = left_camera_location.y + ((right_camera_location.y - left_camera_location.y) * 0.5f);
            stereo_camera_centre.z = left_camera_location.z + ((right_camera_location.z - left_camera_location.z) * 0.5f);
            stereo_camera_centre.pan = robot_pose.pan + head_pan + stereo_camera_pan;
            stereo_camera_centre.tilt = robot_pose.tilt + head_tilt + stereo_camera_tilt;
            stereo_camera_centre.roll = robot_pose.roll + head_roll + stereo_camera_roll;

            // create a set of stereo rays as observed from this pose
            List<evidenceRay> rays = sensormodel.createObservation(
                stereo_camera_centre,
                baseline_mm,
                image_width,
                image_height,
                FOV_degrees,
                stereo_features,
                stereo_features_colour,
                stereo_features_uncertainties,
                true);

		    // insert rays into the occupancy grid
            for (int r = 0; r < rays.Count; r++)
            {
                Insert(rays[r], sensormodel.ray_model, left_camera_location, right_camera_location, false);
            }
        }

        #endregion

        #region "localisation"        
		
        /// <summary>
        /// Localisation
        /// </summary>
        /// <param name="body_width_mm">width of the robot body in millimetres</param>
        /// <param name="body_length_mm">length of the robot body in millimetres</param>
        /// <param name="body_centre_of_rotation_x">x centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_y">y centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_z">z centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="head_centroid_x">head centroid x position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_y">head centroid y position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_z">head centroid z position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_pan">head pan angle in radians</param>
        /// <param name="head_tilt">head tilt angle in radians</param>
        /// <param name="head_roll">head roll angle in radians</param>
        /// <param name="baseline_mm">stereo camera baseline in millimetres</param>
        /// <param name="stereo_camera_position_x">stereo camera x position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_y">stereo camera y position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_z">stereo camera z position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_pan">stereo camera pan in radians relative to the head</param>
        /// <param name="stereo_camera_tilt">stereo camera tilt in radians relative to the head</param>
        /// <param name="stereo_camera_roll">stereo camera roll in radians relative to the head</param>
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="stereo_features">stereo features (disparities) for each stereo camera</param>
        /// <param name="stereo_features_colour">stereo feature colours for each stereo camera</param>
        /// <param name="stereo_features_uncertainties">stereo feature uncertainties (priors) for each stereo camera</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        /// <param name="left_camera_location">returned position and orientation of the left camera on each stereo camera</param>
        /// <param name="right_camera_location">returned position and orientation of the right camera on each stereo camera</param>
        /// <param name="no_of_samples">number of sample poses</param>
        /// <param name="sampling_radius_major_mm">major radius for samples, in the direction of robot movement</param>
        /// <param name="sampling_radius_minor_mm">minor radius for samples, perpendicular to the direction of robot movement</param>
        /// <param name="robot_pose">current estimated position and orientation of the robots centre of rotation</param>
        /// <param name="max_orientation_variance">maximum variance in orientation in radians, used to create sample poses</param>
        /// <param name="max_tilt_variance">maximum variance in tilt angle in radians, used to create sample poses</param>
        /// <param name="max_roll_variance">maximum variance in roll angle in radians, used to create sample poses</param>
        /// <param name="poses">list of poses tried</param>
        /// <param name="pose_score">list of pose matching scores</param>
        /// <param name="pose_offset">offset of the best pose from the current one</param>
		/// <param name="rnd">random number generator</param>
        /// <returns>best localisation matching score</returns>
        public float Localise(
		    float body_width_mm,
		    float body_length_mm,
		    float body_centre_of_rotation_x,
		    float body_centre_of_rotation_y,
		    float body_centre_of_rotation_z,
		    float head_centroid_x,
		    float head_centroid_y,
		    float head_centroid_z,
		    float head_pan,
		    float head_tilt,
		    float head_roll,
		    float[] baseline_mm,
		    float[] stereo_camera_position_x,
		    float[] stereo_camera_position_y,
		    float[] stereo_camera_position_z,
		    float[] stereo_camera_pan,
		    float[] stereo_camera_tilt,
		    float[] stereo_camera_roll,
            int[] image_width,
            int[] image_height,
            float[] FOV_degrees,
		    float[][] stereo_features,
		    byte[][,] stereo_features_colour,
		    float[][] stereo_features_uncertainties,
            stereoModel[] sensormodel,
            ref pos3D[] left_camera_location,
            ref pos3D[] right_camera_location,
            int no_of_samples,
            float sampling_radius_major_mm,
            float sampling_radius_minor_mm,
            pos3D robot_pose,
            float max_orientation_variance,
            float max_tilt_variance,
            float max_roll_variance,
            List<pos3D> poses,
            List<float> pose_score,
		    Random rnd,
            ref pos3D pose_offset)
        {
            float best_matching_score = float.MinValue;

            poses.Clear();
            pose_score.Clear();
		    gridCells.CreateMoireGrid(
		        sampling_radius_major_mm,
		        sampling_radius_minor_mm,
		        no_of_samples,
		        robot_pose.pan,
		        robot_pose.tilt,
		        robot_pose.roll,
		        max_orientation_variance,
		        max_tilt_variance,
		        max_roll_variance,
		        rnd,
		        ref poses,
		        null, null, 0, 0);
		        
            // positions of the left and right camera relative to the robots centre of rotation
            pos3D head_location = new pos3D(0,0,0);
            left_camera_location = new pos3D[baseline_mm.Length];
            right_camera_location = new pos3D[baseline_mm.Length];
            pos3D[] camera_centre_location = new pos3D[baseline_mm.Length];
            pos3D[] relative_left_cam = new pos3D[baseline_mm.Length];
            pos3D[] relative_right_cam = new pos3D[baseline_mm.Length];
            for (int cam = 0; cam < baseline_mm.Length; cam++)
            {
                occupancygridBase.calculateCameraPositions(
		            body_width_mm,
		            body_length_mm,
		            body_centre_of_rotation_x,
		            body_centre_of_rotation_y,
		            body_centre_of_rotation_z,
		            head_centroid_x,
		            head_centroid_y,
		            head_centroid_z,
		            head_pan,
		            head_tilt,
		            head_roll,
		            baseline_mm[cam],
		            stereo_camera_position_x[cam],
		            stereo_camera_position_y[cam],
		            stereo_camera_position_z[cam],
		            stereo_camera_pan[cam],
		            stereo_camera_tilt[cam],
		            stereo_camera_roll[cam],
                    ref head_location,
                    ref camera_centre_location[cam],
                    ref relative_left_cam[cam],
                    ref relative_right_cam[cam]);
                                           
                left_camera_location[cam] = relative_left_cam[cam].translate(robot_pose.x, robot_pose.y, robot_pose.z);
                right_camera_location[cam] = relative_right_cam[cam].translate(robot_pose.x, robot_pose.y, robot_pose.z);
            }

            pos3D stereo_camera_centre = new pos3D(0, 0, 0);

            pose_score.Clear();
            for (int p = 0; p < poses.Count; p++)
            {
                pose_score.Add(0);
            }

            // try a number of random poses
			// we can do this in parallel
		    Parallel.For(0, poses.Count, delegate(int i)
		    {			
                pos3D sample_pose = poses[i];
                
                float matching_score = 0;

                for (int cam = 0; cam < baseline_mm.Length; cam++)
                {
					// update the position of the left camera for this pose
                    pos3D sample_pose_left_cam = relative_left_cam[cam].add(sample_pose);
                    sample_pose_left_cam.pan = 0;
                    sample_pose_left_cam.tilt = 0;
                    sample_pose_left_cam.roll = 0;
                    sample_pose_left_cam = sample_pose_left_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);
                    sample_pose_left_cam.x += robot_pose.x;
                    sample_pose_left_cam.y += robot_pose.y;
                    sample_pose_left_cam.z += robot_pose.z;

					// update the position of the right camera for this pose
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

					// insert rays into the occupancy grid
                    for (int r = 0; r < rays.Count; r++)
                    {
                        matching_score += Insert(rays[r], sensormodel[cam].ray_model, sample_pose_left_cam, sample_pose_right_cam, true);
                    }
                }

                // add the pose to the list
				sample_pose.pan -= robot_pose.pan;
				sample_pose.tilt -= robot_pose.tilt;
				sample_pose.roll -= robot_pose.roll;				
                pose_score[i] = matching_score;
            });

			// locate the best possible pose
			pos3D best_robot_pose = new pos3D(0, 0, 0);
            gridCells.FindBestPose(poses, pose_score, ref best_robot_pose, sampling_radius_major_mm, null, null, 0, 0, 0, 0);

            // rotate the best pose to the robot's current orientation
            // this becomes an offset, which may be used for course correction
            pose_offset = best_robot_pose.rotate(robot_pose.pan, robot_pose.tilt, robot_pose.roll);
            
            // orientation relative to the current heading
            pose_offset.pan = best_robot_pose.pan;
            pose_offset.tilt = best_robot_pose.tilt;
            pose_offset.roll = best_robot_pose.roll;

            // range checks                        
            if (Math.Abs(pose_offset.pan) > max_orientation_variance) Console.WriteLine("pose_offset pan out of range");
            if (Math.Abs(pose_offset.tilt) > max_tilt_variance) Console.WriteLine("pose_offset tilt out of range");
            if (Math.Abs(pose_offset.roll) > max_roll_variance) Console.WriteLine("pose_offset roll out of range");

            return (best_matching_score);
        }

        #endregion
		
        #region "inserting simulated structures, mainly for unit testing purposes"
		
		/// <summary>
		/// create a room-like structure within the grid
		/// </summary>
		/// <param name="tx_mm">top left x coordinate of the room</param>
		/// <param name="ty_mm">top left y coordinate of the room</param>
		/// <param name="bx_mm">bottom right x coordinate of the room</param>
		/// <param name="by_mm">bottom right y coordinate of the room</param>
		/// <param name="height_mm">height of the room</param>
		/// <param name="wall_thickness_mm">thickness of the walls</param>
		/// <param name="probability_variance">variation in probabilities, typically less than 0.2</param>
		/// <param name="floor_r">red component of the floor colour</param>
		/// <param name="floor_g">green component of the floor colour</param>
		/// <param name="floor_b">blue component of the floor colour</param>
		/// <param name="walls_r">red component of the wall colour</param>
		/// <param name="walls_g">green component of the wall colour</param>
		/// <param name="walls_b">blue component of the wall colour</param>
		/// <param name="left_wall_doorways">centre position of doorways on the left wall in mm</param>
		/// <param name="top_wall_doorways">centre position of doorways on the top wall in mm</param>
		/// <param name="right_wall_doorways">centre position of doorways on the right wall in mm</param>
		/// <param name="bottom_wall_doorways">centre position of doorways on the bottom wall in mm</param>
		/// <param name="doorway_width_mm">width of doors</param>
		/// <param name="doorway_height_mm">height of doors</param>
		public void InsertRoom(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int height_mm,
		    int wall_thickness_mm,                           
		    float probability_variance,
		    int floor_r, int floor_g, int floor_b,
		    int walls_r, int walls_g, int walls_b,
		    List<int> left_wall_doorways,
		    List<int> top_wall_doorways,
		    List<int> right_wall_doorways,
		    List<int> bottom_wall_doorways,
		    int doorway_width_mm,
		    int doorway_height_mm)
		{
			for (int grd = 0; grd < grid.Length; grd++)
			{
                grid[grd].InsertRoom(
		            tx_mm, ty_mm,
		            bx_mm, by_mm,
		            height_mm,
		            wall_thickness_mm,                           
		            probability_variance,
		            floor_r, floor_g, floor_b,
		            walls_r, walls_g, walls_b,
		            left_wall_doorways,
		            top_wall_doorways,
		            right_wall_doorways,
		            bottom_wall_doorways,
		            doorway_width_mm,
		            doorway_height_mm);
			}
		}
		
		/// <summary>
		/// inserts a simulated block into the grid
		/// </summary>
		/// <param name="tx_mm">start x coordinate</param>
		/// <param name="ty_mm">start y coordinate</param>
		/// <param name="bx_mm">end x coordinate</param>
		/// <param name="by_mm">end y coordinate</param>
		/// <param name="bottom_height_mm">bottom height of the block</param>
		/// <param name="top_height_mm">bottom height of the block</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public void InsertBlockCells(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int bottom_height_mm,
		    int top_height_mm,
		    float probability_variance,
		    int r, int g, int b)
		{
			for (int grd = 0; grd < grid.Length; grd++)
			{
			    int cellSize_mm = grid[grd].cellSize_mm;
				
			    int tx_cell = (grid[grd].dimension_cells/2) + (int)((tx_mm - grid[grd].x) / cellSize_mm);
			    int ty_cell = (grid[grd].dimension_cells/2) + (int)((ty_mm - grid[grd].y) / cellSize_mm);
			    int bx_cell = (grid[grd].dimension_cells/2) + (int)((bx_mm - grid[grd].x) / cellSize_mm);
			    int by_cell = (grid[grd].dimension_cells/2) + (int)((by_mm - grid[grd].y) / cellSize_mm);
			    int bottom_height_cells = bottom_height_mm / cellSize_mm;
			    int top_height_cells = top_height_mm / cellSize_mm;
				
			    grid[grd].InsertBlockCells(
		            tx_cell, ty_cell,
		            bx_cell, by_cell,
		            bottom_height_cells,
		            top_height_cells,
		            probability_variance,
			        r, g, b);			
			}
		}		
		
		/// <summary>
		/// inserts a simulated wall into the grid
		/// </summary>
		/// <param name="tx_mm">start x coordinate</param>
		/// <param name="ty_mm">start y coordinate</param>
		/// <param name="bx_mm">end x coordinate</param>
		/// <param name="by_mm">end y coordinate</param>
		/// <param name="height_mm">height of the wall</param>
		/// <param name="thickness_mm">thickness of the wall</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public void InsertWall(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int height_mm,
		    int thickness_mm,
		    float probability_variance,
		    int r, int g, int b)
		{
			for (int grd = 0; grd < grid.Length; grd++)
			{
			    int cellSize_mm = grid[grd].cellSize_mm;
				
			    int tx_cell = (grid[grd].dimension_cells/2) + (int)((tx_mm - grid[grd].x) / cellSize_mm);
			    int ty_cell = (grid[grd].dimension_cells/2) + (int)((ty_mm - grid[grd].y) / cellSize_mm);
			    int bx_cell = (grid[grd].dimension_cells/2) + (int)((bx_mm - grid[grd].x) / cellSize_mm);
			    int by_cell = (grid[grd].dimension_cells/2) + (int)((by_mm - grid[grd].y) / cellSize_mm);
			    int height_cells = height_mm / cellSize_mm;
			    int thickness_cells = thickness_mm / cellSize_mm;
				
			    grid[grd].InsertWallCells(
		            tx_cell, ty_cell,
		            bx_cell, by_cell,
		            height_cells,
		            thickness_cells,
		            probability_variance,
			        r, g, b);			
			}
		}
		
		
		/// <summary>
		/// inserts a simulated doorway into the grid
		/// </summary>
		/// <param name="tx_mm">start x coordinate</param>
		/// <param name="ty_mm">start y coordinate</param>
		/// <param name="bx_mm">end x coordinate</param>
		/// <param name="by_mm">end y coordinate</param>
		/// <param name="wall_height_mm">height of the wall</param>
		/// <param name="door_height_mm">height of the door</param>
		/// <param name="door_width_mm">width of the door</param>
		/// <param name="thickness_mm">thickness of the wall</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public void InsertDoorway(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int wall_height_mm,
		    int door_height_mm,
		    int door_width_mm,
		    int thickness_mm,
		    float probability_variance,
		    int r, int g, int b)
		{		
			for (int grd = 0; grd < grid.Length; grd++)
			{
			    int cellSize_mm = grid[grd].cellSize_mm;
				
			    int tx_cell = (grid[grd].dimension_cells/2) + (int)((tx_mm - grid[grd].x) / cellSize_mm);
			    int ty_cell = (grid[grd].dimension_cells/2) + (int)((ty_mm - grid[grd].y) / cellSize_mm);
			    int bx_cell = (grid[grd].dimension_cells/2) + (int)((bx_mm - grid[grd].x) / cellSize_mm);
			    int by_cell = (grid[grd].dimension_cells/2) + (int)((by_mm - grid[grd].y) / cellSize_mm);
			    int wall_height_cells = wall_height_mm / cellSize_mm;
			    int door_height_cells = door_height_mm / cellSize_mm;
			    int door_width_cells = door_width_mm / cellSize_mm;
			    int thickness_cells = thickness_mm / cellSize_mm;
			
			    grid[grd].InsertDoorwayCells(
			        tx_cell, ty_cell,
			        bx_cell, by_cell,
			        wall_height_cells,
			        door_height_cells,
			        door_width_cells,
			        thickness_cells,
			        probability_variance,
			        r, g, b);
			}
		}
		
        #endregion
		
        #region "exporting grid data to third party visualisation programs"

        /// <summary>
        /// export the occupancy grids data to IFrIT basic particle file format for visualisation
        /// </summary>
        /// <param name="filename">name of the file to save as</param>
        public void ExportToIFrIT(string filename)
        {
			if (filename.Contains("."))
			{
				string prefix = "";
				string extension = "";
		        string[] str = filename.Split('.');
				if (str.Length > 1)
				{
					for (int i = 0; i < str.Length-1; i++) prefix += str[i];
					extension = str[str.Length-1];					
					
					for (int i = 0; i < grid.Length; i++)
						grid[i].ExportToIFrIT(prefix + i.ToString() + "." + extension);
				}
			}
        }
		
        #endregion
		
        #region "display"
		
        /// <summary>
        /// show an overhead view of the grid map as an image
        /// </summary>
		/// <param name="grid_index">index number of the grid to be shown</param>
        /// <param name="img">colour image data</param>
        /// <param name="width">width in pixels</param>
        /// <param name="height">height in pixels</param>
		/// <param name="show_all_occupied_cells">show all occupied pixels</param>
        public void Show(
		    int grid_index,
		    byte[] img, 
            int width, 
            int height,
		    bool show_all_occupied_cells)
        {
			switch (grid_type)
			{
			    case TYPE_SIMPLE:
			    {
				    occupancygridSimple grd = (occupancygridSimple)grid[grid_index];
				    grd.Show(img, width, height, show_all_occupied_cells);
			        break;
			    }
			    case TYPE_MULTI_HYPOTHESIS:
			    {
			        // TODO: get the best pose
				    particlePose pose = null;
				    occupancygridMultiHypothesis grd = (occupancygridMultiHypothesis)grid[grid_index];
				    grd.Show(
				         occupancygridMultiHypothesis.VIEW_ABOVE, 
                         img, width, height, pose, 
                         true, true);
				    break;
			    }
			}
		}		
		
        #endregion

    }
}
