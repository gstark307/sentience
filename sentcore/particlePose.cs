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

namespace sentience.core
{
    public class particlePose
    {
        public int index;
        public float x, y;
        public float pan; 
        public float score;

        // observation made form this location
        //public ArrayList rays;

        // stores the grid cells (particlePoseObservedGridCell) which were observed from this pose
        public ArrayList observed_grid_cells;

        // the time step on which this particle was created
        public UInt32 time_step;

        public particlePose parent = null;
        public int no_of_children = 0;

        /*
        public particlePose(int index, float x, float y, int pan, float score)
        {
            this.index = index;
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.score = score;
            observed_grid_cells = new ArrayList();
        }
         */

        public particlePose(float x, float y, float pan)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.score = 0;  // this should be a running average
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
        /// <param name="rays"></param>
        public void AddObservation(ArrayList stereo_rays, occupancygridMultiHypothesis grid)
        {
            // itterate through each ray
            //rays = new ArrayList();
            for (int r = 0; r < stereo_rays.Count; r++)
            {
                // observed ray.  Note that this is in an egocentric
                // coordinate frame relative to the head of the robot
                evidenceRay ray = (evidenceRay)stereo_rays[r];
                
                // translate and rotate this ray appropriately for the pose
                evidenceRay trial_ray = ray.trialPose(pan, x, y);

                // TODO: update the grid cells for this ray
                grid.Insert(trial_ray, this);

                // add to the observation
                //rays.Add(trial_ray);
            }
        }

        /// <summary>
        /// remove the mapping particles associated with this pose
        /// </summary>
        public void Remove()
        {
            //rays = null;
        }
    }
}
