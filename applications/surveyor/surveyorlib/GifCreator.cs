/*
    class used for creating animated gifs
    Copyright (C) 2009 Bob Mottram
    fuzzgun@gmail.com
    based upon code written by Rick van den Bosh
    see http://bloggingabout.net/blogs/rick/archive/2005/05/10/3830.aspx

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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;

namespace sluggish.utilities
{
    public class GifCreator
    {
        public static void CreateFromStereoPair(
            string left_image,
            string right_image,
            string gif_filename,
            int delay_mS,
            float offset_x,
            float offset_y,
            float scale,
            float rotation_degrees)
        {
            string left_image_filename = "";
            string right_image_filename = "";

            CreateFromStereoPair(
                left_image,
                right_image,
                gif_filename,
                delay_mS,
                offset_x,
                offset_y,
                scale,
                rotation_degrees,
                ref left_image_filename,
                ref right_image_filename);
        }

        #region "creating lookup tables for image scaling"

		/// <summary>
		/// returns a lookup table in order to apply scaling, rotation and translation to a colour image
		/// </summary>
		/// <param name="scale">scale factor in the range 0.0 - 2.0</param>
		/// <param name="lookup">returned lookup table</param>
		/// <param name="img_width">image width</param>
		/// <param name="img_height">image height</param>
		/// <param name="offset_x">x offset the image</param>
		/// <param name="offset_y">y offset the image</param>
		/// <param name="rotation_degrees">rotate the image</param>
        public static void CreateScaledImageLookupColour(
            float scale,
            int[] lookup, 
            int img_width, int img_height,
            int offset_x,
            int offset_y,
            float rotation_degrees)
        {
            float rotation_radians = rotation_degrees * (float)Math.PI / 180.0f;

            int cy = img_height / 2;
            int cx = img_width / 2;
            int n = 0;
            for (int y = 0; y < img_height; y++)
            {
                int yy = cy + (int)((y - cy) * scale);
                for (int x = 0; x < img_width; x++, n += 3)
                {
                    int xx = cx + (int)((x - cx) * scale);

                    int xx2 = xx;
                    int yy2 = yy;
                    if (rotation_degrees != 0)
                    {
                        float dx = xx - cx;
                        float dy = yy - cy;
                        float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        float angle = 0;
                        if (dist != 0)
                        {
                            angle = (float)Math.Acos(dy / dist);
                            if (dx < 0) angle = (float)(Math.PI*2) - angle;
                        }
                        angle += rotation_radians;

                        xx2 = cx + (int)(dist * Math.Sin(angle));
                        yy2 = cy + (int)(dist * Math.Cos(angle));
                    }
					
					xx2 += offset_x;
					yy2 += offset_y;					

                    int n2 = ((yy2 * img_width) + xx2) * 3; 
                    if ((n2 > -1) && (n2 < lookup.Length - 3))
                    {
                        lookup[n] = n2;
                        lookup[n + 1] = n2 + 1;
                        lookup[n + 2] = n2 + 2;
                    }
                }
            }
        }

		/// <summary>
		/// returns a lookup table in order to apply scaling, rotation and translation to a mono image
		/// </summary>
		/// <param name="scale">scale factor in the range 0.0 - 2.0</param>
		/// <param name="lookup">returned lookup table</param>
		/// <param name="img_width">image width</param>
		/// <param name="img_height">image height</param>
		/// <param name="offset_x">x offset the image</param>
		/// <param name="offset_y">y offset the image</param>
		/// <param name="rotation_degrees">rotate the image</param>
        public static void CreateScaledImageLookupMono(
            float scale,
            int[] lookup,
            int img_width, int img_height,
            int offset_x,
            int offset_y,
            float rotation_degrees)
        {
            float rotation_radians = rotation_degrees * (float)Math.PI / 180.0f;
            float rotation_radians2 = rotation_radians + (float)Math.PI;

            int cy = img_height / 2;
            int cx = img_width / 2;
            int n = 0;
            for (int y = 0; y < img_height; y++)
            {
                int yy = cy + (int)((y - cy) * scale);
                for (int x = 0; x < img_width; x++, n++)
                {
                    int xx = cx + (int)((x - cx) * scale);

                    int xx2 = xx;
                    int yy2 = yy;
                    if (rotation_degrees != 0)
                    {
                        float dx = xx - cx;
                        float dy = yy - cy;
                        float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        float angle = 0;
                        if (dist != 0)
                        {
                            angle = (float)Math.Acos(dy / dist);
                            if (dx < 0) angle = (float)(Math.PI*2) - angle;
                        }
                        angle += rotation_radians;

                        xx2 = cx + (int)(dist * Math.Sin(angle));
                        yy2 = cy + (int)(dist * Math.Cos(angle));
                    }
					
					xx2 += offset_x;
					yy2 += offset_y;

                    int n2 = (yy2 * img_width) + xx2; 
                    if ((n2 > -1) && (n2 < lookup.Length))
                        lookup[n] = n2;
                }
            }
        }

        #endregion

        public static void CreateFromStereoPair(
            string left_image,
            string right_image,
            string gif_filename,
            int delay_mS,
            float offset_x,
            float offset_y,
            float scale,
            float rotation_degrees,
            ref string left_image_filename,
            ref string right_image_filename)
        {
            int n = 0;
            left_image_filename = left_image;
            right_image_filename = right_image;

            if (scale < 1)
            {
                Bitmap bmp0 = (Bitmap)Bitmap.FromFile(left_image);
                byte[] img0 = new byte[bmp0.Width * bmp0.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp0, img0);
                int[] lookup0 = new int[bmp0.Width * bmp0.Height * 3];
                byte[] img0_scaled = new byte[bmp0.Width * bmp0.Height * 3];
                CreateScaledImageLookupColour(scale, lookup0, bmp0.Width, bmp0.Height, 0, 0, 0);
                for (int i = 0; i < img0_scaled.Length; i++) img0_scaled[i] = img0[lookup0[i]];
                scale = 1;
                bmp0 = new Bitmap(bmp0.Width, bmp0.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(img0_scaled, bmp0);
                left_image_filename = left_image.Substring(0, right_image.Length - 4) + "2.bmp";
                bmp0.Save(left_image_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            Bitmap bmp = (Bitmap)Bitmap.FromFile(right_image);
            byte[] img = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, img);
            byte[] img_offset = new byte[bmp.Width * bmp.Height * 3];
            int[] lookup1 = new int[bmp.Width * bmp.Height * 3];
            scale = 1.0f / scale;
            CreateScaledImageLookupColour(scale, lookup1, bmp.Width, bmp.Height, (int)offset_x, (int)offset_y, rotation_degrees);
            for (int i = 0; i < img_offset.Length; i++) img_offset[i] = img[lookup1[i]];
            bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img_offset, bmp);
            right_image_filename = right_image.Substring(0, right_image.Length - 4) + "2.bmp";
            bmp.Save(right_image_filename, System.Drawing.Imaging.ImageFormat.Bmp);

            List<string> images = new List<string>();
            images.Add(left_image_filename);
            images.Add(right_image_filename);
            CreateAnimatedGif(images, delay_mS, gif_filename);
        }

        public static void CreateAnimatedGif(
            List<string> gifFiles, 
            int delay, 
            string outputFile)
        {
            delay /= 10; // 100th of a second

            FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite);
            BinaryWriter writer = new BinaryWriter(fs);
            byte[] gif_Signature = new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' };
            writer.Write(gif_Signature);

            for (int i = 0; i < gifFiles.Count; i++)
            {
                GifClass gif = new GifClass();
                gif.LoadGifPicture(gifFiles[i]);

                if (i == 0)
                    writer.Write(gif.m_ScreenDescriptor.ToArray());

                writer.Write(GifCreator.CreateGraphicControlExtensionBlock(delay));
                writer.Write(gif.m_ImageDescriptor.ToArray());
                writer.Write(gif.m_ColorTable.ToArray());
                writer.Write(gif.m_ImageData.ToArray());
                gif.Close();
            }

            writer.Write(GifCreator.CreateLoopBlock());
            writer.Write((byte)0x3B); //End file
            fs.Close();
            writer.Close();
        }

        public static byte[] CreateGraphicControlExtensionBlock(int delay)
        {
            byte[] result = new byte[8];

            // Split the delay into high- and lowbyte

            byte d1 = (byte)(delay % 256);

            byte d2 = (byte)(delay / 256);

            result[0] = (byte)0x21; // Start ExtensionBlock

            result[1] = (byte)0xF9; // GraphicControlExtension

            result[2] = (byte)0x04; // Size of DataBlock (4)

            result[3] = d2;

            result[4] = d1;

            result[5] = (byte)0x00;

            result[6] = (byte)0x00;

            result[7] = (byte)0x00;

            return result;

        }

        public static byte[] CreateLoopBlock()

        { return CreateLoopBlock(0); }

        public static byte[] CreateLoopBlock(int numberOfRepeatings)
        {

            byte rep1 = (byte)(numberOfRepeatings % 256);

            byte rep2 = (byte)(numberOfRepeatings / 256);

            byte[] result = new byte[19];

            result[0] = (byte)0x21; // Start ExtensionBlock

            result[1] = (byte)0xFF; // ApplicationExtension

            result[2] = (byte)0x0B; // Size of DataBlock (11) for NETSCAPE2.0)

            result[3] = (byte)'N';

            result[4] = (byte)'E';

            result[5] = (byte)'T';

            result[6] = (byte)'S';

            result[7] = (byte)'C';

            result[8] = (byte)'A';

            result[9] = (byte)'P';

            result[10] = (byte)'E';

            result[11] = (byte)'2';

            result[12] = (byte)'.';

            result[13] = (byte)'0';

            result[14] = (byte)0x03; // Size of Loop Block

            result[15] = (byte)0x01; // Loop Indicator

            result[16] = (byte)rep1; // Number of repetitions

            result[17] = (byte)rep2; // 0 for endless loop

            result[18] = (byte)0x00;

            return result;

        }

    }

}