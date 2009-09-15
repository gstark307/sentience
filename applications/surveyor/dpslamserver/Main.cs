/*
    DP-SLAM Server
    Copyright (C) 2009 Bob Mottram
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
using dpslam.core;

namespace dpslam.server
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            // ensure that the program isn't run more than once
            bool mutex_ok;
            System.Threading.Mutex m = new System.Threading.Mutex(true, "dpslamserver", out mutex_ok);

            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            string forced = commandline.GetParameterValue("force", parameters);

            if ((!mutex_ok) && (forced == ""))
            {
                Console.WriteLine("This service is already running");
            }
            else
            {        
                string help_str = commandline.GetParameterValue("help", parameters);
                if (help_str != "")
                {
                    ShowHelp();
                }
                else
                {
                    int PortNumber = 1055;
                    string port_str = commandline.GetParameterValue("port", parameters);
                    if (port_str != "") PortNumber = Convert.ToInt32(port_str);
					
					int no_of_stereo_cameras = 1;
                    string no_of_stereo_cameras_str = commandline.GetParameterValue("cams", parameters);
					if (no_of_stereo_cameras_str != "")
					{
						no_of_stereo_cameras = Convert.ToInt32(no_of_stereo_cameras_str);
					}

                    Console.WriteLine("DP-SLAM Server version 0.1");
                    
                    bool Attached = false;
                    dpslamServer server = null;
                    
                    // create the server
	                server = new dpslamServer(no_of_stereo_cameras);
	                    
                    // start the server
                    server.Start(PortNumber);

                    while ((server.Running) && 
                           (server.kill == false))
                    {
                        System.Threading.Thread.Sleep(30);
                    }
                }
                
                // keeping the dream alive
                GC.KeepAlive(m);
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

            ValidParameters.Add("help");         // show help info
            ValidParameters.Add("port");         // motion control server port number
            ValidParameters.Add("force");

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
            Console.WriteLine("dpslamserver help");
            Console.WriteLine("-----------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  dpslamserver");
            Console.WriteLine("           -cams <number of stereo cameras>");
            Console.WriteLine("           -port <motion server port number>");
            Console.WriteLine("           -force  (don't check for multiple program instances)");
        }

        #endregion
    }
}