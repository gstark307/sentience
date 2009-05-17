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
        /// <summary>
        /// stereo camera server 
        /// </summary>
        /// <example>
        /// use with a Surveyor stereo vision system
        ///    stereoserver -server 169.254.0.10 
        ///                 -algorithm dense 
        ///                 -calibration0 calibration.xml 
        ///                 -width 320 -height 240 
        ///                 -broadcastport 10010
        ///                 -fps 10
        /// use with a webcam based stereo camera:
        ///    stereoserver -leftdevice /dev/video1 
        ///                 -rightdevice /dev/video2 
        ///                 -algorithm dense 
        ///                 -calibration0 calibration.xml 
        ///                 -width 320 -height 240 
        ///                 -ramdisk /home/myusername/ramdisk
        ///                 -record /home/myusername/recordedimages
        ///                 -broadcastport 10010
        /// use with two stereo cameras (based on webcams):
        ///    stereoserver -leftdevice /dev/video1 
        ///                 -rightdevice /dev/video2 
        ///                 -leftdevice2 /dev/video3 
        ///                 -rightdevice2 /dev/video4 
        ///                 -algorithm dense 
        ///                 -calibration0 calibration0.xml 
        ///                 -calibration1 calibration1.xml 
        ///                 -width 320 -height 240 
        ///                 -ramdisk /home/myusername/ramdisk
        ///                 -record /home/myusername/recordedimages
        ///                 -broadcastport 10010
        ///                 -broadcastport2 10011
        /// use with two stereo cameras (based on webcams) on Microsoft Windows:
        ///    stereoserver -leftdevice 0 
        ///                 -rightdevice 1 
        ///                 -leftdevice2 2 
        ///                 -rightdevice2 3 
        ///                 -algorithm dense 
        ///                 -calibration0 calibration0.xml 
        ///                 -calibration1 calibration1.xml 
        ///                 -width 320 -height 240 
        ///                 -record /home/myusername/recordedimages
        ///                 -broadcastport 10010
        ///                 -broadcastport2 10011        
        /// </example>
        /// <param name="args"></param>
        static void Main(string[] args)
        {            
            // default settings for the surveyor stereo camera
            string stereo_camera_IP = "169.254.0.10";
            string calibration_filename0 = "calibration0.xml";
            string calibration_filename1 = "calibration1.xml";
            int left_port = 10001;
            int right_port = 10002;
                        
            int image_width = 320;
            int image_height = 240;
            int broadcast_port = 10010;
            int broadcast_port2 = 10011;
            int stereo_algorithm_type = StereoVision.SIMPLE;
            float fps = 1.0f;
            
            BaseVisionStereo stereo_camera0 = null;
            BaseVisionStereo stereo_camera1 = null;

            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            bool record = false;
            string record_path = commandline.GetParameterValue("record", parameters);
            if (record_path != "")
            {
                record = true;
                if (record_path.ToLower() == "true") record_path = "";
            }
                
            // You may want to have temporary images written to a ram disk
            // rather than thrashing the hard disk.
            // To create a ram disk:
            //     mkdir /home/myusername/ramdisk
            //     sudo mount -t tmpfs none /home/myusername/ramdisk -o size=10m
            string ramdisk = commandline.GetParameterValue("ramdisk", parameters);

            // device names for the left and right camera (primary stereo camera)
            string left_device = commandline.GetParameterValue("leftdevice", parameters);
            string right_device = commandline.GetParameterValue("rightdevice", parameters);
            
            // device names for the left and right camera (secondary stereo camera)
            string left_device2 = commandline.GetParameterValue("leftdevice2", parameters);
            string right_device2 = commandline.GetParameterValue("rightdevice2", parameters);
            
            // ideal frames per second
            string fps_str = commandline.GetParameterValue("fps", parameters);
            if (fps_str != "") fps = Convert.ToSingle(fps_str);

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

            broadcast_port_str = commandline.GetParameterValue("broadcastport2", parameters);
            if (broadcast_port_str != "") broadcast_port2 = Convert.ToInt32(broadcast_port_str);

            // minimum exposure value for the camera
            // this varies from one webcam to the next
            int min_exposure = 0;
            string min_exposure_str = commandline.GetParameterValue("minexposure", parameters);
            if (min_exposure_str != "") min_exposure = Convert.ToInt32(min_exposure_str);

            // maximum exposure value for the camera
            // this varies from one webcam to the next, and can even be a negative value
            int max_exposure = 650;
            string max_exposure_str = commandline.GetParameterValue("maxexposure", parameters);
            if (max_exposure_str != "") max_exposure = Convert.ToInt32(max_exposure_str);

            // on windows systems use pause on the media control rather than stop
            bool use_media_pause = false;
            string use_media_pause_str = commandline.GetParameterValue("mediapause", parameters);
            if (use_media_pause_str != "") use_media_pause = true;

            bool disable_rectification = false;
            string disable_rectification_str = commandline.GetParameterValue("norectify", parameters);
            if (disable_rectification_str != "") disable_rectification = true;

            bool disable_radial_correction = false;
            string disable_radial_correction_str = commandline.GetParameterValue("noradial", parameters);
            if (disable_radial_correction_str != "") disable_radial_correction = true;

            string force = commandline.GetParameterValue("force", parameters);

            string algorithm_str = commandline.GetParameterValue("algorithm", parameters);
            if (algorithm_str != "")
            {
                algorithm_str = algorithm_str.ToLower();
                if (algorithm_str == "simple") stereo_algorithm_type = StereoVision.SIMPLE;
                if (algorithm_str == "dense") stereo_algorithm_type = StereoVision.DENSE;
            }

            // a file which if present pauses frame capture
            string pause_file = commandline.GetParameterValue("pause", parameters);

            // whether to save debugging images of the stereo disparities
            bool save_debug_images = false;
            string save_debug_images_str = commandline.GetParameterValue("debug", parameters);
            if (save_debug_images_str != "")
            {
                save_debug_images = true;
            }

            calibration_filename0 = commandline.GetParameterValue("calibration0", parameters);
            calibration_filename1 = commandline.GetParameterValue("calibration1", parameters);
            if (calibration_filename0 == "")
            {
                Console.WriteLine("You must supply a calibration file for stereo camera 0");
            }
            else
            {
                if (!File.Exists(calibration_filename0))
                {
                    Console.WriteLine("The calibration file for stereo camera 0 " + calibration_filename0 + " could not be found");
                }
                else
                {                
                    // ensure that a server for the same stereo camera isn't already running
                    bool is_ip_camera;
                    bool mutex_ok;
                    System.Threading.Mutex m = null;
                    
                    if ((left_device == "") || (left_device == null))
                    {
                        m = new System.Threading.Mutex(true, "stereoserver " + stereo_camera_IP, out mutex_ok);
                        is_ip_camera = true;
                    }
                    else
                    {
                        m = new System.Threading.Mutex(true, "stereoserver " + left_device + " " + right_device, out mutex_ok);
                        is_ip_camera = false;
                    }
                    
                    if ((!mutex_ok) && (force == ""))
                    {
                        Console.WriteLine("A server for this stereo camera is already running");
                    }
                    else
                    {
                        if (is_ip_camera)
                        {
                            // surveyor stereo camera
                            stereo_camera0 = Init(stereo_camera_IP, calibration_filename0, image_width, image_height, left_port, right_port, broadcast_port, stereo_algorithm_type, fps);
                        }
                        else
                        {
                            // webcam based stereo camera
                            stereo_camera0 = Init(left_device, right_device, calibration_filename0, image_width, image_height, broadcast_port, stereo_algorithm_type, fps, pause_file);
                            if ((left_device2 != "") && (right_device2 != ""))
                            {
                                if (calibration_filename1 == "")
                                {
                                    Console.WriteLine("No calibration file found for stereo camera 1");
                                }
                                else
                                {
                                    stereo_camera1 = Init(left_device2, right_device2, calibration_filename1, image_width, image_height, broadcast_port2, stereo_algorithm_type, fps, pause_file);
                                }
                            }
                        }
                            
                        stereo_camera0.Record = record;
                        stereo_camera0.temporary_files_path = ramdisk;
                        stereo_camera0.recorded_images_path = record_path;                                                
                        stereo_camera0.SetPauseFile(pause_file);
                        stereo_camera0.stereo_camera_index = -1;
                        stereo_camera0.min_exposure = min_exposure;
                        stereo_camera0.max_exposure = max_exposure;
                        stereo_camera0.disable_rectification = disable_rectification;
                        stereo_camera0.disable_radial_correction = disable_radial_correction;
                        stereo_camera0.use_media_pause = use_media_pause;
                        stereo_camera0.SaveDebugImages = save_debug_images;
                        stereo_camera0.Run();                        
                        if (stereo_camera1 != null)
                        {
                            stereo_camera0.next_camera = stereo_camera1;
                            stereo_camera1.next_camera = stereo_camera0;
                            stereo_camera0.stereo_camera_index = 0;
                            stereo_camera1.stereo_camera_index = 1;
                            stereo_camera1.SaveDebugImages = save_debug_images;
                            stereo_camera1.min_exposure = min_exposure;
                            stereo_camera1.max_exposure = max_exposure;
                            stereo_camera1.use_media_pause = use_media_pause;
                            stereo_camera1.disable_rectification = disable_rectification;
                            stereo_camera1.disable_radial_correction = disable_radial_correction;
                            stereo_camera0.active_camera = true;
                            stereo_camera1.active_camera = false;
                            stereo_camera1.Record = record;
                            stereo_camera1.temporary_files_path = ramdisk;
                            stereo_camera1.recorded_images_path = record_path;
                            stereo_camera1.SetPauseFile(pause_file);
                            stereo_camera1.Run();                            
                        }
                        while (stereo_camera0.Running)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    
                        // keeping the dream alive
                        GC.KeepAlive(m);
                    }
                }
            }
        }

        private static SurveyorVisionStereoWin Init(
            string stereo_camera_IP,
            string calibration_filename,
            int image_width, int image_height,
            int left_port, int right_port,
            int broadcast_port,
            int stereo_algorithm_type,
            float fps)
        {
            SurveyorVisionStereoWin stereo_camera =
                new SurveyorVisionStereoWin(stereo_camera_IP, left_port, right_port, broadcast_port, fps);

            PictureBox picLeftImage = new PictureBox();
            PictureBox picRightImage = new PictureBox();

            picLeftImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRightImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            stereo_camera.window = null;
            stereo_camera.display_image[0] = picLeftImage;
            stereo_camera.display_image[1] = picRightImage;
            stereo_camera.Load(calibration_filename);
            stereo_camera.stereo_algorithm_type = stereo_algorithm_type;
            stereo_camera.UpdateWhenClientsConnected = true;
            stereo_camera.random_rows = 5;
            
            return (stereo_camera);
        }

        private static WebcamVisionStereoWin Init(
            string left_camera_device,
            string right_camera_device,
            string calibration_filename,
            int image_width, int image_height,
            int broadcast_port,
            int stereo_algorithm_type,
            float fps,
            string pause_file)
        {
            WebcamVisionStereoWin stereo_camera =
                new WebcamVisionStereoWin(
                    left_camera_device, right_camera_device,
                    broadcast_port, fps);

            PictureBox picLeftImage = new PictureBox();
            PictureBox picRightImage = new PictureBox();

            picLeftImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRightImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            stereo_camera.window = null;
            stereo_camera.display_image[0] = picLeftImage;
            stereo_camera.display_image[1] = picRightImage;
            stereo_camera.Load(calibration_filename);
            stereo_camera.image_width = image_width;
            stereo_camera.image_height = image_height;
            stereo_camera.stereo_algorithm_type = stereo_algorithm_type;
            stereo_camera.UpdateWhenClientsConnected = true;
            stereo_camera.random_rows = 5;
            stereo_camera.SetPauseFile(pause_file);
            
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
            ValidParameters.Add("leftdevice");
            ValidParameters.Add("rightdevice");
            ValidParameters.Add("leftdevice2");
            ValidParameters.Add("rightdevice2");
            ValidParameters.Add("broadcastport");
            ValidParameters.Add("broadcastport2");
            ValidParameters.Add("algorithm");
            ValidParameters.Add("calibration0");
            ValidParameters.Add("calibration1");
            ValidParameters.Add("record");
            ValidParameters.Add("fps");
            ValidParameters.Add("ramdisk");
            ValidParameters.Add("pause");
            ValidParameters.Add("force");
            ValidParameters.Add("minexposure");
            ValidParameters.Add("maxexposure");
            ValidParameters.Add("mediapause");
            ValidParameters.Add("norectify");
            ValidParameters.Add("noradial");
            ValidParameters.Add("debug");

            return (ValidParameters);
        }
    }
}