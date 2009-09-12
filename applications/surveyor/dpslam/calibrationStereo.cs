/*
    Stereo camera calibration class, used for automatic calibration of two cameras
    Copyright (C) 2000-2007 Bob Mottram
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
using System.Xml;
using System.Collections;
using System.Text;
using sentience.core;
using sluggish.utilities.xml;

namespace sentience.calibration
{
    /// <summary>
    /// scheduled for deletion
    /// </summary>
    public class calibrationStereo
    {
        public calibration leftcam, rightcam;

        // name of the WDM camera driver
        public String DriverName = "Not specified";
        
        // position and orientation of this camera relative to the head
        public pos3D positionOrientation = new pos3D(0, 0, 0);

        // horizontal and vertical offset of the right image relative to the left image
        public float offset_x = 0, offset_y = 0;

        // focal length in millimetres
        public float focalLength = 5;

        // camera baseline distance in millimetres
        public float baseline = 100.0f;

        /// <summary>
        /// set the position of the centre spot relative to the centre of the calibration pattern
        /// </summary>
        /// <param name="position"></param>
        public void setCentreSpotPosition(int position)
        {
            leftcam.centre_spot_position = position;
            rightcam.centre_spot_position = position;
        }

        public void update()
        {
            // set the baseline offsets
            leftcam.baseline_offset = -(baseline/2);
            rightcam.baseline_offset = baseline/2;

            // is calibration complete ?
            int calibration_threshold = 10;
            if ((leftcam.min_RMS_error < calibration_threshold) && (rightcam.min_RMS_error < calibration_threshold))
            {
                if ((leftcam.distance_to_pattern_centre > 0) &&
                    (leftcam.pattern_centre_rectified != null) &&
                    (rightcam.pattern_centre_rectified != null))
                {
                    // if the vertical displacement is too large assume this is a spurious case
                    if (Math.Abs(rightcam.pattern_centre_rectified.y - leftcam.pattern_centre_rectified.y) < 10)
                    {
                        // viewing angle to the centre spot from the left camera
                        float angle_left_radians = (leftcam.pattern_centre_rectified.x - (leftcam.image_width / 2)) / (leftcam.image_width / 2.0f) * (leftcam.camera_FOV_degrees / 2.0f);
                        angle_left_radians = angle_left_radians / 180.0f * (float)Math.PI;

                        // viewing angle to the centre spot from the right camera
                        float angle_right_degrees = (rightcam.pattern_centre_rectified.x - (rightcam.image_width / 2)) / (rightcam.image_width / 2.0f) * (rightcam.camera_FOV_degrees / 2.0f);

                        float d1 = leftcam.distance_to_pattern_centre * (float)Math.Tan(angle_left_radians);
                        float d2 = baseline - d1;
                        float angle_right_predicted_degrees = (float)Math.Atan(d2 / leftcam.distance_to_pattern_centre);
                        angle_right_predicted_degrees = -angle_right_predicted_degrees / (float)Math.PI * 180.0f;

                        // difference between the predicted angle for the right ray and the actual angle observed
                        float angle_diff_degrees = angle_right_degrees - angle_right_predicted_degrees;

                        // convert the angle into a pixel offset
                        offset_x = (angle_diff_degrees * rightcam.image_width) / rightcam.camera_FOV_degrees;

                        offset_y = rightcam.pattern_centre_rectified.y - leftcam.pattern_centre_rectified.y;
                    }
                }
            }
        }

        public void updateCalibrationMaps()
        {
            leftcam.updateCalibrationMap();
            rightcam.updateCalibrationMap();
        }

        /// <summary>
        /// constructor
        /// </summary>
        public calibrationStereo()
        {
            leftcam = new calibration();
            rightcam = new calibration();
        }

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent, int no_of_cameras)
        {
            XmlElement nodeStereoCamera;
            if (no_of_cameras > 1)
                nodeStereoCamera = doc.CreateElement("StereoCamera");
            else
                nodeStereoCamera = doc.CreateElement("MonocularCamera");
            parent.AppendChild(nodeStereoCamera);

            xml.AddComment(doc, nodeStereoCamera, "Name of the WDM software driver for the cameras");
            xml.AddTextElement(doc, nodeStereoCamera, "DriverName", DriverName);

            xml.AddComment(doc, nodeStereoCamera, "Position and orientation of the camera relative to the robots head");
            nodeStereoCamera.AppendChild(positionOrientation.getXml(doc));

            xml.AddComment(doc, nodeStereoCamera, "Focal length in millimetres");
            xml.AddTextElement(doc, nodeStereoCamera, "FocalLengthMillimetres", Convert.ToString(focalLength));

            if (no_of_cameras > 1)
            {
                xml.AddComment(doc, nodeStereoCamera, "Camera baseline distance in millimetres");
                xml.AddTextElement(doc, nodeStereoCamera, "BaselineMillimetres", Convert.ToString(baseline));
            }

            xml.AddComment(doc, nodeStereoCamera, "Calibration Data");

            XmlElement nodeCalibration = doc.CreateElement("Calibration");
            nodeStereoCamera.AppendChild(nodeCalibration);

            if (no_of_cameras > 1)
            {
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String offsets = Convert.ToString(offset_x, format) + "," +
                                 Convert.ToString(offset_y, format);
                xml.AddComment(doc, nodeCalibration, "Image offsets in pixels due to small missalignment from parallel");
                xml.AddTextElement(doc, nodeCalibration, "Offsets", offsets);
            }

            XmlElement elem = leftcam.getXml(doc);
            nodeCalibration.AppendChild(elem);

            if (no_of_cameras > 1)
            {
                elem = rightcam.getXml(doc);
                nodeCalibration.AppendChild(elem);
            }

            return (nodeStereoCamera);
        }

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument(int no_of_cameras)
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeSentience = doc.CreateElement("Sentience");
            doc.AppendChild(nodeSentience);

            nodeSentience.AppendChild(getXml(doc, nodeSentience, no_of_cameras));

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename, int no_of_cameras)
        {
            XmlDocument doc = getXmlDocument(no_of_cameras);
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
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
                int cameraIndex = 0;
                LoadFromXml(xnodDE, 0, ref cameraIndex);

                // close the reader
                xtr.Close();
            }
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level, ref int cameraIndex)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "Camera")
            {
                if (cameraIndex == 0)
                    leftcam.LoadFromXml(xnod, level);
                else
                    rightcam.LoadFromXml(xnod, level);
                cameraIndex++;
            }
            else
            {
                if (xnod.Name == "DriverName")
                {
                    DriverName = xnod.InnerText;
                }

                if (xnod.Name == "PositionOrientation")
                {
                    positionOrientation.LoadFromXml(xnod, level);
                }

                if (xnod.Name == "Offsets")
                {
                    IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                    String[] offsets = xnod.InnerText.Split(',');
                    offset_x = Convert.ToSingle(offsets[0], format);
                    offset_y = Convert.ToSingle(offsets[1], format);
                }

                if (xnod.Name == "FocalLengthMillimetres")
                {
                    focalLength = Convert.ToSingle(xnod.InnerText);
                }

                if (xnod.Name == "BaselineMillimetres")
                {
                    baseline = Convert.ToSingle(xnod.InnerText);
                }                

                // call recursively on all children of the current node
                if (xnod.HasChildNodes)
                {
                    xnodWorking = xnod.FirstChild;
                    while (xnodWorking != null)
                    {
                        LoadFromXml(xnodWorking, level + 1, ref cameraIndex);
                        xnodWorking = xnodWorking.NextSibling;
                    }
                }
            }
        }

        #endregion

    }
}
