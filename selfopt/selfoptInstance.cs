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

        #region "constructors"

        public selfoptInstance(int no_of_parameters)
        {
            parameter = new selfoptParameter[no_of_parameters];
            for (int p = 0; p < no_of_parameters; p++)
                parameter[p] = new selfoptParameter();
        }

        #endregion

        #region "phase space representation"

        /// <summary>
        /// returns the two dimensional phase space position of this individual
        /// </summary>
        /// <param name="phase_x">x coordinate in phase space</param>
        /// <param name="phase_y">y coordinate in phase space</param>
        public void getPhaseSpacePosition(ref float phase_x, ref float phase_y)
        {
            phase_x = 0;
            phase_y = 0;

            for (int p = 0; p < parameter.Length; p++)
            {
                // fractional value of the parameter normalised within its range
                float fraction = (parameter[p].value - parameter[p].min_value) /
                                 (parameter[p].max_value - parameter[p].min_value);

                if (p < parameter.Length / 2)
                {
                    // X coordinate
                    phase_x += fraction;
                }
                else
                {
                    // Y coordinate
                    phase_y += fraction;
                }
            }

            // normalise into a 0.0 - 1.0 range
            phase_x /= (parameter.Length / 2);
            phase_y /= (parameter.Length - (parameter.Length / 2));
        }

        #endregion

        #region "parameter setting"

        /// <summary>
        /// set the range of a given parameter
        /// </summary>
        /// <param name="parameter_index"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void setParameterRange(int parameter_index, float min, float max)
        {
            parameter[parameter_index].min_value = min;
            parameter[parameter_index].max_value = max;
        }

        /// <summary>
        /// set the step size used for the given parameter
        /// </summary>
        /// <param name="parameter_index"></param>
        /// <param name="step_size"></param>
        public void setParameterStepSize(int parameter_index, float step_size)
        {
            parameter[parameter_index].step_size = step_size;
        }

        #endregion

        #region "initialisation"

        /// <summary>
        /// assign random values to all parameters
        /// </summary>
        /// <param name="rnd"></param>
        public void Randomize(Random rnd)
        {
            score = 0;
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Randomize(rnd);
        }

        /// <summary>
        /// seed this individual based upon the given comma separated string of parameter values
        /// </summary>
        /// <param name="rnd">random number generator</param>
        /// <param name="values">values to use for seeding</param>
        /// <param name="variance"></param>
        public void seed(Random rnd, String values, float variance)
        {
            String[] v = values.Split(',');

            score = 0;
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Seed(rnd, Convert.ToSingle(v[p]), variance);
        }

        /// <summary>
        /// seed this individual around the given phase space position
        /// </summary>
        /// <param name="rnd">random number generator</param>
        /// <param name="phase_x">phase space x coordinate</param>
        /// <param name="phase_y">phase space y coordinate</param>
        /// <param name="variance"></param>
        public void seed(Random rnd,
                         float phase_x, float phase_y,
                         float variance)
        {
            float value = 0;

            score = 0;
            for (int p = 0; p < parameter.Length; p++)
            {
                if (p < parameter.Length / 2)
                    value = parameter[p].min_value +
                            (phase_x * (parameter[p].max_value - parameter[p].min_value));
                else
                    value = parameter[p].min_value +
                            (phase_y * (parameter[p].max_value - parameter[p].min_value));

                parameter[p].Seed(rnd, value, variance);
            }
        }

        #endregion

        #region "sexual and asexual reproduction"

        /// <summary>
        /// randomly mutate the given individual
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="rate"></param>
        public void Mutate(Random rnd, float rate)
        {
            float adjusted_rate = rate * score;

            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Mutate(rnd, adjusted_rate);
        }

        /// <summary>
        /// make a clone of the given individual
        /// </summary>
        /// <param name="source"></param>
        public void Copy(selfoptInstance source)
        {
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].Copy(source.parameter[p]);
            score = source.score;
        }

        /// <summary>
        /// perform crossover between two parent individuals
        /// </summary>
        /// <param name="parent1"></param>
        /// <param name="parent2"></param>
        /// <param name="mutation_rate"></param>
        /// <param name="rnd"></param>
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

        #endregion

        #region "saving and loading"

        /// <summary>
        /// save data to the given stream
        /// </summary>
        /// <param name="binfile"></param>
        public void save(BinaryWriter binfile)
        {
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].save(binfile);
            binfile.Write(score);
        }

        /// <summary>
        /// load data from the given stream
        /// </summary>
        /// <param name="binfile"></param>
        public void load(BinaryReader binfile)
        {
            for (int p = 0; p < parameter.Length; p++)
                parameter[p].load(binfile);
            score = binfile.ReadSingle();
        }

        #endregion

    }
}
