/*
    Unit tests for parallel for
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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace sentience.core.tests
{	
	[TestFixture()]
	public class tests_parallel
	{
		[Test()]
		public void parallelForTest()
		{
		    DateTime loop_start_time = DateTime.Now;
		    int itterations = 10;
		    int time_per_itteration_mS = 10;
		    int total = 0;
		    Parallel.For(0, itterations, delegate(int i)
		    {
		        DateTime start_time = DateTime.Now;
		        int time_elapsed_mS = 0;
		        while (time_elapsed_mS < time_per_itteration_mS)
		        {
		            TimeSpan diff = DateTime.Now.Subtract(start_time);
		            time_elapsed_mS = (int)diff.TotalMilliseconds;
		        }
		        total++;
		    });
		    TimeSpan diff2 = DateTime.Now.Subtract(loop_start_time);

		    Assert.AreEqual(10, total);
		    
		    if (System.Environment.ProcessorCount > 1)
		    {
		        Console.WriteLine("Serial execution: " + (itterations * time_per_itteration_mS).ToString());
		        Console.WriteLine("Parallel execution: " + diff2.TotalMilliseconds.ToString());
		        
		        // did the parallel version take less time than serial execution ?
		        Assert.Less(diff2.TotalMilliseconds, itterations * time_per_itteration_mS);
		    }		    
		}
	}
}
