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
        public float fps = 30;
        public int stereo_camera_index = 0;
        public bool endless_thread = true;
        public int wait_for_grab_mS = 500;

        // rectification can be switched off
        public bool disable_rectification;

        // disable only the radial correction, but use offsets and scale/rotation
        public bool disable_radial_correction;

        // on Windows use media control pause or not
        public bool use_media_pause = true;

        // these values are completely arbitrary and specific to the camera model
        // You can use AMcap on Windows to find out what the range of exposure values is
		// or v4l-info on Linux
        public int max_exposure = 650;
        public int min_exposure = 0;

        public string device_name;
        public string part_number;
        public string serial_number;
        public int image_width, image_height;
        public float focal_length_pixels = 1105.2f;
        public float focal_length_mm = 3.6f;    // 90 degree FOV lens
        public float pixels_per_mm = 307;       // density of pixels on the sensor
        public float baseline_mm = 100;
        public float fov_degrees = 90;
        public int stereo_algorithm_type = StereoVision.EDGES;
        protected int prev_stereo_algorithm_type;
        protected bool broadcasting;
        
        public BaseVisionStereo next_camera;
        public bool active_camera = true;
        
        // whether to record raw camera images
        public bool Record;        
        public ulong RecordFrameNumber;

        // whether to save debugging images
        public bool SaveDebugImages;
        public int DebugFrameNumber;
        
        // whether to show the left or right image during calibration
        public bool show_left_image;
        public Bitmap calibration_pattern;
        
        public CalibrationSurvey[] calibration_survey;
        public int[][] calibration_map;
        public int[][,,] calibration_map_inverse;

        // offsets when observing objects at
        // long distance (less than one pixel disparity)
        public float offset_x=0, offset_y=0;

        // scaling factor of one camera image relative to the other
        public float scale = 1;
        
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
        
        public float exposure = 50;
        public float exposure_gain = 1.0f / 50.0f;  // rate at which exposure is changed
        
        // what type of image should be displayed
        public const int DISPLAY_RAW = 0;
        public const int DISPLAY_CALIBRATION_DOTS = 1;
        public const int DISPLAY_CALIBRATION_GRID = 2;
        public const int DISPLAY_CALIBRATION_DIFF = 3;
        public const int DISPLAY_RECTIFIED = 4;
        public const int DISPLAY_STEREO_SIMPLE = 5;
        public const int DISPLAY_DIFFERENCE = 6;
        public int display_type = DISPLAY_CALIBRATION_GRID;
        
        // path where any temporary files will be stored
        public string temporary_files_path;
        
        // path where recorded images will be stored
        public string recorded_images_path;

        // whether to flip images if the cameras are inverted
        public bool flip_left_image, flip_right_image;
        
        #region "constructors"
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="broadcast_port">port number on which to broadcast stereo feature data to other applications</param>
        /// <param name="fps">ideal frames per second</param>
        public BaseVisionStereo(int broadcast_port, float fps)
        {
            this.fps = fps;
        
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
                float adjust = diff * exposure_gain;
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
            float offset_x, float offset_y,
            bool disable_rectification,
            bool disable_radial_correction,
            bool flip_left_image,
            bool flip_right_image)
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
                offset_x, offset_y,
                disable_rectification,
                disable_radial_correction,
                flip_left_image,
                flip_right_image);
            doc.DocumentElement.AppendChild(elem);

            return (doc);
        }

        /// <summary>
        /// save calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">filename to save as</param>
        public void Save(string filename)
        {
            int img_width = image_width;
            int img_height = image_height;

            if ((rectified[0] != null) &&
                (rectified[1] != null))
            {
                img_width = rectified[0].Width;
                img_height = rectified[0].Height;
            }

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
                    img_width, img_height,
                    lens_distortion_curve,
                    centre_of_distortion_x, centre_of_distortion_y,
                    minimum_rms_error,
                    rotation, scale,
                    offset_x, offset_y,
                    disable_rectification,
                    disable_radial_correction,
                    flip_left_image,
                    flip_right_image);

            doc.Save(filename);
						
			SaveCameraParameters(
		        "svs_left",
		        "svs_right",
                lens_distortion_curve,
                centre_of_distortion_x, 
		        centre_of_distortion_y,
                rotation, 
                scale,
                offset_x,
			    offset_y);
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
        /// <param name="flip_left_image">whether the left image should be flipped (camera mounted inverted)</param>
        /// <param name="flip_right_image">whether the right image should be flipped (camera mounted inverted)</param>
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
            float rotation, 
            float scale,
            float offset_x, float offset_y,
            bool disable_rectification,
            bool disable_radial_correction,
            bool flip_left_image,
            bool flip_right_image)
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

            xml.AddComment(doc, nodeCalibration, "Scale of the right image relative to the left");
            xml.AddTextElement(doc, nodeCalibration, "RelativeImageScale", Convert.ToString(scale, format));

            xml.AddComment(doc, nodeCalibration, "Disable all rectification");
            xml.AddTextElement(doc, nodeCalibration, "DisableRectification", Convert.ToString(disable_rectification));

            xml.AddComment(doc, nodeCalibration, "Disable radial correction, and use offsets and scale/rotation only");
            xml.AddTextElement(doc, nodeCalibration, "DisableRadialCorrection", Convert.ToString(disable_radial_correction));

            for (int cam = 0; cam < lens_distortion_curve.Length; cam++)
            {
                bool flip = false;
                if (cam == 0)
                {
                    if (flip_left_image) flip = true;
                }
                else
                {
                    if (flip_right_image) flip = true;
                }
                
                XmlElement elem = getCameraXml(
                    doc, fov_degrees,
                    lens_distortion_curve[cam],
                    centre_of_distortion_x[cam], centre_of_distortion_y[cam],
                    minimum_rms_error[cam],
                    1.0f, flip);
                nodeCalibration.AppendChild(elem);
            }

            return (nodeStereoCamera);
        }

		/// <summary>
		/// saves camera calibration parameters to a binary file suitable
		/// for uploading to the Surveyor SVS
		/// </summary>
		/// <param name="filename_left">filename for left camera params</param>
		/// <param name="filename_right">filename for right camera params</param>
		/// <param name="lens_distortion_curve">polynomials</param>
		/// <param name="centre_of_distortion_x">centre of distortion</param>
		/// <param name="centre_of_distortion_y">centre of distortion</param>
		/// <param name="rotation">rotation in radians</param>
		/// <param name="scale">scale</param>
		/// <param name="offset_x">offset in pixels</param>
		/// <param name="offset_y">offset in pixels</param>
		public void SaveCameraParameters(
		    string filename_left,
		    string filename_right,
            polynomial[] lens_distortion_curve,
            float[] centre_of_distortion_x, 
		    float[] centre_of_distortion_y,
            float rotation, 
            float scale,
            float offset_x, float offset_y)		                                        
		{
			FileStream fs;
			BinaryWriter bw;
			
			for (int cam = 0; cam < 2; cam++)
			{
				if (cam == 0)
                    fs = File.Open(filename_left, FileMode.Create);
				else
					fs = File.Open(filename_right, FileMode.Create);

                bw = new BinaryWriter(fs);

				if (cam == 0)
				{
                    bw.Write((int)0);
					bw.Write((int)0);
				}
				else
				{
					bw.Write(Convert.ToInt32(-offset_x));
					bw.Write(Convert.ToInt32(offset_y));
				}
				bw.Write(Convert.ToInt32(centre_of_distortion_x[cam]));
				bw.Write(Convert.ToInt32(centre_of_distortion_y[cam]));
				if ((cam == 0) || (scale == 1))
				{
                    bw.Write((int)1);
					bw.Write((int)1);
				}
				else
				{
					bw.Write(Convert.ToInt32(scale*6000));
					bw.Write((int)6000);
				}
                int degree=0;
                if (lens_distortion_curve[cam] != null)
				    degree = lens_distortion_curve[cam].GetDegree();
                bw.Write(degree);
                for (int i = 1; i <= degree; i++)
				{
                    if (lens_distortion_curve[cam] != null)
					    bw.Write(Convert.ToInt32(Math.Round(lens_distortion_curve[cam].Coeff(i)*10000000)));
                    else
                        bw.Write(1);
				}
                bw.Write((int)image_width);
                bw.Write((int)image_height);

                bw.Close();
                fs.Close();
			}
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
        /// <param name="scale"></param>
        /// <param name="flip_image">whether to flip the image (camera mounted inverted)</param>
        /// <returns></returns>
        protected static XmlElement getCameraXml(
            XmlDocument doc,
            float fov_degrees,
            polynomial lens_distortion_curve,
            float centre_of_distortion_x, float centre_of_distortion_y,
            float minimum_rms_error,
            float scale,
            bool flip_image)
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
            xml.AddComment(doc, elem, "Scaling factor");
            xml.AddTextElement(doc, elem, "Scale", Convert.ToString(scale, format));
            if (flip_image)
            {
                xml.AddComment(doc, elem, "Whether to flip the image (camera mounted inverted)");
                xml.AddTextElement(doc, elem, "Flip", Convert.ToString(flip_image));
            }

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
                flip_left_image = false;
                flip_right_image = false;
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

            if (xnod.Name == "DisableRadialCorrection")
            {
                disable_radial_correction = Convert.ToBoolean(xnod.InnerText);
            }

            if (xnod.Name == "DisableRectification")
            {
                disable_rectification = Convert.ToBoolean(xnod.InnerText);
            }

            if (xnod.Name == "Scale")
            {
                // do nothing
            }

            if (xnod.Name == "Flip")
            {
                if (camera_index == 0)
                    flip_left_image = Convert.ToBoolean(xnod.InnerText);
                else
                    flip_right_image = Convert.ToBoolean(xnod.InnerText);
            }

            if (xnod.Name == "RelativeImageScale")
            {
                scale = Convert.ToSingle(xnod.InnerText, format);
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
				if ((xnod.InnerText != "") &&
				    (xnod.InnerText != "Infinity"))
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
                    done = SurveyorCalibration.UpdateOffsets(
                        rectified[0], rectified[1],
                        ref offset_x, ref offset_y,
                        ref rotation);
                }
            }
            return(done);
        }

        public void ResetCalibration()
        {
            ResetCalibration(0);
            ResetCalibration(1);
        }

        /// <summary>
        /// rectifies the given images
        /// </summary>
        /// <param name="left_image">left image bitmap</param>
        /// <param name="right_image">right image bitmap</param>        
        protected void RectifyImages(Bitmap left_image, Bitmap right_image)
        {
            byte[][] img_rectified = new byte[2][];
			int ww = 0;
			int hh = 0;
			if (left_image != null)
			{
			    ww = image_width; //left_image.Width;
			    hh = image_height; //left_image.Height;
			}

            if (disable_rectification)
            {
                // if rectification is dissabled make the rectified image the same as the raw image
                for (int cam = 0; cam < 2; cam++)
                {
                    Bitmap bmp = left_image;
                    if (cam == 1) bmp = right_image;

                    byte[] img = new byte[ww * hh * 3];
                    if (img != null)
                    {
                        BitmapArrayConversions.updatebitmap(bmp, img);
                        img_rectified[cam] = (byte[])img.Clone();
                        if (rectified[cam] == null)
                            rectified[cam] = new Bitmap(ww, hh, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapArrayConversions.updatebitmap_unsafe(img_rectified[cam], rectified[cam]);
                    }
                }
            }
            else
            {
                float rot = 0;
                float scale2 = 1.0f / scale;
                float radial_scale = scale2;
								
                for (int cam = 0; cam < 2; cam++)
                {
                    Bitmap bmp = left_image;					
                    if (cam == 1) bmp = right_image;

                    if ((calibration_survey[cam] != null) && (bmp != null))
                    {
						if (bmp != null)
						{							
	                        polynomial distortion_curve = calibration_survey[cam].best_fit_curve;
	                        if (distortion_curve != null)
	                        {
	                            if (calibration_map[cam] == null)
	                            {
	                                if (cam == 0)
	                                {
	                                    if (scale2 > 1)
	                                    {
	                                        radial_scale = 1.0f / scale2;
	                                        scale2 = 1;
	                                    }
	                                }
	                                else
	                                {
	                                    radial_scale = scale2;
	                                    rot = rotation;
	                                }
	
	                                if (!disable_radial_correction)
	                                {
	                                    SurveyorCalibration.updateCalibrationMap(
	                                        ww, hh, 
										    distortion_curve,
	                                        radial_scale, -rot,
	                                        calibration_survey[cam].centre_of_distortion_x, calibration_survey[cam].centre_of_distortion_y,
	                                        0, 0,
	                                        ref calibration_map[cam], ref calibration_map_inverse[cam]);
	                                }
	                                else
	                                {
	                                    SurveyorCalibration.updateCalibrationMap(
	                                        ww, hh,
	                                        radial_scale, -rot,
	                                        calibration_survey[cam].centre_of_distortion_x, calibration_survey[cam].centre_of_distortion_y,
	                                        0, 0,
	                                        ref calibration_map[cam], ref calibration_map_inverse[cam]);
	                                }
	                            }
	
	                            byte[] img = null;
	                            try
	                            {
	                                img = new byte[ww * hh * 3];
	                            }
	                            catch
	                            {
	                            }
	                            if ((img != null) && (bmp != null) &&
								    (calibration_map[cam] != null))
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
	                                    rectified[cam] = new Bitmap(ww, hh, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
	                                BitmapArrayConversions.updatebitmap_unsafe(img_rectified[cam], rectified[cam]);
	                            }
	                        }
	                        else
	                        {
								try
								{
	                                byte[] img = new byte[ww * hh * 3];
	                                if ((img != null) && (bmp != null))
	                                {
	                                    BitmapArrayConversions.updatebitmap(bmp, img);
	                                    img_rectified[cam] = (byte[])img.Clone();
	                                }
								}
								catch
								{
								}
	                        }
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
                case StereoVision.EDGES:
                {                      
                    if (correspondence == null)
                        correspondence = new StereoVisionEdges();
                        
                    if (correspondence.algorithm_type != StereoVision.EDGES)
                        correspondence = new StereoVisionEdges();               

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

            correspondence.vision = this;
            correspondence.random_rows = random_rows;

            if (images_rectified)
            {
                correspondence.Show(ref stereo_features);

                // save debugging images if needed
                // these are saved in gif format so that animations can easily be made
                if ((SaveDebugImages) &&
                    (stereo_features != null))
                {
                    string frame_number_str = DebugFrameNumber.ToString();

                    // prepend some zeros so that everything is in sequential order
                    // when making animations from the images
                    int v = 10;
                    while (v <= 100000)
                    {
                        if (DebugFrameNumber < v)
                            frame_number_str = "0" + frame_number_str;
                        v *= 10;
                    }
                    string debug_filename = "";
                    if (stereo_camera_index > -1) debug_filename += "stereo_camera_" + stereo_camera_index.ToString() + "_";
                    debug_filename += "stereo_features_" + frame_number_str + ".gif";

                    string path = "";
                    if ((recorded_images_path != null) &&
                        (recorded_images_path != ""))
                    {
                        if (recorded_images_path.EndsWith(@"\"))
                        {
                            path = recorded_images_path;
                        }
                        else
                        {
                            if (recorded_images_path.EndsWith("/"))
                                path = recorded_images_path;
                            else
                                path = recorded_images_path + "/";
                        }
                    }

                    debug_filename = path + debug_filename;

                    stereo_features.Save(debug_filename, System.Drawing.Imaging.ImageFormat.Gif);

                    if ((rectified[0] != null) &&
                        (rectified[1] != null))
                    {
                        byte[] img_rectified0 = new byte[rectified[0].Width * rectified[0].Height * 3];
                        byte[] img_rectified1 = new byte[rectified[1].Width * rectified[1].Height * 3];

                        BitmapArrayConversions.updatebitmap(rectified[0], img_rectified0);
                        BitmapArrayConversions.updatebitmap(rectified[1], img_rectified1);

                        byte[] img_composite = new byte[(rectified[0].Width + rectified[1].Width + 1) * rectified[0].Height * 3];

                        int n = 0;
                        for (int y = 0; y < rectified[0].Height; y++)
                        {
                            int yy = y * (rectified[0].Width + rectified[1].Width) * 3;
                            for (int x = 0; x < rectified[0].Width; x++, n += 3, yy += 3)
                            {
                                img_composite[yy] = img_rectified0[n];
                                img_composite[yy + 1] = img_rectified0[n + 1];
                                img_composite[yy + 2] = img_rectified0[n + 2];
                            }
                        }

                        n = 0;
                        for (int y = 0; y < rectified[0].Height; y++)
                        {
                            int yy = ((y * (rectified[0].Width + rectified[1].Width)) + rectified[0].Width + 1) * 3;
                            for (int x = 0; x < rectified[1].Width; x++, n += 3, yy += 3)
                            {
                                img_composite[yy] = img_rectified1[n];
                                img_composite[yy + 1] = img_rectified1[n + 1];
                                img_composite[yy + 2] = img_rectified1[n + 2];
                            }
                        }

                        Bitmap bmp_composite = new Bitmap(rectified[0].Width + rectified[1].Width, rectified[0].Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapArrayConversions.updatebitmap_unsafe(img_composite, bmp_composite);

                        debug_filename = path + "stereo_pair_" + stereo_camera_index.ToString() + "_" + frame_number_str + ".gif";
                        bmp_composite.Save(debug_filename, System.Drawing.Imaging.ImageFormat.Gif);
                        Console.WriteLine("Saved " + debug_filename);

                        bmp_composite.Dispose();
                        img_composite = null;
                        img_rectified0 = null;
                        img_rectified1 = null;
                    }

                    DebugFrameNumber++;
                    if (DebugFrameNumber > 999999999) DebugFrameNumber = 0;  // we live in hope!
                }
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
        
        public virtual void SetPauseFile(string filename)
        {
        }

        public virtual void ClearPauseFile()
        {
        }
        
        #endregion
        
    }
}
