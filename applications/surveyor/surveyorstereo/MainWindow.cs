/*
    Test GUI for the Surveyor stereo camera
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
using Gtk;
using surveyor.vision;
using sluggish.utilities;
using sluggish.utilities.gtk;

public partial class MainWindow: Gtk.Window
{
    int image_width = 320;
    int image_height = 240;
    string stereo_camera_IP = "169.254.0.10";
    string calibration_filename = "calibration.xml";
    int broadcast_port = 10010;
    int fps = 5;
    string log_path = Environment.CurrentDirectory;
	string teleoperation_log = "teleop.dat";
    string temporary_files_path = "";
    string recorded_images_path = "";
	string zip_utility = "tar";
	string path_identifier = "log";
	string replay_path_identifier = "log";

	string manual_camera_alignment_calibration_program = "calibtweaks.exe";
    //bool disable_rectification = true; // false;
    //bool disable_radial_correction = true;
	bool reverse_colours = true;
	bool motors_active;
	int motors_tries = 5;
	bool starting = true;
	
    public SurveyorVisionStereoGtk stereo_camera;
    
    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        Build ();
		
		//SaveHpolarLookup();

        byte[] img = new byte[image_width * image_height * 3];
        Bitmap left_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        Bitmap right_bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        BitmapArrayConversions.updatebitmap_unsafe(img, left_bmp);
        BitmapArrayConversions.updatebitmap_unsafe(img, right_bmp);
        GtkBitmap.setBitmap(left_bmp, leftimage);
        GtkBitmap.setBitmap(right_bmp, rightimage);
        
        stereo_camera = new SurveyorVisionStereoGtk(stereo_camera_IP, 10001, 10002, broadcast_port, fps, this);
        stereo_camera.temporary_files_path = temporary_files_path;
        stereo_camera.recorded_images_path = recorded_images_path;
        stereo_camera.display_image[0] = leftimage;
        stereo_camera.display_image[1] = rightimage;
        stereo_camera.Run();
		
		txtLogging.Text = path_identifier;
		txtReplay.Text = replay_path_identifier;
		
		// enable motors
		SendCommand("M");
				
		motors_active = true;
		starting = false;
    }

    private void SaveHpolarLookup()
	{
        int cartesian_dimension_cells_width = 40; //40;
        int cartesian_dimension_cells_range = 40; //40;
        float cartesian_cell_size_mm = 100;
        int[] HpolarLookup = null;
		float scale_down_factor = 10;
		Hpolar.CreateHpolarLookupSRV(
		    cartesian_dimension_cells_width,
		    cartesian_dimension_cells_range,
		    cartesian_cell_size_mm,
		    scale_down_factor,
		    ref HpolarLookup);
		Hpolar.ShowHpolar(
		    cartesian_dimension_cells_width, 
		    cartesian_dimension_cells_range, 
		    cartesian_cell_size_mm, 
		    HpolarLookup, 
		    1000, 
		    scale_down_factor, 
		    "Hpolar.jpg");
		Hpolar.SaveHpolar(HpolarLookup, "HpolarLookup.txt");
		Hpolar.SaveTrigLookup("TrigLookup.txt");
	}
    
    private void CloseForm()
    {
        stereo_camera.Stop();
        Application.Quit ();
    }
    
    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
		// stop recording
		if (stereo_camera.Record) ToggleLogging(false);
		
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
			//Console.WriteLine("show dots");
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

    protected virtual void OnCmdCalibrateAlignmentClicked (object sender, System.EventArgs e)
    {
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

    protected virtual void OnCmdSimpleStereoClicked (object sender, System.EventArgs e)
    {		
        stereo_camera.stereo_algorithm_type = StereoVision.EDGES;
        stereo_camera.Load(calibration_filename);		
    }

    protected virtual void OnCmdDenseStereoClicked (object sender, System.EventArgs e)
    {
        stereo_camera.stereo_algorithm_type = StereoVision.DENSE;
		stereo_camera.Load(calibration_filename);
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

    protected virtual void OnCmdSaveCalibrationImageClicked (object sender, System.EventArgs e)
    {
        SaveCalibrationImage();
    }

    protected virtual void OnCmdSaveCalibrationClicked (object sender, System.EventArgs e)
    {
        SaveCalibrationFile();
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
                stereo_camera.Save(calibration_filename);
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
	protected virtual void OnCmdTweaksClicked (object sender, System.EventArgs e)
	{
	    RunTweaks();
    }
		protected virtual void OnCmdAnimatedGifClicked (object sender, System.EventArgs e)
	{
	    SaveAnimatedGif();
	}
		    protected virtual void OnChkEmbeddedClicked (object sender, System.EventArgs e)
	{
		if (stereo_camera != null)
	    {
		    if (chkEmbedded.Active) 
			    stereo_camera.EnableEmbeddedStereo();
			else
	            stereo_camera.DisableEmbeddedStereo();
	    }
	}
		protected virtual void OnCmdForwardLeftClicked (object sender, System.EventArgs e)
	{
	    SendCommand("7");
	}
			protected virtual void OnCmdForwardClicked (object sender, System.EventArgs e)
	{
	    SendCommand("8");
	}	
	protected virtual void OnCmdForwardRightClicked (object sender, System.EventArgs e)
	{
	    SendCommand("9");
	}
			protected virtual void OnCmdLeftClicked (object sender, System.EventArgs e)
    {
        SendCommand("4");
	}
		    protected virtual void OnCmdStopClicked (object sender, System.EventArgs e)
	{
	    SendCommand("5");
	}
	protected virtual void OnCmdRightClicked (object sender, System.EventArgs e)
	{
	    SendCommand("6");
    }
			protected virtual void OnCmdBackLeftClicked (object sender, System.EventArgs e)
    {
        SendCommand("1");
	}
			protected virtual void OnCmdBackRightClicked (object sender, System.EventArgs e)
	{
        SendCommand("3");
	}
	protected virtual void OnCmdBackClicked (object sender, System.EventArgs e)
    {
        SendCommand("2");
    }
    protected virtual void OnCmdFastClicked (object sender, System.EventArgs e)
	{
	    SendCommand("+");
    }
	protected virtual void OnCmdSlowClicked (object sender, System.EventArgs e)
	{
	    SendCommand("-");
    }
		    protected virtual void OnCmdAvoidClicked (object sender, System.EventArgs e)
	{
	    SendCommand("F");
	}
		    protected virtual void OnCmdCrashClicked (object sender, System.EventArgs e)
	{
	    SendCommand("f");
    }
    protected virtual void OnCmdLaserOnClicked (object sender, System.EventArgs e)
	{
	    SendCommand("l");
    }
    protected virtual void OnCmdSpinRightClicked (object sender, System.EventArgs e)
    {
        SendCommand(".");
    }
		    protected virtual void OnCmdSpinLeftClicked (object sender, System.EventArgs e)
    {
        SendCommand("0");
	}
    protected virtual void OnCmd160x128Clicked (object sender, System.EventArgs e)
	{
    }
			protected virtual void OnCmd320x256Clicked (object sender, System.EventArgs e)
	{
    }
		protected virtual void OnCmd640x512Clicked (object sender, System.EventArgs e)
	{
	}
		protected virtual void OnCmd1280x1024Clicked (object sender, System.EventArgs e)
	{
    }
		 	protected virtual void OnCmdLaserOffClicked (object sender, System.EventArgs e)
	{
        SendCommand("L");
    }
			protected virtual void OnCmdForwardActivated (object sender, System.EventArgs e)
	{
        SendCommand("8");
	}    
    
    protected virtual void OnCmdBackActivated (object sender, System.EventArgs e)    
    {
        SendCommand("2");
    }
    
    private void SendCommand(string command_str)
    {
        if (stereo_camera.Record) BaseVisionStereo.LogEvent(DateTime.Now, command_str, teleoperation_log);
	    stereo_camera.SendCommand(0, command_str);
    }
    			protected virtual void OnCmdStopActivated (object sender, System.EventArgs e)
			    {
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
        protected virtual void OnChkLoggingClicked (object sender, System.EventArgs e)
    {
        ToggleLogging(chkLogging.Active);
    }
        protected virtual void OnCmdReplayClicked (object sender, System.EventArgs e)
    {
        replay_path_identifier = txtReplay.Text;
        if ((replay_path_identifier != null) &&
            (replay_path_identifier != ""))
        {
            lblReplayState.Text = "Playing";
            stereo_camera.RunReplay(teleoperation_log, "tar", log_path, replay_path_identifier);
        }
    }
        protected virtual void OnCmdReplayStopClicked (object sender, System.EventArgs e)
    {
        stereo_camera.StopReplay();
        lblReplayState.Text = "";
    }




		        }