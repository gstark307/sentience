/*
    Colour functions
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
	public class colours
	{		
		public colours()
		{
		}		
		
		/// <summary>
        /// Gets the RGB value for the specified character
        /// </summary>
        /// <param name="InChar"> The character to convert </param>
        /// <returns>System.Int32</returns>
        private static int GetRGB(char InChar)
        {
            int Value = 0;
            char Char = InChar.ToString().ToUpper().ToCharArray()[0];
            if (Char == 'A')
            {
                Value = 10;
            }
            else if (Char == 'B')
            {
                Value = 11;
            }
            else if (Char == 'C')
            {
                Value = 12;
            }
            else if (Char == 'D')
            {
                Value = 13;
            }
            else if (Char == 'E')
            {
                Value = 14;
            }
            else if (Char == 'F')
            {
                Value = 15;
            }
            else
            {
                Value = int.Parse(Char.ToString());
            }
            return Value;
        }

        /// <summary>
        /// returns RGB values for the specified hexidecimal code
        /// </summary>
        /// <param name="Hexidecimal"> The hexidecimal code to convert </param>
        public static void GetRBGFromHex(string Hexidecimal,
                                         ref int R, ref int G, ref int B)
        {
            char[] arChars = Hexidecimal.Replace("#", "").ToCharArray();
            for (int i = 0; i < arChars.Length; i++)
            {
                switch (i)
                {
                    case 0: { R = (GetRGB(arChars[i]) * 16) + GetRGB(arChars[i + 1]); } break;
                    case 1: { } break;
                    case 2: { G = (GetRGB(arChars[i]) * 16) + GetRGB(arChars[i + 1]); } break;
                    case 3: { } break;
                    case 4: { B = (GetRGB(arChars[i]) * 16) + GetRGB(arChars[i + 1]); } break;
                    case 5: { } break;
                }
            }
        }

        /// <summary>
        /// Gets the hexidecimal color code from the specified RGB
        /// </summary>
        /// <param name="R"> The value of R </param>
        /// <param name="G"> The value of G </param>
        /// <param name="B"> The value of B </param>
        /// <returns></returns>
        public static string GetHexFromRGB(int R, int G, int B)
        {
            string a, b, c, d, e, f, z;
            a = GetHex(Math.Floor((double)R / 16));
            b = GetHex(R % 16);
            c = GetHex(Math.Floor((double)G / 16));
            d = GetHex(G % 16);
            e = GetHex(Math.Floor((double)B / 16));
            f = GetHex(B % 16);
            z = a + b + c + d + e + f;
            return z;
        }

        /// <summary>
        /// Returns the hexidecimal character for the specified value
        /// </summary>
        /// <param name="Dec"> The value to convert </param>
        /// <returns>System.String</returns>
        private static string GetHex(double Dec)
        {
            string Value = "";
            if (Dec == 10)
            {
                Value = "A";
            }
            else if (Dec == 11)
            {
                Value = "B";
            }
            else if (Dec == 12)
            {
                Value = "C";
            }
            else if (Dec == 13)
            {
                Value = "D";
            }
            else if (Dec == 14)
            {
                Value = "E";
            }
            else if (Dec == 15)
            {
                Value = "F";
            }
            else
            {
                Value = "" + Dec;
            }
            return Value;
        }
	}
}
