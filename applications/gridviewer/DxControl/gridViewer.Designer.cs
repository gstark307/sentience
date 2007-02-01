namespace DirectXSample
{
	partial class frmGridViewer
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
            this.label1 = new System.Windows.Forms.Label();
            this.buttonAutoSize = new System.Windows.Forms.Button();
            this.buttonScreenParams = new System.Windows.Forms.Button();
            this.mD3d = new Gosub.Direct3d();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gridToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdZoomIn = new System.Windows.Forms.Button();
            this.cmdZoomOut = new System.Windows.Forms.Button();
            this.cmdPanLeft = new System.Windows.Forms.Button();
            this.cmdPanRight = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 323);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(244, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Press F1 to toggle full screen";
            // 
            // buttonAutoSize
            // 
            this.buttonAutoSize.Location = new System.Drawing.Point(313, 320);
            this.buttonAutoSize.Name = "buttonAutoSize";
            this.buttonAutoSize.Size = new System.Drawing.Size(64, 23);
            this.buttonAutoSize.TabIndex = 2;
            this.buttonAutoSize.Text = "Auto Size";
            this.buttonAutoSize.Click += new System.EventHandler(this.buttonAutoSize_Click);
            // 
            // buttonScreenParams
            // 
            this.buttonScreenParams.Location = new System.Drawing.Point(377, 320);
            this.buttonScreenParams.Name = "buttonScreenParams";
            this.buttonScreenParams.Size = new System.Drawing.Size(56, 23);
            this.buttonScreenParams.TabIndex = 3;
            this.buttonScreenParams.Text = "Screen";
            this.buttonScreenParams.Click += new System.EventHandler(this.buttonScreenParams_Click);
            // 
            // mD3d
            // 
            this.mD3d.BackColor = System.Drawing.Color.Black;
            this.mD3d.DxAutoResize = false;
            this.mD3d.DxFullScreen = false;
            this.mD3d.DxShowCursor = true;
            this.mD3d.Location = new System.Drawing.Point(9, 31);
            this.mD3d.Name = "mD3d";
            this.mD3d.Size = new System.Drawing.Size(424, 289);
            this.mD3d.TabIndex = 0;
            this.mD3d.DxRender3d += new Gosub.Direct3d.DxDirect3dDelegate(this.mD3d_DxRender3d);
            this.mD3d.DxLoaded += new Gosub.Direct3d.DxDirect3dDelegate(this.mD3d_DxLoaded);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "GRD";
            this.openFileDialog1.Title = "Open Grid file";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(441, 24);
            this.menuStrip1.TabIndex = 7;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gridToolStripMenuItem});
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(111, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // gridToolStripMenuItem
            // 
            this.gridToolStripMenuItem.Name = "gridToolStripMenuItem";
            this.gridToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.gridToolStripMenuItem.Text = "Sentience Grid";
            this.gridToolStripMenuItem.Click += new System.EventHandler(this.gridToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(111, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // cmdZoomIn
            // 
            this.cmdZoomIn.Location = new System.Drawing.Point(323, 361);
            this.cmdZoomIn.Name = "cmdZoomIn";
            this.cmdZoomIn.Size = new System.Drawing.Size(24, 24);
            this.cmdZoomIn.TabIndex = 8;
            this.cmdZoomIn.Text = "+";
            this.cmdZoomIn.UseVisualStyleBackColor = true;
            this.cmdZoomIn.Click += new System.EventHandler(this.cmdZoomIn_Click);
            // 
            // cmdZoomOut
            // 
            this.cmdZoomOut.Location = new System.Drawing.Point(353, 361);
            this.cmdZoomOut.Name = "cmdZoomOut";
            this.cmdZoomOut.Size = new System.Drawing.Size(24, 24);
            this.cmdZoomOut.TabIndex = 9;
            this.cmdZoomOut.Text = "-";
            this.cmdZoomOut.UseVisualStyleBackColor = true;
            this.cmdZoomOut.Click += new System.EventHandler(this.cmdZoomOut_Click);
            // 
            // cmdPanLeft
            // 
            this.cmdPanLeft.Location = new System.Drawing.Point(114, 361);
            this.cmdPanLeft.Name = "cmdPanLeft";
            this.cmdPanLeft.Size = new System.Drawing.Size(24, 24);
            this.cmdPanLeft.TabIndex = 10;
            this.cmdPanLeft.Text = "<";
            this.cmdPanLeft.UseVisualStyleBackColor = true;
            this.cmdPanLeft.Click += new System.EventHandler(this.cmdPanLeft_Click);
            // 
            // cmdPanRight
            // 
            this.cmdPanRight.Location = new System.Drawing.Point(144, 361);
            this.cmdPanRight.Name = "cmdPanRight";
            this.cmdPanRight.Size = new System.Drawing.Size(24, 24);
            this.cmdPanRight.TabIndex = 11;
            this.cmdPanRight.Text = ">";
            this.cmdPanRight.UseVisualStyleBackColor = true;
            this.cmdPanRight.Click += new System.EventHandler(this.cmdPanRight_Click);
            // 
            // frmGridViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(441, 394);
            this.Controls.Add(this.cmdPanRight);
            this.Controls.Add(this.cmdPanLeft);
            this.Controls.Add(this.cmdZoomOut);
            this.Controls.Add(this.cmdZoomIn);
            this.Controls.Add(this.buttonScreenParams);
            this.Controls.Add(this.buttonAutoSize);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.mD3d);
            this.Controls.Add(this.menuStrip1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmGridViewer";
            this.Text = "Occupancy Grid Viewer";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SampleForm_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonAutoSize;
        private System.Windows.Forms.Button buttonScreenParams;
        private Gosub.Direct3d mD3d;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Button cmdZoomIn;
        private System.Windows.Forms.Button cmdZoomOut;
        private System.Windows.Forms.Button cmdPanLeft;
        private System.Windows.Forms.Button cmdPanRight;
        private System.Windows.Forms.ToolStripMenuItem gridToolStripMenuItem;


	}
}

