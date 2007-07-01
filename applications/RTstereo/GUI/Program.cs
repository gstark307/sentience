/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
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


    public class globals
    {
        public bool camera_initialised = false;

        //default camera resolution
        public int default_resolution_width = 320; //640;
        public int default_resolution_height = 240; //480;

        public cameraSettings CamSettingsLeft = new cameraSettings();
        public cameraSettings CamSettingsRight = new cameraSettings();

        public String calibration_filename;

        //the image is converted into a standard size for processing
        public int standard_width = 320;
        public int standard_height = 240;

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

        public struct ActiveRight
        {
            public Filter Camera;
            public Capture CaptureInfo;
            public int Counter;
            public int CounterFrames;
            public String PathVideo;
        }

        public ActiveLeft CaptureInformationLeft;
        public ActiveRight CaptureInformationRight;
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

        public void PrepareCamRight(String PathVideo)
        {
            //String[] s;

            //s = PathVideo.Split(".")
            ConfParamCamRight();
            //CaptureInformation.CaptureInfo.Filename = s(0) + CStr(CaptureInformation.Counter) + ".avi"
            CaptureInformationRight.Counter ++;
            CaptureInformationRight.CaptureInfo.RenderPreview();
        }


        public void selectCamera(bool leftImage, int cameraindex)
        {
            if (leftImage)
            {
                CaptureInformationLeft.Camera = WDM_filters.VideoInputDevices[cameraindex];
                CaptureInformationLeft.CaptureInfo = new Capture(CaptureInformationLeft.Camera, null);
                CamSettingsLeft.cameraName = WDM_filters.VideoInputDevices[cameraindex].Name;
                CamSettingsLeft.Save();
            }
            else
            {
                CaptureInformationRight.Camera = WDM_filters.VideoInputDevices[cameraindex];
                CaptureInformationRight.CaptureInfo = new Capture(CaptureInformationRight.Camera, null);
                CamSettingsRight.cameraName = WDM_filters.VideoInputDevices[cameraindex].Name;
                CamSettingsRight.Save();
            }
        }


        public int getCameraIndexContaining(String containsCameraName, int index)
        {
            //return the index of the given camera
            Filter f;
            bool found = false;
            int idx = 0;

            int i = 0;
            while ((!found) && (i < WDM_filters.VideoInputDevices.Count))
            {
                f = WDM_filters.VideoInputDevices[i];
                if (f.Name.Contains(containsCameraName))
                {
                    if (index == idx)
                    {
                        found = true;
                    }
                    else i++;
                    idx++;
                }
                else i++;
            }
            if (found)
                return (i);
            else
                return (-1);
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
                if (f.Name.StartsWith(cameraName))
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
            if (CamSettingsLeft.resolution == "") CamSettingsLeft.resolution = Convert.ToString(default_resolution_width) + "x" + Convert.ToString(default_resolution_height);
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

        public void ConfParamCamRight()
        {
            String[] s;
            Size size;
            double Rate;

            CaptureInformationRight.CaptureInfo.Stop();

            // Change the compressor
            //CaptureInformation.CaptureInfo.VideoCompressor = WDM_filters.VideoCompressors(CaptureInformation.ConfWindow.cmbCompress.Items.IndexOf(CaptureInformation.ConfWindow.cmbCompress.Text))

            // Change the image size
            if (CamSettingsRight.resolution == "") CamSettingsRight.resolution = Convert.ToString(default_resolution_width) + "x" + Convert.ToString(default_resolution_height);
            s = CamSettingsRight.resolution.Split('x');
            size = new Size(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]));
            CaptureInformationRight.CaptureInfo.FrameSize = size;

            // Change the number of frames per second
            if (CamSettingsRight.frameRate == "") CamSettingsRight.frameRate = "30";
            s = CamSettingsRight.frameRate.Split(' ');
            Rate = Convert.ToSingle(s[0]);
            CaptureInformationRight.CaptureInfo.FrameRate = Rate;
        }


    }
}