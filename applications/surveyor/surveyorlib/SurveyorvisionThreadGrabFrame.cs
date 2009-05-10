/*
    Thread used to grab images from a camera
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
    /// this thread is used to update the vision system
    /// </summary>
    public class SurveyorVisionThreadGrabFrame
    {
        private WaitCallback _callback;
        private object _data;
        public bool Halt;

        /// <summary>
        /// constructor
        /// </summary>
        public SurveyorVisionThreadGrabFrame(WaitCallback callback, object data)
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
            SurveyorVisionClient state = (SurveyorVisionClient)_data;
            Update(state);            
        }

        /// <summary>
        /// update vision
        /// </summary>
        /// <param name="state">vision state</param>
        private void Update(SurveyorVisionClient state)
        {
            DateTime data_last_requested = DateTime.Now;
            while ((state.Streaming) && (!Halt))
            {
                switch(state.grab_mode)
                {
                    // for a monocular camera grab frames at regular intervals
                    case SurveyorVisionClient.GRAB_MONOCULAR:
                    {
                        TimeSpan diff = DateTime.Now.Subtract(data_last_requested);
                        if (diff.TotalMilliseconds > 1000 / state.fps)
                        {                
                            data_last_requested = DateTime.Now;
                            //state.RequestResolution640x480();
                            state.RequestFrame();                            
                        }
                        break;
                    }
                    
                    // when using multiple cameras wait for a master synchronisation pulse
                    case SurveyorVisionClient.GRAB_MULTI_CAMERA:
                    {
                        if (state.synchronisation_pulse)
                        {
                            state.synchronisation_pulse = false;
                            //state.RequestResolution640x480();
                            state.RequestFrame();
                            //Console.WriteLine("grab " + DateTime.Now.ToString());
                        }
                        break;
                    }                    
                }
                
                _callback(_data);
                
                Thread.Sleep(10);                
            }
        }
        
        
    }
}
