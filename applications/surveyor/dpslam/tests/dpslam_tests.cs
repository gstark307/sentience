
using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Drawing;
using dpslam.core;

namespace dpslam.core.tests
{		
	[TestFixture()]
	public class dpslam_tests
	{
		private static dpslam CreateSimulation(
		    int map_dim, 
		    byte[] map, 
		    int cellSize_mm)
		{
            int dimension_cells = 100;
            int dimension_cells_vertical = 20;
            int localisationRadius_mm = 2000;
            int maxMappingRange_mm = 2000; 
            float vacancyWeighting = 0.8f;
			
			dpslam sim = new dpslam(
			    dimension_cells, 
			    dimension_cells_vertical, 
			    cellSize_mm, 
			    localisationRadius_mm, 
			    maxMappingRange_mm, 
			    vacancyWeighting);
			Assert.IsNotNull(sim);
			
			int world_tx = -cellSize_mm * dimension_cells / 2;
			int world_ty = -cellSize_mm * dimension_cells / 2;
			//int world_bx = cellSize_mm * dimension_cells / 2;
			//int world_by = cellSize_mm * dimension_cells / 2;
						
			for (int y = 0; y < map_dim; y++)
			{
				int start_x = 0;
				bool wall = false;
			    for (int x = 0; x < map_dim; x++)
			    {
					if ((!wall) && (map[y*map_dim+x] == 1) && (map[y*map_dim+x+1] == 1))
					{
						start_x = x;
						wall = true;
					}
					if ((wall) && (map[y*map_dim+x] == 0))
					{
						int tx = (int)(world_tx + ((start_x + 0.5f)*dimension_cells/map_dim*cellSize_mm));
						int ty = (int)(world_ty + ((y + 0.5f)*dimension_cells/map_dim*cellSize_mm));
						int bx = (int)(world_tx + ((x - 0.5f)*dimension_cells/map_dim*cellSize_mm));
						int by = ty;
						sim.InsertWall(tx,ty,bx,by,(int)cellSize_mm*10, (int)cellSize_mm, 0.2f, 0,0,0);
						wall = false;
					}
				}
			}
			
			for (int x = 0; x < map_dim; x++)
			{
				int start_y = 0;
				bool wall = false;
			    for (int y = 0; y < map_dim; y++)
			    {
					if ((!wall) && (map[y*map_dim+x] == 1) && (map[(y+1)*map_dim+x] == 1))
					{
						start_y = y;
						wall = true;
					}
					if ((wall) && (map[y*map_dim+x] == 0))
					{
						int tx = (int)(world_tx + ((x + 0.5f)*dimension_cells/map_dim*cellSize_mm));
						int ty = (int)(world_ty + ((start_y + 0.5f)*dimension_cells/map_dim*cellSize_mm));
						int bx = tx;
						int by = (int)(world_ty + ((y - 0.5f)*dimension_cells/map_dim*cellSize_mm));
						sim.InsertWall(tx,ty,bx,by,cellSize_mm*10, cellSize_mm, 0.2f, 0,0,0);
						wall = false;
					}
				}
			}			
			return(sim);
		}
		
		[Test()]
		public void ProbeView()
		{		
			int map_dim = 14;
			byte[] map = {
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,				
				0,0,0,0,1,1,1,1,0,0,0,0,0,0,
				0,0,0,0,1,0,0,1,0,0,0,0,0,0,
				0,0,0,0,1,0,0,1,0,0,0,0,0,0,
				0,0,0,0,1,0,0,1,1,1,1,0,0,0,
				0,0,0,0,1,0,0,0,0,0,1,0,0,0,
				0,0,0,0,1,0,0,0,0,0,1,0,0,0,
				0,0,0,0,1,0,0,0,0,0,1,0,0,0,
				0,0,0,0,1,1,1,0,1,1,1,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,			
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0
			};
			dpslam sim = CreateSimulation(map_dim, map, 50);
			
			int image_width = 320;
			int image_height = 240;
			float FOV_degrees = 90;
			float max_range_mm = 2000;
			pos3D camPose = new pos3D(0,0,0);
			
            float x0_mm = 0;
		    float y0_mm = 0;
		    float z0_mm = 100;
	        float x1_mm = 0;
		    float y1_mm = 1500;
		    float z1_mm = -300;
		    bool use_ground_plane=true;
			float range = sim.ProbeRange(
		        null,
                x0_mm, y0_mm, z0_mm,
	            x1_mm, y1_mm, z1_mm,
		        use_ground_plane);
			Assert.IsTrue(range > -1, "Ground plane was not detected");
			Assert.IsTrue(range > 550);
			Assert.IsTrue(range < 650);
			
			int step_size = 10;
			float[] range_buffer = new float[(image_width/step_size)*(image_height/step_size)];
			sim.ProbeView(
			    null, 
			    camPose.x, camPose.y, camPose.z, 
			    camPose.pan, camPose.tilt, camPose.roll, 
			    FOV_degrees, 
			    image_width, image_height, 
			    step_size, 
			    max_range_mm, false, 
			    range_buffer);
			
			int ctr=0;
			for (int i = 0; i < range_buffer.Length; i++)
			{
				if (range_buffer[i] > -1) ctr++;
				//Console.WriteLine("Range: " + range_buffer[i].ToString());
			}
			Assert.IsTrue(ctr > 0, "No objects were ranged within the simulation");
		}
		
		[Test()]
		public void PointLineDistance()			
		{
			float x0 = 382.5575f;
			float y0 = 705.5103f;
			float x1 = 389.1954f;
			float y1 = 712.9895f;
			float px = 393.5956f;
			float py = 696.0023f;
			
			float dist = geometry.circleDistanceFromLine(x0,y0,x1,y1,px,py,0.01f);
			Assert.IsTrue(dist < 20, "dist out of range = " + dist.ToString());
		}
		
		//[Test()]
		public static void CreateSim()
		{		
            int dimension_cells = 100;
            int dimension_cells_vertical = 20;
            int cellSize_mm = 50;
            int localisationRadius_mm = 2000;
            int maxMappingRange_mm = 2000; 
            float vacancyWeighting = 0.8f;
			
			int map_dim = 14;
			byte[] map = {
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,				
				0,0,0,0,1,1,1,1,0,0,0,0,0,0,
				0,0,0,0,1,0,0,1,0,0,0,0,0,0,
				0,0,0,0,1,0,0,1,0,0,0,0,0,0,
				0,0,0,0,1,0,0,1,1,1,1,0,0,0,
				0,0,0,0,1,0,0,0,0,0,1,0,0,0,
				0,0,0,0,1,0,0,0,0,0,1,0,0,0,
				0,0,0,0,1,0,0,0,0,0,1,0,0,0,
				0,0,0,0,1,1,1,0,1,1,1,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,			
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0
			};
			dpslam sim = CreateSimulation(map_dim, map, 50);
			
			particlePose pose = null;
			int img_width = 640;
			byte[] img = new byte[img_width * img_width * 3];
			sim.Show(0, img, img_width, img_width, pose, true, true);
			Bitmap bmp = new Bitmap(img_width, img_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
			bmp.Save("dpslam_tests_CreateSimulation1.bmp", System.Drawing.Imaging.ImageFormat.Bmp);			
			
			robot rob = new robot(1);
			rob.WheelBase_mm = 90;
			rob.WheelDiameter_mm = 30;
			rob.x = 0;
			rob.y = 0;
			rob.pan = 90 * (float)Math.PI / 180.0f;
			rob.head.cameraFOVdegrees[0] = 90;
			rob.head.cameraSensorSizeMm[0] = 4.17f;
			rob.head.cameraFocalLengthMm[0] = 3.6f;
			rob.head.cameraImageWidth[0] = 320;
			rob.head.cameraImageHeight[0] = 240;

			rayModelLookup sensor_model = new rayModelLookup(10, 10);
			sensor_model.InitSurveyorSVS();
			
			rob.head.sensormodel[0] = sensor_model;
			
			float time_elapsed_sec = 1;
			float forward_velocity = 30;
			float angular_velocity_pan = 0;
			float angular_velocity_tilt = 0;
			float angular_velocity_roll = 0;
			for (float t = 0; t < 4; t += time_elapsed_sec)
			{
  			    rob.updateFromVelocities(sim, forward_velocity, angular_velocity_pan, angular_velocity_tilt, angular_velocity_roll, time_elapsed_sec);
				Console.WriteLine("xy: " + rob.x.ToString() + " " + rob.y.ToString());
			}
			rob.SaveGridImage("dpslam_tests_CreateSimulation2.bmp");
		}
		
	}
}
