/*
    Thread used for parallel region growing, which utilises multi core processors
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
using System.Threading;

namespace sluggish.imageprocessing.regiongrowing
{
    public class ThreadRegionGrowingState
    {
        public bool active;

        public byte[] bmp;
        public byte[] bmp_mono;
        public int image_width, image_height;
        public int min_region_volume_percent;
        public float min_aspect_ratio, max_aspect_ratio;
        public float vertex_inflation;
        public float vertex_angular_offset;
        public int[] segmentation_threshold;
        public int[,] segmented;
        public ArrayList corner_features;
        public int downsampling_factor;
        public int border_percent;
        public String rectangle_classification;
        public String square_classification;

        public regionCollection regions;

        public ThreadRegionGrowingState(
                           byte[] bmp, int image_width, int image_height,
                           int min_region_volume_percent,
                           float min_aspect_ratio, float max_aspect_ratio,
                           float vertex_inflation,
                           float vertex_angular_offset,
                           int[] segmentation_threshold,
                           int[,] segmented, ArrayList corner_features,
                           int downsampling_factor,
                           int border_percent,
                           byte[] bmp_mono,
                           String rectangle_classification,
                           String square_classification)
        {
            active = true;
            this.bmp = bmp;
            this.bmp_mono = bmp_mono;
            this.image_width = image_width;
            this.image_height = image_height;
            this.min_region_volume_percent = min_region_volume_percent;
            this.min_aspect_ratio = min_aspect_ratio;
            this.max_aspect_ratio = max_aspect_ratio;
            this.vertex_inflation = vertex_inflation;
            this.vertex_angular_offset = vertex_angular_offset;
            this.segmentation_threshold = segmentation_threshold;
            this.segmented = segmented;
            this.corner_features = corner_features;
            this.downsampling_factor = downsampling_factor;
            this.border_percent = border_percent;
            this.rectangle_classification = rectangle_classification;
            this.square_classification = square_classification;
        }
    }

    /// <summary>
    /// this thread is used to parallelise the process of region growing
    /// </summary>
    public class ThreadRegionGrowing
    {
        private WaitCallback _callback;
        private object _data;

        /// <summary>
        /// constructor
        /// </summary>
        public ThreadRegionGrowing(WaitCallback callback, object data)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            _data = data;
        }

        /// <summary>
        /// ThreadStart delegate
        /// </summary>
        public void Execute()
        {
            // perform region growing using the available data
            ThreadRegionGrowingState state = (ThreadRegionGrowingState)_data;
            DetectRegions(state);
            state.active = false;
            _callback(_data);
        }

        /// <summary>
        /// detect regions within the given image
        /// </summary>
        /// <param name="state">region growing state</param>
        private static void DetectRegions(
            ThreadRegionGrowingState state)
        {
            state.regions = DetectRegions(state.bmp, state.image_width, state.image_height,
                                 state.min_region_volume_percent,
                                 state.min_aspect_ratio, state.max_aspect_ratio,
                                 state.vertex_inflation,
                                 state.vertex_angular_offset,
                                 state.segmentation_threshold,
                                 state.segmented, state.corner_features,
                                 state.downsampling_factor,
                                 state.border_percent,
                                 state.bmp_mono,
                                 state.rectangle_classification,
                                 state.square_classification);
        }

        /// <summary>
        /// detect regions within the given image
        /// </summary>
        /// <param name="bmp">image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="min_region_volume_percent">ignore regions which are smaller than this threshold</param>
        /// <param name="min_aspect_ratio">minimum aspect ratio</param>
        /// <param name="max_aspect_ratio">maxiumum aspect ratio</param>
        /// <param name="vertex_inflation"></param>
        /// <param name="vertex_angular_offset"></param>
        /// <param name="segmentation_threshold">thresholds to use for segmentation</param>
        /// <param name="segmented">segmentation map</param>
        /// <param name="corner_features">returns detected corner features as a list of integers, suitable for use by other functions</param>
        /// <param name="downsampling_factor">factor by which to reduce the original image size</param>
        /// <param name="border_percent">defines a border within which the centres of detected regions must be</param>
        /// <param name="rectangle_classification">the classification to be applied to shapes having a rectangular aspect ratio</param>
        /// <param name="square_classification">the classification to be applied to shapes having a square aspect ratio</param>
        /// <returns>list of regions detected</returns>
        public static regionCollection DetectRegions(
                           byte[] bmp, int image_width, int image_height,
                           int min_region_volume_percent,
                           float min_aspect_ratio, float max_aspect_ratio,
                           float vertex_inflation,
                           float vertex_angular_offset,
                           int[] segmentation_threshold,
                           int[,] segmented, ArrayList corner_features,
                           int downsampling_factor,
                           int border_percent,
                           byte[] bmp_mono,
                           String rectangle_classification,
                           String square_classification)
        {
            // dimensions of the downsampled image
            int small_image_width = image_width / downsampling_factor;
            int small_image_height = image_height / downsampling_factor;

            // the centre of detected regions must be inside this region
            // in order to be considered as candidates
            int min_x = small_image_width * border_percent / 100;
            int max_x = small_image_width - min_x;
            int min_y = small_image_height * border_percent / 100;
            int max_y = small_image_height - min_y;

            // stack used by the flood fill algorithm
            // allocating space here just saves the time needed to create the
            // array for each flood fill routine call
            int[] flood_fill_stack = new int[small_image_width *
                                             small_image_height * 5];

            // this object stores the detected regions
            regionCollection regions = new regionCollection();

            if (bmp != null)
            {
                // min region volume in pixels
                int min_region_volume = (image_width / downsampling_factor) *
                                        (image_height / downsampling_factor) *
                                        min_region_volume_percent / 1000;

                // create arrays which will be used for flood fill operations
                bool[,] filled = new bool[small_image_width, small_image_height];
                bool[,] region_image = new bool[small_image_width, small_image_height];

                for (int threshold_level = 0; threshold_level < segmentation_threshold.Length; threshold_level++)
                {
                    // clear the filled array
                    if (threshold_level > 0)
                        for (int clear_x = 0; clear_x < small_image_width; clear_x++)
                            for (int clear_y = 0; clear_y < small_image_height; clear_y++)
                                filled[clear_x, clear_y] = false;

                    int tx, ty, bx, by;
                    for (int y = 0; y < small_image_height; y++)
                    {
                        for (int x = 0; x < small_image_width; x++)
                        {
                            if (filled[x, y] == false)
                                if (segmented[x, y] > segmentation_threshold[threshold_level])
                                {
                                    tx = x; ty = y; bx = x; by = y;
                                    long pixels = 0;
                                    long av_intensity = 0;
                                    long av_x = 0, av_y = 0;

                                    // clear the region_image array
                                    for (int clear_x = 0; clear_x < small_image_width; clear_x++)
                                        for (int clear_y = 0; clear_y < small_image_height; clear_y++)
                                            region_image[clear_x, clear_y] = false;

                                    // using a linear flood fill avoids stack overflow exceptions
                                    sluggish.utilities.drawing.floodFillLinear(
                                        x, y, segmentation_threshold[threshold_level],
                                        flood_fill_stack,
                                        ref tx, ref ty, ref bx, ref by,
                                        ref pixels, ref av_intensity,
                                        segmented, filled, region_image, ref av_x, ref av_y,
                                        small_image_width, small_image_height);

                                    if (by > ty)
                                    {
                                        if (pixels > min_region_volume)
                                        {
                                            av_x = av_x / pixels;
                                            av_y = av_y / pixels;

                                            // is the centre of the region inside the border?
                                            if ((av_x > min_x) && (av_x < max_x) &&
                                                (av_y > min_y) && (av_y < max_y))
                                            {
                                                // convert values from region growing back into
                                                // the original image resolution
                                                if (downsampling_factor > 1)
                                                {
                                                    av_x *= downsampling_factor;
                                                    av_y *= downsampling_factor;
                                                    tx *= downsampling_factor;
                                                    ty *= downsampling_factor;
                                                    bx *= downsampling_factor;
                                                    by *= downsampling_factor;

                                                }

                                                // add an extra border		                                                
                                                int border = (bx - tx) / 20;
                                                int tx2 = tx - border;
                                                if (tx2 < 0) tx2 = 0;
                                                int ty2 = ty - border;
                                                if (ty2 < 0) ty2 = 0;
                                                int bx2 = bx + border;
                                                if (bx2 >= image_width) bx2 = image_width - 1;
                                                int by2 = by + border;
                                                if (by2 >= image_height) by2 = image_height - 1;

                                                // create a region object                                            
                                                int wdth = bx2 - tx2;
                                                int hght = by2 - ty2;
                                                sluggish.imageprocessing.region new_region =
                                                    new sluggish.imageprocessing.region(tx2, ty2, wdth, hght,
                                                                                        region_image, segmented, (int)av_x, (int)av_y,
                                                                                        corner_features,
                                                                                        bmp_mono, image_width, image_height,
                                                                                        vertex_inflation,
                                                                                        vertex_angular_offset,
                                                                                        downsampling_factor);

                                                if (new_region.aspect_ratio > max_aspect_ratio) new_region.classification = "text";

                                                // add this region to the collection
                                                regions.Add(new_region);
                                                new_region.classification = rectangle_classification;

                                                if ((new_region.aspect_ratio >= min_aspect_ratio) &&
                                                    (new_region.aspect_ratio <= max_aspect_ratio))
                                                {
                                                    new_region.classification = square_classification;
                                                }

                                            }
                                        }
                                    }
                                }
                        }
                    }
                }

            }

            return (regions);
        }


        // WaitCallback delegate
        static void DetectRegions(object state)
        {
            Console.WriteLine("State: " + state);
        }
    }
}
