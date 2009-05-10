using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// a 2D grid cell intended for use when applying 2D grids to images for applications such as checkerboard calibration
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
    /// represents a 2D grid
    /// </summary>
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

            float length3, length4;
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
                    int ty2 = ty + offset_x;
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
            //int ty = (int)search_region.top();
            int bx = (int)search_region.right();
            //int by = (int)search_region.bottom();

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
