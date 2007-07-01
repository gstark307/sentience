/*
    Classes for storing information about areas or locations on a map
    This is done in the KML format, as for Google earth/maps
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
using System.IO;
using System.Xml;
using System.Collections;
using sluggish.utilities.xml;

namespace sluggish.utilities.xml
{
    /// <summary>
    /// point on a map
    /// </summary>
    public class kmlPoint
    {
        public float longitude, latitude, altitude;

        #region "conversions"

        // circumference of the earth in metres
        private const float earth_circumference = 40075.16f * 1000;

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
            longitude = x_mm * 180 / ((earth_circumference / 2) * 1000);
            lattitude = y_mm * 180 / ((earth_circumference / 2) * 1000);
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

        #endregion

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("Point");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "coordinates", Convert.ToString(longitude) + "," +
                                                          Convert.ToString(latitude) + "," +
                                                          Convert.ToString(altitude));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "coordinates")
            {
                String[] dimStr = xnod.InnerText.Split(',');
                longitude = Convert.ToInt32(dimStr[0]);
                latitude = Convert.ToInt32(dimStr[1]);
                altitude = Convert.ToInt32(dimStr[2]);
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

    /// <summary>
    /// a single point icon on a map
    /// </summary>
    public class kmlPlacemarkPoint
    {
        // name of the place
        public String Name;

        // description of the place
        public String Description;

        public kmlPoint Point = new kmlPoint();

        public void Relocate(float longitude, float latitude)
        {
            Point.longitude = longitude;
            Point.latitude = latitude;
        }

        /// <summary>
        /// set the position of the point in millimetres
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        public void SetPositionMillimetres(float x_mm, float y_mm)
        {
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref Point.longitude, ref Point.latitude);
        }

        /// <summary>
        /// return the position in millimetres
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        public void GetPositionMillimetres(ref float x_mm, ref float y_mm)
        {
            kmlPoint.DegreesToMillimetres(Point.longitude, Point.latitude, ref x_mm, ref y_mm);
        }

        /// <summary>
        /// returns the distance to the given coordinate in millimetres
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public float DistanceTo(float longitude, float latitude)
        {
            float x1_mm = 0, y1_mm = 0, x2_mm = 0, y2_mm = 0;

            kmlPoint.DegreesToMillimetres(Point.longitude, Point.latitude, ref x1_mm, ref y1_mm);
            kmlPoint.DegreesToMillimetres(longitude, latitude, ref x2_mm, ref y2_mm);
            float dx = x2_mm - x1_mm;
            float dy = y2_mm - y1_mm;
            float dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
            return (dist);
        }

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("Placemark");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "name", Name);
            xml.AddTextElement(doc, elem, "description", Description);
            elem.AppendChild(Point.getXml(doc));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "name")
                Name = xnod.InnerText;

            if (xnod.Name == "description")
                Description = xnod.InnerText;

            if (xnod.Name == "Point")
                Point.LoadFromXml(xnod, level);

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

    public class kmlPlacemarkPath
    {
        // name of the place
        public String Name;

        // description of the place
        public String Description;

        public kmlLineString Path = new kmlLineString();

        #region "relocation - moving all path points at the same time"

        public void Relocate(float longitude, float latitude)
        {
            Path.Relocate(longitude, latitude);
        }

        public void RelocateMillimetres(float x_mm, float y_mm)
        {
            float longitude = 0, latitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref latitude);
            Relocate(longitude, latitude);
        }

        #endregion

        #region "adding points to the path"

        public void Add(float longitude, float latitude)
        {
            Path.Add(longitude, latitude, 0);
        }

        public void AddMillimetres(float x_mm, float y_mm)
        {
            float longitude = 0, latitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref latitude);
            Add(longitude, latitude);
        }

        #endregion

        #region "removing the last point added"

        public void Remove()
        {
            if (Path.Points.Count > 0)
                Path.Points.RemoveAt(Path.Points.Count - 1);
        }

        #endregion

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("Placemark");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "name", Name);
            xml.AddTextElement(doc, elem, "description", Description);
            elem.AppendChild(Path.getXml(doc));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "name")
                Name = xnod.InnerText;

            if (xnod.Name == "description")
                Description = xnod.InnerText;

            if (xnod.Name == "LineString")
                Path.LoadFromXml(xnod, level);

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

    public class kmlPlacemarkPolygon
    {
        // name of the place
        public String Name;

        // description of the place
        public String Description;

        public kmlPolygon Polygon = new kmlPolygon();

        #region "relocation - when you want to move all vertices"

        public void Relocate(float longitude, float latitude)
        {
            Polygon.Relocate(longitude, latitude);
        }

        public void RelocateMillimetres(float x_mm, float y_mm)
        {
            float longitude = 0, latitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref latitude);
            Relocate(longitude, latitude);
        }

        #endregion

        #region "adding vertices"

        /// <summary>
        /// add an outer vertex to the polygon
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        public void AddVertexMillimetres(float x_mm, float y_mm)
        {
            float longitude = 0, latitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref latitude);
            Polygon.outerBoundaryIs.Add(longitude, latitude, 0);
        }

        /// <summary>
        /// add an outer vertex
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        public void AddVertex(float longitude, float latitude)
        {
            Polygon.outerBoundaryIs.Add(longitude, latitude, 0);
        }

        /// <summary>
        /// add an inner vertex to the polygon
        /// </summary>
        /// <param name="x_mm"></param>
        /// <param name="y_mm"></param>
        public void AddInnerVertexMillimetres(float x_mm, float y_mm)
        {
            float longitude = 0, latitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref latitude);
            Polygon.innerBoundaryIs.Add(longitude, latitude, 0);
        }

        /// <summary>
        /// add an inner vertex
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        public void AddInnerVertex(float longitude, float latitude)
        {
            Polygon.innerBoundaryIs.Add(longitude, latitude, 0);
        }

        #endregion

        #region "removing vertices"

        /// <summary>
        /// remove the last outer vertex which was added
        /// </summary>
        public void Remove()
        {
            if (Polygon.outerBoundaryIs.Points.Count > 0)
                Polygon.outerBoundaryIs.Points.RemoveAt(Polygon.outerBoundaryIs.Points.Count - 1);
        }

        /// <summary>
        /// remove the last outer vertex which was added
        /// </summary>
        public void RemoveInner()
        {
            if (Polygon.innerBoundaryIs.Points.Count > 0)
                Polygon.innerBoundaryIs.Points.RemoveAt(Polygon.innerBoundaryIs.Points.Count - 1);
        }

        #endregion

        /// <summary>
        /// returns true if the given location is inside the polygon
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public bool isInside(float longitude, float latitude)
        {
            if (Polygon.outerBoundaryIs.isInside(longitude, latitude))
            {
                if (!Polygon.innerBoundaryIs.isInside(longitude, latitude))
                    return (true);
                else
                    return (false);
            }
            else return (false);
        }

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("Placemark");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "name", Name);
            xml.AddTextElement(doc, elem, "description", Description);
            elem.AppendChild(Polygon.getXml(doc));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "name")
                Name = xnod.InnerText;

            if (xnod.Name == "description")
                Description = xnod.InnerText;

            if (xnod.Name == "Polygon")
                Polygon.LoadFromXml(xnod, level);

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

    /// <summary>
    /// one linear ring to rule them all
    /// </summary>
    public class kmlLinearRing
    {
        // centre point of the ring
        public float centre_longitude, centre_latitude;

        public ArrayList Points = new ArrayList();

        /// <summary>
        /// recalculate the centre point of the ring
        /// </summary>
        private void updateCentrePoint()
        {
            if (Points.Count > 0)
            {
                float tot_x = 0;
                float tot_y = 0;
                for (int i = 0; i < Points.Count; i++)
                {
                    kmlPoint pt = (kmlPoint)Points[i];
                    tot_x += pt.longitude;
                    tot_y += pt.latitude;
                }
                centre_longitude = tot_x / Points.Count;
                centre_latitude = tot_y / Points.Count;
            }
        }

        /// <summary>
        /// relocate the ring at the new position
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        public void Relocate(float longitude, float latitude)
        {
            // adjust the position of each point relative to the centre
            for (int i = 0; i < Points.Count; i++)
            {
                kmlPoint pt = (kmlPoint)Points[i];
                pt.longitude += longitude - centre_longitude;
                pt.latitude += latitude - centre_latitude;
            }
            updateCentrePoint();
        }

        public void RelocateMillimetres(float x_mm, float y_mm)
        {
            float lattitude = 0, longitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref lattitude);
            Relocate(longitude, lattitude);
        }

        /// <summary>
        /// add a point to the ring
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <param name="altitude"></param>
        public void Add(float longitude, float latitude, float altitude)
        {
            kmlPoint pt = new kmlPoint();
            pt.longitude = longitude;
            pt.latitude = latitude;
            pt.altitude = altitude;
            Add(pt);
        }

        /// <summary>
        /// add a point to the ring
        /// </summary>
        /// <param name="pt"></param>
        public void Add(kmlPoint pt)
        {
            Points.Add(pt);
            updateCentrePoint();
        }

        /// <summary>
        /// returns true if the given location is inside the ring
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns>true if the given location is inside this ring</returns>
        public bool isInside(float longitude, float latitude)
        {
            bool inside = false;

            int i, j;
            for (i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
            {
                kmlPoint pt1 = (kmlPoint)Points[i];
                kmlPoint pt2 = (kmlPoint)Points[j];

                if ((((pt1.latitude <= latitude) && (latitude < pt2.latitude)) ||
                   ((pt2.latitude <= latitude) && (latitude < pt1.latitude))) &&
                   (longitude < (pt2.longitude - pt1.longitude) * (latitude - pt1.latitude) /
                   (pt2.latitude - pt1.latitude) + pt1.longitude))
                    inside = !inside;
            }
            return (inside);
        }


        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            String coords = "";
            for (int i = 0; i < Points.Count; i++)
            {
                kmlPoint pt = (kmlPoint)Points[i];
                coords += "\r\n" +
                          Convert.ToString(pt.longitude) + "," +
                          Convert.ToString(pt.latitude) + "," +
                          Convert.ToString(pt.altitude) + ",";
            }
            coords += "\r\n";

            XmlElement elem = doc.CreateElement("LinearRing");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "coordinates", coords);
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "coordinates")
            {
                Points.Clear();
                String[] coords = xnod.InnerText.Split(',');
                for (int i = 0; i < coords.Length; i += 3)
                {
                    float longitude = Convert.ToSingle(coords[i]);
                    float latitude = Convert.ToSingle(coords[i + 1]);
                    float altitude = Convert.ToSingle(coords[i + 2]);
                    Add(longitude, latitude, altitude);
                }
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

    public class kmlLineString
    {
        // centre point of the line
        public float centre_longitude, centre_latitude;

        public bool extrude;
        public bool tessellate;
        public String altitudeMode = "absolute";
        public ArrayList Points = new ArrayList();

        /// <summary>
        /// recalculate the centre point of the ring
        /// </summary>
        private void updateCentrePoint()
        {
            if (Points.Count > 0)
            {
                float tot_x = 0;
                float tot_y = 0;
                for (int i = 0; i < Points.Count; i++)
                {
                    kmlPoint pt = (kmlPoint)Points[i];
                    tot_x += pt.longitude;
                    tot_y += pt.latitude;
                }
                centre_longitude = tot_x / Points.Count;
                centre_latitude = tot_y / Points.Count;
            }
        }

        /// <summary>
        /// relocate the line at the new position
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        public void Relocate(float longitude, float latitude)
        {
            // adjust the position of each point relative to the centre
            for (int i = 0; i < Points.Count; i++)
            {
                kmlPoint pt = (kmlPoint)Points[i];
                pt.longitude += longitude - centre_longitude;
                pt.latitude += latitude - centre_latitude;
            }
            updateCentrePoint();
        }

        public void RelocateMillimetres(float x_mm, float y_mm)
        {
            float lattitude = 0, longitude = 0;
            kmlPoint.MillimetresToDegrees(x_mm, y_mm, ref longitude, ref lattitude);
            Relocate(longitude, lattitude);
        }

        /// <summary>
        /// add a point to the ring
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <param name="altitude"></param>
        public void Add(float longitude, float latitude, float altitude)
        {
            kmlPoint pt = new kmlPoint();
            pt.longitude = longitude;
            pt.latitude = latitude;
            pt.altitude = altitude;
            Add(pt);
        }

        /// <summary>
        /// add a point to the ring
        /// </summary>
        /// <param name="pt"></param>
        public void Add(kmlPoint pt)
        {
            Points.Add(pt);
            updateCentrePoint();
        }

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            String coords = "";
            for (int i = 0; i < Points.Count; i++)
            {
                kmlPoint pt = (kmlPoint)Points[i];
                coords += "\r\n" +
                          Convert.ToString(pt.longitude) + "," +
                          Convert.ToString(pt.latitude) + "," +
                          Convert.ToString(pt.altitude) + ",";
            }
            coords += "\r\n";

            XmlElement elem = doc.CreateElement("LineString");
            doc.DocumentElement.AppendChild(elem);
            if (extrude)
                xml.AddTextElement(doc, elem, "extrude", "1");
            else
                xml.AddTextElement(doc, elem, "extrude", "0");
            if (tessellate)
                xml.AddTextElement(doc, elem, "tessellate", "1");
            else
                xml.AddTextElement(doc, elem, "tessellate", "0");
            xml.AddTextElement(doc, elem, "altitudeMode", altitudeMode);
            xml.AddTextElement(doc, elem, "coordinates", coords);
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "extrude")
            {
                if (xnod.InnerText.Contains("1"))
                    extrude = true;
                else
                    extrude = false;
            }

            if (xnod.Name == "tessellate")
            {
                if (xnod.InnerText.Contains("1"))
                    tessellate = true;
                else
                    tessellate = false;
            }

            if (xnod.Name == "altitudeMode")
            {
                altitudeMode = xnod.InnerText;
            }

            if (xnod.Name == "coordinates")
            {
                Points.Clear();
                String[] coords = xnod.InnerText.Split(',');
                for (int i = 0; i < coords.Length; i += 3)
                {
                    float longitude = Convert.ToSingle(coords[i]);
                    float latitude = Convert.ToSingle(coords[i + 1]);
                    float altitude = Convert.ToSingle(coords[i + 2]);
                    Add(longitude, latitude, altitude);
                }
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

    public class kmlGroundOverlay
    {
        // name of the place
        public String Name;

        // description of the place
        public String Description;

        // image URL
        public String href;

        // latitude/longitude box
        public float north, south, east, west;

        // rotation of the overlay in radians
        public float rotation;

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("GroundOverlay");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "name", Name);
            xml.AddTextElement(doc, elem, "description", Description);

            XmlElement elemIcon = doc.CreateElement("Icon");
            elem.AppendChild(elemIcon);
            xml.AddTextElement(doc, elemIcon, "href", href);

            XmlElement elemBox = doc.CreateElement("LatLonBox");
            elem.AppendChild(elemBox);
            xml.AddTextElement(doc, elemBox, "north", Convert.ToString(north));
            xml.AddTextElement(doc, elemBox, "south", Convert.ToString(south));
            xml.AddTextElement(doc, elemBox, "east", Convert.ToString(east));
            xml.AddTextElement(doc, elemBox, "west", Convert.ToString(west));
            xml.AddTextElement(doc, elemBox, "rotation", Convert.ToString(rotation));
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "name")
                Name = xnod.InnerText;

            if (xnod.Name == "description")
                Description = xnod.InnerText;

            if (xnod.Name == "href")
                href = xnod.InnerText;

            if (xnod.Name == "north")
                north = Convert.ToSingle(xnod.InnerText);

            if (xnod.Name == "south")
                south = Convert.ToSingle(xnod.InnerText);

            if (xnod.Name == "east")
                east = Convert.ToSingle(xnod.InnerText);

            if (xnod.Name == "west")
                west = Convert.ToSingle(xnod.InnerText);

            if (xnod.Name == "rotation")
                rotation = Convert.ToSingle(xnod.InnerText);

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

    public class kmlPolygon
    {
        public bool extrude;
        public String altitudeMode = "relativeToGround";
        public kmlLinearRing outerBoundaryIs = new kmlLinearRing();
        public kmlLinearRing innerBoundaryIs = new kmlLinearRing();

        public void Relocate(float longitude, float latitude)
        {
            outerBoundaryIs.Relocate(longitude, latitude);
            innerBoundaryIs.Relocate(longitude, latitude);
        }

        #region "loading and saving"

        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("Polygon");
            doc.DocumentElement.AppendChild(elem);
            if (extrude)
                xml.AddTextElement(doc, elem, "extrude", "1");
            else
                xml.AddTextElement(doc, elem, "extrude", "0");
            xml.AddTextElement(doc, elem, "altitudeMode", altitudeMode);

            XmlElement elemOuter = doc.CreateElement("outerBoundaryIs");
            elem.AppendChild(elemOuter);
            elemOuter.AppendChild(outerBoundaryIs.getXml(doc));

            if (innerBoundaryIs.Points.Count > 0)
            {
                XmlElement elemInner = doc.CreateElement("innerBoundaryIs");
                elem.AppendChild(elemInner);
                elemInner.AppendChild(innerBoundaryIs.getXml(doc));
            }
            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "extrude")
            {
                if (xnod.InnerText.Contains("1"))
                    extrude = true;
                else
                    extrude = false;
            }

            if (xnod.Name == "altitudeMode")
            {
                altitudeMode = xnod.InnerText;
            }

            if (xnod.Name == "outerBoundaryIs")
            {
                outerBoundaryIs.LoadFromXml(xnod, level);
            }

            if (xnod.Name == "innerBoundaryIs")
            {
                innerBoundaryIs.LoadFromXml(xnod, level);
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

    /// <summary>
    /// a zone which may contain different kinds of geometric objects
    /// </summary>
    public class kmlZone
    {
        // name of the zone
        public String Name;

        // description of the zone
        public String Description;

        public ArrayList Polygons = new ArrayList();
        public ArrayList Points = new ArrayList();
        public ArrayList Paths = new ArrayList();

        #region "adding new objects"

        public void Add(kmlPlacemarkPolygon polygon)
        {
            Polygons.Add(polygon);
        }

        public void Add(kmlPlacemarkPoint pt)
        {
            Points.Add(pt);
        }

        public void Add(kmlPlacemarkPath path)
        {
            Paths.Add(path);
        }

        #endregion

        #region "miscellaneous"

        public void Clear()
        {
            Polygons.Clear();
            Points.Clear();
            Paths.Clear();
        }

        public bool isActive()
        {
            if ((Polygons.Count > 0) || (Points.Count > 0) || (Paths.Count > 0))
                return (true);
            else
                return (false);
        }

        #endregion

        #region "finding objects"

        /// <summary>
        /// if the given point is inside any objects return a list of their names
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public ArrayList isInside(float longitude, float latitude)
        {
            ArrayList result = new ArrayList();

            for (int i = 0; i < Polygons.Count; i++)
            {
                kmlPlacemarkPolygon poly = (kmlPlacemarkPolygon)Polygons[i];
                if (poly.isInside(longitude, latitude))
                    result.Add(poly.Name);
            }
            return (result);
        }

        /// <summary>
        /// returns a list of nearby points
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <param name="radius_mm"></param>
        /// <returns></returns>
        public ArrayList isNear(float longitude, float latitude, float radius_mm)
        {
            ArrayList result = new ArrayList();

            for (int i = 0; i < Points.Count; i++)
            {
                kmlPlacemarkPoint pt = (kmlPlacemarkPoint)Points[i];
                if (pt.DistanceTo(longitude, latitude) < radius_mm)
                    result.Add(pt.Name);
            }
            return (result);
        }


        /// <summary>
        /// returns the polygon with the given location name
        /// </summary>
        /// <param name="location_name"></param>
        /// <returns></returns>
        public kmlPlacemarkPolygon GetPolygon(String location_name)
        {
            kmlPlacemarkPolygon location = null;
            int i = 0;
            while ((i < Polygons.Count) && (location == null))
            {
                kmlPlacemarkPolygon poly = (kmlPlacemarkPolygon)Polygons[i];
                if (poly.Name == location_name) location = poly;
                i++;
            }
            return (location);
        }

        /// <summary>
        /// returns the point with the given location name
        /// </summary>
        /// <param name="location_name"></param>
        /// <returns></returns>
        public kmlPlacemarkPoint GetPoint(String location_name)
        {
            kmlPlacemarkPoint pt = null;
            int i = 0;
            while ((i < Points.Count) && (pt == null))
            {
                kmlPlacemarkPoint p = (kmlPlacemarkPoint)Points[i];
                if (p.Name == location_name) pt = p;
                i++;
            }
            return (pt);
        }

        /// <summary>
        /// returns the path with the given location name
        /// </summary>
        /// <param name="location_name"></param>
        /// <returns></returns>
        public kmlPlacemarkPath GetPath(String location_name)
        {
            kmlPlacemarkPath path = null;
            int i = 0;
            while ((i < Paths.Count) && (path == null))
            {
                kmlPlacemarkPath p = (kmlPlacemarkPath)Paths[i];
                if (p.Name == location_name) path = p;
                i++;
            }
            return (path);
        }

        #endregion

        #region "loading and saving"

        /// <summary>
        /// return a KML document
        /// </summary>
        /// <returns></returns>
        private XmlDocument getKmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System KML interface");
            doc.AppendChild(commentnode);

            XmlElement nodeKml = doc.CreateElement("kml");
            nodeKml.SetAttribute("xmlns", "http://earth.google.com/kml/2.1");
            doc.AppendChild(nodeKml);

            XmlElement elem = getXml(doc);
            nodeKml.AppendChild(elem);

            return (doc);
        }

        /// <summary>
        /// save data as an KML file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename)
        {
            XmlDocument doc = getKmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load data from file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
        {
            if (File.Exists(filename))
            {
                // use an XmlTextReader to open an XML document
                XmlTextReader xtr = new XmlTextReader(filename);
                xtr.WhitespaceHandling = WhitespaceHandling.None;

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                xd.Load(xtr);

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
                LoadFromXml(xnodDE, 0, "");

                // close the reader
                xtr.Close();
            }
        }


        /// <summary>
        /// return an xml element
        /// </summary>
        /// <param name="doc">xml document to add the data to</param>
        /// <returns>an xml element</returns>
        public XmlElement getXml(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("Document");
            doc.DocumentElement.AppendChild(elem);
            xml.AddTextElement(doc, elem, "name", Name);
            xml.AddTextElement(doc, elem, "description", Description);

            if (Polygons.Count > 0)
            {
                XmlElement elemPoly = doc.CreateElement("Polygons");
                elem.AppendChild(elemPoly);
                for (int i = 0; i < Polygons.Count; i++)
                {
                    kmlPlacemarkPolygon poly = (kmlPlacemarkPolygon)Polygons[i];
                    elemPoly.AppendChild(poly.getXml(doc));
                }
            }

            if (Points.Count > 0)
            {
                XmlElement elemPoints = doc.CreateElement("Points");
                elem.AppendChild(elemPoints);
                for (int i = 0; i < Points.Count; i++)
                {
                    kmlPlacemarkPoint pt = (kmlPlacemarkPoint)Points[i];
                    elemPoints.AppendChild(pt.getXml(doc));
                }
            }

            if (Paths.Count > 0)
            {
                XmlElement elemPaths = doc.CreateElement("Paths");
                elem.AppendChild(elemPaths);
                for (int i = 0; i < Paths.Count; i++)
                {
                    kmlPlacemarkPath path = (kmlPlacemarkPath)Paths[i];
                    elemPaths.AppendChild(path.getXml(doc));
                }
            }

            return (elem);
        }

        /// <summary>
        /// parse an xml node to extract data
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level, String ObjectType)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "name")
                Name = xnod.InnerText;

            if (xnod.Name == "description")
                Description = xnod.InnerText;

            if (xnod.Name == "Polygons")
            {
                ObjectType = xnod.Name;
                Polygons.Clear();
            }

            if (xnod.Name == "Points")
            {
                ObjectType = xnod.Name;
                Points.Clear();
            }

            if (xnod.Name == "Paths")
            {
                ObjectType = xnod.Name;
                Paths.Clear();
            }

            if (xnod.Name == "Placemark")
            {
                if (ObjectType == "Polygons")
                {
                    kmlPlacemarkPolygon poly = new kmlPlacemarkPolygon();
                    poly.LoadFromXml(xnod, level);
                    Polygons.Add(poly);
                }
                if (ObjectType == "Points")
                {
                    kmlPlacemarkPoint pt = new kmlPlacemarkPoint();
                    pt.LoadFromXml(xnod, level);
                    Points.Add(pt);
                }
                if (xnod.Name == "Paths")
                {
                    kmlPlacemarkPath path = new kmlPlacemarkPath();
                    path.LoadFromXml(xnod, level);
                    Paths.Add(path);
                }
            }

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                while (xnodWorking != null)
                {
                    LoadFromXml(xnodWorking, level + 1, ObjectType);
                    xnodWorking = xnodWorking.NextSibling;
                }
            }
        }

        #endregion

    }

}
