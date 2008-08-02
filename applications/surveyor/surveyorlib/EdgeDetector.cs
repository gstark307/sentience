/*
    base class for edge detection
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
using System.Collections.Generic;

namespace sluggish.utilities
{
    /// <summary>
    /// base class for edge detection
    /// </summary>
    public class EdgeDetector
    {
        // list of edge locations
        public List<int> edges;

        // image showing edges
        public byte[] edgesImage;

        // automatically detect appropriate edge detection thresholds
        public bool automatic_thresholds;

        public virtual byte[] Update(byte[] img, int width, int height)
        {
            return (null);
        }

        #region "detecting corner points"

        public List<int> GetCorners(int width, int height)
        {
            List<int> corners = new List<int>();

            int bytes_per_pixel = edgesImage.Length / (width * height);
            int stride = (width - 3) * bytes_per_pixel;

            for (int i = 0; i < edges.Count; i += 2)
            {
                int x = edges[i];
                int y = edges[i + 1];
                if ((x > 0) && (x < width - 1) &&
                    (y > 0) && (y < height - 1))
                {
                    int adjacent_edges = 0;
                    int n = (((y - 1) * width) + x - 1) * bytes_per_pixel;
                    for (int yy = y - 1; yy <= y + 1; yy++)
                    {
                        for (int xx = x - 1; xx <= x + 1; xx++)
                        {
                            if (edgesImage[n] == 0)
                            {
                                adjacent_edges++;
                                if (adjacent_edges > 2)
                                {
                                    xx = x + 2;
                                    yy = y + 2;
                                }
                            }
                            n += 3;
                        }
                        n += stride;
                    }

                    if (adjacent_edges <= 2)
                    {
                        corners.Add(x);
                        corners.Add(y);
                    }
                }
            }
            return (corners);
        }

        public void ConnectBrokenEdges(int maximum_separation,
                                       int width, int height)
        {
            List<int> corners = GetCorners(width, height);
            bool[] connected = new bool[corners.Count];

            for (int i = 0; i < corners.Count - 2; i += 2)
            {
                int x0 = corners[i];
                int y0 = corners[i + 1];
                for (int j = i + 2; j < corners.Count; j += 2)
                {
                    if (!connected[j])
                    {
                        int x1 = corners[j];
                        int dx = x1 - x0;
                        if (dx < 0) dx = -dx;
                        if (dx <= maximum_separation)
                        {
                            int y1 = corners[j + 1];
                            int dy = y1 - y0;
                            if (dy < 0) dy = -dy;
                            if (dy <= maximum_separation)
                            {
                                int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));
                                for (int d = 1; d < dist; d++)
                                {
                                    int ix = x0 + (d * dx / dist);
                                    int iy = y0 + (d * dy / dist);
                                    edges.Add(ix);
                                    edges.Add(iy);
                                }

                                connected[j] = true;
                                j = corners.Count;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
