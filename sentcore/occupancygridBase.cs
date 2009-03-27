/*
    base class for occupancy grids
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
	public class occupancygridBase : pos3D
	{
        // lookup table for log odds calculations
        protected const int LOG_ODDS_LOOKUP_LEVELS = 1000;
        protected float[] LogOdds;
        
        // the number of cells across in the (xy) plane
        public int dimension_cells;

        // the number of cells in the vertical (z) axis
        public int dimension_cells_vertical;

        // size of each grid cell (voxel) in millimetres
        public int cellSize_mm;
        
        // when localising search a wider area than when mapping
        public int localisation_search_cells = 1;

        // the maximum range of features to insert into the grid
        protected int max_mapping_range_cells;
        
        // take some shortcuts to speed things up
        // this sacrifices some detail, but for most grid cell sizes is fine
        public bool TurboMode = true;
        
        // a quick lookup table for gaussian values
        protected float[] gaussianLookup;
        
						
	    #region "constructors"
	
		public occupancygridBase() : base(0, 0, 0)
		{
		}
		
		#endregion
		
        #region "setting the position of the grid"

        /// <summary>
        /// set the absolute position of the centre of the occupancy grid
        /// </summary>
        /// <param name="centre_x_mm">centre x position in millimetres</param>
        /// <param name="centre_y_mm">centre y position in millimetres</param>
        public void SetCentrePosition(float centre_x_mm, 
                                      float centre_y_mm)
        {
            x = centre_x_mm;
            y = centre_y_mm;
        }

        #endregion

        #region "clearing the grid"

        public virtual void Clear()
        {
        }

        #endregion

        #region "sensor model"

        // a weight value used to define how aggressively the
        // carving out of space using the vacancy function works
        public float vacancy_weighting = 2.0f;

        protected float[] vacancy_model_lookup;

        /// <summary>
        /// creates a loouk table for the cacancy part of the ray model
        /// This avoids having to run slow Exp functions
        /// </summary>
        /// <param name="min_vacancy_probability">minimum probability value</param>
        /// <param name="max_vacancy_probability">maximum probability value</param>
        /// <param name="levels">number of discrete steps in the lookup</param>
        protected void CreateVacancyModelLookup(
            float min_vacancy_probability, 
            float max_vacancy_probability,
            int levels)
        {
            vacancy_model_lookup = new float[levels];
            for (int i = 0; i < levels; i++)
            {
                float fraction = i / (float)levels;
                vacancy_model_lookup[i] = 
                    vacancyFunction(fraction, 
                                    min_vacancy_probability, max_vacancy_probability);
            }
        }

        /// <summary>
        /// function for vacancy within the sensor model
        /// </summary>
        /// <param name="fraction">fractional distance along the vacancy part of the ray model</param>
        /// <param name="min_vacancy_probability">minimum probability</param>
        /// <param name="max_vacancy_probability">maximum probability</param>
        /// <returns>probability</returns>
        static public float vacancyFunction(
            float fraction,
            float min_vacancy_probability, 
            float max_vacancy_probability)
        {
            float prob = min_vacancy_probability + ((max_vacancy_probability - min_vacancy_probability) *
                         (float)Math.Exp(-(fraction * fraction)));
            return (prob);
        }

        /// <summary>
        /// vacancy part of the ray model
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        protected float vacancyFunction(float fraction, int steps)
        {
            if (vacancy_model_lookup == null)
            {
                float min_vacancy_probability = 0.1f;
                float max_vacancy_probability = vacancy_weighting;
                CreateVacancyModelLookup(min_vacancy_probability, max_vacancy_probability, 1000);
            }
            float prob = vacancy_model_lookup[(int)(fraction * (vacancy_model_lookup.Length-1))];
            prob = 0.5f - (prob / steps);
            return (prob);
        }

        #endregion	
										
        #region "calculating the matching probability"

        /// <summary>
        /// returns a measure of the difference between two colours
        /// </summary>
        /// <param name="colour1">the first colour</param>
        /// <param name="colour2">the second colour</param>
        /// <returns>difference between the two colours</returns>
        protected float getColourDifference(byte[] colour1, float[] colour2)
        {
            // note that relative colour values are used, since comparing absolute RGB
            // values is a road to nowhere
            float colour_difference = 0;
            for (int col = 0; col < 3; col++)
            {
                int col2 = col + 1;
                if (col2 > 2) col2 -= 3;
                int col3 = col + 2;
                if (col3 > 2) col3 -= 3;

                float c1 = (colour1[col] * 2) - colour1[col2] - colour1[col3];
                if (c1 < 0) c1 = 0;

                float c2 = (int)((colour2[col] * 2) - colour2[col2] - colour2[col3]);
                if (c2 < 0) c2 = 0;

                colour_difference += Math.Abs(c1 - c2);
            }
            //colour_difference /= (6 * 255.0f);
            colour_difference *= 0.0006535947712418f;

            return (colour_difference);
        }
        
        #endregion
        
        #region "calculating the positions of the robots cameras"
        
        /// <summary>
        /// calculate the position of the robots head and cameras for this pose
        /// </summary>
        /// <param name="rob">robot object</param>
        /// <param name="head_location">location of the centre of the head</param>
        /// <param name="camera_centre_location">location of the centre of each stereo camera</param>
        /// <param name="left_camera_location">location of the left camera within each stereo camera</param>
        /// <param name="right_camera_location">location of the right camera within each stereo camera</param>
        protected void calculateCameraPositions(
            robot rob,
            ref pos3D head_location,
            ref pos3D[] camera_centre_location,
            ref pos3D[] left_camera_location,
            ref pos3D[] right_camera_location)
        {
            // calculate the position of the centre of the head relative to 
            // the centre of rotation of the robots body
            pos3D head_centroid = new pos3D(-(rob.BodyWidth_mm / 2) + rob.head.x,
                                            -(rob.BodyLength_mm / 2) + rob.head.y,
                                            rob.head.z);

            // location of the centre of the head on the grid map
            // adjusted for the robot pose and the head pan and tilt angle.
            // Note that the positions and orientations of individual cameras
            // on the head have already been accounted for within stereoModel.createObservation
            pos3D head_locn = head_centroid.rotate(rob.head.pan + pan, rob.head.tilt, 0);
            head_locn = head_locn.translate(x, y, 0);
            head_location.copyFrom(head_locn);

            for (int cam = 0; cam < rob.head.no_of_stereo_cameras; cam++)
            {
                // calculate the position of the centre of the stereo camera
                // (baseline centre point)
                pos3D camera_centre_locn = new pos3D(rob.head.calibration[cam].positionOrientation.x, rob.head.calibration[cam].positionOrientation.y, rob.head.calibration[cam].positionOrientation.y);
                camera_centre_locn = camera_centre_locn.rotate(rob.head.calibration[cam].positionOrientation.pan + rob.head.pan + pan, rob.head.calibration[cam].positionOrientation.tilt, rob.head.calibration[cam].positionOrientation.roll);
                camera_centre_location[cam] = camera_centre_locn.translate(head_location.x, head_location.y, head_location.z);

                // where are the left and right cameras?
                // we need to know this for the origins of the vacancy models
                float half_baseline_length = rob.head.calibration[cam].baseline / 2;
                pos3D left_camera_locn = new pos3D(-half_baseline_length, 0, 0);
                left_camera_locn = left_camera_locn.rotate(rob.head.calibration[cam].positionOrientation.pan + rob.head.pan + pan, rob.head.calibration[cam].positionOrientation.tilt, rob.head.calibration[cam].positionOrientation.roll);
                pos3D right_camera_locn = new pos3D(-left_camera_locn.x, -left_camera_locn.y, -left_camera_locn.z);
                left_camera_location[cam] = left_camera_locn.translate(camera_centre_location[cam].x, camera_centre_location[cam].y, camera_centre_location[cam].z);
                right_camera_location[cam] = right_camera_locn.translate(camera_centre_location[cam].x, camera_centre_location[cam].y, camera_centre_location[cam].z);
                right_camera_location[cam].pan = left_camera_location[cam].pan;
            }
        }
                
        #endregion
		
        #region "inserting simulated structures, mainly for unit testing purposes"
		
		/// <summary>
		/// inserts a simulated wall into the grid
		/// </summary>
		/// <param name="tx_cell">start x cell coordinate</param>
		/// <param name="ty_cell">start y cell coordinate</param>
		/// <param name="bx_cell">end x cell coordinate</param>
		/// <param name="by_cell">end y cell coordinate</param>
		/// <param name="height_cells">height of the wall in cells</param>
		/// <param name="thickness_cells">thickness of the wall in cells</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public virtual void InsertWallCells(
		    int tx_cell, int ty_cell,
		    int bx_cell, int by_cell,
		    int height_cells,
		    int thickness_cells,
		    float probability_variance,
		    int r, int g, int b)
		{
		}
		
		/// <summary>
		/// inserts a simulated doorway into the grid
		/// </summary>
		/// <param name="tx_cell">start x cell coordinate</param>
		/// <param name="ty_cell">start y cell coordinate</param>
		/// <param name="bx_cell">end x cell coordinate</param>
		/// <param name="by_cell">end y cell coordinate</param>
		/// <param name="wall_height_cells">height of the wall in cells</param>
		/// <param name="door_height_cells">height of the doorway in cells</param>
		/// <param name="door_width_cells">width of the doorway in cells</param>
		/// <param name="thickness_cells">thickness of the wall in cells</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public virtual void InsertDoorwayCells(
		    int tx_cell, int ty_cell,
		    int bx_cell, int by_cell,
		    int wall_height_cells,
		    int door_height_cells,
		    int door_width_cells,
		    int thickness_cells,
		    float probability_variance,
		    int r, int g, int b)
		{
		}
		
		/// <summary>
		/// inserts a simulated wall into the grid
		/// </summary>
		/// <param name="tx_mm">start x coordinate</param>
		/// <param name="ty_mm">start y coordinate</param>
		/// <param name="bx_mm">end x coordinate</param>
		/// <param name="by_mm">end y coordinate</param>
		/// <param name="height_mm">height of the wall</param>
		/// <param name="thickness_mm">thickness of the wall</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public virtual void InsertWall(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int height_mm,
		    int thickness_mm,
		    float probability_variance,
		    int r, int g, int b)
		{
		    int tx_cell = (dimension_cells/2) + (int)((tx_mm - x) / cellSize_mm);
		    int ty_cell = (dimension_cells/2) + (int)((ty_mm - y) / cellSize_mm);
		    int bx_cell = (dimension_cells/2) + (int)((bx_mm - x) / cellSize_mm);
		    int by_cell = (dimension_cells/2) + (int)((by_mm - y) / cellSize_mm);
			int height_cells = height_mm / cellSize_mm;
			int thickness_cells = thickness_mm / cellSize_mm;

			InsertWallCells(
		        tx_cell, ty_cell,
		        bx_cell, by_cell,
		        height_cells,
		        thickness_cells,
		        probability_variance,
			    r, g, b);			
		}
		
		
		/// <summary>
		/// inserts a simulated doorway into the grid
		/// </summary>
		/// <param name="tx_mm">start x coordinate</param>
		/// <param name="ty_mm">start y coordinate</param>
		/// <param name="bx_mm">end x coordinate</param>
		/// <param name="by_mm">end y coordinate</param>
		/// <param name="wall_height_mm">height of the wall</param>
		/// <param name="door_height_mm">height of the door</param>
		/// <param name="door_width_mm">width of the door</param>
		/// <param name="thickness_mm">thickness of the wall</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public virtual void InsertDoorway(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int wall_height_mm,
		    int door_height_mm,
		    int door_width_mm,
		    int thickness_mm,
		    float probability_variance,
		    int r, int g, int b)
		{		
		    int tx_cell = (dimension_cells/2) + (int)((tx_mm - x) / cellSize_mm);
		    int ty_cell = (dimension_cells/2) + (int)((ty_mm - y) / cellSize_mm);
		    int bx_cell = (dimension_cells/2) + (int)((bx_mm - x) / cellSize_mm);
		    int by_cell = (dimension_cells/2) + (int)((by_mm - y) / cellSize_mm);
			int wall_height_cells = wall_height_mm / cellSize_mm;
			int door_height_cells = door_height_mm / cellSize_mm;
			int door_width_cells = door_width_mm / cellSize_mm;
			int thickness_cells = thickness_mm / cellSize_mm;
			
			InsertDoorwayCells(
			    tx_cell, ty_cell,
			    bx_cell, by_cell,
			    wall_height_cells,
			    door_height_cells,
			    door_width_cells,
			    thickness_cells,
			    probability_variance,
			    r, g, b);
		}
		
        #endregion
	}
}
