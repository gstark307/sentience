/*
    Unit tests for metagrids
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
using Aced.Compression;

namespace sentience.core.tests
{	
	[TestFixture()]
	public class tests_metagridbuffer
	{
        #region "saving a test path"
		
		internal class StereoFeatureTest
	    {
	        public float x, y;
	        public float disparity;
			public byte[] colour;
	        
	        public StereoFeatureTest(float x, float y, float disparity)
	        {
	            this.x = x;
	            this.y = y;
	            this.disparity = disparity;
	        }
	
	        public void SetColour(byte r, byte g, byte b)
	        {
	            colour = new byte[3];
	            colour[0] = r;
	            colour[1] = g;
	            colour[2] = b;
	        }
	    }		
		
		/// <summary>
		/// process the full pose information
		/// </summary>
		/// <param name="t">
		/// </param>
		/// <param name="path_identifier">name of the path</param>
		/// <param name="x">x coordinate of the robot in millimetres</param>
		/// <param name="y">y coordinate of the robot in millimetres</param>
		/// <param name="orientation">orientation of the robot in radians</param>
		/// <param name="head_pan">orientation of the robot's head relative to the body in radians</param>
		/// <param name="head_tilt">tilt of the robot's head relative to the body in radians</param>
		/// <param name="head_roll">roll of the robot's head relative to the body in radians</param>
		/// <param name="stereo_camera_index">stereo camera index</param>
		/// <param name="features">stereo features</param>
		private static void ProcessPose(
		    string path_identifier,
		    DateTime t,
		    float x,
		    float y,
		    float orientation,
		    float head_pan,
		    float head_tilt,
		    float head_roll,
			int stereo_camera_index,
			List<StereoFeatureTest> features)		                           
		{
            const bool use_compression = false;

		    // record pose positions to file, which may later be used as an index on the main disparities data
		    FileStream fs;
		    string disparities_index_filename = path_identifier + "_disparities_index.dat";
		    if (File.Exists(disparities_index_filename))
		        fs = File.Open(disparities_index_filename, FileMode.Append);
		    else
		        fs = File.Open(disparities_index_filename, FileMode.Create);
			
			BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(x);
            bw.Write(y);
            
            bw.Close();
            fs.Close();
		    			    			    			    
		    // record the full pose and disparities data to file
		    string disparities_filename = path_identifier + "_disparities.dat";
		    if (File.Exists(disparities_filename))
		        fs = File.Open(disparities_filename, FileMode.Append);
		    else
		        fs = File.Open(disparities_filename, FileMode.Create);
			
			bw = new BinaryWriter(fs);

            bw.Write(t.ToBinary());
            bw.Write(x);
            bw.Write(y);
            bw.Write(orientation);
            bw.Write(head_pan);
            bw.Write(head_tilt);
            bw.Write(head_roll);
            bw.Write(stereo_camera_index);
            bw.Write(features.Count);
			
			// convert stereo features to byte arrays
			int ctr0 = 0, ctr1 = 0;
			float[] packed_stereo_features = new float[features.Count * 3];
			byte[] packed_stereo_feature_colours = new byte[features.Count * 3];
			
            for (int i = 0; i < features.Count; i++)
            {
				packed_stereo_features[ctr0++] = features[i].x;
				packed_stereo_features[ctr0++] = features[i].y;
				packed_stereo_features[ctr0++] = features[i].disparity;
				packed_stereo_feature_colours[ctr1++] = features[i].colour[0];
				packed_stereo_feature_colours[ctr1++] = features[i].colour[1];
				packed_stereo_feature_colours[ctr1++] = features[i].colour[2];
            }
			
			// convert float array to bytes
			byte[] packed_stereo_features2 = ArrayConversions.ToByteArray(packed_stereo_features);

            if (use_compression)
            {
                // compress the feature data and write it to file
                byte[] packed_stereo_features2_compressed = AcedDeflator.Instance.Compress(packed_stereo_features2, 0, packed_stereo_features2.Length, AcedCompressionLevel.Fastest, 0, 0);
                bw.Write(packed_stereo_features2_compressed.Length);
                bw.Write(packed_stereo_features2_compressed);
            }
            else
            {
                bw.Write(packed_stereo_features2);
            }

            if (use_compression)
            {
                // compress the colour data and write it to file			
                byte[] packed_stereo_feature_colours_compressed = AcedDeflator.Instance.Compress(packed_stereo_feature_colours, 0, packed_stereo_feature_colours.Length, AcedCompressionLevel.Fastest, 0, 0);
                bw.Write(packed_stereo_feature_colours_compressed.Length);
                bw.Write(packed_stereo_feature_colours_compressed);
            }
            else
            {
                bw.Write(packed_stereo_feature_colours);
            }
			
            bw.Close();
			fs.Close();
		}
		
        private static void ShowPath(
            List<OdometryData> path,
            byte[] img, int img_width, int img_height,
            int r, int g, int b,
            bool clear,
            ref float tx, ref float ty,
            ref float bx, ref float by)
        {
            if (clear) for (int i = img.Length - 1; i >= 0; i--) img[i] = 255;

            float w, h;
            if (bx <= tx)
            {
                tx = float.MaxValue;
                ty = float.MaxValue;
                bx = float.MinValue;
                by = float.MinValue;
                for (int i = 0; i < path.Count; i++)
                {
                    float xx = path[i].x;
                    float yy = path[i].y;
                    if (xx < tx) tx = xx;
                    if (yy < ty) ty = yy;
                    if (xx > bx) bx = xx;
                    if (yy > by) by = yy;
                }

                w = bx - tx;
                tx -= w * 0.1f;
                bx += w * 0.1f;

                h = by - ty;
                ty -= h * 0.1f;
                by += h * 0.1f;
            }

            w = bx - tx;
            h = by - ty;

            if (w > h)
            {
                float cy = ty + ((by - ty) / 2);
                ty = cy - (w / 2);
                by = cy + (w / 2);
                h = w;
            }
            else
            {
                float cx = tx + ((bx - tx) / 2);
                tx = cx - (h / 2);
                bx = cx + (h / 2);
                w = h;
            }

            if ((w > 0) && (h > 0))
            {
                int prev_px = 0, prev_py = 0;
                for (int i = 0; i < path.Count; i++)
                {
                    int px = (int)((path[i].x - tx) * img_width / w);
                    int py = img_height - 1 - (int)((path[i].y - ty) * img_height / h);

                    if (i > 0)
                    {
                        drawing.drawLine(img, img_width, img_height, prev_px, prev_py, px, py, r, g, b, 1, false);
                    }

                    prev_px = px;
                    prev_py = py;
                }
            }
        }
				
        /// <summary>
        /// saves path data to file
        /// </summary>
        /// <param name="filename">filename to save as</param>
        /// <param name="path_length_mm">length of the path</param>
        /// <param name="start_orientation">orientation at the start of the path</param>
        /// <param name="end_orientation">orientation at the end of the path</param>
        /// <param name="distance_between_poses_mm">distance between poses</param>
        private static void SavePath(
		    string filename,
		    float path_length_mm,
		    float start_orientation,
		    float end_orientation,
		    float distance_between_poses_mm,
            float disparity,
		    ref List<OdometryData> save_path)
        {
            string[] str = filename.Split('.');
			int no_of_stereo_features = 300;
			float start_x_mm = 0;
			float start_y_mm = 0;
			float x_mm = start_x_mm, y_mm = start_y_mm;
			float orientation = start_orientation;
			int steps = (int)(path_length_mm / distance_between_poses_mm);
			save_path = new List<OdometryData>();
			Random rnd = new Random(0);
			
			if (File.Exists(str[0] + "_disparities_index.dat"))
				File.Delete(str[0] + "_disparities_index.dat");
			if (File.Exists(str[0] + "_disparities.dat"))
				File.Delete(str[0] + "_disparities.dat");
			
			for (int i = 0; i < steps; i++)
			{
				OdometryData data = new OdometryData();
				data.orientation = orientation;
				data.x = x_mm;
				data.y = y_mm;
				save_path.Add(data);
				
				List<StereoFeatureTest> features = new List<StereoFeatureTest>();
				for (int f = 0; f < no_of_stereo_features; f++)
				{
					StereoFeatureTest feat;
					if (f < no_of_stereo_features/2)
					    feat = new StereoFeatureTest(20, rnd.Next(239), disparity);
					else
						feat = new StereoFeatureTest(300, rnd.Next(239), disparity);
                    feat.SetColour(0, 0, 0);
					features.Add(feat);
				}

                ProcessPose(
		            str[0],
		            DateTime.Now,
		            x_mm, y_mm,
		            orientation,
		            0,0,0,
			        0, features);
				
				x_mm += distance_between_poses_mm * (float)Math.Sin(orientation);
				y_mm += distance_between_poses_mm * (float)Math.Cos(orientation);
				orientation = start_orientation + ((end_orientation - start_orientation) * i / steps);
			}

            FileStream fs = File.Open(filename, FileMode.Create);

            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(save_path.Count);
            for (int i = 0; i < save_path.Count; i++)
                save_path[i].Write(bw);

            bw.Close();
            fs.Close();

            // save images of the path
            if (filename.Contains("."))
            {
                int img_width = 640;
                int img_height = 480;
                byte[] img = new byte[img_width * img_height * 3];
                Bitmap bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                filename = str[0] + "_positions.jpg";
                float tx=0, ty=0;
                float bx=0, by=0;
                ShowPath(
				    save_path, img, img_width, img_height,
                    0,0,0, true,
                    ref tx, ref ty,
                    ref bx, ref by);				         
                BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }
		
        #endregion
		
		
		[Test()]
		public void LoadPath()
		{
		    string filename = "load_path.dat";
		    float path_length_mm = 10000;
		    float start_orientation = 0;
		    float end_orientation = 90 * (float)Math.PI / 180.0f;
		    float distance_between_poses_mm = 100;
            float disparity = 15;
			
			string[] str = filename.Split('.');
				
			List<OdometryData> path = null;
            SavePath(
		        filename,
		        path_length_mm,
		        start_orientation,
		        end_orientation,
		        distance_between_poses_mm,
                disparity,
			    ref path);
			
			Assert.AreEqual(true, File.Exists(filename));
			Assert.AreEqual(true, File.Exists(str[0] + "_disparities_index.dat"));
			Assert.AreEqual(true, File.Exists(str[0] + "_disparities.dat"));

            int no_of_grids = 2;
            int grid_type = metagrid.TYPE_SIMPLE;
            int dimension_mm = 3000;
            int dimension_vertical_mm = 2000;
            int cellSize_mm = 32;
            int localisationRadius_mm = 2000;
            int maxMappingRange_mm = 2000;
            float vacancyWeighting = 0.5f;

            metagridBuffer buffer =
                new metagridBuffer(
                    no_of_grids,
                    grid_type,
                    dimension_mm,
                    dimension_vertical_mm,
                    cellSize_mm,
                    localisationRadius_mm,
                    maxMappingRange_mm,
                    vacancyWeighting);

            buffer.LoadPath(filename, str[0] + "_disparities_index.dat", str[0] + "_disparities.dat");
            
            int img_width = 640;
            int img_height = 480;
            byte[] img = new byte[img_width * img_height * 3];
            buffer.ShowPath(img, img_width, img_height, true, true);
            Bitmap bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            bmp.Save("load_path.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }		

		[Test()]
		public void LocaliseAlongPath()
		{
            // systematic bias
            float bias_x_mm = -200;
            float bias_y_mm = 0;

		    string filename = "localise_along_path.dat";
		    float path_length_mm = 20000;
		    float start_orientation = 0;
		    float end_orientation = 0; //90 * (float)Math.PI / 180.0f;
		    float distance_between_poses_mm = 100;
            float disparity = 15;
			
			string[] str = filename.Split('.');
				
			List<OdometryData> path = null;
            SavePath(
		        filename,
		        path_length_mm,
		        start_orientation,
		        end_orientation,
		        distance_between_poses_mm,
                disparity,
			    ref path);
			
			Assert.AreEqual(true, File.Exists(filename));
			Assert.AreEqual(true, File.Exists(str[0] + "_disparities_index.dat"));
			Assert.AreEqual(true, File.Exists(str[0] + "_disparities.dat"));

            int no_of_grids = 1;
            int grid_type = metagrid.TYPE_SIMPLE;
            int dimension_mm = 8000;
            int dimension_vertical_mm = 2000;
            int cellSize_mm = 50;
            int localisationRadius_mm = 8000;
            int maxMappingRange_mm = 10000;
            float vacancyWeighting = 0.5f;

            metagridBuffer buffer =
                new metagridBuffer(
                    no_of_grids,
                    grid_type,
                    dimension_mm,
                    dimension_vertical_mm,
                    cellSize_mm,
                    localisationRadius_mm,
                    maxMappingRange_mm,
                    vacancyWeighting);

            buffer.LoadPath(filename, str[0] + "_disparities_index.dat", str[0] + "_disparities.dat");
            
            int img_width = 640;
            int img_height = 480;
            byte[] img = new byte[img_width * img_height * 3];
            Bitmap bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            buffer.ShowPath(img, img_width, img_height, true, true);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            bmp.Save("localise_along_path.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
			
			int no_of_stereo_features = 300;
		    float body_width_mm = 400;
		    float body_length_mm = 400;
		    float body_centre_of_rotation_x = body_width_mm/2;
		    float body_centre_of_rotation_y = body_length_mm/2;
		    float body_centre_of_rotation_z = 0;
		    float head_centroid_x = body_width_mm/2;
		    float head_centroid_y = body_length_mm/2;
		    float head_centroid_z = 0;
		    float head_pan = 0;
		    float head_tilt = 0;
		    float head_roll = 0;
		    float[] baseline_mm = { 100 };
		    float[] stereo_camera_position_x = { 0 };
		    float[] stereo_camera_position_y = { 0 };
		    float[] stereo_camera_position_z = { 0 };
		    float[] stereo_camera_pan = { 0 };
		    float[] stereo_camera_tilt = { 0 };
		    float[] stereo_camera_roll = { 0 };
            int[] image_width = { 320 };
            int[] image_height = { 240 };
            float[] FOV_degrees = { 78 };
            pos3D[] left_camera_location = new pos3D[1];
            pos3D[] right_camera_location = new pos3D[1];
            int no_of_samples = 300;
            float sampling_radius_major_mm = cellSize_mm*10;
            float sampling_radius_minor_mm = cellSize_mm*10;
            pos3D robot_pose = new pos3D(0,0,0);
            float max_orientation_variance = 5 * (float)Math.PI / 180.0f;
            float max_tilt_variance = 0;
            float max_roll_variance = 0;
            List<pos3D> poses = new List<pos3D>();
            List<float> pose_score = new List<float>();
		    Random rnd = new Random(0);
            pos3D pose_offset = null;
            bool buffer_transition = false;

		    float[][] stereo_features = new float[1][];
		    byte[][,] stereo_features_colour = new byte[1][,];
		    float[][] stereo_features_uncertainties = new float[1][];
			stereo_features_uncertainties[0] = new float[no_of_stereo_features];
			for (int i = 0; i < no_of_stereo_features; i++)
				stereo_features_uncertainties[0][i] = 1;

			stereoModel[] sensormodel = new stereoModel[1];
			sensormodel[0] = new stereoModel();
			sensormodel[0].createLookupTable(cellSize_mm, image_width[0], image_height[0]);

            float average_offset_x_mm = 0;
            float average_offset_y_mm = 0;
			List<OdometryData> estimated_path = new List<OdometryData>();
			
			for (int i = 0; i < path.Count-1; i += 5)
			{
                string debug_mapping_filename = "localise_along_path_map_" + i.ToString() + ".jpg";

				OdometryData p0 = path[i];
				OdometryData p1 = path[i + 1];
				
				// create an intermediate pose
				robot_pose.x = p0.x + ((p1.x - p0.x)/2) + bias_x_mm;
				robot_pose.y = p0.y + ((p1.y - p0.y)/2) + bias_y_mm;
				robot_pose.z = 0;
				robot_pose.pan = p0.orientation + ((p1.orientation - p0.orientation)/2);
				
				// create stereo features
				int ctr = 0;
				stereo_features[0] = new float[no_of_stereo_features * 3];
				stereo_features_colour[0] = new byte[no_of_stereo_features, 3];
				for (int f = 0; f < no_of_stereo_features; f += 5)
				{
					if (f < no_of_stereo_features/2)
					{
						stereo_features[0][ctr++] = 20;
						stereo_features[0][ctr++] = rnd.Next(239);
					}
					else
					{
						stereo_features[0][ctr++] = image_width[0] - 20;
						stereo_features[0][ctr++] = rnd.Next(239);
					}
				    stereo_features[0][ctr++] = disparity;
				}
				
                buffer.Localise(
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
		            image_width,
		            image_height,
		            FOV_degrees,
				    stereo_features,
				    stereo_features_colour,
				    stereo_features_uncertainties,
		            sensormodel,
		            ref left_camera_location,
		            ref right_camera_location,
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
		            ref pose_offset,
		            ref buffer_transition,
                    debug_mapping_filename,
                    bias_x_mm, bias_y_mm);
				
				Console.WriteLine("pose_offset (mm): " + pose_offset.x.ToString() + ", " + pose_offset.y.ToString() + ", " + pose_offset.pan.ToString());
				OdometryData estimated_pose = new OdometryData();
				estimated_pose.x = robot_pose.x + pose_offset.x;
				estimated_pose.y = robot_pose.y + pose_offset.y;
				estimated_pose.orientation = robot_pose.pan + pose_offset.pan;
				estimated_path.Add(estimated_pose);
                average_offset_x_mm += pose_offset.x;
                average_offset_y_mm += pose_offset.y;
			}

            buffer.ShowPath(img, img_width, img_height, true, true);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            bmp.Save("localisations_along_path.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            average_offset_x_mm /= estimated_path.Count;
            average_offset_y_mm /= estimated_path.Count;
            Console.WriteLine("Average offsets: " + average_offset_x_mm.ToString() + ", " + average_offset_y_mm.ToString());

            float diff_x_mm = Math.Abs(average_offset_x_mm - bias_x_mm);
            float diff_y_mm = Math.Abs(average_offset_y_mm - bias_y_mm);
            Assert.Less(diff_x_mm, 20.0f, "x bias not detected");
            Assert.Less(diff_y_mm, 20.0f, "y bias not detected");			
        }
				
		[Test()]
		public void Create()
		{
            int no_of_grids = 2;
            int grid_type = metagrid.TYPE_SIMPLE;
		    int dimension_mm = 3000;
            int dimension_vertical_mm = 2000; 
            int cellSize_mm = 32; 
            int localisationRadius_mm = 2000;
            int maxMappingRange_mm = 2000;
            float vacancyWeighting = 0.5f;
				
			metagridBuffer buffer = 
				new metagridBuffer(
                    no_of_grids,
                    grid_type,
		            dimension_mm, 
                    dimension_vertical_mm, 
                    cellSize_mm, 
                    localisationRadius_mm, 
                    maxMappingRange_mm, 
                    vacancyWeighting);
			
			Assert.AreNotEqual(null, buffer);
			Assert.AreNotEqual(null, buffer.buffer);
			Assert.AreEqual(2, buffer.buffer.Length);
			Assert.AreNotEqual(null, buffer.buffer[0]);
			Assert.AreNotEqual(null, buffer.buffer[1]);
			
			buffer.Reset();
		}

        [Test()]
        public void MoveToNextLocalGrid()
        {
            int no_of_grids = 2;
            int grid_type = metagrid.TYPE_SIMPLE;
		    int dimension_mm = 3000;
            int dimension_vertical_mm = 2000; 
            int cellSize_mm = 32; 
            int localisationRadius_mm = 2000;
            int maxMappingRange_mm = 2000;
            float vacancyWeighting = 0.5f;

            int current_grid_index = 0;
            int current_disparity_index = 0;
            pos3D robot_pose = new pos3D(0,0,0);
            metagrid[] buffer = new metagrid[2];
            int current_buffer_index = 0;
            List<float> grid_centres = new List<float>();
            bool update_map = false;

            float grid_centre_x_mm;
            float grid_centre_y_mm;
            float grid_centre_z_mm;

            // create some grid centres along a straight line path
            float path_length_mm = 10000;
            int steps = (int)(path_length_mm / (dimension_mm/2));
            for (int i = 0; i < steps; i++)
            {
                grid_centre_x_mm = 0;
                grid_centre_y_mm = i * path_length_mm / steps;
                grid_centre_z_mm = 0;
                grid_centres.Add(grid_centre_x_mm);
                grid_centres.Add(grid_centre_y_mm);
                grid_centres.Add(grid_centre_z_mm);
            }

            // create the buffer
            for (int i = 0; i < 2; i++)
            {
                buffer[i] = new metagrid(
                    no_of_grids,
                    grid_type,
                    dimension_mm,
                    dimension_vertical_mm,
                    cellSize_mm,
                    localisationRadius_mm,
                    maxMappingRange_mm,
                    vacancyWeighting);
            }

            // move along a straight line path
            int transitions = 0;
            for (int y = 0; y < path_length_mm; y += 100)
            {
                robot_pose.y = y;

                if (metagridBuffer.MoveToNextLocalGrid(
                    ref current_grid_index,
                    ref current_disparity_index,
                    robot_pose,
                    buffer,
                    ref current_buffer_index,
                    grid_centres,
                    ref update_map,
                    null))
                {
                    transitions++;
                }
                current_disparity_index = 1;
            }
            Assert.AreEqual(steps - 1, transitions, "Incorrect number of local grid transitions");
            Assert.AreEqual(steps - 1, current_grid_index, "Did not reach the final grid");
        }
	}
}
