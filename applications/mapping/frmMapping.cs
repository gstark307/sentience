/*
    Sentience 3D Perception System: Mapping test program
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using sentience.core;

namespace StereoMapping
{
    public partial class frmMapping : common
    {
        float sigma = 0.005f;


        processstereo stereo = new processstereo();
        processstereo stereo2 = new processstereo();
        static String app_path = System.AppDomain.CurrentDomain.BaseDirectory + "\\";
        bool show_features = false;
        int position_index = 0;
        int no_of_glimpses = 1;

        robot sentience_robot;
        //Byte[] bmp;
        robotOdometry position;
        occupancygridMultiResolution map;
        robotTestTracks track;

        Bitmap bmpGrid;
        Byte[] img_grid;
        int grid_width = 400;
        int grid_height = 400;

        //String TrackName = "test";

        public frmMapping()
        {
            InitializeComponent();
            init();
        }

        private void init()
        {
            // load calibration data
            stereo.LoadCalibration();
            stereo2.LoadCalibration();

            sentience_robot = new robot(2, 100 * 6);
            sentience_robot.initRobotSingleStereo();
            sentience_robot.setMappingParameters(sigma);

            position = new robotOdometry();

            // grid map
            map = new occupancygridMultiResolution(1, 128, 40); //50);

            img_grid = new Byte[grid_width * grid_height * 3];
            bmpGrid = new Bitmap(grid_width, grid_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            picGrid.Image = bmpGrid;

            track = new robotTestTracks();
            int trackType;

            
            trackType = 3;
            no_of_glimpses = 45*2;
            track.Add(sentience_robot, app_path, "c1", 1, "test", true, trackType, true);
            

            /*
            trackType = 4;
            no_of_glimpses = 64;
            track.Add(sentience_robot, app_path, "r1", 1, "test", true, trackType, true);
             */
            
            /*
            trackType = 5;
            no_of_glimpses = ((21 * 2 * 3) - 2) + (16 * 2 * 3);
            track.Add(sentience_robot, app_path, "r2", 1, "test", true, trackType, true);
              */          
        }


        private void createMap()
        {
            map.Clear();
            map.insert(track.getMappingTrack(0));
            map.Save(0, "testmap.grd");
        }


        public void showStereoFeatures(robot rob, int position_index,
                               PictureBox picCamera0)
        {
            Byte[] bmp0 = null;

            String left_filename0 = (String)track.image_filenames_stereoCamera0[position_index*2];

            stereoFeatures left_features = (stereoFeatures)track.features_stereoCamera0[position_index];
            bmp0 = util.loadFromBitmap(left_filename0, rob.head.image_width, rob.head.image_height, 3);

            showStereoFeatures(rob.head.image_width, rob.head.image_height, 
                               bmp0, picCamera0, left_features, 9999);
        }

        public void showVacancyFunction(robot rob, PictureBox pic)
        {
            Byte[] bmp = new Byte[rob.head.image_width * rob.head.image_height * 3];
            map.showVacancyFunction(bmp, rob.head.image_width, rob.head.image_height);

            if (pic.Image == null)
                pic.Image = new Bitmap(rob.head.image_width, rob.head.image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            updatebitmap_unsafe(bmp, (Bitmap)pic.Image);

            pic.Refresh();

        }


        private void showRays(robot rob, PictureBox pic)
        {
            Byte[] bmp0 = null;
            Byte[] bmp1 = null;

            String left_filename = (String)track.image_filenames_stereoCamera0[position_index * 2];
            String right_filename = (String)track.image_filenames_stereoCamera0[(position_index * 2) + 1];

            bmp0 = util.loadFromBitmap(left_filename, rob.head.image_width, rob.head.image_height, 3);
            bmp1 = util.loadFromBitmap(right_filename, rob.head.image_width, rob.head.image_height, 3);

            // find stereo correspondences            
            stereo2.correspondence_algorithm_type = 0;
            stereo2.stereoMatch(bmp0, bmp1, rob.head.image_width, rob.head.image_height, true);

            if (pic.Image == null)
                pic.Image = new Bitmap(rob.head.image_width, rob.head.image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            updatebitmap_unsafe(stereo2.getRaysImage(rob.head.image_width, rob.head.image_height), (Bitmap)pic.Image);
            
            pic.Refresh();
        }

        public void showDepthMap(robot rob, int position_index,
                                 PictureBox pic)
        {
            Byte[] bmp0 = null;
            Byte[] bmp1 = null;

            String left_filename = (String)track.image_filenames_stereoCamera0[position_index * 2];
            String right_filename = (String)track.image_filenames_stereoCamera0[(position_index * 2) + 1];

            bmp0 = util.loadFromBitmap(left_filename, rob.head.image_width, rob.head.image_height, 3);
            bmp1 = util.loadFromBitmap(right_filename, rob.head.image_width, rob.head.image_height, 3);

            // find stereo correspondences
            stereo.correspondence_algorithm_type = 1;
            stereo.stereoMatch(bmp0, bmp1, rob.head.image_width, rob.head.image_height, true);

            Byte[] disp_bmp_data = new Byte[rob.head.image_width * rob.head.image_height * 3];
            stereo.getDisparityMap(disp_bmp_data, rob.head.image_width, rob.head.image_height, 0);
            if (pic.Image == null)
                pic.Image = new Bitmap(rob.head.image_width, rob.head.image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            updatebitmap_unsafe(disp_bmp_data, (Bitmap)pic.Image);
            pic.Refresh();
            /*
            String filename = "track_disparitymap" + Convert.ToString(position_index) + ".jpg";
            if (File.Exists(filename)) File.Delete(filename);
            pic.Image.Save(filename,System.Drawing.Imaging.ImageFormat.Jpeg);
             */
        }



        private void showStereoFeatures(int image_width, int image_height, Byte[] background, PictureBox pic, stereoFeatures features, int threshold)
        {
            Pen p;
            SolidBrush brush;
            Rectangle rect;
            int i, radius_x, max_radius_x, radius_y, max_radius_y, x, y;
            Graphics gr;

            if (background != null)
            {
                pic.Image = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                updatebitmap_unsafe(background, (Bitmap)pic.Image);

                gr = Graphics.FromImage(pic.Image);
                rect = new Rectangle();

                max_radius_x = pic.Image.Width / 30;
                max_radius_y = pic.Image.Width / 30;

                brush = new SolidBrush(Color.FromArgb(120, 0, 160, 0));
                p = new Pen(brush);

                for (i = 0; i < features.features.Length; i += 3)
                {
                    float disparity = features.features[i + 2];
                    radius_x = (int)(disparity / 2);
                    radius_y = radius_x;
                    x = (int)(features.features[i]);
                    y = (int)(features.features[i + 1]);
                    rect.X = x;
                    rect.Y = y;
                    rect.Width = radius_x * 2;
                    rect.Height = radius_y * 2;

                    if (disparity < threshold)
                        gr.FillEllipse(brush, rect);
                    else
                        gr.DrawEllipse(p, rect);
                }
                pic.Refresh();
            }
        }

        private void cmdMap_Click(object sender, EventArgs e)
        {
            createMap();
            MessageBox.Show("Map generated");
        }

        private void cmdShowFeatures_Click(object sender, EventArgs e)
        {
            position_index = 1;
            show_features = !show_features;
        }

        private void timAnimate_Tick(object sender, EventArgs e)
        {
            if (show_features)
            {
                lblPositionIndex.Text = Convert.ToString(position_index);
                showStereoFeatures(sentience_robot, position_index, picGrid);
                showDepthMap(sentience_robot, position_index, picDepthMap);
                showVacancyFunction(sentience_robot, picRays);
                //showRays(sentience_robot, picRays);
                position_index++;
                if (position_index >= no_of_glimpses) position_index = 1;
            }
        }

        private void frmMapping_Load(object sender, EventArgs e)
        {

        }

    }
}