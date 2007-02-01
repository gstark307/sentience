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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sentience.core
{
    public class robotOdometry
    {
        public int countsPerRev = 4096;
        public float wheelDiameter_mm = 100;
        public float wheelSeparation_mm = 500;

        public int no_of_measurements = 0;
        ArrayList leftWheel, rightWheel; // odometry readings for left and right wheels
        ArrayList position;  // odometry converted into x,y positions

        public robotOdometry()
        {
            leftWheel = new ArrayList();
            rightWheel = new ArrayList();
            position = new ArrayList();
        }

        public void Clear()
        {
            leftWheel.Clear();
            rightWheel.Clear();
            position.Clear();
        }

        /// <summary>
        /// returns the length of the odometry path in mm
        /// </summary>
        /// <returns></returns>
        public float Length()
        {
            float tot = 0;
            for (int i = 1; i < position.Count; i++)
            {
                pos3D p1 = (pos3D)position[i-1];
                pos3D p2 = (pos3D)position[i-1];
                float dx = p2.x - p1.x;
                float dy = p2.y - p1.y;
                tot += (float)Math.Sqrt((dx*dx)+(dy*dy));
            }
            return (tot);
        }

        public void Add(long leftWheelCounts, long rightWheelCounts)
        {
            leftWheel.Add(leftWheelCounts);
            rightWheel.Add(rightWheelCounts);
            updatePositions();
            no_of_measurements = leftWheel.Count;
        }

        public void AddPosition(float x, float y, float pan)
        {
            pos3D pos = new pos3D(x, y, 0);
            pos.pan = pan;
            position.Add(pos);
            no_of_measurements = position.Count;
        }

        public pos3D getPosition(int index)
        {
            return ((pos3D)position[index]);
        }

        public void setPosition(int index, float x, float y, float z, float pan)
        {
            pos3D p = (pos3D)position[index];
            p.x = x;
            p.y = y;
            p.z = z;
            p.pan = pan;
        }

        /// <summary>
        /// read in one of Hans' PATH files storing ground truth data
        /// </summary>
        /// <param name="filename"></param>
        public void readPathFile(String filename, String path_index)
        {
            StreamReader oRead = null;
            String str;
            bool filefound = true;

            try
            {
                oRead = File.OpenText(filename);
            }
            catch //(Exception ex)
            {
                filefound = false;
            }

            if (filefound)
            {
                position.Clear();

                //read the first line and throw it away
                str = oRead.ReadLine();

                if (path_index!="M")
                    for (int i=0;i<=8;i++)
                        str = oRead.ReadLine();

                no_of_measurements = 0;
                for (int i = 0; i <= 8; i++)
                {
                    str = oRead.ReadLine();
                    String[] data = str.Split(' ');
                    float x = Convert.ToSingle(data[2]);
                    float y = Convert.ToSingle(data[3]);
                    float pan = Convert.ToSingle(data[4]);
                    pos3D pos = new pos3D(x, y, 0);
                    pos.pan = pan;
                    position.Add(pos);
                    no_of_measurements++;
                }
                oRead.Close();
            }
        }

        /// <summary>
        /// read sentience path file storing ground truth data
        /// </summary>
        /// <param name="filename"></param>
        public void readPathFileSentience(String filename, String trackType, robot rob)
        {
            StreamReader oRead = null;
            String str;
            bool filefound = true;

            try
            {
                oRead = File.OpenText(filename);
            }
            catch //(Exception ex)
            {
                filefound = false;
            }

            if (filefound)
            {
                bool mapping;
                int forward_mm, right_mm, height_mm, pan_degrees;
                int prev_fwd = -1;
                String left_filename, right_filename;

                position.Clear();

                //read the first line and throw it away
                no_of_measurements = 0;
                str = oRead.ReadLine();
                while ((str != null) && (!oRead.EndOfStream))
                {
                    if (str == "True")
                        mapping = true;
                    else
                        mapping = false;

                    forward_mm = Convert.ToInt32(oRead.ReadLine());
                    right_mm = Convert.ToInt32(oRead.ReadLine());
                    height_mm = Convert.ToInt32(oRead.ReadLine());
                    pan_degrees = Convert.ToInt32(oRead.ReadLine());
                    left_filename = oRead.ReadLine();
                    right_filename = oRead.ReadLine();

                    if (((mapping) && (trackType == "M")) ||
                        ((!mapping) && (trackType == "L")))
                    {
                        if (prev_fwd < forward_mm)
                        {
                            pos3D pos = new pos3D(right_mm, forward_mm, height_mm);
                            pos.pan = pan_degrees / 180.0f * 3.1415927f;
                            position.Add(pos);
                            prev_fwd = forward_mm;
                            no_of_measurements++;
                        }

                        //rob.head.imageFilename[i] = image_filename;                      
                    }
 
                    str = oRead.ReadLine();
                }

                oRead.Close();
            }
        }

        private void getPositionChange(long leftWheelCounts, long rightWheelCounts,
                                       long prev_leftWheelCounts, long prev_rightWheelCounts,
                                       ref float dx, ref float dy, ref float dpan)
        {
            long leftWheelCountsDiff = leftWheelCounts - prev_leftWheelCounts;
            long rightWheelCountsDiff = rightWheelCounts - prev_rightWheelCounts;
            float leftWheelDist = leftWheelCountsDiff * (float)Math.PI * wheelDiameter_mm / (float)countsPerRev;
            float rightWheelDist = rightWheelCountsDiff * (float)Math.PI * wheelDiameter_mm / (float)countsPerRev;
            //TODO: update position

            dx = 0;
            dy = 0;
            dpan = 0;
        }


        /// <summary>
        /// return the distance travelled since the last recorded point
        /// </summary>
        /// <param name="leftWheelCounts"></param>
        /// <param name="rightWheelCounts"></param>
        /// <returns>dist travelled in mm</returns>
        public float distFromLastPoint(long leftWheelCounts, long rightWheelCounts)
        {
            float dist = 9999;
            float dx = 0;
            float dy = 0;
            float dpan = 0;

            int p = leftWheel.Count-1;
            if (p > -1)
            {
                getPositionChange(leftWheelCounts, rightWheelCounts,
                                  (long)leftWheel[p], (long)rightWheel[p],
                                  ref dx, ref dy, ref dpan);
                dist = (float)Math.Sqrt((dx*dx)+(dy*dy));
            }
            return (dist);
        }

        /// <summary>
        /// update the robot position based using the latest odometry position
        /// </summary>
        /// <param name="rob"></param>
        public void updateRobotPosition(robot rob)
        {
            if (position.Count - 1 > -1)
            {
                rob.x = (float)position[position.Count - 1];
                rob.y = (float)position[position.Count - 1];
                rob.pan = (float)position[position.Count - 1];
            }
            else
            {
                rob.x = 0;
                rob.y = 0;
                rob.pan = 0;
            }
        }


        public void updatePositions()
        {
            for (int p = position.Count; p < leftWheel.Count; p++)
            {
                pos3D pos = new pos3D(0, 0, 0);

                if (p != 0)
                {
                    float dx = 0;
                    float dy = 0;
                    float dpan = 0;
                    getPositionChange((long)leftWheel[p], (long)rightWheel[p],
                                      (long)leftWheel[p - 1], (long)rightWheel[p - 1],
                                      ref dx, ref dy, ref dpan);
                    pos.x = (float)position[position.Count - 1] + dx;
                    pos.y = (float)position[position.Count - 1] + dy;
                    pos.pan = (float)position[position.Count - 1] + dpan;
                }
                else
                {
                    pos.x = 0;
                    pos.y = 0;
                    pos.pan = 0;
                }
                position.Add(pos);
            }
        }

        public void show(Byte[] img, int img_width, int img_height, int r, int g, int b, int scale)
        {
            if (position.Count > 0)
            {
                // find the centre of the path
                float centre_x = 0;
                float centre_y = 0;
                for (int v = 0; v < position.Count; v++)
                {
                    pos3D view = (pos3D)position[v];
                    centre_x += view.x;
                    centre_y += view.y;
                }
                centre_x /= position.Count;
                centre_y /= position.Count;

                // insert the positions
                int prev_x = 0;
                int prev_y = 0;
                for (int v = 0; v < position.Count; v++)
                {
                    pos3D view = (pos3D)position[v];
                    float x = view.x - centre_x;
                    float y = view.y - centre_y;

                    int screen_x = (int)(x * scale / 10000.0f * img_width / 640) + (img_width / 2);
                    int screen_y = (int)(y * scale / 10000.0f * img_height / 480) + (img_height / 2);

                    if (v > 0)
                        util.drawLine(img, img_width, img_height, screen_x, screen_y, prev_x, prev_y, r, g, b, 1, false);

                    prev_x = screen_x;
                    prev_y = screen_y;
                }
            }
        }

    }
}
