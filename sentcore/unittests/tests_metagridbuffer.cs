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
		private void ProcessPose(
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
			
			// compress the feature data and write it to file
			byte[] packed_stereo_features2_compressed = AcedDeflator.Instance.Compress(packed_stereo_features2, 0, packed_stereo_features2.Length, AcedCompressionLevel.Fastest, 0, 0);
			bw.Write(packed_stereo_features2_compressed.Length);
			bw.Write(packed_stereo_features2_compressed);

			// compress the colour data and write it to file
			byte[] packed_stereo_feature_colours_compressed = AcedDeflator.Instance.Compress(packed_stereo_feature_colours, 0, packed_stereo_feature_colours.Length, AcedCompressionLevel.Fastest, 0, 0);
			bw.Write(packed_stereo_feature_colours_compressed.Length);
			bw.Write(packed_stereo_feature_colours_compressed);							
			
            bw.Close();
			fs.Close();
		}
		
        private void ShowPath(
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
        private void SavePath(
		    string filename,
		    float path_length_mm,
		    float start_orientation,
		    float end_orientation,
		    float distance_between_poses_mm)
        {
            string[] str = filename.Split('.');
			int no_of_stereo_features = 300;
			float start_x_mm = 0;
			float start_y_mm = 0;
			float x_mm = start_x_mm, y_mm = start_y_mm;
			float orientation = start_orientation;
			int steps = (int)(path_length_mm / distance_between_poses_mm);
			List<OdometryData> save_path = new List<OdometryData>();
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
				float disparity = 2;
				for (int f = 0; f < no_of_stereo_features; f++)
				{
					StereoFeatureTest feat;
					if (f < no_of_stereo_features/2)
					    feat = new StereoFeatureTest(100, rnd.Next(479), disparity);
					else
						feat = new StereoFeatureTest(540, rnd.Next(479), disparity);
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
		    float path_length_mm = 50000;
		    float start_orientation = 0;
		    float end_orientation = 40 * (float)Math.PI / 180.0f;
		    float distance_between_poses_mm = 100;
			
			string[] str = filename.Split('.');
				
            SavePath(
		        filename,
		        path_length_mm,
		        start_orientation,
		        end_orientation,
		        distance_between_poses_mm);
			
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
	}
}
