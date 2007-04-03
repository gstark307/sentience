using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    /// <summary>
    /// describes a section of a path used for simulation
    /// </summary>
    public class simulationPathSegment
    {
        public float x, y;                 // initial x,y position of the robot in millimetres
        public float pan;                  // initial heading of the robot in radians
        public int no_of_steps;            // number of steps along the path segment
        public float distance_per_step_mm; // distance per step in millimetres
        public float pan_per_step;         // change in pan angle per step in radians

        public simulationPathSegment(float x, float y, float pan,
                                     int no_of_steps, float distance_per_step_mm, float pan_per_step)
        {
            this.x = x;
            this.y = y;
            this.pan = pan;
            this.no_of_steps = no_of_steps;
            this.distance_per_step_mm = distance_per_step_mm;
            this.pan_per_step = pan_per_step;
        }

        /// <summary>
        /// return this path segment as a sequence of poses
        /// </summary>
        /// <returns></returns>
        public ArrayList getPoses()
        {
            ArrayList result = new ArrayList();
            float xx = x;
            float yy = y;
            float curr_pan = pan;
            for (int i = 0; i < no_of_steps; i++)
            {
                particlePose pose = new particlePose(xx, yy, curr_pan, null);
                result.Add(pose);
                curr_pan += pan_per_step;
                xx += (distance_per_step_mm * (float)Math.Sin(curr_pan));
                yy += (distance_per_step_mm * (float)Math.Cos(curr_pan));
            }
            return (result);
        }
    }
}
