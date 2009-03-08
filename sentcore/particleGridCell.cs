/*
    Grid cell particle for distributed particle SLAM
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
using System.Text;
using sluggish.utilities;

namespace sentience.core
{
    /// <summary>
    /// grid particle base class
    /// </summary>
    public class particleGridCellBase
    {
        // observed colour
        public byte[] colour;

        // probability of occupancy, taken from the sensor model and stored as log odds
        public float probabilityLogOdds;

        /// <summary>
        /// constructor
        /// </summary>
        public particleGridCellBase()
        {
        }
    }


    /// <summary>
    /// grid particle used to store a single hypothesis about occupancy
    /// </summary>
    public sealed class particleGridCell : particleGridCellBase
    {
        // whether this particle is enabled or dead
        // this flag allows the system to go around collecting garbage
        public bool Enabled;

        // grid cell coordinate
        public short x, y, z;

        // the pose which made this observation
        public particlePose pose;

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x">x coordinate of the cell within the grid</param>
        /// <param name="y">y coordinate of the cell within the grid</param>
        /// <param name="z">z coordinate of the cell within the grid</param>
        /// <param name="probability">probability assigned to this cell</param>
        /// <param name="pose">the pose from which this cell was observed</param>
        /// <param name="colour">the colour of this cell</param>
        public particleGridCell(
            int x, int y, int z, 
            float probability, 
            particlePose pose,
            byte[] colour)
        {
            this.x = (short)x;
            this.y = (short)y;
            this.z = (short)z;
            this.probabilityLogOdds = probabilities.LogOdds(probability); // store as log odds
            this.pose = pose;
            this.colour = colour;
            Enabled = true;
        }

        #endregion
    }
}
