/*
    functions for grabbing images using DirectShow
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
using sluggish.utilities;

namespace surveyor.vision
{
    public class WebcamVisionDirectShow : WebcamVisionDirectShowBase
    {
        protected int start_camera_index = 0;
        protected string output_format;
        protected int[] directshow_filter_index;
        protected WebcamVisionDirectShowCapture[] cam;
        protected int[] camera_filter_index;
        protected IntPtr[] m_ip;
        protected PictureBox[] preview;

        /// <summary>
        /// destructor
        /// </summary>
        ~WebcamVisionDirectShow()
        {
            if (cam != null) Close();
        }

        /// <summary>
        /// opens camera devices
        /// </summary>
        public override void Open()
        {
            Initialise();
        }

        public override void Open(int device_index)
        {
            camera_devices = device_index.ToString();

            Initialise();
        }

        public override void Open(int device_index0, int device_index1)
        {
            camera_devices = device_index0.ToString() + "," + device_index1.ToString();

            Initialise();
        }

        protected void Initialise()
        {
            directshow_filter_index = GetFilterIndexes();

            if (directshow_filter_index != null)
            {
                cam = new WebcamVisionDirectShowCapture[directshow_filter_index.Length];
                camera_filter_index = new int[directshow_filter_index.Length];
                m_ip = new IntPtr[directshow_filter_index.Length];
                preview = new PictureBox[directshow_filter_index.Length];

                bool auto_exposure = false;
                //if (exposure <= 0) 
                    //auto_exposure = true;

                // get the devices
                no_of_cameras = 1;
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

                output_format = "jpg";
                if (output_filename.Contains("."))
                {
                    string[] str = output_filename.Split('.');
                    output_filename = str[0];
                    output_format = str[1];
                }

                // create camera objects
                for (int i = no_of_cameras - 1; i >= 0; i--)
                {
                    preview[i] = new PictureBox();
                    StartCamera(image_width, image_height, ref cam, directshow_filter_index, camera_filter_index, ref preview, i, exposure, min_exposure, max_exposure, auto_exposure, use_pause);
                }
            }
        }

        /// <summary>
        /// closes camera devices
        /// </summary>
        public override void Close()
        {
            if (cam != null)
            {
                // dispose camera objects
                for (int i = 0; i < no_of_cameras; i++)
                    if (cam[i] != null) cam[i].Dispose();
                cam = null;
            }
        }

        /// <summary>
        /// grabs images from the cameras
        /// </summary>
        public override void Grab()
        {
            if (cam != null)
            {
                if (no_of_cameras > 1)
                {
                    string filename = output_filename;
                    if (stereo_camera_index > -1) filename += stereo_camera_index.ToString() + "_";
                    if (no_of_cameras > 2) filename += start_camera_index.ToString() + (start_camera_index + 1).ToString() + "_";
                    CaptureFrames(
                        start_camera_index,
                        cam,
                        initial_frames, filename, 
                        output_format, 
                        m_ip, 
                        save_images,
                        min_exposure + (int)(exposure * (max_exposure - min_exposure) / 100),
                        use_pause,
                        ref left_image_filename, 
                        ref right_image_filename, 
                        ref left_image_bitmap, 
                        ref right_image_bitmap,
                        ref left_image_capture,
                        ref right_image_capture);

                    start_camera_index += 2;
                    if (start_camera_index >= no_of_cameras) start_camera_index = 0;
                }
                else
                {
                    string filename = output_filename;
                    if (stereo_camera_index > -1) filename += stereo_camera_index.ToString() + "_";
                    CaptureFrame(cam[0], initial_frames, filename, output_format, ref m_ip[0], min_exposure + (int)(exposure * (max_exposure - min_exposure) / 100), use_pause);
                }

            }
        }  

        /// <summary>
        /// grabs frames from two cameras
        /// </summary>
        protected static void CaptureFrames(
            int start_camera_index,
            WebcamVisionDirectShowCapture[] cam,
            int initial_frames,
            string output_filename,
            string output_format,
            IntPtr[] m_ip,
            bool save_images,
            int exposure,
            bool use_pause,
            ref string left_image_filename,
            ref string right_image_filename,
            ref Bitmap left_image_bitmap,
            ref Bitmap right_image_bitmap,
            ref DateTime left_image_capture,
            ref DateTime right_image_capture)
        {
            const int step_size = 5; // when checking if frames are blank
            if (left_image_bitmap != null) left_image_bitmap.Dispose();
            if (right_image_bitmap != null) right_image_bitmap.Dispose();

            if ((cam[start_camera_index] != null) && (cam[start_camera_index+1] != null))
            {
                /*
                if (m_ip[start_camera_index] != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(m_ip[start_camera_index]);
                    m_ip[start_camera_index] = IntPtr.Zero;
                }

                if (m_ip[start_camera_index+1] != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(m_ip[start_camera_index+1]);
                    m_ip[start_camera_index+1] = IntPtr.Zero;
                }
                */

                for (int i = 0; i < 2; i++)
                {
                    WebcamVisionDirectShowCapture c = cam[start_camera_index];
                    if (i > 0) c = cam[start_camera_index+1];

                    // start rolling the cameras
                    if (c.lastFrame != null)
                        c.lastFrame.Dispose();

                    c.Resume();
                }

                
                // grab frames       
                Bitmap grabbed_image0 = null;
                Bitmap grabbed_image1 = null;
                bool is_blank0 = true;
                bool is_blank1 = true;
                DateTime left_image_cap = DateTime.Now;
                DateTime right_image_cap = DateTime.Now;
                //for (int j = 0; j < 2; j++)
                Parallel.For(0, 2, delegate(int j)
                {
                    for (int i = 0; i < initial_frames + 1; i++)
                    {

                        if (j == 0)
                        {
                            left_image_cap = DateTime.Now;
                            cam[start_camera_index].SetExposure(exposure);
                            grabbed_image0 = cam[start_camera_index].Grab(ref m_ip[start_camera_index], true);
                            is_blank0 = IsBlankFrame(grabbed_image0, step_size);
                            if ((!is_blank0) && (i > 1)) break;
                        }
                        else
                        {
                            right_image_cap = DateTime.Now;
                            cam[start_camera_index+1].SetExposure(exposure);
                            grabbed_image1 = cam[start_camera_index+1].Grab(ref m_ip[start_camera_index+1], true);
                            is_blank1 = IsBlankFrame(grabbed_image1, step_size);
                            if ((!is_blank0) && (i > 1)) break;
                        }
                    }
                } );
                
                left_image_capture = left_image_cap;
                right_image_capture = right_image_cap;

                if ((grabbed_image0 != null) &&
                    (grabbed_image1 != null))
                {
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    output_format = output_format.ToLower();
                    if (output_format == "bmp") format = System.Drawing.Imaging.ImageFormat.Bmp;
                    if (output_format == "png") format = System.Drawing.Imaging.ImageFormat.Png;
                    if (output_format == "gif") format = System.Drawing.Imaging.ImageFormat.Gif;
                    left_image_bitmap = grabbed_image0;
                    right_image_bitmap = grabbed_image1; 
                    left_image_filename = output_filename + "0." + output_format;
                    right_image_filename = output_filename + "1." + output_format;
                    try
                    {
                        if (save_images)
                        {
                            grabbed_image0.Save(left_image_filename, format);
                            grabbed_image1.Save(right_image_filename, format);
                        }
                    }
                    catch
                    {
                        left_image_filename = "";
                        right_image_filename = "";
                    }
                }                

                for (int i = 0; i < 2; i++)
                {
                    WebcamVisionDirectShowCapture c = cam[start_camera_index];
                    if (i > 0) c = cam[start_camera_index+1];

                    // stop the camera
                    if (use_pause)
                        c.Pause();
                    else
                        c.Stop();
                }
            }
        }

        /// <summary>
        /// grabs a frame from the camera
        /// </summary>
        protected static void CaptureFrame(
            WebcamVisionDirectShowCapture cam,
            int initial_frames,
            string output_filename,
            string output_format,
            ref IntPtr m_ip,
            int exposure,
            bool use_pause)
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
                cam.SetExposure(exposure);
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

                // stop the camera
                if (use_pause)
                    cam.Pause();
                else
                    cam.Stop();
            }
        }

        protected static void StartCamera(
            int image_width,
            int image_height,
            ref WebcamVisionDirectShowCapture[] cam,
            int[] directshow_filter_index,
            int[] camera_filter_index,
            ref PictureBox[] preview,
            int index,
            float exposure,
            int min_exposure,
            int max_exposure,
            bool auto_exposure,
            bool use_pause)
        {
            cam[index] = new WebcamVisionDirectShowCapture(directshow_filter_index[camera_filter_index[index]], image_width, image_height, preview[index], true);
            if (cam[index] != null)
            {
                if (!cam[index].Active)
                {
                    // if still image capture mode fails
                    // use regular video capture

                    // trash the previous object
                    cam[index].Dispose();

                    // then try again
                    cam[index] = new WebcamVisionDirectShowCapture(directshow_filter_index[camera_filter_index[index]], image_width, image_height, preview[index], false);
                }

                if (cam[index] != null)
                {
                    // set the initial exposure value
                    if (auto_exposure)
                        cam[index].SetExposureAuto();
                    else
                        cam[index].SetExposure(min_exposure + (int)(exposure*(max_exposure - min_exposure)/100));

                    if (use_pause)
                        cam[index].Pause();
                    else
                        cam[index].Stop();
                }
            }
        }

        protected static int[] GetFilterIndexes()
        {
            List<int> filter_indexes = new List<int>();

            string[] filter_names = WebcamVisionDirectShowCapture.GetDeviceNames();
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
        /// is the given bitmap a blank frame ?
        /// </summary>
        /// <param name="bmp">bitmap object</param>
        /// <param name="step_size">sampling step size</param>
        /// <returns>true if blank</returns>
        protected static bool IsBlankFrame(Bitmap bmp, int step_size)
        {
            bool is_blank = true;
            if (bmp != null)
            {
                byte[] image = new byte[bmp.Width * bmp.Height * 3];
                BitmapArrayConversions.updatebitmap(bmp, image);

                int i = image.Length - 1;
                while ((is_blank) && (i > 0))
                {
                    if (image[i] != 0) is_blank = false;
                    i -= step_size;
                }
                image = null;
            }
            return (is_blank);
        }		
		
    }
}
