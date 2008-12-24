/*
    winwebcam: a Windows utility for grabbing images from webcams
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace sluggish.winwebcam
{
    class Program
    {
        static void Main(string[] args)
        {
            // ensure that the program isn't run more than once
            bool mutex_ok;
            System.Threading.Mutex m = new System.Threading.Mutex(true, "winwebcam", out mutex_ok);

            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            // force the program to run even if there are previous instances of it already running
            string force = commandline.GetParameterValue("force", parameters);

            if ((!mutex_ok) && (force == ""))
            {
                Console.WriteLine("This program is already running");
            }
            else
            {
                // get teh filter indexes of camera devices to be used
                string camera_devices = "0";
                string camera_devices2 = commandline.GetParameterValue("d", parameters);
                if (camera_devices2 != "") camera_devices = camera_devices2;

                // get the resolution
                int image_width = 640;
                int image_height = 480;
                string resolution = commandline.GetParameterValue("r", parameters);
                if (resolution.Contains("x"))
                {
                    string[] str = resolution.Split('x');
                    if (str.Length > 1)
                    {
                        image_width = Convert.ToInt32(str[0]);
                        image_height = Convert.ToInt32(str[1]);
                    }
                }

                // get the exposure
                int exposure = 0;
                string exposure_str = commandline.GetParameterValue("e", parameters);
                if (exposure_str != "")
                    exposure = Convert.ToInt32(exposure_str);

                // initial frames to capture
                int initial_frames = 3;
                string initial_frames_str = commandline.GetParameterValue("i", parameters);
                if (initial_frames_str != "") initial_frames = Convert.ToInt32(initial_frames_str);

                string output_filename = "capture.jpg";
                string output_filename2 = commandline.GetParameterValue("save", parameters);
                if (output_filename2 != "") output_filename = output_filename2;

                int no_of_captures = 1;
                string no_of_captures_str = commandline.GetParameterValue("cap", parameters);
                if (no_of_captures_str != "") no_of_captures = Convert.ToInt32(no_of_captures_str);

                int capture_interval_mS = 500;
                string capture_interval_mS_str = commandline.GetParameterValue("delay", parameters);
                if (capture_interval_mS_str != "") capture_interval_mS = Convert.ToInt32(capture_interval_mS_str);

                wincamera.Update(camera_devices, image_width, image_height, initial_frames, output_filename, exposure, no_of_captures, capture_interval_mS);
            }

            // keeping the dream alive
            GC.KeepAlive(m);
        }

        #region "validation"

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("force");
            ValidParameters.Add("d");
            ValidParameters.Add("r");
            ValidParameters.Add("e");
            ValidParameters.Add("i");
            ValidParameters.Add("save");
            ValidParameters.Add("cap");
            ValidParameters.Add("delay");
            ValidParameters.Add("port");
            return (ValidParameters);
        }


        #endregion

        #region "help information"

        /// <summary>
        /// shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("winwebcam Help");
            Console.WriteLine("-------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  winwebcam");
            Console.WriteLine("         -force Forces execution even if another instance is already running");
            Console.WriteLine("         -d <DirectShow filter name of the camera>");
            Console.WriteLine("         -r <Resolution, eg. 640x480>");
            Console.WriteLine("         -e <exposure value>");
            Console.WriteLine("         -i <number of initial frames>");
            Console.WriteLine("         -save <filename to save the captured image as>");
            Console.WriteLine("         -cap <number of sets of images to capture (0 = inifinite)>");
            Console.WriteLine("         -delay <time between captures in mS>");
            Console.WriteLine("         -port <port number>");
        }

        #endregion

    }
}
