using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using System;
using System.Dynamic;

namespace RadialReview.Models.UserModels {
	public class HangfireCaller {
		public HangfireCaller() {

		}
		public HangfireCaller(UserOrganizationModel caller) {
			UserOrganizationId = caller.Id;
			TimezoneOffset = caller.GetTimezoneOffset();
			ConnectionId = caller.GetConnectionId();
		}

		public long UserOrganizationId { get; set; }
		public int TimezoneOffset { get; set; }
		public string ConnectionId { get; set; }
		public DateTime GetCallerLocalTime() {
			try {
				return DateTime.UtcNow.AddMinutes(TimezoneOffset);
			} catch (ArgumentOutOfRangeException) {
				if (TimezoneOffset > 0) {
					return DateTime.MaxValue;
				}
				return DateTime.MinValue;
			}
		}

		private dynamic Hub { get; set; }

		public dynamic GetCallerHub() {
			if (Hub == null) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				if (ConnectionId != null) {
					Hub = hub.Clients.Client(ConnectionId);
				} else if (UserOrganizationId != null) {
					Hub = hub.Clients.Group(RealTimeHub.Keys.UserId(UserOrganizationId));
				} else {
					Hub = (dynamic)new ExpandoObject();
				}
			}
			return Hub;
		}
	}
}