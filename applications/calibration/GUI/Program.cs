/*
    Copyright (C) 2007  Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA. 
*/


using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using DirectX.Capture;

namespace WindowsApplication1
{    
    static class Program
    {        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new frmMain());
        }
    }


    public class interactiveObject
    {
        public String text;           //text associated with the object
        public float x, y;            //position of the object
        public Byte r, g, b;          //colour of the object
        public int size = 1;          //size of the object
        public int size_change = 0;   //change in size per frame
        public int counter = 0;       //persistence counter
        public float vx, vy;          //velocity
        public int shape = 0;         //shape of teh object
        public int state = 0;         //state of the object
        public int state_counter = 0; //counter used to change state
        public int no_of_states = 0;
        public int state_ticks = 0;
    }


    public class globals
    {
        public bool camera_initialised = false;

        public cameraSettings CamSettingsLeft = new cameraSettings();

        public String calibration_filename;
        public String calibration_filename_left;

        //the image is converted into a standard size for processing
        public int standard_width = 120;
        public int standard_height = 120;

        //bitmaps for left and right images
        public Image image_left;
        public Image image_right;
        public Byte[] left_bmp=null;
        public Byte[] right_bmp=null;
        public bool camerasRunning;

        public bool initStereo;
        public bool gvBusy;
        public int startupTicks;

        public struct ActiveLeft
        {
            public Filter Camera;
            public Capture CaptureInfo;
            public int Counter;
            public int CounterFrames;
            public String PathVideo;
        }

        public ActiveLeft CaptureInformationLeft;
        public Filters WDM_filters = new Filters();

        public void PrepareCamLeft(String PathVideo)
        {
            //String[] s;

            //s = PathVideo.Split(".")
            ConfParamCamLeft();
            //CaptureInformation.CaptureInfo.Filename = s(0) + CStr(CaptureInformation.Counter) + ".avi"
            CaptureInformationLeft.Counter ++;
            CaptureInformationLeft.CaptureInfo.RenderPreview();
        }

        public void selectCamera(bool leftImage, int cameraindex)
        {
                CaptureInformationLeft.Camera = WDM_filters.VideoInputDevices[cameraindex];
                CaptureInformationLeft.CaptureInfo = new Capture(CaptureInformationLeft.Camera, null);
                CamSettingsLeft.cameraName = WDM_filters.VideoInputDevices[cameraindex].Name;
                CamSettingsLeft.Save();
        }


        public int getCameraIndex(String cameraName)
        {
            //return the index of the given camera
            int i;
            Filter f;
            bool found = false;

            i = 0;
            while ((!found) && (i < WDM_filters.VideoInputDevices.Count))
            {
                f = WDM_filters.VideoInputDevices[i];
                if (f.Name == cameraName)
                    found = true;
                    else
                    i++;            
            }
            if (found)
                return(i);
                else
                return(-1);        
        }


        public void ConfParamCamLeft()
        {
            String[] s;
            Size size;
            double Rate;

            CaptureInformationLeft.CaptureInfo.Stop();

            // Change the compressor
            //CaptureInformation.CaptureInfo.VideoCompressor = WDM_filters.VideoCompressors(CaptureInformation.ConfWindow.cmbCompress.Items.IndexOf(CaptureInformation.ConfWindow.cmbCompress.Text))

            // Change the image size
            //s = CaptureInformationLeft.ConfWindow.cmbTam.Text.Split("x")
            if (CamSettingsLeft.resolution == "") CamSettingsLeft.resolution = "320x240";
            s = CamSettingsLeft.resolution.Split('x');
            size = new Size(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]));
            CaptureInformationLeft.CaptureInfo.FrameSize = size;

            // Change the number of frames per second
            //s = CaptureInformationLeft.ConfWindow.cmbFPS.Text.Split(" ")
            if (CamSettingsLeft.frameRate == "") CamSettingsLeft.frameRate = "30";
            s = CamSettingsLeft.frameRate.Split(' ');
            Rate = Convert.ToSingle(s[0]);
            CaptureInformationLeft.CaptureInfo.FrameRate = Rate;
        }


    }
}