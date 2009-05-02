/*
    simple odometry data object
    Copyright (C) 2009 Bob Mottram
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
using System.IO;
using System.Collections.Generic;

namespace sentience.core
{
    public class OdometryData
    {
        public float x, y, z;
        public float orientation, tilt, roll;
        public DateTime timestamp;

        public OdometryData Copy()
        {
            OdometryData data = new OdometryData();
            data.x = x;
            data.y = y;
			data.y = z;
            data.orientation = orientation;
			data.tilt = tilt;
			data.roll = roll;
            data.timestamp = timestamp;
            return (data);
        }

        /// <summary>
        /// returns an interploated pose between two given poses at the given time
        /// </summary>
        /// <param name="t">time of the interpolated pose</param>
        /// <param name="start_point">start pose</param>
        /// <param name="end_point">end pose</param>
        /// <returns>interpolated pose, or null</returns>
        public static OdometryData Interpolate(
            DateTime t,
            OdometryData start_point,
            OdometryData end_point)
        {
            OdometryData interpolated = null;

            // is t between the start end end poses ?
            if ((t >= start_point.timestamp) &&
                (t <= end_point.timestamp))
            {
                // calculate the fraction of time elapsed since the start point
                TimeSpan diff = end_point.timestamp.Subtract(start_point.timestamp);
                TimeSpan elapsed = t.Subtract(start_point.timestamp);
                float fraction = (float)(elapsed.TotalMilliseconds / diff.TotalMilliseconds);

                // create interpolated pose
                interpolated.x = start_point.x + ((end_point.x - start_point.x) * fraction);
                interpolated.y = start_point.y + ((end_point.y - start_point.y) * fraction);
                interpolated.z = start_point.z + ((end_point.z - start_point.z) * fraction);
                interpolated.orientation = start_point.orientation + ((end_point.orientation - start_point.orientation) * fraction);
                interpolated.tilt = start_point.tilt + ((end_point.tilt - start_point.tilt) * fraction);
                interpolated.roll = start_point.roll + ((end_point.roll - start_point.roll) * fraction);
                interpolated.timestamp = t;
            }
            return (interpolated);
        }

        #region "loading and saving"

        public static OdometryData Read(BinaryReader br)
        {
            OdometryData data = new OdometryData();
            data.x = br.ReadSingle();
            data.y = br.ReadSingle();
            data.z = br.ReadSingle();
            data.orientation = br.ReadSingle();
            data.tilt = br.ReadSingle();
            data.roll = br.ReadSingle();
            data.timestamp = DateTime.FromBinary(br.ReadInt64());
            return (data);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(x);
            bw.Write(y);
			bw.Write(z);
            bw.Write(orientation);
            bw.Write(tilt);
            bw.Write(roll);
            bw.Write(timestamp.ToBinary());
        }

        #endregion
    }
}
