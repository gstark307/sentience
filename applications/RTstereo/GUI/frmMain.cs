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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using DirectX.Capture;
using sluggish.utilities;
using sluggish.utilities.timing;
using sentience.core;

namespace WindowsApplication1
{

    public partial class frmMain : Form
    {
        // object for doing stereo correspondence
        processstereo stereo = new processstereo();

        globals global_variables;
        int display_type = 0;

        bool left_camera_running = false;
        bool right_camera_running = false;
        bool left_imageloaded=false;
        bool right_imageloaded=false;
        String left_image_filename="";
        String right_image_filename="";
        String output_filename1="";
        String output_filename2 = "";
        String output_filename3 = "";

        //recording and simulation
        bool recordImages = false;
        int output_file_index = 0;
        bool simulation_running = false;
        int simulation_file_index = 101;
        int delay_counter = 0;
        DateTime last_image_recorded = DateTime.Now;

        int[] captureState = new int[2];
        Bitmap leftbmp, left_image;
        Bitmap rightbmp, right_image;

        RotateFlipType transformLeft = 0;
        RotateFlipType transformRight = 0;

        //output image
        Byte[] disp_bmp_data;
        bool outputInitialised = false;

        //used for calculating stereo processing time
        int updates = 0;
        int totTime = 0;

        // used for storing path data
        int viewpointNumber = 1;

        private stopwatch clock = new stopwatch();

        Byte[] background_bmp = null;

#region "Camera stuff"
        public void updateVisionRight(Image input_img, bool leftImage)
        {
            try
            {
                if (captureState[1] == 1)
                {
                    if (global_variables.right_bmp == null)
                        global_variables.right_bmp = new Byte[input_img.Width * input_img.Height * 3];

                    right_image = (Bitmap)input_img.Clone();
                    BitmapArrayConversions.updatebitmap(right_image, global_variables.right_bmp);                    
                    captureState[1] = 2;
                }
                right_camera_running = true;
            }
            catch //(Exception ex)
            {
                //MessageBox.Show("updateVisionRight/" + ex.Message);
            }
        }

        public void updateVisionLeft(Image input_img, bool leftImage)
        {
            try
            {
                if (captureState[0] == 1)
                {
                    if (global_variables.left_bmp == null)
                        global_variables.left_bmp = new Byte[input_img.Width * input_img.Height * 3];

                    left_image = (Bitmap)input_img.Clone();
                    BitmapArrayConversions.updatebitmap(left_image, global_variables.left_bmp);

                    captureState[0] = 2;
                }
                left_camera_running = true;
            }
            catch //(Exception ex)
            {
                //MessageBox.Show("updateVisionLeft/" + ex.Message);
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
                //transform the image appropriately
                transformImage(Frame, transformLeft);

                global_variables.CaptureInformationLeft.CounterFrames ++;
                updateVisionLeft(Frame.Image, true);
            }
            catch //(Exception ex)
            {

            }
        }

        public void RefreshRightImage(PictureBox Frame) //object sender, FrameGrabArgs f)
        {
            try
            {
                //transform the image appropriately
                transformImage(Frame, transformRight);            
                global_variables.CaptureInformationRight.CounterFrames ++;
                updateVisionRight(Frame.Image, false);
            }
            catch //(Exception ex)
            {

            }
        }

        #endregion


        /// <summary>
        /// initialise the left camera
        /// </summary>
        private void initCameraLeft2()
        {
            leftbmp = new Bitmap(global_variables.default_resolution_width, global_variables.default_resolution_height, PixelFormat.Format24bppRgb);
            picLeftImage.Image = leftbmp;
            picLeftImage.Visible = true;

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

        /// <summary>
        /// choose a left camera from a drop down list
        /// </summary>
        public void initCameraLeft()
        {
            //Call to AddCam to select an available camera
            AddCam AddCamera = new AddCam(global_variables);

            AddCamera.LeftImage = true;
            AddCamera.ShowDialog(this);
            initCameraLeft2();
        }

        /// <summary>
        /// initialise the right camera
        /// </summary>
        private void initCameraRight2()
        {
            global_variables.CaptureInformationRight.CaptureInfo.PreviewWindow = this.picRightImage;

            rightbmp = new Bitmap(global_variables.default_resolution_width, global_variables.default_resolution_height, PixelFormat.Format24bppRgb);
            picRightImage.Image = rightbmp;
            picRightImage.Visible = true;

            global_variables.ConfParamCamRight();
            global_variables.PrepareCamRight(global_variables.CaptureInformationRight.PathVideo);

            //Define RefreshImage as event handler of FrameCaptureComplete
            global_variables.CaptureInformationRight.CaptureInfo.FrameCaptureComplete += new Capture.FrameCapHandler(this.RefreshRightImage);

            global_variables.CaptureInformationRight.CaptureInfo.CaptureFrame();

            global_variables.CaptureInformationRight.Counter = 1;
            global_variables.CaptureInformationRight.CounterFrames = 1;

            this.Show();
        }


        /// <summary>
        /// choose a right camera from a drop down list
        /// </summary>
        private void initCameraRight()
        {
            //Call to AddCam to select an available camera
            AddCam AddCamera = new AddCam(global_variables);

            AddCamera.LeftImage = false;
            AddCamera.ShowDialog(this);
            initCameraRight2();
        }
#endregion

        public frmMain()
        {
            Random rnd = new Random();

            global_variables = new globals();

            InitializeComponent();

            initialise();
            
            // load calibration data
            stereo.loadCalibrationData(global_variables.calibration_filename);

            //what cameras are available?  Populate the list box
            lstCameraDevices.Items.Clear();
            for (short i = 0; i < global_variables.WDM_filters.VideoInputDevices.Count; i++)
            {
                Filter f = global_variables.WDM_filters.VideoInputDevices[i];
                if (!f.Name.ToLower().Contains("(vfw)"))  // don't show VFW drivers
                    lstCameraDevices.Items.Add(f.Name);
            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void initialise()
        {
            //set some filenames for use in calibration
            String path = System.AppDomain.CurrentDomain.BaseDirectory + "\\";
            global_variables.calibration_filename = path + "calibration.xml";

            captureState[0] = 0;
            captureState[1] = 0;            
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            
        }


        #region "stuff not used"

        /// <summary>
        /// load a pair of stereo images
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        private void loadStereoPair(String path, int index)
        {
            String filename;
            FileStream fs;

            filename = path + "\\left" + index + ".jpg";
            fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            picLeftImage.Image = Image.FromStream(fs);
            fs.Close();
            picLeftImage.Visible = true;
            picLeftImage.Refresh();
            left_image_filename = filename;
            left_imageloaded = true;

            filename = path + "\\right" + index + ".jpg";
            fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            picRightImage.Image = Image.FromStream(fs);
            fs.Close();
            picRightImage.Visible = true;
            picRightImage.Refresh();
            right_image_filename = filename;
            right_imageloaded = true;
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

        #endregion


        private void captureCameraImages()
        {
            //capture images from the two cameras
            //note the use of a state indicator to ensure that captures are only initiated at the appropriate time
            if ((left_camera_running) && (right_camera_running))
            {
                try
                {
                    if ((captureState[0] < 2) && (!global_variables.CaptureInformationLeft.CaptureInfo.Capturing))
                    {
                        captureState[0] = 1;
                        global_variables.CaptureInformationLeft.CaptureInfo.CaptureFrame();
                    }
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

        private void recordRawImages()
        {
            //record images for demo purposes
            if (recordImages)
            {
                TimeSpan diff = DateTime.Now.Subtract(last_image_recorded);
                if (diff.TotalSeconds > 0) // we don't want to kill the hard disk with a glut of images
                {
                    output_file_index = output_file_index + 1;
                    if (output_file_index > 998) output_file_index = 0;
                    output_filename1 = System.AppDomain.CurrentDomain.BaseDirectory + "\\left_" + Convert.ToString(output_file_index) + ".jpg";
                    output_filename2 = System.AppDomain.CurrentDomain.BaseDirectory + "\\right_" + Convert.ToString(output_file_index) + ".jpg";
                    output_filename3 = System.AppDomain.CurrentDomain.BaseDirectory + "\\output_" + Convert.ToString(output_file_index) + ".jpg";
                    left_image.Save(output_filename1, ImageFormat.Jpeg);
                    right_image.Save(output_filename2, ImageFormat.Jpeg);
                    last_image_recorded = DateTime.Now;
                }
            }
        }

        private void loadSimulationImages()
        {
            String left_filename = System.AppDomain.CurrentDomain.BaseDirectory + "\\left_" + Convert.ToString(simulation_file_index) + ".jpg";
            String right_filename = System.AppDomain.CurrentDomain.BaseDirectory + "\\right_" + Convert.ToString(simulation_file_index) + ".jpg";

            captureState[0] = 0;
            captureState[1] = 0;
            if ((File.Exists(left_filename)) && (File.Exists(right_filename)))
            {
                picLeftImage.Image = new Bitmap(left_filename);
                picLeftImage.Refresh();
                left_image = (Bitmap)picLeftImage.Image.Clone();
                if (global_variables.left_bmp == null)
                    global_variables.left_bmp = new Byte[left_image.Width * left_image.Height * 3];
                BitmapArrayConversions.updatebitmap(left_image, global_variables.left_bmp);
                captureState[0] = 2;

                picRightImage.Image = new Bitmap(right_filename);
                picRightImage.Refresh();
                right_image = (Bitmap)picRightImage.Image.Clone();
                if (global_variables.right_bmp == null)
                    global_variables.right_bmp = new Byte[right_image.Width * right_image.Height * 3];
                BitmapArrayConversions.updatebitmap(right_image, global_variables.right_bmp);
                captureState[1] = 2;
            }

            // we need to slow things down, otherwise the simulation runs at lightning speed
            delay_counter++;
            if (delay_counter > 2)
            {
                delay_counter = 0;
                simulation_file_index++;
                if (simulation_file_index >= output_file_index)
                {
                    simulation_file_index = 1;
                }
            }
        }



        /// <summary>
        /// get the number of frames in the simulation
        /// </summary>
        /// <returns></returns>
        private int getNoOfSimulationFrames()
        {
            String filename;
            bool found = true;
            int i = 1;
            int max_frame = 0;

            while (found)
            {
                filename = System.AppDomain.CurrentDomain.BaseDirectory + "\\left_" + Convert.ToString(i) + ".jpg";
                if (File.Exists(filename))
                    i++;
                else
                    found = false;
            }
            if (i > 1) max_frame = i - 1;
            return (max_frame);
        }


        private void updateProcessingTime(long processingTime)
        {
            totTime += (int)processingTime;
            updates++;

            if (updates > 10)
            {
                if (totTime/updates < 5)
                    txtStereoTime.Text = "< 18";
                else
                    txtStereoTime.Text = Convert.ToString(totTime / updates);
                updates = 0;
                totTime = 0;
            }            
        }

        private void updateObstacleRange()
        {
            float focal_length_mm = Convert.ToSingle(txtFocalLength.Text);
            float baseline_mm = Convert.ToSingle(txtBaseline.Text);
            float FOV_radians = Convert.ToSingle(txtFOV.Text) * (float)Math.PI / 180.0f;
            float range = stereo.getObstacleDistance(focal_length_mm, baseline_mm, global_variables.standard_width, FOV_radians);
            range = (int)(range/10)*10;
            txtObstacleRange.Text = Convert.ToString(range);
        }

        private void loadBackgroundImage(String filename)
        {
            if (File.Exists(filename))
            {
                Bitmap background = new Bitmap(filename);
                Byte[] back_bmp = new Byte[background.Width * background.Height * 3];
                BitmapArrayConversions.updatebitmapslow(background, back_bmp);
                background_bmp = new Byte[global_variables.standard_width * global_variables.standard_height * 3];

                int n = 0;
                for (int y = 0; y < global_variables.standard_height; y++)
                {
                    int yy = y * background.Height / global_variables.standard_height;
                    for (int x = 0; x < global_variables.standard_width; x++)
                    {
                        int xx = x * background.Width / global_variables.standard_width;
                        int n2 = ((yy * background.Width) + xx) * 3;
                        for (int c = 0; c < 3; c++)
                        {
                            background_bmp[n] = back_bmp[n2+c];
                            n++;
                        }
                    }
                }
            }
        }

        private void timUpdate_Tick(object sender, EventArgs e)
        {
            int processingTime;
            bool readyToUpdate = false;

            if ((simulation_running) || ((left_imageloaded) && (right_imageloaded)) || ((left_camera_running) && (right_camera_running)))
            {
                // get images from the two cameras
                if (!simulation_running)
                {
                    // get images from cameras
                    captureCameraImages();
                }
                else
                {
                    // get stored images for simulation
                    loadSimulationImages();
                }

                if ((!simulation_running) && (!((left_camera_running) && (right_camera_running))))
                {
                    //update from loaded images
                    picLeftImage.Image = new Bitmap(left_image_filename);
                    picRightImage.Image = new Bitmap(right_image_filename);

                    global_variables.standard_width = picLeftImage.Image.Width;
                    global_variables.standard_height = picLeftImage.Image.Height;

                    // initialise arrays if necessary
                    if (global_variables.left_bmp == null)
                    {
                        global_variables.left_bmp = new Byte[global_variables.standard_width * global_variables.standard_height * 3];
                        global_variables.right_bmp = new Byte[global_variables.standard_width * global_variables.standard_height * 3];
                    }

                    left_image = (Bitmap)picLeftImage.Image;
                    right_image = (Bitmap)picRightImage.Image;

                    // copy bitmap data into byte arrays
                    BitmapArrayConversions.updatebitmap(left_image, global_variables.left_bmp);
                    BitmapArrayConversions.updatebitmap(right_image, global_variables.right_bmp);

                    readyToUpdate = true;
                }
                else
                {
                    //update from live cameras
                    if ((captureState[0] == 2) && (captureState[1] == 2))
                    {
                        // reset the capture state, so the system knows it can begin
                        // grabbing frames once again
                        captureState[0] = 0;
                        captureState[1] = 0;
                        readyToUpdate = true;
                    }
                }

                // we have the images. Now it's time to do something with them...
                if (readyToUpdate)
                {
                    // start timing the stereo processing
                    clock.Start();

                    // find stereo correspondences
                    stereo.stereoMatch(global_variables.left_bmp, global_variables.right_bmp,
                                   global_variables.standard_width, global_variables.standard_height, true);

                    // initialise some bitmaps to show the output of stereo processing
                    if (!outputInitialised)
                    {
                        picOutput.Image = new Bitmap(global_variables.standard_width, global_variables.standard_height, PixelFormat.Format24bppRgb);
                        disp_bmp_data = new Byte[global_variables.standard_width * global_variables.standard_height * 3];
                        outputInitialised = true;
                    }

                    // how long did the processing take ?
                    processingTime = (int)clock.Stop();

                    // update the frames per second and other info
                    updateProcessingTime(processingTime);
                    txtNoOfFeatures.Text = Convert.ToString(stereo.no_of_disparities);

                    // show range to obstacles
                    updateObstacleRange();

                    // show stereo disparities as circles
                    switch(display_type)
                    {
                        case 0:
                            {
                                showDisparities();
                                break;
                            }
                        case 1:
                            {
                                showRadar();
                                break;
                            }
                        case 2:
                            {
                                showDisparityMap();
                                break;
                            }
                        case 3:
                            {
                                showCloseObjects();
                                break;
                            }
                        case 4:
                            {
                                showRays();
                                break;
                            }
                        case 5:
                            {
                                showDisparityMap();
                                break;
                            }
                    }

                    // record left and right images to be used later for simulation or analysis
                    if (!simulation_running) recordRawImages();
                }

            }

        }


        private void recordImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // record images for subsequent demos/analysis
            recordImagesToolStripMenuItem.Checked = !recordImagesToolStripMenuItem.Checked;
            output_file_index = 0;
            recordImages = recordImagesToolStripMenuItem.Checked;
        }

        private void startSentienceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // open a calibration file if none was found
            if (!File.Exists(global_variables.calibration_filename))
                OpenCalibrationFile();

            if (File.Exists(global_variables.calibration_filename))
            {
                // load calibration settings
                stereo.LoadCalibration(global_variables.calibration_filename);

                String cameraFilterName = stereo.getCameraDriverName();
                txtCameraDeviceName.Text = cameraFilterName;

                // grab the required camera WDM device name from the text box
                // (hope you remembered to fill that in correctly)
                /*
                string cameraFilterName = txtCameraDeviceName.Text;

                if (cameraFilterName.Contains("#"))
                {
                    String[] s = cameraFilterName.Split('#');
                    cameraFilterName = s[0].Trim();
                }
                 */

                // get the device list index number for this WDM device
                int index = global_variables.getCameraIndexContaining(cameraFilterName, 0);
                if (index > -1)
                {
                    // start running the left camera
                    global_variables.selectCamera(true, index);
                    initCameraLeft2();

                    index = global_variables.getCameraIndexContaining(cameraFilterName, 1);
                    if (index > -1)
                    {
                        //start running the right camera
                        // note: it's assumed that the device index for the right
                        // camera is immediately after the left one
                        global_variables.selectCamera(false, index);
                        initCameraRight2();

                        // do some crap with the menu bar
                        startSentienceToolStripMenuItem.Enabled = false;
                        startSimulationToolStripMenuItem.Enabled = false;
                        optomiseToolStripMenuItem.Enabled = false;

                        // and lo, the cameras were initialised...
                        global_variables.camera_initialised = true;

                        tabSentience.SelectedIndex = 1;

                        // and lo, the cameras were initialised...
                        global_variables.camera_initialised = true;
                    }
                    else MessageBox.Show("Cannot find a WDM driver for the right camera device known as '" + cameraFilterName + "'.  Is the right camera plugged in ?", "Sentience Demo");
                }
                else MessageBox.Show("Cannot find a WDM driver for the left camera device known as '" + cameraFilterName + "'.  Is the left camera plugged in ?", "Sentience Demo");
            }
            else MessageBox.Show("Could not locate a calibration file '" + global_variables.calibration_filename + "'", "Sentience Demo");
        }

        private void startSimulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int frames = getNoOfSimulationFrames();
            if (frames > 0)
            {
                output_file_index = frames;
                startSentienceToolStripMenuItem.Enabled = false;
                startSimulationToolStripMenuItem.Enabled = false;
                simulation_running = true;
                tabSentience.SelectedIndex = 1;
            }
        }


        private void showDisparities()
        {
            Pen p;
            SolidBrush brush;
            Rectangle rect;
            int i, radius_x, radius_y, x, y;
            Graphics gr;
            
            if (left_image != null)
            {
                // copy the left image, as a backround
                picOutput.Image = (Bitmap)left_image.Clone();

                // graphics junk
                gr = Graphics.FromImage(picOutput.Image);
                rect = new Rectangle();

                // make a brush with some alpha blending
                brush = new SolidBrush(Color.FromArgb(120, 0, 160, 0));
                p = new Pen(brush);

                for (i = 0; i < stereo.no_of_disparities; i++)
                {
                    // get the position and disparity for this feature
                    x = (int)stereo.disparities[(i * 3)];
                    y = (int)stereo.disparities[(i * 3) + 1];
                    float disparity_pixels = stereo.disparities[(i * 3) + 2];

                    // circle radii
                    radius_x = (int)disparity_pixels/2;                    
                    radius_y = (int)disparity_pixels/2;

                    // make sure that the circles don't get so small that you can't see them!
                    if (radius_x < 2) radius_x = 2;
                    if (radius_y < 2) radius_y = 2;

                    // the rectangle of doom
                    rect.X = x;
                    rect.Y = y;
                    rect.Width = radius_x * 2;
                    rect.Height = radius_y * 2;

                    // draw the circle
                    gr.FillEllipse(brush, rect);
                }
                picOutput.Refresh();
            }
             
        }


        private void showRadar()
        {
            BitmapArrayConversions.updatebitmap_unsafe(stereo.getRadarImage(), (Bitmap)picOutput.Image);
            picOutput.Refresh();
        }

        private void showRays()
        {
            BitmapArrayConversions.updatebitmap_unsafe(stereo.getRaysImage(picOutput.Image.Width, picOutput.Image.Height), (Bitmap)picOutput.Image);
            picOutput.Refresh();
        }

        private void showDisparityMap()
        {
            stereo.getDisparityMap(disp_bmp_data, picOutput.Image.Width, picOutput.Image.Height, 0);
            BitmapArrayConversions.updatebitmap_unsafe(disp_bmp_data, (Bitmap)picOutput.Image);
            picOutput.Refresh();
        }

        private void showCloseObjects()
        {
            stereo.getCloseObjects(global_variables.left_bmp, disp_bmp_data, background_bmp,picOutput.Image.Width, picOutput.Image.Height, 130);
            BitmapArrayConversions.updatebitmap_unsafe(disp_bmp_data, (Bitmap)picOutput.Image);
            picOutput.Refresh();
        }

        private void lstCameraDevices_Click(object sender, EventArgs e)
        {
            txtCameraDeviceName.Text = (String)lstCameraDevices.Items[lstCameraDevices.SelectedIndex];
        }

        /// <summary>
        /// save images along a path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdSavePosition_Click(object sender, EventArgs e)
        {
            String left_filename = txtPathName.Text + "_left_" + Convert.ToString(viewpointNumber + ".bmp");
            String right_filename = txtPathName.Text + "_right_" + Convert.ToString(viewpointNumber + ".bmp");

            if (File.Exists(left_filename))
                File.Delete(left_filename);
            left_image.Save(left_filename, ImageFormat.Bmp);
            if (File.Exists(right_filename))
                File.Delete(right_filename);
            right_image.Save(right_filename, ImageFormat.Bmp);                

            viewpointNumber++;

            MessageBox.Show("Path data saved");
        }

        /// <summary>
        /// clear the images recorded for a path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdClear_Click(object sender, EventArgs e)
        {
            ///Directory.Delete(txtPathName.Text + "_left_*.bmp");
            //Directory.Delete(txtPathName.Text + "_right_*.bmp");
            viewpointNumber = 1;
            MessageBox.Show("Path data cleared");
        }

        private void stereoFeaturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo.setCorrespondenceAlgorithmType(0);
            display_type = 0;
            stereoFeaturesToolStripMenuItem.Checked = true;
            roboRadarToolStripMenuItem.Checked = false; 
            disparityMapToolStripMenuItem.Checked = false;
            nearbyObjectsToolStripMenuItem.Checked = false;
            raysToolStripMenuItem.Checked = false;
            linesToolStripMenuItem.Checked = false;
        }

        private void roboRadarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo.setCorrespondenceAlgorithmType(0);
            display_type = 1;
            stereoFeaturesToolStripMenuItem.Checked = false;
            roboRadarToolStripMenuItem.Checked = true;
            disparityMapToolStripMenuItem.Checked = false;
            nearbyObjectsToolStripMenuItem.Checked = false;
            raysToolStripMenuItem.Checked = false;
            linesToolStripMenuItem.Checked = false;
        }

        private void disparityMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo.setCorrespondenceAlgorithmType(1);
            display_type = 2;
            stereoFeaturesToolStripMenuItem.Checked = false;
            roboRadarToolStripMenuItem.Checked = false;
            disparityMapToolStripMenuItem.Checked = true;
            nearbyObjectsToolStripMenuItem.Checked = false;
            raysToolStripMenuItem.Checked = false;
            linesToolStripMenuItem.Checked = false;
        }

        private void nearbyObjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo.setCorrespondenceAlgorithmType(1);
            display_type = 3;
            stereoFeaturesToolStripMenuItem.Checked = false;
            roboRadarToolStripMenuItem.Checked = false;
            disparityMapToolStripMenuItem.Checked = false;
            nearbyObjectsToolStripMenuItem.Checked = true;
            raysToolStripMenuItem.Checked = false;
            linesToolStripMenuItem.Checked = false;
        }

        private void openBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                loadBackgroundImage(openFileDialog1.FileName);
        }

        private void raysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo.setCorrespondenceAlgorithmType(0);
            display_type = 4;
            stereoFeaturesToolStripMenuItem.Checked = false;
            roboRadarToolStripMenuItem.Checked = false;
            disparityMapToolStripMenuItem.Checked = false;
            nearbyObjectsToolStripMenuItem.Checked = false;
            raysToolStripMenuItem.Checked = true;
            linesToolStripMenuItem.Checked = false;
        }

        private void linesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stereo.setCorrespondenceAlgorithmType(2);
            display_type = 0; // 5;
            stereoFeaturesToolStripMenuItem.Checked = false;
            roboRadarToolStripMenuItem.Checked = false;
            disparityMapToolStripMenuItem.Checked = false;
            nearbyObjectsToolStripMenuItem.Checked = false;
            raysToolStripMenuItem.Checked = false;
            linesToolStripMenuItem.Checked = true;
        }

        private void OpenCalibrationFile()
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Load calibration file";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.Copy(openFileDialog1.FileName, global_variables.calibration_filename);
            }
        }

        private void loadCalibrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenCalibrationFile();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

    }
}