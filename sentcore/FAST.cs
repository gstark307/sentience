/*
    FAST corner detector
    C# version adapted from original C code by Edward Rosten 
    http://mi.eng.cam.ac.uk/~er258/work/fast.html
    Copyright (C) 2006 Bob Mottram
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
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
        /// <summary>
        /// stores information about a corner feature
        /// </summary>
        public class FASTcorner
        {
            public const int constellation_size = 5;

            public int x, y;
            public int score;
            public int[,] constellation;
            public float disparity = 0;

            public FASTcorner(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public int matching_score(FASTcorner other)
            {
                int score = -1;
                for (int x = 0; x < constellation_size * 2; x++)
                {
                    for (int y = 0; y < constellation_size * 2; y++)
                    {
                        if ((constellation[x, y] > 0) || (other.constellation[x, y] > 0))
                        {
                            if (score == -1) score = 0;
                            score += Math.Abs(constellation[x, y] - other.constellation[x, y]);
                        }
                    }
                }
                return (score);
            }

            /// <summary>
            /// builds a constellation for this corner
            /// </summary>
            /// <param name="other_corner">array of corner features</param>
            /// <param name="max_x_diff">max search radius for x axis</param>
            /// <param name="max_y_diff">max search radius for y axis</param>
            public void update(FASTcorner[] other_corner, int max_x_diff, int max_y_diff)
            {
                constellation = new int[(constellation_size * 2) + 1, (constellation_size * 2) + 1];

                for (int i = 0; i < other_corner.Length; i++)
                {
                    FASTcorner other = other_corner[i];
                    if ((other != this) && (other != null))
                    {
                        int dx = other.x - x;
                        if ((dx > -max_x_diff) && (dx < max_x_diff))
                        {
                            int dy = other.y - y;
                            if ((dy > -max_y_diff) && (dy < max_y_diff))
                            {
                                int cx = constellation_size + (dx * constellation_size / max_x_diff);
                                int cy = constellation_size + (dy * constellation_size / max_y_diff);
                                constellation[cx, cy]++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// stores information about a line feature consisting of two corners
        /// </summary>
        public class FASTline
        {
            public bool Visible = false;
            public bool onHorizon = false;
            public FASTcorner point1;
            public FASTcorner point2;

            /// <summary>
            /// returns true if the points given are on a line
            /// </summary>
            /// <param name="img"></param>
            /// <param name="img_width"></param>
            /// <param name="img_height"></param>
            /// <param name="p1"></param>
            /// <param name="p2"></param>
            /// <returns></returns>
            public static bool isLine(Byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                      FASTcorner p1, FASTcorner p2, int line_threshold)
            {
                bool line = true;
                int diff_x = 0, diff_y = 0, sample_number = 0;
                const int max_samples = 50;
                const int radius = 10;

                int dx = p2.x - p1.x;
                int dy = p2.y - p1.y;
                while ((sample_number < max_samples) && (line))
                {
                    line = false;

                    int x = p1.x + (dx * sample_number / max_samples);
                    int y = p1.y + (dy * sample_number / max_samples);

                    if ((x > radius) && (x < img_width - radius) && (y > radius) && (y < img_height - radius))
                    {
                        int n = ((y * img_width) + x) * bytes_per_pixel;
                        int r = radius * bytes_per_pixel;

                        diff_x = (img[n - r] + img[n - r + bytes_per_pixel]) -
                                     (img[n + r] + img[n + r - bytes_per_pixel]);
                        if (diff_x < 0) diff_x = -diff_x;

                        if (diff_x < line_threshold)
                        {
                            r = radius * img_width * bytes_per_pixel;

                            diff_y = (img[n - r] + img[n - r + bytes_per_pixel]) -
                                     (img[n + r] + img[n + r - bytes_per_pixel]);
                            if (diff_y < 0) diff_y = -diff_y;
                        }

                        if ((diff_x > line_threshold) || (diff_y > line_threshold))
                            line = true;
                    }
                    else line = true;
                    sample_number++;
                }

                return (line);
            }


            public bool isOnHorizon(Byte[] colour_img, int img_width, int img_height, int horizon_threshold)
            {
                const int bytes_per_pixel = 3;
                bool horizon = false;
                int sample_number = 0;
                const int max_samples = 10;
                int radius = img_height / 10;

                int dx = point2.x - point1.x;
                int dy = point2.y - point1.y;

                if ((point1.y > img_height / 10) && (point2.y > img_height / 10))
                {
                    if ((point1.y < img_height - (img_height / 8)) && (point2.y < img_height - (img_height / 8)))
                    {
                        if (Math.Abs(dx) > img_width / 10)
                        {
                            horizon = true;
                            while ((sample_number < max_samples) && (horizon))
                            {
                                horizon = false;

                                int x = point1.x + (dx * sample_number / max_samples);
                                int y = point1.y + (dy * sample_number / max_samples);

                                if ((x > radius) && (x < img_width - radius) && (y > radius) && (y < img_height - radius))
                                {
                                    int interval = (y - 10) / 15;
                                    int tot = 0;
                                    int hits = 0;
                                    for (int yy = y - 10; yy > 0; yy -= interval)
                                    {
                                        int n = ((yy * img_width) + x) * bytes_per_pixel;
                                        tot += colour_img[n];
                                        hits++;
                                    }
                                    if (hits > 0)
                                    {
                                        tot /= hits;
                                        if (tot > 230)
                                        {
                                            horizon = true;
                                        }
                                    }

                                }
                                else horizon = true;
                                sample_number++;
                            }
                        }
                    }
                }

                return (horizon);
            }


            public FASTline(FASTcorner point1, FASTcorner point2)
            {
                this.point1 = point1;
                this.point2 = point2;
            }
        }


        /// <summary>
        /// A C# implementation of the FAST corner detector
        /// </summary>
    public class FAST
    {
        public static float horizon_detection(Byte[] colour_image, int img_width, int img_height,
                                              FASTline[] lines, int horizon_threshold,
                                              ref int vertical_position)
        {
            float gradient = 0;
            float gravity_angle = 0;
            int hits = 0;
            int av_y = 0;

            vertical_position = 0;
            if (lines != null)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    FASTline line = lines[i];
                    if (line.Visible)
                    {
                        if (line.isOnHorizon(colour_image, img_width, img_height, horizon_threshold))
                        {
                            line.onHorizon = true;
                            float dx = line.point2.x - line.point1.x;
                            float dy = line.point2.y - line.point1.y;
                            float grad = 0;
                            if (dy != 0) grad = dx / dy;
                            int y = (int)((grad * (img_width / 2)) + line.point1.y);
                            if ((y > 0) && (y < img_height))
                            {
                                av_y += y;
                                hits++;
                                gradient += grad;
                            }
                        }
                    }
                }
            }
            if (hits > 0)
            {
                av_y /= hits;
                gradient /= (float)hits;
                vertical_position = av_y;
                gravity_angle = gradient;
            }

            return (gravity_angle);
        }

        public static float gravity_direction(FASTline[] lines)
        {
            float gravity_angle = 0;

            if (lines != null)
            {
                int[] orientation_histogram = new int[30];

                for (int i = 0; i < lines.Length; i++)
                {
                    FASTline line = lines[i];
                    if (line.Visible)
                    {
                        // get the orientation of the line
                        int dx = line.point2.x - line.point1.x;
                        int dy = line.point2.y - line.point1.y;
                        int length = (int)Math.Sqrt((dx * dx) + (dy * dy));
                        if (length > 0)
                        {
                            float angle = (float)Math.Asin(dx / (float)length);
                            if (dy < 0) angle = (float)(Math.PI * 2) - angle;
                            if (angle < 0) angle = (float)(Math.PI) + angle;
                            int bin_number = (int)(angle / (float)(Math.PI * 2) * (orientation_histogram.Length - 1));
                            orientation_histogram[bin_number] += 1;
                        }
                    }
                }

                int max = 0;
                for (int i = 0; i < orientation_histogram.Length / 4; i++)
                {
                    int curr_i = i;
                    int prev_i = curr_i - 1;
                    if (prev_i < 0) prev_i = orientation_histogram.Length + prev_i;
                    int next_i = curr_i + 1;
                    if (next_i >= orientation_histogram.Length) next_i -= orientation_histogram.Length;
                    int value = orientation_histogram[curr_i] + ((orientation_histogram[prev_i] + orientation_histogram[next_i]) / 4);

                    curr_i = i + (orientation_histogram.Length / 4);
                    prev_i = curr_i - 1;
                    if (prev_i < 0) prev_i = orientation_histogram.Length + prev_i;
                    next_i = curr_i + 1;
                    if (next_i >= orientation_histogram.Length) next_i -= orientation_histogram.Length;
                    value += orientation_histogram[curr_i] + ((orientation_histogram[prev_i] + orientation_histogram[next_i]) / 4);

                    curr_i = i + (orientation_histogram.Length / 2);
                    prev_i = curr_i - 1;
                    if (prev_i < 0) prev_i = orientation_histogram.Length + prev_i;
                    next_i = curr_i + 1;
                    if (next_i >= orientation_histogram.Length) next_i -= orientation_histogram.Length;
                    value += orientation_histogram[curr_i] + ((orientation_histogram[prev_i] + orientation_histogram[next_i]) / 4);

                    if (value > max)
                    {
                        max = value;
                        gravity_angle = i * (float)Math.PI * 2 / orientation_histogram.Length;
                    }
                }
            }


            if (gravity_angle > (float)(Math.PI / 4))
                gravity_angle -= (float)(Math.PI / 2);

            return (gravity_angle);
        }

        /// <summary>
        /// get a score for a corner feature
        /// </summary>
        /// <param name="imp"></param>
        /// <param name="pointer_dir"></param>
        /// <param name="barrier"></param>
        /// <returns></returns>
        private static unsafe int corner_score(Byte* imp, int* pointer_dir, int barrier)
        {
            /*The score for a positive feature is sum of the difference between the pixels
              and the barrier if the difference is positive. Negative is similar.
              The score is the max of those two.
	  
               B = {x | x = points on the Bresenham circle around c}
               Sp = { I(x) - t | x E B , I(x) - t > 0 }
               Sn = { t - I(x) | x E B, t - I(x) > 0}
	  
               Score = max sum(Sp), sum(Sn)*/

            int cb = *imp + barrier;
            int c_b = *imp - barrier;
            int sp = 0, sn = 0;

            int i = 0;

            for (i = 0; i < 16; i++)
            {
                int p = imp[pointer_dir[i]];

                if (p > cb)
                    sp += p - cb;
                else if (p < c_b)
                    sn += c_b - p;
            }

            if (sp > sn)
                return sp;
            else
                return sn;
        }

        /// <summary>
        /// perform non-maximal supression
        /// </summary>
        /// <param name="img">image - one byte per pixel</param>
        /// <param name="xsize">width of the image</param>
        /// <param name="ysize">height of the image</param>
        /// <param name="corners">returned corner features</param>
        /// <param name="barrier">detection threshold</param>
        /// <returns></returns>
        public static unsafe FASTcorner[] fast_nonmax(Byte[] img, int xsize, int ysize, FASTcorner[] corners, int barrier, int calibration_offset_x, int calibration_offset_y)
        {
            bool found;

            fixed (Byte* im = img)
            {
                int numcorners = corners.Length;

                // Create a list of integer pointer offstes, corresponding to the 
                // direction offsets in dir[]
                int[] mpointer_dir = new int[16];
                int[] row_start = new int[ysize];
                int[] scores = new int[numcorners];
                FASTcorner[] nonmax_corners = new FASTcorner[numcorners];
                int num_nonmax = 0;
                int prev_row = -1;
                int i, j;
                int point_above = 0;
                int point_below = 0;

                fixed (int* pointer_dir = mpointer_dir)
                {

                    pointer_dir[0] = 0 + 3 * xsize;
                    pointer_dir[1] = 1 + 3 * xsize;
                    pointer_dir[2] = 2 + 2 * xsize;
                    pointer_dir[3] = 3 + 1 * xsize;
                    pointer_dir[4] = 3 + 0 * xsize;
                    pointer_dir[5] = 3 + -1 * xsize;
                    pointer_dir[6] = 2 + -2 * xsize;
                    pointer_dir[7] = 1 + -3 * xsize;
                    pointer_dir[8] = 0 + -3 * xsize;
                    pointer_dir[9] = -1 + -3 * xsize;
                    pointer_dir[10] = -2 + -2 * xsize;
                    pointer_dir[11] = -3 + -1 * xsize;
                    pointer_dir[12] = -3 + 0 * xsize;
                    pointer_dir[13] = -3 + 1 * xsize;
                    pointer_dir[14] = -2 + 2 * xsize;
                    pointer_dir[15] = -1 + 3 * xsize;

                    if (numcorners < 5)
                        return null;

                    // xsize ysize numcorners corners

                    // Compute the score for each detected corner, and find where each row begins
                    // (the corners are output in raster scan order). A beginning of -1 signifies
                    // that there are no corners on that row.

                    for (i = 0; i < ysize; i++)
                        row_start[i] = -1;

                    for (i = 0; i < numcorners; i++)
                    {
                        if (corners[i].y != prev_row)
                        {
                            row_start[corners[i].y] = i;
                            prev_row = corners[i].y;
                        }

                        scores[i] = corner_score(im + corners[i].x + corners[i].y * xsize, pointer_dir, barrier);
                    }


                    // Point above points (roughly) to the pixel above the one of interest, if there
                    // is a feature there.

                    int ctr;
                    for (i = 1; i < numcorners - 1; i++)
                    {
                        int score = scores[i];
                        FASTcorner pos = corners[i];

                        // Check left
                        if (corners[i - 1].x == pos.x - 1 && corners[i - 1].y == pos.y && scores[i - 1] > score)
                            continue;

                        // Check right
                        if (corners[i + 1].x == pos.x + 1 && corners[i + 1].y == pos.y && scores[i - 1] > score)
                            continue;

                        // Check above
                        if (pos.y > 0 && pos.y < ysize - 1 && row_start[pos.y - 1] > -1)
                        {
                            if (corners[point_above].y < pos.y - 1)
                                point_above = row_start[pos.y - 1];

                            // Make point above point to the first of the pixels above the current point,
                            // if it exists.
                            ctr = 0;
                            for (; corners[point_above].y < pos.y && corners[point_above].x < pos.x - 1; point_above++)
                            {
                                ctr++;
                                if (ctr > 100) break;
                            }

                            for (j = point_above; corners[j].y < pos.y && corners[j].x <= pos.x + 1; j++)
                            {
                                int x = corners[j].x;
                                if ((x == pos.x - 1 || x == pos.x || x == pos.x + 1) && scores[j] > score)
                                {
                                    goto cont;
                                }
                            }

                        }

                        // Check below
                        if (pos.y > 0 && pos.y < ysize - 2 && row_start[pos.y + 1] > -1) // Nothing below
                        {
                            if (corners[point_below].y < pos.y + 1)
                                point_below = row_start[pos.y + 1];

                            // Make point below point to one of the pixels below the current point, if it
                            // exists.
                            ctr = 0;
                            if (point_below < corners.Length)
                            {
                                for (; corners[point_below].y == pos.y + 1 && corners[point_below].x < pos.x - 1; point_below++)
                                {
                                    ctr++;
                                    if (ctr > 100) break;
                                    if (point_below >= corners.Length - 1) break;
                                }

                                found = false;
                                j = point_below;
                                while ((!found) && (j < corners.Length))
                                {
                                    if (corners[j].y == pos.y + 1 && corners[j].x <= pos.x + 1) found = true;

                                    int x = corners[j].x;
                                    if ((x == pos.x - 1 || x == pos.x || x == pos.x + 1) && scores[j] > score)
                                    {
                                        goto cont;
                                    }
                                    if (j >= corners.Length) found = true;
                                    j++;
                                }
                            }
                        }

                        int xx = corners[i].x + calibration_offset_x;
                        int yy = corners[i].y + calibration_offset_y;
                        if ((xx > -1) && (xx < xsize) && (yy > -1) && (yy < ysize))
                        {
                            nonmax_corners[num_nonmax] = new FASTcorner(xx, yy);
                            nonmax_corners[num_nonmax].score = score;
                            num_nonmax++;
                        }

                    cont:
                        ;
                    }

                    Array.Resize(ref nonmax_corners, num_nonmax);
                    return nonmax_corners;
                }
            }
        }


        /// <summary>
        /// update corner properties
        /// </summary>
        /// <param name="corners"></param>
        /// <param name="xradius"></param>
        /// <param name="yradius"></param>
        public static void fast_update(FASTcorner[] corners, int xradius, int yradius)
        {
            if (corners != null)
                for (int i = 0; i < corners.Length; i++)
                    if (corners[i] != null)
                        corners[i].update(corners, xradius, yradius);
        }

        public static FASTline[] fast_lines(Byte[] img, int xsize, int ysize,
                                            FASTcorner[] corners, int line_threshold, int min_length)
        {
            int min_length_y = min_length * ysize / xsize;
            int no_of_lines = 0;
            FASTline[] lines = new FASTline[corners.Length * corners.Length];

            for (int i = 0; i < corners.Length - 1; i++)
            {
                FASTcorner corner1 = corners[i];
                for (int j = i + 1; j < corners.Length; j++)
                {
                    FASTcorner corner2 = corners[j];

                    int dx = corner2.x - corner1.x;
                    if (dx < 0) dx = -dx;
                    int dy = corner2.y - corner1.y;
                    if (dy < 0) dy = -dy;
                    if ((dx > min_length) || (dy > min_length_y))
                    {
                        if (FASTline.isLine(img, xsize, ysize, 1, corner1, corner2, line_threshold))
                        {
                            lines[no_of_lines] = new FASTline(corner1, corner2);
                            no_of_lines++;
                        }
                    }
                }
            }
            if (no_of_lines > 0)
                Array.Resize(ref lines, no_of_lines);
            else
                lines = null;

            return (lines);
        }

        public static unsafe FASTcorner[] fast_corner_detect_10(Byte[] img, int xsize, int ysize, int barrier)
        {
            int boundary = 3, y, cb, c_b;
            Byte* line_max;
            Byte* line_min;
            int rsize = 512, total = 0;
            FASTcorner[] ret = new FASTcorner[rsize];
            Byte* cache_0;
            Byte* cache_1;
            Byte* cache_2;
            int[] mpixel = new int[16];

            fixed (int* pixel = mpixel)
            {
                fixed (Byte* im = img)
                {
                    pixel[0] = 0 + 3 * xsize;
                    pixel[1] = 1 + 3 * xsize;
                    pixel[2] = 2 + 2 * xsize;
                    pixel[3] = 3 + 1 * xsize;
                    pixel[4] = 3 + 0 * xsize;
                    pixel[5] = 3 + -1 * xsize;
                    pixel[6] = 2 + -2 * xsize;
                    pixel[7] = 1 + -3 * xsize;
                    pixel[8] = 0 + -3 * xsize;
                    pixel[9] = -1 + -3 * xsize;
                    pixel[10] = -2 + -2 * xsize;
                    pixel[11] = -3 + -1 * xsize;
                    pixel[12] = -3 + 0 * xsize;
                    pixel[13] = -3 + 1 * xsize;
                    pixel[14] = -2 + 2 * xsize;
                    pixel[15] = -1 + 3 * xsize;
                    for (y = boundary; y < ysize - boundary; y++)
                    {
                        cache_0 = im + boundary + y * xsize;
                        line_min = cache_0 - boundary;
                        line_max = im + xsize - boundary + y * xsize;

                        cache_1 = cache_0 + pixel[9];
                        cache_2 = cache_0 + pixel[3];

                        int ctr = 0;
                        for (; cache_0 < line_max; cache_0++, cache_1++, cache_2++)
                        {
                            ctr++;
                            if (ctr > 1000) break;

                            cb = *cache_0 + barrier;
                            c_b = *cache_0 - barrier;
                            if (*cache_1 > cb)
                                if (*(cache_0 + pixel[2]) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[0]) > cb)
                                                if (*cache_2 > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_1 + 2) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    goto success;
                                                                else if (*(cache_1 + 1) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[14]) > cb)
                                                                        if (*(cache_0 + pixel[15]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else if (*(cache_1 + 2) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_0 + pixel[15]) > cb)
                                                                        if (*(cache_0 + pixel[1]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[5]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + -3) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_0 + pixel[14]) > cb)
                                                                        if (*(cache_0 + pixel[15]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*cache_2 < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + -3) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_1 + 2) > cb)
                                                                        goto success;
                                                                    else if (*(cache_1 + 2) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[15]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[0]) < c_b)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_1 + 2) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_1 + 2) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    if (*cache_2 > cb)
                                                                        goto success;
                                                                    else if (*cache_2 < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_2 + -6) > cb)
                                                                            if (*(cache_0 + -3) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[11]) < c_b)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    if (*cache_2 > cb)
                                                                        if (*(cache_1 + 1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[15]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            goto success;
                                                                        else if (*(cache_0 + pixel[10]) < c_b)
                                                                            continue;
                                                                        else
                                                                            if (*cache_2 > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[11]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    if (*cache_2 > cb)
                                                                        if (*(cache_2 + -6) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*(cache_2 + -6) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + -3) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[14]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                                            continue;
                                                                        else
                                                                            goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[0]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[11]) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_1 + 2) > cb)
                                                                            if (*(cache_1 + 1) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else if (*(cache_0 + pixel[0]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[6]) > cb)
                                                                        if (*(cache_1 + 2) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[15]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_1 + 2) > cb)
                                                                    if (*(cache_2 + -6) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            if (*(cache_1 + 1) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[2]) < c_b)
                                    if (*(cache_0 + pixel[14]) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_1 + 2) > cb)
                                                        if (*(cache_0 + -3) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*(cache_0 + -3) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*(cache_0 + -3) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_1 + 2) > cb)
                                                                    if (*(cache_2 + -6) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[1]) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_0 + -3) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_1 + 2) > cb)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        if (*(cache_0 + -3) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[0]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_1 + 1) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    if (*(cache_1 + 2) > cb)
                                                                        if (*(cache_2 + -6) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else if (*(cache_1 + 2) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[1]) > cb)
                                                                            if (*(cache_2 + -6) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[14]) < c_b)
                                        if (*(cache_0 + pixel[5]) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*cache_2 > cb)
                                                    if (*(cache_0 + -3) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*cache_2 < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_0 + 3) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*cache_2 < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_2 + -6) > cb)
                                                if (*(cache_1 + 2) < c_b)
                                                    if (*(cache_0 + 3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_2 + -6) < c_b)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*cache_2 < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + 3) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + 3) < c_b)
                                                            if (*(cache_0 + pixel[15]) < c_b)
                                                                if (*cache_2 < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_1 + 2) < c_b)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_2 + -6) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*(cache_1 + 2) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_0 + -3) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + -3) > cb)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_1 + 1) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_1 + 2) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else if (*(cache_2 + -6) < c_b)
                                                                continue;
                                                            else
                                                                if (*cache_2 > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[6]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else if (*(cache_0 + pixel[0]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_1 + 2) > cb)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        if (*(cache_2 + -6) > cb)
                                                                            goto success;
                                                                        else if (*(cache_2 + -6) < c_b)
                                                                            continue;
                                                                        else
                                                                            if (*cache_2 > cb)
                                                                                if (*(cache_0 + 3) > cb)
                                                                                    goto success;
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[14]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*cache_2 > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            if (*(cache_1 + 2) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*cache_2 < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_0 + pixel[6]) > cb)
                                                                        if (*(cache_0 + pixel[5]) > cb)
                                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_1 + 2) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_0 + pixel[11]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*cache_2 < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb || *(cache_0 + pixel[0]) < c_b)
                                                            continue;
                                                        else
                                                            goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_1 + 2) > cb)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        if (*(cache_2 + -6) > cb)
                                                                            goto success;
                                                                        else if (*(cache_2 + -6) < c_b)
                                                                            continue;
                                                                        else
                                                                            if (*cache_2 > cb)
                                                                                if (*(cache_0 + 3) > cb)
                                                                                    goto success;
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + 3) > cb)
                                                            if (*cache_2 > cb)
                                                                goto success;
                                                            else if (*cache_2 < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_2 + -6) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + 3) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_2 + -6) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*cache_2 > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            if (*(cache_1 + 1) > cb)
                                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                                    goto success;
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else if (*cache_1 < c_b)
                                if (*(cache_0 + pixel[1]) > cb)
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_2 + -6) > cb)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_2 + -6) < c_b)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_1 + 2) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + 3) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_1 + 2) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + 3) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[6]) < c_b)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*cache_2 > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_0 + pixel[15]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_0 + -3) > cb)
                                                            continue;
                                                        else if (*(cache_0 + -3) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*cache_2 < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_2 + -6) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*cache_2 > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_0 + pixel[15]) < c_b)
                                                    if (*(cache_1 + 2) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_1 + 2) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[15]) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_1 + 2) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + -3) > cb)
                                                    continue;
                                                else if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_2 + -6) > cb)
                                                        continue;
                                                    else if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_1 + 2) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*cache_2 < c_b)
                                                            if (*(cache_1 + 2) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[11]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[5]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[5]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_1 + 2) > cb || *(cache_1 + 2) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[1]) < c_b)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_1 + 1) > cb)
                                                        goto success;
                                                    else if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[15]) < c_b)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        continue;
                                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_0 + pixel[15]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[2]) > cb || *(cache_0 + pixel[2]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_1 + 2) > cb || *(cache_1 + 2) < c_b)
                                                        continue;
                                                    else
                                                        goto success;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*cache_2 > cb)
                                                continue;
                                            else if (*cache_2 < c_b)
                                                if (*(cache_0 + pixel[5]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[5]) < c_b)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_1 + 2) > cb)
                                                                continue;
                                                            else if (*(cache_1 + 2) < c_b)
                                                                if (*(cache_1 + 1) > cb)
                                                                    continue;
                                                                else if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (*(cache_2 + -6) < c_b)
                                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + -3) < c_b)
                                                                if (*(cache_0 + pixel[11]) < c_b)
                                                                    if (*(cache_1 + 2) < c_b)
                                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_1 + 2) < c_b)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    if (*(cache_0 + -3) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[11]) < c_b)
                                                                if (*(cache_0 + pixel[14]) < c_b)
                                                                    if (*(cache_2 + -6) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_2 + -6) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        continue;
                                                                    else if (*(cache_0 + pixel[5]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*(cache_2 + -6) < c_b)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[11]) < c_b)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            continue;
                                                                        else if (*(cache_0 + pixel[10]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            if (*cache_2 < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_0 + pixel[11]) < c_b)
                                                    if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_1 + 1) > cb)
                                                                continue;
                                                            else if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[15]) < c_b)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        continue;
                                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        if (*(cache_1 + 2) < c_b)
                                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[11]) < c_b)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*cache_2 > cb)
                                                continue;
                                            else if (*cache_2 < c_b)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + 3) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + 3) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_1 + 2) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_2 + -6) < c_b)
                                                    if (*(cache_0 + 3) > cb)
                                                        continue;
                                                    else if (*(cache_0 + 3) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[2]) > cb || *(cache_0 + pixel[2]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_1 + 2) < c_b)
                                                if (*(cache_0 + -3) > cb)
                                                    continue;
                                                else if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_2 + -6) > cb)
                                                                continue;
                                                            else if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*cache_2 < c_b)
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        if (*(cache_0 + 3) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_2 + -6) < c_b)
                                                                        if (*(cache_1 + 1) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + 3) < c_b)
                                                            if (*(cache_2 + -6) > cb)
                                                                continue;
                                                            else if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*cache_2 < c_b)
                                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*cache_2 < c_b)
                                                        if (*(cache_0 + 3) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[5]) < c_b)
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_1 + 2) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                if (*cache_2 > cb)
                                                    if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*cache_2 < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_1 + 2) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + -3) < c_b)
                                                                if (*(cache_1 + 2) < c_b)
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        if (*(cache_1 + 1) < c_b)
                                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + -3) < c_b)
                                                                if (*(cache_1 + 2) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                                            if (*(cache_1 + 1) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            if (*(cache_1 + 2) < c_b)
                                                                if (*(cache_2 + -6) < c_b)
                                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                    else
                                        continue;
                            else
                                if (*cache_2 > cb)
                                    if (*(cache_0 + pixel[15]) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_2 + -6) > cb)
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[14]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_0 + pixel[14]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_1 + 1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_0 + -3) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_2 + -6) < c_b)
                                                    if (*(cache_1 + 1) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_1 + 1) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_1 + 2) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_1 + 2) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        if (*(cache_0 + pixel[1]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_1 + 1) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[0]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                if (*(cache_0 + pixel[14]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        if (*(cache_0 + pixel[2]) > cb)
                                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + -3) > cb)
                                                        if (*(cache_1 + 1) > cb || *(cache_1 + 1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        continue;
                                                                    else if (*(cache_0 + pixel[5]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        if (*(cache_0 + pixel[14]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[11]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        if (*(cache_0 + pixel[0]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else if (*(cache_0 + pixel[11]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[5]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + 3) > cb)
                                                            if (*(cache_1 + 2) > cb || *(cache_1 + 2) < c_b)
                                                                continue;
                                                            else
                                                                goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_2 + -6) > cb)
                                                                        if (*(cache_0 + pixel[2]) > cb)
                                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        if (*(cache_2 + -6) > cb)
                                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                                    goto success;
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*cache_2 < c_b)
                                    if (*(cache_0 + pixel[15]) < c_b)
                                        if (*(cache_0 + pixel[5]) > cb)
                                            if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_1 + 2) > cb)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_1 + 2) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_1 + 2) > cb)
                                                if (*(cache_2 + -6) < c_b)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[6]) < c_b)
                                                        goto success;
                                                    else
                                                        if (*(cache_0 + -3) < c_b)
                                                            if (*(cache_0 + 3) > cb)
                                                                continue;
                                                            else if (*(cache_0 + 3) < c_b)
                                                                if (*(cache_0 + pixel[14]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (*(cache_1 + 2) < c_b)
                                                if (*(cache_0 + 3) > cb)
                                                    continue;
                                                else if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + -3) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_2 + -6) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_0 + pixel[14]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_2 + -6) < c_b)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_0 + 3) > cb)
                                                            continue;
                                                        else if (*(cache_0 + 3) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[14]) < c_b)
                                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + -3) < c_b)
                                                                if (*(cache_0 + pixel[11]) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                                            if (*(cache_1 + 1) > cb || *(cache_1 + 1) < c_b)
                                                                                continue;
                                                                            else
                                                                                goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + -3) < c_b)
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    if (*(cache_0 + 3) > cb)
                                                                        continue;
                                                                    else if (*(cache_0 + 3) < c_b)
                                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                                            if (*(cache_0 + pixel[11]) < c_b)
                                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                                    goto success;
                                                                                else
                                                                                    continue;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                if (*(cache_2 + -6) < c_b)
                                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + 3) < c_b)
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_0 + pixel[1]) < c_b)
                                                                    if (*(cache_2 + -6) < c_b)
                                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                        success:
                            if (total >= rsize)
                            {
                                rsize *= 2;
                                Array.Resize(ref ret, rsize);
                            }

                            int xx = (int)(cache_0 - line_min);
                            int yy = y;
                            ret[total] = new FASTcorner(xx, yy);
                            total++;
                        }
                    }
                    // resize the array so that we don't need to explicityly pass back the total
                    // number of corners found
                    Array.Resize(ref ret, total);
                    return ret;
                }
            }
        }
    }
   
}
