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
using System.Collections.Generic;
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
        // 2D array stores the sensor models
        public float[][] probability = null;

        // the length of each sensor model, given as an array index
        public int[] length = null;

        // width of the ray model
        public int dimension_disparity;
        
        // length of the ray model
        public int dimension_probability;

        /// <summary>
        /// initialise
        /// </summary>
        /// <param name="max_disparity_pixels">maximum disparity in pixels</param>
        /// <param name="max_sensor_model_length">maximum sensor model length in grid cells</param>
        private void init(
		    int max_disparity_pixels,
            int max_sensor_model_length)
        {
            dimension_disparity = max_disparity_pixels * 2;
            dimension_probability = max_sensor_model_length;
            probability = new float[dimension_disparity][];
            for (int i = 0; i < probability.Length; i++)
                probability[i] = new float[dimension_probability];
            length = new int[dimension_disparity];
        }

     
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="max_disparity_pixels">maximum disparity in pixels</param>
        /// <param name="max_sensor_model_length">maximum sensor model length in grid cells</param>
        public rayModelLookup(int max_disparity_pixels,
                              int max_sensor_model_length)
        {
            init(max_disparity_pixels, max_sensor_model_length);
        }

        #region "saving and loading"

        /// <summary>
        /// returns the ray model as an XML object
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="parent"></param>
        /// <returns></returns>
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
                    dataStr += Convert.ToString(probability[d][i], format);
                    if (i < length[d] - 1) dataStr += ",";
                }
                if (dataStr != "")
                    xml.AddTextElement(doc, nodeSensorModels, "RayModel", dataStr);
            }

            return (nodeSensorModels);
        }

        /// <summary>
        /// returns the ray model (all integer values) as an XML object
        /// </summary>
        /// <param name="doc">xml document object</param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public XmlElement getXmlInteger(XmlDocument doc, XmlElement parent)
        {
            xml.AddComment(doc, parent, "Inverse sensor model data");

            XmlElement nodeSensorModels = doc.CreateElement("InverseSensorModels");
            parent.AppendChild(nodeSensorModels);

            xml.AddComment(doc, nodeSensorModels, "Interval between models in pixels");
            xml.AddTextElement(doc, nodeSensorModels, "SensorModelInterval", "1.0");

            xml.AddComment(doc, nodeSensorModels, "Index numbers for the start of each model");
			string dataStr="";
			int total = 0;
            for (int d = 0; d < dimension_disparity; d += 2)
            {
                dataStr += Convert.ToString(total) + ",";
				Console.WriteLine("data " + length[d].ToString() + " " + total.ToString());
                total += length[d];
			}
            if (dataStr != "")
			{
                dataStr += Convert.ToString(total) + "";
                xml.AddTextElement(doc, nodeSensorModels, "RayModelIndexes", dataStr);
			}
			
            xml.AddComment(doc, nodeSensorModels, "Model Data");
            for (int d = 0; d < dimension_disparity; d+=2)
            {
                dataStr = "";
                for (int i = 0; i < length[d]; i++)
                {
                    dataStr += Convert.ToString((int)(probability[d][i]*10000));
                    if (i < length[d] - 1) dataStr += ",";
                }
                if (dataStr != "")
                    xml.AddTextElement(doc, nodeSensorModels, "RayModel", dataStr);
            }

            return (nodeSensorModels);
        }
		
        /// <summary>
        /// return an Xml document containing sensor model parameters
        /// </summary>
        /// <returns>xml document object</returns>
        private XmlDocument getXmlDocument()
        {
			return(getXmlDocument(false));
        }

        /// <summary>
        /// return an Xml document containing sensor model parameters
        /// </summary>
        /// <returns>xml document object</returns>
        private XmlDocument getXmlDocument(bool integer_version)
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

			if (integer_version)
                nodeSentience.AppendChild(getXmlInteger(doc, nodeSentience));
			else
				nodeSentience.AppendChild(getXml(doc, nodeSentience));

            return (doc);
        }
		
        /// <summary>
        /// save ray model parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(string filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// creates a new ray model array from the given list of loaded ray models
        /// </summary>
        /// <param name="rayModelsData">list containing ray model data as strings</param>
        public void LoadSensorModelData(List<string> rayModelsData)
        {
            // get the maximum array size needed to store the data
            int ray_model_normal_length = 10;
            for (int i = 0; i < rayModelsData.Count; i++)
            {
                string[] dataStr = (rayModelsData[i]).Split(',');
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
                    string[] dataStr = ((string)rayModelsData[i]).Split(',');
                    length[d] = dataStr.Length;
                    for (int j = 0; j < dataStr.Length; j++)
                        probability[d][j] = Convert.ToSingle(dataStr[j], format);
                }
            }
        }

        /// <summary>
        /// load ray model parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public bool Load(string filename)
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
                List<string> rayModelsData = new List<string>();
                LoadFromXml(xnodDE, 0, rayModelsData);
                LoadSensorModelData(rayModelsData);

                // close the reader
                xtr.Close();
                loaded = true;
            }
            return (loaded);
        }

        /// <summary>
        /// parse an xml node to extract ray model parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        /// <param name="rayModelsData">ray model data loaded so far</param>
        public void LoadFromXml(XmlNode xnod, int level, List<string> rayModelsData)
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
            if (xnod.HasChildNodes)
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
