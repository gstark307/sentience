/*
    Motion model used to predict the next step
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
using sluggish.utilities.xml;
using CenterSpace.Free;

namespace sentience.core
{
    /// <summary>
    /// robot motion model
    /// </summary>
    public class motionModel
    {
        // default noise parameters for the motion model
        const float default_motion_noise_1 = 0.08f;
        const float default_motion_noise_2 = 0.00025f;
        const float default_motion_noise_3 = 0.00005f;

        // different modes used for creating pose lists
        public const int MODE_EGOCENTRIC = 0;  // sets all poses to the current known robot location
        public const int MODE_MONTE_CARLO = 1; // randomly distribute poses for monte carlo localisation

        // the maximum length of paths containing possible poses
        const int max_path_length = 500;

        // time step index which may be assigned to poses
        private UInt32 time_step = 0;

        // the bounding box within which the path tree occurs
        float min_tree_x = float.MaxValue;
        float min_tree_y = float.MaxValue;
        float max_tree_x = float.MinValue;
        float max_tree_y = float.MinValue;

        // counter used to uniquely identify paths
        private UInt32 path_ID = 0;

        // the most probable path taken by the robot
        public particlePath best_path = null;

        // random number generator
        //private MersenneTwister rnd = new MersenneTwister(100);
        private Random rnd = new Random(0);

        // the time step at which all branches converge to a single path
        private UInt32 root_time_step = UInt32.MaxValue;

        private robot rob;
        public occupancygridMultiHypothesis LocalGrid;
        
        // the estimated current robot pose
        public pos3D current_robot_pose;

        // the best current path score
        public float current_robot_path_score;

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
        public List<particlePath> Poses;

        // stores all active paths so that they're not garbage collected
        private List<particlePath> ActivePoses;

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

        public motionModel(robot rob, 
                           occupancygridMultiHypothesis LocalGrid,
                           int random_seed)
        {
            // seed the random number generator
            rnd = new Random(random_seed); // MersenneTwister(random_seed);

            this.rob = rob;
            this.LocalGrid = LocalGrid;
            Poses = new List<particlePath>();
            ActivePoses = new List<particlePath>();
            motion_noise = new float[6];

            // some default noise values
            int i = 0;
            while (i < 2)
            {
                motion_noise[i] = default_motion_noise_1;
                i++;
            }
            while (i < 4)
            {
                motion_noise[i] = default_motion_noise_2;
                i++;
            }
            while (i < motion_noise.Length)
            {
                motion_noise[i] = default_motion_noise_3;
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
            if (path_ID > UInt32.MaxValue - 3) path_ID = 0; // rollover
        }

        /// <summary>
        /// creates some new poses
        /// </summary>
        private void createNewPoses(float x, float y, float pan)
        {
            for (int sample = Poses.Count; sample < survey_trial_poses; sample++)
            {
                particlePath path = new particlePath(x, y, pan, max_path_length, time_step, path_ID, rob.LocalGridDimension);
                createNewPose(path);
            }
        }


        /// <summary>
        /// create poses distributed over an area, suitable for
        /// monte carlo localisation
        /// </summary>
        /// <param name="tx">top left of the area in millimetres</param>
        /// <param name="ty">top left of the area in millimetres</param>
        /// <param name="bx">bottom right of the area in millimetres</param>
        /// <param name="by">bottom right of the area in millimetres</param>
        private void createNewPosesMonteCarlo(float tx, float ty, float bx, float by)
        {
            for (int sample = Poses.Count; sample < survey_trial_poses; sample++)
            {
                float x = tx + rnd.Next((int)(bx - tx));
                float y = ty + rnd.Next((int)(by - ty));
                float pan = 2 * (float)Math.PI * rnd.Next(100000) / 100000.0f;
                particlePath path = new particlePath(x, y, pan, max_path_length, time_step, path_ID, rob.LocalGridDimension);
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
            float v = 0;

            for (int i = 0; i < 12; i++)
                v += (((float)rnd.Next() * 2) - 1.0f) * b;
                //v += ((rnd.Next(200000) / 100000.0f) - 1.0f) * b;

            return(v / 2.0f);
        }

        /// <summary>
        /// update the given pose using the motion model
        /// </summary>
        /// <param name="path">path to add the new estimated pose to</param>
        /// <param name="time_elapsed_sec">time elapsed since the last update in seconds</param>
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
            float current_pan = path.current_pose.pan;

            // if scan matching is active use the current estimated pan angle
            if (rob.ScanMatchingPanAngleEstimate != scanMatching.NOT_MATCHED)
                current_pan = rob.ScanMatchingPanAngleEstimate;

            float pan2 = current_pan - (ang_velocity * time_elapsed_sec);

            float new_y = path.current_pose.y + (fraction * (float)Math.Sin(current_pan)) -
                  (fraction * (float)Math.Sin(pan2));
            float new_x = path.current_pose.x - (fraction * (float)Math.Cos(current_pan)) +
                              (fraction * (float)Math.Cos(pan2));
            float new_pan = pan2 + (v * time_elapsed_sec);

            particlePose new_pose = new particlePose(new_x, new_y, new_pan, path);
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
            float max_score = float.MinValue;
            best_path = null;

            // sort poses by score
            for (int i = 0; i < Poses.Count-1; i++)
            {
                particlePath p1 = Poses[i];

                // keep track of the bounding region within which the path tree occurs
                if (p1.current_pose.x < min_tree_x) min_tree_x = p1.current_pose.x;
                if (p1.current_pose.x > max_tree_x) max_tree_x = p1.current_pose.x;
                if (p1.current_pose.y < min_tree_y) min_tree_y = p1.current_pose.y;
                if (p1.current_pose.y > max_tree_y) max_tree_y = p1.current_pose.y;

                max_score = p1.total_score;
                particlePath winner = null;
                int winner_index = 0;
                for (int j = i + 1; j < Poses.Count; j++)
                {
                    particlePath p2 = Poses[i];
                    float sc = p2.total_score;
                    if ((sc > max_score) ||
                        ((max_score == 0) && (sc != 0)))
                    {
                        max_score = sc;
                        winner = p2;
                        winner_index = j;
                    }
                }
                if (winner != null)
                {
                    Poses[i] = winner;
                    Poses[winner_index] = p1;
                }
            }            

            // the best path is at the top
            best_path = Poses[0];

            // It's culling season
            int cull_index = (100 - cull_threshold) * Poses.Count / 100;
            if (cull_index > Poses.Count - 2) cull_index = Poses.Count - 2;
            for (int i = Poses.Count - 1; i > cull_index; i--)
            {
                particlePath path = Poses[i];
                if (path.path.Count >= pose_maturation)
                {                    
                    // remove mapping hypotheses for this path
                    path.Remove(LocalGrid);

                    // now remove the path itself
                    Poses.RemoveAt(i);                    
                }
            }

            // garbage collect any dead paths (R.I.P.)
            List<particlePath> possible_roots = new List<particlePath>(); // stores paths where all branches have coinverged to a single possibility
            for (int i = ActivePoses.Count - 1; i >= 0; i--)
            {
                particlePath path = ActivePoses[i];

                if ((!path.Enabled) ||
                    ((path.total_children == 0) && (path.branch_pose == null) && (path.current_pose.time_step < time_step - pose_maturation - 5)))
                {
                    ActivePoses.RemoveAt(i);
                }
                else
                {
                    // record any fully collapsed paths
                    if (!path.Collapsed)
                        if (path.branch_pose == null)
                            possible_roots.Add(path);
                        else
                        {
                            if (path.branch_pose.path.Collapsed)
                                possible_roots.Add(path);
                        }
                }
            }

            if (possible_roots.Count == 1)
            {
                // collapse tha psth
                particlePath path = possible_roots[0];

                if (path.branch_pose != null)
                {
                    particlePath previous_path = path.branch_pose.path;
                    previous_path.Distill(LocalGrid);
                    path.branch_pose.parent = null;
                    path.branch_pose = null;                    
                }
                
                path.Collapsed = true;

                // take note of the time step.  This is for use with the display functions only
                root_time_step = path.current_pose.time_step;
            }

            if (best_path != null)
            {
                // update the current robot position with the best available pose
                if (current_robot_pose == null) current_robot_pose = 
                    new pos3D(best_path.current_pose.x,
                              best_path.current_pose.y,
                              0);
                current_robot_pose.pan = best_path.current_pose.pan;
                current_robot_path_score = best_path.total_score;

                // generate new poses from the ones which have survived culling
                int new_poses_required = survey_trial_poses; // -Poses.Count;
                int max = Poses.Count;
                int n = 0, added = 0;
                while ((max > 0) &&
                       (n < new_poses_required*4) && 
                       (added < new_poses_required))
                {
                    // identify a potential parent at random, 
                    // from one of the surviving paths
                    int random_parent_index = rnd.Next(max-1);
                    particlePath parent_path = Poses[random_parent_index];
                    
                    // only mature parents can have children
                    if (parent_path.path.Count >= pose_maturation)
                    {
                        // generate a child branching from the parent path
                        particlePath child_path = new particlePath(parent_path, path_ID, rob.LocalGridDimension);
                        createNewPose(child_path);
                        added++;
                    }
                    n++;
                }
                
                // like salmon, after parents spawn they die
                if (added > 0)
                    for (int i = max - 1; i >= 0; i--)
                        Poses.RemoveAt(i);

                // if the robot has rotated through a large angle since
                // the previous time step then reset the scan matching estimate
                //if (Math.Abs(best_path.current_pose.pan - rob.pan) > rob.ScanMatchingMaxPanAngleChange * Math.PI / 180)
                    rob.ScanMatchingPanAngleEstimate = scanMatching.NOT_MATCHED;
            }


            PosesEvaluated = false;
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show an above view of the robot using the best available pose
        /// </summary>
        /// <param name="img">image data within which to draw</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="clear_background">whether to clear the image before drawing</param>
        public void ShowBestPose(byte[] img, int width, int height,
                                 bool clear_background, bool showFieldOfView)
        {
            if (best_path != null)
            {
                particlePose best_pose = best_path.current_pose;
                if (best_pose != null)
                {
                    int min_x = (int)(best_pose.x - rob.BodyWidth_mm);
                    int min_y = (int)(best_pose.y - rob.BodyLength_mm);
                    int max_x = (int)(best_pose.x + rob.BodyWidth_mm);
                    int max_y = (int)(best_pose.y + rob.BodyLength_mm);

                    ShowBestPose(img, width, height, min_x, min_y, max_x, max_y, clear_background, showFieldOfView);
                }
            }
        }

        /// <summary>
        /// show an above view of the robot using the best available pose
        /// </summary>
        /// <param name="img">image data within which to draw</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="min_x">bounding box top left x coordinate</param>
        /// <param name="min_y">bounding box top left y coordinate</param>
        /// <param name="max_x">bounding box bottom right x coordinate</param>
        /// <param name="max_y">bounding box bottom right y coordinate</param>
        /// <param name="clear_background">whether to clear the image before drawing</param>
        public void ShowBestPose(Byte[] img, int width, int height,
                                 int min_x, int min_y, int max_x, int max_y,
                                 bool clear_background, bool showFieldOfView)
        {
            if (best_path != null)
            {
                particlePose best_pose = best_path.current_pose;
                if (best_pose != null)
                {
                    best_pose.Show(rob, img, width, height,
                                   clear_background, min_x, min_y, max_x, max_y, 0, showFieldOfView);
                }
            }
        }

        /// <summary>
        /// show the position uncertainty distribution
        /// </summary>
        /// <param name="img">image data within which to draw</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        public void Show(Byte[] img, int width, int height,
                         bool clearBackground)
        {
            float min_x = 99999;
            float min_y = 99999;
            float max_x = -99999;
            float max_y = -99999;
            for (int sample = 0; sample < Poses.Count; sample++)
            {
                particlePose pose = Poses[sample].current_pose;
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
        /// <param name="img">image data within which to draw</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="min_x">bounding box top left x coordinate</param>
        /// <param name="min_y">bounding box top left y coordinate</param>
        /// <param name="max_x">bounding box bottom right x coordinate</param>
        /// <param name="max_y">bounding box bottom right y coordinate</param>
        /// <param name="clear_background">whether to clear the image before drawing</param>
        public void Show(byte[] img, int width, int height,
                         float min_x_mm, float min_y_mm,
                         float max_x_mm, float max_y_mm,
                         bool clear_background)
        {
            if (clear_background)
            {
                // clear the image
                for (int i = 0; i < width * height * 3; i++)
                    img[i] = (Byte)250;
            }

            if ((max_x_mm > min_x_mm) && (max_y_mm > min_y_mm))
            {
                for (int sample = 0; sample < Poses.Count; sample++)
                {
                    particlePose pose = Poses[sample].current_pose;
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
        /// <param name="img">image data within which to draw</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="line_thickness">thickness of the path</param>
        /// <param name="min_x">bounding box top left x coordinate</param>
        /// <param name="min_y">bounding box top left y coordinate</param>
        /// <param name="max_x">bounding box bottom right x coordinate</param>
        /// <param name="max_y">bounding box bottom right y coordinate</param>
        /// <param name="clear_background">whether to clear the image before drawing</param>
        public void ShowPath(byte[] img, int width, int height,
                             int r, int g, int b, int line_thickness,
                             float min_x_mm, float min_y_mm,
                             float max_x_mm, float max_y_mm,
                             bool clear_background)
        {
            if (best_path != null)
                best_path.Show(img, width, height, r, g, b, line_thickness,
                               min_x_mm, min_y_mm, max_x_mm, max_y_mm, 
                               clear_background, 0);
        }

        /// <summary>
        /// show the tree of possible paths
        /// </summary>
        /// <param name="img"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="line_thickness"></param>
        public void ShowTree(Byte[] img, int width, int height,
                             int r, int g, int b, int line_thickness)
        {
            for (int i = 0; i < ActivePoses.Count; i++)
            {
                bool clearBackground = false;
                if (i == 0) clearBackground = true;
                particlePath path = ActivePoses[i];
                path.Show(img, width, height, r, g, b, line_thickness,
                          min_tree_x, min_tree_y, max_tree_x, max_tree_y,
                          clearBackground, root_time_step);
            }
                
        }


        #endregion

        #region "update poses"

        /// <summary>
        /// updates the given path with the score obtained from the current pose
        /// Note that scores are always expressed as log odds matching probability
        /// from grid localisation
        /// </summary>
        /// <param name="path">path whose score is to be updated</param>
        /// <param name="new_score">the new score to be added to the path</param>
        public void updatePoseScore(particlePath path, float new_score)
        {
            path.total_score += new_score;
            path.local_score += new_score;
        }

        /// <summary>
        /// reset the list of possible robot poses
        /// </summary>
        /// <param name="mode"></param>
        public void Reset(int mode)
        {
            switch (mode)
            {
                // normally used during SLAM
                case MODE_EGOCENTRIC:
                    {
                        ResetEgocentric();
                        break;
                    }
                    
                // normally used during monte carlo localisation
                case MODE_MONTE_CARLO:
                    {
                        ResetMonteCarlo();
                        break;
                    }
            }
        }

        /// <summary>
        /// resets the pose list to the current known robot position
        /// </summary>
        private void ResetEgocentric()
        {
            // create some initial poses
            Poses.Clear();
            createNewPoses(rob.x, rob.y, rob.pan);
            initialised = true;
        }

        /// <summary>
        /// resets the pose list distributed randomly across the entire grid
        /// for use with monte carlo localisation
        /// </summary>
        private void ResetMonteCarlo()
        {
            // create some initial monte carlo poses
            Poses.Clear();
            int half_width_mm = (int)(rob.LocalGridDimension * rob.LocalGridCellSize_mm) / 2;
            half_width_mm = half_width_mm * 90 / 100; // not quite the whole iguana
            createNewPosesMonteCarlo(LocalGrid.x - half_width_mm, LocalGrid.y - half_width_mm,
                                     LocalGrid.x + half_width_mm, LocalGrid.y + half_width_mm);
            initialised = true;
        }


        /// <summary>
        /// forward prediction - predicts the next state/pose of the robot
        /// </summary>
        /// <param name="time_elapsed_sec">time elapsed since the last update in seconds</param>
        public void Predict(float time_elapsed_sec)
        {
            if (time_elapsed_sec > 0)
            {
                if (!initialised) Reset(MODE_EGOCENTRIC);

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
                for (int sample = Poses.Count-1; sample >= 0; sample--)
                    sample_motion_model_velocity(Poses[sample], time_elapsed_sec);

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
        /// <param name="localiseOnly">if true does not add any mapping particles (pure localisation)</param>
        public void AddObservation(List<evidenceRay>[] stereo_rays, bool localiseOnly)
        {
            for (int p = 0; p < Poses.Count; p++)
            {
                particlePath path = Poses[p];
                float logodds_localisation_score = 
                    path.current_pose.AddObservation(stereo_rays,
                                                     rob, LocalGrid, 
                                                     localiseOnly);

                if (logodds_localisation_score != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                    updatePoseScore(path, logodds_localisation_score);
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

            xml.AddComment(doc, nodeSensorModel, "The number of possible poses to maintain at any point in time");
            xml.AddTextElement(doc, nodeSensorModel, "NoOfPoses", Convert.ToString(survey_trial_poses));

            xml.AddComment(doc, nodeSensorModel, "A culling threshold in the range 1-100 below which low probability poses are exterminated");
            xml.AddTextElement(doc, nodeSensorModel, "CullThreshold", Convert.ToString(cull_threshold));

            xml.AddComment(doc, nodeSensorModel, "The number of time steps after which a pose is considered to be mature");
            xml.AddTextElement(doc, nodeSensorModel, "MaturationTimeSteps", Convert.ToString(pose_maturation));

            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            String noise = "";
            for (int i = 0; i < motion_noise.Length; i++)
            {
                noise += Convert.ToString(motion_noise[i], format);
                if (i < motion_noise.Length - 1) noise += ",";
            }
            xml.AddComment(doc, nodeSensorModel, "Motion noise (standard deviations)");
            xml.AddTextElement(doc, nodeSensorModel, "MotionNoise", noise);

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
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String[] noise = xnod.InnerText.Split(',');
                motion_noise = new float[noise.Length];
                for (int i = 0; i < noise.Length; i++)
                    motion_noise[i] = Convert.ToSingle(noise[i], format);
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
