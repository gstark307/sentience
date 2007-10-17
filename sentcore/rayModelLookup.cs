/*
    Sentience 3D Perception System
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
using sluggish.utilities.xml;

namespace sentience.core
{
    /// <summary>
    /// stores a lookup table for sensor models
    /// The first dimension is the visual disparity in 0.5 pixel steps
    /// The second dimension is the probability value at each distance
    /// Normally the lookup is quantised such that each array element corresponds
    /// to a single occupancy grid cell
    /// </summary>
    public sealed class rayModelLookup
    {
        // stores the sensor models
        public float[,] probability = null;

        // the length of each sensor model, given as an array index
        public int[] length = null;

        public int dimension_disparity, dimension_probability;

        private void init(int max_disparity_pixels,
                          int max_sensor_model_length)
        {
            dimension_disparity = max_disparity_pixels * 2;
            dimension_probability = max_sensor_model_length;
            probability = new float[dimension_disparity, dimension_probability];
            length = new int[dimension_disparity];
        }

        public rayModelLookup(int max_disparity_pixels,
                              int max_sensor_model_length)
        {
            init(max_disparity_pixels, max_sensor_model_length);
        }

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            xml.AddComment(doc, parent, "Inverse sensor model data");

            XmlElement nodeSensorModels = doc.CreateElement("InverseSensorModels");
            parent.AppendChild(nodeSensorModels);

            xml.AddComment(doc, nodeSensorModels, "Interval between models in pixels");
            xml.AddTextElement(doc, nodeSensorModels, "SensorModelInterval", "0.5");

            xml.AddComment(doc, nodeSensorModels, "Model Data");
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            for (int d = 0; d < dimension_disparity; d++)
            {
                String dataStr = "";
                for (int i = 0; i < length[d]; i++)
                {
                    dataStr += Convert.ToString(probability[d, i], format);
                    if (i < length[d] - 1) dataStr += ",";
                }
                if (dataStr != "")
                    xml.AddTextElement(doc, nodeSensorModels, "RayModel", dataStr);
            }

            return (nodeSensorModels);
        }

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

            XmlElement nodeSentience = doc.CreateElement("Sentience");
            doc.AppendChild(nodeSentience);

            nodeSentience.AppendChild(getXml(doc, nodeSentience));

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
        /// creates a new sensor model array from the given list of loaded ray models
        /// </summary>
        /// <param name="rayModelsData">list containing ray model data as strings</param>
        public void LoadSensorModelData(ArrayList rayModelsData)
        {
            // get the maximum array size needed to store the data
            int ray_model_normal_length = 10;
            for (int i = 0; i < rayModelsData.Count; i++)
            {
                String[] dataStr = ((String)rayModelsData[i]).Split(',');
                if (dataStr.Length > ray_model_normal_length) ray_model_normal_length = dataStr.Length;
            }

            // initialise the ray model array
            init((rayModelsData.Count / 2) + 1, ray_model_normal_length);

            // insert the data into the array
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            for (int i = 0; i < rayModelsData.Count; i++)
            {
                int d = i + 2; // we add 2 here because the first two slots
                if (d < length.Length)
                {
                    // corresponding to zero and 0.5 pixel disparity don't exist
                    String[] dataStr = ((String)rayModelsData[i]).Split(',');
                    length[d] = dataStr.Length;
                    for (int j = 0; j < dataStr.Length; j++)
                        probability[d, j] = Convert.ToSingle(dataStr[j], format);
                }
            }
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public bool Load(String filename)
        {
            bool loaded = false;

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
                ArrayList rayModelsData = new ArrayList();
                LoadFromXml(xnodDE, 0, rayModelsData);
                LoadSensorModelData(rayModelsData);

                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level, ArrayList rayModelsData)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "SensorModelInterval")
            {
                float ray_model_interval_pixels = Convert.ToSingle(xnod.InnerText);
            }

            if (xnod.Name == "RayModel")
            {
                rayModelsData.Add(xnod.InnerText);
            }

            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) &&
                (xnod.Name == "InverseSensorModels"))
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1, rayModelsData);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }
}
