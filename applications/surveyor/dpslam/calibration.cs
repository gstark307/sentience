/*
    Camera calibration class, used for automatic calibration with a pattern of lines
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
using System.IO;
using System.Xml;
using System.Collections;
using sentience.core;
using sluggish.utilities;
using sluggish.utilities.xml;

namespace sentience.calibration
{
    /// <summary>
    /// scheduled for deletion
    /// </summary>
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
    /// scheduled for deletion - defines a region of interest containing the calibration pattern
    /// </summary>
    public class calibration_region_of_interest
    {
        public int tx, ty, bx, by;

        public void setTopLeft(int x, int y)
        {
            tx = x;
            ty = y;
        }

        public void setTopRight(int x, int y)
        {
            bx = x;
            ty = y;
        }

        public void setBottomLeft(int x, int y)
        {
            tx = x;
            by = y;
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
            drawing.drawLine(img, width, height, tx, ty, bx, ty, 0, 255, 0, 0, false);
            drawing.drawLine(img, width, height, bx, ty, bx, by, 0, 255, 0, 0, false);
            drawing.drawLine(img, width, height, bx, by, tx, by, 0, 255, 0, 0, false);
            drawing.drawLine(img, width, height, tx, by, tx, ty, 0, 255, 0, 0, false);
        }
    }

    /// <summary>
    /// scheduled for deletion
    /// </summary>
    public class calibration_line
    {
        private float lin = -1;
        public ArrayList points = new ArrayList();
        public ArrayList intersections = new ArrayList();
        public int index;  // grid index of the line

        public void Add(int x, int y)
        {
            points.Add(new calibration_point(x, y));
        }

        /// <summary>
        /// adds a point to the line
        /// </summary>
        /// <param name="line"></param>
        public void Add(calibration_line line)
        {
            for (int i = 0; i < line.points.Count; i++)
                points.Add((calibration_point)line.points[i]);
        }

        /// <summary>
        /// reverses the order of the points within the line
        /// </summary>
        public void Reverse()
        {
            ArrayList new_points = new ArrayList();
            for (int i = points.Count-1; i >= 0; i--)
                new_points.Add((calibration_point)points[i]);
            points = new_points;
        }

        /// <summary>
        /// It's a linear love-in!
        /// Match this line as closely as possible to the original image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="max_deviance"></param>
        public void Hugg(Byte[] image, int width, int height,
                         int max_deviance)
        {
            calibration_point p1 = (calibration_point)points[0];
            calibration_point p2 = (calibration_point)points[points.Count - 1];
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;

            for (int i = 0; i < points.Count; i++)
            {
                calibration_point p = (calibration_point)points[i];

                int max_diff = 0;
                if (Math.Abs(dy) > Math.Abs(dx))
                {
                    int winner = (int)p.x;
                    for (int x = (int)p.x - max_deviance; x < (int)p.x + max_deviance; x++)
                    {
                        int diff = 0;
                        if ((x > max_deviance) && (x < width - 1 - max_deviance))
                        {
                            for (int y = (int)p.y - max_deviance; y < (int)p.y + max_deviance; y++)
                            {
                                if ((y > max_deviance) && (y < height - 1 - max_deviance))
                                {
                                    int n = (y * width) + x;
                                    diff += ((image[n - 2] + image[n + 2]) / 2) -
                                              image[n];
                                }
                            }
                        }
                        if (diff > max_diff)
                        {
                            winner = x;
                            max_diff = diff;
                        }
                    }
                    p.x = winner;
                }
                else
                {
                    int winner = (int)p.y;
                    for (int y = (int)p.y - max_deviance; y < (int)p.y + max_deviance; y++)
                    {
                        int diff = 0;
                        if ((y > max_deviance) && (y < height - 1 - max_deviance))
                        {
                            for (int x = (int)p.x - max_deviance; x < (int)p.x + max_deviance; x++)
                            {
                                if ((x > max_deviance) && (x < width - 1 - max_deviance))
                                {
                                    int n = (y * width) + x;
                                    diff += ((image[n - (width*2)] + image[n + (width*2)]) / 2) -
                                              image[n];
                                }
                            }
                        }
                        if (diff > max_diff)
                        {
                            winner = y;
                            max_diff = diff;
                        }
                    }
                    p.y = winner;
                }
            }
        }

        /// <summary>
        /// returns a value (in pixels) indicating on average how far from straight
        /// this line is
        /// </summary>
        /// <returns></returns>
        public float Linearity()
        {
            float linearity = 0;

            if (lin == -1)
            {

                calibration_point p1 = (calibration_point)points[0];
                calibration_point p2 = (calibration_point)points[points.Count - 1];
                float dx = p2.x - p1.x;
                float dy = p2.y - p1.y;

                const int no_of_points = 30;
                for (int j = 0; j < no_of_points; j++)
                {
                    calibration_point p3 = (calibration_point)points[(points.Count - 1) * j / no_of_points];
                    float mid_x = p1.x + (dx * j / no_of_points);
                    float mid_y = p1.y + (dy * j / no_of_points);

                    if (Math.Abs(dy) > dx)
                    {
                        linearity += Math.Abs(mid_x - p3.x);
                        for (int i = 1; i < points.Count; i++)
                        {
                            if (((calibration_point)points[i]).y < ((calibration_point)points[i - 1]).y)
                                linearity = 99999;
                        }
                    }
                    else
                    {
                        linearity += Math.Abs(mid_y - p3.y);
                        for (int i = 1; i < points.Count; i++)
                        {
                            if (((calibration_point)points[i]).x < ((calibration_point)points[i - 1]).x)
                                linearity = 99999;
                        }
                    }
                }
                linearity /= no_of_points;

                // store this value so that it may be simply looked up on subsequent calls
                lin = linearity;  
            }
            else linearity = lin;
            return (linearity);
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
                drawing.drawLine(img, width, height, (int)p1.x, (int)p1.y, (int)p2.x, (int)p2.y, r, g, b, 0, false);
            }
        }
    }

    /// <summary>
    /// scheduled for deletion
    /// </summary>
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

    /// <summary>
    /// scheduled for deletion
    /// </summary>
    public class calibration
    {
        Random rnd = new Random();

        // the position of the centre spot relative to the centre of the calibration pattern
        public const int CENTRE_SPOT_NW = 0;
        public const int CENTRE_SPOT_NE = 1;
        public const int CENTRE_SPOT_SE = 2;
        public const int CENTRE_SPOT_SW = 3;
        public int centre_spot_position = CENTRE_SPOT_NW;

        public float separation_factor = 1.0f / 30.0f;

        //horizontal offset of the camera relative to the centre of the calibration pattern
        public float baseline_offset = 0;

        // position of the camera relative to the calibration pattern
        public float camera_height_mm = 785;
        public float camera_dist_to_pattern_centre_mm = 500;

        calibration_edge[,] coverage;

        float vertical_gradient = 0.05f;

        // the size of each square on the calibration pattern
        public float calibration_pattern_spacing_mm = 50;

        // horizontal field of vision
        public float camera_FOV_degrees = 50;

        public String camera_name = "";
        public int image_width=320, image_height=240;
        public float min_RMS_error = 999999;
        public float polyfit_error = 999999;

        private bool isValidRectification;

        // the centre of distortion in image pixel coordinates
        calibration_point centre_of_distortion;
        calibration_point centre_spot;        

        public int grid_centre_x, grid_centre_y;
        public calibration_edge[,] grid;

        public int[] calibration_map;
        public int[] temp_calibration_map;
        public int[,,] calibration_map_inverse;
        public int[, ,] temp_calibration_map_inverse;
        public byte[] rectified_image;

        public byte[] corners_image;
        public byte[] edges_image;
        public byte[] centrealign_image;
        public byte[] lines_image;
        public byte[] curve_fit;

        polynomial fitter, best_curve;
        public float temp_scale, scale=1;
        public float rotation = 0;

        // centre of the calibration pattern
        public float distance_to_pattern_centre = 0;
        public float pattern_centre_x = 0;
        public float pattern_centre_y = 0;
        public calibration_point pattern_centre_rectified;

        byte[] calibration_image;
        ArrayList edges_horizontal;
        ArrayList edges_vertical;
        ArrayList detected_corners;
        ArrayList[] corners;        
        int corners_index = 0;

        // a minimum number of frames should be grabbed and analysed before
        // coming to a decision.  This prevents the system from looking at
        // one or two lucky but erroneous images and drawing a false conclusion
        int samples = 0;
        const int min_samples = 10;

        int closest_to_centreline_x = 0;
        int closest_to_centreline_y = 0;
        int av_centreline_x = 0;
        int av_centreline_x_hits = 0;
        int av_centreline_y = 0;
        int av_centreline_y_hits = 0;

        // threshold used when detecting horizontal and vertical lines
        int min_connectedness = 30;

        const int no_of_images = 4;
        int binary_image_index = 0;
        bool[, ,] binary_image;
        bool[,] edges_binary;

        int[,] horizontal_magnitude;
        int[,] vertical_magnitude;
        ArrayList horizontal_lines, vertical_lines;
        ArrayList all_horizontal_lines;
        ArrayList all_vertical_lines;

        #region "region of interest"

        public calibration_region_of_interest ROI;

        /// <summary>
        /// sets the top left or bottom right point of the region of interest
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="topLeft"></param>
        public void setRegionOfInterestPoint(int x, int y)
        {
            if (ROI == null) ROI = new calibration_region_of_interest();

            if (y < image_height / 2)
            {
                if (x < image_width / 2)
                    ROI.setTopLeft(x, y);
                else
                    ROI.setTopRight(x, y);
            }
            else
            {
                if (x < image_width / 2)
                    ROI.setBottomLeft(x, y);
                else
                    ROI.setBottomRight(x, y);
            }
        }

        #endregion

        #region "centre spot detection"

        /// <summary>
        /// updates the rectified position of the centre of the calibration pattern.  This can be used to
        /// calculate the horizontal and vertical alignment offsets for stereo cameras
        /// </summary>
        private void updatePatternCentreRectified()
        {
            int rectified_x = 0, rectified_y = 0;

            if (rectifyPoint((int)pattern_centre_x, (int)pattern_centre_y, ref rectified_x, ref rectified_y))
            {
                pattern_centre_rectified = new calibration_point(rectified_x, rectified_y);
            }
        }


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

            int search_radius_x = (int)(width * separation_factor / 1.0f);
            if (search_radius_x < 2) search_radius_x = 2;
            int search_radius_y = (int)(height * separation_factor / 1.0f);
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
                int px = (int)(coverage[cx, cy].x / coverage[cx, cy].hits);
                int py = (int)(coverage[cx, cy].y / coverage[cx, cy].hits);
                drawing.drawBox(corners_image, width, height, px, py, 4, 255, 255, 255, 0);
            }
            else
            {
                bool found = false;
                int r = 0;
                while ((r < 3) && (!found))
                {
                    int xx = cx - r;
                    int yy = cy;
                    while ((xx <= cx + r) && (!found))
                    {
                        yy = cy - r;
                        while ((yy <= cy + r) && (!found))
                        {
                            if (coverage[xx, yy] != null)
                            {
                                grid_x = coverage[xx, yy].grid_x;
                                grid_y = coverage[xx, yy].grid_y;
                                int px = (int)(coverage[xx, yy].x / coverage[xx, yy].hits);
                                int py = (int)(coverage[xx, yy].y / coverage[xx, yy].hits);
                                drawing.drawBox(corners_image, width, height, px, py, 4, 255, 255, 255, 0);
                                found = true;
                            }
                            yy++;
                        }
                        xx++;
                    }
                    r++;
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
                            drawing.drawCross(img, width, height, (int)grid[x, y].rectified_x, (int)grid[x, y].rectified_y, 2, 255, 255, 0, 0);
                            
                            if (rectifyPoint((int)grid[x, y].x, (int)grid[x, y].y, ref rectified_x, ref rectified_y))
                            {
                                //util.drawCross(img, width, height, rectified_x, rectified_y, 2, 255, 255, 255, 0);
                                drawing.drawLine(img, width, height, (int)grid[x, y].rectified_x, (int)grid[x, y].rectified_y, rectified_x, rectified_y, 255, 255, 255, 0, false);
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
                corners = new ArrayList[5];
                for (int i = 0; i < corners.Length; i++)
                    corners[i] = new ArrayList();
            }            
            corners[corners_index].Clear();

            // clear intersection points
            for (int i = 0; i < all_horizontal_lines.Count; i++)
            {
                calibration_line line = (calibration_line)all_horizontal_lines[i];
                line.intersections.Clear();
            }
            for (int i = 0; i < all_vertical_lines.Count; i++)
            {
                calibration_line line = (calibration_line)all_vertical_lines[i];
                line.intersections.Clear();
            }


            for (int i = 0; i < all_horizontal_lines.Count; i++)
            {
                calibration_line line1 = (calibration_line)all_horizontal_lines[i];
                calibration_point prev_pt = null;
                for (int j = 0; j < line1.points.Count; j++)
                {
                    calibration_point pt = (calibration_point)line1.points[j];
                    if (j > 0)
                    {
                        for (int i2 = 0; i2 < all_vertical_lines.Count; i2++)
                        {
                            calibration_line line2 = (calibration_line)all_vertical_lines[i2];
                            calibration_point prev_pt2 = null;
                            for (int j2 = 0; j2 < line2.points.Count; j2++)
                            {
                                calibration_point pt2 = (calibration_point)line2.points[j2];
                                if (j2 > 0)
                                {
                                    float ix = 0;
                                    float iy = 0;
                                    if (geometry.intersection((float)prev_pt.x, (float)prev_pt.y, (float)pt.x, (float)pt.y,
                                                          (float)prev_pt2.x, (float)prev_pt2.y, (float)pt2.x, (float)pt2.y,
                                                          ref ix, ref iy))
                                    {
                                        //int av = averageIntensity(calibration_image, width, height, (int)ix - 10, (int)iy - 10, (int)ix + 10, (int)iy + 10);
                                        //if (av < 170)
                                        {
                                            calibration_edge intersection_point = new calibration_edge(ix, iy, 0);

                                            intersection_point.grid_x = line2.index; // i2;
                                            intersection_point.grid_y = line1.index; // i;
                                            corners[corners_index].Add(intersection_point);
                                            line1.intersections.Add(intersection_point);
                                            line2.intersections.Add(intersection_point);

                                            //util.drawCross(corners_image, width, height, (int)ix, (int)iy, 2, 255, 255, 255, 0);
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

                    int px = (int)(pt.x / pt.hits);
                    int py = (int)(pt.y / pt.hits);

                    if ((px < width) && (py < height))
                    {
                        if (pt.grid_x < grid_tx) grid_tx = pt.grid_x;
                        if (pt.grid_y < grid_ty) grid_ty = pt.grid_y;
                        if (pt.grid_x > grid_bx) grid_bx = pt.grid_x;
                        if (pt.grid_y > grid_by) grid_by = pt.grid_y;

                        if (coverage[px, py] == null)
                        {
                            int radius = (int)(width * separation_factor / 1.5f);
                            if (radius < 3) radius = 3;
                            for (int xx = px - radius; xx <= px + radius; xx++)
                            {
                                if ((xx > -1) && (xx < width))
                                {
                                    for (int yy = py - radius; yy <= py + radius; yy++)
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
                            coverage[px, py].x += px;
                            coverage[px, py].y += py;
                            coverage[px, py].hits++;

                            if (coverage[px, py].hits > 1000)
                            {
                                coverage[px, py].x /= 2;
                                coverage[px, py].y /= 2;
                                coverage[px, py].hits /= 2;
                            }

                            if (pt.grid_x < coverage[px, py].grid_x)
                                coverage[px, py].grid_x = pt.grid_x;
                            if (pt.grid_y < coverage[px, py].grid_y)
                                coverage[px, py].grid_y = pt.grid_y;
                            detected_corners.Add(coverage[px, py]);
                        }
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
                //pt.x /= pt.hits;
                //pt.y /= pt.hits;
                //pt.hits = 1;
                int gx = pt.grid_x - grid_tx;
                int gy = pt.grid_y - grid_ty;
                if (grid != null)
                {
                    grid[gx, gy] = new calibration_edge(pt.x/pt.hits,pt.y/pt.hits,1);
                }
            }

            // show the corner positions
            showCorners(width, height);
        }

        private void showCorners(int width, int height)
        {
            if (grid != null)
            {
                for (int grid_x = 0; grid_x < grid.GetLength(0); grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid.GetLength(1); grid_y++)
                    {
                        calibration_edge pt = (calibration_edge)grid[grid_x, grid_y];
                        if (pt != null)
                            drawing.drawCross(corners_image, width, height, (int)pt.x, (int)pt.y, 3, 255, 0, 0, 0);
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
                        if (ROI == null)
                        {
                            index = y * horizontal_magnitude.GetLength(0) / height;
                        }
                        else
                        {
                            index = (y-ROI.ty) * horizontal_magnitude.GetLength(0) / (ROI.by - ROI.ty);
                        }
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
                        if (ROI == null)
                        {
                            index = x * vertical_magnitude.GetLength(0) / width;
                        }
                        else
                        {
                            index = (x - ROI.tx) * vertical_magnitude.GetLength(0) / (ROI.bx - ROI.tx);
                        }
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

            // non-maximal supression in the horizontal
            int search_width = (int)(width * separation_factor * 1.0f);
            for (int i = 0; i < horizontal_magnitude.GetLength(0); i++)
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
            }

            // non-maximal supression in the vertical
            search_width = (int)(height * separation_factor*2.0f);
            for (int i = 0; i < vertical_magnitude.GetLength(0); i++)
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
            }
        }

        private void detectHorizontalEdges(Byte[] calibration_image, int width, int height, ArrayList edges, int min_magnitude)
        {            
            int search_radius = 2;
            ArrayList temp_edges = new ArrayList();            

            edges.Clear();
            for (int y = 1; y < height - 1; y++)
            {
                float factor = y / (float)height * width * 0.8f;
                int inhibit_radius = (int)((width + factor) * separation_factor);
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
                for (int i = 0; i < temp_edges.Count-1; i++)
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
            //int inhibit_radius = height / separation_factor;
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
                for (int i = 0; i < temp_edges.Count-1; i++)
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
                                    //float factor = (e1.y - (height / 2)) * vertical_gradient;
                                    float factor = e1.y / (float)height * width * 0.8f;
                                    int inhibit_radius = (int)((height + factor) * separation_factor);

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

        #region "line detection"

        // detect the rotation
        private float detectRotation(int width, int height)
        {
            float rot = 0;
            int hits = 0;

            for (int i = 0; i < horizontal_lines.Count; i++)
            {
                calibration_line line = (calibration_line)horizontal_lines[i];
                calibration_point pt_start = (calibration_point)line.points[0];
                calibration_point pt_end = (calibration_point)line.points[line.points.Count - 1];
                float dx = pt_end.x - pt_start.x;
                float dy = pt_end.y - pt_start.y;
                float length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float ang = (float)Math.Asin(dy / length);

                rot += ang;
                hits++;
            }
            /*
            for (int i = 0; i < vertical_lines.Count; i++)
            {
                calibration_line line = (calibration_line)vertical_lines[i];
                calibration_point pt_start = (calibration_point)line.points[0];
                calibration_point pt_end = (calibration_point)line.points[line.points.Count - 1];
                float dx = pt_end.x - pt_start.x;
                float dy = pt_end.y - pt_start.y;
                float length = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float ang = (float)Math.Asin(-dx / length);

                rot += ang;
                hits++;
            }
             */
            if (hits > 0) rot /= horizontal_lines.Count;
            return (rot);
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

            int step_size = 10;
            int radius_x = (int)(width * separation_factor / 4);
            int radius_x2 = (int)(width * separation_factor * 10 / 4);
            if (radius_x < 1) radius_x = 1;
            int radius_y = (int)(height * separation_factor / 4);
            int radius_y2 = (int)(height * separation_factor * 10 / 4);
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
                step_size = radius_y2 * 3 / 2; // *3 / 4;
                if (step_size < 1) step_size = 1;
                for (int y = ty; y < by; y += step_size * height / 240)
                {
                    int x = tx + (dx * (y - ty) / dy);

                    radius_x = (int)(width * separation_factor * 1.0f);
                    if (radius_x < 1) radius_x = 1;
                    radius_y = (int)(height * (separation_factor * 2.0f));
                    if (radius_y < 1) radius_y = 1;

                    int av_x = 0;
                    int av_y = 0;
                    int hits = 0;
                    for (int xx = x - radius_x; xx < x + radius_x; xx++)
                    {
                        if ((xx > 2) && (xx < width-2))
                        {
                            for (int yy = y - radius_y2; yy < y + radius_y2; yy++)
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
                        for (int xx = av_x - 3; xx <= av_x + 3; xx++)
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
                step_size = radius_x2 * 3 / 2; // *3 / 4;
                if (step_size < 1) step_size = 1;
                for (int x = tx; x < bx; x += step_size * width / 320)
                {
                    int y = ty + (dy * (x - tx) / dx);

                    radius_x = (int)(width * separation_factor * 0.5f);
                    if (radius_x < 1) radius_x = 1;
                    radius_y = (int)(height * separation_factor * 1.0f);
                    if (radius_y < 1) radius_y = 1;

                    int av_x = 0;
                    int av_y = 0;
                    int hits = 0;
                    //int max_diff = 0;

                    for (int xx = x - radius_x2; xx < x + radius_x2; xx++)
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
        /// returns a value indicating how connected the two points are
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        private int pointsConnected(int width, int height,
                                    int x1, int y1, int x2, int y2)
        {
            int line_search = 0;

            int x3 = x1;
            int y3 = y1;
            int x4 = x2;
            int y4 = y2;

            int dx = x2 - x1;
            int dy = y2 - y1;
            int hits = 0;
            int samples = 0;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                if (x1 > x2)
                {
                    x3 = x2;
                    y3 = y2;
                    x4 = x1;
                    y4 = y1;
                    dx = x4 - x3;
                    dy = y4 - y3;
                }

                for (int x = x3; x <= x4; x++)
                {
                    int y = y3;
                    if (dx !=0)
                        y = y3 + ((x - x3) * dy / dx);

                    for (int yy = y - line_search; yy <= y + line_search; yy++)
                    {
                        if ((yy > -1) && (yy < height))
                        {
                            if (edges_binary[x, yy]) hits++;
                            samples++;
                        }
                    }
                }
            }
            else
            {
                if (y1 > y2)
                {
                    x3 = x2;
                    y3 = y2;
                    x4 = x1;
                    y4 = y1;
                    dx = x4 - x3;
                    dy = y4 - y3;
                }

                for (int y = y3; y <= y4; y++)
                {
                    int x = x3;
                    if (dy!=0)
                        x = x3 + ((y - y3) * dx / dy);

                    for (int xx = x - line_search; xx <= x + line_search; xx++)
                    {
                        if ((xx > -1) && (xx < width))
                        {
                            if (edges_binary[xx, y]) hits++;
                            samples++;
                        }
                    }
                }
            }
            if (samples>0)
                return(hits * 100 / samples);
            else
                return(0);
        }

        private int pointsConnectedByIntensity(int width, int height,
                            int x1, int y1, int x2, int y2)
        {
            int line_search = 1;
            int outer = line_search + 3;

            int x3 = x1;
            int y3 = y1;
            int x4 = x2;
            int y4 = y2;

            int dx = x2 - x1;
            int dy = y2 - y1;
            int hits = 0;
            int samples = 0;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                if (x1 > x2)
                {
                    x3 = x2;
                    y3 = y2;
                    x4 = x1;
                    y4 = y1;
                    dx = x4 - x3;
                    dy = y4 - y3;
                }

                for (int x = x3; x <= x4; x++)
                {
                    int y = y3;
                    if (dx != 0)
                        y = y3 + ((x - x3) * dy / dx);

                    for (int yy = y - line_search; yy <= y + line_search; yy++)
                    {
                        if ((yy > -1) && (yy < height))
                        {
                            int n = (yy * width) + x;
                            int v = 255 - calibration_image[n];
                            if (yy + outer < height)
                                n = ((yy+outer) * width) + x;
                            else
                                n = ((yy-outer) * width) + x;
                            v -= (255 - calibration_image[n]);
                            if (v < 0) v = 0;
                            hits += (v * v);
                            samples++;
                        }
                    }
                }
            }
            else
            {
                if (y1 > y2)
                {
                    x3 = x2;
                    y3 = y2;
                    x4 = x1;
                    y4 = y1;
                    dx = x4 - x3;
                    dy = y4 - y3;
                }

                for (int y = y3; y <= y4; y++)
                {
                    int x = x3;
                    if (dy != 0)
                        x = x3 + ((y - y3) * dx / dy);

                    for (int xx = x - line_search; xx <= x + line_search; xx++)
                    {
                        if ((xx > -1) && (xx < width))
                        {
                            int n = (y * width) + xx;
                            int v = 255 - calibration_image[n];
                            if (x + outer < width)
                                n = (y * width) + xx + outer;
                            else
                                n = (y * width) + xx - outer;
                            v -= (255 - calibration_image[n]);
                            if (v < 0) v = 0;
                            hits += (v * v);
                            samples++;
                        }
                    }
                }
            }
            if (samples > 0)
                return (hits / samples);
            else
                return (0);
        }


        private void detectLinesSort(int width, int height, bool horizontal, ArrayList lines)
        {
            ArrayList victims = new ArrayList();

            // sort the lines into order
            for (int i = 0; i < lines.Count - 1; i++)
            {
                calibration_line line1 = (calibration_line)lines[i];
                calibration_point pt1 = (calibration_point)line1.points[0];
                for (int j = i + 1; j < lines.Count; j++)
                {
                    calibration_line line2 = (calibration_line)lines[j];
                    calibration_point pt2 = (calibration_point)line2.points[0];
                    calibration_point pt3 = (calibration_point)line2.points[line2.points.Count-1];
                    if (((horizontal) && (pt2.y < pt1.y)) ||
                        ((!horizontal) && (pt2.x < pt1.x)))
                    {
                        lines[i] = line2;
                        lines[j] = line1;
                        line1 = line2;
                        pt1 = (calibration_point)line1.points[0];
                    }
                    if (Math.Abs(pt3.x - pt1.x) < 2)
                        if (Math.Abs(pt3.y - pt1.y) < 2)
                            victims.Add(line1);
                }
            }

            if (ROI != null)
            {
                for (int i = 0; i < lines.Count - 1; i++)
                {
                    calibration_line line1 = (calibration_line)lines[i];
                    calibration_point pt1 = (calibration_point)line1.points[0];
                    calibration_point pt2 = (calibration_point)line1.points[line1.points.Count - 1];
                    if (!horizontal)
                    {
                        if ((pt1.y > ROI.ty + ((ROI.by-ROI.ty)/5)) ||
                            (pt2.y < ROI.by - ((ROI.by-ROI.ty)/5)))
                            if (!victims.Contains(line1))
                                victims.Add(line1);
                    }
                }
            }

            float max_linearity = 0.25f;
            for (int i = 0; i < lines.Count - 1; i++)
            {
                calibration_line line1 = (calibration_line)lines[i];
                float max_deviation = (width * separation_factor * max_linearity);
                if (horizontal) max_deviation = (height * separation_factor * max_linearity);
                //if (max_deviation < 2) max_deviation = 2;
                if (line1.Linearity() > max_deviation)
                    if (!victims.Contains(line1))
                        victims.Add(line1);
            }

            // remove any stragglers
            for (int i = 0; i < victims.Count; i++)
                lines.Remove((calibration_line)victims[i]);

            // remove lines which are too close together
            int max_horizontal_difference = (int)(width * separation_factor * 0.5f);
            int max_vertical_difference = (int)(height * separation_factor * 1.2f);
            int max_vertical_difference2;
            float vertical_adjust = height * separation_factor * 2.0f;
            for (int j = 0; j < 2; j++)
            {
                for (int i = lines.Count - 1; i > 0; i--)
                {
                    calibration_line line1 = (calibration_line)lines[i];
                    calibration_point pt1 = (calibration_point)line1.points[0];
                    calibration_line line2 = (calibration_line)lines[i - 1];
                    calibration_point pt2 = (calibration_point)line2.points[0];
                    max_vertical_difference2 = max_vertical_difference + (int)(pt1.y * vertical_adjust / height);
                    if (((!horizontal) && (pt1.x - pt2.x < max_horizontal_difference)) ||
                        ((horizontal) && (pt1.y - pt2.y < max_vertical_difference2)))
                    {
                        lines.RemoveAt(i);
                    }
                }
                for (int i = lines.Count - 1; i > 0; i--)
                {
                    calibration_line line1 = (calibration_line)lines[i];
                    calibration_point pt1 = (calibration_point)line1.points[line1.points.Count - 1];
                    calibration_line line2 = (calibration_line)lines[i - 1];
                    calibration_point pt2 = (calibration_point)line2.points[line2.points.Count - 1];
                    max_vertical_difference2 = max_vertical_difference + (int)(pt1.y * vertical_adjust / height);
                    if (((!horizontal) && (pt1.x - pt2.x < max_horizontal_difference)) ||
                        ((horizontal) && (pt1.y - pt2.y < max_vertical_difference2)))
                    {
                        lines.RemoveAt(i);
                    }
                }
            }
            for (int j = 0; j < 2; j++)
            {
                for (int i = 1; i < lines.Count; i++)
                {
                    calibration_line line1 = (calibration_line)lines[i];
                    calibration_point pt1 = (calibration_point)line1.points[0];
                    calibration_line line2 = (calibration_line)lines[i - 1];
                    calibration_point pt2 = (calibration_point)line2.points[0];
                    max_vertical_difference2 = max_vertical_difference + (int)(pt1.y * vertical_adjust / height);
                    if (((!horizontal) && (pt1.x - pt2.x < max_horizontal_difference)) ||
                        ((horizontal) && (pt1.y - pt2.y < max_vertical_difference2)))
                    {
                        lines.RemoveAt(i);
                    }
                }
                for (int i = 1; i < lines.Count; i++)
                {
                    calibration_line line1 = (calibration_line)lines[i];
                    calibration_point pt1 = (calibration_point)line1.points[line1.points.Count - 1];
                    calibration_line line2 = (calibration_line)lines[i - 1];
                    calibration_point pt2 = (calibration_point)line2.points[line2.points.Count - 1];
                    max_vertical_difference2 = max_vertical_difference + (int)(pt1.y * vertical_adjust / height);
                    if (((!horizontal) && (pt1.x - pt2.x < max_horizontal_difference)) ||
                        ((horizontal) && (pt1.y - pt2.y < max_vertical_difference2)))
                    {
                        lines.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// update the grid index numbers for each line
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="horizontal"></param>
        /// <param name="lines"></param>
        private void indexLines(int width, int height, bool horizontal, ArrayList lines)
        {
            float av_dx = 0;
            float av_dy = 0;
            for (int i = 1; i < lines.Count; i++)
            {
                calibration_line line1 = (calibration_line)lines[i - 1];
                calibration_point pt1 = (calibration_point)line1.points[line1.points.Count - 1];
                calibration_line line2 = (calibration_line)lines[i];
                calibration_point pt2 = (calibration_point)line2.points[line2.points.Count - 1];
                av_dx += (pt2.x - pt1.x);
                av_dy += (pt2.y - pt1.y);
            }
            if (lines.Count > 1)
            {
                av_dx /= (lines.Count - 1);
                av_dy /= (lines.Count - 1);
                int index = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    calibration_line line1 = (calibration_line)lines[i-1];
                    calibration_point pt1 = (calibration_point)line1.points[line1.points.Count - 1];
                    calibration_line line2 = (calibration_line)lines[i];
                    calibration_point pt2 = (calibration_point)line2.points[line2.points.Count - 1];
                    float dx = (pt2.x - pt1.x);
                    float dy = (pt2.y - pt1.y);

                    int diff = (int)(dy * 100 / av_dy);
                    int threshold = 130 + (int)(20 * pt2.y / height);
                    if (!horizontal)
                    {
                        diff = (int)(dx * 100 / av_dx);
                        threshold = 140;
                    }

                    line1.index = index;
                    if (diff < threshold)
                        index += 1;
                    else
                        index += 2;
                    line2.index = index;
                }
            }
        }

        private void updateAllLines(int width, int height, bool horizontal, ArrayList lines)
        {
            ArrayList all_lines=null;
            if (horizontal)
            {
                if (all_horizontal_lines == null) all_horizontal_lines = new ArrayList();
                all_lines = all_horizontal_lines;
            }
            else
            {
                if (all_vertical_lines == null) all_vertical_lines = new ArrayList();
                all_lines = all_vertical_lines;
            }

            int max_horizontal_variance = (int)(width * separation_factor * 0.8f);
            int max_vertical_variance = (int)(height * separation_factor * 0.8f);
            for (int i = 0; i < lines.Count; i++)
            {
                calibration_line line1 = (calibration_line)lines[i];
                calibration_point pt1 = (calibration_point)line1.points[0];
                bool found = false;
                int insert_index = 0;

                int j = 0;
                while ((j < all_lines.Count) && (!found))
                {
                    calibration_line line2 = (calibration_line)all_lines[j];
                    calibration_point pt2 = (calibration_point)line2.points[0];

                    if (horizontal)
                    {
                        if (pt2.y < pt1.y) insert_index = j;

                        float dy = pt2.y - pt1.y;
                        if (Math.Abs(dy) < max_vertical_variance)
                        {
                            if (line1.Linearity() < line2.Linearity())
                            {
                                all_lines[j] = line1;
                                found = true;
                            }
                        }
                    }
                    else
                    {
                        if (pt2.x < pt1.x) insert_index = j;

                        float dx = pt2.x - pt1.x;
                        if (Math.Abs(dx) < max_horizontal_variance)
                        {
                            if (line1.Linearity() < line2.Linearity())
                            {
                                all_lines[j] = line1;
                                found = true;
                            }
                        }
                    }
                    j++;
                }

                if (!found)
                {
                    if (all_lines.Count > 0)
                        all_lines.Insert(insert_index, line1);
                    else
                        all_lines.Add(line1);
                }

            }

            detectLinesSort(width, height, horizontal, all_lines);

            int hugg_radius = (int)(width * separation_factor * 0.5f);
            if (!horizontal) hugg_radius = (int)(height * separation_factor * 0.5f);
            for (int i = 0; i < all_lines.Count; i++)
            {
                calibration_line line = (calibration_line)all_lines[i];
                line.Hugg(calibration_image, width, height, hugg_radius);
            }
        }


        private void detectHorizontalLines(int width, int height)
        {
            horizontal_lines = new ArrayList();
            ArrayList[] vertical_positions = new ArrayList[vertical_magnitude.GetLength(0)];
            ArrayList[] vertical_positions_used = new ArrayList[vertical_magnitude.GetLength(0)];

            int max_x = width - (width / 4);
            if (ROI != null) max_x = ROI.bx;
            int min_x = width / 4;
            if (ROI != null) min_x = ROI.tx;

            // detect the positions of horizontal disscontinuities
            for (int i = 0; i < vertical_magnitude.GetLength(0); i++)
            {
                vertical_positions[i] = new ArrayList();
                vertical_positions_used[i] = new ArrayList();
                for (int y = 0; y < height; y++)
                    if (vertical_magnitude[i, y] > 0)
                    {
                        vertical_positions[i].Add(y);
                        vertical_positions_used[i].Add(false);
                        
                        int x = (width - 1) * i / vertical_magnitude.GetLength(0) + (width / (vertical_magnitude.GetLength(0)*2));
                        if (ROI != null)
                            x = ROI.tx + (i * (ROI.bx - ROI.tx) / vertical_magnitude.GetLength(0)) + ((ROI.bx - ROI.tx) / (vertical_magnitude.GetLength(0)*2));                         
                    }
            }
            
            int max_vertical_difference = (int)((height * separation_factor) / 1.0f);
            for (int j = 0; j < vertical_positions[0].Count; j++)
            {
                int y = (int)vertical_positions[0][j];
                int x = width / (vertical_magnitude.GetLength(0) * 2);
                if (ROI != null) x = ROI.tx;

                bool drawn = false;
                calibration_line line = new calibration_line();

                for (int i = 1; i < vertical_magnitude.GetLength(0); i++)
                {
                    int max_connectedness = min_connectedness;
                    int best_x = -1;
                    int best_y = -1;
                    int idx = -1;

                    for (int k = 0; k < vertical_positions[i].Count; k++)
                    {
                        int y2 = (int)vertical_positions[i][k];
                        int dy = y2 - y;
                        if (Math.Abs(dy) < max_vertical_difference)
                        {
                            int x2 = (width - 1) * i / vertical_magnitude.GetLength(0) + (width / (vertical_magnitude.GetLength(0) * 2));
                            if (ROI != null) x2 = ROI.tx + (i * (ROI.bx - ROI.tx) / vertical_magnitude.GetLength(0)) + ((ROI.bx - ROI.tx) / (vertical_magnitude.GetLength(0) * 2));

                            // are these two connected?
                            int connectedness = pointsConnectedByIntensity(width, height, x, y, x2, y2);
                            if (connectedness > max_connectedness)
                            {
                                best_x = x2;
                                best_y = y2;
                                max_connectedness = connectedness;
                                idx = k;
                            }
                        }
                    }

                    if (best_x > -1)
                    {
                        vertical_positions_used[i][idx] = true;
                        calibration_line temp_line = traceLine(width, height, x, y, best_x, best_y);
                        line.Add(temp_line);
                        x = best_x;
                        y = best_y;
                        drawn = true;
                    }
                }

                if (drawn)
                {
                    calibration_line temp_line = traceLine(width, height, x, y, max_x, y);
                    line.Add(temp_line);
                    if (line.points.Count > 0)
                        horizontal_lines.Add(line);
                }
            }

            int index = vertical_magnitude.GetLength(0) - 1;
            for (int j = 0; j < vertical_positions[index].Count; j++)
            {
                if ((bool)vertical_positions_used[index][j] == false)
                {
                    int y = (int)vertical_positions[index][j];
                    int x = width - (width / (vertical_magnitude.GetLength(0) * 2));
                    if (ROI != null) x = ROI.bx - ((ROI.bx - ROI.tx) / (vertical_magnitude.GetLength(0) * 2));

                    bool drawn = false;
                    calibration_line line = new calibration_line();

                    for (int i = vertical_magnitude.GetLength(0) - 2; i >= 0; i--)
                    {
                        int max_connectedness = min_connectedness;
                        int best_x = -1;
                        int best_y = -1;
                        int idx = -1;

                        for (int k = 0; k < vertical_positions[i].Count; k++)
                        {
                            int y2 = (int)vertical_positions[i][k];
                            int dy = y2 - y;
                            if (Math.Abs(dy) < max_vertical_difference)
                            {
                                int x2 = width - ((width - 1) * i / vertical_magnitude.GetLength(0) + (width / (vertical_magnitude.GetLength(0) * 2)));
                                if (ROI != null) x2 = ROI.bx - (i * (ROI.bx - ROI.tx) / vertical_magnitude.GetLength(0)) + ((ROI.bx - ROI.tx) / (vertical_magnitude.GetLength(0) * 2));

                                // are these two connected?
                                int connectedness = pointsConnectedByIntensity(width, height, x, y, x2, y2);
                                if (connectedness > max_connectedness)
                                {
                                    best_x = x2;
                                    best_y = y2;
                                    max_connectedness = connectedness;
                                    idx = k;
                                }
                            }
                        }

                        if (best_x > -1)
                        {
                            calibration_line temp_line = traceLine(width, height, best_x, best_y, x, y);
                            temp_line.Reverse();
                            line.Add(temp_line);
                            x = best_x;
                            y = best_y;
                            drawn = true;
                        }
                    }

                    if (drawn)
                    {
                        calibration_line temp_line = traceLine(width, height, min_x, y, x, y);
                        temp_line.Reverse();
                        line.Add(temp_line);
                        line.Reverse();
                        if (line.points.Count > 0)
                            horizontal_lines.Add(line);                        
                    }
                }
            }

            // sort and prune
            detectLinesSort(width, height, true, horizontal_lines);

            // remove first and last lines, since they're often poorly detected
            if (horizontal_lines.Count > 2)
            {
                horizontal_lines.RemoveAt(horizontal_lines.Count - 1);
                horizontal_lines.RemoveAt(0);
            }

            updateAllLines(width, height, true, horizontal_lines);

            // add index numbers to the lines
            indexLines(width, height, true, all_horizontal_lines);

            // draw
            for (int i = 0; i < all_horizontal_lines.Count; i++)
            {
                calibration_line line = (calibration_line)all_horizontal_lines[i];
                line.Draw(lines_image, width, height, 255, 0, 0);
            }
        }


        private void detectVerticalLines(int width, int height)
        {
            vertical_lines = new ArrayList();
            ArrayList[] horizontal_positions = new ArrayList[horizontal_magnitude.GetLength(0)];
            ArrayList[] horizontal_positions_used = new ArrayList[horizontal_magnitude.GetLength(0)];

            int max_y = height - (height / 4);
            if (ROI != null) max_y = ROI.by;
            int min_y = width / 4;
            if (ROI != null) min_y = ROI.ty;

            float bottom_spacing = 0;
            float top_spacing = 0;

            // detect the positions of horizontal disscontinuities
            for (int i = 0; i < horizontal_magnitude.GetLength(0); i++)
            {
                horizontal_positions[i] = new ArrayList();
                horizontal_positions_used[i] = new ArrayList();
                int n = 0;
                int prev_x = -1;
                for (int x = 0; x < width; x++)
                    if (horizontal_magnitude[i, x] > 0)
                    {
                        horizontal_positions[i].Add(x);
                        horizontal_positions_used[i].Add(false);

                        int y = (height - 1) * i / horizontal_magnitude.GetLength(0) + (height / (horizontal_magnitude.GetLength(0) * 2));
                        if (ROI != null)
                            y = ROI.ty + (i * (ROI.by - ROI.ty) / horizontal_magnitude.GetLength(0)) + ((ROI.by - ROI.ty) / (horizontal_magnitude.GetLength(0) * 2));

                        if (prev_x > -1)
                        {
                            if (i == 0) top_spacing += (x - prev_x);
                            if (i == horizontal_magnitude.GetLength(0)-1) bottom_spacing += (x - prev_x);
                            n++;
                        }
                        prev_x = x;
                    }
                if ((i == 0) && (n > 0)) top_spacing /= n;
                if ((i == horizontal_magnitude.GetLength(0) - 1) && (n > 0)) bottom_spacing /= n;
            }

            // calculate the vertical gradient using the top and bottom horizontal spacings
            if ((top_spacing > 0) && (bottom_spacing > 0))
            {
                if (bottom_spacing < top_spacing * 1.3f)
                    bottom_spacing = top_spacing * 1.3f;

                if (ROI != null)
                    vertical_gradient = (bottom_spacing - top_spacing) / (float)(ROI.by - ROI.ty);
                else
                    vertical_gradient = (bottom_spacing - top_spacing) / (float)(height*3/4);
            }

            float vertical_additive = 0.5f;

            int max_horizontal_difference = (int)(width * separation_factor * 1.9f);
            for (int j = 0; j < horizontal_positions[0].Count; j++)
            {
                int x = (int)horizontal_positions[0][j];
                int y = height / (horizontal_magnitude.GetLength(0) * 2);
                if (ROI != null) y = ROI.ty;

                bool drawn = false;
                calibration_line line = new calibration_line();

                for (int i = 1; i < horizontal_magnitude.GetLength(0); i++)
                {
                    int max_connectedness = min_connectedness;
                    int best_x = -1;
                    int best_y = -1;
                    int idx = -1;

                    for (int k = 0; k < horizontal_positions[i].Count; k++)
                    {
                        int x2 = (int)horizontal_positions[i][k];
                        int dx = x2 - x;
                        if (Math.Abs(dx) < max_horizontal_difference)
                        {
                            int y2 = (height - 1) * i / horizontal_magnitude.GetLength(0) + (height / (horizontal_magnitude.GetLength(0) * 2));
                            if (ROI != null) y2 = ROI.ty + (i * (ROI.by - ROI.ty) / horizontal_magnitude.GetLength(0)) + ((ROI.by - ROI.ty) / (horizontal_magnitude.GetLength(0) * 2));

                            // are these two connected?
                            if ((y < height) && (y2 < height))
                            {
                                int grid_width = (int)(top_spacing + (vertical_additive * (bottom_spacing - top_spacing) * y2 / height));
                                if (ROI != null)
                                    grid_width = (int)(top_spacing + (vertical_additive * (bottom_spacing - top_spacing) * (y2 - ROI.ty) / (ROI.by - ROI.ty)));

                                if (!(((x > width * 55 / 100) && (x>x2) && (x2 < x + grid_width)) ||
                                    ((x < width * 45 / 100) && (x < x2) && (x2 > x - grid_width))))
                                {
                                    int connectedness = pointsConnectedByIntensity(width, height, x, y, x2, y2);
                                    if (connectedness > max_connectedness)
                                    {
                                        best_x = x2;
                                        best_y = y2;
                                        max_connectedness = connectedness;
                                        idx = k;
                                    }
                                }
                            }
                        }
                        
                    }

                    if (best_x > -1)
                    {
                        horizontal_positions_used[i][idx] = true;
                        calibration_line temp_line = traceLine(width, height, x, y, best_x, best_y);
                        line.Add(temp_line);
                        x = best_x;
                        y = best_y;
                        drawn = true;
                    }
                }

                if (drawn)
                {
                    calibration_line temp_line = traceLine(width, height, x, y, x, max_y);
                    line.Add(temp_line);
                    if (line.points.Count > 0)
                        vertical_lines.Add(line);
                }
            }

            int index = horizontal_magnitude.GetLength(0) - 1;
            for (int j = 0; j < horizontal_positions[index].Count; j++)
            {
                if ((bool)horizontal_positions_used[index][j] == false)
                {
                    int x = (int)horizontal_positions[index][j];
                    int y = height - (height / (horizontal_magnitude.GetLength(0) * 2));
                    if (ROI != null) y = ROI.by - ((ROI.by - ROI.ty) / (horizontal_magnitude.GetLength(0) * 2));

                    bool drawn = false;
                    calibration_line line = new calibration_line();

                    for (int i = horizontal_magnitude.GetLength(0) - 2; i >= 0; i--)
                    {
                        int max_connectedness = min_connectedness;
                        int best_x = -1;
                        int best_y = -1;
                        int idx = -1;

                        for (int k = 0; k < horizontal_positions[i].Count; k++)
                        {
                            int x2 = (int)horizontal_positions[i][k];
                            int dx = x2 - x;
                            if (Math.Abs(dx) < max_horizontal_difference)
                            {
                                int y2 = height - ((height - 1) * i / horizontal_magnitude.GetLength(0) + (height / (horizontal_magnitude.GetLength(0) * 2)));
                                if (ROI != null) y2 = ROI.by - (i * (ROI.by - ROI.ty) / horizontal_magnitude.GetLength(0)) + ((ROI.by - ROI.ty) / (horizontal_magnitude.GetLength(0) * 2));

                                // are these two connected?
                                if ((y < height) && (y2 < height))
                                {
                                    int grid_width = (int)(top_spacing + (vertical_additive * (bottom_spacing - top_spacing) * y2 / height)); 
                                    if (ROI != null)
                                        grid_width = (int)(top_spacing + (vertical_additive * (bottom_spacing - top_spacing) * (y2 - ROI.ty) / (ROI.by - ROI.ty)));

                                    if (!(((x > width * 55 / 100) && (x > x2) && (x2 < x + grid_width)) ||
                                        ((x < width * 45 / 100) && (x < x2) && (x2 > x - grid_width))))
                                    {
                                        int connectedness = pointsConnectedByIntensity(width, height, x, y, x2, y2);
                                        if (connectedness > max_connectedness)
                                        {
                                            best_x = x2;
                                            best_y = y2;
                                            max_connectedness = connectedness;
                                            idx = k;
                                        }
                                    }
                                }
                            }
                        }

                        if (best_x > -1)
                        {
                            calibration_line temp_line = traceLine(width, height, best_x, best_y, x, y);
                            temp_line.Reverse();
                            line.Add(temp_line);
                            x = best_x;
                            y = best_y;
                            drawn = true;
                        }
                    }

                    if (drawn)
                    {
                        calibration_line temp_line = traceLine(width, height, x, min_y, x, y);
                        temp_line.Reverse();
                        line.Add(temp_line);
                        line.Reverse();
                        if (line.points.Count > 0)
                            vertical_lines.Add(line);
                    }
                }
            }

            // sort and prune
            detectLinesSort(width, height, false, vertical_lines);

            // remove first and last lines , because these are usually poorly detected                                  
            if (vertical_lines.Count > 2)
            {
                vertical_lines.RemoveAt(vertical_lines.Count - 1);
                vertical_lines.RemoveAt(0);
            }            

            updateAllLines(width, height, false, vertical_lines);

            // add index numbers to the lines
            indexLines(width, height, false, all_vertical_lines);

            // draw
            for (int i = 0; i < all_vertical_lines.Count; i++)
            {
                calibration_line line = (calibration_line)all_vertical_lines[i];
                line.Draw(lines_image, width, height, 255, 0, 0);
            }
        }

        #endregion

        #region "lens distortion calculation"

        /// <summary>
        /// add a data point to the given curve fitter
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="rectified_x"></param>
        /// <param name="rectified_y"></param>
        /// <param name="polycurve"></param>
        private void addDataPoint(float x, float y, float rectified_x, float rectified_y,
                                  polynomial polycurve)
        {
            float dx = rectified_x - centre_of_distortion.x;
            float dy = rectified_y - centre_of_distortion.y;
            float radial_dist_rectified = (float)Math.Sqrt((dx * dx) + (dy * dy));
            dx = x - centre_of_distortion.x;
            dy = y - centre_of_distortion.y;
            float radial_dist_original = (float)Math.Sqrt((dx * dx) + (dy * dy));
            polycurve.AddPoint(radial_dist_rectified, radial_dist_original);
        }

        private void detectLensDistortion(int width, int height,
                                          int grid_x, int grid_y)
        {
            if (grid != null)
            {
                if (grid[grid_x, grid_y] != null)
                {
                    // field of vision in radians
                    float FOV_horizontal = camera_FOV_degrees * (float)Math.PI / 180.0f;
                    float FOV_vertical = FOV_horizontal *height / (float)width;

                    // center point of the grid within the image
                    pattern_centre_x = (int)grid[grid_x, grid_y].x;
                    pattern_centre_y = (int)grid[grid_x, grid_y].y;

                    // calculate the distance to the centre grid point on the ground plane
                    float ground_dist_to_point = camera_dist_to_pattern_centre_mm;

                    // line of sight distance between the camera lens and the centre point
                    float camera_to_point_dist = (float)Math.Sqrt((ground_dist_to_point * ground_dist_to_point) +
                                                 (camera_height_mm * camera_height_mm));
                    distance_to_pattern_centre = camera_to_point_dist;

                    // tilt angle at the centre point
                    float centre_tilt = (float)Math.Asin(camera_height_mm / camera_to_point_dist);


                    // angle subtended by one grid spacing at the centre
                    float point_pan = (float)Math.Asin(calibration_pattern_spacing_mm / camera_to_point_dist);

                    // grid width at the centre point
                    float x1 = pattern_centre_x + (point_pan * width / FOV_horizontal);

                    // calculate the distance to the observed grid point on the ground plane
                    ground_dist_to_point = camera_dist_to_pattern_centre_mm + ((grid_y + 2) * calibration_pattern_spacing_mm);

                    // line of sight distance between the camera lens and the observed point
                    camera_to_point_dist = (float)Math.Sqrt((ground_dist_to_point * ground_dist_to_point) +
                                                 (camera_height_mm * camera_height_mm));

                    // tilt angle
                    float point_tilt = (float)Math.Asin(camera_height_mm / camera_to_point_dist);

                    // angle subtended by one grid spacing
                    point_pan = (float)Math.Asin(calibration_pattern_spacing_mm / camera_to_point_dist);

                    // calc the position of the grid point within the image after rectification
                    float x2 = pattern_centre_x + (point_pan * width / FOV_horizontal);
                    float y2 = pattern_centre_y + ((point_tilt - centre_tilt) * height / FOV_vertical);

                    // calc the gradient
                    float grad = (x2 - x1) / (float)(y2 - pattern_centre_y);

                    float baseline_fraction = baseline_offset / (float)(calibration_pattern_spacing_mm);

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
                                float ground_dist_to_point_x = ((x - grid_x) * calibration_pattern_spacing_mm) + baseline_offset;

                                // pan angle
                                point_pan = (float)Math.Asin(ground_dist_to_point_x / camera_to_point_dist);

                                // calc the position of the grid point within the image after rectification
                                float w = ((x1 - pattern_centre_x) + ((grid[x, y].y - pattern_centre_y) * grad));
                                float wbaseline = baseline_fraction * (grid[x, y].y - pattern_centre_y) * grad;
                                float rectified_x = pattern_centre_x + (w * (x - grid_x)) - wbaseline;
                                float rectified_y = pattern_centre_y + ((point_tilt - centre_tilt) * height / FOV_vertical);

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
                                polynomial curvefit = new polynomial();
                                curvefit.SetDegree(2);

                                for (int x = 0; x < grid.GetLength(0); x++)
                                {
                                    for (int y = 0; y < grid.GetLength(1); y++)
                                    {
                                        if (grid[x, y] != null)
                                        {
                                            addDataPoint(grid[x, y].x, grid[x, y].y,
                                                         grid[x, y].rectified_x, grid[x, y].rectified_y,
                                                         curvefit);

                                            // intermediary points
                                            if (y > 0)
                                            {
                                                if (grid[x, y - 1] != null)
                                                {
                                                    float xx = grid[x, y - 1].x + ((grid[x, y].x - grid[x, y - 1].x) / 2);
                                                    float yy = grid[x, y - 1].y + ((grid[x, y].y - grid[x, y - 1].y) / 2);
                                                    float rectified_xx = grid[x, y - 1].rectified_x + ((grid[x, y].rectified_x - grid[x, y - 1].rectified_x) / 2);
                                                    float rectified_yy = grid[x, y - 1].rectified_y + ((grid[x, y].rectified_y - grid[x, y - 1].rectified_y) / 2);

                                                    addDataPoint(xx, yy,
                                                                 rectified_xx, rectified_yy,
                                                                 curvefit);
                                                }
                                            }
                                            if (x > 0)
                                            {
                                                if (grid[x-1, y] != null)
                                                {
                                                    float xx = grid[x-1, y].x + ((grid[x, y].x - grid[x-1, y].x) / 2);
                                                    float yy = grid[x-1, y].y + ((grid[x, y].y - grid[x-1, y].y) / 2);
                                                    float rectified_xx = grid[x-1, y].rectified_x + ((grid[x, y].rectified_x - grid[x-1, y].rectified_x) / 2);
                                                    float rectified_yy = grid[x-1, y].rectified_y + ((grid[x, y].rectified_y - grid[x-1, y].rectified_y) / 2);

                                                    addDataPoint(xx, yy,
                                                                 rectified_xx, rectified_yy,
                                                                 curvefit);
                                                }
                                            }
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
        }

        /// <summary>
        /// returns the separation factor, which is the radius used for non-maximal
        /// suppression when detecting edges
        /// </summary>
        /// <param name="width"></param>
        /// <returns></returns>
        private float GetSeparationFactor(int width)
        {
             // field of vision in radians
             float FOV_horizontal = camera_FOV_degrees * (float)Math.PI / 180.0f;

             // calculate the distance to the centre grid point on the ground plane
             float ground_dist_to_point = camera_dist_to_pattern_centre_mm;

             // line of sight distance between the camera lens and the centre point
             float camera_to_point_dist = (float)Math.Sqrt((ground_dist_to_point * ground_dist_to_point) +
                                             (camera_height_mm * camera_height_mm));

             // pan angle at the centre
             float point_pan = (float)Math.Asin(calibration_pattern_spacing_mm / camera_to_point_dist);

             // width of a grid cell
             float x1 = (point_pan * width / FOV_horizontal);

             float factor = x1 / (float)width;
             return (factor/2.4f);
        }


        private void detectScale(int width, int height)
        {
            if (fitter != null)
            {
                const int fraction = 35;
                int x;
                int prev_x_start = 0;
                int x_start = -1;
                int y = height / 2;

                temp_scale = 1;
                isValidRectification = true;

                for (int i = 0; i < 2; i++)
                {
                    x_start = -1;
                    x = 0;
                    if (i > 0) y += (height * fraction / 100);
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

                if (isValidRectification)
                {
                    int prev_y_start = 0;
                    int y_start = -1;
                    x = width / 2;
                    y = height-1;
                    for (int i = 0; i < 2; i++)
                    {
                        y_start = -1;
                        y = height-1;
                        if (i > 0) x += (width * fraction / 100);
                        while ((y >height-(height / 4)) && (y_start < 0))
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
                                    y_start = (int)Math.Round(centre_of_distortion.y + (dy * ratio));
                                }
                            }
                            y--;
                        }
                        if (y_start > -1)
                        {
                            y++;
                            if (i == 0)
                            {
                                prev_y_start = y;
                            }
                            else
                            {
                                if (y < prev_y_start)
                                    isValidRectification = false;
                            }
                        }
                    }

                }

            }
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

        public void rotatePoint(float x, float y, float ang,
                                ref float rotated_x, ref float rotated_y)
        {
            float hyp;
            rotated_x = x;
            rotated_y = y;

            if (ang != 0)
            {
                hyp = (float)Math.Sqrt((x * x) + (y * y));
                if (hyp > 0)
                {
                    float rot_angle = (float)Math.Acos(y / hyp);
                    if (x < 0) rot_angle = (float)(Math.PI * 2) - rot_angle;
                    float new_angle = ang + rot_angle;
                    rotated_x = hyp * (float)Math.Sin(new_angle);
                    rotated_y = hyp * (float)Math.Cos(new_angle);
                }
            }
        }

        /// <summary>
        /// returns a rectified version of the given image location
        /// </summary>
        /// <param name="original_x"></param>
        /// <param name="original_y"></param>
        /// <param name="rectified_x"></param>
        /// <param name="rectified_y"></param>
        /// <returns></returns>
        public bool rectifyPoint(int original_x, int original_y,
                                 ref int rectified_x, ref int rectified_y)
        {
            bool isValid = true;
            if (temp_calibration_map_inverse != null)
            {
                rectified_x = temp_calibration_map_inverse[original_x, original_y, 0];
                rectified_y = temp_calibration_map_inverse[original_x, original_y, 1];
                if ((rectified_x == 0) && (rectified_y == 0)) isValid = false;
            }
            else isValid = false;
            return (isValid);
        }

        /// <summary>
        /// update the calibration lookup table, which maps pixels
        /// in the rectified image into the original image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void updateCalibrationMap(int width, int height, polynomial curve, float scale, float rotation)
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
                            float x2 = (float)Math.Round(centre_of_distortion.x + (dx * ratio));
                            x2 = (x2 - (width / 2)) * scale;
                            float y2 = (float)Math.Round(centre_of_distortion.y + (dy * ratio));
                            y2 = (y2 - (height / 2)) * scale;

                            // apply rotation
                            float x3=x2, y3=y2;
                            rotatePoint(x2, y2, -rotation, ref x3, ref y3);

                            x3 += (width / 2);
                            y3 += (height / 2);

                            if (((int)x3 > -1) && ((int)x3 < width) && ((int)y3 > -1) && ((int)y3 < height))
                            {
                                int n = (y * width) + x;
                                int n2 = ((int)y3 * width) + (int)x3;

                                temp_calibration_map[n] = n2;
                                temp_calibration_map_inverse[(int)x3, (int)y3, 0] = x;
                                temp_calibration_map_inverse[(int)x3, (int)y3, 1] = y;
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

        private void showAlignmentLines(Byte[] img, int width, int height)
        {
            int tx1 = width / 2;
            int ty1 = 0;
            int bx1 = width / 2;
            int by1 = height - 1;

            int tx2 = 0;
            int ty2 = height / 2;
            int bx2 = width - 1;
            int by2 = height / 2;

            drawing.drawLine(img, width, height, tx1, ty1, bx1, by1, 255, 255, 255, 0, false);
            drawing.drawLine(img, width, height, tx2, ty2, bx2, by2, 255, 255, 255, 0, false);
        }

        /// <summary>
        /// update the calibration map after loading calibration data
        /// </summary>
        public void updateCalibrationMap()
        {
            if ((fitter != null) && (scale > 0) && (image_height > 0) && (centre_of_distortion != null))
            {
                updateCalibrationMap(image_width, image_height, fitter, scale, rotation);

                calibration_map = new int[image_width * image_height];
                for (int i = 0; i < temp_calibration_map.Length; i++)
                    calibration_map[i] = temp_calibration_map[i];
                calibration_map_inverse = new int[image_width, image_height, 2];
                for (int x = 0; x < image_width; x++)
                {
                    for (int y = 0; y < image_height; y++)
                    {
                        calibration_map_inverse[x, y, 0] = temp_calibration_map_inverse[x, y, 0];
                        calibration_map_inverse[x, y, 1] = temp_calibration_map_inverse[x, y, 1];
                    }
                }
            }
        }

        public void Update(Byte[] img, int width, int height, bool alignMode)
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
                horizontal_magnitude = new int[8, width];
                vertical_magnitude = new int[5, height];
                if (binary_image == null) binary_image = new bool[no_of_images, width, height];
                for (int i = 0; i < img.Length; i++)
                {
                    lines_image[i] = img[i];
                    centrealign_image[i] = img[i];
                    edges_image[i] = img[i];
                    corners_image[i] = img[i];
                }
                
                // show region of interest
                if (ROI == null)
                {
                    // create a default region of interest
                    ROI = new calibration_region_of_interest();
                    ROI.tx = width / 10;
                    ROI.bx = width - ROI.tx;
                    ROI.ty = height / 10;
                    ROI.by = height - ROI.ty;
                }

                // show the region of interest
                ROI.Show(centrealign_image, width, height);

                // determine the separation factor used for non-maximal suppression
                // during edge detection
                //separation_factor = (int)(1.0f / GetSeparationFactor(width));
                separation_factor = GetSeparationFactor(width);

                // image used for aligning the centre of the calibration pattern
                showAlignmentLines(centrealign_image, width, height);

                // create a mono image
                calibration_image = image.monoImage(img, width, height);

                if (!alignMode)
                {
                    if (samples < min_samples) samples++;

                    int min_magnitude = 1;
                    clearBinaryImage(width, height);
                    detectHorizontalEdges(calibration_image, width, height, edges_horizontal, min_magnitude);
                    detectVerticalEdges(calibration_image, width, height, edges_vertical, min_magnitude);
                    updateEdgesImage(width, height);

                    // detect lines
                    detectHorizontalLines(width, height);
                    detectVerticalLines(width, height);

                    float rotn = detectRotation(width, height);
                    if (rotation == 0)
                        rotation = rotn;
                    else
                        rotation = (rotation * 0.9f) + (rotn * 0.1f);

                    // create a grid to store the edges
                    if ((vertical_lines.Count > 0) && (horizontal_lines.Count > 0))
                    {
                        // locate corners
                        detectCorners(width, height);

                        // hunt the centre spot
                        grid_centre_x = 0;
                        grid_centre_y = 0;
                        detectCentreSpot(calibration_image, width, height, ref grid_centre_x, ref grid_centre_y);
                        if (grid_centre_x > 0)
                        {
                            // detect the lens distortion
                            detectLensDistortion(width, height, grid_centre_x, grid_centre_y);

                            if (fitter != null)
                            {
                                // store the polynomial coefficients which will be used as
                                // a basis for a more detailed search
                                float[] C = new float[3];
                                C[1] = fitter.Coeff(1);
                                C[2] = fitter.Coeff(2);

                                // itterate a number of time trying different possible matches
                                // the number of itterations is adjusted depending upon the size of the error
                                int max_v = 1;
                                if (min_RMS_error > 5) max_v = 5;
                                //if (min_RMS_error > 10) max_v = 10;
                                for (int v = 0; v < max_v; v++)
                                {
                                    // add small amount of noise to the polynomial coefficients
                                    for (int c = 1; c <= 2; c++)
                                        fitter.SetCoeff(c, C[c] * (1.0f + ((((rnd.Next(2000000) / 1000000.0f) - 1.0f) * 0.01f))));

                                    // does this equation cause the image to be re-scaled?
                                    // if it does we can explicitly detect this and correct for it later
                                    detectScale(width, height);

                                    // the rectification is only considered valid if it is roughly concave
                                    if (isValidRectification)
                                    {
                                        // update the calibration lookup
                                        updateCalibrationMap(width, height, fitter, 1.0f, rotation);

                                        float RMS_error = GetRMSerror();
                                        if ((RMS_error < min_RMS_error) && (samples >= min_samples))
                                        {
                                            // update the graph
                                            curve_fit = new Byte[width * height * 3];
                                            fitter.Show(curve_fit, width, height);

                                            updateCalibrationMap(width, height, fitter, temp_scale, rotation);
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

                                            // update the rectified position of the centre of the pattern
                                            updatePatternCentreRectified();

                                            scale = temp_scale;
                                            best_curve = fitter;
                                            min_RMS_error = RMS_error;
                                        }
                                    }
                                }

                                // rectify
                                Rectify(img, width, height);

                                ShowRectifiedCorners(rectified_image, width, height);
                            }
                        }

                        corners_index++;
                        if (corners_index >= corners.Length) corners_index = 0;
                    }

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
                else
                {
                    samples = 0;
                }

                binary_image_index++;
                if (binary_image_index >= no_of_images) binary_image_index = 0;

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

            xml.AddComment(doc, nodeCalibration, "Camera calibration data");

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
            if (File.Exists(filename))
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
        }

        /// <summary>
        /// return an xml element containing camera calibration parameters
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            String coefficients = "";
            if (fitter != null)
            {                
                int degree = fitter.GetDegree();
                for (int i = 0; i <= degree; i++)
                {
                    coefficients += Convert.ToString(fitter.Coeff(i), format);
                    if (i < degree) coefficients += ",";
                }
            }
            else coefficients = "0,0,0";

            XmlElement elem = doc.CreateElement("Camera");
            doc.DocumentElement.AppendChild(elem);
            xml.AddComment(doc, elem, "Horizontal field of view of the camera in degrees");
            xml.AddTextElement(doc, elem, "FieldOfViewDegrees", Convert.ToString(camera_FOV_degrees));
            xml.AddComment(doc, elem, "Image dimensions in pixels");
            xml.AddTextElement(doc, elem, "ImageDimensions", Convert.ToString(image_width, format) + "," + Convert.ToString(image_height, format));
            if (centre_of_distortion != null)
            {
                xml.AddComment(doc, elem, "The centre of distortion in pixels");
                xml.AddTextElement(doc, elem, "CentreOfDistortion", Convert.ToString(centre_of_distortion.x, format) + "," + Convert.ToString(centre_of_distortion.y, format));
            }
            xml.AddComment(doc, elem, "Polynomial coefficients used to describe the camera lens distortion");
            xml.AddTextElement(doc, elem, "DistortionCoefficients", coefficients);
            xml.AddComment(doc, elem, "Scaling factor");
            xml.AddTextElement(doc, elem, "Scale", Convert.ToString(scale));
            xml.AddComment(doc, elem, "Rotation of the image in degrees");
            xml.AddTextElement(doc, elem, "RotationDegrees", Convert.ToString(rotation / (float)Math.PI * 180.0f));
            xml.AddComment(doc, elem, "The minimum RMS error between the distortion curve and plotted points");
            xml.AddTextElement(doc, elem, "RMSerror", Convert.ToString(min_RMS_error));
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
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String[] dimStr = xnod.InnerText.Split(',');
                image_width = Convert.ToInt32(dimStr[0], format);
                image_height = Convert.ToInt32(dimStr[1], format);
            }

            if (xnod.Name == "CentreOfDistortion")
            {
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String[] centreStr = xnod.InnerText.Split(',');
                centre_of_distortion = new calibration_point(
                    Convert.ToSingle(centreStr[0], format),
                    Convert.ToSingle(centreStr[1], format));
            }

            if (xnod.Name == "DistortionCoefficients")
            {
                if (xnod.InnerText != "")
                {
                    IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                    String[] coeffStr = xnod.InnerText.Split(',');
                    fitter = new polynomial();
                    fitter.SetDegree(coeffStr.Length - 1);
                    for (int i = 0; i < coeffStr.Length; i++)
                        fitter.SetCoeff(i, Convert.ToSingle(coeffStr[i], format));
                }
            }

            if (xnod.Name == "Scale")
            {
                scale = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "RotationDegrees")
            {
                rotation = Convert.ToSingle(xnod.InnerText) / 180.0f * (float)Math.PI;
            }

            if (xnod.Name == "RMSerror")
            {
                min_RMS_error = Convert.ToSingle(xnod.InnerText);

                // update the calibration lookup table
                updateCalibrationMap();
            }

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
