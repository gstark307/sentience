/*
    base class for drawing graphs
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
using System.IO;
using System.Collections;
using sluggish.utilities;
using System.Drawing;

namespace sluggish.utilities.graph
{
    /// <summary>
    /// base class for drawing graphs
    /// </summary>
    public class graph
    {
        // dimensions of the screen
        public int screen_width, screen_height;

        // thickness of the line drawn on the screen
        public int line_thickness = 1;

        // recent history of measurements
        protected ArrayList history;

        // record the history of measurements
        public bool Recording = true;

        // screen image
        public Byte[] image;

        // value range
        public float max_value_x;
        public float min_value_x;
        public float max_value_y;
        public float min_value_y;

        public byte[] colour = new byte[3];

        /// <summary>
        /// resets the display
        /// </summary>
        public virtual void Reset()
        {
        }

        #region "saving and loading"

        /// <summary>
        /// update the graph image using the measurement history data
        /// </summary>
        protected virtual void updateFromHistory()
        {
        }

        /// <summary>
        /// saves graph points as comma separated variables, so that
        /// it may subsequently be loaded into a spreadsheet
        /// </summary>
        /// <param name="filename"></param>
        public void Save(String filename)
        {
            if (history != null)
            {
                StreamWriter oWrite = null;
                bool allowWrite = true;

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
                    for (int i = 0; i < history.Count; i++)
                    {
                        oWrite.Write((float)history[i]);
                        oWrite.Write(",");
                    }
                    oWrite.Close();
                }
            }
        }

        /// <summary>
        /// loads graph points from a comma separated variable file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
        {
            StreamReader oRead = null;
            bool filefound = true;

            Reset();

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
                String str = oRead.ReadLine();
                if (str != null)
                {
                    String[] values = str.Split(',');
                    for (int i = 0; i < values.Length; i++)
                        if (values[i] != "")
                            history.Add(Convert.ToSingle(values[i]));
                    updateFromHistory();
                }

                oRead.Close();
            }
        }

        #endregion

        #region "adding new measurements"

        /// <summary>
        /// update a time series
        /// </summary>
        /// <param name="measurement">measurement value</param>
        public virtual void Update(float measurement)
        {
        }

        /// <summary>
        /// update from an x,y measurement
        /// </summary>
        /// <param name="measurement_x">measurement value on the x axis</param>
        /// <param name="measurement_y">measurement value on the y axis</param>
        public virtual void Update(float measurement_x, float measurement_y)
        {
        }

        #endregion

        #region "drawing lines/markers on the graph"

        /// <summary>
        /// draws a horizontal line on the graph
        /// </summary>
        /// <param name="y">y coordinate of the line</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="line_width">width of the line</param>
        public void DrawHorizontalLine(float y,
                                       int r, int g, int b, int line_width)
        {
            float offset_y = (max_value_y / (max_value_y - min_value_y)) * screen_height;
            int screen_y = (int)(offset_y + (y * screen_height / (max_value_y - min_value_y)));

            drawing.drawLine(image, screen_width, screen_height,
                             0, screen_y, screen_width - 1, screen_y,
                             r, g, b, line_width, false);
        }

        /// <summary>
        /// draws a vertical line on the graph
        /// </summary>
        /// <param name="x">x coordinate of the line</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="line_width">width of the line</param>
        public void DrawVerticalLine(float x,
                                     int r, int g, int b, int line_width)
        {
            float offset_x = (Math.Abs(min_value_x) / (max_value_x - min_value_x)) * screen_width;
            int screen_x = (int)(offset_x + (x * screen_width / (max_value_x - min_value_x)));

            drawing.drawLine(image, screen_width, screen_height,
                             screen_x, 0, screen_x, screen_height - 1,
                             r, g, b, line_width, false);
        }

        #endregion

        #region "drawing axes"

        /// <summary>
        /// draws axes on the graph
        /// </summary>
        /// <param name="x_increment_minor">minor incremnent along the x axis</param>
        /// <param name="y_increment_minor">minor increment along the y axis</param>
        /// <param name="x_increment_major">major increment along the x axis</param>
        /// <param name="y_increment_major">major increment along the y axis</param>
        /// <param name="increment_marking_size">size of the increment markings</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="lineWidth">line width</param>
        /// <param name="horizontal_scale">name of the horizontal scale</param>
        /// <param name="vertical_scale">name of the vertical scale</param>
        public void DrawAxes(float x_increment_minor, float y_increment_minor,
                             float x_increment_major, float y_increment_major,
                             int increment_marking_size,
                             int r, int g, int b, int lineWidth,
                             bool show_numbers,
                             String horizontal_scale, String vertical_scale)
        {
            // draw the horizontal axis
            float y = screen_height - 1 - (((0 - min_value_y) / (max_value_y - min_value_y)) * screen_height);
            drawing.drawLine(image, screen_width, screen_height,
                             0, (int)y, screen_width - 1, (int)y,
                             r, g, b, lineWidth, false);

            //float x = ((Math.Abs(min_value_x) / (max_value_x - min_value_x)) * screen_width);
            float x = ((0 - min_value_x) / (max_value_x - min_value_x)) * screen_width;

            // show the name of the horizontal axis
            if (horizontal_scale != "")
                AddText(horizontal_scale, "Arial", 10, r, g, b,
                        min_value_x + ((max_value_x - min_value_x) * 0.45f),
                        -(max_value_y - min_value_y) / 20);

            for (int i = 0; i < 2; i++)
            {
                float increment_size = x_increment_minor;
                int marking_size = increment_marking_size;
                if (i > 0)
                {
                    increment_size = x_increment_major;
                    marking_size = (increment_marking_size * 2);
                }

                float xx = 0;
                while (xx < max_value_x)
                {
                    int screen_x = (int)(x + (xx * screen_width / (max_value_x - min_value_x)));
                    drawing.drawLine(image, screen_width, screen_height,
                                     screen_x, (int)y, screen_x, (int)(y + marking_size),
                                     r, g, b, lineWidth, false);
                    if ((show_numbers) && (i > 0) && (xx != 0))
                    {
                        String number_str = ((int)(xx * 100) / 100.0f).ToString();
                        float xx2 = xx - ((max_value_x - min_value_x) / 100);
                        float yy2 = -(max_value_y - min_value_y) / 40;
                        AddText(number_str, "Arial", 8, r, g, b, xx2, yy2);
                    }
                    xx += increment_size;
                }


                xx = 0;
                while (xx > min_value_x)
                {
                    int screen_x = (int)(x + (xx * screen_width / (max_value_x - min_value_x)));
                    drawing.drawLine(image, screen_width, screen_height,
                                     screen_x, (int)y, screen_x, (int)(y + marking_size),
                                     r, g, b, lineWidth, false);
                    if ((show_numbers) && (i > 0) && (xx != 0))
                    {
                        String number_str = ((int)(xx * 100) / 100.0f).ToString();
                        float xx2 = xx - ((max_value_x - min_value_x) / 100);
                        float yy2 = -(max_value_y - min_value_y) / 40;
                        AddText(number_str, "Arial", 8, r, g, b, xx2, yy2);
                    }
                    xx -= increment_size;
                }

            }

            // draw the vertical axis
            x = ((0 - min_value_x) / (max_value_x - min_value_x)) * screen_width;

            //x = (Math.Abs(min_value_x) / (max_value_x - min_value_x)) * screen_width;
            drawing.drawLine(image, screen_width, screen_height,
                             (int)x, 0, (int)x, screen_height - 1,
                             r, g, b, lineWidth, false);

            y = screen_height - 1 - (((0 - min_value_y) / (max_value_y - min_value_y)) * screen_height);

            // show the name of the vertical axis
            if (horizontal_scale != "")
                AddText(vertical_scale, "Arial", 10, r, g, b,
                        (max_value_x - min_value_x) / 150,
                        min_value_y + ((max_value_y - min_value_y) * 0.98f));

            for (int i = 0; i < 2; i++)
            {
                float increment_size = y_increment_minor;
                int marking_size = (int)(increment_marking_size * screen_width / (float)screen_height);
                if (i > 0)
                {
                    increment_size = y_increment_major;
                    marking_size = (int)(increment_marking_size * 2 * screen_width / (float)screen_height);
                }

                float yy = 0;
                while (yy < max_value_y)
                {
                    int screen_y = (int)(screen_height - 1 - (((yy - min_value_y) / (max_value_y - min_value_y)) * screen_height));
                    drawing.drawLine(image, screen_width, screen_height,
                                     (int)x, screen_y, (int)x - marking_size, screen_y,
                                     r, g, b, lineWidth, false);
                    if ((show_numbers) && (i > 0) && (yy != 0))
                    {
                        String number_str = ((int)(yy * 100) / 100.0f).ToString();
                        float yy2 = yy - ((max_value_y - min_value_y) / 200);
                        float xx2 = -(max_value_x - min_value_x) / 20;
                        AddText(number_str, "Arial", 8, r, g, b, xx2, yy2);
                    }
                    yy += increment_size;
                }
                yy = 0;
                while (yy > min_value_y)
                {
                    int screen_y = (int)(screen_height - 1 - (((yy - min_value_y) / (max_value_y - min_value_y)) * screen_height));
                    drawing.drawLine(image, screen_width, screen_height,
                                     (int)x, screen_y, (int)x - marking_size, screen_y,
                                     r, g, b, lineWidth, false);
                    yy -= increment_size;
                }
            }

        }

        #endregion

        #region "drawing text"

        /// <summary>
        /// add some text to the graph at the given position
        /// </summary>
        /// <param name="text">text to be added</param>
        /// <param name="font">font style</param>
        /// <param name="font_size">font size</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="position_x">x coordinate at which to insert the text</param>
        /// <param name="position_y">y coordinate at which to insert the text</param>
        public void AddText(String text,
                            String font, int font_size,
                            int r, int g, int b,
                            float position_x, float position_y)
        {
            // convert from graph coordinates into screen coordinates
            float x = (position_x - min_value_x) * screen_width / (max_value_x - min_value_x);
            float y = screen_height - 1 - ((position_y - min_value_y) * screen_height / (max_value_y - min_value_y));

            sluggish.utilities.drawing.AddText(image, screen_width, screen_height,
                                               text, font, font_size,
                                               r, g, b,
                                               x, y);
        }

        #endregion
    }
}
