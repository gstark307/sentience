/*
    Unit tests for occupancy grids
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
using CenterSpace.Free;

namespace sentience.core.tests
{
	[TestFixture()]
	public class tests_occupancygrid_simple
	{
		[Test()]
		public void RaysIntersection()
		{
			int debug_img_width = 640;
			int debug_img_height = 480;
		    byte[] debug_img = new byte[debug_img_width * debug_img_height * 3];
			for (int i = (debug_img_width * debug_img_height * 3)-1; i >= 0; i--)
				debug_img[i] = 255;
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		    float max_y=0, max_x=0;
			float ray_uncertainty = 0.5f;
			
			for (int example = 0; example < 10; example++)
			{
				float disparity = 2;
			    float x1 = 640/2; 
				float x2 = x1 - disparity; 
				
				//disparity = 0.8f + (example * 0.5f);				
				x1 = (640/2) + (example * 1.5f); 
				x2 = x1 - disparity;
				
			    int grid_dimension = 2000; 				
	            float x_start = 0;
				float y_start = 0;
	            float x_end = 0;
				float y_end = 0;
	            float x_left = 0;
				float y_left = 0;
	            float x_right = 0;
				float y_right = 0;
				float focal_length = 5;
				float sensor_pixels_per_mm = 100;
				float baseline = 100;
				float distance = stereoModel.DisparityToDistance(disparity, focal_length, sensor_pixels_per_mm, baseline);
					
				stereoModel inverseSensorModel = new stereoModel();
				inverseSensorModel.image_width = 640;
				inverseSensorModel.image_height = 480;
	            inverseSensorModel.raysIntersection(
			        x1, x2, 
			        grid_dimension, ray_uncertainty,
			        distance,
	                ref x_start, ref y_start,
	                ref x_end, ref y_end,
	                ref x_left, ref y_left,
	                ref x_right, ref y_right);
				
				if (example == 0)
				{
				    max_y = -y_end * 1.2f;
				    max_x = x_start * 2;
				}
				
				x_start = x_start * debug_img_width / max_x;
				y_start = -y_start * debug_img_height / max_y;
				x_end = x_end * debug_img_width / max_x;
				y_end = -y_end * debug_img_height / max_y;
				x_left = x_left * debug_img_width / max_x;
				y_left = -y_left * debug_img_height / max_y;
				x_right = x_right * debug_img_width / max_x;
				y_right = -y_right * debug_img_height / max_y;		
				
				y_start = debug_img_height - 1 - y_start;
				y_end = debug_img_height - 1 - y_end;
				y_left = debug_img_height - 1 - y_left;
				y_right = debug_img_height - 1 - y_right;
				
				//Console.WriteLine("max: " + max.ToString());
							
				drawing.drawLine(debug_img, debug_img_width, debug_img_height, (int)x_start, (int)y_start, (int)x_left, (int)y_left, 0,0,0,0,false);
				drawing.drawLine(debug_img, debug_img_width, debug_img_height, (int)x_end, (int)y_end, (int)x_left, (int)y_left, 0,0,0,0,false);
				drawing.drawLine(debug_img, debug_img_width, debug_img_height, (int)x_end, (int)y_end, (int)x_right, (int)y_right, 0,0,0,0,false);
				drawing.drawLine(debug_img, debug_img_width, debug_img_height, (int)x_start, (int)y_start, (int)x_right, (int)y_right, 0,0,0,0,false);
			
			}
			
			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_occupancygrid_simple_RaysIntersection.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);			
		}
		
		[Test()]
		public void EvidenceRayRotation()
		{
			int debug_img_width = 640;
			int debug_img_height = 480;
		    byte[] debug_img = new byte[debug_img_width * debug_img_height * 3];
			for (int i = (debug_img_width * debug_img_height * 3)-1; i >= 0; i--)
				debug_img[i] = 255;
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			
			int cellSize_mm = 32;
		    int image_width = 320;
		    int image_height = 240;
			
			Console.WriteLine("Creating sensor models");			
			stereoModel inverseSensorModel = new stereoModel();
			inverseSensorModel.createLookupTable(cellSize_mm, image_width, image_height);
			
            // create a ray
			float FOV_horizontal = 78 * (float)Math.PI / 180.0f;
			inverseSensorModel.FOV_horizontal = FOV_horizontal;
			inverseSensorModel.FOV_vertical = FOV_horizontal * image_height / image_width;
            evidenceRay ray = 
			    inverseSensorModel.createRay(
				    image_width/2, image_height/2, 4, 
					0, 255, 255, 255);
			
			pos3D[] start_vertices = (pos3D[])ray.vertices.Clone();
			
			Console.WriteLine("x,y,z:  " + start_vertices[0].x.ToString() + ", " + start_vertices[0].y.ToString() + ", " + start_vertices[0].z.ToString());
			for (int i = 0; i <  ray.vertices.Length; i++)
			{
				int j = i + 1;
				if (j == ray.vertices.Length) j = 0;
				int x0 = (debug_img_width/2) + (int)ray.vertices[i].x/50;
				int y0 = (debug_img_height/2) + (int)ray.vertices[i].y/50;
				int x1 = (debug_img_width/2) + (int)ray.vertices[j].x/50;
				int y1 = (debug_img_height/2) + (int)ray.vertices[j].y/50;
				drawing.drawLine(debug_img, debug_img_width, debug_img_height, x0,y0,x1,y1,0,255,0,0,false);
			}
			
			float angle_degrees = 30;
			float angle_radians = angle_degrees / 180.0f * (float)Math.PI;
			pos3D rotation = new pos3D(0, 0, 0);
			rotation.pan = angle_degrees;
			ray.translateRotate(rotation);
			
			Console.WriteLine("x,y,z:  " + ray.vertices[0].x.ToString() + ", " + ray.vertices[0].y.ToString() + ", " + ray.vertices[0].z.ToString());
			for (int i = 0; i <  ray.vertices.Length; i++)
			{
				int j = i + 1;
				if (j == ray.vertices.Length) j = 0;
				int x0 = (debug_img_width/2) + (int)ray.vertices[i].x/50;
				int y0 = (debug_img_height/2) + (int)ray.vertices[i].y/50;
				int x1 = (debug_img_width/2) + (int)ray.vertices[j].x/50;
				int y1 = (debug_img_height/2) + (int)ray.vertices[j].y/50;
				drawing.drawLine(debug_img, debug_img_width, debug_img_height, x0,y0,x1,y1,255,0,0,0,false);
			}

			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_occupancygrid_simple_EvidenceRayRotation.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);			
		}
		
		[Test()]
		public void InsertRays()
		{
		    int image_width = 640;
		    int image_height = 480;
			int no_of_stereo_cameras = 1;
		    int localisationRadius_mm = 16000;
		    int maxMappingRange_mm = 16000;
		    int cellSize_mm = 32;
		    int dimension_cells = 16000 / cellSize_mm;
		    int dimension_cells_vertical = dimension_cells/2;
		    float vacancyWeighting = 0; //2.0f;
			float FOV_horizontal = 78 * (float)Math.PI / 180.0f;
					    
			// create a grid
			Console.WriteLine("Creating grid");
		    occupancygridSimple grid = 
		        new occupancygridSimple(
		            dimension_cells,
		            dimension_cells_vertical,
		            cellSize_mm,
		            localisationRadius_mm,
		            maxMappingRange_mm,
		            vacancyWeighting);
		    
		    Assert.AreNotEqual(grid, null, "object occupancygridSimple was not created");
			
			Console.WriteLine("Creating sensor models");			
			stereoModel inverseSensorModel = new stereoModel();
			inverseSensorModel.FOV_horizontal = FOV_horizontal;
			inverseSensorModel.FOV_vertical = FOV_horizontal * image_height / image_width;			
			inverseSensorModel.createLookupTable(cellSize_mm, image_width, image_height);

            //Assert.AreNotEqual(0, inverseSensorModel.ray_model.probability[1][5], "Ray model probabilities not updated");
						
			// observer parameters
		    pos3D observer = new pos3D(0,0,0);
		    float stereo_camera_baseline_mm = 100;
			pos3D left_camera_location = new pos3D(stereo_camera_baseline_mm*0.5f,0,0);
			pos3D right_camera_location = new pos3D(-stereo_camera_baseline_mm*0.5f,0,0);
		    float FOV_degrees = 78;
			int no_of_stereo_features = 2000;
		    float[] stereo_features = new float[no_of_stereo_features * 3];
		    byte[,] stereo_features_colour = new byte[no_of_stereo_features, 3];
		    float[] stereo_features_uncertainties = new float[no_of_stereo_features];
			
			// create some stereo disparities within the field of view
			Console.WriteLine("Adding disparities");
			MersenneTwister rnd = new MersenneTwister(0);
			for (int correspondence = 0; correspondence < no_of_stereo_features; correspondence++)
			{
				float x = rnd.Next(image_width-1);
				float y = rnd.Next(image_height/50) + (image_height/2);
				float disparity = 7;
				if ((x < image_width/5) || (x > image_width * 4/5))
				{
					disparity = 7; //15;
				}
				byte colour_red = (byte)rnd.Next(255);
				byte colour_green = (byte)rnd.Next(255);
				byte colour_blue = (byte)rnd.Next(255);
				
				stereo_features[correspondence*3] = x;
				stereo_features[(correspondence*3)+1] = y;
				stereo_features[(correspondence*3)+2] = disparity;
				stereo_features_colour[correspondence, 0] = colour_red;
				stereo_features_colour[correspondence, 1] = colour_green;
				stereo_features_colour[correspondence, 2] = colour_blue;
				stereo_features_uncertainties[correspondence] = 0;
			}
			
            // create an observation as a set of rays from the stereo correspondence results
            List<evidenceRay>[] stereo_rays = new List<evidenceRay>[no_of_stereo_cameras];
            for (int cam = 0; cam < no_of_stereo_cameras; cam++)
			{
				Console.WriteLine("Creating rays");
                stereo_rays[cam] = 
					inverseSensorModel.createObservation(
					    observer,
		                stereo_camera_baseline_mm,
		                image_width,
		                image_height,
		                FOV_degrees,
		                stereo_features,
		                stereo_features_colour,
		                stereo_features_uncertainties);

				// insert rays into the grid
				Console.WriteLine("Throwing rays");
				for (int ray = 0; ray < stereo_rays[cam].Count; ray++)
				{
					grid.Insert(stereo_rays[cam][ray], inverseSensorModel.ray_model, left_camera_location, right_camera_location, false);
				}
			}
					
			// save the result as an image
			Console.WriteLine("Saving grid");
			int debug_img_width = 640;
			int debug_img_height = 480;
		    byte[] debug_img = new byte[debug_img_width * debug_img_height * 3];
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			grid.Show(debug_img, debug_img_width, debug_img_height, false);
			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_occupancygrid_simple_InsertRays_overhead.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

			grid.ShowFront(debug_img, debug_img_width, debug_img_height, true);
			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_occupancygrid_simple_InsertRays_front.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
		}	
	
		[Test()]
		public void GridCreation()
		{
		    int dimension_cells = 50;
		    int dimension_cells_vertical = 30;
		    int cellSize_mm = 50;
		    int localisationRadius_mm = 1000;
		    int maxMappingRange_mm = 5000;
		    float vacancyWeighting = 0;
		    
		    occupancygridSimple grid = 
		        new occupancygridSimple(
		            dimension_cells,
		            dimension_cells_vertical,
		            cellSize_mm,
		            localisationRadius_mm,
		            maxMappingRange_mm,
		            vacancyWeighting);
		    
		    Assert.AreNotEqual(grid, null, "object occupancygridSimple was not created");
		
		}
	}
	
	[TestFixture()]
	public class tests_occupancygrid_multi_hypothesis
	{		
		[Test()]
		public void GridCreation()
		{
		    int dimension_cells = 50;
		    int dimension_cells_vertical = 30;
		    int cellSize_mm = 50;
		    int localisationRadius_mm = 1000;
		    int maxMappingRange_mm = 5000;
		    float vacancyWeighting = 0;
		    
		    occupancygridMultiHypothesis grid = 
		        new occupancygridMultiHypothesis(dimension_cells,
		                                         dimension_cells_vertical,
		                                         cellSize_mm,
		                                         localisationRadius_mm,
		                                         maxMappingRange_mm,
		                                         vacancyWeighting);
		    
		    Assert.AreNotEqual(grid, null, "object occupancygridMultiHypothesis was not created");
		}
	}
}
