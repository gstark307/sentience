
using System;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using sentience.core;
using sluggish.utilities;
using sluggish.utilities.xml;

namespace sentience.sensormodel
{
	class MainClass
	{
		public static void Main(string[] args)
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
                Console.WriteLine("sensormodel: stereo vision sensor modelling for dummies");
            
	            int no_of_cameras = 1;
                robot rob = new robot(no_of_cameras, robot.MAPPING_DPSLAM);
                        
                string filename = commandline.GetParameterValue("filename", parameters);
                if (filename == "") filename = "sensormodel.xml";
                
                float baseline_mm = 100;
                string baseline_mm_str = commandline.GetParameterValue("baseline", parameters);
                if (baseline_mm_str != "")
                {
                    baseline_mm = Convert.ToSingle(baseline_mm_str);
                }
                
                float camera_FOV_degrees = 78;
                string camera_FOV_degrees_str = commandline.GetParameterValue("fov", parameters);
                if (camera_FOV_degrees_str != "")
                {
                    camera_FOV_degrees = Convert.ToSingle(camera_FOV_degrees_str);
                }
                
                float LocalGridCellSize_mm = 32;
                string LocalGridCellSize_mm_str = commandline.GetParameterValue("cellsize", parameters);
                if (LocalGridCellSize_mm_str != "")
                {
                    LocalGridCellSize_mm = Convert.ToSingle(LocalGridCellSize_mm_str);
                }

                int image_width = 640;
                string image_width_str = commandline.GetParameterValue("imagewidth", parameters);
                if (image_width_str != "")
                {
                    image_width = Convert.ToInt32(image_width_str);
                }
                int image_height = image_width / 2;
                                                               		
	            for (int i = 0; i < rob.head.no_of_stereo_cameras; i++)
	            {
	                rob.head.calibration[i].baseline = baseline_mm;
	                rob.head.calibration[i].leftcam.camera_FOV_degrees = camera_FOV_degrees;
	                rob.head.calibration[i].rightcam.camera_FOV_degrees = camera_FOV_degrees;
	                rob.head.calibration[i].positionOrientation.roll = 0;
	                rob.head.calibration[i].leftcam.image_width = 640;
                    rob.head.calibration[i].leftcam.image_height = 480;
	            }
	            
	            if (File.Exists(filename))
	            {
	                Console.WriteLine("Loading " + filename);
	                rob.head.sensormodel = new rayModelLookup[no_of_cameras];
	                rob.head.sensormodel[0] = new rayModelLookup(image_width/2, 4000);
	                rob.head.sensormodel[0].Load(filename);
	            }
	            
	            // create the sensor models
	            Console.Write("Creating sensor models, please wait...");
                rob.inverseSensorModel.createLookupTables(rob.head, (int)LocalGridCellSize_mm);
                Console.WriteLine("Done");
                
                string raymodel_image_filename = "ray_model.jpg";
                Console.WriteLine("Saving ray model image: " + raymodel_image_filename);
                int img_width = 640;
                int img_height = 480;
                byte[] img = new byte[img_width * img_height *3];
                rob.inverseSensorModel.ray_model_to_graph_image(img, img_width, img_height);
                Bitmap bmp = new Bitmap(img_width, img_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
                bmp.Save(raymodel_image_filename, System.Drawing.Imaging.ImageFormat.Jpeg);

                string distribution_image_filename = "gaussian_distribution.jpg";
                Console.WriteLine("Saving gaussian distribution image: " + distribution_image_filename);
                rob.inverseSensorModel.showDistribution(img, img_width, img_height);
                BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
                bmp.Save(distribution_image_filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                
                Console.WriteLine("Saving " + filename);
                rob.Save(filename);
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

            ValidParameters.Add("help");
            ValidParameters.Add("filename");
            ValidParameters.Add("baseline");
            ValidParameters.Add("fov");
            ValidParameters.Add("cellsize");            
            ValidParameters.Add("imagewidth");

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
            Console.WriteLine("sensormodel help");
            Console.WriteLine("----------------");
            Console.WriteLine("");
            Console.WriteLine("Syntax:  sensormodel");
            Console.WriteLine("         -filename <xml filename to be generated>");
            Console.WriteLine("         -baseline <stereo camera baseline in mm>");
            Console.WriteLine("         -fov <camera field of view in degrees>");
            Console.WriteLine("         -cellsize <size of each grid cell in mm>");
            Console.WriteLine("         -imagewidth <width of the camera image in pixels>");
        }

        #endregion		
	}
}