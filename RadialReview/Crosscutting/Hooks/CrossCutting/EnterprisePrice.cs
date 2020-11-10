using System;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Models;
using static RadialReview.Accessors.PaymentAccessor;

namespace RadialReview.Utilities.Hooks {
	public class EnterprisePrice : IEnterpriseHook {

		public bool CanRunRemotely() {
			return false;
		}

		public bool AbsorbErrors() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}

		public async Task BecomeEnterprise(ISession s, UserCalculator calc, OrganizationModel org) {
			calc.Plan.BaselinePrice = 499m;
			calc.Plan.FirstN_Users_Free = 45;
			calc.Plan.L10PricePerPerson = 2m;
			org.PaymentPlan = calc.Plan;
			s.Update(org);
		}

		public async Task LeaveEnterprise(ISession s, UserCalculator calc, OrganizationModel org) {
			calc.Plan.BaselinePrice = 149m;
			calc.Plan.FirstN_Users_Free = 10;
			calc.Plan.L10PricePerPerson = 10m;
			org.PaymentPlan = calc.Plan;
			s.Update(org);
		}
	}
}