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
    public class possiblePath
    {
        // the current pose
        public possiblePose current_pose = null;

        // the maximum number of poses which may be stored within the path
        public int max_length;

        // list of poses within the path
        public ArrayList path;

        // total score for all poses within the path
        public float total_score = 0;

        public possiblePath(int max_length)
        {
            this.max_length = max_length;
            path = new ArrayList();
        }

        public possiblePath(possiblePath parent)
        {
            current_pose = parent.current_pose;

            this.max_length = parent.max_length;

            // copy the path of the parent
            path = (ArrayList)parent.path.Clone();

            total_score = parent.total_score;
        }

        public possiblePath(float x, float y, float pan,
                            int max_length)
        {
            this.max_length = max_length;
            path = new ArrayList();
            Add(new possiblePose(x, y, pan));
        }

        public void Add(possiblePose pose)
        {
            // set the current pose
            current_pose = pose;

            // add the pose to the path
            path.Add(pose);
            total_score += pose.score;

            // ensure that the path does not exceed a maximum length
            if (path.Count > max_length)
            {
                possiblePose victim = (possiblePose)path[0];
                total_score -= victim.score;
                path.RemoveAt(0);
            }
        }
    }
}
