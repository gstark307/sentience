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
using sentience.calibration;

namespace sentience.core
{
    /// <summary>
    /// head containing one or more stereo cameras
    /// </summary>
    public class stereoHead : pos3D
    {
        public int no_of_cameras;                // number of cameras on the head

        //public int baseline_mm = 100;            // distance between cameras
        //public int image_width = 320;
        //public int image_height = 240;

        public pos3D[] cameraPosition;           // position and orientation of each camera
        public stereoFeatures[] features;        // stereo features observed by each camera
        public String[] imageFilename;           // filename of raw image for each camera
        public calibrationStereo[] calibration;  // calibration data for each camera
        public rayModelLookup[] sensormodel;     // sensor model data for each camera
        public scanMatching[] scanmatch;         // simple scan matching

        public stereoHead(int no_of_cameras) : base(0,0,0)
        {
            this.no_of_cameras = no_of_cameras;
            // create feature lists
            features = new stereoFeatures[no_of_cameras];
            // store filenames of the images for each camera
            imageFilename = new String[no_of_cameras * 2];
            // create the cameras
            cameraPosition = new pos3D[no_of_cameras];
            // calibration data
            calibration = new calibrationStereo[no_of_cameras];
            for (int cam = 0; cam < no_of_cameras; cam++)
            {
                calibration[cam] = new calibrationStereo();
                cameraPosition[cam] = new pos3D(0, 0, 0);
                features[cam] = null; // new stereoFeatures();
            }
            // sensor models
            sensormodel = new rayModelLookup[no_of_cameras];
            //scan matching
            scanmatch = new scanMatching[no_of_cameras];

            /*
            if (no_of_cameras == 4) initQuadCam();
            if (no_of_cameras == 2) initDualCam();
            if (no_of_cameras == 1) initSingleStereoCamera(false);
             */
        }

        /*
        public void initQuadCam()
        {
            if (no_of_cameras == 4)
            {
                // Cam 1
                cameraPosition[0].roll = (float)Math.PI / 4;
                cameraPosition[0].pan = (float)Math.PI / 2;

                // Cam 2
                cameraPosition[1].roll = -(float)Math.PI / 4;
                cameraPosition[1].pan = 0;

                // Cam 3
                cameraPosition[2].roll = (float)Math.PI / 4;
                cameraPosition[2].pan = -(float)Math.PI / 2;

                // Cam 4
                cameraPosition[3].roll = -(float)Math.PI / 4;
                cameraPosition[3].pan = -(float)Math.PI;
            }
        }
         */

        /*
        public void initQuadCamRotated()
        {
            if (no_of_cameras == 4)
            {
                // Cam 1
                cameraPosition[0].roll = (float)Math.PI / 4;
                cameraPosition[0].pan = (float)(Math.PI / 4);

                // Cam 2
                cameraPosition[1].roll = -(float)Math.PI / 4;
                cameraPosition[1].pan = -(float)(Math.PI / 4);

                // Cam 3
                cameraPosition[2].roll = (float)Math.PI / 4;
                cameraPosition[2].pan = -(float)(Math.PI * 3 / 4);

                // Cam 4
                cameraPosition[3].roll = -(float)Math.PI / 4;
                cameraPosition[3].pan = -(float)(Math.PI * 5 / 4);
            }
        }
        */

        /*
        public void initDualCam()
        {
            image_width = 320;
            image_height = 240;

            if (no_of_cameras == 2)
            {
                float roll_angle = 45.0f / 180.0f * (float)Math.PI;
                float pan_angle = 45.0f / 180.0f * (float)Math.PI;

                // A
                cameraPosition[0].roll = -roll_angle;
                cameraPosition[0].pan = pan_angle;

                // B
                cameraPosition[1].roll = -roll_angle;
                cameraPosition[1].pan = -pan_angle;
            }
        }
        */

        /*
        public void initSingleStereoCamera(bool rolled)
        {
            image_width = 320;
            image_height = 240;

            float roll_angle = 0;
            if (rolled) roll_angle = 45.0f / 180.0f * (float)Math.PI;
            float pan_angle = 0.0f / 180.0f * (float)Math.PI;

            cameraPosition[0].roll = -roll_angle;
            cameraPosition[0].pan = pan_angle;
        }
         */

        /// <summary>
        /// loads calibration data for the given camera
        /// </summary>
        /// <param name="camera_index"></param>
        /// <param name="calibrationFilename"></param>
        public void loadCalibrationData(int camera_index, String calibrationFilename)
        {
            // load the data
            calibration[camera_index].Load(calibrationFilename);
            cameraPosition[camera_index] = calibration[camera_index].positionOrientation;

            // create the rectification lookup tables
            calibration[camera_index].updateCalibrationMaps();
        }

        /// <summary>
        /// load calibration data for all cameras from the given directory
        /// </summary>
        /// <param name="directory"></param>
        public void loadCalibrationData(String directory)
        {
            for (int cam = 0; cam < no_of_cameras; cam++)
            {
                String filename = directory + "calibration" + Convert.ToString(cam) + ".xml";
                loadCalibrationData(cam, filename);
            }
        }

        /// <summary>
        /// assign stereo features to the given camera
        /// </summary>
        /// <param name="camera_index"></param>
        /// <param name="featurelist"></param>
        public void setStereoFeatures(int camera_index, float[] featurelist, float[] uncertainties, int no_of_features)
        {
            if (features[camera_index] == null)
                features[camera_index] = new stereoFeatures(no_of_features);

            if (featurelist != null)
            {
                features[camera_index].no_of_features = no_of_features;
                features[camera_index].features = (float[])featurelist.Clone();
                if (uncertainties != null)
                    features[camera_index].uncertainties = (float[])uncertainties.Clone();
            }
            else
            {
                features[camera_index].no_of_features = 0;
                features[camera_index].features = null;
            }
        }

        public void setStereoFeatures(int camera_index, stereoFeatures feat)
        {
            features[camera_index] = feat;
        }

        /// <summary>
        /// load stereo features for all cameras
        /// </summary>
        /// <param name="binfile"></param>
        /// <param name="head"></param>
        public void loadStereoFeatures(BinaryReader binfile)
        {            
            for (int i = 0; i < no_of_cameras; i++)
            {
                // create a new object
                stereoFeatures feat = new stereoFeatures(1);

                // load features for this camera
                feat.load(binfile);

                // update the head with these features
                setStereoFeatures(i, feat);
            }
        }

        /// <summary>
        /// save stereo features for all cameras to file
        /// </summary>
        /// <param name="binfile"></param>
        public void saveStereoFeatures(BinaryWriter binfile)
        {
            for (int i = 0; i < no_of_cameras; i++)
                features[i].save(binfile);
        }

        /// <summary>
        /// update the colour of each feature from the given image
        /// </summary>
        /// <param name="camera_index"></param>
        /// <param name="img"></param>
        public void updateFeatureColours(int camera_index, Byte[] img)
        {
            for (int f = 0; f < features[camera_index].no_of_features; f++)
            {
                int idx = f * 3;
                stereoFeatures feat = features[camera_index];
                int x = (int)feat.features[idx];
                int y = (int)feat.features[idx + 1];
                int n = ((y * calibration[camera_index].leftcam.image_width) + x) * 3;
                feat.colour[f, 2] = img[n];
                feat.colour[f, 1] = img[n+1];
                feat.colour[f, 0] = img[n+2];
            }
        }

    }
}
