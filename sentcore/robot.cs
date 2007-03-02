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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class robot : pos3D
    {        
        public int no_of_stereo_cameras;   // number of stereo cameras on the head

        // head geometry, stereo features and calibration data
        public stereoHead head;            

        // describes how the robot moves, used to predict the next step as a probabilistic distribution
        // of possible poses
        public motionModel motion;         

        // sensor models used for mapping and localisation
        public stereoModel sensorModelMapping;
        public stereoModel sensorModelLocalisation;

        // routines for performing stereo correspondence
        public stereoCorrespondence correspondence;
        int correspondence_algorithm_type = 1;  //the type of stereo correspondance algorithm to be used

        public String Name = "My Robot";          // name of the robot
        public float TotalMass_kg;                // total mass of the robot

        // dimensions of the body
        public float BodyWidth_mm;                // width of the body
        public float BodyLength_mm;               // length of the body
        public float BodyHeight_mm;               // height of the body from the ground
        public int BodyShape = 0;                 // shape of the body of the robot

        // the type of propulsion for the robot
        public int propulsionType = 0;            // the type of propulsion used

        // wheel settings
        public float WheelDiameter_mm;            // diameter of the wheel
        public float WheelBase_mm;                // wheel base length
        public float WheelBaseForward_mm;         // wheel base distance from the front of the robot
        public int WheelPositionFeedbackType = 0; // the type of position feedback 
        public int GearRatio = 30;                // motor gear ratio
        public int CountsPerRev = 4096;           // encoder counts per rev
        private long prev_left_wheel_encoder = 0, prev_right_wheel_encoder = 0;

        // motion limits
        public float MotorNoLoadSpeedRPM = 175;   // max motor speed with no load
        public float MotorTorqueKgMm = 80;        // toque rating in Kg/mm

        public int HeadType = 0;                  // type of head
        public float HeadSize_mm;                 // size of the head
        public int HeadShape = 0;                 // shape of the head
        public int CameraOrientation = 0;         // the general configuration of camera positions

        // grid settings
        public int LocalGridLevels = 1;           // The number of scales used within the local grid
        public int LocalGridDimension = 128;      // Cubic dimension of the local grid in cells
        public float LocalGridCellSize_mm = 32;   // Size of each grid cell (voxel) in millimetres
        public float LocalGridInterval_mm = 100;  // The distance which the robot must travel before new data is inserted into the grid during mapping
        public occupancygridMultiHypothesis LocalGrid;  // grid containing the current local observations
       

        #region "constructors"

        public robot()
            : base(0, 0, 0)
        {
        }

        public robot(int no_of_stereo_cameras)
            : base(0, 0, 0)
        {
            init(no_of_stereo_cameras);
            initDualCam();
        }

        #endregion

        #region "camera calibration"

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

        #endregion

        #region "initialisation functions"

        /// <summary>
        /// recreate the local grid
        /// </summary>
        private void createLocalGrid()
        {
            // create the local grid
            LocalGrid = new occupancygridMultiHypothesis(LocalGridDimension, (int)LocalGridCellSize_mm);
        }

        /// <summary>
        /// initialise with the given number of stereo cameras
        /// </summary>
        /// <param name="no_of_stereo_cameras"></param>
        private void init(int no_of_stereo_cameras)
        {
            this.no_of_stereo_cameras = no_of_stereo_cameras;

            // head and shoulders
            head = new stereoHead(no_of_stereo_cameras);

            // sensor model used for mapping
            sensorModelMapping = new stereoModel();
            sensorModelMapping.no_of_stereo_features = 300;
            correspondence = new stereoCorrespondence(sensorModelMapping.no_of_stereo_features);

            // sensor model used for localisation
            sensorModelLocalisation = new stereoModel();
            sensorModelLocalisation.mapping = false;
            sensorModelLocalisation.no_of_stereo_features = 100;

            // add a local occupancy grid
            createLocalGrid();

            // create a motion model
            motion = new motionModel(this);

            // zero encoder positions
            prev_left_wheel_encoder = 0;
            prev_right_wheel_encoder = 0;
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

        public void initDualCam()
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

        #endregion

        #region "paths"

        // odometry
        public long leftWheelCounts, rightWheelCounts;

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

        #endregion

        #region "image loading"

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

        #endregion

        #region "parameter setting"

        /// <summary>
        /// set the type of stereo correspondence algorithm
        /// </summary>
        /// <param name="algorithm_type"></param>
        public void setCorrespondenceAlgorithmType(int algorithm_type)
        {
            correspondence_algorithm_type = algorithm_type;
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

        #endregion

        #region "viewpoints"

        /// <summary>
        /// calculates a viewpoint based upon the stereo features currently
        /// associated with the head of the robot
        /// </summary>
        /// <returns></returns>
        public viewpoint getViewpoint()
        {
            return (sensorModelMapping.createViewpoint(head, (pos3D)this));
        }

        #endregion

        #region "update routines"

        // the path which was used to construct the current local grid
        private robotPath LocalGridPath = new robotPath();

        // the previous position and orientation of the robot
        private pos3D previousPosition = new pos3D(-1,-1,0);

        /// <summary>
        /// stores the previous position of the robot
        /// </summary>
        private void storePreviousPosition()
        {
            previousPosition.x = x;
            previousPosition.y = y;
            previousPosition.z = z;
            previousPosition.pan = pan;
            previousPosition.tilt = tilt;
        }

        private void loadImages(ArrayList images, bool mapping)
        {
            for (int i = 0; i < images.Count / 2; i++)
            {
                Byte[] left_image = (Byte[])images[i * 2];
                Byte[] right_image = (Byte[])images[(i * 2) + 1];
                loadRawImages(i, left_image, right_image, 3, mapping);
            }
        }

        /// <summary>
        /// check if the robot has moved out of bounds of the current local grid
        /// if so, create a new grid for it to move into and centre it appropriately
        /// </summary>
        private void checkOutOfBounds(bool mapping)
        {
            bool out_of_bounds = false;
            pos3D new_grid_centre = new pos3D(LocalGrid.x, LocalGrid.y, LocalGrid.z);

            float innermost_gridSize_mm = LocalGrid.cellSize_mm; // .getCellSize(LocalGrid.levels - 1);
            float border = innermost_gridSize_mm / 4.0f;
            if (x < LocalGrid.x - border)
            {
                new_grid_centre.x = LocalGrid.x - border;
                out_of_bounds = true;
            }
            if (x > LocalGrid.x + border)
            {
                new_grid_centre.x = LocalGrid.x + border;
                out_of_bounds = true;
            }
            if (y < LocalGrid.y - border)
            {
                new_grid_centre.y = LocalGrid.y - border;
                out_of_bounds = true;
            }
            if (y > LocalGrid.y + border)
            {
                new_grid_centre.y = LocalGrid.y + border;
                out_of_bounds = true;
            }

            if (out_of_bounds)
            {
                // file name for this grid, based upon its position
                String grid_filename = "grid" + Convert.ToString((int)Math.Round(LocalGrid.x / border)) + "_" +
                                                Convert.ToString((int)Math.Round(LocalGrid.y / border)) + ".grd";

                // store the path which was used to create the previous grid
                // this is far more efficient in terms of memory and disk storage
                // than trying to store the entire grid, most of which will be just empty cells
                LocalGridPath.Save(grid_filename);          

                // clear out the path data
                LocalGridPath.Clear();

                // make a new grid
                createLocalGrid();

                // position the grid
                LocalGrid.x = new_grid_centre.x;
                LocalGrid.y = new_grid_centre.y;
                LocalGrid.z = new_grid_centre.z;

                if (!mapping)
                {
                    // file name of the grid to be loaded
                    grid_filename = "grid" + Convert.ToString((int)Math.Round(LocalGrid.x / border)) + "_" +
                                             Convert.ToString((int)Math.Round(LocalGrid.y / border)) + ".grd";
                    LocalGridPath.Load(grid_filename);
                    
                    // TODO: update the local grid using the loaded path
                    //LocalGrid.insert(LocalGridPath, false);
                }

            }
        }

        private void update(ArrayList images, bool mapping)
        {
            if (images != null)
            {
                // load stereo images
                loadImages(images, mapping);

                // create an observation as a set of rays from the stereo correspondence results
                ArrayList stereo_rays = null;
                if (mapping)
                    stereo_rays = sensorModelMapping.createObservation(head);
                else
                    stereo_rays = sensorModelLocalisation.createObservation(head);

                // update all current poses with the observation
                motion.AddObservation(stereo_rays);

                /*
                // store the viewpoint in the path
                LocalGridPath.Add(v);

                // what's the relative position of the robot inside the grid ?
                pos3D relative_position = new pos3D(x - LocalGrid.x, y - LocalGrid.y, 0);
                relative_position.pan = pan - LocalGrid.pan;

                // have we moved off the current grid ?
                checkOutOfBounds(mapping);

                if (mapping)
                {
                    // update the grid
                    LocalGrid.insert(v, mapping, relative_position);
                }
                else
                {
                    // localise within the grid
                    robotLocalisation.surveyPoses(v, LocalGrid, motion);
                }
                 */
            }
        }

        /// <summary>
        /// update from a known position and orientation
        /// </summary>
        /// <param name="images">list of bitmap images (3bpp)</param>
        /// <param name="x">x position in mm</param>
        /// <param name="y">y position in mm</param>
        /// <param name="pan">pan angle in radians</param>
        /// <param name="mapping">mapping or localisation</param>
        public void updateFromKnownPosition(ArrayList images,
                                            float x, float y, float pan, 
                                            bool mapping)                   
        {
            // update the grid
            update(images, mapping);

            // set the robot at the known position
            this.x = x;
            this.y = y;
            this.pan = pan;

            if (!mapping)
            {
                // update the motion model
                motion.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
                if (!((previousPosition.x == -1) && (previousPosition.y == -1)))
                {
                    motion.forward_velocity = y - previousPosition.y;
                    motion.angular_velocity = pan - previousPosition.pan;
                    motion.Predict(1);
                }
            }
            storePreviousPosition();
        }

        public void Reset()
        {
            previousPosition.x = -1;
            previousPosition.y = -1;
            motion.Reset();
        }

        /// <summary>
        /// update using wheel encoder positions
        /// </summary>
        /// <param name="images"></param>
        /// <param name="left_wheel_encoder">left wheel encoder position</param>
        /// <param name="right_wheel_encoder">right wheel encoder position</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in sec</param>
        /// <param name="mapping">mapping or localisation</param>
        public void updateFromEncoderPositions(ArrayList images,
                                               long left_wheel_encoder, long right_wheel_encoder,
                                               float time_elapsed_sec, bool mapping)
        {
            // update the grid
            update(images, mapping);

            float wheel_circumference_mm = (float)Math.PI * WheelDiameter_mm;
            long countsPerWheelRev = CountsPerRev * GearRatio;

            if ((time_elapsed_sec > 0.00001f) && 
                (countsPerWheelRev > 0) && 
                (prev_left_wheel_encoder != 0))
            {
                // calculate angular velocity of the left wheel in radians/sec
                float angle_traversed_radians = (float)(left_wheel_encoder - prev_left_wheel_encoder) * 2 * (float)Math.PI / countsPerWheelRev;
                motion.LeftWheelAngularVelocity = angle_traversed_radians / time_elapsed_sec;                

                // calculate angular velocity of the right wheel in radians/sec
                angle_traversed_radians = (float)(right_wheel_encoder - prev_right_wheel_encoder) * 2 * (float)Math.PI / countsPerWheelRev;
                motion.RightWheelAngularVelocity = angle_traversed_radians / time_elapsed_sec;
            }

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_WHEEL_ANGULAR_VELOCITY;
            motion.Predict(time_elapsed_sec);

            prev_left_wheel_encoder = left_wheel_encoder;
            prev_right_wheel_encoder = right_wheel_encoder;

            storePreviousPosition();
        }


        /// <summary>
        /// update using known velocities
        /// </summary>
        /// <param name="images">list of bitmap images (3bpp)</param>
        /// <param name="forward_velocity">forward velocity in mm/sec</param>
        /// <param name="angular_velocity">angular velocity in radians/sec</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in sec</param>
        /// <param name="mapping">mapping or localisation</param>
        public void updateFromVelocities(ArrayList images, 
                                         float forward_velocity, float angular_velocity,
                                         float time_elapsed_sec, bool mapping)
        {
            // update the grid
            update(images, mapping);

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            motion.forward_velocity = forward_velocity;
            motion.angular_velocity = angular_velocity;
            motion.Predict(time_elapsed_sec);

            storePreviousPosition();
        }

        #endregion

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
            
            util.AddComment(doc, nodePropulsion, "Motor no load speed in RPM");
            util.AddTextElement(doc, nodePropulsion, "MotorNoLoadSpeedRPM", Convert.ToString(MotorNoLoadSpeedRPM));

            util.AddComment(doc, nodePropulsion, "Motor torque rating in Kg/mm");
            util.AddTextElement(doc, nodePropulsion, "MotorTorqueKgMm", Convert.ToString(MotorTorqueKgMm));            

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

            util.AddComment(doc, nodeOccupancyGrid, "The distance which the robot must travel before new data is inserted into the grid during mapping");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridIntervalMillimetres", Convert.ToString(LocalGridInterval_mm));

            nodeRobot.AppendChild(motion.getXml(doc, nodeRobot));

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

                createLocalGrid();
                
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

            if (xnod.Name == "MotorNoLoadSpeedRPM")
            {
                MotorNoLoadSpeedRPM = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "MotorTorqueKgMm")
            {
                MotorTorqueKgMm = Convert.ToSingle(xnod.InnerText);
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

            if (xnod.Name == "LocalGridIntervalMillimetres")
            {
                LocalGridInterval_mm = Convert.ToSingle(xnod.InnerText);
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

            if (xnod.Name == "MotionModel")
            {
                motion.LoadFromXml(xnod.FirstChild, level + 1);
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
