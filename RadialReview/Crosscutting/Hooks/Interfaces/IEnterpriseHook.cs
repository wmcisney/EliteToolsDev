using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Accessors.PaymentAccessor;

namespace RadialReview.Utilities.Hooks {
	public interface IEnterpriseHook : IHook {
		Task BecomeEnterprise(ISession s, UserCalculator calc, OrganizationModel org);
		Task LeaveEnterprise(ISession s, UserCalculator calc, OrganizationModel org);
	}
}