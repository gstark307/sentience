using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using surveyor.vision;

namespace surveyorstereo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
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
                    leftport = Convert.ToInt32(leftport_str);
                }

                int rightport = 10002;
                string rightport_str = commandline.GetParameterValue("rightport", parameters);
                if (rightport_str != "")
                {
                    rightport = Convert.ToInt32(rightport_str);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmStereo(stereo_camera_IP, leftport, rightport));
            }
        }


        #region "validation"

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        [STAThread]
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
        [STAThread]
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