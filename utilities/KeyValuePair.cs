/*
    Key/Value pairs for use in .NET version 1.1 programs
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
using System.Runtime.InteropServices;
using System.Text;

namespace sluggish.utilities.DOTNET_1_1
{
    /// <summary>
    /// This is a workaround to enable the system to run on .NET 1.1
    /// It's the same as KeyValuePair<int, int> from Collections.Generic
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct KeyValuePairInt
    {
        private int key;
        private int value;

        public KeyValuePairInt(int key, int value)
        {
            this.key = key;
            this.value = value;
        }

        public override string ToString()
        {
            StringBuilder builder1 = new StringBuilder();
            builder1.Append('[');
            builder1.Append(this.Key.ToString());
            builder1.Append(", ");
            builder1.Append(this.Value.ToString());
            builder1.Append(']');
            return builder1.ToString();
        }


        /// <summary>
        /// Gets the Value in the Key/Value Pair
        /// </summary>
        public int Value
        {
            get
            {
                return this.value;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the Key in the Key/Value pair
        /// </summary>
        public int Key
        {
            get
            {
                return this.key;
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }

}
