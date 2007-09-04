/*
    Stores a set of regions which exist within an image
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

namespace sluggish.imageprocessing
{
    public class regionCollection : ArrayList
    {
        public regionCollection()
        {
        }

        /// <summary>
        /// returns the regions as a set of polygons
        /// </summary>
        /// <returns></returns>
        public ArrayList GetPolygons()
        {
            ArrayList polygons = new ArrayList();
            for (int i = 0; i < Count; i++)
            {
                region r = (region)this[i];
                polygons.Add(r.GetPolygon());
            }
            return (polygons);
        }

        public void Merge(int minimum_separation)
        {
            ArrayList new_regions = new ArrayList();

            for (int i = 0; i < Count; i++)
            {
                region r1 = (region)this[i];
                ArrayList merge_regions = new ArrayList();
                if (r1 != null)
                {
                    if (r1.width > 0)
                    {
                        for (int j = i + 1; j < Count; j++)
                        {
                            region r2 = (region)this[j];
                            if (r2 != null)
                            {
                                // similar centre x position
                                float dx = (r1.tx + r1.centre_x) - (r2.tx + r2.centre_x);
                                if (Math.Abs(dx) < minimum_separation)
                                {
                                    // similar centre y position
                                    float dy = (r1.ty + r1.centre_y) - (r2.ty + r2.centre_y);
                                    if (Math.Abs(dy) < minimum_separation)
                                    {
                                        // similar width
                                        float dw = (r2.width - r1.width) * 100 / r1.width;
                                        if (Math.Abs(dw) < 20)
                                        {
                                            merge_regions.Add(r2);
                                            this[j] = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (merge_regions.Count > 0)
                {
                    // merge regions together
                    float average_width = r1.width;
                    float average_orientation = r1.orientation;
                    int orientation_hits = 1;
                    for (int j = 0; j < merge_regions.Count; j++)
                    {
                        region r3 = (region)merge_regions[j];
                        average_width += r3.width;

                        //Console.WriteLine("orientation = " + ((int)(r3.orientation / Math.PI * 180)).ToString());

                        float orient = r3.orientation;
                        if (orient > Math.PI / 2) orient -= (float)Math.PI;

                        //average_orientation += orient;
                        //orientation_hits++;


                        float diff1 = Math.Abs(average_orientation - orient);
                        float diff2 = Math.Abs(average_orientation - (orient + (float)Math.PI));
                        float diff3 = Math.Abs(average_orientation - (orient - (float)Math.PI));
                        if ((diff1 < diff2) && (diff1 < diff3))
                        {
                            if (diff1 < Math.PI / 4)
                            {
                                average_orientation += orient;
                                orientation_hits++;
                            }
                        }
                        if ((diff2 < diff1) && (diff2 < diff3))
                        {
                            if (diff2 < Math.PI / 4)
                            {
                                average_orientation += (orient + (float)Math.PI);
                                orientation_hits++;
                            }
                        }
                        if ((diff3 < diff1) && (diff3 < diff2))
                        {
                            if (diff3 < Math.PI / 4)
                            {
                                average_orientation += (orient - (float)Math.PI);
                                orientation_hits++;
                            }
                        }


                    }

                    average_width /= (merge_regions.Count + 1);
                    //average_orientation /= (merge_regions.Count + 1);
                    if (orientation_hits > 0)
                    {
                        average_orientation /= orientation_hits;
                        float orient = r1.orientation;
                        if (orient > Math.PI / 2) orient -= (float)Math.PI;
                        //r1.polygon.rotate(orient - average_orientation, r1.centre_x, r1.centre_y);
                    }
                    r1.polygon.Scale(average_width / r1.width);
                    r1.confidence = merge_regions.Count;
                    //r1.orientation = average_orientation;

                    for (int j = 0; j < r1.polygon.x_points.Count; j++)
                    {
                        r1.corners[j * 2] = (float)r1.polygon.x_points[j];
                        r1.corners[(j * 2) + 1] = (float)r1.polygon.y_points[j];
                    }

                }
                if (r1 != null) new_regions.Add(r1);
            }

            Clear();
            for (int i = 0; i < new_regions.Count; i++)
                Add((region)new_regions[i]);
        }

        /// <summary>
        /// returns the region with the highest confidence value
        /// </summary>
        /// <returns></returns>
        public region highestConfidence()
        {
            float max = -1;
            region rmax = null;
            for (int i = 0; i < Count; i++)
            {
                region r = (region)this[i];
                if (r.confidence >= max)
                {
                    max = r.confidence;
                    rmax = r;
                }
            }
            return (rmax);
        }

        /// <summary>
        /// returns the region with most square aspect ratio
        /// </summary>
        /// <returns></returns>
        public region highestSquareness()
        {
            float max = -1;
            region rmax = null;
            for (int i = 0; i < Count; i++)
            {
                region r = (region)this[i];
                float squareness = r.getSquareness();
                if (squareness >= max)
                {
                    max = squareness;
                    rmax = r;
                }
            }
            return (rmax);
        }

        /// <summary>
        /// show all regions within the given image
        /// </summary>
        /// <param name="bmp">image to draw into</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="line_width">line width to use when drawing</param>
        /// <param name="style">display </param>
        public void Show(byte[] bmp, int image_width, int image_height,
                         int bytes_per_pixel, int line_width, int style)
        {
            for (int i = 0; i < Count; i++)
            {
                region r = (region)this[i];
                r.Show(bmp, image_width, image_height, bytes_per_pixel, line_width, style);
            }
        }
    }
}
