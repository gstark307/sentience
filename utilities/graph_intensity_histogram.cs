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

        /// <summary>
        /// create a histogram type graph from the given data
        /// </summary>
        /// <param name="histogram">histogram data</param>
        private void initHistogram(float[] histogram)
        {
            // find the maximum level
            float max_value = 0;
            for (int level = 0; level < histogram.Length; level++)
                if (histogram[level] > max_value) max_value = histogram[level];

            // inflate the maximum slightly
            max_value *= 1.02f;

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
            float x_axis_major_increment = 100;
            float y_axis_minor_increment = 0.1f;
            float y_axis_major_increment = 0.5f;
            DrawAxes(x_axis_minor_increment, y_axis_minor_increment,
                     x_axis_major_increment, y_axis_major_increment,
                     5, 0, 0, 0, 1);
        }

        /// <summary>
        /// create a histogram type graph from the given data and
        /// additionally draw a line indicating the position of a threshold value
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="threshold_value">a threshold value to be displayed on the histogram</param>
        private void initHistogram(float[] histogram,
                                   float threshold_value)
        {
            // draw the histogram
            initHistogram(histogram);

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
        public graph_intensity_histogram(float[] histogram)
            : base(-5, 255, -0.05f, 1)
        {
            initHistogram(histogram);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="threshold_value">a threshold value to be displayed on the histogram</param>
        public graph_intensity_histogram(float[] histogram,
                                         float threshold_value)
            : base(-5, 255, -0.05f, 1)
        {
            initHistogram(histogram, threshold_value);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="screen_width">width of the screen</param>
        /// <param name="screen_height">height of the screen</param>
        public graph_intensity_histogram(float[] histogram,
                                         int screen_width, int screen_height)
            : base(-5, 255, -0.05f, 1, screen_width, screen_height)
        {
            initHistogram(histogram);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="histogram">histogram data</param>
        /// <param name="screen_width">width of the screen</param>
        /// <param name="screen_height">height of the screen</param>
        /// <param name="threshold_value">a threshold value to be displayed on the histogram</param>
        public graph_intensity_histogram(float[] histogram,
                                         int screen_width, int screen_height,
                                         float threshold_value)
            : base(-5, 255, -0.05f, 1, screen_width, screen_height)
        {
            initHistogram(histogram, threshold_value);
        }

        #endregion
    }
}
