/*
    Class which stores an image region, sometimes also known as a superpixel
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
using sluggish.utilities;

namespace sluggish.imageprocessing
{
    /// <summary>
    /// stores a region of an image, used during region growing
    /// </summary>
    public class region
    {
        // the type of region and any associated description
        public String classification; // eg text, grid pattern
        public String geometry_type;  // eg. square, triangle
        public String description;

        public float confidence;  // a confidence level which may be assigned to this region

        // top left corner of the region
        public int tx, ty;

        // dimensions of the region
        public int width, height;

        // binary array describing the shape of the region
        public bool[,] shape;

        // segmentation response - mainly used for debugging purposes
        public byte[,] segmented;

        // intensity image of the region
        public byte[] mono_image;

        // dominant orientation of the region
        public float orientation;

        // corner points defining the shape of the region
        public ArrayList corners;

        // corner features, such as FAST
        public ArrayList corner_features;

        // polygon describing the shape of the region
        public polygon2D polygon;

        // used to highlight one of the corners when drawing the region
        public int highlight_corner = -1;

        // centre point of the region, as found by region growing
        public int centre_x, centre_y;

        #region "resampling"

        public static region upsample(region original, float scale,
                                      byte[] bmp, int bmp_width, int bmp_height,
                                      bool propertiesOnly)
        {
            region upsampled = new region();

            upsampled.classification = original.classification;
            upsampled.geometry_type = original.geometry_type;
            upsampled.description = original.description;
            upsampled.tx = (int)(original.tx * scale);
            upsampled.ty = (int)(original.ty * scale);
            upsampled.width = (int)(original.width * scale);
            upsampled.height = (int)(original.height * scale);
            upsampled.orientation = original.orientation;
            upsampled.centre_x = (int)(original.centre_x * scale);
            upsampled.centre_y = (int)(original.centre_y * scale);
            upsampled.aspect_ratio = original.aspect_ratio;

            upsampled.corners = new ArrayList();
            for (int i = 0; i < original.corners.Count; i += 2)
            {
                float x = (float)original.corners[i] * scale;
                float y = (float)original.corners[i + 1] * scale;
                upsampled.corners.Add(x);
                upsampled.corners.Add(y);
            }

            upsampled.polygon = new polygon2D();
            for (int i = 0; i < original.polygon.x_points.Count; i++)
            {
                float x = (float)original.polygon.x_points[i] * scale;
                float y = (float)original.polygon.y_points[i] * scale;
                upsampled.polygon.Add(x, y);
            }

            if (!propertiesOnly)
            {
                upsampled.shape = new bool[upsampled.width, upsampled.height];
                upsampled.segmented = new byte[upsampled.width, upsampled.height];
                upsampled.mono_image = new byte[upsampled.width * upsampled.height];
                for (int y = 0; y < upsampled.height; y++)
                {
                    int y_original = (int)(y / scale);
                    for (int x = 0; x < upsampled.width; x++)
                    {
                        int x_original = (int)(x / scale);
                        upsampled.shape[x, y] = original.shape[x_original, y_original];
                        upsampled.segmented[x, y] = original.segmented[x_original, y_original];

                        int n1 = (y * upsampled.width) + x;
                        int n2 = (((upsampled.ty + y) * bmp_width) + (upsampled.tx + x)) * 3;
                        upsampled.mono_image[n1] = (byte)((bmp[n2] + bmp[n2 + 1] + bmp[n2 + 2]) / 3);
                    }
                }


                upsampled.corner_features = new ArrayList();
                for (int i = 0; i < original.corner_features.Count; i += 2)
                {
                    int x = (int)((int)original.corner_features[i] * scale);
                    int y = (int)((int)original.corner_features[i + 1] * scale);
                    upsampled.corner_features.Add(x);
                    upsampled.corner_features.Add(y);
                }

                upsampled.outline = new ArrayList();
                for (int i = 0; i < original.outline.Count; i += 2)
                {
                    int x = (int)((int)original.outline[i] * scale);
                    int y = (int)((int)original.outline[i + 1] * scale);
                    upsampled.outline.Add(x);
                    upsampled.outline.Add(y);
                }
            }

            return (upsampled);
        }

        #endregion

        #region "saving/loading/exporting"

        /// <summary>
        /// export this region as an image (array or bytes)
        /// The exported image is corrected for any rotation, so that it is always
        /// presented in a standard orientation, regardless of the original orientation of the region
        /// </summary>
        /// <param name="full_img">the original colour image from which the region was taken</param>
        /// <param name="full_img_width">original image width</param>
        /// <param name="full_img_height">original image height</param>
        /// <param name="image_width">returned image width</param>
        /// <param name="image_height">returned image height</param>
        /// <returns>region as a colour image</returns>
        public byte[] Export(byte[] full_img, int full_img_width, int full_img_height,
                             ref int image_width, ref int image_height)
        {
            byte[] img = null;
            image_width = 0;
            image_height = 0;

            if (polygon != null)
            {
                // square or rectangular shape
                if (polygon.x_points.Count == 4)
                {
                    // get the dimensions of the image to be returned
                    image_width = (int)polygon.getLongestSide();
                    image_height = (int)polygon.getShortestSide();
                }

                //if (image_height != image_width)
                {
                    img = new byte[image_width * image_height * 3];

                    for (int x = 0; x < image_width; x++)
                    {
                        float dx = x - (image_width / 2);
                        for (int y = 0; y < image_height; y++)
                        {
                            float dy = y - (image_height / 2);
                            float hyp = (float)Math.Sqrt((dx * dx) + (dy * dy));
                            if (hyp > 0)
                            {
                                float angle = (float)Math.Acos(dy / hyp);
                                if (dx < 0) angle = ((float)Math.PI * 2) - angle;
                                angle += (float)(Math.PI * 3 / 2);

                                //Console.WriteLine("orientation = " + ((int)(orientation / Math.PI * 180)).ToString());

                                float orient = orientation;
                                if (orient > Math.PI) orient -= (float)Math.PI;
                                if (orient < -Math.PI) orient += (float)Math.PI;

                                //angle -= orient - (float)(Math.PI);

                                //if (angle < 0) angle += (float)(Math.PI * 2);
                                //if (angle > Math.PI * 2) angle -= (float)(Math.PI * 2);
                                //if (angle > Math.PI * 2) angle -= (float)(Math.PI*2);
                                //if (angle > Math.PI) angle -= (float)Math.PI;


                                int x_original = (int)Math.Round(centre_x + (hyp * (float)Math.Cos(angle)));
                                int y_original = (int)Math.Round(centre_y - (hyp * (float)Math.Sin(angle)));
                                int n = ((y * image_width) + x) * 3;

                                if (full_img != null)
                                {
                                    int xx = tx + x_original;
                                    if ((xx > -1) && (xx < full_img_width))
                                    {
                                        int yy = ty + y_original;
                                        if ((yy > -1) && (yy < full_img_height))
                                        {
                                            int n_original = (((yy * full_img_width) + xx) * 3);
                                            for (int col = 0; col < 3; col++)
                                                img[n + col] = full_img[n_original + col];
                                        }
                                    }
                                }
                                else
                                {
                                    if ((x_original > -1) && (x_original < width))
                                    {
                                        if ((y_original > -1) && (y_original < height))
                                        {
                                            int n_original = (y_original * width) + x_original;
                                            for (int col = 0; col < 3; col++)
                                                img[n + col] = mono_image[n_original];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (img);
        }

        #endregion

        #region "initialisation / constructors"

        public region()
        {
        }

        public region(int tx, int ty,
                      int width, int height,
                      bool[,] region_image, int[,] segmented,
                      int centre_x, int centre_y,
                      ArrayList corner_features,
                      byte[] bmp_mono, int bmp_width, int bmp_height,
                      float vertex_inflation, float vertex_angular_offset,
                      int downsampling_factor)
        {
            this.tx = tx;
            this.ty = ty;
            this.width = width;
            this.height = height;
            this.centre_x = centre_x - tx;
            this.centre_y = centre_y - ty;

            shape = new bool[width, height];
            mono_image = new byte[width * height];

            if (segmented != null)
                this.segmented = new byte[width, height];

            for (int y = ty; y < ty + height; y++)
            {
                int yy = y - ty;
                int w1 = yy * width;
                int w2 = y * bmp_width;
                for (int x = tx; x < tx + width; x++)
                {
                    int xx = x - tx;

                    int xx2 = x / downsampling_factor;
                    int yy2 = y / downsampling_factor;
                    if ((xx2 < bmp_width / downsampling_factor) &&
                        (yy2 < bmp_height / downsampling_factor))
                    {
                        // downsampled position
                        int downsampled_x = x / downsampling_factor;
                        int downsampled_y = y / downsampling_factor;

                        // get the shape as a binary image
                        shape[xx, yy] = region_image[downsampled_x,
                                                     downsampled_y];

                        // copy the segmentation response
                        // this is mainly just for debugging purposes
                        if (segmented != null)
                            this.segmented[xx, yy] = (byte)segmented[downsampled_x, downsampled_y];

                        int n1 = w1 + xx;
                        int n2 = w2 + x;
                        mono_image[n1] = bmp_mono[n2];
                    }
                }
            }

            // record corner features which are inside the bounding box
            int corners_border = 10;  // a small extra border around the bounding box
            this.corner_features = new ArrayList();
            for (int i = 0; i < corner_features.Count; i += 2)
            {
                int x = (int)corner_features[i];
                int y = (int)corner_features[i + 1];
                if ((x >= tx - corners_border) && (x <= tx + width + corners_border))
                {
                    if ((y >= ty - corners_border) && (y <= ty + height + corners_border))
                    {
                        this.corner_features.Add(x);
                        this.corner_features.Add(y);
                    }
                }
            }
            //this.corner_features = corner_features;

            // trace around the outline of the shape
            int trace_downsampling = 4;
            if (width < 200) trace_downsampling = 2;
            if (width < 100) trace_downsampling = 1;
            traceOutline(trace_downsampling);

            // find corners
            int min_corner_separation = 15;
            int angular_step_size_degrees = 10;
            locateCorners(min_corner_separation,
                          2, angular_step_size_degrees);

            // detect the angle of each corner
            detectAngles();

            if (aspect_ratio > 1.3f)
                // for elongated shapes just calculate a bounding box
                // based upon minor and major axis lengths
                createCornersFromAxes();
            else
                // find corners
                fitCorners(this.corner_features, vertex_inflation, vertex_angular_offset);

            // create a polygon from the corners
            polygon = createPolygon();

            // classify the shape depending upon number of corners 
            // and angle properties
            classifyShape();
        }

        #endregion

        #region "attentional saliency"

        /// <summary>
        /// returns a simple measure of contrast by measuring the difference
        /// between the highest and lowest intensity pixel within the region
        /// </summary>
        /// <param name="min_intensity">minimum intensity value</param>
        /// <param name="max_intensity">maximum intensity value</param>
        /// <param name="radius">local averaging radius</param>
        public float getSimpleContrast(ref int min_intensity,
                                       ref int max_intensity,
                                       int radius)
        {
            // these correspond to Rmax and Rmin
            int high_intensity = 0;
            int low_intensity = 255;

            int step_size = radius;
            for (int x = step_size; x < width; x += step_size)
            {
                for (int y = step_size; y < height; y += step_size)
                {
                    int intensity = 0;
                    int hits = 0;
                    for (int xx = x - step_size; xx < x; xx++)
                    {
                        for (int yy = y - step_size; yy < y; yy++)
                        {
                            intensity += mono_image[(yy * width) + xx];
                            hits++;
                        }
                    }
                    intensity /= hits;
                    if (intensity < low_intensity) low_intensity = intensity;
                    if (intensity > high_intensity) high_intensity = intensity;
                }
            }

            float simple_contrast = (high_intensity - low_intensity) / 255.0f;

            min_intensity = low_intensity;
            max_intensity = high_intensity;

            return (simple_contrast);
        }

        /// <summary>
        /// texture contrast can be a useful measure of saliency when
        /// selecting possible regions of interest
        /// see "Texture contrast attracts overt visual attention in
        /// natural scenes" by Parkhurst & Niebur, 2004
        /// </summary>
        /// <param name="corner_features">list of corner features</param>
        public float getTextureContrast(ArrayList corner_features)
        {
            float texture_contrast = 0;

            for (int i = 0; i < corner_features.Count; i += 2)
            {
                int x = (int)corner_features[i] - tx;
                int y = (int)corner_features[i + 1] - ty;

                if ((x > 0) && (x < width - 1) &&
                    (y > 0) && (y < height - 1))
                {
                    if (shape[(int)x, (int)y])
                    {
                        // find the local minimum and maximum intensity
                        int intensity = mono_image[((int)y * width) + (int)x - 1];
                        int min_intensity = intensity;
                        int max_intensity = intensity;

                        intensity = mono_image[((int)y * width) + (int)x + 1];
                        if (intensity < min_intensity) min_intensity = intensity;
                        if (intensity > max_intensity) max_intensity = intensity;

                        intensity = mono_image[((int)(y - 1) * width) + (int)x];
                        if (intensity < min_intensity) min_intensity = intensity;
                        if (intensity > max_intensity) max_intensity = intensity;

                        intensity = mono_image[((int)(y + 1) * width) + (int)x];
                        if (intensity < min_intensity) min_intensity = intensity;
                        if (intensity > max_intensity) max_intensity = intensity;

                        // calculate the local contrast at this corner point
                        int local_contrast = max_intensity - min_intensity;

                        // update texture
                        texture_contrast += local_contrast;
                    }
                }
            }
            return (texture_contrast / (width * height));
        }

        #endregion

        #region "shape measurements"

        // angles of each corner
        public ArrayList angles;

        // the number of right angles within the shape
        private int no_of_right_angles;

        /// <summary>
        /// create a polygon from the corners
        /// </summary>
        private polygon2D createPolygon()
        {
            polygon2D new_polygon = new sluggish.utilities.polygon2D();
            for (int i = 0; i < corners.Count; i += 2)
            {
                float x = (float)corners[i];
                float y = (float)corners[i + 1];
                new_polygon.Add(x, y);
            }
            return (new_polygon);
        }

        public float getSquareness()
        {
            return (polygon.getSquareness());
        }

        /// <summary>
        /// classify the shape depending upon number of corners
        /// and angle properties
        /// </summary>
        private void classifyShape()
        {
            // classify the shape
            int no_of_corners = corners.Count / 2;

            if (no_of_corners == 3)
            {
                geometry_type = "triangle";
                if (no_of_right_angles == 1)
                    geometry_type = "right angle triangle";
            }

            if (no_of_corners == 4)
            {
                if (no_of_right_angles > 2)
                {
                    geometry_type = "square";
                    if (aspect_ratio > 1.3f)
                        geometry_type = "rectangle";
                }
            }
        }

        /// <summary>
        /// returns true if the region is horizontally oriented
        /// </summary>
        /// <returns></returns>
        public bool isHorizontal()
        {
            if ((orientation < Math.PI * 20 / 100) || (orientation > Math.PI * 280 / 100) ||
                ((orientation > Math.PI * 85 / 100) && (orientation < Math.PI * 115 / 100)))
                return (true);
            else
                return (false);
        }


        #endregion

        #region "integral image of the region"

        // integral image, which may be useful for subsequent processing
        // of the region
        public long[,] integral_image;

        /// <summary>
        /// update the integral image
        /// </summary>
        private void UpdateIntegralImage()
        {
            integral_image = sluggish.utilities.image.updateIntegralImage(mono_image, width, height);
        }

        #endregion

        #region "intensity histogram"

        /// <summary>
        /// returns an intensity histogram for the region
        /// </summary>
        public float[] GetIntensityHistogram(int no_of_levels)
        {
            float[] histogram = new float[no_of_levels];
            int max_intensity = 0;
            int min_intensity = 256;
            int max_hist = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (shape[x, y])
                    {
                        int intensity = mono_image[(y * width) + x];
                        // record min and max intensities
                        if (intensity < min_intensity) min_intensity = intensity;
                        if (intensity > max_intensity) max_intensity = intensity;
                        int level = intensity * (no_of_levels - 1) / 255;
                        histogram[level]++;
                        if (histogram[level] > max_hist) max_hist = (int)histogram[level];
                    }
                }
            }
            // normalise into the range 0 - 1.0
            if (max_hist > 0)
            {
                for (int i = 0; i < no_of_levels; i++)
                    histogram[i] = histogram[i] / max_hist;
            }
            return (histogram);
        }

        #endregion

        #region "background modeling"

        // binary image of the region after a threshold has been applied
        public bool[,] binary_image;

        // background model
        private int[,] background_high;
        private int[,] background_low;
        public float average_high;
        public float average_low;
        public float global_threshold;

        /// <summary>
        /// explicitly model the background, based upon observed corner features
        /// For each corner feature we examine four adjacent pixels and decide
        /// which have the highest and lowest intensity values.
        /// These intensity values are then used to update the high and low
        /// background models, using a radius and probability value
        /// </summary>
        /// <param name="corner_features">corner features</param>
        /// <param name="radius_percent">radius to use when updating the background models for each corner feature</param>
        /// <param name="corner_samples_percent">the percentage of the total number of corner features to use to generate the background models, int the range 0-100</param>
        /// <param name="downsampling_factor">downsampling factor used to speed up background modelling</param>
        public void UpdateBackground(ArrayList corner_features,
                                     int radius_percent,
                                     int corner_samples_percent,
                                     int downsampling_factor)
        {
            // this downsampling factor greatly increases the speed
            // of background model calculation, especially on large images
            int background_downsampling_factor = downsampling_factor;

            // to speed things along only sparsely
            // sample the features
            int corners_sampled = corner_features.Count * corner_samples_percent / 100;
            if (corners_sampled < 1) corners_sampled = 1;
            int step_size = corner_features.Count / corners_sampled;
            if (step_size < 1) step_size = 1;

            // convert radius from a percent into pixels
            int radius = (radius_percent * width / 100) / background_downsampling_factor;
            if (radius < 2) radius = 2;
            int radiusSquared = radius * radius;

            background_high = new int[width, height];
            background_low = new int[width, height];

            int[, ,] small_background_high = new int[width / background_downsampling_factor, height / background_downsampling_factor, 2];
            int[, ,] small_background_low = new int[width / background_downsampling_factor, height / background_downsampling_factor, 2];

            for (int i = 0; i < corner_features.Count; i += (2 * step_size))
            {
                int high_value = 0;
                int low_value = 255;

                int x = (int)corner_features[i] - tx;

                if ((x > -1) && (x < width))
                {
                    int y = (int)corner_features[i + 1] - ty;
                    if ((y > -1) && (y < height))
                    {
                        int n = 0;
                        int intensity = 0;

                        if (x > 0)  // left
                        {
                            n = (y * width) + x - 1;
                            intensity = mono_image[n];
                            if (intensity < low_value) low_value = intensity;
                            if (intensity > high_value) high_value = intensity;
                        }

                        if (x < width - 1) // right
                        {
                            n = (y * width) + x + 1;
                            intensity = mono_image[n];
                            if (intensity < low_value) low_value = intensity;
                            if (intensity > high_value) high_value = intensity;
                        }

                        if (y > 0)  // above
                        {
                            n = ((y - 1) * width) + x;
                            intensity = mono_image[n];
                            if (intensity < low_value) low_value = intensity;
                            if (intensity > high_value) high_value = intensity;

                            if (x > 0)
                            {
                                intensity = mono_image[n - 1];
                                if (intensity < low_value) low_value = intensity;
                                if (intensity > high_value) high_value = intensity;
                            }

                            if (x < width - 1)
                            {
                                intensity = mono_image[n + 1];
                                if (intensity < low_value) low_value = intensity;
                                if (intensity > high_value) high_value = intensity;
                            }
                        }

                        if (y < height - 1) // below
                        {
                            n = ((y + 1) * width) + x;
                            intensity = mono_image[n];
                            if (intensity < low_value) low_value = intensity;
                            if (intensity > high_value) high_value = intensity;

                            if (x > 0)
                            {
                                intensity = mono_image[n - 1];
                                if (intensity < low_value) low_value = intensity;
                                if (intensity > high_value) high_value = intensity;
                            }

                            if (x < width - 1)
                            {
                                intensity = mono_image[n + 1];
                                if (intensity < low_value) low_value = intensity;
                                if (intensity > high_value) high_value = intensity;
                            }
                        }

                        if (high_value > low_value)
                        {
                            int small_x = x / background_downsampling_factor;
                            int small_y = y / background_downsampling_factor;

                            for (int xx = small_x - radius; xx <= small_x + radius; xx++)
                            {
                                if ((xx > -1) && (xx < width / background_downsampling_factor))
                                {
                                    int dx = xx - small_x;
                                    for (int yy = small_y - radius; yy <= small_y + radius; yy++)
                                    {
                                        if ((yy > -1) && (yy < height / background_downsampling_factor))
                                        {
                                            int dy = yy - small_y;
                                            // note that square root is avoided here to save time
                                            float distSquared = (dx * dx) + (dy * dy);

                                            if (distSquared < radiusSquared)
                                            {
                                                // calculate lookup table position
                                                int probability = (int)(radius - Math.Sqrt(distSquared));
                                                small_background_high[xx, yy, 0] += high_value * probability;
                                                small_background_high[xx, yy, 1] += probability;
                                                small_background_low[xx, yy, 0] += low_value * probability;
                                                small_background_low[xx, yy, 1] += probability;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // average the values
            float average_value = 0;
            int average_value_hits = 0;
            for (int x = 0; x < width / background_downsampling_factor; x++)
            {
                for (int y = 0; y < height / background_downsampling_factor; y++)
                {
                    if (small_background_high[x, y, 1] > 0)
                        small_background_high[x, y, 0] /= small_background_high[x, y, 1];

                    if (small_background_low[x, y, 1] > 0)
                        small_background_low[x, y, 0] /= small_background_low[x, y, 1];

                    if (small_background_high[x, y, 0] > small_background_low[x, y, 0])
                    {
                        average_value += small_background_low[x, y, 0] + ((small_background_high[x, y, 0] - small_background_low[x, y, 0]) / 2.0f);
                        average_value_hits++;
                    }
                }
            }

            // calculate global threshold
            if (average_value_hits > 0)
                global_threshold = average_value / average_value_hits;

            // produce full size background model
            for (int x = 0; x < width; x++)
            {
                int xx = x * small_background_high.GetLength(0) / width;
                for (int y = 0; y < height; y++)
                {
                    int yy = y * small_background_high.GetLength(1) / height;
                    background_high[x, y] = small_background_high[xx, yy, 0];
                    background_low[x, y] = small_background_low[xx, yy, 0];
                }
            }

        }

        /// <summary>
        /// creates a binary image using the high and low background model
        /// each pixel intensity is compared to the two models, and the closest
        /// model is chosen to update the binary image
        /// </summary>
        /// <param name="binary_image_threshold">a threshold used to create the binary image, in the range 0-100</param>
        /// <param name="solitary_pixel_removal_itterations">the number of itterations to use when removing solitary pixels</param>
        /// <param name="minimum_surrounding_matches">when removing solitary pixels this gives the minimum number of surrounding pixels having the same state</param>
        public void CreateBinaryImage(float binary_image_threshold,
                                      int solitary_pixel_removal_itterations,
                                      int minimum_surrounding_matches)
        {
            binary_image = new bool[width, height];

            float binary_thresh = binary_image_threshold / 100.0f;

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    binary_image[x, y] = true;

                    int n = (y * width) + x;
                    int intensity = ((int)mono_image[n] +
                                     (int)mono_image[n - width] +
                                     (int)mono_image[n + 1]) / 3;

                    int diff_high = Math.Abs(intensity - background_high[x, y]);
                    int diff_low = Math.Abs(intensity - background_low[x, y]);
                    int variance = diff_high + diff_low;

                    // note that the threshold is proportional to the variance between high and low models
                    if (diff_high > diff_low + (int)(variance * binary_thresh))
                        binary_image[x, y] = false;
                }
            }

            // remove solitary pixels
            for (int i = 0; i < solitary_pixel_removal_itterations; i++)
                binary_image = sluggish.utilities.image.removeSolitaryPixels(binary_image, minimum_surrounding_matches);
        }

        #endregion

        #region "tracing the perimeter of the region"

        // line traced around the region
        public ArrayList outline;

        /// <summary>
        /// traces the outline of the region
        /// </summary>
        private void traceOutline(int downsampling_factor)
        {
            outline = new ArrayList();
            int x = 0, y = 0, x1 = 0, y1 = 0, dx, dy;
            for (x = 0; x < width; x += downsampling_factor)
            {
                y = 0;
                while ((y < height - 1) && (shape[x, y] == false)) y += downsampling_factor;
                if (y < height - 1)
                {
                    outline.Add(x);
                    outline.Add(y);
                    x1 = x;
                    y1 = y;
                }
            }
            bool first = true;
            for (x = width - 1; x >= 0; x -= downsampling_factor)
            {
                y = height - 1;
                while ((y > 0) && (shape[x, y] == false)) y -= downsampling_factor;
                if (y > 0)
                {
                    if (first)
                    {
                        dy = y - y1;
                        dx = x - x1;
                        for (int yy = y1; yy < y; yy += downsampling_factor)
                        {
                            int xx = x1 + (dx * (yy - y1) / dy);
                            outline.Add(xx);
                            outline.Add(yy);
                        }
                        first = false;
                    }
                    outline.Add(x);
                    outline.Add(y);
                    x1 = x;
                    y1 = y;
                }
            }

            if (outline.Count > 1)
            {
                x = (int)outline[0];
                y = (int)outline[1];
                dy = y1 - y;
                dx = x - x1;
                for (int yy = y1; yy > y; yy -= downsampling_factor)
                {
                    int xx = x1 + (dx * (y1 - yy) / dy);
                    outline.Add(xx);
                    outline.Add(yy);
                }
            }
        }

        #endregion

        #region "display functions"

        /// <summary>
        /// show the region within the given image
        /// </summary>
        /// <param name="bmp">image to draw into</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="bytes_per_pixel">number of bytes per pixel</param>
        /// <param name="line_width">line width to use when drawing</param>
        public void Show(byte[] bmp, int image_width, int image_height,
                         int bytes_per_pixel, int line_width, int style)
        {
            if (bytes_per_pixel == 3)
            {
                int[] colr = new int[3];
                colr[0] = 255;
                colr[1] = 255;
                colr[2] = 255;

                // use different colours for different types of region

                if (classification == "interesting area")
                {
                    colr[0] = 0;
                    colr[1] = 255;
                    colr[2] = 0;
                }

                if (classification == "datamatrix")
                {
                    colr[0] = 0;
                    colr[1] = 255;
                    colr[2] = 0;
                }

                if (classification == "text")
                {
                    colr[0] = 255;
                    colr[1] = 255;
                    colr[2] = 0;
                }


                /*
                if (geometry_type == "square")
                {
                    colr[0] = 0;
                    colr[1] = 255;
                    colr[2] = 255;
                }
                if (geometry_type == "triangle")
                {
                    colr[0] = 255;
                    colr[1] = 0;
                    colr[2] = 255;
                }
                */

                switch (style)
                {
                    case 0: // show boxes
                        {
                            float prev_x = 0, prev_y = 0;
                            float initial_x = -1, initial_y = 0;
                            float x = 0, y = 0;
                            for (int i = 0; i < corners.Count; i += 2)
                            {
                                x = tx + (float)corners[i];
                                y = ty + (float)corners[i + 1];

                                if (i > 0)
                                {
                                    sluggish.utilities.drawing.drawLine(
                                        bmp, image_width, image_height,
                                        (int)prev_x, (int)prev_y, (int)x, (int)y, colr[0], colr[1], colr[2],
                                        line_width, false);
                                }
                                else
                                {
                                    initial_x = x;
                                    initial_y = y;
                                }

                                prev_x = x;
                                prev_y = y;
                            }
                            if (initial_x > -1)
                            {
                                sluggish.utilities.drawing.drawLine(
                                    bmp, image_width, image_height,
                                    (int)initial_x, (int)initial_y, (int)x, (int)y, colr[0], colr[1], colr[2],
                                    line_width, false);
                            }
                            break;
                        }
                    case 1: // show colonisation
                        {
                            for (int x = tx; x < tx + width; x++)
                            {
                                for (int y = ty; y < ty + height; y++)
                                {
                                    if (shape[x - tx, y - ty])
                                    {
                                        int n = ((y * image_width) + x) * 3;
                                        for (int col = 0; col < 3; col++)
                                            bmp[n + col] = (byte)colr[col];
                                    }
                                }
                            }
                            break;
                        }
                    case 2: // show outline
                        {
                            float x = 0, y = 0;
                            float prev_x = 0;
                            float prev_y = 0;
                            for (int i = 0; i < outline.Count; i += 2)
                            {
                                x = tx + (int)outline[i];
                                y = ty + (int)outline[i + 1];

                                if (i > 0)
                                {
                                    sluggish.utilities.drawing.drawLine(
                                        bmp, image_width, image_height,
                                        (int)prev_x, (int)prev_y, (int)x, (int)y, colr[0], colr[1], colr[2],
                                        line_width, false);
                                }

                                prev_x = x;
                                prev_y = y;
                            }
                            // show corners
                            for (int i = 0; i < corners.Count; i += 2)
                            {
                                x = tx + (float)corners[i];
                                y = ty + (float)corners[i + 1];
                                if (i / 2 != highlight_corner)
                                {
                                    sluggish.utilities.drawing.drawCircle(
                                        bmp, image_width, image_height,
                                        (int)x, (int)y, 5, colr[0], colr[1], colr[2], line_width);
                                }
                                else
                                {
                                    sluggish.utilities.drawing.drawCircle(
                                        bmp, image_width, image_height,
                                        (int)x, (int)y, 5, 255, 0, 0, line_width + 1);
                                }

                            }
                            break;
                        }
                    case 3: // show orientations
                        {
                            int centre_xx = tx + centre_x;
                            int centre_yy = ty + centre_y;
                            int dx = (int)((major_axis_length / 2) * Math.Cos(orientation));
                            int dy = (int)((major_axis_length / 2) * Math.Sin(orientation));

                            sluggish.utilities.drawing.drawLine(
                                bmp, image_width, image_height,
                                centre_xx - dx, centre_yy - dy, centre_xx + dx, centre_yy + dy, colr[0], colr[1], colr[2],
                                line_width, false);

                            dx = (int)((minor_axis_length / 2) * Math.Cos(orientation - (Math.PI / 2)));
                            dy = (int)((minor_axis_length / 2) * Math.Sin(orientation - (Math.PI / 2)));

                            sluggish.utilities.drawing.drawLine(
                                bmp, image_width, image_height,
                                centre_xx - dx, centre_yy - dy, centre_xx + dx, centre_yy + dy, colr[0], colr[1], colr[2],
                                line_width, false);

                            break;
                        }
                    case 4: // binary threshold
                        {
                            if (binary_image != null)
                            {
                                for (int y = 0; y < height; y++)
                                    for (int x = 0; x < width; x++)
                                    //if (polygon.isInside(x, y))
                                    {
                                        int xx = tx + x;
                                        int yy = ty + y;
                                        int n = ((yy * image_width) + xx) * 3;
                                        for (int col = 0; col < 3; col++)
                                            if (binary_image[x, y])
                                                bmp[n + col] = (byte)255;
                                            else
                                                bmp[n + col] = (byte)0;
                                    }
                            }
                            break;
                        }
                    case 5: // background model low
                        {
                            if (binary_image != null)
                            {
                                for (int y = 0; y < height; y++)
                                    for (int x = 0; x < width; x++)
                                    {
                                        int xx = tx + x;
                                        int yy = ty + y;
                                        int n = ((yy * image_width) + xx) * 3;
                                        for (int col = 0; col < 3; col++)
                                            bmp[n + col] = (byte)background_low[x, y];
                                    }
                            }
                            break;
                        }
                    case 6: // background model high
                        {
                            if (binary_image != null)
                            {
                                for (int y = 0; y < height; y++)
                                    for (int x = 0; x < width; x++)
                                    {
                                        int xx = tx + x;
                                        int yy = ty + y;
                                        int n = ((yy * image_width) + xx) * 3;
                                        for (int col = 0; col < 3; col++)
                                            bmp[n + col] = (byte)background_high[x, y];
                                    }
                            }
                            break;
                        }
                    case 7: // polygon
                        {
                            if (polygon != null)
                            {
                                polygon.show(bmp, image_width, image_height, 255, 255, 0, 0, tx, ty);
                            }
                            break;
                        }
                    case 8: // spot responses
                        {
                            if (spot_map != null)
                            {
                                for (int y = 0; y < height; y++)
                                    for (int x = 0; x < width; x++)
                                    {
                                        if (polygon.isInside(x, y))
                                        {
                                            int xx = tx + x;
                                            int yy = ty + y;
                                            int n = ((yy * image_width) + xx) * 3;
                                            byte response_value = (byte)(spot_map[x, y] * 255);
                                            if (response_value > 30)
                                            {
                                                bmp[n] = 0;
                                                bmp[n + 1] = response_value;
                                                bmp[n + 2] = response_value;
                                            }
                                        }
                                    }
                            }
                            break;
                        }
                    case 9: // spot centres
                        {
                            if (spots != null)
                            {
                                polygon = createPolygon();
                                for (int y = 0; y < height; y++)
                                    for (int x = 0; x < width; x++)
                                    {
                                        if (polygon.isInside(x, y))
                                        {
                                            int xx = tx + x;
                                            int yy = ty + y;
                                            int n = ((yy * image_width) + xx) * 3;
                                            byte value = (byte)(spot_map[x, y] * 255);
                                            if (value > 5)
                                            {
                                                bmp[n] = value;
                                                bmp[n + 1] = 0;
                                                bmp[n + 2] = 0;
                                            }
                                        }
                                    }
                                for (int i = 0; i < spots.Count; i++)
                                {
                                    blob spot = (blob)spots[i];
                                    int n = (((ty + (int)Math.Round(spot.interpolated_y)) * image_width) + (tx + (int)Math.Round(spot.interpolated_x))) * 3;
                                    bmp[n] = (byte)255;
                                    bmp[n + 1] = (byte)255;
                                    bmp[n + 2] = (byte)255;
                                }
                            }
                            break;
                        }
                    case 10: // spots
                        {
                            if (spots != null)
                            {
                                for (int i = 0; i < spots.Count; i++)
                                {
                                    blob spot = (blob)spots[i];
                                    int x = tx + (int)Math.Round(spot.interpolated_x);
                                    int y = ty + (int)Math.Round(spot.interpolated_y);
                                    int radius = (int)Math.Round(spot.average_radius);

                                    int r = 0;
                                    int g = 255;
                                    int b = 0;
                                    if (spot.selected)
                                    {
                                        r = 255;
                                    }
                                    if (spot.touched)
                                    {
                                        r = 255;
                                        g = 0;
                                        b = 255;
                                    }

                                    sluggish.utilities.drawing.drawCircle(bmp, image_width, image_height,
                                                                            x, y, radius, r, g, b, 0);
                                }
                            }
                            break;
                        }
                    case 11: // connected points
                        {
                            if (spots != null)
                            {
                                for (int i = 0; i < spots.Count; i++)
                                {
                                    blob spot = (blob)spots[i];
                                    int x1 = tx + (int)spot.x;
                                    int y1 = ty + (int)spot.y;

                                    for (int j = 0; j < spot.neighbours.Count; j++)
                                    {
                                        blob neighbour = (blob)spot.neighbours[j];
                                        int x2 = tx + (int)neighbour.x;
                                        int y2 = ty + (int)neighbour.y;

                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height,
                                                                            x1, y1, x2, y2, 0, 255, 0,
                                                                            0, false);
                                    }
                                }
                            }
                            break;
                        }
                    case 12: // shear angle
                        {
                            if (shear_angle_point != null)
                            {
                                int x0 = tx + (int)(shear_angle_point[0, 0]);
                                int y0 = ty + (int)(shear_angle_point[0, 1]);
                                int x1 = tx + (int)(shear_angle_point[1, 0]);
                                int y1 = ty + (int)(shear_angle_point[1, 1]);
                                int x2 = tx + (int)(shear_angle_point[2, 0]);
                                int y2 = ty + (int)(shear_angle_point[2, 1]);

                                sluggish.utilities.drawing.drawLine(bmp, image_width, image_height,
                                                                    x0, y0, x1, y1, 0, 255, 0,
                                                                    0, false);
                                sluggish.utilities.drawing.drawLine(bmp, image_width, image_height,
                                                                    x1, y1, x2, y2, 0, 255, 0,
                                                                    0, false);
                            }
                            break;
                        }
                    case 13: // square/rectangle detection
                        {
                            /*
                                if (square_shape != null)
                                {
                                    int prev_x = 0;
                                    int prev_y = 0;
                                    for (int i = 0; i < square_shape.x_points.Count+1; i++)
                                    {
                                        int index = i;
                                        if (index >= square_shape.x_points.Count)
                                            index -= square_shape.x_points.Count;
                                  
                                        int x = tx + (int)square_shape.x_points[index];
                                        int y = ty + (int)square_shape.y_points[index];

                                        if (i > 0)                                
                                            sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, prev_x, prev_y, x, y, 0,255,0, 0, false);
                                
                                        prev_x = x;
                                        prev_y = y;
                                    }
                                }
                                */
                            break;
                        }

                    case 14: // edges
                        {
                            for (int i = 0; i < bmp.Length; i++)
                            {
                                int v = (int)(bmp[i] / 2.5f);
                                bmp[i] = (byte)v;
                            }

                            if (spot_radius > 0)
                            {
                                int grid_padding = 2;
                                float grid_fitting_pixels_per_index = 1.12f;
                                int edge_tracing_search_depth = 2;
                                float edge_tracing_threshold = 0.24f;
                                float suppression_radius_factor = 1.23f;
                                ArrayList horizontal_lines = null;
                                ArrayList vertical_lines = null;
                                float[] grid_spacing_horizontal = null;
                                float[] grid_spacing_vertical = null;
                                float dominant_orientation = 0;
                                float secondary_orientation = 0;
                                float shear_angle_radians = 0;
                                ArrayList horizontal_maxima = null;
                                ArrayList vertical_maxima = null;
                                polygon2D grid = fitGrid(ref horizontal_lines,
                                                         ref vertical_lines,
                                                         grid_fitting_pixels_per_index,
                                                         ref dominant_orientation,
                                                         ref secondary_orientation,
                                                         ref grid_spacing_horizontal,
                                                         ref grid_spacing_vertical,
                                                         ref horizontal_maxima,
                                                         ref vertical_maxima,
                                                         ref shear_angle_radians,
                                                         ref shear_angle_point,
                                                         grid_padding,
                                                         suppression_radius_factor,
                                                         edge_tracing_search_depth,
                                                         edge_tracing_threshold);

                                for (int i = 0; i < vertical_lines.Count; i++)
                                {
                                    linefeature line = (linefeature)vertical_lines[i];
                                    float x0 = tx + line.x0;
                                    float y0 = ty + line.y0;
                                    float x1 = tx + line.x1;
                                    float y1 = ty + line.y1;

                                    sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)x0, (int)y0, (int)x1, (int)y1, 255, 255, 0, 0, false);
                                }

                                for (int i = 0; i < horizontal_lines.Count; i++)
                                {
                                    linefeature line = (linefeature)horizontal_lines[i];
                                    float x0 = tx + line.x0;
                                    float y0 = ty + line.y0;
                                    float x1 = tx + line.x1;
                                    float y1 = ty + line.y1;

                                    sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)x0, (int)y0, (int)x1, (int)y1, 255, 255, 0, 0, false);
                                }

                                float line_length = image_height / 2;
                                float dxx = line_length * (float)Math.Sin(grid_orientation);
                                float dyy = line_length * (float)Math.Cos(grid_orientation);
                                float dxx2 = line_length * (float)Math.Sin(grid_orientation + shear_angle + (Math.PI / 2));
                                float dyy2 = line_length * (float)Math.Cos(grid_orientation + shear_angle + (Math.PI / 2));
                                float cx = tx + centre_x;
                                float cy = ty + centre_y;
                                //sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)(cx + dxx), (int)(cy + dyy), (int)(cx - dxx), (int)(cy - dyy), 255, 0, 0, 0, false);
                                //sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)(cx + dxx2), (int)(cy + dyy2), (int)(cx - dxx2), (int)(cy - dyy2), 255, 255, 255, 0, false);


                                dxx = line_length * (float)Math.Sin(grid_orientation);
                                dyy = line_length * (float)Math.Cos(grid_orientation);
                                //dxx2 = line_length * (float)Math.Sin(grid_orientation + shear_angle + (Math.PI / 2));
                                //dyy2 = line_length * (float)Math.Cos(grid_orientation + shear_angle + (Math.PI / 2));
                                dxx2 = line_length * (float)Math.Sin(grid_secondary_orientation);
                                dyy2 = line_length * (float)Math.Cos(grid_secondary_orientation);
                                cx = tx + centre_x;
                                cy = ty + centre_y;
                                //sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)(cx + dxx), (int)(cy + dyy), (int)(cx - dxx), (int)(cy - dyy), 255, 0, 0, 0, false);
                                //sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)(cx + dxx2), (int)(cy + dyy2), (int)(cx - dxx2), (int)(cy - dyy2), 255, 255, 255, 0, false);
                                sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)(cx + dxx), (int)(cy + dyy), (int)(cx), (int)(cy), 255, 0, 0, 0, false);
                                sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, (int)(cx + dxx2), (int)(cy + dyy2), (int)(cx), (int)(cy), 255, 255, 255, 0, false);

                            }

                            break;
                        }
                    case 15: // spatial frequency histogram
                        {
                            if (spatial_frequency_histogram != null)
                            {
                                // clear the image
                                for (int i = 0; i < bmp.Length; i++)
                                    bmp[i] = 0;

                                // find the maximum non zero index, so that we can scale the graph over the width of the image
                                int max_index = 1;
                                for (int d = 0; d < spatial_frequency_histogram.Length; d++)
                                    if (spatial_frequency_histogram[d] > 0.05f) max_index = d;
                                max_index += 2;
                                if (max_index >= spatial_frequency_histogram.Length)
                                    max_index = spatial_frequency_histogram.Length - 1;

                                // draw the histogram                            
                                int prev_x = 0;
                                int prev_y = image_height - 1;
                                for (int d = 0; d < max_index; d++)
                                {
                                    int x = d * (image_width - 1) / max_index;
                                    int y = image_height - 1 - (int)(spatial_frequency_histogram[d] * (image_height - 1));
                                    sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, prev_x, prev_y, x, y, 0, 255, 0, 0, false);
                                    prev_x = x;
                                    prev_y = y;
                                }
                            }
                            break;
                        }
                    case 16: // grid spacings
                        {
                            for (int i = 0; i < bmp.Length; i++)
                                bmp[i] = 0;

                            if ((spot_radius > 0) && (grid_graph_horizontal != null))
                            {
                                for (int axis = 0; axis < 2; axis++)
                                {
                                    int prev_x = 0, prev_y = 0;
                                    int start_index = 0;
                                    int end_index = 0;
                                    float[] grid_spacing = grid_graph_horizontal;
                                    if (axis == 1) grid_spacing = grid_graph_vertical;
                                    for (int i = 0; i < grid_spacing.Length; i++)
                                    {
                                        if (grid_spacing[i] > 0)
                                        {
                                            end_index = i;
                                            if (start_index == 0)
                                                start_index = i;
                                        }
                                        i++;
                                    }
                                    if (end_index > start_index)
                                    {
                                        for (int i = 0; i < grid_spacing.Length; i++)
                                        {
                                            int x = (i - start_index) * image_width / (end_index - start_index);
                                            int y = image_height - 1 - (int)(grid_spacing[i] * ((image_height - 1) / 2)) - (image_height * (1 - axis) / 2);
                                            if (i > 0)
                                                sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, prev_x, prev_y, x, y, 0, 255, 0, 0, false);
                                            prev_x = x;
                                            prev_y = y;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case 17: // grid lines
                        {
                            if ((spot_radius > 0) && (grid_horizontal_maxima != null))
                            {
                                int line_length = width * 120 / 100;
                                float secondary_orientation = grid_secondary_orientation; // + shear_angle + (float)(Math.PI / 2);
                                for (int axis = 0; axis < 2; axis++)
                                {
                                    ArrayList grid_maxima = grid_horizontal_maxima;
                                    float orient = grid_orientation;
                                    if (axis == 1)
                                    {
                                        grid_maxima = grid_vertical_maxima;
                                        orient = secondary_orientation;
                                    }
                                    for (int i = 0; i < grid_maxima.Count; i++)
                                    {
                                        float r = (float)grid_maxima[i];
                                        int x0 = tx + centre_x + (int)(r * Math.Sin(orient));
                                        int y0 = ty + centre_y + (int)(r * Math.Cos(orient));
                                        int dx = (int)(line_length / 2 * Math.Sin(orient + (Math.PI / 2)));
                                        int dy = (int)(line_length / 2 * Math.Cos(orient + (Math.PI / 2)));
                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, x0, y0, x0 + dx, y0 + dy, 0, 255, 0, 0, false);
                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, x0, y0, x0 - dx, y0 - dy, 0, 255, 0, 0, false);
                                    }
                                }

                                /*
                                polygon2D cell = getGridCellPerimeter(10, 5,
                                                                      horizontal_maxima, vertical_maxima,
                                                                      dominant_orientation);
                                cell.show(bmp, image_width, image_height, 255, 0, 0, 0, tx, ty);
                                 */
                            }

                            break;
                        }
                    case 18:  // grid non-uniformity
                        {
                            if (polygon != null)
                            {
                                int line_length = width;
                                float secondary_orientation = grid_secondary_orientation; // + shear_angle + (float)(Math.PI / 2);
                                for (int axis = 0; axis < 2; axis++)
                                {
                                    ArrayList grid_maxima = grid_horizontal_maxima;
                                    float orient = grid_orientation;
                                    if (axis == 1)
                                    {
                                        grid_maxima = grid_vertical_maxima;
                                        orient = secondary_orientation;
                                    }
                                    for (int i = 0; i < grid_maxima.Count; i++)
                                    {
                                        float r = (float)grid_maxima[i];
                                        int x0 = tx + centre_x + (int)(r * Math.Sin(orient));
                                        int y0 = ty + centre_y + (int)(r * Math.Cos(orient));
                                        int dx = (int)(line_length / 2 * Math.Sin(orient + (Math.PI / 2)));
                                        int dy = (int)(line_length / 2 * Math.Cos(orient + (Math.PI / 2)));
                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, x0, y0, x0 + dx, y0 + dy, 255, 0, 0, 0, false);
                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, x0, y0, x0 - dx, y0 - dy, 255, 0, 0, 0, false);
                                    }
                                }

                                if (polygon.x_points.Count == 4)
                                {
                                    float x0 = (float)polygon.x_points[0];
                                    float y0 = (float)polygon.y_points[0];
                                    float x1 = (float)polygon.x_points[1];
                                    float y1 = (float)polygon.y_points[1];
                                    float x2 = (float)polygon.x_points[2];
                                    float y2 = (float)polygon.y_points[2];
                                    float x3 = (float)polygon.x_points[3];
                                    float y3 = (float)polygon.y_points[3];

                                    float dx_top = x1 - x0;
                                    float dy_top = y1 - y0;
                                    float dx_bottom = x2 - x3;
                                    float dy_bottom = y2 - y3;
                                    float dx_left = x3 - x0;
                                    float dy_left = y3 - y0;
                                    float dx_right = x2 - x1;
                                    float dy_right = y2 - y1;

                                    for (int grid_x = 0; grid_x < grid_columns; grid_x++)
                                    {
                                        float x_top = x0 + (grid_x * dx_top / grid_columns);
                                        float x_bottom = x3 + (grid_x * dx_bottom / grid_columns);
                                        float y_top = y0 + (grid_x * dy_top / grid_columns);
                                        float y_bottom = y3 + (grid_x * dy_bottom / grid_columns);
                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, tx + (int)x_top, ty + (int)y_top, tx + (int)x_bottom, ty + (int)y_bottom, 0, 255, 0, 0, false);
                                    }
                                    for (int grid_y = 0; grid_y < grid_rows; grid_y++)
                                    {
                                        float x_left = x0 + (grid_y * dx_left / grid_rows);
                                        float x_right = x1 + (grid_y * dx_right / grid_rows);
                                        float y_left = y0 + (grid_y * dy_left / grid_rows);
                                        float y_right = y1 + (grid_y * dy_right / grid_rows);
                                        sluggish.utilities.drawing.drawLine(bmp, image_width, image_height, tx + (int)x_left, ty + (int)y_left, tx + (int)x_right, ty + (int)y_right, 0, 255, 0, 0, false);
                                    }

                                }
                            }


                            break;
                        }
                    case 19:  // corner features
                        {
                            if (corner_features != null)
                            {
                                for (int i = 0; i < corner_features.Count; i += 2)
                                {
                                    int x = (int)corner_features[i];
                                    int y = (int)corner_features[i + 1];

                                    int n = ((y * image_width) + x) * 3;
                                    bmp[n] = 0;
                                    bmp[n + 1] = (byte)255;
                                    bmp[n + 2] = 0;
                                }
                            }
                            break;
                        }
                    case 20: // show segmentation responses
                        {
                            if (segmented != null)
                            {
                                for (int x = tx; x < tx + width; x++)
                                {
                                    for (int y = ty; y < ty + height; y++)
                                    {
                                        byte v = (byte)segmented[x - tx, y - ty];
                                        int n = ((y * image_width) + x) * 3;
                                        for (int col = 0; col < 3; col++) bmp[n + col] = v;
                                    }
                                }
                            }
                            break;
                        }
                    case 21: // horizontal maxima
                        {
                            int local_radius = 2;
                            int inhibitory_radius = image_width / 50;
                            int min_intensity = 500;
                            int max_intensity = 2500;
                            int image_threshold = 5;
                            int localAverageRadius = 500;
                            int difference_threshold = 35;
                            int step_size = 2;
                            int max_features_per_row = 9;
                            float average_magnitude_horizontal = 0;
                            ArrayList[] hor_maxima = image.horizontal_maxima(bmp, image_width, image_height, 3,
                                                                         max_features_per_row, local_radius, inhibitory_radius,
                                                                         min_intensity, max_intensity,
                                                                         image_threshold, localAverageRadius,
                                                                         difference_threshold, step_size,
                                                                         ref average_magnitude_horizontal);

                            if (hor_maxima != null)
                            {
                                for (int y = 0; y < image_height; y += step_size)
                                {
                                    int no_of_features = hor_maxima[y].Count;
                                    for (int i = 0; i < no_of_features; i += 2)
                                    {
                                        float x = (float)hor_maxima[y][i];
                                        float magnitude = (float)hor_maxima[y][i + 1];

                                        if (magnitude > average_magnitude_horizontal * 0.3f)
                                        {
                                            int radius = 2;

                                            int r = 0;
                                            int g = 255;
                                            int b = 0;

                                            sluggish.utilities.drawing.drawCircle(bmp, image_width, image_height,
                                                                                  (int)x, y, radius, r, g, b, 0);
                                        }
                                    }
                                }
                            }

                            float average_magnitude_vertical = 0;
                            ArrayList[] ver_maxima = image.vertical_maxima(bmp, image_width, image_height, 3,
                                                                         max_features_per_row, local_radius, inhibitory_radius,
                                                                         min_intensity, max_intensity,
                                                                         image_threshold, localAverageRadius,
                                                                         difference_threshold, step_size,
                                                                         ref average_magnitude_vertical);
                            if (ver_maxima != null)
                            {
                                for (int x = 0; x < image_width; x += step_size)
                                {
                                    int no_of_features = ver_maxima[x].Count;
                                    for (int i = 0; i < no_of_features; i += 2)
                                    {
                                        float y = (float)ver_maxima[x][i];
                                        float magnitude = (float)ver_maxima[x][i + 1];

                                        if (magnitude > average_magnitude_vertical * 0.3f)
                                        {
                                            int radius = 2;

                                            int r = 0;
                                            int g = 255;
                                            int b = 0;

                                            sluggish.utilities.drawing.drawCircle(bmp, image_width, image_height,
                                                                                  x, (int)y, radius, r, g, b, 0);
                                        }
                                    }
                                }
                            }
                            break;
                        }


                }

            }
            else sluggish.utilities.logging.EventLog.AddEvent("Can't display regions in a mono image");
        }

        #endregion

        #region "fitting a grid as closely as possible to a square/checkerboard pattern"

        public float grid_orientation;           // orientation of the grid in radians
        public float grid_secondary_orientation; // orientation perpendicular(or nearly so) to the main grid orientation in radians
        public float[] grid_graph_horizontal;    // graph showing horizontal grid responses
        public float[] grid_graph_vertical;      // graph showing vertical grid responses
        public ArrayList grid_horizontal_maxima; // list of horizontal grid spacing positions
        public ArrayList grid_vertical_maxima;   // list of vertical grid spacing positions
        public int grid_rows, grid_columns;      // dimensions of the grid

        /// <summary>
        /// returns the average grid spacing for either horizontal or vertical axes
        /// </summary>
        /// <param name="horizontal">return value for the horizontal axis</param>
        public float getAverageGridSpacing(bool horizontal)
        {
            float av_spacing = 0;
            ArrayList grid_maxima = grid_vertical_maxima;

            if (horizontal)
                grid_maxima = grid_horizontal_maxima;

            if (grid_maxima != null)
            {
                float prev_maxima = 0;
                int hits = 0;
                for (int i = 0; i < grid_maxima.Count; i++)
                {
                    float maxima = (float)grid_maxima[i];
                    if (i > 0)
                    {
                        av_spacing += (maxima - prev_maxima);
                        hits++;
                    }
                    prev_maxima = maxima;
                }
                if (hits > 0)
                    av_spacing /= hits;
            }

            return (av_spacing);
        }

        /// <summary>
        /// fits a grid to a region containing square or checkerboard pattern
        /// </summary>
        /// <param name="dominant_orientation">the main orientation of the region</param>
        /// <param name="secondary_orientation">orientation perpendicular(or nearly so) to the main region orientation</param>
        /// <param name="grid_spacing_horizontal">graph showing horizontal grid spacing responses</param>
        /// <param name="grid_spacing_vertical">graph showing vertical grid spacing responses</param>
        /// <param name="shear_angle_radians">shear angle in radians</param>
        /// <param name="grid_padding">padding cells around the perimeter of the grid</param>
        /// <param name="suppression_radius_factor">scaling factor used to adjust non-maximal suppression radius when finding grid spacings, typically in the range 1.0-3.0</param>
        /// <param name="edge_tracing_search_depth">when tracing along edges to build line features this defines the depth of search to use</param>
        /// <param name="edge_tracing_threshold">a threshold applied to the spot map to produce edge features</param>
        public polygon2D fitGrid(ref float dominant_orientation,
                                 ref float secondary_orientation,
                                 ref float[] grid_spacing_horizontal,
                                 ref float[] grid_spacing_vertical,
                                 ref float shear_angle_radians,
                                 ref float[,] shear_angle_point,
                                 int grid_padding,
                                 float suppression_radius_factor,
                                 int edge_tracing_search_depth,
                                 float edge_tracing_threshold,
                                 float grid_fitting_pixels_per_index)
        {
            ArrayList horizontal_lines = null;
            ArrayList vertical_lines = null;
            ArrayList horizontal_maxima = null;
            ArrayList vertical_maxima = null;
            polygon2D grid = fitGrid(ref horizontal_lines,
                                     ref vertical_lines,
                                     grid_fitting_pixels_per_index,
                                     ref dominant_orientation,
                                     ref secondary_orientation,
                                     ref grid_spacing_horizontal,
                                     ref grid_spacing_vertical,
                                     ref horizontal_maxima,
                                     ref vertical_maxima,
                                     ref shear_angle_radians,
                                     ref shear_angle_point,
                                     grid_padding,
                                     suppression_radius_factor,
                                     edge_tracing_search_depth,
                                     edge_tracing_threshold);
            return (grid);
        }

        /// <summary>
        /// fills in any missing data in a list which contains a series
        /// of positions representing the detected lines within a grid pattern
        /// </summary>
        /// <param name="maxima">positions of each line</param>
        /// <param name="buffer_cells">add this number of buffer cells to the start and end of the data</param>
        /// <returns></returns>
        private ArrayList fillGrid(ArrayList maxima,
                                   int buffer_cells)
        {
            ArrayList filled = new ArrayList();

            // calculate the average grid spacing width
            float average_width = 0;
            float prev_dist = 0;
            int hits = 0;
            for (int i = 0; i < maxima.Count; i++)
            {
                float dist = (float)maxima[i];
                if (i > 0)
                {
                    float width = Math.Abs(dist - prev_dist);
                    average_width += width;
                    hits++;
                }
                prev_dist = dist;
            }
            if (hits > 0)
            {
                // find the average
                average_width /= hits;

                // add an initial buffer
                for (int i = 0; i < buffer_cells; i++)
                {
                    float dist = (float)maxima[0] - ((buffer_cells - i) * average_width);
                    filled.Add(dist);
                }

                // compare each spacing to the average
                float max_width = average_width * 1.3f;
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
                            for (int j = 1; j <= (int)(width / average_width); j++)
                            {
                                float intermediate_dist = prev_dist + (j * average_width);
                                if (Math.Abs(dist - intermediate_dist) > average_width * 0.5f)
                                {
                                    filled.Add(intermediate_dist);
                                }
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
                    float dist = (float)maxima[maxima.Count - 1] + (i * average_width);
                    filled.Add(dist);
                }

            }

            return (filled);
        }

        /// <summary>
        /// equalise the spacing between grid lines
        /// </summary>
        /// <param name="maxima">positions of maxima which correspond to grid lines</param>
        /// <returns>equalised maxima</returns>
        private ArrayList equaliseGrid(ArrayList maxima)
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

        /// <summary>
        /// turn maxima into an ideal grid with perfectly regular spacing
        /// </summary>
        /// <param name="maxima">positions of maxima which correspond to grid lines</param>
        /// <returns>ideal maxima</returns>
        private ArrayList idealGrid(ArrayList maxima)
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
        /// returns an array containing grid occupancy, average pixel intensity
        /// and total pixel count for each cell
        /// </summary>
        /// <param name="black_on_white">whether this region contains darker features on a lighter background</param>
        /// <param name="horizontal_maxima">horizontal grid line positions</param>
        /// <param name="vertical_maxima">vertical grid line positions</param>
        /// <param name="dominant_orientation">orientation of the grid pattern</param>
        /// <param name="secondary_orientation">orientation perpendicular (or nearly so) to the main grid orientation</param>
        /// <param name="shear angle_radians">deviation from perfectly perpendicular axes</param>
        /// <returns>array containing grid occupancy, average pixel intensity and total pixel count for each cell</returns>
        public float[, ,] getGridOccupancy(bool black_on_white,
                                          ArrayList horizontal_maxima,
                                          ArrayList vertical_maxima,
                                          float dominant_orientation,
                                          float secondary_orientation,
                                          float shear_angle_radians)
        {
            float[, ,] occupancy = null;
            if ((vertical_maxima != null) && (horizontal_maxima != null))
            {
                // create the array
                occupancy = new float[vertical_maxima.Count,
                                      horizontal_maxima.Count, 13];

                for (int grid_x = 0; grid_x < vertical_maxima.Count - 1; grid_x++)
                {
                    for (int grid_y = 0; grid_y < horizontal_maxima.Count - 1; grid_y++)
                    {
                        // get the occupancy data for for this grid cell
                        float average_intensity = 0;
                        float diff_from_global_threshold = 9999;
                        int cell_pixels = 0;
                        int occupied_pixels = 0;
                        ArrayList grid_cell_perimeter = null;
                        occupancy[grid_x, grid_y, 0] = getGridCellOccupancy(grid_x, grid_y,
                                                                            black_on_white,
                                                                            horizontal_maxima,
                                                                            vertical_maxima,
                                                                            dominant_orientation,
                                                                            secondary_orientation,
                                                                            shear_angle_radians,
                                                                            ref average_intensity,
                                                                            ref diff_from_global_threshold,
                                                                            ref occupied_pixels,
                                                                            ref cell_pixels,
                                                                            ref grid_cell_perimeter);
                        occupancy[grid_x, grid_y, 1] = average_intensity;
                        occupancy[grid_x, grid_y, 2] = diff_from_global_threshold;
                        occupancy[grid_x, grid_y, 3] = cell_pixels;
                        occupancy[grid_x, grid_y, 4] = occupied_pixels;
                        if (grid_cell_perimeter != null)
                        {
                            for (int i = 0; i < grid_cell_perimeter.Count; i++)
                                occupancy[grid_x, grid_y, 5 + i] = (float)grid_cell_perimeter[i];
                        }
                    }
                }
            }
            return (occupancy);
        }

        /// <summary>
        /// returns the occupancy of a grid cell at the given coordinate
        /// </summary>
        /// <param name="grid_x">grid x coordinate</param>
        /// <param name="grid_y">grid y coordinate</param>
        /// <param name="black_on_white">whether this region contains darker features on a lighter background</param>
        /// <param name="horizontal_maxima">horizontal grid line positions</param>
        /// <param name="vertical_maxima">vertical grid line positions</param>
        /// <param name="dominant_orientation">orientation of the grid pattern</param>
        /// <param name="secondary_orientation">orientation perpendicular (or nearly so) to the main grid orientation</param>
        /// <param name="shear angle_radians">deviation from perfectly perpendicular axes</param>
        /// <param name="average_intensity">average pixel intensity value in the range 0-255</param>
        /// <param name="diff_from_global_threshold">average difference of pixel intensities from the global threshold</param>
        /// <param name="occupied_pixels">the total number of pixels occupied</param>
        /// <param name="total_pixels">the total number of pixels inside the grid cell</param>
        /// <param name="cell_perimeter">four corner coordinates describing the perimeter of the grid cell</param>
        /// <returns>occupancy value in the range 0.0 - 1.0</returns>
        private float getGridCellOccupancy(int grid_x, int grid_y,
                                           bool black_on_white,
                                           ArrayList horizontal_maxima,
                                           ArrayList vertical_maxima,
                                           float dominant_orientation,
                                           float secondary_orientation,
                                           float shear_angle_radians,
                                           ref float average_intensity,
                                           ref float diff_from_global_threshold,
                                           ref int occupied_pixels,
                                           ref int total_pixels,
                                           ref ArrayList cell_perimeter)
        {
            // get the perimeter of the grid cell
            polygon2D perimeter = getGridCellPerimeter(grid_x, grid_y,
                                             horizontal_maxima,
                                             vertical_maxima,
                                             dominant_orientation,
                                             secondary_orientation,
                                             shear_angle_radians);

            cell_perimeter = new ArrayList();
            for (int i = 0; i < perimeter.x_points.Count; i++)
            {
                cell_perimeter.Add(tx + (float)perimeter.x_points[i]);
                cell_perimeter.Add(ty + (float)perimeter.y_points[i]);
            }

            // get the bounding box from the perimeter shape
            int left = (int)Math.Round(perimeter.left()) - 1;
            int top = (int)Math.Round(perimeter.top()) - 1;
            int right = (int)Math.Round(perimeter.right()) + 1;
            int bottom = (int)Math.Round(perimeter.bottom()) + 1;

            // search within the bounding box for occupied pixels
            average_intensity = 0;
            diff_from_global_threshold = 0;
            float occupancy = 0;
            total_pixels = 0;
            occupied_pixels = 0;
            for (int x = left; x <= right; x++)
            {
                if ((x > -1) && (x < width))
                {
                    for (int y = top; y <= bottom; y++)
                    {
                        if ((y > -1) && (y < height))
                        {
                            // is this coordinate inside the perimeter?
                            if (perimeter.isInside(x, y))
                            {
                                int intensity = mono_image[(y * width) + x];

                                if (((black_on_white) && (!binary_image[x, y])) ||
                                    ((!black_on_white) && (binary_image[x, y])))
                                {
                                    // increment occupied pixels
                                    occupied_pixels++;
                                }

                                // increment total number of pixels
                                total_pixels++;

                                // update the average pixel intensity
                                average_intensity += intensity;

                                // difference from global threshold
                                diff_from_global_threshold += Math.Abs(intensity - global_threshold);
                            }
                        }
                    }
                }
            }

            // calculate the ratio of occupied pixels
            if (total_pixels > 0)
            {
                occupancy = occupied_pixels / (float)total_pixels;
                //occupancy *= 2;
                if (occupancy > 1) occupancy = 1;
                average_intensity /= total_pixels;
                diff_from_global_threshold /= total_pixels;
            }

            return (occupancy);
        }


        /// <summary>
        /// returns a polygon describing the perimeter of a grid cell
        /// at the given coordinate
        /// </summary>
        /// <param name="grid_x">x grid coordinate</param>
        /// <param name="grid_y">y grid coordinate</param>
        /// <param name="horizontal_maxima">horizontal grid line positions</param>
        /// <param name="vertical_maxima">vertical grid line positions</param>
        /// <param name="dominant_orientation">oprientation of the grid pattern</param>
        /// <param name="shear angle_radians">deviation from perfectly perpendicular axes</param>
        /// <returns>polygon object for the grid cell</returns>
        private polygon2D getGridCellPerimeter(int grid_x, int grid_y,
                                               ArrayList horizontal_maxima,
                                               ArrayList vertical_maxima,
                                               float dominant_orientation,
                                               float secondary_orientation,
                                               float shear_angle_radians)
        {
            polygon2D perimeter = new polygon2D();

            float r0 = (float)horizontal_maxima[grid_y];
            float x0 = centre_x + (float)(r0 * Math.Sin(dominant_orientation));
            float y0 = centre_y + (float)(r0 * Math.Cos(dominant_orientation));

            float r1 = (float)horizontal_maxima[grid_y + 1];
            float x1 = centre_x + (float)(r1 * Math.Sin(dominant_orientation));
            float y1 = centre_y + (float)(r1 * Math.Cos(dominant_orientation));

            float r2 = (float)vertical_maxima[grid_x];
            //float secondary_orientation = dominant_orientation + 
            //                              shear_angle_radians + 
            //                              (float)(Math.PI / 2);
            float x2 = (float)(r2 * Math.Sin(secondary_orientation));
            float y2 = (float)(r2 * Math.Cos(secondary_orientation));

            float r3 = (float)vertical_maxima[grid_x + 1];
            float x3 = (float)(r3 * Math.Sin(secondary_orientation));
            float y3 = (float)(r3 * Math.Cos(secondary_orientation));

            perimeter.Add(x0 + x2, y0 + y2);
            perimeter.Add(x0 + x3, y0 + y3);
            perimeter.Add(x1 + x3, y1 + y3);
            perimeter.Add(x1 + x2, y1 + y2);

            return (perimeter);
        }

        /// <summary>
        /// alternate the points in a set of grid line maxima
        /// </summary>
        /// <param name="maxima">list of points to be alternated</param>
        /// <returns>the alternated list</returns>
        private ArrayList alternateMaxima(ArrayList maxima)
        {
            ArrayList alternate = new ArrayList();

            float prev_dist = 0;
            for (int i = 0; i < maxima.Count; i++)
            {
                float dist = (float)maxima[i];
                if (i > 0)
                {
                    float width = dist - prev_dist;
                    float intermediate_position = prev_dist + ((dist - prev_dist) / 2);

                    if (i == 1)
                        alternate.Add(intermediate_position - width);

                    alternate.Add(intermediate_position);

                    if (i == maxima.Count - 1)
                        alternate.Add(intermediate_position + width);

                }
                prev_dist = dist;
            }

            return (alternate);
        }

        /// <summary>
        /// finds local maxima within a grid spacing graph
        /// </summary>
        /// <param name="grid_spacing">a graph of grid responses</param>
        /// <param name="pixels_per_index">number of pixels for each graph entry</param>
        /// <param name="threshold">minimum threshold which the grid response must exceed in order to qualify as a maxima, in the range 0.0-1.0</param>
        /// <param name="fill_missing_spacings">fills in spacing values which are probably missing</param>
        /// <param name="buffer_cells">padding around the perimeter of the grid (quiet zone)</param>
        /// <param name="equalise_spacings">perform additional regularisation of grid spacings</param>
        /// <param name="supression_radius_factor">a scaling factor used to adjust the local suppression radius, typically in the range 1.0-3.0</param>
        /// <param name="equalisation_steps">the number of steps to use when regularising the grid spacing</param>
        private ArrayList fitGridMaxima(float[] grid_spacing,
                                        float pixels_per_index,
                                        float threshold,
                                        bool fill_missing_spacings,
                                        int buffer_cells,
                                        bool equalise_spacings,
                                        float supression_radius_factor,
                                        int equalisation_steps)
        {
            // make a list to store the maxima points
            ArrayList maxima = new ArrayList();

            // make a copy of the grid spacing graph
            float[] grid_spacing_maxima = (float[])grid_spacing.Clone();

            // perform non-maximal supression using the spot radius
            int supression_radius =
                (int)Math.Round(spot_radius * supression_radius_factor / pixels_per_index);

            for (int i = 0; i < grid_spacing_maxima.Length - 1; i++)
            {
                if (grid_spacing_maxima[i] > 0)
                {
                    int max_index = i + supression_radius;
                    if (max_index >= grid_spacing_maxima.Length)
                        max_index = grid_spacing_maxima.Length - 1;

                    for (int j = i + 1; j <= max_index; j++)
                    {
                        if (grid_spacing_maxima[i] >= grid_spacing_maxima[j])
                        {
                            grid_spacing_maxima[j] = 0;
                        }
                        else
                        {
                            grid_spacing_maxima[i] = 0;
                            j = max_index;
                        }
                    }
                }
            }

            // add the survivors to the list            
            for (int i = 0; i < grid_spacing_maxima.Length - 1; i++)
                if (grid_spacing_maxima[i] >= threshold)
                {
                    // average over three indexes of the spacing graph
                    // to get a more accurate localisation
                    float av_position = 0;
                    float tot = 0;
                    for (int j = i - 1; j <= i + 1; j++)
                    {
                        if ((j > -1) && (j < grid_spacing.Length))
                        {
                            av_position += (grid_spacing[j] * j);
                            tot += grid_spacing[j];
                        }
                    }
                    if (tot > 0)
                    {
                        av_position /= tot;
                        // position relative to the centre of the region
                        float position = (av_position - (grid_spacing_maxima.Length / 2.0f)) * pixels_per_index;
                        maxima.Add(position);
                    }
                }

            // complete missing data
            if (fill_missing_spacings)
                maxima = fillGrid(maxima, buffer_cells);

            // equalise the grid spacings
            if (equalise_spacings)
            {
                for (int i = 0; i < equalisation_steps; i++)
                    maxima = equaliseGrid(maxima);
                maxima = idealGrid(maxima);
            }

            return (maxima);
        }

        /// <summary>
        /// fits a grid to a region containing square or checkerboard pattern
        /// </summary>    
        /// <param name="horizontal_lines">horizontal line features detected</param>
        /// <param name="vertical_lines">vertical line features detected</param>
        /// <param name="grid_fitting_pixels_per_index">number of pixels to be represented by each index of the spacings frequency array</param>
        /// <param name="dominant_orientation">the main orientation of the region</param>
        /// <param name="secondary_orientation">orientation of the axis perpendicular (or nearly so) to the main region orientation</param>
        /// <param name="grid_spacing_horizontal">graph showing horizontal grid spacing responses</param>
        /// <param name="grid_spacing_vertical">graph showing vertical grid spacing responses</param>
        /// <param name="horizontal_maxima">horizontal grid maxima distances from the centre of the region</param>
        /// <param name="vertical_maxima">vertical grid maxima distances from the centre of the region</param>
        /// <param name="shear_angle_radians">shear angle in radians</param>
        /// <param name="shear_angle_point">points used to display the shear angle</param>
        /// <param name="grid_padding">padding cells around the perimeter of the grid</param>
        /// <param name="suppression_radius_factor">scaling factor used for non maximal suppression when finding grid spacings, typically in the range 1.0-3.0</param>
        /// <param name="edge_tracing_search_depth">when tracing along edges to build line features this defines the depth of search to use</param>
        /// <param name="edge_tracing_threshold">a threshold applied to the spot map to produce edge features</param>
        public polygon2D fitGrid(ref ArrayList horizontal_lines,
                                 ref ArrayList vertical_lines,
                                 float grid_fitting_pixels_per_index,
                                 ref float dominant_orientation,
                                 ref float secondary_orientation,
                                 ref float[] grid_spacing_horizontal,
                                 ref float[] grid_spacing_vertical,
                                 ref ArrayList horizontal_maxima,
                                 ref ArrayList vertical_maxima,
                                 ref float shear_angle_radians,
                                 ref float[,] shear_angle_point,
                                 int grid_padding,
                                 float suppression_radius_factor,
                                 int edge_tracing_search_depth,
                                 float edge_tracing_threshold)
        {
            shear_angle_radians = 0;
            polygon2D grid = new polygon2D();

            // a threshold applied to the spot map
            float edge_threshold = edge_tracing_threshold;

            // pixels per index defines the number of pixels which will be
            // represented by every index of the grid spacing array
            // This value should be proportional to the estimated spot radius
            // as previously derrived from a frequency analysis of the binary image
            //int pixels_per_index = (int)(spot_radius/2);
            //if (pixels_per_index < 1) pixels_per_index = 1;

            // pixels per index defines the number of pixels which will be
            // represented by every index of the grid spacing array
            // This value should be proportional to the estimated spot radius
            // as previously derrived from a frequency analysis of the binary image
            float pixels_per_index = (spot_radius * grid_fitting_pixels_per_index);
            if (pixels_per_index < 0.1f) pixels_per_index = 0.1f;

            // whether to use the spot map or the binary image to find edges
            // if the spot radius is too small then the spot map just looks like
            // a blur and grid spacings are hard to distinguish clearly
            float square_pattern_min_spot_radius = 3.0f;
            float spot_radius_percent = spot_radius * 100 / width;
            bool use_edges_from_binary_image = false;
            if (spot_radius < square_pattern_min_spot_radius)
                use_edges_from_binary_image = true;
            //Console.WriteLine("Spot radius = " + spot_radius_percent.ToString());

            // when tracing along edges to build line features this
            // defines the depth of search to use
            // if the spot radius is only very small (a couple of pixels)
            // we don't want to search too far, otherwise lines are inappropriately joined
            if (use_edges_from_binary_image)
                edge_tracing_search_depth = 1;  // looking for smaller features

            // detect vertical and horizontal edge features
            // this is done by applying a threshold to the spot map            

            // detect vertical lines by tracing along edges
            if (vertical_lines == null)
            {
                ArrayList[] vertical_edges = null;
                if (use_edges_from_binary_image)
                    vertical_edges = sluggish.utilities.image.detectVerticalEdges(binary_image);
                else
                    vertical_edges = sluggish.utilities.image.detectVerticalEdges(spot_map, edge_threshold);

                vertical_lines = sluggish.utilities.image.traceVerticalLines(vertical_edges, width, 2, edge_tracing_search_depth);
                vertical_lines = sluggish.utilities.image.cropLines(vertical_lines, polygon);
            }

            // detect horizontal lines by tracing along edges
            if (horizontal_lines == null)
            {
                ArrayList[] horizontal_edges = null;
                if (use_edges_from_binary_image)
                    horizontal_edges = sluggish.utilities.image.detectHorizontalEdges(binary_image);
                else
                    horizontal_edges = sluggish.utilities.image.detectHorizontalEdges(spot_map, edge_threshold);

                horizontal_lines = sluggish.utilities.image.traceHorizontalLines(horizontal_edges, width, 2, edge_tracing_search_depth);
                horizontal_lines = sluggish.utilities.image.cropLines(horizontal_lines, polygon);
            }

            // find the dominant orientation
            // best vertical orientation
            float score_vertical = 0;
            float orientation_vertical = sluggish.utilities.image.getDominantOrientation(vertical_lines, 1, ref score_vertical);

            // best horizontal orientation
            float score_horizontal = 0;
            float orientation_horizontal = sluggish.utilities.image.getDominantOrientation(horizontal_lines, 2, ref score_horizontal);

            // choose the orientation with the strongest response
            dominant_orientation = orientation_vertical;
            secondary_orientation = orientation_horizontal;
            if (score_horizontal > score_vertical)
            {
                dominant_orientation = orientation_horizontal - (float)(Math.PI / 2);
                secondary_orientation = orientation_horizontal;

                //dominant_orientation = orientation_horizontal;
                //secondary_orientation = orientation_vertical;
            }


            // keep the orientation within the range -PI/2 - PI/2
            // so that it's always pointing "up"
            if (dominant_orientation > Math.PI / 2)
                dominant_orientation -= (float)Math.PI;
            if (dominant_orientation < -Math.PI / 2)
                dominant_orientation += (float)Math.PI;

            // calculate shear angle
            shear_angle_radians = orientation_vertical - orientation_horizontal;
            if (shear_angle_radians > 0)
                shear_angle_radians -= (float)(Math.PI / 2);
            else
                shear_angle_radians += (float)(Math.PI / 2);

            // create an array to store interceptions with the dominant axis
            grid_spacing_horizontal = new float[(int)Math.Round(width * 2 / pixels_per_index)];
            grid_spacing_vertical = new float[(int)Math.Round(width * 2 / pixels_per_index)];

            // find interception points between lines and the dominant axis
            int x0, y0, x1, y1;
            float dxx, dyy;            // vector in the dominant orientation
            float dxx2, dyy2;          // vector perpendicular to the dominant orientation
            float line_length = 1000;  // some arbitrary length - really all that we're interested in is the orientation
            float[] grid_spacing = null;
            ArrayList lines = null;
            for (int axis = 0; axis < 2; axis++)
            {
                if (axis == 0)
                {
                    lines = horizontal_lines;
                    grid_spacing = grid_spacing_horizontal;
                    // vector in the dominant orientation
                    dxx = line_length * (float)Math.Sin(dominant_orientation);
                    dyy = line_length * (float)Math.Cos(dominant_orientation);
                    // vector perpendicular to the dominant orientation
                    dxx2 = line_length * (float)Math.Sin(dominant_orientation + (Math.PI / 2));
                    dyy2 = line_length * (float)Math.Cos(dominant_orientation + (Math.PI / 2));
                }
                else
                {
                    lines = vertical_lines;
                    grid_spacing = grid_spacing_vertical;
                    // vector in the dominant orientation
                    dxx = line_length * (float)Math.Sin(dominant_orientation + (Math.PI / 2));
                    dyy = line_length * (float)Math.Cos(dominant_orientation + (Math.PI / 2));
                    // vector perpendicular to the dominant orientation
                    dxx2 = line_length * (float)Math.Sin(dominant_orientation);
                    dyy2 = line_length * (float)Math.Cos(dominant_orientation);
                }

                // coordinates for a line along the axis                
                x0 = (int)(centre_x + dxx);
                y0 = (int)(centre_y + dyy);
                x1 = (int)(centre_x - dxx);
                y1 = (int)(centre_y - dyy);

                float histogram_max = 0;
                for (int i = 0; i < lines.Count; i++)
                {
                    linefeature line = (linefeature)lines[i];

                    for (int j = 0; j < 5; j++)
                    {
                        // use the start and end points of the line
                        float px = line.x0;
                        float py = line.y0;
                        switch (j)
                        {
                            case 1:
                                {
                                    px = line.x1;
                                    py = line.y1;
                                    break;
                                }
                            case 2:
                                {
                                    px = line.x0 + ((line.x1 - line.x0) / 2);
                                    py = line.y0 + ((line.y1 - line.y0) / 2);
                                    break;
                                }
                            case 3:
                                {
                                    px = line.x0 + ((line.x1 - line.x0) / 4);
                                    py = line.y0 + ((line.y1 - line.y0) / 4);
                                    break;
                                }
                            case 4:
                                {
                                    px = line.x0 + ((line.x1 - line.x0) * 3 / 4);
                                    py = line.y0 + ((line.y1 - line.y0) * 3 / 4);
                                    break;
                                }
                        }

                        // locate intersection
                        float ix = 0, iy = 0; // intersection coordinate
                        sluggish.utilities.geometry.intersection(x0, y0, x1, y1,
                                                                 px, py,
                                                                 px + dxx2, py + dyy2,
                                                                 ref ix, ref iy);

                        if (ix != 9999)
                        {
                            // measure the distance of the intersection point from
                            // the centre of the region
                            float dx = ix - centre_x;
                            float dy = iy - centre_y;
                            float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                            if (dist < width)
                            {
                                if (((axis == 0) && (dy < 0)) ||
                                    ((axis == 1) && (dx < 0)))
                                    dist = -dist;

                                int index = (int)Math.Round((dist / (float)pixels_per_index) + (grid_spacing.Length / 2.0f));
                                if (index >= grid_spacing.Length)
                                    index = grid_spacing.Length - 1;

                                grid_spacing[index]++;

                                if (grid_spacing[index] > histogram_max)
                                    histogram_max = grid_spacing[index];
                            }
                        }
                    }
                }

                if (histogram_max > 0)
                {
                    for (int j = 0; j < grid_spacing.Length; j++)
                        grid_spacing[j] /= histogram_max;
                }
            }

            // locate maxima within the horizontal and vertical graphs            
            float min_grid_threshold = 0.2f;
            int equalisation_steps = 3;
            equaliseGridAxes(ref horizontal_maxima, ref vertical_maxima,
                             grid_spacing_horizontal,
                             grid_spacing_vertical,
                             pixels_per_index, grid_padding,
                             suppression_radius_factor,
                             equalisation_steps,
                             min_grid_threshold);

            return (grid);
        }

        /// <summary>
        /// equalise the horizontal and vertical grid spacings
        /// </summary>
        /// <param name="horizontal_maxima">horizontal grid maxima distances from the centre of the region</param>
        /// <param name="vertical_maxima">vertical grid maxima distances from the centre of the region</param>
        /// <param name="grid_spacing_horizontal">graph showing horizontal grid spacing responses</param>
        /// <param name="grid_spacing_vertical">graph showing vertical grid spacing responses</param>
        /// <param name="pixels_per_index">number of pixels for each graph entry</param>
        /// <param name="grid_padding">padding cells around the perimeter of the grid</param>
        /// <param name="suppression_radius_factor">scaling factor used for non maximal suppression when finding grid spacings, typically in the range 1.0-3.0</param>
        /// <param name="equalisation_steps">number of itterations to use during equalisation</param>
        /// <param name="min_grid_threshold"></param>
        private void equaliseGridAxes(ref ArrayList horizontal_maxima,
                                      ref ArrayList vertical_maxima,
                                      float[] grid_spacing_horizontal,
                                      float[] grid_spacing_vertical,
                                      float pixels_per_index,
                                      int grid_padding,
                                      float suppression_radius_factor,
                                      int equalisation_steps,
                                      float min_grid_threshold)
        {
            horizontal_maxima = fitGridMaxima(grid_spacing_horizontal, pixels_per_index, min_grid_threshold, true, grid_padding, true, suppression_radius_factor, equalisation_steps);
            vertical_maxima = fitGridMaxima(grid_spacing_vertical, pixels_per_index, min_grid_threshold, true, grid_padding, true, suppression_radius_factor, equalisation_steps);
        }


        #endregion

        #region "localisation of corners against the binary image"

        /// <summary>
        /// returns a polygon shape based upon the corner points located
        /// </summary>
        /// <returns></returns>
        public polygon2D GetPolygon()
        {
            polygon2D poly = new polygon2D();

            for (int i = 0; i < corners.Count; i += 2)
            {
                float x = tx + (float)corners[i];
                float y = ty + (float)corners[i + 1];
                poly.Add(x, y);
            }
            return (poly);
        }

        /// <summary>
        /// returns the nearest high (foreground) level within the binary image
        /// </summary>
        /// <param name="x">starting x position</param>
        /// <param name="y">starting y position</param>
        /// <param name="radius">radius within which to search as a percentage of the</param>
        /// <param name="black_on_white">whether this image contains dark features on a lighter background</param>
        /// <param name="nearest_x">x coordinate of the nearest high pixel</param>
        /// <param name="nearest_y">y coordinate of the nearest high pixel</param>
        private void nearestHigh(int x, int y, int radius, bool black_on_white,
                                 ref int nearest_x, ref int nearest_y)
        {
            float cx = 0, cy = 0;
            polygon.getCentreOfGravity(ref cx, ref cy);

            bool isHigh;
            float nearest_dist = 9999;

            int radius_pixels = width * radius / 1000;

            int dx = x - (int)cx;
            int dy = y - (int)cy;
            float dist_from_centre = (float)Math.Sqrt((dx * dx) + (dy * dy));

            nearest_x = -1;
            nearest_y = -1;
            int xx = x - radius_pixels;
            while (xx <= x + radius_pixels)
            {
                if ((xx > -1) && (xx < width))
                {
                    int yy = y - radius_pixels;
                    while (yy <= y + radius_pixels)
                    {
                        if ((yy > -1) && (yy < height))
                        {
                            if (black_on_white)
                                isHigh = !binary_image[xx, yy];
                            else
                                isHigh = binary_image[xx, yy];

                            if (isHigh)
                            {
                                dx = xx - x;
                                dy = yy - y;
                                float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                                if (dist < nearest_dist)
                                {
                                    // calculate distance from the centre
                                    dx = xx - (int)cx;
                                    dy = yy - (int)cy;
                                    float dist_centre = (float)Math.Sqrt((dx * dx) + (dy * dy));
                                    if (dist_centre <= dist_from_centre)
                                    {
                                        // how many neighbouring pixels are also high?
                                        int no_of_neighbours = 0;
                                        int xx2 = xx + 1;
                                        if (xx2 < width)
                                            if (((black_on_white) && (!binary_image[xx2, yy])) ||
                                                ((!black_on_white) && (binary_image[xx2, yy])))
                                                no_of_neighbours++;
                                        xx2 = xx - 1;
                                        if (xx2 > -1)
                                            if (((black_on_white) && (!binary_image[xx2, yy])) ||
                                                ((!black_on_white) && (binary_image[xx2, yy])))
                                                no_of_neighbours++;
                                        int yy2 = yy + 1;
                                        if (yy2 < height)
                                        {
                                            if (((black_on_white) && (!binary_image[xx, yy2])) ||
                                                ((!black_on_white) && (binary_image[xx, yy2])))
                                                no_of_neighbours++;

                                            xx2 = xx - 1;
                                            if (xx2 > -1)
                                                if (((black_on_white) && (!binary_image[xx2, yy2])) ||
                                                    ((!black_on_white) && (binary_image[xx2, yy2])))
                                                    no_of_neighbours++;

                                            xx2 = xx + 1;
                                            if (xx2 < width)
                                                if (((black_on_white) && (!binary_image[xx2, yy2])) ||
                                                    ((!black_on_white) && (binary_image[xx2, yy2])))
                                                    no_of_neighbours++;

                                        }
                                        yy2 = yy - 1;
                                        if (yy2 > -1)
                                        {
                                            if (((black_on_white) && (!binary_image[xx, yy2])) ||
                                                ((!black_on_white) && (binary_image[xx, yy2])))
                                                no_of_neighbours++;

                                            xx2 = xx - 1;
                                            if (xx2 > -1)
                                                if (((black_on_white) && (!binary_image[xx2, yy2])) ||
                                                    ((!black_on_white) && (binary_image[xx2, yy2])))
                                                    no_of_neighbours++;

                                            xx2 = xx + 1;
                                            if (xx2 < width)
                                                if (((black_on_white) && (!binary_image[xx2, yy2])) ||
                                                    ((!black_on_white) && (binary_image[xx2, yy2])))
                                                    no_of_neighbours++;

                                        }

                                        // if this isn't just a lone pixel
                                        if (no_of_neighbours > 3)
                                        {
                                            nearest_dist = dist;
                                            nearest_x = xx;
                                            nearest_y = yy;
                                        }
                                    }
                                }
                            }
                        }
                        yy++;
                    }
                }
                xx++;
            }
        }


        /// <summary>
        /// position the vertices using the binary image
        /// </summary>
        /// <param name="radius_percent">search radius for the CG algorithm</param>
        /// <param name="black_on_white">whether this region contains dark features on a lighter background</param>
        /// <returns>a new polygon shape</returns>        
        public polygon2D localiseCornersFromBinaryImage(int radius_percent,
                                                        bool black_on_white,
                                                        float inflation)
        {
            // find the centre of the shape
            float cx = 0, cy = 0;
            polygon.getCentreOfGravity(ref cx, ref cy);

            for (int i = 0; i < corners.Count; i += 2)
            {
                // get x,y coordinate just outside the perimeter
                // of the shape
                float x = (float)corners[i];
                float y = (float)corners[i + 1];
                float dx = x - (int)cx;
                float dy = y - (int)cy;
                x = (float)(cx + (dx * inflation));
                y = (float)(cy + (dy * inflation));

                // what is the nearest high value within the binary image ?
                int nearest_x = 0, nearest_y = 0;
                nearestHigh((int)x, (int)y, radius_percent, black_on_white,
                            ref nearest_x, ref nearest_y);

                if (nearest_x > -1)
                {
                    // set the corner to the nearest high position
                    corners[i] = (float)nearest_x;
                    corners[i + 1] = (float)nearest_y;
                }
            }

            // return the resulting shape
            return (createPolygon());
        }


        /// <summary>
        /// set the position of corners from the given polygon
        /// </summary>
        /// <param name="p"></param>
        public void SetCorners(polygon2D p)
        {
            corners = new ArrayList();
            for (int i = 0; i < p.x_points.Count; i++)
            {
                corners.Add((float)(p.x_points[i]));
                corners.Add((float)(p.y_points[i]));
            }
        }

        /// <summary>
        /// returns the local centre of gravity position
        /// </summary>
        /// <param name="x">starting x position</param>
        /// <param name="y">starting y position</param>
        /// <param name="radius">search radius</param>
        /// <param name="black_on_white"></param>
        /// <param name="centre_x"></param>
        /// <param name="centre_y"></param>
        /*
        private void localCentreOfGravity(int x, int y, int radius, bool black_on_white,
                                         ref float centre_x, ref float centre_y)
        {
            int value;

            centre_x = 0;
            centre_y = 0;
            int tot = 0;
            for (int xx = x - radius; xx <= x + radius; xx++)
            {
                if ((xx > -1) && (xx < width))
                {
                    for (int yy = y - radius; yy <= y + radius; yy++)
                    {
                        if ((yy > -1) && (yy < height))
                        {
                            if (!black_on_white)
                                value = 255 - mono_image[(yy * width) + xx];
                            else
                                value = mono_image[(yy * width) + xx];

                            value *= value;

                            centre_x += (xx * value);
                            centre_y += (yy * value);
                            tot += value;
                        }
                    }
                }
            }
            if (tot > 0)
            {
                centre_x = (centre_x / (float)tot);
                centre_y = (centre_y / (float)tot);
            }
        }
        */

        #endregion

        #region "spot detection"

        public ArrayList spots;    // list of spots
        public float spot_radius;  // radius obtained from frequency analysis
        private float[,] spot_map; // map containing spot response magnitudes
        private float[] spatial_frequency_histogram; // histogram containing lengths of connected areas of the image
        public float spatial_frequency_ratio;        // ratio between pre-peak and post peak values in the spatial frequency histogram

        public float shear_angle;  // difference from perfectly perpendicular axes
        public float[,] shear_angle_point;  // three points used to show the shear angle

        /// <summary>
        /// samples the binary image just outside the radius of each spot
        /// and returns a value indicative of whether this is a spot or a square shape
        /// </summary>
        /// <returns>value in the range 0.0-1.0 indicating the likelihood that this image contains spots</returns>
        public float getSpottiness()
        {
            // the total number of samples taken
            int total_samples = 0;

            // number of samplings of the binary image per spot
            const int samples_per_spot = 8;

            // make the spot radius a little larger, so that the sample is taken outside it
            const float inflation = 1.05f;

            // how far shall we rotate for each sample?
            float angle_increment = ((float)Math.PI * 2) / samples_per_spot;

            // spotty dotty
            float spotiness = 0;

            // image dimensions
            int width = binary_image.GetLength(0);
            int height = binary_image.GetLength(1);

            if ((spots != null) && (binary_image != null))
            {
                for (int i = 0; i < spots.Count; i++)
                {
                    blob spot = (blob)spots[i];
                    float radius = spot.average_radius * inflation;
                    bool spot_state = binary_image[(int)spot.x, (int)spot.y];

                    float angle = 0;
                    for (int sample = 0; sample < samples_per_spot; sample++)
                    {
                        int sample_x = (int)(spot.interpolated_x + (radius * (float)Math.Sin(angle)));
                        int sample_y = (int)(spot.interpolated_y + (radius * (float)Math.Cos(angle)));

                        if ((sample_x > -1) && (sample_x < width))
                        {
                            if ((sample_y > -1) && (sample_y < height))
                            {
                                bool periphery_state = binary_image[sample_x, sample_y];
                                if (periphery_state != spot_state) spotiness++;
                                total_samples++;
                            }
                        }

                        angle += angle_increment;
                    }
                }
            }

            // normalise into the range 0.0-1.0
            if (total_samples > 0) spotiness /= total_samples;

            return (spotiness);
        }


        /// <summary>
        /// get the total pixel value for the given area
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public int binaryGetIntegral(int[,] Integral, int tx, int ty, int bx, int by)
        {
            return (Integral[bx, by] + Integral[tx, ty] - (Integral[tx, by] + Integral[bx, ty]));
        }

        /// <summary>
        /// create an integral image for the binary image
        /// this is used when calculating the spot map
        /// </summary>
        public int[,] binaryIntegralImage(bool black_on_white)
        {
            int x, y, p, n = 0;

            int[,] Integral = new int[width, height];

            for (y = 1; y < height; y++)
            {
                p = 0;
                for (x = 0; x < width; x++)
                {
                    int value = 0;
                    if (((black_on_white) && (!binary_image[x, y])) ||
                        ((!black_on_white) && (binary_image[x, y])))
                        value = 1;

                    p += value;
                    Integral[x, y] = p + Integral[x, y - 1];
                    n++;
                }
            }
            return (Integral);
        }

        /// <summary>
        /// applies a perimeter inside which spots should be contained
        /// any spots outside of the perimeter are removed
        /// </summary>
        /// <param name="perimeter">polygon shape of the perimeter</param>
        public void applySpotPerimeter(polygon2D perimeter)
        {
            if (spots != null)
            {
                for (int i = spots.Count - 1; i >= 0; i--)
                {
                    blob spot = (blob)spots[i];
                    if (!perimeter.isInside((int)spot.x, (int)spot.y))
                        spots.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// returns the average separation between spots
        /// This is used when detecting the maximum aligned spots, to search
        /// for chains which are packed closely together
        /// </summary>
        /// <param name="aligned_spots">a list of aligned spots</param>
        /// <returns>average separation between the spots</returns>
        /*
        private float averageSpotSeparation(ArrayList aligned_spots)
        {
            float average_separation = 0;

            for (int i = 0; i < aligned_spots.Count; i++)
            {
                float x1 = ((blob)aligned_spots[i]).interpolated_x;
                float y1 = ((blob)aligned_spots[i]).interpolated_y;
                float min_dist = 9999;
                for (int j = i+1; j < aligned_spots.Count; j++)
                {
                    float x2 = ((blob)aligned_spots[j]).interpolated_x;
                    float y2 = ((blob)aligned_spots[j]).interpolated_y;
                    float dx = x2 - x1;
                    float dy = y2 - y1;
                    float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (dist < min_dist) min_dist = dist;
                }
                if (min_dist != 9999) average_separation += min_dist;
            }
            average_separation /= (aligned_spots.Count - 1);
            return (average_separation);
        }
         */

        /// <summary>
        /// returns a longest chain of spot features
        /// </summary>
        /// <param name="ignore_selected_spots">ignore spots which have a selected status</param>
        /// <param name="preferred_orientation_radians">preferred direction of neighbourhood links, in the range 0-2PI.  If set to -1 no orientation is preferred</param>
        /// <param name="orientation_tollerance">maximum displacement from the preferred orientation</param>
        /// <param name="max_distance">the maximum perpendicular distance below which the spot is considered to touch the line, in the range, typically in the range 0.0-1.0 as a fraction of the spot radius</param>
        public ArrayList MaxAlignedSpots(bool ignore_selected_spots,
                                         float preferred_orientation_radians,
                                         float orientation_tollerance,
                                         float max_distance)
        {
            ArrayList MaxAligned = null;
            int max = 0;

            if (preferred_orientation_radians > Math.PI)
                preferred_orientation_radians = (float)(Math.PI * 2) - preferred_orientation_radians;

            if (spots != null)
            {
                for (int i = 0; i < spots.Count; i++)
                {
                    blob spot = (blob)spots[i];
                    if ((!ignore_selected_spots) ||
                        ((ignore_selected_spots) && (spot.selected == false)))
                    {
                        for (int j = 0; j < spot.neighbours.Count; j++)
                        {
                            blob neighbour = (blob)spot.neighbours[j];
                            if ((!ignore_selected_spots) ||
                                ((ignore_selected_spots) && (neighbour.selected == false)))
                            {
                                // angle between this spot and the neighbour
                                float neighbour_orientation = (float)spot.angle[j];
                                //if (neighbour_orientation > Math.PI)
                                //  neighbour_orientation = (float)(Math.PI*2) - neighbour_orientation;

                                // difference between this angle and the preferred orientation
                                float orientation_difference = preferred_orientation_radians - neighbour_orientation;
                                //orientation_difference = Math.Abs(orientation_difference);
                                if (orientation_difference > (float)(Math.PI))
                                    orientation_difference = -((float)(Math.PI * 2) - orientation_difference);

                                // is the orientation of the neighbouring spot within
                                // the preferred orientation range?
                                if ((preferred_orientation_radians == -1) ||
                                   (Math.Abs(orientation_difference) < orientation_tollerance))
                                {
                                    // how many spots are there along this orientation?
                                    float average_separation = 0;
                                    ArrayList intersections =
                                        SpotsIntersectWithLine(
                                                    spot.interpolated_x, spot.interpolated_y,
                                                    neighbour.interpolated_x,
                                                    neighbour.interpolated_y,
                                                    max_distance,
                                                    ref average_separation);

                                    // look for the maximum number of aligned spots
                                    if (intersections.Count > max)
                                    {
                                        max = intersections.Count;
                                        MaxAligned = intersections;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (MaxAligned);
        }


        /// <summary>
        /// returns a list of spots which intersect the given line
        /// </summary>
        /// <param name="x0">line first point x coordinate</param>
        /// <param name="y0">line first point y coordinate</param>
        /// <param name="x1">line second point x coordinate</param>
        /// <param name="y1">line second point y coordinate</param>
        /// <param name="max_distance">the maximum perpendicular distance below which the blob is considered to touch the line, in the range, typically in the range 0.0-1.0 as a fraction of the blob radius</param>
        /// <returns>list of spots</returns>
        private ArrayList SpotsIntersectWithLine(float x0, float y0, float x1, float y1,
                                                 float max_distance,
                                                 ref float average_separation)
        {
            ArrayList results = new ArrayList();

            average_separation = 0;
            for (int i = 0; i < spots.Count; i++)
            {
                blob spot = (blob)spots[i];
                float perpendicular_distance = 0;

                // does this spot intersect with the line?
                if (spot.intersectsWithLine(x0, y0, x1, y1,
                                            max_distance,
                                            ref perpendicular_distance))
                {
                    // update the average distance from the line
                    average_separation += perpendicular_distance;

                    // add this spot to the results
                    results.Add(spot);
                }
            }

            // calculate the average distance from the line
            if (spots.Count > 0)
                average_separation /= spots.Count;

            return (results);
        }

        /// <summary>
        /// returns a list of spots arranged in a line between the two given spots
        /// </summary>
        /// <param name="spot1">the first spot</param>
        /// <param name="spot2">the second spot</param>
        /// <param name="max_distance">the maximum perpendicular distance below which the spot is considered to touch the line, in the range, typically in the range 0.0-1.0 as a fraction of the spot radius</param>
        /// <returns></returns>
        private ArrayList SpotsConnected(blob spot1, blob spot2, float max_distance, ref float average_separation)
        {
            return (SpotsIntersectWithLine(spot1.interpolated_x, spot1.interpolated_y, spot2.interpolated_x, spot2.interpolated_y, max_distance, ref average_separation));
        }

        /// <summary>
        /// detect spot radii
        /// </summary>
        /// <param name="max_radius_variance">maximum variance of the radius from average, in the range 0-1</param>
        private void detectSpotRadii(float max_radius_variance)
        {
            if (spots.Count > 0)
            {
                // maximum radius, based upon the earlier frequency analysis
                int max_spot_radius = (int)Math.Round(spot_radius * 2);

                // update spot radii
                float av_radius = 0;
                for (int i = 0; i < spots.Count; i++)
                {
                    blob spot = (blob)spots[i];
                    spot.detectRadius(spot_map, max_spot_radius, mono_image, 0.1f);
                    av_radius += spot.average_radius;
                }
                av_radius /= (float)spots.Count;

                // remove any individuals significantly
                // above or below average radius
                // This is useful for removing occasional spurious detections with small radii
                float max_radius = av_radius * (1 + max_radius_variance);
                float min_radius = av_radius * (1 - max_radius_variance);
                for (int i = spots.Count - 1; i >= 0; i--)
                {
                    blob spot = (blob)spots[i];

                    // out damn spot!
                    if ((spot.average_radius < min_radius) ||
                        (spot.average_radius > max_radius))
                        spots.RemoveAt(i);
                }

            }
        }


        /// <summary>
        /// detect spots within the region
        /// </summary>
        /// <param name="black_on_white">whether this image contains darker features on a lighter background</param>
        /// <param name="spot_detection_threshold">threshold used to remove low magnitude spot responses</param>
        /// <param name="max_radius_variance">maximum allowable variation in spot radius</param>
        /// <param name="minimum_spot_diameter_percent">minimum spot diameter as a percentage of the region height, which allows the system to ignore noise</param>
        public void detectSpots(bool black_on_white,
                                float spot_detection_threshold,
                                float max_radius_variance,
                                float minimum_spot_diameter_percent)
        {
            // create a map of spottyness
            spot_map = new float[width, height];

            // this map will contain the positions of spot centres
            // after non-maximal supression
            float[,] spot_centres_map = new float[width, height];

            float maximal_response = 0;

            // estimate the spot radius if necessary
            if (spot_radius == 0)
                spot_radius = getSpotAverageRadius(black_on_white, minimum_spot_diameter_percent);

            // spot radius as an integer
            //int spot_radius_rounded = (int)Math.Round(spot_radius);
            int spot_radius_rounded = (int)spot_radius;

            // make an integral image from the binary image to speed things up
            int[,] integral_image = binaryIntegralImage(black_on_white);

            // apply the mask to the region of interest
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int txx = x - spot_radius_rounded;
                    int tyy = y - spot_radius_rounded;
                    int bxx = x + spot_radius_rounded;
                    int byy = y + spot_radius_rounded;

                    if (txx < 0) txx = 0;
                    if (tyy < 0) tyy = 0;
                    if (bxx >= width) bxx = width - 1;
                    if (byy >= height) byy = height - 1;

                    // get the magnitude of response at this point
                    int spot_response_magnitude = binaryGetIntegral(integral_image, txx, tyy, bxx, byy);

                    // update the map using squared magnitude
                    int squared_magnitude = spot_response_magnitude * spot_response_magnitude;
                    spot_map[x, y] = squared_magnitude;

                    // record the maximal response
                    if (squared_magnitude > maximal_response)
                        maximal_response = squared_magnitude;
                }
            }

            if (maximal_response > 0)
            {
                // errode the contours of the map (like mountains becoming weathered)
                // this helps to eliminate plateau regions where the 
                // response is locally maximum

                // erode horizontally
                for (int y = 1; y < height; y++)
                {
                    float prev_spot_value = 0;
                    for (int x = 1; x < width; x++)
                    {
                        float spot_value = spot_map[x, y];
                        float new_spot_value = prev_spot_value + ((spot_value - prev_spot_value) / 2);
                        spot_map[x, y] = new_spot_value;
                        prev_spot_value = new_spot_value;
                    }
                }
                // erode vertically
                maximal_response = 0;
                for (int x = 1; x < width; x++)
                {
                    float prev_spot_value = 0;
                    for (int y = 1; y < height; y++)
                    {
                        float spot_value = spot_map[x, y];
                        float new_spot_value = prev_spot_value + ((spot_value - prev_spot_value) / 2);
                        spot_map[x, y] = new_spot_value;
                        prev_spot_value = new_spot_value;

                        // find the maximal response, which will allow
                        // subsequent normalisation
                        if (new_spot_value > maximal_response)
                            maximal_response = new_spot_value;
                    }
                }

                // normalise spot responses, and apply threshold to spot centres map
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        float spot_value = spot_map[x, y] / maximal_response;
                        spot_centres_map[x, y] = spot_value;
                        spot_map[x, y] = spot_value;
                        if (spot_value < spot_detection_threshold)
                            spot_centres_map[x, y] = 0;
                    }

                // perform non-maximal supression
                int supression_radius = (int)(spot_radius * 19 / 10);
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        if (spot_centres_map[x, y] > 0)
                        {
                            bool killed = false;

                            int xx = x - supression_radius;
                            while ((xx <= x + supression_radius) && (!killed))
                            {
                                if ((xx > -1) && (xx < width))
                                {
                                    int yy = y - supression_radius;
                                    while ((yy <= y + supression_radius) && (!killed))
                                    {
                                        if ((yy > -1) && (yy < height))
                                        {
                                            if (!((xx == x) && (yy == y)))
                                            {
                                                if (spot_centres_map[x, y] >= spot_centres_map[xx, yy])
                                                    spot_centres_map[xx, yy] = 0;
                                                else
                                                {
                                                    spot_centres_map[x, y] = 0;
                                                    killed = true;
                                                }
                                            }
                                        }
                                        yy++;
                                    }
                                }
                                xx++;
                            }
                        }
                    }

                // store the spot centres in a list
                spots = new ArrayList();
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        if (spot_centres_map[x, y] > 0)
                        {
                            blob new_spot = new blob(x, y);
                            spots.Add(new_spot);
                        }

                // sub-pixel interpolate spot positions
                for (int i = 0; i < spots.Count; i++)
                {
                    blob spot = (blob)spots[i];
                    float interpolated_x = 0;
                    float interpolated_y = 0;
                    float interpolated_total = 0;
                    for (int x = (int)spot.x - 1; x <= (int)spot.x + 1; x++)
                    {
                        if ((x > -1) && (x < width))
                        {
                            for (int y = (int)spot.y - 1; y <= (int)spot.y + 1; y++)
                            {
                                if ((y > -1) && (y < height))
                                {
                                    float map_value = spot_map[x, y];
                                    map_value *= map_value;
                                    interpolated_total += map_value;
                                    interpolated_x += (map_value * x);
                                    interpolated_y += (map_value * y);
                                }
                            }
                        }
                    }
                    if (interpolated_total > 0)
                    {
                        interpolated_x /= interpolated_total;
                        interpolated_y /= interpolated_total;
                        spot.interpolated_x = interpolated_x;
                        spot.interpolated_y = interpolated_y;
                    }
                }

                // detect radii
                detectSpotRadii(max_radius_variance);
            }
        }

        /// <summary>
        /// fits a line to the greatest number of aligned spots
        /// </summary>
        /// <param name="spots">list of detected spot features</param>
        /// <param name="ignore_selected_spots">whether to ignore spots which have already been selected in previous line fits</param>
        /// <param name="spots_aligned">list of aligned spots</param>
        /// <param name="preferred_orientation">preferred direction of the line, in the range 0-2PI, or if set to -1 direction is ignored</param>
        /// <param name="orientation_tollerance">max deviation from the preferred tollerance</param>
        /// <param name="max_distance">the maximum perpendicular distance below which the spot is considered to touch the line, in the range, typically in the range 0.0-1.0 as a fraction of the spot radius</param>
        /// <param name="line_centre_x">x centre point of the line</param>
        /// <param name="line_centre_y">y centre point of the line</param>
        /// <returns>polynomial line fit</returns>
        private polynomial fitLineToSpots(ArrayList spots,
                                          bool ignore_selected_spots,
                                          ref ArrayList spots_aligned,
                                          float preferred_orientation,
                                          float orientation_tollerance,
                                          float max_distance,
                                          ref float line_centre_x,
                                          ref float line_centre_y)
        {
            polynomial best_fit_line = null;
            spots_aligned = null;
            line_centre_x = 0;
            line_centre_y = 0;

            // find the maximum number of aligned spots
            ArrayList max_spots_aligned =
                MaxAlignedSpots(ignore_selected_spots,
                                preferred_orientation,
                                orientation_tollerance,
                                max_distance);
            if (max_spots_aligned != null)
            {
                spots_aligned = max_spots_aligned;
                if (max_spots_aligned.Count > 0)
                {
                    // get the position of the centre of the line
                    for (int i = 0; i < max_spots_aligned.Count; i++)
                    {
                        blob spot = (blob)max_spots_aligned[i];
                        line_centre_x += spot.interpolated_x;
                        line_centre_y += spot.interpolated_y;
                    }
                    line_centre_x /= max_spots_aligned.Count;
                    line_centre_y /= max_spots_aligned.Count;

                    // fit a line to the points
                    best_fit_line = new polynomial();
                    best_fit_line.SetDegree(1);
                    for (int i = 0; i < max_spots_aligned.Count; i++)
                    {
                        blob spot = (blob)max_spots_aligned[i];
                        float dx = spot.interpolated_x - line_centre_x;
                        float dy = spot.interpolated_y - line_centre_y;
                        best_fit_line.AddPoint(dx, dy);
                        spot.selected = true;
                    }
                    // solve the line equation
                    best_fit_line.Solve();
                }
            }
            return (best_fit_line);
        }

        /// <summary>
        /// returns the shear angle between two lines
        /// </summary>    
        /// <param name="line1_x0">first line x0</param>
        /// <param name="line1_y0">first line y0</param>
        /// <param name="line1_x1">first line x1</param>
        /// <param name="line1_y1">first line y1</param>
        /// <param name="line2_x0">second line x0</param>
        /// <param name="line2_y0">second line y0</param>
        /// <param name="line2_x1">second line x1</param>
        /// <param name="line2_y1">second line y1</param>
        /// <param name="shear_angle_point">three points used to display the shear angle</param>
        /// <returns>shear angle in radians, relative to perpendicular</returns>
        private float getShearAngle(float line1_x0, float line1_y0,
                                    float line1_x1, float line1_y1,
                                    float line2_x0, float line2_y0,
                                    float line2_x1, float line2_y1,
                                    ref float[,] shear_angle_point)
        {
            float shear_angle_radians = 0;

            // find the intersection point between the two lines
            float ix = 9999;
            float iy = 9999;
            sluggish.utilities.geometry.intersection(
                line1_x0, line1_y0,
                line1_x1, line1_y1,
                line2_x0, line2_y0,
                line2_x1, line2_y1,
                ref ix, ref iy);

            if (ix != 9999)
            {
                // if the intersection point of the two lines
                // is inside the region
                if ((ix > -width * 25 / 100) && (ix < width * 125 / 100) &&
                    (iy > -height * 25 / 100) && (iy < height * 125 / 100))
                {
                    // create three points which can be used to display the
                    // shear angle
                    float dx0 = line1_x0 - ix;
                    float dy0 = line1_y0 - iy;
                    float hyp0 = (float)Math.Sqrt((dx0 * dx0) + (dy0 * dy0));
                    float scale_factor = 1.0f;
                    if (hyp0 > 0) scale_factor = width / hyp0;
                    dx0 *= scale_factor;
                    dy0 *= scale_factor;
                    float dx1 = line2_x0 - ix;
                    float dy1 = line2_y0 - iy;
                    float hyp1 = (float)Math.Sqrt((dx1 * dx1) + (dy1 * dy1));
                    scale_factor = 1.0f;
                    if (hyp1 > 0) scale_factor = width / hyp1;
                    dx1 *= scale_factor;
                    dy1 *= scale_factor;

                    shear_angle_point = new float[3, 2];
                    shear_angle_point[0, 0] = ix + dx0;
                    shear_angle_point[0, 1] = iy + dy0;
                    shear_angle_point[1, 0] = ix;
                    shear_angle_point[1, 1] = iy;
                    shear_angle_point[2, 0] = ix + dx1;
                    shear_angle_point[2, 1] = iy + dy1;

                    // find the angle subtended by the two lines
                    shear_angle_radians = sluggish.utilities.geometry.threePointAngle(
                        ix + dx0, iy + dy0,
                        ix, iy,
                        ix + dx1, iy + dy1);

                    // convert angle so that it's relative to perfectly perpendicular
                    shear_angle_radians -= (float)(Math.PI / 2);
                }
            }

            return (shear_angle_radians);
        }


        /// <summary>
        /// after finding the horizontal and vertical axis of a region
        /// this removes any spots which are unlikely to lie inside the 
        /// axis of a square or rectangular region
        /// </summary>
        /// <param name="spots">list of spot features</param>
        /// <param name="shear_angle_point">angle defining the primary axis of the region</param>
        /// <param name="spot_culling_threshold">the ratio of possible out of bounds spots to the total number of spots must be below this threshold in order for out of bounds cases to be removed</param>
        private void removeSpots(ArrayList spots,
                                 float[,] shear_angle_point,
                                 float spot_culling_threshold)
        {
            if (shear_angle_point != null)
            {
                polygon2D area_perimeter = new polygon2D();

                float tx = shear_angle_point[0, 0];
                float ty = shear_angle_point[0, 1];
                float cx = shear_angle_point[1, 0];
                float cy = shear_angle_point[1, 1];
                float bx = shear_angle_point[2, 0];
                float by = shear_angle_point[2, 1];

                float dx1 = cx - tx;
                float dy1 = cy - ty;
                float dx2 = cx - bx;
                float dy2 = cy - by;

                float dx = dx1;
                if (Math.Abs(dx2) > Math.Abs(dx1)) dx = dx2;
                float dy = dy1;
                if (Math.Abs(dy2) > Math.Abs(dy1)) dy = dy2;

                // add a small border
                float x_offset = 4;
                float y_offset = 4;
                if (dx < 0) x_offset = -x_offset;
                if (dy < 0) y_offset = -y_offset;

                // create a polygon inside which the spot features are expected to lie
                area_perimeter.Add(tx + x_offset, ty + y_offset);
                area_perimeter.Add(cx + x_offset, cy + y_offset);
                area_perimeter.Add(bx + x_offset, by + y_offset);
                area_perimeter.Add(bx + (tx - cx) + x_offset, by + (ty - cy) + y_offset);

                // remove any spots outside of this perimeter
                ArrayList potential_victims = new ArrayList();
                for (int i = spots.Count - 1; i >= 0; i--)
                {
                    blob spot = (blob)spots[i];
                    if (!area_perimeter.isInside(spot.interpolated_x, spot.interpolated_y))
                    {
                        // add the index of this spot to the list of potential victims <evil laughter>
                        potential_victims.Add(i);
                    }
                }

                if (potential_victims.Count > 0)
                {
                    // what fraction of the spots are potential victims?
                    // if this ratio is too large then perhaps we have made a dreadful mistake!
                    float victims_ratio = potential_victims.Count / (float)spots.Count;

                    if (victims_ratio < spot_culling_threshold)
                    {
                        // let the slaughter commence
                        for (int i = 0; i < potential_victims.Count; i++)
                        {
                            int victim_index = (int)potential_victims[i];
                            spots.RemoveAt(victim_index);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// fits a grid to a region containing spot features
        /// </summary> 
        /// <param name="spots">list of detected spot features</param>
        /// <param name="max_distance">the maximum perpendicular distance below which the spot is considered to touch the line, in the range, typically in the range 0.0-1.0 as a fraction of the spot radius</param>
        /// <param name="connection_radius">radius within which spots are considered to be neighbours, typically in the range 2.0-3.0</param>
        /// <param name="grid_fitting_pixels_per_index">number of pixels to be represented by each index of the spacings frequency array</param>
        /// <param name="dominant_orientation">the main orientation of the region</param>
        /// <param name="secondary_orientation">orientation perpendicular (or nearly so) to the dominant orientation</param>
        /// <param name="grid_spacing_horizontal">graph showing horizontal grid spacing responses</param>
        /// <param name="grid_spacing_vertical">graph showing vertical grid spacing responses</param>
        /// <param name="horizontal_maxima">horizontal grid maxima distances from the centre of the region</param>
        /// <param name="vertical_maxima">vertical grid maxima distances from the centre of the region</param>
        /// <param name="shear_angle_radians">difference from perfectly perpendicular axes</param>
        /// <param name="shear_angle_point">points used to display the shear angle</param>
        /// <param name="grid_padding">padding cells around the perimeter of the grid</param>
        /// <param name="supression_radius_factor">scaling factor used to adjust non-maximal suppression radius when finding grid spacings, typically in the range 1.0-3.0</param>
        /// <param name="orientation_tollerance">maximum deviation from the preferred orientation when finding the main axis</param>
        /// <param name="spot_culling_threshold">threshold used to remove possible out of bounds spots, in the range 0.0 - 1.0</param>
        public polygon2D fitGrid(ArrayList spots, float max_distance,
                                 float connection_radius,
                                 float grid_fitting_pixels_per_index,
                                 ref float dominant_orientation,
                                 ref float secondary_orientation,
                                 ref float[] grid_spacing_horizontal,
                                 ref float[] grid_spacing_vertical,
                                 ref ArrayList horizontal_maxima,
                                 ref ArrayList vertical_maxima,
                                 ref float shear_angle_radians,
                                 ref float[,] shear_angle_point,
                                 int grid_padding,
                                 float supression_radius_factor,
                                 float orientation_tollerance,
                                 float spot_culling_threshold)
        {
            polygon2D grid = new polygon2D();

            // connect neighbouring spots
            if (spots != null)
            {
                for (int i = 0; i < spots.Count - 1; i++)
                {
                    blob spot1 = (blob)spots[i];
                    float neighbour_radius = spot1.average_radius * connection_radius;
                    for (int j = i + 1; j < spots.Count; j++)
                    {
                        blob spot2 = (blob)spots[j];
                        if (spot1.AddNeighbour(spot2, neighbour_radius))
                        {
                            spot2.AddNeighbour(spot1, neighbour_radius);
                        }
                    }
                }
            }

            // find the maximum number of aligned spots
            ArrayList spots_aligned = null;
            float preferred_orientation = -1;
            float line_centre_x0 = 0;
            float line_centre_y0 = 0;
            polynomial best_fit_line = fitLineToSpots(spots, false,
                                                      ref spots_aligned,
                                                      preferred_orientation,
                                                      orientation_tollerance,
                                                      max_distance,
                                                      ref line_centre_x0,
                                                      ref line_centre_y0);
            if (best_fit_line != null)
            {
                // find the orientation of the best fit line
                float sample_x = 100;
                float sample_y = best_fit_line.RegVal(sample_x);
                float hyp = (float)Math.Sqrt((sample_x * sample_x) + (sample_y * sample_y));
                dominant_orientation = (float)Math.Asin(sample_x / hyp);
                if (sample_y < 0) dominant_orientation = (float)(Math.PI * 2) - dominant_orientation;
                float orient = dominant_orientation;

                // if the main orientation discovered is horizontally
                // oriented then rotate it into a vertical orientation
                //bool dominant_orientation_horizontal = false;
                if (sample_x > Math.Abs(sample_y))
                {
                    //dominant_orientation_horizontal = true; 
                    dominant_orientation -= (float)(Math.PI / 2);
                }

                // always point downwards
                if (hyp * Math.Cos(dominant_orientation) < 0)
                    dominant_orientation -= (float)Math.PI;

                orientation = dominant_orientation;

                // orientation perpendicular (or nearly perpendicular) to the
                // dominant orientation
                secondary_orientation = dominant_orientation +
                                        (float)(Math.PI / 2);


                // find the second maximum number of aligned spots
                ArrayList second_spots_aligned = null;
                preferred_orientation = orient + (float)(Math.PI / 2);
                if (preferred_orientation > (float)(Math.PI * 2))
                    preferred_orientation -= (float)(Math.PI * 2);

                float line_centre_x1 = 0;
                float line_centre_y1 = 0;
                polynomial second_best_fit_line = fitLineToSpots(spots, true,
                                                                 ref second_spots_aligned,
                                                                 preferred_orientation,
                                                                 orientation_tollerance,
                                                                 max_distance,
                                                                 ref line_centre_x1,
                                                                 ref line_centre_y1);
                if (second_best_fit_line != null)
                {
                    // find a couple of points on the second best fit line
                    float sample_x2 = 100;
                    float sample_y2 = second_best_fit_line.RegVal(sample_x2);

                    //float hyp2 = (float)Math.Sqrt((sample_x2 * sample_x2) + (sample_y2 * sample_y2));
                    //float orient2 = (float)Math.Asin(sample_x2 / hyp2);
                    //if (sample_y2 < 0) orient2 = (float)(Math.PI * 2) - orient2;


                    // get the shear angle
                    shear_angle_radians = getShearAngle(
                        line_centre_x0, line_centre_y0,
                        line_centre_x0 + sample_x, line_centre_y0 + sample_y,
                        line_centre_x1, line_centre_y1,
                        line_centre_x1 + sample_x2, line_centre_y1 + sample_y2,
                        ref shear_angle_point);

                    // remove spots which are unlikely to be useful
                    removeSpots(spots, shear_angle_point, spot_culling_threshold);
                }

                // pixels per index defines the number of pixels which will be
                // represented by every index of the grid spacing array
                // This value should be proportional to the estimated spot radius
                // as previously derrived from a frequency analysis of the binary image
                float pixels_per_index = (spot_radius * grid_fitting_pixels_per_index);
                if (pixels_per_index < 0.1f) pixels_per_index = 0.1f;

                // create an array to store interceptions with the dominant axis
                grid_spacing_horizontal = new float[(int)(width * 2 / pixels_per_index) + 1];
                grid_spacing_vertical = new float[(int)(width * 2 / pixels_per_index) + 1];

                // find interception points between lines and the dominant axis
                int x0, y0, x1, y1;
                float dxx, dyy;            // vector in the dominant orientation
                float dxx2, dyy2;          // vector perpendicular to the dominant orientation
                float line_length = 1000;  // some arbitrary length - really all that we're interested in is the orientation
                float[] grid_spacing = null;
                for (int axis = 0; axis < 2; axis++)
                {
                    if (axis == 0)
                    {
                        grid_spacing = grid_spacing_horizontal;
                        // vector in the dominant orientation
                        dxx = line_length * (float)Math.Sin(dominant_orientation);
                        dyy = line_length * (float)Math.Cos(dominant_orientation);
                        // vector perpendicular to the dominant orientation
                        dxx2 = line_length * (float)Math.Sin(secondary_orientation);
                        dyy2 = line_length * (float)Math.Cos(secondary_orientation);
                    }
                    else
                    {
                        grid_spacing = grid_spacing_vertical;
                        // vector in the dominant orientation
                        dxx = line_length * (float)Math.Sin(secondary_orientation);
                        dyy = line_length * (float)Math.Cos(secondary_orientation);
                        // vector perpendicular to the dominant orientation
                        dxx2 = line_length * (float)Math.Sin(dominant_orientation);
                        dyy2 = line_length * (float)Math.Cos(dominant_orientation);
                    }

                    // coordinates for a line along the axis                
                    x0 = (int)(centre_x + dxx);
                    y0 = (int)(centre_y + dyy);
                    x1 = (int)(centre_x - dxx);
                    y1 = (int)(centre_y - dyy);

                    float histogram_max = 0;
                    if (spots != null)
                    {
                        for (int i = 0; i < spots.Count; i++)
                        {
                            blob spot = (blob)spots[i];

                            float px = spot.interpolated_x;
                            float py = spot.interpolated_y;

                            // locate intersection
                            float ix = 0, iy = 0; // intersection coordinate
                            sluggish.utilities.geometry.intersection(x0, y0, x1, y1,
                                                                     px, py,
                                                                     px + dxx2, py + dyy2,
                                                                     ref ix, ref iy);

                            if (ix != 9999)
                            {
                                // measure the distance of the intersection point from
                                // the centre of the region
                                float dx = ix - centre_x;
                                float dy = iy - centre_y;
                                float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                                if (dist < width)
                                {
                                    if (((axis == 0) && (dy < 0)) ||
                                        ((axis == 1) && (dx < 0)))
                                        dist = -dist;

                                    int index = (int)Math.Round((dist / pixels_per_index) + (grid_spacing.Length / 2.0f));
                                    if (index >= grid_spacing.Length)
                                        index = grid_spacing.Length - 1;

                                    grid_spacing[index]++;

                                    if (grid_spacing[index] > histogram_max)
                                        histogram_max = grid_spacing[index];
                                }
                            }
                        }
                    }

                    if (histogram_max > 0)
                    {
                        for (int j = 0; j < grid_spacing.Length; j++)
                            grid_spacing[j] /= histogram_max;
                    }
                }

                // locate maxima within the horizontal and vertical graphs            
                float min_grid_threshold = 0.1f;
                int equalisation_steps = 1;
                equaliseGridAxes(ref horizontal_maxima, ref vertical_maxima,
                                 grid_spacing_horizontal,
                                 grid_spacing_vertical,
                                 pixels_per_index, grid_padding,
                                 supression_radius_factor,
                                 equalisation_steps,
                                 min_grid_threshold);


                // alternate the maxima points
                horizontal_maxima = alternateMaxima(horizontal_maxima);
                vertical_maxima = alternateMaxima(vertical_maxima);
            }

            return (grid);
        }


        /// <summary>
        /// frequency analysis of the binary image
        /// returns an estimate of the spot size in a binary image
        /// this can be used with spot based calibration patterns, with the
        /// value returned used to initialise a more restricted search within
        /// a narrow range of radii
        /// </summary>
        /// <param name="black_on_white">whether this region contains dark features on a lighter background</param>
        /// <param name="minimum_diameter_percent">minimum spot diameter as a percentage of the region height, which allows the system to ignore noise</param>
        /// <returns>average spot radius</returns>
        public float getSpotAverageRadius(bool black_on_white,
                                          float minimum_diameter_percent)
        {
            int divisor = 200;
            float radius = 0;

            // diameter histogram
            int[] diameter_hits = new int[width];

            // search vertically
            int max_misses = height / divisor;
            if (max_misses <= 1) max_misses = 0;
            for (int x = 0; x < width; x++)
            {
                int start = -1;
                int misses = 0;
                int diameter = 0;
                for (int y = 0; y < height; y++)
                {
                    // inside the region of interest
                    if (polygon.isInside(x, y))
                    {
                        bool isHigh = false;

                        if (((black_on_white) && (!binary_image[x, y])) ||
                            ((!black_on_white) && (binary_image[x, y])))
                            isHigh = true;

                        if (start == -1)
                        {
                            if (isHigh)
                            {
                                start = y;
                                diameter = 1;
                                misses = 0;
                            }
                        }
                        else
                        {
                            if (isHigh)
                            {
                                diameter = y - start;
                                //misses = 0;
                            }
                            else
                            {
                                misses++;
                                if (misses > max_misses)
                                {
                                    start = -1;

                                    // update the diameter histogram
                                    if (diameter < diameter_hits.Length)
                                        diameter_hits[diameter]++;
                                }
                            }
                        }
                    }
                }
            }

            // search horizontally
            max_misses = width / divisor;
            //if (max_misses < 1) max_misses = 1;
            for (int y = 0; y < height; y++)
            {
                int start = -1;
                int misses = 0;
                int diameter = 0;
                for (int x = 0; x < width; x++)
                {
                    // inside theregion of interest
                    if (polygon.isInside(x, y))
                    {
                        bool isHigh = false;

                        if (((black_on_white) && (!binary_image[x, y])) ||
                            ((!black_on_white) && (binary_image[x, y])))
                            isHigh = true;

                        if (start == -1)
                        {
                            if (isHigh)
                            {
                                start = x;
                                diameter = 1;
                                misses = 0;
                            }
                        }
                        else
                        {
                            if (isHigh)
                            {
                                diameter = x - start;
                                //misses = 0;
                            }
                            else
                            {
                                misses++;
                                if (misses > max_misses)
                                {
                                    start = -1;

                                    // update the diameter histogram
                                    if (diameter < diameter_hits.Length)
                                        diameter_hits[diameter]++;
                                }
                            }
                        }
                    }
                }
            }

            // ignore very tiny diameters, which are probably just noise
            // or spurious background detections
            int min_diameter = (int)(height * minimum_diameter_percent / 100);
            if (min_diameter < 1) min_diameter = 1;

            // look for a peak in the diameter histogram
            int max_hits = 0;
            int winner = -1;
            for (int d = min_diameter; d < diameter_hits.Length - 2; d++)
            {
                int combined_hits = diameter_hits[d] + diameter_hits[d + 1] + diameter_hits[d + 2];
                if (combined_hits > max_hits)
                {
                    max_hits = combined_hits;
                    winner = d;
                }
            }
            if (winner > -1)
            {
                int tot_hits = diameter_hits[winner] +
                               diameter_hits[winner + 1] +
                               diameter_hits[winner + 2];
                float diam = ((winner * diameter_hits[winner]) +
                              ((winner + 1) * diameter_hits[winner + 1]) +
                              ((winner + 2) * diameter_hits[winner + 2])) / (float)tot_hits;
                radius = diam / 2.0f;
            }

            spot_radius = radius;


            //store the histogram for later display using the Show method            
            spatial_frequency_histogram = new float[width];

            max_hits = 0;
            int max_diameter = 0;
            for (int d = min_diameter; d < diameter_hits.Length; d++)
                if (diameter_hits[d] > max_hits)
                {
                    max_hits = diameter_hits[d];
                    max_diameter = d;
                }

            if (max_hits > 0)
                for (int d = min_diameter; d < spatial_frequency_histogram.Length; d++)
                    spatial_frequency_histogram[d] = diameter_hits[d] / (float)max_hits;

            // calculate spatial frequency ratio
            // this is the ratio between the total pre-peak values
            // in the diameter histogram to the total post peak values
            float pre_peak = 0;
            float post_peak = 0;
            for (int d = min_diameter; d < diameter_hits.Length; d++)
            {
                if (d <= max_diameter + 1)
                    pre_peak += diameter_hits[d];
                else
                {
                    post_peak += diameter_hits[d];
                    //if (diameter_hits[d] == 0) d = diameter_hits.Length;
                }
            }
            if (post_peak > 0)
            {
                spatial_frequency_ratio = pre_peak / post_peak;
            }
            else spatial_frequency_ratio = 10.0f;

            return (radius);
        }

        #endregion

        #region "finding corners of square shapes"

        // major and minor radii
        private float major_axis_length = 0;
        private float minor_axis_length = 99999;
        public float aspect_ratio = 1;

        /// <summary>
        /// adjust the orientation or the region
        /// this compensates for the way in which the major axis is detected
        /// as being the longest radius from the centre, which tends to result
        /// in detection of a diagonal across the region
        /// </summary>
        private void adjustOrientation()
        {
            //if (aspect_ratio > 1.3f)
            {
                // radius of the longest axis from the centre point
                float dx = (float)((major_axis_length / 2) * Math.Cos(orientation));
                float dy = (float)((major_axis_length / 2) * Math.Sin(orientation));

                // one end point of the longest axis
                float p1_x = centre_x - dx;
                float p1_y = centre_y - dy;

                dx = (float)((minor_axis_length / 2) * Math.Cos(orientation - (Math.PI / 2)));
                dy = (float)((minor_axis_length / 2) * Math.Sin(orientation - (Math.PI / 2)));

                // two end points of the shortest axis
                float p2_x = centre_x - dx;
                float p2_y = centre_y - dy;
                float p3_x = centre_x + dx;
                float p3_y = centre_y + dy;

                // calculate the distances between the end of the longest axis
                // and the two points for the shortest axis
                float dx_p2 = p2_x - p1_x;
                float dy_p2 = p2_y - p1_y;
                float dist_p1_p2 = (float)Math.Sqrt((dx_p2 * dx_p2) + (dy_p2 * dy_p2));
                float orientation1 = (float)Math.Acos(dx_p2 / dist_p1_p2);
                if (dy_p2 < 0) orientation1 = (float)(Math.PI * 2) - orientation1;

                float dx_p3 = p3_x - p1_x;
                float dy_p3 = p3_y - p1_y;
                float dist_p1_p3 = (float)Math.Sqrt((dx_p3 * dx_p3) + (dy_p3 * dy_p3));
                float orientation2 = (float)Math.Acos(dx_p3 / dist_p1_p3);
                if (dy_p3 < 0) orientation2 = (float)(Math.PI * 2) - orientation2;

                // test these two potential orientations to determine which is the more likely
                int max_hits = 0;
                float current_orientation = orientation;
                for (int test = 1; test <= 3; test++)
                {
                    float orient = orientation1;
                    switch (test)
                    {
                        case 1: { orient = orientation1; break; }
                        case 2: { orient = orientation2; break; }
                        case 3: { orient = current_orientation; break; }
                    }
                    int hits = 0;

                    for (int r = (int)major_axis_length / 4; r < (int)major_axis_length / 2; r++)
                    {
                        for (int side = 0; side < 2; side++)
                        {
                            float orient2 = orient;
                            if (side == 1) orient2 = orient + (float)Math.PI;

                            dx = (r * (float)Math.Cos(orient2));
                            dy = (r * (float)Math.Sin(orient2));
                            int x = (int)(centre_x - dx);
                            int y = (int)(centre_y - dy);
                            if ((x > -1) && (x < width))
                            {
                                if ((y > -1) && (y < height))
                                {
                                    if (shape[x, y] == true)
                                        hits++;
                                }
                            }

                            // check above and below the line
                            float dx2 = (float)((minor_axis_length / 4) * Math.Cos(orient2 + (Math.PI / 2)));
                            float dy2 = (float)((minor_axis_length / 4) * Math.Sin(orient2 + (Math.PI / 2)));
                            int x2 = (int)(x - dx2);
                            int y2 = (int)(y - dy2);
                            if ((x2 > -1) && (x2 < width) && (y2 > -1) && (y2 < height))
                            {
                                if (shape[x2, y2] == true)
                                    hits++;
                            }

                            x2 = (int)(x + dx2);
                            y2 = (int)(y + dy2);
                            if ((x2 > -1) && (x2 < width) && (y2 > -1) && (y2 < height))
                            {
                                if (shape[x2, y2] == true)
                                    hits++;
                            }
                        }
                    }
                    if (hits >= max_hits)
                    {
                        max_hits = hits;
                        orientation = orient;
                    }
                }
            }
        }

        /// <summary>
        /// create corners based upon minor and major axis lengths and the dominant orientation
        /// </summary>
        private void createCornersFromAxes()
        {
            // create a corners list
            corners = new ArrayList();
            angles = new ArrayList();

            // minor and major radius lengths
            float minor_radius = minor_axis_length / 2;
            float major_radius = major_axis_length / 2;

            // length of the diagonal radius for the bounding box
            float diagonal_length = (float)Math.Sqrt((minor_radius * minor_radius) + (major_radius * major_radius));
            diagonal_length *= 1.004f;

            if (diagonal_length > 0)
            {
                float diagonal_angle = (float)Math.Acos(major_radius / diagonal_length);

                // for each of the four diagonals
                for (int diag = 0; diag < 4; diag++)
                {
                    // calculate the diagonal angle
                    float angle = orientation - diagonal_angle + (diag * (float)(Math.PI / 2));

                    // find the end position of the diagonal
                    float diag_x = centre_x + (float)(diagonal_length * Math.Cos(angle));
                    float diag_y = centre_y + (float)(diagonal_length * Math.Sin(angle));

                    corners.Add(diag_x);
                    corners.Add(diag_y);
                    angles.Add((float)Math.PI / 2);

                    // flip the angle
                    diagonal_angle = ((float)Math.PI / 2) - diagonal_angle;
                }
            }
        }

        /// <summary>
        /// for a square or rectangular shape return the two diagonal lengths
        /// </summary>
        /// <param name="length1">length of the first diagonal</param>
        /// <param name="length2">length of the second diagonal</param>
        public void getDiagonalLengths(ref float length1, ref float length2)
        {
            // only applies to 4 sided shapes
            if (corners.Count / 2 == 4)
            {
                // the first diagonal
                int x1 = (int)corners[0];
                int y1 = (int)corners[1];
                int x2 = (int)corners[4];
                int y2 = (int)corners[5];
                float dx = x2 - x1;
                float dy = y2 - y1;
                length1 = (float)Math.Sqrt((dx * dx) + (dy * dy));

                // the second diagonal
                x1 = (int)corners[2];
                y1 = (int)corners[3];
                x2 = (int)corners[6];
                y2 = (int)corners[7];
                dx = x2 - x1;
                dy = y2 - y1;
                length2 = (float)Math.Sqrt((dx * dx) + (dy * dy));
            }
        }

        /// <summary>
        /// returns the angle subtended by the given corner
        /// </summary>
        public float getCornerAngle(int corner_index)
        {
            float ang = (float)angles[corner_index];
            return (ang);
        }

        /// <summary>
        /// fit corners as closely as possible
        /// This works by locating the closest feature to the diagonal end points
        /// </summary>
        /// <param name="corner_features">list of corner features</param>
        /// <param name="inflation">a multiplier used to inflate the radius from the centre to each vertex</param>
        /// <param name="angular_offset_degrees">for each corner two possible angles are tried, with this being the magnitude of the offset</param>
        private void fitCorners(ArrayList corner_features,
                                float inflation,
                                float angular_offset_degrees)
        {
            //float inflation = 1.2f;
            //float angular_offset_degrees = 6;
            float angular_offset = (float)(angular_offset_degrees * Math.PI / 180.0f);

            // create a corners list
            corners = new ArrayList();
            angles = new ArrayList();

            // minor and major radius lengths
            float minor_radius = minor_axis_length / 2;
            float major_radius = major_axis_length / 2;

            // length of the diagonal radius for the bounding box
            float diagonal_length = (float)Math.Sqrt((minor_radius * minor_radius) + (major_radius * major_radius));

            if (diagonal_length > 0)
            {
                float diagonal_angle = (float)Math.Acos(major_radius / diagonal_length);

                // extend the length slightly
                diagonal_length *= inflation;

                // for each of the four diagonals
                for (int diag = 0; diag < 4; diag++)
                {
                    // calculate the diagonal angle
                    float angle = orientation - diagonal_angle + (diag * (float)(Math.PI / 2));

                    int[] winner_x = new int[3];
                    int[] winner_y = new int[3];

                    for (int offset = 0; offset < 3; offset++)
                    {
                        winner_x[offset] = -1;

                        // offset the angle slightly so that diagonal
                        // corners can be detected
                        float offset_angle = 0;
                        switch (offset)
                        {
                            case 0: { offset_angle = angle; break; }
                            case 1: { offset_angle = angle + angular_offset; break; }
                            case 2: { offset_angle = angle - angular_offset; break; }
                        }

                        // find the end position of the diagonal
                        float diag_x = centre_x + (float)(diagonal_length * Math.Cos(offset_angle));
                        float diag_y = centre_y + (float)(diagonal_length * Math.Sin(offset_angle));

                        // find the closest corner feature to this end position
                        bool corner_found = false;
                        int i = 0;
                        float min_dist = 99999;
                        while ((i < corner_features.Count) &&
                               (corner_found == false))
                        {
                            int corner_x = (int)corner_features[i] - tx;
                            if ((corner_x > -1) && (corner_x < width))
                            {
                                int corner_y = (int)corner_features[i + 1] - ty;
                                if ((corner_y > -1) && (corner_y < height))
                                {
                                    if (shape[corner_x, corner_y])
                                    {
                                        float dx = diag_x - corner_x;
                                        float dy = diag_y - corner_y;
                                        float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                                        if (dist <= min_dist)
                                        {
                                            min_dist = dist;
                                            winner_x[offset] = corner_x;
                                            winner_y[offset] = corner_y;
                                        }
                                    }
                                }
                            }

                            i += 2;
                        }
                    }

                    if (winner_x[0] > -1)
                    {
                        float xx = winner_x[0];
                        float yy = winner_y[0];

                        for (int idx = 1; idx <= 2; idx++)
                        {
                            if (winner_x[idx] > -1)
                            {
                                if ((winner_x[idx] != winner_x[0]) ||
                                    (winner_y[idx] != winner_y[0]))
                                {
                                    float dxx = winner_x[idx] - winner_x[0];
                                    if (Math.Abs(dxx) < width / 10)
                                    {
                                        float dyy = winner_y[idx] - winner_y[0];
                                        if (Math.Abs(dyy) < height / 10)
                                        {
                                            xx += (dxx / 2.0f);
                                            yy += (dyy / 2.0f);

                                            xx = centre_x + ((xx - centre_x) * (1.0f + Math.Abs(dxx / (2 * (float)(xx - centre_x)))));
                                            yy = centre_y + ((yy - centre_y) * (1.0f + Math.Abs(dyy / (2 * (float)(yy - centre_y)))));
                                            //idx = 3;
                                        }
                                    }
                                }
                            }
                        }
                        corners.Add(xx);
                        corners.Add(yy);
                        angles.Add((float)Math.PI / 2);
                    }

                    // flip the angle
                    diagonal_angle = ((float)Math.PI / 2) - diagonal_angle;
                }
            }
        }

        /// <summary>
        /// removes flat angles
        /// <summary>
        private void removeFlatAngles()
        {
            // remove flat angles
            ArrayList new_corners = new ArrayList();
            ArrayList new_angles = new ArrayList();

            for (int i = 0; i < angles.Count; i++)
            {
                // get the angle
                float angle = (float)angles[i];

                // convert to degrees, simply for readability
                angle = angle * 180 / (float)Math.PI;

                // keep in the range 0-90 degrees
                if (angle > 90)
                    angle = 180 - angle;

                // get the corner index
                int corner_index = (i + 1) * 2;
                if (corner_index >= corners.Count)
                    corner_index -= corners.Count;
                float x = (float)corners[corner_index];
                float y = (float)corners[corner_index + 1];

                // only non-flat angles may pass
                if (angle > 15)
                {
                    new_corners.Add(x);
                    new_corners.Add(y);
                    new_angles.Add((float)angles[i]);
                }
            }
            corners = new_corners;
            angles = new_angles;
        }

        /// <summary>
        /// calculates the angle subtended at each corner
        /// <summary>
        private void calculateAngles()
        {
            // find the angle for each corner
            int i = 0, prev_i = -1, next_i = -1;
            for (int j = 0; j < corners.Count + 2; j += 2)
            {
                if (j > 0)
                {
                    float x1 = (float)corners[prev_i];
                    float y1 = (float)corners[prev_i + 1];
                    float x2 = (float)corners[i];
                    float y2 = (float)corners[i + 1];
                    float x3 = (float)corners[next_i];
                    float y3 = (float)corners[next_i + 1];

                    float dx1 = x2 - x1;
                    float dy1 = y2 - y1;
                    float hyp1 = (float)Math.Sqrt((dx1 * dx1) + (dy1 * dy1));
                    float ang1 = (float)Math.PI;
                    if (dy1 < 0) ang1 = 0;
                    if (hyp1 > 0) ang1 = (float)Math.Acos(dx1 / hyp1);

                    float dx2 = x3 - x2;
                    float dy2 = y3 - y2;
                    float hyp2 = (float)Math.Sqrt((dx2 * dx2) + (dy2 * dy2));
                    float ang2 = (float)Math.PI;
                    if (dy2 < 0) ang2 = 0;
                    if (hyp2 > 0) ang2 = (float)Math.Acos(dx2 / hyp2);

                    float angle = ang2 - ang1;
                    if (angle < 0) angle += (float)Math.PI;
                    angles.Add(angle);
                }

                prev_i = i;
                i += 2;
                if (i >= corners.Count)
                    i -= corners.Count;
                next_i = i + 2;
                if (next_i >= corners.Count)
                    next_i -= corners.Count;
            }
        }


        /// <summary>
        /// detect angles
        /// <summary>        
        private void detectAngles()
        {
            angles = new ArrayList();

            if (corners.Count / 2 > 2)
            {
                calculateAngles();
                removeFlatAngles();

                if (corners.Count / 2 > 2)
                {
                    calculateAngles();

                    // find right angles		        		        
                    for (int i = 0; i < angles.Count; i++)
                    {
                        // get the angle
                        float angle = (float)angles[i];

                        // convert to degrees, simply for readability
                        angle = angle * 180 / (float)Math.PI;

                        // keep in the range 0-90 degrees
                        if (angle > 90)
                            angle = 180 - angle;

                        //if (corners.Count/2 == 4)
                        //Console.WriteLine("angle = " + Convert.ToString(angle));

                        if (angle > 70)
                            no_of_right_angles++;
                    }
                }
            }
        }



        /// <summary>
        /// returns the average pixel intensity along a line within the mono image
        /// </summary>
        public float averageLineIntensity(int x1, int y1, int x2, int y2)
        {
            float av_intensity = 0;
            int hits = 0;

            int w, h, x = 0, y = 0, step_x, step_y, dx, dy;
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
                    int s = 0;
                    while (s * Math.Abs(step_x) <= Math.Abs(dx))
                    {
                        y = (int)(m * (x - x1)) + y1;

                        if ((x >= 0) && (x < width) && (y >= 0) && (y < height))
                        {
                            int n = ((width * y) + x);
                            av_intensity += (int)mono_image[n];
                            hits++;
                        }

                        x += step_x;
                        s++;
                    }
                }
            }
            else
            {
                if (dy != 0)
                {
                    m = dx / (float)dy;
                    y = y1;
                    int s = 0;
                    while (s * Math.Abs(step_y) <= Math.Abs(dy))
                    {
                        x = (int)(m * (y - y1)) + x1;
                        if ((x >= 0) && (x < width) && (y >= 0) && (y < height))
                        {
                            int n = (width * y) + x;
                            av_intensity += (int)mono_image[n];
                            hits++;
                        }

                        y += step_y;
                        s++;
                    }
                }
            }
            if (hits > 0)
                return (av_intensity / (float)hits);
            else
                return (0);
        }

        /// <summary>
        /// determines whether this is dark features on a lighter
        /// background or vice versa
        /// the binary image must have been calculated beforehand
        /// </summary>
        /// <returns>true if there are farder features on a lighter background</returns>
        public bool BlackOnWhite()
        {
            bool isBlackOnWhite = true;
            const float inflation = 1.03f;

            if (binary_image != null)
            {
                if (corners.Count > 2)
                {
                    float av_intensity = 0;

                    for (int i = 0; i < corners.Count; i += 2)
                    {
                        int corner_index1 = i;
                        int corner_index2 = corner_index1 + 2;
                        if (corner_index2 >= corners.Count)
                            corner_index2 -= corners.Count;

                        // get the start and end points for the line
                        float x1 = (float)corners[corner_index1];
                        float y1 = (float)corners[corner_index1 + 1];

                        float dx = x1 - centre_x;
                        float dy = y1 - centre_y;
                        x1 = centre_x + (float)(dx * inflation);
                        y1 = centre_y + (float)(dy * inflation);

                        float x2 = (float)corners[corner_index2];
                        float y2 = (float)corners[corner_index2 + 1];

                        dx = x2 - centre_x;
                        dy = y2 - centre_y;
                        x2 = centre_x + (float)(dx * inflation);
                        y2 = centre_y + (float)(dy * inflation);

                        // get the average intensity along the line
                        // using the binary image
                        av_intensity += averageLineIntensityFromBinaryImage((int)x1, (int)y1, (int)x2, (int)y2);
                    }
                    av_intensity /= (corners.Count / 2);

                    // test the ratio of white to black
                    if (av_intensity < 0.5f)
                        isBlackOnWhite = false;
                }
            }
            else
                Console.WriteLine("binary image must have been calculated prior to calling BlackOnWhite()");

            return (isBlackOnWhite);
        }

        /// <summary>
        /// returns the intensity of foreground pixels for each side of the shape
        /// </summary>
        public float[] SideIntensities(bool isBlackOnWhite)
        {
            float[] side_intensity = new float[corners.Count / 2];

            if (binary_image != null)
            {
                if (corners.Count > 2)
                {
                    for (int i = 0; i < corners.Count; i += 2)
                    {
                        int corner_index1 = i;
                        int corner_index2 = corner_index1 + 2;
                        if (corner_index2 >= corners.Count)
                            corner_index2 -= corners.Count;

                        // get the start and end points for the line
                        int x1 = (int)corners[corner_index1];
                        int y1 = (int)corners[corner_index1 + 1];


                        int x2 = (int)corners[corner_index2];
                        int y2 = (int)corners[corner_index2 + 1];

                        // get the average intensity along the line
                        // using the binary image
                        side_intensity[i / 2] += averageLineIntensityFromBinaryImage(x1, y1, x2, y2);
                        if (isBlackOnWhite)
                            side_intensity[i / 2] = 1.0f - side_intensity[i / 2];
                    }
                }
            }
            else
                Console.WriteLine("binary image must have been calculated prior to calling SideIntensities");

            return (side_intensity);
        }


        /// <summary>
        /// return the average binary image intensity for the given line in the range 0.0-1.0
        /// </summary>
        private float averageLineIntensityFromBinaryImage(int x1, int y1, int x2, int y2)
        {
            float av_intensity = 0;
            int hits = 0;

            int w, h, x = 0, y = 0, step_x, step_y, dx, dy;
            float m;

            dx = x2 - x1;
            dy = y2 - y1;
            w = Math.Abs(dx);
            h = Math.Abs(dy);
            if ((w < width) && (h < height))
            {
                if (x2 >= x1) step_x = 1; else step_x = -1;
                if (y2 >= y1) step_y = 1; else step_y = -1;

                if (w > h)
                {
                    if (dx != 0)
                    {
                        m = dy / (float)dx;
                        x = x1;
                        int s = 0;
                        while (s * Math.Abs(step_x) <= w)
                        {
                            y = (int)(m * (x - x1)) + y1;

                            if ((x >= 0) && (x < width) && (y >= 0) && (y < height))
                            {
                                if (binary_image[x, y])
                                    av_intensity++;
                                hits++;
                            }

                            if ((x >= 0) && (x < width) && (y >= 0) && (y < height - 1))
                            {
                                if (binary_image[x, y + 1])
                                    av_intensity++;
                                hits++;
                            }

                            if ((x >= 0) && (x < width - 1) && (y >= 0) && (y < height - 1))
                            {
                                if (binary_image[x + 1, y])
                                    av_intensity++;
                                hits++;
                            }

                            if ((x > 0) && (x < width) && (y >= 0) && (y < height))
                            {
                                if (binary_image[x - 1, y])
                                    av_intensity++;
                                hits++;
                            }

                            x += step_x;
                            s++;
                        }
                    }
                }
                else
                {
                    if (dy != 0)
                    {
                        m = dx / (float)dy;
                        y = y1;
                        int s = 0;
                        while (s * Math.Abs(step_y) <= h)
                        {
                            x = (int)(m * (y - y1)) + x1;
                            if ((x >= 0) && (x < width) && (y >= 0) && (y < height))
                            {
                                if (binary_image[x, y])
                                    av_intensity++;
                                hits++;
                            }

                            if ((x >= 0) && (x < width) && (y >= 0) && (y < height - 1))
                            {
                                if (binary_image[x, y + 1])
                                    av_intensity++;
                                hits++;
                            }

                            if ((x >= 0) && (x < width - 1) && (y >= 0) && (y < height - 1))
                            {
                                if (binary_image[x + 1, y])
                                    av_intensity++;
                                hits++;
                            }

                            if ((x > 0) && (x < width) && (y >= 0) && (y < height))
                            {
                                if (binary_image[x - 1, y])
                                    av_intensity++;
                                hits++;
                            }

                            y += step_y;
                            s++;
                        }
                    }
                }
            }
            if (hits > 0)
                return (av_intensity / (float)hits);
            else
                return (0);
        }


        /// <summary>
        /// locate corners within the region
        /// </summary>
        /// <param name="min_separation">minimum separation in pixels used to combine corners which are close together</param>
        /// <param name="outline_step_size">step size when moving around the outline</param>
        /// <param name="angular_step_size_degrees">when creating a histogram of the outline radii use this angular step size</param>
        private void locateCorners(int min_separation,
                                   int outline_step_size,
                                   int angular_step_size_degrees)
        {
            int angle_step = angular_step_size_degrees;

            // create a radial profile of the shape
            ArrayList radial_profile = new ArrayList();
            ArrayList radial_profile_angle = new ArrayList();
            ArrayList[] radial_profile_angle_lookup = new ArrayList[361 / angle_step];
            float av_radius = 0;  // average radius
            for (int i = 0; i < outline.Count; i += outline_step_size)
            {
                // position of a point on the outline
                int x = (int)outline[i];
                int y = (int)outline[i + 1];

                // its x and y distance from the centre
                int dx = x - centre_x;
                int dy = y - centre_y;

                // radius value
                float radius = (float)Math.Sqrt((dx * dx) + (dy * dy));

                // orientation of this point
                float angle = (float)Math.PI;
                if (radius > 0)
                {
                    angle = (float)Math.Acos(dx / radius);
                    if (dy < 0) angle = (float)(Math.PI * 2) - angle;
                }
                int angle_degrees = (int)(angle * 180 / Math.PI);
                if (angle_degrees < 0) angle_degrees += 360;

                // create a new lookup list if needed	            
                if (radial_profile_angle_lookup[angle_degrees / angle_step] == null)
                    radial_profile_angle_lookup[angle_degrees / angle_step] = new ArrayList();

                // update the radial profile
                radial_profile.Add(radius);
                radial_profile_angle.Add(angle_degrees);

                // update the lookup table
                radial_profile_angle_lookup[angle_degrees / angle_step].Add(radius);

                // update the average radius
                av_radius += radius;
            }

            // average distance of the outline from the centre
            av_radius /= (outline.Count / 2);
            av_radius *= 1.1f;

            // look for corners
            corners = new ArrayList();
            float max_radius = 0;
            int max_index = 0;
            float max_difference = 0;
            for (int r = 0; r < radial_profile.Count; r++)
            {
                float radius = (float)radial_profile[r];

                // record major and minor lengths
                int angle_degrees = (int)radial_profile_angle[r];
                int angle_degrees_opposite = angle_degrees + 180;
                int angle_degrees_opposite2 = angle_degrees + 90;
                int angle_degrees_opposite3 = angle_degrees - 90;
                if (angle_degrees_opposite > 359) angle_degrees_opposite -= 360;
                if (angle_degrees_opposite2 > 359) angle_degrees_opposite2 -= 360;
                if (angle_degrees_opposite3 < 0) angle_degrees_opposite3 += 360;
                ArrayList opposite_candidates = radial_profile_angle_lookup[angle_degrees_opposite / angle_step];
                ArrayList opposite_candidates2 = radial_profile_angle_lookup[angle_degrees_opposite2 / angle_step];
                ArrayList opposite_candidates3 = radial_profile_angle_lookup[angle_degrees_opposite3 / angle_step];
                float radius_opposite = 0;
                float radius_opposite2 = 0;
                float radius2 = 0;
                if (opposite_candidates != null)
                {
                    for (int j = 0; j < opposite_candidates.Count; j++)
                    {
                        float opposite = (float)opposite_candidates[j];
                        if (opposite > radius_opposite)
                            radius_opposite = opposite;
                    }

                    if (opposite_candidates2 != null)
                    {
                        for (int j = 0; j < opposite_candidates2.Count; j++)
                        {
                            float opposite2 = (float)opposite_candidates2[j];
                            if (opposite2 > radius_opposite2)
                                radius_opposite2 = opposite2;
                        }

                        if (opposite_candidates3 != null)
                        {
                            for (int j = 0; j < opposite_candidates3.Count; j++)
                            {
                                float rr = (float)opposite_candidates3[j];
                                if (rr > radius2)
                                    radius2 = rr;
                            }
                        }
                    }
                }

                if ((radius2 > 0) &&
                    (radius_opposite > 0) &&
                    (radius_opposite2 > 0))
                {
                    float diameter1 = radius + radius_opposite;
                    float diameter2 = radius2 + radius_opposite2;
                    if ((diameter1 * diameter2 > max_difference) &&
                        (diameter1 > diameter2))
                    {
                        max_difference = diameter1 * diameter2;
                        major_axis_length = diameter1;
                        minor_axis_length = diameter2;
                        orientation = angle_degrees * (float)Math.PI / 180.0f;
                    }
                }


                if (radius >= av_radius)
                {
                    if (radius > max_radius)
                    {
                        max_radius = radius;
                        max_index = r;
                    }
                }
                if ((radius < av_radius) ||
                    ((max_radius > 0) && (radius < max_radius * 0.95f)))
                {
                    if (max_radius > 0)
                    {
                        float x = (int)outline[max_index * 2];
                        float y = (int)outline[(max_index * 2) + 1];
                        corners.Add(x);
                        corners.Add(y);
                    }
                    max_radius = 0;
                }
            }
            if (max_radius > 0)
            {
                float x = (int)outline[max_index * 2];
                float y = (int)outline[(max_index * 2) + 1];
                corners.Add(x);
                corners.Add(y);
            }

            // calculate aspect ratio
            if (minor_axis_length > 0)
                aspect_ratio = major_axis_length / minor_axis_length;

            // remove corners which are too close together
            ArrayList corners2 = new ArrayList();
            for (int i = 0; i < corners.Count; i += 2)
            {
                float x1 = (float)corners[i];
                float y1 = (float)corners[i + 1];
                if (x1 != 9999)
                {
                    corners2.Add(x1);
                    corners2.Add(y1);
                    for (int j = i + 2; j < corners.Count; j += 2)
                    {
                        float x2 = (float)corners[j];
                        float y2 = (float)corners[j + 1];
                        float dx = x2 - x1;
                        float dy = y2 - y1;
                        float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                        if (dist < min_separation)
                        {
                            corners[i] = ((x1 + x2) / 2);
                            corners[i + 1] = ((y1 + y2) / 2);
                            corners[j] = 9999.0f;
                        }
                    }
                }
            }
            corners = corners2;

            // adjust the orientation of the region
            // to compensate for detection of diagonals
            adjustOrientation();
        }

        #endregion

    }
}
