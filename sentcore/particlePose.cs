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
using System.Text;
using sentience.calibration;

namespace sentience.core
{
    public class particlePose
    {
        // position of the pose in millimetres
        public float x, y;

        // pan angle in radians
        public float pan;

        // grid cells (particlePoseObservedGridCell) which were observed from this pose
        public ArrayList observed_grid_cells;

        // the path with which this pose is associated
        public particlePath path;
        public ArrayList previous_paths;

        // the time step on which this particle was created
        public UInt32 time_step;

        public particlePose parent = null;
        public int no_of_children = 0;

        public particlePose(float x, float y, float pan, particlePath path)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.path = path;
            observed_grid_cells = new ArrayList();
        }

        public pos3D subtract(pos3D pos)
        {
            pos3D sum = new pos3D(x, y, 0);
            sum.x = x - pos.x;
            sum.y = y - pos.y;
            sum.pan = pan - pos.pan;
            return (sum);
        }

        /// <summary>
        /// add a new grid hypothesis to this pose
        /// </summary>
        /// <param name="hypothesis">occupancy hypothesis for a grid cell</param>
        public bool AddHypothesis(particleGridCell hypothesis, int radius_cells, int grid_dimension, int grid_dimension_vertical)
        {
            bool added = false;

            if (path != null)
            {
                added = path.Add(hypothesis, radius_cells, grid_dimension, grid_dimension_vertical);
                if (added) observed_grid_cells.Add(hypothesis);
            }
            return (added);
        }

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
        /// <param name="min_x_mm"></param>
        /// <param name="min_y_mm"></param>
        /// <param name="max_x_mm"></param>
        /// <param name="max_y_mm"></param>
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
            pos3D[] camera_centre_location = new pos3D[rob.head.no_of_cameras];
            pos3D[] left_camera_location = new pos3D[rob.head.no_of_cameras];
            pos3D[] right_camera_location = new pos3D[rob.head.no_of_cameras];
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
            util.drawBox(img, width, height, xx, yy, wdth, hght, pan, 0, 255, 0, line_width);

            // draw the head
            xx = (int)((head_location.x - min_x_mm) * width / w);
            yy = (int)((head_location.y - min_y_mm) * height / h);
            int radius = (int)(rob.HeadSize_mm * width / w);
            util.drawBox(img, width, height, xx, yy, radius, radius, head_location.pan, 0, 255, 0, line_width);

            // draw the cameras
            for (int cam = 0; cam < rob.head.no_of_cameras; cam++)
            {
                // draw the left camera
                int xx1 = (int)((left_camera_location[cam].x - min_x_mm) * width / w);
                int yy1 = (int)((left_camera_location[cam].y - min_y_mm) * height / h);
                wdth = (int)((rob.head.calibration[cam].baseline / 4) * width / w);
                hght = (int)((rob.head.calibration[cam].baseline / 12) * height / h);
                if (hght < 1) hght = 1;
                util.drawBox(img, width, height, xx1, yy1, wdth, hght, left_camera_location[cam].pan + (float)(Math.PI/2), 0, 255, 0, line_width);

                // draw the right camera
                int xx2 = (int)((right_camera_location[cam].x - min_x_mm) * width / w);
                int yy2 = (int)((right_camera_location[cam].y - min_y_mm) * height / h);
                util.drawBox(img, width, height, xx2, yy2, wdth, hght, right_camera_location[cam].pan + (float)(Math.PI / 2), 0, 255, 0, line_width);

                if (showFieldOfView)
                {
                    float half_FOV = rob.head.calibration[cam].leftcam.camera_FOV_degrees * (float)Math.PI / 360.0f;
                    int xx_ray = xx1 + (int)(width * Math.Sin(left_camera_location[cam].pan + half_FOV));
                    int yy_ray = yy1 + (int)(width * Math.Cos(left_camera_location[cam].pan + half_FOV));
                    util.drawLine(img, width, height, xx1, yy1, xx_ray, yy_ray, 200, 200, 255, 0, false);
                    xx_ray = xx1 + (int)(width * Math.Sin(left_camera_location[cam].pan - half_FOV));
                    yy_ray = yy1 + (int)(width * Math.Cos(left_camera_location[cam].pan - half_FOV));
                    util.drawLine(img, width, height, xx1, yy1, xx_ray, yy_ray, 200, 200, 255, 0, false);
                    xx_ray = xx2 + (int)(width * Math.Sin(right_camera_location[cam].pan + half_FOV));
                    yy_ray = yy2 + (int)(width * Math.Cos(right_camera_location[cam].pan + half_FOV));
                    util.drawLine(img, width, height, xx2, yy2, xx_ray, yy_ray, 200, 200, 255, 0, false);
                    xx_ray = xx2 + (int)(width * Math.Sin(right_camera_location[cam].pan - half_FOV));
                    yy_ray = yy2 + (int)(width * Math.Cos(right_camera_location[cam].pan - half_FOV));
                    util.drawLine(img, width, height, xx2, yy2, xx_ray, yy_ray, 200, 200, 255, 0, false);
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

            for (int cam = 0; cam < rob.head.no_of_cameras; cam++)
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

        /// <summary>
        /// add an observation taken from this pose
        /// </summary>
        /// <param name="rays">list of ray objects in this observation</param>
        /// <param name="grid">the occupancy grid into which to insert the observation</param>
        /// <param name="sensormodel">the sensor model to use for updating the grid</param>
        /// <param name="head_pan">pan angle of the head</param>
        public float AddObservation(ArrayList[] stereo_rays, 
                                    robot rob)
        {
            // clear the localisation score
            float localisation_score = occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE;

            // get the positions of the head and cameras for this pose
            pos3D head_location = new pos3D(0,0,0);
            pos3D[] camera_centre_location = new pos3D[rob.head.no_of_cameras];
            pos3D[] left_camera_location = new pos3D[rob.head.no_of_cameras];
            pos3D[] right_camera_location = new pos3D[rob.head.no_of_cameras];
            calculateCameraPositions(rob, ref head_location,
                                     ref camera_centre_location,
                                     ref left_camera_location,
                                     ref right_camera_location);
            
            // itterate for each stereo camera
            int cam = 0;
            while (cam < stereo_rays.Length)
            {
                // itterate through each ray
                int r = 0;
                while (r < stereo_rays[cam].Count)
                {
                    // observed ray.  Note that this is in an egocentric
                    // coordinate frame relative to the head of the robot
                    evidenceRay ray = (evidenceRay)stereo_rays[cam][r];

                    // translate and rotate this ray appropriately for the pose
                    evidenceRay trial_ray = ray.trialPose(camera_centre_location[cam].pan, 
                                                          camera_centre_location[cam].x, 
                                                          camera_centre_location[cam].y);

                    // update the grid cells for this ray and update the
                    // localisation score accordingly
                    float score =
                        rob.LocalGrid.Insert(trial_ray, this, 
                                             rob.head.sensormodel[cam],
                                             left_camera_location[cam], 
                                             right_camera_location[cam]);
                    if (score != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                        if (localisation_score != occupancygridCellMultiHypothesis.NO_OCCUPANCY_EVIDENCE)
                            localisation_score += score;
                        else
                            localisation_score = score;
                    r++;
                }
                cam++;
            }

            return (localisation_score);
        }

        /// <summary>
        /// remove the mapping particles associated with this pose
        /// </summary>
        public void Remove(occupancygridMultiHypothesis grid)
        {
            for (int i = 0; i < observed_grid_cells.Count; i++)
            {
                particleGridCell hypothesis = (particleGridCell)observed_grid_cells[i];
                grid.Remove(hypothesis);
            }
        }

        /// <summary>
        /// distills all grid particles associated with this pose
        /// </summary>
        /// <param name="grid"></param>
        public void Distill(occupancygridMultiHypothesis grid)
        {
            for (int i = 0; i < observed_grid_cells.Count; i++)
            {
                particleGridCell hypothesis = (particleGridCell)observed_grid_cells[i];
                grid.Distill(hypothesis);
            }
        }

    }
}
