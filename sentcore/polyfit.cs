/*
    Polynomial curve fitting
    Adapted from a visual basic algorithm originally written by Frank Schindler, 16.9.2000
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class polyfit
    {
        private const int MaxO = 25; // max polynomial degree
        private int GlobalO;         // degree of the polynomial expected
        private bool Finished;

        private float[] SumX;
        private float[] SumYX;
        private float[,] M;
        private float[] C; // coefficients

        public polyfit()
        {
            SumX = new float[2 * MaxO];
            SumYX = new float[MaxO];
            M = new float[MaxO, MaxO + 1];
            C = new float[MaxO]; // coefficients in: Y = C(0)*X^0 + C(1)*X^1 + C(2)*X^2 + ...

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

            Finished = false;
            for (i = 0; i <= MaxO; i++)
            {
                SumX[i] = 0;
                SumX[i + MaxO] = 0;
                SumYX[i] = 0;
                C[i] = 0;
            }
        }

        public float Coeff(int Exponent)
        {
            int Ex, O;

            if (!Finished) Solve();
            Ex = Math.Abs(Exponent);
            O = GlobalO;
            if (XYCount() <= O) O = XYCount() - 1;
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
            if (XYCount() <= O) O = XYCount() - 1;
            for (i = 0; i <= O; i++)
            {
                retval = retval + C[i] * (float)Math.Pow(x,i);
            }
            return (retval);
        }

    }
}
