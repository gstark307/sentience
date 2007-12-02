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
using System.Collections;
using System.Threading;
using sluggish.utilities.timing;

namespace sentience.core
{
    /// <summary>
    /// this thread is used to update the occupancy grid map 
    /// and current pose estimate for the robot
    /// </summary>
    public class ThreadStereoCorrespondence
    {
        private WaitCallback _callback;
        private object _data;

        /// <summary>
        /// constructor
        /// </summary>
        public ThreadStereoCorrespondence(WaitCallback callback, object data)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            _data = data;
        }

        /// <summary>
        /// ThreadStart delegate
        /// </summary>
        public void Execute()
        {
            ThreadStereoCorrespondenceState state = (ThreadStereoCorrespondenceState)_data;
            Update(state);
            state.active = false;
            _callback(_data);
        }

        /// <summary>
        /// calculate stereo correspondence
        /// </summary>
        /// <param name="state">stereo correspondence state</param>
        private static void Update(ThreadStereoCorrespondenceState state)
        {
			// set the number of features we need, later to be turned into rays
            state.correspondence.setRequiredFeatures(state.no_of_stereo_features);

            // set the calibration data for this camera
            state.correspondence.setCalibration(state.head.calibration[state.stereo_camera_index]);

			// run the correspondence algorithm
			state.correspondence.loadRawImages(
			    state.stereo_camera_index, 
			    state.fullres_left, 
			    state.fullres_right, 
			    state.head, 
			    state.no_of_stereo_features, 
			    state.bytes_per_pixel, 
			    state.correspondence_algorithm_type);
			
            // perform scan matching for forwards or rearwards looking cameras
            float pan_angle = state.head.pan + state.head.cameraPosition[state.stereo_camera_index].pan;
            if ((pan_angle == 0) || (pan_angle == (float)Math.PI))
            {
                // create a scan matching object if needed
                if (state.head.scanmatch[state.stereo_camera_index] == null)
                    state.head.scanmatch[state.stereo_camera_index] = new scanMatching();

                if (state.EnableScanMatching)
                {
                    // perform scan matching
                    state.head.scanmatch[state.stereo_camera_index].update(
					    state.correspondence.getRectifiedImage(true),
                        state.head.calibration[state.stereo_camera_index].leftcam.image_width,
                        state.head.calibration[state.stereo_camera_index].leftcam.image_height,
                        state.head.calibration[state.stereo_camera_index].leftcam.camera_FOV_degrees * (float)Math.PI / 180.0f,
                        state.head.cameraPosition[state.stereo_camera_index].roll,
                        state.ScanMatchingMaxPanAngleChange * (float)Math.PI / 180.0f);
                    if (state.head.scanmatch[state.stereo_camera_index].pan_angle_change != scanMatching.NOT_MATCHED)
                    {
                        state.scanMatchesFound = true;
                        if (state.ScanMatchingPanAngleEstimate == scanMatching.NOT_MATCHED)
                        {
                            // if this is the first time a match has been found 
                            // use the current pan estimate
                            state.ScanMatchingPanAngleEstimate = state.pan;
                        }
                        else
                        {
                            if (pan_angle == 0)
                                // forward facing camera
                                state.ScanMatchingPanAngleEstimate -= state.head.scanmatch[state.stereo_camera_index].pan_angle_change;
                            else
                                // rearward facing camera
                                state.ScanMatchingPanAngleEstimate += state.head.scanmatch[state.stereo_camera_index].pan_angle_change;
                        }
                    }
                }
            }                 
            else state.head.scanmatch[state.stereo_camera_index] = null;
			
        }
    }
}
