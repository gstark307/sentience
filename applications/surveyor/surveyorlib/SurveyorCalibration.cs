/*
    Calibration functions for the surveyor stereo camera
    Copyright (C) 2008 Bob Mottram
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
using sluggish.utilities;

namespace surveyor.vision
{
    public class SurveyorCalibration
    {
        public const int dots_across = 20;
        public const int dot_radius_percent = 30;    
            
        /// <summary>
        /// use canny algorithm to find edges
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="edge_detector"></param>
        /// <param name="dots"></param>
        /// <param name="edges_bmp"></param>
        private static void DetectEdges(Bitmap bmp, ref EdgeDetectorCanny edge_detector,
                                        ref hypergraph dots,
                                        ref Bitmap edges_bmp)
        {
            byte[] image_data = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, image_data);
            
            if (edge_detector == null) edge_detector = new EdgeDetectorCanny();
            edge_detector.automatic_thresholds = true;
            edge_detector.connected_sets_only = true;
            byte[] edges_data = edge_detector.Update(image_data, bmp.Width, bmp.Height);

            bool connected_sets_ok = false;
            try
            {
                edges_data = edge_detector.GetConnectedSetsImage(image_data, 10, bmp.Width / SurveyorCalibration.dots_across * 3, true, ref dots);
                connected_sets_ok = true;
            }
            catch
            {
            }

            if (connected_sets_ok)
            {
                // locate the red centre dot
                int max_redness = 0;
                CalibrationDot centre_dot = null;
                for (int i = 0; i < dots.Nodes.Count; i++)
                {
                    CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                    int n = (((int)dot.y * bmp.Width) + (int)dot.x) * 3;
                    if ((n > 3) && (n < image_data.Length - 4))
                    {
                        int r = image_data[n + 2] + image_data[n + 2 + 3] + image_data[n + 2 - 3];
                        int g = image_data[n + 1] + image_data[n + 1 + 3] + image_data[n + 1 - 3];
                        int b = image_data[n] + image_data[n + 3] + image_data[n - 3];

                        int redness = (r * 2) - g - b;
                        if (redness > max_redness)
                        {
                            max_redness = redness;
                            centre_dot = dot;
                        }
                    }
                }
                if (centre_dot != null) centre_dot.centre = true;

                if (edges_bmp == null)
                    edges_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                BitmapArrayConversions.updatebitmap_unsafe(edges_data, edges_bmp);
            }
        }
        
        /// <summary>
        /// detects a square around the centre dot on the calibration pattern
        /// </summary>
        /// <param name="dots">detected dots</param>
        /// <param name="centredots">returned dots belonging to the centre square</param>
        /// <returns>centre square</returns>
        private static polygon2D GetCentreSquare(hypergraph dots,
                                                 ref List<CalibrationDot> centredots)
        {
            polygon2D centre_square = null;
            centredots = new List<CalibrationDot>();

            // find the centre dot
            CalibrationDot centre = null;
            int i = 0;
            while ((i < dots.Nodes.Count) && (centre == null))
            {
                CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                if (dot.centre) centre = dot;
                i++;
            }

            if (centre != null)
            {            
                // look for the four surrounding dots
                List<CalibrationDot> centre_dots = new List<CalibrationDot>();
                List<double> distances = new List<double>();
                i = 0;
                for (i = 0; i < dots.Nodes.Count; i++)
                {
                    CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                    if (!dot.centre)
                    {
                        double dx = dot.x - centre.x;
                        double dy = dot.y - centre.y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (distances.Count == 4)
                        {
                            int index = -1;
                            double max_dist = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                if (distances[j] > max_dist)
                                {
                                    index = j;
                                    max_dist = distances[j];
                                }
                            }
                            if (dist < max_dist)
                            {
                                distances[index] = dist;
                                centre_dots[index] = dot;
                            }
                        }
                        else
                        {
                            distances.Add(dist);
                            centre_dots.Add(dot);
                        }
                    }
                }

                if (centre_dots.Count == 4)
                {
                    centre_square = new polygon2D();
                    for (i = 0; i < 4; i++)
                        centre_square.Add(0, 0);

                    double xx = centre.x;
                    double yy = centre.y;
                    int index = 0;
                    for (i = 0; i < 4; i++)
                    {
                        if ((centre_dots[i].x < xx) &&
                            (centre_dots[i].y < yy))
                        {
                            xx = centre_dots[i].x;
                            yy = centre_dots[i].y;
                            centre_square.x_points[0] = (float)xx;
                            centre_square.y_points[0] = (float)yy;
                            index = i;
                        }
                    }
                    centredots.Add(centre_dots[index]);

                    xx = centre.x;
                    yy = centre.y;
                    for (i = 0; i < 4; i++)
                    {
                        if ((centre_dots[i].x > xx) &&
                            (centre_dots[i].y < yy))
                        {
                            xx = centre_dots[i].x;
                            yy = centre_dots[i].y;
                            centre_square.x_points[1] = (float)xx;
                            centre_square.y_points[1] = (float)yy;
                            index = i;
                        }
                    }
                    centredots.Add(centre_dots[index]);

                    xx = centre.x;
                    yy = centre.y;
                    for (i = 0; i < 4; i++)
                    {
                        if ((centre_dots[i].x > xx) &&
                            (centre_dots[i].y > yy))
                        {
                            xx = centre_dots[i].x;
                            yy = centre_dots[i].y;
                            centre_square.x_points[2] = (float)xx;
                            centre_square.y_points[2] = (float)yy;
                            index = i;
                        }
                    }
                    centredots.Add(centre_dots[index]);

                    xx = centre.x;
                    yy = centre.y;
                    for (i = 0; i < 4; i++)
                    {
                        if ((centre_dots[i].x < xx) &&
                            (centre_dots[i].y > yy))
                        {
                            xx = centre_dots[i].x;
                            yy = centre_dots[i].y;
                            centre_square.x_points[3] = (float)xx;
                            centre_square.y_points[3] = (float)yy;
                            index = i;
                        }
                    }
                    centredots.Add(centre_dots[index]);
                }
            }
            return (centre_square);
        }
        
        /// <summary>
        /// links detected dots together
        /// </summary>
        /// <param name="dots">detected dots</param>
        /// <param name="current_dot">the current dot of interest</param>
        /// <param name="horizontal_dx">current expected horizontal displacement x coordinate</param>
        /// <param name="horizontal_dy">current expected horizontal displacement y coordinate</param>
        /// <param name="vertical_dx">current expected vertical displacement x coordinate</param>
        /// <param name="vertical_dy">current expected vertical displacement x coordinate</param>
        /// <param name="start_index">index number indicating which direction we will search in first</param>
        /// <param name="search_regions">returned list of search regions</param>
        private static void LinkDots(hypergraph dots,
                                     CalibrationDot current_dot,
                                     double horizontal_dx, double horizontal_dy,
                                     double vertical_dx, double vertical_dy,
                                     int start_index,
                                     List<CalibrationDot> search_regions)
        {
            if (!current_dot.centre)
            {
                int start_index2 = 0;
                double tollerance_divisor = 0.3f;
                double horizontal_tollerance = Math.Sqrt((horizontal_dx * horizontal_dx) + (horizontal_dy * horizontal_dy)) * tollerance_divisor;
                double vertical_tollerance = Math.Sqrt((vertical_dx * vertical_dx) + (vertical_dy * vertical_dy)) * tollerance_divisor;

                double x = 0, y = 0;
                List<int> indexes_found = new List<int>();
                List<bool> found_vertical = new List<bool>();

                // check each direction
                for (int i = 0; i < 4; i++)
                {
                    // starting direction offset
                    int ii = i + start_index;
                    if (ii >= 4) ii -= 4;

                    if (current_dot.Flags[ii] == false)
                    {
                        current_dot.Flags[ii] = true;
                        int opposite_flag = ii + 2;
                        if (opposite_flag >= 4) opposite_flag -= 4;

                        switch (ii)
                        {
                            case 0:
                                {
                                    // look above
                                    x = current_dot.x - vertical_dx;
                                    y = current_dot.y - vertical_dy;
                                    break;
                                }
                            case 1:
                                {
                                    // look right
                                    x = current_dot.x + horizontal_dx;
                                    y = current_dot.y + horizontal_dy;
                                    break;
                                }
                            case 2:
                                {
                                    // look below
                                    x = current_dot.x + vertical_dx;
                                    y = current_dot.y + vertical_dy;
                                    break;
                                }
                            case 3:
                                {
                                    // look left
                                    x = current_dot.x - horizontal_dx;
                                    y = current_dot.y - horizontal_dy;
                                    break;
                                }
                        }

                        CalibrationDot search_region = new CalibrationDot();
                        search_region.x = x;
                        search_region.y = y;
                        search_region.radius = (float)horizontal_tollerance;
                        search_regions.Add(search_region);

                        for (int j = 0; j < dots.Nodes.Count; j++)
                        {
                            if ((!((CalibrationDot)dots.Nodes[j]).centre) &&
                                (dots.Nodes[j] != current_dot))
                            {
                                double dx = ((CalibrationDot)dots.Nodes[j]).x - x;
                                double dy = ((CalibrationDot)dots.Nodes[j]).y - y;
                                double dist_from_expected_position = Math.Sqrt(dx * dx + dy * dy);
                                bool dot_found = false;
                                if ((ii == 0) || (ii == 2))
                                {
                                    // vertical search
                                    if (dist_from_expected_position < vertical_tollerance)
                                    {
                                        dot_found = true;
                                        found_vertical.Add(true);
                                    }
                                }
                                else
                                {
                                    // horizontal search
                                    if (dist_from_expected_position < horizontal_tollerance)
                                    {
                                        dot_found = true;
                                        found_vertical.Add(false);
                                    }
                                }

                                if (dot_found)
                                {
                                    indexes_found.Add(j);
                                    j = dots.Nodes.Count;
                                }

                            }
                        }



                    }
                }

                for (int i = 0; i < indexes_found.Count; i++)
                {
                    start_index2 = start_index + 1;
                    if (start_index2 >= 4) start_index2 -= 4;

                    double found_dx = ((CalibrationDot)dots.Nodes[indexes_found[i]]).x - current_dot.x;
                    double found_dy = ((CalibrationDot)dots.Nodes[indexes_found[i]]).y - current_dot.y;

                    CalibrationLink link = new CalibrationLink();

                    if (found_vertical[i])
                    {
                        link.horizontal = false;

                        if (((vertical_dy > 0) && (found_dy < 0)) ||
                            ((vertical_dy < 0) && (found_dy > 0)))
                        {
                            found_dx = -found_dx;
                            found_dy = -found_dy;
                        }
                        LinkDots(dots, (CalibrationDot)dots.Nodes[indexes_found[i]], horizontal_dx, horizontal_dy, found_dx, found_dy, start_index2, search_regions);
                    }
                    else
                    {
                        link.horizontal = true;

                        if (((horizontal_dx > 0) && (found_dx < 0)) ||
                            ((horizontal_dx < 0) && (found_dx > 0)))
                        {
                            found_dx = -found_dx;
                            found_dy = -found_dy;
                        }
                        LinkDots(dots, (CalibrationDot)dots.Nodes[indexes_found[i]], found_dx, found_dy, vertical_dx, vertical_dy, start_index2, search_regions);
                    }

                    dots.LinkByReference((CalibrationDot)dots.Nodes[indexes_found[i]], current_dot, link);

                }

            }
        }
        
        private static void ApplyGrid(hypergraph dots,
                                      List<CalibrationDot> centre_dots)
        {
            const int UNASSIGNED = 9999;

            // mark all dots as unassigned
            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                dot.grid_x = UNASSIGNED;
                dot.grid_y = UNASSIGNED;
            }

            // assign grid positions to the four dots
            // surrounding the centre dot
            centre_dots[0].grid_x = -1;
            centre_dots[0].grid_y = 1;

            centre_dots[1].grid_x = 0;
            centre_dots[1].grid_y = 1;

            centre_dots[2].grid_x = 0;
            centre_dots[2].grid_y = 0;

            centre_dots[3].grid_x = -1;
            centre_dots[3].grid_y = 0;

            int dots_assigned = 4;

            // recursively assign grid positions to dots
            for (int i = 0; i < centre_dots.Count; i++)
                ApplyGrid(centre_dots[i], UNASSIGNED, ref dots_assigned);

            //Console.WriteLine(dots_assigned.ToString() + " dots assigned grid coordinates");
        }

        /// <summary>
        /// applies grid coordinates to the given connected dots
        /// </summary>
        /// <param name="current_dot">the current dot of interest</param>
        /// <param name="unassigned_value"></param>
        /// <param name="dots_assigned"></param>
        private static void ApplyGrid(CalibrationDot current_dot, 
                                      int unassigned_value, ref int dots_assigned)
        {
            for (int i = 0; i < current_dot.Links.Count; i++)
            {
                CalibrationLink link = (CalibrationLink)current_dot.Links[i];
                CalibrationDot dot = (CalibrationDot)link.From;
                if (dot.grid_x == unassigned_value)
                {
                    if (link.horizontal)
                    {
                        if (dot.x < current_dot.x)
                            dot.grid_x = current_dot.grid_x - 1;
                        else
                            dot.grid_x = current_dot.grid_x + 1;
                        dot.grid_y = current_dot.grid_y;
                    }
                    else
                    {
                        dot.grid_x = current_dot.grid_x;
                        if (dot.y > current_dot.y)
                            dot.grid_y = current_dot.grid_y - 1;
                        else
                            dot.grid_y = current_dot.grid_y + 1;
                    }
                    dots_assigned++;
                    ApplyGrid(dot, unassigned_value, ref dots_assigned);
                }
            }
        }
        
        /// <summary>
        /// shows the grid fitted to the calibration dots
        /// </summary>
        /// <param name="bmp">raw image</param>
        /// <param name="dots">detected dots on the grid</param>
        /// <param name="search_regions">search regions around the dots</param>
        private static void ShowLinkedDots(Bitmap bmp,
                                           hypergraph dots,
                                           List<CalibrationDot> search_regions,
                                           ref Bitmap output_bmp)
        {
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);

            if (search_regions != null)
            {
                for (int i = 0; i < search_regions.Count; i++)
                {
                    CalibrationDot dot = (CalibrationDot)search_regions[i];
                    drawing.drawCircle(img, bmp.Width, bmp.Height, (float)dot.x, (float)dot.y, (float)dot.radius, 255, 255, 0, 0);
                }
            }

            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                drawing.drawCircle(img, bmp.Width, bmp.Height, (float)dot.x, (float)dot.y, dot.radius, 0, 255, 0, 0);
            }

            for (int i = 0; i < dots.Links.Count; i++)
            {
                CalibrationDot from_dot = (CalibrationDot)dots.Links[i].From;
                CalibrationDot to_dot = (CalibrationDot)dots.Links[i].To;
                drawing.drawLine(img, bmp.Width, bmp.Height, (int)from_dot.x, (int)from_dot.y, (int)to_dot.x, (int)to_dot.y, 255, 0, 0, 0, false);
            }

            if (output_bmp == null)
                output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
        }

        /// <summary>
        /// shows the difference between an ideal grid and the actually detected grid
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="grid"></param>
        /// <param name="output_bmp"></param>
        private static void ShowIdealGridDiff(Bitmap bmp,
                                              CalibrationDot[,] grid,
                                              ref Bitmap output_bmp)
        {
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);
            
            if (grid != null)
            {
                for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                    {
                        if (grid[grid_x, grid_y] != null)
                        {
                            drawing.drawCross(img, bmp.Width, bmp.Height, 
                                             (int)grid[grid_x, grid_y].x, (int)grid[grid_x, grid_y].y,
                                             2, 255,0,0,0);
                                             
                            drawing.drawLine(img, bmp.Width, bmp.Height,
                                             (int)grid[grid_x, grid_y].x, (int)grid[grid_x, grid_y].y,
                                             (int)grid[grid_x, grid_y].rectified_x, (int)grid[grid_x, grid_y].rectified_y,
                                             255,255,0,0,false);
                        }
                    }
                }
            }
            
            if (output_bmp == null)
                output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
        }

        private static void ShowIdealGrid(Bitmap bmp,
                                          grid2D ideal_grid,
                                          CalibrationDot[,] grid,
                                          ref Bitmap output_bmp)
        {
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);
            
            if (grid != null)
            {
                for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                    {
                        if (grid[grid_x, grid_y] != null)
                        {
                            drawing.drawCross(img, bmp.Width, bmp.Height,
                                              (int)grid[grid_x, grid_y].x, (int)grid[grid_x, grid_y].y,
                                              1, 0,255,0,0);
                        }
                    }
                }
            }
            if (ideal_grid != null) ideal_grid.ShowIntercepts(img, bmp.Width, bmp.Height, 255,0,0, 3, 0);
            
            if (output_bmp == null)
                output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
        }


        private static void ShowRectifiedImage(Bitmap bmp,
                                               float centre_of_distortion_x,
                                               float centre_of_distortion_y,
                                               polynomial distortion_curve,
                                               ref Bitmap rectified)
        {
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);
            byte[] img_rectified = (byte[])img.Clone();
            
            if (distortion_curve != null)
            {
                int[] calibration_map = null;
                int[,,] calibration_map_inverse = null;
                
                updateCalibrationMap(
                    bmp.Width, bmp.Height, distortion_curve,
                    1, 0, 
                    centre_of_distortion_x,centre_of_distortion_y,
				    0, 0,
                    ref calibration_map, ref calibration_map_inverse);
                    
                int n = 0;
                for (int i = 0; i < img.Length; i+=3, n++)
                {
                    int index = calibration_map[n]*3;
                    for (int col = 0; col < 3; col++)
                        img_rectified[i+col] = img[index+col];
                }
            }
                        
            if (rectified == null)
                rectified = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img_rectified, rectified);
        }

        #region "camera offsets calibration"
        
        /// <summary>
        /// Calculate offsets from observing a distant object
        /// Here the rays of light from the object to the camera ideally
        /// should be parallel, but in practice there is always some
        /// small missalignment which is measured here as an x and y offset in pixels
        /// </summary>
        /// <param name="rectified_left">left rectified image</param>
        /// <param name="rectified_right">right rectified image</param>
        /// <param name="best_offset_x">returned x offset in pixels</param>
        /// <param name="best_offset_y">returned y offset in pixels</param>
        /// <param name="rotation">relative rotation of the right image to the left in radians</param>
        public static bool UpdateOffsets(Bitmap rectified_left, Bitmap rectified_right,
                                         ref float best_offset_x,
                                         ref float best_offset_y,
                                         ref float rotation)
        {
			bool success = false;
			
            byte[][] rectified = new byte[2][];
            rectified[0] = new byte[rectified_left.Width * rectified_left.Height * 3];
            rectified[1] = new byte[rectified_right.Width * rectified_right.Height * 3];
            
			int tries = 0;
			while ((!success) && (tries++ < 10))
			{
	            bool r1 = BitmapArrayConversions.updatebitmap(rectified_left, rectified[0]);
	            bool r2 = BitmapArrayConversions.updatebitmap(rectified_right, rectified[1]);
				
				if (r1 && r2)
				{     
					success = true;
		            // search window to compare between the two images
		            int tx = rectified_right.Width * 10 / 100;
		            int bx = rectified_right.Width - 1 - tx;
		            int ty = rectified_right.Height * 10 / 100;
		            int by = rectified_right.Height - 1 - ty;
		            
		            int w = rectified_left.Width;
		            int attention_box_width = bx - tx;
		            int attention_box_height = by - ty;
		            
		            int horizontal_search = (rectified_right.Width - 1 - bx);
		            int vertical_search = (rectified_right.Height - 1 - by) / 2;
		            int step_size = attention_box_width / 50;
		            if (step_size < 1) step_size = 1;

		            ulong min_diff = ulong.MaxValue;          
		            for (int offset_y = -vertical_search; offset_y <= vertical_search; offset_y++)
		            {
		                for (int offset_x = -horizontal_search; offset_x <= horizontal_search; offset_x++)
		                {
		                    ulong diff = 0;
		                    for (int x_right = tx + offset_x; x_right < tx + offset_x + attention_box_width; x_right += step_size)
		                    {
		                        int x_left = x_right - offset_x;
		                        for (int y_right = ty + offset_y; y_right < ty + offset_y + attention_box_height; y_right += step_size)
		                        {
		                            int y_left = y_right - offset_y;
		                            
		                            int n1 = ((y_left * w) + x_left) * 3;
		                            int n2 = ((y_right * w) + x_right) * 3;
		                            
		                            for (int col = 0; col < 3; col++)
		                            {
		                                int col_diff = rectified[1][n2+col] - rectified[0][n1+col];
		                                diff += (ulong)(col_diff * col_diff);
		                            }
		                        }
		                    }
		                    
		                    if (diff < min_diff)
		                    {
		                        min_diff = diff;
		                        best_offset_x = offset_x;
		                        best_offset_y = offset_y;
		                    }
		                }
		            }
				}
			}
			return(success);
        }
        
        #endregion
        
        #region "creating calibration lookup tables, used to rectify whole images"

        private static void rotatePoint(double x, double y, double ang,
                                        ref double rotated_x, ref double rotated_y)
        {
            double hyp;
            rotated_x = x;
            rotated_y = y;

            if (ang != 0)
            {
                hyp = Math.Sqrt((x * x) + (y * y));
                if (hyp > 0)
                {
                    double rot_angle = Math.Acos(y / hyp);
                    if (x < 0) rot_angle = (Math.PI * 2) - rot_angle;
                    double new_angle = ang + rot_angle;
                    rotated_x = hyp * Math.Sin(new_angle);
                    rotated_y = hyp * Math.Cos(new_angle);
                }
            }
        }
		
        /// <summary>
        /// update the calibration lookup table, which maps pixels
        /// in the rectified image into the original image
        /// </summary>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <param name="curve">polynomial curve describing the lens distortion</param>
        /// <param name="radial_scale">scaling factor</param>
        /// <param name="rotation">rotation in radians</param>
        /// <param name="centre_of_distortion_x">x centre of radial distortion</param>
        /// <param name="centre_of_distortion_y">y centre of radial distortion</param>
        /// <param name="offset_x">x offset</param>
        /// <param name="offset_y">y offset</param>
        /// <param name="calibration_map">lookup table which converts raw image to rectified image</param>
        /// <param name="calibration_map_inverse">lookup table which converts rectified image to raw image</param>
        public static void updateCalibrationMap(
		    int image_width,
            int image_height,
            polynomial curve,
            float radial_scale,
            float rotation,
            float centre_of_distortion_x,
            float centre_of_distortion_y,
		    float offset_x,
		    float offset_y,
            ref int[] calibration_map,
            ref int[, ,] calibration_map_inverse)
        {
            int half_width = image_width / 2;
            int half_height = image_height / 2;
            calibration_map = new int[image_width * image_height];
            calibration_map_inverse = new int[image_width, image_height, 2];
            for (int x = 0; x < image_width; x++)
            {
                float dx = x - centre_of_distortion_x;

                for (int y = 0; y < image_height; y++)
                {
                    float dy = y - centre_of_distortion_y;

                    float radial_dist_rectified = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (radial_dist_rectified >= 0.01f)
                    {
                        double radial_dist_original = curve.RegVal(radial_dist_rectified);
                        if (radial_dist_original > 0)
                        {
                            double ratio = radial_dist_original / radial_dist_rectified;
                            float x2 = (float)Math.Round(centre_of_distortion_x + (dx * ratio));
                            x2 = ((x2 + offset_x) - (image_width / 2)) * radial_scale;
                            float y2 = (float)Math.Round(centre_of_distortion_y + (dy * ratio));
                            y2 = ((y2 + offset_y) - (image_height / 2)) * radial_scale;

                            // apply rotation
                            double x3 = x2, y3 = y2;
                            rotatePoint(x2, y2, -rotation, ref x3, ref y3);

                            x3 += half_width;
                            y3 += half_height;

                            if (((int)x3 > -1) && ((int)x3 < image_width) &&
                                ((int)y3 > -1) && ((int)y3 < image_height))
                            {
                                int n = (y * image_width) + x;
                                int n2 = ((int)y3 * image_width) + (int)x3;

                                calibration_map[n] = n2;
                                calibration_map_inverse[(int)x3, (int)y3, 0] = x;
                                calibration_map_inverse[(int)x3, (int)y3, 1] = y;
                            }
                        }
                    }
                }
            }
        }

        #endregion
        


        /// <summary>
        /// puts the detected dots into a grid for easy lookup
        /// </summary>
        /// <param name="dots">detected calibration dots</param>
        /// <returns>grid object</returns>
        private static CalibrationDot[,] CreateGrid(hypergraph dots)
        {
            int grid_tx = 9999, grid_ty = 9999;
            int grid_bx = -9999, grid_by = -9999;
            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                if (Math.Abs(dot.grid_x) < 50)
                {
                    if (dot.grid_x < grid_tx) grid_tx = dot.grid_x;
                    if (dot.grid_y < grid_ty) grid_ty = dot.grid_y;
                    if (dot.grid_x > grid_bx) grid_bx = dot.grid_x;
                    if (dot.grid_y > grid_by) grid_by = dot.grid_y;
                }
            }

            CalibrationDot[,] grid = null;
            if (grid_bx > grid_tx + 1)
            {
                grid = new CalibrationDot[grid_bx - grid_tx + 1, grid_by - grid_ty + 1];

                for (int i = 0; i < dots.Nodes.Count; i++)
                {
                    CalibrationDot dot = (CalibrationDot)dots.Nodes[i];
                    if ((!dot.centre) && (Math.Abs(dot.grid_x) < 50))
                    {
                        grid[dot.grid_x - grid_tx, dot.grid_y - grid_ty] = dot;
                    }
                }
            }

            return (grid);
        }

        /// <summary>
        /// returns an ideal grid with perfectly regular spacing
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <returns>
        /// </returns>
        private static grid2D GetIdealGrid(CalibrationDot[,] grid,
                                           int image_width, int image_height)
        {
            grid2D ideal_grid = null;
            float ideal_spacing = 0;
            double centre_x = image_width / 2;
            double centre_y = image_height / 2;            
            double min_dist = double.MaxValue;
            
            int grid_cx=0, grid_cy=0;
            
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null)
                    {
                        double dx = grid[x, y].x - centre_x;
                        double dy = grid[x, y].y - centre_y;
                        double dist = dx*dx + dy*dy;
                        if (dist < min_dist)
                        {
                            min_dist = dist;
                            grid_cx = x;
                            grid_cy = y;
                        }
                    }
                }
            }
            
            if (grid_cx > 0)
            {
                int[] orientation_histogram = new int[361];
                List<double>[] orientations = new List<double>[361];
                double average_dist = 0;
                int hits = 0;
                
                int local_search = 2;
                for (int x = grid_cx - local_search; x <= grid_cx + local_search; x++)
                {
                    if ((x >= 0) && (x < grid.GetLength(0)-1))
                    {
                        for (int y = grid_cy - local_search; y <= grid_cy + local_search; y++)
                        {
                            if ((y >= 1) && (y < grid.GetLength(1)-1))
                            {
                                if (grid[x, y] != null)
                                {
                                    for (int i = 0; i < grid[x, y].Links.Count; i++)
                                    {
                                        CalibrationLink link = (CalibrationLink)grid[x, y].Links[i];
                                        CalibrationDot from_dot = (CalibrationDot)link.From;
                                        
                                        double dx = grid[x, y].x - from_dot.x;
                                        double dy = grid[x, y].y - from_dot.y;
                                        double dist = Math.Sqrt(dx*dx + dy*dy);
                                        
                                        if (dist > 0)
                                        {
                                            double orientation = Math.Asin(dx / dist);
                                            if (orientation < 0) orientation += Math.PI;
                                            if (dy < 0) orientation = (Math.PI * 2) - orientation;
                                            orientation = orientation / Math.PI * 180;
                                            int bucket = (int)orientation;
                                            orientation_histogram[bucket]++;
                                            if (orientations[bucket] == null) orientations[bucket] = new List<double>();
                                            orientations[bucket].Add(orientation);
                                        
                                            average_dist += Math.Sqrt(dx*dx + dy*dy);
                                            hits++;
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                }
                if (hits > 0) average_dist = average_dist / hits;
                ideal_spacing = (float)average_dist;
                
                int max_orientation_response = 0;
                int best_ang = 0;
                for (int ang = 0; ang < 90; ang++)
                {
                    int response = orientation_histogram[ang] +
                                   orientation_histogram[ang + 90] + 
                                   orientation_histogram[ang + 180];
                    if (response > max_orientation_response)
                    {
                        max_orientation_response = response;
                        best_ang = ang;
                    }
                }
                double average_orientation = 0;
                hits = 0;
                for (int offset = 0; offset < 3; offset++)
                {
                    if (orientations[best_ang + offset] != null)
                    {
                        for (int ang = 0; ang < orientations[best_ang + offset].Count; ang++)
                        {
                            average_orientation += orientations[best_ang + offset][ang] - (Math.PI*offset/2);
                            hits++;
                        }
                    }
                }
                if (hits > 0)
                {
                    average_orientation /= hits;
                    float pattern_orientation = (float)average_orientation;
                    
                    float offset_x = (float)grid[grid_cx, grid_cy].x;
                    float offset_y = (float)grid[grid_cx, grid_cy].y;
                    float r = ideal_spacing * dots_across;
                    
                    polygon2D perimeter = new polygon2D();
                    perimeter.Add(-r + offset_x, -r + offset_y);
                    perimeter.Add(r + offset_x, -r + offset_y);
                    perimeter.Add(r + offset_x, r + offset_y);
                    perimeter.Add(-r + offset_x, r + offset_y);
                    perimeter.rotate((float)average_orientation / 180 * (float)Math.PI, offset_x, offset_y);
                    ideal_grid = new grid2D(dots_across*2, dots_across*2, perimeter, 0, false);
                    
                    int grid_width = grid.GetLength(0);
                    int grid_height = grid.GetLength(1);
                    int idx = 0;
                    int xx=0, yy=0;
                    float ideal_x, ideal_y;
       
                    for (int orient = 0; orient < 8; orient++)
                    {
                        for (int x = 0; x < grid_width; x++)
                        {
                            for (int y = 0; y < grid_height; y++)
                            {
                                if (grid[x, y] != null)
                                {
                                    switch(orient)
                                    {
                                        case 0:
                                        {
                                            xx = (x - grid_cx) + dots_across;
                                            yy = (y - grid_cy) + dots_across;
                                            break;
                                        }
                                        case 1:
                                        {
                                            xx = (x - grid_cx) + dots_across;
                                            yy = (grid_cy - y) + dots_across;
                                            break;
                                        }
                                        case 2:
                                        {
                                            xx = (grid_cx - x) + dots_across;
                                            yy = (y - grid_cy) + dots_across;
                                            break;
                                        }
                                        case 3:
                                        {
                                            xx = (grid_cx - x) + dots_across;
                                            yy = (grid_cy - y) + dots_across;
                                            break;
                                        }
                                        case 4:
                                        {
                                            xx = (y - grid_cy) + dots_across;
                                            yy = (x - grid_cx) + dots_across;
                                            break;
                                        }
                                        case 5:
                                        {
                                            xx = (y - grid_cy) + dots_across;
                                            yy = (grid_cx - x) + dots_across;
                                            break;
                                        }
                                        case 6:
                                        {
                                            xx = (grid_cy - y) + dots_across;
                                            yy = (x - grid_cx) + dots_across;
                                            break;
                                        }
                                        case 7:
                                        {
                                            xx = (grid_cy - y) + dots_across;
                                            yy = (grid_cx - x) + dots_across;
                                            break;
                                        }
                                    }
                                    
                                    if ((xx >= 0) && (xx < ideal_grid.cell.Length) &&
                                        (yy >= 0) && (yy < ideal_grid.cell[0].Length))
                                    {
                                        ideal_x = ideal_grid.cell[xx][yy].perimeter.x_points[idx];
                                        ideal_y = ideal_grid.cell[xx][yy].perimeter.y_points[idx];
                                    
                                        if (orient == 0)
                                        {
                                            grid[x, y].rectified_x = ideal_x;
                                            grid[x, y].rectified_y = ideal_y;
                                        }
                                        else
                                        {
                                            double diff_x1 = grid[x, y].rectified_x - grid[x, y].x;
                                            double diff_y1 = grid[x, y].rectified_y - grid[x, y].y;
                                            double dist1 = diff_x1*diff_x1 + diff_y1*diff_y1;

                                            double diff_x2 = ideal_x - grid[x, y].x;
                                            double diff_y2 = ideal_y - grid[x, y].y;
                                            double dist2 = diff_x2*diff_x2 + diff_y2*diff_y2;
                                            
                                            if (dist2 < dist1)
                                            {
                                                grid[x, y].rectified_x = ideal_x;
                                                grid[x, y].rectified_y = ideal_y;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
            }
            
            return(ideal_grid);
        }
        
        /// <summary>
        /// is the given set of connected dots valid ?
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <param name="tollerance_degrees"></param>
        /// <returns>
        /// </returns>
        private static bool ValidGrid(hypergraph graph,
                                      int image_width, int image_height,
                                      int tollerance_degrees)
        {
            bool is_valid = true;
            
            int tx = image_width / 3;
            int bx = image_width - tx;
            int ty = image_height / 3;
            int by = image_height - tx;
            
            int i = 0;
            while ((i < graph.Nodes.Count) && (is_valid))
            {
                CalibrationDot n = (CalibrationDot)graph.Nodes[i];
                if ((n.x > tx) && (n.x < bx) &&
                    (n.y > ty) && (n.y < by))
                {
                    int j = 0;
                    while ((j < n.Links.Count) && (is_valid))
                    {
                        CalibrationLink link = (CalibrationLink)n.Links[j];
                        double dx = ((CalibrationDot)link.From).x - n.x;
                        double dy = ((CalibrationDot)link.From).y - n.y;
                        double dist = Math.Sqrt(dx*dx + dy*dy);
                        if (dist > 0)
                        {
                            double angle = Math.Asin(dx / dist);
                            if (dy < 0) angle = (Math.PI * 2) - angle;
                            angle = angle / Math.PI * 180;
                            if (angle < 0) angle += 360;
                            
                            bool within_tollerance = false;
                            if (angle > 360 - tollerance_degrees) within_tollerance = true;
                            if (angle < tollerance_degrees) within_tollerance = true;
                            if ((angle > 180 - tollerance_degrees) &&
                                (angle < 180 + tollerance_degrees)) within_tollerance = true;
                            if ((angle > 90 - tollerance_degrees) &&
                                (angle < 90 + tollerance_degrees)) within_tollerance = true;
                            if ((angle > 270 - tollerance_degrees) &&
                                (angle < 270 + tollerance_degrees)) within_tollerance = true;
                                
                            is_valid = within_tollerance;
                        }
                        j++;
                    }
                }
                i++;
            }
            
            return(is_valid);
        }
        
                
        public static hypergraph DetectDots(Bitmap bmp, ref EdgeDetectorCanny edge_detector,
                                            CalibrationSurvey survey,
                                            ref Bitmap detected_dots,
                                            ref Bitmap linked_dots,
                                            ref Bitmap grd,
                                            ref Bitmap grid_diff,
                                            ref Bitmap rectified)
        {
            const int minimum_links = (dots_across * (dots_across/2)) / 2;
            hypergraph dots = null;
            DetectEdges(bmp, ref edge_detector, ref dots, ref detected_dots);
            
            if (dots != null)
            {
                // find the dots around the red centre dot
                List<CalibrationDot> centre_dots = null;
                polygon2D centre_square = GetCentreSquare(dots, ref centre_dots);
                
                if (centre_square != null)
                {
                    if (centre_square.x_points.Count == 4)
                    {
                        // find the orientation of the centre square
                        float angle = geometry.threePointAngle(
                            (float)centre_dots[1].x, 0,
                            (float)centre_dots[1].x, (float)centre_dots[1].y,
                            (float)centre_dots[2].x, (float)centre_dots[2].y);
                        angle  = angle / (float)Math.PI * 180;
                        
                        if (angle > 160)
                        {
                            angle = geometry.threePointAngle(
                                (float)centre_dots[2].x, 0,
                                (float)centre_dots[2].x, (float)centre_dots[2].y,
                                (float)centre_dots[3].x, (float)centre_dots[3].y);
                            angle  = angle / (float)Math.PI * 180;
                        
                            if ((angle >= 70) && (angle < 120))
                            {
                                // link dots together
                                List<CalibrationDot> search_regions = new List<CalibrationDot>();
                                double horizontal_dx = centre_dots[1].x - centre_dots[0].x;
                                double horizontal_dy = centre_dots[1].y - centre_dots[0].y;
                                double vertical_dx = centre_dots[3].x - centre_dots[0].x;
                                double vertical_dy = centre_dots[3].y - centre_dots[0].y;
                                for (int i = 0; i < centre_dots.Count; i++)
                                    LinkDots(dots, centre_dots[i], horizontal_dx, horizontal_dy, vertical_dx, vertical_dy, 0, search_regions);

                                if (dots.Links.Count > minimum_links)
                                {
                                    int tollerance_degrees = 10;
                                    if (ValidGrid(dots, bmp.Width, bmp.Height, tollerance_degrees))
                                    {                                
                                        ShowLinkedDots(bmp, dots, search_regions, ref linked_dots);
                                    
                                        // assign a grid coordinate to each dot
                                        ApplyGrid(dots, centre_dots);
                                        
                                        // put the dots into a 2D grid for convenient lookup
                                        CalibrationDot[,] grid = CreateGrid(dots);

                                        if (grid != null)
                                        {
                                            grid2D ideal_grid = GetIdealGrid(grid, bmp.Width, bmp.Height);
                                            
                                            ShowIdealGrid(bmp, ideal_grid, grid, ref grd);
                                            ShowIdealGridDiff(bmp, grid, ref grid_diff);
                                            
                                            survey.Update(bmp.Width, bmp.Height, grid);
                                            
                                            ShowRectifiedImage(bmp, survey.centre_of_distortion_x, survey.centre_of_distortion_y, survey.best_fit_curve, ref rectified);
                                            
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return(dots);
        }
    
        #region "creating a calibration dot pattern"    
        
        private static void DrawDot(byte[] image_data, int image_width, int image_height,
                                    float x, float y, float radius,
                                    int r, int g, int b)
        {
            int tx = (int)(x - radius);
            int bx = (int)(x + radius);
            int ty = (int)(y - radius);
            int by = (int)(y + radius);
            
            if (tx < 0) tx = 0;
            if (ty < 0) ty = 0;
            if (bx >= image_width) bx = image_width - 1;
            if (by >= image_height) by = image_height -1;
            
            float radius_sqr = radius * radius;
            
            for (int xx = tx; xx <= bx; xx++)
            {
                float dx = x - xx;
                dx *= dx;
                
                for (int yy = ty; yy <= by; yy++)
                {
                    float dy = y - yy;
                    dy *= dy;
                    
                    if (dx + dy < radius_sqr)
                    {
                        int n = ((yy * image_width) + xx) * 3;
                        image_data[n++] = (byte)b;
                        image_data[n++] = (byte)g;
                        image_data[n] = (byte)r;
                    }
                }
            }
        }
        
        public static Bitmap CreateDotPattern(int image_width, int image_height,
                                              int dots_across, int dot_radius_percent)
        {
            // create the image
            Bitmap calibration_pattern = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte[] image_data = new byte[image_width * image_height * 3];
            
            // set all pixels to white
            for (int i = image_data.Length-1; i >= 0; i--) image_data[i] = 255;
            
            float dot_spacing = image_width / dots_across;
            int dots_down = (int)(image_height / dot_spacing);

            // calculate the radius of each dot in pixels
            float radius = dot_spacing * 0.5f * dot_radius_percent / 100; 
            
            // draw black dots
            float offset = dot_spacing / 2;
            for (int grid_x = 0; grid_x < dots_across; grid_x++)
            {
                float x = (grid_x * dot_spacing) + offset;
                for (int grid_y = 0; grid_y < dots_down; grid_y++)
                {
                    float y = (grid_y * dot_spacing) + offset;
                    DrawDot(image_data, image_width, image_height, x, y, radius, 0, 0, 0);
                }
            }
            
            // draw the red centre dot
            float centre_dot_x = dots_across / 2 * dot_spacing;
            float centre_dot_y = dots_down / 2 * dot_spacing;
            DrawDot(image_data, image_width, image_height, centre_dot_x, centre_dot_y, radius, 255, 0, 0);
            
            // insert the data into a bitmap object
            BitmapArrayConversions.updatebitmap_unsafe(image_data, calibration_pattern);
            
            return(calibration_pattern);
        }
        
        #endregion
               
    }
}
