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
    public class robotTestTracks
    {
        public String Name;

        public int no_of_mapping_features = 500;
        public int no_of_localisation_features = 50;

        public int noOfMappingTracks = 0;
        ArrayList mapping_tracks = new ArrayList();

        public int noOfLocalisationTracks = 0;
        ArrayList localisation_tracks = new ArrayList();

        // this stuff is used for displaying a sequences of images and their stereo features
        public ArrayList image_filenames_stereoCamera0 = new ArrayList();
        public ArrayList features_stereoCamera0 = new ArrayList();
        public ArrayList image_filenames_stereoCamera1 = new ArrayList();
        public ArrayList features_stereoCamera1 = new ArrayList();

        int calibration_offset_x = 0, calibration_offset_y = 0;

        public String errorMessage = "";


        public robotTestTracks()
        {
        }

        /// <summary>
        /// add a track to the list of test tracks
        /// </summary>
        /// <param name="track"></param>
        /// <param name="use_for_mapping"></param>
        public void Add(robotPath track, Boolean use_for_mapping)
        {
            if (use_for_mapping)
            {
                mapping_tracks.Add(track);
                noOfMappingTracks = mapping_tracks.Count;
            }
            else
            {
                localisation_tracks.Add(track);
                noOfLocalisationTracks = localisation_tracks.Count;
            }            
        }



        public void Add(robot rob, String path, String trackName, 
                        int starting_index, String TrackName, bool sentienceTrack, int trackType,
                        bool mappingOnly)
        {
            int starting_index1 = starting_index;
            int starting_index2 = starting_index;

            if (sentienceTrack)
            {
                //starting_index1 += 42;
                rob.correspondence.LoadCalibration(path + trackName + "\\calibration.txt", ref calibration_offset_x, ref calibration_offset_y);
            }

            Name = trackName;
            robotPath p;
            if (!mappingOnly)
            {
                p = getPath(rob, path, trackName, "L", starting_index1, TrackName, sentienceTrack, trackType);
                Add(p, false);
            }
            p = getPath(rob, path, trackName, "M", starting_index2, TrackName, sentienceTrack, trackType);
            Add(p, true);
        }

        public robotPath getMappingTrack(int track_index)
        {
            return ((robotPath)mapping_tracks[track_index]);
        }

        public robotPath getLocalisationTrack(int track_index)
        {
            return ((robotPath)localisation_tracks[track_index]);
        }

        public void testLocalisation(robot rob,
                                     int localisation_track_index,
                                     int localisation_test_index,
                                     occupancygridMultiResolution grid,
                                     int ray_thickness,
                                     int no_of_trial_poses,
                                     bool pruneSurvey,
                                     int survey_diameter_mm,
                                     int randomSeed, int pruneThreshold, float survey_angular_offset,
                                     float momentum,
                                     ref float error_x, ref float error_y, ref float error_pan,
                                     pos3D estimatedPos)
        {
            robotPath pathLocalisation = getLocalisationTrack(localisation_track_index);
            robotPath pathMap = getMappingTrack(localisation_track_index);

            viewpoint view_localisation = pathLocalisation.getViewpoint(localisation_test_index);
            viewpoint view_map = pathMap.getViewpoint(localisation_test_index);
            float max_score = 0;
            // position estimate from odometry, which positions the robot within the grid
            // as an initial estimate from which to search
            pos3D local_odometry_position = view_map.odometry_position.subtract(pathMap.pathCentre());
            ArrayList survey_results = rob.sensorModel.surveyXYP(view_localisation, grid, survey_diameter_mm, survey_diameter_mm,
                                                                 no_of_trial_poses, ray_thickness, pruneSurvey, randomSeed,
                                                                 pruneThreshold, survey_angular_offset, 
                                                                 local_odometry_position, momentum,
                                                                 ref max_score);
            float peak_x = 0;
            float peak_y = 0;
            float peak_pan = 0;
            rob.sensorModel.SurveyPeak(survey_results, ref peak_x, ref peak_y);
            //float[] peak = rob.sensorModel.surveyPan(view_map, view_localisation, separation_tollerance, ray_thickness, peak_x, peak_y);
            float[] peak = rob.sensorModel.surveyPan(view_localisation, grid, ray_thickness, peak_x, peak_y);
            peak_pan = rob.sensorModel.SurveyPeakPan(peak);

            float half_track_length = 1000;

            float dx = view_localisation.odometry_position.x - view_map.odometry_position.x;
            float dy = view_localisation.odometry_position.y - (view_map.odometry_position.y - half_track_length);
            float dp = view_localisation.odometry_position.pan - view_map.odometry_position.pan;
            error_x = dx + peak_x;
            error_y = dy + peak_y;
            error_pan = dp + peak_pan;
            error_pan = error_pan / (float)Math.PI * 180;

            estimatedPos.x = view_map.odometry_position.x - peak_x;
            estimatedPos.y = view_map.odometry_position.y - peak_y;
            estimatedPos.pan = view_map.odometry_position.pan - peak_pan;
        }

        /// <summary>
        /// create a map using all available track data
        /// </summary>
        /// <param name="grid"></param>
        public void testMapping(occupancygridMultiResolution grid)
        {
            robotPath pathLocalisation = getLocalisationTrack(0);
            robotPath pathMap = getMappingTrack(0);

            grid.Clear();
            grid.insert(pathMap);
            grid.insert(pathLocalisation);
        }


        // produces an X track pattern in the odometry data
        private void standardXTrack(int length_mm, int displacement_mm, robotOdometry odometry, String trackType)
        {
            float x, y, z, pan;

            if (trackType == "M")
            {
                for (int i = 0; i < odometry.no_of_measurements; i++)
                {
                    x = 0;
                    y = (i * length_mm) / odometry.no_of_measurements;
                    z = 0;
                    pan = 0;

                    odometry.setPosition(i, x, y, z, pan);
                }
            }
            else
            {
                float half_length = length_mm/2;
                float hyp = (float)Math.Sqrt((displacement_mm*displacement_mm)+(half_length*half_length));
                float angle = (float)Math.Asin(half_length / hyp);
                if (displacement_mm < 0) angle = -angle+(float)Math.PI;
                for (int i = 0; i < odometry.no_of_measurements; i++)
                {
                    float forward = (i * length_mm) / odometry.no_of_measurements;
                    x = (forward - half_length) * (float)Math.Cos(angle);
                    y = (forward - half_length) * (float)Math.Sin(angle);
                    z = 0;
                    pan = angle;

                    odometry.setPosition(i, x, y, z, pan);
                }
            }
        }

        /// <summary>
        /// defines a square shaped path where the robot moves (clockwise) forward, right, backward, left
        /// </summary>
        /// <param name="displacement_mm"></param>
        /// <param name="no_of_measurements_forward"></param>
        /// <param name="no_of_measurements_right"></param>
        /// <param name="odometry"></param>
        /// <param name="offset_mm"></param>
        private void squareTrack(int displacement_mm, int no_of_measurements_forward, int no_of_measurements_right, robotOdometry odometry, int offset_mm)
        {
            float pan=0;
            float x = 0, y = 0;
            int i;
            int side1 = no_of_measurements_forward * displacement_mm;
            int side2 = no_of_measurements_right * displacement_mm;

            for (int side = 0; side < 4; side++)
            {
                switch (side)
                {
                    case 0:
                        {
                            x = -(side2 / 2);
                            pan = 0;
                            for (i = 0; i < no_of_measurements_forward; i++)
                            {
                                y = -(side1 / 2) + (i * displacement_mm);
                                odometry.AddPosition(x, y + offset_mm, pan);
                            }
                            break;
                        }
                    case 1:
                        {
                            pan = (float)Math.PI * 3 / 2;                            
                            y += offset_mm;
                            for (i = 0; i < no_of_measurements_right; i++)
                            {
                                x += displacement_mm;
                                odometry.AddPosition(x + offset_mm, y, pan);
                            }
                            break;
                        }
                    case 2:
                        {
                            pan = (float)Math.PI;
                            for (i = 0; i < no_of_measurements_forward; i++)
                            {
                                y -= displacement_mm;
                                odometry.AddPosition(x, y - offset_mm, pan);
                            }
                            break;
                        }
                    case 3:
                        {
                            pan = (float)Math.PI / 2;
                            y = -(side1 / 2);
                            for (i = 0; i < no_of_measurements_right; i++)
                            {
                                x -= displacement_mm;
                                odometry.AddPosition(x - offset_mm, y, pan);
                            }
                            break;
                        }
                }
            }
        }


        private void squareAndBack(int displacement_mm, int no_of_measurements_forward, int no_of_measurements_right, robotOdometry odometry, int offset_mm)
        {
            squareTrack(displacement_mm, no_of_measurements_forward, no_of_measurements_right, odometry, offset_mm);

            float x, y, pan;
            int i;
            int side1 = no_of_measurements_forward * displacement_mm;
            int side2 = no_of_measurements_right * displacement_mm;
            x = -(side2 / 2);
            y = -(side1 / 2);

            for (int side = 0; side < 4; side++)
            {
                switch (side)
                {
                    case 0:
                        {
                            pan = (float)Math.PI * 3 / 2;                            
                            for (i = 0; i < no_of_measurements_right; i++)
                            {
                                x += displacement_mm;
                                odometry.AddPosition(x + offset_mm, y, pan);
                            }
                            break;
                        }
                    case 1:
                        {
                            pan = 0;
                            for (i = 0; i < no_of_measurements_forward; i++)
                            {
                                y += displacement_mm;
                                odometry.AddPosition(x, y + offset_mm, pan);
                            }
                            break;
                        }
                    case 2:
                        {
                            pan = (float)Math.PI / 2;
                            for (i = 0; i < no_of_measurements_right; i++)
                            {
                                x -= displacement_mm;
                                odometry.AddPosition(x - offset_mm, y, pan);
                            }
                            break;
                        }
                    case 3:
                        {
                            pan = (float)Math.PI;
                            for (i = 0; i < no_of_measurements_forward; i++)
                            {
                                y -= displacement_mm;
                                odometry.AddPosition(x, y - offset_mm, pan);
                            }
                            break;
                        }
                }
            }

        }


        private void specialTrack(int displacement_mm, int no_of_measurements_x, int no_of_measurements_y, int lines, robotOdometry odometry, int offset_mm)
        {
            int clip = 0;

            for (int l = 0; l < lines; l++)
            {
                if (l == lines - 1) clip = 2;
                thereAndBack(displacement_mm, no_of_measurements_y, odometry, offset_mm, -l * 500, clip);
            }
            for (int l = 0; l < lines; l++)
            {
                thereAndBack_x(displacement_mm, no_of_measurements_x, odometry, offset_mm, 0, (no_of_measurements_y * displacement_mm) - (l * 500));
            }
        }


        private void thereAndBack(int displacement_mm, int no_of_measurements_there, robotOdometry odometry, int offset_mm, int offset_x_mm, int clip)
        {
            float x, y = 0, y2 = 0, pan=0, tot=0;
            float length_mm = displacement_mm * no_of_measurements_there;
            
            for (int i = 0; i < (no_of_measurements_there*2)-clip; i++)
            {
                x = 0;
                if (tot < length_mm)
                {
                    y = (i * displacement_mm) + offset_mm;
                    y2 = y;
                    pan = 0;
                }
                else
                {
                    y2 -= displacement_mm;
                    y = y2 + displacement_mm - offset_mm;
                    pan = (float)Math.PI;
                }                

                odometry.AddPosition(x + offset_x_mm, y, pan);

                tot += displacement_mm;
            }
        }


        private void thereAndBack_x(int displacement_mm, int no_of_measurements_there, robotOdometry odometry, int offset_mm, int offset_x_mm, int offset_y_mm)
        {
            float x = 0, y = 0, x2 = 0, pan = 0, tot = 0;
            float length_mm = displacement_mm * no_of_measurements_there;

            for (int i = 0; i < no_of_measurements_there * 2; i++)
            {
                y = 0;
                if (tot < length_mm)
                {
                    x = (i * displacement_mm) + offset_mm;
                    x2 = x;
                    pan = (float)Math.PI / 2;
                    //pan = (float)Math.PI*3/2;
                }
                else
                {
                    x2 -= displacement_mm;
                    x = x2 + displacement_mm - offset_mm;
                    pan = (float)Math.PI * 3 / 2;
                    //pan = (float)Math.PI/2;
                }

                odometry.AddPosition(-x + offset_x_mm, y + offset_y_mm, pan);

                tot += displacement_mm;
            }
        }


        private robotPath getPath(robot rob, String path, String track, String index, int starting_index, String TrackName, bool sentienceTrack, int trackType)
        {
            robotPath rpath = new robotPath();
            robotOdometry odometry = new robotOdometry();

            if (sentienceTrack)
            {
                odometry.readPathFileSentience(path + track + "\\" + TrackName + ".path", index, rob);

                switch (trackType)
                {
                    case 1:
                        {
                            // mapping X track
                            standardXTrack(2000, 500, odometry, index);
                            break;
                        }
                    case 2:
                        {
                            //localisation X track
                            standardXTrack(2000, -500, odometry, index);
                            break;
                        }
                    case 3:
                        {
                            //there and back track
                            odometry.Clear();
                            thereAndBack(100, 46, odometry, 110, 0, 0);
                            break;
                        }
                    case 4:
                        {
                            //square
                            odometry.Clear();
                            squareAndBack(100, 10, 6, odometry, 110);
                            break;
                        }
                    case 5:
                        {
                            //special
                            odometry.Clear();
                            specialTrack(100, 16, 21, 3, odometry, 110);
                            break;
                        }
                }                    

                if (odometry.no_of_measurements == 0)
                {
                    errorMessage = path + track + "\\" + TrackName + ".path not found or contains no data.  ";
                    errorMessage += "Check that the path file is included as 'Content' within the project folder";
                }
            }
            else
            {
                odometry.readPathFile(path + track + "\\" + track + ".path", index);

                if (odometry.no_of_measurements == 0)
                    errorMessage = path + track + "\\" + track + ".path not found or contains no data";
            }


            for (int distance_index = 0; distance_index < odometry.no_of_measurements; distance_index++)
            {
                pos3D pos = odometry.getPosition(distance_index);
                rob.x = pos.x;
                rob.y = pos.y;
                rob.z = pos.z;
                rob.pan = pos.pan;
                rob.tilt = pos.tilt;
                rob.roll = pos.roll;

                if (sentienceTrack)
                    loadGlimpseSentience(rob, path + track + "\\", TrackName, index, distance_index + starting_index, odometry.no_of_measurements, index);
                else
                    loadGlimpse(rob, path + track + "\\", index, distance_index + starting_index, index);

                rob.updatePath(rpath);
            }
            return (rpath);
        }

        public float loadGlimpse(robot rob, String path, String track, int distance_index, String index)
        {
            float matching_score = 0;

            Byte[] left_bmp=null;
            Byte[] right_bmp=null;
            String filename, image_filename;

            filename = path + track + "\\Rect\\";
            if (track == "L")
                filename += "RMeters_" + Convert.ToString(distance_index) + ".";
            else
                filename += "RFeet_" + Convert.ToString(distance_index) + ".";

            // use different numbers of features for mapping and localisation
            if (index == "M")
                rob.correspondence.setRequiredFeatures(500);
            else
                rob.correspondence.setRequiredFeatures(100);

            int stereo_cam_index = 0;
            bool left_camera = true;
            for (int i = 0; i < rob.head.no_of_cameras*2; i++)
            {
                image_filename = filename + Convert.ToString(i) + ".pgm";
                rob.head.imageFilename[i] = image_filename;

                if (left_camera)
                    left_bmp = util.loadFromPGM(image_filename, rob.head.image_width, rob.head.image_height, 1);
                else
                {
                    right_bmp = util.loadFromPGM(image_filename, rob.head.image_width, rob.head.image_height, 1);
                    matching_score += rob.loadRectifiedImages(stereo_cam_index, left_bmp, right_bmp, 1);

                    // store images and features for later display
                    if (stereo_cam_index == 0)
                    {
                        image_filenames_stereoCamera0.Add(rob.head.imageFilename[i - 1]);
                        image_filenames_stereoCamera0.Add(rob.head.imageFilename[i]);
                        features_stereoCamera0.Add(rob.head.features[stereo_cam_index]);
                    }
                    else
                    {
                        image_filenames_stereoCamera1.Add(rob.head.imageFilename[i - 1]);
                        image_filenames_stereoCamera1.Add(rob.head.imageFilename[i]);
                        features_stereoCamera1.Add(rob.head.features[stereo_cam_index]);
                    }

                    stereo_cam_index++;
                }
                left_camera = !left_camera;
            }

            return (matching_score);
        }


        public float loadGlimpseSentience(robot rob, String path, String TrackName, String track, int distance_index, int pathPoints, String index)
        {
            float matching_score = 0;

            Byte[] left_bmp = null;
            Byte[] right_bmp = null;
            String image_filename;

            // use different numbers of features for mapping and localisation
            if (index == "M")
                rob.correspondence.setRequiredFeatures(no_of_mapping_features);
            else
                rob.correspondence.setRequiredFeatures(no_of_localisation_features);

            int stereo_cam_index = 0;
            bool left_camera = true;
            for (int i = 0; i < rob.head.no_of_cameras * 2; i++)
            {
                if (left_camera)
                {
                    image_filename = path + TrackName + "_left_" + Convert.ToString(distance_index + (stereo_cam_index * pathPoints)) + ".bmp";
                    if (!File.Exists(image_filename)) errorMessage = image_filename + " not found";
                    rob.head.imageFilename[i] = image_filename;
                    left_bmp = util.loadFromBitmap(image_filename, rob.head.image_width, rob.head.image_height, 3);
                }
                else
                {
                    image_filename = path + TrackName + "_right_" + Convert.ToString(distance_index + (stereo_cam_index * pathPoints)) + ".bmp";
                    if (!File.Exists(image_filename)) errorMessage = image_filename + " not found";
                    rob.head.imageFilename[i] = image_filename;
                    right_bmp = util.loadFromBitmap(image_filename, rob.head.image_width, rob.head.image_height, 3);
                    matching_score += rob.loadRectifiedImages(stereo_cam_index, left_bmp, right_bmp, 3);

                    // store images and features for later display
                    if (stereo_cam_index == 0)
                    {
                        image_filenames_stereoCamera0.Add(rob.head.imageFilename[i - 1]);
                        image_filenames_stereoCamera0.Add(rob.head.imageFilename[i]);
                        features_stereoCamera0.Add(rob.head.features[stereo_cam_index]);
                    }
                    else
                    {
                        image_filenames_stereoCamera1.Add(rob.head.imageFilename[i - 1]);
                        image_filenames_stereoCamera1.Add(rob.head.imageFilename[i]);
                        features_stereoCamera1.Add(rob.head.features[stereo_cam_index]);
                    }

                    stereo_cam_index++;
                }
                left_camera = !left_camera;
            }

            return (matching_score);
        }

    }
}
