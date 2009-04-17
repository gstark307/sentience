/*
    robot geometry
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
	public class robotGeometry
	{
		// bounding box for the robot's body
	    public float body_width_mm;
	    public float body_length_mm;
	    public float body_height_mm;
		
		// centre of rotation, relative to the top left corner of the body's bounding box
	    public float body_centre_of_rotation_x;
	    public float body_centre_of_rotation_y;
	    public float body_centre_of_rotation_z;
		
		// head centroid relative to the top left corner of the body's bounding box
	    public float head_centroid_x;
	    public float head_centroid_y;
	    public float head_centroid_z;
		
		// head orientation in radians
	    public float head_pan;
	    public float head_tilt;
	    public float head_roll;
		
		// stereo camera baseline
	    public float[] baseline_mm;
		
		// stereo camera position relative to the head centroid
	    public float[] stereo_camera_position_x;
	    public float[] stereo_camera_position_y;
	    public float[] stereo_camera_position_z;
		
		// stereo camera orientation relative to the head
	    public float[] stereo_camera_pan;
	    public float[] stereo_camera_tilt;
	    public float[] stereo_camera_roll;
		
		// image dimensions for each stereo camera
        public int[] image_width;
        public int[] image_height;
		
		// field of view for each stereo camera in degrees
        public float[] FOV_degrees;
		
		// sensor models for each stereo camera
        public stereoModel[][] sensormodel;
		
		// current positions of the left and right cameras on each stereo camera
        public pos3D[] left_camera_location;
        public pos3D[] right_camera_location;
		
		// current estimated pose
        public pos3D pose;
				
		// this defines dimensions of the pose uncertainty ellipse
        public float sampling_radius_major_mm;
        public float sampling_radius_minor_mm;

		// maximum orientation variance within the pose list
        public float max_orientation_variance;
        public float max_tilt_variance;
        public float max_roll_variance;
		
		// pose list
        public int no_of_sample_poses;
        public List<pos3D> poses;
		
		// probability for each pose in the list
        public List<float> pose_probability;
		
		public robotGeometry()
		{
	        no_of_sample_poses = 300;
	        sampling_radius_major_mm = 300;
	        sampling_radius_minor_mm = 300;
	        max_orientation_variance = 5 * (float)Math.PI / 180.0f;
	        max_tilt_variance = 0;
	        max_roll_variance = 0;
			poses = new List<pos3D>();
			pose_probability = new List<float>();			
		}
		
		public void CreateSensorModels(
		    metagridBuffer buf)
		{
			List<int> cell_sizes = buf.GetCellSizes();
			
			sensormodel = new stereoModel[image_width.Length][];
			for (int stereo_cam = 0; stereo_cam < image_width.Length; stereo_cam++)
				sensormodel[stereo_cam] = new stereoModel[cell_sizes.Count];
			
			for (int stereo_cam = 0; stereo_cam < image_width.Length; stereo_cam++)
			{
				for (int grid_level = 0; grid_level < cell_sizes.Count; grid_level++)
				{
					if (stereo_cam > 0)
					{
						if (image_width[stereo_cam - 1] == 
						    image_width[stereo_cam])
						{
							sensormodel[stereo_cam][grid_level] = sensormodel[stereo_cam-1][grid_level];
						}
					}
					
					if (sensormodel[stereo_cam][grid_level] == null)
					{
					    sensormodel[stereo_cam][grid_level] = new stereoModel();
					    sensormodel[stereo_cam][grid_level].createLookupTable(
						    cell_sizes[grid_level], 
						    image_width[stereo_cam], 
						    image_height[stereo_cam]);
					}
				}
			}
		}
	}
}
