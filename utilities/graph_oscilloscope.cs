/*
    Class simulating the output of an graph_oscilloscope
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

namespace sluggish.utilities.graph
{
    /// <summary>
    /// class simulating the output of an graph_oscilloscope
    /// </summary>
    public class graph_oscilloscope : graph
    {
        // current time step
        public int time_step;

        // maximum time in ticks
        public int max_time;

        // previous measurement display coordinates
        private int prev_x, prev_y;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="max_time">maximum time in ticks</param>
        /// <param name="max_value_y">maximum value</param>
        /// <param name="min_value_y">minimum value</param>
        public graph_oscilloscope(int max_time, float min_value_y, float max_value_y)
        {
            this.max_time = max_time;
            this.max_value_y = max_value_y;
            this.min_value_y = min_value_y;
            screen_width = 640;
            screen_height = 480;
            time_step = 0;
            image = new Byte[screen_width * screen_height * 3];
            history = new ArrayList();
            Reset();
        }

        /// <summary>
        /// resets the display
        /// </summary>
        public override void Reset()
        {
            prev_x = -1;
            prev_y = -1;
            for (int i = 0; i < image.Length; i++)
                image[i] = 0;
            time_step = 0;
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
                for (int i = 0; i < history.Count; i++)
                    Update((float)history[i]);
                Recording = prev_recording;
            }
        }


        /// <summary>
        /// draw a line
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="linewidth"></param>
        /// <param name="overwrite"></param>
        private void drawLine(Byte[] img, int img_width, int img_height,
                              int x1, int y1, int x2, int y2, int r, int g, int b, int linewidth, bool overwrite)
        {
            if (img != null)
            {
                int w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
                float m;

                dx = x2 - x1;
                dy = y2 - y1;
                w = Math.Abs(dx);
                h = Math.Abs(dy);
                if (x2 >= x1) step_x = 1; else step_x = -1;
                if (y2 >= y1) step_y = 1; else step_y = -1;

                if (w > h)
                {
                    if (dx != 0)
                    {
                        m = dy / (float)dx;
                        x = x1;
                        int s = 0;
                        while (s * Math.Abs(step_x) <= Math.Abs(dx))
                        {
                            y = (int)(m * (x - x1)) + y1;

                            for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                                for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                                {
                                    if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                    {
                                        int n = ((img_width * yy2) + xx2) * 3;
                                        if ((img[n] == 0) || (!overwrite))
                                        {
                                            img[n] = (Byte)b;
                                            img[n + 1] = (Byte)g;
                                            img[n + 2] = (Byte)r;
                                        }
                                        else
                                        {
                                            img[n] = (Byte)((img[n] + b) / 2);
                                            img[n + 1] = (Byte)((img[n] + g) / 2);
                                            img[n + 2] = (Byte)((img[n] + r) / 2);
                                        }
                                    }
                                }

                            x += step_x;
                            s++;
                        }
                    }
                }
                else
                {
                    if (dy != 0)
                    {
                        m = dx / (float)dy;
                        y = y1;
                        int s = 0;
                        while (s * Math.Abs(step_y) <= Math.Abs(dy))
                        {
                            x = (int)(m * (y - y1)) + x1;
                            for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                                for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                                {
                                    if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                    {
                                        int n = ((img_width * yy2) + xx2) * 3;
                                        if ((img[n] == 0) || (!overwrite))
                                        {
                                            img[n] = (Byte)b;
                                            img[n + 1] = (Byte)g;
                                            img[n + 2] = (Byte)r;
                                        }
                                        else
                                        {
                                            img[n] = (Byte)((img[n] + b) / 2);
                                            img[n + 1] = (Byte)((img[n] + g) / 2);
                                            img[n + 2] = (Byte)((img[n] + r) / 2);
                                        }
                                    }
                                }

                            y += step_y;
                            s++;
                        }
                    }
                }
            }
        }

        #region "adding new measurements"

        /// <summary>
        /// updates the scope with a new measurement
        /// </summary>
        /// <param name="measurement">the value being measured</param>
        public override void Update(float measurement)
        {
            // record values if necessary
            if (Recording) history.Add(measurement);

            // increment the time step
            time_step++;
            if (time_step > max_time)
                Reset();

            // get the screen coordinates
            int x = (int)(time_step * (screen_width - 1) / max_time);
            int y = screen_height - 1 - (int)((measurement - min_value_y) * (screen_height - 1) / (max_value_y - min_value_y));

            if (y < 0) y = 0;
            if (y >= screen_height) y = screen_height - 1;

            // draw the screen output
            if (prev_x > -1)
                drawLine(image, screen_width, screen_height, prev_x, prev_y, x, y, 0, 255, 0, line_thickness, false);

            // store screen coordinates
            prev_x = x;
            prev_y = y;
        }

        #endregion
    }
}
