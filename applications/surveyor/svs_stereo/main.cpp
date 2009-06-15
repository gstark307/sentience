/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  main.c - test application for stereo correspondence
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

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "stereo.h"
#include "bitmap.h"
#include "fileio.h"
#include "drawing.h"

unsigned int imgWidth = 320;
unsigned int imgHeight = 240;

/* Two structures are created:
 * SVS_data stores features obtained from this camera
 * SVS_data_received stores features received from the opposite camera */
struct SVS_data_struct {

	/* array storing x coordinates of detected features */
	short int feature_x[SVS_MAX_FEATURES];

	/* array storing the number of features detected on each row */
	unsigned short int features_per_row[SVS_MAX_IMAGE_HEIGHT/SVS_VERTICAL_SAMPLING];

	/* Array storing a binary descriptor, 32bits in length, for each detected feature.
	 * This will be used for matching purposes.*/
	unsigned int descriptor[SVS_MAX_FEATURES];

	/* mean luminance for each feature */
	unsigned char mean[SVS_MAX_FEATURES];

} svs_data, svs_data_received;

/* buffer which stores sliding sum */
int row_sum[SVS_MAX_IMAGE_WIDTH];

/* peaks along the row */
unsigned int row_peaks[SVS_MAX_IMAGE_WIDTH];

/* array stores matching probabilities */
unsigned int svs_matches[SVS_MAX_FEATURES*4];

/* copy the data from one structure to the other */
void copy_to_received() {
	memcpy(svs_data_received.feature_x, svs_data.feature_x, SVS_MAX_FEATURES * sizeof(short int));
	memcpy(svs_data_received.features_per_row, svs_data.features_per_row, (SVS_MAX_IMAGE_HEIGHT/SVS_VERTICAL_SAMPLING) * sizeof(unsigned short int));
	memcpy(svs_data_received.descriptor, svs_data.descriptor, SVS_MAX_FEATURES * sizeof(unsigned int));
	memcpy(svs_data_received.mean, svs_data.mean, SVS_MAX_FEATURES * sizeof(unsigned char));
}

/* draw a binary patch descriptor */
void draw_descriptor(
    int px,
    int py,
	int descriptor_index,
	unsigned char* rectified_frame_buf) {

	int i, idx, bit = 1;
	unsigned int desc;

	/* offsets of pixels to be compared within the patch region
	 * arranged into a bresenham ring structure */
	const int pixel_offsets[] = {
		-2,-2,  -2,-3,  -1,-3,   0,-3,   1,-3,   2,-3,   2,-2,
		 3,-2,   3,-1,   3, 0,   3, 1,   3, 2,   2, 2,
		 2, 3,   1, 3,   0, 3,  -1, 3,  -2, 3,  -2, 2,
		-3, 2,  -3, 1,  -3, 0,  -3,-1,  -3,-2,
		 0,-2,   2, 0,   0,-2,  -2, 0
	};

	desc = svs_data.descriptor[descriptor_index];
	for (i = 0; i < SVS_DESCRIPTOR_PIXELS; i++, bit *= 2) {
		idx = pixindex((px + pixel_offsets[i*2]), (py + pixel_offsets[i*2+1]));
		if (desc & bit) {
			rectified_frame_buf[idx] = 0;
			rectified_frame_buf[idx + 1] = 255;
			rectified_frame_buf[idx + 2] = 0;
		}
		else {
			rectified_frame_buf[idx] = 0;
			rectified_frame_buf[idx + 1] = 0;
			rectified_frame_buf[idx + 2] = 255;
		}
	}
}


int main() {

	printf("frame size %d bytes\n", sizeof(svs_data));

	bool show_descriptors = false;
	bool disparity_spots = false;
	std::string left_image_filename = "left3.bmp";
	//std::string left_image_filename = "test2.bmp";
	std::string left_features_filename = "left_feats.ppm";
	std::string right_image_filename = "right3.bmp";
	std::string right_features_filename = "right_feats.ppm";
	std::string matched_features_filename = "matches.ppm";
	std::string matched_features_two_images_filename = "matches_two.ppm";
	int cam;
    int no_of_feats = 0;

	/* feature detection params */
	int calibration_offset_x = 5;
	int calibration_offset_y = 6;
    int inhibition_radius = 16;
    unsigned int minimum_response = 180;

    /* matching params */
	int ideal_no_of_matches = 200;
	int max_disparity_percent = 20;
	int descriptor_match_threshold = SVS_DESCRIPTOR_PIXELS * 10 / 100;
	int learnDesc = 5;
	int learnLuma = 2;
	int learnDisp = 1;

	if ((fileio::FileExists(left_image_filename)) &&
		(fileio::FileExists(right_image_filename))) {

		Bitmap* bmp_left = new Bitmap();
		bmp_left->FromFile(left_image_filename);
		Bitmap* bmp_right = new Bitmap();
		bmp_right->FromFile(right_image_filename);

		unsigned char* rectified_frame_buf;
		unsigned char* img_matches = new unsigned char[imgWidth * imgHeight * 3];
		unsigned char* img_matches_two_images = new unsigned char[imgWidth * imgHeight * 2 * 3];

		for (int n = 0; n < (int)(imgWidth*imgHeight*3); n++) {
			img_matches_two_images[n] = bmp_left->Data[n];
			img_matches_two_images[n+(imgWidth*imgHeight*3)] = bmp_right->Data[n];
		}

		for (cam = 1; cam >= 0; cam--) {

			if (cam == 0) {
				rectified_frame_buf = bmp_left->Data;
				imgWidth = bmp_left->Width;
				imgHeight = bmp_left->Height;
			}
			else {
				rectified_frame_buf = bmp_right->Data;
				imgWidth = bmp_right->Width;
				imgHeight = bmp_right->Height;
			}

			no_of_feats = svs_get_features(
		        rectified_frame_buf,
		        inhibition_radius,
		        minimum_response,
		        calibration_offset_x,
		        calibration_offset_y);

			printf("cam %d:  %d\n", cam, no_of_feats);

			if (cam == 1)
				copy_to_received();
			else
				memcpy(img_matches, rectified_frame_buf, imgWidth*imgHeight*3*sizeof(unsigned char));

			/* display the features */
			int row = 0;
			int feats_remaining = svs_data.features_per_row[row];

			for (int f = 0; f < no_of_feats; f++, feats_remaining--) {

				int x = (int)svs_data.feature_x[f] - calibration_offset_x;
				int y = 4 + (row * SVS_VERTICAL_SAMPLING) + calibration_offset_y;

				if (show_descriptors) {
				    draw_descriptor(x, y, f, rectified_frame_buf);
				}
				else {
				    drawing::drawCross(rectified_frame_buf, imgWidth, imgHeight, x, y, 2, 0, 255, 0, 0);
				}

				/* move to the next row */
				if (feats_remaining == 0) {
					row++;
					feats_remaining = svs_data.features_per_row[row];
				}
			}

			calibration_offset_x = 0;
			calibration_offset_y = 0;
		}

		bmp_left->SavePPM(left_features_filename.c_str());
		bmp_right->SavePPM(right_features_filename.c_str());
		delete bmp_left;
		delete bmp_right;

		int matches = svs_match(
			ideal_no_of_matches,
			max_disparity_percent,
			descriptor_match_threshold,
			learnDesc,
			learnLuma,
			learnDisp);
		printf("matches = %d\n", matches);

		/* show disparity as spots */
		for (int i = 0; i < matches; i++) {
			int x = svs_matches[i*4 + 1];
			int y = svs_matches[i*4 + 2];
			int disp = svs_matches[i*4 + 3];
			drawing::drawSpot(img_matches, imgWidth, imgHeight, x, y, disp/8, 0, 255, 0);
		}

		/* show disparity as lines */
		for (int i = 0; i < matches; i+= matches/30) {
			int x = svs_matches[i*4 + 1];
			int r=0,g=0,b=0;
			switch((x/4) % 6) {
				case 0: {
					r = 255;
					g = 0;
					b = 0;
					break;
				}
				case 1: {
					r = 0;
					g = 255;
					b = 0;
					break;
				}
				case 2: {
					r = 0;
					g = 0;
					b = 255;
					break;
				}
				case 3: {
					r = 255;
					g = 0;
					b = 255;
					break;
				}
				case 4: {
					r = 255;
					g = 255;
					b = 0;
					break;
				}
				case 5: {
					r = 0;
					g = 255;
					b = 255;
					break;
				}
			}
			int y = svs_matches[i*4 + 2];
			int disp = svs_matches[i*4 + 3];
			int x2 = x - disp;
			int y2 = imgHeight + y - calibration_offset_y;
			drawing::drawLine(img_matches_two_images, imgWidth, imgHeight*2, x,y, x2, y2, r,g,b,0,false);
			drawing::drawCircle(img_matches_two_images, imgWidth, imgHeight*2, x,y, 2, r,g,b, 0);
			drawing::drawCircle(img_matches_two_images, imgWidth, imgHeight*2, x2,y2, 2, r,g,b, 0);
		}

		Bitmap* bmp_matches = new Bitmap(img_matches, imgWidth, imgHeight, 3);
		bmp_matches->SavePPM(matched_features_filename.c_str());
		delete[] img_matches;
		delete bmp_matches;

		Bitmap* bmp_matches_two_images = new Bitmap(img_matches_two_images, imgWidth, imgHeight*2, 3);
		bmp_matches_two_images->SavePPM(matched_features_two_images_filename.c_str());
		delete[] img_matches_two_images;
		delete bmp_matches_two_images;
	}
	else {
		printf("File not found\n");
	}

	printf("Exit success");
	return(0);
}
