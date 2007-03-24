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

            for (int z = 0; z < Hypothesis.Length; z++)
            {
                float variance = 0;
                float probLO = GetProbability(pose, x, y, z, true, colour, ref variance);
                if (probLO != NO_OCCUPANCY_EVIDENCE)
                {
                    probabilityLogOdds += probLO;
                    for (int col = 0; col < 3; col++)
                        mean_colour[col] += colour[col];
                    mean_variance += variance;
                    hits++;
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
        /// returns the distilled probability for the given cell
        /// </summary>
        /// <param name="z">z coordinate (height) of the cell</param>
        /// <param name="returnLogOdds">whether to return log odds or probability</param>
        /// <param name="colour">returns the distilled colour of this cell</param>
        /// <returns></returns>
        /*
        public float GetProbabilityDistilled(int z, bool returnLogOdds,
                                             float[] colour)
        {
            float prob;

            if (distilled !=null)
            {
                if (distilled[z] != null)
                {
                    prob = distilled[z].probabilityLogOdds;
                    if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                    {
                        if (!returnLogOdds)
                            prob = util.LogOddsToProbability(prob);

                        for (int col = 0; col < 3; col++)
                            colour[col] = distilled[z].colour[col];
                    }
                }
                else
                    prob = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;
            }
            else
                prob = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;

            return(prob);
        }
        */
        /// <summary>
        /// returns the distilled probability for the entire column
        /// </summary>
        /// <param name="colour">mean colour</param>
        /// <returns></returns>
        /*
        public float GetProbabilityDistilled(float[] colour)
        {
            float prob = 0;
            int hits = 0;
            for (int col = 0; col < 3; col++)
                colour[col] = 0;

            if (distilled != null)
            {
                for (int z = 0; z < distilled.Length; z++)
                {
                    if (distilled[z] != null)
                    {
                        if (distilled[z].probabilityLogOdds != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            prob += distilled[z].probabilityLogOdds;
                            for (int col = 0; col < 3; col++)
                                colour[col] += distilled[z].colour[col];
                            hits++;
                        }
                    }
                }
                if (hits > 0)
                {
                    prob = util.LogOddsToProbability(prob);
                    for (int col = 0; col < 3; col++)
                        colour[col] /= hits;
                }
                else
                    prob = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;
            }
            else
                prob = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;

            return (prob);
        }
        */

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

            for (int col = 0; col < 3; col++)
                colour[col] = 0;

            // first retrieve any distilled occupancy value
            if (distilled != null)
                if (distilled[z] != null)
                {
                    probabilityLogOdds += distilled[z].probabilityLogOdds;
                    for (int col = 0; col < 3; col++)
                        colour[col] += distilled[z].colour[col];
                    hits++;
                }

            min_level = new float[3];
            max_level = new float[3];

            // and now get the data for any additional non-distilled particles
            if (Hypothesis[z] != null)
            {
                if (pose.previous_paths != null)
                {

                    // cycle through the previous paths for this pose            
                    for (int p = pose.previous_paths.Count-1; p >= 0; p--)
                    {
                        particlePath path = (particlePath)pose.previous_paths[p];
                        if (path != null)
                        {
                            if (path.Enabled)
                            {
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
                            else
                            {
                                // remove a dead path, because it has been distilled
                                pose.previous_paths.RemoveAt(p);
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

        #region "distillation"

        // distilled occupancy and colour data
        // this is the total value of all particles for the best available pose summed together
        private particleGridCellBase[] distilled;

        /// <summary>
        /// distill an individual grid particle
        /// </summary>
        /// <param name="hypothesis">the grid particle to be distilled</param>
        public void Distill(particleGridCell hypothesis)
        {
            int z = hypothesis.z;

            // create the distilled array
            if (distilled == null)
                distilled = new particleGridCellBase[Hypothesis.Length];

            bool initialised = false;
            if (distilled[z] == null)
            {
                // create a new distilled particle
                distilled[z] = new particleGridCellBase();
                distilled[z].colour = new Byte[3];
                initialised = true;
            }

            // update an existing distilled value
            distilled[z].probabilityLogOdds += hypothesis.probabilityLogOdds;

            // and update the distilled colour value
            for (int col = 0; col < 3; col++)
            {
                if (initialised)
                    distilled[z].colour[col] = hypothesis.colour[col];
                else
                    distilled[z].colour[col] = (Byte)((hypothesis.colour[col] +
                                                   distilled[z].colour[col]) / 2);
            }
        }

        /// <summary>
        /// distill particles for this pose down into single values
        /// </summary>
        /// <param name="pose">the best pose from which to create the probability</param>
        /// <param name="x">x coordinate in cells</param>
        /// <param name="y">y coordinate in cells</param>
        /// <param name="updateExistingValues">update existing distilled values or create new ones</param>
        /*
        public void Distill(particlePose pose, int x, int y, 
                            bool updateExistingValues)
        {
            float[] colour = new float[3];
            float mean_variance = 0;

            if ((distilled == null) || (!updateExistingValues))
                distilled = new particleGridCellBase[Hypothesis.Length];

            for (int z = 0; z < Hypothesis.Length; z++)
            {                
                // get the distilled probability
                float probLogOdds = GetProbability(pose, x, y, z, true, colour, ref mean_variance);
                if (probLogOdds != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                {
                    if (distilled[z] == null)
                    {
                        // create a new distilled value
                        distilled[z] = new particleGridCellBase();
                        distilled[z].probabilityLogOdds = probLogOdds;

                        // and update the distilled colour value
                        distilled[z].colour = new Byte[3];
                        for (int col = 0; col < 3; col++)
                            distilled[z].colour[col] = (Byte)colour[col];
                    }
                    else
                    {
                        // update an existing distilled value
                        distilled[z].probabilityLogOdds += probLogOdds;

                        // and update the distilled colour value
                        for (int col = 0; col < 3; col++)
                            distilled[z].colour[col] = (Byte)((colour[col] + distilled[z].colour[col]) / 2);
                    }
                }
            }
            isDistilled = true;
        }
        */

        /// <summary>
        /// sets distilled probability values
        /// This is used when loading a grid from file
        /// </summary>
        /// <param name="distilled"></param>
        public void SetDistilledValues(particleGridCellBase[] distilled)
        {
            this.distilled = distilled;
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
