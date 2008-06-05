/*
    Fishfood stereo camera calibration utility
    Copyright (C) 2000-2008 Bob Mottram
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
using System.Collections;
using System.Collections.Generic;

namespace sentience.calibration
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Test();
            
            
            // extract command line parameters
            ArrayList parameters = commandline.ParseCommandLineParameters(args, "-", GetValidParameters());

            string help = commandline.GetParameterValue("help", parameters);
            if (help != "")
            {
                ShowHelp();
            }
            else
            {
                string directory = commandline.GetParameterValue("dir", parameters);
                if (directory == "")
                {
                    Console.WriteLine("Please specify a directory for the calibration files");
                }
                else
                {
                    if (!Directory.Exists(directory))
                    {
                        Console.WriteLine("The directory " + directory + " was not found");
                    }
                    else
                    {
                        string baseline_str = commandline.GetParameterValue("baseline", parameters);
                        if (baseline_str == "")
                        {
                            Console.WriteLine("Please specify a baseline distance for the camera separation in millimetres");
                        }
                        else
                        {
                            int baseline_mm = Convert.ToInt32(baseline_str);

                            string dotdist_str = commandline.GetParameterValue("dotdist", parameters);
                            if (dotdist_str == "")
                            {
                                Console.WriteLine("Please specify the horizontal distance to the calibration pattern dot in millimetres");
                            }
                            else
                            {
                                int dotdist_mm = Convert.ToInt32(dotdist_str);

                                string height_str = commandline.GetParameterValue("height", parameters);
                                if (height_str == "")
                                {
                                    Console.WriteLine("Please specify the height above the calibration pattern in millimetres");
                                }
                                else
                                {
                                    int height_mm = Convert.ToInt32(height_str);
                                    
                                    string fov_str = commandline.GetParameterValue("fov", parameters);
                                    if (fov_str == "")
                                    {
                                        Console.WriteLine("Please specify the camera horizontal field of view in degrees");
                                    }
                                    else
                                    {
                                        float fov_degrees = Convert.ToSingle(fov_str);
                                    
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        #region "calibration"
        
        private static void Calibrate(string directory,
                               int baseline_mm,
                               int dotdist_mm,
                               int height_mm,
                               float fov_degrees,
                               string file_extension)
        {
            string[] filename = Directory.GetFiles(directory, "*." + file_extension);
            if (filename != null)
            {
                // two lists to store filenames for camera 0 and camera 1
                List<string>[] image_filename = new List<string>[2];
                image_filename[0] = new List<string>();
                image_filename[1] = new List<string>();
                
                // populate the lists
                for (int i = 0; i < filename.Length; i++)
                {
                    if (filename[i].StartsWith("raw0"))
                        image_filename[0].Add(filename[i]);
                    if (filename[i].StartsWith("raw1"))
                        image_filename[1].Add(filename[i]);
                }
                
                if (image_filename[0].Count == 0)
                {
                    Console.WriteLine("Did not find any calibration files.  Do they begin with raw0 or raw1 ?");
                }
                else
                {
                    if (image_filename[0].Count != image_filename[1].Count)
                    {
                        Console.WriteLine("There must be the same number of images from camera 0 and camera 1");
                    }
                    else
                    {
                        // find dots within the images
                        for (int cam = 0; cam < 2; cam++)
                        {
                            for (int i = 0; i < image_filename[cam].Count; i++)
                            {
                                hypergraph dots = DetectDots(image_filename[cam][i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No calibration " + file_extension + " images were found");
            }
        }
        
        #endregion

        private static void Test()
        {
            string filename = "/home/motters/calibrationdata/forward2/raw0_5000_2000.jpg";
            int grouping_radius_percent = 2;
            int erosion_dilation = 0;
            float minimum_width = 0.0f;
            float maximum_width = 10;
            List<int> edges = null;
            
            List<calibrationDot> dot_shapes = shapes.DetectDots(filename, grouping_radius_percent, erosion_dilation,
                                                                minimum_width, maximum_width,
                                                                ref edges, "edges.jpg", "groups.jpg", "dots.jpg");            
        }

        
        #region "detecting dots"
        
        private static hypergraph DetectDots(string filename)
        {
            hypergraph dots = new hypergraph();
            int grouping_radius_percent = 3;
            int erosion_dilation = 0;
            int minimum_width = 2;
            int maximum_width = 100;
            List<int> edges = null;
            
            List<calibrationDot> dot_shapes = shapes.DetectDots(filename, grouping_radius_percent, erosion_dilation,
                                                                minimum_width, maximum_width,
                                                                ref edges, "edges.jpg", "groups.jpg", "dots.jpg");
            
            float tx = 99999;
            float ty = 99999;
            float bx = 0;
            float by = 0;
            for (int i = 0; i < dot_shapes.Count; i++)
            {
                if (dot_shapes[i].x < tx) tx = dot_shapes[i].x;
                if (dot_shapes[i].y < ty) ty = dot_shapes[i].y;
                if (dot_shapes[i].x > bx) bx = dot_shapes[i].x;
                if (dot_shapes[i].y > by) by = dot_shapes[i].y;
                dots.Add(dot_shapes[i]);
            }
            
            Console.WriteLine(dot_shapes.Count.ToString() + " dots discovered");

            // create a histogram of the spacings between dots            
            int distance_quantisation = 5;
            if (bx - tx > distance_quantisation)
            {
                float[] dot_spacing_histogram = new float[(int)(bx-tx) / distance_quantisation];
                for (int i = 0; i < dot_shapes.Count; i++)
                {
                    for (int j = 0; j < dot_shapes.Count; j++)
                    {
                        if (i != j)
                        {
                            float dx = dot_shapes[i].x - dot_shapes[j].x;
                            float dy = dot_shapes[i].y - dot_shapes[j].y;
                            float dist = (float)Math.Sqrt((dx*dx)+(dy*dy)) / distance_quantisation;
                            if ((int)dist < dot_spacing_histogram.Length)
                                dot_spacing_histogram[(int)dist]++;
                        }
                    }
                }
                
                // find the histogram peak
                float histogram_max = 0;
                float typical_dot_separation = 0;
                for (int i = 0; i < dot_spacing_histogram.Length; i++)
                {
                    if (dot_spacing_histogram[i] > histogram_max)
                    {
                        histogram_max = dot_spacing_histogram[i];
                        typical_dot_separation = i * distance_quantisation;
                    }
                }
                
                // join dots within the typical separation radius
                int joins = 0;
                float max_separation = typical_dot_separation * 1.2f;
                for (int i = 0; i < dot_shapes.Count; i++)
                {
                    for (int j = i+1; j < dot_shapes.Count; j++)
                    {
                        if (i != j)
                        {
                            float dx = dot_shapes[i].x - dot_shapes[j].x;
                            float dy = dot_shapes[i].y - dot_shapes[j].y;
                            float dist = (float)Math.Sqrt((dx*dx)+(dy*dy));
                            if (dist < max_separation)
                            {
                                dots.LinkByIndex(i, j);
                                dots.LinkByIndex(j, i);
                                joins++;
                            }
                        }
                    }
                }
                
                Console.WriteLine(joins.ToString() + " dots joined");
                                
                // remove crossed links
                List<hypergraph_link> victims = new List<hypergraph_link>();
                for (int i = 0; i < dots.Links.Count; i++)
                {
                    hypergraph_link link1 = dots.Links[i];
                    calibrationDot link1_from = (calibrationDot)link1.From;
                    calibrationDot link1_to = (calibrationDot)link1.To;
                    float x0 = link1_from.x;
                    float y0 = link1_from.y;
                    float x1 = link1_to.x;
                    float y1 = link1_to.y;
                    for (int j = i + 1; j < dots.Links.Count; j++)
                    {
                        hypergraph_link link2 = dots.Links[j];
                        if ((link2.From != link1.From) &&
                            (link2.From != link1.To) &&
                            (link2.To != link1.From) &&
                            (link2.To != link1.To))
                        {
                            calibrationDot link2_from = (calibrationDot)link2.From;
                            calibrationDot link2_to = (calibrationDot)link2.To;
                            float x2 = link2_from.x;
                            float y2 = link2_from.y;
                            float x3 = link2_to.x;
                            float y3 = link2_to.y;
                            
                            float ix = 0, iy = 0;
                            if (geometry.intersection(x0,y0,x1,y1, x2,y2,x3,y3, ref ix, ref iy))
                            {
                                if (!victims.Contains(link1)) victims.Add(link1);
                                if (!victims.Contains(link2)) victims.Add(link2);
                            }
                        }
                    }
                }
                
                dots.RemoveLinks(victims);
                
                Console.WriteLine(victims.Count.ToString() + " crossed links removed");
            }
                
            return(dots);
        }
        
        #endregion
        
        #region "validation"

        /// <summary>
        /// returns a list of valid parameter names
        /// </summary>
        /// <returns>list of valid parameter names</returns>
        private static ArrayList GetValidParameters()
        {
            ArrayList ValidParameters = new ArrayList();

            ValidParameters.Add("help");         // show help info
            ValidParameters.Add("dir");          // directory where calibration files are stored
            ValidParameters.Add("baseline");     // baseline distance between the cameras
            ValidParameters.Add("dotdist");      // horizontal distance to the calibration pattern dot
            ValidParameters.Add("height");       // height above the calibration pattern in millimetres
            ValidParameters.Add("fov");          // field of view in degrees

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
            Console.WriteLine("fishfood Help");
            Console.WriteLine("-------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  fishfood");
            Console.WriteLine("                  -dir <calibration files directory>");
            Console.WriteLine("                  -baseline <stereo camera baseline in millimetres>");
            Console.WriteLine("                  -dotdist <horizontal distance to the calibration pattern dot in millimetres>");
            Console.WriteLine("                  -height <height above the calibration pattern in millimetres>");
            Console.WriteLine("                  -fov <horizontal field of view in degrees>");
        }

        #endregion
    }
}