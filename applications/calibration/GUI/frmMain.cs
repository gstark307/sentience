/*
    Demonstration of detection of line features using the FAST corner detector
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
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using DirectX.Capture;
using sentience.calibration;

namespace WindowsApplication1
{

    public partial class frmMain : common
    {
        calibration cam = new calibration();

        Random rnd = new Random();

        globals global_variables;

        bool left_camera_running = false;

        int[] captureState = new int[2];
        Bitmap leftbmp;

        //list of interactive objects
        RotateFlipType transformLeft = 0;

        //output image
        Byte[] disp_bmp_data=null;
        bool outputInitialised = false;
        int output_width, output_height;

        const int DISPLAY_EDGES = 0;
        const int DISPLAY_CORNERS = 1;
        const int DISPLAY_LINES = 2;
        public int display_type = DISPLAY_CORNERS;

#region "Camera stuff"

        public void updateVisionLeft(Image input_img, bool leftImage)
        {
            Byte[] ary;

            try
            {
                if (captureState[0] == 1)
                {
                    //if (global_variables.left_bmp == null)
                        //global_variables.left_bmp = new Byte[input_img.Width * input_img.Height * 3];

                    if (disp_bmp_data == null)
                    {
                        disp_bmp_data = new Byte[input_img.Width * input_img.Height * 3];
                    }

                    ary = disp_bmp_data;
                    updatebitmap((Bitmap)(input_img.Clone()), ary);

                    captureState[0] = 2;
                }
                left_camera_running = true;
            }
            catch
            {
            }
        }


        private void transformImage(PictureBox Frame, RotateFlipType transformType)
        {
            if (transformType > 0)
                Frame.Image.RotateFlip(transformType);
        }


        #region "Capture Event functions"


        public void RefreshLeftImage(PictureBox Frame) //object sender, FrameGrabArgs f)
        {
            try
            {
                //picLeftImage.Image = null;
                //picLeftImage.Image = Frame.Image;

                //transform the image appropriately
                transformImage(Frame, transformLeft);

                global_variables.CaptureInformationLeft.CounterFrames ++;
                updateVisionLeft(Frame.Image, true);

                //picLeftImage.Refresh();

            }
            catch //(Exception ex)
            {

            }
        }


        #endregion


        private void initCameraLeft2()
        {
            //leftbmp = New Bitmap(CaptureInformationLeft.ConfWindow.Width, CaptureInformationLeft.ConfWindow.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            leftbmp = new Bitmap(320, 240, PixelFormat.Format24bppRgb);
            picLeftImage.Image = leftbmp;
            picLeftImage.Visible = true;

            global_variables.CaptureInformationLeft.CaptureInfo.PreviewWindow = this.picLeftImage;

            global_variables.ConfParamCamLeft();
            global_variables.PrepareCamLeft(global_variables.CaptureInformationLeft.PathVideo);

            //Define RefreshImage as event handler of FrameCaptureComplete
            global_variables.CaptureInformationLeft.CaptureInfo.FrameCaptureComplete += new Capture.FrameCapHandler(this.RefreshLeftImage);
            //AddHandler(global_variables.CaptureInformationLeft.CaptureInfo.FrameCaptureComplete, *RefreshLeftImage);

            global_variables.CaptureInformationLeft.CaptureInfo.CaptureFrame();

            global_variables.CaptureInformationLeft.Counter = 1;
            global_variables.CaptureInformationLeft.CounterFrames = 1;

            this.Show();
        }


        public void initCameraLeft()
        {
            //Call to AddCam to select an available camera
            AddCam AddCamera = new AddCam(global_variables);

            AddCamera.LeftImage = true;
            AddCamera.ShowDialog(this);
            if (global_variables.camera_initialised)
            {
                startSentienceToolStripMenuItem.Enabled = false;
                initCameraLeft2();
            }
        }


#endregion

        //is the given bitmap blank?
        private bool isBlankFrame(Byte[] data, int width, int height)
        {
            bool isBlank = true;
            int i=0;

            while ((isBlank) && (i < width * height * 3))
            {
                if (data[i] > 0) isBlank = false;
                i += (3 * (width / 20));
            }

            return (isBlank);
        }

        public frmMain()
        {
            global_variables = new globals();

            InitializeComponent();
        }



        private void frmMain_Load(object sender, EventArgs e)
        {
            captureState[0] = 0;
            captureState[1] = 0;
            this.WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Show an image within the given picture box control
        /// </summary>
        /// <param name="pic">picture box control in which to draw the image</param>
        /// <param name="imageIndex">An index number corresponding to the type of image to be displayed</param>
        public void showImage(PictureBox pic, Byte[] imagedata, int image_width, int image_height)
        {
            SolidBrush brush;
            Rectangle rect;
            Byte r, g, b;
            int x, y;
            long n;
            Graphics gr;

            gr = Graphics.FromImage(pic.Image);
            rect = new Rectangle();            

            n = 0;
            for (y = 0;y < image_height; y++)
            {
                for (x = 0; x < image_width; x++)
                {
                    b = imagedata[n];
                    n++;
                    g = imagedata[n];
                    n++;
                    r = imagedata[n];
                    n++;

                    brush = new SolidBrush(Color.FromArgb(r, g, b));
                    rect.X = (pic.Image.Width * x) / image_width;
                    rect.Y = (pic.Image.Height * y) / image_height;
                    rect.Width = pic.Image.Width / image_width * 2;
                    rect.Height = pic.Image.Height / image_height * 2;
                    gr.FillRectangle(brush, rect);
                }
            }
            pic.Refresh();
        }


        private void captureCameraImages()
        {
            //capture images from the two cameras
            //note the use of a state indicator to ensure that captures are only initiated at the appropriate time
            if (left_camera_running)
            {
                try
                {
                    if ((captureState[0] < 2) && (!global_variables.CaptureInformationLeft.CaptureInfo.Capturing))
                    {
                        captureState[0] = 1;
                        global_variables.CaptureInformationLeft.CaptureInfo.CaptureFrame();
                    }
                }
                catch
                {
                }
            }
        }

        private void timUpdate_Tick(object sender, EventArgs e)
        {
            if (left_camera_running)
            {
                //get images from the two cameras
                captureCameraImages();
                
                //update from camera
                if (captureState[0] == 2)
                {
                    captureState[0] = 0;

                    //show disparity
                    if (!outputInitialised)
                    {
                        picOutput1.Image = new Bitmap(picLeftImage.Image.Width, picLeftImage.Image.Height, PixelFormat.Format24bppRgb);
                        output_width = picLeftImage.Image.Width;
                        output_height = picLeftImage.Image.Height;
                        outputInitialised = true;
                    }

                    cam.Update(disp_bmp_data, output_width, output_height);

                    switch (display_type)
                    {
                        case DISPLAY_EDGES:
                            {
                                updatebitmap(cam.edges_image, (Bitmap)picOutput1.Image);
                                break;
                            }
                        case DISPLAY_CORNERS:
                            {
                                updatebitmap(cam.corners_image, (Bitmap)picOutput1.Image);
                                break;
                            }
                        case DISPLAY_LINES:
                            {
                                updatebitmap(cam.lines_image, (Bitmap)picOutput1.Image);
                                break;
                            }
                    }

                    picOutput1.Refresh();
                }

            }

        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Redraw()
        {
            picOutput1.Left = grpParameters.Left;
            picOutput1.Top = menuStrip1.Height + grpParameters.Height + 10;
            picOutput1.Width = this.Width - picOutput1.Left;
            picOutput1.Height = this.Height - picOutput1.Top;
            grpParameters.Width = picOutput1.Width;
            grpParameters.Visible = true;

            cmbDisplayType.SelectedIndex = display_type;
            txtPatternSpacing.Text = Convert.ToString(cam.calibration_pattern_spacing_mm);
            txtFOV.Text = Convert.ToString(cam.camera_FOV_degrees);
            txtSpacingFactor.Text = Convert.ToString(cam.separation_factor);
            txtCameraHeight.Text = Convert.ToString(cam.camera_height_mm);
            txtDistToCentre.Text = Convert.ToString(cam.camera_dist_to_pattern_centre_mm);
        }

        private void startCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Redraw();
            initCameraLeft();            
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            Redraw();
        }

        private void cmbDisplayType_SelectedIndexChanged(object sender, EventArgs e)
        {
            display_type = cmbDisplayType.SelectedIndex;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void txtPatternSpacing_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtPatternSpacing.Text == "") txtPatternSpacing.Text = "50";
                cam.calibration_pattern_spacing_mm = Convert.ToSingle(txtPatternSpacing.Text);
            }
        }

        private void txtPatternSpacing_Leave(object sender, EventArgs e)
        {
            if (txtPatternSpacing.Text == "") txtPatternSpacing.Text = "50";
            cam.calibration_pattern_spacing_mm = Convert.ToSingle(txtPatternSpacing.Text);
        }

        private void txtFOV_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtFOV.Text == "") txtFOV.Text = "40";
                cam.camera_FOV_degrees = Convert.ToSingle(txtFOV.Text);
            }
        }

        private void txtFOV_Leave(object sender, EventArgs e)
        {
            if (txtFOV.Text == "") txtFOV.Text = "40";
            cam.camera_FOV_degrees = Convert.ToSingle(txtFOV.Text);
        }

        private void txtSpacingFactor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtSpacingFactor.Text == "") txtSpacingFactor.Text = "15";
                cam.separation_factor = Convert.ToInt32(txtSpacingFactor.Text);
            }
        }

        private void txtSpacingFactor_Leave(object sender, EventArgs e)
        {
            if (txtSpacingFactor.Text == "") txtSpacingFactor.Text = "15";
            cam.separation_factor = Convert.ToInt32(txtSpacingFactor.Text);
        }

        private void txtCameraHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtCameraHeight.Text == "") txtCameraHeight.Text = "500";
                cam.camera_height_mm = Convert.ToInt32(txtCameraHeight.Text);
            }
        }

        private void txtCameraHeight_Leave(object sender, EventArgs e)
        {
            if (txtCameraHeight.Text == "") txtCameraHeight.Text = "500";
            cam.camera_height_mm = Convert.ToInt32(txtCameraHeight.Text);
        }

        private void txtDistToCentre_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtDistToCentre.Text == "") txtDistToCentre.Text = "500";
                cam.camera_dist_to_pattern_centre_mm = Convert.ToInt32(txtDistToCentre.Text);
            }
        }

    }
}