/*
    Test GUI for webcam based stereo camera
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
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Text;
using Gtk;
using surveyor.vision;
using sluggish.utilities;
using sluggish.utilities.gtk;

public partial class MainWindow: Gtk.Window
{
    int image_width = 320;
    int image_height = 240;
    string left_camera_device = "/dev/video2";
    string right_camera_device = "/dev/video1";
    string calibration_filename = "calibration.xml";
    int broadcast_port = 10010;
    int fps = 10;
    string temporary_files_path = "";
    string recorded_images_path = "";
	string manual_camera_alignment_calibration_program = "calibtweaks.exe";
    bool disable_rectification = true;
    bool disable_radial_correction = true;
	bool reverse_colours = true;
	int baseline_mm = 120;
	
	// flip images if the camera is mounted inverted
	bool flip_left_image = true;
	bool flip_right_image = false;
	
	// use v4l-info <device> to find the min and max brightness levels
	// for the camera, which are hardware specific
	// for Creative Webcam NX Ultra the brightness range is 0->650
	// for Minoru the brightness range is -10->10
	int minimum_brightness = -10;
	int maximum_brightness = 10;
         
    public WebcamVisionStereoGtk stereo_camera;
    
    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        Build ();

        byte[] img = new byte[image_width * image_height * 3];
        Bitmap left_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        Bitmap right_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        BitmapArrayConversions.updatebitmap_unsafe(img, left_bmp);
        BitmapArrayConversions.updatebitmap_unsafe(img, right_bmp);
        GtkBitmap.setBitmap(left_bmp, leftimage);
        GtkBitmap.setBitmap(right_bmp, rightimage);
        
        stereo_camera = new WebcamVisionStereoGtk(left_camera_device, right_camera_device, broadcast_port, fps);
        stereo_camera.temporary_files_path = temporary_files_path;
        stereo_camera.recorded_images_path = recorded_images_path;
        stereo_camera.window = this;
        stereo_camera.display_image[0] = leftimage;
        stereo_camera.display_image[1] = rightimage;
        //stereo_camera.Load(calibration_filename);
        stereo_camera.image_width = image_width;
        stereo_camera.image_height = image_height;        
		stereo_camera.min_exposure = minimum_brightness;
		stereo_camera.max_exposure = maximum_brightness;
		stereo_camera.flip_left_image = flip_left_image;
		stereo_camera.flip_right_image = flip_right_image;
		stereo_camera.baseline_mm = baseline_mm;
        stereo_camera.Run();
		
		stereo_camera.disable_rectification = disable_rectification;
		stereo_camera.disable_radial_correction = disable_radial_correction;
		chkRectification.Active = !disable_rectification;
		chkRadialCorrection.Active = !disable_radial_correction;
		chkFlipLeft.Active = stereo_camera.flip_left_image;
		chkFlipRight.Active = stereo_camera.flip_right_image;
		txtBaseline.Text = stereo_camera.baseline_mm.ToString();
    }	
    
    private void CloseForm()
    {
        stereo_camera.Stop();
        Application.Quit ();
    }
    
    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        CloseForm();
        a.RetVal = true;
    }

    protected virtual void OnExitActionActivated (object sender, System.EventArgs e)
    {
        CloseForm();
    }

    protected virtual void OnRecordImagesActionActivated (object sender, System.EventArgs e)
    {
        RecordImagesAction.Active = !RecordImagesAction.Active;
        stereo_camera.Record = RecordImagesAction.Active;
    }

    protected virtual void OnChkRecordClicked (object sender, System.EventArgs e)
    {
        stereo_camera.Record = !stereo_camera.Record;
        chkRecord.Active = stereo_camera.Record; 
    }

    private void ShowDotPattern(Gtk.Image dest_img)
    {
        stereo_camera.calibration_pattern = SurveyorCalibration.CreateDotPattern(image_width, image_height, SurveyorCalibration.dots_across, SurveyorCalibration.dot_radius_percent);
        GtkBitmap.setBitmap(stereo_camera.calibration_pattern, dest_img);
    }

    private void Calibrate(bool Active)
    {
        Gtk.Image dest_img = null;
        int window_index = 0;
        if (stereo_camera.show_left_image)
        {
            dest_img = leftimage;
            window_index = 0;
        }
        else
        {
            dest_img = rightimage;
            window_index = 1;
        }
    
        if (Active)
        {            
            ShowDotPattern(dest_img);
            stereo_camera.display_image[window_index] = dest_img;
            //stereo_camera.display_type = SurveyorVisionStereo.DISPLAY_CALIBRATION_DIFF;
            stereo_camera.display_type = SurveyorVisionStereo.DISPLAY_RECTIFIED;
            //stereo_camera.display_type = SurveyorVisionStereo.DISPLAY_DIFFERENCE;
            stereo_camera.ResetCalibration(1 - window_index);
			
			if (!stereo_camera.show_left_image)
			{
		        chkCalibrateRight.Active = false;
			}
			else
			{
		        chkCalibrateLeft.Active = false;
			}
        }
        else
        {
            stereo_camera.calibration_pattern = null;
            stereo_camera.display_image[0] = leftimage;
            stereo_camera.display_image[1] = rightimage;
            stereo_camera.display_type = SurveyorVisionStereo.DISPLAY_RAW;
        }
				
    }

    protected virtual void OnChkCalibrateLeftClicked (object sender, System.EventArgs e)
    {
        stereo_camera.show_left_image = false;
        Calibrate(chkCalibrateLeft.Active);
    }

    protected virtual void OnChkCalibrateRightClicked (object sender, System.EventArgs e)
    {
        stereo_camera.show_left_image = true;
        Calibrate(chkCalibrateRight.Active);
    }   

    private void ShowMessage(string message_str)
    {
        Console.WriteLine(message_str);
        /*
        Gtk.MessageDialog md = 
            new MessageDialog (this,
                               DialogFlags.DestroyWithParent,
    	                       MessageType.Info, 
                               ButtonsType.Close, 
                               message_str);
        this.GdkWindow.ProcessUpdates(true);
        md.Run();
        md.Destroy();
        */
    }
	
	private void CalibrateAlignment()
	{
		stereo_camera.baseline_mm = Convert.ToInt32(txtBaseline.Text);
        if (stereo_camera.CalibrateCameraAlignment())
        {
            stereo_camera.Save(calibration_filename);
            
            ShowMessage("Calibration complete");
        }
        else
        {
            ShowMessage("Please individually calibrate left and right cameras before the calibrating the alignment");
        }
	}

    protected virtual void OnCmdCalibrateAlignmentClicked (object sender, System.EventArgs e)
    {
		CalibrateAlignment();
    }

    protected virtual void OnCmdSimpleStereoClicked (object sender, System.EventArgs e)
    {
        stereo_camera.stereo_algorithm_type = StereoVision.EDGES;
		stereo_camera.Load(calibration_filename);
		chkFlipLeft.Active = stereo_camera.flip_left_image;
		chkFlipRight.Active = stereo_camera.flip_right_image;
		chkRectification.Active = !stereo_camera.disable_rectification;
		chkRadialCorrection.Active = !stereo_camera.disable_radial_correction;
		txtBaseline.Text = stereo_camera.baseline_mm.ToString();
    }

    protected virtual void OnCmdDenseStereoClicked (object sender, System.EventArgs e)
    {
        stereo_camera.stereo_algorithm_type = StereoVision.DENSE;
		stereo_camera.Load(calibration_filename);
		chkFlipLeft.Active = stereo_camera.flip_left_image;
		chkFlipRight.Active = stereo_camera.flip_right_image;
		chkRectification.Active = !stereo_camera.disable_rectification;
		chkRadialCorrection.Active = !stereo_camera.disable_radial_correction;
		txtBaseline.Text = stereo_camera.baseline_mm.ToString();
    }

    /// <summary>
    /// saves the calibration settings file to the desktop
    /// </summary>
    private void SaveCalibrationFile()
    {
        string desktop_path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string filename = desktop_path + "/calibration.xml";
        if (calibration_filename != filename)
        {
            if (File.Exists(filename)) File.Delete(filename);
            File.Copy(calibration_filename, filename);            
        }
    }

    /// <summary>
    /// saves the calibration image to the desktop so that it may be easily printed
    /// </summary>
    private void SaveCalibrationImage()
    {
        string desktop_path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string filename = desktop_path + "/calibration_pattern.bmp";
        Bitmap bmp = SurveyorCalibration.CreateDotPattern(1000, image_height * 1000 / image_width, SurveyorCalibration.dots_across, SurveyorCalibration.dot_radius_percent);
        bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);        
    }
	
    private void LoadManualCameraAlignmentCalibrationParameters(string filename)
    {
        StreamReader oRead = null;
        string str;
        bool filefound = true;

        try
        {
            oRead = File.OpenText(filename);
        }
        catch
        {
            filefound = false;
        }

        if (filefound)
        {
            str = oRead.ReadLine();
            if (str != null)
            {
                string left_image_filename = str;
            }

            str = oRead.ReadLine();
            if (str != null)
            {
                string right_image_filename = str;
            }

            str = oRead.ReadLine();
            if (str != null)
            {
                stereo_camera.offset_x = Convert.ToSingle(str);
            }

            str = oRead.ReadLine();
            if (str != null)
            {
                stereo_camera.offset_y = Convert.ToSingle(str);
            }

            str = oRead.ReadLine();
            if (str != null)
            {
                stereo_camera.scale = Convert.ToSingle(str);
            }

            str = oRead.ReadLine();
            if (str != null)
            {
                stereo_camera.rotation = Convert.ToSingle(str) * (float)Math.PI / 180.0f;
            }

            str = oRead.ReadLine();
            if (str != null)
            {
                float delay_mS = Convert.ToInt32(str);
            }

            oRead.Close();
        }
    }
	
	private void SaveImages(
	    string left_image_filename,
	    string right_image_filename)
	{
		byte[] left = new byte[image_width * image_height * 3];
		byte[] right = new byte[image_width * image_height * 3];
		GtkBitmap.getBitmap(leftimage, left);
		GtkBitmap.getBitmap(rightimage, right);
		Bitmap left_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		Bitmap right_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		BitmapArrayConversions.updatebitmap_unsafe(left, left_bmp);
		BitmapArrayConversions.updatebitmap_unsafe(right, right_bmp);
		if (left_image_filename.ToLower().EndsWith("png"))
		{
		    left_bmp.Save(left_image_filename, System.Drawing.Imaging.ImageFormat.Png);
            right_bmp.Save(right_image_filename, System.Drawing.Imaging.ImageFormat.Png);
		}
		if (left_image_filename.ToLower().EndsWith("bmp"))
		{
		    left_bmp.Save(left_image_filename, System.Drawing.Imaging.ImageFormat.Bmp);
            right_bmp.Save(right_image_filename, System.Drawing.Imaging.ImageFormat.Bmp);
		}
		if (left_image_filename.ToLower().EndsWith("gif"))
		{
		    left_bmp.Save(left_image_filename, System.Drawing.Imaging.ImageFormat.Gif);
            right_bmp.Save(right_image_filename, System.Drawing.Imaging.ImageFormat.Gif);
		}
		if (left_image_filename.ToLower().EndsWith("jpg"))
		{
		    left_bmp.Save(left_image_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            right_bmp.Save(right_image_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
		}
	}
	
	private void RunTweaks()
	{
        if ((leftimage != null) && (rightimage != null))
        {
			
            string left_filename = System.Environment.CurrentDirectory + "\\calib0.bmp";
            string right_filename = System.Environment.CurrentDirectory + "\\calib1.bmp";    
            SaveImages(left_filename, right_filename);
			
            if ((File.Exists(left_filename)) &&
                (File.Exists(right_filename)))
            {
				if (File.Exists(manual_camera_alignment_calibration_program))
				{
	                System.Diagnostics.Process proc = new System.Diagnostics.Process();
	                proc.EnableRaisingEvents = false;
	                proc.StartInfo.FileName = "mono " + manual_camera_alignment_calibration_program;
	                proc.StartInfo.Arguments = "-left " + '"' + left_filename + '"' + " ";
	                proc.StartInfo.Arguments += "-right " + '"' + right_filename + '"' + " ";
	                proc.StartInfo.Arguments += "-offsetx " + stereo_camera.offset_x.ToString() + " ";
	                proc.StartInfo.Arguments += "-offsety " + stereo_camera.offset_y.ToString() + " ";
	                proc.StartInfo.Arguments += "-scale " + stereo_camera.scale.ToString() + " ";
	                proc.StartInfo.Arguments += "-rotation " + (stereo_camera.rotation * 180 / (float)Math.PI).ToString();
					if (reverse_colours) proc.StartInfo.Arguments += " -reverse";
	                proc.Start();
	                proc.WaitForExit();
	
	                string params_filename = "manualoffsets_params.txt";
	                LoadManualCameraAlignmentCalibrationParameters(params_filename);										
					stereo_camera.baseline_mm = Convert.ToInt32(txtBaseline.Text);
	                stereo_camera.Save(calibration_filename);
				}
				else
				{
					Console.WriteLine(manual_camera_alignment_calibration_program + " could not be found");
				}
            }
            
        }
	}
	
	private void SaveAnimatedGif()
	{
        if ((leftimage != null) && (rightimage != null))
        {
			SaveImages("anim0.bmp", "anim1.bmp");
			
            if ((File.Exists("anim0.bmp")) &&
                (File.Exists("anim1.bmp")))
            {
                //List<string> images = new List<string>();
                //images.Add("anim0.gif");
                //images.Add("anim1.gif");

                GifCreator.CreateFromStereoPair("anim0.bmp", "anim1.bmp", "anim.gif", 1000, stereo_camera.offset_x, stereo_camera.offset_y, stereo_camera.scale, stereo_camera.rotation * 180 / (float)Math.PI, reverse_colours);
                //File.Delete("anim0.gif");
                //File.Delete("anim1.gif");
                //MessageBox.Show("Animated gif created");
            }
        }		
	}

    protected virtual void OnCmdSaveCalibrationImageClicked (object sender, System.EventArgs e)
    {
        SaveCalibrationImage();
    }

    protected virtual void OnCmdSaveCalibrationClicked (object sender, System.EventArgs e)
    {
        SaveCalibrationFile();
    }    	
	protected virtual void OnCmdTweaksClicked (object sender, System.EventArgs e)
    {
		RunTweaks();
	}    
    protected virtual void OnSaveAsAnimatedGifActionActivated (object sender, System.EventArgs e)
	{
	    SaveAnimatedGif();
	}    
		
	protected virtual void OnCalibrateAlignmentActionActivated (object sender, System.EventArgs e)
    {
        CalibrateAlignment();
    }
	
    protected virtual void OnManualTweaksActionActivated (object sender, System.EventArgs e)
	{
	    RunTweaks();
    }		
		
	protected virtual void OnChkDisableRectificationClicked (object sender, System.EventArgs e)
	{
        disable_rectification = !chkRectification.Active;
        stereo_camera.disable_rectification = disable_rectification;
	}    
		
	protected virtual void OnChkRadialCorrectionClicked (object sender, System.EventArgs e)
    {
		disable_radial_correction = !chkRadialCorrection.Active;
        stereo_camera.disable_radial_correction = disable_radial_correction;
    }
    	protected virtual void OnChkFlipLeftClicked (object sender, System.EventArgs e)
    {
        flip_left_image = chkFlipLeft.Active;
        stereo_camera.flip_left_image = chkFlipLeft.Active;
    }
    protected virtual void OnChkFlipRightClicked (object sender, System.EventArgs e)
    {
        flip_right_image = chkFlipRight.Active;
        stereo_camera.flip_right_image = chkFlipRight.Active;
    }
    
}
