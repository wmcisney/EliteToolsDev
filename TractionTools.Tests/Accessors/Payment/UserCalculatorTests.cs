using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Models.Payments;
using System.Collections.Generic;

namespace TractionTools.Tests.Accessors.Payment {
	[TestClass]
	public class UserCalculatorTests {

#if false
		[TestMethod]
		public void TestUserCalculator() {

			var events = new List<UserProductEvent>() {
				new UserProductEvent(1,0, new DateTime(2018,1,1),UserProductEventType.Activate,ProductType.L10),
				new UserProductEvent(1,0, new DateTime(2018,1,1),UserProductEventType.Activate,ProductType.People),
				new UserProductEvent(2,0, new DateTime(2018,1,1),UserProductEventType.Activate,ProductType.L10), //Ignore, for user 2
				new UserProductEvent(1,0, new DateTime(2018,5,1),UserProductEventType.Deactivate,ProductType.People),
				new UserProductEvent(1,0, new DateTime(2018,7,1),UserProductEventType.Deactivate,ProductType.L10),
				new UserProductEvent(1,0, new DateTime(2019,2,1),UserProductEventType.Activate,ProductType.L10),
			};


			{
				//Two year range
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2017, 1, 1), new DateTime(2019, 1, 1));
				Assert.AreEqual(181.0 / 730, p);
			}

			{
				//First Month
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2018, 1, 1), new DateTime(2018, 2, 1));
				Assert.AreEqual(1.0, p);
			}

			{
				//3 days, 2 in range
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2017, 12, 31), new DateTime(2018, 1, 3));
				Assert.AreEqual(2.0 / 3.0, p);
			}


			{
				//End range overlap last event
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2019, 1, 1), new DateTime(2019, 3, 1));
				Assert.AreEqual(28.0 / 59.0, p);
			}


			{
				//After range
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2019, 3, 1), new DateTime(2019, 4, 1));
				Assert.AreEqual(1.0, p);
			}

			{
				//After range (People Tools)
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.People, new DateTime(2019, 3, 1), new DateTime(2019, 4, 1));
				Assert.AreEqual(0.0, p);
			}

			{
				//After range overlap last event (People Tools)
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.People, new DateTime(2018, 4, 1), new DateTime(2018, 6, 1));
				Assert.AreEqual(30.0/61.0, p);
			}

			{
				//Three year range
				var activeDates = 181.0 + 334.0;
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2017, 1, 1), new DateTime(2020, 1, 1));
				Assert.AreEqual(activeDates / 1095.0, p);
			}

			{
				//Inactive range
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2018, 8, 1), new DateTime(2018, 9, 1));
				Assert.AreEqual(0.0, p);
			}

			{
				//interrange
				var p = UserCalculator.PercentageChargeable(events, 1, ProductType.L10, new DateTime(2018, 6, 1), new DateTime(2018, 8, 1));
				Assert.AreEqual(30.0/61.0, p);
			}


		}
#endif
	}
}
