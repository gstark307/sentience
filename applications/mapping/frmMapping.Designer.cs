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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabParameters = new System.Windows.Forms.TabPage();
            this.txtTuningParameters = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.tabSimulation = new System.Windows.Forms.TabPage();
            this.label18 = new System.Windows.Forms.Label();
            this.txtPositionError = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.picBestPose = new System.Windows.Forms.PictureBox();
            this.picGridSideViewRight = new System.Windows.Forms.PictureBox();
            this.label16 = new System.Windows.Forms.Label();
            this.txtBestScore = new System.Windows.Forms.TextBox();
            this.picGridSideViewLeft = new System.Windows.Forms.PictureBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtMeanColourVariance = new System.Windows.Forms.TextBox();
            this.cmdOptimise = new System.Windows.Forms.Button();
            this.cmdStopSimulation = new System.Windows.Forms.Button();
            this.cmdRunSimulation = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.lstBenchmarks = new System.Windows.Forms.ListView();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.cmdReset = new System.Windows.Forms.Button();
            this.picGridMap = new System.Windows.Forms.PictureBox();
            this.cmdRunOneStep = new System.Windows.Forms.Button();
            this.tabPathTree = new System.Windows.Forms.TabPage();
            this.label19 = new System.Windows.Forms.Label();
            this.picPathTree = new System.Windows.Forms.PictureBox();
            this.tabOptimisation = new System.Windows.Forms.TabPage();
            this.label21 = new System.Windows.Forms.Label();
            this.picColourVariance = new System.Windows.Forms.PictureBox();
            this.timSimulation = new System.Windows.Forms.Timer(this.components);
            this.label20 = new System.Windows.Forms.Label();
            this.picNoOfParticles = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picPath)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.grpNewPathSegment.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabParameters.SuspendLayout();
            this.tabSimulation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picBestPose)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGridSideViewRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGridSideViewLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGridMap)).BeginInit();
            this.tabPathTree.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPathTree)).BeginInit();
            this.tabOptimisation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picColourVariance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picNoOfParticles)).BeginInit();
            this.SuspendLayout();
            // 
            // picPath
            // 
            this.picPath.Location = new System.Drawing.Point(451, 148);
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
            this.menuStrip1.Size = new System.Drawing.Size(845, 24);
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
            this.txtRobotDefinitionFile.Location = new System.Drawing.Point(120, 38);
            this.txtRobotDefinitionFile.Name = "txtRobotDefinitionFile";
            this.txtRobotDefinitionFile.Size = new System.Drawing.Size(598, 20);
            this.txtRobotDefinitionFile.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Robot design file";
            // 
            // cmdRobotDefinitionBrowse
            // 
            this.cmdRobotDefinitionBrowse.Location = new System.Drawing.Point(724, 37);
            this.cmdRobotDefinitionBrowse.Name = "cmdRobotDefinitionBrowse";
            this.cmdRobotDefinitionBrowse.Size = new System.Drawing.Size(54, 20);
            this.cmdRobotDefinitionBrowse.TabIndex = 7;
            this.cmdRobotDefinitionBrowse.Text = "Browse";
            this.cmdRobotDefinitionBrowse.UseVisualStyleBackColor = true;
            this.cmdRobotDefinitionBrowse.Click += new System.EventHandler(this.cmdRobotDefinitionBrowse_Click);
            // 
            // cmdStereoImagesPathBrowse
            // 
            this.cmdStereoImagesPathBrowse.Location = new System.Drawing.Point(724, 63);
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
            this.label2.Location = new System.Drawing.Point(17, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Stereo Images path";
            // 
            // txtStereoImagesPath
            // 
            this.txtStereoImagesPath.Location = new System.Drawing.Point(120, 64);
            this.txtStereoImagesPath.Name = "txtStereoImagesPath";
            this.txtStereoImagesPath.Size = new System.Drawing.Size(598, 20);
            this.txtStereoImagesPath.TabIndex = 8;
            // 
            // lstPathSegments
            // 
            this.lstPathSegments.FullRowSelect = true;
            this.lstPathSegments.HideSelection = false;
            this.lstPathSegments.Location = new System.Drawing.Point(19, 148);
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
            this.grpNewPathSegment.Location = new System.Drawing.Point(19, 329);
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
            this.label9.Location = new System.Drawing.Point(16, 132);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(79, 13);
            this.label9.TabIndex = 25;
            this.label9.Text = "Path Segments";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(17, 16);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 13);
            this.label10.TabIndex = 27;
            this.label10.Text = "Simulation Title";
            // 
            // txtTitle
            // 
            this.txtTitle.Location = new System.Drawing.Point(120, 12);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.Size = new System.Drawing.Size(598, 20);
            this.txtTitle.TabIndex = 26;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabParameters);
            this.tabControl1.Controls.Add(this.tabSimulation);
            this.tabControl1.Controls.Add(this.tabPathTree);
            this.tabControl1.Controls.Add(this.tabOptimisation);
            this.tabControl1.Location = new System.Drawing.Point(12, 27);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(823, 659);
            this.tabControl1.TabIndex = 28;
            // 
            // tabParameters
            // 
            this.tabParameters.Controls.Add(this.txtTuningParameters);
            this.tabParameters.Controls.Add(this.label14);
            this.tabParameters.Controls.Add(this.label10);
            this.tabParameters.Controls.Add(this.picPath);
            this.tabParameters.Controls.Add(this.txtTitle);
            this.tabParameters.Controls.Add(this.txtRobotDefinitionFile);
            this.tabParameters.Controls.Add(this.label9);
            this.tabParameters.Controls.Add(this.label1);
            this.tabParameters.Controls.Add(this.grpNewPathSegment);
            this.tabParameters.Controls.Add(this.cmdRobotDefinitionBrowse);
            this.tabParameters.Controls.Add(this.lstPathSegments);
            this.tabParameters.Controls.Add(this.txtStereoImagesPath);
            this.tabParameters.Controls.Add(this.cmdStereoImagesPathBrowse);
            this.tabParameters.Controls.Add(this.label2);
            this.tabParameters.Location = new System.Drawing.Point(4, 22);
            this.tabParameters.Name = "tabParameters";
            this.tabParameters.Padding = new System.Windows.Forms.Padding(3);
            this.tabParameters.Size = new System.Drawing.Size(815, 633);
            this.tabParameters.TabIndex = 0;
            this.tabParameters.Text = "Parameters";
            this.tabParameters.UseVisualStyleBackColor = true;
            // 
            // txtTuningParameters
            // 
            this.txtTuningParameters.Location = new System.Drawing.Point(120, 90);
            this.txtTuningParameters.Multiline = true;
            this.txtTuningParameters.Name = "txtTuningParameters";
            this.txtTuningParameters.Size = new System.Drawing.Size(598, 37);
            this.txtTuningParameters.TabIndex = 28;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(17, 94);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(96, 13);
            this.label14.TabIndex = 29;
            this.label14.Text = "Tuning Parameters";
            // 
            // tabSimulation
            // 
            this.tabSimulation.Controls.Add(this.label18);
            this.tabSimulation.Controls.Add(this.txtPositionError);
            this.tabSimulation.Controls.Add(this.label17);
            this.tabSimulation.Controls.Add(this.picBestPose);
            this.tabSimulation.Controls.Add(this.picGridSideViewRight);
            this.tabSimulation.Controls.Add(this.label16);
            this.tabSimulation.Controls.Add(this.txtBestScore);
            this.tabSimulation.Controls.Add(this.picGridSideViewLeft);
            this.tabSimulation.Controls.Add(this.label15);
            this.tabSimulation.Controls.Add(this.txtMeanColourVariance);
            this.tabSimulation.Controls.Add(this.cmdOptimise);
            this.tabSimulation.Controls.Add(this.cmdStopSimulation);
            this.tabSimulation.Controls.Add(this.cmdRunSimulation);
            this.tabSimulation.Controls.Add(this.label13);
            this.tabSimulation.Controls.Add(this.lstBenchmarks);
            this.tabSimulation.Controls.Add(this.label12);
            this.tabSimulation.Controls.Add(this.label11);
            this.tabSimulation.Controls.Add(this.picRightImage);
            this.tabSimulation.Controls.Add(this.picLeftImage);
            this.tabSimulation.Controls.Add(this.cmdReset);
            this.tabSimulation.Controls.Add(this.picGridMap);
            this.tabSimulation.Controls.Add(this.cmdRunOneStep);
            this.tabSimulation.Location = new System.Drawing.Point(4, 22);
            this.tabSimulation.Name = "tabSimulation";
            this.tabSimulation.Padding = new System.Windows.Forms.Padding(3);
            this.tabSimulation.Size = new System.Drawing.Size(815, 633);
            this.tabSimulation.TabIndex = 1;
            this.tabSimulation.Text = "Simulation";
            this.tabSimulation.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(17, 422);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(87, 13);
            this.label18.TabIndex = 46;
            this.label18.Text = "Position error mm";
            // 
            // txtPositionError
            // 
            this.txtPositionError.Location = new System.Drawing.Point(134, 419);
            this.txtPositionError.Name = "txtPositionError";
            this.txtPositionError.Size = new System.Drawing.Size(108, 20);
            this.txtPositionError.TabIndex = 45;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(102, 466);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(67, 13);
            this.label17.TabIndex = 44;
            this.label17.Text = "Current pose";
            // 
            // picBestPose
            // 
            this.picBestPose.Location = new System.Drawing.Point(52, 482);
            this.picBestPose.Name = "picBestPose";
            this.picBestPose.Size = new System.Drawing.Size(164, 142);
            this.picBestPose.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picBestPose.TabIndex = 43;
            this.picBestPose.TabStop = false;
            // 
            // picGridSideViewRight
            // 
            this.picGridSideViewRight.Location = new System.Drawing.Point(260, 538);
            this.picGridSideViewRight.Name = "picGridSideViewRight";
            this.picGridSideViewRight.Size = new System.Drawing.Size(538, 86);
            this.picGridSideViewRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picGridSideViewRight.TabIndex = 42;
            this.picGridSideViewRight.TabStop = false;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(17, 448);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(91, 13);
            this.label16.TabIndex = 41;
            this.label16.Text = "Minimum error mm";
            // 
            // txtBestScore
            // 
            this.txtBestScore.Location = new System.Drawing.Point(134, 445);
            this.txtBestScore.Name = "txtBestScore";
            this.txtBestScore.Size = new System.Drawing.Size(108, 20);
            this.txtBestScore.TabIndex = 40;
            // 
            // picGridSideViewLeft
            // 
            this.picGridSideViewLeft.Location = new System.Drawing.Point(260, 446);
            this.picGridSideViewLeft.Name = "picGridSideViewLeft";
            this.picGridSideViewLeft.Size = new System.Drawing.Size(538, 86);
            this.picGridSideViewLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picGridSideViewLeft.TabIndex = 39;
            this.picGridSideViewLeft.TabStop = false;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(17, 396);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(112, 13);
            this.label15.TabIndex = 38;
            this.label15.Text = "Mean Colour Variance";
            // 
            // txtMeanColourVariance
            // 
            this.txtMeanColourVariance.Location = new System.Drawing.Point(134, 393);
            this.txtMeanColourVariance.Name = "txtMeanColourVariance";
            this.txtMeanColourVariance.Size = new System.Drawing.Size(108, 20);
            this.txtMeanColourVariance.TabIndex = 37;
            // 
            // cmdOptimise
            // 
            this.cmdOptimise.Location = new System.Drawing.Point(20, 79);
            this.cmdOptimise.Name = "cmdOptimise";
            this.cmdOptimise.Size = new System.Drawing.Size(76, 27);
            this.cmdOptimise.TabIndex = 36;
            this.cmdOptimise.Text = "Optimise";
            this.cmdOptimise.UseVisualStyleBackColor = true;
            this.cmdOptimise.Click += new System.EventHandler(this.cmdOptimise_Click);
            // 
            // cmdStopSimulation
            // 
            this.cmdStopSimulation.Location = new System.Drawing.Point(105, 46);
            this.cmdStopSimulation.Name = "cmdStopSimulation";
            this.cmdStopSimulation.Size = new System.Drawing.Size(76, 27);
            this.cmdStopSimulation.TabIndex = 35;
            this.cmdStopSimulation.Text = "Stop";
            this.cmdStopSimulation.UseVisualStyleBackColor = true;
            this.cmdStopSimulation.Click += new System.EventHandler(this.cmdStopSimulation_Click);
            // 
            // cmdRunSimulation
            // 
            this.cmdRunSimulation.Location = new System.Drawing.Point(20, 46);
            this.cmdRunSimulation.Name = "cmdRunSimulation";
            this.cmdRunSimulation.Size = new System.Drawing.Size(76, 27);
            this.cmdRunSimulation.TabIndex = 34;
            this.cmdRunSimulation.Text = "Run";
            this.cmdRunSimulation.UseVisualStyleBackColor = true;
            this.cmdRunSimulation.Click += new System.EventHandler(this.cmdRunSimulation_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(17, 237);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(25, 13);
            this.label13.TabIndex = 33;
            this.label13.Text = "Info";
            // 
            // lstBenchmarks
            // 
            this.lstBenchmarks.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstBenchmarks.Location = new System.Drawing.Point(20, 253);
            this.lstBenchmarks.Name = "lstBenchmarks";
            this.lstBenchmarks.Size = new System.Drawing.Size(222, 129);
            this.lstBenchmarks.TabIndex = 32;
            this.lstBenchmarks.UseCompatibleStateImageBehavior = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(259, 22);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(82, 13);
            this.label12.TabIndex = 31;
            this.label12.Text = "Occupancy grid";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(17, 113);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(83, 13);
            this.label11.TabIndex = 30;
            this.label11.Text = "The robots view";
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(134, 138);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(108, 96);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 29;
            this.picRightImage.TabStop = false;
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(20, 138);
            this.picLeftImage.Name = "picLeftImage";
            this.picLeftImage.Size = new System.Drawing.Size(108, 96);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 28;
            this.picLeftImage.TabStop = false;
            // 
            // cmdReset
            // 
            this.cmdReset.Location = new System.Drawing.Point(20, 13);
            this.cmdReset.Name = "cmdReset";
            this.cmdReset.Size = new System.Drawing.Size(76, 27);
            this.cmdReset.TabIndex = 27;
            this.cmdReset.Text = "Reset";
            this.cmdReset.UseVisualStyleBackColor = true;
            this.cmdReset.Click += new System.EventHandler(this.cmdReset_Click);
            // 
            // picGridMap
            // 
            this.picGridMap.Location = new System.Drawing.Point(260, 38);
            this.picGridMap.Name = "picGridMap";
            this.picGridMap.Size = new System.Drawing.Size(538, 401);
            this.picGridMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picGridMap.TabIndex = 26;
            this.picGridMap.TabStop = false;
            // 
            // cmdRunOneStep
            // 
            this.cmdRunOneStep.Location = new System.Drawing.Point(105, 13);
            this.cmdRunOneStep.Name = "cmdRunOneStep";
            this.cmdRunOneStep.Size = new System.Drawing.Size(76, 27);
            this.cmdRunOneStep.TabIndex = 25;
            this.cmdRunOneStep.Text = "Step";
            this.cmdRunOneStep.UseVisualStyleBackColor = true;
            this.cmdRunOneStep.Click += new System.EventHandler(this.cmdRunOneStep_Click);
            // 
            // tabPathTree
            // 
            this.tabPathTree.Controls.Add(this.label19);
            this.tabPathTree.Controls.Add(this.picPathTree);
            this.tabPathTree.Location = new System.Drawing.Point(4, 22);
            this.tabPathTree.Name = "tabPathTree";
            this.tabPathTree.Size = new System.Drawing.Size(815, 633);
            this.tabPathTree.TabIndex = 2;
            this.tabPathTree.Text = "Path Tree";
            this.tabPathTree.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(17, 14);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(239, 13);
            this.label19.TabIndex = 33;
            this.label19.Text = "Possible paths which the robot could be following";
            // 
            // picPathTree
            // 
            this.picPathTree.Location = new System.Drawing.Point(18, 30);
            this.picPathTree.Name = "picPathTree";
            this.picPathTree.Size = new System.Drawing.Size(777, 582);
            this.picPathTree.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picPathTree.TabIndex = 32;
            this.picPathTree.TabStop = false;
            // 
            // tabOptimisation
            // 
            this.tabOptimisation.Controls.Add(this.label20);
            this.tabOptimisation.Controls.Add(this.picNoOfParticles);
            this.tabOptimisation.Controls.Add(this.label21);
            this.tabOptimisation.Controls.Add(this.picColourVariance);
            this.tabOptimisation.Location = new System.Drawing.Point(4, 22);
            this.tabOptimisation.Name = "tabOptimisation";
            this.tabOptimisation.Size = new System.Drawing.Size(815, 633);
            this.tabOptimisation.TabIndex = 3;
            this.tabOptimisation.Text = "Optimisation";
            this.tabOptimisation.UseVisualStyleBackColor = true;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(18, 9);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(82, 13);
            this.label21.TabIndex = 37;
            this.label21.Text = "Colour Variance";
            // 
            // picColourVariance
            // 
            this.picColourVariance.Location = new System.Drawing.Point(19, 25);
            this.picColourVariance.Name = "picColourVariance";
            this.picColourVariance.Size = new System.Drawing.Size(777, 235);
            this.picColourVariance.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picColourVariance.TabIndex = 36;
            this.picColourVariance.TabStop = false;
            // 
            // timSimulation
            // 
            this.timSimulation.Interval = 500;
            this.timSimulation.Tick += new System.EventHandler(this.timSimulation_Tick);
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(18, 277);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(98, 13);
            this.label20.TabIndex = 39;
            this.label20.Text = "Number of particles";
            // 
            // picNoOfParticles
            // 
            this.picNoOfParticles.Location = new System.Drawing.Point(19, 293);
            this.picNoOfParticles.Name = "picNoOfParticles";
            this.picNoOfParticles.Size = new System.Drawing.Size(777, 235);
            this.picNoOfParticles.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picNoOfParticles.TabIndex = 38;
            this.picNoOfParticles.TabStop = false;
            // 
            // frmMapping
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(845, 696);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.lblPositionIndex);
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
            this.tabControl1.ResumeLayout(false);
            this.tabParameters.ResumeLayout(false);
            this.tabParameters.PerformLayout();
            this.tabSimulation.ResumeLayout(false);
            this.tabSimulation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picBestPose)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGridSideViewRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGridSideViewLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGridMap)).EndInit();
            this.tabPathTree.ResumeLayout(false);
            this.tabPathTree.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPathTree)).EndInit();
            this.tabOptimisation.ResumeLayout(false);
            this.tabOptimisation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picColourVariance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picNoOfParticles)).EndInit();
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
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabParameters;
        private System.Windows.Forms.TabPage tabSimulation;
        private System.Windows.Forms.PictureBox picGridMap;
        private System.Windows.Forms.Button cmdRunOneStep;
        private System.Windows.Forms.Button cmdReset;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.PictureBox picRightImage;
        private System.Windows.Forms.PictureBox picLeftImage;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListView lstBenchmarks;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button cmdRunSimulation;
        private System.Windows.Forms.Timer timSimulation;
        private System.Windows.Forms.Button cmdStopSimulation;
        private System.Windows.Forms.Button cmdOptimise;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtMeanColourVariance;
        private System.Windows.Forms.TextBox txtTuningParameters;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.PictureBox picGridSideViewLeft;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtBestScore;
        private System.Windows.Forms.PictureBox picGridSideViewRight;
        private System.Windows.Forms.PictureBox picBestPose;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtPositionError;
        private System.Windows.Forms.TabPage tabPathTree;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.PictureBox picPathTree;
        private System.Windows.Forms.TabPage tabOptimisation;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.PictureBox picColourVariance;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.PictureBox picNoOfParticles;
    }
}

