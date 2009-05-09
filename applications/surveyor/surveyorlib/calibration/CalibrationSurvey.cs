/*
    calibration survey - used to find the best matching curve
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
using sluggish.utilities;

namespace surveyor.vision
{
    public class CalibrationSurvey
    {
        // the survey radius as a percentage of the image width
        public int radius_percent = 5;
        
        // number of coefficients
        public int degree = 3;
        
        // population of polynomials within the survey
        private polynomial[,] survey;
        
        // number of updates completed so far
        private int survey_updates;
        
        // after this number of updates test to find the minimum rms error
        public int test_interval = 10;
        
        // best results
        public double minimum_rms_error = double.MaxValue;
        public polynomial best_fit_curve;
        
        // the best centre of distortion value
        public float centre_of_distortion_x;
        public float centre_of_distortion_y;
		
        public void Reset()
        {
            survey = null;
            survey_updates = 0;
            minimum_rms_error = double.MaxValue;
            best_fit_curve = null;
        }

        public void Update(
		    int image_width, 
		    int image_height,
            CalibrationDot[,] grid)
        {
            if (survey_updates < test_interval)
            {
                int cx = image_width / 2;
                int cy = image_height / 2;
                int radius_pixels = image_width * radius_percent / 100;
                int radius_pixels_sqr = radius_pixels*radius_pixels;
                int diameter_pixels = radius_pixels * 2;
                int tx = cx - radius_pixels;
                int bx = tx + diameter_pixels;
                int ty = cy - radius_pixels;
                int by = ty + diameter_pixels;
                
                if (survey == null) survey = new polynomial[(diameter_pixels*2)+1, (diameter_pixels*2)+1];
                if (survey.GetLength(0) != diameter_pixels) survey = new polynomial[(diameter_pixels*2)+1, (diameter_pixels*2)+1];

                for (float centre_x = tx; centre_x <= bx; centre_x += 0.5f)
                {
                    float dcx = centre_x - cx;
                    dcx*= dcx;
                    for (float centre_y = ty; centre_y <= by; centre_y += 0.5f)
                    {
                        float dcy = centre_y - cy;
                        dcy *= dcy;
                        
                        float r = dcx*dcx + dcy*dcy;
                        if (r < radius_pixels_sqr)
                        {
                            int xx = (int)((centre_x -tx)*2);
                            int yy = (int)((centre_y -ty)*2);
                            
                            // get the curve associated with this possible centre of distortion
                            if (survey[xx, yy] == null)
                            {
                                polynomial p = new polynomial();
                                p.SetDegree(degree);
                                survey[xx, yy] = p;
                            }
                            polynomial curve = survey[xx, yy];
                            
                            for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                            {
                                for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                                {
                                    CalibrationDot dot = grid[grid_x, grid_y];
                                    if (dot != null)
                                    {
                                        if (dot.rectified_x > 0)
                                        {
                                            double dx = dot.x - centre_x;
                                            double dy = dot.y - centre_y;
                                            double actual_radial_dist = Math.Sqrt(dx*dx + dy*dy);

                                            dx = dot.rectified_x - centre_x;
                                            dy = dot.rectified_y - centre_y;
                                            double rectified_radial_dist = Math.Sqrt(dx*dx + dy*dy);
                                            
                                            curve.AddPoint(rectified_radial_dist, actual_radial_dist);
                                        }
                                    }
                                }
                            }
                                
                        }
                    }
                }
                survey_updates++;
                if (survey_updates >= test_interval)
                {
                    FindBestCurve(tx, ty);
                    survey = null;
                    survey_updates = 0;
                }
            }
        }
        
        private void FindBestCurve(int tx, int ty)
        {
            for (int x = 0; x < survey.GetLength(0); x++)
            {
                for (int y = 0; y < survey.GetLength(1); y++)
                {
                    polynomial p = survey[x,y];
                    if (p != null)
                    {
                        p.Solve();
                        double rms_error = p.GetRMSerror();
                        if (rms_error < minimum_rms_error)
                        {
                            best_fit_curve = p;
                            minimum_rms_error = rms_error;
                            centre_of_distortion_x = tx + (x/2.0f);
                            centre_of_distortion_y = ty + (y/2.0f);
                        }
                    }
                }
            }
            Console.WriteLine("Minimum RMS error: " + minimum_rms_error.ToString());
        }
        
    }
}
