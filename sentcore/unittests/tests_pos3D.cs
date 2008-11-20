// pos3D.cs created with MonoDevelop
// User: motters at 21:29 05/11/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using NUnit.Framework;
using sentience.core;

namespace sentience.core.tests
{
	[TestFixture()]
	public class tests_pos3D
	{
		[Test()]
		public void AddPoints()
		{
		    pos3D[] point = new pos3D[3];
		    for (int i = 0; i < 2; i++) point[i] = new pos3D(0,0,0);
		    
		    point[0].x = 10;
		    point[0].y = 20;
		    point[0].z = 30;
		    point[0].pan = 1.4f;
		    point[0].tilt = 0.01f;
		    point[0].roll = 0.002f;

   		    point[1].x = 40;
		    point[1].y = 50;
		    point[1].z = 60;
		    point[1].pan = 2.1f;
		    point[1].tilt = 0.03f;
		    point[1].roll = 0.001f;
		    
		    point[2] = point[0].add(point[1]);
		    
		    Assert.AreEqual(10 + 40, point[2].x, "x");
		    Assert.AreEqual(20 + 50, point[2].y, "y");
		    Assert.AreEqual(30 + 60, point[2].z, "z");
		    Assert.AreEqual(1.4f + 2.1f, point[2].pan, "pan");
		    Assert.AreEqual(0.01f + 0.03f, point[2].tilt, "tilt");
		    Assert.AreEqual(0.002f + 0.001f, point[2].roll, "roll");
		}

		[Test()]
		public void SubtractPoints()
		{
		    pos3D[] point = new pos3D[3];
		    for (int i = 0; i < 2; i++) point[i] = new pos3D(0,0,0);
		    
		    point[0].x = 10;
		    point[0].y = 20;
		    point[0].z = 30;
		    point[0].pan = 1.4f;
		    point[0].tilt = 0.01f;
		    point[0].roll = 0.002f;

   		    point[1].x = 40;
		    point[1].y = 50;
		    point[1].z = 60;
		    point[1].pan = 2.1f;
		    point[1].tilt = 0.03f;
		    point[1].roll = 0.001f;
		    
		    point[2] = point[0].subtract(point[1]);
		    
		    Assert.AreEqual(10 - 40, point[2].x, "x");
		    Assert.AreEqual(20 - 50, point[2].y, "y");
		    Assert.AreEqual(30 - 60, point[2].z, "z");
		    Assert.AreEqual(1.4f - 2.1f, point[2].pan, "pan");
		    Assert.AreEqual(0.01f - 0.03f, point[2].tilt, "tilt");
		    Assert.AreEqual(0.002f - 0.001f, point[2].roll, "roll");
		}

        private float DegreesToRadians(float degrees)
        {
            return degrees * (float)Math.PI / 180.0f;
        }

        private float RadiansToDegrees(float radians)
        {
            return radians * 180.0f / (float)Math.PI;
        }

		[Test()]
		public void RotatePoint()
		{
		    pos3D point = new pos3D(0, 100, 0);
		    point.pan = DegreesToRadians(10);
		    
		    pos3D rotated = point.rotate(DegreesToRadians(30), 0, 0);
            int pan = (int)Math.Round(RadiansToDegrees(rotated.pan));		    		    		    		    
		    Assert.AreEqual(40, pan, "pan positive");
		    Assert.AreEqual(50, (int)Math.Round(rotated.x), "pan positive x");
		    Assert.AreEqual(87, (int)Math.Round(rotated.y), "pan positive y");

   		    rotated = point.rotate(DegreesToRadians(-30), 0, 0);
            pan = (int)Math.Round(RadiansToDegrees(rotated.pan));
		    Assert.AreEqual(-20, pan, "pan negative");
		    Assert.AreEqual(-50, (int)Math.Round(rotated.x), "pan negative x");
		    Assert.AreEqual(87, (int)Math.Round(rotated.y), "pan negative y");
		    
		    point.pan = 0;
		    point.tilt = DegreesToRadians(5);
   		    pos3D tilted = point.rotate(0, DegreesToRadians(30), 0);
            int tilt = (int)Math.Round(RadiansToDegrees(rotated.tilt));
		    Assert.AreEqual(35, tilt, "tilt positive");

   		    point.pan = 0;
   		    point.tilt = 0;
		    point.roll = DegreesToRadians(2);
   		    pos3D rolled = point.rotate(0, 0, DegreesToRadians(-20));
            int roll = (int)Math.Round(RadiansToDegrees(rotated.roll));
		    Assert.AreEqual(-18, roll, "roll negative");

		    //Console.WriteLine("x = " + rotated.x.ToString());
		    //Console.WriteLine("y = " + rotated.y.ToString());
		}


	}
}
