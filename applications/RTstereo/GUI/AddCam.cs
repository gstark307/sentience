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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DirectX.Capture;

namespace WindowsApplication1
{
    public partial class AddCam : Form
    {
        globals global_variables;
        //bool initialised = false;
        public bool LeftImage;


        public AddCam(globals global_variables)
        {
            this.global_variables = global_variables;

            InitializeComponent();

            short i;
            Filter f;

            //what cameras are available?  Populate the combo box
            cboCamaras.Items.Clear();
            for (i = 0; i < global_variables.WDM_filters.VideoInputDevices.Count; i++)
            {
                f = global_variables.WDM_filters.VideoInputDevices[i];
                cboCamaras.Items.Add(f.Name);
            }
        }


        private void Bo_Selecta()
        {
            //String IdVentana;

            if (cboCamaras.SelectedItem == null)
            {
                MessageBox.Show("Select an available camera.", "Error",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                this.Close();
            }

            global_variables.selectCamera(LeftImage, cboCamaras.SelectedIndex);

            this.Dispose();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            if ((cboCamaras.Text.IndexOf("vfw") == -1) && (cboCamaras.Text.IndexOf("Vfw") == -1) && (cboCamaras.Text.IndexOf("VFW") == -1))
            {
                global_variables.CamSettingsLeft.frameRate = "";
                global_variables.CamSettingsLeft.resolution = "";
                Bo_Selecta();
                global_variables.camera_initialised = true;
            }
            else MessageBox.Show("Only WDM drivers may be used");
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
           //this.Dispose();
           this.Close();
        }

        private void AddCam_Load(object sender, EventArgs e)
        {
                global_variables.CamSettingsLeft.leftImage = true;
                global_variables.CamSettingsLeft.Load();
                if (global_variables.CamSettingsLeft.cameraName != "")
                {
                    cboCamaras.Text = global_variables.CamSettingsLeft.cameraName;
                    //if (CamSettingsLeft.firstTime) Bo_Selecta();
                }
        }

    }
}