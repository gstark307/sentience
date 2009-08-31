/*
    Thread used to replay a sequence of actions previously recorded
    Copyright (C) 2009 Bob Mottram
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
    /// 
    /// </summary>
    public class SurveyorReplayThread
    {
        private WaitCallback _callback;
        public bool Pause;
        private string teleop_file;
        private string zip_utility;
        private string record_path;
        private string log_identifier;
        private SurveyorVisionStereo state;
        
        /// <summary>
        /// constructor
        /// </summary>
        public SurveyorReplayThread(
            WaitCallback callback,
            SurveyorVisionStereo state,
            string teleop_file,
            string zip_utility,
            string record_path,
            string log_identifier)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            this.teleop_file = teleop_file;
            this.zip_utility = zip_utility;
            this.record_path = record_path;
            this.log_identifier = log_identifier;
            this.state = state;
        }

        /// <summary>
        /// ThreadStart delegate
        /// </summary>
        public void Execute()
        {
            Update();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Update()
        {
            state.Replay(
                teleop_file,
                zip_utility,
                record_path,
                log_identifier);
                         
            _callback(state);
        }
        
    }
}
