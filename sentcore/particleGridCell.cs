/*
    Grid cell particle for distributed particle SLAM
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
    /// <summary>
    /// used to store a single grid cell hypothesis
    /// </summary>
    public class particleGridCell
    {
        // whether this particle is enabled or dead
        // this flag allows the system to go around collecting garbage
        public bool Enabled;

        // grid cell coordinate
        public short x, y, z;

        // probability of occupancy, taken from the sensor model and stored as log odds
        public float probabilityLogOdds;

        // the pose which made this observation
        public particlePose pose;

        public particleGridCell(int x, int y, int z, float probability, particlePose pose)
        {
            this.x = (short)x;
            this.y = (short)y;
            this.z = (short)z;
            this.probabilityLogOdds = util.LogOdds(probability); // store as log odds
            this.pose = pose;
            Enabled = true;
        }
    }
}
