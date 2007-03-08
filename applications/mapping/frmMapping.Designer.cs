namespace StereoMapping
{
    partial class frmMapping
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
            this.picGrid = new System.Windows.Forms.PictureBox();
            this.timAnimate = new System.Windows.Forms.Timer(this.components);
            this.lblPositionIndex = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.txtRobotDefinitionFile = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmdRobotDefinitionBrowse = new System.Windows.Forms.Button();
            this.cmdStereoImagesPathBrowse = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtStereoImagesPath = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.lstPathSegments = new System.Windows.Forms.ListView();
            this.label3 = new System.Windows.Forms.Label();
            this.txtX = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtY = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtHeading = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtNoOfSteps = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtDistPerStep = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtHeadingChangePerStep = new System.Windows.Forms.TextBox();
            this.grpNewPathSegment = new System.Windows.Forms.GroupBox();
            this.cmdAdd = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picGrid)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.grpNewPathSegment.SuspendLayout();
            this.SuspendLayout();
            // 
            // picGrid
            // 
            this.picGrid.Location = new System.Drawing.Point(485, 111);
            this.picGrid.Name = "picGrid";
            this.picGrid.Size = new System.Drawing.Size(180, 162);
            this.picGrid.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picGrid.TabIndex = 0;
            this.picGrid.TabStop = false;
            // 
            // lblPositionIndex
            // 
            this.lblPositionIndex.AutoSize = true;
            this.lblPositionIndex.Location = new System.Drawing.Point(357, 12);
            this.lblPositionIndex.Name = "lblPositionIndex";
            this.lblPositionIndex.Size = new System.Drawing.Size(0, 13);
            this.lblPositionIndex.TabIndex = 3;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(693, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(133, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // txtRobotDefinitionFile
            // 
            this.txtRobotDefinitionFile.Location = new System.Drawing.Point(117, 41);
            this.txtRobotDefinitionFile.Name = "txtRobotDefinitionFile";
            this.txtRobotDefinitionFile.Size = new System.Drawing.Size(464, 20);
            this.txtRobotDefinitionFile.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Robot design file";
            // 
            // cmdRobotDefinitionBrowse
            // 
            this.cmdRobotDefinitionBrowse.Location = new System.Drawing.Point(587, 40);
            this.cmdRobotDefinitionBrowse.Name = "cmdRobotDefinitionBrowse";
            this.cmdRobotDefinitionBrowse.Size = new System.Drawing.Size(54, 20);
            this.cmdRobotDefinitionBrowse.TabIndex = 7;
            this.cmdRobotDefinitionBrowse.Text = "Browse";
            this.cmdRobotDefinitionBrowse.UseVisualStyleBackColor = true;
            this.cmdRobotDefinitionBrowse.Click += new System.EventHandler(this.cmdRobotDefinitionBrowse_Click);
            // 
            // cmdStereoImagesPathBrowse
            // 
            this.cmdStereoImagesPathBrowse.Location = new System.Drawing.Point(587, 66);
            this.cmdStereoImagesPathBrowse.Name = "cmdStereoImagesPathBrowse";
            this.cmdStereoImagesPathBrowse.Size = new System.Drawing.Size(54, 20);
            this.cmdStereoImagesPathBrowse.TabIndex = 10;
            this.cmdStereoImagesPathBrowse.Text = "Browse";
            this.cmdStereoImagesPathBrowse.UseVisualStyleBackColor = true;
            this.cmdStereoImagesPathBrowse.Click += new System.EventHandler(this.cmdStereoImagesPathBrowse_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Stereo Images path";
            // 
            // txtStereoImagesPath
            // 
            this.txtStereoImagesPath.Location = new System.Drawing.Point(117, 67);
            this.txtStereoImagesPath.Name = "txtStereoImagesPath";
            this.txtStereoImagesPath.Size = new System.Drawing.Size(464, 20);
            this.txtStereoImagesPath.TabIndex = 8;
            // 
            // lstPathSegments
            // 
            this.lstPathSegments.Location = new System.Drawing.Point(17, 111);
            this.lstPathSegments.Name = "lstPathSegments";
            this.lstPathSegments.Size = new System.Drawing.Size(426, 162);
            this.lstPathSegments.TabIndex = 11;
            this.lstPathSegments.UseCompatibleStateImageBehavior = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 27);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "X position mm";
            // 
            // txtX
            // 
            this.txtX.Location = new System.Drawing.Point(107, 24);
            this.txtX.Name = "txtX";
            this.txtX.Size = new System.Drawing.Size(41, 20);
            this.txtX.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Y position mm";
            // 
            // txtY
            // 
            this.txtY.Location = new System.Drawing.Point(107, 50);
            this.txtY.Name = "txtY";
            this.txtY.Size = new System.Drawing.Size(41, 20);
            this.txtY.TabIndex = 14;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 79);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(88, 13);
            this.label5.TabIndex = 17;
            this.label5.Text = "Heading degrees";
            // 
            // txtHeading
            // 
            this.txtHeading.Location = new System.Drawing.Point(107, 76);
            this.txtHeading.Name = "txtHeading";
            this.txtHeading.Size = new System.Drawing.Size(41, 20);
            this.txtHeading.TabIndex = 16;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(191, 27);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(84, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Number of steps";
            // 
            // txtNoOfSteps
            // 
            this.txtNoOfSteps.Location = new System.Drawing.Point(362, 24);
            this.txtNoOfSteps.Name = "txtNoOfSteps";
            this.txtNoOfSteps.Size = new System.Drawing.Size(41, 20);
            this.txtNoOfSteps.TabIndex = 18;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(191, 53);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(109, 13);
            this.label7.TabIndex = 21;
            this.label7.Text = "Distance per step mm";
            // 
            // txtDistPerStep
            // 
            this.txtDistPerStep.Location = new System.Drawing.Point(362, 50);
            this.txtDistPerStep.Name = "txtDistPerStep";
            this.txtDistPerStep.Size = new System.Drawing.Size(41, 20);
            this.txtDistPerStep.TabIndex = 20;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(191, 79);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(168, 13);
            this.label8.TabIndex = 23;
            this.label8.Text = "Heading change per step degrees";
            // 
            // txtHeadingChangePerStep
            // 
            this.txtHeadingChangePerStep.Location = new System.Drawing.Point(362, 76);
            this.txtHeadingChangePerStep.Name = "txtHeadingChangePerStep";
            this.txtHeadingChangePerStep.Size = new System.Drawing.Size(41, 20);
            this.txtHeadingChangePerStep.TabIndex = 22;
            // 
            // grpNewPathSegment
            // 
            this.grpNewPathSegment.Controls.Add(this.cmdAdd);
            this.grpNewPathSegment.Controls.Add(this.label3);
            this.grpNewPathSegment.Controls.Add(this.label8);
            this.grpNewPathSegment.Controls.Add(this.txtX);
            this.grpNewPathSegment.Controls.Add(this.txtHeadingChangePerStep);
            this.grpNewPathSegment.Controls.Add(this.txtY);
            this.grpNewPathSegment.Controls.Add(this.label7);
            this.grpNewPathSegment.Controls.Add(this.label4);
            this.grpNewPathSegment.Controls.Add(this.txtDistPerStep);
            this.grpNewPathSegment.Controls.Add(this.txtHeading);
            this.grpNewPathSegment.Controls.Add(this.label6);
            this.grpNewPathSegment.Controls.Add(this.label5);
            this.grpNewPathSegment.Controls.Add(this.txtNoOfSteps);
            this.grpNewPathSegment.Location = new System.Drawing.Point(17, 297);
            this.grpNewPathSegment.Name = "grpNewPathSegment";
            this.grpNewPathSegment.Size = new System.Drawing.Size(426, 139);
            this.grpNewPathSegment.TabIndex = 24;
            this.grpNewPathSegment.TabStop = false;
            this.grpNewPathSegment.Text = "Add Path Segment";
            // 
            // cmdAdd
            // 
            this.cmdAdd.Location = new System.Drawing.Point(174, 106);
            this.cmdAdd.Name = "cmdAdd";
            this.cmdAdd.Size = new System.Drawing.Size(76, 27);
            this.cmdAdd.TabIndex = 24;
            this.cmdAdd.Text = "Add";
            this.cmdAdd.UseVisualStyleBackColor = true;
            // 
            // frmMapping
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 448);
            this.Controls.Add(this.grpNewPathSegment);
            this.Controls.Add(this.lstPathSegments);
            this.Controls.Add(this.cmdStereoImagesPathBrowse);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtStereoImagesPath);
            this.Controls.Add(this.cmdRobotDefinitionBrowse);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtRobotDefinitionFile);
            this.Controls.Add(this.lblPositionIndex);
            this.Controls.Add(this.picGrid);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMapping";
            this.Text = "Sentience Mapping";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMapping_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.picGrid)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.grpNewPathSegment.ResumeLayout(false);
            this.grpNewPathSegment.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picGrid;
        private System.Windows.Forms.Timer timAnimate;
        private System.Windows.Forms.Label lblPositionIndex;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.TextBox txtRobotDefinitionFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button cmdRobotDefinitionBrowse;
        private System.Windows.Forms.Button cmdStereoImagesPathBrowse;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtStereoImagesPath;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ListView lstPathSegments;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtX;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtY;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtHeading;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtNoOfSteps;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtDistPerStep;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtHeadingChangePerStep;
        private System.Windows.Forms.GroupBox grpNewPathSegment;
        private System.Windows.Forms.Button cmdAdd;
    }
}

