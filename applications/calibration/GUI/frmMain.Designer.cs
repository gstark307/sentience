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
            this.fileToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.videoToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.startCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.resetCalibrationDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSentienceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recordImagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.timUpdate = new System.Windows.Forms.Timer(this.components);
            this.picOutput1 = new System.Windows.Forms.PictureBox();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showDirectionOfGravityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.grpParameters = new System.Windows.Forms.GroupBox();
            this.txtBaseline = new System.Windows.Forms.TextBox();
            this.lblBaseline = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbCentreSpotPosition = new System.Windows.Forms.ComboBox();
            this.txtDistToCentre = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtCameraHeight = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtFOV = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPatternSpacing = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbDisplayType = new System.Windows.Forms.ComboBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.picOutput2 = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            this.grpParameters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem1,
            this.videoToolStripMenuItem1,
            this.toolsToolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(863, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem1
            // 
            this.fileToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.exitToolStripMenuItem1});
            this.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
            this.fileToolStripMenuItem1.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem1.Text = "File";
            // 
            // OpenToolStripMenuItem
            // 
            this.OpenToolStripMenuItem.Name = "OpenToolStripMenuItem";
            this.OpenToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.OpenToolStripMenuItem.Text = "Open";
            this.OpenToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem1
            // 
            this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
            this.exitToolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem1.Text = "Exit";
            this.exitToolStripMenuItem1.Click += new System.EventHandler(this.exitToolStripMenuItem1_Click);
            // 
            // videoToolStripMenuItem1
            // 
            this.videoToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startCameraToolStripMenuItem});
            this.videoToolStripMenuItem1.Name = "videoToolStripMenuItem1";
            this.videoToolStripMenuItem1.Size = new System.Drawing.Size(45, 20);
            this.videoToolStripMenuItem1.Text = "Video";
            // 
            // startCameraToolStripMenuItem
            // 
            this.startCameraToolStripMenuItem.Name = "startCameraToolStripMenuItem";
            this.startCameraToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.startCameraToolStripMenuItem.Text = "Start Camera";
            this.startCameraToolStripMenuItem.Click += new System.EventHandler(this.startCameraToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem1
            // 
            this.toolsToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetCalibrationDataToolStripMenuItem});
            this.toolsToolStripMenuItem1.Name = "toolsToolStripMenuItem1";
            this.toolsToolStripMenuItem1.Size = new System.Drawing.Size(44, 20);
            this.toolsToolStripMenuItem1.Text = "Tools";
            // 
            // resetCalibrationDataToolStripMenuItem
            // 
            this.resetCalibrationDataToolStripMenuItem.Name = "resetCalibrationDataToolStripMenuItem";
            this.resetCalibrationDataToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.resetCalibrationDataToolStripMenuItem.Text = "Reset Calibration Data";
            this.resetCalibrationDataToolStripMenuItem.Click += new System.EventHandler(this.resetCalibrationDataToolStripMenuItem_Click);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // videoToolStripMenuItem
            // 
            this.videoToolStripMenuItem.Name = "videoToolStripMenuItem";
            this.videoToolStripMenuItem.Size = new System.Drawing.Size(45, 20);
            this.videoToolStripMenuItem.Text = "Video";
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.startToolStripMenuItem.Text = "Start";
            // 
            // startSentienceToolStripMenuItem
            // 
            this.startSentienceToolStripMenuItem.Name = "startSentienceToolStripMenuItem";
            this.startSentienceToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // recordImagesToolStripMenuItem
            // 
            this.recordImagesToolStripMenuItem.Name = "recordImagesToolStripMenuItem";
            this.recordImagesToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Xml files|*.xml";
            // 
            // timUpdate
            // 
            this.timUpdate.Enabled = true;
            this.timUpdate.Interval = 20;
            this.timUpdate.Tick += new System.EventHandler(this.timUpdate_Tick);
            // 
            // picOutput1
            // 
            this.picOutput1.Location = new System.Drawing.Point(12, 112);
            this.picOutput1.Name = "picOutput1";
            this.picOutput1.Size = new System.Drawing.Size(330, 219);
            this.picOutput1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOutput1.TabIndex = 3;
            this.picOutput1.TabStop = false;
            this.picOutput1.Click += new System.EventHandler(this.picOutput1_Click);
            this.picOutput1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picOutput1_MouseMove);
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(374, 112);
            this.picLeftImage.Name = "picLeftImage";
            this.picLeftImage.Size = new System.Drawing.Size(64, 55);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 1;
            this.picLeftImage.TabStop = false;
            this.picLeftImage.Visible = false;
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // showDirectionOfGravityToolStripMenuItem
            // 
            this.showDirectionOfGravityToolStripMenuItem.Name = "showDirectionOfGravityToolStripMenuItem";
            this.showDirectionOfGravityToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // grpParameters
            // 
            this.grpParameters.Controls.Add(this.txtBaseline);
            this.grpParameters.Controls.Add(this.lblBaseline);
            this.grpParameters.Controls.Add(this.lblStatus);
            this.grpParameters.Controls.Add(this.label6);
            this.grpParameters.Controls.Add(this.cmbCentreSpotPosition);
            this.grpParameters.Controls.Add(this.txtDistToCentre);
            this.grpParameters.Controls.Add(this.label5);
            this.grpParameters.Controls.Add(this.txtCameraHeight);
            this.grpParameters.Controls.Add(this.label4);
            this.grpParameters.Controls.Add(this.txtFOV);
            this.grpParameters.Controls.Add(this.label2);
            this.grpParameters.Controls.Add(this.txtPatternSpacing);
            this.grpParameters.Controls.Add(this.label1);
            this.grpParameters.Controls.Add(this.cmbDisplayType);
            this.grpParameters.Location = new System.Drawing.Point(12, 27);
            this.grpParameters.Name = "grpParameters";
            this.grpParameters.Size = new System.Drawing.Size(839, 69);
            this.grpParameters.TabIndex = 4;
            this.grpParameters.TabStop = false;
            this.grpParameters.Text = "Parameters";
            this.grpParameters.Visible = false;
            // 
            // txtBaseline
            // 
            this.txtBaseline.Location = new System.Drawing.Point(427, 43);
            this.txtBaseline.Name = "txtBaseline";
            this.txtBaseline.Size = new System.Drawing.Size(33, 20);
            this.txtBaseline.TabIndex = 15;
            this.txtBaseline.Text = "100";
            this.txtBaseline.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtBaseline.Leave += new System.EventHandler(this.txtBaseline_Leave);
            this.txtBaseline.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBaseline_KeyPress);
            // 
            // lblBaseline
            // 
            this.lblBaseline.AutoSize = true;
            this.lblBaseline.Location = new System.Drawing.Point(355, 47);
            this.lblBaseline.Name = "lblBaseline";
            this.lblBaseline.Size = new System.Drawing.Size(66, 13);
            this.lblBaseline.TabIndex = 14;
            this.lblBaseline.Text = "Baseline mm";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 46);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(65, 13);
            this.lblStatus.TabIndex = 13;
            this.lblStatus.Text = "Calibrating...";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(149, 46);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Centre spot position";
            // 
            // cmbCentreSpotPosition
            // 
            this.cmbCentreSpotPosition.FormattingEnabled = true;
            this.cmbCentreSpotPosition.Items.AddRange(new object[] {
            "North West",
            "North East",
            "South East",
            "South West"});
            this.cmbCentreSpotPosition.Location = new System.Drawing.Point(257, 43);
            this.cmbCentreSpotPosition.Name = "cmbCentreSpotPosition";
            this.cmbCentreSpotPosition.Size = new System.Drawing.Size(88, 21);
            this.cmbCentreSpotPosition.TabIndex = 11;
            this.cmbCentreSpotPosition.Text = "North West";
            this.cmbCentreSpotPosition.SelectedIndexChanged += new System.EventHandler(this.cmbCentreSpotPosition_SelectedIndexChanged);
            // 
            // txtDistToCentre
            // 
            this.txtDistToCentre.Location = new System.Drawing.Point(679, 18);
            this.txtDistToCentre.Name = "txtDistToCentre";
            this.txtDistToCentre.Size = new System.Drawing.Size(33, 20);
            this.txtDistToCentre.TabIndex = 10;
            this.txtDistToCentre.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtDistToCentre.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtDistToCentre_KeyPress);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(584, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Dist To centre mm";
            // 
            // txtCameraHeight
            // 
            this.txtCameraHeight.Location = new System.Drawing.Point(543, 17);
            this.txtCameraHeight.Name = "txtCameraHeight";
            this.txtCameraHeight.Size = new System.Drawing.Size(33, 20);
            this.txtCameraHeight.TabIndex = 8;
            this.txtCameraHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtCameraHeight.Leave += new System.EventHandler(this.txtCameraHeight_Leave);
            this.txtCameraHeight.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCameraHeight_KeyPress);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(449, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Camera Height mm";
            // 
            // txtFOV
            // 
            this.txtFOV.Location = new System.Drawing.Point(404, 17);
            this.txtFOV.Name = "txtFOV";
            this.txtFOV.Size = new System.Drawing.Size(33, 20);
            this.txtFOV.TabIndex = 4;
            this.txtFOV.Text = "40";
            this.txtFOV.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtFOV.Leave += new System.EventHandler(this.txtFOV_Leave);
            this.txtFOV.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtFOV_KeyPress);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(296, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Field Of View degrees";
            // 
            // txtPatternSpacing
            // 
            this.txtPatternSpacing.Location = new System.Drawing.Point(257, 17);
            this.txtPatternSpacing.Name = "txtPatternSpacing";
            this.txtPatternSpacing.Size = new System.Drawing.Size(33, 20);
            this.txtPatternSpacing.TabIndex = 2;
            this.txtPatternSpacing.Text = "50";
            this.txtPatternSpacing.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtPatternSpacing.Leave += new System.EventHandler(this.txtPatternSpacing_Leave);
            this.txtPatternSpacing.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtPatternSpacing_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(149, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Pattern Spacing mm";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // cmbDisplayType
            // 
            this.cmbDisplayType.FormattingEnabled = true;
            this.cmbDisplayType.Items.AddRange(new object[] {
            "Edge features",
            "Corner points",
            "Lines",
            "Align centre point",
            "Best fit curve",
            "Rectified image"});
            this.cmbDisplayType.Location = new System.Drawing.Point(15, 18);
            this.cmbDisplayType.Name = "cmbDisplayType";
            this.cmbDisplayType.Size = new System.Drawing.Size(128, 21);
            this.cmbDisplayType.TabIndex = 0;
            this.cmbDisplayType.Text = "Corner points";
            this.cmbDisplayType.SelectedIndexChanged += new System.EventHandler(this.cmbDisplayType_SelectedIndexChanged);
            // 
            // picOutput2
            // 
            this.picOutput2.Location = new System.Drawing.Point(358, 190);
            this.picOutput2.Name = "picOutput2";
            this.picOutput2.Size = new System.Drawing.Size(302, 219);
            this.picOutput2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOutput2.TabIndex = 5;
            this.picOutput2.TabStop = false;
            this.picOutput2.Visible = false;
            this.picOutput2.Click += new System.EventHandler(this.picOutput2_Click);
            this.picOutput2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picOutput2_MouseMove);
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(444, 112);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(64, 55);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 6;
            this.picRightImage.TabStop = false;
            this.picRightImage.Visible = false;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(863, 457);
            this.Controls.Add(this.picRightImage);
            this.Controls.Add(this.picOutput2);
            this.Controls.Add(this.grpParameters);
            this.Controls.Add(this.picOutput1);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Sluggish Software:  Camera Calibration";
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            this.grpParameters.ResumeLayout(false);
            this.grpParameters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startSentienceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recordImagesToolStripMenuItem;
        private System.Windows.Forms.PictureBox picLeftImage;
        private System.Windows.Forms.PictureBox picOutput1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer timUpdate;
        private System.Windows.Forms.ToolStripMenuItem videoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showDirectionOfGravityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem videoToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem startCameraToolStripMenuItem;
        private System.Windows.Forms.GroupBox grpParameters;
        private System.Windows.Forms.ComboBox cmbDisplayType;
        private System.Windows.Forms.TextBox txtPatternSpacing;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtFOV;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtCameraHeight;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtDistToCentre;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem resetCalibrationDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OpenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.PictureBox picOutput2;
        private System.Windows.Forms.PictureBox picRightImage;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbCentreSpotPosition;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtBaseline;
        private System.Windows.Forms.Label lblBaseline;
    }
}

