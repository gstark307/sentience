/*
    demo client receiving stereo feature data
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
using surveyor.vision;
using sluggish.utilities;

namespace stereoclient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            string server = commandline.GetParameterValue("server", parameters);
            if (server == "")
            {
                Console.WriteLine("Please specify a server");
            }
            else
            {
                string port_str = commandline.GetParameterValue("port", parameters);
                if (port_str == "")
                {
                    Console.WriteLine("Please specify a port number");
                }
                else
                {
                    StereoVisionClient client = new StereoVisionClient();
                    client.Connect(server, Convert.ToInt32(port_str));
                    while (client.IsConnected())
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
        
        }
        
        
        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("server");
            ValidParameters.Add("port");

            return (ValidParameters);
        }
        
    }
    
}