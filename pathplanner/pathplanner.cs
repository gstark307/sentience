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
        public int max_search_range_mm = 2000;

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

        #region "update navigable areas"

        /// <summary>
        /// returns the distance in grid cells to the nearest obstacle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="max_range_cells">maximum search range in cells</param>
        /// <returns>distance in grid cells</returns>
        private int closestObstacle(int x, int y, int max_range_cells)
        {
            int dimension = navigable_space.GetLength(0);
            int radius = 1;
            int xx = 0, yy = 0;
            bool found = false;
            while ((radius <= max_range_cells) && (!found))
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
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
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
            for (int i = 0; i < path.Count; i++)
            {
                AStar_PathFinderNode node = path[i];
                int x = ((node.X - safety_offset) - (dimension/2)) * cellSize_mm;
                int y = ((node.Y - safety_offset) - (dimension/2)) * cellSize_mm;
                PathPoint pt = new PathPoint(x, y);
                result.Add(pt);
            }
            return (result);
        }

        #endregion
    }
}
