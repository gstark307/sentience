/*
    functions for grabbing images using DirectShow
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

//#define SHOW_TIMING
//#define SHOW_CAPTURE_TIME

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using sluggish.utilities;

namespace surveyor.vision
{
    public class WebcamVisionDirectShowBase
    {
        public string camera_devices = "0";
        public int image_width = 320;
        public int image_height = 240;
        public int initial_frames = 3;
        public string output_filename = "capture.jpg";
        public float exposure = 100;    // in the range 0 - 100
        public int min_exposure = 0;    // set this to the minimum exposure value for the camera
        public int max_exposure = 650;  // set this to the maximum exposure value for the camera
        public int no_of_cameras = 1;
        public int stereo_camera_index = -1;
        public bool save_images = false;

        //use pause rather than stop command on the media control
        public bool use_pause = true;

        // filenames which images were saved to
        public string left_image_filename;
        public string right_image_filename;

        // bitmaps captured
        public Bitmap left_image_bitmap;
        public Bitmap right_image_bitmap;

        // times when images were captured
        public DateTime left_image_capture;
        public DateTime right_image_capture;


        /// <summary>
        /// opens camera devices
        /// </summary>
        public virtual void Open()
        {
        }

        public virtual void Open(int device_index)
        {
        }

        public virtual void Open(int device_index0, int device_index1)
        {
        }

        /// <summary>
        /// closes camera devices
        /// </summary>
        public virtual void Close()
        {
        }

        /// <summary>
        /// grabs images from the cameras
        /// </summary>
        public virtual void Grab()
        {
        }  
		
    }
}
