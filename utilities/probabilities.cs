/*
    Probability functions
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

namespace sluggish.utilities
{	
	public class probabilities
	{		
		public probabilities()
		{
		}
		
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
	}
}
