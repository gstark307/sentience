/*
    Sentience 3D Perception System
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WindowsApplication1
{
    /*
     
     Note that there is a HUGE difference in performance between using the 
     safe and unsafe methods for grabbing an image.
      
    */

    public partial class common : Form
    {
        #region bitmap loading/storing


        /// <summary>
        /// copy a bitmap to a byte array safely
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="bmp"></param>
        public Byte[] updatebitmap_safe(Bitmap bmp)
        {
            BitmapData bmpData = null;
            byte[] imageData = null;

            MemoryStream stream = new MemoryStream();
            try
            {
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                bmp.Save(stream, ImageFormat.Bmp);
                imageData = stream.ToArray();
                bmp.UnlockBits(bmpData);
            }
            catch
            {
                stream.Close();
                //imageData = new byte[bmp.Width * bmp.Height * 3];
            }
            return (imageData);
        }


        /// <summary>
        /// Copy the given bitmap object to a byte array
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        /// <param name="imageData">Destination Array</param>
        public unsafe Byte[] updatebitmap_unsafe(Bitmap bmp)
        {
            byte[] imageData = null;

            try
            {
                if (bmp != null)
                {
                    // Lock bitmap and retrieve bitmap pixel data pointer
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                    if (imageData == null) imageData = new byte[bmp.Width * bmp.Height * 3];

                    // fix the bitmap data in place whilst processing as the garbage collector may move it
                    fixed (Byte* pimageData = imageData)
                    {
                        // Copy the bitmap to the image data
                        Byte* dst = (Byte*)bmpData.Scan0;
                        Byte* src = (Byte*)pimageData;
                        for (int i = 0; i < imageData.Length; i++) *src++ = *dst++;
                    }

                    bmp.UnlockBits(bmpData);
                }
            }
            catch //(Exception ex)
            {
                //MessageBox.Show("updatebitmap2/" + ex.Message);
            }

            return (imageData);
        }



        public void updatebitmap_safe(Byte[] imageData, Bitmap bmp)
        {
            MemoryStream stream = new MemoryStream(imageData);
            stream.Write(imageData, 0, imageData.Length);

            //Bitmap bmp = new Bitmap(new MemoryStream(imageData));
            
            //Image img = Image.FromStream(stream);
            bmp = (Bitmap)Image.FromStream(stream);

            //note: stream not closed
            //return ((Bitmap)img);
            //return (bmp);
        }



        /// <summary>
        /// Copy the given byte array to a bitmap object
        /// </summary>
        /// <param name="imageData">Array to be inserted</param>
        /// <param name="bmp">Destination bitmap object</param>
        public unsafe void updatebitmap_unsafe(byte[] imageData, Bitmap bmp)
        {
            try
            {
                if (imageData != null)
                {
                    // Lock bitmap and retrieve bitmap pixel data pointer
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                    // fix the bitmap data in place whilst processing as the garbage collector may move it
                    fixed (byte* pimageData = imageData)
                    {
                        // Copy the image data to the bitmap
                        byte* dst = (byte*)bmpData.Scan0;
                        byte* src = (byte*)pimageData;

                        for (int i = 0; i < imageData.Length; i++) *dst++ = *src++;
                    }
                    bmp.UnlockBits(bmpData);
                }
            }
            catch //(Exception ex)
            {
                //MessageBox.Show("updatebitmap1/" + ex.Message);
            }
        }


        /// <summary>
        /// Copy the given bitmap object to a byte array (slow but sure version)
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        /// <param name="imageData">Destination Array</param>        
        public void updatebitmapslow(Bitmap bmp, byte[] imageData)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show("updatebitmapslow1/" + ex.Message);
            }
        }


        public void updatebitmapslow(byte[] imageData, Bitmap bmp)
        {
            try
            {
                int x, y, n;

                n = 0;
                for (y = 0; y < bmp.Height; y++)
                    for (x = 0; x < bmp.Width; x++)
                    {
                        bmp.SetPixel(x, y, Color.FromArgb(imageData[n + 2], imageData[n + 1], imageData[n]));
                        n += 3;
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show("updatebitmapslow2/" + ex.Message);
            }
        }

        #endregion


        #region graph drawing

        public String dayStr(int day_no)
        {
            String str = "";

            switch (day_no)
            {
                case 0: { str = "Mon"; break; }
                case 1: { str = "Tue"; break; }
                case 2: { str = "Wed"; break; }
                case 3: { str = "Thu"; break; }
                case 4: { str = "Fri"; break; }
                case 5: { str = "Sat"; break; }
                case 6: { str = "Sun"; break; }
            }
            return (str);
        }

        public void drawGridWeekly(Graphics gr, bool showCntreLine, PictureBox pic, int day_offset, Color background)
        {
            int t, tx, ty, bx, by, d, d2;
            Pen pen = null;
            RectangleF rect;
            SolidBrush brush;
            Font fnt;

            gr.Clear(background);
            pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 100, 100));
            brush = new SolidBrush(Color.FromArgb(180, 180, 180));
            fnt = new Font(FontFamily.GenericSansSerif, 8);
            for (d = 0; d < 7; d++)
            {
                t = (d * pic.Image.Width) / 7;
                gr.DrawLine(pen, t, 0, t, pic.Image.Height - 1);

                tx = t;
                bx = t + (pic.Image.Width / 8);
                ty = 0;
                by = pic.Image.Height / 20;
                rect = new RectangleF(tx, ty, bx - tx, by - ty);

                d2 = d + day_offset;
                if (d2 > 6) d2 -= 7;
                gr.DrawString(dayStr(d2), fnt, brush, rect);

                tx = t;
                bx = t + (pic.Image.Width / 8);
                ty = pic.Image.Height - (pic.Image.Height / 20);
                by = pic.Image.Height;
                rect = new RectangleF(tx, ty, bx - tx, by - ty);
                gr.DrawString(dayStr(d), fnt, brush, rect);
            }
            if (showCntreLine) gr.DrawLine(pen, 0, pic.Image.Height / 2, pic.Image.Width - 1, pic.Image.Height / 2);
        }

        public void drawGridDaily(Graphics gr, PictureBox pic, Color background)
        {
            int hour, t, tx, ty, bx, by;
            Pen pen = null;
            RectangleF rect;
            SolidBrush brush;
            Font fnt;

            gr.Clear(background);
            pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 100, 100));
            brush = new SolidBrush(Color.FromArgb(180, 180, 180));
            fnt = new Font(FontFamily.GenericSansSerif, 8);
            for (hour = 0; hour < 24; hour++)
            {
                t = (hour * pic.Image.Width) / 24;
                gr.DrawLine(pen, t, 0, t, pic.Image.Height - 1);

                tx = t;
                bx = t + (pic.Image.Width / 25);
                ty = 0;
                by = pic.Image.Height / 20;
                rect = new RectangleF(tx, ty, bx - tx, by - ty);
                gr.DrawString(Convert.ToString(hour), fnt, brush, rect);

                tx = t;
                bx = t + (pic.Image.Width / 25);
                ty = pic.Image.Height - (pic.Image.Height / 20);
                by = pic.Image.Height;
                rect = new RectangleF(tx, ty, bx - tx, by - ty);
                gr.DrawString(Convert.ToString(hour), fnt, brush, rect);
            }
        }


        #endregion

    }
}