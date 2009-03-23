/*
    Meta level occupancy grid
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
using System.Collections.Generic;
using sluggish.utilities;

namespace sentience.core
{
	public class metagrid
	{
        public const int TYPE_SIMPLE = 0;
        public const int TYPE_MULTI_HYPOTHESIS = 1;
        
        // the type of occupancy grid to be used
        public int grid_type;

        // sub grids
        protected occupancygridBase[] grid;  

        // weighting for each sub grid, used to compute localisation matching scores
        protected float[] grid_weight;       

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="no_of_grids">The number of sub grids</param>
        /// <param name="grid_type">the type of sub grids</param>
        /// <param name="dimension_mm">dimension of the smallest sub grid</param>
        /// <param name="dimension_vertical_mm">vertical dimension of the smallest sub grid</param>
        /// <param name="cellSize_mm">cell size of the smallest sub grid</param>
        /// <param name="localisationRadius_mm">localisation radius within the smallest sub grid</param>
        /// <param name="maxMappingRange_mm">maximum mapping radius within the smallest sub grid</param>
        /// <param name="vacancyWeighting">vacancy model weighting, typically between 0.2 and 2</param>
        public metagrid(
            int no_of_grids,
            int grid_type,
		    int dimension_mm, 
            int dimension_vertical_mm, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
        {
            int dimension_cells = dimension_mm / cellSize_mm;
            int dimension_cells_vertical = dimension_vertical_mm / cellSize_mm;
            if (vacancyWeighting < 0.2f) vacancyWeighting = 0.2f;
            this.grid_type = grid_type;
            grid = new occupancygridBase[no_of_grids];

            // values used to weight the matching score for each sub grid
            grid_weight = new float[no_of_grids];
            grid_weight[0] = 1;
            for (int i = 1; i < no_of_grids; i++)
                grid_weight[i] = grid_weight[i] * 0.5f;

            switch (grid_type)
            {
                case TYPE_SIMPLE:
                    {
                        for (int g = 0; g < no_of_grids; g++)
                            grid[g] = new occupancygridSimple(dimension_cells, dimension_cells_vertical, cellSize_mm * (g + 1), localisationRadius_mm * (g + 1), maxMappingRange_mm * (g + 1), vacancyWeighting);

                        break;
                    }
                case TYPE_MULTI_HYPOTHESIS:
                    {
                        for (int g = 0; g < no_of_grids; g++)
                            grid[g] = new occupancygridMultiHypothesis(dimension_cells, dimension_cells_vertical, cellSize_mm * (g + 1), localisationRadius_mm * (g + 1), maxMappingRange_mm * (g + 1), vacancyWeighting);

                        break;
                    }
            }
        }

        #endregion

        #region "clearing the grid"

        public void Clear()
        {
            for (int i = 0; i < grid.Length; i++)
                grid[i].Clear();
        }

        #endregion

        #region "setting the grid position"

        /// <summary>
        /// set the position and orientation of the grid
        /// </summary>
        /// <param name="centre_x_mm">centre x coordinate in millimetres</param>
        /// <param name="centre_y_mm">centre y coordinate in millimetres</param>
        /// <param name="centre_z_mm">centre z coordinate in millimetres</param>
        /// <param name="orientation_radians">grid orientation in radians</param>
        public void SetPosition(
            float centre_x_mm,
            float centre_y_mm,
            float centre_z_mm,
            float orientation_radians)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i].x = centre_x_mm;
                grid[i].y = centre_y_mm;
                grid[i].z = centre_z_mm;
                grid[i].pan = orientation_radians;
            }
        }

        #endregion

        #region "updating the grid"

        /// <summary>
        /// insert a stereo ray into the grid
        /// </summary>
        /// <param name="ray">stereo ray</param>
        /// <param name="origin">pose from which this observation was made</param>
        /// <param name="sensormodel_lookup">sensor model to be used</param>
        /// <param name="left_camera_location">left stereo camera position and orientation</param>
        /// <param name="right_camera_location">right stereo camera position and orientation</param>
        /// <param name="localiseOnly">whether we are mapping or localising</param>
        /// <returns>localisation matching score</returns>
        public float Insert(
            evidenceRay ray,
            particlePose origin,
            rayModelLookup sensormodel_lookup,
            pos3D left_camera_location,
            pos3D right_camera_location,
            bool localiseOnly)
        {
            float matchingScore = 0;

            switch (grid_type)
            {
                case TYPE_MULTI_HYPOTHESIS:
                    {
                        for (int i = 0; i < grid.Length; i++)
                        {
                            occupancygridMultiHypothesis grd = (occupancygridMultiHypothesis)grid[i];
                            matchingScore += grid_weight[i] * grd.Insert(ray, origin, sensormodel_lookup, left_camera_location, right_camera_location, localiseOnly);
                        }
                        break;
                    }
            }
            return (matchingScore);
        }

        /// <summary>
        /// insert a stereo ray into the grid
        /// </summary>
        /// <param name="ray">stereo ray</param>
        /// <param name="sensormodel_lookup">sensor model to be used</param>
        /// <param name="left_camera_location">left stereo camera position and orientation</param>
        /// <param name="right_camera_location">right stereo camera position and orientation</param>
        /// <param name="localiseOnly">whether we are mapping or localising</param>
        /// <returns>localisation matching score</returns>
        public float Insert(
            evidenceRay ray,
            rayModelLookup sensormodel_lookup,
            pos3D left_camera_location,
            pos3D right_camera_location,
            bool localiseOnly)
        {
            float matchingScore = 0;
            switch (grid_type)
            {
                case TYPE_SIMPLE:
                {
                    for (int i = 0; i < grid.Length; i++)
                    {
                        occupancygridSimple grd = (occupancygridSimple)grid[i];
                        matchingScore += grid_weight[i] * grd.Insert(ray, sensormodel_lookup, left_camera_location, right_camera_location, localiseOnly);
                    }
                    break;
                }                
            }
            return (matchingScore);
        }

        #endregion

        #region "localisation"
        
		/// <summary>
		/// creates a set of random poses
		/// </summary>
		/// <param name="no_of_poses">number of poses to be created</param>
		/// <param name="sampling_radius_major_mm">major axis of the ellipse</param>
		/// <param name="sampling_radius_minor_mm">minor axis of the ellipse</param>
		/// <param name="pan">current pan angle</param>
		/// <param name="tilt">current tilt angle</param>
		/// <param name="roll">current roll angle</param>
		/// <param name="max_orientation_variance">maximum variance around the current pan angle</param>
		/// <param name="max_tilt_variance">maximum variance around the current tilt angle</param>
		/// <param name="max_roll_variance">maximum variance around the current roll angle</param>
		/// <param name="rnd">random number generator</param>
		/// <param name="poses">list of poses</param>
		static public void CreatePoses(
		    int no_of_poses,
		    float sampling_radius_major_mm,
		    float sampling_radius_minor_mm,
		    float pan,
		    float tilt,
		    float roll,
		    float max_orientation_variance,
		    float max_tilt_variance,
		    float max_roll_variance,
		    Random rnd,
            byte[] img,
            int img_width,
            int img_height,		                               
		    ref List<pos3D> poses)
		{
			if (poses == null) poses = new List<pos3D>();
			poses.Clear();
			
			float place_cell_area = (sampling_radius_minor_mm * sampling_radius_major_mm * 4) / no_of_poses;
			float place_cell_dimension = (float)Math.Sqrt(place_cell_area);
			place_cell_dimension *= 0.9f;
			
			int place_cell_x = 0;
			int place_cell_y = 0;
			int place_cells_across = (int)(sampling_radius_minor_mm*2/place_cell_dimension);
			int place_cells_down = (int)(sampling_radius_major_mm*2/place_cell_dimension);
			float place_cell_offset = 0;
			int half_place_cells_across = place_cells_across/2;
			int half_place_cells_down = place_cells_down/2;
			float place_cell_dimension_offset = place_cell_dimension*0.5f;
			float dist = 1;
			float x_offset = 0;
			float y_offset = 0;
			int ctr = 0;
			
			for (int i = 0; i < no_of_poses; i++)
			{	
			    int tries = 0;
			    dist = sampling_radius_minor_mm;
			    while ((dist >= sampling_radius_minor_mm) && (tries < half_place_cells_across))
			    {
					x_offset = place_cell_offset + ((place_cell_x - half_place_cells_across) * place_cell_dimension);
					y_offset = place_cell_dimension_offset + (place_cell_y - half_place_cells_down) * place_cell_dimension;
					
					place_cell_x++;
					if (place_cell_x >= place_cells_across)
					{
					    ctr = rnd.Next(2);
					    place_cell_x = 0;
					    place_cell_y++;
					    if (place_cell_offset == 0)
					        place_cell_offset = place_cell_dimension_offset;
					    else
					        place_cell_offset = 0;
					}
					
	                float dx = x_offset;
	                float dy = y_offset * sampling_radius_minor_mm / sampling_radius_major_mm;								
					dist = (float)Math.Sqrt(dx*dx+dy*dy);
				    tries++;
				}
				
				if (tries < half_place_cells_across)
				{					
					//Console.WriteLine("x,y: " + x_offset.ToString() + ", " + y_offset.ToString());
	
	                pos3D sample_pose = new pos3D(x_offset, y_offset, 0);
	                if (ctr % 2 == 0)
	                    sample_pose.pan = pan + ((float)rnd.NextDouble() * max_orientation_variance);
	                else
	                    sample_pose.pan = pan - ((float)rnd.NextDouble() * max_orientation_variance);
	                sample_pose.tilt = tilt + (((float)rnd.NextDouble() - 0.5f) * 2 * max_tilt_variance);
	                sample_pose.roll = roll + (((float)rnd.NextDouble() - 0.5f) * 2 * max_roll_variance);
					
					poses.Add(sample_pose);
					ctr++;
				}
			}
			
            // create an image showing the results
            if (img != null)
            {
                float max_radius = sampling_radius_major_mm * 0.025f;
                for (int i = img.Length - 1; i >= 0; i--) img[i] = 0;
                int tx = -(int)(sampling_radius_major_mm);
                int ty = -(int)(sampling_radius_major_mm);
                int bx = (int)(sampling_radius_major_mm);
                int by = (int)(sampling_radius_major_mm);

                for (int i = 0; i < poses.Count; i++)
                {
                    pos3D p = poses[i];
                    int x = (int)((p.x - tx) * img_width / (sampling_radius_major_mm*2));
                    int y = (int)((p.y - ty) * img_height / (sampling_radius_major_mm*2));
                    int radius = (int)(max_radius * img_width / (sampling_radius_major_mm*2));
					int r = (int)(((p.pan - pan) - (-max_orientation_variance)) * 255 / (max_orientation_variance*2));
					int g = 255 - r;
					int b = 0;
					//Console.WriteLine("x,y,r: " + x.ToString() + ", " + y.ToString() + ", " + r.ToString());
					drawing.drawSpot(img, img_width, img_height, x, y, radius, r, g, b);
                }
            }
			
		}
		
		/// <summary>
		/// given a set of poses and their matching scores return the best pose
		/// </summary>
		/// <param name="poses">list of poses which have been evaluated</param>
		/// <param name="scores">localisation matching score for each pose</param>
		/// <param name="best_pose">returned best pose</param>
		/// <param name="sampling_radius_major_mm">major axis length</param>
		/// <param name="img">optional image data</param>
		/// <param name="img_width">image width</param>
		/// <param name="img_height">image height</param>
        static public void FindBestPose(
            List<pos3D> poses,
            List<float> scores,
            ref pos3D best_pose,
            float sampling_radius_major_mm,
            byte[] img_poses, byte[] img_graph,
            int img_width,
            int img_height,
            float known_best_pose_x,
            float known_best_pose_y)
        {
			float peak_radius = sampling_radius_major_mm * 0.5f;
            float max_score = float.MinValue;
			float peak_x = 0;
			float peak_y = 0;
			float peak_z = 0;
            for (int i = 0; i < scores.Count; i++)
            {
                if (scores[i] > max_score)
                {
                    max_score = scores[i];
					peak_x = poses[i].x;
					peak_y = poses[i].y;
					peak_z = poses[i].z;
                }
            }
			
			float min_score = max_score * 0.5f;
			float min_score2 = max_score * 0.8f;
			
			if (best_pose == null) best_pose = new pos3D(0,0,0);
			best_pose.x = 0;
			best_pose.y = 0;
			best_pose.z = 0;
			best_pose.pan = 0;
			best_pose.tilt = 0;
			best_pose.roll = 0;
			float hits = 0;
			for (int i = 0; i < poses.Count; i++)
			{
			    if (scores[i] > min_score2)
			    {
					float dx = poses[i].x - peak_x;
					float dy = poses[i].y - peak_y;
					float dz = poses[i].z - peak_z;
					//if (Math.Abs(dx) < peak_radius)
					{
					    //if (Math.Abs(dy) < peak_radius)
					    {
					        //if (Math.Abs(dz) < peak_radius)
					        {
								float score = Math.Abs(scores[i]);
								score *= score;
								best_pose.x += poses[i].x * score;
								best_pose.y += poses[i].y * score;
								best_pose.z += poses[i].z * score;
								best_pose.pan += poses[i].pan * score;
								best_pose.tilt += poses[i].tilt * score;
								best_pose.roll += poses[i].roll * score;
								hits += score;
							}
						}
					}
				}
			}
			if (hits > 0)
			{
				best_pose.x /= hits;
				best_pose.y /= hits;
				best_pose.z /= hits;
				best_pose.pan /= hits;
				best_pose.tilt /= hits;
				best_pose.roll /= hits;				
			}
			
			float grad = 1;
			if (Math.Abs(best_pose.x) < 0.0001f) grad = best_pose.y / best_pose.x;
			float d1 = (float)Math.Sqrt(best_pose.x*best_pose.x + best_pose.y*best_pose.y);
			float origin_x = best_pose.x*sampling_radius_major_mm*5/d1; //sampling_radius_major_mm*best_pose.x/Math.Abs(best_pose.x);
			float origin_y = best_pose.y*sampling_radius_major_mm*5/d1;
			
			List<float> points = new List<float>();
			float points_min_distance = float.MaxValue;
			float points_max_distance = float.MinValue;
			float dxx = best_pose.y;
			float dyy = best_pose.x;
			float dist_to_best_pose = (float)Math.Sqrt(dxx*dxx + dyy*dyy);
			float max_score2 = 0;
			for (int i = 0; i < poses.Count; i++)
			{
			    if (scores[i] > min_score)
			    {
				    float ix = 0;
				    float iy = 0;
				    geometry.intersection(
				        0, 0, best_pose.x, best_pose.y,
				        poses[i].x-dxx, poses[i].y-dyy, poses[i].x, poses[i].y,
				        ref ix, ref iy);
				        
				    float dxx2 = ix - origin_x;
				    float dyy2 = iy - origin_y;
				    float dist = (float)Math.Sqrt(dxx2*dxx2 + dyy2*dyy2);
				    float score = scores[i];
				    points.Add(dist);
				    points.Add(score);
				    if (score > max_score2) max_score2 = score;
				    if (dist < points_min_distance) points_min_distance = dist;
				    if (dist > points_max_distance) points_max_distance = dist;
			    }
			}
			

            // create an image showing the results
            if (img_poses != null)
            {
                float max_radius = sampling_radius_major_mm * 0.1f;
                for (int i = img_poses.Length - 1; i >= 0; i--)
                {
                    img_poses[i] = 0;
                    img_graph[i] = 255;
                }
                int tx = -(int)(sampling_radius_major_mm);
                int ty = -(int)(sampling_radius_major_mm);
                int bx = (int)(sampling_radius_major_mm);
                int by = (int)(sampling_radius_major_mm);

  			    int origin_x2 = (int)((origin_x - tx) * img_width / (sampling_radius_major_mm*2));
                int origin_y2 = (int)((origin_y - ty) * img_height / (sampling_radius_major_mm*2));
  			    int origin_x3 = (int)((0 - tx) * img_width / (sampling_radius_major_mm*2));
                int origin_y3 = (int)((0 - ty) * img_height / (sampling_radius_major_mm*2));
                drawing.drawLine(img_poses, img_width, img_height, origin_x3, origin_y3, origin_x2, origin_y2, 255, 255, 0, 0, false);

                for (int i = 0; i < poses.Count; i++)
                {
                    pos3D p = poses[i];
                    float score = scores[i];
                    int x = (int)((p.x - tx) * img_width / (sampling_radius_major_mm*2));
                    int y = (int)((p.y - ty) * img_height / (sampling_radius_major_mm*2));
                    int radius = (int)(score * max_radius / max_score);
					byte intensity_r = (byte)(score * 255 / max_score);
					byte intensity_g = intensity_r;
					byte intensity_b = intensity_r;
					if (score < min_score)
					{
					    intensity_r = 0;
					    intensity_g = 0;
					}
					if ((score >= min_score) &&
					    (score < max_score - ((max_score - min_score)*0.5f)))
					{
					    intensity_g = 0;
					}
					drawing.drawSpot(img_poses, img_width, img_height, x,y,radius,intensity_r, intensity_g, intensity_b);
                }

				int best_x = (int)((best_pose.x - tx) * img_width / (sampling_radius_major_mm*2));
                int best_y = (int)((best_pose.y - ty) * img_height / (sampling_radius_major_mm*2));
				drawing.drawCross(img_poses, img_width, img_height, best_x,best_y,(int)max_radius, 255, 0, 0, 1);

  			    int known_best_x = (int)((known_best_pose_x - tx) * img_width / (sampling_radius_major_mm*2));
                int known_best_y = (int)((known_best_pose_y - ty) * img_height / (sampling_radius_major_mm*2));
				drawing.drawCross(img_poses, img_width, img_height, known_best_x,known_best_y,(int)max_radius, 0, 255, 0, 1);  			    

                // draw the graph								
				for (int i = 0; i < points.Count; i += 2)
				{
				    int graph_x = (int)((points[i] - points_min_distance) * (img_width-1) / (points_max_distance - points_min_distance));
				    int graph_y = img_height-1-((int)(points[i+1] * (img_height-1) / (max_score2*1.1f)));
				    drawing.drawCross(img_graph, img_width, img_height, graph_x, graph_y, 3, 0,0,0,0);
				}
            }
        }

        /// <summary>
        /// Localisation
        /// </summary>
        /// <param name="baseline_mm">baseline distance for each stereo camera in millimetres</param>
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="stereo_features">stereo features (disparities) for each stereo camera</param>
        /// <param name="stereo_features_colour">stereo feature colours for each stereo camera</param>
        /// <param name="stereo_features_uncertainties">stereo feature uncertainties (priors) for each stereo camera</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        /// <param name="left_camera_location">position and orientation of the left camera on each stereo camera</param>
        /// <param name="right_camera_location">position and orientation of the right camera on each stereo camera</param>
        /// <param name="no_of_samples">number of sample poses</param>
        /// <param name="sampling_radius_major_mm">major radius for samples, in the direction of robot movement</param>
        /// <param name="sampling_radius_minor_mm">minor radius for samples, perpendicular to the direction of robot movement</param>
        /// <param name="robot_pose">position and orientation of the robots centre of rotation</param>
        /// <param name="max_orientation_variance">maximum variance in orientation in radians, used to create sample poses</param>
        /// <param name="max_tilt_variance">maximum variance in tilt angle in radians, used to create sample poses</param>
        /// <param name="max_roll_variance">maximum variance in roll angle in radians, used to create sample poses</param>
        /// <param name="poses">list of poses tried</param>
        /// <param name="pose_score">list of pose matching scores</param>
        /// <param name="best_robot_pose">returned best pose estimate</param>
		/// <param name="rnd">random number generator</param>
        /// <returns>best localisation matching score</returns>
        public float Localise(
            float[] baseline_mm,
            int[] image_width,
            int[] image_height,
            float[] FOV_degrees,
		    float[][] stereo_features,
		    byte[][,] stereo_features_colour,
		    float[][] stereo_features_uncertainties,
            stereoModel[] sensormodel,
            pos3D[] left_camera_location,
            pos3D[] right_camera_location,
            int no_of_samples,
            float sampling_radius_major_mm,
            float sampling_radius_minor_mm,
            pos3D robot_pose,
            float max_orientation_variance,
            float max_tilt_variance,
            float max_roll_variance,
            List<pos3D> poses,
            List<float> pose_score,
		    Random rnd,
            ref pos3D best_robot_pose)
        {
            float best_matching_score = float.MinValue;

            poses.Clear();
            pose_score.Clear();
			
            CreatePoses(
			    no_of_samples,
		        sampling_radius_major_mm,
		        sampling_radius_minor_mm,
		        robot_pose.pan,
		        robot_pose.tilt,
		        robot_pose.roll,
		        max_orientation_variance,
		        max_tilt_variance,
		        max_roll_variance,
			    rnd,
			    null, 0, 0,
		        ref poses);

            // positions of the left and right camera relative to the robots centre of rotation
            pos3D[] relative_left_cam = new pos3D[left_camera_location.Length];
            pos3D[] relative_right_cam = new pos3D[right_camera_location.Length];
            for (int cam = 0; cam < baseline_mm.Length; cam++)
            {
                relative_left_cam[cam] = left_camera_location[cam].Subtract(robot_pose);
                relative_right_cam[cam] = right_camera_location[cam].Subtract(robot_pose);
            }

            pos3D stereo_camera_centre = new pos3D(0, 0, 0);

            // try a number of random poses
            for (int i = 0; i < no_of_samples; i++)
            {
                pos3D sample_pose = poses[i];
                
                float matching_score = 0;

                for (int cam = 0; cam < baseline_mm.Length; cam++)
                {
					// update the position of the left camera for this pose
                    pos3D sample_pose_left_cam = relative_left_cam[cam].add(sample_pose);
                    sample_pose_left_cam.pan = 0;
                    sample_pose_left_cam.tilt = 0;
                    sample_pose_left_cam.roll = 0;
                    sample_pose_left_cam = sample_pose_left_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);
                    sample_pose_left_cam.x += robot_pose.x;
                    sample_pose_left_cam.y += robot_pose.y;
                    sample_pose_left_cam.z += robot_pose.z;

					// update the position of the right camera for this pose
                    pos3D sample_pose_right_cam = relative_right_cam[cam].add(sample_pose);
                    sample_pose_right_cam.pan = 0;
                    sample_pose_right_cam.tilt = 0;
                    sample_pose_right_cam.roll = 0;
                    sample_pose_right_cam = sample_pose_right_cam.rotate(sample_pose.pan, sample_pose.tilt, sample_pose.roll);
                    sample_pose_right_cam.x += robot_pose.x;
                    sample_pose_right_cam.y += robot_pose.y;
                    sample_pose_right_cam.z += robot_pose.z;

                    // centre position between the left and right cameras
                    stereo_camera_centre.x = sample_pose_left_cam.x + ((sample_pose_right_cam.x - sample_pose_left_cam.x) * 0.5f);
                    stereo_camera_centre.y = sample_pose_left_cam.y + ((sample_pose_right_cam.y - sample_pose_left_cam.y) * 0.5f);
                    stereo_camera_centre.z = sample_pose_left_cam.z + ((sample_pose_right_cam.z - sample_pose_left_cam.z) * 0.5f);
                    stereo_camera_centre.pan = sample_pose.pan;
                    stereo_camera_centre.tilt = sample_pose.tilt;
                    stereo_camera_centre.roll = sample_pose.roll;

                    // create a set of stereo rays as observed from this pose
                    List<evidenceRay> rays = sensormodel[cam].createObservation(
                        stereo_camera_centre,
                        baseline_mm[cam],
                        image_width[cam],
                        image_height[cam],
                        FOV_degrees[cam],
                        stereo_features[cam],
                        stereo_features_colour[cam],
                        stereo_features_uncertainties[cam],
                        true);

					// insert rays into the occupancy grid
                    for (int r = 0; r < rays.Count; r++)
                    {
                        matching_score += Insert(rays[r], sensormodel[cam].ray_model, sample_pose_left_cam, sample_pose_right_cam, true);
                    }
                }

                // add the pose to the list
				sample_pose.pan -= robot_pose.pan;
				sample_pose.tilt -= robot_pose.tilt;
				sample_pose.roll -= robot_pose.roll;
                pose_score.Add(matching_score);
            }

			// locate the best possible pose
            FindBestPose(poses, pose_score, ref best_robot_pose, sampling_radius_major_mm, null, null, 0, 0, 0, 0);

            return (best_matching_score);
        }

        #endregion

    }
}
