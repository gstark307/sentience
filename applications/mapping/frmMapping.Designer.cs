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
            this.cmdMap = new System.Windows.Forms.Button();
            this.cmdShowFeatures = new System.Windows.Forms.Button();
            this.timAnimate = new System.Windows.Forms.Timer(this.components);
            this.lblPositionIndex = new System.Windows.Forms.Label();
            this.picDepthMap = new System.Windows.Forms.PictureBox();
            this.picRays = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDepthMap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRays)).BeginInit();
            this.SuspendLayout();
            // 
            // picGrid
            // 
            this.picGrid.Location = new System.Drawing.Point(12, 12);
            this.picGrid.Name = "picGrid";
            this.picGrid.Size = new System.Drawing.Size(209, 162);
            this.picGrid.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picGrid.TabIndex = 0;
            this.picGrid.TabStop = false;
            // 
            // cmdMap
            // 
            this.cmdMap.Location = new System.Drawing.Point(405, 16);
            this.cmdMap.Name = "cmdMap";
            this.cmdMap.Size = new System.Drawing.Size(94, 31);
            this.cmdMap.TabIndex = 1;
            this.cmdMap.Text = "Create Map";
            this.cmdMap.UseVisualStyleBackColor = true;
            this.cmdMap.Click += new System.EventHandler(this.cmdMap_Click);
            // 
            // cmdShowFeatures
            // 
            this.cmdShowFeatures.Location = new System.Drawing.Point(405, 53);
            this.cmdShowFeatures.Name = "cmdShowFeatures";
            this.cmdShowFeatures.Size = new System.Drawing.Size(94, 31);
            this.cmdShowFeatures.TabIndex = 2;
            this.cmdShowFeatures.Text = "Show Features";
            this.cmdShowFeatures.UseVisualStyleBackColor = true;
            this.cmdShowFeatures.Click += new System.EventHandler(this.cmdShowFeatures_Click);
            // 
            // timAnimate
            // 
            this.timAnimate.Enabled = true;
            this.timAnimate.Interval = 500;
            this.timAnimate.Tick += new System.EventHandler(this.timAnimate_Tick);
            // 
            // lblPositionIndex
            // 
            this.lblPositionIndex.AutoSize = true;
            this.lblPositionIndex.Location = new System.Drawing.Point(357, 12);
            this.lblPositionIndex.Name = "lblPositionIndex";
            this.lblPositionIndex.Size = new System.Drawing.Size(0, 13);
            this.lblPositionIndex.TabIndex = 3;
            // 
            // picDepthMap
            // 
            this.picDepthMap.Location = new System.Drawing.Point(12, 190);
            this.picDepthMap.Name = "picDepthMap";
            this.picDepthMap.Size = new System.Drawing.Size(209, 162);
            this.picDepthMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picDepthMap.TabIndex = 4;
            this.picDepthMap.TabStop = false;
            // 
            // picRays
            // 
            this.picRays.Location = new System.Drawing.Point(242, 107);
            this.picRays.Name = "picRays";
            this.picRays.Size = new System.Drawing.Size(209, 162);
            this.picRays.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRays.TabIndex = 5;
            this.picRays.TabStop = false;
            // 
            // frmMapping
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 376);
            this.Controls.Add(this.picRays);
            this.Controls.Add(this.picDepthMap);
            this.Controls.Add(this.lblPositionIndex);
            this.Controls.Add(this.cmdShowFeatures);
            this.Controls.Add(this.cmdMap);
            this.Controls.Add(this.picGrid);
            this.Name = "frmMapping";
            this.Text = "Sentience Mapping";
            this.Load += new System.EventHandler(this.frmMapping_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDepthMap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRays)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picGrid;
        private System.Windows.Forms.Button cmdMap;
        private System.Windows.Forms.Button cmdShowFeatures;
        private System.Windows.Forms.Timer timAnimate;
        private System.Windows.Forms.Label lblPositionIndex;
        private System.Windows.Forms.PictureBox picDepthMap;
        private System.Windows.Forms.PictureBox picRays;
    }
}

