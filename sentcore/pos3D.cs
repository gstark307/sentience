/*
    Sentience 3D Perception System
    Copyright (C) 2000-2007 Bob Mottram
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
using System.Xml;
using System.Collections;
using System.Text;
using sluggish.utilities.xml;

namespace sentience.core
{
    /// <summary>
    /// stores a three dimensional coordinate
    /// </summary>
    public class pos3Dbase
    {
        public float x, y, z;

        public pos3Dbase(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// a more elaborate 3D object which includes position and orientation
    /// </summary>
    public class pos3D : pos3Dbase
    {
        public float pan, tilt, roll;

        public float new_pan_angle, dist_xy;

        public pos3D(float x, float y, float z) : base(x,y,z)
        {
        }

        public pos3D add(pos3D other)
        {
            pos3D sum = new pos3D(x + other.x, y + other.y, z + other.z);
            sum.pan = pan + other.pan;
            sum.tilt = tilt + other.tilt;
            sum.roll = roll + other.roll;
            return (sum);
        }

        public pos3D subtract(pos3D other)
        {
            pos3D sum = new pos3D(x - other.x, y - other.y, z - other.z);
            sum.pan = pan - other.pan;
            sum.tilt = tilt - other.tilt;
            sum.roll = roll - other.roll;
            return (sum);
        }

        public pos3D rotate_old(float pan, float tilt, float roll)
        {
            float hyp;
            pos3D rotated = new pos3D(x,y,z);

            // roll            
            //if (roll != 0)
            {
                hyp = (float)Math.Sqrt((rotated.x * rotated.x) + (rotated.z * rotated.z));
                if (hyp > 0)
                {
                    float roll_angle = (float)Math.Acos(rotated.z / hyp);
                    if (rotated.x < 0) roll_angle = (float)(Math.PI * 2) - roll_angle;
                    float new_roll_angle = roll + roll_angle;
                    rotated.x = hyp * (float)Math.Sin(new_roll_angle);
                    rotated.z = hyp * (float)Math.Cos(new_roll_angle);
                }
            }
            if (tilt != 0)
            {
                // tilt
                hyp = (float)Math.Sqrt((rotated.y * rotated.y) + (rotated.z * rotated.z));
                if (hyp > 0)
                {
                    float tilt_angle = (float)Math.Acos(rotated.y / hyp);
                    if (rotated.z < 0) tilt_angle = (float)(Math.PI * 2) - tilt_angle;
                    float new_tilt_angle = tilt + tilt_angle;
                    rotated.y = hyp * (float)Math.Sin(new_tilt_angle);
                    rotated.z = hyp * (float)Math.Cos(new_tilt_angle);
                }
            }

            
            
            //if (pan != 0)
            {
                // pan                
                hyp = (float)Math.Sqrt((rotated.x * rotated.x) + (rotated.y * rotated.y));
                if (hyp > 0)
                {
                    float pan_angle = (float)Math.Acos(rotated.y / hyp);
                    if (rotated.x < 0) pan_angle = (float)(Math.PI * 2) - pan_angle;
                    rotated.new_pan_angle = pan - pan_angle;
                    rotated.dist_xy = hyp;
                    rotated.x = hyp * (float)Math.Sin(rotated.new_pan_angle);
                    rotated.y = hyp * (float)Math.Cos(rotated.new_pan_angle);
                }
            }
            rotated.pan = this.pan + pan;
            rotated.tilt = this.tilt + tilt;
            rotated.roll = this.roll + roll;
            return (rotated);
        }

        /// <summary>
        /// rotates this position, using Y forward convention
        /// </summary>
        /// <param name="pan">pan angle in radians</param>
        /// <param name="tilt">tilt angle in radians</param>
        /// <param name="roll">roll angle in radians</param>
        /// <returns>rotated position</returns>                
        public pos3D rotate(float pan, float tilt, float roll)
        {
            float x2 = x;
            float y2 = y;
            float z2 = z;
            
            float x3 = x;
            float y3 = y;
            float z3 = z;

            // Rotation about the y axis
            if (roll != 0)
            {
                float roll2 = roll + (float)Math.PI;
                x3 = (float)((Math.Cos(roll2) * x2) + (Math.Sin(roll2) * z2));
                z3 = (float)(-(Math.Sin(roll) * x2) + (Math.Cos(roll) * z2));
                x2 = x3;
                z2 = z3;
            }

            // Rotatation about the x axis
            if (tilt != 0)
            {
                float tilt2 = tilt;
                z3 = (float)((Math.Cos(tilt2) * y2) - (Math.Sin(tilt2) * z2));
                y3 = (float)((Math.Sin(tilt2) * y2) + (Math.Cos(tilt2) * z2));
                y2 = y3;
                z2 = z3;
            }
                                    
            // Rotation about the z axis: 
            if (pan != 0)
            {                
                float pan2 = pan + (float)Math.PI;
                x3 = (float)((Math.Cos(pan2) * x2) - (Math.Sin(pan2) * y2));
                y3 = (float)((Math.Sin(pan) * x2) + (Math.Cos(pan) * y2));                 
            }

            pos3D rotated = new pos3D(x3, y3, z3);
            rotated.pan = this.pan + pan;
            rotated.tilt = this.tilt + tilt;
            rotated.roll = this.roll + roll;
            return (rotated);
        }
        

        /// <summary>
        /// return a translated version of the point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public pos3D translate(float x, float y, float z)
        {
            pos3D result = new pos3D(this.x + x, this.y + y, this.z + z);
            result.pan = pan;
            result.tilt = tilt;
            result.roll = roll;
            return(result);
        }

        /// <summary>
        /// copy the position from another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public void copyFrom(pos3D other)
        {
            x = other.x;
            y = other.y;
            z = other.z;
            pan = other.pan;
            tilt = other.tilt;
            roll = other.roll;
        }

        public pos3D Subtract(pos3D p)
        {
            pos3D new_p = new pos3D(x - p.x, y - p.y, z - p.z);
            new_p.pan = pan - p.pan;
            new_p.tilt = tilt - p.tilt;
            new_p.roll = roll - p.roll;
            return (new_p);
        }


        #region "saving and loading"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
            XmlElement elem = doc.CreateElement("PositionOrientation");
            xml.AddComment(doc, elem, "Position in millimetres");
            xml.AddTextElement(doc, elem, "PositionMillimetres", Convert.ToString(x, format) + "," +
                                                                 Convert.ToString(y, format) + "," +
                                                                 Convert.ToString(z, format));
            xml.AddComment(doc, elem, "Orientation in degrees - pan, tilt and roll");
            xml.AddTextElement(doc, elem, "OrientationDegrees", Convert.ToString(pan / (float)Math.PI * 180.0f, format) + "," +
                                                                Convert.ToString(tilt / (float)Math.PI * 180.0f, format) + "," +
                                                                Convert.ToString(roll / (float)Math.PI * 180.0f, format));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "PositionMillimetres")
            {
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String[] dimStr = xnod.InnerText.Split(',');
                x = Convert.ToSingle(dimStr[0], format);
                y = Convert.ToSingle(dimStr[1], format);
                z = Convert.ToSingle(dimStr[2], format);
            }

            if (xnod.Name == "OrientationDegrees")
            {
                IFormatProvider format = new System.Globalization.CultureInfo("en-GB");
                String[] dimStr = xnod.InnerText.Split(',');
                pan = Convert.ToSingle(dimStr[0], format) / 180.0f * (float)Math.PI;
                tilt = Convert.ToSingle(dimStr[1], format) / 180.0f * (float)Math.PI;
                roll = Convert.ToSingle(dimStr[2], format) / 180.0f * (float)Math.PI;
            }

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }

}
