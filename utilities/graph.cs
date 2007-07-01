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
    }
}
