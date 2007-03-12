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
                                    occupancygridMultiHypothesis grid,
                                    stereoHead head)
        {
            // clear the localisation score
            float localisation_score = 0;

            // itterate for each stereo camera
            for (int cam = 0; cam < stereo_rays.Length; cam++)
            {
                // position of the centre of the baseline of the camera
                // this should be adjusted depending upon the position of the 
                // robots head relative to the centre of rotation of its body
                pos3D camera_centre = new pos3D(head.x, head.y, head.z);
                camera_centre.rotate(pan, 0, 0);
                camera_centre.translate(x, y, 0);

                // where are the left and right cameras?
                // these position offsets will be used to calculate the 
                // location of both cameras, and is used as the origin 
                // for the vacancy part of the sensor model
                float half_baseline_length = head.calibration[cam].baseline / 2;
                half_baseline_length *= (float)Math.Cos(head.calibration[cam].positionOrientation.roll); // correct for camera roll angle
                float cam_dx = half_baseline_length * (float)Math.Sin(camera_centre.pan - (Math.PI / 2));
                float cam_dy = half_baseline_length * (float)Math.Cos(camera_centre.pan - (Math.PI / 2));
                
                // where are the left and right cameras?
                float leftcam_x = camera_centre.x + cam_dx;
                float leftcam_y = camera_centre.y + cam_dy;
                float rightcam_x = camera_centre.x - cam_dx;
                float rightcam_y = camera_centre.y - cam_dy;

                // itterate through each ray
                for (int r = 0; r < stereo_rays[cam].Count; r++)
                {
                    // observed ray.  Note that this is in an egocentric
                    // coordinate frame relative to the head of the robot
                    evidenceRay ray = (evidenceRay)stereo_rays[cam][r];

                    // translate and rotate this ray appropriately for the pose
                    evidenceRay trial_ray = ray.trialPose(camera_centre.pan, camera_centre.x, camera_centre.y);

                    // update the grid cells for this ray and update the
                    // localisation score accordingly
                    localisation_score += grid.Insert(trial_ray, this, head.sensormodel[cam],
                                                      leftcam_x, leftcam_y,
                                                      rightcam_x, rightcam_y);
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
