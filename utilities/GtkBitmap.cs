/*
    Functions to enable accessing byte arrays within Gtk Image/Pixbuf objects
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
using System.IO;
using sluggish.utilities.logging;

namespace sluggish.utilities.gtk
{
	/// <summary>
	/// This class contains functions for accessing bitmap arrays within Gtk Image objects
	/// </summary>
	public class GtkBitmap
	{
		// length of the Pixbuf data header
		const int pixdata_header = 24;
		
		/// <summary>
		/// downsample the given bitmap
		/// </summary>
		public byte[] Downsample(byte[] bmp, int image_width, int image_height, int bytes_per_pixel,
		                         int downsample_factor)
		{
		    // downsampled image dimensions
		    int downsampled_width = image_width / downsample_factor;
		    int downsampled_height = image_height / downsample_factor;
		    
		    // create downsampled image array
		    byte[] downsampled = new byte[downsampled_width * downsampled_height * bytes_per_pixel];
		    
		    int n = 0;
		    int[] area_average = new int[bytes_per_pixel]; 
		    for (int y = 0; y < downsampled_height; y++)
		    {
		        for (int x = 0; x < downsampled_width; x++)
		        {
		            // clear the average values
		            int hits = 0;
		            for (int b = 0; b < bytes_per_pixel; b++)
		                area_average[b] = 0;
		                
		            for (int yy = y * downsample_factor; yy < (y+1) * downsample_factor; yy++)
		            {
  		                for (int xx = x * downsample_factor; xx < (x+1) * downsample_factor; xx++)
		                {
		                    for (int b = 0; b < bytes_per_pixel; b++)
		                        area_average[b] += bmp[(((yy * image_height) + image_width) * bytes_per_pixel) + b];
		                    hits++;
		                }
		            }
		            
		            // update the downsampled image
		            if (hits > 0)
		            {
		                for (int b = 0; b < bytes_per_pixel; b++)
		                    downsampled[n + b] = (byte)(area_average[b] / hits);
		            }
		            
		            n += bytes_per_pixel;
		        }
		    }
		    return(downsampled);
		}
				
				
		#region "loading and saving from file"
				
		/// <summary> 
		/// load bitmap data from file
		/// </summary>
		public static byte[] Load(String filename, 
		                          ref int image_width, ref int image_height)
		{
		    if (File.Exists(filename))
		    {
		        sluggish.utilities.gtk.Image img = new sluggish.utilities.gtk.Image(filename);
		        Gdk.Pixbuf buffer = img.MakePixbufFromCompressedData();
		        image_width = img.Width;
		        image_height = img.Height;
		        byte[] bmp = new byte[img.Width * img.Height * 3];
		        getBitmap(buffer, bmp);		    
		        return(bmp);
		    }
		    else
		        return(null);
		}
		
		/// <summary>
		/// save the given bitmap to a file in the given format (jpeg/png)
		/// </summary>
		private static void Save(byte[] bmp, int image_width, int image_height,
		                         String filename, String format)
		{
		    Gdk.Pixbuf buffer = createPixbuf(image_width, image_height);		    
		    setBitmap(bmp, buffer);
		    buffer.Save(filename, format);
		}
		
		/// <summary>
		/// save the given bitmap to a jpeg file
		/// </summary>
		public static void SaveAsJpeg(byte[] bmp, int image_width, int image_height,
		                              String filename)
		{
		    Save(bmp, image_width, image_height, filename, "jpeg");
		}

		/// <summary>
		/// save the given bitmap to a png file
		/// </summary>
		public static void SaveAsPng(byte[] bmp, int image_width, int image_height,
		                             String filename)
		{
		    Save(bmp, image_width, image_height, filename, "png");
		}
		
		#endregion
				
		#region "conversion between Pixbuf and byte array"
				
		/// <summary>
	    /// updates the given Pixbuf object from the bitmap array
	    /// </summary>
		/// updates the given image object from the bitmap array
		public static Gdk.Pixbuf setBitmap(byte[] bmp, Gdk.Pixbuf pixbuffer)
		{
		    if (pixbuffer != null)
		    {
				Gdk.Pixdata pd = new Gdk.Pixdata();
					
				// extract the raw data in uncompressed format
			    pd.FromPixbuf(pixbuffer, false);
				byte[] pixel = pd.Serialize();
				
				// set the pixel values
				for (int i = 0; i < bmp.Length; i++) 
				    pixel[pixdata_header + i] = (byte)bmp[i];				    
					
				pd.Deserialize((uint)pixel.Length, pixel);		
				
				// return the pixel data as a Pixbuf object in compressed format
				return(Gdk.Pixbuf.FromPixdata(pd, true));
		    }
		    else
		    {
		        EventLog.AddError("GtkBitmap/getBitmap/pixbuffer has not been initialised");
		        return(null);
		    }
		}
		
		/// <summary>
	    /// updates the given image object from the bitmap array
	    /// </summary>
		/// updates the given image object from the bitmap array
		public static void setBitmap(byte[] bmp, Gtk.Image img)
		{
			img.Pixbuf = setBitmap(bmp, img.Pixbuf);
		}

		/// <summary>
		/// returns the bitmap data from a Pixbuf object
		/// </summary>
		public static void getBitmap(Gdk.Pixbuf pixbuffer, byte[] bmp)
		{
		    if (pixbuffer != null)
		    {
		        if (bmp != null)
		        {
					Gdk.Pixdata pd = new Gdk.Pixdata();
																	
					pd.FromPixbuf(pixbuffer, false);
					byte[] pixel = pd.Serialize();
					
					if (pd.Rowstride != pixbuffer.Width*3)
					{
					    // uneven stride length
					    long real_length = pd.Rowstride * pixbuffer.Height;
					    long n = 0;
					    long w = pixbuffer.Width*3;
					    for (int y = 0; y < pixbuffer.Height; y++)
					    {
					        long n1 = (y*pixbuffer.Width)*3;
					        for (int x = 0; x < pd.Rowstride; x++)
					        {
					            if (x < w) bmp[n1] = pixel[n];
					            n++;
					            n1++;
					        }
					    }
					}
					else
					{
			            for (int i = 0; i < bmp.Length; i++)
		                    bmp[i] = pixel[pixdata_header + i];
		            }
					
					pd.Deserialize((uint)pixel.Length, pixel);
				}
				else EventLog.AddError("GtkBitmap/getBitmap/bmp not defined");
		    }
		    else EventLog.AddError("GtkBitmap/getBitmap/pixbuffer has not been initialised");
		}
		
		/// <summary>
		/// returns the bitmap data from an image
		/// </summary>
		public static void getBitmap(Gtk.Image img, byte[] bmp)
		{
		    getBitmap(img.Pixbuf, bmp);
		}
		
		#endregion
		
		
		#region "creation of pixbuf objects"
		
		/// <summary>
	    /// create a pixbuf object with the given dimensions
	    /// </summary>
		public static Gdk.Pixbuf createPixbuf(int width, int height)
		{			
			Gdk.Pixbuf pixels = new Gdk.Pixbuf(Gdk.Colorspace.Rgb, false, 8, width, height);
			
		    return(pixels);
		}
		
		#endregion

	}
}