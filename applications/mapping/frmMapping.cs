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
using System.Collections;
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
        // default path for loading and saving files
        String defaultPath = System.Windows.Forms.Application.StartupPath + "\\";

        // simulation object
        simulation sim;

        public frmMapping()
        {
            InitializeComponent();
            init();
        }

        private void init()
        {
            sim = new simulation(defaultPath + "robotdesign.xml", defaultPath);
            LoadSimulation(defaultPath + "simulation.xml");

            lstPathSegments.Items.Clear();
            lstPathSegments.Columns.Clear();
            lstPathSegments.FullRowSelect = true;

            //lst.Dock = DockStyle.Fill;
            lstPathSegments.View = View.Details;
            lstPathSegments.Sorting = SortOrder.None;

            // Create and initialize column headers for myListView.
            ColumnHeader columnHeader0 = new ColumnHeader();
            columnHeader0.Text = "x";
            columnHeader0.Width = 50;
            ColumnHeader columnHeader1 = new ColumnHeader();
            columnHeader1.Text = "y";
            columnHeader1.Width = 50;
            ColumnHeader columnHeader2 = new ColumnHeader();
            columnHeader2.Text = "heading";
            columnHeader2.Width = 70;
            ColumnHeader columnHeader3 = new ColumnHeader();
            columnHeader3.Text = "Steps";
            columnHeader3.Width = 50;
            ColumnHeader columnHeader4 = new ColumnHeader();
            columnHeader4.Text = "Size mm";
            columnHeader4.Width = 80;
            ColumnHeader columnHeader5 = new ColumnHeader();
            columnHeader5.Text = "Heading change";
            columnHeader5.Width = 100;

            // Add the column headers to myListView.
            lstPathSegments.Columns.AddRange(new ColumnHeader[] { columnHeader0, columnHeader1, 
                                                          columnHeader2, columnHeader3,
                                                          columnHeader4, columnHeader5  });
            showPathSegments();
            showNextPose();
        }

        private void LoadSimulation(String filename)
        {
            if (sim.Load(filename))
            {
                txtTitle.Text = sim.Name;
                txtRobotDefinitionFile.Text = sim.RobotDesignFile;
                txtStereoImagesPath.Text = sim.ImagesPath;
                update();
                showPathSegments();
                showNextPose();
            }
        }

        private void update()
        {
            sim.Name = txtTitle.Text;
            sim.RobotDesignFile = txtRobotDefinitionFile.Text;
            sim.ImagesPath = txtStereoImagesPath.Text;

            if (sim.RobotDesignFile != "")
            {
                sim.Reset();

                if (sim.ImagesPath != "")
                    sim.Save(defaultPath + "simulation.xml");
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            update();

            saveFileDialog1.DefaultExt = "xml";
            saveFileDialog1.FileName = "simulation_" + sim.rob.Name + ".xml";
            saveFileDialog1.Filter = "Xml files|*.xml";
            saveFileDialog1.Title = "Save simulation file";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                sim.Save(saveFileDialog1.FileName);
        }

        private void cmdRobotDefinitionBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Load robot design file";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtRobotDefinitionFile.Text = openFileDialog1.FileName;
                update();
            }

        }

        private void cmdStereoImagesPathBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Set stereo images path";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtStereoImagesPath.Text = folderBrowserDialog1.SelectedPath;
                update();
            }
        }

        private void frmMapping_FormClosing(object sender, FormClosingEventArgs e)
        {
            sim.Name = txtTitle.Text;
            sim.Save(defaultPath + "simulation.xml");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Open simulation file";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadSimulation(openFileDialog1.FileName);
                update();
            }
        }

        /// <summary>
        /// display the path segments within the list view
        /// </summary>
        private void showPathSegments()
        {
            // add items to the list
            lstPathSegments.Items.Clear();
            for (int i = 0; i < sim.pathSegments.Count; i++)
            {
                simulationPathSegment segment = (simulationPathSegment)sim.pathSegments[i];

                ListViewItem result = new ListViewItem(new string[] 
                                    {Convert.ToString(segment.x), 
                                     Convert.ToString(segment.y), 
                                     Convert.ToString(segment.pan*180.0f/(float)Math.PI),
                                     Convert.ToString(segment.no_of_steps),
                                     Convert.ToString(segment.distance_per_step_mm),
                                     Convert.ToString(segment.pan_per_step*180.0f/(float)Math.PI)});
                lstPathSegments.Items.Add(result);
            }

            // create an image to display the path
            picPath.Image = new Bitmap(sim.results_image_width, sim.results_image_height, 
                                       System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Byte[] path_img = new Byte[sim.results_image_width * sim.results_image_height * 3];
            sim.ShowPath(path_img, sim.results_image_width, sim.results_image_height);
            updatebitmap_unsafe(path_img, (Bitmap)(picPath.Image));

        }

        /// <summary>
        /// displays the occupancy grid
        /// </summary>
        private void showOccupancyGrid()
        {
            picGridMap.Image = new Bitmap(sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells,
                                          System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Byte[] grid_img = new Byte[sim.rob.LocalGrid.dimension_cells * sim.rob.LocalGrid.dimension_cells * 3];
            sim.ShowGrid(grid_img, sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells);
            updatebitmap_unsafe(grid_img, (Bitmap)(picGridMap.Image));
        }

        /// <summary>
        /// show the next pose
        /// </summary>
        private void showNextPose()
        {
            if (sim.path.current_pose != null)
            {
                txtX.Text = Convert.ToString(sim.path.current_pose.x);
                txtY.Text = Convert.ToString(sim.path.current_pose.y);
                txtHeading.Text = Convert.ToString(sim.path.current_pose.pan * 180.0f / (float)Math.PI);
            }
        }

        private void cmdAdd_Click(object sender, EventArgs e)
        {
            if (txtX.Text != "")
            {
                if (txtY.Text != "")
                {
                    if (txtHeading.Text != "")
                    {
                        if (txtNoOfSteps.Text != "")
                        {
                            if (txtDistPerStep.Text != "")
                            {
                                if (txtHeadingChangePerStep.Text != "")
                                {
                                    // add the new path segment
                                    sim.Add(Convert.ToSingle(txtX.Text), Convert.ToSingle(txtY.Text),
                                            Convert.ToSingle(txtHeading.Text)*(float)Math.PI/180.0f, Convert.ToInt32(txtNoOfSteps.Text),
                                            Convert.ToSingle(txtDistPerStep.Text),
                                            Convert.ToSingle(txtHeadingChangePerStep.Text) * (float)Math.PI / 180.0f);

                                    // show the next pose in the path segment sequence
                                    showNextPose();

                                    // clear the entry boxes
                                    txtNoOfSteps.Text = "";
                                    txtDistPerStep.Text = "";
                                    txtHeadingChangePerStep.Text = "";

                                    // update the list view
                                    showPathSegments();
                                }
                            }
                        }
                    }
                }
            }
        }


        private void cmdRemovePathSegment_Click(object sender, EventArgs e)
        {
            if (lstPathSegments.SelectedIndices.Count > 0)
            {
                sim.RemoveSegment(lstPathSegments.SelectedIndices[0]);
                showPathSegments();
                showNextPose();
            }
        }


        /// <summary>
        /// retrieves a set of stereo images for the given time step
        /// filenames should be in the format "camera_0_left_2.jpg" where
        /// the first number is the index of the stereo camera and the second 
        /// number is the time step upon which the image was taken
        /// </summary>
        /// <param name="time_step"></param>
        /// <returns></returns>
        private ArrayList getStereoImages(int time_step)
        {
            ArrayList images = new ArrayList();

            // get the file names for the left and right images for this time step
            String[] left_file = Directory.GetFiles(sim.ImagesPath, "*left_" + Convert.ToString(time_step+1) + ".jpg");
            String[] right_file = Directory.GetFiles(sim.ImagesPath, "*right_" + Convert.ToString(time_step+1) + ".jpg");
            for (int i = 0; i < left_file.Length; i++)
            {
                // load the images as bitmap objects
                Bitmap left_image = new Bitmap(left_file[i]);
                Bitmap right_image = new Bitmap(right_file[i]);

                // extract the raw byte arrays
                Byte[] left_bmp = updatebitmap_unsafe(left_image);
                Byte[] right_bmp = updatebitmap_unsafe(right_image);

                // put the byte arrays into the list of results
                images.Add(left_bmp);
                images.Add(right_bmp);

                // show the current images
                // this is a good way of checking that updatebitmap returned a valid byte array
                picLeftImage.Image = new Bitmap(left_image.Width, left_image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                picRightImage.Image = new Bitmap(right_image.Width, right_image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                updatebitmap_unsafe(left_bmp, (Bitmap)(picLeftImage.Image));
                updatebitmap_unsafe(right_bmp, (Bitmap)(picRightImage.Image));
            }

            return (images);
        }

        /// <summary>
        /// reset the simulation
        /// </summary>
        private void Simulation_Reset()
        {
            sim.Reset();
        }

        // run the simulation one step forwards
        private void Simulation_RunOneStep()
        {
            ArrayList images = getStereoImages(sim.current_time_step);
            sim.RunOneStep(images);

            // show the grid
            showOccupancyGrid();

            // show the benchmarks
            ArrayList benchmarks = sim.GetBenchmarks();
            lstBenchmarks.View = View.List;
            lstBenchmarks.Items.Clear();
            for (int i = 0; i < benchmarks.Count; i++)
                lstBenchmarks.Items.Add((String)benchmarks[i]);
        }

        private void cmdRunOneStep_Click(object sender, EventArgs e)
        {
            Simulation_RunOneStep();
        }

        private void cmdReset_Click(object sender, EventArgs e)
        {
            Simulation_Reset();
        }

    }
}