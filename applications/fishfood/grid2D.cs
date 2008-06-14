using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace sentience.calibration
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

    public class grid2D
    {
        // perimeter of the grid
        public polygon2D perimeter;

        // number of border cells
        public int border_cells;

        // if a border has been added this is the border perimeter
        public polygon2D border_perimeter;

        // cells within the grid
        public grid2Dcell[][] cell;

        // interception points between lines
        public float[, ,] line_intercepts;

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

            cell = new grid2Dcell[cells_across-1][];
			for (int x = 0; x < cells_across-1; x++)
			    cell[x] = new grid2Dcell[cells_down-1];

            for (int x = 0; x < cells_across - 1; x++)
            {
                for (int y = 0; y < cells_down - 1; y++)
                {
                    cell[x][y] = new grid2Dcell();
                    cell[x][y].perimeter = new polygon2D();
                    cell[x][y].perimeter.Add(line_intercepts[x, y, 0], line_intercepts[x, y, 1]);
                    cell[x][y].perimeter.Add(line_intercepts[x+1, y, 0], line_intercepts[x+1, y, 1]);
                    cell[x][y].perimeter.Add(line_intercepts[x+1, y+1, 0], line_intercepts[x+1, y+1, 1]);
                    cell[x][y].perimeter.Add(line_intercepts[x, y + 1, 0], line_intercepts[x, y + 1, 1]);
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
                int intercepts_x = line[0].Count/4;
                int intercepts_y = line[1].Count/4;
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
                        line_intercepts[i/4, j/4, 0] = ix;
                        line_intercepts[i/4, j/4, 1] = iy;
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

        /// <summary>
        /// initialise the grid
        /// </summary>
        /// <param name="dimension_x"></param>
        /// <param name="dimension_y"></param>
        /// <param name="perimeter"></param>
        /// <param name="border_cells"></param>
        /// <param name="simple_orientation"></param>
        public void init(int dimension_x, int dimension_y,
                         polygon2D perimeter, int border_cells, 
                         bool simple_orientation)
        {
            this.border_cells = border_cells;
            this.perimeter = perimeter;

            cell = new grid2Dcell[dimension_x][];
            for (int x = 0; x < dimension_x; x++)
                cell[x] = new grid2Dcell[dimension_y];

            line = new ArrayList[2];

            float length1, length2, length3, length4;
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
                length1 = perimeter.getSideLength(idx1);

                int next_idx1 = idx1 + 1;
                if (next_idx1 >= 4) next_idx1 -= 4;
                length3 = perimeter.getSideLength(next_idx1);

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
                length2 = perimeter.getSideLength(idx3);

                int next_idx3 = next_idx1 + 2;
                if (next_idx3 >= 4) next_idx3 -= 4;
                length4 = perimeter.getSideLength(next_idx3);

                float w1 = Math.Abs(x3 - x2);
                float h1 = Math.Abs(y3 - y2);

                int dimension = dimension_x;
                if (!simple_orientation)
                {
                    if (i > 0)
                        dimension = dimension_y;
                }
                else
                {
                    if (h0 > w0) dimension = dimension_y;
                }

                // how much shorter is one line than the other on the opposite axis?
                float shortening = 0;
                //if (h0 > w0)
                {
                    if (length3 > length4)
                        shortening = (length3 - length4) / length3;
                    else
                        shortening = (length4 - length3) / length4;
                }


                for (int j = -border_cells; j <= dimension + border_cells; j++)
                {
                    // locate the position along the first line
                    float xx0, yy0;  // position along the first line
                    
                    float fraction = j / (float)dimension;

                    // modify for foreshortening
                    //if ((h0 > w0) && (shortening > 0))
                    if (shortening > 0)
                    {
                        fraction = (fraction * (1.0f - shortening)) + ((float)Math.Sin(fraction * (float)Math.PI / 2) * shortening);
                        if (length3 > length4) fraction = 1.0f - fraction;
                    }

                    if (w0 > h0)
                    {
                        float grad = (y1 - y0) / (x1 - x0);
                        if (x1 > x0)
                            xx0 = x0 + (w0 * fraction);
                        else
                            xx0 = x0 - (w0 * fraction);
                        yy0 = y0 + ((xx0 - x0) * grad);
                    }
                    else
                    {
                        float grad = (x1 - x0) / (y1 - y0);
                        if (y1 > y0)
                            yy0 = y0 + (h0 * fraction);
                        else
                            yy0 = y0 - (h0 * fraction);
                        xx0 = x0 + ((yy0 - y0) * grad);
                    }

                    // locate the position along the second line
                    float xx1, yy1;  // position along the second line

                    if (w1 > h1)
                    {
                        float grad = (y2 - y3) / (x2 - x3);
                        if (x2 > x3)
                            xx1 = x3 + (w1 * fraction);
                        else
                            xx1 = x3 - (w1 * fraction);
                        yy1 = y3 + ((xx1 - x3) * grad);
                    }
                    else
                    {
                        float grad = (x2 - x3) / (y2 - y3);
                        if (y2 > y3)
                            yy1 = y3 + (h1 * fraction);
                        else
                            yy1 = y3 - (h1 * fraction);
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
            cell = new grid2Dcell[dimension_x][];
			for (int x = 0; x < dimension_x; x++)
			    cell[x] = new grid2Dcell[dimension_y];
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension_x">number of cells across the grid</param>
        /// <param name="dimension_y">number of cells down the grid</param>
        /// <param name="perimeter">perimeter of the grid</param>
        /// <param name="border_cells">number of cells to use as a border around the grid</param>
        /// <param name="orientation_simple"></param>
        public grid2D(int dimension_x, int dimension_y,
                      polygon2D perimeter, int border_cells,
                      bool orientation_simple)
        {
            init(dimension_x, dimension_y, perimeter, border_cells, orientation_simple);
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
                      int border_cells,
                      bool orientation_simple)
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
            init(dim_x, dim_y, perimeter, border_cells, orientation_simple);
        }

        #endregion

        #region "grid spacing detection from features"

        // quantisation used when detecting grid intersections with horizontal and vertical axes
        private const float quantisation = 2.0f;

        /// <summary>
        /// defines thew function which is matched against the grid spacing data
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="x_radians"></param>
        /// <param name="spacing"></param>
        /// <returns></returns>
        private static float GridFittingFunction(float offset,
                                                 float x_radians, 
                                                 float spacing)
        {
            float y = (float)Math.Sin(offset + x_radians);

            return (y);
        }

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

            // find the maximum response value
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

            // find the variance from the average response
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
                phase_steps = 40;
                frequency_steps = 40;
            }

            // seeded random number generator
            Random rnd = new Random(5826);

            // number of histogram samples to examine            
            int waveform_samples = 100;

            // create a graph to show the results
            //graph_points_weighted samples_graph = new graph_points_weighted(min_frequency, max_frequency, min_phase_offset, max_phase_offset, 0, 5000);
            //samples_graph.invert_weights = true;

            int no_of_passes = 2;
            for (int pass = 1; pass <= no_of_passes; pass++)
            {
                float phase_range = max_phase_offset - min_phase_offset;
                float frequency_range = max_frequency - min_frequency;

                // for each phase offset value
                for (int ph = 0; ph < phase_steps; ph++)
                {
                    //float offset = min_phase_offset + (phase_range * (float)rnd.NextDouble());
                    float offset = min_phase_offset + (ph * phase_range / phase_steps);

                    // for each possible frequency
                    for (int freq = 0; freq < frequency_steps; freq++)
                    {
                        //float f = min_frequency + (frequency_range * (float)rnd.NextDouble());
                        float f = min_frequency + (freq * frequency_range / frequency_steps);

                        // calculate the total sum of squared differences between
                        // this phase/frequency combination and the actual data
                        float current_value = 0;
                        for (int w = 0; w <= waveform_samples; w++)
                        {
                            float idx = start_index + ((end_index - start_index) * (float)rnd.NextDouble());
                            float x = idx;
                            //float y = (float)Math.Sin(offset + ((x / f) * Math.PI * 2));
                            float y = GridFittingFunction(offset, (x / f) * (float)Math.PI * 2, 0.1f);
                            y = (y * amplitude) + average_response;

                            int response_index = (int)Math.Round(idx);
                            if ((response_index > -1) && (response_index < response.Length))
                            {
                                float diff = y - response[response_index];
                                current_value += (diff * diff * y);
                            }
                        }

                        // update the graph
                        //samples_graph.Update(f, offset, current_value);

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

                if (pass < no_of_passes)
                {
                    const float scale_down_factor = 0.1f;

                    // first pass - refine the search range
                    float new_freq_range = frequency_range * scale_down_factor;
                    min_frequency = frequency - new_freq_range;
                    max_frequency = frequency + new_freq_range;

                    float new_phase_range = phase_range * scale_down_factor;
                    min_phase_offset = phase_offset - new_phase_range;
                    max_phase_offset = phase_offset + new_phase_range;
                }
            }

            //samples_graph.SaveAsBitmap("c:\\temp\\frequency_detection.bmp");

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
        /// <param name="ideal_grid_colour">colour used to draw the ideal grid</param>
        /// <param name="detected_grid_colour">colour used to draw the detected grid</param>
        private static void DetectGrid(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                       float dominant_orientation, float known_grid_spacing,
                                       float grid_horizontal, float grid_vertical,
                                       int minimum_dimension_horizontal, int maximum_dimension_horizontal,
                                       int minimum_dimension_vertical, int maximum_dimension_vertical,
                                       String horizontal_scale_spacings, String vertical_scale_spacings,
                                       ref float average_grid_spacing_horizontal, ref float average_grid_spacing_vertical,
                                       ref float horizontal_phase_offset, ref float vertical_phase_offset,
                                       ref int cells_horizontal, ref int cells_vertical,
                                       ref byte[] output_img, int output_img_width, int output_img_height, int output_img_type,
                                       byte[] ideal_grid_colour, byte[] detected_grid_colour)
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
                            true,
                            horizontal_buckets, vertical_buckets,
                            average_grid_spacing_horizontal, horizontal_phase_offset,
                            average_grid_spacing_vertical, vertical_phase_offset,
                            ideal_grid_colour[0], ideal_grid_colour[1], ideal_grid_colour[2], 
                            detected_grid_colour[0], detected_grid_colour[1], detected_grid_colour[2],
                            horizontal_scale_spacings, vertical_scale_spacings,
                            ref output_img, output_img_width, output_img_height);
                        break;
                    }
            }
            
        }

        /// <summary>
        /// detect a grid within the given image using the given orientation
        /// based upon previously detected spot centre points
        /// </summary>
        /// <param name="spot_centres">list containing spot centre positions</param>
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
        /// <param name="ideal_grid_colour">colour used to draw the ideal grid</param>
        /// <param name="detected_grid_colour">colour used to draw the detected grid</param>
        private static void DetectGrid(ArrayList spot_centres,
                                       byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                       float dominant_orientation, float known_grid_spacing,
                                       float grid_horizontal, float grid_vertical,
                                       int minimum_dimension_horizontal, int maximum_dimension_horizontal,
                                       int minimum_dimension_vertical, int maximum_dimension_vertical,
                                       String horizontal_scale_spacings, String vertical_scale_spacings,
                                       ref float average_grid_spacing_horizontal, 
                                       ref float average_grid_spacing_vertical,
                                       ref float horizontal_phase_offset, 
                                       ref float vertical_phase_offset,
                                       ref int cells_horizontal, ref int cells_vertical,
                                       ref byte[] output_img, int output_img_width, int output_img_height, 
                                       int output_img_type,
                                       byte[] ideal_grid_colour, byte[] detected_grid_colour)
        {
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
            DetectGrid(spot_centres, dominant_orientation, centre_x, centre_y, img_width / 2,
                       distortion_angle, known_grid_spacing,
                       min_grid_spacing_horizontal, max_grid_spacing_horizontal,
                       min_grid_spacing_vertical, max_grid_spacing_vertical,
                       ref horizontal_buckets, ref vertical_buckets,
                       ref average_grid_spacing_horizontal,
                       ref average_grid_spacing_vertical,
                       ref horizontal_phase_offset,
                       ref vertical_phase_offset);

            // because this is based upon spot centres
            // rather than square edges we need to offset the
            // phase by 180 degrees
            horizontal_phase_offset -= (float)Math.PI;
            vertical_phase_offset -= (float)Math.PI;

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
                            spot_centres,
                            ref output_img, output_img_width, output_img_height);
                        break;
                    }
                case 2: // grid spacing responses
                    {
                        ShowGridAxisResponsesPoints(
                            false,
                            horizontal_buckets, vertical_buckets,
                            average_grid_spacing_horizontal, horizontal_phase_offset,
                            average_grid_spacing_vertical, vertical_phase_offset,
                            ideal_grid_colour[0], ideal_grid_colour[1], ideal_grid_colour[2], 
                            detected_grid_colour[0], detected_grid_colour[1], detected_grid_colour[2],
                            horizontal_scale_spacings, vertical_scale_spacings,
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
        /// <param name="spot_centres">previously detected spot centres</param>
        /// <param name="minimum_dimension_horizontal">minimum number of cells across</param>
        /// <param name="maximum_dimension_horizontal">maximum number of cells across</param>
        /// <param name="minimum_dimension_vertical">minimum number of cells down</param>
        /// <param name="maximum_dimension_vertical">maximum number of cells down</param>
        /// <param name="known_grid_spacing">known grid spacing (cell diameter) value in pixels</param>
        /// <param name="known_even_dimension">set to true if it is known that the number of cells in horizontal and vertical axes is even</param>
        /// <param name="border_cells">extra cells to add as a buffer zone around the grid</param>
        /// <param name="horizontal_scale_spacings">description used on the spacings diagram</param>
        /// <param name="vertical_scale_spacings">description used on the spacings diagram</param>
        /// <param name="average_spacing_horizontal"></param>
        /// <param name="average_spacing_vertical"></param>
        /// <param name="output_img"></param>
        /// <param name="output_img_width"></param>
        /// <param name="output_img_height"></param>
        /// <param name="output_img_type"></param>
        /// <param name="ideal_grid_colour">colour used to draw the ideal grid</param>
        /// <param name="detected_grid_colour">colour used to draw the detected grid</param>
        /// <param name="simple_orientation"></param>
        /// <returns>2D grid</returns>
        public static grid2D DetectGrid(byte[] img, int img_width, int img_height, int bytes_per_pixel,
                                        polygon2D perimeter, ArrayList spot_centres,
                                        int minimum_dimension_horizontal, int maximum_dimension_horizontal,
                                        int minimum_dimension_vertical, int maximum_dimension_vertical,
                                        float known_grid_spacing, bool known_even_dimension,
                                        int border_cells,
                                        String horizontal_scale_spacings, String vertical_scale_spacings,
                                        ref float average_spacing_horizontal, 
                                        ref float average_spacing_vertical,
                                        ref byte[] output_img, int output_img_width, int output_img_height,
                                        int output_img_type,
                                        byte[] ideal_grid_colour, byte[] detected_grid_colour,
                                        bool simple_orientation)
        {
            int tx = (int)perimeter.left();
            int ty = (int)perimeter.top();
            int bx = (int)perimeter.right();
            int by = (int)perimeter.bottom();

            // adjust spot centre positions so that they're relative to the perimeter
            // top left position
            if (spot_centres != null)
            {
                for (int i = 0; i < spot_centres.Count; i += 2)
                {
                    spot_centres[i] = (float)spot_centres[i] - tx;
                    spot_centres[i + 1] = (float)spot_centres[i + 1] - ty;
                }
            }

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

            if (spot_centres == null)
                DetectGrid(grid_img, wdth, hght, bytes_per_pixel, 
                           dominant_orientation, known_grid_spacing,
                           grid_horizontal, grid_vertical,
                           minimum_dimension_horizontal, maximum_dimension_horizontal,
                           minimum_dimension_vertical, maximum_dimension_vertical,
                           horizontal_scale_spacings, vertical_scale_spacings,
                           ref average_spacing_horizontal, ref average_spacing_vertical,
                           ref horizontal_phase_offset, ref vertical_phase_offset,
                           ref cells_horizontal, ref cells_vertical,
                           ref output_img, output_img_width, output_img_height, output_img_type,
                           ideal_grid_colour, detected_grid_colour);
            else
                DetectGrid(spot_centres, grid_img, wdth, hght, bytes_per_pixel, 
                           dominant_orientation, known_grid_spacing,
                           grid_horizontal, grid_vertical,
                           minimum_dimension_horizontal, maximum_dimension_horizontal,
                           minimum_dimension_vertical, maximum_dimension_vertical,
                           horizontal_scale_spacings, vertical_scale_spacings,
                           ref average_spacing_horizontal, ref average_spacing_vertical,
                           ref horizontal_phase_offset, ref vertical_phase_offset,
                           ref cells_horizontal, ref cells_vertical,
                           ref output_img, output_img_width, output_img_height, output_img_type,
                           ideal_grid_colour, detected_grid_colour);

            
            grid2D detectedGrid = null;

            // apply some range limits
            bool range_limited = false;
            if (cells_horizontal < 3)
            {
                cells_horizontal = 3;
                range_limited = true;
            }
            if (cells_vertical < 3)
            {
                cells_vertical = 3;
                range_limited = true;
            }
            if (cells_horizontal > maximum_dimension_horizontal)
            {
                cells_horizontal = maximum_dimension_horizontal;
                range_limited = true;
            }
            if (cells_vertical > maximum_dimension_vertical)
            {
                cells_vertical = maximum_dimension_vertical;
                range_limited = true;
            }
            if (range_limited)
            {
                Console.WriteLine("WARNING: When detecting the grid the matrix dimension had to be artificially restricted.");
                Console.WriteLine("         This probably means that there is a problem with the original image");
            }
            
            // if we know the number of cells should be even correct any inaccuracies
            if (known_even_dimension)
            {
                cells_horizontal = (int)(cells_horizontal / 2) * 2;
                cells_vertical = (int)(cells_vertical / 2) * 2;
            }

            // get the centre of the region
            float cx = tx + ((bx - tx) / 2);
            float cy = ty + ((by - ty) / 2);
            
            detectedGrid = new grid2D(cells_horizontal, cells_vertical,
                                      cx, cy, dominant_orientation,
                                      average_spacing_horizontal, average_spacing_vertical,
                                      horizontal_phase_offset, vertical_phase_offset,
                                      border_cells, simple_orientation);

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
        /// turn maxima into an ideal grid with perfectly regular spacing
        /// </summary>
        /// <param name="maxima">positions of maxima which correspond to grid lines</param>
        /// <returns>ideal maxima</returns>
        private static ArrayList idealGrid(ArrayList maxima)
        {
            ArrayList equalised = new ArrayList();

            if (maxima.Count > 1)
            {
                // get the average spacing
                float average_spacing = 0;
                for (int i = 1; i < maxima.Count; i++)
                {
                    average_spacing += (float)maxima[i] - (float)maxima[i - 1];
                }
                average_spacing /= (maxima.Count - 1);

                // get the average offset
                float average_offset = 0;
                float initial_position = (float)maxima[0];
                for (int i = 0; i < maxima.Count; i++)
                {
                    average_offset += (float)maxima[i] - (initial_position + (average_spacing * i));
                }
                average_offset /= maxima.Count;


                for (int i = 0; i < maxima.Count; i++)
                {
                    float equalised_position = initial_position + (average_spacing * i) - (average_offset / 2.0f);
                    equalised.Add(equalised_position);
                }
            }

            return (equalised);
        }


        /// <summary>
        /// fills in any missing data in a list which contains a series
        /// of positions representing the detected lines within a grid pattern
        /// </summary>
        /// <param name="maxima">positions of each line</param>
        /// <param name="grid_spacing">estimated grid spacing value</param>
        /// <param name="buffer_cells">add this number of buffer cells to the start and end of the data</param>
        /// <returns></returns>
        /*
        private static ArrayList FillGrid(ArrayList maxima,
                                          float grid_spacing,
                                          int buffer_cells)
        {
            ArrayList filled = new ArrayList();

            // add an initial buffer
            for (int i = 0; i < buffer_cells; i++)
            {
                float dist = (float)maxima[0] - ((buffer_cells - i) * grid_spacing);
                filled.Add(dist);
            }

            // compare each spacing to the average
            float prev_dist = 0;
            float max_width = grid_spacing * 1.3f;
            for (int i = 0; i < maxima.Count; i++)
            {
                float dist = (float)maxima[i];
                if (i > 0)
                {
                    float width = Math.Abs(dist - prev_dist);
                    if (width > max_width)
                    {
                        // the width is bigger than the usual range
                        // fill in the intermediate spacings
                        for (int j = 1; j <= (int)(width / grid_spacing); j++)
                        {
                            float intermediate_dist = prev_dist + (j * grid_spacing);
                            if (Math.Abs(dist - intermediate_dist) > grid_spacing * 0.5f)
                                filled.Add(intermediate_dist);
                        }
                    }

                    filled.Add(dist);
                }
                else
                {
                    filled.Add(dist);
                }
                prev_dist = dist;
            }

            // add a trailing buffer
            for (int i = 1; i <= buffer_cells; i++)
            {
                float dist = (float)maxima[maxima.Count - 1] + (i * grid_spacing);
                filled.Add(dist);
            }

            return (filled);
        }
         */

        /// <summary>
        /// equalise the spacing between grid lines
        /// </summary>
        /// <param name="maxima">positions of maxima which correspond to grid lines</param>
        /// <returns>equalised maxima</returns>
        /*
        private static ArrayList EqualiseGrid(ArrayList maxima)
        {
            ArrayList equalised = new ArrayList();

            for (int i = 0; i < maxima.Count; i++)
            {
                if ((i > 0) && (i < maxima.Count - 1))
                {
                    float prev_dist = (float)maxima[i - 1];
                    float equalised_dist = prev_dist +
                                           (((float)maxima[i + 1] - prev_dist) / 2.0f);
                    equalised.Add(equalised_dist);
                }
                else
                {
                    float dist = (float)maxima[i];
                    equalised.Add(dist);
                }
            }

            return (equalised);
        }
        */



        /// <summary>
        /// detects peak positions within an array of samples
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="quantisation"></param>
        /// <param name="minimum_grid_width"></param>
        /// <returns></returns>
        private static ArrayList DetectPeaks(int[] samples, float quantisation,
                                             float minimum_grid_width,
                                             ref float average_peak_separation,
                                             ref bool[] peak)
        {
            // this list will contain the positions of peaks along the axis
            ArrayList peaks = new ArrayList();

            // find the average value of the samples
            int no_of_samples = samples.Length;
            float average = 0;
            int hits = 0;
            for (int i = 0; i < no_of_samples; i++)
            {
                if (samples[i] > 0)
                {
                    average += samples[i];
                    hits++;
                }
            }
            if (hits > 0) average /= hits;

            // perform non-maximal supression
            peak = new bool[no_of_samples];
            for (int i = 0; i < no_of_samples; i++)
                if (samples[i] > 0) peak[i] = true;

            int r = (int)(minimum_grid_width / quantisation);
            if (r < 1) r = 1;
            for (int i = 0; i < no_of_samples; i++)
            {
                float value = samples[i];
                if (peak[i])
                {
                    int j = i + 1;
                    while ((j <= i + r) &&
                           (j < no_of_samples) &&
                           (peak[i]))
                    {
                        if (peak[j])
                        {
                            if (samples[j] <= value)
                            {
                                peak[j] = false;
                            }
                            else
                            {
                                peak[i] = false;
                            }
                        }
                        j++;
                    }
                }
            }

            // extract peak values and put them into a list
            float prev_peak_position = 9999;
            average_peak_separation = 0;
            int average_peak_separation_hits = 0;
            for (int i = 0; i < no_of_samples; i++)
            {
                if (peak[i])
                {
                    // try to make the peak position more accurate
                    int local_radius = 2;
                    float tot = 0;
                    float peak_position = 0;
                    for (int j = i - local_radius; j <= i + local_radius; j++)
                    {
                        if ((j > -1) && (j < no_of_samples))
                        {
                            tot += samples[j];
                            peak_position += (samples[j] * j);
                        }
                        if (tot > 0) peak_position /= tot;
                    }
                    peak_position *= quantisation;

                    if (prev_peak_position != 9999)
                    {
                        average_peak_separation += Math.Abs(peak_position - prev_peak_position);
                        average_peak_separation_hits++;
                    }

                    peaks.Add(peak_position);
                    prev_peak_position = peak_position;
                }
            }

            // calculate average peak separation
            if (average_peak_separation_hits > 0)
                average_peak_separation /= average_peak_separation_hits;

            return (peaks);
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
                geometry.intersection(x_axis_origin, y_axis_origin,
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
                geometry.intersection(x_axis_origin, y_axis_origin,
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

        #region "rescaling the grid"

        /// <summary>
        /// rescales the grid to a larger or smaller image
        /// </summary>
        /// <param name="original_image_width"></param>
        /// <param name="original_image_height"></param>
        /// <param name="new_image_width"></param>
        /// <param name="new_image_height"></param>
        /// <returns></returns>
        public grid2D Scale(int original_image_width, int original_image_height,
                            int new_image_width, int new_image_height)
        {
            polygon2D new_perimeter = perimeter.Copy();
            for (int i = 0; i < new_perimeter.x_points.Count; i++)
            {
                float x = (float)new_perimeter.x_points[i] * new_image_width / original_image_width;
                float y = (float)new_perimeter.y_points[i] * new_image_height / original_image_height;
                new_perimeter.x_points[i] = x;
                new_perimeter.y_points[i] = y;
            }

            grid2D new_grid = new grid2D(cell.Length, cell[0].Length);
            new_grid.init(cell.Length, cell[0].Length,
                          new_perimeter, 0, false);
            return (new_grid);
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
        /// <param name="has_square_cells">whether the pattern has square cells</param>
        /// <param name="horizontal_response"></param>
        /// <param name="vertical_response"></param>
        /// <param name="horizontal_grid_spacing"></param>
        /// <param name="horizontal_phase_offset"></param>
        /// <param name="vertical_grid_spacing"></param>
        /// <param name="vertical_phase_offset"></param>
        /// <param name="ideal_r">ideal spacing colour</param>
        /// <param name="ideal_g">ideal spacing colour</param>
        /// <param name="ideal_b">ideal spacing colour</param>
        /// <param name="detected_r">detected spacing colour</param>
        /// <param name="detected_g">detected spacing colour</param>
        /// <param name="detected_b">detected spacing colour</param>
        /// <param name="horizontal_scale_spacings">description for the horizontal spacings</param>
        /// <param name="vertical_scale_spacings">description for the vertical spacings</param>
        /// <param name="output_img">image to save the result to</param>
        /// <param name="output_img_width">width of the image</param>
        /// <param name="output_img_height">height of the image</param>
        private static void ShowGridAxisResponsesPoints(bool has_square_cells, int[] horizontal_response, int[] vertical_response,
                                                        float horizontal_grid_spacing, float horizontal_phase_offset,
                                                        float vertical_grid_spacing, float vertical_phase_offset,
                                                        int ideal_r, int ideal_g, int ideal_b,
                                                        int detected_r, int detected_g, int detected_b,
                                                        String horizontal_scale_spacings, String vertical_scale_spacings,
                                                        ref byte[] output_img, int output_img_width, int output_img_height)
        {
            const String font = "Arial";
            const int font_size = 10;
            int line_width = 0;

            // if the detection was based upon a spot pattern
            // we need to offset by 180 degrees to get the correct grid
            if (!has_square_cells)
            {
                horizontal_phase_offset += (float)Math.PI;
                vertical_phase_offset += (float)Math.PI;
            }

            output_img = new byte[output_img_width * output_img_height * 3];
            for (int i = 0; i < output_img.Length; i++) output_img[i] = 255;

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

                // show the response data
                int prev_x = 0, prev_y = 0;
                float amplitude = average_variance;                // show the responses
                int max_response_height = (output_img_height / 3);
                if ((start_index > -1) && (end_index > -1) && (start_index != end_index))
                {
                    for (int j = 0; j < response.Length; j++)
                    {
                        int x = (j - start_index) * output_img_width / (end_index - start_index);
                        int y = max_response_height - 1 - (response[j] * max_response_height / max_response) + (font_size * 3);
                        if (i > 0) y += (output_img_height / 2);
                        if (j > 0)
                        {
                            drawing.drawLine(output_img, output_img_width, output_img_height,
                                             prev_x, prev_y, x, y, detected_r, detected_g, detected_b, line_width, false);
                        }
                        prev_x = x;
                        prev_y = y;
                    }
                }

                // show the best frequency match
                prev_x = 0;
                prev_y = 0;
                float v = 0;
                for (float j = start_index; j < end_index; j += 0.1f)
                {
                    if (i == 0)
                        v = GridFittingFunction(horizontal_phase_offset, (j / (horizontal_grid_spacing / quantisation)) * (float)Math.PI * 2, 0.1f);
                    else
                        v = GridFittingFunction(vertical_phase_offset, (j / (vertical_grid_spacing / quantisation)) * (float)Math.PI * 2, 0.1f);

                    v *= amplitude;

                    v += average_response;

                    int x = (int)((j - start_index) * output_img_width / (end_index - start_index));
                    int y = max_response_height - 1 - (int)(v * max_response_height / max_response) + (font_size * 3);
                    if (i > 0) y += (output_img_height / 2);
                    if (j > start_index)
                    {
                        drawing.drawLine(output_img, output_img_width, output_img_height,
                                         prev_x, prev_y, x, y, ideal_r, ideal_g, ideal_b, line_width, false);
                    }
                    prev_x = x;
                    prev_y = y;
                }
            }

            // show some text: "horizontal spacings" and "vertical spacings"
            if (horizontal_scale_spacings != "")
            {
                drawing.AddText(output_img, output_img_width, output_img_height,
                                horizontal_scale_spacings,
                                font, font_size,
                                0, 0, 0,
                                output_img_width / 50, (font_size / 2));
            }
            if (vertical_scale_spacings != "")
            {
                drawing.AddText(output_img, output_img_width, output_img_height,
                                vertical_scale_spacings,
                                font, font_size,
                                0, 0, 0,
                                output_img_width / 50, (output_img_height/2) + (font_size / 2));
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

                        if ((j < border_cells * 4) ||
                            (j >= lines.Count - 1 - (border_cells * 4)))
                        {
                            float cx = x0 + (x1 - x0) / 2;
                            float cy = y0 + (y1 - y0) / 2;

                            float scale;

                            if (j < border_cells * 4) 
                               scale = 1.0f + (((border_cells - (j / 4)) * 2) / (float)((lines.Count / 4) - border_cells));
                            else
                               scale = 1.0f + (((border_cells - ((lines.Count-1-j)/4)) * 2) / (float)((lines.Count / 4) - border_cells));
                            
                            float dx = x0 - cx;
                            float dy = y0 - cy;
                            dx *= scale;
                            dy *= scale;
                            x0 = cx + dx;
                            y0 = cy + dy;
                            
                            dx = x1 - cx;
                            dy = y1 - cy;
                            dx *= scale;
                            dy *= scale;
                            x1 = cx + dx;
                            y1 = cy + dy;
                        }

                        drawing.drawLine(img, img_width, img_height,
                                         (int)x0, (int)y0, (int)x1, (int)y1,
                                         r, g, b, lineWidth, false);
                    }
                }
            }
        }

        public void ShowLines(byte[] img, int img_width, int img_height,
                              int original_img_width, int original_img_height,
                              int r, int g, int b, int lineWidth)
        {
            if (line != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    ArrayList lines = line[i];
                    for (int j = 0; j < lines.Count; j += 4)
                    {
                        float x0 = (float)lines[j] * img_width / original_img_width;
                        float y0 = (float)lines[j + 1] * img_height / original_img_height;
                        float x1 = (float)lines[j + 2] * img_width / original_img_width;
                        float y1 = (float)lines[j + 3] * img_height / original_img_height;

                        if ((j < border_cells * 4) ||
                            (j >= lines.Count - 1 - (border_cells * 4)))
                        {
                            float cx = x0 + (x1 - x0) / 2;
                            float cy = y0 + (y1 - y0) / 2;

                            float scale;

                            if (j < border_cells * 4) 
                               scale = 1.0f + (((border_cells - (j / 4)) * 2) / (float)((lines.Count / 4) - border_cells));
                            else
                               scale = 1.0f + (((border_cells - ((lines.Count-1-j)/4)) * 2) / (float)((lines.Count / 4) - border_cells));
                            
                            float dx = x0 - cx;
                            float dy = y0 - cy;
                            dx *= scale;
                            dy *= scale;
                            x0 = cx + dx;
                            y0 = cy + dy;
                            
                            dx = x1 - cx;
                            dy = y1 - cy;
                            dx *= scale;
                            dy *= scale;
                            x1 = cx + dx;
                            y1 = cy + dy;
                        }

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

        #region "detecting spots within a grid"

        public const int SPOT_PARAMETERS = 9;

        /// <summary>
        /// returns the probability that a given grid cell is occupied
        /// </summary>
        /// <param name="raw_image_mono">mono image data</param>
        /// <param name="raw_image_width">image width</param>
        /// <param name="black_on_white">whether the spots are dark on a lighter background</param>
        /// <param name="tx">top left of area to be sampled</param>
        /// <param name="ty">top left of area to be sampled</param>
        /// <param name="bx">bottom right of area to be sampled</param>
        /// <param name="by">bottom right of area to be sampled</param>
        /// <param name="threshold">intensity threshold</param>
        /// <param name="tollerance"></param>
        /// <returns>probability in the range 0.0 - 1.0</returns>
        private unsafe static float GridCellProbability(byte* unsafe_raw_image_mono, int raw_image_width,
                                                        bool black_on_white,
                                                        int tx, int ty, int bx, int by,
                                                        float threshold, int tollerance)
        {
            float prob = 0;
            int occupied_votes = 0;
            int vacant_votes = 0;
            float occupied_prob = 0;
            float vacant_prob = 0;

            bool searching = true;
            int max_offset = 1;

            int offset_x = -max_offset;
            while ((offset_x <= max_offset) && (searching))
            {
                int offset_y = -max_offset;
                while ((offset_y <= max_offset) && (searching))
                {
                    // sample a few pixels around the centre
                    // and determine whether they are light or dark
                    // using the appropriate threshold value
                    int tx2 = tx + offset_x;
                    int bx2 = bx + offset_x;
                    int ty2 = ty + offset_x;
                    int by2 = by + offset_x;
                    int light_pixels = 0;
                    int dark_pixels = 0;
                    int n, n0 = (ty2 * raw_image_width) + tx2;
                    for (int y = ty; y <= by; y++)
                    {
                        n = n0;
                        for (int x = tx; x <= bx; x++)
                        {
                            byte v = unsafe_raw_image_mono[n++];

                            // is it light or dark ?
                            if (v > threshold)
                                light_pixels++;
                            else
                                dark_pixels++;
                        }
                        n0 += raw_image_width;
                    }

                    if (black_on_white)
                    {
                        // if there are more dark pixels than light
                        if (dark_pixels >= light_pixels + tollerance)
                        {
                            occupied_votes++;
                            occupied_prob += dark_pixels / (float)(dark_pixels + light_pixels);
                        }
                        else
                        {
                            vacant_votes++;
                            occupied_prob += light_pixels / (float)(dark_pixels + light_pixels);
                        }

                    }
                    else
                    {
                        // if there are more light pixels than dark
                        if (light_pixels >= dark_pixels + tollerance)
                        {
                            occupied_votes++;
                            occupied_prob += light_pixels / (float)(dark_pixels + light_pixels);
                        }
                        else
                        {
                            vacant_votes++;
                            occupied_prob += dark_pixels / (float)(dark_pixels + light_pixels);
                        }

                    }

                    // see if one side has won
                    if ((occupied_votes > 4) || (vacant_votes > 4))
                    {
                        searching = false;
                    }

                    offset_y++;
                }
                offset_x++;
            }

            if (occupied_votes > vacant_votes)
                prob = occupied_prob / occupied_votes;
            else
                prob = vacant_prob / occupied_votes;

            return (prob);
        }

        /// <summary>
        /// detects the radius of a spot
        /// </summary>
        /// <param name="raw_image_mono">mono image data</param>
        /// <param name="raw_image_width">image width</param>
        /// <param name="raw_image_height">image height</param>
        /// <param name="spot_centre_x">centre x position of the spot</param>
        /// <param name="spot_centre_y">centre y position of the spot</param>
        /// <param name="min_radius">minimum radius of the spot</param>
        /// <param name="max_radius">maximum radius of the spot</param>
        /// <param name="ovality">returns ovality of the spot</param>
        /// <returns>radius of the spot</returns>
        private static unsafe float DetectSpotRadius(byte* raw_image_mono,
                                                     int raw_image_width, int raw_image_height,
                                                     float spot_centre_x, float spot_centre_y,
                                                     float min_radius, float max_radius,
                                                     ref float ovality)
        {
            float radius = 0;
            int no_of_samples = 20;
            float angular_increment = ((float)Math.PI * 2) / no_of_samples;

            // sample pixels along a number of radii
            float av_radius_vertical = 0;
            int av_radius_vertical_hits = 0;
            float av_radius_horizontal = 0;
            int av_radius_horizontal_hits = 0;
            float angle = 0;
            float radius_increment = 0.5f;
            float radius_range = max_radius - min_radius;
            int no_of_radial_samples = (int)(radius_range / radius_increment);
            float[] radial_intensity = new float[no_of_radial_samples];
            int[] temp_radial_intensity = new int[no_of_radial_samples];
            int pixels = raw_image_width * raw_image_height;

            // for each radius
            for (int i = 0; i < no_of_samples; i++)
            {
                float r = min_radius;
                int prev_n = -1;
                for (int j = 0; j < no_of_radial_samples; j++)
                {
                    // sample a pixel at this radius
                    int x = (int)(spot_centre_x + (r * (float)Math.Sin(angle)));
                    int y = (int)(spot_centre_y + (r * (float)Math.Cos(angle)));
                    int n = (y * raw_image_width) + x;
                    if ((n > -1) && (n < pixels) && (n != prev_n))
                    {
                        // squared pixel value
                        int intensity_squared = raw_image_mono[n] * raw_image_mono[n];

                        // update radial intensity
                        radial_intensity[j] += intensity_squared;

                        // this temporary array is later used
                        // to estimate the local radius
                        temp_radial_intensity[j] = intensity_squared;
                    }
                    r += radius_increment;
                    prev_n = n;
                }

                // average edge position
                // which may correspond to the local radius
                // this is used to calculate ovality
                r = min_radius + (radius_increment*2);
                float local_r = 0;
                float tot_diff = 0;
                for (int j = 2; j < no_of_radial_samples - 2; j++)
                {
                    // edge response at this radius
                    float diff = (temp_radial_intensity[j - 2] +
                                temp_radial_intensity[j - 1]) -
                               (temp_radial_intensity[j] +
                                temp_radial_intensity[j + 1]);
                    
                    // squared response value
                    diff *= diff;
                    local_r += (r * diff);
                    tot_diff += diff;
                    r += radius_increment;
                }
                
                // find the centre of gravity position for edge responses along this radius
                if (tot_diff > 0) local_r /= tot_diff;

                if (local_r > 0)
                {
                    // update the average radius in the vertical axis
                    if ((i < (no_of_samples / 4) || (i > (no_of_samples * 3 / 4))))
                    {
                        av_radius_vertical += local_r;
                        av_radius_vertical_hits++;
                    }

                    // update the average radius in the horizontal axis
                    if (i < no_of_samples / 2)
                    {
                        av_radius_horizontal += local_r;
                        av_radius_horizontal_hits++;
                    }
                }

                // increment the angle at which the radius is sampled
                angle += angular_increment;
            }

            // calculate the ovality
            ovality = 0;
            if ((av_radius_vertical_hits > 0) && (av_radius_horizontal_hits > 0))
            {
                av_radius_vertical /= av_radius_vertical_hits;
                av_radius_horizontal /= av_radius_horizontal_hits;

                // ovality is the absolute percentage deviation from an even aspect ratio
                // between vertical and horizontal
                ovality = ((av_radius_vertical / av_radius_horizontal) - 1.0f) * 100;
                if (ovality < 0) ovality = -ovality;
            }

            // find the radius
            // for each possible radius compare the average pixel intensity
            // less than this radius to the average pixel intensity greater
            // than this radius.  The biggest difference wins
            float max_diff2 = 0;
            int buffer = (int)(2.0f / radius_increment);
            if (no_of_radial_samples - buffer <= buffer) buffer = (int)(1.0f / radius_increment);
            for (int j = buffer; j < no_of_radial_samples - buffer; j++)
            {
                float average_inner_intensity = 0;
                float average_outer_intensity = 0;

                for (int i = 0; i < j; i++)
                    average_inner_intensity += radial_intensity[i];
                average_inner_intensity /= j;
                for (int i = j; i < no_of_radial_samples; i++)
                    average_outer_intensity += radial_intensity[i];
                average_outer_intensity /= (no_of_radial_samples - j);

                float diff2 = average_inner_intensity - average_outer_intensity;
                if (diff2 < 0) diff2 = -diff2;
                if (diff2 > max_diff2)
                {
                    max_diff2 = diff2;
                    radius = min_radius + (j * radius_range / no_of_radial_samples);
                }
            }

            return (radius);
        }

        /// <summary>
        /// locates the centre position of a spot
        /// </summary>
        /// <param name="raw_image_mono">mono image data</param>
        /// <param name="raw_image_width">image width</param>
        /// <param name="raw_image_height">image height</param>
        /// <param name="search_region">region within which to search</param>
        /// <param name="black_on_white">whether this image contains dark markings on a lighter background</param>
        /// <param name="centre_x">returned x centre position</param>
        /// <param name="centre_y">returned y centre position</param>
        private static unsafe void LocateSpotCentre(byte* raw_image_mono,
                                                    int raw_image_width, int raw_image_height,
                                                    polygon2D search_region,
                                                    bool black_on_white,
                                                    ref float centre_x, ref float centre_y)
        {
            centre_x = 0;
            centre_y = 0;

            // get dimensions of the region to be searched
            int tx = (int)search_region.left();
            int ty = (int)search_region.top();
            int bx = (int)search_region.right();
            int by = (int)search_region.bottom();

            int r = (bx - tx) / 3;
            if (r < 1) r = 1;
            int search_r = (bx - tx) / 4;
            if (search_r < 1) search_r = 1;

            // centre of the search region
            float search_centre_x = 0;
            float search_centre_y = 0;
            search_region.GetSquareCentre(ref search_centre_x, ref search_centre_y);
            //search_region.getCentreOfGravity(ref search_centre_x, ref search_centre_y);

            int image_pixels = raw_image_width * raw_image_height;
            float max = 0;
            float v;
            for (float offset_y = search_centre_y - search_r; offset_y <= search_centre_y + search_r; offset_y += 0.5f)
            {
                for (float offset_x = search_centre_x - search_r; offset_x <= search_centre_x + search_r; offset_x += 0.5f)
                {
                    float tot = 0;
                    for (int yy = (int)(offset_y - r); yy <= (int)(offset_y + r); yy += 2)
                    {
                        int n0 = yy * raw_image_width;
                        for (int xx = (int)(offset_x - r); xx <= (int)(offset_x + r); xx += 2)
                        {
                            int n1 = n0 + xx;
                            if ((n1 > -1) && (n1 < image_pixels))
                            {
                                if (black_on_white)
                                    v = 255 - raw_image_mono[n1];
                                else
                                    v = raw_image_mono[n1];
                                tot += v * v;
                            }
                        }
                    }
                    if (tot > max)
                    {
                        max = tot;
                        centre_x = offset_x;
                        centre_y = offset_y;
                    }
                }
            }
        }


        #endregion

        #region "testing functions"

        /// <summary>
        /// simple function used to test grid creation
        /// </summary>
        /// <param name="filename">filename of the test image to be created</param>
        public static void Test(string filename)
        {
            int image_width = 640;
            int image_height = 480;
            int dimension_x = 10;
            int dimension_y = 10;

            Bitmap bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte[] img = new byte[image_width * image_height * 3];

            polygon2D test_poly;
            grid2D grid;

            test_poly = new polygon2D();
            test_poly.Add(image_width * 15 / 100, image_height * 20 / 100);
            test_poly.Add(image_width * 40 / 100, image_height * 20 / 100);
            test_poly.Add(image_width * 40 / 100, image_height * 40 / 100);
            test_poly.Add(image_width * 15 / 100, image_height * 40 / 100);
            grid = new grid2D(dimension_x, dimension_y, test_poly, 0, false);
            grid.ShowLines(img, image_width, image_height, 0, 255, 0, 0);

            int half_width = image_width / 2;
            test_poly = new polygon2D();
            test_poly.Add(half_width + (image_width * 20 / 100), image_height * 20 / 100);
            test_poly.Add(half_width + (image_width * 35 / 100), image_height * 20 / 100);
            test_poly.Add(half_width + (image_width * 40 / 100), image_height * 40 / 100);
            test_poly.Add(half_width + (image_width * 15 / 100), image_height * 40 / 100);
            grid = new grid2D(dimension_x, dimension_y, test_poly, 0, false);
            grid.ShowLines(img, image_width, image_height, 0, 255, 0, 0);
            
            int half_height = image_height / 2;
            test_poly = new polygon2D();
            test_poly.Add(image_width * 15 / 100, half_height + (image_height * 24 / 100));
            test_poly.Add(image_width * 40 / 100, half_height + (image_height * 20 / 100));
            test_poly.Add(image_width * 40 / 100, half_height + (image_height * 40 / 100));
            test_poly.Add(image_width * 15 / 100, half_height + (image_height * 36 / 100));
            grid = new grid2D(dimension_x, dimension_y, test_poly, 0, false);
            grid.ShowLines(img, image_width, image_height, 0, 255, 0, 0);

            test_poly = new polygon2D();
            test_poly.Add(half_width + (image_width * 20 / 100), half_height + (image_height * 24 / 100));
            test_poly.Add(half_width + (image_width * 35 / 100), half_height + (image_height * 20 / 100));
            test_poly.Add(half_width + (image_width * 40 / 100), half_height + (image_height * 40 / 100));
            test_poly.Add(half_width + (image_width * 15 / 100), half_height + (image_height * 36 / 100));
            grid = new grid2D(dimension_x, dimension_y, test_poly, 0, false);
            grid.ShowLines(img, image_width, image_height, 0, 255, 0, 0);            

            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            bmp.Save(filename);
        }

        #endregion
    }
}
