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
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using sentience.core;
using sentience.pathplanner;
using sluggish.utilities;

namespace WindowsApplication1
{
    public partial class frmMain : Form
    {
        Random rnd = new Random();

        Bitmap rays;
        Byte[] img_rays;
        int standard_width = 500;
        int standard_height = 500;

        float[,,] grid_layer;
        int grid_dimension = 2000;

        private stereoModel stereo_model;
        stereoHead robot_head;
        float[] stereo_features;
        float[] stereo_uncertainties;
        private float[] pos3D_x;
        private float[] pos3D_y;

        #region "stopwatch"

        DateTime stopWatchTime;
        private void beginStopWatch()
        {
            stopWatchTime = DateTime.Now;
        }

        private long endStopWatch()
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan timeDiff;
            long milliseconds;

            timeDiff = currentTime.Subtract(stopWatchTime);
            milliseconds = (timeDiff.Minutes * 60000) + (timeDiff.Seconds * 1000) + timeDiff.Milliseconds;
            return (milliseconds);
        }

        #endregion


        public frmMain()
        {
            InitializeComponent();
        }

        private void createSingleRayModel()
        {
            bool mirror = false;
            int divisor = 6;
            grid_dimension = 10000;
            grid_layer = new float[grid_dimension / divisor, grid_dimension, 3];
            stereo_model.showSingleRay(grid_layer, grid_dimension, img_rays, standard_width, standard_height, divisor, false, mirror);
        }

        private void createSensorModelLookup()
        {
            stereo_model.createLookupTable(32, img_rays, standard_width, standard_height);
        }
        /*
        private void test_head()
        {
            pos3D robotPosition = new pos3D(0, 0, 0);
            pos3D robotOrientation = new pos3D(0, 0, 0);

            beginStopWatch();

            for (int i = 0; i < view.Length; i++)
            {
                for (int cam = 0; cam < 4; cam++)
                {
                    //invent some stereo features
                    for (int f = 0; f < stereo_features.Length; f += 3)
                    {
                        stereo_features[f] = rnd.Next(robot_head.image_width - 1);
                        stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);

                        //stereo_features[f] = (robot_head.image_width*1/10) + (rnd.Next(robot_head.image_width/300) - 1);
                        //stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);

                        stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                    }
                    robot_head.setStereoFeatures(cam, stereo_features, stereo_uncertainties, stereo_features.Length);
                }

                view[i] = stereo_model.createViewpoint(robot_head, robotOrientation);
                view[0].showAbove(img_rays, standard_width, standard_height, 2000, 255, 255, 255, true, 0, false);
                robotPosition.y += 0;
                robotPosition.pan += 0.0f;
            }            

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);
        }
        */

        /*
        private void test_head2()
        {
            robot_head = new stereoHead(2);            
            robot_head.initDualCam();

            pos3D robotPosition = new pos3D(0, 0, 0);
            pos3D robotOrientation = new pos3D(0, 0, 0);

            beginStopWatch();

            for (int i = 0; i < view.Length; i++)
            {
                for (int cam = 0; cam < 2; cam++)
                {
                    //invent some stereo features
                    for (int f = 0; f < stereo_features.Length; f += 3)
                    {
                        stereo_features[f] = rnd.Next(robot_head.image_width - 1);
                        stereo_features[f + 1] = robot_head.image_height / 2; // rnd.Next(robot_head.image_height - 1);

                        //stereo_features[f] = (robot_head.image_width*1/10) + (rnd.Next(robot_head.image_width/300) - 1);
                        //stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);

                        stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                    }
                    robot_head.setStereoFeatures(cam, stereo_features, stereo_uncertainties, stereo_features.Length);
                }

                view[i] = stereo_model.createViewpoint(robot_head, robotOrientation);
                view[0].showFront(img_rays, standard_width, standard_height, 2000, 255, 255, 255, true);
                robotPosition.y += 0;
                robotPosition.pan += 0.0f;
            }

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);
        }
        */

        /*
        private void test_grid()
        {
            occupancygridClassic grd = new occupancygridClassic(128, 32);
            pos3D robotPosition = new pos3D(0, 0, 0);
            pos3D robotOrientation = new pos3D(0, 0, 0);

            beginStopWatch();

            //int i = 0;
            //for (i = 0; i < 9; i++)
            {
                for (int cam = 0; cam < 4; cam++)
                {
                    //invent some stereo features
                    for (int f = 0; f < stereo_features.Length; f += 3)
                    {
                        stereo_features[f] = rnd.Next(robot_head.image_width - 1);
                        stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);
                        stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                    }
                    robot_head.setStereoFeatures(cam, stereo_features, stereo_uncertainties, stereo_features.Length);
                }

                view[0] = stereo_model.createViewpoint(robot_head, robotOrientation);
                pos3D centre = new pos3D(0, 0, 0);
                grd.insert(view[0],true,centre);
                //view[0].show(img_rays, standard_width, standard_height, 2000, 255, 255, 255, true);
                robotPosition.y += 50;
                robotPosition.pan += 0.0f;
            }

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);

            grd.show(img_rays, standard_width, standard_height);
        }
        */

        /// <summary>
        /// generate scores for poses.  This is only for testing purposes.
        /// </summary>
        /// <param name="rob"></param>        
        private void surveyPosesDummy(robot rob)
        {
            motionModel motion_model = rob.GetBestMotionModel();

            // examine the pose list
            for (int sample = 0; sample < motion_model.survey_trial_poses; sample++)
            {
                particlePath path = (particlePath)motion_model.Poses[sample];
                particlePose pose = path.current_pose;

                float dx = rob.x - pose.x;
                float dy = rob.y - pose.y;
                float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                float score = 1.0f / (1 + dist);

                // update the score for this pose
                motion_model.updatePoseScore(path, score);
            }

            // indicate that the pose scores have been updated
            motion_model.PosesEvaluated = true;
        }


        private void test_path_planner(int grid_dimension, int cellSize_mm)
        {
            int grid_centre_x_mm = 0;
            int grid_centre_y_mm = 0;
            bool[,] navigable_space = new bool[grid_dimension, grid_dimension];

            pathplanner planner = new pathplanner(navigable_space, cellSize_mm, grid_centre_x_mm, grid_centre_y_mm);
            
            // create a test map
            int w = grid_dimension / 20;
            planner.AddNavigableSpace((grid_dimension / 2)-w, grid_dimension / 50, w + (grid_dimension / 2), grid_dimension - (grid_dimension / 50));
            planner.AddNavigableSpace((grid_dimension / 2) - w, (grid_dimension * 70 / 100), grid_dimension - (grid_dimension / 50), (grid_dimension * 70 / 100) + (w * 2));
            planner.AddNavigableSpace((grid_dimension / 5), (grid_dimension * 30 / 100), grid_dimension / 2, (grid_dimension * 30 / 100) + (w * 2));
            planner.AddNavigableSpace((grid_dimension / 20), (grid_dimension / 10), grid_dimension * 44 / 100, (grid_dimension * 50 / 100));
            planner.AddNavigableSpace((grid_dimension / 2), (grid_dimension * 20 / 100), grid_dimension - (grid_dimension / 50), (grid_dimension * 20 / 100) + (w * 2));
            planner.AddNavigableSpace((grid_dimension * 80 / 100), (grid_dimension * 50 / 100), (grid_dimension * 80 / 100) + (w * 2), (grid_dimension * 70 / 100));
            planner.AddNavigableSpace((grid_dimension * 57 / 100), (grid_dimension * 31 / 100), (grid_dimension * 95 / 100), (grid_dimension * 69 / 100));
            planner.AddNavigableSpace((grid_dimension / 20), (grid_dimension * 52 / 100), grid_dimension * 44 / 100, (grid_dimension * 95 / 100));             
            planner.AddNavigableSpace((grid_dimension * 20 / 100), (grid_dimension * 80 / 100), (grid_dimension * 50 / 100), (grid_dimension * 80 / 100) + (w * 2));
            planner.Update(0, 0, grid_dimension - 1, grid_dimension - 1);

            ArrayList plan = new ArrayList();
            int start_x=0, start_y=0, finish_x=0, finish_y=0;
            int i = 0;
            bool found = false;
            while ((i < 10000) && (!found))
            {
                start_x = 1 + rnd.Next(grid_dimension - 3);
                start_y = 1 + rnd.Next(grid_dimension - 3);
                if ((navigable_space[start_x, start_y]) && 
                    (navigable_space[start_x-1, start_y-1]) &&
                    (navigable_space[start_x - 1, start_y]))
                    found = true;
                i++;
            }
            if (found)
            {
                found = false;
                i = 0;
                while ((i < 10000) && (!found))
                {
                    finish_x = 1 + rnd.Next(grid_dimension - 3);
                    finish_y = 1 + rnd.Next(grid_dimension - 3);
                    if ((navigable_space[finish_x, finish_y]) &&
                        (navigable_space[finish_x-1, finish_y-1]) &&
                        (navigable_space[finish_x - 1, finish_y]))
                        found = true;
                    i++;
                }
                if (found)
                {

                    int start_xx = ((start_x - (grid_dimension / 2)) * cellSize_mm) + grid_centre_x_mm;
                    int start_yy = ((start_y - (grid_dimension / 2)) * cellSize_mm) + grid_centre_y_mm;
                    int finish_xx = ((finish_x - (grid_dimension / 2)) * cellSize_mm) + grid_centre_x_mm;
                    int finish_yy = ((finish_y - (grid_dimension / 2)) * cellSize_mm) + grid_centre_y_mm;
                    plan = planner.CreatePlan(start_xx, start_yy, finish_xx, finish_yy);
                }
            }

            planner.Show(img_rays, standard_width, standard_height, plan);

            drawing.drawCircle(img_rays, standard_width, standard_height,
                            start_x * standard_width / grid_dimension,
                            start_y * standard_height / grid_dimension,
                            standard_width / 100, 255, 0, 0, 1);
            drawing.drawCircle(img_rays, standard_width, standard_height,
                            finish_x * standard_width / grid_dimension,
                            finish_y * standard_height / grid_dimension,
                            standard_width / 100, 255, 0, 255, 1);
        }

        private void test_motion_model(bool closed_loop)
        {
            robot rob = new robot(1);

            int min_x_mm = 0;
            int min_y_mm = 0;
            int max_x_mm = 1000;
            int max_y_mm = 1000;
            int step_size = (max_y_mm - min_y_mm) / 15;
            int x = (max_x_mm - min_x_mm) / 2;
            bool initial = true;
            float pan = 0; // (float)Math.PI / 4;
            for (int y = min_y_mm; y < max_y_mm; y += step_size)
            {
                if (closed_loop) surveyPosesDummy(rob);
                rob.updateFromKnownPosition(null, x, y, pan);
                
                rob.GetBestMotionModel().Show(img_rays, standard_width, standard_height, 
                                              min_x_mm, min_y_mm, max_x_mm, max_y_mm,
                                              initial);
                initial = false;
            }
        }

        private void pathPlanningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grid_layer = new float[grid_dimension, grid_dimension, 3];

            pos3D_x = new float[4];
            pos3D_y = new float[4];

            stereo_model = new stereoModel();
            robot_head = new stereoHead(4);
            stereo_features = new float[900];
            stereo_uncertainties = new float[900];

            img_rays = new Byte[standard_width * standard_height * 3];
            rays = new Bitmap(standard_width, standard_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRays.Image = rays;

            test_path_planner(200, 50);

            BitmapArrayConversions.updatebitmap_unsafe(img_rays, (Bitmap)picRays.Image);
        }

        private void singleRayModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grid_layer = new float[grid_dimension, grid_dimension, 3];

            pos3D_x = new float[4];
            pos3D_y = new float[4];

            stereo_model = new stereoModel();
            robot_head = new stereoHead(4);
            stereo_features = new float[900];
            stereo_uncertainties = new float[900];

            img_rays = new Byte[standard_width * standard_height * 3];
            rays = new Bitmap(standard_width, standard_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRays.Image = rays;

            createSingleRayModel();

            BitmapArrayConversions.updatebitmap_unsafe(img_rays, (Bitmap)picRays.Image);
        }

        private void multipleStereoRaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grid_layer = new float[grid_dimension, grid_dimension, 3];

            pos3D_x = new float[4];
            pos3D_y = new float[4];

            stereo_model = new stereoModel();
            robot_head = new stereoHead(4);
            stereo_features = new float[900];
            stereo_uncertainties = new float[900];

            img_rays = new Byte[standard_width * standard_height * 3];
            rays = new Bitmap(standard_width, standard_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRays.Image = rays;

            bool mirror = false;
            stereo_model.showProbabilities(grid_layer, grid_dimension, img_rays, standard_width, standard_height, false, true, mirror);
            BitmapArrayConversions.updatebitmap_unsafe(img_rays, (Bitmap)picRays.Image);

        }

        private void motionModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grid_layer = new float[grid_dimension, grid_dimension, 3];

            pos3D_x = new float[4];
            pos3D_y = new float[4];

            stereo_model = new stereoModel();
            robot_head = new stereoHead(4);
            stereo_features = new float[900];
            stereo_uncertainties = new float[900];

            img_rays = new Byte[standard_width * standard_height * 3];
            rays = new Bitmap(standard_width, standard_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRays.Image = rays;

            test_motion_model(false);

            BitmapArrayConversions.updatebitmap_unsafe(img_rays, (Bitmap)picRays.Image);
        }

        private void gaussianFunctionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grid_layer = new float[grid_dimension, grid_dimension, 3];

            pos3D_x = new float[4];
            pos3D_y = new float[4];

            stereo_model = new stereoModel();
            robot_head = new stereoHead(4);
            stereo_features = new float[900];
            stereo_uncertainties = new float[900];

            img_rays = new Byte[standard_width * standard_height * 3];
            rays = new Bitmap(standard_width, standard_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRays.Image = rays;
            
            stereo_model.showDistribution(img_rays, standard_width, standard_height);

            BitmapArrayConversions.updatebitmap_unsafe(img_rays, (Bitmap)picRays.Image);
        }

        /*
        private void test_trial_poses()
        {
            pos3D robotPosition = new pos3D(0, 0, 0);
            pos3D robotOrientation = new pos3D(0, 0, 0);

            beginStopWatch();

            for (int cam = 0; cam < 4; cam++)
            {
                //invent some stereo features
                for (int f = 0; f < stereo_features.Length; f += 3)
                {
                    stereo_features[f] = rnd.Next(robot_head.image_width - 1);
                    stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);
                    stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                }
                robot_head.setStereoFeatures(cam, stereo_features, stereo_uncertainties, stereo_features.Length);
            }
            view[0] = stereo_model.createViewpoint(robot_head, robotOrientation);
            view[0].showAbove(img_rays, standard_width, standard_height, 2000, 255, 255, 255, true, 0, false);

            for (int i = 1; i < view.Length; i++)
            {
                view[i] = view[0].createTrialPose(robotPosition.pan,robotPosition.x, robotPosition.y);
                view[i].showAbove(img_rays, standard_width, standard_height, 2000, 0, 0, 255, true, 0, false);
                robotPosition.y += 0;
                robotPosition.pan += 0.01f;
            }

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);
        }
        */

        /*
        private void test_survey(int offset_x, int offset_y)
        {
            pos3D robotPosition = new pos3D(0, 0, 0);
            pos3D robotOrientation = new pos3D(0, 0, 0);
            occupancygridMultiResolution grid = new occupancygridMultiResolution(1, 128, 64);

            beginStopWatch();

            for (int cam = 0; cam < 4; cam++)
            {
                //invent some stereo features
                for (int f = 0; f < stereo_features.Length; f += 3)
                {
                    stereo_features[f] = rnd.Next(robot_head.image_width - 1);
                    stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);
                    stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                }
                robot_head.setStereoFeatures(cam, stereo_features, stereo_uncertainties, stereo_features.Length);
            }
            view[0] = stereo_model.createViewpoint(robot_head, robotOrientation);
            //view[0].show(raysimage, 2000, 155, 155, 155);

            robotPosition.x = offset_x;
            robotPosition.y = offset_y;
            robotPosition.pan = 0.2f;  //seems to be something peculiar about a value of 0.01
            view[1] = view[0].createTrialPose(robotPosition.pan, robotPosition.x, robotPosition.y);
            pos3D centre = new pos3D(0, 0, 0);
            grid.insert(view[1], true, centre);
            //view[1].show(raysimage, 2000, 0, 0, 155);

            ArrayList intersects = new ArrayList();
            view[0].matchingScore(view[1], 200, 50, intersects);
            //view[0].showPoints(raysimage, intersects, 2000, 255, 255, 0);

            float max_score = 0;
            int ray_thickness = 13;
            int no_of_trial_poses = 227;
            bool pruneSurvey = false;
            int randomSeed = 1000;
            int pruneThreshold = 20;
            float survey_angular_offset = 0.0f;
            pos3D odometry_position = new pos3D(0, 0, 0);
            ArrayList survey_results = stereo_model.surveyXYP(view[0], grid, 1000, 1000, no_of_trial_poses, ray_thickness, pruneSurvey, randomSeed, pruneThreshold, survey_angular_offset, odometry_position, 0.0f, ref max_score);
            float peak_x = 0;
            float peak_y = 0;
            float peak_pan = 0;
            stereo_model.SurveyPeak(survey_results, ref peak_x, ref peak_y);
            float[] peak = stereo_model.surveyPan(view[0], grid, ray_thickness, peak_x, peak_y);
            peak_pan = stereo_model.SurveyPeakPan(peak);
            
            stereo_model.showSurveyArea(img_rays, standard_width, standard_height, survey_results, robotPosition.x, robotPosition.y, robotPosition.pan, peak_pan);

            //calculate the position error
            float dx = peak_x - robotPosition.x;
            float dy = peak_y - robotPosition.y;
            float position_error = (float)Math.Sqrt((dx * dx) + (dy * dy));
            txtPositionError.Text = Convert.ToString(position_error);

            float angular_error = Math.Abs((peak_pan - robotPosition.pan) / (float)Math.PI * 180);
            txtAngularError.Text = Convert.ToString(angular_error);

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);
        }
        */

        /*
        private void test_survey_pan(int offset_x, int offset_y)
        {
            pos3D robotPosition = new pos3D(0, 0, 0);

            beginStopWatch();

            for (int cam = 0; cam < 4; cam++)
            {
                //invent some stereo features
                for (int f = 0; f < stereo_features.Length; f += 3)
                {
                    stereo_features[f] = rnd.Next(robot_head.image_width - 1);
                    stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);
                    stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                }
                robot_head.setStereoFeatures(cam, stereo_features, stereo_features.Length);
            }
            view[0] = stereo_model.createViewpoint(robot_head);
            //view[0].show(raysimage, 2000, 155, 155, 155);

            robotPosition.x = offset_x;
            robotPosition.y = offset_y;
            robotPosition.pan = 0.1f;
            view[1] = view[0].createTrialPose(robotPosition.pan, robotPosition.x, robotPosition.y);
            //view[1].show(raysimage, 2000, 0, 0, 155);

            ArrayList intersects = new ArrayList();
            view[0].matchingScore(view[1], 200, 50, intersects);
            //view[0].showPoints(raysimage, intersects, 2000, 255, 255, 0);

            float max_score = 0;
            float separation_tollerance = 1.065f;
            int ray_thickness = 11;
            int no_of_trial_poses = 227;
            bool pruneSurvey = false;
            ArrayList survey_results = stereo_model.surveyXYP(view[0], view[1], 1000, 1000, no_of_trial_poses, separation_tollerance, ray_thickness, pruneSurvey, ref max_score);
            float peak_x = 0;
            float peak_y = 0;
            stereo_model.SurveyPeak(survey_results, ref peak_x, ref peak_y);
            float[] peak = stereo_model.surveyPan(view[0], view[1], separation_tollerance, ray_thickness, peak_x, peak_y);
            float peak_pan = stereo_model.SurveyPeakPan(peak);
            stereo_model.showSurveyPan(peak, img_rays, standard_width, standard_height, robotPosition.pan, peak_pan);

            //calculate the position error
            float dx = peak_x - robotPosition.x;
            float dy = peak_y - robotPosition.y;
            float position_error = (float)Math.Sqrt((dx * dx) + (dy * dy));
            txtPositionError.Text = Convert.ToString(position_error);

            float angular_error = Math.Abs((peak_pan - robotPosition.pan) / (float)Math.PI * 180);
            txtAngularError.Text = Convert.ToString(angular_error);

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);
        }
        */
    }
}