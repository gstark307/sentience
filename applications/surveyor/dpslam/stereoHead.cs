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

namespace dpslam.core
{
    /// <summary>
    /// head containing one or more stereo cameras
    /// </summary>
    public class stereoHead : pos3D
    {
        public int no_of_stereo_cameras;                // number of cameras on the head

		public pos3D[] cameraPositionHeadRelative; // position and orientation of each camera repative to the head
        public pos3D[] cameraPosition;             // absolute position and orientation of each camera
		public float[] cameraBaseline;             // baseline of each stereo camera
		public float[] cameraFOVdegrees;           // FOV of each stereo camera
		public int[] cameraImageWidth;             // width of the camera image on each stereo camera
		public int[] cameraImageHeight;            // height of the camera image on each stereo camera
		public float[] cameraFocalLengthMm;        // focal length for each camera in mm
		public float[] cameraSensorSizeMm;         // sensor size of each camera
        public float[][] features;                 // stereo features observed by each camera (4 floats per feature, prob,x,y,disp)
        public byte[][] featureColours;            // colour for each stereo feature
        public int[] no_of_features;               // number of stereo features observed by each camera
        public rayModelLookup[] sensormodel;       // sensor model data for each camera
        public scanMatching[] scanmatch;           // simple scan matching

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
            features = new float[no_of_stereo_cameras][];
            featureColours = new byte[no_of_stereo_cameras][];
            no_of_features = new int[no_of_stereo_cameras];
                        
            // create the cameras
            cameraPosition = new pos3D[no_of_stereo_cameras];
            cameraPositionHeadRelative = new pos3D[no_of_stereo_cameras];
			cameraBaseline = new float[no_of_stereo_cameras];
			cameraFOVdegrees = new float[no_of_stereo_cameras];
			cameraImageWidth = new int[no_of_stereo_cameras];
			cameraImageHeight = new int[no_of_stereo_cameras];
			cameraFocalLengthMm = new float[no_of_stereo_cameras];
			cameraSensorSizeMm = new float[no_of_stereo_cameras];
                        
            // create objects for each stereo camera
            for (int cam = 0; cam < no_of_stereo_cameras; cam++)
            {
                cameraPosition[cam] = new pos3D(0, 0, 0);
                cameraPositionHeadRelative[cam] = new pos3D(0, 0, 0);
				cameraBaseline[cam] = 120;
				cameraFOVdegrees[cam] = 68;
				cameraImageWidth[cam] = 320;
				cameraImageHeight[cam] = 240;
				cameraFocalLengthMm[cam] = 3.6f;
				cameraSensorSizeMm[cam] = 4;
                features[cam] = null; // new stereoFeatures();
				featureColours[cam] = null;
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

		public int MAX_STEREO_FEATURES = 500;
		
        /// <summary>
        /// assign stereo features to the given camera
        /// </summary>
        /// <param name="camera_index">stereo camera index</param>
        /// <param name="feats">stereo features</param>
        /// <param name="no_of_feats">number of stereo features</param>
        public void setStereoFeatures(
		    int camera_index, 
            float[] feats, 
            int no_of_feats)
        {
            if (features[camera_index] == null)
			{
                features[camera_index] = new float[MAX_STEREO_FEATURES*4];
				featureColours[camera_index] = new byte[MAX_STEREO_FEATURES*3];
			}

			no_of_features[camera_index] = no_of_feats;
			
            if (feats != null)
            {			
				Buffer.BlockCopy(feats, 0, features[camera_index], 0, no_of_feats*4*4);
            }
        }
		
        /// <summary>
        /// assign stereo feature colours to the given camera
        /// </summary>
        /// <param name="camera_index">stereo camera index</param>
        /// <param name="feat_cols">stereo feature colours</param>
        /// <param name="no_of_feats">number of stereo features</param>
        public void setStereoFeatureColours(
		    int camera_index, 
            byte[] feat_cols, 
            int no_of_feats)
        {
            if (featureColours[camera_index] == null)
			{
                features[camera_index] = new float[MAX_STEREO_FEATURES*4];
				featureColours[camera_index] = new byte[MAX_STEREO_FEATURES*3];
			}

			no_of_features[camera_index] = no_of_feats;
			
            if (feat_cols != null)
            {			
				Buffer.BlockCopy(feat_cols, 0, featureColours[camera_index], 0, no_of_feats*3);
            }
        }

    }
}
