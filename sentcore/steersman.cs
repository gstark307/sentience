/*
    Steers a robot along a pre-recorded path using vision
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
using System.Collections.Generic;
using System.Drawing;
using sluggish.utilities;
using sluggish.utilities.xml;

namespace sentience.core
{	
	public class steersman
	{
		// geometry of the robot
	    public robotGeometry robot_geometry;
		
		// occupancy grid double buffer
		public metagridBuffer buffer;
		
		// random number generator
		protected Random rnd;
		
		// overall map
	    public string overall_map_filename;
		protected byte[] overall_map_img = null;
		protected int overall_map_dimension_mm;
		protected int overall_map_centre_x_mm;
		protected int overall_map_centre_y_mm;		
		
		#region "constructors"
		
		public steersman()
		{
			rnd = new Random(0);
		}
		
		public steersman(
		    int body_width_mm,
		    int body_length_mm,
		    int body_height_mm,
		    int centre_of_rotation_x,
		    int centre_of_rotation_y,
		    int centre_of_rotation_z,
		    int head_centroid_x,
		    int head_centroid_y,
		    int head_centroid_z,
		    string sensormodels_filename,
		    int no_of_stereo_cameras,
		    float baseline_mm,
		    int image_width,
		    int image_height,
		    float FOV_degrees,
		    float head_diameter_mm,
		    float default_head_orientation_degrees,
            int no_of_grid_levels,
		    int dimension_mm, 
            int dimension_vertical_mm, 
            int cellSize_mm)		                 
		{
			rnd = new Random(0);
            int grid_type = metagrid.TYPE_SIMPLE;
            int localisationRadius_mm = dimension_mm * 50/100;
            int maxMappingRange_mm = dimension_mm * 50/100;
            float vacancyWeighting = 0.5f;
			
			buffer = new metagridBuffer(
			    no_of_grid_levels,
                grid_type,
		        dimension_mm, 
                dimension_vertical_mm, 
                cellSize_mm, 
                localisationRadius_mm, 
                maxMappingRange_mm, 
                vacancyWeighting);
							
			robot_geometry = new robotGeometry();
			robot_geometry.SetBodyDimensions(body_width_mm, body_length_mm, body_height_mm);
			robot_geometry.SetCentreOfRotation(centre_of_rotation_x, centre_of_rotation_y, centre_of_rotation_z);
			robot_geometry.SetHeadPosition(head_centroid_x, head_centroid_y, head_centroid_z);
			robot_geometry.CreateStereoCameras(
			    no_of_stereo_cameras, 
			    baseline_mm, 
			    image_width,
			    image_height,
			    FOV_degrees,
			    head_diameter_mm,
			    default_head_orientation_degrees);
			
			robot_geometry.CreateSensorModels(buffer);
		}
		
        #endregion
			                            
		#region "loading and saving"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeComp = doc.CreateElement("SteersmanComponents");
            parent.AppendChild(nodeComp);								
			
            XmlElement nodeGeom = doc.CreateElement("SteersmanGeometry");
            nodeComp.AppendChild(nodeGeom);								

			nodeGeom.AppendChild(
			    robot_geometry.getXml(doc, nodeGeom));
			
            XmlElement nodeBuffer = doc.CreateElement("SteersmanBuffer");
            nodeComp.AppendChild(nodeBuffer);								
			
            nodeBuffer.AppendChild(
			    buffer.getXml(doc, nodeBuffer));

			if (robot_geometry.sensormodel != null)
			{
                XmlElement nodeModels = doc.CreateElement("SteersmanSensorModels");
                nodeComp.AppendChild(nodeModels);								
			
                nodeModels.AppendChild(
			        robot_geometry.getXmlSensorModels(doc, nodeModels));
			}
			
            return (nodeComp);
        }
		
        /// <summary>
        /// parse an xml node
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(
		    XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

			if (xnod.Name == "SteersmanGeometry")
			{
                int cameraIndex = -1;
				if (robot_geometry == null) robot_geometry = new robotGeometry();
                robot_geometry.LoadFromXml(xnod, level + 1, ref cameraIndex);				
			}			

			if (xnod.Name == "SteersmanBuffer")
			{
                if (buffer == null) buffer = new metagridBuffer();
	            int no_of_grid_levels=0;
	            int grid_type=0;
			    int dimension_mm=0; 
	            int dimension_vertical_mm=0; 
	            int cellSize_mm=0; 
	            int localisationRadius_mm=0; 
	            int maxMappingRange_mm=0; 
	            float vacancyWeighting=0;
                buffer.LoadFromXml(xnod, level + 1,
	                ref no_of_grid_levels,
	                ref grid_type,
			        ref dimension_mm,
	                ref dimension_vertical_mm,
	                ref cellSize_mm,
	                ref localisationRadius_mm,
	                ref maxMappingRange_mm,
	                ref vacancyWeighting
				);
				
				buffer.Initialise(
	                no_of_grid_levels,
	                grid_type,
			        dimension_mm,
	                dimension_vertical_mm,
	                cellSize_mm, 
	                localisationRadius_mm,
	                maxMappingRange_mm, 
	                vacancyWeighting);
			}

			if (xnod.Name == "SteersmanSensorModels")
			{
				if (robot_geometry == null) robot_geometry = new robotGeometry();
				int no_of_stereo_cameras = 0;
				int no_of_grid_levels = 0;
				int camera_index = 0;
				int grid_level = 0;
                robot_geometry.LoadFromXmlSensorModels(
				    xnod, level + 1, 
				    ref no_of_stereo_cameras, 
				    ref no_of_grid_levels, 
				    ref camera_index,
				    ref grid_level);
			}			
			
            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }		
		
        /// <summary>
        /// return an Xml document
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlElement nodeSteersman = doc.CreateElement("Steersman");
            doc.AppendChild(nodeSteersman);
			
            nodeSteersman.AppendChild(
			    getXml(doc, nodeSteersman));
			
            return (doc);
        }

        /// <summary>
        /// save as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(string filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load parameters from file
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

				LoadFromXml(xnodDE, 0);				
				                           
                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }		
		
		#endregion
		
		#region "display"
		
		public void ShowLocalisations(
            string image_filename, 
		    int img_width, 
		    int img_height)
		{
            byte[] img = new byte[img_width * img_height * 3];
            Bitmap bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            buffer.ShowPath(img, img_width, img_height, true, true);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);

			if (image_filename.ToLower().EndsWith("gif"))
                bmp.Save(image_filename, System.Drawing.Imaging.ImageFormat.Gif);			
			if (image_filename.ToLower().EndsWith("png"))
                bmp.Save(image_filename, System.Drawing.Imaging.ImageFormat.Png);			
			if (image_filename.ToLower().EndsWith("bmp"))
                bmp.Save(image_filename, System.Drawing.Imaging.ImageFormat.Bmp);			
			if (image_filename.ToLower().EndsWith("jpg"))
                bmp.Save(image_filename, System.Drawing.Imaging.ImageFormat.Jpeg);			
		}		
		
		#endregion
		
		#region "loading a path"
		
		protected string current_path_filename;
		protected string current_disparities_index_filename;
		protected string current_disparities_filename;		
		
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
			current_path_filename = path_filename;
			current_disparities_index_filename = disparities_index_filename;
			current_disparities_filename = disparities_filename;
			
			buffer.LoadPath(
			    path_filename, 
			    disparities_index_filename, 
			    disparities_filename,
			    ref overall_map_dimension_mm,
			    ref overall_map_centre_x_mm,
			    ref overall_map_centre_y_mm);
			overall_map_img = null;
		}		
		
		public void ResetPath()
		{
			buffer.Reset();
			if ((current_path_filename != null) &&
			    (current_path_filename != ""))
			{			    
				overall_map_img = null;
			    buffer.LoadPath(
				    current_path_filename, 
				    current_disparities_index_filename, 
				    current_disparities_filename,
				    ref overall_map_dimension_mm,
				    ref overall_map_centre_x_mm,
				    ref overall_map_centre_y_mm);
			}
		}
		
		#endregion
		
		#region "localisation"
		
		/// <summary>
		/// localise and return offset values
		/// </summary>
		/// <param name="stereo_features">disparities for each stereo camera (x,y,disparity)</param>
		/// <param name="stereo_features_colour">colour for each disparity</param>
		/// <param name="stereo_features_uncertainties">uncertainty for each disparity</param>
		/// <param name="debug_mapping_filename">optional filename used for saving debugging info</param>
		/// <param name="known_offset_x_mm">ideal x offset, if known</param>
		/// <param name="known_offset_y_mm">ideal y offset, if known</param>
		/// <param name="offset_x_mm">returned x offset</param>
		/// <param name="offset_y_mm">returned y offset</param>
		/// <param name="offset_z_mm">returned z offset</param>
		/// <param name="offset_pan_radians">returned pan</param>
		/// <param name="offset_tilt_radians">returned tilt</param>
		/// <param name="offset_roll_radians">returned roll</param>
		/// <returns>true if the localisation was valid</returns>
		public bool Localise(
		    float[][] stereo_features,
		    byte[][,] stereo_features_colour,
		    float[][] stereo_features_uncertainties,		                     
		    string debug_mapping_filename,
		    float known_offset_x_mm,
		    float known_offset_y_mm,
		    ref float offset_x_mm,
		    ref float offset_y_mm,
		    ref float offset_z_mm,
		    ref float offset_pan_radians,
		    ref float offset_tilt_radians,
		    ref float offset_roll_radians)
		{
			bool valid_localisation = true;
			pos3D pose_offset = new pos3D(0,0,0);
			bool buffer_transition = false;
			
            float matching_score = buffer.Localise(
                robot_geometry,
		        stereo_features,
		        stereo_features_colour,
		        stereo_features_uncertainties,
		        rnd,
                ref pose_offset,
                ref buffer_transition,
                debug_mapping_filename,
                known_offset_x_mm, 
			    known_offset_y_mm,
		        overall_map_filename,
		        ref overall_map_img,
		        overall_map_dimension_mm,
		        overall_map_centre_x_mm,
		        overall_map_centre_y_mm);
			
			if (matching_score == occupancygridBase.NO_OCCUPANCY_EVIDENCE)
				valid_localisation = false;
			
		    offset_x_mm = pose_offset.x;
		    offset_y_mm = pose_offset.y;
		    offset_z_mm = pose_offset.z;
		    offset_pan_radians = pose_offset.pan;
		    offset_tilt_radians = pose_offset.tilt;
		    offset_roll_radians = pose_offset.roll;
			
			return(valid_localisation);
		}
		
		#endregion
	
		#region "setters"
		
		/// <summary>
		/// sets the orientation of the robots head 
		/// </summary>
		/// <param name="pan"></param>
		/// <param name="tilt"></param>
		/// <param name="roll"></param>
		public void SetHeadOrientation(
		    float pan_degrees,
		    float tilt_degrees,
		    float roll_degrees)
		{
			robot_geometry.SetHeadOrientation(pan_degrees, tilt_degrees, roll_degrees);
		}
		
		/// <summary>
		/// sets the centroid position of the robot's head 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public void SetHeadPosition(
		    float x,
            float y,
		    float z)
		{
			robot_geometry.SetHeadPosition(x, y, z);
		}
				
		/// <summary>
		/// Defines the major and minor axes of an ellipse which describes the pos uncertainty 
		/// The major axis of the ellipse is in the direction of motion
		/// </summary>
		/// <param name="major_axis_mm">length of the major axis, in the direction of motion, in millimetres</param>
		/// <param name="minor_axis_mm">length of the minor axis, perpendicular to the direction of motion, in millimetres</param>
		public void SetPoseUncertaintyEllipse(
		    float major_axis_mm,
		    float minor_axis_mm)
		{
			robot_geometry.SetPoseUncertaintyEllipse(major_axis_mm, minor_axis_mm);
		}
		
		/// <summary>
		/// Set the maximum orientation variance, used when considering possible poses 
		/// </summary>
		/// <param name="pan_degrees">maximum variance in pan angle</param>
		/// <param name="tilt_degrees">maximum variance in tilt angle</param>
		/// <param name="roll_degrees">maximum variance in roll angle</param>
		public void SetMaximumOrientationVariance(
		    float pan_degrees,
		    float tilt_degrees,
		    float roll_degrees)
		{
			robot_geometry.SetMaximumOrientationVariance(pan_degrees, tilt_degrees, roll_degrees);
		}

		/// <summary>
		/// sets the estimates position and orientation for the given stereo camera observation
		/// </summary>
		/// <param name="stereo_camera_index">index number of the stereo camera from which the observation was made</param>
		/// <param name="x_mm">estimated x position when the observation was made</param>
		/// <param name="y_mm">estimated y position when the observation was made</param>
		/// <param name="z_mm">estimated z position when the observation was made</param>
		/// <param name="pan_radians">estimated pan angle when the observation was made</param>
		/// <param name="tilt_radians">estimated tilt angle when the observation was made</param>
		/// <param name="roll_radians">estimated roll angle when the observation was made</param>
		public void SetCurrentPosition(
		    int stereo_camera_index,
		    float x_mm,
		    float y_mm,
		    float z_mm,
		    float pan_radians,
		    float tilt_radians,
		    float roll_radians)
		{
			if (robot_geometry.pose[stereo_camera_index] != null)
			{
			    robot_geometry.pose[stereo_camera_index].x = x_mm;
			    robot_geometry.pose[stereo_camera_index].y = y_mm;
			    robot_geometry.pose[stereo_camera_index].z = z_mm;				
			}
			else
			{	
				robot_geometry.pose[stereo_camera_index] = new pos3D(x_mm,y_mm,z_mm);
			}
			robot_geometry.pose[stereo_camera_index].pan = pan_radians;
			robot_geometry.pose[stereo_camera_index].tilt = tilt_radians;
			robot_geometry.pose[stereo_camera_index].roll = roll_radians;
		}

		#endregion
		
	}
}