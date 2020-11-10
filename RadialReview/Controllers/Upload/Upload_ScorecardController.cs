using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsvHelper;
using RadialReview.Utilities;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using System.Net;
using System.Globalization;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.RealTime;
using System.Text.RegularExpressions;
using RadialReview.Utilities.NHibernate;
using System.Text;

namespace RadialReview.Controllers {
	public partial class UploadController : BaseController {

		//private static Regex thousandRegex = new Regex("[\\d]\\s*k(\\s+|$)");
		private static decimal? ParseScore(string score) {
			string s = score.ToLower();
			var mult = 1.0m;
			if (s.Contains("mm"))
				mult = 1000000;
			else if (s.Contains("k") && Regex.IsMatch(s, "[\\d]\\s*k(\\s+|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				mult = 1000;

			//s = Regex.Replace(s, "[^0-9\\.-\\s]", "");
			var parsed = Regex.Replace(score, "[^0-9\\s\\.-]", "").Trim().Split(new char[] { ' ', '\t' }).Select(x => x.TryParseDecimal()).FirstOrDefault(x => x != null);
			//s = s.Trim();

			//var parsed = s.TryParseDecimal();
			if (parsed != null)
				return parsed.Value * mult;
			return null;
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<FileResult> SampleScorecard() {
			var startDates = new List<string>();
			var endDates = new List<string>();
			var commas = new List<string>();
			var rand = new Random();
			for (var i = 13; i >= 1; i--) {
				startDates.Add(DateTime.UtcNow.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday).AddDays(-i * 7).ToString("MM/dd/yyyy"));
				endDates.Add(DateTime.UtcNow.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday).AddDays(-(i - 1) * 7 - 1).ToString("MM/dd/yyyy"));
				commas.Add("$" + rand.Next(-10, 500) * 100);
			}
			var builder = new StringBuilder();
			builder.AppendLine(",,," + string.Join(",", startDates));
			builder.AppendLine("Who,Measurables,Goal," + string.Join(",", endDates));
			builder.AppendLine($"{GetUser().GetName()},Revenue,>$15000," + string.Join(",", commas));

			var bytes = Encoding.UTF8.GetBytes(builder.ToString());
			Response.AddHeader("Content-Disposition", "attachment;filename=example_scorecard.csv");

			return new FileContentResult(bytes, "text/csv");
		}



		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<PartialViewResult> ProcessScorecardSelection(IEnumerable<int> users, IEnumerable<int> dates, IEnumerable<int> measurables, IEnumerable<int> goals, long recurrenceId, string path) {
			try {
				var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);
				var csvData = ui.Csv;
				var userRect = new Rect(users);
				var measurableRect = new Rect(measurables);
				var goalsRect = new Rect(goals);

				userRect.EnsureRowOrColumnOrCell();
				userRect.EnsureSameRangeAs(measurableRect);
				userRect.EnsureSameRangeAs(goalsRect);

				Rect dateRect = null;
				if (dates != null)
					dateRect = new Rect(dates);

				if (dateRect != null && userRect.GetType() != dateRect.GetType())
					throw new ArgumentOutOfRangeException("rect", "Date selection and owner selection must be of different selection types (either row or column)");

				var userStrings = userRect.GetArray1D(csvData);
				var measurableStrings = measurableRect.GetArray1D(csvData);
				var goals1 = goalsRect.GetArray1D(csvData, x => ParseScore(x) ?? 0m);

				List<DateTime> dates1 = new List<DateTime>();
				if (dateRect != null) {
					var dateStrings = dateRect.GetArray1D(csvData);
					dates1 = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));
				}

				var future = DateTime.UtcNow.AddDays(120);
				var invalidDates = dates1.Where(x => x < new DateTime(2005, 1, 1) || x > future);
				if (invalidDates.Any()) {
					throw new Exception("Error uploading scorecard, " + invalidDates.Count() + " dates were invalid. Please make sure your scorecard dates are between January 1, 2005 and " + future.ToString("MMMM dd, yyyy"));
				}


				var orgId = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, LoadMeeting.False()).OrganizationId;
				var allUsers = TinyUserAccessor.GetOrganizationMembers(GetUser(), orgId);
				// var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id);
				var userLookups = DistanceUtility.TryMatch(userStrings, allUsers);

				Rect scoreRect = null;
				List<List<Decimal?>> scores;
				if (dateRect != null) {
					if (dateRect.GetRectType() == RectType.Row || (dateRect.IsCell() && userRect.GetRectType() == RectType.Column)) {
						scoreRect = new Rect(dateRect.MinX, userRect.MinY, dateRect.MaxX, userRect.MaxY);
					} else {
						scoreRect = new Rect(userRect.MinX, dateRect.MinY, userRect.MaxX, dateRect.MaxY);
					}

					scores = scoreRect.GetArray(csvData, x => ParseScore(x));
				} else {
					scores = goals1.Select(x => new List<Decimal?>()).ToList();
				}
				var direction = goalsRect.GetArray1D(csvData, x => {
					if (x.Contains("<="))
						return LessGreater.LessThanOrEqual;
					if (x.Contains(">="))
						return LessGreater.GreaterThan;
					if (x.Contains("<"))
						return LessGreater.LessThan;
					if (x.Contains(">"))
						return LessGreater.GreaterThan;
					if (x.Contains("="))
						return LessGreater.EqualTo;
					return LessGreater.GreaterThan;
				});
				var units = goalsRect.GetArray1D(csvData, x => {
					if (x.Contains("$") || x.ToLower().Contains("usd") || x.ToLower().Contains("dollar"))
						return UnitType.Dollar;
					if (x.Contains("%"))
						return UnitType.Percent;
					if (x.Contains("£") || x.Contains("₤") || x.ToLower().Contains("gbp") || x.ToLower().Contains("pound"))
						return UnitType.Pound;
					if (x.Contains("€") || x.Contains("€") || x.ToLower().Contains("euro") || x.ToLower().Contains("eur"))
						return UnitType.Euros;
					return UnitType.None;
				});



				//Try and match to an existing measurable
				var existingScorecard = await L10Accessor.GetOrGenerateScorecardDataForRecurrence(GetUser(), recurrenceId, includeAutoGenerated: false, getMeasurables: true, getScores: true, generateMissingData: false);
				var visibleMeasurableByNameAndOwner = existingScorecard.Measurables
														 .Distinct(x => Tuple.Create(x.Title, x.AccountableUserId))
														 .ToDefaultDictionary(x => Tuple.Create(x.Title, x.AccountableUserId), x => (long?)x.Id);

				var measurablesFull = measurableStrings.Select((x, i) => new UploadScorecardSelectedDataVM.Measurable {
					Name = x,
					ExistingMeasurableId = null,
					Order = i
				}).ToList();

				//Attach match
				var matchedMeasurableIds = new List<long>();
				foreach (var mm in measurablesFull) {
					var owner = userLookups[userStrings[mm.Order]].GetProbabilities().OrderByDescending(x => x.Value);
					if (owner.Any()) {
						var measurableId = visibleMeasurableByNameAndOwner[Tuple.Create(mm.Name, owner.First().Key.UserOrgId)];
						mm.ExistingMeasurableId = measurableId;
						if (measurableId.HasValue) {
							matchedMeasurableIds.Add(measurableId.Value);
						}
					}
				}
				//Remove all 
				//Remove Duplicates
				var duplicates = matchedMeasurableIds.Duplicates().ToList();
				if (duplicates.Any()) {
					foreach (var d in measurablesFull.Where(x => duplicates.Any(y => y == x.ExistingMeasurableId))) {
						d.MatchFailed = true;
						d.ExistingMeasurableId = null;
						d.MatchFailReason = "Could not merge measurable, measureable appeared in the file twice.";
					}
				}
				matchedMeasurableIds = matchedMeasurableIds.Where(x => !duplicates.Any(y => y == x)).ToList();

				//Finally match up the scores
				var matchedMeasurableByOrder = measurablesFull.ToDefaultDictionary(x => x.Order, x => x.ExistingMeasurableId);
				var scoresFull = scores.Select((a, i) => a.Select((x, w) => new UploadScorecardSelectedDataVM.Score {
					Value = x,
					Row = i,
					Column = w,
					ExistingMeasurableId = matchedMeasurableByOrder[i]
				}).ToList()).ToList();


				//Inject old scores
				int _weekShift;
				List<DateTime> orderedDates;
				CaptureDates(dateRect, csvData, out orderedDates, out _weekShift);
				foreach (var s in scoresFull.SelectMany(x => x).Where(x => x.ExistingMeasurableId != null)) {
					var mid = s.ExistingMeasurableId.Value;
					var weekId = TimingUtility.GetWeekSinceEpoch(orderedDates[s.Column].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday)) + _weekShift;
					s.OldValue = existingScorecard.Scores.FirstOrDefault(x => x.MeasurableId == mid && x.DataContract_ForWeek == weekId).NotNull(x => x.Measured);
				}





				var m = new UploadScorecardSelectedDataVM() {
					Rows = csvData,
					Users = userStrings,
					UserLookup = userLookups,
					Measurables = measurablesFull,
					Dates = dates1,
					Goals = goals1,
					Scores = scoresFull,
					RecurrenceId = recurrenceId,
					Path = path,
					//UseAWS = useAWS,
					ScoreRange = scoreRect.NotNull(x => string.Join(",", x.ToString())),
					MeasurableRectType = "" + dateRect.NotNull(x => x.GetRectType()),
					DateRange = dateRect.NotNull(x => string.Join(",", x.ToString())),
					AllUsers = allUsers.Select(x => new SelectListItem() { Text = x.FirstName + " " + x.LastName, Value = x.UserOrgId + "" }).ToList(),
					Direction = direction,
					Units = units
				};
				return PartialView("UploadScorecardSelected", m);
			} catch (Exception e) {
				throw new Exception(e.Message + "[" + path + "]", e);
			}
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> SubmitScorecard(FormCollection model) {
			var path = model["Path"].ToString();
			try {
				//var useAws = model["UseAWS"].ToBoolean();
				var recurrence = model["recurrenceId"].ToLong();
				var noTitleBar = model["noTitleBar"].ToBoolean();
				var measurableRectType = model["MeasurableRectType"].ToString();
				Rect scoreRect = null;
				if (!string.IsNullOrWhiteSpace(model["ScoreRange"]))
					scoreRect = new Rect(model["ScoreRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());
				Rect dateRect = null;
				if (!string.IsNullOrWhiteSpace(model["DateRange"]))
					dateRect = new Rect(model["DateRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());

				PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));
				var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);
				var csvData = ui.Csv;

				var keys = model.Keys.OfType<string>();
				var measurables = keys.Where(x => x.StartsWith("m_measurable_"))
					.ToDictionary(x => x.SubstringAfter("m_measurable_").ToInt(), x => (string)model[x]);
				var goals = keys.Where(x => x.StartsWith("m_goal_"))
					.ToDictionary(x => x.SubstringAfter("m_goal_").ToInt(), x => ParseScore(model[x]) ?? 0);
				var users = keys.Where(x => x.StartsWith("m_user_"))
					.ToDictionary(x => x.SubstringAfter("m_user_").ToInt(), x => model[x].ToLong());
				var goalDirs = keys.Where(x => x.StartsWith("m_goaldir_"))
					.ToDictionary(x => x.SubstringAfter("m_goaldir_").ToInt(), x => (LessGreater)Enum.Parse(typeof(LessGreater), model[x]));
				var goalUnits = keys.Where(x => x.StartsWith("m_goalunits_"))
					.ToDictionary(x => x.SubstringAfter("m_goalunits_").ToInt(), x => (UnitType)Enum.Parse(typeof(UnitType), model[x]));

				List<List<Decimal?>> scores = null;
				if (scoreRect != null)
					scores = scoreRect.GetArray(csvData, (x, c) => ParseScore(x));
				List<DateTime> dates;
				int weekShift;
				CaptureDates(dateRect, csvData, out dates, out weekShift);

				//Create score replacement dictionary
				var scoreOverride = new Dictionary<Tuple<int, int>, decimal?>();
				var updateMeasurableRows = new List<Tuple<int, long>>();
				foreach (var sr in keys.Where(x => x.StartsWith("m_use_"))) {
					var rowId = sr.SubstringAfter("m_use_").ToInt();
					string rowKey;
					if (model[sr].ToLower() == "existing") {
						var measId = model["m_existing_meas_id_" + rowId].ToLong();
						rowKey = "m_score_existing_" + rowId + "_";
						updateMeasurableRows.Add(Tuple.Create(rowId, measId));
					} else if (model[sr].ToLower() == "create") {
						continue;
						//rowKey = "m_score_create_" + rowId + "_";
					} else {
						continue;
					}
					foreach (var sc in keys.Where(x => x.StartsWith(rowKey))) {
						var colId = sc.SubstringAfter(rowKey).ToInt();
						var value = model[rowKey + colId];
						scoreOverride.Add(Tuple.Create(rowId, colId), ParseScore(value));
					}
				}


				var caller = GetUser();
				var now = DateTime.UtcNow;

				var measurableLookup = new Dictionary<int, MeasurableModel>();
				using (var s = HibernateSession.GetCurrentSession(/*singleSession:false*/)) {
					using (var tx = s.BeginTransaction()) {
						using (var rt = RealTimeUtility.Create(false)) {
							var org = s.Get<L10Recurrence>(recurrence).Organization;
							var perms = PermissionsUtility.Create(s, caller).ViewOrganization(org.Id);
							var ii = 1;
							foreach (var m in measurables) {
								var ident = m.Key;
								var owner = users[ident];
								var goal = goals[ident];
								var goaldir = goalDirs[ident];
								//var measurable = new MeasurableModel() {
								//	Title = m.Value,
								//	OrganizationId = org.Id,
								//	Goal = goal,
								//	GoalDirection = goaldir,
								//	AccountableUserId = owner,
								//	AdminUserId = owner,
								//	CreateTime = now,
								//	_Ordering = ii
								//};

								//Empty row?
								if (string.IsNullOrWhiteSpace(m.Value)) {
									if (scoreRect == null) {
										continue;
									} else {
										var scoreRow = measurableRectType != "Column"
									  ? new Rect(scoreRect.MinX, scoreRect.MinY + ident, scoreRect.MaxX, scoreRect.MinY + ident)
									  : new Rect(scoreRect.MinX + ident, scoreRect.MinY, scoreRect.MinX + ident, scoreRect.MaxY);

										var scoresFound = scoreRow.GetArray1D(csvData, x => ParseScore(x));
										if (scoresFound.All(x => x == 0 || x == null))
											continue;
									}
								}

								var units = UnitType.None;
								try {
									units = goalUnits[ident];
								} catch (Exception) {
								}

								//await L10Accessor.AddMeasurable(s, perms, rt, recurrence, L10Controller.AddMeasurableVm.CreateMeasurableViewModel(recurrence, measurable), skipRealTime: true, rowNum: ii);


								MeasurableModel measurable;
								if (updateMeasurableRows.Any(x => x.Item1 == ident)) {
									var mid = updateMeasurableRows.First(x => x.Item1 == ident).Item2;
									await ScorecardAccessor.UpdateMeasurable(s, perms, mid, m.Value, goaldir, goal, owner,unitType: units);
									measurable = ScorecardAccessor.GetMeasurable(s, perms, mid);
								} else {
									measurable = await ScorecardAccessor.CreateMeasurable(s, perms, MeasurableBuilder.Build(m.Value, owner, type: units, goal: goal, goalDirection: goaldir, now: now));
									await L10Accessor.AttachMeasurable(s, perms, recurrence, measurable.Id, true, ii, now);
								}

								ii += 1;
								measurableLookup[ident] = measurable;




								if (scoreRect != null) {
									var scoreRow = measurableRectType != "Column"
										? new Rect(scoreRect.MinX, scoreRect.MinY + ident, scoreRect.MaxX, scoreRect.MinY + ident)
										: new Rect(scoreRect.MinX + ident, scoreRect.MinY, scoreRect.MinX + ident, scoreRect.MaxY);

									var scoresFound = scoreRow.GetArray1D(csvData, x => ParseScore(x));

									for (var i = 0; i < dates.Count; i++) {
										var week = TimingUtility.GetWeekSinceEpoch(dates[i].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday)) + weekShift;
										var score = scoresFound[i];

										var overrideKey = Tuple.Create(ident, i);
										if (scoreOverride.ContainsKey(overrideKey)) {
											score = scoreOverride[overrideKey];
										}

										await ScorecardAccessor.UpdateScore_Unordered(s, perms, measurable.Id, TimingUtility.GetDateSinceEpoch(week), score);
										//await L10Accessor._UpdateScore(s, perms, rt, measurable.Id, week, score, null, noSyncException: true, skipRealTime: true);
									}
								}
							}
							var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
								.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrence)
								.Select(x => x.User.Id)
								.List<long>().ToList();

							foreach (var u in users.Where(x => !existing.Any(y => y == x.Value)).Select(x => x.Value).Distinct()) {
								s.Save(new L10Recurrence.L10Recurrence_Attendee() {
									User = s.Load<UserOrganizationModel>(u),
									L10Recurrence = s.Load<L10Recurrence>(recurrence),
									CreateTime = now,
								});
							}
							tx.Commit();
							s.Flush();
						}
					}
				}

				//ShowAlert("Uploaded Scorecard", AlertType.Success);
				return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "?noheading=" + noTitleBar + "#Scorecard", "Uploaded Scorecard"));
				//return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "#Scorecard", "Uploaded Scorecard"));
			} catch (Exception e) {
				throw new Exception(e.Message + "[" + path + "]", e);
			}
		}

		private void CaptureDates(Rect dateRect, List<List<string>> csvData, out List<DateTime> dates, out int weekShift) {
			dates = new List<DateTime>();
			if (dateRect != null) {
				var dateStrings = dateRect.GetArray1D(csvData);
				dates = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));
			}
			weekShift = 0;
			if (dates.Any()) {
				var weekNum = TimingUtility.GetWeekSinceEpoch(dates[0].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday));
				var weekStart = GetUser().GetOrganizationSettings().WeekStart;
				var genDate = TimingUtility.GetDateSinceEpoch(weekNum).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
				while (genDate.AddDays(7) <= dates[0]) {
					weekShift += 1;
					genDate = TimingUtility.GetDateSinceEpoch(weekNum + weekShift).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
				}
				genDate = TimingUtility.GetDateSinceEpoch(weekNum + weekShift).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
				while (dates[0] < genDate) {
					weekShift -= 1;
					genDate = TimingUtility.GetDateSinceEpoch(weekNum + weekShift).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
				}
			}
		}

		public class UploadScorecardSelectedDataVM {

			public class Measurable {
				public string Name { get; set; }
				public long? ExistingMeasurableId { get; set; }
				public int Order { get; set; }
				public bool MatchFailed { get; set; }
				public string MatchFailReason { get; set; }

			}

			public class Score {
				public decimal? Value { get; set; }
				public long? ExistingScoreId { get; set; }
				public int Row { get; internal set; }
				public decimal? OldValue { get; set; }
				public long? ExistingMeasurableId { get; internal set; }
				public int Column { get; internal set; }
			}


			public List<UnitType> Units { get; set; }
			public List<LessGreater> Direction { get; set; }
			public List<Measurable> Measurables { get; set; }
			public List<string> Users { get; set; }
			public List<decimal> Goals { get; set; }
			public List<DateTime> Dates { get; set; }

			public List<List<String>> Rows { get; set; }

			public List<Tuple<long, long>> Errors { get; set; }

			public Dictionary<string, DiscreteDistribution<TinyUser>> UserLookup { get; set; }

			public List<List<Score>> Scores { get; set; }

			public long RecurrenceId { get; set; }

			public bool UseAWS { get; set; }

			public string Path { get; set; }

			public string ScoreRange { get; set; }

			public string MeasurableRectType { get; set; }

			public string DateRange { get; set; }

			public List<SelectListItem> AllUsers { get; set; }
		}

	}
}