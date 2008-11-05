/*
    A multiple hypothesis occupancy grid, based upon distributed particle SLAM
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
using System.Collections.Generic;
using System.Diagnostics;
using CenterSpace.Free;
using sluggish.utilities;

namespace sentience.core
{
    /// <summary>
    /// occupancy grid cell capable of storing multiple hypotheses
    /// There is one grid cell of this type per X,Y coordinate within the occupancy grid.
    /// The grid cell object stores the vertical positions of particles so
    /// that no 3D information is lost
    /// </summary>
    public sealed class occupancygridCellMultiHypothesis
    {
        // a value which is returned by the probability method 
        // if no occupancy evidence was discovered
        public const int NO_OCCUPANCY_EVIDENCE = 99999999;

        // list of occupancy hypotheses, of type particleGridCell
        // each hypothesis list corresponds to a particular vertical (z) cell index
        private List<particleGridCell>[] Hypothesis;

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
                    particleGridCell h = Hypothesis[vertical_index][i];
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
                for (int i = garbage.Length-1; i >= 0; i--)
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
                Hypothesis[vertical_index] = new List<particleGridCell>();
            Hypothesis[vertical_index].Add(h);
        }

        #endregion

        #region "probability calculations"

        // arrays used to calculuate the colour variance
        private float[] min_level = new float[3];
        private float[] max_level = new float[3];
        private float[] temp_colour = new float[3];

        /// <summary>
        /// return the probability of occupancy for the entire cell
        /// </summary>
        /// <param name="pose">pose from which the observation was made</param>
        /// <returns>probability as log odds</returns>
        public float GetProbability(particlePose pose, int x, int y,
                                    float[] mean_colour, ref float mean_variance)
        {
            float probabilityLogOdds = 0;
            int hits = 0;
            mean_variance = 0;

            for (int col = 0; col < 3; col++)
                mean_colour[col] = 0;

            for (int z = Hypothesis.Length - 1; z >= 0; z--)
            {
                float variance = 0;
                float probLO = GetProbability(pose, x, y, z, true, temp_colour, ref variance);
                if (probLO != NO_OCCUPANCY_EVIDENCE)
                {
                    probabilityLogOdds += probLO;
                    for (int col = 2; col >= 0; col--)
                        mean_colour[col] += temp_colour[col];
                    mean_variance += variance;
                    hits++;
                }
            }
            if (hits > 0)
            {
                mean_variance /= hits;
                for (int col = 2; col >= 0; col--)
                    mean_colour[col] /= hits;
            }
            return (probabilities.LogOddsToProbability(probabilityLogOdds));
        }

        /// <summary>
        /// returns the probability of occupancy at this grid cell at the given vertical (z) coordinate
        /// warning: this could potentially suffer from path ID or time step rollover problems
        /// </summary>
        /// <param name="pose">the robot pose from which this cell was observed</param>
        /// <param name="x">x grid coordinate</param>
        /// <param name="y">y grid coordinate</param>
        /// <param name="z">z grid coordinate</param>
        /// <param name="returnLogOdds">return the probability value expressed as log odds</param>
        /// <param name="colour">average colour of this grid cell</param>
        /// <param name="mean_variance">average colour variance of this grid cell</param>
        /// <returns>probability value</returns>
        public unsafe float GetProbability(particlePose pose, 
                                           int x, 
                                           int y, 
                                           int z, 
                                           bool returnLogOdds,
                                           float[] colour, 
                                           ref float mean_variance)
        {
            int col, p, i, hits = 0;
            float probabilityLogOdds = 0;
			byte level;
			List<particleGridCell> map_cache_observations;
			List<particlePath> previous_paths;
			particleGridCell h;
			particlePath path;
			uint time_step;
            mean_variance = 1;

			// pin some arrays to avoid range checking
			fixed (float* unsafe_colour = colour)
			{
			    fixed (float* unsafe_min_level = min_level)
			    {			
			        fixed (float* unsafe_max_level = max_level)
			        {
			            for (col = 2; col >= 0; col--)
			                unsafe_colour[col] = 0;

			            // first retrieve any distilled occupancy value
			            if (distilled != null)
			                if (distilled[z] != null)
			                {
			                    probabilityLogOdds += distilled[z].probabilityLogOdds;
			                    for (col = 2; col >= 0; col--)
			                        unsafe_colour[col] += distilled[z].colour[col];
			                    hits++;
			                }

			            // and now get the data for any additional non-distilled particles
			            if ((Hypothesis[z] != null) && (pose != null))
			            {
                            previous_paths = pose.previous_paths;                            
			                if (previous_paths != null)
			                {
                                time_step = pose.time_step;

			                    // cycle through the previous paths for this pose            
			                    for (p = previous_paths.Count-1; p >= 0; p--)
			                    {
			                        path = previous_paths[p];
			                        if (path != null)
			                        {
			                            if (path.Enabled)
			                            {
			                                // do any hypotheses for this path exist at this location ?
			                                map_cache_observations = path.GetHypotheses(x, y, z);
			                                if (map_cache_observations != null)
			                                {
			                                    for (i = map_cache_observations.Count - 1; i >= 0; i--)
			                                    {
			                                        h = map_cache_observations[i];
			                                        if (h.Enabled)
			                                        {
			                                            // only use evidence older than the current time 
			                                            // step to avoid getting into a muddle
			                                            if (time_step != h.pose.time_step)
			                                            {
			                                                probabilityLogOdds += h.probabilityLogOdds;

			                                                // update mean colour and variance
			                                                for (col = 2; col >= 0; col--)
			                                                {
			                                                    level = h.colour[col];
			                                                    unsafe_colour[col] += level;

			                                                    // update colour variance
			                                                    if (hits > 0)
			                                                    {
			                                                        if (level < unsafe_min_level[col])
			                                                            unsafe_min_level[col] = level;

			                                                        if (level > unsafe_max_level[col])
			                                                            unsafe_max_level[col] = level;
			                                                    }
			                                                    else
			                                                    {
			                                                        unsafe_min_level[col] = level;
			                                                        unsafe_max_level[col] = level;
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
					}
				}
			}

            if (hits > 0)
            {
                // calculate mean colour variance
                mean_variance = 0;
                for (col = 0; col < 3; col++)
                    mean_variance += max_level[col] - min_level[col];
                //mean_variance /= (3 * 255.0f);
                mean_variance *= 0.001307189542483660130718954248366f;

                // calculate the average colour
                for (col = 2; col >= 0; col--)
                    colour[col] /= hits;

                // at the end we convert the total log odds value into a probability
                if (returnLogOdds)
                    return (probabilityLogOdds);
                else
                    return (probabilities.LogOddsToProbability(probabilityLogOdds));
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
                distilled[z].colour = new byte[3];
                initialised = true;
            }

            // update an existing distilled value
            distilled[z].probabilityLogOdds += hypothesis.probabilityLogOdds;

            // and update the distilled colour value
            for (int col = 2; col >= 0; col--)
            {
                if (initialised)
                    distilled[z].colour[col] = hypothesis.colour[col];
                else
                    distilled[z].colour[col] = (byte)((hypothesis.colour[col] +
                                                   distilled[z].colour[col]) / 2);
            }
        }

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

        #region "constructors/initialisation"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="vertical_dimension_cells">vertical dimension of the grid in cells</param>
        public occupancygridCellMultiHypothesis(int vertical_dimension_cells)
        {
            //occupied = false;
            Hypothesis = new List<particleGridCell>[vertical_dimension_cells];
            garbage = new bool[vertical_dimension_cells];
        }

        #endregion
    }
}