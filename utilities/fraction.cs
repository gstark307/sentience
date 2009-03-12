/*
    fractions
    Copyright (C) 2008 Bob Mottram
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
	public class fraction
	{
	    public long numerator, denominator;
	
		public fraction()
		{
		}
		
		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="numerator">fraction numerator</param>
		/// <param name="denominator">fraction denominator</param>
		public fraction(int numerator, int denominator)
		{
		    this.numerator = numerator;
		    this.denominator = denominator;
		}
				
		/// <summary>
		/// add fractions
		/// </summary>
		/// <param name="f1">fraction</param>
		/// <param name="f2">fraction to be added</param>
		/// <returns>resulting fraction</returns>
		public static fraction operator + (fraction f1, fraction f2)
		{
			fraction frac = new fraction();
			if ((f1.numerator != 0) && (f2.numerator != 0))
			{
			    frac.numerator = (f1.numerator * f2.denominator) + (f2.numerator * f1.denominator);
			    frac.denominator = f2.denominator * f1.denominator;
			}
			else
			{
			    if (f1.numerator == 0)
			    {
			        frac.numerator = f2.numerator;
			        frac.denominator = f2.denominator;
			    }
			    else
			    {
			        frac.numerator = f1.numerator;
			        frac.denominator = f1.denominator;
			    }
			}
			if ((frac.denominator < 0) && (frac.numerator >= 0))
			{
			    frac.numerator = -frac.numerator;
			    frac.denominator = -frac.denominator;
			}
			return(frac);
		}
		
		/// <summary>
		/// subtract fractions
		/// </summary>
		/// <param name="f1">fraction</param>
		/// <param name="f2">fraction to be subtracted</param>
		/// <returns>resulting fraction</returns> 
		public static fraction operator - (fraction f1, fraction f2)
		{
			fraction frac = new fraction();
			if ((f1.numerator != 0) && (f2.numerator != 0))
			{
			    if (f2.numerator > 0)
			    {
			        frac.numerator = (f1.numerator * f2.denominator) - (f2.numerator * f2.denominator);
			        frac.denominator = f1.denominator * f2.denominator;
			    }
			    else
			    {
			        frac.numerator = (f1.numerator * f2.denominator) + (-f2.numerator * f1.denominator);
			        frac.denominator = f2.denominator * f1.denominator;
			    }
			}
			else
			{
			    if (f1.numerator == 0)
			    {
			        frac.numerator = -f2.numerator;
			        frac.denominator = f2.denominator;
			    }
			    else
			    {
			        frac.numerator = f1.numerator;			    
			        frac.denominator = f1.denominator;
			    }
			}
						    			
			if ((frac.denominator < 0) && (frac.numerator >= 0))
			{
			    frac.numerator = -frac.numerator;
			    frac.denominator = -frac.denominator;
			}
			return(frac);
		}
		
		/// <summary>
		/// multiply fractions
		/// </summary>
		/// <param name="f1">fraction</param>
		/// <param name="f2">fraction to be multiplied with</param>
		/// <returns>resulting fraction</returns> 
		public static fraction operator *(fraction f1, fraction f2)
		{
			fraction frac = new fraction();
			frac.numerator = f1.numerator * f2.numerator;
			frac.denominator = f1.denominator * f2.denominator;
			if ((frac.denominator < 0) && (frac.numerator >= 0))
			{
			    frac.numerator = -frac.numerator;
			    frac.denominator = -frac.denominator;
			}
			return(frac);
		}
		
		/// <summary>
		/// divide fractions
		/// </summary>
		/// <param name="f1">fraction</param>
		/// <param name="f2">fraction to be divided by</param>
		/// <returns>resulting fraction</returns> 
		public static fraction operator /(fraction f1, fraction f2)
		{
			fraction frac = new fraction();
			frac.numerator = f1.numerator * f2.denominator;
			frac.denominator = f1.denominator * f2.numerator;
			if(frac.denominator < 0)
			{
			    frac.numerator *= -1;
			    frac.denominator *= -1;
			}
			if ((frac.denominator < 0) && (frac.numerator >= 0))
			{
			    frac.numerator = -frac.numerator;
			    frac.denominator = -frac.denominator;
			}
			return(frac);
		}

		public static bool operator ==(fraction f1, fraction f2)
		{
			if ((f1.numerator == f2.numerator) &&
			    (f1.denominator == f2.denominator))
			    return(true);
			else
			    return(false);
		}

		public static bool operator !=(fraction f1, fraction f2)
		{
			if ((f1.numerator != f2.numerator) ||
			    (f1.denominator != f2.denominator))
			    return(true);
			else
			    return(false);
		}

								
		/// <summary>
		/// returns an inexact float value for the fraction
		/// </summary>
		/// <returns>float value</returns>
		public float Value()
		{
			if (denominator != 0)
			    return (numerator/(float)denominator);
			else
				return(0.0f);
		}
				
		/// <summary>
		/// returns the greatest common denominator
		/// </summary>
		/// <param name="num1"></param>
		/// <param name="remainder"></param>
		/// <returns>denominator</returns>
		private static long GCD(long num1, long remainder)
		{
			if (remainder == 0)
			{
				return(num1);
			}
			else
			{
				return(GCD(remainder, num1 % remainder));
			}
		}
		
		/// <summary>
		/// reduces the fraction
		/// </summary>
		/// <param name="numerator">fraction numerator</param>
		/// <param name="denominator">fraction denominator</param>  
		public static void Reduce(ref long numerator, ref long denominator)
		{
			long rdc = 0;
			if(denominator > numerator)
				rdc = GCD(denominator, numerator);
			else if(denominator < numerator)
				rdc = GCD(numerator, denominator);
			else
				rdc = GCD(numerator, denominator);
			numerator /= rdc;
			denominator /= rdc;
			
			if ((denominator < 0) && (numerator >= 0))
			{
			    numerator = -numerator;
			    denominator = -denominator;
			}			
		}
		
		/// <summary>
		/// reduces the fraction
		/// </summary>
		public void Reduce()
		{
			Reduce(ref numerator, ref denominator);
		}
		
		/// <summary>
		/// truncates the fraction
		/// </summary>
		/// <param name="max_value"></param>
        public void Truncate(int max_value)
        {
	        while (numerator > max_value)
	        {
	            numerator /= 2;
	            denominator /= 2;
	        }
        }
		
				
	}
}
