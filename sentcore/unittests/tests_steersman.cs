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
		public void LocaliseAlongPath()
		{
            // systematic bias
            float bias_x_mm = -200;
            float bias_y_mm = 0;
			
			// number of stereo features per step during mapping
			int no_of_mapping_stereo_features = 300;
			
			// number of stereo features observed per localisation step
			int no_of_localisation_stereo_features = 50;

		    string filename = "steersman_localise_along_path.dat";
		    float path_length_mm = 10000;
		    float start_orientation = 0;
		    float end_orientation = 0; //90 * (float)Math.PI / 180.0f;
		    float distance_between_poses_mm = 100;
            float disparity = 15;
			
			string steersman_filename = "tests_steersman_LocaliseAlongPath.xml";
		    int body_width_mm = 500;
		    int body_length_mm = 500;
		    int body_height_mm= 500;
		    int centre_of_rotation_x = body_width_mm/2;
		    int centre_of_rotation_y = body_length_mm/2;
		    int centre_of_rotation_z = 10;
		    int head_centroid_x = body_width_mm/2;
		    int head_centroid_y = body_length_mm/2;
		    int head_centroid_z = 10;
		    string sensormodels_filename = "";
		    int no_of_stereo_cameras = 2;
		    float baseline_mm = 100;
		    int image_width = 320;
		    int image_height = 240;
		    float FOV_degrees = 78;
		    float head_diameter_mm = 100;
		    float default_head_orientation_degrees = 0;
            int no_of_grid_levels = 1;
		    int dimension_mm = 8000;
            int dimension_vertical_mm = 2000; 
            int cellSize_mm = 50;
						
			string[] str = filename.Split('.');
				
			List<OdometryData> path = null;
            tests_metagridbuffer.SavePath(
		        filename,
		        path_length_mm,
		        start_orientation,
		        end_orientation,
		        distance_between_poses_mm,
                disparity,
			    no_of_mapping_stereo_features,
			    no_of_stereo_cameras,
			    ref path);
			
			Assert.AreEqual(true, File.Exists(filename));
			Assert.AreEqual(true, File.Exists(str[0] + "_disparities_index.dat"));
			Assert.AreEqual(true, File.Exists(str[0] + "_disparities.dat"));
						
			steersman visual_guidance = null;
			if (File.Exists(steersman_filename))
			{
				visual_guidance = new steersman();
				visual_guidance.Load(steersman_filename);
			}
			else
			{
				visual_guidance = new steersman(
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
				
			    visual_guidance.Save(steersman_filename);
			}
			
            visual_guidance.LoadPath(filename, str[0] + "_disparities_index.dat", str[0] + "_disparities.dat");
            
			visual_guidance.ShowLocalisations("steersman_localise_along_path.jpg", 640, 480);
			
		    Random rnd = new Random(0);
            pos3D pose_offset = null;
            bool buffer_transition = false;

		    float[][] stereo_features = new float[no_of_stereo_cameras][];
		    byte[][,] stereo_features_colour = new byte[no_of_stereo_cameras][,];
		    float[][] stereo_features_uncertainties = new float[no_of_stereo_cameras][];
			for (int i = 0; i < no_of_stereo_cameras; i++)
			{
			    stereo_features_uncertainties[i] = new float[no_of_localisation_stereo_features];
			    for (int j = 0; j < no_of_localisation_stereo_features; j++)
				    stereo_features_uncertainties[i][j] = 1;
			}

            float average_offset_x_mm = 0;
            float average_offset_y_mm = 0;
			List<OdometryData> estimated_path = new List<OdometryData>();
			
			int no_of_localisation_failures = 0;

			DateTime start_time = DateTime.Now;
			int no_of_localisations = 0;
			for (int i = 0; i < path.Count-1; i += 5, no_of_localisations++)
			{
                string debug_mapping_filename = "steersman_localise_along_path_map_" + i.ToString() + ".jpg";

				OdometryData p0 = path[i];
				OdometryData p1 = path[i + 1];
				
				float current_x_mm = p0.x + ((p1.x - p0.x)/2) + bias_x_mm; 
				float current_y_mm = p0.y + ((p1.y - p0.y)/2) + bias_y_mm;
				float current_pan = p0.orientation + ((p1.orientation - p0.orientation)/2);
				
				// create an intermediate pose
				for (int cam = 0; cam < no_of_stereo_cameras; cam++)
				{
					// set the current pose
					visual_guidance.SetCurrentPosition(
					    cam,
				        current_x_mm,
				        current_y_mm,
				        0,
				        current_pan,
					    0, 0);
								
					// create stereo features				
					int ctr = 0;
					stereo_features[cam] = new float[no_of_localisation_stereo_features * 3];
					stereo_features_colour[cam] = new byte[no_of_localisation_stereo_features, 3];
					for (int f = 0; f < no_of_localisation_stereo_features; f += 5)
					{
						if (f < no_of_localisation_stereo_features/2)
						{
							stereo_features[cam][ctr++] = 20;
							stereo_features[cam][ctr++] = rnd.Next(239);
						}
						else
						{
							stereo_features[cam][ctr++] = image_width - 20;
							stereo_features[cam][ctr++] = rnd.Next(239);
						}
					    stereo_features[cam][ctr++] = disparity;
					}
				}
				
				float offset_x_mm = 0;
				float offset_y_mm = 0;
				float offset_z_mm = 0;
				float offset_pan = 0;
				float offset_tilt = 0;
				float offset_roll = 0;
				
				bool valid_localisation = visual_guidance.Localise(
				    stereo_features,
				    stereo_features_colour,
				    stereo_features_uncertainties,
				    debug_mapping_filename,
				    bias_x_mm, bias_y_mm,
				    ref offset_x_mm,
				    ref offset_y_mm,
				    ref offset_z_mm,
				    ref offset_pan,
				    ref offset_tilt,
				    ref offset_roll);
				
				if (valid_localisation)
				{				
					Console.WriteLine("pose_offset (mm): " + offset_x_mm.ToString() + ", " + offset_y_mm.ToString() + ", " + offset_pan.ToString());
					OdometryData estimated_pose = new OdometryData();
					estimated_pose.x = current_x_mm + offset_x_mm;
					estimated_pose.y = current_y_mm + offset_y_mm;
					estimated_pose.orientation = current_pan + offset_pan;
					estimated_path.Add(estimated_pose);
	                average_offset_x_mm += offset_x_mm;
	                average_offset_y_mm += offset_y_mm;
				}
				else
				{
					// fail!
					no_of_localisation_failures++;
					Console.WriteLine("Localisation failure");
				}
			}
			
			TimeSpan diff = DateTime.Now.Subtract(start_time);
			float time_per_localisation_sec = (float)diff.TotalSeconds / no_of_localisations;
			Console.WriteLine("Time per localisation: " + time_per_localisation_sec.ToString() + " sec");

            visual_guidance.ShowLocalisations("steersman_localisations_along_path.jpg", 640, 480);

            average_offset_x_mm /= estimated_path.Count;
            average_offset_y_mm /= estimated_path.Count;
            Console.WriteLine("Average offsets: " + average_offset_x_mm.ToString() + ", " + average_offset_y_mm.ToString());

            float diff_x_mm = Math.Abs(average_offset_x_mm - bias_x_mm);
            float diff_y_mm = Math.Abs(average_offset_y_mm - bias_y_mm);
            Assert.Less(diff_x_mm, cellSize_mm*3/2, "x bias not detected");
            Assert.Less(diff_y_mm, cellSize_mm*3/2, "y bias not detected");	
			
			if (no_of_localisation_failures > 0)
				Console.WriteLine("Localisation failures: " + no_of_localisation_failures.ToString());
			else
				Console.WriteLine("No localisation failures!");
			Assert.Less(no_of_localisation_failures, 4, "Too many localisation failures");
        }
		
		
		
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
		    string sensormodels_filename = "";
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
			
			if (File.Exists(filename))
			{
				visual_guidance1 = new steersman();
				visual_guidance1.Load(filename);
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
