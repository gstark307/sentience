/*
    Fishfood stereo camera calibration utility
    Copyright (C) 2000-2008 Bob Mottram
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace sentience.calibration
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Test();
            
            
            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            string help = commandline.GetParameterValue("help", parameters);
            if (help != "")
            {
                ShowHelp();
            }
            else
            {
                string directory = commandline.GetParameterValue("dir", parameters);
                if (directory == "")
                {
                    Console.WriteLine("Please specify a directory for the calibration files");
                }
                else
                {
                    if (!Directory.Exists(directory))
                    {
                        Console.WriteLine("The directory " + directory + " was not found");
                    }
                    else
                    {
                        string baseline_str = commandline.GetParameterValue("baseline", parameters);
                        if (baseline_str == "")
                        {
                            Console.WriteLine("Please specify a baseline distance for the camera separation in millimetres");
                        }
                        else
                        {
                            int baseline_mm = Convert.ToInt32(baseline_str);

                            string dotdist_str = commandline.GetParameterValue("dotdist", parameters);
                            if (dotdist_str == "")
                            {
                                Console.WriteLine("Please specify the horizontal distance to the calibration pattern dot in millimetres");
                            }
                            else
                            {
                                int dotdist_mm = Convert.ToInt32(dotdist_str);

                                string height_str = commandline.GetParameterValue("height", parameters);
                                if (height_str == "")
                                {
                                    Console.WriteLine("Please specify the height above the calibration pattern in millimetres");
                                }
                                else
                                {
                                    int height_mm = Convert.ToInt32(height_str);
                                    
                                    string fov_str = commandline.GetParameterValue("fov", parameters);
                                    if (fov_str == "")
                                    {
                                        Console.WriteLine("Please specify the camera horizontal field of view in degrees");
                                    }
                                    else
                                    {
                                        float fov_degrees = Convert.ToSingle(fov_str);
                                    
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        #region "calibration"
        
        private static hypergraph Detect(string filename)
        {
            int image_width = 0, image_height = 0;
            hypergraph dots = DetectDots(filename, ref image_width, ref image_height);

            List<calibrationDot> centre_dots = null;
            polygon2D centre_square = GetCentreSquare(dots, ref centre_dots);

            ShowSquare(filename, centre_square, "centre_square.jpg");

            List<calibrationDot> search_regions = new List<calibrationDot>();
            float horizontal_dx = centre_dots[1].x - centre_dots[0].x;
            float horizontal_dy = centre_dots[1].y - centre_dots[0].y;
            float vertical_dx = centre_dots[3].x - centre_dots[0].x;
            float vertical_dy = centre_dots[3].y - centre_dots[0].y;
            for (int i = 0; i < centre_dots.Count; i++)
                LinkDots(dots, centre_dots[i], horizontal_dx, horizontal_dy, vertical_dx, vertical_dy, 0, search_regions);

            Console.WriteLine(dots.Links.Count.ToString() + " links created");

            ApplyGrid(dots, centre_dots);

            calibrationDot[,] grid = CreateGrid(dots);
            grid2D overlay_grid = OverlayIdealGrid(grid);
            if (overlay_grid != null)
            {
                List<List<float>> lines = CreateLines(dots, grid);

                polynomial lens_distortion_curve = null;
                calibrationDot centre_of_distortion = null;
                List<List<float>> best_rectified_lines = null;
                DetectLensDistortion(image_width, image_height, grid, lines,
                                     ref lens_distortion_curve, ref centre_of_distortion,
                                     ref best_rectified_lines);



                if (best_rectified_lines != null)
                {
                    ShowLines(filename, lines, "lines.jpg");
                    ShowLines(filename, best_rectified_lines, "rectified_lines.jpg");
                }
            }

            ShowOverlayGrid(filename, overlay_grid, "grid_overlay.jpg");
            ShowGrid(filename, dots, search_regions, "grid.jpg");
            ShowGridCoordinates(filename, dots, "coordinates.jpg");
            return (dots);
        }

        private static void Calibrate(string directory,
                               int baseline_mm,
                               int dotdist_mm,
                               int height_mm,
                               float fov_degrees,
                               string file_extension)
        {
            string[] filename = Directory.GetFiles(directory, "*." + file_extension);
            if (filename != null)
            {
                // two lists to store filenames for camera 0 and camera 1
                List<string>[] image_filename = new List<string>[2];
                image_filename[0] = new List<string>();
                image_filename[1] = new List<string>();
                
                // populate the lists
                for (int i = 0; i < filename.Length; i++)
                {
                    if (filename[i].StartsWith("raw0"))
                        image_filename[0].Add(filename[i]);
                    if (filename[i].StartsWith("raw1"))
                        image_filename[1].Add(filename[i]);
                }
                
                if (image_filename[0].Count == 0)
                {
                    Console.WriteLine("Did not find any calibration files.  Do they begin with raw0 or raw1 ?");
                }
                else
                {
                    if (image_filename[0].Count != image_filename[1].Count)
                    {
                        Console.WriteLine("There must be the same number of images from camera 0 and camera 1");
                    }
                    else
                    {
                        // find dots within the images
                        hypergraph[,] dots = new hypergraph[2, image_filename[0].Count];
                        for (int cam = 0; cam < 2; cam++)
                        {
                            for (int i = 0; i < image_filename[cam].Count; i++)
                            {
                                dots[cam, i] = Detect(image_filename[cam][i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No calibration " + file_extension + " images were found");
            }
        }

        #region "creating calibration lookup tables, used to rectify whole images"

        protected static void rotatePoint(float x, float y, float ang,
                                          ref float rotated_x, ref float rotated_y)
        {
            float hyp;
            rotated_x = x;
            rotated_y = y;

            if (ang != 0)
            {
                hyp = (float)Math.Sqrt((x * x) + (y * y));
                if (hyp > 0)
                {
                    float rot_angle = (float)Math.Acos(y / hyp);
                    if (x < 0) rot_angle = (float)(Math.PI * 2) - rot_angle;
                    float new_angle = ang + rot_angle;
                    rotated_x = hyp * (float)Math.Sin(new_angle);
                    rotated_y = hyp * (float)Math.Cos(new_angle);
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
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        /// <param name="centre_of_distortion_x"></param>
        /// <param name="centre_of_distortion_y"></param>
        /// <param name="calibration_map"></param>
        /// <param name="calibration_map_inverse"></param>
        private static void updateCalibrationMap(int image_width,
                                                 int image_height,
                                                 polynomial curve,
                                                 float scale,
                                                 float rotation,
                                                 float centre_of_distortion_x,
                                                 float centre_of_distortion_y,
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
                        float radial_dist_original = curve.RegVal(radial_dist_rectified);
                        if (radial_dist_original > 0)
                        {
                            float ratio = radial_dist_original / radial_dist_rectified;
                            float x2 = (float)Math.Round(centre_of_distortion_x + (dx * ratio));
                            x2 = (x2 - (image_width / 2)) * scale;
                            float y2 = (float)Math.Round(centre_of_distortion_y + (dy * ratio));
                            y2 = (y2 - (image_height / 2)) * scale;

                            // apply rotation
                            float x3 = x2, y3 = y2;
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

        #region "making grids and lines out of the detected calibration dots"

        /// <summary>
        /// extracts lines from the given detected dots
        /// </summary>
        /// <param name="dots"></param>
        /// <returns></returns>
        private static List<List<float>> CreateLines(hypergraph dots,
                                                     calibrationDot[,] grid)
        {
            List<List<float>> lines = new List<List<float>>();            
            if (grid != null)
            {
                for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                {
                    List<float> line = new List<float>();
                    for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                    {
                        if (grid[grid_x, grid_y] != null)
                        {
                            line.Add(grid[grid_x, grid_y].x);
                            line.Add(grid[grid_x, grid_y].y);
                        }
                    }
                    if (line.Count > 6)
                    {
                        lines.Add(line);
                    }
                }
                for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                {
                    List<float> line = new List<float>();
                    for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                    {
                        if (grid[grid_x, grid_y] != null)
                        {
                            line.Add(grid[grid_x, grid_y].x);
                            line.Add(grid[grid_x, grid_y].y);
                        }
                    }
                    if (line.Count > 6)
                    {
                        lines.Add(line);
                    }
                }
            }
            return (lines);
        }


        /// <summary>
        /// puts the given dots into a grid for easy lookup
        /// </summary>
        /// <param name="dots">detected calibration dots</param>
        /// <returns>grid object</returns>
        private static calibrationDot[,] CreateGrid(hypergraph dots)
        {
            int grid_tx = 9999, grid_ty = 9999;
            int grid_bx = -9999, grid_by = -9999;
            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                calibrationDot dot = (calibrationDot)dots.Nodes[i];
                if (Math.Abs(dot.grid_x) < 50)
                {
                    if (dot.grid_x < grid_tx) grid_tx = dot.grid_x;
                    if (dot.grid_y < grid_ty) grid_ty = dot.grid_y;
                    if (dot.grid_x > grid_bx) grid_bx = dot.grid_x;
                    if (dot.grid_y > grid_by) grid_by = dot.grid_y;
                }
            }

            calibrationDot[,] grid = null;
            if (grid_bx > grid_tx + 1)
            {
                grid = new calibrationDot[grid_bx - grid_tx+1, grid_by - grid_ty+1];

                for (int i = 0; i < dots.Nodes.Count; i++)
                {
                    calibrationDot dot = (calibrationDot)dots.Nodes[i];
                    if ((!dot.centre) && (Math.Abs(dot.grid_x) < 50))
                    {
                        grid[dot.grid_x - grid_tx, dot.grid_y - grid_ty] = dot;
                    }
                }
            }

            return (grid);
        }

        #endregion

        #region "detecting the curvature of lines"

        /// <summary>
        /// returns the average curvature value of the given set of line segments
        /// </summary>
        /// <param name="lines">list of line segments</param>
        /// <returns>average curvature of the lines</returns>
        private static float LineCurvature(List<List<float>> lines)
        {
            float curvature = 0;

            for (int i = 0; i < lines.Count; i++)
                curvature += LineCurvature(lines[i]);

            //if (lines.Count > 0) curvature /= lines.Count;
            return (curvature);
        }

        /// <summary>
        /// returns a measure proportional to the curvature of the given line segment
        /// </summary>
        /// <param name="line">line segment as a list of coordinates</param>
        /// <returns>measure of curvature</returns>
        private static float LineCurvature(List<float> line)
        {
            float curvature = 0;

            float x0 = line[0];
            float y0 = line[1];
            float x1 = line[line.Count - 2];
            float y1 = line[line.Count - 1];

            int hits = 0;
            for (int i = 2; i < line.Count - 2; i += 2)
            {
                float x = line[i];
                float y = line[i + 1];
                float c = Math.Abs(geometry.pointDistanceFromLine(x0, y0, x1, y1, x, y));
                curvature += c*c;
                hits++;
            }
            if (hits > 0) curvature = (float)Math.Sqrt(curvature / hits);
            return (curvature);
        }

        #endregion

        #region "detecting the length of lines"

        /// <summary>
        /// returns the total length of all lines
        /// </summary>
        /// <param name="lines">list of line segments</param>
        /// <returns>total length</returns>
        private static float LineLength(List<List<float>> lines)
        {
            float length = 0;

            for (int i = 0; i < lines.Count; i++)
                length += LineLength(lines[i]);

            return (length);
        }

        /// <summary>
        /// returns the length of the given line segment
        /// </summary>
        /// <param name="line">line segment as a list of coordinates</param>
        /// <returns>line length</returns>
        private static float LineLength(List<float> line)
        {
            float length = 0;

            float prev_x = 0, prev_y = 0;
            for (int i = 0; i < line.Count; i += 2)
            {
                float x = line[i];
                float y = line[i + 1];
                if (i > 0)
                {
                    float dx = x - prev_x;
                    float dy = y - prev_y;
                    length += (float)Math.Sqrt((dx * dx) + (dy * dy));
                }
                prev_x = x;
                prev_y = y;
            }
            return (length);
        }


        #endregion

        #region "rectification"

        /// <summary>
        /// applies the given curve to the lines to produce rectified coordinates
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="distortion_curve"></param>
        /// <param name="centre_of_distortion"></param>
        /// <returns></returns>
        private static List<List<float>> RectifyLines(List<List<float>> lines,
                                                      int image_width, int image_height,
                                                      polynomial curve,
                                                      calibrationDot centre_of_distortion,
                                                      float rotation,
                                                      float scale)
        {
            List<List<float>> rectified_lines = new List<List<float>>();

            float half_width = image_width / 2;
            float half_height = image_height / 2;

            for (int i = 0; i < lines.Count; i++)
            {
                List<float> line = lines[i];
                List<float> rectified_line = new List<float>();
                for (int j = 0; j < line.Count; j += 2)
                {
                    float x = line[j];
                    float y = line[j + 1];

                    float dx = x - centre_of_distortion.x;
                    float dy = y - centre_of_distortion.y;
                    float radial_dist_rectified = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (radial_dist_rectified >= 0.01f)
                    {
                        float radial_dist_original = curve.RegVal(radial_dist_rectified);
                        if (radial_dist_original > 0)
                        {
                            float ratio = radial_dist_original / radial_dist_rectified;
                            float x2 = (float)Math.Round(centre_of_distortion.x + (dx * ratio));
                            x2 = (x2 - (image_width / 2)) * scale;
                            float y2 = (float)Math.Round(centre_of_distortion.y + (dy * ratio));
                            y2 = (y2 - (image_height / 2)) * scale;

                            // apply rotation
                            float rectified_x = x2, rectified_y = y2;
                            rotatePoint(x2, y2, -rotation, ref rectified_x, ref rectified_y);

                            rectified_x += half_width;
                            rectified_y += half_height;

                            rectified_line.Add(rectified_x);
                            rectified_line.Add(rectified_y);
                        }
                    }
                }

                if (rectified_line.Count > 6)
                    rectified_lines.Add(rectified_line);
            }

            return (rectified_lines);
        }

        #endregion

        #region "detecting lens distortion"

        private static void ScaleLines(List<List<float>> lines, float scale, 
                                       int image_width, int image_height,
                                       int centre_x, int centre_y)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                List<float> line = lines[i];
                for (int j = 0; j < line.Count; j += 2)
                {
                    float x = line[j];
                    float y = line[j + 1];
                    float dx = x - centre_x;
                    float dy = y - centre_y;
                    
                    line[j] = centre_x + (dx * scale);
                    line[j + 1] = centre_y + (dy * scale);
                }
            }
        }

        private static grid2D OverlayIdealGrid(calibrationDot[,] grid)
        {
            grid2D overlay_grid = null;
            int grid_tx = -1;
            int grid_ty = -1;
            int grid_bx = -1;
            int grid_by = -1;

            int offset_x = 0;
            int offset_y = 0;

            bool found = false;

            while ((offset_y < 3) && (!found))
            {
                offset_x = 0;
                while ((offset_x < 3) && (!found))
                {
                    grid_tx = offset_x;
                    grid_ty = offset_y;
                    grid_bx = grid.GetLength(0) - 1 - offset_x;
                    grid_by = grid.GetLength(1) - 1 - offset_y;

                    if ((grid[grid_tx, grid_ty] != null) &&
                        (grid[grid_bx, grid_ty] != null) &&
                        (grid[grid_bx, grid_by] != null) &&
                        (grid[grid_tx, grid_by] != null))
                    {
                        found = true;
                    }

                    offset_x++;
                }
                offset_y++;
            }

            if (found)
            {
                float dx, dy;
                float x0 = 0, y0 = 0, x1 = 0, y1 = 0;
                float x2 = 0, y2 = 0, x3 = 0, y3 = 0;
                for (int i = 0; i < 2; i++)
                {
                    float cx=0,cy=0;
                    int prev_grid_x = 0;
                    float prev_x = -1;
                    float prev_y = -1;
                    float spacing = 0;
                    int spacing_hits = 0;
                    float x_left = 0;
                    float y_left = 0;
                    float x_right = 0;
                    float y_right = 0;
                    int hits_left = 0;
                    int hits_right = 0;
                    int grid_y = grid_ty;
                    if (i > 0) grid_y = grid_by;
                    for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                    {
                        if (grid[grid_x, grid_y] != null)
                        {
                            if (grid_x < grid.GetLength(0) / 2)
                            {
                                x_left += grid[grid_x, grid_y].x;
                                y_left += grid[grid_x, grid_y].y;
                                hits_left++;
                            }
                            else
                            {
                                x_right += grid[grid_x, grid_y].x;
                                y_right += grid[grid_x, grid_y].y;
                                hits_right++;
                            }

                            if (prev_x > -1)
                            {
                                dx = grid[grid_x, grid_y].x - prev_x;
                                dy = grid[grid_x, grid_y].y - prev_y;
                                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                spacing += dist / (grid_x - prev_grid_x);
                                spacing_hits++;
                            }
                            prev_x = grid[grid_x, grid_y].x;
                            prev_y = grid[grid_x, grid_y].y;
                            prev_grid_x = grid_x;
                        }
                    }

                    if ((hits_left > 0) && (hits_right > 0))
                    {
                        cx = (x_left + x_right) / (hits_left + hits_right);
                        cy = (y_left + y_right) / (hits_left + hits_right);

                        x_left /= hits_left;
                        y_left /= hits_left;
                        x_right /= hits_right;
                        y_right /= hits_right;
                    }

                    if (spacing_hits > 0)
                    {
                        spacing /= spacing_hits;

                        dx = x_right - x_left;
                        dy = y_right - y_left;
                        float hyp = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (hyp > 0)
                        {
                            float angle = (float)Math.Acos(dy / hyp);
                            if (dx < 0) angle = ((float)Math.PI * 2) - angle;

                            float width = spacing * grid.GetLength(0) / 2;

                            if (i == 0)
                            {
                                x0 = cx + (width * (float)Math.Sin(angle - Math.PI));
                                y0 = cy + (width * (float)Math.Cos(angle - Math.PI));
                                x1 = cx + (width * (float)Math.Sin(angle));
                                y1 = cy + (width * (float)Math.Cos(angle));
                            }
                            else
                            {
                                x2 = cx + (width * (float)Math.Sin(angle - Math.PI));
                                y2 = cy + (width * (float)Math.Cos(angle - Math.PI));
                                x3 = cx + (width * (float)Math.Sin(angle));
                                y3 = cy + (width * (float)Math.Cos(angle));
                            }
                        }
                    }
                }

                polygon2D perimeter = new polygon2D();
                perimeter.Add(x0, y0);
                perimeter.Add(x1, y1);
                perimeter.Add(x3, y3);
                perimeter.Add(x2, y2);
                overlay_grid = new grid2D(grid_bx - grid_tx, grid_by - grid_ty, perimeter, 0, false);

                dx = 0;
                dy = 0;
                int hits = 0;
                for (int grid_x = 0; grid_x < grid.GetLength(0)-1; grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid.GetLength(1)-1; grid_y++)
                    {
                        if (grid[grid_x, grid_y] != null)
                        {
                            float intercept_x = overlay_grid.line_intercepts[grid_x, grid_y+1, 0];
                            float intercept_y = overlay_grid.line_intercepts[grid_x, grid_y+1, 1];
                            dx += grid[grid_x, grid_y].x - intercept_x;
                            dy += grid[grid_x, grid_y].y - intercept_y;
                            hits++;
                        }
                    }
                }
                if (hits > 0)
                {
                    dx /= hits;
                    dy /= hits;

                    x0 += dx;
                    y0 += dy;
                    x1 += dx;
                    y1 += dy;
                    x2 += dx;
                    y2 += dy;
                    x3 += dx;
                    y3 += dy;

                    perimeter = new polygon2D();
                    perimeter.Add(x0, y0);
                    perimeter.Add(x1, y1);
                    perimeter.Add(x3, y3);
                    perimeter.Add(x2, y2);
                    overlay_grid = new grid2D(grid_bx - grid_tx, grid_by - grid_ty, perimeter, 0, false);
                }
            }

            return (overlay_grid);
        }


        private static void DetectLensDistortion(int image_width, int image_height,
                                                 calibrationDot[,] grid,
                                                 List<List<float>> lines,
                                                 ref polynomial curve,
                                                 ref calibrationDot centre_of_distortion,
                                                 ref List<List<float>> best_rectified_lines)
        {
        }



        private static void DetectLensDistortionOld(int image_width, int image_height,
                                                 List<List<float>> lines,
                                                 ref polynomial curve,
                                                 ref calibrationDot centre_of_distortion,
                                                 ref List<List<float>> best_rectified_lines)
        {
            float line_length = LineLength(lines);
            float min_line_length = line_length * 0.8f;
            float max_line_length = line_length * 1.2f;

            int search_steps = 200;
            float[] search_range_min = new float[3];
            float[] search_range_max = new float[3];

            search_range_min[0] = 0;
            search_range_max[0] = 0;
            search_range_min[1] = 1;
            search_range_max[1] = 1.2f;
            search_range_min[2] = 0.0001f;
            search_range_max[2] = 0.001f;

            float scale = 1;
            float rotation = 0;
            curve = new polynomial();
            curve.SetDegree(3);
            
            centre_of_distortion = new calibrationDot();
            centre_of_distortion.x = image_width/2;
            centre_of_distortion.y = image_height/2;
            float minimum_curvature, prev_minimum_curvature = float.MaxValue;
            float best_coeff0 = 0;
            float best_coeff1 = search_range_min[1] + ((search_range_max[1] - search_range_min[1]) / 2);
            float best_coeff2 = search_range_min[2] + ((search_range_max[2] - search_range_min[2]) / 2);

            curve.SetCoeff(0, best_coeff0);

            best_rectified_lines = null;

            Random rnd = new Random();

            int tries = 0;
            while (tries < 20)
            {
                minimum_curvature = float.MaxValue-1;
                
                float range1 = (search_range_max[1] - search_range_min[1]) / search_steps;
                float range2 = (search_range_max[2] - search_range_min[2]) / search_steps;
                
                // perform the search
                for (int i = 0; i < search_steps; i++)
                {
                    float coeff1 = search_range_min[1] + (i * (search_range_max[1] - search_range_min[1]) / search_steps);
                    coeff1 += ((float)rnd.NextDouble() - 0.5f) * range1;
                    if (coeff1 != 0)
                    {
                        curve.SetCoeff(1, coeff1);                        
                        for (int j = 0; j < search_steps; j++)
                        {
                            float coeff2 = search_range_min[2] + (j * (search_range_max[2] - search_range_min[2]) / search_steps);
                            coeff2 += ((float)rnd.NextDouble() - 0.5f) * range2;
                            if (coeff2 != 0)
                            {
                                curve.SetCoeff(2, coeff2);

                                List<List<float>> rectified_lines =
                                    RectifyLines(lines,
                                                 image_width, image_height,
                                                 curve,
                                                 centre_of_distortion,
                                                 rotation, scale);
                                if (rectified_lines.Count == lines.Count)
                                {
                                    float rectified_line_length = LineLength(rectified_lines);
                                    
                                    float scale2 = line_length / rectified_line_length;
                                    ScaleLines(rectified_lines, scale2, image_width, image_height, image_width/2, image_height/2);
                                    
                                    if ((rectified_line_length > min_line_length) &&
                                        (rectified_line_length < max_line_length))
                                    {
                                        float curvature = LineCurvature(rectified_lines);
                                        if (curvature > 0)
                                        {
                                            //Console.WriteLine("Curvature0: " + curvature.ToString());
                                            if (curvature < minimum_curvature)
                                            {
                                                best_coeff1 = coeff1;
                                                best_coeff2 = coeff2;
                                                best_rectified_lines = rectified_lines;
                                                minimum_curvature = curvature;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                float search_multiplier = 0.9f;

                // refine the search
                float search_radius = ((search_range_max[1] - search_range_min[1])/2.0f) * search_multiplier;
                search_range_min[1] = best_coeff1 - search_radius;
                search_range_max[1] = best_coeff1 + search_radius;                 

                search_radius = ((search_range_max[2] - search_range_min[2])/2.0f) * search_multiplier;
                search_range_min[2] = best_coeff2 - search_radius;
                search_range_max[2] = best_coeff2 + search_radius;
                
                //search_range_max[1] *= 0.9f;
                //search_range_max[2] *= 0.9f;
                
                Console.WriteLine("search range min: " + search_range_min[2].ToString());
                Console.WriteLine("search range max: " + search_range_max[2].ToString());
                Console.WriteLine("Coeff 1: " + best_coeff1.ToString());
                Console.WriteLine("Coeff 2: " + best_coeff2.ToString());
                Console.WriteLine("Curvature: " + minimum_curvature.ToString());
                //if (best_rectified_lines != null)
                //    Console.WriteLine("Rectified lines: " + best_rectified_lines.Count.ToString());
                tries++;
            }

        }

        #endregion

        #endregion

        #region "testing"

        private static void Test()
        {
            //string filename = "~/calibrationdata/forward2/raw0_5000_2000.jpg";
            string filename = "c:\\develop\\sentience\\calibrationimages\\raw0_5000_2000.jpg";
            Detect(filename);
        }

        #endregion

        #region "display"

        private static void ShowSquare(string filename, polygon2D square,
                                       string output_filename)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);
            square.show(img, bmp.Width, bmp.Height, 0, 255, 0, 0);

            Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
            if (output_filename.ToLower().EndsWith("jpg"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (output_filename.ToLower().EndsWith("bmp"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        private static void ShowLines(string filename, List<List<float>> lines,
                                      string output_filename)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);

            Random rnd = new Random();

            for (int i = 0; i < lines.Count; i++)
            {
                int r = 0;
                int g = 255;
                int b = 0;

                r = rnd.Next(255);
                g = rnd.Next(255);
                b = rnd.Next(255);

                List<float> line = lines[i];
                float prev_x=0, prev_y=0;
                for (int j = 0; j < line.Count; j += 2)
                {
                    float x = line[j];
                    float y = line[j + 1];
                    if (j > 0)
                        drawing.drawLine(img, bmp.Width, bmp.Height, (int)prev_x, (int)prev_y, (int)x, (int)y, r, g, b, 0, false);
                    prev_x = x;
                    prev_y = y;
                }
            }

            Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
            if (output_filename.ToLower().EndsWith("jpg"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (output_filename.ToLower().EndsWith("bmp"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        private static void ShowGrid(string filename, 
                                     hypergraph dots,
                                     List<calibrationDot> search_regions,
                                     string output_filename)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);

            if (search_regions != null)
            {
                for (int i = 0; i < search_regions.Count; i++)
                {
                    calibrationDot dot = (calibrationDot)search_regions[i];
                    drawing.drawCircle(img, bmp.Width, bmp.Height, dot.x, dot.y, dot.radius, 255, 255, 0, 0);
                }
            }

            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                calibrationDot dot = (calibrationDot)dots.Nodes[i];
                drawing.drawCircle(img, bmp.Width, bmp.Height, dot.x, dot.y, dot.radius, 0, 255, 0, 0);
            }

            for (int i = 0; i < dots.Links.Count; i++)
            {
                calibrationDot from_dot = (calibrationDot)dots.Links[i].From;
                calibrationDot to_dot = (calibrationDot)dots.Links[i].To;
                drawing.drawLine(img, bmp.Width, bmp.Height, (int)from_dot.x, (int)from_dot.y, (int)to_dot.x, (int)to_dot.y, 255, 0, 0, 0, false);
            }

            Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
            if (output_filename.ToLower().EndsWith("jpg"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (output_filename.ToLower().EndsWith("bmp"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        private static void ShowOverlayGrid(string filename,
                                            grid2D overlay_grid,
                                            string output_filename)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);

            if (overlay_grid != null)
            {
                overlay_grid.ShowIntercepts(img, bmp.Width, bmp.Height, 255, 0, 0, 5, 0);
            }

            Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
            if (output_filename.ToLower().EndsWith("jpg"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (output_filename.ToLower().EndsWith("bmp"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
        }


        private static void ShowGridCoordinates(string filename, 
                                                hypergraph dots,
                                                string output_filename)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);

            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                calibrationDot dot = (calibrationDot)dots.Nodes[i];
                drawing.drawCircle(img, bmp.Width, bmp.Height, dot.x, dot.y, dot.radius, 0, 255, 0, 0);
                
                if (dot.grid_x != 9999)
                {
                    string coord = dot.grid_x.ToString() + "," + dot.grid_y.ToString();
                    drawing.AddText(img, bmp.Width, bmp.Height, coord, "Courier New", 8, 0,0,0, (int)dot.x-0, (int)dot.y+5);
                }
            }

            Bitmap output_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, output_bmp);
            if (output_filename.ToLower().EndsWith("jpg"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (output_filename.ToLower().EndsWith("bmp"))
                output_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
        }


        #endregion        

        #region "detecting dots"

        private static void ApplyGrid(hypergraph dots, 
                                      List<calibrationDot> centre_dots)
        {
            const int UNASSIGNED = 9999;
            
            // mark all dots as unassigned
            for (int i = 0; i < dots.Nodes.Count; i++)
            {
                calibrationDot dot = (calibrationDot)dots.Nodes[i];
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
                
            Console.WriteLine(dots_assigned.ToString() + " dots assigned grid coordinates");
        }
        
        private static void ApplyGrid(calibrationDot current_dot, int unassigned_value, ref int dots_assigned)
        {
            for (int i = 0; i < current_dot.Links.Count; i++)
            {
                calibrationLink link = (calibrationLink)current_dot.Links[i];
                calibrationDot dot = (calibrationDot)link.From;
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

        private static void LinkDots(hypergraph dots,
                                     calibrationDot current_dot,
                                     float horizontal_dx, float horizontal_dy,
                                     float vertical_dx, float vertical_dy,
                                     int start_index,
                                     List<calibrationDot> search_regions)
        {
            if (!current_dot.centre)
            {
                int start_index2 = 0;
                float tollerance_divisor = 0.3f;
                float horizontal_tollerance = (float)Math.Sqrt((horizontal_dx * horizontal_dx) + (horizontal_dy * horizontal_dy)) * tollerance_divisor;
                float vertical_tollerance = (float)Math.Sqrt((vertical_dx * vertical_dx) + (vertical_dy * vertical_dy)) * tollerance_divisor;

                float x=0, y=0;
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

                        calibrationDot search_region = new calibrationDot();
                        search_region.x = x;
                        search_region.y = y;
                        search_region.radius = horizontal_tollerance;
                        search_regions.Add(search_region);

                        for (int j = 0; j < dots.Nodes.Count; j++)
                        {
                            if ((!((calibrationDot)dots.Nodes[j]).centre) &&
                                (dots.Nodes[j] != current_dot))
                            {
                                float dx = ((calibrationDot)dots.Nodes[j]).x - x;
                                float dy = ((calibrationDot)dots.Nodes[j]).y - y;
                                float dist_from_expected_position = (float)Math.Sqrt(dx * dx + dy * dy);
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

                    float found_dx = ((calibrationDot)dots.Nodes[indexes_found[i]]).x - current_dot.x;
                    float found_dy = ((calibrationDot)dots.Nodes[indexes_found[i]]).y - current_dot.y;

                    calibrationLink link = new calibrationLink();
                    
                    if (found_vertical[i])
                    {
                        link.horizontal = false;
                        
                        if (((vertical_dy > 0) && (found_dy < 0)) ||
                            ((vertical_dy < 0) && (found_dy > 0)))
                        {
                            found_dx = -found_dx;
                            found_dy = -found_dy;
                        }
                        LinkDots(dots, (calibrationDot)dots.Nodes[indexes_found[i]], horizontal_dx, horizontal_dy, found_dx, found_dy, start_index2, search_regions);
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
                        LinkDots(dots, (calibrationDot)dots.Nodes[indexes_found[i]], found_dx, found_dy, vertical_dx, vertical_dy, start_index2, search_regions);
                    }
                    
                    dots.LinkByReference((calibrationDot)dots.Nodes[indexes_found[i]], current_dot, link);
                    
                }

            }
        }

        private static polygon2D GetCentreSquare(hypergraph dots,
                                                 ref List<calibrationDot> centredots)
        {
            centredots = new List<calibrationDot>();

            // find the centre dot
            calibrationDot centre = null;
            int i = 0;
            while ((i < dots.Nodes.Count) && (centre == null))
            {
                calibrationDot dot = (calibrationDot)dots.Nodes[i];
                if (dot.centre) centre = dot;
                i++;
            }

            // look for the four surrounding dots
            List<calibrationDot> centre_dots = new List<calibrationDot>();
            List<float> distances = new List<float>();
            i = 0;
            for (i = 0; i < dots.Nodes.Count; i++)
            {
                calibrationDot dot = (calibrationDot)dots.Nodes[i];
                if (!dot.centre)
                {
                    float dx = dot.x - centre.x;
                    float dy = dot.y - centre.y;
                    float dist = (float)Math.Sqrt(dx*dx + dy*dy);
                    if (distances.Count == 4)
                    {
                        int index = -1;
                        float max_dist = 0;
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

            polygon2D centre_square = null;
            if (centre_dots.Count == 4)
            {
                centre_square = new polygon2D();
                for (i = 0; i < 4; i++)
                    centre_square.Add(0, 0);

                float xx = centre.x;
                float yy = centre.y;
                int index = 0;
                for (i = 0; i < 4; i++)
                {
                    if ((centre_dots[i].x < xx) &&
                        (centre_dots[i].y < yy))
                    {
                        xx = centre_dots[i].x;
                        yy = centre_dots[i].y;
                        centre_square.x_points[0] = xx;
                        centre_square.y_points[0] = yy;
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
                        centre_square.x_points[1] = xx;
                        centre_square.y_points[1] = yy;
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
                        centre_square.x_points[2] = xx;
                        centre_square.y_points[2] = yy;
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
                        centre_square.x_points[3] = xx;
                        centre_square.y_points[3] = yy;
                        index = i;
                    }
                }
                centredots.Add(centre_dots[index]);
            }
            return (centre_square);
        }

        /// <summary>
        /// detects calibration dots within the given image
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static hypergraph DetectDots(string filename,
                                             ref int image_width, ref int image_height)
        {
            Console.WriteLine("Detecting dots in image " + filename);

            hypergraph dots = new hypergraph();

            int grouping_radius_percent = 2;
            int erosion_dilation = 0;
            float minimum_width = 0.0f;
            float maximum_width = 10;
            List<int> edges = null;
            
            List<calibrationDot> dot_shapes = shapes.DetectDots(filename, grouping_radius_percent, erosion_dilation,
                                                                minimum_width, maximum_width,
                                                                ref edges, "edges.jpg", "groups.jpg", "dots.jpg",
                                                                ref image_width, ref image_height);
            
            float tx = 99999;
            float ty = 99999;
            float bx = 0;
            float by = 0;
            for (int i = 0; i < dot_shapes.Count; i++)
            {
                if (dot_shapes[i].x < tx) tx = dot_shapes[i].x;
                if (dot_shapes[i].y < ty) ty = dot_shapes[i].y;
                if (dot_shapes[i].x > bx) bx = dot_shapes[i].x;
                if (dot_shapes[i].y > by) by = dot_shapes[i].y;
                dots.Add(dot_shapes[i]);
            }
            
            Console.WriteLine(dot_shapes.Count.ToString() + " dots discovered");

            return(dots);
        }
        
        #endregion
        
        #region "validation"

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("help");         // show help info
            ValidParameters.Add("dir");          // directory where calibration files are stored
            ValidParameters.Add("baseline");     // baseline distance between the cameras
            ValidParameters.Add("dotdist");      // horizontal distance to the calibration pattern dot
            ValidParameters.Add("height");       // height above the calibration pattern in millimetres
            ValidParameters.Add("fov");          // field of view in degrees

            return (ValidParameters);
        }

        #endregion

        #region "help information"

        /// <summary>
        /// shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("fishfood Help");
            Console.WriteLine("-------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  fishfood");
            Console.WriteLine("                  -dir <calibration files directory>");
            Console.WriteLine("                  -baseline <stereo camera baseline in millimetres>");
            Console.WriteLine("                  -dotdist <horizontal distance to the calibration pattern dot in millimetres>");
            Console.WriteLine("                  -height <height above the calibration pattern in millimetres>");
            Console.WriteLine("                  -fov <horizontal field of view in degrees>");
        }

        #endregion
    }
}