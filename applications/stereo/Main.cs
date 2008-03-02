/*
    Stereo correspondence command line utility
    Copyright (C) 2000-2008 Bob Mottram
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
using System.Text;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using sentience.core;
using sluggish.utilities;

namespace stereo
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if (!GetHelp(args))
            {
                // left and right image filenames
                List<string> filename = GetFilenames(args);
                if (filename == null)
                {
                    Console.WriteLine("You must provide two images in JPG, BMP or PGM format");
                }
                else
                {
                    // get the image data as byte arrays
                    int image_width = 0, image_height = 0;
                    List<byte[]> image_data = GetImageData(filename, ref image_width, ref image_height);

                    if (image_data.Count == 2)
                    {
                        // get the file in which raw data will be saved
                        string data_filename = GetDataFile(args);

                        // get the maximum disparity value
                        int maximum_disparity_percent = GetMaximumDisparity(args);

                        // get the stereo camera calibration filename
                        string calibration_filename = GetCalibrationFile(args);

                        string output_filename = "depthmap.bmp";
                        if (filename.Count > 2) output_filename = filename[2];

                        // perform stereo correspondence
                        StereoMatch(calibration_filename, data_filename, image_data, image_width, image_height, maximum_disparity_percent, output_filename);
                    }
                }
            }
        }

        #region "getting command line values"

        /// <summary>
        /// get help on the syntax
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool GetHelp(string[] args)
        {
            bool help = false;

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].ToLower() == "-help") ||
                    (args[i].ToLower() == "--help"))
                {
                    Console.WriteLine("stereo <left image filename>");
                    Console.WriteLine("       <right image filename>");
                    Console.WriteLine("       <output disparity map image filename>");
                    Console.WriteLine("       -calib <calibration filename>");
                    Console.WriteLine("       -disp <maximum disparity as % of image width>");
                    help = true;
                }
            }
            return (help);
        }

        /// <summary>
        /// returns the maximum disparity as a percentage of the image width
        /// </summary>
        /// <param name="args">command line arguments</param>
        /// <returns>maximum disparity value</returns>
        private static int GetMaximumDisparity(string[] args)
        {
            int maximum_disparity_percent = 20;

            for (int i = 0; i < args.Length - 1; i++)
            {
                string arg_str = args[i].ToLower();
                if ((arg_str == "-disparity") ||
                    (arg_str == "--disparity") ||
                    (arg_str == "-disp") ||
                    (arg_str == "--disp") ||
                    (arg_str == "-d") ||
                    (arg_str == "--d"))
                {
                    maximum_disparity_percent = Convert.ToInt32(args[i + 1]);
                }
            }
            return (maximum_disparity_percent);
        }

        /// <summary>
        /// returns the stereo camera calibration filename
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string GetCalibrationFile(string[] args)
        {
            string calibration_filename = "";

            for (int i = 0; i < args.Length - 1; i++)
            {
                string arg_str = args[i].ToLower();
                if ((arg_str == "-calibration") ||
                    (arg_str == "--calibration") ||
                    (arg_str == "-calib") ||
                    (arg_str == "--calib") ||
                    (arg_str == "-c") ||
                    (arg_str == "--c"))
                {
                    calibration_filename = args[i + 1];
                }
            }
            return (calibration_filename);
        }


        /// <summary>
        /// gets the data filename
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string GetDataFile(string[] args)
        {
            string data_filename = "";

            for (int i = 0; i < args.Length - 1; i++)
            {
                string arg_str = args[i].ToLower();
                if ((arg_str == "-data") ||
                    (arg_str == "--data"))
                {
                    data_filename = args[i + 1];
                }
            }
            return (data_filename);
        }

        #endregion

        #region "saving depth map data"

        /// <summary>
        /// save the depth map data as a binary file
        /// </summary>
        /// <param name="data_filename"></param>
        /// <param name="depthmap"></param>
        /// <param name="depthmap_width"></param>
        /// <param name="depthmap_height"></param>
        private static void SaveDataRaw(string data_filename,
                                        byte[] depthmap,
                                        int depthmap_width, int depthmap_height)
        {
            FileStream fp = new FileStream(data_filename, FileMode.Create);
            BinaryWriter binfile = new BinaryWriter(fp);

            // the first two integers are the image dimensions
            binfile.Write(depthmap_width);
            binfile.Write(depthmap_height);

            // and the rest is bytes
            int n = 0;
            for (int y = 0; y < depthmap_height; y++)
            {
                for (int x = 0; x < depthmap_width; x++)
                {
                    binfile.Write(depthmap[n]);
                    n++;
                }
            }
            
            binfile.Close();
            fp.Close();
        }

        /// <summary>
        /// saves depth map data as a csv file, 
        /// suitable for loading into a spreadsheet
        /// </summary>
        /// <param name="data_filename"></param>
        /// <param name="depthmap"></param>
        /// <param name="depthmap_width"></param>
        /// <param name="depthmap_height"></param>
        private static void SaveDataCSV(string data_filename,
                                        byte[] depthmap,
                                        int depthmap_width, int depthmap_height)
        {
            StreamWriter oWrite = null;
            bool allowWrite = true;

            try
            {
                oWrite = File.CreateText(data_filename);
            }
            catch
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                int n = 0;
                for (int y = 0; y < depthmap_height; y++)
                {
                    for (int x = 0; x < depthmap_width; x++)
                    {
                        if (depthmap[n] > 0)
                        {
                            if (depthmap[n] < 100) oWrite.Write("0");
                            oWrite.Write(depthmap[n].ToString());
                        }
                        else
                        {
                            oWrite.Write("000");
                        }
                        if (x < depthmap_width - 1) oWrite.WriteLine(",");
                        n++;
                    }
                }
                oWrite.Close();
            }
        }

        /// <summary>
        /// saves the depth map data to a file
        /// </summary>
        /// <param name="data_filename"></param>
        /// <param name="depthmap"></param>
        /// <param name="depthmap_width"></param>
        /// <param name="depthmap_height"></param>
        private static void SaveData(string data_filename,
                                     byte[] depthmap,
                                     int depthmap_width, int depthmap_height)
        {
            if (data_filename != "")
            {
                if (data_filename.ToLower().EndsWith(".csv"))
                    SaveDataCSV(data_filename, depthmap, depthmap_width, depthmap_height);

                if ((data_filename.ToLower().EndsWith(".raw")) ||
                    (data_filename.ToLower().EndsWith(".bin")))
                    SaveDataRaw(data_filename, depthmap, depthmap_width, depthmap_height);
            }
        }

        #endregion

        #region "stereo correspondence"

        /// <summary>
        /// perform stereo matching of the given images and save the 
        /// results with the given filename
        /// </summary>
        /// <param name="calibration_filename">stereo camera calibration filename</param>
        /// <param name="data_filename">file into which disparity data will be saved</param>
        /// <param name="image_data">list containing two mono images</param>
        /// <param name="image_width">width of the image in pixels</param>
        /// <param name="image_height">height of the image in pixels</param>
        /// <param name="maximum_disparity_percent">maximum disparity as a percentage of the image width</param>
        /// <param name="output_filename">filename to save the depth map as</param>
        private static void StereoMatch(string calibration_filename,
                                        string data_filename,
                                        List<byte[]> image_data, 
                                        int image_width, int image_height,
                                        int maximum_disparity_percent,
                                        string output_filename)
        {
            int horizontal_compression = 1;
            int vertical_compression = 3;

            // an interface to different stereo correspondence algorithms
            sentience_stereo_interface stereointerface = new sentience_stereo_interface();

            // load calibration data
            if (calibration_filename != "")
                stereointerface.loadCalibration(calibration_filename);

            //the type of stereo correspondance algorithm to be used
            int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_CONTOURS;

            // load images into the correspondence object
            stereointerface.loadImage(image_data[0], image_width, image_height, true, 1);
            stereointerface.loadImage(image_data[1], image_width, image_height, false, 1);

            // set the quality of the disparity map
            stereointerface.setDisparityMapCompression(horizontal_compression, vertical_compression);

            // set the maximum disparity as a percentage of the image width
            stereointerface.setMaxDisparity(maximum_disparity_percent);

            // perform stereo correspondence
            stereointerface.stereoMatchRun(0, 8, correspondence_algorithm_type);

            // make a bitmap to store the depth map
            byte[] depthmap = new byte[image_width * image_height * 3];
            stereointerface.getDisparityMap(depthmap, image_width, image_height, 0);

            // save disparity data to file
            if (data_filename != "")
                SaveData(data_filename, depthmap, image_width, image_height);

            if (output_filename != "")
            {
                // save output as a jpeg
                if ((output_filename.ToLower().EndsWith(".jpg")) ||
                    (output_filename.ToLower().EndsWith(".jpeg")))
                {
                    Bitmap depthmap_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    BitmapArrayConversions.updatebitmap_unsafe(depthmap, depthmap_bmp);
                    depthmap_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                // save output as a bitmap
                if (output_filename.ToLower().EndsWith(".bmp"))
                {
                    Bitmap depthmap_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    BitmapArrayConversions.updatebitmap_unsafe(depthmap, depthmap_bmp);
                    depthmap_bmp.Save(output_filename, System.Drawing.Imaging.ImageFormat.Bmp);
                }

                // save output as a pgm
                if (output_filename.ToLower().EndsWith(".pgm"))
                {
                    image.saveToPGM(depthmap, image_width, image_height, output_filename);
                }
            }
        }

        #endregion

        #region "getting stereo image data as byte arrays"

        /// <summary>
        /// returns the given images as byte arrays
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static List<byte[]> GetImageData(List<string> filename, 
                                                 ref int image_width,
                                                 ref int image_height)
        {
            int img_width = 0, img_height = 0;
            List<byte[]> images = new List<byte[]>();

            int max = filename.Count;
            if (max > 2) max = 2;
            for (int i = 0; i < max; i++)
            {
                byte[] img = GetImageData(filename[i], ref img_width, ref img_height);
                if (img != null)
                {
                    images.Add(img);
                    image_width = img_width;
                    image_height = img_height;
                }
            }

            return (images);
        }

        /// <summary>
        /// returns the given image as a byte array
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <returns></returns>
        private static byte[] GetImageData(string filename,
                                           ref int image_width,
                                           ref int image_height)
        {
            byte[] img = null;

            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            if (bmp != null)
            {
                image_width = bmp.Width;
                image_height = bmp.Height;
                img = new byte[image_width * image_height * 3];

                // get the as a byte array
                BitmapArrayConversions.updatebitmap(bmp, img);

                // convert the image to mono format (1 byte per pixel)
                img = image.monoImage(img, image_width, image_height);
            }

            return (img);
        }

        #endregion

        #region "getting stereo image filenames"

        /// <summary>
        /// returns the filenames of stereo images contained withing the given string array
        /// </summary>
        /// <param name="args"></param>
        /// <returns>array containing two filenames</returns>
        private static List<string> GetFilenames(string[] args)
        {
            int index = 0;
            List<string> filename = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string arg_str = args[i].ToLower();
                if ((arg_str.EndsWith(".bmp")) ||
                    (arg_str.EndsWith(".jpg")) ||
                    (arg_str.EndsWith(".jpeg")) ||
                    (arg_str.EndsWith(".pgm")))
                {
                    if ((File.Exists(args[i])) || (index >= 2))
                    {
                        filename.Add(args[i]);
                        index++;
                    }
                    else
                    {
                        Console.WriteLine("The file " + args[i] + " could not be found");
                    }
                }
            }
            if (index >= 2)
                return (filename);
            else
                return (null);
        }

        #endregion    
    }
}