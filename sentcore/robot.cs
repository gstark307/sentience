/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
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
using System.Collections;
using sentience.pathplanner;
using sluggish.utilities.xml;
using sluggish.utilities.timing;
using System.Threading;

namespace sentience.core
{
    public class robot : pos3D
    {
        // name of the robot
        public String Name = "My Robot";  

        // the number of threads to use for mapping
        private int mapping_threads = 1;
        
        // the number of rays per stereo camera to be thrown at each time step
        private const int rays_per_stereo_camera = 50;

        #region "benchmark timings"

        // object used for taking benchmark timings
        private stopwatch clock = new stopwatch();

        // timing results in milliseconds
        public long benchmark_stereo_correspondence;
        public long benchmark_observation_update;
        public long benchmark_garbage_collection;
        public long benchmark_prediction;
        public long benchmark_concurrency;

        #endregion

        #region "getting the best results"

        /// <summary>
        /// returns the best available motion model
        /// </summary>
        /// <returns></returns>
        public motionModel GetBestMotionModel()
        {
            return(motion[best_grid_index]);
        }

        /// <summary>
        /// returns the best performing (most probable) occupancy grid
        /// </summary>
        /// <returns></returns>
        public occupancygridMultiHypothesis GetBestGrid()
        {
            return (LocalGrid[best_grid_index]);
        }

        #endregion

        #region "various models"

        #region "motion model"

        // describes how the robot moves, used to predict the next step as a probabilistic distribution
        // of possible poses
        public motionModel[] motion;

        #endregion

        #region "inverse sensor model"

        // object used to construct rays and sensor models
        public stereoModel inverseSensorModel;

        #endregion

        #endregion

        #region "stereo correspondence"

        // routines for performing stereo correspondence
        public stereoCorrespondence[] correspondence;

        //the type of stereo correspondance algorithm to be used
        //int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_LINES;
        int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_CONTOURS;
        //int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_SIMPLE;

        #endregion

        #region "parameters"

        #region "body parameters"

        public float TotalMass_kg;                // total mass of the robot

        // dimensions of the body
        public float BodyWidth_mm;                // width of the body
        public float BodyLength_mm;               // length of the body
        public float BodyHeight_mm;               // height of the body from the ground
        public int BodyShape = 0;                 // shape of the body of the robot

        #endregion

        #region "propulsion parameters"

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

        #endregion

        #region "robot head parameters"

        // number of stereo cameras on the head
        public int no_of_stereo_cameras;

        // head geometry, stereo features and calibration data
        public stereoHead head;

        public int HeadType = 0;                  // type of head
        public float HeadSize_mm;                 // size of the head
        public int HeadShape = 0;                 // shape of the head
        public int CameraOrientation = 0;         // the general configuration of camera positions

        #endregion

        #region "occupancy grid parameters"

        // grid settings
        public int LocalGridLevels = 1;               // The number of scales used within the local grid
        public int LocalGridDimension = 128;          // dimension of the local grid in cells (x-y plane)
        public int LocalGridDimensionVertical = 100;  // vertical (z) dimension of the local grid in cells
        public float LocalGridCellSize_mm = 100;      // Size of each grid cell (voxel) in millimetres
        public float LocalGridInterval_mm = 100;      // The distance which the robot must travel before new data is inserted into the grid during mapping
        public float LocalGridMappingRange_mm = 2500; // the maximum range of features used to update the grid map.  Otherwise very long range features end up hogging processor resource
        public float LocalGridLocalisationRadius_mm = 200;  // an extra radius applied when localising within the grid, to make localisation rays wider
        public float LocalGridVacancyWeighting = 1.0f;      // a weighting applied to the vacancy part of the sensor model

        public int best_grid_index = 0;                     // array index of the best performing occupancy grid
        public occupancygridMultiHypothesis[] LocalGrid;    // grid containing the current local observations

        // parameters discovered by auto tuning
        public String TuningParameters = "";

        // minimum colour variance discovered during auto tuning
        public float MinimumColourVariance = float.MaxValue;
        public float MinimumPositionError_mm = float.MaxValue;

        #endregion

        #region "scan matching parameters"

        // whether to enable scan matching for more accurate pose estimation
        public bool EnableScanMatching = true;

        // when scan matching is used this is the maximum change in
        // the robots pan angle which is detectable per time step
        // This should not be bigger than a third of the horizontal field of view
        public int ScanMatchingMaxPanAngleChange = 20;

        // keeps an estimate of the robots pan angle based upon scan matching
        public float ScanMatchingPanAngleEstimate = scanMatching.NOT_MATCHED;

        #endregion

        #region "path planning parameters"

        // object containing path planning functions
        private sentience.pathplanner.pathplanner planner;

        // sites (waypoints) to which the robot may move
        private kmlZone worksites;

        #endregion

        #endregion

        #region "constructors"

        public robot()
            : base(0, 0, 0)
        {
        }

        public robot(int no_of_stereo_cameras)
            : base(0, 0, 0)
        {            
            init(no_of_stereo_cameras, 
                 rays_per_stereo_camera);
            //initDualCam();
        }

        #endregion

        #region "camera calibration"

        /// <summary>
        /// loads calibration data for the given camera
        /// </summary>
        /// <param name="camera_index">index of the stereo camera</param>
        /// <param name="calibrationFilename"></param>
        public void loadCalibrationData(int camera_index, String calibrationFilename)
        {
            head.loadCalibrationData(camera_index, calibrationFilename);
        }

        /// <summary>
        /// load calibration data for all cameras from the given directory
        /// </summary>
        /// <param name="directory">directory in which the calibration data resides</param>
        public void loadCalibrationData(String directory)
        {
            head.loadCalibrationData(directory);
        }

        #endregion

        #region "initialisation functions"

        /// <summary>
        /// recreate the local grid
        /// </summary>
        /// <param name="index">index number of the occupancy grid being considered</param>
        private void createLocalGrid(int index)
        {
            // create the local grid
            LocalGrid[index] = new occupancygridMultiHypothesis(LocalGridDimension, LocalGridDimensionVertical, (int)LocalGridCellSize_mm, (int)LocalGridLocalisationRadius_mm, (int)LocalGridMappingRange_mm, LocalGridVacancyWeighting);

            //createPlanner(LocalGrid[index]);
        }

        /// <summary>
        /// create a path planner for the given grid
        /// </summary>
        /// <param name="grid">occupancy grid</param>
        private void createPlanner(occupancygridMultiHypothesis grid)
        {
            if (planner == null)
                planner = new sentience.pathplanner.pathplanner(grid.navigable_space, (int)LocalGridCellSize_mm, grid.x, grid.y);
            else
                planner.navigable_space = grid.navigable_space;
        }

        /// <summary>
        /// initialise with the given number of stereo cameras
        /// </summary>
        /// <param name="no_of_stereo_cameras">the number of stereo cameras on the robot (not the total number of cameras)</param>
        /// <param name="rays_per_stereo_camera">the number of rays which will be thrown from each stereo camera per time step</param>
        private void init(int no_of_stereo_cameras, 
                          int rays_per_stereo_camera)
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
            inverseSensorModel.no_of_stereo_features = rays_per_stereo_camera;
			for (int i = 0; i < no_of_stereo_cameras; i++)
                correspondence[i] = new stereoCorrespondence(inverseSensorModel.no_of_stereo_features);

            // add local occupancy grids
            LocalGrid = new occupancygridMultiHypothesis[mapping_threads];
            for (int i = 0; i < mapping_threads; i++) createLocalGrid(i);

            // create a motion model for each possible grid
            motion = new motionModel[mapping_threads];
            for (int i = 0; i < mapping_threads; i++) motion[i] = new motionModel(this, LocalGrid[i], 100 * (i+1));

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

        /// <summary>
        /// loads images which have already been rectified
        /// </summary>
        /// <param name="stereo_cam_index">index of the stereo camera</param>
        /// <param name="fullres_left">left image data</param>
        /// <param name="fullres_right">right image data</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <returns></returns>
        public float loadRectifiedImages(int stereo_cam_index, 
                                         Byte[] fullres_left, 
                                         Byte[] fullres_right, 
                                         int bytes_per_pixel)
        {
            // set the required number of stereo features
            correspondence[stereo_cam_index].setRequiredFeatures(inverseSensorModel.no_of_stereo_features);

            // load the rectified images
            return (correspondence[stereo_cam_index].loadRectifiedImages(stereo_cam_index, fullres_left, fullres_right, head, inverseSensorModel.no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        /// <summary>
        /// load raw (unrectified) images
        /// </summary>
        /// <param name="stereo_cam_index">index of the stereo camera</param>
        /// <param name="fullres_left">left image data</param>
        /// <param name="fullres_right">right image data</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <returns></returns>
        public float loadRawImages(int stereo_cam_index, 
                                   Byte[] fullres_left, 
                                   Byte[] fullres_right, 
                                   int bytes_per_pixel)
        {
            correspondence[stereo_cam_index].setRequiredFeatures(inverseSensorModel.no_of_stereo_features);

            // set the calibration data for this camera
            correspondence[stereo_cam_index].setCalibration(head.calibration[stereo_cam_index]);

            return (correspondence[stereo_cam_index].loadRawImages(stereo_cam_index, fullres_left, fullres_right, head, inverseSensorModel.no_of_stereo_features, bytes_per_pixel, correspondence_algorithm_type));
        }

        #endregion

        #region "parameter setting"

        #region "setting the number of threads"

        /// <summary>
        /// return the number of mapping threads in use
        /// </summary>
        /// <returns></returns>
        public int GetMappingThreads()
        {
            return (mapping_threads);
        }

        /// <summary>
        /// set the number of mapping threads to be used
        /// </summary>
        /// <param name="no_of_threads"></param>
        public void SetMappingThreads(int no_of_threads)
        {
            mapping_threads = no_of_threads;

            // add local occupancy grids
            LocalGrid = new occupancygridMultiHypothesis[mapping_threads];
            for (int i = 0; i < mapping_threads; i++) createLocalGrid(i);

            // create a motion model for each possible grid
            motion = new motionModel[mapping_threads];
            for (int i = 0; i < mapping_threads; i++) motion[i] = new motionModel(this, LocalGrid[i], 100 * (i + 1));
        }

        #endregion

        #region "setting motion model parameters"

        /// <summary>
        /// set the number of trial poses within the motion model
        /// </summary>
        /// <param name="no_of_poses"></param>
        public void SetMotionModelTrialPoses(int no_of_poses)
        {
            for (int i = 0; i < mapping_threads; i++)
                motion[i].survey_trial_poses = no_of_poses;
        }

        /// <summary>
        /// set the culling threshold for the motion model
        /// </summary>
        /// <param name="culling_threshold"></param>
        public void SetMotionModelCullingThreshold(int culling_threshold)
        {
            for (int i = 0; i < mapping_threads; i++)
                motion[i].cull_threshold = culling_threshold;
        }

        #endregion

        #region "setting grid parameters"

        /// <summary>
        /// sets the position of the local grid
        /// </summary>
        /// <param name="x">x position in millimetres</param>
        /// <param name="y">y position in millimetres</param>
        /// <param name="z">z position in millimetres</param>
        public void SetLocalGridPosition(float x, float y, float z)
        {
            for (int i = 0; i < mapping_threads; i++)
            {
                LocalGrid[i].x = x;
                LocalGrid[i].y = y;
                LocalGrid[i].z = z;
            }
        }

        #endregion

        #region "setting optimisation parameters"

        /// <summary>
        /// set the tuning parameters from a comma separated string
        /// </summary>
        /// <param name="parameters">comma separated tuning parameters</param>
        public void SetTuningParameters(String parameters)
        {
            this.TuningParameters = parameters;

            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            String[] tuningParams = parameters.Split(',');
            float[] tuningParameters = new float[tuningParams.Length];
            for (int i = 0; i < tuningParams.Length; i++)
                tuningParameters[i] = Convert.ToSingle(tuningParams[i], format);

            // Motion model culling threshold
            for (int i = 0; i < mapping_threads; i++) motion[i].cull_threshold = (int)tuningParameters[0];
            // Localisation radius
            LocalGridLocalisationRadius_mm = tuningParameters[1];
            // Number of position uncertainty particles
            for (int i = 0; i < mapping_threads; i++) motion[i].survey_trial_poses = (int)tuningParameters[2];
            // A weighting factor which determines how aggressively the vacancy part of the sensor model carves out space
            LocalGridVacancyWeighting = tuningParameters[3];
            for (int i = 0; i < mapping_threads; i++) LocalGrid[i].vacancy_weighting = tuningParameters[3];
            // surround radius percent (for contour stereo) and matching threshold
            setStereoParameters(10, 100, tuningParameters[4], tuningParameters[5]);
        }

        #endregion

        #region "setting stereo correspondence parameters"

        /// <summary>
        /// set the type of stereo correspondence algorithm
        /// </summary>
        /// <param name="algorithm_type"></param>
        public void setCorrespondenceAlgorithmType(int algorithm_type)
        {
            correspondence_algorithm_type = algorithm_type;
        }

        /// <summary>
        /// set parameters used for stereo correspondence
        /// </summary>
        /// <param name="max_disparity"></param>
        /// <param name="difference_threshold"></param>
        /// <param name="context_radius_1"></param>
        /// <param name="context_radius_2"></param>
        /// <param name="local_averaging_radius"></param>
        /// <param name="required_features"></param>
        public void setStereoParameters(int max_disparity, int difference_threshold,
                                        int context_radius_1, int context_radius_2,
                                        int local_averaging_radius, int required_features)
        {
			for (int i = 0; i < no_of_stereo_cameras; i++)
			{
                correspondence[i].setDifferenceThreshold(difference_threshold);
                correspondence[i].setMaxDisparity(5);
                correspondence[i].setRequiredFeatures(required_features);
                correspondence[i].setLocalAverageRadius(local_averaging_radius);
                correspondence[i].setContextRadii(context_radius_1, context_radius_2);
			}
        }

        /// <summary>
        /// set parameters used for stereo correspondence
        /// </summary>
        /// <param name="max_disparity"></param>
        /// <param name="required_features"></param>
        /// <param name="surround_radius"></param>
        /// <param name="matching_threshold"></param>
        public void setStereoParameters(int max_disparity, int required_features, 
                                        float surround_radius, float matching_threshold)
        {
			for (int i = 0; i < no_of_stereo_cameras; i++)
			{
                correspondence[i].setMaxDisparity(max_disparity);
                correspondence[i].setRequiredFeatures(required_features);
                correspondence[i].setSurroundRadius(surround_radius);
                correspondence[i].setMatchingThreshold(matching_threshold);
			}
        }

        #endregion

        /// <summary>
        /// set parameters required for mapping
        /// </summary>
        /// <param name="sigma"></param>
        public void setMappingParameters(float sigma)
        {
            inverseSensorModel.sigma = sigma;
        }

        #endregion

        #region "mapping update"

        /// <summary>
        /// the mapping thread has called back
        /// </summary>
        /// <param name="state"></param>
        private static void MappingUpdateCallback(object state)
        {
            // get the returned state
            ThreadMappingState mstate = (ThreadMappingState)state;
        }

        /// <summary>
        /// mapping update, which can consist of multiple threads running concurrently
        /// </summary>
        /// <param name="stereo_rays">rays to be thrown into the grid map</param>
        private void MappingUpdate(ArrayList[] stereo_rays)
        {
            // list of currently active threads
            ArrayList activeThreads = new ArrayList();

            // create a set of threads
            Thread[] mapping_thread = new Thread[mapping_threads];
            
            for (int th = 0; th < mapping_threads; th++)
            {
                // create a state for the thread
                ThreadMappingState state = new ThreadMappingState();
                state.active = true;
                state.pose = new pos3D(x, y, z);
                state.pose.pan = pan;
                state.motion = motion[th];
                state.grid = LocalGrid[th];
                state.stereo_rays = stereo_rays;

                // add this state to the threads to be processed
                ThreadMappingUpdate mapupdate = new ThreadMappingUpdate(new WaitCallback(MappingUpdateCallback), state);
                mapping_thread[th] = new Thread(new ThreadStart(mapupdate.Execute));
                mapping_thread[th].Name = "occupancy grid map " + th.ToString();
                mapping_thread[th].Priority = ThreadPriority.AboveNormal;
                activeThreads.Add(state);
            }

            // clear benchmarks
            benchmark_observation_update = 0;
            benchmark_garbage_collection = 0;

            clock.Start();

            // start all threads
            for (int th = 0; th < mapping_threads; th++)
                mapping_thread[th].Start();

            // now sit back and wait for all threads to complete
            while (activeThreads.Count > 0)
            {
                for (int th = activeThreads.Count - 1; th >= 0; th--)
                {
                    // is this thread still active?
                    ThreadMappingState state = (ThreadMappingState)activeThreads[th];
                    if (!state.active)
                    {
                        // remove from the list of active threads
                        activeThreads.RemoveAt(th);

                        // update benchmark timings
                        benchmark_observation_update += state.benchmark_observation_update;
                        benchmark_garbage_collection += state.benchmark_garbage_collection;
                    }
                }
            }

            clock.Stop();
            long parallel_processing_time = clock.time_elapsed_mS;

            // how much time have we gained from multithreading ?
            long serial_processing_time = benchmark_observation_update + benchmark_garbage_collection;
            long average_update_time = serial_processing_time / mapping_threads;
            benchmark_concurrency = serial_processing_time - parallel_processing_time;

            // average benchmarks
            benchmark_observation_update /= mapping_threads;
            benchmark_garbage_collection /= mapping_threads;

            // compare the results and see which is the best looking map
            pos3D best_pose = null;
            float max_path_score = 0;
            for (int th = 0; th < mapping_threads; th++)
            {
                if ((motion[th].current_robot_path_score > max_path_score) ||
                    (th == 0))
                {
                    max_path_score = motion[th].current_robot_path_score;
                    best_pose = motion[th].current_robot_pose;
                    best_grid_index = th;
                }
            }

            // update the robots position and orientation
            // with the pose from the highest scoring path
            // across all maps
            if (best_pose != null)
            {
                x = best_pose.x;
                y = best_pose.y;
                z = best_pose.z;
                pan = best_pose.pan;
            }

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

		/// <summary>
		/// process a pair of images from a stereo camera
		/// </summary>
		/// <param name="stereo_camera_index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="left_image">
		/// A <see cref="Byte"/>
		/// </param>
		/// <param name="right_image">
		/// A <see cref="Byte"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		/*
        private bool loadImages(Thread th, int stereo_camera_index, 
		                        Byte[] left_image, Byte[] right_image)
        {            
            bool scanMatchesFound = false;

			loadRawImages(stereo_camera_index, left_image, right_image, 3);

            // perform scan matching for forwards or rearwards looking cameras
            float pan_angle = head.pan + head.cameraPosition[stereo_camera_index].pan;
            if ((pan_angle == 0) || (pan_angle == (float)Math.PI))
            {
                // create a scan matching object if needed
                if (head.scanmatch[stereo_camera_index] == null)
                    head.scanmatch[stereo_camera_index] = new scanMatching();

                if (EnableScanMatching)
                {
                    // perform scan matching
                    head.scanmatch[stereo_camera_index].update(
					    correspondence[stereo_camera_index].getRectifiedImage(true),
                        head.calibration[stereo_camera_index].leftcam.image_width,
                        head.calibration[stereo_camera_index].leftcam.image_height,
                        head.calibration[stereo_camera_index].leftcam.camera_FOV_degrees * (float)Math.PI / 180.0f,
                        head.cameraPosition[stereo_camera_index].roll,
                        ScanMatchingMaxPanAngleChange * (float)Math.PI / 180.0f);
                    if (head.scanmatch[stereo_camera_index].pan_angle_change != scanMatching.NOT_MATCHED)
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
                                ScanMatchingPanAngleEstimate -= head.scanmatch[stereo_camera_index].pan_angle_change;
                            else
                                // rearward facing camera
                                ScanMatchingPanAngleEstimate += head.scanmatch[stereo_camera_index].pan_angle_change;
                        }
                    }
                }
            }                 
            else head.scanmatch[stereo_camera_index] = null;
			return(scanMatchesFound);
		}		
		*/
		
        /// <summary>
        /// the stereo correspondence thread has called back
        /// </summary>
        /// <param name="state"></param>
        private static void StereoCorrespondenceCallback(object state)
        {
            // get the returned state
            ThreadStereoCorrespondenceState cstate = (ThreadStereoCorrespondenceState)state;
        }		
		
        /// <summary>
        /// load stereo images from a list
        /// </summary>
        /// <param name="images">list of images (byte arrays) in left/right order</param>
        private void loadImages(ArrayList images)
        {            
            clock.Start();
			
            bool scanMatchesFound = false;

            // create a set of threads
            Thread[] correspondence_thread = new Thread[no_of_stereo_cameras];			
			ArrayList activeThreads = new ArrayList();
			
            for (int stereo_camera_index = 0; stereo_camera_index < images.Count / 2; stereo_camera_index++)
            {
                Byte[] left_image = (Byte[])images[stereo_camera_index * 2];
                Byte[] right_image = (Byte[])images[(stereo_camera_index * 2) + 1];

				// create a state for the thread
				ThreadStereoCorrespondenceState state = new ThreadStereoCorrespondenceState();
				state.stereo_camera_index = stereo_camera_index;
				state.correspondence = correspondence[stereo_camera_index];
				state.correspondence_algorithm_type = correspondence_algorithm_type;
				state.fullres_left = left_image;
				state.fullres_right = right_image;
				state.bytes_per_pixel = 3;
				state.head = head;
				state.no_of_stereo_features = inverseSensorModel.no_of_stereo_features;
		        state.EnableScanMatching = EnableScanMatching;
		        state.ScanMatchingMaxPanAngleChange = ScanMatchingMaxPanAngleChange;
		        state.ScanMatchingPanAngleEstimate = ScanMatchingPanAngleEstimate;
		        state.pan = pan;				
				
                // add this state to the threads to be processed
                ThreadStereoCorrespondence correspondence_update = new ThreadStereoCorrespondence(new WaitCallback(StereoCorrespondenceCallback), state);
                correspondence_thread[stereo_camera_index] = new Thread(new ThreadStart(correspondence_update.Execute));
                correspondence_thread[stereo_camera_index].Name = "stereo correspondence " + stereo_camera_index.ToString();
                correspondence_thread[stereo_camera_index].Priority = ThreadPriority.AboveNormal;
                activeThreads.Add(state);				
								
				//if (loadImages(correspondence_thread[stereo_camera_index],
				//               stereo_camera_index, left_image, right_image))
				//	scanMatchesFound = true;
            }
			
            // start all stereo correspondence threads
			for (int th = 0; th < correspondence_thread.Length; th++)
                correspondence_thread[th].Start();

            // now sit back and wait for all threads to complete
            while (activeThreads.Count > 0)
            {
                for (int th = activeThreads.Count - 1; th >= 0; th--)
                {
                    // is this thread still active?
                    ThreadStereoCorrespondenceState state = (ThreadStereoCorrespondenceState)activeThreads[th];
                    if (!state.active)
                    {
						if (state.scanMatchesFound) scanMatchesFound = true;
						
                        // remove from the list of active threads
                        activeThreads.RemoveAt(th);
						
						Console.WriteLine("Correspondence thread complete");
                    }
                }
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
        /// <param name="index">index number of the occupancy grid being considered</param>
        private void checkOutOfBounds(int index)
        {
            bool out_of_bounds = false;
            pos3D new_grid_centre = new pos3D(LocalGrid[index].x, LocalGrid[index].y, LocalGrid[index].z);

            float innermost_gridSize_mm = LocalGrid[index].cellSize_mm; // .getCellSize(LocalGrid.levels - 1);
            float border = innermost_gridSize_mm / 4.0f;
            if (x < LocalGrid[index].x - border)
            {
                new_grid_centre.x = LocalGrid[index].x - border;
                out_of_bounds = true;
            }
            if (x > LocalGrid[index].x + border)
            {
                new_grid_centre.x = LocalGrid[index].x + border;
                out_of_bounds = true;
            }
            if (y < LocalGrid[index].y - border)
            {
                new_grid_centre.y = LocalGrid[index].y - border;
                out_of_bounds = true;
            }
            if (y > LocalGrid[index].y + border)
            {
                new_grid_centre.y = LocalGrid[index].y + border;
                out_of_bounds = true;
            }

            if (out_of_bounds)
            {
                // file name for this grid, based upon its position
                String grid_filename = "grid" + Convert.ToString((int)Math.Round(LocalGrid[index].x / border)) + "_" +
                                                Convert.ToString((int)Math.Round(LocalGrid[index].y / border)) + ".grd";

                // store the path which was used to create the previous grid
                // this is far more efficient in terms of memory and disk storage
                // than trying to store the entire grid, most of which will be just empty cells
                //LocalGridPath.Save(grid_filename);          

                // clear out the path data
                //LocalGridPath.Clear();

                // make a new grid
                createLocalGrid(index);

                // position the grid
                LocalGrid[index].SetCentrePosition(new_grid_centre.x, new_grid_centre.x);
                LocalGrid[index].z = new_grid_centre.z;

                // file name of the grid to be loaded
                grid_filename = "grid" + Convert.ToString((int)Math.Round(LocalGrid[index].x / border)) + "_" +
                                         Convert.ToString((int)Math.Round(LocalGrid[index].y / border)) + ".grd";
                //LocalGridPath.Load(grid_filename);
                    
                // TODO: update the local grid using the loaded path
                //LocalGrid[index].insert(LocalGridPath, false);
            }
        }

        /// <summary>
        /// returns the average colour variance for the entire occupancy grid
        /// </summary>
        public float GetMeanColourVariance()
        {
            return (GetMeanColourVariance(best_grid_index));
        }

        /// <summary>
        /// returns the average colour variance for the entire occupancy grid
        /// </summary>
        /// <param name="index">index number of the occupancy grid being considered</param>
        /// <returns>mean colour variance</returns>
        public float GetMeanColourVariance(int index)
        {
            float mean_variance = 0;

            if (motion[index].best_path != null)
                if (motion[index].best_path.current_pose != null)
                    mean_variance = LocalGrid[index].GetMeanColourVariance(motion[index].best_path.current_pose);

            return (mean_variance);
        }


        /// <summary>
        /// update the state of the robot using a list of images from its stereo camera/s
        /// </summary>
        /// <param name="images">list containing stereo images in left/right order</param>
        /// <param name="motion_model">the motion model to be used</param>
        /// <param name="grid">the ocupancy grid to be updated</param>
        private void update(ArrayList images)
        {
            if (images != null)
            {                
                // load stereo images
                loadImages(images);

                // create an observation as a set of rays from the stereo correspondence results
                ArrayList[] stereo_rays = new ArrayList[head.no_of_stereo_cameras];
                for (int cam = 0; cam < head.no_of_stereo_cameras; cam++)
                    stereo_rays[cam] = inverseSensorModel.createObservation(head, cam);

                // update the motion model and occupancy grid
                MappingUpdate(stereo_rays);
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
            motionModel motion_model = motion[best_grid_index];
            motion_model.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            if (!((previousPosition.x == -1) && (previousPosition.y == -1)))
            {
                float time_per_index_sec = 1;

                float dx = x - previousPosition.x;
                float dy = y - previousPosition.y;
                float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float acceleration = (2 * (distance - (motion_model.forward_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float forward_velocity = motion_model.forward_velocity + (acceleration * time_per_index_sec);

                for (int i = 0; i < mapping_threads; i++)
                {
                    motion[i].forward_acceleration = acceleration;
                    motion[i].forward_velocity = forward_velocity;
                }

                distance = pan - previousPosition.pan;
                acceleration = (2 * (distance - (motion_model.angular_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity = motion_model.angular_velocity + (acceleration * time_per_index_sec);

                for (int i = 0; i < mapping_threads; i++)
                    motion[i].angular_velocity = angular_velocity;

                clock.Start();

                for (int i = 0; i < mapping_threads; i++)
                    motion[i].Predict(time_per_index_sec);

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
            for (int i = 0; i < mapping_threads; i++) motion[i].Reset(mode);
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
                for (int i = 0; i < mapping_threads; i++)
                    motion[i].LeftWheelAngularVelocity = angle_traversed_radians / time_elapsed_sec;                

                // calculate angular velocity of the right wheel in radians/sec
                angle_traversed_radians = (float)(right_wheel_encoder - prev_right_wheel_encoder) * 2 * (float)Math.PI / countsPerWheelRev;
                for (int i = 0; i < mapping_threads; i++)
                    motion[i].RightWheelAngularVelocity = angle_traversed_radians / time_elapsed_sec;
            }

            clock.Start();

            // update the motion model
            for (int i = 0; i < mapping_threads; i++)
            {
                motion[i].InputType = motionModel.INPUTTYPE_WHEEL_ANGULAR_VELOCITY;
                motion[i].Predict(time_elapsed_sec);
            }

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
            for (int i = 0; i < mapping_threads; i++)
            {
                motion[i].InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
                motion[i].forward_acceleration = forward_velocity - motion[i].forward_velocity;
                motion[i].forward_velocity = forward_velocity;
                motion[i].angular_acceleration = angular_velocity - motion[i].angular_velocity;
                motion[i].angular_velocity = angular_velocity;
            }

            clock.Start();

            for (int i = 0; i < mapping_threads; i++)
                motion[i].Predict(time_elapsed_sec);

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

                // the best performing local grid
                occupancygridMultiHypothesis grid = LocalGrid[best_grid_index];

                // create a planner
                createPlanner(grid);

                // set variables, in the unlikely case that the centre position
                // of the grid has been changed since the planner was initialised
                planner.init(grid.navigable_space, grid.x, grid.y);

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
        /// <param name="img">image within which to show the grid</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        public void ShowGrid(int view_type, Byte[] img, int width, int height, bool show_robot, 
                             bool colour, bool scalegrid)
        {
            occupancygridMultiHypothesis grid = LocalGrid[best_grid_index];
            motionModel motion_model = motion[best_grid_index];

            if (motion_model.best_path != null)
                if (motion_model.best_path.current_pose != null)
                {
                    grid.Show(view_type, img, width, height, motion_model.best_path.current_pose, colour, scalegrid);
                    if (show_robot)
                    {
                        if (view_type == occupancygridMultiHypothesis.VIEW_ABOVE)
                        {
                            int half_grid_dimension_mm = (grid.dimension_cells * grid.cellSize_mm) / 2;
                            int min_x = (int)(grid.x - half_grid_dimension_mm);
                            int min_y = (int)(grid.y - half_grid_dimension_mm);
                            int max_x = (int)(grid.x + half_grid_dimension_mm);
                            int max_y = (int)(grid.y + half_grid_dimension_mm);
                            motion_model.ShowBestPose(img, width, height, min_x, min_y, max_x, max_y, false, false);
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
            motion[best_grid_index].ShowTree(img, width, height, 0, 0, 0, 0);
        }

        #endregion

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeRobot = doc.CreateElement("Robot");
            parent.AppendChild(nodeRobot);

            xml.AddComment(doc, nodeRobot, "Name");
            xml.AddTextElement(doc, nodeRobot, "Name", Name);

            xml.AddComment(doc, nodeRobot, "Total mass in kilograms");
            xml.AddTextElement(doc, nodeRobot, "TotalMassKilograms", Convert.ToString(TotalMass_kg));

            XmlElement nodeComputation = doc.CreateElement("ComputingEnvironment");
            nodeRobot.AppendChild(nodeComputation);
            xml.AddComment(doc, nodeComputation, "The number of threads to run concurrently");
            xml.AddTextElement(doc, nodeComputation, "NoOfThreads", Convert.ToString(mapping_threads));

            XmlElement nodeBody = doc.CreateElement("BodyDimensions");
            nodeRobot.AppendChild(nodeBody);

            xml.AddComment(doc, nodeBody, "Shape of the body");
            xml.AddComment(doc, nodeBody, "0 - square");
            xml.AddComment(doc, nodeBody, "1 - round");
            xml.AddTextElement(doc, nodeBody, "BodyShape", Convert.ToString(BodyShape));

            xml.AddComment(doc, nodeBody, "Width of the body in millimetres");
            xml.AddTextElement(doc, nodeBody, "BodyWidthMillimetres", Convert.ToString(BodyWidth_mm));

            xml.AddComment(doc, nodeBody, "Length of the body in millimetres");
            xml.AddTextElement(doc, nodeBody, "BodyLengthMillimetres", Convert.ToString(BodyLength_mm));

            xml.AddComment(doc, nodeBody, "Height of the body in millimetres");
            xml.AddTextElement(doc, nodeBody, "BodyHeightMillimetres", Convert.ToString(BodyHeight_mm));

            XmlElement nodePropulsion = doc.CreateElement("PropulsionSystem");
            nodeRobot.AppendChild(nodePropulsion);

            xml.AddComment(doc, nodePropulsion, "Propulsion type");
            xml.AddTextElement(doc, nodePropulsion, "PropulsionType", Convert.ToString(propulsionType));

            xml.AddComment(doc, nodePropulsion, "Distance between the wheels in millimetres");
            xml.AddTextElement(doc, nodePropulsion, "WheelBaseMillimetres", Convert.ToString(WheelBase_mm));

            xml.AddComment(doc, nodePropulsion, "How far from the front of the robot is the wheel base in millimetres");
            xml.AddTextElement(doc, nodePropulsion, "WheelBaseForwardMillimetres", Convert.ToString(WheelBaseForward_mm));

            xml.AddComment(doc, nodePropulsion, "Wheel diameter in millimetres");
            xml.AddTextElement(doc, nodePropulsion, "WheelDiameterMillimetres", Convert.ToString(WheelDiameter_mm));

            xml.AddComment(doc, nodePropulsion, "Wheel Position feedback type");
            xml.AddTextElement(doc, nodePropulsion, "WheelPositionFeedbackType", Convert.ToString(WheelPositionFeedbackType));

            xml.AddComment(doc, nodePropulsion, "Motor gear ratio");
            xml.AddTextElement(doc, nodePropulsion, "GearRatio", Convert.ToString(GearRatio));

            xml.AddComment(doc, nodePropulsion, "Encoder counts per revolution");
            xml.AddTextElement(doc, nodePropulsion, "CountsPerRev", Convert.ToString(CountsPerRev));
            
            xml.AddComment(doc, nodePropulsion, "Motor no load speed in RPM");
            xml.AddTextElement(doc, nodePropulsion, "MotorNoLoadSpeedRPM", Convert.ToString(MotorNoLoadSpeedRPM));

            xml.AddComment(doc, nodePropulsion, "Motor torque rating in Kg/mm");
            xml.AddTextElement(doc, nodePropulsion, "MotorTorqueKgMm", Convert.ToString(MotorTorqueKgMm));            

            XmlElement nodeSensorPlatform = doc.CreateElement("SensorPlatform");
            nodeRobot.AppendChild(nodeSensorPlatform);

            xml.AddComment(doc, nodeSensorPlatform, "Number of stereo cameras");
            xml.AddTextElement(doc, nodeSensorPlatform, "NoOfStereoCameras", Convert.ToString(head.no_of_stereo_cameras));

            xml.AddComment(doc, nodeSensorPlatform, "The type of head");
            xml.AddTextElement(doc, nodeSensorPlatform, "HeadType", Convert.ToString(HeadType));

            xml.AddComment(doc, nodeSensorPlatform, "Size of the head in millimetres");
            xml.AddTextElement(doc, nodeSensorPlatform, "HeadSizeMillimetres", Convert.ToString(HeadSize_mm));

            xml.AddComment(doc, nodeSensorPlatform, "Shape of the head");
            xml.AddTextElement(doc, nodeSensorPlatform, "HeadShape", Convert.ToString(HeadShape));

            xml.AddComment(doc, nodeSensorPlatform, "Offset of the head from the leftmost side of the robot in millimetres");
            xml.AddTextElement(doc, nodeSensorPlatform, "HeadPositionLeftMillimetres", Convert.ToString(head.x));

            xml.AddComment(doc, nodeSensorPlatform, "Offset of the head from the front of the robot in millimetres");
            xml.AddTextElement(doc, nodeSensorPlatform, "HeadPositionForwardMillimetres", Convert.ToString(head.y));

            xml.AddComment(doc, nodeSensorPlatform, "Height of the head above the ground in millimetres");
            xml.AddTextElement(doc, nodeSensorPlatform, "HeadHeightMillimetres", Convert.ToString(head.z));

            xml.AddComment(doc, nodeSensorPlatform, "Orientation of the cameras");
            xml.AddTextElement(doc, nodeSensorPlatform, "CameraOrientation", Convert.ToString(CameraOrientation));

            for (int i = 0; i < head.no_of_stereo_cameras; i++)
            {
                nodeSensorPlatform.AppendChild(head.calibration[i].getXml(doc, nodeSensorPlatform, 2));

                if (head.sensormodel[i] != null)
                    nodeSensorPlatform.AppendChild(head.sensormodel[i].getXml(doc, nodeRobot));
            }

            XmlElement nodeOccupancyGrid = doc.CreateElement("OccupancyGrid");
            nodeRobot.AppendChild(nodeOccupancyGrid);

            xml.AddComment(doc, nodeOccupancyGrid, "The number of scales used within the local grid");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridLevels", Convert.ToString(LocalGridLevels));

            xml.AddComment(doc, nodeOccupancyGrid, "Whether to use scan matching");
            xml.AddTextElement(doc, nodeOccupancyGrid, "EnableScanMatching", Convert.ToString(EnableScanMatching));

            xml.AddComment(doc, nodeOccupancyGrid, "Dimension of the local grid in the XY plane in cells");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridDimension", Convert.ToString(LocalGridDimension));

            xml.AddComment(doc, nodeOccupancyGrid, "Dimension of the local grid in the vertical (Z) plane in cells");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridDimensionVertical", Convert.ToString(LocalGridDimensionVertical));

            xml.AddComment(doc, nodeOccupancyGrid, "Size of each grid cell (voxel) in millimetres");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridCellSizeMillimetres", Convert.ToString(LocalGridCellSize_mm));

            xml.AddComment(doc, nodeOccupancyGrid, "The distance which the robot must travel before new data is inserted into the grid during mapping");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridIntervalMillimetres", Convert.ToString(LocalGridInterval_mm));

            xml.AddComment(doc, nodeOccupancyGrid, "An extra radius applied when localising within the grid, to make localisation rays wider");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridLocalisationRadiusMillimetres", Convert.ToString(LocalGridLocalisationRadius_mm));

            xml.AddComment(doc, nodeOccupancyGrid, "When updating the grid map this is the maximum range within which cells will be updated");
            xml.AddComment(doc, nodeOccupancyGrid, "This prevents the system from being slowed down by the insertion of a lot of very long range rays");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridMappingRangeMillimetres", Convert.ToString(LocalGridMappingRange_mm));

            xml.AddComment(doc, nodeOccupancyGrid, "A weighting factor which determines how aggressively the vacancy part of the sensor model carves out space");
            xml.AddTextElement(doc, nodeOccupancyGrid, "LocalGridVacancyWeighting", Convert.ToString(LocalGridVacancyWeighting));

            if (TuningParameters != "")
            {
                xml.AddComment(doc, nodeOccupancyGrid, "Parameters discovered by auto tuning");
                xml.AddTextElement(doc, nodeOccupancyGrid, "TuningParameters", TuningParameters);

                if (MinimumColourVariance != float.MaxValue)
                {
                    xml.AddComment(doc, nodeOccupancyGrid, "Minimum colour variance discovered by auto tuning");
                    xml.AddTextElement(doc, nodeOccupancyGrid, "MinimumColourVariance", Convert.ToString(MinimumColourVariance));
                }

                if (MinimumPositionError_mm != float.MaxValue)
                {
                    xml.AddComment(doc, nodeOccupancyGrid, "Minimum position error found during auto tuning");
                    xml.AddTextElement(doc, nodeOccupancyGrid, "MinimumPositionErrorMillimetres", Convert.ToString(MinimumPositionError_mm));
                }
            }

            nodeRobot.AppendChild(motion[best_grid_index].getXml(doc, nodeRobot));

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

                for (int i = 0; i < mapping_threads; i++)
                    createLocalGrid(i);

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

            if (xnod.Name == "NoOfThreads")
            {
                mapping_threads = Convert.ToInt32(xnod.InnerText);
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
                init(no_of_cameras, rays_per_stereo_camera);
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
                for (int i = 0; i < mapping_threads; i++)
                    motion[i].LoadFromXml(xnod, level + 1);
            }

            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) && 
                ((xnod.Name == "Robot") || 
                 (xnod.Name == "Sentience") ||
                 (xnod.Name == "BodyDimensions") ||
                 (xnod.Name == "PropulsionSystem") ||
                 (xnod.Name == "SensorPlatform") ||
                 (xnod.Name == "OccupancyGrid") ||
                 (xnod.Name == "ComputingEnvironment")
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
        /// <param name="filename">name of the file to save as</param>
        public void SaveGrid(String filename)
        {
            LocalGrid[best_grid_index].Save(filename, motion[best_grid_index].best_path.current_pose);
        }

        /// <summary>
        /// returns the grid data as a byte array suitable for
        /// subsequent zip compression
        /// </summary>
        /// <returns>occupancy grid data</returns>
        public Byte[] SaveGrid()
        {
            return (LocalGrid[best_grid_index].Save(motion[best_grid_index].best_path.current_pose));
        }

        public void LoadGrid(Byte[] data)
        {
            createLocalGrid(best_grid_index);
            LocalGrid[best_grid_index].Load(data);
        }

        /// <summary>
        /// load the occupancy grid from file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadGrid(String filename)
        {
            createLocalGrid(best_grid_index);
            LocalGrid[best_grid_index].Load(filename);
        }

        #endregion

        #region "exporting data to third part visualisation tools"

        /// <summary>
        /// export occupancy grid data so that it can be visualised within IFrIT
        /// </summary>
        /// <param name="filename">name of the file to export as</param>
        public void ExportToIFrIT(String filename)
        {
            LocalGrid[best_grid_index].ExportToIFrIT(filename, motion[best_grid_index].best_path.current_pose);
        }


        #endregion
    }
}
