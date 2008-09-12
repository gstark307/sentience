/*
    Stereo vision for Surveyor robots
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
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    public class WebcamVisionStereo : BaseVisionStereo
    {
        protected string[] camera_device;
        public int skip_frames = 2;
                
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="left_camera_device">device name of the left camera (eg. /dev/video0)</param>
        /// <param name="right_camera_device">device name of the left camera (eg. /dev/video1)</param>
        /// <param name="broadcast_port">port number on which to broadcast stereo feature data to other applications</param>
        public WebcamVisionStereo(string left_camera_device,
                                  string right_camera_device,
                                  int broadcast_port) : base (broadcast_port)
        {
            device_name = "Webcam stereo camera";
            
            camera_device = new string[2];
            camera_device[0] = left_camera_device;
            camera_device[1] = right_camera_device;
        }
        
        #endregion

        #region "callbacks"
        
        private void FrameGrabCallback(object state)
        {
            // pause or resume grabbing frames from the cameras
            if (correspondence != null)
            {
                if ((!UpdateWhenClientsConnected) ||
                    ((UpdateWhenClientsConnected) && (correspondence.GetNoOfClients() > 0)))
                    grab_frames.Pause = false;
                else
                    grab_frames.Pause = true;
            }
        }

        /// <summary>
        /// grab a images using the fswebcam utility
        /// </summary>
        public void GrabFswebcam()
        {
            string command_str;
            
            command_str = "fswebcam -d " + camera_device[0] + "," + camera_device[1];
            command_str += " -r " + image_width.ToString() + "x" + image_height.ToString();
            command_str += " --no-banner";
            command_str += " -S " + skip_frames.ToString();
            if (exposure > 0) command_str += " -s brightness=" + exposure.ToString() + "%";
            command_str += " --save capture_.jpg";
            
            string left_image_filename = "capture_0.jpg";
            string right_image_filename = "capture_1.jpg";
            
            // delete any existing images
            for (int cam = 0; cam < 2; cam++)
            {
                if (File.Exists("capture_" + cam.ToString() + ".jpg"))
                {
                    try
                    {
                        File.Delete("capture_" + cam.ToString() + ".jpg");
                    }
                    catch
                    {
                    }
                }
            }
            
            bool command_succeeded = false;
            Process proc = new Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = command_str;
            try
            {
                proc.Start();
                command_succeeded = true;
            }
            catch
            {
            }

            if (!command_succeeded)
            {
                Console.WriteLine("Command failed.  fswebcam may not be installed.");
            }
            else
            {
                proc.WaitForExit();
                proc.Close();          

                // wait for the file to appear
                const int timeout_secs = 10;
                DateTime start_time = DateTime.Now;
                int seconds_elapsed = 0;
                while ((!File.Exists(right_image_filename)) &&
                       (seconds_elapsed < timeout_secs))
                {
                    System.Threading.Thread.Sleep(50);
                    TimeSpan diff = DateTime.Now.Subtract(start_time);
                    seconds_elapsed = (int)diff.TotalSeconds;
                }

                if ((File.Exists(left_image_filename)) &&
                    (File.Exists(right_image_filename)))
                {
                    // grab the data from the captured images
                    Bitmap[] bmp = new Bitmap[2];
                    
                    for (int cam = 0; cam < 2; cam++)
                    {
                        bmp[cam] = (Bitmap)Bitmap.FromFile("capture_" + cam.ToString() + ".jpg");                        
                        if (bmp[cam] == null) break;
                        
                        image_width = bmp[cam].Width;
                        image_height = bmp[cam].Height;
                        
                        byte[] raw_image_data = new byte[image_width * image_height * 3];
                        BitmapArrayConversions.updatebitmap(bmp[cam], raw_image_data);
                    }
                    
                    if ((bmp[0] != null) && (bmp[1] != null))
                    {
                        if (calibration_pattern != null)
                        {
                            if (!show_left_image)
                                SurveyorCalibration.DetectDots(bmp[0], ref edge_detector, calibration_survey[0], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[0]);
                            else
                                SurveyorCalibration.DetectDots(bmp[1], ref edge_detector, calibration_survey[1], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[1]);
                        }

                        RectifyImages(bmp[0], bmp[1]);
                                             
                        Process(bmp[0], bmp[1]);
                        
                        // save images to file
                        if (Record)
                        {
                            RecordFrameNumber++;
                            bmp[0].Save("raw0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            bmp[1].Save("raw1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            if ((rectified[0] != null) && (rectified[0] != null))
                            {
                                rectified[0].Save("rectified0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                rectified[1].Save("rectified1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                        }
                    }

                }

            }
            
        }
        
        #endregion
        
        #region "starting and stopping"

        private WebcamVisionThreadGrabFrameMulti grab_frames;

        public override void Run()
        {
            if (!Running)
            {
                // create a thread to send the master pulse
                grab_frames = new WebcamVisionThreadGrabFrameMulti(new WaitCallback(FrameGrabCallback), this);        
                sync_thread = new Thread(new ThreadStart(grab_frames.Execute));
                sync_thread.Priority = ThreadPriority.Normal;
                Running = true;
                sync_thread.Start();                
                Console.WriteLine("Stereo camera active");
            }
        }

        public override void Stop()
        {
            if (Running)
            {
                Running = false;
                if (sync_thread != null) sync_thread.Abort();
            }
        }
        
        #endregion
        
        #region "displaying images"
       
        protected override void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
        }

        #endregion
    }
}
