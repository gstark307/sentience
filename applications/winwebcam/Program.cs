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
                int[] directshow_filter_index = GetFilterIndexes();

                if (directshow_filter_index != null)
                {
                    Capture[] cam = new Capture[directshow_filter_index.Length];
                    int[] camera_filter_index = new int[directshow_filter_index.Length];
                    PictureBox[] preview = new PictureBox[directshow_filter_index.Length];

                    // get the devices
                    int no_of_cameras = 1;
                    string camera_devices = commandline.GetParameterValue("d", parameters);
                    if (camera_devices.Contains(","))
                    {
                        string[] str = camera_devices.Split(',');
                        no_of_cameras = str.Length;
                        if (no_of_cameras > cam.Length) no_of_cameras = cam.Length;
                        for (int i = 0; i < no_of_cameras; i++)
                        {
                            camera_filter_index[i] = Convert.ToInt32(str[i]);
                        }
                    }
                    else camera_filter_index[0] = Convert.ToInt32(camera_devices);

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
                    bool auto_exposure = true;
                    int exposure = 127;
                    string exposure_str = commandline.GetParameterValue("e", parameters);
                    if (exposure_str != "")
                    {
                        exposure = Convert.ToInt32(exposure_str);
                        auto_exposure = false;
                    }

                    // initial frames to capture
                    int initial_frames = 1;
                    string initial_frames_str = commandline.GetParameterValue("i", parameters);
                    if (initial_frames_str != "") initial_frames = Convert.ToInt32(initial_frames_str);

                    // create camera objects
                    for (int i = 0; i < no_of_cameras; i++)
                    {
                        preview[i] = new PictureBox();
                        StartCamera(image_width, image_height, cam, directshow_filter_index, camera_filter_index, preview, i, exposure, auto_exposure);
                    }

                    if (no_of_cameras > 1)
                    {
                        for (int i = 0; i < no_of_cameras - 1; i += 2)
                        {
                            CaptureFrames(cam[i], cam[i + 1], initial_frames, "capture", "jpg");
                        }
                    }
                    else
                    {
                        CaptureFrame(cam[0], initial_frames, "capture", "jpg");
                    }

                    // dispose camera objects
                    for (int i = 0; i < no_of_cameras; i++)
                        if (cam[i] != null) cam[i].Dispose();

                }
                else
                {
                    Console.WriteLine("No camera devices were found");
                }
            }

            // keeping the dream alive
            GC.KeepAlive(m);
        }

        /// <summary>
        /// grabs frames from two cameras
        /// </summary>
        private static void CaptureFrames(
            Capture cam0, 
            Capture cam1,
            int initial_frames,
            string output_filename,
            string output_format)
        {
            if ((cam0 != null) && (cam1 != null))
            {
                IntPtr m_ip0 = IntPtr.Zero;
                IntPtr m_ip1 = IntPtr.Zero;

                for (int i = 0; i < 2; i++)
                {
                    Capture cam = cam0;
                    IntPtr m_ip = m_ip0;
                    if (i > 0)
                    {
                        cam = cam1;
                        m_ip = m_ip1;
                    }

                    // start rolling the cameras
                    if (cam.lastFrame != null)
                        cam.lastFrame.Dispose();

                    cam.Resume();

                    if (m_ip != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(m_ip);
                        m_ip = IntPtr.Zero;
                    }
                }

                // grab frames       
                Bitmap grabbed_image0 = null;
                Bitmap grabbed_image1 = null;                
                Parallel.For(0, 2, delegate(int j)
                {
                    for (int i = 0; i < initial_frames+1; i++)
                    {
                        if (j == 0)
                        {
                            grabbed_image0 = cam0.Grab(ref m_ip0, i == initial_frames);
                            if (m_ip0 != IntPtr.Zero)
                            {
                                Marshal.FreeCoTaskMem(m_ip0);
                                m_ip0 = IntPtr.Zero;
                            }
                        }
                        else
                        {
                            grabbed_image1 = cam1.Grab(ref m_ip1, i == initial_frames);
                            if (m_ip1 != IntPtr.Zero)
                            {
                                Marshal.FreeCoTaskMem(m_ip1);
                                m_ip1 = IntPtr.Zero;
                            }
                        }
                    }
                });
                if ((grabbed_image0 != null) &&
                    (grabbed_image1 != null))
                {
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    output_format = output_format.ToLower();
                    if (output_format == "bmp") format = System.Drawing.Imaging.ImageFormat.Bmp;
                    if (output_format == "png") format = System.Drawing.Imaging.ImageFormat.Png;
                    if (output_format == "gif") format = System.Drawing.Imaging.ImageFormat.Gif;
                    grabbed_image0.Save(output_filename + "0." + output_format, format);
                    grabbed_image1.Save(output_filename + "1." + output_format, format);
                }

                for (int i = 0; i < 2; i++)
                {
                    Capture cam = cam0;
                    if (i > 0) cam = cam1;

                    // pause the camera
                    cam.Pause();
                }
            }
        }

        /// <summary>
        /// grabs a frame from the camera
        /// </summary>
        private static void CaptureFrame(
            Capture cam,
            int initial_frames,
            string output_filename,
            string output_format)
        {
            if (cam != null)
            {
                IntPtr m_ip = IntPtr.Zero;

                // start rolling the cameras
                if (cam.lastFrame != null)
                    cam.lastFrame.Dispose();

                cam.Resume();

                if (m_ip != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(m_ip);
                    m_ip = IntPtr.Zero;
                }

                // grab frames                
                for (int i = 0; i < initial_frames; i++)
                {
                    cam.Grab(ref m_ip, false);

                    if (m_ip != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(m_ip);
                        m_ip = IntPtr.Zero;
                    }
                }
                Bitmap grabbed_image = cam.Grab(ref m_ip, true);
                if (grabbed_image != null)
                {
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    output_format = output_format.ToLower();
                    if (output_format == "bmp") format = System.Drawing.Imaging.ImageFormat.Bmp;
                    if (output_format == "png") format = System.Drawing.Imaging.ImageFormat.Png;
                    if (output_format == "gif") format = System.Drawing.Imaging.ImageFormat.Gif;
                    grabbed_image.Save(output_filename + "." + output_format, format);
                }

                // pause the camera
                cam.Pause();
            }
        }

        private static void StartCamera(
            int image_width,
            int image_height,
            Capture[] cam, 
            int[] directshow_filter_index,
            int[] camera_filter_index,
            PictureBox[] preview,
            int index,
            int exposure,
            bool auto_exposure)
        {
            cam[index] = new Capture(directshow_filter_index[camera_filter_index[index]], image_width, image_height, preview[index], true);
            if (cam[index] != null)
            {
                if (!cam[index].Active)
                {
                    // if still image capture mode fails
                    // use regular video capture

                    // trash the previous object
                    cam[index].Dispose();

                    // then try again
                    cam[index] = new Capture(directshow_filter_index[camera_filter_index[index]], image_width, image_height, preview[index], false);
                }

                if (cam[index] != null)
                {
                    // set the initial exposure value
                    if (auto_exposure)
                        cam[index].SetExposureAuto();
                    else
                        cam[index].SetExposure(exposure);

                    cam[index].Pause();
                }
            }
        }

        private static int[] GetFilterIndexes()
        {
            List<int> filter_indexes = new List<int>();

            string[] filter_names = Capture.GetDeviceNames();
            if (filter_names != null)
            {
                for (int i = 0; i < filter_names.Length; i++)
                {
                    if (!filter_names[i].ToLower().Contains("vfw"))
                    {
                        Console.WriteLine(filter_names[i]);
                        filter_indexes.Add(i);
                    }
                }
                if (filter_indexes.Count > 0)
                {
                    int[] filter_indexes2 = new int[filter_indexes.Count];
                    for (int i = 0; i < filter_indexes.Count; i++)
                        filter_indexes2[i] = filter_indexes[i];
                    return (filter_indexes2);
                }
                else return (null);
            }
            else return (null);
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
        }

        #endregion

    }
}
