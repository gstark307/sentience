/*
    A simple type of occupancy grid
    Copyright (C) 2009 Bob Mottram
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
	public class occupancygridSimple : occupancygridBase
	{
	    // grid cells
	    protected particleGridCellBase[][][] cell; 

        #region "constructors"
				
		protected void Init(
		    int dimension_cells, 
            int dimension_cells_vertical, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
		{
		    cell = new particleGridCellBase[dimension_cells][][];
		    for (int x = 0; x < dimension_cells; x++)
		        cell[x] = new particleGridCellBase[dimension_cells][];
		        
		    this.dimension_cells = dimension_cells;
		    this.dimension_cells_vertical = dimension_cells_vertical;
		    this.cellSize_mm = cellSize_mm;
            this.localisation_search_cells = localisationRadius_mm / cellSize_mm;
            this.max_mapping_range_cells = maxMappingRange_mm / cellSize_mm;
            this.vacancy_weighting = vacancyWeighting;		    
            
            // make a lookup table for gaussians - saves doing a lot of floating point maths
            gaussianLookup = stereoModel.createHalfGaussianLookup(10);

            // create a lookup table for log odds calculations, to avoid having
            // to run slow Log calculations at runtime
            LogOdds = probabilities.CreateLogOddsLookup(LOG_ODDS_LOOKUP_LEVELS);
            
		}
		
        public occupancygridSimple(
            int dimension_cells, 
            int dimension_cells_vertical, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
        {
            Init(dimension_cells, 
                 dimension_cells_vertical, 
                 cellSize_mm, 
                 localisationRadius_mm, 
                 maxMappingRange_mm, 
                 vacancyWeighting);
		}
		
		#endregion
		
        #region "calculating the matching probability"
        
        public const int NO_OCCUPANCY_EVIDENCE = 99999999;        

        /// <summary>
        /// returns the localisation probability
        /// </summary>
        /// <param name="x_cell">x grid coordinate</param>
        /// <param name="y_cell">y grid coordinate</param>
        /// <param name="sensormodel_probability">probability value from a specific point in the ray, taken from the sensor model</param>
        /// <param name="colour">colour of the localisation ray</param>
        /// <returns>log odds probability of there being a match between the ray and the grid</returns>
        private float matchingProbability(
            int x_cell, int y_cell, int z_cell,
            float sensormodel_probability,
            byte[] colour)
        {
            float prob_log_odds = NO_OCCUPANCY_EVIDENCE;
                        
            if (cell[x_cell][y_cell] != null)
            {
                particleGridCellBase c = cell[x_cell][y_cell][z_cell];
                float existing_probability = probabilities.LogOddsToProbability(c.probabilityLogOdds);

                // combine the occupancy probabilities
                float occupancy_probability = 
                    ((sensormodel_probability * existing_probability) +
                    ((1.0f - sensormodel_probability) * (1.0f - existing_probability)));

                // localisation matching probability, expressed as log odds
                prob_log_odds = LogOdds[(int)(occupancy_probability * LOG_ODDS_LOOKUP_LEVELS)];
            }
            
            return(prob_log_odds);
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
        /// <param name="sensormodel_lookup">the sensor model to be used</param>
        /// <param name="left_camera_location">location and pose of the left camera</param>
        /// <param name="right_camera_location">location and pose of the right camera</param>
        /// <param name="localiseOnly">if true does not add any mapping particles (pure localisation)</param>
        /// <returns>matching probability, expressed as log odds</returns>
        public float Insert(
            evidenceRay ray, 
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
            float matchingScore = NO_OCCUPANCY_EVIDENCE;  // total localisation matching score
            int rayWidth = 0;         // widest point in the ray in cells
            int widest_point;         // widest point index
            int step_size = 1;

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
                                            float score = NO_OCCUPANCY_EVIDENCE;
                                            if (longest_axis == X_AXIS)
                                                score = matchingProbability(x_cell2, y_cell2, z_cell, prob_localisation, ray.colour);

                                            if (longest_axis == Y_AXIS)
                                                score = matchingProbability(x_cell2, y_cell2, z_cell, prob_localisation, ray.colour);

                                            if (score != NO_OCCUPANCY_EVIDENCE)
                                            {
                                                if (matchingScore != NO_OCCUPANCY_EVIDENCE)
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
                                        // generate a grid cell if necessary
                                        if (cell[x_cell2][y_cell2] == null)
                                            cell[x_cell2][y_cell2] = new particleGridCellBase[dimension_cells_vertical];
                                        if (cell[x_cell2][y_cell2][z_cell] == null)
                                            cell[x_cell2][y_cell2][z_cell] = new particleGridCellBase();
                                            
                                        particleGridCellBase c = cell[x_cell2][y_cell2][z_cell];
                                        c.probabilityLogOdds += probabilities.LogOdds(prob);
                                        c.colour = ray.colour;    // this is simplistic, but we'll live with it                     
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
		
	}
}
