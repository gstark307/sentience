/*
    Thread used to grab frames from webcams
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
        public bool _loop;
        
        // use the existance of this file to pause or resume frame capture
        public string PauseFile;        

        /// <summary>
        /// constructor
        /// </summary>
        public WebcamVisionThreadGrabFrameMulti(WaitCallback callback, object data, bool loop)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            _data = data;
            _loop = loop;
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
            DateTime reference_time = new DateTime(2008, 1, 1);
            int time_step_mS = (int)(1000 / state.fps);
            bool grabbed = false;

            time_step_mS = (int)(1000 / state.fps);

            DateTime last_called = DateTime.Now;

            bool looping = true;
            while (looping)
            {

                if (!Pause)
                {
                    // grab images
                    if (state.active_camera)
                    {
                        DateTime start_time = DateTime.Now;

                        state.Grab();
                        _callback(_data);
                        grabbed = true;

                        TimeSpan diff = DateTime.Now.Subtract(start_time);

                        while ((diff.TotalMilliseconds < time_step_mS) &&
                               (state.Running))
                        {
                            diff = DateTime.Now.Subtract(start_time);
                            Thread.Sleep(10);
                        }

                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    //for (int i = 0; i < 100; i++)
                        Thread.Sleep(5);

                    _callback(_data);
                }

                if (grabbed)
                {
                    if ((PauseFile != null) &&
                        (PauseFile != ""))
                    {
                        if (File.Exists(PauseFile))
                            Pause = true;
                        else
                            Pause = false;
                    }
                }

                // break out
                if (((_loop) && (!state.Running)) ||
                    (!_loop)) looping = false;
            }
        }
    }
}
