﻿using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.L10;
using RadialReview.Crosscutting.Hooks.Realtime;
using RadialReview.Models;

namespace RadialReview.Crosscutting.Hooks.Meeting {
	public class CalculateCumulative : IScoreHook, IMeasurableHook {
		public bool CanRunRemotely() {
			return true;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task UpdateScore(ISession s, ScoreModel score, IScoreHookUpdates updates) {
			if (updates.ValueChanged) {
                if (score.Measurable.ShowCumulative || score.Measurable.ShowAverage) {
                    _UpdateCumulative(s, score.MeasurableId, score);
                }
            }
		}

		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
            if (updates.GoalChanged || updates.CumulativeRangeChanged || updates.ShowCumulativeChanged || updates.AverageRangeChanged || updates.ShowAverageChanged) {
                _UpdateCumulative(s, m.Id);
            }
        }
		public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			foreach (var sau in scoreAndUpdates)
				await UpdateScore(s, sau.score, sau.updates);
		}
		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> createdScores) {
			//noop
		}
		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//noop
		}


        private static void _UpdateCumulative(ISession s, long measurableId, ScoreModel updatedScore = null) {
            var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s, measurableId);
            var measurable = s.Get<MeasurableModel>(measurableId);
            using (var rt = RealTimeUtility.Create()) {
                L10Accessor._RecalculateCumulative_Unsafe(s, rt, measurable, recurrenceIds, updatedScore);
				if (measurable.ShowCumulative)
					rt.UpdateUsers(measurable.AccountableUserId,measurable.AdminUserId).AddLowLevelAction(x => x.updateCumulative(measurableId, measurable._Cumulative.NotNull(y => y.Value.ToString("#,##0.###"))));
				if (measurable.ShowAverage)
					rt.UpdateUsers(measurable.AccountableUserId, measurable.AdminUserId).AddLowLevelAction(x => x.updateAverage(measurableId, measurable._Average.NotNull(y => y.Value.ToString("#,##0.###"))));

				if (measurable.ShowCumulative)
                    rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.updateCumulative(measurableId, measurable._Cumulative.NotNull(y => y.Value.ToString("#,##0.###"))));
                if(measurable.ShowAverage)
                    rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.updateAverage(measurableId, measurable._Average.NotNull(y => y.Value.ToString("#,##0.###"))));
            }
        }

		public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			//noop
		}

		public async Task RemoveFormula(ISession ses, long measurableId) {
			//noop
		}

		public async Task PreSaveRemoveFormula(ISession s, long measurableId) {
			//noop
		}
	}
}