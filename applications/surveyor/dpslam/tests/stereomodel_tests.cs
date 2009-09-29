
using System;
using System.Drawing;
using System.Collections.Generic;
using NUnit.Framework;
using dpslam.core;

namespace dpslam.core.tests
{	
	[TestFixture()]
	public class stereomodel_tests
	{
		[Test()]
		public void CreateRay()
		{
			stereoModel model = new stereoModel();
			evidenceRay ray = model.createRay(20,10,5,0,255,255,255);
			Assert.IsNotNull(ray.vertices);
			Assert.IsNotNull(ray.vertices[0]);
			Assert.IsNotNull(ray.vertices[1]);
		}
		
		[Test()]
		public void CreateGaussianLookup()
		{
            float[] lookup = stereoModel.createGaussianLookup(6);
			for (int i = 0; i < lookup.Length; i++)
			{
				Console.WriteLine(lookup[i].ToString());
				Assert.IsTrue(lookup[i] >= 0, "Gaussian lookup value out of range " + lookup[i].ToString());
				Assert.IsTrue(lookup[i] <= 1, "Gaussian lookup value out of range " + lookup[i].ToString());
			}
			Assert.IsTrue(lookup[2] > lookup[1]);
			Assert.IsTrue(lookup[1] > lookup[0]);
			Assert.IsTrue(lookup[3] > lookup[4]);
			Assert.IsTrue(lookup[4] > lookup[5]);
		}
		
		[Test()]
		public void CreateHalfGaussianLookup()
		{
            float[] lookup = stereoModel.createHalfGaussianLookup(6);
			for (int i = 0; i < lookup.Length; i++)
			{
				Console.WriteLine(lookup[i].ToString());
				Assert.IsTrue(lookup[i] >= 0, "Gaussian lookup value out of range " + lookup[i].ToString());
				Assert.IsTrue(lookup[i] <= 1, "Gaussian lookup value out of range " + lookup[i].ToString());
			}
			Assert.IsTrue(lookup[0] > lookup[1]);
			Assert.IsTrue(lookup[1] > lookup[2]);
			Assert.IsTrue(lookup[2] > lookup[3]);
			Assert.IsTrue(lookup[4] > lookup[5]);
		}		
		
		[Test()]
		public void DisparityToDistance()
		{
			int image_width = 320;
			float baseline_mm = 100;
			float focal_length_mm = 3.6f;
			float sensor_size_mm = 4.2f;
			float focal_length_pixels = focal_length_mm * image_width / sensor_size_mm;
			float disparity = 5;
			float range_mm = stereoModel.DisparityToDistance(disparity, focal_length_mm, image_width / sensor_size_mm, baseline_mm);
			Console.WriteLine("range_mm: " + range_mm.ToString());
			Assert.IsTrue(range_mm > 5400, "range " + range_mm.ToString() + " > 5400");
			Assert.IsTrue(range_mm < 5500, "range " + range_mm.ToString() + " < 5500");
			
			float disparity2 = stereoModel.DistanceToDisparity(range_mm, focal_length_pixels, baseline_mm);
			Assert.AreEqual(disparity, disparity2, 0.1f, "disparity " + disparity.ToString() + " != " + disparity2.ToString());
		}
		
		[Test()]
		public void CreateObservation()
		{			
		    float baseline = 120;
		    int image_width = 320;
		    int image_height = 240;
		    float FOV_degrees = 68;
			int no_of_stereo_features = 10;
			float[] stereo_features = new float[no_of_stereo_features*4];
			byte[] stereo_features_colour = new byte[no_of_stereo_features*3];
			bool translate = false;
			
			for (int i = 0; i < no_of_stereo_features; i++)
			{
				stereo_features[i*4] = 1;
				stereo_features[i*4+1] = i * image_width / no_of_stereo_features;
				stereo_features[i*4+2] = image_height/2;
				stereo_features[i*4+3] = 1;
				stereo_features_colour[i*3] = 200;
				stereo_features_colour[i*3+1] = 200;
				stereo_features_colour[i*3+2] = 200;
			}
			
			for (int rotation_degrees = 0; rotation_degrees < 360; rotation_degrees += 90)
			{
				stereoModel model = new stereoModel();
				pos3D observer = new pos3D(0,0,0);
				observer = observer.rotate(rotation_degrees/180.0f*(float)Math.PI,0,0);	
	            List<evidenceRay> rays = model.createObservation(
			        observer,
			        baseline,
			        image_width,
			        image_height,
			        FOV_degrees,
			        stereo_features,
			        stereo_features_colour,
			        translate);
				
				float tx = float.MaxValue;
				float ty = float.MaxValue;
				float bx = float.MinValue;
				float by = float.MinValue;
				for (int i = 0; i < no_of_stereo_features; i++)
				{
					//float pan_degrees = rays[i].pan_angle * 180 / (float)Math.PI; 
					//Console.WriteLine(pan_degrees.ToString());
					for (int j = 0; j < rays[i].vertices.Length; j++)
					{
					    Console.WriteLine("Vertex " + j.ToString());
					    Console.WriteLine("xyz: " + rays[i].vertices[j].x.ToString() + " " + rays[i].vertices[j].y.ToString() + " " + rays[i].vertices[j].z.ToString());
						
						if (rays[i].vertices[j].x < tx) tx = rays[i].vertices[j].x;
						if (rays[i].vertices[j].x > bx) bx = rays[i].vertices[j].x;
	
						if (rays[i].vertices[j].y < ty) ty = rays[i].vertices[j].y;
						if (rays[i].vertices[j].y > by) by = rays[i].vertices[j].y;
					}
				}
				
				int img_width = 640;
				Bitmap bmp = new Bitmap(img_width, img_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				byte[] img = new byte[img_width * img_width * 3];
				for (int i = 0; i < img.Length; i++) img[i] = 255;
	
				for (int i = 0; i < no_of_stereo_features; i++)
				{
					int x0 = (int)((rays[i].vertices[0].x - tx) * img_width / (bx - tx));
					int y0 = (int)((rays[i].vertices[0].y - ty) * img_width / (by - ty));
					int x1 = (int)((rays[i].vertices[1].x - tx) * img_width / (bx - tx));
					int y1 = (int)((rays[i].vertices[1].y - ty) * img_width / (by - ty));
					drawing.drawLine(img, img_width, img_width, x0,y0,x1,y1,0,0,0,0,false);
				}
				
				BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
				bmp.Save("dpslam_tests_createobservation_" + rotation_degrees.ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
				Console.WriteLine("dpslam_tests_createobservation_" + rotation_degrees.ToString() + ".bmp");
			}
		}
	}
}
