/*
    Stores a set of regions which exist within an image
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

namespace sluggish.imageprocessing
{	
	public class regionCollection : ArrayList
	{
		public regionCollection()
		{
		}

		/// <summary>
		/// show all regions within the given image
		/// </summary>
		/// <param name="bmp">image to draw into</param>
		/// <param name="image_width">width of the image</param>
		/// <param name="image_height">height of the image</param>
		/// <param name="bytes_per_pixel">number of bytes per pixel</param>
		/// <param name="line_width">line width to use when drawing</param>
		/// <param name="style">display </param>
		public void Show(byte[] bmp, int image_width, int image_height,
		                 int bytes_per_pixel, int line_width, int style)
		{
		    for (int i = 0; i < Count; i++)
		    {
		        region r = (region)this[i];
		        r.Show(bmp, image_width, image_height, bytes_per_pixel, line_width, style);
		    }
		}		
	}
}
