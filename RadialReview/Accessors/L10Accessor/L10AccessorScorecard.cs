using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.SqlCommand;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
//using System.Web.WebPages.Html;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using Microsoft.VisualStudio.Profiler;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {
		#region Scorecard	

		#region Models   
		public class ScorecardData {
			public List<ScoreModel> Scores { get; set; }
			public List<MeasurableModel> Measurables { get; set; }
			public List<L10Recurrence.L10Recurrence_Measurable> MeasurablesAndDividers { get; set; }
			public ITimeData TimeSettings { get; set; }

			public ScorecardData() { }
			public static ScorecardData FromScores(List<ScoreModel> scores) {
				return new ScorecardData() {
					Scores = scores,
					Measurables = scores.GroupBy(x => x.MeasurableId).Select(x => x.First().Measurable).ToList(),
					MeasurablesAndDividers = scores.GroupBy(x => x.MeasurableId).Select(x => new L10Recurrence.L10Recurrence_Measurable() {
						Measurable = x.First().Measurable
					}).ToList(),

				};
			}
		}
		#endregion

		#region Attach
		public static async Task AttachMeasurable(UserOrganizationModel caller, long recurrenceId, long measurableId, bool skipRealTime = false, int? rowNum = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);

						await AttachMeasurable(s, perms, recurrenceId, measurableId, skipRealTime, rowNum);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static async Task AttachMeasurable(ISession s, PermissionsUtility perm, long recurrenceId, long measurableId, bool skipRealTime = false, int? rowNum = null, DateTime? now = null) {
			perm.AdminL10Recurrence(recurrenceId);
			var measurable = s.Get<MeasurableModel>(measurableId);
			if (measurable == null) {
				throw new PermissionsException("Measurable does not exist.");
			}

			perm.ViewMeasurable(measurable.Id);

			var alreadyExist = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.Measurable.Id == measurableId).RowCount() > 0;
			if (alreadyExist) {
				throw new PermissionsException("Measurable already attached to meeting");
			}

			if (rowNum == null) {
				var orders = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).Select(x => x._Ordering).List<int>().ToList();
				if (orders.Any()) {
					rowNum = orders.Max() + 1;
				}
			}


			var rm = new L10Recurrence.L10Recurrence_Measurable() {
				CreateTime = now ?? DateTime.UtcNow,
				L10Recurrence = s.Load<L10Recurrence>(recurrenceId),
				Measurable = measurable,
				_Ordering = rowNum ?? 0
			};
			s.Save(rm);

			await HooksRegistry.Each<IMeetingMeasurableHook>((ses, x) => x.AttachMeasurable(ses, perm.GetCaller(), measurable, rm));
		}
		public static void CreateMeasurableDivider(UserOrganizationModel caller, long recurrenceId, int ordering = -1) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
					var recur = s.Get<L10Recurrence>(recurrenceId);
					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					var group = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

					var now = DateTime.UtcNow;

					var divider = new L10Recurrence.L10Recurrence_Measurable() {
						_Ordering = ordering,
						IsDivider = true,
						L10Recurrence = recur,
						Measurable = null,
					};

					s.Save(divider);


					var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
					//var l10Scores = L10Accessor.GetScoresForRecurrence(s, perm, recurrenceId);
					if (current != null) {


						var mm = new L10Meeting.L10Meeting_Measurable() {
							L10Meeting = current,
							Measurable = null,
							IsDivider = true,

						};
						s.Save(mm);

						var settings = current.Organization.Settings;
						var sow = settings.WeekStart;
						var offset = current.Organization.GetTimezoneOffset();
						var scorecardType = settings.ScorecardPeriod;

#pragma warning disable CS0618 // Type or member is obsolete
						var ts = current.Organization.GetTimeSettings();
#pragma warning restore CS0618 // Type or member is obsolete
						ts.Descending = recur.ReverseScorecard;

						var weeks = TimingUtility.GetPeriods(ts, now, current.StartTime, true);


						var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
						// var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();

						var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
							MeetingId = current.Id,
							RecurrenceId = recurrenceId,
							MeetingMeasurable = mm,
							IsDivider = true,
							Weeks = weeks
						});
						row.ViewData["row"] = rowId - 1;

						var first = row.Execute();
						row.ViewData["ShowRow"] = false;
						var second = row.Execute();
						group.addMeasurable(first, second);
					}
					var scorecard = new AngularScorecard(recurrenceId);
					scorecard.Measurables = new List<AngularMeasurable>() { AngularMeasurable.CreateDivider(divider) };
					scorecard.Scores = new List<AngularScore>();

					group.update(new AngularUpdate() { scorecard });

					Audit.L10Log(s, caller, recurrenceId, "CreateMeasurableDivider", ForModel.Create(divider));


					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion

		#region Get

		public static async Task<ScorecardData> GetOrGenerateScorecardDataForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null, bool getMeasurables = false, bool getScores = true, L10Recurrence.L10LookupCache queryCache = null, bool generateMissingData = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
#pragma warning disable CS0618 // Type or member is obsolete
					var ang = await GetOrGenerateScorecardDataForRecurrence(s, perm, recurrenceId, includeAutoGenerated: includeAutoGenerated, now: now, range: range, getMeasurables: getMeasurables, getScores: getScores, queryCache: queryCache, generateMissingData: false);
					tx.Commit();
					s.Flush();
#pragma warning restore CS0618 // Type or member is obsolete
					return ang;
				}
			}
		}


		public static async Task<List<ScoreModel>> GetOrGenerateScoresForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);//.ViewL10Recurrence(recurrenceId);
#pragma warning disable CS0618 // Type or member is obsolete
					var angulareRecur = await GetOrGenerateScoresForRecurrence(s, perm, recurrenceId);
					tx.Commit();
					s.Flush();
#pragma warning restore CS0618 // Type or member is obsolete
					return angulareRecur;
				}
			}
		}

		[Obsolete("Must call commit")]
		public static async Task<ScorecardData> GetOrGenerateScorecardDataForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId,
					bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null, bool getMeasurables = false, bool getScores = true, bool forceIncludeTodoCompletion = false, L10Recurrence.L10LookupCache queryCache = null,
					bool generateMissingData = true) {

			//DataCollection.MarkProfile(1);

			queryCache = queryCache ?? new L10Recurrence.L10LookupCache(recurrenceId);

			if (queryCache.RecurrenceId != recurrenceId) {
				throw new PermissionsException("Id does not match");
			}

			var now1 = now ?? DateTime.UtcNow;
			perm.ViewL10Recurrence(recurrenceId);

			if (forceIncludeTodoCompletion) {
				includeAutoGenerated = true;
			}

			var recurrenceMeasurables = queryCache.GetAllMeasurablesAndDividers(() => {
				MeasurableModel mAlias = null;
				return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
							.JoinAlias(x => x.Measurable, () => mAlias, JoinType.LeftOuterJoin)
							.Fetch(x => x.Measurable).Lazy
							.Fetch(x => x.L10Recurrence).Lazy
							.Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null && (x.Measurable == null || mAlias.DeleteTime == null))
							.List().ToList();
			});

			var measurableModels = recurrenceMeasurables.Where(x => x.Measurable != null).Distinct(x => x.Measurable.Id).Select(x => x.Measurable).ToList();
			var measurables = measurableModels.Select(x => x.Id).ToList();

			var scoreModels = new List<ScoreModel>();
			IEnumerable<ScoreModel> scoresF = null;

			if (getScores) {
				var scoresQ = s.QueryOver<ScoreModel>()
					.Where(x => x.DeleteTime == null)
					.Fetch(x => x.Measurable).Lazy;
				if (range != null) {
					var st = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
					var et = range.EndTime.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
					scoresQ = scoresQ.Where(x => x.ForWeek >= st && x.ForWeek <= et);
				}
				scoresF = scoresQ.WhereRestrictionOn(x => x.MeasurableId).IsIn(measurables).Future();
			}
			//List<MeasurableModel> measurableModels = null;
			//if (getMeasurables) {
			//    measurableModels = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(measurables).Future().ToList();
			//}
			if (getScores) {
				scoreModels = scoresF.ToList();
				if (scoreModels.Any() || range != null) {

					var rangeTemp = range;
					if (rangeTemp == null) {
						var minDate = Math2.Max(new DateTime(2013, 1, 1), scoreModels.Select(x => x.ForWeek).Min());
						var maxDate = Math2.Min(DateTime.UtcNow.AddDays(14), scoreModels.Select(x => x.ForWeek).Max());
						rangeTemp = new DateRange(minDate, maxDate);
					}

					if (generateMissingData) {
						var extra = await ScorecardAccessor._GenerateScoreModels_AddMissingScores_Unsafe(s, rangeTemp, measurables, scoreModels);
						scoreModels.AddRange(extra);
					}
				}
			}

			var recur = s.Get<L10Recurrence>(recurrenceId);

			var ts = perm.GetCaller().GetTimeSettings();
			//time setting overrides 
			ts.WeekStart = recur.StartOfWeekOverride ?? ts.WeekStart;
			ts.Descending = recur.ReverseScorecard;


			if (includeAutoGenerated && (recur.IncludeAggregateTodoCompletion || recur.IncludeIndividualTodos || forceIncludeTodoCompletion)) {
				var currentTime = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false).NotNull(x => x.StartTime);
				List<TodoModel> todoCompletion = null;
				todoCompletion = GetAllTodosForRecurrence(s, perm, recurrenceId);
				var periods = TimingUtility.GetPeriods(ts, now1, currentTime, true);
				//Hides the todo completion % if we're not in this week. Purely asthetic since the calculation is indeed correct.
				var hideCompletion = new Func<DateTime, bool>(weekStart => currentTime < weekStart);

				if (getScores && (recur.IncludeAggregateTodoCompletion || forceIncludeTodoCompletion)) {
					var todoScores = CalculateAggregateTodoCompletionScores(ts, todoCompletion, periods, hideCompletion, currentTime);
					scoreModels.AddRange(todoScores);
				}

				if (getScores && (recur.IncludeIndividualTodos || forceIncludeTodoCompletion)) {
					var individualTodoScores = CalculateIndividualTodoCompletionScores(ts, todoCompletion, periods, hideCompletion, currentTime);
					scoreModels.AddRange(individualTodoScores);
				}
			}

			var userQueries = scoreModels.SelectMany(x => {
				var o = new List<long>(){
					x.Measurable.AccountableUser.NotNull(y => y.Id),
					x.AccountableUser.NotNull(y => y.Id),
					x.Measurable.AdminUser.NotNull(y => y.Id),
				};
				return o;
			}).Distinct().ToList();

			IEnumerable<UserOrganizationModel> __allUsers = null;
			if (getScores) {
				var allUserIds = userQueries;
				__allUsers = s.QueryOver<UserOrganizationModel>()
					.Fetch(x => x.Positions).Lazy
					.Fetch(x => x.Organization).Lazy
					.Fetch(x => x.Reviews).Lazy
					.Fetch(x => x.Cache).Eager
					.WhereRestrictionOn(x => x.Id)
					.IsIn(allUserIds)
					.Future();
			}


			//CUMULATIVE
			if (getMeasurables) {
				if (__allUsers != null) {
					__allUsers.ToList();
				}

				_RecalculateCumulative_Unsafe(s, null, measurableModels, recur.AsList());
			}

			//Touch 
			if (getScores) {
				if (__allUsers != null) {
					var __allUsersResolved = __allUsers.ToList();
				}


				foreach (var a in scoreModels) {
					try {
						if (a.Measurable != null) {
							var i = a.Measurable.Goal;
							if (a.Measurable.AccountableUser != null) {
								var u = a.Measurable.AccountableUser.GetName();
								var v = a.Measurable.AccountableUser.ImageUrl(true);
							}
							if (a.Measurable.AdminUser != null) {
								var u1 = a.Measurable.AdminUser.GetName();
								var v1 = a.Measurable.AdminUser.ImageUrl(true);
							}

							if (a.Measurable.HasFormula) {
								a._Editable = false;
							}

						}
						if (a.AccountableUser != null) {
							var j = a.AccountableUser.GetName();
							var k = a.AccountableUser.ImageUrl(true);
						}
					} catch (Exception) {
						//Opps
					}
				}
			}

			if (recur.PreventEditingUnownedMeasurables) {
				var userId = perm.GetCaller().Id;
				scoreModels.ForEach(x => {
					if (x.Measurable != null) {
						x._Editable = x.Measurable.AccountableUserId == userId || x.Measurable.AdminUserId == userId;
					}
				});
				if (getMeasurables) {
					measurableModels.ForEach(x => x._Editable = x.AccountableUserId == userId || x.AdminUserId == userId);
				}
				recurrenceMeasurables.ForEach(x => {
					if (x.Measurable != null) {
						x.Measurable._Editable = x.Measurable.AccountableUserId == userId || x.Measurable.AdminUserId == userId;
					}
				});
			}

			//DataCollection.MarkProfile(2);
			return new ScorecardData() {
				Scores = scoreModels,
				Measurables = measurableModels,
				MeasurablesAndDividers = recurrenceMeasurables,
				TimeSettings = ts
			};
		}

		public static IEnumerable<ScoreModel> CalculateIndividualTodoCompletionScores(TimeSettings ts, List<TodoModel> allTodos, List<L10MeetingVM.WeekVM> periods, Func<DateTime, bool> hideCompletion, DateTime? currentTime = null) {
			return periods.SelectMany(period => {
				return allTodos.GroupBy(x => x.AccountableUserId).SelectMany(todos => {
					var a = todos.First().AccountableUser;
					try {
						var rangeTodos = TimingUtility.GetRange(ts, period);
						var ss = GetTodoCompletion(todos.ToList(), rangeTodos.StartTime, rangeTodos.EndTime, currentTime);
						decimal? percent = null;
						if (ss.IsValid()) {
							percent = Math.Round(ss.GetValue(0) * 100m, 1);
						}

						if (hideCompletion != null && hideCompletion(rangeTodos.StartTime)) {
							percent = null;
						}

						var mm = GenerateTodoMeasureable(a);
						return new ScoreModel() {
							_Editable = false,
							AccountableUserId = a.Id,
							ForWeek = period.ForWeek,
							Measurable = mm,
							Measured = percent,
							MeasurableId = mm.Id,
							OriginalGoal = mm.Goal,
							OriginalGoalDirection = mm.GoalDirection

						}.AsList();
					} catch (Exception) {
						return new List<ScoreModel>();
					}
				});
			});
		}

		public static IEnumerable<ScoreModel> CalculateAggregateTodoCompletionScores(TimeSettings ts, List<TodoModel> allTodos, List<L10MeetingVM.WeekVM> periods, Func<DateTime, bool> hideCompletion, DateTime? currentTime = null) {
			return periods.SelectMany(period => {
				try {
					var rangeTodos = TimingUtility.GetRange(ts, period);
					var ss = GetTodoCompletion(allTodos, rangeTodos.StartTime, rangeTodos.EndTime, currentTime);
					decimal? percent = null;
					if (ss.IsValid()) {
						percent = Math.Round(ss.GetValue(0) * 100m, 1);
					}
					if (hideCompletion != null && hideCompletion(rangeTodos.StartTime)) {
						percent = null;
					}

					return new ScoreModel() {
						_Editable = false,
						AccountableUserId = -1,
						ForWeek = period.ForWeek,
						Measurable = TodoMeasurable,
						Measured = percent,
						MeasurableId = TodoMeasurable.Id,
						OriginalGoalDirection = TodoMeasurable.GoalDirection,
						OriginalGoal = TodoMeasurable.Goal
					}.AsList();
				} catch (Exception) {
					return new List<ScoreModel>();
				}
			});
		}

		[Obsolete("Must call commit")]
		public static async Task<List<ScoreModel>> GetOrGenerateScoresForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId, bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null) {
			var sam = await GetOrGenerateScorecardDataForRecurrence(s, perm, recurrenceId, includeAutoGenerated, now, range);
			return sam.Scores;
		}

		public class MeasurableCount {
			public int Measurables { get; set; }
			public int Dividers { get; set; }
		}

		public static MeasurableCount GetMeasurableCount(ISession s, PermissionsUtility perms, long recurrenceId) {
			perms.ViewL10Recurrence(recurrenceId);

			MeasurableModel mAlias = null;

			var dividers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
				.JoinAlias(x => x.Measurable, () => mAlias)
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && (x.Measurable == null || mAlias.DeleteTime == null))
				.Select(x => x.IsDivider)
				.List<bool>()
				.ToList();



			return new MeasurableCount {
				Measurables = dividers.Count(isDivider => !isDivider),
				Dividers = dividers.Count(isDivider => isDivider),
			};
		}

		public static List<Tuple<long, int?, bool>> GetMeasurableOrdering(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
						.Select(x => x.Measurable.Id, x => x._Ordering, x => x.IsDivider)
						.List<object[]>()
						.Where(x => x[0] != null)
						.Select(x => {
							return Tuple.Create((long)x[0], (int?)x[1], (bool)x[2]);
						}).ToList();

				}
			}
		}
		public class MeasurableRecurrence {
			public long RecurrenceId { get; set; }
			public string RecurrenceName { get; set; }
			public bool CanAdmin { get; set; }
		}

		public static async Task<List<MeasurableRecurrence>> GetMeasurableRecurrences(UserOrganizationModel caller, long measurableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewMeasurable(measurableId);

					L10Recurrence recurAlias = null;
					var meetingMeas = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.JoinAlias(x => x.L10Recurrence, () => recurAlias)
						.Where(x => x.DeleteTime == null && x.IsDivider == false && x.Measurable.Id == measurableId && recurAlias.DeleteTime == null)
						.Fetch(x => x.L10Recurrence).Eager
						.List();

					var res = new List<MeasurableRecurrence>();
					foreach (var m in meetingMeas) {
						var canAdmin = perms.IsPermitted(x => x.AdminL10Recurrence(m.L10Recurrence.Id));
						res.Add(new MeasurableRecurrence() {
							CanAdmin = canAdmin,
							RecurrenceId = m.L10Recurrence.Id,
							RecurrenceName = m.L10Recurrence.Name
						});
					}

					return res.Distinct(x => x.RecurrenceId).ToList();
				}
			}
		}

		public static List<L10Recurrence.L10Recurrence_Measurable> GetMeasurablesForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					MeasurableModel measurable = null;
					var q = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.JoinAlias(x => x.Measurable, () => measurable).Where(x => x.L10Recurrence.Id == recurrenceId);

					q = q.Where(x => x.DeleteTime == null && measurable.DeleteTime == null);
					var found = q.Fetch(x => x.Measurable).Eager.List().ToList();
					foreach (var f in found) {
						if (f.Measurable.AccountableUser != null) {
							var a = f.Measurable.AccountableUser.GetName();
							var b = f.Measurable.AccountableUser.ImageUrl(true, ImageSize._32);
						}
					}
					return found;
				}
			}
		}
		#endregion

		#region Update
		[Untested("ESA")]
		public static async Task SetMeetingMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Meeting_Measurables) {
			//using (var s = HibernateSession.GetCurrentSession()) {
			//	using (var tx = s.BeginTransaction()) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.MeasurableReorder(recurrenceId), async s => {
				PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

				//SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

				var l10measurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>().WhereRestrictionOn(x => x.Id).IsIn(orderedL10Meeting_Measurables).Where(x => x.DeleteTime == null).List().ToList();

				if (!l10measurables.Any()) {
					throw new PermissionsException("None found.");
				}

				if (l10measurables.GroupBy(x => x.L10Meeting.Id).Count() > 1) {
					throw new PermissionsException("Measurables must be part of the same meeting");
				}

				if (l10measurables.First().L10Meeting.L10RecurrenceId != recurrenceId) {
					throw new PermissionsException("Not part of the specified L10");
				}

				var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

				for (var i = 0; i < orderedL10Meeting_Measurables.Count; i++) {
					var id = orderedL10Meeting_Measurables[i];
					var f = l10measurables.FirstOrDefault(x => x.Id == id);
					if (f != null) {
						f._Ordering = i;
						s.Update(f);
						var g = recurMeasurables.FirstOrDefault(x => (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) || ((x.Measurable == null && f.Measurable == null) && !x._WasModified));
						if (g != null) {
							g._WasModified = true;
							g._Ordering = i;
							s.Update(g);
						}
					}
				}

				Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering", null);

				//tx.Commit();
				//s.Flush();

				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

				group.reorderMeasurables(orderedL10Meeting_Measurables);

				var updates = new AngularUpdate();
				foreach (var x in recurMeasurables) {
					if (x.IsDivider) {
						updates.Add(AngularMeasurable.CreateDivider(x));
					} else {
						updates.Add(new AngularMeasurable(x.Measurable) { Ordering = x._Ordering });
					}
				}
				group.update(updates);
				//	}
				//}
			});
		}
		public static async Task SetRecurrenceMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Recurrene_Measurables) {
			//using (var s = HibernateSession.GetCurrentSession()) {
			//	using (var tx = s.BeginTransaction()) {
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.MeasurableReorder(recurrenceId), async s => {
				var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);


				//SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

				/*var l10measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                    .WhereRestrictionOn(x => x.Measurable.Id).IsIn(orderedL10Recurrene_Measurables)
                    .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                    .List().ToList();*/
				MeasurableModel mm = null;

				var l10RecurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().JoinAlias(p => p.Measurable, () => mm)
					.WhereRestrictionOn(() => mm.Id)
					.IsIn(orderedL10Recurrene_Measurables.Where(x => x >= 0).ToArray())
					.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
					.List<L10Recurrence.L10Recurrence_Measurable>();

				var dividers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
					.WhereRestrictionOn(x => x.Id)
					.IsIn(orderedL10Recurrene_Measurables.Where(x => x < 0).Select(x => -x).ToArray())
					.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
					.List<L10Recurrence.L10Recurrence_Measurable>();

				if (!l10RecurMeasurables.Any()) {
					throw new PermissionsException("None found.");
				}

				if (l10RecurMeasurables.GroupBy(x => x.L10Recurrence.Id).Count() > 1) {
					throw new PermissionsException("Measurables must be part of the same meeting");
				}

				if (l10RecurMeasurables.First().L10Recurrence.Id != recurrenceId) {
					throw new PermissionsException("Not part of the specified L10");
				}

				var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

				var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

				var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
				if (meeting != null) {
					var l10MeetingMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.List().ToList();/*.JoinAlias(p => p.Measurable, () => mm)
							.WhereRestrictionOn(() => mm.Id)
							.IsIn(orderedL10Recurrene_Measurables)
							.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
							.List<L10Meeting.L10Meeting_Measurable>();*/




					var orderedL10Meeting_Measurables = new List<long>();
					for (var i = 0; i < orderedL10Recurrene_Measurables.Count; i++) {
						var id = orderedL10Recurrene_Measurables[i];
						var f = l10MeetingMeasurables.FirstOrDefault(x => (x.Measurable != null && x.Measurable.Id == id) || (x.Measurable == null && !x._WasModified));
						if (f != null) {
							f._WasModified = true;
							f._Ordering = i;
							s.Update(f);
							/*var g = l10MeetingMeasurables.FirstOrDefault(x => 
                                (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) 
                                || ((x.Measurable == null && f.Measurable == null) && !x._WasModified));
                            if (g != null)
                            {
                                g._WasModified = true;
                                g._Ordering = i;
                                s.Update(g);
                            }*/
							orderedL10Meeting_Measurables.Add(f.Id);

						}
					}

					group.reorderMeasurables(orderedL10Meeting_Measurables);
				}

				for (var i = 0; i < orderedL10Recurrene_Measurables.Count; i++) {
					var id = orderedL10Recurrene_Measurables[i];
					var f = l10RecurMeasurables.FirstOrDefault(x => x.Measurable.Id == id) ?? dividers.FirstOrDefault(x => x.Id == -id);
					if (f != null) {
						f._Ordering = i;
						s.Update(f);
						/*var g = recurMeasurables.FirstOrDefault(x => (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) || (x.Measurable == null && f.Measurable == null && x.Id==f.Id));
                        if (g != null)
                        {
                            g._Ordering = i;
                            s.Update(g);
                        }*/
					} else {
						//int a = 0;
					}
				}

				Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering", null);

				//tx.Commit();
				//s.Flush();



				group.reorderRecurrenceMeasurables(orderedL10Recurrene_Measurables);

				var updates = new AngularUpdate();
				foreach (var x in recurMeasurables) {
					if (x.IsDivider) {
						updates.Add(AngularMeasurable.CreateDivider(x));
					} else {
						updates.Add(new AngularMeasurable(x.Measurable) { Ordering = x._Ordering });
					}
				}
				group.update(updates);

			});
			//	}
			//}
		}
		#endregion

		#region Detatch
		public static async Task DetachMeasurable(UserOrganizationModel caller, long recurrenceId, long measurableId, bool archiveIfNoLongerInMeetings = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						await DetachMeasurable(s, perms, rt, recurrenceId, measurableId, archiveIfNoLongerInMeetings);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static async Task DetachMeasurable(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long measurableId, bool archiveIfNoLongerInMeetings) {
			perm.AdminL10Recurrence(recurrenceId);
			//Probably only one...
			var meetingMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.Measurable.Id == measurableId)
				.List().ToList();

			if (!meetingMeasurables.Any()) {
				throw new PermissionsException("Measurable does not exist.").SkipRevert();
			}

			var now = DateTime.UtcNow;
			foreach (var r in meetingMeasurables) {
				r.DeleteTime = now;
				s.Update(r);
			}

			var cur = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);

			if (cur != null) {
				var mmeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null && x.L10Meeting.Id == cur.Id && x.Measurable.Id == measurableId)
					.List().ToList();
				foreach (var r in mmeasurables) {
					r.DeleteTime = now;
					s.Update(r);
				}
			}

			s.Flush();
			if (archiveIfNoLongerInMeetings) {
				var measurableInOthers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId).RowCount();
				if (measurableInOthers == 0) {
					var measurable = s.Get<MeasurableModel>(measurableId);
					if (measurable.FromTemplateItemId == null) {
						perm.SetCache(true, "EditMeasurable", measurableId);
						await ScorecardAccessor.DeleteMeasurable(s, perm, measurableId);
					}
				}
			}

			foreach (var r in meetingMeasurables) {
				await HooksRegistry.Each<IMeetingMeasurableHook>((ses, x) => x.DetachMeasurable(ses, perm.GetCaller(), r.Measurable, recurrenceId));
			}
		}
		public static void DeleteMeetingMeasurableDivider(UserOrganizationModel caller, long l10Meeting_measurableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var divider = s.Get<L10Meeting.L10Meeting_Measurable>(l10Meeting_measurableId);
					if (divider == null) {
						throw new PermissionsException("Divider does not exist");
					}

					var recurrenceId = divider.L10Meeting.L10RecurrenceId;
					var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
					if (!divider.IsDivider) {
						throw new PermissionsException("Not a divider");
					}

					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					var group = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

					var matchingMeasurable = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.IsDivider && x._Ordering == divider._Ordering)
						.List().FirstOrDefault();

					var now = DateTime.UtcNow;
					divider.DeleteTime = now;

					if (matchingMeasurable != null) {
						matchingMeasurable.DeleteTime = now;
						s.Update(matchingMeasurable);
					} else {
					}

					s.Update(divider);
					tx.Commit();
					s.Flush();
					group.removeDivider(l10Meeting_measurableId);
				}
			}
		}
		#endregion

		#region Helpers

		public static void _RecalculateCumulative_Unsafe(ISession s, RealTimeUtility rt, MeasurableModel measurable, List<long> recurIds, ScoreModel updatedScore = null, bool forceNoSkip = true) {
			var recurs = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(recurIds).List().ToList();
			_RecalculateCumulative_Unsafe(s, rt, measurable.AsList(), recurs, updatedScore);
		}

		private class CalcScores {
			public class TinyScore {
				public DateTime ForWeek { get; set; }
				public decimal? Measured { get; set; }

			}
			public long MeasurableId { get; set; }
			public bool HasCumulative { get; set; }
			public bool HasAverage { get; set; }
			public List<TinyScore> Scores { get; set; }
		}

		public static void _RecalculateCumulative_Unsafe(ISession s, RealTimeUtility rt, List<MeasurableModel> measurables, List<L10Recurrence> recurs, ScoreModel updatedScore = null, bool forceNoSkip = true) {
			var scoresByMeasurable = new Dictionary<long, CalcScores>();
			{
				var scoreFutures = new Dictionary<long, IEnumerable<CalcScores.TinyScore>>();



				//Grab interesting scores
				foreach (var mm in measurables.Where(x => x.Id > 0 && ((x.ShowCumulative && x.CumulativeRange.HasValue) || (x.ShowAverage && x.AverageRange.HasValue))).Distinct(x => x.Id)) {

					var minDate = DateTime.MaxValue;
					if (mm.CumulativeRange.HasValue) {
						minDate = Math2.Min(minDate, mm.CumulativeRange.Value.AddDays(-7));
					}

					if (mm.AverageRange.HasValue) {
						minDate = Math2.Min(minDate, mm.AverageRange.Value.AddDays(-7));
					}

					scoreFutures[mm.Id] = s.QueryOver<ScoreModel>()
					.Where(x => x.MeasurableId == mm.Id && x.DeleteTime == null && x.Measured != null && (x.ForWeek > minDate))
					.Select(x => x.ForWeek, x => x.Measured)
					.Future<object[]>()
					.Select(x => new CalcScores.TinyScore() {
						ForWeek = (DateTime)x[0],
						Measured = (decimal?)x[1]
					});

					scoresByMeasurable[mm.Id] = new CalcScores() {
						HasAverage = mm.ShowAverage && mm.AverageRange.HasValue,
						HasCumulative = mm.ShowCumulative && mm.CumulativeRange.HasValue,
						MeasurableId = mm.Id,
					};
				}
				foreach (var k in scoresByMeasurable.Keys) {
					scoresByMeasurable[k].Scores = scoreFutures[k].ToList();
				}
			}

			var defaultDay = measurables.FirstOrDefault().NotNull(x => x.Organization.NotNull(y => y.Settings.WeekStart));

			//Set Cumulative Values
			if (recurs == null || recurs.Count == 0) {
				recurs = new List<L10Recurrence>() { null };
			}
			foreach (var recur in recurs) {
				var startOfWeek = defaultDay;
				if (recur != null) {
					startOfWeek = recur.StartOfWeekOverride ?? recur.Organization.Settings.WeekStart;
				}
				foreach (var k in scoresByMeasurable.Keys) {
					foreach (var mm in measurables.Where(x => x.Id == k).ToList()) {
						var measCalc = scoresByMeasurable[k];
						//Use the updated score if we have it.
						if (updatedScore != null) {
							for (var i = 0; i < measCalc.Scores.Count; i++) {
								if (updatedScore.ForWeek == measCalc.Scores[i].ForWeek) {
									measCalc.Scores[i] = new CalcScores.TinyScore {
										ForWeek = updatedScore.ForWeek,
										Measured = updatedScore.Measured
									};
								}
							}
						}

						if (measCalc.HasCumulative) {
							var foundScores = measCalc.Scores.Where(x => x.ForWeek > mm.CumulativeRange.Value.AddDays(-(int)startOfWeek)).ToList();
							mm._Cumulative = foundScores.GroupBy(x => x.ForWeek)
												.Select(x => x.FirstOrDefault(y => y.Measured != null).NotNull(y => y.Measured))
												.Where(x => x != null)
												.Sum();
						}
						if (measCalc.HasAverage) {
							var foundScores = measCalc.Scores.Where(x => x.ForWeek > mm.AverageRange.Value.AddDays(-(int)startOfWeek)).ToList();
							var interesting = foundScores.GroupBy(x => x.ForWeek)
												.Select(x => x.FirstOrDefault(y => y.Measured != null).NotNull(y => y.Measured))
												.Where(x => x != null);

							mm._Average = interesting.Any() ? interesting.Average() : null;

						}
					}
				}
			}

			if (rt != null) {
				foreach (var mm in measurables.Where(x => (x.ShowCumulative || x.ShowAverage) && x.Id > 0).Distinct(x => x.Id)) {
					rt.UpdateRecurrences(recurs.Where(x => x != null).Select(x => x.Id)).UpdateMeasurable(mm, forceNoSkip: forceNoSkip);
					rt.UpdateUsers(mm.AdminUserId, mm.AccountableUserId).Update(new AngularMeasurable(mm, true));
				}
			}

		}

		#endregion

		#endregion
	}
}
