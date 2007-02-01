/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
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
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class util
    {
        
        public static Byte[] loadFromBitmap(String filename, int image_width, int image_height, int bytes_per_pixel)
        {
            Byte[] bmp = new Byte[image_width * image_height * bytes_per_pixel];

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);
                int n = 54;
                int n2 = 0;
                Byte[] data = new Byte[(image_width * image_height * bytes_per_pixel) + n];
                binfile.Read(data, 0, (image_width * image_height * bytes_per_pixel) + n);
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        n2 = (((image_height-1-y) * image_width) + x) * 3;
                        for (int c = 0; c < bytes_per_pixel; c++)
                        {
                            bmp[n2+c] = data[n];
                            n++;
                        }                        
                    }
                }
                binfile.Close();
                fp.Close();
            }
            return (bmp);
        }
        

        public static Byte[] loadFromPGM(String filename, int image_width, int image_height, int bytes_per_pixel)
        {
            Byte[] bmp = new Byte[image_width * image_height * bytes_per_pixel];

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);
                int n = 96;
                int n2 = 0;
                Byte[] data = new Byte[(image_width * image_height) + n];
                binfile.Read(data, 0, (image_width * image_height) + n);
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        for (int c = 0; c < bytes_per_pixel; c++)
                        {
                            bmp[n2] = data[n];
                            n2++;
                        }
                        n++;
                    }
                }
                binfile.Close();
                fp.Close();
            }
            return (bmp);
        }

        
        //---------------------------------------------------------------------------------------------
        //draw a line between two points in the given image
        //---------------------------------------------------------------------------------------------
        public static void drawLine(Byte[] img, int img_width, int img_height,
                                    int x1, int y1, int x2, int y2, int r, int g, int b, int linewidth, bool overwrite)
        {
            int w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
            float m;

            dx = x2 - x1;
            dy = y2 - y1;
            w = Math.Abs(dx);
            h = Math.Abs(dy);
            if (x2 >= x1) step_x = 1; else step_x = -1;
            if (y2 >= y1) step_y = 1; else step_y = -1;

            if (w > h)
            {
                if (dx != 0)
                {
                    m = dy / (float)dx;
                    x = x1;
                    while (x != x2 + step_x)
                    {
                        y = (int)(m * (x - x1)) + y1;

                        for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                            for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                            {
                                if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                {
                                    int n = ((img_width * yy2) + xx2) * 3;
                                    if ((img[n] == 0) || (!overwrite))
                                    {
                                        img[n] = (Byte)b;
                                        img[n + 1] = (Byte)g;
                                        img[n + 2] = (Byte)r;
                                    }
                                    else
                                    {
                                        img[n] = (Byte)((img[n] + b) / 2);
                                        img[n + 1] = (Byte)((img[n] + g) / 2);
                                        img[n + 2] = (Byte)((img[n] + r) / 2);
                                    }
                                }
                            }

                        x += step_x;
                    }
                }
            }
            else
            {
                if (dy != 0)
                {
                    m = dx / (float)dy;
                    y = y1;
                    while (y != y2 + step_y)
                    {
                        x = (int)(m * (y - y1)) + x1;
                        for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                            for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                            {
                                if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                {
                                    int n = ((img_width * yy2) + xx2) * 3;
                                    if ((img[n] == 0) || (!overwrite))
                                    {
                                        img[n] = (Byte)b;
                                        img[n + 1] = (Byte)g;
                                        img[n + 2] = (Byte)r;
                                    }
                                    else
                                    {
                                        img[n] = (Byte)((img[n] + b) / 2);
                                        img[n + 1] = (Byte)((img[n] + g) / 2);
                                        img[n + 2] = (Byte)((img[n] + r) / 2);
                                    }
                                }
                            }

                        y += step_y;
                    }
                }
            }
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <returns></returns>
        public static Byte[] monoImage(Byte[] img_colour, int img_width, int img_height)
        {
            Byte[] mono_image = new Byte[img_width * img_height];
            int n=0;

            for (int i = 0; i < img_width * img_height * 3; i += 3)
            {
                int tot = 0;
                for (int col = 0; col < 3; col++)
                {
                    tot += img_colour[i + col];
                }
                mono_image[n] = (Byte)(tot / 3);
                n++;
            }
            return (mono_image);
        }

    }
}
