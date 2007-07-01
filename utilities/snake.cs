/*
    Snake algorithm originally used for motion segmentation on the Rodney humanoid
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
using sluggish.utilities;

namespace sluggish.imageprocessing
{
    public class snake
    {
        public polygon2D shape;

        // number of points in the snake
        public int no_of_points;
        
        // array storing the point positions
        private float[,] SnakePoint;

        // constants used with SnakePoint array
        private const int SNAKE_X = 0;
        private const int SNAKE_Y = 1;

        // how many points are no longer moving ?
        private int SnakeStationaryPoints;
        private int prevSnakeStationaryPoints;
        private int snakeStationary;

        // elasticity between adjacent points
        public float elasticity = 0.2f;

        // tendency to move towards the centre of gravity
        public float gravity = 0.05f;

        // constructor
        public snake(int no_of_points)
        {
            this.no_of_points = no_of_points;
            SnakePoint = new float[no_of_points, 2];
        }

        /// <summary>
        /// returns the centre of gravity of all snake points
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CentreOfGravity(ref float x, ref float y)
        {
            x = 0;
            y = 0;

            for (int i = 0; i < no_of_points; i++)
            {
                x += SnakePoint[i, SNAKE_X];
                y += SnakePoint[i, SNAKE_Y];
            }
            if (no_of_points > 0)
            {
                x /= no_of_points;
                y /= no_of_points;
            }
        }

        private bool Update(bool[,] binary_image, bool BlackOnWhite,
                           float elasticity, float gravity,
                           Random rnd)
        {
            int i,j;
            int[] index = new int[3];
            float[] x = new float[3];
            float[] y = new float[3];
            float mid_x, mid_y, dxx, dyy;
            float dx, dy, px, py, segmentLength;

            // dimensions of the binary image
            int width = binary_image.GetLength(0);
            int height = binary_image.GetLength(1);

            SnakeStationaryPoints = 0;

            // get the centre of gravity position
            float centre_x = 0, centre_y = 0;
            CentreOfGravity(ref centre_x, ref centre_y);

            for (i = 0; i < no_of_points; i++)
            {
                px = SnakePoint[i, SNAKE_X];
                py = SnakePoint[i, SNAKE_Y];
                if (px < 0) px = 0;
                if (py < 0) py = 0;
                if (px > width-1) px = width-1;
                if (py > height-1) py=height-1;
                if (((BlackOnWhite) && (binary_image[(int)px, (int)py])) ||
                    ((!BlackOnWhite) && (!binary_image[(int)px, (int)py])))
                {
                    index[1] = i;
                    if (i < no_of_points-1)
                        index[2] = i+1;
                    else
                        index[2] = 0;
                    if (i > 0)
                        index[0] = i-1;
                    else
                        index[0] = no_of_points - 1;

                    for (j = 0; j < 3; j++)
                    {
                        x[j] = SnakePoint[index[j], SNAKE_X];
                        y[j] = SnakePoint[index[j], SNAKE_Y];
                    }

                    dxx = x[2] - x[1];
                    dyy = y[2] - y[1];
                    segmentLength = (float)Math.Sqrt((dxx*dxx)+(dyy*dyy));

                    if (segmentLength > 0.001f)
                    {
                        // mid point between adjacent snake points
                        mid_x = x[0] + ((x[2] - x[0])/2);
                        mid_y = y[0] + ((y[2] - y[0])/2);

                        // distance to the mid point
                        dx = x[1] - mid_x;
                        dy = y[1] - mid_y;

                        // move towards the centre of gravity
                        float dx_centre = x[1] - centre_x;
                        float dy_centre = y[1] - centre_y;

                        px = x[1] - (dx * elasticity * (rnd.Next(10000)/10000.0f));
                        px += x[1] - (dx_centre * gravity * (rnd.Next(10000) / 10000.0f));
                        py = y[1] - (dy * elasticity * (rnd.Next(10000)/10000.0f));
                        py += y[1] - (dy_centre * gravity * (rnd.Next(10000) / 10000.0f));
                        if ((px >= 0) && (px < width) && (py >= 0) && (py < height))
                        {
                            SnakePoint[i, SNAKE_X] = px;
                            SnakePoint[i, SNAKE_Y] = py;
                        }
                        else
                        {
                            SnakeStationaryPoints++;
                        }
                    }
                    else
                    {
                        SnakeStationaryPoints++;
                    }
                }
                else
                {
                    SnakeStationaryPoints++;
                }
            }

            if (prevSnakeStationaryPoints == SnakeStationaryPoints)
                // nothing seems to be occuring
                snakeStationary++;
            else
                snakeStationary = 0;

            prevSnakeStationaryPoints = SnakeStationaryPoints;

            if (((SnakeStationaryPoints / (float)no_of_points) > 0.8f) && (snakeStationary > 5))
                return(true);
            else
                return(false);
        }

        /// <summary>
        /// initialise the snake based upon a polygon shape
        /// </summary>
        /// <param name="binary_image"></param>
        /// <param name="BlackOnWhite"></param>
        /// <param name="initial_points"></param>
        /// <param name="max_itterations"></param>
        /// <param name="rnd"></param>
        public void Snake(bool[,] binary_image, bool BlackOnWhite,
                          polygon2D initial_points,
                          int max_itterations,
                          Random rnd)
        {
            if (initial_points.x_points.Count > 1)
            {
                // get the perimeter length of the initial shape
                float perimeter_length = initial_points.getPerimeterLength();

                // distribute points evenly along the perimeter
                int side_index = 0;
                float side_length_total = 0;
                for (int i = 0; i < no_of_points; i++)
                {
                    // position of this point along the perimeter
                    float perimeter_position = i * perimeter_length / no_of_points;

                    float total = side_length_total;
                    while (total < perimeter_position)
                    {
                        side_length_total = total;
                        total += initial_points.getSideLength(side_index);
                        if (total < perimeter_position) side_index++;
                    }

                    float side_length = initial_points.getSideLength(side_index);
                    if (side_length > 0)
                    {
                        float perimeter_diff = perimeter_position - side_length_total;
                        float fraction = perimeter_diff / side_length;
                        float tx = 0, ty = 0, bx = 0, by = 0;
                        initial_points.getSidePositions(side_index, ref tx, ref ty, ref bx, ref by);
                        float dx = bx - tx;
                        float dy = by - ty;
                        SnakePoint[i, SNAKE_X] = tx + (dx * fraction);
                        SnakePoint[i, SNAKE_Y] = ty + (dy * fraction);
                    }
                }

                Snake(binary_image, BlackOnWhite, max_itterations, rnd);
            }
        }


        public void Snake(bool[,] binary_image, bool BlackOnWhite, 
                          int tx, int ty, int bx, int by,
                          int max_itterations,
                          Random rnd)
        {
            int w = bx - tx;
            int h = by - ty;

            //initialise the snake as a square the same size as the image
            int sidePoints = no_of_points / 4;
            for (int i = 0; i < sidePoints; i++)
            {
                for (int j = 0; j <= 4; j++)
                {
                    switch (j)
                    {
                        case 0:
                            {
                                SnakePoint[i, SNAKE_X] = tx + ((w / (sidePoints + 1)) * i);
                                SnakePoint[i, SNAKE_Y] = ty;
                                break;
                            }
                        case 1:
                            {
                                SnakePoint[i + (sidePoints * j), SNAKE_X] = tx + w;
                                SnakePoint[i + (sidePoints * j), SNAKE_Y] = ty + ((h / (sidePoints + 1)) * i);
                                break;
                            }
                        case 2:
                            {
                                SnakePoint[i + (sidePoints * j), SNAKE_X] = tx + w - ((w / (sidePoints + 1)) * i);
                                SnakePoint[i + (sidePoints * j), SNAKE_Y] = ty + h;
                                break;
                            }
                        case 3:
                            {
                                SnakePoint[i + (sidePoints * j), SNAKE_X] = tx;
                                SnakePoint[i + (sidePoints * j), SNAKE_Y] = ty + h - ((h / (sidePoints + 1)) * i);
                                break;
                            }
                    }
                }
            }

            Snake(binary_image, BlackOnWhite, max_itterations, rnd);
        }

        private void Snake(bool[,] binary_image, bool BlackOnWhite,
                          int max_itterations,
                          Random rnd)
        {
            bool SnakeComplete = false;

            //set the initial parameters for the snake
            prevSnakeStationaryPoints = 0;
            snakeStationary = 0;

            // itterate until the snake can get no smaller
            int i = 0;
            while ((!SnakeComplete) && (i < max_itterations))
            {
                SnakeComplete = Update(binary_image, BlackOnWhite, elasticity, gravity, rnd);
                i++;
            }

            // create a new polygon shape
            shape = new polygon2D();
            for (i = 0; i < no_of_points; i++)
                shape.Add((int)SnakePoint[i, SNAKE_X], (int)SnakePoint[i, SNAKE_Y]);
        }

    }
}
