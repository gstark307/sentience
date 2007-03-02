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
    /// <summary>
    /// stores a series of robot poses
    /// </summary>
    public class particlePath
    {
        // a unique ID for this path
        public UInt32 ID;

        // the index in the path arraylist at which the parent path connects to this path
        public particlePose branch_pose = null;

        // the current pose
        public particlePose current_pose = null;

        // the maximum number of poses which may be stored within the path
        public int max_length;

        // list of poses within the path
        public ArrayList path;

        // total score for all poses within the path
        public float total_score = 0;
        public int total_poses = 0;

        public particlePath(int max_length)
        {
            this.max_length = max_length;
            path = new ArrayList();
        }

        public particlePath(particlePath parent, UInt32 path_ID)
        {
            ID = path_ID;
            parent.current_pose.no_of_children++;
            current_pose = parent.current_pose;
            branch_pose = parent.current_pose;

            this.max_length = parent.max_length;            
            path = new ArrayList();
            total_score = parent.total_score;
            total_poses = parent.total_poses;
        }

        public particlePath(float x, float y, float pan,
                            int max_length, UInt32 time_step, UInt32 path_ID)
        {
            ID = path_ID;
            this.max_length = max_length;
            path = new ArrayList();
            particlePose pose = new particlePose(x, y, pan, ID);
            pose.time_step = time_step;
            Add(pose);
        }

        public void Add(particlePose pose)
        {
            // set the parent of this pose
            pose.parent = current_pose;

            // set the current pose
            current_pose = pose;

            // add the pose to the path
            path.Add(pose);

            total_score += pose.score;
            total_poses++;

            // ensure that the path does not exceed a maximum length
            if (path.Count > max_length)
                path.RemoveAt(0);
        }

        /// <summary>
        /// remove the mapping particles associated with this path
        /// </summary>
        public bool Remove(occupancygridMultiHypothesis grid)
        {
            UInt32 path_ID = 0;
            particlePose pose = current_pose;
            if (current_pose != null)
            {
                path_ID = current_pose.path_ID;
                while ((pose != branch_pose) && (path_ID == pose.path_ID))
                {
                    pose.Remove(grid);
                    if (path_ID == pose.path_ID) pose = pose.parent;
                }
                if (pose != null)
                {
                    pose.no_of_children--;
                    if (pose.no_of_children == 0)
                    {
                        // there are no children remaining, so label the previous path
                        // with the current path ID
                        int children = 0;
                        while ((pose != null) && (children < 1))
                        {
                            children = pose.no_of_children;
                            pose.path_ID = path_ID;
                            if (children < 1) pose = pose.parent;
                        }
                    }
                }
            }
            if (pose == branch_pose)
                return (true);
            else
                return (false);
        }
    }
}
