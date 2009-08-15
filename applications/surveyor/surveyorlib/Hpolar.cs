/*
    H-polar coordinate system
    See A Multi-Range Architecture for Collision-Free Off-Road Robot Navigation
    Pierre Sermanet, Raia Hadsell, Marco Scoffier, Matt Grimes, Jan Ben, Ayse Erkan, Chris Crudele, Urs Muller, Yann LeCun.
    Journal of Field Robotics (JFR) 2009
    http://cs.nyu.edu/~raia/docs/jfr-system.pdf
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
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using sluggish.utilities;

namespace surveyor.vision
{
    public class Hpolar
    {
        public uint imgWidth = 320;
        public uint imgHeight = 240;
        private uint prev_imgWidth = 320;
        
        public int robot_x_mm, robot_y_mm;
        public int robot_orientation_degrees;
        public int map_x_mm, map_y_mm;

        private int prev_dist_moved_mm, dist_moved_mm;

        /* horizon y coordinate in pixels */
        public int svs_ground_y_percent;

        /* horizon slope in pixels */
        public int svs_ground_slope_percent;

        public int svs_sensor_width_mmx100 = 470;

        public int SVS_HORIZONTAL_SAMPLING = 8;
        
        public int SVS_BASELINE_MM = 107;
        public int SVS_FOCAL_LENGTH_MMx100 = 360;
        public int SVS_FOV_DEGREES = 90;

        private int SVS_MAP_RDIM = 75;
        private int SVS_MAP_HYPRDIM = 75;
        private int SVS_MAP_TDIM = 400;
        private int SVS_MAP_WIDTH_CELLS = 40;
        private int SVS_MAP_CELL_SIZE_MM = 100;
        const int SVS_EMPTY_CELL = 999999;

        const int SVS_MAX_FEATURES = 2000;

        /* array used to estimate footline of objects on the ground plane */
        public ushort[] footline;
        public ushort[] footline_dist_mm;

        public int[] svs_matches;
        
        private int[] Hpolar_map_coords;
        private byte[] Hpolar_map_occupancy;
        private int prev_array_length;

        int[] HpolarLookup = {
            109,-150,108,-148,107,-146,106,-144,104,-142,103,-140,102,-138,101,-136,100,-133,99,-131,97,-128,96,-126,95,-123,94,-120,94,-117,93,-114,92,-111,92,-108,91,-104,91,-101,91,-98,91,-95,92,-91,92,-88,93,-85,94,-82,94,-79,95,-76,96,-73,97,-71,99,-68,100,-66,101,-63,102,-61,103,-59,104,-57,106,-55,107,-53,108,-51,109,-50,108,-151,107,-150,105,-148,104,-146,103,-144,102,-142,100,-140,99,-137,98,-135,96,-132,95,-130,94,-127,
            93,-124,92,-121,91,-118,90,-115,89,-111,89,-108,88,-105,88,-101,88,-98,88,-94,89,-91,89,-88,90,-84,91,-81,92,-78,93,-75,94,-72,95,-69,96,-67,98,-64,99,-62,100,-59,102,-57,103,-55,104,-53,105,-51,107,-50,108,-48,107,-153,105,-151,104,-150,103,-148,101,-146,100,-144,98,-141,97,-139,95,-137,94,-134,93,-131,91,-128,90,-125,89,-122,87,-119,87,-116,86,-112,85,-109,85,-105,84,-101,84,-98,85,-94,85,-90,
            86,-87,87,-83,87,-80,89,-77,90,-74,91,-71,93,-68,94,-65,95,-62,97,-60,98,-58,100,-55,101,-53,103,-51,104,-50,105,-48,107,-46,106,-155,104,-153,103,-151,101,-150,100,-148,98,-145,96,-143,95,-141,93,-138,91,-136,90,-133,88,-130,87,-127,85,-123,84,-120,83,-116,82,-113,81,-109,81,-105,80,-101,80,-98,81,-94,81,-90,82,-86,83,-83,84,-79,85,-76,87,-72,88,-69,90,-66,91,-63,93,-61,95,-58,96,-56,
            98,-54,100,-51,101,-50,103,-48,104,-46,106,-44,104,-157,103,-155,101,-153,100,-151,98,-150,96,-147,94,-145,93,-143,91,-140,89,-137,87,-135,85,-131,83,-128,82,-125,80,-121,79,-117,78,-114,77,-110,76,-106,76,-102,76,-97,76,-93,77,-89,78,-85,79,-82,80,-78,82,-74,83,-71,85,-68,87,-64,89,-62,91,-59,93,-56,94,-54,96,-52,98,-50,100,-48,101,-46,103,-44,104,-42,103,-159,102,-157,100,-155,98,-154,96,-152,
            94,-150,92,-147,90,-145,88,-142,86,-139,84,-136,82,-133,80,-130,78,-126,76,-123,74,-119,73,-115,72,-110,71,-106,70,-102,70,-97,71,-93,72,-89,73,-84,74,-80,76,-76,78,-73,80,-69,82,-66,84,-63,86,-60,88,-57,90,-54,92,-52,94,-50,96,-47,98,-45,100,-44,102,-42,103,-40,102,-161,100,-159,98,-158,96,-156,94,-154,92,-152,90,-150,88,-147,85,-144,83,-142,80,-139,78,-135,75,-132,73,-128,71,-124,69,-120,
            67,-116,66,-111,65,-107,64,-102,64,-97,65,-92,66,-88,67,-83,69,-79,71,-75,73,-71,75,-67,78,-64,80,-60,83,-57,85,-55,88,-52,90,-50,92,-47,94,-45,96,-43,98,-41,100,-40,102,-38,101,-163,99,-162,97,-160,95,-158,93,-156,90,-154,88,-152,85,-150,82,-147,80,-144,77,-141,74,-138,71,-134,68,-130,65,-126,63,-121,61,-117,59,-112,58,-107,58,-102,58,-97,58,-92,59,-87,61,-82,63,-78,65,-73,68,-69,
            71,-65,74,-61,77,-58,80,-55,82,-52,85,-50,88,-47,90,-45,93,-43,95,-41,97,-39,99,-37,101,-36,100,-166,98,-164,95,-162,93,-161,91,-159,88,-157,85,-155,82,-152,79,-150,76,-147,73,-143,69,-140,66,-136,63,-132,59,-128,56,-123,54,-118,52,-113,50,-108,49,-102,49,-97,50,-91,52,-86,54,-81,56,-76,59,-71,63,-67,66,-63,69,-59,73,-56,76,-52,79,-50,82,-47,85,-44,88,-42,91,-40,93,-38,95,-37,
            98,-35,100,-33,99,-168,96,-167,94,-165,91,-163,89,-162,86,-160,83,-157,80,-155,76,-152,72,-150,68,-146,64,-143,60,-139,56,-135,52,-130,49,-125,45,-120,43,-114,41,-109,40,-103,40,-96,41,-90,43,-85,45,-79,49,-74,52,-69,56,-64,60,-60,64,-56,68,-53,72,-50,76,-47,80,-44,83,-42,86,-39,89,-37,91,-36,94,-34,96,-32,99,-31,97,-171,95,-169,93,-168,90,-166,87,-164,84,-163,80,-160,77,-158,73,-156,
            68,-153,64,-150,59,-146,54,-142,49,-138,44,-133,40,-128,35,-122,32,-116,29,-109,28,-103,28,-96,29,-90,32,-83,35,-77,40,-71,44,-66,49,-61,54,-57,59,-53,64,-50,68,-46,73,-43,77,-41,80,-39,84,-36,87,-35,90,-33,93,-31,95,-30,97,-28,96,-173,94,-172,91,-171,88,-169,85,-168,82,-166,78,-164,74,-161,69,-159,64,-156,59,-153,54,-150,48,-146,42,-141,35,-136,29,-130,23,-124,19,-118,15,-111,13,-103,
            13,-96,15,-88,19,-81,23,-75,29,-69,35,-63,42,-58,48,-53,54,-50,59,-46,64,-43,69,-40,74,-38,78,-35,82,-33,85,-31,88,-30,91,-28,94,-27,96,-26,95,-176,93,-175,90,-174,87,-172,83,-171,80,-169,75,-167,71,-165,66,-163,60,-160,54,-157,48,-153,41,-150,33,-145,25,-140,17,-134,9,-127,3,-120,-1,-112,-4,-104,-4,-95,-1,-87,3,-79,9,-72,17,-65,25,-59,33,-54,41,-50,48,-46,54,-42,60,-39,
            66,-36,71,-34,75,-32,80,-30,83,-28,87,-27,90,-25,93,-24,95,-23,94,-179,92,-178,89,-177,85,-176,82,-174,78,-173,73,-171,68,-169,63,-167,56,-164,49,-161,42,-158,33,-154,23,-150,13,-144,3,-138,-7,-131,-17,-123,-24,-114,-28,-104,-28,-95,-24,-85,-17,-76,-7,-68,3,-61,13,-55,23,-50,33,-45,42,-41,49,-38,56,-35,63,-32,68,-30,73,-28,78,-26,82,-25,85,-23,89,-22,92,-21,94,-20,94,-182,91,-181,
            87,-180,84,-179,80,-178,76,-176,71,-175,65,-173,59,-171,52,-169,44,-166,35,-163,25,-159,13,-155,0,-150,-13,-143,-28,-136,-42,-127,-54,-116,-61,-105,-61,-94,-54,-83,-42,-72,-28,-63,-13,-56,0,-50,13,-44,25,-40,35,-36,44,-33,52,-30,59,-28,65,-26,71,-24,76,-23,80,-21,84,-20,87,-19,91,-18,94,-17,93,-185,90,-184,87,-183,83,-183,79,-182,74,-180,69,-179,63,-178,56,-176,49,-174,40,-171,29,-169,17,-165,
            3,-161,-13,-156,-33,-150,-54,-142,-76,-132,-96,-120,-108,-107,-108,-92,-96,-79,-76,-67,-54,-57,-33,-50,-13,-43,3,-38,17,-34,29,-30,40,-28,49,-25,56,-23,63,-21,69,-20,74,-19,79,-17,83,-16,87,-16,90,-15,93,-14,92,-188,89,-188,86,-187,82,-186,78,-185,73,-184,67,-183,61,-182,54,-181,45,-179,35,-177,23,-175,9,-172,-7,-168,-28,-163,-54,-157,-86,-150,-122,-139,-157,-125,-181,-109,-181,-90,-157,-74,-122,-60,-86,-50,
            -54,-42,-28,-36,-7,-31,9,-27,23,-24,35,-22,45,-20,54,-18,61,-17,67,-16,73,-15,78,-14,82,-13,86,-12,89,-11,92,-11,92,-191,89,-191,85,-190,81,-190,77,-189,72,-189,66,-188,59,-187,52,-186,43,-185,32,-183,19,-181,3,-179,-17,-176,-42,-172,-76,-167,-122,-160,-181,-150,-252,-134,-310,-112,-310,-87,-252,-65,-181,-50,-122,-39,-76,-32,-42,-27,-17,-23,3,-20,19,-18,32,-16,43,-14,52,-13,59,-12,66,-11,72,-10,
            77,-10,81,-9,85,-9,89,-8,92,-8,91,-195,88,-194,85,-194,81,-194,76,-193,71,-193,65,-192,58,-192,50,-191,41,-190,29,-190,15,-188,-1,-187,-24,-185,-54,-183,-96,-179,-157,-174,-252,-165,-403,-150,-593,-120,-593,-79,-403,-50,-252,-34,-157,-25,-96,-20,-54,-16,-24,-14,-1,-12,15,-11,29,-9,41,-9,50,-8,58,-7,65,-7,71,-6,76,-6,81,-5,85,-5,88,-5,91,-4,91,-198,88,-198,84,-198,80,-198,76,-197,70,-197,
            64,-197,58,-197,49,-197,40,-196,28,-196,13,-196,-4,-195,-28,-195,-61,-194,-108,-192,-181,-190,-310,-187,-593,-179,-1516,-150,-1516,-50,-593,-20,-310,-12,-181,-9,-108,-7,-61,-5,-28,-4,-4,-4,13,-3,28,-3,40,-3,49,-2,58,-2,64,-2,70,-2,76,-2,80,-1,84,-1,88,-1,91,-1,91,198,88,198,84,198,80,198,76,197,70,197,64,197,58,197,49,197,40,196,28,196,13,196,-4,195,-28,195,-61,194,-108,192,-181,190,
            -310,187,-593,179,-1516,150,-1516,50,-593,20,-310,12,-181,9,-108,7,-61,5,-28,4,-4,4,13,3,28,3,40,3,49,2,58,2,64,2,70,2,76,2,80,1,84,1,88,1,91,1,91,195,88,194,85,194,81,194,76,193,71,193,65,192,58,192,50,191,41,190,29,190,15,188,-1,187,-24,185,-54,183,-96,179,-157,174,-252,165,-403,150,-593,120,-593,79,-403,50,-252,34,-157,25,-96,20,-54,16,-24,14,-1,12,
            15,11,29,9,41,9,50,8,58,7,65,7,71,6,76,6,81,5,85,5,88,5,91,4,92,191,89,191,85,190,81,190,77,189,72,189,66,188,59,187,52,186,43,185,32,183,19,181,3,179,-17,176,-42,172,-76,167,-122,160,-181,150,-252,134,-310,112,-310,87,-252,65,-181,50,-122,39,-76,32,-42,27,-17,23,3,20,19,18,32,16,43,14,52,13,59,12,66,11,72,10,77,10,81,9,85,9,89,8,
            92,8,92,188,89,188,86,187,82,186,78,185,73,184,67,183,61,182,54,181,45,179,35,177,23,175,9,172,-7,168,-28,163,-54,157,-86,150,-122,139,-157,125,-181,109,-181,90,-157,74,-122,60,-86,50,-54,42,-28,36,-7,31,9,27,23,24,35,22,45,20,54,18,61,17,67,16,73,15,78,14,82,13,86,12,89,11,92,11,93,185,90,184,87,183,83,183,79,182,74,180,69,179,63,178,56,176,49,174,
            40,171,29,169,17,165,3,161,-13,156,-33,150,-54,142,-76,132,-96,120,-108,107,-108,92,-96,79,-76,67,-54,57,-33,50,-13,43,3,38,17,34,29,30,40,28,49,25,56,23,63,21,69,20,74,19,79,17,83,16,87,16,90,15,93,14,94,182,91,181,87,180,84,179,80,178,76,176,71,175,65,173,59,171,52,169,44,166,35,163,25,159,13,155,0,150,-13,143,-28,136,-42,127,-54,116,-61,105,-61,94,
            -54,83,-42,72,-28,63,-13,56,0,50,13,44,25,40,35,36,44,33,52,30,59,28,65,26,71,24,76,23,80,21,84,20,87,19,91,18,94,17,94,179,92,178,89,177,85,176,82,174,78,173,73,171,68,169,63,167,56,164,49,161,42,158,33,154,23,150,13,144,3,138,-7,131,-17,123,-24,114,-28,104,-28,95,-24,85,-17,76,-7,68,3,61,13,55,23,50,33,45,42,41,49,38,56,35,63,32,
            68,30,73,28,78,26,82,25,85,23,89,22,92,21,94,20,95,176,93,175,90,174,87,172,83,171,80,169,75,167,71,165,66,163,60,160,54,157,48,153,41,150,33,145,25,140,17,134,9,127,3,120,-1,112,-4,104,-4,95,-1,87,3,79,9,72,17,65,25,59,33,54,41,50,48,46,54,42,60,39,66,36,71,34,75,32,80,30,83,28,87,27,90,25,93,24,95,23,96,173,94,172,91,171,
            88,169,85,168,82,166,78,164,74,161,69,159,64,156,59,153,54,150,48,146,42,141,35,136,29,130,23,124,19,118,15,111,13,103,13,96,15,88,19,81,23,75,29,69,35,63,42,58,48,53,54,50,59,46,64,43,69,40,74,38,78,35,82,33,85,31,88,30,91,28,94,27,96,26,97,171,95,169,93,168,90,166,87,164,84,163,80,160,77,158,73,156,68,153,64,150,59,146,54,142,49,138,
            44,133,40,128,35,122,32,116,29,109,28,103,28,96,29,90,32,83,35,77,40,71,44,66,49,61,54,57,59,53,64,50,68,46,73,43,77,41,80,39,84,36,87,35,90,33,93,31,95,30,97,28,99,168,96,167,94,165,91,163,89,162,86,160,83,157,80,155,76,152,72,150,68,146,64,143,60,139,56,135,52,130,49,125,45,120,43,114,41,109,40,103,40,96,41,90,43,85,45,79,49,74,
            52,69,56,64,60,60,64,56,68,53,72,50,76,47,80,44,83,42,86,39,89,37,91,36,94,34,96,32,99,31,100,166,98,164,95,162,93,161,91,159,88,157,85,155,82,152,79,150,76,147,73,143,69,140,66,136,63,132,59,128,56,123,54,118,52,113,50,108,49,102,49,97,50,91,52,86,54,81,56,76,59,71,63,67,66,63,69,59,73,56,76,52,79,50,82,47,85,44,88,42,91,40,
            93,38,95,37,98,35,100,33,101,163,99,162,97,160,95,158,93,156,90,154,88,152,85,150,82,147,80,144,77,141,74,138,71,134,68,130,65,126,63,121,61,117,59,112,58,107,58,102,58,97,58,92,59,87,61,82,63,78,65,73,68,69,71,65,74,61,77,58,80,55,82,52,85,50,88,47,90,45,93,43,95,41,97,39,99,37,101,36,102,161,100,159,98,158,96,156,94,154,92,152,90,150,
            88,147,85,144,83,142,80,139,78,135,75,132,73,128,71,124,69,120,67,116,66,111,65,107,64,102,64,97,65,92,66,88,67,83,69,79,71,75,73,71,75,67,78,64,80,60,83,57,85,55,88,52,90,50,92,47,94,45,96,43,98,41,100,40,102,38,103,159,102,157,100,155,98,154,96,152,94,150,92,147,90,145,88,142,86,139,84,136,82,133,80,130,78,126,76,123,74,119,73,115,72,110,
            71,106,70,102,70,97,71,93,72,89,73,84,74,80,76,76,78,73,80,69,82,66,84,63,86,60,88,57,90,54,92,52,94,50,96,47,98,45,100,44,102,42,103,40,104,157,103,155,101,153,100,151,98,150,96,147,94,145,93,143,91,140,89,137,87,135,85,131,83,128,82,125,80,121,79,117,78,114,77,110,76,106,76,102,76,97,76,93,77,89,78,85,79,82,80,78,82,74,83,71,85,68,
            87,64,89,62,91,59,93,56,94,54,96,52,98,50,100,48,101,46,103,44,104,42,106,155,104,153,103,151,101,150,100,148,98,145,96,143,95,141,93,138,91,136,90,133,88,130,87,127,85,123,84,120,83,116,82,113,81,109,81,105,80,101,80,98,81,94,81,90,82,86,83,83,84,79,85,76,87,72,88,69,90,66,91,63,93,61,95,58,96,56,98,54,100,51,101,50,103,48,104,46,106,44,
            107,153,105,151,104,150,103,148,101,146,100,144,98,141,97,139,95,137,94,134,93,131,91,128,90,125,89,122,87,119,87,116,86,112,85,109,85,105,84,101,84,98,85,94,85,90,86,87,87,83,87,80,89,77,90,74,91,71,93,68,94,65,95,62,97,60,98,58,100,55,101,53,103,51,104,50,105,48,107,46,108,151,107,150,105,148,104,146,103,144,102,142,100,140,99,137,98,135,96,132,95,130,
            94,127,93,124,92,121,91,118,90,115,89,111,89,108,88,105,88,101,88,98,88,94,89,91,89,88,90,84,91,81,92,78,93,75,94,72,95,69,96,67,98,64,99,62,100,59,102,57,103,55,104,53,105,51,107,50,108,48,109,150,108,148,107,146,106,144,104,142,103,140,102,138,101,136,100,133,99,131,97,128,96,126,95,123,94,120,94,117,93,114,92,111,92,108,91,104,91,101,91,98,91,95,
            92,91,92,88,93,85,94,82,94,79,95,76,96,73,97,71,99,68,100,66,101,63,102,61,103,59,104,57,106,55,107,53,108,51,109,50,
        };

        int[] SinLookup = {
            0,174,348,523,697,871,1045,1218,1391,1564,1736,1908,2079,2249,2419,2588,2756,2923,3090,3255,3420,3583,3746,3907,4067,4226,4383,4539,4694,4848,5000,5150,5299,5446,5591,5735,5877,6018,6156,6293,6427,6560,6691,6819,6946,7071,
            7193,7313,7431,7547,7660,7771,7880,7986,8090,8191,8290,8386,8480,8571,8660,8746,8829,8910,8987,9063,9135,9205,9271,9335,9396,9455,9510,9563,9612,9659,9702,9743,9781,9816,9848,9876,9902,9925,9945,9961,9975,9986,9993,9998,9999,
            9998,9993,9986,9975,9961,9945,9925,9902,9876,9848,9816,9781,9743,9702,9659,9612,9563,9510,9455,9396,9335,9271,9205,9135,9063,8987,8910,8829,8746,8660,8571,8480,8386,8290,8191,8090,7986,7880,7771,7660,7547,7431,7313,7193,7071,
            6946,6819,6691,6560,6427,6293,6156,6018,5877,5735,5591,5446,5299,5150,5000,4848,4694,4539,4383,4226,4067,3907,3746,3583,3420,3255,3090,2923,2756,2588,2419,2249,2079,1908,1736,1564,1391,1218,1045,871,697,523,348,174,0,
            -174,-348,-523,-697,-871,-1045,-1218,-1391,-1564,-1736,-1908,-2079,-2249,-2419,-2588,-2756,-2923,-3090,-3255,-3420,-3583,-3746,-3907,-4067,-4226,-4383,-4539,-4694,-4848,-5000,-5150,-5299,-5446,-5591,-5735,-5877,-6018,-6156,-6293,-6427,-6560,-6691,-6819,-6946,-7071,
            -7193,-7313,-7431,-7547,-7660,-7771,-7880,-7986,-8090,-8191,-8290,-8386,-8480,-8571,-8660,-8746,-8829,-8910,-8987,-9063,-9135,-9205,-9271,-9335,-9396,-9455,-9510,-9563,-9612,-9659,-9702,-9743,-9781,-9816,-9848,-9876,-9902,-9925,-9945,-9961,-9975,-9986,-9993,-9998,-9999,
            -9998,-9993,-9986,-9975,-9961,-9945,-9925,-9902,-9876,-9848,-9816,-9781,-9743,-9702,-9659,-9612,-9563,-9510,-9455,-9396,-9335,-9271,-9205,-9135,-9063,-8987,-8910,-8829,-8746,-8660,-8571,-8480,-8386,-8290,-8191,-8090,-7986,-7880,-7771,-7660,-7547,-7431,-7313,-7193,-7071,
            -6946,-6819,-6691,-6560,-6427,-6293,-6156,-6018,-5877,-5735,-5591,-5446,-5299,-5150,-4999,-4848,-4694,-4539,-4383,-4226,-4067,-3907,-3746,-3583,-3420,-3255,-3090,-2923,-2756,-2588,-2419,-2249,-2079,-1908,-1736,-1564,-1391,-1218,-1045,-871,-697,-523,-348,-174,
        };


        public void SaveDisparities(
            string filename,
            int stereo_matches)
        {
            int max_disp_pixels = 40 * (int)imgWidth / 100;
            byte[] img = new byte[imgWidth*imgHeight*3];
            Bitmap bmp = new Bitmap((int)imgWidth, (int)imgHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            for (int i = 0; i < stereo_matches; i++)
            {
                int x = svs_matches[i*4+1];
                int y = svs_matches[i*4+2];
                int disp = svs_matches[i*4+3];
                int radius = 1 + (disp/8);
                int v = disp * 255 / max_disp_pixels;
                if (v > 255) v = 255;
                drawing.drawSpot(img, (int)imgWidth, (int)imgHeight, x,y,radius,v,v,v);
            }
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.EndsWith("bmp")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.EndsWith("jpg")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filename.EndsWith("gif")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
            if (filename.EndsWith("png")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }
        
        public void SimulateStereoView(
            int max_disparity_percent,
            int map_x, int map_y,
            byte[] cartesian_map,
            ref int stereo_matches)
        {
            int max_disp_pixels = max_disparity_percent * (int)imgWidth / 100;
            int ground_y = (int)imgHeight / 2;

            if (svs_matches == null) svs_matches = new int[SVS_MAX_FEATURES*4];
            
            stereo_matches = 0;
            for (int cell_y = 0; cell_y < map_y; cell_y++)
            {
                int y = (map_y/2) - cell_y;
                int y_mm = y * SVS_MAP_CELL_SIZE_MM;
                if (y_mm > 0)
                {
                    for (int cell_x = 0; cell_x < map_x; cell_x++)
                    {
                        int x = cell_x - (map_x/2);
                        if (cartesian_map[(cell_y*map_x + cell_x)*3] != 0)
                        {
                            int x_mm = x * SVS_MAP_CELL_SIZE_MM;
                            Console.WriteLine("x = " + x.ToString());
                            Console.WriteLine("x_mm = " + x_mm.ToString());
                            Console.WriteLine("y_mm = " + y_mm.ToString());
                            float angle_degrees = (float)(Math.Atan(x_mm / (float)y_mm) * 180.0 / Math.PI);
                            Console.WriteLine("angle = " + angle_degrees.ToString());
                            if ((angle_degrees > -SVS_FOV_DEGREES/2) &&
                                (angle_degrees < SVS_FOV_DEGREES/2))
                            {
                                int disparityx100_mm = (SVS_BASELINE_MM*SVS_FOCAL_LENGTH_MMx100) / y_mm;
                                int disparity = disparityx100_mm * (int)imgWidth / svs_sensor_width_mmx100;
                                int image_x = (int)((angle_degrees  + (SVS_FOV_DEGREES/2)) * (int)imgWidth / SVS_FOV_DEGREES);
                                int image_y = (int)imgHeight - 1 - (ground_y - (ground_y * disparity / max_disp_pixels));
                                Console.WriteLine("image_x = " + image_x.ToString());
                                for (int yy = image_y; yy > 0; yy -= 4)
                                {
                                    if (stereo_matches == SVS_MAX_FEATURES) break;

                                    svs_matches[stereo_matches*4 + 1] = image_x;
                                    svs_matches[stereo_matches*4 + 2] = yy;
                                    svs_matches[stereo_matches*4 + 3] = disparity;
                                    stereo_matches++;
                                }
                            }
                        }
                    }
                }
            }
        }
                
        public void Recenter()
        {
            /* compute change in translation */
            int dx = robot_x_mm - map_x_mm;
            int dy = robot_y_mm - map_y_mm;
            
            if ((dx < -SVS_MAP_CELL_SIZE_MM) || (dx > SVS_MAP_CELL_SIZE_MM) ||
                (dy < -SVS_MAP_CELL_SIZE_MM) || (dy > SVS_MAP_CELL_SIZE_MM)) {
        
                int array_length = (SVS_MAP_RDIM + SVS_MAP_HYPRDIM)*SVS_MAP_TDIM*2;
                int n, i, offset, cell_x_mm, cell_y_mm, cell_x, cell_y, rP, tP;
                byte[] buffer_occupancy = new byte[array_length];
                int[] buffer_coords = new int[array_length];
                int offset2 = -SVS_MAP_TDIM*1/4;
        
                /* clear the buffer */
                for (i = array_length-2; i >= 0; i-=2) buffer_coords[i] = SVS_EMPTY_CELL;
                for (i = buffer_occupancy.Length-1; i >= 0; i--) buffer_occupancy[i]=0;
        
                /* update map cells */
                offset = SVS_MAP_WIDTH_CELLS/2;    
                for (i = array_length-2; i >= 0; i -= 2) {
                    if (Hpolar_map_coords[i] != SVS_EMPTY_CELL) {
                    
                        /* update cell position */
                        cell_x_mm = Hpolar_map_coords[i] - map_x_mm - dx;
                        cell_y_mm = Hpolar_map_coords[i+1] - map_y_mm - dy;
                    
                        /* convert millimetres to a cartesian grid coordinate */
                        cell_x = offset + (cell_x_mm / SVS_MAP_CELL_SIZE_MM);
                        cell_y = offset + (cell_y_mm / SVS_MAP_CELL_SIZE_MM);
                        if ((cell_x >= 0) && (cell_x < SVS_MAP_WIDTH_CELLS) &&
                            (cell_y >= 0) && (cell_y < SVS_MAP_WIDTH_CELLS)) {  
                                           
                            /* convert from cartesian grid to H-polar grid using lookup */
                            n = (cell_y*SVS_MAP_WIDTH_CELLS + cell_x)*2;
                            rP = HpolarLookup[n];
                            tP = HpolarLookup[n+1] + offset2;
                            if (tP < 0) tP += SVS_MAP_TDIM;
                            if (tP >= SVS_MAP_TDIM) tP -= SVS_MAP_TDIM;
                            n = (rP*SVS_MAP_TDIM + tP)*2;
                            buffer_occupancy[n] = Hpolar_map_occupancy[i];
                            buffer_occupancy[n+1] = Hpolar_map_occupancy[i+1];
                            buffer_coords[n] = cell_x_mm + map_x_mm;
                            buffer_coords[n+1] = cell_y_mm + map_y_mm;
                        }
                    }
                }
            
                /* copy buffer back to the map */
                Buffer.BlockCopy(buffer_coords, 0, Hpolar_map_coords, 0, array_length*sizeof(int));
                Buffer.BlockCopy(buffer_occupancy, 0, Hpolar_map_occupancy, 0, array_length);
            
                /* set the new map centre position */
                map_x_mm = robot_x_mm;
                map_y_mm = robot_y_mm;
                
                prev_dist_moved_mm = dist_moved_mm;                
            }
        }

        public void GroundPlaneUpdate()
        {
            int i, j, x, y, diff, prev_y = 0, prev_i=0;
                
            /* Remove points which have a large vertical difference.
               Successive points with small vertical difference are likely
               to correspond to real borders rather than carpet patterns, etc*/
            int max_diff = (int)imgHeight / 30;        
            for (i = 0; i < (int)imgWidth / SVS_HORIZONTAL_SAMPLING; i++) {
                x = i * SVS_HORIZONTAL_SAMPLING;
                y = footline[i];
                if (y != 0) {
                    if (prev_y != 0) {
                        diff = y - prev_y;
                        if (diff < 0) diff = -diff;
                        if (diff > max_diff) {
                            if (y < prev_y) {
                                footline[prev_i] = 0;
                            }
                            else {
                                footline[i] = 0;
                            }
                        }
                    }
                    prev_i = i;
                    prev_y = y;
                }
            }
            
            /* fill in missing data to create a complete ground plane */
            prev_i = 0;
            prev_y = 0;
            int max = (int)imgWidth / SVS_HORIZONTAL_SAMPLING;
            for (i = 0; i < max; i++) {
                x = i * SVS_HORIZONTAL_SAMPLING;
                y = footline[i];
                if (y != 0) {
                    if (prev_y == 0) prev_y = y;
                    for (j = prev_i; j < i; j++) {
                        footline[j] = (ushort)(prev_y + ((j - prev_i) * (y - prev_y) / (i - prev_i)));
                    }
                    prev_y = y;
                    prev_i = i;
                }
            }
            for (j = prev_i; j < max; j++) {
                footline[j] = (ushort)prev_y;
            }
        }
        
        public void FootlineUpdate(
            int max_disparity_percent)
        {
            int range_const = SVS_FOCAL_LENGTH_MMx100 * SVS_BASELINE_MM;
            int max = (int)imgWidth / (int)SVS_HORIZONTAL_SAMPLING;
            int i, x, y, disp, disp_mmx100, y_mm;
            int ground_y = (int)imgHeight * svs_ground_y_percent/100;
            int ground_y_sloped=0, ground_height_sloped=0;    
            int half_width = (int)imgWidth/2;
            int max_disp_pixels = max_disparity_percent * (int)imgWidth / 100;    
            int forward_movement_mm = 0;
            int forward_movement_hits = 0;
            
            if ((footline == null) || 
                (prev_imgWidth != imgWidth))
            {
                footline = new ushort[imgWidth / SVS_HORIZONTAL_SAMPLING];
                footline_dist_mm = new ushort[imgWidth / SVS_HORIZONTAL_SAMPLING];
            }        
            prev_imgWidth = imgWidth;
                
            for (i = 0; i < max; i++) {
                x = i * SVS_HORIZONTAL_SAMPLING;
                y = footline[i];
                ground_y_sloped = ground_y + ((half_width - x) * svs_ground_slope_percent / 100);
                ground_height_sloped = (int)imgHeight - 1 - ground_y_sloped;
                disp = (y - ground_y_sloped) * max_disp_pixels / ground_height_sloped;
                disp_mmx100 = disp * svs_sensor_width_mmx100 / (int)imgWidth;
                if (disp_mmx100 > 0) {
                    // get position of the feature in space 
                    y_mm = range_const / disp_mmx100;
        
                    if (footline_dist_mm[i] != 0) {
                        forward_movement_mm += y_mm - footline_dist_mm[i];
                        forward_movement_hits++;
                    }
                    footline_dist_mm[i] = (ushort)y_mm;
                }
                else {
                    footline_dist_mm[i] = 0;
                }
            }    
            if (forward_movement_hits > 0) {
                forward_movement_mm /= forward_movement_hits;
            }
        }

        public void MapUpdate(
            int no_of_matches,
            int max_disparity_percent)
        {
            int i, j, d, w, rot, x, y, disp, disp_radius;
            int x_mm, y_mm, curr_y_mm, x_rotated_mm, y_rotated_mm, disp_mmx100;
            int rP, tP, r, n, SinVal, CosVal, on_ground_plane;
            int cell_x, cell_y;
            int range_const = SVS_FOCAL_LENGTH_MMx100 * SVS_BASELINE_MM;
            int max_disp_pixels = max_disparity_percent * (int)imgWidth / 100;    
            int half_width = (int)imgWidth/2;
            int max_r = SVS_MAP_RDIM + SVS_MAP_HYPRDIM;
            int polar_stride = SVS_MAP_TDIM*2;
            int offset2 = -SVS_MAP_TDIM*1/4;
            int dx = robot_x_mm - map_x_mm;
            int dy = robot_y_mm - map_y_mm;
            int offset = SVS_MAP_WIDTH_CELLS/2;
            int array_length = (SVS_MAP_RDIM + SVS_MAP_HYPRDIM)*SVS_MAP_TDIM*2;

            if ((Hpolar_map_occupancy == null) ||
                (array_length != prev_array_length))
            {
                Hpolar_map_occupancy = new byte[array_length];
                Hpolar_map_coords = new int[array_length];
            }
            prev_array_length = array_length;

            if (svs_matches == null)
            {
                svs_matches = new int[SVS_MAX_FEATURES*4];
            }
            
            for (i = 0; i < no_of_matches*4; i += 4) {
                if (svs_matches[i] > 0) {  // match probability > 0
                    x = svs_matches[i + 1];
                    y = svs_matches[i + 2];
                    if (y <= footline[x / SVS_HORIZONTAL_SAMPLING])
                        on_ground_plane = 0;
                    else
                        on_ground_plane = 1;
                    
                    disp = svs_matches[i + 3];
                    disp_mmx100 = disp * svs_sensor_width_mmx100 / (int)imgWidth;
                    if (disp_mmx100 > 0) {
                        // get position of the feature in space
                        y_mm = range_const / disp_mmx100;
                        
                        //if (y_mm > max_range) y = max_range; // hack!
        
                        disp_radius = disp/2;
                        if (disp_radius > 10) disp_radius = 10;
                        curr_y_mm = y_mm;                    
                        int rot_off;
                        int max_rot_off = (int)SVS_FOV_DEGREES/2;
                                        
                        rot_off = (x - half_width) * (int)SVS_FOV_DEGREES / (int)imgWidth;
                        if ((rot_off > -max_rot_off) && (rot_off < max_rot_off)) {
                            rot = robot_orientation_degrees + rot_off; 
        
                            CosVal = 90 - rot;
                            if (CosVal < 0) CosVal += 360;
                            CosVal = SinLookup[CosVal];
                            SinVal = SinLookup[rot];
                            
                            // rotate by the orientation of the robot 
                            y_rotated_mm = SinVal * curr_y_mm / (int)10000;
                            x_rotated_mm = CosVal * curr_y_mm / (int)10000;                        
                    
                            // convert millimetres to a cartesian grid coordinate 
                            cell_x = offset + ((x_rotated_mm + dx) / (int)SVS_MAP_CELL_SIZE_MM);
                            cell_y = offset + ((y_rotated_mm + dy) / (int)SVS_MAP_CELL_SIZE_MM);
                            if ((cell_x >= 0) && (cell_x < SVS_MAP_WIDTH_CELLS) &&
                                (cell_y >= 0) && (cell_y < SVS_MAP_WIDTH_CELLS)) { 
                                
                                w = disp * 40 / max_disp_pixels;
                                    
                                // absolute position of the point in cartesian space 
                                x_mm = x_rotated_mm + robot_x_mm;
                                y_mm = y_rotated_mm + robot_y_mm;
                                        
                                // convert from cartesian grid to H-polar grid using lookup 
                                n = (cell_y*SVS_MAP_WIDTH_CELLS + cell_x)*2;
                                rP = HpolarLookup[n];
                                d = HpolarLookup[n+1] + offset2;
                                
                                for (j = d-w; j <= d+w; j++) {
                                    tP = j;
                                    if (tP >= SVS_MAP_TDIM) tP -= SVS_MAP_TDIM;
                                    if (tP < 0) tP += SVS_MAP_TDIM;
                        
                                    // vacancy 
                                    n = tP*2;
                                    int prob = 5;
                                    if (prob > rP) prob = rP;
                                    for (r = 0; r < rP-1; r++, n += polar_stride) {
                                        if (Hpolar_map_coords[n] == SVS_EMPTY_CELL) {
                                            Hpolar_map_coords[n] = x_mm;
                                            Hpolar_map_coords[n+1] = y_mm;
                                        }
                                        else {
                                            Hpolar_map_coords[n] += (x_mm - Hpolar_map_coords[n])/2;
                                            Hpolar_map_coords[n+1] += (y_mm - Hpolar_map_coords[n+1])/2;
                                        }
                                        if (Hpolar_map_occupancy[n] < 245) {
                                            // increment vacancy                                     
                                            Hpolar_map_occupancy[n] += (byte)prob;
                                            prob--;
                                            if (prob < 1) prob = 1;
                                        }
                                        else {
                                            // decrement occupancy 
                                            if (Hpolar_map_occupancy[n+1] > 0) Hpolar_map_occupancy[n+1]--;
                                        }                    
                                    }
                                    if (on_ground_plane == 0) {
                                        // occupancy
                                        n++;
                                        prob = 10;
                                        while (r < rP+2+w) {
                                            if (Hpolar_map_coords[n-1] == SVS_EMPTY_CELL) {
                                                Hpolar_map_coords[n-1] = x_mm;
                                                Hpolar_map_coords[n] = y_mm;
                                            }
                                            else {
                                                Hpolar_map_coords[n-1] += (x_mm - Hpolar_map_coords[n-1])/2;
                                                Hpolar_map_coords[n] += (y_mm - Hpolar_map_coords[n])/2;
                                            }
                                            if (r < max_r) {
                                                if (Hpolar_map_occupancy[n] < 245) {
                                                    // increment occupancy (very crude sensor model)
                                                    Hpolar_map_occupancy[n] += (byte)prob;
                                                    prob--;
                                                    if (prob < 1) prob = 1;
                                                }
                                                else {
                                                    // decrement vacancy 
                                                    if (Hpolar_map_occupancy[n-1] > 0) 
                                                        Hpolar_map_occupancy[n-1]--;
                                                }
                                            }
                                            else {
                                                break;
                                            }
                                            n += polar_stride;
                                            r++;
                                        }
                                    }
                                }
                            }
                        }
                    }            
                }
            }                
        }

        public void Show(
            string filename)
        {
            int image_width = 640;
            int image_height = 480;
            byte[] img = new byte[image_width * image_height * 3];
            Show(image_width, image_height, img);
            Bitmap bmp = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.EndsWith("bmp")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.EndsWith("png")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            if (filename.EndsWith("gif")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
            if (filename.EndsWith("jpg")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        
        public void Show(
            int image_width,
            int image_height,
            byte[] outbuf)
        {
            int tP, rP,x,y,dx,dy,CosVal,r,n,n2;
            int centre_x = (int)imgWidth/2;
            int centre_y = (int)imgHeight/2;
            int radius = ((int)imgHeight/2)-1;
            int orient, orient2;
            
            /* clear the map */
            rP = radius*radius;
            for (y = centre_y - radius; y < centre_y + radius; y++) {
                dy = y - centre_y;
                for (x = centre_x - radius; x < centre_x + radius; x++) {
                    dx = x - centre_x;
                    r = dx*dx + dy*dy;
                    if (r < rP) {
                        /* black */
                        n = (y*(int)imgWidth + x) * 3;
                        outbuf[n] = 0;
                        outbuf[n+1] = 0;
                        outbuf[n+2] = 0;
                    }
                }
            }
            
            /* draw occupancy and vacancy */
            for (orient = 0; orient < 360; orient++) {
                
                orient2 = orient + robot_orientation_degrees;
                tP = (orient2 * (int)SVS_MAP_TDIM / (int)360);
                if (tP < 0) tP += (int)SVS_MAP_TDIM;
                if (tP >= (int)SVS_MAP_TDIM) tP -= (int)SVS_MAP_TDIM;
                
                dx = radius * SinLookup[orient] / (int)10000;
                CosVal = 90 - orient;
                if (CosVal < 0) CosVal += 360;
                dy = radius * SinLookup[CosVal] / (int)10000;
                for (r = 0; r < radius; r++) {
                    rP = r * (SVS_MAP_RDIM + SVS_MAP_HYPRDIM-1) / radius;
                    x = centre_x + (dx * r / radius);
                    y = centre_y - (dy * r / radius);
                    n = ((y*(int)imgWidth) + x) * 3;
                    n2 = (rP*SVS_MAP_TDIM + tP) * 2;
                    if (Hpolar_map_coords[n2] != SVS_EMPTY_CELL) {
                        if (Hpolar_map_occupancy[n2] >= Hpolar_map_occupancy[n2+1]) {
                            /* vacant */
                            outbuf[n] = 0;
                            outbuf[n+1] = 255;
                            outbuf[n+2] = 0;
                        }
                        else {
                            /* occupied */
                            outbuf[n] = 0;
                            outbuf[n+1] = 0;
                            outbuf[n+2] = 255;
                        }
                    }
                }
            }            
        }
                
        
        /// <summary>
        /// Returns radius of the first hyperbolic cell
        /// </summary>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="hCam">Height of the camera</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="hRmin">Height of the image at radius Rmin</param>
        /// <returns>Radius of the first hyperbolic cell</returns>
        public static float CalcRmin(
            float Cres,
            float hCam,
            float hypRdim,
            float hRmin)
        {
            return(Cres * (((hCam * hypRdim) / hRmin) - 1));
        }
        
        /// <summary>
        /// Converts from cartesian coords to H-polar
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <param name="z_mm"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="rP">radial position</param>
        /// <param name="tP"></param>
        /// <param name="zP">height</param>
        public static void XYZtoPolar(
            float x_mm,
            float y_mm,
            float z_mm,
            float Cres,
            float Rmin,
            float hCam,
            float hRmin,
            float Rdim,
            float hypRdim,
            float Tdim,                                      
            ref float rP,
            ref float tP,
            ref float zP)
        {
            zP = z_mm;
            rP = (((Rmin * (zP - hCam)) / (float)Math.Sqrt(x_mm*x_mm + y_mm*y_mm)) + hCam) *
                 (hypRdim / hRmin) + Rdim;
            tP = (float)(Math.Atan2(y_mm,x_mm) * Tdim / (2*Math.PI));
        }

        /// <summary>
        /// Convert from H-polar coords to cartesian
        /// </summary>
        /// <param name="rP"></param>
        /// <param name="tP"></param>
        /// <param name="zP"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <param name="z_mm"></param>
        public static void PolartoXYZ(
            float rP,
            float tP,
            float zP,
            float Cres,
            float Rmin,
            float hCam,
            float hRmin,
            float Rdim,
            float hypRdim,
            float Tdim,
            ref float x_mm,
            ref float y_mm,
            ref float z_mm)
        {
            float radiusP = (Rmin * (zP - hCam)) / ((rP - Rdim) * (hRmin/hypRdim) - hCam);
            float alpha = tP * 2 * (float)Math.PI / Tdim;
            x_mm = (float)Math.Cos(alpha) * radiusP;
            y_mm = (float)Math.Sin(alpha) * radiusP;
            z_mm = zP;
        }


        /// <summary>
        /// creates a lookup table to convert between cartesian and H-polar coords
        /// </summary>
        /// <param name="cartesian_dimension_cells_width">width of the cartesian grid map in cells</param>
        /// <param name="cartesian_dimension_cells_range">range of the cartesian grid map in cells</param>
        /// <param name="cartesian_cell_size_mm"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="Hpolar">returned lookup table</param>
        public static void CreateHpolarLookup(
            int cartesian_dimension_cells_width,
            int cartesian_dimension_cells_range,
            float cartesian_cell_size_mm,
            float Cres,
            float Rmin,
            float hCam,
            float hRmin,
            float Rdim,
            float hypRdim,
            float Tdim,
            ref int[] Hpolar)
        {
            float rP=0, tP=0, zP=0;

            if (Hpolar == null)
                Hpolar = new int[cartesian_dimension_cells_width * cartesian_dimension_cells_range * 2];

            XYZtoPolar(
                0, 100, 0,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                ref rP, ref tP, ref zP);
            Console.WriteLine("0,100  tP = " + tP.ToString());
            XYZtoPolar(
                100, 0, 0,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                ref rP, ref tP, ref zP);
            Console.WriteLine("100,0  tP = " + tP.ToString());
            XYZtoPolar(
                0, -100, 0,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                ref rP, ref tP, ref zP);
            Console.WriteLine("0,-100  tP = " + tP.ToString());
            
            int n = 0;
            for (int y = 0; y < cartesian_dimension_cells_range; y++)
            {
                float y_mm = ((y+0.5f) - (cartesian_dimension_cells_range/2)) * cartesian_cell_size_mm;
                for (int x = 0; x < cartesian_dimension_cells_width; x++, n += 2)
                {
                    float x_mm = ((x+0.5f) - (cartesian_dimension_cells_width/2)) * cartesian_cell_size_mm;
                    XYZtoPolar(
                        x_mm, y_mm, 0,
                        Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,                                      
                        ref rP, ref tP, ref zP);
                    Hpolar[n] = (int)rP;
                    Hpolar[n+1] = (int)tP;
                }
            }
        }


        /// <summary>
        /// creates a lookup table to convert between cartesian and H-polar coords
        /// suitable for the SRV robot
        /// </summary>
        /// <param name="cartesian_dimension_cells_width">width of the cartesian grid map in cells</param>
        /// <param name="cartesian_dimension_cells_range">range of the cartesian grid map in cells</param>
        /// <param name="cartesian_cell_size_mm"></param>
        /// <param name="Cres">Cell radial size in the constant area</param>
        /// <param name="Rmin">Radius of the first hyperbolic cell</param>
        /// <param name="hCam">Height of the pseudo-camera</param>
        /// <param name="hRmin">Height of the pseudo-image at radius Rmin</param>
        /// <param name="Rdim">Number of cells in the constant area</param>
        /// <param name="hypRdim">Number of cells in the hyperbolic area</param>
        /// <param name="Tdim">Number of cells in the angular dimension</param>
        /// <param name="Hpolar">returned lookup table</param>
        public static void CreateHpolarLookupSRV(
            int cartesian_dimension_cells_width,
            int cartesian_dimension_cells_range,
            float cartesian_cell_size_mm,
            float scale_down_factor,
            ref int[] Hpolar)
        {
            float rP=0, tP=0, zP=0;
            float Cres = 200 / scale_down_factor;
            float hCam = 1000 / scale_down_factor;
            float hRmin = 970 / scale_down_factor;
            float Rdim = 75; //30;
            float hypRdim = 75; //30;
            float Tdim = 400; //200;

            float Rmin = (int)CalcRmin(Cres, hCam, hypRdim, hRmin);

            CreateHpolarLookup(
                cartesian_dimension_cells_width,
                cartesian_dimension_cells_range,
                cartesian_cell_size_mm,
                Cres, Rmin, hCam, hRmin, Rdim, hypRdim, Tdim,
                ref Hpolar);
        }

        public static void ShowHpolar(
            int cartesian_dimension_cells_width,
            int cartesian_dimension_cells_range,
            float cartesian_cell_size_mm,
            int[] Hpolar,
            int image_width,
            float scale_down_factor,
            string filename)
        {
            float Cres = 200 / scale_down_factor;
            float hCam = 1000 / scale_down_factor;
            float hRmin = 970 / scale_down_factor;
            float Rdim = 75; //30;
            float hypRdim = 75; //30;
            float Tdim = 400; //200;
            float Rmin = (int)CalcRmin(Cres, hCam, hypRdim, hRmin);
            Console.WriteLine("Rmin " + Rmin.ToString());
            float rP=0, tP=0, zP=0;
            float x_mm=0, y_mm=0, z_mm=0;
            float min_x = float.MaxValue;
            float max_x = float.MinValue;
            float min_y = float.MaxValue;
            float max_y = float.MinValue;
            List<float> positions = new List<float>();
            List<int> colours = new List<int>();
            int r=0,g=0,b=0;

            for (int i = 0; i < Hpolar.Length; i += 2)
            {
                rP = Hpolar[i];
                tP = Hpolar[i+1];

                int val = (int)Math.Abs(rP*tP) % 7;
                //int val = (int)Math.Abs(rP) % 7;
                switch(val)
                {
                    case 0: { r=255; g=0; b=0; break; }
                    case 1: { r=0; g=255; b=0; break; }
                    case 2: { r=0; g=0; b=255; break; }
                    case 3: { r=255; g=0; b=255; break; }
                    case 4: { r=255; g=255; b=0; break; }
                    case 5: { r=0; g=255; b=255; break; }
                    case 6: { r=0; g=0; b=0; break; }
                }
                colours.Add(r);
                colours.Add(g);
                colours.Add(b);
                
                PolartoXYZ(
                    rP, tP, zP,
                    Cres, Rmin, hCam, hRmin, 
                    Rdim, hypRdim, Tdim,
                    ref x_mm,
                    ref y_mm,
                    ref z_mm);

                positions.Add(x_mm);
                positions.Add(y_mm);

                if (x_mm < min_x) min_x = x_mm;
                if (x_mm > min_x) max_x = x_mm;
                if (y_mm < min_y) min_y = y_mm;
                if (y_mm > min_y) max_y = y_mm;
            }
            Console.WriteLine("Min x: " + min_x.ToString());
            Console.WriteLine("Min y: " + min_y.ToString());
            Console.WriteLine("Max x: " + max_x.ToString());
            Console.WriteLine("Max y: " + max_y.ToString());

            byte[] img = new byte[image_width * image_width * 3];
            for (int i = 0; i < img.Length; i++) img[i] = 255;
            Bitmap bmp = new Bitmap(image_width, image_width, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int n = 0;
            for (int i = 0; i < positions.Count; i += 2, n += 3)
            {
                x_mm = positions[i];
                y_mm = positions[i + 1];                
                int x = (int)((x_mm - min_x) * image_width / (max_x - min_x));
                int y = (int)((y_mm - min_y) * image_width / (max_y - min_y));
                drawing.drawSpot(img, image_width, image_width, x, y, 4, colours[n],colours[n+1],colours[n+2]);
            }
            BitmapArrayConversions.updatebitmap_unsafe(img, bmp);
            if (filename.EndsWith("bmp")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            if (filename.EndsWith("jpg")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (filename.EndsWith("gif")) bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Gif);
        }
        
        public static void SaveHpolar(
            int[] Hpolar,
            string filename)
        {
            StreamWriter oWrite=null;
            bool allowWrite = true;

            try
            {
                oWrite = File.CreateText(filename);
            }
            catch
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                oWrite.WriteLine("int HpolarLookup[] = {");
                int i = 0;
                for (int n = 0; n < Hpolar.Length; n += 2, i++)
                {
                    oWrite.Write(Hpolar[n].ToString() + "," + Hpolar[n+1].ToString() + ",");
                    if (i > 50)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.Close();
            }

        }
        
        public static void SaveTrigLookup(
            string filename)
        {
            const int mult = 10000;
            StreamWriter oWrite=null;
            bool allowWrite = true;

            try
            {
                oWrite = File.CreateText(filename);
            }
            catch
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                oWrite.WriteLine("int SinLookup[] = {");
                int n, i = 0;
                for (n = 0; n < 360; n++, i++)
                {
                    float angle = n * (float)Math.PI / 180.0f;
                    int val = (int)(Math.Sin(angle) * mult);
                    oWrite.Write(val.ToString() + ",");
                    if (i >= 45)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.WriteLine("");
                oWrite.WriteLine("int CosLookup[] = {");
                i = 0;
                for (n = 0; n < 360; n++, i++)
                {
                    float angle = n * (float)Math.PI / 180.0f;
                    int val = (int)(Math.Cos(angle) * mult);
                    oWrite.Write(val.ToString() + ",");
                    if (i >= 45)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.WriteLine("");
                oWrite.WriteLine("int TanLookup[] = {");
                i = 0;
                for (n = 0; n < 360; n++, i++)
                {
                    float angle = n * (float)Math.PI / 180.0f;
                    int val = (int)(Math.Tan(angle) * mult);
                    oWrite.Write(val.ToString() + ",");
                    if (i >= 45)
                    {
                        oWrite.WriteLine("");
                        i = 0;
                    }
                }
                oWrite.WriteLine("");
                oWrite.WriteLine("};");
                oWrite.Close();
            }

        }

    }

}
