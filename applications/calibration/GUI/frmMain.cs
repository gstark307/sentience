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
using System.Xml;
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
using sentience.core;

namespace WindowsApplication1
{

    public partial class frmMain : common
    {
        // this file stores the general setup parameters
        const String calibration_setup_filename = "calibration_setup.xml";

        int no_of_cameras = 1;
        calibrationStereo cam = new calibrationStereo();

        Random rnd = new Random();

        globals global_variables;

        bool left_camera_running = false;
        bool right_camera_running = false;

        int[] captureState = new int[2];
        Bitmap leftbmp, rightbmp;

        //list of interactive objects
        RotateFlipType transformLeft = 0;
        RotateFlipType transformRight = 0;

        //output image
        Byte[] bmp_data_left = null;
        Byte[] bmp_data_right = null;
        bool outputInitialised = false;
        int output_width, output_height;

        const int DISPLAY_EDGES = 0;
        const int DISPLAY_CORNERS = 1;
        const int DISPLAY_LINES = 2;
        const int DISPLAY_CENTREALIGN = 3;
        const int DISPLAY_CURVE = 4;
        const int DISPLAY_RECTIFIED = 5;
        public int display_type = DISPLAY_CENTREALIGN;

        #region "Camera stuff"

        public void updateVision(Image input_img, bool leftImage)
        {
            int cameraIndex = 0;
            Byte[] disp_bmp_data = bmp_data_left;
            if (!leftImage)
            {
                cameraIndex = 1;
                disp_bmp_data = bmp_data_right;
            }

            Byte[] ary;

            try
            {
                if (captureState[cameraIndex] == 1)
                {
                    if (disp_bmp_data == null)
                    {
                        if (leftImage)
                        {
                            bmp_data_left = new Byte[input_img.Width * input_img.Height * 3];
                            disp_bmp_data = bmp_data_left;
                        }
                        else
                        {
                            bmp_data_right = new Byte[input_img.Width * input_img.Height * 3];
                            disp_bmp_data = bmp_data_right;
                        }
                    }

                    ary = disp_bmp_data;
                    updatebitmap((Bitmap)(input_img.Clone()), ary);

                    captureState[cameraIndex] = 2;
                }

                if (leftImage)
                    left_camera_running = true;
                else
                    right_camera_running = true;
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


        public void RefreshLeftImage(PictureBox Frame)
        {
            try
            {
                transformImage(Frame, transformLeft);

                global_variables.CaptureInformationLeft.CounterFrames ++;
                updateVision(Frame.Image, true);
            }
            catch 
            {

            }
        }

        public void RefreshRightImage(PictureBox Frame)
        {
            try
            {
                transformImage(Frame, transformRight);

                global_variables.CaptureInformationRight.CounterFrames++;
                updateVision(Frame.Image, false);
            }
            catch 
            {

            }
        }

        #endregion


        private void initCameraLeft2()
        {
            //leftbmp = New Bitmap(CaptureInformationLeft.ConfWindow.Width, CaptureInformationLeft.ConfWindow.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            leftbmp = new Bitmap(320, 240, PixelFormat.Format24bppRgb);
            picLeftImage.Image = leftbmp;
            picLeftImage.Visible = false;

            global_variables.CaptureInformationLeft.CaptureInfo.PreviewWindow = this.picLeftImage;

            global_variables.ConfParamCamLeft();
            global_variables.PrepareCamLeft(global_variables.CaptureInformationLeft.PathVideo);

            //Define RefreshImage as event handler of FrameCaptureComplete
            global_variables.CaptureInformationLeft.CaptureInfo.FrameCaptureComplete += new Capture.FrameCapHandler(this.RefreshLeftImage);

            global_variables.CaptureInformationLeft.CaptureInfo.CaptureFrame();

            global_variables.CaptureInformationLeft.Counter = 1;
            global_variables.CaptureInformationLeft.CounterFrames = 1;

            this.Show();
        }

        private void initCameraRight2()
        {
            //rightbmp = New Bitmap(CaptureInformationLeft.ConfWindow.Width, CaptureInformationLeft.ConfWindow.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            rightbmp = new Bitmap(320, 240, PixelFormat.Format24bppRgb);
            picRightImage.Image = rightbmp;
            picRightImage.Visible = false;

            global_variables.CaptureInformationRight.CaptureInfo.PreviewWindow = this.picRightImage;

            global_variables.ConfParamCamRight();
            global_variables.PrepareCamRight(global_variables.CaptureInformationRight.PathVideo);

            //Define RefreshImage as event handler of FrameCaptureComplete
            global_variables.CaptureInformationRight.CaptureInfo.FrameCaptureComplete += new Capture.FrameCapHandler(this.RefreshRightImage);

            global_variables.CaptureInformationRight.CaptureInfo.CaptureFrame();

            global_variables.CaptureInformationRight.Counter = 1;
            global_variables.CaptureInformationRight.CounterFrames = 1;

            this.Show();
        }

        public void initCamera()
        {
            //Call to AddCam to select an available camera
            AddCam AddCamera = new AddCam(global_variables);

            AddCamera.LeftImage = true;
            AddCamera.ShowDialog(this);
        }


        #endregion

        #region "saving and loading the calibration setup"

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeCalibration = doc.CreateElement("Sentience");
            doc.AppendChild(nodeCalibration);

            util.AddComment(doc, nodeCalibration, "Calibration apparatus setup parameters");

            XmlElement nodeCalibSetup = doc.CreateElement("CalibrationSetup");
            nodeCalibration.AppendChild(nodeCalibSetup);

            util.AddComment(doc, nodeCalibSetup, "Horizontal field of view of the camera in degrees");
            util.AddTextElement(doc, nodeCalibSetup, "FieldOfViewDegrees", txtFOV.Text);
            util.AddComment(doc, nodeCalibSetup, "Position of the centre spot relative to the centre of the calibration pattern");
            util.AddComment(doc, nodeCalibSetup, "0 - North West");
            util.AddComment(doc, nodeCalibSetup, "1 - North East");
            util.AddComment(doc, nodeCalibSetup, "2 - South East");
            util.AddComment(doc, nodeCalibSetup, "3 - South West");
            util.AddTextElement(doc, nodeCalibSetup, "CentreSpotPosition", Convert.ToString(cmbCentreSpotPosition.SelectedIndex));
            util.AddComment(doc, nodeCalibSetup, "Distance from the camera to the centre of the calibration pattern along the ground in mm");
            util.AddTextElement(doc, nodeCalibSetup, "DistToCentreMillimetres", txtDistToCentre.Text);
            util.AddComment(doc, nodeCalibSetup, "height of the camera above the ground in mm");
            util.AddTextElement(doc, nodeCalibSetup, "CameraHeightMillimetres", txtCameraHeight.Text);
            util.AddComment(doc, nodeCalibSetup, "Factor indicating the typical spacing of the calibration grid pattern as observed within the image");
            util.AddTextElement(doc, nodeCalibSetup, "PatternSpacingFactor", txtSpacingFactor.Text);
            util.AddComment(doc, nodeCalibSetup, "Calibration pattern spacing in mm");
            util.AddTextElement(doc, nodeCalibSetup, "PatternSpacingMillimetres", txtPatternSpacing.Text);

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void SaveCalibrationSetup(String filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadCalibrationSetup(String filename)
        {
            if (File.Exists(filename))
            {
                // use an XmlTextReader to open an XML document
                XmlTextReader xtr = new XmlTextReader(filename);
                xtr.WhitespaceHandling = WhitespaceHandling.None;

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                xd.Load(xtr);

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
                int cameraIndex = 0;
                LoadFromXml(xnodDE, 0, ref cameraIndex);

                // close the reader
                xtr.Close();

                // now reset with the loaded parameters
                ResetCalibration();
            }
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        private void LoadFromXml(XmlNode xnod, int level, ref int cameraIndex)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "FieldOfViewDegrees")
            {
                txtFOV.Text = xnod.InnerText;
            }

            if (xnod.Name == "DistToCentreMillimetres")
            {
                txtDistToCentre.Text = xnod.InnerText;
            }

            if (xnod.Name == "CameraHeightMillimetres")
            {
                txtCameraHeight.Text = xnod.InnerText;
            }

            if (xnod.Name == "PatternSpacingFactor")
            {
                txtSpacingFactor.Text = xnod.InnerText;
            }

            if (xnod.Name == "PatternSpacingMillimetres")
            {
                txtPatternSpacing.Text = xnod.InnerText;
            }

            if (xnod.Name == "CentreSpotPosition")
            {
                cmbCentreSpotPosition.SelectedIndex = Convert.ToInt32(xnod.InnerText);
            }

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1, ref cameraIndex);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion


        /// <summary>
        /// reset the calibration
        /// </summary>
        private void ResetCalibration()
        {
            cam = new calibrationStereo();
            cam.setCentreSpotPosition(cmbCentreSpotPosition.SelectedIndex);
            cam.leftcam.camera_dist_to_pattern_centre_mm = Convert.ToInt32(txtDistToCentre.Text);
            cam.rightcam.camera_dist_to_pattern_centre_mm = cam.leftcam.camera_dist_to_pattern_centre_mm;
            cam.leftcam.camera_height_mm = Convert.ToInt32(txtCameraHeight.Text);
            cam.rightcam.camera_height_mm = cam.leftcam.camera_height_mm;
            cam.leftcam.separation_factor = Convert.ToInt32(txtSpacingFactor.Text);
            cam.rightcam.separation_factor = cam.leftcam.separation_factor;
            cam.leftcam.camera_FOV_degrees = Convert.ToSingle(txtFOV.Text);
            cam.rightcam.camera_FOV_degrees = cam.leftcam.camera_FOV_degrees;
            cam.leftcam.calibration_pattern_spacing_mm = Convert.ToSingle(txtPatternSpacing.Text);
            cam.rightcam.calibration_pattern_spacing_mm = cam.leftcam.calibration_pattern_spacing_mm;
        }

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

            LoadCalibrationSetup(calibration_setup_filename);
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
            if (right_camera_running)
            {
                try
                {
                    if ((captureState[1] < 2) && (!global_variables.CaptureInformationRight.CaptureInfo.Capturing))
                    {
                        captureState[1] = 1;
                        global_variables.CaptureInformationRight.CaptureInfo.CaptureFrame();
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
                if (((no_of_cameras == 1) && (captureState[0] == 2)) ||
                    ((no_of_cameras > 1) && (captureState[1] == 2)))
                {
                    captureState[0] = 0;
                    captureState[1] = 0;

                    //show disparity
                    if (!outputInitialised)
                    {
                        picOutput1.Image = new Bitmap(picLeftImage.Image.Width, picLeftImage.Image.Height, PixelFormat.Format24bppRgb);
                        if (no_of_cameras > 1) picOutput2.Image = new Bitmap(picRightImage.Image.Width, picRightImage.Image.Height, PixelFormat.Format24bppRgb);
                        output_width = picLeftImage.Image.Width;
                        output_height = picLeftImage.Image.Height;
                        outputInitialised = true;
                    }

                    for (int i = 0; i < no_of_cameras; i++)
                    {
                        calibration calib = cam.leftcam;
                        PictureBox pic = picOutput1;
                        Byte[] disp_bmp_data = bmp_data_left;

                        if (i > 0)
                        {
                            calib = cam.rightcam;
                            pic = picOutput2;
                            disp_bmp_data = bmp_data_right;
                        }                        

                        calib.Update(disp_bmp_data, output_width, output_height);

                        switch (display_type)
                        {
                            case DISPLAY_EDGES:
                                {
                                    updatebitmap(calib.edges_image, (Bitmap)pic.Image);
                                    break;
                                }
                            case DISPLAY_CORNERS:
                                {
                                    updatebitmap(calib.corners_image, (Bitmap)pic.Image);
                                    break;
                                }
                            case DISPLAY_LINES:
                                {
                                    updatebitmap(calib.lines_image, (Bitmap)pic.Image);
                                    break;
                                }
                            case DISPLAY_CENTREALIGN:
                                {
                                    updatebitmap(calib.centrealign_image, (Bitmap)pic.Image);
                                    break;
                                }
                            case DISPLAY_CURVE:
                                {
                                    if (calib.curve_fit != null)
                                        updatebitmap(calib.curve_fit, (Bitmap)pic.Image);
                                    break;
                                }
                            case DISPLAY_RECTIFIED:
                                {
                                    if (calib.rectified_image != null)
                                        updatebitmap(calib.rectified_image, (Bitmap)pic.Image);
                                    break;
                                }
                        }

                        pic.Refresh();
                    }

                    
                }

            }

        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// redraw for either monocular or binocular camera
        /// </summary>
        private void Redraw()
        {
            if (no_of_cameras == 1)
            {
                picOutput2.Visible = false;
                picOutput1.Left = grpParameters.Left;
                picOutput1.Top = menuStrip1.Height + grpParameters.Height + 10;
                picOutput1.Width = this.Width - picOutput1.Left;
                picOutput1.Height = this.Height - picOutput1.Top - 40;                
            }
            else
            {
                picOutput2.Visible = true;
                picOutput1.Left = grpParameters.Left;
                picOutput1.Top = menuStrip1.Height + grpParameters.Height + 10;
                picOutput1.Width = ((this.Width - picOutput1.Left) / 2)-1;
                picOutput1.Height = this.Height - picOutput1.Top - 40;

                picOutput2.Left = picOutput1.Left + picOutput1.Width + 2;
                picOutput2.Top = picOutput1.Top;
                picOutput2.Width = picOutput1.Width;
                picOutput2.Height = picOutput1.Height;
            }

            grpParameters.Width = (picOutput1.Width*2) - picOutput1.Left;
            grpParameters.Visible = true;

            cmbDisplayType.SelectedIndex = display_type;
            cmbCentreSpotPosition.SelectedIndex = cam.leftcam.centre_spot_position;
            txtPatternSpacing.Text = Convert.ToString(cam.leftcam.calibration_pattern_spacing_mm);
            txtFOV.Text = Convert.ToString(cam.leftcam.camera_FOV_degrees);
            txtSpacingFactor.Text = Convert.ToString(cam.leftcam.separation_factor);
            txtCameraHeight.Text = Convert.ToString(cam.leftcam.camera_height_mm);
            txtDistToCentre.Text = Convert.ToString(cam.leftcam.camera_dist_to_pattern_centre_mm);
        }

        private void startCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            initCamera();
            if (global_variables.selectedCameraName != "")
            {
                startCameraToolStripMenuItem.Enabled = false;

                global_variables.camera_initialised = false;

                // grab the required camera WDM device name from the text box
                // (hope you remembered to fill that in correctly)
                String cameraFilterName = global_variables.selectedCameraName;

                if (cameraFilterName.Contains("#"))
                {
                    String[] str = cameraFilterName.Split('#');
                    cameraFilterName = str[0].Trim();
                }

                //transformRight = RotateFlipType.Rotate180FlipNone;

                // get the device list index number for this WDM device
                int index = global_variables.getCameraIndexContaining(cameraFilterName, 0);
                if (index > -1)
                {
                    // start running the left camera
                    global_variables.selectCamera(true, index);
                    initCameraLeft2();
                    no_of_cameras = 1;

                    index = global_variables.getCameraIndexContaining(cameraFilterName, 1);
                    if (index > -1)
                    {
                        //start running the right camera
                        // note: it's assumed that the device index for the right
                        // camera is immediately after the left one
                        global_variables.selectCamera(false, index);
                        initCameraRight2();
                        no_of_cameras = 2;
                    }

                    // and lo, the cameras were initialised...
                    global_variables.camera_initialised = true;
                }
                else MessageBox.Show("Cannot find a WDM driver for the device known as '" + cameraFilterName + "'.  Are the cameras plugged in ?", "Sentience Demo");

                Redraw();
            }
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
                cam.leftcam.calibration_pattern_spacing_mm = Convert.ToSingle(txtPatternSpacing.Text);
                cam.rightcam.calibration_pattern_spacing_mm = cam.leftcam.calibration_pattern_spacing_mm;
                ResetCalibration();
            }
        }

        private void txtPatternSpacing_Leave(object sender, EventArgs e)
        {
            if (txtPatternSpacing.Text == "") txtPatternSpacing.Text = "50";
            cam.leftcam.calibration_pattern_spacing_mm = Convert.ToSingle(txtPatternSpacing.Text);
            cam.rightcam.calibration_pattern_spacing_mm = cam.leftcam.calibration_pattern_spacing_mm;
            ResetCalibration();
        }

        private void txtFOV_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtFOV.Text == "") txtFOV.Text = "40";
                cam.leftcam.camera_FOV_degrees = Convert.ToSingle(txtFOV.Text);
                cam.rightcam.camera_FOV_degrees = cam.leftcam.camera_FOV_degrees;
                ResetCalibration();
            }
        }

        private void txtFOV_Leave(object sender, EventArgs e)
        {
            if (txtFOV.Text == "") txtFOV.Text = "40";
            cam.leftcam.camera_FOV_degrees = Convert.ToSingle(txtFOV.Text);
            cam.rightcam.camera_FOV_degrees = cam.leftcam.camera_FOV_degrees;
            ResetCalibration();
        }

        private void txtSpacingFactor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtSpacingFactor.Text == "") txtSpacingFactor.Text = "15";
                cam.leftcam.separation_factor = Convert.ToInt32(txtSpacingFactor.Text);
                cam.rightcam.separation_factor = cam.leftcam.separation_factor;
                ResetCalibration();
            }
        }

        private void txtSpacingFactor_Leave(object sender, EventArgs e)
        {
            if (txtSpacingFactor.Text == "") txtSpacingFactor.Text = "15";
            cam.leftcam.separation_factor = Convert.ToInt32(txtSpacingFactor.Text);
            cam.rightcam.separation_factor = cam.leftcam.separation_factor;
            ResetCalibration();
        }

        private void txtCameraHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtCameraHeight.Text == "") txtCameraHeight.Text = "500";
                cam.leftcam.camera_height_mm = Convert.ToInt32(txtCameraHeight.Text);
                cam.rightcam.camera_height_mm = cam.leftcam.camera_height_mm;
                ResetCalibration();
            }
        }

        private void txtCameraHeight_Leave(object sender, EventArgs e)
        {
            if (txtCameraHeight.Text == "") txtCameraHeight.Text = "500";
            cam.leftcam.camera_height_mm = Convert.ToInt32(txtCameraHeight.Text);
            cam.rightcam.camera_height_mm = cam.leftcam.camera_height_mm;
            ResetCalibration();
        }

        private void txtDistToCentre_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtDistToCentre.Text == "") txtDistToCentre.Text = "500";
                cam.leftcam.camera_dist_to_pattern_centre_mm = Convert.ToInt32(txtDistToCentre.Text);
                cam.rightcam.camera_dist_to_pattern_centre_mm = cam.leftcam.camera_dist_to_pattern_centre_mm;
                ResetCalibration();
            }
        }

        private void resetCalibrationDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetCalibration();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Load calibration file";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                cam.Load(openFileDialog1.FileName);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            saveFileDialog1.DefaultExt = "xml";
            saveFileDialog1.FileName = "calibration_" + global_variables.CamSettingsLeft.cameraName + ".xml";
            saveFileDialog1.Filter = "Xml files|*.xml";
            saveFileDialog1.Title = "Save calibration file";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                cam.Save(saveFileDialog1.FileName);
        }

        private void cmbCentreSpotPosition_SelectedIndexChanged(object sender, EventArgs e)
        {
            cam.setCentreSpotPosition(cmbCentreSpotPosition.SelectedIndex);
            ResetCalibration();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveCalibrationSetup(calibration_setup_filename);
        }

    }
}