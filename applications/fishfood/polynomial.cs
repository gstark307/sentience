/*
    Polynomial curve fitting
    Adapted from a visual basic algorithm originally written by Frank Schindler, 16.9.2000
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
using System.Collections.Generic;

namespace sentience.calibration
{
    public class polynomial
    {
        private const int MaxO = 25; // max polynomial degree
        private int GlobalO;         // degree of the polynomial expected
        private bool Finished;

        private double[] SumX;
        private double[] SumYX;
        private double[,] M;
        private double[] C; // coefficients

        private List<double> Xpoints;
        private List<double> Ypoints;

        public polynomial()
        {
            SumX = new double[(2 * MaxO) + 1];
            SumYX = new double[MaxO + 1];
            M = new double[MaxO, MaxO + 1];
            C = new double[MaxO + 1]; // coefficients in: Y = C(0)*X^0 + C(1)*X^1 + C(2)*X^2 + ...

            Init();
            GlobalO = 4;
        }

        /// <summary>
        /// gauss algorithm implementation,
        /// following R.Sedgewick's "Algorithms in C", Addison-Wesley, with minor modifications
        /// </summary>
        /// <param name="?"></param>
        private void GaussSolve(int O)
        {
            int i, j, k, iMax;
            double T, O1;
  
            O1 = O + 1;
            // first triangulize the matrix
            for (i = 0; i <= O; i++)
            {
                iMax = i;
                T = Math.Abs(M[iMax, i]);
                for (j = i + 1; j <= O; j++)
                {
                    //find the line with the largest absvalue in this row
                    if (T < Math.Abs(M[j, i]))
                    {
                        iMax = j;
                        T = Math.Abs(M[iMax, i]);
                    }
                }
                if (i < iMax) // exchange the two lines
                {
                    for (k = i; k <= O1; k++)
                    {
                        T = M[i, k];
                        M[i, k] = M[iMax, k];
                        M[iMax, k] = T;
                    }
                }
                for (j = i + 1; j <= O; j++) // scale all following lines to have a leading zero
                {
                    T = M[j, i] / M[i, i];
                    M[j, i] = 0.0;
                    for (k = i + 1; k <= O1; k++)
                    {
                        M[j, k] = M[j, k] - M[i, k] * T;
                    }
                }
            }
            // then substitute the coefficients
            for (j = O; j >= 0; j--)
            {
                T = M[j, (int)O1];
                for (k = j + 1; k <= O; k++)
                {
                    T = T - M[j, k] * C[k];
                }
                C[j] = T / M[j, j];
            }
            Finished = true;
        }

        private void BuildMatrix(int O)
        {
            int i, k, O1;
            
            O1 = O + 1;
            for (i = 0; i<= O; i++)
            {
                for (k = 0; k <= O; k++)
                {
                    M[i, k] = SumX[i + k];
                }
                M[i, O1] = SumYX[i];
            }
        }

        private void FinalizeMatrix(int O)
        {
            int i, O1;
            
            O1 = O + 1;
            for (i = 0; i <= O; i++)
            {
                M[i, O1] = SumYX[i];
            }
        }

        public void Solve()
        {
            int O;
     
            O = GlobalO;
            if (XYCount() <= O)
                O = XYCount() - 1;

            if (O >= 0)
            {
                BuildMatrix(O);
                
                try
                {
                    GaussSolve(O);
                    while (1 < O)
                    {      
                        C[0] = 0.0f;
                        O = O - 1;
                        FinalizeMatrix(O);
                    }
                }
                catch
                {
                }
            }
        }


        public void Init()
        {
            int i;

            Xpoints = new List<double>();
            Ypoints = new List<double>();

            Finished = false;
            for (i = 0; i <= MaxO; i++)
            {
                SumX[i] = 0;
                SumX[i + MaxO] = 0;
                SumYX[i] = 0;
                C[i] = 0;
            }
        }

        public void SetCoeff(int Exponent, double value)
        {
            Finished = true;
            C[Exponent] = value;
        }

        public double Coeff(int Exponent)
        {
            int Ex, O;

            if (!Finished) Solve();
            Ex = Math.Abs(Exponent);
            O = GlobalO;
            //if (XYCount() <= O) O = XYCount() - 1;
            if (O < Ex) 
                return(0); 
            else 
                return(C[Ex]);
        }

        public int GetDegree()
        {
            return(GlobalO);
        }

        public void SetDegree(int NewVal)
        {
            if (!((NewVal < 0) || (MaxO < NewVal)))
            {
                Init();
                GlobalO = NewVal;
            }
        }

        public int XYCount()
        {
            return((int)(SumX[0]));
        }

        public void AddPoint(double x, double y)
        {
            int i, Max2O;
            double TX;

            Xpoints.Add(x);
            Ypoints.Add(y);

            Finished = false;
            Max2O = 2 * GlobalO;
            TX = 1;
            SumX[0] = SumX[0] + 1;
            SumYX[0] = SumYX[0] + y;
            for (i = 1; i <= GlobalO; i++)
            {
                TX = TX * x;
                SumX[i] = SumX[i] + TX;
                SumYX[i] = SumYX[i] + y * TX;
            }
            for (i = GlobalO + 1; i <= Max2O; i++)
            {
                TX = TX * x;
                SumX[i] = SumX[i] + TX;
            }
        }

        public double RegVal(double x)
        {
            int i, O;
            double retval=0;

            if (!Finished) Solve();
            O = GlobalO;
            //if (XYCount() <= O) O = XYCount() - 1;
            for (i = 0; i <= O; i++)
                retval = retval + C[i] * Math.Pow(x, i);
            return (retval);
        }


        public double GetRMSerror()
        {
            return (Math.Sqrt(GetSquaredError()));
        }


        public double GetSquaredError()
        {
            double error = 0;

            if (Xpoints.Count > 0)
            {
                for (int i = 0; i < Xpoints.Count; i++)
                {
                    double x = Xpoints[i];
                    double y = Ypoints[i];
                    double y2 = RegVal(x);
                    double diff = y - y2;
                    error += (diff * diff);
                }
                error /= Xpoints.Count;
            }
            return (error);
        }

        public double GetMeanError()
        {
            double error = 0;

            if (Xpoints.Count > 0)
            {
                for (int i = 0; i < Xpoints.Count; i++)
                {
                    double x = Xpoints[i];
                    double y = Ypoints[i];
                    double y2 = RegVal(x);
                    double diff = y - y2;
                    if (diff < 0) diff = -diff;
                    error += diff;
                }
                error /= Xpoints.Count;
            }
            return (error);
        }

        private double[] RunningAverage(int steps)
        {
            double min_x = 0;
            double min_y = 0;
            double max_x = 1;
            double max_y = 1;
            int i = 0;

            double[] mean_x = new double[steps];
            int [] hits_x = new int[steps];
            double[] mean_y = new double[steps];
            int[] hits_y = new int[steps];

            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                if (x > max_x) max_x = x;
                if (y > max_y) max_y = y;
                if (x < min_x) min_x = x;
                if (y < min_y) min_y = y;
            }

            double x_range = max_x - min_x;
            double y_range = max_y - min_y;

            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                int bucket_x = (int)((x - min_x) * (steps - 1) / x_range);
                int bucket_y = (int)((y - min_y) * (steps - 1) / y_range);
                
                mean_x[bucket_x] += x;
                hits_x[bucket_x]++;

                mean_y[bucket_y] += y;
                hits_y[bucket_y]++;

                if (bucket_x > 0)
                {
                    mean_x[bucket_x - 1] += x;
                    hits_x[bucket_x - 1]++;
                }
                if (bucket_x < steps - 1)
                {
                    mean_x[bucket_x + 1] += x;
                    hits_x[bucket_x + 1]++;
                }
                if (bucket_y > 0)
                {
                    mean_y[bucket_y - 1] += y;
                    hits_y[bucket_y - 1]++;
                }
                if (bucket_y < steps - 1)
                {
                    mean_y[bucket_y + 1] += y;
                    hits_y[bucket_y + 1]++;
                }
            }

            double[] mean = new double[steps * 2];
            for (int step = 0; step < steps; step++)
            {
                if (hits_x[step] > 0) mean_x[step] /= hits_x[step];
                if (hits_y[step] > 0) mean_y[step] /= hits_y[step];
                mean[step * 2] = mean_x[step];
                mean[(step * 2) + 1] = mean_y[step];
            }
            return (mean);
        }

        public double GetVariance()
        {
            double[] minimum = null;
            double[] maximum = null;
            double[] mean = null;
            return (Variance(20, ref minimum, ref maximum, ref mean));
        }

        private double Variance(int steps,
                                ref double[] minimum,
                                ref double[] maximum,
                                ref double[] mean)
        {
            double min_x = 0;
            double min_y = 0;
            double max_x = 1;
            double max_y = 1;
            int i = 0;

            double[] mean_x = new double[steps];
            int[] hits_x = new int[steps];
            double[] mean_y = new double[steps];
            int[] hits_y = new int[steps];

            double[] minx = new double[steps];
            double[] miny = new double[steps];
            double[] maxx = new double[steps];
            double[] maxy = new double[steps];

            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                if (x > max_x) max_x = x;
                if (y > max_y) max_y = y;
                if (x < min_x) min_x = x;
                if (y < min_y) min_y = y;
            }

            double x_range = max_x - min_x;
            double y_range = max_y - min_y;

            for (i = 0; i < minx.Length; i++)
            {
                minx[i] = min_x + ((i+1) * x_range / minx.Length);
                maxx[i] = min_x + ((i+1) * x_range / minx.Length);
                miny[i] = double.MaxValue;
                maxy[i] = double.MinValue;
            }

            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                int bucket_x = (int)((x - min_x) * (steps - 1) / x_range);
                int bucket_y = (int)((y - min_y) * (steps - 1) / y_range);

                mean_x[bucket_x] += x;
                hits_x[bucket_x]++;

                mean_y[bucket_y] += y;
                hits_y[bucket_y]++;

                if (y < miny[bucket_x]) miny[bucket_x] = y;
                if (y > maxy[bucket_x]) maxy[bucket_x] = y;

                if (bucket_x > 0)
                {
                    mean_x[bucket_x - 1] += x;
                    hits_x[bucket_x - 1]++;
                }
                if (bucket_x < steps - 1)
                {
                    mean_x[bucket_x + 1] += x;
                    hits_x[bucket_x + 1]++;
                }
                if (bucket_y > 0)
                {
                    mean_y[bucket_y - 1] += y;
                    hits_y[bucket_y - 1]++;

                    //if (y < miny[bucket_y - 1]) miny[bucket_y - 1] = y;
                    //if ((maxy[bucket_y - 1] == 0) || (y > maxy[bucket_y - 1])) maxy[bucket_y - 1] = y;
                }
                if (bucket_y < steps - 1)
                {
                    mean_y[bucket_y + 1] += y;
                    hits_y[bucket_y + 1]++;

                    //if (y < miny[bucket_y + 1]) miny[bucket_y + 1] = y;
                    //if (y > maxy[bucket_y + 1]) maxy[bucket_y + 1] = y;
                }
            }

            mean = new double[steps * 2];
            minimum = new double[steps * 2];
            maximum = new double[steps * 2];
            double mean_variance = 0;
            int mean_variance_hits = 0;
            for (int step = 0; step < steps; step++)
            {
                if (hits_x[step] > 0) mean_x[step] /= hits_x[step];
                if (hits_y[step] > 0) mean_y[step] /= hits_y[step];
                mean[step * 2] = mean_x[step];
                mean[(step * 2) + 1] = mean_y[step];
                minimum[step * 2] = minx[step];
                minimum[(step * 2) + 1] = miny[step];
                maximum[step * 2] = maxx[step];
                maximum[(step * 2) + 1] = maxy[step];
                if ((miny[step] < double.MaxValue) &&
                    (maxy[step] > double.MinValue))
                {
                    mean_variance += (maxy[step] - miny[step]) * (steps - step) * (steps - step);
                    mean_variance_hits++;
                }
            }
            if (mean_variance_hits > 0) mean_variance = Math.Sqrt(mean_variance) / mean_variance_hits;
            return (mean_variance);
        }


        public void ShowVariance(Byte[] img, int width, int height)
        {
            double min_x = 0;
            double min_y = 0;
            double max_x = 1;
            double max_y = 1;
            int i = 0;

            for (i = 0; i < width * height * 3; i++) img[i] = 255;

            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                if (x > max_x) max_x = x;
                if (y > max_y) max_y = y;
                if (x < min_x) min_x = x;
                if (y < min_y) min_y = y;
            }

            // draw axes
            if (min_x < 0)
            {
                int xx = (int)((0 - min_x) * (width - 1) / (max_x - min_x));
                drawing.drawLine(img, width, height, xx, 0, xx, height - 1, 200, 200, 200, 0, false);
            }
            if (min_y < 0)
            {
                int yy = (int)((0 - min_y) * (height - 1) / (max_y - min_y));
                drawing.drawLine(img, width, height, 0, yy, width - 1, yy, 200, 200, 200, 0, false);
            }

            // show data points
            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                int xx = (int)((x - min_x) * (width - 1) / (max_x - min_x));
                int yy = (int)(height - 1 - ((y - min_y) * (height - 1) / (max_y - min_y)));

                drawing.drawCross(img, width, height, xx, yy, 2, 150, 150, 150, 0);
            }

            double[] average = null;
            double[] minimum = null;
            double[] maximum = null;
            Variance(20, ref minimum, ref maximum, ref average);

            int r = 255, g = 0, b = 0;
            for (int j = 0; j < 3; j++)
            {                
                double[] graph = null;
                switch (j)
                {
                    case 0: { graph = average; r = 0; g = 255; b = 0;  break; }
                    case 1: { graph = minimum; r = 0; g = 0; b = 255; break; }
                    case 2: { graph = maximum; r = 255; g = 0; b = 0; break; }
                }

                int prev_xx = 0, prev_yy = height - 1;
                for (i = 0; i < graph.Length; i += 2)
                {
                    double x = graph[i];
                    double y = graph[i + 1];
                    if ((y > double.MinValue) && (y < double.MaxValue))
                    {
                        int xx = (int)((x - min_x) * (width - 1) / (max_x - min_x));
                        int yy = (int)(height - 1 - ((y - min_y) * (height - 1) / (max_y - min_y)));

                        drawing.drawLine(img, width, height, prev_xx, prev_yy, xx, yy, r, g, b, 0, false);

                        prev_xx = xx;
                        prev_yy = yy;
                    }
                }
            }
        }



        public void Show(Byte[] img, int width, int height,
                         string horizontal_scale, string vertical_scale)
        {
            const double border_x = 0.07;
            const double border_y = 0.1;
            double min_x = -border_x;
            double min_y = -border_y;
            double max_x = 1;
            double max_y = 1;
            int i = 0;

            for (i = 0; i < width * height * 3; i++) img[i] = 255;

            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                if (x > max_x) max_x = x;
                if (y > max_y) max_y = y;
                if (x < min_x) min_x = x;
                if (y < min_y) min_y = y;
            }
            min_x = -max_x * border_x;
            min_y = -max_y * border_y;

            float minor_increment = 10;
            float major_increment = 50;
            int axes_line_width = 0;
            DrawAxes(img, width, height,
                     minor_increment, minor_increment,
                     major_increment, major_increment,
                     1, 0, 0, 0, axes_line_width, true,
                     horizontal_scale, vertical_scale,
                     (float)min_x, (float)min_y,
                     (float)max_x, (float)max_y);

            // show diagonal
            int x1 = (int)(-min_x * (width - 1) / (max_x - min_x));
            int y1 = height - 1 - (int)(-min_y * (height - 1) / (max_y - min_y));
            drawing.drawLine(img, width, height, x1, y1, width - 1, 0, 200, 200, 200, 0, false);

            // show data points
            for (i = 0; i < Xpoints.Count; i++)
            {
                double x = (double)Xpoints[i];
                double y = (double)Ypoints[i];
                int xx = (int)((x - min_x) * (width - 1) / (max_x - min_x));
                int yy = (int)(height - 1 - ((y - min_y) * (height - 1) / (max_y - min_y)));

                drawing.drawCross(img, width, height, xx, yy, 2, 150, 150, 150, 0);
            }

            int prev_x = 0;
            int prev_y = height - 1;
            for (int x = 0; x < max_x; x++)
            {
                int xx = (int)((x - min_x) * (width - 1) / (max_x- min_x));
                int yy = height - 1 - (int)((RegVal(x) - min_y) * (height-1) / (max_y - min_y));
                if (x > 0)
                    drawing.drawLine(img, width, height, prev_x, prev_y, xx, yy, 255, 0, 0, 0, false);
                prev_x = xx;
                prev_y = yy;
            }
        }


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
        public void DrawAxes(byte[] img, int image_width, int image_height,
                             float x_increment_minor, float y_increment_minor,
                             float x_increment_major, float y_increment_major,
                             int increment_marking_size,
                             int r, int g, int b, int lineWidth,
                             bool show_numbers,
                             string horizontal_scale, string vertical_scale,
                             float min_value_x, float min_value_y,
                             float max_value_x, float max_value_y)
        {
            // draw the horizontal axis
            float y = image_height - 1 - (((0 - min_value_y) / (max_value_y - min_value_y)) * image_height);
            drawing.drawLine(img, image_width, image_height,
                             0, (int)y, image_width - 1, (int)y,
                             r, g, b, lineWidth, false);

            //float x = ((Math.Abs(min_value_x) / (max_value_x - min_value_x)) * screen_width);
            float x = ((0 - min_value_x) / (max_value_x - min_value_x)) * image_width;

            // show the name of the horizontal axis
            if (horizontal_scale != "")
                AddText(img, image_width, image_height, horizontal_scale, "Arial", 10, r, g, b,
                        min_value_x + ((max_value_x - min_value_x) * 0.45f),
                        -(max_value_y - min_value_y) / 20,
                        min_value_x, min_value_y,
                        max_value_x, max_value_y);

            AddText(img, image_width, image_height, "RMS error (pixels): " + GetRMSerror().ToString(), "Arial", 10, r, g, b,
                    min_value_x + ((max_value_x - min_value_x) * 0.6f),
                    (max_value_y - min_value_y) / 20,
                    min_value_x, min_value_y,
                    max_value_x, max_value_y);

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
                    int screen_x = (int)(x + (xx * image_width / (max_value_x - min_value_x)));
                    drawing.drawLine(img, image_width, image_height,
                                     screen_x, (int)y, screen_x, (int)(y + marking_size),
                                     r, g, b, lineWidth, false);
                    if ((show_numbers) && (i > 0) && (xx != 0))
                    {
                        String number_str = ((int)(xx * 100) / 100.0f).ToString();
                        float xx2 = xx - ((max_value_x - min_value_x) / 100);
                        float yy2 = -(max_value_y - min_value_y) / 40;
                        AddText(img, image_width, image_height, number_str, "Arial", 8, r, g, b, xx2, yy2,
                                min_value_x, min_value_y,
                                max_value_x, max_value_y);
                    }
                    xx += increment_size;
                }


                xx = 0;
                while (xx > min_value_x)
                {
                    int screen_x = (int)(x + (xx * image_width / (max_value_x - min_value_x)));
                    drawing.drawLine(img, image_width, image_height,
                                     screen_x, (int)y, screen_x, (int)(y + marking_size),
                                     r, g, b, lineWidth, false);
                    if ((show_numbers) && (i > 0) && (xx != 0))
                    {
                        String number_str = ((int)(xx * 100) / 100.0f).ToString();
                        float xx2 = xx - ((max_value_x - min_value_x) / 100);
                        float yy2 = -(max_value_y - min_value_y) / 40;
                        AddText(img, image_width, image_height, number_str, "Arial", 8, r, g, b, xx2, yy2,
                                min_value_x, min_value_y,
                                max_value_x, max_value_y);
                    }
                    xx -= increment_size;
                }

            }

            // draw the vertical axis
            x = ((0 - min_value_x) / (max_value_x - min_value_x)) * image_width;

            //x = (Math.Abs(min_value_x) / (max_value_x - min_value_x)) * screen_width;
            drawing.drawLine(img, image_width, image_height,
                             (int)x, 0, (int)x, image_height - 1,
                             r, g, b, lineWidth, false);

            y = image_height - 1 - (((0 - min_value_y) / (max_value_y - min_value_y)) * image_height);

            // show the name of the vertical axis
            if (horizontal_scale != "")
                AddText(img, image_width, image_height, vertical_scale, "Arial", 10, r, g, b,
                        (max_value_x - min_value_x) / 150,
                        min_value_y + ((max_value_y - min_value_y) * 0.98f),
                        min_value_x, min_value_y,
                        max_value_x, max_value_y);

            for (int i = 0; i < 2; i++)
            {
                float increment_size = y_increment_minor;
                int marking_size = (int)(increment_marking_size * image_width / (float)image_height);
                if (i > 0)
                {
                    increment_size = y_increment_major;
                    marking_size = (int)(increment_marking_size * 2 * image_width / (float)image_height);
                }

                float yy = 0;
                while (yy < max_value_y)
                {
                    int screen_y = (int)(image_height - 1 - (((yy - min_value_y) / (max_value_y - min_value_y)) * image_height));
                    drawing.drawLine(img, image_width, image_height,
                                     (int)x, screen_y, (int)x - marking_size, screen_y,
                                     r, g, b, lineWidth, false);
                    if ((show_numbers) && (i > 0) && (yy != 0))
                    {
                        String number_str = ((int)(yy * 100) / 100.0f).ToString();
                        float yy2 = yy - ((max_value_y - min_value_y) / 200);
                        float xx2 = -(max_value_x - min_value_x) / 20;
                        AddText(img, image_width, image_height, number_str, "Arial", 8, r, g, b, xx2, yy2,
                                min_value_x, min_value_y,
                                max_value_x, max_value_y);
                    }
                    yy += increment_size;
                }
                yy = 0;
                while (yy > min_value_y)
                {
                    int screen_y = (int)(image_height - 1 - (((yy - min_value_y) / (max_value_y - min_value_y)) * image_height));
                    drawing.drawLine(img, image_width, image_height,
                                     (int)x, screen_y, (int)x - marking_size, screen_y,
                                     r, g, b, lineWidth, false);
                    yy -= increment_size;
                }
            }

        }

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
        public void AddText(byte[] img, int image_width, int image_height, 
                            String text,
                            String font, int font_size,
                            int r, int g, int b,
                            float position_x, float position_y,
                            float min_value_x, float min_value_y,
                            float max_value_x, float max_value_y)
        {
            // convert from graph coordinates into screen coordinates
            float x = (position_x - min_value_x) * image_width / (max_value_x - min_value_x);
            float y = image_height - 1 - ((position_y - min_value_y) * image_height / (max_value_y - min_value_y));

            drawing.AddText(img, image_width, image_height,
                            text, font, font_size,
                            r, g, b,
                            x, y);
        }

        #endregion

    }
}
