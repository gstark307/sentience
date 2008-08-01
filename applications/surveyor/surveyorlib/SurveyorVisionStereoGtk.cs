/*
    Stereo vision for Surveyor robots
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
using System.Threading;
using System.Drawing;
using Gdk;
using sluggish.utilities.gtk;

namespace surveyor.vision
{    
    /// <summary>
    /// stereo vision object which displays images using Gtk
    /// </summary>
    public class SurveyorVisionStereoGtk : SurveyorVisionStereo
    {
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="host">host name or IP address</param>
        /// <param name="port_number_left">port number for the left camera</param>
        /// <param name="port_number_right">port number for the right camera</param>
        public SurveyorVisionStereoGtk(string host,
                                       int port_number_left,
                                       int port_number_right) : base (host, port_number_left, port_number_right)
        {
            display_image = new Gtk.Image[2];
        }
        
        #endregion
    
        #region "displaying images"
    
        // images used for display
        public Gtk.Image[] display_image;
        public Gtk.Window window;

        /// <summary>
        /// refreshes the GUI images
        /// </summary>
        /// <param name="window">Gtk window to be refreshed</param>
        private void UpdateGUI(Gtk.Window window)
        {            
            window.GdkWindow.ProcessUpdates(true);
        }
    
        /// <summary>
        /// shows both left and right camera images within the GUI
        /// </summary>
        /// <param name="left_image">left image bitmap</param>
        /// <param name="right_image">right image bitmap</param>
        protected override void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
         
            if (display_image[0] != null)
            {
                GtkBitmap.setBitmap(left_image, display_image[0]);
            }
            
            if (display_image[1] != null)
            {
                GtkBitmap.setBitmap(right_image, display_image[1]);

                // Here we need to update the GUI after receiving the right camera image
                // Since we're running in a separate thread from the GUI we have to
                // call it in a special way
                RunOnMainThread.Run(this, "UpdateGUI", new object[] { window });
            }
            
            if (window != null)
            {
                Gdk.Threads.Enter();
                window.QueueDraw();
                Gdk.Threads.Leave();
            }
        }
        
        #endregion
        
        #region "process images"
        
        public override void Process(Bitmap left_image, Bitmap right_image)
        {
            DisplayImages(left_image, right_image);
        }
        
        #endregion

    }
}
