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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class robotLocalisation
    {
        /// <summary>
        /// Perform a survey over the given possible poses
        /// Tries to match a viewpoint within a grid and updates the pose scores accordingly
        /// </summary>
        /// <param name="current_view">viewpoint containing range data</param>
        /// <param name="grid">occupancy grid</param>
        /// <param name="motion">motion model</param>
        public static void surveyPoses(viewpoint current_view,
                                       occupancygridMultiResolution grid,
                                       motionModel motion)
        {
            // examine the pose list
            for (int sample = 0; sample < motion.survey_trial_poses; sample++)
            {
                possiblePath path = (possiblePath)motion.Poses[sample];
                possiblePose pose = path.current_pose;

                // get the position of this pose inside the grid
                pos3D relative_position = pose.subtract((pos3D)grid);

                // The trial pose is generated
                viewpoint trialPose = current_view.createTrialPose(pose.pan, pose.x, pose.y);

                // insert the trial pose into the grid and record the number
                // of matched cells
                grid.insert(trialPose, false, relative_position);

                // update the score for this pose
                motion.updatePoseScore(path, grid.matchedCells());
            }

            // indicate that the pose scores have been updated
            motion.PosesEvaluated = true;
        }

        /// <summary>
        /// generate scores for poses.  This is only for testing purposes.
        /// </summary>
        /// <param name="rob"></param>
        public static void surveyPosesDummy(robot rob)
        {
            // examine the pose list
            for (int sample = 0; sample < rob.motion.survey_trial_poses; sample++)
            {
                possiblePath path = (possiblePath)rob.motion.Poses[sample];
                possiblePose pose = path.current_pose;

                float dx = rob.x - pose.x;
                float dy = rob.y - pose.y;
                float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float score = 1.0f / (1 + dist);

                // update the score for this pose
                rob.motion.updatePoseScore(path, score);
            }

            // indicate that the pose scores have been updated
            rob.motion.PosesEvaluated = true;
        }




        /*
        private robotPath teachPath;
        FileStream fp;
        BinaryWriter binfile_write;
        BinaryReader binfile_read;
        bool teaching = false;
        bool following = false;
        bool starting = false;

        // minimum distance between points in path data
        int minDistBetweenPoints_mm = 100;

        // the number of points to insert into each grid
        int pointsPerGrid = 20;  // should be a multiple of 2
        int pointsLoaded = 0;
        int pointsFollowed = 0;
        float distFollowed = 0;

        // the grid currently in use
        occupancygridMultiResolution currentGrid;

        // the grid being pre-loaded
        occupancygridMultiResolution nextGrid;

        robotOdometry odometry;
        robotPath pathFollow;
        pos3D estimatedPos;

        int survey_diameter_mm=5000;
        int no_of_trial_poses=206;
        int ray_thickness=1;
        bool pruneSurvey=false;
        int randomSeed=1000;
        int pruneThreshold=10;
        float survey_angular_offset=0.0f;
        float momentum=0.1f;

        public robotLocalisation(int grid_levels, 
                                 int grid_dimension, 
                                 int max_cellSize_mm,
                                 int countsPerRev,
                                 float wheelDiameter_mm,
                                 float wheelSeparation_mm)
        {
            currentGrid = new occupancygridMultiResolution(grid_levels, grid_dimension, max_cellSize_mm);
            nextGrid = new occupancygridMultiResolution(grid_levels, grid_dimension, max_cellSize_mm);

            teachPath = new robotPath();
            pathFollow = new robotPath();
            estimatedPos = new pos3D(0,0,0);

            odometry = new robotOdometry();
            odometry.countsPerRev = countsPerRev;
            odometry.wheelDiameter_mm = wheelDiameter_mm;
            odometry.wheelSeparation_mm = wheelSeparation_mm;
        }

        #region "teaching"

        public void StartTeaching(String teachPath, String fromLocation, String toLocation)
        {
            String filename = teachPath + fromLocation + "_" + toLocation + ".path";

            fp = new FileStream(filename, FileMode.Create);
            binfile_write = new BinaryWriter(fp);
            odometry.Clear();
            teaching = true;
        }

        public void StopTeaching()
        {
            if (teaching)
            {
                binfile_write.Close();
                fp.Close();
                teaching = false;
            }
        }

        public void Teach(robot rob)
        {
            if (teaching)
            {
                // ensure that a minimum distance has been traversed
                float dist = odometry.distFromLastPoint(rob.leftWheelCounts, rob.rightWheelCounts);
                if (dist > minDistBetweenPoints_mm)
                {
                    // add an odometry entry
                    odometry.Add(rob.leftWheelCounts, rob.rightWheelCounts);

                    // set the robot position to the odometry position
                    odometry.updateRobotPosition(rob);

                    // save position and stereo features
                    rob.savePathPoint(binfile_write);
                }
            }
        }

        #endregion


        #region "following"

        public void StartFollowing(String teachPath, String fromLocation, String toLocation)
        {
            String filename = teachPath + fromLocation + "_" + toLocation + ".path";

            fp = new FileStream(filename, FileMode.Open);
            binfile_read = new BinaryReader(fp);
            odometry.Clear();
            pathFollow.Clear();
            following = true;
            starting = true;
            distFollowed = 0;
        }

        public void StopFollowing()
        {
            if (following)
            {
                binfile_read.Close();
                fp.Close();
                following = false;
            }
        }

        public void Follow(robot rob)
        {
            int p;

            if (following)
            {
                // get the current view
                viewpoint view_localisation = rob.getViewpoint();

                if (starting)
                {
                    // create an initial grid
                    starting = false;
                    pathFollow.Clear();
                    for (p = 0; p < pointsPerGrid; p++)
                        rob.loadPathPoint(binfile_read, pathFollow);
                    currentGrid.insert(pathFollow);

                    // clear the path ready for the next data
                    pathFollow.Clear();
                }

                float dist = odometry.distFromLastPoint(rob.leftWheelCounts, rob.rightWheelCounts);
                if (dist > minDistBetweenPoints_mm)
                {
                    // update the total distance travelled
                    distFollowed += dist;
                    pointsFollowed = (int)(distFollowed / minDistBetweenPoints_mm);
                    if (pointsFollowed >= pointsPerGrid)
                    {
                        // swap grids
                        distFollowed -= (pointsPerGrid * minDistBetweenPoints_mm);
                        occupancygridMultiResolution tempGrid = nextGrid;
                        nextGrid = currentGrid;
                        currentGrid = tempGrid;
                        pointsLoaded = 0;
                    }

                    // update the current odometry position of the robot
                    odometry.Add(rob.leftWheelCounts, rob.rightWheelCounts);

                    if (pointsLoaded < pointsPerGrid)
                    {
                        for (p = 0; p < 2; p++)
                            rob.loadPathPoint(binfile_read, pathFollow);

                        pointsLoaded += p;
                        if (pointsLoaded >= pointsPerGrid)
                        {
                            // create the next grid
                            nextGrid.Clear();
                            nextGrid.insert(pathFollow);

                            // clear the path ready for the next data
                            pathFollow.Clear();
                        }
                    }


                    // localise within the grid
                    // position estimate from odometry, which positions the robot within the grid
                    // as an initial estimate from which to search
                    float max_score = 0;
                    //TODO: get local odometry position within the grid
                    pos3D local_odometry_position = new pos3D(0, 0, 0); // view_map.odometry_position.subtract(pathMap.pathCentre());
                    ArrayList survey_results = rob.sensorModelLocalisation.surveyXYP(view_localisation, currentGrid, survey_diameter_mm, survey_diameter_mm,
                                                                         no_of_trial_poses, ray_thickness, pruneSurvey, randomSeed,
                                                                         pruneThreshold, survey_angular_offset,
                                                                         local_odometry_position, momentum,
                                                                         ref max_score);
                    float peak_x = 0;
                    float peak_y = 0;
                    float peak_pan = 0;
                    rob.sensorModelLocalisation.SurveyPeak(survey_results, ref peak_x, ref peak_y);
                    float[] peak = rob.sensorModelLocalisation.surveyPan(view_localisation, currentGrid, ray_thickness, peak_x, peak_y);
                    peak_pan = rob.sensorModelLocalisation.SurveyPeakPan(peak);


                }
            }
        }

        #endregion
        */
    }
}
