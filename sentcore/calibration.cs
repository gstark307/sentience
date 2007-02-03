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

        Byte[] calibration_image;
        public Byte[] edges_image;
        ArrayList edges_horizontal;
        ArrayList edges_vertical;
        ArrayList[] corners;
        int corners_index = 0;

        const int no_of_images = 5;
        int binary_image_index = 0;
        bool[, ,] binary_image;
        bool[,] edges_binary;

        int[,] horizontal_magnitude;
        int[,] vertical_magnitude;
        ArrayList horizontal_lines, vertical_lines;

        private void detectCorners(int width, int height)
        {
            if (corners == null)
            {
                corners = new ArrayList[5];
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
                                        int cg_x = 0;
                                        int cg_y = 0;
                                        localCG(calibration_image, width, height, (int)ix, (int)iy, 10, ref cg_x, ref cg_y);
                                        calibration_point intersection_point = new calibration_point(cg_x, cg_y);
                                        corners[corners_index].Add(intersection_point);
                                        line1.intersections.Add(intersection_point);
                                        line2.intersections.Add(intersection_point);                                        
                                    }
                                }
                                prev_pt2 = pt2;
                            }
                        }
                    }
                    prev_pt = pt;
                }
            }

            for (int i = 0; i < corners.Length; i++)
            {
                for (int j = 0; j < corners[i].Count; j++)
                {
                    calibration_point pt = (calibration_point)corners[i][j];
                    util.drawCross(edges_image, width, height, pt.x, pt.y, 3, 255, 0, 0, 0);
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
            int inhibit_radius = width / 20;
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
            int inhibit_radius = height / 20;
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

        /// <summary>
        /// returns the local centre of gravity
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="radius"></param>
        /// <param name="cg_x"></param>
        /// <param name="cg_y"></param>
        private void localCG(Byte[] img, int width, int height, int x, int y, int radius,
                             ref int cg_x, ref int cg_y)
        {
            cg_x = 0;
            cg_y = 0;
            int max_score = 0;
            for (int xx = x - radius; xx < x + radius; xx++)
            {
                if ((xx > -1) && (xx < width))
                {
                    for (int yy = y - radius; yy < y + radius; yy++)
                    {
                        if ((yy > -1) && (yy < height))
                        {
                            int score = 0;
                            for (int xx2 = xx - radius; xx2 < xx + radius; xx2++)
                            {
                                if ((xx2 > -1) && (xx2 < width))
                                {
                                    int n = (yy * width) + xx2;
                                    score += (255 - img[n]);
                                }
                            }
                            for (int yy2 = yy - radius; yy2 < yy + radius; yy2++)
                            {
                                if ((yy2 > -1) && (yy2 < height))
                                {
                                    int n = (yy2 * width) + xx;
                                    score += (255 - img[n]);
                                }
                            }
                            if (score > max_score)
                            {
                                max_score = score;
                                cg_x = xx;
                                cg_y = yy;
                            }
                        }
                    }
                }
            }
        }

        private void updateEdgesImage(int width, int height)
        {
            for (int x = 5; x < width-5; x++)
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

                        /*
                        int n = ((y * width) + x) * 3;
                        edges_image[n] = 0;
                        edges_image[n + 1] = (Byte)255;
                        edges_image[n + 2] = 0;
                         */
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
                for (int x = width * 9 / 10; x < width - 1; x++)
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

        private void detectVerticalLines(int width, int height)
        {
            vertical_lines = new ArrayList();
            int search_width = width / 10;
            int line_width = 5;
            int start_y = height / 20;
            int end_y = height - start_y;
            
            for (int x = 0; x < width; x++)
            {
                if (horizontal_magnitude[0, x] > 0)
                {
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

            for (int y = 0; y < height; y++)
            {
                if (vertical_magnitude[0, y] > 0)
                {
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

            detectCorners(width, height);

            corners_index++;
            if (corners_index >= corners.Length) corners_index = 0;
            binary_image_index++;
            if (binary_image_index >= no_of_images) binary_image_index = 0;
        }
    }
}
