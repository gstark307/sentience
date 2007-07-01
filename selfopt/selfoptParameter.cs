/*
    Sentience 3D Perception System: Genetic Autotuner
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
using System.IO;
using System.Collections;
using System.Text;

namespace sentience.learn
{
    public class selfoptParameter
    {
        public float value;
        public float min_value;
        public float max_value;
        public float step_size;

        //pick a random value
        public void Randomize(Random rnd)
        {
            if (step_size > 0)
            {
                // use step intervals
                int intervals = (int)((max_value - min_value) / step_size);
                value = min_value + (rnd.Next(intervals) * step_size);
            }
            else
            {
                // continuous value
                value = min_value + (rnd.Next(100000) * (max_value - min_value) / 100000.0f);
            }
        }

        public void Mutate(Random rnd, float rate)
        {
            Seed(rnd, value, rate);

            //if (rnd.Next(100000) / 100000.0f < rate)
            //    Randomize(rnd);
        }

        public void Seed(Random rnd, float seedValue, float variance)
        {
            float variance_magnitude = variance * (max_value - min_value);
            if ((variance_magnitude < step_size * 4) && (step_size > 0)) variance_magnitude = step_size * 4;

            if (rnd.Next(100) < 10)
                value = seedValue - (variance_magnitude / 2.0f) + (variance_magnitude * rnd.Next(100000) / 100000.0f);
            else
                value = seedValue;

            if (step_size > 0) value = (int)(value / step_size) * step_size;
            if (value > max_value) value = max_value;
            if (value < min_value) value = min_value;
        }

        public void Copy(selfoptParameter source)
        {
            this.value = source.value;
            this.min_value = source.min_value;
            this.max_value = source.max_value;
            this.step_size = source.step_size;
        }

        public void save(BinaryWriter binfile)
        {
            binfile.Write(value);
            binfile.Write(min_value);
            binfile.Write(max_value);
            binfile.Write(step_size);
        }
        public void load(BinaryReader binfile)
        {
            value = binfile.ReadSingle();
            min_value = binfile.ReadSingle();
            max_value = binfile.ReadSingle();
            step_size = binfile.ReadSingle();
        }
    }
}
