/*
    A multiple hypothesis occupancy grid, based upon distributed particle SLAM
    Copyright (C) 2000-2007 Bob Mottram
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CenterSpace.Free;

namespace sentience.core
{
    /// <summary>
    /// occupancy grid cell capable of storing multiple hypotheses
    /// There is one grid cell of this type per X,Y coordinate within the occupancy grid.
    /// The grid cell object stores the vertical positions of particles so
    /// that no 3D information is lost
    /// </summary>
    public class occupancygridCellMultiHypothesis
    {
        // a value which is returned by the probability method 
        // if no occupancy evidence was discovered
        public const int NO_OCCUPANCY_EVIDENCE = 99999999;

        // list of occupancy hypotheses, of type particleGridCell
        // each hypothesis list corresponds to a particular vertical (z) cell index
        private ArrayList[] Hypothesis;

        #region "garbage collection"

        // the number of garbage hypotheses accumulated within this grid cell
        public int garbage_entries = 0;

        // array of flags, one for each vertical cell, indicating whether
        // any trash has accumulated at that position
        public bool[] garbage;

        /// <summary>
        /// any old iron...
        /// </summary>
        /// <param name="vertical_index">the z position at which to check for garbage</param>
        /// <returns>the amount of garbage collected</returns>
        public int GarbageCollect(int vertical_index)
        {
            int collected_items = 0;

            if (Hypothesis[vertical_index] != null)
            {
                int i = Hypothesis[vertical_index].Count - 1;
                while ((i >= 0) && (garbage_entries > 0))
                {
                    particleGridCell h = (particleGridCell)Hypothesis[vertical_index][i];
                    if (!h.Enabled) // has this hypothesis been marked for deletion?
                    {
                        Hypothesis[vertical_index].RemoveAt(i);
                        garbage_entries--;
                        collected_items++;
                    }
                    i--;
                }
                // there is no longer any garbage at this vertical index
                garbage[vertical_index] = false;

                // no hypotheses exist, so remove the list to save on memory usage
                if (Hypothesis[vertical_index].Count == 0)
                    Hypothesis[vertical_index] = null;
            }
            return (collected_items);
        }

        /// <summary>
        /// collect garbage for all vertical indices
        /// </summary>
        /// <returns>the amount of garbage collected</returns>
        public int GarbageCollect()
        {
            int collected_items = 0;

            if (garbage_entries > 0)
            {
                for (int i = 0; i < garbage.Length; i++)
                {
                    if (garbage[i] == true)
                        collected_items += GarbageCollect(i);
                }
            }
            return (collected_items);
        }


        #endregion

        #region "hypothesis updates"

        /// <summary>
        /// add a new occupancy hypothesis to the list
        /// for this grid cell
        /// </summary>
        /// <param name="h">the hypothesis to be added to this grid cell</param>
        public void AddHypothesis(particleGridCell h)
        {
            int vertical_index = h.z;
            if (Hypothesis[vertical_index] == null)
                Hypothesis[vertical_index] = new ArrayList();
            Hypothesis[vertical_index].Add(h);
        }

        #endregion

        #region "probability calculations"

        /// <summary>
        /// return the probability of occupancy for the entire cell
        /// </summary>
        /// <param name="pose"></param>
        /// <returns>probability as log odds</returns>
        public float GetProbability(particlePose pose, int x, int y,
                                    float[] mean_colour, ref float mean_variance)
        {
            float probabilityLogOdds = 0;
            float[] colour = new float[3];
            int hits = 0;
            mean_variance = 0;

            for (int col = 0; col < 3; col++)
                mean_colour[col] = 0;

            for (int i = 0; i < Hypothesis.Length; i++)
            {
                if (Hypothesis[i] != null)
                {
                    float variance = 0;
                    float probLO = GetProbability(pose, x, y, i, true, colour, ref variance);
                    if (probLO != NO_OCCUPANCY_EVIDENCE)
                    {
                        probabilityLogOdds += probLO;
                        for (int col = 0; col < 3; col++)
                            mean_colour[col] += colour[col];
                        mean_variance += variance;
                        hits++;
                    }
                }
            }
            if (hits > 0)
            {
                mean_variance /= hits;
                for (int col = 0; col < 3; col++)
                    mean_colour[col] /= hits;
            }
            return (util.LogOddsToProbability(probabilityLogOdds));
        }

        /// <summary>
        /// returns the probability of occupancy at this grid cell at the given vertical (z) coordinate
        /// warning: this could potentially suffer from path ID or time step rollover problems
        /// </summary>
        /// <param name="path_ID"></param>
        /// <returns>probability value</returns>
        public float GetProbability(particlePose pose, int x, int y, int z, bool returnLogOdds,
                                    float[] colour, ref float mean_variance)
        {
            int hits = 0;
            float probabilityLogOdds = 0;
            float[] min_level = null;
            float[] max_level = null;
            mean_variance = 1;

            if (Hypothesis[z] != null)
            {
                if (pose.previous_paths != null)
                {
                    min_level = new float[3];
                    max_level = new float[3];
                    for (int col = 0; col < 3; col++)
                        colour[col] = 0;

                    // cycle through the path IDs for this pose            
                    for (int p = 0; p < pose.previous_paths.Count; p++)
                    {
                        particlePath path = (particlePath)pose.previous_paths[p];

                        // do any hypotheses for this path exist at this location ?
                        ArrayList map_cache_observations = path.GetHypotheses(x, y, z);
                        if (map_cache_observations != null)
                        {
                            for (int i = 0; i < map_cache_observations.Count; i++)
                            {
                                particleGridCell h = (particleGridCell)map_cache_observations[i];
                                if (h.Enabled)
                                {
                                    // only use evidence older than the current time 
                                    // step to avoid getting into a muddle
                                    if (pose.time_step > h.pose.time_step)
                                    {
                                        probabilityLogOdds += h.probabilityLogOdds;

                                        // update mean colour and variance
                                        for (int col = 0; col < 3; col++)
                                        {
                                            Byte level = h.colour[col];
                                            colour[col] += level;

                                            // update colour variance
                                            if (hits > 0)
                                            {
                                                if (level < min_level[col])
                                                    min_level[col] = level;

                                                if (level > max_level[col])
                                                    max_level[col] = level;
                                            }
                                            else
                                            {
                                                min_level[col] = level;
                                                max_level[col] = level;
                                            }
                                        }

                                        hits++;
                                    }
                                }
                            }
                        }
                    }


                }
            }

            if (hits > 0)
            {
                // calculate mean colour variance                
                mean_variance = 0;
                for (int col = 0; col < 3; col++)
                    mean_variance += max_level[col] - min_level[col];
                mean_variance /= (3 * 255.0f);

                // calculate the average colour
                for (int col = 0; col < 3; col++)
                    colour[col] /= hits;

                // at the end we convert the total log odds value into a probability
                if (returnLogOdds)
                    return (probabilityLogOdds);
                else
                    return (util.LogOddsToProbability(probabilityLogOdds));
            }
            else
                return (NO_OCCUPANCY_EVIDENCE);
        }


        #endregion

        #region "initialisation"

        public occupancygridCellMultiHypothesis(int vertical_dimension_cells)
        {
            //occupied = false;
            Hypothesis = new ArrayList[vertical_dimension_cells];
            garbage = new bool[vertical_dimension_cells];
        }

        #endregion
    }
}
