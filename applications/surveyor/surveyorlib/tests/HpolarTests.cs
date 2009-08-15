/*
    H-polar tests
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
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using sluggish.utilities;
using NUnit.Framework;

namespace surveyor.vision.tests
{
    [TestFixture()]
    public class HpolarTests
    {        
        [Test()]
        public void Recenter()
        {
        }

        [Test()]
        public void FootlineUpdate()
        {
            Hpolar map = new Hpolar();
            map.imgWidth = 320;
            map.imgHeight = 240;
            map.footline = new ushort[map.imgWidth / map.SVS_HORIZONTAL_SAMPLING];
            map.footline_dist_mm = new ushort[map.imgWidth / map.SVS_HORIZONTAL_SAMPLING];
            map.svs_ground_y_percent = 50;

            // create a footline
            for (int x = 0; x < (int)map.imgWidth/2; x++)
                map.footline[x / map.SVS_HORIZONTAL_SAMPLING] = (ushort)(map.imgHeight*70/100);
            for (int x = (int)map.imgWidth/2; x < (int)map.imgWidth; x++)
                map.footline[x / map.SVS_HORIZONTAL_SAMPLING] = (ushort)(map.imgHeight*60/100);

            map.FootlineUpdate(40);

            Console.WriteLine("dist0: " + map.footline_dist_mm[0].ToString());
            Console.WriteLine("dist1: " + map.footline_dist_mm[(map.imgWidth-1) / map.SVS_HORIZONTAL_SAMPLING].ToString());

            Assert.IsTrue(map.footline_dist_mm[0] > 0);
            Assert.IsTrue(map.footline_dist_mm[(map.imgWidth-1) / map.SVS_HORIZONTAL_SAMPLING] > 0);
            Assert.IsTrue(map.footline_dist_mm[(map.imgWidth-1) / map.SVS_HORIZONTAL_SAMPLING] > map.footline_dist_mm[0]);
        }
        
        [Test()]
        public void GroundPlaneUpdate()
        {
            int image_width = 320;
            int image_height = 240;
            Hpolar map = new Hpolar();
            map.footline = new ushort[image_width / map.SVS_HORIZONTAL_SAMPLING];
            map.footline_dist_mm = new ushort[image_width / map.SVS_HORIZONTAL_SAMPLING];

            // create a footline
            for (int x = image_width*20/100; x < image_width*40/100; x++)
                map.footline[x / map.SVS_HORIZONTAL_SAMPLING] = (ushort)(image_height/2);
            for (int x = image_width*60/100; x < image_width*80/100; x++)
                map.footline[x / map.SVS_HORIZONTAL_SAMPLING] = (ushort)(image_height/2);
            
            map.GroundPlaneUpdate();

            Assert.AreEqual(image_height/2, map.footline[(image_width/2) / map.SVS_HORIZONTAL_SAMPLING]);
            Assert.AreEqual(image_height/2, map.footline[(image_width-1) / map.SVS_HORIZONTAL_SAMPLING]);
            Assert.AreEqual(image_height/2, map.footline[0]);
        }
        
        [Test()]
        public void MapUpdate()
        {
            int max_disparity_percent = 40;
            int map_x = 60;
            int map_y = 60;
            byte[] cartesian_map = new byte[map_x * map_y * 3];
            int stereo_matches = 0;
            
            Hpolar map = new Hpolar();
            map.imgWidth = 320;
            map.imgHeight = 240;
            map.footline = new ushort[(int)map.imgWidth / map.SVS_HORIZONTAL_SAMPLING];
            map.footline_dist_mm = new ushort[(int)map.imgWidth / map.SVS_HORIZONTAL_SAMPLING];

            // create a footline
            for (int x = 0; x < (int)map.imgWidth / map.SVS_HORIZONTAL_SAMPLING; x++)
                map.footline[x] = (ushort)(map.imgHeight/2);

            map.FootlineUpdate(max_disparity_percent);
            
            /* create a map */
            drawing.drawLine(cartesian_map, map_x, map_y, (map_x/2) - 2, (map_y/2), (map_x/2) - 2, (map_y/2)-29, 255,255,255,0,false);
            drawing.drawLine(cartesian_map, map_x, map_y, (map_x/2) + 2, (map_y/2), (map_x/2) + 2, (map_y/2)-29, 255,255,255,0,false);

            Bitmap bmp = new Bitmap(map_x,map_y,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(cartesian_map, bmp);
            bmp.Save("HpolarTests_MapUpdate0.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

            /* convert map to stereo view */            
            map.SimulateStereoView(
                max_disparity_percent,
                map_x, map_y,
                cartesian_map,
                ref stereo_matches);

            map.SaveDisparities(
                "HpolarTests_MapUpdate1.bmp",
                stereo_matches);

            map.MapUpdate(stereo_matches, 40);
            map.Show("HpolarTests_MapUpdate2.bmp");

            Assert.IsTrue(stereo_matches > 0);
        }
    }
}
