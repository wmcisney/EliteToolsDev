using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Accessors;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Utilities;
using RadialReview.Hangfire;
using Hangfire;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Utilities.DataTypes;
using log4net;
using RadialReview.Crosscutting.Zapier;

namespace RadialReview.Crosscutting.Hooks.CrossCutting.Zapier {
	public class ZapierEventSubscription : ITodoHook, IRockHook, IIssueHook, IMeasurableHook, IHeadlineHook, IMeetingRockHook, IMeetingMeasurableHook, IScoreHook {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}

		#region Todo
		public async Task CreateTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, todo.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierTodo_Hangfire(todo.Id, ZapierEvents.new_todo));
			}
		}
		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, todo.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierTodo_Hangfire(todo.Id, ZapierEvents.update_todo));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierTodo_{0}")]
		public async static Task ZapierTodo_Hangfire(long todoId, ZapierEvents @event) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var todoModel = s.Get<TodoModel>(todoId);
					var todo = new AngularTodo(todoModel);

					var meeting_id = todoModel.ForRecurrenceId;
					string meeting_name = null;
					if (meeting_id != null) {
						meeting_name = s.Get<L10Recurrence>(meeting_id.Value).Name;
					}

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, todoModel.Organization.Id, @event)
															  .GetSubscriptionResponses(todoId, todo.Owner.Id, todo.L10RecurrenceId, x => x.ViewTodo(todoId), ctx => new {
																  id = todo.Id,
																  title = todo.Name,
																  complete = todo.Complete==true,
																  due_date = todo.DueDate,
																  create_time = todo.CreateTime,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(todo.Owner.Id, false), todo.Owner.Id, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(todo.Owner.Id, false), todo.Owner.Name, null),
																  meeting_id = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), (long?)meeting_id, null),
																  meeting_name = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), meeting_name, null),
																  //tododetails = todo.DetailsUrl,
															  });

					await PostEventToZapier(subscriptionResponses);
				}
			}
		}
		#endregion

		#region Rock
		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, rock.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierRock_Hangfire(rock.Id, ZapierEvents.new_rock));
			}
		}
		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, rock.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierRock_Hangfire(rock.Id, ZapierEvents.update_todo));
			}
		}
		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//Noop
		}


		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierRock_{0}")]
		public async static Task ZapierRock_Hangfire(long rockId, ZapierEvents @event) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rockModel = s.Get<RockModel>(rockId);
					var rock = new AngularRock(rockModel, null);

					L10Recurrence recurAlias = null;
					var recurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
										.JoinAlias(x => x.L10Recurrence, () => recurAlias)
										.Where(x => x.ForRock.Id == rock.Id && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
										.List<object[]>()
										.Select(x => new { Name = (string)x[1], Id = (long)x[0] })
										.ToArray();

					var recurrenceIds = recurrences.Select(x => x.Id).ToArray();


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, rockModel.OrganizationId, ZapierEvents.new_rock)
															  .GetSubscriptionResponses(rockId, rock.Owner.Id, recurrenceIds, x => x.ViewRock(rockId), ctx => new {
																  id = rock.Id,
																  title = rock.Name,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(rock.Owner.Id, false), rock.Owner.Id, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(rock.Owner.Id, false), rock.Owner.Name, null),
																  create_time = rock.CreateTime,
																  status = rock.Completion,
																  complete = rock.Complete==true,
																  due_date = rock.DueDate,
															  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}


        public async Task UnArchiveRock(ISession s, RockModel rock, bool v)
        {
			// Noop
		}
		public async Task UndeleteRock(ISession s, RockModel rock) {
			//Nothing
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, rock.OrganizationId)) {
				Scheduler.Enqueue(() => AttachRockInZapier_Hangfire(rock.Id, recurRock.L10Recurrence.Id));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierRockAttach_{0}")]
		public async static Task AttachRockInZapier_Hangfire(long rockId, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rockModel = s.Get<RockModel>(rockId);
					var rockOwnerName = rockModel.AccountableUser.NotNull(x => x.GetName());
					var recurrenceModel = s.Get<L10Recurrence>(recurrenceId);


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, rockModel.OrganizationId, ZapierEvents.attach_rock)
														  .GetSubscriptionResponses(rockId, rockModel.AccountableUser.Id, recurrenceId, x => x.ViewRock(rockId).ViewL10Recurrence(recurrenceId), ctx => new {
															  rock_id = rockId,
															  rock_title = rockModel.Name,
															  rock_owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(rockModel.AccountableUser.Id, false), rockModel.AccountableUser.Id, null),
															  rock_owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(rockModel.AccountableUser.Id, false), rockOwnerName, null),
															  meeting_id = recurrenceId,
															  meeting_name = recurrenceModel.Name,
														  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}

		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId) {
			// Noop
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			// Noop
		}

		#endregion

		#region Issue
		public async Task CreateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, issue.Issue.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierIssue_Hangfire(issue.Id, ZapierEvents.new_issue));
			}
		}

		public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue, IIssueHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, issue.Issue.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierIssue_Hangfire(issue.Id, ZapierEvents.update_issue));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierIssue_{0}")]
		public async static Task ZapierIssue_Hangfire(long issueRecurrenceId, ZapierEvents @event) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);


					var meeting_id = issueRecur.Recurrence.NotNull(x => (long?)x.Id);
					string meeting_name = null;
					if (meeting_id != null) {
						meeting_name = s.Get<L10Recurrence>(meeting_id.Value).Name;
					}

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, issueRecur.Issue.Organization.Id, @event)
															  .GetSubscriptionResponses(issueRecurrenceId, issueRecur.Owner.Id, issueRecur.Recurrence.Id, x => x.ViewIssue(issueRecur.Issue.Id), ctx => new {
																  id = issueRecur.Id,
																  title = issueRecur.Issue.Message,
																  create_time = issueRecur.CreateTime,
																  complete = issueRecur.CloseTime != null,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(issueRecur.Owner.Id, false), issueRecur.Owner.Id, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(issueRecur.Owner.Id, false), issueRecur.Owner.Name, null),
																  meeting_id = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), meeting_id, null),
																  meeting_name = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), meeting_name, null),
															  });

					await PostEventToZapier(subscriptionResponses);
				}
			}
		}
		#endregion

		#region Headlines
		public async Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, headline.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierHeadline_Hangfire(headline.Id, ZapierEvents.new_headline));
			}
		}
		public async Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, headline.OrganizationId)) {
				var angularHeadline = new AngularHeadline(headline);
				Scheduler.Enqueue(() => ZapierHeadline_Hangfire(headline.Id, ZapierEvents.update_headline));
			}
		}
		public async Task ArchiveHeadline(ISession s, PeopleHeadline headline) {
			// Noop
		}

		public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline) {
			// Noop
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierHeadline_{0}")]
		public async static Task ZapierHeadline_Hangfire(long headlineId, ZapierEvents @event) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var headline = s.Get<PeopleHeadline>(headlineId);
					var meeting_id = headline.RecurrenceId;
					string meeting_name = s.Get<L10Recurrence>(meeting_id).Name;
					string owner_name = headline.Owner.NotNull(x => x.GetName());

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, headline.OrganizationId, @event)
															  .GetSubscriptionResponses(headline.Id, headline.OwnerId, headline.RecurrenceId, x => x.ViewHeadline(headline.Id), ctx => new {
																  id = headline.Id,
																  title = headline.Message,
																  create_time = headline.CreateTime,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(headline.OwnerId, false), headline.OwnerId, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(headline.OwnerId, false), owner_name, null),
																  meeting_id = ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id), (long?)meeting_id, null),
																  meeting_name = ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id), meeting_name, null),
															  });

					await PostEventToZapier(subscriptionResponses);
				}
			}
		}

		#endregion

		#region Measurable	

		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> createdScores) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, measurable.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierMeasurable_Hangfire(measurable.Id, ZapierEvents.new_measurable));
			}
		}
		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, measurable.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierMeasurable_Hangfire(measurable.Id, ZapierEvents.update_measurable));
			}
		}
		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			// Noop
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierMeasurable_{0}")]
		public async static Task ZapierMeasurable_Hangfire(long measurableId, ZapierEvents @event) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {


					var measurableModel = s.Get<MeasurableModel>(measurableId);
					L10Recurrence recurAlias = null;
					var recurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
										.JoinAlias(x => x.L10Recurrence, () => recurAlias)
										.Where(x => x.Measurable.Id == measurableId && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
										.List<object[]>()
										.Select(x => new { Name = (string)x[1], Id = (long)x[0] })
										.ToArray();

					var recurrenceIds = recurrences.Select(x => x.Id).ToArray();

					var ownerName = measurableModel.AccountableUser.NotNull(x => x.GetName());


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, measurableModel.OrganizationId, @event)
															  .GetSubscriptionResponses(measurableId, measurableModel.AccountableUserId, recurrenceIds, x => x.ViewMeasurable(measurableId), ctx => new {
																  id = measurableId,
																  title = measurableModel.Title,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableModel.AccountableUserId, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), ownerName, null),
																  create_time = measurableModel.CreateTime,
																  target = measurableModel.Goal,
																  target_alt = measurableModel.AlternateGoal,
																  target_dir = measurableModel.GoalDirection,
																  units = measurableModel.UnitType
															  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}

		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, measurable.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierMeasurableAttach_Hangfire(measurable.Id, recurMeasurable.L10Recurrence.Id));
			}
		}
		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			// Noop
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierMeasurableAttach_{0}")]
		public async static Task ZapierMeasurableAttach_Hangfire(long measurableId, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var measurableModel = s.Get<MeasurableModel>(measurableId);
					var measurableOwnerName = measurableModel.AccountableUser.NotNull(x => x.GetName());
					var recurrenceModel = s.Get<L10Recurrence>(recurrenceId);


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, measurableModel.OrganizationId, ZapierEvents.attach_measurable)
														  .GetSubscriptionResponses(measurableId, measurableModel.AccountableUserId, recurrenceId, x => x.ViewMeasurable(measurableId).ViewL10Recurrence(recurrenceId), ctx => new {
															  measurable_id = measurableId,
															  measurable_title = measurableModel.Title,
															  measurable_owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableModel.AccountableUserId, null),
															  measurable_owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableOwnerName, null),
															  meeting_id = recurrenceId,
															  meeting_name = recurrenceModel.Name,
														  });
					await PostEventToZapier(subscriptionResponses);

				}
			}
		}
		#endregion

		#region Scores
		public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			var enabledOrgs = scoreAndUpdates.Select(x => x.score.OrganizationId)
											 .Distinct()
											 .Where(x => ZapierAccessor.IsZapierEnabled_Unsafe(s, x))
											 .ToList();

			var enabledScores = scoreAndUpdates.Where(x => enabledOrgs.Contains(x.score.OrganizationId)).ToList();

			if (enabledScores.Any()) {
				foreach (var scoreId in enabledScores.Select(x => x.score.Id).Distinct()) {
					Scheduler.Enqueue(() => ZapierScore_Hangfire(scoreId));
				}
			}
		}


		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierScore_{0}")]
		public async static Task ZapierScore_Hangfire(long scoreId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var scoreModel = s.Get<ScoreModel>(scoreId);
					var measurableModel = scoreModel.Measurable;
					var measurableOwner = measurableModel.AccountableUser.NotNull(x => x.GetName());
					//var score = new AngularRock(rockMoscoreModeldel, null);

					L10Recurrence recurAlias = null;
					var recurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
										.JoinAlias(x => x.L10Recurrence, () => recurAlias)
										.Where(x => x.Measurable.Id == measurableModel.Id && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
										.List<object[]>()
										.Select(x => new { Name = (string)x[1], Id = (long)x[0] })
										.ToArray();

					var recurrenceIds = recurrences.Select(x => x.Id).ToArray();


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, measurableModel.OrganizationId, ZapierEvents.update_score)
															  .GetSubscriptionResponses(measurableModel.Id, measurableModel.AccountableUserId, recurrenceIds, x => x.ViewScore(scoreModel.Id), ctx => new {
																  id = scoreModel.Id,
																  title = measurableModel.Title,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableModel.AccountableUserId, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableOwner, null),
																  week = scoreModel.DataContract_ForWeek,
																  measurable_id = measurableModel.Id,
																  date_entered = scoreModel.DateEntered,
																  value = scoreModel.Measured,
																  target = scoreModel.OriginalGoal,
																  target_alt = scoreModel.AlternateOriginalGoal,
																  target_dir = measurableModel.GoalDirection,
																  units = measurableModel.UnitType,
																  target_met = scoreModel.MetGoal()
															  });
					await PostEventToZapier(subscriptionResponses);

					//					id = score.Id,
					//					type = score.Type,
					//					key = score.Key,
					//					for_week = score.ForWeek,
					//					week = score.Week,
					//					measurable_id = score.MeasurableId,
					//					date_entered = score.DateEntered,
					//					measured = score.Measured,
					//					disabled = score.Disabled,
					//					direction = score.Direction,
					//					target = score.Target,
					//					alt_target = score.AltTarget,
					//					meeting_id = recurrenceId,
					//					meeting_name = recurrenceName,




					//var scoreLookup = s.QueryOver<ScoreModel>()
					//	.WhereRestrictionOn(x => x.Id).IsIn(scoreIds.Distinct().ToList())
					//	.List().ToList()
					//	.ToDefaultDictionary(x => x.Id, x => x);


					//var measurableIds = scoreLookup.Select(x => x.Value.MeasurableId).Distinct().ToList();

					//L10Recurrence recurAlias = null;
					//var recurrenceMeasurableData = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
					//							.JoinAlias(x => x.L10Recurrence, () => recurAlias)
					//							.WhereRestrictionOn(x => x.Measurable.Id).IsIn(measurableIds)
					//							.Where(x => x.DeleteTime == null && recurAlias.DeleteTime == null)
					//							.Select(x => recurAlias.Id, x => recurAlias.Name, x => x.Measurable.Id)
					//							.List<object[]>()
					//							.Select(x => new {
					//								RecurrenceId = (long)x[0],
					//								Name = (string)x[1],
					//								MeasurableId = (long)x[2]
					//							}).ToList();


					//var zapierSubscriptionsCache = new DefaultDictionary<Tuple<long, long, long>, List<ZapierSubscription>>(x => {
					//	var organizationId = x.Item1;
					//	var accountableUserId = x.Item2;
					//	var recurrenceId = x.Item3;
					//	return ZapierAccessor.GetListZapier_unsafe(s, organizationId, accountableUserId, recurrenceId, ZapierEvents.update_score);
					//});

					//var userPermCache = new DefaultDictionary<long, PermissionsUtility>(x => PermissionsUtility.Create(s, s.Get<UserOrganizationModel>(x)));
					//var canViewMeasurable = new DefaultDictionary<Tuple<long, long>, bool>(x => {
					//	var userId = x.Item1;
					//	var measurableId = x.Item2;
					//	var userPerms = userPermCache[userId];
					//	return userPerms.IsPermitted(y => y.ViewMeasurable(measurableId));
					//});


					//foreach (var scoreId in scoreIds) {
					//	var score = new AngularScore(scoreLookup[scoreId], null, false);
					//	//var latestScore = s.Get<ScoreModel>(score.Id).NotNull(x => x.Measured);

					//	var organizationId = score.OrganizationId;
					//	var accountableUserId = score.AccountableUserId;
					//	var measurableId = score.MeasurableId;

					//	var attachedRecurrences = recurrenceMeasurableData.Where(x => x.MeasurableId == measurableId).ToList();


					//	if (attachedRecurrences.Any()) {
					//		foreach (var recurrenceData in attachedRecurrences) {
					//			var recurrenceId = recurrenceData.RecurrenceId;
					//			var recurrenceName = recurrenceData.Name;
					//			var zapierSubcriptions = zapierSubscriptionsCache[Tuple.Create(organizationId, accountableUserId, recurrenceId)];//ZapierAccessor.GetListZapier_unsafe(s, organizationId, accountableUserId, recurrenceId, "update_score");
					//			var zapierHookToPost = new List<ZapierSubscription>();
					//			foreach (var zap in zapierSubcriptions) {
					//				try {
					//					//ensure the creator of this zap subscription has permission to view resource
					//					if (canViewMeasurable[Tuple.Create(zap.UserOrgId, measurableId)]) {
					//						zapierHookToPost.Add(zap);
					//					}
					//				} catch {

					//				}
					//			}
					//			if (zapierHookToPost.Any()) {
					//				await PostEventToZapier(zapierHookToPost, new {
					//					id = score.Id,
					//					type = score.Type,
					//					key = score.Key,
					//					for_week = score.ForWeek,
					//					week = score.Week,
					//					measurable_id = score.MeasurableId,
					//					date_entered = score.DateEntered,
					//					measured = score.Measured,
					//					disabled = score.Disabled,
					//					direction = score.Direction,
					//					target = score.Target,
					//					alt_target = score.AltTarget,
					//					meeting_id = recurrenceId,
					//					meeting_name = recurrenceName,
					//				});
					//			}
					//		}
					//	}
					//}
				}
			}
		}

		public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			//Noop
		}

		public async Task RemoveFormula(ISession ses, long measurableId) {
			//Noop
		}

		public async Task PreSaveRemoveFormula(ISession s, long measurableId) {
			//Noop
		}
		#endregion




		private async static Task PostEventToZapier(List<ZapierSubscriptionQuery.SubscriptionResponse> subscriptionResponses) {
			//try {
			if (subscriptionResponses.Any()) {
				//send to any subscriptions to this event for this user to zapier
				using (HttpClient httpClient = new HttpClient()) {
					foreach (var sr in subscriptionResponses) {
						//Somehow this has to be here otherwise it gets disposed on next web request that cause failure
						StringContent httpContent = new StringContent(sr.Serialized, Encoding.UTF8, "application/json");
						HttpResponseMessage response = await httpClient.PostAsync(sr.Subscription.TargetUrl, httpContent);

					}
				}
			}
			//} catch (Exception e) {
			//	log.Error("[Zapier] " + e.Message, e);
			//	// Do Nothing
			//}

		}

		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}


	}
}
