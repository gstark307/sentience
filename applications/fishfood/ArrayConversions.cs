/*
    Convert between different array types
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

namespace sentience.calibration
{
    /// <summary>
    /// Used to convert between arrays and various other data types
    /// </summary>	
	public class ArrayConversions
	{
		
		public ArrayConversions()
		{
		}

        #region "bytes to words and dwords"
        
        public static int ToWord(byte low_byte, byte high_byte)
        {
            if (high_byte < 0x80)
                return( high_byte * 0x100 | low_byte);
            else
                return((high_byte |0xFF00) * 0x100 | low_byte);            
        }

        public static int ToDWord(int low_word, int high_word)
        {
            return(high_word * 0x10000 | (low_word & 0xFFFF));
        }

        public static int ToDWord(byte[] bytes)
        {
            int low_word = ToWord(bytes[0], bytes[1]);
            int high_word = ToWord(bytes[2], bytes[3]);
            return(ToDWord(low_word, high_word));
        }
        
        #endregion

        /// <summary>
        /// convert a float array to a byte array
        /// </summary>
        /// <param name="array">float array</param>
        /// <returns></returns>
        public static byte[] ToByteArray(float[] array)
        {
            byte[] bytes = new byte[array.Length * 4];
            int x = 0;
            foreach(float f in array)
            {
                Byte[] t = BitConverter.GetBytes(f);
                for (int y = 0; y < 4; y++)
                    bytes[x + y] = t[y];
                x += 4;
            }
            return (bytes);
        }

        /// <summary>
        /// converts a byte array to a float array
        /// </summary>
        /// <param name="array">byte array</param>
        /// <returns></returns>
        public static float[] ToFloatArray(Byte[] array)
        {
            float[] floats = new float[array.Length / 4];
            for (int i = 0; i < floats.Length; i++)
                floats[i] = BitConverter.ToSingle(array, i*4);
            return (floats);
        }

        /// <summary>
        /// convert a boolean array to a byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(bool[] array)
        {
            byte[] powers_of_2 = { 1, 2, 4, 8, 16, 32, 64, 128 };
            int len = array.Length/8;
            if (len * 8 < array.Length) len++;
            byte[] bytes = new Byte[len];
            int offset = 0;
            int n = 0;
            int i = 0;
            for (int b = 0; b < array.Length; b++)
            {
                if (array[i])
                    bytes[n] = (byte)(bytes[n] | powers_of_2[offset]);

                i++;
                offset++;
                if (offset > 7)
                {
                    offset = 0;
                    n++;
                }
            }
            return (bytes);
        }

        /// <summary>
        /// converts a byte array into a boolean array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static bool[] ToBooleanArray(Byte[] array)
        {
            int[] powers_of_2 = { 1, 2, 4, 8, 16, 32, 64, 128 };
            bool[] booleans = new bool[array.Length * 8];
            for (int i = 0; i < array.Length; i++)
            {
                for (int offset = 0; offset < 8; offset++)
                {
                    int result = (int)array[i] & powers_of_2[offset];
                    if (result != 0)
                        booleans[(i * 8) + offset] = true;
                }
            }
            return (booleans);
        }
	}
}
