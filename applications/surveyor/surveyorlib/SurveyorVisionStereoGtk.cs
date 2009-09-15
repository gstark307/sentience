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
using System.Threading;
using System.Drawing;
using Gdk;
<<<<<<< .mine
using Gtk; //Gnome;
=======
using Gtk;
>>>>>>> .r784
using sluggish.utilities;
using sluggish.utilities.gtk;

namespace surveyor.vision
{    
    /// <summary>
    /// stereo vision object which displays images using Gtk
    /// </summary>
    public class SurveyorVisionStereoGtk : SurveyorVisionStereo
    {
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="host">host name or IP address</param>
        /// <param name="port_number_left">port number for the left camera</param>
        /// <param name="port_number_right">port number for the right camera</param>
        /// <param name="broadcast_port">port number on which to broadcast stereo feature data to other applications</param>
        /// <param name="fps">ideal frames per second</param>
        /// <param name="phase_degrees">frame capture phase offset</param>
        /// <param name="window">window object</param>
        public SurveyorVisionStereoGtk(string host,
                                       int port_number_left,
                                       int port_number_right,
                                       int broadcast_port,
                                       float fps,
		                               Gtk.Window window) : base (host, port_number_left, port_number_right, broadcast_port, fps)
        {
			this.window = window;
            display_image = new Gtk.Image[2];
        }
        
        #endregion
    
        #region "beeping"
        
        /// <summary>
        /// plays a sound
        /// </summary>
        /// <param name="sound_filename"></param>
        private void PlaySound(string sound_filename)
<<<<<<< .mine
        {
            //Gnome.Sound.Init("localhost");
            //Gnome.Sound.Play(sound_filename);
=======
        {
            System.Media.SoundPlayer myPlayer = new System.Media.SoundPlayer();
            myPlayer.SoundLocation = sound_filename;
            myPlayer.Play();
            
            //Gnome.Sound.Init("localhost");
            //Gnome.Sound.Play(sound_filename);
>>>>>>> .r784
        }
        
        #endregion
    
        #region "displaying images"
    
        // images used for display
        public Gtk.Image[] display_image;
        public Gtk.Window window;
        
        public Gtk.Image calibration_image;
        public Gtk.Window calibration_window;
        private byte[] buffer = null;
        
        // previous error when calibrating
        // this is used to trigger a beep
        private double prev_minimum_rms_error;

        /// <summary>
        /// refreshes the GUI images
        /// </summary>
        /// <param name="window">Gtk window to be refreshed</param>
        private void UpdateGUI(Gtk.Window window, Gtk.Window calibration_window)
        {            
            window.GdkWindow.ProcessUpdates(true);
            if (calibration_window != null) 
                calibration_window.GdkWindow.ProcessUpdates(true);			
        }
        
        private Bitmap OverlayImage(Bitmap bmp, Bitmap overlay, int overlay_width)
        {
            Bitmap result = (Bitmap)bmp.Clone();
            byte[] image_data = new byte[result.Width * result.Height * 3];
            BitmapArrayConversions.updatebitmap(result, image_data);

            byte[] overlay_data = new byte[overlay.Width * overlay.Height * 3];
            BitmapArrayConversions.updatebitmap(overlay, overlay_data);
            
            int overlay_height = overlay.Height * overlay_width / overlay.Width;
            
            for (int y = 0; y < overlay_height; y++)
            {
                int yy = y * overlay.Height / overlay_height;
                for (int x = 0; x < overlay_width; x++)
                {
                    int xx = x * overlay.Width / overlay_width;
                    int n1 = ((yy * overlay.Width) + xx) * 3;
                    int n2 = ((y * result.Width) + x) * 3;
                    for (int col = 0; col < 3; col++)
                        image_data[n2 + col] = overlay_data[n1 + col];
                }
            }
            
            BitmapArrayConversions.updatebitmap_unsafe(image_data, result);
            return(result);
        }

        private void DisplayImage(Gtk.Image img, Bitmap default_image, bool is_left)
        {
            Bitmap disp_image = null;
            
            switch(display_type)
            {
                case DISPLAY_RAW: { disp_image = default_image; break; }
                case DISPLAY_CALIBRATION_DOTS: { disp_image = edges; break; }
                case DISPLAY_CALIBRATION_GRID: { disp_image = linked_dots; break; }
                case DISPLAY_CALIBRATION_DIFF: { disp_image = grid_diff; break; }
                case DISPLAY_RECTIFIED: { if (is_left) disp_image = rectified[0]; else disp_image = rectified[1]; break; }
            }
            
            if ((calibration_pattern == null) || (disp_image == null))
                disp_image = default_image;
                
            if (calibration_pattern == null)
            {
                if (is_left)
                {
                    if (calibration_map[0] != null)
                    {
                        if (stereo_features == null)
                            disp_image = rectified[0];
                        else
                            disp_image = stereo_features;
                    }
                }
                else
                {
                    if (calibration_map[1] != null)
                        disp_image = rectified[1];
                }                
            }
        
			if ((img != null) && (disp_image != null))
                GtkBitmap.setBitmap(disp_image, img);
        }
    
        /// <summary>
        /// shows both left and right camera images within the GUI
        /// </summary>
        /// <param name="left_image">left image bitmap</param>
        /// <param name="right_image">right image bitmap</param>
        protected override void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
            if (display_image[0] != null)
            {
                if ((show_left_image) &&
                    (calibration_pattern != null))
                {
                    try
                    {
                        GtkBitmap.setBitmap(calibration_pattern, display_image[0], ref buffer);
                    }
                    catch
                    {
                    }
                    
                    if (calibration_survey != null)
                    {
                        CalibrationSurvey survey = calibration_survey[1];
                        if (survey != null)
                        {
                            if ((survey.minimum_rms_error < 3) &&
                                ((prev_minimum_rms_error >= 3) || (prev_minimum_rms_error == 0)))
                                PlaySound("beep.wav");
                            prev_minimum_rms_error = survey.minimum_rms_error;
                        }
                    }
                                       
                }
                else
                {
                    DisplayImage(display_image[0], left_image, true);
                }
            }
            
            if (display_image[1] != null)
            {
                if ((!show_left_image) &&
                    (calibration_pattern != null))
                {
                    try
                    {
                        GtkBitmap.setBitmap(calibration_pattern, display_image[1], ref buffer);
                    }
                    catch
                    {
                    }

                    if (calibration_survey != null)
                    {
                        CalibrationSurvey survey = calibration_survey[0];
                        if (survey != null)
                        {
                            if ((survey.minimum_rms_error < 3) &&
                                ((prev_minimum_rms_error >= 3) || (prev_minimum_rms_error == 0)))
                                PlaySound("beep.wav");
                            prev_minimum_rms_error = survey.minimum_rms_error;
                        }
                    }
                }
                else
                {
                    DisplayImage(display_image[1], right_image, false);
                }

            }			
            
            if (window != null)
            {
                // Here we need to update the GUI after receiving the right camera image
                // Since we're running in a separate thread from the GUI we have to
                // call it in a special way
                RunOnMainThread.Run(this, "UpdateGUI", new object[] { window, calibration_window });
				
                Gdk.Threads.Enter();
                window.QueueDraw();
                Gdk.Threads.Leave();
            }
            
        }
        
        #endregion

    }
}