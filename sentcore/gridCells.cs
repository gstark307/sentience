/*
    Grid cells
    Copyright (C) 2009 Bob Mottram
    fuzzgun@gmail.com
        
    References:
        
    Scale-Invariant Memory Representations Emerge from Moiré 
    Interference between Grid Fields That Produce Theta Oscillations: 
    A Computational Model, Hugh T. Blair, Adam C. Welday, and Kechen Zhang
    http://www.jneurosci.org/cgi/content/abstract/27/12/3211

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
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;

namespace sentience.core
{	
	public class gridCells
	{
        #region "Moiré grids"
		
		/// <summary>
		/// Computes the scaling and orientation of a moiré grid as the interference
		/// pattern between two hexagonal grids
		/// </summary>
		/// <param name="first_grid_spacing">spacing of the first grid</param>
		/// <param name="second_grid_spacing">spacing of the second grid</param>
		/// <param name="first_grid_rotation_degrees">rotation of the first grid</param>
		/// <param name="second_grid_rotation_degrees">rotation of the second grid</param>
		/// <param name="scaling">returned moire grid scaling</param>
		/// <param name="orientation">returned moiré grid orientation in radians</param>
		public static void MoireGrid(
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    ref float scaling,
		    ref float orientation)
		{
			// spacing factor between the two grids
			float alpha = (first_grid_spacing - second_grid_spacing) / first_grid_spacing;
			
			// compute orientation of the moire grid
			float theta1 = first_grid_rotation_degrees * (float)Math.PI / 180;
			float theta2 = second_grid_rotation_degrees * (float)Math.PI / 180;						
			float theta = (float)Math.Atan(
			    (Math.Sin(theta2) - ((1.0f + alpha)*Math.Sin(theta1))) /
			    (Math.Cos(theta2) - ((1.0f + alpha)*Math.Cos(theta1))));
			orientation = theta;
			
			float phi_degrees = first_grid_rotation_degrees - second_grid_rotation_degrees;
			if (Math.Abs(60 - phi_degrees) < Math.Abs(phi_degrees))
				phi_degrees = 60 - phi_degrees;
			float eta = phi_degrees * (float)Math.PI / 180.0f;
			
			scaling = (1.0f + alpha) / 
				      (float)Math.Sqrt((alpha*alpha) + (2 * (1.0f - Math.Cos(eta)) * (1.0f + alpha)));			
		}
		
		/// <summary>
		/// Returns positions of grid cells corresponding to a moire grid
		/// produced by the interference of a pair of hexagonal grids (theta waves)
		/// </summary>
		/// <param name="first_grid_spacing">spacing of the first hexagonal grid (theta wavelength 1)</param>
		/// <param name="second_grid_spacing">spacing of the second hexagonal grid (theta wavelength 2)</param>
		/// <param name="first_grid_rotation_degrees">rotation of the first grid (theta field 1)</param>
		/// <param name="second_grid_rotation_degrees">rotation of the second grid (theta field 2)</param>
		/// <param name="dimension_x_cells">number of grid cells in the x axis</param>
		/// <param name="dimension_y_cells">number of grid cells in the y axis</param>
		/// <param name="cells">returned grid cell positions</param>
		public static void CreateMoireGrid(
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    int dimension_x_cells,
		    int dimension_y_cells,
		    ref List<pos3D> cells)
		{
			cells.Clear();
			float scaling = 0;
			float orientation = 0;
			
			// compute scaling and orientation of the grid
            MoireGrid(
		        first_grid_spacing,
		        second_grid_spacing,
		        first_grid_rotation_degrees,
		        second_grid_rotation_degrees,
		        ref scaling,
		        ref orientation);
			
			float moire_grid_spacing = first_grid_spacing * scaling;
			float half_grid_spacing = moire_grid_spacing/2;
			
			float half_x = dimension_x_cells * moire_grid_spacing / 2.0f;
			float half_y = dimension_y_cells * moire_grid_spacing / 2.0f;
			for (int cell_x = 0; cell_x < dimension_x_cells; cell_x++)
			{
			    for (int cell_y = 0; cell_y < dimension_y_cells; cell_y++)
			    {
				    float x = (cell_x * moire_grid_spacing) - half_x;
				    if (cell_x % 2 == 0) x += half_grid_spacing;
				    float y = (cell_y * moire_grid_spacing) - half_y;
					pos3D grid_cell = new pos3D(x, y, 0);
					grid_cell = grid_cell.rotate(orientation, 0, 0);
					cells.Add(grid_cell);
				}
			}
		}
		
		/// <summary>
		/// Returns positions of grid cells corresponding to a moire grid
		/// produced by the interference of a pair of hexagonal grids (theta waves)
		/// </summary>
		/// <param name="first_grid_spacing">spacing of the first hexagonal grid (theta wavelength 1)</param>
		/// <param name="second_grid_spacing">spacing of the second hexagonal grid (theta wavelength 2)</param>
		/// <param name="first_grid_rotation_degrees">rotation of the first grid (theta field 1)</param>
		/// <param name="second_grid_rotation_degrees">rotation of the second grid (theta field 2)</param>
		/// <param name="dimension_x_cells">number of grid cells in the x axis</param>
		/// <param name="dimension_y_cells">number of grid cells in the y axis</param>
		public static void ShowMoireGrid(
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    int dimension_x_cells,
		    int dimension_y_cells,
		    byte[] img,
		    int img_width,
		    int img_height)
		{
			float[,,] grid1 = new float[dimension_x_cells, dimension_y_cells, 2];
			float[,,] grid2 = new float[dimension_x_cells, dimension_y_cells, 2];
			float rotation = 0;
			float min_x = float.MaxValue;
			float max_x = float.MinValue;
			float min_y = float.MaxValue;
			float max_y = float.MinValue;
			
			for (int grd = 0; grd < 2; grd++)
			{
				float spacing = first_grid_spacing;
				if (grd == 1) spacing = second_grid_spacing;
				
				float half_grid_spacing = spacing/2;
				float half_x = spacing * dimension_x_cells / 2;
				float half_y = spacing * dimension_y_cells / 2;

				if (grd == 0)
					rotation = first_grid_rotation_degrees * (float)Math.PI / 180;
				else
					rotation = second_grid_rotation_degrees * (float)Math.PI / 180;
			
			    for (int cell_x = 0; cell_x < dimension_x_cells; cell_x++)
			    {				    
			        for (int cell_y = 0; cell_y < dimension_y_cells; cell_y++)
			        {
						float x = (cell_x * spacing) - half_x;				    
						if (cell_y % 2 == 0) x += half_grid_spacing;
				        float y = (cell_y * spacing) - half_y;
						pos3D p = new pos3D(x,y,0);
						p = p.rotate(rotation, 0, 0);
						if (grd == 0)
						{
							grid1[cell_x, cell_y, 0] = p.x;
							grid1[cell_x, cell_y, 1] = p.y;
						}
						else
						{
							grid2[cell_x, cell_y, 0] = p.x;
							grid2[cell_x, cell_y, 1] = p.y;
						}	
						if (p.x < min_x) min_x = p.x;
						if (p.y < min_y) min_y = p.y;
						if (p.x > max_x) max_x = p.x;
						if (p.y > max_y) max_y = p.y;
				    }
			    }
				
			}
			
			for (int i = (img_width * img_height * 3)-1; i >= 0; i--) img[i] = 0;
			
			int radius = img_width / (dimension_x_cells*350/100);
			int r,g,b;
			for (int grd = 0; grd < 2; grd++)
			{			
				float[,,] grid = grid1;
				r = 60;
				g = -1;
				b = -1;
				if (grd == 1)
				{
					grid = grid2;
					r = -1;
					g = 60;
					b = -1;
				}
				
			    for (int cell_x = 0; cell_x < dimension_x_cells; cell_x++)
			    {
			        for (int cell_y = 0; cell_y < dimension_y_cells; cell_y++)
			        {
					    int x = (int)((grid[cell_x, cell_y, 0] - min_x) * img_width / (max_x - min_x));
					    int y = (int)((grid[cell_x, cell_y, 1] - min_y) * img_height / (max_y - min_y));
						
						drawing.drawSpotOverlay(img, img_width, img_height, x,y,radius,r,g,b);
				    }
			    }
				
				for (int i = (img_width * img_height * 3)-3; i >= 2; i-=3) 
				{
					if ((img[i+2] > 0) && (img[i+1] > 0))
					{
						img[i+2] = 255;
						img[i+1] = 255;
					}
				}
			}
		}		
		
        #endregion		
		
        #region "generating possible poses"
		
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
		
        #endregion
		
        #region "localisation"
		
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
			float origin_x = best_pose.x*sampling_radius_major_mm*2/d1; //sampling_radius_major_mm*best_pose.x/Math.Abs(best_pose.x);
			float origin_y = best_pose.y*sampling_radius_major_mm*2/d1;
			
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
                drawing.drawLine(img_poses, img_width, img_height, origin_x3, origin_y3, origin_x2, origin_y2, 255, 255, 0, 1, false);

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
		
        #endregion
		
	}
}
