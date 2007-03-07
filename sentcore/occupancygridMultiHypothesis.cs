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

namespace sentience.core
{
    /// <summary>
    /// occupancy grid cell capable of storing multiple hypotheses
    /// </summary>
    public class occupancygridCellMultiHypothesis
    {
        // a value which is returned by the probability method if no evidence was discovered
        public const int NO_OCCUPANCY_EVIDENCE = 99999999;

        // current best estimate of whether this cell is occupied or not
        // note that unknown cells are simply null pointers
        // This boolean value is convenient for display purposes
        public bool occupied;

        // list of occupancy hypotheses, of type particleGridCell
        public ArrayList Hypothesis;

        /// <summary>
        /// returns the probability of occupancy at this grid cell
        /// warning: this could potentially suffer from path ID rollover problems
        /// </summary>
        /// <param name="path_ID"></param>
        /// <returns>probability value</returns>
        public float GetProbability(particlePose pose)
        {           
            float probabilityLogOdds = 0;
            int hits = 0;

            if (pose.previous_paths != null)
            {
                UInt32 curr_path_ID = UInt32.MaxValue;
                int i = Hypothesis.Count - 1;
                // cycle through the path IDs for this pose            
                for (int p = 0; p < pose.previous_paths.Count; p++)
                {
                    UInt32 path_ID = (UInt32)pose.previous_paths[p];
                    while ((i >= 0) && (curr_path_ID >= path_ID))
                    {
                        particleGridCell h = (particleGridCell)Hypothesis[i];
                        curr_path_ID = h.pose.path_ID;
                        if (curr_path_ID == path_ID)
                            // only use evidence older than the current time 
                            // step to avoid getting into a muddle
                            if (pose.time_step > h.pose.time_step)
                            {
                                probabilityLogOdds += h.probabilityLogOdds;
                                hits++;
                            }
                        i--;
                    }
                }
            }

            if (hits > 0)
            {
                // at the end we convert the total log odds value into a probability
                return (util.LogOddsToProbability(probabilityLogOdds));
            }
            else
                return (NO_OCCUPANCY_EVIDENCE);
        }

        public occupancygridCellMultiHypothesis()
        {
            occupied = false;
            Hypothesis = new ArrayList();
        }
    }

    /// <summary>
    /// two dimensional grid storing multiple occupancy hypotheses
    /// </summary>
    public class occupancygridMultiHypothesis : pos3D
    {
        // a quick lookup table for gaussian values
        private float[] gaussianLookup;

        // the number of cells across
        public int dimension_cells;

        // size of each grid cell in millimetres
        public int cellSize_mm;

        // when localising search a wider area than when mapping
        public int localisation_search_cells = 1;

        // cells of the grid
        occupancygridCellMultiHypothesis[,] cell;

        #region "initialisation"

        /// <summary>
        /// initialise the grid
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        private void init(int dimension_cells, int cellSize_mm)
        {
            this.dimension_cells = dimension_cells;
            this.cellSize_mm = cellSize_mm;
            cell = new occupancygridCellMultiHypothesis[dimension_cells, dimension_cells];

            // make a lookup table for gaussians - saves doing a lot of floating point maths
            gaussianLookup = stereoModel.createHalfGaussianLookup(10);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        public occupancygridMultiHypothesis(int dimension_cells, int cellSize_mm)
            : base(0, 0,0)
        {
            init(dimension_cells, cellSize_mm);
        }

        #endregion

        #region "sensor model"

        /// <summary>
        /// function for vacancy within the sensor model
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        private float vacancyFunction(float fraction, int steps)
        {
            float min_vacancy_probability = 0.0f;
            float max_vacancy_probability = 0.1f;
            float prob = min_vacancy_probability + ((max_vacancy_probability - min_vacancy_probability) *
                         (float)Math.Exp(-(fraction * fraction)));
            return (0.5f - (prob / steps));
        }

        #endregion

        #region "grid update"

        /// <summary>
        /// removes an occupancy hypothesis from a grid cell
        /// </summary>
        /// <param name="hypothesis"></param>
        public void Remove(particleGridCell hypothesis)
        {
            cell[hypothesis.x, hypothesis.y].Hypothesis.Remove(hypothesis);
        }

        /// <summary>
        /// returns the localisation probability
        /// </summary>
        /// <param name="x_cell">x grid coordinate</param>
        /// <param name="y_cell">y grid coordinate</param>
        /// <param name="origin">pose of the robot</param>
        /// <param name="sensorModelProbability">probability value from a specific point in the ray, taken from the sensor model</param>
        /// <returns>log odds probability of there being a match between the ray and the grid</returns>
        private float localiseProbability(int x_cell, int y_cell,
                                          particlePose origin,
                                          float sensorModelProbability)
        {
            float value = 0;

            // localise using this grid cell
            // first get the existing probability value at this cell
            float occ = cell[x_cell, y_cell].GetProbability(origin);

            if (occ != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
            {
                // combine the results
                float prob2 = ((sensorModelProbability * occ) + ((1.0f - sensorModelProbability) * (1.0f - occ)));

                // localisation matching score, expressed as log odds
                value = util.LogOdds(prob2);
            }
            return (value);
        }

        /// <summary>
        /// inserts the given ray into the grid
        /// There are three components to the sensor model used here:
        /// two areas determining the probably vacant area and one for 
        /// the probably occupied space
        /// </summary>
        /// <param name="ray">ray object to be inserted into the grid</param>
        /// <param name="origin">the pose of the robot</param>
        /// <param name="sensormodel">the sensor model to be used</param>
        /// <param name="leftcam_x">x position of the left camera in millimetres</param>
        /// <param name="leftcam_y">y position of the left camera in millimetres</param>
        /// <param name="rightcam_x">x position of the right camera in millimetres</param>
        /// <param name="rightcam_y">y position of the right camera in millimetres</param>
        public float Insert(evidenceRay ray, particlePose origin,
                            rayModelLookup sensormodel_lookup,
                            float leftcam_x, float leftcam_y,
                            float rightcam_x, float rightcam_y)
        {
            // some constants to aid readability
            const int OCCUPIED_SENSORMODEL = 0;
            const int VACANT_SENSORMODEL_LEFT_CAMERA = 1;
            const int VACANT_SENSORMODEL_RIGHT_CAMERA = 2;

            const int X_AXIS = 0;
            const int Y_AXIS = 1;

            // which lookup table to use
            int sensormodel_index = (int)(ray.disparity * 2);

            float xdist_mm=0, ydist_mm=0, zdist_mm=0, x=0, y=0, z=0;
            float occupied_dx = 0, occupied_dy = 0, occupied_dz = 0;
            float intersect_x = 0, intersect_y = 0, intersect_z = 0;
            float centre_prob=0, prob = 0; // probability values at the centre axis and outside
            float matchingScore = 0;  // total localisation matching score
            int rayWidth = 0;         // widest point in the ray in cells
            int widest_point;         // widest point index
            particleGridCell hypothesis;

            // ray width at the fattest point in cells
            rayWidth = (int)(ray.width / (cellSize_mm * 2));
            if (rayWidth < 1) rayWidth = 1;

            int max_dimension_cells = dimension_cells - rayWidth;

            for (int modelcomponent = OCCUPIED_SENSORMODEL; modelcomponent <= VACANT_SENSORMODEL_RIGHT_CAMERA; modelcomponent++)
            {
                switch (modelcomponent)
                {
                    case OCCUPIED_SENSORMODEL:
                        {
                            // distance between the beginning and end of the probably
                            // occupied area
                            occupied_dx = ray.vertices[1].x - ray.vertices[0].x;
                            occupied_dy = ray.vertices[1].y - ray.vertices[0].y;
                            occupied_dz = ray.vertices[1].z - ray.vertices[0].z;
                            intersect_x = ray.vertices[0].x + (occupied_dx * ray.fattestPoint);
                            intersect_y = ray.vertices[0].y + (occupied_dx * ray.fattestPoint);
                            intersect_z = ray.vertices[0].z + (occupied_dz * ray.fattestPoint);

                            xdist_mm = occupied_dx;
                            ydist_mm = occupied_dy;
                            zdist_mm = occupied_dz;
                            x = ray.vertices[0].x;
                            y = ray.vertices[0].y;
                            z = ray.vertices[0].z;
                            break;
                        }
                    case VACANT_SENSORMODEL_LEFT_CAMERA:
                        {
                            // distance between the left camera and the left side of
                            // the probably occupied area of the sensor model                            
                            xdist_mm = intersect_x - leftcam_x;
                            ydist_mm = intersect_y - leftcam_y;
                            zdist_mm = intersect_z - ray.observedFrom.z;
                            x = leftcam_x;
                            y = leftcam_y;
                            z = ray.observedFrom.z;
                            break;
                        }
                    case VACANT_SENSORMODEL_RIGHT_CAMERA:
                        {
                            // distance between the right camera and the right side of
                            // the probably occupied area of the sensor model
                            xdist_mm = intersect_x - rightcam_x;
                            ydist_mm = intersect_y - rightcam_y;
                            zdist_mm = intersect_z - ray.observedFrom.z;
                            x = rightcam_x;
                            y = rightcam_y;
                            z = ray.observedFrom.z;
                            break;
                        }
                }

                // which is the longest axis ?
                int longest_axis = X_AXIS;
                float longest = Math.Abs(xdist_mm);
                if (Math.Abs(ydist_mm) > longest)
                {
                    // y has the longest length
                    longest = Math.Abs(ydist_mm);
                    longest_axis = Y_AXIS;
                }

                // ensure that the vacancy model does not overlap
                // the probably occupied area
                // This is crude and could potentially leave a gap
                if (modelcomponent != OCCUPIED_SENSORMODEL)
                    longest -= ray.width;

                int steps = (int)(longest / cellSize_mm);
                if (modelcomponent == OCCUPIED_SENSORMODEL)
                    widest_point = (int)(ray.fattestPoint * steps / ray.length);
                else
                    widest_point = steps;
                float x_incr = xdist_mm / steps;
                float y_incr = ydist_mm / steps;
                float z_incr = zdist_mm / steps;

                int i = 0;
                while (i < steps)
                {
                    // calculate the width of the ray in cells at this point
                    // using a diamond shape ray model
                    int ray_wdth = 0;
                    if (i < widest_point)
                        ray_wdth = i * rayWidth / widest_point;
                    else
                        ray_wdth = (steps - i + widest_point) * rayWidth / (steps - widest_point);

                    x += x_incr;
                    y += y_incr;
                    z += z_incr;
                    int x_cell = (int)Math.Round(x / (float)cellSize_mm);
                    if ((x_cell > ray_wdth) && (x_cell < max_dimension_cells))
                    {
                        int y_cell = (int)Math.Round(y / (float)cellSize_mm);
                        if ((y_cell > ray_wdth) && (y_cell < max_dimension_cells))
                        {
                            int x_cell2 = x_cell;
                            int y_cell2 = y_cell;

                            // get the probability at this point 
                            // for the central axis of the ray using the inverse sensor model
                            if (modelcomponent == OCCUPIED_SENSORMODEL)
                                centre_prob = sensormodel_lookup.probability[sensormodel_index, i];
                            else
                                // calculate the probability from the vacancy model
                                centre_prob = vacancyFunction(i / (float)steps, steps);


                            // width of the ray
                            for (int width = -ray_wdth; width <= ray_wdth; width++)
                            {
                                // adjust the x or y cell position depending upon the 
                                // deviation from the main axis of the ray
                                if (longest_axis == Y_AXIS)
                                    x_cell2 = x_cell + width;
                                else
                                    y_cell2 = y_cell + width;

                                // probability at the central axis
                                prob = centre_prob;

                                // probabilities are symmetrical about the axis of the ray
                                // this multiplier implements a gaussian distribution around the centre
                                if (width != 0)
                                    prob *= gaussianLookup[Math.Abs(width) * 9 / ray_wdth];

                                if (cell[x_cell2, y_cell2] == null)
                                {
                                    // generate a grid cell if necessary
                                    cell[x_cell2, y_cell2] = new occupancygridCellMultiHypothesis();
                                }
                                else
                                {
                                    // only localise using occupancy, not vacancy
                                    if (modelcomponent == OCCUPIED_SENSORMODEL)
                                    {
                                        // note that we search within a small radius to give a
                                        // better chance of finding some occupied cells
                                        if (longest_axis == X_AXIS)
                                        {
                                            for (int y_cell3 = y_cell2 - localisation_search_cells; y_cell3 <= y_cell2 + localisation_search_cells; y_cell3++)
                                                if ((y_cell3 > -1) && (y_cell3 < dimension_cells))
                                                    matchingScore += localiseProbability(x_cell2, y_cell3, origin, prob);
                                        }
                                        if (longest_axis == Y_AXIS)
                                        {
                                            for (int x_cell3 = x_cell2 - localisation_search_cells; x_cell3 <= x_cell2 + localisation_search_cells; x_cell3++)
                                                if ((x_cell3>-1) && (x_cell3 < dimension_cells))
                                                    matchingScore += localiseProbability(x_cell3, y_cell2, origin, prob);
                                        }
                                    }
                                }

                                // add a new hypothesis to this grid coordinate
                                // note that this is also added to the original pose
                                hypothesis = new particleGridCell(x_cell2, y_cell2, prob, origin);
                                cell[x_cell2, y_cell2].Hypothesis.Add(hypothesis);
                                origin.observed_grid_cells.Add(hypothesis);
                            }
                        }
                        else i = steps;
                    }
                    else i = steps;
                    i++;
                }
            }

            return (matchingScore);
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show the grid map as an image
        /// </summary>
        /// <param name="img">bitmap image</param>
        /// <param name="width">width in pixels</param>
        /// <param name="height">height in pixels</param>
        public void Show(Byte[] img, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                int cell_y = y * (dimension_cells-1) / height;
                for (int x = 0; x < width; x++)
                {
                    int cell_x = x * (dimension_cells - 1) / width;
                    int n = ((y * width) + x) * 3;

                    for (int c = 0; c < 3; c++)
                    {
                        if (cell[cell_x, cell_y] == null)
                        {
                            img[n + c] = (Byte)255;
                        }
                        else
                        {
                            if (cell[cell_x,cell_y].occupied)
                                img[n + c] = (Byte)0;
                            else
                                img[n + c] = (Byte)200;
                        }
                    }
                }
            }
        }

        #endregion
    }

}
