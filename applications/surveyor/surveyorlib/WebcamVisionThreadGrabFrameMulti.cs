/*
    
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
    public class WebcamVisionThreadGrabFrameMulti
    {
        private WaitCallback _callback;
        private object _data;
        public bool Pause;
        private bool prev_Pause;

        /// <summary>
        /// constructor
        /// </summary>
        public WebcamVisionThreadGrabFrameMulti(WaitCallback callback, object data)
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
            WebcamVisionStereo state = (WebcamVisionStereo)_data;
            Update(state);
        }

        /// <summary>
        /// update all cameras
        /// </summary>
        /// <param name="state">vision state</param>
        private void Update(WebcamVisionStereo state)
        {   
            DateTime reference_time = new DateTime(2000, 1, 1);
            int time_step_mS = 1000 / state.fps;

            // calculate the phase offset, relative to some reference time in the ancient past
            TimeSpan reference_diff = DateTime.Now.Subtract(reference_time);
            int phase = (int)(reference_diff.TotalMilliseconds % time_step_mS);

            while ((state.Running) && (!Pause) && (phase < state.phase_degrees)) 
            {
                // calculate the phase offset
                reference_diff = DateTime.Now.Subtract(reference_time);
                phase = (int)(reference_diff.TotalMilliseconds % time_step_mS);

                Thread.Sleep(20);
            }
        
            while(state.Running)
            {
                time_step_mS = 1000 / state.fps;
                                
                DateTime last_called = DateTime.Now;
                
                if (!Pause)
                {
                    if (prev_Pause)
                    {
                        // wait for the correct phase
                        while ((state.Running) && (phase < state.phase_degrees)) 
                        {
                            // calculate the phase offset
                            reference_diff = DateTime.Now.Subtract(reference_time);
                            phase = (int)(reference_diff.TotalMilliseconds % time_step_mS);

                            Thread.Sleep(20);
                        }
                    }
                
                    // grab images
                    state.Grab();
                    _callback(_data);
                    
                    // calculate the phase offset, relative to some reference time in the ancient past
                    reference_diff = DateTime.Now.Subtract(reference_time);
                    phase = (int)(reference_diff.TotalMilliseconds % time_step_mS);

                    while ((phase < state.phase_degrees) && 
                           (state.Running))
                    {
                        // calculate the phase offset
                        reference_diff = DateTime.Now.Subtract(reference_time);
                        phase = (int)(reference_diff.TotalMilliseconds % time_step_mS);

                        Thread.Sleep(20);
                    }
                }
                else
                {
                    Thread.Sleep(40);
                    _callback(_data);
                }
                
                prev_Pause = Pause;
            }
        }
    }
}
