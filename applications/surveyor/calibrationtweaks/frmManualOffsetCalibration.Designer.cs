namespace calibrationtweaks
{
    partial class frmManualOffsetCalibration
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
            this.openLeftImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openRightImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveAnimatedGifToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.picLeftImage = new System.Windows.Forms.PictureBox();
            this.picRightImage = new System.Windows.Forms.PictureBox();
            this.timAnimation = new System.Windows.Forms.Timer(this.components);
            this.picAnimation = new System.Windows.Forms.PictureBox();
            this.grpAdjustments = new System.Windows.Forms.GroupBox();
            this.lblRotation = new System.Windows.Forms.Label();
            this.txtRotation = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtInterval = new System.Windows.Forms.TextBox();
            this.chkAnimate = new System.Windows.Forms.CheckBox();
            this.lblScale = new System.Windows.Forms.Label();
            this.txtScale = new System.Windows.Forms.TextBox();
            this.lblOffsetY = new System.Windows.Forms.Label();
            this.txtOffsetY = new System.Windows.Forms.TextBox();
            this.lblOffsetX = new System.Windows.Forms.Label();
            this.txtOffsetX = new System.Windows.Forms.TextBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAnimation)).BeginInit();
            this.grpAdjustments.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(675, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openLeftImageToolStripMenuItem,
            this.openRightImageToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveAnimatedGifToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openLeftImageToolStripMenuItem
            // 
            this.openLeftImageToolStripMenuItem.Name = "openLeftImageToolStripMenuItem";
            this.openLeftImageToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.openLeftImageToolStripMenuItem.Text = "Open left image";
            this.openLeftImageToolStripMenuItem.Click += new System.EventHandler(this.openLeftImageToolStripMenuItem_Click);
            // 
            // openRightImageToolStripMenuItem
            // 
            this.openRightImageToolStripMenuItem.Name = "openRightImageToolStripMenuItem";
            this.openRightImageToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.openRightImageToolStripMenuItem.Text = "Open right image";
            this.openRightImageToolStripMenuItem.Click += new System.EventHandler(this.openRightImageToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(165, 6);
            // 
            // saveAnimatedGifToolStripMenuItem
            // 
            this.saveAnimatedGifToolStripMenuItem.Name = "saveAnimatedGifToolStripMenuItem";
            this.saveAnimatedGifToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.saveAnimatedGifToolStripMenuItem.Text = "Save animated gif";
            this.saveAnimatedGifToolStripMenuItem.Click += new System.EventHandler(this.saveAnimatedGifToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(165, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(25, 38);
            this.picLeftImage.Name = "picLeftImage";
            this.picLeftImage.Size = new System.Drawing.Size(149, 122);
            this.picLeftImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLeftImage.TabIndex = 1;
            this.picLeftImage.TabStop = false;
            // 
            // picRightImage
            // 
            this.picRightImage.Location = new System.Drawing.Point(190, 38);
            this.picRightImage.Name = "picRightImage";
            this.picRightImage.Size = new System.Drawing.Size(149, 122);
            this.picRightImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRightImage.TabIndex = 2;
            this.picRightImage.TabStop = false;
            // 
            // timAnimation
            // 
            this.timAnimation.Enabled = true;
            this.timAnimation.Interval = 1000;
            this.timAnimation.Tick += new System.EventHandler(this.timAnimation_Tick);
            // 
            // picAnimation
            // 
            this.picAnimation.Location = new System.Drawing.Point(25, 166);
            this.picAnimation.Name = "picAnimation";
            this.picAnimation.Size = new System.Drawing.Size(149, 122);
            this.picAnimation.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picAnimation.TabIndex = 3;
            this.picAnimation.TabStop = false;
            // 
            // grpAdjustments
            // 
            this.grpAdjustments.Controls.Add(this.lblRotation);
            this.grpAdjustments.Controls.Add(this.txtRotation);
            this.grpAdjustments.Controls.Add(this.label1);
            this.grpAdjustments.Controls.Add(this.txtInterval);
            this.grpAdjustments.Controls.Add(this.chkAnimate);
            this.grpAdjustments.Controls.Add(this.lblScale);
            this.grpAdjustments.Controls.Add(this.txtScale);
            this.grpAdjustments.Controls.Add(this.lblOffsetY);
            this.grpAdjustments.Controls.Add(this.txtOffsetY);
            this.grpAdjustments.Controls.Add(this.lblOffsetX);
            this.grpAdjustments.Controls.Add(this.txtOffsetX);
            this.grpAdjustments.Location = new System.Drawing.Point(385, 41);
            this.grpAdjustments.Name = "grpAdjustments";
            this.grpAdjustments.Size = new System.Drawing.Size(169, 208);
            this.grpAdjustments.TabIndex = 4;
            this.grpAdjustments.TabStop = false;
            this.grpAdjustments.Text = "Adjustments";
            // 
            // lblRotation
            // 
            this.lblRotation.AutoSize = true;
            this.lblRotation.Location = new System.Drawing.Point(17, 109);
            this.lblRotation.Name = "lblRotation";
            this.lblRotation.Size = new System.Drawing.Size(88, 13);
            this.lblRotation.TabIndex = 10;
            this.lblRotation.Text = "Rotation degrees";
            // 
            // txtRotation
            // 
            this.txtRotation.Location = new System.Drawing.Point(111, 106);
            this.txtRotation.Name = "txtRotation";
            this.txtRotation.Size = new System.Drawing.Size(39, 20);
            this.txtRotation.TabIndex = 3;
            this.txtRotation.Text = "0";
            this.txtRotation.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtRotation.Leave += new System.EventHandler(this.txtRotation_Leave);
            this.txtRotation.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtRotation_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 169);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Interval (mS)";
            // 
            // txtInterval
            // 
            this.txtInterval.Location = new System.Drawing.Point(111, 166);
            this.txtInterval.Name = "txtInterval";
            this.txtInterval.Size = new System.Drawing.Size(39, 20);
            this.txtInterval.TabIndex = 5;
            this.txtInterval.Text = "1000";
            this.txtInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtInterval.Leave += new System.EventHandler(this.txtInterval_Leave);
            this.txtInterval.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtInterval_KeyPress);
            // 
            // chkAnimate
            // 
            this.chkAnimate.AutoSize = true;
            this.chkAnimate.Location = new System.Drawing.Point(22, 143);
            this.chkAnimate.Name = "chkAnimate";
            this.chkAnimate.Size = new System.Drawing.Size(64, 17);
            this.chkAnimate.TabIndex = 4;
            this.chkAnimate.Text = "Animate";
            this.chkAnimate.UseVisualStyleBackColor = true;
            // 
            // lblScale
            // 
            this.lblScale.AutoSize = true;
            this.lblScale.Location = new System.Drawing.Point(17, 83);
            this.lblScale.Name = "lblScale";
            this.lblScale.Size = new System.Drawing.Size(34, 13);
            this.lblScale.TabIndex = 5;
            this.lblScale.Text = "Scale";
            // 
            // txtScale
            // 
            this.txtScale.Location = new System.Drawing.Point(111, 80);
            this.txtScale.Name = "txtScale";
            this.txtScale.Size = new System.Drawing.Size(39, 20);
            this.txtScale.TabIndex = 2;
            this.txtScale.Text = "1";
            this.txtScale.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtScale.Leave += new System.EventHandler(this.txtScale_Leave);
            this.txtScale.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtScale_KeyPress);
            // 
            // lblOffsetY
            // 
            this.lblOffsetY.AutoSize = true;
            this.lblOffsetY.Location = new System.Drawing.Point(17, 57);
            this.lblOffsetY.Name = "lblOffsetY";
            this.lblOffsetY.Size = new System.Drawing.Size(45, 13);
            this.lblOffsetY.TabIndex = 3;
            this.lblOffsetY.Text = "Offset Y";
            // 
            // txtOffsetY
            // 
            this.txtOffsetY.Location = new System.Drawing.Point(111, 54);
            this.txtOffsetY.Name = "txtOffsetY";
            this.txtOffsetY.Size = new System.Drawing.Size(39, 20);
            this.txtOffsetY.TabIndex = 1;
            this.txtOffsetY.Text = "0";
            this.txtOffsetY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtOffsetY.Leave += new System.EventHandler(this.txtOffsetY_Leave);
            this.txtOffsetY.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOffsetY_KeyPress);
            // 
            // lblOffsetX
            // 
            this.lblOffsetX.AutoSize = true;
            this.lblOffsetX.Location = new System.Drawing.Point(17, 31);
            this.lblOffsetX.Name = "lblOffsetX";
            this.lblOffsetX.Size = new System.Drawing.Size(45, 13);
            this.lblOffsetX.TabIndex = 1;
            this.lblOffsetX.Text = "Offset X";
            // 
            // txtOffsetX
            // 
            this.txtOffsetX.Location = new System.Drawing.Point(111, 28);
            this.txtOffsetX.Name = "txtOffsetX";
            this.txtOffsetX.Size = new System.Drawing.Size(39, 20);
            this.txtOffsetX.TabIndex = 0;
            this.txtOffsetX.Text = "0";
            this.txtOffsetX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtOffsetX.Leave += new System.EventHandler(this.txtOffsetX_Leave);
            this.txtOffsetX.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOffsetX_KeyPress);
            // 
            // frmManualOffsetCalibration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(675, 316);
            this.Controls.Add(this.grpAdjustments);
            this.Controls.Add(this.picAnimation);
            this.Controls.Add(this.picRightImage);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmManualOffsetCalibration";
            this.Text = "Stereo camera calibration tweaks";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmManualOffsetCalibration_FormClosing);
            this.Resize += new System.EventHandler(this.frmManualOffsetCalibration_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRightImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAnimation)).EndInit();
            this.grpAdjustments.ResumeLayout(false);
            this.grpAdjustments.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLeftImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openRightImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveAnimatedGifToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.PictureBox picLeftImage;
        private System.Windows.Forms.PictureBox picRightImage;
        private System.Windows.Forms.Timer timAnimation;
        private System.Windows.Forms.PictureBox picAnimation;
        private System.Windows.Forms.GroupBox grpAdjustments;
        private System.Windows.Forms.Label lblScale;
        private System.Windows.Forms.TextBox txtScale;
        private System.Windows.Forms.Label lblOffsetY;
        private System.Windows.Forms.TextBox txtOffsetY;
        private System.Windows.Forms.Label lblOffsetX;
        private System.Windows.Forms.TextBox txtOffsetX;
        private System.Windows.Forms.CheckBox chkAnimate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtInterval;
        private System.Windows.Forms.Label lblRotation;
        private System.Windows.Forms.TextBox txtRotation;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
    }
}