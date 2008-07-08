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
using System.Collections.Generic;

namespace sentience.core
{
    /// <summary>
    /// data used to update an occupancy grid map
    /// </summary>
    public class ThreadMappingState
    {
        public bool active;

        // the robot's current estimated pose
        public pos3D pose;

        // the occupancy grid in which the robot is situated
        public occupancygridMultiHypothesis grid;

        // stereo rays to be thrown into the grid
        public List<evidenceRay>[] stereo_rays;

        // the motion model for the robot
        public motionModel motion;

        // benchmark timings
        public long benchmark_observation_update;
        public long benchmark_garbage_collection;
    }
}
