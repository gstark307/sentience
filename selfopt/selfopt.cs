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
using System.Collections.Generic;
using System.Text;

namespace sentience.learn
{
    public class selfopt
    {
        Random rnd = new Random();

        //store the history of best scores
        private int bestScoreHistoryIndex = 0;
        private float[] bestScoreHistory;

        // population of instances to be evaluated
        private selfoptInstance[] instance;

        // a name given to each parameter
        public String[] parameterName;

        // are smaller score values better or not ?
        public bool smallerScoresAreBetter = true;

        // mutation rate
        public float mutationRate = 0.1f;

        // generations elapsed
        public long generation = 0;

        // best score achieved so far
        public float best_score = 0;

        // the number of instances since an improvement was found
        private int last_improved = 0;

        // index number for the instance currently being evaluated
        private int current_instance_index = 0;

        // index number for the best instance found
        private int best_index = 0;

        // whenever a new best score is produced automatically save the system
        public bool autoSave = false;
        public String autoSaveFilename = "autosave.dat";

        #region "initialisation functions"

        private void init(int no_of_instances, int parameters_per_instance)
        {
            instance = new selfoptInstance[no_of_instances];
            for (int i = 0; i < no_of_instances; i++)
                instance[i] = new selfoptInstance(parameters_per_instance);

            parameterName = new String[parameters_per_instance];

            // store a few best scores
            bestScoreHistory = new float[1000];
        }

        public selfopt(int no_of_instances, int parameters_per_instance)
        {
            init(no_of_instances, parameters_per_instance);
        }

        public void setParameterRange(int parameter_index, float min, float max)
        {
            for (int i = 0; i < instance.Length; i++)
                instance[i].setParameterRange(parameter_index, min, max);
        }

        public void setParameterStepSize(int parameter_index, float step_size)
        {
            for (int i = 0; i < instance.Length; i++)
                instance[i].setParameterStepSize(parameter_index, step_size);
        }

        public void Randomize(bool leaveBestInstance)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                if ((!leaveBestInstance) || ((leaveBestInstance) && (i != best_index)))
                    instance[i].Randomize(rnd);
            }
        }

        #endregion

        #region "set the score for the currently evaluated instance"

        /// <summary>
        /// set the score for the currenly evaluated instance
        /// </summary>
        /// <param name="score"></param>
        public void setScore(float score)
        {
            last_improved++;
            instance[current_instance_index].score = score;

            //is this the best score?
            if (((smallerScoresAreBetter) && (score < instance[best_index].score)) ||
                ((!smallerScoresAreBetter) && (score > instance[best_index].score)) ||
                (best_score == 0))
            {
                best_index = current_instance_index;
                best_score = score;
                last_improved = 0;
                if (autoSave) save(autoSaveFilename);
            }

            //update the best score history
            bestScoreHistoryIndex++;
            if (bestScoreHistoryIndex >= bestScoreHistory.Length)
                bestScoreHistoryIndex = 0;
            bestScoreHistory[bestScoreHistoryIndex] = best_score;

            //go to the next instance whose score has not yet been calculated
            nextInstance();
        }

        /// <summary>
        /// go to the next instance whose score has not yet been calculated
        /// </summary>
        private void nextInstance()
        {
            bool found = false;
            int tries = 0;
            int i = current_instance_index;
            while ((!found) && (tries < instance.Length))
            {
                i++;
                if (i >= instance.Length) i = 0;
                if (instance[i].score == 0)
                {
                    found = true;
                    current_instance_index = i;
                }
                tries++;
            }
            if (tries >= instance.Length) update();

            // if no improvement has been made for a long time
            if (last_improved >= instance.Length*2)
            {
                seedBest();
                //Randomize(true);
                last_improved = 0;
            }
        }

        #endregion

        #region "retrieve information about the best instance"

        /// <summary>
        /// retrieve the given parameter for the best instance so far
        /// </summary>
        /// <param name="parameter_index"></param>
        /// <returns></returns>
        public float getParameterBest(int parameter_index)
        {
            return (instance[best_index].parameter[parameter_index].value);
        }

        /// <summary>
        /// retrieve the given parameter for the currently evaluated instance
        /// </summary>
        /// <param name="parameter_index"></param>
        /// <returns></returns>
        public float getParameter(int parameter_index)
        {
            return (instance[current_instance_index].parameter[parameter_index].value);
        }

        #endregion

        #region "produce the next generation"

        /// <summary>
        /// sort into score order, best first
        /// </summary>
        private void Sort()
        {
            for (int i = 0; i < instance.Length - 1; i++)
            {                
                for (int j = i + 1; j < instance.Length; j++)
                {
                    if (((smallerScoresAreBetter) && (instance[i].score > instance[j].score)) ||
                        ((!smallerScoresAreBetter) && (instance[i].score < instance[j].score)))
                    {
                        selfoptInstance temp = instance[i];
                        instance[i] = instance[j];
                        instance[j] = temp;
                    }
                }
            }
            best_index = 0;
            best_score = instance[best_index].score;
        }

        /// <summary>
        /// proceed to the next generation
        /// </summary>
        private void update()
        {
            // sort by score
            Sort();

            // replace lower scoring instances, using sexual reproduction
            for (int i = instance.Length / 2; i < instance.Length; i++)
            {
                selfoptInstance parent1 = instance[rnd.Next(instance.Length / 2)];
                selfoptInstance parent2 = instance[rnd.Next(instance.Length / 2)];
                instance[i].copysexual(parent1, parent2, mutationRate, rnd);
            }

            //set the current instance to be evaluated
            current_instance_index = instance.Length / 2;

            // increment generations
            generation++;
        }

        public void seed(String values, float variance)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                instance[i].seed(rnd, values, variance);
            }

        }

        public void seedBest()
        {
            int i;
            String values = "";

            for (i = 0; i < instance[best_index].parameter.Length; i++)
            {
                if (i > 0) values += ",";
                values += Convert.ToString(instance[best_index].parameter[i].value);
            }

            for (i = 0; i < instance.Length; i++)
            {
                if (i != best_index)
                    instance[i].seed(rnd, values, mutationRate);
            }

        }

        #endregion

        #region"saving and loading"

        public void save(String filename)
        {
            FileStream fp = new FileStream(filename, FileMode.Create);
            BinaryWriter binfile = new BinaryWriter(fp);

            binfile.Write(instance.Length);
            binfile.Write(instance[0].parameter.Length);

            binfile.Write(bestScoreHistoryIndex);
            binfile.Write(smallerScoresAreBetter);
            binfile.Write(generation);
            binfile.Write(best_score);
            binfile.Write(current_instance_index);
            binfile.Write(best_index);
            binfile.Write(last_improved);

            for (int p = 0; p < instance[0].parameter.Length; p++)
                binfile.Write(parameterName[p]);

            for (int i = 0; i < instance.Length; i++)
                instance[i].save(binfile);

            binfile.Close();
            fp.Close();
        }


        public void load(String filename)
        {
            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);

                int no_of_instances = binfile.ReadInt32();
                int no_of_parameters = binfile.ReadInt32();

                init(no_of_instances, no_of_parameters);

                bestScoreHistoryIndex = binfile.ReadInt32();
                smallerScoresAreBetter = binfile.ReadBoolean();
                generation = binfile.ReadInt64();
                best_score = binfile.ReadSingle();
                current_instance_index = binfile.ReadInt32();
                best_index = binfile.ReadInt32();
                last_improved = binfile.ReadInt32();

                for (int p = 0; p < instance[0].parameter.Length; p++)
                    parameterName[p] = binfile.ReadString();

                for (int i = 0; i < instance.Length; i++)
                    instance[i].load(binfile);

                binfile.Close();
                fp.Close();
            }
        }

        #endregion

        #region "drawing functions"

        /// <summary>
        /// show the best score history within the given bitmap
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showHistory(Byte[] img, int img_width, int img_height)
        {
            for (int i = 0; i < img.Length; i++) img[i] = 240;

            // get the maximum score
            float max_score = 0.01f;
            for (int i = 0; i < bestScoreHistory.Length; i++)
                if (bestScoreHistory[i] > max_score) max_score = bestScoreHistory[i];
            max_score *= 1.1f;

            int x, y;
            int prev_x = 0;
            int prev_y = 0;
            for (int t = 0; t < bestScoreHistory.Length; t++)
            {
                x = t * (img_width-1) / bestScoreHistory.Length;
                y = img_height - 1 - (int)(bestScoreHistory[t] / max_score * img_height);

                if (t > 0)
                    drawLine(img, img_width, img_height, x, y, prev_x, prev_y, 0, 0, 0, 0);

                prev_x = x;
                prev_y = y;
            }
        }

        public static void drawLine(Byte[] img, int img_width, int img_height,
                                    int x1, int y1, int x2, int y2, int r, int g, int b, int linewidth)
        {
            int w, h, x, y, step_x, step_y, dx, dy, xx2, yy2;
            float m;

            dx = x2 - x1;
            dy = y2 - y1;
            w = Math.Abs(dx);
            h = Math.Abs(dy);
            if (x2 >= x1) step_x = 1; else step_x = -1;
            if (y2 >= y1) step_y = 1; else step_y = -1;

            if (w > h)
            {
                if (dx != 0)
                {
                    m = dy / (float)dx;
                    x = x1;
                    while (x != x2 + step_x)
                    {
                        y = (int)(m * (x - x1)) + y1;

                        for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                            for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                            {
                                if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                {
                                    int n = ((img_width * yy2) + xx2) * 3;
                                    img[n] = (Byte)b;
                                    img[n + 1] = (Byte)g;
                                    img[n + 2] = (Byte)r;
                                }
                            }

                        x += step_x;
                    }
                }
            }
            else
            {
                if (dy != 0)
                {
                    m = dx / (float)dy;
                    y = y1;
                    while (y != y2 + step_y)
                    {
                        x = (int)(m * (y - y1)) + x1;
                        for (xx2 = x - linewidth; xx2 <= x + linewidth; xx2++)
                            for (yy2 = y - linewidth; yy2 <= y + linewidth; yy2++)
                            {
                                if ((xx2 >= 0) && (xx2 < img_width) && (yy2 >= 0) && (yy2 < img_height))
                                {
                                    int n = ((img_width * yy2) + xx2) * 3;
                                    img[n] = (Byte)b;
                                    img[n + 1] = (Byte)g;
                                    img[n + 2] = (Byte)r;
                                }
                            }

                        y += step_y;
                    }
                }
            }
        }


        #endregion
    }
}
