using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using RadialReview;

namespace TractionTools.Tests.Utilities.Extensions {
	[TestClass]
	public class Math2Tests {
		[TestMethod]
		public void TestArgMaxMin() {

			var items = new List<Tuple<int, double>>() {
				new Tuple<int, double>(3,4.0),
				new Tuple<int, double>(1,2.0),
				new Tuple<int, double>(2,1.0),
				new Tuple<int, double>(4,3.0),
			};

			{
				var found = items.ArgMax(x => x.Item1);
				Assert.AreEqual(4, found.Item1);
				Assert.AreEqual(3.0, found.Item2);
			}{
				var found = items.ArgMax(x => x.Item2);
				Assert.AreEqual(3, found.Item1);
				Assert.AreEqual(4.0, found.Item2);
			}{
				var found = items.ArgMin(x => x.Item1);
				Assert.AreEqual(1, found.Item1);
				Assert.AreEqual(2.0, found.Item2);
			}{
				var found = items.ArgMin(x => x.Item2);
				Assert.AreEqual(2, found.Item1);
				Assert.AreEqual(1.0, found.Item2);
			}


		}
	}
}
