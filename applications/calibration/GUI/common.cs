/*
    Copyright (C) 2007  Bob Mottram
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace WindowsApplication1
{
    public partial class common : Form
    {
        #region bitmap loading/storing

        /// <summary>
        /// Copy the given byte array to a bitmap object
        /// </summary>
        /// <param name="imageData">Array to be inserted</param>
        /// <param name="bmp">Destination bitmap object</param>
        public unsafe void updatebitmap(byte[] imageData, Bitmap bmp)
        {

            // Lock bitmap and retrieve bitmap pixel data pointer

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // fix the bitmap data in place whilst processing as the garbage collector may move it

            fixed (byte* pimageData = imageData)
            {

                // Copy the image data to the bitmap

                byte* dst = (byte*)bmpData.Scan0;

                byte* src = (byte*)pimageData;

                for (int i = 0; i < imageData.Length; i++)
                {

                    *dst++ = *src++;

                }

            }

            bmp.UnlockBits(bmpData);

        }


        /// <summary>
        /// Copy the given bitmap object to a byte array
        /// NOTE: this version has some problems, in that the array returned producves a skewed bitmap
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        /// <param name="imageData">Destination Array</param>
        public unsafe void updatebitmap(Bitmap bmp, byte[] imageData)
        {

            // Lock bitmap and retrieve bitmap pixel data pointer

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // fix the bitmap data in place whilst processing as the garbage collector may move it

            fixed (byte* pimageData = imageData)
            {

                // Copy the bitmap to the image data

                byte* dst = (byte*)bmpData.Scan0;

                byte* src = (byte*)pimageData;

                for (int i = 0; i < imageData.Length; i++)
                {

                    *src++ = *dst++;

                }

            }

            bmp.UnlockBits(bmpData);

        }


        /// <summary>
        /// Copy the given bitmap object to a byte array (slow version)
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        /// <param name="imageData">Destination Array</param>
        public unsafe void updatebitmapslow(Bitmap bmp, byte[] imageData)
        {
            int x, y, n;
            Color col;

            n = 0;
            for (y = 0; y < bmp.Height; y++)
                for (x = 0; x < bmp.Width; x++)
                {
                    col = bmp.GetPixel(x, y);
                    imageData[n] = col.B;
                    imageData[n + 1] = col.G;
                    imageData[n + 2] = col.R;
                    n += 3;
                }
        }

        #endregion
    }
}