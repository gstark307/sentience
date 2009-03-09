/*
    Unit tests for occupancy grids
    Copyright (C) 2009 Bob Mottram
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
using NUnit.Framework;
using sentience.core;

namespace sentience.core.tests
{
	[TestFixture()]
	public class tests_occupancygrid_simple
	{
		[Test()]
		public void InsertRays()
		{
		    int dimension_cells = 50;
		    int dimension_cells_vertical = 30;
		    int cellSize_mm = 50;
		    int localisationRadius_mm = 1000;
		    int maxMappingRange_mm = 5000;
		    float vacancyWeighting = 0;
		    
		    occupancygridSimple grid = 
		        new occupancygridSimple(
		            dimension_cells,
		            dimension_cells_vertical,
		            cellSize_mm,
		            localisationRadius_mm,
		            maxMappingRange_mm,
		            vacancyWeighting);
		    
		    Assert.AreNotEqual(grid, null, "object occupancygridSimple was not created");
		    
		    robot observer = new robot(1, robot.MAPPING_SIMPLE);
		    Assert.AreNotEqual(observer, null, "robot object was not created");
		
		    
		}	
	
		[Test()]
		public void GridCreation()
		{
		    int dimension_cells = 50;
		    int dimension_cells_vertical = 30;
		    int cellSize_mm = 50;
		    int localisationRadius_mm = 1000;
		    int maxMappingRange_mm = 5000;
		    float vacancyWeighting = 0;
		    
		    occupancygridSimple grid = 
		        new occupancygridSimple(
		            dimension_cells,
		            dimension_cells_vertical,
		            cellSize_mm,
		            localisationRadius_mm,
		            maxMappingRange_mm,
		            vacancyWeighting);
		    
		    Assert.AreNotEqual(grid, null, "object occupancygridSimple was not created");
		
		}
	}
	
	[TestFixture()]
	public class tests_occupancygrid_multi_hypothesis
	{		
		[Test()]
		public void GridCreation()
		{
		    int dimension_cells = 50;
		    int dimension_cells_vertical = 30;
		    int cellSize_mm = 50;
		    int localisationRadius_mm = 1000;
		    int maxMappingRange_mm = 5000;
		    float vacancyWeighting = 0;
		    
		    occupancygridMultiHypothesis grid = 
		        new occupancygridMultiHypothesis(dimension_cells,
		                                         dimension_cells_vertical,
		                                         cellSize_mm,
		                                         localisationRadius_mm,
		                                         maxMappingRange_mm,
		                                         vacancyWeighting);
		    
		    Assert.AreNotEqual(grid, null, "object occupancygridMultiHypothesis was not created");
		}
	}
}
