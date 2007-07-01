/*
    FAST corner detector
    C# version adapted from original C code by Edward Rosten 
    http://mi.eng.cam.ac.uk/~er258/work/fast.html
    Copyright (C) 2006 Bob Mottram
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

namespace sluggish.imageprocessing.FASTcorners
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
            public float radius;
            
            #region "directionality"
            
            // gives an indication of the orientation of the corner
            // with respect to other corner features within the neighbourhood
            public float direction_x;
            public float direction_y;
            public float direction_magnitude;
            
            
            /// <summary>
	        /// does the orientation of this corner intersect with another corner?
	        /// </summary>
	        /// <param name="other_corner">the other corner feature</param>
	        /// <param name="ix">x coordinate of the intersection point</param>
	        /// <param name="iy">y coordinate of the intersection point</param>
	        /// <returns>true if an intersection exists</returns>
            public bool intersects(FASTcorner other_corner,
                                   ref float ix, ref float iy)
            {
                bool result = sluggish.utilities.geometry.intersection(x,y,x+direction_x,y+direction_y,
                                           other_corner.x, other_corner.y, other_corner.x+other_corner.direction_x, other_corner.y+other_corner.direction_y,
                                           ref ix, ref iy);
                return(result);
            }
           
            
            /// <summary>
            /// update the directionality of this corner
            /// </summary>            
            /// <param name="corners">array of detected corner features</param>
            /// <param name="radius">neighbourhood radius</param>
            public void updateDirection(FASTcorner[] corners, int radius)
            {
                float tot_x = 0, tot_y = 0;
                int hits = 0;

                neighbours = null;                
                for (int i = 0; i < corners.Length; i+=5)
                {
                    FASTcorner corner = corners[i];
                    if (corner != this)
                    {
                        int dx = corner.x - x;
                        if (dx > -radius)
                        {
                            if (dx < radius)
	                        {
	                            int dy = corner.y - y;
	                            if (dy > -radius)
	                            {
	                                if (dy < radius)
	                                {
	                                    tot_x += dx;
	                                    tot_y += dy;
	                                    hits++;
	                                    Add(corner);
	                                }
	                            }
	                        }
	                    }
                    }
                }
                if (hits > 0)
                {
                    direction_x = tot_x / hits;
                    direction_y = tot_y / hits;
                    direction_magnitude = (float)Math.Sqrt((direction_x*direction_x)+(direction_y*direction_y));
                }
                else
                {
                    direction_x = 0;
                    direction_y = 0;
                    direction_magnitude = 0;
                }
            }
            
            #endregion
            
            #region "neighbouring features
            
            // neighbouring corner features
            public ArrayList neighbours;
            
            // local neighbourhood radius
            public int neighbour_radius;
            
            public void Add(FASTcorner neighbour)
            {
                if (neighbours == null)
                    neighbours = new ArrayList();
                    
                neighbours.Add(neighbour);
            }
            
            #endregion

            #region "constructors"

            public FASTcorner(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
            
            #endregion

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
	
}
