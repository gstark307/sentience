#ifndef STEREO_H_
#define STEREO_H_

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define STEREO_MAX_FEATURES         2000
#define STEREO_MAX_IMAGE_WIDTH      1024
#define STEREO_MAX_IMAGE_HEIGHT     1024
#define STEREO_VERTICAL_SAMPLING    6
#define STEREO_DESCRIPTOR_PIXELS    24

#define pixindex(xx, yy)  ((yy * imgWidth + xx) * 3)
//#define pixindex(xx, yy)  ((yy * imgWidth + xx) * 2) & 0xFFFFFFFC  // always a multiple of 4

extern int stereo_update_sums(int y, unsigned char* rectified_frame_buf);
extern void stereo_non_max(int inhibition_radius, unsigned int min_response);
extern int stereo_compute_descriptor(int px, int py, unsigned char* rectified_frame_buf, int no_of_features, int row_mean);
extern int stereo_get_features(unsigned char* rectified_frame_buf, int inhibition_radius, unsigned int minimum_response, int calibration_offset_x, int calibration_offset_y);
extern int stereo_match(int ideal_no_of_matches, int max_disparity_percent, int luminance_threshold, int descriptor_match_threshold);

#endif
