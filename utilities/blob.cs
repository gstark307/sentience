/*
    blob properties, used for calibration spot detection
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
    public class blob
    {
        // used to mark this feature as having been selected
        public bool selected;
        public bool touched;

        // position of the blob within the image, only intended to be pixel accurate
        public float x, y;

        // sub-pixel interpolated position within the image
        public float interpolated_x, interpolated_y;

        // average radius of the blob
        public float average_radius;

        // ovality of the blob
        public float ovality;

        // radial profile
        public float[] radial_profile;
        
        // streuth, it's a list of neighbouring blobs mate!
        public ArrayList neighbours;

        // distances between this blob and neighbours
        public ArrayList separation;

        // angle between this blob and neighbours
        public ArrayList angle;

        // average pixel intensity
        public float average_intensity;

        public ArrayList[] neighbourDirections;
        public const int direction_increment_degrees = 5;

        /// <summary>
        /// returns a copy of this blob
        /// </summary>
        public blob Copy()
        {
            blob new_blob = new blob(x, y);
            new_blob.interpolated_x = interpolated_x;
            new_blob.interpolated_y = interpolated_y;
            new_blob.average_radius = average_radius;
            new_blob.ovality = ovality;
            new_blob.average_intensity = average_intensity;
            return(new_blob);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x">x coordinate of the blob within the image</param>
        /// <param name="y">y coordinate of the blob within the image</param>
        public blob(float x, float y)
        {
            this.x = x;
            this.y = y;
            interpolated_x = x;
            interpolated_y = y;

            neighbours = new ArrayList();
            separation = new ArrayList();
            angle = new ArrayList();
            neighbourDirections = new ArrayList[360 / direction_increment_degrees];
        }

        /// <summary>
        /// returns true if the given line intersects with this blob
        /// </summary>
        /// <param name="x0">line first point x coordinate</param>
        /// <param name="y0">line first point y coordinate</param>
        /// <param name="x1">line second point x coordinate</param>
        /// <param name="y1">line second point y coordinate</param>
        /// <param name="max_distance">the maximum perpendicular distance below which the blob is considered to touch the line, in the range, typically in the range 0.0-1.0 as a fraction of the blob radius</param>
        /// <returns>true if the given line intersects with this blob</returns>
        public bool intersectsWithLine(float x0, float y0, float x1, float y1, 
                                       float max_distance,
                                       ref float perpendicular_distance)
        {
            perpendicular_distance = sluggish.utilities.geometry.circleDistanceFromLine(x0, y0, x1, y1, interpolated_x, interpolated_y, average_radius);
            if (perpendicular_distance <= average_radius * max_distance)
                return (true);
            else
                return (false);
        }

        /// <summary>
        /// detect the radius of the spot within the given map
        /// </summary>
        /// <param name="spot_map">map containing spot responses</param>
        /// <param name="max_radius">maximum radius</param>
        public void detectRadius(float[,] spot_map, int max_radius,
                                 byte[] mono_image)
        {
            float min_spot_response = 0.1f;

            int xx = (int)x;
            int yy = (int)y;
            float prev_value = spot_map[xx, yy];
            bool finished_growing = false;
            int hits = 0;

            int w = spot_map.GetLength(0);
            int h = spot_map.GetLength(1);

            // grow to the right
            float length_right = 0;
            while ((xx - (int)x < max_radius) && 
                   (xx < w-1) && 
                   (!finished_growing))
            {
                float value = spot_map[xx, yy];
                if ((value > min_spot_response) && (value <= prev_value))
                {
                    length_right++;
                    average_intensity += mono_image[(yy * w) + xx];
                    hits++;
                }
                else
                {
                    if (prev_value != value)
                        length_right += (min_spot_response - value);

                    finished_growing = true;
                }
                prev_value = value;
                xx++;
            }

            // grow to the left
            xx = (int)x;
            yy = (int)y;
            prev_value = spot_map[xx, yy];
            finished_growing = false;
            float length_left = 0;
            while ((x - (int)xx < max_radius) &&
                   (xx > 1) &&
                   (!finished_growing))
            {
                float value = spot_map[xx, yy];
                if ((value > min_spot_response) && (value <= prev_value))
                {
                    length_left++;
                    average_intensity += mono_image[(yy * w) + xx];
                    hits++;
                }
                else
                {
                    if (prev_value != value)
                        length_left += (min_spot_response - value);

                    finished_growing = true;
                }
                prev_value = value;
                xx--;
            }

            // grow up
            xx = (int)x;
            yy = (int)y;
            prev_value = spot_map[xx, yy];
            finished_growing = false;
            float length_above = 0;
            while (((int)y - yy < max_radius) &&
                   (yy > 1) &&
                   (!finished_growing))
            {
                float value = spot_map[xx, yy];
                if ((value > min_spot_response) && (value <= prev_value))
                {
                    length_above++;
                    average_intensity += mono_image[(yy * w) + xx];
                    hits++;
                }
                else
                {
                    if (prev_value != value)
                        length_above += (min_spot_response - value);

                    finished_growing = true;
                }
                prev_value = value;
                yy--;
            }

            // grow down
            xx = (int)x;
            yy = (int)y;
            prev_value = spot_map[xx, yy];
            finished_growing = false;
            float length_below = 0;
            while ((yy - (int)y < max_radius) &&
                   (yy < h-1) &&
                   (!finished_growing))
            {
                float value = spot_map[xx, yy];
                if ((value > min_spot_response) && (value <= prev_value))
                {
                    length_below++;
                    average_intensity += mono_image[(yy * w) + xx];
                    hits++;
                }
                else
                {
                    if (prev_value != value)
                        length_below += (min_spot_response - value);

                    finished_growing = true;
                }
                prev_value = value;
                yy++;
            }

            // horizontal and vertical diameter of the blob
            float diameter_x = length_left + length_right;
            float diameter_y = length_above + length_below;

            // calc average radius
            average_radius = (diameter_x + diameter_y) / 4.0f;

            // calc ovality
            if (diameter_x > 0)
                ovality = 1.0f - (diameter_y / diameter_x);

            if (hits > 0)
                average_intensity /= hits;
        }

        #region "adding neighbours"

        /// <summary>
        /// add a neighbour if it's in da hood
        /// </summary>
        /// <param name="neighbour">a possibly neighbouring blob</param>
        /// <param name="neighbourhood_radius">the neighbourhood event horizon</param>
        public bool AddNeighbour(blob neighbour, float neighbourhood_radius)
        {
            float dx = neighbour.interpolated_x - interpolated_x;
            float dy = neighbour.interpolated_y - interpolated_y;
            float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));

            if (dist < neighbourhood_radius)
            {
                AddNeighbour(neighbour);
                return (true);
            }
            else return (false);
        }

        /// <summary>
        /// returns the distance from this blob to another
        /// </summary>
        /// <param name="other">the other blob object</param>
        /// <returns>distance to the other blob</returns>
        public float getSeparation(blob other)
        {
            float dx = other.interpolated_x - interpolated_x;
            float dy = other.interpolated_y - interpolated_y;
            float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
            return (dist);
        }

        /// <summary>
        /// everybody needs good neighbours...
        /// </summary>
        /// <param name="neighbour">a neighbouring blob</param>
        public void AddNeighbour(blob neighbour)
        {
            // update the neighbours list
            neighbours.Add(neighbour);

            // calculate the separation
            float dist = getSeparation(neighbour);
            separation.Add(dist);

            // calculate the angle to this neighbour
            float ang = 0;
            if (dist > 0.1f)
            {
                float dx = neighbour.interpolated_x - interpolated_x;
                float dy = neighbour.interpolated_y - interpolated_y;
                ang = (float)Math.Asin(dx / dist);
                if (dy < 0) ang = (float)(Math.PI * 2) - ang;
                if (ang < 0) ang += (float)(Math.PI);
                if (ang == float.NaN) ang = 0;
            }
            angle.Add(ang);
            
            // populate the direction lookup table
            int ang_index = (int)Math.Round((ang * 180 / Math.PI) / direction_increment_degrees);
            if (ang_index >= neighbourDirections.Length)
                ang_index = neighbourDirections.Length - 1;
            if (neighbourDirections[ang_index] == null)
                neighbourDirections[ang_index] = new ArrayList();
            neighbourDirections[ang_index].Add(neighbours.Count-1);
        }

        #endregion

        /// <summary>
        /// returns the length of the longest path from this blob feature
        /// </summary>
        /// <param name="mark_path">mark the longest path as being selected</param>
        /// <param name="best_end_point">blob at the longest distance</param>
        /// <returns></returns>
        public int getLongestPath(bool mark_path, ref blob end_point)
        {
            int longest = 0;
            float best_ang = 0;
            float tollerance_degrees = 5;

            for (int i = 0; i < neighbours.Count; i++)
            {
                float ang = (float)angle[i];
                blob curr_end_point = null;
                int length = getDirectionalLength(ang, false, true,0,1000, tollerance_degrees, ref curr_end_point);

                if (length > longest)
                {
                    longest = length;
                    best_ang = ang;
                    end_point = curr_end_point;
                }
            }
            if ((longest > 0) && (mark_path))
            {
                blob curr_end_point = null;
                getDirectionalLength(best_ang, true, true,0,1000, tollerance_degrees, ref curr_end_point);
            }
            return (longest);
        }

        public int getDirectionalLength(float direction_radians, 
                                        bool mark_path,
                                        bool ignore_selected,
                                        int depth, int max_depth,
                                        float tollerance_degrees,
                                        ref blob end_point)
        {
            int length = 0;

            // mark the path if necessary
            if (mark_path) selected = true;

            if (depth < max_depth)
            {
                // get the index of the lookup table for this direction
                int ang_index = (int)Math.Round((direction_radians * 180 / Math.PI) / direction_increment_degrees);
                if (ang_index >= neighbourDirections.Length)
                    ang_index -= neighbourDirections.Length;

                // search for neighbours in this direction
                float max_ang_diff = tollerance_degrees * (float)Math.PI / 180.0f;
                //float closest_ang = 0;
                blob winner = null;
                for (int j = -1; j <= 1; j++)
                {
                    if ((ang_index + j > -1) &&
                        (ang_index + j < neighbourDirections.Length))
                    {
                        // are there any neighbours in this direction?
                        if (neighbourDirections[ang_index + j] != null)
                        {
                            // search through all neighbours to find the one which
                            // has the most similar direction
                            for (int i = 0; i < neighbourDirections[ang_index + j].Count; i++)
                            {
                                int neighbour_index = (int)neighbourDirections[ang_index + j][i];
                                if ((!ignore_selected) ||
                                   ((ignore_selected) && (!((blob)neighbours[neighbour_index]).selected)))
                                {
                                    float neighbour_ang = (float)angle[neighbour_index];
                                    float ang_diff = Math.Abs(neighbour_ang - direction_radians);
                                    if (ang_diff < max_ang_diff)
                                    {
                                        // this is the most similar direction yet found
                                        //closest_ang = neighbour_ang;
                                        max_ang_diff = ang_diff;
                                        winner = (blob)neighbours[neighbour_index];
                                    }
                                }
                            }
                        }
                    }
                }
                if ((winner != null) && (winner != this))
                {
                    // alter the search direction slighly
                    // note: this might not be needed
                    //direction_radians = (direction_radians * 0.8f) + (closest_ang * 0.2f);

                    // keep note of the end point
                    end_point = winner;

                    // update the length, then carry on searching
                    length = 1 + winner.getDirectionalLength(direction_radians, mark_path, ignore_selected, depth + 1, max_depth, tollerance_degrees, ref end_point);
                }
            }

            return (length);
        }
    }
}
