/*
    Classes for storing information about map locations
    This is done in the same style as the Google maps XML format
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace sentience.core
{
    /// <summary>
    /// point on a map
    /// </summary>
    public class locationPoint
    {
        public float longitude, lattitide;

        public virtual String getXml()
        {
            String XmlStr = "<point lat=\"" + Convert.ToString(lattitide) +
                            "\" lng=\"" + Convert.ToString(lattitide) + "\"/>";
            return (XmlStr);
        }
    }
    
    /// <summary>
    /// map marker
    /// </summary>
    public class locationMarker : locationPoint
    {
        // a unique label for this marker
        public String label;

        // html associated with this marker
        public String html;

        public override String getXml()
        {
            String XmlStr = "<marker lat=\"" + Convert.ToString(lattitide) +
                            "\" lng=\"" + Convert.ToString(lattitide) + 
                            "\" html=\"" + html +
                            "\" label=\"" + label + "\"/>";
            return (XmlStr);
        }
    }

    /// <summary>
    /// An object which represents a location within a bounded area, 
    /// such as "kitchen", "my house", "manchester", etc.
    /// The positions are all given in terms of lattitude and longitude
    /// </summary>
    public class locationArea : locationMarker
    {
        // colour used for this area
        public Byte[] colour = new Byte[3];

        // stores the sequential vertices of the area
        public ArrayList vertices;

        /// <summary>
        /// recalculate the centre point of the area
        /// </summary>
        private void updateCentrePoint()
        {
            if (vertices != null)
            {
                if (vertices.Count > 0)
                {
                    float tot_x = 0;
                    float tot_y = 0;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        locationPoint pt = (locationPoint)vertices[i];
                        tot_x += pt.longitude;
                        tot_y += pt.lattitide;
                    }
                    this.longitude = tot_x / vertices.Count;
                    this.lattitide = tot_y / vertices.Count;
                }
            }
        }

        /// <summary>
        /// add a point to the area
        /// </summary>
        /// <param name="pt">point object</param>
        public void Add(locationPoint pt)
        {
            if (vertices == null)
                vertices = new ArrayList();

            vertices.Add(pt);
            updateCentrePoint();
        }

        /// <summary>
        /// add a point to the area
        /// </summary>
        /// <param name="longitude">longitude of the point</param>
        /// <param name="lattitide">lattitude of the point</param>
        public void Add(float longitude, float lattitide)
        {
            locationPoint pt = new locationPoint();
            pt.longitude = longitude;
            pt.lattitide = lattitide;
            Add(pt);
        }

        /// <summary>
        /// relocate the area to a different position
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="lattitide"></param>
        public void Relocate(float longitude, float lattitide)
        {
            if (vertices != null)
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    locationPoint pt = (locationPoint)vertices[i];
                    float dx = pt.longitude - this.longitude;
                    float dy = pt.lattitide - this.lattitide;
                    pt.longitude = longitude + dx;
                    pt.lattitide = lattitide + dy;
                }
            }
            this.longitude = longitude;
            this.lattitide = lattitide;
        }

        /// <summary>
        /// returns true if the given location is inside the area
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="lattitide"></param>
        /// <returns>true if the given location is inside this area</returns>
        public bool isInside(float longitude, float lattitide)
        {
            bool inside = false;

            if (vertices != null)
            {
                int i, j;
                for (i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
                {
                    locationPoint pt1 = (locationPoint)vertices[i];
                    locationPoint pt2 = (locationPoint)vertices[j];

                    if ((((pt1.lattitide <= lattitide) && (lattitide < pt2.lattitide)) ||
                       ((pt2.lattitide <= lattitide) && (lattitide < pt1.lattitide))) &&
                       (longitude < (pt2.longitude - pt1.longitude) * (lattitide - pt1.lattitide) /
                       (pt2.lattitide - pt1.lattitide) + pt1.longitude))
                        inside = !inside;
                }
            }
            return (inside);
        }

        /// <summary>
        /// returns an XML string representing the area, in a format similar to Google maps
        /// </summary>
        /// <returns></returns>
        public override String getXml()
        {
            String XmlStr = "  <marker lat=\"" + Convert.ToString(lattitide) +
                      "\" lng=\"" + Convert.ToString(lattitide) +
                      "\" html=\"" + html + 
                      "\" label=\"" + label + "\">\r\n";

            if (vertices != null)
            {
                if (vertices.Count > 0)
                {
                    // get the hex format of the colour
                    String hexcol = util.GetHexFromRGB(colour[0], colour[1], colour[2]);

                    XmlStr += "    <line colour=\"#" + hexcol + "\" width=\"1\">\r\n";
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        locationPoint pt = (locationPoint)vertices[i];
                        XmlStr += "      " + pt.getXml() + "\r\n";
                    }
                    XmlStr += "    </line>\r\n";
                }
            }

            XmlStr += "  </marker>";
            return (XmlStr);
        }
    }

    public class locations
    {
        const float earth_diameter_metres = 12756.32f * 1000;
        const float earth_circumference = (float)Math.PI * earth_diameter_metres;

        public ArrayList areas;

        public locations()
        {
            areas = new ArrayList();
        }

        /// <summary>
        /// returns the object corresponding to the given location name
        /// </summary>
        /// <param name="location_name"></param>
        /// <returns></returns>
        public locationArea GetLocation(String location_name)
        {
            locationArea locn = null;
            int i = 0;
            while ((i < areas.Count) && (locn == null))
            {
                locationArea l = (locationArea)areas[i];
                if (l.label == location_name) locn = l;
                i++;
            }
            return (locn);
        }

        /// <summary>
        /// remove a location from the list
        /// </summary>
        /// <param name="location_name"></param>
        public void Remove(String location_name)
        {
            locationArea locn = GetLocation(location_name);
            if (locn != null)
                areas.Remove(locn);
        }


        public void AddMillimetres(String location_name, float x_mm, float y_mm)
        {
            float lattitude = 0, longitude = 0;
            MillimetresToDegrees(x_mm, y_mm, ref longitude, ref lattitude);
            Add(location_name, longitude, lattitude);
        }


        /// <summary>
        /// add a new location to the list
        /// </summary>
        /// <param name="location_name"></param>
        public void Add(String location_name, float longitude, float lattitide)
        {
            locationArea locn = GetLocation(location_name);
            if (locn == null)
            {
                locn = new locationArea();
                areas.Add(locn);
                locn.label = location_name;
                locn.longitude = longitude;
                locn.lattitide = lattitide;
            }
            else 
                locn.Relocate(longitude, lattitide);
        }

        /// <summary>
        /// adds some HTML to a location, which may contain a description or other links
        /// </summary>
        /// <param name="location_name"></param>
        /// <param name="HTML"></param>
        public void AddHTML(String location_name, String HTML)
        {
            locationArea locn = GetLocation(location_name);
            if (locn != null)
                locn.html = HTML;
        }

        /// <summary>
        /// adds a point to a location
        /// </summary>
        /// <param name="location_name"></param>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        public void AddLocationPointMillimetres(String location_name, 
                                                float x_mm, float y_mm)
        {
            float lattitude = 0, longitude = 0;
            MillimetresToDegrees(x_mm, y_mm, ref longitude, ref lattitude);
            AddLocationPoint(location_name, longitude, lattitude);
        }

        /// <summary>
        /// adds a point to a location
        /// </summary>
        /// <param name="location_name"></param>
        /// <param name="point_longitude"></param>
        /// <param name="point_lattitide"></param>
        public void AddLocationPoint(String location_name, float point_longitude, float point_lattitide)
        {
            locationArea locn = GetLocation(location_name);
            if (locn != null)
                locn.Add(point_longitude, point_lattitide);
        }

        /// <summary>
        /// convert a 2D position in millimetres to degrees on the surface of the earth
        /// this is a crude calculation, not intended to be highly accurate
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <param name="longitude"></param>
        /// <param name="lattitude"></param>
        public static void MillimetresToDegrees(float x_mm, float y_mm,
                                                ref float longitude, ref float lattitude)
        {
            longitude = x_mm * 180 / ((earth_circumference/2) * 1000);
            lattitude = y_mm * 180 / ((earth_circumference/2) * 1000);
        }

        /// <summary>
        /// convert lattitude and longitude to millimetres
        /// this is a crude calculation, not intended to be highly accurate
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="lattitude"></param>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        public static void DegreesToMillimetres(float longitude, float lattitude,
                                                ref float x_mm, ref float y_mm)
        {
            x_mm = longitude * (earth_circumference / 2) * 1000 / 180.0f;
            y_mm = lattitude * (earth_circumference / 2) * 1000 / 180.0f;
        }

        /// <summary>
        /// returns a list of areas which the given point is inside
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        /// <returns></returns>
        public ArrayList IsInsideMillimetres(float x_mm, float y_mm)
        {
            float lattitude = 0, longitude = 0;
            MillimetresToDegrees(x_mm, y_mm, ref longitude, ref lattitude);
            return (IsInside(longitude, lattitude));
        }

        /// <summary>
        /// returns a list of areas which the given point is inside
        /// </summary>
        /// <param name="point_longitude"></param>
        /// <param name="point_lattitide"></param>
        /// <returns></returns>
        public ArrayList IsInside(float point_longitude, float point_lattitide)
        {
            ArrayList inside = new ArrayList();

            for (int i = 0; i < areas.Count; i++)
            {
                locationArea area = (locationArea)areas[i];
                if (area.isInside(point_longitude, point_lattitide))
                    inside.Add(area.label);
            }
            return (inside);
        }

        /// <summary>
        /// returns the centre point of all areas
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="lattitude"></param>
        private void getCentrePoint(ref float longitude, ref float lattitude)
        {
            longitude = 9999;
            lattitude = 0;

            if (areas != null)
            {
                if (areas.Count > 0)
                {
                    float tot_x = 0;
                    float tot_y = 0;
                    for (int i = 0; i < areas.Count; i++)
                    {
                        locationArea area = (locationArea)areas[i];
                        tot_x += area.longitude;
                        tot_y += area.lattitide;
                    }
                    longitude = tot_x / areas.Count;
                    lattitude = tot_y / areas.Count;
                }
            }
        }


        public void RelocateMillimetres(float x_mm, float y_mm)
        {
            float lattitude = 0, longitude = 0;
            MillimetresToDegrees(x_mm, y_mm, ref longitude, ref lattitude);
            Relocate(longitude, lattitude);
        }


        public void Relocate(float longitude, float lattitide)
        {
            // calculate the average position
            float av_longitude = 0, av_lattitide = 0;
            getCentrePoint(ref av_longitude, ref av_lattitide);

            if (av_longitude != 9999)
            {
                // adjust the position of each area relative to the average
                for (int i = 0; i < areas.Count; i++)
                {
                    locationArea area = (locationArea)areas[i];
                    area.Relocate(area.longitude + longitude - av_longitude,
                                  area.lattitide + lattitide - av_lattitide);
                }
            }
        }
    }
}
