using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using sentience.core;
using sluggish.utilities;

namespace stereocorrespondence
{
    public partial class frmMain : Form
    {
        // an interface to different stereo correspondence algorithms
        sentience_stereo_interface stereointerface = new sentience_stereo_interface();
        
        // sensor model stuff
        stereoModel inverseSensorModel;

        //the type of stereo correspondance algorithm to be used
        int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_CONTOURS;


        // default path where stereo images are located
        String images_directory = "C:\\Develop\\sentience\\testdata\\seq1";

        // stereo calibration or robot design file
        String calibration_filename = "C:\\Develop\\sentience\\testdata\\seq1\\calibration.xml";

        // undex number of the currently displayed set of stereo images
        int stereo_image_index = 0;


        #region "constructors"

        public frmMain()
        {
            InitializeComponent();

            // Set the help text description for the FolderBrowserDialog.
            folderBrowserDialog1.Description =
                "Select the directory where stereo images are located";

            // Do not allow the user to create new files via the FolderBrowserDialog.
            folderBrowserDialog1.ShowNewFolderButton = false;

            txtImagesDirectory.Text = images_directory;

            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.Filter = "xml files (*.xml)|*.xml";

            txtCalibrationFilename.Text = calibration_filename;


            // sensor model used for mapping
            inverseSensorModel = new stereoModel();
        }

        #endregion

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region "opening a directory"

        private void cmdBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if( result == DialogResult.OK )
            {
                images_directory = folderBrowserDialog1.SelectedPath;
                txtImagesDirectory.Text = images_directory;
            }
        }

        #endregion

        #region "loading stereo images"

        private bool LoadStereoImages()
        {
            bool success = false;
            String left_image_filename = images_directory + "\\test_left_" + stereo_image_index.ToString() + ".jpg";
            String right_image_filename = images_directory + "\\test_right_" + stereo_image_index.ToString() + ".jpg";

            if (File.Exists(left_image_filename))
            {
                if (File.Exists(right_image_filename))
                {
                    picLeftImage.Load(left_image_filename);
                    picRightImage.Load(right_image_filename);
                    success = true;
                }
                else
                {
                    MessageBox.Show("Could not find image " + right_image_filename);
                }
            }
            else
            {
                MessageBox.Show("Could not find image " + left_image_filename);
            }
            return (success);
        }

        #endregion

        #region "moving through the sequence of stereo images"

        private void cmdNext_Click(object sender, EventArgs e)
        {
            stereo_image_index++;
            bool loaded = LoadStereoImages();
            if (loaded)
            {
                update();
            }
            else
            {
                stereo_image_index = 0;
            }
        }

        private void cmdPrevious_Click(object sender, EventArgs e)
        {
            stereo_image_index--;
            if (stereo_image_index < 1) stereo_image_index = 1;
            bool loaded = LoadStereoImages();
            if (loaded)
            {
                update();
            }
            else
            {
                stereo_image_index = 0;
            }
        }

        #endregion

        #region "opening a robot design or calibration file"

        private void cmdRobotDefinitionBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                calibration_filename = openFileDialog1.FileName;
                txtCalibrationFilename.Text = calibration_filename;
            }
        }

        #endregion

        #region "stereo correspondence"

        private void update()
        {
            // load calibration data
            stereointerface.loadCalibration(calibration_filename);

            // get image data from bitmaps
            int bytes_per_pixel = 3;
            int image_width = picLeftImage.Image.Width;
            int image_height = picLeftImage.Image.Height;
            byte[] fullres_left = new byte[image_width * image_height *
                                           bytes_per_pixel];
            byte[] fullres_right = new byte[fullres_left.Length];
            BitmapArrayConversions.updatebitmap((Bitmap)picLeftImage.Image, fullres_left);
            BitmapArrayConversions.updatebitmap((Bitmap)picRightImage.Image, fullres_right);

            // load images into the correspondence object
            stereointerface.loadImage(fullres_left, image_width, image_height, true, bytes_per_pixel);
            stereointerface.loadImage(fullres_right, image_width, image_height, false, bytes_per_pixel);

            stereointerface.setDisparityMapCompression(1, 3);

            // perform stereo matching
            stereointerface.stereoMatchRun(0, 8, correspondence_algorithm_type);

            // make a bitmap
            Bitmap depthmap_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picDepthMap.Image = depthmap_bmp;
            
            byte[] depthmap = new byte[image_width * image_height * 3];
            stereointerface.getDisparityMap(depthmap, image_width, image_height, 0);

            BitmapArrayConversions.updatebitmap_unsafe(depthmap, depthmap_bmp);
            picDepthMap.Refresh();
        }

        #endregion
    }
}