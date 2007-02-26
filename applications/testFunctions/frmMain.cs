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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using sentience.core;

namespace WindowsApplication1
{
    public partial class frmMain : common
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
        viewpoint[] view;
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

            grid_layer = new float[grid_dimension, grid_dimension, 3];

            pos3D_x = new float[4];
            pos3D_y = new float[4];

            stereo_model = new stereoModel();
            robot_head = new stereoHead(4);            
            stereo_features = new float[900];
            stereo_uncertainties = new float[900];
            view = new viewpoint[10];

            img_rays = new Byte[standard_width * standard_height * 3];
            rays = new Bitmap(standard_width, standard_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picRays.Image = rays;

            stereo_model.showProbabilities(grid_layer, grid_dimension, img_rays, standard_width, standard_height, false);
            //stereo_model.showDistribution(img_rays, standard_width, standard_height);
            //stereo_model.showSurveyDistribution(500, img_rays, standard_width, standard_height);

            //stereo_model.updateRayModel(grid_layer, grid_dimension, img_rays, standard_width, standard_height);

            //test_head();
            //test_head2();
            //test_grid();
            //test_trial_poses();
            //test_intercepts();

            int offset_x = rnd.Next(1000) - 500;
            int offset_y = rnd.Next(1000) - 500;
            //test_survey(offset_x, offset_y);
            //test_survey_pan(50, 50);

            updatebitmap_unsafe(img_rays, (Bitmap)picRays.Image);
            //picRays.Refresh();
        }

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


        private void test_grid()
        {
            occupancygrid grd = new occupancygrid(128, 32);
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


        private void test_intercepts()
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
                    //stereo_features[f + 1] = robot_head.image_height / 2; // rnd.Next(robot_head.image_height - 1);
                    stereo_features[f + 1] = rnd.Next(robot_head.image_height - 1);
                    stereo_features[f + 2] = rnd.Next(robot_head.image_width * 35 / 100);
                }
                robot_head.setStereoFeatures(cam, stereo_features, stereo_uncertainties, stereo_features.Length);
            }
            view[0] = stereo_model.createViewpoint(robot_head, robotOrientation);
            view[0].showAbove(img_rays, standard_width, standard_height, 2000, 155, 155, 155, true, 0, false);

            robotPosition.y += 200;
            robotPosition.pan += 0.03f;
            view[1] = view[0].createTrialPose(robotPosition.pan, robotPosition.x, robotPosition.y);
            view[1].showAbove(img_rays, standard_width, standard_height, 2000, 0, 0, 255, true, 0, false);

            ArrayList intersects = new ArrayList();
            view[0].matchingScore(view[1], 200, 80, intersects);
            view[0].showPoints(img_rays, standard_width, standard_height, intersects, 2000, 255, 255, 0);

            long mappingTime = endStopWatch();
            txtMappingTime.Text = Convert.ToString(mappingTime);
        }

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