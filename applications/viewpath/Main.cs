using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using sluggish.utilities;
using sentience.core;

namespace viewpath
{
    class MainProgram
    {
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
                Console.WriteLine("viewpath: creates images used to visualise occupancy grid data along a path");

                string steersman_filename = commandline.GetParameterValue("steer", parameters);
                if (steersman_filename == "")
                {
                    Console.WriteLine("Please provide a steersman filename");
                }
                else
                {
                    if (!File.Exists(steersman_filename))
                    {
                        Console.WriteLine("Cound't find steersman file");
                        Console.WriteLine(steersman_filename);
                    }
                    else
                    {
                        string path_filename = commandline.GetParameterValue("path", parameters);
                        if (path_filename == "")
                        {
                            Console.WriteLine("Please provide a path filename");
                        }
                        else
                        {
                            if (!File.Exists(path_filename))
                            {
                                Console.WriteLine("Cound't find path file");
                                Console.WriteLine(path_filename);
                            }
                            else
                            {

                                string disparities_filename = commandline.GetParameterValue("disp", parameters);
                                if (disparities_filename == "")
                                {
                                    Console.WriteLine("Please provide a disparities filename");
                                }
                                else
                                {

                                    if (!File.Exists(disparities_filename))
                                    {
                                        Console.WriteLine("Cound't find disparities file");
                                        Console.WriteLine(disparities_filename);
                                    }
                                    else
                                    {
                                        string disparities_index_filename = commandline.GetParameterValue("index", parameters);
                                        if (disparities_index_filename == "")
                                        {
                                            Console.WriteLine("Please provide a disparities index filename");
                                        }
                                        else
                                        {
                                            if (!File.Exists(disparities_index_filename))
                                            {
                                                Console.WriteLine("Cound't find disparities index file");
                                                Console.WriteLine(disparities_index_filename);
                                            }
                                            else
                                            {
                                                steersman visual_guidance = new steersman();
                                                if (visual_guidance.Load(steersman_filename))
                                                {
                                                    // load the path
                                                    visual_guidance.LoadPath(
                                                        path_filename,
                                                        disparities_index_filename,
                                                        disparities_filename);

                                                    string path_image_filename = "localise_along_path.jpg";
                                                    visual_guidance.ShowLocalisations(path_image_filename, 640, 480);
                                                    Console.WriteLine("Saved " + path_image_filename);
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Couldn't load steersman file");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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

            ValidParameters.Add("steer");
            ValidParameters.Add("help");
            ValidParameters.Add("path");
            ValidParameters.Add("disp");
            ValidParameters.Add("index");

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
            Console.WriteLine("viewpath help");
            Console.WriteLine("-------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  viewpath");
            Console.WriteLine("         -steer <steersman filename>");
            Console.WriteLine("         -path <path filename>");
            Console.WriteLine("         -disp <disparities filename>");
            Console.WriteLine("         -index <disparity indexes filename>");
        }

        #endregion		

    }
}
