/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  stereo.c - image processing routines for the SRV-1 Blackfin robot.
 *    Copyright (C) 2009  Surveyor Corporation
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details (www.gnu.org/licenses)
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include "stereo.h"

extern unsigned int imgWidth, imgHeight;

/* Two structures are created:
 * stereo_data stores features obtained from this camera
 * stereo_data_received stores features received from the opposite camera */
extern struct stereo_data_struct {

	/* array storing x coordinates of detected features */
	short int feature_x[STEREO_MAX_FEATURES];

	/* array storing the number of features detected on each row */
	unsigned short int features_per_row[STEREO_MAX_IMAGE_HEIGHT/STEREO_VERTICAL_SAMPLING];

	/* Array storing a binary descriptor, 32bits in length, for each detected feature.
	 * This will be used for matching purposes.*/
	unsigned int descriptor[STEREO_MAX_FEATURES];

	/* mean luminance for each feature */
	unsigned char mean[STEREO_MAX_FEATURES];

} stereo_data, stereo_data_received;

/* buffer which stores sliding sum */
extern int row_sum[STEREO_MAX_IMAGE_WIDTH];

/* buffer used to find peaks in edge space */
extern unsigned int row_peaks[STEREO_MAX_IMAGE_WIDTH];

/* array stores matching probabilities (prob,x,y,disp) */
extern unsigned int stereo_matches[STEREO_MAX_FEATURES*4];

/* offsets of pixels to be compared within the patch region
 * arranged into a bresenham ring structure */
const int pixel_offsets[] = {
	-2,-2,  -2,-3,  -1,-3,   0,-3,   1,-3,   2,-3,   2,-2,
	 3,-2,   3,-1,   3, 0,   3, 1,   3, 2,   2, 2,
	 2, 3,   1, 3,   0, 3,  -1, 3,  -2, 3,  -2, 2,
	-3, 2,  -3, 1,  -3, 0,  -3,-1,  -3,-2
};

/* lookup table used for counting the number of set bits */
const unsigned char BitsSetTable256[] =
{
  0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4,
  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
  4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
};


/* Updates sum of squared difference values along a single row
 * Returns the mean luminance along the row */
int stereo_update_sums(
    int y,                                /* row index */
    unsigned char* rectified_frame_buf) { /* image data */

	int x, idx, mean=0;
    unsigned int v;

    /* compute sums along the row */
    row_sum[0] =
    	rectified_frame_buf[2] +
        rectified_frame_buf[1] +
        rectified_frame_buf[0];
    for (x = 1; x < (int)imgWidth; x++) {
    	idx = pixindex(x, y);
    	v = rectified_frame_buf[idx + 2] +
    	    rectified_frame_buf[idx + 1] +
    	    rectified_frame_buf[idx];
    	row_sum[x] = row_sum[x-1] + v;
    }

    /* row mean luminance */
    mean = row_sum[x-1] / (((int)imgWidth-1)*3);

    /* compute peaks */
    int p0, p1;
    for (x = 4; x < (int)imgWidth-5; x++) {

    	p0 = (row_sum[x] - row_sum[x - 2]) -
    		 (row_sum[x + 2] - row_sum[x]);
    	if (p0 < 0) p0 = -p0;

    	p1 = (row_sum[x] - row_sum[x - 4]) -
    		 (row_sum[x + 4] - row_sum[x]);
    	if (p1 < 0) p1 = -p1;

    	row_peaks[x] = p0 + p1;
    }

    return(mean);
}

/* performs non-maximal suppression on the given row */
void stereo_non_max(
    int inhibition_radius, /* radius for non-maximal suppression */
    unsigned int min_response) {  /* minimum threshold as a percent in the range 0-200 */

    int x, r;
    unsigned int v;

    /* average response */
    unsigned int av_peaks = 0;
    for (x = 4; x < (int)imgWidth - 5; x++) {
      	av_peaks += row_peaks[x];
    }
    av_peaks /= (imgWidth - 9);

    /* adjust the threshold */
    av_peaks = av_peaks * min_response / 100;

	for (x = 0; x < (int)imgWidth - inhibition_radius; x++) {

		if (row_peaks[x] < av_peaks) row_peaks[x] = 0;
		v = row_peaks[x];
		if (v > 0) {
			for (r = 1; r < inhibition_radius; r++) {
				if (row_peaks[x + r] < v) {
					row_peaks[x + r] = 0;
				}
				else {
					row_peaks[x] = 0;
					r = inhibition_radius;
				}
			}
		}
	}
}

/* creates a binary descriptor for a feature at the given coordinate
   which can subsequently be used for matching */
int stereo_compute_descriptor(
    int px,
    int py,
    unsigned char* rectified_frame_buf,
    int no_of_features,
    int row_mean) {

	unsigned char bit_count = 0;
	int pixel_offset_idx, ix, bit;
	int mean = 0;
	unsigned int desc = 0;

	/* find the mean luminance for the patch */
	for (pixel_offset_idx = 0; pixel_offset_idx < STEREO_DESCRIPTOR_PIXELS*2; pixel_offset_idx += 2) {

		ix = rectified_frame_buf[pixindex((px + pixel_offsets[pixel_offset_idx]), (py + pixel_offsets[pixel_offset_idx + 1]))];
		mean += rectified_frame_buf[ix + 2] +
			    rectified_frame_buf[ix + 1] +
				rectified_frame_buf[ix];
	}
	mean /= STEREO_DESCRIPTOR_PIXELS;

	/* binarise */
	bit = 1;
	for (pixel_offset_idx = 0; pixel_offset_idx < STEREO_DESCRIPTOR_PIXELS*2; pixel_offset_idx += 2, bit *= 2) {

		ix = rectified_frame_buf[pixindex((px + pixel_offsets[pixel_offset_idx]), (py + pixel_offsets[pixel_offset_idx + 1]))];
		if (rectified_frame_buf[ix + 2] +
			rectified_frame_buf[ix + 1] +
			rectified_frame_buf[ix] > mean) {
		    desc |= bit;
		    bit_count++;
		}
	}

	if ((bit_count > 3) &&
		(bit_count < STEREO_DESCRIPTOR_PIXELS-3)) {
		mean /= 3;

		/* adjust the patch luminance relative to the mean
		 * luminance for the entire row.  This helps to ensure
		 * that comparisons between left and right images are
		 * fair even if there are exposure/illumination differences. */
		mean = mean - row_mean + 127;
		if (mean < 0) mean = 0;
		if (mean > 255) mean = 255;

        stereo_data.mean[no_of_features] = (unsigned char)(mean/3);
        stereo_data.descriptor[no_of_features] = desc;
        return(0);
	}
	else {
		/* probably just noise */
		return(-1);
	}
}

/* returns a set of features suitable for stereo matching */
int stereo_get_features(
    unsigned char* rectified_frame_buf,  /* image data */
    int inhibition_radius,               /* radius for non-maximal supression */
    unsigned int minimum_response,       /* minimum threshold */
    int calibration_offset_x,
    int calibration_offset_y) {

	unsigned short int no_of_feats;
    int x, y, row_mean;
    int no_of_features = 0;
    int row_idx = 0;

    for (y = 4 + calibration_offset_y; y < (int)imgHeight - 4; y += STEREO_VERTICAL_SAMPLING) {

        /* reset number of features on the row */
        no_of_feats = 0;

        if ((y >= 4) && (y <= (int)imgHeight - 4)) {

        	row_mean = stereo_update_sums(y, rectified_frame_buf);
            stereo_non_max(inhibition_radius, minimum_response);

            /* store the features */
            for (x = imgWidth - 1 - inhibition_radius; x > 0; x--) {
                if (row_peaks[x] > 0) {

            	    if (stereo_compute_descriptor(x, y, rectified_frame_buf, no_of_features, row_mean) == 0) {
						stereo_data.feature_x[no_of_features++] = (short int)(x + calibration_offset_x);
						no_of_feats++;
						if (no_of_features == STEREO_MAX_FEATURES) {
							y = imgHeight;
							printf("stereo feature buffer full\n");
							break;
						}
            	    }
                }
            }
        }

        stereo_data.features_per_row[row_idx++] = no_of_feats;
    }
    return(no_of_features);
}

/* Match features from this camera with features from the opposite one.
 * It is assumed that matching is performed on the left camera CPU */
int stereo_match(
	int ideal_no_of_matches,          /* ideal number of matches to be returned */
	int max_disparity_percent,        /* max disparity as a percent of image width */
	int luminance_threshold,          /* threshold used when matching patch luminances, in the range 0-255 */
	int descriptor_match_threshold) { /* minimum no of descriptor bits to be matched, in the range 1 - STEREO_DESCRIPTOR_PIXELS */

	int xL, xR, L, R, y, no_of_feats_left, no_of_feats_right, row, bit;
	int luma_diff, max_disp, meanL, meanR, disp, fL=0, fR=0, bestR;
	unsigned int descL, descLanti, descR, desc_match;
	unsigned int correlation, anticorrelation, total, n;
	unsigned int match_prob, best_prob;
	int i, max, curr_idx, search_idx;
	int no_of_possible_matches = 0, matches = 0;

	/* convert max disparity from percent to pixels */
	max_disp = max_disparity_percent * imgWidth / 100;

	row = 0;
	for (y = 4; y < (int)imgHeight - 4; y += STEREO_VERTICAL_SAMPLING, row++) {

		/* number of features on left and right rows */
        no_of_feats_left = stereo_data.features_per_row[row];
        no_of_feats_right = stereo_data_received.features_per_row[row];

        //printf("feats %d  %d\n", no_of_feats_left, no_of_feats_right);

        /* features along the row in the left camera */
        for (L = 0; L < no_of_feats_left; L++) {

        	/* x coordinate of the feature in the left camera */
        	xL = stereo_data.feature_x[fL + L];

        	/* mean luminance and descriptor for the left camera feature */
        	meanL = stereo_data.mean[fL + L];
        	descL = stereo_data.descriptor[fL + L];

        	/* invert bits of the descriptor for anti-correlation matching */
        	n = descL;
      	    descLanti = 0;
       	    for (bit = 0; bit < 32; bit++) {
       	        /* Shift result vector to higher significance. */
       	    	descLanti <<= 1;
       	        /* Get least significant input bit. */
       	    	descLanti |= n & 1;
       	        /* Shift input vector to lower significance. */
       	        n >>= 1;
       	    }

       	    total = 0;

        	/* features along the row in the right camera */
            for (R = 0; R < no_of_feats_right; R++) {

            	/* set matching score to zero */
            	row_peaks[R] = 0;

            	/* x coordinate of the feature in the right camera */
            	xR = stereo_data_received.feature_x[fR + R];

            	/* compute disparity */
            	disp = xL - xR;

            	/* is the disparity within range? */
            	if ((disp > 0) && (disp < max_disp)) {

                	/* mean luminance for the right camera feature */
            	    meanR = stereo_data_received.mean[fR + R];

            	    /* is the mean luminance similar? */
            	    luma_diff = meanR - meanL;
            	    if ((luma_diff > -luminance_threshold) &&
            	    	(luma_diff < luminance_threshold)) {

            	    	/* right camera feature descriptor */
                	    descR = stereo_data_received.descriptor[fR + R];

            	    	/* bitwise descriptor correlation match */
            	    	desc_match = descL & descR;

            	    	/* count the number of correlation bits */
            	    	correlation =
            	    		BitsSetTable256[desc_match & 0xff] +
            	    	    BitsSetTable256[(desc_match >> 8) & 0xff] +
            	    	    BitsSetTable256[(desc_match >> 16) & 0xff] +
            	    	    BitsSetTable256[desc_match >> 24];

            	    	/* were enough bits matched ? */
            	    	if ((int)correlation > descriptor_match_threshold) {

							/* bitwise descriptor anti-correlation match */
							desc_match = descLanti & descR;

							/* count the number of anti-correlation bits */
							anticorrelation =
								BitsSetTable256[desc_match & 0xff] +
								BitsSetTable256[(desc_match >> 8) & 0xff] +
								BitsSetTable256[(desc_match >> 16) & 0xff] +
								BitsSetTable256[desc_match >> 24];


							if (luma_diff < 0) luma_diff = -luma_diff;
							int score = (int)correlation + (int)(STEREO_DESCRIPTOR_PIXELS - anticorrelation) - luma_diff;
							if (score < 0) score = 0;

							/* store overall matching score */
							row_peaks[R] = (unsigned int)score;
							total += row_peaks[R];
            	    	}
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

                if ((best_prob > 0) &&
                	(no_of_possible_matches < STEREO_MAX_FEATURES)) {

                	/* x coordinate of the feature in the right camera */
                	xR = stereo_data_received.feature_x[fR + bestR];

                	/* possible disparity */
                	disp = xL - xR;

                    /* add the best result to the list of possible matches */
                	stereo_matches[no_of_possible_matches*4] = best_prob;
                	stereo_matches[no_of_possible_matches*4 + 1] = (unsigned int)xL;
                	stereo_matches[no_of_possible_matches*4 + 2] = (unsigned int)y;
                	stereo_matches[no_of_possible_matches*4 + 3] = (unsigned int)disp;
                	no_of_possible_matches++;
                }
            }
        }

        /* increment feature indexes */
        fL += no_of_feats_left;
        fR += no_of_feats_right;
	}

	if (no_of_possible_matches > 1) {

	    /* sort matches in descending order of probability */
        if (no_of_possible_matches < ideal_no_of_matches) {
        	ideal_no_of_matches = no_of_possible_matches;
        }
        curr_idx = 0;
        search_idx = 0;
        for (matches = 0; matches < ideal_no_of_matches; matches++, curr_idx += 4) {

        	match_prob =  stereo_matches[curr_idx];
        	int winner = -1;

        	//printf("prob = %d\n", match_prob);

        	search_idx = curr_idx + 4;
        	max = no_of_possible_matches * 4;
        	while (search_idx < max) {
        		if (stereo_matches[search_idx] > match_prob) {
        			winner = i;
        		}
        		search_idx += 4;
        	}
        	if (winner > -1) {

        		/* swap */
        		int winner_idx = winner * 4;
        		best_prob = stereo_matches[winner_idx];
        		xL = stereo_matches[winner_idx + 1];
        		y = stereo_matches[winner_idx + 2];
        		disp = stereo_matches[winner_idx + 3];

        		stereo_matches[winner_idx] = stereo_matches[curr_idx];
        		stereo_matches[winner_idx + 1] = stereo_matches[curr_idx + 1];
        		stereo_matches[winner_idx + 2] = stereo_matches[curr_idx + 2];
        		stereo_matches[winner_idx + 3] = stereo_matches[curr_idx + 3];

        		stereo_matches[curr_idx] = best_prob;
        		stereo_matches[curr_idx + 1] = xL;
        		stereo_matches[curr_idx + 2] = y;
        		stereo_matches[curr_idx + 3] = disp;
        	}
        }
	}
	return(matches);
}
