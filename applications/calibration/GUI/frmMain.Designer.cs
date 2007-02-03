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
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem1,
            this.videoToolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(757, 24);
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
            this.picOutput1.Location = new System.Drawing.Point(12, 27);
            this.picOutput1.Name = "picOutput1";
            this.picOutput1.Size = new System.Drawing.Size(733, 418);
            this.picOutput1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOutput1.TabIndex = 3;
            this.picOutput1.TabStop = false;
            // 
            // picLeftImage
            // 
            this.picLeftImage.Location = new System.Drawing.Point(500, 27);
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
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(757, 457);
            this.Controls.Add(this.picOutput1);
            this.Controls.Add(this.picLeftImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Sluggish Software:  Camera Calibration";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutput1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLeftImage)).EndInit();
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
    }
}

