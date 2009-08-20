/*
    H-polar coordinate system
    See A Multi-Range Architecture for Collision-Free Off-Road Robot Navigation
    Pierre Sermanet, Raia Hadsell, Marco Scoffier, Matt Grimes, Jan Ben, Ayse Erkan, Chris Crudele, Urs Muller, Yann LeCun.
    Journal of Field Robotics (JFR) 2009
    http://cs.nyu.edu/~raia/docs/jfr-system.pdf
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
    public class Hpolar
    {
        public uint imgWidth = 320;
        public uint imgHeight = 240;
        private uint prev_imgWidth = 320;
        
        public int robot_x_mm, robot_y_mm;
        public int robot_orientation_degrees;
        public int map_x_mm, map_y_mm;
        private int hpolar_rP=0, hpolar_tP=0, hpolar_zP=0;

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

        private int SVS_MAP_CRES = 200;
        private int SVS_MAP_RMIN = 15260;
        private int SVS_MAP_HCAM = 1000;
        private int SVS_MAP_HRMIN = 970;
        private int SVS_MAP_RDIM = 30; //75;
        private int SVS_MAP_HYPRDIM = 30; //75;
        private int SVS_MAP_TDIM = 100;
        
        private int SVS_MAP_WIDTH_CELLS = 40;
        private int SVS_MAP_CELL_SIZE_MM = 100;
        public int SVS_MAP_MAX_RANGE_MM = 25000;
        const int SVS_EMPTY_CELL = 999999;
        const int TRIG_MULT = 1;

        const int SVS_MAX_FEATURES = 2000;

        /* array used to estimate footline of objects on the ground plane */
        public ushort[] footline;
        public ushort[] footline_dist_mm;

        public int[] svs_matches;
        
        private int[] Hpolar_map_coords;
        private byte[] Hpolar_map_occupancy;
        private int prev_array_length;
        
        int[] HpolarSinLookup = {
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
                int y_mm = y * SVS_MAP_CELL_SIZE_MM;
                if (y_mm > 0)
                {
                    for (int cell_x = 0; cell_x < map_x; cell_x++)
                    {
                        int x = cell_x - (map_x/2);
                        if ((cartesian_map[(cell_y*map_x + cell_x)*3] != 0) ||
                            (cartesian_map[(cell_y*map_x + cell_x)*3 + 1] != 0) ||
                            (cartesian_map[(cell_y*map_x + cell_x)*3 + 2] != 0))
                        {
                            int x_mm = x * SVS_MAP_CELL_SIZE_MM;
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
        
                int array_length = (SVS_MAP_RDIM + SVS_MAP_HYPRDIM)*SVS_MAP_TDIM*2;
                int n, i, offset, cell_x_mm, cell_y_mm, cell_x, cell_y;
                byte[] buffer_occupancy = new byte[array_length];
                int[] buffer_coords = new int[array_length];
        
                /* clear the buffer */
                for (i = array_length-2; i >= 0; i-=2) buffer_coords[i] = SVS_EMPTY_CELL;
                for (i = array_length-1; i >= 0; i--) buffer_occupancy[i]=0;
        
                /* update map cells */
                offset = SVS_MAP_WIDTH_CELLS/2;    
                for (i = array_length-2; i >= 0; i -= 2) {
                    if (Hpolar_map_coords[i] != SVS_EMPTY_CELL) {
                    
                        /* update cell position */
                        cell_x_mm = Hpolar_map_coords[i];
                        cell_y_mm = Hpolar_map_coords[i + 1];

                        /* convert from cartesian to H-polar */
                        svs_xyz_to_polar(cell_x_mm - map_x_mm - dx, cell_y_mm - map_y_mm - dy, 0);

                        if (hpolar_rP > 0) {                                                               
                            if (hpolar_tP < 0) hpolar_tP += (int)SVS_MAP_TDIM;
                            if (hpolar_tP >= (int)SVS_MAP_TDIM) hpolar_tP -= (int)SVS_MAP_TDIM;
                            n = (hpolar_rP*(int)SVS_MAP_TDIM + hpolar_tP)*2;
                            if ((n >= 0) && (n < array_length)) {
                                if (buffer_coords[n] == SVS_EMPTY_CELL) {
                                    /* new cell */
                                    buffer_occupancy[n] = Hpolar_map_occupancy[i];
                                    buffer_occupancy[n+1] = Hpolar_map_occupancy[i+1];
                                    buffer_coords[n] = cell_x_mm;
                                    buffer_coords[n+1] = cell_y_mm;
                                }
                                else {
                                    /* merge cells */
                                    buffer_occupancy[n] += (byte)((Hpolar_map_occupancy[i] - buffer_occupancy[n])/2);
                                    buffer_occupancy[n+1] += (byte)((Hpolar_map_occupancy[i+1] - buffer_occupancy[n+1])/2);
                                    buffer_coords[n] += (cell_x_mm - buffer_coords[n])/2;
                                    buffer_coords[n+1] += (cell_y_mm - buffer_coords[n+1])/2;
                                }
                            }
                        }
                    }
                }
            
                /* copy buffer back to the map */
                Buffer.BlockCopy(buffer_coords, 0, Hpolar_map_coords, 0, array_length*4);
                Buffer.BlockCopy(buffer_occupancy, 0, Hpolar_map_occupancy, 0, array_length);
            
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
            int i, rot, x, y, disp;
            int x_mm, y_mm, curr_y_mm, x_rotated_mm, y_rotated_mm, disp_mmx100;
            int r, n, SinVal, CosVal;
            int range_const = SVS_FOCAL_LENGTH_MMx100 * SVS_BASELINE_MM;
            int half_width = (int)imgWidth/2;
            int max_r = SVS_MAP_RDIM + SVS_MAP_HYPRDIM;
            int polar_stride = SVS_MAP_TDIM*2;
            int dx = robot_x_mm - map_x_mm;
            int dy = robot_y_mm - map_y_mm;
            int Hpolar_max = max_r*SVS_MAP_TDIM*2;

            if (Hpolar_map_coords == null)
            {
                Hpolar_map_coords = new int[Hpolar_max];
                Hpolar_map_occupancy = new byte[Hpolar_max];
                for (i = 0; i < Hpolar_max; i+=2) Hpolar_map_coords[i] = SVS_EMPTY_CELL;
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
                        y_mm = range_const / disp_mmx100;
                        
                        curr_y_mm = y_mm;                   
                        int rot_off;
                        int max_rot_off = (int)SVS_FOV_DEGREES/2;
                                        
                        rot_off = (x - half_width) * (int)SVS_FOV_DEGREES / (int)imgWidth;
                        if ((rot_off > -max_rot_off) && (rot_off < max_rot_off)) {
                            rot = robot_orientation_degrees + rot_off;
        
                            CosVal = 90 - rot;
                            if (CosVal < 0) CosVal += 360;
                            CosVal = HpolarSinLookup[CosVal];
                            if (rot >= 360) rot -= 360;
                            if (rot < 0) rot += 360;
                            SinVal = HpolarSinLookup[rot];
                            
                            // rotate by the orientation of the robot 
                            x_rotated_mm = SinVal * curr_y_mm / (int)10000;                        
                            y_rotated_mm = CosVal * curr_y_mm / (int)10000;
                            
                            // convert from cartesian grid to H-polar grid using lookup
                            svs_xyz_to_polar(x_rotated_mm + dx, y_rotated_mm + dy, 0);

                            if (hpolar_rP > 0) {                    
                            
                                // vacancy 
                                n = hpolar_tP*2;
                                int prob = 5;
                                if (prob > hpolar_rP) prob = hpolar_rP;
                                for (r = 0; r < hpolar_rP-1; r++, n += polar_stride) {
                                    if (n >= Hpolar_max) break;
                                        
                                    // absolute position of the point in cartesian space 
                                    x_mm = robot_x_mm + (r*x_rotated_mm/hpolar_rP);
                                    y_mm = robot_y_mm + (r*y_rotated_mm/hpolar_rP);
                                        
                                    if (Hpolar_map_coords[n] == SVS_EMPTY_CELL) {
                                        Hpolar_map_coords[n] = x_mm;
                                        Hpolar_map_coords[n+1] = y_mm;
                                    }
                                    else {
                                        Hpolar_map_coords[n] += (x_mm - Hpolar_map_coords[n])/2;
                                        Hpolar_map_coords[n+1] += (y_mm - Hpolar_map_coords[n+1])/2;
                                    }
                                    if (Hpolar_map_occupancy[n] < 245) {
                                        // increment vacancy                                     
                                        Hpolar_map_occupancy[n] += (byte)prob;
                                        prob--;
                                        if (prob < 1) prob = 1;
                                    }
                                    else {
                                        // decrement occupancy 
                                        if (Hpolar_map_occupancy[n+1] > 0) Hpolar_map_occupancy[n+1]--;
                                    }                    
                                }
                                if ((on_ground_plane == 0) && (n < Hpolar_max)) {
                                    // occupancy
                                    n++;
                                    prob = 20;
                                    /* in principle if the map cell size tracks stereo uncertainty 
                                       there would be no tail growth.  This is another crude
                                       approximation - better is possible */
                                    int tail_length = hpolar_rP/10;
                                    if (tail_length < 2) tail_length = 2;
                                    while (r < hpolar_rP+tail_length) {
                                        if (n >= Hpolar_max) break;
                                            
                                        // absolute position of the point in cartesian space 
                                        x_mm = robot_x_mm + (r*x_rotated_mm/hpolar_rP);
                                        y_mm = robot_y_mm + (r*y_rotated_mm/hpolar_rP);
                                            
                                        if (Hpolar_map_coords[n-1] == SVS_EMPTY_CELL) {
                                            Hpolar_map_coords[n-1] = x_mm;
                                            Hpolar_map_coords[n] = y_mm;
                                        }
                                        else {
                                            Hpolar_map_coords[n-1] += (x_mm - Hpolar_map_coords[n-1])/2;
                                            Hpolar_map_coords[n] += (y_mm - Hpolar_map_coords[n])/2;
                                        }
                                        if (r < max_r) {
                                            if (Hpolar_map_occupancy[n] < 245) {
                                                // increment occupancy (very crude sensor model)
                                                Hpolar_map_occupancy[n] += (byte)prob;
                                                prob--;
                                                if (prob < 1) prob = 1;
                                            }
                                            else {
                                                // decrement vacancy 
                                                if (Hpolar_map_occupancy[n-1] > 0) 
                                                    Hpolar_map_occupancy[n-1]--;
                                            }
                                        }
                                        else {
                                            break;
                                        }
                                        n += polar_stride;
                                        r++;
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
            int tP, rP,x,y,dx,dy,CosVal,r,n,n2;
            int centre_x = image_width/2;
            int centre_y = image_height/2;
            int radius = (image_height/2)-1;
            int orient, orient2, xx, yy;
            
            /* clear the map */
            rP = radius*radius;
            for (y = centre_y - radius; y < centre_y + radius; y++) {
                dy = y - centre_y;
                for (x = centre_x - radius; x < centre_x + radius; x++) {
                    dx = x - centre_x;
                    r = dx*dx + dy*dy;
                    if (r < rP) {
                        /* black */
                        n = (y*(int)image_width + x) * 3;
                        outbuf[n] = 0;
                        outbuf[n+1] = 0;
                        outbuf[n+2] = 0;
                    }
                }
            }
            
            /* draw occupancy and vacancy */
            for (orient = 0; orient < 360*TRIG_MULT; orient++) {
                
                orient2 = orient + robot_orientation_degrees + (90*TRIG_MULT);
                tP = (orient2 * (int)SVS_MAP_TDIM / ((int)360*TRIG_MULT));
                if (tP < 0) tP += (int)SVS_MAP_TDIM;
                if (tP >= (int)SVS_MAP_TDIM) tP -= (int)SVS_MAP_TDIM;
                
                dx = radius * HpolarSinLookup[orient] / (int)10000;
                CosVal = 90*TRIG_MULT - orient;
                if (CosVal < 0) CosVal += 360*TRIG_MULT;
                dy = radius * HpolarSinLookup[CosVal] / (int)10000;
                for (r = 0; r < radius; r++) {
                    rP = r * (SVS_MAP_RDIM + SVS_MAP_HYPRDIM-1) / radius;
                    x = centre_x + (dx * r / radius);
                    y = centre_y - (dy * r / radius);
                    n = ((y*image_width) + x) * 3;
                    n2 = (rP*SVS_MAP_TDIM + tP) * 2;
                    if (Hpolar_map_coords[n2] != SVS_EMPTY_CELL) {
                        if (Hpolar_map_occupancy[n2] >= Hpolar_map_occupancy[n2+1]) {
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
                }
            }            
        }
                
        
        /// <summary>
        /// Returns radius of the first hyperbolic cell
        /// </summary>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="hCam">Height of the camera</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="hRmin">Height of the image at radius Rmin</param>
        /// <returns>Radius of the first hyperbolic cell</returns>
        public static float CalcRmin(
            float Cres,
            float hCam,
            float hypRdim,
            float hRmin)
        {
            return(Cres * (((hCam * hypRdim) / hRmin) - 1));
        }

        /// <summary>
        /// Converts from cartesian coords to H-polar
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <param name="z_mm"></param>
        public void svs_xyz_to_polar(
            int x_mm,
            int y_mm,
            int z_mm)
        {
            const int scale = 16;
            x_mm *= scale;
            y_mm *= scale;
            
            int angle_degrees=0;
            uint v, length_mm;
            int abs_y = (int)y_mm;
            if (abs_y < 0) abs_y = -abs_y;
            
            hpolar_zP = z_mm;

            /* integer square root */
            v = (uint)x_mm*(uint)x_mm + (uint)y_mm*(uint)y_mm;
            for (length_mm=0; v >= (2*length_mm)+1; v -= (2*length_mm++)+1);

            if ((length_mm > 0) && (length_mm < SVS_MAP_MAX_RANGE_MM))
            {
                hpolar_rP =((((int)SVS_MAP_RMIN * (hpolar_zP - (int)SVS_MAP_HCAM)) / (int)length_mm) + (int)SVS_MAP_HCAM) *
                     (int)SVS_MAP_HYPRDIM / (int)SVS_MAP_HRMIN + (int)SVS_MAP_RDIM;
    
                if (abs_y < 1) abs_y = 1;
                if (x_mm >= 0)
                {
                    angle_degrees = 45 - (45 * ((int)x_mm - abs_y) / ((int)x_mm + abs_y));
                }
                else
                {
                    angle_degrees = 135 - (45 * ((int)x_mm + abs_y) / (abs_y - (int)x_mm));
                }
                if (y_mm < 0) {
                    hpolar_tP = -angle_degrees * SVS_MAP_TDIM / 360;
                }
                else {
                    hpolar_tP = angle_degrees * SVS_MAP_TDIM / 360;
                }
                if (hpolar_tP < 0) hpolar_tP += SVS_MAP_TDIM;
            }
        }
       
        /// <summary>
        /// Converts from cartesian coords to H-polar
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <param name="z_mm"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="rP">radial position</param>
        /// <param name="tP"></param>
        /// <param name="zP">height</param>
        public static void XYZtoPolar(
            float x_mm,
            float y_mm,
            float z_mm,
            float Cres,
            float Rmin,
            float hCam,
            float hRmin,
            float Rdim,
            float hypRdim,
            float Tdim,                                      
            ref float rP,
            ref float tP,
            ref float zP)
        {
            int angle_degrees=0;
            uint v, length_mm;
            int abs_y = (int)y_mm;
            if (abs_y < 0) abs_y = -abs_y;
            
            zP = z_mm;

            /* integer square root */
            if ((x_mm < -32000) || (x_mm > 32000) ||
                (y_mm < -32000) || (y_mm > 32000))
            {                
                x_mm/=10;
                y_mm/=10;
                v = (uint)(x_mm*x_mm + y_mm*y_mm);                
                for (length_mm=0; v >= (2*length_mm)+1; v -= (2*length_mm++)+1);
                x_mm*=10;
                y_mm*=10;
                length_mm*=10;
            }
            else
            {
                v = (uint)(x_mm*x_mm + y_mm*y_mm);                
                for (length_mm=0; v >= (2*length_mm)+1; v -= (2*length_mm++)+1);
            }

            //rP = (((Rmin * (zP - hCam)) / (float)length_mm) + hCam) *
              //   (hypRdim / hRmin) + Rdim;
            rP = (((Rmin * ((int)zP - hCam)) / (int)length_mm) + hCam) *
                 (int)hypRdim / (int)hRmin + Rdim;

            if (abs_y < 1) abs_y = 1;
            if (x_mm >= 0)
            {
                angle_degrees = 45 - (45 * ((int)x_mm - abs_y) / ((int)x_mm + abs_y));
            }
            else
            {
                angle_degrees = 135 - (45 * ((int)x_mm + abs_y) / (abs_y - (int)x_mm));
            }
            if (y_mm < 0)
                tP = -angle_degrees * Tdim / 360;
            else
                tP = angle_degrees * Tdim / 360;
        }

        /// <summary>
        /// Convert from H-polar coords to cartesian
        /// </summary>
        /// <param name="rP"></param>
        /// <param name="tP"></param>
        /// <param name="zP"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <param name="z_mm"></param>
        public static void PolartoXYZ(
            float rP,
            float tP,
            float zP,
            float Cres,
            float Rmin,
            float hCam,
            float hRmin,
            float Rdim,
            float hypRdim,
            float Tdim,
            ref float x_mm,
            ref float y_mm,
            ref float z_mm)
        {
            float radiusP = (Rmin * (zP - hCam)) / ((rP - Rdim) * (hRmin/hypRdim) - hCam);
            float alpha = tP * 2 * (float)Math.PI / Tdim;
            x_mm = (float)Math.Cos(alpha) * radiusP;
            y_mm = (float)Math.Sin(alpha) * radiusP;
            z_mm = zP;
        }


        /// <summary>
        /// creates a lookup table to convert between cartesian and H-polar coords
        /// </summary>
        /// <param name="cartesian_dimension_cells_width">width of the cartesian grid map in cells</param>
        /// <param name="cartesian_dimension_cells_range">range of the cartesian grid map in cells</param>
        /// <param name="cartesian_cell_size_mm"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="Hpolar">returned lookup table</param>
        public static void CreateHpolarLookup(
            int cartesian_dimension_cells_width,
            int cartesian_dimension_cells_range,
            float cartesian_cell_size_mm,
            float Cres,
            float Rmin,
            float hCam,
            float hRmin,
            float Rdim,
            float hypRdim,
            float Tdim,
            ref int[] Hpolar)
        {
            float rP=0, tP=0, zP=0;
            float scale = 100000.0f / (cartesian_dimension_cells_width*cartesian_cell_size_mm);

            if (Hpolar == null)
                Hpolar = new int[cartesian_dimension_cells_width * cartesian_dimension_cells_range * 2];

            XYZtoPolar(
                0, 100, 0,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                ref rP, ref tP, ref zP);
            Console.WriteLine("0,100  tP = " + tP.ToString());
            XYZtoPolar(
                100, 0, 0,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                ref rP, ref tP, ref zP);
            Console.WriteLine("100,0  tP = " + tP.ToString());
            XYZtoPolar(
                0, -100, 0,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                ref rP, ref tP, ref zP);
            Console.WriteLine("0,-100  tP = " + tP.ToString());
            
            int n = 0;
            for (int y = 0; y < cartesian_dimension_cells_range; y++)
            {
                float y_mm = ((y+0.5f) - (cartesian_dimension_cells_range/2)) * cartesian_cell_size_mm * scale;
                for (int x = 0; x < cartesian_dimension_cells_width; x++, n += 2)
                {
                    float x_mm = ((x+0.5f) - (cartesian_dimension_cells_width/2)) * cartesian_cell_size_mm * scale;
                    XYZtoPolar(
                        x_mm, y_mm, 0,
                        Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                        ref rP, ref tP, ref zP);
                    Hpolar[n] = (int)rP;
                    Hpolar[n+1] = (int)tP;
                }
            }
        }

        /// <summary>
        /// creates a lookup table to convert between cartesian and H-polar coords
        /// suitable for the SRV robot
        /// </summary>
        /// <param name="cartesian_dimension_cells_width">width of the cartesian grid map in cells</param>
        /// <param name="cartesian_dimension_cells_range">range of the cartesian grid map in cells</param>
        /// <param name="cartesian_cell_size_mm"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="Hpolar">returned lookup table</param>
        public static void CreateHpolarLookupSRV(
            int cartesian_dimension_cells_width,
            int cartesian_dimension_cells_range,
            float cartesian_cell_size_mm,
            float scale_down_factor,
            ref int[] Hpolar)
        {
            float rP=0, tP=0, zP=0;
            float Cres = 200;
            float hCam = 1000;
            float hRmin = 970;
            float Rdim = 75;
            float hypRdim = 75;
            float Tdim = 400;
            float Rmin = 15260;

            CreateHpolarLookup(
                cartesian_dimension_cells_width,
                cartesian_dimension_cells_range,
                cartesian_cell_size_mm,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,
                ref Hpolar);
        }

        public static void ShowHpolar(
            int cartesian_dimension_cells_width,
            int cartesian_dimension_cells_range,
            float cartesian_cell_size_mm,
            int[] Hpolar,
            int image_width,
            float scale_down_factor,
            string filename)
        {
            float Cres = 200;
            float hCam = 1000;
            float hRmin = 970;
            float Rdim = 75;
            float hypRdim = 75;
            float Tdim = 400;
            float Rmin = 15260;
            
            Console.WriteLine("Rmin " + Rmin.ToString());
            float rP=0, tP=0, zP=0;
            float x_mm=0, y_mm=0, z_mm=0;
            float min_x = float.MaxValue;
            float max_x = float.MinValue;
            float min_y = float.MaxValue;
            float max_y = float.MinValue;
            List<float> positions = new List<float>();
            List<int> colours = new List<int>();
            int r=0,g=0,b=0;

            for (int i = 0; i < Hpolar.Length; i += 2)
            {
                rP = Hpolar[i];
                tP = Hpolar[i+1];

                int val = (int)Math.Abs(rP*tP) % 7;
                //int val = (int)Math.Abs(rP) % 7;
                switch(val)
                {
                    case 0: { r=255; g=0; b=0; break; }
                    case 1: { r=0; g=255; b=0; break; }
                    case 2: { r=0; g=0; b=255; break; }
                    case 3: { r=255; g=0; b=255; break; }
                    case 4: { r=255; g=255; b=0; break; }
                    case 5: { r=0; g=255; b=255; break; }
                    case 6: { r=0; g=0; b=0; break; }
                }
                colours.Add(r);
                colours.Add(g);
                colours.Add(b);
                
                PolartoXYZ(
                    rP, tP, zP,
                    Cres, Rmin, hCam, hRmin, 
                    Rdim, hypRdim, Tdim,
                    ref x_mm,
                    ref y_mm,
                    ref z_mm);

                positions.Add(x_mm);
                positions.Add(y_mm);

                if (x_mm < min_x) min_x = x_mm;
                if (x_mm > min_x) max_x = x_mm;
                if (y_mm < min_y) min_y = y_mm;
                if (y_mm > min_y) max_y = y_mm;
            }
            Console.WriteLine("Min x: " + min_x.ToString());
            Console.WriteLine("Min y: " + min_y.ToString());
            Console.WriteLine("Max x: " + max_x.ToString());
            Console.WriteLine("Max y: " + max_y.ToString());

            byte[] img = new byte[image_width * image_width * 3];
            for (int i = 0; i < img.Length; i++) img[i] = 255;
            Bitmap bmp = new Bitmap(image_width, image_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int n = 0;
            for (int i = 0; i < positions.Count; i += 2, n += 3)
            {
                x_mm = positions[i];
                y_mm = positions[i + 1];                
                int x = (int)((x_mm - min_x) * image_width / (max_x - min_x));
                int y = (int)((y_mm - min_y) * image_width / (max_y - min_y));
                drawing.drawSpot(img, image_width, image_width, x, y, 4, colours[n],colours[n+1],colours[n+2]);
            }
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.EndsWith("bmp")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.EndsWith("jpg")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filename.EndsWith("gif")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
        }
        
        public static void SaveHpolar(
            int[] Hpolar,
            string filename)
        {
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
                oWrite.WriteLine("int HpolarLookup[] = {");
                int i = 0;
                for (int n = 0; n < Hpolar.Length; n += 2, i++)
                {
                    oWrite.Write(Hpolar[n].ToString() + "," + Hpolar[n+1].ToString() + ",");
                    if (i > 50)
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
