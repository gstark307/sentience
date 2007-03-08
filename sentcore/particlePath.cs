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
    /// stores a series of robot poses
    /// </summary>
    public class particlePath
    {
        // maximum history to store for path IDs within each particlePose 
        const int MAX_PATH_HISTORY = 500;

        // a unique ID for this path
        public UInt32 ID;

        // the index in the path arraylist at which the parent path connects to this path
        public particlePose branch_pose = null;

        // the current pose
        public particlePose current_pose = null;

        // the maximum number of poses which may be stored within the path
        public int max_length;

        // list of poses within the path
        public ArrayList path;

        // total score for all poses within the path
        public float total_score = 0;
        public int total_poses = 0;

        public particlePath(int max_length)
        {
            this.max_length = max_length;
            path = new ArrayList();
        }

        public particlePath(particlePath parent, UInt32 path_ID)
        {
            ID = path_ID;
            parent.current_pose.no_of_children++;
            current_pose = parent.current_pose;
            branch_pose = parent.current_pose;

            this.max_length = parent.max_length;            
            path = new ArrayList();
            total_score = parent.total_score;
            total_poses = parent.total_poses;
        }

        public particlePath(float x, float y, float pan,
                            int max_length, UInt32 time_step, UInt32 path_ID)
        {
            ID = path_ID;
            this.max_length = max_length;
            path = new ArrayList();
            particlePose pose = new particlePose(x, y, pan, ID);
            pose.time_step = time_step;
            Add(pose);
        }

        public void Add(particlePose pose)
        {
            // set the parent of this pose
            pose.parent = current_pose;

            // update the list of previous path IDs
            if (branch_pose != null)
            {
                if (current_pose == branch_pose)
                {
                    pose.previous_paths = new ArrayList();
                    int min = branch_pose.previous_paths.Count - MAX_PATH_HISTORY;
                    if (min < 0) min = 0;
                    for (int i = min; i < branch_pose.previous_paths.Count; i++)
                        pose.previous_paths.Add((UInt32)branch_pose.previous_paths[i]);
                    pose.previous_paths.Add(pose.path_ID);
                }
                else pose.previous_paths = pose.parent.previous_paths;
            }
            else
            {
                if (pose.parent == null)
                {
                    pose.previous_paths = new ArrayList();
                    pose.previous_paths.Add(pose.path_ID);
                }
                else pose.previous_paths = pose.parent.previous_paths;
            }

            // set the current pose
            current_pose = pose;

            // add the pose to the path
            path.Add(pose);

            total_score += pose.score;
            total_poses++;

            // ensure that the path does not exceed a maximum length
            if (path.Count > max_length)
                path.RemoveAt(0);
        }

        /// <summary>
        /// remove the mapping particles associated with this path
        /// </summary>
        public bool Remove(occupancygridMultiHypothesis grid)
        {
            UInt32 path_ID = 0;
            particlePose pose = current_pose;
            if (current_pose != null)
            {
                path_ID = current_pose.path_ID;
                while ((pose != branch_pose) && (path_ID == pose.path_ID))
                {
                    pose.Remove(grid);
                    if (path_ID == pose.path_ID) pose = pose.parent;
                }
                if (pose != null)
                {
                    pose.no_of_children--;
                    if (pose.no_of_children == 0)
                    {
                        // there are no children remaining, so label the previous path
                        // with the current path ID
                        int children = 0;
                        while ((pose != null) && (children < 1))
                        {
                            children = pose.no_of_children;
                            pose.path_ID = path_ID;
                            if (children < 1)
                            {
                                pose = pose.parent;
                            }
                        }
                    }
                }
            }
            if (pose == branch_pose)
                return (true);
            else
                return (false);
        }

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodePath = doc.CreateElement("RobotPath");
            parent.AppendChild(nodePath);

            util.AddComment(doc, nodePath, "The path through which the robot has moved, as an X,Y coordinate");
            util.AddComment(doc, nodePath, "in millimetres followed by the heading in degrees");
             
            for (int i = 0; i < path.Count; i++)
            {
                particlePose pose = (particlePose)path[i];
                util.AddTextElement(doc, nodePath, "Pose", Convert.ToString(pose.x) + "," +
                                                           Convert.ToString(pose.y) + "," +
                                                           Convert.ToString(pose.pan * 180 / Math.PI));
            }

            return (nodePath);
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

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                path.Clear();

                // recursively walk the node tree
                LoadFromXml(xnodDE, 0);

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

            if (xnod.Name == "Pose")
            {
                String[] poseStr = xnod.InnerText.Split(',');
                particlePose new_pose = new particlePose(Convert.ToSingle(poseStr[0]),
                                                         Convert.ToSingle(poseStr[1]),
                                                         Convert.ToSingle(poseStr[2])*(float)Math.PI/180.0f, 0);
                Add(new_pose);
            }


            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) &&
                ((xnod.Name == "RobotPath") ||
                 (xnod.Name == "Sentience")
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
