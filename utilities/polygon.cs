/*
    2D polygon object
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
using System.Collections;
using System.IO;

namespace sluggish.utilities
{
    /// <summary>
    /// polygons are used to define regions of interest or alert regions
    /// </summary>
    public class polygon2D
    {
        public String name = "";
        public int type=0;
        public bool occupied=false;
        public ArrayList x_points=null;
        public ArrayList y_points;


        /// <summary>
        /// draw the polygon within the given image
        /// </summary>
        /// <param name="img">image</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="lineWidth">line width</param>
        public void show(byte[] img, int image_width, int image_height, int r, int g, int b, int lineWidth)
        {
            show(img, image_width, image_height, r, g, b, lineWidth, 0, 0); 
        }

        /// <summary>
        /// draw the polygon within the given image
        /// </summary>
        /// <param name="img">image</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="lineWidth">line width</param>
        public void show(byte[] img, int image_width, int image_height, int r, int g, int b, int lineWidth, int x_offset, int y_offset)
        {
            if (x_points!=null)
            {
                for (int i = 1; i < x_points.Count; i++)
                {
                    sluggish.utilities.drawing.drawLine(img, image_width, image_height, (int)x_points[i - 1]+x_offset, (int)y_points[i - 1]+y_offset, (int)x_points[i]+x_offset, (int)y_points[i]+y_offset, r, g, b, lineWidth, false);
                    if (i == x_points.Count - 1)
                        sluggish.utilities.drawing.drawLine(img, image_width, image_height, (int)x_points[0]+x_offset, (int)y_points[0]+y_offset, (int)x_points[i]+x_offset, (int)y_points[i]+y_offset, r, g, b, lineWidth, false);
                }
            }
        }

        /// <summary>
        /// returns the centre of gravity
        /// </summary>
        /// <param name="centre_x"></param>
        /// <param name="centre_y"></param>
        public void getCentreOfGravity(ref float centre_x, ref float centre_y)
        {
            float x = 0, y = 0;
            for (int i = 0; i < x_points.Count; i++)
            {
                x += (float)x_points[i];
                y += (float)y_points[i];
            }
            if (x_points.Count > 0)
            {
                x /= x_points.Count;
                y /= y_points.Count;
            }
            centre_x = x;
            centre_y = y;
        }
        
        /// <summary>
        /// return a scaled version of the polygon
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        public polygon2D Scale(float factor)
        {
            polygon2D rescaled = new polygon2D();

            float centre_x = 0, centre_y = 0;
            getCentreOfGravity(ref centre_x, ref centre_y);

            for (int i = 0; i < x_points.Count; i++)
            {
                float dx = (float)x_points[i] - centre_x;
                float dy = (float)y_points[i] - centre_y;
                float x = (float)(centre_x + (dx * factor));
                float y = (float)(centre_y + (dy * factor));
                rescaled.Add(x, y);
            }
            return (rescaled);
        }

        /// <summary>
        /// return the perimeter length of the polygon
        /// </summary>
        /// <returns></returns>
        public float getPerimeterLength()
        {
            float x0, y0, x1, y1;
            float perimeter = 0;

            for (int i = 0; i < x_points.Count; i++)
            {
                int index1 = i;
                x0 = (float)x_points[index1];
                y0 = (float)y_points[index1];

                int index2 = i + 1;
                if (index2 >= x_points.Count)
                    index2 -= x_points.Count;

                x1 = (float)x_points[index2];
                y1 = (float)y_points[index2];
                float dx = x1 - x0;
                float dy = y1 - y0;
                perimeter += (float)Math.Sqrt((dx * dx) + (dy * dy));
            }
            return (perimeter);
        }

        /// <summary>
        /// return the shortest side of the polygon
        /// </summary>
        /// <returns></returns>
        public float getShortestSide()
        {
            float x0, y0, x1, y1;
            float shortest = 0;

            for (int i = 0; i < x_points.Count; i++)
            {
                int index1 = i;
                x0 = (float)x_points[index1];
                y0 = (float)y_points[index1];

                int index2 = i + 1;
                if (index2 >= x_points.Count)
                    index2 -= x_points.Count;

                x1 = (float)x_points[index2];
                y1 = (float)y_points[index2];
                float dx = x1 - x0;
                float dy = y1 - y0;
                float side_length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                if ((shortest == 0) ||
                    ((side_length < shortest) && (side_length > 1)))
                    shortest = side_length;
            }
            return (shortest);
        }


        /// <summary>
        /// returns the length of one side of the polygon
        /// </summary>
        /// <param name="index">index of the side</param>
        /// <returns></returns>
        public float getSideLength(int index)
        {
            int index1 = index;
            if (index1 >= x_points.Count)
                index1 -= x_points.Count;

            float x0 = (float)x_points[index1];
            float y0 = (float)y_points[index1];

            int index2 = index + 1;
            if (index2 >= x_points.Count)
                index2 -= x_points.Count;

            float x1 = (float)x_points[index2];
            float y1 = (float)y_points[index2];
            float dx = x1 - x0;
            float dy = y1 - y0;
            return((float)Math.Sqrt((dx * dx) + (dy * dy)));
        }

        /// <summary>
        /// returns start and end positions for a side
        /// </summary>
        /// <param name="index">index of the side</param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        public void getSidePositions(int index,
                                     ref float tx, ref float ty, ref float bx, ref float by)
        {
            int index1 = index;
            if (index1 >= x_points.Count)
                index1 -= x_points.Count;

            tx = (float)x_points[index1];
            ty = (float)y_points[index1];

            int index2 = index + 1;
            if (index2 >= x_points.Count)
                index2 -= x_points.Count;

            bx = (float)x_points[index2];
            by = (float)y_points[index2];
        }

        /// <summary>
        /// return the perimeter length of the polygon
        /// </summary>
        /// <returns></returns>
        public float getSquareness()
        {
            float squareness = -1;
            
            if (x_points != null)
            {
	            if (x_points.Count == 4)
	            {
		            float x0, y0, x1, y1;
		            float[] side_length = new float[4];
		            float perimeter = 0;

		            for (int i = 0; i < x_points.Count; i++)
		            {
		                int index1 = i;
		                x0 = (float)x_points[index1];
		                y0 = (float)y_points[index1];

		                int index2 = i + 1;
		                if (index2 >= x_points.Count)
		                    index2 -= x_points.Count;

		                x1 = (float)x_points[index2];
		                y1 = (float)y_points[index2];
		                float dx = x1 - x0;
		                float dy = y1 - y0;
		                side_length[i] = (float)Math.Sqrt((dx * dx) + (dy * dy));
		                perimeter += side_length[i];
		            }
		            
		            if (perimeter > 0)
		            {
			            float average_side_length = perimeter / 4;
			            squareness = 0;
			            for (int i = 0; i < 4; i++)
			            {
			                squareness += Math.Abs(side_length[i] - average_side_length);
			            }
			            if (squareness == 0)
			                squareness = -1;
			            else
			                squareness = 1.0f / (1.0f + (squareness / perimeter));
		            }
	            }
            }
            return (squareness);
        }


        /// <summary>
        /// show the polygon in the given image
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="r">red colour component 0-255</param>
        /// <param name="g">green colour component 0-255</param>
        /// <param name="b">blue colour component 0-255</param>
        /// <param name="lineWidth">line width to use for drawing</param>
        public void plot(byte[] img, int image_width, int image_height,
                         int r, int g, int b, int lineWidth)
        {
            float x0, y0, x1, y1;

            for (int i = 0; i < x_points.Count; i++)
            {
                if (i > 0)
                {
                    x0 = ((float)x_points[i - 1] * image_width) / 1000;
                    y0 = ((float)y_points[i - 1] * image_height) / 1000;
                }
                else
                {
                    x0 = ((float)x_points[x_points.Count - 1] * image_width) / 1000;
                    y0 = ((float)y_points[y_points.Count - 1] * image_height) / 1000;
                }
                x1 = ((float)x_points[i] * image_width) / 1000;
                y1 = ((float)y_points[i] * image_height) / 1000;
                sluggish.utilities.drawing.drawLine(img, image_width, image_height, (int)x0, (int)y0, (int)x1, (int)y1, r, g, b, lineWidth, false);
            }
        }

        /// <summary>
        /// returns the leftmost coordinate of the polygon
        /// </summary>
        /// <returns></returns>
        public float left()
        {
            float min_x = 0;
            float x;

            for (int i = 0; i < x_points.Count; i++)
            {
                x = (float)x_points[i];
                if ((min_x == 0) || (x < min_x)) min_x = x;
            }
            return (min_x);
        }

        /// <summary>
        /// returns the rightmost coordinate of the polygon
        /// </summary>
        /// <returns></returns>
        public float right()
        {
            float max_x = 0;
            float x;

            for (int i = 0; i < x_points.Count; i++)
            {
                x = (float)x_points[i];
                if ((max_x == 0) || (x > max_x)) max_x = x;
            }
            return (max_x);
        }

        /// <summary>
        /// returns the top coordinate of the polygon
        /// </summary>
        /// <returns></returns>
        public float top()
        {
            float min_y = 0;
            float v;

            for (int i = 1; i < y_points.Count; i++)
            {
                v = (float)y_points[i];
                if ((min_y == 0) || (v < min_y)) min_y = v;                
            }
            return (min_y);
        }

        /// <summary>
        /// returns the bottom coordinate of the polygon
        /// </summary>
        /// <returns></returns>
        public float bottom()
        {
            float max_y = 0;
            float v;

            for (int i = 1; i < y_points.Count; i++)
            {
                v = (float)y_points[i];
                if ((max_y == 0) || (v > max_y)) max_y = v;
            }
            return (max_y);
        }

        /// <summary>
        /// does the polygon intersect with the given line?
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="xi">intersection x coordinate</param>
        /// <param name="yi">intersection y coordinate</param>
        /// <returns></returns>
        public bool intersection(float x0, float y0, float x1, float y1,
                                 ref float xi, ref float yi)
        {
            float a1, b1, c1,         //constants of linear equations
                  a2, b2, c2,
                  det_inv,          //the inverse of the determinant of the coefficient        
                  m1, m2, dm;         //the gradients of each line
            bool insideLine = false;  //is the intersection along the lines given, or outside them
            float tx, ty, bx, by;
            int i;
            float x2, y2, x3, y3;

            if (x_points != null)
            {
                i = 0;
                while ((i < x_points.Count) && (!insideLine))
                {
                    if (i > 0)
                    {
                        x2 = (float)x_points[i - 1];
                        y2 = (float)y_points[i - 1];
                    }
                    else
                    {
                        x2 = (float)x_points[x_points.Count - 1];
                        y2 = (float)y_points[y_points.Count - 1];
                    }
                    x3 = (float)x_points[i];
                    y3 = (float)y_points[i];

                    //compute gradients, note the cludge for infinity, however, this will
                    //be close enough
                    if ((x1 - x0) != 0)
                        m1 = (float)(y1 - y0) / (x1 - x0);
                    else
                        m1 = (float)1e+10;   //close, but no cigar

                    if ((x3 - x2) != 0)
                        m2 = (float)(y3 - y2) / (x3 - x2);
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
                        xi = (int)((b1 * c2 - b2 * c1) * det_inv);
                        yi = (int)((a2 * c1 - a1 * c2) * det_inv);

                        //is the intersection inside the line or outside it?
                        if (x0 < x1) { tx = x0; bx = x1; } else { tx = x1; bx = x0; }
                        if (y0 < y1) { ty = y0; by = y1; } else { ty = y1; by = y0; }
                        if ((xi >= tx) && (xi <= bx) && (yi >= ty) && (yi <= by))
                        {
                            insideLine = true;
                        }
                    }
                    else
                    {
                        //parallel (or parallel-ish) lines, return some indicative value
                        xi = 9999;
                    }
                    i++;
                }
            }

            return (insideLine);
        }

        /// <summary>
        /// save the polygon data to file
        /// </summary>
        /// <param name="binfile"></param>
        public void save(BinaryWriter binfile)
        {
            int i;

            binfile.Write(name);
            binfile.Write(type);
            binfile.Write(occupied);
            if (x_points != null)
            {
                binfile.Write(x_points.Count);
                for (i = 0; i < x_points.Count; i++)
                {
                    binfile.Write((float)x_points[i]);
                    binfile.Write((float)y_points[i]);
                }
            }
            else
            {
                binfile.Write((float)0);
            }
        }

        /// <summary>
        /// load the polygon data from file
        /// </summary>
        /// <param name="binfile"></param>
        public void load(BinaryReader binfile)
        {
            int i,n;
            float x,y;

            name = binfile.ReadString();
            type = binfile.ReadInt32();
            occupied = binfile.ReadBoolean();
            n = binfile.ReadInt32();
            if (n > 0)
            {
                x_points = new ArrayList();
                y_points = new ArrayList();

                for (i = 0; i < n; i++)
                {
                    x = binfile.ReadSingle();
                    y = binfile.ReadSingle();
                    x_points.Add(x);
                    y_points.Add(y);
                }
            }
        }


        /// <summary>
        /// clear all points
        /// </summary>
        public void Clear()
        {
            if (x_points != null)
            {
                x_points.Clear();
                y_points.Clear();
            }
        }

        /// <summary>
        /// add a new point
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        public void Add(float x, float y)
        {
            if (x_points == null)
            {
                x_points = new ArrayList();
                y_points = new ArrayList();
            }
            x_points.Add(x);
            y_points.Add(y);
        }

        /// <summary>
        /// add a new point
        /// </summary>
        /// <param name="index">position at which to insert the data</param>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        public void Add(int index, float x, float y)
        {
            if (x_points == null)
            {
                x_points = new ArrayList();
                y_points = new ArrayList();
            }

            // add any entries as necessary
            for (int i = x_points.Count; i <= index; i++)
            {
                x_points.Add(0.0f);
                y_points.Add(0.0f);
            }
            
            x_points[index] = x;
            y_points[index] = y;
        }

        /// <summary>
        /// remove the last point in the list
        /// </summary>
        public void Remove()
        {
            if (x_points != null)
            {
                int i = x_points.Count-1;
                if (i >= 0)
                {
                    x_points.RemoveAt(i);
                    y_points.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// is the given x,y point inside the polygon?
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool isInside(float x, float y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = x_points.Count - 1; i < x_points.Count; j = i++)
            {
                if (((((float)y_points[i] <= y) && (y < (float)y_points[j])) ||
                   (((float)y_points[j] <= y) && (y < (float)y_points[i]))) &&
                  (x < ((float)x_points[j] - (float)x_points[i]) * (y - (float)y_points[i]) / ((float)y_points[j] - (float)y_points[i]) + (float)x_points[i]))
                    c = !c;
            }
            return (c); 
        }

        /// <summary>
        /// return true if this polygon overlaps with another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool overlaps(polygon2D other)
        {
            int i;
            bool retval = false;

            i = 0;
            while ((i < x_points.Count) && (retval == false))
            {
                if (other.isInside((float)x_points[i],(float)y_points[i])) retval=true;
                i++;
            }

            i = 0;
            while ((i < other.x_points.Count) && (retval == false))
            {
                if (isInside((float)other.x_points[i], (float)other.y_points[i])) retval = true;
                i++;
            }
            return (retval);
        }

        /// <summary>
        /// does this polygon overlap with the other, within the given screen dimensions
        /// </summary>
        /// <param name="other">other polygon object</param>
        /// <param name="image_width">image width</param>
        /// <param name="image_height">image height</param>
        /// <returns></returns>
        public bool overlaps(polygon2D other, int image_width, int image_height)
        {
            int i;
            bool retval = false;

            i = 0;
            while ((i < x_points.Count) && (retval == false))
            {
                if (other.isInside((float)x_points[i] * 1000 / image_width, (float)y_points[i] * 1000 / image_height)) retval = true;
                i++;
            }

            i = 0;
            while ((i < other.x_points.Count) && (retval == false))
            {
                if (isInside((float)other.x_points[i] * image_width / 1000, (float)other.y_points[i] * image_height / 1000)) retval = true;
                i++;
            }
            return (retval);
        }
    }
	
}
