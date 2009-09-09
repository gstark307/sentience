/*
    Surveyor stereo camera
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using surveyor.vision;
using sluggish.utilities;

namespace surveyorstereo
{
    public partial class frmStereo : Form
    {
        int image_width = 320;
        int image_height = 240;
        string stereo_camera_IP = "169.254.0.10";
        string calibration_filename = "calibration.xml";
        SurveyorVisionStereoWin stereo_camera;
        int left_port = 10001;
        int right_port = 10002;
        int broadcast_port = 10010;
        int fps = 5;
        string log_path = Environment.CurrentDirectory;
        string teleoperation_log = "teleop.dat";
        string temporary_files_path = "";
        string recorded_images_path = "";
        string zip_utility = "zip";
        string path_identifier = "log";
        string replay_path_identifier = "log";

        string manual_camera_alignment_program = "calibtweaks.exe";
        bool reverse_colours = true;
        bool motors_active;
        int motors_tries = 5;
        bool starting = true;

        public frmStereo()
        {
            InitializeComponent();

            picLeftImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRightImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Init();
        }

        public void Init()
        {
            stereo_camera = new SurveyorVisionStereoWin(stereo_camera_IP, left_port, right_port, broadcast_port, fps);
            stereo_camera.temporary_files_path = temporary_files_path;
            stereo_camera.recorded_images_path = recorded_images_path;
            stereo_camera.window = this;
            stereo_camera.display_image[0] = picLeftImage;
            stereo_camera.display_image[1] = picRightImage;
            stereo_camera.Load(calibration_filename);
            stereo_camera.Run();

            if (stereo_camera.stereo_algorithm_type == StereoVision.EDGES)
            {
                denseToolStripMenuItem.Checked = false;
                simpleToolStripMenuItem.Checked = true;
            }
            else
            {
                denseToolStripMenuItem.Checked = true;
                simpleToolStripMenuItem.Checked = false;
            }

            txtLogging.Text = path_identifier;
            txtReplay.Text = replay_path_identifier;

            SendCommand("M");

            motors_active = true;
            starting = false;
        }

        /// <summary>
        /// Toggles logging of images on or off  
        /// </summary>
        /// <param name="enable"></param>
        private void ToggleLogging(bool enable)
        {
            Console.WriteLine("");
            Console.WriteLine("");

            if (enable)
                Console.WriteLine("Logging Enabled");
            else
                Console.WriteLine("Logging Disabled");

            Console.WriteLine("");
            Console.WriteLine("");

            stereo_camera.recorded_images_path = log_path;

            // reset the frame number
            if (!stereo_camera.replaying_actions)
            {
                if (enable == true)
                {
                    if ((txtLogging.Text != "") &&
                        (txtLogging.Text != null))
                    {
                        path_identifier = txtLogging.Text;
                    }

                    BaseVisionStereo.LogEvent(DateTime.Now, "BEGIN", teleoperation_log);
                    // clear any previous recorded data
                    stereo_camera.RecordFrameNumber = 0;
                }

                stereo_camera.Record = enable;
            }

            if (enable == false)
            {
                BaseVisionStereo.LogEvent(DateTime.Now, "END", teleoperation_log);

                // compress the recorded images  
                BaseVisionStereo.CompressRecordedData(
                    zip_utility,
                    path_identifier,
                    log_path);
            }
        }

        private void frmStereo_FormClosing(object sender, FormClosingEventArgs e)
        {
            stereo_camera.Stop();
        }

        /// <summary>
        /// resize controls on the form
        /// </summary>
        private void ResizeControls()
        {
            if ((picLeftImage.Image != null) &&
                (picRightImage.Image != null))
            {
                picLeftImage.Width = (this.Width / 2) - (picLeftImage.Left * 2);
                picRightImage.Left = (picLeftImage.Left * 2) + picLeftImage.Width;
                picRightImage.Width = picLeftImage.Width;

                picLeftImage.Height = this.Height - picLeftImage.Top - 30;
                picRightImage.Height = picLeftImage.Height;
            }
        }

        private void frmStereo_ResizeEnd(object sender, EventArgs e)
        {
            //ResizeControls();
        }

        private void frmStereo_SizeChanged(object sender, EventArgs e)
        {
            //ResizeControls();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private Bitmap ShowDotPattern()
        {
            stereo_camera.calibration_pattern = SurveyorCalibration.CreateDotPattern(image_width, image_height, SurveyorCalibration.dots_across, SurveyorCalibration.dot_radius_percent);
            return ((Bitmap)stereo_camera.calibration_pattern);
        }

        private void Calibrate(bool Active)
        {
            PictureBox dest_img = null;
            int window_index = 0;
            if (stereo_camera.show_left_image)
            {
                dest_img = picLeftImage;
                window_index = 0;
            }
            else
            {
                dest_img = picRightImage;
                window_index = 1;
            }

            if (Active)
            {
                Bitmap bmp = ShowDotPattern();
                dest_img.Image = bmp;
                stereo_camera.display_type = SurveyorVisionStereo.DISPLAY_RECTIFIED;
                stereo_camera.ResetCalibration(1 - window_index);
                stereo_camera.display_image[window_index] = dest_img;
            }
            else
            {
                stereo_camera.calibration_pattern = null;
                stereo_camera.display_image[0] = picLeftImage;
                stereo_camera.display_image[1] = picRightImage;
                stereo_camera.display_type = SurveyorVisionStereo.DISPLAY_RAW;
            }
        }

        private void calibrateLeftCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateLeftCameraToolStripMenuItem.Checked = !calibrateLeftCameraToolStripMenuItem.Checked;
            if ((calibrateLeftCameraToolStripMenuItem.Checked) &&
                (calibrateRightCameraToolStripMenuItem.Checked))
                calibrateRightCameraToolStripMenuItem.Checked = false;
            stereo_camera.show_left_image = false;
            Calibrate(calibrateLeftCameraToolStripMenuItem.Checked);
        }

        private void calibrateRightCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateRightCameraToolStripMenuItem.Checked = !calibrateRightCameraToolStripMenuItem.Checked;
            if ((calibrateRightCameraToolStripMenuItem.Checked) &&
                (calibrateLeftCameraToolStripMenuItem.Checked))
                calibrateLeftCameraToolStripMenuItem.Checked = false;
            stereo_camera.show_left_image = true;
            Calibrate(calibrateRightCameraToolStripMenuItem.Checked);
        }

        private void ShowMessage(string message_str)
        {
            Console.WriteLine(message_str);
            MessageBox.Show(message_str);
        }

        private void calibrateCameraAlignmentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // stop the cameras in order avoid thread collisions
            stereo_camera.Stop();

            for (int i = 0; i < 100; i++)
                System.Threading.Thread.Sleep(5);

            if (stereo_camera.CalibrateCameraAlignment())
            {
                stereo_camera.Save(calibration_filename);

                ShowMessage("Calibration complete");
            }
            else
            {
                ShowMessage("Please individually calibrate left and right cameras before the calibrating the focus");
            }

            // restart the cameras
            stereo_camera.Run();
        }

        private void simpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo_camera.stereo_algorithm_type = StereoVision.SIMPLE;
            denseToolStripMenuItem.Checked = false;
            simpleToolStripMenuItem.Checked = true;
        }

        private void denseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo_camera.stereo_algorithm_type = StereoVision.DENSE;
            denseToolStripMenuItem.Checked = true;
            simpleToolStripMenuItem.Checked = false;
        }

        private void saveCalibrationPatternToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap bmp = SurveyorCalibration.CreateDotPattern(1000, image_height * 1000 / image_width, SurveyorCalibration.dots_across, SurveyorCalibration.dot_radius_percent);
            SaveFileDialog save_calibration_pattern = new SaveFileDialog();

            save_calibration_pattern.Title = "Save calibration pattern image";
            save_calibration_pattern.Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*";
            save_calibration_pattern.FilterIndex = 1;
            save_calibration_pattern.RestoreDirectory = true;

            if (save_calibration_pattern.ShowDialog() == DialogResult.OK)
            {
                bmp.Save(save_calibration_pattern.FileName);
            }
        }

        private void saveCalibrationFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(calibration_filename))
            {
                SaveFileDialog save_calibration_file = new SaveFileDialog();

                save_calibration_file.Title = "Save file containing calibration settings";
                save_calibration_file.Filter = "Xml files (*.xml)|*.xml";
                save_calibration_file.FilterIndex = 1;
                save_calibration_file.RestoreDirectory = true;

                if (save_calibration_file.ShowDialog() == DialogResult.OK)
                {
                    if (calibration_filename != save_calibration_file.FileName)
                        File.Copy(calibration_filename, save_calibration_file.FileName);
                }
            }
        }

        private void SendCommand(string command_str)
        {
            if (stereo_camera.Record) BaseVisionStereo.LogEvent(DateTime.Now, command_str, teleoperation_log);
            stereo_camera.SendCommand(0, command_str);
        }

        private void cmdRight_Click(object sender, EventArgs e)
        {
            SendCommand("6");
        }

        private void cmdForwardLeft_Click(object sender, EventArgs e)
        {
            SendCommand("7");
        }

        private void cmdForward_Click(object sender, EventArgs e)
        {
            SendCommand("8");
        }

        private void cmdForwardRight_Click(object sender, EventArgs e)
        {
            SendCommand("9");
        }

        private void cmdLeft_Click(object sender, EventArgs e)
        {
            SendCommand("4");
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            SendCommand("5");
        }

        private void cmdBackLeft_Click(object sender, EventArgs e)
        {
            SendCommand("1");
        }

        private void cmdBack_Click(object sender, EventArgs e)
        {
            SendCommand("2");
        }

        private void cmdBackRight_Click(object sender, EventArgs e)
        {
            SendCommand("3");
        }

        private void cmdSpinLeft_Click(object sender, EventArgs e)
        {
            SendCommand("0");
        }

        private void cmdSpinRight_Click(object sender, EventArgs e)
        {
            SendCommand(".");
        }

        private void cmdFast_Click(object sender, EventArgs e)
        {
            SendCommand("+");
        }

        private void cmdSlow_Click(object sender, EventArgs e)
        {
            SendCommand("-");
        }

        private void cmdAvoid_Click(object sender, EventArgs e)
        {
            SendCommand("F");
        }

        private void cmdCrash_Click(object sender, EventArgs e)
        {
            SendCommand("f");
        }

        private void cmdLaserOn_Click(object sender, EventArgs e)
        {
            SendCommand("l");
        }

        private void cmdLaserOff_Click(object sender, EventArgs e)
        {
            SendCommand("L");
        }

        private void cmdReplay_Click(object sender, EventArgs e)
        {
            replay_path_identifier = txtReplay.Text;
            if ((replay_path_identifier != null) &&
                (replay_path_identifier != "") &&
                (!stereo_camera.replaying_actions) &&
                (!stereo_camera.Record))
            {
                lblReplayState.Text = "Playing";
                stereo_camera.RunReplay(teleoperation_log, zip_utility, log_path, replay_path_identifier);
            }
        }

        private void cmdReplayStop_Click(object sender, EventArgs e)
        {
            stereo_camera.StopReplay();
            lblReplayState.Text = "";
        }

        private void enableLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableLoggingToolStripMenuItem.Checked = !enableLoggingToolStripMenuItem.Checked;
            ToggleLogging(enableLoggingToolStripMenuItem.Checked);
        }

        private void enableEmbeddedStereoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableEmbeddedStereoToolStripMenuItem.Checked = !enableEmbeddedStereoToolStripMenuItem.Checked;
            if (stereo_camera != null)
            {
                if (enableEmbeddedStereoToolStripMenuItem.Checked)
                    stereo_camera.EnableEmbeddedStereo();
                else
                    stereo_camera.DisableEmbeddedStereo();
            }
        }

    }
}
