/*
    byte array image functions
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
using System.IO;
using sluggish.imageprocessing;

namespace sluggish.utilities
{
	public class image
	{
		public image()
		{
        }

        #region "multi-frame averaging"

        /// <summary>
		/// returns an image averaged over several frames
		/// </summary>
		/// <param name="img"></param>
		/// <param name="img_width"></param>
		/// <param name="img_height"></param>
		/// <param name="buffer"></param>
		/// <param name="buffer_index"></param>
		/// <param name="average_img"></param>
        public static void averageImage(
            byte[] img, int img_width, int img_height,
            byte[,] buffer, ref int buffer_index, ref int buffer_fill,
            byte[] average_img)
        {
            // update the buffer
            for (int i = 0; i < buffer.GetLength(1); i++)
                buffer[buffer_index, i] = img[i];

            // sum the buffer contents
            for (int i = 0; i < buffer.GetLength(1); i++)
            {
                float av_value = 0;
                for (int j = 0; j < buffer_fill; j++)
                {
                    av_value += buffer[j, i];
                }
                av_value /= buffer_fill;
                average_img[i] = (byte)Math.Round(av_value);
            }

            buffer_fill++;
            if (buffer_fill > buffer.GetLength(0))
                buffer_fill = buffer.GetLength(0);

            // rollover
            buffer_index++;
            if (buffer_index >= buffer.GetLength(0))
            {
                buffer_index = 0;
            }
        }

        #endregion

        #region "noise removal"

        /// <summary>
		/// remove solitary pixels from a binary image
		/// </summary>
		/// <param name="binary_image">a binary image</param>
		/// <param name="minimum_matches">the minimum number of pixels which have the same state as the centre pixel at each position</param>
		public static bool[,] removeSolitaryPixels(bool[,] binary_image,
		                                           int minimum_matches)
		{		
		    int width = binary_image.GetLength(0);
		    int height = binary_image.GetLength(1);
		    
		    bool[,] result = new bool[width, height];
		    
		    for (int x = 1; x < width-1; x++)
		    {
		        for (int y = 1; y < height-1; y++)
		        {
		            bool state = binary_image[x, y];
		            result[x, y] = state;
		            
		            int opposing_pixel_count = 0;

                    int xx = x - 1;
                    while ((xx <= x + 1) && (opposing_pixel_count < minimum_matches))
                    {
                        int yy = y - 1;
                        while ((yy <= y + 1) && (opposing_pixel_count < minimum_matches))
                        {
                            if (binary_image[xx, yy] != state) opposing_pixel_count++;
                            yy++;
                        }
                        xx++;
                    }
		                        
		            if (opposing_pixel_count > minimum_matches)
	    		        result[x, y] = !state;
		        }
		    }
		    return(result);
		}
		
		#endregion
		
		#region "edge detection/tracing within a line grid calibration pattern"

        /// <summary>
        /// crops line features to the given perimeter shape
        /// </summary>
        /// <param name="lines">list of line features</param>
        /// <param name="perimeter">defines the shape within which lines should reside</param>
        public static ArrayList cropLines(ArrayList lines, polygon2D perimeter)
        {
            ArrayList cropped = new ArrayList();
            
            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                if (perimeter.isInside((int)line1.x0, (int)line1.y0))
                    if (perimeter.isInside((int)line1.x1, (int)line1.y1))
                        cropped.Add(line1);
            }
            return(cropped);
        }

        /// <summary>
        /// returns the average gradient of all the given lines
        /// </summary>
        /// <param name="lines">list of line features</param>
        public static float getAverageGradient(ArrayList lines)
        {
            float av = 0;
            float tot_length = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                float grad = line1.getGradient();
                if (grad != 9999)
                {
                    float length = line1.getLength();
                    av += (grad * length * length);
                    tot_length += (length * length);
                }
            }
            if (av != 0)
                return (av / tot_length);
            else
                return (0);
        }

        /// <summary>
        /// returns the average orientation of all the given lines
        /// </summary>
        /// <param name="lines">list of line features</param>
        public static float getAverageOrientation(ArrayList lines)
        {
            float av = 0;
            float tot_length = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                float orient = line1.getOrientation();
                float length = line1.getLength();
                av += (orient * length * length);
                tot_length += (length * length);
            }
            if (av != 0)
                return (av / tot_length);
            else
                return (0);
        }


        /// <summary>
        /// returns the average orientation of all the given lines
        /// </summary>
        /// <param name="lines">list of line features</param>
        public static float getDominantOrientation(ArrayList lines, 
                                                   ref float score)
        {
            return(getDominantOrientation(lines, 0, ref score));
        }


        /// <summary>
        /// returns the average orientation of all the given lines
        /// </summary>
        /// <param name="lines">list of line features</param>
        /// <param name="orientation_type">0=any, 1=vertical, 2=horizontal</param>
        /// <param name="score">the maximum response score for the returned orientation</param>
        /// <returns>orientation in radians</return>
        public static float getDominantOrientation(ArrayList lines, 
                                                   int orientation_type,
                                                   ref float score)
        {
            float[] orientation_histogram = new float[360];
            float histogram_max = 0;
            float orientation = 0;
            
            score = 0;

            // find the maximum line length
            float max_length = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                float length = line1.getLength();
                if (length > max_length)
                    max_length = length;                
            }
            if (max_length > 0)
            {
                // only consider features with a significant length
                // to avoid small "noisy" lines
                float minimum_length = max_length*70/100;
                
	            for (int i = 0; i < lines.Count; i++)
	            {
	                linefeature line1 = (linefeature)lines[i];
	                float length = line1.getLength();
	                if (length > minimum_length)
	                {
	                    // get the orientation of the line in degrees
	                    int orientation_degrees = (int)Math.Round(line1.getOrientation() * 180.0f / Math.PI);
	                    if (orientation_degrees > 359) orientation_degrees = 359;
	                    
	                    bool update_histogram = true;
	                    
	                    if (orientation_type == 1)
	                    {	                        
	                        if (!((((orientation_degrees > 300) || (orientation_degrees < 50)) ||
	                            ((orientation_degrees > 130) && (orientation_degrees < 230)))))
	                            update_histogram = false;	                            
	                    }
	                    if (orientation_type == 2)
	                    {
	                        if (!((((orientation_degrees > 30) && (orientation_degrees < 140)) ||
	                            ((orientation_degrees > 220) && (orientation_degrees < 320)))))
	                            update_histogram = false;	                            
	                    }
	                    
	                    if (update_histogram)
	                    {
	                        // update the orientation histogram
	                        orientation_histogram[orientation_degrees] += (length*length);
	                        if (orientation_histogram[orientation_degrees] > histogram_max)
	                            histogram_max = orientation_histogram[orientation_degrees];
	                    }
	                }
	            }

	            if (histogram_max > 0)
	            {
	                // look for maxima
	                int search_angle = 2;
	                float max_response = 0;
	                int winner = -1;
	                for (int i = 0; i < 180; i++)
	                {
	                    float tot = 0;
	                    for (int j = i - search_angle; j <= i + search_angle; j++)
	                    {	                    
	                        int index = j;
	                        if (index < 0) index += 360;
	                        if (index >= 360) index -= 360;
	                        tot += orientation_histogram[index];

	                        index = j + 180;
	                        if (index < 0) index += 360;
	                        if (index >= 360) index -= 360;
	                        tot += orientation_histogram[index];
	                    }
	                    if (tot > max_response)
	                    {
	                        max_response = tot;
	                        winner = i;                        
	                    }                    
	                }
	                
	                score = max_response;
	                
	                float total = 0;
	                for (int j = winner - search_angle; j <= winner + search_angle; j++)
	                {
	                    int index1 = j;
	                    if (index1 < 0) index1 += 360;
	                    if (index1 >= 360) index1 -= 360;
	                    float v = orientation_histogram[index1];
	                    orientation += (v * index1);
	                    total += v;

	                    int index2 = j + 180;
	                    if (index2 < 0) index2 += 360;
	                    if (index2 >= 360) index2 -= 360;
	                    v = orientation_histogram[index2];
	                    orientation += (v * index1);
	                    total += v;
	                }
	                if (total > 0)
	                {
	                    orientation /= total;
	                    orientation = orientation * (float)Math.PI / 180.0f;
	                }
	            }
            }
                            
            return (orientation);
        }

/*
        /// <summary>
        /// returns the average orientation of all the given blob features
        /// </summary>
        /// <param name="blobs">list of blob features</param>
        /// <param name="orientation_type">0=any, 1=vertical, 2=horizontal</param>
        /// <param name="estimated_orientation_radians">estimated orientation in radians</param>
        /// <param name="score">the maximum response score for the returned orientation</param>
        /// <returns>orientation in radians</return>
        public static float getDominantOrientationBlobs(ArrayList blobs,
                                                        int orientation_type,
                                                        float estimated_orientation_radians,
                                                        ref float score)
        {
            float[] orientation_histogram = new float[360];
            float histogram_max = 0;
            float orientation = 0;
            float tollerance_degrees = 5;

            // initial estimate of the orientation
            //int estimated_orientation_degrees = (int)(estimated_orientation_radians * 180 / (float)Math.PI);
            //if (estimated_orientation_degrees < 0) estimated_orientation_degrees += 360;
            //if (estimated_orientation_degrees >= 360) estimated_orientation_degrees -= 360;
            //int search_tollerance_degrees = 20;

            score = 0;

            if (blobs != null)
            {
                for (int i = 0; i < blobs.Count; i++)
                {
                    blob b = (blob)blobs[i];

                    for (int j = 0; j < b.neighbours.Count; j++)
                    {
                        // direction to the neighbouring blob
                        float angle_radians = (float)b.angle[j];

                        blob end_point = null;
                        int max_depth = 50;
                        int path_length = b.getDirectionalLength(angle_radians, false, true, 0, max_depth, tollerance_degrees, ref end_point);


                        // get the orientation of the connection in degrees
                        float orientation_degrees = (int)Math.Round(angle_radians * 180.0f / Math.PI);
                        if (orientation_degrees > 359) orientation_degrees = 359;

                        bool update_histogram = true;

                        //float diff = 0;

                        if (orientation_type == 1)
                        {
                            //diff = orientation_degrees - estimated_orientation_degrees;

                            if (!((((orientation_degrees > 300) || (orientation_degrees < 50)) ||
                                ((orientation_degrees > 130) && (orientation_degrees < 230)))))
                                update_histogram = false;
                        }
                        if (orientation_type == 2)
                        {
                            //int est = estimated_orientation_degrees + 90;
                            //if (est >= 360) est -= 360;
                            //diff = orientation_degrees - est;

                            if (!((((orientation_degrees > 30) && (orientation_degrees < 140)) ||
                                ((orientation_degrees > 220) && (orientation_degrees < 320)))))
                                update_histogram = false;
                        }

                        //if (!((diff < search_tollerance_degrees) || 
                        //    ((diff > 180 - search_tollerance_degrees))))
                        //    update_histogram = false;

                        if (update_histogram)
                        {
                            // update the orientation histogram
                            int idx1 = (int)Math.Round(orientation_degrees / (float)blob.direction_increment_degrees);
                            if (idx1 >= b.neighbourDirections.Length) idx1 -= b.neighbourDirections.Length;
                            int blob_score = 1;

                            if (b.neighbourDirections[idx1] != null)
                                blob_score += b.neighbourDirections[idx1].Count;
                            int idx2 = idx1 + (b.neighbourDirections.Length / 2);
                            if (idx2 >= b.neighbourDirections.Length) idx2 -= b.neighbourDirections.Length;
                            if (b.neighbourDirections[idx2] != null)
                                blob_score += b.neighbourDirections[idx2].Count;

                            orientation_histogram[(int)orientation_degrees] += blob_score;
                            if (orientation_histogram[(int)orientation_degrees] > histogram_max)
                                histogram_max = orientation_histogram[(int)orientation_degrees];
                        }
                    }
                }

                if (histogram_max > 0)
                {
                    // look for maxima
                    int search_angle = 3;
                    float max_response = 0;
                    int winner = -1;
                    for (int i = 0; i < 180; i++)
                    {
                        float tot = 0;
                        for (int j = i - search_angle; j <= i + search_angle; j++)
                        {
                            float dist = 1.0f;

                            int index = j;
                            if (index < 0) index += 360;
                            if (index >= 360) index -= 360;
                            tot += (orientation_histogram[index] * dist);

                            index = j + 180;
                            if (index < 0) index += 360;
                            if (index >= 360) index -= 360;
                            tot += (orientation_histogram[index] * dist);
                        }
                        if (tot > max_response)
                        {
                            max_response = tot;
                            winner = i;
                        }
                    }

                    score = max_response;

                    float total = 0;
                    for (int j = winner - search_angle; j <= winner + search_angle; j++)
                    {
                        float dist = 1.0f;

                        int index1 = j;
                        if (index1 < 0) index1 += 360;
                        if (index1 >= 360) index1 -= 360;
                        float v = orientation_histogram[index1] * dist;
                        orientation += (v * index1);
                        total += v;

                        int index2 = j + 180;
                        if (index2 < 0) index2 += 360;
                        if (index2 >= 360) index2 -= 360;
                        v = orientation_histogram[index2] * dist;
                        orientation += (v * index1);
                        total += v;
                    }
                    if (total > 0)
                    {
                        orientation /= total;
                        orientation = orientation * (float)Math.PI / 180.0f;
                    }
                }
            }

            return (orientation);
        }
*/
        
        public static float getDominantGradient(ArrayList lines)
        {
            float max_length = 0;
            float av = 0;
            float hits = 0;
            
            // find the maximum length
            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                float length = line1.getLength();
                if (length > max_length)
                    max_length = length;
            }
            max_length *= max_length;

            float length_threshold = max_length*30/100;
            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                float length = line1.getLength();
                length *= length;
                if (length > length_threshold)
                {
                    av += (line1.getGradient() * length);
                    hits += length;
                }
            }
            if (hits > 0)
                return(av / hits);
            else
                return(0);            
        }

        /// <summary>
        /// links line features together
        /// It is assumed that all lines under consideration are of a similar orientation
        /// </summary>
        /// <param name="lines">list of line features to be joined</param>
        /// <param name="join_radius">the start or end points of the lines must be within this radius to be joined</param>
        public static ArrayList joinLines(ArrayList lines,  float join_radius)
        {
            for (int i = 0; i < lines.Count-1; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                float min_start_start_separation = 9999;
                float min_start_end_separation = 9999;
                float min_end_start_separation = 9999;
                float min_end_end_separation = 9999;
                for (int j= i + 1; j < lines.Count; j++)
                {
                    linefeature line2 = (linefeature)lines[j];
                    
                    // start to start
                    float dx = line2.x0 - line1.x0;
                    if (dx < 0) dx = -dx;
                    if (dx < join_radius)
                    {
                        float dy = line2.y0 - line1.y0;
                        if (dy < 0) dy = -dy;
                        if (dy < join_radius)
                        {
                            float separation = dx + dy;
                            if (separation < min_start_start_separation)
                            {
                                min_start_start_separation = separation;
                                line1.join_start = line2;
                                line2.join_start = line1;
                            }
                        }
                    }
                    
                    // start to end
                    dx = line2.x1 - line1.x0;
                    if (dx < 0) dx = -dx;
                    if (dx < join_radius)
                    {
                        float dy = line2.y1 - line1.y0;
                        if (dy < 0) dy = -dy;
                        if (dy < join_radius)
                        {
                            float separation = dx + dy;
                            if (separation < min_start_end_separation)
                            {
                                min_start_end_separation = separation;
                                line1.join_start = line2;
                                line2.join_end = line1;
                            }
                        }
                    }
                    
                    // end to start
                    dx = line2.x0 - line1.x1;
                    if (dx < 0) dx = -dx;
                    if (dx < join_radius)
                    {
                        float dy = line2.y0 - line1.y1;
                        if (dy < 0) dy = -dy;
                        if (dy < join_radius)
                        {
                            float separation = dx + dy;
                            if (separation < min_end_start_separation)
                            {
                                min_end_start_separation = separation;
                                line1.join_end = line2;
                                line2.join_start = line1;
                            }
                        }
                    }
                    
                    // end to end
                    dx = line2.x1 - line1.x1;
                    if (dx < 0) dx = -dx;
                    if (dx < join_radius)
                    {
                        float dy = line2.y0 - line1.y1;
                        if (dy < 0) dy = -dy;
                        if (dy < join_radius)
                        {
                            float separation = dx + dy;
                            if (separation < min_end_end_separation)
                            {
                                min_end_end_separation = separation;
                                line1.join_end = line2;
                                line2.join_end = line1;
                            }
                        }
                    }
                }
            }
            
            ArrayList joined_lines = new ArrayList();
            int max_joins = 10;
            for (int i = 0; i < lines.Count; i++)
            {
                linefeature line1 = (linefeature)lines[i];
                
                if ((line1.join_start == null) && (line1.join_end == null))
                {
                    linefeature new_line = new linefeature(line1.x0, line1.y0, line1.x1, line1.y1);
                    joined_lines.Add(new_line);
                }
                
                // note that we limit the number of itterations
                // to avoid getting trapped in circular joins
                                
                linefeature current_line = line1;
                int l = 0;
                if (line1.join_start == null)
                {
	                while ((l < max_joins) && (current_line.join_end != null))
	                {
	                    current_line = current_line.join_end;
	                    l++;
	                }
	                if (current_line != line1)
	                {
	                    // make a new line which combines these
	                    linefeature new_line = new linefeature(line1.x0, line1.y0, current_line.x1, current_line.y1);
	                    joined_lines.Add(new_line);
	                }
                }
                
                current_line = line1;
                l = 0;
                if (line1.join_end == null)
                {
	                while ((l < max_joins) && (current_line.join_start != null))
	                {
	                    current_line = current_line.join_start;
	                    l++;
	                }
	                if (current_line != line1)
	                {
	                    // make a new line which combines these
	                    linefeature new_line = new linefeature(current_line.x0, current_line.y0, line1.x1, line1.y1);
	                    joined_lines.Add(new_line);
	                }
                }
            }
            
            return(joined_lines);
        }

        /// <summary>
        /// trace along horizontal edges
        /// </summary>
        /// <param name="horizontal_edges">a list of horizontal edge features</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="min_length">the minimum length of line features to be extracted</param>
        /// <param name="search_depth">depth of search used to join edges</param>
        /// <returns>a list of line features</returns>
        public static ArrayList traceHorizontalLines(ArrayList[] horizontal_edges, 
                                                     int image_height,
                                                     int min_length,
                                                     int search_depth)
        {
            ArrayList lines = new ArrayList();
            
            // arrays to store edge positions
            bool[] previous_edges4 = new bool[image_height];
            bool[] previous_edges3 = new bool[image_height];
            bool[] previous_edges2 = new bool[image_height];
            bool[] previous_edges = new bool[image_height];
            bool[] current_edges = new bool[image_height];
            ArrayList[] current_lines = new ArrayList[image_height];
            
            for (int x = 0; x < horizontal_edges.Length; x++)
            {
                // clear the array of current edge positions
                for (int y = 0; y < image_height; y++)
                    current_edges[y] = false;
                    
                
                ArrayList edges = horizontal_edges[x];
                for (int i = 0; i < edges.Count; i++)
                {
                    int y = (int)edges[i];

                    int prev_y = -1;
                    
                    if ((y > -1) && (y < image_height))
                    {                    
	                    if ((previous_edges[y]) || (previous_edges2[y]) || (previous_edges3[y]) || (previous_edges4[y])) 
	                        prev_y = y;
	                    else
	                    {
	                        if (y > 0)
	                            if (previous_edges[y-1]) prev_y = y-1;
	                        if (y < image_height-1)
	                            if (previous_edges[y+1]) prev_y = y+1;

                            if (search_depth > 1)
                            {                                
                                if (prev_y == -1)
                                {
                                    if (y > 1)
                                        if (previous_edges[y-2]) prev_y = y-2;
                                    if (y < image_height-2)
                                        if (previous_edges[y+2]) prev_y = y+2;
                                }
	                        
                                if (prev_y == -1)
                                {
                                    if (y > 0)
                                        if (previous_edges2[y-1]) prev_y = y-1;
                                    if (y < image_height-1)
                                        if (previous_edges2[y+1]) prev_y = y+1;
                                }

                                if (prev_y == -1)
                                {
                                    if (y > 1)
                                        if (previous_edges2[y-2]) prev_y = y-2;
                                    if (y < image_height-2)
                                        if (previous_edges2[y+2]) prev_y = y+2;
                                }

                                if (search_depth > 2)
                                {
                                    if (prev_y == -1)
                                    {
                                        if (y > 0)
                                            if (previous_edges3[y - 1]) prev_y = y - 1;
                                        if (y < image_height - 1)
                                            if (previous_edges3[y + 1]) prev_y = y + 1;
                                    }

                                    if (prev_y == -1)
                                    {
                                        if (y > 1)
                                            if (previous_edges3[y - 2]) prev_y = y - 2;
                                        if (y < image_height - 2)
                                            if (previous_edges3[y + 2]) prev_y = y + 2;
                                    }

                                    if (search_depth > 3)
                                    {
                                        if (prev_y == -1)
                                        {
                                            if (y > 0)
                                                if (previous_edges4[y - 1]) prev_y = y - 1;
                                            if (y < image_height - 1)
                                                if (previous_edges4[y + 1]) prev_y = y + 1;
                                        }

                                        if (prev_y == -1)
                                        {
                                            if (y > 1)
                                                if (previous_edges4[y - 2]) prev_y = y - 2;
                                            if (y < image_height - 2)
                                                if (previous_edges4[y + 2]) prev_y = y + 2;
                                        }

                                        if (prev_y == -1)
                                        {
                                            if (y > 2)
                                                if (previous_edges4[y - 3]) prev_y = y - 3;
                                            if (y < image_height - 3)
                                                if (previous_edges4[y + 3]) prev_y = y + 3;
                                        }

                                        if (prev_y == -1)
                                        {
                                            if (y > 3)
                                                if (previous_edges4[y - 4]) prev_y = y - 4;
                                            if (y < image_height - 4)
                                                if (previous_edges4[y + 4]) prev_y = y + 4;
                                        }
                                    }
                                }
                            }
	                    }
	                    
	                    
	                    if (prev_y > -1)
	                    {
	                        if (current_lines[prev_y] == null)
	                            current_lines[prev_y] = new ArrayList();
	                            
	                        current_lines[prev_y].Add(x);
	                        current_lines[prev_y].Add(y);
	                        
	                        if (prev_y != y)
	                        {
	                            current_lines[y] = current_lines[prev_y];
	                            current_lines[prev_y] = null;
	                        }
	                    }
	                    
	                    current_edges[y] = true;
                    
                    }
                }
                
                // which lines are broken?
                for (int y = 1; y < image_height-1; y++)
                {
                    ArrayList line = current_lines[y];
                    if ((line != null) && ((!current_edges[y]) || (x == horizontal_edges.Length-1)))
                    {                        
                        int line_length = line.Count/2;
                        if (line_length > min_length)
                        {
                            // calc centre of the line
                            float av_start_x = 0;
                            float av_start_y = 0;
                            float av_end_x = 0;
                            float av_end_y = 0;
                            float av_x = 0;
                            float av_y = 0;
                            int hits_start = 0;
                            int hits_end = 0;
                            for (int j = 0; j < line.Count; j += 2)
                            {
                                int xx = (int)line[j];
                                int yy = (int)line[j + 1];
                                av_x += xx;
                                av_y += yy;
                                if (j < line_length)
                                {
                                    av_start_x += xx;
                                    av_start_y += yy;
                                    hits_start++;
                                }
                                else
                                {
                                    av_end_x += xx;
                                    av_end_y += yy;
                                    hits_end++;
                                }
                            }
                            av_x /= line_length;
                            av_y /= line_length;
                            av_start_x /= hits_start;
                            av_start_y /= hits_start;
                            av_end_x /= hits_end;
                            av_end_y /= hits_end;
                            
                            float dx = av_start_x - av_x;
                            float dy = av_start_y - av_y;
                            av_start_x = av_x + (dx*2);
                            av_start_y = av_y + (dy*2);

                            dx = av_end_x - av_x;
                            dy = av_end_y - av_y;
                            av_end_x = av_x + (dx*2);
                            av_end_y = av_y + (dy*2);
                            
                            linefeature new_line = new linefeature(av_start_x, av_start_y, av_end_x, av_end_y);
                            lines.Add(new_line);
                        }
                        current_lines[y] = null;
                    }
                }
                
                // swap arrays
                bool[] temp = previous_edges4;
                previous_edges4 = previous_edges3;
                previous_edges3 = previous_edges2;
                previous_edges2 = previous_edges;
                previous_edges = current_edges;
                current_edges = temp;      
            }
            return(lines);
        }


        /// <summary>
        /// trace along vertical edges
        /// </summary>
        /// <param name="vertical_edges">a list of vertical edge features</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="min_length">the minimum length of line features to be extracted</param>
        /// <param name="search_depth">depth of search used to join edges</param>
        /// <returns>a list of line features</returns>
        public static ArrayList traceVerticalLines(ArrayList[] vertical_edges, 
                                                   int image_width,
                                                   int min_length,
                                                   int search_depth)
        {
            ArrayList lines = new ArrayList();
            
            // arrays to store edge positions
            bool[] previous_edges4 = new bool[image_width];
            bool[] previous_edges3 = new bool[image_width];
            bool[] previous_edges2 = new bool[image_width];
            bool[] previous_edges = new bool[image_width];
            bool[] current_edges = new bool[image_width];
            ArrayList[] current_lines = new ArrayList[image_width];
            
            for (int y = 0; y < vertical_edges.Length; y++)
            {
                // clear the array of current edge positions
                for (int x = 0; x < image_width; x++)
                    current_edges[x] = false;
                    
                
                ArrayList edges = vertical_edges[y];
                for (int i = 0; i < edges.Count; i++)
                {
                    int x = (int)edges[i];
                    
                    int prev_x = -1;
                    
                    if ((x > -1) && (x < image_width))
                    {                    
	                    if ((previous_edges[x]) || (previous_edges2[x]) || (previous_edges3[x]) || (previous_edges4[x])) 
	                        prev_x = x;
	                    else
	                    {
	                        if (x > 0)
	                            if (previous_edges[x-1]) prev_x = x-1;
	                        if (x < image_width-1)
	                            if (previous_edges[x+1]) prev_x = x+1;

                            if (search_depth > 1)
                            {
                                if (prev_x == -1)
                                {
                                    if (x > 1)
                                        if (previous_edges[x - 2]) prev_x = x - 2;
                                    if (x < image_width - 2)
                                        if (previous_edges[x + 2]) prev_x = x + 2;
                                }

                                if (prev_x == -1)
                                {
                                    if (x > 0)
                                        if (previous_edges2[x - 1]) prev_x = x - 1;
                                    if (x < image_width - 1)
                                        if (previous_edges2[x + 1]) prev_x = x + 1;
                                }

                                if (prev_x == -1)
                                {
                                    if (x > 1)
                                        if (previous_edges2[x - 2]) prev_x = x - 2;
                                    if (x < image_width - 2)
                                        if (previous_edges2[x + 2]) prev_x = x + 2;
                                }

                                if (search_depth > 2)
                                {

                                    if (prev_x == -1)
                                    {
                                        if (x > 0)
                                            if (previous_edges3[x - 1]) prev_x = x - 1;
                                        if (x < image_width - 1)
                                            if (previous_edges3[x + 1]) prev_x = x + 1;
                                    }

                                    if (prev_x == -1)
                                    {
                                        if (x > 1)
                                            if (previous_edges3[x - 2]) prev_x = x - 2;
                                        if (x < image_width - 2)
                                            if (previous_edges3[x + 2]) prev_x = x + 2;
                                    }

                                    if (search_depth > 3)
                                    {
                                        if (prev_x == -1)
                                        {
                                            if (x > 0)
                                                if (previous_edges4[x - 1]) prev_x = x - 1;
                                            if (x < image_width - 1)
                                                if (previous_edges4[x + 1]) prev_x = x + 1;
                                        }

                                        if (prev_x == -1)
                                        {
                                            if (x > 1)
                                                if (previous_edges4[x - 2]) prev_x = x - 2;
                                            if (x < image_width - 2)
                                                if (previous_edges4[x + 2]) prev_x = x + 2;
                                        }

                                        if (prev_x == -1)
                                        {
                                            if (x > 2)
                                                if (previous_edges4[x - 3]) prev_x = x - 3;
                                            if (x < image_width - 3)
                                                if (previous_edges4[x + 3]) prev_x = x + 3;
                                        }

                                        if (prev_x == -1)
                                        {
                                            if (x > 3)
                                                if (previous_edges4[x - 4]) prev_x = x - 4;
                                            if (x < image_width - 4)
                                                if (previous_edges4[x + 4]) prev_x = x + 4;
                                        }
                                    }
                                }
                            }
	                    }
	                    if (prev_x > -1)
	                    {
	                        if (current_lines[prev_x] == null)
	                            current_lines[prev_x] = new ArrayList();
	                            
	                        current_lines[prev_x].Add(x);
	                        current_lines[prev_x].Add(y);
	                        
	                        if (prev_x != x)
	                        {
	                            current_lines[x] = current_lines[prev_x];
	                            current_lines[prev_x] = null;
	                        }
	                    }
	                    
	                    current_edges[x] = true;
                    }
                }
                
                // which lines are broken?
                for (int x = 1; x < image_width-1; x++)
                {
                    ArrayList line = current_lines[x];
                    if ((line != null) && ((!current_edges[x]) || (y == vertical_edges.Length-1)))
                    {                        
                        int line_length = line.Count/2;
                        if (line_length > min_length)
                        {
                            // calc centre of the line
                            float av_start_x = 0;
                            float av_start_y = 0;
                            float av_end_x = 0;
                            float av_end_y = 0;
                            float av_x = 0;
                            float av_y = 0;
                            int hits_start = 0;
                            int hits_end = 0;
                            for (int j = 0; j < line.Count; j += 2)
                            {
                                int xx = (int)line[j];
                                int yy = (int)line[j + 1];
                                av_x += xx;
                                av_y += yy;
                                if (j < line_length)
                                {
                                    av_start_x += xx;
                                    av_start_y += yy;
                                    hits_start++;
                                }
                                else
                                {
                                    av_end_x += xx;
                                    av_end_y += yy;
                                    hits_end++;
                                }
                            }
                            av_x /= line_length;
                            av_y /= line_length;
                            av_start_x /= hits_start;
                            av_start_y /= hits_start;
                            av_end_x /= hits_end;
                            av_end_y /= hits_end;
                            
                            float dx = av_start_x - av_x;
                            float dy = av_start_y - av_y;
                            av_start_x = av_x + (dx*2);
                            av_start_y = av_y + (dy*2);

                            dx = av_end_x - av_x;
                            dy = av_end_y - av_y;
                            av_end_x = av_x + (dx*2);
                            av_end_y = av_y + (dy*2);
                            
                            linefeature new_line = new linefeature(av_start_x, av_start_y, av_end_x, av_end_y);
                            lines.Add(new_line);
                        }
                        current_lines[x] = null;
                    }
                }
                
                // swap arrays
                bool[] temp = previous_edges4;
                previous_edges4 = previous_edges3;
                previous_edges3 = previous_edges2;
                previous_edges2 = previous_edges;
                previous_edges = current_edges;
                current_edges = temp;      
            }
            return(lines);
        }

        /// <summary>
        /// detect vertical edges within a binary image
        /// </summary>
		public static ArrayList[] detectVerticalEdges(bool[,] binary_image)
		{
		    ArrayList[] vertical_edges = new ArrayList[binary_image.GetLength(1)]; 
		    
		    for (int y = 0; y < binary_image.GetLength(1); y++)
		    {		    
		        ArrayList detections = new ArrayList();
		        for (int x = 3; x < binary_image.GetLength(0)-3; x++)
		        {
		            if (binary_image[x-1, y] != binary_image[x, y])
		                if (binary_image[x-3, y] == binary_image[x-1, y])
		                    if (binary_image[x-2, y] == binary_image[x-1, y])
		                        if (binary_image[x, y] == binary_image[x+1, y])
		                            if (binary_image[x, y] == binary_image[x+2, y])
		                                detections.Add(x);
		        }
		        vertical_edges[y] = detections;		        
		    }
		    return(vertical_edges);
		}

        /// <summary>
        /// detect vertical edges within a blob image
        /// </summary>
		public static ArrayList[] detectVerticalEdges(float[,] blob_image, float threshold)
		{
		    ArrayList[] vertical_edges = new ArrayList[blob_image.GetLength(1)]; 
		    
		    for (int y = 0; y < blob_image.GetLength(1); y++)
		    {		    
		        ArrayList detections = new ArrayList();
		        for (int x = 2; x < blob_image.GetLength(0)-2; x++)
		        {		            
		            if (blob_image[x-1, y] < threshold)
		            {
		                if (blob_image[x, y] >= threshold)
		                    detections.Add(x);
		            }
		            else
		            {
		                if (blob_image[x, y] < threshold)
		                    detections.Add(x);
		            }
		        }
		        vertical_edges[y] = detections;		        
		    }
		    return(vertical_edges);
		}


        /// <summary>
        /// detect horizontal edges within a binary image
        /// </summary>
		public static ArrayList[] detectHorizontalEdges(bool[,] binary_image)
		{
		    ArrayList[] horizontal_edges = new ArrayList[binary_image.GetLength(0)];
		    
		    for (int x = 0; x < binary_image.GetLength(0); x++)		    
		    {
		        ArrayList detections = new ArrayList();
                for (int y = 3; y < binary_image.GetLength(1)-3; y++)		        
		        {
		            if (binary_image[x, y-1] != binary_image[x, y])
		                if (binary_image[x, y-3] == binary_image[x, y-1])
		                    if (binary_image[x, y-2] == binary_image[x, y-1])
		                        if (binary_image[x, y] == binary_image[x, y + 1])
		                            if (binary_image[x, y] == binary_image[x, y + 2])
		                                detections.Add(y);
		        }
		        horizontal_edges[x] = detections;
		    }
		    return(horizontal_edges);
		}

        /// <summary>
        /// detect horizontal edges within a blob image
        /// </summary>
		public static ArrayList[] detectHorizontalEdges(float[,] blob_image, float threshold)
		{
		    ArrayList[] horizontal_edges = new ArrayList[blob_image.GetLength(0)];
		    
		    for (int x = 0; x < blob_image.GetLength(0); x++)		    
		    {
		        ArrayList detections = new ArrayList();
                for (int y = 2; y < blob_image.GetLength(1)-2; y++)		        
		        {
		            if (blob_image[x, y-1] < threshold)
		            {
		                if (blob_image[x, y] >= threshold)
		                    detections.Add(y);
		            }
		            else
		            {
		                if (blob_image[x, y] < threshold)
		                    detections.Add(y);
		            }
		        }
		        horizontal_edges[x] = detections;
		    }
		    return(horizontal_edges);
		}
		
        /// <summary>
        /// detect vertically oriented edge features within a line grid pattern
        /// </summary>
		public static bool[,] detectVerticalEdges(byte[] mono_image, int width, int height,
		                                          int horizontal_suppression_radius,
		                                          int edge_patch_radius, int edge_threshold)
		{
		    // array which returns the results
		    bool[,] vertical_edges = new bool[width, height];
		
		    // edge threshold should be proportional to the number of pixels being considered
            edge_threshold *= edge_patch_radius;		    
		
		    // this array stores the absolute magnitude of edge responses
		    int[] edge_response = new int[width];
		    
		    // this array stores sliding sums of pixel intensities for each row
		    // of the image
		    int[] sliding_sum = new int[width];
		
		    for (int y = 0; y < height; y++)
		    {
		        int x = 0;
		        
		        // calc sliding sum for this row
		        sliding_sum[0] = mono_image[y * width];
		        for (x = 1; x < width; x++)
		            sliding_sum[x] = sliding_sum[x-1] + mono_image[(y*width)+x];  
		            
		        // calc edge responses
		        for (x = edge_patch_radius; x < width - edge_patch_radius; x++)
		        {
		            // total pixel intensity to the left
		            int left = sliding_sum[x] - sliding_sum[x - edge_patch_radius];
		            
		            // total pixel intensity to the right
		            int right = sliding_sum[x + edge_patch_radius] - sliding_sum[x];
		            
		            // update response
		            edge_response[x] = Math.Abs(left - right);
		            
		            // apply edge threshold
		            if (edge_response[x] < edge_threshold)
		                edge_response[x] = 0;
		        }
		        
		        // perform non-maximal supression
		        x = 0;		        
		        while (x < width)
		        {
		            int response = edge_response[x];
		            if (response > 0)
		            {
		                bool killed = false;
			            int xx = x + 1; // - horizontal_suppression_radius;
			            while ((xx < x + horizontal_suppression_radius) && (!killed))
			            {
			                if (xx < width)
			                {
	                            if (response >= edge_response[xx])
			                    {
			                        edge_response[xx] = 0;
			                    }
			                    else
			                    {
			                        edge_response[x] = 0;
			                        killed = true;
			                    }
			                }
			                xx++;
			            }
		            }
		            x++;
		        }
		        
		        // count the survivors and add them to the result
		        for (x = 0; x < width; x++)
		            if (edge_response[x] > 0)
		                vertical_edges[x, y] = true;
		    }
		    return(vertical_edges);
		}

        /// <summary>
        /// detect horizontally oriented edge features within a line grid pattern
        /// </summary>
		public static bool[,] detectHorizontalEdges(byte[] mono_image, int width, int height,
		                                            int vertical_suppression_radius,
		                                            int edge_patch_radius, int edge_threshold)
		{
		    // array which returns the results
		    bool[,] horizontal_edges = new bool[width, height];
		
		    // edge threshold should be proportional to the number of pixels being considered
            edge_threshold *= edge_patch_radius;		    
		
		    // this array stores the absolute magnitude of edge responses
		    int[] edge_response = new int[height];
		    
		    // this array stores sliding sums of pixel intensities for each row
		    // of the image
		    int[] sliding_sum = new int[height];
		
		    for (int x = 0; x < width; x++)
		    {
		        int y = 0;
		        
		        // calc sliding sum for this row
		        sliding_sum[0] = mono_image[x];
		        for (y = 1; y < height; y++)
		            sliding_sum[y] = sliding_sum[y-1] + mono_image[(y*width)+x];  
		            
		        // calc edge responses
		        for (y = edge_patch_radius; y < height - edge_patch_radius; y++)
		        {
		            // total pixel intensity to the left
		            int above = sliding_sum[y] - sliding_sum[y - edge_patch_radius];
		            
		            // total pixel intensity to the right
		            int below = sliding_sum[y + edge_patch_radius] - sliding_sum[y];
		            
		            // update response
		            edge_response[y] = Math.Abs(above - below);
		            
		            // apply edge threshold
		            if (edge_response[y] < edge_threshold)
		                edge_response[y] = 0;
		        }
		        
		        // perform non-maximal supression
		        y = 0;		        
		        while (y < height)
		        {
		            int response = edge_response[y];
		            if (response > 0)
		            {
		                bool killed = false;
			            int yy = y + 1;
			            while ((yy < y + vertical_suppression_radius) && (!killed))
			            {
			                if (yy < height)
			                {
		                        if (response >= edge_response[yy])
				                {
				                    edge_response[yy] = 0;
				                }
				                else
				                {
				                    edge_response[y] = 0;
				                    killed = true;
				                }
			                }
			                yy++;
			            }
		            }
		            y++;
		        }
		        
		        // count the survivors and add them to the result
		        for (y = 0; y < height; y++)
		            if (edge_response[y] > 0)
		                horizontal_edges[x, y] = true;
		    }
		    return(horizontal_edges);
		}
		
		#endregion
				
		#region "integral image"
        
        /// <summary>
        /// update the integral image, using the given mono bitmap
        /// </summary>
        public static long[,] updateIntegralImage(byte[] bmp, int image_width, int image_height)
        {
            int x, y, p, n = 0;
            
            long[,] Integral = new long[image_width, image_height];
                
            for (y = 1; y < image_height; y++)
            {
                p = 0;
                for (x = 0; x < image_width; x++)
                {
                    p += bmp[n];
                    Integral[x, y] = p + Integral[x, y-1];
                    n++;
                }
            }
            return(Integral);
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public static long getIntegral(long[,] Integral, int tx, int ty, int bx, int by)
        {
            return(Integral[bx,by] + Integral[tx,ty] - (Integral[tx,by] + Integral[bx,ty]));
        }
        
        #endregion

        #region "conversions"

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <returns></returns>
        public static byte[] monoImage(Byte[] img_colour, int img_width, int img_height)
        {
            byte[] mono_image = new byte[img_width * img_height];
            int n=0;

            for (int i = 0; i < img_width * img_height * 3; i += 3)
            {
                int tot = 0;
                for (int col = 0; col < 3; col++)
                {
                    tot += img_colour[i + col];
                }
                mono_image[n] = (Byte)(tot / 3);
                n++;
            }
            return (mono_image);
        }

        #endregion

        #region "rescaling / resampling"

        /// <summary>
        /// sub-sample a colour image 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <returns></returns>
        public static byte[] downSample(byte[] img, int img_width, int img_height,
                                        int new_width, int new_height)
        {
            byte[] new_img = new byte[new_width * new_height * 3];
            int n = 0;

            for (int y = 0; y < new_height; y++)
            {
                int yy = y * img_height / new_height;
                for (int x = 0; x < new_width; x++)
                {
                    int xx = x * img_width / new_width;
                    int n2 = ((yy * img_width) + xx)*3;
                    for (int col = 0; col < 3; col++)
                    {                    
                        new_img[n] = img[n2 + col];
                        n++;
                    }
                }
            }

            return (new_img);
        }

        #endregion

        #region "loading from different formats"

        /// <summary>
		/// load a bitmap file and return a byte array
		/// </summary>
		public static byte[] loadFromBitmap(String filename, int image_width, int image_height, int bytes_per_pixel)
        {
            byte[] bmp = new Byte[image_width * image_height * bytes_per_pixel];

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);
                int n = 54;
                int n2 = 0;
                byte[] data = new byte[(image_width * image_height * bytes_per_pixel) + n];
                binfile.Read(data, 0, (image_width * image_height * bytes_per_pixel) + n);
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        n2 = (((image_height-1-y) * image_width) + x) * 3;
                        for (int c = 0; c < bytes_per_pixel; c++)
                        {
                            bmp[n2+c] = data[n];
                            n++;
                        }                        
                    }
                }
                binfile.Close();
                fp.Close();
            }
            return (bmp);
        }
        

        public static byte[] loadFromPGM(String filename, int image_width, int image_height, int bytes_per_pixel)
        {
            byte[] bmp = new byte[image_width * image_height * bytes_per_pixel];

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);
                int n = 96;
                int n2 = 0;
                byte[] data = new byte[(image_width * image_height) + n];
                binfile.Read(data, 0, (image_width * image_height) + n);
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        for (int c = 0; c < bytes_per_pixel; c++)
                        {
                            bmp[n2] = data[n];
                            n2++;
                        }
                        n++;
                    }
                }
                binfile.Close();
                fp.Close();
            }
            return (bmp);
        }

        #endregion

        #region "line fitting"

        /// <summary>
        /// find the best position for a corner feature by fitting lines
        /// before and after the corner feature as closely as possible
        /// </summary>
        /// <param name="bmp_mono">image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        public static void matchCorner(byte[] bmp_mono, int image_width, int image_height,
                                       int x1, int y1, int x2, int y2, int x3, int y3, 
                                       int corner_search_radius, int line_search_radius,
                                       ref int best_x2, ref int best_y2)
        {
            if (line_search_radius < 1) 
                line_search_radius = 1;
            
            best_x2 = x2;
            best_y2 = y2;
            float max_score = 0;
            for (int x = x2 - corner_search_radius; x <= x2 + corner_search_radius; x++)
            {
                for (int y = y2 - corner_search_radius; y <= y2 + corner_search_radius; y++)
                {
                    float score = linearity(bmp_mono, image_width, image_height,
                                            x1, y1, x, y, line_search_radius) +
                                  linearity(bmp_mono, image_width, image_height,
                                            x3, y3, x, y, line_search_radius);
                    if (score >= max_score)
                    {
                        max_score = score;
                        best_x2 = x;
                        best_y2 = y;
                    }
                }
            }
        }

        /// <summary>
        /// returns a measure of how likely the given points are the ends of a line
        /// used for line fitting from corner features
        /// </summary>
        /// <param name="bmp_mono">mono image data</param>
        /// <param name="image_width">image width</param>
        /// <param name="image_height">image height</param>
        /// <param name="x1">start x coordinate</param>
        /// <param name="y1">start y coordinate</param>
        /// <param name="x2">end x coordinate</param>
        /// <param name="y2">end y coordinate</param>
        /// <param name="search_radius">small search radius within which to inspect pixels at points along the line</param>
        /// <returns></returns>
        public static float linearity(byte[] bmp_mono, int image_width, int image_height,
                                    int x1, int y1, int x2, int y2, int search_radius)
        {
            const int no_of_samples = 25;
            if (x2 < x1)
            {
                int temp_x = x1;
                x1 = x2;
                x2 = temp_x;
            }
            if (y2 < y1)
            {
                int temp_y = y1;
                y1 = y2;
                y2 = temp_y;
            }
        
            int dx = x2 - x1;
            int dy = y2 - y1;
            
            float side1=0, side2=0;
            int hits1=0, hits2=0;
            int step = dx / no_of_samples;
            if (step < 1) step = 1;
            if (dx > dy)
            {
                // horizontal orientation
                for (int x = x1; x < x2; x += step)
                {
                    int y = y1 + ((x-x1)*dy/dx);
                    for (int r = 1; r <= search_radius; r++)
                    {
                        if (y - r > 0)
                        {
                            int n1 = ((y - r) * image_width) + x;
                            side1 += bmp_mono[n1];
                            hits1++;
                        }
                        if ((y + r > 0) && (y + r < image_height))
                        {
                            int n2 = ((y + r) * image_width) + x;
                            side2 += bmp_mono[n2];
                            hits2++;
                        }
                    }
                }
            }
            else
            {
                // vertical orientation
                step = dy / no_of_samples;
                if (step < 1) step = 1;
                for (int y = y1; y < y2; y += step)
                {
                    if ((y > 0) && (y < image_height))
                    {
                        int x = x1 + ((y-y1)*dx/dy);
                        for (int r = 1; r <= search_radius; r++)
                        {
                            if (x - r > 0)
                            {
                                int n1 = (y * image_width) + x - r;
                                side1 += bmp_mono[n1];
                                hits1++;
                            }
                            if ((x + r > 0) && (x + r < image_width))
                            {
                                int n2 = (y * image_width) + x + r;
                                side2 += bmp_mono[n2];
                                hits2++;
                            }
                        }
                    }
                }
            }
            
            if (hits1 > 0) side1 /= hits1;
            if (hits2 > 0) side2 /= hits2;
            return(Math.Abs(side1 - side2));
        }

        #endregion
    }
}
