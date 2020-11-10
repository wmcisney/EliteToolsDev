using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Controllers.AbstractController;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Events;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.Notifications;
using RadialReview.Models.Onboard;
using RadialReview.Models.Reviews;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Notifications;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Constants;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Encrypt;
using RadialReview.Utilities.Integrations;
using RadialReview.Utilities.Pdf;
using RadialReview.Utilities.Productivity;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static RadialReview.Accessors.AdminAccessor;

namespace RadialReview.Controllers {

	public class AdminController : BaseExpensiveController {

		#region Implementers
		[Access(AccessLevel.Radial)]
		public ActionResult Implementers() {
			return View(ApplicationAccessor.GetCoaches(GetUser()));
		}
		[Access(AccessLevel.Radial)]
		public PartialViewResult EditImplementers(long id = 0) {
			return PartialView(ApplicationAccessor.GetCoach(GetUser(), id));
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult EditImplementers(Coach model) {
			ApplicationAccessor.EditCoach(GetUser(), model);
			return Json(ResultObject.SilentSuccess(model));
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult DeleteImplementers(long id) {
			var model = ApplicationAccessor.GetCoach(GetUser(), id);
			model.DeleteTime = DateTime.UtcNow;
			ApplicationAccessor.EditCoach(GetUser(), model);
			return Json(ResultObject.SilentSuccess(model));
		}
		[Access(AccessLevel.Radial)]
		public ActionResult SupportMembers() {
			return View(ApplicationAccessor.GetSupportMembers(GetUser()));
		}
		[Access(AccessLevel.Radial)]
		public PartialViewResult EditSupportMember(long id = 0) {
			return PartialView(ApplicationAccessor.GetSupportMember(GetUser(), id));
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult EditSupportMember(SupportMember model) {
			ApplicationAccessor.EditSupportMember(GetUser(), model);
			return Json(ResultObject.SilentSuccess(model));
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult DeleteSupportMember(long id) {
			var model = ApplicationAccessor.GetSupportMember(GetUser(), id);
			model.DeleteTime = DateTime.UtcNow;
			ApplicationAccessor.EditSupportMember(GetUser(), model);
			return Json(ResultObject.SilentSuccess(model));
		}
		[Access(AccessLevel.Radial)]
		public ActionResult Campaigns() {
			return View(ApplicationAccessor.GetCampaigns(GetUser(), false));
		}
		[Access(AccessLevel.Radial)]
		public PartialViewResult EditCampaign(long id = 0) {
			return PartialView(ApplicationAccessor.GetCampaign(GetUser(), id));
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult EditCampaign(Campaign model) {
			ApplicationAccessor.EditCampaign(GetUser(), model);
			return Json(ResultObject.SilentSuccess(model));
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult DeleteCampaign(long id) {
			var model = ApplicationAccessor.GetCampaign(GetUser(), id);
			model.DeleteTime = DateTime.UtcNow;
			ApplicationAccessor.EditCampaign(GetUser(), model);
			return Json(ResultObject.SilentSuccess(model));
		}
		#endregion

		#region Metadata
		[Access(AccessLevel.Radial)]
		public async Task<string> Name() {
			return HttpContext.Server.MachineName;
		}

		[Access(AccessLevel.Radial)]
		public async Task<string> Hangfirecs() {
			return Config.GetHangfireConnectionString();
		}

		[Access(AccessLevel.Radial)]
		public ActionResult DbTime() {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return Content("DbTimestamp:" + HibernateSession.GetDbTime(s));
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult DbIdentifier() {
			return Content(KeyManager.ProductionDatabaseCredentials.DatabaseIdentifier);
		}
		#endregion

		#region Data
		[Access(AccessLevel.Radial)]
		public ActionResult Signups(int days = 14) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var notRecent = DateTime.UtcNow.AddDays(-days);
					//var  = DateTime.UtcNow.AddDays(-1);
					var users = s.QueryOver<OnboardingUser>().OrderBy(x => x.StartTime).Desc.Where(x => x.StartTime > notRecent).List().ToList();
					return View(users);
				}
			}
		}
		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> AccountsAtRisk(int days = 60, decimal growth = -.1m, AccountType type = AccountType.Paying,
			int lastLoginDays = 3, int lastScoreDays = 3) {
			var start = DateTime.UtcNow.AddDays(-days);
			var end = DateTime.UtcNow;
			var stats = StatsAccessor.GetSuperAdminStatistics_Unsafe(start, end);

			var range = stats.Where(x =>
				(x.LastLogin != null && x.LastLogin < DateTime.UtcNow.AddDays(-lastLoginDays)) ||
				(x.LastScoreUpdate != null && x.LastScoreUpdate < DateTime.UtcNow.AddDays(-lastScoreDays)) ||
				(x.Registrations != null && x.Registrations.PercentageFromWindowMax.GetValue(2) < 1m + growth)
			).ToList();

			range = range.Where(x => x.AccountType == type).ToList();

			ViewBag.Start = start;
			ViewBag.End = end;
			ViewBag.AccountType = type;

			return View(range);
		}

		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> EmployeeCount(CancellationToken token, Divisor divisor = null) {
			divisor = divisor ?? new Divisor();
			ViewBag.Divisor = divisor;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					LocalizedStringModel alias = null;
					var orgIds = s.QueryOver<OrganizationModel>()
									.JoinAlias(x => x.Name, () => alias)
									.Where(x => x.AccountType != AccountType.Cancelled)
									.Where(Mod<OrganizationModel>(x => x.Id, divisor))
									.Select(x => x.Id, x => alias.Standard)
									.List<object[]>()
									.Select(x => new { Id = (long)x[0], Name = (string)x[1] })
									.ToList();
					var burndowns = orgIds.Select(i => {
						return new EmpCount {
							Id = i.Id,
							Name = i.Name,
							chart = StatsAccessor.GetOrganizationMemberBurndown(s, PermissionsUtility.CreateAdmin(s), i.Id)
						};
					}).ToList();

					return View(burndowns);
				}
			}
			// return View();
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Meetings(int minutes = 120) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recent = DateTime.UtcNow.AddMinutes(-minutes);
					var notRecent = DateTime.UtcNow.AddDays(-1);
					var measurables = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && (x.CompleteTime == null || x.CompleteTime >= recent) && x.CreateTime > notRecent).List().ToList();
					return View(measurables);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult UserInfo(long id = 0) {
#pragma warning disable CS0618 // Type or member is obsolete
			return View(_UserAccessor.GetUserOrganizationUnsafe(id));
#pragma warning restore CS0618 // Type or member is obsolete
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Events(int days = 30, long? orgId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var evtsQ = s.QueryOver<AccountEvent>().Where(x => x.DeleteTime == null && x.CreateTime > DateTime.UtcNow.AddDays(-days));
					if (orgId != null) {
						evtsQ = evtsQ.Where(x => x.OrgId == orgId.Value);
						ViewBag.FixSidebar = false;
					}

					var evts = evtsQ.List().ToList();
					var org = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(evts.Select(x => x.OrgId).ToArray()).List().ToList();
					ViewBag.OrgLookup = new DefaultDictionary<long?, string>(x => org.FirstOrDefault(y => y.Id == x).NotNull(y => y.GetName()) ?? "" + x);
					ViewBag.OrgStatusLookup = new DefaultDictionary<long?, AccountType>(x => org.FirstOrDefault(y => y.Id == x).NotNull(y => (AccountType?)y.AccountType) ?? AccountType.Invalid);
					return View(evts);
				}
			}
		}
		[Access(AccessLevel.Radial)]
		public ActionResult EventsCsv(int days = 30, long? orgId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var evtsQ = s.QueryOver<AccountEvent>().Where(x => x.DeleteTime == null && x.CreateTime > DateTime.UtcNow.AddDays(-days));
					if (orgId != null) {
						evtsQ = evtsQ.Where(x => x.OrgId == orgId.Value);
						ViewBag.FixSidebar = false;
					}

					var evts = evtsQ.List().ToList();
					var org = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(evts.Select(x => x.OrgId).ToArray()).List().ToList();
					var OrgLookup = new DefaultDictionary<long?, string>(x => org.FirstOrDefault(y => y.Id == x).NotNull(y => y.GetName()) ?? "" + x);
					var OrgStatusLookup = new DefaultDictionary<long?, AccountType>(x => org.FirstOrDefault(y => y.Id == x).NotNull(y => (AccountType?)y.AccountType) ?? AccountType.Invalid);

					var csv = new Csv();
					foreach (var evt in evts) {
						csv.Add("" + evt.Id, "eid", "" + evt.Id);
						csv.Add("" + evt.Id, "CreateTime", "" + evt.CreateTime);
						csv.Add("" + evt.Id, "OrgId", "" + evt.OrgId);
						csv.Add("" + evt.Id, "Org", "" + OrgLookup[evt.OrgId]);
						csv.Add("" + evt.Id, "Status", "" + OrgStatusLookup[evt.OrgId]);
						csv.Add("" + evt.Id, "Type", "" + evt.Type.Kind());
						csv.Add("" + evt.Id, "Duration", "" + evt.Type.Duration());
						csv.Add("" + evt.Id, "ByUser", "" + evt.TriggeredBy);
						csv.Add("" + evt.Id, "Arg1", "" + evt.Argument1);
					}

					return File(csv.ToBytes(), "text/csv", DateTime.UtcNow.ToJavascriptMilliseconds() + "_Events" + orgId.NotNull(x => "_" + x) + ".csv");
				}
			}
		}
		[Access(AccessLevel.Radial)]
		public ActionResult MeetingsTable(int weeks = 3) {
			ViewBag.Weeks = weeks;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recent = DateTime.UtcNow.AddDays(-weeks * 7);
					var notRecent = DateTime.UtcNow.AddDays(-weeks * 7 - 1);
					var measurables = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && (x.CompleteTime == null || x.CompleteTime >= recent) && x.CreateTime > notRecent)
						.List().ToList()
						.Where(x => x.Organization.AccountType != AccountType.SwanServices)
						.ToList();
					return View(measurables);
				}
			}
		}

		public class EmpCount {
			public long Id { get; set; }
			public MetricGraphic chart { get; set; }
			public string Name { get; set; }
		}

		public class ErrorResult {
			public List<ErrorLog> Logs { get; set; }
			public List<KeyValuePair<string, int>> CountByType { get; set; }
			public List<KeyValuePair<string, int>> CountByUser { get; set; }
			public List<KeyValuePair<string, int>> CountByPath { get; set; }
			public List<KeyValuePair<string, int>> CountByMessage { get; set; }
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Errors(long days = 7, int limit = 10) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var errs = s.QueryOver<ErrorLog>()
								.Where(x => x.DeleteTime == null && x.CreateTime > DateTime.UtcNow.AddDays(-days))
								.List().ToList();

					var res = new ErrorResult() {
						Logs = errs,
						CountByMessage = errs.GroupBy(x => x.Message).Select(x => new KeyValuePair<string, int>(x.Key, x.Count())).OrderByDescending(x => x.Value).Take(limit).ToList(),
						CountByPath = errs.GroupBy(x => x.Path).Select(x => new KeyValuePair<string, int>(x.Key, x.Count())).OrderByDescending(x => x.Value).Take(limit).ToList(),
						CountByUser = errs.GroupBy(x => x.UserId).Select(x => new KeyValuePair<string, int>(x.Key, x.Count())).OrderByDescending(x => x.Value).Take(limit).ToList(),
						CountByType = errs.GroupBy(x => x.ExceptionType).Select(x => new KeyValuePair<string, int>(x.Key, x.Count())).OrderByDescending(x => x.Value).Take(limit).ToList(),
					};
					return View(res);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Error(string id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var errs = s.Get<ErrorLog>(Guid.Parse(id));

					var res = new ErrorResult() {
						Logs = errs.AsList(),
					};
					return View("Errors", errs);
				}
			}
		}


		[Access(AccessLevel.Radial)]
		public ActionResult AllUsers(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);

					var allUsers = s.QueryOver<UserLookup>().Where(x => x.OrganizationId == id).List().ToList();

					var items = allUsers.Select(x => {
						return new AllUserEmail() {
							UserName = x.Name,
							UserEmail = x.Email,
							UserId = x.UserId,
							OrgId = x.OrganizationId,
							OrgName = org.GetName(),
							AccountType = "" + org.NotNull(y => y.AccountType),
							OrgCreateTime = org.CreationTime,
							UserCreateTime = x.CreateTime

						};
					}).ToList();

					var csv = new Csv();
					csv.Title = "UserId";
					foreach (var o in items) {
						csv.Add("" + o.UserId, "UserName", o.UserName);
						csv.Add("" + o.UserId, "UserEmail", o.UserEmail);
						csv.Add("" + o.UserId, "OrgName", o.OrgName);
						csv.Add("" + o.UserId, "UserId", "" + o.UserId);
						csv.Add("" + o.UserId, "OrgId", "" + o.OrgId);
						csv.Add("" + o.UserId, "UserCreateTime", "" + o.UserCreateTime);
						csv.Add("" + o.UserId, "AccountType", o.AccountType);
						csv.Add("" + o.UserId, "OrgCreateTime", "" + o.OrgCreateTime);
					}

					return File(csv.ToBytes(), "text/csv", DateTime.UtcNow.ToJavascriptMilliseconds() + "_AllUsers_" + org.GetName() + ".csv");
				}
			}
		}

		[Access(AccessLevel.RadialData)]
		public ActionResult AllDeleted() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					UserModel userAlias = null;
					TempUserModel tempUserAlias = null;
					OrganizationModel orgAlias = null;

					var allDeleted = s.QueryOver<UserOrganizationModel>()
						.Left.JoinAlias(x => x.User, () => userAlias)
						.Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
						.Left.JoinAlias(x => x.Organization, () => orgAlias)
						.Where(x => x.DeleteTime != null || orgAlias.DeleteTime != null)
							.Select(x => x.Id, x => x.DeleteTime, x => userAlias.UserName, x => tempUserAlias.Email, x => orgAlias.DeleteTime)
						.List<object[]>().ToList();

					var csv = new Csv();
					csv.Title = "UserId";
					foreach (var d in allDeleted) {
						var userName = (string)d[2];
						if (userName == null) {
							userName = (string)d[3];
						}

						csv.Add("" + (long)d[0], "UserEmail", "" + userName);

						var deleteTime = (DateTime?)d[1];
						if (deleteTime == null) {
							deleteTime = (DateTime?)d[4];
						}

						csv.Add("" + (long)d[0], "DeleteTime", "" + deleteTime);
					}

					return File(csv.ToBytes(), "text/csv", DateTime.UtcNow.ToJavascriptMilliseconds() + "_AllDeletedUsers.csv");
				}
			}
		}
		#endregion

		#region Test Actions
		[Access(AccessLevel.Radial)]
		[AsyncTimeout(5000)]
		public async Task<ActionResult> Wait(CancellationToken ct, int seconds = 10, int timeout = 5) {
			await Task.Delay((int)(seconds * 1000));
			return Content("done " + DateTime.UtcNow.ToJsMs());
		}

		[Access(AccessLevel.Radial)]
		public async Task<bool> DeleteNotification(long id, DateTime? expires = null) {
			await NotificationAccessor.DeleteNotification_Unsafe(id, expires);
			return true;
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> Notify(long userId, string message, string groupKey, bool canMarkSeen = true, DateTime? expires = null, string details = null, bool sensitive = true, string imageUrl = null, bool onPhone = false, bool onComputer = false, string actionUrl = null) {

			return await Notify(new NotifyModel() {
				canMarkSeen = canMarkSeen,
				actionUrl = actionUrl,
				details = details,
				groupKey = groupKey,
				message = message,
				expires = expires,
				imageUrl = imageUrl,
				onComputer = onComputer,
				onPhone = onPhone,
				userId = userId

			});
		}

		public class NotifyModel {
			public long userId { get; set; }
			[AllowHtml]
			public string message { get; set; }
			[AllowHtml]
			public string details { get; set; }
			public string imageUrl { get; set; }
			public bool onPhone { get; set; }
			public bool onComputer { get; set; }
			public string groupKey { get; set; }
			public DateTime? expires { get; set; }
			public string actionUrl { get; set; }
			public bool canMarkSeen { get; set; }

			public NotifyModel() {
			}
		}

		[Access(AccessLevel.Radial)]
		[HttpPost]
		public async Task<JsonResult> Notify(NotifyModel model) {
			NotificationDevices dev = (model.onPhone ? NotificationDevices.Phone : NotificationDevices.None) | (model.onComputer ? NotificationDevices.Computer : NotificationDevices.None);

			if (string.IsNullOrWhiteSpace(model.message)) {
				throw new Exception("Message empty");
			}

			if (string.IsNullOrWhiteSpace(model.groupKey)) {
				throw new Exception("groupKey empty. Notifications need a message-unique groupKey. GroupKeys are useful when you want to delete notifications with the same groupKey.");
			}

			await NotificationAccessor.FireNotification_Unsafe(NotificationGroupKey.FromString(model.groupKey), model.userId, dev, model.message, model.details, model.expires, model.actionUrl, model.canMarkSeen);

			return Json(model, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> DeleteNotificationGroup(string group) {
			if (group == null) {
				return Json(new { error = "please specify ?group=..." }, JsonRequestBehavior.AllowGet);
			}
			var count = await NotificationAccessor.DeleteGroupKey_Unsafe(group);
			return Json(new { deleted = count }, JsonRequestBehavior.AllowGet);
		}

		[HttpGet]
		[Access(AccessLevel.Radial)]
		public ActionResult Decrypt(string message, string shared) {
			return Content(Crypto.DecryptStringAES(message, ERROR_CODE_SHARED));
		}

		[HttpGet]
		[Access(AccessLevel.Radial)]
		public ActionResult AddLog(string message = "test-log-message") {
			log.Info(message);
			return Content("Added message to log:<br/>" + message);
		}


		#endregion

		#region Actions

		#region Client Success

		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> ResetRole() {
			var user = GetUser();

			if (user.User != null) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var u = s.Get<UserModel>(user.User.Id);
						if (u.UserOrganizationIds != null) {
							var ids = u.UserOrganizationIds.Where(x => x > 0).ToList();
							if (ids.Any()) {
								u.CurrentRole = ids.First();
								s.Update(u);
								tx.Commit();
								s.Flush();
							} else {
								return Content("No user organizations.");
							}
						} else {
							return Content("User organization ids was null.");
						}
					}
				}
			}
			return RedirectToAction("Index", "Home");
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> ResetDemo(long recurId = 1) {

			if (GetUser().Id == 600 || GetUser().IsRadialAdmin || GetUser().User.IsRadialAdmin) {
				//fall through
			} else {
				throw new PermissionsException();
			}

			var issues = new[] { "Board meeting location?", "Bonus allocation", "Equipment leases (current?)", "Sales department working remotely", "sales department morale" };
			var todos = new[] { "Call Vendors re: outstanding issues", "'Turnover' was not entered.", "call that speech writer back -- 'AthleticBusiness keynote speech'",
						"Send HR review documents for my team", "call back that canidant -- 'Shipping Errors' goal was missed by 8",
						"Send Lindsey data in prep for Board meeting",
						"Prepare meeting agenda for upcoming Board meeting", "Provide job description to HR for new EA", "Pass fitness room lead to sales for follow up",
						"Call StorEdge re: SEO & loop in Sales Team", "Send project update to Sales Team re: new software", "schedule time ......'AthleticBusiness keynote speech'",
						 };


			//var recurId = 1;
			var recur = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), recurId);
			var possibleUsers = recur.Attendees.Select(x => x.Id).ToList();
			possibleUsers.Add(600);

			var addedTodos = 0;
			var addedIssues = 0;
			var addedScores = 0;
			var deletedTodos = 0;
			var deletedIssues = 0;
			var deletedScores = 0;
			var deletedHeadlines = 0;

			DateTime start = DateTime.UtcNow;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recur1 = s.Get<L10Recurrence>(recurId);

					if (recur1.OrganizationId != 592) {
						throw new Exception("Cannot edit meetings outside of Gart Sports");
					}

					var caller = s.Get<UserOrganizationModel>(600L);
					var perms = PermissionsUtility.Create(s, caller);

					var r = new Random(22586113);

					foreach (var at in recur.Todos.Where(x => !x.Complete.Value)) {
						var todo = s.Load<TodoModel>(at.Id);
						todo.CompleteTime = DateTime.MinValue;
						s.Update(todo);
						deletedTodos += 1;
					}
					var createTime = DateTime.UtcNow.AddDays(-5);
					foreach (var todo in todos) {
						var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;

						var todoC = TodoCreation.GenerateL10Todo(recurId, todo, null, possibleUsers[r.Next(possibleUsers.Count - 1)], DateTime.UtcNow.AddDays(r.Next(1, 2)), now: createTime);
						await TodoAccessor.CreateTodo(s, perms, todoC);


						//await TodoAccessor.CreateTodo(s, perms, recurId, new Models.Todo.TodoModel {
						//	AccountableUserId = possibleUsers[r.Next(possibleUsers.Count - 1)],
						//	Message = todo,
						//	ForRecurrenceId = recurId,
						//	DueDate = DateTime.UtcNow.AddDays(r.Next(1, 2)),
						//	CompleteTime = complete,
						//	CreateTime = createTime,
						//	OrganizationId = caller.Organization.Id,
						//});
						createTime = createTime.AddMinutes(r.Next(3, 8));
						addedTodos += 1;
					}


					foreach (var at in recur.IssuesList.Issues.Where(x => !x.Complete.Value)) {
						var issue = s.Load<IssueModel.IssueModel_Recurrence>(at.Id);
						issue.CloseTime = DateTime.MinValue;
						s.Update(issue);
						deletedIssues += 1;
					}


					foreach (var h in recur.Headlines) {
						var headline = s.Load<PeopleHeadline>(h.Id);
						headline.CloseTime = DateTime.MinValue;
						s.Update(headline);
						deletedHeadlines += 1;
					}

					createTime = DateTime.UtcNow.AddDays(-5);
					foreach (var issue in issues) {
						//var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						var owner = possibleUsers[r.Next(possibleUsers.Count - 1)];
						var creation = IssueCreation.CreateL10Issue(issue, null, owner, recurId, now: createTime);
						await IssuesAccessor.CreateIssue(s, perms, creation);
						createTime = createTime.AddMinutes(r.Next(5, 15));
						addedIssues += 1;
					}


					var headlines = new[] {
								new {Message ="Just had a baby", AboutId = (long?)604, AboutName = "Irene Bunn", About=(ResponsibilityGroupModel)s.Load<UserOrganizationModel>(604L), Details="Her baby was 17lbs!!! she broke the state record!!" },
								new {Message ="Congratulations on retirement", AboutId = (long?)615, AboutName = "Don Barber", About=(ResponsibilityGroupModel)s.Load<UserOrganizationModel>(615L), Details=(string)null},
								new {Message ="Supplier just raised shipping rates", AboutId = (long?)null, AboutName = "Maurice Sporting Goods", About=(ResponsibilityGroupModel)null, Details=(string)null},
								new {Message ="Team pulled together after a customer shipment was lost", AboutId = (long?)644, AboutName = "Fulfillment Team", About=(ResponsibilityGroupModel)s.Load<OrganizationTeamModel>(644L), Details=(string)null}
							};


					foreach (var h in headlines) {
						//var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						var owner = possibleUsers[r.Next(possibleUsers.Count - 1)];
						await HeadlineAccessor.CreateHeadline(s, perms, new PeopleHeadline {
							Message = h.Message,
							AboutId = h.AboutId,
							AboutName = h.AboutName,
							OwnerId = owner,
							RecurrenceId = recurId,
							About = h.About,
							Owner = s.Load<UserOrganizationModel>(owner),
							_Details = h.Details,

							OrganizationId = caller.Organization.Id,
							CreateTime = createTime,
						});
						createTime = createTime.AddMinutes(r.Next(5, 15));
						addedIssues += 1;
					}



					var current = recur.Scorecard.Weeks.FirstOrDefault(x => x.IsCurrentWeek).ForWeekNumber;
					var emptyMeasurable = recur.Scorecard.Measurables.ElementAtOrDefault(r.Next(recur.Scorecard.Measurables.Count() - 1)).NotNull(x => x.Id);

					foreach (var angScore in recur.Scorecard.Scores.Where(x => x.ForWeek > current - 13)) {
						if (angScore.Id > 0) {
							if (angScore.ForWeek == current && angScore.Measurable.Id == emptyMeasurable) {
								var score = s.Load<ScoreModel>(angScore.Id);
								score.Measured = null;
								s.Update(score);
								deletedScores += 0;

							} else if (angScore.Measured == null) {
								var score = s.Load<ScoreModel>(angScore.Id);
								double stdDev = (double)(angScore.Measurable.Target.Value * (angScore.Measurable.Id.GetHashCode() % 5 * 2 + 1) / 100.0m);
								double mean = (double)angScore.Measurable.Target.Value;
								score.Measured = (decimal)Math.Floor(100 * r.NextNormal(mean, stdDev)) / 100m;
								s.Update(score);
								addedScores += 1;
							}
						}

					}

					tx.Commit();
					s.Flush();
				}
			}
			var duration = (DateTime.UtcNow - start).TotalSeconds;

			return Content("Todos: +" + addedTodos + "/-" + deletedTodos + " <br/>Issues: +" + addedIssues + "/-" + deletedIssues + " <br/>Scores: +" + addedScores + "/-" + deletedScores + " <br/>Duration: " + duration + "s");
		}

		public class MergeAcc {
			public UserOrganizationModel Main { get; set; }
			public UserOrganizationModel ToMerge { get; set; }
		}
		[Access(AccessLevel.Radial)]
		public ActionResult MergeAccounts(long? id = null) {
			var model = new MergeAcc {
				Main = GetUser()
			};
			if (id != null) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var other = s.Get<UserOrganizationModel>(id);
						model.ToMerge = other;
					}
				}
			}
			return View(model);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult PerformMergeAccounts(long mainId, long mergeId) {
			UserOrganizationModel main;
			UserOrganizationModel merge;
			string email;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					main = s.Get<UserOrganizationModel>(mainId);
					merge = s.Get<UserOrganizationModel>(mergeId);

					if (main.TempUser != null) {
						throw new PermissionsException("Cannot combine users: Main user has not registered");
					}

					if (main.DeleteTime != null) {
						throw new PermissionsException("Cannot combine users: Main user was deleted");
					}

					if (merge.DeleteTime != null) {
						throw new PermissionsException("Cannot combine users: Merge user was deleted");
					}

					if (main.Organization.DeleteTime != null) {
						throw new PermissionsException("Cannot combine users: Main Organization was deleted");
					}

					if (merge.Organization.DeleteTime != null) {
						throw new PermissionsException("Cannot combine users: Merge Organization was deleted");
					}

					email = main.User.UserName;
					var newIds = main.User.UserOrganizationIds.ToList();
					newIds.Add(mergeId);
					newIds = newIds.Distinct().ToList();


					main.User.UserOrganizationIds = newIds.ToArray();
					main.User.UserOrganizationCount = newIds.Count();
					main.User.UserOrganization.Add(merge);

					//if (merge.TempUser != null && merge.User.Id != main.User.Id) {
					//	merge.User.UserOrganization = merge.User.UserOrganization.Where(x => x.Id != mergeId).ToArray();
					//	s.Update(merge.User);
					//}

					s.Update(main.User);

					//	tx.Commit();
					//}

					//using (var tx = s.BeginTransaction()) {

					merge.EmailAtOrganization = email;

					if (merge.TempUser != null) {
						merge.AttachTime = DateTime.UtcNow;
						merge.User = main.User;
						//merge.Organization = ;
						//merge.CurrentRole = userOrgPlaceholder;
						s.Delete(merge.TempUser);
						merge.TempUser = null;
					} else {
						if (merge.User.Id != main.User.Id) {
							merge.User.UserOrganizationIds = merge.User.UserOrganizationIds.Where(x => x != mergeId).Distinct().ToArray();
							merge.User.UserOrganizationCount = merge.User.UserOrganizationIds.Count();
							merge.User.CurrentRole = merge.User.UserOrganizationIds.FirstOrDefault();
							merge.User.UserOrganization = merge.User.UserOrganization.Where(x => x.Id != mergeId).ToArray();

							//merge.User.UserName = merge.User.UserName + "_merged";
							//s.Update(merge.User);
							s.Update(merge.User);

							merge.User = main.User;
						}

						s.Update(merge.User);
					}

					s.Update(merge);


					main.UpdateCache(s);
					merge.UpdateCache(s);

					tx.Commit();
					s.Flush();

				}
			}
			
			//REPLACE_ME
			return Content(
				@"Merged accounts.<br/>==================================<br/><br/>
						Hi " + main.GetFirstName() + @",<br/><br/>
						I've merged your accounts, please use '" + main.User.UserName + @"' to log on from now on to EliteTools. To switch between accounts, click 'Change Organization' from the dropdown in the top-right.<br/><br/>
						Thank you,<br/><br/>
						" + GetUserModel().Name());
		}

		[Access(AccessLevel.Radial)]
		public ActionResult ShiftScorecard(long recurrence = 0, int weeks = 0) {
			if (recurrence == 0 || weeks == 0) {
				return Content("Requires a recurrence and weeks parameter ?recurrence=&weeks= <br/>Warning: this command will shift the measurable regardless of whether it has been shifted for another meeting.");
			}

			var messages = new List<string>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrence)
						.Fetch(x => x.Measurable).Eager
						.Select(x => x.Measurable).List<MeasurableModel>().ToList();

					foreach (var measurable in measurables) {
						if (measurable != null) {
							var message = "Measurable [" + string.Format("{0,-18}", measurable.Id) + "] shifted (" + string.Format("{0,-5}", weeks) + ") weeks.";
							messages.Add(message);
							log.Info(message);
							var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id).List().ToList();
							foreach (var score in scores) {
								score.DateDue = score.DateDue.AddDays(7 * weeks);
								score.ForWeek = score.ForWeek.AddDays(7 * weeks);
								s.Update(score);
							}
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return Content(string.Join("<br/>", messages));
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> ResetSwanServices(long id) {
			var recurId = id;
			if (GetUser().Organization.AccountType == AccountType.SwanServices) {
				//fall through
			} else {
				throw new PermissionsException("Must be a Swan Services account");
			}

			var issues = new[] {
					"Working outside the Core Focus",
					"Scorecards & Measurables for all",
					"Marketing Process",
					"Technical Training",
					"Revise financial department structure",
					"Finance Department Level 10 Meeting",
					"Accounting Software",
					"Next Generation Technology",

				};
			var todos = new[] {
					"Review Sales Process with Sales Team",
					"Meet with Acme Industries for lunch",
					"Schedule meeting with Dan",
					"Send quote to 3 New Prospects",
					"Find three possible locations for new HQ",
					"Create Incentive plan for Sales Team (Rough Draft)",
					"Create and email new clients technology solutions",
					"Make sure entire team is following Marketing Core Process",
					"Call Amber to schedule meeting",
					"Meet with Carol in Finance",
					"Give credit to Mike Paton",
				};





			//var recurId = 1;
			var recur = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), recurId);
			var possibleUsers = recur.Attendees.Select(x => x.Id).ToList();
			possibleUsers.Add(GetUser().Id);

			var addedTodos = 0;
			var addedIssues = 0;
			var addedScores = 0;
			var deletedTodos = 0;
			var deletedIssues = 0;
			var deletedScores = 0;
			var deletedHeadlines = 0;

			DateTime start = DateTime.UtcNow;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recur1 = s.Get<L10Recurrence>(recurId);

					if (recur1.OrganizationId != GetUser().Organization.Id) {
						throw new PermissionsException("Recurrence not part of organization");
					}

					var r = new Random(22586113);
					var u1 = recur.Attendees.ToList()[r.Next(recur.Attendees.Count() - 1)];
					var u2 = recur.Attendees.ToList()[r.Next(recur.Attendees.Count() - 1)];
					var u3 = recur.Attendees.ToList()[r.Next(recur.Attendees.Count() - 1)];

					var headlines = new[] {
							new {Message ="Huge Deal Closed With New Client", AboutId = (long?)u1.Id, AboutName = u1.Name, About=(ResponsibilityGroupModel)s.Load<UserOrganizationModel>(u1.Id), Details =(string)null },
							new {Message ="Jenny had her Baby!!It's A BOY!!!", AboutId = (long?)u2.Id, AboutName = u2.Name, About=(ResponsibilityGroupModel)s.Load<UserOrganizationModel>(u2.Id), Details="Her baby was 17lbs!!! she broke the state record!!"},
							new {Message ="8 New Prospects from Business Convention", AboutId = (long?)u3.Id, AboutName = u3.Name, About=(ResponsibilityGroupModel)s.Load<UserOrganizationModel>(u3.Id), Details =(string)null},
							//new {Message ="Team pulled together after a customer shipment was lost", AboutId = (long?)644, AboutName = "Fulfillment Team", About=(ResponsibilityGroupModel)s.Load<OrganizationTeamModel>(644L)}
						};


					//if (recur1.OrganizationId != 592)
					//	throw new Exception("Cannot edit meetings outside of Gart Sports");
					var me = GetUser().Organization.Members.FirstOrDefault() ?? GetUser();
					var caller = s.Get<UserOrganizationModel>(me.Id);
					var perms = PermissionsUtility.Create(s, caller);


					foreach (var at in recur.Todos.Where(x => !x.Complete.Value)) {
						var todo = s.Load<TodoModel>(at.Id);
						todo.CompleteTime = DateTime.MinValue;
						s.Update(todo);
						deletedTodos += 1;
					}
					var createTime = DateTime.UtcNow.AddDays(-5);
					foreach (var todo in todos) {
						var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						var todoC = TodoCreation.GenerateL10Todo(recurId, todo, null, possibleUsers[r.Next(possibleUsers.Count - 1)], DateTime.UtcNow.AddDays(r.Next(1, 2)), now: createTime);
						await TodoAccessor.CreateTodo(s, perms, todoC);

						//await TodoAccessor.CreateTodo(s, perms, recurId, new Models.Todo.TodoModel {
						//	AccountableUserId = possibleUsers[r.Next(possibleUsers.Count - 1)],
						//	Message = todo,
						//	ForRecurrenceId = recurId,
						//	DueDate = DateTime.UtcNow.AddDays(r.Next(1, 2)),
						//	CompleteTime = complete,
						//	CreateTime = createTime,
						//	OrganizationId = caller.Organization.Id,
						//});
						createTime = createTime.AddMinutes(r.Next(3, 8));
						addedTodos += 1;
					}


					foreach (var at in recur.IssuesList.Issues.Where(x => !x.Complete.Value)) {
						var issue = s.Load<IssueModel.IssueModel_Recurrence>(at.Id);
						issue.CloseTime = DateTime.MinValue;
						s.Update(issue);
						deletedIssues += 1;
					}


					foreach (var h in recur.Headlines) {
						var headline = s.Load<PeopleHeadline>(h.Id);
						headline.CloseTime = DateTime.MinValue;
						s.Update(headline);
						deletedHeadlines += 1;
					}

					createTime = DateTime.UtcNow.AddDays(-5);
					foreach (var issue in issues) {
						//var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						var owner = possibleUsers[r.Next(possibleUsers.Count - 1)];

						var creation = IssueCreation.CreateL10Issue(issue, null, owner, recurId, now: createTime);
						await IssuesAccessor.CreateIssue(s, perms, creation);

						createTime = createTime.AddMinutes(r.Next(5, 15));
						addedIssues += 1;
					}




					foreach (var h in headlines) {
						//var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						var owner = possibleUsers[r.Next(possibleUsers.Count - 1)];
						await HeadlineAccessor.CreateHeadline(s, perms, new PeopleHeadline {
							Message = h.Message,
							AboutId = h.AboutId,
							AboutName = h.AboutName,
							OwnerId = owner,
							RecurrenceId = recurId,
							About = h.About,
							Owner = s.Load<UserOrganizationModel>(owner),

							_Details = h.Details,

							OrganizationId = caller.Organization.Id,
							CreateTime = createTime,
						});
						createTime = createTime.AddMinutes(r.Next(5, 15));
						addedIssues += 1;
					}



					//var regen = await ScorecardAccessor._GenerateScoreModels_Unsafe(s,recur.Scorecard.Weeks.Select(x=>x.ForWeek), recur.Scorecard.Measurables.Select(x=>x.Id));

					if (true/*regen*/) {
						s.Flush();
						recur = await L10Accessor.GetOrGenerateAngularRecurrence(s, perms, recurId);
					}


					var current = recur.Scorecard.Weeks.FirstOrDefault(x => x.IsCurrentWeek).ForWeekNumber;
					var emptyMeasurable = recur.Scorecard.Measurables.ElementAtOrDefault(r.Next(recur.Scorecard.Measurables.Count() - 1)).NotNull(x => x.Id);

					foreach (var angScore in recur.Scorecard.Scores.Where(x => x.ForWeek > current - 13)) {
						if (angScore.Id > 0) {
							if (angScore.ForWeek == current && angScore.Measurable.Id == emptyMeasurable) {
								var score = s.Load<ScoreModel>(angScore.Id);
								score.Measured = null;
								s.Update(score);
								deletedScores += 0;

							} else if (angScore.Measured == null) {
								var score = s.Load<ScoreModel>(angScore.Id);
								double stdDev = (double)(angScore.Measurable.Target.Value * (angScore.Measurable.Id.GetHashCode() % 5 * 2 + 1) / 100.0m);
								double mean = (double)angScore.Measurable.Target.Value;
								score.Measured = (decimal)Math.Floor(100 * r.NextNormal(mean, stdDev)) / 100m;
								s.Update(score);
								addedScores += 1;
							}
						}

					}

					//Commit is required
					tx.Commit();
					s.Flush();
				}
			}
			var duration = (DateTime.UtcNow - start).TotalSeconds;

			return Content("Todos: +" + addedTodos + "/-" + deletedTodos + " <br/>Issues: +" + addedIssues + "/-" + deletedIssues + " <br/>Scores: +" + addedScores + "/-" + deletedScores + " <br/>Duration: " + duration + "s");
		}


		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> ResetPassword(long id) {
			var token = Guid.NewGuid();
			UserModel user;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var userOrg = s.Get<UserOrganizationModel>(id);
					user = userOrg.User;

				}
			}

			var until = DateTime.UtcNow.AddDays(1);
			if (null != user) {
				//Generating a token
				var nexus = new NexusModel(token) { DateCreated = DateTime.UtcNow, DeleteTime = until, ActionCode = NexusActions.ResetPassword };
				nexus.SetArgs(user.Id);
				var result = _NexusAccessor.Put(nexus);
				var email = string.Format(EmailStrings.PasswordReset_Body, user.Name(), Config.BaseUrl(null) + "n/" + token, Config.BaseUrl(null) + "n/" + token, Config.ProductName(null));
				return Content(email);

			}
			return Content("User not found");



		}
		#endregion

		#region Marketing
		[Access(AccessLevel.RadialData)]
		public ActionResult AllEmails(long? id = null) {

			var exports = AdminAccessor.GetExportList();
			var found = exports.FirstOrDefault(x => x.Id == id);
			if (found == null) {
				return View(exports);
			}

			return File(found.Data.ToBytes(), "text/csv", found.GeneratedAt.ToJavascriptMilliseconds() + "_AllValidUsers.csv");

		}

		[Access(AccessLevel.RadialData)]
		public ActionResult ClearAllEmails() {
			AdminAccessor.ClearExports();
			return Content("ok");
		}

		[Access(AccessLevel.RadialData)]
		public ActionResult GenerateAllEmails() {
			var now = DateTime.UtcNow;
			Scheduler.Enqueue(() => AdminAccessor.GenerateAllUserData_Admin_Unsafe(now));
			return Content("Generating: " + now.ToString());
		}

		#endregion

		#region Engineering
		public class SwitchVm {
			public string FeatureId { get; set; }
		}


		[Access(AccessLevel.Radial)]
		public ActionResult Switches() {
			return View(SwitchesAccessor.GetSwitches());
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> AddSwitch(GroupedFeatureSwitches model) {
			var m = await SwitchesAccessor.Update(GetUser(), model);
			return Json(ResultObject.SilentSuccess(m), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> RemoveSwitch(string id) {
			await SwitchesAccessor.RemoveSelector(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		#endregion

		#region Super-duper admin
		[Access(AccessLevel.Radial)]
		public ContentResult SetSoftwareVersion(int version, bool force = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var oldVersion = s.GetSettingOrDefault(Variable.Names.SOFTWARE_VERSION, 1);
					if (oldVersion > version) {
						throw new PermissionsException("Old version is greater than supplied version");
					}

					if (oldVersion == version) {
						if ((DateTime.UtcNow - SoftwareVersion.GetLastCheck()) > TimeSpan.FromMinutes(5) || force) {
							SoftwareVersion.Reset();
							return Content("Old version is equal to than supplied version. Resetting cache.");
						}
						return Content("Old version is equal to than supplied version. NOT resetting cache.");
					}

					s.UpdateSetting(Variable.Names.SOFTWARE_VERSION, version);

					tx.Commit();
					s.Flush();
				}
			}
			SoftwareVersion.Reset();

			var m = _ForceRefreshAll(5, "Service updated. Page will refresh in 30 seconds.");

			return Content("Setting software version to " + version + ". Force refresh over " + m + " minutes.");

		}

		[Access(AccessLevel.Radial)]
		[HttpPost]
		public ActionResult KillDeadlock(FormCollection form) {
			var sb = new StringBuilder();
			sb.Append("Killing selected tasks:");
			sb.Append("<table>");

			foreach (var item in form.AllKeys) {
				sb.Append("<tr>");

				if (item.StartsWith("kill_")) {
					var id = item.SubstringAfter("kill_");
					sb.Append("<td>" + id + "</td>");
					try {
						using (var s = HibernateSession.GetCurrentSession()) {
							using (var tx = s.BeginTransaction()) {
								var res = s.CreateSQLQuery("kill " + id).List<object[]>();
								sb.Append("<td>killed</td><td></td>");
							}
						}
					} catch (Exception e) {
						sb.Append("<td style='background-color:red'>failed</td>");
						sb.Append("<td>" + e.Message + "</td>");
					}

				}
				sb.Append("</tr>");

			}
			sb.Append("</table>");

			sb.Append("<style>td{border:1px solid gray;padding:10px;} table{border-collapse: collapse;}</style>");

			return Content(sb.ToString());
			//return RedirectToAction(nameof(Deadlock));
		}

		[Access(AccessLevel.Radial)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
		public ContentResult Deadlock() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var res = s.CreateSQLQuery("SELECT * FROM INFORMATION_SCHEMA.PROCESSLIST WHERE command = 'Query'").List<object[]>();
					var sb = new StringBuilder();
					sb.Append("<form action='/admin/killdeadlock' method='post'>");
					sb.Append("<input type='submit' value='Kill Selected' tabindex='-1'/> [ ");
					sb.Append("<a href='javscript:void(0);' onclick='unSelectAll()'>Deselect All</a> | ");
					sb.Append("<a href='javscript:void(0);' onclick='selectAll()'>Select All</a> | ");
					sb.Append("<a href='javscript:void(0);' onclick='selectClass(\"yellow\")'>Select Yellow</a> | ");
					sb.Append("<a href='javscript:void(0);' onclick='selectClass(\"synclock\")'>Select SyncLock</a> | ");
					sb.Append("<a href='javscript:void(0);' onclick='selectClass(\"alter\")'>Select 'Alter'</a> ");
					sb.Append("]<br/>Rows:").Append(res.Count);
					sb.Append("<table>");

					res = res.OrderByDescending(r => {
						try {
							return r[6].ToString().ToLower().Contains("metadata lock") || r[7].ToString().ToLower().Contains("synclock");
						} catch (Exception) {
							return false;
						}
					}).ToList();

					foreach (var r in res) {
						var classes = new List<string>();

						try {
							if (r[6].ToString().ToLower().Contains("metadata lock")) {
								classes.Add("yellow");
							}
						} catch (Exception) {
						}
						try {
							if (r[7].ToString().ToLower().Contains("synclock")) {
								classes.Add("yellow");
								classes.Add("synclock");
							}
						} catch (Exception) {
						}
						try {
							if (r[7].ToString().ToLower().Contains("alter table")) {
								classes.Add("alter");
							}
						} catch (Exception) {
						}
						sb.Append("<tr class='" + string.Join(" ", classes) + "'>");

						sb.Append("<td><input class='i-checkbox' type='checkbox' name='kill_" + r.First() + "'/></td>");
						foreach (var i in r) {
							sb.Append("<td>");
							sb.Append(i);
							sb.Append("</td>");
						}
						sb.Append("</tr>");
					}
					sb.Append("</table>");
					sb.Append("</form>");

					sb.Append("<style>td{border:1px solid gray;padding:10px;} table{border-collapse: collapse;} .yellow{background-color:yellow;}</style>");
					sb.Append(@"
<script>
    function unSelectAll() {
        var items = document.getElementsByClassName('i-checkbox');
        for (var i = 0; i < items.length; i++) {
            if (items[i].type == 'checkbox')
                items[i].checked = false;
        }
    }
    function selectAll() {
        var items = document.getElementsByClassName('i-checkbox');
        for (var i = 0; i < items.length; i++) {
            if (items[i].type == 'checkbox')
                items[i].checked = true;
        }
    }	
    function selectClass(clss) {
        var items = document.getElementsByClassName(clss);
        for (var i = 0; i < items.length; i++) {
            var item = items[i].getElementsByTagName('input');
			for (var j = 0; j < item.length; j++) {
				var input=item[j];
				if (input.type == 'checkbox'){
					input.checked = true;
				}
			}
        }
    }	
</script>");
					return Content(sb.ToString());
				}
			}
		}


		[Access(AccessLevel.Radial)]
		public ActionResult Variables() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var vars = s.QueryOver<Variable>().List().ToList();
					return View(vars);
				}
			}
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public JsonResult Variables(string id, Variable model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var variable = s.UpdateSetting(id, model.V);

					tx.Commit();
					s.Flush();
					return Json(ResultObject.SilentSuccess(variable));
				}
			}
		}
		[HttpGet]
		[Access(AccessLevel.Radial)]
		public JsonResult DeleteVariable(string id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var variable = s.Get<Variable>(id);
					s.Delete(variable);
					tx.Commit();
					s.Flush();

					return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult UpdateAppVariables() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					ApplicationAccessor.InitializeAppVariables(s);
					tx.Commit();
					s.Flush();
					return Json(LayoutTrialResult.Weighting, JsonRequestBehavior.AllowGet);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult ForcingRefreshDone(int? minutes = null) {

			var url = Config.BaseUrl(null, "/admin/ForceRefreshAll");
			var builder = url;
			builder += "<br/>?minutes=5<br/>&message=<optional message>";

			if (minutes != null) {
				builder += "<br/></br>Refreshing all within the next " + minutes + " minutes.<br/>All complete at " + DateTime.UtcNow.AddMinutes(minutes.Value + .75).ToLongTimeString() + " GMT";
			}

			return Content(builder);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult ForceRefreshAll(int minutes = 5, string message = "Service updated. Page will refresh in 30 seconds.") {
			minutes = _ForceRefreshAll(minutes, message);
			return RedirectToAction("ForcingRefreshDone", new { minutes = minutes });
		}

		private static int _ForceRefreshAll(int minutes, string message) {
			minutes = Math.Max(1, minutes);
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			hub.Clients.All.forceRefresh(minutes, message);
			return minutes;
		}

		[Access(AccessLevel.Radial)]
		public ActionResult ForceRefresh(string email, string message = null) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			hub.Clients.User(email.ToLower()).forceRefresh(0, message);
			return Content("Hard refreshing for " + email + ".");
		}


		[Access(AccessLevel.Radial)]
		public ActionResult SetReadOnly(bool readOnly) {
			var res = AdminAccessor.SetReadonlyMode(readOnly);


			return Content("readonly:" + res + ".");
		}

		#endregion

		#endregion




	}
	public partial class AccountController : UserManagementController {

		#region Actions

		[Access(Controllers.AccessLevel.Radial)]
		public async Task<string> SetRadialAdmin(bool admin = true, string user = null) {
			user = user ?? GetUser().User.Id;
			var u = _UserAccessor.GetUserById(user);
			if (admin) {
				await UserManager.AddToRoleAsync(user, "RadialAdmin");
				return "Added " + u.UserName + ". (" + string.Join(", ", await UserManager.GetRolesAsync(u.Id)) + ")";
			} else {
				await UserManager.RemoveFromRoleAsync(user, "RadialAdmin");
				return "Removed " + u.UserName + ". (" + string.Join(", ", await UserManager.GetRolesAsync(u.Id)) + ")";
			}
		}

		[Access(Controllers.AccessLevel.Radial)]
		public ActionResult ListRadialAdmin() {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var res = s.CreateSQLQuery("select user.UserName,role.UserModel_id from UserRoleModel as role inner join UserModel as user where role.Role='RadialAdmin' and user.Id=role.UserModel_id").List<object[]>();
					var builder = "<table>";
					foreach (var o in res) {
						builder += "<tr><td>" + o[0] + "</td><td>" + o[1] + "</td></tr>";
					}
					builder += "</table>";
					return Content(builder);
				}
			}
		}


		[Access(AccessLevel.Radial)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
		public ActionResult FixEmail() {
			return View();
		}

		[Access(AccessLevel.Radial)]
		[HttpPost]
		public ActionResult FixEmail(FormCollection form) {
			var user = GetUser();
			var newEmail = form["newEmail"].ToLower();

			if (user.GetEmail() != form["oldEmail"] || user.Id != form["userId"].ToLong()) {
				throw new PermissionsException("Incorrect User.");
			}

			if (!IsValidEmail(newEmail)) {
				throw new PermissionsException("Email invalid.");
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var any = s.QueryOver<UserModel>().Where(x => x.UserName == newEmail).Take(1).SingleOrDefault();

					if (any != null) {
						throw new PermissionsException("User already exists with this email address. Could not change.");
					}

					s.Evict(user);
					user = s.Get<UserOrganizationModel>(user.Id);
					user.EmailAtOrganization = form["newEmail"];

					if (user.User != null) {
						//s.Evict(user.User);
						user.User.UserName = form["newEmail"].ToLower();
						//s.Update(user.User);
					}

					if (user.TempUser != null) {
						//s.Evict(user.TempUser);
						user.TempUser.Email = form["newEmail"];
						//s.Update(user.TempUser);
					}
					user.UpdateCache(s);
					var c = new Cache();
					c.InvalidateForUser(user, CacheKeys.USERORGANIZATION);
					c.InvalidateForUser(user, CacheKeys.USER);
					s.Update(user);
					tx.Commit();
					s.Flush();
				}
			}
			ViewBag.InfoAlert = "Make sure to email this person with their new login.";
			return RedirectToAction("FixEmail");
		}
		private bool IsValidEmail(string email) {
			try {
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			} catch {
				return false;
			}
		}


		[Access(AccessLevel.Radial)]
		public String SendMessage(long id, string message) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(id)).status(message);
			return "Sent: " + message;
		}

		[Access(AccessLevel.Radial)]
		public String UpdateCache(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<UserOrganizationModel>(id).UpdateCache(s);
					tx.Commit();
					s.Flush();
					return "Updated: " + org.GetName();
				}
			}
		}


		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> TestChargeOrg(long id, decimal amt) {
#pragma warning disable CS0618 // Type or member is obsolete
			return Json(await PaymentAccessor.Unsafe.ChargeOrganizationAmount(id, amt, true), JsonRequestBehavior.AllowGet);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> TestChargeToken(string token, decimal amt, bool bank = false) {
#pragma warning disable CS0618 // Type or member is obsolete
			var pr = await PaymentSpringUtil.ChargeToken(null, token, amt, true, bank);
			return Json(pr, JsonRequestBehavior.AllowGet);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		[Access(AccessLevel.Radial)]
		public JsonResult ClearCache() {
			var urlToRemove = Url.Action("UserScorecard", "TileData");
			HttpResponse.RemoveOutputCacheItem(urlToRemove);
			return Json("cleared", JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.RadialData)]
		public JsonResult CalculateOrganizationCharge(long id) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);
					return Json(PaymentAccessor.CalculateCharge(s, org, org.PaymentPlan, DateTime.UtcNow), JsonRequestBehavior.AllowGet);
				}
			}
		}

		#endregion

		#region Data
		[Access(AccessLevel.Radial)]
		public ActionResult Headers() {
			return View();
		}

		[Access(AccessLevel.Radial)]
		public JsonResult GetRedis() {
			return Json(Config.RedisSignalR("CHANNEL"), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.Radial)]
		public string Chrome(string id) {
			ChromeExtensionComms.SendCommand(id);
			return "ok: \"" + id + "\"";
		}

		[Access(AccessLevel.Radial)]
		public async Task<string> SendSlack(string message) {
			Slack.SendNotification(message);
			return "sending";
		}

		[Access(AccessLevel.RadialData)]
		public ActionResult Version() {

			var version = Assembly.GetExecutingAssembly().GetName().Version;
			var buildDate = new DateTime(2000, 1, 1)
				.AddDays(version.Build)
				.AddSeconds(version.Revision * 2);

			//var server = NetworkAccessor.GetPublicIP();//Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			var serverRow = "<tr><td>Amazon Server: </td><td><i>failed</i></td></tr>";
			try {
				serverRow = "<tr><td>Amazon Server: </td><td>" + Amazon.Util.EC2InstanceMetadata.InstanceId.ToString() + "</td></tr>";
			} catch (Exception) {

			}
			var gitRow = "<tr><td>Git:</td><td><i>failed</i></td></tr>";
			try {
				gitRow = "<tr><td>Git:</td><td>" + ThisAssembly.Git.Branch + " </td><td>" + ThisAssembly.Git.Sha + "</td></tr>";
			} catch (Exception) {

			}

			DateTime? dbTime = null;
			var now = DateTime.UtcNow;
			double? diff = null;
			var dbTimeRow = "<tr><td>DbTime:</td><td><i>failed</i></td></tr>";
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						dbTime = TimingUtility.GetDbTimestamp(s);
					}
				}
				var nowAfter = DateTime.UtcNow;
				var half = new DateTime((nowAfter.Ticks - now.Ticks) / 2 + now.Ticks);
				diff = (dbTime - half).Value.TotalMilliseconds;
				dbTimeRow = "<tr><td>DbTime:</td><td>" + dbTime.Value.ToString("U") + " </td><td> [diff: " + diff + "ms]</td></tr>";

			} catch (Exception) {
			}
			var txt = "<table>";
			txt += "<tr><td>Server Time:</td><td>" + now.ToString("U") + " </td><td> [ticks: " + now.Ticks + "]</td></tr>";
			txt += "<tr><td>Build Date: </td><td>" + buildDate.ToString("U") + " </td><td> [version: " + version.ToString() + "]</td></tr>";
			txt += gitRow;
			txt += serverRow;
			txt += dbTimeRow;
			txt += "<tr><td>Server Time:</td><td>" + now.ToString("U") + " </td><td> [ticks: " + now.Ticks + "]</td></tr>";
			txt += serverRow;
			txt += "<tr><td>Version:</td><td>" + Config.GetVersion() + " </td><td></td></tr>";
			txt += "</table>";

			return Content(txt);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult Stats() {
			return Json(ApplicationAccessor.Stats(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult AdminAllUserLookups(string search) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var users = s.QueryOver<UserLookup>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(c => c.Email).IsLike("%" + search + "%")
						.Select(x => x.Email, x => x.UserId, x => x.Name, x => x.OrganizationId)
						.List<object[]>().ToList();
					var orgs = s.QueryOver<OrganizationModel>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Id).IsIn(users.Select(x => (long)x[3]).ToList())
						.List().ToDictionary(x => x.Id, x => x.GetName());

					return Json(new {
						results = users.Select(x => new {
							text = "" + x[0],
							value = "" + x[1],
							name = "" + x[2],
							organization = "" + orgs.GetOrDefault((long)x[3], "")
						}).ToArray()
					}, JsonRequestBehavior.AllowGet);
				}
			}
		}
		#endregion

		#region Deprecated/Test

		[Access(AccessLevel.Radial)]
		public ActionResult Subscribe(long org, NotificationKind kind) {
			PubSub.Subscribe(GetUser(), GetUser().Id, ForModel.Create<OrganizationModel>(org), kind);
			return Content("Subscribed");
		}


		[Access(AccessLevel.Radial)]
		public String TempDeep(long id) {
			var now = DateTime.UtcNow;
			var count = _UserAccessor.CreateDeepSubordinateTree(GetUser(), id, now);

			var o = "TempDeep - " + now.Ticks + " - " + count;
			log.Info(o);
			return o;
		}

		[Access(AccessLevel.Radial)]
		public int FixTeams() {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var teams = s.QueryOver<OrganizationTeamModel>().List();
					foreach (var t in teams) {
						if (t.Type == TeamType.Subordinates && t.DeleteTime == null) {
							var mid = t.ManagedBy;
							var m = s.Get<UserOrganizationModel>(mid);
							if (m.DeleteTime != null) {
								t.DeleteTime = m.DeleteTime;
								s.Update(t);
								count++;
							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count;

		}

		[Access(AccessLevel.Radial)]
		public string UndoRandomReview(long id) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var values = s.QueryOver<CompanyValueAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var roles = s.QueryOver<GetWantCapacityAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var rocks = s.QueryOver<RockAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var feedbacks = s.QueryOver<FeedbackAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();

					foreach (var v in values) {
						v.Exhibits = PositiveNegativeNeutral.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					foreach (var v in roles) {
						v.GetIt = FiveState.Indeterminate;
						v.WantIt = FiveState.Indeterminate;
						v.HasCapacity = FiveState.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count += 3;
					}
					foreach (var v in rocks) {
						v.Finished = Tristate.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					foreach (var v in feedbacks) {
						v.Feedback = null;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					tx.Commit();
					s.Flush();

				}
			}
			return "Undo Random Review. Update: " + count;

		}

		[Access(AccessLevel.Radial)]
		public string RandomReview(long id) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var desenterPercent = .1;
					var standardPercent = .8;
					var standardPercentDeviation = .2;

					var unstartedPercent = .05;
					var incompletePercent = .1;

					var rockPercent = .88;

					var values = s.QueryOver<CompanyValueAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var roles = s.QueryOver<GetWantCapacityAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var rocks = s.QueryOver<RockAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var feebacks = s.QueryOver<FeedbackAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();

					var about2 = new HashSet<long>(values.Select(x => x.RevieweeUserId));
					roles.Select(x => x.RevieweeUserId).ToList().ForEach(x => about2.Add(x));
					rocks.Select(x => x.RevieweeUserId).ToList().ForEach(x => about2.Add(x));

					var reviewIds = new HashSet<long>();

					var about = about2.ToList();

					var r = new Random();
					var unstartedList = new List<long>();
					var incompleteList = new List<long>();

					for (var i = 0; i <= unstartedPercent * about.Count; i++) {
						unstartedList.Add(about[r.Next(about.Count)]);
					}

					for (var i = 0; i <= incompletePercent * about.Count; i++) {
						incompleteList.Add(about[r.Next(about.Count)]);
					}

					var lookup = about.ToDictionary(x => x, x => {
						//BadEgg
						var luckA = 0.3;
						var luckB = 0.3;

						if (r.NextDouble() > desenterPercent) {//Standard
							luckA = Math.Max(Math.Min((r.NextDouble() - .5) * standardPercentDeviation + standardPercent, 1), 0);
						}
						if (r.NextDouble() > desenterPercent) {//Standard
							luckB = Math.Max(Math.Min((r.NextDouble() - .5) * standardPercentDeviation + standardPercent, 1), 0);
						}

						return new { luckA, luckB };
					});

					foreach (var v in values) {
						var a = lookup[v.RevieweeUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ReviewerUserId)) {
								continue;
							}

							if (incompleteList.Contains(v.ReviewerUserId) && r.NextDouble() > .5) {
								continue;
							}

							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							if (r.NextDouble() < a.luckA) {
								v.Exhibits = PositiveNegativeNeutral.Positive;
							} else {
								if (r.NextDouble() < a.luckA / 2) {
									v.Exhibits = PositiveNegativeNeutral.Negative;
								} else {
									v.Exhibits = PositiveNegativeNeutral.Neutral;
								}
							}
							s.Update(v);
						}
					}

					foreach (var v in roles) {
						var a = lookup[v.RevieweeUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ReviewerUserId)) {
								continue;
							}

							if (incompleteList.Contains(v.ReviewerUserId) && r.NextDouble() > .5) {
								continue;
							}

							count += 3;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							if (r.NextDouble() < a.luckB) {
								v.GetIt = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
								v.WantIt = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
								v.HasCapacity = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
							} else {
								v.GetIt = (r.NextDouble() > .25) ? FiveState.Rarely : FiveState.Never;
								v.WantIt = (r.NextDouble() > 25) ? FiveState.Rarely : FiveState.Never;
								v.HasCapacity = (r.NextDouble() > 25) ? FiveState.Rarely : FiveState.Never;
							}
							s.Update(v);
						}
					}

					foreach (var v in rocks) {
						var a = lookup[v.RevieweeUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ReviewerUserId)) {
								continue;
							}

							if (incompleteList.Contains(v.ReviewerUserId) && r.NextDouble() > .5) {
								continue;
							}

							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							v.Finished = (r.NextDouble() < rockPercent) ? Tristate.True : Tristate.False;
							s.Update(v);
						}
					}

					var allFeedbacks = new[] { "No comment.", "Good progress.", "Could use some work", "Excellent", "A pleasure to work with" };

					foreach (var v in feebacks) {
						var a = lookup[v.RevieweeUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ReviewerUserId)) {
								continue;
							}

							if (incompleteList.Contains(v.ReviewerUserId) && r.NextDouble() > .5) {
								continue;
							}

							if (!v.Required) {
								continue;
							}

							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;

							v.Feedback = (r.NextDouble() < .05) ? allFeedbacks[r.Next(allFeedbacks.Length)] : "";
							s.Update(v);
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return "Completed Randomize. Updated:" + count;
		}


		[Access(AccessLevel.Radial)]
		public String FixScatterChart(bool delete = false) {
			var i = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var scatters = s.QueryOver<ClientReviewModel>().List();
					foreach (var sc in scatters) {
						if (sc.ScatterChart == null || delete) {
							i++;
							sc.ScatterChart = new LongTuple();
							if (sc.Charts.Any()) {
								sc.ScatterChart.Filters = sc.Charts.First().Filters;
								sc.ScatterChart.Groups = sc.Charts.First().Groups;
								sc.ScatterChart.Title = sc.Charts.First().Title;
							}
							s.Update(sc);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}

			return "" + i;
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> Emails(int id) {
			
			//REPLACE_ME
			var emails = Enumerable.Range(0, id).Select(x => Mail.To(EmailTypes.Test, "rcisney@dlpre.com").Subject("TestBulk").Body("Email #{0}", "" + x));
			var result = (await Emailer.SendEmails(emails));
			result.Errors = null;

			return Json(result, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult FixReviewData() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviews = s.QueryOver<ReviewModel>().List().ToList();
					var allAnswers = s.QueryOver<AnswerModel>().List().ToList();

					foreach (var r in reviews) {
						var update = false;
						if (r.DurationMinutes == null && r.Complete) {
							var ans = allAnswers.Where(x => x.ForReviewId == r.Id).ToList();
							r.DurationMinutes = (decimal?)TimingUtility.ReviewDurationMinutes(ans, TimingUtility.ExcludeLongerThan);
							update = true;
						}

						if (r.Started == false) {
							var started = allAnswers.Any(x => x.ForReviewId == r.Id && x.Complete);
							r.Started = started;
							update = true;
						}
						if (update) {
							s.Update(r);
						}
					}

					tx.Commit();
					s.Flush();
				}
			}

			return Json(true, JsonRequestBehavior.AllowGet);
		}

		private RadialReview.Controllers.ReviewController.ReviewDetailsViewModel GetReviewDetails(ReviewModel review) {
			var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);
			var answers = _ReviewAccessor.GetAnswersForUserReview(GetUser(), review.ReviewerUserId, review.ForReviewContainerId);
			var model = new RadialReview.Controllers.ReviewController.ReviewDetailsViewModel() {
				Review = review,
				Axis = categories.ToSelectList(x => x.Category.Translate(), x => x.Id),
				xAxis = categories.FirstOrDefault().NotNull(x => x.Id),
				yAxis = categories.Skip(1).FirstOrDefault().NotNull(x => x.Id),
				AnswersAbout = answers,
				Categories = categories.ToDictionary(x => x.Id, x => x.Category.Translate()),
				NumberOfWeeks = TimingUtility.NumberOfWeeks(GetUser())
			};
			return model;
		}

		[Access(AccessLevel.Any)]
		public bool TestTask(long id) {
			var fire = DateTime.UtcNow.AddSeconds(id);
			TaskAccessor.AddTask(new ScheduledTask() { Fire = fire, Url = "/Account/TestTaskRecieve" });
			log.Debug("TestTaskRecieve scheduled for: " + fire.ToString());
			return true;
		}

		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public bool TestTaskRecieve() {
			log.Debug("TestTaskRecieve hit: " + DateTime.UtcNow.ToString());
			return true;
		}

		[Access(AccessLevel.Any)]
		public ActionResult TestChart(long id, long reviewsId) {
			var review = _ReviewAccessor.GetReview(GetUser(), id);

			var model = GetReviewDetails(review);
			return View(model);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult XLS() {
			return Xls(CsvUtility.ToXls(false, files: (List<Csv>)null), "myxml");
		}

		#endregion
	}
}
