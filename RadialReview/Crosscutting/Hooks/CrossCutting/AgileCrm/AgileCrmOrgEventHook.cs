using RadialReview.Utilities.Hooks;
using System;
using System.Linq;
using NHibernate;
using RadialReview.Utilities;
using System.Net.Http;
using System.Threading.Tasks;
using RadialReview.Models;
using static RadialReview.Utilities.Config;
using RadialReview.Hooks.CrossCutting;
using Newtonsoft.Json;
using RadialReview.Models.Application;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using static RadialReview.AgileCrm.AgileCrmConstants;
using RadialReview.AgileCrm;
using RadialReview.Models.Enums;
using System.Collections.Generic;

namespace RadialReview.Hooks {
	public class AgileCrmOrgEventHook : IOrganizationHook, IPaymentHook {
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Highest;
		}
		public bool AbsorbErrors() {
			return true;
		}

		public AgileCrmConfig Configs { get; protected set; }
		public AgileCrmConnector Connector { get; protected set; }

		public AgileCrmOrgEventHook() {
			Configs = Config.GetAgileCrmConfig();
			Connector = new AgileCrmConnector(Configs);
		}

		public async Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization) {
			await GetOrCreateAgileOrgId(s, organization.Id);
			await CreatePrimaryContact(s, organization);
			//var ocd = s.QueryOver<OrgCreationData>().Where(x => x.OrgId == organization.Organization.Id).Take(1).List().SingleOrDefault();
			//if (ocd != null && ocd.AssignedTo != null)
			//{
			//    var assignedTo = s.Get<SupportMember>(ocd.AssignedTo.Value).User.GetEmail();
			//    if (assignedTo != null)
			//    {
			//        await Connector.ChangeOwner(organization.AgileOrganizationId, assignedTo);
			//    }
			//}
		}

		public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
			var org = s.Get<OrganizationModel>(token.OrganizationId);
			if (org != null && org.AgileOrganizationId != null) {
				await Connector.TagsAsync("PaymentCardUpdated", org.AgileOrganizationId.Value);
			}
		}

		public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token,decimal amount) {
			var org = s.Get<OrganizationModel>(token.OrganizationId);
			if (org != null && org.AgileOrganizationId != null) {
				await Connector.TagsAsync("PaymentChargedSuccessfully", org.AgileOrganizationId.Value);
			}
		}

		public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
			var org = s.Get<OrganizationModel>(orgId);
			if (org != null && org.AgileOrganizationId != null) {
				await Connector.TagsAsync("PaymentFailedCaptured", org.AgileOrganizationId.Value);
			}
		}

		public async Task<long> GetOrCreateAgileOrgId(ISession s, long organizationId) {
			var organization = s.Get<OrganizationModel>(organizationId);

			var orgEmail = s.Get<UserOrganizationModel>(organization.PrimaryContactUserId);
			
			if (orgEmail != null) {
				var companyEmail = Config.ModifyEmail(orgEmail.GetEmail());
				var agileContactId = orgEmail.AgileUserId;
				var responseId = await Connector.PullAgileCrmData(agileContactId);
				var ocd = s.QueryOver<OrgCreationData>().Where(x => x.OrgId == organizationId).Take(1).List().SingleOrDefault();
				if (ocd != null) {
					var agileOrgId = organization.AgileOrganizationId;
					if (agileOrgId == null || agileOrgId == 0) {
						var coach = s.Get<Coach>(ocd.CoachId.Value);
						//AgileCrmUtility agileUtility = new AgileCrmUtility();
						//string responseCompanyId = await agileUtility.AgileCrmOrgCreationData(organization, companyEmail, responseId, ocd, coach);
						var ocdCoachName = coach.NotNull(x => x.Name);
						var ocdCoachType = "" + coach.NotNull(x => x.CoachType);
						var ocdHasEOSI = "" + ocd.NotNull(x => x.HasCoach);
						var ocdEnablePeople = "" + ocd.NotNull(x => x.EnablePeople);
						var ocdEnableReviews = "" + ocd.NotNull(x => x.EnableReview);
						var ocdEnableL10 = "" + ocd.NotNull(x => x.EnableL10);
						var ocdEnableAc = "" + ocd.NotNull(x => x.EnableAC);

						var responseCompanyId = await Connector.RequestAsync("contacts/edit-properties", HttpMethod.Put, JsonConvert.SerializeObject(
						new {
							id = responseId.companyId,
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
						//dynamic orgObj = JsonConvert.DeserializeObject<dynamic>(responseCompanyId);
						dynamic orgObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseCompanyId)["id"];
						//var orgObjId = ((string)(orgObj.id)).TryParseLong();
						organization.AgileOrganizationId = orgObj;
						s.Update(organization);
					}
					return organization.AgileOrganizationId ?? 0;
					throw new Exception("AgileOrgId response was null");
				}
			}
			throw new Exception("orgEmail response was null");

		}

		public async Task CreatePrimaryContact(ISession s, OrganizationModel organization) {
			var contact = s.Get<UserOrganizationModel>(organization.PrimaryContactUserId);
			var tags = new List<string> { "primary_contact" };

			if (contact.Organization.AccountType == AccountType.Implementer) {
				tags.Add("is_eosi");
			}
			// Add tag
			if (contact.AgileUserId != null && contact.AgileUserId != 0) {
				await Connector.AddTags(contact.AgileUserId ?? 0, tags);
			}
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
			//Noop
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			//Noop
		}

		public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
			//Noop
		}

		public async Task UpdateOrganization(ISession s, long organizationId, IOrganizationHookUpdates updates) {
			//noop
		}

	}
}