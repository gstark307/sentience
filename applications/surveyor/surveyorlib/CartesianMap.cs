/*
    Local cartesian map
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

namespace surveyor.vision
{
    public class CartesianMap
    {
        public uint imgWidth = 320;
        public uint imgHeight = 240;
        private uint prev_imgWidth = 320;
        
        public int robot_x_mm, robot_y_mm;
        public int robot_orientation_degrees;
        public int map_x_mm, map_y_mm;

        private int prev_dist_moved_mm, dist_moved_mm;

        /* horizon y coordinate in pixels */
        public int svs_ground_y_percent;

        /* horizon slope in pixels */
        public int svs_ground_slope_percent;

        public int svs_sensor_width_mmx100 = 470;

        public int SVS_HORIZONTAL_SAMPLING = 8;
        
        public int SVS_BASELINE_MM = 107;
        public int SVS_FOCAL_LENGTH_MMx100 = 360;
        public int SVS_FOV_DEGREES = 90;

        private int MAP_WIDTH_CELLS = 100;
        private int MAP_CELL_SIZE_MM = 40;
        //public int SVS_MAP_MIN_RANGE_MM = 200;
        const int EMPTY_CELL = 999999;
        const int TRIG_MULT = 1;

        const int SVS_MAX_FEATURES = 2000;

        /* array used to estimate footline of objects on the ground plane */
        public ushort[] footline;
        public ushort[] footline_dist_mm;

        public int[] svs_matches;
        
        private int[] map_coords;
        private int[] map_occupancy;
        private int prev_array_length;

        /* min and max occupancy values */
        private int max_occupancy, min_occupancy;
        
        /* index numbers for the start of each ray model */
        const int sensmodel_indexes = 131;
        int[] sensmodelindex = {0,0,136,363,645,904,1057,1154,1222,1272,1311,1342,1367,1388,1406,1421,1435,1447,1458,1467,1476,1484,1491,1497,1503,1508,1513,1518,1522,1526,1530,1534,1537,1540,1543,1546,1549,1552,1554,1556,1558,1560,1562,1564,1566,1568,1570,1572,1574,1575,1576,1577,1578,1579,1580,1581,1582,1583,1584,1585,1586,1587,1588,1589,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590};
        
        /* sensor model, computed for baseline = 107mm, grid cell size = 40mm */
        int[] sensmodel = {
        1,1,1,1,2,2,2,2,2,2,2,2,2,3,4,4,4,4,4,5,5,5,5,5,7,7,7,7,7,8,8,8,8,8,12,12,12,12,12,14,14,14,14,14,18,19,19,19,19,21,22,22,22,22,26,30,30,29,29,31,34,34,33,33,37,44,43,43,43,44,49,49,48,48,50,62,62,61,61,61,68,68,68,67,67,84,84,84,83,83,91,92,91,91,91,108,112,111,111,110,118,121,120,120,119,134,144,143,143,142,148,154,153,153,152,164,181,180,180,179,183,192,192,191,190,198,223,222,221,220,222,235,234,233,232,233,
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,5,6,6,6,6,6,8,8,8,7,8,10,10,10,10,10,12,12,12,12,12,15,15,15,15,14,17,18,18,18,17,20,21,21,21,21,23,25,25,24,24,27,29,28,28,28,30,33,32,32,32,34,37,37,36,36,38,41,41,41,41,41,46,45,45,45,45,50,50,50,49,49,55,54,54,54,54,59,59,59,58,58,62,63,63,63,62,66,67,67,67,66,70,71,71,71,70,73,75,75,74,74,76,79,78,78,78,79,82,82,81,81,82,85,84,84,84,84,88,87,87,86,86,90,90,89,89,88,92,92,91,91,90,93,93,93,93,92,95,95,94,94,94,96,96,96,95,95,96,97,97,96,96,97,98,97,97,96,97,98,97,97,97,97,98,97,97,97,97,
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,2,2,2,2,3,3,4,4,4,4,6,6,6,6,7,8,9,9,9,9,13,13,13,13,14,16,17,18,18,18,22,23,23,23,24,27,28,29,29,29,34,36,36,36,37,40,42,43,43,42,46,50,49,49,50,52,56,56,56,55,57,63,62,62,62,63,68,67,67,67,67,73,72,72,71,71,76,76,75,75,74,78,78,78,77,76,80,81,79,79,78,80,80,80,79,78,80,82,80,79,79,79,79,78,78,76,77,79,77,76,76,75,74,74,73,71,72,74,72,71,71,70,68,67,67,65,65,67,66,64,64,63,60,60,60,58,57,59,58,57,56,56,53,52,52,51,50,50,50,49,49,48,46,44,44,44,43,43,43,42,41,41,39,37,37,37,36,36,36,35,34,34,33,31,31,31,30,29,30,29,28,28,28,25,25,25,24,24,24,24,23,23,23,21,20,20,20,20,20,19,19,19,19,17,16,16,16,16,16,16,15,15,15,14,13,13,13,13,13,12,12,12,12,12,10,10,10,10,10,10,10,10,10,9,8,8,8,8,8,8,8,8,8,8,7,6,6,6,6,6,6,6,6,6,5,
        2,2,3,3,3,5,5,5,7,7,8,9,9,12,12,14,15,16,20,19,22,24,24,29,29,31,35,34,40,41,43,47,47,53,54,56,61,61,66,68,69,75,75,79,82,82,89,88,91,95,94,100,100,102,105,105,109,109,110,113,113,115,115,116,118,118,119,119,118,120,120,119,119,118,119,118,117,116,115,115,115,113,111,110,109,109,107,104,103,102,101,100,95,95,94,93,92,86,86,85,84,83,77,77,75,74,74,68,67,66,65,65,60,59,58,57,56,52,50,50,49,48,45,43,43,41,41,38,36,36,35,35,33,30,30,29,29,27,25,25,24,24,23,21,21,20,20,19,17,17,16,16,16,14,14,13,13,13,11,11,11,11,11,9,9,9,9,9,7,7,7,7,7,6,6,6,5,5,5,4,4,4,4,4,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        2,2,3,3,4,5,7,7,9,10,13,14,17,19,23,25,29,32,37,40,44,50,55,60,65,72,76,82,90,96,98,109,113,120,123,133,135,142,148,155,154,163,165,171,170,177,176,180,181,185,181,183,183,184,182,180,179,177,176,173,170,167,163,160,158,151,147,144,141,134,131,128,120,117,115,109,103,101,97,90,87,85,78,75,73,69,63,62,60,54,51,50,46,43,41,40,35,34,33,30,27,27,25,22,21,21,17,17,17,15,14,13,12,11,10,10,9,8,8,7,6,6,6,5,5,4,4,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,
        3,3,5,6,8,11,13,18,22,26,34,38,50,56,67,77,86,105,111,129,139,154,173,179,201,208,223,239,242,260,262,272,279,280,284,283,284,283,281,272,268,262,254,249,233,226,216,203,197,181,172,163,148,142,129,119,114,99,95,86,77,73,62,58,53,46,44,37,34,31,26,25,21,19,17,14,13,11,10,9,7,7,6,5,5,3,3,3,2,2,1,1,1,1,1,0,0,
        3,5,6,11,15,20,30,41,49,67,85,104,125,154,173,203,237,266,282,320,338,360,384,392,398,408,410,407,398,382,371,355,335,315,290,267,244,227,198,180,158,143,121,111,90,82,67,61,47,44,33,31,23,22,15,14,10,10,6,6,4,4,2,2,1,1,1,1,
        4,7,11,17,29,45,63,84,115,158,204,245,287,348,398,454,483,510,541,554,558,547,520,493,467,423,393,332,295,259,217,193,145,126,101,81,69,47,40,29,23,18,12,10,6,5,4,2,2,1,
        5,9,18,32,55,86,126,184,260,342,434,502,577,656,702,728,709,688,654,594,527,449,376,305,253,197,141,110,84,55,42,30,18,13,9,5,3,2,1,
        5,12,26,53,99,167,266,374,505,640,768,873,898,893,862,786,677,556,441,343,241,174,121,80,51,30,19,11,6,3,1,
        6,17,42,95,187,327,499,685,883,1045,1136,1111,989,851,680,515,363,232,144,82,48,26,13,6,3,
        7,25,70,167,323,554,867,1155,1340,1339,1209,1006,750,515,316,175,93,46,21,9,3,
        9,34,106,265,597,999,1309,1559,1568,1341,998,600,326,164,73,29,10,3,
        10,47,155,480,954,1534,1863,1761,1419,934,492,220,83,29,8,
        12,63,271,747,1482,2114,2123,1564,946,452,159,46,11,2,
        14,80,433,1164,2159,2509,1912,1117,432,138,33,4,
        16,129,646,1827,2733,2455,1481,530,153,21,3,
        20,204,999,2419,3105,2170,841,211,24,
        22,278,1412,3159,3180,1579,314,49,3,
        23,399,2117,3944,2615,794,100,4,
        27,540,2812,4198,2033,363,23,
        28,774,3717,4135,1225,114,
        39,1098,4532,3650,649,28,
        53,1559,5217,2900,265,
        70,2052,5701,2057,116,
        99,2701,6087,1085,26,
        125,3322,5806,740,
        165,4162,5382,288,
        200,5506,4153,139,
        104,5932,3922,40,
        273,6561,3138,
        342,7817,1837,
        411,8188,1399,
        483,8853,662,
        405,9149,445,
        892,8503,603,
        1390,8558,
        1888,8073,
        2368,7606,
        2447,7551,
        2917,7081,
        2994,7004,
        1734,8263,
        4679,5320,
        5082,4917,
        5812,4187,
        6033,3966,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000,
        10000};
            
        int[] SinLookup = {
            0,174,348,523,697,871,1045,1218,1391,1564,1736,1908,2079,2249,2419,2588,2756,2923,3090,3255,3420,3583,3746,3907,4067,4226,4383,4539,4694,4848,5000,5150,5299,5446,5591,5735,5877,6018,6156,6293,6427,6560,6691,6819,6946,7071,
            7193,7313,7431,7547,7660,7771,7880,7986,8090,8191,8290,8386,8480,8571,8660,8746,8829,8910,8987,9063,9135,9205,9271,9335,9396,9455,9510,9563,9612,9659,9702,9743,9781,9816,9848,9876,9902,9925,9945,9961,9975,9986,9993,9998,9999,
            9998,9993,9986,9975,9961,9945,9925,9902,9876,9848,9816,9781,9743,9702,9659,9612,9563,9510,9455,9396,9335,9271,9205,9135,9063,8987,8910,8829,8746,8660,8571,8480,8386,8290,8191,8090,7986,7880,7771,7660,7547,7431,7313,7193,7071,
            6946,6819,6691,6560,6427,6293,6156,6018,5877,5735,5591,5446,5299,5150,5000,4848,4694,4539,4383,4226,4067,3907,3746,3583,3420,3255,3090,2923,2756,2588,2419,2249,2079,1908,1736,1564,1391,1218,1045,871,697,523,348,174,0,
            -174,-348,-523,-697,-871,-1045,-1218,-1391,-1564,-1736,-1908,-2079,-2249,-2419,-2588,-2756,-2923,-3090,-3255,-3420,-3583,-3746,-3907,-4067,-4226,-4383,-4539,-4694,-4848,-5000,-5150,-5299,-5446,-5591,-5735,-5877,-6018,-6156,-6293,-6427,-6560,-6691,-6819,-6946,-7071,
            -7193,-7313,-7431,-7547,-7660,-7771,-7880,-7986,-8090,-8191,-8290,-8386,-8480,-8571,-8660,-8746,-8829,-8910,-8987,-9063,-9135,-9205,-9271,-9335,-9396,-9455,-9510,-9563,-9612,-9659,-9702,-9743,-9781,-9816,-9848,-9876,-9902,-9925,-9945,-9961,-9975,-9986,-9993,-9998,-9999,
            -9998,-9993,-9986,-9975,-9961,-9945,-9925,-9902,-9876,-9848,-9816,-9781,-9743,-9702,-9659,-9612,-9563,-9510,-9455,-9396,-9335,-9271,-9205,-9135,-9063,-8987,-8910,-8829,-8746,-8660,-8571,-8480,-8386,-8290,-8191,-8090,-7986,-7880,-7771,-7660,-7547,-7431,-7313,-7193,-7071,
            -6946,-6819,-6691,-6560,-6427,-6293,-6156,-6018,-5877,-5735,-5591,-5446,-5299,-5150,-4999,-4848,-4694,-4539,-4383,-4226,-4067,-3907,-3746,-3583,-3420,-3255,-3090,-2923,-2756,-2588,-2419,-2249,-2079,-1908,-1736,-1564,-1391,-1218,-1045,-871,-697,-523,-348,-174,
        };
        
        public static float arctan2_float(float y, float x)
        {
           float coeff_1 = (float)Math.PI/4;
           float coeff_2 = 3*coeff_1;
           float abs_y = (float)(Math.Abs(y)+0.00001);      // kludge to prevent 0/0 condition
           float angle=0, r;
           if (x >= 0)
           {
              r = (x - abs_y) / (x + abs_y);
              angle = coeff_1 - coeff_1 * r;
           }
           else
           {
              r = (x + abs_y) / (abs_y - x);
              angle = coeff_2 - coeff_1 * r;
           }
           if (y < 0)
               return(-angle); // negate if in quad III or IV
           else
               return(angle);
        }

        public static int arctan2(int y, int x)
        {
           int coeff_1 = 45;
           int coeff_2 = 3*coeff_1;
           int abs_y = y;
           if (abs_y < 0) abs_y = -abs_y;
           if (abs_y < 1) abs_y = 1;
           int angle_degrees=0;
           if (x >= 0)
           {
              angle_degrees = coeff_1 - (coeff_1 * (x - abs_y) / (x + abs_y));
           }
           else
           {
              angle_degrees = coeff_2 - (coeff_1 * (x + abs_y) / (abs_y - x));
           }
           if (y < 0)
               return(-angle_degrees);
           else
               return(angle_degrees);
        }
        
        
        public void SaveDisparities(
            string filename,
            int stereo_matches)
        {
            int max_disp_pixels = 40 * (int)imgWidth / 100;
            byte[] img = new byte[imgWidth*imgHeight*3];
            Bitmap bmp = new Bitmap((int)imgWidth, (int)imgHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            for (int i = 0; i < stereo_matches; i++)
            {
                int x = svs_matches[i*4+1];
                int y = svs_matches[i*4+2];
                int disp = svs_matches[i*4+3];
                int radius = 1 + (disp/8);
                int v = disp * 255 / max_disp_pixels;
                if (v > 255) v = 255;
                drawing.drawSpot(img, (int)imgWidth, (int)imgHeight, x,y,radius,v,v,v);
            }
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.EndsWith("bmp")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.EndsWith("jpg")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filename.EndsWith("gif")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
            if (filename.EndsWith("png")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }
        
        public void SimulateStereoView(
            int max_disparity_percent,
            int map_x, int map_y,
            int cell_size_mm,
            byte[] cartesian_map,
            ref int stereo_matches)
        {
            int max_disp_pixels = max_disparity_percent * (int)imgWidth / 100;
            int ground_y = (int)imgHeight / 2;

            if (svs_matches == null) svs_matches = new int[SVS_MAX_FEATURES*4];
            
            stereo_matches = 0;
            for (int cell_y = 0; cell_y < map_y; cell_y++)
            {
                int y = (map_y/2) - cell_y;
                int y_mm = y * cell_size_mm;
                if (y_mm > 0)
                {
                    for (int cell_x = 0; cell_x < map_x; cell_x++)
                    {
                        int x = cell_x - (map_x/2);
                        if ((cartesian_map[(cell_y*map_x + cell_x)*3] != 0) ||
                            (cartesian_map[(cell_y*map_x + cell_x)*3 + 1] != 0) ||
                            (cartesian_map[(cell_y*map_x + cell_x)*3 + 2] != 0))
                        {
                            int x_mm = x * cell_size_mm;
                            float angle_degrees = (float)(Math.Atan(x_mm / (float)y_mm) * 180.0 / Math.PI);
                            if ((angle_degrees > -SVS_FOV_DEGREES/2) &&
                                (angle_degrees < SVS_FOV_DEGREES/2))
                            {
                                int disparityx100_mm = (SVS_BASELINE_MM*SVS_FOCAL_LENGTH_MMx100) / y_mm;
                                int disparity = disparityx100_mm * (int)imgWidth / svs_sensor_width_mmx100;
                                int image_x = (int)((angle_degrees  + (SVS_FOV_DEGREES/2)) * (int)imgWidth / SVS_FOV_DEGREES);
                                int image_y = (int)imgHeight - 1 - (ground_y - (ground_y * disparity / max_disp_pixels));
                                for (int yy = image_y; yy > 0; yy -= 4)
                                {
                                    if (stereo_matches == SVS_MAX_FEATURES) break;

                                    svs_matches[stereo_matches*4] = 1;
                                    svs_matches[stereo_matches*4 + 1] = image_x;
                                    svs_matches[stereo_matches*4 + 2] = yy;
                                    svs_matches[stereo_matches*4 + 3] = disparity;
                                    //Console.WriteLine(image_x.ToString() + "," + yy.ToString() + ", " + disparity.ToString());
                                    stereo_matches++;
                                }
                            }
                        }
                    }
                }
            }
        }
                
        public void Recenter()
        {            
            /* compute change in translation */
            int dx = robot_x_mm - map_x_mm;
            int dy = robot_y_mm - map_y_mm;
            
            if ((dx < -MAP_CELL_SIZE_MM) || (dx > MAP_CELL_SIZE_MM) ||
                (dy < -MAP_CELL_SIZE_MM) || (dy > MAP_CELL_SIZE_MM)) {

                int array_length = MAP_WIDTH_CELLS * MAP_WIDTH_CELLS;
                int n, i, offset, cell_x_mm, cell_y_mm, cell_x, cell_y;
                                
                int[] buffer_occupancy = new int[array_length];
                int[] buffer_coords = new int[array_length*2];
        
                /* clear the buffer */
                for (i = array_length*2-2; i >= 0; i-=2) buffer_coords[i] = EMPTY_CELL;
                for (i = array_length-1; i >= 0; i--) buffer_occupancy[i] = 0;

                /* update map cells */
                offset = MAP_WIDTH_CELLS/2;    
                for (i = 0; i < array_length; i++) {
                    if (map_coords[i*2] != EMPTY_CELL) {
                    
                        /* update cell position */
                        cell_x_mm = map_coords[i*2];
                        cell_y_mm = map_coords[i*2 + 1];

                        cell_x = offset + ((((cell_x_mm - map_x_mm - dx)+1)*2) / (MAP_CELL_SIZE_MM*2));
                        cell_y = offset + ((((cell_y_mm - map_y_mm - dy)+1)*2) / (MAP_CELL_SIZE_MM*2));
                        n = cell_y * MAP_WIDTH_CELLS + cell_x;
                        if ((cell_x >= 0) && (cell_y >= 0) &&
                            (cell_x < MAP_WIDTH_CELLS) && (cell_y < MAP_WIDTH_CELLS)) {

                            if (buffer_coords[n*2] == EMPTY_CELL) {
                                /* new cell */
                                buffer_occupancy[n] = map_occupancy[i];
                                buffer_coords[n*2] = cell_x_mm;
                                buffer_coords[n*2+1] = cell_y_mm;
                            }
                            else {
                                /* merge cells */
                                buffer_occupancy[n] += map_occupancy[i];
                                buffer_coords[n*2] += (cell_x_mm - buffer_coords[n*2])/2;
                                buffer_coords[n*2+1] += (cell_y_mm - buffer_coords[n*2+1])/2;
                            }
                        }
                    }
                }
            
                /* copy buffer back to the map */
                Buffer.BlockCopy(buffer_coords, 0, map_coords, 0, array_length*4*2);
                Buffer.BlockCopy(buffer_occupancy, 0, map_occupancy, 0, array_length*4);
            
                /* set the new map centre position */
                map_x_mm = robot_x_mm;
                map_y_mm = robot_y_mm;
                
                prev_dist_moved_mm = dist_moved_mm;                
            }
        }

        public void GroundPlaneUpdate()
        {
            int i, j, x, y, diff, prev_y = 0, prev_i=0;
                
            /* Remove points which have a large vertical difference.
               Successive points with small vertical difference are likely
               to correspond to real borders rather than carpet patterns, etc*/
            int max_diff = (int)imgHeight / 30;        
            for (i = 0; i < (int)imgWidth / SVS_HORIZONTAL_SAMPLING; i++) {
                x = i * SVS_HORIZONTAL_SAMPLING;
                y = footline[i];
                if (y != 0) {
                    if (prev_y != 0) {
                        diff = y - prev_y;
                        if (diff < 0) diff = -diff;
                        if (diff > max_diff) {
                            if (y < prev_y) {
                                footline[prev_i] = 0;
                            }
                            else {
                                footline[i] = 0;
                            }
                        }
                    }
                    prev_i = i;
                    prev_y = y;
                }
            }
            
            /* fill in missing data to create a complete ground plane */
            prev_i = 0;
            prev_y = 0;
            int max = (int)imgWidth / SVS_HORIZONTAL_SAMPLING;
            for (i = 0; i < max; i++) {
                x = i * SVS_HORIZONTAL_SAMPLING;
                y = footline[i];
                if (y != 0) {
                    if (prev_y == 0) prev_y = y;
                    for (j = prev_i; j < i; j++) {
                        footline[j] = (ushort)(prev_y + ((j - prev_i) * (y - prev_y) / (i - prev_i)));
                    }
                    prev_y = y;
                    prev_i = i;
                }
            }
            for (j = prev_i; j < max; j++) {
                footline[j] = (ushort)prev_y;
            }
        }
        
        public void FootlineUpdate(
            int max_disparity_percent)
        {
            int range_const = SVS_FOCAL_LENGTH_MMx100 * SVS_BASELINE_MM;
            int max = (int)imgWidth / (int)SVS_HORIZONTAL_SAMPLING;
            int i, x, y, disp, disp_mmx100, y_mm;
            int ground_y = (int)imgHeight * svs_ground_y_percent/100;
            int ground_y_sloped=0, ground_height_sloped=0;    
            int half_width = (int)imgWidth/2;
            int max_disp_pixels = max_disparity_percent * (int)imgWidth / 100;    
            int forward_movement_mm = 0;
            int forward_movement_hits = 0;
            
            if ((footline == null) || 
                (prev_imgWidth != imgWidth))
            {
                footline = new ushort[imgWidth / SVS_HORIZONTAL_SAMPLING];
                footline_dist_mm = new ushort[imgWidth / SVS_HORIZONTAL_SAMPLING];
            }        
            prev_imgWidth = imgWidth;
                
            for (i = 0; i < max; i++) {
                x = i * SVS_HORIZONTAL_SAMPLING;
                y = footline[i];
                ground_y_sloped = ground_y + ((half_width - x) * svs_ground_slope_percent / 100);
                ground_height_sloped = (int)imgHeight - 1 - ground_y_sloped;
                disp = (y - ground_y_sloped) * max_disp_pixels / ground_height_sloped;
                disp_mmx100 = disp * svs_sensor_width_mmx100 / (int)imgWidth;
                if (disp_mmx100 > 0) {
                    // get position of the feature in space 
                    y_mm = range_const / disp_mmx100;
        
                    if (footline_dist_mm[i] != 0) {
                        forward_movement_mm += y_mm - footline_dist_mm[i];
                        forward_movement_hits++;
                    }
                    footline_dist_mm[i] = (ushort)y_mm;
                }
                else {
                    footline_dist_mm[i] = 0;
                }
            }    
            if (forward_movement_hits > 0) {
                forward_movement_mm /= forward_movement_hits;
            }
        }

        public void MapUpdate(
            int no_of_matches,
            int max_disparity_percent)
        {
            int i, rot, x, y, xx, yy, disp;
            int x_mm, y_mm, curr_x_mm, curr_y_mm, x_rotated_mm, y_rotated_mm, disp_mmx100;
            int r, n, SinVal, CosVal;
            int range_const = SVS_FOCAL_LENGTH_MMx100 * SVS_BASELINE_MM;
            int half_width = (int)imgWidth/2;
            int dx = robot_x_mm - map_x_mm;
            int dy = robot_y_mm - map_y_mm;
            int array_length = MAP_WIDTH_CELLS * MAP_WIDTH_CELLS;
            int centre_cell = MAP_WIDTH_CELLS/2;

            if (map_coords == null)
            {
                map_coords = new int[array_length*2];
                map_occupancy = new int[array_length];
                for (i = 0; i < array_length*2; i += 2) map_coords[i] = EMPTY_CELL;

                max_occupancy = 2147473647;
                min_occupancy = -max_occupancy;
            }

            int on_ground_plane;
            for (i = 0; i < no_of_matches*4; i += 4) {           
                if (svs_matches[i] > 0) {  // match probability > 0
                    x = svs_matches[i + 1];
                    y = svs_matches[i + 2];
                    if (y < footline[x / SVS_HORIZONTAL_SAMPLING])
                        on_ground_plane = 0;
                    else
                        on_ground_plane = 1;
                    
                    disp = svs_matches[i + 3];
                    disp_mmx100 = disp * svs_sensor_width_mmx100 / (int)imgWidth;
                    if (disp_mmx100 > 0) {
                        // get position of the feature in space
                        int range_mm = range_const / disp_mmx100;
                        
                        curr_y_mm = range_mm;
                        int rot_off;
                        int max_rot_off = (int)SVS_FOV_DEGREES/2;
                                        
                        rot_off = (x - half_width) * (int)SVS_FOV_DEGREES / (int)imgWidth;
                        if ((rot_off > -max_rot_off) && (rot_off < max_rot_off)) {
                            if (rot_off < 0) rot_off += 360;
                            curr_x_mm = SinLookup[rot_off] * curr_y_mm / (int)10000;
                        
                            rot = -robot_orientation_degrees;
                            if (rot < 0) rot += 360;
                            if (rot >= 360) rot -= 360;
        
                            SinVal = SinLookup[rot];
                            CosVal = 90 - rot;
                            if (CosVal < 0) CosVal += 360;
                            CosVal = SinLookup[CosVal];
                            
                            // rotate by the orientation of the robot 
                            x_rotated_mm = (curr_x_mm*CosVal - curr_y_mm*SinVal) / (int)10000;
                            y_rotated_mm = (curr_x_mm*SinVal + curr_y_mm*CosVal) / (int)10000;
                            
                            int x_cell = ((x_rotated_mm+dx+1)*2) / (MAP_CELL_SIZE_MM*2);
                            int y_cell = ((y_rotated_mm+dy+1)*2) / (MAP_CELL_SIZE_MM*2);

                            if ((x_cell > -centre_cell) && (x_cell < centre_cell) &&
                                (y_cell > -centre_cell) && (y_cell < centre_cell)) {

                                //Console.WriteLine("x_cell " + x_cell.ToString() + " y_cell " + y_cell.ToString());

                                int abs_x_cell = x_cell;
                                if (abs_x_cell < 0) abs_x_cell = -abs_x_cell;
                                int abs_y_cell = y_cell;
                                if (abs_y_cell < 0) abs_y_cell = -abs_y_cell;

                                /* vacancy */
                                int cells = range_mm / MAP_CELL_SIZE_MM;
                                if (cells < 1) cells = 1;
                                int prob_average = 10000 / cells;
                                int prob = prob_average * 120 / 100;
                                int prob_decr = prob_average * 40 / (100 * cells);
                                if (abs_x_cell > abs_y_cell) {
                                    for (x = 0; x <= abs_x_cell; x++) {
                                        y = x * y_cell / abs_x_cell;
                                        xx = x;
                                        if (x_cell < 0) xx = -x;

                                        x_mm = robot_x_mm + (x*x_rotated_mm/abs_x_cell);
                                        y_mm = robot_y_mm + (x*y_rotated_mm/abs_x_cell);
                                        
                                        n = (((y + centre_cell) * MAP_WIDTH_CELLS) + (xx + centre_cell));
                                        if (map_coords[n*2] == EMPTY_CELL) {
                                            map_coords[n*2] = x_mm;
                                            map_coords[n*2+1] = y_mm;
                                        }
                                        else {
                                            map_coords[n*2] += (x_mm - map_coords[n*2])/2;
                                            map_coords[n*2+1] += (y_mm - map_coords[n*2+1])/2;
                                        }
                                        if ((map_occupancy[n] > min_occupancy) &&
                                            (map_occupancy[n] < max_occupancy)) {
                                            // increment vacancy                                     
                                            map_occupancy[n] -= prob;
                                            prob-=prob_decr;
                                            if (prob < 1) prob = 1;
                                        }
                                    }                                    
                                }
                                else {
                                    for (y = 0; y < abs_y_cell; y++) {
                                        x = y * x_cell / abs_y_cell;
                                        yy = y;
                                        if (y_cell < 0) yy = -y;

                                        x_mm = robot_x_mm + (y*x_rotated_mm/abs_y_cell);
                                        y_mm = robot_y_mm + (y*y_rotated_mm/abs_y_cell);
                                        
                                        n = (((yy + centre_cell) * MAP_WIDTH_CELLS) + (x + centre_cell));
                                        if (map_coords[n*2] == EMPTY_CELL) {
                                            map_coords[n*2] = x_mm;
                                            map_coords[n*2+1] = y_mm;
                                        }
                                        else {
                                            map_coords[n*2] += (x_mm - map_coords[n*2])/2;
                                            map_coords[n*2+1] += (y_mm - map_coords[n*2+1])/2;
                                        }
                                        if ((map_occupancy[n] > min_occupancy) &&
                                            (map_occupancy[n] < max_occupancy)) {
                                            // increment vacancy                                     
                                            map_occupancy[n] -= prob;
                                            prob-=prob_decr;
                                            if (prob < 1) prob = 1;
                                        }
                                    }                                    
                                }
                                
                                if (on_ground_plane == 0) {
                                    // occupancy
                                    int tail_length = 0;
                                    int tail_index = disp;
                                    if (disp+1 >= sensmodel_indexes) tail_index = sensmodel_indexes-2;
                                    tail_length = sensmodelindex[tail_index+1] - sensmodelindex[tail_index];
                                    int idx = sensmodelindex[disp];

                                    if (abs_x_cell > abs_y_cell) {
                                        for (x = abs_x_cell; x < abs_x_cell+tail_length; x++, idx++) {
                                            y = x * y_cell / abs_x_cell;
                                            xx = x;
                                            if (x_cell < 0) xx = -x;

                                            x_mm = robot_x_mm + (x*x_rotated_mm/abs_x_cell);
                                            y_mm = robot_y_mm + (x*y_rotated_mm/abs_x_cell);
                                            
                                            n = (((y + centre_cell) * MAP_WIDTH_CELLS) + (xx + centre_cell));
                                            if ((y+centre_cell >= 0) && (xx+centre_cell >= 0) &&
                                                (y+centre_cell < MAP_WIDTH_CELLS) && (xx+centre_cell < MAP_WIDTH_CELLS)) {
                                                if (map_coords[n*2] == EMPTY_CELL) {
                                                    map_coords[n*2] = x_mm;
                                                    map_coords[n*2+1] = y_mm;
                                                }
                                                else {
                                                    map_coords[n*2] += (x_mm - map_coords[n*2])/2;
                                                    map_coords[n*2+1] += (y_mm - map_coords[n*2+1])/2;
                                                }
                                                if ((map_occupancy[n] > min_occupancy) &&
                                                    (map_occupancy[n] < max_occupancy)) {
                                                    map_occupancy[n] += sensmodel[idx];
                                                }
                                            }
                                        }
                                    }
                                    else {
                                        for (y = abs_y_cell; y <= abs_y_cell+tail_length; y++, idx++) {
                                            x = y * x_cell / abs_y_cell;
                                            yy = y;
                                            if (y_cell < 0) yy = -y;

                                            x_mm = robot_x_mm + (y*x_rotated_mm/abs_y_cell);
                                            y_mm = robot_y_mm + (y*y_rotated_mm/abs_y_cell);
                                            
                                            n = (((yy + centre_cell) * MAP_WIDTH_CELLS) + (x + centre_cell));
                                            if ((yy+centre_cell >= 0) && (x+centre_cell >= 0) &&
                                                (yy+centre_cell < MAP_WIDTH_CELLS) && (x+centre_cell < MAP_WIDTH_CELLS)) {
                                                if (map_coords[n*2] == EMPTY_CELL) {
                                                    map_coords[n*2] = x_mm;
                                                    map_coords[n*2+1] = y_mm;
                                                }
                                                else {
                                                    map_coords[n*2] += (x_mm - map_coords[n*2])/2;
                                                    map_coords[n*2+1] += (y_mm - map_coords[n*2+1])/2;
                                                }
                                                if ((map_occupancy[n] > min_occupancy) &&
                                                    (map_occupancy[n] < max_occupancy)) {
                                                    // increment occupancy
                                                    map_occupancy[n] += sensmodel[idx];
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                           
                        }
                    }            
                }
            }

        }

        public void Show(
            string filename)
        {
            int image_width = 320;
            int image_height = 240;
            byte[] img = new byte[image_width * image_height * 3];
            Show(image_width, image_height, img);
            Bitmap bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.EndsWith("bmp")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.EndsWith("png")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            if (filename.EndsWith("gif")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
            if (filename.EndsWith("jpg")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        
        public void Show(
            int image_width,
            int image_height,
            byte[] outbuf)
        {
            int x,y,xx,yy,n,n2, prob, logodds;
            int occupancy_thresh=0, vacancy_thresh=0;
            int centre_cell = MAP_WIDTH_CELLS/2;

            /* find average occupancy and vacancy */
            uint average_occupancy = 0;
            uint average_vacancy = 0;
            uint occupancy_hits = 0;
            uint vacancy_hits = 0;
            n2 = MAP_WIDTH_CELLS*MAP_WIDTH_CELLS - 1;
            for (n = (MAP_WIDTH_CELLS*MAP_WIDTH_CELLS*2) - 2; n >= 0; n-=2, n2--) {
                if (map_coords[n] != EMPTY_CELL) {
                    logodds = map_occupancy[n2];
                    if (logodds > 0) {
                        prob = 100000 - (100000 / (1 + (logodds/65536)));                        
                        average_occupancy += (uint)(prob/64);
                        occupancy_hits++;
                    }
                    else {
                        prob = 100000 - (100000 / (1 - (logodds/16384)));
                        average_vacancy += (uint)prob;
                        vacancy_hits++;
                    }
                }
            }
            if (occupancy_hits > 0) {
                average_occupancy /= occupancy_hits;
                /* set the occupancy threshold */
                occupancy_thresh = (int)average_occupancy*64;
            }    
            if (vacancy_hits > 0) {
                average_vacancy /= vacancy_hits;
                /* set the vacancy threshold */
                vacancy_thresh = (int)average_vacancy * 40 / 100;
            }    
                        
            n2=0;
            for (y = 0; y < image_height; y++) {
                yy = (image_height - 1 - y) * MAP_WIDTH_CELLS / image_height;
                for (x = 0; x < image_width; x++) {
                    xx = x * MAP_WIDTH_CELLS / image_width;
                    n = (y*(int)image_width + x) * 3;
                    n2 = (yy * MAP_WIDTH_CELLS) + xx;
                    
                    if (map_coords[n2*2] != EMPTY_CELL) {
                        logodds = map_occupancy[n2];
                        if (logodds >= 0) 
                            prob = 100000 - (100000 / (1 + (logodds/65536)));
                        else
                            prob = 100000 - (100000 / (1 - (logodds/16384)));
                        
                        if ((logodds < 0) && (prob > vacancy_thresh)) {
                            /* vacant */
                            outbuf[n] = 0;
                            outbuf[n+1] = 255;
                            outbuf[n+2] = 0;
                        }
                        else {
                            if ((logodds >= 0) && (prob > occupancy_thresh)) {
                                /* occupied */
                                outbuf[n] = 0;
                                outbuf[n+1] = 0;
                                outbuf[n+2] = 255;
                            }
                            else {
                                /* not enough confidence to determine
                                   whether this is occupied or vacant */
                                outbuf[n] = 0;
                                outbuf[n+1] = 0;
                                outbuf[n+2] = 0;
                            }
                        }
                    }
                    else {
                        /* terra incognita */                    
                        outbuf[n] = 0;
                        outbuf[n+1] = 0;
                        outbuf[n+2] = 0;
                    }
                }
            }            
        }

        public static void SaveTrigLookup(
            string filename)
        {
            const int mult = 10000;
            StreamWriter oWrite=null;
            bool allowWrite = true;

            try
            {
                oWrite = File.CreateText(filename);
            }
            catch
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                oWrite.WriteLine("int SinLookup[] = {");
                int n, i = 0;
                for (n = 0; n < 360*TRIG_MULT; n++, i++)
                {
                    float angle = n * (float)Math.PI / (180.0f*TRIG_MULT);
                    int val = (int)(Math.Sin(angle) * mult);
                    oWrite.Write(val.ToString() + ",");
                    if (i >= 45)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.WriteLine("");
                oWrite.WriteLine("int CosLookup[] = {");
                i = 0;
                for (n = 0; n < 360*TRIG_MULT; n++, i++)
                {
                    float angle = n * (float)Math.PI / (180.0f*TRIG_MULT);
                    int val = (int)(Math.Cos(angle) * mult);
                    oWrite.Write(val.ToString() + ",");
                    if (i >= 45)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.WriteLine("");
                oWrite.WriteLine("int TanLookup[] = {");
                i = 0;
                for (n = 0; n < 360*TRIG_MULT; n++, i++)
                {
                    float angle = n * (float)Math.PI / (180.0f*TRIG_MULT);
                    int val = (int)(Math.Tan(angle) * mult);
                    oWrite.Write(val.ToString() + ",");
                    if (i >= 45)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.Close();
            }

        }

    }

}
