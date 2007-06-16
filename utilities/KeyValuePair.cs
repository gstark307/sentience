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
