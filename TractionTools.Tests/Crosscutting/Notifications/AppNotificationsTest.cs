using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using System.Threading.Tasks;
using RadialReview.Models.Notifications;

namespace TractionTools.Tests.Notifications {
	[TestClass]
	public class AppNotificationsTest {
		[TestMethod]
		public async Task SendIOS() {

			var b = NotifcationCreation.Build("iostest",0, "Test Heading",NotificationDevices.Phone, "Test body", sensitive: true);

			await NotifcationCreation.SendToDevice(new RadialReview.Models.Notifications.UserDevice() {
				DeviceType = "ios",
				DeviceId = "//REPLACE_ME",

			}, b);
			Console.WriteLine("here");
		}
	}
}
