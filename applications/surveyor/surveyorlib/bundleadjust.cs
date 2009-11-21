/*
    Bundle adjustment utilities
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
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace surveyor.vision
{
      
    public class bundleadjust
    {
        /// <summary>
        /// Returns the hexidecimal character for the specified value
        /// </summary>
        /// <param name="Dec"> The value to convert </param>
        /// <returns>System.String</returns>
        private static string GetHex(double Dec)
        {
            string Value = "";
            if (Dec == 10)
            {
                Value = "A";
            }
            else if (Dec == 11)
            {
                Value = "B";
            }
            else if (Dec == 12)
            {
                Value = "C";
            }
            else if (Dec == 13)
            {
                Value = "D";
            }
            else if (Dec == 14)
            {
                Value = "E";
            }
            else if (Dec == 15)
            {
                Value = "F";
            }
            else
            {
                Value = "" + Dec;
            }
            return Value;
        }
        
        /// <summary>
        /// Gets the hexidecimal color code from the specified RGB
        /// </summary>
        /// <param name="R"> The value of R </param>
        /// <param name="G"> The value of G </param>
        /// <param name="B"> The value of B </param>
        /// <returns></returns>
        private static string GetHexFromRGB(int R, int G, int B)
        {
            string a, b, c, d, e, f, z;
            a = GetHex(Math.Floor((double)R / 16));
            b = GetHex(R % 16);
            c = GetHex(Math.Floor((double)G / 16));
            d = GetHex(G % 16);
            e = GetHex(Math.Floor((double)B / 16));
            f = GetHex(B % 16);
            z = a + b + c + d + e + f;
            return z;
        }
        
        public static void BundleFileToIFrIT(
            string bundle_filename,
            string IFrIT_filename)
        {
            if (File.Exists(bundle_filename))
            {
                StreamReader oRead = null;
                bool allowRead = true;

                try
                {
                    oRead = File.OpenText(bundle_filename);
                }
                catch
                {
                    allowRead = false;
                }           
                
                if (allowRead)
                {
                    int state = 0;
                    int no_of_cameras = 0;
                    int no_of_points = 0;
                    int camera_ctr = 0;
                    int point_ctr = 0;
                    string str="";
                    List<float> values = new List<float>();
                    while ((str != null) && 
                           (!oRead.EndOfStream))
                    {
                        str = oRead.ReadLine();
                        if (str != null)
                        {
                            switch(state)
                            {
                                case 0:
                                {
                                    if (str.Trim().ToLower().StartsWith("# bundle"))
                                    {
                                        state=1;
                                    }
                                    break;
                                }
                                case 1:
                                {
                                    string[] numstr = str.Split(' ');
                                    if (numstr.Length == 2)
                                    {
                                        no_of_cameras = Convert.ToInt32(numstr[0]);
                                        no_of_points = Convert.ToInt32(numstr[1]);
                                        state = 2;
                                        camera_ctr = 0;
                                        point_ctr = 0;
                                    }
                                    if (no_of_cameras == 0) str = null;
                                    break;
                                }
                                case 2:
                                {
                                    string[] str2 = str.Split(' ');
                                    for (int i = 0; i < str2.Length; i++)
                                    {
                                        values.Add(Convert.ToSingle(str2[i]));
                                    }
                                    break;
                                }
                            }
                            
                        }
                    }

                    if (no_of_cameras > 0)
                    {
                        int idx = 0;
                        for (int cam = 0; cam < no_of_cameras; cam++, idx += 15)
                        {
                            float focal_length = values[idx];
                            float coeff0 = values[idx+1];
                            float coeff1 = values[idx+2];
                            float x = values[idx+12];
                            float y = values[idx+13];
                            float z = values[idx+14];
                        }

                        List<float> points = new List<float>();
                        List<byte> points_colour = new List<byte>();
                        float tx = float.MaxValue;
                        float ty = float.MaxValue;
                        float tz = float.MaxValue;
                        float bx = float.MinValue;
                        float by = float.MinValue;
                        float bz = float.MinValue;
                        while (idx < values.Count)
                        {
                            float x = values[idx++];
                            float y = values[idx++];
                            float z = values[idx++];
                            points.Add(x);
                            points.Add(y);
                            points.Add(z);
                            if (x < tx) tx = x;
                            if (y < ty) ty = y;
                            if (z < tz) tz = z;
                            if (x > bx) bx = x;
                            if (y > by) by = y;
                            if (z > bz) bz = z;
                            byte r = Convert.ToByte(values[idx++]);
                            byte g = Convert.ToByte(values[idx++]);
                            byte b = Convert.ToByte(values[idx++]);
                            points_colour.Add(r);
                            points_colour.Add(g);
                            points_colour.Add(b);
                            int no_of_views = Convert.ToInt32(values[idx++]);
                            for (int i = 0; i < no_of_views; i++)
                            {
                                int cam = Convert.ToInt32(values[idx++]);
                                int keypoint = Convert.ToInt32(values[idx++]);
                                float keypoint_x = values[idx++];
                                float keypoint_y = values[idx++];
                            }
                        }

                        string bounding_box = 
                            Convert.ToString(tx) + " " + 
                            Convert.ToString(ty) + " " +
                            Convert.ToString(tz) + " " +
                            Convert.ToString(bx) + " " + 
                            Convert.ToString(by) + " " +
                            Convert.ToString(bz) + " X Y Z";

                        List<string> particles = new List<string>();
                        for (int p = 0; p < points.Count; p+=3)
                        {
                            float colour_value = 
                                int.Parse(GetHexFromRGB(points_colour[p], points_colour[p+1], points_colour[p+2]), NumberStyles.HexNumber);
                            
                            string particleStr = 
                                Convert.ToString(points[p]) + " " +
                                Convert.ToString(points[p+1]) + " " +
                                Convert.ToString(points[p+2]) + " " +
                                Convert.ToString(1.0f) + " " +
                                Convert.ToString(colour_value);
                            
                            particles.Add(particleStr);
                        }

                        // write the particles to a text file
                        if (particles.Count > 0)
                        {
                            StreamWriter oWrite = null;
                            bool allowWrite = true;
            
                            try
                            {
                                oWrite = File.CreateText(IFrIT_filename);
                            }
                            catch
                            {
                                allowWrite = false;
                            }
            
                            if (allowWrite)
                            {
                                oWrite.WriteLine(Convert.ToString(particles.Count));
                                oWrite.WriteLine(bounding_box);
                                for (int p = 0; p < particles.Count; p++)
                                    oWrite.WriteLine(particles[p]);
                                oWrite.Close();
                            }
                        }
                        
                                                
                    }
                }
            }
            
        }
    }
}
