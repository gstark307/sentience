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

namespace sentience.core
{
    /// <summary>
    /// this thread is used to update the occupancy grid map 
    /// and current pose estimate for the robot
    /// </summary>
    public class ThreadMappingUpdate
    {
        private WaitCallback _callback;
        private object _data;

        /// <summary>
        /// constructor
        /// </summary>
        public ThreadMappingUpdate(WaitCallback callback, object data)
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
            ThreadMappingState state = (ThreadMappingState)_data;
            Update(state);
            state.active = false;
            _callback(_data);
        }

        /// <summary>
        /// update the robot's map
        /// </summary>
        /// <param name="state">robot state</param>
        private static void Update(ThreadMappingState state)
        {
            // update all current poses with the observed rays
            state.motion.AddObservation(state.stereo_rays, false);

            // what's the relative position of the robot inside the grid ?
            pos3D relative_position = new pos3D(state.pose.x - state.grid.x, state.pose.y - state.grid.y, 0);
            relative_position.pan = state.pose.pan - state.grid.pan;

            // garbage collect dead occupancy hypotheses
            state.grid.GarbageCollect();
        }
    }
}
