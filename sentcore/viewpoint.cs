/*
    Sentience 3D Perception System
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
using System.Text;

namespace sentience.core
{
    /// <summary>
    /// stores rays for each camera
    /// </summary>
    public class viewpoint
    {
        //odometry position at which the measurements were taken
        public pos3D odometry_position;

        //rays thrown from this viewpoint
        public ArrayList[] rays;

        //pointers to adjacent views
        public viewpoint next = null;
        public viewpoint previous = null;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="no_of_cameras"></param>
        public viewpoint(int no_of_cameras)
        {
            odometry_position = new pos3D(0, 0, 0);

            rays = new ArrayList[no_of_cameras];
            for (int cam = 0; cam < no_of_cameras; cam++)
                rays[cam] = new ArrayList();
        }

        /// <summary>
        /// sets the odometry position for this viewpoint
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void set_odometry_position(float x, float y, float z)
        {
            odometry_position.x = x;
            odometry_position.y = y;
            odometry_position.z = z;
        }

        /// <summary>
        /// create a new viewpoint which is a trial pose
        /// </summary>
        /// <param name="extra_pan"></param>
        /// <param name="translation_x"></param>
        /// <param name="translation_y"></param>
        /// <returns></returns>
        public viewpoint createTrialPose(float extra_pan,
                                         float translation_x, float translation_y)
        {
            viewpoint trialPose = new viewpoint(rays.Length);

            for (int cam = 0; cam < rays.Length; cam++)
            {
                for (int r = 0; r < rays[cam].Count; r++)
                {
                    evidenceRay ray = (evidenceRay)rays[cam][r];
                    evidenceRay trial_ray = ray.trialPose(extra_pan, translation_x, translation_y);
                    trialPose.rays[cam].Add(trial_ray);
                }
            }
            return (trialPose);
        }

        /// <summary>
        /// returns a score indicating how closely matched the two viewpoints are
        /// </summary>
        /// <param name="other"></param>
        /// <param name="intersects"></param>
        /// <returns></returns>
        public float matchingScore(viewpoint other, float separation_tollerance, int ray_thickness, ArrayList intersects)
        {
            float separation;
            float score = 0;
            pos3D intersectPos = new pos3D(0, 0, 0);

            if (ray_thickness < 1) ray_thickness = 1;

            for (int cam1 = 0; cam1 < rays.Length; cam1++)
            {
                for (int ry1 = 0; ry1 < rays[cam1].Count; ry1++)
                {
                    evidenceRay ray1 = (evidenceRay)rays[cam1][ry1];
                    int pan_index1 = ray1.pan_index - 1;
                    int pan_index2 = ray1.pan_index;
                    int pan_index3 = ray1.pan_index + 1;
                    if (pan_index1 < 0) pan_index1 = evidenceRay.pan_steps - 1;
                    if (pan_index3 >= evidenceRay.pan_steps) pan_index3 = 0;

                    int cam2 = cam1;
                    //for (int cam2 = 0; cam2 < other.rays.Length; cam2++)
                    {
                        for (int ry2 = 0; ry2 < other.rays[cam2].Count; ry2++)
                        {
                            evidenceRay ray2 = (evidenceRay)other.rays[cam2][ry2];
                            int pan_index4 = ray2.pan_index;
                            if ((pan_index1 == pan_index4) ||
                                (pan_index2 == pan_index4) ||
                                (pan_index3 == pan_index4))
                            {
                                //do these rays intersect
                                separation = 999;
                                if (stereoModel.raysOverlap(ray1, ray2, intersectPos, separation_tollerance, ray_thickness, ref separation))
                                {
                                    float p1 = ray1.probability(intersectPos.x, intersectPos.y);
                                    float p2 = ray2.probability(intersectPos.x, intersectPos.y);
                                    float prob = (p1 * p2) + ((1.0f - p1) * (1.0f - p2));

                                    if (intersects != null)
                                    {
                                        // add the intersection to the list
                                        intersects.Add(intersectPos);
                                        intersectPos = new pos3D(0, 0, 0);
                                    }

                                    //increment the matching score
                                    score += 1.0f / (1.0f + ((1.0f - prob) * separation * separation));
                                    //score += 1.0f / (1.0f + (separation * separation) + (ray2.length * ray1.length / 2));
                                    //score += 1.0f / (1.0f + (separation * separation));
                                }
                            }
                        }
                    }

                }
            }
            return (score);
        }

        /// <summary>
        /// shows rays within the viewpoint from above
        /// </summary>
        /// <param name="img"></param>
        /// <param name="scale"></param>
        public void showAbove(Byte[] img, int img_width, int img_height, int scale,
                         Byte r, Byte g, Byte b, bool showProbabilities, int vertical_adjust, 
                         bool lowUncertaintyOnly)
        {
            Byte rr, gg, bb;
            int half_width = img_width / 2;
            int half_height = (img_height / 2);
            int vertAdjust = vertical_adjust * img_height / 100;
            float start_x, start_y, end_x, end_y;

            //img.clear();
            for (int cam = 0; cam < rays.Length; cam++)
            {
                for (int ry = 0; ry < rays[cam].Count; ry++)
                {
                    evidenceRay ray = (evidenceRay)rays[cam][ry];
                    if ((!lowUncertaintyOnly) || ((lowUncertaintyOnly) && (ray.uncertainty < 0.8f)))
                    {

                        rr = r;
                        gg = g;
                        bb = b;

                        if (!showProbabilities)
                        {
                            start_x = ray.vertices[0].x * half_width / scale;
                            start_y = ray.vertices[0].y * half_height / scale;
                            end_x = ray.vertices[1].x * half_width / scale;
                            end_y = ray.vertices[1].y * half_height / scale;

                            start_y -= vertAdjust;
                            end_y -= vertAdjust;

                            util.drawLine(img, img_width, img_height, half_width + (int)start_x, half_height + (int)start_y, half_width + (int)end_x, half_height + (int)end_y, rr, gg, bb, 0, true);
                        }
                        else
                        {
                            float dx = ray.vertices[1].x - ray.vertices[0].x;
                            float dy = ray.vertices[1].y - ray.vertices[0].y;

                            float sx = ray.vertices[0].x;
                            float sy = ray.vertices[0].y;
                            for (int l = 1; l < 50; l++)
                            {
                                float ex = ray.vertices[0].x + (dx * l / 50);
                                float ey = ray.vertices[0].y + (dy * l / 50);

                                int intensity = (int)(ray.probability(ex, ey) * 255);
                                rr = (Byte)(r * intensity / 255);
                                gg = (Byte)(g * intensity / 255);
                                bb = (Byte)(b * intensity / 255);

                                start_x = sx * half_width / scale;
                                start_y = sy * half_height / scale;
                                end_x = ex * half_width / scale;
                                end_y = ey * half_height / scale;

                                start_y -= vertAdjust;
                                end_y -= vertAdjust;

                                util.drawLine(img, img_width, img_height, half_width + (int)start_x, half_height + (int)start_y, half_width + (int)end_x, half_height + (int)end_y, rr, gg, bb, 0, true);

                                sx = ex;
                                sy = ey;
                            }

                        }
                    }
                }
            }
        }


        /// <summary>
        /// shows rays within the viewpoint from the front
        /// </summary>
        /// <param name="img"></param>
        /// <param name="scale"></param>
        public void showFront(Byte[] img, int img_width, int img_height, int scale,
                         Byte r, Byte g, Byte b, bool showProbabilities)
        {
            Byte rr, gg, bb;
            int half_width = img_width / 2;
            int half_height = img_height / 2;
            float start_x, start_z, end_x, end_z;

            //img.clear();
            for (int cam = 0; cam < rays.Length; cam++)
            {
                for (int ry = 0; ry < rays[cam].Count; ry++)
                {
                    evidenceRay ray = (evidenceRay)rays[cam][ry];

                    rr = r;
                    gg = g;
                    bb = b;

                    if (!showProbabilities)
                    {
                        start_x = ray.vertices[0].x * half_width / scale;
                        start_z = ray.vertices[0].z * half_height / scale;
                        end_x = ray.vertices[1].x * half_width / scale;
                        end_z = ray.vertices[1].z * half_height / scale;

                        util.drawLine(img, img_width, img_height, half_width + (int)start_x, half_height + (int)start_z, half_width + (int)end_x, half_height + (int)end_z, rr, gg, bb, 0, true);
                    }
                    else
                    {
                        float dx = ray.vertices[1].x - ray.vertices[0].x;
                        float dy = ray.vertices[1].y - ray.vertices[0].y;
                        float dz = ray.vertices[1].z - ray.vertices[0].z;

                        float sx = ray.vertices[0].x;
                        float sz = ray.vertices[0].z;
                        for (int l = 1; l < 50; l++)
                        {
                            float ex = ray.vertices[0].x + (dx * l / 50);
                            float ey = ray.vertices[0].y + (dy * l / 50);
                            float ez = ray.vertices[0].z + (dz * l / 50);

                            int intensity = (int)(ray.probability(ex, ey) * 255);
                            rr = (Byte)(r * intensity / 255);
                            gg = (Byte)(g * intensity / 255);
                            bb = (Byte)(b * intensity / 255);

                            start_x = sx * half_width / scale;
                            start_z = sz * half_height / scale;
                            end_x = ex * half_width / scale;
                            end_z = ez * half_height / scale;
                            util.drawLine(img, img_width, img_height, half_width + (int)start_x, half_height + (int)start_z, half_width + (int)end_x, half_height + (int)end_z, rr, gg, bb, 0, true);

                            sx = ex;
                            sz = ez;
                        }
                    }
                }
            }
        }



        /// <summary>
        /// used for showing intersection points
        /// </summary>
        /// <param name="img"></param>
        /// <param name="points"></param>
        /// <param name="scale"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public void showPoints(Byte[] img, int img_width, int img_height, ArrayList points, int scale, Byte r, Byte g, Byte b)
        {
            int half_width = img_width / 2;
            int half_height = img_height / 2;

            //img.clear();
            for (int i = 0; i < points.Count; i++)
            {
                pos3D pt = (pos3D)points[i];
                int x = half_width + (int)(pt.x * half_width / scale);
                int y = half_height + (int)(pt.y * half_height / scale);
                if ((x > 0) && (y > 0) && (x < img_width) && (y < img_height))
                {
                    for (int xx = x - 1; xx <= x; xx++)
                        for (int yy = y - 1; yy <= y; yy++)
                        {
                            int n = ((img_width * yy) + xx)*3;
                            img[n+2] = r;
                            img[n+1] = g;
                            img[n] = b;
                        }
                }
            }
        }
    }
}
