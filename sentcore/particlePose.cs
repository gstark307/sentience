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
using System.Collections;
using System.Collections.Generic;
using sentience.calibration;
using sluggish.utilities;

namespace sentience.core
{
    /// <summary>
    /// represents a possible robot pose
    /// </summary>
    public sealed class particlePose
    {
        // position of the pose in millimetres
        public float x, y;

        // pan angle in radians
        public float pan;

        // grid cells which were observed from this pose
        private List<particleGridCell> observed_grid_cells;

        // the path with which this pose is associated
        public particlePath path;
        public List<particlePath> previous_paths;

        // the time step on which this particle was created
        public UInt32 time_step;

        // the parent of this pose
        public particlePose parent = null;

        // the number of child poses derived from this one
        public int no_of_children = 0;

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x">x position in millimetres</param>
        /// <param name="y">y position in millimetres</param>
        /// <param name="pan">pan angle in radians</param>
        /// <param name="path">the path which this pose belongs to</param>
        public particlePose(float x, float y, 
                            float pan, 
                            particlePath path)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.path = path;
            observed_grid_cells = new List<particleGridCell>();
        }

        #endregion

        #region "adding new occupancy hypotheses"

        /// <summary>
        /// add a new grid hypothesis to this pose
        /// </summary>
        /// <param name="hypothesis">occupancy hypothesis for a grid cell</param>
        /// <param name="radius_cells">local map cache radius in cells</param>
        /// <param name="grid_dimension">dimension of the occupancy grid in cells</param>
        /// <param name="grid_dimension_vertical">height of the occupancy grid (z axis) in cells</param>
        /// <returns>true if the hypothesis was added</returns>
        public bool AddHypothesis(particleGridCell hypothesis, 
                                  int radius_cells, 
                                  int grid_dimension, 
                                  int grid_dimension_vertical)
        {
            bool added = false;

            if (path != null)
            {
                added = path.Add(hypothesis, radius_cells, grid_dimension, grid_dimension_vertical);
                if (added) observed_grid_cells.Add(hypothesis);
            }
            return (added);
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// Show a diagram of the robot in this pose
        /// This is useful for checking that the positions of cameras have 
        /// been calculated correctly
        /// </summary>
        /// <param name="robot">robot object</param>
        /// <param name="img">image as a byte array</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="clearBackground">clear the background before drawing</param>
        /// <param name="min_x_mm">top left x coordinate</param>
        /// <param name="min_y_mm">top left y coordinate</param>
        /// <param name="max_x_mm">bottom right x coordinate</param>
        /// <param name="max_y_mm">bottom right y coordinate</param>
        /// <param name="showFieldOfView">whether to show the field of view of each camera</param>
        public void Show(robot rob, 
                         Byte[] img, int width, int height, bool clearBackground,
                         int min_x_mm, int min_y_mm,
                         int max_x_mm, int max_y_mm, int line_width,
                         bool showFieldOfView)
        {
            if (clearBackground)
                for (int i = 0; i < width * height * 3; i++)
                    img[i] = 255;

            // get the positions of the head and cameras for this pose
            pos3D head_location = new pos3D(0, 0, 0);
            pos3D[] camera_centre_location = new pos3D[rob.head.no_of_stereo_cameras];
            pos3D[] left_camera_location = new pos3D[rob.head.no_of_stereo_cameras];
            pos3D[] right_camera_location = new pos3D[rob.head.no_of_stereo_cameras];
            calculateCameraPositions(rob, ref head_location,
                                     ref camera_centre_location,
                                     ref left_camera_location,
                                     ref right_camera_location);
            
            int w = max_x_mm - min_x_mm;
            int h = max_y_mm - min_y_mm;

            // draw the body
            int xx = (int)((x - min_x_mm) * width / w);
            int yy = (int)((y - min_y_mm) * height / h);
            int wdth = (int)(rob.BodyWidth_mm * width / w);
            int hght = (int)(rob.BodyLength_mm * height / h);
            drawing.drawBox(img, width, height, xx, yy, wdth, hght, pan, 0, 255, 0, line_width);

            // draw the head
            xx = (int)((head_location.x - min_x_mm) * width / w);
            yy = (int)((head_location.y - min_y_mm) * height / h);
            int radius = (int)(rob.HeadSize_mm * width / w);
            drawing.drawBox(img, width, height, xx, yy, radius, radius, head_location.pan, 0, 255, 0, line_width);

            // draw the cameras
            for (int cam = 0; cam < rob.head.no_of_stereo_cameras; cam++)
            {
                // draw the left camera
                int xx1 = (int)((left_camera_location[cam].x - min_x_mm) * width / w);
                int yy1 = (int)((left_camera_location[cam].y - min_y_mm) * height / h);
                wdth = (int)((rob.head.calibration[cam].baseline / 4) * width / w);
                hght = (int)((rob.head.calibration[cam].baseline / 12) * height / h);
                if (hght < 1) hght = 1;
                drawing.drawBox(img, width, height, xx1, yy1, wdth, hght, left_camera_location[cam].pan + (float)(Math.PI / 2), 0, 255, 0, line_width);

                // draw the right camera
                int xx2 = (int)((right_camera_location[cam].x - min_x_mm) * width / w);
                int yy2 = (int)((right_camera_location[cam].y - min_y_mm) * height / h);
                drawing.drawBox(img, width, height, xx2, yy2, wdth, hght, right_camera_location[cam].pan + (float)(Math.PI / 2), 0, 255, 0, line_width);

                if (showFieldOfView)
                {
                    float half_FOV = rob.head.calibration[cam].leftcam.camera_FOV_degrees * (float)Math.PI / 360.0f;
                    int xx_ray = xx1 + (int)(width * Math.Sin(left_camera_location[cam].pan + half_FOV));
                    int yy_ray = yy1 + (int)(width * Math.Cos(left_camera_location[cam].pan + half_FOV));
                    drawing.drawLine(img, width, height, xx1, yy1, xx_ray, yy_ray, 200, 200, 255, 0, false);
                    xx_ray = xx1 + (int)(width * Math.Sin(left_camera_location[cam].pan - half_FOV));
                    yy_ray = yy1 + (int)(width * Math.Cos(left_camera_location[cam].pan - half_FOV));
                    drawing.drawLine(img, width, height, xx1, yy1, xx_ray, yy_ray, 200, 200, 255, 0, false);
                    xx_ray = xx2 + (int)(width * Math.Sin(right_camera_location[cam].pan + half_FOV));
                    yy_ray = yy2 + (int)(width * Math.Cos(right_camera_location[cam].pan + half_FOV));
                    drawing.drawLine(img, width, height, xx2, yy2, xx_ray, yy_ray, 200, 200, 255, 0, false);
                    xx_ray = xx2 + (int)(width * Math.Sin(right_camera_location[cam].pan - half_FOV));
                    yy_ray = yy2 + (int)(width * Math.Cos(right_camera_location[cam].pan - half_FOV));
                    drawing.drawLine(img, width, height, xx2, yy2, xx_ray, yy_ray, 200, 200, 255, 0, false);
                }
            }
        }

        /// <summary>
        /// calculate the position of the robots head and cameras for this pose
        /// </summary>
        /// <param name="rob"></param>
        /// <param name="head_location">location of the centre of the head</param>
        /// <param name="camera_centre_location">location of the centre of each stereo camera</param>
        /// <param name="left_camera_location">location of the left camera within each stereo camera</param>
        /// <param name="right_camera_location">location of the right camera within each stereo camera</param>
        public void calculateCameraPositions(robot rob,
                                             ref pos3D head_location,
                                             ref pos3D[] camera_centre_location,
                                             ref pos3D[] left_camera_location,
                                             ref pos3D[] right_camera_location)
        {
            // calculate the position of the centre of the head relative to 
            // the centre of rotation of the robots body
            pos3D head_centroid = new pos3D(-(rob.BodyWidth_mm / 2) + rob.head.x,
                                            -(rob.BodyLength_mm / 2) + rob.head.y,
                                            rob.head.z);

            // location of the centre of the head on the grid map
            // adjusted for the robot pose and the head pan and tilt angle.
            // Note that the positions and orientations of individual cameras
            // on the head have already been accounted for within stereoModel.createObservation
            pos3D head_locn = head_centroid.rotate(rob.head.pan + pan, rob.head.tilt, 0);
            head_locn = head_locn.translate(x, y, 0);
            head_location.copyFrom(head_locn);

            for (int cam = 0; cam < rob.head.no_of_stereo_cameras; cam++)
            {
                // calculate the position of the centre of the stereo camera
                // (baseline centre point)
                pos3D camera_centre_locn = new pos3D(rob.head.calibration[cam].positionOrientation.x, rob.head.calibration[cam].positionOrientation.y, rob.head.calibration[cam].positionOrientation.y);
                camera_centre_locn = camera_centre_locn.rotate(rob.head.calibration[cam].positionOrientation.pan + rob.head.pan + pan, rob.head.calibration[cam].positionOrientation.tilt, rob.head.calibration[cam].positionOrientation.roll);
                camera_centre_location[cam] = camera_centre_locn.translate(head_location.x, head_location.y, head_location.z);

                // where are the left and right cameras?
                // we need to know this for the origins of the vacancy models
                float half_baseline_length = rob.head.calibration[cam].baseline / 2;
                pos3D left_camera_locn = new pos3D(-half_baseline_length, 0, 0);
                left_camera_locn = left_camera_locn.rotate(rob.head.calibration[cam].positionOrientation.pan + rob.head.pan + pan, rob.head.calibration[cam].positionOrientation.tilt, rob.head.calibration[cam].positionOrientation.roll);
                pos3D right_camera_locn = new pos3D(-left_camera_locn.x, -left_camera_locn.y, -left_camera_locn.z);
                left_camera_location[cam] = left_camera_locn.translate(camera_centre_location[cam].x, camera_centre_location[cam].y, camera_centre_location[cam].z);
                right_camera_location[cam] = right_camera_locn.translate(camera_centre_location[cam].x, camera_centre_location[cam].y, camera_centre_location[cam].z);
                right_camera_location[cam].pan = left_camera_location[cam].pan;
            }
        }

        #endregion

        #region "adding a new observation as a set of stereo rays"

        /// <summary>
        /// add an observation taken from this pose
        /// </summary>
        /// <param name="stereo_rays">list of ray objects in this observation</param>
        /// <param name="rob">robot object</param>
        /// <param name="LocalGrid">occupancy grid into which to insert the observation</param>
        /// <param name="localiseOnly">if true does not add any mapping particles (pure localisation)</param>
        /// <returns>localisation matching score</returns>
        public float AddObservation(List<evidenceRay>[] stereo_rays,
                                    robot rob, 
                                    occupancygridMultiHypothesis LocalGrid,
                                    bool localiseOnly)
        {
            // clear the localisation score
            float localisation_score = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;

            // get the positions of the head and cameras for this pose
            pos3D head_location = new pos3D(0,0,0);
            pos3D[] camera_centre_location = new pos3D[rob.head.no_of_stereo_cameras];
            pos3D[] left_camera_location = new pos3D[rob.head.no_of_stereo_cameras];
            pos3D[] right_camera_location = new pos3D[rob.head.no_of_stereo_cameras];
            calculateCameraPositions(rob, ref head_location,
                                     ref camera_centre_location,
                                     ref left_camera_location,
                                     ref right_camera_location);
            
            // itterate for each stereo camera
            int cam = stereo_rays.Length - 1;
            while (cam >= 0)
            {
                // itterate through each ray
                int r = stereo_rays[cam].Count - 1;
                while (r >= 0)
                {
                    // observed ray.  Note that this is in an egocentric
                    // coordinate frame relative to the head of the robot
                    evidenceRay ray = stereo_rays[cam][r];

                    // translate and rotate this ray appropriately for the pose
                    evidenceRay trial_ray = ray.trialPose(camera_centre_location[cam].pan, 
                                                          camera_centre_location[cam].x, 
                                                          camera_centre_location[cam].y);

                    // update the grid cells for this ray and update the
                    // localisation score accordingly
                    float score =
                        LocalGrid.Insert(trial_ray, this, 
                                         rob.head.sensormodel[cam],
                                         left_camera_location[cam], 
                                         right_camera_location[cam],
                                         localiseOnly);
                    if (score != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        if (localisation_score != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                            localisation_score += score;
                        else
                            localisation_score = score;
                    r--;
                }
                cam--;
            }

            return (localisation_score);
        }

        #endregion

        #region "removing this pose"

        /// <summary>
        /// remove the mapping particles associated with this pose from the given grid
        /// </summary>
        /// <param name="grid">grid from which this pose is to be removed</param>
        public void Remove(occupancygridMultiHypothesis grid)
        {
            for (int i = observed_grid_cells.Count-1; i >= 0; i--)
            {
                particleGridCell hypothesis = observed_grid_cells[i];
                grid.Remove(hypothesis);
            }
        }

        #endregion

        #region "distillation"

        /// <summary>
        /// distills all grid particles associated with this pose
        /// </summary>
        /// <param name="grid"></param>
        public void Distill(occupancygridMultiHypothesis grid)
        {
            for (int i = observed_grid_cells.Count-1; i >= 0; i--)
            {
                particleGridCell hypothesis = observed_grid_cells[i];
                grid.Distill(hypothesis);
            }
        }

        #endregion

    }
}
