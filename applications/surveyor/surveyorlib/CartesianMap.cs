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

        private int SVS_MAP_WIDTH_CELLS = 100;
        private int SVS_MAP_CELL_SIZE_MM = 40;
        public int SVS_MAP_MAX_RANGE_MM = 50*100/2;
        public int SVS_MAP_MIN_RANGE_MM = 200;
        const int SVS_EMPTY_CELL = 999999;
        const int TRIG_MULT = 1;

        const int SVS_MAX_FEATURES = 2000;

        /* array used to estimate footline of objects on the ground plane */
        public ushort[] footline;
        public ushort[] footline_dist_mm;

        public int[] svs_matches;
        
        private int[] map_coords;
        private byte[] map_occupancy;
        private int prev_array_length;
        
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

        int[] CosLookup = {
            10000,9998,9993,9986,9975,9961,9945,9925,9902,9876,9848,9816,9781,9743,9702,9659,9612,9563,9510,9455,9396,9335,9271,9205,9135,9063,8987,8910,8829,8746,8660,8571,8480,8386,8290,8191,8090,7986,7880,7771,7660,7547,7431,7313,7193,7071,
            6946,6819,6691,6560,6427,6293,6156,6018,5877,5735,5591,5446,5299,5150,4999,4848,4694,4539,4383,4226,4067,3907,3746,3583,3420,3255,3090,2923,2756,2588,2419,2249,2079,1908,1736,1564,1391,1218,1045,871,697,523,348,174,0,
            -174,-348,-523,-697,-871,-1045,-1218,-1391,-1564,-1736,-1908,-2079,-2249,-2419,-2588,-2756,-2923,-3090,-3255,-3420,-3583,-3746,-3907,-4067,-4226,-4383,-4539,-4694,-4848,-5000,-5150,-5299,-5446,-5591,-5735,-5877,-6018,-6156,-6293,-6427,-6560,-6691,-6819,-6946,-7071,
            -7193,-7313,-7431,-7547,-7660,-7771,-7880,-7986,-8090,-8191,-8290,-8386,-8480,-8571,-8660,-8746,-8829,-8910,-8987,-9063,-9135,-9205,-9271,-9335,-9396,-9455,-9510,-9563,-9612,-9659,-9702,-9743,-9781,-9816,-9848,-9876,-9902,-9925,-9945,-9961,-9975,-9986,-9993,-9998,-9999,
            -9998,-9993,-9986,-9975,-9961,-9945,-9925,-9902,-9876,-9848,-9816,-9781,-9743,-9702,-9659,-9612,-9563,-9510,-9455,-9396,-9335,-9271,-9205,-9135,-9063,-8987,-8910,-8829,-8746,-8660,-8571,-8480,-8386,-8290,-8191,-8090,-7986,-7880,-7771,-7660,-7547,-7431,-7313,-7193,-7071,
            -6946,-6819,-6691,-6560,-6427,-6293,-6156,-6018,-5877,-5735,-5591,-5446,-5299,-5150,-4999,-4848,-4694,-4539,-4383,-4226,-4067,-3907,-3746,-3583,-3420,-3255,-3090,-2923,-2756,-2588,-2419,-2249,-2079,-1908,-1736,-1564,-1391,-1218,-1045,-871,-697,-523,-348,-174,0,
            174,348,523,697,871,1045,1218,1391,1564,1736,1908,2079,2249,2419,2588,2756,2923,3090,3255,3420,3583,3746,3907,4067,4226,4383,4539,4694,4848,4999,5150,5299,5446,5591,5735,5877,6018,6156,6293,6427,6560,6691,6819,6946,7071,
            7193,7313,7431,7547,7660,7771,7880,7986,8090,8191,8290,8386,8480,8571,8660,8746,8829,8910,8987,9063,9135,9205,9271,9335,9396,9455,9510,9563,9612,9659,9702,9743,9781,9816,9848,9876,9902,9925,9945,9961,9975,9986,9993,9998,
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
            
            if ((dx < -SVS_MAP_CELL_SIZE_MM) || (dx > SVS_MAP_CELL_SIZE_MM) ||
                (dy < -SVS_MAP_CELL_SIZE_MM) || (dy > SVS_MAP_CELL_SIZE_MM)) {
        
                int array_length = SVS_MAP_WIDTH_CELLS * SVS_MAP_WIDTH_CELLS * 2;
                int n, i, offset, cell_x_mm, cell_y_mm, cell_x, cell_y;
                byte[] buffer_occupancy = new byte[array_length];
                int[] buffer_coords = new int[array_length];
        
                /* clear the buffer */
                for (i = array_length-2; i >= 0; i-=2) buffer_coords[i] = SVS_EMPTY_CELL;
                for (i = array_length-1; i >= 0; i--) buffer_occupancy[i] = 0;
        
                /* update map cells */
                offset = SVS_MAP_WIDTH_CELLS/2;    
                for (i = array_length-2; i >= 0; i -= 2) {
                    if (map_coords[i] != SVS_EMPTY_CELL) {
                    
                        /* update cell position */
                        cell_x_mm = map_coords[i];
                        cell_y_mm = map_coords[i + 1];

                        cell_x = offset + ((((cell_x_mm - map_x_mm - dx)+1)*2) / (SVS_MAP_CELL_SIZE_MM*2));
                        cell_y = offset + ((((cell_y_mm - map_y_mm - dy)+1)*2) / (SVS_MAP_CELL_SIZE_MM*2));
                        n = (cell_y * SVS_MAP_WIDTH_CELLS + cell_x) * 2;
                        if ((n >= 0) && (n < array_length)) {
                            
                            if (buffer_coords[n] == SVS_EMPTY_CELL) {
                                /* new cell */
                                buffer_occupancy[n] = map_occupancy[i];
                                buffer_occupancy[n+1] = map_occupancy[i+1];
                                buffer_coords[n] = cell_x_mm;
                                buffer_coords[n+1] = cell_y_mm;
                            }
                            else {
                                /* merge cells */
                                buffer_occupancy[n] += (byte)((map_occupancy[i] - buffer_occupancy[n])/2);
                                buffer_occupancy[n+1] += (byte)((map_occupancy[i+1] - buffer_occupancy[n+1])/2);
                                buffer_coords[n] += (cell_x_mm - buffer_coords[n])/2;
                                buffer_coords[n+1] += (cell_y_mm - buffer_coords[n+1])/2;
                            }
                        }
                    }
                }
            
                /* copy buffer back to the map */
                Buffer.BlockCopy(buffer_coords, 0, map_coords, 0, array_length*4);
                Buffer.BlockCopy(buffer_occupancy, 0, map_occupancy, 0, array_length);
            
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
            int x_mm, y_mm, curr_y_mm, x_rotated_mm, y_rotated_mm, disp_mmx100;
            int r, n, SinVal, CosVal;
            int range_const = SVS_FOCAL_LENGTH_MMx100 * SVS_BASELINE_MM;
            int half_width = (int)imgWidth/2;
            int dx = robot_x_mm - map_x_mm;
            int dy = robot_y_mm - map_y_mm;
            int array_length = SVS_MAP_WIDTH_CELLS * SVS_MAP_WIDTH_CELLS * 2;
            int centre_cell = SVS_MAP_WIDTH_CELLS/2;

            if (map_coords == null)
            {
                map_coords = new int[array_length];
                map_occupancy = new byte[array_length];
                for (i = 0; i < array_length; i+=2) map_coords[i] = SVS_EMPTY_CELL;
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
                            rot = robot_orientation_degrees + rot_off;

                            if (rot >= 360) rot -= 360;
                            if (rot < 0) rot += 360;

                            SinVal = SinLookup[rot];
                            CosVal = 90 - rot;
                            if (CosVal < 0) CosVal += 360;
                            CosVal = SinLookup[CosVal];                            
                            
                            // rotate by the orientation of the robot 
                            x_rotated_mm = SinVal * curr_y_mm / (int)10000;
                            y_rotated_mm = CosVal * curr_y_mm / (int)10000;

                            int x_cell = ((x_rotated_mm+dx+1)*2) / (SVS_MAP_CELL_SIZE_MM*2);
                            int y_cell = ((y_rotated_mm+dy+1)*2) / (SVS_MAP_CELL_SIZE_MM*2);

                            if ((x_cell > -centre_cell) && (x_cell < centre_cell) &&
                                (y_cell > -centre_cell) && (y_cell < centre_cell)) {

                                //Console.WriteLine("x_cell " + x_cell.ToString() + " y_cell " + y_cell.ToString());

                                int abs_x_cell = x_cell;
                                if (abs_x_cell < 0) abs_x_cell = -abs_x_cell;
                                int abs_y_cell = y_cell;
                                if (abs_y_cell < 0) abs_y_cell = -abs_y_cell;

                                /* vacancy */
                                int prob = 10;
                                if (abs_x_cell > abs_y_cell) {
                                    for (x = 0; x <= abs_x_cell; x++) {
                                        y = x * y_cell / abs_x_cell;
                                        xx = x;
                                        if (x_cell < 0) xx = -x;

                                        x_mm = robot_x_mm + (x*x_rotated_mm/abs_x_cell);
                                        y_mm = robot_y_mm + (x*y_rotated_mm/abs_x_cell);
                                        
                                        n = (((y + centre_cell) * SVS_MAP_WIDTH_CELLS) + (xx + centre_cell)) * 2;
                                        if (map_coords[n] == SVS_EMPTY_CELL) {
                                            map_coords[n] = x_mm;
                                            map_coords[n+1] = y_mm;
                                        }
                                        else {
                                            map_coords[n] += (x_mm - map_coords[n])/2;
                                            map_coords[n+1] += (y_mm - map_coords[n+1])/2;
                                        }
                                        if (map_occupancy[n] < 245) {
                                            // increment vacancy                                     
                                            map_occupancy[n] += (byte)prob;
                                            prob--;
                                            if (prob < 1) prob = 1;
                                        }
                                        else {
                                            // decrement occupancy 
                                            if (map_occupancy[n+1] > 0) map_occupancy[n+1]--;
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
                                        
                                        n = (((yy + centre_cell) * SVS_MAP_WIDTH_CELLS) + (x + centre_cell)) * 2;
                                        if (map_coords[n] == SVS_EMPTY_CELL) {
                                            map_coords[n] = x_mm;
                                            map_coords[n+1] = y_mm;
                                        }
                                        else {
                                            map_coords[n] += (x_mm - map_coords[n])/2;
                                            map_coords[n+1] += (y_mm - map_coords[n+1])/2;
                                        }
                                        if (map_occupancy[n] < 245) {
                                            // increment vacancy                                     
                                            map_occupancy[n] += (byte)prob;
                                            prob--;
                                            if (prob < 1) prob = 1;
                                        }
                                        else {
                                            // decrement occupancy 
                                            if (map_occupancy[n+1] > 0) map_occupancy[n+1]--;
                                        }                                        
                                    }                                    
                                }
                                
                                if ((on_ground_plane == 0) && 
                                    (range_mm > SVS_MAP_MIN_RANGE_MM)) {
                                    // occupancy
                                    prob = 10;
                                    int tail_length = range_mm / ((int)SVS_MAP_CELL_SIZE_MM*3);

                                    if (abs_x_cell > abs_y_cell) {
                                        for (x = abs_x_cell; x <= abs_x_cell+tail_length; x++) {
                                            y = x * y_cell / abs_x_cell;
                                            xx = x;
                                            if (x_cell < 0) xx = -x;

                                            x_mm = robot_x_mm + (x*x_rotated_mm/abs_x_cell);
                                            y_mm = robot_y_mm + (x*y_rotated_mm/abs_x_cell);
                                            
                                            n = (((y + centre_cell) * SVS_MAP_WIDTH_CELLS) + (xx + centre_cell)) * 2;
                                            if ((n >= 0) && (n < array_length)) {
                                                if (map_coords[n] == SVS_EMPTY_CELL) {
                                                    map_coords[n] = x_mm;
                                                    map_coords[n+1] = y_mm;
                                                }
                                                else {
                                                    map_coords[n] += (x_mm - map_coords[n])/2;
                                                    map_coords[n+1] += (y_mm - map_coords[n+1])/2;
                                                }
                                                if (map_occupancy[n+1] < 245) {
                                                    // increment occupancy (very crude sensor model)
                                                    map_occupancy[n+1] += (byte)prob;
                                                    prob--;
                                                    if (prob < 1) prob = 1;
                                                }
                                                else {
                                                    // decrement vacancy 
                                                    if (map_occupancy[n] > 0) 
                                                        map_occupancy[n]--;
                                                }                                                
                                            }
                                        }
                                    }
                                    else {
                                        for (y = abs_y_cell; y <= abs_y_cell+tail_length; y++) {
                                            x = y * x_cell / abs_y_cell;
                                            yy = y;
                                            if (y_cell < 0) yy = -y;

                                            x_mm = robot_x_mm + (y*x_rotated_mm/abs_y_cell);
                                            y_mm = robot_y_mm + (y*y_rotated_mm/abs_y_cell);
                                            
                                            n = (((yy + centre_cell) * SVS_MAP_WIDTH_CELLS) + (x + centre_cell)) * 2;
                                            if ((n >= 0) && (n < array_length)) {
                                                if (map_coords[n] == SVS_EMPTY_CELL) {
                                                    map_coords[n] = x_mm;
                                                    map_coords[n+1] = y_mm;
                                                }
                                                else {
                                                    map_coords[n] += (x_mm - map_coords[n])/2;
                                                    map_coords[n+1] += (y_mm - map_coords[n+1])/2;
                                                }
                                                if (map_occupancy[n+1] < 245) {
                                                    // increment occupancy (very crude sensor model)
                                                    map_occupancy[n+1] += (byte)prob;
                                                    prob--;
                                                    if (prob < 1) prob = 1;
                                                }
                                                else {
                                                    // decrement vacancy 
                                                    if (map_occupancy[n] > 0) 
                                                        map_occupancy[n]--;
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
            int x,y,xx,yy,n,n2;
            int centre_cell = SVS_MAP_WIDTH_CELLS/2;
            
            n2=0;
            for (y = 0; y < image_height; y++) {
                yy = (image_height - 1 - y) * SVS_MAP_WIDTH_CELLS / image_height;
                for (x = 0; x < image_width; x++) {
                    xx = x * SVS_MAP_WIDTH_CELLS / image_width;
                    n = (y*(int)image_width + x) * 3;
                    n2 = ((yy * SVS_MAP_WIDTH_CELLS) + xx) * 2;
                    
                    if (map_coords[n2] != SVS_EMPTY_CELL) {
                        if (map_occupancy[n2] >= map_occupancy[n2+1]) {
                            /* vacant */
                            outbuf[n] = 0;
                            outbuf[n+1] = 255;
                            outbuf[n+2] = 0;
                        }
                        else {
                            /* occupied */
                            outbuf[n] = 0;
                            outbuf[n+1] = 0;
                            outbuf[n+2] = 255;
                        }
                    }
                    else {
                        /* black */                    
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
