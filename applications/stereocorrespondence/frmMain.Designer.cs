namespace stereocorrespondence
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.grpControls = new System.Windows.Forms.GroupBox();
            this.cmdNext = new System.Windows.Forms.Button();
            this.cmdPrevious = new System.Windows.Forms.Button();
            this.cmdBrowse = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtImagesDirectory = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.picDepthMap = new System.Windows.Forms.PictureBox();
            this.txtCalibrationFilename = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmdRobotDefinitionBrowse = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip1.SuspendLayout();
            this.grpControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDepthMap)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(876, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // grpControls
            // 
            this.grpControls.Controls.Add(this.txtCalibrationFilename);
            this.grpControls.Controls.Add(this.label2);
            this.grpControls.Controls.Add(this.cmdRobotDefinitionBrowse);
            this.grpControls.Controls.Add(this.cmdNext);
            this.grpControls.Controls.Add(this.cmdPrevious);
            this.grpControls.Controls.Add(this.cmdBrowse);
            this.grpControls.Controls.Add(this.label1);
            this.grpControls.Controls.Add(this.txtImagesDirectory);
            this.grpControls.Location = new System.Drawing.Point(12, 36);
            this.grpControls.Name = "grpControls";
            this.grpControls.Size = new System.Drawing.Size(852, 92);
            this.grpControls.TabIndex = 1;
            this.grpControls.TabStop = false;
            // 
            // cmdNext
            // 
            this.cmdNext.Location = new System.Drawing.Point(779, 18);
            this.cmdNext.Name = "cmdNext";
            this.cmdNext.Size = new System.Drawing.Size(67, 24);
            this.cmdNext.TabIndex = 4;
            this.cmdNext.Text = "Next";
            this.cmdNext.UseVisualStyleBackColor = true;
            this.cmdNext.Click += new System.EventHandler(this.cmdNext_Click);
            // 
            // cmdPrevious
            // 
            this.cmdPrevious.Location = new System.Drawing.Point(710, 18);
            this.cmdPrevious.Name = "cmdPrevious";
            this.cmdPrevious.Size = new System.Drawing.Size(67, 24);
            this.cmdPrevious.TabIndex = 3;
            this.cmdPrevious.Text = "Previous";
            this.cmdPrevious.UseVisualStyleBackColor = true;
            this.cmdPrevious.Click += new System.EventHandler(this.cmdPrevious_Click);
            // 
            // cmdBrowse
            // 
            this.cmdBrowse.Location = new System.Drawing.Point(609, 19);
            this.cmdBrowse.Name = "cmdBrowse";
            this.cmdBrowse.Size = new System.Drawing.Size(61, 24);
            this.cmdBrowse.TabIndex = 2;
            this.cmdBrowse.Text = "Browse";
            this.cmdBrowse.UseVisualStyleBackColor = true;
            this.cmdBrowse.Click += new System.EventHandler(this.cmdBrowse_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Images directory";
            // 
            // txtImagesDirectory
            // 
            this.txtImagesDirectory.Location = new System.Drawing.Point(156, 22);
            this.txtImagesDirectory.Name = "txtImagesDirectory";
            this.txtImagesDirectory.Size = new System.Drawing.Size(447, 20);
            this.txtImagesDirectory.TabIndex = 0;
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(12, 134);
            this.picLeftImage.Name = "picLeftImage";
            this.picLeftImage.Size = new System.Drawing.Size(423, 378);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 2;
            this.picLeftImage.TabStop = false;
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(441, 134);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(423, 378);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 3;
            this.picRightImage.TabStop = false;
            // 
            // picDepthMap
            // 
            this.picDepthMap.Location = new System.Drawing.Point(228, 518);
            this.picDepthMap.Name = "picDepthMap";
            this.picDepthMap.Size = new System.Drawing.Size(423, 378);
            this.picDepthMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picDepthMap.TabIndex = 4;
            this.picDepthMap.TabStop = false;
            // 
            // txtCalibrationFilename
            // 
            this.txtCalibrationFilename.Location = new System.Drawing.Point(156, 49);
            this.txtCalibrationFilename.Name = "txtCalibrationFilename";
            this.txtCalibrationFilename.Size = new System.Drawing.Size(447, 20);
            this.txtCalibrationFilename.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(140, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Calibration/Robot design file";
            // 
            // cmdRobotDefinitionBrowse
            // 
            this.cmdRobotDefinitionBrowse.Location = new System.Drawing.Point(609, 48);
            this.cmdRobotDefinitionBrowse.Name = "cmdRobotDefinitionBrowse";
            this.cmdRobotDefinitionBrowse.Size = new System.Drawing.Size(61, 21);
            this.cmdRobotDefinitionBrowse.TabIndex = 10;
            this.cmdRobotDefinitionBrowse.Text = "Browse";
            this.cmdRobotDefinitionBrowse.UseVisualStyleBackColor = true;
            this.cmdRobotDefinitionBrowse.Click += new System.EventHandler(this.cmdRobotDefinitionBrowse_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(876, 917);
            this.Controls.Add(this.picDepthMap);
            this.Controls.Add(this.picRightImage);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.grpControls);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Stereo Correspondence";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.grpControls.ResumeLayout(false);
            this.grpControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDepthMap)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.GroupBox grpControls;
        private System.Windows.Forms.Button cmdBrowse;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtImagesDirectory;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button cmdNext;
        private System.Windows.Forms.Button cmdPrevious;
        private System.Windows.Forms.PictureBox picLeftImage;
        private System.Windows.Forms.PictureBox picRightImage;
        private System.Windows.Forms.PictureBox picDepthMap;
        private System.Windows.Forms.TextBox txtCalibrationFilename;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button cmdRobotDefinitionBrowse;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

