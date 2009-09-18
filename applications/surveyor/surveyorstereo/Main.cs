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
using System.Collections;
using Gtk;

namespace surveyor.vision
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());
			
            string help_str = commandline.GetParameterValue("help", parameters);
            if (help_str != "")
            {
                ShowHelp();
            }
            else
            {
			    string stereo_camera_IP = "169.254.0.10";
				string stereo_camera_IP_str = commandline.GetParameterValue("i", parameters);
				if (stereo_camera_IP_str != "")
				{
					stereo_camera_IP = stereo_camera_IP_str;
				}

				int leftport = 10001;
				string leftport_str = commandline.GetParameterValue("leftport", parameters);
				if (leftport_str != "")
				{
					leftport= Convert.ToInt32(leftport_str);
				}

				int rightport = 10002;
				string rightport_str = commandline.GetParameterValue("rightport", parameters);
				if (rightport_str != "")
				{
					rightport = Convert.ToInt32(rightport_str);
				}
			
	            Application.Init ();
	            MainWindow win = new MainWindow (stereo_camera_IP, leftport, rightport);
	            win.ShowAll ();
	        
			    Application.Run ();
				
			}
        }
		
        #region "validation"

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("help");  // show help info
            ValidParameters.Add("i");     // motion control server port number
            ValidParameters.Add("leftport");
            ValidParameters.Add("rightport");

            return (ValidParameters);
        }

        #endregion

        #region "help information"

        /// <summary>
        /// shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("surveyorstereo help");
            Console.WriteLine("-------------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  surveyorstereo");
            Console.WriteLine("    -i <IP address of the SVS>");
            Console.WriteLine("    -leftport <port number for the left camera>");
            Console.WriteLine("    -rightport <port number for the right camera>");
        }

        #endregion		
        
    }
}