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
using sentience.core;

namespace robotDesigner
{
    public partial class frmMain : Form
    {
        // default path for loading and saving files
        String defaultPath = System.Windows.Forms.Application.StartupPath + "\\";

        // robot object
        robot rob = new robot(1, robot.MAPPING_DPSLAM);

        // whether the grid cell size has changed
        bool cellSizeChanged = false;

        private int current_stereo_camera_index = 0;

        public frmMain()
        {
            InitializeComponent();
            LoadRobot(defaultPath + "robotdesign.xml");
        }

        /// <summary>
        /// load camera calibration data
        /// </summary>
        /// <param name="filename"></param>
        public void LoadCalibration(String filename)
        {
            rob.head.calibration[current_stereo_camera_index].Load(filename);
        }

        /// <summary>
        /// load sensor models from file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadSensorModels(String filename)
        {
            rob.head.sensormodel[current_stereo_camera_index].Load(filename);
        }

        /// <summary>
        /// load a robot design file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadRobot(String filename)
        {            
            if (rob.Load(filename))
            {
                motionModel motion_model = rob.GetBestMotionModel();

                txtName.Text = rob.Name;
                txtTotalMass.Text = Convert.ToString(rob.TotalMass_kg);
                txtNoOfThreads.Text = Convert.ToString(rob.GetMappingThreads());
                txtBodyWidth.Text = Convert.ToString(rob.BodyWidth_mm);
                txtBodyLength.Text = Convert.ToString(rob.BodyLength_mm);
                txtBodyHeight.Text = Convert.ToString(rob.BodyHeight_mm);
                cmbBodyShape.SelectedIndex = Convert.ToInt32(rob.BodyShape);
                cmbPropulsion.SelectedIndex = rob.propulsionType;
                txtWheelBase.Text = Convert.ToString(rob.WheelBase_mm);
                txtWheelBaseForward.Text = Convert.ToString(rob.WheelBaseForward_mm);
                txtWheelDiameter.Text = Convert.ToString(rob.WheelDiameter_mm);
                cmbWheelFeedback.SelectedIndex = rob.WheelPositionFeedbackType;
                txtGearRatio.Text = Convert.ToString(rob.GearRatio);
                txtCountsPerRev.Text = Convert.ToString(rob.CountsPerRev);
                txtMotorNoLoadSpeedRPM.Text = Convert.ToString(rob.MotorNoLoadSpeedRPM);
                txtMotorTorque.Text = Convert.ToString(rob.MotorTorqueKgMm);
                txtCameraBaseline.Text = Convert.ToString(rob.head.calibration[0].baseline);
                txtCameraFOV.Text = Convert.ToString(rob.head.calibration[0].leftcam.camera_FOV_degrees);
                txtRollAngle.Text = Convert.ToString(rob.head.calibration[0].positionOrientation.roll * 180.0f / (float)Math.PI);
                cmbHeadType.SelectedIndex = rob.HeadType;
                txtHeadSize.Text = Convert.ToString(rob.HeadSize_mm);
                cmbHeadShape.SelectedIndex = rob.HeadShape;
                txtHeadHeightFromGround.Text = Convert.ToString(rob.head.z);
                txtHeadPositionLeft.Text = Convert.ToString(rob.head.x);
                txtHeadPositionForward.Text = Convert.ToString(rob.head.y);
                txtNoOfCameras.Text = Convert.ToString(rob.head.no_of_stereo_cameras);
                cmbCameraOrientation.SelectedIndex = rob.CameraOrientation;

                txtGridLevels.Text = Convert.ToString(rob.LocalGridLevels);
                txtGridWidth.Text = Convert.ToString(rob.LocalGridDimension * rob.LocalGridCellSize_mm);
                txtGridHeight.Text = Convert.ToString(rob.LocalGridDimensionVertical * rob.LocalGridCellSize_mm);
                txtGridCellDimension.Text = Convert.ToString(rob.LocalGridCellSize_mm);
                txtGridInterval.Text = Convert.ToString(rob.LocalGridInterval_mm);
                txtMappingRange.Text = Convert.ToString(rob.LocalGridMappingRange_mm);
                txtLocalGridLocalisationRadius.Text = Convert.ToString(rob.LocalGridLocalisationRadius_mm);
                txtTrialPoses.Text = Convert.ToString(motion_model.survey_trial_poses);
                txtCullingThreshold.Text = Convert.ToString(motion_model.cull_threshold);
                chkEnableScanMatching.Checked = rob.EnableScanMatching;

                updateSensorModelStatus();
            }
        }

        public void update()
        {
            int no_of_cameras = Convert.ToInt32(txtNoOfCameras.Text);            
            robot new_rob = new robot(no_of_cameras, robot.MAPPING_DPSLAM);

            for (int i = 0; i < rob.head.no_of_stereo_cameras; i++)
            {
                new_rob.head.calibration[i] = rob.head.calibration[i];
                new_rob.head.sensormodel[i] = rob.head.sensormodel[i];
            }
            rob = new_rob;

            rob.Name = txtName.Text;
            rob.TotalMass_kg = Convert.ToSingle(txtTotalMass.Text);
            rob.SetMappingThreads(Convert.ToInt32(txtNoOfThreads.Text));
            rob.BodyWidth_mm = Convert.ToSingle(txtBodyWidth.Text);
            rob.BodyLength_mm = Convert.ToSingle(txtBodyLength.Text);
            rob.BodyHeight_mm = Convert.ToSingle(txtBodyHeight.Text);
            rob.BodyShape = cmbBodyShape.SelectedIndex;
            rob.propulsionType = Convert.ToInt32(cmbPropulsion.SelectedIndex);
            rob.WheelBase_mm = Convert.ToSingle(txtWheelBase.Text);
            rob.WheelBaseForward_mm = Convert.ToSingle(txtWheelBaseForward.Text);
            rob.WheelDiameter_mm = Convert.ToSingle(txtWheelDiameter.Text);
            rob.WheelPositionFeedbackType = Convert.ToInt32(cmbWheelFeedback.SelectedIndex);
            rob.GearRatio = Convert.ToInt32(txtGearRatio.Text);
            rob.CountsPerRev = Convert.ToInt32(txtCountsPerRev.Text);
            rob.MotorNoLoadSpeedRPM = Convert.ToSingle(txtMotorNoLoadSpeedRPM.Text);
            rob.MotorTorqueKgMm = Convert.ToSingle(txtMotorTorque.Text);
            for (int i = 0; i < rob.head.no_of_stereo_cameras; i++)
            {
                rob.head.calibration[i].baseline = Convert.ToSingle(txtCameraBaseline.Text);
                rob.head.calibration[i].leftcam.camera_FOV_degrees = Convert.ToSingle(txtCameraFOV.Text);
                rob.head.calibration[i].rightcam.camera_FOV_degrees = Convert.ToSingle(txtCameraFOV.Text);
                rob.head.calibration[i].positionOrientation.roll = Convert.ToSingle(txtRollAngle.Text) * (float)Math.PI / 180.0f;
            }
            rob.HeadType = Convert.ToInt32(cmbHeadType.SelectedIndex);
            rob.HeadSize_mm = Convert.ToSingle(txtHeadSize.Text);
            rob.HeadShape = Convert.ToInt32(cmbHeadShape.SelectedIndex);
            rob.head.x = Convert.ToSingle(txtHeadPositionLeft.Text);
            rob.head.y = Convert.ToSingle(txtHeadPositionForward.Text);
            rob.head.z = Convert.ToSingle(txtHeadHeightFromGround.Text);
            rob.head.no_of_stereo_cameras = Convert.ToInt32(txtNoOfCameras.Text);
            rob.CameraOrientation = Convert.ToInt32(cmbCameraOrientation.SelectedIndex);

            rob.LocalGridCellSize_mm = Convert.ToSingle(txtGridCellDimension.Text);
            rob.LocalGridLevels = Convert.ToInt32(txtGridLevels.Text);
            rob.LocalGridDimension = (int)(Convert.ToInt32(txtGridWidth.Text) / rob.LocalGridCellSize_mm);
            rob.LocalGridDimensionVertical = (int)(Convert.ToInt32(txtGridHeight.Text) / rob.LocalGridCellSize_mm);
            rob.LocalGridInterval_mm = Convert.ToSingle(txtGridInterval.Text);
            rob.LocalGridMappingRange_mm = Convert.ToSingle(txtMappingRange.Text);
            rob.LocalGridLocalisationRadius_mm = Convert.ToSingle(txtLocalGridLocalisationRadius.Text);
            rob.SetMotionModelTrialPoses(Convert.ToInt32(txtTrialPoses.Text));
            rob.SetMotionModelCullingThreshold(Convert.ToInt32(txtCullingThreshold.Text));
            rob.EnableScanMatching = chkEnableScanMatching.Checked;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmbPropulsion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPropulsion.SelectedIndex > 1)
                grpPropulsion.Visible = false;
            else
                grpPropulsion.Visible = true;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {           
            if (rob.head.sensormodel[0] != null)
            {
                update();

                saveFileDialog1.DefaultExt = "xml";
                saveFileDialog1.FileName = rob.Name + ".xml";
                saveFileDialog1.Filter = "Xml files|*.xml";
                saveFileDialog1.Title = "Save robot design file";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    rob.Save(saveFileDialog1.FileName);
            }
            else MessageBox.Show("Please update the sensor models before saving");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Load robot design file";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadRobot(openFileDialog1.FileName);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            update();
            rob.Save(defaultPath + "robotdesign.xml");
        }

        private void importCalibrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Load camera calibration data";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadCalibration(openFileDialog1.FileName);

                // sensor models may need to be recalculated
                // for the new cell size
                rob.head.sensormodel[current_stereo_camera_index] = null;
                updateSensorModelStatus();
            }
        }

        private void importSensorModelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.DefaultExt = "xml";
            openFileDialog1.FileName = ".xml";
            openFileDialog1.Filter = "Xml files|*.xml";
            openFileDialog1.Title = "Load sensor model data";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadSensorModels(openFileDialog1.FileName);
            }
        }

        private void updateSensorModelStatus()
        {
            if (rob.head.sensormodel[0] == null)
                txtSensorModelsStatus.Text = "No sensor models have been generated.  Click below to make some.";
            else
                txtSensorModelsStatus.Text = "Click below to regenerate the sensor models.";
        }

        private void cmdGenerateSensorModels_Click(object sender, EventArgs e)
        {
            update();
            txtSensorModelsStatus.Text = "Please wait whilst sensor models are being generated.  This may take several minutes.";
            cmdGenerateSensorModels.Enabled = false;
            rob.inverseSensorModel.createLookupTables(rob.head, Convert.ToInt32(txtGridCellDimension.Text));
            updateSensorModelStatus();
            MessageBox.Show("Sensor models have been updated");
            cmdGenerateSensorModels.Enabled = true;
        }

        private void txtGridDimension_TextChanged(object sender, EventArgs e)
        {
            cellSizeChanged = true;
        }

        private void txtGridCellDimension_KeyPress(object sender, KeyPressEventArgs e)
        {
            cellSizeChanged = true;
            if (e.KeyChar == 13)
            {
                updateGridWidth();
                updateGridHeight();
                // sensor models may need to be recalculated
                // for the new cell size
                rob.head.sensormodel[0] = null;
                updateSensorModelStatus();
                checkLocalisationRadius();
            }

        }

        private void txtGridCellDimension_Leave(object sender, EventArgs e)
        {
            if (cellSizeChanged)
            {
                updateGridWidth();
                updateGridHeight();
                // sensor models may need to be recalculated
                // for the new cell size
                rob.head.sensormodel[0] = null;
                updateSensorModelStatus();
                cellSizeChanged = false;
                checkLocalisationRadius();
            }
        }

        private void checkLocalisationRadius()
        {
            // don't allow the localisation radius to be smaller than the grid cell size
            float locRadius = Convert.ToSingle(txtLocalGridLocalisationRadius.Text);
            float dimension = Convert.ToSingle(txtGridCellDimension.Text);
            if (locRadius < dimension)
            {
                locRadius = dimension;
                txtLocalGridLocalisationRadius.Text = Convert.ToString(dimension);
            }
        }

        private void txtLocalGridLocalisationRadius_Leave(object sender, EventArgs e)
        {
            checkLocalisationRadius();
        }

        private void txtCullingThreshold_Leave(object sender, EventArgs e)
        {
            int cull_threshold = Convert.ToInt32(txtCullingThreshold.Text);
            if (cull_threshold < 10) cull_threshold = 10;
            if (cull_threshold > 90) cull_threshold = 90;
            txtCullingThreshold.Text = Convert.ToString(cull_threshold);
        }

        private void updateGridWidth()
        {
            if (txtGridWidth.Text != "")
            {
                int dimension = Convert.ToInt32(txtGridWidth.Text) / Convert.ToInt32(txtGridCellDimension.Text);
                dimension = (int)(dimension / 8) * 8; // this just ensures that the grid will display properly
                txtGridWidth.Text = Convert.ToString(dimension * Convert.ToInt32(txtGridCellDimension.Text));
            }
        }

        private void updateGridHeight()
        {
            if (txtGridHeight.Text != "")
            {
                int dimension = Convert.ToInt32(txtGridHeight.Text) / Convert.ToInt32(txtGridCellDimension.Text);
                dimension = (int)(dimension / 8) * 8; // this just ensures that the grid will display properly
                txtGridHeight.Text = Convert.ToString(dimension * Convert.ToInt32(txtGridCellDimension.Text));
                rob.LocalGridDimensionVertical = dimension;
            }
        }

        private void txtGridWidth_Leave(object sender, EventArgs e)
        {
            updateGridWidth();
        }

        private void txtGridWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                updateGridWidth();
        }

        private void txtGridHeight_Leave(object sender, EventArgs e)
        {
            updateGridHeight();
        }

        private void txtGridHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                updateGridHeight();
        }
    }
}