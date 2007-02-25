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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class motionModel
    {
        private Random rnd = new Random();
        private robot rob;

        // a list of possible poses
        public int survey_trial_poses = 200;   // the number of possible poses to keep track of
        public int pose_maturation = 5;        // the number of updates after which a pose is considered to be mature
        public ArrayList Poses;

        // pose score threshold in the range 0-100 below which low probability poses are pruned
        int cull_threshold = 25;

        // standard deviation values for noise within the motion model
        public float[] motion_noise;

        // angular velocity of the wheels in radians/sec
        public float LeftWheelAngularVelocity = 0;
        public float RightWheelAngularVelocity = 0;

        // speed in mm / sec
        public float forward_velocity = 0;

        // angular speed in radians / sec
        public float angular_velocity = 0;

        public motionModel(robot rob)
        {
            this.rob = rob;
            Poses = new ArrayList();
            motion_noise = new float[6];

            // some default noise values
            for (int i = 0; i < motion_noise.Length; i++)
                motion_noise[i] = 0.0001f;

            // create some initial poses
            createNewPoses(rob.x, rob.y, rob.pan);
        }

        /// <summary>
        /// creates some new poses
        /// </summary>
        private void createNewPoses(float x, float y, float pan)
        {
            for (int sample = Poses.Count; sample < survey_trial_poses; sample++)
            {
                possiblePose pose = new possiblePose(x, y, pan);
                Poses.Add(pose);
            }
        }

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
        private void sample_motion_model_velocity(possiblePose pose, float time_elapsed_sec)
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
            if (Math.Abs(ang_velocity) > 0.00001f) fraction = fwd_velocity / ang_velocity;
            float new_pan = pose.pan + (ang_velocity * time_elapsed_sec);

            // update the pose
            pose.x = pose.x - (fraction * (float)Math.Sin(pose.pan)) +
                              (fraction * (float)Math.Sin(new_pan));
            pose.y = pose.y + (fraction * (float)Math.Cos(pose.pan)) -
                              (fraction * (float)Math.Cos(new_pan));
            pose.pan = pose.pan + (ang_velocity * time_elapsed_sec) + (v * time_elapsed_sec);

            pose.time_steps++;
        }

        /// <summary>
        /// removes low probability poses
        /// Note that a maturity threshold is used, so that poses which may initially 
        /// be unfairly judged as improbable have time to prove their worth
        /// </summary>
        private void Prune()
        {
            possiblePose best_pose = null;

            // gather mature poses
            float max_score = 0;
            ArrayList maturePoses = new ArrayList();
            for (int sample = 0; sample < Poses.Count; sample++)
            {
                possiblePose pose = (possiblePose)Poses[sample];

                if (pose.time_steps > pose_maturation)
                {
                    maturePoses.Add(pose);
                }

                // record the maximum score
                if (pose.score > max_score)
                {
                    max_score = pose.score;
                    best_pose = pose;
                }
            }

            // remove mature poses with a score below the cull threshold
            float threshold = max_score * cull_threshold / 100;
            for (int mature = 0; mature < maturePoses.Count; mature++)
            {
                possiblePose pose = (possiblePose)maturePoses[mature];
                if (pose.score < threshold)
                    Poses.Remove(pose);
            }

            if (best_pose != null)
            {
                // create new poses to maintain the population
                createNewPoses(best_pose.x, best_pose.y, best_pose.pan);

                // update the robot position with the best available pose
                rob.x = best_pose.x;
                rob.y = best_pose.y;
                rob.pan = best_pose.pan;
            }
        }

        /// <summary>
        /// predict the next time step
        /// </summary>
        public void Predict(float time_elapsed_sec)
        {
            if (time_elapsed_sec > 0)
            {
                // remove low probability poses
                Prune();

                // deterministic prediction of angular and forward velocities
                float WheelRadius = rob.WheelDiameter_mm / 2;
                angular_velocity = ((WheelRadius / (2 * rob.WheelBase_mm)) * (RightWheelAngularVelocity - LeftWheelAngularVelocity));
                rob.pan += angular_velocity / time_elapsed_sec;
                forward_velocity = ((WheelRadius / 2) * (RightWheelAngularVelocity + LeftWheelAngularVelocity)) / time_elapsed_sec;

                // update poses
                for (int sample = 0; sample < Poses.Count; sample++)
                    sample_motion_model_velocity((possiblePose)Poses[sample], time_elapsed_sec);
            }
        }
    }
}
