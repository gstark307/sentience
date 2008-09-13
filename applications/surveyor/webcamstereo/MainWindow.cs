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
using Gtk;
using surveyor.vision;
using sluggish.utilities;
using sluggish.utilities.gtk;

public partial class MainWindow: Gtk.Window
{
    int image_width = 320;
    int image_height = 240;
    string left_camera_device = "/dev/video3";
    string right_camera_device = "/dev/video4";
    string calibration_filename = "calibration.xml";
    int broadcast_port = 10010;
    int fps = 1;
    int phase_degrees = 180;
         
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
        
        stereo_camera = new WebcamVisionStereoGtk(left_camera_device, right_camera_device, broadcast_port, fps, phase_degrees);
        stereo_camera.window = this;
        stereo_camera.display_image[0] = leftimage;
        stereo_camera.display_image[1] = rightimage;
        stereo_camera.Load(calibration_filename);
        stereo_camera.image_width = image_width;
        stereo_camera.image_height = image_height;        
        stereo_camera.Run();
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
        stereo_camera.stereo_algorithm_type = StereoVision.SIMPLE;
    }

    protected virtual void OnCmdDenseStereoClicked (object sender, System.EventArgs e)
    {
        stereo_camera.stereo_algorithm_type = StereoVision.DENSE;
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

}