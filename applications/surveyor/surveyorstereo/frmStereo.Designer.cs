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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateLeftCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateRightCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateCameraAlignmentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.algorithmToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simpleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.denseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.saveCalibrationPatternToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCalibrationFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
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
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.calibrateLeftCameraToolStripMenuItem,
            this.calibrateRightCameraToolStripMenuItem,
            this.calibrateCameraAlignmentToolStripMenuItem,
            this.recordToolStripMenuItem});
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
            // recordToolStripMenuItem
            // 
            this.recordToolStripMenuItem.Name = "recordToolStripMenuItem";
            this.recordToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.recordToolStripMenuItem.Text = "Record";
            this.recordToolStripMenuItem.Click += new System.EventHandler(this.recordToolStripMenuItem_Click);
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
            // saveCalibrationPatternToolStripMenuItem
            // 
            this.saveCalibrationPatternToolStripMenuItem.Name = "saveCalibrationPatternToolStripMenuItem";
            this.saveCalibrationPatternToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.saveCalibrationPatternToolStripMenuItem.Text = "Save calibration image";
            this.saveCalibrationPatternToolStripMenuItem.Click += new System.EventHandler(this.saveCalibrationPatternToolStripMenuItem_Click);
            // 
            // saveCalibrationFileToolStripMenuItem
            // 
            this.saveCalibrationFileToolStripMenuItem.Name = "saveCalibrationFileToolStripMenuItem";
            this.saveCalibrationFileToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.saveCalibrationFileToolStripMenuItem.Text = "Save calibration file";
            this.saveCalibrationFileToolStripMenuItem.Click += new System.EventHandler(this.saveCalibrationFileToolStripMenuItem_Click);
            // 
            // frmStereo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(610, 243);
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
        private System.Windows.Forms.ToolStripMenuItem recordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem algorithmToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem simpleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem denseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveCalibrationPatternToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveCalibrationFileToolStripMenuItem;
    }
}

