using System;
using System.Collections.Generic;
using System.Text;

namespace sentience.pathplanner
{
    public class PathPoint
    {
        public int X, Y, Z;

        public PathPoint(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    public class PathPointSingle
    {
        public float X, Y;

        public PathPointSingle(float X, float Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
