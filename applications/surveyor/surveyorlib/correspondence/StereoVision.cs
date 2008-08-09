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
    
        public int algorithm_type;
    
        // stereo features detected
        public List<StereoFeature> features;
        
        // determines the size of stereo features displayed as dots
        public float feature_scale = 0.2f;
        
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
            
            Update(img[0], img[1], rectified_left.Width, rectified_left.Height,
                   calibration_offset_x, calibration_offset_y);
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
            Buffer.BlockCopy(left_bmp, 0, output_bmp, 0, left_bmp.Length);
            
            for (int i = 0; i < features.Count; i++)
            {
                StereoFeature f = features[i];
                drawing.drawSpot(output_bmp, image_width, image_height, (int)f.x, (int)f.y, (int)(f.disparity*feature_scale), 0, 255, 0);
            }
        }
        
        public virtual void Show(ref Bitmap output)
        {
            byte[] output_img = (byte[])img[0].Clone();
            if (output == null)
                output = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Show(img[0], image_width, image_height, output_img);            
            BitmapArrayConversions.updatebitmap_unsafe(output_img, output);
        }
        
    }
}
