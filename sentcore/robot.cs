/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class robot : pos3D
    {        
        public int no_of_stereo_cameras;   // number of stereo cameras on the head
        public stereoHead head;            // head geometry, stereo features and calibration data

        // sensor models used for mapping and localisation
        public stereoModel sensorModelMapping;
        public stereoModel sensorModelLocalisation;

        public stereoCorrespondence correspondence;
        int correspondence_algorithm_type = 1;  //the type of stereo correspondance algorithm to be used

        public String Name = "My Robot";
        public float TotalMass_kg;

        // dimensions of the body
        public float BodyWidth_mm;
        public float BodyLength_mm;
        public float BodyHeight_mm;
        public int BodyShape = 0;

        // the type of propulsion for the robot
        public int propulsionType = 0;

        // wheel settings
        public float WheelDiameter_mm;
        public float WheelBase_mm;
        public float WheelBaseForward_mm;
        public int WheelPositionFeedbackType = 0;
        public int GearRatio = 30;
        public int CountsPerRev = 4096;

        public int HeadType = 0;
        public float HeadSize_mm;
        public int HeadShape = 0;
        public int CameraOrientation = 0;

        // grid settings
        public int LocalGridLevels = 1;
        public int LocalGridDimension = 128;
        public float LocalGridCellSize_mm = 32;

        // odometry
        public long leftWheelCounts, rightWheelCounts;

        private void init(int no_of_stereo_cameras)
        {
            this.no_of_stereo_cameras = no_of_stereo_cameras;

            head = new stereoHead(no_of_stereo_cameras);
        
            sensorModelMapping = new stereoModel();
            sensorModelMapping.no_of_stereo_features = 300;
            correspondence = new stereoCorrespondence(sensorModelMapping.no_of_stereo_features);

            sensorModelLocalisation = new stereoModel();
            sensorModelLocalisation.mapping = false;
            sensorModelLocalisation.no_of_stereo_features = 100;
        }

        public robot()
            : base(0, 0, 0)
        {
        }

        public robot(int no_of_stereo_cameras)
            : base(0, 0, 0)
        {
            init(no_of_stereo_cameras);
            initRobotSentience();
        }

        /// <summary>
        /// loads calibration data for the given camera
        /// </summary>
        /// <param name="camera_index"></param>
        /// <param name="calibrationFilename"></param>
        public void loadCalibrationData(int camera_index, String calibrationFilename)
        {
            head.loadCalibrationData(camera_index, calibrationFilename);
        }

        /// <summary>
        /// load calibration data for all cameras from the given directory
        /// </summary>
        /// <param name="directory"></param>
        public void loadCalibrationData(String directory)
        {
            head.loadCalibrationData(directory);
        }

        /// <summary>
        /// set the type of stereo correspondence algorithm
        /// </summary>
        /// <param name="algorithm_type"></param>
        public void setCorrespondenceAlgorithmType(int algorithm_type)
        {
            correspondence_algorithm_type = algorithm_type;
        }

        /// <summary>
        /// initialise a sensor model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <param name="FOV_degrees"></param>
        /// <param name="baseline_mm"></param>
        private void initSensorModel(stereoModel model,
                                     int image_width, int image_height,
                                     int FOV_degrees, int baseline_mm)
        {
            model.image_width = image_width;
            model.image_height = image_height;
            model.baseline = baseline_mm;
            model.FOV_horizontal = FOV_degrees * (float)Math.PI / 180.0f;
            model.FOV_vertical = model.FOV_horizontal * image_height / image_width;
        }

        public void initRobotSentience()
        {
            head.initDualCam();

            initSensorModel(sensorModelMapping, 78, head.image_width, head.image_height, head.baseline_mm);
            initSensorModel(sensorModelLocalisation, 78, head.image_width, head.image_height, head.baseline_mm);
        }

        public void initQuadCam()
        {
            head.initQuadCam();

            // using Creative Webcam NX Ultra - 78 degrees FOV
            initSensorModel(sensorModelMapping, 78, head.image_width, head.image_height, head.baseline_mm);
            initSensorModel(sensorModelLocalisation, 78, head.image_width, head.image_height, head.baseline_mm);
        }

        public void initRobotSingleStereo()
        {
            head.initSingleStereoCamera(false);

            // using Creative Webcam NX Ultra - 78 degrees FOV
            initSensorModel(sensorModelMapping, 78, head.image_width, head.image_height, head.baseline_mm);
            initSensorModel(sensorModelLocalisation, 78, head.image_width, head.image_height, head.baseline_mm);
        }

        /// <summary>
        /// save the current position and stereo features to file
        /// </summary>
        /// <param name="binfile"></param>
        public void savePathPoint(BinaryWriter binfile)
        {
            // save the position and orientation
            // note that the position and pan angle here is just naively calculated 
            // from odometry with no filtering
            binfile.Write(x);
            binfile.Write(y);
            binfile.Write(pan);

            // save the stereo features
            head.saveStereoFeatures(binfile);
        }

        /// <summary>
        /// load path point data from file
        /// </summary>
        /// <param name="binfile"></param>
        /// <param name="rPath"></param>
        public void loadPathPoint(BinaryReader binfile, robotPath rPath)
        {
            // load position into the odometry
            float xx=-999, yy=0, ppan=0;
            try
            {
                xx = binfile.ReadSingle();
                yy = binfile.ReadSingle();
                ppan = binfile.ReadSingle();
            }
            catch
            {
            }

            if (xx != -999)
            {
                // load stereo features
                head.loadStereoFeatures(binfile);

                viewpoint view = getViewpoint();
                view.odometry_position.x = xx;
                view.odometry_position.y = yy;
                view.odometry_position.pan = ppan;
                rPath.Add(view);
            }
        }

        public float loadRectifiedImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, int bytes_per_pixel, bool mapping)
        {
            stereoModel sensorModel = sensorModelMapping;
            if (!mapping) sensorModel = sensorModelLocalisation;
            correspondence.setRequiredFeatures(sensorModel.no_of_stereo_features);

            return (correspondence.loadRectifiedImages(stereo_cam_index, fullres_left, fullres_right, head, sensorModel.no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        public float loadRawImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, int bytes_per_pixel, bool mapping)
        {
            stereoModel sensorModel = sensorModelMapping;
            if (!mapping) sensorModel = sensorModelLocalisation;
            correspondence.setRequiredFeatures(sensorModel.no_of_stereo_features);

            // set the calibration data for this camera
            correspondence.setCalibration(head.calibration[stereo_cam_index]);

            return (correspondence.loadRawImages(stereo_cam_index, fullres_left, fullres_right, head, sensorModel.no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        public void setMappingParameters(float sigma)
        {
            sensorModelMapping.sigma = sigma;
        }

        public void setStereoParameters(int max_disparity, int difference_threshold,
                                        int context_radius_1, int context_radius_2,
                                        int local_averaging_radius, int required_features)
        {
            correspondence.setDifferenceThreshold(difference_threshold);
            correspondence.setMaxDisparity(5);
            correspondence.setRequiredFeatures(required_features);
            correspondence.setLocalAverageRadius(local_averaging_radius);
            correspondence.setContextRadii(context_radius_1, context_radius_2);
        }

        /// <summary>
        /// calculates a viewpoint based upon the stereo features currently
        /// associated with the head of the robot
        /// </summary>
        /// <returns></returns>
        public viewpoint getViewpoint()
        {
            return (sensorModelMapping.createViewpoint(head, (pos3D)this));
        }

        /// <summary>
        /// update the given path using the current viewpoint
        /// </summary>
        /// <param name="path"></param>
        public void updatePath(robotPath path)
        {
            viewpoint view = getViewpoint();
            view.odometry_position.x = x;
            view.odometry_position.y = y;
            view.odometry_position.z = z;
            view.odometry_position.pan = pan;
            path.Add(view);
        }


        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeRobot = doc.CreateElement("Robot");
            parent.AppendChild(nodeRobot);

            util.AddComment(doc, nodeRobot, "Name");
            util.AddTextElement(doc, nodeRobot, "Name", Name);

            util.AddComment(doc, nodeRobot, "Total mass in kilograms");
            util.AddTextElement(doc, nodeRobot, "TotalMassKilograms", Convert.ToString(TotalMass_kg));

            XmlElement nodeBody = doc.CreateElement("BodyDimensions");
            nodeRobot.AppendChild(nodeBody);

            util.AddComment(doc, nodeBody, "Shape of the body");
            util.AddComment(doc, nodeBody, "0 - square");
            util.AddComment(doc, nodeBody, "1 - round");
            util.AddTextElement(doc, nodeBody, "BodyShape", Convert.ToString(BodyShape));

            util.AddComment(doc, nodeBody, "Width of the body in millimetres");
            util.AddTextElement(doc, nodeBody, "BodyWidthMillimetres", Convert.ToString(BodyWidth_mm));

            util.AddComment(doc, nodeBody, "Length of the body in millimetres");
            util.AddTextElement(doc, nodeBody, "BodyLengthMillimetres", Convert.ToString(BodyLength_mm));

            util.AddComment(doc, nodeBody, "Height of the body in millimetres");
            util.AddTextElement(doc, nodeBody, "BodyHeightMillimetres", Convert.ToString(BodyHeight_mm));

            XmlElement nodePropulsion = doc.CreateElement("PropulsionSystem");
            nodeRobot.AppendChild(nodePropulsion);

            util.AddComment(doc, nodePropulsion, "Propulsion type");
            util.AddTextElement(doc, nodePropulsion, "PropulsionType", Convert.ToString(propulsionType));

            util.AddComment(doc, nodePropulsion, "Distance between the wheels in millimetres");
            util.AddTextElement(doc, nodePropulsion, "WheelBaseMillimetres", Convert.ToString(WheelBase_mm));

            util.AddComment(doc, nodePropulsion, "How far from the front of the robot is the wheel base in millimetres");
            util.AddTextElement(doc, nodePropulsion, "WheelBaseForwardMillimetres", Convert.ToString(WheelBaseForward_mm));

            util.AddComment(doc, nodePropulsion, "Wheel diameter in millimetres");
            util.AddTextElement(doc, nodePropulsion, "WheelDiameterMillimetres", Convert.ToString(WheelDiameter_mm));

            util.AddComment(doc, nodePropulsion, "Wheel Position feedback type");
            util.AddTextElement(doc, nodePropulsion, "WheelPositionFeedbackType", Convert.ToString(WheelPositionFeedbackType));

            util.AddComment(doc, nodePropulsion, "Motor gear ratio");
            util.AddTextElement(doc, nodePropulsion, "GearRatio", Convert.ToString(GearRatio));

            util.AddComment(doc, nodePropulsion, "Encoder counts per revolution");
            util.AddTextElement(doc, nodePropulsion, "CountsPerRev", Convert.ToString(CountsPerRev));

            XmlElement nodeSensorPlatform = doc.CreateElement("SensorPlatform");
            nodeRobot.AppendChild(nodeSensorPlatform);

            util.AddComment(doc, nodeSensorPlatform, "Number of stereo cameras");
            util.AddTextElement(doc, nodeSensorPlatform, "NoOfStereoCameras", Convert.ToString(head.no_of_cameras));

            util.AddComment(doc, nodeSensorPlatform, "The type of head");
            util.AddTextElement(doc, nodeSensorPlatform, "HeadType", Convert.ToString(HeadType));

            util.AddComment(doc, nodeSensorPlatform, "Size of the head in millimetres");
            util.AddTextElement(doc, nodeSensorPlatform, "HeadSizeMillimetres", Convert.ToString(HeadSize_mm));

            util.AddComment(doc, nodeSensorPlatform, "Shape of the head");
            util.AddTextElement(doc, nodeSensorPlatform, "HeadShape", Convert.ToString(HeadShape));

            util.AddComment(doc, nodeSensorPlatform, "Offset of the head from the leftmost side of the robot in millimetres");
            util.AddTextElement(doc, nodeSensorPlatform, "HeadPositionLeftMillimetres", Convert.ToString(head.x));

            util.AddComment(doc, nodeSensorPlatform, "Offset of the head from the front of the robot in millimetres");
            util.AddTextElement(doc, nodeSensorPlatform, "HeadPositionForwardMillimetres", Convert.ToString(head.y));

            util.AddComment(doc, nodeSensorPlatform, "Height of the head above the ground in millimetres");
            util.AddTextElement(doc, nodeSensorPlatform, "HeadHeightMillimetres", Convert.ToString(head.z));

            util.AddComment(doc, nodeSensorPlatform, "Orientation of the cameras");
            util.AddTextElement(doc, nodeSensorPlatform, "CameraOrientation", Convert.ToString(CameraOrientation));

            for (int i = 0; i < head.no_of_cameras; i++)
            {
                nodeSensorPlatform.AppendChild(head.calibration[i].getXml(doc, nodeSensorPlatform, 2));
            }

            XmlElement nodeSensorModels = doc.CreateElement("SensorModels");
            nodeRobot.AppendChild(nodeSensorModels);

            nodeSensorModels.AppendChild(sensorModelMapping.getXml(doc, nodeSensorModels));
            nodeSensorModels.AppendChild(sensorModelLocalisation.getXml(doc, nodeSensorModels));

            XmlElement nodeOccupancyGrid = doc.CreateElement("OccupancyGrid");
            nodeRobot.AppendChild(nodeOccupancyGrid);

            util.AddComment(doc, nodeOccupancyGrid, "The number of scales used within the local grid");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridLevels", Convert.ToString(LocalGridLevels));

            util.AddComment(doc, nodeOccupancyGrid, "Cubic dimension of the local grid in cells");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridDimension", Convert.ToString(LocalGridDimension));

            util.AddComment(doc, nodeOccupancyGrid, "Size of each grid cell (voxel) in millimetres");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridCellSizeMillimetres", Convert.ToString(LocalGridCellSize_mm));

            return (nodeRobot);
        }

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeSentience = doc.CreateElement("Sentience");
            doc.AppendChild(nodeSentience);

            nodeSentience.AppendChild(getXml(doc, nodeSentience));

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public bool Load(String filename)
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
                int cameraIndex = 0;
                LoadFromXml(xnodDE, 0, ref cameraIndex);
                
                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level,
                                ref int cameraIndex)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "Name")
            {
                Name = xnod.InnerText;
            }

            if (xnod.Name == "TotalMassKilograms")
            {
                TotalMass_kg = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "BodyWidthMillimetres")
            {
                BodyWidth_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "BodyLengthMillimetres")
            {
                BodyLength_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "BodyHeightMillimetres")
            {
                BodyHeight_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "BodyShape")
            {
                BodyShape = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "PropulsionType")
            {
                propulsionType = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "WheelDiameterMillimetres")
            {
                WheelDiameter_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "WheelBaseMillimetres")
            {
                WheelBase_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "WheelBaseForwardMillimetres")
            {
                WheelBaseForward_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "WheelPositionFeedbackType")
            {
                WheelPositionFeedbackType = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "GearRatio")
            {
                GearRatio = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "CountsPerRev")
            {
                CountsPerRev = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "HeadType")
            {
                HeadType = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "HeadSizeMillimetres")
            {
                HeadSize_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "HeadShape")
            {
                HeadShape = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "HeadPositionLeftMillimetres")
            {
                head.x = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "HeadPositionForwardMillimetres")
            {
                head.y = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "HeadHeightMillimetres")
            {
                head.z = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "CameraOrientation")
            {
                CameraOrientation = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "NoOfStereoCameras")
            {
                int no_of_cameras = Convert.ToInt32(xnod.InnerText);
                init(no_of_cameras);
            }

            if (xnod.Name == "LocalGridLevels")
            {
                LocalGridLevels = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridDimension")
            {
                LocalGridDimension = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridCellSizeMillimetres")
            {
                LocalGridCellSize_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "StereoCamera")
            {
                int camIndex = 0;
                head.calibration[cameraIndex].LoadFromXml(xnod.FirstChild, level + 1, ref camIndex);

                head.image_width = head.calibration[cameraIndex].leftcam.image_width;
                head.image_height = head.calibration[cameraIndex].leftcam.image_height;
                initSensorModel(sensorModelMapping, head.image_width, head.image_height, (int)head.calibration[cameraIndex].leftcam.camera_FOV_degrees, head.baseline_mm);
                initSensorModel(sensorModelLocalisation, head.image_width, head.image_height, (int)head.calibration[cameraIndex].leftcam.camera_FOV_degrees, head.baseline_mm);
                cameraIndex++;
            }

            if (xnod.Name == "SensorModels")
            {
                sensorModelMapping.LoadFromXml(xnod.FirstChild, level + 1);
                sensorModelLocalisation.LoadFromXml(xnod.FirstChild, level + 1);
            }

            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) && 
                ((xnod.Name == "Robot") || 
                 (xnod.Name == "Sentience") ||
                 (xnod.Name == "BodyDimensions") ||
                 (xnod.Name == "PropulsionSystem") ||
                 (xnod.Name == "SensorPlatform") ||
                 (xnod.Name == "OccupancyGrid")
                 ))
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1, ref cameraIndex);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }
}
