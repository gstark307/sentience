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
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class possiblePose
    {
        public int index;
        public float x, y;
        public float pan; 
        public float score;
        public int time_steps;  //the number of time steps for which this pose has been updated

        public possiblePose(int index, float x, float y, int pan, float score)
        {
            this.index = index;
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.score = score;
        }

        public possiblePose(float x, float y, float pan)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.score = 0;  // this should be a running average
            this.time_steps = 1;
        }

        public pos3D subtract(pos3D pos)
        {
            pos3D sum = new pos3D(x, y, 0);
            sum.x = x - pos.x;
            sum.y = y - pos.y;
            sum.pan = pan - pos.pan;
            return (sum);
        }
    }
}
