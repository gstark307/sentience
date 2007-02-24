/*
    Motion model used to predict the next step
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
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class motionModel
    {
        private robot rob;

        // angular velocity of the wheels in radians/sec
        public float LeftWheelAngularVelocity = 0;
        public float RightWheelAngularVelocity = 0;

        // speed in mm / sec
        public float speed = 0;

        public motionModel(robot rob)
        {
            this.rob = rob;
        }

        /// <summary>
        /// predict the next time step
        /// </summary>
        public void Predict()
        {
            // motion for a simple two wheeled robot

            float WheelRadius = rob.WheelDiameter_mm / 2;

            rob.x += (speed * (float)Math.Cos(rob.pan));
            rob.y += (speed * (float)Math.Sin(rob.pan));

            rob.pan += ((WheelRadius / (2 * rob.WheelBase_mm)) * (RightWheelAngularVelocity - LeftWheelAngularVelocity));
            speed += ((WheelRadius / 2) * (RightWheelAngularVelocity + LeftWheelAngularVelocity));
        }
    }
}
