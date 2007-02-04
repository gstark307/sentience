using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using sentience.core;

namespace sentience.calibration
{
    public class calibration_point
    {
        public int x, y;

        public calibration_point(int x, int y)
        {
            this.x = x;
            this.y = y;
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
                util.drawLine(img, width, height, p1.x, p1.y, p2.x, p2.y, r, g, b, 0, false);
            }
        }
    }

    public class calibration_edge : calibration_point
    {
        public int magnitude;
        public bool enabled;
        public int hits = 1;
        public int grid_x, grid_y;

        public calibration_edge(int x, int y, int magnitude) : base(x,y)
        {
            this.magnitude = magnitude;
            enabled = true;
        }
    }

    public class calibration
    {
        public int separation_factor = 15;
        public bool showLines = false;
        public bool showEdges = false;

        Byte[] calibration_image;
        public Byte[] edges_image;
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

        private int averageIntensity(Byte[] img, int width, int height, int tx, int ty, int bx, int by)
        {
            int hits = 0;
            int tot = 0;
            for (int x = tx; x < bx; x++)
            {
                if ((x > -1) && (x < width) && (x != (tx+((bx-tx)/2))))
                {
                    for (int y = ty; y < by; y++)
                    {
                        if ((y > -1) && (y < height) && (y != (ty + ((by - ty) / 2))))
                        {
                            int n = (y * width) + x;
                            tot += (255-img[n]);
                            hits++;
                        }
                    }
                }
            }
            if (hits > 0) tot /= hits;
            return (tot);
        }

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
                                        int cg_x = (int)ix;
                                        int cg_y = (int)iy;
                                        int magnitude = localCG(calibration_image, width, height, (int)ix, (int)iy, 10, ref cg_x, ref cg_y);
                                        //if (magnitude > 10)
                                        {
                                            int av = averageIntensity(calibration_image, width, height, cg_x - 10, cg_y - 10, cg_x + 10, cg_y + 10);
                                            //if ((av < magnitude * 97 / 100) && (av < 170))
                                            if (av < 170)
                                            {
                                                calibration_edge intersection_point = new calibration_edge(cg_x, cg_y, magnitude);

                                                // get the grid coordinate of this corner
                                                int grid_x = 0, grid_y = 0;
                                                getGridCoordinate(cg_x, cg_y, width, height, ref grid_x, ref grid_y);

                                                intersection_point.grid_x = grid_x;
                                                intersection_point.grid_y = grid_y;
                                                corners[corners_index].Add(intersection_point);
                                                line1.intersections.Add(intersection_point);
                                                line2.intersections.Add(intersection_point);                                                
                                            }
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
            calibration_edge[,] coverage = new calibration_edge[width, height];
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
                    
                    if (coverage[pt.x, pt.y] == null)
                    {
                        int radius = 10;
                        for (int xx = pt.x - radius; xx < pt.x + radius; xx++)
                        {
                            if ((xx > -1) && (xx < width))
                            {
                                for (int yy = pt.y - radius; yy < pt.y + radius; yy++)
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
                        int xx = pt.x;
                        int yy = pt.y;
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
                //for (int grid_x = 2; grid_x < 3; grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                    //for (int grid_y = 1; grid_y < 2; grid_y++)
                    {
                        calibration_edge pt = (calibration_edge)grid[grid_x, grid_y];
                        if (pt != null)
                            util.drawCross(edges_image, width, height, pt.x, pt.y, 3, 255, 0, 0, 0);
                    }
                }
            }

        }

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
                                    int dx = e2.x - e1.x;
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
                        for (int xx = e.x - 1; xx < e.x; xx++)
                            for (int yy = e.y - 1; yy < e.y; yy++)
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
                                    int dy = e2.y - e1.y;
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
                        for (int xx = e.x - 1; xx < e.x; xx++)
                            for (int yy = e.y - 1; yy < e.y; yy++)
                                binary_image[binary_image_index, xx, yy] = true;
                    }
                }
            }
        }

        private int localCG(Byte[] img, int width, int height, int x, int y, int radius,
                             ref int cg_x, ref int cg_y)
        {
            cg_x = 0;
            cg_y = 0;
            int tot = 0;
            int hits = 0;
            int score = 0;
            for (int xx = x - radius; xx < x + radius; xx++)
            {
                if ((xx > -1) && (xx < width-1))
                {
                    for (int yy = y - radius; yy < y + radius; yy++)
                    {
                        if ((yy > -1) && (yy < height-1))
                        {
                            if (edges_binary[xx, yy])
                            {
                                int n = (yy * width) + xx;
                                score = (255 - img[n]);
                                cg_x += (score * xx);
                                cg_y += (score * yy);
                                tot += score;
                                hits++;
                            }
                        }
                    }
                }
            }

            if (hits > 0)
            {
                cg_x /= tot;
                cg_y /= tot;
            }
            return (tot/hits);
        }


        private void updateEdgesImage(int width, int height)
        {
            int start_x = width / 5;
            int end_x = width - start_x;

            for (int x = start_x; x < end_x; x++)
            {
                for (int y = 5; y < height-5; y++)
                {
                    int hits = 0;
                    for (int i = 0; i < no_of_images; i++)
                    {
                        if (binary_image[i, x, y]) hits++;
                    }
                    if (hits > no_of_images-3)
                    {
                        edges_binary[x, y] = true;

                        int index = -1;
                        if (y < height / 3) index = 0;
                        if (y > height * 2 / 3) index = 1;
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

                        if (showEdges)
                        {
                            int n = ((y * width) + x) * 3;
                            edges_image[n] = 0;
                            edges_image[n + 1] = (Byte)255;
                            edges_image[n + 2] = 0;
                        }
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
                for (int y = height*9/10; y < height - 1; y++)
                {
                    if (vertical_magnitude[i, y] > 0) vertical_magnitude[i, y] = 0;
                }
            }
        }

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

            const int step_size = 10;
            int dx = bx - tx;
            int dy = by - ty;

            if (dy > dx)
            {
                for (int y = ty; y < by; y += step_size)
                {
                    int x = tx + (dx * (y - ty) / dy);

                    int av_x = 0;
                    int av_y = 0;
                    int hits = 0;

                    for (int xx = x - step_size; xx < x + step_size; xx++)
                    {
                        if ((xx > -1) && (xx < width))
                        {
                            for (int yy = y - step_size; yy < y + step_size; yy++)
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
                        line.Add(av_x, av_y);
                    }
                }
            }
            else
            {
                for (int x = tx; x < bx; x += step_size)
                {
                    int y = ty + (dy * (x - tx) / dx);

                    int av_x = 0;
                    int av_y = 0;
                    int hits = 0;

                    for (int xx = x - step_size; xx < x + step_size; xx++)
                    {
                        if ((xx > -1) && (xx < width))
                        {
                            for (int yy = y - step_size; yy < y + step_size; yy++)
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
                        line.Add(av_x, av_y);
                    }
                }
            }
            return (line);
        }

        /// <summary>
        /// return the grid coordinate given the screen coordinate
        /// </summary>
        /// <param name="image_x"></param>
        /// <param name="image_y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="grid_x"></param>
        /// <param name="grid_y"></param>
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

        private void detectVerticalLines(int width, int height)
        {
            vertical_lines = new ArrayList();
            int search_width = width / 10;
            int line_width = 5;
            int start_y = height / 20;
            int end_y = height - start_y;
            int prev_x_top = 0;
            int prev_x_bottom = 0;

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
                    for (int x2 = x - search_width; x2 < x + search_width; x2++)
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
                        calibration_line line = traceLine(width, height, x, start_y, winner, end_y);
                        vertical_lines.Add(line);
                        if (showLines) line.Draw(edges_image, width, height, 255, 0, 0);
                    }
                }
            }
        }


        private void detectHorizontalLines(int width, int height)
        {
            horizontal_lines = new ArrayList();
            int search_width = height / 10;
            int line_width = 5;
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
                        calibration_line line = traceLine(width, height, start_x, y, end_x, winner);
                        horizontal_lines.Add(line);
                        if (showLines) line.Draw(edges_image, width, height, 255, 0, 0);
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

        /// <summary>
        /// show a centre line guide to help when aligning the calibration pattern
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void showVerticalCentreline(Byte[] img, int width, int height)
        {
            int x = width/2;
            int y = height/12;
            util.drawLine(img, width, height, x - 2, 0, x, y, 100, 100, 255, 0, false);
            util.drawLine(img, width, height, x, y, x + 2, 0, 100, 100, 255, 0, false);
            util.drawLine(img, width, height, x - 2, height-1, x, height-y, 100, 100, 255, 0, false);
            util.drawLine(img, width, height, x, height-y, x + 2, height-1, 100, 100, 255, 0, false);

            x = width / 12;
            y = height / 2;
            util.drawLine(img, width, height, 0, y - 2, x, y, 100, 100, 255, 0, false);
            util.drawLine(img, width, height, x, y, 0, y + 2, 100, 100, 255, 0, false);
            util.drawLine(img, width, height, width-1, y - 2, width-x, y, 100, 100, 255, 0, false);
            util.drawLine(img, width, height, width-x, y, width-1, y + 2, 100, 100, 255, 0, false);
        }


        public void Update(Byte[] img, int width, int height)
        {
            // create lists to store edges
            edges_horizontal = new ArrayList();
            edges_vertical = new ArrayList();
            edges_image = new Byte[width * height * 3];
            edges_binary = new bool[width, height];
            horizontal_magnitude = new int[2,width];
            vertical_magnitude = new int[2,height];
            if (binary_image == null) binary_image = new bool[no_of_images, width, height];
            for (int i = 0; i < img.Length; i++) edges_image[i] = img[i];

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
                detectCorners(width, height);

                corners_index++;
                if (corners_index >= corners.Length) corners_index = 0;

                drawGrid(edges_image, width, height);
            }

            showVerticalCentreline(edges_image, width, height);

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
}
