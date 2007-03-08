/*
    Sentience 3D Perception System
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
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    /// <summary>
    /// a class used for simulating a robot moving along a path
    /// </summary>
    public class simulation
    {
        public String Name = "Simulation of My Robot";

        // name of the design file for the robot
        public String RobotDesignFile = "robotdesign.xml";
        
        // path where images can be located
        public String ImagesPath = "";

        // stores individual poses along the path
        public particlePath path = null;

        // segments which make up the path
        public ArrayList pathSegments = null;

        public robot rob;

        #region "results of the simulation"

        // resolution of the images returned as results
        public int results_image_width = 640;
        public int results_image_height = 480;

        // image showing the path through which the robot has moved, together with
        // pose uncertainty
        public Byte[] robot_path;

        // occupancy grid map formed during the simulation
        public Byte[] grid_map;

        #endregion

        /// <summary>
        /// reset the simulation
        /// </summary>
        public void Reset()
        {
            // create the robot object
            rob = new robot();

            // load the design file
            rob.Load(RobotDesignFile);
        }

        public simulation(String RobotDesignFile, String ImagesPath)
        {
            this.RobotDesignFile = RobotDesignFile;
            this.ImagesPath = ImagesPath;
            pathSegments = new ArrayList();
            Reset();
        }


        /// <summary>
        /// add a path segment
        /// </summary>
        /// <param name="x">x coordinate in millimetres</param>
        /// <param name="y">y coordinate in millimetres</param>
        /// <param name="pan">heading in radians</param>
        public void Add(float x, float y, float pan,
                        int no_of_steps, float dist_per_step_mm,
                        float pan_per_step)
        {
            simulationPathSegment segment = new simulationPathSegment(x, y, pan, no_of_steps, dist_per_step_mm, pan_per_step);
            pathSegments.Add(segment);

            // create a path if needed
            if (path == null) path = new particlePath(999999999);

            // update the list of poses
            ArrayList poses = segment.getPoses();
            for (int i = 0; i < poses.Count; i++)
            {
                particlePose pose = (particlePose)poses[i];
                path.Add(pose);
            }
        }


        public void Run()
        {
        }


        #region "saving and loading"

        private String[] temp_poseStr;
        private int temp_no_of_steps;
        private float temp_dist_per_step;

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeSimulation = doc.CreateElement("Simulation");
            parent.AppendChild(nodeSimulation);

            util.AddComment(doc, nodeSimulation, "Name");
            util.AddTextElement(doc, nodeSimulation, "Name", Name);

            util.AddComment(doc, nodeSimulation, "The path and filename for the xml file which contains the robot design definition");
            util.AddTextElement(doc, nodeSimulation, "RobotDesignFile", RobotDesignFile);

            util.AddComment(doc, nodeSimulation, "Path where the stereo images can be found");
            util.AddTextElement(doc, nodeSimulation, "ImagesPath", ImagesPath);

            if (pathSegments != null)
            {
                XmlElement nodePath = doc.CreateElement("RobotPath");
                parent.AppendChild(nodePath);

                for (int i = 0; i < pathSegments.Count; i++)
                {
                    simulationPathSegment segment = (simulationPathSegment)pathSegments[i];
                    util.AddComment(doc, nodePath, "The initial pose of the robot at the beginning of this path segment");
                    util.AddComment(doc, nodePath, "X,Y position in millimetres, followed by heading in degrees");
                    util.AddTextElement(doc, nodePath, "InitialPose", Convert.ToString(segment.x) + "," +
                                                               Convert.ToString(segment.y) + "," +
                                                               Convert.ToString(segment.pan * 180.0f / Math.PI));
                    util.AddComment(doc, nodePath, "The number of steps which this segment consists of");
                    util.AddTextElement(doc, nodePath, "NumberOfSteps", Convert.ToString(segment.no_of_steps));
                    util.AddComment(doc, nodePath, "The distance of each step in millimetres");
                    util.AddTextElement(doc, nodePath, "StepSizeMillimetres", Convert.ToString(segment.distance_per_step_mm));
                    util.AddComment(doc, nodePath, "The change in heading per step in degrees");
                    util.AddTextElement(doc, nodePath, "HeadingChangePerStep", Convert.ToString(segment.pan_per_step));
                }
                
            }

            return (nodeSimulation);
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

                pathSegments = new ArrayList();

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
                LoadFromXml(xnodDE, 0);

                Reset();

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
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "Name")
            {
                Name = xnod.InnerText;
            }

            if (xnod.Name == "RobotDesignFile")
            {
                RobotDesignFile = xnod.InnerText;
            }

            if (xnod.Name == "ImagesPath")
            {
                ImagesPath = xnod.InnerText;
            }

            if (xnod.Name == "InitialPose")
            {
                temp_poseStr = xnod.InnerText.Split(',');
            }
            if (xnod.Name == "NumberOfSteps")
            {
                temp_no_of_steps = Convert.ToInt32(xnod.InnerText);
            }
            if (xnod.Name == "StepSizeMillimetres")
            {
                temp_dist_per_step = Convert.ToSingle(xnod.InnerText);
            }
            if (xnod.Name == "HeadingChangePerStep")
            {
                float temp_pan_per_step = Convert.ToSingle(xnod.InnerText);
                Add(Convert.ToSingle(temp_poseStr[0]), Convert.ToSingle(temp_poseStr[1]), Convert.ToSingle(temp_poseStr[2]),
                    temp_no_of_steps, temp_dist_per_step, temp_pan_per_step);
            }
            

            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) &&
                ((xnod.Name == "Simulation") ||
                 (xnod.Name == "Sentience") ||
                 (xnod.Name == "RobotPath")
                 ))
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }
}
