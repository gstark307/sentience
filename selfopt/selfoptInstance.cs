/*
    Sentience 3D Perception System: Genetic Autotuner
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
using System.IO;
using System.Collections;
using System.Text;

namespace sentience.learn
{
    public class selfoptInstance
    {
        public selfoptParameter[] parameter;
        public float score;

        #region "conversions"

        /// <summary>
        /// save the parameters for this instance as a float array
        /// </summary>
        /// <returns></returns>
        public float[] ToFloatArray()
        {
            float[] result = new float[parameter.Length];
            for (int i = 0; i < parameter.Length; i++)
                result[i] = parameter[i].value;
            return (result);
        }

        /// <summary>
        /// return the values as a string array
        /// </summary>
        /// <returns></returns>
        public String[] AsString()
        {
            String[] result = new String[parameter.Length];
            for (int i = 0; i < parameter.Length; i++)
                result[i] = parameter[i].value.ToString();
            return (result);
        }

        #endregion

        public selfoptInstance(int no_of_parameters)
        {
            parameter = new selfoptParameter[no_of_parameters];
            for (int p = 0; p < no_of_parameters; p++)
                parameter[p] = new selfoptParameter();
        }

        public void setParameterRange(int parameter_index, float min, float max)
        {
            parameter[parameter_index].min_value = min;
            parameter[parameter_index].max_value = max;
        }

        public void setParameterStepSize(int parameter_index, float step_size)
        {
            parameter[parameter_index].step_size = step_size;
        }

        public void Randomize(Random rnd)
        {
            score = 0;
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Randomize(rnd);
        }

        public void seed(Random rnd, String values, float variance)
        {
            String[] v = values.Split(',');

            score = 0;
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Seed(rnd, Convert.ToSingle(v[p]), variance);
        }

        public void Mutate(Random rnd, float rate)
        {
            float adjusted_rate = rate * score;

            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Mutate(rnd, adjusted_rate);
        }

        public void Copy(selfoptInstance source)
        {
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Copy(source.parameter[p]);
            score = source.score;
        }

        public void copysexual(selfoptInstance parent1, selfoptInstance parent2, float mutation_rate, Random rnd)
        {
            //copy from first parent
            Copy(parent1);

            //copy half the parameters randomly from second parent
            for (int p = 0; p < parameter.Length / 2; p++)
            {
                int idx = rnd.Next(parameter.Length - 1);
                parameter[idx].Copy(parent2.parameter[idx]);
            }
            //mutate
            score = 1;
            Mutate(rnd, mutation_rate);
            score = 0;
        }

        public void save(BinaryWriter binfile)
        {
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].save(binfile);
            binfile.Write(score);
        }

        public void load(BinaryReader binfile)
        {
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].load(binfile);
            score = binfile.ReadSingle();
        }

    }
}
