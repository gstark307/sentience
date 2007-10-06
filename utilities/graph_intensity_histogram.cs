/*
    Class used to plot points within a graph
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
using sluggish.utilities;

namespace sluggish.utilities.graph
{
    public class graph_intensity_histogram : graph_lines
    {
        #region "constructors"

        const float MIN_X = -15;
        const float MAX_X = 255 - (MIN_X / 2);
        const float MIN_Y = -0.1f;
        const float MAX_Y = 1.0f - (MIN_Y / 2);

        /// <summary>
        /// create a histogram type graph from the given data
        /// </summary>
        /// <param name="histogram">histogram data</param>
        private void initHistogram(float[] histogram, String horizontal_scale, String vertical_scale)
        {
            // find the maximum level
            float max_value = 0;
            for (int level = 0; level < histogram.Length; level++)
                if (histogram[level] > max_value) max_value = histogram[level];

            if (max_value > 0)
            {
                for (int level = 0; level < histogram.Length; level++)
                {
                    float x = level * 255 / histogram.Length;
                    float y = histogram[level] / max_value;
                    Update(x, y);
                }
            }

            // draw axes
            float x_axis_minor_increment = 10;
            float x_axis_major_increment = 50;
            float y_axis_minor_increment = 0.25f / 2;
            float y_axis_major_increment = 0.25f;
            DrawAxes(x_axis_minor_increment, y_axis_minor_increment,
                     x_axis_major_increment, y_axis_major_increment,
                     5, 0, 0, 0, 1, true, horizontal_scale, vertical_scale);
        }

        /// <summary>
        /// create a histogram type graph from the given data and
        /// additionally draw a line indicating the position of a threshold value
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="threshold_value">a threshold value to be displayed on the histogram</param>
        private void initHistogram(float[] histogram,
                                   float threshold_value,
                                   String horizontal_scale, String vertical_scale)
        {
            // draw the histogram
            initHistogram(histogram, horizontal_scale, vertical_scale);

            // draw the threshold line
            int screen_x = (int)(threshold_value * screen_width / 255);
            drawing.drawLine(image, screen_width, screen_height,
                             screen_x, 0, screen_x, screen_height - 1,
                             255, 0, 0, 0, false);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        public graph_intensity_histogram(float[] histogram,
                                         String horizontal_scale,
                                         String vertical_scale)
            : base(MIN_X, MAX_X, MIN_Y, MAX_Y)
        {
            initHistogram(histogram, horizontal_scale, vertical_scale);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="threshold_value">a threshold value to be displayed on the histogram</param>
        public graph_intensity_histogram(float[] histogram,
                                         float threshold_value,
                                         String horizontal_scale,
                                         String vertical_scale)
            : base(MIN_X, MAX_X, MIN_Y, MAX_Y)
        {
            initHistogram(histogram, threshold_value, horizontal_scale, vertical_scale);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="screen_width">width of the screen</param>
        /// <param name="screen_height">height of the screen</param>
        /// <param name="horizontal_scale">name of the horizontal scale</param>
        /// <param name="vertical_scale">name of the vertical scale</param>
        public graph_intensity_histogram(float[] histogram,
                                         int screen_width, int screen_height,
                                         String horizontal_scale,
                                         String vertical_scale)
            : base(MIN_X, MAX_X, MIN_Y, MAX_Y, screen_width, screen_height)
        {
            initHistogram(histogram, horizontal_scale, vertical_scale);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="screen_width">width of the screen</param>
        /// <param name="screen_height">height of the screen</param>
        /// <param name="threshold_value">a threshold value to be displayed on the histogram</param>
        /// <param name="horizontal_scale">name of the horizontal scale</param>
        /// <param name="vertical_scale">name of the vertical scale</param>
        public graph_intensity_histogram(float[] histogram,
                                         int screen_width, int screen_height,
                                         float threshold_value,
                                         String horizontal_scale,
                                         String vertical_scale)
            : base(MIN_X, MAX_X, MIN_Y, MAX_Y, screen_width, screen_height)
        {
            initHistogram(histogram, threshold_value, horizontal_scale, vertical_scale);
        }

        #endregion
    }
}
