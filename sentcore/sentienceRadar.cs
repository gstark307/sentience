/*
    Sentience 3D Perception System
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
using sluggish.utilities;

namespace sentience.core
{
    public class sentienceRadar
    {
        public float scale = 2000;
        public int display_type = 0;
        public int smoothing_steps = 10;

        public Byte[] img;
        float[] rawranges_horizontal = null;
        float[] rawranges_vertical = null;
        float[] ranges_horizontal = null;
        float[] tempranges_horizontal = null;
        float[] ranges_vertical = null;
        float[] variation_horizontal = null;
        float[] variation_vertical = null;
        //float[] av_variation_horizontal = null;
        int[] hits_horizontal;
        int[] hits_vertical;

        float average_disparity = 0;

        /// <summary>
        /// return the distance to obstacles in mm
        /// </summary>
        /// <param name="focal_length_mm"></param>
        /// <param name="camera_baseline_mm"></param>
        /// <param name="img_width"></param>
        /// <param name="FOV_radians"></param>
        /// <returns></returns>
        public float getObstacleDistance(float focal_length_mm, float camera_baseline_mm, 
                                                int img_width, float FOV_radians)
        {
            float disparity_angle = average_disparity * FOV_radians / img_width;
            return (focal_length_mm * camera_baseline_mm / disparity_angle);
        }

        /// <summary>
        /// update the radar
        /// </summary>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="features"></param>
        public void update(int img_width, int img_height,
                           stereoFeatures features)
        {
            if (ranges_horizontal == null)
            {
                ranges_horizontal = new float[img_width];
                ranges_vertical = new float[img_height];
                rawranges_horizontal = new float[img_width];
                tempranges_horizontal = new float[img_width];
                rawranges_vertical = new float[img_height];
                variation_horizontal = new float[img_width];
                variation_vertical = new float[img_height];
                //av_variation_horizontal = new float[img_width];
                hits_horizontal = new int[img_width];
                hits_vertical = new int[img_height];
                img = new Byte[img_width * img_height * 3];
                for (int h = 0; h < img_height; h++)
                {
                    rawranges_vertical[h] = 0;
                    hits_vertical[h] = 0;
                }
                for (int w = 0; w < img_width; w++)
                {
                    rawranges_horizontal[w] = 0;
                    hits_horizontal[w] = 0;
                    //av_variation_horizontal[w] = 0;
                }
            }
            for (int h = 0; h < img_height; h++)
            {
                variation_vertical[h] = ranges_vertical[h];
                rawranges_vertical[h] = 0;
                hits_vertical[h] = 0;
            }
            for (int w = 0; w < img_width; w++)
            {
                variation_horizontal[w] = ranges_horizontal[w];
                rawranges_horizontal[w] = 0;
                hits_horizontal[w] = 0;
            }

            for (int i = 0; i < features.no_of_features; i++)
            {
                int x = (int)features.features[i * 3];
                int y = (int)features.features[(i * 3) + 1];
                float disp = features.features[(i * 3) + 2];
                if (disp > 0)
                {
                    float dist = 1 / disp;
                    rawranges_horizontal[x] += dist;
                    hits_horizontal[x]++;
                    rawranges_vertical[y] += dist;
                    hits_vertical[y]++;
                }
            }

            // clear the image
            for (int i = 0; i < img_width * img_height * 3; i++) img[i] = 0;

            for (int w = 0; w < img_width; w++)
            {
                if (hits_horizontal[w] > 0)
                {
                    rawranges_horizontal[w] = rawranges_horizontal[w] * scale / hits_horizontal[w];
                    variation_horizontal[w] -= rawranges_horizontal[w];
                    ranges_horizontal[w] = rawranges_horizontal[w];
                    tempranges_horizontal[w] = ranges_horizontal[w];
                }
            }

            // smooth out noise
            int prev_range_index = -1;
            int prev_range_index2 = -1;
            for (int smooth = 0; smooth < smoothing_steps; smooth++)
            {
                for (int w = 1; w < img_width-1; w++)
                    if (hits_horizontal[w] > 0)
                    {
                        if ((prev_range_index > -1) && (prev_range_index2 > -1))
                        {
                            ranges_horizontal[prev_range_index] = (tempranges_horizontal[w] + tempranges_horizontal[prev_range_index] + tempranges_horizontal[prev_range_index2]) / 3.0f;
                        }
                        prev_range_index2 = prev_range_index;
                        prev_range_index = w;
                    }
                for (int w = 1; w < img_width-1; w++)
                    if (hits_horizontal[w] > 0)
                        tempranges_horizontal[w] = ranges_horizontal[w];
            }


            prev_range_index = -1;
            prev_range_index2 = -1;
            for (int w = 0; w < img_width; w++)
                if (hits_horizontal[w] > 0)
                {
                    if (variation_horizontal[w] < 0) variation_horizontal[w] = -variation_horizontal[w];
                    
                    if (ranges_horizontal[w]>0.01f)
                        hits_horizontal[w] = 1;
                    else
                        hits_horizontal[w] = 0;
                    prev_range_index2 = prev_range_index;
                    prev_range_index = w;
                }

            prev_range_index = -1;
            prev_range_index2 = -1;
            for (int h = 0; h < img_height; h++)
                if (hits_vertical[h] > 0)
                {
                    rawranges_vertical[h] = rawranges_vertical[h] * scale / hits_vertical[h];
                    variation_vertical[h] -= rawranges_vertical[h];
                    ranges_vertical[h] = rawranges_vertical[h];
                    if ((h > 0) && (h < img_height - 1) &&
                        (prev_range_index > -1) && (prev_range_index2 > -1))
                    {
                        ranges_vertical[prev_range_index] = (rawranges_vertical[h] + rawranges_vertical[prev_range_index] + rawranges_vertical[prev_range_index2]) / 3.0f;
                    }

                    if (variation_vertical[h] < 0) variation_vertical[h] = -variation_vertical[h];

                    if (ranges_vertical[h] > 0.01f)
                        hits_vertical[h] = 1;
                    else
                        hits_vertical[h] = 0;
                    prev_range_index2 = prev_range_index;
                    prev_range_index = h;
                }



            int prev_x = 0;
            int prev_y = 0;
            //overhead
            for (int w = 0; w < img_width; w++)
            {
                if ((hits_horizontal[w] > 0) && (ranges_horizontal[w] > 0.01f))
                {
                    if ((variation_horizontal[w] < 50) && (variation_horizontal[prev_x] < 50))
                    {
                        if (prev_x > 0)
                        {
                            for (int xx = prev_x+1; xx < w; xx++)
                            {
                                ranges_horizontal[xx] = ranges_horizontal[prev_x] + ((xx - prev_x) * (ranges_horizontal[w] - ranges_horizontal[prev_x]) / (w - prev_x));
                                hits_horizontal[xx] = 1;
                            }
                            if (display_type == 0)
                            {
                                if (((int)ranges_horizontal[w] > 0) && ((int)ranges_horizontal[w] < img_height))
                                    drawing.drawLine(img, img_width, img_height, w, img_height - 1 - (int)ranges_horizontal[w], prev_x, prev_y, 0, 255, 0, 1, false);
                            }
                        }
                        prev_x = w;
                        prev_y = img_height - 1 - (int)ranges_horizontal[w];
                    }
                }
            }

            //side
            prev_x = 0;
            prev_y = 0;
            for (int h = 0; h < img_height; h++)
            {
                if ((hits_vertical[h] > 0) && (ranges_vertical[h] > 0.01f))
                {
                    if ((variation_vertical[h] < 50) && (variation_vertical[prev_y] < 50))
                    {
                        if (prev_y > 0)
                        {
                            for (int yy = prev_y + 1; yy < h; yy++)
                            {
                                ranges_vertical[yy] = ranges_vertical[prev_y] + ((yy - prev_y) * (ranges_vertical[h] - ranges_vertical[prev_y]) / (h - prev_y));
                                hits_vertical[yy] = 1;
                            }
                            if (display_type == 1)
                            {
                                if (((int)ranges_vertical[h] > 0) && ((int)ranges_vertical[h] < img_width))
                                    drawing.drawLine(img, img_width, img_height, (int)ranges_vertical[h], h, prev_x, prev_y, 0, 255, 0, 1, false);
                            }
                        }
                        prev_x = (int)ranges_vertical[h];
                        prev_y = h;
                    }
                }
            }

            //calculate average disparity
            int hits = 0;
            float av_disparity = 0;
            for (int w = img_width / 4; w < img_width - (img_width / 4); w++)
                if (hits_horizontal[w] > 0)
                {
                    av_disparity += (ranges_horizontal[w] / scale);
                    hits++;
                }
            if (hits > 0) av_disparity /= hits;
            if (av_disparity > 0) av_disparity = 1.0f / av_disparity;
            average_disparity = (average_disparity * 0.7f) + (av_disparity * 0.3f);

            if (display_type == 2)
            {
                for (int xx = 0; xx < img_width; xx++)
                {
                    if (hits_horizontal[xx] > 0)
                        for (int yy = 0; yy < img_height; yy++)
                        {
                            if (hits_vertical[yy] > 0)
                            {
                                int intensity = 255 - (int)((ranges_horizontal[xx] + ranges_vertical[yy]) * 255 / img_width);
                                if (intensity < 0) intensity = 0;
                                for (int col=0;col<3;col++)
                                    img[(((yy * img_width) + xx)*3) + col] = (Byte)intensity;
                            }
                        }
                }
            }

        }
    }
}
