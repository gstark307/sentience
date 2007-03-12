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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using sentience.calibration;

namespace sentience.core
{
    public class particlePose
    {
        // position of the pose in millimetres
        public float x, y;

        // pan angle in radians
        public float pan;

        // localisation score
        public float score;

        // grid cells (particlePoseObservedGridCell) which were observed from this pose
        public ArrayList observed_grid_cells;

        // the path with which this pose is associated
        public UInt32 path_ID;
        public ArrayList previous_paths;

        // the time step on which this particle was created
        public UInt32 time_step;

        public particlePose parent = null;
        public int no_of_children = 0;

        public particlePose(float x, float y, float pan, UInt32 path_ID)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.score = 0;  // this should be a running average
            this.path_ID = path_ID;
            observed_grid_cells = new ArrayList();
        }

        public pos3D subtract(pos3D pos)
        {
            pos3D sum = new pos3D(x, y, 0);
            sum.x = x - pos.x;
            sum.y = y - pos.y;
            sum.pan = pan - pos.pan;
            return (sum);
        }

        /// <summary>
        /// add an observation taken from this pose
        /// </summary>
        /// <param name="rays">list of ray objects in this observation</param>
        /// <param name="grid">the occupancy grid into which to insert the observation</param>
        /// <param name="sensormodel">the sensor model to use for updating the grid</param>
        /// <param name="head_pan">pan angle of the head</param>
        public float AddObservation(ArrayList[] stereo_rays, 
                                    robot rob)
        {
            // clear the localisation score
            float localisation_score = 0;

            // calculate the position of the centre of the head relative to 
            // the centre of rotation of the robots body
            pos3D head_centroid = new pos3D(-(rob.BodyWidth_mm/2) + rob.head.x, 
                                            -(rob.BodyLength_mm/2) + rob.head.y,
                                            rob.head.z);

            // location of the centre of the head on the grid map
            // adjusted for the robot pose and the head pan and tilt angle.
            // Note that the positions and orientations of individual cameras
            // on the head have already been accounted for within stereoModel.createObservation
            pos3D head_location = head_centroid.rotate(rob.head.pan + pan, rob.head.tilt, 0);
            head_location = head_location.translate(x, y, 0);

            
            // itterate for each stereo camera
            for (int cam = 0; cam < stereo_rays.Length; cam++)
            {
                // calculate the position of the centre of the stereo camera
                // (baseline centre point)
                pos3D camera_centre_location = new pos3D(rob.head.calibration[cam].positionOrientation.x, rob.head.calibration[cam].positionOrientation.y, rob.head.calibration[cam].positionOrientation.y);
                camera_centre_location = camera_centre_location.rotate(rob.head.calibration[cam].positionOrientation.pan + rob.head.pan + pan, rob.head.calibration[cam].positionOrientation.tilt, rob.head.calibration[cam].positionOrientation.roll);
                camera_centre_location = camera_centre_location.translate(head_location.x, head_location.y, head_location.z);

                // where are the left and right cameras?
                // we need to know this for the origins of the vacancy models
                float half_baseline_length = rob.head.calibration[cam].baseline / 2;
                pos3D left_camera_location = new pos3D(-half_baseline_length, 0, 0);
                left_camera_location = left_camera_location.rotate(rob.head.calibration[cam].positionOrientation.pan + rob.head.pan + pan, rob.head.calibration[cam].positionOrientation.tilt + rob.head.tilt, rob.head.calibration[cam].positionOrientation.roll);
                pos3D right_camera_location = new pos3D(-left_camera_location.x, -left_camera_location.y, -left_camera_location.z);
                left_camera_location = left_camera_location.translate(camera_centre_location.x, camera_centre_location.y, camera_centre_location.z);
                right_camera_location = right_camera_location.translate(camera_centre_location.x, camera_centre_location.y, camera_centre_location.z);

                // itterate through each ray
                for (int r = 0; r < stereo_rays[cam].Count; r++)
                {
                    // observed ray.  Note that this is in an egocentric
                    // coordinate frame relative to the head of the robot
                    evidenceRay ray = (evidenceRay)stereo_rays[cam][r];

                    // translate and rotate this ray appropriately for the pose
                    evidenceRay trial_ray = ray.trialPose(camera_centre_location.pan, camera_centre_location.x, camera_centre_location.y);

                    // update the grid cells for this ray and update the
                    // localisation score accordingly
                    localisation_score += 
                        rob.LocalGrid.Insert(trial_ray, this, rob.head.sensormodel[cam],
                                             left_camera_location, right_camera_location);
                }
            }
            return (localisation_score);
        }

        /// <summary>
        /// remove the mapping particles associated with this pose
        /// </summary>
        public void Remove(occupancygridMultiHypothesis grid)
        {
            for (int i = 0; i < observed_grid_cells.Count; i++)
            {
                particleGridCell hypothesis = (particleGridCell)observed_grid_cells[i];
                grid.Remove(hypothesis);
            }
            observed_grid_cells.Clear();
            if (previous_paths != null) previous_paths.Clear();
        }

    }
}
