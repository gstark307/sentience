/*
    A buffer containing a number of metagrids
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
using System.Xml;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;
using sluggish.utilities.xml;
using Aced.Compression;

namespace sentience.core
{
	public class metagridBuffer
	{
		// buffer containing grids
		public metagrid[] buffer;
		
		// index number of the currently active grid
		protected int current_buffer_index;
		
		protected List<float> grid_centres;
		protected List<int> disparities_index;
		
		// index number of the current grid
		protected int current_grid_index;
		
		// index number in the disparities data
		protected int current_disparity_index;
		
		protected FileStream disparities_file;
		protected BinaryReader disparities_reader;
		protected bool disparities_file_open;
		
		public int dimension_mm;
		public int dimension_vertical_mm;
		public int no_of_grid_levels;
		public int grid_type;
		public int cellSize_mm;
		public int localisationRadius_mm;
        public int maxMappingRange_mm;
		public float vacancyWeighting;
		
		protected List<float> localisations;
		
        #region "constructor"
		
        /// <summary>
        /// initialise the buffer
        /// </summary>
        /// <param name="no_of_grid_levels">The number of sub grids</param>
        /// <param name="grid_type">the type of sub grids</param>
        /// <param name="dimension_mm">dimension of the smallest sub grid</param>
        /// <param name="dimension_vertical_mm">vertical dimension of the smallest sub grid</param>
        /// <param name="cellSize_mm">cell size of the smallest sub grid</param>
        /// <param name="localisationRadius_mm">localisation radius within the smallest sub grid</param>
        /// <param name="maxMappingRange_mm">maximum mapping radius within the smallest sub grid</param>
        /// <param name="vacancyWeighting">vacancy model weighting, typically between 0.2 and 2</param>
        public void Initialise(
            int no_of_grid_levels,
            int grid_type,
		    int dimension_mm, 
            int dimension_vertical_mm, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
        {
            this.dimension_mm = dimension_mm;
			this.dimension_vertical_mm = dimension_vertical_mm;
			this.no_of_grid_levels = no_of_grid_levels;
			this.grid_type = grid_type;
			this.cellSize_mm = cellSize_mm;
			this.localisationRadius_mm = localisationRadius_mm;
			this.maxMappingRange_mm = maxMappingRange_mm;
			this.vacancyWeighting = vacancyWeighting;
        
			// create the buffer
			buffer = new metagrid[2];
			for (int i = 0; i < 2; i++)
			{
				buffer[i] = 
					new metagrid(
				        no_of_grid_levels,
				        grid_type,
				        dimension_mm,
				        dimension_vertical_mm,
				        cellSize_mm,
				        localisationRadius_mm,
				        maxMappingRange_mm,
				        vacancyWeighting);
			}
			current_buffer_index = 0;
			current_grid_index = 0;
			grid_centres = new List<float>();
			localisations = new List<float>();
			disparities_index = new List<int>();
		}

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="no_of_grid_levels">The number of sub grids</param>
        /// <param name="grid_type">the type of sub grids</param>
        /// <param name="dimension_mm">dimension of the smallest sub grid</param>
        /// <param name="dimension_vertical_mm">vertical dimension of the smallest sub grid</param>
        /// <param name="cellSize_mm">cell size of the smallest sub grid</param>
        /// <param name="localisationRadius_mm">localisation radius within the smallest sub grid</param>
        /// <param name="maxMappingRange_mm">maximum mapping radius within the smallest sub grid</param>
        /// <param name="vacancyWeighting">vacancy model weighting, typically between 0.2 and 2</param>
        public metagridBuffer(
            int no_of_grid_levels,
            int grid_type,
		    int dimension_mm, 
            int dimension_vertical_mm, 
            int cellSize_mm, 
            int localisationRadius_mm, 
            int maxMappingRange_mm, 
            float vacancyWeighting)
        {
			Initialise(
                no_of_grid_levels,
                grid_type,
		        dimension_mm, 
                dimension_vertical_mm, 
                cellSize_mm, 
                localisationRadius_mm, 
                maxMappingRange_mm, 
                vacancyWeighting);
		}		

        public metagridBuffer()
		{
		}
		
        #endregion
		
		#region "loading and saving"
		
        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeBuffer = doc.CreateElement("MetaGridBuffer");
            parent.AppendChild(nodeBuffer);								
			
            xml.AddComment(doc, nodeBuffer, "The number of grid scales");
            xml.AddTextElement(doc, nodeBuffer, "NoOfGridLevels", Convert.ToString(no_of_grid_levels));
            xml.AddComment(doc, nodeBuffer, "Type of occupancy grid (0 = simple, 1 = multi hypothesis)");
            xml.AddTextElement(doc, nodeBuffer, "GridType", Convert.ToString(grid_type));
            xml.AddComment(doc, nodeBuffer, "Dimension of the smallest grid scale in millimetres");
            xml.AddTextElement(doc, nodeBuffer, "DimensionMillimetres", Convert.ToString(dimension_mm));
            xml.AddComment(doc, nodeBuffer, "Vertical dimension of the smallest grid scale in millimetres");
            xml.AddTextElement(doc, nodeBuffer, "VerticalDimensionMillimetres", Convert.ToString(dimension_vertical_mm));
            xml.AddComment(doc, nodeBuffer, "Occupancy grid cell size on millimetres");
            xml.AddTextElement(doc, nodeBuffer, "CellSizeMillimetres", Convert.ToString(cellSize_mm));
            xml.AddComment(doc, nodeBuffer, "Radius used for localisation rays in millimetres");
            xml.AddTextElement(doc, nodeBuffer, "LocalisationRadiusMillimetres", Convert.ToString(localisationRadius_mm));
            xml.AddComment(doc, nodeBuffer, "Radius used for mapping rays in millimetres");
            xml.AddTextElement(doc, nodeBuffer, "MappingRadiusMillimetres", Convert.ToString(maxMappingRange_mm));
            xml.AddComment(doc, nodeBuffer, "Weighting applied to vacancy model, typically in the range 0.3-1.0");
            xml.AddTextElement(doc, nodeBuffer, "VacancyWeighting", Convert.ToString(vacancyWeighting));
			
            return (nodeBuffer);
        }

        /// <summary>
        /// return an Xml document containing metagridBuffer
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlElement nodeGeom = doc.CreateElement("OccupancyGrids");
            doc.AppendChild(nodeGeom);
			
            nodeGeom.AppendChild(
			    getXml(doc, nodeGeom));

            return (doc);
        }

        /// <summary>
        /// save buffer as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(string filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }
		
        /// <summary>
        /// parse an xml node to extract buffer parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(
		    XmlNode xnod, int level,
            ref int no_of_grid_levels,
            ref int grid_type,
		    ref int dimension_mm,
            ref int dimension_vertical_mm,
            ref int cellSize_mm,
            ref int localisationRadius_mm,
            ref int maxMappingRange_mm,
            ref float vacancyWeighting)
        {
            XmlNode xnodWorking;

			if (xnod.Name == "NoOfGridLevels")
			{
				no_of_grid_levels = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "GridType")
			{
				grid_type = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "DimensionMillimetres")
			{
				dimension_mm = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "VerticalDimensionMillimetres")
			{
				dimension_vertical_mm = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "CellSizeMillimetres")
			{
				cellSize_mm = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "LocalisationRadiusMillimetres")
			{
				localisationRadius_mm = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "MappingRadiusMillimetres")
			{
				maxMappingRange_mm = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "VacancyWeighting")
			{
				vacancyWeighting = Convert.ToSingle(xnod.InnerText);
			}
			
            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1,
			            ref no_of_grid_levels,
			            ref grid_type,
					    ref dimension_mm,
			            ref dimension_vertical_mm,
			            ref cellSize_mm,
			            ref localisationRadius_mm,
			            ref maxMappingRange_mm,
			            ref vacancyWeighting);
					            
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }
		
        /// <summary>
        /// load buffer parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public bool Load(string filename)
        {
            bool loaded = false;

            if (File.Exists(filename))
            {
                // use an XmlTextReader to open an XML document
                XmlTextReader xtr = new XmlTextReader(filename);
                xtr.WhitespaceHandling = WhitespaceHandling.None;

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                xd.Load(xtr);

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
	            int no_of_grid_levels=0;
	            int grid_type=0;
			    int dimension_mm=0; 
	            int dimension_vertical_mm=0; 
	            int cellSize_mm=0; 
	            int localisationRadius_mm=0; 
	            int maxMappingRange_mm=0; 
	            float vacancyWeighting=0;
                LoadFromXml(xnodDE, 0,
	                ref no_of_grid_levels,
	                ref grid_type,
			        ref dimension_mm,
	                ref dimension_vertical_mm,
	                ref cellSize_mm,
	                ref localisationRadius_mm,
	                ref maxMappingRange_mm,
	                ref vacancyWeighting
				);
				
				Initialise(
	                no_of_grid_levels,
	                grid_type,
			        dimension_mm,
	                dimension_vertical_mm,
	                cellSize_mm, 
	                localisationRadius_mm,
	                maxMappingRange_mm, 
	                vacancyWeighting);
				                           
                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }		
		
		#endregion
			
		
        #region "getting cell sizes"
		
		/// <summary>
		/// returns the cell size for each grid within a metagrid
		/// </summary>
		/// <returns>cell sizes</returns>
		public List<int> GetCellSizes()
		{
			return(buffer[0].GetCellSizes());
		}
		
        #endregion
		
		#region "resetting"
		
		public void Reset()
		{
		    for (int i = 0; i < 2; i++) buffer[i].Clear();
		    grid_centres.Clear();
		    localisations.Clear();
		    disparities_index.Clear();
		    current_buffer_index = 0;
		    current_grid_index = 0;
		    current_disparity_index = 0;
			
			// close the disparities file if it is still open
			if (disparities_file_open)
			{				
				disparities_reader.Close();
				disparities_file.Close();
			}
		}
		
		#endregion
		
		#region "loading a path"
		
		// x,y,z coordinates along the path in millimetres
		protected List<float> path;
		
		// file containing stereo disparities observations taken along a path
		protected string disparities_filename;
		
        /// <summary>
        /// loads path data generated from odometry and stereo vision observations
        /// </summary>
        /// <param name="path_filename">file containing the path data (estimated position/orientation over time)</param>
        /// <param name="disparities_index_filename">file containing positions at which each stereo disparity set was observed</param>
        /// <param name="disparities_filename">file containing observed stereo disparities</param>
        public void LoadPath(
            string path_filename,
            string disparities_index_filename,
            string disparities_filename)
        {
            this.disparities_filename = disparities_filename;
            float half_dimension_sqr = dimension_mm * 0.5f;
            half_dimension_sqr *= half_dimension_sqr;
            
            if (File.Exists(path_filename))
            {                
                Reset();
                path = new List<float>();
                FileStream fs = File.Open(path_filename, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                int entries = br.ReadInt32();
                float last_x=0, last_y=0, last_z=0;
                for (int t = 0; t < entries; t++)
                {
                    OdometryData data = OdometryData.Read(br);
                    path.Add(data.x);
                    path.Add(data.y);
                    path.Add(0.0f);

                    //Console.WriteLine("x: " + data.x.ToString() + "  y: " + data.y.ToString());
                    
                    if (t == 0)
                    {
                        last_x = data.x;
                        last_y = data.y;
                        grid_centres.Add(data.x);
                        grid_centres.Add(data.y);
                        grid_centres.Add(0.0f);
                    }
                    else
                    {
                        float dx = data.x - last_x;
                        float dy = data.y - last_y;
                        float dist_sqr = dx*dx + dy*dy;
                        if (dist_sqr > half_dimension_sqr*150/100)
                        {
                            last_x = data.x;
                            last_y = data.y;
                            grid_centres.Add(data.x);
                            grid_centres.Add(data.y);
                            grid_centres.Add(0.0f);
                        }
                    }
                }

                br.Close();
                fs.Close();
                
	            // position the first two grids
	            if (grid_centres.Count >= 3)
	            {
	                SetPosition(grid_centres[0], grid_centres[1], grid_centres[2]);
	                SetNextPosition(grid_centres[0], grid_centres[1], grid_centres[2]);
	            }
	            if (grid_centres.Count >= 6)
	            {
	                SetNextPosition(grid_centres[3], grid_centres[4], grid_centres[5]);
	            }
                
                // save path data for debugging purposes
                int img_width = 640;
                int img_height = 480;
                byte[] img = new byte[img_width * img_height * 3];
                Bitmap bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                string filename = "debug_path_grids.jpg";
                ShowPath(img, img_width, img_height, true, false);
                BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
								
	            if ((grid_centres.Count >= 2) &&
	                (File.Exists(disparities_index_filename) &&
				    (File.Exists(disparities_filename))))
	            {
	                fs = File.Open(disparities_index_filename, FileMode.Open);
	                br = new BinaryReader(fs);
	
	                bool finished = false;
	                int disp_index = 0;
	                int grid_centres_index = 0;
	                while (!finished)
	                {
	                    try
	                    {
			                float position_x = br.ReadSingle();
			                float position_y = br.ReadSingle();

                            float dx = position_x - grid_centres[grid_centres_index * 3];
                            float dy = position_y - grid_centres[(grid_centres_index * 3) + 1];
                            float dist_sqr0 = dx * dx + dy * dy;
                            int next_grid_centres_index = grid_centres_index + 1;
                            if (next_grid_centres_index >= grid_centres.Count / 3) next_grid_centres_index = (grid_centres.Count / 3) - 1;
                            dx = position_x - grid_centres[next_grid_centres_index * 3];
                            dy = position_y - grid_centres[(next_grid_centres_index * 3) + 1];
                            float dist_sqr1 = dx * dx + dy * dy;

                            if (dist_sqr0 > dist_sqr1)
			                {
			                    disparities_index.Add(disp_index);
                                if (next_grid_centres_index < (grid_centres.Count / 3)-1) grid_centres_index++;
			                }
			                disp_index++;
	                    }
	                    catch
	                    {
	                         disparities_index.Add(disp_index);
	                         finished = true;
	                    }
	                }
	
	                br.Close();
	                fs.Close();
					
					// open the main disparities file
					try
					{
					    disparities_file = File.Open(disparities_filename, FileMode.Open);
					    disparities_reader = new BinaryReader(disparities_file);
						disparities_file_open = true;
					}
					catch
					{
					}
	            }
	            else
	            {
					if (!File.Exists(disparities_index_filename))
	                    Console.WriteLine("disparities index file " + disparities_index_filename + " not found");
					if (!File.Exists(disparities_filename))
	                    Console.WriteLine("disparities file " + disparities_filename + " not found");
					if (grid_centres.Count < 2) Console.WriteLine("Not enough grids");
	            }            
					
            }
            else
            {
                Console.WriteLine("path file " + path_filename + " not found");
            }            

        }
        
        public void ShowPath(
            byte[] img, 
            int img_width, 
            int img_height,
            bool show_grids,
            bool show_localisations)
        {
            // clear the image
            for (int i = (img_width*img_height*3)-1; i >= 0; i--) img[i] = 255;
            
            // find the bounding box
            float tx = float.MaxValue;
            float bx = float.MinValue;
            float ty = float.MaxValue;
            float by = float.MinValue;
            for (int  i = 0; i < path.Count; i += 3)
            {
                float x = path[i];
                float y = path[i+1];
                if (x < tx) tx = x;
                if (x > bx) bx = x;
                if (y < ty) ty = y;
                if (y > by) by = y;
            }
            
            // enlarge the bounding box
            float w = bx - tx;
            tx -= (w * 0.1f);
            bx += (w * 0.1f);
            float h = by - ty;
            ty -= (h * 0.1f);
            by += (h * 0.1f);
			
			if (w > h)
			{
				float cy = ty + ((by - ty) / 2);
				ty = cy - ((bx - tx)/2);
				by = cy + ((bx - tx)/2);
			}
			else
			{
				float cx = tx + ((bx - tx) / 2);
				tx = cx - ((by - ty)/2);
				bx = cx + ((by - ty)/2);
			}

            // show the path                        
            int prev_x = 0;
            int prev_y = 0;
            for (int  i = 0; i < path.Count; i += 3)
            {
                int x = (int)((path[i] - tx) * img_width / (bx - tx));
                int y = img_height - 1 - (int)((path[i+1] - ty) * img_height / (by - ty));
                if (i > 0)
                    drawing.drawLine(img, img_width, img_height, prev_x, prev_y, x, y, 0,0,0, 0,false);
                prev_x = x;
                prev_y = y;
            }
            
            // show grids along the path
            if (show_grids)
            {
	            int radius = (int)((dimension_mm/2) * img_width / (bx - tx));
	            for (int i = 0; i < grid_centres.Count; i += 3)
	            {
	                int x = (int)((grid_centres[i] - tx) * img_width / (bx - tx));
	                int y = img_height - 1 - (int)((grid_centres[i + 1] - ty) * img_height / (by - ty));
	                drawing.drawLine(img, img_width, img_height, x - radius, y - radius, x + radius, y - radius, 0,255,0, 0, false);
	                drawing.drawLine(img, img_width, img_height, x - radius, y - radius, x - radius, y + radius, 0,255,0, 0, false);
	                drawing.drawLine(img, img_width, img_height, x - radius, y + radius, x + radius, y + radius, 0,255,0, 0, false);
	                drawing.drawLine(img, img_width, img_height, x + radius, y - radius, x + radius, y + radius, 0,255,0, 0, false);
	            }
            }
            
            // show localisations
            if (show_localisations)
            {
                int radius = (int)(200 * img_width / (bx - tx));
	            for (int i = 0; i < localisations.Count; i += 5)
	            {
	                int x = (int)((localisations[i] - tx) * img_width / (bx - tx));
	                int y = img_height - 1 - (int)((localisations[i + 1] - ty) * img_height / (by - ty));
	                float pan = localisations[i + 3] + (float)Math.PI;
                    if (localisations[i + 4] > 0)
                    {
                        drawing.drawSpot(img, img_width, img_height, x, y, 2, 0, 255, 0);
                    }
                    else
                    {
                        drawing.drawSpot(img, img_width, img_height, x, y, 2, 255, 0, 0);
                    }
	            }
            }
        }
		
		#endregion
		
        #region "setting the centre position of the grid"
		
		/// <summary>
		/// sets the centre position for the given grid buffer
		/// </summary>
		/// <param name="buffer_index">index number of the buffer</param>
		/// <param name="centre_x_mm">x coordinate in millimetres</param>
		/// <param name="centre_y_mm">y coordinate in millimetres</param>
		/// <param name="centre_z_mm">z coordinate in millimetres</param>
		protected void SetPosition(
		    int buffer_index,
		    float centre_x_mm,
		    float centre_y_mm,
		    float centre_z_mm)
		{
			buffer[buffer_index].SetPosition(centre_x_mm, centre_y_mm, centre_z_mm, 0.0f);
		}

		/// <summary>
		/// sets the centre position for the current grid
		/// </summary>
		/// <param name="centre_x_mm">x coordinate in millimetres</param>
		/// <param name="centre_y_mm">y coordinate in millimetres</param>
		/// <param name="centre_z_mm">z coordinate in millimetres</param>
		protected void SetPosition(
		    float centre_x_mm,
		    float centre_y_mm,
		    float centre_z_mm)
		{
			SetPosition(current_buffer_index, centre_x_mm, centre_y_mm, centre_z_mm);
		}

		/// <summary>
		/// sets the centre position for the next grid which will be entered
		/// </summary>
		/// <param name="centre_x_mm">x coordinate in millimetres</param>
		/// <param name="centre_y_mm">y coordinate in millimetres</param>
		/// <param name="centre_z_mm">z coordinate in millimetres</param>
		protected void SetNextPosition(
		    float centre_x_mm,
		    float centre_y_mm,
		    float centre_z_mm)
		{
			SetPosition(1 - current_buffer_index, centre_x_mm, centre_y_mm, centre_z_mm);
		}
		
        #endregion
		
        #region "mapping and localising"

        /// <summary>
        /// Mapping
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
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="stereo_features">stereo features (disparities) for each stereo camera</param>
        /// <param name="stereo_features_colour">stereo feature colours for each stereo camera</param>
        /// <param name="stereo_features_uncertainties">stereo feature uncertainties (priors) for each stereo camera</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        /// <param name="robot_pose">current estimated position and orientation of the robots centre of rotation</param>
        protected void Map(
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
		    int stereo_camera_index,
		    float baseline_mm,
		    float stereo_camera_position_x,
		    float stereo_camera_position_y,
		    float stereo_camera_position_z,
		    float stereo_camera_pan,
		    float stereo_camera_tilt,
		    float stereo_camera_roll,
            int image_width,
            int image_height,
            float FOV_degrees,
		    float[] stereo_features,
		    byte[,] stereo_features_colour,
		    float[] stereo_features_uncertainties,
            stereoModel[][] sensormodel,
            pos3D robot_pose)
        {        
		    Parallel.For(0, 2, delegate(int i)
		    {			
                pos3D left_camera_location = null;
                pos3D right_camera_location = null;
                
                buffer[i].Map(
		            body_width_mm,
		            body_length_mm,
		            body_centre_of_rotation_x,
		            body_centre_of_rotation_y,
		            body_centre_of_rotation_z,
		            head_centroid_x,
		            head_centroid_y,
		            head_centroid_z,
		            head_pan,
		            head_tilt,
		            head_roll,
				    stereo_camera_index,
		            baseline_mm,
		            stereo_camera_position_x,
		            stereo_camera_position_y,
		            stereo_camera_position_z,
		            stereo_camera_pan,
		            stereo_camera_tilt,
		            stereo_camera_roll,
                    image_width,
                    image_height,
                    FOV_degrees,
		            stereo_features,
		            stereo_features_colour,
		            stereo_features_uncertainties,
                    sensormodel,
                    ref left_camera_location,
                    ref right_camera_location,
                    robot_pose);
            });
        }

        /// <summary>
        /// Update the current grid with new mapping rays loaded from the disparities file
        /// </summary>
        /// <param name="body_width_mm">width of the robot body in millimetres</param>
        /// <param name="body_length_mm">length of the robot body in millimetres</param>
        /// <param name="body_centre_of_rotation_x">x centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_y">y centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="body_centre_of_rotation_z">z centre of rotation of the robot, relative to the top left corner</param>
        /// <param name="head_centroid_x">head centroid x position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_y">head centroid y position in millimetres relative to the top left corner of the body</param>
        /// <param name="head_centroid_z">head centroid z position in millimetres relative to the top left corner of the body</param>
        /// <param name="baseline_mm">stereo camera baseline in millimetres</param>
        /// <param name="stereo_camera_position_x">stereo camera x position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_y">stereo camera y position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_position_z">stereo camera z position in millimetres relative to the head centroid</param>
        /// <param name="stereo_camera_pan">stereo camera pan in radians relative to the head</param>
        /// <param name="stereo_camera_tilt">stereo camera tilt in radians relative to the head</param>
        /// <param name="stereo_camera_roll">stereo camera roll in radians relative to the head</param>
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        protected void UpdateMap(
		    float body_width_mm,
		    float body_length_mm,
		    float body_centre_of_rotation_x,
		    float body_centre_of_rotation_y,
		    float body_centre_of_rotation_z,
		    float head_centroid_x,
		    float head_centroid_y,
		    float head_centroid_z,
		    float[] baseline_mm,
		    float[] stereo_camera_position_x,
		    float[] stereo_camera_position_y,
		    float[] stereo_camera_position_z,
		    float[] stereo_camera_pan,
		    float[] stereo_camera_tilt,
		    float[] stereo_camera_roll,
            int[] image_width,
            int[] image_height,
            float[] FOV_degrees,
            stereoModel[][] sensormodel)
        {
            const bool use_compression = false;

            if (disparities_file_open)
			{
				float[] stereo_features;
				byte[,] stereo_features_colour;
		        float[] stereo_features_uncertainties;
				pos3D robot_pose = new pos3D(0,0,0);

                //int next_grid_index = current_grid_index + 1;
                //if (next_grid_index >= grid_centres.Count / 3) next_grid_index = (grid_centres.Count / 3) - 1;

                int next_grid_index = current_grid_index;
                //if (current_grid_index < 1)
                //    next_grid_index = 1;
                
                int next_disparity_index = disparities_index[next_grid_index];
	            for (int i = current_disparity_index; i < next_disparity_index; i++)
				{
	                long time_long = disparities_reader.ReadInt64();
	                robot_pose.x = disparities_reader.ReadSingle();
					robot_pose.y = disparities_reader.ReadSingle();
					robot_pose.pan = disparities_reader.ReadSingle();
					float head_pan = disparities_reader.ReadSingle();
					float head_tilt = disparities_reader.ReadSingle();
					float head_roll = disparities_reader.ReadSingle();
	                int stereo_camera_index = disparities_reader.ReadInt32();
					int features_count = disparities_reader.ReadInt32();

                    if (use_compression)
                    {
                        int features_bytes = disparities_reader.ReadInt32();
                        byte[] fb = new byte[features_bytes];
                        disparities_reader.Read(fb, 0, features_bytes);
                        byte[] packed_stereo_features2 = AcedInflator.Instance.Decompress(fb, 0, 0, 0);
                        stereo_features = ArrayConversions.ToFloatArray(packed_stereo_features2);
                    }
                    else
                    {
                        byte[] fb = new byte[features_count * 3 * 4];
                        disparities_reader.Read(fb, 0, fb.Length);
                        stereo_features = ArrayConversions.ToFloatArray(fb);
                    }

                    byte[] packed_stereo_feature_colours = null;
                    if (use_compression)
                    {
                        int colour_bytes = disparities_reader.ReadInt32();
                        byte[] cb = new byte[colour_bytes];
                        disparities_reader.Read(cb, 0, colour_bytes);
                        packed_stereo_feature_colours = AcedInflator.Instance.Decompress(cb, 0, 0, 0);
                    }
                    else
                    {
                        packed_stereo_feature_colours = new byte[features_count * 3];
                        disparities_reader.Read(packed_stereo_feature_colours, 0, packed_stereo_feature_colours.Length);
                    }
					
					// unpack stereo features
					int ctr = 0;
					stereo_features_colour = new byte[features_count,3];
					stereo_features_uncertainties = new float[features_count];
					
	                for (int f = 0; f < features_count; f++)
	                {
						stereo_features_uncertainties[f] = 1;
						stereo_features_colour[f, 0] = packed_stereo_feature_colours[ctr++];
						stereo_features_colour[f, 1] = packed_stereo_feature_colours[ctr++];
						stereo_features_colour[f, 2] = packed_stereo_feature_colours[ctr++];
	                }
					
					// insert the rays into the map
                    Map(body_width_mm,
		                body_length_mm,
		                body_centre_of_rotation_x,
		                body_centre_of_rotation_y,
		                body_centre_of_rotation_z,
		                head_centroid_x,
		                head_centroid_y,
		                head_centroid_z,
		                head_pan,
		                head_tilt,
		                head_roll,
					    stereo_camera_index,
		                baseline_mm[stereo_camera_index],
		                stereo_camera_position_x[stereo_camera_index],
		                stereo_camera_position_y[stereo_camera_index],
		                stereo_camera_position_z[stereo_camera_index],
		                stereo_camera_pan[stereo_camera_index],
		                stereo_camera_tilt[stereo_camera_index],
		                stereo_camera_roll[stereo_camera_index],
                        image_width[stereo_camera_index],
                        image_height[stereo_camera_index],
                        FOV_degrees[stereo_camera_index],
		                stereo_features,
		                stereo_features_colour,
		                stereo_features_uncertainties,
                        sensormodel,
                        robot_pose);
					
					stereo_features = null;
					stereo_features_colour = null;
					stereo_features_uncertainties = null;                    
				}
                current_disparity_index = next_disparity_index;
			}
			else
			{
				Console.WriteLine("disparities file not open");
			}
        }

        /// <summary>
        /// swaps the two metagrids, clears the previous grid and assigns it the next centre position
        /// </summary>
        /// <param name="grid_centres">list of grid centre positions (x,y,z)</param>
        /// <param name="current_grid_index">index of the current grid which we are in</param>
        /// <param name="buffer">grid buffer, consisting of two metagrids</param>
        /// <param name="current_buffer_index">index of the currently active buffer - i.e. which of the two we are currently in</param>
        /// <param name="update_map">returns whether or not to update with new mapping rays</param>
        public static void SwapBuffers(
            List<float> grid_centres,
            ref int current_grid_index,
            metagrid[] buffer,
            ref int current_buffer_index,
            ref bool update_map)
        {
            int curr_buffer = 1 - current_buffer_index;
            int next_buffer = current_buffer_index;

            int no_of_grids = grid_centres.Count / 3;
            current_buffer_index = curr_buffer;
            if (current_grid_index < no_of_grids)
            {
                // move into the next grid
                current_grid_index++;

                if (current_grid_index < no_of_grids - 1)
                {
                    // clear the grid which we have just passed through
                    buffer[next_buffer].Clear();

                    // update the next map
                    update_map = true;

                    // retrieve the next grid centre position
                    int next_grid_index = current_grid_index + 1;
                    float next_grid_centre_x = grid_centres[(next_grid_index * 3)];
                    float next_grid_centre_y = grid_centres[(next_grid_index * 3) + 1];
                    float next_grid_centre_z = grid_centres[(next_grid_index * 3) + 2];

                    // set the next grid centre
                    buffer[next_buffer].SetPosition(next_grid_centre_x, next_grid_centre_y, next_grid_centre_z, 0.0f);
                }
            }
        }

        /// <summary>
        /// moves to the next grid in the sequence, if necessary
        /// </summary>
        /// <param name="current_grid_index">index of the current local grid</param>
        /// <param name="current_disparity_index">index of teh current disparities set within the disparities file</param>
        /// <param name="robot_pose">the current robot pose</param>
        /// <param name="buffer">buffer containing two metagrids</param>
        /// <param name="current_buffer_index">index of the currently active grid within the buffer (0 or 1)</param>
        /// <param name="grid_centres">list of grid centre positions (x,y,z)</param>
        /// <param name="update_map">returns whether the map should be updated</param>
        /// <returns>true if we have transitioned from one grid to the next</returns>
        public static bool MoveToNextLocalGrid(
            ref int current_grid_index,
            ref int current_disparity_index,
            pos3D robot_pose,
            metagrid[] buffer,
            ref int current_buffer_index,
            List<float> grid_centres,
            ref bool update_map,
            string debug_mapping_filename)
        {
            bool buffer_transition = false;
            update_map = false;
            
            // if this is the first time that localisation
            // has been called since loading the path
            // then update the map
            if ((current_grid_index == 0) &&
                (current_disparity_index == 0))
            {
                update_map = true;

                float grid_centre_x_mm = grid_centres[current_grid_index * 3];
                float grid_centre_y_mm = grid_centres[(current_grid_index * 3) + 1];
                float grid_centre_z_mm = grid_centres[(current_grid_index * 3) + 2];
                buffer[current_buffer_index].SetPosition(grid_centre_x_mm, grid_centre_y_mm, grid_centre_z_mm, 0);
                int next_grid_index = current_grid_index + 1;
                if (next_grid_index >= grid_centres.Count / 3) next_grid_index = current_grid_index;
                grid_centre_x_mm = grid_centres[next_grid_index * 3];
                grid_centre_y_mm = grid_centres[(next_grid_index * 3) + 1];
                grid_centre_z_mm = grid_centres[(next_grid_index * 3) + 2];
                buffer[1 - current_buffer_index].SetPosition(grid_centre_x_mm, grid_centre_y_mm, grid_centre_z_mm, 0);
            }
        
            // distance to the centre of the currently active grid
            float dx = robot_pose.x - buffer[current_buffer_index].x;
            float dy = robot_pose.y - buffer[current_buffer_index].y;
            float dz = robot_pose.z - buffer[current_buffer_index].z;
            float dist_to_grid_centre_sqr_0 = dx*dx + dy*dy + dz*dz;
            dx = robot_pose.x - buffer[1 - current_buffer_index].x;
            dy = robot_pose.y - buffer[1 - current_buffer_index].y;
            dz = robot_pose.z - buffer[1 - current_buffer_index].z;
            float dist_to_grid_centre_sqr_1 = dx*dx + dy*dy + dz*dz;
            
            // if we are closer to the next grid than the current one
            // then swap the currently active grid
            //if (dist_to_grid_centre_sqr_0 > dimension_mm/2)
            if (dist_to_grid_centre_sqr_1 < dist_to_grid_centre_sqr_0)
            {
                if ((debug_mapping_filename != null) &&
                    (debug_mapping_filename != ""))
                {
                    int debug_img_width = 640;
                    int debug_img_height = 480;
                    byte[] debug_img = new byte[debug_img_width * debug_img_height * 3];
                    Bitmap debug_bmp = new Bitmap(debug_img_width, debug_img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    buffer[current_buffer_index].Show(0, debug_img, debug_img_width, debug_img_height, false);
                    BitmapArrayConversions.updatebitmap_unsafe(debug_img, debug_bmp);
                    if (debug_mapping_filename.ToLower().EndsWith("png"))
                        debug_bmp.Save(debug_mapping_filename, System.Drawing.Imaging.ImageFormat.Png);
                    if (debug_mapping_filename.ToLower().EndsWith("gif"))
                        debug_bmp.Save(debug_mapping_filename, System.Drawing.Imaging.ImageFormat.Gif);
                    if (debug_mapping_filename.ToLower().EndsWith("jpg"))
                        debug_bmp.Save(debug_mapping_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                    if (debug_mapping_filename.ToLower().EndsWith("bmp"))
                        debug_bmp.Save(debug_mapping_filename, System.Drawing.Imaging.ImageFormat.Bmp);

                    string[] str = debug_mapping_filename.Split('.');
                    string debug_mapping_filename2 = str[0] + "b." + str[1];
                    buffer[1 - current_buffer_index].Show(0, debug_img, debug_img_width, debug_img_height, false);
                    BitmapArrayConversions.updatebitmap_unsafe(debug_img, debug_bmp);
                    if (debug_mapping_filename2.ToLower().EndsWith("png"))
                        debug_bmp.Save(debug_mapping_filename2, System.Drawing.Imaging.ImageFormat.Png);
                    if (debug_mapping_filename2.ToLower().EndsWith("gif"))
                        debug_bmp.Save(debug_mapping_filename2, System.Drawing.Imaging.ImageFormat.Gif);
                    if (debug_mapping_filename2.ToLower().EndsWith("jpg"))
                        debug_bmp.Save(debug_mapping_filename2, System.Drawing.Imaging.ImageFormat.Jpeg);
                    if (debug_mapping_filename2.ToLower().EndsWith("bmp"))
                        debug_bmp.Save(debug_mapping_filename2, System.Drawing.Imaging.ImageFormat.Bmp);
                }

                // swap the two metagrids
                SwapBuffers(
                    grid_centres,
                    ref current_grid_index,
                    buffer,
                    ref current_buffer_index,
                    ref update_map);

                buffer_transition = true;
            }
            return(buffer_transition);
        }

        /// <summary>
        /// Localisation
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
        /// <param name="image_width">image width for each stereo camera</param>
        /// <param name="image_height">image height for each stereo camera</param>
        /// <param name="FOV_degrees">field of view for each stereo camera in degrees</param>
        /// <param name="stereo_features">stereo features (disparities) for each stereo camera</param>
        /// <param name="stereo_features_colour">stereo feature colours for each stereo camera</param>
        /// <param name="stereo_features_uncertainties">stereo feature uncertainties (priors) for each stereo camera</param>
        /// <param name="sensormodel">sensor model for each stereo camera</param>
        /// <param name="left_camera_location">returned position and orientation of the left camera on each stereo camera</param>
        /// <param name="right_camera_location">returned position and orientation of the right camera on each stereo camera</param>
        /// <param name="no_of_samples">number of sample poses</param>
        /// <param name="sampling_radius_major_mm">major radius for samples, in the direction of robot movement</param>
        /// <param name="sampling_radius_minor_mm">minor radius for samples, perpendicular to the direction of robot movement</param>
        /// <param name="robot_pose">current estimated position and orientation of the robots centre of rotation, for each stereo camera observation</param>
        /// <param name="max_orientation_variance">maximum variance in orientation in radians, used to create sample poses</param>
        /// <param name="max_tilt_variance">maximum variance in tilt angle in radians, used to create sample poses</param>
        /// <param name="max_roll_variance">maximum variance in roll angle in radians, used to create sample poses</param>
        /// <param name="poses">list of poses tried</param>
        /// <param name="pose_score">list of pose matching scores</param>
        /// <param name="pose_offset">offset of the best pose from the current one</param>
		/// <param name="rnd">random number generator</param>
        /// <param name="buffer_transition">have we transitioned to the next grid buffer?</param>
        /// <returns>best localisation matching score</returns>
        protected float Localise(
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
		    float[] baseline_mm,
		    float[] stereo_camera_position_x,
		    float[] stereo_camera_position_y,
		    float[] stereo_camera_position_z,
		    float[] stereo_camera_pan,
		    float[] stereo_camera_tilt,
		    float[] stereo_camera_roll,
            int[] image_width,
            int[] image_height,
            float[] FOV_degrees,
		    float[][] stereo_features,
		    byte[][,] stereo_features_colour,
		    float[][] stereo_features_uncertainties,
            stereoModel[][] sensormodel,
            ref pos3D[] left_camera_location,
            ref pos3D[] right_camera_location,
            int no_of_samples,
            float sampling_radius_major_mm,
            float sampling_radius_minor_mm,
            pos3D[] robot_pose,
            float max_orientation_variance,
            float max_tilt_variance,
            float max_roll_variance,
            List<pos3D> poses,
            List<float> pose_score,
		    Random rnd,
            ref pos3D pose_offset,
            ref bool buffer_transition,
            string debug_mapping_filename,
            float known_best_pose_x_mm,
            float known_best_pose_y_mm)
        {
            bool update_map = false;

            // move to the next grid
            buffer_transition = MoveToNextLocalGrid(
                ref current_grid_index,
                ref current_disparity_index,
                robot_pose[0],
                buffer,
                ref current_buffer_index,
                grid_centres,
                ref update_map,
                debug_mapping_filename);
            
            // create the map if necessary
            if (update_map)
            {
                UpdateMap(
		            body_width_mm,
		            body_length_mm,
		            body_centre_of_rotation_x,
		            body_centre_of_rotation_y,
		            body_centre_of_rotation_z,
		            head_centroid_x,
		            head_centroid_y,
		            head_centroid_z,
		            baseline_mm,
		            stereo_camera_position_x,
		            stereo_camera_position_y,
		            stereo_camera_position_z,
		            stereo_camera_pan,
		            stereo_camera_tilt,
		            stereo_camera_roll,
                    image_width,
                    image_height,
                    FOV_degrees,
                    sensormodel);
            }

            int img_poses_width = 640;
            int img_poses_height = 480;
            byte[] img_poses = null;
            if ((debug_mapping_filename != null) &&
                (debug_mapping_filename != ""))
            {
                img_poses = new byte[img_poses_width * img_poses_height * 3];
            }
        
            // localise within the currently active grid
            float matching_score = 
	            buffer[current_buffer_index].Localise(
			        body_width_mm,
			        body_length_mm,
			        body_centre_of_rotation_x,
			        body_centre_of_rotation_y,
			        body_centre_of_rotation_z,
			        head_centroid_x,
			        head_centroid_y,
			        head_centroid_z,
			        head_pan,
			        head_tilt,
			        head_roll,
			        baseline_mm,
			        stereo_camera_position_x,
			        stereo_camera_position_y,
			        stereo_camera_position_z,
			        stereo_camera_pan,
			        stereo_camera_tilt,
			        stereo_camera_roll,
	                image_width,
	                image_height,
	                FOV_degrees,
			        stereo_features,
			        stereo_features_colour,
			        stereo_features_uncertainties,
	                sensormodel,
	                ref left_camera_location,
	                ref right_camera_location,
	                no_of_samples,
	                sampling_radius_major_mm,
	                sampling_radius_minor_mm,
	                robot_pose,
	                max_orientation_variance,
	                max_tilt_variance,
	                max_roll_variance,
	                poses,
	                pose_score,
			        rnd,
	                ref pose_offset,
                    img_poses,
                    img_poses_width,
                    img_poses_height,
                    known_best_pose_x_mm,
                    known_best_pose_y_mm);
			
			if (matching_score != occupancygridBase.NO_OCCUPANCY_EVIDENCE)
			{	        
		        // add this to the list of localisations                
		        localisations.Add(robot_pose[0].x + pose_offset.x);
		        localisations.Add(robot_pose[0].y + pose_offset.y);
		        localisations.Add(robot_pose[0].z + pose_offset.z);
		        localisations.Add(pose_offset.pan);
		        localisations.Add(matching_score);
	
	            if ((debug_mapping_filename != null) &&
	                (debug_mapping_filename != ""))
	            {
	                string[] str = debug_mapping_filename.Split('.');
	                string poses_filename = str[0] + "_gridcells.gif";
	                Bitmap poses_bmp = new Bitmap(img_poses_width, img_poses_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
	                BitmapArrayConversions.updatebitmap_unsafe(img_poses, poses_bmp);
	                poses_bmp.Save(poses_filename, System.Drawing.Imaging.ImageFormat.Gif);
	            }
			}
	        
	        return(matching_score);
        }
		
        public float Localise(
		    robotGeometry geom,
		    float[][] stereo_features,
		    byte[][,] stereo_features_colour,
		    float[][] stereo_features_uncertainties,
		    Random rnd,
            ref pos3D pose_offset,
            ref bool buffer_transition,
            string debug_mapping_filename,
            float known_best_pose_x_mm,
            float known_best_pose_y_mm)
        {
			return(Localise(
		        geom.body_width_mm,
		        geom.body_length_mm,
		        geom.body_centre_of_rotation_x,
		        geom.body_centre_of_rotation_y,
		        geom.body_centre_of_rotation_z,
		        geom.head_centroid_x,
		        geom.head_centroid_y,
		        geom.head_centroid_z,
		        geom.head_pan,
		        geom.head_tilt,
		        geom.head_roll,
		        geom.baseline_mm,
		        geom.stereo_camera_position_x,
		        geom.stereo_camera_position_y,
		        geom.stereo_camera_position_z,
		        geom.stereo_camera_pan,
		        geom.stereo_camera_tilt,
		        geom.stereo_camera_roll,
                geom.image_width,
                geom.image_height,
                geom.FOV_degrees,
		        stereo_features,
		        stereo_features_colour,
		        stereo_features_uncertainties,
                geom.sensormodel,
                ref geom.left_camera_location,
                ref geom.right_camera_location,
                geom.no_of_sample_poses,
                geom.sampling_radius_major_mm,
                geom.sampling_radius_minor_mm,
                geom.pose,
                geom.max_orientation_variance,
                geom.max_tilt_variance,
                geom.max_roll_variance,
                geom.poses,
                geom.pose_probability,
		        rnd,
                ref pose_offset,
                ref buffer_transition,
                debug_mapping_filename,
                known_best_pose_x_mm,
                known_best_pose_y_mm			                
			));
		}
		
        #endregion
		
	}
}
