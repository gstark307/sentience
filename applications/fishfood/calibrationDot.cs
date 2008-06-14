/*
    object representing a dot on the calibration pattern
    Copyright (C) 2008 Bob Mottram
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

namespace sentience.calibration
{
    public class calibrationDot : hypergraph_node
    {   
        // is this the centre dot?
        public bool centre;
        
        // colour of the dot
        public float r, g, b;

        // pixel position within the image
        public double x, y;

        // rectified pixel position within the image
        public float rectified_x, rectified_y;
        
        // radius of the dot in pixels
        public float radius;
        
        // coordinate on the grid
        public int grid_x, grid_y;
        
        public calibrationDot() : base(4)
        {
        }
    }
}
