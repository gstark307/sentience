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
    public class robotPath
    {
        private ArrayList viewpoints = new ArrayList();

        public void Add(viewpoint v)
        {
            viewpoints.Add(v);
        }

        public void Clear()
        {
            viewpoints.Clear();
        }

        public viewpoint getViewpoint(int index)
        {
            return((viewpoint)viewpoints[index]);
        }

        public int getNoOfViewpoints()
        {
            return (viewpoints.Count);
        }

        /// <summary>
        /// update the odometry positions
        /// </summary>
        /// <param name="odometry"></param>
        public void update(robotOdometry odometry)
        {
            for (int v = 0; v < viewpoints.Count; v++)
            {
                viewpoint view = (viewpoint)viewpoints[v];
                view.odometry_position = odometry.getPosition(v);
            }
        }

        /// <summary>
        /// shows rays along the path
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        /// <param name="scale"></param>
        public void showRays(Byte[] img, int img_width, int img_height, int scale)
        {
            // clear the image
            for (int i = 0; i < img.Length; i++) img[i] = 0;

            if (viewpoints.Count > 0)
            {
                // find the centre of the path
                float centre_x = 0;
                float centre_y = 0;
                for (int v = 0; v < viewpoints.Count; v++)
                {
                    viewpoint view = (viewpoint)viewpoints[v];
                    centre_x += view.odometry_position.x;
                    centre_y += view.odometry_position.y;
                }
                centre_x /= viewpoints.Count;
                centre_y /= viewpoints.Count;

                // insert the viewpoints
                for (int v = 0; v < viewpoints.Count; v++)
                {
                    viewpoint view = (viewpoint)viewpoints[v];
                    float x = view.odometry_position.x - centre_x;
                    float y = view.odometry_position.y - centre_y;
                    float pan = view.odometry_position.pan;
                    viewpoint viewAdjusted = view.createTrialPose(pan, x, y);
                    viewAdjusted.showAbove(img, img_width, img_height, scale, 255, 255, 255, true, 0, false);
                }
            }
        }

        // find the centre of the path
        public pos3D pathCentre()
        {            
            float centre_x = 0;
            float centre_y = 0;
            for (int v = 0; v < viewpoints.Count; v++)
            {
                viewpoint view = (viewpoint)viewpoints[v];
                centre_x += view.odometry_position.x;
                centre_y += view.odometry_position.y;
            }
            centre_x /= viewpoints.Count;
            centre_y /= viewpoints.Count;

            pos3D centre = new pos3D(centre_x, centre_y, 0);
            return (centre);
        }

        public void show(Byte[] img, int img_width, int img_height, int scale)
        {
            if (viewpoints.Count > 0)
            {
                // find the centre of the path
                pos3D centre = pathCentre();

                // insert the viewpoints
                int prev_x = 0;
                int prev_y = 0;
                for (int v = 0; v < viewpoints.Count; v++)
                {
                    viewpoint view = (viewpoint)viewpoints[v];
                    float x = view.odometry_position.x - centre.x;
                    float y = view.odometry_position.y - centre.y;

                    int screen_x = (int)(x * scale / 10000.0f * img_width/640) + (img_width / 2);
                    int screen_y = (int)(y * scale / 10000.0f * img_height/480) + (img_height / 2);

                    if (v > 0)
                        util.drawLine(img, img_width, img_height, screen_x, screen_y, prev_x, prev_y, 0, 255, 0, v * 4 / viewpoints.Count, false);

                    prev_x = screen_x;
                    prev_y = screen_y;
                }
            }
        }
    }


}
