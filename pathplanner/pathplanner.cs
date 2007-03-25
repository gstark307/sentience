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

        // an array which indicates how safe the available navigable space is
        // safe areas are as far as possible from areas of high occupancy probability
        private Byte[,] navigable_safety;
        private int safety_offset = 0;

        // A* path finder object
        private AStar_IPathFinder pathfinder;

        #region "initialisation"

        public pathplanner(bool[,] navigable_space, int cellSize_mm)
        {
            this.cellSize_mm = cellSize_mm;
            this.navigable_space = navigable_space;

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

        /// <summary>
        /// returns the distance in grid cells to the nearest obstacle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>distance in grid cells</returns>
        private int closestObstacle(int x, int y)
        {
            int dimension = navigable_space.GetLength(0);
            int radius = 1;
            int xx = 0, yy = 0;
            bool found = false;
            while ((radius < max_search_range_mm / cellSize_mm) && (!found))
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
                        int closest = closestObstacle(x, y);
                        if (closest > -1)
                            safety = (Byte)(closest * 255 / max_range_cells);

                        navigable_safety[x + safety_offset, y + safety_offset] = safety;
                    }
                }
            }
        }
    }
}
