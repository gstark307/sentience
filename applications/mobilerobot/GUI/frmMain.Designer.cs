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
            this.disparityMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSentienceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recordImagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optomiseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.qualityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.superHighQualityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullResolutionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.timUpdate = new System.Windows.Forms.Timer(this.components);
            this.tabSentience = new System.Windows.Forms.TabControl();
            this.tabPageCamera = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.lstCameraDevices = new System.Windows.Forms.ListBox();
            this.txtCameraDeviceName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.picOutput = new System.Windows.Forms.PictureBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            this.tabSentience.SuspendLayout();
            this.tabPageCamera.SuspendLayout();
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
            this.disparityMapToolStripMenuItem});
            this.viewMenuItem.Name = "viewMenuItem";
            this.viewMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewMenuItem.Text = "View";
            // 
            // stereoFeaturesToolStripMenuItem
            // 
            this.stereoFeaturesToolStripMenuItem.Checked = true;
            this.stereoFeaturesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.stereoFeaturesToolStripMenuItem.Name = "stereoFeaturesToolStripMenuItem";
            this.stereoFeaturesToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.stereoFeaturesToolStripMenuItem.Text = "Stereo Features";
            this.stereoFeaturesToolStripMenuItem.Click += new System.EventHandler(this.stereoFeaturesToolStripMenuItem_Click);
            // 
            // disparityMapToolStripMenuItem
            // 
            this.disparityMapToolStripMenuItem.Name = "disparityMapToolStripMenuItem";
            this.disparityMapToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.disparityMapToolStripMenuItem.Text = "Disparity Map";
            this.disparityMapToolStripMenuItem.Click += new System.EventHandler(this.disparityMapToolStripMenuItem_Click);
            // 
            // videoToolStripMenuItem
            // 
            this.videoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startSentienceToolStripMenuItem});
            this.videoToolStripMenuItem.Name = "videoToolStripMenuItem";
            this.videoToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.videoToolStripMenuItem.Text = "Video";
            // 
            // startSentienceToolStripMenuItem
            // 
            this.startSentienceToolStripMenuItem.Name = "startSentienceToolStripMenuItem";
            this.startSentienceToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.startSentienceToolStripMenuItem.Text = "Start Sentience";
            this.startSentienceToolStripMenuItem.Click += new System.EventHandler(this.startSentienceToolStripMenuItem_Click);
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
            this.picLeftImage.Size = new System.Drawing.Size(22, 23);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 1;
            this.picLeftImage.TabStop = false;
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(40, 43);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(24, 23);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 2;
            this.picRightImage.TabStop = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "JPEG Files|*.jpg|Bitmap Files|*.bmp";
            this.openFileDialog1.Title = "Open Background Image";
            // 
            // timUpdate
            // 
            this.timUpdate.Enabled = true;
            this.timUpdate.Interval = 500;
            this.timUpdate.Tick += new System.EventHandler(this.timUpdate_Tick);
            // 
            // tabSentience
            // 
            this.tabSentience.Controls.Add(this.tabPageCamera);
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
            // picOutput
            // 
            this.picOutput.Location = new System.Drawing.Point(128, 43);
            this.picOutput.Name = "picOutput";
            this.picOutput.Size = new System.Drawing.Size(194, 183);
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
            this.Text = "Sentience Mobile Robot Application";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            this.tabSentience.ResumeLayout(false);
            this.tabPageCamera.ResumeLayout(false);
            this.tabPageCamera.PerformLayout();
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
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer timUpdate;
        private System.Windows.Forms.ToolStripMenuItem optomiseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem speedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem qualityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem superHighQualityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fullResolutionToolStripMenuItem;
        private System.Windows.Forms.TabControl tabSentience;
        private System.Windows.Forms.TabPage tabPageCamera;
        private System.Windows.Forms.TextBox txtCameraDeviceName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox picOutput;
        private System.Windows.Forms.ListBox lstCameraDevices;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ToolStripMenuItem viewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stereoFeaturesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disparityMapToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openBackgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadCalibrationToolStripMenuItem;
    }
}

