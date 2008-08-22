/*
    dense stereo correspondence
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
using System.Drawing;
using System.IO;
using System.Collections;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// contour based stereo correspondence algorithm
    /// </summary>
    public class StereoVisionDense : StereoVision
    {
        public double max_difference = 10000000000;
        public double min_difference = 1000;
        public double a = 20;
        public double[] frequency = { 0.02, 0.04 };
        public int vertical_compression = 4;
        public int minimum_intensity = 40;
        public int no_of_masks = 10;
        public int position_search_radius = 10;

        public byte[] disparity_map;

        #region "constructors"

        /// <summary>
        /// constructor
        /// This sets some initial default values.
        /// </summary>
        public StereoVisionDense()
        {
            algorithm_type = DENSE;
            convert_to_mono = true;
        }

        #endregion
        
        #region "conversion to mono"
        
        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="conversion_type">method for converting to mono</param>
        /// <returns></returns>
        private static unsafe byte[] monoImage(byte[] img_colour, int img_width, int img_height,
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

                //fixed (byte* unsafe_mono_image = mono_image)
                {
                    //fixed (byte* unsafe_img_colour = img_colour)
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
                                            tot += img_colour[i + col];
                                        }
                                        mono_image[n] = (byte)(tot / 3);
                                        break;
                                    }
                                case 1: // luminance
                                    {
                                        luminence = ((img_colour[i + 2] * 299) +
                                                     (img_colour[i + 1] * 587) +
                                                     (img_colour[i] * 114)) / 1000;
                                        //if (luminence > 255) luminence = 255;
                                        mono_image[n] = (byte)luminence;
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
        private static byte[] monoImage(byte[] img_colour, int img_width, int img_height)
        {
            return (monoImage(img_colour, img_width, img_height, 0, null));
        }
                
        #endregion
        
        #region "gabor filters"


        public static void ShowGaborFilter(byte[] img, int image_width, int image_height,
                                           int tx, int ty, int bx, int by,
                                           double a, double b, double frequency, 
                                           double phase_degrees, double orientation_degrees)
        {
            double mean = 0;
            double[,] mask = GaborFilter(a, b, frequency, phase_degrees, orientation_degrees, ref mean);

            int mw = mask.GetLength(0)-1;
            int mh = mask.GetLength(1)-1;

            int w = bx - tx;
            int h = by - ty;
            for (int y = ty; y < by; y++)
            {
                int mask_y = (y - ty) * mh / h;
                for (int x = tx; x < bx; x++)
                {
                    int n = ((y * image_width) + x) * 3;
                    int mask_x = (x - tx) * mw / w;
                    double v = mask[mask_x, mask_y];
                    v = v * 255 * 2;
                    if (v < 0)
                    {
                        if (v < -255) v = -255;
                        img[n] = 0;
                        img[n + 1] = 0;
                        img[n + 2] = (byte)(-v);
                    }
                    else
                    {
                        if (v > 255) v = 255;
                        img[n] = (byte)v;
                        img[n + 1] = 0;
                        img[n + 2] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// creates a bank of 2D gabor filters
        /// </summary>
        /// <param name="increments">number of masks in the filter bank</param>
        /// <param name="start_a"></param>
        /// <param name="start_b"></param>
        /// <param name="start_frequency"></param>
        /// <param name="start_phase_degrees"></param>
        /// <param name="start_orientation_degrees"></param>
        /// <param name="end_a"></param>
        /// <param name="end_b"></param>
        /// <param name="end_frequency"></param>
        /// <param name="end_phase_degrees"></param>
        /// <param name="end_orientation_degrees"></param>
        /// <returns>filter bank</returns>
        private static double[][,] GaborFilterBank(int increments,
                                                  double start_a, double start_b,
                                                  double start_frequency,
                                                  double start_phase_degrees,
                                                  double start_orientation_degrees,
                                                  double end_a, double end_b,
                                                  double end_frequency,
                                                  double end_phase_degrees,
                                                  double end_orientation_degrees)
        {
            double[][,] filters = new double[increments][,];

            for (int i = 0; i < increments; i++)
            {
                double mean = 0;
                filters[i] = GaborFilter(start_a + (i * (end_a - start_a) / increments),
                                         start_b + (i * (end_b - start_b) / increments),
                                         start_frequency + (i * (end_frequency - start_frequency) / increments),
                                         start_phase_degrees + (i * (end_phase_degrees - start_phase_degrees) / increments),
                                         start_orientation_degrees + (i * (end_orientation_degrees - start_orientation_degrees) / increments),
                                         ref mean);
            }
            return (filters);
        }

        /// <summary>
        /// creates a bank of 1D gabor filters
        /// </summary>
        /// <param name="increments">number of masks in the filter bank</param>
        /// <param name="start_a"></param>
        /// <param name="start_b"></param>
        /// <param name="start_frequency"></param>
        /// <param name="start_phase_degrees"></param>
        /// <param name="end_a"></param>
        /// <param name="end_b"></param>
        /// <param name="end_frequency"></param>
        /// <param name="end_phase_degrees"></param>
        /// <returns>filter bank</returns>
        private static double[][] GaborFilterBank(int increments,
                                                 double start_a, double start_b,
                                                 double start_frequency,
                                                 double start_phase_degrees,
                                                 double end_a, double end_b,
                                                 double end_frequency,
                                                 double end_phase_degrees)
        {
            double[][] filters = new double[increments][];

            for (int i = 0; i < increments; i++)
            {
                double mean = 0;
                filters[i] = GaborFilter1D(start_a + (i * (end_a - start_a) / increments),
                                           start_b + (i * (end_b - start_b) / increments),
                                           start_frequency + (i * (end_frequency - start_frequency) / increments),
                                           start_phase_degrees + (i * (end_phase_degrees - start_phase_degrees) / increments),
                                           ref mean);
            }
            return (filters);
        }

        /// <summary>
        /// returns a 2D mask for a gabor filter
        /// </summary>
        /// <see cref="http://mplab.ucsd.edu/tutorials/pdfs/gabor.pdf"/>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="frequency"></param>
        /// <param name="phase_degrees"></param>
        /// <param name="orientation_degrees"></param>
        /// <param name="mean"></param>
        /// <returns>2D mask</returns>
        private static double[,] GaborFilter(double a, double b, 
                                            double frequency, 
                                            double phase_degrees, 
                                            double orientation_degrees,
                                            ref double mean)
        {
            double orientation = orientation_degrees * Math.PI / 180f;
            double phase = phase_degrees * Math.PI / 180f;
            int size;
            if (a > b)
                size = (int)a;
            else
                size = (int)b;
            size *= 2;
            size++;

            double[,] weight = new double[size, size];
            double[,] mask = new double[size, size];

            int xx = 0;
            for (int x = -size/2; x <= size / 2; x++) 
            {
                int yy = 0;
                for (int y = -size/2; y <= size / 2; y++) 
                {
                    double radius = Math.Sqrt(x * x + y * y);
                    double theta = Math.Atan2(y, x);

                    double xp = x * Math.Cos(orientation) + y * Math.Sin(orientation);
                    double yp = y * Math.Cos(orientation) - x * Math.Sin(orientation);
                    double v = Math.Exp((-1 * Math.PI * (xp * xp / a / a + yp * yp / b / b)));
                    weight[xx, yy] = v;
                    mask[xx, yy] = v * Math.Sin(phase + (2 * Math.PI * frequency * radius * Math.Cos(theta - orientation)));
                    yy++;
                }
                xx++;
            }
  
            //calculate mean
            double sum = 0;
            double weightSum = 0;
            for (int c = 0; c < size; c++) 
            {
                for (int d = 0; d < size; d++) 
                {
                    sum += mask[c, d];
                    weightSum += weight[c, d];
                }
            }
            mean = sum / weightSum;
            return (mask);
        }

        /// <summary>
        /// returns a 1D mask for a gabor filter
        /// </summary>
        /// <see cref="http://mplab.ucsd.edu/tutorials/pdfs/gabor.pdf"/>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="frequency"></param>
        /// <param name="phase_degrees"></param>
        /// <param name="mean"></param>
        /// <returns>2D mask</returns>
        private static double[] GaborFilter1D(double a, double b,
                                             double frequency,
                                             double phase_degrees,
                                             ref double mean)
        {
            double orientation = 0;
            double phase = phase_degrees * Math.PI / 180f;
            int size;
            if (a > b)
                size = (int)a;
            else
                size = (int)b;
            size *= 2;
            size++;

            double[] weight = new double[size];
            double[] mask = new double[size];

            int y = 0;
            int xx = 0;
            for (int x = -size / 2; x <= size / 2; x++)
            {
                double radius = Math.Sqrt(x * x);
                double theta = Math.Atan2(y, x);

                double xp = x * Math.Cos(orientation) + y * Math.Sin(orientation);
                double yp = y * Math.Cos(orientation) - x * Math.Sin(orientation);
                double v = Math.Exp((-1 * Math.PI * (xp * xp / a / a + yp * yp / b / b)));
                weight[xx] = v;
                mask[xx] = v * Math.Sin(phase + (2 * Math.PI * frequency * radius * Math.Cos(theta - orientation)));
                xx++;
            }

            //calculate mean
            double sum = 0;
            double weightSum = 0;
            for (int c = 0; c < size; c++)
            {
                sum += mask[c];
                weightSum += weight[c];
            }
            mean = sum / weightSum;
            return (mask);
        }

        #endregion
        
        #region "convolution"
        
        public static void ConvolveRow(byte[] mono_img, int image_width, int image_height,
                                       int y, double[][] filter,
                                       double[][] result,
                                       bool clear,
                                       bool anticorrelated)
        {
            int bank, x, f, offset_x, intensity, n2;
            
            // clear the result
            if (clear)
                for (bank = result.Length-1; bank >= 0; bank--)
                    for (int i = result[0].Length - 1; i >= 0; i--)
                        result[bank][i] = 0;

            int half_filter_width = filter[0].Length / 2;
            int filter_width = filter[0].Length;
            int no_of_banks = result.Length - 1;
            
            int n = (y * image_width) + half_filter_width;
            int min_offset = -half_filter_width;
            int max_offset = min_offset + filter_width;
            int stride = mono_img.Length;
            double[] result2, filter2;
            
            for (x = half_filter_width; x < image_width - half_filter_width - 1; x++, n++)
            {                
                if ((n + min_offset > -1) && (n + max_offset < stride))
                {                    
                    for (bank = no_of_banks; bank >= 0; bank--)                    
                    {
                        f = 0;
                        n2 = n + min_offset;
                        result2 = result[bank];
                        filter2 = filter[bank];
                        
                        for (offset_x = min_offset; offset_x < max_offset; offset_x++, f++, n2++)
                        {
                            if (!anticorrelated)
                                result2[x] += mono_img[n2] * filter2[f];
                            else
                                result2[x] += (255 - mono_img[n2]) * filter2[f];
                        }                    
                    }
                }
            }
        }
                
        #endregion
		
		#region "matching"
		
        protected static int PointSimilarity1D(int left_y, int right_y, byte[] left_img, byte[] right_img, int image_width,
                                               int left_x, int right_x, int radius)
        {            
            int diff, ssd = 1;
            int n1 = (left_y * image_width) + left_x - radius;
            int n2 = (right_y * image_width) + right_x - radius;
            if ((n1 >-1) && (n2 > -1) &&
                (n1 + radius*2 < left_img.Length) &&
                (n2 + radius*2 < right_img.Length))
            {
                for (int x = (radius*2)-1; x >= 0; x--, n1++, n2++)
                {
                    diff = left_img[n1] - right_img[n2];
                    ssd += diff * diff;
                }
            }
            else ssd = int.MaxValue;
            return(ssd);
        }

        private static void MatchRowAnticorrelated(int y, byte[] left_img, byte[] right_img,
                                    double[][] left, double[][] right, int border,
                                    int[] disparity, 
                                    int max_disparity_pixels,
                                    double min_difference,
                                    double max_difference,
                                    int position_search_radius,
                                    int calibration_offset_x,
                                    int calibration_offset_y,
                                    int minimum_intensity)
        {
            int image_width = disparity.Length;
            int no_of_banks = left.Length;
            int d, b, posn_diff, best_disparity, x2;
            int y2 = y+calibration_offset_y;
            double v, d1, max, best_diff;

            int start_x = image_width/2;
            int end_x = left[0].Length - 1;
            for (int x = end_x; x >= start_x; x--)
            {
                disparity[x] = 0;
                if (left_img[(y * image_width) + x] > minimum_intensity)
                {                
                    best_disparity = 0;
                    best_diff = max_difference;

                    max = min_difference;
                    for (d = 0; d < max_disparity_pixels; d++)
                    {
                        x2 = x - d + calibration_offset_x;
                        if (x2 > position_search_radius)
                        {
                            posn_diff = 1 + PointSimilarity1D(y, y2, left_img, right_img, disparity.Length,
                                                              x, x2, position_search_radius);
                                                              
                            v = 0;
                            for (b = no_of_banks-1; b >= 0; b--)
                            {
                                d1 = left[b][x] - right[b][x2];
                                v += d1 * d1;
                            }
                            v /= posn_diff;
                            if (v > max)
                            {
                                max = v;
                                best_disparity = d;
                            }
                        }
                    }
                    
                    disparity[x] = best_disparity;
                }
            }

            start_x = position_search_radius;
            end_x = disparity.Length/2;
            for (int x = end_x; x >= start_x; x--)
            {
                disparity[x] = 0;
                if (right_img[(y * image_width) + x] > minimum_intensity)
                {
                    best_disparity = 0;
                    best_diff = max_difference;

                    max = min_difference;
                    for (d = 0; d < max_disparity_pixels; d++)
                    {
                        x2 = x + d - calibration_offset_x;
                        if (x2 > position_search_radius)
                        {
                            posn_diff = 1 + PointSimilarity1D(y, y2, left_img, right_img, disparity.Length,
                                                              x2, x, position_search_radius);
                                                              
                            v = 0;
                            for (b = no_of_banks-1; b >= 0; b--)
                            {
                                d1 = left[b][x2] - right[b][x];
                                v += d1 * d1;
                            }
                            v /= posn_diff;
                            if (v > max)
                            {
                                max = v;
                                best_disparity = d;
                            }
                        }
                    }
                    disparity[x] = best_disparity;
                }
            }

        }

        public static void MatchImages(string left_filename, string right_filename,
                                       int no_of_masks,
                                       double a, double b,
                                       double min_phase_degrees, double max_phase_degrees,
                                       double[] frequency,
                                       int max_disparity,                                        
                                       double min_difference, double max_difference,
                                       string disparity_map_filename,
                                       int calibration_offset_x,
                                       int calibration_offset_y,
                                       int vertical_compression,
                                       int position_search_radius,
                                       int minimum_intensity)
        {
            // load images
            Bitmap left_bmp = (Bitmap)Bitmap.FromFile(left_filename);
            Bitmap right_bmp = (Bitmap)Bitmap.FromFile(right_filename);

            // extract data
            byte[] left_img_colour = new byte[left_bmp.Width * left_bmp.Height * 3];
            byte[] right_img_colour = new byte[right_bmp.Width * right_bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(left_bmp, left_img_colour);
            BitmapArrayConversions.updatebitmap(right_bmp, right_img_colour);

            // convert to mono
            byte[] left_img = monoImage(left_img_colour, left_bmp.Width, left_bmp.Height);
            byte[] right_img = monoImage(right_img_colour, right_bmp.Width, right_bmp.Height);

            byte[] disparity_map = new byte[left_bmp.Width * left_bmp.Height * 3];

            MatchImages(left_img, right_img, left_bmp.Width, left_bmp.Height,
                        no_of_masks, a, b,
                        min_phase_degrees, max_phase_degrees,
                        frequency,
                        max_disparity,                         
                        min_difference, max_difference, 
                        calibration_offset_x, calibration_offset_y,
                        vertical_compression,
                        position_search_radius,
                        minimum_intensity,
                        disparity_map);

            Bitmap disparity_bmp = new Bitmap(left_bmp.Width, left_bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(disparity_map, disparity_bmp);

            if (disparity_map_filename.ToLower().EndsWith("bmp"))
                disparity_bmp.Save(disparity_map_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (disparity_map_filename.ToLower().EndsWith("jpg"))
                disparity_bmp.Save(disparity_map_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (disparity_map_filename.ToLower().EndsWith("gif"))
                disparity_bmp.Save(disparity_map_filename, System.Drawing.Imaging.ImageFormat.Gif);
            if (disparity_map_filename.ToLower().EndsWith("png"))
                disparity_bmp.Save(disparity_map_filename, System.Drawing.Imaging.ImageFormat.Png);
        }
        
        static double[][][] masks;
        static double[][] left;
        static double[][] right;
        
        private static void MatchImages(byte[] left_img, byte[] right_img,
                                        int image_width, int image_height,
                                        int no_of_masks,
                                        double a, double b,
                                        double min_phase_degrees, double max_phase_degrees,
                                        double[] frequency,
                                        int max_disparity, 
                                        double min_difference, double max_difference,
                                        int calibration_offset_x, int calibration_offset_y,
                                        int vertical_compression,
                                        int position_search_radius,
                                        int minimum_intensity,
                                        byte[] disparity_map)
        {
            int max_disparity_pixels = max_disparity * image_width / 100;
            bool initialise = false;
            if (left == null)
                initialise = true;
            else
            {
                if (left.Length != no_of_masks) initialise = true;
                if (left[0].Length != image_width) initialise = true;
            }
            
            if (initialise)
            {
                left = new double[no_of_masks][];
                right = new double[no_of_masks][];
                for (int i = 0; i < no_of_masks; i++)
                {
                    left[i] = new double[image_width];
                    right[i] = new double[image_width];
                }
                masks = new double[frequency.Length][][];
                for (int f = 0; f < frequency.Length; f++)
                {
                    masks[f] = GaborFilterBank(no_of_masks, 
                                               a, b, frequency[f], min_phase_degrees,
                                               a, b, frequency[f], max_phase_degrees);
                }
            }

            if (masks[0].Length > image_width/4)
            {
                Console.WriteLine("Convolution masks too large");
            }
            else
            {
                int[] disparity = new int[image_width];
                int border = masks[0].Length / 2;

                int n;
                int compressed_y = 0;
                for (int y = 0; y < image_height; y += vertical_compression)
                {
                    int y_right = y + calibration_offset_y;
                    
                    if ((y_right > -1) && (y_right < image_height))
                    {
                        for (int f = 0; f < frequency.Length; f++)
                        {
                            ConvolveRow(left_img, image_width, image_height, y, masks[f], left, f == 0, false);
                            ConvolveRow(right_img, image_width, image_height, y_right, masks[f], right, f == 0, true);
                        }
                        MatchRowAnticorrelated(y, left_img, right_img, left, right, border, disparity, max_disparity_pixels, min_difference, max_difference, position_search_radius, calibration_offset_x, calibration_offset_y, minimum_intensity);

                        for (int yy = 0; yy < vertical_compression; yy++)
                        {
                            if (y + yy < image_height)
                            {
                                n = (y + yy) * image_width * 3;
                                for (int x = 0; x < image_width; x++, n += 3)
                                {
                                    byte disp = (byte)(disparity[x] * 255 / max_disparity_pixels);
                                    if (disp > 200) disp = 0;
                                    disparity_map[n] = disp;
                                    disparity_map[n + 1] = disp;
                                    disparity_map[n + 2] = disp;

                                }
                            }
                        }
                    }
                    compressed_y++;
                }
            }
        }
				
		#endregion
		
        #region "main update routine"

        /// <summary>
        /// main update routine for contour based stereo correspondence
        /// </summary>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right image data</param>
        /// <param name="wdth">width of the images</param>
        /// <param name="hght">height of the images</param>
        /// <param name="calibration_offset_x">calibration offset to counter for any small vergence angle between the cameras</param>
        /// <param name="calibration_offset_y">calibration offset to counter for any small vergence angle between the cameras</param>
        public override void Update(byte[] left_bmp, byte[] right_bmp,
                                    int wdth, int hght,
                                    float calibration_offset_x, float calibration_offset_y)
        {
            //Console.WriteLine("calib x: " + calibration_offset_x.ToString());
            //Console.WriteLine("calib y: " + calibration_offset_y.ToString());
            if (disparity_map == null)
                disparity_map = new byte[wdth * hght * 3];
            else
            {
                if (disparity_map.Length != wdth * hght * 3)
                    disparity_map = new byte[wdth * hght * 3];
            }

            const int min_phase_degrees = -90;
            const int max_phase_degrees = 90;
            double b = a * 30 / 100;

            MatchImages(left_bmp, right_bmp, wdth, hght,
                        no_of_masks, a, b,
                        min_phase_degrees, max_phase_degrees,
                        frequency, 
                        max_disparity,                         
                        min_difference, max_difference, 
                        (int)calibration_offset_x, (int)calibration_offset_y,
                        vertical_compression,
                        position_search_radius,
                        minimum_intensity,
                        disparity_map);
        }


        #endregion

        #region "display"

        public override void Show(ref Bitmap output)
        {
            byte[] output_img = null;
            
            if (img[0].Length == image_width * image_height * 3)
            {
                output_img = new byte[image_width*image_height];
            }
            else
            {
                output_img = new byte[image_width*image_height*3];
            }
            
            if (output == null)
                output = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            if (disparity_map != null)
                BitmapArrayConversions.updatebitmap_unsafe(disparity_map, output);
        }

        public static void ShowGaborFilter(string filename, int image_width, int image_height,
                                           double a, double b, double frequency, 
                                           double phase_degrees, double orientation_degrees)
        {
            Bitmap bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte[] img = new byte[image_width * image_height * 3];
            ShowGaborFilter(img, image_width, image_height,
                            0, 0, image_width - 1, image_height - 1,
                            a, b, frequency, phase_degrees, orientation_degrees);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.ToLower().EndsWith("jpg"))
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filename.ToLower().EndsWith("bmp"))
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.ToLower().EndsWith("png"))
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            if (filename.ToLower().EndsWith("gif"))
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
        }

        #endregion


    }


}