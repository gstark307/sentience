/*
    Sentience 3D Perception System
    Copyright (C) 2000-2009 Bob Mottram
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
using System.Collections.Generic;
using System.Threading;

namespace dpslam.core
{
    /// <summary>
    /// object containing the state of a robot
    /// </summary>
    public class robot : pos3D
    {
        // name of the robot
        public string Name = "My Robot";  
        
        // the number of rays per stereo camera to be thrown at each time step
        private const int rays_per_stereo_camera = 50;
		
		// whether to save sensor model values as integers
		public bool integer_sensor_model_values;		

        #region "benchmark timings"

        // object used for taking benchmark timings
        private stopwatch clock = new stopwatch();

        // timing results in milliseconds
        public long benchmark_stereo_correspondence;
        public long benchmark_observation_update;
        public long benchmark_garbage_collection;
        public long benchmark_prediction;
        public long benchmark_concurrency;
        
        public const int MAPPING_DPSLAM = 0;
        public const int MAPPING_SIMPLE = 1;
        public int mapping_type;

        #endregion

        #region "getting the best results"

        /// <summary>
        /// returns the best available motion model
        /// </summary>
        /// <returns></returns>
        public motionModel GetMotionModel()
        {
            return(motion);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public dpslam GetGrid()
        {
            return (LocalGrid);
        }

        #endregion

        #region "various models"

        #region "motion model"

        // describes how the robot moves, used to predict the next step as a probabilistic distribution
        // of possible poses
        public motionModel motion;

        #endregion

        #region "inverse sensor model"

        // object used to construct rays and sensor models
        public stereoModel inverseSensorModel;

        #endregion

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
        public float WheelDiameter_mm = 30;       // diameter of the wheel
        public float WheelBase_mm = 90;           // wheel base length
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

        public dpslam LocalGrid;       // grid containing the current local observations

        // parameters discovered by auto tuning
        public string TuningParameters = "";

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

        #region "initialisation functions"

        /// <summary>
        /// recreate the local grid
        /// </summary>
        private void createLocalGrid()
        {
            // create the local grid
            LocalGrid = new dpslam(LocalGridDimension, LocalGridDimensionVertical, (int)LocalGridCellSize_mm, (int)LocalGridLocalisationRadius_mm, (int)LocalGridMappingRange_mm, LocalGridVacancyWeighting);
		}

        /// <summary>
        /// initialise with the given number of stereo cameras
        /// </summary>
        /// <param name="no_of_stereo_cameras">the number of stereo cameras on the robot (not the total number of cameras)</param>
        /// <param name="rays_per_stereo_camera">the number of rays which will be thrown from each stereo camera per time step</param>
        private void init(
		    int no_of_stereo_cameras, 
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

            // add local occupancy grid
            createLocalGrid();
	
            // create a motion model for each possible grid
            motion = new motionModel(this, LocalGrid, 100);
            
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

        #region "parameter setting"

        #region "setting motion model parameters"

        /// <summary>
        /// set the number of trial poses within the motion model
        /// </summary>
        /// <param name="no_of_poses"></param>
        public void SetMotionModelTrialPoses(
		    int no_of_poses)
        {
            motion.survey_trial_poses = no_of_poses;
        }

        /// <summary>
        /// set the culling threshold for the motion model
        /// </summary>
        /// <param name="culling_threshold"></param>
        public void SetMotionModelCullingThreshold(
		    int culling_threshold)
        {
            motion.cull_threshold = culling_threshold;
        }

        #endregion

        #region "setting grid parameters"

        /// <summary>
        /// sets the position of the local grid
        /// </summary>
        /// <param name="x">x position in millimetres</param>
        /// <param name="y">y position in millimetres</param>
        /// <param name="z">z position in millimetres</param>
        public void SetLocalGridPosition(
		    float x, 
		    float y, 
		    float z)
        {
            LocalGrid.x = x;
            LocalGrid.y = y;
            LocalGrid.z = z;
        }

        #endregion

        #region "setting optimisation parameters"

        /// <summary>
        /// set the tuning parameters from a comma separated string
        /// </summary>
        /// <param name="parameters">comma separated tuning parameters</param>
        public void SetTuningParameters(
		    string parameters)
        {
            this.TuningParameters = parameters;

            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            string[] tuningParams = parameters.Split(',');
            float[] tuningParameters = new float[tuningParams.Length];
            for (int i = 0; i < tuningParams.Length; i++)
                tuningParameters[i] = Convert.ToSingle(tuningParams[i], format);

            // Motion model culling threshold
            motion.cull_threshold = (int)tuningParameters[0];
            // Localisation radius
            LocalGridLocalisationRadius_mm = tuningParameters[1];
            // Number of position uncertainty particles
            motion.survey_trial_poses = (int)tuningParameters[2];
            // A weighting factor which determines how aggressively the vacancy part of the sensor model carves out space
            LocalGridVacancyWeighting = tuningParameters[3];
            LocalGrid.vacancy_weighting = tuningParameters[3];
        }

        #endregion

        /// <summary>
        /// set parameters required for mapping
        /// </summary>
        /// <param name="sigma"></param>
        public void setMappingParameters(
		    float sigma)
        {
            inverseSensorModel.sigma = sigma;
        }

        #endregion

        #region "mapping update"

        /// <summary>
        /// mapping update, which can consist of multiple threads running concurrently
        /// </summary>
        /// <param name="stereo_rays">rays to be thrown into the grid map</param>
        private void MappingUpdate(
		    List<evidenceRay>[] stereo_rays)
        {
		    pos3D pose = new pos3D(x, y, z);

            // object used for taking benchmark timings
            stopwatch clock = new stopwatch();

            clock.Start();

            // update all current poses with the observed rays
            motion.LocalGrid = LocalGrid;
            motion.AddObservation(stereo_rays, false);

            clock.Stop();
            benchmark_observation_update = clock.time_elapsed_mS;

            // what's the relative position of the robot inside the grid ?
            pos3D relative_position = new pos3D(pose.x - LocalGrid.x, pose.y - LocalGrid.y, 0);
            relative_position.pan = pose.pan - LocalGrid.pan;

            clock.Start();

            // garbage collect dead occupancy hypotheses
            LocalGrid.GarbageCollect();

            clock.Stop();
            benchmark_garbage_collection = clock.time_elapsed_mS;
				
            // update the robots position and orientation
            // with the pose from the highest scoring path
			if (motion.current_robot_pose != null)
			{
	            x = motion.current_robot_pose.x;
	            y = motion.current_robot_pose.y;
	            z = motion.current_robot_pose.z;
	            pan = motion.current_robot_pose.pan;
				//Console.WriteLine("Position " + x.ToString() + " " + y.ToString());
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
        /// returns the average colour variance for the entire occupancy grid
        /// </summary>
        /// <param name="index">index number of the occupancy grid being considered</param>
        /// <returns>mean colour variance</returns>
        public float GetMeanColourVariance()
        {
            float mean_variance = 0;

            if (motion.best_path != null)
                if (motion.best_path.current_pose != null)
                    mean_variance = LocalGrid.GetMeanColourVariance(motion.best_path.current_pose);

            return (mean_variance);
        }

		/// <summary>
		/// transform the given stereo matches into a set of rays
		/// </summary>
        /// <param name="stereo_matches">list of stereo matches (prob/x/y/disp/r/g/b)</param>		
        /// <returns></returns>
        public List<evidenceRay>[] GetRays(
		    List<ushort[]> stereo_matches)
        {
            if (stereo_matches != null)
            {                
                loadStereoMatches(stereo_matches);

                // create an observation as a set of rays from the stereo correspondence results
                List<evidenceRay>[] stereo_rays = new List<evidenceRay>[head.no_of_stereo_cameras];
                for (int cam = 0; cam < head.no_of_stereo_cameras; cam++)
                    stereo_rays[cam] = inverseSensorModel.createObservation(head, cam);
                    
                return(stereo_rays);
            }
            else return(null);
        }
		
		private void loadStereoMatches(
		    List<ushort[]> stereo_matches)
		{
		}

        private void updateSimulation(
		    dpslam sim_map)
        {
			// create simulated observation based upon the given map
            List<evidenceRay>[] stereo_rays = new List<evidenceRay>[head.no_of_stereo_cameras];
            for (int cam = 0; cam < head.no_of_stereo_cameras; cam++)
			    stereo_rays[cam] = sim_map.createSimulatedObservation(this, cam);
            
			// update the motion model and occupancy grid using the observations
            MappingUpdate(stereo_rays);			
		}
		
        /// <summary>
        /// update the state of the robot using a list of images from its stereo camera/s
        /// </summary>
        /// <param name="stereo_matches">list of stereo matches (prob/x/y/disp/r/g/b)</param>
        private void update(
		    List<ushort[]> stereo_matches)
        {
            if (stereo_matches != null)
            {                
                // load stereo matches
                loadStereoMatches(stereo_matches);

                // create an observation as a set of rays from the stereo correspondence results
                List<evidenceRay>[] stereo_rays = new List<evidenceRay>[head.no_of_stereo_cameras];
                for (int cam = 0; cam < head.no_of_stereo_cameras; cam++)
                    stereo_rays[cam] = inverseSensorModel.createObservation(head, cam);

                // update the motion model and occupancy grid
                MappingUpdate(stereo_rays);
            }
        }

        /// <summary>
        /// update from a known position and orientation
        /// </summary>
        /// <param name="stereo_matches">list of stereo matches (prob/x/y/disp/r/g/b)</param>
        /// <param name="x">x position in mm</param>
        /// <param name="y">y position in mm</param>
        /// <param name="pan">pan angle in radians</param>
        /// <param name="tilt">tilt angle in radians</param>
        /// <param name="roll">roll angle in radians</param>
        public void updateFromKnownPosition(
		    List<ushort[]> stereo_matches,
            float x, 
		    float y, 
		    float z,
		    float pan, 
		    float tilt, 
		    float roll)
        {
            // update the grid
            update(stereo_matches);

            // set the robot at the known position
            this.x = x;
            this.y = y;
			this.z = z;
            this.pan = pan;
			this.tilt = tilt;
			this.roll = roll;

            // update the motion model
            motionModel motion_model = motion;
            motion_model.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            if (!((previousPosition.x == -1) && 
			      (previousPosition.y == -1)))
            {
                float time_per_index_sec = 1;

                float dx = x - previousPosition.x;
                float dy = y - previousPosition.y;
				float dz = z - previousPosition.z;
                float distance = (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                float acceleration = (2 * (distance - (motion_model.forward_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float forward_velocity = motion_model.forward_velocity + (acceleration * time_per_index_sec);

                motion.forward_acceleration = acceleration;
                motion.forward_velocity = forward_velocity;

                distance = pan - previousPosition.pan;
                acceleration = (2 * (distance - (motion_model.angular_velocity_pan * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity_pan = motion_model.angular_velocity_pan + (acceleration * time_per_index_sec);
                motion.angular_velocity_pan = angular_velocity_pan;

                distance = tilt - previousPosition.tilt;
                acceleration = (2 * (distance - (motion_model.angular_velocity_tilt * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity_tilt = motion_model.angular_velocity_tilt + (acceleration * time_per_index_sec);
                motion.angular_velocity_tilt = angular_velocity_tilt;

				distance = roll - previousPosition.roll;
				acceleration = (2 * (distance - (motion_model.angular_velocity_roll * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity_roll = motion_model.angular_velocity_roll + (acceleration * time_per_index_sec);
                motion.angular_velocity_roll = angular_velocity_roll;
				
                clock.Start();

                motion.Predict(time_per_index_sec);

                clock.Stop();
                benchmark_prediction = clock.time_elapsed_mS;
            }

            storePreviousPosition();
        }

        /// <summary>
        /// update from a known position and orientation within a simulated environment
        /// </summary>
        /// <param name="sim_map">simulated environment</param>
        /// <param name="x">x position in mm</param>
        /// <param name="y">y position in mm</param>
        /// <param name="z">z position in mm</param>
        /// <param name="pan">pan angle in radians</param>
        /// <param name="tilt">tilt angle in radians</param>
        /// <param name="roll">roll angle in radians</param>
        /// <param name="mapping">mapping or localisation</param>
        public void updateFromKnownPosition(
		    dpslam sim_map,
            float x, 
		    float y, 
		    float z,
		    float pan, 
		    float tilt, 
		    float roll)
        {
            // update the grid
            updateSimulation(sim_map);

            // set the robot at the known position
            this.x = x;
            this.y = y;
			this.z = z;
            this.pan = pan;
			this.tilt = tilt;
			this.roll = roll;

            // update the motion model
            motionModel motion_model = motion;
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

                motion.forward_acceleration = acceleration;
                motion.forward_velocity = forward_velocity;

                distance = pan - previousPosition.pan;
                acceleration = (2 * (distance - (motion_model.angular_velocity_pan * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity_pan = motion_model.angular_velocity_pan + (acceleration * time_per_index_sec);
                motion.angular_velocity_pan = angular_velocity_pan;

                distance = tilt - previousPosition.tilt;
                acceleration = (2 * (distance - (motion_model.angular_velocity_tilt * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity_tilt = motion_model.angular_velocity_tilt + (acceleration * time_per_index_sec);
                motion.angular_velocity_tilt = angular_velocity_tilt;

				distance = roll - previousPosition.roll;
                acceleration = (2 * (distance - (motion_model.angular_velocity_roll * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                float angular_velocity_roll = motion_model.angular_velocity_roll + (acceleration * time_per_index_sec);
                motion.angular_velocity_roll = angular_velocity_roll;
				
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
        public void Reset(
		    int mode)
        {
            previousPosition.x = -1;
            previousPosition.y = -1;
            motion.Reset(mode);
        }

        /// <summary>
        /// update using wheel encoder positions
        /// </summary>
        /// <param name="stereo_matches"></param>
        /// <param name="left_wheel_encoder">left wheel encoder position</param>
        /// <param name="right_wheel_encoder">right wheel encoder position</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in sec</param>
        /// <param name="mapping">mapping or localisation</param>
        public void updateFromEncoderPositions(
		    List<ushort[]> stereo_matches,
            long left_wheel_encoder, 
		    long right_wheel_encoder,
            float time_elapsed_sec)
        {
            // update the grid
            update(stereo_matches);

            //float wheel_circumference_mm = (float)Math.PI * WheelDiameter_mm;
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
        /// update using wheel encoder positions within a simulated environment
        /// </summary>
        /// <param name="sim_map">simulated environment</param>
        /// <param name="left_wheel_encoder">left wheel encoder position</param>
        /// <param name="right_wheel_encoder">right wheel encoder position</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in sec</param>
        /// <param name="mapping">mapping or localisation</param>
        public void updateFromEncoderPositions(
		    dpslam sim_map,
            long left_wheel_encoder, 
		    long right_wheel_encoder,
            float time_elapsed_sec)
        {
            // update the grid
            updateSimulation(sim_map);

            //float wheel_circumference_mm = (float)Math.PI * WheelDiameter_mm;
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
        /// <param name="stereo_matches">list of stereo matches (prob/x/y/disp/r/g/b)</param>
        /// <param name="forward_velocity">forward velocity in mm/sec</param>
        /// <param name="angular_velocity_pan">angular velocity in the pan axis in radians/sec</param>
        /// <param name="angular_velocity_tilt">angular velocity in the tilt axis in radians/sec</param>
        /// <param name="angular_velocity_roll">angular velocity in the roll axis in radians/sec</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in sec</param>
        public void updateFromVelocities(
		    List<ushort[]> stereo_matches, 
            float forward_velocity, 
		    float angular_velocity_pan,
		    float angular_velocity_tilt,
		    float angular_velocity_roll,
            float time_elapsed_sec)
        {
            // update the grid
            update(stereo_matches);

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            motion.forward_acceleration = forward_velocity - motion.forward_velocity;
            motion.forward_velocity = forward_velocity;
            motion.angular_acceleration_pan = angular_velocity_pan - motion.angular_velocity_pan;
            motion.angular_velocity_pan = angular_velocity_pan;
            //motion.angular_acceleration_tilt = angular_velocity_tilt - motion.angular_velocity_tilt;
            //motion.angular_velocity_tilt = angular_velocity_tilt;
            //motion.angular_acceleration_roll = angular_velocity_roll - motion.angular_velocity_roll;
            //motion.angular_velocity_roll = angular_velocity_roll;

            clock.Start();

            motion.Predict(time_elapsed_sec);

            clock.Stop();
            benchmark_prediction = clock.time_elapsed_mS;
			
		    float deviation = 0;
		    float deviation_forward = 0;
		    float deviation_perp = 0;
		    float deviation_vertical = 0;
            motion.AveragePose(
		        ref x, 
		        ref y, 
		        ref z,
		        ref pan,
		        ref tilt,
		        ref roll,
		        ref deviation,
		        ref deviation_forward,
		        ref deviation_perp,
		        ref deviation_vertical);

            storePreviousPosition();
        }

        /// <summary>
        /// update using known velocities within a simulated map
        /// </summary>
        /// <param name="sim_map">simulated environment</param>
        /// <param name="forward_velocity">forward velocity in mm/sec</param>
        /// <param name="angular_velocity_pan">angular velocity in the pan axis in radians/sec</param>
        /// <param name="angular_velocity_tilt">angular velocity in the tilt axis in radians/sec</param>
        /// <param name="angular_velocity_roll">angular velocity in the roll axis in radians/sec</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in sec</param>
        public void updateFromVelocities(
		    dpslam sim_map, 
            float forward_velocity, 
		    float angular_velocity_pan,
		    float angular_velocity_tilt,
		    float angular_velocity_roll,
            float time_elapsed_sec)
        {
            // update the grid
            updateSimulation(sim_map);

            // update the motion model
            motion.InputType = motionModel.INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;
            motion.forward_acceleration = forward_velocity - motion.forward_velocity;
            motion.forward_velocity = forward_velocity;
            motion.angular_acceleration_pan = angular_velocity_pan - motion.angular_velocity_pan;
            motion.angular_velocity_pan = angular_velocity_pan;
            //motion.angular_acceleration_tilt = angular_velocity_tilt - motion.angular_velocity_tilt;
            //motion.angular_velocity_tilt = angular_velocity_tilt;
            //motion.angular_acceleration_roll = angular_velocity_roll - motion.angular_velocity_roll;
            //motion.angular_velocity_roll = angular_velocity_roll;

            clock.Start();

            motion.Predict(time_elapsed_sec);
			
		    float deviation = 0;
		    float deviation_forward = 0;
		    float deviation_perp = 0;
		    float deviation_vertical = 0;
            motion.AveragePose(
		        ref x, 
		        ref y, 
		        ref z,
		        ref pan,
		        ref tilt,
		        ref roll,
		        ref deviation,
		        ref deviation_forward,
		        ref deviation_perp,
		        ref deviation_vertical);
			
            clock.Stop();
            benchmark_prediction = clock.time_elapsed_mS;

            storePreviousPosition();
        }
				
        #endregion

        #region "display functions"

        /// <summary>
        /// show the occupancy grid
        /// </summary>
        /// <param name="img">image within which to show the grid</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        public void ShowGrid(
		    int view_type, 
		    byte[] img, 
		    int width, 
		    int height, 
		    bool show_robot, 
            bool colour, 
		    bool scalegrid)
        {
            dpslam grid = LocalGrid;
            motionModel motion_model = motion;

            if (motion_model.best_path != null)
                if (motion_model.best_path.current_pose != null)
                {
                    grid.Show(view_type, img, width, height, motion_model.best_path.current_pose, colour, scalegrid);
                    if (show_robot)
                    {
                        if (view_type == dpslam.VIEW_ABOVE)
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
        public void ShowPathTree(
		    byte[] img, 
		    int width, 
		    int height)
        {
            motion.ShowTree(img, width, height, 0, 0, 0, 0);
        }

        #endregion

        #region "saving and loading"
		
        public XmlElement getXml(
		    XmlDocument doc, 
		    XmlElement parent)
        {
            XmlElement nodeRobot = doc.CreateElement("Robot");
            parent.AppendChild(nodeRobot);

            xml.AddComment(doc, nodeRobot, "Name");
            xml.AddTextElement(doc, nodeRobot, "Name", Name);

            xml.AddComment(doc, nodeRobot, "Total mass in kilograms");
            xml.AddTextElement(doc, nodeRobot, "TotalMassKilograms", Convert.ToString(TotalMass_kg));

            XmlElement nodeComputation = doc.CreateElement("ComputingEnvironment");
            nodeRobot.AppendChild(nodeComputation);

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

			/*
            for (int i = 0; i < head.no_of_stereo_cameras; i++)
            {
                nodeSensorPlatform.AppendChild(head.calibration[i].getXml(doc, nodeSensorPlatform, 2));

                if (head.sensormodel[i] != null)
				{
					if (!integer_sensor_model_values)
                        nodeSensorPlatform.AppendChild(head.sensormodel[i].getXml(doc, nodeRobot));
					else
						nodeSensorPlatform.AppendChild(head.sensormodel[i].getXmlInteger(doc, nodeRobot));
				}
            }
            */

            XmlElement nodeOccupancyGrid = doc.CreateElement("OccupancyGrid");
            nodeRobot.AppendChild(nodeOccupancyGrid);

            xml.AddComment(doc, nodeOccupancyGrid, "The type of grid mapping");
            xml.AddTextElement(doc, nodeOccupancyGrid, "MappingType", Convert.ToString(mapping_type));

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
        public void Save(
		    string filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }


        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        public bool Load(
		    string filename)
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
        public void LoadFromXml(
		    XmlNode xnod, 
		    int level,
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

            if (xnod.Name == "MappingType")
            {
                mapping_type = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "NoOfStereoCameras")
            {
                int no_of_cameras = Convert.ToInt32(xnod.InnerText);
                init(no_of_cameras, rays_per_stereo_camera);
            }

            if (xnod.Name == "EnableScanMatching")
            {
                string str = xnod.InnerText.ToUpper();
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
                //int camIndex = 0;
                //head.calibration[cameraIndex].LoadFromXml(xnod, level + 1, ref camIndex);

                // set the position of the camera relative to the robots head
                head.cameraPosition[cameraIndex].copyFrom(head.cameraPositionHeadRelative[cameraIndex]);

                //initSensorModel(inverseSensorModel, head.image_width, head.image_height, (int)head.calibration[cameraIndex].leftcam.camera_FOV_degrees, head.baseline_mm);
                cameraIndex++;
            }

            if (xnod.Name == "InverseSensorModels")
            {
                List<string> rayModelsData = new List<string>();
                head.sensormodel[cameraIndex - 1] = new rayModelLookup(1, 1);
                head.sensormodel[cameraIndex-1].LoadFromXml(xnod, level + 1, rayModelsData);
                head.sensormodel[cameraIndex-1].LoadSensorModelData(rayModelsData, head.sensormodel[cameraIndex-1].ray_model_interval_pixels);
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
        public void SaveGrid(
		    string filename)
        {
            LocalGrid.Save(filename, motion.best_path.current_pose);
        }

		public void SaveGridImage(
		    string filename)
        {
            LocalGrid.Show(filename, 640, 640, motion.best_path.current_pose);
        }

        /// <summary>
        /// returns the grid data as a byte array suitable for
        /// subsequent zip compression
        /// </summary>
        /// <returns>occupancy grid data</returns>
        public byte[] SaveGrid()
        {
            return (LocalGrid.Save(motion.best_path.current_pose));
        }

        public void LoadGrid(
		    byte[] data)
        {
            createLocalGrid();
            LocalGrid.Load(data);
        }

        /// <summary>
        /// load the occupancy grid from file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadGrid(
		    string filename)
        {
            createLocalGrid();
            LocalGrid.Load(filename);
        }

        #endregion

        #region "exporting data to third part visualisation tools"

        /// <summary>
        /// export occupancy grid data so that it can be visualised within IFrIT
        /// </summary>
        /// <param name="filename">name of the file to export as</param>
        public void ExportToIFrIT(
		    string filename)
        {
            LocalGrid.ExportToIFrIT(filename, motion.best_path.current_pose);
        }


        #endregion		
    }
}
