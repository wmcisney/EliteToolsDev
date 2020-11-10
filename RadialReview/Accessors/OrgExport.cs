using Newtonsoft.Json;
using NHibernate.Proxy;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.Encrypt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using static RadialReview.Models.Issues.IssueModel;

namespace RadialReview.Accessors {
	public class OrgExport {


		public static class ObjectToDictionaryHelper {

			public static IDictionary<string, object> ToDictionary(object source) {
				if (source == null) {
					ThrowExceptionWhenSourceArgumentIsNull();
				}

				var dictionary = new Dictionary<string, object>();
				foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source)) {
					AddPropertyToDictionary<object>(property, source, dictionary);
				}

				return dictionary;
			}

			private static void AddPropertyToDictionary<T>(PropertyDescriptor property, object source, Dictionary<string, T> dictionary) {
				object value = property.GetValue(source);
				if (IsOfType<T>(value)) {
					dictionary.Add(property.Name, (T)value);
				}
			}

			private static bool IsOfType<T>(object value) {
				return value is T;
			}

			private static void ThrowExceptionWhenSourceArgumentIsNull() {
				throw new ArgumentNullException("source", "Unable to convert object to a dictionary. The source object is null.");
			}
		}

		//public class SeenObj {
		//	public object Seen { get; set; }
		//	public object Transcribed { get; set; }
		//}

		//public static object Convert(object o, List<SeenObj> seen, int level) {

		//	if (o == null) {
		//		return null;
		//	}

		//	if (o.GetType().IsValueType || o.GetType() == typeof(string)) {
		//		return o;
		//	}

		//	var proxy = o as INHibernateProxy;
		//	if (proxy != null) {
		//		return null;
		//	}

		//	var found = seen.FirstOrDefault(x => x.Seen == o);
		//	if (found != null) {
		//		if (level < 4) {
		//			return found.Transcribed;
		//		}

		//		return null;
		//	}
		//	var me = new SeenObj() {
		//		Seen = o
		//	};
		//	seen.Add(me);

		//	var ol = o as IEnumerable;
		//	var od = o as IDictionary;
		//	if (ol != null && od == null) {
		//		var list = new List<object>();
		//		foreach (var i in ol) {
		//			list.Add(Convert(i, seen, level + 1));
		//			//seen.Add(i);
		//		}
		//		me.Transcribed = list;
		//		return list;
		//	}

		//	if (od != null) {
		//		var list = new Dictionary<object, object>();
		//		foreach (var i in od.Keys) {
		//			list.Add(i, Convert(od[i], seen, level + 1));
		//			//seen.Add(od[i]);
		//		}
		//		me.Transcribed = list;
		//		return list;
		//	}

		//	var res = Convert(ObjectToDictionaryHelper.ToDictionary(o), seen, level + 1);

		//	me.Transcribed = res;
		//	return res;

		//}


		public class JsonOrg {

			public object Users { get; set; }
			public object Meetings { get; set; }
			public object Rocks { get; set; }
			public object Milestones { get; set; }
			public object Measurables { get; set; }
			public object Scores { get; set; }
			public object Todos { get; set; }
			public object Headlines { get; set; }
			public object Issues { get; set; }
			public object MeetingAttendees { get; set; }
			public object MeetingRocks { get; set; }
			public object MeetingMeasurables { get; set; }
			public object AccountabilityChartNodes { get; set; }
			public object AccountabilityChartRoleGroups { get; set; }
			public object AccountabilityChartRoles { get; set; }
			public object AccountabilityChartRolesMap { get; set; }
			public object AccountabilityChartPositions { get; set; }
			public object Vtos { get; set; }
			public object VtoStrings { get; set; }
			public object VtoKVs { get; set; }
			public object VtoMarketingStrategies;
			public object PermissionItems { get; set; }
			public object Organization { get; set; }
			public object AccountabilityChartRolesMap_Deprecated { get; set; }


			//public List<UserOrganizationModel> Users { get; set; }
			//public List<L10Recurrence> Meetings { get; internal set; }
			//public List<RockModel> Rocks { get; internal set; }
			//public List<MeasurableModel> Measurables { get; internal set; }
			//public List<ScoreModel> Scores { get; internal set; }
			//public List<TodoModel> Todos { get; internal set; }
			//public List<PeopleHeadline> Headlines { get; internal set; }
			//public List<IssueModel_Recurrence> Issues { get; internal set; }
		}





		public static string GetUsers(UserOrganizationModel caller, long orgId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin(true);

					var userOrgs = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.Organization.Id == orgId && x.DeleteTime == null)
						.List().ToList();




					var str = JsonConvert.SerializeObject(userOrgs.Where(x => x.User != null).Select(x => new {
						x.User.UserName,
						x.User.FirstName,
						x.User.LastName,
						x.User.Id,
						x.User.CreateTime,
						x.User.DeleteTime,
						x.User.SendTodoTime,
						Password = x.User.PasswordHash,
						ImageUrl = x.User.ImageGuid,
					}).ToList(),Formatting.Indented);

					var encryptKey = Config.GetAppSetting("V2_UserEncryptionKey", null);
					if (encryptKey == null) {
						throw new Exception("null-key");
					}

					var res =  EncryptionUtility.Encrypt(str, encryptKey);
					var _test = EncryptionUtility.Decrypt(res, encryptKey);

					if (str != _test)
						throw new Exception("Decrypt Failed");

					return res;
				}
			}
		}


		public static object GetJson(UserOrganizationModel caller, long orgId) {
			JsonOrg res = null;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin(true);


					#region Users
					var users = s.QueryOver<UserOrganizationModel>()
							.Where(x => x.DeleteTime == null && x.Organization.Id == orgId)
							.List().Select(x => new {
								x.AttachTime,
								x.ClientOrganizationName,
								x.CreateTime,
								x.DeleteTime,
								x.DetachTime,
								EmailAtOrganization_DONTUSE = x.EmailAtOrganization,
								x.EvalOnly,
								x.Id,
								x.IsClient,
								x.IsImplementer,
								x.IsPlaceholder,
								IsManager = x.ManagerAtOrganization,
								IsOrgAdmin = x.ManagingOrganization,
								x.Name,
								OrgId = x.Organization.Id,
								UserModelId = x.User.NotNull(y => y.Id),
							}).ToList();

					#endregion
					#region Meetings

					var meetings = s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new {
							x.AttendingOffByDefault,
							x.CombineRocks,
							x.CountDown,
							x.CreatedById,
							x.CreateTime,
							x.CurrentWeekHighlightShift,
							x.DefaultIssueOwner,
							x.DefaultTodoOwner,
							x.DeleteTime,
							x.EnableTranscription,
							x.ForumCode,
							x.ForumStep,
							x.HeadlinesId,
							x.HeadlineType,
							x.Id,
							x.IncludeAggregateTodoCompletion,
							x.IncludeAggregateTodoCompletionOnPrintout,
							x.IncludeIndividualTodos,
							x.MeetingInProgress,
							x.MeetingType,
							x.Name,
							x.OrderIssueBy,
							OrgId = x.Organization.Id,
							x.PrintOutRockStatus,
							x.Prioritization,
							x.Pristine,
							x.ReverseScorecard,
							x.RockType,
							ZoomId = x.SelectedVideoProvider.NotNull(y => y.Url),
							x.StartOfWeekOverride,
							x.TeamType,
							x.VideoId,
							x.VtoId,
						}).ToList();
					#endregion
					#region Meeting Items
					var rocks = s.QueryOver<RockModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new {
							OwnerId = x.AccountableUser.Id,
							x.Archived,
							x.CompleteTime,
							x.Completion,
							x.CreateTime,
							x.DeleteTime,
							x.DueDate,
							x.ForUserId,
							x.Id,
							x.Name,
							x.OnlyAsk,
							x.OrganizationId,
							x.PadId,
							x.Rock,
						}).ToList();
					var milestones = s.QueryOver<Milestone>()
						.WhereRestrictionOn(x=>x.RockId).IsIn(rocks.Select(y=>y.Id).ToList())
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {						
							x.CompleteTime,
							x.CreateTime,
							x.DeleteTime,
							x.DueDate,
							x.Id,
							x.Name,
							x.PadId,
							x.Required,
							x.RockId,
							x.Status,
						}).ToList();
					var measurables = s.QueryOver<MeasurableModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new {
							x.AccountableUserId,
							x.AdminUserId,
							x.AlternateGoal,
							x.Archived,
							x.AverageRange,
							x.BackReferenceMeasurables,
							x.CreateTime,
							x.CumulativeRange,
							x.DeleteTime,
							//x.DueDate,
							x.Formula,
							x.Goal,
							x.GoalDirection,
							x.HasFormula,
							x.Id,
							x.OrganizationId,
							x.ShowAverage,
							x.ShowCumulative,
							x.Title,
							x.UnitType,
						}).ToList();
					var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.ForWeek > new DateTime(2010, 1, 1))
						.List().Select(x => new {
							x.AccountableUserId,
							x.AlternateOriginalGoal,
							//x.DateDue,
							x.DateEntered,
							x.DeleteTime,
							x.ForWeek,
							x.Id,
							x.MeasurableId,
							x.Measured,
							x.OrganizationId,
							x.OriginalGoal,
							x.OriginalGoalDirection,
						}).ToList();
					var todos = s.QueryOver<TodoModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new {
							x.AccountableUserId,
							x.ClearedInMeeting,
							x.CloseTime,
							x.CompleteDuringMeetingId,
							x.CompleteTime,
							x.CreatedById,
							x.CreatedDuringMeetingId,
							x.CreateTime,
							x.DeleteTime,
							x.DueDate,
							x.ForModel,
							x.ForModelId,
							x.ForRecurrenceId,
							x.Id,
							x.Message,
							x.Ordering,
							x.OrganizationId,
							x.PadId,
							x.TodoType
						}).ToList();
					var headlines = s.QueryOver<PeopleHeadline>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new {
							x.AboutId,
							x.AboutIdText,
							x.AboutName,
							x.CloseDuringMeetingId,
							x.CloseTime,
							x.CreatedBy,
							x.CreatedDuringMeetingId,
							x.CreateTime,
							x.DeleteTime,
							x.HeadlinePadId,
							x.Id,
							x.Message,
							x.Ordering,
							x.OrganizationId,
							x.OwnerId,
							x.RecurrenceId,
						}).ToList();

					L10Recurrence recurAlias = null;
					var issues = s.QueryOver<IssueModel_Recurrence>()
						.JoinAlias(x => x.Recurrence, () => recurAlias)
						.Where(x => x.DeleteTime == null && x.Recurrence != null && recurAlias.OrganizationId == orgId)
						.List().Select(x => new {
							x.AwaitingSolve,
							x.CloseTime,
							CopiedFromIssueRecurrenceId = x.CopiedFrom.NotNull(y => y.Id),

							x.CreatedBy,
							x.CreateTime,
							x.DeleteTime,
							x.Id,
							x.LastUpdate_Priority,
							x.MarkedForClose,
							x.Ordering,
							OwnerId = x.Owner.NotNull(y => y.Id),
							x.Priority,
							x.Rank,
							RecurrenceId = x.Recurrence.NotNull(y => y.Id),

							Issue_Id = x.Issue.NotNull(y => y.Id),
							Issue_Message = x.Issue.NotNull(y => y.Message),
							Issue_PadId = x.Issue.NotNull(y => y.PadId),
							Issue_CreatedById = x.Issue.NotNull(y => y.CreatedById),
							Issue_CreatedDuringMeetingId = x.Issue.NotNull(y => y.CreatedDuringMeetingId),
							Issue_CreateTime = x.Issue.NotNull(y => y.CreateTime),
							Issue_DeleteTime = x.Issue.NotNull(y => y.DeleteTime),
							Issue_ForModel = x.Issue.NotNull(y => y.ForModel),
							Issue_ForModelId = x.Issue.NotNull(y => y.ForModelId),
						}).ToList();
					#endregion
					#region Meeting Attachments
					var meetingIds = meetings.Select(x => x.Id).ToArray();
					var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
						.WhereRestrictionOn(x => x.L10Recurrence.Id)
						.IsIn(meetingIds)
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {
							x.Id,
							x.CreateTime,
							x.DeleteTime,
							RecurrenceId = x.L10Recurrence.Id,
							x.SharePeopleAnalyzer,
							x.StarDate,
							UserId = x.User.Id
						}).ToList();

					var attachedRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
						.WhereRestrictionOn(x => x.L10Recurrence.Id)
						.IsIn(meetingIds)
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {
							x.Id,
							x.CreateTime,
							x.DeleteTime,
							RockId = x.ForRock.Id,
							RecurrenceId = x.L10Recurrence.Id,
							OnTheVto = x.VtoRock,

						}).ToList();

					var attachedMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.WhereRestrictionOn(x => x.L10Recurrence.Id)
						.IsIn(meetingIds)
						.Where(x => x.DeleteTime == null && x.Measurable != null)
						.List().Select(x => new {
							x.Id,
							x.CreateTime,
							x.DeleteTime,
							x.IsDivider,
							MeasurableId = x.Measurable.Id,
							Ordering = x._Ordering,
							RecurrenceId = x.L10Recurrence.Id
						}).ToList();

					#endregion
					#region AC

					//AC

					var acNodes = s.QueryOver<AccountabilityNode>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new {
							x.AccountabilityChartId,
							x.AccountabilityRolesGroupId,
							x.CreateTime,
							x.DeleteTime,
							x.Id,
							x.ModelId,
							x.ModelType,
							x.Ordering,
							x.ParentNodeId,
							x.UserId,
						}).ToList();

					var acRoleGroups = s.QueryOver<AccountabilityRolesGroup>()
						.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
						.List().Select(x => new {
							x.AccountabilityChartId,
							x.CreateTime,
							x.DeleteTime,
							x.Id,
							x.OrganizationId,
							x.PositionId,
						}).ToList();

					var roles = s.QueryOver<RoleModel>()
						.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
						.List().Select(x => new {
							x.CreateTime,
							x.DeleteTime,
							x.ForUserId,
							x.Id,
							x.Role
						}).ToList();

					var roleLink = s.QueryOver<RoleLink>()
										.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
										.List().Select(x => new {
											x.AttachId,
											x.AttachType,
											x.CreateTime,
											x.DeleteTime,
											x.Id,
											x.Ordering,
											x.RoleId,
										}).ToList();

					var roleNodeMap = s.QueryOver<AccountabilityNodeRoleMap>()
						.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
						.List().Select(x => new {
							x.Id,
							x.AccountabilityChartId,
							x.AccountabilityGroupId,
							x.CreateTime,
							x.DeleteTime,
							x.RoleId,
						}).ToList();


					var positions = s.QueryOver<OrganizationPositionModel>()
						.Where(x => x.Organization.Id == orgId && x.DeleteTime == null)
						.List().Select(x => new {
							x.Id,
							x.CreatedBy,
							x.CreateTime,
							Name = x.CustomName,
							x.DeleteTime,
						}).ToList();


					#endregion
					#region VTO

					var vtos = s.QueryOver<VtoModel>()
						.Where(x => x.Organization.Id == orgId && x.DeleteTime == null)
						.List().Select(x => new {
							x.CopiedFrom,
							CoreFocus_Title = x.CoreFocus.CoreFocusTitle,
							CoreFocus_Niche = x.CoreFocus.Niche,
							CoreFocus_Purpose = x.CoreFocus.Purpose,
							CoreFocus_PurposeTitle = x.CoreFocus.PurposeTitle,
							x.CoreValueTitle,
							x.CreateTime,
							x.DeleteTime,
							x.Id,
							x.IssuesListTitle,
							RecurrenceId = x.L10Recurrence,
							x.Name,
							OneYearPlan_FutureDate = x.OneYearPlan.FutureDate,
							OneYearPlan_Title = x.OneYearPlan.OneYearPlanTitle,
							x.OrganizationWide,
							QuarterlyRocks_FutureDate = x.QuarterlyRocks.FutureDate,
							QuarterlyRocks_Title = x.QuarterlyRocks.RocksTitle,
							x.TenYearTarget,
							x.TenYearTargetTitle,
							x.ThreeYearPicture.FutureDate,
							x.ThreeYearPicture.ThreeYearPictureTitle,
						}).ToList();

					var vtoIds = vtos.Select(x => x.Id).ToArray();

					var vtoStrings = s.QueryOver<VtoItem_String>()
						.WhereRestrictionOn(x => x.Vto.Id)
						.IsIn(vtoIds)
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {
							x.BaseId,
							x.CopiedFrom,
							x.CreateTime,
							x.Data,
							x.DeleteTime,
							x.ForModel,
							x.Id,
							x.MarketingStrategyId,
							x.Ordering,
							x.Type,
							VtoId = x.Vto.Id
						}).ToList();

					var vtoKVs = s.QueryOver<VtoItem_KV>()
						.WhereRestrictionOn(x => x.Vto.Id)
						.IsIn(vtoIds)
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {
							x.BaseId,
							x.CopiedFrom,
							x.CreateTime,
							x.DeleteTime,
							x.K,
							x.ForModel,
							x.Id,
							x.Ordering,
							x.Type,
							x.V,
							VtoId = x.Vto.Id
						}).ToList();

					//Vto Market strategy

					var marketingStrategies = s.QueryOver<MarketingStrategyModel>()
						.WhereRestrictionOn(x => x.Vto)
						.IsIn(vtoIds)
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {
							x.CreateTime,
							x.DeleteTime,
							x.Guarantee,
							x.Id,
							x.MarketingStrategyTitle,
							x.ProvenProcess,
							x.TargetMarket,
							x.Title,
							x.Vto,
						}).ToList();

					//Special meeting permissions

					var _permItems = s.QueryOver<PermItem>()
						.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
						.List().Select(x => new {
							x.Id,
							Accessor = new { Id = x.AccessorId, Type = x.AccessorType },
							Resource = new { Id = x.ResId, Type = x.ResType },
							x.CanView,
							x.CanEdit,
							x.CanAdmin,
							x.CreateTime,
							x.CreatorId,
							x.DeleteTime,
						}).ToList();

					var _emailPermIds = _permItems.Where(x => x.Accessor.Type == PermItem.AccessType.Email)
											.Select(x => x.Accessor.Id)
											.ToArray();

					var emailPermItems = s.QueryOver<EmailPermItem>()
						.WhereRestrictionOn(x => x.Id).IsIn(_emailPermIds)
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new {
							AccessorId = x.Id,
							x.Email,
							x.CreateTime,
							x.CreatorId,
							x.DeleteTime,
						}).ToDefaultDictionary(x => x.AccessorId, x => x);


					var permissions = _permItems.Select(x => new {
						x.Id,
						Accessor = new { x.Accessor.Id, x.Accessor.Type, Email = emailPermItems[x.Accessor.Id] },
						Resource = new { x.Resource.Id, x.Resource.Type },
						x.CanView,
						x.CanEdit,
						x.CanAdmin,
						x.CreateTime,
						x.CreatorId,
						x.DeleteTime,
					});



					//Org
					var _org = s.Get<OrganizationModel>(orgId);

					var paymentPlan = s.Get<PaymentPlan_Monthly>(_org.PaymentPlan.Id);

					var org = new {
						_org.AccountabilityChartId,
						_org.AccountType,
						_org.CreationTime,
						_org.DeleteTime,
						_org.Id,
						ImageUrl = _org.Image.NotNull(y => y.Url),
						_org.ImplementerEmail,
						LockoutStatus = _org.Lockout,
						Settings = new {
							_org.ManagersCanEdit,
							_org.ManagersCanEditPositions,
							_org.ManagersCanRemoveUsers,
							_org.PrimaryContactUserId,
							_org.SendEmailImmediately,
							_org.StrictHierarchy,
							_org.Settings.AllowAddClient,
							_org.Settings.AutoUpgradePayment,
							_org.Settings.Branding,
							_org.Settings.DateFormat,
							_org.Settings.DefaultSendTodoTime,
							_org.Settings.DisableAC,
							_org.Settings.DisableUpgradeUsers,
							_org.Settings.EmployeeCanCreateL10,
							_org.Settings.EmployeesCanCreateSurvey,
							_org.Settings.EmployeesCanEditSelf,
							_org.Settings.EmployeesCanViewScorecard,
							_org.Settings.EnableCoreProcess,
							_org.Settings.EnableL10,
							_org.Settings.EnablePeople,
							_org.Settings.EnableReview,
							_org.Settings.EnableSurvey,
							_org.Settings.EnableZapier,
							_org.Settings.ImageGuid,
							_org.Settings.LimitFiveState,
							_org.Settings.ManagersCanCreateL10,
							_org.Settings.ManagersCanCreateSurvey,
							_org.Settings.ManagersCanEditSelf,
							_org.Settings.ManagersCanEditSubordinateL10,
							_org.Settings.ManagersCanViewScorecard,
							_org.Settings.ManagersCanViewSubordinateL10,
							_org.Settings.NumberFormat,
							_org.Settings.OnlySeeRocksAndScorecardBelowYou,
							_org.Settings.PrimaryColor,
							_org.Settings.RockName,
							_org.Settings.ScorecardPeriod,
							_org.Settings.ShareVtoPages,
							_org.Settings.StartOfYearMonth,
							_org.Settings.StartOfYearOffset,
							_org.Settings.TextColor,
							_org.Settings.TimeZoneId,
							_org.Settings.WeekStart,
							_org.Settings.YearStart,
						},
						Name = _org.Name.Standard,
						Payment = new {
							paymentPlan.Description,
							paymentPlan.FreeUntil,
							paymentPlan.Id,
							paymentPlan.LastExecuted,
							paymentPlan.PlanCreated,
							paymentPlan.PlanType,

							paymentPlan.BaselinePrice,
							paymentPlan.FirstN_Users_Free,
							paymentPlan.L10FreeUntil,
							paymentPlan.L10PricePerPerson,
							paymentPlan.NoChargeForClients,
							paymentPlan.NoChargeForUnregisteredUsers,
							paymentPlan.OrgId,
							paymentPlan.ReviewFreeUntil,
							paymentPlan.ReviewPricePerPerson,
							paymentPlan.SchedulePeriod,
							Task = new {
								paymentPlan.Task.CreatedFromTaskId,
								paymentPlan.Task.CreateTime,
								paymentPlan.Task.DeleteTime,
								paymentPlan.Task.EmailOnException,
								paymentPlan.Task.ExceptionCount,
								paymentPlan.Task.Executed,
								paymentPlan.Task.Fire,
								paymentPlan.Task.FirstFire,
								paymentPlan.Task.Id,
								paymentPlan.Task.MaxException,
								paymentPlan.Task.NextSchedule,
								paymentPlan.Task.OriginalTaskId,
								paymentPlan.Task.Started,
								paymentPlan.Task.TaskName,
								paymentPlan.Task.Url,
							},
						}
					};



					#endregion
					res = new JsonOrg() {
						Users = users,
						Meetings = meetings,
						Rocks = rocks,
						Milestones = milestones,
						Measurables = measurables,
						Scores = scores,
						Todos = todos,
						Headlines = headlines,
						Issues = issues,
						MeetingAttendees = attendees,
						MeetingRocks = attachedRocks,
						MeetingMeasurables = attachedMeasurables,
						AccountabilityChartNodes = acNodes,
						AccountabilityChartRoleGroups = acRoleGroups,
						AccountabilityChartRoles = roles,
						AccountabilityChartRolesMap_Deprecated = roleNodeMap,
						AccountabilityChartRolesMap = roleLink,
						AccountabilityChartPositions = positions,
						Vtos = vtos,
						VtoStrings = vtoStrings,
						VtoKVs = vtoKVs,
						VtoMarketingStrategies = marketingStrategies,
						PermissionItems = permissions,
						Organization = org,

					};

					return res;
					//return Convert(res,new List<SeenObj>(),0);
				}
			}





		}
	}
}