using System;

namespace CenterSpace.Free
{
	/// <summary>
	/// Simple example using the MersenneTwister class.
	/// </summary>
	class MersenneTwisterExample
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
		  MersenneTwister randGen = new MersenneTwister();
          Console.WriteLine( "100 uniform random integers in [0,{0}]:", MersenneTwister.MaxRandomInt );
          int i;
          for ( i = 0; i < 100; ++i )
          {
            Console.Write( "{0} ", randGen.Next() );
            if ( i%5 == 4 ) Console.WriteLine("");
          }

          Console.WriteLine( "100 uniform random doubles in [0,1):" );
          for ( i = 0; i < 100; ++i )
          {
            Console.Write( "{0} ", randGen.NextDouble().ToString("F8") );
            if ( i%5 == 4 ) Console.WriteLine("");
          }
		}
	}
}
