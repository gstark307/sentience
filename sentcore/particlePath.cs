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
using System.Collections.Generic;
using sluggish.utilities;
using sluggish.utilities.xml;

namespace sentience.core
{
    /// <summary>
    /// stores a series of robot poses
    /// </summary>
    public sealed class particlePath
    {
        // used to indicate whether this path is currently active
        // if false, this path may be garbage collected
        public bool Enabled;

        // this is set to true if the tree is totally collapsed at this point in the path
        public bool Collapsed;

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
        public List<particlePose> path;

        // total score for all poses within the path
        public float total_score = 0;
        public float local_score = 0;
        public int total_poses = 0;
        public int total_children = 0;

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="max_length">the maximum number of poses which may be stored within this path</param>
        public particlePath(int max_length)
        {
            this.max_length = max_length;
            path = new List<particlePose>();
            Enabled = true;
        }

        /// <summary>
        /// increment or decrement the total number of child branches supported by this path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="increment">increment of 1 or -1</param>
        private void incrementPathChildren(particlePath path, int increment)
        {
            path.total_children += increment;
            particlePath p = path;
            while (p.branch_pose != null)
            {
                p = p.branch_pose.path;
                p.total_children += increment;
            }
        }

        public particlePath(particlePath parent, UInt32 path_ID, int grid_dimension_cells)
        {
            ID = path_ID;
            parent.current_pose.no_of_children++;
            current_pose = parent.current_pose;
            branch_pose = parent.current_pose;

            incrementPathChildren(parent, 1);

            this.max_length = parent.max_length;
            path = new List<particlePose>();
            total_score = parent.total_score;
            total_poses = parent.total_poses;
            Enabled = true;

            // map cache for this path
            //map_cache = new ArrayList[grid_dimension_cells][][];
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x">x position in millimetres</param>
        /// <param name="y">y position in millimetres</param>
        /// <param name="pan">pan angle in radians</param>
        /// <param name="max_length">the maximum number of poses which may be stored within this path</param>
        /// <param name="time_step">time step</param>
        /// <param name="path_ID">ID number for the path</param>
        /// <param name="grid_dimension_cells">occupancy grid dimension</param>
        public particlePath(float x, float y, float pan,
                            int max_length, UInt32 time_step, UInt32 path_ID,
                            int grid_dimension_cells)
        {
            ID = path_ID;
            this.max_length = max_length;
            path = new List<particlePose>();
            particlePose pose = new particlePose(x, y, pan, this);
            pose.time_step = time_step;
            Enabled = true;
            Add(pose);
        }

        #endregion

        #region "map cache"

        // The map cache stores any grid cell hypotheses which have been observed
        // along this part of the path within a convenient lookup table.
        // It is used to answer the question "was this grid cell observed by this path?"
        // when retrieving occupancy probability for a given pose

        // grid map cache for quick lookup
        private List<particleGridCell>[][][] map_cache;
        private int map_cache_tx, map_cache_ty, map_cache_width_cells;

        /// <summary>
        /// TODO: changes the size of the map cache
        /// Note it may not be necessary to do this at all
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void resize_map_cache(int x, int y)
        {
            
        }

        /// <summary>
        /// adds a new hypothesis to the map cache
        /// </summary>
        /// <param name="hypothesis">grid hypothesis to be added</param>
        /// <param name="radius_cells">radius of the local map cache</param>
        /// <param name="grid_dimension">dimension of the occupancy grid</param>
        /// <param name="grid_dimension_vertical">height of the occupancy grid (z axis) in cells</param>
        /// <returns>true if the hypothesis was added</returns>
        public bool Add(particleGridCell hypothesis, 
                        int radius_cells, 
                        int grid_dimension, 
                        int grid_dimension_vertical)
        {
            bool added = true;

            // create a new list for this grid coordinate if necessary
            // allocating memory as its needed for each grid dimension
            // is far more efficient than just allocating a big three 
            // dimensional chunk in one go
            if (map_cache == null)
            {
                // store the top left position of the map cache
                map_cache_tx = hypothesis.x - radius_cells;
                map_cache_ty = hypothesis.y - radius_cells;

                // store the width of the map cache
                map_cache_width_cells = radius_cells * 2;
                map_cache = new List<particleGridCell>[map_cache_width_cells][][];
            }

            // get the x,y coordinate within the map cache
            int x = hypothesis.x - map_cache_tx;
            int y = hypothesis.y - map_cache_ty;

            if (((x < 0) || (x >= map_cache_width_cells)) ||
                ((y < 0) || (y >= map_cache_width_cells)))
            {
                // we're outside of the cache
                added = false;
                //resize_map_cache(x, y);
            }
            else
            {
                // add the hypothesis to the cache map
                int z = hypothesis.z;

                // create new lists as they are needed
                if (map_cache[x] == null)
                    map_cache[x] = new List<particleGridCell>[map_cache_width_cells][];

                if (map_cache[x][y] == null)
                    map_cache[x][y] = new List<particleGridCell>[grid_dimension_vertical];

                if (map_cache[x][y][z] == null)
                    map_cache[x][y][z] = new List<particleGridCell>();

                // add to the list
                map_cache[x][y][z].Add(hypothesis);
            }
            return (added);
        }


        /// <summary>
        /// returns the list of hypotheses for the given grid cell location
        /// This is used when calculating occupancy probability for a given pose
        /// </summary>
        /// <param name="x">x coordinate of the grid cell</param>
        /// <param name="y">y coordinate of the grid cell</param>
        /// <returns>Occupancy hypotheses</returns>
        public List<particleGridCell> GetHypotheses(int x, int y, int z)
        {
            int xx = x - map_cache_tx;
            if ((xx > -1) && (xx < map_cache_width_cells))
            {
                int yy = y - map_cache_ty;
                if ((yy > -1) && (yy < map_cache_width_cells))
                {
                    // most of time we will be returning nulls, so
                    // prioritise these conditions
                    if (map_cache == null)
                        return (null);
                    else
                    {
                        if (map_cache[xx] == null)
                            return (null);
                        else
                        {
                            if (map_cache[xx][yy] == null)
                                return (null);
                            else
                            {
                                if (map_cache[xx][yy][z] == null)
                                    return (null);
                                else
                                    return (map_cache[xx][yy][z]);
                            }
                        }
                    }
                }
                else return (null);
            }
            else return (null);
        }

        #endregion

        #region "adding new poses"

        /// <summary>
        /// add a new pose to the path
        /// </summary>
        /// <param name="pose"></param>
        public void Add(particlePose pose)
        {
            pose.path = this;

            // set the parent of this pose
            pose.parent = current_pose;

            // update the list of previous path IDs
            if (branch_pose != null)
            {
                if (current_pose == branch_pose)
                {
                    pose.previous_paths = new List<particlePath>();
                    int min = branch_pose.previous_paths.Count - MAX_PATH_HISTORY;
                    if (min < 0) min = 0;
                    for (int i = min; i < branch_pose.previous_paths.Count; i++)
                        pose.previous_paths.Add(branch_pose.previous_paths[i]);
                    pose.previous_paths.Add(this);
                }
                else pose.previous_paths = pose.parent.previous_paths;
            }
            else
            {
                if (pose.parent == null)
                {
                    pose.previous_paths = new List<particlePath>();
                    pose.previous_paths.Add(this);
                }
                else pose.previous_paths = pose.parent.previous_paths;
            }

            // set the current pose
            current_pose = pose;

            // add the pose to the path
            path.Add(pose);

            //total_score += pose.score;
            total_poses++;

            // ensure that the path does not exceed a maximum length
            if (path.Count > max_length)
            {
                // remove the oldest pose in the path
                path.RemoveAt(0);
            }
        }

        #endregion

        #region "returning velocity data"

        /// <summary>
        /// returns a list containing the robots velocity at each point in the path
        /// </summary>
        /// <param name="starting_forward_velocity">the forward velocity to start with in mm/sec</param>
        /// <param name="starting_angular_velocity">the angular velocity to start with in radians/sec</param>
        /// <param name="time_per_index_sec">time taken between indexes along the path in seconds</param>
        /// <returns></returns>
        public List<float> getVelocities(float starting_forward_velocity,
                                         float starting_angular_velocity,
                                         float time_per_index_sec)
        {
            List<float> result = new List<float>();

            float forward_velocity = starting_forward_velocity;
            float angular_velocity = starting_angular_velocity;
            for (int i = 1; i < path.Count; i++)
            {
                particlePose prev_pose = path[i-1];
                particlePose pose = path[i];

                float dx = pose.x - prev_pose.x;
                float dy = pose.y - prev_pose.y;
                float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float acceleration = (2 * (distance - (forward_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                forward_velocity = forward_velocity + (acceleration * time_per_index_sec);
                result.Add(forward_velocity);

                distance = pose.pan - prev_pose.pan;
                acceleration = (2 * (distance - (angular_velocity * time_per_index_sec))) / (time_per_index_sec * time_per_index_sec);
                acceleration /= time_per_index_sec;
                angular_velocity = angular_velocity + (acceleration * time_per_index_sec);
                result.Add(angular_velocity);
            }
            return (result);
        }

        #endregion

        #region "distillation"

        /// <summary>
        /// distill all grid particles within this path
        /// </summary>
        /// <param name="grid"></param>
        public void Distill(occupancygridMultiHypothesis grid)
        {
            particlePose pose = current_pose;
            while (pose != null)
            {
                pose.Distill(grid);
                pose = pose.parent;
            }
            map_cache = null;
            Enabled = false;
        }

        #endregion

        #region "removing branches"

        /// <summary>
        /// remove all poses along the path until a branch point is located
        /// </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        private particlePose CollapseBranch(particlePose pose,
                                            occupancygridMultiHypothesis grid)
        {
            int children = 0;
            while ((pose != branch_pose) &&
                   (pose.path == this) &&
                   (children == 0))
            {
                if (pose != current_pose)
                    children = pose.no_of_children;

                if (children == 0)
                    if (pose.path == this)
                    {
                        pose.Remove(grid);
                        pose = pose.parent;
                    }
            }

            return (pose);
        }

        /// <summary>
        /// remove the mapping particles associated with this path
        /// </summary>
        public bool Remove(occupancygridMultiHypothesis grid)
        {
            Enabled = false;

            particlePose pose = current_pose;
            if (current_pose != null)
            {
                // collapse this path down to the next branching point
                pose = CollapseBranch(pose, grid);
                if (pose != null)
                {
                    if (pose != current_pose)
                    {
                        if (pose.no_of_children > 0)
                        {
                            // reduce the number of children at the branch point
                            pose.no_of_children--;
                        }                        
                    }

                    // keep on truckin' down the line...
                    incrementPathChildren(pose.path, -1);
                    if (pose.path.total_children == 0)
                        pose.path.Remove(grid);
                }
            }

            return (!Enabled);
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show the path
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="min_x_mm"></param>
        /// <param name="min_y_mm"></param>
        /// <param name="max_x_mm"></param>
        /// <param name="max_y_mm"></param>
        /// <param name="clearBackground"></param>
        public void Show(Byte[] img, int width, int height,
                             int r, int g, int b, int line_thickness,
                             float min_x_mm, float min_y_mm,
                             float max_x_mm, float max_y_mm,
                             bool clearBackground, UInt32 root_time_step)
        {
            if (clearBackground)
            {
                for (int i = 0; i < width * height * 3; i++)
                    img[i] = 255;
            }

            int rr, gg, bb;
            int x = -1, y = -1;
            int prev_x = -1, prev_y = -1;
            if ((max_x_mm > min_x_mm) && (max_y_mm > min_y_mm))
            {
                particlePose pose = current_pose;
                if (pose != null)
                {
                    while (pose.parent != null)
                    {
                        if ((pose.time_step <= root_time_step) &&
                            (root_time_step != UInt32.MaxValue))
                        {
                            rr = 0;
                            gg = 255;
                            bb = 0;
                        }
                        else
                        {
                            rr = r;
                            gg = g;
                            bb = b;
                        }

                        x = (int)((pose.x - min_x_mm) * (width - 1) / (max_x_mm - min_x_mm));
                        if ((x > 0) && (x < width))
                        {
                            y = height - 1 - (int)((pose.y - min_y_mm) * (height - 1) / (max_y_mm - min_y_mm));
                            if ((y < 0) || (y >= height)) y = -1;
                        }
                        else x = -1;
                        if ((x > -1) && (y > -1))
                        {
                            if (prev_x > -1)
                                drawing.drawLine(img, width, height, x, y, prev_x, prev_y, rr, gg, bb, line_thickness, false);

                            prev_x = x;
                            prev_y = y;
                        }

                        // move along, nothing to see...
                        pose = pose.parent;
                    }
                }
            }
        }

        #endregion

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodePath = doc.CreateElement("RobotPath");
            parent.AppendChild(nodePath);

            xml.AddComment(doc, nodePath, "The path through which the robot has moved, as an X,Y coordinate");
            xml.AddComment(doc, nodePath, "in millimetres followed by the heading in degrees");

            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            for (int i = 0; i < path.Count; i++)
            {
                particlePose pose = path[i];
                xml.AddTextElement(doc, nodePath, "Pose", Convert.ToString(pose.x, format) + "," +
                                                           Convert.ToString(pose.y, format) + "," +
                                                           Convert.ToString(pose.pan * 180 / Math.PI, format));
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
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String[] poseStr = xnod.InnerText.Split(',');
                particlePose new_pose = new particlePose(Convert.ToSingle(poseStr[0], format),
                                                         Convert.ToSingle(poseStr[1], format),
                                                         Convert.ToSingle(poseStr[2], format)*(float)Math.PI/180.0f, null);
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
