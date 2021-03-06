﻿using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Flags;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.Permissions;
using RadialReview.Models.UserModels;
using RadialReview.Models.V2;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Testing;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class OrganizationController : BaseController {
		//
		// GET: /Organization/
		//[Access(AccessLevel.Any)]
		//public ActionResult Index() {
		//	return View();
		//}

		public class OrgStatDto {
			public AccountType AccountType { get; set; }
			public long Count { get; set; }
		}

		public class OrgStatsSummary {
			public List<OrgStatDto> Statistic { get; set; }
		}

		public class OrgStats {
			public long OrgId { get; set; }
			public string OrgName { get; set; }
			public string Username { get; set; }
			public DateTime? LastLogin { get; set; }
			public string Position { get; set; }
			public DateTime? OrgCreateTime { get; set; }
			public DateTime? TrialEnd { get; set; }
			public DateTime? LastMeeting { get; set; }
			public DateTime? CreditCardExp { get; set; }

			public AccountType Status { get; set; }
			public bool L10Enabled { get; internal set; }
			public bool EvalEnabled { get; internal set; }
			public bool PeopleEnabled { get; internal set; }
		}

		private static string[] supportNames = new string[] {};

		//private static List<OrgStats> GenerateStats() {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {

		//			var orgs = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).Future();
		//			var listQ = s.QueryOver<UserLookup>().Where(x => x.DeleteTime == null && !x.IsRadialAdmin)
		//				.WhereRestrictionOn(x => x.Name).Not.IsIn(supportNames)
		//				.OrderBy(x => x.LastLogin).Desc
		//				.SelectList(a =>
		//					a.Select(x => x.OrganizationId)
		//					 .Select(x => x.Name)
		//					 .Select(x => x.Positions)
		//					 .Select(x => x.LastLogin)
		//				)/*.Take(1)*/
		//				.Future<object[]>();

		//			var tokensQ = s.QueryOver<PaymentSpringsToken>().Where(x => x.Active == true && x.DeleteTime == null).Future();

		//			var meetingLastLUQ = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.CompleteTime != null)
		//				.OrderBy(x => x.CompleteTime).Desc.Future();

		//			var tokens = tokensQ.ToDictionary(x => x.OrganizationId, x => x);
		//			var meetingLastLU = meetingLastLUQ.ToList().GroupBy(x => x.OrganizationId).Select(x => x.First())
		//				.ToDictionary(x => x.OrganizationId, x => x.CompleteTime);
		//			//.Select(
		//			//    Projections.Distinct(Projections.Property("OrganizationId")),
		//			//    Projections.Property("CompleteTime")
		//			//).List<object[]>().ToDictionary(x => (long)x[0], x => (DateTime?)x[1]);

		//			var list = listQ.Select(x => new {
		//				OrganizationId = (long)x[0],
		//				Name = (string)x[1],
		//				Positions = (string)x[2],
		//				LastLogin = (DateTime?)x[3],
		//			}).ToList();

		//			var stats = orgs.Select(x => {
		//				var user = list.Where(y => y.OrganizationId == x.Id).OrderByDescending(y => y.LastLogin).FirstOrDefault();
		//				DateTime? trialEnd = DateTime.MinValue;
		//				try {
		//					trialEnd = tokens.ContainsKey(x.Id) ? (DateTime?)null : x.NotNull(u => u.PaymentPlan.NotNull(z => z.FreeUntil));
		//				} catch (Exception) {
		//				}

		//				return new OrgStats() {
		//					OrgId = x.NotNull(y => y.Id),
		//					OrgName = x.NotNull(y => y.GetName()),
		//					L10Enabled = x.NotNull(y => y.Settings.EnableL10),
		//					EvalEnabled = x.NotNull(y => y.Settings.EnableReview),
		//					PeopleEnabled = x.NotNull(y => y.Settings.EnablePeople),
		//					Username = user.NotNull(y => y.Name),
		//					Position = user.NotNull(y => y.Positions),
		//					LastLogin = user.NotNull(y => y.LastLogin),
		//					OrgCreateTime = x.NotNull(u => u.CreationTime),
		//					Status = x.NotNull(y => y.AccountType),
		//					LastMeeting = meetingLastLU.GetOrDefault(x.NotNull(y => y.Id), null),
		//					TrialEnd = trialEnd,
		//					CreditCardExp = !tokens.ContainsKey(x.Id) || tokens[x.Id].TokenType != PaymentSpringTokenType.CreditCard ? (DateTime?)null : new DateTime(tokens[x.Id].YearExpire, tokens[x.Id].MonthExpire, 1)
		//				};
		//			}).OrderByDescending(x => x.OrgId).ToList();

		//			return stats;

		//		}
		//	}
		//}

		private static OrgStatsSummary GenerateStats() {
			OrgStatsSummary statSummary = new OrgStatsSummary();
			List<OrgStatDto> list = new List<OrgStatDto>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var query = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).Select(Projections.Group<OrganizationModel>(e => e.AccountType), Projections.Count<OrganizationModel>(e => e.Id)).List<object[]>().ToList();

					foreach (var item in query) {
						var accountType = (AccountType)item[0];
						var count = Convert.ToInt64(item[1]);
						OrgStatDto dto = new OrgStatDto { AccountType = accountType, Count = count };
						list.Add(dto);
					}
				}
			}
			statSummary.Statistic = list;
			return statSummary;
		}



		private void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs) {
			var currentError = errorArgs.ErrorContext.Error.Message;
			errorArgs.ErrorContext.Handled = true;
		}


		public class PortVM {
			public string InjectedPortScript { get; set; }
			public long OrgId { get; set; }
			public DateTime? DeleteTime { get; set; }
			public bool AlreadyMigrated { get; set; }
			public string OrgName { get; set; }
		}


		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> UnmarkMigrate(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);
					var now = DateTime.UtcNow;
					if (org.Lockout == LockoutType.MigratedToV2) {
						org.Lockout = LockoutType.NoLockout;
						org.DeleteTime = null;
						s.Update(org);
					}


					var v2Signups = s.QueryOver<V2Signup>()
										.Where(x => x.DeleteTime != null && x.OrgId == id)
										.List().ToList();

					foreach (var v in v2Signups) {
						if (v.MigrationExecuted != null) {
							v.MigrationExecuted = null;
							v.MigratedBy = null;
							s.Update(v);
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> MarkMigrate(long id, bool force = false, bool email = true) {
			var callerId = GetUser().Id;
			var markedOrgMigrated = false;
			var markedV2SignupMigrated = false;
			var notifications = new List<string>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);
					if (!force && org.Lockout == LockoutType.MigratedToV2) {
						throw new Exception("Already marked migrated");
					}
					var now = DateTime.UtcNow;
					if (org.Lockout != LockoutType.MigratedToV2) {
						org.Lockout = LockoutType.MigratedToV2;
						org.DeleteTime = now;
						s.Update(org);
						markedOrgMigrated = true;
					}

					var v2Signups = s.QueryOver<V2Signup>()
										.Where(x => x.DeleteTime != null && x.OrgId == id)
										.List().ToList();

					foreach (var v in v2Signups) {
						if (v.MigrationExecuted == null) {
							v.MigrationExecuted = now;
							v.MigratedBy = callerId;
							s.Update(v);
							markedV2SignupMigrated = true;
						}
						var pc = s.Get<UserOrganizationModel>(v.PrimaryContactId);
						if (pc.DeleteTime == null && pc.Organization.Id == id) {
							notifications.Add(pc.GetEmail());
						}
					}

					var admins = s.QueryOver<UserOrganizationModel>()
								.Where(x => x.DeleteTime == null && x.Organization.Id == id && x.ManagingOrganization)
								.List().ToList();

					foreach (var a in admins) {
						notifications.Add(a.GetEmail());
					}

					notifications = notifications.Distinct().ToList();

					if (notifications.Any() && email) {
						//REPLACE_ME
						var body = s.GetSettingOrDefault(Variable.Names.V2_MIGRATIONDONE_BODY, () =>
						 "We've completed your migration! Login to <a href='https://dlptools.com/account/login'>https://dlptools.com/account/login</a> to take a tour." +
						 "<br/><br/>If you have any questions you can reply to this email, our support team will make sure you have everything you need." +
						 "<br/><br/>Thanks for helping make this possible!<br/>The EliteTools Team");

						//REPLACE_ME
						var m = Mail.Bcc("V2Migrated", notifications.ToArray())
							.SubjectPlainText("EliteTools has completed your migration!")
							.Body(body);
						//REPLACE_ME
						m.ReplyToAddress = "support@dlptools.com";
						m.ReplyToName = "EliteTools Support";

						await Emailer.EnqueueEmail(m);
					} else {
						notifications = new List<string>();
					}


					tx.Commit();
					s.Flush();

				}
			}
			return Json(ResultObject.SilentSuccess(new {
				markedOrgMigrated,
				markedV2SignupMigrated,
				notifications,
				email,
				forceMigrate = force,
				notificationsCount = notifications.Count
			}), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> Port(long id) {
			var o = new PortVM();
			o.InjectedPortScript = VariableAccessor.Get(Variable.Names.V2_PORT_SCRIPT, () => "");
			o.OrgId = id;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);
					o.AlreadyMigrated = org.Lockout == LockoutType.MigratedToV2;
					o.DeleteTime = org.DeleteTime;
					o.OrgName = org.GetName();
					var signups = s.QueryOver<V2Signup>().Where(x => x.OrgId == id && x.DeleteTime == null).List().ToList();
					ViewBag.V2SignedUp = signups;
				}
			}

			return View(o);
		}


		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> UserEnc(long id) {
			var o = OrgExport.GetUsers(GetUser(), id);
			return Content(o);
		}
		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> Json(long id) {
			var o = OrgExport.GetJson(GetUser(), id);
			var res = JsonConvert.SerializeObject(o, new StringEnumConverter());


			var zip = ZipUtility.Zip(new ZipUtility.File() {
				Name = "json.txt",
				Contents = res
			},
			new ZipUtility.File() {
				Name="user.txt",
				Contents = OrgExport.GetUsers(GetUser(), id)
			});

			string orgName = "na";

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					orgName = s.Get<OrganizationModel>(id).GetName();
				}
			}

			var zipName = String.Format("{0:0000000000}", id)+"-"+DateTime.UtcNow.ToString("yyyyMMddhhmm")+"-"+ orgName+".zip";
			
			string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
			foreach (char c in invalid) {
				zipName = zipName.Replace(c.ToString(), "");
			}


			return File(zip, "application/zip", zipName);
			
		}


		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> Flags(long? id = null) {
			id = id ?? GetUser().Organization.Id;
			var flags = await OrganizationAccessor.GetFlags(GetUser(), id.Value);
			return View(flags);
		}





		[Access(AccessLevel.Radial)]
		[HttpPost]
		public async Task<JsonResult> SetFlags(long id, bool status, OrganizationFlagType flag) {
			await OrganizationAccessor.SetFlag(GetUser(), id, flag, status);
			return Json(ResultObject.SilentSuccess());
		}


		[Access(AccessLevel.RadialData)]
		public ActionResult Stats(string type = null) {
			var stats = GenerateStats();
			return View(stats);

			//         type = (type ?? "").ToLower();

			//if (type != "csv")
			//	return View(stats);
			//else {
			//	var csv = new Csv();
			//	csv.Title = "OrgId";
			//	foreach (var o in stats) {
			//		csv.Add("" + o.OrgId, "OrgName", o.OrgName);
			//		csv.Add("" + o.OrgId, "User", o.Username);
			//		csv.Add("" + o.OrgId, "Position", o.Position);
			//		csv.Add("" + o.OrgId, "CreateTime", o.OrgCreateTime.NotNull(x => x.Value.ToString()));
			//		csv.Add("" + o.OrgId, "Status", "" + o.Status);
			//		csv.Add("" + o.OrgId, "LastLogin", o.LastLogin.NotNull(x => x.Value.ToString()));
			//		csv.Add("" + o.OrgId, "TrialEnd", o.TrialEnd.ToString());
			//		csv.Add("" + o.OrgId, "LastMeeting", o.LastMeeting.NotNull(x => x.Value.ToString()));
			//		csv.Add("" + o.OrgId, "CreditCardExp", o.CreditCardExp.ToString());
			//		csv.Add("" + o.OrgId, "L10_Enabled", o.L10Enabled.ToString());
			//		csv.Add("" + o.OrgId, "Eval_Enabled", o.EvalEnabled.ToString());
			//		csv.Add("" + o.OrgId, "People_Enabled", o.PeopleEnabled.ToString());
			//	}

			//	return File(csv.ToBytes(), "text/csv", DateTime.UtcNow.ToJavascriptMilliseconds() + "_OrgStats.csv");
			//}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Invites() {
			var members = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails);

			var temps = members.Where(x => x.HasJoined == false).Select(x => UserAccessor.GetUserOrganization(GetUser(), x.UserId, true, false, PermissionType.EditEmployeeDetails).TempUser).Where(x => x != null).ToList();

			return View(temps);
		}

		//[Access(AccessLevel.Radial)]
		//public ActionResult StatsExport() {
		//	var stats = GenerateStats();
		//}

		public class TinyOrgVM {
			public long Id { get; set; }
			public string Name { get; set; }
			public AccountType AccountType { get; set; }
			public long? PlanId { get; set; }
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Which(long? id = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					if (id == null) {
						PaymentPlanModel planAlias = null;
						LocalizedStringModel nameAlias = null;
						var list = s.QueryOver<OrganizationModel>()
							.JoinAlias(x => x.PaymentPlan, () => planAlias)
							.JoinAlias(x => x.Name, () => nameAlias)
							.Where(x => x.DeleteTime == null)
							.Select(x => nameAlias.Standard, x => x.Id, x => x.AccountType, x => planAlias.Id)
							.List<object[]>()
							.Select(x => new TinyOrgVM() {
								Name = (string)x[0],
								Id = (long)x[1],
								AccountType = (AccountType)x[2],
								PlanId = (long?)x[3],
							})
							.ToList();


						return View("WhichList", list);
					}

					Thread.SetData(Thread.GetNamedDataSlot("AllowSpecialOrgs"), true);

					var user = GetUser();
					user._PermissionsOverrides.Admin.AllowAdminWithoutAudit = true;
					var perms = PermissionsUtility.Create(s, user);//.ManagingOrganization(id.Value);

					var org = s.Get<OrganizationModel>(id);
					if (org == null) {
						return Content("Not an organization");
					}

					var b = org.PaymentPlan.FreeUntil;

					ViewBag.Members = OrganizationAccessor.GetOrganizationMembersLookup(s, perms, id.Value, false);
					var meetings = s.QueryOver<L10Meeting>()
						.Fetch(x => x.L10Recurrence).Lazy
						.Where(x => x.OrganizationId == id && x.CreateTime > DateTime.UtcNow.AddDays(-90))
						.List().ToList();

					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>().WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetings.Select(x => x.Id).ToList()).Select(x => x.L10Meeting.Id).List<long>().ToList();

					var attendeeslookup = attendees.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

					foreach (var m in meetings) {
						m._MeetingAttendees = Enumerable.Range(0, attendeeslookup.GetOrDefault(m.Id, 0)).Select(x => (L10Meeting.L10Meeting_Attendee)null).ToList();
					}

					ViewBag.Meetings = meetings;

					ViewBag.Credits = PaymentAccessor.GetCredits(s, perms, id.Value);// s.QueryOver<PaymentCredit>().Where(x => x.OrgId == id && x.DeleteTime == null).List().ToList();

					var signups = s.QueryOver<V2Signup>().Where(x => x.OrgId == id && x.DeleteTime == null).List().ToList();

					ViewBag.V2SignedUp = signups;

					return View(org);

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult SetAccountType(long id, AccountType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);

					if (org.AccountType == AccountType.Cancelled && type != AccountType.Cancelled) {
						org.DeleteTime = null;
					}
					if (type == AccountType.Cancelled && org.AccountType != AccountType.Cancelled) {
						org.DeleteTime = DateTime.UtcNow;
					}
					org.AccountType = type;
					s.Update(org);

					tx.Commit();
					s.Flush();

					return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
				}
			}
		}



		[Access(AccessLevel.Radial)]
		public JsonResult Delete(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, GetUser()).ManagingOrganization(id);

					var org = s.Get<OrganizationModel>(id);
					org.DeleteTime = DateTime.UtcNow;

					tx.Commit();
					s.Flush();

					return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
				}
			}
		}

		private void PrepareCreateOrgViewBag() {
			var implementers = ApplicationAccessor.GetCoaches(GetUser()).OrderBy(x => x.Name).ToSelectList(x => x.Name, x => x.Id);
			implementers.Insert(0, new SelectListItem() { Text = "<select coach>", Value = "-1" });
			ViewBag.Implementers = implementers;

			var support = ApplicationAccessor.GetSupportMembers(GetUser()).OrderBy(x => x.User.GetName());
			var myIds = new long[0];
			try {
				myIds = GetUser().User.UserOrganizationIds;
			} catch (Exception e) {
			}
			ViewBag.MySupportId = support.FirstOrDefault(x => myIds.Any(y => y == x.UserOrgId)).NotNull(x => x.Id);
			var supportTeam = support.ToSelectList(x => x.User.GetName(), x => x.Id);

			var campaigns = ApplicationAccessor.GetCampaigns(GetUser(), true).OrderBy(x => x.Name).ToSelectList(x => x.Name, x => x.Name).ToList();
			campaigns.Insert(0, new SelectListItem() { Text = "n/a", Value = null });
			ViewBag.Campaigns = campaigns;

			if (!supportTeam.Any()) {
				supportTeam.Add(new SelectListItem() {
					Text = "Me",
					Value = "" + GetUser().Id
				});
			}
			ViewBag.SupportTeam = supportTeam;
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Create(int? count, bool generate = false) {
			var user = GetUserModel();
			PrepareCreateOrgViewBag();
			var ocd = new OrgCreationData() {
				AssignedTo = ViewBag.MySupportId,
				CoachId = -1
			};
			if (generate) {
				var name = RandomWordUtility.GenerateName();
				ocd.ContactEmail = name.Item1.ToLower() + "." + name.Item2.ToLower() + "@email.com";
				ocd.ContactFN = name.Item1;
				ocd.ContactLN = name.Item2;
				ocd.Name = RandomWordUtility.GenerateCompanyName();
			}
			return View(ocd);
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> Create(OrgCreationData data) {// String name, bool enableL10, bool enableReview, bool enableAC, string contactFN, string contactLN, string contactEmail,long implementer, ) {
			var user = GetUserModel();
			string planType = "professional";

			if (data.AssignedTo == null) {
				ModelState.AddModelError("AssignedTo", "Organization must be assigned to someone in customer support.");
			}

			if (string.IsNullOrWhiteSpace(data.ContactEmail)) {
				ModelState.AddModelError("ContactEmail", "Please select an email for the primary contact");
			}

			if (string.IsNullOrWhiteSpace(data.ContactFN)) {
				ModelState.AddModelError("ContactFN", "Please select a first name for the primary contact");
			}

			if (string.IsNullOrWhiteSpace(data.ContactLN)) {
				ModelState.AddModelError("ContactLN", "Please select a last name for the primary contact");
			}

			if (ModelState.IsValid) {
				var paymentPlanType = PaymentAccessor.GetPlanType(planType);
				var result = await _OrganizationAccessor.CreateOrganization(user, paymentPlanType, DateTime.UtcNow, data);
				var uOrg = result.NewUser;

				if (data.AccountType == AccountType.Implementer || data.AccountType == AccountType.Coach) {
					ApplicationAccessor.EditCoach(GetUser(), new Models.Application.Coach() {
						CoachType = data.AccountType == AccountType.Implementer ? CoachType.CertifiedOrProfessionalEOSi : CoachType.BusinessCoach,
						Name = data.ContactFN + " " + data.ContactLN,
						Email = data.ContactEmail,
						UserOrgId = uOrg.Id,
					});
				}

				return RedirectToAction("SetRole", "Account", new { id = uOrg.Id });
			}

			PrepareCreateOrgViewBag();
			return View(data);
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Products() {
			PermissionsAccessor.Permitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));
			return View(GetUser().Organization);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Products(OrganizationModel model) {
			if (ModelState.IsValid) {
				await _OrganizationAccessor.UpdateProducts(GetUser(),
					model.Settings.EnableReview,
					model.Settings.EnableL10,
					model.Settings.EnableSurvey,
					model.Settings.EnablePeople,
					model.Settings.EnableCoreProcess,
					model.Settings.EnableZapier,
					model.Settings.Branding
				);
				return RedirectToAction("Index", "Manage");
			}
			return View(GetUser().Organization);
		}

		[Access(AccessLevel.Any)]
		public async Task<ActionResult> Join(String id = null) {
			if (String.IsNullOrWhiteSpace(id)) {
				throw new PermissionsException("Id cannot be empty.");
			}

			var nexus = _NexusAccessor.Get(id);
			if (nexus.DateExecuted != null) {
				throw new RedirectException(ExceptionStrings.AlreadyMember);
			}

			var user = GetUserModel();

			//new Cache().Invalidate(AlertHub.REGISTERED_KEY + user.UserName);

			var orgId = int.Parse(nexus.GetArgs()[0]);
			var placeholderUserId = long.Parse(nexus.GetArgs()[2]);
			if (user == null) {
				return RedirectToAction("Login", "Account", new { returnUrl = "Organization/Join/" + id });
			}

			try {
				var userOrg = GetUser(placeholderUserId);
				if (!user.IsRadialAdmin) {
					throw new RedirectException(ExceptionStrings.AlreadyMember);
				} else {
					throw new OrganizationIdException();
				}
			} catch (OrganizationIdException) {
				//We want to hit this exception.
				new Cache().Invalidate(CacheKeys.ORGANIZATION_ID);
				//Session["OrganizationId"] = null;
				var org = await OrganizationAccessor.JoinOrganization(user, nexus.ByUserId, placeholderUserId);
				_NexusAccessor.Execute(nexus);
				return RedirectToAction("Index", "Home", new { message = String.Format(MessageStrings.SuccessfullyJoinedOrganization, org.Organization.Name) });
			}
		}


		/*
		[Access(AccessLevel.UserOrganization)]
		public ActionResult ManageList()
		{
		}*/

		/*
		public ActionResult Manage(int? organizationId)
		{
			if (organizationId == null)
			{
				var userOrgs = GetUserOrganizations();
				var managing = userOrgs.Where(x => x.IsManager());
				var count = managing.Count();
				if (count == 0)
					throw new PermissionsException();
				else if (count == 1)
					return RedirectToAction("Manage", new { organizationId = managing.First().Organization.Id });
				else
					return View("ManageList", managing.Select(x => x.Organization).ToList());
			}
			else
			{
				var userOrg = GetOneUserOrganization(organizationId.Value)
					.Hydrate()
					.ManagingGroups(questions:true)
					.ManagingUsers(subordinates:true)
					.Organization(questions:true)
					.Reviews(answers:true)
					.Nexus()
					.Execute();

				if (userOrg == null)
					throw new PermissionsException();

				if (!userOrg.IsManager())
					throw new PermissionsException();
                
				return View(new ManageViewModel(userOrg));
			}
		}*/

		[Access(AccessLevel.Radial)]
		public ActionResult Begin(int? count = null) {
			ViewBag.Count = count;
			int[] roundUp = new int[] { 10, 15, 25, 50, 100, 500 };
			double[] prices = new double[] { 0, 199, 499, 999, 1999, 3999, Double.MaxValue };

			if (count != null) {
				ViewBag.Price = prices[0];
				for (int i = 0; i < roundUp.Length; i++) {
					if (count > roundUp[i]) {
						ViewBag.Price = prices[i + 1];
					}
				}
			}
			return View();
		}

		[Access(AccessLevel.Any)]
		public ActionResult Redirect(int organizationId, string returnUrl) {
			if (returnUrl.Contains("?")) {
				return RedirectToLocal(returnUrl + "&setUserOrganizationId=" + organizationId);
			} else {
				return RedirectToLocal(returnUrl + "?setUserOrganizationId=" + organizationId);
			}
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Tree(string type = "cartesian") {
			if (type.ToLower() == "radial") {
				return View("RadialTree", GetUser().Organization.Id);
			} else if (type.ToLower() == "forcedirected") {
				return View("ForceDirected", GetUser().Organization.Id);
			} else {
				return View("Tree", GetUser().Organization.Id);
			}
		}

		[Access(AccessLevel.Manager)]
		public ActionResult ResendJoin(long id) {
			var found = UserAccessor.GetUserOrganization(GetUser(), id, true, false, PermissionType.EditEmployeeDetails);

			if (found.TempUser == null) {
				throw new PermissionsException("User is already a part of the organization");
			}

			if (found.IsPlaceholder) {
				if (!PermissionsAccessor.IsPermitted(GetUser(), x => x.CanUpgradeUsersAtOrganization(found.Organization.Id))) {
					return Content("You're not permitted to upgrade placeholders to paid accounts.");
				}
			}

			return PartialView(found.TempUser);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult ResendJoinEmailManual(long id) {
			var found = UserAccessor.GetUserOrganization(GetUser(), id, true, false, PermissionType.EditEmployeeDetails);

			if (found.TempUser == null) {
				throw new PermissionsException("User is already a part of the organization");
			}

			if (found.IsPlaceholder) {
				if (!PermissionsAccessor.IsPermitted(GetUser(), x => x.CanUpgradeUsersAtOrganization(found.Organization.Id))) {
					return Content("You're not permitted to upgrade placeholders to paid accounts.");
				}
			}

			//var id = found.TempUser.Guid;
			var url = "Account/Register?returnUrl=%2FOrganization%2FJoin%2F" + found.TempUser.Guid + "&utm_source=tt_welcome_email&utm_medium=1st_link";
			url = Config.BaseUrl(GetUser().Organization) + url;
			var productName = Config.ProductName(GetUser().Organization);
			var getTemplate = VariableAccessor.Get(Variable.Names.JOIN_ORGANIZATION_UNDER_MANAGER_BODY, null);
			//found.TempUser.EmailTemplate = String.Format(EmailStrings.JoinOrganizationUnderManager_Body, new String[] { found.TempUser.FirstName, GetUser().Organization.Name.Translate(), url, url, productName, found.TempUser.Guid.ToUpper() });
			found.TempUser.EmailTemplate = String.Format(getTemplate, new String[] { found.TempUser.FirstName, GetUser().Organization.Name.Translate(), url, url, null });


			return PartialView(found.TempUser);
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public async Task<JsonResult> ResendJoin(long id, TempUserModel model, long TempId, bool resendEmail) {
			var found = UserAccessor.GetUserOrganization(GetUser(), id, true, false, PermissionType.EditEmployeeDetails);
			if (found.TempUser == null) {
				throw new PermissionsException("User is already a part of the organization");
			}

			if (found.IsPlaceholder) {
				if (!PermissionsAccessor.IsPermitted(GetUser(), x => x.CanUpgradeUsersAtOrganization(found.Organization.Id))) {
					return Json(ResultObject.CreateError("You're not permitted to upgrade placeholders to paid accounts."));
				}
			}

			if (found.IsPlaceholder) {
				await UserAccessor.RemoveRole(GetUser(), found.Id, UserRoleType.PlaceholderOnly);
			}


			_UserAccessor.UpdateTempUser(GetUser(), id, model.FirstName, model.LastName, model.Email, DateTime.UtcNow);
			model.Id = TempId;
			if (resendEmail) {
				var result = await Emailer.SendEmail(JoinOrganizationAccessor.CreateJoinEmailToGuid(GetUser(), model));
				var prefix = "Resent";
				if (model.LastSent == null) {
					prefix = "Sent";
				}

				var o = result.ToResults(prefix + " invite to " + model.Name() + ".");
				o.Object = model;

				return Json(o);
			} else {
				return Json(ResultObject.SilentSuccess(model));
			}
		}


		#region Uploads
		public class UploadVM {
			public long OrganizationId { get; set; }
			public HttpPostedFileBase File { get; set; }
			public Guid Guid { get; set; }

		}

		[Access(AccessLevel.Manager)]
		[HttpGet]
		public ActionResult Upload() {
			var orgId = GetUser().Organization.Id;

			return View(new UploadVM() {
				OrganizationId = orgId,
			});
		}

		public static Dictionary<Guid, String> CSVs = new Dictionary<Guid, String>();

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public ActionResult Upload(UploadVM model) {
			var file = model.File;

			if (file != null && file.ContentLength > 0) {
				var guid = Guid.NewGuid();
				CSVs[guid] = file.InputStream.ReadToEnd();
				return RedirectToAction("Fields", new { id = guid.ToString() });
			}
			ViewBag.Message = "An error has occurred.";
			return RedirectToAction("Upload");
		}

		public class FieldsVM {
			public Guid Guid { get; set; }
			public string[] Fields { get; set; }
			public String FirstNameColumn { get; set; }
			public String LastNameColumn { get; set; }
			public String EmailColumn { get; set; }
			public String PositionColumn { get; set; }
			public String ManagerFirstNameColumn { get; set; }
			public String ManagerLastNameColumn { get; set; }

		}

		[Access(AccessLevel.Manager)]
		public ActionResult Fields(string id = null) {
			try {
				var guid = Guid.Parse(id);

				var data = CSVs[guid];
				using (var sr = new StreamReader(data.ToStream())) {
					var csv = new CsvReader(sr);
					while (csv.Read()) { }

					var headers = csv.FieldHeaders.Select((x, i) => {
						if (String.IsNullOrWhiteSpace(x)) {
							return "(Column " + i.ToLetter() + ")";
						}

						return x;
					}).ToList();

					var model = new FieldsVM() {
						Guid = guid,
						Fields = headers.ToArray(),
					};

					return View(model);
				}

			} catch (Exception) {
				ViewBag.Message = "An error has occurred.";
				return RedirectToAction("Upload");
			}
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public async Task<ActionResult> Fields(FieldsVM model) {
			var nexus = new NexusController();

#pragma warning disable CS0618 // Type or member is obsolete
			var existingPositions = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);
#pragma warning restore CS0618 // Type or member is obsolete
			var existingUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			var pos = _PositionAccessor.AllPositions().First();

			var data = CSVs[model.Guid];

			var managerLookup = new Dictionary<long, string[]>();

			var errors = new CounterSet<String>();

			using (var sr = new StreamReader(data.ToStream())) {
				var csv = new CsvReader(sr);
				while (csv.Read()) {
					var email = csv.GetField(model.EmailColumn).Trim();
					var firstName = csv.GetField(model.FirstNameColumn).Trim();
					var lastName = csv.GetField(model.LastNameColumn).Trim();
					var position = csv.GetField(model.PositionColumn).Trim();
					var managerFirst = csv.GetField(model.ManagerFirstNameColumn).Trim();
					var managerLast = csv.GetField(model.ManagerLastNameColumn).Trim();

					if ((new[] { email, firstName, lastName, position, managerFirst, managerLast }.All(string.IsNullOrWhiteSpace))) {
						//Empty row
						continue;
					}

					var positionFound = existingPositions.FirstOrDefault(x => x.CustomName == position);

					if (positionFound == null && !String.IsNullOrWhiteSpace(position)) {
						var newPosition = await _OrganizationAccessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, /*pos.Id,*/ position);
						existingPositions.Add(newPosition);
						positionFound = newPosition;
					}

					var vm = new CreateUserOrganizationViewModel() {
						Email = email,
						FirstName = firstName,
						LastName = lastName,
						OrgId = GetUser().Organization.Id,
						ManagerNodeId = null,
						Position = new UserPositionViewModel() {
							CustomPosition = null,
							PositionId = positionFound != null ? positionFound.Id : -2
						},
					};
					try {
						var user = (await _UserAccessor.CreateUser(GetUser(), vm)).Item2;

						existingUsers.Add(user);
						managerLookup.Add(user.Id, new[] { managerFirst, managerLast });
					} catch (PermissionsException e) {
						errors.Add(e.Message);
					} catch (Exception) {
						errors.Add("An error has occurred.");
					}
				}
			}


			AccountabilityAccessor._FinishUploadAccountabilityChart(GetUser(), existingUsers, managerLookup, errors);

			ViewBag.Message = String.Join("\n", errors.Select(x => x.Key));
			return RedirectToAction("Members", "Manage");

		}
		#endregion





	}
}