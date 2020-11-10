using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Askables;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.Hooks.Realtime.Dashboard {
	public class RealTime_Dashboard_Rock : IRockHook {

		public bool AbsorbErrors() {
			return false;
		}
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//noop
		}

		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {

			//s.QueryOver<

			//var recurrenceId = rock.RecurrenceId;
			//RealTimeHelpers.DoRecurrenceUpdate(s, recurrenceId, () =>
			//	 new AngularUpdate() {
			//			new AngularTileId<IEnumerable<AngularRock>>(0, recurrenceId, null,AngularTileKeys.L10RocksList(recurrenceId)) {
			//				Contents = AngularList.CreateFrom(AngularListType.Add, new AngularRock(rock))
			//			}
			//	 }
			//);
			//noop
		}

		public async Task UnArchiveRock(ISession s, RockModel rock, bool v) {
			//noop
		}

		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			//noop
		}
		public async Task UndeleteRock(ISession s, RockModel rock) {
			//Nothing
		}
	}
}