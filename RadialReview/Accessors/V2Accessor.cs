using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Models.V2;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Accessors {
	public class V2Accessor : BaseAccessor{

		public static V2Signup GetSignUp(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);

					var found = s.QueryOver<V2Signup>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.List().ToList();

					return found.FirstOrDefault();
				}
			}
		}
		public static bool AlreadySignedup(UserOrganizationModel caller, long orgId) {
			return GetSignUp(caller, orgId) != null;
		}
		public static async Task<long> AddSignup(UserOrganizationModel caller, V2Signup signup) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(signup.OrgId);
					signup.ByUser = caller.Id;
					signup.CreateTime = DateTime.UtcNow;
					signup.ByUserEmail = caller.GetEmail();

					s.Save(signup);

					tx.Commit();
					s.Flush();
				}
			}

			var json=JsonConvert.SerializeObject(signup,Formatting.Indented, new StringEnumConverter());
			try {
				using (var client = new HttpClient()) {
					var content = new StringContent(json, Encoding.UTF8, "application/json");
					//var response = await client.PostAsync("REPLACE_ME", content);
				//	var responseString = await response.Content.ReadAsStringAsync();
				}
			} catch (Exception e) {
				log.Error("Filed to send roll-out flexie information");
				log.Info(json);
				//var mail = Mail.To("rollout-error","REPLACE_ME")
				//				.Subject("Rollout - Flexie error")
				//				.Body("Caller:"+caller.NotNull(x=>x.Id)+"<br/>\n Json:"+json);
				//await Emailer.EnqueueEmail(mail);
			}


			return signup.Id;
		}

		public static void HaltSignup(UserOrganizationModel caller, long orgId, string reason) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);

					var found = s.QueryOver<V2Signup>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.List().ToList();

					foreach (var f in found) {
						f.HaltedBy = caller.Id;
						f.HaltedReason = reason;
						f.DeleteTime = DateTime.UtcNow;
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void Increment(string id,long userId, string userHostAddress) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Save(new V2ViewCount() {
						Name = id,
						UserId = userId,
						Ip = userHostAddress
					});
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<SelectListItem> GetPossiblePrimaryContacts(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);

					var users = s.QueryOver<UserLookup>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().OrderBy(x=>x.Name).ToList();

					var all = new SelectListGroup() { Name = "All Users" };
					var managers = new SelectListGroup() { Name = "Managers" };
					var admins = new SelectListGroup() { Name = "Admins" };
					var leadership = new SelectListGroup() { Name = "Leadership Team" };
					var ltMembers = new List<long>();

					try {
						var ltMeetings = s.QueryOver<L10Recurrence>()
							.Where(x => x.OrganizationId == orgId && x.DeleteTime == null && x.TeamType == L10TeamType.LeadershipTeam)
							.Select(x => x.Id)
							.List<long>().ToArray();

						ltMembers = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
							.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(ltMeetings)
							.Where(x => x.DeleteTime == null)
							.Select(x => x.User.Id)
							.List<long>().ToList();
					} catch (Exception e) {
					}

					var output = new List<SelectListItem>();

					foreach (var u in users) {
						if (u.IsAdmin)
							output.Add(new SelectListItem() { Text = u.Name, Value = "" + u.UserId, Group = admins });
					}
					foreach (var u in users) {
						if (ltMembers.Contains(u.UserId))
							output.Add(new SelectListItem() { Text = u.Name, Value = "" + u.UserId, Group = leadership });
					}
					foreach (var u in users) {
						if (u.IsManager)
							output.Add(new SelectListItem() { Text = u.Name, Value = "" + u.UserId, Group = managers });
					}
					foreach (var u in users) {
						output.Add(new SelectListItem() { Text = u.Name, Value = "" + u.UserId, Group = all });
					}

					return output;
				}
			}
		}
	}
}