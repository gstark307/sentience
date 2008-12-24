using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace surveyor.vision
{
    public partial class FormStereo : Form
    {
        /// <summary>
        /// updates left image
        /// </summary>
        /// <param name="left"></param>
        public virtual void UpdateLeftImage(PictureBox left)
        {
        }

        /// <summary>
        /// updates right image
        /// </summary>
        /// <param name="left"></param>
        public virtual void UpdateRightImage(PictureBox right)
        {
        }

    }
}
