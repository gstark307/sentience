/*
    Probability functions
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

namespace sluggish.utilities
{	
	public class probabilities
	{		
		public probabilities()
		{
		}

        #region "log odds"
		
		/// <summary>
        /// convert probability to log odds
        /// </summary>
        /// <param name="probability"></param>
        /// <returns></returns>
        public static float LogOdds(float probability)
        {
            if (probability > 0.999f) probability = 0.999f;
            if (probability < 0.001f) probability = 0.001f;
            return ((float)Math.Log10(probability / (1.0f - probability)));
        }

        /// <summary>
        /// convert a log odds value back into a probability value
        /// </summary>
        /// <param name="logodds"></param>
        /// <returns></returns>
        public static float LogOddsToProbability(float logodds)
        {
            return(1.0f - (1.0f/(1.0f + (float)Math.Exp(logodds))));
        }
        
        #endregion
        
        #region "gaussian distribution"
        
        /// <summary>
        /// gaussian function
        /// </summary>
        /// <param name="fraction"></param>
        /// <returns></returns>
        public static float Gaussian(float fraction)
        {
            fraction *= 3.0f;
            float prob = (float)((1.0f / Math.Sqrt(2.0*Math.PI))*Math.Exp(-0.5*fraction*fraction));

            return (prob*2.5f);
        }

        public static float[] CreateGaussianLookup(int levels)
        {
            float[] gaussLookup = new float[levels];

            for (int i = 0; i < levels; i++)
            {
                float fract = ((i * 2.0f) / levels) - 1.0f;
                gaussLookup[i] = Gaussian(fract);
            }
            return (gaussLookup);
        }

        public static float[] CreateHalfGaussianLookup(int levels)
        {
            float[] gaussLookup = new float[levels];

            for (int i = 0; i < levels; i++)
            {
                float fract = (i / (float)levels);
                gaussLookup[i] = Gaussian(fract);
            }
            return (gaussLookup);
        }                
        #endregion
	}
}
