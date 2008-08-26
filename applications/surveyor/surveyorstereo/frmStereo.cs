using System;
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

        public frmStereo()
        {
            InitializeComponent();

            picLeftImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRightImage.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Init();
        }

        public void Init()
        {
            stereo_camera = new SurveyorVisionStereoWin(stereo_camera_IP, 10001, 10002);
            stereo_camera.window = this;
            stereo_camera.display_image[0] = picLeftImage;
            stereo_camera.display_image[1] = picRightImage;
            stereo_camera.Load(calibration_filename);
            stereo_camera.Run();

            if (stereo_camera.stereo_algorithm_type == StereoVision.SIMPLE)
            {
                denseToolStripMenuItem.Checked = false;
                simpleToolStripMenuItem.Checked = true;
            }
            else
            {
                denseToolStripMenuItem.Checked = true;
                simpleToolStripMenuItem.Checked = false;
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

        private void recordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            recordToolStripMenuItem.Checked = !recordToolStripMenuItem.Checked;
            stereo_camera.Record = !stereo_camera.Record;
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
            stereo_camera.show_left_image = false;
            Calibrate(calibrateLeftCameraToolStripMenuItem.Checked);
        }

        private void calibrateRightCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateRightCameraToolStripMenuItem.Checked = !calibrateRightCameraToolStripMenuItem.Checked;
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

    }
}