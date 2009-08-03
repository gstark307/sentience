/*
    robot geometry
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
using sluggish.utilities.xml;

namespace sentience.core
{
	public class robotGeometry
	{
		// bounding box for the robot's body
	    public float body_width_mm;
	    public float body_length_mm;
	    public float body_height_mm;
		
		// centre of rotation, relative to the top left corner of the body's bounding box
	    public float body_centre_of_rotation_x;
	    public float body_centre_of_rotation_y;
	    public float body_centre_of_rotation_z;
		
		// head centroid relative to the top left corner of the body's bounding box
	    public float head_centroid_x;
	    public float head_centroid_y;
	    public float head_centroid_z;
		
		// diameter of the head
		public float head_diameter_mm;
		
		// head orientation in radians
	    public float head_pan;
	    public float head_tilt;
	    public float head_roll;
		
		// stereo camera baseline
	    public float[] baseline_mm;
		
		// stereo camera position relative to the head centroid
	    public float[] stereo_camera_position_x;
	    public float[] stereo_camera_position_y;
	    public float[] stereo_camera_position_z;
		
		// stereo camera orientation relative to the head
	    public float[] stereo_camera_pan;
	    public float[] stereo_camera_tilt;
	    public float[] stereo_camera_roll;
		
		// image dimensions for each stereo camera
        public int[] image_width;
        public int[] image_height;
		
		// field of view for each stereo camera in degrees
        public float[] FOV_degrees;
		
		// sensor models for each stereo camera
        public stereoModel[][] sensormodel;
		
		// current positions of the left and right cameras on each stereo camera
        public pos3D[] left_camera_location;
        public pos3D[] right_camera_location;
		
		// current estimated pose for each stereo camera
		// if all stereo cameras observe at the same time this
		// will be the same, but potentially observations could be made with
		// some small time delay which may result in a slightly different
		// pose associated with each
        public pos3D[] pose;
				
		// this defines dimensions of the pose uncertainty ellipse
        public float sampling_radius_major_mm;
        public float sampling_radius_minor_mm;

		// maximum orientation variance within the pose list
        public float max_orientation_variance;
        public float max_tilt_variance;
        public float max_roll_variance;
		
		// pose list
        public int no_of_sample_poses;
        public List<pos3D> poses;
		
		// probability for each pose in the list
        public List<float> pose_probability;
		
		public robotGeometry()
		{
	        no_of_sample_poses = 200;
	        sampling_radius_major_mm = 300;
	        sampling_radius_minor_mm = 300;
	        max_orientation_variance = 5 * (float)Math.PI / 180.0f;
	        max_tilt_variance = 0;
	        max_roll_variance = 0;
			poses = new List<pos3D>();
			pose_probability = new List<float>();			
		}
		
		public void CreateStereoCameras(
		    int no_of_stereo_cameras,
		    float cam_baseline_mm,
			float dist_from_centre_of_tilt_mm, 
		    int cam_image_width, 
		    int cam_image_height,
		    float cam_FOV_degrees,
		    float head_diameter_mm,
		    float default_head_orientation_degrees)
		{
			this.head_diameter_mm = head_diameter_mm;
			pose = new pos3D[no_of_stereo_cameras];
			for (int i = 0; i < no_of_stereo_cameras; i++)
			    pose[i] = new pos3D(0,0,0);
			
			baseline_mm = new float[no_of_stereo_cameras];
			image_width = new int[no_of_stereo_cameras];
			image_height = new int[no_of_stereo_cameras];
			FOV_degrees = new float[no_of_stereo_cameras];
			stereo_camera_position_x = new float[no_of_stereo_cameras];
			stereo_camera_position_y = new float[no_of_stereo_cameras];
			stereo_camera_position_z = new float[no_of_stereo_cameras];
			stereo_camera_pan = new float[no_of_stereo_cameras];
			stereo_camera_tilt = new float[no_of_stereo_cameras];
			stereo_camera_roll = new float[no_of_stereo_cameras];
			left_camera_location = new pos3D[no_of_stereo_cameras];
			right_camera_location = new pos3D[no_of_stereo_cameras];
			
			for (int cam = 0; cam < no_of_stereo_cameras; cam++)
			{
				float cam_orientation = cam * (float)Math.PI*2 / no_of_stereo_cameras;
				cam_orientation += default_head_orientation_degrees * (float)Math.PI / 180.0f;
				stereo_camera_position_x[cam] = head_diameter_mm * 0.5f * (float)Math.Sin(cam_orientation);
				stereo_camera_position_y[cam] = head_diameter_mm * 0.5f * (float)Math.Cos(cam_orientation);				
				stereo_camera_position_z[cam] = dist_from_centre_of_tilt_mm;
				stereo_camera_pan[cam] = cam_orientation;
				
				baseline_mm[cam] = cam_baseline_mm;
				image_width[cam] = cam_image_width;
				image_height[cam] = cam_image_height;
				FOV_degrees[cam] = cam_FOV_degrees;
			}
		}
		
		public void CreateSensorModels(
		    metagridBuffer buf)
		{
			List<int> cell_sizes = buf.GetCellSizes();
			
			sensormodel = new stereoModel[image_width.Length][];
			for (int stereo_cam = 0; stereo_cam < image_width.Length; stereo_cam++)
				sensormodel[stereo_cam] = new stereoModel[cell_sizes.Count];
			
			for (int stereo_cam = 0; stereo_cam < image_width.Length; stereo_cam++)
			{
				for (int grid_level = 0; grid_level < cell_sizes.Count; grid_level++)
				{
					if (stereo_cam > 0)
					{
						if (image_width[stereo_cam - 1] == 
						    image_width[stereo_cam])
						{
							sensormodel[stereo_cam][grid_level] = sensormodel[stereo_cam-1][grid_level];
						}
					}
					
					if (sensormodel[stereo_cam][grid_level] == null)
					{
					    sensormodel[stereo_cam][grid_level] = new stereoModel();
					    sensormodel[stereo_cam][grid_level].createLookupTable(
						    cell_sizes[grid_level], 
						    image_width[stereo_cam], 
						    image_height[stereo_cam]);
					}
				}
			}
		}
		
		#region "loading and saving"
		
        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeRobot = doc.CreateElement("RobotGeometry");
            parent.AppendChild(nodeRobot);								
			
            xml.AddComment(doc, nodeRobot, "Dimensions of the body in millimetres");
            xml.AddTextElement(doc, nodeRobot, "BodyWidthMillimetres", Convert.ToString(body_width_mm));
            xml.AddTextElement(doc, nodeRobot, "BodyLengthMillimetres", Convert.ToString(body_length_mm));
            xml.AddTextElement(doc, nodeRobot, "BodyHeightMillimetres", Convert.ToString(body_height_mm));

            xml.AddComment(doc, nodeRobot, "centre of rotation, relative to the top left corner of the body's bounding box");
            xml.AddTextElement(doc, nodeRobot, "CentreOfRotationX", Convert.ToString(body_centre_of_rotation_x));
            xml.AddTextElement(doc, nodeRobot, "CentreOfRotationY", Convert.ToString(body_centre_of_rotation_y));
            xml.AddTextElement(doc, nodeRobot, "CentreOfRotationZ", Convert.ToString(body_centre_of_rotation_z));

            xml.AddComment(doc, nodeRobot, "Head centroid relative to the top left corner of the body's bounding box");
            xml.AddTextElement(doc, nodeRobot, "HeadCentreX", Convert.ToString(head_centroid_x));
            xml.AddTextElement(doc, nodeRobot, "HeadCentreY", Convert.ToString(head_centroid_y));
            xml.AddTextElement(doc, nodeRobot, "HeadCentreZ", Convert.ToString(head_centroid_z));

			xml.AddTextElement(doc, nodeRobot, "HeadDiameterMillimetres", Convert.ToString(head_diameter_mm));

            xml.AddComment(doc, nodeRobot, "Maximum pose variance");
            xml.AddTextElement(doc, nodeRobot, "MaxOrientationVarianceDegrees", Convert.ToString(max_orientation_variance / (float)Math.PI * 180));
            xml.AddTextElement(doc, nodeRobot, "MaxTiltVarianceDegrees", Convert.ToString(max_tilt_variance / (float)Math.PI * 180));
            xml.AddTextElement(doc, nodeRobot, "MaxRollVarianceDegrees", Convert.ToString(max_roll_variance / (float)Math.PI * 180));			

			int no_of_stereo_cameras = baseline_mm.Length;
			xml.AddTextElement(doc, nodeRobot, "NoOfStereoCameras", Convert.ToString(no_of_stereo_cameras));			
			
            XmlElement nodeCameras = doc.CreateElement("Cameras");
            nodeRobot.AppendChild(nodeCameras);
			
			for (int i = 0; i < no_of_stereo_cameras; i++)
			{
                XmlElement nodeStereoCamera = doc.CreateElement("StereoCamera");
                nodeCameras.AppendChild(nodeStereoCamera);
				xml.AddTextElement(doc, nodeStereoCamera, "BaselineMillimetres", Convert.ToString(baseline_mm[i]));
				
				string str = "";
				str = Convert.ToString(stereo_camera_position_x[i]) + " ";
				str += Convert.ToString(stereo_camera_position_y[i]) + " ";
				str += Convert.ToString(stereo_camera_position_z[i]);
  			    xml.AddComment(doc, nodeStereoCamera, "stereo camera position relative to the head centroid");			
				xml.AddTextElement(doc, nodeStereoCamera, "PositionMillimetres", str);

				str = Convert.ToString(stereo_camera_pan[i] / (float)Math.PI * 180) + " ";
				str += Convert.ToString(stereo_camera_tilt[i] / (float)Math.PI * 180) + " ";
				str += Convert.ToString(stereo_camera_roll[i] / (float)Math.PI * 180);
			    xml.AddComment(doc, nodeStereoCamera, "stereo camera orientation relative to the head");
		 	    xml.AddTextElement(doc, nodeStereoCamera, "OrientationDegrees", str);
			
				str = Convert.ToString(image_width[i]) + " ";
				str += Convert.ToString(image_height[i]) + " ";
			    xml.AddComment(doc, nodeStereoCamera, "image dimensions for each stereo camera");
			    xml.AddTextElement(doc, nodeStereoCamera, "ImageDimensions", str);

				xml.AddTextElement(doc, nodeStereoCamera, "FOVDegrees", Convert.ToString(FOV_degrees[i]));
			}
			
            return (nodeRobot);
        }

        /// <summary>
        /// return an Xml document containing geometry parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlElement nodeGeom = doc.CreateElement("Geometry");
            doc.AppendChild(nodeGeom);

            nodeGeom.AppendChild(getXml(doc, nodeGeom));

            return (doc);
        }

        /// <summary>
        /// save camera geometry as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(string filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }
		
        /// <summary>
        /// parse an xml node to extract geometry parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(
		    XmlNode xnod, int level,
            ref int cameraIndex)
        {
            XmlNode xnodWorking;

			if (xnod.Name == "StereoCamera")
			{
				cameraIndex++;
			}			
            if (xnod.Name == "BodyWidthMillimetres")
            {
                body_width_mm = Convert.ToSingle(xnod.InnerText);
            }
            if (xnod.Name == "BodyLengthMillimetres")
            {
                body_length_mm = Convert.ToSingle(xnod.InnerText);
            }
            if (xnod.Name == "BodyHeightMillimetres")
            {
                body_height_mm = Convert.ToSingle(xnod.InnerText);
            }

			if (xnod.Name == "CentreOfRotationX")
            {
                body_centre_of_rotation_x = Convert.ToSingle(xnod.InnerText);
            }
			if (xnod.Name == "CentreOfRotationY")
            {
                body_centre_of_rotation_y = Convert.ToSingle(xnod.InnerText);
            }
			if (xnod.Name == "CentreOfRotationZ")
            {
                body_centre_of_rotation_z = Convert.ToSingle(xnod.InnerText);
            }
			
			if (xnod.Name == "HeadCentreX")
			{
				head_centroid_x = Convert.ToSingle(xnod.InnerText);
			}
			if (xnod.Name == "HeadCentreY")
			{
				head_centroid_y = Convert.ToSingle(xnod.InnerText);
			}
			if (xnod.Name == "HeadCentreZ")
			{
				head_centroid_z = Convert.ToSingle(xnod.InnerText);
			}

			if (xnod.Name == "HeadDiameterMillimetres")
			{
				head_diameter_mm = Convert.ToSingle(xnod.InnerText);
			}

			if (xnod.Name == "MaxOrientationVarianceDegrees")
			{
				max_orientation_variance = Convert.ToSingle(xnod.InnerText) / 180.0f * (float)Math.PI;
			}
			if (xnod.Name == "MaxTiltVarianceDegrees")
			{
				max_tilt_variance = Convert.ToSingle(xnod.InnerText) / 180.0f * (float)Math.PI;
			}
			if (xnod.Name == "MaxRollVarianceDegrees")
			{
				max_roll_variance = Convert.ToSingle(xnod.InnerText) / 180.0f * (float)Math.PI;
			}

			if (xnod.Name == "NoOfStereoCameras")
			{
				cameraIndex = -1;
				int no_of_stereo_cameras = Convert.ToInt32(xnod.InnerText);
				
				baseline_mm = new float[no_of_stereo_cameras];
				pose = new pos3D[no_of_stereo_cameras];
				poses = new List<pos3D>();
				stereo_camera_position_x = new float[no_of_stereo_cameras];
				stereo_camera_position_y = new float[no_of_stereo_cameras];
				stereo_camera_position_z = new float[no_of_stereo_cameras];
				stereo_camera_pan = new float[no_of_stereo_cameras];
				stereo_camera_tilt = new float[no_of_stereo_cameras];
				stereo_camera_roll = new float[no_of_stereo_cameras];
				image_width = new int[no_of_stereo_cameras];
				image_height = new int[no_of_stereo_cameras];
				FOV_degrees = new float[no_of_stereo_cameras];
			    left_camera_location = new pos3D[no_of_stereo_cameras];
			    right_camera_location = new pos3D[no_of_stereo_cameras];
				for (int i = 0; i < no_of_stereo_cameras; i++)
				{
					left_camera_location[i] = new pos3D(0,0,0);
					right_camera_location[i] = new pos3D(0,0,0);
				}
			}
			
			if (xnod.Name == "BaselineMillimetres")
			{
				baseline_mm[cameraIndex] = Convert.ToSingle(xnod.InnerText);
			}
			if (xnod.Name == "PositionMillimetres")
			{
				string[] str = xnod.InnerText.Split(' ');
				stereo_camera_position_x[cameraIndex] = Convert.ToSingle(str[0]);
				stereo_camera_position_y[cameraIndex] = Convert.ToSingle(str[1]);
				stereo_camera_position_z[cameraIndex] = Convert.ToSingle(str[2]);
			}
			if (xnod.Name == "OrientationDegrees")
			{
				string[] str = xnod.InnerText.Split(' ');
				stereo_camera_pan[cameraIndex] = Convert.ToSingle(str[0]) / 180.0f * (float)Math.PI;
				stereo_camera_tilt[cameraIndex] = Convert.ToSingle(str[1]) / 180.0f * (float)Math.PI;
				stereo_camera_roll[cameraIndex] = Convert.ToSingle(str[2]) / 180.0f * (float)Math.PI;
			}
			
			if (xnod.Name == "ImageDimensions")
			{
				string[] str = xnod.InnerText.Split(' ');
				image_width[cameraIndex] = Convert.ToInt32(str[0]);
				image_height[cameraIndex] = Convert.ToInt32(str[1]);
			}
			
			if (xnod.Name == "FOVDegrees")
			{
				FOV_degrees[cameraIndex] = Convert.ToSingle(xnod.InnerText);
			}			
			
            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1, ref cameraIndex);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }
		
        /// <summary>
        /// load geometry parameters from file
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
                int cameraIndex = -1;
                LoadFromXml(xnodDE, 0, ref cameraIndex);
                
                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }		
		
		#endregion
		
		#region "setters"
		
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
	        sampling_radius_major_mm = major_axis_mm;
	        sampling_radius_minor_mm = minor_axis_mm;			
		}
		
		/// <summary>
		///set the centre of rotation relative to the top left corner of the body 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public void SetCentreOfRotation(
		    float x, 
		    float y,
		    float z)
		{
		    body_centre_of_rotation_x = x;
		    body_centre_of_rotation_y = y;
		    body_centre_of_rotation_z = z;
		}
		
		/// <summary>
		///sets the centroid position of the robot's head 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public void SetHeadPosition(
		    float x,
            float y,
		    float z)
		{
			head_centroid_x = x;
			head_centroid_y = y;
			head_centroid_z = z;
		}

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
			head_pan = pan_degrees * (float)Math.PI / 180.0f;
			head_tilt = tilt_degrees * (float)Math.PI / 180.0f;
			head_roll = roll_degrees * (float)Math.PI / 180.0f;
		}
		
		/// <summary>
		/// sets the position and orientation of the robots head
		/// </summary>
		/// <param name="p">
		/// A <see cref="pos3D"/>
		/// </param>
		public void SetHeadPositionOrientation(pos3D p)
		{
			head_centroid_x = p.x;
			head_centroid_y = p.y;
			head_centroid_z = p.z;
			head_pan = p.pan;
			head_tilt = p.tilt;
			head_roll = p.roll;
		}
		
		public void SetBodyDimensions(
		    float width_mm,
		    float length_mm,
		    float height_mm)
		{
			body_width_mm = width_mm;
			body_length_mm = length_mm;
			body_height_mm = height_mm;
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
			max_orientation_variance = pan_degrees * (float)Math.PI / 180.0f;
			max_tilt_variance = tilt_degrees * (float)Math.PI / 180.0f;
			max_roll_variance = roll_degrees * (float)Math.PI / 180.0f;
		}
		
		#endregion

		#region "loading and saving sensor models"
		
        /// <summary>
        /// parse an xml node to extract buffer parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXmlSensorModels(
		    XmlNode xnod, int level,
		    ref int no_of_stereo_cameras,
		    ref int no_of_grid_levels,
            ref int camera_index,
		    ref int grid_level)
        {
            XmlNode xnodWorking;

			if (xnod.Name == "NoOfStereoCameras")
			{
				no_of_stereo_cameras = Convert.ToInt32(xnod.InnerText);
			}
			if (xnod.Name == "NoOfGridLevels")
			{
				no_of_grid_levels = Convert.ToInt32(xnod.InnerText);
				camera_index = 0;
				grid_level = -1;
			    
				sensormodel = new stereoModel[no_of_stereo_cameras][];
			    for (int stereo_cam = 0; stereo_cam < no_of_stereo_cameras; stereo_cam++)
			    {
				    sensormodel[stereo_cam] = new stereoModel[no_of_grid_levels];
				    for (int size = 0; size < no_of_grid_levels; size++)
				    {
				        sensormodel[stereo_cam][size] = new stereoModel();
					    sensormodel[stereo_cam][size].ray_model = new rayModelLookup(1,1);
				    }
			    }
			}
			if (xnod.Name == "Model")
			{
				grid_level++;
				if (grid_level >= no_of_grid_levels)
				{
					grid_level = 0;
				    camera_index++;
				}
				List<string> rayModelsData = new List<string>();
				sensormodel[camera_index][grid_level].ray_model.LoadFromXml(xnod, level+1, rayModelsData);
				sensormodel[camera_index][grid_level].ray_model.LoadSensorModelData(rayModelsData);
				if (rayModelsData.Count == 0)
				{
					Console.WriteLine("Warning: ray models not loaded");
				}
			}
			
            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXmlSensorModels(xnodWorking, level + 1,
		                ref no_of_stereo_cameras,
		                ref no_of_grid_levels,
                        ref camera_index,
		                ref grid_level);
					            
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }
		
        /// <summary>
        /// load buffer parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public bool LoadSensorModels(string filename)
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
		        int no_of_stereo_cameras = 0;
		        int no_of_grid_levels = 0;
                int camera_index = 0;
		        int grid_level = 0;
                LoadFromXmlSensorModels(xnodDE, 0,
				    ref no_of_stereo_cameras,
				    ref no_of_grid_levels,
				    ref camera_index,
				    ref grid_level);
								                           
                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }				
		
		
        public XmlElement getXmlSensorModels(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeModels = doc.CreateElement("StereoCameraSensorModels");
            parent.AppendChild(nodeModels);								
			
			int no_of_stereo_cameras = image_width.Length;
			int no_of_grid_levels = sensormodel[0].Length;
            xml.AddTextElement(doc, nodeModels, "NoOfStereoCameras", Convert.ToString(no_of_stereo_cameras));
            xml.AddTextElement(doc, nodeModels, "NoOfGridLevels", Convert.ToString(no_of_grid_levels));

			for (int stereo_cam = 0; stereo_cam < no_of_stereo_cameras; stereo_cam++)
			{
				for (int size = 0; size < no_of_grid_levels; size++)
				{
                    XmlElement nodeCamera = doc.CreateElement("Model");
                    nodeModels.AppendChild(nodeCamera);						
					
					rayModelLookup ray_model = sensormodel[stereo_cam][size].ray_model;

					nodeCamera.AppendChild(
			            ray_model.getXml(doc, nodeCamera));
				}
			}
			
            return (nodeModels);
        }

        /// <summary>
        /// return an Xml document containing sensor models
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocumentSensorModels()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlElement nodeSensors = doc.CreateElement("Sensors");
            doc.AppendChild(nodeSensors);
			
            nodeSensors.AppendChild(
			    getXmlSensorModels(doc, nodeSensors));

            return (doc);
        }

        /// <summary>
        /// save buffer as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void SaveSensorModels(string filename)
        {
            XmlDocument doc = getXmlDocumentSensorModels();
            doc.Save(filename);
        }
		
		#endregion
		
	}
}
