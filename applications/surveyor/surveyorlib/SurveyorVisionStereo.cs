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
using System.IO;
using System.Threading;
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    public class SurveyorVisionStereo
    {
        private SurveyorVisionClient[] camera;
        private string host;
        private int[] port_number;
        public int fps = 10;
        
        // whether to record raw camera images
        public bool Record;
        public ulong RecordFrameNumber;
        
        // whether to show the left or right image during calibration
        public bool show_left_image;
        public Bitmap calibration_pattern;
        
        public CalibrationSurvey[] calibration_survey;
        public int[][] calibration_map;
        public int[][,,] calibration_map_inverse;
        
        // rectified images
        public Bitmap[] rectified;
        
        // what type of image should be displayed
        public const int DISPLAY_RAW = 0;
        public const int DISPLAY_CALIBRATION_DOTS = 1;
        public const int DISPLAY_CALIBRATION_GRID = 2;
        public const int DISPLAY_CALIBRATION_DIFF = 3;
        public const int DISPLAY_RECTIFIED = 4;
        public int display_type = DISPLAY_CALIBRATION_GRID;
                
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
            
            calibration_survey = new CalibrationSurvey[2];
            calibration_map = new int[2][];
            calibration_map_inverse = new int[2][,,];
            calibration_survey[0] = new CalibrationSurvey();
            calibration_survey[1] = new CalibrationSurvey();
            
            rectified = new Bitmap[2];
            
            // delete and previously recorded images
            string[] victims = Directory.GetFiles(".", "raw*.jpg");
            if (victims != null)
            {
                for (int v = 0; v < victims.Length; v++)
                    File.Delete(victims[v]);
            }
        }
        
        #endregion
        
        /// <summary>
        /// rectifies the given images
        /// </summary>
        /// <param name="left_image">left image bitmap</param>
        /// <param name="right_image">right image bitmap</param>
        protected void RectifyImages(Bitmap left_image, Bitmap right_image)
        {
            for (int cam = 0; cam < 2; cam++)
            {
                Bitmap bmp = left_image;
                if (cam == 1) bmp = right_image;
                
                if (calibration_survey[cam] != null)
                {
                    polynomial distortion_curve = calibration_survey[cam].best_fit_curve;
                    if (distortion_curve != null)
                    {
                        if (calibration_map[cam] == null)
                        {
                            SurveyorCalibration.updateCalibrationMap(
                                bmp.Width, bmp.Height, distortion_curve,
                                1, 0, 
                                calibration_survey[cam].centre_of_distortion_x,calibration_survey[cam].centre_of_distortion_y,
                                ref calibration_map[cam], ref calibration_map_inverse[cam]);
                        }
                        
                        byte[] img = new byte[bmp.Width * bmp.Height * 3];
                        BitmapArrayConversions.updatebitmap(bmp, img);
                        byte[] img_rectified = (byte[])img.Clone();                        
                    
                        int n = 0;
                        int[] map = calibration_map[cam];
                        for (int i = 0; i < img.Length; i+=3, n++)
                        {
                            int index = map[n]*3;
                            for (int col = 0; col < 3; col++)
                                img_rectified[i+col] = img[index+col];
                        }
                        
                        if (rectified[cam] == null)
                            rectified[cam] = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapArrayConversions.updatebitmap_unsafe(img_rectified, rectified[cam]);
                    }
                }
            }
        }
        
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

                    if (calibration_pattern != null)
                    {
                        hypergraph dots = null;
                        if (!show_left_image)
                            dots = SurveyorCalibration.DetectDots(left, ref edge_detector, calibration_survey[0], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[0]);
                        else
                            dots = SurveyorCalibration.DetectDots(right, ref edge_detector, calibration_survey[1], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[1]);
                    }
                    
                    RectifyImages(left, right);
                                                            
                    Process(left, right);
                    
                    // save images to file
                    if (Record)
                    {
                        RecordFrameNumber++;
                        left.Save("raw0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        right.Save("raw1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    
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
        
        #region "edge detection"
                
        protected Bitmap edges;
        protected Bitmap linked_dots;
        protected Bitmap grid;
        protected Bitmap grid_diff;
        private EdgeDetectorCanny edge_detector;
        
        protected Bitmap DetectEdges(Bitmap bmp, EdgeDetectorCanny edge_detector,
                                     ref hypergraph dots)
        {
            byte[] image_data = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, image_data);
            
            if (edge_detector == null) edge_detector = new EdgeDetectorCanny();
            edge_detector.automatic_thresholds = true;
            edge_detector.connected_sets_only = true;
            byte[] edges_data = edge_detector.Update(image_data, bmp.Width, bmp.Height);

            edges_data = edge_detector.GetConnectedSetsImage(image_data, 10, bmp.Width / SurveyorCalibration.dots_across * 3, true, ref dots);
            
            Bitmap edges_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(edges_data, edges_bmp);
            
            return(edges_bmp);
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
