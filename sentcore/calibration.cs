/*
    Camera calibration class, used for automatic calibration with a pattern of lines
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using sentience.core;

namespace sentience.calibration
{
    public class calibration_point
    {
        public float x, y;

        public calibration_point(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// defines a region of interest containing the calibration pattern
    /// </summary>
    public class calibration_region_of_interest
    {
        public int tx, ty, bx, by;

        public void setTopLeft(int x, int y)
        {
            tx = x;
            ty = y;
        }

        public void setBottomRight(int x, int y)
        {
            bx = x;
            by = y;
        }

        /// <summary>
        /// show the region within the given image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Show(Byte[] img, int width, int height)
        {
            util.drawLine(img, width, height, tx, ty, bx, ty, 0, 255, 0, 0, false);
            util.drawLine(img, width, height, bx, ty, bx, by, 0, 255, 0, 0, false);
            util.drawLine(img, width, height, bx, by, tx, by, 0, 255, 0, 0, false);
            util.drawLine(img, width, height, tx, by, tx, ty, 0, 255, 0, 0, false);
        }
    }

    public class calibration_line
    {
        public ArrayList points = new ArrayList();
        public ArrayList intersections = new ArrayList();

        public void Add(int x, int y)
        {
            points.Add(new calibration_point(x, y));
        }

        public void AddIntersectionPoint(int x, int y)
        {
            intersections.Add(new calibration_point(x, y));
        }

        public void Draw(Byte[] img, int width, int height, int r, int g, int b)
        {
            for (int i = 1; i < points.Count; i++)
            {
                calibration_point p1 = (calibration_point)points[i - 1];
                calibration_point p2 = (calibration_point)points[i];
                util.drawLine(img, width, height, (int)p1.x, (int)p1.y, (int)p2.x, (int)p2.y, r, g, b, 0, false);
            }
        }
    }

    public class calibration_edge : calibration_point
    {
        public int magnitude;
        public bool enabled;
        public int hits = 1;
        public int grid_x, grid_y;
        public float rectified_x, rectified_y;

        public calibration_edge(float x, float y, int magnitude) : base(x,y)
        {
            this.magnitude = magnitude;
            enabled = true;
        }
    }

    public class calibration
    {
        Random rnd = new Random();

        // the position of the centre spot relative to the centre of the calibration pattern
        public const int CENTRE_SPOT_NW = 0;
        public const int CENTRE_SPOT_NE = 1;
        public const int CENTRE_SPOT_SE = 2;
        public const int CENTRE_SPOT_SW = 3;
        public int centre_spot_position = CENTRE_SPOT_NW;

        public int separation_factor = 30;

        // position of the camera relative to the calibration pattern
        public float camera_height_mm = 785;
        public float camera_dist_to_pattern_centre_mm = 450;

        calibration_edge[,] coverage;
        float vertical_adjust = 1.0f;
        float temp_vertical_adjust = 1.0f;

        // the size of each square on the calibration pattern
        public float calibration_pattern_spacing_mm = 50;

        // horizontal field of vision
        public float camera_FOV_degrees = 50;

        public String camera_name = "";
        private int image_width, image_height;
        public float min_RMS_error = 999999;
        public float polyfit_error = 999999;

        private bool isValidRectification;

        // the centre of distortion in image pixel coordinates
        calibration_point centre_of_distortion;
        calibration_point centre_spot;

        public int[] calibration_map;
        public int[] temp_calibration_map;
        public int[,,] calibration_map_inverse;
        public int[, ,] temp_calibration_map_inverse;
        public Byte[] rectified_image;

        public Byte[] corners_image;
        public Byte[] edges_image;
        public Byte[] centrealign_image;
        public Byte[] lines_image;
        public Byte[] curve_fit;

        polyfit fitter, best_curve;
        public float temp_scale, scale=1;

        Byte[] calibration_image;
        ArrayList edges_horizontal;
        ArrayList edges_vertical;
        ArrayList detected_corners;
        ArrayList[] corners;
        calibration_edge[,] grid;
        int corners_index = 0;

        float horizontal_separation_top = 0;
        int horizontal_separation_top_hits = 0;
        float horizontal_separation_bottom = 0;
        int horizontal_separation_bottom_hits = 0;
        float vertical_separation_top = 0;
        int vertical_separation_top_hits = 0;
        float vertical_separation_bottom = 0;
        int vertical_separation_bottom_hits = 0;
        int closest_to_centreline_x = 0;
        int closest_to_centreline_y = 0;
        int av_centreline_x = 0;
        int av_centreline_x_hits = 0;
        int av_centreline_y = 0;
        int av_centreline_y_hits = 0;

        const int no_of_images = 4;
        int binary_image_index = 0;
        bool[, ,] binary_image;
        bool[,] edges_binary;

        int[,] horizontal_magnitude;
        int[,] vertical_magnitude;
        ArrayList horizontal_lines, vertical_lines;

        #region "region of interest"

        public calibration_region_of_interest ROI;

        /// <summary>
        /// sets the top left or bottom right point of the region of interest
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="topLeft"></param>
        public void setRegionOfInterestPoint(int x, int y, bool topLeft)
        {
            if (ROI == null) ROI = new calibration_region_of_interest();
            if (topLeft)
                ROI.setTopLeft(x, y);
            else
                ROI.setBottomRight(x, y);
        }

        #endregion

        #region "centre spot detection"

        /// <summary>
        /// detect the centre spot within the image
        /// </summary>
        /// <param name="calibration_image"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void detectCentreSpot(Byte[] calibration_image, int width, int height,
                                      ref int grid_x, ref int grid_y)
        {
            grid_x = 0;
            grid_y = 0;

            int search_radius_x = width / (separation_factor * 2);
            if (search_radius_x < 2) search_radius_x = 2;
            int search_radius_y = height / (separation_factor * 2);
            if (search_radius_y < 2) search_radius_y = 2;
            int search_radius_x2 = search_radius_x / 2;
            int search_radius_y2 = search_radius_y / 2;

            int tx = width/4;
            int bx = width - 1 - tx;
            int ty = height/4;
            int by = height - 1 - ty;
            if (ROI != null)
            {
                tx = ROI.tx + ((ROI.bx - ROI.tx) / 4);
                bx = ROI.tx + ((ROI.bx - ROI.tx) * 3 / 4);
                ty = ROI.ty + ((ROI.by - ROI.ty) / 4);
                by = ROI.ty + ((ROI.by - ROI.ty) * 3 / 4);
            }

            float max_diff = 0;
            centre_spot = new calibration_point(width / 2, height / 2);
            for (int x = tx; x < bx; x++)
            {
                for (int y = ty; y < by; y++)
                {
                    int inner = 0;
                    int inner_hits = 0;
                    int outer = 0;
                    int outer_hits = 0;
                    for (int xx = x - search_radius_x; xx < x + search_radius_x; xx++)
                    {
                        for (int yy = y - search_radius_y; yy < y + search_radius_y; yy++)
                        {
                            if (coverage[xx, yy] == null)
                            {
                                int n = (yy * width) + xx;

                                if ((xx > x - search_radius_x2) && (xx < x + search_radius_x2) &&
                                    (yy > y - search_radius_y2) && (yy < y + search_radius_y2))
                                {
                                    inner += calibration_image[n];
                                    inner_hits++;
                                }
                                else
                                {
                                    outer += calibration_image[n];
                                    outer_hits++;
                                }
                            }
                        }
                    }
                    if (inner_hits > 0)
                    {
                        float diff = (outer / (float)outer_hits) - (inner / (float)inner_hits);
                        if (diff > max_diff)
                        {
                            max_diff = diff;
                            centre_spot.x = x;
                            centre_spot.y = y;
                        }
                    }
                }
            }

            int cx = (int)centre_spot.x;
            int cy = (int)centre_spot.y;
            switch (centre_spot_position)
            {
                case CENTRE_SPOT_NW:
                    {
                        cx = (int)centre_spot.x + search_radius_x;
                        cy = (int)centre_spot.y + search_radius_y;
                        break;
                    }
                case CENTRE_SPOT_NE:
                    {
                        cx = (int)centre_spot.x - search_radius_x;
                        cy = (int)centre_spot.y + search_radius_y;
                        break;
                    }
                case CENTRE_SPOT_SW:
                    {
                        cx = (int)centre_spot.x + search_radius_x;
                        cy = (int)centre_spot.y - search_radius_y;
                        break;
                    }
                case CENTRE_SPOT_SE:
                    {
                        cx = (int)centre_spot.x - search_radius_x;
                        cy = (int)centre_spot.y - search_radius_y;
                        break;
                    }
            }            

            if (coverage[cx, cy] != null)
            {
                grid_x = coverage[cx, cy].grid_x;
                grid_y = coverage[cx, cy].grid_y;
                util.drawBox(corners_image, width, height, (int)coverage[cx, cy].x, (int)coverage[cx, cy].y, 4, 255, 255, 255, 0);
            }
            else
            {
                int xx = cx - 2;
                int yy = cy;
                while ((xx <= cx + 2) && (coverage[xx, yy] == null))
                {
                    yy = cy - 2;
                    while ((yy <= cy + 2) && (coverage[xx, yy] == null))
                    {
                        yy++;
                    }
                    xx++;
                }
                if (coverage[xx, yy] != null)
                {
                    grid_x = coverage[xx, yy].grid_x;
                    grid_y = coverage[xx, yy].grid_y;
                    util.drawBox(corners_image, width, height, (int)coverage[xx, yy].x, (int)coverage[xx, yy].y, 4, 255, 255, 255, 0);
                }
            }
        }

        #endregion

        #region "corner detection"

        public void ShowRectifiedCorners(Byte[] img, int width, int height)
        {
            int rectified_x = 0, rectified_y = 0;

            if (grid != null)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        if (grid[x, y] != null)
                        {
                            util.drawCross(img, width, height, (int)grid[x, y].rectified_x, (int)grid[x, y].rectified_y, 2, 255, 255, 0, 0);
                            
                            if (rectifyPoint((int)grid[x, y].x, (int)grid[x, y].y, ref rectified_x, ref rectified_y))
                            {
                                //util.drawCross(img, width, height, rectified_x, rectified_y, 2, 255, 255, 255, 0);
                                util.drawLine(img, width, height, (int)grid[x, y].rectified_x, (int)grid[x, y].rectified_y, rectified_x, rectified_y, 255, 255, 255, 0, false);
                            }                            
                        }
                    }
                }
            }
        }

        /// <summary>
        /// detect corners within the grid pattern
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void detectCorners(int width, int height)
        {
            if (corners == null)
            {
                corners = new ArrayList[30];
                for (int i = 0; i < corners.Length; i++)
                    corners[i] = new ArrayList();
            }
            corners[corners_index].Clear();

            for (int i = 0; i < horizontal_lines.Count; i++)
            {
                calibration_line line1 = (calibration_line)horizontal_lines[i];
                calibration_point prev_pt = null;
                for (int j = 0; j < line1.points.Count; j++)
                {
                    calibration_point pt = (calibration_point)line1.points[j];
                    if (j > 0)
                    {
                        for (int i2 = 0; i2 < vertical_lines.Count; i2++)
                        {
                            calibration_line line2 = (calibration_line)vertical_lines[i2];
                            calibration_point prev_pt2 = null;
                            for (int j2 = 0; j2 < line2.points.Count; j2++)
                            {
                                calibration_point pt2 = (calibration_point)line2.points[j2];
                                if (j2 > 0)
                                {
                                    float ix = 0;
                                    float iy = 0;
                                    if (util.intersection((float)prev_pt.x, (float)prev_pt.y, (float)pt.x, (float)pt.y,
                                                          (float)prev_pt2.x, (float)prev_pt2.y, (float)pt2.x, (float)pt2.y,
                                                          ref ix, ref iy))
                                    {
                                        int av = averageIntensity(calibration_image, width, height, (int)ix - 10, (int)iy - 10, (int)ix + 10, (int)iy + 10);
                                        if (av < 170)
                                        {
                                            calibration_edge intersection_point = new calibration_edge(ix, iy, 0);

                                            intersection_point.grid_x = i2;
                                            intersection_point.grid_y = i;
                                            corners[corners_index].Add(intersection_point);
                                            line1.intersections.Add(intersection_point);
                                            line2.intersections.Add(intersection_point);                                                
                                        }
                                    }
                                }
                                prev_pt2 = pt2;
                            }
                        }
                    }
                    prev_pt = pt;
                }
            }

            int grid_tx=9999, grid_ty=9999, grid_bx=0, grid_by=0;
            coverage = new calibration_edge[width, height];
            detected_corners = new ArrayList();
            for (int i = 0; i < corners.Length; i++)
            {
                for (int j = 0; j < corners[i].Count; j++)
                {
                    calibration_edge pt = (calibration_edge)corners[i][j];

                    if (pt.grid_x < grid_tx) grid_tx = pt.grid_x;
                    if (pt.grid_y < grid_ty) grid_ty = pt.grid_y;
                    if (pt.grid_x > grid_bx) grid_bx = pt.grid_x;
                    if (pt.grid_y > grid_by) grid_by = pt.grid_y;
                    
                    if (coverage[(int)pt.x, (int)pt.y] == null)
                    {
                        int radius = 4;
                        for (int xx = (int)pt.x - radius; xx < (int)pt.x + radius; xx++)
                        {
                            if ((xx > -1) && (xx < width))
                            {
                                for (int yy = (int)pt.y - radius; yy < (int)pt.y + radius; yy++)
                                {
                                    if ((yy > -1) && (yy < height))
                                    {
                                        coverage[xx, yy] = pt;
                                    }
                                }
                            }
                        }
                        detected_corners.Add(pt);
                    }
                    else
                    {
                        int xx = (int)pt.x;
                        int yy = (int)pt.y;
                        coverage[xx, yy].x += xx;
                        coverage[xx, yy].y += yy;
                        coverage[xx, yy].hits++;
                        coverage[xx, yy].grid_x = pt.grid_x;
                        coverage[xx, yy].grid_y = pt.grid_y;
                    }
                }
            }

            grid = null;
            if ((grid_bx > grid_tx) && (grid_by > grid_ty))
            {
                grid = new calibration_edge[grid_bx - grid_tx+1, grid_by - grid_ty+1];
            }


            for (int i = 0; i < detected_corners.Count; i++)
            {
                calibration_edge pt = (calibration_edge)detected_corners[i];
                pt.x /= pt.hits;
                pt.y /= pt.hits;
                pt.hits = 1;
                pt.grid_x -= grid_tx;
                pt.grid_y -= grid_ty;
                if (grid != null)
                {
                    grid[pt.grid_x, pt.grid_y] = pt;
                }
            }

            if (grid != null)
            {
                for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                    {
                        calibration_edge pt = (calibration_edge)grid[grid_x, grid_y];
                        if (pt != null)
                            util.drawCross(corners_image, width, height, (int)pt.x, (int)pt.y, 3, 255, 0, 0, 0);
                    }
                }
            }

        }

        /// <summary>
        /// returns the average pixel intensity within the given area
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        private int averageIntensity(Byte[] img, int width, int height, int tx, int ty, int bx, int by)
        {
            int hits = 0;
            int tot = 0;
            for (int x = tx; x < bx; x++)
            {
                if ((x > -1) && (x < width) && (x != (tx + ((bx - tx) / 2))))
                {
                    for (int y = ty; y < by; y++)
                    {
                        if ((y > -1) && (y < height) && (y != (ty + ((by - ty) / 2))))
                        {
                            int n = (y * width) + x;
                            tot += (255 - img[n]);
                            hits++;
                        }
                    }
                }
            }
            if (hits > 0) tot /= hits;
            return (tot);
        }

        #endregion

        #region "edge detection"

        /// <summary>
        /// clear the current binary image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void clearBinaryImage(int width, int height)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    binary_image[binary_image_index, x, y] = false;
        }

        /// <summary>
        /// creates a binary edges image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void updateEdgesImage(int width, int height)
        {
            int start_x = width / 5;
            if (ROI != null) start_x = ROI.tx;
            int end_x = width - start_x;
            if (ROI != null) end_x = ROI.bx;
            int start_y = 5;
            if (ROI != null) start_y = ROI.ty;
            int end_y = height - 5;
            if (ROI != null) end_y = ROI.by;

            if (start_x < 5) start_x = 5;
            if (start_y < 5) start_y = 5;
            if (end_x > width - 5) end_x = width - 5;
            if (end_y > height - 5) end_y = height - 5;

            for (int x = start_x; x < end_x; x++)
            {
                for (int y = start_y; y < end_y; y++)
                {
                    int hits = 0;
                    for (int i = 0; i < no_of_images; i++)
                    {
                        if (binary_image[i, x, y]) hits++;
                    }
                    if (hits > no_of_images - 3)
                    {
                        edges_binary[x, y] = true;

                        int index = -1;
                        if (y < height / 4) index = 0;
                        if (y > height * 3 / 4) index = 1;
                        if (index > -1)
                        {
                            horizontal_magnitude[index, x]++;
                            for (int j = 0; j < 4; j++)
                            {
                                horizontal_magnitude[index, x + j]++;
                                horizontal_magnitude[index, x - j]++;
                            }
                        }
                        index = -1;
                        if (x < width / 2) index = 0;
                        if (x > width / 2) index = 1;
                        if (index > -1)
                        {
                            vertical_magnitude[index, y]++;
                            for (int j = 0; j < 4; j++)
                            {
                                vertical_magnitude[index, y - j]++;
                                vertical_magnitude[index, y + j]++;
                            }
                        }

                        int n = ((y * width) + x) * 3;
                        edges_image[n] = 0;
                        edges_image[n + 1] = (Byte)255;
                        edges_image[n + 2] = 0;
                    }
                    else edges_binary[x, y] = false;
                }
            }

            int search_width = width / separation_factor;
            for (int i = 0; i < 2; i++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int xx = x + 1; xx < x + search_width; xx++)
                    {
                        if ((xx > -1) && (xx < width))
                        {
                            if (horizontal_magnitude[i, xx] < horizontal_magnitude[i, x])
                                horizontal_magnitude[i, xx] = 0;
                            else
                            {
                                horizontal_magnitude[i, x] = 0;
                                xx = width;
                            }
                        }
                    }
                }
                for (int x = width * 9 / 10; x < width; x++)
                {
                    if (horizontal_magnitude[i, x] > 0) horizontal_magnitude[i, x] = 0;
                }
            }


            search_width = height / separation_factor;
            for (int i = 0; i < 2; i++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int yy = y + 1; yy < y + search_width; yy++)
                    {
                        if ((yy > -1) && (yy < height))
                        {
                            if (vertical_magnitude[i, yy] < vertical_magnitude[i, y])
                                vertical_magnitude[i, yy] = 0;
                            else
                            {
                                vertical_magnitude[i, y] = 0;
                                yy = height;
                            }
                        }
                    }
                }
                for (int y = height * 9 / 10; y < height - 1; y++)
                {
                    if (vertical_magnitude[i, y] > 0) vertical_magnitude[i, y] = 0;
                }
            }
        }

        private void detectHorizontalEdges(Byte[] calibration_image, int width, int height, ArrayList edges, int min_magnitude)
        {
            int inhibit_radius = width / separation_factor;
            int search_radius = 2;
            ArrayList temp_edges = new ArrayList();

            edges.Clear();
            for (int y = 1; y < height - 1; y++)
            {
                temp_edges.Clear();
                for (int x = search_radius; x < width - search_radius; x++)
                {
                    int val1 = 0;
                    int val2 = 0;
                    int n = (y * width) + x - search_radius;
                    for (int xx = x - search_radius; xx < x + search_radius; xx++)
                    {
                        if (xx < x)
                            val1 += calibration_image[n];
                        else
                            val2 += calibration_image[n];
                        n++;
                    }
                    val1 /= search_radius;
                    val2 /= search_radius;
                    int magnitude = Math.Abs(val1 - val2);
                    if (magnitude > min_magnitude)
                    {
                        calibration_edge new_edge = new calibration_edge(x, y, magnitude);
                        temp_edges.Add(new_edge);
                    }
                }

                // perform non-maximal supression
                for (int i = 0; i < temp_edges.Count; i++)
                {
                    calibration_edge e1 = (calibration_edge)temp_edges[i];
                    if (e1.enabled)
                    {
                        for (int j = i + 1; j < temp_edges.Count; j++)
                        {
                            if (i != j)
                            {
                                calibration_edge e2 = (calibration_edge)temp_edges[j];
                                if (e2.enabled)
                                {
                                    int dx = (int)(e2.x - e1.x);
                                    if (dx < 0) dx = -dx;
                                    if (dx < inhibit_radius)
                                    {
                                        if (e1.magnitude > e2.magnitude)
                                            e2.enabled = false;
                                        else
                                        {
                                            e1.enabled = false;
                                            j = temp_edges.Count;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // the survivors linger on
                for (int i = 0; i < temp_edges.Count; i++)
                {
                    calibration_edge e = (calibration_edge)temp_edges[i];
                    if (e.enabled)
                    {
                        edges.Add(e);
                        for (int xx = (int)e.x - 1; xx < (int)e.x; xx++)
                            for (int yy = (int)e.y - 1; yy < (int)e.y; yy++)
                                binary_image[binary_image_index, xx, yy] = true;
                    }
                }
            }
        }

        private void detectVerticalEdges(Byte[] calibration_image, int width, int height, ArrayList edges, int min_magnitude)
        {
            int inhibit_radius = height / separation_factor;
            int search_radius = 2;
            ArrayList temp_edges = new ArrayList();

            edges.Clear();
            for (int x = 1; x < width - 1; x++)
            {
                temp_edges.Clear();
                for (int y = search_radius; y < height - search_radius; y++)
                {
                    int val1 = 0;
                    int val2 = 0;                    
                    for (int yy = y - search_radius; yy < y + search_radius; yy++)
                    {
                        int n = (yy * width) + x;
                        if (yy < y)
                            val1 += calibration_image[n];
                        else
                            val2 += calibration_image[n];
                    }
                    val1 /= search_radius;
                    val2 /= search_radius;
                    int magnitude = Math.Abs(val1 - val2);
                    if (magnitude > min_magnitude)
                    {
                        calibration_edge new_edge = new calibration_edge(x, y, magnitude);
                        temp_edges.Add(new_edge);
                    }
                }

                // perform non-maximal supression
                for (int i = 0; i < temp_edges.Count; i++)
                {
                    calibration_edge e1 = (calibration_edge)temp_edges[i];
                    if (e1.enabled)
                    {
                        for (int j = i + 1; j < temp_edges.Count; j++)
                        {
                            if (i != j)
                            {
                                calibration_edge e2 = (calibration_edge)temp_edges[j];
                                if (e2.enabled)
                                {
                                    int dy = (int)(e2.y - e1.y);
                                    if (dy < 0) dy = -dy;
                                    if (dy < inhibit_radius)
                                    {
                                        if (e1.magnitude > e2.magnitude)
                                            e2.enabled = false;
                                        else
                                        {
                                            e1.enabled = false;
                                            j = temp_edges.Count;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // the survivors linger on
                for (int i = 0; i < temp_edges.Count; i++)
                {
                    calibration_edge e = (calibration_edge)temp_edges[i];
                    if (e.enabled)
                    {
                        edges.Add(e);
                        for (int xx = (int)e.x - 1; xx < (int)e.x; xx++)
                            for (int yy = (int)e.y - 1; yy < (int)e.y; yy++)
                                binary_image[binary_image_index, xx, yy] = true;
                    }
                }
            }
        }

        #endregion

        #region "old stuff"
        /// <summary>
        /// return the grid coordinate given the screen coordinate
        /// </summary>
        /// <param name="image_x"></param>
        /// <param name="image_y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="grid_x"></param>
        /// <param name="grid_y"></param>
        /*
        private void getGridCoordinate(int image_x, int image_y,
                                       int width, int height,
                                       ref int grid_x, ref int grid_y)
        {
            if ((horizontal_separation_top_hits > 0) && 
                (horizontal_separation_bottom_hits > 0))
            {
                // get the horizontal separation at the given y coordinate
                int separation_top = (int)(horizontal_separation_top / horizontal_separation_top_hits);
                int separation_bottom = (int)(horizontal_separation_bottom / horizontal_separation_bottom_hits);
                int grid_dx = separation_top + (image_y * (separation_bottom - separation_top) / height);
                //int grid_dy = grid_dx * height / width;
                int av_separation_x = (separation_top + separation_bottom) / 2;
                int centre_adjust_x = av_centreline_x / av_centreline_x_hits;
                if (centre_adjust_x > av_separation_x / 2) centre_adjust_x -= (av_separation_x / 2);
                grid_x = -(int)Math.Round(((((image_x - (width / 2.0)) / (double)grid_dx)) + 0.5 + (centre_adjust_x / (double)av_separation_x)));

                int av_separation_y = av_separation_x;
                float fraction = separation_top / (float)separation_bottom;
                //fraction = fraction * width / height;
                int min_separation = (int)(av_separation_y * (1.0f - (fraction / 2)));
                int max_separation = (int)(av_separation_y * (1.0f - (fraction / 2) + (fraction * fraction)));
                int grid_dy = min_separation + (image_y * (max_separation - min_separation) / height);

                int centre_adjust_y = av_centreline_y / av_centreline_y_hits;
                if (centre_adjust_y > av_separation_y / 2) centre_adjust_y -= (av_separation_y / 2);
                grid_y = -(int)Math.Round(((((image_y - (height / 2.0)) / (double)grid_dy)) + 0.0) - 0.0 + (centre_adjust_y / (double)av_separation_y));
            }
        }
        /// <summary>
        /// draw the detected grid
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void drawGrid(Byte[] img, int width, int height)
        {
            int grid_x = 0, grid_y = 0;
            int prev_grid_x, prev_grid_y;
            for (int y = 0; y < height; y++)
            {                
                prev_grid_x = -9999;
                for (int x = 0; x < width; x++)
                {
                    getGridCoordinate(x, y, width, height, ref grid_x, ref grid_y);
                    if (grid_x != prev_grid_x)
                    {
                        int n = ((y * width) + x) * 3;
                        img[n] = 0;
                        img[n+1] = (Byte)255;
                        img[n + 2] = 0;
                    }
                    prev_grid_x = grid_x;
                }
            }
            for (int x = 0; x < width; x++)            
            {
                grid_y = 0;
                prev_grid_y = -9999;
                for (int y = 0; y < height; y++)
                {
                    getGridCoordinate(x, y, width, height, ref grid_x, ref grid_y);
                    if (grid_y != prev_grid_y)
                    {
                        int n = ((y * width) + x) * 3;
                        img[n] = 0;
                        img[n + 1] = (Byte)255;
                        img[n + 2] = 0;
                    }
                    prev_grid_y = grid_y;
                }
            }
        }
         
         */

        #endregion

        #region "line detection"

        /// <summary>
        /// trace along a line
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        private calibration_line traceLine(int width, int height,
                                           int tx, int ty, int bx, int by)
        {
            calibration_line line = new calibration_line();

            int step_size = 10;
            int radius_x = width / (separation_factor * 2);
            if (radius_x < 1) radius_x = 1;
            int radius_y = height / (separation_factor * 2);
            if (radius_y < 1) radius_y = 1;
            int dx = bx - tx;
            int dy = by - ty;

            if (ROI != null)
            {
                if (tx < ROI.tx) tx = ROI.tx;
                if (ty < ROI.ty) ty = ROI.ty;
                if (bx > ROI.bx) bx = ROI.bx;
                if (by > ROI.by) by = ROI.by;
            }

            if (dy > dx)
            {
                step_size = radius_y*2;
                for (int y = ty; y < by; y += step_size)
                {
                    int x = tx + (dx * (y - ty) / dy);

                    int av_x = 0;
                    int av_y = 0;
                    int hits = 0;
                    //int max_diff = 0;
                    for (int xx = x - radius_x; xx < x + radius_x; xx++)
                    {
                        if ((xx > 2) && (xx < width-2))
                        {
                            for (int yy = y - (radius_y*4); yy < y + (radius_y*4); yy++)
                            {
                                if ((yy > -1) && (yy < height))
                                {                                    
                                    if (edges_binary[xx, yy])
                                    {
                                        av_x += xx;
                                        av_y += yy;
                                        hits++;
                                    }                                    
                                }
                            }
                        }
                    }

                    if (hits > 0)
                    {
                        av_x /= hits;
                        av_y /= hits;

                        int n = (y * width) + av_x;
                        int intensity = calibration_image[n];
                        int min_x = av_x;
                        for (int xx = av_x - 2; xx <= av_x + 2; xx++)
                        {
                            if ((xx > -1) && (xx < width))
                            {
                                n = (y * width) + xx;
                                if (calibration_image[n] < intensity)
                                {
                                    intensity = calibration_image[n];
                                    min_x = xx;
                                }
                            }
                        }

                        line.Add(min_x, y);
                    }
                }
            }
            else
            {
                step_size = radius_x*2;
                for (int x = tx; x < bx; x += step_size)
                {
                    int y = ty + (dy * (x - tx) / dx);

                    int av_x = 0;
                    int av_y = 0;
                    int hits = 0;
                    //int max_diff = 0;

                    for (int xx = x - (radius_x*4); xx < x + (radius_x*4); xx++)
                    {
                        if ((xx > -1) && (xx < width))
                        {
                            for (int yy = y - radius_y; yy < y + radius_y; yy++)
                            {
                                if ((yy > 2) && (yy < height-2))
                                {                                    
                                    if (edges_binary[xx, yy])
                                    {
                                        av_x += xx;
                                        av_y += yy;
                                        hits++;
                                    }
                                    
                                }
                            }
                        }
                    }

                    if (hits > 0)
                    {
                        av_x /= hits;
                        av_y /= hits;

                        int n = (av_y * width) + x;
                        int intensity = calibration_image[n];
                        int min_y = av_y;
                        for (int yy = av_y - 2; yy <= av_y + 2; yy++)
                        {
                            if ((yy > -1) && (yy < height))
                            {
                                n = (yy * width) + x;
                                if (calibration_image[n] < intensity)
                                {
                                    intensity = calibration_image[n];
                                    min_y = yy;
                                }
                            }
                        }

                        line.Add(x, min_y);
                    }
                }
            }
            return (line);
        }


        /// <summary>
        /// detect vertical lines
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void detectVerticalLines(int width, int height)
        {
            vertical_lines = new ArrayList();
            int search_width = width / 10;
            int line_width = 4;
            int start_y = height / 20;
            int end_y = height - start_y;
            int prev_x_top = 0;
            int prev_x_bottom = 0;
            int prev_line_x = 0;

            closest_to_centreline_x = 0;
            for (int x = 0; x < width; x++)
            {
                if (horizontal_magnitude[1, x] > 0)
                {
                    // is this the closest to the vertical centre line?
                    int dc = x - (width / 2);
                    if ((closest_to_centreline_x == 0) || (dc < closest_to_centreline_x))
                        closest_to_centreline_x = dc;

                    if (prev_x_bottom > 0)
                    {
                        horizontal_separation_bottom += (x - prev_x_bottom);
                        horizontal_separation_bottom_hits++;
                        if (horizontal_separation_bottom_hits > 50)
                        {
                            horizontal_separation_bottom /= 2;
                            horizontal_separation_bottom_hits /= 2;
                        }
                    }
                    prev_x_bottom = x;
                }
                
                if (horizontal_magnitude[0, x] > 0)
                {
                    // is this the closest to the vertical centre line?
                    int dc = x - (width / 2);
                    if ((closest_to_centreline_x == 0) || (dc < closest_to_centreline_x))
                        closest_to_centreline_x = dc;

                    if (prev_x_top > 0)
                    {
                        horizontal_separation_top += (x - prev_x_top);
                        horizontal_separation_top_hits++;
                        if (horizontal_separation_top_hits > 50)
                        {
                            horizontal_separation_top /= 2;
                            horizontal_separation_top_hits /= 2;
                        }
                    }
                    prev_x_top = x;

                    int max_score = (end_y-start_y)*8/10;
                    int winner = -1;
                    int additive = (x - (width / 2)) / 15;
                    additive *= Math.Abs(additive);
                    int search_left = x - search_width;
                    int search_right = x + search_width;

                    if (x > width / 2)
                        search_left = x-line_width;
                    else
                        search_right = x+line_width;

                    search_left += additive;
                    search_right += additive;

                    for (int x2 = search_left; x2 < search_right; x2++)
                    {
                        if ((x2 > -1) && (x2 < width))
                        {
                            if (horizontal_magnitude[1, x2] > 0)
                            {
                                int score = 0;
                                int dx = x2-x;
                                for (int y = start_y; y < end_y; y++)
                                {
                                    int xx = x + ((dx * (y - start_y)) / (end_y - start_y));
                                    for (int xx2 = xx - line_width; xx2 < xx + line_width; xx2++)
                                        if ((xx2 > -1) && (xx2 < width))
                                            if (edges_binary[xx2, y]) score++;
                                }
                                if (score > max_score)
                                {
                                    winner = x2;
                                    max_score = score;
                                }
                            }
                        }
                    }

                    if (winner > -1)
                    {
                        int line_x = (x + winner) / 2;
                        int min_separation_x = width / separation_factor;
                        if ((prev_line_x == 0) || (line_x - prev_line_x > min_separation_x))
                        {
                            calibration_line line = traceLine(width, height, x, start_y, winner, end_y);
                            vertical_lines.Add(line);
                            line.Draw(lines_image, width, height, 255, 0, 0);
                        }
                        prev_line_x = line_x;
                    }
                }
            }
        }

        /// <summary>
        /// detect horizontal lines
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void detectHorizontalLines(int width, int height)
        {
            horizontal_lines = new ArrayList();
            int search_width = height / 20;
            int line_width = 4;
            int start_x = width / 8;
            int end_x = width - start_x;
            int prev_y_left = 0;
            int prev_y_right = 0;
            bool first_right = true;
            bool first_left = true;
            int curr_separation_left = 0;
            int max_separation_left = 0;
            int curr_separation_right = 0;
            int max_separation_right = 0;
            int prev_line_y = 0;

            closest_to_centreline_y = 0;
            for (int y = 0; y < height; y++)
            {
                if (vertical_magnitude[1, y] > 0)
                {
                    // is this the closest to the horizontal centre line?
                    int dc = y - (height / 2);
                    if ((closest_to_centreline_y == 0) || (dc < closest_to_centreline_y))
                        closest_to_centreline_y = dc;

                    if (prev_y_right > 0)
                    {
                        curr_separation_right = y - prev_y_right;
                        if (curr_separation_right > max_separation_right)
                            max_separation_right = curr_separation_right;
                        if (first_right)
                        {
                            first_right = false;
                            vertical_separation_top += curr_separation_right;
                            vertical_separation_top_hits++;
                            if (vertical_separation_top_hits > 50)
                            {
                                vertical_separation_top /= 2;
                                vertical_separation_top_hits /= 2;
                            }
                        }
                    }
                    prev_y_right = y;
                }

                if (vertical_magnitude[0, y] > 0)
                {
                    // is this the closest to the horizontal centre line?
                    int dc = y - (height / 2);
                    if ((closest_to_centreline_y == 0) || (dc < closest_to_centreline_y))
                        closest_to_centreline_y = dc;

                    if (prev_y_left > 0)
                    {
                        curr_separation_left = y - prev_y_left;
                        if (curr_separation_left > max_separation_left)
                            max_separation_left = curr_separation_left;
                        if (first_left)
                        {
                            first_left = false;
                            vertical_separation_top += curr_separation_left;
                            vertical_separation_top_hits++;
                            if (vertical_separation_top_hits > 50)
                            {
                                vertical_separation_top /= 2;
                                vertical_separation_top_hits /= 2;
                            }
                        }
                    }
                    prev_y_left = y;

                    int max_score = (end_x - start_x) * 8 / 10;
                    int winner = -1;
                    for (int y2 = y - search_width; y2 < y + search_width; y2++)
                    {
                        if ((y2 > -1) && (y2 < height))
                        {
                            if (vertical_magnitude[1, y2] > 0)
                            {
                                int score = 0;
                                int dy = y2 - y;
                                for (int x = start_x; x < end_x; x++)
                                {
                                    int yy = y + ((dy * (x - start_x)) / (end_x - start_x));
                                    for (int yy2 = yy - line_width; yy2 < yy + line_width; yy2++)
                                        if ((yy2 > -1) && (yy2 < height))
                                            if (edges_binary[x, yy2]) score++;
                                }
                                if (score > max_score)
                                {
                                    winner = y2;
                                    max_score = score;
                                }
                            }
                        }
                    }

                    if (winner > -1)
                    {
                        int line_y = (y + winner) / 2;
                        int min_separation_y = height / separation_factor;
                        if ((prev_line_y == 0) || (line_y - prev_line_y > min_separation_y))
                        {
                            calibration_line line = traceLine(width, height, start_x, y, end_x, winner);
                            horizontal_lines.Add(line);
                            line.Draw(lines_image, width, height, 255, 0, 0);
                        }
                        prev_line_y = line_y;
                    }
                }
            }

            if (max_separation_right > 0)
            {
                vertical_separation_bottom += max_separation_right;
                vertical_separation_bottom_hits++;
                if (vertical_separation_bottom_hits > 50)
                {
                    vertical_separation_bottom /= 2;
                    vertical_separation_bottom_hits /= 2;
                }
            }
            if (max_separation_left > 0)
            {
                vertical_separation_bottom += max_separation_left;
                vertical_separation_bottom_hits++;
                if (vertical_separation_bottom_hits > 50)
                {
                    vertical_separation_bottom /= 2;
                    vertical_separation_bottom_hits /= 2;
                }
            }

        }

        #endregion

        #region "lens distortion calculation"

        private void detectLensDistortion(int width, int height,
                                          int grid_x, int grid_y)
        {
            if (grid[grid_x, grid_y] != null)
            {
                // field of vision in radians
                float FOV_horizontal = camera_FOV_degrees * (float)Math.PI / 180.0f;
                float FOV_vertical = FOV_horizontal * height / (float)width;
                //FOV_vertical = FOV_vertical * temp_vertical_adjust;
                FOV_vertical = (FOV_vertical * height / width) * temp_vertical_adjust;
                //FOV_vertical = FOV_vertical * temp_vertical_adjust;

                // center point of the grid within the image
                int centre_x = (int)grid[grid_x, grid_y].x;
                int centre_y = (int)grid[grid_x, grid_y].y;

                // calculate the distance to the centre grid point on the ground plane
                float ground_dist_to_point = camera_dist_to_pattern_centre_mm;

                // line of sight distance between the camera lens and the centre point
                float camera_to_point_dist = (float)Math.Sqrt((ground_dist_to_point * ground_dist_to_point) +
                                             (camera_height_mm * camera_height_mm));

                // tilt angle at the centre point
                float centre_tilt = (float)Math.Asin(camera_height_mm / camera_to_point_dist);




                // pan angle at the centre
                float point_pan = (float)Math.Asin(calibration_pattern_spacing_mm / camera_to_point_dist);

                // grid width at the centre point
                float x1 = centre_x + (point_pan * width / FOV_horizontal);

                // calculate the distance to the observed grid point on the ground plane
                ground_dist_to_point = camera_dist_to_pattern_centre_mm + ((grid_y + 2) * calibration_pattern_spacing_mm);

                // line of sight distance between the camera lens and the observed point
                camera_to_point_dist = (float)Math.Sqrt((ground_dist_to_point * ground_dist_to_point) +
                                             (camera_height_mm * camera_height_mm));

                // tilt angle
                float point_tilt = (float)Math.Asin(camera_height_mm / camera_to_point_dist);

                // pan angle
                point_pan = (float)Math.Asin(calibration_pattern_spacing_mm / camera_to_point_dist);

                // calc the position of the grid point within the image after rectification
                float x2 = centre_x + (point_pan * width / FOV_horizontal);
                float y2 = centre_y + ((point_tilt - centre_tilt) * height / FOV_vertical);

                // calc the gradient
                float grad = (x2 - x1) / (float)(y2 - centre_y);


                ArrayList rectifiedPoints = new ArrayList();

                centre_of_distortion = new calibration_point(0, 0);
                int hits = 0;
                float cx = 0, cy = 0;

                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        if (grid[x, y] != null)
                        {
                            // calculate the distance to the observed grid point on the ground plane
                            ground_dist_to_point = camera_dist_to_pattern_centre_mm + ((grid_y - y) * calibration_pattern_spacing_mm);

                            // line of sight distance between the camera lens and the observed point
                            camera_to_point_dist = (float)Math.Sqrt((ground_dist_to_point * ground_dist_to_point) +
                                                         (camera_height_mm * camera_height_mm));

                            // tilt angle
                            point_tilt = (float)Math.Asin(camera_height_mm / camera_to_point_dist);

                            // distance to the point on the ground plave along the x (horizontal axis)
                            float ground_dist_to_point_x = (x - grid_x) * calibration_pattern_spacing_mm;

                            // pan angle
                            point_pan = (float)Math.Asin(ground_dist_to_point_x / camera_to_point_dist);

                            // calc the position of the grid point within the image after rectification
                            //float rectified_x = centre_x + (point_pan * width / FOV_horizontal);
                            float rectified_x = centre_x + (((x1-centre_x) + ((grid[x, y].y - centre_y) * grad)) * (x - grid_x));
                            float rectified_y = centre_y + ((point_tilt - centre_tilt) * height / FOV_vertical);
                            grid[x, y].rectified_x = rectified_x;
                            grid[x, y].rectified_y = rectified_y;
                            cx += (grid[x, y].x - rectified_x);
                            cy += (grid[x, y].y - rectified_y);

                            hits++;
                        }
                    }
                }

                if (hits > 0)
                {
                    // a ballpack figure for the centre of distortion
                    centre_of_distortion.x = (width / 2) + (cx / (float)hits);
                    centre_of_distortion.y = (height / 2) + (cy / (float)hits);

                    float winner_x = centre_of_distortion.x;
                    float winner_y = centre_of_distortion.y;
                    float min_rms_err = 999999;
                    int radius = 5;
                    for (int search_x = (int)centre_of_distortion.x - radius; search_x <= (int)centre_of_distortion.x + radius; search_x++)
                    {
                        for (int search_y = (int)centre_of_distortion.y - radius; search_y <= (int)centre_of_distortion.y + radius; search_y++)
                        {
                            polyfit curvefit = new polyfit();
                            curvefit.SetDegree(2);

                            int i = 0;
                            for (int x = 0; x < grid.GetLength(0); x++)
                            {
                                for (int y = 0; y < grid.GetLength(1); y++)
                                {
                                    if (grid[x, y] != null)
                                    {
                                        float dx = grid[x, y].rectified_x - centre_of_distortion.x;
                                        float dy = grid[x, y].rectified_y - centre_of_distortion.y;
                                        float radial_dist_rectified = (float)Math.Sqrt((dx * dx) + (dy * dy));
                                        dx = grid[x, y].x - centre_of_distortion.x;
                                        dy = (grid[x, y].y - centre_of_distortion.y);
                                        float radial_dist_original = (float)Math.Sqrt((dx * dx) + (dy * dy));

                                        curvefit.AddPoint(radial_dist_rectified, radial_dist_original);
                                        i++;
                                    }
                                }
                            }
                            curvefit.Solve();
                            float rms_err = curvefit.GetRMSerror();
                            if (rms_err < min_rms_err)
                            {
                                min_rms_err = rms_err;
                                winner_x = search_x;
                                winner_y = search_y;
                                fitter = curvefit;
                            }
                        }
                    }

                    centre_of_distortion.x = winner_x;
                    centre_of_distortion.y = winner_y;
                }
            }
        }


        private void detectScale(int width, int height)
        {
            if (fitter != null)
            {
                int x;
                int prev_x_start = 0;
                int x_start = -1;
                int y = height / 2;

                temp_scale = 1;
                isValidRectification = true;

                for (int i = 0; i < 2; i++)
                {
                    x = 0;
                    if (i > 0) y = height - 10;
                    while ((x < width / 4) && (x_start < 0))
                    {
                        float dx = x - centre_of_distortion.x;
                        float dy = y - centre_of_distortion.y;

                        float radial_dist_rectified = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        if (radial_dist_rectified >= 0.01f)
                        {
                            float radial_dist_original = fitter.RegVal(radial_dist_rectified);
                            if (radial_dist_original > 0)
                            {
                                float ratio = radial_dist_original / radial_dist_rectified;
                                x_start = (int)Math.Round(centre_of_distortion.x + (dx * ratio));
                            }
                        }
                        x++;
                    }
                    if (x_start > -1)
                    {
                        x--;
                        if (i == 0)
                        {
                            temp_scale = 1.0f - (x / (float)(width / 2));
                            prev_x_start = x;
                        }
                        else
                        {
                            if (x > prev_x_start)
                                isValidRectification = false;
                        }                        
                    }
                }

            }
        }

        private float GetRMSerror_old()
        {
            float rms_error = 0;
            int hits = 0;

            if (grid != null)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    int top = -1;
                    int bottom = -1;
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        if (grid[x, y] != null)
                        {
                            if (top == -1) top = y;
                            bottom = y;
                        }
                    }

                    if (bottom > top + 1)
                    {
                        int rectified_x = 0;
                        int rectified_y = 0;
                        if (rectifyPoint((int)grid[x, top].x,(int)grid[x, top].y,ref rectified_x,ref rectified_y))
                        {
                            int tx = rectified_x;
                            int ty = rectified_y;
                            if (rectifyPoint((int)grid[x, bottom].x, (int)grid[x, bottom].y, ref rectified_x, ref rectified_y))
                            {
                                int bx = rectified_x;
                                int by = rectified_y;
                                int dx = bx - tx;
                                int dy = by - ty;

                                if (dy > 0)
                                {
                                    for (int y = top + 1; y < bottom; y++)
                                    {
                                        if (grid[x, y] != null)
                                        {
                                            if (rectifyPoint((int)grid[x, y].x, (int)grid[x, y].y, ref rectified_x, ref rectified_y))
                                            {
                                                int xx = tx + (((rectified_y - ty) * dx) / dy);
                                                int diff = xx - rectified_x;
                                                rms_error += (diff * diff);
                                                hits++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    int top = -1;
                    int bottom = -1;
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        if (grid[x, y] != null)
                        {
                            if (top == -1) top = x;
                            bottom = x;
                        }
                    }

                    if (bottom > top + 1)
                    {
                        int rectified_x = 0;
                        int rectified_y = 0;
                        if (rectifyPoint((int)grid[top, y].x, (int)grid[top, y].y, ref rectified_x, ref rectified_y))
                        {
                            int tx = rectified_x;
                            int ty = rectified_y;
                            if (rectifyPoint((int)grid[bottom, y].x, (int)grid[bottom, y].y, ref rectified_x, ref rectified_y))
                            {
                                int bx = rectified_x;
                                int by = rectified_y;
                                int dx = bx - tx;
                                int dy = by - ty;

                                if (dx > 0)
                                {
                                    for (int x = top + 1; x < bottom; x++)
                                    {
                                        if (grid[x, y] != null)
                                        {
                                            if (rectifyPoint((int)grid[x, y].x, (int)grid[x, y].y, ref rectified_x, ref rectified_y))
                                            {
                                                int yy = ty + (((rectified_x - tx) * dy) / dx);
                                                int diff = yy - rectified_y;
                                                rms_error += (diff * diff);
                                                hits++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

            }
            if (hits > 2)
                rms_error = (float)Math.Sqrt(rms_error / (float)hits);
            else
                rms_error = 99999;
            return (rms_error);
        }

        private float GetRMSerror()
        {
            float rms_error = 0;
            int rectified_x = 0, rectified_y = 0;
            int hits = 0;

            if (grid != null)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        if (grid[x, y] != null)
                        {
                            if (rectifyPoint((int)grid[x, y].x, (int)grid[x, y].y, ref rectified_x, ref rectified_y))
                            {
                                float dx = rectified_x - grid[x, y].rectified_x;
                                float dy = rectified_y - grid[x, y].rectified_y;
                                rms_error += ((dx * dx) + (dy * dy));
                                hits++;
                            }
                        }
                    }
                }
            }
            if (hits > 2)
                rms_error = (float)Math.Sqrt(rms_error / (float)hits);
            else
                rms_error = 99999;
            return (rms_error);
        }

        #endregion

        #region "image rectification"

        /// <summary>
        /// returns a rectified version of the given image location
        /// </summary>
        /// <param name="original_x"></param>
        /// <param name="original_y"></param>
        /// <param name="rectified_x"></param>
        /// <param name="rectified_y"></param>
        /// <returns></returns>
        private bool rectifyPoint(int original_x, int original_y,
                                  ref int rectified_x, ref int rectified_y)
        {
            bool isValid = true;
            rectified_x = temp_calibration_map_inverse[original_x, original_y, 0];
            rectified_y = temp_calibration_map_inverse[original_x, original_y, 1];
            if ((rectified_x == 0) && (rectified_y == 0)) isValid = false;
            return (isValid);
        }

        /// <summary>
        /// update the calibration lookup table, which maps pixels
        /// in the rectified image into the original image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void updateCalibrationMap(int width, int height, polyfit curve, float scale)
        {
            temp_calibration_map = new int[width * height];            
            temp_calibration_map_inverse = new int[width, height, 2];
            for (int x = 0; x < width; x++)
            {
                float dx = x - centre_of_distortion.x;

                for (int y = 0; y < height; y++)
                {
                    float dy = y - centre_of_distortion.y;

                    float radial_dist_rectified = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (radial_dist_rectified >= 0.01f)
                    {
                        float radial_dist_original = curve.RegVal(radial_dist_rectified);
                        if (radial_dist_original > 0)
                        {
                            float ratio = radial_dist_original / radial_dist_rectified;
                            int x2 = (int)Math.Round(centre_of_distortion.x + (dx * ratio));
                            x2 = (width/2) + (int)((x2 - (width / 2)) * scale);
                            int y2 = (int)Math.Round(centre_of_distortion.y + (dy * ratio));
                            y2 = (height / 2) + (int)((y2 - (height / 2)) * scale);

                            if ((x2 > -1) && (x2 < width) && (y2 > -1) && (y2 < height))
                            {
                                int n = (y * width) + x;
                                int n2 = (y2 * width) + x2;

                                temp_calibration_map[n] = n2;
                                temp_calibration_map_inverse[x2, y2, 0] = x;
                                temp_calibration_map_inverse[x2, y2, 1] = y;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// rectify the given image
        /// </summary>
        /// <param name="img">raw image</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <returns></returns>
        public Byte[] Rectify(Byte[] img, int width, int height)
        {
            if (calibration_map != null)
            {
                rectified_image = new Byte[width * height * 3];
                for (int i = 0; i < width * height; i++)
                {
                    int j = calibration_map[i];
                    if ((j < width * height) && (j > -1))
                    {
                        for (int col=0;col<3;col++)
                            rectified_image[(i * 3) + col] = img[(j * 3) + col];
                    }
                }
            }
            return (rectified_image);
        }


        #endregion

        #region "main update method"

        private bool validRectification(Byte[] img, int width, int height)
        {
            int x = 10;
            int y = 10;
            int n = (y * width) + x;
            if ((img[n] != 0) || (img[n + 2] != 0) || (img[n + 4] != 0))
                return (true);
            else
                return (false);
        }

        public void Update(Byte[] img, int width, int height)
        {
            if (img != null)
            {
                image_width = width;
                image_height = height;

                // create lists to store edges
                edges_horizontal = new ArrayList();
                edges_vertical = new ArrayList();
                edges_image = new Byte[width * height * 3];
                centrealign_image = new Byte[width * height * 3];
                corners_image = new Byte[width * height * 3];
                lines_image = new Byte[width * height * 3];
                edges_binary = new bool[width, height];
                horizontal_magnitude = new int[2, width];
                vertical_magnitude = new int[2, height];
                if (binary_image == null) binary_image = new bool[no_of_images, width, height];
                for (int i = 0; i < img.Length; i++)
                {
                    lines_image[i] = img[i];
                    centrealign_image[i] = img[i];
                    edges_image[i] = img[i];
                    corners_image[i] = img[i];
                }

                // image used for aligning the centre of the calibration pattern
                util.drawLine(centrealign_image, width, height, width / 2, 0, width / 2, height - 1, 255, 255, 255, 0, false);
                util.drawLine(centrealign_image, width, height, 0, height / 2, width - 1, height / 2, 255, 255, 255, 0, false);
                // show region of interest
                if (ROI != null) ROI.Show(centrealign_image, width, height);

                // create a mono image
                calibration_image = util.monoImage(img, width, height);

                int min_magnitude = 1;
                clearBinaryImage(width, height);
                detectHorizontalEdges(calibration_image, width, height, edges_horizontal, min_magnitude);
                detectVerticalEdges(calibration_image, width, height, edges_vertical, min_magnitude);
                updateEdgesImage(width, height);

                detectHorizontalLines(width, height);
                detectVerticalLines(width, height);

                // create a grid to store the edges
                if ((vertical_lines.Count > 0) && (horizontal_lines.Count > 0))
                {
                    // locate corners
                    detectCorners(width, height);

                    // hunt the centre spot
                    int grid_cx = 0, grid_cy = 0;
                    detectCentreSpot(calibration_image, width, height, ref grid_cx, ref grid_cy);
                    //util.drawCross(corners_image, width, height, (int)centre_spot.x, (int)centre_spot.y, 4, 0, 255, 0, 0);
                    if (grid_cx > 0)
                    {
                        //util.drawBox(corners_image, width, height, image_cx, image_cy, 2, 0, 255, 0, 0);

                        // detect the lens distortion
                        detectLensDistortion(width, height, grid_cx, grid_cy);

                        if (fitter != null)
                        {
                            float[] C = new float[4];
                            C[1] = fitter.Coeff(1);
                            C[2] = fitter.Coeff(2);
                            //C[3] = fitter.Coeff(3);

                            int max_v = 1;
                            if (min_RMS_error > 5) max_v = 5;
                            if (min_RMS_error > 10) max_v = 15;
                            for (int v = 0; v < max_v; v++)
                            {
                                temp_vertical_adjust = 1.15f + ((rnd.Next(1000000) / 1000000.0f) * 0.2f);

                                for (int c = 1; c <= 2; c++)
                                    fitter.SetCoeff(c, C[c] * (1.0f + ((((rnd.Next(2000000) / 1000000.0f) - 1.0f) * 0.05f))));

                                detectScale(width, height);

                                if (isValidRectification)
                                {
                                    // update the calibration lookup
                                    updateCalibrationMap(width, height, fitter, 1.0f);

                                    float RMS_error = GetRMSerror();
                                    if (RMS_error < min_RMS_error)
                                    {
                                        // update the graph
                                        curve_fit = new Byte[width * height * 3];
                                        fitter.Show(curve_fit, width, height);

                                        updateCalibrationMap(width, height, fitter, temp_scale);
                                        calibration_map = new int[width * height];
                                        for (int i = 0; i < temp_calibration_map.Length; i++)
                                            calibration_map[i] = temp_calibration_map[i];
                                        calibration_map_inverse = new int[width, height, 2];
                                        for (int x = 0; x < width; x++)
                                        {
                                            for (int y = 0; y < height; y++)
                                            {
                                                calibration_map_inverse[x, y, 0] = temp_calibration_map_inverse[x, y, 0];
                                                calibration_map_inverse[x, y, 1] = temp_calibration_map_inverse[x, y, 1];
                                            }
                                        }

                                        //if (validRectification(calibration_image, width, height))
                                        {
                                            scale = temp_scale;
                                            vertical_adjust = temp_vertical_adjust;

                                            //RMS_error = MinimiseError(width, height, 100) * err1;
                                            best_curve = fitter;

                                            min_RMS_error = RMS_error;
                                        }

                                    }
                                }
                            }

                            // rectify
                            Rectify(img, width, height);
                        }

                        //ShowRectifiedCorners(rectified_image, width, height);
                    }

                    corners_index++;
                    if (corners_index >= corners.Length) corners_index = 0;

                    //drawGrid(edges_image, width, height);
                }

                binary_image_index++;
                if (binary_image_index >= no_of_images) binary_image_index = 0;

                av_centreline_x += closest_to_centreline_x;
                av_centreline_x_hits++;
                if (av_centreline_x_hits > 50)
                {
                    av_centreline_x /= 2;
                    av_centreline_x_hits /= 2;
                }

                av_centreline_y += closest_to_centreline_y;
                av_centreline_y_hits++;
                if (av_centreline_y_hits > 50)
                {
                    av_centreline_y /= 2;
                    av_centreline_y_hits /= 2;
                }
            }

        }

        #endregion

        #region "saving and loading"

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeCalibration = doc.CreateElement("Sentience");
            doc.AppendChild(nodeCalibration);

            util.AddComment(doc, nodeCalibration, "Camera calibration data");

            XmlElement elem = getXml(doc);
            doc.DocumentElement.AppendChild(elem);

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
        {
            // use an XmlTextReader to open an XML document
            XmlTextReader xtr = new XmlTextReader(filename);
            xtr.WhitespaceHandling = WhitespaceHandling.None;

            // load the file into an XmlDocuent
            XmlDocument xd = new XmlDocument();
            xd.Load(xtr);

            // get the document root node
            XmlNode xnodDE = xd.DocumentElement;

            // recursively walk the node tree
            LoadFromXml(xnodDE, 0);

            // close the reader
            xtr.Close();

            // show the best fit curve
            curve_fit = new Byte[image_width * image_height * 3];
            fitter.Show(curve_fit, image_width, image_height);
        }

        /// <summary>
        /// return an xml element containing camera calibration parameters
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            String coefficients = "";
            if (fitter != null)
            {
                int degree = fitter.GetDegree();
                for (int i = 0; i <= degree; i++)
                {
                    coefficients += Convert.ToString(fitter.Coeff(i));
                    if (i < degree) coefficients += ",";
                }
            }

            XmlElement elem = doc.CreateElement("Camera");
            doc.DocumentElement.AppendChild(elem);
            util.AddComment(doc, elem, "Horizontal field of view of the camera in degrees");
            util.AddTextElement(doc, elem, "FieldOfViewDegrees", Convert.ToString(camera_FOV_degrees));
            util.AddComment(doc, elem, "Image dimensions in pixels");
            util.AddTextElement(doc, elem, "ImageDimensions", Convert.ToString(image_width) + "," + Convert.ToString(image_height));
            if (centre_of_distortion != null)
            {
                util.AddComment(doc, elem, "The centre of distortion in pixels");
                util.AddTextElement(doc, elem, "CentreOfDistortion", Convert.ToString(centre_of_distortion.x) + "," + Convert.ToString(centre_of_distortion.y));
            }
            util.AddComment(doc, elem, "Polynomial coefficients used to describe the camera lens distortion");
            util.AddTextElement(doc, elem, "DistortionCoefficients", coefficients);
            util.AddComment(doc, elem, "The minimum RMS error between the distortion curve and plotted points");
            util.AddTextElement(doc, elem, "RMSerror", Convert.ToString(min_RMS_error));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "FieldOfViewDegrees")
                camera_FOV_degrees = Convert.ToInt32(xnod.InnerText);

            if (xnod.Name == "ImageDimensions")
            {
                String[] dimStr = xnod.InnerText.Split(',');
                image_width = Convert.ToInt32(dimStr[0]);
                image_height = Convert.ToInt32(dimStr[1]);
            }

            if (xnod.Name == "CentreOfDistortion")
            {
                String[] centreStr = xnod.InnerText.Split(',');
                centre_of_distortion = new calibration_point(
                    Convert.ToSingle(centreStr[0]),
                    Convert.ToSingle(centreStr[1]));
            }

            if (xnod.Name == "DistortionCoefficients")
            {
                if (xnod.InnerText != "")
                {
                    String[] coeffStr = xnod.InnerText.Split(',');
                    fitter = new polyfit();
                    fitter.SetDegree(coeffStr.Length - 1);
                    for (int i = 0; i < coeffStr.Length; i++)
                        fitter.SetCoeff(i, Convert.ToSingle(coeffStr[i]));
                }
            }

            if (xnod.Name == "RMSerror")
                min_RMS_error = Convert.ToSingle(xnod.InnerText);

            // if this is an element, extract any attributes
            /*
            if (xnod.NodeType == XmlNodeType.Element)
            {
                XmlNamedNodeMap mapAttributes = xnod.Attributes;
                for (int i = 0; i < mapAttributes.Count; i += 1)
                {
                    //Console.WriteLine(pad + " " + mapAttributes.Item(i).Name
                    //    + " = " + mapAttributes.Item(i).Value);
                }
            }
            */

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }
}
