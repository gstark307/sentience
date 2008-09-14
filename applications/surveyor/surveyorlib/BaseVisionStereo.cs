/*
    Base class for stereo vision
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
    public class BaseVisionStereo
    {
        protected int broadcast_port_number;
        public float fps = 10;
        public int phase_degrees;

        public string device_name;
        public string part_number;
        public string serial_number;
        public int image_width, image_height;
        public float focal_length_pixels = 1105.2f;
        public float focal_length_mm = 3.6f;    // 90 degree FOV lens
        public float pixels_per_mm = 307;       // density of pixels on the sensor
        public float baseline_mm = 100;
        public float fov_degrees = 90;
        public int stereo_algorithm_type = StereoVision.SIMPLE;
        protected int prev_stereo_algorithm_type;
        protected bool broadcasting;
        
        public BaseVisionStereo next_camera;
        public bool active_camera = true;
        
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
        public float offset_x=0, offset_y=0;
        
        // rotation of the right camera relative to the left
        public float rotation = 0;

        // if true this performs stereo correspondence calculations
        // only when other applications are connected and ready to
        // receive the results
        public bool UpdateWhenClientsConnected;
        
        // rectified images
        public Bitmap[] rectified;
        
        // Select rows at random from which to obtain disparities
        // this helps to reduce processing time
        // If this value is set to zero then all rows of the image are considered
        public int random_rows;
        
        public int exposure = 50;
        
        // what type of image should be displayed
        public const int DISPLAY_RAW = 0;
        public const int DISPLAY_CALIBRATION_DOTS = 1;
        public const int DISPLAY_CALIBRATION_GRID = 2;
        public const int DISPLAY_CALIBRATION_DIFF = 3;
        public const int DISPLAY_RECTIFIED = 4;
        public const int DISPLAY_STEREO_SIMPLE = 5;
        public const int DISPLAY_DIFFERENCE = 6;
        public int display_type = DISPLAY_CALIBRATION_GRID;
                
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="broadcast_port">port number on which to broadcast stereo feature data to other applications</param>
        /// <param name="fps">ideal frames per second</param>
        /// <param name="phase_degrees">frame capture phase offset</param>
        public BaseVisionStereo(int broadcast_port, float fps, int phase_degrees)
        {
            this.fps = fps;
            this.phase_degrees = phase_degrees;
        
            broadcast_port_number = broadcast_port;
            
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
        
        #region "automatic exposure adjustment"
        
        private float[] intensity_histogram = new float[256];
        public int exposure_deadband = 10;
        
        /// <summary>
        /// calculate mean light value from intensity histogram
        /// </summary>
        /// <param name="histogram">histogram data</param>
        private unsafe float GetMeanLight(float[] histogram)
        {
            float MeanDark=0, MeanLight=0;
            float MinVariance = 999999;  // some large figure
            float currMeanDark, currMeanLight, VarianceDark, VarianceLight;

            float DarkHits = 0;
            float LightHits = 0;
            int histlength = histogram.Length;

            // calculate squared magnitudes
            // this avoids unnecessary multiplications later on
            float[] histogram_squared_magnitude = new float[histogram.Length];
            for (int i = 0; i < histogram.Length; i++)
                histogram_squared_magnitude[i] = histogram[i] * histogram[i];

            //const float threshold_increment = 0.25f;
            const int max_grey_levels = 255;

            int h, bucket;
            float magnitude_sqr, Variance, divisor;

            fixed (float* unsafe_histogram = histogram_squared_magnitude)
            {
                // evaluate all possible thresholds
                for (int grey_level = max_grey_levels - 1; grey_level >= 0; grey_level--)
                {
                    // compute mean and variance for this threshold
                    // in a struggle between light and darkness
                    DarkHits = 0;
                    LightHits = 0;
                    currMeanDark = 0;
                    currMeanLight = 0;
                    VarianceDark = 0;
                    VarianceLight = 0;

                    bucket = grey_level;
                    for (h = histlength - 1; h >= 0; h--)
                    {
                        magnitude_sqr = unsafe_histogram[h];
                        if (h < bucket)
                        {
                            currMeanDark += h * magnitude_sqr;
                            VarianceDark += (bucket - h) * magnitude_sqr;
                            DarkHits += magnitude_sqr;
                        }
                        else
                        {
                            currMeanLight += h * magnitude_sqr;
                            VarianceLight += (bucket - h) * magnitude_sqr;
                            LightHits += magnitude_sqr;
                        }
                    }

                    // compute means
                    if (DarkHits > 0)
                    {
                        // rescale into 0-255 range
                        divisor = DarkHits * histlength;
                        currMeanDark = currMeanDark * 255 / divisor;
                        VarianceDark = VarianceDark * 255 / divisor;
                    }
                    if (LightHits > 0)
                    {
                        // rescale into 0-255 range
                        divisor = LightHits * histlength;
                        currMeanLight = currMeanLight * 255 / divisor;
                        VarianceLight = VarianceLight * 255 / divisor;
                    }

                    Variance = VarianceDark + VarianceLight;
                    if (Variance < 0) Variance = -Variance;

                    if (Variance < MinVariance)
                    {
                        MinVariance = Variance;
                        MeanDark = currMeanDark;
                        MeanLight = currMeanLight;
                    }
                    if ((int)(Variance * 1000) == (int)(MinVariance * 1000))
                    {
                        MeanLight = currMeanLight;
                    }
                }
            }
            return(MeanLight);
        }
                
        protected void AutoExposure(byte[] left_img, byte[] right_img)
        {        
            const int ideal_mean_light = 200;
            
            if ((left_img != null) && (right_img != null))
            {
                // clear the histogram
                for (int i = intensity_histogram.Length-1; i >= 0; i--)
                    intensity_histogram[i] = 0;
                
                // update the histogram
                int sampling_step = left_img.Length / (3*300);
                if (sampling_step < 3) sampling_step = 3;
                
                for (int i = left_img.Length-1; i >= 0; i -= sampling_step)
                    intensity_histogram[left_img[i]]++;
                for (int i = right_img.Length-1; i >= 0; i -= sampling_step)
                    intensity_histogram[right_img[i]]++;
                    
                float MeanLight = GetMeanLight(intensity_histogram);
                int diff = (int)(MeanLight - ideal_mean_light);
                int adjust = diff / 5;
                if (adjust == 0)
                {
                    if (diff > 0) 
                        adjust = 1;
                    else
                        adjust = -1;
                }
                if ((MeanLight > ideal_mean_light + exposure_deadband) ||
                    (MeanLight < ideal_mean_light - exposure_deadband))
                    exposure -= adjust;
                
                if (exposure > 100) exposure = 100;
                if (exposure < 1) exposure = 1;
            }
        }
        
        #endregion
        
        #region "saving calibration data as Xml"

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <param name="device_name"></param>
        /// <param name="part_number"></param>
        /// <param name="serial_number"></param>
        /// <param name="focal_length_pixels"></param>
        /// <param name="focal_length_mm"></param>
        /// <param name="baseline_mm"></param>
        /// <param name="fov_degrees"></param>
        /// <param name="image_width"></param>
        /// <param name="image_height"></param>
        /// <param name="lens_distortion_curve"></param>
        /// <param name="centre_of_distortion_x"></param>
        /// <param name="centre_of_distortion_y"></param>
        /// <param name="minimum_rms_error"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="offset_x"></param>
        /// <param name="offset_y"></param>
        /// <returns></returns>
        protected static XmlDocument getXmlDocument(
            string device_name,
            string part_number,
            string serial_number,
            float focal_length_pixels,
            float focal_length_mm,
            float baseline_mm,
            float fov_degrees,
            int image_width, int image_height,
            polynomial[] lens_distortion_curve,
            float[] centre_of_distortion_x, float[] centre_of_distortion_y,
            float[] minimum_rms_error,
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

            XmlElement nodeCalibration = doc.CreateElement("Sentience");
            doc.AppendChild(nodeCalibration);

            XmlElement elem = getXml(doc, nodeCalibration,
                device_name,
                part_number,
                serial_number,
                focal_length_pixels,
                focal_length_mm,
                baseline_mm,
                fov_degrees,
                image_width, image_height,
                lens_distortion_curve,
                centre_of_distortion_x, centre_of_distortion_y,
                minimum_rms_error,
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
                float[] minimum_rms_error = new float[2];
                for (int cam = 0; cam < 2; cam++)
                {
                    lens_distortion_curve[cam] = calibration_survey[cam].best_fit_curve;
                    centre_of_distortion_x[cam] = calibration_survey[cam].centre_of_distortion_x;
                    centre_of_distortion_y[cam] = calibration_survey[cam].centre_of_distortion_y;
                    minimum_rms_error[cam] = (float)calibration_survey[cam].minimum_rms_error;
                }
            
                XmlDocument doc =
                    getXmlDocument(
                        device_name,
                        part_number,
                        serial_number,
                        focal_length_mm,
                        pixels_per_mm,
                        baseline_mm,
                        fov_degrees,
                        image_width, image_height,
                        lens_distortion_curve,
                        centre_of_distortion_x, centre_of_distortion_y,
                        minimum_rms_error,
                        rotation, scale,
                        offset_x, offset_y);

                doc.Save(filename);
            }
        }

		/// <summary>
		/// returns stereo camera parameters as an xml document
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="parent"></param>
		/// <param name="device_name">camera device name</param>
		/// <param name="part_number">camera device part number</param>
		/// <param name="serial_number">camera device serial number</param>
		/// <param name="focal_length_mm">camera focal length in millimetres</param>
		/// <param name="pixels_per_mm">number of pixels per millimetre, from the image sensor size/resolution</param>
		/// <param name="baseline_mm">stereo baseline in millimetres</param>
		/// <param name="fov_degrees">camera field of view in degrees</param>
		/// <param name="image_width">camera image width in pixels</param>
		/// <param name="image_height">camera image height in pixels</param>
		/// <param name="lens_distortion_curve">polynomial describing the lens distortion curve</param>
		/// <param name="centre_of_distortion_x">x coordinate of the centre of distortion within the image in pixels</param>
		/// <param name="centre_of_distortion_y">y coordinate of the centre of distortion within the image in pixels</param>
		/// <param name="minimum_rms_error">polynomial curve fitting</param>
		/// <param name="rotation">rotation of the right image relative to the left in degrees</param>
		/// <param name="scale"></param>
		/// <param name="offset_x">offset from parallel alignment in pixels in the horizontal axis</param>
		/// <param name="offset_y">offset from parallel alignment in pixels in the vertical axis</param>
		/// <returns></returns>
        protected static XmlElement getXml(
            XmlDocument doc, XmlElement parent,
            string device_name,
            string part_number,
            string serial_number,
            float focal_length_mm,
            float pixels_per_mm,
            float baseline_mm,
            float fov_degrees,
            int image_width, int image_height,
            polynomial[] lens_distortion_curve,
            float[] centre_of_distortion_x, float[] centre_of_distortion_y,
            float[] minimum_rms_error,
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

            xml.AddComment(doc, nodeStereoCamera, "Supplier of the camera device");
            xml.AddTextElement(doc, nodeStereoCamera, "SupplierName", "Surveyor Corporation");

            xml.AddComment(doc, nodeStereoCamera, "Part number of the camera device");
            xml.AddTextElement(doc, nodeStereoCamera, "PartNumber", part_number);

            xml.AddComment(doc, nodeStereoCamera, "Serial number of the camera device");
            xml.AddTextElement(doc, nodeStereoCamera, "SerialNumber", serial_number);

            xml.AddComment(doc, nodeStereoCamera, "Dimensions of the images in pixels");
            xml.AddTextElement(doc, nodeStereoCamera, "ImageDimensions", Convert.ToString(image_width) + "x" + Convert.ToString(image_height));

            xml.AddComment(doc, nodeStereoCamera, "Focal length in millimetres");
            xml.AddTextElement(doc, nodeStereoCamera, "FocalLengthMillimetres", Convert.ToString(focal_length_mm, format));

            xml.AddComment(doc, nodeStereoCamera, "Sensor density in pixels per millimetre");
            xml.AddTextElement(doc, nodeStereoCamera, "SensorDensityPixelsPerMillimetre", Convert.ToString(pixels_per_mm, format));

            xml.AddComment(doc, nodeStereoCamera, "Camera baseline distance in millimetres");
            xml.AddTextElement(doc, nodeStereoCamera, "BaselineMillimetres", Convert.ToString(baseline_mm, format));

            xml.AddComment(doc, nodeStereoCamera, "Calibration Data");

            XmlElement nodeCalibration = doc.CreateElement("Calibration");
            nodeStereoCamera.AppendChild(nodeCalibration);

            string offsets = Convert.ToString(offset_x, format) + " " +
                             Convert.ToString(offset_y, format);
            xml.AddComment(doc, nodeCalibration, "Image offsets in pixels due to small missalignment from parallel");
            xml.AddTextElement(doc, nodeCalibration, "Offsets", offsets);

            xml.AddComment(doc, nodeCalibration, "Rotation of the right image relative to the left in degrees");
            xml.AddTextElement(doc, nodeCalibration, "RelativeRotationDegrees", Convert.ToString(rotation / (float)Math.PI * 180.0f, format));

            for (int cam = 0; cam < lens_distortion_curve.Length; cam++)
            {
                XmlElement elem = getCameraXml(
                    doc, fov_degrees,
                    lens_distortion_curve[cam],
                    centre_of_distortion_x[cam], centre_of_distortion_y[cam],
                    minimum_rms_error[cam]);
                nodeCalibration.AppendChild(elem);
            }

            return (nodeStereoCamera);
        }


        /// <summary>
        /// return an xml element containing camera calibration parameters
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="fov_degrees">field of view in degrees</param>
        /// <param name="lens_distortion_curve">polynomial describing the lens distortion</param>
        /// <param name="centre_of_distortion_x">x coordinate of the centre of distortion within the image in pixels</param>
        /// <param name="centre_of_distortion_y">y coordinate of the centre of distortion within the image in pixels</param>
        /// <param name="minimum_rms_error">minimum curve fitting error in pixels</param>
        /// <returns></returns>
        protected static XmlElement getCameraXml(
            XmlDocument doc,
            float fov_degrees,
            polynomial lens_distortion_curve,
            float centre_of_distortion_x, float centre_of_distortion_y,
            float minimum_rms_error)
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
                    if (i < degree) coefficients += " ";
                }
            }
            else coefficients = "0,0,0";

            XmlElement elem = doc.CreateElement("Camera");
            doc.DocumentElement.AppendChild(elem);
            xml.AddComment(doc, elem, "Horizontal field of view of the camera in degrees");
            xml.AddTextElement(doc, elem, "FieldOfViewDegrees", Convert.ToString(fov_degrees, format));
            xml.AddComment(doc, elem, "The centre of distortion in pixels");
            xml.AddTextElement(doc, elem, "CentreOfDistortion", Convert.ToString(centre_of_distortion_x, format) + " " + Convert.ToString(centre_of_distortion_y, format));
            xml.AddComment(doc, elem, "Polynomial coefficients used to describe the camera lens distortion");
            xml.AddTextElement(doc, elem, "DistortionCoefficients", coefficients);
            xml.AddComment(doc, elem, "The minimum RMS error between the distortion curve and plotted points");
            xml.AddTextElement(doc, elem, "RMSerror", Convert.ToString(minimum_rms_error, format));

            return (elem);
        }

        #endregion        

        #region "loading calibration data from xml"

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename">filename to load from</param>
        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                // use an XmlTextReader to open an XML document
                XmlTextReader xtr = new XmlTextReader(filename);
                xtr.WhitespaceHandling = WhitespaceHandling.None;

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                xd.Load(xtr);

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
                int cameraIndex = -1;
                LoadFromXml(xnodDE, 0, ref cameraIndex);

                // trigger calculation of the calibration maps
                // by resetting them to null
                calibration_map[0] = null;
                calibration_map[1] = null;

                // close the reader
                xtr.Close();
            }
        }

        /// <summary>
        /// reset the calibration for left or right camera
        /// </summary>
        /// <param name="camera_index">index number of the camera</param>
        public void ResetCalibration(int camera_index)
        {
            calibration_survey[camera_index].Reset();
            calibration_map[camera_index] = null;
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level, ref int camera_index)
        {
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");

            if (xnod.Name == "Camera")
            {
                camera_index++;
                calibration_survey[camera_index] = new CalibrationSurvey();
            }
            
            if (xnod.Name == "DeviceName")
            {
                device_name = xnod.InnerText;
            }

            if (xnod.Name == "PartNumber")
            {
                part_number = xnod.InnerText;
            }

            if (xnod.Name == "SerialNumber")
            {
                serial_number = xnod.InnerText;
            }

            if (xnod.Name == "Offsets")
            {
                string[] offsets = xnod.InnerText.Split(' ');
                offset_x = Convert.ToSingle(offsets[0], format);
                offset_y = Convert.ToSingle(offsets[1], format);
            }

            if (xnod.Name == "FocalLengthMillimetres")
            {
                focal_length_mm = Convert.ToSingle(xnod.InnerText, format);
            }

            if (xnod.Name == "SensorDensityPixelsPerMillimetre")
            {
                pixels_per_mm = Convert.ToSingle(xnod.InnerText, format);
                focal_length_pixels = focal_length_mm * pixels_per_mm;
            }

            if (xnod.Name == "BaselineMillimetres")
            {
                baseline_mm = Convert.ToSingle(xnod.InnerText, format);
            }
                        
            if (xnod.Name == "FieldOfViewDegrees")
                fov_degrees = Convert.ToSingle(xnod.InnerText, format);

            if (xnod.Name == "ImageDimensions")
            {
                string[] dimStr = xnod.InnerText.Split('x');
                image_width = Convert.ToInt32(dimStr[0]);
                image_height = Convert.ToInt32(dimStr[1]);
            }

            if (xnod.Name == "CentreOfDistortion")
            {
                string[] centreStr = xnod.InnerText.Split(' ');                
                calibration_survey[camera_index].centre_of_distortion_x =
                    Convert.ToSingle(centreStr[0], format);
                calibration_survey[camera_index].centre_of_distortion_y =
                    Convert.ToSingle(centreStr[1], format);
            }

            if (xnod.Name == "DistortionCoefficients")
            {
                if (xnod.InnerText != "")
                {
                    string[] coeffStr = xnod.InnerText.Split(' ');
                    calibration_survey[camera_index].best_fit_curve = new polynomial();
                    calibration_survey[camera_index].best_fit_curve.SetDegree(coeffStr.Length - 1);
                    for (int i = 0; i < coeffStr.Length; i++)
                        calibration_survey[camera_index].best_fit_curve.SetCoeff(i, Convert.ToSingle(coeffStr[i], format));
                }
            }

            if (xnod.Name == "RelativeRotationDegrees")
            {
                rotation = Convert.ToSingle(xnod.InnerText, format) / 180.0f * (float)Math.PI;
            }

            if (xnod.Name == "RMSerror")
            {
                calibration_survey[camera_index].minimum_rms_error = Convert.ToSingle(xnod.InnerText, format);
            }

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                XmlNode xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1, ref camera_index);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }


        #endregion

		/// <summary>
		/// Calculates horizontal and vertical offsets which are the
		/// result of the cameras not being perfectly aligned in parallel
		/// </summary>
		/// <returns>
		/// true if offsets were calculated successfully
		/// </returns>
        public bool CalibrateCameraAlignment()
        {
            bool done = false;
            if (rectified != null)
            {
                if ((rectified[0] != null) &&
                    (rectified[1] != null))
                {
                    done = SurveyorCalibration.UpdateOffsets(rectified[0], rectified[1],
                                                             ref offset_x, ref offset_y,
                                                             ref rotation);
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
            byte[][] img_rectified = new byte[2][];
        
            for (int cam = 0; cam < 2; cam++)
            {
                Bitmap bmp = left_image;
                if (cam == 1) bmp = right_image;

                if ((calibration_survey[cam] != null) && (bmp != null))
                {
                    polynomial distortion_curve = calibration_survey[cam].best_fit_curve;
                    if (distortion_curve != null)
                    {
                        if (calibration_map[cam] == null)
                        {
                            SurveyorCalibration.updateCalibrationMap(
                                bmp.Width, bmp.Height, distortion_curve,
                                1, 0,
                                calibration_survey[cam].centre_of_distortion_x, calibration_survey[cam].centre_of_distortion_y,
                                ref calibration_map[cam], ref calibration_map_inverse[cam]);
                        }

                        byte[] img = null;
                        try
                        {
                            img = new byte[bmp.Width * bmp.Height * 3];
                        }
                        catch
                        {
                        }
                        if (img != null)
                        {
                            BitmapArrayConversions.updatebitmap(bmp, img);
                            img_rectified[cam] = (byte[])img.Clone();

                            int n = 0;
                            int[] map = calibration_map[cam];
                            for (int i = 0; i < img.Length; i += 3, n++)
                            {
                                int index = map[n] * 3;
                                for (int col = 0; col < 3; col++)
                                    img_rectified[cam][i + col] = img[index + col];
                            }

                            if (rectified[cam] == null)
                                rectified[cam] = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            BitmapArrayConversions.updatebitmap_unsafe(img_rectified[cam], rectified[cam]);
                        }
                    }
                }
            }
            
            // adjust exposure
            AutoExposure(img_rectified[0], img_rectified[1]);
        }
        
        
        #region "starting and stopping"

        public bool Running;
        protected Thread sync_thread;
        
        public virtual void Run()
        {
            bool cameras_started = true;
            
        }

        public virtual void Stop()
        {
        }
        
        #endregion
        
        #region "getters and setters"
        
        public virtual void SetFramesPerSecond(int fps)
        {
            this.fps = fps;
        }
        
        #endregion
        
        #region "edge detection"
                
        protected Bitmap edges;
        protected Bitmap linked_dots;
        protected Bitmap grid;
        protected Bitmap grid_diff;
        protected Bitmap raw_difference;
        protected EdgeDetectorCanny edge_detector;
        
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

        protected void UpdateRawDifference(Bitmap left_image, Bitmap right_image)
        {
            if (raw_difference == null)
            {
                raw_difference = new Bitmap(left_image.Width, left_image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);    
            }
            else
            {
                if ((raw_difference.Width != left_image.Width) ||
                    (raw_difference.Height != left_image.Height))
                {
                    raw_difference = new Bitmap(left_image.Width, left_image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);    
                }
            }

            int pixels = left_image.Width * left_image.Height * 3;

            byte[] left_img = new byte[pixels];
            byte[] right_img = new byte[pixels];

            BitmapArrayConversions.updatebitmap(left_image, left_img);
            BitmapArrayConversions.updatebitmap(right_image, right_img);
            
            byte[] raw_difference_img = new byte[pixels];
            int n = 0;
            int cam = 0;
            int w = left_image.Width;
            int h = left_image.Height;
            for (int y = 0; y < h; y++)
            {
                cam = y % 2;
                for (int x = 0; x < w; x++)
                {
                    if (cam == 0)
                    {
                        for (int col = 0; col < 3; col++)
                            raw_difference_img[n+col] = left_img[n+col];
                    }
                    else
                    {
                        int xx = x + (int)offset_x;
                        int yy = y + (int)offset_y;
                        if ((xx > -1) && (xx < w) &&
                            (yy > -1) && (yy < h))
                        {
                            int n2 = ((yy * w) + xx) * 3; 
                            for (int col = 0; col < 3; col++)
                                raw_difference_img[n+col] = right_img[n2+col];
                        }
                        else
                        {
                            for (int col = 0; col < 3; col++)
                                raw_difference_img[n+col] = left_img[n+col];
                        }
                    }
                    cam = 1 - cam;
                    n += 3;
                }
            }
            BitmapArrayConversions.updatebitmap_unsafe(raw_difference_img, raw_difference);
        }

        #endregion
        
        #region "stereo correspondence"
        
        protected StereoVision correspondence;
        protected Bitmap stereo_features;
        
        /// <summary>
        /// perform stereo correspondence between the two images
        /// </summary>
        protected void StereoCorrespondence()
        {        
            bool images_rectified = false;
            if ((rectified[0] != null) && (rectified[1] != null)) images_rectified = true;
        
            if (correspondence != null)
            {
                if ((broadcasting) && (stereo_algorithm_type != prev_stereo_algorithm_type))
                {
                    correspondence.StopService();
                    broadcasting = false;
                }
            }

            switch(stereo_algorithm_type)
            {
                case StereoVision.SIMPLE:
                {                      
                    if (correspondence == null)
                        correspondence = new StereoVisionSimple();
                        
                    if (correspondence.algorithm_type != StereoVision.SIMPLE)
                        correspondence = new StereoVisionSimple();                        

                    if (images_rectified)
                        correspondence.Update(rectified[0], rectified[1],
                                              -offset_x, offset_y);
                        
                    break;
                }
                case StereoVision.GEOMETRIC:
                {
                    if (correspondence == null)
                        correspondence = new StereoVisionGeometric();
                        
                    if (correspondence.algorithm_type != StereoVision.GEOMETRIC)
                        correspondence = new StereoVisionGeometric();

                    if (images_rectified)
                        correspondence.Update(rectified[0], rectified[1],
                                              offset_x, offset_y);
                    
                    break;
                }
                case StereoVision.DENSE:
                {
                    if (correspondence == null)
                        correspondence = new StereoVisionDense();
                        
                    if (correspondence.algorithm_type != StereoVision.DENSE)
                        correspondence = new StereoVisionDense();

                    if (images_rectified)
                        correspondence.Update(rectified[0], rectified[1],
                                              offset_x, offset_y);
                    
                    break;
                }
            }

            correspondence.random_rows = random_rows;

            if (images_rectified)
            {
                correspondence.Show(ref stereo_features);
            }
            else
            {
                Console.WriteLine("Warning: Images not rectified");
            }

            if (!broadcasting)
                broadcasting = correspondence.StartService(broadcast_port_number);
                
            prev_stereo_algorithm_type = stereo_algorithm_type;
        }
        
        #endregion
       
        #region "process images"

        public virtual void Process(Bitmap left_image, Bitmap right_image)
        {        
            DisplayImages(left_image, right_image);
            StereoCorrespondence();
        }
        
        #endregion
        
        #region "pause and resume"
        
        public virtual void Pause()
        {
        }
        
        public virtual void Resume()
        {
        }
        
        #endregion
        
    }
}
