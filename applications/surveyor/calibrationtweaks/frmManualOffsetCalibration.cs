using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using sluggish.utilities;

namespace calibrationtweaks
{
    public partial class frmManualOffsetCalibration : Form
    {
        private string left_image_filename = "";
        private string right_image_filename = "";
        private string processed_left_image_filename = "";
        private string processed_right_image_filename = "";
        private float offset_x = 0;
        private float offset_y = 0;
        private float scale = 1;
        private float rotation_degrees = 0;
        private string gif_filename = "anim.gif";
        private int delay_mS = 200;
		private bool reverse_colours = false;

        public frmManualOffsetCalibration(
            string left_image_filename,
            string right_image_filename,
            float offset_x,
            float offset_y,
            float scale,
            float rotation_degrees,
            bool parameters_exist,
		    bool reverse_colours)
        {
            InitializeComponent();
			
            this.left_image_filename = left_image_filename;
            this.right_image_filename = right_image_filename;
            this.offset_x = offset_x;
            this.offset_y = offset_y;
            this.scale = scale;
            this.rotation_degrees = rotation_degrees;
			this.reverse_colours = reverse_colours;

            if (!parameters_exist)
			{
				LoadPreviousParameters();
                if (left_image_filename != "") this.left_image_filename = left_image_filename;
                if (right_image_filename != "") this.right_image_filename = right_image_filename;				
			}

            if ((this.left_image_filename != null) &&
                (this.left_image_filename != ""))
            {
				Console.WriteLine(this.left_image_filename);
				Bitmap bmp = (Bitmap)Bitmap.FromFile(this.left_image_filename);
				if (reverse_colours)
				{
					byte[] img = new byte[bmp.Width * bmp.Height * 3];
					BitmapArrayConversions.updatebitmap(bmp, img);
					BitmapArrayConversions.RGB_BGR(img);
					BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
				}
                picLeftImage.Image = bmp;
            }
            if ((this.right_image_filename != null) &&
                (this.right_image_filename != ""))
            {
				Bitmap bmp = (Bitmap)Bitmap.FromFile(this.right_image_filename);
				if (reverse_colours)
				{
					byte[] img = new byte[bmp.Width * bmp.Height * 3];
					BitmapArrayConversions.updatebitmap(bmp, img);
					BitmapArrayConversions.RGB_BGR(img);
					BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
				}
                picRightImage.Image = bmp;
            }

            txtOffsetX.Text = this.offset_x.ToString();
            txtOffsetY.Text = this.offset_y.ToString();
            txtScale.Text = this.scale.ToString();
            txtRotation.Text = this.rotation_degrees.ToString();

            Update();
        }

        private void ResizeForm()
        {
            int border = 10;
            int image_width = this.Width - grpAdjustments.Width - (border*4);            
            int w0 = (image_width - (border*3))/2;

            picLeftImage.Left = border;
            picLeftImage.Top = border + MainMenuStrip.Height;
            picLeftImage.Width = w0;
            picLeftImage.Height = w0 / 2;
            picRightImage.Left = border + w0 + border;
            picRightImage.Top = picLeftImage.Top;
            picRightImage.Width = w0;
            picRightImage.Height = w0 / 2;
            picAnimation.Top = picLeftImage.Top + picLeftImage.Height + border;
            picAnimation.Left = border;
            picAnimation.Width = (w0 * 2) + border;
            picAnimation.Height = picAnimation.Width / 2;
            this.Height = picAnimation.Top + picAnimation.Height + (border * 2) + 30;
            grpAdjustments.Top = picLeftImage.Top;
            grpAdjustments.Left = image_width + border;
        }

        private void Update()
        {
            if ((picLeftImage.Image != null) &&
                (picRightImage.Image != null) &&
                (left_image_filename != "") &&
                (right_image_filename != ""))
            {
                picAnimation.Enabled = false;
                if (picAnimation.Image != null)
                {
                    picAnimation.Image.Dispose();
                    picAnimation.Image = null;
                }
                //System.Threading.Thread.Sleep(2000);                

                if ((processed_left_image_filename != "") &&
                    (processed_left_image_filename != null))
                {
                    if (processed_left_image_filename != left_image_filename)
                    {
                        if (File.Exists(processed_left_image_filename))
                        {
                            File.Delete(processed_left_image_filename);
                            processed_left_image_filename = "";
                        }
                    }
                }
                if ((processed_right_image_filename != "") &&
                    (processed_right_image_filename != null))
                {
                    if (processed_right_image_filename != right_image_filename)
                    {
                        if (File.Exists(processed_right_image_filename))
                        {
                            File.Delete(processed_right_image_filename);
                            processed_right_image_filename = "";
                        }
                    }
                }

                if (delay_mS < 10) delay_mS = 10;

                timAnimation.Interval = delay_mS;

                GifCreator.CreateFromStereoPair(
                    left_image_filename,
                    right_image_filename,
                    gif_filename,
                    delay_mS,
                    offset_x,
                    offset_y,
                    scale,
                    rotation_degrees,
				    reverse_colours,
                    ref processed_left_image_filename,
                    ref processed_right_image_filename);
                ResizeForm();

                picAnimation.Enabled = true;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openLeftImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open_left_image = new OpenFileDialog();

            open_left_image.Title = "Open left camera image";
            open_left_image.Filter = "Bmp files (*.bmp)|*.bmp|Jpeg files (*.jpg)|*.jpg|Gif files (*.gif)|*.gif|PNG files (*.png)|*.png";
            open_left_image.FilterIndex = 1;
            open_left_image.RestoreDirectory = true;

            if (open_left_image.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(open_left_image.FileName))
                {
                    left_image_filename = open_left_image.FileName;
                    picLeftImage.Image = (Bitmap)Bitmap.FromFile(left_image_filename);
                    picLeftImage.Refresh();
                    Update();
                }
            }
        }

        private bool animation_state;

        private void timAnimation_Tick(object sender, EventArgs e)
        {
            if ((File.Exists(processed_left_image_filename)) &&
                (File.Exists(processed_right_image_filename)))
            {
                if (picAnimation.Image != null) picAnimation.Image.Dispose();

                if (animation_state)
				{
				    Bitmap bmp = (Bitmap)Bitmap.FromFile(processed_left_image_filename);
				    if (reverse_colours)
				    {
					    byte[] img = new byte[bmp.Width * bmp.Height * 3];
					    BitmapArrayConversions.updatebitmap(bmp, img);
					    BitmapArrayConversions.RGB_BGR(img);
					    BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
				    }					
                    picAnimation.Image = bmp;
				}
                else
				{
				    Bitmap bmp = (Bitmap)Bitmap.FromFile(processed_right_image_filename);
				    if (reverse_colours)
				    {
					    byte[] img = new byte[bmp.Width * bmp.Height * 3];
					    BitmapArrayConversions.updatebitmap(bmp, img);
					    BitmapArrayConversions.RGB_BGR(img);
					    BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
				    }					
                    picAnimation.Image = bmp;
				}
                picAnimation.Refresh();                
                
                if (chkAnimate.Checked) animation_state = !animation_state;
            }
        }

        private void openRightImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open_right_image = new OpenFileDialog();

            open_right_image.Title = "Open right camera image";
            open_right_image.Filter = "Bmp files (*.bmp)|*.bmp|Jpeg files (*.jpg)|*.jpg|Gif files (*.gif)|*.gif|PNG files (*.png)|*.png";
            open_right_image.FilterIndex = 1;
            open_right_image.RestoreDirectory = true;

            if (open_right_image.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(open_right_image.FileName))
                {
                    right_image_filename = open_right_image.FileName;
                    picRightImage.Image = (Bitmap)Bitmap.FromFile(right_image_filename);
                    picRightImage.Refresh();
                    Update();
                }
            }
        }

        private void SaveAll()
        {
            if (txtOffsetX.Text != "")
            {
                try
                {
                    offset_x = Convert.ToSingle(txtOffsetX.Text);
                    Update();
                }
                catch
                {
                }
            }
            if (txtOffsetY.Text != "")
            {
                try
                {
                    offset_y = Convert.ToSingle(txtOffsetY.Text);
                    Update();
                }
                catch
                {
                }
            }
            if (txtScale.Text != "")
            {
                try
                {
                    scale = Convert.ToSingle(txtScale.Text);
                    Update();
                }
                catch
                {
                }
            }
            if (txtRotation.Text != "")
            {
                try
                {
                    rotation_degrees = Convert.ToSingle(txtRotation.Text);
                    Update();
                }
                catch
                {
                }
            }
        }

        private void txtOffsetX_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtOffsetX.Text != "")
                {
                    try
                    {
                        offset_x = Convert.ToSingle(txtOffsetX.Text);
                        Update();
                    }
                    catch
                    {
                        MessageBox.Show("Invalid entry");
                    }
                }
            }
        }

        private void txtOffsetY_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtOffsetY.Text != "")
                {
                    try
                    {
                        offset_y = Convert.ToSingle(txtOffsetY.Text);
                        Update();
                    }
                    catch
                    {
                        MessageBox.Show("Invalid entry");
                    }
                }
            }
        }

        private void txtScale_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtScale.Text != "")
                {
                    try
                    {
                        scale = Convert.ToSingle(txtScale.Text);
                        Update();
                    }
                    catch
                    {
                        MessageBox.Show("Invalid entry");
                    }
                }
            }
        }

        private void frmManualOffsetCalibration_Resize(object sender, EventArgs e)
        {
            ResizeForm();
        }

        #region "loading and saving parameters"

        private void LoadPreviousParameters()
        {
            StreamReader oRead = null;
            string str;
            bool filefound = true;
            string filename = "manualoffsets_params.txt";

            try
            {
                oRead = File.OpenText(filename);
            }
            catch
            {
                filefound = false;
            }

            if (filefound)
            {
                str = oRead.ReadLine();
                if (str != null)
                {
                    left_image_filename = str;
                }

                str = oRead.ReadLine();
                if (str != null)
                {
                    right_image_filename = str;
                }

                str = oRead.ReadLine();
                if (str != null)
                {
                    offset_x = Convert.ToSingle(str);
                }

                str = oRead.ReadLine();
                if (str != null)
                {
                    offset_y = Convert.ToSingle(str);
                }

                str = oRead.ReadLine();
                if (str != null)
                {
                    scale = Convert.ToSingle(str);
                }

                str = oRead.ReadLine();
                if (str != null)
                {
                    rotation_degrees = Convert.ToSingle(str);
                }

                str = oRead.ReadLine();
                if (str != null)
                {
                    delay_mS = Convert.ToInt32(str);
                }

                oRead.Close();
            }
        }

        private void SaveParameters()
        {
            StreamWriter oWrite = null;
            bool allowWrite = true;
            string filename = "manualoffsets_params.txt";

            try
            {
                oWrite = File.CreateText(filename);
            }
            catch
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                oWrite.WriteLine(left_image_filename);
                oWrite.WriteLine(right_image_filename);
                oWrite.WriteLine(offset_x.ToString());
                oWrite.WriteLine(offset_y.ToString());
                oWrite.WriteLine(scale.ToString());
                oWrite.WriteLine(rotation_degrees.ToString());
                oWrite.WriteLine(delay_mS.ToString());
                oWrite.Close();
            }
        }

        #endregion

        private void frmManualOffsetCalibration_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAll();
            SaveParameters();
        }

        private void txtInterval_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtInterval.Text != "")
                {
                    delay_mS = Convert.ToInt32(txtInterval.Text);
                    timAnimation.Interval = delay_mS;
                }
            }
        }

        private void txtOffsetX_Leave(object sender, EventArgs e)
        {
            if (txtOffsetX.Text != "")
            {
                try
                {
                    offset_x = Convert.ToSingle(txtOffsetX.Text);
                    Update();
                }
                catch
                {
                    MessageBox.Show("Invalid entry");
                }
            }
        }

        private void txtOffsetY_Leave(object sender, EventArgs e)
        {
            if (txtOffsetY.Text != "")
            {
                try
                {
                    offset_y = Convert.ToSingle(txtOffsetY.Text);
                    Update();
                }
                catch
                {
                    MessageBox.Show("Invalid entry");
                }
            }
        }

        private void txtScale_Leave(object sender, EventArgs e)
        {
            if (txtScale.Text != "")
            {
                try
                {
                    scale = Convert.ToSingle(txtScale.Text);
                    Update();
                }
                catch
                {
                    MessageBox.Show("Invalid entry");
                }
            }
        }

        private void txtInterval_Leave(object sender, EventArgs e)
        {
            if (txtInterval.Text != "")
            {
                delay_mS = Convert.ToInt32(txtInterval.Text);
                timAnimation.Interval = delay_mS;
            }
        }

        private void txtRotation_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtRotation.Text != "")
                {
                    try
                    {
                        rotation_degrees = Convert.ToSingle(txtRotation.Text);
                        Update();
                    }
                    catch
                    {
                        MessageBox.Show("Invalid entry");
                    }
                }
            }
        }

        private void txtRotation_Leave(object sender, EventArgs e)
        {
            if (txtRotation.Text != "")
            {
                try
                {
                    rotation_degrees = Convert.ToSingle(txtRotation.Text);
                    Update();
                }
                catch
                {
                    MessageBox.Show("Invalid entry");
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //frmAbout frm = new frmAbout();
            //frm.ShowDialog();
        }

        private void saveAnimatedGifToolStripMenuItem_Click(object sender, EventArgs e)
        {
			/*
			if (reverse_colours)
			{
				reverse_colours = false;
				Update();
				reverse_colours = true;
			}
			else
			{
                Update();
			}
			*/
			
			Update();
            if (File.Exists(gif_filename))
            {
                SaveFileDialog save_gif = new SaveFileDialog();

                save_gif.Title = "Save Animated Gif";
                save_gif.Filter = "Gif files (*.gif)|*.gif";
                save_gif.FilterIndex = 1;
                save_gif.RestoreDirectory = true;

                if (save_gif.ShowDialog() == DialogResult.OK)
                {					
					if (File.Exists(save_gif.FileName))
					{
						File.Delete(save_gif.FileName);
					}
					
                    if (gif_filename != save_gif.FileName)
                        File.Copy(gif_filename, save_gif.FileName);
                }
            }
        }
    }
}
