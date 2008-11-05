// occupancygrid.cs created with MonoDevelop
// User: motters at 19:50 05/11/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using NUnit.Framework;
using sentience.core;

namespace sentience.core.tests
{
	[TestFixture()]
	public class tests_occupancygrid
	{
		
		[Test()]
		public void TestGridCreation()
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
