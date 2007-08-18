/*
    Sentience 3D Perception System: mapping application
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
using System.IO;
using Gtk;

using sentience.core;
using sentience.learn;
using sluggish.utilities;
using sluggish.utilities.timing;
using Aced.Compression;

namespace mapping
{

	public partial class MainWindow: Gtk.Window
	{
	    #region "variables"

	    bool simulation_running = false;
	    bool optimiser_running = false;
	    bool busy = false;

	    // default path for loading and saving files
	    String defaultPath = "/home/motters/develop/sentience/applications/mapping";

	    // robot simulation object
	    simulation sim;
	    
	    #endregion
		
		public MainWindow (): base (Gtk.WindowType.Toplevel)
		{
			Build ();
		}
		
		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			Application.Quit ();
			a.RetVal = true;
		}
		

	    #region "initialisation"
	    
	    private void init()
	    {
	        // create the simulation object
	        sim = new simulation(defaultPath + "robotdesign.xml", defaultPath);
	        LoadSimulation(defaultPath + "simulation.xml");

	/*
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
	        
	        */
	    }


	    #endregion
	    
	    #region "loading and saving"
	    
	    /// <summary>
	    /// load a simulation file
	    /// </summary>
	    private void LoadSimulation(String filename)
	    {
	        if (sim.Load(filename))
	        {
	            txtSimulationName.Buffer.Text = sim.Name;
	            txtRobotDesignFile.Buffer.Text = sim.RobotDesignFile;
	            txtStereoImagesPath.Buffer.Text = sim.ImagesPath;
	            txtTuningParameters.Buffer.Text = sim.GetTuningParameters();
	            //txtBestScore.Text = Convert.ToString(sim.rob.MinimumPositionError_mm);
	            //update();
	            //showPathSegments();
	            //showNextPose();
	        }
	    }

	    #endregion

	    #region "parameter update"

	    /// <summary>
	    /// update the simulation object
	    /// </summary>
	    private void UpdateParameters()
	    {
	        String simulation_name = txtSimulationName.Buffer.Text;
	        String robot_definition_file = txtRobotDesignFile.Buffer.Text;
	        String stereo_images_path = txtStereoImagesPath.Buffer.Text;
	        UpdateParameters(simulation_name, robot_definition_file, stereo_images_path);
	    }

	    
	    /// <summary>
	    /// update the simulation object
	    /// </summary>
	    private void UpdateParameters(String simulation_name,
	                                  String robot_definition_file,
	                                  String stereo_images_path)
	    {
	        sim.Name = simulation_name;
	        sim.RobotDesignFile = robot_definition_file;
	        sim.ImagesPath = stereo_images_path;

	        if (sim.RobotDesignFile != "")
	        {
	            sim.Reset();

	            if (sim.ImagesPath != "")
	                    sim.Save(defaultPath + "simulation.xml");
	        }
	    }

	    #endregion


	    protected virtual void mnuSimulation (object sender, System.EventArgs e)
	    {
	        frmSimulation winsim = new frmSimulation(sim);
			winsim.Show();
	    }

	    protected virtual void OnExit1Activated (object sender, System.EventArgs e)
	    {
	        Application.Quit();
	    }
	}
}
