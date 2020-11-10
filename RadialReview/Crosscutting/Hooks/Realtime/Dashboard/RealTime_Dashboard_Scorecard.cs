using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.Dashboard {
	public class RealTime_Dashboard_Scorecard : IMeasurableHook, IMeetingMeasurableHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}


		private void AddRemoveMeas(long userId, MeasurableModel meas, AngularListType type, List<ScoreModel> scores = null) {

			RealTimeHelpers.GetUserHubForRecurrence(userId).update(new AngularUpdate() {
					new AngularScorecard(-1) {
						Measurables = meas.NotNull(y=>AngularList.CreateFrom(type, new AngularMeasurable(y))),
						Scores = scores.NotNull(y=> AngularList.Create(type, y.Select(x=>new AngularScore(x,null,true))))
					}
			});
		}


		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> createdScores) {
			//add
			AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.ReplaceIfNewer, createdScores);
			AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.ReplaceIfNewer, createdScores);
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//remove
			AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.Remove);
			AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.Remove);
		}

		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (updates.AccountableUserChanged) {
				AddRemoveMeas(updates.OriginalAccountableUserId, measurable, AngularListType.Remove);
				AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.ReplaceIfNewer);
			}
			if (updates.AdminUserChanged) {
				AddRemoveMeas(updates.OriginalAdminUserId, measurable, AngularListType.Remove);
				AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.ReplaceIfNewer);
			}

			if (updates.GoalDirectionChanged || updates.GoalChanged) {
				AddRemoveMeas(measurable.AccountableUserId, null, AngularListType.ReplaceIfExists, updatedScores);
				AddRemoveMeas(measurable.AdminUserId, null, AngularListType.ReplaceIfExists, updatedScores);
			}

		}

		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {

			//Noop
			//var recurrenceId = recurMeasurable.L10Recurrence.Id;
			//RealTimeHelpers.DoRecurrenceUpdate(s, recurMeasurable.L10Recurrence.Id, () =>
			//		new AngularUpdate() {
			//			new AngularTileId<AngularScorecard>(0, recurrenceId, null,AngularTileKeys.L10TodoList(recurrenceId)) {
			//				Contents = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo))
			//			}
			//		}
			//);
		}

		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			//throw new System.NotImplementedException();
		}
	}
}