/*
    simple stereo vision using both luminence and colour
    Copyright (C) 2008 Bob Mottram
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
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;

namespace surveyor.vision
{
    public class StereoVisionCanny : StereoVision
    {        
        private EdgeDetectorCanny[] edge_detector;
        private byte[][] edge_img;
        public int vertical_compression = 4;
        public int max_edges_per_row = 6;
        private int matching_threshold = 10000;
        public int minimum_variance_percent = 5;
        public int max_disparity = 35;
        const int compare_radius = 4;
        private int mean_variance;
    
        public StereoVisionCanny()
        {
            edge_detector = new EdgeDetectorCanny[2];
            edge_detector[0] = new EdgeDetectorCanny();
            edge_detector[1] = new EdgeDetectorCanny();
            edge_img = new byte[2][];
        }
        
        private void Supress(List<int> edges, List<int> edges_x, int max_edges, EdgeDetectorCanny detector)
        {
        
            while (edges.Count > max_edges)
            {
                int min_index = 0;
                int min_magnitude = int.MaxValue;
                for (int i = edges.Count-1; i >= 0; i--)
                {
                    int idx = edges[i];
                    //Console.WriteLine("idx: " + idx.ToString() + "  " + detector.magnitude.Length.ToString()); 
                    if (detector.magnitude[idx] < min_magnitude)
                    {
                        min_magnitude = detector.magnitude[idx];
                        min_index = i;
                    }
                }
                edges.RemoveAt(min_index);
                edges_x.RemoveAt(min_index);
            }
            
        }

        /// <summary>
        /// remove low variance edge features, because we may have difficulty in matching these
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="edges_x"></param>
        /// <param name="bmp"></param>
        /// <param name="minimum_variance"></param>
        private void SupressLowVariance(List<int> edges, List<int> edges_x, 
                                        byte[] bmp, int minimum_variance,
                                        ref int current_mean_variance,
                                        ref int mean_variance_hits)
        {
            int pixels = bmp.Length;
            
            for (int i = edges.Count-1; i >= 0; i--)
            {
                int n = edges[i];
                
                if ((n - compare_radius > -1) && (n + compare_radius < pixels))
                {                
                    int variance = 0;
                    for (int x = -compare_radius; x < 0; x++)
                    {
                        int nn1 = n + x;
                        int nn2 = n - x;
                        int diff = bmp[nn1] - bmp[nn2];
                        diff += bmp[nn1] - bmp[n + compare_radius + x];
                        variance += diff * diff;
                    }
                    
                    current_mean_variance += variance;
                    mean_variance_hits++;
                    
                    if (variance < minimum_variance)
                    {
                        edges.RemoveAt(i);
                        edges_x.RemoveAt(i);
                    }
                }
            }
        }


        /// <summary>
        /// returns a measure of the similarity of two points
        /// </summary>
        /// <param name="n1">pixel index in the left image</param>
        /// <param name="n2">pixel index in the right image</param>
        /// <param name="pixels">number of pixels in the image</param>
        /// <returns>
        /// sum of squared differences
        /// </returns>
        private int similarity(int n1, int n2, int pixels)
        {
            int result = 0;            
            int hits = 0;

            for (int x = -compare_radius; x <= compare_radius; x++)
            {
                int nn1 = n1 + x;
                int nn2 = n2 + x;
                if ((nn1 > -1) && (nn2 > -1) &&
                    (nn1 < pixels) && (nn2 < pixels))
                {
                    int diff = edge_img[0][nn1] - edge_img[1][nn2];  
                    result += diff * diff;
                }
                else
                {
                    result = int.MaxValue;
                    break;
                }
            }
            
            return(result);
        }
        
        private void CompareRows(int y,
                                 List<int> left_edges, List<int> left_edges_x,
                                 List<int> right_edges, List<int> right_edges_x, 
                                 int max_edges,
                                 int matching_threshold,
                                 ref int current_mean_variance,
                                 ref int mean_variance_hits)
        {
            int pixels = image_width * image_height;
            int max_disparity_pixels = image_width * max_disparity / 100;
            
            // limit the number of edges
            Supress(left_edges, left_edges_x, max_edges, edge_detector[0]);
            Supress(right_edges, right_edges_x, max_edges, edge_detector[1]);
            
            // remove low variance features
            int minimum_variance = matching_threshold * minimum_variance_percent / 100;
            SupressLowVariance(left_edges, left_edges_x, edge_img[0], minimum_variance, ref current_mean_variance, ref mean_variance_hits);
            SupressLowVariance(right_edges, right_edges_x, edge_img[1], minimum_variance, ref current_mean_variance, ref mean_variance_hits);
            
            for (int i = 0; i < left_edges.Count; i++)
            {
                for (int j = 0; j < right_edges.Count; j++)
                {
                    int disparity = left_edges_x[i] - right_edges_x[j];
                    if ((disparity >= 0) && (disparity < max_disparity_pixels))
                    {
                        int v = similarity(left_edges[i], right_edges[j], pixels);
                        if (v < matching_threshold)
                        {
                            features.Add(new StereoFeature(left_edges_x[i], y, disparity));
                        }
                    }
                }
            }
        }
        
        private void Match(int max_edges_per_row, int matching_threshold,
                           int calibration_offset_x, int calibration_offset_y)
        {
            int current_mean_variance = 0;
            int mean_variance_hits = 0;
            features.Clear();
            int offset = calibration_offset_x + ((calibration_offset_y / vertical_compression) * image_width);
            int n1 = 0;
            int n2 = 0;
            int max = image_width * (image_height / vertical_compression);
            for (int y = 0; y < image_height / vertical_compression; y++)
            {
                List<int> left_edges = new List<int>();
                List<int> left_edges_x = new List<int>();
                List<int> right_edges = new List<int>();
                List<int> right_edges_x = new List<int>();
                
                for (int x = 0; x < image_width; x++)
                {
                    if (edge_detector[0].edgesImage[n1] == 0)
                    {
                        left_edges.Add(n2);
                        left_edges_x.Add(x);
                    }
                    if (edge_detector[1].edgesImage[n1] == 0)
                    {
                        int nn2 = n2 + offset;
                        if ((nn2 > 0) && (nn2 < max))
                        {
                            right_edges.Add(nn2);
                            right_edges_x.Add(x + calibration_offset_x);
                        }
                    }
                    n1 += 3;
                    n2++;
                }
                
                CompareRows(y * vertical_compression, left_edges, left_edges_x, right_edges, right_edges_x, max_edges_per_row, matching_threshold, 
                            ref current_mean_variance, ref mean_variance_hits);
            }
            
            if (mean_variance_hits > 0)
            {
                current_mean_variance /= mean_variance_hits;
                matching_threshold = current_mean_variance * 100 / minimum_variance_percent;
            }
        }

        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right_image_data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        public override void Update(byte[] left_bmp, byte[] right_bmp,
                                   int image_width, int image_height,
                                   float calibration_offset_x, float calibration_offset_y)
        {
            this.image_width = image_width;
            this.image_height = image_height;
            
            if (edge_img[0] == null)
            {
                edge_img[0] = new byte[image_width * (image_height / vertical_compression)];
                edge_img[1] = new byte[edge_img[0].Length];
            }
            
            int n = 0;
            int n2 = 0;
            int incr = image_width * 3 * vertical_compression;
            for (int y = 0; y < (image_height / vertical_compression); y++)
            {
                int n3 = n2;
                for (int x = 0; x < image_width; x++)
                {
                    edge_img[0][n] = (byte)((left_bmp[n3] + left_bmp[n3+1] + left_bmp[n3+2]) * 0.33f); 
                    edge_img[1][n] = (byte)((right_bmp[n3] + right_bmp[n3+1] + right_bmp[n3+2]) * 0.33f); 
                    n3 += 3;
                    n++;
                }
                n2 += incr;
            }
            
            edge_detector[0].Update(edge_img[0], image_width, image_height / vertical_compression);
            edge_detector[1].Update(edge_img[1], image_width, image_height / vertical_compression);
            Match(max_edges_per_row, matching_threshold, (int)calibration_offset_x, (int)calibration_offset_y);
        }
    }
}
