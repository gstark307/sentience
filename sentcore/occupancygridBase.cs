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

        #region "checking if a grid cell is occupied"
		
		/// <summary>
		/// is the given grid cell occupied ?
		/// </summary>
		/// <param name="cell_x">x cell coordinate</param>
		/// <param name="cell_y">y cell coordinate</param>
		/// <param name="cell_z">z cell coordinate</param>
		/// <returns>returns true if the probability of occupancy is greater than 0.5</returns>
		public virtual bool IsOccupied(
		    int cell_x,
		    int cell_y,
		    int cell_z)
		{
			return(false);
		}
		
        #endregion

        #region "probing the grid"
		
		/// <summary>
		/// probes the grid using the given 3D line and returns the distance to the nearest occupied grid cell
		/// </summary>
		/// <param name="x0_mm">start x coordinate</param>
		/// <param name="y0_mm">start y coordinate</param>
		/// <param name="z0_mm">start z coordinate</param>
		/// <param name="x1_mm">end x coordinate</param>
		/// <param name="y1_mm">end y coordinate</param>
		/// <param name="z1_mm">end z coordinate</param>
		/// <returns>range to the nearest occupied grid cell in millimetres</returns>
		public virtual float ProbeRange(
	        float x0_mm,
		    float y0_mm,
		    float z0_mm,
	        float x1_mm,
		    float y1_mm,
		    float z1_mm)
		{
			return(-1);
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

        /// <summary>
        /// Calculate the position of the robots head and cameras for this pose
        /// the positions returned are relative to the robots centre of rotation
        /// </summary>
        /// <param name="body_width_mm">width of the robot body in millimetres</param>
        /// <param name="body_length_mm">length of the robot body in millimetres</param>
        /// <param name="body_centre_of_rotation_x">x centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_y">y centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_z">z centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="head_centroid_x">head centroid x position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_y">head centroid y position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_z">head centroid z position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_pan">head pan angle in radians</param>
        /// <param name="head_tilt">head tilt angle in radians</param>
        /// <param name="head_roll">head roll angle in radians</param>
        /// <param name="baseline_mm">stereo camera baseline in millimetres</param>
        /// <param name="stereo_camera_position_x">stereo camera x position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_y">stereo camera y position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_z">stereo camera z position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_pan">stereo camera pan in radians relative to the head</param>
        /// <param name="stereo_camera_tilt">stereo camera tilt in radians relative to the head</param>
        /// <param name="stereo_camera_roll">stereo camera roll in radians relative to the head</param>
        /// <param name="head_location">returned location/orientation of the robot head</param>
        /// <param name="camera_centre_location">returned stereo camera centre position/orientation</param>
        /// <param name="left_camera_location">returned left camera position/orientation</param>
        /// <param name="right_camera_location">returned right camera position/orientation</param>
        public static void calculateCameraPositions(
		    float body_width_mm,
		    float body_length_mm,
		    float body_centre_of_rotation_x,
		    float body_centre_of_rotation_y,
		    float body_centre_of_rotation_z,
		    float head_centroid_x,
		    float head_centroid_y,
		    float head_centroid_z,
		    float head_pan,
		    float head_tilt,
		    float head_roll,
		    float baseline_mm,
		    float stereo_camera_position_x,
		    float stereo_camera_position_y,
		    float stereo_camera_position_z,
		    float stereo_camera_pan,
		    float stereo_camera_tilt,
		    float stereo_camera_roll,
            ref pos3D head_location,
            ref pos3D camera_centre_location,
            ref pos3D left_camera_location,
            ref pos3D right_camera_location)
        {
            // calculate the position of the centre of the head relative to 
            // the centre of rotation of the robots body
            pos3D head_centroid = 
				new pos3D(
				    -(body_width_mm * 0.5f) + (body_centre_of_rotation_x - (body_width_mm * 0.5f)) + head_centroid_x,
                    -(body_length_mm * 0.5f) + (body_centre_of_rotation_y - (body_length_mm * 0.5f)) + head_centroid_y,
                    head_centroid_z);

            // location of the centre of the head
            // adjusted for the robot pose and the head pan and tilt angle.
            // Note that the positions and orientations of individual cameras
            // on the head have already been accounted for within stereoModel.createObservation
            pos3D head_locn = 
				head_centroid.rotate(
				    head_pan, head_tilt, 0);
            head_location.copyFrom(head_locn);

            // calculate the position of the centre of the stereo camera
            // (baseline centre point)
            pos3D camera_centre_locn = 
				new pos3D(
				    stereo_camera_position_x, 
				    stereo_camera_position_y, 
				    stereo_camera_position_z);
			
			// rotate the stereo camera	    	    
            camera_centre_locn = 
                camera_centre_locn.rotate(
                    stereo_camera_pan + head_pan, 
                    stereo_camera_tilt + head_tilt, 
                    stereo_camera_roll + head_roll);
            
            // move the stereo camera relative to the head position
            camera_centre_location = 
                camera_centre_locn.translate(
                    head_location.x, 
                    head_location.y, 
                    head_location.z);

            // where are the left and right cameras?
            // we need to know this for the origins of the vacancy models
            float half_baseline_length = baseline_mm * 0.5f;
            pos3D left_camera_locn = new pos3D(-half_baseline_length, 0, 0);
            left_camera_locn = left_camera_locn.rotate(stereo_camera_pan + head_pan, stereo_camera_tilt + head_tilt, stereo_camera_roll + head_roll);            
            pos3D right_camera_locn = new pos3D(-left_camera_locn.x, -left_camera_locn.y, -left_camera_locn.z);
            left_camera_location = left_camera_locn.translate(camera_centre_location.x, camera_centre_location.y, camera_centre_location.z);
            right_camera_location = right_camera_locn.translate(camera_centre_location.x, camera_centre_location.y, camera_centre_location.z);
            right_camera_location.pan = left_camera_location.pan;
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
		/// inserts a simulated block into the grid
		/// </summary>
		/// <param name="tx_cell">start x cell coordinate</param>
		/// <param name="ty_cell">start y cell coordinate</param>
		/// <param name="bx_cell">end x cell coordinate</param>
		/// <param name="by_cell">end y cell coordinate</param>
		/// <param name="bottom_height_cells">bottom height of the block in cells</param>
		/// <param name="top_height_cells">bottom height of the block in cells</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public virtual void InsertBlockCells(
		    int tx_cell, int ty_cell,
		    int bx_cell, int by_cell,
		    int bottom_height_cells,
		    int top_height_cells,
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
		/// inserts a simulated block into the grid
		/// </summary>
		/// <param name="tx_mm">start x coordinate</param>
		/// <param name="ty_mm">start y coordinate</param>
		/// <param name="bx_mm">end x coordinate</param>
		/// <param name="by_mm">end y coordinate</param>
		/// <param name="bottom_height_mm">bottom height of the block</param>
		/// <param name="top_height_mm">top height of the block</param>
		/// <param name="probability_variance">variation of probabilities, typically less than 0.2</param>
		/// <param name="r">red colour component in the range 0-255</param>
		/// <param name="g">green colour component in the range 0-255</param>
		/// <param name="b">blue colour component in the range 0-255</param>
		public virtual void InsertBlock(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int bottom_height_mm,
		    int top_height_mm,
		    float probability_variance,
		    int r, int g, int b)
		{
		    int tx_cell = (dimension_cells/2) + (int)((tx_mm - x) / cellSize_mm);
		    int ty_cell = (dimension_cells/2) + (int)((ty_mm - y) / cellSize_mm);
		    int bx_cell = (dimension_cells/2) + (int)((bx_mm - x) / cellSize_mm);
		    int by_cell = (dimension_cells/2) + (int)((by_mm - y) / cellSize_mm);
			int bottom_height_cells = bottom_height_mm / cellSize_mm;
			int top_height_cells = top_height_mm / cellSize_mm;

			InsertBlockCells(
		        tx_cell, ty_cell,
		        bx_cell, by_cell,
		        bottom_height_cells,
			    top_height_cells,
		        probability_variance,
			    r, g, b);			
		}
				
		/// <summary>
		/// create a room-like structure within the grid
		/// </summary>
		/// <param name="tx_mm">top left x coordinate of the room</param>
		/// <param name="ty_mm">top left y coordinate of the room</param>
		/// <param name="bx_mm">bottom right x coordinate of the room</param>
		/// <param name="by_mm">bottom right y coordinate of the room</param>
		/// <param name="height_mm">height of the room</param>
		/// <param name="wall_thickness_mm">thickness of the walls</param>
		/// <param name="probability_variance">variation in probabilities, typically less than 0.2</param>
		/// <param name="floor_r">red component of the floor colour</param>
		/// <param name="floor_g">green component of the floor colour</param>
		/// <param name="floor_b">blue component of the floor colour</param>
		/// <param name="walls_r">red component of the wall colour</param>
		/// <param name="walls_g">green component of the wall colour</param>
		/// <param name="walls_b">blue component of the wall colour</param>
		/// <param name="left_wall_doorways">centre position of doorways on the left wall in mm</param>
		/// <param name="top_wall_doorways">centre position of doorways on the top wall in mm</param>
		/// <param name="right_wall_doorways">centre position of doorways on the right wall in mm</param>
		/// <param name="bottom_wall_doorways">centre position of doorways on the bottom wall in mm</param>
		/// <param name="doorway_width_mm">width of doors</param>
		/// <param name="doorway_height_mm">height of doors</param>
		public virtual void InsertRoom(
		    int tx_mm, int ty_mm,
		    int bx_mm, int by_mm,
		    int height_mm,
		    int wall_thickness_mm,                           
		    float probability_variance,
		    int floor_r, int floor_g, int floor_b,
		    int walls_r, int walls_g, int walls_b,
		    List<int> left_wall_doorways,
		    List<int> top_wall_doorways,
		    List<int> right_wall_doorways,
		    List<int> bottom_wall_doorways,
		    int doorway_width_mm,
		    int doorway_height_mm)
		{
			// floor			
			InsertBlock(
		        tx_mm, ty_mm,
		        bx_mm, by_mm,
		        0,
			    10,
		        probability_variance,
			    floor_r, floor_g, floor_b);
			
			// left wall
			InsertWall(
			    tx_mm, ty_mm,
			    tx_mm, by_mm,
			    height_mm,
			    wall_thickness_mm,
			    probability_variance,
			    walls_r, walls_g, walls_b);     
			    			           

			// top wall
			InsertWall(
			    tx_mm, ty_mm,
			    bx_mm, ty_mm,
			    height_mm,
			    wall_thickness_mm,
			    probability_variance,
			    walls_r, walls_g, walls_b);            

			// right wall
			InsertWall(
			    bx_mm, ty_mm,
			    bx_mm, by_mm,
			    height_mm,
			    wall_thickness_mm,
			    probability_variance,
			    walls_r, walls_g, walls_b);            

			// bottom wall
			InsertWall(
			    tx_mm, by_mm,
			    bx_mm, by_mm,
			    height_mm,
			    wall_thickness_mm,
			    probability_variance,
			    walls_r, walls_g, walls_b);            
			
			int doorway_centre_x_mm, doorway_centre_y_mm;
			if (left_wall_doorways != null)
			{
				doorway_centre_x_mm = tx_mm;
				for (int i = 0; i < left_wall_doorways.Count; i++)
				{
					doorway_centre_y_mm = ty_mm + left_wall_doorways[i];
					int doorway_ty_mm = doorway_centre_y_mm - (doorway_width_mm/2);
					int doorway_by_mm = doorway_ty_mm + doorway_width_mm;
					
                    InsertDoorway(
		                doorway_centre_x_mm, doorway_ty_mm,
		                doorway_centre_x_mm, doorway_by_mm,
		                height_mm,
		                doorway_height_mm,
		                doorway_width_mm,
		                wall_thickness_mm,
		                probability_variance,
		                walls_r, walls_g, walls_b);					
				}
			}
			
			if (top_wall_doorways != null)
			{
				doorway_centre_y_mm = ty_mm;
				for (int i = 0; i < top_wall_doorways.Count; i++)
				{
					doorway_centre_x_mm = tx_mm + top_wall_doorways[i];
					int doorway_tx_mm = doorway_centre_x_mm - (doorway_width_mm/2);
					int doorway_bx_mm = doorway_tx_mm + doorway_width_mm;
					
                    InsertDoorway(
		                doorway_tx_mm, doorway_centre_y_mm,
		                doorway_bx_mm, doorway_centre_y_mm,
		                height_mm,
		                doorway_height_mm,
		                doorway_width_mm,
		                wall_thickness_mm,
		                probability_variance,
		                walls_r, walls_g, walls_b);				
				}
			}
			if (right_wall_doorways != null)
			{
				doorway_centre_x_mm = bx_mm;
				for (int i = 0; i < right_wall_doorways.Count; i++)
				{
					doorway_centre_y_mm = ty_mm + right_wall_doorways[i];
					int doorway_ty_mm = doorway_centre_y_mm - (doorway_width_mm/2);
					int doorway_by_mm = doorway_ty_mm + doorway_width_mm;
					
                    InsertDoorway(
		                doorway_centre_x_mm, doorway_ty_mm,
		                doorway_centre_x_mm, doorway_by_mm,
		                height_mm,
		                doorway_height_mm,
		                doorway_width_mm,
		                wall_thickness_mm,
		                probability_variance,
		                walls_r, walls_g, walls_b);					
				}
			}
			if (bottom_wall_doorways != null)
			{
				doorway_centre_y_mm = by_mm;
				for (int i = 0; i < bottom_wall_doorways.Count; i++)
				{
					doorway_centre_x_mm = tx_mm + bottom_wall_doorways[i];
					int doorway_tx_mm = doorway_centre_x_mm - (doorway_width_mm/2);
					int doorway_bx_mm = doorway_tx_mm + doorway_width_mm;
					
                    InsertDoorway(
		                doorway_tx_mm, doorway_centre_y_mm,
		                doorway_bx_mm, doorway_centre_y_mm,
		                height_mm,
		                doorway_height_mm,
		                doorway_width_mm,
		                wall_thickness_mm,
		                probability_variance,
		                walls_r, walls_g, walls_b);				
				}
			}
			
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
		
        #region "exporting grid data to other applications"
		
        /// <summary>
        /// export the occupancy grid data to IFrIT basic particle file format for visualisation
        /// </summary>
        /// <param name="filename">name of the file to save as</param>
        public virtual void ExportToIFrIT(string filename)
        {
		}
		
        #endregion
	}
}
