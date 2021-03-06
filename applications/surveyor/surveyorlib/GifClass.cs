﻿/*
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace sluggish.utilities
{

    public class GifClass
    {
        public enum GIFVersion
        {
            GIF87a,
            GIF89a
        }

        public enum GIFBlockType
        {
            ImageDescriptor = 0x2C,
            Extension = 0x21,
            Trailer = 0x3B
        }

        public GIFVersion m_Version = GIFVersion.GIF87a;

        public Bitmap m_Image = null;

        public List<byte> m_GifSignature = new List<byte>();

        public List<byte> m_ScreenDescriptor = new List<byte>();

        public List<byte> m_ColorTable = new List<byte>();

        public List<byte> m_ImageDescriptor = new List<byte>();

        public List<byte> m_ImageData = new List<byte>();
		
		public bool reverse_colours = false;

        public GifClass()
        { 
        }

        public void Close()
        {
            if (m_Image != null)
            {
                m_Image.Dispose();
            }
        }

        public void LoadGifPicture(string filename)
        { 
			Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
			if (reverse_colours)
			{
				byte[] img = new byte[bmp.Width * bmp.Height * 3];
				BitmapArrayConversions.updatebitmap(bmp, img);
				BitmapArrayConversions.RGB_BGR(img);
				BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
			}
            LoadGifPicture(bmp); 
        }

        public void LoadGifPicture(Bitmap gifPicture)
        {
            MemoryStream stream = new MemoryStream();
            
            m_Image = gifPicture;
            m_Image.Save(stream, ImageFormat.Gif);

            List<byte> dataList = new List<byte>(stream.ToArray());

            if (!AnalyzeGifSignature(dataList))
            {
                throw (new Exception("File is not a gif!"));
            }

            AnalyzeScreenDescriptor(dataList);

            GIFBlockType blockType = GetTypeOfNextBlock(dataList);

            while (blockType != GIFBlockType.Trailer)
            {
                switch (blockType)
                {

                    case GIFBlockType.ImageDescriptor:

                        AnalyzeImageDescriptor(dataList);

                        break;

                    case GIFBlockType.Extension:

                        ThrowAwayExtensionBlock(dataList);

                        break;

                    default:

                        break;

                }

                blockType = GetTypeOfNextBlock(dataList);

            }
        }

        private bool AnalyzeGifSignature(List<byte> gifData)
        {

            for (int i = 0; i < 6; i++)

                m_GifSignature.Add(gifData[i]);

            gifData.RemoveRange(0, 6);

            List<char> chars = m_GifSignature.ConvertAll<char>(new Converter<byte, char>(ByteToChar));

            string s = new string(chars.ToArray());

            if (s == GIFVersion.GIF89a.ToString())

                m_Version = GIFVersion.GIF89a;

            else if (s == GIFVersion.GIF87a.ToString())

                m_Version = GIFVersion.GIF87a;

            else

                return false;

            return true;

        }

        private char ByteToChar(byte b)

        { return (char)b; }

        private void AnalyzeScreenDescriptor(List<byte> gifData)
        {

            for (int i = 0; i < 7; i++)

                m_ScreenDescriptor.Add(gifData[i]);

            gifData.RemoveRange(0, 7);

            // if the first bit of the fifth byte is set the GlobelColorTable follows this block

            bool globalColorTableFollows = (m_ScreenDescriptor[4] & 0x80) != 0;

            if (globalColorTableFollows)
            {

                int pixel = m_ScreenDescriptor[4] & 0x07;

                int lengthOfColorTableInByte = 3 * ((int)Math.Pow(2, pixel + 1));

                for (int i = 0; i < lengthOfColorTableInByte; i++)

                    m_ColorTable.Add(gifData[i]);

                gifData.RemoveRange(0, lengthOfColorTableInByte);

            }

            m_ScreenDescriptor[4] = (byte)(m_ScreenDescriptor[4] & 0x7F);

        }

        private GIFBlockType GetTypeOfNextBlock(List<byte> gifData)
        {

            GIFBlockType blockType = (GIFBlockType)gifData[0];

            return blockType;

        }

        private void AnalyzeImageDescriptor(List<byte> gifData)
        {

            for (int i = 0; i < 10; i++)

                m_ImageDescriptor.Add(gifData[i]);

            gifData.RemoveRange(0, 10);

            // get ColorTable if exists

            bool localColorMapFollows = (m_ImageDescriptor[9] & 0x80) != 0;

            if (localColorMapFollows)
            {

                int pixel = m_ImageDescriptor[9] & 0x07;

                int lengthOfColorTableInByte = 3 * ((int)Math.Pow(2, pixel + 1));

                m_ColorTable.Clear();

                for (int i = 0; i < lengthOfColorTableInByte; i++)

                    m_ColorTable.Add(gifData[i]);

                gifData.RemoveRange(0, lengthOfColorTableInByte);

            }

            else
            {

                int lastThreeBitsOfGlobalTableDescription = m_ScreenDescriptor[4] & 0x07;

                m_ImageDescriptor[9] = (byte)(m_ImageDescriptor[9] & 0xF8);

                m_ImageDescriptor[9] = (byte)(m_ImageDescriptor[9] | lastThreeBitsOfGlobalTableDescription);

            }

            m_ImageDescriptor[9] = (byte)(m_ImageDescriptor[9] | 0x80);

            GetImageData(gifData);

        }

        private void GetImageData(List<byte> gifData)
        {

            m_ImageData.Add(gifData[0]);

            gifData.RemoveAt(0);

            while (gifData[0] != 0x00)
            {

                int countOfFollowingDataBytes = gifData[0];

                for (int i = 0; i <= countOfFollowingDataBytes; i++)
                {

                    m_ImageData.Add(gifData[i]);

                }

                gifData.RemoveRange(0, countOfFollowingDataBytes + 1);

            }

            m_ImageData.Add(gifData[0]);

            gifData.RemoveAt(0);

        }

        private void ThrowAwayExtensionBlock(List<byte> gifData)
        {

            gifData.RemoveRange(0, 2); // Delete ExtensionBlockIndicator and ExtensionDetermination

            while (gifData[0] != 0)
            {

                gifData.RemoveRange(0, gifData[0] + 1);

            }

            gifData.RemoveAt(0);

        }

    }

}