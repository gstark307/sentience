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
using sentience.pathplanner;

namespace sentience.core
{
    public class robot : pos3D
    {        
        public int no_of_stereo_cameras;   // number of stereo cameras on the head

        // object used for taking benchmark timings
        private stopwatch clock = new stopwatch();

        // timing results in milliseconds
        public long benchmark_stereo_correspondence;
        public long benchmark_observation_update;
        public long benchmark_garbage_collection;
        public long benchmark_prediction;

        // head geometry, stereo features and calibration data
        public stereoHead head;

        // describes how the robot moves, used to predict the next step as a probabilistic distribution
        // of possible poses
        public motionModel motion;

        // object used to construct rays and sensor models
        public stereoModel inverseSensorModel;

        // routines for performing stereo correspondence
        public stereoCorrespondence correspondence;

        //the type of stereo correspondance algorithm to be used
        int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_LINES; // .CORRESPONDENCE_CONTOURS;  

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
        public int LocalGridLevels = 1;               // The number of scales used within the local grid
        public int LocalGridDimension = 128;          // dimension of the local grid in cells (x-y plane)
        public int LocalGridDimensionVertical = 100;  // vertical (z) dimension of the local grid in cells
        public float LocalGridCellSize_mm = 100;      // Size of each grid cell (voxel) in millimetres
        public float LocalGridInterval_mm = 100;      // The distance which the robot must travel before new data is inserted into the grid during mapping
        public float LocalGridMappingRange_mm = 2500; // the maximum range of features used to update the grid map.  Otherwise very long range features end up hogging processor resource
        public float LocalGridLocalisationRadius_mm = 200;  // an extra radius applied when localising within the grid, to make localisation rays wider
        public float LocalGridVacancyWeighting = 1.0f;      // a weighting applied to the vacancy part of the sensor model
        public occupancygridMultiHypothesis LocalGrid;      // grid containing the current local observations

        // parameters discovered by auto tuning
        public String TuningParameters = "";

        // minimum colour variance discovered during auto tuning
        public float MinimumColourVariance = float.MaxValue;
        public float MinimumPositionError_mm = float.MaxValue;

        // whether to enable scan matching for more accurate pose estimation
        public bool EnableScanMatching = true;

        // when scan matching is used this is the maximum change in
        // the robots pan angle which is detectable per time step
        // This should not be bigger than a third of the horizontal field of view
        public int ScanMatchingMaxPanAngleChange = 20;

        // keeps an estimate of the robots pan angle based upon scan matching
        public float ScanMatchingPanAngleEstimate = scanMatching.NOT_MATCHED;

        // object containing path planning functions
        private sentience.pathplanner.pathplanner planner;

        // sites (waypoints) to which the robot may move
        private kmlZone worksites;

        #region "constructors"

        public robot()
            : base(0, 0, 0)
        {
        }

        public robot(int no_of_stereo_cameras)
            : base(0, 0, 0)
        {
            init(no_of_stereo_cameras);
            //initDualCam();
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
            LocalGrid = new occupancygridMultiHypothesis(LocalGridDimension, LocalGridDimensionVertical, (int)LocalGridCellSize_mm, (int)LocalGridLocalisationRadius_mm, (int)LocalGridMappingRange_mm, LocalGridVacancyWeighting);

            // create a path planning object linked to the grid
            planner = new sentience.pathplanner.pathplanner(LocalGrid.navigable_space, (int)LocalGridCellSize_mm, LocalGrid.x, LocalGrid.y);
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
            inverseSensorModel = new stereoModel();

            // set the number of stereo features to be detected and inserted into the grid
            // on each time step.  This figure should be large enough to get reasonable
            // detail, but not so large that the mapping consumes a huge amount of 
            // processing resource
            inverseSensorModel.no_of_stereo_features = 100;
            correspondence = new stereoCorrespondence(inverseSensorModel.no_of_stereo_features);

            // add a local occupancy grid
            createLocalGrid();

            // create a motion model
            motion = new motionModel(this);

            // a list of places where the robot might work or make observations
            worksites = new kmlZone();

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
        /*
        private void initSensorModel(stereoModel model,
                                     int image_width, int image_height,
                                     int FOV_degrees, int baseline_mm)
        {
            model.image_width = image_width;
            model.image_height = image_height;
            model.baseline = baseline_mm;
            model.FOV_horizontal = FOV_degrees * (float)Math.PI / 180.0f;
            model.FOV_vertical = model.FOV_horizontal * image_height / image_width;
            // 1/2 pixel standard deviation
            model.sigma = 1.0f / (image_width * 2) * model.FOV_horizontal;
            model.sigma *= image_width / 320; // makes the uncertainty invariant of resolution
        }
         */

        /*
        public void initDualCam()
        {
            head.initDualCam();

            initSensorModel(inverseSensorModel, head.image_width, head.image_height, 78, head.baseline_mm);
        }

        public void initQuadCam()
        {
            head.initQuadCam();

            // using Creative Webcam NX Ultra - 78 degrees FOV
            initSensorModel(inverseSensorModel, head.image_width, head.image_height, 78, head.baseline_mm);
        }

        public void initRobotSingleStereo()
        {
            head.initSingleStereoCamera(false);

            // using Creative Webcam NX Ultra - 78 degrees FOV
            initSensorModel(inverseSensorModel, head.image_width, head.image_height, 78, head.baseline_mm);
        }
         */

        #endregion

        #region "paths"

        // odometry
        public long leftWheelCounts, rightWheelCounts;

        #endregion

        #region "image loading"

        public float loadRectifiedImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, int bytes_per_pixel)
        {
            correspondence.setRequiredFeatures(inverseSensorModel.no_of_stereo_features);

            return (correspondence.loadRectifiedImages(stereo_cam_index, fullres_left, fullres_right, head, inverseSensorModel.no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        public float loadRawImages(int stereo_cam_index, Byte[] fullres_left, Byte[] fullres_right, int bytes_per_pixel)
        {
            correspondence.setRequiredFeatures(inverseSensorModel.no_of_stereo_features);

            // set the calibration data for this camera
            correspondence.setCalibration(head.calibration[stereo_cam_index]);

            return (correspondence.loadRawImages(stereo_cam_index, fullres_left, fullres_right, head, inverseSensorModel.no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        #endregion

        #region "parameter setting"

        /// <summary>
        /// set the tuning parameters from a comma separated string
        /// </summary>
        /// <param name="parameters">comma separated tuning parameters</param>
        public void SetTuningParameters(String parameters)
        {
            this.TuningParameters = parameters;

            String[] tuningParams = parameters.Split(',');
            float[] tuningParameters = new float[tuningParams.Length];
            for (int i = 0; i < tuningParams.Length; i++)
                tuningParameters[i] = Convert.ToSingle(tuningParams[i]);

            // Motion model culling threshold
            motion.cull_threshold = (int)tuningParameters[0];
            // Localisation radius
            LocalGridLocalisationRadius_mm = tuningParameters[1];
            // Number of position uncertainty particles
            motion.survey_trial_poses = (int)tuningParameters[2];
            // A weighting factor which determines how aggressively the vacancy part of the sensor model carves out space
            LocalGridVacancyWeighting = tuningParameters[3];
            LocalGrid.vacancy_weighting = tuningParameters[3];
            // surround radius percent (for contour stereo) and matching threshold
            setStereoParameters(10, 100, tuningParameters[4], tuningParameters[5]);
        }

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
            inverseSensorModel.sigma = sigma;
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

        public void setStereoParameters(int max_disparity, int required_features, 
                                        float surround_radius, float matching_threshold)
        {
            correspondence.setMaxDisparity(max_disparity);
            correspondence.setRequiredFeatures(required_features);
            correspondence.setSurroundRadius(surround_radius);
            correspondence.setMatchingThreshold(matching_threshold);
        }

        #endregion

        #region "update routines"

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

        private void loadImages(ArrayList images)
        {            
            clock.Start();

            bool scanMatchesFound = false;

            for (int i = 0; i < images.Count / 2; i++)
            {
                Byte[] left_image = (Byte[])images[i * 2];
                Byte[] right_image = (Byte[])images[(i * 2) + 1];
                loadRawImages(i, left_image, right_image, 3);

                // perform scan matching for forwards or rearwards looking cameras
                float pan_angle = head.pan + head.cameraPosition[i].pan;
                if ((pan_angle == 0) || (pan_angle == (float)Math.PI))
                {
                    // create a scan matching object if needed
                    if (head.scanmatch[i] == null)
                        head.scanmatch[i] = new scanMatching();

                    if (EnableScanMatching)
                    {
                        // perform scan matching
                        head.scanmatch[i].update(correspondence.getRectifiedImage(true),
                                                 head.calibration[i].leftcam.image_width,
                                                 head.calibration[i].leftcam.image_height,
                                                 head.calibration[i].leftcam.camera_FOV_degrees * (float)Math.PI / 180.0f,
                                                 head.cameraPosition[i].roll,
                                                 ScanMatchingMaxPanAngleChange * (float)Math.PI / 180.0f);
                        if (head.scanmatch[i].pan_angle_change != scanMatching.NOT_MATCHED)
                        {
                            scanMatchesFound = true;
                            if (ScanMatchingPanAngleEstimate == scanMatching.NOT_MATCHED)
                            {
                                // if this is the first time a match has been found 
                                // use the current pan estimate
                                ScanMatchingPanAngleEstimate = pan;
                            }
                            else
                            {
                                if (pan_angle == 0)
                                    // forward facing camera
                                    ScanMatchingPanAngleEstimate -= head.scanmatch[i].pan_angle_change;
                                else
                                    // rearward facing camera
                                    ScanMatchingPanAngleEstimate += head.scanmatch[i].pan_angle_change;
                            }
                        }
                    }
                }                 
                else head.scanmatch[i] = null;
            }

            // if no scan matches were found set the robots pan angle estimate accordingly
            if (!scanMatchesFound) ScanMatchingPanAngleEstimate = scanMatching.NOT_MATCHED;

            clock.Stop();
            benchmark_stereo_correspondence = clock.time_elapsed_mS;
        }

        /// <summary>
        /// check if the robot has moved out of bounds of the current local grid
        /// if so, create a new grid for it to move into and centre it appropriately
        /// </summary>
        private void checkOutOfBounds()
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
                //LocalGridPath.Save(grid_filename);          

                // clear out the path data
                //LocalGridPath.Clear();

                // make a new grid
                createLocalGrid();

                // position the grid
                LocalGrid.SetCentrePosition(new_grid_centre.x, new_grid_centre.x);
                LocalGrid.z = new_grid_centre.z;

                // file name of the grid to be loaded
                grid_filename = "grid" + Convert.ToString((int)Math.Round(LocalGrid.x / border)) + "_" +
                                         Convert.ToString((int)Math.Round(LocalGrid.y / border)) + ".grd";
                //LocalGridPath.Load(grid_filename);
                    
                // TODO: update the local grid using the loaded path
                //LocalGrid.insert(LocalGridPath, false);
            }
        }

        /// <summary>
        /// returns the average colour variance for the entire occupancy grid
        /// </summary>
        /// <returns></returns>
        public float GetMeanColourVariance()
        {
            float mean_variance = 0;

            if (motion.best_path != null)
                if (motion.best_path.current_pose != null)
                    mean_variance = LocalGrid.GetMeanColourVariance(motion.best_path.current_pose);

            return (mean_variance);
        }

        private void update(ArrayList images)
        {
            if (images != null)
            {                
                // load stereo images
                loadImages(images);

                // create an observation as a set of rays from the stereo correspondence results
                ArrayList[] stereo_rays = new ArrayList[head.no_of_cameras];
                for (int cam = 0; cam < head.no_of_cameras; cam++)
                    stereo_rays[cam] = inverseSensorModel.createObservation(head, cam);

                clock.Start();

                // update all current poses with the observation
                motion.AddObservation(stereo_rays, false);

                clock.Stop();
                benchmark_observation_update = clock.time_elapsed_mS;
               
                // what's the relative position of the robot inside the grid ?
                pos3D relative_position = new pos3D(x - LocalGrid.x, y - LocalGrid.y, 0);
                relative_position.pan = pan - LocalGrid.pan;

                clock.Start();

                // randomly garbage collect a percentage of the grid cells
                // this removes dead hypotheses which would otherwise clogg
                // up the system
                LocalGrid.GarbageCollect(90);

                clock.Stop();
                benchmark_garbage_collection = clock.time_elapsed_mS;

                // have we moved off the current grid ?
                //checkOutOfBounds(); 
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
                                            float x, float y, float pan)                   
        {
            // update the grid
            update(images);

            // set the robot at the known position
            this.x = x;
            this.y = y;
            this.pan = pan;

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            if (!((previousPosition.x == -1) && (previousPosition.y == -1)))
            {
                float time_per_index_sec = 1;

                float dx = x - previousPosition.x;
                float dy = y - previousPosition.y;
                float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float acceleration = (2 * (distance - (motion.forward_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float forward_velocity = motion.forward_velocity + (acceleration * time_per_index_sec);
                motion.forward_acceleration = acceleration; // forward_velocity - motion.forward_velocity;
                motion.forward_velocity = forward_velocity;

                distance = pan - previousPosition.pan;
                acceleration = (2 * (distance - (motion.angular_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity = motion.angular_velocity + (acceleration * time_per_index_sec);
                motion.angular_velocity = angular_velocity;

                clock.Start();

                motion.Predict(time_per_index_sec);

                clock.Stop();
                benchmark_prediction = clock.time_elapsed_mS;
            }

            storePreviousPosition();
        }

        /// <summary>
        /// reset the pose list for the robot
        /// </summary>
        /// <param name="mode">a motionmodel MODE constant: egocentric or monte carlo</param>
        public void Reset(int mode)
        {
            previousPosition.x = -1;
            previousPosition.y = -1;
            motion.Reset(mode);
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
                                               float time_elapsed_sec)
        {
            // update the grid
            update(images);

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

            clock.Start();

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_WHEEL_ANGULAR_VELOCITY;
            motion.Predict(time_elapsed_sec);

            clock.Stop();
            benchmark_prediction = clock.time_elapsed_mS;

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
        public void updateFromVelocities(ArrayList images, 
                                         float forward_velocity, float angular_velocity,
                                         float time_elapsed_sec)
        {
            // update the grid
            update(images);

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            motion.forward_acceleration = forward_velocity - motion.forward_velocity;
            motion.forward_velocity = forward_velocity;
            motion.angular_acceleration = angular_velocity - motion.angular_velocity;
            motion.angular_velocity = angular_velocity;

            clock.Start();

            motion.Predict(time_elapsed_sec);

            clock.Stop();
            benchmark_prediction = clock.time_elapsed_mS;

            storePreviousPosition();
        }

        #endregion

        #region "planning"

        // a planned path which the robot is following
        particlePath planned_path;

        /// <summary>
        /// remove the named waypoint
        /// </summary>
        /// <param name="name"></param>
        public void RemoveWaypoint(String name)
        {
            kmlPlacemarkPoint waypoint = worksites.GetPoint(name);
            if (waypoint != null)
                worksites.Points.Remove(waypoint);
        }

        /// <summary>
        /// adds a waypoint for the current robot position to the set of work sites
        /// </summary>
        /// <param name="name"></param>
        public void AddWaypoint(String name)
        {
            kmlPlacemarkPoint waypoint = new kmlPlacemarkPoint();
            waypoint.SetPositionMillimetres(x, y);
            worksites.Add(waypoint);
            SaveWorkSite("worksites.kml");
        }

        /// <summary>
        /// add a waypoint at the specified position
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position_x_mm"></param>
        /// <param name="position_y_mm"></param>
        public void AddWaypoint(String name, float position_x_mm, float position_y_mm)
        {
            kmlPlacemarkPoint waypoint = new kmlPlacemarkPoint();
            waypoint.SetPositionMillimetres(position_x_mm, position_y_mm);
            worksites.Add(waypoint);
            SaveWorkSite("worksites.kml");
        }

        /// <summary>
        /// plan a route to a given location
        /// </summary>
        /// <param name="destination_waypoint"></param>
        public void PlanRoute(String destination_waypoint)
        {
            // clear the planned path
            planned_path = null;

            // retrieve the waypoint from the list of work sites
            kmlPlacemarkPoint waypoint = worksites.GetPoint(destination_waypoint);
            if (waypoint != null)
            {
                // get the destination waypoint position in millimetres
                // (the KML format stores positions in degrees)
                float destination_x = 0, destination_y = 0;
                waypoint.GetPositionMillimetres(ref destination_x, ref destination_y);

                // create a planner
                if (planner == null)
                    planner = new sentience.pathplanner.pathplanner(LocalGrid.navigable_space, (int)LocalGridCellSize_mm, LocalGrid.x, LocalGrid.y);

                // set variables, in the unlikely case that the centre position
                // of the grid has been changed since the planner was initialised
                planner.init(LocalGrid.navigable_space, LocalGrid.x, LocalGrid.y);

                // update the planner, in order to assign safety scores to the
                // navigable space.  The efficiency of this could be improved
                // by updating only those areas of the map which have changed
                planner.Update(0, 0, LocalGridDimension - 1, LocalGridDimension - 1);

                // create the plan
                ArrayList new_plan = planner.CreatePlan(x, y, destination_x, destination_y);
                if (new_plan.Count > 0)
                {
                    // convert the plan into a set of poses
                    planned_path = new particlePath(new_plan.Count / 2);
                    float prev_xx = 0, prev_yy = 0;
                    for (int i = 0; i < new_plan.Count; i += 2)
                    {
                        float xx = (float)new_plan[i];
                        float yy = (float)new_plan[i + 1];
                        if (i > 0)
                        {
                            float dx = xx - prev_xx;
                            float dy = yy - prev_yy;
                            float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                            if (dist > 0)
                            {
                                // TODO: check this pan angle
                                float pan = (float)Math.Sin(dx / dist);
                                if (dy < 0) pan = (2 * (float)Math.PI) - pan;

                                // create a pose and add it to the planned path
                                particlePose new_pose = new particlePose(prev_xx, prev_yy, pan, planned_path);
                                planned_path.Add(new_pose);
                            }
                        }
                        prev_xx = xx;
                        prev_yy = yy;
                    }
                }
            }
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show the occupancy grid
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ShowGrid(int view_type, Byte[] img, int width, int height, bool show_robot, 
                             bool colour, bool scalegrid)
        {
            if (motion.best_path != null)
                if (motion.best_path.current_pose != null)
                {
                    LocalGrid.Show(view_type, img, width, height, motion.best_path.current_pose, colour, scalegrid);
                    if (show_robot)
                    {
                        if (view_type == occupancygridMultiHypothesis.VIEW_ABOVE)
                        {
                            int half_grid_dimension_mm = (LocalGrid.dimension_cells * LocalGrid.cellSize_mm) / 2;
                            int min_x = (int)(LocalGrid.x - half_grid_dimension_mm);
                            int min_y = (int)(LocalGrid.y - half_grid_dimension_mm);
                            int max_x = (int)(LocalGrid.x + half_grid_dimension_mm);
                            int max_y = (int)(LocalGrid.y + half_grid_dimension_mm);
                            motion.ShowBestPose(img, width, height, min_x, min_y, max_x, max_y, false, false);
                        }
                    }
                }
        }

        /// <summary>
        /// shows the tree of possible paths
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ShowPathTree(Byte[] img, int width, int height)
        {
            motion.ShowTree(img, width, height, 0, 0, 0, 0);
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

                if (head.sensormodel[i] != null)
                    nodeSensorPlatform.AppendChild(head.sensormodel[i].getXml(doc, nodeRobot));
            }

            XmlElement nodeOccupancyGrid = doc.CreateElement("OccupancyGrid");
            nodeRobot.AppendChild(nodeOccupancyGrid);

            util.AddComment(doc, nodeOccupancyGrid, "The number of scales used within the local grid");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridLevels", Convert.ToString(LocalGridLevels));

            util.AddComment(doc, nodeOccupancyGrid, "The number of scales used within the local grid");
            util.AddTextElement(doc, nodeOccupancyGrid, "EnableScanMatching", Convert.ToString(EnableScanMatching));

            util.AddComment(doc, nodeOccupancyGrid, "Dimension of the local grid in the XY plane in cells");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridDimension", Convert.ToString(LocalGridDimension));

            util.AddComment(doc, nodeOccupancyGrid, "Dimension of the local grid in the vertical (Z) plane in cells");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridDimensionVertical", Convert.ToString(LocalGridDimensionVertical));

            util.AddComment(doc, nodeOccupancyGrid, "Size of each grid cell (voxel) in millimetres");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridCellSizeMillimetres", Convert.ToString(LocalGridCellSize_mm));

            util.AddComment(doc, nodeOccupancyGrid, "The distance which the robot must travel before new data is inserted into the grid during mapping");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridIntervalMillimetres", Convert.ToString(LocalGridInterval_mm));

            util.AddComment(doc, nodeOccupancyGrid, "An extra radius applied when localising within the grid, to make localisation rays wider");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridLocalisationRadiusMillimetres", Convert.ToString(LocalGridLocalisationRadius_mm));

            util.AddComment(doc, nodeOccupancyGrid, "When updating the grid map this is the maximum range within which cells will be updated");
            util.AddComment(doc, nodeOccupancyGrid, "This prevents the system from being slowed down by the insertion of a lot of very long range rays");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridMappingRangeMillimetres", Convert.ToString(LocalGridMappingRange_mm));

            util.AddComment(doc, nodeOccupancyGrid, "A weighting factor which determines how aggressively the vacancy part of the sensor model carves out space");
            util.AddTextElement(doc, nodeOccupancyGrid, "LocalGridVacancyWeighting", Convert.ToString(LocalGridVacancyWeighting));

            if (TuningParameters != "")
            {
                util.AddComment(doc, nodeOccupancyGrid, "Parameters discovered by auto tuning");
                util.AddTextElement(doc, nodeOccupancyGrid, "TuningParameters", TuningParameters);

                if (MinimumColourVariance != float.MaxValue)
                {
                    util.AddComment(doc, nodeOccupancyGrid, "Minimum colour variance discovered by auto tuning");
                    util.AddTextElement(doc, nodeOccupancyGrid, "MinimumColourVariance", Convert.ToString(MinimumColourVariance));
                }

                if (MinimumPositionError_mm != float.MaxValue)
                {
                    util.AddComment(doc, nodeOccupancyGrid, "Minimum position error found during auto tuning");
                    util.AddTextElement(doc, nodeOccupancyGrid, "MinimumPositionErrorMillimetres", Convert.ToString(MinimumPositionError_mm));
                }
            }

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
        /// load worksite locations from a KML file
        /// </summary>
        /// <param name="Kml"></param>
        /// <returns></returns>
        public bool LoadWorkSite(String Kml)
        {
            bool loaded = false;

            if (File.Exists(Kml))
            {
                worksites = new kmlZone();
                worksites.Load(Kml);
            }
            return (loaded);
        }

        public void SaveWorkSite(String Kml)
        {
            if (worksites != null)
                worksites.Save(Kml);
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
                TuningParameters = "";
                int cameraIndex = 0;
                LoadFromXml(xnodDE, 0, ref cameraIndex);

                createLocalGrid();

                if (TuningParameters != "") 
                    SetTuningParameters(TuningParameters);
                
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

            if (xnod.Name == "EnableScanMatching")
            {
                String str = xnod.InnerText.ToUpper();
                if ((str =="TRUE") || (str == "YES") || (str == "ON") || 
                    (str == "ENABLE") || (str == "ENABLED"))
                    EnableScanMatching = true;
                else
                    EnableScanMatching = false;
            }

            if (xnod.Name == "LocalGridLevels")
            {
                LocalGridLevels = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridDimension")
            {
                LocalGridDimension = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridDimensionVertical")
            {
                LocalGridDimensionVertical = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridCellSizeMillimetres")
            {
                LocalGridCellSize_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridIntervalMillimetres")
            {
                LocalGridInterval_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridLocalisationRadiusMillimetres")
            {
                LocalGridLocalisationRadius_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridMappingRangeMillimetres")
            {
                LocalGridMappingRange_mm = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "LocalGridVacancyWeighting")
            {
                LocalGridVacancyWeighting = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "TuningParameters")
            {
                TuningParameters = xnod.InnerText;
            }

            if (xnod.Name == "MinimumColourVariance")
            {
                MinimumColourVariance = Convert.ToSingle(xnod.InnerText);
            }           

            if (xnod.Name == "MinimumPositionErrorMillimetres")
            {
                MinimumPositionError_mm = Convert.ToSingle(xnod.InnerText);
            }           
                        

            if (xnod.Name == "StereoCamera")
            {
                int camIndex = 0;
                head.calibration[cameraIndex].LoadFromXml(xnod, level + 1, ref camIndex);

                // set the position of the camera relative to the robots head
                head.cameraPosition[cameraIndex] = head.calibration[cameraIndex].positionOrientation;

                //initSensorModel(inverseSensorModel, head.image_width, head.image_height, (int)head.calibration[cameraIndex].leftcam.camera_FOV_degrees, head.baseline_mm);
                cameraIndex++;
            }

            if (xnod.Name == "InverseSensorModels")
            {
                ArrayList rayModelsData = new ArrayList();
                head.sensormodel[cameraIndex - 1] = new rayModelLookup(1, 1);
                head.sensormodel[cameraIndex-1].LoadFromXml(xnod, level + 1, rayModelsData);
                head.sensormodel[cameraIndex-1].LoadSensorModelData(rayModelsData);
            }

            if (xnod.Name == "MotionModel")
            {
                motion.LoadFromXml(xnod, level + 1);
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

        /// <summary>
        /// save the occupancy grid to file
        /// </summary>
        /// <param name="filename"></param>
        public void SaveGrid(String filename)
        {
            LocalGrid.Save(filename, motion.best_path.current_pose);
        }

        /// <summary>
        /// returns the grid data as a byte array suitable for
        /// subsequent zip compression
        /// </summary>
        /// <returns>occupancy grid data</returns>
        public Byte[] SaveGrid()
        {
            return (LocalGrid.Save(motion.best_path.current_pose));
        }

        public void LoadGrid(Byte[] data)
        {
            createLocalGrid();
            LocalGrid.Load(data);
        }

        /// <summary>
        /// load the occupancy grid from file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadGrid(String filename)
        {
            createLocalGrid();
            LocalGrid.Load(filename);
        }

        #endregion

    }
}
