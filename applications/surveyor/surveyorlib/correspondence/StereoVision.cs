/*
    base class for stereo vision
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
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// base class for stereo correspondence
    /// </summary>
    public class StereoVision
    {
        public const int SIMPLE = 0;        
        public const int SIMPLE_COLOUR = 1;
        public const int CONTOURS = 2;
    
        public int algorithm_type;
    
        // stereo features detected
        public List<StereoFeature> features;
        
        // determines the size of stereo features displayed as dots
        public float feature_scale = 0.2f;
        
        // convert the image to mono befure calling the main update routine
        protected bool convert_to_mono;
        
        protected int image_width, image_height;
        protected byte[][] img = new byte[2][];        

        public StereoVision()
        {
            features = new List<StereoFeature>();
        }
    
        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right_image_data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        public virtual void Update(byte[] left_bmp, byte[] right_bmp,
                                   int image_width, int image_height,
                                   float calibration_offset_x, float calibration_offset_y)
        {
            this.image_width = image_width;
            this.image_height = image_height;
        }

        public void Update(Bitmap rectified_left, Bitmap rectified_right,
                           float calibration_offset_x, float calibration_offset_y)
        {
            image_width = rectified_left.Width;
            image_height = rectified_left.Height;
            int pixels = image_width * image_height * 3;
            
            if (img[0] == null)
            {
                img[0] = new byte[pixels];
                img[1] = new byte[pixels];
            }
            else
            {
                if (img[0].Length != pixels)
                {
                    img[0] = new byte[pixels];
                    img[1] = new byte[pixels];
                }
            }
            
            BitmapArrayConversions.updatebitmap(rectified_left, img[0]);
            BitmapArrayConversions.updatebitmap(rectified_right, img[1]);
            
            if (convert_to_mono)
            {
               monoImage(img[0], image_width, image_height, 1, ref img[0]);
               monoImage(img[1], image_width, image_height, 1, ref img[1]);
            }
            
            Update(img[0], img[1], rectified_left.Width, rectified_left.Height,
                   calibration_offset_x, calibration_offset_y);
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="conversion_type">method for converting to mono</param>
        /// <returns></returns>
        protected void monoImage(byte[] img_colour, int img_width, int img_height,
                                 int conversion_type, ref byte[] output)
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
                float luminence = 0;

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
                                mono_image[n] = (byte)(tot * 0.3333f);
                                break;
                            }
                        case 1: // luminance
                            {
                                luminence = ((img_colour[i + 2] * 299) +
                                             (img_colour[i + 1] * 587) +
                                             (img_colour[i] * 114)) * 0.001f;
                                //if (luminence > 255) luminence = 255;
                                mono_image[n] = (byte)luminence;
                                break;
                            }
                        case 2: // hue
                            {
                                int r = img_colour[i + 2];
                                int g = img_colour[i + 1];
                                int b = img_colour[i];
                                
                                int hue = 0;
                                int min = 255;
                                if ((r < g) && (r < b)) min = r;
                                if ((g < r) && (g < b)) min = g;
                                if ((b < g) && (b < r)) min = b;
                                
                                if ((r > g) && (r > b) && (r - min > 0))
                                    hue = (60 * (g - b) / (r - min)) % 360; 
                                if ((b > g) && (b > r) && (b - min > 0))
                                    hue = (60 * (b - r) / (b - min)) + 120;
                                if ((g > b) && (g > r) && (g - min > 0))
                                    hue = (60 * (r - g) / (g - min)) + 240;
                                hue = hue * 255 / 360;
                                
                                mono_image[n] = (byte)hue;                            
                                break;
                            }
                    }
                    n++;
                }
            }
            output = mono_image;
        }
        
        /// <summary>
        /// show results
        /// </summary>
        /// <param name="left_bmp">left image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="output_bmp">image to be displayed</param>
        public virtual void Show(byte[] left_bmp, int image_width, int image_height,
                                 byte[] output_bmp)
        {
            if (left_bmp.Length == output_bmp.Length)
            {
                Buffer.BlockCopy(left_bmp, 0, output_bmp, 0, left_bmp.Length);
            }
            else
            {
                if (left_bmp.Length == output_bmp.Length/3);
                {
                    int n = 0;
                    for (int i = 0; i < output_bmp.Length; i+=3, n++)
                    {
                        for (int col=0;col<3;col++)
                            output_bmp[i+col] = left_bmp[n];
                    }
                    
                }
            }
            
            for (int i = 0; i < features.Count; i++)
            {
                StereoFeature f = features[i];
                //drawing.drawSpot(output_bmp, image_width, image_height, (int)f.x, (int)f.y, (int)(f.disparity*feature_scale), 0, 255, 0);
                drawing.drawSpotBlended(output_bmp, image_width, image_height, (int)f.x, (int)f.y, (int)(f.disparity*feature_scale), 100, 255, 100);
            }
        }
        
        public virtual void Show(ref Bitmap output)
        {
            byte[] output_img = new byte[image_width * image_height  * 3];
            if (output == null)
                output = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Show(img[0], image_width, image_height, output_img);            
            BitmapArrayConversions.updatebitmap_unsafe(output_img, output);
        }
        
    }
}
