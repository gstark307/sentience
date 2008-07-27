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
    
        protected override void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
         
            if (display_image[0] != null)
            {
                //left_image.Save("left.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                //display_image[0].Pixbuf = new Pixbuf("left.jpg");

                GtkBitmap.setBitmap(left_image, display_image[0]);

                Gdk.Threads.Enter();
                display_image[0].QueueDraw();
                display_image[0].Show();
                Gdk.Threads.Leave();

            }
            
            if (display_image[1] != null)
            {
                GtkBitmap.setBitmap(right_image, display_image[1]);

                Gdk.Threads.Enter();
                display_image[1].QueueDraw();
                display_image[1].Show();
                Gdk.Threads.Leave();
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
