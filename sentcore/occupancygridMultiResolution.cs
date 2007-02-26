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
    public class occupancygridMultiResolution : pos3D
    {
        public occupancygrid[] grid;
        public int levels;
        public int dimension;
        public float cellSize_mm;

        private void init(int levels, int dimension, float max_cellSize_mm)
        {
            this.levels = levels;
            this.dimension = dimension;
            this.cellSize_mm = max_cellSize_mm;

            grid = new occupancygrid[levels];
            for (int l = 0; l < levels; l++)
            {
                grid[l] = new occupancygrid(dimension, getCellSize(l));
                grid[l].parent = this;
            }
        }

        /// <summary>
        /// get the grid cell size at the given level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public float getCellSize(int level)
        {
            return (cellSize_mm / ((level * 3) + 1));
        }

        public void showVacancyFunction(Byte[] img, int img_width, int img_height)
        {
            if (grid[0] != null)
                grid[0].showVacancyFunction(img, img_width, img_height);
        }

        public occupancygridMultiResolution(int levels, int dimension, int max_cellSize_mm) : base(0,0,0)
        {
            init(levels, dimension, max_cellSize_mm);
        }

        public void Clear()
        {
            for (int l = 0; l < levels; l++) grid[l].clear();
        }

        public void setCellPeakGain(float gain)
        {
            for (int l = 0; l < levels; l++) grid[l].cellPeakGain = gain;
        }

        public void Save(int level, String filename)
        {
            grid[level].Save(filename);
        }

        public void Load(int level, String filename)
        {
            grid[level].Load(filename);
        }

        public void Show(int level, Byte[] img, int img_width, int img_height)
        {
            grid[level].show(img, img_width, img_height);
        }

        /// <summary>
        /// return the number of matched cells after localisation
        /// </summary>
        /// <returns></returns>
        public float matchedCells()
        {
            float matches = 0;
            for (int l = 0; l < levels; l++) matches += (grid[l].matchedCells * (levels-l));
            return (matches);
        }

        /// <summary>
        /// return the number of occupied cells for all levels
        /// </summary>
        /// <returns></returns>
        public int occupiedCells()
        {
            int occupied = 0;
            for (int l = 0; l < levels; l++) occupied += grid[l].occupiedCells;
            return (occupied);
        }


        /// <summary>
        /// insert a path into the grids
        /// </summary>
        /// <param name="p"></param>
        public void insert(robotPath p, bool autoCentre)
        {
            for (int l = 0; l < levels; l++) grid[l].insert(p, autoCentre);
        }

        /// <summary>
        /// insert a viewpoint into the grids
        /// </summary>
        /// <param name="v"></param>
        /// <param name="mapping"></param>
        /// <param name="centre"></param>
        public void insert(viewpoint v, bool mapping, pos3D centre)
        {
            for (int l = 0; l < levels; l++) grid[l].insert(v, mapping, centre);
        }

        /// <summary>
        /// insert a ray into the grids
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mapping"></param>
        /// <param name="origin"></param>
        public void insert(evidenceRay ray, bool mapping, pos3D origin)
        {
            for (int l = 0; l < levels; l++) grid[l].insert(ray, mapping, origin);
        }


        public void setForwardBias(float bias)
        {
            for (int l = 0; l < levels; l++) grid[l].forwardBias = bias;
        }
    }
}
