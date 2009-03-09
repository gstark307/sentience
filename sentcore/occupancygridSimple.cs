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
		
		#region "adding a new observation as a set of stereo rays"

        /// <summary>
        /// add an observation taken from this pose
        /// </summary>
        /// <param name="stereo_rays">list of ray objects in this observation</param>
        /// <param name="rob">robot object</param>
        /// <param name="LocalGrid">occupancy grid into which to insert the observation</param>
        /// <param name="localiseOnly">if true does not add any mapping particles (pure localisation)</param>
        /// <returns>localisation matching score</returns>
        public float AddObservation(
            List<evidenceRay>[] stereo_rays,
            robot rob, 
            bool localiseOnly)
        {
            // clear the localisation score
            float localisation_score = NO_OCCUPANCY_EVIDENCE;

            // get the positions of the head and cameras for this pose
            pos3D head_location = new pos3D(0,0,0);
            pos3D[] camera_centre_location = new pos3D[rob.head.no_of_stereo_cameras];
            pos3D[] left_camera_location = new pos3D[rob.head.no_of_stereo_cameras];
            pos3D[] right_camera_location = new pos3D[rob.head.no_of_stereo_cameras];
            calculateCameraPositions(rob, ref head_location,
                                     ref camera_centre_location,
                                     ref left_camera_location,
                                     ref right_camera_location);
            
            // itterate for each stereo camera
            int cam = stereo_rays.Length - 1;
            while (cam >= 0)
            {
                // itterate through each ray
                int r = stereo_rays[cam].Count - 1;
                while (r >= 0)
                {
                    // observed ray.  Note that this is in an egocentric
                    // coordinate frame relative to the head of the robot
                    evidenceRay ray = stereo_rays[cam][r];

                    // translate and rotate this ray appropriately for the pose
                    evidenceRay trial_ray = ray.trialPose(camera_centre_location[cam].pan, 
                                                          camera_centre_location[cam].x, 
                                                          camera_centre_location[cam].y);

                    // update the grid cells for this ray and update the
                    // localisation score accordingly
                    float score =
                        Insert(trial_ray, 
                               rob.head.sensormodel[cam],
                               left_camera_location[cam], 
                               right_camera_location[cam],
                               localiseOnly);
                    if (score != NO_OCCUPANCY_EVIDENCE)
                        if (localisation_score != NO_OCCUPANCY_EVIDENCE)
                            localisation_score += score;
                        else
                            localisation_score = score;
                    r--;
                }
                cam--;
            }

            return (localisation_score);
        }

        #endregion
		
        #region "exporting grid data to third party visualisation programs"

        /// <summary>
        /// export the occupancy grid data to IFrIT basic particle file format for visualisation
        /// </summary>
        /// <param name="filename">name of the file to save as</param>
        public void ExportToIFrIT(string filename)
        {
            ExportToIFrIT(filename, dimension_cells / 2, dimension_cells / 2, dimension_cells);
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
                                    float prob = cell[x][y][z].probabilityLogOdds;

                                    if (prob != NO_OCCUPANCY_EVIDENCE)
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

                List<string> particles = new List<string>();

                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        if (cell[x][y] != null)
                        {
                            for (int z = 0; z < dimension_cells_vertical; z++)
                            {
                                if (cell[x][y][z] != null)
                                {
                                    float prob = cell[x][y][z].probabilityLogOdds;
                                    
                                    if (prob != NO_OCCUPANCY_EVIDENCE)
                                    {
                                        prob = probabilities.LogOddsToProbability(prob);
                                        if (prob > threshold)  // probably occupied space
                                        {
                                            // get the colour of the grid cell as a floating point value
                                            float colour_value = int.Parse(colours.GetHexFromRGB((int)cell[x][y][z].colour[0], (int)cell[x][y][z].colour[1], (int)cell[x][y][z].colour[2]), NumberStyles.HexNumber);

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
                            oWrite.WriteLine(particles[p]);
                        oWrite.Close();
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
        public void Save(string filename)
        {
            FileStream fp = new FileStream(filename, FileMode.Create);
            BinaryWriter binfile = new BinaryWriter(fp);
            Save(binfile);
            binfile.Close();
            fp.Close();
        }

        /// <summary>
        /// save the entire grid to a single file
        /// </summary>
        /// <param name="binfile"></param>
        public void Save(BinaryWriter binfile)
        {
            SaveTile(binfile, dimension_cells / 2, dimension_cells / 2, dimension_cells);
        }

        /// <summary>
        /// save the entire grid as a byte array, suitable for subsequent compression
        /// </summary>
        /// <returns></returns>
        public byte[] Save()
        {
            return(SaveTile(dimension_cells / 2, dimension_cells / 2, dimension_cells));
        }

        /// <summary>
        /// save the occupancy grid data to file as a tile
        /// it is expected that multiple tiles will be saved per grid
        /// </summary>
        /// <param name="binfile">file to write to</param>
        /// <param name="centre_x">centre of the tile in grid cells</param>
        /// <param name="centre_y">centre of the tile in grid cells</param>
        /// <param name="width_cells">width of the tile in grid cells</param>
        public void SaveTile(BinaryWriter binfile,
                             int centre_x, 
                             int centre_y, 
                             int width_cells)
        {
            // write the whole thing to disk in one go
            binfile.Write(SaveTile(centre_x, centre_y, width_cells));
        }

        /// <summary>
        /// save the occupancy grid data to file as a tile
        /// it is expected that multiple tiles will be saved per grid
        /// This returns a byte array, which may subsequently be 
        /// compressed as a zip file for extra storage efficiency
        /// </summary>
        /// <param name="centre_x">centre of the tile in grid cells</param>
        /// <param name="centre_y">centre of the tile in grid cells</param>
        /// <param name="width_cells">width of the tile in grid cells</param>
        /// <returns>byte array containing the data</returns>
        public byte[] SaveTile(
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
                                if (cell[x][y][z] != null)
                                {
                                    float prob = probabilities.LogOddsToProbability(cell[x][y][z].probabilityLogOdds);
                                    int index = (n * dimension_cells_vertical) + z;
                                    occupancy[index] = prob;
                                    if (prob != NO_OCCUPANCY_EVIDENCE)
                                        for (int col = 0; col < 3; col++)
                                            colourData[(index * 3) + col] = (byte)cell[x][y][z].colour[col];
                                }
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
            LoadTile(binfile);
        }

        /// <summary>
        /// load an entire grid from the given byte array
        /// </summary>
        /// <param name="data"></param>
        public void Load(byte[] data)
        {
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
                        cell[x][y] = new particleGridCellBase[dimension_cells_vertical];
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
                            cell[x][y] = new particleGridCellBase[dimension_cells_vertical];
                            for (int z = 0; z < dimension_cells_vertical; z++)
                            {
                                // set the probability value
                                int index = (n * dimension_cells_vertical) + z;
                                float probLogOdds = probabilities.LogOdds(occupancy[index]);
                                if (probLogOdds != NO_OCCUPANCY_EVIDENCE)
                                {
                                    // create a distilled grid particle
                                    cell[x][y][z] = new particleGridCellBase();                                

                                    cell[x][y][z].probabilityLogOdds = probLogOdds;

                                    // set the colour
                                    cell[x][y][z].colour = new byte[3];

                                    for (int col = 0; col < 3; col++)
                                        cell[x][y][z].colour[col] = colourData[(index * 3) + col];
                                }
                            }

                            n++;
                        }
                    }
                }
            }
        }


        #endregion

		
	}
}
