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
using System.IO;
using sentience.calibration;

namespace sentience.core
{
    /// <summary>
    /// head containing one or more stereo cameras
    /// </summary>
    public class stereoHead : pos3D
    {
        public int no_of_stereo_cameras;                // number of cameras on the head

        public pos3D[] cameraPosition;           // position and orientation of each camera
        public stereoFeatures[] features;        // stereo features observed by each camera
        public String[] imageFilename;           // filename of raw image for each camera
        public calibrationStereo[] calibration;  // calibration data for each camera
        public rayModelLookup[] sensormodel;     // sensor model data for each camera
        public scanMatching[] scanmatch;         // simple scan matching

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="no_of_stereo_cameras">
		/// A <see cref="System.Int32"/>
		/// The number of stereo cameras
		/// </param>
        public stereoHead(int no_of_stereo_cameras) : base(0,0,0)
        {
            this.no_of_stereo_cameras = no_of_stereo_cameras;
            
            // create feature lists
            features = new stereoFeatures[no_of_stereo_cameras];
            
            // store filenames of the images for each camera
            imageFilename = new String[no_of_stereo_cameras * 2];
            
            // create the cameras
            cameraPosition = new pos3D[no_of_stereo_cameras];
            
            // calibration data
            calibration = new calibrationStereo[no_of_stereo_cameras];
            
            // create objects for each stereo camera
            for (int cam = 0; cam < no_of_stereo_cameras; cam++)
            {
                calibration[cam] = new calibrationStereo();
                cameraPosition[cam] = new pos3D(0, 0, 0);
                features[cam] = null; // new stereoFeatures();
            }

            // sensor models
            sensormodel = new rayModelLookup[no_of_stereo_cameras];

            //scan matching
            scanmatch = new scanMatching[no_of_stereo_cameras];

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
        /// <param name="camera_index">stereo camera index</param>
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
            for (int cam = 0; cam < no_of_stereo_cameras; cam++)
            {
                String filename = directory + "calibration" + Convert.ToString(cam) + ".xml";
                loadCalibrationData(cam, filename);
            }
        }

        /// <summary>
        /// assign stereo features to the given camera
        /// </summary>
        /// <param name="camera_index">stereo camera index</param>
        /// <param name="featurelist">stereo features</param>
        /// <param name="uncertainties">governs the standard deviation used when ray throwing</param>
        /// <param name="no_of_features">number of stereo features</param>
        public void setStereoFeatures(int camera_index, 
                                      float[] featurelist, 
                                      float[] uncertainties, 
                                      int no_of_features)
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

        /// <summary>
        /// load stereo features for all cameras
        /// </summary>
        /// <param name="camera_index">stereo camera index</param>
        /// <param name="feat">stereo camera features</param>
        public void setStereoFeatures(int camera_index, stereoFeatures feat)
        {
            features[camera_index] = feat;
        }

        /// <summary>
        /// load stereo features for all cameras
        /// </summary>
        /// <param name="binfile">file to load from</param>
        public void loadStereoFeatures(BinaryReader binfile)
        {            
            for (int i = 0; i < no_of_stereo_cameras; i++)
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
        /// <param name="binfile">file to save to</param>
        public void saveStereoFeatures(BinaryWriter binfile)
        {
            for (int i = 0; i < no_of_stereo_cameras; i++)
                features[i].save(binfile);
        }

        /// <summary>
        /// update the colour of each feature from the given image
        /// </summary>
        /// <param name="camera_index">stereo camera index</param>
        /// <param name="img">colour image</param>
        public void updateFeatureColours(int camera_index, byte[] img)
        {
            // get the dimensions of the colour image
            int wdth = calibration[camera_index].leftcam.image_width;
            int hght = calibration[camera_index].leftcam.image_height;

            for (int f = 0; f < features[camera_index].no_of_features; f++)
            {
                int idx = f * 3;
                stereoFeatures feat = features[camera_index];
                
                // get the coordinate of the feature within the image
                int x = (int)feat.features[idx];
                int y = (int)feat.features[idx + 1];

                // correct the positions using the inverse calibration lookup
                if (calibration[camera_index].leftcam.calibration_map_inverse != null)
                {
                    //int x2 = calibration[camera_index].leftcam.calibration_map_inverse[x, y, 0];
                    //int y2 = calibration[camera_index].leftcam.calibration_map_inverse[x, y, 1];
                    //x = x2;
                    //y = y2;
                }

                // update the colour for this feature
                // there's a little averaging here to help reduce noise
                if (x > wdth - 2) x = wdth - 2;
                int n = ((y * wdth) + x) * 3;
                feat.colour[f, 2] = (byte)((img[n] + img[n + 3]) / 2);
                feat.colour[f, 1] = (byte)((img[n + 1] + img[n + 4]) / 2);
                feat.colour[f, 0] = (byte)((img[n + 2] + img[n + 5]) / 2);
                

                /*
                //testing colours
                int middle = x * hght / wdth;
                if (y > middle + (hght / 6))
                {
                    feat.colour[f, 2] = (Byte)255;
                    feat.colour[f, 1] = 0;
                    feat.colour[f, 0] = 0;
                }
                else
                {
                    if (y < middle - (hght / 6))
                    {
                        feat.colour[f, 2] = 0;
                        feat.colour[f, 1] = 0;
                        feat.colour[f, 0] = (Byte)255;
                    }
                    else
                    {
                        feat.colour[f, 2] = 0;
                        feat.colour[f, 1] = (Byte)255;
                        feat.colour[f, 0] = 0;
                    }
                }
                 */
            }
        }

    }
}
