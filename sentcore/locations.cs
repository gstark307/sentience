using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
    }
}
