using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public class IOrganizationHookUpdates {
		public class UpdatedSettings {
			internal bool DateFormat;

			public bool StrictHierarchy { get; internal set; }
			public bool ManagersCanEditPositions { get; internal set; }
			public bool SendEmailImmediately { get; internal set; }
			public bool ManagersCanEditSelf { get; internal set; }
			public bool LimitFiveState { get; internal set; }
			public bool EmployeesCanEditSelf { get; internal set; }
			public bool EmployeesCanCreateSurvey { get; internal set; }
			public bool OnlySeeRocksAndScorecardBelowYou { get; internal set; }
			public bool ScorecardPeriod { get; internal set; }
			public bool ManagersCanCreateSurvey { get; internal set; }
			public bool RockName { get; internal set; }
			public bool TimeZoneId { get; internal set; }
			public bool WeekStart { get; internal set; }
			public bool StartOfYearMonth { get; internal set; }
			public bool StartOfYearOffset { get; internal set; }
			public bool NumberFormat { get; internal set; }
			public bool AllowAddClient { get; internal set; }
			public bool DefaultSendTodoTime { get; internal set; }
			public bool PrimaryColor { get; internal set; }
			public bool ShareVto { get; internal set; }
			public bool ShareVtoPages { get; internal set; }
		}
		public IOrganizationHookUpdates() {
			Settings = new UpdatedSettings();
		}
		public bool UpdateName { get;  set; }
		public bool AnySettings { get; set; }
		public UpdatedSettings Settings { get; private set; }
		
	}

	public interface IOrganizationHook : IHook {
		Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization);
		Task UpdateOrganization(ISession s,  long organizationId, IOrganizationHookUpdates updates);
	}
}
