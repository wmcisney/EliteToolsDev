using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.Dashboard {
	public class RealTime_Dashboard_Headline : IHeadlineHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ArchiveHeadline(ISession s, PeopleHeadline headline) {
			//noop
		}
		public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline) {
			//noop
		}

		public async Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline) {
			var recurrenceId = headline.RecurrenceId;
			RealTimeHelpers.DoRecurrenceUpdate(s, recurrenceId, () =>
				 new AngularUpdate() {
						new AngularTileId<IEnumerable<AngularHeadline>>(0, recurrenceId, null,AngularTileKeys.L10HeadlineList(recurrenceId)) {
							Contents = AngularList.CreateFrom(AngularListType.Add, new AngularHeadline(headline))
						}
				 }
			);
		}


		public async Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates) {
			//handled in other headline.

			//var recurrenceId = headline.RecurrenceId;
			//RealTimeHelpers.DoRecurrenceUpdate(s, recurrenceId, () =>
			//	 new AngularUpdate() {
			//		 new AngularHeadline(headline)
			//	 }
			//);
		}
	}
}