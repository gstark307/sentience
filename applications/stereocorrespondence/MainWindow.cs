/*
    Stereo correspondence test program
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
using Gtk;
using sentience.core;
using sluggish.utilities;
using sluggish.utilities.gtk;
using sluggish.utilities.timing;

public partial class MainWindow: Gtk.Window
{
    // timer for benchmarking the stereo algorithm
    stopwatch clock = new stopwatch();

    int horizontal_compression = 1;
    int vertical_compression = 3;

    // an interface to different stereo correspondence algorithms
    sentience_stereo_interface stereointerface = new sentience_stereo_interface();
        
    //the type of stereo correspondance algorithm to be used
    int correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_CONTOURS;

    // default path where stereo images are located
    string images_directory = "/home/motters/develop/sentience/testdata/seq2";

    // stereo calibration or robot design file
    string calibration_filename = "/home/motters/develop/sentience/testdata/seq2/calibration.xml";

    // undex number of the currently displayed set of stereo images
    int stereo_image_index = 0;

    int image_width=0, image_height=0;
    byte[] fullres_left;
    byte[] fullres_right;
	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		
		txtImagesDirectory.Buffer.Text = images_directory;
        txtCalibrationFilename.Buffer.Text = calibration_filename;
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected virtual void OnExitActivated (object sender, System.EventArgs e)
	{
	    Application.Quit();
	}

    #region "setting files and directories"

	protected virtual void OnCmdImagesDirectoryClicked (object sender, System.EventArgs e)
	{
	}

	protected virtual void OnCmdCalibrationFilenameClicked (object sender, System.EventArgs e)
	{
		Gtk.FileChooserDialog fc=
            new Gtk.FileChooserDialog("Choose the calibration or robot design file to open",
                                      this,
                                      FileChooserAction.Open,
                                      "Cancel",ResponseType.Cancel,
                                      "Open",ResponseType.Accept);

        fc.SetCurrentFolder(images_directory);
        if (fc.Run() == (int)ResponseType.Accept) 
        {
            String filename = fc.Filename;
            
            if (filename.EndsWith(".xml"))
            {
                calibration_filename = filename;
                txtCalibrationFilename.Buffer.Text = filename;
            }
            else
            {
                MessageDialog md = new MessageDialog (this, 
                    DialogFlags.DestroyWithParent,
                    MessageType.Warning, 
                    ButtonsType.Ok, 
                    "The file you selected does not appear to be in xml format");
     
                md.Run ();
                md.Destroy();
            }
        }
        fc.Destroy();	
	}
	
	#endregion

    #region "moving through the sequence of images"

	protected virtual void OnCmdPreviousClicked (object sender, System.EventArgs e)
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

	protected virtual void OnCmdNextClicked (object sender, System.EventArgs e)
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
	
	#endregion
	
    #region "loading stereo images"

    private bool LoadStereoImages()
    {
        bool success = false;
        string left_image_filename = images_directory + "/test_left_" + stereo_image_index.ToString() + ".jpg";
        string right_image_filename = images_directory + "/test_right_" + stereo_image_index.ToString() + ".jpg";

        if (System.IO.File.Exists(left_image_filename))
        {
            if (System.IO.File.Exists(right_image_filename))
            {
                fullres_left = GtkBitmap.Load(left_image_filename, ref image_width, ref image_height);
                picLeftImage.Pixbuf = GtkBitmap.createPixbuf(image_width, image_height);
		        GtkBitmap.setBitmap(fullres_left, picLeftImage);

                fullres_right = GtkBitmap.Load(right_image_filename, ref image_width, ref image_height);
                picRightImage.Pixbuf = GtkBitmap.createPixbuf(image_width, image_height);
		        GtkBitmap.setBitmap(fullres_right, picRightImage);

                success = true;
            }
            else
            {
                //MessageBox.Show("Could not find image " + right_image_filename);
            }
        }
        else
        {
            //MessageBox.Show("Could not find image " + left_image_filename);
        }
        return (success);
    }

    #endregion


    #region "stereo correspondence"

    private void update()
    {
        // load calibration data
        stereointerface.loadCalibration(calibration_filename);

        // get image data from bitmaps
        int bytes_per_pixel = 3;
        
        // load images into the correspondence object
        stereointerface.loadImage(fullres_left, image_width, image_height, true, bytes_per_pixel);
        stereointerface.loadImage(fullres_right, image_width, image_height, false, bytes_per_pixel);

        // set the quality of the disparity map
        stereointerface.setDisparityMapCompression(horizontal_compression, vertical_compression);

        clock.Start();

        // perform stereo matching
        stereointerface.stereoMatchRun(0, 8, correspondence_algorithm_type);

        long correspondence_time_mS = clock.Stop();
        txtStereoCorrespondenceTime.Buffer.Text = correspondence_time_mS.ToString();

        // make a bitmap            
        byte[] depthmap = new byte[image_width * image_height * 3];        
        stereointerface.getDisparityMap(depthmap, image_width, image_height, 0);

        picDepthMap.Pixbuf = GtkBitmap.createPixbuf(image_width, image_height);
		GtkBitmap.setBitmap(depthmap, picDepthMap);

    }

    #endregion

    #region "set the quality of the depth map"

    protected virtual void OnLowActivated (object sender, System.EventArgs e)
    {
        horizontal_compression = 3;
        vertical_compression = 4;  
        Low.IsImportant = true;
        Medium.IsImportant = false;
        High.IsImportant = false;
    }

    protected virtual void OnMediumActivated (object sender, System.EventArgs e)
    {
        horizontal_compression = 2;
        vertical_compression = 3;
        Low.IsImportant = false;
        Medium.IsImportant = true;
        High.IsImportant = false;
    }

    protected virtual void OnHighActivated (object sender, System.EventArgs e)
    {
        horizontal_compression = 1;
        vertical_compression = 1;
        Low.IsImportant = false;
        Medium.IsImportant = false;
        High.IsImportant = true;
    }
    
    #endregion

    #region "choosing different types of correspondence algorithm"
    
    protected virtual void OnDepthMapActivated (object sender, System.EventArgs e)
    {
        correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_CONTOURS;
    }

    protected virtual void OnSimpleStereoActivated (object sender, System.EventArgs e)
    {
        correspondence_algorithm_type = sentience_stereo_interface.CORRESPONDENCE_SIMPLE;
    }
    
    #endregion
	
}