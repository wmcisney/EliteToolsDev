using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.AWS {
	[TestClass]
	public class TestDynamoDbSettings {
		[TestMethod]
		public void GetDatabaseSetting() {
			{
				var found = SettingsAccessor.GetProductionSetting(SettingsAccessor.SettingsKey.DatabaseUpdate("test-notfound",""));
				Assert.IsNull(found);
			}
			var key = SettingsAccessor.SettingsKey.DatabaseUpdate("test-found","");
			{
				var value = "test-value";
				SettingsAccessor.SetProductionSetting(key, value);
				var found = SettingsAccessor.GetProductionSetting(key);
				Assert.IsNotNull(found);
				Assert.AreEqual(value, found);
			}
			{			
				var value = "test-update";
				SettingsAccessor.SetProductionSetting(key, value);
				var found = SettingsAccessor.GetProductionSetting(key);
				Assert.IsNotNull(found);
				Assert.AreEqual(value, found);
			}

		}
	}
}
