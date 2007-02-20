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

        // odometry
        public long leftWheelCounts, rightWheelCounts;

        public robot(int no_of_stereo_cameras, int no_of_stereo_features)
            : base(0, 0, 0)
        {
            this.no_of_stereo_cameras = no_of_stereo_cameras;
            this.no_of_stereo_features = no_of_stereo_features;

            correspondence = new stereoCorrespondence(no_of_stereo_features);
            head = new stereoHead(no_of_stereo_cameras);
            sensorModel = new stereoModel();

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
    }
}
