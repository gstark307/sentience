/*
    Motion model used to predict the next step
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
    public class motionModel
    {
        // the maximum length of paths containing possible poses
        const int max_path_length = 500;

        // time step index which may be assigned to poses
        private UInt32 time_step = 0;

        // counter used to uniquely identify paths
        private UInt32 path_ID = 0;

        // the most probable path taken by the robot
        public particlePath best_path = null;

        private Random rnd = new Random();
        private robot rob;

        // have the pose scores been updated?
        public bool PosesEvaluated;

        public const int INPUTTYPE_WHEEL_ANGULAR_VELOCITY = 0;
        public const int INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY = 1;
        public int InputType = INPUTTYPE_BODY_FORWARD_AND_ANGULAR_VELOCITY;

        // a list of possible poses
        public int survey_trial_poses = 200;   // the number of possible poses to keep track of

        // the number of updates after which a pose is considered to be mature
        /// This is so that poses which may initially be unfairly judged as 
        /// improbable have a few time steps to prove their worth
        public int pose_maturation = 2;        

        // list of poses
        public ArrayList Poses;

        // stores all active paths so that they're not garbage collected
        private ArrayList ActivePoses;

        // pose score threshold in the range 0-100 below which low probability poses are pruned
        public int cull_threshold = 75;

        // standard deviation values for noise within the motion model
        public float[] motion_noise;

        // angular velocity of the wheels in radians/sec
        public float LeftWheelAngularVelocity = 0;
        public float RightWheelAngularVelocity = 0;

        // speed in mm / sec
        public float forward_velocity = 0;

        // forward acceleration in mm/sec^2
        public float forward_acceleration = 0;

        // angular speed in radians / sec
        public float angular_velocity = 0;

        // angular acceleration in radians/sec^2
        public float angular_acceleration = 0;

        private bool initialised = false;

        #region "initialisation"

        public motionModel(robot rob)
        {
            this.rob = rob;
            Poses = new ArrayList();
            ActivePoses = new ArrayList();
            motion_noise = new float[6];

            // some default noise values
            int i = 0;
            while (i < 2)
            {
                motion_noise[i] = 0.08f;
                i++;
            }
            while (i < 4)
            {
                motion_noise[i] = 0.00025f;
                i++;
            }
            while (i < motion_noise.Length)
            {
                motion_noise[i] = 0.00005f;
                i++;
            }

            // create some initial poses
            createNewPoses(rob.x, rob.y, rob.pan);
        }

        #endregion

        #region "creation of new paths"

        /// <summary>
        /// add a new path to the list
        /// </summary>
        /// <param name="path"></param>
        private void createNewPose(particlePath path)
        {
            Poses.Add(path);
            ActivePoses.Add(path);

            // increment the path ID
            path_ID++;
            if (path_ID > UInt32.MaxValue - 10) path_ID = 0; // rollover
        }

        /// <summary>
        /// creates some new poses
        /// </summary>
        private void createNewPoses(float x, float y, float pan)
        {
            for (int sample = Poses.Count; sample < survey_trial_poses; sample++)
            {
                particlePath path = new particlePath(x, y, pan, max_path_length, time_step, path_ID);
                createNewPose(path);
            }
        }


        #endregion

        #region "apply motion model"

        /// <summary>
        /// return a random gaussian distributed value
        /// </summary>
        /// <param name="b"></param>
        private float sample_normal_distribution(float b)
        {
            float value = 0;

            for (int i = 0; i < 12; i++)
                value += ((rnd.Next(200000) / 100000.0f) - 1.0f) * b;

            return(value / 2.0f);
        }

        /// <summary>
        /// update the given pose using the motion model
        /// </summary>
        /// <param name="pose"></param>
        /// <param name="time_elapsed_sec"></param>
        private void sample_motion_model_velocity(particlePath path, float time_elapsed_sec)
        {
            // calculate noisy velocities
            float fwd_velocity = forward_velocity + sample_normal_distribution(
                (motion_noise[0] * Math.Abs(forward_velocity)) +
                (motion_noise[1] * Math.Abs(angular_velocity)));

            float ang_velocity = angular_velocity + sample_normal_distribution(
                (motion_noise[2] * Math.Abs(forward_velocity)) +
                (motion_noise[3] * Math.Abs(angular_velocity)));

            float v = sample_normal_distribution(
                (motion_noise[4] * Math.Abs(forward_velocity)) +
                (motion_noise[5] * Math.Abs(angular_velocity)));

            float fraction = 0;
            if (Math.Abs(ang_velocity) > 0.000001f) fraction = fwd_velocity / ang_velocity;
            float pan2 = path.current_pose.pan + (ang_velocity * time_elapsed_sec);

            // update the pose
            float new_y = path.current_pose.y - (fraction * (float)Math.Sin(path.current_pose.pan)) +
                              (fraction * (float)Math.Sin(pan2));
            float new_x = path.current_pose.x + (fraction * (float)Math.Cos(path.current_pose.pan)) -
                              (fraction * (float)Math.Cos(pan2));
            float new_pan = pan2 + (v * time_elapsed_sec);

            particlePose new_pose = new particlePose(new_x, new_y, new_pan, path.ID);
            new_pose.time_step = time_step;
            path.Add(new_pose);
        }

        /// <summary>
        /// removes low probability poses
        /// Note that a maturity threshold is used, so that poses which may initially 
        /// be unfairly judged as improbable have time to prove their worth
        /// </summary>
        private void Prune()
        {
            best_path = null;

            // gather mature poses
            float max_score = 0;
            float min_score = float.MaxValue;
            ArrayList maturePoses = new ArrayList();
            for (int sample = Poses.Count-1; sample >= 0; sample--)
            {
                particlePath path = (particlePath)Poses[sample];
                particlePose pose = path.current_pose;

                // use poses which are considered to be mature, or which
                // have a negative score.  A negative pose score indicates
                // that it has collided with occupied space within a grid map
                if ((path.path.Count >= pose_maturation) || (pose.score <= 0))
                {
                    // store the index of the pose
                    maturePoses.Add(sample);
                }

                // record the maximum score
                // Note that the score for each pose should be calculated as a running average
                float score = pose.score;
                if (score > max_score)
                {
                    max_score = score;
                    best_path = path;
                }
                else
                {
                    if (score < min_score)
                        min_score = score;
                }
            }

            // remove mature poses with a score below the cull threshold
            float threshold = min_score + ((max_score-min_score) * cull_threshold / 100);
            for (int mature = 0; mature < maturePoses.Count; mature++)
            {
                // index of the mature pose
                int index = (int)maturePoses[mature];

                // get the score for this pose
                particlePath path = (particlePath)Poses[index];
                float score = path.current_pose.score;

                // remove the pose if it's below the culling threshold
                if (score < threshold)
                {
                    // remove mapping hypotheses for this path
                    if (path.Remove(rob.LocalGrid))
                        ActivePoses.Remove(path);

                    // now remove the path itself
                    Poses.RemoveAt(index);
                }
            }

            if (best_path != null)
            {
                // choose a good pose, but not necessarily the best
                // this avoids too much elitism
                int new_poses_required = survey_trial_poses - Poses.Count;
                int max = Poses.Count;
                //float score_threshold = best_path.current_pose.score * 50 / 100;
                while (new_poses_required > 0)
                {
                    particlePath path = (particlePath)Poses[rnd.Next(max)];

                    particlePath p = new particlePath(path, path_ID);
                    createNewPose(p);
                    new_poses_required--;                        
                }

                // create new poses to maintain the population
                //createNewPoses(best);

                // update the robot position with the best available pose
                rob.x = best_path.current_pose.x;
                rob.y = best_path.current_pose.y;
                rob.pan = best_path.current_pose.pan;
            }

            PosesEvaluated = false;
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show the position uncertainty distribution
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Show(Byte[] img, int width, int height,
                         bool clearBackground)
        {
            float min_x = 99999;
            float min_y = 99999;
            float max_x = -99999;
            float max_y = -99999;
            for (int sample = 0; sample < Poses.Count; sample++)
            {
                particlePose pose = ((particlePath)Poses[sample]).current_pose;
                if (pose.x < min_x) min_x = pose.x;
                if (pose.y < min_y) min_y = pose.y;
                if (pose.x > max_x) max_x = pose.x;
                if (pose.y > max_y) max_y = pose.y;
            }
            Show(img, width, height, min_x, min_y, max_x, max_y, clearBackground);
        }

        /// <summary>
        /// show the position uncertainty distribution within the given region
        /// </summary>
        /// <param name="img">bitmap image (3bpp)</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="min_x_mm"></param>
        /// <param name="min_y_mm"></param>
        /// <param name="max_x_mm"></param>
        /// <param name="max_y_mm"></param>
        public void Show(Byte[] img, int width, int height,
                         float min_x_mm, float min_y_mm,
                         float max_x_mm, float max_y_mm,
                         bool clearBackground)
        {
            if (clearBackground)
            {
                // clear the image
                for (int i = 0; i < width * height * 3; i++)
                    img[i] = (Byte)250;
            }

            if ((max_x_mm > min_x_mm) && (max_y_mm > min_y_mm))
            {
                for (int sample = 0; sample < Poses.Count; sample++)
                {
                    particlePose pose = ((particlePath)Poses[sample]).current_pose;
                    int x = (int)((pose.x - min_x_mm) * (width - 1) / (max_x_mm - min_x_mm));
                    if ((x > 0) && (x < width))
                    {
                        int y = height - 1 - (int)((pose.y - min_y_mm) * (height - 1) / (max_y_mm - min_y_mm));
                        if ((y > 0) && (y < height))
                        {
                            int n = ((y * width) + x) * 3;
                            for (int col = 0; col < 3; col++)
                                img[n + col] = (Byte)0;
                        }
                    }
                }
            }

            // show the path
            ShowPath(img, width, height, 0, 255, 0, 0, min_x_mm, min_y_mm, max_x_mm, max_y_mm, false);
        }


        /// <summary>
        /// show the most probable path taken by the robot
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="min_x_mm"></param>
        /// <param name="min_y_mm"></param>
        /// <param name="max_x_mm"></param>
        /// <param name="max_y_mm"></param>
        /// <param name="clearBackground"></param>
        public void ShowPath(Byte[] img, int width, int height,
                             int r, int g, int b, int line_thickness,
                             float min_x_mm, float min_y_mm,
                             float max_x_mm, float max_y_mm,
                             bool clearBackground)
        {
            if (best_path != null)
                best_path.Show(img, width, height, r, g, b, line_thickness,
                               min_x_mm, min_y_mm, max_x_mm, max_y_mm, 
                               clearBackground);
        }

        #endregion

        #region "update poses"

        /// <summary>
        /// updates the given pose score using a running average
        /// Using a running average prevents dinosaurs who have been around for a long
        /// time from dominating the pose list despite recent poor performances.  
        /// Successful poses must have received high scores relatively recently.
        /// </summary>
        /// <param name="pose"></param>
        /// <param name="new_score"></param>
        public void updatePoseScore(particlePath path, float new_score)
        {
            float fraction = 1.0f / (float)pose_maturation;
            path.current_pose.score = (path.current_pose.score * (1.0f - fraction)) + (new_score * fraction);
        }

        /// <summary>
        /// resets the pose list to the current known robot position
        /// </summary>
        public void Reset()
        {
            // create some initial poses
            Poses.Clear();
            createNewPoses(rob.x, rob.y, rob.pan);
            initialised = true;
        }

        /// <summary>
        /// predict the next time step
        /// </summary>
        public void Predict(float time_elapsed_sec)
        {
            if (time_elapsed_sec > 0)
            {
                if (!initialised) Reset();

                // remove low probability poses
                if (PosesEvaluated) Prune();
                
                if (InputType == INPUTTYPE_WHEEL_ANGULAR_VELOCITY)
                {
                    // deterministic prediction of angular and forward velocities from
                    // left and right wheel angular velocities
                    float WheelRadius = rob.WheelDiameter_mm / 2;
                    angular_velocity = ((WheelRadius / (2 * rob.WheelBase_mm)) * (RightWheelAngularVelocity - LeftWheelAngularVelocity));
                    rob.pan += angular_velocity / time_elapsed_sec;
                    forward_velocity = ((WheelRadius / 2) * (RightWheelAngularVelocity + LeftWheelAngularVelocity)) / time_elapsed_sec;
                }

                // update poses
                for (int sample = 0; sample < Poses.Count; sample++)
                    sample_motion_model_velocity((particlePath)Poses[sample], time_elapsed_sec);

                time_step++;

                // check for rollovers
                if (time_step > UInt32.MaxValue - 10)
                    time_step = 0;
            }
        }

        /// <summary>
        /// update all current poses with the current observation
        /// </summary>
        /// <param name="stereo_rays">list of evidence rays to be inserted into the grid</param>
        public void AddObservation(ArrayList[] stereo_rays)
        {
            for (int p = 0; p < Poses.Count; p++)
            {
                particlePath path = (particlePath)Poses[p];
                float localisation_score = 
                    path.current_pose.AddObservation(stereo_rays, 
                                                     rob.LocalGrid, 
                                                     rob.head);
                updatePoseScore(path, localisation_score);
            }

            // adding an observation is sufficient to update the 
            // pose localisation scores.  This is, after all, SLAM.
            PosesEvaluated = true;
        }

        #endregion

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeSensorModel = doc.CreateElement("MotionModel");
            parent.AppendChild(nodeSensorModel);

            util.AddComment(doc, nodeSensorModel, "The number of possible poses to maintain at any point in time");
            util.AddTextElement(doc, nodeSensorModel, "NoOfPoses", Convert.ToString(survey_trial_poses));

            util.AddComment(doc, nodeSensorModel, "A culling threshold in the range 1-100 below which low probability poses are exterminated");
            util.AddTextElement(doc, nodeSensorModel, "CullThreshold", Convert.ToString(cull_threshold));

            util.AddComment(doc, nodeSensorModel, "The number of time steps after which a pose is considered to be mature");
            util.AddTextElement(doc, nodeSensorModel, "MaturationTimeSteps", Convert.ToString(pose_maturation));

            String noise = "";
            for (int i = 0; i < motion_noise.Length; i++)
            {
                noise += Convert.ToString(motion_noise[i]);
                if (i < motion_noise.Length - 1) noise += ",";
            }
            util.AddComment(doc, nodeSensorModel, "Motion noise (standard deviations)");
            util.AddTextElement(doc, nodeSensorModel, "MotionNoise", noise);

            return (nodeSensorModel);
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

            if (xnod.Name == "NoOfPoses")
            {
                survey_trial_poses = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "CullThreshold")
            {
                cull_threshold = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "MaturationTimeSteps")
            {
                pose_maturation = Convert.ToInt32(xnod.InnerText);
            }

            if (xnod.Name == "MotionNoise")
            {
                String[] noise = xnod.InnerText.Split(',');
                motion_noise = new float[noise.Length];
                for (int i = 0; i < noise.Length; i++)
                    motion_noise[i] = Convert.ToSingle(noise[i]);
            }
        
            // call recursively on all children of the current node
            if ((xnod.HasChildNodes) &&
                (xnod.Name == "MotionModel"))
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
