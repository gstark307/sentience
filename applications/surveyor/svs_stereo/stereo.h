#ifndef STEREO_H_
#define STEREO_H_

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* are we running on the blackfin or on a PC ? */
//#define SVS_EMBEDDED

#define SVS_MAX_FEATURES         2000
#define SVS_MAX_IMAGE_WIDTH      1280
#define SVS_MAX_IMAGE_HEIGHT     1024
#define SVS_VERTICAL_SAMPLING    2
#define SVS_DESCRIPTOR_PIXELS    30

#define pixindex(xx, yy)  ((yy * imgWidth + xx) * 3)

extern int svs_update_sums(int y, unsigned char* rectified_frame_buf);
extern void svs_non_max(int inhibition_radius, unsigned int min_response);
extern int svs_compute_descriptor(int px, int py, unsigned char* rectified_frame_buf, int no_of_features, int row_mean);
extern int svs_get_features(unsigned char* rectified_frame_buf, int inhibition_radius, unsigned int minimum_response, int calibration_offset_x, int calibration_offset_y);
extern int svs_match(int ideal_no_of_matches, int max_disparity_percent, int descriptor_match_threshold, int learnDesc, int learnLuma, int learnDisp);

extern void svs_filter(int no_of_possible_matches, int max_disparity_pixels, int tolerance);
extern void svs_rectify(unsigned char* raw_image, unsigned char* rectified_frame_buf);

#ifdef SVS_EMBEDDED
void svs_master(unsigned short *outbuf16, unsigned short *inbuf16, int bufsize);
void svs_slave(unsigned short *inbuf16, unsigned short *outbuf16, int bufsize);
extern int svs_grab(int calibration_offset_x, int calibration_offset_y);
#endif

#endif
