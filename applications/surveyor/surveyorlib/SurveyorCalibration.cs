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
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    public class SurveyorCalibration
    {
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

            // calculate teh radius of each dot in pixels
            float radius = dot_spacing * 0.5f * dot_radius_percent / 100; 
            
            Console.WriteLine("radius = " + radius.ToString());
            
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
    }
}
