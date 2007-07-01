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
    public class selfopt
    {
        Random rnd = new Random();

        public int parameters_per_instance;

        //store the history of best scores
        private int scoreHistoryIndex = 0;
        private float[] bestScoreHistory;
        private float[] scoreHistory;

        // phase space scores, only in 2D
        private float[, ,] phase_space_scores;

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
        public String allHistoryFilename = "optimiser_all.dat";

        #region "initialisation functions"

        private void init(int no_of_instances, int parameters_per_instance)
        {
            this.parameters_per_instance = parameters_per_instance;
            instance = new selfoptInstance[no_of_instances];
            for (int i = 0; i < no_of_instances; i++)
                instance[i] = new selfoptInstance(parameters_per_instance);

            parameterName = new String[parameters_per_instance];

            // store a few best scores
            bestScoreHistory = new float[1000];
            scoreHistory = new float[1000];

            // create an array to keep track of phase space scores
            phase_space_scores = new float[50, 50, 2];
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

            // update the phase space scores
            float phase_x = 0, phase_y = 0;
            instance[current_instance_index].getPhaseSpacePosition(ref phase_x, ref phase_y);
            int px = (int)Math.Round(phase_x * (phase_space_scores.GetLength(0) - 1));
            int py = (int)Math.Round(phase_y * (phase_space_scores.GetLength(1) - 1));
            phase_space_scores[px, py, 0] += score;
            phase_space_scores[px, py, 1]++;

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
            scoreHistoryIndex++;
            if (scoreHistoryIndex >= bestScoreHistory.Length)
            {
                shuffleScoreHistory();
            }
            bestScoreHistory[scoreHistoryIndex] = best_score;
            scoreHistory[scoreHistoryIndex] = score;

            //go to the next instance whose score has not yet been calculated
            nextInstance();
        }

        /// <summary>
        /// shuffles the score history along and archives old values
        /// so that we can maintain a record of the complete history
        /// </summary>
        private void shuffleScoreHistory()
        {
            int index = scoreHistory.Length / 2;

            // archive old values to file            
            FileStream fp = null;
            if (File.Exists(allHistoryFilename))
                fp = new FileStream(allHistoryFilename, FileMode.Append);
            else
                fp = new FileStream(allHistoryFilename, FileMode.Create);

            BinaryWriter binfile = new BinaryWriter(fp);

            int i = 0;
            for (i = 0; i < index; i++)
                binfile.Write(scoreHistory[i]);

            binfile.Close();
            fp.Close();

            // reorganise the history
            while (i < scoreHistory.Length)
            {
                scoreHistory[i - index] = scoreHistory[i];
                scoreHistory[i] = 0;
                bestScoreHistory[i - index] = bestScoreHistory[i];
                bestScoreHistory[i] = 0;
                i++;
            }
            scoreHistoryIndex = index;
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
            if (last_improved >= instance.Length * 2)
            {
                // get the phase space position of the best individual
                float phase_x = 0, phase_y = 0;
                instance[best_index].getPhaseSpacePosition(ref phase_x, ref phase_y);

                seedBest();

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
        /// return parameters for the best instance
        /// </summary>
        /// <returns></returns>
        public String[] getBestInstanceAsString()
        {
            String[] result = instance[best_index].AsString();
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = parameterName[i] + " = " + result[i];
            }
            return (result);
        }


        /// <summary>
        /// return the current instance
        /// </summary>
        /// <returns></returns>
        public selfoptInstance getCurrentInstance()
        {
            return (instance[current_instance_index]);
        }

        public selfoptInstance getBestInstance()
        {
            return (instance[best_index]);
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

        /// <summary>
        /// seed using the given values
        /// </summary>
        /// <param name="values">string containing comma separated values</param>
        /// <param name="variance"></param>
        public void seed(String values, float variance)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                instance[i].seed(rnd, values, variance);
            }
        }

        /// <summary>
        /// seed around the given phase space position
        /// </summary>
        /// <param name="phase_x">phase space x coordinate</param>
        /// <param name="phase_y">phase space y coordinate</param>
        /// <param name="variance"></param>
        private void seed(float phase_x, float phase_y, float variance)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                instance[i].seed(rnd, phase_x, phase_y, variance);
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

        /// <summary>
        /// returns the average phase space position for all individuals in the population
        /// This can be used to relocate the population if it gets stuck in one area
        /// </summary>
        /// <param name="av_phase_x">average x coordinate within the phase space</param>
        /// <param name="av_phase_y">average y coordinate within the phase space</param>
        private void getAveragePhaseSpacePosition(ref float av_phase_x, ref float av_phase_y)
        {
            float phase_x = 0, phase_y = 0;
            av_phase_x = 0;
            av_phase_y = 0;

            for (int i = 0; i < instance.Length; i++)
            {
                instance[i].getPhaseSpacePosition(ref phase_x, ref phase_y);
                av_phase_x += phase_x;
                av_phase_y += phase_y;
            }
            av_phase_x /= instance.Length;
            av_phase_y /= instance.Length;
        }

        #endregion

        #region"saving and loading"

        public void save(String filename)
        {
            FileStream fp = new FileStream(filename, FileMode.Create);
            BinaryWriter binfile = new BinaryWriter(fp);

            binfile.Write(instance.Length);
            binfile.Write(instance[0].parameter.Length);

            binfile.Write(scoreHistoryIndex);

            for (int i = 0; i < scoreHistoryIndex; i++)
            {
                binfile.Write(bestScoreHistory[i]);
                binfile.Write(scoreHistory[i]);
            }

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

            binfile.Write((int)phase_space_scores.GetLength(0));
            binfile.Write((int)phase_space_scores.GetLength(1));
            for (int phase_x = 0; phase_x < phase_space_scores.GetLength(0); phase_x++)
                for (int phase_y = 0; phase_y < phase_space_scores.GetLength(1); phase_y++)
                {
                    binfile.Write(phase_space_scores[phase_x, phase_y, 0]);
                    binfile.Write(phase_space_scores[phase_x, phase_y, 1]);
                }

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

                scoreHistoryIndex = binfile.ReadInt32();
                for (int i = 0; i < scoreHistoryIndex; i++)
                {
                    bestScoreHistory[i] = binfile.ReadSingle();
                    scoreHistory[i] = binfile.ReadSingle();
                }

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

                int px = binfile.ReadInt32();
                int py = binfile.ReadInt32();
                phase_space_scores = new float[px, py, 2];
                for (int phase_x = 0; phase_x < px; phase_x++)
                    for (int phase_y = 0; phase_y < py; phase_y++)
                    {
                        phase_space_scores[phase_x, phase_y, 0] = binfile.ReadSingle();
                        phase_space_scores[phase_x, phase_y, 1] = binfile.ReadSingle();
                    }

                binfile.Close();
                fp.Close();
            }
        }

        /// <summary>
        /// load the entire score history from file
        /// </summary>
        private ArrayList loadEntireHistory(String filename)
        {
            ArrayList all_scores = new ArrayList();

            if (File.Exists(filename))
            {
                FileStream fp = new FileStream(filename, FileMode.Open);
                BinaryReader binfile = new BinaryReader(fp);

                bool file_finished = false;
                while (!file_finished)
                {
                    try
                    {
                        float score = binfile.ReadSingle();
                        all_scores.Add(score);
                    }
                    catch
                    {
                        file_finished = true;
                    }
                }

                for (int i = scoreHistory.Length / 2; i < scoreHistoryIndex; i++)
                    all_scores.Add((float)scoreHistory[i]);

                binfile.Close();
                fp.Close();
            }
            else
            {
                for (int i = 0; i < scoreHistoryIndex; i++)
                    all_scores.Add((float)scoreHistory[i]);
            }
            return (all_scores);
        }


        #endregion

        #region "drawing functions"

        /// <summary>
        /// displays the population distribution within a phase space diagram
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="point_radius">radius used to draw each individual</param>
        public void showPhaseSpacePopulation(byte[] img,
                                             int img_width, int img_height,
                                             int point_radius)
        {
            float phase_x = 0, phase_y = 0;

            // clear background
            for (int i = 0; i < img.Length; i++) img[i] = 240;

            for (int i = 0; i < instance.Length; i++)
            {
                instance[i].getPhaseSpacePosition(ref phase_x, ref phase_y);
                int x = (int)(phase_x * img_width);
                if (x >= img_width) x = img_width - 1;
                int y = (int)(phase_y * img_height);
                if (y >= img_height) y = img_height - 1;

                for (int xx = x - point_radius; xx <= x + point_radius; xx++)
                {
                    int dx = xx - x;
                    for (int yy = y - point_radius; yy <= y + point_radius; yy++)
                    {
                        int dy = yy - y;
                        int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));
                        if (dist <= point_radius)
                        {
                            int n = ((yy * img_width) + xx) * 3;
                            for (int col = 0; col < 3; col++)
                                img[n + col] = (byte)100;
                        }
                    }
                }

            }
        }

        /// <summary>
        /// displays the scores obtained within different areas of the phase space
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showPhaseSpaceScores(byte[] img,
                                         int img_width, int img_height)
        {
            // get the maximum score
            float max_score = 0;
            for (int phase_x = 0; phase_x < phase_space_scores.GetLength(0); phase_x++)
            {
                for (int phase_y = 0; phase_y < phase_space_scores.GetLength(1); phase_y++)
                {
                    if (phase_space_scores[phase_x, phase_y, 1] > 0)
                    {
                        float score = phase_space_scores[phase_x, phase_y, 0] /
                                      phase_space_scores[phase_x, phase_y, 1];
                        if (score > max_score) max_score = score;
                    }
                }
            }

            if (max_score > 0)
            {
                for (int x = 0; x < img_width; x++)
                {
                    int px = x * (phase_space_scores.GetLength(0) - 1) / img_width;
                    for (int y = 0; y < img_height; y++)
                    {
                        int py = y * (phase_space_scores.GetLength(1) - 1) / img_height;
                        byte value = 0;
                        if (phase_space_scores[px, py, 1] > 0)
                        {
                            value = (byte)((phase_space_scores[px, py, 0] / phase_space_scores[px, py, 1]) *
                                            255 / max_score);
                        }
                        int n = ((y * img_width) + x) * 3;
                        img[n] = value;
                        img[n + 1] = value;
                        img[n + 2] = 0;
                    }
                }
            }
        }


        /// <summary>
        /// show the score history within the given bitmap
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showHistory(byte[] img, int img_width, int img_height)
        {
            // clear background
            for (int i = 0; i < img.Length; i++) img[i] = 240;

            // get the maximum score
            float max_score_best = 0.01f;
            float max_score = 0.01f;
            for (int i = 0; i < bestScoreHistory.Length; i++)
            {
                if (bestScoreHistory[i] > max_score_best) max_score_best = bestScoreHistory[i];
                if (scoreHistory[i] > max_score) max_score = scoreHistory[i];
            }
            max_score *= 1.1f;
            max_score_best *= 1.1f;

            int x, y;
            int prev_x = 0;
            int prev_y = 0;
            int x_best, y_best;
            int prev_x_best = 0;
            int prev_y_best = 0;
            for (int t = 0; t < bestScoreHistory.Length; t++)
            {
                x = t * (img_width - 1) / bestScoreHistory.Length;
                y = img_height - 1 - (int)(scoreHistory[t] / max_score * img_height);
                x_best = t * (img_width - 1) / bestScoreHistory.Length;
                y_best = img_height - 1 - (int)(bestScoreHistory[t] / max_score_best * img_height);

                if (t > 0)
                {
                    if ((y != 0) && (prev_y != 0)) drawLine(img, img_width, img_height, x, y, prev_x, prev_y, 255, 0, 255, 0);
                    drawLine(img, img_width, img_height, x_best, y_best, prev_x_best, prev_y_best, 0, 0, 0, 0);
                }

                prev_x = x;
                prev_y = y;
                prev_x_best = x_best;
                prev_y_best = y_best;
            }
        }

        /// <summary>
        /// show the full score history within the given bitmap
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showEntireHistory(Byte[] img, int img_width, int img_height)
        {
            ArrayList scores = loadEntireHistory(allHistoryFilename);

            for (int i = 0; i < img.Length; i++) img[i] = 240;

            if (scores.Count > 0)
            {
                float max_score = 0.01f;
                for (int i = 0; i < scores.Count; i++)
                {
                    float sc = (float)scores[i];
                    if (sc > max_score) max_score = sc;
                }
                max_score *= 1.1f;

                int x, y;
                int prev_x = 0;
                int prev_y = 0;
                int x_best, y_best;
                int prev_x_best = 0;
                int prev_y_best = 0;
                float best_sc = 0;
                for (int t = 0; t < scores.Count; t++)
                {
                    float sc = (float)scores[t];
                    if (((sc < best_sc) && (smallerScoresAreBetter)) ||
                        ((sc > best_sc) && (!smallerScoresAreBetter)) ||
                        (t == 0))
                        best_sc = sc;

                    x = t * (img_width - 1) / scores.Count;
                    y = img_height - 1 - (int)(sc / max_score * img_height);
                    x_best = t * (img_width - 1) / scores.Count;
                    y_best = img_height - 1 - (int)(best_sc / max_score * img_height);

                    if (t > 0)
                    {
                        if ((y != 0) && (prev_y != 0)) drawLine(img, img_width, img_height, x, y, prev_x, prev_y, 255, 0, 255, 0);
                        drawLine(img, img_width, img_height, x_best, y_best, prev_x_best, prev_y_best, 0, 0, 0, 0);
                    }

                    prev_x = x;
                    prev_y = y;
                    prev_x_best = x_best;
                    prev_y_best = y_best;
                }
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