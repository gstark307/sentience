/*
    2D polygon object
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
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace sentience.calibration
{
    /// <summary>
    /// polygons are used to define regions of interest or alert regions
    /// </summary>
    public class polygon2D
    {
        public string name = "";
        public int type=0;
        public bool occupied=false;
        public List<float> x_points=null;
        public List<float> y_points;

        #region "creating a circle"
        
        /// <summary>
        /// creates a circle shape
        /// </summary>
        /// <param name="centre_x">
        /// centre x coordinate of the circle <see cref="System.Single"/>
        /// </param>
        /// <param name="centre_y">
        /// centre y coordinate of the circle <see cref="System.Single"/>
        /// </param>
        /// <param name="radius">
        /// radius of teh circle <see cref="System.Single"/>
        /// </param>
        /// <param name="circumference_steps">
        /// number of steps to use when drawing the circle <see cref="System.Int32"/>
        /// </param>
        /// <returns>
        /// polygon representing a circle <see cref="polygon2D"/>
        /// </returns>
        public static polygon2D CreateCircle(float centre_x, float centre_y,
                                             float radius, int circumference_steps)
        {
            polygon2D circle = new polygon2D();
            for (int i = 0; i < circumference_steps; i++)
            {
                float angle = i * (float)Math.PI * 2 / circumference_steps;
                float x = centre_x + (radius * (float)Math.Sin(angle));
                float y = centre_y + (radius * (float)Math.Cos(angle));
                circle.Add(x, y);
            }
            return(circle);
        }
        
        #endregion
        
        #region "copying"

        /// <summary>
        /// returns a copy of the polygon
        /// </summary>
        /// <returns></returns>
        public polygon2D Copy()
        {
            polygon2D new_poly = new polygon2D();
            new_poly.name = name;
            new_poly.type = type;
            new_poly.occupied = occupied;

            if (x_points != null)
            {
                for (int i = 0; i < x_points.Count; i++)
                {
                    float x = x_points[i];
                    float y = y_points[i];
                    new_poly.Add(x, y);
                }
            }
            return (new_poly);
        }

        #endregion
        
        #region "randomly move points"
        
        /// <summary>
        /// returns a copy of this polygon with each vetex randomly perturbed
        /// </summary>
        /// <param name="max_displacement">
        /// maximum perturbation <see cref="System.Single"/>
        /// </param>
        /// <param name="rnd">
        /// random number generator <see cref="Random"/>
        /// </param>
        /// <returns>
        /// polygon object <see cref="polygon2D"/>
        /// </returns>
        public polygon2D Jiggle(float max_displacement, Random rnd)
        {
            polygon2D result = null;
            
            if (x_points != null)
            {
                result = new polygon2D();
                float max = max_displacement*2;
                for (int i = 0; i < x_points.Count; i++)
                {
                    float jiggled_x = x_points[i] + ((float)rnd.NextDouble() * max) - max_displacement;
                    float jiggled_y = x_points[i] + ((float)rnd.NextDouble() * max) - max_displacement;
                    result.Add(jiggled_x, jiggled_y);
                }
            }
            return(result);
        }
        
        
        #endregion

        #region "rotation and scaling"

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
                float dx = x_points[i] - centre_x;
                float dy = y_points[i] - centre_y;
                float x = (float)(centre_x + (dx * factor));
                float y = (float)(centre_y + (dy * factor));
                rescaled.Add(x, y);
            }
            return (rescaled);
        }

        /// <summary>
        /// scale the polygon to a new image size
        /// </summary>
        /// <param name="original_image_width"></param>
        /// <param name="original_image_height"></param>
        /// <param name="new_image_width"></param>
        /// <param name="new_image_height"></param>
        /// <returns></returns>
        public polygon2D Scale(int original_image_width, int original_image_height,
                               int new_image_width, int new_image_height)
        {
            polygon2D rescaled = new polygon2D();

            for (int i = 0; i < x_points.Count; i++)
            {
                float x = x_points[i] * new_image_width / original_image_width;
                float y = y_points[i] * new_image_height / original_image_height;
                rescaled.Add(x, y);
            }
            return (rescaled);
        }

        /// <summary>
        /// adds the given rotation angle to the orientation of the polygon
        /// using the given centre point
        /// </summary>
        /// <param name="rotation">rotation to be added in radians</param>
        /// <param name="centre_x">centre about which to rotate</param>
        /// <param name="centre_y">centre about which to rotate</param>
        public void rotate(float rotation, 
                           float centre_x, float centre_y)
        {
            for (int i = 0; i < x_points.Count; i++)
            {
                float dx = x_points[i] - centre_x;
                float dy = y_points[i] - centre_y;
                float hyp = (float)Math.Sqrt((dx * dx) + (dy * dy));
                if (hyp > 0)
                {
                    float angle = (float)Math.Acos(dy / hyp);
                    if (dx < 0) angle = ((float)Math.PI * 2) - angle;
                    angle += (float)(Math.PI * 3 / 2);

                    angle += rotation;
                    x_points[i] = centre_x + (hyp * (float)Math.Sin(angle));
                    y_points[i] = centre_y + (hyp * (float)Math.Cos(angle));
                }
            }
        }

        #endregion

        #region "display functions"

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
                    x0 = (x_points[i - 1] * image_width) / 1000;
                    y0 = (y_points[i - 1] * image_height) / 1000;
                }
                else
                {
                    x0 = (x_points[x_points.Count - 1] * image_width) / 1000;
                    y0 = (y_points[y_points.Count - 1] * image_height) / 1000;
                }
                x1 = (x_points[i] * image_width) / 1000;
                y1 = (y_points[i] * image_height) / 1000;
                drawing.drawLine(img, image_width, image_height, (int)x0, (int)y0, (int)x1, (int)y1, r, g, b, lineWidth, false);
            }
        }

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
                    drawing.drawLine(img, image_width, image_height, (int)(x_points[i - 1]+x_offset), (int)(y_points[i - 1]+y_offset), (int)(x_points[i]+x_offset), (int)(y_points[i]+y_offset), r, g, b, lineWidth, false);
                    if (i == x_points.Count - 1)
                        drawing.drawLine(img, image_width, image_height, (int)(x_points[0]+x_offset), (int)(y_points[0]+y_offset), (int)(x_points[i]+x_offset), (int)(y_points[i]+y_offset), r, g, b, lineWidth, false);
                }
            }
        }

        #endregion

        #region "finding the centre"

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
                x += x_points[i];
                y += y_points[i];
            }
            if (x_points.Count > 0)
            {
                x /= x_points.Count;
                y /= y_points.Count;
            }
            centre_x = x;
            centre_y = y;
        }

        #endregion

        #region "measuring intensities"

        /// <summary>
        /// returns the average pixel intensity for the perimeter of the polygon
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <returns>average pixel intensity</returns>
        public float GetAveragePerimeterIntensity(byte[] img, int img_width, int img_height, int bytes_per_pixel)
        {
            float av_intensity = 0;
            float x_next = 0, y_next = 0;

            for (int i = 0; i < x_points.Count; i++)
            {
                float x = x_points[i];
                float y = y_points[i];
                if (i < x_points.Count - 1)
                {
                    x_next = x_points[i + 1];
                    y_next = y_points[i + 1];
                }
                else
                {
                    x_next = x_points[0];
                    y_next = y_points[0];
                }

                av_intensity += image.averageLineIntensity(img, img_width, img_height, bytes_per_pixel,
                                                          (int)x_next, (int)y_next, (int)x, (int)y, 1);
            }
            if (x_points.Count > 0) av_intensity /= x_points.Count;
            return (av_intensity);
        }

        #endregion

        #region "measuring lengths"

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
                x0 = x_points[index1];
                y0 = y_points[index1];

                int index2 = i + 1;
                if (index2 >= x_points.Count)
                    index2 -= x_points.Count;

                x1 = x_points[index2];
                y1 = y_points[index2];
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
            float shortest = float.MaxValue;

            for (int i = 0; i < x_points.Count; i++)
            {
                int index1 = i;
                x0 = x_points[index1];
                y0 = y_points[index1];

                int index2 = i + 1;
                if (index2 >= x_points.Count)
                    index2 -= x_points.Count;

                x1 = x_points[index2];
                y1 = y_points[index2];
                float dx = x1 - x0;
                float dy = y1 - y0;
                float side_length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                if (side_length < shortest)
                    shortest = side_length;
            }
            if (shortest == float.MaxValue) shortest = 0;

            return (shortest);
        }

        /// <summary>
        /// returns orientations of each edge of the polygon
        /// </summary>
        /// <returns></returns>
        public float[] getOrientations()
        {
            float[] orientations = new float[4];

            for (int i = 0; i < x_points.Count; i++)
            {
                float x0, y0, x1, y1;

                if (i < x_points.Count - 1)
                {
                    x0 = x_points[i];
                    y0 = y_points[i];
                    x1 = x_points[i + 1];
                    y1 = y_points[i + 1];
                }
                else
                {
                    x0 = x_points[i];
                    y0 = y_points[i];
                    x1 = x_points[0];
                    y1 = y_points[0];
                }

                float dx = x1 - x0;
                float dy = y1 - y0;
                float hyp = (float)Math.Sqrt((dx * dx) + (dy * dy));
                orientations[i] = (float)Math.Acos(dx / hyp);
                if (dy < 0) orientations[i] = ((float)Math.PI * 2) - orientations[i];
            }
            return (orientations);
        }

        /// <summary>
        /// returns gradients of each edge of the polygon
        /// </summary>
        /// <returns></returns>
        public float[] getGradients()
        {
            float[] gradients = new float[4];

            for (int i = 0; i < x_points.Count; i++)
            {
                float x0, y0, x1, y1;

                if (i < x_points.Count - 1)
                {
                    x0 = x_points[i];
                    y0 = y_points[i];
                    x1 = x_points[i + 1];
                    y1 = y_points[i + 1];
                }
                else
                {
                    x0 = x_points[i];
                    y0 = y_points[i];
                    x1 = x_points[0];
                    y1 = y_points[0];
                }

                float dx = x1 - x0;
                float dy = y1 - y0;
                if (dx != 0) gradients[i] = dy / dx;
            }
            return (gradients);
        }

        /// <summary>
        /// return the longest side of the polygon
        /// </summary>
        /// <returns></returns>
        public float getLongestSide()
        {
            float x0, y0, x1, y1;
            float longest = float.MinValue;

            for (int i = 0; i < x_points.Count; i++)
            {
                int index1 = i;
                x0 = x_points[index1];
                y0 = y_points[index1];

                int index2 = i + 1;
                if (index2 >= x_points.Count)
                    index2 -= x_points.Count;

                x1 = x_points[index2];
                y1 = y_points[index2];
                float dx = x1 - x0;
                float dy = y1 - y0;
                float side_length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                if (side_length > longest)
                    longest = side_length;
            }
            if (longest == float.MinValue) longest = 0;

            return (longest);
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

            float x0 = x_points[index1];
            float y0 = y_points[index1];

            int index2 = index + 1;
            if (index2 >= x_points.Count)
                index2 -= x_points.Count;

            float x1 = x_points[index2];
            float y1 = y_points[index2];
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

            tx = x_points[index1];
            ty = y_points[index1];

            int index2 = index + 1;
            if (index2 >= x_points.Count)
                index2 -= x_points.Count;

            bx = x_points[index2];
            by = y_points[index2];
        }

        #endregion

        #region "measuring angles"

        /// <summary>
        /// returns the interior angle subtended at the given vertex
        /// </summary>
        /// <param name="vertex">
        /// index number of the vertex <see cref="System.Int32"/>
        /// </param>
        /// <returns>
        /// angle in radians <see cref="System.Single"/>
        /// </returns>
        public float GetInteriorAngle(int vertex)
        {
            float angle = 0;
            if (x_points != null)
            {
                int previous_vertex = vertex - 1;
                if (previous_vertex < 0) previous_vertex += x_points.Count;

                int next_vertex = vertex + 1;
                if (next_vertex >= x_points.Count) next_vertex -= x_points.Count;
                
                angle = geometry.threePointAngle(x_points[previous_vertex], y_points[previous_vertex],
                                                 x_points[vertex], y_points[vertex],
                                                 x_points[next_vertex], y_points[next_vertex]);
                
                // ensure that this is an angle less than 180 degrees
                if (angle < 0) angle = -angle;
                if (angle > Math.PI)
                    angle = (2 * (float)Math.PI) - angle;
                
                //float angle_degrees = angle / (float)Math.PI * 180.0f;
                //Console.WriteLine("angle = " + angle_degrees.ToString());
            }
            return(angle);
        }
        
        /// <summary>
        /// returns the maximum difference from perfectly square corners
        /// </summary>
        /// <returns>
        /// maximum difference from a right angle in radians <see cref="System.Single"/>
        /// </returns>
        public float GetMaxDifferenceFromSquare()
        {
            float max_diff_angle = 0;

            if (x_points != null)
            {
                // is this a square?
                if (x_points.Count == 4)
                {
                    float square_angle = (float)Math.PI / 2.0f;
                    
                    // get the angles for each vertex
                    for (int vertex = 0; vertex < 4; vertex++)
                    {
                        float angle_diff = GetInteriorAngle(vertex) - square_angle;
                        if (angle_diff < 0) angle_diff = -angle_diff;
                        if (angle_diff > max_diff_angle) max_diff_angle = angle_diff;
                    }
                }
            }
            
            return(max_diff_angle);
        }
        
        /// <summary>
        /// return the orientation of the longest side
        /// </summary>
        /// <returns>orientation in radians</returns>
        public float getLongestSideOrientation()
        {
            float x0, y0, x1, y1;
            float longest_dx=0, longest_dy=0;
            float longest = 0;
            float orientation = 0;

            for (int i = 0; i < x_points.Count; i++)
            {
                int index1 = i;
                x0 = x_points[index1];
                y0 = y_points[index1];

                int index2 = i + 1;
                if (index2 >= x_points.Count)
                    index2 -= x_points.Count;

                x1 = x_points[index2];
                y1 = y_points[index2];
                float dx = x1 - x0;
                float dy = y1 - y0;
                float side_length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                if ((longest == 0) ||
                    ((side_length > longest) && (side_length > 1)))
                {
                    longest = side_length;
                    longest_dx = dx;
                    longest_dy = dy;
                }
            }

            if (longest_dx != 0)
            {
                orientation = (float)Math.Acos(longest_dx / longest);
                if (longest_dy < 0) orientation = ((float)Math.PI * 2) - orientation;
                if (orientation < 0) orientation += ((float)Math.PI*2);
                if (orientation > (float)Math.PI) orientation -= (float)Math.PI;
            }

            return (orientation);
        }

        /// <summary>
        /// returns the orientation of the given side
        /// </summary>
        /// <param name="side_index">index number of the side</param>
        /// <returns>orientation in radians</returns>
        public float GetSideOrientation(int side_index)
        {
            int next_side_index = side_index + 1;
            if (next_side_index >= x_points.Count) next_side_index -= x_points.Count;
            float dx = x_points[next_side_index] - x_points[side_index];
            float dy = y_points[next_side_index] - y_points[side_index];

            float length = (float)Math.Sqrt(dx*dx+dy*dy);
            float orientation = (float)Math.Acos(dy / length);
            if (dx < 0) orientation = ((float)Math.PI * 2) - orientation;
            return (orientation);
        }

        #endregion

        /// <summary>
        /// scales the length of one of the sides of the polygon
        /// </summary>
        /// <param name="side_index">vertex index</param>
        /// <param name="scale">scaling factor</param>
        /// <returns>scaled version</returns>
        public polygon2D ScaleSideLength(int side_index, float scale)
        {
            polygon2D scaled = this.Copy();
            int next_side_index = side_index + 1;
            if (next_side_index >= x_points.Count) next_side_index = 0;

            float x0 = x_points[side_index];
            float y0 = y_points[side_index];
            float x1 = x_points[next_side_index];
            float y1 = y_points[next_side_index];
            float dx = x1 - x0;
            float dy = y1 - y0;
            float cx = x0 + (dx * 0.5f);
            float cy = y0 + (dy * 0.5f);
            dx *= 0.5f;
            dy *= 0.5f;
            scaled.x_points[side_index] = cx - (dx * scale);
            scaled.y_points[side_index] = cy - (dy * scale);
            scaled.x_points[next_side_index] = cx + (dx * scale);
            scaled.y_points[next_side_index] = cy + (dy * scale);
            return (scaled);
        }

        #region "special stuff for squares"

        /// <summary>
        /// returns the centre point of a square shaped polygon by finding
        /// the intersection of the two diagonals
        /// </summary>
        /// <param name="centre_x">x centre of the square</param>
        /// <param name="centre_y">y centre of the square</param>
        public void GetSquareCentre(ref float centre_x, ref float centre_y)
        {
            if (x_points != null)
            {
                if (x_points.Count == 4)
                {
                    geometry.intersection(x_points[0], y_points[0],
                                          x_points[2], y_points[2],
                                          x_points[1], y_points[1],
                                          x_points[3], y_points[3],
                                          ref centre_x, ref centre_y);
                }
            }
        }

        /// <summary>
        /// return a value indicating how even the aspect ratio of the sides are
        /// with 1.0 being a perfect square
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
		                x0 = x_points[index1];
		                y0 = y_points[index1];

		                int index2 = i + 1;
		                if (index2 >= x_points.Count)
		                    index2 -= x_points.Count;

		                x1 = x_points[index2];
		                y1 = y_points[index2];
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
        /// returns an orientation angle for a square polygon by looking
        /// at the longest vertical line
        /// </summary>
        /// <returns>orientation of the polygon</returns>
        public float GetSquareOrientation()
        {
            float orientation = 0;

            if (x_points != null)
            {
                float max_vertical = 0;
                for (int i = 0; i < x_points.Count; i++)
                {
                    float x0 = x_points[i];
                    float y0 = y_points[i];
                    float x1, y1;
                    if (i < x_points.Count - 1)
                    {
                        x1 = x_points[i + 1];
                        y1 = y_points[i + 1];
                    }
                    else
                    {
                        x1 = x_points[0];
                        y1 = y_points[0];
                    }

                    // keep the orientation consistent
                    if (y1 < y0)
                    {
                        float temp = y1;
                        y1 = y0;
                        y0 = temp;
                        temp = x1;
                        x1 = x0;
                        x0 = temp;
                    }
                    
                    float dy = y1 - y0;
                    float dx = x1 - x0;

                    // vertically oriented lines
                    if (Math.Abs(dy) > Math.Abs(dx))
                    {
                        float vertical = Math.Abs(dy);
                        if (vertical > max_vertical)
                        {
                            max_vertical = vertical;

                            float length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                            orientation = (float)Math.Acos(dy / length);
                            if (dx < 0) orientation = (float)(Math.PI * 2) - orientation;
                        }
                    }
                }
            }
            return (orientation);
        }

        /// <summary>
        /// returns an horizontal width of the square
        /// </summary>
        /// <returns>horizontal width in pixels</returns>
        public float GetSquareHorizontal()
        {
            float length = 0;

            if (x_points != null)
            {
                float max_horizontal = 0;
                for (int i = 0; i < x_points.Count; i++)
                {
                    float x0 = x_points[i];
                    float y0 = y_points[i];
                    float x1, y1;
                    if (i < x_points.Count - 1)
                    {
                        x1 = x_points[i + 1];
                        y1 = y_points[i + 1];
                    }
                    else
                    {
                        x1 = x_points[0];
                        y1 = y_points[0];
                    }

                    float dy = y1 - y0;
                    float dx = x1 - x0;

                    // horizontally oriented lines
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        float horizontal = Math.Abs(dx);
                        if (horizontal > max_horizontal)
                        {
                            max_horizontal = horizontal;

                            length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        }
                    }
                }
            }
            return (length);
        }

        /// <summary>
        /// returns an vertical height of the square
        /// </summary>
        /// <returns>vertical height in pixels</returns>
        public float GetSquareVertical()
        {
            float length = 0;

            if (x_points != null)
            {
                float max_vertical = 0;
                for (int i = 0; i < x_points.Count; i++)
                {
                    float x0 = x_points[i];
                    float y0 = y_points[i];
                    float x1, y1;
                    if (i < x_points.Count - 1)
                    {
                        x1 = x_points[i + 1];
                        y1 = y_points[i + 1];
                    }
                    else
                    {
                        x1 = x_points[0];
                        y1 = y_points[0];
                    }

                    float dy = y1 - y0;
                    float dx = x1 - x0;

                    // vertically oriented lines
                    if (Math.Abs(dy) > Math.Abs(dx))
                    {
                        float vertical = Math.Abs(dy);
                        if (vertical > max_vertical)
                        {
                            max_vertical = vertical;

                            length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        }
                    }
                }
            }
            return (length);
        }


        #endregion

        #region "bounding box"

        /// <summary>
        /// returns the bounding box of the polygon
        /// </summary>
        /// <param name="tx">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="ty">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="bx">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="by">
        /// A <see cref="System.Single"/>
        /// </param>
        public void BoundingBox(ref float tx, ref float ty,
                                ref float bx, ref float by)
        {
            float x,y;
            tx = float.MaxValue;
            bx = float.MinValue;
            ty = float.MaxValue;
            by = float.MinValue;
            
            for (int i = 0; i < x_points.Count; i++)
            {
                x = x_points[i];
                if (x < tx) tx = x;
                if (x > bx) bx = x;

                y = y_points[i];
                if (y < ty) ty = y;
                if (y > by) by = y;
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
                x = x_points[i];
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
                x = x_points[i];
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

            for (int i = 0; i < y_points.Count; i++)
            {
                v = y_points[i];
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

            for (int i = 0; i < y_points.Count; i++)
            {
                v = y_points[i];
                if ((max_y == 0) || (v > max_y)) max_y = v;
            }
            return (max_y);
        }

        #endregion

        #region "saving and loading"

        /// <summary>
        /// returns the polygon as an XML element
        /// </summary>
        /// <param name="doc">XML document</param>
        /// <param name="parent">parent node</param>
        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            // ensure that all floating point values are in GB format
            // with a full stop representing the decimal place
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");

            XmlElement poly = doc.CreateElement("Polygon2D");
            parent.AppendChild(poly);
                        
            xml.AddTextElement(doc, poly, "Name", name);
            
            string coords = "";
            for (int i = 0; i < x_points.Count; i++)
            {
                coords += Convert.ToString(x_points[i], format) + "," +
                          Convert.ToString(y_points[i], format);
                if (i < x_points.Count-1) coords += ", ";
            }
            
            xml.AddTextElement(doc, poly, "Coordinates", coords);            return(poly);            
        }

        public void LoadFromXml(XmlNode xnod, int level)
        {
            // ensure that all floating point values are in GB format
            // with a full stop representing the decimal place
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");

            if (xnod.Name == "Name")
            {
                name = xnod.InnerText;
            }

            if (xnod.Name == "Coordinates")
            {
                if (x_points == null)
                {
                    x_points = new List<float>();
                    y_points = new List<float>();
                }
                else
                {
                    x_points.Clear();
                    y_points.Clear();
                }
                string[] coords_str = xnod.InnerText.Split(',');
                for (int i = 0; i < coords_str.Length; i += 2)
                {
                    x_points.Add(Convert.ToSingle(coords_str[i], format));
                    y_points.Add(Convert.ToSingle(coords_str[i+1], format));
                }
            }

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                XmlNode xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
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
                    binfile.Write(x_points[i]);
                    binfile.Write(y_points[i]);
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
                x_points = new List<float>();
                y_points = new List<float>();

                for (i = 0; i < n; i++)
                {
                    x = binfile.ReadSingle();
                    y = binfile.ReadSingle();
                    x_points.Add(x);
                    y_points.Add(y);
                }
            }
        }

        #endregion

        #region "adding and removing points"

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
                x_points = new List<float>();
                y_points = new List<float>();
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
                x_points = new List<float>();
                y_points = new List<float>();
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

        #endregion

        #region "mirroring and flipping"

        /// <summary>
        /// mirror the polygon with respect to the given image size
        /// </summary>
        /// <param name="image_width">width of the image within which the polygon is contained</param>
        /// <param name="image_height">height of the image within which the polygon is contained</param>
        public void Mirror(int image_width, int image_height)
        {
            if (x_points != null)
            {
                for (int i = 0; i < x_points.Count; i++)
                    x_points[i] = image_width - 1 - x_points[i];
            }
        }

        /// <summary>
        /// flip the polygon with respect to the given image size
        /// </summary>
        /// <param name="image_width">width of the image within which the polygon is contained</param>
        /// <param name="image_height">height of the image within which the polygon is contained</param>
        public void Flip(int image_width, int image_height)
        {
            if (y_points != null)
            {
                for (int i = 0; i < y_points.Count; i++)
                    y_points[i] = image_height - 1 - y_points[i];
            }
        }

        #endregion

        #region "looking for overlaps or points which are inside the polygon"

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
                if ((((y_points[i] <= y) && (y < y_points[j])) ||
                   ((y_points[j] <= y) && (y < y_points[i]))) &&
                  (x < (x_points[j] - x_points[i]) * (y - y_points[i]) / (y_points[j] - y_points[i]) + x_points[i]))
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
                if (other.isInside(x_points[i],y_points[i])) retval=true;
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
                if (other.isInside(x_points[i] * 1000 / image_width, y_points[i] * 1000 / image_height)) retval = true;
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

        #endregion

        #region "comparing with another polygon"

        /// <summary>
        /// returns the difference in the positions of vertices
        /// compared to another polygon
        /// </summary>
        /// <param name="other">the other polygon</param>
        /// <returns></returns>
        public float Compare(polygon2D other)
        {
            float difference = 0;

            // vertices exist
            if ((x_points != null) && (other.x_points != null))
            {
                // same number of vertices
                if (x_points.Count == other.x_points.Count)
                {
                    for (int i = 0; i < x_points.Count; i++)
                    {
                        float dx = Math.Abs(x_points[i] - other.x_points[i]);
                        float dy = Math.Abs(y_points[i] - other.y_points[i]);
                        difference += dx + dy;
                    }
                }
            }

            return (difference);
        }

        #endregion

        #region "fitting the polygon to a grid"

        /// <summary>
        /// snaps the vertices of the polygon to a grid having the given spacing
        /// </summary>
        /// <param name="grid_spacing">spacing of the grid</param>
        /// <returns>fitted polygon</returns>
        public polygon2D SnapToGrid(float grid_spacing)
        {
            polygon2D result = new polygon2D();
            if (x_points != null)
            {
                for (int i = 0; i < x_points.Count; i++)
                {
                    float x = x_points[i] / grid_spacing;
                    x = (float)Math.Round(x) * grid_spacing;
                    float y = y_points[i] / grid_spacing;
                    y = (float)Math.Round(y) * grid_spacing;
                    result.Add(x, y);
                }
            }
            return (result);
        }

        #endregion

        #region "area of the polygon"

        /// <summary>
        /// approximately calculates the area of a polygon
        /// by snapping the vertices to a grid, then applying
        /// pick's theorem to find the area
        /// </summary>
        /// <param name="grid_spacing">spacing of the grid which vertices will be snapped to</param>
        /// <returns>area of the polygon</returns>
        public float ApproximateArea(float grid_spacing)
        {
            float area = 0;
            float tollerance = 0.01f;

            if (x_points != null)
            {
                if (x_points.Count > 2)
                {
                    polygon2D fit_to_grid = SnapToGrid(grid_spacing);
                    float tx = fit_to_grid.left();
                    float ty = fit_to_grid.top();
                    float bx = fit_to_grid.right();
                    float by = fit_to_grid.bottom();

                    int points_interior = 0;
                    for (float y = ty; y <= by; y += grid_spacing)
                    {
                        for (float x = tx; x <= bx; x += grid_spacing)
                        {
                            if (fit_to_grid.isInside(x, y))
                                points_interior++;
                        }
                    }

                    int points_boundary = 0;
                    for (int vertex = 0; vertex < fit_to_grid.x_points.Count - 1; vertex++)
                    {
                        float dx = fit_to_grid.x_points[vertex + 1] - fit_to_grid.x_points[vertex];
                        float dy = fit_to_grid.y_points[vertex + 1] - fit_to_grid.y_points[vertex];
                        float length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        int steps = (int)(length / grid_spacing);
                        float start_x = fit_to_grid.x_points[vertex];
                        float start_y = fit_to_grid.y_points[vertex];
                        for (int i = 0; i < steps; i++)
                        {
                            float x = start_x + (i * dx / steps);
                            float x_snapped = x / grid_spacing;
                            x_snapped = (float)Math.Round(x_snapped) * grid_spacing;

                            float dx2 = x_snapped - x;
                            if ((dx2 > -tollerance) && (dx2 < tollerance))
                            {
                                float y = start_y + (i * dy / steps);
                                float y_snapped = y / grid_spacing;
                                y_snapped = (float)Math.Round(y_snapped) * grid_spacing;
                                float dy2 = y_snapped - y;
                                if ((dy2 > -tollerance) && (dx2 < tollerance))
                                    points_boundary++;
                            }
                        }
                    }

                    // calculate area by pick's theorem
                    area = points_interior + (points_boundary / 2) - 1;
                    area *= grid_spacing;
                }
            }

            return area;
        }

        #endregion
    }

    /// <summary>
    /// tracks the movement of polygons in 2D
    /// </summary>
    public class polygon2DTracker
    {
        /// <summary>
        /// contains data used to track a polygon
        /// </summary>
        internal class polygon2DTrackerData
        {
            // centre point of the polygon
            public float centre_x, centre_y;

            // velocity
            public float vx, vy, v_angular;

            // when was this polygon last seen?
            public DateTime last_seen = DateTime.Now;

            // dominant orientation of the polygon
            public float[] orientation = new float[4];

            // colour used to display this polygon
            public byte[] colour = new byte[3];

            // how many times has this polygon been seen ?
            public long persistence = 1;

            public long av_radius;
        }

        // random number generator
        private Random rnd = new Random();

        // the number of seconds which must elapse before a polygon is
        // removed from the tracking list if not seen
        public int timeout_milliseconds = 2000;

        // minimum width or height of the polygon for it to be considered worth tracking
        public int minimum_dimension = 80;

        // polygons currently being tracked
        private ArrayList tracked = new ArrayList();

        #region "updating"

        /// <summary>
        /// update the position and orientation of all tracked polygons
        /// </summary>
        private void UpdatePositionOrientation()
        {
            DateTime current_t = DateTime.Now;

            for (int j = tracked.Count - 1; j >= 0; j--)
            {
                polygon2DTrackerData polytrack = (polygon2DTrackerData)tracked[j];

                // time since the last sighting
                TimeSpan dt = current_t.Subtract(polytrack.last_seen);
                float seconds_elapsed = (float)dt.TotalSeconds;

                if (dt.TotalMilliseconds > timeout_milliseconds)
                {
                    // remove a polygon if it hasn't been seen for a while
                    tracked.RemoveAt(j);
                }
                else
                {
                    // predict the new state
                    float new_centre_x = polytrack.centre_x + (seconds_elapsed * polytrack.vx);
                    float new_centre_y = polytrack.centre_y + (seconds_elapsed * polytrack.vy);
                    //float new_angular = polytrack.orientation + (seconds_elapsed * polytrack.v_angular);

                    // update the state
                    polytrack.centre_x = new_centre_x;
                    polytrack.centre_y = new_centre_y;
                    //polytrack.orientation = new_angular;
                }
            }
        }

        public void Update(ArrayList polygons)
        {
            DateTime current_t = DateTime.Now;

            // forward prediction: update the state of all tracked polygons
            UpdatePositionOrientation();

            // matching: do any of the tracked polygons match what we can currently see?
            for (int i = 0; i < polygons.Count; i++)
            {
                polygon2D poly = (polygon2D)polygons[i];

                float poly_width = poly.right() - poly.left();
                float poly_height = poly.bottom() - poly.top();
                float av_radius = (poly_width + poly_height) / 4.0f;
                float[] orient = poly.getOrientations();
                //float[] grad = poly.getGradients();

                if ((poly_width > minimum_dimension) &&
                    (poly_height > minimum_dimension))
                {
                    // find the centre of the polygon
                    float centre_x = 0, centre_y = 0;
                    poly.getCentreOfGravity(ref centre_x, ref centre_y);

                    // are any existing centre points close to this?
                    float min_displacement = 9999;
                    int index = -1;
                    for (int j = 0; j < tracked.Count; j++)
                    {
                        polygon2DTrackerData polytrack = (polygon2DTrackerData)tracked[j];

                        // exclusion zone
                        float perimeter_radius = polytrack.av_radius * 1.4f / polytrack.persistence;

                        float cx = polytrack.centre_x;
                        float dx = Math.Abs(cx - centre_x);
                        if (dx < perimeter_radius)
                        {
                            float cy = polytrack.centre_y;
                            float dy = Math.Abs(cy - centre_y);
                            if (dy < perimeter_radius)
                            {
                                float displacement = (float)Math.Sqrt((dx * dx) + (dy * dy));
                                if ((displacement < min_displacement) ||
                                    (min_displacement == 9999))
                                {
                                    min_displacement = displacement;
                                    index = j;
                                }
                            }
                        }
                    }

                    if (index > -1)
                    {
                        polygon2DTrackerData matched = (polygon2DTrackerData)tracked[index];

                        // update the position and velocities
                        float dx = centre_x - matched.centre_x;
                        float dy = centre_y - matched.centre_y;
                        //float dorient = orient - matched.orientation;
                        TimeSpan dt_seconds = current_t.Subtract(matched.last_seen);

                        if (dt_seconds.TotalSeconds > 0)
                        {
                            // use running averages here to smooth out error
                            float momentum = 0.99f;
                            matched.vx = (matched.vx * momentum) + ((dx / (float)dt_seconds.TotalSeconds) * (1.0f - momentum));
                            matched.vy = (matched.vy * momentum) + ((dy / (float)dt_seconds.TotalSeconds) * (1.0f - momentum));
                            //matched.v_angular = ((matched.v_angular * momentum) + (dorient / (float)dt_seconds.TotalSeconds) * (1.0f - momentum));
                        }

                        matched.centre_x = centre_x;
                        matched.centre_y = centre_y;

                        for (int k = 0; k < 4; k++)
                        {
                            matched.orientation[k] = orient[k];
                        }


                        // find the closest orientation
                        /*
                        float min_diff = 9999;
                        int closest_index = -1;
                        for (int k = 0; k < 4; k++)
                        {
                            float diff = Math.Abs(orient[k] - matched.orientation);
                            if (diff < min_diff)
                            {
                                min_diff = diff;
                                closest_index = k;
                            }
                        }

                        // update the orientation
                        if (closest_index > -1)
                        {
                            matched.orientation = (orient[closest_index] * 0.01f) + (matched.orientation * 0.99f);
                        }
                         */
                        
                        matched.last_seen = current_t;
                        matched.persistence++;
                        matched.av_radius += (long)av_radius;

                        // avoid overflows
                        if (matched.persistence > 99999)
                        {
                            matched.persistence /= 2;
                            matched.av_radius /= 2;
                        }
                    }
                    else
                    {
                        /*
                        float min_orient = 9999;
                        for (int k = 0; k < 4; k++)
                        {
                            if (orient[k] < min_orient)
                            {
                                min_orient = orient[k];
                            }
                        }
                         */

                        // add a new tracked polygon to the list
                        polygon2DTrackerData new_poly = new polygon2DTrackerData();
                        new_poly.centre_x = centre_x;
                        new_poly.centre_y = centre_y;
                        new_poly.orientation = orient;
                        new_poly.colour[0] = (byte)(100 + rnd.Next(155));
                        new_poly.colour[1] = (byte)(100 + rnd.Next(155));
                        new_poly.colour[2] = (byte)(100 + rnd.Next(155));
                        new_poly.av_radius = (long)av_radius;
                        tracked.Add(new_poly);
                    }
                }
            }
        }

        #endregion

        #region "display"

        /// <summary>
        /// show the tracked polygons within the given image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void Show(byte[] img, int img_width, int img_height, int minimum_persistence)
        {
            for (int i = 0; i < tracked.Count; i++)
            {
                polygon2DTrackerData poly = (polygon2DTrackerData)tracked[i];

                if (poly.persistence > minimum_persistence)
                {
                    int x = (int)poly.centre_x;
                    int y = (int)poly.centre_y;
                    int perimeter_radius = (int)(poly.av_radius / poly.persistence);

                    drawing.drawCircle(img, img_width, img_height,
                                       x, y, perimeter_radius,
                                       poly.colour[0], poly.colour[1], poly.colour[2], 1);

                    for (int k = 0; k < 4; k++)
                    {
                        int x2 = x + (int)(perimeter_radius * Math.Sin(poly.orientation[k]));
                        int y2 = y - (int)(perimeter_radius * Math.Cos(poly.orientation[k]));
                        drawing.drawLine(img, img_width, img_height,
                                         x, y, x2, y2,
                                         poly.colour[0], poly.colour[1], poly.colour[2],
                                         1, false);
                    }
                }
            }
        }

        #endregion
    }
}
