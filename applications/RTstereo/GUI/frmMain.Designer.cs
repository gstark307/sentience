namespace WindowsApplication1
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openBackgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadCalibrationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stereoFeaturesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.roboRadarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disparityMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nearbyObjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.raysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.linesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSentienceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSimulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recordImagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optomiseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.qualityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.superHighQualityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullResolutionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.txtNoOfFeatures = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtStereoTime = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.timUpdate = new System.Windows.Forms.Timer(this.components);
            this.tabSentience = new System.Windows.Forms.TabControl();
            this.tabPageCamera = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.lstCameraDevices = new System.Windows.Forms.ListBox();
            this.txtCameraDeviceName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPagePerformance = new System.Windows.Forms.TabPage();
            this.txtObstacleRange = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tabParameters = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.txtFOV = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtFocalLength = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtBaseline = new System.Windows.Forms.TextBox();
            this.tabPageLocation = new System.Windows.Forms.TabPage();
            this.txtPathName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmdClear = new System.Windows.Forms.Button();
            this.cmdSavePosition = new System.Windows.Forms.Button();
            this.picOutput = new System.Windows.Forms.PictureBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            this.tabSentience.SuspendLayout();
            this.tabPageCamera.SuspendLayout();
            this.tabPagePerformance.SuspendLayout();
            this.tabParameters.SuspendLayout();
            this.tabPageLocation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewMenuItem,
            this.videoToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.optomiseToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(823, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openBackgroundToolStripMenuItem,
            this.loadCalibrationToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openBackgroundToolStripMenuItem
            // 
            this.openBackgroundToolStripMenuItem.Name = "openBackgroundToolStripMenuItem";
            this.openBackgroundToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.openBackgroundToolStripMenuItem.Text = "Open Background";
            this.openBackgroundToolStripMenuItem.Click += new System.EventHandler(this.openBackgroundToolStripMenuItem_Click);
            // 
            // loadCalibrationToolStripMenuItem
            // 
            this.loadCalibrationToolStripMenuItem.Name = "loadCalibrationToolStripMenuItem";
            this.loadCalibrationToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.loadCalibrationToolStripMenuItem.Text = "Load Calibration ";
            this.loadCalibrationToolStripMenuItem.Click += new System.EventHandler(this.loadCalibrationToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // viewMenuItem
            // 
            this.viewMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stereoFeaturesToolStripMenuItem,
            this.roboRadarToolStripMenuItem,
            this.disparityMapToolStripMenuItem,
            this.nearbyObjectsToolStripMenuItem,
            this.raysToolStripMenuItem,
            this.linesToolStripMenuItem});
            this.viewMenuItem.Name = "viewMenuItem";
            this.viewMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewMenuItem.Text = "View";
            // 
            // stereoFeaturesToolStripMenuItem
            // 
            this.stereoFeaturesToolStripMenuItem.Checked = true;
            this.stereoFeaturesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.stereoFeaturesToolStripMenuItem.Name = "stereoFeaturesToolStripMenuItem";
            this.stereoFeaturesToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.stereoFeaturesToolStripMenuItem.Text = "Stereo Features";
            this.stereoFeaturesToolStripMenuItem.Click += new System.EventHandler(this.stereoFeaturesToolStripMenuItem_Click);
            // 
            // roboRadarToolStripMenuItem
            // 
            this.roboRadarToolStripMenuItem.Name = "roboRadarToolStripMenuItem";
            this.roboRadarToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.roboRadarToolStripMenuItem.Text = "Robo Radar";
            this.roboRadarToolStripMenuItem.Click += new System.EventHandler(this.roboRadarToolStripMenuItem_Click);
            // 
            // disparityMapToolStripMenuItem
            // 
            this.disparityMapToolStripMenuItem.Name = "disparityMapToolStripMenuItem";
            this.disparityMapToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.disparityMapToolStripMenuItem.Text = "Disparity Map";
            this.disparityMapToolStripMenuItem.Click += new System.EventHandler(this.disparityMapToolStripMenuItem_Click);
            // 
            // nearbyObjectsToolStripMenuItem
            // 
            this.nearbyObjectsToolStripMenuItem.Name = "nearbyObjectsToolStripMenuItem";
            this.nearbyObjectsToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.nearbyObjectsToolStripMenuItem.Text = "Nearby Objects";
            this.nearbyObjectsToolStripMenuItem.Click += new System.EventHandler(this.nearbyObjectsToolStripMenuItem_Click);
            // 
            // raysToolStripMenuItem
            // 
            this.raysToolStripMenuItem.Name = "raysToolStripMenuItem";
            this.raysToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.raysToolStripMenuItem.Text = "Rays";
            this.raysToolStripMenuItem.Click += new System.EventHandler(this.raysToolStripMenuItem_Click);
            // 
            // linesToolStripMenuItem
            // 
            this.linesToolStripMenuItem.Name = "linesToolStripMenuItem";
            this.linesToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.linesToolStripMenuItem.Text = "Lines";
            this.linesToolStripMenuItem.Click += new System.EventHandler(this.linesToolStripMenuItem_Click);
            // 
            // videoToolStripMenuItem
            // 
            this.videoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startSentienceToolStripMenuItem,
            this.startSimulationToolStripMenuItem});
            this.videoToolStripMenuItem.Name = "videoToolStripMenuItem";
            this.videoToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.videoToolStripMenuItem.Text = "Video";
            // 
            // startSentienceToolStripMenuItem
            // 
            this.startSentienceToolStripMenuItem.Name = "startSentienceToolStripMenuItem";
            this.startSentienceToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.startSentienceToolStripMenuItem.Text = "Start Sentience";
            this.startSentienceToolStripMenuItem.Click += new System.EventHandler(this.startSentienceToolStripMenuItem_Click);
            // 
            // startSimulationToolStripMenuItem
            // 
            this.startSimulationToolStripMenuItem.Name = "startSimulationToolStripMenuItem";
            this.startSimulationToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.startSimulationToolStripMenuItem.Text = "Start Simulation";
            this.startSimulationToolStripMenuItem.Click += new System.EventHandler(this.startSimulationToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.recordImagesToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // recordImagesToolStripMenuItem
            // 
            this.recordImagesToolStripMenuItem.Name = "recordImagesToolStripMenuItem";
            this.recordImagesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.recordImagesToolStripMenuItem.Text = "Record Images";
            this.recordImagesToolStripMenuItem.Click += new System.EventHandler(this.recordImagesToolStripMenuItem_Click);
            // 
            // optomiseToolStripMenuItem
            // 
            this.optomiseToolStripMenuItem.Name = "optomiseToolStripMenuItem";
            this.optomiseToolStripMenuItem.Size = new System.Drawing.Size(12, 20);
            // 
            // speedToolStripMenuItem
            // 
            this.speedToolStripMenuItem.Name = "speedToolStripMenuItem";
            this.speedToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // qualityToolStripMenuItem
            // 
            this.qualityToolStripMenuItem.Name = "qualityToolStripMenuItem";
            this.qualityToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // superHighQualityToolStripMenuItem
            // 
            this.superHighQualityToolStripMenuItem.Name = "superHighQualityToolStripMenuItem";
            this.superHighQualityToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // fullResolutionToolStripMenuItem
            // 
            this.fullResolutionToolStripMenuItem.Name = "fullResolutionToolStripMenuItem";
            this.fullResolutionToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(12, 43);
            this.picLeftImage.Name = "picLeftImage";
            this.picLeftImage.Size = new System.Drawing.Size(194, 172);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 1;
            this.picLeftImage.TabStop = false;
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(231, 43);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(194, 172);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 2;
            this.picRightImage.TabStop = false;
            // 
            // txtNoOfFeatures
            // 
            this.txtNoOfFeatures.Location = new System.Drawing.Point(159, 52);
            this.txtNoOfFeatures.Name = "txtNoOfFeatures";
            this.txtNoOfFeatures.Size = new System.Drawing.Size(41, 20);
            this.txtNoOfFeatures.TabIndex = 23;
            this.txtNoOfFeatures.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(18, 52);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(104, 13);
            this.label10.TabIndex = 22;
            this.label10.Text = "No of features found";
            // 
            // txtStereoTime
            // 
            this.txtStereoTime.Location = new System.Drawing.Point(159, 18);
            this.txtStereoTime.Name = "txtStereoTime";
            this.txtStereoTime.Size = new System.Drawing.Size(41, 20);
            this.txtStereoTime.TabIndex = 1;
            this.txtStereoTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Stereo calculation time (mS)";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "JPEG Files|*.jpg|Bitmap Files|*.bmp";
            this.openFileDialog1.Title = "Open Background Image";
            // 
            // timUpdate
            // 
            this.timUpdate.Enabled = true;
            this.timUpdate.Interval = 50;
            this.timUpdate.Tick += new System.EventHandler(this.timUpdate_Tick);
            // 
            // tabSentience
            // 
            this.tabSentience.Controls.Add(this.tabPageCamera);
            this.tabSentience.Controls.Add(this.tabPagePerformance);
            this.tabSentience.Controls.Add(this.tabParameters);
            this.tabSentience.Controls.Add(this.tabPageLocation);
            this.tabSentience.Location = new System.Drawing.Point(441, 38);
            this.tabSentience.Name = "tabSentience";
            this.tabSentience.SelectedIndex = 0;
            this.tabSentience.Size = new System.Drawing.Size(365, 192);
            this.tabSentience.TabIndex = 6;
            // 
            // tabPageCamera
            // 
            this.tabPageCamera.Controls.Add(this.label3);
            this.tabPageCamera.Controls.Add(this.lstCameraDevices);
            this.tabPageCamera.Controls.Add(this.txtCameraDeviceName);
            this.tabPageCamera.Controls.Add(this.label2);
            this.tabPageCamera.Location = new System.Drawing.Point(4, 22);
            this.tabPageCamera.Name = "tabPageCamera";
            this.tabPageCamera.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageCamera.Size = new System.Drawing.Size(357, 166);
            this.tabPageCamera.TabIndex = 0;
            this.tabPageCamera.Text = "Camera";
            this.tabPageCamera.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Available Devices";
            // 
            // lstCameraDevices
            // 
            this.lstCameraDevices.FormattingEnabled = true;
            this.lstCameraDevices.Location = new System.Drawing.Point(17, 66);
            this.lstCameraDevices.Name = "lstCameraDevices";
            this.lstCameraDevices.Size = new System.Drawing.Size(322, 56);
            this.lstCameraDevices.TabIndex = 5;
            this.lstCameraDevices.Click += new System.EventHandler(this.lstCameraDevices_Click);
            // 
            // txtCameraDeviceName
            // 
            this.txtCameraDeviceName.Location = new System.Drawing.Point(92, 18);
            this.txtCameraDeviceName.Name = "txtCameraDeviceName";
            this.txtCameraDeviceName.Size = new System.Drawing.Size(247, 20);
            this.txtCameraDeviceName.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Device Name";
            // 
            // tabPagePerformance
            // 
            this.tabPagePerformance.Controls.Add(this.txtObstacleRange);
            this.tabPagePerformance.Controls.Add(this.label7);
            this.tabPagePerformance.Controls.Add(this.txtStereoTime);
            this.tabPagePerformance.Controls.Add(this.label1);
            this.tabPagePerformance.Controls.Add(this.txtNoOfFeatures);
            this.tabPagePerformance.Controls.Add(this.label10);
            this.tabPagePerformance.Location = new System.Drawing.Point(4, 22);
            this.tabPagePerformance.Name = "tabPagePerformance";
            this.tabPagePerformance.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePerformance.Size = new System.Drawing.Size(357, 166);
            this.tabPagePerformance.TabIndex = 1;
            this.tabPagePerformance.Text = "Performance";
            this.tabPagePerformance.UseVisualStyleBackColor = true;
            // 
            // txtObstacleRange
            // 
            this.txtObstacleRange.Location = new System.Drawing.Point(159, 88);
            this.txtObstacleRange.Name = "txtObstacleRange";
            this.txtObstacleRange.Size = new System.Drawing.Size(41, 20);
            this.txtObstacleRange.TabIndex = 25;
            this.txtObstacleRange.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(18, 88);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(103, 13);
            this.label7.TabIndex = 24;
            this.label7.Text = "Obstacle Range mm";
            // 
            // tabParameters
            // 
            this.tabParameters.Controls.Add(this.label8);
            this.tabParameters.Controls.Add(this.txtFOV);
            this.tabParameters.Controls.Add(this.label6);
            this.tabParameters.Controls.Add(this.txtFocalLength);
            this.tabParameters.Controls.Add(this.label5);
            this.tabParameters.Controls.Add(this.txtBaseline);
            this.tabParameters.Location = new System.Drawing.Point(4, 22);
            this.tabParameters.Name = "tabParameters";
            this.tabParameters.Size = new System.Drawing.Size(357, 166);
            this.tabParameters.TabIndex = 3;
            this.tabParameters.Text = "Parameters";
            this.tabParameters.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 77);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(113, 13);
            this.label8.TabIndex = 5;
            this.label8.Text = "Field of Vision degrees";
            // 
            // txtFOV
            // 
            this.txtFOV.Location = new System.Drawing.Point(131, 74);
            this.txtFOV.Name = "txtFOV";
            this.txtFOV.Size = new System.Drawing.Size(51, 20);
            this.txtFOV.TabIndex = 4;
            this.txtFOV.Text = "78";
            this.txtFOV.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 51);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "Focal Length mm";
            // 
            // txtFocalLength
            // 
            this.txtFocalLength.Location = new System.Drawing.Point(131, 48);
            this.txtFocalLength.Name = "txtFocalLength";
            this.txtFocalLength.Size = new System.Drawing.Size(51, 20);
            this.txtFocalLength.TabIndex = 2;
            this.txtFocalLength.Text = "0.8";
            this.txtFocalLength.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 25);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(105, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Camera Baseline mm";
            // 
            // txtBaseline
            // 
            this.txtBaseline.Location = new System.Drawing.Point(131, 22);
            this.txtBaseline.Name = "txtBaseline";
            this.txtBaseline.Size = new System.Drawing.Size(51, 20);
            this.txtBaseline.TabIndex = 0;
            this.txtBaseline.Text = "100";
            this.txtBaseline.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tabPageLocation
            // 
            this.tabPageLocation.Controls.Add(this.txtPathName);
            this.tabPageLocation.Controls.Add(this.label4);
            this.tabPageLocation.Controls.Add(this.cmdClear);
            this.tabPageLocation.Controls.Add(this.cmdSavePosition);
            this.tabPageLocation.Location = new System.Drawing.Point(4, 22);
            this.tabPageLocation.Name = "tabPageLocation";
            this.tabPageLocation.Size = new System.Drawing.Size(357, 166);
            this.tabPageLocation.TabIndex = 2;
            this.tabPageLocation.Text = "Location";
            this.tabPageLocation.UseVisualStyleBackColor = true;
            // 
            // txtPathName
            // 
            this.txtPathName.Location = new System.Drawing.Point(113, 13);
            this.txtPathName.Name = "txtPathName";
            this.txtPathName.Size = new System.Drawing.Size(112, 20);
            this.txtPathName.TabIndex = 25;
            this.txtPathName.Text = "test";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 24;
            this.label4.Text = "Path Name";
            // 
            // cmdClear
            // 
            this.cmdClear.Location = new System.Drawing.Point(231, 13);
            this.cmdClear.Name = "cmdClear";
            this.cmdClear.Size = new System.Drawing.Size(106, 27);
            this.cmdClear.TabIndex = 23;
            this.cmdClear.Text = "Clear";
            this.cmdClear.Click += new System.EventHandler(this.cmdClear_Click);
            // 
            // cmdSavePosition
            // 
            this.cmdSavePosition.Location = new System.Drawing.Point(231, 46);
            this.cmdSavePosition.Name = "cmdSavePosition";
            this.cmdSavePosition.Size = new System.Drawing.Size(106, 27);
            this.cmdSavePosition.TabIndex = 22;
            this.cmdSavePosition.Text = "Save";
            this.cmdSavePosition.Click += new System.EventHandler(this.cmdSavePosition_Click);
            // 
            // picOutput
            // 
            this.picOutput.Location = new System.Drawing.Point(12, 236);
            this.picOutput.Name = "picOutput";
            this.picOutput.Size = new System.Drawing.Size(794, 443);
            this.picOutput.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOutput.TabIndex = 7;
            this.picOutput.TabStop = false;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(823, 691);
            this.Controls.Add(this.picOutput);
            this.Controls.Add(this.tabSentience);
            this.Controls.Add(this.picRightImage);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Sentience Demo:  Real time stereo correspondence using webcams";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            this.tabSentience.ResumeLayout(false);
            this.tabPageCamera.ResumeLayout(false);
            this.tabPageCamera.PerformLayout();
            this.tabPagePerformance.ResumeLayout(false);
            this.tabPagePerformance.PerformLayout();
            this.tabParameters.ResumeLayout(false);
            this.tabParameters.PerformLayout();
            this.tabPageLocation.ResumeLayout(false);
            this.tabPageLocation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem videoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startSentienceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recordImagesToolStripMenuItem;
        private System.Windows.Forms.PictureBox picLeftImage;
        private System.Windows.Forms.PictureBox picRightImage;
        private System.Windows.Forms.TextBox txtStereoTime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer timUpdate;
        private System.Windows.Forms.ToolStripMenuItem optomiseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem speedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem qualityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startSimulationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem superHighQualityToolStripMenuItem;
        private System.Windows.Forms.TextBox txtNoOfFeatures;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ToolStripMenuItem fullResolutionToolStripMenuItem;
        private System.Windows.Forms.TabControl tabSentience;
        private System.Windows.Forms.TabPage tabPageCamera;
        private System.Windows.Forms.TabPage tabPagePerformance;
        private System.Windows.Forms.TabPage tabPageLocation;
        private System.Windows.Forms.TextBox txtCameraDeviceName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox picOutput;
        private System.Windows.Forms.ListBox lstCameraDevices;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button cmdClear;
        private System.Windows.Forms.Button cmdSavePosition;
        private System.Windows.Forms.TextBox txtPathName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolStripMenuItem viewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stereoFeaturesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem roboRadarToolStripMenuItem;
        private System.Windows.Forms.TabPage tabParameters;
        private System.Windows.Forms.TextBox txtObstacleRange;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtFocalLength;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtBaseline;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtFOV;
        private System.Windows.Forms.ToolStripMenuItem disparityMapToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nearbyObjectsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openBackgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem raysToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem linesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadCalibrationToolStripMenuItem;
    }
}

