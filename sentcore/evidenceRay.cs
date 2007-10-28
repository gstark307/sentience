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
using System.IO;
using System.Text;

namespace sentience.core
{
    public sealed class evidenceRay
    {
        public const int pan_steps = 25;

        public int cameraID;
        //public pos3D centre;
        public pos3D[] vertices;    //start and end position of the ray
        public Byte[] colour;       //colour of the ray
        public float width = 1;     //width at the fattest point
        public float length = 1;    //length of the ray
        public float fattestPoint;  //fraction of the rays length at which it is fattest
        public float uncertainty = 1;
        public float disparity;     //original correspondence disparity value
        public float sigma;         //horizontal uncertainty

        public float pan_angle;
        public float start_dist;
        public int pan_index;

        public pos3D observedFrom;

        public evidenceRay()
        {
            vertices = new pos3D[2];
            colour = new Byte[3];
            colour[0] = 255;
        }

        #region "saving and loading"

        /// <summary>
        /// save the ray to file
        /// </summary>
        /// <param name="binfile"></param>
        public void Save(BinaryWriter binfile)
        {
            for (int i = 0; i < 2; i++)
            {
                binfile.Write(vertices[i].x);
                binfile.Write(vertices[i].y);
                binfile.Write(vertices[i].z);
            }

            for (int c = 0; c < 3; c++)
                binfile.Write(colour[c]);

            binfile.Write(width);
            binfile.Write(fattestPoint);
        }

        /// <summary>
        /// load the ray from file
        /// </summary>
        /// <param name="binfile"></param>
        public void Load(BinaryReader binfile)
        {
            for (int i = 0; i < 2; i++)
            {
                vertices[i].x = binfile.ReadSingle();
                vertices[i].y = binfile.ReadSingle();
                vertices[i].z = binfile.ReadSingle();
            }

            for (int c = 0; c < 3; c++)
                colour[c] = binfile.ReadByte();

            width = binfile.ReadSingle();
            fattestPoint = binfile.ReadSingle();

            // calculate the length
            float dx = vertices[1].x - vertices[0].x;
            float dy = vertices[1].y - vertices[0].y;
            float dz = vertices[1].z - vertices[0].z;
            length = (float)Math.Sqrt((dx*dx)+(dy*dy)+(dz*dz));
        }

        #endregion

        /// <summary>
        /// what is the occupancy probability at the given point ?
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float probability(float x, float y)
        {
            float dx, dy;
            float prob = -1;

            // what is the distance from the observation point ?
            dx = x - observedFrom.x;
            dy = y - observedFrom.y;
            float distFromObserver = (float)Math.Sqrt((dx*dx)+(dy*dy));

            if (distFromObserver >= start_dist)
            {
                if (distFromObserver < start_dist + length)
                {
                    //this just assumes that the highest probability
                    //is at the fattest part of the ray, which is actually wrong
                    //but will do for now
                    float fraction = (distFromObserver - start_dist) / length;
                    if (fraction > fattestPoint)
                    {
                        prob = (fraction - fattestPoint) / (1.0f - fattestPoint);
                    }
                    else
                    {
                        prob = (fattestPoint - fraction) / fattestPoint;
                    }
                    //prob = 1.0f - prob;
                    prob = stereoModel.Gaussian(prob);
                }
            }

            return (prob);
        }

        public float probability(float x, float y, float[] gaussianLookup, float forwardBias)
        {
            float dx, dy;
            float prob = -1;

            // what is the distance from the observation point ?
            dx = x - observedFrom.x;
            dy = y - observedFrom.y;
            float distFromObserver = (float)Math.Sqrt((dx * dx) + (dy * dy));

            if (distFromObserver >= start_dist)
            {
                if (distFromObserver < start_dist + length)
                {
                    //this just uses a crude forward adjustment parameter
                    //to bias the peak probability position.
                    //Better than this is possible.
                    float fraction = (distFromObserver - start_dist) / length;
                    float peakProbability = fattestPoint - ((0.5f-fattestPoint)*forwardBias);                    
                    if (fraction > peakProbability)
                    {
                        prob = (fraction - peakProbability) / (1.0f - peakProbability);
                    }
                    else
                    {
                        prob = (peakProbability - fraction) / peakProbability;
                    }
                    //prob = 1.0f - prob;
                    int idx = (int)((prob + 1.0f) * (gaussianLookup.Length-1) / 2.0f);
                    prob = gaussianLookup[idx];
                }
            }

            return (prob);
        }


        /// <summary>
        /// translate and rotate the ray by the given values
        /// </summary>
        /// <param name="r"></param>        
        public void translateRotate(pos3D r)
        {
            //take a note of where the ray was observed from
            //in egocentric coordinates.  This will help to
            //restrict localisation searching
            observedFrom = new pos3D(r.x, r.y, r.z);
            observedFrom.pan = r.pan;
            observedFrom.tilt = r.tilt;

            //centre = centre.rotate(r.pan, r.tilt, r.roll);
            for (int v = 0; v < 2; v++)
                vertices[v] = vertices[v].rotate(r.pan, r.tilt, r.roll);

            //store the XY plane pan angle, which may later be used
            //to reduce the searching during localisation            
            pan_angle = vertices[0].new_pan_angle;
            pan_index = (int)(pan_angle * pan_steps / (2 * 3.1415927f));
            start_dist = vertices[0].dist_xy;
            length = vertices[1].dist_xy - start_dist;            

            //centre = centre.translate(r.x, r.y, r.z);
            for (int v = 0; v < 2; v++)
                vertices[v] = vertices[v].translate(r.x, r.y, r.z);
        }
        

        /// <summary>
        /// returns a version of this evidence ray rotated through the given 
        /// pan angle in the XY plane.  This is used for generating trial poses.
        /// </summary>
        /// <param name="extra_pan"></param>
        /// <returns></returns>
        public evidenceRay trialPose(float extra_pan,
                                     float translation_x, float translation_y)
        {
            evidenceRay rotated_ray = new evidenceRay();
            float new_pan_angle = extra_pan + pan_angle;

            // as observed from the centre of the camera baseline
            rotated_ray.observedFrom = new pos3D(translation_x, translation_y, 0);
            rotated_ray.fattestPoint = fattestPoint;

            rotated_ray.vertices[0] = new pos3D(0, 0, 0);
            rotated_ray.vertices[0].x = translation_x + (start_dist * (float)Math.Sin(new_pan_angle));
            rotated_ray.vertices[0].y = translation_y + (start_dist * (float)Math.Cos(new_pan_angle));
            rotated_ray.vertices[0].z = vertices[0].z;

            rotated_ray.vertices[1] = new pos3D(0, 0, 0);
            rotated_ray.vertices[1].x = translation_x + ((start_dist + length) * (float)Math.Sin(new_pan_angle));
            rotated_ray.vertices[1].y = translation_y + ((start_dist + length) * (float)Math.Cos(new_pan_angle));
            rotated_ray.vertices[1].z = vertices[1].z;

            rotated_ray.pan_angle = new_pan_angle;
            rotated_ray.pan_index = (int)(new_pan_angle * pan_steps / 6.2831854f);
            rotated_ray.length = length;
            rotated_ray.width = width;
            rotated_ray.start_dist = start_dist;
            rotated_ray.disparity = disparity;

            for (int col = 2; col >= 0; col--)
                rotated_ray.colour[col] = colour[col];

            return (rotated_ray);
        }
    }
}
