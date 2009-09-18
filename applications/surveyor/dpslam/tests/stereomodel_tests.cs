
using System;
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
	}
}
