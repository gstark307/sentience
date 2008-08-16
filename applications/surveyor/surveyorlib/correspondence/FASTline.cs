/*
    FAST corner detector
    C# version adapted from original C code by Edward Rosten 
    http://mi.eng.cam.ac.uk/~er258/work/fast.html
    Copyright (C) 2006 Bob Mottram
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

namespace surveyor.vision
{	
    /// <summary>
    /// stores information about a line feature consisting of two corners
    /// </summary>
    public class FASTline
    {
       public int response;
       public FASTcorner point1;
       public FASTcorner point2;

        /// <summary>
        /// returns true if the points given are on a line
        /// </summary>
        /// <param name="img_mono">mono image data (1 byte per pixel)</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="p1">first corner</param>
        /// <param name="p2">second corner</param>
        /// <returns>true if this is a line</returns>
        public static bool isLine(byte[] img_mono, 
		                          int img_width, int img_height,
                                  FASTcorner p1, FASTcorner p2, 
		                          int line_threshold,
		                          ref int response)
		{
            bool line = true;
            int diff_x = 0, diff_y = 0, sample_number = 0;
            const int max_samples = 100;
            const int radius = 10;

            int dx = p2.x - p1.x;
            int dy = p2.y - p1.y;
            while ((sample_number < max_samples) && (line))
			{
                line = false;

                int x = p1.x + (dx * sample_number / max_samples);
                int y = p1.y + (dy * sample_number / max_samples);

                if ((x > radius) && (x < img_width - radius) && (y > radius) && (y < img_height - radius))
                {
                    int n = (y * img_width) + x;
                    int r = radius;

                    diff_x = (img_mono[n - r] + img_mono[n - r]) -
                             (img_mono[n + r] + img_mono[n + r]);
                    if (diff_x < 0) diff_x = -diff_x;

                    if (diff_x < line_threshold)
                    {
                        r = radius * img_width;

                        diff_y = (img_mono[n - r] + img_mono[n - r]) -
                                 (img_mono[n + r] + img_mono[n + r]);
                        if (diff_y < 0) diff_y = -diff_y;
                    }

                    if ((diff_x > line_threshold) || (diff_y > line_threshold))
			        {
                        line = true;
					    response += diff_x + diff_y;
					}
				}
                else line = true;
                sample_number++;
			}

           return (line);
		}


        public FASTline(FASTcorner point1, FASTcorner point2, int response)
        {
            this.point1 = point1;
            this.point2 = point2;
            this.response = response;
        }
    }
}