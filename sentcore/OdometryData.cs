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
		public float x, y;
		public float orientation;
		public float linear_velocity, angular_velocity;
		public DateTime timestamp;
		
		public OdometryData Copy()
		{
			OdometryData data = new OdometryData();
			data.x = x;
			data.y = y;
			data.orientation = orientation;
			data.linear_velocity = linear_velocity;
			data.angular_velocity = angular_velocity;
			data.timestamp = timestamp;
			return(data);
		}
		
        #region "loading and saving"
		
		public static OdometryData Read(BinaryReader br)
		{
			OdometryData data = new OdometryData();
			data.x = br.ReadSingle();
			data.y = br.ReadSingle();
			data.orientation = br.ReadSingle();
			data.linear_velocity = br.ReadSingle();
			data.angular_velocity = br.ReadSingle();
			data.timestamp = DateTime.FromBinary(br.ReadInt64());
			return(data);
		}
		
		public void Write(BinaryWriter bw)
		{
			bw.Write(x);
			bw.Write(y);
			bw.Write(orientation);
			bw.Write(linear_velocity);
			bw.Write(angular_velocity);
			bw.Write(timestamp.ToBinary());
		}
		
        #endregion
	}
}
