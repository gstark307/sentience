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

        }

        private void LoadSimulation(String filename)
        {
            if (sim.Load(filename))
            {
                txtRobotDefinitionFile.Text = sim.RobotDesignFile;
                txtStereoImagesPath.Text = sim.ImagesPath;
            }
        }

        private void update()
        {
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

    }
}