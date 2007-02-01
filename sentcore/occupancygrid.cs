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
    public class occupancyGridParticle : pos3Dbase
    {
        public float probability;

        public occupancyGridParticle(float x, float y, float z, float probability) : base(x,y,z)
        {
            this.probability = probability;
        }
    }
    
    public class occupancyGridCell
    {
        // occupancy value of the cell
        public float occupancy = 0;

        // position of peak occupancy within the cell
        public pos3Dbase occupancy_peak = null;
        public ArrayList particles = null;

        public void setOccupancyPeak(float x, float y, float z)
        {
            occupancy_peak.x = x;
            occupancy_peak.y = y;
            occupancy_peak.z = z;
        }

        /// <summary>
        /// add a new probability inside the cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="probability"></param>
        public void setProbability(float x, float y, float z, float probability)
        {
            if (particles == null)
                particles = new ArrayList();

            particles.Add(new occupancyGridParticle(x, y, z, probability));
        }

        // average colour of the cell
        public Byte[] colour = new Byte[3];

        // variation in colour
        public float colour_variance = 0;
        public int colour_variance_hits = 1;

        public occupancyGridCell()
        {
            occupancy_peak = new pos3Dbase(-1, -1, -1);
        }
    }

    public class occupancygrid
    {
        public int dimension;            // how many cells across
        private float half_dimension_mm;
        public float cellSize_mm = 10;
        public occupancyGridCell[, ,] cell;
        public int occupiedCells = 0;
        public float matchedCells = 0;
        private float[] gaussLookup;
        private float maxProbability = 1;
        public float forwardBias = 0.2f;
        public bool interpolationEnabled = true;
        public float cellPeakGain = 0.2f;  //gain used to adjust the position of peak occupancy inside a cell
        ArrayList occupations = new ArrayList();

        public bool usePlanView = true;
        public float[,] plan_view;             // a 2D overhead view of the grid
        public bool[, ,] display_cell = null;  // which cells should be displayed when visualising the grid
        public bool[,]   empty = null;         // which cells are empty, and can be used for path planning

        // how wide is the ray when doing localisation ?
        public int localisationRayWidth = 2;

        // how wide is the ray when creating a map ?
        public int mappingRayWidth = 1;

        private void init(int dimension, float cellSize_mm)
        {
            this.dimension = dimension;
            this.cellSize_mm = cellSize_mm;
            cell = new occupancyGridCell[dimension, dimension, dimension];
            plan_view = new float[dimension, dimension];
            half_dimension_mm = dimension * cellSize_mm / 2;
            gaussLookup = stereoModel.createGaussianLookup(50);
        }

        public occupancygrid(int dimension, float cellSize_mm)        
        {
            init(dimension, cellSize_mm);
        }

        /// <summary>
        /// clear the grid
        /// </summary>
        public void clear()
        {
            // clear the plan view
            if (usePlanView)
                for (int x = 0; x < dimension; x++)
                    for (int y = 0; y < dimension; y++)
                        plan_view[x, y] = 0;

            // clear the 3D grid
            for (int i = 0; i < occupations.Count; i++)
            {
                occupancyGridCell c = (occupancyGridCell)occupations[i];
                c.colour[0] = 0;
                c.occupancy = 0;
                if (c.particles != null) c.particles.Clear();
            }

            // clear the occupancy list
            occupations.Clear();
            occupiedCells = 0;
        }

        /// <summary>
        /// counts the number of empty cells within the neighbourhood
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private int emptyCellsNearby(int x, int y, int radius)
        {
            int emptyCells = 0;
            for (int xx = x - radius; xx < x + radius; xx++)
            {
                if ((xx > -1) && (xx < dimension))
                {
                    for (int yy = y - radius; yy < y + radius; yy++)
                    {
                        if ((yy > -1) && (yy < dimension))
                        {
                            if (plan_view[xx, yy] < 0)
                            {
                                emptyCells++;
                            }
                        }
                    }
                }
            }
            return (emptyCells);
        }



        /// <summary>
        /// Create a binary grid which is used for display purposes
        /// This also creates a 2D grid indicating which areas are empty, which may be used for path planning
        /// </summary>
        public void updateDisplayCells()
        {
            int min_column_hits = dimension / 20;

            // create the display grid if necessary
            if (display_cell == null)
            {
                display_cell = new bool[dimension, dimension, dimension];
                empty = new bool[dimension, dimension];
            }


            //clear the grid and find the average confidence
            float average_confidence = 0;
            int hits = 0;
            for (int x = 0; x < dimension; x++)
            {
                for (int y = 0; y < dimension; y++)
                {
                    if (plan_view[x, y] != 0)
                    {
                        // update the average confidence
                        if (plan_view[x, y] > 0)
                        {
                            average_confidence += plan_view[x, y];
                            hits++;
                        }
                    }

                    for (int z = 0; z < dimension; z++)
                    {
                        display_cell[x, y, z] = false;
                    }
                }
            }
            if (hits > 0) average_confidence /= hits;

            average_confidence *= 10 / 100;
            float occupancy_threshold = 0.03f * dimension;

            float average_colour_variance = getAverageColourVariance();
            average_colour_variance *= 100 / 100;

            for (int x = 1; x < dimension - 1; x++)
            {
                for (int y = 1; y < dimension - 1; y++)
                {
                    bool unseen = false;
                    empty[x, y] = false;
                    if (plan_view[x, y] == 0) unseen = true;
                    float occ = plan_view[x, y] - average_confidence;

                    // detect empty cells
                    if (emptyCellsNearby(x, y, 3) > 30)
                    {
                        empty[x, y] = true;
                        for (int z = 0; z < dimension; z++)
                        {
                            occupancyGridCell c = cell[x, y, z];
                            if (c != null)
                            {
                                //if (c.occupancy > occupancy_threshold) display_cell[x, y, z] = true;
                            }
                        }
                    }

                    if (occ < 0) occ = 0;
                    int intens = (int)(occ * 255 / 5);
                    if (intens > 255) intens = 255;
                    Byte intensity = (Byte)intens;
                    if (unseen) intensity = 0;

                    if (emptyCellsNearby(x, y, 2) > 1)
                    {
                        if (intensity > 0)
                        {
                            int verticality = 0;
                            for (int z = 0; z < dimension; z++)
                            {
                                occupancyGridCell c = cell[x, y, z];
                                if (c != null)
                                {
                                    if (c.occupancy > occupancy_threshold)
                                    {
                                        //if ((c.colour[0] > 10) && (c.colour[1] > 10) && (c.colour[2] > 10))
                                        {
                                            display_cell[x, y, z] = true;
                                            verticality++;
                                        }
                                    }
                                }
                            }
                            if (verticality < 5)
                            {
                                for (int z = 0; z < dimension; z++)
                                    display_cell[x, y, z] = false;
                            }
                        }
                    }
                }
            }

            int col_width = 0;
            for (int x = 1; x < dimension - 1; x++)
            {
                for (int y = 1; y < dimension - 1; y++)
                {
                    int column_hits = 0;
                    for (int z = 0; z < dimension; z++)
                    {
                        for (int xx = x - col_width; xx <= x; xx++)
                            for (int yy = y - col_width; yy <= y; yy++)
                                if (display_cell[xx, yy, z]) column_hits++;
                    }

                    if (column_hits > min_column_hits)
                    {
                        int prev_z = -1;
                        int r=0, g=0, b=0;
                        int prev_r=0, prev_g=0, prev_b=0;
                        for (int z = 0; z < dimension; z++)
                        {
                            column_hits = 0;
                            r = 0;
                            g = 0;
                            b = 0;
                            for (int xx = x - col_width; xx <= x; xx++)
                                for (int yy = y - col_width; yy <= y; yy++)
                                    if (display_cell[xx, yy, z])
                                    {
                                        column_hits++;
                                        r += cell[xx, yy, z].colour[0];
                                        g += cell[xx, yy, z].colour[1];
                                        b += cell[xx, yy, z].colour[2];
                                    }

                            if ((prev_z > -1) && (column_hits > 1) && (z - prev_z < 3))
                            {
                                for (int zz = prev_z; zz <= z; zz++)
                                {
                                    if (cell[x, y, zz] != null)
                                        display_cell[x, y, z] = true;
                                    else
                                    {
                                        int rr = r / column_hits;
                                        int gg = g / column_hits;
                                        int bb = b / column_hits;
                                        cell[x, y, z] = new occupancyGridCell();
                                        cell[x, y, z].colour[0] = (Byte)(rr + ((prev_r - rr) / 2));
                                        cell[x, y, z].colour[1] = (Byte)(gg + ((prev_g - gg) / 2));
                                        cell[x, y, z].colour[2] = (Byte)(bb + ((prev_b - bb) / 2));
                                        display_cell[x, y, z] = true;
                                    }
                                }
                            }

                            if (column_hits > 1)
                            {
                                prev_r = r / column_hits;
                                prev_g = g / column_hits;
                                prev_b = b / column_hits;
                                prev_z = z;
                            }
                        }
                    }

                }
            }

        }

        /// <summary>
        /// insert the given path into the grid
        /// </summary>
        /// <param name="p"></param>
        public void insert(robotPath p)
        {
            pos3D centre = p.pathCentre();
            //centre.y += 1000;
            int length = p.getNoOfViewpoints();
            for (int i = 0; i < length; i++)
            {
                viewpoint v = p.getViewpoint(i);
                insert(v, true, centre);
            }
        }

        /// <summary>
        /// insert a viewpoint into the grid
        /// </summary>
        /// <param name="v"></param>
        public void insert(viewpoint v, bool mapping, pos3D centre)
        {
            if (!mapping) matchedCells = 0;

            for (int cam = 0; cam < v.rays.Length; cam++)
            {
                for (int ry1 = 0; ry1 < v.rays[cam].Count; ry1++)
                {
                    evidenceRay ray = (evidenceRay)v.rays[cam][ry1];
                    insert(ray, mapping, v.odometry_position.subtract(centre));
                }
            }

        }


        /// <summary>
        /// load the grid
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
        {
            FileStream fp = new FileStream(filename, FileMode.Open);
            BinaryReader binfile = new BinaryReader(fp);

            // write the size of the grid
            dimension = binfile.ReadInt32();
            cellSize_mm = binfile.ReadSingle();
            init(dimension, cellSize_mm);

            // read the plan view
            for (int y = 0; y < dimension; y++)
                for (int x = 0; x < dimension; x++)
                    plan_view[x, y] = binfile.ReadSingle();

            // read the binary header and create cells
            for (int z = 0; z < dimension; z++)
            {
                for (int y = 0; y < dimension; y++)
                {
                    for (int x = 0; x < dimension; x++)
                    {
                        bool isOccupied = binfile.ReadBoolean();
                        if (isOccupied)
                            cell[x, y, z] = new occupancyGridCell();
                    }
                }
            }

            // now read the data for the occupied cells
            occupations.Clear();
            for (int z = 0; z < dimension; z++)
            {
                for (int y = 0; y < dimension; y++)
                {
                    for (int x = 0; x < dimension; x++)
                    {
                        occupancyGridCell c = cell[x, y, z];
                        if (c != null)
                        {
                            occupations.Add(c);
                            c.occupancy = binfile.ReadSingle();
                            c.colour_variance = binfile.ReadSingle();
                            c.colour_variance_hits = 1;
                            for (int col = 0; col < 3; col++)
                                c.colour[col] = binfile.ReadByte();
                        }
                    }
                }
            }

            binfile.Close();
            fp.Close();

            // update cells used for display of the grid
            updateDisplayCells();
        }


        /// <summary>
        /// save the grid
        /// </summary>
        public void Save(String filename)
        {
            FileStream fp = new FileStream(filename, FileMode.Create);
            BinaryWriter binfile = new BinaryWriter(fp);

            // write the size of the grid
            binfile.Write(dimension);
            binfile.Write(cellSize_mm);

            // write the plan view
            for (int y = 0; y < dimension; y++)
                for (int x = 0; x < dimension; x++)
                    binfile.Write(plan_view[x, y]);

            // write the binary header
            for (int z = 0; z < dimension; z++)
            {
                for (int y = 0; y < dimension; y++)
                {
                    for (int x = 0; x < dimension; x++)
                    {
                        occupancyGridCell c = cell[x, y, z];
                        if (c != null)
                            binfile.Write(true);
                        else 
                            binfile.Write(false);
                    }
                }
            }

            // now write the non-zero data
            for (int z = 0; z < dimension; z++)
            {
                for (int y = 0; y < dimension; y++)
                {
                    for (int x = 0; x < dimension; x++)
                    {
                        occupancyGridCell c = cell[x, y, z];
                        if (c != null)
                        {
                            binfile.Write(c.occupancy);
                            binfile.Write(c.colour_variance / c.colour_variance_hits);
                            for (int col = 0; col < 3; col++)
                                binfile.Write(c.colour[col]);
                        }
                    }
                }
            }

            binfile.Close();
            fp.Close();
        }


        /// <summary>
        /// update the peak position for the occupancy within the cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        private void updateOccupancyPeak(float x, float y, float z)
        {
            int int_x = (int)x;
            int int_y = (int)y;
            int int_z = (int)z;
            occupancyGridCell c = cell[int_x, int_y, int_z];
            if (c != null)
            {                
                if (c.occupancy_peak.x < 0)
                {
                    c.setOccupancyPeak(x - int_x, y - int_y, z - int_z);
                }
                else                 
                {
                    float dx = (x - int_x) - c.occupancy_peak.x;
                    float dy = (y - int_y) - c.occupancy_peak.y;
                    float dz = (z - int_z) - c.occupancy_peak.z;

                    c.occupancy_peak.x += (dx * cellPeakGain);
                    c.occupancy_peak.y += (dy * cellPeakGain);
                    c.occupancy_peak.z += (dz * cellPeakGain);
                }
            }
        }


        /// <summary>
        /// returns an interpolated occupancy value
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>        
        private float interpolatedOccupancy(float x, float y, float z)
        {
            occupancyGridCell c1, c2;
            float interpolated = 0;
            float occ1=0, occ2=0, factor, dx, dy, dz, cx, cy, cz;
            float cx2, cy2, cz2;
            int int_x = (int)x;
            int int_y = (int)y;
            int int_z = (int)z;

            c1 = cell[int_x, int_y, int_z];
            if (c1 != null)
            {
                cx = c1.occupancy_peak.x;
                cy = c1.occupancy_peak.y;
                cz = c1.occupancy_peak.z;
            }
            else
            {
                cx = 0.5f;
                cy = 0.5f;
                cz = 0.5f;
            }
            dx = (x - int_x) - cx;
            dy = (y - int_y) - cy;
            dz = (z - int_z) - cz;
            
            // get the cell occupancy at this location
            if (c1 != null) occ1 = c1.occupancy; else occ1 = 0;
            
            cx2 = 0.5f;
            cy2 = 0.5f;
            cz2 = 0.5f;
            if (dx > 0)
            {
                if (int_x < dimension-1)
                {
                    c2 = cell[int_x + 1, int_y, int_z];
                    if (c2 != null)
                    {
                        occ2 = c2.occupancy;
                        cx2 = c2.occupancy_peak.x;
                        if (cx2 < 0) cx2 = 0.5f;
                    }
                    else occ2 = 0;
                }
            }
            else
            {
                if (int_x > 0)
                {
                    c2 = cell[int_x - 1, int_y, int_z];
                    if (c2 != null)
                    {
                        occ2 = c2.occupancy;
                        cx2 = c2.occupancy_peak.x;
                        if (cx2 < 0) cx2 = 0.5f;
                    }
                    else occ2 = 0;
                }
            }
            if (dx >= 0)
                factor = (cx - dx) / ((1.0f - cx) + cx2);
            else
                factor = -(cx - dx) / ((1.0f - cx2) + cx);
            float x_occ = occ1 + ((occ2 - occ1) * factor);

            occ2 = 0;
            cx2 = 0.5f;
            cy2 = 0.5f;
            cz2 = 0.5f;
            if (dy > 0)
            {
                if (int_y < dimension - 1)
                {
                    c2 = cell[int_x, int_y + 1, int_z];
                    if (c2 != null)
                    {
                        occ2 = c2.occupancy;
                        cy2 = c2.occupancy_peak.y;
                        if (cy2 < 0) cy2 = 0.5f;
                    }
                    else occ2 = 0;
                }
            }
            else
            {
                if (int_y > 0)
                {
                    c2 = cell[int_x, int_y - 1, int_z];
                    if (c2 != null)
                    {
                        occ2 = c2.occupancy;
                        cy2 = c2.occupancy_peak.y;
                        if (cy2 < 0) cy2 = 0.5f;
                    }
                    else occ2 = 0;
                }
            }
            if (dy >= 0)
                factor = (cy - dy) / ((1.0f - cy) + cy2);
            else
                factor = -(cy - dy) / ((1.0f - cy2) + cy);
            float y_occ = occ1 + ((occ2 - occ1) * factor);
            
            occ2 = 0;
            cx2 = 0.5f;
            cy2 = 0.5f;
            cz2 = 0.5f;
            if (dz > 0)
            {
                if (int_z < dimension - 1)
                {
                    c2 = cell[int_x, int_y, int_z + 1];
                    if (c2 != null)
                    {
                        occ2 = c2.occupancy;
                        cz2 = c2.occupancy_peak.z;
                        if (cz2 < 0) cz2 = 0.5f;
                    }
                    else occ2 = 0;
                }
            }
            else
            {
                if (int_z > 0)
                {
                    c2 = cell[int_x, int_y, int_z - 1];
                    if (c2 != null)
                    {
                        occ2 = c2.occupancy;
                        cz2 = c2.occupancy_peak.z;
                        if (cz2 < 0) cz2 = 0.5f;
                    }
                    else occ2 = 0;
                }
            }
            if (dz >= 0)
                factor = (cz - dz) / ((1.0f - cz) + cz2);
            else
                factor = -(cz - dz) / ((1.0f - cz2) + cz);
            float z_occ = occ1 + ((occ2 - occ1) * factor);
             

            interpolated = (x_occ + y_occ + z_occ) / 3.0f;
            //interpolated = (x_occ + y_occ) / 2.0f;

            return (interpolated);
        }

        /// <summary>
        /// returns the variance between two colours
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <returns></returns>
        private int getColourVariance(Byte[] col1, Byte[] col2)
        {
            int variance = 0;
            for (int col = 0; col < 3; col++)
            {
                int diff = col1[col] - col2[col];
                variance += (diff*diff);
            }
            return (variance);
        }


        private occupancyGridCell updateGridCellBase(int x, int y, int z, 
                                                     float gridupdate, Byte[] colour,
                                                     occupancyGridCell prev_cell,
                                                     ref float occupied)
        {
            occupancyGridCell c = cell[x, y, z];
            if (c == null)
            {
                c = new occupancyGridCell();
                cell[x, y, z] = c;
                occupations.Add(c);
                occupied++;

                for (int col = 0; col < 3; col++)
                    c.colour[col] = colour[col];
            }
            else
            {                                
                if (gridupdate > 0)
                {
                    //calculate the colour variance
                    c.colour_variance = getColourVariance(c.colour, colour);
                    c.colour_variance_hits++;
                }                

                for (int col = 0; col < 3; col++)
                    c.colour[col] = (Byte)((c.colour[col] + colour[col]) / 2);
            }

            if (c != prev_cell)
            {
                // update the 2D plan view
                if (usePlanView)
                {
                    if (plan_view[x, y] == 0)
                        plan_view[x, y] = gridupdate;
                    else
                        plan_view[x, y] += gridupdate;
                }

                // update the 3D grid
                if (c.occupancy == 0)
                    c.occupancy = gridupdate;
                else
                    c.occupancy += gridupdate;
            }
            return(c);
        }

        /// <summary>
        /// returns the average colour variance
        /// </summary>
        /// <returns></returns>
        public float getAverageColourVariance()
        {
            float tot = 0;
            if (occupations.Count > 0)
            {
                for (int i = 0; i < occupations.Count; i++)
                {
                    occupancyGridCell c = (occupancyGridCell)occupations[i];
                    tot += (c.colour_variance / c.colour_variance_hits);
                }
                tot /= occupations.Count;
            }
            else
            {
                int hits = 0;
                for (int x = 0; x < dimension; x++)
                    for (int y = 0; y < dimension; y++)
                        for (int z = 0; z < dimension; z++)
                            if (cell[x, y, z] != null)
                            {
                                tot += (cell[x, y, z].colour_variance / cell[x, y, z].colour_variance_hits);
                                hits++;
                            }
                if (hits > 0) tot /= hits;
            }
            return (tot);
        }

        /// <summary>
        /// update the given grid cell with the given probability
        /// </summary>
        /// <param name="probability">probability in the range 0-1</param>
        /// <param name="x">x grid cell</param>
        /// <param name="y">y grid cell</param>
        /// <param name="z">z grid cell</param>
        /// <param name="ray_width">ray width in grid cells</param>
        /// <param name="direction">longest axis of the ray</param>
        private void updateGridCell(float probability, int x, int y, int z, 
                                    int ray_width, int longest_axis,
                                    Byte[] colour,
                                    ref float occupied)
        {
            // convert probability to log odds
            if (probability >= 1.0f) probability = 0.999f;
            float gridupdate = 100 * (float)Math.Log(probability / (1.0f - probability));

            occupancyGridCell prev_cell = null;

            // insert updates depending upon the longest length
            switch (longest_axis)
            {
                    case 0:
                        {
                            for (int yy = y - ray_width; yy <= y + ray_width; yy++)
                                if ((yy > -1) && (yy < dimension))
                                    for (int zz = z - ray_width; zz <= z + ray_width; zz++)
                                        if ((zz > -1) && (zz < dimension))
                                            prev_cell = updateGridCellBase(x, yy, zz, gridupdate, colour, prev_cell, ref occupied);
                            break;
                        }
                case 1:
                    {
                        // y axis length of the ray is longest
                        for (int xx = x - ray_width; xx <= x + ray_width; xx++)
                            if ((xx > -1) && (xx < dimension))
                                for (int zz = z - ray_width; zz <= z + ray_width; zz++)
                                    if ((zz > -1) && (zz < dimension))
                                        prev_cell = updateGridCellBase(xx, y, zz, gridupdate, colour, prev_cell, ref occupied);
                        break;
                    }
                case 2:
                    {
                        // z axis length of the ray is longest
                        for (int xx = x - ray_width; xx <= x + ray_width; xx++)
                            if ((xx > -1) && (xx < dimension))
                                for (int yy = y - ray_width; yy <= y + ray_width; yy++)
                                    if ((yy > -1) && (yy < dimension))
                                        prev_cell = updateGridCellBase(xx, yy, z, gridupdate, colour, prev_cell, ref occupied);
                        break;
                    }
            }
        }

        /*
        private float vacancyFunction(float fraction, int steps)
        {
            const float vacancy_constant = 0.2f;
            float prob = 0.5f - (vacancy_constant * fraction * fraction / steps);
            return (prob);
        }
        */

        private float vacancyFunction(float fraction, int steps)
        {
            float min_vacancy_probability = 0.0f;
            float max_vacancy_probability = 0.1f;
            float prob = min_vacancy_probability + ((max_vacancy_probability - min_vacancy_probability) *
                         (float)Math.Exp(-(fraction * fraction)));
            return (0.5f - (prob / steps));
        }

        private float vacancyFunction_accurate(evidenceRay ray, float fraction, int steps)
        {
            float vacancy_multiplier = 0.5f;
            float camera_baseline = 100;
            float focal_length = 0.8f;
            float range = fraction * steps * cellSize_mm;
            float prob = vacancy_multiplier * ((float)SpecialMath.erf(0.5f * ray.disparity / ray.sigma) -
                         (float)SpecialMath.erf(0.5f * ((-2 * camera_baseline * focal_length) + (2 * range * ray.disparity) + ray.disparity) /
                         (ray.sigma * ((2 * range) + 1))));
            return (0.5f - (prob / steps));
        }

        /// <summary>
        /// show the vacancy function within the given image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showVacancyFunction(Byte[] img, int img_width, int img_height)
        {
            // clear the image
            for (int i = 0; i < img_width * img_height * 3; i++)
                img[i] = 0;

            for (int x = 0; x < img_width; x++)
            {
                float fraction = x / (float)img_width;
                float prob = vacancyFunction(fraction, x);
                if (prob < 0) prob = 0;
                if (prob > 1) prob = 1;
                for (int y = 0; y < (prob * img_height); y++)
                {
                    int yy = img_height - 1 - y;
                    int n = ((yy * img_width) + x) * 3;
                    img[n + 1] = (Byte)255;
                }
            }
        }


        /// <summary>
        /// inserts the given ray into the grid
        /// </summary>
        /// <param name="ray"></param>
        public void insert(evidenceRay ray, bool mapping, pos3D origin)
        {
            float xdist_mm, ydist_mm, zdist_mm, x, y, z, occ;
            float prob=0;
            float occupied;
            int rayWidth = 0;
            int widest_point;

            if (!mapping)
                rayWidth = localisationRayWidth;
            else
                rayWidth = (int)(ray.width * mappingRayWidth / (cellSize_mm*2));

            for (int j = 0; j < 2; j++)
            {
                occupied = 0;

                if (j == 0)
                {
                    xdist_mm = ray.vertices[1].x - ray.vertices[0].x;
                    ydist_mm = ray.vertices[1].y - ray.vertices[0].y;
                    zdist_mm = ray.vertices[1].z - ray.vertices[0].z;
                    x = ray.vertices[0].x;
                    y = ray.vertices[0].y;
                    z = ray.vertices[0].z;                    
                }
                else
                {
                    xdist_mm = ray.vertices[0].x - ray.observedFrom.x;
                    ydist_mm = ray.vertices[0].y - ray.observedFrom.y;
                    zdist_mm = ray.vertices[0].z - ray.observedFrom.z;
                    x = ray.observedFrom.x;
                    y = ray.observedFrom.y;
                    z = ray.observedFrom.z;
                }

                // which is the longest axis ?
                int longest_axis = 0;
                float longest = Math.Abs(xdist_mm);
                if (Math.Abs(ydist_mm) > longest)
                {
                    // y has the longest length
                    longest = Math.Abs(ydist_mm);
                    longest_axis = 1;
                }
                if (Math.Abs(zdist_mm) > longest)
                {
                    // z has the longest length
                    longest = Math.Abs(zdist_mm);
                    longest_axis = 2;
                }

                int steps = (int)(longest / cellSize_mm);
                if (j == 0)
                    widest_point = (int)(ray.fattestPoint * steps / ray.length);
                else
                    widest_point = steps;
                float x_incr = xdist_mm / steps;
                float y_incr = ydist_mm / steps;
                float z_incr = zdist_mm / steps;
                if ((interpolationEnabled) && (mapping))
                {
                    /*
                    x_incr2 = x_incr / 3.0f;
                    y_incr2 = y_incr / 3.0f;
                    z_incr2 = z_incr / 3.0f;
                    x_cell_incr = (x_incr2 / cellSize_mm);
                    y_cell_incr = (y_incr2 / cellSize_mm);
                    z_cell_incr = (z_incr2 / cellSize_mm);
                    
                    x_incr3 = x_incr / 6.0f;
                    y_incr3 = y_incr / 6.0f;
                    z_incr3 = z_incr / 6.0f;
                    x_cell_incr2 = (x_incr3 / cellSize_mm);
                    y_cell_incr2 = (y_incr3 / cellSize_mm);
                    z_cell_incr2 = (z_incr3 / cellSize_mm);
                     */
                }
                int i = 0;
                while (i < steps)
                {
                    // calculate the width of the ray at this point
                    // using a diamond shape ray model
                    int ray_wdth = 0;
                    if (i < widest_point)
                        ray_wdth = i * rayWidth / widest_point;
                    else
                        ray_wdth = (steps - i + widest_point) * rayWidth / (steps - widest_point);

                    x += x_incr;
                    y += y_incr;
                    z += z_incr;
                    float x_cell_float = ((x + half_dimension_mm + origin.x) / cellSize_mm);
                    int x_cell = (int)x_cell_float;
                    if ((x_cell >= rayWidth) && (x_cell < dimension - rayWidth))
                    {
                        float y_cell_float = ((y + half_dimension_mm + origin.y) / cellSize_mm);
                        int y_cell = (int)y_cell_float;
                        if ((y_cell >= rayWidth) && (y_cell < dimension - rayWidth))
                        {
                            float z_cell_float = ((z + half_dimension_mm + origin.z) / cellSize_mm);
                            int z_cell = (int)z_cell_float;
                            if ((z_cell >= rayWidth) && (z_cell < dimension - rayWidth))
                            {
                                if (j == 0)
                                {
                                    // probability of the ray at this point
                                    prob = ray.probability(x, y, gaussLookup, forwardBias);
                                    prob = 0.5f + (prob / (2.0f * steps));
                                }

                                int xx_cell = x_cell;
                                int yy_cell = y_cell;
                                int zz_cell = z_cell;

                                // get the cell at this location
                                occupancyGridCell c = cell[xx_cell, yy_cell, zz_cell];

                                // when localising if a cell does not exist here search the local neighbourhood
                                if ((c == null) && (!mapping))
                                {
                                    // search the local vacinity
                                    int searchRadius = 0;
                                    while ((searchRadius < 2) && (c == null))
                                    {
                                        xx_cell = x_cell - searchRadius;
                                        while ((xx_cell < x_cell + searchRadius) && (c == null))
                                        {
                                            if ((xx_cell > -1) && (xx_cell < dimension))
                                            {
                                                yy_cell = y_cell - searchRadius;
                                                while ((yy_cell < y_cell + searchRadius) && (c == null))
                                                {
                                                    if ((yy_cell > -1) && (yy_cell < dimension))
                                                    {
                                                        zz_cell = z_cell;;
                                                        c = cell[xx_cell, yy_cell, zz_cell];
                                                    }
                                                    yy_cell++;
                                                }
                                            }
                                            xx_cell++;
                                        }
                                        searchRadius++;
                                    }
                                }
                                
                                if ((mapping) || ((c != null) && (!mapping)))
                                {
                                    // make a new cell if necessary
                                    /*
                                    if (c == null)
                                    {
                                        c = new occupancyGridCell();
                                        cell[xx_cell, yy_cell, zz_cell] = c;

                                        if (interpolationEnabled)
                                        {
                                            if (xx_cell < dimension - 1)
                                            {
                                                if (cell[xx_cell + 1, yy_cell, zz_cell] == null)
                                                {
                                                    cell[xx_cell + 1, yy_cell, zz_cell] = new occupancyGridCell();
                                                    //cell[xx_cell + 1, yy_cell, zz_cell].setOccupancyPeak(0.1f, 0.5f, 0.5f);
                                                }
                                            }
                                            if (xx_cell > 1)
                                            {
                                                if (cell[xx_cell - 1, yy_cell, zz_cell] == null)
                                                {
                                                    cell[xx_cell - 1, yy_cell, zz_cell] = new occupancyGridCell();
                                                    //cell[xx_cell - 1, yy_cell, zz_cell].setOccupancyPeak(0.9f, 0.5f, 0.5f);
                                                }
                                            }
                                            if (yy_cell < dimension - 1)
                                            {
                                                if (cell[xx_cell, yy_cell + 1, zz_cell] == null)
                                                {
                                                    cell[xx_cell, yy_cell + 1, zz_cell] = new occupancyGridCell();
                                                    //cell[xx_cell, yy_cell + 1, zz_cell].setOccupancyPeak(0.5f, 0.1f, 0.5f);
                                                }
                                            }
                                            if (yy_cell > 1)
                                            {
                                                if (cell[xx_cell, yy_cell - 1, zz_cell] == null)
                                                {
                                                    cell[xx_cell, yy_cell - 1, zz_cell] = new occupancyGridCell();
                                                    //cell[xx_cell, yy_cell - 1, zz_cell].setOccupancyPeak(0.5f, 0.9f, 0.5f);
                                                }
                                            }
                                        }
                                    }
                                    */

                                    // update the occupancy of the cell
                                    if (j == 0)
                                    {
                                        if (mapping)
                                        {
                                            // update the occupancy of this grid cell
                                            updateGridCell(prob, xx_cell, yy_cell, zz_cell, ray_wdth, longest_axis, ray.colour, ref occupied);

                                            c = cell[xx_cell, yy_cell, zz_cell];

                                            // update the peak occupancy position
                                            if (interpolationEnabled)
                                            {
                                                updateOccupancyPeak(x_cell_float, y_cell_float, z_cell_float);
                                                c.setProbability(x_cell_float, y_cell_float, z_cell_float, prob);
                                            }
                                            if (c.occupancy > maxProbability) maxProbability = c.occupancy;
                                        }

                                        if (!mapping)
                                        {
                                            if (!interpolationEnabled)
                                                occ = c.occupancy;
                                            else
                                                occ = interpolatedOccupancy(x_cell_float, y_cell_float, z_cell_float);

                                            // adjust for colour variance
                                            if (occ > 0)
                                            {
                                                //float colour_variance = getColourVariance(c.colour, ray.colour);

                                                // adjust update value based upon the colour variance
                                                //occ *= (1.0f / (1.0f + colour_variance));
                                            }

                                            float prob2 = ((prob * occ) + ((1.0f - prob) * (1.0f - occ)));

                                            /*
                                            float tot_col = 0;
                                            for (int col=0;col<3;col++)
                                            {
                                                int col_diff = c.colour[col] - ray.colour[col];
                                                if (col_diff < 0) col_diff = -col_diff;
                                                tot_col += col_diff;
                                            }
                                            prob2 *= (1.0f - (tot_col / (255.0f*3)));                                                
                                            */

                                            occupied += prob2;
                                        }
                                    }
                                    else
                                    {
                                        if (mapping)
                                        {
                                            //prob = vacancyFunction_accurate(ray, i / (float)steps, steps);
                                            prob = vacancyFunction(i / (float)steps, steps);
                                            updateGridCell(prob, xx_cell, yy_cell, zz_cell, ray_wdth, longest_axis, ray.colour, ref occupied);

                                            if (interpolationEnabled)
                                            {
                                                c = cell[xx_cell, yy_cell, zz_cell];

                                                updateOccupancyPeak(x_cell_float, y_cell_float, z_cell_float);
                                                c.setProbability(x_cell_float, y_cell_float, z_cell_float, -1);
                                                //c.setProbability(x_cell_float - x_cell_incr, y_cell_float - y_cell_incr, z_cell_float - z_cell_incr, prob2);
                                                //c.setProbability(x_cell_float + x_cell_incr, y_cell_float + y_cell_incr, z_cell_float + z_cell_incr, prob3);
                                            }
                                        }
                                    }
                                }


                            }
                            else i = steps;
                        }
                        else i = steps;
                    }
                    else i = steps;
                    i++;
                }

                // break out!
                if (!mapping)
                {
                    matchedCells += occupied;
                    break;
                }

                occupiedCells += (int)occupied;
            }

        }

        public void show(Byte[] img, int img_width, int img_height)
        {
            /*
            if (!interpolationEnabled)
                showStandard(img, img_width, img_height);
            else
                showInterpolated(img, img_width, img_height);
             */

            //showPlanView(img, img_width, img_height);
            showStandard2(img, img_width, img_height);
        }

        /// <summary>
        /// show an above view of the grid within the given bitmsp
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showStandard(Byte[] img, int img_width, int img_height)
        {
            for (int y_cell = 0; y_cell < dimension; y_cell++)
            {
                int y = y_cell * img_height / dimension;
                for (int x_cell = 0; x_cell < dimension; x_cell++)
                {
                    int x = x_cell * img_width / dimension;
                    float occ = 0;
                    for (int z_cell = 0; z_cell < dimension; z_cell++)
                    {
                        occupancyGridCell c = cell[x_cell, y_cell, z_cell];
                        if (c != null)
                        {
                            if (c.occupancy > 0) occ += c.occupancy;
                        }
                    }
                    //if (occ == -9999) occ = 0;

                    bool vacant = false;
                    if (occ < 0)
                    {
                        occ = -occ;
                        vacant = true;
                    }
                    //Byte intensity = (Byte)(occ * 255 / maxProbability);
                    int intens = (int)(occ * 255 / 5);
                    if (intens > 255) intens = 255;
                    Byte intensity = (Byte)intens;

                    int x2 = (x_cell+1) * img_width / dimension;
                    int y2 = (y_cell+1) * img_height / dimension;

                    for (int xx = x; xx < x2; xx++)
                    {
                        if (xx < img_width)
                        {
                            for (int yy = y; yy < y2; yy++)
                            {
                                if (yy < img_height)
                                {
                                    int n = ((yy * img_width) + xx) * 3;
                                    if (!vacant)
                                    {
                                        for (int col = 0; col < 3; col++)
                                        {
                                            img[n + 2 - col] = intensity;
                                        }
                                    }
                                    else
                                    {
                                        img[n] = intensity;
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        /// <summary>
        /// show a 2D plan view of the grid
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img_width"></param>
        /// <param name="img_height"></param>
        public void showPlanView(Byte[] img, int img_width, int img_height)
        {
            float average_confidence = 0;
            int hits = 0;
            for (int y_cell = 0; y_cell < dimension; y_cell++)
            {
                for (int x_cell = 0; x_cell < dimension; x_cell++)
                {
                    if (plan_view[x_cell, y_cell] != 0)
                    {
                        average_confidence += plan_view[x_cell, y_cell];
                        hits++;
                    }
                }
            }
            if (hits > 0) average_confidence /= hits;

            for (int y_cell = 0; y_cell < dimension; y_cell++)
            {
                int y = y_cell * img_height / dimension;
                for (int x_cell = 0; x_cell < dimension; x_cell++)
                {
                    int x = x_cell * img_width / dimension;
                    bool unseen = false;
                    if (plan_view[x_cell, y_cell] == 0) unseen = true;
                    float occ = plan_view[x_cell, y_cell] - average_confidence;

                    bool vacant = false;
                    if (occ < 0)
                    {
                        occ = -occ;
                        vacant = true;
                    }
                    //Byte intensity = (Byte)(occ * 255 / maxProbability);
                    int intens = (int)(occ * 255 / 5);
                    if (intens > 255) intens = 255;
                    Byte intensity = (Byte)intens;
                    if (unseen) intensity = 0;

                    int x2 = (x_cell + 1) * img_width / dimension;
                    int y2 = (y_cell + 1) * img_height / dimension;

                    for (int xx = x; xx < x2; xx++)
                    {
                        if (xx < img_width)
                        {
                            for (int yy = y; yy < y2; yy++)
                            {
                                if (yy < img_height)
                                {
                                    int n = ((yy * img_width) + xx) * 3;
                                        if (!vacant)
                                        {
                                            for (int col = 0; col < 3; col++)
                                            {
                                                img[n + 2 - col] = intensity;
                                            }
                                        }
                                        else
                                        {
                                            img[n] = intensity;
                                        }
                                }
                            }
                        }
                    }

                }
            }
        }


        public void showStandard2(Byte[] img, int img_width, int img_height)
        {
            for (int i = 0; i < img_width * img_height * 3; i++)
                img[i] = 0;

            float average_confidence = 0;
            int hits = 0;
            for (int y_cell = 0; y_cell < dimension; y_cell++)
            {
                for (int x_cell = 0; x_cell < dimension; x_cell++)
                {
                    if (plan_view[x_cell, y_cell] != 0)
                    {
                        average_confidence += plan_view[x_cell, y_cell];
                        hits++;
                    }
                }
            }
            if (hits > 0) average_confidence /= hits;
            //average_confidence *= 150 / 100;

            //float averageColourVariance = getAverageColourVariance();

            for (int y_cell = 0; y_cell < dimension; y_cell++)
            {
                int y = y_cell * img_height / dimension;
                for (int x_cell = 0; x_cell < dimension; x_cell++)
                {
                    int x = x_cell * img_width / dimension;
                    bool unseen = false;
                    if (plan_view[x_cell, y_cell] == 0) unseen = true;
                    float occ = plan_view[x_cell, y_cell] - average_confidence;

                    bool vacant = false;
                    if (occ < 0)
                    {
                        occ = 0;
                        vacant = true;
                    }
                    //Byte intensity = (Byte)(occ * 255 / maxProbability);
                    int intens = (int)(occ * 255 / 5);
                    if (intens > 255) intens = 255;
                    Byte intensity = (Byte)intens;
                    if (unseen) intensity = 0;

                    int r=0, g=0, b=0;
                    if (intensity > 0)
                    {
                        for (int z_cell = 0; z_cell < dimension; z_cell++)
                        {
                            occupancyGridCell c = cell[x_cell, y_cell, z_cell];
                            if (c != null)
                            {
                                if ((c.colour[0] > 10) && (c.colour[1] > 10) && (c.colour[2] > 10))
                                {
                                    r = c.colour[0];
                                    g = c.colour[1];
                                    b = c.colour[2];
                                }
                            }
                        }
                    }

                    int x2 = (x_cell + 1) * img_width / dimension;
                    int y2 = (y_cell + 1) * img_height / dimension;

                    for (int xx = x; xx < x2; xx++)
                    {
                        if (xx < img_width)
                        {
                            for (int yy = y; yy < y2; yy++)
                            {
                                if (yy < img_height)
                                {
                                    int n = ((yy * img_width) + xx) * 3;
                                    if (!vacant)
                                    {
                                        img[n] = (byte)b;
                                        img[n + 1] = (byte)g;
                                        img[n + 2] = (byte)r;
                                        /*
                                        for (int col = 0; col < 3; col++)
                                        {
                                            img[n + 2 - col] = intensity;
                                        }
                                         */
                                    }
                                    else
                                    {
                                        img[n] = intensity;
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }


        public void showPointCloud(Byte[] img, int img_width, int img_height)
        {
            for (int i = 0; i < img_width * img_height * 3; i++) img[i] = 0;

            for (int y_cell = 0; y_cell < dimension; y_cell++)
            {
                int y = y_cell * img_height / dimension;
                for (int x_cell = 0; x_cell < dimension; x_cell++)
                {
                    int x = x_cell * img_width / dimension;
                    for (int z_cell = 0; z_cell < dimension; z_cell++)
                    {
                        occupancyGridCell c = cell[x_cell, y_cell, z_cell];
                        if (c != null)
                        {
                            if (c.particles != null)
                            {
                                for (int p = 0; p < c.particles.Count; p++)
                                {
                                    occupancyGridParticle part = (occupancyGridParticle)c.particles[p];
                                    if (part.probability > 0)
                                    {
                                        int intens = (int)(part.probability * 255 / 30.0f * c.particles.Count);
                                        if (intens > 255) intens = 255;
                                        Byte intensity = (Byte)intens;

                                        int x2 = (int)(part.x * img_width / dimension);
                                        int y2 = (int)(part.y * img_height / dimension);

                                        for (int xx = x2; xx < x2; xx++)
                                        {
                                            if (xx < img_width)
                                            {
                                                for (int yy = y2; yy < y2; yy++)
                                                {
                                                    if (yy < img_height)
                                                    {
                                                        int n = ((yy * img_width) + xx) * 3;
                                                        for (int col = 0; col < 3; col++) 
                                                            img[n + col] = intensity;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }

                }
            }
        }


        public void showInterpolated(Byte[] img, int img_width, int img_height)
        {
            float incr = 0.2f;
            int rr=0, gg=0, bb=0;

            for (float y_cell = 0; y_cell < dimension; y_cell+=incr)
            {
                int y = (int)(y_cell * img_height / dimension);
                for (float x_cell = 0; x_cell < dimension; x_cell+=incr)
                {
                    int x = (int)(x_cell * img_width / dimension);
                    float occ = 0;
                    for (float z_cell = 0; z_cell < dimension; z_cell+=incr)
                    {
                        occupancyGridCell c = cell[(int)x_cell, (int)y_cell, (int)z_cell];
                        if (c != null)
                        {
                            if (c.occupancy > 0)
                            {
                                float occ2 = interpolatedOccupancy(x_cell, y_cell, z_cell);
                                occ += (occ2 * occ2 * occ2);
                                //occ += (interpolatedOccupancy(x_cell, y_cell, z_cell) * c.particles.Count * c.particles.Count);
                                //occ += c.particles.Count * c.particles.Count;
                                rr = c.colour[0];
                                gg = c.colour[1];
                                bb = c.colour[2];
                            }
                        }
                    }
                    //if (occ == -9999) occ = 0;

                    bool vacant = false;
                    if (occ < 0)
                    {
                        occ = -occ;
                        vacant = true;
                    }
                    //Byte intensity = (Byte)(occ * 255 / maxProbability);
                    int intens = (int)(occ * 255 / 80);
                    if (intens > 255) intens = 255;
                    Byte intensity = (Byte)intens;

                    int x2 = (int)((x_cell + incr) * img_width / dimension);
                    int y2 = (int)((y_cell + incr) * img_height / dimension);

                    for (int xx = x; xx < x2; xx++)
                    {
                        if (xx < img_width)
                        {
                            for (int yy = y; yy < y2; yy++)
                            {
                                if (yy < img_height)
                                {
                                    int n = ((yy * img_width) + xx) * 3;
                                    if (!vacant)
                                    {
                                        img[n + 2 - 0] = (Byte)(intensity * rr / 255);
                                        img[n + 2 - 1] = (Byte)(intensity * gg / 255);
                                        img[n + 2 - 2] = (Byte)(intensity * bb / 255);

                                        for (int col = 0; col < 3; col++)
                                        {
                                            //img[n + 2 - col] = intensity;
                                        }
                                    }
                                    else
                                    {
                                        img[n] = intensity;
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

    }

}
