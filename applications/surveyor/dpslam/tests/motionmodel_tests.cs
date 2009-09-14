
using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using dpslam.core;

namespace dpslam.core.tests
{
	[TestFixture()]
	public class motionmodel_tests
	{		
        /// <summary>
        /// generate scores for poses.  This is only for testing purposes.
        /// </summary>
        /// <param name="rob"></param>        
        private void surveyPosesDummy(robot rob)
        {
            motionModel motion_model = rob.motion;

            // examine the pose list
            for (int sample = 0; sample < motion_model.survey_trial_poses; sample++)
            {
                particlePath path = (particlePath)motion_model.Poses[sample];
                particlePose pose = path.current_pose;

                float dx = rob.x - pose.x;
                float dy = rob.y - pose.y;
                float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float score = 1.0f / (1 + dist);

                // update the score for this pose
                motion_model.updatePoseScore(path, score);
            }

            // indicate that the pose scores have been updated
            motion_model.PosesEvaluated = true;
        }
		
		[Test()]
		public void SampleNormalDistribution()
		{
			int image_width = 640;
            robot rob = new robot(1);
			motionModel mm = new motionModel(rob, rob.LocalGrid, 1);
			byte[] img_rays = new byte[image_width*image_width*3];
			Bitmap bmp = new Bitmap(image_width, image_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int[] hist = new int[20];
			int max = 1;
			int max_index = 0;
			for (int sample = 0; sample < 2000; sample++)
			{
				float v = mm.sample_normal_distribution(20);
			    int index = (hist.Length/2) + ((int)v/(hist.Length/2));
				if ((index > -1) && (index < hist.Length))
				{
					hist[index]++;
					if (hist[index] > max)
					{
						max = hist[index];
						max_index = index;
					}
				}
			}
			
			max += 5;
			for (int x = 0; x < image_width; x++)
			{
				int index = x * hist.Length / image_width;
				drawing.drawLine(img_rays, image_width, image_width, x,image_width-1, x, image_width-1-(hist[index] * image_width / max), 255,255,255,0,false);
			}
			
			BitmapArrayConversions.updatebitmap_unsafe(img_rays, bmp);
			bmp.Save("motionmodel_tests_SampleNormalDistribution.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
			
			Assert.IsTrue(max_index == hist.Length/2, "Peak of normal distribution is offset");
		}
				
		[Test()]
        public void OpenLoop()
        {
			int image_width = 640;
			byte[] img_rays = new byte[image_width*image_width*3];
			Bitmap bmp = new Bitmap(image_width, image_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			
			bool closed_loop = false;
            robot rob = new robot(1);

            int min_x_mm = 0;
            int min_y_mm = 0;
            int max_x_mm = 1000;
            int max_y_mm = 1000;
            int step_size = (max_y_mm - min_y_mm) / 15;
            int x = min_x_mm + ((max_x_mm - min_x_mm) / 2);
            bool initial = true;
            float pan = 0; // (float)Math.PI / 4;
            for (int y = min_y_mm; y <= max_y_mm; y += step_size)
            {
                if (closed_loop) surveyPosesDummy(rob);
				List<byte[]> disparities = new List<byte[]>();
                rob.updateFromKnownPosition(disparities, x, y, 0, pan, 0, 0);
                
                rob.motion.Show(
				    img_rays, image_width, image_width, 
                    min_x_mm, min_y_mm, max_x_mm, max_y_mm,
				    true, false,
                    initial);
                initial = false;
            }
									
			BitmapArrayConversions.updatebitmap_unsafe(img_rays, bmp);
			bmp.Save("motionmodel_tests_OpenLoop.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
			
			Assert.IsTrue(rob.y > max_y_mm-50, "The robot did not move far enough " + rob.y.ToString());
        }
		
		[Test()]
        public void SpeedControl()
        {
			int image_width = 640;
			byte[] img_rays = new byte[image_width*image_width*3];
			Bitmap bmp = new Bitmap(image_width, image_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			
			bool closed_loop = false;
            robot rob = new robot(1);

            int min_x_mm = 0;
            int min_y_mm = 0;
            int max_x_mm = 1000;
            int max_y_mm = 1000;
            int step_size = (max_y_mm - min_y_mm) / 15;
            int x = min_x_mm + ((max_x_mm - min_x_mm) / 2);
            bool initial = true;
            float pan = 0; // (float)Math.PI / 4;
			rob.x = x-200;
			rob.y = 0;
            for (int y = min_y_mm; y <= max_y_mm; y += step_size)
            {
                if (closed_loop) surveyPosesDummy(rob);
				List<byte[]> disparities = new List<byte[]>();
				float forward_velocity = step_size;
				float angular_velocity_pan = -4 * (float)Math.PI / 180.0f;
				rob.updateFromVelocities(disparities, forward_velocity, angular_velocity_pan, 0, 0, 1.0f);
                
                rob.motion.Show(
				    img_rays, image_width, image_width, 
                    min_x_mm, min_y_mm, max_x_mm, max_y_mm,
				    true, false,
                    initial);
                initial = false;
            }
									
			BitmapArrayConversions.updatebitmap_unsafe(img_rays, bmp);
			bmp.Save("motionmodel_tests_SpeedControl.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
			
			Assert.IsTrue(rob.y > max_y_mm-50, "The robot did not move far enough " + rob.y.ToString());
        }
	}
}
