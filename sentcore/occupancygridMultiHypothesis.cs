using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    /// <summary>
    /// occupancy grid cell capable of storing multiple hypotheses
    /// </summary>
    public class occupancygridCellMultiHypothesis
    {
        // current best estimate of whether this cell is occupied or not
        // note that unknown cells are simply null pointers
        // This boolean value is convenient for display purposes
        public bool occupied;

        // list of occupancy hypotheses, of type particleGridCell
        public ArrayList Hypothesis;

        /// <summary>
        /// returns the probability of occupancy at this grid cell
        /// warning: this could potentially suffer from path ID rollover problems
        /// </summary>
        /// <param name="path_ID"></param>
        /// <returns>probability value</returns>
        public float GetProbability(particlePose pose)
        {           
            float probabilityLogOdds = 0;

            if (pose.previous_paths != null)
            {
                UInt32 curr_path_ID = UInt32.MaxValue;
                int i = Hypothesis.Count - 1;
                // cycle through the path IDs for this pose            
                for (int p = 0; p < pose.previous_paths.Count; p++)
                {
                    UInt32 path_ID = (UInt32)pose.previous_paths[p];
                    while ((i >= 0) && (curr_path_ID >= path_ID))
                    {
                        particleGridCell h = (particleGridCell)Hypothesis[i];
                        curr_path_ID = h.pose.path_ID;
                        if (curr_path_ID == path_ID) probabilityLogOdds += h.probabilityLogOdds;
                        i--;
                    }
                }
            }
            return (util.LogOddsToProbability(probabilityLogOdds));
        }

        public occupancygridCellMultiHypothesis()
        {
            occupied = false;
            Hypothesis = new ArrayList();
        }
    }

    /// <summary>
    /// two dimensional grid storing multiple occupancy hypotheses
    /// </summary>
    public class occupancygridMultiHypothesis : pos3D
    {
        // the number of cells across
        public int dimension_cells;

        // size of each grid cell in millimetres
        public int cellSize_mm;

        // cells of the grid
        occupancygridCellMultiHypothesis[,] cell;

        #region "initialisation"

        /// <summary>
        /// initialise the grid
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        private void init(int dimension_cells, int cellSize_mm)
        {
            this.dimension_cells = dimension_cells;
            this.cellSize_mm = cellSize_mm;
            cell = new occupancygridCellMultiHypothesis[dimension_cells, dimension_cells];
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_cells">number of cells across</param>
        /// <param name="cellSize_mm">size of each grid cell in millimetres</param>
        public occupancygridMultiHypothesis(int dimension_cells, int cellSize_mm) : base(0,0,0)
        {
            init(dimension_cells, cellSize_mm);
        }

        #endregion

        #region "sensor model"

        /// <summary>
        /// function for vacancy within the sensor model
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        private float vacancyFunction(float fraction, int steps)
        {
            float min_vacancy_probability = 0.0f;
            float max_vacancy_probability = 0.1f;
            float prob = min_vacancy_probability + ((max_vacancy_probability - min_vacancy_probability) *
                         (float)Math.Exp(-(fraction * fraction)));
            return (0.5f - (prob / steps));
        }

        #endregion

        #region "grid update"

        /// <summary>
        /// removes an occupancy hypothesis from a grid cell
        /// </summary>
        /// <param name="hypothesis"></param>
        public void Remove(particleGridCell hypothesis)
        {
            cell[hypothesis.x, hypothesis.y].Hypothesis.Remove(hypothesis);
        }

        /// <summary>
        /// inserts the given ray into the grid
        /// </summary>
        /// <param name="ray"></param>
        public float Insert(evidenceRay ray, particlePose origin)
        {
            const int OCCUPIED_SENSORMODEL = 0;
            const int VACANT_SENSORMODEL = 1;
            float xdist_mm=0, ydist_mm=0, zdist_mm=0, x=0, y=0, z=0, occ;
            float prob = 0;
            float matchingScore = 0;  // total localisation matching score
            int rayWidth = 0;
            int widest_point;
            particleGridCell hypothesis;

            rayWidth = (int)(ray.width / cellSize_mm);
            if (rayWidth < 2) rayWidth = 2;

            for (int j = OCCUPIED_SENSORMODEL; j <= VACANT_SENSORMODEL; j++)
            {
                if (j == OCCUPIED_SENSORMODEL)
                {
                    // distance between the beginning and end of the probably
                    // occupied area
                    xdist_mm = ray.vertices[1].x - ray.vertices[0].x;
                    ydist_mm = ray.vertices[1].y - ray.vertices[0].y;
                    zdist_mm = ray.vertices[1].z - ray.vertices[0].z;
                    x = ray.vertices[0].x;
                    y = ray.vertices[0].y;
                    z = ray.vertices[0].z;
                }

                if (j == VACANT_SENSORMODEL)
                {
                    // distance between the observation point (pose) and the beginning of
                    // the probably occupied area of the sensor model
                    xdist_mm = ray.vertices[0].x - ray.observedFrom.x;
                    ydist_mm = ray.vertices[0].y - ray.observedFrom.y;
                    zdist_mm = ray.vertices[0].z - ray.observedFrom.z;
                    x = ray.observedFrom.x;
                    y = ray.observedFrom.y;
                    z = ray.observedFrom.z;
                }

                // which is the longest axis ?
                int longest_axis = 0;
                float longest = Math.Abs(xdist_mm);
                if (Math.Abs(ydist_mm) > longest)
                {
                    // y has the longest length
                    longest = Math.Abs(ydist_mm);
                    longest_axis = 1;
                }

                int steps = (int)(longest / cellSize_mm);
                if (j == 0)
                    widest_point = (int)(ray.fattestPoint * steps / ray.length);
                else
                    widest_point = steps;
                float x_incr = xdist_mm / steps;
                float y_incr = ydist_mm / steps;
                float z_incr = zdist_mm / steps;

                int i = 0;
                while (i < steps)
                {
                    // calculate the width of the ray at this point
                    // using a diamond shape ray model
                    int ray_wdth = 0;
                    if (i < widest_point)
                        ray_wdth = i * rayWidth / widest_point;
                    else
                        ray_wdth = (steps - i + widest_point) * rayWidth / (steps - widest_point);

                    x += x_incr;
                    y += y_incr;
                    z += z_incr;
                    int x_cell = (int)Math.Round(x / (float)cellSize_mm);
                    if ((x_cell > -1) && (x_cell < dimension_cells))
                    {
                        int y_cell = (int)Math.Round(y / (float)cellSize_mm);
                        if ((y_cell > -1) && (y_cell < dimension_cells))
                        {
                            // get the probability at this point using the sensor model
                            if (j == OCCUPIED_SENSORMODEL)
                                prob = 0; // TODO  ray.probability(x, y, gaussLookup, forwardBias);
                            else
                                // calculate the probability from the vacancy model
                                prob = vacancyFunction(i / (float)steps, steps);

                            if (cell[x_cell, y_cell] == null)
                            {
                                // generate a grid cell if necessary
                                cell[x_cell, y_cell] = new occupancygridCellMultiHypothesis();
                            }
                            else
                            {
                                // only localise using occupancy, not vacancy
                                if (j == OCCUPIED_SENSORMODEL)
                                {
                                    // localise using this grid cell
                                    // first get the existing probability value at this cell
                                    occ = cell[x_cell, y_cell].GetProbability(origin);
                                    // combine the results
                                    float prob2 = ((prob * occ) + ((1.0f - prob) * (1.0f - occ)));
                                    // update the localisation matching score
                                    matchingScore += util.LogOdds(prob2);
                                }
                            }

                            // add a new hypothesis to this grid coordinate
                            // note that this is also added to the original pose
                            hypothesis = new particleGridCell(x_cell, y_cell, prob, origin);
                            cell[x_cell, y_cell].Hypothesis.Add(hypothesis);
                            origin.observed_grid_cells.Add(hypothesis);

                        }
                        else i = steps;
                    }
                    else i = steps;
                    i++;
                }
            }

            return (matchingScore);
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show the grid map as an image
        /// </summary>
        /// <param name="img">bitmap image</param>
        /// <param name="width">width in pixels</param>
        /// <param name="height">height in pixels</param>
        public void Show(Byte[] img, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                int cell_y = y * (dimension_cells-1) / height;
                for (int x = 0; x < width; x++)
                {
                    int cell_x = x * (dimension_cells - 1) / width;
                    int n = ((y * width) + x) * 3;

                    for (int c = 0; c < 3; c++)
                    {
                        if (cell[cell_x, cell_y] == null)
                        {
                            img[n + c] = (Byte)255;
                        }
                        else
                        {
                            if (cell[cell_x,cell_y].occupied)
                                img[n + c] = (Byte)0;
                            else
                                img[n + c] = (Byte)200;
                        }
                    }
                }
            }
        }

        #endregion
    }

}
