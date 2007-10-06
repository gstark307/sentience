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
            this.picRays = new System.Windows.Forms.PictureBox();
            this.txtMappingTime = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPositionError = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtAngularError = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gaussianFunctionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.singleRayModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.multipleStereoRaysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pathPlanningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.motionModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.picRays)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // picRays
            // 
            this.picRays.Location = new System.Drawing.Point(15, 46);
            this.picRays.Name = "picRays";
            this.picRays.Size = new System.Drawing.Size(769, 845);
            this.picRays.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRays.TabIndex = 0;
            this.picRays.TabStop = false;
            // 
            // txtMappingTime
            // 
            this.txtMappingTime.Location = new System.Drawing.Point(103, 897);
            this.txtMappingTime.Name = "txtMappingTime";
            this.txtMappingTime.Size = new System.Drawing.Size(63, 20);
            this.txtMappingTime.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 897);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Mapping time";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(191, 897);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Position Error (mm)";
            // 
            // txtPositionError
            // 
            this.txtPositionError.Location = new System.Drawing.Point(291, 897);
            this.txtPositionError.Name = "txtPositionError";
            this.txtPositionError.Size = new System.Drawing.Size(63, 20);
            this.txtPositionError.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(377, 897);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Angular Error (degrees)";
            // 
            // txtAngularError
            // 
            this.txtAngularError.Location = new System.Drawing.Point(498, 897);
            this.txtAngularError.Name = "txtAngularError";
            this.txtAngularError.Size = new System.Drawing.Size(63, 20);
            this.txtAngularError.TabIndex = 5;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(791, 24);
            this.menuStrip1.TabIndex = 7;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gaussianFunctionToolStripMenuItem,
            this.singleRayModelToolStripMenuItem,
            this.multipleStereoRaysToolStripMenuItem,
            this.pathPlanningToolStripMenuItem,
            this.motionModelToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // gaussianFunctionToolStripMenuItem
            // 
            this.gaussianFunctionToolStripMenuItem.Name = "gaussianFunctionToolStripMenuItem";
            this.gaussianFunctionToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.gaussianFunctionToolStripMenuItem.Text = "Gaussian function";
            this.gaussianFunctionToolStripMenuItem.Click += new System.EventHandler(this.gaussianFunctionToolStripMenuItem_Click);
            // 
            // singleRayModelToolStripMenuItem
            // 
            this.singleRayModelToolStripMenuItem.Name = "singleRayModelToolStripMenuItem";
            this.singleRayModelToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.singleRayModelToolStripMenuItem.Text = "Single Ray Model";
            this.singleRayModelToolStripMenuItem.Click += new System.EventHandler(this.singleRayModelToolStripMenuItem_Click);
            // 
            // multipleStereoRaysToolStripMenuItem
            // 
            this.multipleStereoRaysToolStripMenuItem.Name = "multipleStereoRaysToolStripMenuItem";
            this.multipleStereoRaysToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.multipleStereoRaysToolStripMenuItem.Text = "Multiple Stereo Rays";
            this.multipleStereoRaysToolStripMenuItem.Click += new System.EventHandler(this.multipleStereoRaysToolStripMenuItem_Click);
            // 
            // pathPlanningToolStripMenuItem
            // 
            this.pathPlanningToolStripMenuItem.Name = "pathPlanningToolStripMenuItem";
            this.pathPlanningToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.pathPlanningToolStripMenuItem.Text = "Path Planning";
            this.pathPlanningToolStripMenuItem.Click += new System.EventHandler(this.pathPlanningToolStripMenuItem_Click);
            // 
            // motionModelToolStripMenuItem
            // 
            this.motionModelToolStripMenuItem.Name = "motionModelToolStripMenuItem";
            this.motionModelToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.motionModelToolStripMenuItem.Text = "Motion Model";
            this.motionModelToolStripMenuItem.Click += new System.EventHandler(this.motionModelToolStripMenuItem_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(791, 884);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtAngularError);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtPositionError);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtMappingTime);
            this.Controls.Add(this.picRays);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Sentience test";
            ((System.ComponentModel.ISupportInitialize)(this.picRays)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picRays;
        private System.Windows.Forms.TextBox txtMappingTime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPositionError;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtAngularError;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gaussianFunctionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem singleRayModelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem multipleStereoRaysToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pathPlanningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem motionModelToolStripMenuItem;
    }
}

