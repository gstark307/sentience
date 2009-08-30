/*
    Stereo vision using webcams
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
        protected string PauseFile;   		
                
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="left_camera_device">device name of the left camera (eg. /dev/video0)</param>
        /// <param name="right_camera_device">device name of the left camera (eg. /dev/video1)</param>
        /// <param name="broadcast_port">port number on which to broadcast stereo feature data to other applications</param>
        /// <param name="fps">ideal frames per second</param>
        public WebcamVisionStereo(string left_camera_device,
                                  string right_camera_device,
                                  int broadcast_port,
                                  float fps) : base (broadcast_port, fps)
        {
            device_name = "Webcam stereo camera";
            
            camera_device = new string[2];
            camera_device[0] = left_camera_device;
            camera_device[1] = right_camera_device;
        }
        
        #endregion

        #region "pause and resume"

        public override void Pause()
        {
            if (grab_frames != null)
                grab_frames.Pause = true;
        }
        
        public override void Resume()
        {
            if (grab_frames != null)
                grab_frames.Pause = false;
        }
        
        public override void SetPauseFile(string filename)
        {
            PauseFile = filename;
            if (grab_frames != null)
                grab_frames.PauseFile = filename;
        }

        public override void ClearPauseFile()
        {
            PauseFile = "";
            if (grab_frames != null)
                grab_frames.PauseFile = null;
        }
                
        #endregion

        #region "callbacks"
        
        protected virtual void FrameGrabCallback(object state)
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

            if (!endless_thread)
            {
                sync_thread = new Thread(new ThreadStart(grab_frames.Execute));
                sync_thread.Priority = ThreadPriority.Normal;
                sync_thread.IsBackground = true;
                sync_thread.Start();
            }
        }

		public string capture_utility = "v4l2stereo";
		
        protected void GrabLinux()
        {
            string filename = "capture";

            // append temporary files path if specified
            if ((temporary_files_path != null) &&
                (temporary_files_path != ""))
            {
                if (temporary_files_path.EndsWith("/"))
                    filename = temporary_files_path + filename;
                else
                    filename = temporary_files_path + "/" + filename;
            }

            // Extract numbers from the camera device names
            // This is ised to uniquely identify devices so that
            // potentially more than one stereo camera could be running
            // at the same time
            string identifier = "";
            for (int cam = 0; cam < 2; cam++)
            {
                char[] ch = camera_device[cam].ToCharArray();
                for (int i = 0; i < ch.Length; i++)
                {
                    if ((ch[i] >= '0') && (ch[i] <= '9'))
                        identifier += ch[i];
                }
            }
            filename += identifier;

            string command_str = "fswebcam -q -d " + camera_device[0] + "," + camera_device[1];
            command_str += " -r " + image_width.ToString() + "x" + image_height.ToString();
            command_str += " --no-banner";
            command_str += " -S " + skip_frames.ToString();
            if (exposure > 0) command_str += " -s brightness=" + exposure.ToString() + "%";
            command_str += " --save " + filename + "_.jpg";

			// fswebcam -q -d /dev/video1,/dev/video2 -r 320x240 --no-banner -S 2 -s brightness=50% --save capture12_.jpg
			
			if (capture_utility == "v4l2stereo")
			{
			    command_str = "v4l2stereo --dev0 " + camera_device[0] + " ";
			    command_str += "--dev1 " + camera_device[1] + " ";
                if (flip_left_image) command_str += "--flipleft ";
                if (flip_right_image) command_str += "--flipright ";
			    command_str += "--save " + filename + "_ ";
			}
			
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(command_str);
            Console.WriteLine("");
            Console.WriteLine("");

			string left_image_filename = filename + "_0.jpg";
            string right_image_filename = filename + "_1.jpg";
			
            // delete any existing images
            for (int cam = 0; cam < 2; cam++)
            {
                if (File.Exists(filename + "_" + cam.ToString() + ".jpg"))
                {
                    try
                    {
                        File.Delete(filename + "_" + cam.ToString() + ".jpg");
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
                //proc.WaitForExit();
                proc.Close();

                // wait for the file to appear
                const int timeout_secs = 10;
                DateTime start_time = DateTime.Now;
                int seconds_elapsed = 0;
                while (((!File.Exists(left_image_filename)) ||
                        (!File.Exists(right_image_filename))) &&
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
                        try
                        {
                            bmp[cam] = (Bitmap)Bitmap.FromFile(filename + "_" + cam.ToString() + ".jpg");
                        }
                        catch
                        {
                            bmp[cam] = null;
                        }
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
                            string path = "";
                            if ((recorded_images_path != null) &&
                                (recorded_images_path != ""))
                            {
                                if (recorded_images_path.EndsWith("/"))
                                    path = recorded_images_path;
                                else
                                    path = recorded_images_path + "/";
                            }

                            RecordFrameNumber++;
                            DateTime t = DateTime.Now;
                            LogEvent(t, "RAW_L " + stereo_camera_index.ToString() + " raw" + identifier + "_0_" + RecordFrameNumber.ToString() + ".jpg", image_log);
                            LogEvent(t, "RAW_R " + stereo_camera_index.ToString() + " raw" + identifier + "_1_" + RecordFrameNumber.ToString() + ".jpg", image_log);                            
                            bmp[0].Save(path + "raw" + identifier + "_0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            bmp[1].Save(path + "raw" + identifier + "_1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            if ((rectified[0] != null) && (rectified[0] != null))
                            {
                                LogEvent(t, "REC_L " + stereo_camera_index.ToString() + " rectified" + identifier + "_0_" + RecordFrameNumber.ToString() + ".jpg", image_log);
                                LogEvent(t, "REC_R " + stereo_camera_index.ToString() + " rectified" + identifier + "_1_" + RecordFrameNumber.ToString() + ".jpg", image_log);
                                
                                rectified[0].Save(path + "rectified" + identifier + "_0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                rectified[1].Save(path + "rectified" + identifier + "_1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 2; i++)
                            if (bmp[i] == null) Console.WriteLine("Warning: Did not acquire image from " + camera_device[i]);
                    }

                }
                else
                {
                    if (!File.Exists(left_image_filename))
                        Console.WriteLine("Timed out: File not found " + left_image_filename + ".");
                    if (!File.Exists(right_image_filename))
                        Console.WriteLine("Timed out: File not found " + right_image_filename + ".");
                }

            }

            if (next_camera != null)
            {
                active_camera = false;
                Pause();
                next_camera.active_camera = true;
                next_camera.Resume();
            }
        }

        /// <summary>
        /// grab a images using the fswebcam utility
        /// </summary>
        public virtual void Grab()
        {
            GrabLinux();
        }
        
        #endregion
        
        #region "starting and stopping"

        protected WebcamVisionThreadGrabFrameMulti grab_frames;

        public override void Run()
        {
            if (!Running)
            {
                // create a thread to send the master pulse
                grab_frames = new WebcamVisionThreadGrabFrameMulti(new WaitCallback(FrameGrabCallback), this, endless_thread);        
                grab_frames.PauseFile = PauseFile;
                sync_thread = new Thread(new ThreadStart(grab_frames.Execute));
                sync_thread.Priority = ThreadPriority.Normal;
                sync_thread.IsBackground = true;
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
