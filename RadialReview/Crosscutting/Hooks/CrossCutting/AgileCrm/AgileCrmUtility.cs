using Newtonsoft.Json;
using NHibernate;
using RadialReview.Hooks.CrossCutting;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using static RadialReview.AgileCrm.AgileCrmConstants;
using static RadialReview.Utilities.Config;

namespace RadialReview.AgileCrm {
	public class AgileCrmUtility {

		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Highest;
		}

		public AgileCrmConfig Configs { get; protected set; }
		public AgileCrmConnector Connector { get; protected set; }

		public AgileCrmUtility() {
			Configs = Config.GetAgileCrmConfig();
			Connector = new AgileCrmConnector(Configs);
		}

		public async Task<string> AgileCrmOrgCreationData(OrganizationModel organization, string companyEmail, AgileCrmConnector.AgileContactModel responseId, OrgCreationData ocd, Coach coach) {
			var ocdCoachName = coach.NotNull(x => x.Name);
			var ocdCoachType = "" + coach.NotNull(x => x.CoachType);
			var ocdHasEOSI = "" + ocd.NotNull(x => x.HasCoach);
			var ocdEnablePeople = "" + ocd.NotNull(x => x.EnablePeople);
			var ocdEnableReviews = "" + ocd.NotNull(x => x.EnableReview);
			var ocdEnableL10 = "" + ocd.NotNull(x => x.EnableL10);
			var ocdEnableAc = "" + ocd.NotNull(x => x.EnableAC);

			var responseCompanyId = await Connector.RequestAsync("contacts/edit-properties", HttpMethod.Put, JsonConvert.SerializeObject(
			new {
				id = responseId.id,
				properties = new object[] {
					new {name=AgileCrmConst.EMAIL, type=AgileCrmConst.SYSTEM, value=companyEmail},
					new {name=AgileCrmConst.NAME, type=AgileCrmConst.SYSTEM, value=organization.Organization.GetName()},
					new {name=AgileCrmConst.PAYMENT_STATUS, type=AgileCrmConst.CUSTOM, value=""+organization.Organization.AccountType},
					new {name=AgileCrmConst.BILLING_NAME, type=AgileCrmConst.CUSTOM, value=organization.GetName()},
					new {name=AgileCrmConst.ORGID, type=AgileCrmConst.CUSTOM, value=+organization.Organization.Id},
					new {name=AgileCrmConst.MEMBER_SINCE, type=AgileCrmConst.CUSTOM, value=(long)(organization.CreationTime.ToJsMs()/1000)},
					new {name=AgileCrmConst.COACH, type=AgileCrmConst.CUSTOM, value=ocdHasEOSI},
					new {name=AgileCrmConst.COACH_TYPE, type=AgileCrmConst.CUSTOM, value=ocdCoachType},
					new {name=AgileCrmConst.ENABLE_PEOPLE, type=AgileCrmConst.CUSTOM, value=ocdEnablePeople},
					new {name=AgileCrmConst.ENABLE_REVIEWS, type=AgileCrmConst.CUSTOM, value=ocdEnableReviews},
					new {name=AgileCrmConst.ENABLE_L10, type=AgileCrmConst.CUSTOM, value=ocdEnableL10},
					new {name=AgileCrmConst.ENABLE_AC, type=AgileCrmConst.CUSTOM, value=ocdEnableAc},
				}
			}));
			return responseCompanyId;
		}
	}
}