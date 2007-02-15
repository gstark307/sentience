/*
    Stereo camera calibration class, used for automatic calibration of two cameras
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using sentience.core;

namespace sentience.calibration
{
    public class calibrationStereo
    {
        public calibration leftcam, rightcam;

        // horizontal and vertical offset of the right image relative to the left image
        public int offset_x = 0, offset_y = 0;

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
            if ((leftcam.centre_spot_rectified != null) && (rightcam.centre_spot_rectified != null))
            {
                offset_y = (int)(rightcam.centre_spot_rectified.y - leftcam.centre_spot_rectified.y);
            }
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

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
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

            util.AddComment(doc, nodeCalibration, "Calibration data");

            XmlElement nodeCameras = doc.CreateElement("Calibration");
            nodeCalibration.AppendChild(nodeCameras);

            String offsets = Convert.ToString(offset_x) + "," +
                             Convert.ToString(offset_y);
            util.AddComment(doc, nodeCameras, "Image offsets in pixels");
            util.AddTextElement(doc, nodeCameras, "Offsets", offsets);            

            XmlElement elem = leftcam.getXml(doc);
            nodeCameras.AppendChild(elem);
            elem = rightcam.getXml(doc);
            nodeCameras.AppendChild(elem);

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
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
                if (xnod.Name == "Offsets")
                {
                    String[] offsets = xnod.InnerText.Split(',');
                    offset_x = Convert.ToInt32(offsets[0]);
                    offset_y = Convert.ToInt32(offsets[1]);
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
