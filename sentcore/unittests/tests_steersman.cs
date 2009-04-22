/*
    Unit tests for steersman class
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
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using sentience.core;
using sluggish.utilities;

namespace sentience.core.tests
{
	[TestFixture()]
	public class tests_steersman
	{
		
		[Test()]
		public void SaveAndLoad()
		{
			string filename = "tests_steersman_SaveAndLoad.xml";
			
		    int body_width_mm = 1000;
		    int body_length_mm = 900;
		    int body_height_mm = 800;
		    int centre_of_rotation_x = body_width_mm/2;
		    int centre_of_rotation_y = body_length_mm/2;
		    int centre_of_rotation_z = 10;
		    int head_centroid_x = body_width_mm/2;
		    int head_centroid_y = body_length_mm/2;
		    int head_centroid_z = 1200;
		    string sensormodels_filename = "tests_steersman_SaveAndLoad.xml";
            int no_of_grid_levels = 2;
		    int dimension_mm = 8000; 
            int dimension_vertical_mm = 2000; 
            int cellSize_mm = 50;
			int no_of_stereo_cameras = 2;
			float baseline_mm = 100;
			int image_width = 320;
			int image_height = 240;
			float FOV_degrees = 78;
			float head_diameter_mm = 100;
			float default_head_orientation_degrees = 0;
			
			steersman visual_guidance1 = null;
			
			if (File.Exists(sensormodels_filename))
			{
				visual_guidance1 = new steersman();
				visual_guidance1.Load(sensormodels_filename);
			}
			else
			{			
			    visual_guidance1 = new steersman(
		            body_width_mm,
		            body_length_mm,
		            body_height_mm,
		            centre_of_rotation_x,
		            centre_of_rotation_y,
		            centre_of_rotation_z,
		            head_centroid_x,
		            head_centroid_y,
		            head_centroid_z,
		            sensormodels_filename,
		            no_of_stereo_cameras,
		            baseline_mm,
		            image_width,
		            image_height,
		            FOV_degrees,
		            head_diameter_mm,
		            default_head_orientation_degrees,
                    no_of_grid_levels,
		            dimension_mm, 
                    dimension_vertical_mm, 
                    cellSize_mm);		                 
			}
			visual_guidance1.Save(filename);
			
			Assert.AreEqual(File.Exists(filename), true);
			
			steersman visual_guidance2 = new steersman();
			visual_guidance2.Load(filename);
			
			Assert.AreEqual(visual_guidance1.buffer.no_of_grid_levels, visual_guidance2.buffer.no_of_grid_levels);
			Assert.AreEqual(visual_guidance1.buffer.grid_type, visual_guidance2.buffer.grid_type);
		    Assert.AreEqual(visual_guidance1.buffer.dimension_mm, visual_guidance2.buffer.dimension_mm);
			Assert.AreEqual(visual_guidance1.buffer.dimension_vertical_mm, visual_guidance2.buffer.dimension_vertical_mm);
			Assert.AreEqual(visual_guidance1.buffer.cellSize_mm, visual_guidance2.buffer.cellSize_mm);
			Assert.AreEqual(visual_guidance1.buffer.localisationRadius_mm, visual_guidance2.buffer.localisationRadius_mm);
			Assert.AreEqual(visual_guidance1.buffer.maxMappingRange_mm, visual_guidance2.buffer.maxMappingRange_mm);
			Assert.AreEqual(visual_guidance1.buffer.vacancyWeighting, visual_guidance2.buffer.vacancyWeighting);
						
			Assert.AreEqual(visual_guidance1.robot_geometry.body_width_mm, visual_guidance2.robot_geometry.body_width_mm);
			Assert.AreEqual(visual_guidance1.robot_geometry.body_length_mm, visual_guidance2.robot_geometry.body_length_mm);
			Assert.AreEqual(visual_guidance1.robot_geometry.body_height_mm, visual_guidance2.robot_geometry.body_height_mm);
			
			Assert.AreEqual(visual_guidance1.robot_geometry.body_centre_of_rotation_x, visual_guidance2.robot_geometry.body_centre_of_rotation_x);
			Assert.AreEqual(visual_guidance1.robot_geometry.body_centre_of_rotation_y, visual_guidance2.robot_geometry.body_centre_of_rotation_y);
			Assert.AreEqual(visual_guidance1.robot_geometry.body_centre_of_rotation_z, visual_guidance2.robot_geometry.body_centre_of_rotation_z);
			
			Assert.AreEqual(visual_guidance1.robot_geometry.head_centroid_x, visual_guidance2.robot_geometry.head_centroid_x);
			Assert.AreEqual(visual_guidance1.robot_geometry.head_centroid_y, visual_guidance2.robot_geometry.head_centroid_y);
            Assert.AreEqual(visual_guidance1.robot_geometry.head_centroid_z, visual_guidance2.robot_geometry.head_centroid_z);
						
			for (int cam = 0; cam < no_of_stereo_cameras; cam++)
			{
				Assert.AreEqual(visual_guidance1.robot_geometry.baseline_mm[cam], visual_guidance2.robot_geometry.baseline_mm[cam]);
				Assert.AreEqual(visual_guidance1.robot_geometry.image_width[cam], visual_guidance2.robot_geometry.image_width[cam]);
				Assert.AreEqual(visual_guidance1.robot_geometry.image_height[cam], visual_guidance2.robot_geometry.image_height[cam]);
				Assert.AreEqual(visual_guidance1.robot_geometry.FOV_degrees[cam], visual_guidance2.robot_geometry.FOV_degrees[cam]);
				Assert.AreEqual(visual_guidance1.robot_geometry.stereo_camera_position_x[cam], visual_guidance2.robot_geometry.stereo_camera_position_x[cam]);
				Assert.AreEqual(visual_guidance1.robot_geometry.stereo_camera_position_y[cam], visual_guidance2.robot_geometry.stereo_camera_position_y[cam]);
				Assert.AreEqual(visual_guidance1.robot_geometry.stereo_camera_position_z[cam], visual_guidance2.robot_geometry.stereo_camera_position_z[cam]);
                Assert.AreEqual(visual_guidance1.robot_geometry.stereo_camera_pan[cam], visual_guidance2.robot_geometry.stereo_camera_pan[cam]);
                Assert.AreEqual(visual_guidance1.robot_geometry.stereo_camera_tilt[cam], visual_guidance2.robot_geometry.stereo_camera_tilt[cam]);
                Assert.AreEqual(visual_guidance1.robot_geometry.stereo_camera_roll[cam], visual_guidance2.robot_geometry.stereo_camera_roll[cam]);
			}
			
		}
	}
}
