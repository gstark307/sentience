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
		public void InsertRays()
		{
			int no_of_stereo_cameras = 1;
		    int localisationRadius_mm = 1000;
		    int maxMappingRange_mm = 5000;
		    int cellSize_mm = 32;
		    int dimension_cells = 8000 / cellSize_mm;
		    int dimension_cells_vertical = dimension_cells/2;
		    float vacancyWeighting = 0.2f;
					    
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

		    int image_width = 320;
		    int image_height = 240;
			
			Console.WriteLine("Creating sensor models");			
			stereoModel inverseSensorModel = new stereoModel();
			inverseSensorModel.createLookupTable(cellSize_mm, image_width, image_height);
			
			// observer parameters
		    pos3D observer = new pos3D(0,0,0);
		    float stereo_camera_baseline_mm = 100;
			pos3D left_camera_location = new pos3D(-stereo_camera_baseline_mm*0.5f,0,0);
			pos3D right_camera_location = new pos3D(stereo_camera_baseline_mm*0.5f,0,0);
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
				float y = rnd.Next(image_height-1);
				float disparity = 0.5f + (rnd.Next(10)/2.0f);
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
			grid.Show(debug_img, debug_img_width, debug_img_height);
			Bitmap bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			BitmapArrayConversions.updatebitmap_unsafe(debug_img, bmp);
			bmp.Save("tests_occupancygrid_simple_InsertRays.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
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
