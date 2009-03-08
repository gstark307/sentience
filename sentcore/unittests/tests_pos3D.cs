/*
    Unit tests for 3D functions
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
            int tilt = (int)Math.Round(RadiansToDegrees(tilted.tilt));
		    Assert.AreEqual(35, tilt, "tilt positive");

   		    point.pan = 0;
   		    point.tilt = 0;
		    point.roll = DegreesToRadians(2);
   		    pos3D rolled = point.rotate(0, 0, DegreesToRadians(-20));
            int roll = (int)Math.Round(RadiansToDegrees(rolled.roll));
		    Assert.AreEqual(-18, roll, "roll negative");

		    //Console.WriteLine("x = " + rotated.x.ToString());
		    //Console.WriteLine("y = " + rotated.y.ToString());
		}


        [Test()]
        public void Pan()
        {
            int pan_angle1 = -40;
            float pan1 = pan_angle1 * (float)Math.PI / 180.0f;
            
            pos3D pos1 = new pos3D(0, 50, 0);
            pos3D pos2 = pos1.rotate_old(pan1,0,0);
            pos3D pos3 = pos1.rotate(pan1,0,0);
            
            float dx = Math.Abs(pos2.x - pos3.x);
            float dy = Math.Abs(pos2.y - pos3.y);
            float dz = Math.Abs(pos2.z - pos3.z);
            Console.WriteLine("pos old: " + pos2.x.ToString() + ",  " + pos2.y.ToString() + ",  " + pos2.z.ToString());
            Console.WriteLine("pos new: " + pos3.x.ToString() + ",  " + pos3.y.ToString() + ",  " + pos3.z.ToString());
            Assert.Less(dx, 1);
            Assert.Less(dy, 1);
            Assert.Less(dz, 1);
	    }
	
        [Test()]
        public void Tilt()
        {
            int tilt_angle1 = 30;
            float tilt1 = tilt_angle1 * (float)Math.PI / 180.0f;
            
            pos3D pos1 = new pos3D(0, 50, 0);
            pos3D pos2 = pos1.rotate_old(0,tilt1,0);
            pos3D pos3 = pos1.rotate(0,tilt1,0);
            
            float dx = Math.Abs(pos2.x - pos3.x);
            float dy = Math.Abs(pos2.y - pos3.y);
            float dz = Math.Abs(pos2.z - pos3.z);
            Console.WriteLine("pos old: " + pos2.x.ToString() + ",  " + pos2.y.ToString() + ",  " + pos2.z.ToString());
            Console.WriteLine("pos new: " + pos3.x.ToString() + ",  " + pos3.y.ToString() + ",  " + pos3.z.ToString());
            Assert.Less(dx, 1);
            Assert.Less(dy, 1);
            Assert.Less(dz, 1);
	    }

        [Test()]
        public void Roll()
        {
            int roll_angle1 = 20;
            float roll1 = roll_angle1 * (float)Math.PI / 180.0f;
            
            pos3D pos1 = new pos3D(50, 0, 0);
            pos3D pos2 = pos1.rotate_old(0,0,roll1);
            pos3D pos3 = pos1.rotate(0,0,roll1);
            
            float dx = Math.Abs(pos2.x - pos3.x);
            float dy = Math.Abs(pos2.y - pos3.y);
            float dz = Math.Abs(pos2.z - pos3.z);
            Console.WriteLine("pos old: " + pos2.x.ToString() + ",  " + pos2.y.ToString() + ",  " + pos2.z.ToString());
            Console.WriteLine("pos new: " + pos3.x.ToString() + ",  " + pos3.y.ToString() + ",  " + pos3.z.ToString());
            Assert.Less(dx, 1);
            Assert.Less(dy, 1);
            Assert.Less(dz, 1);
	    }

        [Test()]
        public void PanTilt()
        {
            int pan_angle1 = -40;
            float pan1 = pan_angle1 * (float)Math.PI / 180.0f;
            int tilt_angle1 = 20;
            float tilt1 = tilt_angle1 * (float)Math.PI / 180.0f;
            
            pos3D pos1 = new pos3D(0, 50, 0);
            pos3D pos2 = pos1.rotate_old(pan1,tilt1,0);
            pos3D pos3 = pos1.rotate(pan1,tilt1,0);
            
            float dx = Math.Abs(pos2.x - pos3.x);
            float dy = Math.Abs(pos2.y - pos3.y);
            float dz = Math.Abs(pos2.z - pos3.z);
            Console.WriteLine("pos old: " + pos2.x.ToString() + ",  " + pos2.y.ToString() + ",  " + pos2.z.ToString());
            Console.WriteLine("pos new: " + pos3.x.ToString() + ",  " + pos3.y.ToString() + ",  " + pos3.z.ToString());
            Assert.Less(dx, 1);
            Assert.Less(dy, 1);
            Assert.Less(dz, 1);
	    }
		
	}
}
