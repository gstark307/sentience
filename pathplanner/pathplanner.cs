/*
    Sentience 3D Perception System: path planning
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
        public int max_search_range_mm = 500;

        // pointer to the navigable space map within an occupancy grid object
        public bool[][] navigable_space;

        // whether to apply smoothing to the path or not
        public bool pathSmoothing = true;

        // position of the occupancy grid
        public float OccupancyGridCentre_x_mm, OccupancyGridCentre_y_mm;

        // an array which indicates how safe the available navigable space is
        // safe areas are as far as possible from areas of high occupancy probability
        private Byte[][] navigable_safety;
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
        public pathplanner(bool[][] navigable_space, int cellSize_mm,
                           float OccupancyGridCentre_x_mm, float OccupancyGridCentre_y_mm)
        {
            this.cellSize_mm = cellSize_mm;
            init(navigable_space, OccupancyGridCentre_x_mm, OccupancyGridCentre_y_mm);

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
            navigable_safety = new Byte[safe_dimension][];
            for (int j = 0; j < navigable_safety.Length; j++)
                navigable_safety[j] = new Byte[safe_dimension];

            pathfinder = new AStar_PathFinderFast(navigable_safety);
        }

        /// <summary>
        /// set the navigable space and the centre position of the grid
        /// </summary>
        /// <param name="navigable_space"></param>
        /// <param name="OccupancyGridCentre_x_mm"></param>
        /// <param name="OccupancyGridCentre_y_mm"></param>
        public void init(bool[][] navigable_space,
                    float OccupancyGridCentre_x_mm, float OccupancyGridCentre_y_mm)
        {
            this.navigable_space = navigable_space;
            this.OccupancyGridCentre_x_mm = OccupancyGridCentre_x_mm;
            this.OccupancyGridCentre_y_mm = OccupancyGridCentre_y_mm;
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
                    navigable_space[x][y] = true;
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
                            if (!navigable_space[xx][yy])
                                found = true;
                        if (!found)
                        {
                            xx = x + radius;
                            if (xx < dimension)
                                if (!navigable_space[xx][yy])
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
                            if (!navigable_space[xx][yy])
                                found = true;
                        if (!found)
                        {
                            yy = y + radius;
                            if (yy < dimension)
                                if (!navigable_space[xx][yy])
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
                    if (navigable_space[x][y])
                    {
                        Byte safety = 255;
                        int closest = closestObstacle(x, y, max_range_cells);
                        if (closest > -1)
                            safety = (Byte)(155 + (closest * 100 / max_range_cells));

                        navigable_safety[x + safety_offset][y + safety_offset] = safety;
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
        public List<float> CreatePlan(float start_x_mm, float start_y_mm,
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
            List<float> result = new List<float>();
            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    AStar_PathFinderNode node = path[i];
                    float x = (float)((((node.X - safety_offset) - (dimension / 2)) * cellSize_mm) + OccupancyGridCentre_x_mm);
                    float y = (float)((((node.Y - safety_offset) - (dimension / 2)) * cellSize_mm) + OccupancyGridCentre_y_mm);
                    result.Add(x);
                    result.Add(y);
                }

                if (pathSmoothing)
                {
                    ArrayList spline_x = new ArrayList();
                    ArrayList spline_y = new ArrayList();
                    float[] pts = new float[4*2];

                    int interval = 8;
                    for (int i = 0; i < result.Count; i += interval*2)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            int idx = i - (2 * interval * 2) + (j * interval * 2);
                            if (idx < 0) idx = 0;
                            if (idx >= result.Count-1) idx = result.Count - 2;
                            pts[j * 2] = (float)result[idx];
                            pts[(j * 2) + 1] = (float)result[idx + 1];
                        }
                        
                        double temp = Math.Sqrt(Math.Pow(pts[2*2] - pts[1*2], 2F) + Math.Pow(pts[(2*2)+1] - pts[(1*2)+1], 2F));
                        int interpol = System.Convert.ToInt32(temp);

                        SplinePoint(pts, interpol, spline_x, spline_y);
                    }

                    if (spline_x.Count > 1)
                    {
                        List<float> smoothed_result = new List<float>();
                        smoothed_result.Add((float)result[0]);
                        smoothed_result.Add((float)result[1]);
                        for (int i = 1; i < spline_x.Count; i++)
                        {
                            float x = Convert.ToSingle((double)spline_x[i]);
                            float y = Convert.ToSingle((double)spline_y[i]);
                            smoothed_result.Add(x);
                            smoothed_result.Add(y);
                        }
                        smoothed_result.Add((float)result[result.Count - 2]);
                        smoothed_result.Add((float)result[result.Count - 1]);
                        result = smoothed_result;                      
                    }
                }
            }
            return (result);
        }

        #endregion

        #region "curve smoothing"

        /// <summary>
        /// B spline curve calculation
        /// </summary>
        /// <param name="pts">contains 4 successive points to be smoothed</param>
        /// <param name="divisions">interpolation</param>
        /// <param name="spline_x">x coordinates returned</param>
        /// <param name="spline_y">y coordinates returned</param>
        private void SplinePoint(float[] pts, 
                                int divisions, 
                                ArrayList spline_x, ArrayList spline_y)
        {
            double[] a = new double[5];
            double[] b = new double[5];
            a[0] = (-pts[0*2] + 3 * pts[1*2] - 3 * pts[2*2] + pts[3*2]) / 6.0;
            a[1] = (3 * pts[0*2] - 6 * pts[1*2] + 3 * pts[2*2]) / 6.0;
            a[2] = (-3 * pts[0*2] + 3 * pts[2*2]) / 6.0;
            a[3] = (pts[0*2] + 4 * pts[1*2] + pts[2*2]) / 6.0;
            b[0] = (-pts[(0*2)+1] + 3 * pts[(1*2)+1] - 3 * pts[(2*2)+1] + pts[(3*2)+1]) / 6.0;
            b[1] = (3 * pts[(0*2)+1] - 6 * pts[(1*2)+1] + 3 * pts[(2*2)+1]) / 6.0;
            b[2] = (-3 * pts[(0*2)+1] + 3 * pts[(2*2)+1]) / 6.0;
            b[3] = (pts[(0*2)+1] + 4 * pts[(1*2)+1] + pts[(2*2)+1]) / 6.0;

            if (spline_x.Count == 0)
            {
                spline_x.Add((double)0.0);
                spline_y.Add((double)0.0);
            }

            spline_x[0] = a[3];
            spline_y[0] = b[3];

            for (int i = 1; i < divisions; i++)
            {
                float t = Convert.ToSingle(i) / Convert.ToSingle(divisions);
                spline_x.Add((double)((a[2] + t * (a[1] + t * a[0])) * t + a[3]));
                spline_y.Add((double)((b[2] + t * (b[1] + t * b[0])) * t + b[3]));
            }
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

                    if (navigable_space[cell_x][cell_y])
                    {
                        Byte safety = navigable_safety[cell_x + safety_offset][cell_y + safety_offset];
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
            for (int i = 0; i < plan.Count; i+=2)
            {
                float px = (float)plan[i];
                float py = (float)plan[i+1];
                float cell_x = (float)((px - OccupancyGridCentre_x_mm) / (float)cellSize_mm) + (dimension_cells / 2);
                float cell_y = (float)((py - OccupancyGridCentre_y_mm) / (float)cellSize_mm) + (dimension_cells / 2);
                int x = (int)(cell_x * width / dimension_cells);
                int y = (int)(cell_y * height / dimension_cells);

                if (i > 0)
                    drawLine(img, width, height, x, y, prev_x, prev_y, 0, 255, 0, 0, false);

                prev_x = x;
                prev_y = y;
            }
        }

        #endregion
    }
}
