namespace robotDesigner
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
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importCalibrationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.grpBody = new System.Windows.Forms.GroupBox();
            this.label22 = new System.Windows.Forms.Label();
            this.cmbBodyShape = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbPropulsion = new System.Windows.Forms.ComboBox();
            this.txtBodyHeight = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtBodyLength = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtBodyWidth = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.grpPropulsion = new System.Windows.Forms.GroupBox();
            this.txtWheelBaseForward = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.txtWheelBase = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.txtCountsPerRev = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtGearRatio = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbWheelFeedback = new System.Windows.Forms.ComboBox();
            this.txtWheelDiameter = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtHeadPositionForward = new System.Windows.Forms.TextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.txtHeadPositionLeft = new System.Windows.Forms.TextBox();
            this.label23 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.cmbCameraOrientation = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.cmbHeadShape = new System.Windows.Forms.ComboBox();
            this.txtNoOfCameras = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtHeadHeightFromGround = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.txtHeadSize = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.cmbHeadType = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtTotalMass = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.grpStereoCamera = new System.Windows.Forms.GroupBox();
            this.txtCameraFOV = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.txtCameraBaseline = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tabRobot = new System.Windows.Forms.TabControl();
            this.tabGeometry = new System.Windows.Forms.TabPage();
            this.tabPerception = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txtGridInterval = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.txtGridLevels = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.txtGridDimension = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.txtGridCellDimension = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.txtMotorNoLoadSpeedRPM = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.txtMotorTorque = new System.Windows.Forms.TextBox();
            this.label30 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.grpBody.SuspendLayout();
            this.grpPropulsion.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.grpStereoCamera.SuspendLayout();
            this.tabRobot.SuspendLayout();
            this.tabGeometry.SuspendLayout();
            this.tabPerception.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(577, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.importCalibrationToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // importCalibrationToolStripMenuItem
            // 
            this.importCalibrationToolStripMenuItem.Name = "importCalibrationToolStripMenuItem";
            this.importCalibrationToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.importCalibrationToolStripMenuItem.Text = "Import Calibration";
            this.importCalibrationToolStripMenuItem.Click += new System.EventHandler(this.importCalibrationToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // grpBody
            // 
            this.grpBody.Controls.Add(this.label22);
            this.grpBody.Controls.Add(this.cmbBodyShape);
            this.grpBody.Controls.Add(this.txtBodyHeight);
            this.grpBody.Controls.Add(this.label3);
            this.grpBody.Controls.Add(this.txtBodyLength);
            this.grpBody.Controls.Add(this.label2);
            this.grpBody.Controls.Add(this.txtBodyWidth);
            this.grpBody.Controls.Add(this.label1);
            this.grpBody.Location = new System.Drawing.Point(18, 94);
            this.grpBody.Name = "grpBody";
            this.grpBody.Size = new System.Drawing.Size(212, 175);
            this.grpBody.TabIndex = 1;
            this.grpBody.TabStop = false;
            this.grpBody.Text = "Body";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(19, 110);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(38, 13);
            this.label22.TabIndex = 25;
            this.label22.Text = "Shape";
            // 
            // cmbBodyShape
            // 
            this.cmbBodyShape.FormattingEnabled = true;
            this.cmbBodyShape.Items.AddRange(new object[] {
            "Square",
            "Round",
            "Other"});
            this.cmbBodyShape.Location = new System.Drawing.Point(80, 107);
            this.cmbBodyShape.Name = "cmbBodyShape";
            this.cmbBodyShape.Size = new System.Drawing.Size(93, 21);
            this.cmbBodyShape.TabIndex = 24;
            this.cmbBodyShape.Text = "Square";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 26);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(31, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Type";
            // 
            // cmbPropulsion
            // 
            this.cmbPropulsion.FormattingEnabled = true;
            this.cmbPropulsion.Items.AddRange(new object[] {
            "Wheels",
            "Tracks",
            "Legs"});
            this.cmbPropulsion.Location = new System.Drawing.Point(170, 19);
            this.cmbPropulsion.Name = "cmbPropulsion";
            this.cmbPropulsion.Size = new System.Drawing.Size(93, 21);
            this.cmbPropulsion.TabIndex = 6;
            this.cmbPropulsion.Text = "Wheels";
            this.cmbPropulsion.SelectedIndexChanged += new System.EventHandler(this.cmbPropulsion_SelectedIndexChanged);
            // 
            // txtBodyHeight
            // 
            this.txtBodyHeight.Location = new System.Drawing.Point(80, 78);
            this.txtBodyHeight.Name = "txtBodyHeight";
            this.txtBodyHeight.Size = new System.Drawing.Size(59, 20);
            this.txtBodyHeight.TabIndex = 5;
            this.txtBodyHeight.Text = "80";
            this.txtBodyHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 81);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Height mm";
            // 
            // txtBodyLength
            // 
            this.txtBodyLength.Location = new System.Drawing.Point(80, 52);
            this.txtBodyLength.Name = "txtBodyLength";
            this.txtBodyLength.Size = new System.Drawing.Size(59, 20);
            this.txtBodyLength.TabIndex = 3;
            this.txtBodyLength.Text = "200";
            this.txtBodyLength.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Length mm";
            // 
            // txtBodyWidth
            // 
            this.txtBodyWidth.Location = new System.Drawing.Point(80, 26);
            this.txtBodyWidth.Name = "txtBodyWidth";
            this.txtBodyWidth.Size = new System.Drawing.Size(59, 20);
            this.txtBodyWidth.TabIndex = 1;
            this.txtBodyWidth.Text = "200";
            this.txtBodyWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Width mm";
            // 
            // grpPropulsion
            // 
            this.grpPropulsion.Controls.Add(this.txtMotorTorque);
            this.grpPropulsion.Controls.Add(this.label30);
            this.grpPropulsion.Controls.Add(this.label4);
            this.grpPropulsion.Controls.Add(this.txtMotorNoLoadSpeedRPM);
            this.grpPropulsion.Controls.Add(this.cmbPropulsion);
            this.grpPropulsion.Controls.Add(this.label27);
            this.grpPropulsion.Controls.Add(this.txtWheelBaseForward);
            this.grpPropulsion.Controls.Add(this.label21);
            this.grpPropulsion.Controls.Add(this.txtWheelBase);
            this.grpPropulsion.Controls.Add(this.label20);
            this.grpPropulsion.Controls.Add(this.txtCountsPerRev);
            this.grpPropulsion.Controls.Add(this.label9);
            this.grpPropulsion.Controls.Add(this.label8);
            this.grpPropulsion.Controls.Add(this.txtGearRatio);
            this.grpPropulsion.Controls.Add(this.label7);
            this.grpPropulsion.Controls.Add(this.label6);
            this.grpPropulsion.Controls.Add(this.cmbWheelFeedback);
            this.grpPropulsion.Controls.Add(this.txtWheelDiameter);
            this.grpPropulsion.Controls.Add(this.label5);
            this.grpPropulsion.Location = new System.Drawing.Point(254, 94);
            this.grpPropulsion.Name = "grpPropulsion";
            this.grpPropulsion.Size = new System.Drawing.Size(269, 272);
            this.grpPropulsion.TabIndex = 2;
            this.grpPropulsion.TabStop = false;
            this.grpPropulsion.Text = "Propulsion";
            // 
            // txtWheelBaseForward
            // 
            this.txtWheelBaseForward.Location = new System.Drawing.Point(170, 185);
            this.txtWheelBaseForward.Name = "txtWheelBaseForward";
            this.txtWheelBaseForward.Size = new System.Drawing.Size(59, 20);
            this.txtWheelBaseForward.TabIndex = 18;
            this.txtWheelBaseForward.Text = "25";
            this.txtWheelBaseForward.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(22, 191);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(121, 13);
            this.label21.TabIndex = 17;
            this.label21.Text = "Wheelbase Forward mm";
            // 
            // txtWheelBase
            // 
            this.txtWheelBase.Location = new System.Drawing.Point(170, 159);
            this.txtWheelBase.Name = "txtWheelBase";
            this.txtWheelBase.Size = new System.Drawing.Size(59, 20);
            this.txtWheelBase.TabIndex = 16;
            this.txtWheelBase.Text = "175";
            this.txtWheelBase.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(22, 165);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(80, 13);
            this.label20.TabIndex = 15;
            this.label20.Text = "Wheelbase mm";
            // 
            // txtCountsPerRev
            // 
            this.txtCountsPerRev.Location = new System.Drawing.Point(170, 133);
            this.txtCountsPerRev.Name = "txtCountsPerRev";
            this.txtCountsPerRev.Size = new System.Drawing.Size(59, 20);
            this.txtCountsPerRev.TabIndex = 14;
            this.txtCountsPerRev.Text = "32";
            this.txtCountsPerRev.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(22, 139);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 13);
            this.label9.TabIndex = 13;
            this.label9.Text = "Counts/Rev";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(207, 110);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(19, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = ": 1";
            // 
            // txtGearRatio
            // 
            this.txtGearRatio.Location = new System.Drawing.Point(170, 107);
            this.txtGearRatio.Name = "txtGearRatio";
            this.txtGearRatio.Size = new System.Drawing.Size(31, 20);
            this.txtGearRatio.TabIndex = 11;
            this.txtGearRatio.Text = "54";
            this.txtGearRatio.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(22, 113);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 13);
            this.label7.TabIndex = 10;
            this.label7.Text = "Gear Ratio";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(22, 83);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(55, 13);
            this.label6.TabIndex = 9;
            this.label6.Text = "Feedback";
            // 
            // cmbWheelFeedback
            // 
            this.cmbWheelFeedback.FormattingEnabled = true;
            this.cmbWheelFeedback.Items.AddRange(new object[] {
            "None",
            "Encoder",
            "Resolver"});
            this.cmbWheelFeedback.Location = new System.Drawing.Point(170, 77);
            this.cmbWheelFeedback.Name = "cmbWheelFeedback";
            this.cmbWheelFeedback.Size = new System.Drawing.Size(93, 21);
            this.cmbWheelFeedback.TabIndex = 8;
            this.cmbWheelFeedback.Text = "None";
            // 
            // txtWheelDiameter
            // 
            this.txtWheelDiameter.Location = new System.Drawing.Point(170, 48);
            this.txtWheelDiameter.Name = "txtWheelDiameter";
            this.txtWheelDiameter.Size = new System.Drawing.Size(59, 20);
            this.txtWheelDiameter.TabIndex = 3;
            this.txtWheelDiameter.Text = "70";
            this.txtWheelDiameter.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(22, 54);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(68, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Diameter mm";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtHeadPositionForward);
            this.groupBox1.Controls.Add(this.label24);
            this.groupBox1.Controls.Add(this.txtHeadPositionLeft);
            this.groupBox1.Controls.Add(this.label23);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.cmbCameraOrientation);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.cmbHeadShape);
            this.groupBox1.Controls.Add(this.txtNoOfCameras);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.txtHeadHeightFromGround);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.txtHeadSize);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.cmbHeadType);
            this.groupBox1.Location = new System.Drawing.Point(254, 372);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(269, 254);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Head";
            // 
            // txtHeadPositionForward
            // 
            this.txtHeadPositionForward.Location = new System.Drawing.Point(170, 162);
            this.txtHeadPositionForward.Name = "txtHeadPositionForward";
            this.txtHeadPositionForward.Size = new System.Drawing.Size(59, 20);
            this.txtHeadPositionForward.TabIndex = 29;
            this.txtHeadPositionForward.Text = "120";
            this.txtHeadPositionForward.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(20, 165);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(119, 13);
            this.label24.TabIndex = 28;
            this.label24.Text = "Offset from the front mm";
            // 
            // txtHeadPositionLeft
            // 
            this.txtHeadPositionLeft.Location = new System.Drawing.Point(170, 136);
            this.txtHeadPositionLeft.Name = "txtHeadPositionLeft";
            this.txtHeadPositionLeft.Size = new System.Drawing.Size(59, 20);
            this.txtHeadPositionLeft.TabIndex = 27;
            this.txtHeadPositionLeft.Text = "100";
            this.txtHeadPositionLeft.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(20, 139);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(134, 13);
            this.label23.TabIndex = 26;
            this.label23.Text = "Offset from the left side mm";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(20, 218);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(58, 13);
            this.label12.TabIndex = 25;
            this.label12.Text = "Orientation";
            // 
            // cmbCameraOrientation
            // 
            this.cmbCameraOrientation.FormattingEnabled = true;
            this.cmbCameraOrientation.Items.AddRange(new object[] {
            "Standard forward looking",
            "Diagonal"});
            this.cmbCameraOrientation.Location = new System.Drawing.Point(84, 215);
            this.cmbCameraOrientation.Name = "cmbCameraOrientation";
            this.cmbCameraOrientation.Size = new System.Drawing.Size(164, 21);
            this.cmbCameraOrientation.TabIndex = 24;
            this.cmbCameraOrientation.Text = "Standard forward looking";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(20, 79);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(38, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "Shape";
            // 
            // cmbHeadShape
            // 
            this.cmbHeadShape.FormattingEnabled = true;
            this.cmbHeadShape.Items.AddRange(new object[] {
            "Cube",
            "Sphere",
            "Other"});
            this.cmbHeadShape.Location = new System.Drawing.Point(86, 76);
            this.cmbHeadShape.Name = "cmbHeadShape";
            this.cmbHeadShape.Size = new System.Drawing.Size(93, 21);
            this.cmbHeadShape.TabIndex = 22;
            this.cmbHeadShape.Text = "Cube";
            // 
            // txtNoOfCameras
            // 
            this.txtNoOfCameras.Location = new System.Drawing.Point(137, 189);
            this.txtNoOfCameras.Name = "txtNoOfCameras";
            this.txtNoOfCameras.Size = new System.Drawing.Size(27, 20);
            this.txtNoOfCameras.TabIndex = 21;
            this.txtNoOfCameras.Text = "1";
            this.txtNoOfCameras.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(20, 192);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(111, 13);
            this.label15.TabIndex = 20;
            this.label15.Text = "No of Stereo Cameras";
            // 
            // txtHeadHeightFromGround
            // 
            this.txtHeadHeightFromGround.Location = new System.Drawing.Point(170, 110);
            this.txtHeadHeightFromGround.Name = "txtHeadHeightFromGround";
            this.txtHeadHeightFromGround.Size = new System.Drawing.Size(59, 20);
            this.txtHeadHeightFromGround.TabIndex = 19;
            this.txtHeadHeightFromGround.Text = "1000";
            this.txtHeadHeightFromGround.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(20, 113);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(144, 13);
            this.label14.TabIndex = 18;
            this.label14.Text = "Height above the ground mm";
            // 
            // txtHeadSize
            // 
            this.txtHeadSize.Location = new System.Drawing.Point(86, 48);
            this.txtHeadSize.Name = "txtHeadSize";
            this.txtHeadSize.Size = new System.Drawing.Size(59, 20);
            this.txtHeadSize.TabIndex = 13;
            this.txtHeadSize.Text = "50";
            this.txtHeadSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(20, 51);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(46, 13);
            this.label13.TabIndex = 12;
            this.label13.Text = "Size mm";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(20, 24);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(60, 13);
            this.label10.TabIndex = 11;
            this.label10.Text = "Head Type";
            // 
            // cmbHeadType
            // 
            this.cmbHeadType.FormattingEnabled = true;
            this.cmbHeadType.Items.AddRange(new object[] {
            "None or Fixed to the chassis",
            "Tilt only",
            "Pan and Tilt",
            "Pan only",
            "Elevate only",
            "Tilt and Elevate",
            "Pan, Tilt and Elevate"});
            this.cmbHeadType.Location = new System.Drawing.Point(86, 21);
            this.cmbHeadType.Name = "cmbHeadType";
            this.cmbHeadType.Size = new System.Drawing.Size(162, 21);
            this.cmbHeadType.TabIndex = 10;
            this.cmbHeadType.Text = "None or Fixed to the chassis";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtTotalMass);
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.txtName);
            this.groupBox2.Controls.Add(this.label16);
            this.groupBox2.Location = new System.Drawing.Point(18, 17);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(505, 71);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "General";
            // 
            // txtTotalMass
            // 
            this.txtTotalMass.Location = new System.Drawing.Point(94, 45);
            this.txtTotalMass.Name = "txtTotalMass";
            this.txtTotalMass.Size = new System.Drawing.Size(59, 20);
            this.txtTotalMass.TabIndex = 5;
            this.txtTotalMass.Text = "1";
            this.txtTotalMass.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(17, 48);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(75, 13);
            this.label17.TabIndex = 4;
            this.label17.Text = "Total Mass Kg";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(94, 19);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(390, 20);
            this.txtName.TabIndex = 3;
            this.txtName.Text = "My Robot";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(17, 22);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(35, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Name";
            // 
            // grpStereoCamera
            // 
            this.grpStereoCamera.Controls.Add(this.txtCameraFOV);
            this.grpStereoCamera.Controls.Add(this.label19);
            this.grpStereoCamera.Controls.Add(this.txtCameraBaseline);
            this.grpStereoCamera.Controls.Add(this.label18);
            this.grpStereoCamera.Location = new System.Drawing.Point(18, 290);
            this.grpStereoCamera.Name = "grpStereoCamera";
            this.grpStereoCamera.Size = new System.Drawing.Size(212, 129);
            this.grpStereoCamera.TabIndex = 5;
            this.grpStereoCamera.TabStop = false;
            this.grpStereoCamera.Text = "Stereo Camera";
            // 
            // txtCameraFOV
            // 
            this.txtCameraFOV.Location = new System.Drawing.Point(131, 45);
            this.txtCameraFOV.Name = "txtCameraFOV";
            this.txtCameraFOV.Size = new System.Drawing.Size(59, 20);
            this.txtCameraFOV.TabIndex = 5;
            this.txtCameraFOV.Text = "78";
            this.txtCameraFOV.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(17, 48);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(108, 13);
            this.label19.TabIndex = 4;
            this.label19.Text = "Field of View degrees";
            // 
            // txtCameraBaseline
            // 
            this.txtCameraBaseline.Location = new System.Drawing.Point(131, 19);
            this.txtCameraBaseline.Name = "txtCameraBaseline";
            this.txtCameraBaseline.Size = new System.Drawing.Size(59, 20);
            this.txtCameraBaseline.TabIndex = 3;
            this.txtCameraBaseline.Text = "100";
            this.txtCameraBaseline.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(17, 22);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(66, 13);
            this.label18.TabIndex = 2;
            this.label18.Text = "Baseline mm";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // tabRobot
            // 
            this.tabRobot.Controls.Add(this.tabGeometry);
            this.tabRobot.Controls.Add(this.tabPerception);
            this.tabRobot.Location = new System.Drawing.Point(12, 37);
            this.tabRobot.Name = "tabRobot";
            this.tabRobot.SelectedIndex = 0;
            this.tabRobot.Size = new System.Drawing.Size(555, 667);
            this.tabRobot.TabIndex = 6;
            // 
            // tabGeometry
            // 
            this.tabGeometry.Controls.Add(this.groupBox2);
            this.tabGeometry.Controls.Add(this.grpStereoCamera);
            this.tabGeometry.Controls.Add(this.grpBody);
            this.tabGeometry.Controls.Add(this.grpPropulsion);
            this.tabGeometry.Controls.Add(this.groupBox1);
            this.tabGeometry.Location = new System.Drawing.Point(4, 22);
            this.tabGeometry.Name = "tabGeometry";
            this.tabGeometry.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeometry.Size = new System.Drawing.Size(547, 641);
            this.tabGeometry.TabIndex = 0;
            this.tabGeometry.Text = "Geometry";
            this.tabGeometry.UseVisualStyleBackColor = true;
            // 
            // tabPerception
            // 
            this.tabPerception.Controls.Add(this.groupBox3);
            this.tabPerception.Location = new System.Drawing.Point(4, 22);
            this.tabPerception.Name = "tabPerception";
            this.tabPerception.Padding = new System.Windows.Forms.Padding(3);
            this.tabPerception.Size = new System.Drawing.Size(547, 550);
            this.tabPerception.TabIndex = 1;
            this.tabPerception.Text = "Perception";
            this.tabPerception.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.txtGridInterval);
            this.groupBox3.Controls.Add(this.label26);
            this.groupBox3.Controls.Add(this.txtGridLevels);
            this.groupBox3.Controls.Add(this.label25);
            this.groupBox3.Controls.Add(this.txtGridDimension);
            this.groupBox3.Controls.Add(this.label28);
            this.groupBox3.Controls.Add(this.txtGridCellDimension);
            this.groupBox3.Controls.Add(this.label29);
            this.groupBox3.Location = new System.Drawing.Point(17, 19);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(217, 143);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Local Occupancy Map";
            // 
            // txtGridInterval
            // 
            this.txtGridInterval.Location = new System.Drawing.Point(131, 104);
            this.txtGridInterval.Name = "txtGridInterval";
            this.txtGridInterval.Size = new System.Drawing.Size(59, 20);
            this.txtGridInterval.TabIndex = 7;
            this.txtGridInterval.Text = "200";
            this.txtGridInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(17, 104);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(83, 13);
            this.label26.TabIndex = 6;
            this.label26.Text = "Grid Interval mm";
            // 
            // txtGridLevels
            // 
            this.txtGridLevels.Location = new System.Drawing.Point(131, 78);
            this.txtGridLevels.Name = "txtGridLevels";
            this.txtGridLevels.Size = new System.Drawing.Size(59, 20);
            this.txtGridLevels.TabIndex = 5;
            this.txtGridLevels.Text = "1";
            this.txtGridLevels.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(17, 78);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(60, 13);
            this.label25.TabIndex = 4;
            this.label25.Text = "Grid Levels";
            // 
            // txtGridDimension
            // 
            this.txtGridDimension.Location = new System.Drawing.Point(131, 52);
            this.txtGridDimension.Name = "txtGridDimension";
            this.txtGridDimension.Size = new System.Drawing.Size(59, 20);
            this.txtGridDimension.TabIndex = 3;
            this.txtGridDimension.Text = "128";
            this.txtGridDimension.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(17, 55);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(108, 13);
            this.label28.TabIndex = 2;
            this.label28.Text = "Grid Dimension (cells)";
            // 
            // txtGridCellDimension
            // 
            this.txtGridCellDimension.Location = new System.Drawing.Point(131, 29);
            this.txtGridCellDimension.Name = "txtGridCellDimension";
            this.txtGridCellDimension.Size = new System.Drawing.Size(59, 20);
            this.txtGridCellDimension.TabIndex = 1;
            this.txtGridCellDimension.Text = "32";
            this.txtGridCellDimension.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(17, 29);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(95, 13);
            this.label29.TabIndex = 0;
            this.label29.Text = "Cell Dimension mm";
            // 
            // txtMotorNoLoadSpeedRPM
            // 
            this.txtMotorNoLoadSpeedRPM.Location = new System.Drawing.Point(170, 211);
            this.txtMotorNoLoadSpeedRPM.Name = "txtMotorNoLoadSpeedRPM";
            this.txtMotorNoLoadSpeedRPM.Size = new System.Drawing.Size(59, 20);
            this.txtMotorNoLoadSpeedRPM.TabIndex = 20;
            this.txtMotorNoLoadSpeedRPM.Text = "175";
            this.txtMotorNoLoadSpeedRPM.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(22, 217);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(131, 13);
            this.label27.TabIndex = 19;
            this.label27.Text = "Motor no load speed RPM";
            // 
            // txtMotorTorque
            // 
            this.txtMotorTorque.Location = new System.Drawing.Point(170, 237);
            this.txtMotorTorque.Name = "txtMotorTorque";
            this.txtMotorTorque.Size = new System.Drawing.Size(59, 20);
            this.txtMotorTorque.TabIndex = 22;
            this.txtMotorTorque.Text = "80";
            this.txtMotorTorque.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(22, 243);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(133, 13);
            this.label30.TabIndex = 21;
            this.label30.Text = "Motor torque rating Kg/mm";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(577, 716);
            this.Controls.Add(this.tabRobot);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Robot Designer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.grpBody.ResumeLayout(false);
            this.grpBody.PerformLayout();
            this.grpPropulsion.ResumeLayout(false);
            this.grpPropulsion.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.grpStereoCamera.ResumeLayout(false);
            this.grpStereoCamera.PerformLayout();
            this.tabRobot.ResumeLayout(false);
            this.tabGeometry.ResumeLayout(false);
            this.tabPerception.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.GroupBox grpBody;
        private System.Windows.Forms.TextBox txtBodyLength;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtBodyWidth;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cmbPropulsion;
        private System.Windows.Forms.TextBox txtBodyHeight;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox grpPropulsion;
        private System.Windows.Forms.TextBox txtCountsPerRev;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtGearRatio;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbWheelFeedback;
        private System.Windows.Forms.TextBox txtWheelDiameter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmbHeadType;
        private System.Windows.Forms.TextBox txtHeadHeightFromGround;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtHeadSize;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtNoOfCameras;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cmbCameraOrientation;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cmbHeadShape;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtTotalMass;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.GroupBox grpStereoCamera;
        private System.Windows.Forms.TextBox txtCameraBaseline;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtCameraFOV;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox txtWheelBase;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox txtWheelBaseForward;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ComboBox cmbBodyShape;
        private System.Windows.Forms.TextBox txtHeadPositionForward;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.TextBox txtHeadPositionLeft;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TabControl tabRobot;
        private System.Windows.Forms.TabPage tabGeometry;
        private System.Windows.Forms.TabPage tabPerception;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtGridDimension;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.TextBox txtGridCellDimension;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.TextBox txtGridLevels;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.ToolStripMenuItem importCalibrationToolStripMenuItem;
        private System.Windows.Forms.TextBox txtGridInterval;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.TextBox txtMotorTorque;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.TextBox txtMotorNoLoadSpeedRPM;
        private System.Windows.Forms.Label label27;
    }
}

