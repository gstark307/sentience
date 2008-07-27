using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using surveyor.vision;

namespace surveyorstereo
{
    public partial class frmStereo : Form
    {
        SurveyorVisionStereoWin stereo_camera;

        public frmStereo()
        {
            InitializeComponent();

            stereo_camera = new SurveyorVisionStereoWin("169.254.0.10", 10001, 10002);
            stereo_camera.window = this;
            stereo_camera.display_image[0] = picLeftImage;
            stereo_camera.display_image[1] = picRightImage;
            stereo_camera.Run();
        }

        private void frmStereo_FormClosing(object sender, FormClosingEventArgs e)
        {
            stereo_camera.Stop();
        }
    }
}