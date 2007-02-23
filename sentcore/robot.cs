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
        public int no_of_stereo_features;  // required number of stereo features per camera pair
        public stereoModel sensorModel;
        public stereoCorrespondence correspondence;
        int correspondence_algorithm_type = 1;  //the type of stereo correspondance algorithm to be used

        public String Name = "My Robot";
        public float TotalMass_kg;

        // dimensions of the body
        public float BodyWidth_mm;
        public float BodyLength_mm;
        public float BodyHeight_mm;

        // the type of propulsion for the robot
        public int propulsionType = 0;

        // wheel settings
        public float WheelDiameter_mm;
        public int WheelPositionFeedbackType = 0;
        public int GearRatio = 30;
        public int CountsPerRev = 4096;

        public int HeadType = 0;
        public float HeadSize_mm;
        public int HeadShape = 0;
        public float HeadHeight_mm = 1000;
        public int CameraOrientation = 0;


        // odometry
        public long leftWheelCounts, rightWheelCounts;

        private void init(int no_of_stereo_cameras, int no_of_stereo_features)
        {
            this.no_of_stereo_cameras = no_of_stereo_cameras;
            this.no_of_stereo_features = no_of_stereo_features;

            correspondence = new stereoCorrespondence(no_of_stereo_features);
            head = new stereoHead(no_of_stereo_cameras);
            sensorModel = new stereoModel();
        }

        public robot()
            : base(0, 0, 0)
        {
        }

        public robot(int no_of_stereo_cameras, int no_of_stereo_features)
            : base(0, 0, 0)
        {
            init(no_of_stereo_cameras, no_of_stereo_features);
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

        public void initRobotSentience()
        {
            head.initSentience();

            // using Creative Webcam NX Ultra - 78 degrees FOV
            sensorModel.FOV_horizontal = 78 * (float)Math.PI / 180.0f;
            sensorModel.FOV_vertical = 39 * (float)Math.PI / 180.0f;
            sensorModel.image_width = head.image_width;
            sensorModel.image_height = head.image_height;
            sensorModel.baseline = head.baseline_mm;
        }

        public void initQuadCam()
        {
            head.initQuadCam();

            // using Creative Webcam NX Ultra - 78 degrees FOV
            sensorModel.FOV_horizontal = 78 * (float)Math.PI / 180.0f;
            sensorModel.FOV_vertical = 39 * (float)Math.PI / 180.0f;
            sensorModel.image_width = head.image_width;
            sensorModel.image_height = head.image_height;
            sensorModel.baseline = head.baseline_mm;
        }

        public void initRobotSingleStereo()
        {
            head.initSingleStereoCamera();

            // using Creative Webcam NX Ultra - 78 degrees FOV
            sensorModel.FOV_horizontal = 78 * (float)Math.PI / 180.0f;
            sensorModel.FOV_vertical = 39 * (float)Math.PI / 180.0f;
            sensorModel.image_width = head.image_width;
            sensorModel.image_height = head.image_height;
            sensorModel.baseline = head.baseline_mm;
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

        public float loadRectifiedImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, int bytes_per_pixel)
        {
            return (correspondence.loadRectifiedImages(stereo_cam_index, fullres_left, fullres_right, head, no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        public float loadRawImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, int bytes_per_pixel)
        {
            // set the correspondence data for this camera
            correspondence.setCalibration(head.calibration[stereo_cam_index]);

            return (correspondence.loadRawImages(stereo_cam_index, fullres_left, fullres_right, head, no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        public void setMappingParameters(float sigma)
        {
            sensorModel.sigma = sigma;
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
            return (sensorModel.createViewpoint(head, (pos3D)this));
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

            util.AddComment(doc, nodeRobot, "Total mass in kilogrammes");
            util.AddTextElement(doc, nodeRobot, "TotalMassKilogrammes", Convert.ToString(TotalMass_kg));

            util.AddComment(doc, nodeRobot, "Width of the body in millimetres");
            util.AddTextElement(doc, nodeRobot, "BodyWidthMillimetres", Convert.ToString(BodyWidth_mm));

            util.AddComment(doc, nodeRobot, "Length of the body in millimetres");
            util.AddTextElement(doc, nodeRobot, "BodyLengthMillimetres", Convert.ToString(BodyLength_mm));

            util.AddComment(doc, nodeRobot, "Height of the body in millimetres");
            util.AddTextElement(doc, nodeRobot, "BodyHeightMillimetres", Convert.ToString(BodyHeight_mm));

            util.AddComment(doc, nodeRobot, "Propulsion type");
            util.AddTextElement(doc, nodeRobot, "PropulsionType", Convert.ToString(propulsionType));

            util.AddComment(doc, nodeRobot, "Wheel diameter in millimetres");
            util.AddTextElement(doc, nodeRobot, "WheelDiameterMillimetres", Convert.ToString(WheelDiameter_mm));

            util.AddComment(doc, nodeRobot, "Wheel Position feedback type");
            util.AddTextElement(doc, nodeRobot, "WheelPositionFeedbackType", Convert.ToString(WheelPositionFeedbackType));

            util.AddComment(doc, nodeRobot, "Motor gear ratio");
            util.AddTextElement(doc, nodeRobot, "GearRatio", Convert.ToString(GearRatio));

            util.AddComment(doc, nodeRobot, "Encoder counts per revolution");
            util.AddTextElement(doc, nodeRobot, "CountsPerRev", Convert.ToString(CountsPerRev));

            util.AddComment(doc, nodeRobot, "The type of head");
            util.AddTextElement(doc, nodeRobot, "HeadType", Convert.ToString(HeadType));

            util.AddComment(doc, nodeRobot, "Size of the head in millimetres");
            util.AddTextElement(doc, nodeRobot, "HeadSizeMillimetres", Convert.ToString(HeadSize_mm));

            util.AddComment(doc, nodeRobot, "Shape of the head");
            util.AddTextElement(doc, nodeRobot, "HeadShape", Convert.ToString(HeadShape));

            util.AddComment(doc, nodeRobot, "Height of the head above the ground in millimetres");
            util.AddTextElement(doc, nodeRobot, "HeadHeightMillimetres", Convert.ToString(HeadHeight_mm));

            util.AddComment(doc, nodeRobot, "Orientation of the cameras");
            util.AddTextElement(doc, nodeRobot, "CameraOrientation", Convert.ToString(CameraOrientation));

            util.AddComment(doc, nodeRobot, "Number of stereo cameras");
            util.AddTextElement(doc, nodeRobot, "NoOfStereoCameras", Convert.ToString(head.no_of_cameras));

            util.AddComment(doc, nodeRobot, "Stereo camera baseline in millimetres");
            util.AddTextElement(doc, nodeRobot, "CameraBaselineMillimetres", Convert.ToString(head.calibration[0].baseline));

            util.AddComment(doc, nodeRobot, "Stereo camera field of view in degrees");
            util.AddTextElement(doc, nodeRobot, "CameraFieldOfViewDegrees", Convert.ToString(head.calibration[0].leftcam.camera_FOV_degrees));

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
                int no_of_cameras = 0;
                int no_of_features_per_camera = 500;
                float camera_baseline_mm = 0;
                float camera_FOV_degrees = 0;
                LoadFromXml(xnodDE, 0, ref no_of_cameras, ref camera_baseline_mm, ref camera_FOV_degrees);
                
                // initialise with the loaded settings
                if ((no_of_cameras > 0) && (camera_baseline_mm > 0) && (camera_FOV_degrees > 0))
                {
                    init(no_of_cameras, no_of_features_per_camera);

                    for (int i = 0; i < no_of_cameras; i++)
                    {
                        head.calibration[i].baseline = camera_baseline_mm;
                        head.calibration[i].leftcam.camera_FOV_degrees = camera_FOV_degrees;
                        head.calibration[i].rightcam.camera_FOV_degrees = camera_FOV_degrees;
                    }
                }

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
                                ref int no_of_cameras, ref float camera_baseline_mm,
                                ref float camera_FOV_degrees)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "Name")
            {
                Name = xnod.InnerText;
            }

            if (xnod.Name == "TotalMassKilogrammes")
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

            if (xnod.Name == "PropulsionType")
            {
                propulsionType = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "WheelDiameterMillimetres")
            {
                WheelDiameter_mm = Convert.ToSingle(xnod.InnerText);
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

            if (xnod.Name == "HeadHeightMillimetres")
            {
                HeadHeight_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "CameraOrientation")
            {
                CameraOrientation = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "NoOfStereoCameras")
            {
                no_of_cameras = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "CameraBaselineMillimetres")
            {
                camera_baseline_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "CameraFieldOfViewDegrees")
            {
                camera_FOV_degrees = Convert.ToSingle(xnod.InnerText);
            }

                // call recursively on all children of the current node
                if ((xnod.HasChildNodes) && ((xnod.Name == "Robot") || (xnod.Name == "Sentience")))
                {
                    xnodWorking = xnod.FirstChild;
                    while (xnodWorking != null)
                    {
                        LoadFromXml(xnodWorking, level + 1, ref no_of_cameras, ref camera_baseline_mm, ref camera_FOV_degrees);
                        xnodWorking = xnodWorking.NextSibling;
                    }
                }
        }

        #endregion

    }
}
