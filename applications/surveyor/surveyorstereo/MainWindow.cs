/*
    Test GUI
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
using Gtk;
using surveyor.vision;
using sluggish.utilities.gtk;

public partial class MainWindow: Gtk.Window
{    
    public SurveyorVisionStereoGtk stereo_camera;
    
    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        Build ();
        
        stereo_camera = new SurveyorVisionStereoGtk("169.254.0.10", 10001, 10002);
        stereo_camera.window = this;
        stereo_camera.display_image[0] = leftimage;
        stereo_camera.display_image[1] = rightimage;
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
        
}