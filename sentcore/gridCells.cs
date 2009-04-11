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
		/// <param name="k">scaling factor</param>
		/// <param name="scaling">returned moire grid scaling</param>
		/// <param name="orientation">returned moiré grid orientation in radians</param>
		public static void MoireGrid(
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    float k,
		    ref float scaling,
		    ref float orientation)
		{
			// spacing factor between the two grids
			float alpha = (first_grid_spacing - second_grid_spacing) / first_grid_spacing;
			alpha /= k;
			
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

		public static void CreateMoireGrid(
		    float sampling_radius_major_mm,
		    float sampling_radius_minor_mm,
		    int no_of_poses,
		    float pan,
		    float tilt,
		    float roll,
		    float max_orientation_variance,
		    float max_tilt_variance,
		    float max_roll_variance,
		    Random rnd,
		    ref List<pos3D> cells,
		    byte[] img_basic,
		    byte[] img_moire,
		    int img_width,
		    int img_height)
		{
		    float first_grid_spacing = sampling_radius_minor_mm / (float)Math.Sqrt(no_of_poses);
		    float second_grid_spacing = first_grid_spacing * 1.1f;
		    float first_grid_rotation_degrees = 0;
		    float second_grid_rotation_degrees = 10;
		    float scaling_factor = 0.3f;		    

		    int dimension_x_cells = 50;
		    int dimension_y_cells = 50;

            int cells_percent = 0;
            
            cells.Clear();
            int tries = 0;
            while (((cells_percent < 90) || (cells_percent > 110)) &&
                   (tries < 10))
            {
			    CreateMoireGrid(
			        sampling_radius_major_mm,
			        sampling_radius_minor_mm,
			        first_grid_spacing,
			        second_grid_spacing,
			        first_grid_rotation_degrees,
			        second_grid_rotation_degrees,
			        dimension_x_cells,
			        dimension_y_cells,
			        scaling_factor,
			        ref cells);
			        
			    cells_percent = cells.Count * 100 / no_of_poses;
			    
			    if (cells_percent < 90) scaling_factor *= 0.9f;
			    if (cells_percent > 110) scaling_factor *= 1.1f;
		        tries++;
		        
		        Console.WriteLine("Cells = " + cells.Count.ToString());
		    }
		    
		    for (int i = cells.Count-1; i >= 0; i--)
		    {
		        pos3D sample_pose = cells[i];
                if (i % 2 == 0)
                    sample_pose.pan = pan + ((float)rnd.NextDouble() * max_orientation_variance);
                else
                    sample_pose.pan = pan - ((float)rnd.NextDouble() * max_orientation_variance);
                sample_pose.tilt = tilt + (((float)rnd.NextDouble() - 0.5f) * 2 * max_tilt_variance);
                sample_pose.roll = roll + (((float)rnd.NextDouble() - 0.5f) * 2 * max_roll_variance);
		    }
		    
            // create an image showing the results
            if (img_basic != null)
            {
                ShowMoireGrid(
		            sampling_radius_major_mm,
		            sampling_radius_minor_mm,
		            first_grid_spacing,
		            second_grid_spacing,
		            first_grid_rotation_degrees,
		            second_grid_rotation_degrees,
		            dimension_x_cells*2,
		            dimension_y_cells*2,
		            scaling_factor,
		            img_moire,
		            img_width,
		            img_height);
            
                float max_radius = sampling_radius_major_mm * 0.025f;
                for (int i = img_basic.Length - 1; i >= 0; i--) img_basic[i] = 0;
                int tx = -(int)(sampling_radius_major_mm);
                int ty = -(int)(sampling_radius_major_mm);
                int bx = (int)(sampling_radius_major_mm);
                int by = (int)(sampling_radius_major_mm);

                for (int i = cells.Count - 1; i >= 0; i--)
                {
                    pos3D p = cells[i];
                    int x = ((img_width - img_height)/2) + (int)((p.x - tx) * img_height / (sampling_radius_major_mm*2));
                    int y = (int)((p.y - ty) * img_height / (sampling_radius_major_mm*2));
                    int radius = (int)(max_radius * img_width / (sampling_radius_major_mm*2));
					int r = (int)(((p.pan - pan) - (-max_orientation_variance)) * 255 / (max_orientation_variance*2));
					int g = 255 - r;
					int b = 0;
					drawing.drawSpot(img_basic, img_width, img_height, x, y, radius, r, g, b);
                }
            }
		    
        }
								
		/// <summary>
		/// Returns positions of grid cells corresponding to a moire grid
		/// produced by the interference of a pair of hexagonal grids (theta waves)
		/// </summary>
		/// <param name="first_grid_spacing">spacing of the first hexagonal grid (theta wavelength 1)</param>
		/// <param name="second_grid_spacing">spacing of the second hexagonal grid (theta wavelength 2)</param>
		/// <param name="phase_major_degrees">phase precession in the direction of motion in degrees</param>
		/// <param name="phase_minor_degrees">phase precession perpendicular to the direction of motion in degrees</param>
		/// <param name="first_grid_rotation_degrees">rotation of the first grid (theta field 1)</param>
		/// <param name="second_grid_rotation_degrees">rotation of the second grid (theta field 2)</param>
		/// <param name="dimension_x_cells">number of grid cells in the x axis</param>
		/// <param name="dimension_y_cells">number of grid cells in the y axis</param>
		/// <param name="scaling_factor">scaling factor (k)</param>
		/// <param name="cells">returned grid cell positions</param>
		public static void CreateMoireGrid(
		    float sampling_radius_major_mm,
		    float sampling_radius_minor_mm,
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    int dimension_x_cells,
		    int dimension_y_cells,
		    float scaling_factor,
		    ref List<pos3D> cells)
		{
			cells.Clear();
			float scaling = 0;
			float orientation = 0;
			float dx, dy;
			
			// compute scaling and orientation of the grid
            MoireGrid(
		        first_grid_spacing,
		        second_grid_spacing,
		        first_grid_rotation_degrees,
		        second_grid_rotation_degrees,
		        scaling_factor,
		        ref scaling,
		        ref orientation);
			
			float moire_grid_spacing = first_grid_spacing * scaling;
			float half_grid_spacing = moire_grid_spacing/2;
						
			float radius_sqr = sampling_radius_minor_mm*sampling_radius_minor_mm;
			
			Console.WriteLine("moire_grid_spacing: " + moire_grid_spacing.ToString());
			Console.WriteLine("radius_sqr: " + radius_sqr.ToString());			
						
			float half_x = dimension_x_cells * moire_grid_spacing / 2.0f;
			float half_y = dimension_y_cells * moire_grid_spacing / 2.0f;
			for (int cell_x = 0; cell_x < dimension_x_cells; cell_x++)
			{
			    for (int cell_y = 0; cell_y < dimension_y_cells; cell_y++)
			    {			    
				    float x = (cell_x * moire_grid_spacing) - half_x;
				    if (cell_y % 2 == 0) x += half_grid_spacing;
				    float y = (cell_y * moire_grid_spacing) - half_y;
				    pos3D grid_cell = new pos3D(x, y, 0);
				    grid_cell = grid_cell.rotate(-orientation, 0, 0);
				    
				    dx = grid_cell.x;
				    dy = (grid_cell.y * sampling_radius_minor_mm) / sampling_radius_major_mm;
				    
				    float dist = dx*dx + dy*dy;
				    if (dist <= radius_sqr)
				    {
					    cells.Add(grid_cell);
					}
				}
			}
		}

		/// <summary>
		/// Returns positions of grid cells corresponding to a moire grid
		/// produced by the interference of a pair of hexagonal grids (theta waves)
		/// </summary>
		/// <param name="sampling_radius_major_mm">radius of the major axis of the bounding ellipse</param>
		/// <param name="sampling_radius_minor_mm">radius of the minor axis of the bounding ellipse</param>
		/// <param name="first_grid_spacing">spacing of the first hexagonal grid (theta wavelength 1)</param>
		/// <param name="second_grid_spacing">spacing of the second hexagonal grid (theta wavelength 2)</param>
		/// <param name="first_grid_rotation_degrees">rotation of the first grid (theta field 1)</param>
		/// <param name="second_grid_rotation_degrees">rotation of the second grid (theta field 2)</param>
		/// <param name="dimension_x_cells">number of grid cells in the x axis</param>
		/// <param name="dimension_y_cells">number of grid cells in the y axis</param>
		/// <param name="scaling_factor">scaling factor (k)</param>
		/// <param name="img">image data</param>
		/// <param name="img_width">image width</param>
		/// <param name="img_height">image height</param>
		/// <param name="radius">radius in pixels</param>
		public static void ShowMoireGridVertices(
		    float sampling_radius_major_mm,
		    float sampling_radius_minor_mm,
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    int dimension_x_cells,
		    int dimension_y_cells,
		    float scaling_factor,
		    byte[] img,
		    int img_width,
		    int img_height,
		    int radius)
		{
		    List<pos3D> cells = new List<pos3D>();
		    
            CreateMoireGrid(
		        sampling_radius_major_mm,
		        sampling_radius_minor_mm,
		        first_grid_spacing,
		        second_grid_spacing,
		        first_grid_rotation_degrees,
		        second_grid_rotation_degrees,
		        dimension_x_cells,
		        dimension_y_cells,
		        scaling_factor,
		        ref cells);
		        
		    float min_x = float.MaxValue;
		    float max_x = float.MinValue;
		    float min_y = float.MaxValue;
		    float max_y = float.MinValue;
		    
		    for (int i = 0; i < cells.Count; i++)
		    {
		        if (cells[i].x < min_x) min_x = cells[i].x;
		        if (cells[i].y < min_y) min_y = cells[i].y;
		        if (cells[i].x > max_x) max_x = cells[i].x;
		        if (cells[i].y > max_y) max_y = cells[i].y;
		    }
		    
		    if (max_x - min_x > max_y - min_y)
		    {
		        float cy = min_y + ((max_y - min_y)/2);
		        min_y = cy - ((max_x - min_x)/2);
		        max_y = min_y + (max_x - min_x);
		    }
		    else
		    {
		        float cx = min_x + ((max_x - min_x)/2);
		        min_x = cx - ((max_y - min_y)/2);
		        max_x = min_x + (max_y - min_y);
		    }
		    
		    for (int i = (img_width * img_height * 3)-1; i >= 0; i--) img[i] = 0;
		    
		    for (int i = 0; i < cells.Count; i++)
		    {
		        pos3D cell = cells[i];
		        int x = (int)((cell.x - min_x) * img_width / (max_x - min_x));
		        int y = (int)((cell.y - min_y) * img_height / (max_y - min_y));
		        drawing.drawSpot(img, img_width, img_height, x, y, radius, 255,255,255);
            }		    
        }
								
		/// <summary>
		/// Returns positions of grid cells corresponding to a moire grid
		/// produced by the interference of a pair of hexagonal grids (theta waves)
		/// </summary>
		/// <param name="sampling_radius_major_mm">radius of the major axis of the bounding ellipse</param>
		/// <param name="sampling_radius_minor_mm">radius of the minor axis of the bounding ellipse</param>
		/// <param name="first_grid_spacing">spacing of the first hexagonal grid (theta wavelength 1)</param>
		/// <param name="second_grid_spacing">spacing of the second hexagonal grid (theta wavelength 2)</param>
		/// <param name="first_grid_rotation_degrees">rotation of the first grid (theta field 1)</param>
		/// <param name="second_grid_rotation_degrees">rotation of the second grid (theta field 2)</param>
		/// <param name="dimension_x_cells">number of grid cells in the x axis</param>
		/// <param name="dimension_y_cells">number of grid cells in the y axis</param>
		/// <param name="img">image data</param>
		/// <param name="img_width">image width</param>
		/// <param name="img_height">image height</param>
		public static void ShowMoireGrid(
		    float sampling_radius_major_mm,
		    float sampling_radius_minor_mm,
		    float first_grid_spacing,
		    float second_grid_spacing,
		    float first_grid_rotation_degrees,
		    float second_grid_rotation_degrees,
		    int dimension_x_cells,
		    int dimension_y_cells,
		    float scaling_factor,
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
			float radius_sqr = sampling_radius_minor_mm*sampling_radius_minor_mm;
			
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
				        x *= scaling_factor;
				        y *= scaling_factor;
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
			if (radius < 2) radius = 2;
			int r,g,b;
			for (int grd = 0; grd < 2; grd++)
			{			
				float[,,] grid = grid1;
				r = 5;
				g = -1;
				b = -1;
				if (grd == 1)
				{
					grid = grid2;
					r = -1;
					g = 5;
					b = -1;
				}
				
			    for (int cell_x = 0; cell_x < dimension_x_cells; cell_x++)
			    {
			        for (int cell_y = 0; cell_y < dimension_y_cells; cell_y++)
			        {
					    int x = ((img_width-img_height)/2) + (int)((grid[cell_x, cell_y, 0] - min_x) * img_height / (max_x - min_x));
					    int y = (int)((grid[cell_x, cell_y, 1] - min_y) * img_height / (max_y - min_y));
						
				        float dx = grid[cell_x, cell_y, 0];
				        float dy = (grid[cell_x, cell_y, 1] * sampling_radius_minor_mm) / sampling_radius_major_mm;
				    
				        float dist = dx*dx + dy*dy;
				        if (dist <= radius_sqr)
				        {
						    drawing.drawSpotOverlay(img, img_width, img_height, x,y,radius,r,g,b);
				        }
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
            float min_score = float.MaxValue;
			float peak_x = 0;
			float peak_y = 0;
			float peak_z = 0;
            for (int i = 0; i < scores.Count; i++)
            {
                if (scores[i] < min_score)
                {
                    min_score = scores[i];
                }
                if (scores[i] > max_score)
                {
                    max_score = scores[i];
					peak_x = poses[i].x;
					peak_y = poses[i].y;
					peak_z = poses[i].z;
                }
            }

            float score_range = max_score - min_score;
			float minimum_score = min_score + (score_range * 0.5f);
            float minimum_score2 = min_score + (score_range * 0.8f);
			
			if (best_pose == null) best_pose = new pos3D(0,0,0);
			best_pose.x = 0;
			best_pose.y = 0;
			best_pose.z = 0;
			best_pose.pan = 0;
			best_pose.tilt = 0;
			best_pose.roll = 0;
			float hits = 0;
            if (score_range > 0)
            {
                for (int i = 0; i < poses.Count; i++)
                {
                    if (scores[i] > minimum_score2)
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
                                    float score = (scores[i] - min_score) / score_range;
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
            if (d1 < 0.001f) d1 = 0.001f;
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
			    if (scores[i] > minimum_score)
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
            if ((img_poses != null) && (score_range > 0))
            {
                float max_radius = sampling_radius_major_mm * 0.1f;
                for (int i = img_poses.Length - 1; i >= 0; i--)
                {
                    img_poses[i] = 0;
                    if (img_graph != null) img_graph[i] = 255;
                }
                int tx = -(int)(sampling_radius_major_mm);
                int ty = -(int)(sampling_radius_major_mm);
                int bx = (int)(sampling_radius_major_mm);
                int by = (int)(sampling_radius_major_mm);

  			    int origin_x2 = (int)((origin_x - tx) * img_width / (sampling_radius_major_mm*2));
                int origin_y2 = img_height - 1 - (int)((origin_y - ty) * img_height / (sampling_radius_major_mm*2));
  			    int origin_x3 = (int)((0 - tx) * img_width / (sampling_radius_major_mm*2));
                int origin_y3 = img_height - 1 - (int)((0 - ty) * img_height / (sampling_radius_major_mm * 2));
                drawing.drawLine(img_poses, img_width, img_height, origin_x3, origin_y3, origin_x2, origin_y2, 255, 255, 0, 1, false);

                for (int i = 0; i < poses.Count; i++)
                {
                    pos3D p = poses[i];
                    float score = scores[i];
                    int x = (int)((p.x - tx) * img_width / (sampling_radius_major_mm*2));
                    int y = img_height - 1 - (int)((p.y - ty) * img_height / (sampling_radius_major_mm * 2));
                    int radius = (int)((score - min_score) * max_radius / score_range);
					byte intensity_r = (byte)((score - min_score) * 255 / score_range);
					byte intensity_g = intensity_r;
					byte intensity_b = intensity_r;
					if (score < minimum_score)
					{
					    intensity_r = 0;
					    intensity_g = 0;
					}
					if ((score >= minimum_score) &&
					    (score < max_score - ((max_score - minimum_score)*0.5f)))
					{
					    intensity_g = 0;
					}
					drawing.drawSpot(img_poses, img_width, img_height, x,y,radius,intensity_r, intensity_g, intensity_b);
                }

				int best_x = (int)((best_pose.x - tx) * img_width / (sampling_radius_major_mm*2));
                int best_y = img_height - 1 - (int)((best_pose.y - ty) * img_height / (sampling_radius_major_mm * 2));
				drawing.drawCross(img_poses, img_width, img_height, best_x,best_y,(int)max_radius, 255, 0, 0, 1);

  			    int known_best_x = (int)((known_best_pose_x - tx) * img_width / (sampling_radius_major_mm*2));
                int known_best_y = img_height - 1 - (int)((known_best_pose_y - ty) * img_height / (sampling_radius_major_mm * 2));
				drawing.drawCross(img_poses, img_width, img_height, known_best_x,known_best_y,(int)max_radius, 0, 255, 0, 1);

                if (img_graph != null)
                {
                    // draw the graph								
                    for (int i = 0; i < points.Count; i += 2)
                    {
                        int graph_x = (int)((points[i] - points_min_distance) * (img_width - 1) / (points_max_distance - points_min_distance));
                        int graph_y = img_height - 1 - ((int)(points[i + 1] * (img_height - 1) / (max_score2 * 1.1f)));
                        drawing.drawCross(img_graph, img_width, img_height, graph_x, graph_y, 3, 0, 0, 0, 0);
                    }
                }
            }
        }
		
        #endregion
		
	}
}
