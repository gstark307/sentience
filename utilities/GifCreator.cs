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
            float scale)
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
                ref left_image_filename,
                ref right_image_filename);
        }

        public static void CreateFromStereoPair(
            string left_image,
            string right_image,
            string gif_filename,
            int delay_mS,
            float offset_x,
            float offset_y,
            float scale,
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
                byte[] img0_scaled = new byte[bmp0.Width * bmp0.Height * 3];
                int cy0 = bmp0.Height / 2;
                int cx0 = bmp0.Width / 2;
                n = 0;
                for (int y = 0; y < bmp0.Height; y++)
                {
                    int yy = cy0 + (int)((y - cy0) * scale);
                    for (int x = 0; x < bmp0.Width; x++, n += 3)
                    {
                        int xx = cx0 + (int)((x - cx0) * scale);
                        int n2 = ((yy * bmp0.Width) + xx) * 3;
                        if ((n2 > -1) && (n2 < img0.Length - 3))
                        {
                            img0_scaled[n] = img0[n2];
                            img0_scaled[n + 1] = img0[n2 + 1];
                            img0_scaled[n + 2] = img0[n2 + 2];
                        }
                    }
                }
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
            n = 0;
            int cy = bmp.Height / 2;
            int cx = bmp.Width / 2;
            scale = 1.0f / scale;
            for (int y = 0; y < bmp.Height; y++)
            {
                int yy = cy + (int)((y - cy) * scale);
                for (int x = 0; x < bmp.Width; x++, n += 3)
                {
                    int xx = cx + (int)((x - cx) * scale);
                    int n2 = (((int)(yy + offset_y) * bmp.Width) + (int)(xx + offset_x)) * 3;
                    if ((n2 > -1) && (n2 < img.Length - 3))
                    {
                        img_offset[n] = img[n2];
                        img_offset[n + 1] = img[n2 + 1];
                        img_offset[n + 2] = img[n2 + 2];
                    }
                }
            }
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