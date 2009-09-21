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
using System.ComponentModel;
using System.Windows.Forms;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// stereo vision object which displays images using Winforms
    /// </summary>
    public class SurveyorVisionStereoWin : SurveyorVisionStereo
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
        public SurveyorVisionStereoWin(string host,
                                       int port_number_left,
                                       int port_number_right,
                                       int broadcast_port,
                                       float fps)
            : base(host, port_number_left, port_number_right, broadcast_port, fps)
        {
            display_image = new PictureBox[2];
        }

        #endregion

        #region "displaying images"

        // images used for display
        public PictureBox[] display_image;
        public PictureBox calibration_image;
        public Form window;
        //private byte[] buffer = null;
        private double prev_minimum_rms_error;

/*
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
            return (result);
        }
*/

        private void DisplayImage(Bitmap img, Bitmap default_image, bool is_left)
        {
            Bitmap disp_image = null;

            switch (display_type)
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

            byte[] image_data = new byte[disp_image.Width * disp_image.Height * 3];
            BitmapArrayConversions.updatebitmap(disp_image, image_data);
            BitmapArrayConversions.updatebitmap_unsafe(image_data, img);
        }

        protected override void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
            if (display_image[0] != null)
            {
                if (display_image[0].Image == null)
                    display_image[0].Image = (Bitmap)left_image.Clone();

                if ((show_left_image) &&
                    (calibration_pattern != null))
                {
                    display_image[0].Image = calibration_pattern;

                    if (calibration_survey != null)
                    {
                        CalibrationSurvey survey = calibration_survey[1];
                        if (survey != null)
                        {
                            if ((survey.minimum_rms_error < 3) &&
                                ((prev_minimum_rms_error >= 3) || (prev_minimum_rms_error == 0)))
                                BeepSound.Play("beep.wav");
                            prev_minimum_rms_error = survey.minimum_rms_error;
                        }
                    }
                }
                else
                {
                    DisplayImage((Bitmap)display_image[0].Image, left_image, true);
                }

                if (window != null)
                {
                    try
                    {
                        window.Invoke((MethodInvoker)delegate
                        {
                            bool success = false;
                            DateTime start_time = DateTime.Now;
                            int time_elapsed_mS = 0;
                            while ((!success) && (time_elapsed_mS < 500))
                            {
                                try
                                {
                                    display_image[0].Refresh();
                                    success = true;
                                }
                                catch
                                {
                                    System.Threading.Thread.Sleep(5);
                                    TimeSpan diff = DateTime.Now.Subtract(start_time);
                                    time_elapsed_mS = (int)diff.TotalMilliseconds;
                                }
                            }
                        });
                    }
                    catch
                    {
                    }
                }
            }

            if (display_image[1] != null)
            {
                if (display_image[1].Image == null)
                    display_image[1].Image = (Bitmap)right_image.Clone();

                if ((!show_left_image) &&
                    (calibration_pattern != null))
                {
                    display_image[1].Image = calibration_pattern;

                    if (calibration_survey != null)
                    {
                        CalibrationSurvey survey = calibration_survey[0];
                        if (survey != null)
                        {
                            if ((survey.minimum_rms_error < 3) &&
                                ((prev_minimum_rms_error >= 3) || (prev_minimum_rms_error == 0)))
                                BeepSound.Play("beep.wav");
                            prev_minimum_rms_error = survey.minimum_rms_error;
                        }
                    }
                }
                else
                {
                    DisplayImage((Bitmap)display_image[1].Image, right_image, false);
                }

                if (window != null)
                {
                    try
                    {
                        window.Invoke((MethodInvoker)delegate
                        {
                            bool success = false;
                            DateTime start_time = DateTime.Now;
                            int time_elapsed_mS = 0;
                            while ((!success) && (time_elapsed_mS < 500))
                            {
                                try
                                {
                                    display_image[1].Refresh();
                                    success = true;
                                }
                                catch
                                {
                                    System.Threading.Thread.Sleep(5);
                                    TimeSpan diff = DateTime.Now.Subtract(start_time);
                                    time_elapsed_mS = (int)diff.TotalMilliseconds;
                                }
                            }
                        });
                    }
                    catch
                    {
                    }
                }
            }
            
        }


        #endregion


    }
}
