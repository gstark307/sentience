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

namespace sluggish.utilities
{
    public class polynomial
    {
        private const int MaxO = 25; // max polynomial degree
        private int GlobalO;         // degree of the polynomial expected
        private bool Finished;

        private float[] SumX;
        private float[] SumYX;
        private float[,] M;
        private float[] C; // coefficients

        private ArrayList Xpoints;
        private ArrayList Ypoints;

        public polynomial()
        {
            SumX = new float[(2 * MaxO) + 1];
            SumYX = new float[MaxO + 1];
            M = new float[MaxO, MaxO + 1];
            C = new float[MaxO + 1]; // coefficients in: Y = C(0)*X^0 + C(1)*X^1 + C(2)*X^2 + ...

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
            float T, O1;
  
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
                    M[j, i] = (float)0;
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

        public void SetCoeff(int Exponent, float value)
        {
            Finished = true;
            C[Exponent] = value;
        }

        public float Coeff(int Exponent)
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

        public void AddPoint(float x, float y)
        {
            int i, Max2O;
            float TX;

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

        public float RegVal(float x)
        {
            int i, O;
            float retval=0;

            if (!Finished) Solve();
            O = GlobalO;
            //if (XYCount() <= O) O = XYCount() - 1;
            for (i = 0; i <= O; i++)
                retval = retval + C[i] * (float)Math.Pow(x, i);
            return (retval);
        }


        public float GetRMSerror()
        {
            float rms_error = 0;

            if (Xpoints.Count > 0)
            {
                for (int i = 0; i < Xpoints.Count; i++)
                {
                    float x = (float)Xpoints[i];
                    float y = (float)Ypoints[i];
                    float y2 = RegVal(x);
                    float diff = y - y2;
                    rms_error += (diff * diff);
                }
                rms_error = (float)Math.Sqrt(rms_error/(float)Xpoints.Count);
            }
            return (rms_error);
        }

        public void Show(Byte[] img, int width, int height)
        {
            float min_x = 0;
            float min_y = 0;
            float max_x = 1;
            float max_y = 1;
            int i = 0;

            for (i = 0; i < width * height * 3; i++) img[i] = 255;

            for (i = 0; i < Xpoints.Count; i++)
            {
                float x = (float)Xpoints[i];
                float y = (float)Ypoints[i];
                if (x > max_x) max_x = x;
                if (y > max_y) max_y = y;
                if (x < min_x) min_x = x;
                if (y < min_y) min_y = y;
            }

            // draw axes
            if (min_x < 0)
            {
                int xx = (int)((0 - min_x) * (width - 1) / (max_x - min_x));
                sluggish.utilities.drawing.drawLine(img, width, height, xx, 0, xx, height - 1, 200, 200, 200, 0, false);
            }
            if (min_y < 0)
            {
                int yy = (int)((0 - min_y) * (height - 1) / (max_y - min_y));
                sluggish.utilities.drawing.drawLine(img, width, height, 0, yy, width-1, yy, 200, 200, 200, 0, false);
            }

            for (i = 0; i < Xpoints.Count; i++)
            {
                float x = (float)Xpoints[i];
                float y = (float)Ypoints[i];
                int xx = (int)((x-min_x) * (width - 1) / (max_x-min_x));
                int yy = (int)(height - 1 - ((y-min_y) * (height - 1) / (max_y-min_y)));
                int n = ((yy * width) + xx) * 3;
                img[n] = 150;
                img[n + 1] = 150;
                img[n + 2] = 150;
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
                sluggish.utilities.drawing.drawLine(img, width, height, prev_x, prev_y, xx, yy, 100, 100, 100, 0, false);
                prev_x = xx;
                prev_y = yy;
            }
        }
    }
}
