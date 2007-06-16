/*
    Banchmark timing routines
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
using System.Text;

namespace sluggish.utilities.timing
{
    /// <summary>
    /// a class used to calculate benchmark timings
    /// </summary>
    public class stopwatch
    {
        private DateTime stopWatchTime;
        public long time_elapsed_mS = 0;

        public void Start()
        {
            stopWatchTime = DateTime.Now;
        }

        public long Stop()
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan timeDiff;

            timeDiff = currentTime.Subtract(stopWatchTime);
            time_elapsed_mS = (long)timeDiff.TotalMilliseconds;
            return (time_elapsed_mS);
        }

    }
}