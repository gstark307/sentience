/*
    Sentience 3D Perception System: path planning
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
using System.Collections.Generic;

namespace sentience.pathplanner
{
    /// <summary>
    /// creates plans for safe routes through the environment
    /// </summary>
    public class pathplanner
    {
        // size of each cell in the grid in millimetres
        public int cellSize_mm;

        // the maximum search range when updating safe navigation data
        public int max_search_range_mm = 600;

        // pointer to the navigable space map within an occupancy grid object
        private bool[,] navigable_space;

        // position of the occupancy grid
        public float OccupancyGridCentre_x_mm, OccupancyGridCentre_y_mm;

        // an array which indicates how safe the available navigable space is
        // safe areas are as far as possible from areas of high occupancy probability
        private Byte[,] navigable_safety;
        private int safety_offset = 0;

        // A* path finder object
        private AStar_IPathFinder pathfinder;

        #region "initialisation"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="navigable_space">2D binary array of navigable (probably vacant) space</param>
        /// <param name="cellSize_mm">size of each occupancy grid cell in millimetres</param>
        /// <param name="OccupancyGridCentre_x_mm">location of the centre of the occupancy grid in millimetres</param>
        /// <param name="OccupancyGridCentre_y_mm">location of the centre of the occupancy grid in millimetres</param>
        public pathplanner(bool[,] navigable_space, int cellSize_mm,
                           float OccupancyGridCentre_x_mm, float OccupancyGridCentre_y_mm)
        {
            this.cellSize_mm = cellSize_mm;
            this.navigable_space = navigable_space;
            this.OccupancyGridCentre_x_mm = OccupancyGridCentre_x_mm;
            this.OccupancyGridCentre_y_mm = OccupancyGridCentre_y_mm;

            // for efficiency reasons the dimension of the navigable 
            // safety grid must be a power of 2
            int safe_dimension = 1;
            int i = 0;
            bool finished = false;
            while ((i < 100) && (!finished))
            {
                int dimension = (int)Math.Pow(2, i);
                int diff = dimension - navigable_space.GetLength(0);
                if (diff >= 0)
                {
                    safe_dimension = dimension;
                    safety_offset = diff / 2;
                    finished = true;
                }
                i++;
            }

            // create a safety array, to remain friendly            
            navigable_safety = new Byte[safe_dimension, safe_dimension];

            pathfinder = new AStar_PathFinderFast(navigable_safety);
        }

        #endregion

        #region "test functions"

        /// <summary>
        /// defines an area of the map as being navigable
        /// This is only used for testing purposes, since normally this data would come 
        /// directly from an occupancy grid
        /// </summary>
        /// <param name="tx">top left coordinate in cells</param>
        /// <param name="ty">top left coordinate in cells</param>
        /// <param name="bx">bottom right coordinate in cells</param>
        /// <param name="by">bottom right coordinate in cells</param>
        public void AddNavigableSpace(int tx, int ty, int bx, int by)
        {
            for (int x = tx; x <= bx; x++)
                for (int y = ty; y <= by; y++)
                    navigable_space[x, y] = true;
        }

        #endregion

        #region "update navigable areas"

        /// <summary>
        /// returns the distance in grid cells to the nearest obstacle
        /// </summary>
        /// <param name="x">x coordinate to measure from in cells</param>
        /// <param name="y">y coordinate to measure from in cells</param>
        /// <param name="max_range_cells">maximum search range in cells</param>
        /// <returns>distance in grid cells</returns>
        private int closestObstacle(int x, int y, int max_range_cells)
        {
            int dimension = navigable_space.GetLength(0);
            int radius = 1;
            int xx = 0, yy = 0;
            bool found = false;
            while ((radius < max_range_cells) && (!found))
            {
                int i = 0;
                while ((i < radius * 2) && (!found))
                {
                    xx = x - radius;
                    yy = i - radius + y;
                    if ((yy > -1) && (yy < dimension))
                    {
                        if (xx > -1)
                            if (!navigable_space[xx, yy])
                                found = true;
                        if (!found)
                        {
                            xx = x + radius;
                            if (xx < dimension)
                                if (!navigable_space[xx, yy])
                                    found = true;
                        }
                    }
                    i++;
                }

                i = 0;
                while ((i < radius * 2) && (!found))
                {
                    xx = i - radius + x;
                    yy = y - radius;
                    if ((xx > -1) && (xx < dimension))
                    {
                        if (yy > -1)
                            if (!navigable_space[xx, yy])
                                found = true;
                        if (!found)
                        {
                            yy = y + radius;
                            if (yy < dimension)
                                if (!navigable_space[xx, yy])
                                    found = true;
                        }
                    }
                    i++;
                }

                radius++;
            }
            if (found)
                return (radius);
            else
                return (-1);
        }

        /// <summary>
        /// update the safety estimates for navigable space within the given region
        /// </summary>
        /// <param name="tx">top left cell of the area to be updated</param>
        /// <param name="ty">top left cell of the area to be updated</param>
        /// <param name="bx">bottom right cell of the area to be updated</param>
        /// <param name="by">bottom right cell of the area to be updated</param>
        public void Update(int tx, int ty, int bx, int by)
        {
            int max_range_cells = max_search_range_mm / cellSize_mm;

            for (int x = tx; x <= bx; x++)
            {
                for (int y = ty; y <= by; y++)
                {
                    if (navigable_space[x, y])
                    {
                        Byte safety = 255;
                        int closest = closestObstacle(x, y, max_range_cells);
                        if (closest > -1)
                            safety = (Byte)(closest * 255 / max_range_cells);

                        navigable_safety[x + safety_offset, y + safety_offset] = safety;
                    }
                }
            }
        }

        #endregion

        #region "planning"

        /// <summary>
        /// plan a safe route through the terrain, using the given start and end locations
        /// </summary>
        /// <param name="start_x_mm">starting x coordinate in mm</param>
        /// <param name="start_y_mm">starting y coordinate in mm</param>
        /// <param name="finish_x_mm">finishing x coordinate in mm</param>
        /// <param name="finish_y_mm">finishing y coordinate in mm</param>
        /// <returns>the path positions in millimetres</returns>
        public ArrayList CreatePlan(float start_x_mm, float start_y_mm,
                                    float finish_x_mm, float finish_y_mm)
        {
            // get the dimension of the occupancy grid
            int dimension = navigable_space.GetLength(0);

            // calculate the start and end points on the safe navigation grid
            // which may differ from the original grid size due to the power of 2 constraint
            PathPoint PathStart = new PathPoint((int)((start_x_mm - OccupancyGridCentre_x_mm) / cellSize_mm) + (dimension / 2) + safety_offset,
                                                (int)((start_y_mm - OccupancyGridCentre_y_mm) / cellSize_mm) + (dimension / 2) + safety_offset);
            PathPoint PathEnd = new PathPoint((int)((finish_x_mm - OccupancyGridCentre_x_mm) / cellSize_mm) + (dimension / 2) + safety_offset,
                                              (int)((finish_y_mm - OccupancyGridCentre_y_mm) / cellSize_mm) + (dimension / 2) + safety_offset);
            
            // calculate a path, if one exists
            List<AStar_PathFinderNode> path = pathfinder.FindPath(PathStart, PathEnd);

            // convert the path into millimetres and store it in an array list
            ArrayList result = new ArrayList();
            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    AStar_PathFinderNode node = path[i];
                    int x = (int)((((node.X - safety_offset) - (dimension / 2)) * cellSize_mm) + OccupancyGridCentre_x_mm);
                    int y = (int)((((node.Y - safety_offset) - (dimension / 2)) * cellSize_mm) + OccupancyGridCentre_y_mm);
                    PathPoint pt = new PathPoint(x, y);
                    result.Add(pt);
                }
            }
            return (result);
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// shows the safety estimates for navigable space
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Show(Byte[] img, int width, int height)
        {
            int dimension_cells = navigable_space.GetLength(0);

            // clear the image
            for (int i = 0; i < width * height * 3; i++)
                img[i] = 0;

            for (int y = 0; y < height; y++)
            {
                int cell_y = y * (dimension_cells - 1) / height;
                for (int x = 0; x < width; x++)
                {
                    int cell_x = x * (dimension_cells - 1) / width;

                    int n = ((y * width) + x) * 3;

                    if (navigable_space[cell_x, cell_y])
                    {
                        Byte safety = navigable_safety[cell_x + safety_offset, cell_y + safety_offset];
                        for (int c = 0; c < 3; c++)
                            img[n + c] = safety;
                    }
                }
            }
        }

        /// <summary>
        /// draws a line within an image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="linewidth"></param>
        /// <param name="overwrite"></param>
        private void drawLine(Byte[] img, int img_width, int img_height,
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
                        int s = 0;
                        while (s * Math.Abs(step_x) <= Math.Abs(dx))
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
                            s++;
                        }
                    }
                }
                else
                {
                    if (dy != 0)
                    {
                        m = dx / (float)dy;
                        y = y1;
                        int s = 0;
                        while (s * Math.Abs(step_y) <= Math.Abs(dy))
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
                            s++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// show a path through the navigable space
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="plan">planned path data</param>
        public void Show(Byte[] img, int width, int height, ArrayList plan)
        {
            Show(img, width, height);

            int dimension_cells = navigable_space.GetLength(0);

            int prev_x = 0, prev_y = 0;
            for (int i = 0; i < plan.Count; i++)
            {
                PathPoint p = (PathPoint)plan[i];
                int cell_x = (int)((p.X - OccupancyGridCentre_x_mm) / cellSize_mm) + (dimension_cells / 2);
                int cell_y = (int)((p.Y - OccupancyGridCentre_y_mm) / cellSize_mm) + (dimension_cells / 2);
                int x = cell_x * width / dimension_cells;
                int y = cell_y * height / dimension_cells;

                if (i > 0)
                    drawLine(img, width, height, x, y, prev_x, prev_y, 0, 255, 0, 1, false);

                prev_x = x;
                prev_y = y;
            }
        }

        #endregion
    }
}
