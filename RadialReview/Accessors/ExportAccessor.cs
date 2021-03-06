﻿using Hangfire;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Downloads;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Accessors.L10Accessor;

namespace RadialReview.Accessors {
	public class ExportAccessor : BaseAccessor {
		public static async Task Scorecard(UserOrganizationModel caller, FileOrigin origin, long recurrenceId, DateRange range, int timezoneOffsetMinutes, string type = "csv") {
			//var scores = L10Accessor.GetScoresForRecurrence(caller, recurrenceId);
			long fileId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var recur = s.Get<L10Recurrence>(recurrenceId);
					fileId = FileAccessor.SaveGeneratedFilePlaceholder_Unsafe(
						s, caller.Id,
						recur.Name + " Scorecards", type,
						"generated " + DateTime.UtcNow.AddMinutes(-timezoneOffsetMinutes),
						origin,
						FileOutputMethod.Download,
						null,
						TagModel.Create("Scorecard"),
						TagModel.Create<L10Recurrence>(recurrenceId, "Scorecard")
					);

					tx.Commit();
					s.Flush();
				}
			}
			Scheduler.Enqueue(() => GenerateScorecard_Hangfire(caller.Id, fileId, recurrenceId, range, timezoneOffsetMinutes, type, FileNotification.NotifyCaller(caller)));
		}



		[AutomaticRetry(Attempts = 0)]
		[Queue(HangfireQueues.Immediate.GENERATE_SCORECARD)]
		public static async Task<bool> GenerateScorecard_Hangfire(long callerId, long fileId, long recurrenceId, DateRange range, int timezoneOffsetMinutes, string type, FileNotification notify) {
			UserOrganizationModel caller;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserOrganizationModel>(callerId);
				}
			}
			string fileContents = await _Scorecard(caller, recurrenceId, range, timezoneOffsetMinutes, type);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await FileAccessor.Save_Unsafe(s, fileId, fileContents.ToStream(), notify);
					tx.Commit();
					s.Flush();
				}
			}
			return true;
		}

		[Obsolete("User Scorecard instead.")]
		public static async Task<string> _Scorecard(UserOrganizationModel caller, long recurrenceId, DateRange range, int timezoneOffsetMinutes, string type) {
			var data = await L10Accessor.GetOrGenerateScorecardDataForRecurrence(caller, recurrenceId,generateMissingData:false);
			var fileContents = "";
			switch (type.ToLower()) {
				case "csv": {
						fileContents = GenerateScorecardCsv("Measurable", data, range, timezoneOffsetMinutes).ToCsv(false);
						break;
					}
				default:
					throw new Exception("Unrecognized Type");
			}

			return fileContents;
		}

		private static String TITLE = " ";
		private static String OWNER = "  ";
		private static String ADMIN = "   ";
		private static String GOAL = "    ";
		private static String GOALDIRECTION = "     ";

		private class ExportMeasurable {
			public ExportMeasurable(long ordering, long id, string title, string accountableUserName, string adminUserName, string goal, LessGreater? goalDirection) {
				Ordering = ordering;
				Id = id;
				Title = title;
				AccountableUserName = accountableUserName;
				AdminUserName = adminUserName;
				Goal = goal;
				GoalDirection = goalDirection.NotNull(x => x.Value.ToSymbol(true));
			}

			public long Ordering { get; internal set; }
			public long Id { get; internal set; }
			public string Title { get; internal set; }
			public string AccountableUserName { get; internal set; }
			public string AdminUserName { get; internal set; }
			public string Goal { get; internal set; }
			public string GoalDirection { get; internal set; }
		}
		private class ExportScore {
			public ExportScore(DateTime forWeek, long measurableId, decimal? measured, UnitType? units) {
				ForWeek = forWeek;
				MeasurableId = measurableId;
				Measured = measured.NotNull(x => units.Value.Format(x.Value)) ?? "";
			}

			public DateTime ForWeek { get; internal set; }
			public long MeasurableId { get; internal set; }
			public string Measured { get; set; }

		}

		private static Csv GenerateScorecardCsv(string title, DateRange range, int timezoneOffsetMinutes, List<ExportMeasurable> measurables, List<ExportScore> scores) {
			var csv = new Csv();
			csv.SetTitle("Measurable");

			var minWeek = DateTime.MinValue;
			if (scores.Any()) {
				var nonNull = scores.Where(x => x.Measured != null);
				if (nonNull.Any()) {
					minWeek = new DateTime(nonNull.Min(x => x.ForWeek.Ticks));
				}
			}

			//Establish headings
			foreach (var s in scores.OrderBy(x => x.ForWeek)) {
				if (s.ForWeek >= minWeek && range.Contains(s.ForWeek)) {
					var weekStart = s.ForWeek.AddDaysSafe(-6).AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
					var weekEnd = s.ForWeek.AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
					csv.Add("Date", TITLE, "Title");
					csv.Add("Date", OWNER, "Owner");
					csv.Add("Date", ADMIN, "Admin");
					csv.Add("Date", GOAL, "Goal");
					csv.Add("Date", GOALDIRECTION, "Goal Direction");
					csv.Add("Date", weekStart, weekEnd);
				}
			}

			//Add Measurables in order
			foreach (var s in measurables.OrderBy(x => x.Ordering)) {
				var measurable = s;
				if (measurable != null) {
					csv.Add(measurable.Id + "", TITLE, measurable.Title);
					csv.Add(measurable.Id + "", OWNER, measurable.AccountableUserName);
					csv.Add(measurable.Id + "", ADMIN, measurable.AdminUserName);
					csv.Add(measurable.Id + "", GOAL, "" + measurable.Goal);
					csv.Add(measurable.Id + "", GOALDIRECTION, "" + measurable.GoalDirection);
				}
			}

			//Populate scores
			foreach (var s in scores.OrderBy(x => x.ForWeek)) {
				if (s.ForWeek >= minWeek && range.Contains(s.ForWeek)) {
					var weekStart = s.ForWeek.AddDaysSafe(-6).AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
					csv.Add(s.MeasurableId + "", weekStart, s.Measured ?? "");
				}
			}
			return csv;
		}

		public static Csv GenerateScorecardCsv(string title, ScorecardData data, DateRange range, int timezoneOffsetMinutes) {

			var measurables = new Dictionary<long, ExportMeasurable>();
			foreach (var m in data.MeasurablesAndDividers) {
				var meas = m.Measurable;
				if (meas != null && !measurables.ContainsKey(meas.Id)) {

					measurables[m.Measurable.Id] = new ExportMeasurable(
						m._Ordering,
						meas.Id,
						meas.Title,
						meas.AccountableUser.NotNull(x => x.GetName()),
						meas.AdminUser.NotNull(x => x.GetName()),
						meas.Goal.NotNull(x => meas.UnitType.Format(x)),
						meas.GoalDirection);
				}
			}

			foreach (var s in data.Scores.Where(x => x.Measurable != null && x.Measurable.Id < 0).Distinct(x => x.MeasurableId)) {
				var meas = s.Measurable;
				if (meas != null && !measurables.ContainsKey(meas.Id)) {
					measurables[s.Measurable.Id] = new ExportMeasurable(
						long.MaxValue,
						meas.Id,
						meas.Title,
						meas.AccountableUser.NotNull(x => x.GetName()),
						meas.AdminUser.NotNull(x => x.GetName()),
						meas.Goal.NotNull(x => meas.UnitType.Format(x)),
						meas.GoalDirection);
				}
			}

			var scores = new List<ExportScore>();
			foreach (var s in data.Scores.OrderBy(x => x.ForWeek)) {
				scores.Add(new ExportScore(s.ForWeek, s.Measurable.Id, s.Measured, s.Measurable.UnitType));
			}

			return GenerateScorecardCsv(title, range, timezoneOffsetMinutes, measurables.Values.ToList(), scores);


			//var csv = new Csv();
			//csv.SetTitle("Measurable");

			//var minWeek = DateTime.MinValue;
			//if (data.Scores.Any()) {
			//	var nonNull = data.Scores.Where(x => x.Measured != null);
			//	if (nonNull.Any()) {
			//		minWeek = new DateTime(nonNull.Min(x => x.ForWeek.Ticks));
			//	}
			//}

			//foreach (var s in data.Scores.OrderBy(x => x.ForWeek)) {
			//	if (s.ForWeek >= minWeek && range.Contains(s.ForWeek)) {
			//		var weekStart = s.ForWeek.AddDaysSafe(-6).AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
			//		var weekEnd = s.ForWeek.AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
			//		csv.Add("Date", TITLE, "Title");
			//		csv.Add("Date", OWNER, "Owner");
			//		csv.Add("Date", ADMIN, "Admin");
			//		csv.Add("Date", GOAL, "Goal");
			//		csv.Add("Date", GOALDIRECTION, "Goal Direction");
			//		csv.Add("Date", weekStart, weekEnd);
			//	}
			//}

			//foreach (var s in data.MeasurablesAndDividers.OrderBy(x => x._Ordering)) {// scores.GroupBy(x => x.MeasurableId).OrderBy(x=>x.First().Measurable._Ordering)) {
			//	var measurable = s.Measurable;
			//	//var ss = s.First();
			//	if (measurable != null) {
			//		csv.Add(measurable.Id + "", TITLE			, measurable.Title);
			//		csv.Add(measurable.Id + "", OWNER			, measurable.AccountableUser.NotNull(x => x.GetName()));
			//		csv.Add(measurable.Id + "", ADMIN			, measurable.AdminUser.NotNull(x => x.GetName()));
			//		csv.Add(measurable.Id + "", GOAL			, "" + measurable.Goal.NotNull(x => measurable.UnitType.Format(x)));
			//		csv.Add(measurable.Id + "", GOALDIRECTION	, "" + measurable.GoalDirection);
			//	}
			//}

			//foreach (var s in data.Scores.Where(x => x.Measurable != null && x.Measurable.Id < 0).Distinct(x => x.MeasurableId)) {
			//	var measurable = s.Measurable;
			//	if (measurable != null) {
			//		csv.Add(measurable.Id + "", TITLE			, measurable.Title);
			//		csv.Add(measurable.Id + "", OWNER			, measurable.AccountableUser.NotNull(x => x.GetName()));
			//		csv.Add(measurable.Id + "", ADMIN			, measurable.AdminUser.NotNull(x => x.GetName()));
			//		csv.Add(measurable.Id + "", GOAL			, "" + measurable.Goal.NotNull(x => measurable.UnitType.Format(x)));
			//		csv.Add(measurable.Id + "", GOALDIRECTION	, "" + measurable.GoalDirection);
			//	}
			//}

			//foreach (var s in data.Scores.OrderBy(x => x.ForWeek)) {
			//	if (s.ForWeek >= minWeek && range.Contains(s.ForWeek)) {
			//		var weekStart = s.ForWeek.AddDaysSafe(-6).AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
			//		csv.Add(s.Measurable.Id + "", weekStart, s.Measured.NotNull(x => s.Measurable.UnitType.Format(x.Value)) ?? "");
			//	}
			//}
			//return csv;
		}

		public static Csv GenerateScorecardCsv(string title, AngularScorecard data, DateRange range, int timezoneOffsetMinutes) {

			var measurables = new Dictionary<long, ExportMeasurable>();


			foreach (var s in data.Measurables.OrderBy(x => x.Ordering)) {// scores.GroupBy(x => x.MeasurableId).OrderBy(x=>x.First().Measurable._Ordering)) {
				var measurable = s;
				//var ss = s.First();
				if (measurable != null && !measurables.ContainsKey(measurable.Id)) {

					measurables[measurable.Id] = new ExportMeasurable(
						measurable.Ordering ?? 0,
						measurable.Id,
						measurable.Name,
						measurable.Owner.NotNull(x => x.Name),
						measurable.Admin.NotNull(x => x.Name),
						 "" + measurable.Target.NotNull(x => measurable.Modifiers.Value.Format(x.Value)),
						 measurable.Direction

					);
				}
			}

			foreach (var s in data.Scores.Where(x => x.Measurable != null && x.Measurable.Id < 0).Distinct(x => x.MeasurableId)) {
				var measurable = s.Measurable;
				if (measurable != null && !measurables.ContainsKey(measurable.Id)) {
					measurables[measurable.Id] = new ExportMeasurable(
						measurable.Ordering ?? 0,
						measurable.Id,
						measurable.Name,
						measurable.Owner.NotNull(x => x.Name),
						measurable.Admin.NotNull(x => x.Name),
						 "" + measurable.Target.NotNull(x => measurable.Modifiers.Value.Format(x.Value)),
						 measurable.Direction
					);
				}
			}

			var scores = new List<ExportScore>();
			foreach (var s in data.Scores.OrderBy(x => x.ForWeek)) {
				scores.Add(new ExportScore(s.Week, s.MeasurableId, s.Measured, s.Measurable.NotNull(x => x.Modifiers)));
			}

			return GenerateScorecardCsv(title, range, timezoneOffsetMinutes, measurables.Values.ToList(), scores);


			//var csv = new Csv();
			//csv.SetTitle("Measurable");

			//var minWeek = DateTime.MinValue;
			//if (data.Scores.Any()) {
			//	var nonNull = data.Scores.Where(x => x.Measured != null);
			//	if (nonNull.Any()) {
			//		minWeek = new DateTime(nonNull.Min(x => x.Week.Ticks));
			//	}
			//}

			//foreach (var s in data.Scores.OrderBy(x => x.ForWeek)) {
			//	if (s.Week >= minWeek && range.Contains(s.Week)) {
			//		var weekStart = s.Week.AddDaysSafe(-6).AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
			//		var weekEnd = s.Week.AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
			//		csv.Add("Date", TITLE,		"Title");
			//		csv.Add("Date", OWNER,		"Owner");
			//		csv.Add("Date", ADMIN,		"Admin");
			//		csv.Add("Date", GOAL,		"Goal");
			//		csv.Add("Date", GOALDIRECTION, "GoalDirection");
			//		csv.Add("Date", weekStart, weekEnd);
			//	}
			//}

			//foreach (var s in data.Measurables.OrderBy(x => x.Ordering)) {// scores.GroupBy(x => x.MeasurableId).OrderBy(x=>x.First().Measurable._Ordering)) {
			//	var measurable = s;
			//	//var ss = s.First();
			//	if (measurable != null) {
			//		csv.Add(measurable.Id + "", TITLE			, measurable.Name);
			//		csv.Add(measurable.Id + "", OWNER			, measurable.Owner.NotNull(x => x.Name));
			//		csv.Add(measurable.Id + "", ADMIN			, measurable.Admin.NotNull(x => x.Name));
			//		csv.Add(measurable.Id + "", GOAL			, "" + measurable.Target.NotNull(x => measurable.Modifiers.Value.Format(x.Value)));
			//		csv.Add(measurable.Id + "", GOALDIRECTION	, "" + measurable.Direction.Value);
			//	}
			//}

			//foreach (var s in data.Scores.Where(x => x.Measurable != null && x.Measurable.Id < 0).Distinct(x => x.MeasurableId)) {
			//	var measurable = s.Measurable;
			//	if (measurable != null) {
			//		csv.Add(measurable.Id + "",TITLE			, measurable.Name);
			//		csv.Add(measurable.Id + "",OWNER			, measurable.Owner.NotNull(x => x.Name));
			//		csv.Add(measurable.Id + "",ADMIN			, measurable.Admin.NotNull(x => x.Name));
			//		csv.Add(measurable.Id + "",GOAL			, "" + measurable.Target.NotNull(x => measurable.Modifiers.Value.Format(x.Value)));
			//		csv.Add(measurable.Id + "",GOALDIRECTION, "" + measurable.Direction.Value);
			//	}
			//}

			//foreach (var s in data.Scores.OrderBy(x => x.ForWeek)) {
			//	if (s.Week >= minWeek && range.Contains(s.Week)) {
			//		var weekStart = s.Week.AddDaysSafe(-6).AddMinutes(-timezoneOffsetMinutes).ToShortDateString();
			//		csv.Add(s.Measurable.Id + "", s.Week.AddMinutes(-timezoneOffsetMinutes).ToShortDateString(), s.Measured.NotNull(x => s.Measurable.Modifiers.Value.Format(x.Value)) ?? "");
			//	}
			//}
			//return csv;
		}

		public static async Task<byte[]> TodoList(UserOrganizationModel caller, long recurrenceId, bool includeDetails) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var todos = L10Accessor.GetAllTodosForRecurrence(s, PermissionsUtility.Create(s, caller), recurrenceId);
					var csv = new Csv();

					Dictionary<string, string> padTexts = null;
					if (includeDetails) {
						try {
							var pads = todos.Select(x => x.PadId).ToList();
							padTexts = await PadAccessor.GetTexts(pads);
							//sb.Append(",Details");
						} catch (Exception e) {
							log.Error(e);
						}
					}


					var tasks = todos.Select(t => {
						return GrabTodo(csv, t, padTexts);
					});

					await Task.WhenAll(tasks);

					return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(false));
				}
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private static async Task GrabTodo(Csv csv, Models.Todo.TodoModel t, Dictionary<string, string> padLookup) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

			csv.Add("" + t.Id, "Owner", t.AccountableUser.NotNull(x => x.GetName()));
			csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
			csv.Add("" + t.Id, "Due Date", t.DueDate.ToShortDateString());
			var time = "";
			if (t.CompleteTime != null) {
				time = t.CompleteTime.Value.ToShortDateString();
			}

			csv.Add("" + t.Id, "Completed", time);
			csv.Add("" + t.Id, "To-Do", "" + t.Message);


			if (padLookup != null) {
				var padDetails = padLookup.GetOrDefault(t.PadId, "");
				csv.Add("" + t.Id, "Details", Csv.CsvQuote(padDetails));
			}
		}

		public static async Task<byte[]> IssuesList(UserOrganizationModel caller, long recurrenceId, bool includeDetails) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var issues = L10Accessor.GetAllIssuesForRecurrence(s, PermissionsUtility.Create(s, caller), recurrenceId);

					var sb = new StringBuilder();

					sb.Append("Id," +/*Depth,*/"Owner,Created,Closed,Issue");
					//var id = 0;

					var rows = new List<Tuple<long, List<string>>>();

					Dictionary<string, string> padTexts = null;

					if (includeDetails) {
						try {
							var pads = issues.Select(x => x.Issue.PadId).ToList();
							padTexts = await PadAccessor.GetTexts(pads);
							sb.Append(",Details");
						} catch (Exception e) {
							log.Error(e);
						}
					}
					sb.AppendLine();


					var tasks = issues.Select((i, id) => {
						return RecurseIssue(rows, id, i, 0, padTexts);
					});

					await Task.WhenAll(tasks);

					foreach (var r in rows.OrderBy(x => x.Item1)) {
						foreach (var c in r.Item2) {
							sb.Append(c).Append(",");
						}
						sb.AppendLine();
					}
					return new System.Text.UTF8Encoding().GetBytes(sb.ToString());
				}
			}
		}

		public static async Task<List<Tuple<string, byte[]>>> Notes(UserOrganizationModel caller, long recurrenceId) {
			var recur = L10Accessor.GetL10Recurrence(caller, recurrenceId, LoadMeeting.True());
			var lists = new List<Tuple<string, byte[]>>();
			var existing = new Dictionary<string, int>();
			foreach (var note in recur._MeetingNotes) {
				var padDetails = await PadAccessor.GetText(note.PadId);
				var bytes = new System.Text.UTF8Encoding().GetBytes(padDetails);
				var append = "";
				if (!existing.ContainsKey(note.Name)) {
					existing.Add(note.Name, 1);
				} else {
					append = " (" + existing[note.Name] + ")";
					existing[note.Name] += 1;
				}
				lists.Add(Tuple.Create(note.Name + append + ".txt", bytes));

			}
			return lists;
		}

		public static async Task<byte[]> Rocks(UserOrganizationModel caller, long recurrenceId, bool includeDetails) {
			//var meetingId = L10Accessor.GetLatestMeetingId(caller, recurrenceId);
			var rocks = L10Accessor.GetRocksForRecurrence(caller, recurrenceId, true);

			Dictionary<string, string> padTexts = null;
			if (includeDetails) {
				try {
					var pads = rocks.Select(x => x.ForRock.PadId).ToList();
					padTexts = await PadAccessor.GetTexts(pads);
				} catch (Exception e) {
					log.Error(e);
				}
			}

			var csv = new Csv();
			foreach (var rockMilestones in rocks) {
				var t = rockMilestones.ForRock;
				csv.Add("" + t.Id, "Owner", t.AccountableUser.NotNull(x => x.GetName()));
				csv.Add("" + t.Id, "Rock", t.Rock);
				csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
				var time = "";
				if (t.CompleteTime != null) {
					time = t.CompleteTime.Value.ToShortDateString();
				}

				csv.Add("" + t.Id, "Completed", time);

				csv.Add("" + t.Id, "Status", RockStateExtensions.GetCompletionVal(t.Completion));
				csv.Add("" + t.Id, "ArchivedTime", "" + t.DeleteTime);

				if (includeDetails) {
					var padDetails = padTexts.GetOrDefault(t.PadId, "");
					csv.Add("" + t.Id, "Notes", Csv.CsvQuote(padDetails));
				}
			}

			return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(false));
		}
		public static byte[] MeetingSummary(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var ratings = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null)
						.JoinQueryOver(x => x.L10Meeting)
						.Where(x => x.L10RecurrenceId == recurrenceId)
						.Fetch(x => x.L10Meeting).Eager
						.List().ToList();

					var csv = new Csv();
					foreach (var t in ratings.OrderBy(x => x.L10Meeting.CompleteTime).GroupBy(x => x.L10Meeting.Id)) {
						var sum = t.Where(x => x.Rating.HasValue).Select(x => x.Rating.Value).Sum();
						decimal count = t.Where(x => x.Rating.HasValue).Select(x => x.Rating.Value).Count();

						var first = t.First();
						csv.Add("" + first.L10Meeting.Id, "Start Time", first.L10Meeting.CreateTime.ToString("MM/dd/yyyy HH:mm:ss"));
						var time = "";
						if (first.L10Meeting.CompleteTime != null) {
							time = first.L10Meeting.CompleteTime.Value.ToString("MM/dd/yyyy HH:mm:ss");
						}

						csv.Add("" + first.L10Meeting.Id, "End Time", time);
						var avg = "";
						if (count > 0) {
							avg = String.Format("{0:##.###}", sum / count);
						}

						csv.Add("" + first.L10Meeting.Id, "Average Rating", avg);
					}

					foreach (var t in ratings) {
						csv.Add("" + t.L10Meeting.Id, t.User.GetName(), "" + t.Rating);
					}

					return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(false));
				}
			}
		}
		public static byte[] Ratings(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var ratings = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null)
						.JoinQueryOver(x => x.L10Meeting)
						.Where(x => x.L10RecurrenceId == recurrenceId)
						.Fetch(x => x.L10Meeting).Eager
						.List().ToList();

					var csv = new Csv();
					foreach (var t in ratings.OrderBy(x => x.L10Meeting.CompleteTime).GroupBy(x => x.L10Meeting.Id)) {
						foreach (var u in t) {
							if (u.L10Meeting.CompleteTime != null) {
								csv.Add(u.User.GetName(), u.L10Meeting.CompleteTime.Value.ToString(), u.Rating.NotNull(x => "" + String.Format("{0:##.###}", x)) ?? "NR");
							}
						}
					}

					foreach (var t in ratings.OrderBy(x => x.L10Meeting.CompleteTime).GroupBy(x => x.L10Meeting.Id)) {
						var sum = t.Where(x => x.Rating.HasValue).Select(x => x.Rating.Value).Sum();
						decimal count = t.Where(x => x.Rating.HasValue).Select(x => x.Rating.Value).Count();
						var avg = "";
						if (count > 0) {
							avg = String.Format("{0:##.###}", sum / count);
						}

						if (t.First().L10Meeting.CompleteTime != null) {
							csv.Add("Average Rating", t.First().L10Meeting.CompleteTime.Value.ToString(), "" + avg);
						}
					}

					return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(true));
				}
			}
		}
		public static async Task RecurseIssue(List<Tuple<long, List<string>>> rows, int index, IssueModel.IssueModel_Recurrence parent, int depth, Dictionary<string, string> padLookup) {
			var cells = new List<string>();
			var row = Tuple.Create((long)index, cells);

			var time = "";
			if (parent.CloseTime != null) {
				time = parent.CloseTime.Value.ToShortDateString();
			}

			cells.Add("" + index);
			cells.Add("" + Csv.CsvQuote(parent.Owner.NotNull(x => x.GetName())));
			cells.Add("" + parent.CreateTime.ToShortDateString());
			cells.Add("" + time);
			cells.Add("" + Csv.CsvQuote(parent.Issue.Message));

			if (padLookup != null) {
				var padDetails = padLookup.GetOrDefault(parent.Issue.PadId, "");
				cells.Add(Csv.CsvQuote(padDetails));
			}

			rows.Add(row);
			foreach (var child in parent._ChildIssues) {
				await RecurseIssue(rows, index, child, depth + 1, padLookup);
			}
		}


		public static Csv ExportAllUsers(UserOrganizationModel caller, long orgId) {
			PermissionsAccessor.EnsurePermitted(caller, x => x.ManagingOrganization(orgId));

			var users = TinyUserAccessor.GetOrganizationMembers(caller, orgId, true);
			var csv = new Csv("Users");
			foreach (var u in users) {
				csv.Add("" + u.UserOrgId, "Email", u.Email);
				csv.Add("" + u.UserOrgId, "FirstName", u.FirstName);
				csv.Add("" + u.UserOrgId, "LastName", u.LastName);
			}
			return csv;
		}
	}
}
