﻿using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Askables;
using System.Threading.Tasks;
using RadialReview.Models;
using RadialReview.Models.Scorecard;

namespace RadialReview.Crosscutting.Hooks {
	public class UpdateUserCache : IRockHook, IMeasurableHook {
		public bool CanRunRemotely() {
			return false; //Must be false, needs context.
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Unset;
		}

		public bool AbsorbErrors() {
			return false;
		}

		private async Task UpdateForUser(ISession s, long userId) {
			s.Flush();
			var user = s.Get<UserOrganizationModel>(userId);
			if (user != null) {
				user.UpdateCache(s);
			}
		}


		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {
			await UpdateForUser(s, rock.ForUserId);
		}

		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			await UpdateForUser(s, rock.ForUserId);
		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			await UpdateForUser(s, rock.ForUserId);
		}
		public async Task UnArchiveRock(ISession s, RockModel rock, bool v) {
			await UpdateForUser(s, rock.ForUserId);
		}

		public async Task UndeleteRock(ISession s, RockModel rock) {
			await UpdateForUser(s, rock.ForUserId);
		}

		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> createdScores) {
			await UpdateForUser(s, m.AccountableUserId);
			if (m.AccountableUserId != m.AdminUserId)
				await UpdateForUser(s, m.AdminUserId);
		}

		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (updates.AccountableUserChanged)
				m.AccountableUser.UpdateCache(s);

			if (updates.AdminUserChanged)
				m.AdminUser.UpdateCache(s);

		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			s.Flush();
			s.GetFresh<UserOrganizationModel>(measurable.AccountableUserId).UpdateCache(s);
			if (measurable.AccountableUserId != measurable.AdminUserId)
				s.GetFresh<UserOrganizationModel>(measurable.AdminUserId).UpdateCache(s);

			
		}
	}
}