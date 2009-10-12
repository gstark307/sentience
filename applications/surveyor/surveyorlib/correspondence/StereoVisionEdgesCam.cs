/*
    edge based stereo
    Copyright (C) 2009 Bob Mottram
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
using System.Drawing;
using System.Collections.Generic;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// 
    /// </summary>
    public class StereoVisionEdgesCam
    {
        const int SVS_MAX_FEATURES = 2000;
        const int SVS_MAX_MATCHES = 2000;
        const int SVS_MAX_IMAGE_WIDTH = 1024;
        const int SVS_MAX_IMAGE_HEIGHT = 1024;
        const int SVS_VERTICAL_SAMPLING = 2;
        const int SVS_HORIZONTAL_SAMPLING = 8;
        const int SVS_DESCRIPTOR_PIXELS = 30;
        const int SVS_MAX_LINES = 200;
        const int SVS_PEAKS_HISTORY = 10;
        const bool SVS_VERBOSE = true;
        const int SVS_FILTER_SAMPLING = 40;

        public uint imgWidth, imgHeight;
    
        /* array storing x coordinates of detected features */
        public short[] feature_x;
    
        /* array storing y coordinates of detected features */
        private short[] feature_y;
    
        /* array storing the number of features detected on each row */
        public ushort[] features_per_row;
    
        /* array storing the number of features detected on each column */
        private ushort[] features_per_col;
    
        /* Array storing a binary descriptor, 32bits in length, for each detected feature.
         * This will be used for matching purposes.*/
        public uint[] descriptor;
    
        /* mean luminance for each feature */
        public byte[] mean;
    
        /* buffer which stores sliding sum */
        private int[] row_sum;
    
        /* buffer used to find peaks in edge space */
        private uint[] row_peaks;
    
        /* array stores matching probabilities (prob,x,y,disp) */
        public uint[] svs_matches;
    
        /* used during filtering */
        private byte[] valid_quadrants;
    
        /* a brief history of histogram peaks, used for filtering */
        private ushort[] peaks_history;
        private int peaks_history_index;
        private int enable_peaks_filter;
    
        /* array used to store a disparity histogram */
        private int[] disparity_histogram_plane;
        private int[] disparity_plane_fit;
        
        /* number of detected planes found during the filtering step */
        private int no_of_planes;
        private int[] plane;
    
        /* maps raw image pixels to rectified pixels */
        private int[] calibration_map;
    
        /* priors used when processing a video stream */
        private int[] disparity_priors;

        /* offsets of pixels to be compared within the patch region
         * arranged into a roughly rectangular structure */
        int[] pixel_offsets = { -2, -4, -1, -4, 1, -4, 2, -4, -5, -2, -4, -2, -3,
                -2, -2, -2, -1, -2, 0, -2, 1, -2, 2, -2, 3, -2, 4, -2, 5, -2, -5, 2,
                -4, 2, -3, 2, -2, 2, -1, 2, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2, 5, 2, -2, 4,
                -1, 4, 1, 4, 2, 4 };
        
        /* lookup table used for counting the number of set bits */
        uint[] BitsSetTable256 = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3,
                2, 3, 3, 4, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 1, 2, 2, 3,
                2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5,
                4, 5, 5, 6, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4,
                3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5,
                4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 1, 2, 2, 3,
                2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5,
                4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5,
                4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5,
                4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 3, 4, 4, 5,
                4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7,
                6, 7, 7, 8 };
                
        public void init(int width, int height)
        {
            imgWidth = (uint)width;
            imgHeight = (uint)height;
        
            /* array storing x coordinates of detected features */
            feature_x = new short[SVS_MAX_FEATURES];
            feature_y = new short[SVS_MAX_FEATURES];
        
            disparity_histogram_plane = new int[(SVS_MAX_IMAGE_WIDTH/SVS_FILTER_SAMPLING)*(SVS_MAX_IMAGE_WIDTH / 2)];
            disparity_plane_fit = new int[SVS_MAX_IMAGE_WIDTH / SVS_FILTER_SAMPLING];
            plane = new int[11*9];

            calibration_map = null;
        
            /* array storing the number of features detected on each row */
            features_per_row = new ushort[SVS_MAX_IMAGE_HEIGHT / SVS_VERTICAL_SAMPLING];
            features_per_col = new ushort[SVS_MAX_IMAGE_WIDTH / SVS_HORIZONTAL_SAMPLING];
        
            /* Array storing a binary descriptor, 32bits in length, for each detected feature.
             * This will be used for matching purposes.*/
            descriptor = new uint[SVS_MAX_FEATURES];
        
            /* mean luminance for each feature */
            mean = new byte[SVS_MAX_FEATURES];
        
            /* buffer which stores sliding sum */
            row_sum = new int[SVS_MAX_IMAGE_WIDTH];
        
            /* buffer used to find peaks in edge space */
            row_peaks = new uint[SVS_MAX_IMAGE_WIDTH];
        
            /* array stores matching probabilities (prob,x,y,disp) */
            svs_matches = null;
        
            /* array used during filtering */
            valid_quadrants = null;
        
            /* priors */
            disparity_priors = null;
        
            /* disparity histogram peaks */
            peaks_history = null;
            peaks_history_index = 0;
            enable_peaks_filter = 0;            
        }

        private int pixindex(int x, int y)
        {
            return(((y*(int)imgWidth)+x)*3);
        }

        /* Updates sliding sums and edge response values along a single row or column
         * Returns the mean luminance along the row or column */
        private int svs_update_sums(
            int cols, /* if non-zero we're dealing with columns not rows */
            int i, /* row or column index */
            byte[] rectified_frame_buf) { /* image data */
        
            int j, x, y, idx, max, sum = 0, mean = 0;
        
            if (cols == 0) {
                /* compute sums along the row */
                y = i;
                idx = (int)imgWidth * y * 3 + 2;
                max = (int)imgWidth;
        
                row_sum[0] = rectified_frame_buf[idx];
                for (x = 1; x < max; x++, idx += 3) {
                    sum += rectified_frame_buf[idx];
                    row_sum[x] = sum;
                }
            } else {
                /* compute sums along the column */
                idx = i*3+2;
                x = i;
                max = (int) imgHeight;
                int stride = (int)imgWidth*3;
        
                row_sum[0] = rectified_frame_buf[idx];
                for (y = 1; y < max; y++, idx += stride) {
                    sum += rectified_frame_buf[idx];
                    row_sum[y] = sum;
                }
            }
        
            /* row mean luminance */
            mean = row_sum[max - 1] / (max * 2);
        
            /* compute peaks */
            int p0, p1;
            for (j = 2; j < max - 2; j++) {
                sum = row_sum[j];
                /* edge using 2 pixel radius */
                p0 = (sum - row_sum[j - 1]) - (row_sum[j + 1] - sum);
                if (p0 < 0)
                    p0 = -p0;
        
                /* edge using 4 pixel radius */
                p1 = (sum - row_sum[j - 2]) - (row_sum[j + 2] - sum);
                if (p1 < 0)
                    p1 = -p1;
        
                /* overall edge response */
                row_peaks[j] = (uint)(p0 + p1);
            }
        
            return (mean);
        }
        
        /* performs non-maximal suppression on the given row or column */
        private void svs_non_max(
            int cols, /* if non-zero we're dealing with columns not rows */
            int inhibition_radius, /* radius for non-maximal suppression */
            uint min_response) /* minimum threshold as a percent in the range 0-200 */
        {         
            int i, r, max, max2;
            uint v;
        
            /* average response */
            uint av_peaks = 0;
            max = (int) imgWidth;
            if (cols != 0)
                max = (int) imgHeight;
            for (i = 4; i < max - 4; i++) {
                av_peaks += row_peaks[i];
            }
            av_peaks /= (uint)(max - 8);
        
            /* adjust the threshold */
            av_peaks = av_peaks * min_response / 100;
        
            max2 = max - inhibition_radius;
            for (i = 4; i < max2; i++) {
                if (row_peaks[i] < av_peaks)
                    row_peaks[i] = 0;
                v = row_peaks[i];
                if (v > 0) {
                    for (r = 1; r < inhibition_radius; r++) {
                        if (row_peaks[i + r] < v) {
                            row_peaks[i + r] = 0;
                        } else {
                            row_peaks[i] = 0;
                            r = inhibition_radius;
                        }
                    }
                }
            }
        }
        
        /* creates a binary descriptor for a feature at the given coordinate
         which can subsequently be used for matching */
        private int svs_compute_descriptor(
            int px, 
            int py, 
            byte[] rectified_frame_buf,
            int no_of_features, int row_mean) 
        {        
            byte bit_count = 0;
            int pixel_offset_idx, ix;
            int meanval = 0;
            uint bit, desc = 0;
        
            /* find the mean luminance for the patch */
            for (pixel_offset_idx = 0; pixel_offset_idx < SVS_DESCRIPTOR_PIXELS * 2; pixel_offset_idx
                    += 2) {
                ix
                        = rectified_frame_buf[pixindex((px + pixel_offsets[pixel_offset_idx]), (py + pixel_offsets[pixel_offset_idx + 1]))];
                meanval += rectified_frame_buf[ix + 2] + rectified_frame_buf[ix + 1]
                        + rectified_frame_buf[ix];
            }
            meanval /= SVS_DESCRIPTOR_PIXELS;
        
            /* binarise */
            bit = 1;
            for (pixel_offset_idx = 0; pixel_offset_idx < SVS_DESCRIPTOR_PIXELS * 2; pixel_offset_idx
                    += 2, bit *= 2) {
        
                ix
                        = rectified_frame_buf[pixindex((px + pixel_offsets[pixel_offset_idx]), (py + pixel_offsets[pixel_offset_idx + 1]))];
                if (rectified_frame_buf[ix + 2] + rectified_frame_buf[ix + 1]
                        + rectified_frame_buf[ix] > meanval) {
                    desc |= bit;
                    bit_count++;
                }
            }
        
            if ((bit_count > 3) && (bit_count < SVS_DESCRIPTOR_PIXELS - 3)) {
                meanval /= 3;
        
                /* adjust the patch luminance relative to the mean
                 * luminance for the entire row.  This helps to ensure
                 * that comparisons between left and right images are
                 * fair even if there are exposure/illumination differences. */
                meanval = meanval - row_mean + 127;
                if (meanval < 0)
                    meanval = 0;
                if (meanval > 255)
                    meanval = 255;
        
                mean[no_of_features] = (byte) (meanval / 3);
                descriptor[no_of_features] = desc;
                return (0);
            } else {
                /* probably just noise */
                return (-1);
            }
        }
        
        /* returns a set of vertically oriented edge features suitable for stereo matching */
        public int svs_get_features_vertical(
            byte[] rectified_frame_buf, /* image data */
            int inhibition_radius, /* radius for non-maximal supression */
            uint minimum_response, /* minimum threshold */
            int calibration_offset_x, /* calibration x offset in pixels */
            int calibration_offset_y) /* calibration y offset in pixels */
        {        
            ushort no_of_feats;
            int x, y, row_mean, start_x;
            int no_of_features = 0;
            int row_idx = 0;

            for (int i = (SVS_MAX_IMAGE_HEIGHT / SVS_VERTICAL_SAMPLING)-1; i>=0; i--) features_per_row[i]=0;
        
            start_x = (int)imgWidth - 15;
            if ((int) imgWidth - inhibition_radius - 1 < start_x)
                start_x = (int) imgWidth - inhibition_radius - 1;
        
            for (y = 4 + calibration_offset_y; y < (int) imgHeight - 4; y
                    += SVS_VERTICAL_SAMPLING) {
        
                /* reset number of features on the row */
                no_of_feats = 0;
        
                if ((y >= 4) && (y <= (int) imgHeight - 4)) {
        
                    row_mean = svs_update_sums(0, y, rectified_frame_buf);
                    svs_non_max(0, inhibition_radius, minimum_response);
        
                    /* store the features */
                    for (x = start_x; x > 15; x--) {
                        if (row_peaks[x] > 0) {
        
                            if (svs_compute_descriptor(x, y, rectified_frame_buf, no_of_features, row_mean) == 0)
                            {        
                                feature_x[no_of_features++] = (short)(x + calibration_offset_x);
                                no_of_feats++;
                                if (no_of_features == SVS_MAX_FEATURES) {
                                    y = (int)imgHeight;
                                    Console.WriteLine("stereo feature buffer full");
                                    break;
                                }
                            }
                        }
                    }
                }
        
                features_per_row[row_idx++] = no_of_feats;
            }
        
            no_of_features = no_of_features;
        
            if (SVS_VERBOSE)
                Console.WriteLine(no_of_features.ToString() + " vertically oriented edge features located");        
        
            return (no_of_features);
        }
        
        /* returns a set of horizontally oriented features
         these can't be matched directly, but their disparities might be infered */
        public int svs_get_features_horizontal(
            byte[] rectified_frame_buf, /* image data */
            int inhibition_radius, /* radius for non-maximal supression */
            uint minimum_response, /* minimum threshold */
            int calibration_offset_x, int calibration_offset_y) 
        {        
            ushort no_of_feats;
            int x, y, col_mean, start_y;
            int no_of_features = 0;
            int col_idx = 0;
        
            /* create arrays */
            if (features_per_col == null) {
                features_per_col = new ushort[SVS_MAX_IMAGE_WIDTH / SVS_HORIZONTAL_SAMPLING];
                feature_y = new short[SVS_MAX_FEATURES];
            }

            for (int i = (SVS_MAX_IMAGE_WIDTH / SVS_HORIZONTAL_SAMPLING)-1;i>=0; i--) features_per_col[i]=0;
        
            start_y = (int)imgHeight - 15;
            if ((int) imgHeight - inhibition_radius - 1 < start_y)
                start_y = (int) imgHeight - inhibition_radius - 1;
        
            for (x = 4 + calibration_offset_x; x < (int) imgWidth - 4; x += SVS_HORIZONTAL_SAMPLING) {
        
                /* reset number of features on the row */
                no_of_feats = 0;
        
                if ((x >= 4) && (x <= (int) imgWidth - 4)) {
        
                    col_mean = svs_update_sums(1, x, rectified_frame_buf);
                    svs_non_max(1, inhibition_radius, minimum_response);
        
                    /* store the features */
                    for (y = start_y; y > 15; y--) {
                        if (row_peaks[y] > 0) {
        
                            if (svs_compute_descriptor(x, y, rectified_frame_buf,
                                    no_of_features, col_mean) == 0) {
                                feature_y[no_of_features++] = (short) (y
                                        + calibration_offset_y);
                                no_of_feats++;
                                if (no_of_features == SVS_MAX_FEATURES) {
                                    x = (int)imgWidth;
                                    Console.WriteLine("stereo feature buffer full");
                                    break;
                                }
                            }
                        }
                    }
                }
        
                features_per_col[col_idx++] = no_of_feats;
            }
        
            if (SVS_VERBOSE)
                Console.WriteLine(no_of_features.ToString() + " horizontally oriented edge features located");
        
            return (no_of_features);
        }
        
        /* Match features from this camera with features from the opposite one.
         * It is assumed that matching is performed on the left camera CPU */
        public int svs_match(
            StereoVisionEdgesCam other, 
            int ideal_no_of_matches, /* ideal number of matches to be returned */
            int max_disparity_percent, /* max disparity as a percent of image width */
            int learnDesc, /* descriptor match weight */
            int learnLuma, /* luminance match weight */
            int learnDisp, /* disparity weight */
            int learnPrior, /* prior weight */
            int use_priors) /* if non-zero then use priors, assuming time between frames is small */
        {         
            int x, xL = 0, xR, L, R, y, no_of_feats, no_of_feats_left,
                    no_of_feats_right, row, col = 0, bit, disp_diff;
            int luma_diff, disp_prior = 0, min_disp, max_disp = 0, max_disp_pixels,
                    meanL, meanR, disp = 0, fL = 0, fR = 0, bestR = 0;
            uint descLanti, descR, desc_match;
            uint correlation, anticorrelation, total;
            uint descL, n, match_prob, best_prob;
            int idx, max, curr_idx = 0, search_idx, winner_idx = 0;
            int no_of_possible_matches = 0, matches = 0;
            int itt, prev_matches, row_offset, col_offset;
        
            uint meandescL, meandescR;
            short[] meandesc = new short[SVS_DESCRIPTOR_PIXELS];
        
            /* create arrays */
            if (svs_matches == null) {
                svs_matches = new uint[SVS_MAX_MATCHES * 4];
                valid_quadrants = new byte[SVS_MAX_MATCHES];
                peaks_history = new ushort[4*SVS_PEAKS_HISTORY];
                disparity_priors = new int[SVS_MAX_IMAGE_WIDTH * SVS_MAX_IMAGE_HEIGHT / (16*SVS_VERTICAL_SAMPLING)];
            }
        
            /* convert max disparity from percent to pixels */
            max_disp_pixels = max_disparity_percent * (int)imgWidth / 100;
            min_disp = -10;
            max_disp = max_disp_pixels;
        
            row = 0;
            for (y = 4; y < (int) imgHeight - 4; y += SVS_VERTICAL_SAMPLING, row++) {
        
                /* number of features on left and right rows */
                no_of_feats_left = features_per_row[row];
                no_of_feats_right = other.features_per_row[row];
        
                /* compute mean descriptor for the left row
                 * this will be used to create eigendescriptors */
                meandescL = (uint)0;
                for (int i = SVS_DESCRIPTOR_PIXELS-1; i>=0;i--) meandesc[i]=0;
                for (L = 0; L < no_of_feats_left; L++) {
                    descL = descriptor[fL + L];
                    n = 1;
                    for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2) {
                        uint v = descL & n;
                        if (v != 0)
                            meandesc[bit]++;
                        else
                            meandesc[bit]--;
                    }
                }
                n = 1;
                for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2) {
                    if (meandesc[bit] >= 0)
                        meandescL |= (uint)n;
                }
        
                /* compute mean descriptor for the right row
                 * this will be used to create eigendescriptors */
                meandescR = 0;
                for (int i = SVS_DESCRIPTOR_PIXELS-1; i>=0;i--) meandesc[i]=0;
                for (R = 0; R < no_of_feats_right; R++) {
                    descR = other.descriptor[fR + R];
                    n = 1;
                    for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2) {
                        uint v = descR & n;
                        if (v != 0)
                            meandesc[bit]++;
                        else
                            meandesc[bit]--;
                    }
                }
                n = 1;
                for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2) {
                    if (meandesc[bit] > 0)
                        meandescR |= (uint)n;
                }
        
                /* features along the row in the left camera */
                for (L = 0; L < no_of_feats_left; L++) {
        
                    /* x coordinate of the feature in the left camera */
                    xL = feature_x[fL + L];
        
                    if (use_priors != 0) {
                        disp_prior = disparity_priors[(row * imgWidth + xL) / 16];
                    }
        
                    /* mean luminance and eigendescriptor for the left camera feature */
                    meanL = mean[fL + L];
                    descL = descriptor[fL + L] & meandescL;
        
                    /* invert bits of the descriptor for anti-correlation matching */
                    n = descL;
                    descLanti = 0;
                    for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++) {
                        /* Shift result vector to higher significance. */
                        descLanti <<= 1;
                        /* Get least significant input bit. */
                        descLanti |= (uint)(n & 1);
                        /* Shift input vector to lower significance. */
                        n >>= 1;
                    }
        
                    total = 0;
        
                    /* features along the row in the right camera */
                    for (R = 0; R < no_of_feats_right; R++) {
        
                        /* set matching score to zero */
                        row_peaks[R] = 0;
        
                        /* x coordinate of the feature in the right camera */
                        xR = other.feature_x[fR + R];
        
                        /* compute disparity */
                        disp = xL - xR;
        
                        /* is the disparity within range? */
                        if ((disp >= min_disp) && (disp < max_disp)) {
                            if (disp < 0)
                                disp = 0;
        
                            /* mean luminance for the right camera feature */
                            meanR = other.mean[fR + R];
        
                            /* is the mean luminance similar? */
                            luma_diff = meanR - meanL;
        
                            /* right camera feature eigendescriptor */
                            descR = other.descriptor[fR + R] & meandescR;
        
                            /* bitwise descriptor correlation match */
                            desc_match = descL & descR;
        
                            /* count the number of correlation bits */
                            correlation = BitsSetTable256[desc_match & 0xff]
                                    + BitsSetTable256[(desc_match >> 8) & 0xff]
                                    + BitsSetTable256[(desc_match >> 16) & 0xff]
                                    + BitsSetTable256[desc_match >> 24];
        
        
                                /* bitwise descriptor anti-correlation match */
                                desc_match = descLanti & descR;
        
                                /* count the number of anti-correlation bits */
                                anticorrelation = BitsSetTable256[desc_match & 0xff]
                                        + BitsSetTable256[(desc_match >> 8) & 0xff]
                                        + BitsSetTable256[(desc_match >> 16) & 0xff]
                                        + BitsSetTable256[desc_match >> 24];
        
                                if (luma_diff < 0)
                                    luma_diff = -luma_diff;
                                int score =
                                        10000 + (max_disp * learnDisp)
                                                + (((int) correlation
                                                        + (int) (SVS_DESCRIPTOR_PIXELS
                                                                - anticorrelation))
                                                        * learnDesc) - (luma_diff
                                                * learnLuma) - (disp * learnDisp);
                                if (use_priors!=0) {
                                    disp_diff = disp - disp_prior;
                                    if (disp_diff < 0)
                                        disp_diff = -disp_diff;
                                    score -= disp_diff * learnPrior;
                                }
                                if (score < 0)
                                    score = 0;
        
                                /* store overall matching score */
                                row_peaks[R] = (uint) score;
                                total += row_peaks[R];
                            
                        } else {
                            if ((disp < min_disp) && (disp > -max_disp)) {
                                row_peaks[R] = (uint) ((max_disp - disp)
                                        * learnDisp);
                                total += row_peaks[R];
                            }
                        }
                    }
        
                    /* non-zero total matching score */
                    if (total > 0) {
        
                        /* convert matching scores to probabilities */
                        best_prob = 0;
                        for (R = 0; R < no_of_feats_right; R++) {
                            if (row_peaks[R] > 0) {
                                match_prob = row_peaks[R] * 1000 / total;
                                if (match_prob > best_prob) {
                                    best_prob = match_prob;
                                    bestR = R;
                                }
                            }
                        }
        
                        if ((best_prob > 0) && (best_prob < 1000)
                                && (no_of_possible_matches < SVS_MAX_FEATURES)) {
        
                            /* x coordinate of the feature in the right camera */
                            xR = other.feature_x[fR + bestR];
        
                            /* possible disparity */
                            disp = xL - xR;
        
                            if (disp >= -10) {
                                if (disp < 0)
                                    disp = 0;
                                /* add the best result to the list of possible matches */
                                svs_matches[no_of_possible_matches * 4] = best_prob;
                                svs_matches[no_of_possible_matches * 4 + 1]
                                        = (uint) xL;
                                svs_matches[no_of_possible_matches * 4 + 2]
                                        = (uint) y;
                                svs_matches[no_of_possible_matches * 4 + 3]
                                        = (uint) disp;
                                no_of_possible_matches++;
                            }
                        }
                    }
                }
        
                /* increment feature indexes */
                fL += no_of_feats_left;
                fR += no_of_feats_right;
            }
        
            // clear priors
            int priors_length = (int)(imgWidth * imgHeight / (16*SVS_VERTICAL_SAMPLING));
        
            if (no_of_possible_matches > 20) {
                for (int i=priors_length-1;i>=0;i--) disparity_priors[i]=0;
        
                /* filter the results */
                svs_filter_plane(no_of_possible_matches, max_disp);
        
                /* sort matches in descending order of probability */
                if (no_of_possible_matches < ideal_no_of_matches) {
                    ideal_no_of_matches = no_of_possible_matches;
                }
                curr_idx = 0;
                search_idx = 0;
                for (matches = 0; matches < ideal_no_of_matches; matches++, curr_idx
                        += 4) {
        
                    match_prob = svs_matches[curr_idx];
                    winner_idx = -1;
        
                    search_idx = curr_idx + 4;
                    max = no_of_possible_matches * 4;
                    while (search_idx < max) {
                        if (svs_matches[search_idx] > match_prob) {
                            match_prob = svs_matches[search_idx];
                            winner_idx = search_idx;
                        }
                        search_idx += 4;
                    }
                    if (winner_idx > -1) {
        
                        /* swap */
                        best_prob = svs_matches[winner_idx];
                        xL = (int)svs_matches[winner_idx + 1];
                        y = (int)svs_matches[winner_idx + 2];
                        disp = (int)svs_matches[winner_idx + 3];
        
                        svs_matches[winner_idx] = svs_matches[curr_idx];
                        svs_matches[winner_idx + 1] = svs_matches[curr_idx + 1];
                        svs_matches[winner_idx + 2] = svs_matches[curr_idx + 2];
                        svs_matches[winner_idx + 3] = svs_matches[curr_idx + 3];
        
                        svs_matches[curr_idx] = best_prob;
                        svs_matches[curr_idx + 1] = (uint)xL;
                        svs_matches[curr_idx + 2] = (uint)y;
                        svs_matches[curr_idx + 3] = (uint)disp;
        
                        /* update your priors */
                        row = y / SVS_VERTICAL_SAMPLING;
                        for (row_offset = -3; row_offset <= 3; row_offset++) {
                            for (col_offset = -1; col_offset <= 1; col_offset++) {
                                idx = (((row + row_offset) * (int)imgWidth + xL) / 16)
                                        + col_offset;
                                if ((idx > -1) && (idx < priors_length)) {
                                    if (disparity_priors[idx] == 0)
                                        disparity_priors[idx] = disp;
                                    else
                                        disparity_priors[idx] = (disp
                                                + disparity_priors[idx]) / 2;
                                }
                            }
                        }
                    }
        
                    if (svs_matches[curr_idx] == 0) {
                        break;
                    }
        
                }
        
                /* attempt to assign disparities to vertical features */
                for (int i = 0; i < SVS_MAX_MATCHES; i++) valid_quadrants[i]=0;
                itt = 0;
                prev_matches = matches;
                for (itt = 0; itt < 10; itt++) {
                    fL = 0;
                    col = 0;
                    for (x = 4; x < (int) imgWidth - 4; x += SVS_HORIZONTAL_SAMPLING, col++) {
        
                        no_of_feats = features_per_col[col];
        
                        /* features along the row in the left camera */
                        for (L = 0; L < no_of_feats; L++) {
        
                            if (valid_quadrants[fL + L] == 0) {
                                /* y coordinate of the feature in the left camera */
                                y = feature_y[fL + L];
        
                                /* lookup disparity from priors */
        
                                row = y / SVS_VERTICAL_SAMPLING;
                                disp_prior
                                        = disparity_priors[(row * imgWidth + x) / 16];
        
                                if ((disp_prior > 0) && (matches < SVS_MAX_MATCHES)) {
                                    curr_idx = matches * 4;
                                    svs_matches[curr_idx] = 1000;
                                    svs_matches[curr_idx + 1] = (uint)x;
                                    svs_matches[curr_idx + 2] = (uint)y;
                                    svs_matches[curr_idx + 3] = (uint)disp_prior;
                                    matches++;
        
                                    /* update your priors */
                                    for (row_offset = -3; row_offset <= 3; row_offset++) {
                                        for (col_offset = -1; col_offset <= 1; col_offset++) {
                                            idx = (((row + row_offset) * (int)imgWidth + x)
                                                    / 16) + col_offset;
                                            if ((idx > -1) && (idx < priors_length)) {
                                                if (disparity_priors[idx] == 0)
                                                    disparity_priors[idx] = disp_prior;
                                            }
                                        }
                                    }
        
                                    valid_quadrants[fL + L] = 1;
                                }
                            }
                        }
                        fL += no_of_feats;
                    }
                    if (prev_matches == matches)
                        break;
                    prev_matches = matches;
                }
            }
        
            return (matches);
        }

        /* filtering function removes noise by fitting planes to the disparities
           and disguarding outliers */
        private void svs_filter_plane(
            int no_of_possible_matches, /* the number of stereo matches */
            int max_disparity_pixels) /*maximum disparity in pixels */
        {
            int i, j, hf, hist_max, w = SVS_FILTER_SAMPLING, w2, n, horizontal = 0;
            int x, y, disp, tx = 0, ty = 0, bx = 0, by = 0;
            int hist_thresh, hist_mean, hist_mean_hits, mass, disp2;
            int min_ww, max_ww, m, ww, d;
            int ww0, ww1, disp0, disp1, cww, dww, ddisp;
            int max_hits;
            no_of_planes = 0;
        
            /* clear quadrants */
            for (i = no_of_possible_matches-1; i >= 0; i--)
                valid_quadrants[i] = 0;
        
            /* create disparity histograms within different
             * zones of the image */
            for (hf = 0; hf < 11; hf++) {
        
                switch (hf) {
        
                    // overall horizontal
                case 0: {
                    tx = 0;
                    ty = 0;
                    bx = (int)imgWidth;
                    by = (int)imgHeight;
                    horizontal = 1;
                    w = bx;
                    break;
                }
                // overall vertical
                case 1: {
                    tx = 0;
                    ty = 0;
                    bx = (int)imgWidth;
                    by = (int)imgHeight;
                    horizontal = 0;
                    w = by;
                    break;
                }
                // left hemifield 1
                case 2: {
                    tx = 0;
                    ty = 0;
                    bx = (int)imgWidth / 3;
                    by = (int)imgHeight;
                    horizontal = 1;
                    w = bx;
                    break;
                }
                // left hemifield 2
                case 3: {
                    tx = 0;
                    ty = 0;
                    bx = (int)imgWidth / 2;
                    by = (int)imgHeight;
                    horizontal = 1;
                    w = bx;
                    break;
                }
        
                // centre field (vertical)
                case 4: {
                    tx = (int)imgWidth / 3;
                    bx = (int)imgWidth * 2 / 3;
                    w = (int)imgHeight;
                    horizontal = 0;
                    break;
                }
                // centre above field
                case 5: {
                    tx = (int)imgWidth / 3;
                    ty = 0;
                    bx = (int)imgWidth * 2 / 3;
                    by = (int)imgHeight / 2;
                    w = by;
                    horizontal = 0;
                    break;
                }
                // centre below field
                case 6: {
                    tx = (int)imgWidth / 3;
                    ty = (int)imgHeight / 2;
                    bx = (int)imgWidth * 2 / 3;
                    by = (int)imgHeight;
                    w = ty;
                    horizontal = 0;
                    break;
                }
                // right hemifield 0
                case 7: {
                    tx = (int)imgWidth * 2 / 3;
                    bx = (int)imgWidth;
                    horizontal = 1;
                    break;
                }
                // right hemifield 1
                case 8: {
                    tx = (int)imgWidth / 2;
                    bx = (int)imgWidth;
                    w = tx;
                    break;
                }
                // upper hemifield
                case 9: {
                    tx = 0;
                    ty = 0;
                    bx = (int)imgWidth;
                    by = (int)imgHeight / 2;
                    horizontal = 0;
                    w = by;
                    break;
                }
                // lower hemifield
                case 10: {
                    ty = by;
                    by = (int)imgHeight;
                    horizontal = 0;
                    break;
                }
                }
        
                /* clear the histogram */
                w2 = w / SVS_FILTER_SAMPLING;
                if (w2 < 1) w2 = 1;
                for (i = w2 * max_disparity_pixels - 1; i >= 0; i--) disparity_histogram_plane[i] = 0;
                for (i = w2 - 1; i >= 0; i--) disparity_plane_fit[i] = 0;
                hist_max = 0;
        
                /* update the disparity histogram */
                n = 0;
                for (i = no_of_possible_matches-1; i >= 0; i--) {
                    x = (int)svs_matches[i * 4 + 1];
                    if ((x > tx) && (x < bx)) {
                        y = (int)svs_matches[i * 4 + 2];
                        if ((y > ty) && (y < by)) {
                            disp = (int)svs_matches[i * 4 + 3];
                            if ((int) disp < max_disparity_pixels) {
                                if (horizontal != 0) {
                                    n = (((x - tx) / SVS_FILTER_SAMPLING)
                                         * max_disparity_pixels) + disp;
                                } else {
                                    n = (((y - ty) / SVS_FILTER_SAMPLING)
                                         * max_disparity_pixels) + disp;
                                }
                                disparity_histogram_plane[n]++;
                                if (disparity_histogram_plane[n] > hist_max)
                                    hist_max = disparity_histogram_plane[n];
                            }
                        }
                    }
                }
        
                /* find peak disparities along a range of positions */
                hist_thresh = hist_max / 4;
                hist_mean = 0;
                hist_mean_hits = 0;
                disp2 = 0;
                min_ww = w2;
                max_ww = 0;
                for (ww = 0; ww < (int) w2; ww++) {
                    mass = 0;
                    disp2 = 0;
                    for (d = 1; d < max_disparity_pixels - 1; d++) {
                        n = ww * max_disparity_pixels + d;
                        if (disparity_histogram_plane[n] > hist_thresh) {
                            m = disparity_histogram_plane[n]
                                + disparity_histogram_plane[n - 1]
                                + disparity_histogram_plane[n + 1];
                            mass += m;
                            disp2 += m * d;
                        }
                        if (disparity_histogram_plane[n] > 0) {
                            hist_mean += disparity_histogram_plane[n];
                            hist_mean_hits++;
                        }
                    }
                    if (mass > 0) {
                        // peak disparity at this position
                        disparity_plane_fit[ww] = disp2 / mass;
                        if (min_ww == (int) w2)
                            min_ww = ww;
                        if (ww > max_ww)
                            max_ww = ww;
                    }
                }
                if (hist_mean_hits > 0)
                    hist_mean /= hist_mean_hits;
        
                /* fit a line to the disparity values */
                ww0 = 0;
                ww1 = 0;
                disp0 = 0;
                disp1 = 0;
                int hits0,hits1;
                if (max_ww >= min_ww) {
                    cww = min_ww + ((max_ww - min_ww) / 2);
                    hits0 = 0;
                    hits1 = 0;
                    for (ww = min_ww; ww <= max_ww; ww++) {
                        if (ww < cww) {
                            disp0 += disparity_plane_fit[ww];
                            ww0 += ww;
                            hits0++;
                        } else {
                            disp1 += disparity_plane_fit[ww];
                            ww1 += ww;
                            hits1++;
                        }
                    }
                    if (hits0 > 0) {
                        disp0 /= hits0;
                        ww0 /= hits0;
                    }
                    if (hits1 > 0) {
                        disp1 /= hits1;
                        ww1 /= hits1;
                    }
                }
                dww = ww1 - ww0;
                ddisp = disp1 - disp0;
        
                /* find inliers */
                int plane_tx = (int)imgWidth;
                int plane_ty = 0;
                int plane_bx = (int)imgHeight;
                int plane_by = 0;
                int plane_disp0 = 0;
                int plane_disp1 = 0;
                int hits = 0;
                for (i = no_of_possible_matches-1; i >= 0; i--) {
                    x = (int)svs_matches[i * 4 + 1];
                    if ((x > tx) && (x < bx)) {
                        y = (int)svs_matches[i * 4 + 2];
                        if ((y > ty) && (y < by)) {
                            disp = (int)svs_matches[i * 4 + 3];
        
                            if (horizontal != 0) {
                                ww = (x - tx) / SVS_FILTER_SAMPLING;
                                n = ww * max_disparity_pixels + disp;
                            } else {
                                ww = (y - ty) / SVS_FILTER_SAMPLING;
                                n = ww * max_disparity_pixels + disp;
                            }
        
                            if (dww > 0) {
                                disp2 = disp0 + ((ww - ww0) * ddisp / dww);
                            }
                            else {
                                disp2 = disp0;
                            }
        
                            /* check how far this is from the plane */
                            if (((int)disp >= disp2-2) &&
                                    ((int)disp <= disp2+2) &&
                                    ((int)disp < max_disparity_pixels)) {
        
                                /* inlier detected - this disparity lies along the plane */
                                valid_quadrants[i]++;
                                hits++;
                                
                                /* keep note of the bounds of the plane */
                                if (x < plane_tx) {
                                    plane_tx = x;
                                    if (horizontal == 1) plane_disp0 = disp2;
                                }
                                if (y < plane_ty) {
                                    plane_ty = y;
                                    if (horizontal == 0) plane_disp0 = disp2;
                                }
                                if (x > plane_bx) {
                                    plane_bx = x;
                                    if (horizontal == 1) plane_disp1 = disp2;
                                }
                                if (y > plane_by) {
                                    plane_by = y;
                                    if (horizontal == 0) plane_disp1 = disp2;
                                }
                            }
                        }
                    }
                }
                if (hits > 5) {
                    /* add a detected plane */
                    plane[no_of_planes*9+0]=plane_tx;
                    plane[no_of_planes*9+1]=plane_ty;
                    plane[no_of_planes*9+2]=plane_bx;
                    plane[no_of_planes*9+3]=plane_by;
                    plane[no_of_planes*9+4]=horizontal;
                    plane[no_of_planes*9+5]=plane_disp0;
                    plane[no_of_planes*9+6]=plane_disp1;
                    plane[no_of_planes*9+7]=hits;
                    plane[no_of_planes*9+8]=0;
                    no_of_planes++;
                }
            }
        
            /* deal with the outliers */
            for (i = no_of_possible_matches-1; i >= 0; i--) {
                if (valid_quadrants[i] == 0) {
                
                    /* by default set outlier probability to zero,
                       which eliminates it from further enquiries */
                    svs_matches[i * 4] = 0;
        
                    /* if the point is within a known plane region then force
                       its disparity onto the plane */
                    x = (int)svs_matches[i * 4 + 1];
                    y = (int)svs_matches[i * 4 + 2];
                    max_hits = 0;
                    for (j = no_of_planes-1; j >= 0; j--) {
                        if ((x > plane[j*9+0]) &&
                                (x < plane[j*9+2]) &&
                                (y > plane[j*9+1]) &&
                                (y < plane[j*9+3])) {
        
                            if (max_hits < plane[j*9+7]) {
                                                
                                max_hits = plane[j*9+7];
                                
                                /* find the disparity value at this point on the plane */
                                if (plane[j*9+4] == 1) {
                                
                                    disp = plane[j*9+5] +
                                           ((x - plane[j*9+0]) *
                                            (plane[j*9+6] - plane[j*9+5]) /
                                            (plane[j*9+2] - plane[j*9+0]));
                                }
                                else {
        
                                    disp = plane[j*9+5] +
                                            ((y - plane[j*9+1]) *
                                             (plane[j*9+6] - plane[j*9+5]) /
                                             (plane[j*9+3] - plane[j*9+1]));
                                }
                                
                                /* ignore big disparities, which are likely to be noise */
                                if (disp < 4) { 
                                    /* update disparity for this stereo match */
                                    svs_matches[i * 4 + 3] = (uint)disp;
                                    
                                    /* non zero match probability resurects this stereo match */
                                    svs_matches[i * 4] = 1; 
                                }
                            }
                        }
                    }
                }
            }
        }
        
        
    }
}
