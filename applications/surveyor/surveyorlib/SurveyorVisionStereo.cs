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
using System.Xml;
using System.IO;
using System.Threading;
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    public class SurveyorVisionStereo
    {
        private SurveyorVisionClient[] camera;
        private string host;
        private int[] port_number;
        public int fps = 10;

        public string device_name = "Surveyor stereo camera";
        public float focal_length_pixels;
        public float baseline_mm = 100;
        public float fov_degrees = 90;
        
        // whether to record raw camera images
        public bool Record;
        public ulong RecordFrameNumber;
        
        // whether to show the left or right image during calibration
        public bool show_left_image;
        public Bitmap calibration_pattern;
        
        public CalibrationSurvey[] calibration_survey;
        public int[][] calibration_map;
        public int[][,,] calibration_map_inverse;

        // offsets when observing objects at
        // long distance (less than one pixel disparity)
        public int offset_x=0, offset_y=0;
        
        // rotation of the right camera relative to the left
        public float rotation = 0;
        
        // rectified images
        public Bitmap[] rectified;
        
        // what type of image should be displayed
        public const int DISPLAY_RAW = 0;
        public const int DISPLAY_CALIBRATION_DOTS = 1;
        public const int DISPLAY_CALIBRATION_GRID = 2;
        public const int DISPLAY_CALIBRATION_DIFF = 3;
        public const int DISPLAY_RECTIFIED = 4;
        public int display_type = DISPLAY_CALIBRATION_GRID;
                
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="host">host name or IP address</param>
        /// <param name="port_number_left">port number for the left camera</param>
        /// <param name="port_number_right">port number for the right camera</param>
        public SurveyorVisionStereo(string host,
                                    int port_number_left,
                                    int port_number_right)
        {
            this.host = host;
            
            camera = new SurveyorVisionClient[2];
            for (int cam = 0; cam < 2; cam++)
            {
                camera[cam] = new SurveyorVisionClient();
                camera[cam].grab_mode = SurveyorVisionClient.GRAB_MULTI_CAMERA;
            }
            
            port_number = new int[2];
            port_number[0] = port_number_left;
            port_number[1] = port_number_right;
            
            calibration_survey = new CalibrationSurvey[2];
            calibration_map = new int[2][];
            calibration_map_inverse = new int[2][,,];
            calibration_survey[0] = new CalibrationSurvey();
            calibration_survey[1] = new CalibrationSurvey();
            
            rectified = new Bitmap[2];
            
            // delete and previously recorded images
            string[] victims = Directory.GetFiles(".", "raw*.jpg");
            if (victims != null)
            {
                for (int v = 0; v < victims.Length; v++)
                    File.Delete(victims[v]);
            }
        }
        
        #endregion
        

        #region "saving calibration data as Xml"

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <param name="device_name"></param>
        /// <param name="focal_length_pixels"></param>
        /// <param name="baseline_mm"></param>
        /// <param name="fov_degrees"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <param name="lens_distortion_curve"></param>
        /// <param name="centre_of_distortion_x"></param>
        /// <param name="centre_of_distortion_y"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="offset_x"></param>
        /// <param name="offset_y"></param>
        /// <returns></returns>
        private static XmlDocument getXmlDocument(
            string device_name,
            float focal_length_pixels,
            float baseline_mm,
            float fov_degrees,
            int image_width, int image_height,
            polynomial[] lens_distortion_curve,
            float[] centre_of_distortion_x, float[] centre_of_distortion_y,
            float rotation, float scale,
            float offset_x, float offset_y)
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeCalibration = doc.CreateElement("SurveyorCorporation");
            doc.AppendChild(nodeCalibration);

            XmlElement elem = getXml(doc, nodeCalibration,
                device_name,
                focal_length_pixels,
                baseline_mm,
                fov_degrees,
                image_width, image_height,
                lens_distortion_curve,
                centre_of_distortion_x, centre_of_distortion_y,
                rotation, scale,
                offset_x, offset_y);
            doc.DocumentElement.AppendChild(elem);

            return (doc);
        }

        /// <summary>
        /// save calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">filename to save as</param>
        public void Save(string filename)
        {
            if ((rectified[0] != null) &&
                (rectified[1] != null))
            {
                float scale = 1;
                int image_width = rectified[0].Width;
                int image_height = rectified[0].Height;
                polynomial[] lens_distortion_curve = new polynomial[2];
                float[] centre_of_distortion_x = new float[2];
                float[] centre_of_distortion_y = new float[2];
                for (int cam = 0; cam < 2; cam++)
                {
                    lens_distortion_curve[cam] = calibration_survey[cam].best_fit_curve;
                    centre_of_distortion_x[cam] = calibration_survey[cam].centre_of_distortion_x;
                    centre_of_distortion_y[cam] = calibration_survey[cam].centre_of_distortion_y;
                }
            
                XmlDocument doc =
                    getXmlDocument(
                        device_name,
                        focal_length_pixels,
                        baseline_mm,
                        fov_degrees,
                        image_width, image_height,
                        lens_distortion_curve,
                        centre_of_distortion_x, centre_of_distortion_y,
                        rotation, scale,
                        offset_x, offset_y);

                doc.Save(filename);
            }
        }

        private static XmlElement getXml(
            XmlDocument doc, XmlElement parent,
            string device_name,
            float focal_length_pixels,
            float baseline_mm,
            float fov_degrees,
            int image_width, int image_height,
            polynomial[] lens_distortion_curve,
            float[] centre_of_distortion_x, float[] centre_of_distortion_y,
            float rotation, float scale,
            float offset_x, float offset_y)
        {
            // make sure that floating points are saved in a standard format
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");

            // camera parameters
            XmlElement nodeStereoCamera = doc.CreateElement("StereoCamera");
            parent.AppendChild(nodeStereoCamera);

            if ((device_name != null) && (device_name != ""))
            {
                xml.AddComment(doc, nodeStereoCamera, "Name of the camera device");
                xml.AddTextElement(doc, nodeStereoCamera, "DeviceName", device_name);
            }

            xml.AddComment(doc, nodeStereoCamera, "Focal length in pixels");
            xml.AddTextElement(doc, nodeStereoCamera, "FocalLengthPixels", Convert.ToString(focal_length_pixels, format));

            xml.AddComment(doc, nodeStereoCamera, "Camera baseline distance in millimetres");
            xml.AddTextElement(doc, nodeStereoCamera, "BaselineMillimetres", Convert.ToString(baseline_mm, format));

            xml.AddComment(doc, nodeStereoCamera, "Calibration Data");

            XmlElement nodeCalibration = doc.CreateElement("Calibration");
            nodeStereoCamera.AppendChild(nodeCalibration);

            string offsets = Convert.ToString(offset_x, format) + "," +
                             Convert.ToString(offset_y, format);
            xml.AddComment(doc, nodeCalibration, "Image offsets in pixels due to small missalignment from parallel");
            xml.AddTextElement(doc, nodeCalibration, "Offsets", offsets);

            xml.AddComment(doc, nodeCalibration, "Scaling factor");
            xml.AddTextElement(doc, nodeCalibration, "Scale", Convert.ToString(scale));

            xml.AddComment(doc, nodeCalibration, "Rotation of the right image relative to the left in degrees");
            xml.AddTextElement(doc, nodeCalibration, "RelativeRotationDegrees", Convert.ToString(rotation / (float)Math.PI * 180.0f));

            for (int cam = 0; cam < lens_distortion_curve.Length; cam++)
            {
                XmlElement elem = getCameraXml(
                    doc, fov_degrees,
                    image_width, image_height,
                    lens_distortion_curve[cam],
                    centre_of_distortion_x[cam], centre_of_distortion_y[cam]);
                nodeCalibration.AppendChild(elem);
            }

            return (nodeStereoCamera);
        }


        /// <summary>
        /// return an xml element containing camera calibration parameters
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="fov_degrees"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <param name="lens_distortion_curve"></param>
        /// <param name="centre_of_distortion_x"></param>
        /// <param name="centre_of_distortion_y"></param>
        /// <returns></returns>
        private static XmlElement getCameraXml(
            XmlDocument doc,
            float fov_degrees,
            int image_width, int image_height,
            polynomial lens_distortion_curve,
            float centre_of_distortion_x, float centre_of_distortion_y)
        {
            // make sure that floating points are saved in a standard format
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");

            string coefficients = "";
            if (lens_distortion_curve != null)
            {
                int degree = lens_distortion_curve.GetDegree();
                for (int i = 0; i <= degree; i++)
                {
                    coefficients += Convert.ToString(lens_distortion_curve.Coeff(i), format);
                    if (i < degree) coefficients += ",";
                }
            }
            else coefficients = "0,0,0";

            XmlElement elem = doc.CreateElement("Camera");
            doc.DocumentElement.AppendChild(elem);
            xml.AddComment(doc, elem, "Horizontal field of view of the camera in degrees");
            xml.AddTextElement(doc, elem, "FieldOfViewDegrees", Convert.ToString(fov_degrees, format));
            xml.AddComment(doc, elem, "Image dimensions in pixels");
            xml.AddTextElement(doc, elem, "ImageDimensions", Convert.ToString(image_width, format) + "," + Convert.ToString(image_height, format));
            xml.AddComment(doc, elem, "The centre of distortion in pixels");
            xml.AddTextElement(doc, elem, "CentreOfDistortion", Convert.ToString(centre_of_distortion_x, format) + "," + Convert.ToString(centre_of_distortion_y, format));
            xml.AddComment(doc, elem, "Polynomial coefficients used to describe the camera lens distortion");
            xml.AddTextElement(doc, elem, "DistortionCoefficients", coefficients);
            xml.AddComment(doc, elem, "The minimum RMS error between the distortion curve and plotted points");
            xml.AddTextElement(doc, elem, "RMSerror", Convert.ToString(lens_distortion_curve.GetRMSerror(), format));

            return (elem);
        }


        #endregion        

        public bool CalibrateFocus()
        {
            bool done = false;
            if (rectified != null)
            {
                if ((rectified[0] != null) &&
                    (rectified[1] != null))
                {
                    SurveyorCalibration.UpdateOffsets(rectified[0], rectified[1],
                                                      ref offset_x, ref offset_y,
                                                      ref rotation);
                    done = true;
                }
            }
            return(done);
        }
        
        /// <summary>
        /// rectifies the given images
        /// </summary>
        /// <param name="left_image">left image bitmap</param>
        /// <param name="right_image">right image bitmap</param>
        protected void RectifyImages(Bitmap left_image, Bitmap right_image)
        {
            for (int cam = 0; cam < 2; cam++)
            {
                Bitmap bmp = left_image;
                if (cam == 1) bmp = right_image;
                
                if (calibration_survey[cam] != null)
                {
                    polynomial distortion_curve = calibration_survey[cam].best_fit_curve;
                    if (distortion_curve != null)
                    {
                        if (calibration_map[cam] == null)
                        {
                            SurveyorCalibration.updateCalibrationMap(
                                bmp.Width, bmp.Height, distortion_curve,
                                1, 0, 
                                calibration_survey[cam].centre_of_distortion_x,calibration_survey[cam].centre_of_distortion_y,
                                ref calibration_map[cam], ref calibration_map_inverse[cam]);
                        }
                        
                        byte[] img = new byte[bmp.Width * bmp.Height * 3];
                        BitmapArrayConversions.updatebitmap(bmp, img);
                        byte[] img_rectified = (byte[])img.Clone();                        
                    
                        int n = 0;
                        int[] map = calibration_map[cam];
                        for (int i = 0; i < img.Length; i+=3, n++)
                        {
                            int index = map[n]*3;
                            for (int col = 0; col < 3; col++)
                                img_rectified[i+col] = img[index+col];
                        }
                        
                        if (rectified[cam] == null)
                            rectified[cam] = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapArrayConversions.updatebitmap_unsafe(img_rectified, rectified[cam]);
                    }
                }
            }
        }
        
        #region "callbacks"

        private bool busy_processing;

        /// <summary>
        /// both images have arrived and are awaiting processing
        /// </summary>
        /// <param name="state"></param>
        private void FrameGrabCallback(object state)
        {
            SurveyorVisionClient[] istate = (SurveyorVisionClient[])state;
            if ((istate[0].current_frame != null) && 
                (istate[1].current_frame != null))
            {
                if (!busy_processing)
                {
                    Bitmap left = istate[0].current_frame;
                    Bitmap right = istate[1].current_frame;
                    
                    busy_processing = true;

                    if (calibration_pattern != null)
                    {
                        hypergraph dots = null;
                        if (!show_left_image)
                            dots = SurveyorCalibration.DetectDots(left, ref edge_detector, calibration_survey[0], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[0]);
                        else
                            dots = SurveyorCalibration.DetectDots(right, ref edge_detector, calibration_survey[1], ref edges, ref linked_dots, ref grid, ref grid_diff, ref rectified[1]);
                    }
                    
                    RectifyImages(left, right);
                                                            
                    Process(left, right);
                    
                    // save images to file
                    if (Record)
                    {
                        RecordFrameNumber++;
                        left.Save("raw0_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        right.Save("raw1_" + RecordFrameNumber.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    
                    busy_processing = false;
                }
            }
        }

        #endregion
        
        #region "starting and stopping"
        
        private Thread sync_thread;
        public bool Running;
        
        public void Run()
        {
            bool cameras_started = true;
            
            // start running the cameras
            for (int cam = 0; cam < 2; cam++)
            {
                camera[cam].fps = fps;
                camera[cam].Start(host, port_number[cam]);
                if (camera[cam].Running)
                {
                    camera[cam].StartStream();
                }
                else
                {
                    cameras_started = false;
                    break;
                }
            }
            
            if (cameras_started)
            {
                // create a thread to send the master pulse
                SurveyorVisionThreadGrabFrameMulti grab_frames = new SurveyorVisionThreadGrabFrameMulti(new WaitCallback(FrameGrabCallback), camera);        
                sync_thread = new Thread(new ThreadStart(grab_frames.Execute));
                sync_thread.Priority = ThreadPriority.Normal;
                sync_thread.Start();   
                Running = true;
                Console.WriteLine("Stereo camera active on " + host);
            }
        }

        public void Stop()
        {
            if (Running)
            {
                for (int cam = 0; cam < 2; cam++)
                {
                    camera[cam].StopStream();
                    camera[cam].Stop();
                }

                
            }            
        }
        
        #endregion
        
        #region "getters and setters"
        
        public void SetFramesPerSecond(int fps)
        {
            this.fps = fps;
            camera[0].fps = fps;
            camera[1].fps = fps;
        }
        
        #endregion
        
        #region "edge detection"
                
        protected Bitmap edges;
        protected Bitmap linked_dots;
        protected Bitmap grid;
        protected Bitmap grid_diff;
        private EdgeDetectorCanny edge_detector;
        
        protected Bitmap DetectEdges(Bitmap bmp, EdgeDetectorCanny edge_detector,
                                     ref hypergraph dots)
        {
            byte[] image_data = new byte[bmp.Width * bmp.Height * 3];
            BitmapArrayConversions.updatebitmap(bmp, image_data);
            
            if (edge_detector == null) edge_detector = new EdgeDetectorCanny();
            edge_detector.automatic_thresholds = true;
            edge_detector.connected_sets_only = true;
            byte[] edges_data = edge_detector.Update(image_data, bmp.Width, bmp.Height);

            edges_data = edge_detector.GetConnectedSetsImage(image_data, 10, bmp.Width / SurveyorCalibration.dots_across * 3, true, ref dots);
            
            Bitmap edges_bmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(edges_data, edges_bmp);
            
            return(edges_bmp);
        }

        #endregion
        
        #region "displaying images"
       
        protected virtual void DisplayImages(Bitmap left_image, Bitmap right_image)
        {
        }

        #endregion
        
        #region "process images"

        public virtual void Process(Bitmap left_image, Bitmap right_image)
        {
            DisplayImages(left_image, right_image);
        }
        
        #endregion
        
    }
}
