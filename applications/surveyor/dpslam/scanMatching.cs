/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Text;

namespace dpslam.core
{
    /// <summary>
    /// A class used to carry out a visual equivalent of scan matching.
    /// This may be used to reduce the uncertainties used within the motion model
    /// </summary>
    public class scanMatching
    {
        // a value returned if the images could not be matched
        public const float NOT_MATCHED = 999999;

        // the previous image
        private byte[] prev_img = null;

        // determines the number of samples tested for each possible pose change
        // in the range 0-100.  Higher figures give corser sampling.
        public int sampling_step = 10;

        // maximum allowable difference between images in order
        // for this to be considered a match, in the range 0-100
        public int max_difference = 10;

        
        public float pan_angle_change = NOT_MATCHED;

        /// <summary>
        /// returns the estimated change in pan angle of the robot
        /// for a forward or rearward looking camera
        /// </summary>
        /// <param name="img">mono camera image</param>
        /// <param name="width">width of the image</param>
        /// <param name="height">height of the image</param>
        /// <param name="FOV_radians">horizontal field of vision in radians</param>
        /// <param name="camera_roll_radians">camera roll angle in radians</param>
        /// <param name="max_angle_change_radians">the maximum detectable change in pan angle per update in radians</param>
        public void update(
		    byte[] img, int width, int height, 
            float FOV_radians, 
            float camera_roll_radians,
            float max_angle_change_radians)
        {
            pan_angle_change = NOT_MATCHED;

            if ((prev_img != null) && (max_angle_change_radians < FOV_radians/3))
            {
                int min_diff = int.MaxValue;
                int min_hits = 0;
                int x_border = (int)(width * max_angle_change_radians / FOV_radians);
                int y_border = (int)(height * max_angle_change_radians / FOV_radians);
                float dx = 0;

                int step_size = sampling_step * width / 100;
                if (step_size < 1) step_size = 1;

                // find the offset which gives the minimum difference
                // between the two images
                for (int x_offset = -x_border; x_offset < x_border; x_offset++)
                {
                    for (int y_offset = -y_border; y_offset <= y_border; y_offset++)
                    {
                        int hits = 0;
                        int total_diff = 0;
                        for (int x = x_border; x < width - x_border; x += step_size)
                        {
                            for (int y = y_border; y < height - y_border; y += step_size)
                            {
                                int n1 = (y * width) + x;
                                int n2 = ((y + y_offset) * width) + (x + x_offset);
                                total_diff += Math.Abs(img[n1] - prev_img[n2]);
                                hits++;
                            }
                        }
                        if (total_diff < min_diff)
                        {
                            dx = x_offset;
                            min_diff = total_diff;
                            min_hits = hits;
                        }
                    }
                }
                // average the difference value, to get a difference per pixel
                if (min_hits > 0) min_diff /= min_hits;

                // if the camera is rolled perform a compensation such that dx
                // is the horizontal change in non-egocentric coordinates
                dx *= (float)Math.Cos(camera_roll_radians);

                // is the difference below a minimum threshold?
                // if the difference is too large then consider the two images to be different
                // (for example, the robot turned through a large angle or a large object
                // came into view)
                if (min_diff * 100 / 255 < max_difference)
                {
                    pan_angle_change = dx * FOV_radians / width;
                }
            }

            // copy the image
            if (prev_img == null) prev_img = new Byte[width * height];
            for (int i = 0; i < width * height; i++)
                prev_img[i] = img[i];
        }
    }
}
