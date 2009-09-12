
using System;
using NUnit.Framework;
using dpslam.core;

namespace dpslam.core.tests
{
	[TestFixture()]
	public class motionmodel_tests
	{
		
		[Test()]
		public void Create()
		{
			robot rob = new robot(1);
			motionModel mm = new motionModel(rob, rob.LocalGrid, 1);
			
			
		}
	}
}
