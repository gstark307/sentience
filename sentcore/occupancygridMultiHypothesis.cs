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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CenterSpace.Free;

namespace sentience.core
{
    /// <summary>
    /// two dimensional grid storing multiple occupancy hypotheses
    /// </summary>
    public class occupancygridMultiHypothesis : pos3D
    {
        // types of view of the occupancy grid returned by the Show method
        public const int VIEW_ABOVE = 0;
        public const int VIEW_LEFT_SIDE = 1;
        public const int VIEW_RIGHT_SIDE = 2;

        // random number generator
        private MersenneTwister rnd = new MersenneTwister(100);

        // list grid cells which need to be cleared of garbage
        private ArrayList garbage;

        // a quick lookup table for gaussian values
        private float[] gaussianLookup;

        // the number of cells across in the (xy) plane
        public int dimension_cells;

        // the number of cells in the vertical (z) axis
        public int dimension_cells_vertical;

        // size of each grid cell (voxel) in millimetres
        public int cellSize_mm;

        // when localising search a wider area than when mapping
        public int localisation_search_cells = 1;

        // the total number of hypotheses (particles) within the grid
        public int total_valid_hypotheses = 0;

        // the total amount of garbage awaiting collection
        public int total_garbage_hypotheses = 0;

        // the maximum range of features to insert into the grid
        private int max_mapping_range_cells;

        // a weight value used to define how aggressively the
        // carving out of space using the vacancy function works
        public float vacancy_weighting = 2.0f;

        // whether this grid has been distilled or not
        public bool isDistilled;

        // take some shortcuts to speed things up
        // this sacrifices some detail, but for most grid cell sizes is fine
        public bool TurboMode = true;

        // cells of the grid
        occupancygridCellMultiHypothesis[,] cell;

        #region "initialisation"

        /// <summary>
        /// initialise the grid
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        private void init(int dimension_cells, int dimension_cells_vertical, int cellSize_mm, 
                          int localisationRadius_mm, int maxMappingRange_mm, float vacancyWeighting)
        {
            this.dimension_cells = dimension_cells;
            this.dimension_cells_vertical = dimension_cells_vertical;
            this.cellSize_mm = cellSize_mm;
            this.localisation_search_cells = localisationRadius_mm / cellSize_mm;
            this.max_mapping_range_cells = maxMappingRange_mm / cellSize_mm;
            this.vacancy_weighting = vacancyWeighting;
            cell = new occupancygridCellMultiHypothesis[dimension_cells, dimension_cells];

            // make a lookup table for gaussians - saves doing a lot of floating point maths
            gaussianLookup = stereoModel.createHalfGaussianLookup(10);

            garbage = new ArrayList();
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        public occupancygridMultiHypothesis(int dimension_cells, int dimension_cells_vertical, 
                                            int cellSize_mm, int localisationRadius_mm, int maxMappingRange_mm, float vacancyWeighting)
            : base(0, 0,0)
        {
            init(dimension_cells, dimension_cells_vertical, cellSize_mm, localisationRadius_mm, maxMappingRange_mm, vacancyWeighting);
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
            float min_vacancy_probability = 0.1f;
            float max_vacancy_probability = vacancy_weighting;
            float prob = min_vacancy_probability + ((max_vacancy_probability - min_vacancy_probability) *
                         (float)Math.Exp(-(fraction * fraction)));
            return (0.5f - (prob / steps));
        }

        #endregion

        #region "grid update"

        /// <summary>
        /// returns the average colour variance for the entire grid
        /// </summary>
        /// <returns></returns>
        public float GetMeanColourVariance(particlePose pose)
        {
            float[] mean_colour = new float[3];
            float total_variance = 0;
            int hits = 0;

            for (int cell_y = 0; cell_y < dimension_cells; cell_y++)
            {
                for (int cell_x = 0; cell_x < dimension_cells; cell_x++)
                {
                    if (cell[cell_x, cell_y] != null)
                    {
                        float mean_variance = 0;
                        float prob = cell[cell_x, cell_y].GetProbability(pose, cell_x, cell_y,
                                                                         mean_colour, ref mean_variance);
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


        /// <summary>
        /// removes an occupancy hypothesis from a grid cell
        /// </summary>
        /// <param name="hypothesis"></param>
        public void Remove(particleGridCell hypothesis)
        {
            occupancygridCellMultiHypothesis c = cell[hypothesis.x, hypothesis.y];

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

        public void GarbageCollect(int percentage)
        {
            int max = garbage.Count-1;
            for (int i = max; i >= 0; i--)
            {
                int index = i;

                occupancygridCellMultiHypothesis c = (occupancygridCellMultiHypothesis)garbage[index];
                if (c.garbage_entries > 0)
                    total_garbage_hypotheses -= c.GarbageCollect();

                // if the garbage has been cleared remove the entry
                if (c.garbage_entries == 0)
                    garbage.RemoveAt(index);
            }
        }


        /// <summary>
        /// returns the localisation probability
        /// </summary>
        /// <param name="x_cell">x grid coordinate</param>
        /// <param name="y_cell">y grid coordinate</param>
        /// <param name="origin">pose of the robot</param>
        /// <param name="sensorModelProbability">probability value from a specific point in the ray, taken from the sensor model</param>
        /// <returns>log odds probability of there being a match between the ray and the grid</returns>
        private float matchingProbability(int x_cell, int y_cell, int z_cell,
                                          particlePose origin,
                                          float sensormodel_probability,
                                          Byte[] colour)
        {
            float value = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;
            float colour_variance = 0;

            // localise using this grid cell
            // first get the existing probability value at this cell
            float[] existing_colour = new float[3];
            float existing_probability;

            if (!isDistilled)
                // use grid particles to calculate the probability
                existing_probability =
                    cell[x_cell, y_cell].GetProbability(origin, x_cell, y_cell, z_cell,
                                                        false, existing_colour, ref colour_variance);
            else
                // use distilled probability values
                existing_probability = cell[x_cell, y_cell].GetProbabilityDistilled(existing_colour);

            if (existing_probability != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
            {
                // combine the occupancy probabilities
                float occupancy_probability = ((sensormodel_probability * existing_probability) +
                              ((1.0f - sensormodel_probability) * (1.0f - existing_probability)));

                // get the colour difference between the map and the observation
                float colour_difference = 0;
                for (int col = 0; col < 3; col++)
                    colour_difference += Math.Abs(colour[col] - existing_colour[col]);

                // turn the colour difference into a probability
                float colour_probability = 1.0f - (colour_difference / (3 * 255.0f));
                colour_probability *= colour_variance;

                // localisation matching probability, expressed as log odds
                value = util.LogOdds(occupancy_probability * colour_probability);
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
                            pos3D left_camera_location, pos3D right_camera_location)
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
                                    centre_prob = 0.5f + (sensormodel_lookup.probability[sensormodel_index, grid_step] / 2.0f);
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

                                    if ((cell[x_cell2, y_cell2] != null) && (withinMappingRange))
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

                                    if ((!isDistilled) && 
                                        (isInsideMappingRayWidth) && 
                                        (withinMappingRange))
                                    {
                                        // generate a grid cell if necessary
                                        if (cell[x_cell2, y_cell2] == null)
                                            cell[x_cell2, y_cell2] = new occupancygridCellMultiHypothesis(dimension_cells_vertical);

                                        // add a new hypothesis to this grid coordinate
                                        // note that this is also added to the original pose
                                        hypothesis = new particleGridCell(x_cell2, y_cell2, z_cell, 
                                                                          prob, origin,
                                                                          ray.colour);
                                        cell[x_cell2, y_cell2].AddHypothesis(hypothesis);
                                        origin.AddHypothesis(hypothesis, dimension_cells, dimension_cells_vertical);
                                        total_valid_hypotheses++;
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

        public void Show(int view_type, 
                         Byte[] img, int width, int height, particlePose pose, 
                         bool colour, bool scalegrid)
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
                                util.drawLine(img, width, height, xx, 0, xx, height - 1, r, g, b, 0, false);
                            }
                            for (float y = 0; y < dimension_mm; y += 1000)
                            {
                                int yy = (int)(y * height / dimension_mm);
                                util.drawLine(img, width, height, 0, yy, width - 1, yy, r, g, b, 0, false);
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
            }

        }


        private void ShowSide(Byte[] img, int width, int height, particlePose pose, bool leftSide)
        {
            float[] mean_colour = new float[3];

            // clear the image
            for (int i = 0; i < width * height * 3; i++)
                img[i] = (Byte)255;

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
                    if (cell[cell_x, cell_y] != null)
                    {
                        // what's the probability of there being a vertical structure here?
                        float mean_variance = 0;
                        float prob;
                        if (!cell[cell_x, cell_y].isDistilled)
                            prob = cell[cell_x, cell_y].GetProbability(pose, cell_x, cell_y,
                                                                       mean_colour, ref mean_variance);
                        else
                            prob = cell[cell_x, cell_y].GetProbabilityDistilled(mean_colour);

                        if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            if (prob > 0.45f)
                            {
                                // show this vertical grid section
                                occupancygridCellMultiHypothesis c = cell[cell_x, cell_y];
                                for (int cell_z = 0; cell_z < dimension_cells_vertical; cell_z++)
                                {
                                    mean_variance = 0;
                                    if (!c.isDistilled)
                                        prob = c.GetProbability(pose, cell_x, cell_y, cell_z, false, mean_colour, ref mean_variance);
                                    else
                                        prob = c.GetProbabilityDistilled(cell_z, false, mean_colour);

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
                                                        img[n + 2 - col] = (Byte)mean_colour[col];
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
        /// <param name="pose">pose from which the map is observed</param>
        private void Show(Byte[] img, int width, int height, particlePose pose)
        {
            float[] mean_colour = new float[3];

            for (int y = 0; y < height; y++)
            {
                int cell_y = y * (dimension_cells-1) / height;
                for (int x = 0; x < width; x++)
                {
                    int cell_x = x * (dimension_cells - 1) / width;

                    int n = ((y * width) + x) * 3;

                    if (cell[cell_x, cell_y] == null)
                    {
                        for (int c = 0; c < 3; c++)
                            img[n + c] = (Byte)255; // terra incognita
                    }
                    else
                    {
                        float mean_variance = 0;
                        float prob;
                        if (!cell[cell_x, cell_y].isDistilled)
                            prob = cell[cell_x, cell_y].GetProbability(pose, cell_x, cell_y,
                                                                       mean_colour, ref mean_variance);
                        else
                            prob = cell[cell_x, cell_y].GetProbabilityDistilled(mean_colour);

                        if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            for (int c = 0; c < 3; c++)
                                if (prob > 0.5f)
                                {
                                    if (prob > 0.7f)
                                        img[n + c] = (Byte)0;  // occupied
                                    else
                                        img[n + c] = (Byte)100;  // occupied
                                }
                                else
                                {
                                    if (prob < 0.3f)
                                        img[n + c] = (Byte)230;  // vacant
                                    else
                                        img[n + c] = (Byte)200;  // vacant
                                }
                        }
                        else
                        {
                            for (int c = 0; c < 3; c++)
                                img[n + c] = (Byte)255; // terra incognita
                        }
                    }
                }
            }
        }

        private void ShowColour(Byte[] img, int width, int height, particlePose pose)
        {
            float[] mean_colour = new float[3];

            for (int y = 0; y < height; y++)
            {
                int cell_y = y * (dimension_cells - 1) / height;
                for (int x = 0; x < width; x++)
                {
                    int cell_x = x * (dimension_cells - 1) / width;

                    int n = ((y * width) + x) * 3;

                    if (cell[cell_x, cell_y] == null)
                    {
                        for (int c = 0; c < 3; c++)
                            img[n + c] = (Byte)255; // terra incognita
                    }
                    else
                    {
                        float mean_variance = 0;
                        float prob;
                        if (!cell[cell_x, cell_y].isDistilled)
                            prob = cell[cell_x, cell_y].GetProbability(pose, cell_x, cell_y,
                                                                       mean_colour, ref mean_variance);
                        else
                            prob = cell[cell_x, cell_y].GetProbabilityDistilled(mean_colour);

                        if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        {
                            if ((mean_variance < 0.7f) || (prob < 0.5f))
                            {
                                for (int c = 0; c < 3; c++)
                                    if (prob > 0.6f)
                                    {
                                        img[n + 2 - c] = (Byte)mean_colour[c];  // occupied
                                    }
                                    else
                                    {
                                        if (prob < 0.3f)
                                            img[n + c] = (Byte)200;
                                        else
                                            img[n + c] = (Byte)255;
                                    }
                            }
                            else
                            {
                                for (int c = 0; c < 3; c++)
                                    img[n + c] = (Byte)255;
                            }
                        }
                        else
                        {
                            for (int c = 0; c < 3; c++)
                                img[n + c] = (Byte)255; // terra incognita
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
        public void Save(String filename, particlePose pose)
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
        public void Save(BinaryWriter binfile, particlePose pose)
        {
            SaveTile(binfile, pose, dimension_cells / 2, dimension_cells / 2, dimension_cells);
        }

        /// <summary>
        /// save the entire grid as a byte array, suitable for subsequent compression
        /// </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        public Byte[] Save(particlePose pose)
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
                             int centre_x, int centre_y, 
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
        public Byte[] SaveTile(particlePose pose,
                               int centre_x, int centre_y,
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
                            if (cell[x, y] != null)
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
            Byte[] intbytes = BitConverter.GetBytes(tx);
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
                    if (cell[x, y] != null)
                    {
                        occupied_cells++;
                        binary_index[n] = true;
                    }
                    n++;
                }
            }

            // write the binary index in one go by converting it to a byte array
            // this is much faster than trying to write individual values one at a time
            Byte[] indexBytes = util.ToByteArray(binary_index);
            for (int i = 0; i < indexBytes.Length; i++)
                data.Add(indexBytes[i]);            

            // dummy variables needed by GetProbability
            float[] colour = new float[3];

            if (occupied_cells > 0)
            {
                float[] occupancy = new float[occupied_cells * dimension_cells_vertical];
                Byte[] colourData = new Byte[occupied_cells * dimension_cells_vertical * 3];

                n = 0;
                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        if (cell[x, y] != null)
                        {
                            // distill particles down to single values
                            cell[x, y].Distill(pose, x, y);

                            for (int z = 0; z < dimension_cells_vertical; z++)
                            {
                                float prob = cell[x, y].GetProbabilityDistilled(z, true, colour);
                                int index = (n * dimension_cells_vertical) + z;
                                occupancy[index] = prob;
                                if (prob != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                                    for (int col = 0; col < 3; col++)
                                        colourData[(index * 3) + col] = (Byte)colour[col];
                            }
                            n++;
                        }
                    }
                }

                // write the occupancy data in one go by converting it to a byte array
                // this is much faster than trying to write individual values one at a time
                Byte[] occupancyBytes = util.ToByteArray(occupancy);
                for (int i = 0; i < occupancyBytes.Length; i++)
                    data.Add(occupancyBytes[i]);
                for (int i = 0; i < colourData.Length; i++)
                    data.Add(colourData[i]);            
            }
            Byte[] result = (Byte[])data.ToArray(typeof(Byte));
            return(result);
        }


        /// <summary>
        /// load an entire grid from a single file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
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
            LoadTile(binfile);
        }

        /// <summary>
        /// load an entire grid from the given byte array
        /// </summary>
        /// <param name="data"></param>
        public void Load(Byte[] data)
        {
            LoadTile(data);
        }

        /// <summary>
        /// load tile data from file
        /// </summary>
        /// <param name="binfile"></param>
        public void LoadTile(BinaryReader binfile)
        {
            Byte[] data = new Byte[binfile.BaseStream.Length];
            binfile.Read(data,0,data.Length);
            LoadTile(data);
        }

        public void LoadTile(Byte[] data)
        {
            // indicate that this grid contains distilled cell values
            isDistilled = true;

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
            Byte[] indexData = new Byte[no_of_bytes];
            for (int i = 0; i < no_of_bytes; i++)
                indexData[i] = data[array_index + i];
            bool[] binary_index = util.ToBooleanArray(indexData);
            array_index += no_of_bytes;

            int n = 0;
            int occupied_cells = 0;
            for (int y = ty; y < by; y++)
            {
                for (int x = tx; x < bx; x++)
                {
                    if (binary_index[n])
                    {
                        cell[x, y] = new occupancygridCellMultiHypothesis(dimension_cells_vertical);
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
                Byte[] occupancyData = new Byte[no_of_bytes];
                for (int i = 0; i < no_of_bytes; i++)
                    occupancyData[i] = data[array_index + i];
                array_index += no_of_bytes;
                float[] occupancy = util.ToFloatArray(occupancyData);

                // read colour values
                no_of_bytes = occupied_cells * dimension_cells_vertical * 3;
                Byte[] colourData = new Byte[no_of_bytes];
                for (int i = 0; i < no_of_bytes; i++)
                    colourData[i] = data[array_index + i];
                array_index += no_of_bytes;

                // insert the data into the grid
                n = 0;
                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        if (cell[x, y] != null)
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
                                    distilled[z].colour = new Byte[3];

                                    for (int col = 0; col < 3; col++)
                                        distilled[z].colour[col] = colourData[(index * 3) + col];
                                }
                            }
                            // insert the distilled particles into the grid cell
                            cell[x, y].SetDistilledValues(distilled);
                            n++;
                        }
                    }
                }
            }
        }


        #endregion
    }

}
