/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  stereo.c - SVS (stereo vision system) functions
 *    Copyright (C) 2005-2009  Surveyor Corporation
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

#ifdef SVS_EMBEDDED

#include <cdefBF537.h>
#include "config.h"
#include "uart.h"
#include "print.h"
#include "xmodem.h"
#include "srv.h"

void svs_master(unsigned short *outbuf16, unsigned short *inbuf16, int bufsize)
{
    int remainingBytes;
    unsigned short ix, dummy;

    *pPORTF_FER |= (PF14|PF13|PF12|PF11|PF10);  // SLCK, MISO, MOSI perhipheral
    *pPORT_MUX |= PJSE;  // Enable PJ10 (SSEL2/3) PORT_MUX PJSE=1
    *pSPI_BAUD = 4;
    *pSPI_FLG = FLS3;    // set FLS3 (FLG3 not required with CHPA=0
    *pSPI_CTL = SPE|MSTR|CPOL|SIZE|EMISO|0x00;  // we use CPHA=0 hardware control
    SSYNC;

    remainingBytes = bufsize;

    ix = (unsigned int)crc16_ccitt((void *)outbuf16, bufsize-8);
    outbuf16[(bufsize/2)-2] = ix; // write CRC into buffer

    *pSPI_TDBR = *outbuf16++;
    dummy = *pSPI_RDBR;  // slave will not have written yet
    remainingBytes -= 2;
    SSYNC;

    while(remainingBytes)
    {
        while((*pSPI_STAT&SPIF) ==0 )
            ; // ensure spi tranfer complete
        while((*pSPI_STAT&TXS) >0 )
            ;  // ensure tx buffer empty
        while((*pSPI_STAT&RXS) ==0 )
            ; // ensure rx buffer full
        *pSPI_TDBR = *outbuf16++;
        SSYNC;
        if (remainingBytes == (bufsize-2))  // first data is junk
            dummy = *pSPI_RDBR;
        else
            *inbuf16++ = *pSPI_RDBR; // read data from slave processor
        SSYNC;
        remainingBytes -= 2;
    }
    printf("##$X SPI Master - Ack = 0x%x\n\r", (unsigned int)dummy);
    *pSPI_CTL = 0x400;
    *pSPI_FLG = 0x0;
    *pSPI_BAUD = 0x0;
    SSYNC;
}

void svs_slave(
    unsigned short *inbuf16,
    unsigned short *outbuf16,
    int bufsize)
{
    int remainingBytes;
    unsigned short ix, *bufsave;

    bufsave = inbuf16;
    *pPORTF_FER    |= (PF14|PF13|PF12|PF11|PF10);    // SPISS select PF14 input as slave,
    // MOSI PF11 enabled (note shouldn't need PF10 as that's the flash memory)
    *pPORT_MUX    |= PJSE;     // Enable PJ10 SSEL2 & 3 PORT_MUX PJSE=1  not required as we're slave..
    *pSPI_BAUD    = 4;
    *pSPI_CTL     = SPE|CPOL|SIZE|EMISO|0x00; // tc on read
    SSYNC;

    remainingBytes = bufsize;

    //ix = (unsigned int)crc16_ccitt((void *)buf16, bufsize-8);
    //buf16[(bufsize/2)-2] = ix; // write CRC into buffer

    while(remainingBytes)
    {
        while( (*pSPI_STAT&SPIF) == 0 )
            ; // ensure spif transfer complete
        while( (*pSPI_STAT&RXS) == 0  )
            ; // ensure rx buffer full

        *pSPI_TDBR = *outbuf16++;
        SSYNC;
        *inbuf16++ = *pSPI_RDBR; // read full 16 bits in one
        remainingBytes -= 2;
        while( (remainingBytes>0) && ((*pSPI_STAT&TXS) > 0)  )
            ; // ensure tx buffer empty
    };

    *pSPI_CTL = 0x400;
    *pSPI_FLG = 0x0;
    *pSPI_BAUD = 0x0;
    SSYNC;

    printf("##$R SPI Slave\n\r");
    ix = (unsigned int)crc16_ccitt((void *)bufsave, bufsize-8);
    inbuf16 -= 2;
    printf("     CRC-sent: 0x%x CRC-received: 0x%x\n", (unsigned short)*inbuf16, ix);
}

/* Two structures are created:
 * svs_data stores features obtained from this camera
 * svs_data_received stores features received from the opposite camera */
struct svs_data_struct
    {

        /* array storing x coordinates of detected features */
        short int feature_x[SVS_MAX_FEATURES];

        /* array storing the number of features detected on each row */
        unsigned short int features_per_row[SVS_MAX_IMAGE_HEIGHT/SVS_VERTICAL_SAMPLING];

        /* Array storing a binary descriptor, 32bits in length, for each detected feature.
         * This will be used for matching purposes.*/
        unsigned int descriptor[SVS_MAX_FEATURES];

        /* mean luminance for each feature */
        unsigned char mean[SVS_MAX_FEATURES];

    }
svs_data, svs_data_received;

/* buffer which stores sliding sum */
int row_sum[SVS_MAX_IMAGE_WIDTH];

/* buffer used to find peaks in edge space */
unsigned int row_peaks[SVS_MAX_IMAGE_WIDTH];

/* array stores matching probabilities (prob,x,y,disp) */
unsigned int svs_matches[SVS_MAX_FEATURES*4];

/* used during filtering */
unsigned char valid_quadrants[SVS_MAX_IMAGE_WIDTH];

/* array used to store a disparity histogram */
int disparity_histogram[SVS_MAX_IMAGE_WIDTH];

/* maps raw image pixels to rectified pixels */
int calibration_map[SVS_MAX_IMAGE_WIDTH*SVS_MAX_IMAGE_HEIGHT];


#else

extern unsigned int imgWidth, imgHeight;

/* Two structures are created:
 * svs_data stores features obtained from this camera
 * svs_data_received stores features received from the opposite camera */
extern struct svs_data_struct
    {

        /* array storing x coordinates of detected features */
        short int feature_x[SVS_MAX_FEATURES];

        /* array storing the number of features detected on each row */
        unsigned short int features_per_row[SVS_MAX_IMAGE_HEIGHT/SVS_VERTICAL_SAMPLING];

        /* Array storing a binary descriptor, 32bits in length, for each detected feature.
         * This will be used for matching purposes.*/
        unsigned int descriptor[SVS_MAX_FEATURES];

        /* mean luminance for each feature */
        unsigned char mean[SVS_MAX_FEATURES];

    }
svs_data, svs_data_received;

/* buffer which stores sliding sum */
extern int row_sum[SVS_MAX_IMAGE_WIDTH];

/* buffer used to find peaks in edge space */
extern unsigned int row_peaks[SVS_MAX_IMAGE_WIDTH];

/* array stores matching probabilities (prob,x,y,disp) */
extern unsigned int svs_matches[SVS_MAX_FEATURES*4];

/* used during filtering */
extern unsigned char valid_quadrants[SVS_MAX_IMAGE_WIDTH];

/* array used to store a disparity histogram */
extern int disparity_histogram[SVS_MAX_IMAGE_WIDTH];

/* maps raw image pixels to rectified pixels */
extern int calibration_map[SVS_MAX_IMAGE_WIDTH*SVS_MAX_IMAGE_HEIGHT];

#endif

extern unsigned int imgWidth, imgHeight;

/* offsets of pixels to be compared within the patch region
 * arranged into a rectangular structure */
const int pixel_offsets[] =
    {
        -2,-4,  -1,-4,         1,-4,  2,-4,
        -5,-2,  -4,-2,  -3,-2,  -2,-2,  -1,-2,  0,-2,  1,-2,  2,-2,  3,-2,  4,-2,  5,-2,
        -5, 2,  -4, 2,  -3, 2,  -2, 2,  -1, 2,  0, 2,  1, 2,  2, 2,  3, 2,  4, 2,  5, 2,
        -2, 4,  -1, 4,         1, 4,  2, 4
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


/* Updates sliding sums and edge response values along a single row
 * Returns the mean luminance along the row */
int svs_update_sums(
    int y,                                /* row index */
    unsigned char* rectified_frame_buf)   /* image data */
{

    int x, idx, mean=0;
    unsigned int v;

    /* compute sums along the row */
    int stride = pixindex(imgWidth, 0);
    idx = stride * y;

#ifdef SVS_EMBEDDED

    row_sum[0] = rectified_frame_buf[idx];
    for (x = 1; x < (int)imgWidth; x++)
    {
        idx = pixindex(x, y);
        v = rectified_frame_buf[idx];
        row_sum[x] = row_sum[x-1] + v;
    }

    /* row mean luminance */
    mean = row_sum[x-1] / (int)imgWidth;
#else

    row_sum[0] =
        rectified_frame_buf[idx + 2] +
        rectified_frame_buf[idx + 1] +
        rectified_frame_buf[idx + 0];
    for (x = 1; x < (int)imgWidth; x++)
    {
        idx = pixindex(x, y);
        v = rectified_frame_buf[idx + 2] +
            rectified_frame_buf[idx + 1] +
            rectified_frame_buf[idx];
        row_sum[x] = row_sum[x-1] + v;
    }

    /* row mean luminance */
    mean = row_sum[x-1] / (((int)imgWidth-1)*6);
#endif

    /* compute peaks */
    int p0, p1;
    for (x = 4; x < (int)imgWidth-4; x++)
    {

        /* edge using 2 pixel radius */
        p0 = (row_sum[x] - row_sum[x - 2]) -
             (row_sum[x + 2] - row_sum[x]);
        if (p0 < 0)
            p0 = -p0;

        /* edge using 4 pixel radius */
        p1 = (row_sum[x] - row_sum[x - 4]) -
             (row_sum[x + 4] - row_sum[x]);
        if (p1 < 0)
            p1 = -p1;

        /* overall edge response */
        row_peaks[x] = p0 + p1;
    }

    return(mean);
}


/* performs non-maximal suppression on the given row */
void svs_non_max(
    int inhibition_radius,     /* radius for non-maximal suppression */
    unsigned int min_response) /* minimum threshold as a percent in the range 0-200 */
{

    int x, r;
    unsigned int v;

    /* average response */
    unsigned int av_peaks = 0;
    for (x = 4; x < (int)imgWidth - 4; x++)
    {
        av_peaks += row_peaks[x];
    }
    av_peaks /= (imgWidth - 8);

    /* adjust the threshold */
    av_peaks = av_peaks * min_response / 100;

    for (x = 4; x < (int)imgWidth - inhibition_radius; x++)
    {

        if (row_peaks[x] < av_peaks)
            row_peaks[x] = 0;
        v = row_peaks[x];
        if (v > 0)
        {
            for (r = 1; r < inhibition_radius; r++)
            {
                if (row_peaks[x + r] < v)
                {
                    row_peaks[x + r] = 0;
                }
                else
                {
                    row_peaks[x] = 0;
                    r = inhibition_radius;
                }
            }
        }
    }
}

/* creates a binary descriptor for a feature at the given coordinate
   which can subsequently be used for matching */
int svs_compute_descriptor(
    int px,
    int py,
    unsigned char* rectified_frame_buf,
    int no_of_features,
    int row_mean)
{

    unsigned char bit_count = 0;
    int pixel_offset_idx, ix, bit;
    int meanval = 0;
    unsigned int desc = 0;

    /* find the mean luminance for the patch */
    for (pixel_offset_idx = 0; pixel_offset_idx < SVS_DESCRIPTOR_PIXELS*2; pixel_offset_idx += 2)
    {
        ix = rectified_frame_buf[pixindex((px + pixel_offsets[pixel_offset_idx]), (py + pixel_offsets[pixel_offset_idx + 1]))];
        meanval += rectified_frame_buf[ix];
    }
    meanval /= SVS_DESCRIPTOR_PIXELS;

    /* binarise */
    bit = 1;
    for (pixel_offset_idx = 0; pixel_offset_idx < SVS_DESCRIPTOR_PIXELS*2; pixel_offset_idx += 2, bit *= 2)
    {
        ix = rectified_frame_buf[pixindex((px + pixel_offsets[pixel_offset_idx]), (py + pixel_offsets[pixel_offset_idx + 1]))];
        if (rectified_frame_buf[ix] > meanval)
        {
            desc |= bit;
            bit_count++;
        }
    }

    if ((bit_count > 3) &&
            (bit_count < SVS_DESCRIPTOR_PIXELS-3))
    {
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

        svs_data.mean[no_of_features] = (unsigned char)(meanval/3);
        svs_data.descriptor[no_of_features] = desc;
        return(0);
    }
    else
    {
        /* probably just noise */
        return(-1);
    }
}

/* returns a set of features suitable for stereo matching */
int svs_get_features(
    unsigned char* rectified_frame_buf,  /* image data */
    int inhibition_radius,               /* radius for non-maximal supression */
    unsigned int minimum_response,       /* minimum threshold */
    int calibration_offset_x,            /* calibration x offset in pixels */
    int calibration_offset_y)            /* calibration y offset in pixels */
{

    unsigned short int no_of_feats;
    int x, y, row_mean, start_x;
    int no_of_features = 0;
    int row_idx = 0;

    memset(svs_data.features_per_row, 0, SVS_MAX_IMAGE_HEIGHT/SVS_VERTICAL_SAMPLING * sizeof(unsigned short));

    start_x = imgWidth - 15;
    if ((int)imgWidth - inhibition_radius - 1 < start_x)
        start_x = (int)imgWidth - inhibition_radius - 1;

    for (y = 4 + calibration_offset_y; y < (int)imgHeight - 4; y += SVS_VERTICAL_SAMPLING)
    {

        /* reset number of features on the row */
        no_of_feats = 0;

        if ((y >= 4) && (y <= (int)imgHeight - 4))
        {

            row_mean = svs_update_sums(y, rectified_frame_buf);
            svs_non_max(inhibition_radius, minimum_response);

            /* store the features */
            for (x = start_x; x > 15; x--)
            {
                if (row_peaks[x] > 0)
                {

                    if (svs_compute_descriptor(
                                x, y, rectified_frame_buf, no_of_features, row_mean) == 0)
                    {

                        svs_data.feature_x[no_of_features++] = (short int)(x + calibration_offset_x);
                        no_of_feats++;
                        if (no_of_features == SVS_MAX_FEATURES)
                        {
                            y = imgHeight;
                            printf("stereo feature buffer full\n");
                            break;
                        }
                    }
                }
            }
        }

        svs_data.features_per_row[row_idx++] = no_of_feats;
    }
    return(no_of_features);
}

#ifdef SVS_EMBEDDED

/* updates a set of features suitable for stereo matching */
int svs_grab(
    int calibration_offset_x,  /* calibration x offset in pixels */
    int calibration_offset_y)  /* calibration y offset in pixels */
{
    /* some default values */
    const int inhibition_radius = 16;
    const unsigned int minimum_response = 180;

    /* grab new frame */
    move_image((unsigned char *)DMA_BUF1, (unsigned char *)DMA_BUF2, (unsigned char *)FRAME_BUF, imgWidth, imgHeight);

    /* convert to YUV */
    move_yuv422_to_planar((unsigned char *)FRAME_BUF, (unsigned char *)FRAME_BUF3, imgWidth, imgHeight);

    /* rectify the image */
    svs_rectify((unsigned char *)FRAME_BUF3, (unsigned char *)FRAME_BUF4);

    /* compute edge features */
    return(svs_get_features((unsigned char *)FRAME_BUF4, inhibition_radius, minimum_response, calibration_offset_x, calibration_offset_y));
}

#endif

/* Match features from this camera with features from the opposite one.
 * It is assumed that matching is performed on the left camera CPU */
int svs_match(
    int ideal_no_of_matches,          /* ideal number of matches to be returned */
    int max_disparity_percent,        /* max disparity as a percent of image width */
    int descriptor_match_threshold,   /* minimum no of descriptor bits to be matched, in the range 1 - SVS_DESCRIPTOR_PIXELS */
    int learnDesc,                    /* descriptor match weight */
    int learnLuma,                    /* luminance match weight */
    int learnDisp)                    /* disparity weight */
{

    int xL, xR, L, R, y, no_of_feats_left, no_of_feats_right, row, bit;
    int luma_diff, max_disp, meanL, meanR, disp, fL=0, fR=0, bestR;
    unsigned int descL, descLanti, descR, desc_match;
    unsigned int correlation, anticorrelation, total, n;
    unsigned int match_prob, best_prob;
    int max, curr_idx, search_idx, winner_idx=0;
    int no_of_possible_matches = 0, matches = 0;

    unsigned int meandescL, meandescR;
    short meandesc[SVS_DESCRIPTOR_PIXELS];

    /* convert max disparity from percent to pixels */
    max_disp = max_disparity_percent * imgWidth / 100;

    row = 0;
    for (y = 4; y < (int)imgHeight - 4; y += SVS_VERTICAL_SAMPLING, row++)
    {

        /* number of features on left and right rows */
        no_of_feats_left = svs_data.features_per_row[row];
        no_of_feats_right = svs_data_received.features_per_row[row];

        /* compute mean descriptor for the left row
         * this will be used to create eigendescriptors */
        meandescL = 0;
        memset(meandesc, 0, (SVS_DESCRIPTOR_PIXELS)* sizeof(short));
        for (L = 0; L < no_of_feats_left; L++)
        {
            descL = svs_data.descriptor[fL + L];
            n = 1;
            for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2)
            {
                if (descL & n)
                    meandesc[bit]++;
                else
                    meandesc[bit]--;
            }
        }
        n = 1;
        for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2)
        {
            if (meandesc[bit] >= 0)
                meandescL |= n;
        }

        /* compute mean descriptor for the right row
         * this will be used to create eigendescriptors */
        meandescR = 0;
        memset(meandesc, 0, (SVS_DESCRIPTOR_PIXELS)* sizeof(short));
        for (R = 0; R < no_of_feats_right; R++)
        {
            descR = svs_data_received.descriptor[fR + R];
            n = 1;
            for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2)
            {
                if (descR & n)
                    meandesc[bit]++;
                else
                    meandesc[bit]--;
            }
        }
        n = 1;
        for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++, n *= 2)
        {
            if (meandesc[bit] > 0)
                meandescR |= n;
        }

        /* features along the row in the left camera */
        for (L = 0; L < no_of_feats_left; L++)
        {

            /* x coordinate of the feature in the left camera */
            xL = svs_data.feature_x[fL + L];

            /* mean luminance and eigendescriptor for the left camera feature */
            meanL = svs_data.mean[fL + L];
            descL = svs_data.descriptor[fL + L] & meandescL;

            /* invert bits of the descriptor for anti-correlation matching */
            n = descL;
            descLanti = 0;
            for (bit = 0; bit < SVS_DESCRIPTOR_PIXELS; bit++)
            {
                /* Shift result vector to higher significance. */
                descLanti <<= 1;
                /* Get least significant input bit. */
                descLanti |= n & 1;
                /* Shift input vector to lower significance. */
                n >>= 1;
            }

            total = 0;

            /* features along the row in the right camera */
            for (R = 0; R < no_of_feats_right; R++)
            {

                /* set matching score to zero */
                row_peaks[R] = 0;

                /* x coordinate of the feature in the right camera */
                xR = svs_data_received.feature_x[fR + R];

                /* compute disparity */
                disp = xL - xR;

                /* is the disparity within range? */
                if ((disp >= -10) && (disp < max_disp))
                {
                    if (disp < 0)
                        disp = 0;


                    /* mean luminance for the right camera feature */
                    meanR = svs_data_received.mean[fR + R];

                    /* is the mean luminance similar? */
                    luma_diff = meanR - meanL;

                    /* right camera feature eigendescriptor */
                    descR = svs_data_received.descriptor[fR + R] & meandescR;

                    /* bitwise descriptor correlation match */
                    desc_match = descL & descR;

                    /* count the number of correlation bits */
                    correlation =
                        BitsSetTable256[desc_match & 0xff] +
                        BitsSetTable256[(desc_match >> 8) & 0xff] +
                        BitsSetTable256[(desc_match >> 16) & 0xff] +
                        BitsSetTable256[desc_match >> 24];

                    /* were enough bits matched ? */
                    if ((int)correlation > descriptor_match_threshold)
                    {

                        /* bitwise descriptor anti-correlation match */
                        desc_match = descLanti & descR;

                        /* count the number of anti-correlation bits */
                        anticorrelation =
                            BitsSetTable256[desc_match & 0xff] +
                            BitsSetTable256[(desc_match >> 8) & 0xff] +
                            BitsSetTable256[(desc_match >> 16) & 0xff] +
                            BitsSetTable256[desc_match >> 24];

                        if (luma_diff < 0)
                            luma_diff = -luma_diff;
                        int score =
                            10000 +
                            (max_disp * learnDisp) +
                            (((int)correlation + (int)(SVS_DESCRIPTOR_PIXELS - anticorrelation)) * learnDesc) -
                            (luma_diff * learnLuma) -
                            (disp * learnDisp);
                        if (score < 0)
                            score = 0;

                        /* store overall matching score */
                        row_peaks[R] = (unsigned int)score;
                        total += row_peaks[R];
                    }
                }
                else
                {
                    if ((disp < 0) && (disp > -max_disp))
                    {
                        row_peaks[R] = (unsigned int)((max_disp - disp) * learnDisp);
                        total += row_peaks[R];
                    }
                }
            }

            /* non-zero total matching score */
            if (total > 0)
            {

                /* convert matching scores to probabilities */
                best_prob = 0;
                for (R = 0; R < no_of_feats_right; R++)
                {
                    if (row_peaks[R] > 0)
                    {
                        match_prob = row_peaks[R] * 1000 / total;
                        if (match_prob > best_prob)
                        {
                            best_prob = match_prob;
                            bestR = R;
                        }
                    }
                }

                if ((best_prob > 0) &&
                        (best_prob < 1000) &&
                        (no_of_possible_matches < SVS_MAX_FEATURES))
                {

                    /* x coordinate of the feature in the right camera */
                    xR = svs_data_received.feature_x[fR + bestR];

                    /* possible disparity */
                    disp = xL - xR;

                    if (disp >= -10)
                    {
                        if (disp < 0)
                            disp = 0;
                        /* add the best result to the list of possible matches */
                        svs_matches[no_of_possible_matches*4] = best_prob;
                        svs_matches[no_of_possible_matches*4 + 1] = (unsigned int)xL;
                        svs_matches[no_of_possible_matches*4 + 2] = (unsigned int)y;
                        svs_matches[no_of_possible_matches*4 + 3] = (unsigned int)disp;
                        no_of_possible_matches++;
                    }
                }
            }
        }

        /* increment feature indexes */
        fL += no_of_feats_left;
        fR += no_of_feats_right;
    }

    if (no_of_possible_matches > 1)
    {

        /* filter the results */
        svs_filter(no_of_possible_matches, max_disp, 3);

        /* sort matches in descending order of probability */
        if (no_of_possible_matches < ideal_no_of_matches)
        {
            ideal_no_of_matches = no_of_possible_matches;
        }
        curr_idx = 0;
        search_idx = 0;
        for (matches = 0; matches < ideal_no_of_matches; matches++, curr_idx += 4)
        {

            match_prob =  svs_matches[curr_idx];
            winner_idx = -1;

            search_idx = curr_idx + 4;
            max = no_of_possible_matches * 4;
            while (search_idx < max)
            {
                if (svs_matches[search_idx] > match_prob)
                {
                    match_prob = svs_matches[search_idx];
                    winner_idx = search_idx;
                }
                search_idx += 4;
            }
            if (winner_idx > -1)
            {

                /* swap */
                best_prob = svs_matches[winner_idx];
                xL = svs_matches[winner_idx + 1];
                y = svs_matches[winner_idx + 2];
                disp = svs_matches[winner_idx + 3];

                svs_matches[winner_idx] = svs_matches[curr_idx];
                svs_matches[winner_idx + 1] = svs_matches[curr_idx + 1];
                svs_matches[winner_idx + 2] = svs_matches[curr_idx + 2];
                svs_matches[winner_idx + 3] = svs_matches[curr_idx + 3];

                svs_matches[curr_idx] = best_prob;
                svs_matches[curr_idx + 1] = xL;
                svs_matches[curr_idx + 2] = y;
                svs_matches[curr_idx + 3] = disp;
            }

            if (svs_matches[curr_idx] == 0)
            {
                break;
            }
        }
    }
    return(matches);
}


/* filtering function removes noise by searching for a peak in the disparity histogram */
void svs_filter(
    int no_of_possible_matches, /* the number of stereo matches */
    int max_disparity_pixels,   /*maximum disparity in pixels */
    int tolerance)              /* tolerance around the peak in pixels of disparity */
{

    int i, hf;
    unsigned int tx=0, ty=0, bx=0, by=0;

    /* clear quadrants */
    memset(valid_quadrants, 0, SVS_MAX_IMAGE_WIDTH * sizeof(unsigned char));

    /* create disparity histograms within different
     * zones of the image */
    for (hf = 0; hf < 4; hf++)
    {

        switch(hf)
        {
            /* left hemifield */
        case 0:
            {
                tx = 0;
                ty = 0;
                bx = imgWidth/2;
                by = imgHeight;
                break;
            }
            /* right hemifield */
        case 1:
            {
                tx = bx;
                bx = imgWidth;
                break;
            }
            /* upper hemifield */
        case 2:
            {
                tx = 0;
                ty = 0;
                bx = imgWidth;
                by = imgHeight/2;
                break;
            }
            /* lower hemifield */
        case 3:
            {
                ty = by;
                by = imgHeight;
                break;
            }
        }

        /* clear the histogram */
        memset(disparity_histogram, 0, SVS_MAX_IMAGE_WIDTH * sizeof(int));
        int hist_max = 0;

        /* update the disparity histogram */
        for (i = 0; i < no_of_possible_matches; i++)
        {
            unsigned int x = svs_matches[i*4 + 1];
            if ((x > tx) && (x < bx))
            {
                unsigned int y = svs_matches[i*4 + 2];
                if ((y > ty) && (y < by))
                {
                    int disp = svs_matches[i*4 + 3];
                    disparity_histogram[disp]++;
                    if (disparity_histogram[disp] > hist_max)
                        hist_max = disparity_histogram[disp];
                }
            }
        }

        /* locate the histogram peak */
        int mass = 0;
        int disp2 = 0;
        int hist_thresh = hist_max/4;
        int hist_mean = 0;
        int hist_mean_hits = 0;
        int d;
        for (d = 3; d < max_disparity_pixels-1; d++)
        {
            if (disparity_histogram[d] > hist_thresh)
            {
                int m = disparity_histogram[d] + disparity_histogram[d-1] + disparity_histogram[d+1];
                mass += m;
                disp2 += m * d;
            }
            if (disparity_histogram[d] > 0)
            {
                hist_mean += disparity_histogram[d];
                hist_mean_hits++;
            }
        }
        if (mass > 0)
        {
            disp2 /= mass;
            hist_mean /= hist_mean_hits;
        }

        /* simple near/far classification adjusts
         * the peak disparity that we're interested in */
        int near = 1;
        if (hist_mean*4 > disparity_histogram[0])
        {
            near = 0;
        }

        /* remove matches too far away from the peak by setting
         * their probabilities to zero */
        unsigned int min_disp = disp2 - tolerance;
        unsigned int max_disp = disp2 + tolerance;
        for (i = 0; i < no_of_possible_matches; i++)
        {
            unsigned int x = svs_matches[i*4 + 1];
            if ((x > tx) && (x < bx))
            {
                unsigned int y = svs_matches[i*4 + 2];
                if ((y > ty) && (y < by))
                {
                    unsigned int disp = svs_matches[i*4 + 3];
                    if (near == 1)
                    {
                        if (!((disp < min_disp) || (disp > max_disp)))
                        {
                            /* near - within stereo ranging resolution */
                            valid_quadrants[i]++;
                        }
                    }
                    else
                    {
                        if (disp <= 2)
                        {
                            /* far out man */
                            valid_quadrants[i]++;
                        }
                    }
                }
            }
        }
    }

    for (i = 0; i < no_of_possible_matches; i++)
    {
        if (valid_quadrants[i] == 0)
        {
            /* set probability to zero */
            svs_matches[i*4] = 0;
        }
    }
}

/* takes the raw image and camera calibration parameters and returns a rectified image */
void svs_rectify(
    unsigned char* raw_image,     /* raw image grabbed from camera */
    unsigned char* rectified_frame_buf) /* returned rectified image */
{

    int i, col, n = 0;
    int max = imgWidth * imgHeight * 3;
    for (i = 0; i < max; i += 3, n++)
    {
        int index = calibration_map[n] * 3;
        for (col = 0; col < 3; col++)
            rectified_frame_buf[i + col] = raw_image[index + col];
    }
}

