/*
    FAST corner detector
    C# version adapted from original C code by Edward Rosten 
    http://mi.eng.cam.ac.uk/~er258/work/fast.html
    Copyright (C) 2006-2007 Bob Mottram
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

// use .NET runtime version 1.1, or comment out for later versions
#define DOTNET_V1_1

using System;
using System.Collections;
using System.Text;
using sluggish.utilities;

namespace sluggish.imageprocessing.FASTcorners
{
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
        /// <param name="imp">mono bitmap image - one byte per pixel</param>
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

        #if DOTNET_V1_1
        /// <summary>
        /// resizes the given array
        /// This is a replacement for Array.Resize which exists in .NET version 2.0 or later
        /// </summary>        
        private static Array ArrayResize(Array array, int newSize)
        {
	        Type type = array.GetType();
	        Array newArray = Array.CreateInstance(type.GetElementType(), newSize);
	        Array.Copy(array, 0, newArray, 0, Math.Min(array.Length, newSize));
	        return(newArray);
        }        
        #endif
        
        /// <summary>
        /// perform non-maximal supression
        /// </summary>
        /// <param name="img">image - one byte per pixel</param>
        /// <param name="xsize">width of the image</param>
        /// <param name="ysize">height of the image</param>
        /// <param name="corners">returned corner features</param>
        /// <param name="barrier">detection threshold</param>
        /// <returns></returns>
        public static unsafe FASTcorner[] fast_nonmax(Byte[] img, int xsize, int ysize, FASTcorner[] corners, int barrier, float calibration_offset_x, float calibration_offset_y)
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

                        int xx = (int)(corners[i].x + calibration_offset_x);
                        int yy = (int)(corners[i].y + calibration_offset_y);
                        if ((xx > -1) && (xx < xsize) && (yy > -1) && (yy < ysize))
                        {
                            nonmax_corners[num_nonmax] = new FASTcorner(xx, yy);
                            nonmax_corners[num_nonmax].score = score;
                            num_nonmax++;
                        }

                    cont:
                        ;
                    }

                    #if DOTNET_V1_1
                        nonmax_corners = (FASTcorner[])ArrayResize(nonmax_corners, num_nonmax);
                    #else
                        Array.Resize(ref nonmax_corners, num_nonmax);
                    #endif
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

        /// <summary>
        /// Detect lines in the given mono image using the given corner features
        /// </summary>
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
                #if DOTNET_V1_1
                    lines = (FASTline[])ArrayResize(lines, no_of_lines);
                #else
                    Array.Resize(ref lines, no_of_lines);
                #endif
            else
                lines = null;

            return (lines);
        }


        /// <summary>
        /// detect FAST corners
        //  It is assumed that the image supplied is mono (one byte per pixel)
        /// Note that non-maximal supression should be carried out after running this function
        /// </summary>
        /// <param name="img">image - one byte per pixel</param>
        /// <param name="xsize"></param>
        /// <param name="ysize"></param>
        /// <param name="barrier">detection threshold</param>
        /// <returns>array of corner features</returns>
        public static unsafe FASTcorner[] fast_corner_detect_9(Byte[] img, int xsize, int ysize, int barrier)
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
				        cache_0 = im + boundary + y*xsize;														
				        line_min = cache_0 - boundary;															
				        line_max = im + xsize - boundary + y * xsize;											
																										
				        cache_1 = cache_0 + pixel[5];
				        cache_2 = cache_0 + pixel[14];
																										
						for(; cache_0 < line_max;cache_0++, cache_1++, cache_2++)
						{																						
							cb = *cache_0 + barrier;															
							c_b = *cache_0 - barrier;															
				            if(*cache_1 > cb)
				                if(*cache_2 > cb)
				                    if(*(cache_0+3) > cb)
				                        if(*(cache_0 + pixel[0]) > cb)
				                            if(*(cache_0 + pixel[3]) > cb)
				                                if(*(cache_0 + pixel[6]) > cb)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[15]) > cb)
				                                            if(*(cache_0 + pixel[1]) > cb)
				                                                goto success;
				                                            else if(*(cache_0 + pixel[1]) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_0 + pixel[10]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        if(*(cache_0 + pixel[7]) > cb)
				                                                            if(*(cache_0 + pixel[9]) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else if(*(cache_0 + pixel[15]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[8]) > cb)
				                                                if(*(cache_0 + pixel[7]) > cb)
				                                                    if(*(cache_0 + pixel[1]) > cb)
				                                                        goto success;
				                                                    else if(*(cache_0 + pixel[1]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[10]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else if(*(cache_2+4) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_1+-6) > cb)
				                                            if(*(cache_0 + pixel[9]) > cb)
				                                                if(*(cache_0 + pixel[10]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        if(*(cache_0 + pixel[7]) > cb)
				                                                            goto success;
				                                                        else if(*(cache_0 + pixel[7]) < c_b)
				                                                            continue;
				                                                        else
				                                                            if(*(cache_0+-3) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                    else if(*(cache_0 + pixel[8]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0+-3) > cb)
				                                                            if(*(cache_0 + pixel[1]) > cb)
				                                                                if(*(cache_0 + pixel[13]) > cb)
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
				                                else if(*(cache_0 + pixel[6]) < c_b)
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*(cache_1+-6) > cb)
				                                                continue;
				                                            else if(*(cache_1+-6) < c_b)
				                                                if(*(cache_0 + pixel[15]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[15]) > cb)
				                                            if(*(cache_2+4) > cb)
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    goto success;
				                                                else if(*(cache_0 + pixel[1]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        if(*(cache_1+-6) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                            else if(*(cache_2+4) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    if(*(cache_0+-3) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            if(*(cache_0 + pixel[10]) > cb)
				                                                                if(*(cache_1+-6) > cb)
				                                                                    goto success;
				                                                                else
				                                                                    continue;
				                                                            else
				                                                                continue;
				                                                        else if(*(cache_0 + pixel[1]) < c_b)
				                                                            continue;
				                                                        else
				                                                            if(*(cache_0 + pixel[8]) > cb)
				                                                                if(*(cache_0 + pixel[10]) > cb)
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
				                            else if(*(cache_0 + pixel[3]) < c_b)
				                                continue;
				                            else
				                                if(*(cache_0+-3) > cb)
				                                    if(*(cache_0 + pixel[10]) > cb)
				                                        if(*(cache_1+-6) > cb)
				                                            if(*(cache_0 + pixel[8]) > cb)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    goto success;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_2+4) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else if(*(cache_0 + pixel[8]) < c_b)
				                                                if(*(cache_0 + pixel[7]) > cb || *(cache_0 + pixel[7]) < c_b)
				                                                    continue;
				                                                else
				                                                    goto success;
				                                            else
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[13]) > cb)
				                                                        if(*(cache_0 + pixel[15]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else if(*(cache_2+4) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[9]) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            if(*(cache_0 + pixel[13]) > cb)
				                                                                if(*(cache_0 + pixel[15]) > cb)
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
				                        else if(*(cache_0 + pixel[0]) < c_b)
				                            if(*(cache_0 + pixel[7]) > cb)
				                                if(*(cache_0 + pixel[10]) > cb)
				                                    goto success;
				                                else
				                                    continue;
				                            else
				                                continue;
				                        else
				                            if(*(cache_0 + pixel[7]) > cb)
				                                if(*(cache_0 + pixel[10]) > cb)
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_0 + pixel[6]) > cb)
				                                            if(*(cache_0 + pixel[8]) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[9]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else if(*(cache_2+4) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_1+-6) > cb)
				                                                        if(*(cache_0 + pixel[9]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[6]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[15]) > cb)
				                                                if(*(cache_0+-3) > cb)
				                                                    if(*(cache_0 + pixel[9]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else if(*(cache_0 + pixel[3]) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_0+-3) > cb)
				                                            if(*(cache_0 + pixel[8]) > cb)
				                                                if(*(cache_1+-6) > cb)
				                                                    if(*(cache_0 + pixel[6]) > cb)
				                                                        if(*(cache_0 + pixel[9]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else if(*(cache_0 + pixel[6]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[15]) > cb)
				                                                            if(*(cache_0 + pixel[13]) > cb)
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
				                                else if(*(cache_0 + pixel[10]) < c_b)
				                                    continue;
				                                else
				                                    if(*(cache_0 + pixel[1]) > cb)
				                                        if(*(cache_0 + pixel[9]) > cb)
				                                            if(*(cache_0 + pixel[6]) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[3]) > cb)
				                                                        if(*(cache_0 + pixel[8]) > cb)
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
				                    else if(*(cache_0+3) < c_b)
				                        if(*(cache_0+-3) > cb)
				                            if(*(cache_0 + pixel[9]) > cb)
				                                if(*(cache_1+-6) > cb)
				                                    if(*(cache_0 + pixel[10]) > cb)
				                                        if(*(cache_0 + pixel[6]) > cb)
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
				                        if(*(cache_0+-3) > cb)
				                            if(*(cache_1+-6) > cb)
				                                if(*(cache_0 + pixel[7]) > cb)
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[9]) > cb)
				                                                if(*(cache_0 + pixel[6]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        goto success;
				                                                    else if(*(cache_0 + pixel[8]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[0]) > cb)
				                                                            if(*(cache_0 + pixel[1]) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                        else
				                                                            continue;
				                                                else if(*(cache_0 + pixel[6]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        if(*(cache_0 + pixel[8]) > cb)
				                                                            goto success;
				                                                        else if(*(cache_0 + pixel[8]) < c_b)
				                                                            continue;
				                                                        else
				                                                            if(*(cache_0 + pixel[0]) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                    else
				                                                        continue;
				                                            else if(*(cache_0 + pixel[9]) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[7]) < c_b)
				                                    if(*(cache_0 + pixel[10]) > cb)
				                                        if(*(cache_2+4) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_0 + pixel[0]) > cb)
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
				                                    if(*(cache_0 + pixel[0]) > cb)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else if(*(cache_2+4) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[9]) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            if(*(cache_0 + pixel[15]) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                        else if(*(cache_0 + pixel[1]) < c_b)
				                                                            continue;
				                                                        else
				                                                            if(*(cache_0 + pixel[8]) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                    else
				                                                        continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        if(*(cache_0 + pixel[13]) > cb)
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
				                else if(*cache_2 < c_b)
				                    if(*(cache_0+3) > cb)
				                        if(*(cache_0 + pixel[7]) > cb)
				                            if(*(cache_0 + pixel[1]) > cb)
				                                if(*(cache_0 + pixel[9]) > cb)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[6]) > cb)
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                if(*(cache_0 + pixel[8]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_2+4) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_1+-6) > cb)
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else if(*(cache_0 + pixel[9]) < c_b)
				                                    if(*(cache_0 + pixel[15]) > cb)
				                                        if(*(cache_2+4) > cb)
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    if(*(cache_0 + pixel[0]) > cb)
				                                        if(*(cache_0 + pixel[8]) > cb)
				                                            if(*(cache_2+4) > cb)
				                                                if(*(cache_0 + pixel[3]) > cb)
				                                                    if(*(cache_0 + pixel[6]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[8]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[15]) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                            else if(*(cache_0 + pixel[1]) < c_b)
				                                if(*(cache_1+-6) > cb)
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[6]) > cb)
				                                                if(*(cache_0 + pixel[8]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_0 + pixel[3]) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_0+-3) > cb)
				                                            if(*(cache_0 + pixel[10]) > cb)
				                                                if(*(cache_0 + pixel[6]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else if(*(cache_1+-6) < c_b)
				                                    if(*(cache_0 + pixel[9]) > cb)
				                                        if(*(cache_0 + pixel[3]) > cb)
				                                            if(*(cache_2+4) > cb)
				                                                if(*(cache_0 + pixel[10]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else if(*(cache_2+4) < c_b)
				                                                if(*(cache_0 + pixel[10]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[3]) < c_b)
				                                            if(*(cache_0 + pixel[15]) < c_b)
				                                                if(*(cache_0+-3) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_2+4) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0 + pixel[0]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_2+4) < c_b)
				                                            if(*(cache_0 + pixel[10]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[15]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                if(*(cache_0 + pixel[3]) < c_b)
				                                                    if(*(cache_0 + pixel[15]) < c_b)
				                                                        if(*(cache_0 + pixel[0]) < c_b)
				                                                            if(*(cache_0+-3) < c_b)
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
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[8]) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                            else
				                                if(*(cache_0 + pixel[10]) > cb)
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_2+4) > cb)
				                                            if(*(cache_0 + pixel[6]) > cb)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_2+4) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_1+-6) > cb)
				                                                if(*(cache_0 + pixel[6]) > cb)
				                                                    if(*(cache_0 + pixel[9]) > cb)
				                                                        if(*(cache_0 + pixel[8]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else if(*(cache_0 + pixel[3]) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_0+-3) > cb)
				                                            if(*(cache_1+-6) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else
				                                    continue;
				                        else if(*(cache_0 + pixel[7]) < c_b)
				                            if(*(cache_1+-6) < c_b)
				                                if(*(cache_0 + pixel[15]) > cb)
				                                    continue;
				                                else if(*(cache_0 + pixel[15]) < c_b)
				                                    if(*(cache_0+-3) < c_b)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0 + pixel[13]) < c_b)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        continue;
				                                                    else if(*(cache_0 + pixel[8]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        if(*(cache_0 + pixel[1]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                else
				                                                    if(*(cache_2+4) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0 + pixel[3]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                                else
				                                    if(*(cache_0 + pixel[6]) < c_b)
				                                        if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0+-3) < c_b)
				                                                if(*(cache_0 + pixel[8]) < c_b)
				                                                    if(*(cache_0 + pixel[13]) < c_b)
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
				                            if(*(cache_0 + pixel[0]) < c_b)
				                                if(*(cache_0 + pixel[10]) > cb)
				                                    continue;
				                                else if(*(cache_0 + pixel[10]) < c_b)
				                                    if(*(cache_0 + pixel[9]) > cb)
				                                        continue;
				                                    else if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0+-3) < c_b)
				                                            if(*(cache_0 + pixel[1]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[1]) < c_b)
				                                                if(*(cache_1+-6) < c_b)
				                                                    if(*(cache_0 + pixel[13]) < c_b)
				                                                        if(*(cache_0 + pixel[15]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                if(*(cache_0 + pixel[8]) < c_b)
				                                                    if(*(cache_0 + pixel[13]) < c_b)
				                                                        if(*(cache_1+-6) < c_b)
				                                                            if(*(cache_0 + pixel[15]) < c_b)
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
				                                        if(*(cache_2+4) < c_b)
				                                            if(*(cache_0+-3) < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    if(*(cache_1+-6) < c_b)
				                                                        if(*(cache_0 + pixel[13]) < c_b)
				                                                            if(*(cache_0 + pixel[15]) < c_b)
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
				                                    if(*(cache_0 + pixel[3]) < c_b)
				                                        if(*(cache_1+-6) < c_b)
				                                            if(*(cache_0+-3) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                            else
				                                continue;
				                    else if(*(cache_0+3) < c_b)
				                        if(*(cache_0+-3) > cb)
				                            if(*(cache_0 + pixel[13]) > cb)
				                                goto success;
				                            else
				                                continue;
				                        else if(*(cache_0+-3) < c_b)
				                            if(*(cache_0 + pixel[9]) > cb)
				                                if(*(cache_0 + pixel[13]) < c_b)
				                                    goto success;
				                                else
				                                    continue;
				                            else if(*(cache_0 + pixel[9]) < c_b)
				                                goto success;
				                            else
				                                if(*(cache_0 + pixel[6]) > cb || *(cache_0 + pixel[6]) < c_b)
				                                    continue;
				                                else
				                                    if(*(cache_2+4) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                        else
				                            continue;
				                    else
				                        if(*(cache_1+-6) > cb)
				                            if(*(cache_0 + pixel[13]) > cb)
				                                if(*(cache_0 + pixel[9]) > cb)
				                                    if(*(cache_0 + pixel[7]) > cb)
				                                        if(*(cache_0+-3) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else
				                                continue;
				                        else if(*(cache_1+-6) < c_b)
				                            if(*(cache_0 + pixel[3]) > cb)
				                                if(*(cache_0 + pixel[8]) < c_b)
				                                    if(*(cache_0 + pixel[15]) > cb)
				                                        continue;
				                                    else if(*(cache_0 + pixel[15]) < c_b)
				                                        if(*(cache_0 + pixel[13]) < c_b)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[0]) < c_b)
				                                                goto success;
				                                            else
				                                                if(*(cache_0 + pixel[7]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_0 + pixel[6]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                else
				                                    continue;
				                            else if(*(cache_0 + pixel[3]) < c_b)
				                                if(*(cache_2+4) > cb)
				                                    continue;
				                                else if(*(cache_2+4) < c_b)
				                                    if(*(cache_0 + pixel[0]) < c_b)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[1]) < c_b)
				                                            if(*(cache_0 + pixel[15]) < c_b)
				                                                if(*(cache_0+-3) < c_b)
				                                                    if(*(cache_0 + pixel[13]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0 + pixel[8]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                                else
				                                    if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[1]) < c_b)
				                                            if(*(cache_0+-3) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0 + pixel[8]) < c_b)
				                                                if(*(cache_0 + pixel[0]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                            else
				                                if(*(cache_0 + pixel[1]) > cb)
				                                    continue;
				                                else if(*(cache_0 + pixel[1]) < c_b)
				                                    if(*(cache_0 + pixel[10]) < c_b)
				                                        if(*(cache_0+-3) < c_b)
				                                            if(*(cache_0 + pixel[9]) > cb)
				                                                if(*(cache_2+4) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else if(*(cache_0 + pixel[9]) < c_b)
				                                                if(*(cache_0 + pixel[15]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[15]) < c_b)
				                                                    if(*(cache_0 + pixel[13]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    if(*(cache_0 + pixel[6]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else
				                                                if(*(cache_2+4) < c_b)
				                                                    if(*(cache_0 + pixel[15]) < c_b)
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
				                                    if(*(cache_0 + pixel[7]) > cb)
				                                        continue;
				                                    else if(*(cache_0 + pixel[7]) < c_b)
				                                        if(*(cache_0 + pixel[15]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[15]) < c_b)
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0+-3) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
				                                                        if(*(cache_0 + pixel[13]) < c_b)
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
				                                            if(*(cache_0 + pixel[6]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                    else
				                                        if(*(cache_0 + pixel[0]) < c_b)
				                                            if(*(cache_0 + pixel[8]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                        else
				                            continue;
				                else
				                    if(*(cache_0 + pixel[7]) > cb)
				                        if(*(cache_0 + pixel[3]) > cb)
				                            if(*(cache_0 + pixel[10]) > cb)
				                                if(*(cache_0+3) > cb)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[6]) > cb)
				                                            if(*(cache_0 + pixel[8]) > cb)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    goto success;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else if(*(cache_0 + pixel[8]) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_0 + pixel[15]) > cb)
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_2+4) < c_b)
				                                        if(*(cache_1+-6) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_1+-6) > cb)
				                                            if(*(cache_0 + pixel[6]) > cb)
				                                                if(*(cache_0 + pixel[8]) > cb)
				                                                    if(*(cache_0 + pixel[9]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else if(*(cache_0+3) < c_b)
				                                    continue;
				                                else
				                                    if(*(cache_0+-3) > cb)
				                                        if(*(cache_0 + pixel[13]) > cb)
				                                            if(*(cache_1+-6) > cb)
				                                                if(*(cache_0 + pixel[6]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        if(*(cache_0 + pixel[9]) > cb)
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
				                            else if(*(cache_0 + pixel[10]) < c_b)
				                                if(*(cache_0 + pixel[15]) > cb)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[6]) > cb)
				                                            if(*(cache_0+3) > cb)
				                                                if(*(cache_0 + pixel[0]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[15]) < c_b)
				                                    continue;
				                                else
				                                    if(*(cache_0 + pixel[8]) > cb)
				                                        if(*(cache_0 + pixel[0]) > cb)
				                                            goto success;
				                                        else if(*(cache_0 + pixel[0]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[9]) > cb)
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    if(*(cache_0 + pixel[6]) > cb)
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
				                                if(*(cache_0 + pixel[1]) > cb)
				                                    if(*(cache_0 + pixel[9]) > cb)
				                                        if(*(cache_0 + pixel[6]) > cb)
				                                            if(*(cache_0+3) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        goto success;
				                                                    else if(*(cache_0 + pixel[8]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[15]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0 + pixel[0]) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_0 + pixel[0]) > cb)
				                                            if(*(cache_0+3) > cb)
				                                                if(*(cache_0 + pixel[6]) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        if(*(cache_2+4) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else if(*(cache_0 + pixel[15]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[8]) > cb)
				                                                            if(*(cache_2+4) > cb)
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
				                        else if(*(cache_0 + pixel[3]) < c_b)
				                            if(*(cache_0 + pixel[13]) > cb)
				                                if(*(cache_1+-6) > cb)
				                                    if(*(cache_0 + pixel[9]) > cb)
				                                        if(*(cache_0+-3) > cb)
				                                            if(*(cache_0 + pixel[6]) > cb)
				                                                if(*(cache_0 + pixel[8]) > cb)
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
				                            else if(*(cache_0 + pixel[13]) < c_b)
				                                continue;
				                            else
				                                if(*(cache_0+3) > cb)
				                                    if(*(cache_0+-3) > cb)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                        else
				                            if(*(cache_0+-3) > cb)
				                                if(*(cache_0 + pixel[13]) > cb)
				                                    if(*(cache_1+-6) > cb)
				                                        if(*(cache_0 + pixel[9]) > cb)
				                                            if(*(cache_0 + pixel[6]) > cb)
				                                                if(*(cache_0 + pixel[10]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
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
				                                else if(*(cache_0 + pixel[13]) < c_b)
				                                    if(*(cache_0 + pixel[0]) > cb)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    if(*(cache_0+3) > cb)
				                                        if(*(cache_0 + pixel[9]) > cb)
				                                            if(*(cache_1+-6) > cb)
				                                                if(*(cache_0 + pixel[6]) > cb)
				                                                    if(*(cache_0 + pixel[10]) > cb)
				                                                        if(*(cache_0 + pixel[8]) > cb)
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
				            else if(*cache_1 < c_b)
				                if(*(cache_0 + pixel[15]) > cb)
				                    if(*(cache_1+-6) > cb)
				                        if(*(cache_2+4) > cb)
				                            if(*(cache_0+-3) > cb)
				                                if(*(cache_0 + pixel[10]) > cb)
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*cache_2 > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[1]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[7]) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[10]) < c_b)
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_0 + pixel[13]) > cb)
				                                            if(*cache_2 > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*cache_2 > cb)
				                                                if(*(cache_0 + pixel[0]) > cb)
				                                                    if(*(cache_0 + pixel[13]) > cb)
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
				                        else if(*(cache_2+4) < c_b)
				                            if(*(cache_0 + pixel[7]) > cb)
				                                if(*(cache_0+-3) > cb)
				                                    if(*cache_2 > cb)
				                                        if(*(cache_0 + pixel[13]) > cb)
				                                            if(*(cache_0 + pixel[9]) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else if(*(cache_0 + pixel[7]) < c_b)
				                                if(*(cache_0 + pixel[9]) > cb)
				                                    if(*(cache_0 + pixel[1]) > cb)
				                                        if(*(cache_0+-3) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[9]) < c_b)
				                                    if(*(cache_0 + pixel[10]) > cb)
				                                        continue;
				                                    else if(*(cache_0 + pixel[10]) < c_b)
				                                        if(*(cache_0 + pixel[3]) < c_b)
				                                            if(*(cache_0+3) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_0 + pixel[1]) < c_b)
				                                            if(*(cache_0 + pixel[3]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else
				                                    if(*(cache_0 + pixel[0]) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                            else
				                                if(*(cache_0 + pixel[0]) > cb)
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[9]) > cb)
				                                            if(*cache_2 > cb)
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    if(*(cache_0 + pixel[10]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else if(*(cache_0 + pixel[1]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        if(*(cache_0+-3) > cb)
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
				                            if(*(cache_0 + pixel[9]) > cb)
				                                if(*(cache_0+-3) > cb)
				                                    if(*(cache_0 + pixel[1]) > cb)
				                                        if(*cache_2 > cb)
				                                            if(*(cache_0 + pixel[10]) > cb)
				                                                if(*(cache_0 + pixel[13]) > cb)
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_0 + pixel[1]) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_0 + pixel[7]) > cb)
				                                            if(*(cache_0 + pixel[10]) > cb)
				                                                if(*(cache_0 + pixel[13]) > cb)
				                                                    if(*cache_2 > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[7]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                if(*(cache_0 + pixel[8]) > cb)
				                                                    if(*(cache_0 + pixel[6]) < c_b)
				                                                        if(*(cache_0 + pixel[10]) > cb)
				                                                            if(*(cache_0 + pixel[13]) > cb)
				                                                                goto success;
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
				                                continue;
				                    else if(*(cache_1+-6) < c_b)
				                        if(*(cache_0 + pixel[3]) > cb)
				                            if(*(cache_0 + pixel[13]) > cb)
				                                if(*(cache_0+-3) > cb)
				                                    if(*(cache_0+3) > cb)
				                                        goto success;
				                                    else
				                                        continue;
				                                else if(*(cache_0+-3) < c_b)
				                                    if(*(cache_0+3) < c_b)
				                                        if(*(cache_0 + pixel[6]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else if(*(cache_0 + pixel[13]) < c_b)
				                                if(*(cache_0 + pixel[7]) < c_b)
				                                    if(*(cache_0 + pixel[6]) < c_b)
				                                        if(*(cache_0 + pixel[8]) < c_b)
				                                            if(*(cache_0+-3) < c_b)
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
				                                if(*(cache_0+3) < c_b)
				                                    if(*(cache_0+-3) < c_b)
				                                        if(*(cache_0 + pixel[7]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                        else if(*(cache_0 + pixel[3]) < c_b)
				                            if(*(cache_0 + pixel[8]) < c_b)
				                                if(*(cache_0 + pixel[9]) < c_b)
				                                    if(*(cache_0 + pixel[7]) < c_b)
				                                        if(*(cache_0+3) > cb)
				                                            continue;
				                                        else if(*(cache_0+3) < c_b)
				                                            if(*(cache_0 + pixel[10]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[6]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                        else
				                                            if(*(cache_0 + pixel[13]) < c_b)
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
				                            if(*(cache_0+-3) < c_b)
				                                if(*(cache_0+3) > cb)
				                                    continue;
				                                else if(*(cache_0+3) < c_b)
				                                    if(*(cache_0 + pixel[6]) < c_b)
				                                        if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0 + pixel[9]) < c_b)
				                                                if(*(cache_0 + pixel[7]) < c_b)
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
				                                    if(*(cache_0 + pixel[13]) < c_b)
				                                        if(*(cache_0 + pixel[7]) < c_b)
				                                            if(*(cache_0 + pixel[6]) < c_b)
				                                                if(*(cache_0 + pixel[10]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
				                                                        if(*(cache_0 + pixel[9]) < c_b)
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
				                        if(*(cache_2+4) > cb)
				                            if(*(cache_0+3) > cb)
				                                if(*(cache_0+-3) > cb)
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*(cache_0 + pixel[3]) > cb)
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
				                        else if(*(cache_2+4) < c_b)
				                            if(*(cache_0 + pixel[10]) > cb)
				                                continue;
				                            else if(*(cache_0 + pixel[10]) < c_b)
				                                if(*(cache_0+3) < c_b)
				                                    if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0 + pixel[3]) < c_b)
				                                            if(*(cache_0 + pixel[7]) < c_b)
				                                                if(*(cache_0 + pixel[6]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
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
				                                if(*(cache_0 + pixel[1]) < c_b)
				                                    if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0 + pixel[3]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                        else
				                            continue;
				                else if(*(cache_0 + pixel[15]) < c_b)
				                    if(*(cache_0+3) > cb)
				                        if(*(cache_0+-3) < c_b)
				                            if(*(cache_1+-6) < c_b)
				                                if(*(cache_0 + pixel[13]) < c_b)
				                                    if(*(cache_0 + pixel[7]) > cb)
				                                        continue;
				                                    else if(*(cache_0 + pixel[7]) < c_b)
				                                        goto success;
				                                    else
				                                        if(*(cache_0 + pixel[8]) < c_b)
				                                            if(*(cache_0 + pixel[0]) < c_b)
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
				                    else if(*(cache_0+3) < c_b)
				                        if(*(cache_0 + pixel[6]) > cb)
				                            if(*(cache_0 + pixel[13]) > cb)
				                                if(*cache_2 > cb)
				                                    if(*(cache_0 + pixel[10]) > cb)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else if(*(cache_0 + pixel[13]) < c_b)
				                                if(*(cache_0 + pixel[0]) < c_b)
				                                    if(*(cache_2+4) < c_b)
				                                        if(*cache_2 < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else
				                                continue;
				                        else if(*(cache_0 + pixel[6]) < c_b)
				                            if(*(cache_0 + pixel[3]) > cb)
				                                if(*(cache_0+-3) < c_b)
				                                    if(*(cache_0 + pixel[1]) < c_b)
				                                        continue;
				                                    else
				                                        goto success;
				                                else
				                                    continue;
				                            else if(*(cache_0 + pixel[3]) < c_b)
				                                if(*(cache_0 + pixel[7]) > cb)
				                                    if(*cache_2 < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[7]) < c_b)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[10]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else if(*(cache_2+4) < c_b)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[1]) < c_b)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[0]) < c_b)
				                                                goto success;
				                                            else
				                                                if(*(cache_0 + pixel[9]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[8]) < c_b)
				                                                    if(*(cache_0 + pixel[9]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else
				                                        if(*(cache_1+-6) < c_b)
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[8]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else
				                                    if(*cache_2 < c_b)
				                                        if(*(cache_2+4) < c_b)
				                                            if(*(cache_0 + pixel[0]) < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
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
				                                if(*(cache_0+-3) < c_b)
				                                    if(*(cache_1+-6) < c_b)
				                                        if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0 + pixel[8]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[8]) < c_b)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    if(*(cache_0 + pixel[7]) > cb)
				                                                        continue;
				                                                    else if(*(cache_0 + pixel[7]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        if(*(cache_0 + pixel[13]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                else
				                                                    if(*(cache_2+4) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else
				                                                if(*(cache_0 + pixel[13]) < c_b)
				                                                    if(*(cache_0 + pixel[0]) < c_b)
				                                                        if(*(cache_0 + pixel[7]) > cb || *(cache_0 + pixel[7]) < c_b)
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
				                                    continue;
				                        else
				                            if(*(cache_0 + pixel[13]) < c_b)
				                                if(*(cache_2+4) > cb)
				                                    continue;
				                                else if(*(cache_2+4) < c_b)
				                                    if(*cache_2 < c_b)
				                                        if(*(cache_0 + pixel[3]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[3]) < c_b)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[0]) < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                if(*(cache_0 + pixel[7]) < c_b)
				                                                    if(*(cache_1+-6) < c_b)
				                                                        if(*(cache_0 + pixel[8]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else
				                                            if(*(cache_0+-3) < c_b)
				                                                if(*(cache_0 + pixel[10]) < c_b)
				                                                    if(*(cache_0 + pixel[1]) > cb)
				                                                        continue;
				                                                    else if(*(cache_0 + pixel[1]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        if(*(cache_0 + pixel[7]) < c_b)
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
				                                    if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_1+-6) < c_b)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[0]) < c_b)
				                                                if(*cache_2 < c_b)
				                                                    if(*(cache_0 + pixel[10]) < c_b)
				                                                        if(*(cache_0+-3) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                if(*(cache_0 + pixel[7]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
				                                                        if(*(cache_0 + pixel[1]) > cb || *(cache_0 + pixel[1]) < c_b)
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
				                                continue;
				                    else
				                        if(*(cache_0+-3) < c_b)
				                            if(*(cache_0 + pixel[13]) < c_b)
				                                if(*(cache_1+-6) < c_b)
				                                    if(*(cache_0 + pixel[9]) > cb)
				                                        if(*(cache_0 + pixel[3]) < c_b)
				                                            if(*(cache_2+4) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_0 + pixel[9]) < c_b)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0 + pixel[7]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[7]) < c_b)
				                                                if(*cache_2 > cb || *cache_2 < c_b)
				                                                    goto success;
				                                                else
				                                                    if(*(cache_0 + pixel[6]) < c_b)
				                                                        if(*(cache_0 + pixel[8]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                            else
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[1]) < c_b)
				                                                    if(*cache_2 < c_b)
				                                                        if(*(cache_0 + pixel[0]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    if(*(cache_0 + pixel[0]) < c_b)
				                                                        if(*(cache_0 + pixel[8]) < c_b)
				                                                            if(*cache_2 < c_b)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                        else
				                                            if(*(cache_0 + pixel[3]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                    else
				                                        if(*(cache_2+4) < c_b)
				                                            if(*(cache_0 + pixel[1]) < c_b)
				                                                if(*(cache_0 + pixel[10]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[10]) < c_b)
				                                                    if(*cache_2 < c_b)
				                                                        if(*(cache_0 + pixel[0]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    if(*(cache_0 + pixel[3]) < c_b)
				                                                        if(*(cache_0 + pixel[0]) < c_b)
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
				                    if(*(cache_0 + pixel[8]) > cb)
				                        if(*(cache_0 + pixel[6]) > cb)
				                            if(*cache_2 > cb)
				                                if(*(cache_1+-6) > cb)
				                                    if(*(cache_0 + pixel[10]) > cb)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else
				                                continue;
				                        else
				                            continue;
				                    else if(*(cache_0 + pixel[8]) < c_b)
				                        if(*(cache_0 + pixel[3]) > cb)
				                            if(*(cache_0 + pixel[13]) > cb)
				                                continue;
				                            else if(*(cache_0 + pixel[13]) < c_b)
				                                if(*(cache_0+-3) < c_b)
				                                    if(*(cache_0 + pixel[7]) < c_b)
				                                        if(*(cache_1+-6) < c_b)
				                                            if(*(cache_0 + pixel[6]) < c_b)
				                                                if(*(cache_0 + pixel[10]) < c_b)
				                                                    if(*(cache_0 + pixel[9]) < c_b)
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
				                                if(*(cache_0+3) < c_b)
				                                    if(*(cache_0+-3) < c_b)
				                                        if(*(cache_0 + pixel[10]) < c_b)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                        else if(*(cache_0 + pixel[3]) < c_b)
				                            if(*(cache_2+4) > cb)
				                                if(*(cache_1+-6) < c_b)
				                                    if(*(cache_0 + pixel[7]) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else if(*(cache_2+4) < c_b)
				                                if(*(cache_0 + pixel[6]) < c_b)
				                                    if(*(cache_0+3) > cb)
				                                        continue;
				                                    else if(*(cache_0+3) < c_b)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                continue;
				                                            else if(*(cache_0 + pixel[0]) < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                if(*(cache_0 + pixel[9]) < c_b)
				                                                    if(*(cache_0 + pixel[1]) < c_b)
				                                                        if(*(cache_0 + pixel[7]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0 + pixel[7]) < c_b)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    goto success;
				                                                else
				                                                    if(*(cache_0 + pixel[0]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0 + pixel[1]) < c_b)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    if(*(cache_0 + pixel[7]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    if(*(cache_0 + pixel[0]) < c_b)
				                                                        if(*(cache_0 + pixel[7]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                            else
				                                                continue;
				                                    else
				                                        if(*(cache_0+-3) < c_b)
				                                            if(*(cache_0 + pixel[13]) < c_b)
				                                                if(*(cache_1+-6) < c_b)
				                                                    if(*(cache_0 + pixel[7]) < c_b)
				                                                        if(*(cache_0 + pixel[10]) < c_b)
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
				                                if(*(cache_1+-6) < c_b)
				                                    if(*(cache_0+3) > cb)
				                                        continue;
				                                    else if(*(cache_0+3) < c_b)
				                                        if(*(cache_0 + pixel[6]) < c_b)
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[7]) < c_b)
				                                                    if(*(cache_0 + pixel[9]) < c_b)
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
				                                        if(*(cache_0+-3) < c_b)
				                                            if(*(cache_0 + pixel[13]) < c_b)
				                                                if(*(cache_0 + pixel[6]) < c_b)
				                                                    if(*(cache_0 + pixel[7]) < c_b)
				                                                        if(*(cache_0 + pixel[10]) < c_b)
				                                                            if(*(cache_0 + pixel[9]) < c_b)
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
				                            if(*(cache_0+-3) < c_b)
				                                if(*(cache_0 + pixel[13]) > cb)
				                                    if(*(cache_0+3) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[13]) < c_b)
				                                    if(*(cache_1+-6) < c_b)
				                                        if(*(cache_0 + pixel[7]) < c_b)
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[6]) < c_b)
				                                                    if(*(cache_0 + pixel[9]) < c_b)
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
				                                    if(*(cache_0+3) < c_b)
				                                        if(*(cache_0 + pixel[10]) < c_b)
				                                            if(*(cache_0 + pixel[6]) < c_b)
				                                                if(*(cache_1+-6) < c_b)
				                                                    if(*(cache_0 + pixel[7]) < c_b)
				                                                        if(*(cache_0 + pixel[9]) < c_b)
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
				                if(*(cache_0+-3) > cb)
				                    if(*cache_2 > cb)
				                        if(*(cache_0 + pixel[7]) > cb)
				                            if(*(cache_1+-6) > cb)
				                                if(*(cache_0 + pixel[6]) > cb)
				                                    if(*(cache_0 + pixel[13]) > cb)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[9]) > cb)
				                                                if(*(cache_0 + pixel[8]) > cb)
				                                                    goto success;
				                                                else if(*(cache_0 + pixel[8]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else if(*(cache_0 + pixel[9]) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                if(*(cache_0 + pixel[0]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[6]) < c_b)
				                                    continue;
				                                else
				                                    if(*(cache_0 + pixel[15]) > cb)
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_0 + pixel[9]) > cb)
				                                                    if(*(cache_0 + pixel[8]) > cb)
				                                                        goto success;
				                                                    else if(*(cache_0 + pixel[8]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                else if(*(cache_0 + pixel[9]) < c_b)
				                                                    continue;
				                                                else
				                                                    if(*(cache_2+4) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            if(*(cache_0 + pixel[0]) > cb)
				                                                                goto success;
				                                                            else
				                                                                continue;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_0 + pixel[10]) < c_b)
				                                            continue;
				                                        else
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    if(*(cache_2+4) > cb)
				                                                        if(*(cache_0 + pixel[13]) > cb)
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
				                            else if(*(cache_1+-6) < c_b)
				                                continue;
				                            else
				                                if(*(cache_0+3) > cb)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                if(*(cache_0 + pixel[3]) > cb)
				                                                    if(*(cache_0 + pixel[13]) > cb)
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
				                        else if(*(cache_0 + pixel[7]) < c_b)
				                            if(*(cache_2+4) > cb)
				                                if(*(cache_1+-6) > cb)
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_0 + pixel[15]) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_0 + pixel[1]) > cb)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_0 + pixel[3]) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_0 + pixel[10]) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_0 + pixel[0]) > cb)
				                                                    if(*(cache_0 + pixel[1]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else if(*(cache_1+-6) < c_b)
				                                    if(*(cache_0+3) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*(cache_0 + pixel[0]) > cb)
				                                                if(*(cache_0 + pixel[3]) > cb)
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
				                                    if(*(cache_0+3) > cb)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_0 + pixel[3]) > cb)
				                                                    if(*(cache_0 + pixel[0]) > cb)
				                                                        if(*(cache_0 + pixel[15]) > cb)
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
				                            else if(*(cache_2+4) < c_b)
				                                continue;
				                            else
				                                if(*(cache_0 + pixel[9]) > cb)
				                                    if(*(cache_0 + pixel[0]) > cb)
				                                        if(*(cache_1+-6) > cb)
				                                            goto success;
				                                        else
				                                            continue;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                        else
				                            if(*(cache_0 + pixel[0]) > cb)
				                                if(*(cache_0 + pixel[10]) > cb)
				                                    if(*(cache_2+4) > cb)
				                                        if(*(cache_0 + pixel[13]) > cb)
				                                            if(*(cache_1+-6) > cb)
				                                                if(*(cache_0 + pixel[15]) > cb)
				                                                    if(*(cache_0 + pixel[1]) > cb)
				                                                        goto success;
				                                                    else if(*(cache_0 + pixel[1]) < c_b)
				                                                        continue;
				                                                    else
				                                                        if(*(cache_0 + pixel[8]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                else
				                                                    continue;
				                                            else if(*(cache_1+-6) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_0+3) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_2+4) < c_b)
				                                        if(*(cache_0 + pixel[1]) > cb)
				                                            if(*(cache_0 + pixel[3]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_0 + pixel[9]) > cb)
				                                            if(*(cache_0 + pixel[1]) > cb)
				                                                if(*(cache_0 + pixel[13]) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        if(*(cache_1+-6) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else if(*(cache_0 + pixel[1]) < c_b)
				                                                continue;
				                                            else
				                                                if(*(cache_0 + pixel[8]) > cb)
				                                                    if(*(cache_1+-6) > cb)
				                                                        if(*(cache_0 + pixel[13]) > cb)
				                                                            if(*(cache_0 + pixel[15]) > cb)
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
				                                else if(*(cache_0 + pixel[10]) < c_b)
				                                    if(*(cache_0+3) > cb)
				                                        if(*(cache_0 + pixel[13]) > cb)
				                                            if(*(cache_2+4) > cb)
				                                                if(*(cache_0 + pixel[3]) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                    else if(*(cache_0+3) < c_b)
				                                        continue;
				                                    else
				                                        if(*(cache_1+-6) > cb)
				                                            if(*(cache_0 + pixel[3]) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else
				                                    if(*(cache_0 + pixel[3]) > cb)
				                                        if(*(cache_1+-6) > cb)
				                                            if(*(cache_0 + pixel[13]) > cb)
				                                                if(*(cache_2+4) > cb)
				                                                    if(*(cache_0 + pixel[15]) > cb)
				                                                        if(*(cache_0 + pixel[1]) > cb)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else if(*(cache_1+-6) < c_b)
				                                            if(*(cache_0+3) > cb)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0+3) > cb)
				                                                if(*(cache_0 + pixel[13]) > cb)
				                                                    if(*(cache_0 + pixel[1]) > cb)
				                                                        if(*(cache_2+4) > cb)
				                                                            if(*(cache_0 + pixel[15]) > cb)
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
				                else if(*(cache_0+-3) < c_b)
				                    if(*(cache_0 + pixel[15]) > cb)
				                        if(*cache_2 < c_b)
				                            if(*(cache_0 + pixel[6]) < c_b)
				                                if(*(cache_0 + pixel[10]) < c_b)
				                                    if(*(cache_0 + pixel[7]) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else
				                                continue;
				                        else
				                            continue;
				                    else if(*(cache_0 + pixel[15]) < c_b)
				                        if(*(cache_0 + pixel[10]) > cb)
				                            if(*(cache_0+3) > cb)
				                                continue;
				                            else if(*(cache_0+3) < c_b)
				                                if(*(cache_0 + pixel[3]) < c_b)
				                                    if(*(cache_0 + pixel[13]) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                            else
				                                if(*(cache_1+-6) < c_b)
				                                    if(*(cache_0 + pixel[3]) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else
				                                    continue;
				                        else if(*(cache_0 + pixel[10]) < c_b)
				                            if(*cache_2 < c_b)
				                                if(*(cache_0 + pixel[9]) > cb)
				                                    if(*(cache_2+4) < c_b)
				                                        goto success;
				                                    else
				                                        continue;
				                                else if(*(cache_0 + pixel[9]) < c_b)
				                                    if(*(cache_1+-6) > cb)
				                                        continue;
				                                    else if(*(cache_1+-6) < c_b)
				                                        if(*(cache_0 + pixel[13]) < c_b)
				                                            if(*(cache_0 + pixel[1]) > cb)
				                                                if(*(cache_0 + pixel[7]) < c_b)
				                                                    goto success;
				                                                else
				                                                    continue;
				                                            else if(*(cache_0 + pixel[1]) < c_b)
				                                                if(*(cache_0 + pixel[0]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[0]) < c_b)
				                                                    goto success;
				                                                else
				                                                    if(*(cache_0 + pixel[7]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                            else
				                                                if(*(cache_0 + pixel[7]) > cb)
				                                                    continue;
				                                                else if(*(cache_0 + pixel[7]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    if(*(cache_0 + pixel[0]) < c_b)
				                                                        if(*(cache_0 + pixel[8]) < c_b)
				                                                            goto success;
				                                                        else
				                                                            continue;
				                                                    else
				                                                        continue;
				                                        else
				                                            continue;
				                                    else
				                                        if(*(cache_0+3) < c_b)
				                                            if(*(cache_0 + pixel[3]) < c_b)
				                                                goto success;
				                                            else
				                                                continue;
				                                        else
				                                            continue;
				                                else
				                                    if(*(cache_2+4) < c_b)
				                                        if(*(cache_1+-6) > cb)
				                                            continue;
				                                        else if(*(cache_1+-6) < c_b)
				                                            if(*(cache_0 + pixel[13]) < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    if(*(cache_0 + pixel[0]) < c_b)
				                                                        goto success;
				                                                    else
				                                                        continue;
				                                                else
				                                                    continue;
				                                            else
				                                                continue;
				                                        else
				                                            if(*(cache_0+3) < c_b)
				                                                if(*(cache_0 + pixel[3]) < c_b)
				                                                    if(*(cache_0 + pixel[0]) < c_b)
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
				                            if(*(cache_0 + pixel[3]) < c_b)
				                                if(*(cache_1+-6) > cb)
				                                    continue;
				                                else if(*(cache_1+-6) < c_b)
				                                    if(*(cache_2+4) < c_b)
				                                        if(*(cache_0 + pixel[13]) < c_b)
				                                            if(*cache_2 < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    if(*(cache_0 + pixel[0]) < c_b)
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
				                                    if(*(cache_0+3) < c_b)
				                                        if(*(cache_2+4) < c_b)
				                                            if(*cache_2 < c_b)
				                                                if(*(cache_0 + pixel[1]) < c_b)
				                                                    if(*(cache_0 + pixel[13]) < c_b)
				                                                        if(*(cache_0 + pixel[0]) < c_b)
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
				                        if(*(cache_0 + pixel[6]) < c_b)
				                            if(*cache_2 < c_b)
				                                if(*(cache_0 + pixel[7]) < c_b)
				                                    if(*(cache_1+-6) < c_b)
				                                        if(*(cache_0 + pixel[13]) < c_b)
				                                            if(*(cache_0 + pixel[10]) < c_b)
				                                                if(*(cache_0 + pixel[9]) < c_b)
				                                                    if(*(cache_0 + pixel[8]) < c_b)
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
                        success:
                            if (total >= rsize)
                            {
                                rsize *= 2;
                                #if DOTNET_V1_1
                                    ret = (FASTcorner[])ArrayResize(ret, rsize);
                                #else
                                    Array.Resize(ref ret, rsize);
                                #endif
                            }

                            int xx = (int)(cache_0 - line_min);
                            int yy = y;
                            ret[total] = new FASTcorner(xx, yy);
                            total++;
                        }
                    }
                    // resize the array so that we don't need to explicityly pass back the total
                    // number of corners found
                    #if DOTNET_V1_1
                        ret = (FASTcorner[])ArrayResize(ret, total);
                    #else
                        Array.Resize(ref ret, total);
                    #endif
                    return ret;
                }
            }
        }
           
				                    
        /// <summary>
        /// detect FAST corners
        //  It is assumed that the image supplied is mono (one byte per pixel)
        /// Note that non-maximal supression should be carried out after running this function
        /// </summary>
        /// <param name="img">image - one byte per pixel</param>
        /// <param name="xsize"></param>
        /// <param name="ysize"></param>
        /// <param name="barrier">detection threshold</param>
        /// <returns>array of corner features</returns>
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
                                #if DOTNET_V1_1
                                    ret = (FASTcorner[])ArrayResize(ret, rsize);
                                #else
                                    Array.Resize(ref ret, rsize);
                                #endif
                            }

                            int xx = (int)(cache_0 - line_min);
                            int yy = y;
                            ret[total] = new FASTcorner(xx, yy);
                            total++;
                        }
                    }
                    // resize the array so that we don't need to explicityly pass back the total
                    // number of corners found
                    #if DOTNET_V1_1
                        ret = (FASTcorner[])ArrayResize(ret, total);
                    #else
                        Array.Resize(ref ret, total);
                    #endif
                    return ret;
                }
            }
        }
        
        /// <summary>
        /// converts a colour image into a mono one
        /// </summary>
        public static byte[] ConvertToMono(byte[] bmp, int image_width, int image_height)
        {
            int pixels = image_width * image_height;
            int bytes_per_pixel = bmp.Length / pixels;
            
            byte[] mono_image = bmp;
            
            if (bytes_per_pixel > 1)
            {
                int n = 0;
                mono_image = new byte[pixels];
	            for (int i = 0; i < bmp.Length; i += bytes_per_pixel)
	            {
	                int intensity = 0;
	                for (int j = 0; j < bytes_per_pixel; j++)
	                    intensity += bmp[i + j];
	                mono_image[n] = (byte)(intensity / bytes_per_pixel);
	                n++;
	            }
            }
            return(mono_image);
        }


        /// <summary>
        /// show the orientations of features
        /// </summary>
        /// <param name="corners">array containing detected corners</param>
        /// <param name="background">background image to be used</param>
        /// <param name="bmp">image to be returned</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="min_neighbours">minimum number of features within the neighbourhood</param>
        /// <param name="min_direction_magnitude">minimum direction magnitude</param>
        public static void ShowOrientations(FASTcorner[] corners, 
                                            byte[] background,
                                            byte[] bmp, int image_width, int image_height,
                                            int bytes_per_pixel,
                                            int min_neighbours, int min_direction_magnitude)
        {
            // copy the background
            for (int i = 0; i < image_width * image_height * bytes_per_pixel; i++)
                bmp[i] = background[i];

            for (int i = 0; i < corners.Length; i++)
            {
                FASTcorner corner = corners[i];
                
                if (corner.neighbours != null)
                {
                    if (corner.neighbours.Count > min_neighbours)
                    {
                        if (corner.direction_magnitude > min_direction_magnitude)
                        {
			                if (bytes_per_pixel == 3)
			                {
			                    sluggish.utilities.drawing.drawLine(bmp, image_width, image_height,
			                             (int)corner.x, (int)corner.y,
			                             (int)(corner.x + corner.direction_x), (int)(corner.y + corner.direction_y),
			                             0, 255, 0, 0, false);
			                }
		                }
	                }
                }
            }
        }
            
        
        /// <summary>
        /// show the detected corner features in the given image
        /// </summary>
        /// <param name="corners">array containing detected corners</param>
        /// <param name="background">background image to be used</param>
        /// <param name="bmp">image to be returned</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        public static void Show(FASTcorner[] corners, 
                                byte[] background,
                                byte[] bmp, int image_width, int image_height,
                                int bytes_per_pixel)
        {
            // copy the background
            for (int i = 0; i < image_width * image_height * bytes_per_pixel; i++)
                bmp[i] = background[i];
                
            for (int i = 0; i < corners.Length; i++)
            {
                FASTcorner corner = corners[i];
                int n = ((corner.y * image_width) + corner.x) * bytes_per_pixel;
                
                // update the image
                if (bytes_per_pixel < 3)
                {
                    bmp[n] = (byte)255;
                }
                else
                {
                    for (int col = 0; col < bytes_per_pixel; col++)
                    {
                        if (col != 1)
                            bmp[n + col] = 0;
                        else
                            bmp[n + col] = (byte)255;
                    }
                }
            }
        }
        
        
        /// <summary>
        /// show the detected line features in the given image
        /// </summary>
        /// <param name="lines">array containing detected lines</param>
        /// <param name="background">background image to be used</param>
        /// <param name="bmp">image to be returned</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        public static void Show(FASTline[] lines, 
                                byte[] background,
                                byte[] bmp, int image_width, int image_height,
                                int bytes_per_pixel)
        {
            // copy the background
            for (int i = 0; i < image_width * image_height * bytes_per_pixel; i++)
                bmp[i] = background[i];
                
            for (int i = 0; i < lines.Length; i++)
            {
                FASTline line = lines[i];
                
                if (bytes_per_pixel == 3)
                {
                    sluggish.utilities.drawing.drawLine(bmp, image_width, image_height,
                             line.point1.x, line.point1.y, line.point2.x, line.point2.y,
                             0, 255, 0, 0, false);
                }
            }
        }

        
        
        /// <summary>
	    /// detect corners within the given image
	    /// </summary>
	    /// <param name="threshold">corner detection threshold</param>
	    /// <param name="bmp">colour image data</param>
	    /// <param name="image_width">width of the image</param>
	    /// <param name="image_height">height of the image</param>
	    /// <param name="bmp_mono">return the mono image for subsequent use (eg line detection)</param>
		public static FASTcorner[] detectCorners(int threshold, 
		                                         byte[] bmp, int image_width, int image_height,
		                                         ref byte[] bmp_mono,
		                                         int non_maximal_suppression_radius)
		{
		    FASTcorner[] corners = null;
		    
		    if (bmp != null)
		    {
		        // convert the colour image to mono
			    if (bmp_mono == null) bmp_mono = ConvertToMono(bmp, image_width, image_height);
			    
			    // find corner features
			    corners = fast_corner_detect_9(bmp_mono, image_width, image_height, threshold);
			    
			    // perform non-maximal supression
			    if (non_maximal_suppression_radius > 0)
			        corners = fast_nonmax(bmp_mono, image_width, image_height, corners, non_maximal_suppression_radius, 0, 0);
			}
			return(corners);
		}
		
		
        /// <summary>
	    /// detect corners within the given image
	    /// </summary>
	    /// <param name="threshold">corner detection threshold</param>
	    /// <param name="bmp">colour image data</param>
	    /// <param name="image_width">width of the image</param>
	    /// <param name="image_height">height of the image</param>
	    public static FASTcorner[] detectCorners(int threshold, 
		                                         byte[] bmp, int image_width, int image_height,
		                                         int non_maximal_suppression_radius)
		{
		    byte[] bmp_mono = null;
		    return(detectCorners(threshold, bmp, image_width, image_height, ref bmp_mono, non_maximal_suppression_radius));
		}

		
		/// <summary>
		/// update the neighbours for each corner feature
		/// </summary>
		/// <param name="corners">array of corner features</param>
		/// <param name="neighbour_radius">neighbourhood radius</param>
		public static void updateNeighbours(FASTcorner[] corners, int neighbour_radius)
		{
		    for (int i = 0; i < corners.Length; i++)
		        corners[i].updateDirection(corners, neighbour_radius);
		}

    }
   
}

