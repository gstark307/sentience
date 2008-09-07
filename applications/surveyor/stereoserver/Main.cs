/*
    Stereo vision server
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using surveyor.vision;
using sluggish.utilities;

namespace stereoserver
{
    class MainClass
    {
        static void Main(string[] args)
        {
            int image_width = 320;
            int image_height = 240;
            string stereo_camera_IP = "169.254.0.10";
            string calibration_filename = "calibration.xml";
            int left_port = 10001;
            int right_port = 10002;
            int broadcast_port = 10010;
            int stereo_algorithm_type = StereoVision.SIMPLE;
            SurveyorVisionStereoWin stereo_camera = null;

            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            string width_str = commandline.GetParameterValue("width", parameters);
            if (width_str != "") image_width = Convert.ToInt32(width_str);

            string height_str = commandline.GetParameterValue("height", parameters);
            if (height_str != "") image_height = Convert.ToInt32(height_str);

            string server_str = commandline.GetParameterValue("server", parameters);
            if (server_str != "") stereo_camera_IP = server_str;

            string left_port_str = commandline.GetParameterValue("leftport", parameters);
            if (left_port_str != "") left_port = Convert.ToInt32(left_port_str);

            string right_port_str = commandline.GetParameterValue("rightport", parameters);
            if (right_port_str != "") right_port = Convert.ToInt32(right_port_str);

            string broadcast_port_str = commandline.GetParameterValue("broadcastport", parameters);
            if (broadcast_port_str != "") broadcast_port = Convert.ToInt32(broadcast_port_str);

            string algorithm_str = commandline.GetParameterValue("algorithm", parameters);
            if (algorithm_str != "")
            {
                algorithm_str = algorithm_str.ToLower();
                if (algorithm_str == "simple") stereo_algorithm_type = StereoVision.SIMPLE;
                if (algorithm_str == "dense") stereo_algorithm_type = StereoVision.DENSE;
            }

            calibration_filename = commandline.GetParameterValue("calibration", parameters);
            if (calibration_filename == "")
            {
                Console.WriteLine("You must supply a calibration file");
            }
            else
            {
                if (!File.Exists(calibration_filename))
                {
                    Console.WriteLine("The calibration file " + calibration_filename + " could not be found");
                }
                else
                {
                    stereo_camera = Init(stereo_camera_IP, calibration_filename, image_width, image_height, left_port, right_port, broadcast_port, stereo_algorithm_type);
                    stereo_camera.Run();
                    while (stereo_camera.Running)
                    {
                        System.Threading.Thread.Sleep(20);
                    }
                }
            }
        }

        private static SurveyorVisionStereoWin Init(string stereo_camera_IP,
                                                    string calibration_filename,
                                                    int image_width, int image_height,
                                                    int left_port, int right_port,
                                                    int broadcast_port,
                                                    int stereo_algorithm_type)
        {
            SurveyorVisionStereoWin stereo_camera =
                new SurveyorVisionStereoWin(stereo_camera_IP, left_port, right_port, broadcast_port);

            PictureBox picLeftImage = new PictureBox();
            PictureBox picRightImage = new PictureBox();

            picLeftImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRightImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            stereo_camera.window = null;
            stereo_camera.display_image[0] = picLeftImage;
            stereo_camera.display_image[1] = picRightImage;
            stereo_camera.Load(calibration_filename);
            stereo_camera.stereo_algorithm_type = stereo_algorithm_type;
            
            return (stereo_camera);
        }

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("width");
            ValidParameters.Add("height");
            ValidParameters.Add("server");
            ValidParameters.Add("leftport");
            ValidParameters.Add("rightport");
            ValidParameters.Add("broadcastport");
            ValidParameters.Add("algorithm");
            ValidParameters.Add("calibration");

            return (ValidParameters);
        }
    }
}