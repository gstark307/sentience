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
using System.Text;

namespace sentience.core
{
    /// <summary>
    /// describes a section of a path used for simulation
    /// </summary>
    public class simulationPathSegment
    {
        #region "variables"
        
        public float x, y;                 // initial x,y position of the robot in millimetres
        public float pan;                  // initial heading of the robot in radians
        public int no_of_steps;            // number of steps along the path segment
        public float distance_per_step_mm; // distance per step in millimetres
        public float pan_per_step;         // change in pan angle per step in radians
        
        #endregion

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pan"></param>
        /// <param name="no_of_steps"></param>
        /// <param name="distance_per_step_mm"></param>
        /// <param name="pan_per_step"></param>
        public simulationPathSegment(float x, float y, float pan,
                                     int no_of_steps, float distance_per_step_mm, float pan_per_step)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.no_of_steps = no_of_steps;
            this.distance_per_step_mm = distance_per_step_mm;
            this.pan_per_step = pan_per_step;
        }
        
        #endregion

        #region "getting poses along this path segment"

        /// <summary>
        /// return this path segment as a sequence of poses
        /// </summary>
        /// <returns>list of poses of type particlePose</returns>
        public List<particlePose> getPoses()
        {
            List<particlePose> result = new List<particlePose>();
            float xx = x;
            float yy = y;
            float curr_pan = pan;
            for (int i = 0; i < no_of_steps; i++)
            {
                particlePose pose = new particlePose(xx, yy, curr_pan, null);
                result.Add(pose);
                curr_pan += pan_per_step;
                xx += (distance_per_step_mm * (float)Math.Sin(curr_pan));
                yy += (distance_per_step_mm * (float)Math.Cos(curr_pan));
            }
            return (result);
        }
        
        #endregion
    }
}
