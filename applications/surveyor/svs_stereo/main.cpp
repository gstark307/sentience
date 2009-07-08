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
struct SVS_data_struct
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

/* peaks along the row */
unsigned int row_peaks[SVS_MAX_IMAGE_WIDTH];

/* array stores matching probabilities */
unsigned int svs_matches[SVS_MAX_FEATURES*4];

/* used during filtering */
unsigned char valid_quadrants[SVS_MAX_IMAGE_WIDTH];

/* array used to store a disparity histogram */
int disparity_histogram[SVS_MAX_IMAGE_WIDTH];

/* maps raw image pixels to rectified pixels */
int calibration_map[SVS_MAX_IMAGE_WIDTH*SVS_MAX_IMAGE_HEIGHT];

/* copy the data from one structure to the other */
void copy_to_received()
{
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
    unsigned char* rectified_frame_buf)
{

    int i, idx, bit = 1;
    unsigned int desc;

    /* offsets of pixels to be compared within the patch region
     * arranged into a rectangular structure */
    const int pixel_offsets[] =
        {
            -2,-4,  -1,-4,         1,-4,  2,-4,
            -5,-2,  -4,-2,  -3,-2,  -2,-2,  -1,-2,  0,-2,  1,-2,  2,-2,  3,-2,  4,-2,  5,-2,
            -5, 2,  -4, 2,  -3, 2,  -2, 2,  -1, 2,  0, 2,  1, 2,  2, 2,  3, 2,  4, 2,  5, 2,
            -2, 4,  -1, 4,         1, 4,  2, 4
        };

    desc = svs_data.descriptor[descriptor_index];
    for (i = 0; i < SVS_DESCRIPTOR_PIXELS; i++, bit *= 2)
    {
        idx = pixindex((px + pixel_offsets[i*2]), (py + pixel_offsets[i*2+1]));
        if (desc & bit)
        {
            rectified_frame_buf[idx] = 0;
            rectified_frame_buf[idx + 1] = 255;
            rectified_frame_buf[idx + 2] = 0;
        }
        else
        {
            rectified_frame_buf[idx] = 0;
            rectified_frame_buf[idx + 1] = 0;
            rectified_frame_buf[idx + 2] = 255;
        }
    }
}

/*---------------------------------------------------------------------*/
/* learning matching weights */
/*---------------------------------------------------------------------*/


/*!
 * \brief does the line intersect with the given line?
 * \param x0 first line top x
 * \param y0 first line top y
 * \param x1 first line bottom x
 * \param y1 first line bottom y
 * \param x2 second line top x
 * \param y2 second line top y
 * \param x3 second line bottom x
 * \param y3 second line bottom y
 * \param xi intersection x coordinate
 * \param yi intersection y coordinate
 * \return true if the lines intersect
 */
bool intersection(
    float x0,
    float y0,
    float x1,
    float y1,
    float x2,
    float y2,
    float x3,
    float y3,
    float& xi,
    float& yi)
{
    float a1, b1, c1,         //constants of linear equations
    a2, b2, c2,
    det_inv,            //the inverse of the determinant of the coefficient
    m1, m2, dm;         //the gradients of each line
    bool insideLine = false;  //is the intersection along the lines given, or outside them
    float tx, ty, bx, by;

    //compute gradients, note the cludge for infinity, however, this will
    //be close enough
    if ((x1 - x0) != 0)
        m1 = (y1 - y0) / (x1 - x0);
    else
        m1 = (float)1e+10;   //close, but no cigar

    if ((x3 - x2) != 0)
        m2 = (y3 - y2) / (x3 - x2);
    else
        m2 = (float)1e+10;   //close, but no cigar

    dm = (float)ABS(m1 - m2);
    if (dm > 0.000001f)
    {
        //compute constants
        a1 = m1;
        a2 = m2;

        b1 = -1;
        b2 = -1;

        c1 = (y0 - m1 * x0);
        c2 = (y2 - m2 * x2);

        //compute the inverse of the determinate
        det_inv = 1 / (a1 * b2 - a2 * b1);

        //use Kramers rule to compute xi and yi
        xi = ((b1 * c2 - b2 * c1) * det_inv);
        yi = ((a2 * c1 - a1 * c2) * det_inv);

        //is the intersection inside the line or outside it?
        if (x0 < x1)
        {
            tx = x0;
            bx = x1;
        }
        else
        {
            tx = x1;
            bx = x0;
        }
        if (y0 < y1)
        {
            ty = y0;
            by = y1;
        }
        else
        {
            ty = y1;
            by = y0;
        }
        if ((xi >= tx) && (xi <= bx) && (yi >= ty) && (yi <= by))
        {
            if (x2 < x3)
            {
                tx = x2;
                bx = x3;
            }
            else
            {
                tx = x3;
                bx = x2;
            }
            if (y2 < y3)
            {
                ty = y2;
                by = y3;
            }
            else
            {
                ty = y3;
                by = y2;
            }
            if ((xi >= tx) && (xi <= bx) && (yi >= ty) && (yi <= by))
            {
                insideLine = true;
            }
        }
    }
    else
    {
        //parallel (or parallelish) lines, return some indicative value
        xi = 9999;
    }

    return (insideLine);
}

/* returns an estimate of stereo matching quality by
 * counting the number of intersections */
float EstimateMatchingQuality(
    int calibration_offset_x,
    int calibration_offset_y,
    int no_of_matches)
{

    int intersections = 0;

    for (int i = 0; i < no_of_matches-1; i++)
    {
        int x0 = svs_matches[i*4 + 1];
        int y0 = svs_matches[i*4 + 2];
        int disp = svs_matches[i*4 + 3];
        int x1 = (x0 - disp) - (calibration_offset_x*2);
        int y1 = imgHeight + y0 - (calibration_offset_y*2);

        for (int j = i + 1; j < no_of_matches-1; j++)
        {
            int x2 = svs_matches[j*4 + 1];
            int y2 = svs_matches[j*4 + 2];
            disp = svs_matches[j*4 + 3];
            int x3 = (x2 - disp) - (calibration_offset_x*2);
            int y3 = imgHeight + y2 - (calibration_offset_y*2);

            float ix = 0;
            float iy = 0;
            if (intersection(x0,y0,x1,y1,x2,y2,x3,y3,ix,iy))
            {
                intersections++;
            }
        }
    }

    return(1.0f / (1.0f + ((float)intersections/(float)no_of_matches)));
}

void LearnMatchingWeights(
    int calibration_offset_x,
    int calibration_offset_y,
    int minimum_matches)
{

    int ideal_no_of_matches = 200;
    int max_disparity_percent = 20;
    int descriptor_match_threshold = 0;//SVS_DESCRIPTOR_PIXELS * 10 / 100;

    int learnDesc_min = 1;
    int learnDesc_max = 20;
    int learnLuma_min = 1;
    int learnLuma_max = 10;
    int learnDisp_min = 1;
    int learnDisp_max = 10;

    float max_quality = 0;
    int learnDesc_best = 1;
    int learnLuma_best = 1;
    int learnDisp_best = 1;

    for (int learnDesc = learnDesc_min; learnDesc < learnDesc_max; learnDesc++)
    {

        for (int learnLuma = learnLuma_min; learnLuma < learnLuma_max; learnLuma++)
        {

            for (int learnDisp = learnDisp_min; learnDisp < learnDisp_max; learnDisp++)
            {

                int matches = svs_match(
                                  ideal_no_of_matches,
                                  max_disparity_percent,
                                  descriptor_match_threshold,
                                  learnDesc,
                                  learnLuma,
                                  learnDisp);

                if (matches > minimum_matches)
                {
                    float quality = EstimateMatchingQuality(
                                        calibration_offset_x,
                                        calibration_offset_y,
                                        matches);

                    if (quality > max_quality)
                    {
                        max_quality = quality;
                        learnDesc_best = learnDesc;
                        learnLuma_best = learnLuma;
                        learnDisp_best = learnDisp;
                    }
                }
            }
        }
    }

    printf("-- Result --\n");
    printf("learnDesc: %d\n", learnDesc_best);
    printf("learnLuma: %d\n", learnLuma_best);
    printf("learnDisp: %d\n", learnDisp_best);
}


/*---------------------------------------------------------------------*/
/* main */
/*---------------------------------------------------------------------*/

int main()
{

    printf("frame size %d bytes\n", sizeof(svs_data));

    bool show_descriptors = false;
    std::string left_image_filename = "left6.bmp";
    std::string right_image_filename = "right6.bmp";
    std::string left_features_filename = "left_feats.ppm";
    std::string right_features_filename = "right_feats.ppm";
    std::string matched_features_filename = "matches.ppm";
    std::string anaglyph_filename = "anaglyph.ppm";
    std::string matched_features_two_images_filename = "matches_two.ppm";
    int cam;
    int no_of_feats = 0;

    /* feature detection params */
    int calibration_offset_x = 16; //5;
    int calibration_offset_y = -4; //6;
    int inhibition_radius = 16;
    unsigned int minimum_response = 180;

    /* matching params */
    int ideal_no_of_matches = 200;
    int max_disparity_percent = 20;
    int descriptor_match_threshold = 2; //SVS_DESCRIPTOR_PIXELS * 10 / 100;
    int learnDesc = 18; //13;
    int learnLuma = 7; //4;
    int learnDisp = 3; //7;

    if ((fileio::FileExists(left_image_filename)) &&
            (fileio::FileExists(right_image_filename)))
    {

        Bitmap* bmp_left = new Bitmap();
        bmp_left->FromFile(left_image_filename);
        Bitmap* bmp_right = new Bitmap();
        bmp_right->FromFile(right_image_filename);

        unsigned char* rectified_frame_buf;
        unsigned char* img_matches = new unsigned char[imgWidth * imgHeight * 3];
        unsigned char* img_matches_two_images = new unsigned char[imgWidth * imgHeight * 2 * 3];

        for (int n = 0; n < (int)(imgWidth*imgHeight*3); n++)
        {
            img_matches_two_images[n] = bmp_left->Data[n];
            img_matches_two_images[n+(imgWidth*imgHeight*3)] = bmp_right->Data[n];
        }

        int calib_offset_x = calibration_offset_x;
        int calib_offset_y = calibration_offset_y;
        for (cam = 1; cam >= 0; cam--)
        {

            if (cam == 0)
            {
                rectified_frame_buf = bmp_left->Data;
                imgWidth = bmp_left->Width;
                imgHeight = bmp_left->Height;
            }
            else
            {
                rectified_frame_buf = bmp_right->Data;
                imgWidth = bmp_right->Width;
                imgHeight = bmp_right->Height;
            }

            no_of_feats = svs_get_features(
                              rectified_frame_buf,
                              inhibition_radius,
                              minimum_response,
                              calib_offset_x,
                              calib_offset_y);

            printf("cam %d:  %d\n", cam, no_of_feats);

            if (cam == 1)
                copy_to_received();
            else
                memcpy(img_matches, rectified_frame_buf, imgWidth*imgHeight*3*sizeof(unsigned char));

            /* display the features */
            int row = 0;
            int feats_remaining = svs_data.features_per_row[row];

            for (int f = 0; f < no_of_feats; f++, feats_remaining--)
            {

                int x = (int)svs_data.feature_x[f] - calib_offset_x;
                int y = 4 + (row * SVS_VERTICAL_SAMPLING) + calib_offset_y;

                if (show_descriptors)
                {
                    draw_descriptor(x, y, f, rectified_frame_buf);
                }
                else
                {
                    //drawing::drawCross(rectified_frame_buf, imgWidth, imgHeight, x, y, 2, 0, 255, 0, 0);
                }

                /* move to the next row */
                if (feats_remaining <= 0)
                {
                    row++;
                    feats_remaining = svs_data.features_per_row[row];
                }
            }

            calib_offset_x = 0;
            calib_offset_y = 0;
        }

        bmp_left->SavePPM(left_features_filename.c_str());
        bmp_right->SavePPM(right_features_filename.c_str());

        //LearnMatchingWeights(calibration_offset_x, calibration_offset_y, 100);

        int matches = svs_match(
                          ideal_no_of_matches,
                          max_disparity_percent,
                          descriptor_match_threshold,
                          learnDesc,
                          learnLuma,
                          learnDisp);
        printf("matches = %d\n", matches);

        /* show disparity as spots */
        for (int i = 0; i < matches; i++)
        {
            int x = svs_matches[i*4 + 1];
            int y = svs_matches[i*4 + 2];
            int disp = svs_matches[i*4 + 3];
            drawing::drawBlendedSpot(img_matches, imgWidth, imgHeight, x, y, disp/3, 0, 255, 0);
        }

        /* show disparity as lines */
        for (int i = 0; i < matches; i+= matches/20)
        {
            int x = svs_matches[i*4 + 1];
            int r=0,g=0,b=0;
            switch((x/4) % 6)
            {
            case 0:
                {
                    r = 255;
                    g = 0;
                    b = 0;
                    break;
                }
            case 1:
                {
                    r = 0;
                    g = 255;
                    b = 0;
                    break;
                }
            case 2:
                {
                    r = 0;
                    g = 0;
                    b = 255;
                    break;
                }
            case 3:
                {
                    r = 255;
                    g = 0;
                    b = 255;
                    break;
                }
            case 4:
                {
                    r = 255;
                    g = 255;
                    b = 0;
                    break;
                }
            case 5:
                {
                    r = 0;
                    g = 255;
                    b = 255;
                    break;
                }
            }
            int y = svs_matches[i*4 + 2];
            int disp = svs_matches[i*4 + 3];
            int x2 = (x - disp) - calibration_offset_x;
            int y2 = imgHeight + y - calibration_offset_y;
            drawing::drawLine(img_matches_two_images, imgWidth, imgHeight*2, x,y, x2, y2, r,g,b,0,false);
            drawing::drawCircle(img_matches_two_images, imgWidth, imgHeight*2, x,y, 2, r,g,b, 0);
            drawing::drawCircle(img_matches_two_images, imgWidth, imgHeight*2, x2,y2, 2, r,g,b, 0);
        }

        Bitmap* bmp_matches = new Bitmap(img_matches, imgWidth, imgHeight, 3);
        bmp_matches->SavePPM(matched_features_filename.c_str());
        delete[] img_matches;
        delete bmp_matches;

        unsigned char* img_anaglyph = new unsigned char[imgWidth * imgHeight * 3];
        int n = 0;
        for (int y = 0; y < (int)imgHeight; y++)
        {
            for (int x = 0; x < (int)imgWidth; x++, n += 3)
            {
                int n2 = (((y - calibration_offset_y) * imgWidth) + x - calibration_offset_x) * 3;
                if ((n2 > -1) && (n2 < (int)(imgWidth*imgHeight*3)-3))
                {
                    img_anaglyph[n + 2] = 0;
                    img_anaglyph[n + 1] = bmp_right->Data[n2];
                    img_anaglyph[n] = bmp_left->Data[n];
                }
            }
        }
        Bitmap* bmp_anaglyph = new Bitmap(img_anaglyph, imgWidth, imgHeight, 3);
        bmp_anaglyph->SavePPM(anaglyph_filename.c_str());
        delete[] img_anaglyph;
        delete bmp_anaglyph;

        Bitmap* bmp_matches_two_images = new Bitmap(img_matches_two_images, imgWidth, imgHeight*2, 3);
        bmp_matches_two_images->SavePPM(matched_features_two_images_filename.c_str());
        delete[] img_matches_two_images;
        delete bmp_matches_two_images;
        delete bmp_left;
        delete bmp_right;
    }
    else
    {
        printf("File not found\n");
    }

    printf("Exit success");
    return(0);
}
