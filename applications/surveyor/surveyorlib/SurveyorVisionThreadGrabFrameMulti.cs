/*
    Thread used to send a master synchronising pulse to all cameras
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
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Threading;

namespace surveyor.vision
{
    /// <summary>
    /// this thread is used to update a vision system with multiple cameras
    /// </summary>
    public class SurveyorVisionThreadGrabFrameMulti
    {
        private WaitCallback _callback;
        private object _data;
        public bool Pause;
        
        // use the existance of this file to pause or resume frame capture
        public string PauseFile;

        /// <summary>
        /// constructor
        /// </summary>
        public SurveyorVisionThreadGrabFrameMulti(WaitCallback callback, object data)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            _data = data;
        }

        /// <summary>
        /// ThreadStart delegate
        /// </summary>
        public void Execute()
        {
            SurveyorVisionClient[] state = (SurveyorVisionClient[])_data;
            Update(state);
        }

		DateTime data_last_requested = DateTime.Now;
		
        /// <summary>
        /// update all cameras
        /// </summary>
        /// <param name="state">vision state</param>
        private void Update(SurveyorVisionClient[] state)
        {
            int time_step_mS = (int)(1000 / state[0].fps);
                    
            // If a cartain file exists then initiate pause
            // This provides a very simple mechanism for other programs
            // to start or stop the server
            if ((PauseFile != null) &&
                (PauseFile != ""))
            {
                if (File.Exists(PauseFile)) 
                    Pause = true;
                else
                    Pause = false;
            }
        
            if (!Pause)
            {
				//bool all_frames_arrived = false;
                TimeSpan diff = DateTime.Now.Subtract(data_last_requested);
                //if (diff.TotalMilliseconds > time_step_mS)
				if ((((state[0].frame_arrived) && (state[1].frame_arrived)) ||
				     (diff.TotalMilliseconds > time_step_mS)) &&
				     ((!state[0].current_frame_busy) && (!state[1].current_frame_busy)))
                {
                    data_last_requested = DateTime.Now;

                    for (int cam = 0; cam < state.Length; cam++)
					{
						// clear the frame arrived flags on all cameras
						state[cam].frame_arrived = false;
						// initiate master pulse
                        state[cam].synchronisation_pulse = true;
						state[cam].RequestFrame();
					}
				}
                // announce that all frames have arrived
				if ((state[0].frame_arrived) &&
				    (state[1].frame_arrived))
				{
					Console.WriteLine("data arrived");
			        state[0].frame_arrived = false;
			        state[1].frame_arrived = false;
				}
            }
            else
            {
                Thread.Sleep(40);
				Console.WriteLine("Paused");
            }
            Thread.Sleep(10);
		    _callback(_data);
        }
        
    }
}
