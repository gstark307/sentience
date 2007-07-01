/*
    line feature class
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

namespace sluggish.utilities
{
    /// <summary>
    /// class used to represent lines
    /// </summary>
	public class linefeature
	{		
		// joined to other lines
		public linefeature join_start;  // join linked to x0,y0
		public linefeature join_end;    // join linked to x1,y1
		    
		// start and end points of the line
		public float x0, y0, x1, y1;
		
		// length of the line
		private float length;


        #region "constructors"
        
		public linefeature(float x0, float y0, float x1, float y1)
		{
		    this.x0 = x0;
		    this.y0 = y0;
		    this.x1 = x1;
		    this.y1 = y1;
		}

		public linefeature()
		{
		}
		
		#endregion
		
		/// <summary>
		/// returns the length of the line
		/// </summary>
		public float getLength()
		{
		    if (length == 0)
		    {
		        float dx = x1 - x0;
		        float dy = y1 - y0;
		        length = (float)Math.Sqrt((dx*dx) + (dy*dy));
		    }
            return(length);
		}
		
		/// <summary>
		/// returns the gradient of the line
		/// </summary>
		public float getGradient()
		{
		    float grad = 9999;
		    //if (x1 >= x0)
		    {
		        float dx = x1 - x0;
		        float dy = y1 - y0;
                if (dx != 0)
                    grad = dy / dx;
		    }
		    /*
		    else
		    {
		        float dx = x0 - x1;
		        float dy = y0 - y1;
		        if (dx != 0)
		            grad = dy / dx;
		    }
		    */
		    return(grad);
		}

		/// <summary>
		/// returns the orientation of the line in radians
		/// </summary>
		public float getOrientation()
		{
		    float orientation = 0;
            float dx = x1 - x0;
		    float dy = y1 - y0;
		    length = (float)Math.Sqrt((dx*dx) + (dy*dy));
		    
		    if (length > 0)
		        orientation = (float)Math.Asin(dx / length);            
		    if (dy < 0) orientation = (float)(Math.PI*2)-orientation;
            if (orientation < 0) orientation += (float)(Math.PI);
            		    		    
		    return(orientation);
		}
	}
}
