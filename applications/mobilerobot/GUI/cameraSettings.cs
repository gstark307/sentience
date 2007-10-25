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
using System.Collections.Generic;
using System.Text;

namespace WindowsApplication1
{
    public class cameraSettings
    {
        public String cameraName = "";
        public String frameRate = "";
        public String resolution = "";
        public bool firstTime = true;
        public bool leftImage;

        public void Load()
        {
            //System.IO.File oFile;
            StreamReader oRead=null;
            String str;
            bool filefound = true;
            String filename;

            if (leftImage)
                filename = "leftCamera.txt";
                else
                filename = "rightCamera.txt";
            cameraName = "";

            try
            {
                oRead = File.OpenText(System.Windows.Forms.Application.StartupPath + "\\" + filename);
            }
            catch //(Exception ex)
            {
                filefound = false;
            }

            if (filefound)
            {
                str = oRead.ReadLine();
                if (str!=null) cameraName = str;

                str = oRead.ReadLine();
                if (str != null) frameRate = str;

                str = oRead.ReadLine();
                if (str != null) resolution = str;

                oRead.Close();
            }
        }


        public void Save()
        {
            StreamWriter oWrite=null;
            bool allowWrite = true;
            String filename;

            if (leftImage)
                filename = "leftCamera.txt";
                else
                filename = "rightCamera.txt";

            try
            {
                oWrite = File.CreateText(System.Windows.Forms.Application.StartupPath + "\\" + filename);
            }
            catch //(Exception ex)
            {
                allowWrite = false;
            }

            if (allowWrite)
            {
                oWrite.WriteLine(cameraName);
                oWrite.WriteLine(frameRate);
                oWrite.WriteLine(resolution);
                oWrite.Close();
            }

        }

    }
}
