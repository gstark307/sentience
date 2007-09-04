/*
    Stuff for dealing with 2D grids, such as calibration charts.  
    Note that this has nothing to do with occupancy grids.
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


namespace sluggish.utilities.grids
{
    /// <summary>
    /// a 2D grid cell intended for use when applying 2D grids to images
    /// for applications such as checkerboard calibration
    /// </summary>
    public class grid2Dcell
    {
        // a 2D polygon with four vertices describing
        // the grid cell position within the image
        public polygon2D perimeter;

        // the probability of this grid cell being "on"
        // in the range 0.0 - 1.0
        public float occupancy;

        // the number of pixels inside the grid cell
        public float pixels;

        /// <summary>
        /// returns the centre of the square cell
        /// </summary>
        /// <param name="centre_x">x centre position</param>
        /// <param name="centre_y">y centre position</param>
        public void GetCentre(ref float centre_x, ref float centre_y)
        {
            if (perimeter != null)
            {
                perimeter.GetSquareCentre(ref centre_x, ref centre_y);
            }
        }
    }

    /// <summary>
    /// class which handles creating, detecting and drawing 2D grids
    /// </summary>
    public class grid2D
    {
        // perimeter of the grid
        public polygon2D perimeter;

        // if a border has been added this is the border perimeter
        public polygon2D border_perimeter;

        // cells within the grid
        public grid2Dcell[,] cell;

        // interception points between lines
        protected float[, ,] line_intercepts;

        // horizontal and vertical lines which make up the grid
        protected ArrayList[] line;

        #region "initialisation"

        /// <summary>
        /// creates grid cells based upon line interception points
        /// </summary>
        private void initialiseCells()
        {
            int cells_across = line_intercepts.GetLength(0);
            int cells_down = line_intercepts.GetLength(1);

            cell = new grid2Dcell[cells_across - 1, cells_down - 1];

            for (int x = 0; x < cells_across - 1; x++)
            {
                for (int y = 0; y < cells_down - 1; y++)
                {
                    cell[x, y] = new grid2Dcell();
                    cell[x, y].perimeter = new polygon2D();
                    cell[x, y].perimeter.Add(line_intercepts[x, y, 0], line_intercepts[x, y, 1]);
                    cell[x, y].perimeter.Add(line_intercepts[x + 1, y, 0], line_intercepts[x + 1, y, 1]);
                    cell[x, y].perimeter.Add(line_intercepts[x + 1, y + 1, 0], line_intercepts[x + 1, y + 1, 1]);
                    cell[x, y].perimeter.Add(line_intercepts[x, y + 1, 0], line_intercepts[x, y + 1, 1]);
                }
            }
        }

        /// <summary>
        /// finds the interception points between grid lines
        /// </summary>
        private void poltLineIntercepts()
        {
            if (line != null)
            {
                // create an array to store the line intercepts
                int intercepts_x = line[0].Count / 4;
                int intercepts_y = line[1].Count / 4;
                line_intercepts = new float[intercepts_x, intercepts_y, 2];

                for (int i = 0; i < line[0].Count; i += 4)
                {
                    // get the first line coordinates
                    ArrayList lines0 = line[0];
                    float x0 = (float)lines0[i];
                    float y0 = (float)lines0[i + 1];
                    float x1 = (float)lines0[i + 2];
                    float y1 = (float)lines0[i + 3];

                    for (int j = 0; j < line[1].Count; j += 4)
                    {
                        // get the second line coordinates
                        ArrayList lines1 = line[1];
                        float x2 = (float)lines1[j];
                        float y2 = (float)lines1[j + 1];
                        float x3 = (float)lines1[j + 2];
                        float y3 = (float)lines1[j + 3];

                        // find the interception between the two lines
                        float ix = 0, iy = 0;
                        geometry.intersection(x0, y0, x1, y1, x2, y2, x3, y3, ref ix, ref iy);

                        // store the intercept position
                        line_intercepts[i / 4, j / 4, 0] = ix;
                        line_intercepts[i / 4, j / 4, 1] = iy;
                    }
                }

                // update the perimeter
                border_perimeter = new polygon2D();
                float xx = line_intercepts[0, 0, 0];
                float yy = line_intercepts[0, 0, 1];
                border_perimeter.Add(xx, yy);
                xx = line_intercepts[intercepts_x - 1, 0, 0];
                yy = line_intercepts[intercepts_x - 1, 0, 1];
                border_perimeter.Add(xx, yy);
                xx = line_intercepts[intercepts_x - 1, intercepts_y - 1, 0];
                yy = line_intercepts[intercepts_x - 1, intercepts_y - 1, 1];
                border_perimeter.Add(xx, yy);
                xx = line_intercepts[0, intercepts_y - 1, 0];
                yy = line_intercepts[0, intercepts_y - 1, 1];
                border_perimeter.Add(xx, yy);
            }
        }

        private void init(int dimension_x, int dimension_y,
                          polygon2D perimeter, int border_cells)
        {
            this.perimeter = perimeter;

            cell = new grid2Dcell[dimension_x, dimension_y];

            line = new ArrayList[2];

            int index = 0;
            for (int i = 0; i < 2; i++)
            {
                line[i] = new ArrayList();

                int idx1 = index + i;
                if (idx1 >= 4) idx1 -= 4;
                int idx2 = index + i + 1;
                if (idx2 >= 4) idx2 -= 4;
                float x0 = (float)perimeter.x_points[idx1];
                float y0 = (float)perimeter.y_points[idx1];
                float x1 = (float)perimeter.x_points[idx2];
                float y1 = (float)perimeter.y_points[idx2];

                float w0 = Math.Abs(x1 - x0);
                float h0 = Math.Abs(y1 - y0);

                int idx3 = index + i + 2;
                if (idx3 >= 4) idx3 -= 4;
                int idx4 = index + i + 3;
                if (idx4 >= 4) idx4 -= 4;
                float x2 = (float)perimeter.x_points[idx3];
                float y2 = (float)perimeter.y_points[idx3];
                float x3 = (float)perimeter.x_points[idx4];
                float y3 = (float)perimeter.y_points[idx4];

                float w1 = Math.Abs(x3 - x2);
                float h1 = Math.Abs(y3 - y2);

                int dimension = dimension_x;
                if (h0 > w0) dimension = dimension_y;

                for (int j = -border_cells; j <= dimension + border_cells; j++)
                {
                    // locate the position along the first line
                    float xx0, yy0;  // position along the first line

                    if (w0 > h0)
                    {
                        float grad = (y1 - y0) / (x1 - x0);
                        if (x1 > x0)
                            xx0 = x0 + (w0 * j / dimension);
                        else
                            xx0 = x0 - (w0 * j / dimension);
                        yy0 = y0 + ((xx0 - x0) * grad);
                    }
                    else
                    {
                        float grad = (x1 - x0) / (y1 - y0);
                        if (y1 > y0)
                            yy0 = y0 + (h0 * j / dimension);
                        else
                            yy0 = y0 - (h0 * j / dimension);
                        xx0 = x0 + ((yy0 - y0) * grad);
                    }

                    // locate the position along the second line
                    float xx1, yy1;  // position along the second line

                    if (w1 > h1)
                    {
                        float grad = (y2 - y3) / (x2 - x3);
                        if (x2 > x3)
                            xx1 = x3 + (w1 * j / dimension);
                        else
                            xx1 = x3 - (w1 * j / dimension);
                        yy1 = y3 + ((xx1 - x3) * grad);
                    }
                    else
                    {
                        float grad = (x2 - x3) / (y2 - y3);
                        if (y2 > y3)
                            yy1 = y3 + (h1 * j / dimension);
                        else
                            yy1 = y3 - (h1 * j / dimension);
                        xx1 = x3 + ((yy1 - y3) * grad);
                    }

                    // add the line to the list
                    line[i].Add(xx0);
                    line[i].Add(yy0);
                    line[i].Add(xx1);
                    line[i].Add(yy1);
                }
            }

            // find interceptions between lines
            poltLineIntercepts();

            // create grid cells
            initialiseCells();
        }

        #endregion

        #region "constructors"

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_x">number of cells across the grid</param>
        /// <param name="dimension_y">number of cells down the grid</param>
        public grid2D(int dimension_x, int dimension_y)
        {
            cell = new grid2Dcell[dimension_x, dimension_y];
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_x">number of cells across the grid</param>
        /// <param name="dimension_y">number of cells down the grid</param>
        /// <param name="perimeter">perimeter of the grid</param>
        /// <param name="border_cells">number of cells to use as a border around the grid</param>
        public grid2D(int dimension_x, int dimension_y,
                      polygon2D perimeter, int border_cells)
        {
            init(dimension_x, dimension_y, perimeter, border_cells);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_x">number of cells across the grid</param>
        /// <param name="dimension_y">number of cells down the grid</param>
        /// <param name="cx">centre of the grid</param>
        /// <param name="cy">centre of the grid</param>
        /// <param name="orientation">orientation of the grid in radians</param>
        /// <param name="average_spacing_x">grid spacing in the x axis</param>
        /// <param name="average_spacing_y">grid spacing in the y axis</param>
        /// <param name="phase_offset_x">phase offset in the x axis</param>
        /// <param name="phase_offset_y">phase offset in the y axis</param>
        /// <param name="border_cells">number of cells to use as a border around the grid</param>
        public grid2D(int dimension_x, int dimension_y,
                      float cx, float cy,
                      float orientation,
                      float average_spacing_x, float average_spacing_y,
                      float phase_offset_x, float phase_offset_y,
                      int border_cells)
        {
            // create a perimeter region
            polygon2D perimeter = new polygon2D();

            float length_x = average_spacing_x * dimension_x;
            float length_y = average_spacing_y * dimension_y;
            float half_length_x = length_x / 2;
            float half_length_y = length_y / 2;

            // adjust for phase
            cx += (average_spacing_x * phase_offset_x / (2 * (float)Math.PI)) * (float)Math.Sin(orientation);
            cy -= (average_spacing_y * phase_offset_y / (2 * (float)Math.PI)) * (float)Math.Cos(orientation);

            // find the mid point of the top line
            float px1 = cx + (half_length_y * (float)Math.Sin(orientation));
            float py1 = cy + (half_length_y * (float)Math.Cos(orientation));

            // find the top left vertex
            float x0 = px1 + (half_length_x * (float)Math.Sin(orientation - (float)(Math.PI / 2)));
            float y0 = py1 + (half_length_x * (float)Math.Cos(orientation - (float)(Math.PI / 2)));

            // find the top right vertex
            float x1 = px1 + (half_length_x * (float)Math.Sin(orientation + (float)(Math.PI / 2)));
            float y1 = py1 + (half_length_x * (float)Math.Cos(orientation + (float)(Math.PI / 2)));

            // find the bottom vertices by mirroring around the centre
            float x2 = cx + (cx - x0);
            float y2 = cy + (cy - y0);
            float x3 = cx - (x1 - cx);
            float y3 = cy - (y1 - cy);

            // update polygon with the perimeter vertices
            perimeter.Add(x0, y0);
            perimeter.Add(x1, y1);
            perimeter.Add(x2, y2);
            perimeter.Add(x3, y3);

            int dim_x = dimension_x;
            int dim_y = dimension_y;
            float first_side_length = perimeter.getSideLength(0);
            float second_side_length = perimeter.getSideLength(1);
            if (((dimension_x > dimension_y + 2) &&
                 (second_side_length > first_side_length)) ||
                 ((dimension_y > dimension_x + 2) &&
                 (first_side_length > second_side_length)))
            {
                dim_x = dimension_y;
                dim_y = dimension_x;
            }

            // initialise using this perimeter
            init(dim_x, dim_y, perimeter, border_cells);
        }

        #endregion

        #region "grid spacing detection from features"

        // quantisation used when detecting grid intersections with horizontal and vertical axes
        private const float quantisation = 2.0f;

        /// <summary>
        /// detect the wavelength and phase offset for the given responses
        /// </summary>
        /// <param name="response">array containing responses, which we assume contains some frequency</param>
        /// <param name="known_frequency">known value for the frequency, in which case we just calculate the phase offset</param>
        /// <param name="frequency">detected frequency</param>
        /// <param name="phase_offset">detected phase offset</param>
        /// <param name="amplitude">mean variance of the response</param>
        /// <param name="min_grid_spacing">minimum cell diameter in pixels</param>
        /// <param name="max_grid_spacing">maximum cell diameter in pixels</param>
        /// <returns>minimum squared difference value for the best match</returns>
        private static float DetectGridFrequency(int[] response,
                                                 float known_frequency,
                                                 ref float frequency,
                                                 ref float phase_offset,
                                                 ref float amplitude,
                                                 float min_grid_spacing,
                                                 float max_grid_spacing)
        {
            float total_response = 0;

            // find the maximum value
            int max_response = 0;
            int start_index = -1;
            int end_index = -1;
            float average_response = 0;
            int average_response_hits = 0;
            for (int j = 0; j < response.Length; j++)
            {
                // average response
                if (response[j] > 0)
                {
                    average_response += response[j];
                    average_response_hits++;
                }

                if (response[j] > max_response) max_response = response[j];
                if (response[j] > 0)
                {
                    end_index = j;
                    if (start_index == -1) start_index = j;
                }
            }
            if (average_response_hits > 0) average_response /= average_response_hits;

            float average_variance = 0;
            for (int j = 0; j < response.Length; j++)
            {
                if (response[j] > 0)
                {
                    average_variance += Math.Abs(response[j] - average_response);
                }
            }
            if (average_response_hits > 0) average_variance /= average_response_hits;

            amplitude = average_variance;

            // test each frequency against the data
            phase_offset = 0;
            float min_value = 0;
            float min_frequency = min_grid_spacing / quantisation;
            float max_frequency = max_grid_spacing / quantisation;
            int phase_steps = 100;
            int frequency_steps = (int)((max_frequency - min_frequency) / 0.05f);
            if (max_frequency <= min_frequency) max_frequency = min_frequency + 1;
            float min_phase_offset = -(float)Math.PI;
            float max_phase_offset = (float)Math.PI;

            if (known_frequency > 0)
            {
                // here we already know the frequency, so just need to find the phase
                min_frequency = (int)known_frequency - 1;
                max_frequency = (int)known_frequency + 1;
                phase_steps = 200;
                frequency_steps = 100;
            }

            // seeded random number generator
            Random rnd = new Random(5826);

            // number of histogram samples to examine
            int waveform_samples = 100;

            // for each phase offset value
            for (int ph = 0; ph < phase_steps; ph++)
            {
                float offset = min_phase_offset + ((max_phase_offset - min_phase_offset) * (float)rnd.NextDouble());
                // for each possible frequency
                for (int freq = 0; freq < frequency_steps; freq++)
                {
                    float f = min_frequency + ((max_frequency - min_frequency) * (float)rnd.NextDouble());
                    // calculate the total sum of squared differences between
                    // this phase/frequency combination and the actual data
                    float current_value = 0;
                    for (int w = 0; w <= waveform_samples; w++)
                    {
                        float idx = start_index + ((end_index - start_index) * (float)rnd.NextDouble());
                        float x = idx;
                        float y = (float)Math.Sin(offset + ((x / f) * Math.PI * 2));
                        y = (y * amplitude) + average_response;

                        int response_index = (int)Math.Round(idx);
                        if ((response_index > -1) && (response_index < response.Length))
                        {
                            float diff = y - response[response_index];
                            current_value += (diff * diff * y);
                        }
                    }

                    // record the result with the least squared difference
                    if ((min_value == 0) || (current_value < min_value))
                    {
                        min_value = current_value;
                        phase_offset = offset;
                        frequency = f;
                    }

                    total_response += current_value;
                }
            }

            return (total_response);
        }


        /// <summary>
        /// detect a grid within the given image using the given orientation
        /// </summary>
        /// <param name="img">image containing only the grid</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="dominant_orientation">orientation of the grid</param>
        /// <param name="known_grid_spacing">known grid spacing (cell diameter) in pixels</param>
        /// <param name="grid_horizontal">estimated horizontal width of the grid in pixels</param>
        /// <param name="grid_vertical">estimated vertical height of the grid in pixels</param>
        /// <param name="minimum_dimension_horizontal">minimum number of cells across</param>
        /// <param name="maximum_dimension_vertical">maximum number of cells across</param>
        /// <param name="minimum_dimension_vertical">minimum number of cells down</param>
        /// <param name="maximum_dimension_vertical">maximum number of cells down</param>
        /// <param name="average_grid_spacing_horizontal">average cell diameter in pixels in the horizontal dimension</param>
        /// <param name="average_grid_spacing_vertical">average cell diameter in pixels in the vertical dimension</param>
        /// <param name="cells_horizontal">number of cells in the horizontal dimension</param>
        /// <param name="cells_vertical">number of cells in the vertical dimension</param>
        /// <param name="output_img">output image</param>
        /// <param name="output_img_width">width of the output image</param>
        /// <param name="output_img_height">height of the output image</param>
        /// <param name="output_img_type">the type of output image</param>
        private static void DetectGrid(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                       float dominant_orientation, float known_grid_spacing,
                                       float grid_horizontal, float grid_vertical,
                                       int minimum_dimension_horizontal, int maximum_dimension_horizontal,
                                       int minimum_dimension_vertical, int maximum_dimension_vertical,
                                       ref float average_grid_spacing_horizontal, ref float average_grid_spacing_vertical,
                                       ref float horizontal_phase_offset, ref float vertical_phase_offset,
                                       ref int cells_horizontal, ref int cells_vertical,
                                       ref byte[] output_img, int output_img_width, int output_img_height, int output_img_type)
        {
            int max_features_per_row = 10;
            int edge_detection_radius = 2;
            int inhibitory_radius = img_width / 50;
            int min_edge_intensity = 20;
            int max_edge_intensity = 2500;
            int minimum_pixel_intensity = 5;
            int local_averaging_radius = 500;
            int minimum_difference = 15;
            int step_size = 2;
            float average_magnitude = 0;

            if (inhibitory_radius < edge_detection_radius * 3)
                inhibitory_radius = edge_detection_radius * 3;

            ArrayList points = new ArrayList();

            // find horizontal and vertical maxima features within the image
            ArrayList[] horizontal_maxima =
                image.horizontal_maxima(img, img_width, img_height, bytes_per_pixel, max_features_per_row,
                                        edge_detection_radius, inhibitory_radius,
                                        min_edge_intensity, max_edge_intensity,
                                        minimum_pixel_intensity, local_averaging_radius,
                                        minimum_difference, step_size,
                                        ref average_magnitude);

            if (horizontal_maxima != null)
            {
                // add feature points to the list
                for (int y = 0; y < img_height; y += step_size)
                {
                    int no_of_features = horizontal_maxima[y].Count;
                    for (int i = 0; i < no_of_features; i += 2)
                    {
                        float x = (float)horizontal_maxima[y][i];
                        points.Add(x);
                        points.Add((float)y);
                    }
                }
            }

            ArrayList[] vertical_maxima =
                image.vertical_maxima(img, img_width, img_height, bytes_per_pixel, max_features_per_row,
                                      edge_detection_radius, inhibitory_radius,
                                      min_edge_intensity, max_edge_intensity,
                                      minimum_pixel_intensity, local_averaging_radius,
                                      minimum_difference, step_size,
                                      ref average_magnitude);

            if (vertical_maxima != null)
            {
                // add feature points to the list
                for (int x = 0; x < img_width; x += step_size)
                {
                    int no_of_features = vertical_maxima[x].Count;
                    for (int i = 0; i < no_of_features; i += 2)
                    {
                        float y = (float)vertical_maxima[x][i];
                        points.Add((float)x);
                        points.Add(y);
                    }
                }
            }

            // centre point from which spacing measurements are taken
            float centre_x = img_width / 2;
            float centre_y = img_height / 2;

            float distortion_angle = 0;
            int[] horizontal_buckets = null;
            int[] vertical_buckets = null;

            horizontal_phase_offset = 0;
            vertical_phase_offset = 0;

            // minimum and maximum grid spacing in pixels
            // this assists the frequency detection to search within this range
            float min_grid_spacing_horizontal = grid_horizontal / maximum_dimension_horizontal;
            float max_grid_spacing_horizontal = grid_horizontal / minimum_dimension_horizontal;
            float min_grid_spacing_vertical = grid_vertical / maximum_dimension_vertical;
            float max_grid_spacing_vertical = grid_vertical / minimum_dimension_vertical;

            // feed the image feature points into the grid detector
            DetectGrid(points, dominant_orientation, centre_x, centre_y, img_width / 2,
                       distortion_angle, known_grid_spacing,
                       min_grid_spacing_horizontal, max_grid_spacing_horizontal,
                       min_grid_spacing_vertical, max_grid_spacing_vertical,
                       ref horizontal_buckets, ref vertical_buckets,
                       ref average_grid_spacing_horizontal,
                       ref average_grid_spacing_vertical,
                       ref horizontal_phase_offset,
                       ref vertical_phase_offset);

            // estimate the number of cells in the horizontal and vertical axes
            cells_horizontal = (int)Math.Round(grid_horizontal / average_grid_spacing_horizontal);
            cells_vertical = (int)Math.Round(grid_vertical / average_grid_spacing_vertical);

            // show some output mainly for debugging purposes
            switch (output_img_type)
            {
                case 1: // feature points
                    {
                        ShowGridPoints(
                            img, img_width, img_height, bytes_per_pixel,
                            points,
                            ref output_img, output_img_width, output_img_height);
                        break;
                    }
                case 2: // grid spacing responses
                    {
                        ShowGridAxisResponsesPoints(
                            horizontal_buckets, vertical_buckets,
                            average_grid_spacing_horizontal, horizontal_phase_offset,
                            average_grid_spacing_vertical, vertical_phase_offset,
                            ref output_img, output_img_width, output_img_height);
                        break;
                    }
            }
        }

        /// <summary>
        /// detect a grid within the given image using the given perimeter polygon
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="perimeter">bounding perimeter within which the grid exists</param>
        /// <param name="minimum_dimension_horizontal">minimum number of cells across</param>
        /// <param name="maximum_dimension_horizontal">maximum number of cells across</param>
        /// <param name="minimum_dimension_vertical">minimum number of cells down</param>
        /// <param name="maximum_dimension_vertical">maximum number of cells down</param>
        /// <param name="known_grid_spacing">known grid spacing (cell diameter) value in pixels</param>
        /// <param name="known_even_dimension">set to true if it is known that the number of cells in horizontal and vertical axes is even</param>
        /// <param name="border_cells">extra cells to add as a buffer zone around the grid</param>
        /// <returns>2D grid</returns>
        public static grid2D DetectGrid(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                        polygon2D perimeter,
                                        int minimum_dimension_horizontal, int maximum_dimension_horizontal,
                                        int minimum_dimension_vertical, int maximum_dimension_vertical,
                                        float known_grid_spacing, bool known_even_dimension,
                                        int border_cells,
                                        ref float average_spacing_horizontal, ref float average_spacing_vertical,
                                        ref byte[] output_img, int output_img_type)
        {
            int tx = (int)perimeter.left();
            int ty = (int)perimeter.top();
            int bx = (int)perimeter.right();
            int by = (int)perimeter.bottom();

            int wdth = bx - tx;
            int hght = by - ty;

            // create an image of the grid area
            byte[] grid_img = image.createSubImage(img, img_width, img_height, bytes_per_pixel,
                                                   tx, ty, bx, by);

            // get the orientation of the perimeter
            float dominant_orientation = perimeter.GetSquareOrientation();

            // find the horizontal and vertical dimensions of the grid perimeter
            float grid_horizontal = perimeter.GetSquareHorizontal();
            float grid_vertical = perimeter.GetSquareVertical();

            // detect grid within the perimeter
            int cells_horizontal = 0, cells_vertical = 0;
            float horizontal_phase_offset = 0;
            float vertical_phase_offset = 0;
            DetectGrid(grid_img, wdth, hght, bytes_per_pixel, dominant_orientation, known_grid_spacing,
                       grid_horizontal, grid_vertical,
                       minimum_dimension_horizontal, maximum_dimension_horizontal,
                       minimum_dimension_vertical, maximum_dimension_vertical,
                       ref average_spacing_horizontal, ref average_spacing_vertical,
                       ref horizontal_phase_offset, ref vertical_phase_offset,
                       ref cells_horizontal, ref cells_vertical,
                       ref output_img, img_width, img_height, output_img_type);

            //if we know the number of cells should be even correct any inaccuracies
            if (known_even_dimension)
            {
                cells_horizontal = (int)(cells_horizontal / 2) * 2;
                cells_vertical = (int)(cells_vertical / 2) * 2;
            }

            //get the centre of the region
            float cx = tx + ((bx - tx) / 2);
            float cy = ty + ((by - ty) / 2);

            grid2D detectedGrid = new grid2D(cells_horizontal, cells_vertical,
                                             cx, cy, dominant_orientation,
                                             average_spacing_horizontal, average_spacing_vertical,
                                             horizontal_phase_offset, vertical_phase_offset,
                                             border_cells);

            return (detectedGrid);
        }

        /// <summary>
        /// detect grid based upon a set of points
        /// </summary>
        /// <param name="points">list of 2D points</param>
        /// <param name="dominant_orientation">orientation of the region</param>
        /// <param name="origin_x">x origin</param>
        /// <param name="origin_y">y origin</param>
        /// <param name="max_axis_length">maximum axis length</param>
        /// <param name="distortion_angle">distortion angle</param>
        /// <param name="known_grid_spacing">a known grid spacing value</param>
        /// <param name="min_grid_spacing_horizontal">minimum grid spacing in the horizontal</param>
        /// <param name="max_grid_spacing_horizontal">maximum grid spacing in the horizontal</param>
        /// <param name="min_grid_spacing_vertical">minimum grid spacing in the vertical</param>
        /// <param name="max_grid_spacing_vertical">maximum grid spacing in the vertical</param>
        /// <param name="horizontal_bucket">array containing horizontal responses</param>
        /// <param name="vertical_bucket">array containing vertical responses</param>
        /// <param name="horizontal_grid_spacing">detected horizontal spacing</param>
        /// <param name="vertical_grid_spacing">detected vertical spacing</param>
        /// <param name="horizontal_phase_offset">detected horizontal phase offset</param>
        /// <param name="vertical_phase_offset">detected vertical phase offset</param>
        private static void DetectGrid(ArrayList points,
                                       float dominant_orientation,
                                       float origin_x, float origin_y,
                                       float max_axis_length,
                                       float distortion_angle,
                                       float known_grid_spacing,
                                       float min_grid_spacing_horizontal, float max_grid_spacing_horizontal,
                                       float min_grid_spacing_vertical, float max_grid_spacing_vertical,
                                       ref int[] horizontal_bucket,
                                       ref int[] vertical_bucket,
                                       ref float horizontal_grid_spacing,
                                       ref float vertical_grid_spacing,
                                       ref float horizontal_phase_offset,
                                       ref float vertical_phase_offset)
        {
            horizontal_bucket = null;
            vertical_bucket = null;

            // create spacing responses for horizontal and vertical axes
            GetGridQuantisedSpacings(points, dominant_orientation, origin_x, origin_y,
                                     max_axis_length, distortion_angle, quantisation,
                                     ref horizontal_bucket, ref vertical_bucket);

            // convert the known frequency into a quantised value
            float known_frequency = known_grid_spacing / quantisation;

            // detect the horizontal frequency of the grid
            float horizontal_frequency = 0;
            horizontal_phase_offset = 0;
            float horizontal_amplitude = 0;
            DetectGridFrequency(horizontal_bucket,
                                known_frequency,
                                ref horizontal_frequency,
                                ref horizontal_phase_offset,
                                ref horizontal_amplitude,
                                min_grid_spacing_horizontal, max_grid_spacing_horizontal);
            horizontal_grid_spacing = horizontal_frequency * quantisation;

            // detect the vertical frequency of the grid
            float vertical_frequency = 0;
            vertical_phase_offset = 0;
            float vertical_amplitude = 0;
            DetectGridFrequency(vertical_bucket,
                                known_frequency,
                                ref vertical_frequency,
                                ref vertical_phase_offset,
                                ref vertical_amplitude,
                                min_grid_spacing_vertical, max_grid_spacing_vertical);
            vertical_grid_spacing = vertical_frequency * quantisation;
        }

        /// <summary>
        /// returns a quantised set of values for horizontal and vertical grid spacings
        /// </summary>
        /// <param name="points">list of 2D points</param>
        /// <param name="dominant_orientation">dominant orientation of the grid</param>
        /// <param name="origin_x">origin of the grid axis</param>
        /// <param name="origin_y">origin of the grid axis</param>
        /// <param name="max_axis_length">the maximum axis length</param>
        /// <param name="distortion_angle">an optional distortion angle relative to the vertical axis</param>
        /// <param name="quantisation">quantisation level used to group interceptions into buckets</param>
        /// <param name="horizontal_bucket"></param>
        /// <param name="vertical_bucket"></param>
        private static void GetGridQuantisedSpacings(ArrayList points,
                                                    float dominant_orientation,
                                                    float origin_x, float origin_y,
                                                    float max_axis_length,
                                                    float distortion_angle,
                                                    float quantisation,
                                                    ref int[] horizontal_bucket,
                                                    ref int[] vertical_bucket)
        {
            // find the horizontal and vertical axis intercepts
            ArrayList spacings = DetectSpacings(points, dominant_orientation, origin_x, origin_y, distortion_angle);

            ArrayList horizontal_intercepts = (ArrayList)spacings[0];
            ArrayList vertical_intercepts = (ArrayList)spacings[1];

            // here comes the bucket brigade
            int buckets = (int)(max_axis_length * 2 / quantisation);
            horizontal_bucket = new int[buckets];
            vertical_bucket = new int[buckets];

            for (int i = 0; i < horizontal_intercepts.Count; i++)
            {
                // get the horizontal displacement from the axis origin
                float horizontal_displacement = (float)horizontal_intercepts[i];

                // which bucket should this go into?
                int bucket = (int)Math.Round(horizontal_displacement / quantisation) + (buckets / 2);

                if ((bucket > -1) && (bucket < buckets))
                    horizontal_bucket[bucket]++;
            }

            for (int i = 0; i < vertical_intercepts.Count; i++)
            {
                // get the vertical displacement from the axis origin
                float vertical_displacement = (float)vertical_intercepts[i];

                // which bucket should this go into?
                int bucket = (int)Math.Round(vertical_displacement / quantisation) + (buckets / 2);

                if ((bucket > -1) && (bucket < buckets))
                    vertical_bucket[bucket]++;
            }
        }

        /// <summary>
        /// given a set of 2D points, which could have been derrived from any kind of feature
        /// plot these against an axis having the given dominant orientation
        /// to compute horizontal and vertical grid spacings
        /// </summary>
        /// <param name="points">list of 2D points</param>
        /// <param name="dominant_orientation">dominant orientation of the grid</param>
        /// <param name="origin_x">origin of the grid axis</param>
        /// <param name="origin_y">origin of the grid axis</param>
        /// <param name="distortion_angle">an optional distortion angle relative to the vertical axis</param>
        /// <returns>array containing horizontal and vertical interception positions</returns>
        private static ArrayList DetectSpacings(ArrayList points,
                                                float dominant_orientation,
                                                float origin_x, float origin_y,
                                                float distortion_angle)
        {
            ArrayList results = new ArrayList();

            float vertical_orientation = dominant_orientation;
            float horizontal_orientation = dominant_orientation + (float)(Math.PI / 2) + distortion_angle;

            float x_axis_origin = origin_x;
            float y_axis_origin = origin_y;

            // vertical axis
            ArrayList vertical_axis_intercepts = new ArrayList();
            float x_axis_origin_vertical = x_axis_origin + (float)Math.Sin(vertical_orientation);
            float y_axis_origin_vertical = y_axis_origin + (float)Math.Cos(vertical_orientation);

            // horizontal axis
            ArrayList horizontal_axis_intercepts = new ArrayList();
            float x_axis_origin_horizontal = x_axis_origin + (float)Math.Sin(horizontal_orientation);
            float y_axis_origin_horizontal = y_axis_origin + (float)Math.Cos(horizontal_orientation);

            for (int i = 0; i < points.Count; i += 2)
            {
                // point coordinate
                float x = (float)points[i];
                float y = (float)points[i + 1];

                // vertical projection of the point
                float x_vertical = x + (float)Math.Sin(vertical_orientation);
                float y_vertical = y + (float)Math.Cos(vertical_orientation);

                // horizontal projection of the point
                float x_horizontal = x + (float)Math.Sin(horizontal_orientation);
                float y_horizontal = y + (float)Math.Cos(horizontal_orientation);

                // interception point
                float intercept_x = 0, intercept_y = 0;

                // find the interception of this point with the vertical axis
                sluggish.utilities.geometry.intersection(x_axis_origin, y_axis_origin,
                                                         x_axis_origin_vertical, y_axis_origin_vertical,
                                                         x, y, x_horizontal, y_horizontal,
                                                         ref intercept_x, ref intercept_y);
                if (intercept_x != 9999)
                {
                    // calculate the distance from the origin
                    float dx = intercept_x - x_axis_origin;
                    float dy = intercept_y - y_axis_origin;
                    float dist_from_origin = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (dy < 0) dist_from_origin = -dist_from_origin;

                    // update the list of vertical intercepts
                    vertical_axis_intercepts.Add(dist_from_origin);
                }

                // find the interception of this point with the horizontal axis
                sluggish.utilities.geometry.intersection(x_axis_origin, y_axis_origin,
                                                         x_axis_origin_horizontal, y_axis_origin_horizontal,
                                                         x, y, x_vertical, y_vertical,
                                                         ref intercept_x, ref intercept_y);
                if (intercept_x != 9999)
                {
                    // calculate the distance from the origin
                    float dx = intercept_x - x_axis_origin;
                    float dy = intercept_y - y_axis_origin;
                    float dist_from_origin = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (dx < 0) dist_from_origin = -dist_from_origin;

                    // update the list of vertical intercepts
                    horizontal_axis_intercepts.Add(dist_from_origin);
                }
            }

            results.Add(horizontal_axis_intercepts);
            results.Add(vertical_axis_intercepts);

            return (results);
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show point features deteceted
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="bytes_per_pixel"></param>
        /// <param name="points"></param>
        /// <param name="output_img"></param>
        /// <param name="output_img_width"></param>
        /// <param name="output_img_height"></param>
        private static void ShowGridPoints(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                           ArrayList points,
                                           ref byte[] output_img, int output_img_width, int output_img_height)
        {
            output_img = new byte[output_img_width * output_img_height * 3];

            // copy the original image
            for (int y = 0; y < output_img_height; y++)
            {
                int yy = y * img_height / output_img_height;
                for (int x = 0; x < output_img_width; x++)
                {
                    int xx = x * img_width / output_img_width;
                    int n1 = ((y * output_img_width) + x) * 3;
                    int n2 = ((yy * img_width) + xx) * bytes_per_pixel;
                    int intensity = 0;
                    for (int col = 0; col < bytes_per_pixel; col++)
                    {
                        intensity += img[n2 + col];
                    }
                    intensity /= bytes_per_pixel;

                    for (int col = 0; col < 3; col++)
                        output_img[n1 + col] = (byte)intensity;
                }
            }

            // show feature points
            for (int i = 0; i < points.Count; i += 2)
            {
                float x = (float)points[i] * output_img_width / img_width;
                float y = (float)points[i + 1] * output_img_height / img_height;

                drawing.drawCircle(output_img, output_img_width, output_img_height, (int)x, (int)y, 1, 0, 255, 0, 1);
            }
        }

        /// <summary>
        /// show grid spacing responses
        /// </summary>
        /// <param name="horizontal_response"></param>
        /// <param name="vertical_response"></param>
        /// <param name="output_img"></param>
        /// <param name="output_img_width"></param>
        /// <param name="output_img_height"></param>
        private static void ShowGridAxisResponsesPoints(int[] horizontal_response, int[] vertical_response,
                                                        float horizontal_grid_spacing, float horizontal_phase_offset,
                                                        float vertical_grid_spacing, float vertical_phase_offset,
                                                        ref byte[] output_img, int output_img_width, int output_img_height)
        {
            output_img = new byte[output_img_width * output_img_height * 3];

            for (int i = 0; i < 2; i++)
            {
                int[] response = horizontal_response;
                if (i > 0) response = vertical_response;

                // find the maximum value
                int max_response = 0;
                int start_index = -1;
                int end_index = -1;
                float average_response = 0;
                int average_response_hits = 0;
                for (int j = 0; j < response.Length; j++)
                {
                    if (response[j] > max_response) max_response = response[j];
                    if (response[j] > 0)
                    {
                        end_index = j;
                        if (start_index == -1) start_index = j;
                        average_response += response[j];
                        average_response_hits++;
                    }
                }
                if (average_response_hits > 0) average_response /= average_response_hits;

                float average_variance = 0;
                for (int j = 0; j < response.Length; j++)
                {
                    if (response[j] > 0)
                        average_variance += Math.Abs(response[j] - average_response);
                }
                if (average_response_hits > 0) average_variance /= average_response_hits;

                float amplitude = average_variance;                // show the responses
                int max_response_height = (output_img_height / 3);
                int prev_x = 0, prev_y = 0;
                for (int j = 0; j < response.Length; j++)
                {
                    int x = (j - start_index) * output_img_width / (end_index - start_index);
                    int y = max_response_height - 1 - (response[j] * max_response_height / max_response);
                    if (i > 0) y += (output_img_height / 2);
                    if (j > 0)
                    {
                        drawing.drawLine(output_img, output_img_width, output_img_height,
                                         prev_x, prev_y, x, y, 0, 255, 0, 1, false);
                    }
                    prev_x = x;
                    prev_y = y;
                }

                // show the frequency match
                prev_x = 0;
                prev_y = 0;
                float v = 0;
                for (int j = start_index; j < end_index; j++)
                {
                    if (i == 0)
                        v = horizontal_phase_offset + ((j / (horizontal_grid_spacing / quantisation)) * (float)Math.PI * 2);
                    else
                        v = vertical_phase_offset + ((j / (vertical_grid_spacing / quantisation)) * (float)Math.PI * 2);

                    v = (float)Math.Sin(v) * amplitude;
                    v += average_response;

                    int x = (j - start_index) * output_img_width / (end_index - start_index);
                    int y = max_response_height - 1 - (int)(v * max_response_height / max_response);
                    if (i > 0) y += (output_img_height / 2);
                    if (j > 0)
                    {
                        drawing.drawLine(output_img, output_img_width, output_img_height,
                                         prev_x, prev_y, x, y, 255, 255, 0, 1, false);
                    }
                    prev_x = x;
                    prev_y = y;
                }
            }
        }

        /// <summary>
        /// show grid lines within the given image
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="lineWidth">line width in pixels</param>
        public void ShowLines(byte[] img, int img_width, int img_height,
                              int r, int g, int b, int lineWidth)
        {
            if (line != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    ArrayList lines = line[i];
                    for (int j = 0; j < lines.Count; j += 4)
                    {
                        float x0 = (float)lines[j];
                        float y0 = (float)lines[j + 1];
                        float x1 = (float)lines[j + 2];
                        float y1 = (float)lines[j + 3];

                        drawing.drawLine(img, img_width, img_height,
                                         (int)x0, (int)y0, (int)x1, (int)y1,
                                         r, g, b, lineWidth, false);
                    }
                }
            }
        }

        /// <summary>
        /// show interception points between lines
        /// </summary>
        /// <param name="img">image data</param>
        /// <param name="img_width">width of the image</param>
        /// <param name="img_height">height of the image</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="radius">radius of the cross marks</param>
        /// <param name="lineWidth">line width used to draw crosses</param>
        public void ShowIntercepts(byte[] img, int img_width, int img_height,
                                   int r, int g, int b, int radius, int lineWidth)
        {
            if (line_intercepts != null)
            {
                for (int i = 0; i < line_intercepts.GetLength(0); i++)
                {
                    for (int j = 0; j < line_intercepts.GetLength(1); j++)
                    {
                        float x = line_intercepts[i, j, 0];
                        float y = line_intercepts[i, j, 1];
                        drawing.drawCross(img, img_width, img_height, (int)x, (int)y,
                                          radius, r, g, b, lineWidth);
                    }
                }
            }
        }

        #endregion
    }
}
