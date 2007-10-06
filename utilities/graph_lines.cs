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
    public class graph_lines : graph_points
    {
        // previous measurements
        float prev_measurement_x = 9999, prev_measurement_y;

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="min_value_x">minimum x value</param>
        /// <param name="max_value_x">maximum x value</param>
        /// <param name="min_value_y">minimum y value</param>
        /// <param name="max_value_y">maximum y value</param>
        public graph_lines(float min_value_x, float max_value_x,
                           float min_value_y, float max_value_y)
            :
            base(min_value_x, max_value_x, min_value_y, max_value_y)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="min_value_x">minimum x value</param>
        /// <param name="max_value_x">maximum x value</param>
        /// <param name="min_value_y">minimum y value</param>
        /// <param name="max_value_y">maximum y value</param>
        /// <param name="screen_width">width of the screen</param>
        /// <param name="screen_height">height of the screen</param>
        public graph_lines(float min_value_x, float max_value_x,
                           float min_value_y, float max_value_y,
                           int screen_width, int screen_height)
            :
            base(min_value_x, max_value_x, min_value_y, max_value_y, screen_width, screen_height)
        {
        }

        #endregion


        #region "adding new measurements"

        /// <summary>
        /// update from an x,y measurement
        /// </summary>
        /// <param name="measurement_x"></param>
        /// <param name="measurement_y"></param>
        public override void Update(float measurement_x, float measurement_y)
        {
            // update the graph history
            if (Recording)
            {
                history.Add(measurement_x);
                history.Add(measurement_y);
            }

            if (prev_measurement_x != 9999)
            {
                // get the previous screen coordinates
                int prev_x = (int)((prev_measurement_x - min_value_x) * (screen_width - 1) / (max_value_x - min_value_x));
                int prev_y = screen_height - 1 - (int)((prev_measurement_y - min_value_y) * (screen_height - 1) / (max_value_y - min_value_y));

                // get the current screen coordinates
                int x = (int)((measurement_x - min_value_x) * (screen_width - 1) / (max_value_x - min_value_x));
                int y = screen_height - 1 - (int)((measurement_y - min_value_y) * (screen_height - 1) / (max_value_y - min_value_y));

                // draw a line from the previous measurement to the current one
                drawing.drawLine(image, screen_width, screen_height,
                                 prev_x, prev_y, x, y,
                                 colour[0], colour[1], colour[2], 0, false);
            }

            // record previous measurements
            prev_measurement_x = measurement_x;
            prev_measurement_y = measurement_y;
        }

        #endregion
    }
}
