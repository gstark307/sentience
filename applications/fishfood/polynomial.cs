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
using System.Collections;

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

        private ArrayList Xpoints;
        private ArrayList Ypoints;

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
                    M[j, i] = (double)0;
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

            Xpoints = new ArrayList();
            Ypoints = new ArrayList();

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
                    double x = (double)Xpoints[i];
                    double y = (double)Ypoints[i];
                    double y2 = RegVal(x);
                    double diff = y - y2;
                    error += (diff * diff);
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

                drawing.drawSpot(img, width, height, xx, yy, 1, 200, 200, 200);
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



        public void Show(Byte[] img, int width, int height)
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

                drawing.drawSpot(img, width, height, xx, yy, 1, 150, 150, 150);
            }

            // show average
            double[] average = RunningAverage(20);
            int prev_xx = 0, prev_yy = height - 1;
            for (i = 0; i < average.Length; i += 2)
            {
                double x = average[i];
                double y = average[i + 1];
                int xx = (int)((x - min_x) * (width - 1) / (max_x - min_x));
                int yy = (int)(height - 1 - ((y - min_y) * (height - 1) / (max_y - min_y)));

                drawing.drawLine(img, width, height, prev_xx, prev_yy, xx, yy, 0, 255, 0, 1, false);

                prev_xx = xx;
                prev_yy = yy;
            }


            if (max_x == 1)
            {
                max_x = width/2;
                max_y = height/2;
            }

            int prev_x = 0;
            int prev_y = height - 1;
            for (int x = 0; x < max_x; x++)
            {
                int xx = (int)(x * (width - 1) / max_x);
                int yy = height - 1 - (int)(RegVal(x) * (height-1) / max_y);
                drawing.drawLine(img, width, height, prev_x, prev_y, xx, yy, 255, 0, 0, 1, false);
                prev_x = xx;
                prev_y = yy;
            }
        }
    }
}
