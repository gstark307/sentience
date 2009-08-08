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

            int n = 0;
            for (int y = 0; y < cartesian_dimension_cells_range; y++)
            {
                //float y_mm = (y+0.5f) * cartesian_cell_size_mm;
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
            float Rdim = 30;
            float hypRdim = 30;
            float Tdim = 200;

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
            float Rdim = 30;
            float hypRdim = 30;
            float Tdim = 200;
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
        
        
    }
}
