using NHibernate;
using NHibernate.SqlCommand;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Notifications;
using RadialReview.Models.Quarterly;
using RadialReview.Utilities.Hooks;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Notifications {
	public class NotificationOnNewQuarterHooks : IQuarterHook {
		public bool AbsorbErrors() {
			return false;
		}

		public bool CanRunRemotely() {
			return false;
		}

		public async Task GenerateQuarter(ISession s, QuarterModel model) {
			UserModel user=null;
			var userIds = s.QueryOver<UserOrganizationModel>()
				.JoinAlias(x=>x.User,()=> user)
				.Where(x => x.Organization.Id == model.OrganizationId &&
							x.ManagingOrganization && x.IsRadialAdmin == false
				).Select(x => x.Id,x=> user.IsRadialAdmin)
				.List<object[]>().Select(x=> new{
					Id = (long)x[0],
					SuperAdmin =(bool)x[1]
				}).Where(x=>!x.SuperAdmin)
				.Select(x=>x.Id).ToList();

			foreach (var u in userIds) {
				await NotificationAccessor.FireNotification_Unsafe(
					NotificationGroupKey.UpdateQuarterly(model.Id), u,
					NotificationDevices.Computer,
					"Your quarterly information is out of date.",
					"Update your quarter <a href='#' onclick='showModal(\"Update Quarter\",\"/Quarterly/UpdateQuarter/" + model.Id + "\",\"/Quarterly/UpdateQuarter/\")'>here</a>",
					expires: model.EndDate, canMarkSeen: false);
			}

		}

		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}

		public async Task UpdateQuarter(ISession s, QuarterModel model, IQuarterHookUpdates updates) {
			await NotificationAccessor.MarkGroupSeenUnsafe(NotificationGroupKey.UpdateQuarterly(model.Id), true);

		}
	}
}