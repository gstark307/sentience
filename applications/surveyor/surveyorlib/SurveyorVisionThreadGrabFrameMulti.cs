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

        /// <summary>
        /// update all cameras
        /// </summary>
        /// <param name="state">vision state</param>
        private void Update(SurveyorVisionClient[] state)
        {
            int time_step_mS = (int)(1000 / state[0].fps);
        
            DateTime data_last_requested = DateTime.Now;
            while (state[0].Streaming)
            {
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
                    TimeSpan diff = DateTime.Now.Subtract(data_last_requested);
                    if (diff.TotalMilliseconds > time_step_mS)
                    {
                        data_last_requested = DateTime.Now;

                        // clear the frame arrived flags on all cameras
                        for (int cam = 0; cam < state.Length; cam++)
                            state[cam].frame_arrived = false;

                        // initiate master pulse
                        for (int cam = 0; cam < state.Length; cam++)
                            state[cam].synchronisation_pulse = true;

                        // wait for frames to arrive
                        DateTime start_waiting = DateTime.Now;
                        bool all_frames_arrived = false;
                        while (state[0].Streaming)
                        {
                            // check all cameras
                            all_frames_arrived = true;
                            for (int cam = 0; cam < state.Length; cam++)
                            {
                                if (!state[cam].frame_arrived)
                                {
                                    all_frames_arrived = false;
                                    break;
                                }
                            }

                            // is our patience exhausted ?
                            TimeSpan elapsed = DateTime.Now.Subtract(start_waiting);
                            if (elapsed.TotalMilliseconds > time_step_mS - 20)
                                break;

                            Thread.Sleep(10);
                        }

                        // announce that all frames have arrived
                        if (all_frames_arrived) _callback(_data);

                        //Console.WriteLine("pulse " + DateTime.Now.ToString());
                    }
                }
                else
                {
                    Thread.Sleep(40);
                    _callback(_data);
                }
                Thread.Sleep(10);                
            }
        }
        
        
    }
}
