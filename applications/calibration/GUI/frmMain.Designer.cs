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
            this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.videoToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.startCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.txtDistToCentre = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtCameraHeight = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtSpacingFactor = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtFOV = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPatternSpacing = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbDisplayType = new System.Windows.Forms.ComboBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            this.grpParameters.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem1,
            this.videoToolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(863, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem1
            // 
            this.fileToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem1});
            this.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
            this.fileToolStripMenuItem1.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem1.Text = "File";
            // 
            // exitToolStripMenuItem1
            // 
            this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
            this.exitToolStripMenuItem1.Size = new System.Drawing.Size(103, 22);
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
            this.picOutput1.Size = new System.Drawing.Size(733, 333);
            this.picOutput1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOutput1.TabIndex = 3;
            this.picOutput1.TabStop = false;
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
            this.grpParameters.Controls.Add(this.txtDistToCentre);
            this.grpParameters.Controls.Add(this.label5);
            this.grpParameters.Controls.Add(this.txtCameraHeight);
            this.grpParameters.Controls.Add(this.label4);
            this.grpParameters.Controls.Add(this.txtSpacingFactor);
            this.grpParameters.Controls.Add(this.label3);
            this.grpParameters.Controls.Add(this.txtFOV);
            this.grpParameters.Controls.Add(this.label2);
            this.grpParameters.Controls.Add(this.txtPatternSpacing);
            this.grpParameters.Controls.Add(this.label1);
            this.grpParameters.Controls.Add(this.cmbDisplayType);
            this.grpParameters.Location = new System.Drawing.Point(12, 27);
            this.grpParameters.Name = "grpParameters";
            this.grpParameters.Size = new System.Drawing.Size(839, 45);
            this.grpParameters.TabIndex = 4;
            this.grpParameters.TabStop = false;
            this.grpParameters.Text = "Parameters";
            this.grpParameters.Visible = false;
            // 
            // txtDistToCentre
            // 
            this.txtDistToCentre.Location = new System.Drawing.Point(800, 18);
            this.txtDistToCentre.Name = "txtDistToCentre";
            this.txtDistToCentre.Size = new System.Drawing.Size(33, 20);
            this.txtDistToCentre.TabIndex = 10;
            this.txtDistToCentre.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtDistToCentre.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtDistToCentre_KeyPress);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(705, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Dist To centre mm";
            // 
            // txtCameraHeight
            // 
            this.txtCameraHeight.Location = new System.Drawing.Point(664, 17);
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
            this.label4.Location = new System.Drawing.Point(570, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Camera Height mm";
            // 
            // txtSpacingFactor
            // 
            this.txtSpacingFactor.Location = new System.Drawing.Point(527, 17);
            this.txtSpacingFactor.Name = "txtSpacingFactor";
            this.txtSpacingFactor.Size = new System.Drawing.Size(33, 20);
            this.txtSpacingFactor.TabIndex = 6;
            this.txtSpacingFactor.Text = "15";
            this.txtSpacingFactor.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtSpacingFactor.Leave += new System.EventHandler(this.txtSpacingFactor_Leave);
            this.txtSpacingFactor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSpacingFactor_KeyPress);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(442, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(79, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Spacing Factor";
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
            "Best fit curve"});
            this.cmbDisplayType.Location = new System.Drawing.Point(15, 18);
            this.cmbDisplayType.Name = "cmbDisplayType";
            this.cmbDisplayType.Size = new System.Drawing.Size(128, 21);
            this.cmbDisplayType.TabIndex = 0;
            this.cmbDisplayType.Text = "Corner points";
            this.cmbDisplayType.SelectedIndexChanged += new System.EventHandler(this.cmbDisplayType_SelectedIndexChanged);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(863, 457);
            this.Controls.Add(this.grpParameters);
            this.Controls.Add(this.picOutput1);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Sluggish Software:  Camera Calibration";
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            this.grpParameters.ResumeLayout(false);
            this.grpParameters.PerformLayout();
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
        private System.Windows.Forms.TextBox txtSpacingFactor;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtCameraHeight;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtDistToCentre;
        private System.Windows.Forms.Label label5;
    }
}

