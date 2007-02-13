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
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class util
    {
        #region "xml"

        /// <summary>
        /// This method adds a text element to the XML document as the last
        /// child of the current element.
        /// </summary>
        /// <param name="doc">The XML document</param>
        /// <param name="nodeParent">Parent of the node we are adding</param>
        /// <param name="strTag">The tag of the element to add</param>
        /// <param name="strValue">The text value of the new element</param>
        public static XmlElement AddTextElement(XmlDocument doc, XmlElement nodeParent, String strTag, String strValue)
        {
            XmlElement nodeElem = doc.CreateElement(strTag);
            XmlText nodeText = doc.CreateTextNode(strValue);
            nodeParent.AppendChild(nodeElem);
            nodeElem.AppendChild(nodeText);
            return (nodeElem);
        }

        public static void AddComment(XmlDocument doc, XmlElement nodeParent, String comment)
        {
            XmlNode commentnode = doc.CreateComment(comment);
            nodeParent.AppendChild(commentnode);
        }

        #endregion

        /// <summary>
        /// does the line intersect with the given line?
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="xi">intersection x coordinate</param>
        /// <param name="yi">intersection y coordinate</param>
        /// <returns></returns>
        public static bool intersection(float x0, float y0, float x1, float y1,
                                  float x2, float y2, float x3, float y3,
                                  ref float xi, ref float yi)
        {
            float a1, b1, c1,         //constants of linear equations
                  a2, b2, c2,
                  det_inv,            //the inverse of the determinant of the coefficient        
                  m1, m2, dm;         //the gradients of each line
            bool insideLine = false;  //is the intersection along the lines given, or outside them
            float tx, ty, bx, by;

            //compute gradients, note the cludge for infinity, however, this will
            //be close enough
            if ((x1 - x0) != 0)
                m1 = (y1 - y0) / (x1 - x0);
            else
                m1 = (float)1e+10;   //close, but no cigar

            if ((x3 - x2) != 0)
                m2 = (y3 - y2) / (x3 - x2);
            else
                m2 = (float)1e+10;   //close, but no cigar

            dm = (float)Math.Abs(m1 - m2);
            if (dm > 0.01f)
            {
                //compute constants
                a1 = m1;
                a2 = m2;

                b1 = -1;
                b2 = -1;

                c1 = (y0 - m1 * x0);
                c2 = (y2 - m2 * x2);

                //compute the inverse of the determinate
                det_inv = 1 / (a1 * b2 - a2 * b1);

                //use Kramers rule to compute xi and yi
                xi = ((b1 * c2 - b2 * c1) * det_inv);
                yi = ((a2 * c1 - a1 * c2) * det_inv);

                //is the intersection inside the line or outside it?
                if (x0 < x1) { tx = x0; bx = x1; } else { tx = x1; bx = x0; }
                if (y0 < y1) { ty = y0; by = y1; } else { ty = y1; by = y0; }
                if ((xi >= tx) && (xi <= bx) && (yi >= ty) && (yi <= by))
                {
                    if (x2 < x3) { tx = x2; bx = x3; } else { tx = x3; bx = x2; }
                    if (y2 < y3) { ty = y2; by = y3; } else { ty = y3; by = y2; }
                    if ((xi >= tx) && (xi <= bx) && (yi >= ty) && (yi <= by))
                    {
                        insideLine = true;
                    }
                }
            }
            else
            {
                //parallel (or parallel-ish) lines, return some indicative value
                xi = 9999;
            }

            return (insideLine);
        }


        
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


        public static void drawBox(Byte[] img, int img_width, int img_height,
                                   int x, int y, int radius, int r, int g, int b, int line_width)
        {
            int radius_y = radius * img_width / img_height;
            int tx = x - radius;
            int ty = y - radius_y;
            int bx = x + radius;
            int by = y + radius_y;
            drawLine(img, img_width, img_height, tx, ty, bx, ty, r, g, b, line_width, false);
            drawLine(img, img_width, img_height, bx, ty, bx, by, r, g, b, line_width, false);
            drawLine(img, img_width, img_height, tx, by, bx, by, r, g, b, line_width, false);
            drawLine(img, img_width, img_height, tx, by, tx, ty, r, g, b, line_width, false);
        }

        public static void drawCross(Byte[] img, int img_width, int img_height,
                                     int x, int y, int radius, int r, int g, int b, int line_width)
        {
            int radius_y = radius * img_width / img_height;
            int tx = x - radius;
            int ty = y - radius_y;
            int bx = x + radius;
            int by = y + radius_y;
            drawLine(img, img_width, img_height, x, ty, x, by, r, g, b, line_width, false);
            drawLine(img, img_width, img_height, tx, y, bx, y, r, g, b, line_width, false);
        }

        
        //---------------------------------------------------------------------------------------------
        //draw a line between two points in the given image
        //---------------------------------------------------------------------------------------------
        public static void drawLine(Byte[] img, int img_width, int img_height,
                                    int x1, int y1, int x2, int y2, int r, int g, int b, int linewidth, bool overwrite)
        {
            if (img != null)
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
