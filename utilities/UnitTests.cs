

using System;
using NUnit.Framework;

namespace sluggish.utilities
{	
	[TestFixture()]
	public class UnitTests_fraction
	{		
		[Test()]
		public void Multiply()
		{
		    fraction f1 = new fraction(4,5);
		    fraction f2 = new fraction(7,3);
		    fraction result = f1 * f2;
		    Assert.AreEqual(4*7, result.numerator);
		    Assert.AreEqual(5*3, result.denominator);
		}
		
		[Test()]
		public void Divide()
		{
		    fraction f1 = new fraction(4,5);
		    fraction f2 = new fraction(3,2);
		    fraction result = f1 / f2;
		    Assert.AreEqual(8, result.numerator);
		    Assert.AreEqual(15, result.denominator);
		}

  	    [Test()]
		public void Add()
		{
		    fraction f1 = new fraction(4,5);
		    fraction f2 = new fraction(3,5);
		    fraction result = f1 + f2;
		    result.Reduce();
		    Assert.AreEqual(7, result.numerator);
		    Assert.AreEqual(5, result.denominator);
		}

  	    [Test()]
		public void Subtract()
		{
		    fraction f1 = new fraction(7,5);
		    fraction f2 = new fraction(3,5);
		    fraction result = f1 - f2;
		    result.Reduce();
		    Console.WriteLine("numerator " + result.numerator.ToString());
		    Console.WriteLine("denominator " + result.denominator.ToString());
		    Assert.AreEqual(4, result.numerator);
		    Assert.AreEqual(5, result.denominator);
		}

    }
}
