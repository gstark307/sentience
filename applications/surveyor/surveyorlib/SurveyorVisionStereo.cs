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
using System.Threading;
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    public class SurveyorVisionStereo : BaseVisionStereo
    {
        private SurveyorVisionClient[] camera;
        private string host;
        private int[] port_number;

        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="host">host name or IP address</param>
        /// <param name="port_number_left">port number for the left camera</param>
        /// <param name="port_number_right">port number for the right camera</param>
        /// <param name="broadcast_port">port number on which to broadcast stereo feature data to other applications</param>
        /// <param name="fps">ideal frames per second</param>
        /// <param name="phase_degrees">frame capture phase offset</param>
        public SurveyorVisionStereo(string host,
                                    int port_number_left,
                                    int port_number_right,
                                    int broadcast_port,
                                    float fps,
                                    int phase_degrees) : base(broadcast_port, fps, phase_degrees)
        {
            device_name = "Surveyor stereo camera";
            this.host = host;
            
            camera = new SurveyorVisionClient[2];
            for (int cam = 0; cam < 2; cam++)
            {
                camera[cam] = new SurveyorVisionClient();
                camera[cam].grab_mode = SurveyorVisionClient.GRAB_MULTI_CAMERA;
            }
            
            port_number = new int[2];
            port_number[0] = port_number_left;
            port_number[1] = port_number_right;            
        }
        
        #endregion
        
        #region "callbacks"

        protected bool busy_processing;

        /// <summary>
        /// both images have arrived and are awaiting processing
        /// </summary>
        /// <param name="state"></param>
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

            SurveyorVisionClient[] istate = (SurveyorVisionClient[])state;
            if ((istate[0].current_frame != null) && 
                (istate[1].current_frame != null))
            {

                Bitmap left = istate[0].current_frame;
                Bitmap right = istate[1].current_frame;

                image_width = left.Width;
                image_height = left.Height;
                
                if (!busy_processing)
                {
                    busy_processing = true;
                    if (calibration_pattern != null)
                    {
                        if (!show_left_image)
                            SurveyorCalibration.DetectDots(left, ref edge_detector, calibration_survey[0], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[0]);
                        else
                            SurveyorCalibration.DetectDots(right, ref edge_detector, calibration_survey[1], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[1]);
                    }

                    RectifyImages(left, right);
                                         
                    Process(left, right);
                    
                    // save images to file
                    if (Record)
                    {
                        RecordFrameNumber++;
                        left.Save("raw0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        right.Save("raw1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        if ((rectified[0] != null) && (rectified[0] != null))
                        {
                            rectified[0].Save("rectified0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            rectified[1].Save("rectified1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                    }
                    
                    busy_processing = false;
                }
            }
        }

        #endregion
        
        #region "starting and stopping"

        private SurveyorVisionThreadGrabFrameMulti grab_frames;
        
        public override void Run()
        {
            bool cameras_started = true;
            
            // start running the cameras
            for (int cam = 0; cam < 2; cam++)
            {
                camera[cam].fps = fps;
                camera[cam].Start(host, port_number[cam]);
                if (camera[cam].Running)
                {
                    camera[cam].StartStream();
                }
                else
                {
                    cameras_started = false;
                    break;
                }
            }
            
            if (cameras_started)
            {
                // create a thread to send the master pulse
                grab_frames = new SurveyorVisionThreadGrabFrameMulti(new WaitCallback(FrameGrabCallback), camera);        
                sync_thread = new Thread(new ThreadStart(grab_frames.Execute));
                sync_thread.Priority = ThreadPriority.Normal;
                sync_thread.Start();   
                Running = true;
                Console.WriteLine("Stereo camera active on " + host);
            }
        }

        public override void Stop()
        {
            if (Running)
            {
                for (int cam = 0; cam < 2; cam++)
                {
                    camera[cam].StopStream();
                    camera[cam].Stop();
                }
                if (sync_thread != null) sync_thread.Abort();
            }
        }
        
        #endregion
        
        #region "getters and setters"
        
        public override void SetFramesPerSecond(int fps)
        {
            this.fps = fps;
            camera[0].fps = fps;
            camera[1].fps = fps;
        }
        
        #endregion
        
        #region "displaying images"
       
        protected override void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
        }

        #endregion
       
        #region "process images"

        public override void Process(Bitmap left_image, Bitmap right_image)
        {        
            DisplayImages(left_image, right_image);
            StereoCorrespondence();
        }
        
        #endregion
        
    }
}
