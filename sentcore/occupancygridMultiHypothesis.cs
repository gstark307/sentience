/*
    A multiple hypothesis occupancy grid, based upon distributed particle SLAM
    Copyright (C) 2000-2008 Bob Mottram
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using sluggish.utilities;
using CenterSpace.Free;

namespace sentience.core
{
    /// <summary>
    /// two dimensional grid storing multiple occupancy hypotheses
    /// </summary>
    public sealed class occupancygridMultiHypothesis : occupancygridBase
    {
        #region "variables"

        // random number generator
        private MersenneTwister rnd = new MersenneTwister(100);

        // list grid cells which need to be cleared of garbage
        private List<occupancygridCellMultiHypothesis> garbage;

        // the total number of hypotheses (particles) within the grid
        public int total_valid_hypotheses = 0;

        // the total amount of garbage awaiting collection
        public int total_garbage_hypotheses = 0;

        // cells of the grid
        occupancygridCellMultiHypothesis[][] cell;

        // indicates areas of the grid which are navigable
        public bool[][] navigable_space;
        
        #endregion

        #region "constructors/initialisation"

        /// <summary>
        /// initialise the grid
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="dimension_cells_vertical">number of cells in the vertical axis</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        /// <param name="localisationRadius_mm">radius in millimetres used during localisation within the grid</param>
        /// <param name="maxMappingRange_mm">maximum range in millimetres used during mapping</param>
        /// <param name="vacancyWeighting">weighting applied to the vacancy model in the range 0.0 - 1.0</param>
        private void init(int dimension_cells, 
                          int dimension_cells_vertical, 
                          int cellSize_mm, 
                          int localisationRadius_mm, 
                          int maxMappingRange_mm, 
                          float vacancyWeighting)
        {
            this.dimension_cells = dimension_cells;
            this.dimension_cells_vertical = dimension_cells_vertical;
            this.cellSize_mm = cellSize_mm;
            this.localisation_search_cells = localisationRadius_mm / cellSize_mm;
            this.max_mapping_range_cells = maxMappingRange_mm / cellSize_mm;
            this.vacancy_weighting = vacancyWeighting;
            cell = new occupancygridCellMultiHypothesis[dimension_cells][];
            navigable_space = new bool[dimension_cells][];
            for (int i = 0; i < cell.Length; i++)
            {
                cell[i] = new occupancygridCellMultiHypothesis[dimension_cells];
                navigable_space[i] = new bool[dimension_cells];
            }

            // make a lookup table for gaussians - saves doing a lot of floating point maths
            gaussianLookup = stereoModel.createHalfGaussianLookup(10);

            garbage = new List<occupancygridCellMultiHypothesis>();

            // create a lookup table for log odds calculations, to avoid having
            // to run slow Log calculations at runtime
            LogOdds = probabilities.CreateLogOddsLookup(LOG_ODDS_LOOKUP_LEVELS);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="dimension_cells_vertical">number of cells in the vertical axis</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        /// <param name="localisationRadius_mm">radius in millimetres used during localisation within the grid</param>
        /// <param name="maxMappingRange_mm">maximum range in millimetres used during mapping</param>
        /// <param name="vacancyWeighting">weighting applied to the vacancy model in the range 0.0 - 1.0</param>
        public occupancygridMultiHypothesis(int dimension_cells, 
                                            int dimension_cells_vertical, 
                                            int cellSize_mm, 
                                            int localisationRadius_mm, 
                                            int maxMappingRange_mm, 
                                            float vacancyWeighting)
        {
            init(dimension_cells, dimension_cells_vertical, cellSize_mm, localisationRadius_mm, maxMappingRange_mm, vacancyWeighting);
        }

        #endregion

        #region "grid colour variance"

        /// <summary>
        /// returns the average colour variance for the entire grid
        /// </summary>
        /// <param name="pose">best available robot pose hypothesis</param>
        /// <returns>colour variance value for the occupancy grid</returns>
        public float GetMeanColourVariance(particlePose pose)
        {
            float[] mean_colour = new float[3];
            float total_variance = 0;
            int hits = 0;

            for (int cell_y = 0; cell_y < dimension_cells; cell_y++)
            {
                for (int cell_x = 0; cell_x < dimension_cells; cell_x++)
                {
                    if (cell[cell_x][cell_y] != null)
                    {
                        float mean_variance = 0;
                        float prob = cell[cell_x][cell_y].GetProbability(pose, cell_x, cell_y,
                                                                         mean_colour, 
                                                                         ref mean_variance);
                        if (prob > 0.5)
                        {
                            total_variance += mean_variance;
                            hits++;
                        }
                    }
                }
            }
            if (hits > 0) total_variance /= hits;
            return (total_variance);
        }

        #endregion

        #region "removing hypotheses from the grid"

        /// <summary>
        /// removes an occupancy hypothesis from a grid cell
        /// </summary>
        /// <param name="hypothesis">the hypothesis to be removed from the grid</param>
        public void Remove(particleGridCell hypothesis)
        {
            occupancygridCellMultiHypothesis c = cell[hypothesis.x][hypothesis.y];

            // add this to the list of garbage to be collected
            if (c.garbage_entries == 0)                             
                garbage.Add(c);

            c.garbage[hypothesis.z] = true;

            // increment the heap of garbage
            c.garbage_entries++;
            total_garbage_hypotheses++;

            // mark this hypothesis as rubbish
            hypothesis.Enabled = false;
            total_valid_hypotheses--;
        }

        #endregion

        #region "updating the navigable space within the grid"

        /// <summary>
        /// updates the navigable space
        /// </summary>
        /// <param name="pose">best pose from which the occupancy value is calculated</param>
        /// <param name="x">x coordinate in cells</param>
        /// <param name="y">y coordinate in cells</param>
        private void updateNavigableSpace(particlePose pose, int x, int y)
        {
            if (x < 1) x = 1;
            if (y < 1) y = 1;

            float[] mean_colour = new float[3];
            float mean_variance = 0;
            float prob = cell[x][y].GetProbability(pose, x, y,
                                                   mean_colour, 
                                                   ref mean_variance);
            bool state = false;
            if (prob < 0.5f) state = true;
            navigable_space[x][y] = state;
            navigable_space[x - 1][y] = state;
            navigable_space[x - 1][y - 1] = state;
            navigable_space[x][y - 1] = state;
        }

        #endregion

        #region "distillation"

        /// <summary>
        /// distill this grid particle
        /// </summary>
        /// <param name="hypothesis"></param>
        public void Distill(particleGridCell hypothesis)
        {
            occupancygridCellMultiHypothesis c = cell[hypothesis.x][hypothesis.y];
            c.Distill(hypothesis);
            Remove(hypothesis);

            // occasionally update the navigable space
            if (rnd.Next(100) < 5)
                updateNavigableSpace(hypothesis.pose, hypothesis.x, hypothesis.y);
        }

        #endregion

        #region "garbage collection"

        /// <summary>
        /// collect occupancy hypotheses which are no longer active
        /// </summary>
        public void GarbageCollect()
        {
            int max = garbage.Count-1;
            for (int i = max; i >= 0; i--)
            {
                int index = i;

                occupancygridCellMultiHypothesis c = garbage[index];
                if (c.garbage_entries > 0)
                    total_garbage_hypotheses -= c.GarbageCollect();

                // if the garbage has been cleared remove the entry
                if (c.garbage_entries == 0)
                    garbage.RemoveAt(index);
            }
        }

        #endregion

        #region "calculating the matching probability"

        /// <summary>
        /// returns the localisation probability
        /// </summary>
        /// <param name="x_cell">x grid coordinate</param>
        /// <param name="y_cell">y grid coordinate</param>
        /// <param name="origin">pose of the robot</param>
        /// <param name="sensormodel_probability">probability value from a specific point in the ray, taken from the sensor model</param>
        /// <param name="colour">colour of the localisation ray</param>
        /// <returns>log odds probability of there being a match between the ray and the grid</returns>
        private float matchingProbability(
            int x_cell, int y_cell, int z_cell,
            particlePose origin,
            float sensormodel_probability,
            byte[] colour)
        {
            float prob_log_odds = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;
            float colour_variance = 0;

            // localise using this grid cell
            // first get the existing probability value at this cell
            float[] existing_colour = new float[3];

            float existing_probability =
                    cell[x_cell][y_cell].GetProbability(origin, x_cell, y_cell, z_cell,
                                                        false, existing_colour, ref colour_variance);

            if (existing_probability != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
            {
                // combine the occupancy probabilities
                float occupancy_probability = 
                    ((sensormodel_probability * existing_probability) +
                    ((1.0f - sensormodel_probability) * (1.0f - existing_probability)));

                // get the colour difference between the map and the observation
                float colour_difference = getColourDifference(colour, existing_colour);

                // turn the colour difference into a probability
                float colour_probability = 1.0f  - colour_difference;

                // localisation matching probability, expressed as log odds
                prob_log_odds = LogOdds[(int)(occupancy_probability * colour_probability * LOG_ODDS_LOOKUP_LEVELS)];
            }
            return (prob_log_odds);
        }

        #endregion

        #region "throwing rays into the grid"

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
        /// <param name="localiseOnly">if true does not add any mapping particles (pure localisation)</param>
        /// <returns>matching probability, expressed as log odds</returns>
        public float Insert(evidenceRay ray, 
                            particlePose origin,
                            rayModelLookup sensormodel_lookup,
                            pos3D left_camera_location, 
                            pos3D right_camera_location,
                            bool localiseOnly)
        {
            // some constants to aid readability
            const int OCCUPIED_SENSORMODEL = 0;
            const int VACANT_SENSORMODEL_LEFT_CAMERA = 1;
            const int VACANT_SENSORMODEL_RIGHT_CAMERA = 2;

            const int X_AXIS = 0;
            const int Y_AXIS = 1;

            // which disparity index in the lookup table to use
            // we multiply by 2 because the lookup is in half pixel steps
            int sensormodel_index = (int)Math.Round(ray.disparity * 2);
            
            // the initial models are blank, so just default to the one disparity pixel model
            bool small_disparity_value = false;
            if (sensormodel_index < 2)
            {
                sensormodel_index = 2;
                small_disparity_value = true;
            }

            // beyond a certain disparity the ray model for occupied space
            // is always only going to be only a single grid cell
            if (sensormodel_index >= sensormodel_lookup.probability.GetLength(0))
                sensormodel_index = sensormodel_lookup.probability.GetLength(0) - 1;

            float xdist_mm=0, ydist_mm=0, zdist_mm=0, xx_mm=0, yy_mm=0, zz_mm=0;
            float occupied_dx = 0, occupied_dy = 0, occupied_dz = 0;
            float intersect_x = 0, intersect_y = 0, intersect_z = 0;
            float centre_prob = 0, prob = 0, prob_localisation = 0; // probability values at the centre axis and outside
            float matchingScore = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;  // total localisation matching score
            int rayWidth = 0;         // widest point in the ray in cells
            int widest_point;         // widest point index
            int step_size = 1;
            particleGridCell hypothesis;

            // ray width at the fattest point in cells
            rayWidth = (int)Math.Round(ray.width / (cellSize_mm * 2));

            // calculate the centre position of the grid in millimetres
            int half_grid_width_mm = dimension_cells * cellSize_mm / 2;
            //int half_grid_width_vertical_mm = dimension_cells_vertical * cellSize_mm / 2;
            int grid_centre_x_mm = (int)(x - half_grid_width_mm);
            int grid_centre_y_mm = (int)(y - half_grid_width_mm);
            int grid_centre_z_mm = (int)z;

            int max_dimension_cells = dimension_cells - rayWidth;

            // in turbo mode only use a single vacancy ray
            int max_modelcomponent = VACANT_SENSORMODEL_RIGHT_CAMERA;
            if (TurboMode) max_modelcomponent = VACANT_SENSORMODEL_LEFT_CAMERA;

			float[][] sensormodel_lookup_probability = sensormodel_lookup.probability;
			
            // consider each of the three parts of the sensor model
            for (int modelcomponent = OCCUPIED_SENSORMODEL; modelcomponent <= max_modelcomponent; modelcomponent++)
            {
                // the range from the cameras from which insertion of data begins
                // for vacancy rays this will be zero, but will be non-zero for the occupancy area
                int startingRange = 0;

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

                            // begin insertion at the beginning of the 
                            // probably occupied area
                            xx_mm = ray.vertices[0].x;
                            yy_mm = ray.vertices[0].y;
                            zz_mm = ray.vertices[0].z;
                            break;
                        }
                    case VACANT_SENSORMODEL_LEFT_CAMERA:
                        {
                            // distance between the left camera and the left side of
                            // the probably occupied area of the sensor model                            
                            xdist_mm = intersect_x - left_camera_location.x;
                            ydist_mm = intersect_y - left_camera_location.y;
                            zdist_mm = intersect_z - left_camera_location.z;

                            // begin insertion from the left camera position
                            xx_mm = left_camera_location.x;
                            yy_mm = left_camera_location.y;
                            zz_mm = left_camera_location.z;
                            step_size = 2;
                            break;
                        }
                    case VACANT_SENSORMODEL_RIGHT_CAMERA:
                        {
                            // distance between the right camera and the right side of
                            // the probably occupied area of the sensor model
                            xdist_mm = intersect_x - right_camera_location.x;
                            ydist_mm = intersect_y - right_camera_location.y;
                            zdist_mm = intersect_z - right_camera_location.z;

                            // begin insertion from the right camera position
                            xx_mm = right_camera_location.x;
                            yy_mm = right_camera_location.y;
                            zz_mm = right_camera_location.z;
                            step_size = 2;
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
                if (steps < 1) steps = 1;

                // calculate the range from the cameras to the start of the ray in grid cells
                if (modelcomponent == OCCUPIED_SENSORMODEL)
                {
                    if (longest_axis == Y_AXIS)
                        startingRange = (int)Math.Abs((ray.vertices[0].y - ray.observedFrom.y) / cellSize_mm);
                    else
                        startingRange = (int)Math.Abs((ray.vertices[0].x - ray.observedFrom.x) / cellSize_mm);
                }

                // what is the widest point of the ray in cells
                if (modelcomponent == OCCUPIED_SENSORMODEL)
                    widest_point = (int)(ray.fattestPoint * steps / ray.length);
                else
                    widest_point = steps;

                // calculate increment values in millimetres
                float x_incr_mm = xdist_mm / steps;
                float y_incr_mm = ydist_mm / steps;
                float z_incr_mm = zdist_mm / steps;

                // step through the ray, one grid cell at a time
                int grid_step = 0;
                while (grid_step < steps)
                {
                    // is this position inside the maximum mapping range
                    bool withinMappingRange = true;
                    if (grid_step + startingRange > max_mapping_range_cells)
                    {
                        withinMappingRange = false;
                        if ((grid_step==0) && (modelcomponent == OCCUPIED_SENSORMODEL))
                        {
                            grid_step = steps;
                            modelcomponent = 9999;
                        }
                    }

                    // calculate the width of the ray in cells at this point
                    // using a diamond shape ray model
                    int ray_wdth = 0;
                    if (rayWidth > 0)
                    {
                        if (grid_step < widest_point)
                            ray_wdth = grid_step * rayWidth / widest_point;
                        else
                        {
                            if (!small_disparity_value)
                                // most disparity values tail off to some point in the distance
                                ray_wdth = (steps - grid_step + widest_point) * rayWidth / (steps - widest_point);
                            else
                                // for very small disparity values the ray model has an infinite tail
                                // and maintains its width after the widest point
                                ray_wdth = rayWidth; 
                        }
                    }

                    // localisation rays are wider, to enable a more effective matching score
                    // which is not too narrowly focussed and brittle
                    int ray_wdth_localisation = ray_wdth + localisation_search_cells;

                    xx_mm += x_incr_mm;
                    yy_mm += y_incr_mm;
                    zz_mm += z_incr_mm;
                    // convert the x millimetre position into a grid cell position
                    int x_cell = (int)Math.Round((xx_mm - grid_centre_x_mm) / (float)cellSize_mm);
                    if ((x_cell > ray_wdth_localisation) && (x_cell < dimension_cells - ray_wdth_localisation))
                    {
                        // convert the y millimetre position into a grid cell position
                        int y_cell = (int)Math.Round((yy_mm - grid_centre_y_mm) / (float)cellSize_mm);
                        if ((y_cell > ray_wdth_localisation) && (y_cell < dimension_cells - ray_wdth_localisation))
                        {
                            // convert the z millimetre position into a grid cell position
                            int z_cell = (int)Math.Round((zz_mm - grid_centre_z_mm) / (float)cellSize_mm);
                            if ((z_cell >= 0) && (z_cell < dimension_cells_vertical))
                            {

                                int x_cell2 = x_cell;
                                int y_cell2 = y_cell;

                                // get the probability at this point 
                                // for the central axis of the ray using the inverse sensor model
                                if (modelcomponent == OCCUPIED_SENSORMODEL)
                                    centre_prob = 0.5f + (sensormodel_lookup_probability[sensormodel_index][grid_step] * 0.5f);
                                else
                                    // calculate the probability from the vacancy model
                                    centre_prob = vacancyFunction(grid_step / (float)steps, steps);


                                // width of the localisation ray
                                for (int width = -ray_wdth_localisation; width <= ray_wdth_localisation; width++)
                                {
                                    // is the width currently inside the mapping area of the ray ?
                                    bool isInsideMappingRayWidth = false;
                                    if ((width >= -ray_wdth) && (width <= ray_wdth))
                                        isInsideMappingRayWidth = true;

                                    // adjust the x or y cell position depending upon the 
                                    // deviation from the main axis of the ray
                                    if (longest_axis == Y_AXIS)
                                        x_cell2 = x_cell + width;
                                    else
                                        y_cell2 = y_cell + width;

                                    // probability at the central axis
                                    prob = centre_prob;
                                    prob_localisation = centre_prob;

                                    // probabilities are symmetrical about the axis of the ray
                                    // this multiplier implements a gaussian distribution around the centre
                                    if (width != 0) // don't bother if this is the centre of the ray
                                    {
                                        // the probability used for wide localisation rays
                                        prob_localisation *= gaussianLookup[Math.Abs(width) * 9 / ray_wdth_localisation];

                                        // the probability used for narrower mapping rays
                                        if (isInsideMappingRayWidth)
                                            prob *= gaussianLookup[Math.Abs(width) * 9 / ray_wdth];
                                    }

                                    if ((cell[x_cell2][y_cell2] != null) && (withinMappingRange))
                                    {
                                        // only localise using occupancy, not vacancy
                                        if (modelcomponent == OCCUPIED_SENSORMODEL)
                                        {
                                            // update the matching score, by combining the probability
                                            // of the grid cell with the probability from the localisation ray
                                            float score = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;
                                            if (longest_axis == X_AXIS)
                                                score = matchingProbability(x_cell2, y_cell2, z_cell, origin, prob_localisation, ray.colour);

                                            if (longest_axis == Y_AXIS)
                                                score = matchingProbability(x_cell2, y_cell2, z_cell, origin, prob_localisation, ray.colour);

                                            if (score != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                            {
                                                if (matchingScore != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                                    matchingScore += score;
                                                else
                                                    matchingScore = score;
                                            }
                                        }
                                    }

                                    if ((isInsideMappingRayWidth) && 
                                        (withinMappingRange) &&
                                        (!localiseOnly))
                                    {
                                        // add a new hypothesis to this grid coordinate
                                        // note that this is also added to the original pose
                                        hypothesis = new particleGridCell(x_cell2, y_cell2, z_cell, 
                                                                          prob, origin,
                                                                          ray.colour);
                                        if (origin.AddHypothesis(hypothesis, max_mapping_range_cells, dimension_cells, dimension_cells_vertical))
                                        {
                                            // generate a grid cell if necessary
                                            if (cell[x_cell2][y_cell2] == null)
                                                cell[x_cell2][y_cell2] = new occupancygridCellMultiHypothesis(dimension_cells_vertical);

                                            cell[x_cell2][y_cell2].AddHypothesis(hypothesis);                                            
                                            total_valid_hypotheses++;
                                        }
                                    }
                                }
                            }
                            else grid_step = steps;  // its the end of the ray, break out of the loop
                        }
                        else grid_step = steps;  // its the end of the ray, break out of the loop
                    }
                    else grid_step = steps;  // time to bail out chaps!
                    grid_step += step_size;
                }
            }

            return (matchingScore);
        }

        #endregion

        #region "display functions"

        // types of view of the occupancy grid returned by the Show method
        public const int VIEW_ABOVE = 0;
        public const int VIEW_LEFT_SIDE = 1;
        public const int VIEW_RIGHT_SIDE = 2;
        public const int VIEW_NAVIGABLE_SPACE = 3;

        /// <summary>
        /// display the occupancy grid
        /// </summary>
        /// <param name="view_type">the angle from which to view the grid</param>
        /// <param name="img">image within which to insert data</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="pose">the best available robot pose hypothesis</param>
        /// <param name="colour">show grid cell colour, or just occupancy values</param>
        /// <param name="scalegrid">show a grid overlay which gives some idea of scale</param>
        public void Show(int view_type, 
                         byte[] img, 
                         int width, 
                         int height, 
                         particlePose pose, 
                         bool colour, 
                         bool scalegrid)
        {
            switch(view_type)
            {
                case VIEW_ABOVE:
                    {
                        if (!colour)
                            Show(img, width, height, pose);
                        else
                            ShowColour(img, width, height, pose);

                        if (scalegrid)
                        {
                            int r = 200;
                            int g = 200;
                            int b = 255;

                            // draw a grid to give an indication of scale
                            // where the grid resolution is one metre
                            float dimension_mm = cellSize_mm * dimension_cells;

                            for (float x = 0; x < dimension_mm; x += 1000)
                            {
                                int xx = (int)(x * width / dimension_mm);
                                drawing.drawLine(img, width, height, xx, 0, xx, height - 1, r, g, b, 0, false);
                            }
                            for (float y = 0; y < dimension_mm; y += 1000)
                            {
                                int yy = (int)(y * height / dimension_mm);
                                drawing.drawLine(img, width, height, 0, yy, width - 1, yy, r, g, b, 0, false);
                            }
                        }

                        break;
                    }
                case VIEW_LEFT_SIDE:
                    {
                        ShowSide(img, width, height, pose, true);
                        break;
                    }
                case VIEW_RIGHT_SIDE:
                    {
                        ShowSide(img, width, height, pose, false);
                        break;
                    }
                case VIEW_NAVIGABLE_SPACE:
                    {
                        ShowNavigable(img, width, height);
                        break;
                    }
            }

        }

        /// <summary>
        /// show an occupancy grid from one side
        /// </summary>
        /// <param name="img">image in which to insert the data</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="pose">best available robot pose hypothesis</param>
        /// <param name="leftSide">view from the left side or the right</param>
        private void ShowSide(byte[] img, 
                              int width, 
                              int height, 
                              particlePose pose, 
                              bool leftSide)
        {
            float[] mean_colour = new float[3];

            // clear the image
            for (int i = 0; i < width * height * 3; i++)
                img[i] = (byte)255;

            // show the left or right half of the grid
            int start_x = 0;
            int end_x = dimension_cells / 2;
            if (!leftSide)
            {
                start_x = dimension_cells / 2;
                end_x = dimension_cells;
            }

            for (int cell_x = start_x; cell_x < end_x; cell_x++)
            {
                for (int cell_y = 0; cell_y < dimension_cells; cell_y++)
                {
                    if (cell[cell_x][cell_y] != null)
                    {
                        // what's the probability of there being a vertical structure here?
                        float mean_variance = 0;
                        float prob = cell[cell_x][cell_y].GetProbability(pose, cell_x, cell_y,
                                                                       mean_colour, ref mean_variance);

                        if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            if (prob > 0.45f)
                            {
                                // show this vertical grid section
                                occupancygridCellMultiHypothesis c = cell[cell_x][cell_y];
                                for (int cell_z = 0; cell_z < dimension_cells_vertical; cell_z++)
                                {
                                    mean_variance = 0;
                                    prob = c.GetProbability(pose, cell_x, cell_y, cell_z, false, mean_colour, ref mean_variance);

                                    if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                    {
                                        if ((prob > 0.0f) && (mean_variance < 0.5f))
                                        {
                                            int multiplier = 2;
                                            int x = cell_y * width / dimension_cells;
                                            int y = (cell_z * height * multiplier / dimension_cells_vertical);
                                            if ((y >= 0) && (y < height - multiplier-2))
                                            {
                                                for (int yy = y; yy <= y + multiplier+2; yy++)
                                                {
                                                    int n = ((yy * width) + x) * 3;
                                                    for (int col = 0; col < 3; col++)
                                                        img[n + 2 - col] = (byte)mean_colour[col];
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// show the grid map as an image
        /// </summary>
        /// <param name="img">bitmap image</param>
        /// <param name="width">width in pixels</param>
        /// <param name="height">height in pixels</param>
        /// <param name="pose">best pose hypothesis from which the map is observed</param>
        private void Show(byte[] img, 
                          int width, 
                          int height, 
                          particlePose pose)
        {
            float[] mean_colour = new float[3];

            for (int y = 0; y < height; y++)
            {
                // get the y cell coordinate within the grid
                int cell_y = y * (dimension_cells-1) / height;

                for (int x = 0; x < width; x++)
                {
                    // get the x cell coordinate within the grid
                    int cell_x = x * (dimension_cells - 1) / width;

                    int n = ((y * width) + x) * 3;

                    if (cell[cell_x][cell_y] == null)
                    {
                        // terra incognita
                        for (int c = 0; c < 3; c++)
                            img[n + c] = (byte)255; 
                    }
                    else
                    {
                        // get the probability for this vertical column
                        float mean_variance = 0;
                        float prob = cell[cell_x][cell_y].GetProbability(pose, cell_x, cell_y,
                                                                         mean_colour, ref mean_variance);

                        if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            for (int c = 0; c < 3; c++)
                                if (prob > 0.5f)
                                {
                                    if (prob > 0.7f)
                                        img[n + c] = (byte)0;  // occupied
                                    else
                                        img[n + c] = (byte)100;  // occupied
                                }
                                else
                                {
                                    if (prob < 0.3f)
                                        img[n + c] = (byte)230;  // vacant
                                    else
                                        img[n + c] = (byte)200;  // vacant
                                }
                        }
                        else
                        {
                            for (int c = 0; c < 3; c++)
                                img[n + c] = (byte)255; // terra incognita
                        }
                    }
                }
            }
        }

        /// <summary>
        /// show navigable area
        /// </summary>
        /// <param name="img">image in which to insert the data</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        private void ShowNavigable(byte[] img, 
                                   int width, 
                                   int height)
        {
            // clear the image
            for (int i = 0; i < img.Length; i++)
                img[i] = 0;

            if (navigable_space != null)
            {
                for (int y = 0; y < height; y++)
                {
                    int cell_y = y * (dimension_cells - 1) / height;
                    for (int x = 0; x < width; x++)
                    {
                        int cell_x = x * (dimension_cells - 1) / width;

                        int n = ((y * width) + x) * 3;

                        if (navigable_space[cell_x][cell_y])
                            for (int c = 0; c < 3; c++)
                                img[n + c] = (byte)255;
                    }
                }
            }
        }


        /// <summary>
        /// show a colour occupancy grid
        /// </summary>
        /// <param name="img">image in which to insert the data</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="pose">best available robot pose hypothesis</param>
        private void ShowColour(byte[] img, 
                                int width, 
                                int height, 
                                particlePose pose)
        {
            float[] mean_colour = new float[3];

            for (int y = 0; y < height; y++)
            {
                int cell_y = y * (dimension_cells - 1) / height;
                for (int x = 0; x < width; x++)
                {
                    int cell_x = x * (dimension_cells - 1) / width;

                    int n = ((y * width) + x) * 3;

                    if (cell[cell_x][cell_y] == null)
                    {
                        for (int c = 0; c < 3; c++)
                            img[n + c] = (byte)255; // terra incognita
                    }
                    else
                    {
                        float mean_variance = 0;
                        float prob = cell[cell_x][cell_y].GetProbability(pose, cell_x, cell_y,
                                                                         mean_colour, ref mean_variance);

                        if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            if ((mean_variance < 0.7f) || (prob < 0.5f))
                            {
                                for (int c = 0; c < 3; c++)
                                    if (prob > 0.6f)
                                    {
                                        img[n + 2 - c] = (byte)mean_colour[c];  // occupied
                                    }
                                    else
                                    {
                                        if (prob < 0.3f)
                                            img[n + c] = (byte)200;
                                        else
                                            img[n + c] = (byte)255;
                                    }
                            }
                            else
                            {
                                for (int c = 0; c < 3; c++)
                                    img[n + c] = (byte)255;
                            }
                        }
                        else
                        {
                            for (int c = 0; c < 3; c++)
                                img[n + c] = (byte)255; // terra incognita
                        }
                    }
                }
            }
        }

        #endregion

        #region "saving and loading"

        /// <summary>
        /// save the entire grid to file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="pose">best available pose</param>
        public void Save(string filename, 
                         particlePose pose)
        {
            FileStream fp = new FileStream(filename, FileMode.Create);
            BinaryWriter binfile = new BinaryWriter(fp);
            Save(binfile, pose);
            binfile.Close();
            fp.Close();
        }

        /// <summary>
        /// save the entire grid to a single file
        /// </summary>
        /// <param name="binfile"></param>
        /// <param name="pose"></param>
        public void Save(BinaryWriter binfile, 
                         particlePose pose)
        {
            SaveTile(binfile, pose, dimension_cells / 2, dimension_cells / 2, dimension_cells);
        }

        /// <summary>
        /// save the entire grid as a byte array, suitable for subsequent compression
        /// </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        public byte[] Save(particlePose pose)
        {
            return(SaveTile(pose, dimension_cells / 2, dimension_cells / 2, dimension_cells));
        }

        /// <summary>
        /// save the occupancy grid data to file as a tile
        /// it is expected that multiple tiles will be saved per grid
        /// </summary>
        /// <param name="binfile">file to write to</param>
        /// <param name="pose">best available pose</param>
        /// <param name="centre_x">centre of the tile in grid cells</param>
        /// <param name="centre_y">centre of the tile in grid cells</param>
        /// <param name="width_cells">width of the tile in grid cells</param>
        public void SaveTile(BinaryWriter binfile,
                             particlePose pose, 
                             int centre_x, 
                             int centre_y, 
                             int width_cells)
        {
            // write the whole thing to disk in one go
            binfile.Write(SaveTile(pose, centre_x, centre_y, width_cells));
        }

        /// <summary>
        /// save the occupancy grid data to file as a tile
        /// it is expected that multiple tiles will be saved per grid
        /// This returns a byte array, which may subsequently be 
        /// compressed as a zip file for extra storage efficiency
        /// </summary>
        /// <param name="pose">best available pose</param>
        /// <param name="centre_x">centre of the tile in grid cells</param>
        /// <param name="centre_y">centre of the tile in grid cells</param>
        /// <param name="width_cells">width of the tile in grid cells</param>
        /// <returns>byte array containing the data</returns>
        public byte[] SaveTile(particlePose pose,
                               int centre_x, 
                               int centre_y,
                               int width_cells)
        {
            ArrayList data = new ArrayList();

            int half_width_cells = width_cells / 2;

            int tx = centre_x + half_width_cells;
            int ty = centre_y + half_width_cells;
            int bx = centre_x - half_width_cells;
            int by = centre_y - half_width_cells;

            // get the bounding region within which there are actice grid cells
            for (int x = centre_x - half_width_cells; x <= centre_x + half_width_cells; x++)
            {
                if ((x >= 0) && (x < dimension_cells))
                {
                    for (int y = centre_y - half_width_cells; y <= centre_y + half_width_cells; y++)
                    {
                        if ((y >= 0) && (y < dimension_cells))
                        {
                            if (cell[x][y] != null)
                            {
                                if (x < tx) tx = x;
                                if (y < ty) ty = y;
                                if (x > bx) bx = x;
                                if (y > by) by = y;
                            }
                        }
                    }
                }
            }
            bx++;
            by++;

            // write the bounding box dimensions to file
            byte[] intbytes = BitConverter.GetBytes(tx);
            for (int i = 0; i < intbytes.Length; i++)
                data.Add(intbytes[i]);
            intbytes = BitConverter.GetBytes(ty);
            for (int i = 0; i < intbytes.Length; i++)
                data.Add(intbytes[i]);
            intbytes = BitConverter.GetBytes(bx);
            for (int i = 0; i < intbytes.Length; i++)
                data.Add(intbytes[i]);
            intbytes = BitConverter.GetBytes(by);
            for (int i = 0; i < intbytes.Length; i++)
                data.Add(intbytes[i]);

            // create a binary index
            int w1 = bx - tx;
            int w2 = by - ty;
            bool[] binary_index = new bool[w1 * w2];

            int n = 0;
            int occupied_cells = 0;
            for (int y = ty; y < by; y++)
            {
                for (int x = tx; x < bx; x++)
                {
                    if (cell[x][y] != null)
                    {
                        occupied_cells++;
                        binary_index[n] = true;
                    }
                    n++;
                }
            }

            // convert the binary index to a byte array for later storage
            byte[] indexbytes = ArrayConversions.ToByteArray(binary_index);
            for (int i = 0; i < indexbytes.Length; i++)
                data.Add(indexbytes[i]);            

            // dummy variables needed by GetProbability
            float[] colour = new float[3];

            if (occupied_cells > 0)
            {
                float[] occupancy = new float[occupied_cells * dimension_cells_vertical];
                byte[] colourData = new byte[occupied_cells * dimension_cells_vertical * 3];
                float mean_variance = 0;

                n = 0;
                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        if (cell[x][y] != null)
                        {
                            for (int z = 0; z < dimension_cells_vertical; z++)
                            {
                                float prob = cell[x][y].GetProbability(pose,x, y, z, 
                                                                       true, colour, ref mean_variance);
                                int index = (n * dimension_cells_vertical) + z;
                                occupancy[index] = prob;
                                if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                    for (int col = 0; col < 3; col++)
                                        colourData[(index * 3) + col] = (byte)colour[col];
                            }
                            n++;
                        }
                    }
                }

                // store the occupancy and colour data as byte arrays
                byte[] occupancybytes = ArrayConversions.ToByteArray(occupancy);
                for (int i = 0; i < occupancybytes.Length; i++)
                    data.Add(occupancybytes[i]);
                for (int i = 0; i < colourData.Length; i++)
                    data.Add(colourData[i]);            
            }
            byte[] result = (byte[])data.ToArray(typeof(byte));
            return(result);
        }


        /// <summary>
        /// load an entire grid from a single file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(string filename)
        {
            FileStream fp = new FileStream(filename, FileMode.Open);
            BinaryReader binfile = new BinaryReader(fp);
            Load(binfile);
            binfile.Close();
            fp.Close();
        }

        /// <summary>
        /// load an entire grid from a single file
        /// </summary>
        /// <param name="binfile"></param>
        public void Load(BinaryReader binfile)
        {
            navigable_space = new bool[dimension_cells][];
            for (int i = 0; i < dimension_cells; i++)
                navigable_space[i] = new bool[dimension_cells];

            LoadTile(binfile);
        }

        /// <summary>
        /// load an entire grid from the given byte array
        /// </summary>
        /// <param name="data"></param>
        public void Load(byte[] data)
        {
            navigable_space = new bool[dimension_cells][];
            for (int i = 0; i < dimension_cells; i++)
                navigable_space[i] = new bool[dimension_cells];

            LoadTile(data);
        }

        /// <summary>
        /// load tile data from file
        /// </summary>
        /// <param name="binfile"></param>
        public void LoadTile(BinaryReader binfile)
        {
            byte[] data = new byte[binfile.BaseStream.Length];
            binfile.Read(data,0,data.Length);
            LoadTile(data);
        }

        public void LoadTile(byte[] data)
        {
            // read the bounding box
            int array_index = 0;
            const int int32_bytes = 4;
            int tx = BitConverter.ToInt32(data, 0);
            int ty = BitConverter.ToInt32(data, int32_bytes);
            int bx = BitConverter.ToInt32(data, int32_bytes * 2);
            int by = BitConverter.ToInt32(data, int32_bytes * 3);
            array_index = int32_bytes * 4;

            // dimensions of the box
            int w1 = bx - tx;
            int w2 = by - ty;

            //Read binary index as a byte array
            int no_of_bits = w1 * w2;
            int no_of_bytes = no_of_bits / 8;
            if (no_of_bytes * 8 < no_of_bits) no_of_bytes++;
            byte[] indexData = new byte[no_of_bytes];
            for (int i = 0; i < no_of_bytes; i++)
                indexData[i] = data[array_index + i];
            bool[] binary_index = ArrayConversions.ToBooleanArray(indexData);
            array_index += no_of_bytes;

            int n = 0;
            int occupied_cells = 0;
            for (int y = ty; y < by; y++)
            {
                for (int x = tx; x < bx; x++)
                {
                    if (binary_index[n])
                    {
                        cell[x][y] = new occupancygridCellMultiHypothesis(dimension_cells_vertical);
                        occupied_cells++;
                    }
                    n++;
                }
            }

            if (occupied_cells > 0)
            {
                const int float_bytes = 4;

                // read occupancy values
                no_of_bytes = occupied_cells * dimension_cells_vertical * float_bytes;
                byte[] occupancyData = new byte[no_of_bytes];
                for (int i = 0; i < no_of_bytes; i++)
                    occupancyData[i] = data[array_index + i];
                array_index += no_of_bytes;
                float[] occupancy = ArrayConversions.ToFloatArray(occupancyData);

                // read colour values
                no_of_bytes = occupied_cells * dimension_cells_vertical * 3;
                byte[] colourData = new byte[no_of_bytes];
                for (int i = 0; i < no_of_bytes; i++)
                    colourData[i] = data[array_index + i];
                array_index += no_of_bytes;

                // insert the data into the grid
                n = 0;
                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        if (cell[x][y] != null)
                        {
                            particleGridCellBase[] distilled = new particleGridCellBase[dimension_cells_vertical];
                            for (int z = 0; z < dimension_cells_vertical; z++)
                            {
                                // set the probability value
                                int index = (n * dimension_cells_vertical) + z;
                                float probLogOdds = occupancy[index];
                                if (probLogOdds != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                {
                                    // create a distilled grid particle
                                    distilled[z] = new particleGridCellBase();                                

                                    distilled[z].probabilityLogOdds = probLogOdds;

                                    // set the colour
                                    distilled[z].colour = new byte[3];

                                    for (int col = 0; col < 3; col++)
                                        distilled[z].colour[col] = colourData[(index * 3) + col];
                                }
                            }
                            // insert the distilled particles into the grid cell
                            cell[x][y].SetDistilledValues(distilled);

                            // update the navigable space
                            updateNavigableSpace(null, x, y);
                            n++;
                        }
                    }
                }
            }
        }


        #endregion

        #region "exporting grid data to third party visualisation programs"

        /// <summary>
        /// export the occupancy grid data to IFrIT basic particle file format for visualisation
        /// </summary>
        /// <param name="filename">name of the file to save as</param>
        /// <param name="pose">best available pose</param>
        public void ExportToIFrIT(string filename, 
                                  particlePose pose)
        {
            ExportToIFrIT(filename, pose, dimension_cells / 2, dimension_cells / 2, dimension_cells);
        }

        /// <summary>
        /// export the occupancy grid data to IFrIT basic particle file format for visualisation
        /// </summary>
        /// <param name="filename">name of the file to save as</param>
        /// <param name="pose">best available pose</param>
        /// <param name="centre_x">centre of the tile in grid cells</param>
        /// <param name="centre_y">centre of the tile in grid cells</param>
        /// <param name="width_cells">width of the tile in grid cells</param>
        public void ExportToIFrIT(string filename,
                                  particlePose pose,
                                  int centre_x, int centre_y,
                                  int width_cells)
        {
            float threshold = 0.5f;
            int half_width_cells = width_cells / 2;

            int tx = centre_x + half_width_cells;
            int ty = centre_y + half_width_cells;
            int tz = width_cells;
            int bx = centre_x - half_width_cells;
            int by = centre_y - half_width_cells;
            int bz = 0;

            // dummy variables needed by GetProbability
            float[] colour = new float[3];

            // another dummy variable needed by GetProbability but otherwise not used
            float mean_variance = 0;

            // get the bounding region within which there are actice grid cells
            int occupied_cells = 0;
            for (int x = centre_x - half_width_cells; x <= centre_x + half_width_cells; x++)
            {
                if ((x >= 0) && (x < dimension_cells))
                {
                    for (int y = centre_y - half_width_cells; y <= centre_y + half_width_cells; y++)
                    {
                        if ((y >= 0) && (y < dimension_cells))
                        {
                            if (cell[x][y] != null)
                            {
                                for (int z = 0; z < dimension_cells_vertical; z++)
                                {
                                    float prob = cell[x][y].GetProbability(pose, x, y,
                                                                           colour, ref mean_variance);

                                    if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                    {
                                        occupied_cells++;
                                        if (x < tx) tx = x;
                                        if (y < ty) ty = y;
                                        if (z < tz) tz = z;
                                        if (x > bx) bx = x;
                                        if (y > by) by = y;
                                        if (z > bz) bz = z;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            bx++;
            by++;

            if (occupied_cells > 0)
            {
                // add bounding box information
                string bounding_box = Convert.ToString(tx) + " " + 
                                      Convert.ToString(ty) + " " +
                                      Convert.ToString(tz) + " " +
                                      Convert.ToString(bx) + " " + 
                                      Convert.ToString(by) + " " +
                                      Convert.ToString(bz) + " X Y Z";

                ArrayList particles = new ArrayList();

                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        if (cell[x][y] != null)
                        {
                            float prob = cell[x][y].GetProbability(pose, x, y,
                                                                   colour, ref mean_variance);

                            if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                            {
                                for (int z = 0; z < dimension_cells_vertical; z++)
                                {
                                    prob = cell[x][y].GetProbability(pose, x, y, z, false,
                                                                     colour, ref mean_variance);
                                    
                                    if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                    {
                                        if (prob > threshold)  // probably occupied space
                                        {
                                            // get the colour of the grid cell as a floating point value
                                            float colour_value = int.Parse(colours.GetHexFromRGB((int)colour[0], (int)colour[1], (int)colour[2]), NumberStyles.HexNumber);

                                            string particleStr = Convert.ToString(x) + " " +
                                                                 Convert.ToString(y) + " " +
                                                                 Convert.ToString(z) + " " +
                                                                 Convert.ToString(prob) + " " +
                                                                 Convert.ToString(colour_value);
                                            particles.Add(particleStr);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // write the text file
                if (particles.Count > 0)
                {
                    StreamWriter oWrite = null;
                    bool allowWrite = true;

                    try
                    {
                        oWrite = File.CreateText(filename);
                    }
                    catch
                    {
                        allowWrite = false;
                    }

                    if (allowWrite)
                    {
                        oWrite.WriteLine(Convert.ToString(particles.Count));
                        oWrite.WriteLine(bounding_box);
                        for (int p = 0; p < particles.Count; p++)
                            oWrite.WriteLine((string)particles[p]);
                        oWrite.Close();
                    }
                }

            }
        }


        #endregion
    }

}