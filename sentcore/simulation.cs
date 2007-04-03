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

        // a list containing the forward and angular velocity values along the path
        ArrayList velocities;

        // time which elapses per step in seconds
        public float time_per_index_sec = 2;

        // the current time step which the simulation is on
        public int current_time_step = 0;

        // position error between where the robot believes it is and 
        // where it actually is according to the path data
        public float position_error_mm;


        private float min_x=0, min_y=0, max_x=0, max_y=0;

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
        /// set the tuning parameters from a comma separated string
        /// </summary>
        /// <param name="parameters">comma separated tuning parameters</param>
        public void SetTuningParameters(String parameters)
        {
            rob.SetTuningParameters(parameters);
        }

        /// <summary>
        /// returns a comma separated list of tuning parameters
        /// </summary>
        /// <returns></returns>
        public String GetTuningParameters()
        {
            return (rob.TuningParameters);
        }

        /// <summary>
        /// reset the simulation
        /// Note that the path or pathSegment data is not reset
        /// </summary>
        public void Reset()
        {
            // create the robot object
            rob = new robot();

            // load the design file
            rob.Load(RobotDesignFile);

            current_time_step = 0;
            position_error_mm = 0;
            updatePath();
        }

        public simulation(String RobotDesignFile, String ImagesPath)
        {
            this.RobotDesignFile = RobotDesignFile;
            this.ImagesPath = ImagesPath;
            pathSegments = new ArrayList();
            Reset();
        }

        /// <summary>
        /// show the path through which the robot moves
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ShowPath(Byte[] img, int width, int height)
        {
            float min_xx = min_x;
            float max_xx = max_x;
            if (max_xx - min_xx < 10)
            {
                min_xx = -5;
                max_xx = 5;
            }
            float min_yy = min_y;
            float max_yy = max_y;
            if (max_yy - min_yy < 10)
            {
                min_yy = -5;
                max_yy = 5;
            }

            if (path != null)
                path.Show(img, width, height,
                      0, 0, 0, 1, min_xx-100, min_yy-100, max_xx+100, max_yy+100, true, 0);
        }

        /// <summary>
        /// show the motion uncertainty
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="clear_background"></param>
        public void ShowMotionUncertainty(Byte[] img, int width, int height, 
                                          bool clear_background)
        {
            float min_xx = min_x;
            float max_xx = max_x;
            if (max_xx - min_xx < 10)
            {
                min_xx = -5;
                max_xx = 5;
            }
            float min_yy = min_y;
            float max_yy = max_y;
            if (max_yy - min_yy < 10)
            {
                min_yy = -5;
                max_yy = 5;
            }

            if (clear_background)
                rob.motion.Show(img, width, height,
                                min_xx-100, min_yy-100, max_xx+100, max_yy+100,
                                clear_background);
        }


        /// <summary>
        /// show the best pose
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="clear_background"></param>
        public void ShowBestPose(Byte[] img, int width, int height,
                                 bool clear_background)
        {
            rob.motion.ShowBestPose(img, width, height,
                                    clear_background, true);
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
            updatePath();
        }

        /// <summary>
        /// turns a list of path segments into a list of individual poses
        /// </summary>
        private void updatePath()
        {
            particlePose prev_pose=null;
            path = new particlePath(999999999);

            min_x = 9999;
            min_y = 9999;
            max_x = -9999;
            max_y = -9999;
            for (int s = 0; s < pathSegments.Count; s++)
            {
                simulationPathSegment segment = (simulationPathSegment)pathSegments[s];

                // get the last pose
                if (s > 0)
                    prev_pose = (particlePose)path.path[path.path.Count - 1];

                // update the list of poses
                ArrayList poses = segment.getPoses();

                if (prev_pose != null)
                {
                    // is the last pose position the same as the first in this segment?
                    // if so, remove the last pose added to the path
                    particlePose firstPose = (particlePose)poses[0];
                    if (((int)firstPose.x == (int)prev_pose.x) &&
                        ((int)firstPose.y == (int)prev_pose.y) &&
                        (Math.Abs(firstPose.pan - prev_pose.pan) < 0.01f))
                        path.path.RemoveAt(path.path.Count - 1);
                }

                for (int i = 0; i < poses.Count; i++)
                {
                    particlePose pose = (particlePose)poses[i];
                    if (pose.x < min_x) min_x = pose.x;
                    if (pose.y < min_y) min_y = pose.y;
                    if (pose.x > max_x) max_x = pose.x;
                    if (pose.y > max_y) max_y = pose.y;
                    path.Add(pose);
                }
            }

            // update the path velocities
            velocities = path.getVelocities(0, 0, time_per_index_sec);
        }

        /// <summary>
        /// removes a path segment
        /// </summary>
        /// <param name="index"></param>
        public void RemoveSegment(int index)
        {
            if (index < pathSegments.Count)
            {
                pathSegments.RemoveAt(index);
                updatePath();
            }
        }



        /// <summary>
        /// run the simulation, one step at a time
        /// </summary>
        /// <param name="images">stereo image bitmaps for this time step</param>
        public void RunOneStep(ArrayList images)
        {
            if (path != null)
            {
                // position the grid so that the path fits inside it
                rob.LocalGrid.x = (int)(min_x + ((max_x - min_x) / 2));
                rob.LocalGrid.y = (int)(min_y + ((max_y - min_y) / 2));

                if (images.Count > 1)
                {
                    float forward_velocity = (float)velocities[current_time_step * 2];
                    float angular_velocity = (float)velocities[(current_time_step * 2) + 1];
                    
                    // get the robots position according to the path data
                    float actual_x = ((particlePose)path.path[current_time_step]).x;
                    float actual_y = ((particlePose)path.path[current_time_step]).y;

                    rob.updateFromVelocities(images, forward_velocity, angular_velocity, time_per_index_sec);

                    // calculate the position error
                    float err_x = actual_x - rob.x;
                    float err_y = actual_y - rob.y;
                    position_error_mm = (float)Math.Sqrt((err_x * err_x) + (err_y * err_y));
                }

                // increment the simulation time step
                if (current_time_step < path.path.Count-2)
                    current_time_step++;
            }
        }

        /// <summary>
        /// returns the average colour variance for the grid
        /// </summary>
        /// <returns></returns>
        public float GetMeanColourVariance()
        {
            return (rob.GetMeanColourVariance());
        }

        /// <summary>
        /// returns a list of performance benchmarks
        /// </summary>
        /// <returns></returns>
        public ArrayList GetBenchmarks()
        {
            ArrayList benchmarks = new ArrayList();
            benchmarks.Add("Robot position " + Convert.ToString((int)rob.x) + ", " +
                                               Convert.ToString((int)rob.y));

            benchmarks.Add("Grid particles " + Convert.ToString(rob.LocalGrid.total_valid_hypotheses));
            benchmarks.Add("Garbage        " + Convert.ToString(rob.LocalGrid.total_garbage_hypotheses));

            benchmarks.Add("Stereo correspondence  " + Convert.ToString(rob.benchmark_stereo_correspondence) + " mS");
            benchmarks.Add("Observation update     " + Convert.ToString(rob.benchmark_observation_update) + " mS");
            benchmarks.Add("Prediction             " + Convert.ToString(rob.benchmark_prediction) + " mS");
            benchmarks.Add("Garbage collection     " + Convert.ToString(rob.benchmark_garbage_collection) + " mS");

            return (benchmarks);
        }


        /// <summary>
        /// show the occupancy grid
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ShowGrid(int view_type, Byte[] img, int width, int height, bool show_robot)
        {
            rob.ShowGrid(view_type, img, width, height, show_robot, false, true);
        }

        public void ShowPathTree(Byte[] img, int width, int height)
        {
            rob.ShowPathTree(img, width, height);
        }


        #region "saving and loading"

        private String[] temp_poseStr;
        private int temp_no_of_steps;
        private float temp_dist_per_step;

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeSimulation = doc.CreateElement("Simulation");
            parent.AppendChild(nodeSimulation);

            util.AddComment(doc, nodeSimulation, "Name or title of the simulation");
            util.AddTextElement(doc, nodeSimulation, "SimulationName", Name);

            util.AddComment(doc, nodeSimulation, "The path and filename for the xml file which contains the robot design definition");
            util.AddTextElement(doc, nodeSimulation, "RobotDesignFile", RobotDesignFile);

            util.AddComment(doc, nodeSimulation, "Path where the stereo images can be found");
            util.AddTextElement(doc, nodeSimulation, "ImagesPath", ImagesPath);

            util.AddComment(doc, nodeSimulation, "The time which elapses for each step along the path in seconds");
            util.AddTextElement(doc, nodeSimulation, "SimulationTimeStepSeconds", Convert.ToString(time_per_index_sec));

            if (pathSegments != null)
            {
                XmlElement nodePath = doc.CreateElement("RobotPath");
                nodeSimulation.AppendChild(nodePath);

                for (int i = 0; i < pathSegments.Count; i++)
                {
                    simulationPathSegment segment = (simulationPathSegment)pathSegments[i];

                    XmlElement nodePathSegment = doc.CreateElement("PathSegment");
                    nodePath.AppendChild(nodePathSegment);

                    util.AddComment(doc, nodePathSegment, "The initial pose of the robot at the beginning of this path segment");
                    util.AddComment(doc, nodePathSegment, "X,Y position in millimetres, followed by heading in degrees");
                    util.AddTextElement(doc, nodePathSegment, "InitialPose", Convert.ToString(segment.x) + "," +
                                                               Convert.ToString(segment.y) + "," +
                                                               Convert.ToString(segment.pan * 180.0f / Math.PI));
                    util.AddComment(doc, nodePathSegment, "The number of steps which this segment consists of");
                    util.AddTextElement(doc, nodePathSegment, "NumberOfSteps", Convert.ToString(segment.no_of_steps));
                    util.AddComment(doc, nodePathSegment, "The distance of each step in millimetres");
                    util.AddTextElement(doc, nodePathSegment, "StepSizeMillimetres", Convert.ToString(segment.distance_per_step_mm));
                    util.AddComment(doc, nodePathSegment, "The change in heading per step in degrees");
                    util.AddTextElement(doc, nodePathSegment, "HeadingChangePerStep", Convert.ToString(segment.pan_per_step * 180.0f / Math.PI));
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

            if (xnod.Name == "SimulationName")
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

            if (xnod.Name == "SimulationTimeStepSeconds")
            {
                time_per_index_sec = Convert.ToSingle(xnod.InnerText);
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
                float temp_pan_per_step = Convert.ToSingle(xnod.InnerText) * (float)Math.PI / 180.0f;
                Add(Convert.ToSingle(temp_poseStr[0]), Convert.ToSingle(temp_poseStr[1]), Convert.ToSingle(temp_poseStr[2])*(float)Math.PI/180.0f,
                    temp_no_of_steps, temp_dist_per_step, temp_pan_per_step);
            }
            

            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) &&
                ((xnod.Name == "Simulation") ||
                 (xnod.Name == "Sentience") ||
                 (xnod.Name == "RobotPath") ||
                 (xnod.Name == "PathSegment")
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
