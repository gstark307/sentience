/*
    Unit tests for grid cells
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
using System.Drawing;
using NUnit.Framework;
using sentience.core;
using sluggish.utilities;

namespace sentience.core.tests
{	
	[TestFixture()]
	public class tests_gridCells
	{
		[Test()]
		public void CreateMoireGrid()
		{
		    int no_of_poses = 200;
		    float sampling_radius_major_mm = 100;
		    float sampling_radius_minor_mm = 100;
		    float pan = 0;
		    float tilt = 0;
		    float roll = 0;
		    float max_orientation_variance = 5 * (float)Math.PI / 180;
		    float max_tilt_variance = 0 * (float)Math.PI / 180;
		    float max_roll_variance = 0 * (float)Math.PI / 180;
		    Random rnd = new Random(0);		    
		    
		    List<pos3D> cells = new List<pos3D>();

			int debug_img_width = 640;
			int debug_img_height = 480;
		    byte[] debug_img = new byte[debug_img_width * debug_img_height * 3];
		    byte[] debug_img_moire = new byte[debug_img_width * debug_img_height * 3];
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				
		    gridCells.CreateMoireGrid(
		        sampling_radius_major_mm,
		        sampling_radius_minor_mm,
		        no_of_poses,
		        pan, tilt, roll,
		        max_orientation_variance,
		        max_tilt_variance,
		        max_roll_variance,
		        rnd,
		        ref cells,
		        debug_img,
		        debug_img_moire,
		        debug_img_width,
		        debug_img_height);
		        		
		    Assert.Greater(cells.Count, no_of_poses * 90 / 100);
		    Assert.Less(cells.Count, no_of_poses * 110 / 100);

		    BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_gridCells_CreateMoireGrid.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

   		    BitmapArrayConversions.updatebitmap_unsafe(debug_img_moire, bmp);
			bmp.Save("tests_gridCells_CreateMoireGrid_moire.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
        }
	
		[Test()]
		public void ShowMoireGrid()
		{
			int debug_img_width = 640;
			int debug_img_height = 480;
		    byte[] debug_img = new byte[debug_img_width * debug_img_height * 3];
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            float first_grid_spacing = 1.0f;
		    float second_grid_spacing = 1.04f;
		    float first_grid_rotation_degrees = 0;
		    float second_grid_rotation_degrees = 10;
		    int dimension_x_cells = 40;
		    int dimension_y_cells = 40;
		    
		    float sampline_radius_major = 20;
		    float sampling_radius_minor = sampline_radius_major;
		    int radius = 3;
			
		    gridCells.ShowMoireGrid(
		        sampline_radius_major,
		        sampling_radius_minor,
		        first_grid_spacing,
		        second_grid_spacing,
		        first_grid_rotation_degrees,
		        second_grid_rotation_degrees,
		        dimension_x_cells,
		        dimension_y_cells,
		        1.0f,
		        debug_img,
		        debug_img_width,
		        debug_img_height);
		        			
			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_gridCells_ShowMoireGrid.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

		    sampline_radius_major = 100;
		    sampling_radius_minor = sampline_radius_major/2;
            first_grid_spacing = 1.0f;
		    second_grid_spacing = 1.2f;
		    first_grid_rotation_degrees = 0;
		    second_grid_rotation_degrees = 8;
		    dimension_x_cells = 100;
		    dimension_y_cells = 100;
		    float scaling_factor = 2;

   		    gridCells.ShowMoireGridVertices(
		        sampline_radius_major,
		        sampling_radius_minor,
		        first_grid_spacing,
		        second_grid_spacing,
		        first_grid_rotation_degrees,
		        second_grid_rotation_degrees,
		        dimension_x_cells,
		        dimension_y_cells,
		        scaling_factor,
		        debug_img,
		        debug_img_width,
		        debug_img_height,
		        radius);
		        
			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_gridCells_ShowMoireGrid_vertices.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
		}		
		
		[Test()]
		public void FindBestPose()
		{
			int debug_img_width = 640;
			int debug_img_height = 480;
		    byte[] debug_img_poses = new byte[debug_img_width * debug_img_height * 3];
		    byte[] debug_img_graph = new byte[debug_img_width * debug_img_height * 3];
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			
		    int no_of_poses = 400;
		    float sampling_radius_major_mm = 100;
		    float sampling_radius_minor_mm = 80;
		    float pan = 0;
		    float tilt = 0;
		    float roll = 0;
		    float max_orientation_variance = 5 * (float)Math.PI / 180.0f;
		    float max_tilt_variance = 0 * (float)Math.PI / 180.0f;
		    float max_roll_variance = 0 * (float)Math.PI / 180.0f;
		    Random rnd = new Random(81);
		    List<pos3D> poses = new List<pos3D>();
		    float ideal_best_pose_x = ((float)rnd.NextDouble()-0.5f) * sampling_radius_minor_mm * 2 * 0.8f;
		    float ideal_best_pose_y = ((float)rnd.NextDouble()-0.5f) * sampling_radius_major_mm * 2 * 0.8f;
		    float ideal_pan = ((float)rnd.NextDouble() - 0.5f) * (max_orientation_variance*2);
			
		    gridCells.CreateMoireGrid(
		        sampling_radius_major_mm,
		        sampling_radius_minor_mm,
		        no_of_poses,
		        pan, tilt, roll,
		        max_orientation_variance,
		        max_tilt_variance,
		        max_roll_variance,
		        rnd,
		        ref poses,
		        null, null,
		        0, 0);
			    
			const int steps = 10;
			
			for (int s = 0; s < steps; s++)
			{
  		        ideal_best_pose_x = sampling_radius_minor_mm * 0.3f;
		        ideal_best_pose_y = sampling_radius_major_mm * (-0.7f + (s*1.4f/steps));
		        ideal_pan = 0;
			    
				List<float> scores = new List<float>();
				for (int i = 0; i < poses.Count; i++)
				{
				    float score = (float)rnd.NextDouble();
				    scores.Add(score);
				    float dx = poses[i].x - ideal_best_pose_x;
				    float dy = poses[i].y - ideal_best_pose_y;
				    float dp = (poses[i].pan - ideal_pan) * sampling_radius_major_mm / max_orientation_variance;
				    float dist = (float)Math.Sqrt(dx*dx + dy*dy + dp*dp) * 0.01f;
				    scores[i] += 2.0f / (1.0f + dist*dist);
				}
				
				pos3D best_pose = null;
	            gridCells.FindBestPose(
	                poses,
	                scores,
	                ref best_pose,
	                sampling_radius_major_mm,
	                debug_img_poses,
	                debug_img_graph,
	                debug_img_width,
	                debug_img_height,
	                ideal_best_pose_x,
	                ideal_best_pose_y);
	                
	            float pan_angle_error = (ideal_pan - best_pose.pan) * 180 / (float)Math.PI;
	            Console.WriteLine("Target Pan angle: " + (ideal_pan * 180 / (float)Math.PI).ToString() + " degrees");
	            Console.WriteLine("Estimated Pan angle: " + (best_pose.pan * 180 / (float)Math.PI).ToString() + " degrees");
	            Console.WriteLine("Pan error: " + pan_angle_error.ToString() + " degrees");
	            
	            float position_error_x = Math.Abs(ideal_best_pose_x - best_pose.x);
	            float position_error_y = Math.Abs(ideal_best_pose_y - best_pose.y);
	            
	            Assert.Less(pan_angle_error, 1);
	            Assert.Less(position_error_x, sampling_radius_major_mm * 0.3f);
	            Assert.Less(position_error_y, sampling_radius_major_mm * 0.3f);
				
				BitmapArrayConversions.updatebitmap_unsafe(debug_img_poses, bmp);
				bmp.Save("tests_gridCells_FindBestPose" + s.ToString() + ".gif", System.Drawing.Imaging.ImageFormat.Gif);
				BitmapArrayConversions.updatebitmap_unsafe(debug_img_graph, bmp);
				bmp.Save("tests_gridCells_FindBestPose_graph" + s.ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
			}
		}
		
	}
}
