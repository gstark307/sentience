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
using System.Collections;

namespace sentience.calibration
{
    /// <summary>
    /// a class used to plot points within a graph
    /// </summary>
    public class graph_points : graph
    {
        #region "constructors"

        /// <summary>
        /// initialise the graph screen image with the given dimensions
        /// </summary>
        /// <param name="screen_width">width of the screen in pixels</param>
        /// <param name="screen_height">height of the screen in pixels</param>
        protected void initialiseGraph(int screen_width, int screen_height)
        {
            this.screen_width = screen_width;
            this.screen_height = screen_height;
            image = new Byte[screen_width * screen_height * 3];
            history = new ArrayList();
            Reset();
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="min_value_x">minimum x value</param>
        /// <param name="max_value_x">maximum x value</param>
        /// <param name="min_value_y">minimum y value</param>
        /// <param name="max_value_y">maximum y value</param>
        public graph_points(float min_value_x, float max_value_x,
                            float min_value_y, float max_value_y)
        {
            this.max_value_x = max_value_x;
            this.min_value_x = min_value_x;
            this.max_value_y = max_value_y;
            this.min_value_y = min_value_y;
            initialiseGraph(640, 480);
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
        public graph_points(float min_value_x, float max_value_x,
                            float min_value_y, float max_value_y,
                            int screen_width, int screen_height)
        {
            this.max_value_x = max_value_x;
            this.min_value_x = min_value_x;
            this.max_value_y = max_value_y;
            this.min_value_y = min_value_y;
            initialiseGraph(screen_width, screen_height);
        }

        #endregion

        /// <summary>
        /// resets the display
        /// </summary>
        public override void Reset()
        {
            for (int i = 0; i < image.Length; i++)
                image[i] = 255;
            history = new ArrayList();
        }

        /// <summary>
        /// update the graph image using the measurement history data
        /// </summary>
        protected override void updateFromHistory()
        {
            if (history != null)
            {
                bool prev_recording = Recording;
                Recording = false;
                for (int i = 0; i < history.Count; i += 2)
                {
                    float x = (float)history[i];
                    float y = (float)history[i + 1];
                    Update(x, y);
                }
                Recording = prev_recording;
            }
        }


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

            // get the screen coordinates
            int x = (int)((measurement_x - min_value_x) * (screen_width - 1) / (max_value_x - min_value_x));
            int y = screen_height - 1 - (int)((measurement_y - min_value_y) * (screen_height - 1) / (max_value_y - min_value_y));

            if (x < line_thickness) x = line_thickness;
            if (x >= screen_width - line_thickness) x = screen_width - 1 - line_thickness;
            if (y < line_thickness) y = line_thickness;
            if (y >= screen_height - line_thickness) y = screen_height - 1 - line_thickness;

            // show the point
            for (int xx = x - line_thickness; xx <= x + line_thickness; xx++)
            {
                for (int yy = y - line_thickness; yy <= y + line_thickness; yy++)
                {
                    int n = ((yy * screen_width) + xx) * 3;
                    for (int col = 0; col < 3; col++)
                        image[n + col] = 0;
                }
            }
        }

        #endregion
    }
}
