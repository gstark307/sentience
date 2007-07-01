/*
    Computational geometry routines
    Copyright (C) 2000-2007 Bob Mottram
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

namespace sluggish.utilities
{
	public class geometry
	{		
		public geometry()
		{
		}

        /// <summary>
        /// returns the angle subtended by three points
        /// </summary>
        /// <param name="x0">line first point x coordinate</param>
        /// <param name="y0">line first point y coordinate</param>
        /// <param name="x1">line second point x coordinate</param>
        /// <param name="y1">line second point y coordinate</param>
        /// <param name="x2">line third point x coordinate</param>
        /// <param name="y2">line third point y coordinate</param>
        /// <returns>angle in radians</returns>
        public static float threePointAngle(float x0, float y0,
                                            float x1, float y1,
                                            float x2, float y2)
        {
            float pt1 = x0 - x1;
            float pt2 = y0 - y1;
            float pt3 = x2 - x1;
            float pt4 = y2 - y1;

            float angle = ((pt1 * pt3) + (pt2 * pt4)) / 
                           (((float)Math.Sqrt((pt1*pt1) + (pt2*pt2))) * 
                            ((float)Math.Sqrt((pt3*pt3) + (pt4*pt4))));

            angle = (float)Math.Acos(angle);
            return(angle);
        }

        /// <summary>
        /// returns the perpendicular distance of a line from the centre of a circle
        /// </summary>
        /// <param name="x0">line first point x coordinate</param>
        /// <param name="y0">line first point y coordinate</param>
        /// <param name="x1">line second point x coordinate</param>
        /// <param name="y1">line second point y coordinate</param>
        /// <param name="circle_x">circle centre x coordinate</param>
        /// <param name="circle_y">circle centre y coordinate</param>
        /// <param name="circle_radius">circle radius</param>
        /// <returns></returns>
        public static float circleDistanceFromLine(float x0, float y0, float x1, float y1,
                                                   float circle_x, float circle_y, float circle_radius)
        {
            float perpendicular_dist = 999999;

            float dx = x1 - x0;
            float dy = y1 - y0;
            float line_length = (float)Math.Sqrt((dx * dx) + (dy * dy));

            if (line_length > 0)
            {
                // perpendicular line, spanning the circle
                float perp_line_x0 = circle_x - dy;
                float perp_line_y0 = circle_y - dx;
                float perp_line_x1 = circle_x + dy;
                float perp_line_y1 = circle_y + dx;

                float ix = 9999;
                float iy = 0;
                intersection(x0, y0, x1, y1,
                             perp_line_x0, perp_line_y0, perp_line_x1, perp_line_y1,
                             ref ix, ref iy);

                if (ix != 9999)
                {
                    dx = ix - circle_x;
                    dy = iy - circle_y;
                    perpendicular_dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                }
            }
            
            return (perpendicular_dist);
        }

        /// <summary>
        /// does the line intersect with the given line?
        /// </summary>
        /// <param name="x0">first line top x</param>
        /// <param name="y0">first line top y</param>
        /// <param name="x1">first line bottom x</param>
        /// <param name="y1">first line bottom y</param>
        /// <param name="x2">second line top x</param>
        /// <param name="y2">second line top y</param>
        /// <param name="x3">second line bottom x</param>
        /// <param name="y3">second line bottom y</param>
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
                //parallel (or parallelish) lines, return some indicative value
                xi = 9999;
            }

            return (insideLine);
        }

	}
}
