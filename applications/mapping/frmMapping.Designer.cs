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
            this.picPath = new System.Windows.Forms.PictureBox();
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
            this.cmdRemovePathSegment = new System.Windows.Forms.Button();
            this.cmdAdd = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtTitle = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.picPath)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.grpNewPathSegment.SuspendLayout();
            this.SuspendLayout();
            // 
            // picPath
            // 
            this.picPath.Location = new System.Drawing.Point(447, 139);
            this.picPath.Name = "picPath";
            this.picPath.Size = new System.Drawing.Size(343, 314);
            this.picPath.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picPath.TabIndex = 0;
            this.picPath.TabStop = false;
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
            this.menuStrip1.Size = new System.Drawing.Size(804, 24);
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
            this.txtRobotDefinitionFile.Location = new System.Drawing.Point(115, 64);
            this.txtRobotDefinitionFile.Name = "txtRobotDefinitionFile";
            this.txtRobotDefinitionFile.Size = new System.Drawing.Size(598, 20);
            this.txtRobotDefinitionFile.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Robot design file";
            // 
            // cmdRobotDefinitionBrowse
            // 
            this.cmdRobotDefinitionBrowse.Location = new System.Drawing.Point(719, 63);
            this.cmdRobotDefinitionBrowse.Name = "cmdRobotDefinitionBrowse";
            this.cmdRobotDefinitionBrowse.Size = new System.Drawing.Size(54, 20);
            this.cmdRobotDefinitionBrowse.TabIndex = 7;
            this.cmdRobotDefinitionBrowse.Text = "Browse";
            this.cmdRobotDefinitionBrowse.UseVisualStyleBackColor = true;
            this.cmdRobotDefinitionBrowse.Click += new System.EventHandler(this.cmdRobotDefinitionBrowse_Click);
            // 
            // cmdStereoImagesPathBrowse
            // 
            this.cmdStereoImagesPathBrowse.Location = new System.Drawing.Point(719, 89);
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
            this.label2.Location = new System.Drawing.Point(12, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Stereo Images path";
            // 
            // txtStereoImagesPath
            // 
            this.txtStereoImagesPath.Location = new System.Drawing.Point(115, 90);
            this.txtStereoImagesPath.Name = "txtStereoImagesPath";
            this.txtStereoImagesPath.Size = new System.Drawing.Size(598, 20);
            this.txtStereoImagesPath.TabIndex = 8;
            // 
            // lstPathSegments
            // 
            this.lstPathSegments.FullRowSelect = true;
            this.lstPathSegments.HideSelection = false;
            this.lstPathSegments.Location = new System.Drawing.Point(15, 139);
            this.lstPathSegments.Name = "lstPathSegments";
            this.lstPathSegments.Size = new System.Drawing.Size(426, 175);
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
            this.txtX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.txtY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.txtHeading.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.txtNoOfSteps.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.txtDistPerStep.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.txtHeadingChangePerStep.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // grpNewPathSegment
            // 
            this.grpNewPathSegment.Controls.Add(this.cmdRemovePathSegment);
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
            this.grpNewPathSegment.Location = new System.Drawing.Point(15, 320);
            this.grpNewPathSegment.Name = "grpNewPathSegment";
            this.grpNewPathSegment.Size = new System.Drawing.Size(426, 139);
            this.grpNewPathSegment.TabIndex = 24;
            this.grpNewPathSegment.TabStop = false;
            this.grpNewPathSegment.Text = "Add Path Segment";
            // 
            // cmdRemovePathSegment
            // 
            this.cmdRemovePathSegment.Location = new System.Drawing.Point(264, 106);
            this.cmdRemovePathSegment.Name = "cmdRemovePathSegment";
            this.cmdRemovePathSegment.Size = new System.Drawing.Size(76, 27);
            this.cmdRemovePathSegment.TabIndex = 25;
            this.cmdRemovePathSegment.Text = "Remove";
            this.cmdRemovePathSegment.UseVisualStyleBackColor = true;
            this.cmdRemovePathSegment.Click += new System.EventHandler(this.cmdRemovePathSegment_Click);
            // 
            // cmdAdd
            // 
            this.cmdAdd.Location = new System.Drawing.Point(107, 106);
            this.cmdAdd.Name = "cmdAdd";
            this.cmdAdd.Size = new System.Drawing.Size(76, 27);
            this.cmdAdd.TabIndex = 24;
            this.cmdAdd.Text = "Add";
            this.cmdAdd.UseVisualStyleBackColor = true;
            this.cmdAdd.Click += new System.EventHandler(this.cmdAdd_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 123);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(79, 13);
            this.label9.TabIndex = 25;
            this.label9.Text = "Path Segments";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 42);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 13);
            this.label10.TabIndex = 27;
            this.label10.Text = "Simulation Title";
            // 
            // txtTitle
            // 
            this.txtTitle.Location = new System.Drawing.Point(115, 38);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.Size = new System.Drawing.Size(598, 20);
            this.txtTitle.TabIndex = 26;
            // 
            // frmMapping
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 481);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtTitle);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.grpNewPathSegment);
            this.Controls.Add(this.lstPathSegments);
            this.Controls.Add(this.cmdStereoImagesPathBrowse);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtStereoImagesPath);
            this.Controls.Add(this.cmdRobotDefinitionBrowse);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtRobotDefinitionFile);
            this.Controls.Add(this.lblPositionIndex);
            this.Controls.Add(this.picPath);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMapping";
            this.Text = "Sentience Mapping";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMapping_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.picPath)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.grpNewPathSegment.ResumeLayout(false);
            this.grpNewPathSegment.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picPath;
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
        private System.Windows.Forms.Button cmdRemovePathSegment;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtTitle;
    }
}

