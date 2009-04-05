/*
    A buffer containing a number of metagrids
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
	public class metagridBuffer
	{
		// buffer containing grids
		public metagrid[] buffer;
		
		// index number of the currently active grid
		protected int current_buffer_index;
		
        #region "constructor"
		
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
        public metagridBuffer(
            int no_of_grids,
            int grid_type,
		    int dimension_mm, 
            int dimension_vertical_mm, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
        {
			// create the buffer
			buffer = new metagrid[2];
			for (int i = 0; i < 2; i++)
			{
				buffer[i] = 
					new metagrid(
				        no_of_grids,
				        grid_type,
				        dimension_mm,
				        dimension_vertical_mm,
				        cellSize_mm,
				        localisationRadius_mm,
				        maxMappingRange_mm,
				        vacancyWeighting);
			}
			current_buffer_index = 0;
		}
		
        #endregion
		
        #region "setting the centre position of the grid"
		
		/// <summary>
		/// sets the centre position for the given grid buffer
		/// </summary>
		/// <param name="buffer_index">index number of the buffer</param>
		/// <param name="centre_x_mm">x coordinate in millimetres</param>
		/// <param name="centre_y_mm">y coordinate in millimetres</param>
		/// <param name="centre_z_mm">z coordinate in millimetres</param>
		protected void SetPosition(
		    int buffer_index,
		    float centre_x_mm,
		    float centre_y_mm,
		    float centre_z_mm)
		{
			buffer[buffer_index].SetPosition(centre_x_mm, centre_y_mm, centre_z_mm, 0.0f);
		}

		/// <summary>
		/// sets the centre position for the current grid
		/// </summary>
		/// <param name="centre_x_mm">x coordinate in millimetres</param>
		/// <param name="centre_y_mm">y coordinate in millimetres</param>
		/// <param name="centre_z_mm">z coordinate in millimetres</param>
		public void SetPosition(
		    float centre_x_mm,
		    float centre_y_mm,
		    float centre_z_mm)
		{
			SetPosition(current_buffer_index, centre_x_mm, centre_y_mm, centre_z_mm);
		}

		/// <summary>
		/// sets the centre position for the next grid which will be entered
		/// </summary>
		/// <param name="centre_x_mm">x coordinate in millimetres</param>
		/// <param name="centre_y_mm">y coordinate in millimetres</param>
		/// <param name="centre_z_mm">z coordinate in millimetres</param>
		public void SetNextPosition(
		    float centre_x_mm,
		    float centre_y_mm,
		    float centre_z_mm)
		{
			SetPosition(1 - current_buffer_index, centre_x_mm, centre_y_mm, centre_z_mm);
		}
		
        #endregion
		
        #region "updating the grid"
		
		
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
        /// <param name="robot_pose">current estimated position and orientation of the robots centre of rotation</param>
        /// <param name="max_orientation_variance">maximum variance in orientation in radians, used to create sample poses</param>
        /// <param name="max_tilt_variance">maximum variance in tilt angle in radians, used to create sample poses</param>
        /// <param name="max_roll_variance">maximum variance in roll angle in radians, used to create sample poses</param>
        /// <param name="poses">list of poses tried</param>
        /// <param name="pose_score">list of pose matching scores</param>
        /// <param name="best_robot_pose">returned best pose estimate</param>
		/// <param name="rnd">random number generator</param>
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
            float max_roll_variance,
            List<pos3D> poses,
            List<float> pose_score,
		    Random rnd,
            ref pos3D best_robot_pose)
        {
			float matching_score = 
				buffer[current_buffer_index].Localise(
                    baseline_mm,
                    image_width,
                    image_height,
                    FOV_degrees,
		            stereo_features,
		            stereo_features_colour,
		            stereo_features_uncertainties,
                    sensormodel,
                    left_camera_location,
                    right_camera_location,
                    no_of_samples,
                    sampling_radius_major_mm,
                    sampling_radius_minor_mm,
                    robot_pose,
                    max_orientation_variance,
                    max_tilt_variance,
                    max_roll_variance,
                    poses,
                    pose_score,
		            rnd,
                    ref best_robot_pose);
			return(matching_score);
		}		
		
        #endregion
		
	}
}
