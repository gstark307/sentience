/*
    byte array image functions
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
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace sentience.calibration
{
    /// <summary>
    /// various image processing routines
    /// </summary>
	public class image
    {
        #region "constructors"

        public image()
		{
        }

        #endregion
		
        #region "edge filters"
		
        public const int EDGE_FILTER_SIMPLE = 0;		
        public const int EDGE_FILTER_EXP = 1;		
        public const int EDGE_FILTER_SIN = 2;		

		
        private static void CreateEdgeFilterSin(int[] filter, float wavelengths)
		{
			for (int r = 0; r < filter.Length; r++)
			{
				float angle = r * (float)Math.PI * 2 * wavelengths / filter.Length;
				filter[r] = (int)(Math.Sin(angle) * 255);
			}
		}

		private static void CreateEdgeFilterSimple(int[] filter)
		{
			for (int r = 0; r < filter.Length; r++)
				filter[r] = 255;
		}
		
		private static void CreateEdgeFilterExp(int[] filter)
		{
		    int v = 255;	
			for (int r = 0; r < filter.Length; r++)
			{
				filter[r] = v;
				v /= 2;
			}
		}
		
        public static int[] CreateEdgeFilter(int filter_radius, int filter_type)
		{
			return(CreateEdgeFilter(filter_radius, filter_type, 0));
		}
		
        public static int[] CreateEdgeFilter(int filter_radius, int filter_type, float filter_value)
		{
			int[] edge_filter = new int[filter_radius];
			switch(filter_type)
			{
			    case EDGE_FILTER_SIMPLE:
			    {
				    CreateEdgeFilterSimple(edge_filter);
				    break;
			    }
			    case EDGE_FILTER_EXP:
			    {
				    CreateEdgeFilterExp(edge_filter);
				    break;
			    }
			    case EDGE_FILTER_SIN:
			    {
				    if (filter_value == 0) filter_value = 0.7f;
				    CreateEdgeFilterSin(edge_filter, filter_value);
				    break;
			    }
			}

			/*
			Console.Write("Edge filter coefficients = ");
			for (int r = 0; r < edge_filter.Length; r++)
				Console.Write(edge_filter[r].ToString() + ", ");
			Console.WriteLine("");
			*/
			
			return(edge_filter);
		}
		
        #endregion

        #region "bit depth changes"

        private static System.Drawing.Imaging.ColorPalette GetColourPalette(uint nColors)
        {
            // Assume monochrome image.
            System.Drawing.Imaging.PixelFormat bitscolordepth = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
            System.Drawing.Imaging.ColorPalette palette;    // The Palette we are stealing
            Bitmap bitmap;     // The source of the stolen palette

            // Determine number of colors.
            if (nColors > 2)
                bitscolordepth = System.Drawing.Imaging.PixelFormat.Format4bppIndexed;
            if (nColors > 16)
                bitscolordepth = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

            // Make a new Bitmap object to get its Palette.
            bitmap = new Bitmap(1, 1, bitscolordepth);

            palette = bitmap.Palette;   // Grab the palette

            bitmap.Dispose();           // cleanup the source Bitmap

            return palette;             // Send the palette back
        }

        /// <summary>
        /// set the pallette of the given bitmap to greyscale
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        public static void SetGreyScalePallette(Bitmap bmp)
        {
            bool fTransparent = false;
            uint no_of_colours = 255;

            System.Drawing.Imaging.ColorPalette pal = GetColourPalette(no_of_colours);

            for (uint i = 0; i < no_of_colours; i++)
            {
                uint Alpha = 0xFF;  // Colours are opaque.
                uint Intensity = i * 0xFF / (no_of_colours - 1);

                if (i == 0 && fTransparent) // Make this color index...
                    Alpha = 0;          // Transparent

                pal.Entries[i] = Color.FromArgb((int)Alpha,
                                                (int)Intensity,
                                                (int)Intensity,
                                                (int)Intensity);
            }

            // Set the palette into the new Bitmap object.
            bmp.Palette = pal;
        }

        #endregion

        #region "multi-frame averaging"

        /// <summary>
		/// returns an image averaged over several frames
		/// </summary>
		/// <param name="img"></param>
		/// <param name="img_width"></param>
		/// <param name="img_height"></param>
		/// <param name="buffer"></param>
		/// <param name="buffer_index"></param>
		/// <param name="average_img"></param>
        public static void averageImage(
            byte[] img, int img_width, int img_height,
            byte[,] buffer, ref int buffer_index, ref int buffer_fill,
            byte[] average_img)
        {
            // update the buffer
            for (int i = 0; i < buffer.GetLength(1); i++)
                buffer[buffer_index, i] = img[i];

            // sum the buffer contents
            for (int i = 0; i < buffer.GetLength(1); i++)
            {
                float av_value = 0;
                for (int j = 0; j < buffer_fill; j++)
                {
                    av_value += buffer[j, i];
                }
                av_value /= buffer_fill;
                average_img[i] = (byte)Math.Round(av_value);
            }

            buffer_fill++;
            if (buffer_fill > buffer.GetLength(0))
                buffer_fill = buffer.GetLength(0);

            // rollover
            buffer_index++;
            if (buffer_index >= buffer.GetLength(0))
            {
                buffer_index = 0;
            }
        }

        #endregion

        #region "noise removal"

        /// <summary>
		/// remove solitary pixels from a binary image
		/// </summary>
		/// <param name="binary_image">a binary image</param>
		/// <param name="minimum_matches">the minimum number of pixels which have the same state as the centre pixel at each position</param>
		public static bool[,] removeSolitaryPixels(bool[,] binary_image,
		                                           int minimum_matches)
		{		
		    int width = binary_image.GetLength(0);
		    int height = binary_image.GetLength(1);
		    
		    bool[,] result = new bool[width, height];
		    
		    for (int x = 1; x < width-1; x++)
		    {
		        for (int y = 1; y < height-1; y++)
		        {
		            bool state = binary_image[x, y];
		            result[x, y] = state;
		            
		            int opposing_pixel_count = 0;

                    int xx = x - 1;
                    while ((xx <= x + 1) && (opposing_pixel_count < minimum_matches))
                    {
                        int yy = y - 1;
                        while ((yy <= y + 1) && (opposing_pixel_count < minimum_matches))
                        {
                            if (binary_image[xx, yy] != state) opposing_pixel_count++;
                            yy++;
                        }
                        xx++;
                    }
		                        
		            if (opposing_pixel_count > minimum_matches)
	    		        result[x, y] = !state;
		        }
		    }
		    return(result);
		}
		
		#endregion
		


        /// <summary>
        /// detect vertical edges within a binary image
        /// </summary>
		public static ArrayList[] detectVerticalEdges(bool[,] binary_image)
		{
		    ArrayList[] vertical_edges = new ArrayList[binary_image.GetLength(1)]; 
		    
		    for (int y = 0; y < binary_image.GetLength(1); y++)
		    {		    
		        ArrayList detections = new ArrayList();
		        for (int x = 3; x < binary_image.GetLength(0)-3; x++)
		        {
		            if (binary_image[x-1, y] != binary_image[x, y])
		                if (binary_image[x-3, y] == binary_image[x-1, y])
		                    if (binary_image[x-2, y] == binary_image[x-1, y])
		                        if (binary_image[x, y] == binary_image[x+1, y])
		                            if (binary_image[x, y] == binary_image[x+2, y])
		                                detections.Add(x);
		        }
		        vertical_edges[y] = detections;		        
		    }
		    return(vertical_edges);
		}

        /// <summary>
        /// detect vertical edges within a blob image
        /// </summary>
		public static ArrayList[] detectVerticalEdges(float[,] blob_image, float threshold)
		{
		    ArrayList[] vertical_edges = new ArrayList[blob_image.GetLength(1)]; 
		    
		    for (int y = 0; y < blob_image.GetLength(1); y++)
		    {		    
		        ArrayList detections = new ArrayList();
		        for (int x = 2; x < blob_image.GetLength(0)-2; x++)
		        {		            
		            if (blob_image[x-1, y] < threshold)
		            {
		                if (blob_image[x, y] >= threshold)
		                    detections.Add(x);
		            }
		            else
		            {
		                if (blob_image[x, y] < threshold)
		                    detections.Add(x);
		            }
		        }
		        vertical_edges[y] = detections;		        
		    }
		    return(vertical_edges);
		}


        /// <summary>
        /// detect horizontal edges within a binary image
        /// </summary>
		public static ArrayList[] detectHorizontalEdges(bool[,] binary_image)
		{
		    ArrayList[] horizontal_edges = new ArrayList[binary_image.GetLength(0)];
		    
		    for (int x = 0; x < binary_image.GetLength(0); x++)		    
		    {
		        ArrayList detections = new ArrayList();
                for (int y = 3; y < binary_image.GetLength(1)-3; y++)		        
		        {
		            if (binary_image[x, y-1] != binary_image[x, y])
		                if (binary_image[x, y-3] == binary_image[x, y-1])
		                    if (binary_image[x, y-2] == binary_image[x, y-1])
		                        if (binary_image[x, y] == binary_image[x, y + 1])
		                            if (binary_image[x, y] == binary_image[x, y + 2])
		                                detections.Add(y);
		        }
		        horizontal_edges[x] = detections;
		    }
		    return(horizontal_edges);
		}

        /// <summary>
        /// detect horizontal edges within a blob image
        /// </summary>
		public static ArrayList[] detectHorizontalEdges(float[,] blob_image, float threshold)
		{
		    ArrayList[] horizontal_edges = new ArrayList[blob_image.GetLength(0)];
		    
		    for (int x = 0; x < blob_image.GetLength(0); x++)		    
		    {
		        ArrayList detections = new ArrayList();
                for (int y = 2; y < blob_image.GetLength(1)-2; y++)		        
		        {
		            if (blob_image[x, y-1] < threshold)
		            {
		                if (blob_image[x, y] >= threshold)
		                    detections.Add(y);
		            }
		            else
		            {
		                if (blob_image[x, y] < threshold)
		                    detections.Add(y);
		            }
		        }
		        horizontal_edges[x] = detections;
		    }
		    return(horizontal_edges);
		}
		
        /// <summary>
        /// detect vertically oriented edge features within a line grid pattern
        /// </summary>
		public static bool[,] detectVerticalEdges(byte[] mono_image, int width, int height,
		                                          int horizontal_suppression_radius,
		                                          int edge_patch_radius, int edge_threshold)
		{
		    // array which returns the results
		    bool[,] vertical_edges = new bool[width, height];
		
		    // edge threshold should be proportional to the number of pixels being considered
            edge_threshold *= edge_patch_radius;		    
		
		    // this array stores the absolute magnitude of edge responses
		    int[] edge_response = new int[width];
		    
		    // this array stores sliding sums of pixel intensities for each row
		    // of the image
		    int[] sliding_sum = new int[width];
		
		    for (int y = 0; y < height; y++)
		    {
		        int x = 0;
		        
		        // calc sliding sum for this row
		        sliding_sum[0] = mono_image[y * width];
		        for (x = 1; x < width; x++)
		            sliding_sum[x] = sliding_sum[x-1] + mono_image[(y*width)+x];  
		            
		        // calc edge responses
		        for (x = edge_patch_radius; x < width - edge_patch_radius; x++)
		        {
		            // total pixel intensity to the left
		            int left = sliding_sum[x] - sliding_sum[x - edge_patch_radius];
		            
		            // total pixel intensity to the right
		            int right = sliding_sum[x + edge_patch_radius] - sliding_sum[x];
		            
		            // update response
		            edge_response[x] = Math.Abs(left - right);
		            
		            // apply edge threshold
		            if (edge_response[x] < edge_threshold)
		                edge_response[x] = 0;
		        }
		        
		        // perform non-maximal supression
		        x = 0;		        
		        while (x < width)
		        {
		            int response = edge_response[x];
		            if (response > 0)
		            {
		                bool killed = false;
			            int xx = x + 1; // - horizontal_suppression_radius;
			            while ((xx < x + horizontal_suppression_radius) && (!killed))
			            {
			                if (xx < width)
			                {
	                            if (response >= edge_response[xx])
			                    {
			                        edge_response[xx] = 0;
			                    }
			                    else
			                    {
			                        edge_response[x] = 0;
			                        killed = true;
			                    }
			                }
			                xx++;
			            }
		            }
		            x++;
		        }
		        
		        // count the survivors and add them to the result
		        for (x = 0; x < width; x++)
		            if (edge_response[x] > 0)
		                vertical_edges[x, y] = true;
		    }
		    return(vertical_edges);
		}

        /// <summary>
        /// detect horizontally oriented edge features within a line grid pattern
        /// </summary>
		public static bool[,] detectHorizontalEdges(byte[] mono_image, int width, int height,
		                                            int vertical_suppression_radius,
		                                            int edge_patch_radius, int edge_threshold)
		{
		    // array which returns the results
		    bool[,] horizontal_edges = new bool[width, height];
		
		    // edge threshold should be proportional to the number of pixels being considered
            edge_threshold *= edge_patch_radius;		    
		
		    // this array stores the absolute magnitude of edge responses
		    int[] edge_response = new int[height];
		    
		    // this array stores sliding sums of pixel intensities for each row
		    // of the image
		    int[] sliding_sum = new int[height];
		
		    for (int x = 0; x < width; x++)
		    {
		        int y = 0;
		        
		        // calc sliding sum for this row
		        sliding_sum[0] = mono_image[x];
		        for (y = 1; y < height; y++)
		            sliding_sum[y] = sliding_sum[y-1] + mono_image[(y*width)+x];  
		            
		        // calc edge responses
		        for (y = edge_patch_radius; y < height - edge_patch_radius; y++)
		        {
		            // total pixel intensity to the left
		            int above = sliding_sum[y] - sliding_sum[y - edge_patch_radius];
		            
		            // total pixel intensity to the right
		            int below = sliding_sum[y + edge_patch_radius] - sliding_sum[y];
		            
		            // update response
		            edge_response[y] = Math.Abs(above - below);
		            
		            // apply edge threshold
		            if (edge_response[y] < edge_threshold)
		                edge_response[y] = 0;
		        }
		        
		        // perform non-maximal supression
		        y = 0;		        
		        while (y < height)
		        {
		            int response = edge_response[y];
		            if (response > 0)
		            {
		                bool killed = false;
			            int yy = y + 1;
			            while ((yy < y + vertical_suppression_radius) && (!killed))
			            {
			                if (yy < height)
			                {
		                        if (response >= edge_response[yy])
				                {
				                    edge_response[yy] = 0;
				                }
				                else
				                {
				                    edge_response[y] = 0;
				                    killed = true;
				                }
			                }
			                yy++;
			            }
		            }
		            y++;
		        }
		        
		        // count the survivors and add them to the result
		        for (y = 0; y < height; y++)
		            if (edge_response[y] > 0)
		                horizontal_edges[x, y] = true;
		    }
		    return(horizontal_edges);
		}
		

        #region "adaptive threshold / binarization"
        

        public static void binarizeAdaptive(byte[] bmp_mono,
                                            int image_width, int image_height,
                                            int patch_width, int patch_height,
                                            float gain, 
                                            int image_index,
                                            bool black_on_white,
                                            List<float> precomputed_params,
                                            List<bool[]> precomputed_binary_images,
                                            int[] Integral, long[] IntegralSquared,
                                            ref bool[] binary_image,
                                            bool invert)
        {
            bool precomputed = false;
            float bw = 0;
            if (black_on_white) bw = 1;

            // has this image already been computed
            // for the given patch size and gain value ?            
            if (precomputed_params != null)
            {
                if (precomputed_params.Count > 0)
                {
                    int i = 0;
                    while ((i < precomputed_params.Count) && (!precomputed))
                    {
                        if (precomputed_params[i] == image_index)
                            if (precomputed_params[i + 1] == bw)
                                if (precomputed_params[i + 2] == patch_width)
                                    if (precomputed_params[i + 3] == patch_height)
                                        if (precomputed_params[i + 4] == gain)
                                        {
                                            // use the precomputed binary image
                                            binary_image = precomputed_binary_images[i / 5];
                                            precomputed = true;
                                            //Console.WriteLine("precomputed");
                                        }
                        i += 5;
                    }
                }
            }             

            if (!precomputed)
            {
                // create a new binary image
                binarizeAdaptive(bmp_mono, image_width, image_height, patch_width,
                                 patch_height, gain, 
                                 Integral, IntegralSquared, 
                                 binary_image, invert);
                
                // add the parameters to the list
                precomputed_params.Add((float)image_index);
                precomputed_params.Add(bw);
                precomputed_params.Add((float)patch_width);
                precomputed_params.Add((float)patch_height);
                precomputed_params.Add(gain);

                // add the binary image to the list of precomputed images
                precomputed_binary_images.Add((bool[])binary_image.Clone());                
            }
        }

        /// <summary>
        /// computes a binary image using adaptive thresholding
        /// method based upon "Efficient Implementation of Local Adaptive
        /// Thresholding Techniques Using Integral Images" by 
        /// Faisal Shafait1, Daniel Keysers1 and Thomas M. Breuel2
        /// </summary>
        /// <param name="bmp">mono image to be thresholded</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="patch_width">width of patches used to compute means and standard deviations</param>
        /// <param name="patch_height">height of patches used to compute means and standard deviations</param>
        /// <param name="gain">threshold gain in the range 0.0 - 1.0</param>
        /// <param name="Integral">array to be used to store the integral image</param>
        /// <param name="IntegralSquared">array to be used to store the squared integral image</param>
        /// <param name="binary_image">the output binary image</param>
        /// <param name="invert">invert the binary image output</param>
        public static unsafe void binarizeAdaptive(byte[] bmp_mono,
                                                   int image_width, int image_height,
                                                   int patch_width, int patch_height,
                                                   float gain,
                                                   int[] Integral, long[] IntegralSquared,                                            
                                                   bool[] binary_image,
                                                   bool invert)
        {
            double mean, mean_sqr, std_deviation_squared, threshold;
            int x,y, ia, ib, ic, id;
            int tx, ty, bx, by, w, h;
            
            // compute the integral image and squared integral image
            // in a single pass
            updateIntegralImageSquared(bmp_mono, image_width, image_height, Integral, IntegralSquared);

			int half_patch_width = patch_width / 2;
            int half_patch_height = patch_height / 2;

            // convert gain into the range 0.0 - 0.5
            double threshold_gain = gain * 0.5;

            double pixels, pixels_inverse;
            double halfrange_inverse = 1.0 / (128 * 128);
            double patch_pixels = patch_width * patch_height;
            double patch_pixels_inverse = 1.0 / patch_pixels;

            fixed (bool* binary_image_unsafe = binary_image)
            {
                fixed (byte* bmp_mono_unsafe = bmp_mono)
                {
                    fixed (int* integral_unsafe = Integral)
                    {            
                        fixed (long* integral_squared_unsafe = IntegralSquared)
                        {            
                            int n = 0;
                            for (y = 0; y < image_height; y++)
                            {
                                ty = y - half_patch_height;
                                if (ty < 0) ty = 0;
                                by = ty + patch_height;
                                if (by >= image_height) by = image_height - 1;
                                h = by - ty;
                                
                                int n1 = ty * image_width;
                                int n2 = by * image_width;

                                int tx1 =  -half_patch_width;
                                int bx1 = patch_width;
                                
                                for (x = 0; x < image_width; x++)
                                {
                                    tx = tx1;
                                    if (tx < 0) tx = 0;
                                    bx = bx1;
                                    if (bx >= image_width) bx = image_width - 1;
                                    w = bx - tx;
                                    
                                    tx1++;
                                    bx1++;

                                    // number of pixels within the patch
                                    if ((w == patch_width) && (h == patch_height))
                                    {
                                        pixels = patch_pixels;
                                        pixels_inverse = patch_pixels_inverse;
                                    }
                                    else
                                    {
                                        pixels = w * h;
                                        pixels_inverse = 1.0 / pixels;
                                    }
                                                                            
                                    ia = n2 + bx;
                                    ib = n1 + tx;
                                    ic = n2 + tx;
                                    id = n1 + bx;
                                
                                    // compute local mean
                                    mean = (integral_unsafe[ia] + integral_unsafe[ib] - (integral_unsafe[ic] + integral_unsafe[id])) * pixels_inverse;
                                    
                                    // compute local variance
                                    mean_sqr = (integral_squared_unsafe[ia] + integral_squared_unsafe[ib] - 
                                                (integral_squared_unsafe[ic] + integral_squared_unsafe[id])) * pixels_inverse;
                                    std_deviation_squared = mean_sqr - (mean * mean);

                                    // compute local threshold
                                    threshold = mean * (1 + (threshold_gain * ((std_deviation_squared * halfrange_inverse) - 1)));

                                    if (bmp_mono_unsafe[n] >= threshold)
                                        binary_image_unsafe[n] = !invert;
                                    else
                                        binary_image_unsafe[n] = invert;

                                    n++;
                                }
                            }
                        }
                    }
                }
            }
        }
        
				
        /// <summary>
        /// computes a binary image using adaptive thresholding
        /// method based upon "Efficient Implementation of Local Adaptive
        /// Thresholding Techniques Using Integral Images" by 
        /// Faisal Shafait1, Daniel Keysers1 and Thomas M. Breuel2
        /// </summary>
        /// <param name="bmp">mono image to be thresholded</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="patch_width">width of patches used to compute means and standard deviations</param>
        /// <param name="patch_height">height of patches used to compute means and standard deviations</param>
        /// <param name="gain">threshold gain in the range 0.0 - 1.0</param>
        /// <param name="Integral">array to be used to store the integral image</param>
        /// <param name="IntegralSquared">array to be used to store the squared integral image</param>
        /// <param name="binary_image">the output binary image</param>
        /// <param name="invert">invert the binary image output</param>
        public static void binarizeAdaptive(float[] bmp_mono,
                                            int image_width, int image_height,
                                            int patch_width, int patch_height,
                                            float gain,
                                            float[] Integral, float[] IntegralSquared,
                                            byte[] binary_image,
                                            bool invert)
        {
            // compute the integral image
            updateIntegralImage(bmp_mono, image_width, image_height, Integral);

            // compute the squared integral image
            updateIntegralImageSquared(bmp_mono, image_width, image_height, IntegralSquared);

            int half_patch_width = patch_width / 2;
            int half_patch_height = patch_height / 2;

            // convert gain into the range 0.0 - 0.5
            double threshold_gain = gain * 0.5;

            int n = 0;
            for (int y = 0; y < image_height; y++)
            {
                int ty = y - half_patch_height;
                if (ty < 0) ty = 0;
                int by = ty + patch_height;
                if (by >= image_height) by = image_height - 1;
                int w = by - ty;
                for (int x = 0; x < image_width; x++)
                {
                    int tx = x - half_patch_width;
                    if (tx < 0) tx = 0;
                    int bx = tx + patch_width;
                    if (bx >= image_width) bx = image_width - 1;
                    int h = bx - tx;

                    // number of pixels within the patch
                    double pixels = w * h;

                    // compute local mean
                    double mean = getIntegral(Integral, tx, ty, bx, by, image_width) / pixels;

                    // compute local variance
                    double mean_sqr = getIntegral(IntegralSquared, tx, ty, bx, by, image_width) / pixels;
                    double std_deviation_squared = mean_sqr - (mean * mean);

                    // compute local threshold
                    //double threshold = mean * (1 + (threshold_gain * ((Math.Sqrt(std_deviation_squared) / 128) - 1)));
                    double threshold = mean * (1 + (threshold_gain * ((std_deviation_squared / (128 * 128)) - 1)));

                    float intensity = bmp_mono[n];
                    if (intensity >= threshold)
                    {
                        if (!invert)
                            binary_image[n] = 255;
                        else
                            binary_image[n] = 0;
                    }
                    else
                    {
                        if (!invert)
                            binary_image[n] = 0;
                        else
                            binary_image[n] = 255;
                    }
                    n++;
                }
            }
        }

        public static unsafe void Test_BinarizeAdaptiveWithContrast(string filename, string output_filename,
                                                                    int patch_width,
                                                                    float gain0, float gain1, float gain2,
                                                                    bool invert)
        {
            Bitmap bmp = new Bitmap(filename);
            byte[] bmp_data = new byte[bmp.Width * bmp.Height * 3];            
            BitmapArrayConversions.updatebitmap(bmp, bmp_data);
            byte[] bmp_data_mono = monoImage(bmp_data, bmp.Width, bmp.Height, null);
            int[] Integral = new int[bmp.Width * bmp.Height];            
            long[] IntegralSquared = new long[bmp.Width * bmp.Height];
            bool[] binary_image = new bool[bmp.Width * bmp.Height];
            binarizeAdaptiveWithContrast(bmp_data_mono, bmp.Width, bmp.Height, patch_width, patch_width,
                                         gain0, gain1, gain2, Integral, IntegralSquared, binary_image, invert);
            int n = 0;
            byte v = 0;
            for (int i = 0; i < binary_image.Length; i++)
            {
                if (binary_image[i]) v = 255; else v = 0;
                bmp_data[n++] = v;
                bmp_data[n++] = v;
                bmp_data[n++] = v;
            }
            BitmapArrayConversions.updatebitmap_unsafe(bmp_data, bmp);
            bmp.Save(output_filename);
        }

        public static unsafe void Test_BinarizeAdaptive(string filename, string output_filename,
                                                        int patch_width,
                                                        float gain, 
                                                        bool invert)
        {
            Bitmap bmp = new Bitmap(filename);
            byte[] bmp_data = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, bmp_data);
            byte[] bmp_data_mono = monoImage(bmp_data, bmp.Width, bmp.Height, null);
            int[] Integral = new int[bmp.Width * bmp.Height];
            long[] IntegralSquared = new long[bmp.Width * bmp.Height];
            bool[] binary_image = new bool[bmp.Width * bmp.Height];
            binarizeAdaptive(bmp_data_mono, bmp.Width, bmp.Height, patch_width, patch_width,
                             gain, Integral, IntegralSquared, binary_image, invert);
            int n = 0;
            byte v = 0;
            for (int i = 0; i < binary_image.Length; i++)
            {
                if (binary_image[i]) v = 255; else v = 0;
                bmp_data[n++] = v;
                bmp_data[n++] = v;
                bmp_data[n++] = v;
            }
            BitmapArrayConversions.updatebitmap_unsafe(bmp_data, bmp);
            bmp.Save(output_filename);
        }


        /// <summary>
        /// computes a binary image using adaptive thresholding
        /// method based upon "contrast adaptive binarization of low quality document images"
        /// by Meng-Ling Feng and Tap-Peng Tan
        /// </summary>
        /// <param name="bmp">mono image to be thresholded</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="patch_width">width of patches used to compute means and standard deviations</param>
        /// <param name="patch_height">height of patches used to compute means and standard deviations</param>
        /// <param name="gain0">the first weighting parameter (local average pixel value)</param>
        /// <param name="gain1">the second weighting parameter (contrast ratio)</param>
        /// <param name="gain2">the third weighting parameter (image minimum pixel value)</param>
        /// <param name="Integral">array to be used to store the integral image</param>
        /// <param name="IntegralSquared">array to be used to store the squared integral image</param>
        /// <param name="binary_image">the output binary image</param>
        /// <param name="invert">invert the binary image output</param>
        public static unsafe void binarizeAdaptiveWithContrast(byte[] bmp_mono,
                                                   int image_width, int image_height,
                                                   int patch_width, int patch_height,
                                                   float gain0, float gain1, float gain2,
                                                   int[] Integral, long[] IntegralSquared,
                                                   bool[] binary_image,
                                                   bool invert)
        {
            double mean, mean_sqr, std_deviation_squared;
            double mean2, mean_sqr2, std_deviation_squared2;
            double threshold;
            int min;
            int x, y;
            int ia, ib, ic, id;
            int ia2, ib2, ic2, id2;
            int tx, ty, bx, by, w, h;
            int tx2, ty2, bx2, by2, w2, h2;

            // compute the integral image and squared integral image
            // in a single pass
            updateIntegralImageSquared(bmp_mono, image_width, image_height, Integral, IntegralSquared);

            int half_patch_width = patch_width / 2;
            int half_patch_height = patch_height / 2;

            double pixels, pixels_inverse;
            double pixels2, pixels_inverse2;
            double patch_pixels = patch_width * patch_height;
            double patch_pixels2 = patch_pixels * 4;
            double patch_pixels_inverse = 1.0 / patch_pixels;
            double patch_pixels_inverse2 = 1.0 / patch_pixels2;

            // find the minimum intensity within the image
            min = 255*2;
            for (int i = 0; i < bmp_mono.Length-1; i++)
            {
                int v = bmp_mono[i] + bmp_mono[i + 1];
                if (v < min) min = v;
            }
            min /= 2;

            float inverse_gain0 = 1.0f - gain0;

            fixed (bool* binary_image_unsafe = binary_image)
            {
                fixed (byte* bmp_mono_unsafe = bmp_mono)
                {
                    fixed (int* integral_unsafe = Integral)
                    {
                        fixed (long* integral_squared_unsafe = IntegralSquared)
                        {
                            int n = 0;
                            for (y = 0; y < image_height; y++)
                            {
                                ty = y - half_patch_height;
                                ty2 = y - patch_height;
                                if (ty < 0) ty = 0;
                                if (ty2 < 0) ty2 = 0;
                                by = ty + patch_height;
                                by2 = ty + (patch_height*2);
                                if (by >= image_height) by = image_height - 1;
                                if (by2 >= image_height) by2 = image_height - 1;
                                h = by - ty;
                                h2 = by2 - ty2;

                                int n1 = ty * image_width;
                                int n2 = by * image_width;
                                int n1b = ty2 * image_width;
                                int n2b = by2 * image_width;

                                int tx1 = -half_patch_width;
                                int bx1 = patch_width;
                                int tx1b = -patch_width;
                                int bx1b = patch_width*2;

                                for (x = 0; x < image_width; x++)
                                {
                                    tx = tx1;
                                    if (tx < 0) tx = 0;
                                    bx = bx1;
                                    if (bx >= image_width) bx = image_width - 1;
                                    w = bx - tx;

                                    tx2 = tx1b;
                                    if (tx2 < 0) tx2 = 0;
                                    bx2 = bx1b;
                                    if (bx2 >= image_width) bx2 = image_width - 1;
                                    w2 = bx2 - tx2;

                                    tx1++;
                                    bx1++;
                                    tx2++;
                                    bx2++;

                                    // number of pixels within the patch
                                    if ((w == patch_width) && (h == patch_height))
                                    {
                                        pixels = patch_pixels;
                                        pixels_inverse = patch_pixels_inverse;
                                    }
                                    else
                                    {
                                        pixels = w * h;
                                        pixels_inverse = 1.0 / pixels;
                                    }

                                    // number of pixels within the patch
                                    if ((w2 == patch_width) && (h2 == patch_height))
                                    {
                                        pixels2 = patch_pixels2;
                                        pixels_inverse2 = patch_pixels_inverse2;
                                    }
                                    else
                                    {
                                        pixels2 = w2 * h2;
                                        pixels_inverse2 = 1.0 / pixels2;
                                    }


                                    ia = n2 + bx;
                                    ib = n1 + tx;
                                    ic = n2 + tx;
                                    id = n1 + bx;

                                    ia2 = n2b + bx2;
                                    ib2 = n1b + tx2;
                                    ic2 = n2b + tx2;
                                    id2 = n1b + bx2;

                                    // compute local means
                                    mean = (integral_unsafe[ia] + integral_unsafe[ib] - (integral_unsafe[ic] + integral_unsafe[id])) * pixels_inverse;
                                    mean2 = (integral_unsafe[ia2] + integral_unsafe[ib2] - (integral_unsafe[ic2] + integral_unsafe[id2])) * pixels_inverse2;

                                    // compute local variances
                                    mean_sqr = (integral_squared_unsafe[ia] + integral_squared_unsafe[ib] -
                                                (integral_squared_unsafe[ic] + integral_squared_unsafe[id])) * pixels_inverse;
                                    mean_sqr2 = (integral_squared_unsafe[ia2] + integral_squared_unsafe[ib2] -
                                                 (integral_squared_unsafe[ic2] + integral_squared_unsafe[id2])) * pixels_inverse2;
                                    std_deviation_squared = mean_sqr - (mean * mean);
                                    std_deviation_squared2 = mean_sqr2 - (mean2 * mean2);

                                    // compute local threshold
                                    threshold = (inverse_gain0 * mean) +  // local average
                                                (gain2 * min);            // minimum intensity
                                    if (std_deviation_squared2 > 0)       // contrast variance
                                        threshold += (gain1 * std_deviation_squared / std_deviation_squared2 * (mean - min));

                                    if (bmp_mono_unsafe[n] >= threshold)
                                        binary_image_unsafe[n] = !invert;
                                    else
                                        binary_image_unsafe[n] = invert;

                                    n++;
                                }
                            }
                        }
                    }
                }
            }
        }


        #endregion

        #region "integral image"

        /// <summary>
        /// update the integral image, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImage(byte[] bmp,
                                                      int image_width, int image_height,
                                                      int[] Integral)
        {
            int x, y, p, n = image_width;

            fixed (byte* unsafe_bmp = bmp)
            {
                fixed (int* unsafe_Integral = Integral)
                {
                    for (y = 1; y < image_height; y++)
                    {
                        p = 0;
                        for (x = 0; x < image_width; x++)
                        {
                            p += unsafe_bmp[n];
                            unsafe_Integral[n] = p + unsafe_Integral[n - image_width];
                            n++;
                        }
                    }
                }
            }        
        }

        /// <summary>
        /// update the integral image, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImage(float[] bmp,
                                               int image_width, int image_height,
                                               float[] Integral)
        {
            int x, y, n = image_width;
            float p;

            fixed (float* unsafe_bmp = bmp)
            {
                fixed (float* unsafe_Integral = Integral)
                {
                    for (y = 1; y < image_height; y++)
                    {
                        p = 0;
                        for (x = 0; x < image_width; x++)
                        {
                            p += unsafe_bmp[n];
                            unsafe_Integral[n] = p + unsafe_Integral[n - image_width];
                            n++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// update the integral image, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImage(byte[] bmp,
                                                      int image_width, int image_height,
                                                      long[] Integral)
        {
            int x, y, p, n = image_width;

            fixed (byte* unsafe_bmp = bmp)
            {
                fixed (long* unsafe_Integral = Integral)
                {
                    for (y = 1; y < image_height; y++)
                    {
                        p = 0;
                        for (x = 0; x < image_width; x++)
                        {
                            p += unsafe_bmp[n];
                            unsafe_Integral[n] = p + unsafe_Integral[n - image_width];
                            n++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// update the integral image of squared pixel intensities, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImageSquared(byte[] bmp,
                                                      int image_width, int image_height,
                                                      long[] Integral)
        {
            int x, y, n = image_width;
            long p;

            fixed (byte* unsafe_bmp = bmp)
            {
                fixed (long* unsafe_Integral = Integral)
                {
                    for (y = 1; y < image_height; y++)
                    {
                        p = 0;
                        for (x = 0; x < image_width; x++)
                        {
                            p += (unsafe_bmp[n] * unsafe_bmp[n]);
                            unsafe_Integral[n] = p + unsafe_Integral[n - image_width];
                            n++;
                        }
                    }
                }
            }
        }

        
        /// <summary>
        /// update the regular and squared integral image of intensities
        /// at the same time, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImageSquared(byte[] bmp,
                                                             int image_width, int image_height,
                                                             long[] Integral, long[] IntegralSquared)
        {
            int x, y, n = image_width, n2 = 0;
            long p, p_sqr;
            byte b;

            fixed (byte* unsafe_bmp = bmp)
            {
                fixed (long* unsafe_Integral = Integral)
                {
                    fixed (long* unsafe_IntegralSquared = IntegralSquared)
                    {
                        for (y = 1; y < image_height; y++)
                        {
                            p = p_sqr = 0;
                            for (x = 0; x < image_width; x++)
                            {
                                b = unsafe_bmp[n];
                                p += b;
                                p_sqr += b*b;
                                unsafe_Integral[n] = p + unsafe_Integral[n2];
                                unsafe_IntegralSquared[n] = p_sqr + unsafe_IntegralSquared[n2];
                                n++;
                                n2++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// update the regular and squared integral image of intensities
        /// at the same time, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImageSquared(byte[] bmp,
                                                             int image_width, int image_height,
                                                             int[] Integral, long[] IntegralSquared)
        {
            int x, y, p, n = image_width, n2 = 0;
            long p_sqr;
            byte b;

            fixed (byte* unsafe_bmp = bmp)
            {
                fixed (int* unsafe_Integral = Integral)
                {
                    fixed (long* unsafe_IntegralSquared = IntegralSquared)
                    {
                        for (y = 1; y < image_height; y++)
                        {
                            p_sqr = 0;
                            p = 0;
                            for (x = 0; x < image_width; x++)
                            {
                                b = unsafe_bmp[n];
                                p += b;
                                p_sqr += b*b;
                                unsafe_Integral[n] = p + unsafe_Integral[n2];
                                unsafe_IntegralSquared[n] = p_sqr + unsafe_IntegralSquared[n2];
                                n++;
                                n2++;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// update the integral image of squared pixel intensities, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static unsafe void updateIntegralImageSquared(float[] bmp,
                                                      int image_width, int image_height,
                                                      float[] Integral)
        {
            int x, y, n = image_width;
            float p;

            fixed (float* unsafe_bmp = bmp)
            {
                fixed (float* unsafe_Integral = Integral)
                {
                    for (y = 1; y < image_height; y++)
                    {
                        p = 0;
                        for (x = 0; x < image_width; x++)
                        {
                            p += (unsafe_bmp[n] * unsafe_bmp[n]);
                            unsafe_Integral[n] = p + unsafe_Integral[n - image_width];
                            n++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// update the integral image, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>       
        public static long[] updateIntegralImage(byte[] bmp,
                                                 int image_width, int image_height)
        {
            long[] Integral = new long[image_width * image_height];
            updateIntegralImage(bmp, image_width, image_height, Integral);
            return (Integral);
        }

        /// <summary>
        /// update the integral image, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>       
        public static float[] updateIntegralImage(float[] bmp,
                                                  int image_width, int image_height)
        {
            float[] Integral = new float[image_width * image_height];
            updateIntegralImage(bmp, image_width, image_height, Integral);
            return (Integral);
        }

        /// <summary>
        /// update the integral image of squared pixel intensity values, using the given mono bitmap
        /// </summary>
        /// <param name="bmp">mono image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>       
        public static long[] updateIntegralImageSquared(byte[] bmp,
                                                        int image_width, int image_height)
        {
            long[] Integral = new long[image_width * image_height];
            updateIntegralImageSquared(bmp, image_width, image_height, Integral);
            return (Integral);
        }

        /// <summary>
        /// update the integral image, using the given colour bitmap
        /// </summary>
        /// <param name="bmp">colour image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static void updateIntegralImageColour(byte[] bmp,
                                                     int image_width, int image_height,
                                                     long[, ,] Integral)
        {
            int x, y, n = image_width * 3;
            int[] p = new int[3];

            for (y = 1; y < image_height; y++)
            {
                p[0] = 0;
                p[1] = 0;
                p[2] = 0;
                for (x = 0; x < image_width; x++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        p[col] += bmp[n];
                        Integral[x, y, col] = p[col] + Integral[x, y - 1, col];
                        n++;
                    }
                }
            }
        }

        /// <summary>
        /// update the integral image, using the given colour bitmap
        /// </summary>
        /// <param name="bmp">colour image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static void updateIntegralImageColour(float[] bmp,
                                                     int image_width, int image_height,
                                                     float[, ,] Integral)
        {
            int x, y, n = image_width * 3;
            float[] p = new float[3];

            for (y = 1; y < image_height; y++)
            {
                p[0] = 0;
                p[1] = 0;
                p[2] = 0;
                for (x = 0; x < image_width; x++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        p[col] += bmp[n];
                        Integral[x, y, col] = p[col] + Integral[x, y - 1, col];
                        n++;
                    }
                }
            }
        }

        /// <summary>
        /// update the integral image, using the given colour bitmap
        /// </summary>
        /// <param name="bmp">colour image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="Integral">integral image</param>
        public static void updateIntegralImageColour(byte[] bmp,
                                                     int image_width, int image_height,
                                                     int[,] Integral)
        {
            int x, y, n = image_width;
            int[] p = new int[3];
            int w = image_width;

            for (y = 1; y < image_height; y++)
            {
                p[0] = 0;
                p[1] = 0;
                p[2] = 0;
                for (x = 0; x < image_width; x++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        p[col] += bmp[(n*3)+col];
                        Integral[n, col] = p[col] + Integral[n - w, col];                        
                    }
                    n++;
                }
            }
        }

        /// <summary>
        /// update the integral image, using the given colour bitmap
        /// </summary>
        /// <param name="bmp">colour image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        public static long[, ,] updateIntegralImageColour(byte[] bmp, int image_width, int image_height)
        {
            long[, ,] Integral = new long[image_width, image_height, 3];
            updateIntegralImageColour(bmp, image_width, image_height, Integral);
            return (Integral);
        }

        /// <summary>
        /// update the integral image, using the given colour bitmap
        /// </summary>
        /// <param name="bmp">colour image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        public static float[, ,] updateIntegralImageColour(float[] bmp, int image_width, int image_height)
        {
            float[, ,] Integral = new float[image_width, image_height, 3];
            updateIntegralImageColour(bmp, image_width, image_height, Integral);
            return (Integral);
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static unsafe long getIntegral(long[,] Integral, int tx, int ty, int bx, int by)
        {
            return (Integral[bx, by] + Integral[tx, ty] - (Integral[tx, by] + Integral[bx, ty]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <param name="image_width">width of the integral image</param>
        /// <returns>summed pixel value for the area</returns>
        public static long getIntegral(long[] Integral, int tx, int ty, int bx, int by, int image_width)
        {
            int n1 = ty * image_width;
            int n2 = by * image_width;
            return (Integral[n2 + bx] + Integral[n1 + tx] - (Integral[n2 + tx] + Integral[n1 + bx]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static int getIntegral(int[,] Integral, int tx, int ty, int bx, int by)
        {
            return (Integral[bx, by] + Integral[tx, ty] - (Integral[tx, by] + Integral[bx, ty]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static unsafe int getIntegral(int[] Integral, int tx, int ty, int bx, int by, int image_width)
        {
            fixed (int* unsafe_Integral = Integral)
            {
                int n1 = ty * image_width;
                int n2 = by * image_width;
                return (unsafe_Integral[n2 + bx] + 
                        unsafe_Integral[n1 + tx] - 
                        (unsafe_Integral[n2 + tx] + unsafe_Integral[n1 + bx]));
            }
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static float getIntegral(float[] Integral, int tx, int ty, int bx, int by, int image_width)
        {
            int n1 = ty * image_width;
            int n2 = by * image_width;
            return (Integral[n2 + bx] + Integral[n1 + tx] - (Integral[n2 + tx] + Integral[n1 + bx]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static int getIntegral(int[,] Integral, int tx, int ty, int bx, int by, int image_width, int col)
        {
            int n1 = ty * image_width;
            int n2 = by * image_width;
            return (Integral[n2 + bx, col] + Integral[n1 + tx, col] - (Integral[n2 + tx, col] + Integral[n1 + bx, col]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static float getIntegral(float[,] Integral, int tx, int ty, int bx, int by, int image_width, int col)
        {
            int n1 = ty * image_width;
            int n2 = by * image_width;
            return (Integral[n2 + bx, col] + Integral[n1 + tx, col] - (Integral[n2 + tx, col] + Integral[n1 + bx, col]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static long getIntegral(long[, ,] Integral, int tx, int ty, int bx, int by, int col)
        {
            return (Integral[bx, by, col] + Integral[tx, ty, col] - (Integral[tx, by, col] + Integral[bx, ty, col]));
        }

        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx">top left x coordinate</param>
        /// <param name="ty">top left y coordinate</param>
        /// <param name="bx">bottom right x coordinate</param>
        /// <param name="by">bottom right y coordinate</param>
        /// <returns>summed pixel value for the area</returns>
        public static float getIntegral(float[, ,] Integral, int tx, int ty, int bx, int by, int col)
        {
            return (Integral[bx, by, col] + Integral[tx, ty, col] - (Integral[tx, by, col] + Integral[bx, ty, col]));
        }

        #endregion

        #region "image smoothing"

        public static unsafe byte[] BlurMono(byte[] img_mono, int img_width, int img_height,
                                             int blur_radius)
        {
            byte[] img = (byte[])img_mono.Clone();
            byte[] img_blurred = new byte[img_width * img_height];
            
            fixed (byte* img_unsafe = img)
            {
                fixed (byte* img_blurred_unsafe = img_blurred)
                {         
                    for (int r = 0; r < blur_radius; r++)
                    {
                        // horizontal convolution
                        byte b = img_unsafe[0];
                        int conv, i;
                        for (i = 1; i < img.Length-1; i++)
                        {
                            conv = b + (img_unsafe[i]*2) + img_unsafe[i + 1];
                            b = img_unsafe[i];
                            img_blurred_unsafe[i] = (byte)(conv * 0.1f);
                        }
                        // vertical convolution
                        for (int x = 0; x < img_width; x++)
                        {
                            i = img_width + x;
                            b = img_unsafe[x];
                            for (int y = 1; y < img_height-1; y++)
                            {
                                conv = b + (img_unsafe[i]*2) + img_unsafe[i + img_width];
                                b = img_unsafe[i];
                                img_blurred_unsafe[i] += (byte)(conv * 0.1f);
                                i += img_width;
                            }
                        }
                        
                        if (r < blur_radius - 1)
                        {
                            byte* dst = img_unsafe;
                            byte* src = img_blurred_unsafe;
                            for (i = 0; i < img.Length; i++) *dst++ = *src++;
                        }
                        
                    }
                }
            }
            return(img_blurred);
        }

/*        
        public static byte[] BlurMono(byte[] img_mono, int img_width, int img_height,
                                      int blur_radius)
        {
            int pixels = img_mono.Length;
            byte[] img = (byte[])img_mono.Clone();
            byte[] img_blurred = new byte[pixels];
            int n, tx, bx;
            int hits, w, v = 0;
            
            for (int itt = 0; itt < blur_radius; itt++)
            {
                for (int i = pixels-1; i >= 0; i--)
                {
                    v = 0;
                    hits = 0;
                    n = i - img_width - 1;                
                    for (int j = 0; j < 3; j++)
                    {
                        tx = n;
                        bx = tx + 3;
                        if (tx < 0) tx = 0;
                        if (bx >= pixels) bx = pixels-1;
                        for (int x = tx; x < bx; x++)
                        {
                            if (x != i)
                                w = 5;
                            else
                                w = 10;
                            v += img[x] * w;
                            hits += w;
                        }
                        n += img_width;
                    }
                    v /= hits;
                    img_blurred[i] = (byte)v;
                }
                if (itt < blur_radius - 1)
                    img = (byte[])img_blurred.Clone();
            }
            return(img_blurred);
        }
*/
        
        public static byte[] smoothCentreSurround(byte[] img, int img_width, int img_height,
                                                  int surround_radius)
        {
            int hits = 0;
            float max_response = 0;
            float min_response = 9999;
            float average_response = 0;
            float[,] response = new float[img_width, img_height];
            byte[] centresurround = null;
            bool isColour = true;
            if (img.Length == img_width * img_height) isColour = false;

            if (!isColour)
            {
                centresurround = new byte[img_width * img_height];
                int[] integral_img = new int[img_width * img_height];
                updateIntegralImage(img, img_width, img_height, integral_img);

                int diameter = surround_radius * 2;
                int centre_radius = surround_radius / 2;
                int tx, ty, bx, by;
                int tx_centre, ty_centre, bx_centre, by_centre;
                float centre_pixels = centre_radius * centre_radius * 4;
                float surround_pixels = surround_radius * surround_radius * 4;
                for (int x = surround_radius; x < img_width - surround_radius; x++)
                {
                    tx = x - surround_radius;
                    bx = tx + diameter;
                    tx_centre = tx + centre_radius;
                    bx_centre = tx_centre + surround_radius;

                    for (int y = surround_radius; y < img_height - surround_radius; y++)
                    {
                        ty = y - surround_radius;
                        by = ty + diameter;
                        ty_centre = ty + centre_radius;
                        by_centre = ty_centre + surround_radius;

                        float average_surround = image.getIntegral(integral_img, tx, ty, bx, by, img_width);
                        float average_centre = image.getIntegral(integral_img, tx_centre, ty_centre, bx_centre, by_centre, img_width);
                        average_surround -= average_centre;
                        average_surround /= surround_pixels;
                        average_centre /= centre_pixels;

                        float r = average_centre - average_surround;
                        if (r < min_response) min_response = r;
                        if (r > max_response) max_response = r;
                        response[x, y] = r;
                        average_response += Math.Abs(r);
                        hits++;
                    }
                }
            }
            else
            {
                centresurround = new byte[img_width * img_height * 3];
                int[,] integral_img = new int[img_width * img_height, 3];
                updateIntegralImageColour(img, img_width, img_height, integral_img);

                int diameter = surround_radius * 2;
                int centre_radius = surround_radius / 2;
                int tx, ty, bx, by;
                int tx_centre, ty_centre, bx_centre, by_centre;
                float centre_pixels = centre_radius * centre_radius * 4;
                float surround_pixels = surround_radius * surround_radius * 4;
                for (int x = surround_radius; x < img_width - surround_radius; x++)
                {
                    tx = x - surround_radius;
                    bx = tx + diameter;
                    tx_centre = tx + centre_radius;
                    bx_centre = tx_centre + surround_radius;

                    for (int y = surround_radius; y < img_height - surround_radius; y++)
                    {
                        ty = y - surround_radius;
                        by = ty + diameter;
                        ty_centre = ty + centre_radius;
                        by_centre = ty_centre + surround_radius;

                        float average_surround = image.getIntegral(integral_img, tx, ty, bx, by, img_width, 0);
                        float average_centre = image.getIntegral(integral_img, tx_centre, ty_centre, bx_centre, by_centre, img_width, 0);
                        average_surround -= average_centre;
                        average_surround /= surround_pixels;
                        average_centre /= centre_pixels;

                        float r = average_centre - average_surround;
                        r *= r;
                        if (r < min_response) min_response = r;
                        if (r > max_response) max_response = r;
                        response[x, y] = r;
                        average_response += Math.Abs(r);
                        hits++;
                    }
                }
            }

            average_response /= hits;

            for (int x = surround_radius; x < img_width - surround_radius; x++)
            {
                for (int y = surround_radius; y < img_height - surround_radius; y++)
                {
                    float magnitude = Math.Abs(response[x, y] / average_response) * 2;
                    if (magnitude > 1) magnitude = 1;

                    int n = (y * img_width) + x;

                    if (isColour)
                    {
                        n *= 3;

                        centresurround[n] = (byte)(200 * magnitude);
                        centresurround[n + 1] = (byte)(200 * magnitude);
                        centresurround[n + 2] = (byte)(200 * magnitude);
                    }
                    else
                    {
                        centresurround[n] = (byte)(200 * magnitude);
                    }
                }
            }

            return (centresurround);
        }

        public static byte[] despeckleImageByAveraging(byte[] img, int img_width, int img_height,
                                                       int despeckling_radius, float tollerance)
        {
            byte[] despeckled = img;

            if (despeckling_radius > 0)
            {
                // number of pixels over which the smoothed result is averaged
                int despeckling_diameter = despeckling_radius * 2;
                int despeckling_pixels = despeckling_diameter * despeckling_diameter;

                if (img.Length == img_width * img_height)
                {
                    // this is a mono image

                    despeckled = new byte[img_width * img_height];
                    int[] integral_img = new int[img_width * img_height];
                    updateIntegralImage(img, img_width, img_height, integral_img);

                    for (int y = 0; y < img_height; y++)
                    {
                        int ty = y - despeckling_radius;
                        int by = ty + despeckling_diameter;

                        if (ty < 0) ty = 0;
                        if (by >= img_height) by = img_height - 1;

                        for (int x = 0; x < img_width; x++)
                        {
                            int tx = x - despeckling_radius;
                            int bx = tx + despeckling_diameter;

                            if (tx < 0) tx = 0;
                            if (bx >= img_width) bx = img_width - 1;

                            float local_average = (float)(getIntegral(integral_img, tx, ty, bx, by, img_width) / (float)despeckling_pixels);

                            int n = (y * img_width) + x;
                            float pixel_diff = Math.Abs(img[n] - local_average);
                            if (pixel_diff > tollerance)
                                despeckled[n] = (byte)local_average;
                            else
                                despeckled[n] = img[n];
                        }
                    }
                }
                else
                {
                    // this is a colour image
                    despeckled = new byte[img_width * img_height * 3];
                    long[, ,] integral_img = updateIntegralImageColour(img, img_width, img_height);

                    for (int y = 0; y < img_height; y++)
                    {
                        int ty = y- despeckling_radius;
                        int by = ty + despeckling_diameter;

                        if (ty < 0) ty = 0;
                        if (by >= img_height) by = img_height - 1;

                        for (int x = 0; x < img_width; x++)
                        {
                            int tx = x - despeckling_radius;
                            int bx = tx + despeckling_diameter;

                            if (tx < 0) tx = 0;
                            if (bx >= img_width) bx = img_width - 1;

                            for (int col = 0; col < 3; col++)
                            {
                                float local_average = (float)(getIntegral(integral_img, tx, ty, bx, by, col) / (float)despeckling_pixels);

                                int n = (((y * img_width) + x) * 3) + col;
                                float intensity = img[n];
                                float pixel_diff = Math.Abs(intensity - local_average);
                                if (pixel_diff > tollerance)
                                {
                                    despeckled[n] = (byte)local_average;
                                }
                                else
                                    despeckled[n] = img[n];
                            }
                        }
                    }
                }
            }


            return (despeckled);
        }

        /// <summary>
        /// returns a smoothed version of the given image by local averaging of pixel values
        /// </summary>
        /// <param name="img">image (can be mono or colour)</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="smoothing_radius">patch radius to use for local averaging</param>
        /// <returns>smoothed bitmap</returns>
        public static byte[] smoothImageByAveraging(byte[] img, int img_width, int img_height,
                                                    int smoothing_radius)
        {
            byte[] smoothed = img;

            if (smoothing_radius > 0)
            {
                // number of pixels over which the smoothed result is averaged
                int smoothing_diameter = smoothing_radius * 2;
                int smoothing_pixels = smoothing_diameter * smoothing_diameter;

                if (img.Length == img_width * img_height)
                {
                    // this is a mono image

                    smoothed = new byte[img_width * img_height];
                    long[] integral_img = updateIntegralImage(img, img_width, img_height);

                    int n = 0;
                    for (int y = 0; y < img_height; y++)
                    {
                        int ty = y - smoothing_radius;
                        int by = y + smoothing_radius;

                        if (ty < 0) ty = 0;
                        if (by >= img_height) by = img_height - 1;

                        for (int x = 0; x < img_width; x++)
                        {
                            int tx = x - smoothing_radius;
                            int bx = x + smoothing_radius;

                            if (tx < 0) tx = 0;
                            if (bx >= img_width) bx = img_width - 1;

                            long local_magnitude = getIntegral(integral_img, tx, ty, bx, by, img_width);
                            smoothed[n] = (byte)(local_magnitude / smoothing_pixels);
                            n++;
                        }
                    }
                }
                else
                {
                    // this is a colour image
                    smoothed = new byte[img_width * img_height * 3];
                    long[, ,] integral_img = updateIntegralImageColour(img, img_width, img_height);

                    int n = 0;
                    for (int y = 0; y < img_height; y++)
                    {
                        int ty = y - smoothing_radius;
                        int by = y + smoothing_radius;

                        if (ty < 0) ty = 0;
                        if (by >= img_height) by = img_height - 1;

                        for (int x = 0; x < img_width; x++)
                        {
                            int tx = x - smoothing_radius;
                            int bx = x + smoothing_radius;

                            if (tx < 0) tx = 0;
                            if (bx >= img_width) bx = img_width - 1;

                            for (int col = 0; col < 3; col++)
                            {
                                long local_magnitude = getIntegral(integral_img, tx, ty, bx, by, col);
                                smoothed[n] = (byte)(local_magnitude / smoothing_pixels);
                                n++;
                            }
                        }
                    }
                }
            }
            return (smoothed);
        }

        /// <summary>
        /// returns a smoothed version of the given image by local averaging of pixel values
        /// </summary>
        /// <param name="img">image (can be mono or colour)</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="smoothing_radius">patch radius to use for local averaging</param>
        /// <returns>smoothed bitmap</returns>
        public static float[] smoothImageByAveraging(float[] img, int img_width, int img_height,
                                                     int smoothing_radius)
        {
            float[] smoothed = img;

            if (smoothing_radius > 0)
            {
                // number of pixels over which the smoothed result is averaged
                int smoothing_diameter = smoothing_radius * 2;
                int smoothing_pixels = smoothing_diameter * smoothing_diameter;

                if (img.Length == img_width * img_height)
                {
                    // this is a mono image

                    smoothed = new float[img_width * img_height];
                    float[] integral_img = updateIntegralImage(img, img_width, img_height);

                    int n = 0;
                    for (int y = 0; y < img_height; y++)
                    {
                        int ty = y - smoothing_radius;
                        int by = y + smoothing_radius;

                        if (ty < 0) ty = 0;
                        if (by >= img_height) by = img_height - 1;

                        for (int x = 0; x < img_width; x++)
                        {
                            int tx = x - smoothing_radius;
                            int bx = x + smoothing_radius;

                            if (tx < 0) tx = 0;
                            if (bx >= img_width) bx = img_width - 1;

                            float local_magnitude = getIntegral(integral_img, tx, ty, bx, by, img_width);
                            smoothed[n] = local_magnitude / smoothing_pixels;
                            n++;
                        }
                    }
                }
                else
                {
                    // this is a colour image
                    smoothed = new float[img_width * img_height * 3];
                    float[, ,] integral_img = updateIntegralImageColour(img, img_width, img_height);

                    int n = 0;
                    for (int y = 0; y < img_height; y++)
                    {
                        int ty = y - smoothing_radius;
                        int by = y + smoothing_radius;

                        if (ty < 0) ty = 0;
                        if (by >= img_height) by = img_height - 1;

                        for (int x = 0; x < img_width; x++)
                        {
                            int tx = x - smoothing_radius;
                            int bx = x + smoothing_radius;

                            if (tx < 0) tx = 0;
                            if (bx >= img_width) bx = img_width - 1;

                            for (int col = 0; col < 3; col++)
                            {
                                float local_magnitude = getIntegral(integral_img, tx, ty, bx, by, col);
                                smoothed[n] = local_magnitude / smoothing_pixels;
                                n++;
                            }
                        }
                    }
                }
            }
            return (smoothed);
        }

        #endregion        

        #region "conversions"

        /// <summary>
        /// convert a mono image to a colour image
        /// </summary>
        /// <param name="img_mono">mono image data</param>
        /// <param name="img_width">image width</param>
        /// <param name="img_height">image height</param>
        /// <param name="output">optional colour image buffer</param>
        /// <returns>colour image data</returns>
        public static byte[] colourImage(byte[] img_mono, int img_width, int img_height, byte[] output)
        {
            byte[] colour_image = null;
            if (output == null)
            {
                colour_image = new byte[img_width * img_height * 3];
            }
            else
            {
                if (output.Length == img_width * img_height)
                    colour_image = output;
                else
                    colour_image = new byte[img_width * img_height * 3];
            }

            int n = 0;
            for (int i = 0; i < img_mono.Length; i++)
            {
                byte b = img_mono[i];
                colour_image[n++] = b;
                colour_image[n++] = b;
                colour_image[n++] = b;
            }

            return (colour_image);
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="conversion_type">method for converting to mono</param>
        /// <returns></returns>
        public static unsafe byte[] monoImage(byte[] img_colour, int img_width, int img_height,
                                              int conversion_type, byte[] output)
        {
            byte[] mono_image = null;
            if (output == null)
            {
                mono_image = new byte[img_width * img_height];
            }
            else
            {
                if (output.Length == img_width * img_height)
                    mono_image = output;
                else
                    mono_image = new byte[img_width * img_height];
            }

            if (img_colour.Length == mono_image.Length)
            {
                for (int i = 0; i < mono_image.Length; i++)
                    mono_image[i] = img_colour[i];
            }
            else
            {
                int n = 0;
                short tot = 0;
                int luminence = 0;

                fixed (byte* unsafe_mono_image = mono_image)
                {
                    fixed (byte* unsafe_img_colour = img_colour)
                    {
                        for (int i = 0; i < img_width * img_height * 3; i += 3)
                        {
                            switch (conversion_type)
                            {
                                case 0: // magnitude
                                    {
                                        tot = 0;
                                        for (int col = 0; col < 3; col++)
                                        {
                                            tot += unsafe_img_colour[i + col];
                                        }
                                        unsafe_mono_image[n] = (byte)(tot / 3);
                                        break;
                                    }
                                case 1: // luminance
                                    {
                                        luminence = ((unsafe_img_colour[i + 2] * 299) +
                                                     (unsafe_img_colour[i + 1] * 587) +
                                                     (unsafe_img_colour[i] * 114)) / 1000;
                                        //if (luminence > 255) luminence = 255;
                                        unsafe_mono_image[n] = (byte)luminence;
                                        break;
                                    }
                            }
                            n++;
                        }
                    }
                }
            }
            return (mono_image);
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <returns></returns>
        public static byte[] monoImage(byte[] img_colour, int img_width, int img_height)
        {
            return (monoImage(img_colour, img_width, img_height, 0, null));
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static byte[] monoImage(byte[] img_colour, int img_width, int img_height, byte[] output)
        {
            return (monoImage(img_colour, img_width, img_height, 0, output));
        }

        #endregion

        #region "rescaling / resampling"

        /// <summary>
        /// sub-sample a colour image 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <returns></returns>
        public static byte[] downSample(byte[] img, int img_width, int img_height,
                                        int new_width, int new_height)
        {
            byte[] new_img = img;

            if (!((new_width == img_width) && (new_height == img_height)))
            {
                if (img.Length > img_width * img_height)
                {
                    // colour image
                    new_img = new byte[new_width * new_height * 3];
                    int n = 0;

                    for (int y = 0; y < new_height; y++)
                    {
                        int yy = y * (img_height - 1) / new_height;
                        for (int x = 0; x < new_width; x++)
                        {
                            int xx = x * (img_width - 1) / new_width;
                            int n2 = ((yy * img_width) + xx) * 3;
                            for (int col = 0; col < 3; col++)
                            {
                                if (n2 < img.Length - 4) new_img[n] = img[n2 + col];
                                n++;
                            }
                        }
                    }
                }
                else
                {
                    // mono image
                    new_img = new byte[new_width * new_height];
                    int n = 0;

                    for (int y = 0; y < new_height; y++)
                    {
                        int yy = y * (img_height - 1) / new_height;
                        for (int x = 0; x < new_width; x++)
                        {
                            int xx = x * (img_width - 1) / new_width;
                            int n2 = (yy * img_width) + xx;
                            if (n2 < img.Length - 1) new_img[n] = img[n2];
                            n++;
                        }
                    }
                }
            }

            return (new_img);
        }

        #endregion

        #region "loading from different formats"

        /// <summary>
		/// load a bitmap file and return a byte array
		/// </summary>
		public static byte[] loadFromBitmap(String filename,
                                            ref int image_width, ref int image_height,
                                            ref int bytes_per_pixel)
        {
            const int offset = 0x12;
            const int data_offset_position = 0xA;
            byte[] bmp = null;
        
            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);
                
                byte[] data = new byte[54];
                binfile.Read(data, 0, 54);
                
                byte[] width_bytes = new byte[4];
                for (int i = 0; i < width_bytes.Length; i++)
                    width_bytes[i] = data[i + offset];

                byte[] height_bytes = new byte[4];
                for (int i = 0; i < height_bytes.Length; i++)
                    height_bytes[i] = data[i + offset + 4];

                byte[] data_offset_bytes = new byte[4];
                for (int i = 0; i < data_offset_bytes.Length; i++)
                    data_offset_bytes[i] = data[i + data_offset_position];
                    
                bytes_per_pixel = ArrayConversions.ToWord(data[offset + 10], data[offset + 11]) / 8;                    
                image_width = ArrayConversions.ToDWord(width_bytes);
                image_height = ArrayConversions.ToDWord(height_bytes);
                int data_offset = ArrayConversions.ToDWord(data_offset_bytes);
                
                binfile.Close();
                fp.Close();
                
                bmp = loadFromBitmap(filename, image_width, image_height, bytes_per_pixel, data_offset);
            }
            
            return(bmp);
        }

        /// <summary>
		/// load a bitmap file and return a byte array
		/// </summary>
        public static byte[] loadFromBitmap(String filename, int image_width, int image_height, int bytes_per_pixel)
        {
            return (loadFromBitmap(filename, image_width, image_height, bytes_per_pixel, 54));
        }
        
        /// <summary>
		/// load a bitmap file and return a byte array
		/// </summary>
		public static byte[] loadFromBitmap(String filename, int image_width, int image_height, int bytes_per_pixel, int data_offset)
        {
            byte[] bmp = new Byte[image_width * image_height * bytes_per_pixel];

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);

                int n = data_offset;
                int n2 = 0;
                byte[] data = new byte[(image_width * image_height * bytes_per_pixel) + n];
                binfile.Read(data, 0, (image_width * image_height * bytes_per_pixel) + n);
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        n2 = (((image_height - 1 - y) * image_width) + x) * bytes_per_pixel;
                        if (n2 < bmp.Length - bytes_per_pixel)
                        {
                            for (int c = 0; c < bytes_per_pixel; c++)
                            {
                                bmp[n2 + c] = data[n];
                                n++;
                            }
                        }
                        else n += bytes_per_pixel;
                    }
                }
                binfile.Close();
                fp.Close();
            }
            return (bmp);
        }


        public static byte[] loadFromPGM(String filename, int image_width, int image_height, int bytes_per_pixel)
        {
            byte[] bmp = new byte[image_width * image_height * bytes_per_pixel];

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);
                int n = 96;
                int n2 = 0;
                byte[] data = new byte[(image_width * image_height) + n];
                binfile.Read(data, 0, (image_width * image_height) + n);
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        for (int c = 0; c < bytes_per_pixel; c++)
                        {
                            bmp[n2] = data[n];
                            n2++;
                        }
                        n++;
                    }
                }
                binfile.Close();
                fp.Close();
            }
            return (bmp);
        }

        #endregion

        #region "saving to different formats"

        /// <summary>
        /// save the given image data to a PGM file
        /// </summary>
        /// <param name="bmp">image data (can be colour or mono)</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="filename">filename to save as</param>
        public static void saveToPGM(byte[] bmp, int image_width, int image_height,
                                     string filename)
        {
            byte[] img = bmp;

            // convert to mono if necessary
            if (bmp.Length != image_width * image_height)
            {                
                img = new byte[image_width * image_height];
                int n = 0;
                for (int i = 0; i < bmp.Length; i += 3)
                {
                    int luma = (int)(bmp[i + 2] * 0.3 + bmp[i + 1] * 0.59 + bmp[i] * 0.11);
                    img[n] = (byte)luma;
                    n++;
                }
            }

            byte EndLine = (byte)10;
            FileStream OutFile = File.OpenWrite(filename);
            BinaryWriter B_OutFile = new BinaryWriter(OutFile);

            try
            {
                B_OutFile.Write(System.Text.Encoding.ASCII.GetBytes("P5"));
                B_OutFile.Write(EndLine);

                B_OutFile.Write(System.Text.Encoding.ASCII.GetBytes
                    (image_width.ToString() + " " + image_height.ToString()));

                B_OutFile.Write(EndLine);

                B_OutFile.Write(System.Text.Encoding.ASCII.GetBytes("255"));
                B_OutFile.Write(EndLine);

                int n = 0;
                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width; x++)
                    {
                        B_OutFile.Write(img[n]);
                        n++;
                    }
                }

            }
            catch { throw; }
            finally
            {
                B_OutFile.Flush();
                B_OutFile.Close();
            }
        }

        #endregion

        #region "line fitting"

        /// <summary>
        /// find the best position for a corner feature by fitting lines
        /// before and after the corner feature as closely as possible
        /// </summary>
        /// <param name="bmp_mono">image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        public static void matchCorner(byte[] bmp_mono, int image_width, int image_height,
                                       int x1, int y1, int x2, int y2, int x3, int y3, 
                                       int corner_search_radius, int line_search_radius,
                                       ref int best_x2, ref int best_y2)
        {
            if (line_search_radius < 1) 
                line_search_radius = 1;
            
            best_x2 = x2;
            best_y2 = y2;
            float max_score = 0;
            for (int x = x2 - corner_search_radius; x <= x2 + corner_search_radius; x++)
            {
                for (int y = y2 - corner_search_radius; y <= y2 + corner_search_radius; y++)
                {
                    float score = linearity(bmp_mono, image_width, image_height,
                                            x1, y1, x, y, line_search_radius) +
                                  linearity(bmp_mono, image_width, image_height,
                                            x3, y3, x, y, line_search_radius);
                    if (score >= max_score)
                    {
                        max_score = score;
                        best_x2 = x;
                        best_y2 = y;
                    }
                }
            }
        }

        /// <summary>
        /// returns a measure of how likely the given points are the ends of a line
        /// used for line fitting from corner features
        /// </summary>
        /// <param name="bmp_mono">mono image data</param>
        /// <param name="image_width">image width</param>
        /// <param name="image_height">image height</param>
        /// <param name="x1">start x coordinate</param>
        /// <param name="y1">start y coordinate</param>
        /// <param name="x2">end x coordinate</param>
        /// <param name="y2">end y coordinate</param>
        /// <param name="search_radius">small search radius within which to inspect pixels at points along the line</param>
        /// <returns></returns>
        public static float linearity(byte[] bmp_mono, int image_width, int image_height,
                                    int x1, int y1, int x2, int y2, int search_radius)
        {
            const int no_of_samples = 25;
            if (x2 < x1)
            {
                int temp_x = x1;
                x1 = x2;
                x2 = temp_x;
            }
            if (y2 < y1)
            {
                int temp_y = y1;
                y1 = y2;
                y2 = temp_y;
            }
        
            int dx = x2 - x1;
            int dy = y2 - y1;
            
            float side1=0, side2=0;
            int hits1=0, hits2=0;
            int step = dx / no_of_samples;
            if (step < 1) step = 1;
            if (dx > dy)
            {
                // horizontal orientation
                for (int x = x1; x < x2; x += step)
                {
                    int y = y1 + ((x-x1)*dy/dx);
                    for (int r = 1; r <= search_radius; r++)
                    {
                        if (y - r > 0)
                        {
                            int n1 = ((y - r) * image_width) + x;
                            side1 += bmp_mono[n1];
                            hits1++;
                        }
                        if ((y + r > 0) && (y + r < image_height))
                        {
                            int n2 = ((y + r) * image_width) + x;
                            side2 += bmp_mono[n2];
                            hits2++;
                        }
                    }
                }
            }
            else
            {
                // vertical orientation
                step = dy / no_of_samples;
                if (step < 1) step = 1;
                for (int y = y1; y < y2; y += step)
                {
                    if ((y > 0) && (y < image_height))
                    {
                        int x = x1 + ((y-y1)*dx/dy);
                        for (int r = 1; r <= search_radius; r++)
                        {
                            if (x - r > 0)
                            {
                                int n1 = (y * image_width) + x - r;
                                side1 += bmp_mono[n1];
                                hits1++;
                            }
                            if ((x + r > 0) && (x + r < image_width))
                            {
                                int n2 = (y * image_width) + x + r;
                                side2 += bmp_mono[n2];
                                hits2++;
                            }
                        }
                    }
                }
            }
            
            if (hits1 > 0) side1 /= hits1;
            if (hits2 > 0) side2 /= hits2;
            return(Math.Abs(side1 - side2));
        }

        #endregion

        #region "mean shift"

        /// <summary>
        /// returns a set of points where mean shifts above the given threshold occur
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="bytes_per_pixel"></param>
        /// <param name="threshold"></param>
        /// <param name="patch_size"></param>
        /// <returns></returns>
        public static ArrayList GetMeanShiftPoints(byte[] img,
                                            int img_width, int img_height,
                                            int bytes_per_pixel,
                                            float threshold,
                                            int patch_size,
                                            int step_size)
        {
            ArrayList points = new ArrayList();
            ArrayList[] horizontal = meanShiftHorizontal(img, img_width, img_height, bytes_per_pixel, threshold, patch_size, step_size);
            ArrayList[] vertical = meanShiftVertical(img, img_width, img_height, bytes_per_pixel, threshold, patch_size, step_size);

            for (int y = 0; y < img_height; y += step_size)
            {
                for (int i = 0; i < horizontal[y].Count; i += 3)
                {
                    int x = (int)horizontal[y][i];
                    points.Add(x);
                    points.Add(y);
                }
            }

            for (int x = 0; x < img_width; x += step_size)
            {
                for (int i = 0; i < vertical[x].Count; i += 3)
                {
                    int y = (int)vertical[x][i];
                    points.Add(x);
                    points.Add(y);
                }
            }

            return (points);
        }

        /// <summary>
        /// horizontal mean shift
        /// This returns a set of mean shift transition points for each row.
        /// Each point gives the x position of the transition, the length of the segment in pixels
        /// and the average pixel intensity
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="threshold">mean shift threshold</param>
        /// <param name="patch_size">minimum number of pixels over which the mean is calculated</param>
        /// <param name="step_size">used to sample rows</param>
        /// <returns>a set of transition points for the horizontal mean shifts</returns>
        public static ArrayList[] meanShiftHorizontal(byte[] img, 
                                                      int img_width, int img_height,
                                                      int bytes_per_pixel,
                                                      float threshold,
                                                      int patch_size,
                                                      int step_size)
        {
            ArrayList[] mean_transitions = new ArrayList[img_height];

            // the minimum length of any segment
            if (patch_size < 2) patch_size = 2;
            int min_segment_length = patch_size;

            for (int y = 0; y < img_height; y += step_size)
            {
                mean_transitions[y] = new ArrayList();
                float prev_mean_value = -1; // previous mean value
                int mean_hits = 0;          // number of pixels in the current mean
                float mean_tot = 0;         // current mean total
                for (int x = 0; x < img_width; x++)
                {
                    int n = ((y * img_width) + x) * bytes_per_pixel;
                    for (int col = 0; col < bytes_per_pixel; col++)
                    {
                        mean_tot += 1 + img[n];
                        n++;
                    }
                    mean_hits++;

                    float mean_value = mean_tot / mean_hits;
                    if ((prev_mean_value > 0) && 
                        (mean_hits > min_segment_length))
                    {
                        float diff = Math.Abs(mean_value - prev_mean_value) / prev_mean_value;
                        if ((diff > threshold) || (x == img_width-1))
                        {
                            int pixval = (int)(mean_value / bytes_per_pixel);
                            if (pixval > 255) pixval = 255;

                            mean_transitions[y].Add(x - mean_hits);
                            mean_transitions[y].Add(mean_hits);
                            mean_transitions[y].Add(pixval);

                            mean_tot = 0;
                            mean_hits = 0;
                            //prev_mean_value = -1;
                        }
                    }
                    prev_mean_value = mean_value;
                }
            }

            return (mean_transitions);
        }


        /// <summary>
        /// vertical mean shift
        /// This returns a set of mean shift transition points for each column.
        /// Each point gives the x position of the transition, the length of the segment in pixels
        /// and the average pixel intensity
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="threshold">mean shift threshold</param>
        /// <param name="patch_size">minimum number of pixels over which the mean is calculated</param>
        /// <param name="step_size">used to sample columns</param>
        /// <returns>a set of transition points for the horizontal mean shifts</returns>
        public static ArrayList[] meanShiftVertical(byte[] img,
                                                    int img_width, int img_height,
                                                    int bytes_per_pixel,
                                                    float threshold,
                                                    int patch_size,
                                                    int step_size)
        {
            ArrayList[] mean_transitions = new ArrayList[img_width];

            // the minimum length of any segment
            if (patch_size < 2) patch_size = 2;
            int min_segment_length = patch_size;

            for (int x = 0; x < img_width; x += step_size)            
            {
                mean_transitions[x] = new ArrayList();
                float prev_mean_value = -1; // previous mean value
                int mean_hits = 0;          // number of pixels in the current mean
                float mean_tot = 0;         // current mean total
                for (int y = 0; y < img_height; y++)
                {
                    int n = ((y * img_width) + x) * bytes_per_pixel;
                    for (int col = 0; col < bytes_per_pixel; col++)
                    {
                        mean_tot += 1 + img[n];
                        n++;
                    }
                    mean_hits++;

                    float mean_value = mean_tot / mean_hits;
                    if ((prev_mean_value > 0) &&
                        (mean_hits > min_segment_length))
                    {
                        float diff = Math.Abs(mean_value - prev_mean_value) / prev_mean_value;
                        if ((diff > threshold) || (y == img_height - 1))
                        {
                            int pixval = (int)(mean_value / bytes_per_pixel);
                            if (pixval > 255) pixval = 255;

                            mean_transitions[x].Add(y - mean_hits);
                            mean_transitions[x].Add(mean_hits);
                            mean_transitions[x].Add(pixval);

                            mean_tot = 0;
                            mean_hits = 0;
                            prev_mean_value = -1;
                        }
                    }
                    prev_mean_value = mean_value;
                }
            }

            return (mean_transitions);
        }

        #endregion

        #region "locating maxima"

        /// <summary>
        /// returns the horizontal maxima for the given image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="max_features_per_row">maximum number of features per row</param>
        /// <param name="radius">local edge radius</param>
        /// <param name="inhibit_radius">inhibitory radius for non maximal suppression</param>
        /// <param name="min_intensity">minimum edge intensity</param>
        /// <param name="max_intensity">maximum edge_intensity</param>
        /// <param name="image_threshold">minimum image pixel intensity</param>
        /// <param name="localAverageRadius">local averaging radius</param>
        /// <param name="difference_threshold">minimum difference</param>
        /// <param name="step_size">step size for subsampling of rows</param>
        /// <returns></returns>
        public static ArrayList[] horizontal_maxima(byte[] bmp,
                                                    int wdth, int hght, int bytes_per_pixel,
                                                    int max_features_per_row,
                                                    int radius, int inhibit_radius, 
                                                    int min_intensity, int max_intensity, 
                                                    int image_threshold, 
                                                    int localAverageRadius,
                                                    int difference_threshold,
                                                    int step_size,
                                                    ref float average_magnitude)
        {
            // allocate arrays here to save time
            ArrayList[] features = new ArrayList[hght];
            int[] integral = new int[wdth];
            float[] maxima = new float[wdth * 3];
            float[] temp = new float[wdth * 3];

            average_magnitude = 0;
            int hits = 0;

            // examine each row of the image
            for (int y = 0; y < hght; y += step_size)
            {
                // create an empty list for this row
                features[y] = new ArrayList();

                // find maximal edge features
                int no_of_features = row_maxima(y, bmp, wdth, hght, bytes_per_pixel,
                                                integral, maxima, temp,
                                                radius, inhibit_radius,
                                                min_intensity, max_intensity,
                                                max_features_per_row, image_threshold, 
                                                localAverageRadius, difference_threshold);

                // update the list
                for (int i = 0; i < no_of_features; i++)
                {
                    float x = maxima[i * 3];
                    float magnitude = maxima[i * 3] + 2;
                    average_magnitude += magnitude;
                    hits++;
                    features[y].Add(x);
                    features[y].Add(magnitude);
                }
            }

            if (hits > 0) average_magnitude /= hits;
            return (features);
        }

        /// <summary>
        /// returns the vertical maxima for the given image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="max_features_per_column">maximum number of features per column</param>
        /// <param name="radius">local edge radius</param>
        /// <param name="inhibit_radius">inhibitory radius for non maximal suppression</param>
        /// <param name="min_intensity">minimum edge intensity</param>
        /// <param name="max_intensity">maximum edge_intensity</param>
        /// <param name="image_threshold">minimum image pixel intensity</param>
        /// <param name="localAverageRadius">local averaging radius</param>
        /// <param name="difference_threshold">minimum difference</param>
        /// <param name="step_size">step size for subsampling of rows</param>
        /// <returns></returns>
        public static ArrayList[] vertical_maxima(byte[] bmp,
                                                  int wdth, int hght, int bytes_per_pixel,
                                                  int max_features_per_column,
                                                  int radius, int inhibit_radius,
                                                  int min_intensity, int max_intensity,
                                                  int image_threshold,
                                                  int localAverageRadius,
                                                  int difference_threshold,
                                                  int step_size,
                                                  ref float average_magnitude)
        {
            // allocate arrays here to save time
            ArrayList[] features = new ArrayList[wdth];
            int[] integral = new int[hght];
            float[] maxima = new float[hght * 3];
            float[] temp = new float[hght * 3];

            average_magnitude = 0;
            int hits = 0;

            // examine each row of the image
            for (int x = 0; x < wdth; x += step_size)
            {
                // create an empty list for this row
                features[x] = new ArrayList();

                // find maximal edge features
                int no_of_features = column_maxima(x, bmp, wdth, hght, bytes_per_pixel,
                                                   integral, maxima, temp,
                                                   radius, inhibit_radius,
                                                   min_intensity, max_intensity,
                                                   max_features_per_column, image_threshold,
                                                   localAverageRadius, difference_threshold);

                // update the list
                for (int i = 0; i < no_of_features; i++)
                {
                    float y = maxima[i * 3];
                    float magnitude = maxima[i * 3] + 2;
                    average_magnitude += magnitude;
                    hits++;
                    features[x].Add(y);
                    features[x].Add(magnitude);
                }
            }

            if (hits > 0) average_magnitude /= hits;
            return (features);
        }


        /// <summary>
        /// returns a sorted set of maxima
        /// </summary>
        /// <param name="y">the row which is to be analysed</param>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="integral">array used to store sliding sums</param>
        /// <param name="maxima">array used to store maxima</param>
        /// <param name="temp">temporary array</param>
        /// <param name="radius">radius to use when calculating local magnitudes</param>
        /// <param name="inhibit_radius">imhibitory radius</param>
        /// <param name="min_intensity">minimum difference intensity</param>
        /// <param name="max_intensity">maximum difference intensity</param>
        /// <param name="max_maxima">maximum number of maxima to be returned</param>
        /// <param name="image_threshold">minimum pixel intensity</param>
        /// <param name="localAverageRadius">radius to use for local averaging of intensity values</param>
        /// <param name="difference_threshold">minimum difference threshold</param>
        /// <returns>the number of maxima detected</returns>
        public static int row_maxima(int y, byte[] bmp,
                                     int wdth, int hght, int bytes_per_pixel,
                                     int[] integral, float[] maxima, 
                                     float[] temp, 
                                     int radius, int inhibit_radius, 
                                     int min_intensity, int max_intensity, 
                                     int max_maxima,
                                     int image_threshold, 
                                     int localAverageRadius,
                                     int difference_threshold)
        {
            int x, xx, v, i;
            int startpos = y * wdth * bytes_per_pixel;
            int no_of_maxima = 0;
            int no_of_temp_maxima = 0;
            float prev_mag, mag, prev_x, x_accurate, absdiff;

            // update the integrals for the row
            xx = startpos;
            integral[0] = 0;
            for (int b = 0; b < bytes_per_pixel; b++) integral[0] += bmp[xx + b];
            xx += bytes_per_pixel;

            for (x = 1; x < wdth; x++)
            {
                integral[x] = integral[x - 1];
                if (bmp[xx] > image_threshold)
                    for (int b = 0; b < bytes_per_pixel; b++) integral[x] += bmp[xx + b];

                xx += bytes_per_pixel;
            }

            int radius2 = 3 * localAverageRadius / 100;
            if (radius2 < radius+1) radius2 = radius+1;

            // create edges
            for (x = radius; x < wdth - radius - 1; x++)
            {
                v = integral[x];
                float left = (v - integral[x - radius]) / radius;
                float right = (integral[x + radius] - v) / radius;
                float tot = left + right;

                if ((tot > min_intensity) && (tot < max_intensity))
                {
                    int x_min = x - radius2;
                    if (x_min < 0) x_min = 0;
                    int x_max = x + radius2;
                    if (x_max >= wdth) x_max = wdth - 1;
                    float tot_wide = (integral[x_max] - integral[x_min]) / (float)(x_max - x_min);
                    float diff = (left - right) * 140 / tot_wide;

                    absdiff = diff;
                    if (absdiff < 0) absdiff = -absdiff;

                    if (absdiff > difference_threshold)
                    {
                        // a simple kind of sub-pixel interpolation
                        x_accurate = (((x - radius) * left) + ((x + radius) * right)) / tot;

                        temp[no_of_temp_maxima * 3] = x_accurate;
                        temp[(no_of_temp_maxima * 3) + 1] = diff;
                        temp[(no_of_temp_maxima * 3) + 2] = absdiff;
                        no_of_temp_maxima++;
                    }
                }
            }

            // compete
            prev_mag = temp[2];
            prev_x = (int)temp[0];
            for (i = 1; i < no_of_temp_maxima; i++)
            {
                mag = temp[(i * 3) + 2];
                x = (int)temp[i * 3];
                float x_diff = x - prev_x;
                if (x_diff <= inhibit_radius)
                {
                    if (prev_mag <= mag) temp[(i - 1) * 3] = -1;
                    if (mag < prev_mag) temp[i * 3] = -1;
                }

                prev_mag = mag;
                prev_x = x;
            }

            // populate maxima array
            for (i = 1; i < no_of_temp_maxima; i++)
            {
                if (temp[i * 3] > -1)
                {
                    for (int p = 0; p < 3; p++)
                        maxima[(no_of_maxima * 3) + p] = temp[(i * 3) + p];

                    no_of_maxima++;
                }
            }

            // sort edges        
            int search_max = no_of_maxima;
            if (search_max > max_maxima) search_max = max_maxima;
            int winner = -1;
            for (i = 0; i < search_max - 1; i++)
            {
                mag = maxima[(i * 3) + 2];
                winner = -1;
                for (int j = i + 1; j < no_of_maxima; j++)
                {
                    if (maxima[(j * 3) + 2] > mag)
                    {
                        winner = j;
                        mag = maxima[(j * 3) + 2];
                    }
                }
                if (winner > -1)
                {
                    // swap
                    for (int p = 0; p < 3; p++)
                    {
                        float temp2 = maxima[(i * 3) + p];
                        maxima[(i * 3) + p] = maxima[(winner * 3) + p];
                        maxima[(winner * 3) + p] = temp2;
                    }
                }
            }
            no_of_maxima = search_max;

            return (no_of_maxima);
        }

        /// <summary>
        /// returns a sorted set of maxima
        /// </summary>
        /// <param name="x">the column which is to be analysed</param>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="integral">array used to store sliding sums</param>
        /// <param name="maxima">array used to store maxima</param>
        /// <param name="temp">temporary array</param>
        /// <param name="radius">radius to use when calculating local magnitudes</param>
        /// <param name="inhibit_radius">imhibitory radius</param>
        /// <param name="min_intensity">minimum difference intensity</param>
        /// <param name="max_intensity">maximum difference intensity</param>
        /// <param name="max_maxima">maximum number of maxima to be returned</param>
        /// <param name="image_threshold">minimum pixel intensity</param>
        /// <param name="localAverageRadius">radius to use for local averaging of intensity values</param>
        /// <param name="difference_threshold">minimum difference threshold</param>
        /// <returns>the number of maxima detected</returns>
        public static int column_maxima(int x, byte[] bmp,
                                        int wdth, int hght, int bytes_per_pixel,
                                        int[] integral, float[] maxima,
                                        float[] temp,
                                        int radius, int inhibit_radius,
                                        int min_intensity, int max_intensity,
                                        int max_maxima,
                                        int image_threshold,
                                        int localAverageRadius,
                                        int difference_threshold)
        {
            int stride = bytes_per_pixel * wdth;
            int y, yy, v, i;
            int startpos = x * bytes_per_pixel;
            int no_of_maxima = 0;
            int no_of_temp_maxima = 0;
            float prev_mag, mag, prev_y, y_accurate, absdiff;

            // update the integrals for the row
            yy = startpos;
            integral[0] = 0;
            for (int b = 0; b < bytes_per_pixel; b++) integral[0] += bmp[yy + b];
            yy += stride;
            
            for (y = 1; y < hght; y++)
            {
                integral[y] = integral[y - 1];
                if (bmp[yy] > image_threshold)
                    for (int b = 0; b < bytes_per_pixel; b++) integral[y] += bmp[yy + b];

                yy += stride;
            }

            int radius2 = 3 * localAverageRadius / 100;
            if (radius2 < radius + 1) radius2 = radius + 1;

            // create edges
            for (y = radius; y < hght - radius - 1; y++)
            {
                v = integral[y];
                float above = (v - integral[y - radius]) /  radius;
                float below = (integral[y + radius] - v) / radius;
                float tot = above + below;

                if ((tot > min_intensity) && (tot < max_intensity))
                {
                    int y_min = y - radius2;
                    if (y_min < 0) y_min = 0;
                    int y_max = y + radius2;
                    if (y_max >= hght) y_max = hght - 1;
                    float tot_wide = (integral[y_max] - integral[y_min]) / (float)(y_max - y_min);
                    float diff = (above - below) * 140 / tot_wide;

                    absdiff = diff;
                    if (absdiff < 0) absdiff = -absdiff;

                    if (absdiff > difference_threshold)
                    {
                        // a simple kind of sub-pixel interpolation
                        y_accurate = (((y - radius) * above) + ((y + radius) * below)) / tot;

                        temp[no_of_temp_maxima * 3] = y_accurate;
                        temp[(no_of_temp_maxima * 3) + 1] = diff;
                        temp[(no_of_temp_maxima * 3) + 2] = absdiff;
                        no_of_temp_maxima++;
                    }
                }
            }

            // compete
            prev_mag = temp[2];
            prev_y = (int)temp[0];
            for (i = 1; i < no_of_temp_maxima; i++)
            {
                mag = temp[(i * 3) + 2];
                y = (int)temp[i * 3];
                float y_diff = y - prev_y;
                if (y_diff <= inhibit_radius)
                {
                    if (prev_mag <= mag) temp[(i - 1) * 3] = -1;
                    if (mag < prev_mag) temp[i * 3] = -1;
                }

                prev_mag = mag;
                prev_y = y;
            }

            // populate maxima array
            for (i = 1; i < no_of_temp_maxima; i++)
            {
                if (temp[i * 3] > -1)
                {
                    for (int p = 0; p < 3; p++)
                        maxima[(no_of_maxima * 3) + p] = temp[(i * 3) + p];

                    no_of_maxima++;
                }
            }

            // sort edges        
            int search_max = no_of_maxima;
            if (search_max > max_maxima) search_max = max_maxima;
            int winner = -1;
            for (i = 0; i < search_max - 1; i++)
            {
                mag = maxima[(i * 3) + 2];
                winner = -1;
                for (int j = i + 1; j < no_of_maxima; j++)
                {
                    if (maxima[(j * 3) + 2] > mag)
                    {
                        winner = j;
                        mag = maxima[(j * 3) + 2];
                    }
                }
                if (winner > -1)
                {
                    // swap
                    for (int p = 0; p < 3; p++)
                    {
                        float temp2 = maxima[(i * 3) + p];
                        maxima[(i * 3) + p] = maxima[(winner * 3) + p];
                        maxima[(winner * 3) + p] = temp2;
                    }
                }
            }
            no_of_maxima = search_max;

            return (no_of_maxima);
        }

        #endregion

        #region "mirroring and flipping"

        /// <summary>
        /// mirror the given image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <returns>mirrored varesion</returns>
        public static byte[] Mirror(byte[] bmp,
                                    int wdth, int hght,
                                    int bytes_per_pixel)
        {
            byte[] mirrored = new byte[wdth * hght * bytes_per_pixel];

            for (int y = 0; y < hght; y++)
            {
                int n0 = (y * wdth);
                for (int x = 0; x < wdth; x++)
                {                    
                    int n1 = (n0 + x) * bytes_per_pixel;
                    int x2 = wdth - 1 - x;
                    int n2 = (n0 + x2) * bytes_per_pixel;
                    for (int col = 0; col < bytes_per_pixel; col++)
                        mirrored[n2 + col] = bmp[n1 + col];
                }
            }
            return (mirrored);
        }

        /// <summary>
        /// flip the given image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <returns>flipped varesion</returns>
        public static byte[] Flip(byte[] bmp,
                                  int wdth, int hght,
                                  int bytes_per_pixel)
        {
            byte[] flipped = new byte[wdth * hght * bytes_per_pixel];

            for (int y = 0; y < hght; y++)
            {
                int n0 = (y * wdth);
                for (int x = 0; x < wdth; x++)
                {
                    int n1 = (n0 + x) * bytes_per_pixel;
                    int n2 = (((hght - 1 - y) * wdth) + x) * bytes_per_pixel;
                    for (int col = 0; col < bytes_per_pixel; col++)
                        flipped[n2 + col] = bmp[n1 + col];
                }
            }
            return (flipped);
        }

        #endregion

        #region "histograms"

        /// <summary>
        /// returns a grey scale histogram for the given image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="levels">histogram levels</param>
        /// <returns></returns>
        public static float[] GetGreyHistogram(byte[] bmp,
                                        int wdth, int hght,
                                        int bytes_per_pixel,
                                        int levels)
        {
            float[] hist = new float[levels];
            for (int i = 0; i < bmp.Length; i += bytes_per_pixel)
            {
                float intensity = 0;
                for (int col = 0; col < bytes_per_pixel; col++)
                    intensity += bmp[i + col];
                intensity /= bytes_per_pixel;

                int bucket = (int)Math.Round(intensity * levels / 255);
                if (bucket >= levels) bucket = levels-1;
                hist[bucket]++;
            }

            // normalise the histogram
            float max = 1;
            for (int level = 0; level < levels; level++)
                if (hist[level] > max) max = hist[level];

            for (int level = 0; level < levels; level++)
                hist[level] = hist[level] / max;

            return (hist);
        }

        /// <summary>
        /// returns a grey scale histogram for the given image within the given perimeter region
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="levels">histogram levels</param>
        /// <param name="perimeter">perimeter region inside which to calculate the histogram</param>
        /// <returns></returns>
        public static float[] GetGreyHistogram(byte[] bmp,
                                int wdth, int hght,
                                int bytes_per_pixel,
                                int levels,
                                polygon2D perimeter)
        {
            float[] hist = new float[levels];

            int tx = (int)perimeter.left();
            int ty = (int)perimeter.top();
            int bx = (int)perimeter.right();
            int by = (int)perimeter.bottom();

            for (int y = ty; y <= by; y++)
            {
                if ((y > -1) && (y < hght))
                {
                    for (int x = tx; x <= bx; x++)
                    {
                        if ((x > -1) && (x < wdth))
                        {
                            if (perimeter.isInside(x, y))
                            {
                                int n = ((y * wdth) + x) * bytes_per_pixel;
                                float intensity = 0;
                                for (int col = 0; col < bytes_per_pixel; col++)
                                    intensity += bmp[n + col];
                                intensity /= bytes_per_pixel;

                                int bucket = (int)Math.Round(intensity * levels / 255);
                                if (bucket >= levels) bucket = levels - 1;
                                hist[bucket]++;
                            }
                        }
                    }
                }
            }

            // normalise the histogram
            float max = 1;
            for (int level = 0; level < levels; level++)
                if (hist[level] > max) max = hist[level];

            for (int level = 0; level < levels; level++)
                hist[level] = hist[level] / max;

            return (hist);
        }


        /// <summary>
        /// returns a grey scale histogram for the given image
        /// using a circular sample region
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="wdth">width of the image</param>
        /// <param name="hght">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="levels">histogram levels</param>
        /// <param name="radius_fraction">fractional radius value in the range 0.0 - 1.0</param>
        /// <param name="normalise">whether to normalise the histogram into the range 0.0 - 1.0</param>
        /// <param name="sampling_step">step size to use when sampling the image</param>
        /// <returns></returns>
        public static float[] GetGreyHistogramCircular(byte[] bmp,
                                                int wdth, int hght,
                                                int bytes_per_pixel,
                                                int levels,
                                                float radius_fraction,
                                                bool normalise,
                                                int sampling_step)
        {
            int half_width = wdth / 2;
            int half_height = hght / 2;
            int radius = (int)((hght / 2) * radius_fraction);
            int radiusSquared = radius * radius;

            int tx = half_width - radius;
            int bx = half_width + radius;
            int ty = half_height - radius;
            int by = half_height + radius;

            if (tx < 0) tx = 0;
            if (ty < 0) ty = 0;
            if (bx >= wdth) bx = wdth - 1;
            if (by >= hght) by = hght - 1;

            float[] hist = new float[levels];
            for (int y = tx; y < by; y += sampling_step)
            {
                int dy = half_height - y;
                dy *= dy;
                for (int x = tx; x < bx; x += sampling_step)
                {
                    int dx = half_width - x;
                    dx *= dx;
                    if (dx + dy < radiusSquared)
                    {
                        int n = ((y * wdth) + x) * bytes_per_pixel;
                        float intensity = 0;
                        for (int col = 0; col < bytes_per_pixel; col++)
                            intensity += bmp[n + col];
                        intensity /= bytes_per_pixel;
                        int bucket = (int)Math.Round(intensity * levels / 255);
                        if (bucket >= levels) bucket = levels - 1;
                        hist[bucket]++;
                    }
                }
            }

            // normalise the histogram
            if (normalise)
            {
                float max = 1;
                for (int level = 0; level < levels; level++)
                    if (hist[level] > max) max = hist[level];

                for (int level = 0; level < levels; level++)
                    hist[level] = hist[level] / max;
            }

            return (hist);
        }

        #endregion

        #region "adjust contrast"

        /// <summary>
        /// increase or decrease the contrast within an image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="contrast_value">contrast scaling factor (>1 increase <1 decrease)</param>
        /// <returns></returns>
        public static byte[] Contrast(byte[] bmp,
                                      int width, int height,
                                      int bytes_per_pixel,
                                      float contrast_value)
        {
            return (Contrast(bmp, width, height, bytes_per_pixel, 0, 0, width, height, contrast_value));
        }

        /// <summary>
        /// increase or decrease the contrast within an image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="tx">bounding box for calculating the average pixel intensity</param>
        /// <param name="ty">bounding box for calculating the average pixel intensity</param>
        /// <param name="bx">bounding box for calculating the average pixel intensity</param>
        /// <param name="by">bounding box for calculating the average pixel intensity</param>
        /// <param name="contrast_value">contrast scaling factor (>1 increase <1 decrease)</param>
        /// <returns></returns>
        public static byte[] Contrast(byte[] bmp,
                                      int width, int height,
                                      int bytes_per_pixel,
                                      int tx, int ty, int bx, int by,
                                      float contrast_value)
        {
            byte[] new_bmp = bmp;
            if (contrast_value != 1)
            {
                new_bmp = new byte[width * height * bytes_per_pixel];

                // find the average pixel intensity
                int n = 0;
                float average_intensity = 0;
                for (int y = ty; y < by; y++)
                {
                    for (int x = tx; x < bx; x++)
                    {
                        n = ((y * width) + x) * bytes_per_pixel;
                        for (int i = 0; i < bytes_per_pixel; i++)
                            average_intensity += bmp[n + i];
                    }
                }
                average_intensity /= ((bx-tx) * (by-ty) * bytes_per_pixel);

                float scaling_factor = 0;
                float ratio;
                n = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // intensity of this pixel
                        float intensity = 0;
                        for (int col = 0; col < bytes_per_pixel; col++)
                            intensity += bmp[n + col];
                        intensity /= bytes_per_pixel;

                        ratio = 1;
                        if (average_intensity > 0)
                            ratio = intensity / average_intensity;

                        scaling_factor = ((ratio - 1.0f) * contrast_value) + 1.0f;

                        for (int col = 0; col < bytes_per_pixel; col++)
                        {
                            int new_value = (int)(bmp[n + col] * scaling_factor);
                            if (new_value > 255) new_value = 255;
                            if (new_value < 0) new_value = 0;
                            new_bmp[n + col] += (byte)new_value;
                        }

                        n += bytes_per_pixel;
                    }
                }
            }
            return (new_bmp);
        }

        #endregion

        #region "erosion and dilation"

        #region "dilate colour images"

        public static byte[] Dilate(byte[] bmp,
                                    int width, int height,
                                    int bytes_per_pixel,
                                    int radius,
                                    byte[] result)
        {
            if (result == null)
                result = new byte[width * height * bytes_per_pixel];

            int intensity = 0;
            int max_intensity = 0;
            int n, n2, n3;

            byte[] source = (byte[])bmp.Clone();
            int r = 1;

            for (int j = 0; j < radius; j++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int best_n = 0;
                        max_intensity = 0;

                        for (int yy = y - r; yy <= y + r; yy++)
                        {
                            if ((yy > -1) && (yy < height))
                            {
                                n2 = yy * width * bytes_per_pixel;
                                for (int xx = x - r; xx <= x + r; xx++)
                                {
                                    if ((xx > -1) && (xx < width))
                                    {
                                        n3 = n2 + (xx * bytes_per_pixel);

                                        // get the intensity value of this pixel
                                        intensity = 0;
                                        for (int i = 0; i < bytes_per_pixel; i++)
                                            intensity += source[n3 + i];

                                        // is this the biggest intensity ?
                                        if (intensity > max_intensity)
                                        {
                                            max_intensity = intensity;
                                            best_n = n3;
                                        }

                                    }
                                }
                            }
                        }

                        // result pixel
                        n = (((y * width) + x) * bytes_per_pixel);
                        for (int i = 0; i < bytes_per_pixel; i++)
                            result[n + i] = source[best_n + i];
                    }
                }

                if (j < radius-1)
                    for (int i = 0; i < result.Length; i++)
                        source[i] = result[i];
            }

            return (result);
        }

        public static float[] Dilate(float[] bmp,
                                        int width, int height,
                                        int bytes_per_pixel,
                                        int radius,
                                        float[] result)
        {
            if (result == null)
                result = new float[width * height * bytes_per_pixel];

            float intensity = 0;
            float max_intensity = 0;
            int n, n2, n3;
            float[] source = (float[])bmp.Clone();
            int r = 1;

            for (int j = 0; j < radius; j++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        max_intensity = 0;
                        int best_n = 0;

                        for (int yy = y - r; yy <= y + r; yy++)
                        {
                            if ((yy > -1) && (yy < height))
                            {
                                n2 = yy * width * bytes_per_pixel;
                                for (int xx = x - r; xx <= x + r; xx++)
                                {
                                    if ((xx > -1) && (xx < width))
                                    {
                                        n3 = n2 + (xx * bytes_per_pixel);

                                        // get the intensity value of this pixel
                                        intensity = 0;
                                        for (int i = 0; i < bytes_per_pixel; i++)
                                            intensity += source[n3 + i];

                                        // is this the biggest intensity ?
                                        if (intensity > max_intensity)
                                        {
                                            max_intensity = intensity;
                                            best_n = n3;
                                        }

                                    }
                                }
                            }
                        }

                        // result pixel
                        n = (((y * width) + x) * bytes_per_pixel);
                        for (int i = 0; i < bytes_per_pixel; i++)
                            result[n + i] = source[best_n + i];
                    }
                }

                if (j < radius-1)
                    for (int i = 0; i < result.Length; i++)
                        source[i] = result[i];
            }

            return (result);
        }

        #endregion

        #region "perform erosion and dilation at the same time on a mono image"

        public static void ErodeDilate(byte[] bmp_mono,
                                       int width, int height,
                                       int radius,
                                       byte[] result_erode,
                                       byte[] result_dilate)
        {
            byte[] source_erode = (byte[])bmp_mono.Clone();
            byte[] source_dilate = (byte[])bmp_mono.Clone();

            for (int r = 0; r < radius; r++)
            {
                int n = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (i == 0)
                            {
                                if (x > 0)
                                {
                                    // left
                                    if (source_dilate[n - 1] > source_dilate[n])
                                        result_dilate[n] = source_dilate[n - 1];
                                    else
                                        result_dilate[n] = source_dilate[n];

                                    // top left
                                    if (y > 0)
                                        if (source_dilate[n - width - 1] > result_dilate[n]) result_dilate[n] = source_dilate[n - width - 1];

                                    // bottom left
                                    if (y < height - 1)
                                        if (source_dilate[n + width - 1] > result_dilate[n]) result_dilate[n] = source_dilate[n + width - 1];
                                }

                                if (x < width - 1)
                                {
                                    // right
                                    if (source_dilate[n + 1] > result_dilate[n]) result_dilate[n] = source_dilate[n + 1];

                                    // top right
                                    if (y > 0)
                                        if (source_dilate[n - width + 1] > result_dilate[n]) result_dilate[n] = source_dilate[n - width + 1];

                                    // bottom right
                                    if (y < height - 1)
                                        if (source_dilate[n + width + 1] > result_dilate[n]) result_dilate[n] = source_dilate[n + width + 1];
                                }

                                // above
                                if (y > 0)
                                    if (source_dilate[n - width] > result_dilate[n]) result_dilate[n] = source_dilate[n - width];

                                // below
                                if (y < height - 1)
                                    if (source_dilate[n + width] > result_dilate[n]) result_dilate[n] = source_dilate[n + width];
                            }
                            else
                            {
                                if (x > 0)
                                {
                                    if (source_erode[n - 1] < source_erode[n])
                                        result_erode[n] = source_erode[n - 1];
                                    else
                                        result_erode[n] = source_erode[n];

                                    if (y > 0)
                                        if (source_erode[n - width - 1] < result_erode[n]) result_erode[n] = source_erode[n - width - 1];
                                    if (y < height - 1)
                                        if (source_erode[n + width - 1] < result_erode[n]) result_erode[n] = source_erode[n + width - 1];
                                }

                                if (x < width - 1)
                                {
                                    if (source_erode[n + 1] < result_erode[n]) result_erode[n] = source_erode[n + 1];

                                    if (y > 0)
                                        if (source_erode[n - width + 1] < result_erode[n]) result_erode[n] = source_erode[n - width + 1];
                                    if (y < height - 1)
                                        if (source_erode[n + width + 1] < result_erode[n]) result_erode[n] = source_erode[n + width + 1];
                                }

                                if (y > 0)
                                    if (source_erode[n - width] < result_erode[n]) result_erode[n] = source_erode[n - width];
                                if (y < height - 1)
                                    if (source_erode[n + width] < result_erode[n]) result_erode[n] = source_erode[n + width];
                            }

                        }

                        n++;
                    }
                }

                if (r < radius - 1)
                    for (int i = 0; i < source_erode.Length; i++)
                    {
                        source_erode[i] = result_erode[i];
                        source_dilate[i] = result_dilate[i];
                    }
            }
        }

        #endregion

        #region "opening and closing mono images"

        public static byte[] Opening(byte[] bmp,
                                     int width, int height,
                                     int radius,
                                     byte[] result)
        {
            byte[] img1 = Erode(bmp, width, height, radius, null);
            byte[] img2 = Dilate(img1, width, height, radius, result);
            return(img2);
        }

        public static byte[] Closing(byte[] bmp,
                                     int width, int height,
                                     int radius,
                                     byte[] result)
        {
            byte[] img1 = Dilate(bmp, width, height, radius, null);
            byte[] img2 = Erode(img1, width, height, radius, result);
            return(img2);         
        }

        #endregion
        
        #region "dilate mono images"

        
        public static unsafe byte[] Dilate(byte[] bmp,
                                           int width, int height,
                                           int radius,
                                           byte[] result)
        {
            if (result == null) result = new byte[width * height];

            byte[] source = (byte[])bmp.Clone();

            fixed (byte* unsafe_result = result)
            {
                fixed (byte* unsafe_source = source)
                {
                    int n;
                    byte v;
                    int pixels = width * height;
                    int min = width + 1;
                    int max = pixels - width - 1;
                    for (int r = 0; r < radius; r++)
                    {
                        for (int i = min; i < max; i++)
                        {
                            v = unsafe_source[i];

                            // same row
                            if (unsafe_source[i - 1] > v) v = unsafe_source[i - 1];
                            if (unsafe_source[i + 1] > v) v = unsafe_source[i + 1];

                            // row above
                            n = i - width - 1;
                            if (unsafe_source[n] > v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] > v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] > v) v = unsafe_source[n];

                            // row below
                            n = i + width - 1;
                            if (unsafe_source[n] > v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] > v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] > v) v = unsafe_source[n];
                            
                            unsafe_result[i] = v;
                        }

                        if (r < radius - 1)
                        {
                            byte* dst = unsafe_source;
                            byte* src = unsafe_result;
                            for (int i = 0; i < source.Length; i++) *dst++ = *src++;
                        }
                    }
                }
            }

            return (result);
        }
        
        
        public static unsafe float[] Dilate(float[] bmp,
                                     int width, int height,
                                     int radius,
                                     float[] result)
        {
            if (result == null)
                result = new float[width * height];

            float intensity = 0;
            float max_intensity = 0;
            int n, n2, n3;
            float[] source = (float[])bmp.Clone();
            const int r = 1;

            fixed (float* unsafe_result = result)
            {
                fixed (float* unsafe_source = source)
                {
                    for (int j = 0; j < radius; j++)
                    {
                        for (int y = r; y < height - r; y++)
                        {
                            for (int x = r; x < width - r; x++)
                            {
                                max_intensity = 0;
                                int best_n = 0;

                                for (int yy = y - r; yy <= y + r; yy++)
                                {
                                    n2 = yy * width;
                                    for (int xx = x - r; xx <= x + r; xx++)
                                    {
                                        n3 = n2 + xx;

                                        // get the intensity value of this pixel
                                        intensity += unsafe_source[n3];

                                        // is this the biggest intensity ?
                                        if (intensity > max_intensity)
                                        {
                                            max_intensity = intensity;
                                            best_n = n3;
                                        }
                                    }
                                }

                                // result pixel
                                n = (y * width) + x;
                                unsafe_result[n] = unsafe_source[best_n];
                            }
                        }

                        if (j < radius - 1)
                            for (int i = 0; i < result.Length; i++)
                                unsafe_source[i] = unsafe_result[i];
                    }
                }
            }

            return (result);
        }


        #endregion

        #region "erode colour images"

        public static byte[] Erode(byte[] bmp,
                                   int width, int height,
                                   int bytes_per_pixel,
                                   int radius,
                                   byte[] result)
        {
            if (result ==  null)
                result = new byte[width * height * bytes_per_pixel];

            int intensity = 0;
            int min_intensity = 0;
            int n, n2, n3;

            byte[] source = (byte[])bmp.Clone();
            const int r = 1;

            for (int j = 0; j < radius; j++)
            {                
                for (int y = r; y < height-r; y++)
                {
                    for (int x = r; x < width-r; x++)
                    {
                        int best_n = 0;
                        min_intensity = 255 * bytes_per_pixel;

                        for (int yy = y - r; yy <= y + r; yy++)
                        {
                            n2 = yy * width * bytes_per_pixel;
                            for (int xx = x - r; xx <= x + r; xx++)
                            {
                                n3 = n2 + (xx * bytes_per_pixel);

                                // get the intensity value of this pixel
                                intensity = 0;
                                for (int i = 0; i < bytes_per_pixel; i++)
                                    intensity += source[n3 + i];

                                // is this the smallest intensity ?
                                if (intensity < min_intensity)
                                {
                                    min_intensity = intensity;
                                    best_n = n3;
                                }
                            }
                        }

                        // result pixel
                        n = (((y * width) + x) * bytes_per_pixel);
                        for (int i = 0; i < bytes_per_pixel; i++)
                            result[n + i] = source[best_n + i];
                    }
                }

                if (j < radius-1)
                    for (int i = 0; i < result.Length; i++)
                        source[i] = result[i];

            }

            return (result);
        }

        public static float[] Erode(float[] bmp,
                                    int width, int height,
                                    int bytes_per_pixel,
                                    int radius,
                                    float[] result)
        {
            if (result == null)
                result = new float[width * height * bytes_per_pixel];

            float intensity = 0;
            float min_intensity = 0;
            int n, n2, n3;

            float[] source = (float[])bmp.Clone();
            const int r = 1;

            for (int j = 0; j < radius; j++)
            {
                for (int y = r; y < height-r; y++)
                {
                    for (int x = r; x < width-r; x++)
                    {
                        int best_n = 0;
                        min_intensity = 255 * bytes_per_pixel;

                        for (int yy = y - r; yy <= y + r; yy++)
                        {
                            n2 = yy * width * bytes_per_pixel;
                            for (int xx = x - r; xx <= x + r; xx++)
                            {
                                n3 = n2 + (xx * bytes_per_pixel);

                                // get the intensity value of this pixel
                                intensity = 0;
                                for (int i = 0; i < bytes_per_pixel; i++)
                                    intensity += source[n3 + i];

                                // is this the smallest intensity ?
                                if (intensity < min_intensity)
                                {
                                    min_intensity = intensity;
                                    best_n = n3;
                                }
                            }
                        }

                        // result pixel
                        n = (((y * width) + x) * bytes_per_pixel);
                        for (int i = 0; i < bytes_per_pixel; i++)
                            result[n + i] = source[best_n + i];
                    }
                }

                if (j < radius-1)
                    for (int i = 0; i < result.Length; i++)
                        source[i] = result[i];
            }

            return (result);
        }

        #endregion

        #region "erode mono images"

        public static unsafe byte[] Erode(byte[] bmp,
                                          int width, int height,
                                          int radius,
                                          byte[] result)
        {
            if (result == null) result = new byte[width * height];

            byte[] source = (byte[])bmp.Clone();

            fixed (byte* unsafe_result = result)
            {
                fixed (byte* unsafe_source = source)
                {
                    int n;
                    byte v;
                    int pixels = width * height;
                    int min = width + 1;
                    int max = pixels - width - 1;
                    for (int r = 0; r < radius; r++)
                    {
                        for (int i = min; i < max; i++)
                        {
                            v = unsafe_source[i];

                            // same row
                            if (unsafe_source[i - 1] < v) v = unsafe_source[i - 1];
                            if (unsafe_source[i + 1] < v) v = unsafe_source[i + 1];

                            // row above
                            n = i - width - 1;
                            if (unsafe_source[n] < v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] < v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] < v) v = unsafe_source[n];

                            // row below
                            n = i + width - 1;
                            if (unsafe_source[n] < v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] < v) v = unsafe_source[n]; n++;
                            if (unsafe_source[n] < v) v = unsafe_source[n];
                            
                            unsafe_result[i] = v;
                        }

                        if (r < radius - 1)
                        {
                            byte* dst = unsafe_source;
                            byte* src = unsafe_result;
                            for (int i = 0; i < source.Length; i++) *dst++ = *src++;
                        }
                    }
                }
            }

            return (result);
        }        
        
        
 /*       
        public static unsafe byte[] Erode(byte[] bmp,
                                          int width, int height,
                                          int radius,
                                          byte[] result)
        {
            if (result == null) result = new byte[width * height];

            byte[] source = (byte[])bmp.Clone();

            fixed (byte* unsafe_result = result)
            {
                fixed (byte* unsafe_source = source)
                {
                    int n;
                    byte v;
                    for (int r = 0; r < radius; r++)
                    {
                        n = width;
                        for (int y = 1; y < height-1; y++)
                        {
							n++;
                            for (int x = 1; x < width-1; x++)
                            {
								// left
                                if (unsafe_source[n - 1] < unsafe_source[n])
                                    v = unsafe_source[n - 1];
                                else
                                    v = unsafe_source[n];

								// right
                                if (unsafe_source[n + 1] < v) v = unsafe_source[n + 1];

								// top
                                if (unsafe_source[n - width] < v) v = unsafe_source[n - width];
								
								// bottom
                                if (unsafe_source[n + width] < v) v = unsafe_source[n + width];

								// top right
                                if (unsafe_source[n - width + 1] < v) v = unsafe_source[n - width + 1];
								
								// bottom right
                                if (unsafe_source[n + width + 1] < v) v = unsafe_source[n + width + 1];
 
								// top left
                                if (unsafe_source[n - width - 1] < v) v = unsafe_source[n - width - 1];
                                
                                // bottom left								
								if (unsafe_source[n + width - 1] < v) v = unsafe_source[n + width - 1];
								
                                unsafe_result[n] = v;

                                n++;
                            }
							n++;
                        }

                        if (r < radius - 1)
                            for (int i = 0; i < source.Length; i++) unsafe_source[i] = unsafe_result[i];
                    }
                }
            }

            return (result);
        }
*/
        
        public static unsafe float[] Erode(float[] bmp,
                                    int width, int height,
                                    int radius,
                                    float[] result)
        {
            if (result == null) result = new float[width * height];

            float intensity = 0;
            float min_intensity = 0;
            int n, n2, n3;

            float[] source = (float[])bmp.Clone();

            fixed (float* unsafe_result = result)
            {
                fixed (float* unsafe_source = source)
                {
                    for (int j = 0; j < radius; j++)
                    {
                        int r = 1;

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int best_n = 0;
                                min_intensity = 255;

                                for (int yy = y - r; yy <= y + r; yy++)
                                {
                                    if ((yy > -1) && (yy < height))
                                    {
                                        n2 = yy * width;
                                        for (int xx = x - r; xx <= x + r; xx++)
                                        {
                                            if ((xx > -1) && (xx < width))
                                            {
                                                n3 = n2 + xx;

                                                // get the intensity value of this pixel
                                                intensity = unsafe_source[n3];

                                                // is this the smallest intensity ?
                                                if (intensity < min_intensity)
                                                {
                                                    min_intensity = intensity;
                                                    best_n = n3;
                                                }

                                            }
                                        }
                                    }
                                }

                                // result pixel
                                n = (y * width) + x;
                                unsafe_result[n] = unsafe_source[best_n];
                            }
                        }

                        if (j < radius - 1)
                            for (int i = 0; i < result.Length; i++)
                                unsafe_source[i] = unsafe_result[i];
                    }
                }
            }

            return (result);
        }

        #endregion

        #endregion

        #region "average intensity"

        /// <summary>
        /// returns the average pixel intensity for the given line
        /// </summary>
        /// <param name="img">image to be returned</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="x1">top x</param>
        /// <param name="y1">top y</param>
        /// <param name="x2">bottom x</param>
        /// <param name="y2">bottom y</param>
        /// <param name="linewidth">line width</param>
        /// <param name="mean_light">average light value above the average intensity</param>
        /// <param name="mean_dark">average dark value below the average intensity</param>
        /// <param name="variance">variation between light and dark along the line</param>
        /// <param name="intensities">intensity values along the line</param>
        public static float averageLineIntensity(Byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                                 int x1, int y1, int x2, int y2, int linewidth,
                                                 ref float mean_light, ref float mean_dark,
                                                 ref float variance,
                                                 ref ArrayList intensities)
        {
            float av_intensity = 0;
            int hits = 0;
            intensities = new ArrayList();

            ArrayList intensity_sums = new ArrayList();
            float prev_intensity_sum = 0;

            if (img != null)
            {
                int w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
                float m;

                dx = x2 - x1;
                dy = y2 - y1;
                w = Math.Abs(dx);
                h = Math.Abs(dy);
                if (x2 >= x1) step_x = 1; else step_x = -1;
                if (y2 >= y1) step_y = 1; else step_y = -1;

                if ((w < img_width) && (h < img_height))
                {
                    if (w > h)
                    {
                        if (dx != 0)
                        {
                            m = dy / (float)dx;
                            x = x1;
                            int s = 0;
                            while (s * Math.Abs(step_x) <= Math.Abs(dx))
                            {
                                y = (int)(m * (x - x1)) + y1;

                                float intensity = 0;
                                int intensity_hits = 0;
                                for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                                    for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                                    {
                                        if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                        {
                                            int n = ((img_width * yy2) + xx2) * bytes_per_pixel;
                                            if ((n >= 0) && (n < img.Length-3))
                                            {
                                                for (int col = 0; col < bytes_per_pixel; col++)
                                                    intensity += img[n + col];
                                                intensity_hits += bytes_per_pixel;
                                            }
                                        }
                                    }

                                if (intensity_hits > 0)
                                {
                                    intensity /= intensity_hits;
                                    intensities.Add(intensity);
                                    intensity_sums.Add(intensity + prev_intensity_sum);

                                    av_intensity += intensity;
                                    hits++;
                                    prev_intensity_sum += intensity;
                                }

                                x += step_x;
                                s++;
                            }
                        }
                    }
                    else
                    {
                        if (dy != 0)
                        {
                            m = dx / (float)dy;
                            y = y1;
                            int s = 0;
                            while (s * Math.Abs(step_y) <= Math.Abs(dy))
                            {
                                x = (int)(m * (y - y1)) + x1;

                                float intensity = 0;
                                int intensity_hits = 0;
                                for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                                    for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                                    {
                                        if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                        {
                                            int n = ((img_width * yy2) + xx2) * bytes_per_pixel;

                                            if ((n >= 0) && (n < img.Length - 3))
                                            {
                                                for (int col = 0; col < bytes_per_pixel; col++)
                                                    intensity += img[n + col];
                                                intensity_hits += bytes_per_pixel;
                                            }
                                        }
                                    }

                                if (intensity_hits > 0)
                                {
                                    intensity /= intensity_hits;
                                    intensities.Add(intensity);
                                    intensity_sums.Add(intensity + prev_intensity_sum);

                                    av_intensity += intensity;
                                    hits++;
                                    prev_intensity_sum += intensity;
                                }

                                y += step_y;
                                s++;
                            }
                        }
                    }
                }
            }
            if (hits > 0) av_intensity /= hits;

            // calculate mean light and dark values
            mean_light = 0;
            int light_hits = 0;
            mean_dark = 0;
            int dark_hits = 0;
            variance = 0;
            int background_width = intensities.Count * 20 / 100;
            for (int i = 0; i < intensities.Count; i++)
            {
                // get a local value for the threshold
                int min_i = i - background_width;
                if (min_i < 0) min_i = 0;
                int max_i = i + background_width;
                if (max_i >= intensities.Count) max_i = intensities.Count - 1;
                float local_intensity_threshold = ((float)intensity_sums[max_i] - (float)intensity_sums[min_i]) / (max_i - min_i);

                float intensity = (float)intensities[i];
                float diff = intensity - local_intensity_threshold;
                diff *= diff;
                if (intensity > local_intensity_threshold)
                {
                    mean_light += diff;
                    light_hits++;
                }
                else
                {
                    mean_dark += diff;
                    dark_hits++;
                }
            }
            if (light_hits > 0) mean_light = (float)Math.Sqrt(mean_light / light_hits);
            if (dark_hits > 0) mean_dark = (float)Math.Sqrt(mean_dark / dark_hits);
            variance = mean_light + mean_dark;
            mean_light += av_intensity;
            mean_dark = av_intensity - mean_dark;

            return (av_intensity);
        }


        /// <summary>
        /// returns the average pixel intensity for the given line
        /// </summary>
        /// <param name="img">image to be returned</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="x1">top x</param>
        /// <param name="y1">top y</param>
        /// <param name="x2">bottom x</param>
        /// <param name="y2">bottom y</param>
        /// <param name="linewidth">line width</param>
        public static float averageLineIntensity(Byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                                 int x1, int y1, int x2, int y2, int linewidth)
        {
            float av_intensity = 0;
            int hits = 0;

            if (img != null)
            {
                int w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
                float m;

                dx = x2 - x1;
                dy = y2 - y1;
                w = Math.Abs(dx);
                h = Math.Abs(dy);
                if (x2 >= x1) step_x = 1; else step_x = -1;
                if (y2 >= y1) step_y = 1; else step_y = -1;

                if ((w < img_width) && (h < img_height))
                {
                    if (w > h)
                    {
                        if (dx != 0)
                        {
                            m = dy / (float)dx;
                            x = x1;
                            int s = 0;
                            while (s * Math.Abs(step_x) <= Math.Abs(dx))
                            {
                                y = (int)(m * (x - x1)) + y1;

                                for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                                    for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                                    {
                                        if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                        {
                                            int n = ((img_width * yy2) + xx2) * bytes_per_pixel;
                                            if ((n >= 0) && (n < img.Length - 3))
                                            {
                                                for (int col = 0; col < bytes_per_pixel; col++)
                                                    av_intensity += img[n + col];
                                                hits += bytes_per_pixel;
                                            }
                                        }
                                    }

                                x += step_x;
                                s++;
                            }
                        }
                    }
                    else
                    {
                        if (dy != 0)
                        {
                            m = dx / (float)dy;
                            y = y1;
                            int s = 0;
                            while (s * Math.Abs(step_y) <= Math.Abs(dy))
                            {
                                x = (int)(m * (y - y1)) + x1;
                                for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                                    for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                                    {
                                        if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                        {
                                            int n = ((img_width * yy2) + xx2) * bytes_per_pixel;
                                            if ((n >= 0) && (n < img.Length - 3))
                                            {
                                                for (int col = 0; col < bytes_per_pixel; col++)
                                                    av_intensity += img[n + col];
                                                hits += bytes_per_pixel;
                                            }
                                        }
                                    }

                                y += step_y;
                                s++;
                            }
                        }
                    }
                }
            }
            if (hits > 0) av_intensity /= hits;
            return (av_intensity);
        }

        #endregion

        #region "sub image of a larger image"

        /// <summary>
        /// returns a sub image from a larger image
        /// </summary>
        /// <param name="img">large image</param>
        /// <param name="img_width">width of the large image</param>
        /// <param name="img_height">height of the large image</param>
        /// <param name="bytes_per_pixel">bytes per pixel</param>
        /// <param name="tx">sub image top left x</param>
        /// <param name="ty">sub image top left y</param>
        /// <param name="bx">sub image bottom right x</param>
        /// <param name="by">sub image bottom right y</param>
        /// <returns></returns>
        public static byte[] createSubImage(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                            int tx, int ty, int bx, int by)
        {
            int sub_width = bx - tx;
            int sub_height = by - ty;
            byte[] subimage = new byte[img_width * img_height * bytes_per_pixel];
            for (int y = 0; y < sub_height; y++)
            {
                for (int x = 0; x < sub_width; x++)
                {
                    int n1 = ((y * sub_width) + x) * bytes_per_pixel;
                    int n2 = (((y+ty) * img_width) + (x+tx)) * bytes_per_pixel;

                    if ((n2 > -1) && (n2 < img.Length - 3))
                    {
                        for (int col = 0; col < bytes_per_pixel; col++)
                            subimage[n1 + col] = img[n2 + col];
                    }
                }
            }
            return (subimage);
        }

        /// <summary>
        /// returns a sub image from a larger image
        /// </summary>
        /// <param name="img">large image</param>
        /// <param name="img_width">width of the large image</param>
        /// <param name="img_height">height of the large image</param>
        /// <param name="bytes_per_pixel">bytes per pixel</param>
        /// <param name="tx">sub image top left x</param>
        /// <param name="ty">sub image top left y</param>
        /// <param name="bx">sub image bottom right x</param>
        /// <param name="by">sub image bottom right y</param>
        /// <returns></returns>
        public static byte[] cropImage(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                       int tx, int ty, int bx, int by,
                                       ref byte[] buffer)
        {
            int sub_width = bx - tx;
            int sub_height = by - ty;            
            if (buffer == null)
                buffer = new byte[sub_width * sub_height * bytes_per_pixel];
            if (buffer.Length != sub_width * sub_height * bytes_per_pixel)
                buffer = new byte[sub_width * sub_height * bytes_per_pixel];

            byte[] subimage = buffer;
            for (int y = 0; y < sub_height; y++)
            {
                for (int x = 0; x < sub_width; x++)
                {
                    int n1 = ((y * sub_width) + x) * bytes_per_pixel;
                    int n2 = (((y + ty) * img_width) + (x + tx)) * bytes_per_pixel;

                    if ((n2 > -1) && (n2 < img.Length - bytes_per_pixel))
                    {
                        for (int col = 0; col < bytes_per_pixel; col++)
                            subimage[n1 + col] = img[n2 + col];
                    }
                }
            }
            return (subimage);
        }

        /// <summary>
        /// returns a sub image from a larger image
        /// </summary>
        /// <param name="bmp">bitmap</param>
        /// <param name="tx">sub image top left x</param>
        /// <param name="ty">sub image top left y</param>
        /// <param name="bx">sub image bottom right x</param>
        /// <param name="by">sub image bottom right y</param>
        /// <returns>cropped version of the bitmap</returns>
        public static Bitmap cropBitmap(Bitmap bmp,
                                        int tx, int ty, int bx, int by,
                                        ref byte[] buffer, ref Bitmap bmp_buffer)
        {
            byte[] bmp_data = null;
            int bytes_per_pixel;
            if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                bmp_data = new byte[bmp.Width * bmp.Height * 3];
                bytes_per_pixel = 3;
            }
            else
            {
                bmp_data = new byte[bmp.Width * bmp.Height];
                bytes_per_pixel = 1;
            }

            BitmapArrayConversions.updatebitmap(bmp, bmp_data);
            byte[] cropped = cropImage(bmp_data, bmp.Width, bmp.Height, bytes_per_pixel,
                                       tx, ty, bx, by, ref buffer);
            int w = bx - tx;
            int h = by - ty;
            
            if (bmp_buffer == null)
                bmp_buffer = new Bitmap(w, h, bmp.PixelFormat);
            if ((bmp_buffer.Width != w) || (bmp_buffer.Height != h))
                bmp_buffer = new Bitmap(w, h, bmp.PixelFormat);
            Bitmap bmp_cropped = bmp_buffer;
            BitmapArrayConversions.updatebitmap_unsafe(cropped, bmp_cropped);
            return (bmp_cropped);
        }
        

        #endregion

        #region "non-maximal supression"

        /// <summary>
        /// perform non-maximal supression on the given data
        /// </summary>
        /// <param name="data">2D array containing data</param>
        /// <param name="supression_radius">local suppression redius</param>
        /// <param name="minimum_threshold">minimum value</param>
        /// <returns>list of points having locally maximal responses</returns>
        public static ArrayList NonMaximalSupression(float[,] data,
                                                     int supression_radius,
                                                     float minimum_threshold)
        {
            return(NonMaximalSupression(data, supression_radius, supression_radius, minimum_threshold));
        }


        /// <summary>
        /// perform non-maximal supression on the given 2D data
        /// </summary>
        /// <param name="data">2D array containing data</param>
        /// <param name="supression_radius_horizontal">horizontal suppression redius</param>
        /// <param name="supression_radius_vertical">vertical suppression radius</param>
        /// <param name="minimum_threshold">minimum value</param>
        /// <returns>list of points having locally maximal responses</returns>
        public static ArrayList NonMaximalSupression(float[,] data,
                                                     int supression_radius_horizontal,
                                                     int supression_radius_vertical,
                                                     float minimum_threshold)
        {
            ArrayList results = new ArrayList();
            int width = data.GetLength(0);
            int height = data.GetLength(1);

            // move through the data array using a horizontal and vertical window
            for (int x = 0; x < width - supression_radius_horizontal; x += supression_radius_horizontal + 1)
            {
                for (int y = 0; y < height - supression_radius_vertical; y += supression_radius_vertical + 1)
                {
                    // locate the maximal response within the window
                    int cx = x;
                    int cy = y;
                    for (int dx = 0; dx <= supression_radius_horizontal; dx++)
                        for (int dy = 0; dy <= supression_radius_vertical; dy++)
                            if (data[cx, cy] < data[x + dx, y + dy])
                            {
                                cx = x + dx;
                                cy = y + dy;
                            }

                    // check that this is the best responder within the local area
                    bool failed = false;
                    int xx = cx - supression_radius_horizontal;
                    if (xx > -1)
                    {
                        while ((xx <= cx + supression_radius_horizontal) && (!failed))
                        {
                            if (xx < width)
                            {
                                int yy = cy - supression_radius_vertical;
                                if (yy > -1)
                                {
                                    while ((yy <= cy + supression_radius_vertical) && (!failed))
                                    {
                                        if (yy < height)
                                        {
                                            if (data[cx, cy] < data[xx, yy]) failed = true;
                                        }
                                        yy++;
                                    }
                                }
                            }
                            xx++;
                        }
                    }

                    if (!failed)
                    {
                        // is this above the minimum response threshold ?
                        if (data[cx, cy] > minimum_threshold)
                        {
                            // store the maxima position
                            results.Add(cx);
                            results.Add(cy);
                        }
                    }
                }
            }
            return (results);
        }

        /// <summary>
        /// perform non-maximal supression on the given 1D data
        /// </summary>
        /// <param name="data">2D array containing data</param>
        /// <param name="supression_radius">suppression redius</param>
        /// <param name="minimum_threshold">minimum value</param>
        /// <returns>list of points having locally maximal responses</returns>
        public static ArrayList NonMaximalSupression(float[] data,
                                                     int supression_radius,
                                                     float minimum_threshold)
        {
            ArrayList results = new ArrayList();
            int width = data.GetLength(0);

            // move through the data array using a horizontal and vertical window
            for (int x = 0; x < width - supression_radius; x += supression_radius + 1)
            {
                // locate the maximal response within the window
                int cx = x;
                for (int dx = 0; dx <= supression_radius; dx++)
                    if (data[cx] < data[x + dx])
                        cx = x + dx;

                // check that this is the best responder within the local area
                bool failed = false;
                int xx = cx - supression_radius;
                if (xx > -1)
                {
                    while ((xx <= cx + supression_radius) && (!failed))
                    {
                        if (xx < width)
                            if (data[cx] < data[xx]) failed = true;
                        xx++;
                    }
                }
                if (!failed)
                {
                    // is this above the minimum response threshold ?
                    if (data[cx] > minimum_threshold)
                        results.Add(cx);
                }
            }
            return (results);
        }


        #endregion

        #region "contrast enhance"

        /// <summary>
        /// enhances the contrast of the given mono image
        /// </summary>
        /// <param name="img_mono">mono image data</param>
        /// <param name="img_width">image width</param>
        /// <param name="img_height">image_height</param>
        /// <returns>contrast enhanced image</returns>
        public static byte[] ContrastEnhance(byte[] img_mono, 
                                             int img_width, int img_height)
        {
            byte[] enhanced = new byte[img_width * img_height];

            // create a histogram
            int[] histogram = new int[(255 / 2) + 1];
            for (int i = 0; i < img_mono.Length; i++)
            {
                int bucket = img_mono[i] / 2;
                histogram[bucket]++;
            }

            // find the min and max intensity from the histogram
            int max = 0;
            for (int i = 0; i < histogram.Length - 1; i++)
                if (histogram[i] > max) max = histogram[i];
            int min = max * 5 / 100;
            if (min < 1) min = 1;

            int min_intensity = 0;
            for (int i = 0; i < histogram.Length - 1; i++)
            {
                if (histogram[i] < min)
                    min_intensity = i * 2;
                else
                    break;
            }

            int max_intensity = 255;
            for (int i = histogram.Length - 2; i >= 0; i--)
            {
                if (histogram[i] < min)
                    max_intensity = i * 2;
                else
                    break;
            }

            // re-scale within the range
            float range = max_intensity - min_intensity;
            if (range > 200)
            {
                // if the range is already large then don't bother enhancing
                enhanced = img_mono;
            }
            else
            {
                if (range > 1)
                {
                    int n = 0;
                    for (int y = 0; y < img_height; y++)
                    {
                        for (int x = 0; x < img_width; x++)
                        {
                            if (img_mono[n] >= min_intensity)
                            {
                                if (img_mono[n] <= max_intensity)
                                {
                                    // get a floating point value for the pixel
                                    float pixel_value = img_mono[n];
                                    if (x > 0) pixel_value += (img_mono[n - 1] * 0.2f);
                                    if (y > 0) pixel_value += (img_mono[n - img_width] * 0.2f);
                                    if (x < img_width - 1) pixel_value += (img_mono[n + 1] * 0.2f);
                                    if (y < img_height - 1) pixel_value += (img_mono[n + img_width] * 0.2f);
                                    pixel_value /= 1.8f;

                                    // map into the new range
                                    pixel_value = (pixel_value - min_intensity) / range;
                                    if (pixel_value < 0) pixel_value = 0;
                                    if (pixel_value > 255) pixel_value = 255;

                                    enhanced[n] = (byte)pixel_value;
                                }
                                else enhanced[n] = 255;
                            }
                            else enhanced[n] = 0;
                            n++;
                        }
                    }
                }
            }
            return (enhanced);
        }

        #endregion
        
        #region "detection of a light lobe"
        
        /// <summary>
        /// detects and corrects for radial illumination variation across the image (light lobe)
        /// </summary>
        /// <param name="img_mono">
        /// the raw mono image <see cref="System.Byte"/>
        /// </param>
        /// <param name="img_width">
        /// width of the image <see cref="System.Int32"/>
        /// </param>
        /// <param name="img_height">
        /// height of the image <see cref="System.Int32"/>
        /// </param>
        /// <param name="centre_x">
        /// x centre of the image <see cref="System.Int32"/>
        /// </param>
        /// <param name="centre_y">
        /// y centre of the image <see cref="System.Int32"/>
        /// </param>
        /// <param name="gain">
        /// gain value applied to the correction <see cref="System.Single"/>
        /// </param>
        /// <param name="pixel_distances">
        /// a lookup table of the distance of each pixel in the image from the centre position <see cref="System.Int32"/>
        /// </param>
        /// <param name="histogram">
        /// histogram of radial illumination variation <see cref="System.Single"/>
        /// </param>
        /// <param name="img_corrected">
        /// the corrected mono image <see cref="System.Byte"/>
        /// </param>
        public static void DetectLightLobe(byte[] img_mono, int img_width, int img_height,
                                           int centre_x, int centre_y, float gain,
                                           ref int[] pixel_distances,
                                           ref float[] histogram,
                                           ref byte[] img_corrected)
        {
            const int step_size = 1;
            bool regenerate_arrays = false;
            if (pixel_distances != null)
            {
                // if the image has changed size then the
                // arrays need to be regenerated
                if (pixel_distances.Length != img_width * img_height)
                    regenerate_arrays = true;
            }
            
            if ((pixel_distances == null) || (regenerate_arrays))
            {
                // create a distance lookup table
                // for each pixel this calculates the distance to the centre
                // of the image
                pixel_distances = new int[img_width * img_height];
                
                int n = 0;
                for (int y = 0; y < img_height; y++)
                {
                    int dy = y - centre_y;
                    for (int x = 0; x < img_width; x++)
                    {                        
                        int dx = x - centre_x;
                        pixel_distances[n++] = (int)Math.Sqrt((dx * dx) + (dy * dy));
                    }
                }
                
                if (img_width > img_height)
                    histogram = new float[img_width];
                else
                    histogram = new float[img_height];
            }
            
            // clear the histogram
            for (int i = histogram.Length-1; i >= 0; i--)
                histogram[i] = 0;
            
            int[] histogram_hits = new int[histogram.Length];
            
            for (int i = img_mono.Length-1; i >= 0; i-=step_size)
            {
                int distance_from_centre = pixel_distances[i];
                histogram[distance_from_centre] += img_mono[i];
                histogram_hits[distance_from_centre]++;
            }

            // average values
            int length = histogram.Length;
            for (int r = 0; r < histogram.Length; r++)
            {
                float average_intensity = 0;
                if (histogram_hits[r] > 0)
                    average_intensity = histogram[r] / histogram_hits[r];
                
                histogram[r] = average_intensity;
                
                if (histogram_hits[r] == 0)
                {
                    if (r > step_size+1)
                    {
                        length = r;
                        r = histogram.Length;
                    }
                }
            }
            
            // smooth the histogram
            int halfway = length / 2;
            float av_near = 0;
            float av_far = 0;
            float max_intensity = histogram[0];            
            for (int r = 2; r < histogram.Length-2; r++)
            {
                histogram[r] = (histogram[r+1] + histogram[r+2] + histogram[r-1] + histogram[r-2] + histogram[r]) * 0.2f;
                if (histogram[r] > max_intensity) max_intensity = histogram[r];
                
                if (r < halfway)
                    av_near += histogram[r];
                else
                    av_far += histogram[r];

                if (histogram_hits[r] == 0) r = histogram.Length;
            }
            
            // check that we have higher intensities towards
            // the centre.  If not then just return the original uncorrected image
            if (av_far < av_near * 6/10)
            {
                float gain_value = 1.0f / (1.0f + gain);

                // precompute multipliers, to avoid having to do divisions later on
                float[] mult = new float[histogram.Length];
                for (int i = 0; i < mult.Length; i++)
                    mult[i] = max_intensity * gain_value / histogram[i];
                
                if (img_corrected == null)
                    img_corrected = new byte[img_width * img_height];
                
                // produce the corrected image
                if (max_intensity < 1) max_intensity = 1;
                float multiplier;
                for (int i = img_mono.Length-1; i >= 0; i--)
                {
                    int distance_from_centre = pixel_distances[i];
                    float average_radial_intensity = histogram[distance_from_centre];                     
                    
                    multiplier = gain_value;
                    if (average_radial_intensity > 0)
                        multiplier = mult[distance_from_centre];

                    float variance = (img_mono[i] - average_radial_intensity) * multiplier;
                    int corrected_pixel_intensity = (int)(127 + variance);
                    if (corrected_pixel_intensity > 255) corrected_pixel_intensity = 255;
                    if (corrected_pixel_intensity < 0) corrected_pixel_intensity = 0;
                    img_corrected[i] = (byte)corrected_pixel_intensity;
                }
            }
            else
            {
                img_corrected = img_mono;
            }
        }
        
        #endregion

        #region "detecting blank frames"

        /// <summary>
        /// returns true if the given image is blank
        /// </summary>
        /// <param name="img">image data to be examined</param>
        /// <param name="step_size">step size with which to sample the image</param>
        /// <returns>true if the image is blank</returns>
        public static bool IsBlank(byte[] img, int step_size)
        {
            bool is_blank = true;
            int i = 0;
            while ((i < img.Length) && (is_blank))
            {
                if (img[i] > 0) is_blank = false;
                i += step_size;
            }
            return (is_blank);
        }

        /// <summary>
        /// returns true if the given bitmap is blank
        /// </summary>
        /// <param name="bmp">bitmap to examine</param>
        /// <param name="bmp_data">temporary buffer</param>
        /// <param name="step_size">step size with which to sample the image</param>
        /// <returns>true if the bitmap is blank</returns>
        public static bool IsBlank(Bitmap bmp, byte[] bmp_data, int step_size)
        {
            BitmapArrayConversions.updatebitmap(bmp, bmp_data);
            return (IsBlank(bmp_data, step_size));
        }

        #endregion
    }
}
