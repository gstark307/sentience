/*
    Unit tests for robot geometry class
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
using sluggish.utilities.xml;

namespace sentience.core.tests
{	
	[TestFixture()]
	public class tests_robotGeometry
	{
		
		[Test()]
		public void SaveAndLoad()
		{
			string filename = "tests_robotGeometry_SaveAndLoad.xml";
			int no_of_stereo_cameras = 2;
			robotGeometry geom1 = new robotGeometry();
			geom1.CreateStereoCameras(no_of_stereo_cameras, 120, 0, 320, 240, 78, 100, 0);
			geom1.SetBodyDimensions(800,700,600);
			geom1.SetCentreOfRotation(400, 350, 10);
			geom1.SetHeadPosition(400,350, 1000);
			geom1.Save(filename);
			
			robotGeometry geom2 = new robotGeometry();
			geom2.Load(filename);

			Assert.AreEqual(geom1.body_width_mm, geom2.body_width_mm);
			Assert.AreEqual(geom1.body_length_mm, geom2.body_length_mm);
			Assert.AreEqual(geom1.body_height_mm, geom2.body_height_mm);
			
			Assert.AreEqual(geom1.body_centre_of_rotation_x, geom2.body_centre_of_rotation_x);
			Assert.AreEqual(geom1.body_centre_of_rotation_y, geom2.body_centre_of_rotation_y);
			Assert.AreEqual(geom1.body_centre_of_rotation_z, geom2.body_centre_of_rotation_z);
			
			Assert.AreEqual(geom1.head_centroid_x, geom2.head_centroid_x);
			Assert.AreEqual(geom1.head_centroid_y, geom2.head_centroid_y);
            Assert.AreEqual(geom1.head_centroid_z, geom2.head_centroid_z);
						
			for (int cam = 0; cam < no_of_stereo_cameras; cam++)
			{
				Assert.AreEqual(geom1.baseline_mm[cam], geom2.baseline_mm[cam]);
				Assert.AreEqual(geom1.image_width[cam], geom2.image_width[cam]);
				Assert.AreEqual(geom1.image_height[cam], geom2.image_height[cam]);
				Assert.AreEqual(geom1.FOV_degrees[cam], geom2.FOV_degrees[cam]);
				Assert.AreEqual(geom1.stereo_camera_position_x[cam], geom2.stereo_camera_position_x[cam]);
				Assert.AreEqual(geom1.stereo_camera_position_y[cam], geom2.stereo_camera_position_y[cam]);
				Assert.AreEqual(geom1.stereo_camera_position_z[cam], geom2.stereo_camera_position_z[cam]);
                Assert.AreEqual(geom1.stereo_camera_pan[cam], geom2.stereo_camera_pan[cam]);
                Assert.AreEqual(geom1.stereo_camera_tilt[cam], geom2.stereo_camera_tilt[cam]);
                Assert.AreEqual(geom1.stereo_camera_roll[cam], geom2.stereo_camera_roll[cam]);
			}
			
		}
	}
}
