namespace surveyorstereo
{
    partial class frmStereo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmStereo));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCalibrationFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCalibrationPatternToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateLeftCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateRightCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateCameraAlignmentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableEmbeddedStereoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableLoggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.algorithmToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simpleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.denseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.grpTeleop = new System.Windows.Forms.GroupBox();
            this.cmd160x128 = new System.Windows.Forms.Button();
            this.cmd320x256 = new System.Windows.Forms.Button();
            this.cmd640x512 = new System.Windows.Forms.Button();
            this.cmd1280x1024 = new System.Windows.Forms.Button();
            this.cmdLaserOff = new System.Windows.Forms.Button();
            this.cmdLaserOn = new System.Windows.Forms.Button();
            this.cmdCrash = new System.Windows.Forms.Button();
            this.cmdAvoid = new System.Windows.Forms.Button();
            this.cmdSlow = new System.Windows.Forms.Button();
            this.cmdFast = new System.Windows.Forms.Button();
            this.cmdSpinRight = new System.Windows.Forms.Button();
            this.cmdSpinLeft = new System.Windows.Forms.Button();
            this.cmdBackRight = new System.Windows.Forms.Button();
            this.cmdBack = new System.Windows.Forms.Button();
            this.cmdBackLeft = new System.Windows.Forms.Button();
            this.cmdRight = new System.Windows.Forms.Button();
            this.cmdStop = new System.Windows.Forms.Button();
            this.cmdLeft = new System.Windows.Forms.Button();
            this.cmdForwardRight = new System.Windows.Forms.Button();
            this.cmdForward = new System.Windows.Forms.Button();
            this.cmdForwardLeft = new System.Windows.Forms.Button();
            this.grpLogging = new System.Windows.Forms.GroupBox();
            this.lblReplayState = new System.Windows.Forms.Label();
            this.cmdReplayStop = new System.Windows.Forms.Button();
            this.cmdReplay = new System.Windows.Forms.Button();
            this.lblReplay = new System.Windows.Forms.Label();
            this.lblLogName = new System.Windows.Forms.Label();
            this.txtReplay = new System.Windows.Forms.TextBox();
            this.txtLogging = new System.Windows.Forms.TextBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            this.grpTeleop.SuspendLayout();
            this.grpLogging.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.algorithmToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(610, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveCalibrationFileToolStripMenuItem,
            this.saveCalibrationPatternToolStripMenuItem,
            this.closeToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveCalibrationFileToolStripMenuItem
            // 
            this.saveCalibrationFileToolStripMenuItem.Name = "saveCalibrationFileToolStripMenuItem";
            this.saveCalibrationFileToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.saveCalibrationFileToolStripMenuItem.Text = "Save calibration file";
            this.saveCalibrationFileToolStripMenuItem.Click += new System.EventHandler(this.saveCalibrationFileToolStripMenuItem_Click);
            // 
            // saveCalibrationPatternToolStripMenuItem
            // 
            this.saveCalibrationPatternToolStripMenuItem.Name = "saveCalibrationPatternToolStripMenuItem";
            this.saveCalibrationPatternToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.saveCalibrationPatternToolStripMenuItem.Text = "Save calibration image";
            this.saveCalibrationPatternToolStripMenuItem.Click += new System.EventHandler(this.saveCalibrationPatternToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.calibrateLeftCameraToolStripMenuItem,
            this.calibrateRightCameraToolStripMenuItem,
            this.calibrateCameraAlignmentToolStripMenuItem,
            this.enableEmbeddedStereoToolStripMenuItem,
            this.enableLoggingToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // calibrateLeftCameraToolStripMenuItem
            // 
            this.calibrateLeftCameraToolStripMenuItem.Name = "calibrateLeftCameraToolStripMenuItem";
            this.calibrateLeftCameraToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.calibrateLeftCameraToolStripMenuItem.Text = "Calibrate Left Camera";
            this.calibrateLeftCameraToolStripMenuItem.Click += new System.EventHandler(this.calibrateLeftCameraToolStripMenuItem_Click);
            // 
            // calibrateRightCameraToolStripMenuItem
            // 
            this.calibrateRightCameraToolStripMenuItem.Name = "calibrateRightCameraToolStripMenuItem";
            this.calibrateRightCameraToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.calibrateRightCameraToolStripMenuItem.Text = "Calibrate Right Camera";
            this.calibrateRightCameraToolStripMenuItem.Click += new System.EventHandler(this.calibrateRightCameraToolStripMenuItem_Click);
            // 
            // calibrateCameraAlignmentToolStripMenuItem
            // 
            this.calibrateCameraAlignmentToolStripMenuItem.Name = "calibrateCameraAlignmentToolStripMenuItem";
            this.calibrateCameraAlignmentToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.calibrateCameraAlignmentToolStripMenuItem.Text = "Calibrate Camera alignment";
            this.calibrateCameraAlignmentToolStripMenuItem.Click += new System.EventHandler(this.calibrateCameraAlignmentToolStripMenuItem_Click);
            // 
            // enableEmbeddedStereoToolStripMenuItem
            // 
            this.enableEmbeddedStereoToolStripMenuItem.Name = "enableEmbeddedStereoToolStripMenuItem";
            this.enableEmbeddedStereoToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.enableEmbeddedStereoToolStripMenuItem.Text = "Enable embedded stereo";
            this.enableEmbeddedStereoToolStripMenuItem.Click += new System.EventHandler(this.enableEmbeddedStereoToolStripMenuItem_Click);
            // 
            // enableLoggingToolStripMenuItem
            // 
            this.enableLoggingToolStripMenuItem.Name = "enableLoggingToolStripMenuItem";
            this.enableLoggingToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.enableLoggingToolStripMenuItem.Text = "Enable logging";
            this.enableLoggingToolStripMenuItem.Click += new System.EventHandler(this.enableLoggingToolStripMenuItem_Click);
            // 
            // algorithmToolStripMenuItem
            // 
            this.algorithmToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.simpleToolStripMenuItem,
            this.denseToolStripMenuItem});
            this.algorithmToolStripMenuItem.Name = "algorithmToolStripMenuItem";
            this.algorithmToolStripMenuItem.Size = new System.Drawing.Size(73, 20);
            this.algorithmToolStripMenuItem.Text = "Algorithm";
            // 
            // simpleToolStripMenuItem
            // 
            this.simpleToolStripMenuItem.Name = "simpleToolStripMenuItem";
            this.simpleToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.simpleToolStripMenuItem.Text = "Simple";
            this.simpleToolStripMenuItem.Click += new System.EventHandler(this.simpleToolStripMenuItem_Click);
            // 
            // denseToolStripMenuItem
            // 
            this.denseToolStripMenuItem.Name = "denseToolStripMenuItem";
            this.denseToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.denseToolStripMenuItem.Text = "Dense";
            this.denseToolStripMenuItem.Click += new System.EventHandler(this.denseToolStripMenuItem_Click);
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(12, 27);
            this.picLeftImage.Name = "picLeftImage";
            this.picLeftImage.Size = new System.Drawing.Size(281, 198);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 1;
            this.picLeftImage.TabStop = false;
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(316, 27);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(281, 198);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 2;
            this.picRightImage.TabStop = false;
            // 
            // grpTeleop
            // 
            this.grpTeleop.Controls.Add(this.cmd160x128);
            this.grpTeleop.Controls.Add(this.cmd320x256);
            this.grpTeleop.Controls.Add(this.cmd640x512);
            this.grpTeleop.Controls.Add(this.cmd1280x1024);
            this.grpTeleop.Controls.Add(this.cmdLaserOff);
            this.grpTeleop.Controls.Add(this.cmdLaserOn);
            this.grpTeleop.Controls.Add(this.cmdCrash);
            this.grpTeleop.Controls.Add(this.cmdAvoid);
            this.grpTeleop.Controls.Add(this.cmdSlow);
            this.grpTeleop.Controls.Add(this.cmdFast);
            this.grpTeleop.Controls.Add(this.cmdSpinRight);
            this.grpTeleop.Controls.Add(this.cmdSpinLeft);
            this.grpTeleop.Controls.Add(this.cmdBackRight);
            this.grpTeleop.Controls.Add(this.cmdBack);
            this.grpTeleop.Controls.Add(this.cmdBackLeft);
            this.grpTeleop.Controls.Add(this.cmdRight);
            this.grpTeleop.Controls.Add(this.cmdStop);
            this.grpTeleop.Controls.Add(this.cmdLeft);
            this.grpTeleop.Controls.Add(this.cmdForwardRight);
            this.grpTeleop.Controls.Add(this.cmdForward);
            this.grpTeleop.Controls.Add(this.cmdForwardLeft);
            this.grpTeleop.Location = new System.Drawing.Point(12, 240);
            this.grpTeleop.Name = "grpTeleop";
            this.grpTeleop.Size = new System.Drawing.Size(392, 250);
            this.grpTeleop.TabIndex = 3;
            this.grpTeleop.TabStop = false;
            this.grpTeleop.Text = "Teleoperation";
            // 
            // cmd160x128
            // 
            this.cmd160x128.Image = ((System.Drawing.Image)(resources.GetObject("cmd160x128.Image")));
            this.cmd160x128.Location = new System.Drawing.Point(326, 190);
            this.cmd160x128.Name = "cmd160x128";
            this.cmd160x128.Size = new System.Drawing.Size(58, 51);
            this.cmd160x128.TabIndex = 20;
            this.cmd160x128.UseVisualStyleBackColor = true;
            // 
            // cmd320x256
            // 
            this.cmd320x256.Image = ((System.Drawing.Image)(resources.GetObject("cmd320x256.Image")));
            this.cmd320x256.Location = new System.Drawing.Point(326, 133);
            this.cmd320x256.Name = "cmd320x256";
            this.cmd320x256.Size = new System.Drawing.Size(58, 51);
            this.cmd320x256.TabIndex = 19;
            this.cmd320x256.UseVisualStyleBackColor = true;
            // 
            // cmd640x512
            // 
            this.cmd640x512.Image = ((System.Drawing.Image)(resources.GetObject("cmd640x512.Image")));
            this.cmd640x512.Location = new System.Drawing.Point(326, 76);
            this.cmd640x512.Name = "cmd640x512";
            this.cmd640x512.Size = new System.Drawing.Size(58, 51);
            this.cmd640x512.TabIndex = 18;
            this.cmd640x512.UseVisualStyleBackColor = true;
            // 
            // cmd1280x1024
            // 
            this.cmd1280x1024.Image = ((System.Drawing.Image)(resources.GetObject("cmd1280x1024.Image")));
            this.cmd1280x1024.Location = new System.Drawing.Point(326, 19);
            this.cmd1280x1024.Name = "cmd1280x1024";
            this.cmd1280x1024.Size = new System.Drawing.Size(58, 51);
            this.cmd1280x1024.TabIndex = 17;
            this.cmd1280x1024.UseVisualStyleBackColor = true;
            // 
            // cmdLaserOff
            // 
            this.cmdLaserOff.Image = ((System.Drawing.Image)(resources.GetObject("cmdLaserOff.Image")));
            this.cmdLaserOff.Location = new System.Drawing.Point(262, 133);
            this.cmdLaserOff.Name = "cmdLaserOff";
            this.cmdLaserOff.Size = new System.Drawing.Size(58, 51);
            this.cmdLaserOff.TabIndex = 16;
            this.cmdLaserOff.UseVisualStyleBackColor = true;
            this.cmdLaserOff.Click += new System.EventHandler(this.cmdLaserOff_Click);
            // 
            // cmdLaserOn
            // 
            this.cmdLaserOn.Image = ((System.Drawing.Image)(resources.GetObject("cmdLaserOn.Image")));
            this.cmdLaserOn.Location = new System.Drawing.Point(198, 133);
            this.cmdLaserOn.Name = "cmdLaserOn";
            this.cmdLaserOn.Size = new System.Drawing.Size(58, 51);
            this.cmdLaserOn.TabIndex = 15;
            this.cmdLaserOn.UseVisualStyleBackColor = true;
            this.cmdLaserOn.Click += new System.EventHandler(this.cmdLaserOn_Click);
            // 
            // cmdCrash
            // 
            this.cmdCrash.Image = ((System.Drawing.Image)(resources.GetObject("cmdCrash.Image")));
            this.cmdCrash.Location = new System.Drawing.Point(262, 76);
            this.cmdCrash.Name = "cmdCrash";
            this.cmdCrash.Size = new System.Drawing.Size(58, 51);
            this.cmdCrash.TabIndex = 14;
            this.cmdCrash.UseVisualStyleBackColor = true;
            this.cmdCrash.Click += new System.EventHandler(this.cmdCrash_Click);
            // 
            // cmdAvoid
            // 
            this.cmdAvoid.Image = ((System.Drawing.Image)(resources.GetObject("cmdAvoid.Image")));
            this.cmdAvoid.Location = new System.Drawing.Point(198, 76);
            this.cmdAvoid.Name = "cmdAvoid";
            this.cmdAvoid.Size = new System.Drawing.Size(58, 51);
            this.cmdAvoid.TabIndex = 13;
            this.cmdAvoid.UseVisualStyleBackColor = true;
            this.cmdAvoid.Click += new System.EventHandler(this.cmdAvoid_Click);
            // 
            // cmdSlow
            // 
            this.cmdSlow.Image = ((System.Drawing.Image)(resources.GetObject("cmdSlow.Image")));
            this.cmdSlow.Location = new System.Drawing.Point(262, 19);
            this.cmdSlow.Name = "cmdSlow";
            this.cmdSlow.Size = new System.Drawing.Size(58, 51);
            this.cmdSlow.TabIndex = 12;
            this.cmdSlow.UseVisualStyleBackColor = true;
            this.cmdSlow.Click += new System.EventHandler(this.cmdSlow_Click);
            // 
            // cmdFast
            // 
            this.cmdFast.Image = ((System.Drawing.Image)(resources.GetObject("cmdFast.Image")));
            this.cmdFast.Location = new System.Drawing.Point(198, 19);
            this.cmdFast.Name = "cmdFast";
            this.cmdFast.Size = new System.Drawing.Size(58, 51);
            this.cmdFast.TabIndex = 11;
            this.cmdFast.UseVisualStyleBackColor = true;
            this.cmdFast.Click += new System.EventHandler(this.cmdFast_Click);
            // 
            // cmdSpinRight
            // 
            this.cmdSpinRight.Image = ((System.Drawing.Image)(resources.GetObject("cmdSpinRight.Image")));
            this.cmdSpinRight.Location = new System.Drawing.Point(134, 190);
            this.cmdSpinRight.Name = "cmdSpinRight";
            this.cmdSpinRight.Size = new System.Drawing.Size(58, 51);
            this.cmdSpinRight.TabIndex = 10;
            this.cmdSpinRight.UseVisualStyleBackColor = true;
            this.cmdSpinRight.Click += new System.EventHandler(this.cmdSpinRight_Click);
            // 
            // cmdSpinLeft
            // 
            this.cmdSpinLeft.Image = ((System.Drawing.Image)(resources.GetObject("cmdSpinLeft.Image")));
            this.cmdSpinLeft.Location = new System.Drawing.Point(6, 190);
            this.cmdSpinLeft.Name = "cmdSpinLeft";
            this.cmdSpinLeft.Size = new System.Drawing.Size(58, 51);
            this.cmdSpinLeft.TabIndex = 9;
            this.cmdSpinLeft.UseVisualStyleBackColor = true;
            this.cmdSpinLeft.Click += new System.EventHandler(this.cmdSpinLeft_Click);
            // 
            // cmdBackRight
            // 
            this.cmdBackRight.Image = ((System.Drawing.Image)(resources.GetObject("cmdBackRight.Image")));
            this.cmdBackRight.Location = new System.Drawing.Point(134, 133);
            this.cmdBackRight.Name = "cmdBackRight";
            this.cmdBackRight.Size = new System.Drawing.Size(58, 51);
            this.cmdBackRight.TabIndex = 8;
            this.cmdBackRight.UseVisualStyleBackColor = true;
            this.cmdBackRight.Click += new System.EventHandler(this.cmdBackRight_Click);
            // 
            // cmdBack
            // 
            this.cmdBack.Image = ((System.Drawing.Image)(resources.GetObject("cmdBack.Image")));
            this.cmdBack.Location = new System.Drawing.Point(70, 133);
            this.cmdBack.Name = "cmdBack";
            this.cmdBack.Size = new System.Drawing.Size(58, 51);
            this.cmdBack.TabIndex = 7;
            this.cmdBack.UseVisualStyleBackColor = true;
            this.cmdBack.Click += new System.EventHandler(this.cmdBack_Click);
            // 
            // cmdBackLeft
            // 
            this.cmdBackLeft.Image = ((System.Drawing.Image)(resources.GetObject("cmdBackLeft.Image")));
            this.cmdBackLeft.Location = new System.Drawing.Point(6, 133);
            this.cmdBackLeft.Name = "cmdBackLeft";
            this.cmdBackLeft.Size = new System.Drawing.Size(58, 51);
            this.cmdBackLeft.TabIndex = 6;
            this.cmdBackLeft.UseVisualStyleBackColor = true;
            this.cmdBackLeft.Click += new System.EventHandler(this.cmdBackLeft_Click);
            // 
            // cmdRight
            // 
            this.cmdRight.Image = ((System.Drawing.Image)(resources.GetObject("cmdRight.Image")));
            this.cmdRight.Location = new System.Drawing.Point(134, 76);
            this.cmdRight.Name = "cmdRight";
            this.cmdRight.Size = new System.Drawing.Size(58, 51);
            this.cmdRight.TabIndex = 5;
            this.cmdRight.UseVisualStyleBackColor = true;
            this.cmdRight.Click += new System.EventHandler(this.cmdRight_Click);
            // 
            // cmdStop
            // 
            this.cmdStop.Image = ((System.Drawing.Image)(resources.GetObject("cmdStop.Image")));
            this.cmdStop.Location = new System.Drawing.Point(70, 76);
            this.cmdStop.Name = "cmdStop";
            this.cmdStop.Size = new System.Drawing.Size(58, 51);
            this.cmdStop.TabIndex = 4;
            this.cmdStop.UseVisualStyleBackColor = true;
            this.cmdStop.Click += new System.EventHandler(this.cmdStop_Click);
            // 
            // cmdLeft
            // 
            this.cmdLeft.Image = ((System.Drawing.Image)(resources.GetObject("cmdLeft.Image")));
            this.cmdLeft.Location = new System.Drawing.Point(6, 76);
            this.cmdLeft.Name = "cmdLeft";
            this.cmdLeft.Size = new System.Drawing.Size(58, 51);
            this.cmdLeft.TabIndex = 3;
            this.cmdLeft.UseVisualStyleBackColor = true;
            this.cmdLeft.Click += new System.EventHandler(this.cmdLeft_Click);
            // 
            // cmdForwardRight
            // 
            this.cmdForwardRight.Image = ((System.Drawing.Image)(resources.GetObject("cmdForwardRight.Image")));
            this.cmdForwardRight.Location = new System.Drawing.Point(134, 19);
            this.cmdForwardRight.Name = "cmdForwardRight";
            this.cmdForwardRight.Size = new System.Drawing.Size(58, 51);
            this.cmdForwardRight.TabIndex = 2;
            this.cmdForwardRight.UseVisualStyleBackColor = true;
            this.cmdForwardRight.Click += new System.EventHandler(this.cmdForwardRight_Click);
            // 
            // cmdForward
            // 
            this.cmdForward.Image = ((System.Drawing.Image)(resources.GetObject("cmdForward.Image")));
            this.cmdForward.Location = new System.Drawing.Point(70, 19);
            this.cmdForward.Name = "cmdForward";
            this.cmdForward.Size = new System.Drawing.Size(58, 51);
            this.cmdForward.TabIndex = 1;
            this.cmdForward.UseVisualStyleBackColor = true;
            this.cmdForward.Click += new System.EventHandler(this.cmdForward_Click);
            // 
            // cmdForwardLeft
            // 
            this.cmdForwardLeft.Image = ((System.Drawing.Image)(resources.GetObject("cmdForwardLeft.Image")));
            this.cmdForwardLeft.Location = new System.Drawing.Point(6, 19);
            this.cmdForwardLeft.Name = "cmdForwardLeft";
            this.cmdForwardLeft.Size = new System.Drawing.Size(58, 51);
            this.cmdForwardLeft.TabIndex = 0;
            this.cmdForwardLeft.UseVisualStyleBackColor = true;
            this.cmdForwardLeft.Click += new System.EventHandler(this.cmdForwardLeft_Click);
            // 
            // grpLogging
            // 
            this.grpLogging.Controls.Add(this.lblReplayState);
            this.grpLogging.Controls.Add(this.cmdReplayStop);
            this.grpLogging.Controls.Add(this.cmdReplay);
            this.grpLogging.Controls.Add(this.lblReplay);
            this.grpLogging.Controls.Add(this.lblLogName);
            this.grpLogging.Controls.Add(this.txtReplay);
            this.grpLogging.Controls.Add(this.txtLogging);
            this.grpLogging.Location = new System.Drawing.Point(412, 247);
            this.grpLogging.Name = "grpLogging";
            this.grpLogging.Size = new System.Drawing.Size(184, 145);
            this.grpLogging.TabIndex = 4;
            this.grpLogging.TabStop = false;
            // 
            // lblReplayState
            // 
            this.lblReplayState.AutoSize = true;
            this.lblReplayState.Location = new System.Drawing.Point(7, 126);
            this.lblReplayState.Name = "lblReplayState";
            this.lblReplayState.Size = new System.Drawing.Size(10, 13);
            this.lblReplayState.TabIndex = 6;
            this.lblReplayState.Text = " ";
            // 
            // cmdReplayStop
            // 
            this.cmdReplayStop.Location = new System.Drawing.Point(116, 124);
            this.cmdReplayStop.Name = "cmdReplayStop";
            this.cmdReplayStop.Size = new System.Drawing.Size(62, 21);
            this.cmdReplayStop.TabIndex = 5;
            this.cmdReplayStop.Text = "Stop";
            this.cmdReplayStop.UseVisualStyleBackColor = true;
            this.cmdReplayStop.Click += new System.EventHandler(this.cmdReplayStop_Click);
            // 
            // cmdReplay
            // 
            this.cmdReplay.Location = new System.Drawing.Point(116, 99);
            this.cmdReplay.Name = "cmdReplay";
            this.cmdReplay.Size = new System.Drawing.Size(62, 21);
            this.cmdReplay.TabIndex = 4;
            this.cmdReplay.Text = "Replay";
            this.cmdReplay.UseVisualStyleBackColor = true;
            this.cmdReplay.Click += new System.EventHandler(this.cmdReplay_Click);
            // 
            // lblReplay
            // 
            this.lblReplay.AutoSize = true;
            this.lblReplay.Location = new System.Drawing.Point(7, 84);
            this.lblReplay.Name = "lblReplay";
            this.lblReplay.Size = new System.Drawing.Size(69, 13);
            this.lblReplay.TabIndex = 3;
            this.lblReplay.Text = "Replay name";
            // 
            // lblLogName
            // 
            this.lblLogName.AutoSize = true;
            this.lblLogName.Location = new System.Drawing.Point(7, 21);
            this.lblLogName.Name = "lblLogName";
            this.lblLogName.Size = new System.Drawing.Size(54, 13);
            this.lblLogName.TabIndex = 2;
            this.lblLogName.Text = "Log name";
            // 
            // txtReplay
            // 
            this.txtReplay.Location = new System.Drawing.Point(6, 100);
            this.txtReplay.Name = "txtReplay";
            this.txtReplay.Size = new System.Drawing.Size(104, 20);
            this.txtReplay.TabIndex = 1;
            // 
            // txtLogging
            // 
            this.txtLogging.Location = new System.Drawing.Point(6, 43);
            this.txtLogging.Name = "txtLogging";
            this.txtLogging.Size = new System.Drawing.Size(104, 20);
            this.txtLogging.TabIndex = 0;
            // 
            // frmStereo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(610, 497);
            this.Controls.Add(this.grpLogging);
            this.Controls.Add(this.grpTeleop);
            this.Controls.Add(this.picRightImage);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmStereo";
            this.Text = "Surveyor Stereo Vision Test";
            this.SizeChanged += new System.EventHandler(this.frmStereo_SizeChanged);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmStereo_FormClosing);
            this.ResizeEnd += new System.EventHandler(this.frmStereo_ResizeEnd);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            this.grpTeleop.ResumeLayout(false);
            this.grpLogging.ResumeLayout(false);
            this.grpLogging.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.PictureBox picLeftImage;
        private System.Windows.Forms.PictureBox picRightImage;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calibrateLeftCameraToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calibrateRightCameraToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calibrateCameraAlignmentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem algorithmToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem simpleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem denseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveCalibrationPatternToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveCalibrationFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableEmbeddedStereoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableLoggingToolStripMenuItem;
        private System.Windows.Forms.GroupBox grpTeleop;
        private System.Windows.Forms.Button cmdForwardRight;
        private System.Windows.Forms.Button cmdForward;
        private System.Windows.Forms.Button cmdForwardLeft;
        private System.Windows.Forms.Button cmdBack;
        private System.Windows.Forms.Button cmdBackLeft;
        private System.Windows.Forms.Button cmdRight;
        private System.Windows.Forms.Button cmdStop;
        private System.Windows.Forms.Button cmdLeft;
        private System.Windows.Forms.Button cmdSpinRight;
        private System.Windows.Forms.Button cmdSpinLeft;
        private System.Windows.Forms.Button cmdBackRight;
        private System.Windows.Forms.Button cmd640x512;
        private System.Windows.Forms.Button cmd1280x1024;
        private System.Windows.Forms.Button cmdLaserOff;
        private System.Windows.Forms.Button cmdLaserOn;
        private System.Windows.Forms.Button cmdCrash;
        private System.Windows.Forms.Button cmdAvoid;
        private System.Windows.Forms.Button cmdSlow;
        private System.Windows.Forms.Button cmdFast;
        private System.Windows.Forms.Button cmd160x128;
        private System.Windows.Forms.Button cmd320x256;
        private System.Windows.Forms.GroupBox grpLogging;
        private System.Windows.Forms.Label lblReplay;
        private System.Windows.Forms.Label lblLogName;
        private System.Windows.Forms.TextBox txtReplay;
        private System.Windows.Forms.TextBox txtLogging;
        private System.Windows.Forms.Button cmdReplay;
        private System.Windows.Forms.Button cmdReplayStop;
        private System.Windows.Forms.Label lblReplayState;
    }
}

