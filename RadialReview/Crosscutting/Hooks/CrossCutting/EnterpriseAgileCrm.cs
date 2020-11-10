using System;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Models;
using RadialReview.Hooks.CrossCutting;
using static RadialReview.Utilities.Config;
using static RadialReview.Accessors.PaymentAccessor;

namespace RadialReview.Utilities.Hooks {
	public class EnterpriseAgileCrm {
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}

		public AgileCrmConfig Configs { get; protected set; }
		public AgileCrmConnector Connector { get; protected set; }

		public EnterpriseAgileCrm() {
			Configs = Config.GetAgileCrmConfig();
			Connector = new AgileCrmConnector(Configs);
		}

		public async Task BecomeEnterprise(ISession s, UserCalculator calc, OrganizationModel organization) {
			var orgAgileId = s.Get<OrganizationModel>(organization.Id);
			if (orgAgileId != null) {
				await Connector.TagsAsync("ReachedEnterpriseLevel", organization.AgileOrganizationId ?? 0);
			}
		}

		public async Task LeaveEnterprise(ISession s, UserCalculator calc, OrganizationModel organization) {
			var orgAgileId = s.Get<OrganizationModel>(organization.Id);
			if (orgAgileId != null) {
				await Connector.EnterpriseRemoveTag("ReachedEnterpriseLevel", orgAgileId.AgileOrganizationId ?? 0);
			}
		}
	}
}