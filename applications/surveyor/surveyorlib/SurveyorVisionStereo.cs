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
using System.Threading;
using System.Drawing;

namespace surveyor.vision
{
    public class SurveyorVisionStereo
    {
        private SurveyorVisionClient[] camera;
        private string host;
        private int[] port_number;
        public int fps = 10;
                
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="host">host name or IP address</param>
        /// <param name="port_number_left">port number for the left camera</param>
        /// <param name="port_number_right">port number for the right camera</param>
        public SurveyorVisionStereo(string host,
                                    int port_number_left,
                                    int port_number_right)
        {
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

        private bool busy_processing;

        /// <summary>
        /// both images have arrived and are awaiting processing
        /// </summary>
        /// <param name="state"></param>
        private void FrameGrabCallback(object state)
        {
            SurveyorVisionClient[] istate = (SurveyorVisionClient[])state;
            if ((istate[0].current_frame != null) && 
                (istate[1].current_frame != null))
            {
                if (!busy_processing)
                {
                    Bitmap left = istate[0].current_frame;
                    Bitmap right = istate[1].current_frame;
                    
                    busy_processing = true;
                    Process(left, right);
                    busy_processing = false;
                }
            }
        }

        #endregion
        
        #region "starting and stopping"
        
        private Thread sync_thread;
        public bool Running;
        
        public void Run()
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
                SurveyorVisionThreadGrabFrameMulti grab_frames = new SurveyorVisionThreadGrabFrameMulti(new WaitCallback(FrameGrabCallback), camera);        
                sync_thread = new Thread(new ThreadStart(grab_frames.Execute));
                sync_thread.Priority = ThreadPriority.Normal;
                sync_thread.Start();   
                Running = true;
                Console.WriteLine("Stereo camera active on " + host);
            }
        }

        public void Stop()
        {
            if (Running)
            {
                for (int cam = 0; cam < 2; cam++)
                {
                    camera[cam].StopStream();
                    camera[cam].Stop();
                }

                
            }            
        }
        
        #endregion
        
        #region "getters and setters"
        
        public void SetFramesPerSecond(int fps)
        {
            this.fps = fps;
            camera[0].fps = fps;
            camera[1].fps = fps;
        }
        
        #endregion
        
        #region "displaying images"
       
        protected virtual void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
        }
        
        #endregion
        
        #region "process images"

        public virtual void Process(Bitmap left_image, Bitmap right_image)
        {
            DisplayImages(left_image, right_image);
        }
        
        #endregion
        
    }
}
