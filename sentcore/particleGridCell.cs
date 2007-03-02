using System;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    /// <summary>
    /// used to store a single grid cell hypothesis
    /// </summary>
    public class particleGridCell
    {
        // grid cell coordinate
        public short x, y;

        // probability of occupancy, taken from the sensor model
        public float probability;

        // the pose which made this observation
        public particlePose pose;

        public particleGridCell(int x, int y, float probability, particlePose pose)
        {
            this.x = (short)x;
            this.y = (short)y;
            this.probability = probability;
            this.pose = pose;
        }
    }
}
