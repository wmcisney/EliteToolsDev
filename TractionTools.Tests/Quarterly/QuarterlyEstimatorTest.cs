using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Quarterly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Accessors.QuarterlyAccessor;

namespace TractionTools.Tests.Quarterly {
	[TestClass]
	public class QuarterlyEstimatorTest {

		private string QtrAsStr(YearQuarter x) {
			return "Q" + x.Quarter + " " + x.Year;
		}
		[TestMethod]
		public void TestQuarterlyGenerator() {


			{
				//First quarter, no existing
				var qtrs = new List<QuarterModel>();
				var now = new DateTime(2018, 2, 1);
				var found = QuarterlyAccessor._GetCurrentQuarter(qtrs, 1, now, out bool generated);
				Assert.IsTrue(generated);
				Assert.AreEqual("Q1 2018", found.Name);
				Assert.AreEqual(new DateTime(2018, 1, 1), found.StartDate);
				Assert.AreEqual(null, found.EndDate);
			}
			{
				//Second quarter, no existing
				var qtrs = new List<QuarterModel>();
				var now = new DateTime(2018, 4, 1);
				var found = QuarterlyAccessor._GetCurrentQuarter(qtrs, 1, now, out bool generated);
				Assert.IsTrue(generated);
				Assert.AreEqual("Q2 2018", found.Name);
				Assert.AreEqual(new DateTime(2018, 4, 1), found.StartDate);
				Assert.AreEqual(null, found.EndDate);
			}

			{
				//Second quarter, near existing
				var qtrs = new List<QuarterModel>() {
					new QuarterModel(){
						EndDate = new DateTime(2018,1,15),
						Year = 2018,
						Quarter = 1
					}
				};
				var now = new DateTime(2018, 5, 1);
				var found = QuarterlyAccessor._GetCurrentQuarter(qtrs, 1, now, out bool generated);
				Assert.IsTrue(generated);
				Assert.AreEqual("Q3 2018", found.Name);
				Assert.AreEqual(new DateTime(2018, 4, 15), found.StartDate);
				Assert.AreEqual(null, found.EndDate);
			}
			{
				//Second quarter, far existing
				var qtrs = new List<QuarterModel>() {
					new QuarterModel(){
						EndDate = new DateTime(2018,1,15),
						Year = 2018,
						Quarter = 1
					}
				};
				var now = new DateTime(2018, 8, 1);
				var found = QuarterlyAccessor._GetCurrentQuarter(qtrs, 1, now, out bool generated);
				Assert.IsTrue(generated);
				Assert.AreEqual("Q4 2018", found.Name);
				Assert.AreEqual(new DateTime(2018, 7, 15), found.StartDate);
				Assert.AreEqual(null, found.EndDate);
			}
			{
				//Found
				var qtrs = new List<QuarterModel>() {
					new QuarterModel(){
						StartDate = new DateTime(2017,10,15),
						EndDate = new DateTime(2018,1,15),
						Year = 2018,
						Quarter = 1,
						Name = "Q1 2018!"
					}
				};
				var now = new DateTime(2018, 1, 1);
				var found = QuarterlyAccessor._GetCurrentQuarter(qtrs, 1, now, out bool generated);
				Assert.IsFalse(generated);
				Assert.AreEqual("Q1 2018!", found.Name);
				Assert.AreEqual(new DateTime(2017, 10, 15), found.StartDate);
				Assert.AreEqual(new DateTime(2018,1,15), found.EndDate);
			}


		}

		[TestMethod]
		public void TestQuarterlyEstimator(){
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 1, 1), new DateTime(2018, 1, 1))));

			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 2, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 2, 1), new DateTime(2018, 2, 1))));

			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 3, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 3, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 3, 1), new DateTime(2018, 3, 1))));

			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 4, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 4, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 4, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 4, 1), new DateTime(2018, 4, 1))));

			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 5, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 5, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 5, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 5, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 5, 1), new DateTime(2018, 5, 1))));

			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 6, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 6, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 6, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 6, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 6, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 6, 1), new DateTime(2018, 6, 1))));

			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q1 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 7, 1), new DateTime(2018, 7, 1))));

			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 8, 1), new DateTime(2018, 8, 1))));

			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 9, 1), new DateTime(2018, 9, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q2 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 10, 1), new DateTime(2018, 10, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 11, 1), new DateTime(2018, 11, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 1, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2018, 12, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 2, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q3 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 1, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 3, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q1 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 2, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 4, 1))));
			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 3, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2018", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 5, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 4, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 6, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q2 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 5, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 6, 1), new DateTime(2018, 7, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 6, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 6, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 6, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 6, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 6, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 7, 1), new DateTime(2018, 8, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 7, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 7, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 7, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 7, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 8, 1), new DateTime(2018, 9, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 8, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 8, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q3 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 8, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 9, 1), new DateTime(2018, 10, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 9, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 9, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 10, 1), new DateTime(2018, 11, 1))));
			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 10, 1), new DateTime(2018, 12, 1))));

			Assert.AreEqual("Q4 2019", QtrAsStr(QuarterlyAccessor.EstimateQuarter(new DateTime(2019, 11, 1), new DateTime(2018, 12, 1))));





		}
	}
}
