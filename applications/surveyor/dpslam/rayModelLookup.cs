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

namespace dpslam.core
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
		
		// disparity interval between models
		public float ray_model_interval_pixels = 0.5f;

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
		
		#region "some default models"
		
		public void InitSurveyorSVS()
		{	
			int max_disparity_pixels = 320/2;
			int max_sensor_model_length = 100;
			init(max_disparity_pixels, max_sensor_model_length);
			
			/* index numbers for the start of each ray model */
			int[] sensmodelindex = {0,0,136,363,645,904,1057,1154,1222,1272,1311,1342,1367,1388,1406,1421,1435,1447,1458,1467,1476,1484,1491,1497,1503,1508,1513,1518,1522,1526,1530,1534,1537,1540,1543,1546,1549,1552,1554,1556,1558,1560,1562,1564,1566,1568,1570,1572,1574,1575,1576,1577,1578,1579,1580,1581,1582,1583,1584,1585,1586,1587,1588,1589,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590,1590};
			
			/* sensor model, computed for baseline = 107mm, grid cell size = 40mm */
			int[] sensmodel = {
			1,1,1,1,2,2,2,2,2,2,2,2,2,3,4,4,4,4,4,5,5,5,5,5,7,7,7,7,7,8,8,8,8,8,12,12,12,12,12,14,14,14,14,14,18,19,19,19,19,21,22,22,22,22,26,30,30,29,29,31,34,34,33,33,37,44,43,43,43,44,49,49,48,48,50,62,62,61,61,61,68,68,68,67,67,84,84,84,83,83,91,92,91,91,91,108,112,111,111,110,118,121,120,120,119,134,144,143,143,142,148,154,153,153,152,164,181,180,180,179,183,192,192,191,190,198,223,222,221,220,222,235,234,233,232,233,
			0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,5,6,6,6,6,6,8,8,8,7,8,10,10,10,10,10,12,12,12,12,12,15,15,15,15,14,17,18,18,18,17,20,21,21,21,21,23,25,25,24,24,27,29,28,28,28,30,33,32,32,32,34,37,37,36,36,38,41,41,41,41,41,46,45,45,45,45,50,50,50,49,49,55,54,54,54,54,59,59,59,58,58,62,63,63,63,62,66,67,67,67,66,70,71,71,71,70,73,75,75,74,74,76,79,78,78,78,79,82,82,81,81,82,85,84,84,84,84,88,87,87,86,86,90,90,89,89,88,92,92,91,91,90,93,93,93,93,92,95,95,94,94,94,96,96,96,95,95,96,97,97,96,96,97,98,97,97,96,97,98,97,97,97,97,98,97,97,97,97,
			0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,2,2,2,2,3,3,4,4,4,4,6,6,6,6,7,8,9,9,9,9,13,13,13,13,14,16,17,18,18,18,22,23,23,23,24,27,28,29,29,29,34,36,36,36,37,40,42,43,43,42,46,50,49,49,50,52,56,56,56,55,57,63,62,62,62,63,68,67,67,67,67,73,72,72,71,71,76,76,75,75,74,78,78,78,77,76,80,81,79,79,78,80,80,80,79,78,80,82,80,79,79,79,79,78,78,76,77,79,77,76,76,75,74,74,73,71,72,74,72,71,71,70,68,67,67,65,65,67,66,64,64,63,60,60,60,58,57,59,58,57,56,56,53,52,52,51,50,50,50,49,49,48,46,44,44,44,43,43,43,42,41,41,39,37,37,37,36,36,36,35,34,34,33,31,31,31,30,29,30,29,28,28,28,25,25,25,24,24,24,24,23,23,23,21,20,20,20,20,20,19,19,19,19,17,16,16,16,16,16,16,15,15,15,14,13,13,13,13,13,12,12,12,12,12,10,10,10,10,10,10,10,10,10,9,8,8,8,8,8,8,8,8,8,8,7,6,6,6,6,6,6,6,6,6,5,
			2,2,3,3,3,5,5,5,7,7,8,9,9,12,12,14,15,16,20,19,22,24,24,29,29,31,35,34,40,41,43,47,47,53,54,56,61,61,66,68,69,75,75,79,82,82,89,88,91,95,94,100,100,102,105,105,109,109,110,113,113,115,115,116,118,118,119,119,118,120,120,119,119,118,119,118,117,116,115,115,115,113,111,110,109,109,107,104,103,102,101,100,95,95,94,93,92,86,86,85,84,83,77,77,75,74,74,68,67,66,65,65,60,59,58,57,56,52,50,50,49,48,45,43,43,41,41,38,36,36,35,35,33,30,30,29,29,27,25,25,24,24,23,21,21,20,20,19,17,17,16,16,16,14,14,13,13,13,11,11,11,11,11,9,9,9,9,9,7,7,7,7,7,6,6,6,5,5,5,4,4,4,4,4,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
			2,2,3,3,4,5,7,7,9,10,13,14,17,19,23,25,29,32,37,40,44,50,55,60,65,72,76,82,90,96,98,109,113,120,123,133,135,142,148,155,154,163,165,171,170,177,176,180,181,185,181,183,183,184,182,180,179,177,176,173,170,167,163,160,158,151,147,144,141,134,131,128,120,117,115,109,103,101,97,90,87,85,78,75,73,69,63,62,60,54,51,50,46,43,41,40,35,34,33,30,27,27,25,22,21,21,17,17,17,15,14,13,12,11,10,10,9,8,8,7,6,6,6,5,5,4,4,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,
			3,3,5,6,8,11,13,18,22,26,34,38,50,56,67,77,86,105,111,129,139,154,173,179,201,208,223,239,242,260,262,272,279,280,284,283,284,283,281,272,268,262,254,249,233,226,216,203,197,181,172,163,148,142,129,119,114,99,95,86,77,73,62,58,53,46,44,37,34,31,26,25,21,19,17,14,13,11,10,9,7,7,6,5,5,3,3,3,2,2,1,1,1,1,1,0,0,
			3,5,6,11,15,20,30,41,49,67,85,104,125,154,173,203,237,266,282,320,338,360,384,392,398,408,410,407,398,382,371,355,335,315,290,267,244,227,198,180,158,143,121,111,90,82,67,61,47,44,33,31,23,22,15,14,10,10,6,6,4,4,2,2,1,1,1,1,
			4,7,11,17,29,45,63,84,115,158,204,245,287,348,398,454,483,510,541,554,558,547,520,493,467,423,393,332,295,259,217,193,145,126,101,81,69,47,40,29,23,18,12,10,6,5,4,2,2,1,
			5,9,18,32,55,86,126,184,260,342,434,502,577,656,702,728,709,688,654,594,527,449,376,305,253,197,141,110,84,55,42,30,18,13,9,5,3,2,1,
			5,12,26,53,99,167,266,374,505,640,768,873,898,893,862,786,677,556,441,343,241,174,121,80,51,30,19,11,6,3,1,
			6,17,42,95,187,327,499,685,883,1045,1136,1111,989,851,680,515,363,232,144,82,48,26,13,6,3,
			7,25,70,167,323,554,867,1155,1340,1339,1209,1006,750,515,316,175,93,46,21,9,3,
			9,34,106,265,597,999,1309,1559,1568,1341,998,600,326,164,73,29,10,3,
			10,47,155,480,954,1534,1863,1761,1419,934,492,220,83,29,8,
			12,63,271,747,1482,2114,2123,1564,946,452,159,46,11,2,
			14,80,433,1164,2159,2509,1912,1117,432,138,33,4,
			16,129,646,1827,2733,2455,1481,530,153,21,3,
			20,204,999,2419,3105,2170,841,211,24,
			22,278,1412,3159,3180,1579,314,49,3,
			23,399,2117,3944,2615,794,100,4,
			27,540,2812,4198,2033,363,23,
			28,774,3717,4135,1225,114,
			39,1098,4532,3650,649,28,
			53,1559,5217,2900,265,
			70,2052,5701,2057,116,
			99,2701,6087,1085,26,
			125,3322,5806,740,
			165,4162,5382,288,
			200,5506,4153,139,
			104,5932,3922,40,
			273,6561,3138,
			342,7817,1837,
			411,8188,1399,
			483,8853,662,
			405,9149,445,
			892,8503,603,
			1390,8558,
			1888,8073,
			2368,7606,
			2447,7551,
			2917,7081,
			2994,7004,
			1734,8263,
			4679,5320,
			5082,4917,
			5812,4187,
			6033,3966,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000,
			10000};
			
			IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
			List<string> rayModelsData = new List<string>();
			for (int disp = 1; disp < sensmodelindex.Length; disp++)
			{
				string s = "";
				for (int i = sensmodelindex[disp-1]; i < sensmodelindex[disp]; i++)
				{
					s += Convert.ToSingle(sensmodel[i] / 10000.0f, format);
					if (i < sensmodelindex[disp] - 1) s += ",";
				}
				rayModelsData.Add(s);
			}
			ray_model_interval_pixels = 1;
			LoadSensorModelData(rayModelsData, ray_model_interval_pixels);
		}
				
		#endregion

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
        public void LoadSensorModelData(
		    List<string> rayModelsData,
		    float ray_model_interval_pixels)
        {
            // get the maximum array size needed to store the data
            int ray_model_normal_length = 10;
            for (int i = 0; i < rayModelsData.Count; i++)
            {
                string[] dataStr = rayModelsData[i].Split(',');
                if (dataStr.Length > ray_model_normal_length) ray_model_normal_length = dataStr.Length;
            }

            // initialise the ray model array
            init((int)(rayModelsData.Count * ray_model_interval_pixels) + 1, ray_model_normal_length);

            // insert the data into the array
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            for (int i = 0; i < rayModelsData.Count; i++)
            {
                int d = i + 2; // we add 2 here because the first two slots
                if (d < length.Length)
                {
					if (rayModelsData[i] != "")
					{
	                    // corresponding to zero and 0.5 pixel disparity don't exist
	                    string[] dataStr = rayModelsData[i].Split(',');
	                    length[d] = dataStr.Length;
	                    for (int j = 0; j < dataStr.Length; j++)
						{
	                        probability[d][j] = Convert.ToSingle(dataStr[j], format);
						}
					}
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
                LoadSensorModelData(rayModelsData, ray_model_interval_pixels);

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
                ray_model_interval_pixels = Convert.ToSingle(xnod.InnerText);
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
