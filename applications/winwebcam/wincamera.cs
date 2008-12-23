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

//#define SHOW_TIMING
//#define SHOW_CAPTURE_TIME

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
    public class wincamera
    {

        public static void Update(
            string camera_devices,
            int image_width,
            int image_height,
            int initial_frames,
            string output_filename,
            int exposure,
            int no_of_captures,
            int capture_interval_mS)
        {
            int[] directshow_filter_index = GetFilterIndexes();

            if (directshow_filter_index != null)
            {
                Capture[] cam = new Capture[directshow_filter_index.Length];
                int[] camera_filter_index = new int[directshow_filter_index.Length];
                IntPtr[] m_ip = new IntPtr[directshow_filter_index.Length];
                PictureBox[] preview = new PictureBox[directshow_filter_index.Length];

                bool auto_exposure = false;
                if (exposure <= 0) auto_exposure = true;

                // get the devices
                int no_of_cameras = 1;
                if (camera_devices.Contains(","))
                {
                    string[] str = camera_devices.Split(',');
                    no_of_cameras = str.Length;
                    if (no_of_cameras > cam.Length) no_of_cameras = cam.Length;
                    for (int i = 0; i < no_of_cameras; i++)
                    {
                        camera_filter_index[i] = Convert.ToInt32(str[i]);
                        m_ip[i] = IntPtr.Zero;
                    }
                }
                else
                {
                    camera_filter_index[0] = Convert.ToInt32(camera_devices);
                    m_ip[0] = IntPtr.Zero;
                }


                Update(cam, preview, m_ip, no_of_cameras, image_width, image_height, directshow_filter_index, camera_filter_index, initial_frames, output_filename, exposure, auto_exposure, no_of_captures, capture_interval_mS);
            }
        }

        private static void Update(
            Capture[] cam,
            PictureBox[] preview,
            IntPtr[] m_ip,
            int no_of_cameras,
            int image_width,
            int image_height,
            int[] directshow_filter_index,
            int[] camera_filter_index,
            int initial_frames,
            string output_filename,
            int exposure,
            bool auto_exposure,
            int no_of_captures,
            int capture_interval_mS)
        {
            DateTime start_time = DateTime.Now;

            string output_format = "jpg";
            if (output_filename.Contains("."))
            {
                string[] str = output_filename.Split('.');
                output_filename = str[0];
                output_format = str[1];
            }

            // create camera objects
            //Parallel.For(0, no_of_cameras, delegate(int i)
            for (int i = no_of_cameras-1; i >= 0; i--)
            {
                preview[i] = new PictureBox();
                StartCamera(image_width, image_height, ref cam, directshow_filter_index, camera_filter_index, ref preview, i, exposure, auto_exposure);
            } //);

            TimeSpan setup_time = DateTime.Now.Subtract(start_time);
#if SHOW_TIMING 
            Console.WriteLine("Setup time: " + setup_time.TotalMilliseconds.ToString() + " mS");
#endif

            ulong prev_itt = 0;
            ulong itteration = 0;
            start_time = DateTime.Now;
            bool finished = false;
            int start_camera_index = 0;
            while (!finished)
            {
                Console.Write(".");
                if (no_of_cameras > 1)
                {
                    //for (int i = 0; i < no_of_cameras - 1; i += 2)
                    {
                        string filename = output_filename;
                        if (no_of_captures != 1) filename += itteration + "_";
                        if (no_of_cameras > 2) filename += start_camera_index.ToString() + (start_camera_index + 1).ToString() + "_";
                        CaptureFrames(cam[start_camera_index], cam[start_camera_index + 1], initial_frames, filename, output_format, m_ip[start_camera_index], m_ip[start_camera_index + 1]);
                    }
                    start_camera_index += 2;
                    if (start_camera_index >= no_of_cameras) start_camera_index = 0;
                }
                else
                {
                    string filename = output_filename;
                    if (no_of_captures != 1) filename += itteration + "_";
                    CaptureFrame(cam[0], initial_frames, filename, output_format, m_ip[0]);
                }

                itteration++;
                if ((no_of_captures > 0) &&
                    (itteration >= (ulong)no_of_captures))
                {
                    finished = true;
                }
                else
                {
                    for (int i = 0; i < 50; i++) Thread.Sleep(2);

                    // wait for a while
                    ulong itt = prev_itt;
                    while (itt <= prev_itt)
                    {
                        TimeSpan diff = DateTime.Now.Subtract(start_time);
                        itt = (ulong)(diff.TotalMilliseconds / capture_interval_mS);
                        Thread.Sleep(5);
                    }
                    prev_itt = itt;
                }
            }
            Console.WriteLine("");

            TimeSpan capture_time = DateTime.Now.Subtract(start_time);
#if SHOW_CAPTURE_TIME
            Console.WriteLine("Capture time: " + capture_time.TotalMilliseconds.ToString() + " mS");
#endif
            start_time = DateTime.Now;

            // dispose camera objects
            for (int i = 0; i < no_of_cameras; i++)
                if (cam[i] != null) cam[i].Dispose();

            TimeSpan close_time = DateTime.Now.Subtract(start_time);
#if SHOW_TIMING
            Console.WriteLine("Close time: " + close_time.TotalMilliseconds.ToString() + " mS");
#endif
        }

        /// <summary>
        /// grabs frames from two cameras
        /// </summary>
        private static void CaptureFrames(
            Capture cam0,
            Capture cam1,
            int initial_frames,
            string output_filename,
            string output_format,
            IntPtr m_ip0,
            IntPtr m_ip1)
        {
            const int step_size = 5; // when checking if frames are blank

            if ((cam0 != null) && (cam1 != null))
            {
                /*
                if (m_ip0 != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(m_ip0);
                    m_ip0 = IntPtr.Zero;
                }
                
                if (m_ip1 != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(m_ip1);
                    m_ip1 = IntPtr.Zero;
                }
                 */

                //Parallel.For(0, 2, delegate(int i)
                for (int i = 0; i < 2; i++)
                {
                    Capture cam = cam0;
                    if (i > 0) cam = cam1;

                    // start rolling the cameras
                    if (cam.lastFrame != null)
                        cam.lastFrame.Dispose();

                    cam.Resume();
                } // );

                for (int i = 0; i < 50; i++)
                    Thread.Sleep(2);

                // grab frames       
                Bitmap grabbed_image0 = null;
                Bitmap grabbed_image1 = null;
                bool is_blank0 = true;
                bool is_blank1 = true;
                //Parallel.For(0, 2, delegate(int j)
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < initial_frames + 1; i++)
                    {

                        if (j == 0)
                        {
                            /*
                            if (m_ip0 != IntPtr.Zero)
                            {
                                Marshal.FreeCoTaskMem(m_ip0);
                                m_ip0 = IntPtr.Zero;
                            }
                             */

                            grabbed_image0 = cam0.Grab(ref m_ip0, true);
                            is_blank0 = IsBlankFrame(grabbed_image0, step_size);
                            if (!is_blank0) break;
                        }
                        else
                        {
                            /*
                            if (m_ip1 != IntPtr.Zero)
                            {
                                Marshal.FreeCoTaskMem(m_ip1);
                                m_ip1 = IntPtr.Zero;
                            }
                             */

                            grabbed_image1 = cam1.Grab(ref m_ip1, true);
                            is_blank1 = IsBlankFrame(grabbed_image1, step_size);
                            if (!is_blank1) break;
                        }
                    }
                } // );

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
                    //cam.Pause();
                    cam.Stop();
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
            string output_format,
            IntPtr m_ip)
        {
            if (cam != null)
            {
                // start rolling the cameras
                if (cam.lastFrame != null)
                    cam.lastFrame.Dispose();

                if (m_ip != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(m_ip);
                    m_ip = IntPtr.Zero;
                }

                cam.Resume();

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
                //cam.Pause();
                cam.Stop();
            }
        }

        private static void StartCamera(
            int image_width,
            int image_height,
            ref Capture[] cam,
            int[] directshow_filter_index,
            int[] camera_filter_index,
            ref PictureBox[] preview,
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

                    //for (int i = 0; i < 50; i++) Thread.Sleep(2);

                    //cam[index].Pause()
                    cam[index].Stop();
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

        /// <summary>
        /// is teh given bitmap a blank frame ?
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        /// <param name="step_size">sampling step size</param>
        /// <returns>true if blank</returns>
        private static bool IsBlankFrame(Bitmap bmp, int step_size)
        {
            bool is_blank = true;
            if (bmp != null)
            {
                byte[] image = new byte[bmp.Width * bmp.Height * 3];

                int i = image.Length - 1;
                while ((is_blank) && (i > 0))
                {
                    if (image[i] != 0) is_blank = false;
                    i -= step_size;
                }
            }
            return (is_blank);
        }
    }
}
