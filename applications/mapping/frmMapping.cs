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
using sentience.learn;

namespace StereoMapping
{
    public partial class frmMapping : common
    {
        bool simulation_running = false;
        bool optimiser_running = false;
        bool busy = false;

        // default path for loading and saving files
        String defaultPath = System.Windows.Forms.Application.StartupPath + "\\";

        // robot simulation object
        simulation sim;

        public frmMapping()
        {
            InitializeComponent();
            init();
        }

        #region "autotuner"

        // object used for auto tuning
        selfopt autotuner;

        /// <summary>
        /// initialises the autotuner
        /// </summary>
        private void initAutotuner()
        {
            const int NO_OF_TUNING_PARAMETERS = 6;

            autotuner = new selfopt(50, NO_OF_TUNING_PARAMETERS);

            autotuner.smallerScoresAreBetter = true;
            autotuner.parameterName[0] = "Motion model culling threshold";
            autotuner.setParameterRange(0, 60, 90);
            autotuner.setParameterStepSize(0, 1);

            autotuner.parameterName[1] = "Localisation radius";
            autotuner.setParameterRange(1, sim.rob.LocalGridCellSize_mm * 1, sim.rob.LocalGridCellSize_mm * 3);
            autotuner.setParameterStepSize(1, sim.rob.LocalGridCellSize_mm);

            autotuner.parameterName[2] = "Number of position uncertainty particles";
            autotuner.setParameterRange(2, 100, 200);
            autotuner.setParameterStepSize(2, 1);

            autotuner.parameterName[3] = "Vacancy weighting";
            autotuner.setParameterRange(3, 0.8f, 5.0f);
            autotuner.setParameterStepSize(3, 0.05f);

            autotuner.parameterName[4] = "Surround radius percent";
            autotuner.setParameterRange(4, 1.0f, 3.0f);
            autotuner.setParameterStepSize(4, 0.05f);

            autotuner.parameterName[5] = "Matching threshold";
            autotuner.setParameterRange(5, 5.0f, 20.0f);
            autotuner.setParameterStepSize(5, 0.1f);

            autotuner.Randomize(false);
        }

        /// <summary>
        /// load up the next set of test parameters for evaluation
        /// </summary>
        private void nextAutotunerInstance()
        {
            String parameters = "";

            for (int i = 0; i < autotuner.parameters_per_instance; i++)
            {
                parameters += Convert.ToString(autotuner.getParameter(i));
                if (i < autotuner.parameters_per_instance - 1)
                    parameters += ",";
            }
            sim.SetTuningParameters(parameters);
        }

        #endregion

        private void init()
        {
            // create the simulation object
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
                txtTuningParameters.Text = sim.GetTuningParameters();
                txtBestScore.Text = Convert.ToString(sim.rob.MinimumPositionError_mm);
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
            sim.ShowGrid(occupancygridMultiHypothesis.VIEW_ABOVE, grid_img, sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells, true);
            updatebitmap_unsafe(grid_img, (Bitmap)(picGridMap.Image));
        }

        private void showSideViews()
        {
            picGridSideViewLeft.Image = new Bitmap(sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells,
                                               System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Byte[] grid_img = new Byte[sim.rob.LocalGrid.dimension_cells * sim.rob.LocalGrid.dimension_cells * 3];
            sim.ShowGrid(occupancygridMultiHypothesis.VIEW_LEFT_SIDE, grid_img, sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells, true);
            updatebitmap_unsafe(grid_img, (Bitmap)(picGridSideViewLeft.Image));

            picGridSideViewRight.Image = new Bitmap(sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells,
                                               System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            sim.ShowGrid(occupancygridMultiHypothesis.VIEW_RIGHT_SIDE, grid_img, sim.rob.LocalGrid.dimension_cells, sim.rob.LocalGrid.dimension_cells, true);
            updatebitmap_unsafe(grid_img, (Bitmap)(picGridSideViewRight.Image));
        }


        /// <summary>
        /// show the current best pose
        /// </summary>
        private void showBestPose()
        {
            int dimension_mm = sim.rob.LocalGrid.dimension_cells;
            picBestPose.Image = new Bitmap(dimension_mm, dimension_mm,
                                           System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Byte[] best_pose_img = new Byte[dimension_mm * dimension_mm * 3];
            sim.ShowBestPose(best_pose_img, dimension_mm, dimension_mm, true);
            updatebitmap_unsafe(best_pose_img, (Bitmap)(picBestPose.Image));
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
            busy = true;

            int prev_time_step = sim.current_time_step;

            ArrayList images = getStereoImages(sim.current_time_step);
            sim.RunOneStep(images);

            // show the grid
            showOccupancyGrid();

            // show the best pose
            showBestPose();

            // show the benchmarks
            ArrayList benchmarks = sim.GetBenchmarks();
            lstBenchmarks.View = View.List;
            lstBenchmarks.Items.Clear();
            for (int i = 0; i < benchmarks.Count; i++)
                lstBenchmarks.Items.Add((String)benchmarks[i]);

            // save the images, so that they may be used to produce an animation
            if (!optimiser_running)
                picGridMap.Image.Save("Simulation_step_" + Convert.ToString(sim.current_time_step) + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            if (prev_time_step == sim.current_time_step)
            {
                if (optimiser_running)
                {
                    // get the score for this simulation run
                    float score = sim.position_error_mm;

                    // show the position error
                    txtPositionError.Text = Convert.ToString(sim.position_error_mm);

                    // show the average colour variance
                    txtMeanColourVariance.Text = Convert.ToString((int)(sim.GetMeanColourVariance() * 1000000) / 1000000.0f);

                    // set the score for this run
                    autotuner.setScore(score);

                    // if this is the best score save the result
                    if ((score == autotuner.best_score) &&
                        (score < sim.rob.MinimumPositionError_mm))
                    {
                        showSideViews();

                        // set the minimum position error
                        sim.rob.MinimumPositionError_mm = score;

                        // save the robot design file with these tuning parameters
                        if (txtRobotDefinitionFile.Text != "")
                            sim.rob.Save(txtRobotDefinitionFile.Text);

                        // display the parameters
                        txtTuningParameters.Text = sim.GetTuningParameters();

                        // show the best score
                        txtBestScore.Text = Convert.ToString((int)(autotuner.best_score * 1000000) / 1000000.0f);
                    }

                    // show the score graph
                    //picGridSideView.Image = new Bitmap(640, 200, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    //Byte[] score_img = new Byte[640 * 200 * 3];
                    //autotuner.showHistory(score_img, 640, 200);
                    //updatebitmap_unsafe(score_img, (Bitmap)(picGridSideView.Image));

                    // load the next instance
                    nextAutotunerInstance();
                }
                else
                {
                    simulation_running = false;
                    StopSimulation();
                    showSideViews();
                }

                // reset the simulation
                Simulation_Reset();
            }

            busy = false;
        }

        private void cmdRunOneStep_Click(object sender, EventArgs e)
        {
            Simulation_RunOneStep();
        }

        private void cmdReset_Click(object sender, EventArgs e)
        {
            Simulation_Reset();
        }

        private void cmdRunSimulation_Click(object sender, EventArgs e)
        {
            if (txtTuningParameters.Text != "")
                sim.SetTuningParameters(txtTuningParameters.Text);

            cmdOptimise.Enabled = false;
            cmdRunOneStep.Enabled = false;
            cmdReset.Enabled = false;
            cmdRunSimulation.Enabled = false;

            simulation_running = true;
            timSimulation.Enabled = true;
        }

        private void timSimulation_Tick(object sender, EventArgs e)
        {
            if ((simulation_running) && (!busy))
            {
                Simulation_RunOneStep();
            }
        }

        private void StopSimulation()
        {
            cmdReset.Enabled = true;
            cmdRunOneStep.Enabled = true;
            cmdRunSimulation.Enabled = true;
            cmdOptimise.Enabled = true;

            simulation_running = false;
            optimiser_running = false;
            timSimulation.Enabled = false;
        }

        private void cmdStopSimulation_Click(object sender, EventArgs e)
        {
            StopSimulation();
        }

        private void cmdOptimise_Click(object sender, EventArgs e)
        {
            if (autotuner == null)            
            {
                // initialise the autotuner
                initAutotuner();
            }

            // if parameters exist seed the autotuner accordingly
            if (txtTuningParameters.Text != "")
                autotuner.seed(txtTuningParameters.Text, 0.1f);

            cmdReset.Enabled = false;
            cmdRunOneStep.Enabled = false;
            cmdRunSimulation.Enabled = false;
            cmdOptimise.Enabled = false;

            Simulation_Reset();
            nextAutotunerInstance();
            optimiser_running = true;
            simulation_running = true;
            timSimulation.Enabled = true;
        }

    }
}