/*
    steersman utility
    This generates an xml file containing sensor models
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
using System.Xml;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using sentience.core;
using sluggish.utilities;
using sluggish.utilities.xml;
using sentience.core;
using sentience.core.tests;

namespace sentience.Steersman
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			//tests_metagridbuffer.LocaliseAlongPath();
			
            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            string help_str = commandline.GetParameterValue("help", parameters);
            if (help_str != "")
            {
                ShowHelp();
            }
            else
            {
                Console.WriteLine("steersman: creates sensor models to steer a robot along a path");					
				
				int body_width_mm = 500;
                string body_width_mm_str = commandline.GetParameterValue("bodywidth", parameters);
                if (body_width_mm_str != "")
					body_width_mm = Convert.ToInt32(body_width_mm_str);

				int body_length_mm = 500;
                string body_length_mm_str = commandline.GetParameterValue("bodylength", parameters);
                if (body_length_mm_str != "")
					body_length_mm = Convert.ToInt32(body_length_mm_str);

				int body_height_mm = 500;
                string body_height_mm_str = commandline.GetParameterValue("bodyheight", parameters);
                if (body_height_mm_str != "")
					body_height_mm = Convert.ToInt32(body_height_mm_str);

				int centre_of_rotation_x = body_width_mm/2;
                string centre_of_rotation_x_str = commandline.GetParameterValue("centrex", parameters);
                if (centre_of_rotation_x_str != "")
					centre_of_rotation_x = Convert.ToInt32(centre_of_rotation_x_str);

				int centre_of_rotation_y = body_length_mm/2;
                string centre_of_rotation_y_str = commandline.GetParameterValue("centrey", parameters);
                if (centre_of_rotation_y_str != "")
					centre_of_rotation_y = Convert.ToInt32(centre_of_rotation_y_str);

				int centre_of_rotation_z = 0;
                string centre_of_rotation_z_str = commandline.GetParameterValue("centrez", parameters);
                if (centre_of_rotation_z_str != "")
					centre_of_rotation_z = Convert.ToInt32(centre_of_rotation_z_str);

				int head_centroid_x = body_width_mm/2;
                string head_centroid_x_str = commandline.GetParameterValue("headx", parameters);
                if (head_centroid_x_str != "")
					head_centroid_x = Convert.ToInt32(head_centroid_x_str);

				int head_centroid_y = body_length_mm/2;
                string head_centroid_y_str = commandline.GetParameterValue("heady", parameters);
                if (head_centroid_y_str != "")
					head_centroid_y = Convert.ToInt32(head_centroid_y_str);

				int head_centroid_z = body_height_mm;
                string head_centroid_z_str = commandline.GetParameterValue("headz", parameters);
                if (head_centroid_z_str != "")
					head_centroid_z = Convert.ToInt32(head_centroid_z_str);

                string sensormodels_filename = "";

				int no_of_stereo_cameras = 2;
                string no_of_stereo_cameras_str = commandline.GetParameterValue("cameras", parameters);
                if (no_of_stereo_cameras_str != "")
					no_of_stereo_cameras = Convert.ToInt32(no_of_stereo_cameras_str);

				float baseline_mm = 2;
                string baseline_mm_str = commandline.GetParameterValue("baseline", parameters);
                if (baseline_mm_str != "")
					baseline_mm = Convert.ToSingle(baseline_mm_str);

				int image_width = 320;
				int image_height = 240;
                string resolution_str = commandline.GetParameterValue("resolution", parameters);
                if (resolution_str != "")
				{
					string[] str = resolution_str.ToLower().Split('x');
					if (str.Length == 2)
					{
						image_width = Convert.ToInt32(str[0]);
						image_height = Convert.ToInt32(str[1]);
					}					
				}

				float FOV_degrees = 78;
                string FOV_degrees_str = commandline.GetParameterValue("fov", parameters);
                if (FOV_degrees_str != "")
					FOV_degrees = Convert.ToSingle(FOV_degrees_str);

				float head_diameter_mm = 100;
                string head_diameter_mm_str = commandline.GetParameterValue("headdiam", parameters);
                if (head_diameter_mm_str != "")
					head_diameter_mm = Convert.ToSingle(head_diameter_mm_str);

				float default_head_orientation_degrees = 0;
                string default_head_orientation_degrees_str = commandline.GetParameterValue("orient", parameters);
                if (default_head_orientation_degrees_str != "")
					default_head_orientation_degrees = Convert.ToSingle(default_head_orientation_degrees_str);

				int no_of_grid_levels = 1;
                string no_of_grid_levels_str = commandline.GetParameterValue("gridlevels", parameters);
                if (no_of_grid_levels_str != "")
					no_of_grid_levels = Convert.ToInt32(no_of_grid_levels_str);
				
				int dimension_mm = 8000;
                string dimension_mm_str = commandline.GetParameterValue("griddim", parameters);
                if (dimension_mm_str != "")
					dimension_mm = Convert.ToInt32(dimension_mm_str);

				int dimension_vertical_mm = 2000;
                string dimension_vertical_mm_str = commandline.GetParameterValue("griddimvert", parameters);
                if (dimension_vertical_mm_str != "")
					dimension_vertical_mm = Convert.ToInt32(dimension_vertical_mm_str);

				int cellSize_mm = 50;
                string cellSize_mm_str = commandline.GetParameterValue("cellsize", parameters);
                if (cellSize_mm_str != "")
					cellSize_mm = Convert.ToInt32(cellSize_mm_str);
				
                string filename = commandline.GetParameterValue("filename", parameters);
                if (filename == "") filename = "steersman.xml";
                
				
				Console.WriteLine("Computing sensor models...Please wait");
				steersman visual_guidance = new steersman(
				    body_width_mm,
				    body_length_mm,
				    body_height_mm,
				    centre_of_rotation_x,
				    centre_of_rotation_y,
				    centre_of_rotation_z,
				    head_centroid_x,
				    head_centroid_y,
				    head_centroid_z,
				    sensormodels_filename,
				    no_of_stereo_cameras,
				    baseline_mm,
				    image_width,
				    image_height,
				    FOV_degrees,
				    head_diameter_mm,
				    default_head_orientation_degrees,
		            no_of_grid_levels,
				    dimension_mm, 
		            dimension_vertical_mm, 
		            cellSize_mm);
				
				visual_guidance.Save(filename);
				Console.WriteLine("Saved as " + filename);
            }
		}
		

        #region "validation"

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("help");
            ValidParameters.Add("filename");
			ValidParameters.Add("bodywidth");
			ValidParameters.Add("bodylength");
			ValidParameters.Add("bodyheight");
			ValidParameters.Add("centrex");
			ValidParameters.Add("centrey");
			ValidParameters.Add("centrez");
			ValidParameters.Add("headx");
			ValidParameters.Add("heady");
			ValidParameters.Add("headz");
			ValidParameters.Add("cameras");
			ValidParameters.Add("baseline");
			ValidParameters.Add("resolution");
			ValidParameters.Add("fov");
			ValidParameters.Add("headdiam");
			ValidParameters.Add("orient");
			ValidParameters.Add("gridlevels");
			ValidParameters.Add("griddim");
			ValidParameters.Add("griddimvert");
			ValidParameters.Add("cellsize");

            return (ValidParameters);
        }

        #endregion

        #region "help information"

        /// <summary>
        /// shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("steersman help");
            Console.WriteLine("--------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  steersman");
            Console.WriteLine("         -filename <xml filename to be generated>");
			Console.WriteLine("         -bodywidth <body width in mm (x axis)>");
			Console.WriteLine("         -bodylength <body length in mm (y axis)>");
			Console.WriteLine("         -bodyheight <body height in mm (z axis)>");
			Console.WriteLine("         -centrex <x centre of rotation in mm>");
			Console.WriteLine("         -centrey <y centre of rotation in mm>");
			Console.WriteLine("         -centrez <z centre of rotation in mm>");
			Console.WriteLine("         -headx <head x centroid in mm>");
			Console.WriteLine("         -heady <head y centroid in mm>");
			Console.WriteLine("         -headz <head z centroid in mm>");
			Console.WriteLine("         -cameras <number of stereo cameras>");
			Console.WriteLine("         -baseline <stereo camera baseline>");
			Console.WriteLine("         -resolution <widthxheight>");
			Console.WriteLine("         -fov <field of view in degrees>");
			Console.WriteLine("         -headdiam <head diameter in mm>");
			Console.WriteLine("         -orient <default head orientation in degrees>");
			Console.WriteLine("         -gridlevels <no of occupancy grid levels>");
			Console.WriteLine("         -griddim <occupancy grid dimension in mm>");
			Console.WriteLine("         -griddimvert <occupancy grid vertical_dimension in mm>");
			Console.WriteLine("         -cellsize <occupancy grid cell size in mm>");			
        }

        #endregion		
	}
}