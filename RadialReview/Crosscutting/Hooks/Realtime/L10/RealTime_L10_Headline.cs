using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
	public class RealTime_L10_Headline : IHeadlineHook {
		public bool AbsorbErrors() {
			return false;
		}
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline) {
			var recurrenceId = headline.RecurrenceId;
			if (recurrenceId > 0) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var meetingHub = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

				if (headline.CreatedDuringMeetingId == null) {
					headline.CreatedDuringMeetingId = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.CreateAdmin(s), recurrenceId, true, false, false).NotNull(x => (long?)x.Id);
				}
				var aHeadline = new AngularHeadline(headline);
				meetingHub.appendHeadline(".headlines-list", headline.ToRow());
				meetingHub.showAlert("Created people headline.", 1500);
				var updates = new AngularRecurrence(recurrenceId);
				updates.Headlines = AngularList.CreateFrom(AngularListType.Add, aHeadline);
				meetingHub.update(updates);
			}
		}

		public async Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var group = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(headline.RecurrenceId), RealTimeHelpers.GetConnectionString());

			if (updates.MessageChanged) {
				group.updateHeadlineMessage(headline.Id, headline.Message);

				AngularPicture about = null;
				try {
					if (headline.About != null) {
						about = new AngularPicture(headline.About);
					}
				} catch (Exception) {
				}

				group.update(new AngularUpdate() {
					new AngularHeadline(headline.Id) {
						Name = headline.Message,
						About = headline.About.NotNull(x=>new AngularPicture(x))
					}
				});
			}

		}

		public async Task ArchiveHeadline(ISession s, PeopleHeadline headline) {
			using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(headline.RecurrenceId).Update(
				  new AngularRecurrence(headline.RecurrenceId) {
					  Headlines = AngularList.CreateFrom(AngularListType.Remove, new AngularHeadline(headline.Id))
				  }
			  );
			}
		}
		public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline) {
			using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(headline.RecurrenceId).Update(
				  new AngularRecurrence(headline.RecurrenceId) {
					  Headlines = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularHeadline(headline))
				  }
			  );
			}
		}
	}
}
