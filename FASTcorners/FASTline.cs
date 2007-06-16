/*
    FAST corner detector
    C# version adapted from original C code by Edward Rosten 
    http://mi.eng.cam.ac.uk/~er258/work/fast.html
    Copyright (C) 2006 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;

namespace sluggish.imageprocessing.FASTcorners
{	
    /// <summary>
    /// stores information about a line feature consisting of two corners
    /// </summary>
    public class FASTline
    {
       public bool Visible = false;
       public bool onHorizon = false;
       public FASTcorner point1;
       public FASTcorner point2;

       /// <summary>
       /// returns true if the points given are on a line
       /// </summary>
       /// <param name="img"></param>
       /// <param name="img_width"></param>
       /// <param name="img_height"></param>
       /// <param name="p1"></param>
       /// <param name="p2"></param>
       /// <returns></returns>
       public static bool isLine(Byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                 FASTcorner p1, FASTcorner p2, int line_threshold)
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
                   int n = ((y * img_width) + x) * bytes_per_pixel;
                   int r = radius * bytes_per_pixel;

                   diff_x = (img[n - r] + img[n - r + bytes_per_pixel]) -
                                (img[n + r] + img[n + r - bytes_per_pixel]);
                   if (diff_x < 0) diff_x = -diff_x;

                   if (diff_x < line_threshold)
                   {
                       r = radius * img_width * bytes_per_pixel;

                       diff_y = (img[n - r] + img[n - r + bytes_per_pixel]) -
                                (img[n + r] + img[n + r - bytes_per_pixel]);
                       if (diff_y < 0) diff_y = -diff_y;
                   }

                   if ((diff_x > line_threshold) || (diff_y > line_threshold))
                       line = true;
               }
               else line = true;
               sample_number++;
           }

           return (line);
       }


       public bool isOnHorizon(Byte[] colour_img, int img_width, int img_height, int horizon_threshold)
       {
           const int bytes_per_pixel = 3;
           bool horizon = false;
           int sample_number = 0;
           const int max_samples = 10;
           int radius = img_height / 10;

           int dx = point2.x - point1.x;
           int dy = point2.y - point1.y;

           if ((point1.y > img_height / 10) && (point2.y > img_height / 10))
           {
               if ((point1.y < img_height - (img_height / 8)) && (point2.y < img_height - (img_height / 8)))
               {
                   if (Math.Abs(dx) > img_width / 10)
                   {
                       horizon = true;
                       while ((sample_number < max_samples) && (horizon))
                       {
                           horizon = false;

                           int x = point1.x + (dx * sample_number / max_samples);
                           int y = point1.y + (dy * sample_number / max_samples);

                           if ((x > radius) && (x < img_width - radius) && (y > radius) && (y < img_height - radius))
                           {
                               int interval = (y - 10) / 15;
                               int tot = 0;
                               int hits = 0;
                               for (int yy = y - 10; yy > 0; yy -= interval)
                               {
                                   int n = ((yy * img_width) + x) * bytes_per_pixel;
                                   tot += colour_img[n];
                                   hits++;
                               }
                               if (hits > 0)
                               {
                                   tot /= hits;
                                   if (tot > 230)
                                   {
                                       horizon = true;
                                   }
                               }

                           }
                           else horizon = true;
                           sample_number++;
                       }
                   }
               }
           }

           return (horizon);
       }


       public FASTline(FASTcorner point1, FASTcorner point2)
       {
           this.point1 = point1;
           this.point2 = point2;
       }
   }
}
