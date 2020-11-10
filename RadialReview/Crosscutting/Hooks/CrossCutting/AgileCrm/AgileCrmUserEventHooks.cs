using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using RadialReview.Models.Events;
using RadialReview.Utilities;
using System.Net.Http;
using System.Threading.Tasks;
using RadialReview.Models;
using RadialReview.Models.Enums;
using static RadialReview.Utilities.Config;
using log4net;
using RadialReview.Hooks.CrossCutting;
using Newtonsoft.Json;
using RadialReview.Models.Application;
using RadialReview.Models.Payments;
using static RadialReview.AgileCrm.AgileCrmConstants;

namespace RadialReview.Hooks {
	public class AgileCrmUserEventHooks : ICreateUserOrganizationHook, IUpdateUserModelHook, IDeleteUserOrganizationHook {

		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}
		public bool AbsorbErrors() {
			return true;
		}

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public AgileCrmConfig Configs { get; protected set; }
		public AgileCrmConnector Connector { get; protected set; }

		public AgileCrmUserEventHooks() {
			Configs = Config.GetAgileCrmConfig();
			Connector = new AgileCrmConnector(Configs);
		}

		public async Task CreateUserOrganization(ISession s, UserOrganizationModel user) {
			await AddContact(s, user.Id);
		}

		public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel userOrganization) {
			if (userOrganization.AgileUserId != null && userOrganization.AgileUserId != 0) {
				await Connector.TagsAsync("AttachComplete", userOrganization.AgileUserId.Value);
			}
			// Detect First Login User
			await RegisterFirstLogin(s, userOrganization);
			// Change Owner User Attach
			//var ocd = s.QueryOver<OrgCreationData>().Where(x => x.OrgId == userOrganization.Organization.Id).Take(1).List().SingleOrDefault();
			//if (ocd != null && ocd.AssignedTo != null) {
			//	var assignedTo = s.Get<SupportMember>(ocd.AssignedTo.Value).User.GetEmail();
			//	if (assignedTo != null) {
			//		await Connector.ChangeOwner(userOrganization.AgileUserId, assignedTo);
			//	}
			//}
		}

		public async Task UpdateUserModel(ISession s, UserModel user) {
			var ttuserIds = user.UserOrganizationIds;
			foreach (var ttuserId in ttuserIds) {
				var userId = s.Get<UserOrganizationModel>(ttuserId);
				var agileUserId = userId.AgileUserId;
				if (agileUserId != null && agileUserId != 0) {
					var response = await Connector.PullAgileCrmData(agileUserId ?? 0);
					if (agileUserId == response.id) {
						await Connector.RequestAsync("contacts/edit-properties", HttpMethod.Put, JsonConvert.SerializeObject(new {
							id = response.id,
							properties = new[] {
								new {name=AgileCrmConst.FIRST_NAME, type=AgileCrmConst.SYSTEM, value=user.FirstName},
								new {name=AgileCrmConst.LAST_NAME, type=AgileCrmConst.SYSTEM, value=user.LastName}
							}
						}));
					}
				}
			}
		}

		public async Task DeleteUser(ISession s, UserOrganizationModel user) {
			if (user.AgileUserId != null && user.AgileUserId != 0) {
				await Connector.TagsAsync("Deleted", user.AgileUserId ?? 0);
			}
		}

		public async Task UndeleteUser(ISession s, UserOrganizationModel user) {
			var eventTag = "Deleted";
			var agileUserId = user.AgileUserId;
			if (agileUserId != null && agileUserId != 0) {
				var response = await Connector.PullAgileCrmData(agileUserId ?? 0);
				// Remove Tag
				await Connector.RemoveTag(response.id, eventTag);
			}
		}

		//public async Task CreatePrimaryContact(ISession s, AccountEvent evt, AgileCrmConfig configs, AgileCrmConnector connector) {
		//	var contact = s.Get<UserOrganizationModel>(evt.ForModel.ModelId);
		//	var tags = new List<string> { "primary_contact" };

		//	if (contact.Organization.AccountType == AccountType.Implementer) {
		//		tags.Add("is_eosi");
		//	}
		//	// Add tag
		//	await connector.AddTags(contact.AgileUserId ?? 0, tags);
		//}

		public async Task<long> AddContact(ISession s, long contactUserOrganizationId) {
			var connector = this;
			var contact = s.Get<UserOrganizationModel>(contactUserOrganizationId);
			var agileUserId = contact.AgileUserId;
			var contactEmail = contact.GetEmail();
			contactEmail = Config.ModifyEmail(contactEmail);
			if (agileUserId == null && agileUserId == 0) {
				var responseUserId = await Connector.RequestAsync("contacts", HttpMethod.Post, JsonConvert.SerializeObject(new {
					properties = new object[] {
						new {name=AgileCrmConst.EMAIL,type=AgileCrmConst.SYSTEM, value=contactEmail},
						new {name=AgileCrmConst.FIRST_NAME,type=AgileCrmConst.SYSTEM, value=contact.GetFirstName()},
						new {name=AgileCrmConst.LAST_NAME,type=AgileCrmConst.SYSTEM, value=contact.GetLastName()},
						new {name=AgileCrmConst.COMPANY, type=AgileCrmConst.SYSTEM, value=contact.Organization.GetName()},
						new {name=AgileCrmConst.USERID, type=AgileCrmConst.CUSTOM, value=+contact.Id},
						new {name=AgileCrmConst.ORGID, type=AgileCrmConst.CUSTOM, value=+contact.Organization.Id},
						new {name=AgileCrmConst.BEEN_A_MEMBER_SINCE,type=AgileCrmConst.CUSTOM, value=(long)(contact.CreateTime.ToJsMs()/1000)},
						new {name=AgileCrmConst.IS_AN_ACCOUNT_ADMIN,type=AgileCrmConst.CUSTOM, value=contact.IsManagingOrganization()},
					}
				}));

				dynamic userObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseUserId)["id"];
				contact.AgileUserId = userObj;
				s.Update(contact);
			}
			return contact.AgileUserId ?? 0;
		}

		public async Task RegisterFirstLogin(ISession s, UserOrganizationModel contact) {
			var agileCrmId = contact.AgileUserId;
			if (agileCrmId != null) {
				await Connector.RequestAsync("contacts/edit-properties", HttpMethod.Put, JsonConvert.SerializeObject(new {
					id = agileCrmId,
					properties = new object[] {
						new {name=AgileCrmConst.FIRST_LOGIN_COMPLETED, type=AgileCrmConst.CUSTOM, value="True"},
						new {name=AgileCrmConst.FIRST_LOGIN_TIMESTAMP, type=AgileCrmConst.CUSTOM, value=contact.AttachTime},
					}
				}));
			}
		}

		public async Task OnUserRegister(ISession s, UserModel user) {
			//Noop	
		}
	}
}