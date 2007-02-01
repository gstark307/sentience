/*
    Sentience 3D Perception System: Grid viewer
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Gosub;
using sentience;
using sentience.core;

namespace DirectXSample
{
	public partial class frmGridViewer : Form
	{
        float pan = 0, tilt = 0, viewing_distance = 220;

        String grid_type = "914";
        int current_grid_level = 0;
        occupancygrid currentGrid = null;
        String grid_filename;         //file name of the grid
        bool grid_loaded = true;      //whether the grid has been loaded
        int rendered = 0;
		
        AutoMesh mCellMesh;
		Color32	mBackgroundColor = Color.DarkBlue;

	
		public frmGridViewer()
		{
			InitializeComponent();
		}


        /// <summary>
        /// view a grid from the given file
        /// </summary>
        /// <param name="d3d"></param>
        /// <param name="dx"></param>
        /// <param name="filename">grid filename</param>
        /// <param name="grid_level">the level of the grid within the grid to be viewed</param>
        /// <param name="min_evidence">minimum evidence threshold</param>
        public void viewOccupancyGrid(Gosub.Direct3d d3d, Microsoft.DirectX.Direct3D.Device dx,
                            String filename, int grid_level, int min_evidence)
        {
            if (!grid_loaded)
            {
                currentGrid = new occupancygrid(64, 100);
                switch(grid_type)
                {
                    case "914":
                        {
                            currentGrid.Load(filename);
                            break;
                        }
                }
                grid_loaded = true;
            }
            if (currentGrid != null)
            {
                if (grid_type=="914")
                    viewGrid(d3d, dx, currentGrid);
            }
        }

        public void viewGrid(Gosub.Direct3d d3d, Microsoft.DirectX.Direct3D.Device dx,
                            occupancygrid grid)
        {
            bool ground_plane_drawn = false;
            mCellMesh = new AutoMesh(d3d, Mesh.Box(dx, 1, 1, 1));

            //show the grid cells
            for (int z = grid.dimension-1; z >= 0 ; z--)
            {
                int plane_hits = 0;
                for (int x = 1; x < grid.dimension - 1; x++)
                {
                    for (int y = 1; y < grid.dimension-1; y++)
                    {
                        if (grid.display_cell[x, y, z])
                        {
                            plane_hits++;
                            occupancyGridCell c = grid.cell[x, y, z];
                            int r = 0;
                            int g = 0;
                            int b = 255;
                            if (c != null)
                            {
                                r = c.colour[0];
                                g = c.colour[1];
                                b = c.colour[2];
                            }

                            dx.Transform.World = Matrix.Translation(-grid.dimension / 2, -grid.dimension / 2, -grid.dimension / 2) //Center model
                                //* Matrix.Scaling(1, 1, 1) // Make it bigger
                                //* Matrix.RotationYawPitchRoll(0, 0, 0)
                                                 * Matrix.Translation(grid.dimension - 1 - x, y, z)
                                                 * Matrix.RotationYawPitchRoll(0, tilt, 0); // Then move it where you want

                            dx.Material = GraphicsUtility.InitMaterial(Color.FromArgb(r, g, b));

                            mCellMesh.M.DrawSubset(0);
                        }


                        //for (int z = 0; z < grid.dimension; z++)
                        {
                            if (z == grid.dimension / 2)
                            {
                                if ((x == 1) || (x == grid.dimension - 2) || (y == 1) || (y == grid.dimension - 2))
                                {
                                    dx.Transform.World = Matrix.Translation(-grid.dimension / 2, -grid.dimension / 2, -grid.dimension / 2) //Center model
                                                         * Matrix.Translation(x, y, z)
                                                         * Matrix.RotationYawPitchRoll(0, tilt, 0); // Then move it where you want

                                    dx.Material = GraphicsUtility.InitMaterial(Color.Green);
                                    mCellMesh.M.DrawSubset(0);
                                }
                            }
                        }
                    }

                }

                if ((plane_hits > 30) && (!ground_plane_drawn))
                {
                    ground_plane_drawn = true;
                    for (int x = 1; x < grid.dimension - 1; x++)
                    {
                        for (int y = 1; y < grid.dimension - 1; y++)
                        {
                            if (grid.empty[x, y])
                            {
                                //occupancyGridCell c = grid.cell[x, y, z];
                                int r = 0;
                                int g = 0;
                                int b = 255;

                                dx.Transform.World = Matrix.Translation(-grid.dimension / 2, -grid.dimension / 2, -grid.dimension / 2) //Center model
                                    //* Matrix.Scaling(1, 1, 1) // Make it bigger
                                    //* Matrix.RotationYawPitchRoll(0, 0, 0)
                                                     * Matrix.Translation(grid.dimension - 1 - x, y, z)
                                                     * Matrix.RotationYawPitchRoll(0, tilt, 0); // Then move it where you want

                                dx.Material = GraphicsUtility.InitMaterial(Color.FromArgb(r, g, b));

                                mCellMesh.M.DrawSubset(0);
                            }
                        }
                    }
                }


            }

        }


		/// <summary>
		/// Occurs once after DirectX has been initialized for the first time.  
		/// Setup AutoMesh's, AutoVertexBuffer's, and AutoTexture's here.
		/// </summary>
		private void mD3d_DxLoaded(Gosub.Direct3d d3d, Microsoft.DirectX.Direct3D.Device dx)
		{
		}

		// More accurate than Environment.TickCount for smoother motion
		[System.Security.SuppressUnmanagedCodeSecurity]
		[DllImport("winmm.dll")]
		private static extern int timeGetTime();

		
		/// <summary>
		/// Occurs when it is time to render 3d objects.  Place all 3d
		/// drawing code in this event.
		/// </summary>
		private void mD3d_DxRender3d(Gosub.Direct3d d3d, Microsoft.DirectX.Direct3D.Device dx)
		{
            //if (rendered < 5)
            {
                // Setup the lights
                dx.Lights[0].Enabled = true;
                dx.Lights[0].Type = LightType.Directional;
                dx.Lights[0].Direction = new Vector3(0, 0, 1);
                dx.Lights[0].Diffuse = Color.White;
                dx.Lights[0].Position = new Vector3(0, 0, 0);
                dx.RenderState.Lighting = true;

                // Set viewer		
                dx.Transform.View = Matrix.LookAtLH(new Vector3(0, 0, -viewing_distance),
                    new Vector3(0.0f, 0.0f, 0.0f), new Vector3(pan, 1.0f, tilt));

                // Set projection matrix
                dx.Transform.Projection = Matrix.PerspectiveFovLH(
                    (float)Math.PI / 4, 640f / 480f, 50.0f, 2000.0f);
                dx.RenderState.NormalizeNormals = true;

                rendered++;
                viewOccupancyGrid(d3d, dx, grid_filename, current_grid_level, 0);
            }
		}

		/// <summary>
		/// Allow user to toggle full screen
		/// </summary>
		private void SampleForm_KeyDown(object sender, KeyEventArgs e)
		{
            rendered = 0;

			// Toggle full screen
			if (e.KeyCode == Keys.F1)
				mD3d.DxFullScreen = !mD3d.DxFullScreen;
			
			// Exit full screen
			if (e.KeyCode == Keys.Escape)
				mD3d.DxFullScreen = false;

            if (e.KeyCode == Keys.S)
            {
                viewing_distance -= 20;
                if (viewing_distance < 50) viewing_distance = 50;
            }
            if (e.KeyCode == Keys.D)
            {
                viewing_distance += 20;
                if (viewing_distance > 500) viewing_distance = 500;
            }
            if (e.KeyCode == Keys.X)
            {
                pan -= 3.1415927f / 18;
            }
            if (e.KeyCode == Keys.C)
            {
                pan += 3.1415927f / 18;
            }
            if (e.KeyCode == Keys.A)
            {
                tilt -= 3.1415927f / 18;
            }
            if (e.KeyCode == Keys.Z)
            {
                tilt += 3.1415927f / 18;
            }
            if (e.KeyCode == Keys.O)
            {
                pan = 0;
                tilt = 0; 
                viewing_distance = 220;
            }
        }

		/// <summary>
		/// Ask the control to resize to fit the form (even when the form size changes)
		/// </summary>
		private void buttonAutoSize_Click(object sender, EventArgs e)
		{
			mD3d.DxAutoResize = true;
			mD3d.BringToFront();  // Make sure the control is on top of other stuff
		}

		/// <summary>
		/// Display the screen parameters form
		/// </summary>
		private void buttonScreenParams_Click(object sender, EventArgs e)
		{
			mD3d.DxSelectNewDevice();
		}
		

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void cmdZoomIn_Click(object sender, EventArgs e)
        {
            viewing_distance -= 20;
            if (viewing_distance < 50) viewing_distance = 50;
        }

        private void cmdZoomOut_Click(object sender, EventArgs e)
        {
            viewing_distance += 20;
            if (viewing_distance > 500) viewing_distance = 500;
        }

        private void cmdPanLeft_Click(object sender, EventArgs e)
        {
            pan -= 3.1415927f/18;
        }

        private void cmdPanRight_Click(object sender, EventArgs e)
        {
            pan += 3.1415927f / 18;
        }

        private void gridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Open 914 grid file";
            openFileDialog1.InitialDirectory = "C:\\develop\\sentience\\applications";  // System.AppDomain.CurrentDomain.BaseDirectory;
            //openFileDialog1.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                grid_filename = openFileDialog1.FileName;
                grid_loaded = false;
                grid_type = "914";
            }

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
	
	}
}