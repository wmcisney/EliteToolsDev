using Hangfire;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hangfire;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Meeting;
using RadialReview.Models.Todo;
using RadialReview.Models.VTO;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
//using System.Web.WebPages.Html;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using static RadialReview.Utilities.EventUtil;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Meeting Actions

		private static IEnumerable<L10Recurrence.L10Recurrence_Page> GenerateMeetingPages(long recurrenceId, MeetingType meetingType, DateTime createTime) {

			if (meetingType == MeetingType.L10) {
				#region L10 Pages
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Segue",
					Subheading = "Share good news from the last 7 days.<br/> One personal and one business.",
					PageType = L10Recurrence.L10PageType.Segue,
					_Ordering = 0,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Scorecard",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Scorecard,
					_Ordering = 1,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Rock Review",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Rocks,
					_Ordering = 2,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "People Headlines",
					Subheading = "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.",
					PageType = L10Recurrence.L10PageType.Headlines,
					_Ordering = 3,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "To-do List",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Todo,
					_Ordering = 4,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 60,
					Title = "IDS",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.IDS,
					_Ordering = 5,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Conclude",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Conclude,
					_Ordering = 6,
					AutoGen = true
				};
				#endregion
			} else if (meetingType == MeetingType.SamePage) {
				#region Same Page Meeting pages
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Check In",
					Subheading = "How are you doing? State of mind?</br> Business and personal stuff?",
					PageType = L10Recurrence.L10PageType.Empty,
					_Ordering = 0,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Build Issues List",
					Subheading = "List all of your issues, concerns, ideas and disconnects.",
					PageType = L10Recurrence.L10PageType.Empty,
					_Ordering = 1,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 50,
					Title = "IDS",
					Subheading = "IDS all of your issues.",
					PageType = L10Recurrence.L10PageType.IDS,
					_Ordering = 2,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Conclude",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Conclude,
					_Ordering = 3,
					AutoGen = true
				};
				#endregion
			}
		}

		public static async Task<L10Recurrence> CreateBlankRecurrence(UserOrganizationModel caller, long orgId, bool addCreator, MeetingType meetingType = MeetingType.L10) {
			L10Recurrence recur;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					recur = await CreateBlankRecurrence(s, perms, orgId, addCreator, meetingType);
					tx.Commit();
					s.Flush();
				}
			}
			return recur;
		}

		public static async Task<L10Recurrence> CreateBlankRecurrence(ISession s, PermissionsUtility perms, long orgId, bool addCreator, MeetingType meetingType = MeetingType.L10) {
			L10Recurrence recur;
			var caller = perms.GetCaller();
			perms.CreateL10Recurrence(orgId);

			var teamType = L10TeamType.LeadershipTeam;
			var anyLT = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null && x.OrganizationId==orgId && x.TeamType==L10TeamType.LeadershipTeam).Take(1).RowCount();
			if (anyLT > 0) {
				teamType = L10TeamType.DepartmentalTeam;
			}



			recur = new L10Recurrence() {
				OrganizationId = orgId,
				Pristine = true,
				VideoId = Guid.NewGuid().ToString(),
				EnableTranscription = false,
				HeadlinesId = Guid.NewGuid().ToString(),
				CountDown = true,
				CreatedById = caller.Id,
				CreateTime = DateTime.UtcNow,
				TeamType = teamType,

				//Added defaults:
				RockType = orgId.NotEOSW(Model.Enums.L10RockType.Milestones, Model.Enums.L10RockType.Original),
				ReverseScorecard = true,
				CurrentWeekHighlightShift = -1
			};

			if (meetingType == MeetingType.SamePage) {
				recur.TeamType = L10TeamType.SamePageMeeting;
			}

			s.Save(recur);

			foreach (var page in GenerateMeetingPages(recur.Id, meetingType, recur.CreateTime)) {
				s.Save(page);
			}


			var vto = VtoAccessor.CreateRecurrenceVTO(s, perms, recur.Id);
            // add default values in VTO 3-Year Picture
            var defaultEditString = DisplayNameStrings.defaultEditMessage;
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_ThreeYearPicture, null, key: "Revenue:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_ThreeYearPicture, null, key: "Profit:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_ThreeYearPicture, null, key: "Measurables:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_OneYearPlan, null, key: "Revenue:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_OneYearPlan, null, key: "Profit:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_OneYearPlan, null, key: "Measurables:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_QuarterlyRocks, null, key: "Revenue:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_QuarterlyRocks, null, key: "Profit:", value: defaultEditString);
            VtoAccessor.AddKV(s, perms,vto.Id, VtoItemType.Header_QuarterlyRocks, null, key: "Measurables:", value: defaultEditString);

			s.Save(new PermItem() {
				CanAdmin = true,
				CanEdit = true,
				CanView = true,
				AccessorType = PermItem.AccessType.Creator,
				AccessorId = caller.Id,
				ResType = PermItem.ResourceType.L10Recurrence,
				ResId = recur.Id,
				CreatorId = caller.Id,
				OrganizationId = caller.Organization.Id,
				IsArchtype = false,
			});
			s.Save(new PermItem() {
				CanAdmin = true,
				CanEdit = true,
				CanView = true,
				AccessorType = PermItem.AccessType.Members,
				AccessorId = -1,
				ResType = PermItem.ResourceType.L10Recurrence,
				ResId = recur.Id,
				CreatorId = caller.Id,
				OrganizationId = caller.Organization.Id,
				IsArchtype = false,
			});
			s.Save(new PermItem() {
				CanAdmin = true,
				CanEdit = true,
				CanView = true,
				AccessorId = -1,
				AccessorType = PermItem.AccessType.Admins,
				ResType = PermItem.ResourceType.L10Recurrence,
				ResId = recur.Id,
				CreatorId = caller.Id,
				OrganizationId = caller.Organization.Id,
				IsArchtype = false,
			});

			await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.CreateRecurrence(ses, recur));

			if (addCreator) {
				using (var rt = RealTimeUtility.Create()) {
					await AddAttendee(s, perms, rt, recur.Id, caller.Id);
				}
			}

			return recur;
		}

		public static async Task Depristine_Unsafe(ISession s, UserOrganizationModel caller, L10Recurrence recur) {
			if (recur.Pristine == true) {
				recur.Pristine = false;
				s.Update(recur);
				await Trigger(x => x.Create(s, EventType.CreateMeeting, caller, recur, message: recur.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));
			}
		}

		public static async Task<MvcHtmlString> GetMeetingSummary(UserOrganizationModel caller, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meetingId);

					var meeting = s.Get<L10Meeting>(meetingId);
					var completeTime = meeting.CompleteTime;

					var completedIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
											.Where(x => x.DeleteTime == null && x.CloseTime == completeTime && x.Recurrence.Id == meeting.L10RecurrenceId)
											.List().ToList();

					var pads = completedIssues.Select(x => x.Issue.PadId).ToList();
					var padTexts = await PadAccessor.GetHtmls(pads);

					return new MvcHtmlString((await IssuesAccessor.BuildIssuesSolvedTable(completedIssues, showDetails: true, padLookup: padTexts)).ToString());
				}
			}
		}

		public static string GetDefaultStartPage(L10Recurrence recurrence) {

			if (recurrence == null || recurrence._Pages == null) {
				return "nopage";
			}

			var page = recurrence._Pages.FirstOrDefault();
			if (page != null) {
				return "page-" + page.Id;
			} else {
				return "nopage";
			}
			////UNREACHABLE...
			/*var p = "segue";
			if (recurrence.SegueMinutes > 0)
				p = "segue";
			else if (recurrence.ScorecardMinutes > 0)
				p = "scorecard";
			else if (recurrence.RockReviewMinutes > 0)
				p = "rocks";
			else if (recurrence.HeadlinesMinutes > 0)
				p = "headlines";
			else if (recurrence.TodoListMinutes > 0)
				p = "todo";
			else if (recurrence.IDSMinutes > 0)
				p = "ids";
			else
				p = "conclusion";
			return p;*/
		}

		public static async Task<L10Meeting> StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader, long recurrenceId, List<long> attendees, bool preview) {
			L10Recurrence recurrence;
			L10Meeting meeting;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					if (caller.Id != meetingLeader.Id) {
						PermissionsUtility.Create(s, meetingLeader).ViewL10Recurrence(recurrenceId);
					}

					lock ("Recurrence_" + recurrenceId) {
						//Make sure we're unstarted
						try {
							var perms = PermissionsUtility.Create(s, caller);
							_GetCurrentL10Meeting(s, perms, recurrenceId, false);
							throw new MeetingException(recurrenceId, "Meeting has already started.", MeetingExceptionType.AlreadyStarted);
						} catch (MeetingException e) {
							if (e.MeetingExceptionType != MeetingExceptionType.Unstarted) {
								throw;
							}
						}
						var now = DateTime.UtcNow;
						recurrence = s.Get<L10Recurrence>(recurrenceId);
						meeting = new L10Meeting {
							CreateTime = now,
							StartTime = now,
							L10RecurrenceId = recurrenceId,
							L10Recurrence = recurrence,
							OrganizationId = recurrence.OrganizationId,
							MeetingLeader = meetingLeader,
							MeetingLeaderId = meetingLeader.Id,
							Preview = preview,
						};
						s.Save(meeting);
						tx.Commit();
						s.Flush();
					}
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					recurrence.MeetingInProgress = meeting.Id;
					s.Update(recurrence);

					_LoadRecurrences(s, LoadMeeting.True(), recurrence);

					foreach (var m in recurrence._DefaultMeasurables) {
						if (m.Id > 0) {
							var mm = new L10Meeting.L10Meeting_Measurable() {
								L10Meeting = meeting,
								Measurable = m.Measurable,
								_Ordering = m._Ordering,
								IsDivider = m.IsDivider
							};
							s.Save(mm);
							meeting._MeetingMeasurables.Add(mm);
						}
					}
					foreach (var m in attendees) {
						var mm = new L10Meeting.L10Meeting_Attendee() {
							L10Meeting = meeting,
							User = s.Load<UserOrganizationModel>(m),
						};
						s.Save(mm);
						meeting._MeetingAttendees.Add(mm);
					}

					foreach (var r in recurrence._DefaultRocks) {
						var state = RockState.Indeterminate;
						state = r.ForRock.Completion;
						var mm = new L10Meeting.L10Meeting_Rock() {
							ForRecurrence = recurrence,
							L10Meeting = meeting,
							ForRock = r.ForRock,
							Completion = state,
							VtoRock = r.VtoRock,
						};
						s.Save(mm);
						meeting._MeetingRocks.Add(mm);
					}
					var perms2 = PermissionsUtility.Create(s, caller);
					var todos = GetTodosForRecurrence(s, perms2, recurrence.Id, meeting.Id);
					var i = 0;
					foreach (var t in todos.OrderBy(x => x.AccountableUser.NotNull(y => y.GetName()) ?? ("" + x.AccountableUserId)).ThenBy(x => x.Message)) {
						t.Ordering = i;
						s.Update(t);
						i += 1;
					}
					Audit.L10Log(s, caller, recurrenceId, "StartMeeting", ForModel.Create(meeting));
					tx.Commit();
					s.Flush();
					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(meeting)).setupMeeting(meeting.CreateTime.ToJavascriptMilliseconds(), meetingLeader.Id);

				}
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.StartMeeting(ses, recurrence, meeting));
					if (recurrence.TeamType == L10TeamType.LeadershipTeam) {
						await Trigger(x => x.Create(s, EventType.StartLeadershipMeeting, caller, recurrence, message: recurrence.Name));
					}

					if (recurrence.TeamType == L10TeamType.DepartmentalTeam) {
						await Trigger(x => x.Create(s, EventType.StartDepartmentMeeting, caller, recurrence, message: recurrence.Name));
					}

					tx.Commit();
					s.Flush();
				}
			}

			return meeting;
		}


		public static async Task SetMeetingLeader(UserOrganizationModel caller, long meetingLeaderId, long recurrenceId) {
			L10Meeting meeting;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					perms.ViewUserOrganization(meetingLeaderId, true);

					var meetingLeaderResolved = s.Get<UserOrganizationModel>(meetingLeaderId);
					var meetingLeaderPerms = PermissionsUtility.Create(s, meetingLeaderResolved);
					meetingLeaderPerms.ViewL10Recurrence(recurrenceId);


					var getCurrentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
					meeting = s.Get<L10Meeting>(getCurrentMeeting.Id);
					meeting.MeetingLeaderId = meetingLeaderId;
					s.Update(meeting);
					tx.Commit();
					s.Flush();

					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(meeting)).setupMeeting(meeting.CreateTime.ToJavascriptMilliseconds(), meeting.MeetingLeaderId);

				}
			}
		}


		public static async Task ConcludeMeeting(UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, decimal?>> ratingValues, ConcludeSendEmail sendEmail, bool closeTodos, bool closeHeadlines, string connectionId, bool isNewEmailFormat = true) {
			L10Recurrence recurrence = null;
			L10Meeting meeting = null;

			var sendToExternal = false;
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var now = DateTime.UtcNow;
						//Make sure we're unstarted
						var perms = PermissionsUtility.Create(s, caller);
						meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
						perms.ViewL10Meeting(meeting.Id);

						var todoRatio = new Ratio();
						var todos = GetTodosForRecurrence(s, perms, recurrenceId, meeting.Id);

						foreach (var todo in todos) {
							if (todo.CreateTime < meeting.StartTime) {
								if (todo.CompleteTime != null) {
									todo.CompleteDuringMeetingId = meeting.Id;
									if (closeTodos) {
										todo.CloseTime = now;
									}
									s.Update(todo);
								}
								todoRatio.Add(todo.CompleteTime != null ? 1 : 0, 1);
							}
						}

						var headlines = GetHeadlinesForMeeting(s, perms, recurrenceId);
						if (closeHeadlines) {
							CloseHeadlines_Unsafe(meeting.Id, s, now, headlines);
						}


						//Conclude the forum
						recurrence = s.Get<L10Recurrence>(recurrenceId);
						await SendConclusionTextMessages_Unsafe(recurrenceId, recurrence, s, now);

						CloseIssuesOnConclusion_Unsafe(recurrenceId, meeting, s, now);

						meeting.TodoCompletion = todoRatio;
						meeting.CompleteTime = now;
						meeting.SendConcludeEmailTo = sendEmail;
						s.Update(meeting);

						var attendees = GetMeetingAttendees_Unsafe(meeting.Id, s);
						var raters = SetConclusionRatings_Unsafe(ratingValues, meeting, s, attendees);

						CloseLogsOnConclusion_Unsafe(meeting, s, now);

						//Close all sub issues
						IssueModel issueAlias = null;
						var issue_recurParents = s.QueryOver<IssueModel.IssueModel_Recurrence>()
							.Where(x => x.DeleteTime == null && x.CloseTime >= meeting.StartTime && x.CloseTime <= meeting.CompleteTime && x.Recurrence.Id == recurrenceId)
							.List().ToList();
						_RecursiveCloseIssues(s, issue_recurParents.Select(x => x.Id).ToList(), now);


						recurrence.MeetingInProgress = null;
						recurrence.SelectedVideoProvider = null;
						s.Update(recurrence);

						var sendEmailTo = new List<L10Meeting.L10Meeting_Attendee>();

						//send emails
						if (sendEmail != ConcludeSendEmail.None) {
							switch (sendEmail) {
								case ConcludeSendEmail.AllAttendees:
									sendEmailTo = attendees;
									sendToExternal = true;
									break;
								case ConcludeSendEmail.AllRaters:
									sendEmailTo = raters.ToList();
									sendToExternal = true;
									break;
								default:
									break;
							}
						}

						ConclusionItems.Save_Unsafe(recurrenceId, meeting.Id, s, todos.Where(x => x.CloseTime == null).ToList(), headlines, issue_recurParents, sendEmailTo);

						await Trigger(x => x.Create(s, EventType.ConcludeMeeting, caller, recurrence, message: recurrence.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));

						Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting", ForModel.Create(meeting));
						tx.Commit();
						s.Flush();
					}
				}
				if (meeting != null) {
					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(meeting), connectionId).concludeMeeting();
				}

                // Unsafe2 is the previous Conclusion Email format. Will leave it here if business decides to revert back or provide two version of emails
                if (isNewEmailFormat)
                    Scheduler.Enqueue(() => SendConclusionEmail_Unsafe(meeting.Id, null, sendToExternal));
				else
                    Scheduler.Enqueue(() => SendConclusionEmail_Unsafe2(meeting.Id, null, sendToExternal));

				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.ConcludeMeeting(ses, recurrence, meeting));
						tx.Commit();
						s.Flush();
					}
				}
			} catch (Exception e) {
				int a = 0;
			}
		}



		public class ConclusionItems {
			public List<IssueModel.IssueModel_Recurrence> ClosedIssues { get; set; }
			public List<TodoModel> OutstandingTodos { get; set; }
			public List<PeopleHeadline> MeetingHeadlines { get; set; }
			public List<L10Meeting.L10Meeting_Attendee> SendEmailsTo { get; set; }
			public long MeetingId { get; private set; }

			public static void Save_Unsafe(long recurrenceId, long meetingId, ISession s, List<TodoModel> todos, List<PeopleHeadline> headlines, List<IssueModel.IssueModel_Recurrence> issue_recurParents, List<L10Meeting.L10Meeting_Attendee> sendEmailTo) {
				//Emails
				foreach (var emailed in sendEmailTo) {
					s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(emailed), L10Meeting.ConclusionDataType.SendEmailSummaryTo));
				}

				//Closed Issues
				foreach (var issue in issue_recurParents) {
					s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(issue), L10Meeting.ConclusionDataType.CompletedIssue));
				}

				//All todos
				foreach (var todo in todos) {
					s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(todo), L10Meeting.ConclusionDataType.OutstandingTodo));
				}

				//All headlines
				foreach (var headline in headlines) {
					s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(headline), L10Meeting.ConclusionDataType.MeetingHeadline));
				}
			}

			public static ConclusionItems Get_Unsafe(ISession s, long meetingId) {
				var meetingItems = s.QueryOver<L10Meeting.L10Meeting_ConclusionData>().Where(x => x.DeleteTime == null && x.L10MeetingId == meetingId).List().ToList();

				var issueIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.CompletedIssue).Select(x => x.ForModel.ModelId).ToArray();
				var headlineIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.MeetingHeadline).Select(x => x.ForModel.ModelId).ToArray();
				var todoIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.OutstandingTodo).Select(x => x.ForModel.ModelId).ToArray();
				var attendeeIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.SendEmailSummaryTo).Select(x => x.ForModel.ModelId).ToArray();

				var issueQ = s.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issueIds).Future();
				var headlineQ = s.QueryOver<PeopleHeadline>().WhereRestrictionOn(x => x.Id).IsIn(headlineIds).Future();
				var todoQ = s.QueryOver<TodoModel>().WhereRestrictionOn(x => x.Id).IsIn(todoIds).Future();
				var attendeeQ = s.QueryOver<L10Meeting.L10Meeting_Attendee>().WhereRestrictionOn(x => x.Id).IsIn(attendeeIds).Future();

				return new ConclusionItems() {
					MeetingId = meetingId,
					ClosedIssues = issueQ.ToList(),
					MeetingHeadlines = headlineQ.ToList(),
					OutstandingTodos = todoQ.ToList(),
					SendEmailsTo = attendeeQ.ToList()
				};
			}
		}

        #region Build Email Summary Html
        /// <summary>
        /// Separated the Whole Html Generation of Email Summary to it not be dependent on an external db source. Thus, the output can easily be tested.
        /// </summary>

        public class EmailConclusionData
        {
            public EmailConclusionData(string owner, string name, string date, string description = null)
            {
                Owner = owner;
                Date = date;
                Name = name;
                Description = description;
            }

            public string Owner { get; }
            public string Date { get; }
            public string Name { get; }
            public string Description { get; }
        }

        private static IEnumerable<EmailConclusionData> OrderAndGroupByOwner(IEnumerable<EmailConclusionData> data)
        {
            return data
                .OrderBy(i => i.Owner).ThenByDescending(i => i.Name)  // order by Owner
                .GroupBy(i => i.Owner)  // process by owner
                .SelectMany(i =>
                    i.Select((i2, index) =>
                        index == 0 ? i2 : new EmailConclusionData(string.Empty, i2.Name, i2.Date) // if still the same owner, empty property on succeding batches
                    )
                    .Union(new[] {new EmailConclusionData(string.Empty, string.Empty, string.Empty)}) // add separate line per Owner
                );
        }

        public class EmailConclusionIssueData : EmailConclusionData
        {
            public EmailConclusionIssueData(string owner, string name, string date, string description = null, List<EmailConclusionData> toDoCreatedList = null)
                : base(owner, name, date, description)
            {
                ToDoCreatedList = toDoCreatedList;
            }

            public List<EmailConclusionData> ToDoCreatedList { get; }
        }


        public static string BuildEmailConclusion(string title,
            string productName,
            Tuple<string, string, string, string, string> stats,
            IEnumerable<EmailConclusionData> todoList,
            IEnumerable<EmailConclusionData> headlineList,
            IEnumerable<EmailConclusionIssueData> issuesList)
        {
            var head = string.Concat(EmailConclusionMetaTags(),
                EmailConclusionCss()
                );
            var todoArray = todoList as EmailConclusionData[] ?? todoList.ToArray();
            var body = string.Concat(
                        EmailConclusionImageHeader(),
                        EmailConclusionStats(stats.Item1, stats.Item2, stats.Item3, stats.Item4, stats.Item5),
                        EmailConclusionTitle(title),
                        todoArray.Any() ? EmailConclusionTodoList(OrderAndGroupByOwner(todoArray)) : string.Empty,
                        EmailConclusionHeadlines(headlineList),
                        EmailConclusionIssues(issuesList),
                        EmailConclusionFooter(productName)
                    );

            return string.Concat($"<head>{head}</head>", 
                                $"<body>{body}</body>");
        }

        private static string EmailConclusionMetaTags()
        {
            return "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />";
        }
        private static string EmailConclusionCss()
        {
            return "<style type=\"text/css\">ol.lst-kix_list_1-3{list-style-type:none}ol.lst-kix_list_1-4{list-style-type:none}ol.lst-kix_list_1-5{list-style-type:none}ol.lst-kix_list_1-6{list-style-type:none}ol.lst-kix_list_1-0{list-style-type:none}.lst-kix_list_1-4>li{counter-increment:lst-ctn-kix_list_1-4}ol.lst-kix_list_1-1{list-style-type:none}ol.lst-kix_list_1-2{list-style-type:none}ol.lst-kix_list_1-6.start{counter-reset:lst-ctn-kix_list_1-6 0}.lst-kix_list_1-1>li{counter-increment:lst-ctn-kix_list_1-1}ol.lst-kix_list_1-3.start{counter-reset:lst-ctn-kix_list_1-3 0}ol.lst-kix_list_1-2.start{counter-reset:lst-ctn-kix_list_1-2 0}ol.lst-kix_list_1-8.start{counter-reset:lst-ctn-kix_list_1-8 0}.lst-kix_list_1-0>li:before{content:\"\" counter(lst-ctn-kix_list_1-0,decimal) \". \"}ol.lst-kix_list_1-5.start{counter-reset:lst-ctn-kix_list_1-5 0}ol.lst-kix_list_1-7{list-style-type:none}.lst-kix_list_1-1>li:before{content:\"\" counter(lst-ctn-kix_list_1-1,lower-latin) \". \"}.lst-kix_list_1-2>li:before{content:\"\" counter(lst-ctn-kix_list_1-2,lower-roman) \". \"}.lst-kix_list_1-7>li{counter-increment:lst-ctn-kix_list_1-7}ol.lst-kix_list_1-8{list-style-type:none}.lst-kix_list_1-3>li:before{content:\"\" counter(lst-ctn-kix_list_1-3,decimal) \". \"}.lst-kix_list_1-4>li:before{content:\"\" counter(lst-ctn-kix_list_1-4,lower-latin) \". \"}ol.lst-kix_list_1-0.start{counter-reset:lst-ctn-kix_list_1-0 0}.lst-kix_list_1-0>li{counter-increment:lst-ctn-kix_list_1-0}.lst-kix_list_1-6>li{counter-increment:lst-ctn-kix_list_1-6}.lst-kix_list_1-7>li:before{content:\"\" counter(lst-ctn-kix_list_1-7,lower-latin) \". \"}.lst-kix_list_1-3>li{counter-increment:lst-ctn-kix_list_1-3}.lst-kix_list_1-5>li:before{content:\"\" counter(lst-ctn-kix_list_1-5,lower-roman) \". \"}.lst-kix_list_1-6>li:before{content:\"\" counter(lst-ctn-kix_list_1-6,decimal) \". \"}ol.lst-kix_list_1-7.start{counter-reset:lst-ctn-kix_list_1-7 0}.lst-kix_list_1-2>li{counter-increment:lst-ctn-kix_list_1-2}.lst-kix_list_1-5>li{counter-increment:lst-ctn-kix_list_1-5}.lst-kix_list_1-8>li{counter-increment:lst-ctn-kix_list_1-8}ol.lst-kix_list_1-4.start{counter-reset:lst-ctn-kix_list_1-4 0}.lst-kix_list_1-8>li:before{content:\"\" counter(lst-ctn-kix_list_1-8,lower-roman) \". \"}ol.lst-kix_list_1-1.start{counter-reset:lst-ctn-kix_list_1-1 0}ol{margin:0;padding:0}table td,table th{padding:0}.c19{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:214.5pt;border-top-color:#000000;border-bottom-style:solid}.c1{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:117pt;border-top-color:#000000;border-bottom-style:solid}.c14{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:21.8pt;border-top-color:#000000;border-bottom-style:solid}.c27{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:129pt;border-top-color:#000000;border-bottom-style:solid}.c36{border-right-style:solid;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:middle;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;background-color:#ffffff;border-left-style:solid;border-bottom-width:0pt;width:427.5pt;border-top-color:#000000;border-bottom-style:solid}.c10{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:226.5pt;border-top-color:#000000;border-bottom-style:solid}.c15{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:224.2pt;border-top-color:#000000;border-bottom-style:solid}.c29{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:116.2pt;border-top-color:#000000;border-bottom-style:solid}.c3{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:125.2pt;border-top-color:#000000;border-bottom-style:solid}.c18{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:468pt;border-top-color:#000000;border-bottom-style:solid}.c8{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:446.2pt;border-top-color:#000000;border-bottom-style:solid}.c5{border-right-style:solid;padding:5pt 5pt 5pt 5pt;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:top;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;border-left-style:solid;border-bottom-width:0pt;width:114.8pt;border-top-color:#000000;border-bottom-style:solid}.c22{border-right-style:solid;border-bottom-color:#000000;border-top-width:0pt;border-right-width:0pt;border-left-color:#000000;vertical-align:middle;border-right-color:#000000;border-left-width:0pt;border-top-style:solid;background-color:#ffffff;border-left-style:solid;border-bottom-width:0pt;width:22.5pt;border-top-color:#000000;border-bottom-style:solid}.c12{margin-left:18pt;padding-top:0pt;text-indent:-18pt;padding-bottom:8pt;line-height:1.0791666666666666;orphans:2;widows:2;text-align:left;height:11pt}.c17{background-color:#ffffff;color:#000000;font-weight:400;text-decoration:none;vertical-align:baseline;font-size:10.5pt;font-family:\"Arial\";font-style:normal}.c42{-webkit-text-decoration-skip:none;color:#0563c1;font-weight:400;text-decoration:underline;text-decoration-skip-ink:none;font-size:10.5pt;font-family:\"Arial\"}.c43{color:#000000;font-weight:700;text-decoration:none;vertical-align:baseline;font-size:16pt;font-family:\"Calibri\";font-style:normal}.c30{color:#222222;font-weight:400;text-decoration:none;vertical-align:baseline;font-size:10.5pt;font-family:\"Helvetica Neue\";font-style:normal}.c9{color:#999999;font-weight:400;text-decoration:none;vertical-align:baseline;font-size:7pt;font-family:\"Arial\";font-style:normal}.c6{color:#000000;font-weight:400;text-decoration:none;vertical-align:baseline;font-size:11pt;font-family:\"Calibri\";font-style:normal}.c21{color:#000000;font-weight:700;text-decoration:none;vertical-align:baseline;font-size:11pt;font-family:\"Calibri\";font-style:normal}.c24{padding-top:0pt;padding-bottom:8pt;line-height:1.0791666666666666;orphans:2;widows:2;text-align:left}.c37{padding-top:0pt;padding-bottom:8pt;line-height:1.0791666666666666;orphans:2;widows:2;text-align:center}.c32{padding-top:0pt;padding-bottom:0pt;line-height:1.0791666666666666;orphans:2;widows:2;text-align:left}.c41{padding-top:0pt;padding-bottom:0pt;line-height:1.0;orphans:2;widows:2;text-align:center}.c4{padding-top:0pt;padding-bottom:0pt;line-height:1.0;text-align:right;height:11pt}.c26{text-decoration-skip-ink:none;font-size:14pt;-webkit-text-decoration-skip:none;font-weight:700;text-decoration:underline}.c28{text-decoration-skip-ink:none;-webkit-text-decoration-skip:none;font-weight:700;text-decoration:underline}.c23{padding-top:0pt;padding-bottom:0pt;line-height:1.0;text-align:right}.c33{text-decoration-skip-ink:none;font-size:14pt;-webkit-text-decoration-skip:none;text-decoration:underline}.c25{background-color:#ffffff;text-decoration:none;vertical-align:baseline;font-style:normal}.c2{padding-top:0pt;padding-bottom:0pt;line-height:1.0;text-align:left}.c11{border-spacing:0;border-collapse:collapse;margin-right:auto}.c34{font-size:10.5pt;font-family:\"Helvetica Neue\";color:#222222;font-weight:400}.c13{font-size:10.5pt;font-family:\"Arial\";color:#222222;font-weight:400}.c40{margin-left:auto;border-spacing:0;border-collapse:collapse;margin-right:auto}.c31{margin-left:18pt;text-indent:-18pt}.c39{max-width:468pt;padding:72pt 72pt 72pt 72pt}.c7{color:inherit;text-decoration:inherit}.c35{margin-left:36pt;text-indent:-36pt}.c38{background-color:#ffffff}.c20{height:21pt}.c0{height:0pt}.c16{height:11pt}.title{padding-top:24pt;color:#000000;font-weight:700;font-size:36pt;padding-bottom:6pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left}.subtitle{padding-top:18pt;color:#666666;font-size:24pt;padding-bottom:4pt;font-family:\"Georgia\";line-height:1.0791666666666666;page-break-after:avoid;font-style:italic;orphans:2;widows:2;text-align:left}li{color:#000000;font-size:11pt;font-family:\"Calibri\"}p{margin:0;color:#000000;font-size:11pt;font-family:\"Calibri\"}h1{padding-top:24pt;color:#000000;font-weight:700;font-size:24pt;padding-bottom:6pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left}h2{padding-top:18pt;color:#000000;font-weight:700;font-size:18pt;padding-bottom:4pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left}h3{padding-top:14pt;color:#000000;font-weight:700;font-size:14pt;padding-bottom:4pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left}h4{padding-top:12pt;color:#000000;font-weight:700;font-size:12pt;padding-bottom:2pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left}h5{padding-top:11pt;color:#000000;font-weight:700;font-size:11pt;padding-bottom:2pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left}h6{padding-top:10pt;color:#000000;font-weight:700;font-size:10pt;padding-bottom:2pt;font-family:\"Calibri\";line-height:1.0791666666666666;page-break-after:avoid;orphans:2;widows:2;text-align:left;}.underline{border-bottom: 1.5px solid black;width:100%;display:block;}"
                    + ".ReadMsgBody {width: 100%;} .ExternalClass {width: 100%;}"
                    + "</style>";
        }

        private static object EmailConclusionFooter(string productName)
        {
            var body =
@"
<p class=""c24 c16""><span class=""c21""></span></p>
<p style='margin: 0; color: #000000; font-size: 11pt; font-family: ""Calibri""; padding-top: 0pt; padding-bottom: 8pt; line-height: 1.0791666666666666; orphans: 2; widows: 2; text-align: left;'>
    <span style='font-size: 10.5pt; font-family: ""Arial""; color: #222222; font-weight: 400; background-color: #ffffff;'>Sincerely,</span>
    <span style='font-size: 10.5pt; font-family: ""Arial""; color: #222222; font-weight: 400;'><br /></span>
    <span style='background-color: #ffffff; text-decoration: none; vertical-align: baseline; font-style: normal; font-size: 10.5pt; font-family: ""Arial""; color: #222222; font-weight: 400;'>
The " + productName + @" Team </span></p>

<p class=""c12""><span class=""c25 c13""></span></p>
<div style='width: 100%'>

<div style='width:40%; margin: 0 auto; max-width: 225px;' >
<span  class=""c38 c42""> 
<a href='https://tractiontools.happyfox.com/home/' 
style=""color: white; background:#005ed7;border:15px solid #005ed7;text-align:center;text-decoration:none;display:block;border-radius:6px;font-weight:bold;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;font-size:12px"" >
Check Out the Elite Tools Knowledge Base</a> 
</span>
</div>

</div>
<p class=""c12""><span class=""c6""></span></p>"

+ $"<p style=\"font-size:9px;color:#999;line-height:12px;margin-top:0px;text-align:center;font-family:Arial,Helvetica,sans-serif\" align=\"center\">{EmailStrings.Footer}</p>"
            


//+ @"<table class=""c40""><tbody><tr class=""c0""><td class=""c22"" colspan=""1"" rowspan=""1""><p class=""c32""><span class=""c34""><br></span><span style = ""overflow: hidden; display: inline-block; margin: 0.00px 0.00px; border: 0.00px solid #000000; transform: rotate(0.00rad) translateZ(0px); -webkit-transform: rotate(0.00rad) translateZ(0px); width: 1.33px; height: 1.00px;"" >< img alt=""https://mail.google.com/mail/u/1/#search/meeting+summary/FMfcgxwCgpTNfCmVglXlrWBvLbDSQgrP"" src=""images/image1.png"" style=""width: 1.33px; height: 1.00px; margin-left: 0.00px; margin-top: 0.00px; transform: rotate(0.00rad) translateZ(0px); -webkit-transform: rotate(0.00rad) translateZ(0px);"" title=""""></span></p></td><td class=""c36"" colspan=""1"" rowspan=""1""><p class=""c37""><span class=""c9"">This message was generated automatically by Elite Tools.<br>95 Highland Ave., Suite 300, Bethlehem, PA 18017<br>If you feel you have received this message in error you can respond to this email.</span></p></td></tr></tbody></table>"
;

            return body;
        }

        private static string EmailConclusionImageHeader()
        {
            // header is not viewable unless the image is deployed in production. Fow now, gonna use a conditional compiler for testing
            //  
#if DEBUG
            var companyLogo = "https://traction.tools/Content/img/TRACTION-TOOLS_large_stacked_2c.png\" style='width: 25%' ";            
#else
            var companyLogo = "https://traction.tools/Content/img/TractionTools%20Email%20Banner.jpg";
#endif            
            return $"<div align='center'><img src=\"{companyLogo}\" /></div>"
                    + @"<p class=""c16 c37"" style='margin: 0; color: #000000; font-size: 11pt; font-family: ""Calibri""; padding-top: 0pt; padding-bottom: 8pt; line-height: 1.0791666666666666; orphans: 2; widows: 2; text-align: center; height: 11pt;'><span class=""c6"" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: ""Calibri""; font-style: normal;'></span></p>";

        }
        private static string EmailConclusionTitle(string title)
        {
            return
                $"<p style='float: left; margin: 20px 0 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 8pt; line-height: 1.0791666666666666; orphans: 2; widows: 2; text-align: center;'><span class=\"c43\" style='color: #000000; font-weight: 700; text-decoration: none; vertical-align: baseline; font-size: 14pt; font-family: \"Calibri\"; font-style: normal;'>{title} Meeting Summary:</span></p>"
                + "<div style='clear:both'></div>";
        }


        private static readonly Func<IEnumerable<EmailConclusionData>, string> EmailConclusionTodoList
            = EmailSectionPartial("To-Do List:", EmailConclusionRow_3Cols);

        private static readonly Func<IEnumerable<EmailConclusionData>, string> EmailConclusionHeadlines
            = EmailSectionPartial("People Headlines:", EmailConclusionHeadline);


        private static string EmailConclusionIssues(IEnumerable<EmailConclusionIssueData> issues)
        {
            return EmailConclusionIssueSection("Issues Solved:", issues);
        }

        private static Func<IEnumerable<EmailConclusionData>, string> EmailSectionPartial(string sectionTitle, Func<EmailConclusionData, string> rowTemplate)
        {
            return sectionData => EmailConclusionSection(sectionTitle, sectionData, rowTemplate);
        }

        private static string EmailConclusionPreventClipping()
        {
            //return $"<span style=\"opacity: 0\"> {DateTime.Now.Ticks.ToString()} </span>";
            return string.Empty; // hotfix for clipping (opacity does now work in Outlook)
        }

        private static string EmailConclusionSection(string sectionTitle, IEnumerable<EmailConclusionData> sectionData, Func<EmailConclusionData, string> rowTemplate)
        {
            var emailConclusionDatas = sectionData as EmailConclusionData[] ?? sectionData.ToArray();

            if (emailConclusionDatas.Any() == false)
                return string.Empty;

            var sectionHeading =
                "<h3 class=\"underline\" style='padding-top: 14pt; color: #000000; font-weight: 700; font-size: 14pt; padding-bottom: 4pt; font-family: \"Calibri\"; line-height: 1.0791666666666666; page-break-after: avoid; orphans: 2; widows: 2; text-align: left; border-bottom: 1.5px solid black; width: 100%; display: block;'>"
                + $"{sectionTitle} " + EmailConclusionPreventClipping() + "</h3>";

            const string sectionHeader = "<table class=\"c11\" style=\"border-spacing: 0; border-collapse: collapse; margin-right: auto;\"><tbody>";

            var dataRows = emailConclusionDatas
                            .Select(rowTemplate)
                            .Aggregate(string.Concat);

            return string.Concat(sectionHeading, sectionHeader, dataRows, "</tbody></table><p class=\"c24 c16\"><span class=\"c21\"></span></p>");
        }

        private static string EmailConclusionHeadline(EmailConclusionData data)
        {
            var rowData = "<tr class=\"c0\" style=\"height: 0pt;\">"
                          + $"<td class=\"c3\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 235.2pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'><b>{data.Name}</b></span></p></td>"
                          + $"<td class=\"c10\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 116.5pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'><b>{data.Owner}</b></span></p></td>"
                    + "</tr>";
            var description = "";
            if (!string.IsNullOrEmpty(data.Description))
            {
                description = $"<tr class=\"c20\" style=\"height: 21pt;\"><td class=\"c18\" colspan=\"2\" rowspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 468pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{data.Description}</span></p></td></tr>"
                            + "<tr class=\"c20\" style=\"height: 21pt;\"><td class=\"c18\" colspan=\"2\" rowspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 468pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'> </span></p></td></tr>";
            }

            return string.Concat(rowData, description);
        }

        private static string EmailConclusionRow_3Cols(EmailConclusionData data)
        {
            var rowData = "<tr class=\"c0\" style=\"height: 0pt;\">"
                          +
                          $"<td class=\"c3\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 120.2pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'><b>{data.Owner}</b></span></p></td>"
                          +
                          $"<td class=\"c10\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 231.5pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{data.Name}</span></p></td>"
                          +
                          $"<td class=\"c29\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 116.2pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{data.Date}</span></p></td>"
                          + "</tr>";
            var description = "";
            if (!string.IsNullOrEmpty(data.Description))
            {
                description = $"<tr class=\"c20\"><td class=\"c18\" colspan=\"3\" rowspan=\"1\"><p class=\"c2\"><span class=\"c6\">{data.Description}</span></p></td></tr>"
                            + "<tr class=\"c20\"><td class=\"c18\" colspan=\"3\" rowspan=\"1\"><p class=\"c2\"><span class=\"c6\">&nbsp;</span></p></td></tr>";
            }

            return string.Concat(rowData, description);
        }

        private static string EmailConclusionIssueSection(string sectionTitle, IEnumerable<EmailConclusionIssueData> issueData)
        {
            
            var sectionHeading =
                $"<h3 class=\"underline\" style='padding-top: 14pt; color: #000000; font-weight: 700; font-size: 14pt; padding-bottom: 4pt; font-family: \"Calibri\"; line-height: 1.0791666666666666; page-break-after: avoid; orphans: 2; widows: 2; text-align: left; border-bottom: 1.5px solid black; width: 100%; display: block;'>{sectionTitle}</h3>";

            var emailConclusionIssueDatas = issueData as EmailConclusionIssueData[] ?? issueData.ToArray();
            if (emailConclusionIssueDatas.Any() == false)
            {
                return string.Concat(sectionHeading, @"<div align='center'><i>-No Issues solved this Week-</i></div>");
            }

            var sectionHeader = @"<table class=""c11"" style=""border-spacing: 0; border-collapse: collapse; margin-right: auto;""><tbody>";

            var dottedHr =
                @"<tr><td colspan=""4"" style=""padding: 0;""><hr style=""border-top: dashed 1px; margin-bottom: 20px;"" /></td></tr>";

            var issuesList = string.Concat(sectionHeader
                                            , emailConclusionIssueDatas
                                                .Select(EmailConclusionIssueRow)
                                                .Aggregate((row1, row2) => string.Concat(row1, dottedHr, row2))
                                            , "</tbody></table>");
            
            return string.Concat(sectionHeading, issuesList);
        }

        private static string EmailConclusionIssueRow(EmailConclusionIssueData data, int index)
        {

            var rowData = "<tr class=\"c0\" style=\"height: 0pt;\">"
                          +
                          $"<td style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 5.8pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c23\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: right;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'><b>{(index + 1).ToString() + "."}</b></span></p></td>"
                          +
                          $"<td colspan=\"2\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 227.5pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'><b>{data.Name}</b></span></p></td>"
                          +
                          $"<td class=\"c1\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 117pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'><b>{data.Owner}</b></span></p></td>"
                          + "</tr>";

            var description = "";
            if (!string.IsNullOrEmpty(data.Description))
            {
                description = "<tr>" +
                                "<td class=\"c14\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 21.8pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c4\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: right; height: 11pt;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'></span></p></td>" +
                                $"<td class=\"c8\" colspan=\"3\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 446.2pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{data.Description}</span></p></td>" +
                              "</tr>";
            }
            var whitespace = "<tr><td colspan=\"4\">&nbsp;</td></tr>";

            var todoCreated = "";
            if (data.ToDoCreatedList != null && data.ToDoCreatedList.Any())
            {
                todoCreated = "<tr class=\"c20\" style=\"height: 21pt;\">"
                              + "<td class=\"c14\" colspan=\"1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 21.8pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c4\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: right; height: 11pt;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'></span></p></td>"
                              + "<td class=\"c8\" colspan=\"3\" style=\"background-color: rgb(221, 221, 221); padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 446.2pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c21\" style='color: #000000; font-weight: 700; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>Context Aware To-Dos:</span></p></td>"
                              + "</tr>"                        

                        + "<tr><td style=\"padding: 0;\">&nbsp;</td><td colspan=\"3\" style=\"padding: 0;\">"
                        + "<table>"
                        + OrderAndGroupByOwner(data.ToDoCreatedList)
                            .Select(todo => "<tr class=\"c0\" style=\"height: 0pt;\">"
                                            + $"<td class=\"c5\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 114.8pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{todo.Owner}</span></p></td>"
                                            + $"<td class=\"c19\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 214.5pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{todo.Name }</span></p></td>"
                                            + $"<td class=\"c1\" style=\"padding: 5pt 5pt 5pt 5pt; border-right-style: solid; border-bottom-color: #000000; border-top-width: 0pt; border-right-width: 0pt; border-left-color: #000000; vertical-align: top; border-right-color: #000000; border-left-width: 0pt; border-top-style: solid; border-left-style: solid; border-bottom-width: 0pt; width: 117pt; border-top-color: #000000; border-bottom-style: solid;\"><p class=\"c2\" style='margin: 0; color: #000000; font-size: 11pt; font-family: \"Calibri\"; padding-top: 0pt; padding-bottom: 0pt; line-height: 1.0; text-align: left;'><span class=\"c6\" style='color: #000000; font-weight: 400; text-decoration: none; vertical-align: baseline; font-size: 11pt; font-family: \"Calibri\"; font-style: normal;'>{todo.Date}</span></p></td>"
                                            + "</tr>")
                            .Aggregate(string.Concat)
                        + "</table></td></tr>"
                        

                        + whitespace;
            }

            return string.Concat(rowData, description, todoCreated, whitespace);
        }

        private static string EmailConclusionStats(string todoCompletion, string issuesSolved, string meetingRatingStr, string ellapse, string unit)
        {
            var table = new StringBuilder();
            table.Append(@"<table width=""100%""><tr><td valign=""middle"" align=""center"">");
            table.Append(@"<table width=""500""  border=""0"" cellpadding=""0"" cellspacing=""10"" style=""font-family:Areal, Helvetica, sans-serif"">");
            table.Append(@"	<tr>");
            table.Append(@"		<td width=""250"" height=""100"" valign=""middle"" align=""center"" style=""background-color:#f8f8f8"">");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(issuesSolved).Append("</td></tr></table>");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">Issues solved</td></tr></table>");
            table.Append(@"		</td>");
            table.Append(@"		<td width=""250"" height=""100"" valign=""middle"" align=""center""  style=""background-color:#f8f8f8"">");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(todoCompletion).Append("</td></tr></table>");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">To-do completion</td></tr></table>");
            table.Append(@"		</td>");
            table.Append(@"	</tr>");
            table.Append(@"	<tr>");
            table.Append(@"		<td width=""250""  height=""100"" valign=""middle"" align=""center""  style=""background-color:#f8f8f8"">");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(meetingRatingStr).Append("</td></tr></table>");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">Average Rating</td></tr></table>");
            table.Append(@"		</td>");
            table.Append(@"		<td width=""250""  height=""100"" valign=""middle"" align=""center""  style=""background-color:#f8f8f8"">");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(ellapse).Append("</td></tr></table>");
            table.Append(@"			<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">").Append(unit).Append("</td></tr></table>");
            //table.Append(@"		<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""font-size:24px;font-weight:bold;color:333333;padding: 5px 0px 0px 0px;"">").Append(startTime).Append(" - ").Append(endTime).Append("</td></tr></table>");
            //table.Append(@"		<table cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""color:gray;font-size:12px;padding: 5px 0px 0px 0px;"">").Append(duration).Append("</td></tr></table>");
            table.Append(@"		</td>");
            table.Append(@"	</tr>");
            table.Append(@"</table>");
            table.Append(@"</td></tr></table>");

            return table.ToString();
        }

#endregion

        private static void CloseHeadlines_Unsafe(long meetingId, ISession s, DateTime now, List<PeopleHeadline> headlines) {
			foreach (var headline in headlines) {
				if (headline.CloseTime == null) {
					headline.CloseDuringMeetingId = meetingId;
					headline.CloseTime = now;
				}
				s.Update(headline);
			}
		}

#region Unsafe conclusion methods
		private static void CloseLogsOnConclusion_Unsafe(L10Meeting meeting, ISession s, DateTime now) {
			//End all logs 
			var logs = s.QueryOver<L10Meeting.L10Meeting_Log>()
				.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.EndTime == null)
				.List().ToList();
			foreach (var l in logs) {
				l.EndTime = now;
				s.Update(l);
			}
		}

		private static IEnumerable<L10Meeting.L10Meeting_Attendee> SetConclusionRatings_Unsafe(List<Tuple<long, decimal?>> ratingValues, L10Meeting meeting, ISession s, List<L10Meeting.L10Meeting_Attendee> attendees) {
			var ids = ratingValues.Select(x => x.Item1).ToArray();

			//Set rating for attendees
			var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));
			var raterCount = 0m;
			var raterValue = 0m;

			foreach (var a in raters) {
				a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
				s.Update(a);

				if (a.Rating != null) {
					raterCount += 1;
					raterValue += a.Rating.Value;
				}
			}

			meeting.AverageMeetingRating = new Ratio(raterValue, raterCount);
			s.Update(meeting);
			return raters;
		}

		private static List<L10Meeting.L10Meeting_Attendee> GetMeetingAttendees_Unsafe(long meetingId, ISession s) {
			return s.QueryOver<L10Meeting.L10Meeting_Attendee>()
							.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId)
							.List().ToList();
		}

		private static void CloseIssuesOnConclusion_Unsafe(long recurrenceId, L10Meeting meeting, ISession s, DateTime now) {
			var issuesToClose = s.QueryOver<IssueModel.IssueModel_Recurrence>()
									.Where(x => x.DeleteTime == null && x.MarkedForClose && x.Recurrence.Id == recurrenceId && x.CloseTime == null)
									.List().ToList();
			foreach (var i in issuesToClose) {
				i.CloseTime = now;
				s.Update(i);
			}
		}

		private static async Task SendConclusionTextMessages_Unsafe(long recurrenceId, L10Recurrence recurrence, ISession s, DateTime now) {
			var externalForumNumbers = s.QueryOver<ExternalUserPhone>()
														.Where(x => x.DeleteTime > now && x.ForModel.ModelId == recurrenceId && x.ForModel.ModelType == ForModel.GetModelType<L10Recurrence>())
														.List().ToList();
			if (externalForumNumbers.Any()) {
				try {
					var twilioData = Config.Twilio();
					TwilioClient.Init(twilioData.Sid, twilioData.AuthToken);

					var allMessages = new List<Task<MessageResource>>();
					foreach (var number in externalForumNumbers) {
						try {
							if (twilioData.ShouldSendText) {

								var to = new PhoneNumber(number.UserNumber);
								var from = new PhoneNumber(number.SystemNumber);

								var url = Config.BaseUrl(null, "/su?id=" + number.LookupGuid);
								var message = MessageResource.CreateAsync(to, from: from,
									body: "Thanks for participating in the " + recurrence.Name + "!\nWant a demo of Elite Tools? Click here\n" + url
								);
								allMessages.Add(message);
							}
						} catch (Exception e) {
							log.Error("Particular Forum text was not sent", e);
						}

						number.DeleteTime = now;
						s.Update(number);
					}
					await Task.WhenAll(allMessages);

				} catch (Exception e) {
					log.Error("Forum texts were not sent", e);
				}
			}
		}

		private static string BuildConcludeStatsTable(int tzOffset, Ratio todoCompletion, Ratio meetingRating, DateTime? start, DateTime? end, int issuesSolved)
        {
			try
            {
                var meetingRatingStr = !meetingRating.IsValid() ? "N/A" : "" + (Math.Round(meetingRating.GetValue(0) * 10) / 10m);

                var startTime = "...";
                var endTime = "...";
                var duration = "unconcluded";

                var ellapse = "";
                var unit = "";

                if (start != null)
                {
                    startTime = TimeData.ConvertFromServerTime(start.Value, tzOffset).ToString("HH:mm");
                }

                if (end != null)
                {
                    endTime = TimeData.ConvertFromServerTime(end.Value, tzOffset).ToString("HH:mm");
                }

                if (end != null && start != null)
                {
                    var durationMins = (end.Value - start.Value).TotalMinutes;
                    var durationSecs = (end.Value - start.Value).TotalSeconds;

                    if (durationMins < 0)
                    {
                        duration = "";
                        ellapse = "1";
                        unit = "Minute";
                    }
                    else if (durationMins > 1)
                    {
                        duration = (int)durationMins + " minute".Pluralize((int)durationMins);
                        ellapse = "" + (int)Math.Max(1, durationMins);
                        unit = "Minute".Pluralize((int)durationMins);
                    }
                    else
                    {
                        ellapse = "" + (int)Math.Max(1, durationSecs);
                        duration = (int)(durationSecs) + " second".Pluralize((int)durationSecs);
                        unit = "Second".Pluralize((int)durationSecs);
                    }
                }

                return EmailConclusionStats(todoCompletion.ToPercentage("N/A"), issuesSolved.ToString(), meetingRatingStr, ellapse, unit);
            }
            catch (Exception e) {
				log.Error(e);
			}

            return string.Empty;
		}

        private static Tuple<string, string, string, string, string> BuildConcludeStatsTuple(int tzOffset, Ratio todoCompletion,
            Ratio meetingRating, DateTime? start, DateTime? end, int issuesSolved)
        {
            	try
            {
                var meetingRatingStr = !meetingRating.IsValid() ? "N/A" : "" + (Math.Round(meetingRating.GetValue(0) * 10) / 10m);

                var startTime = "...";
                var endTime = "...";
                var duration = "unconcluded";

                var ellapse = "";
                var unit = "";

                if (start != null)
                {
                    startTime = TimeData.ConvertFromServerTime(start.Value, tzOffset).ToString("HH:mm");
                }

                if (end != null)
                {
                    endTime = TimeData.ConvertFromServerTime(end.Value, tzOffset).ToString("HH:mm");
                }

                if (end != null && start != null)
                {
                    var durationMins = (end.Value - start.Value).TotalMinutes;
                    var durationSecs = (end.Value - start.Value).TotalSeconds;

                    if (durationMins < 0)
                    {
                        duration = "";
                        ellapse = "1";
                        unit = "Minute";
                    }
                    else if (durationMins > 1)
                    {
                        duration = (int)durationMins + " minute".Pluralize((int)durationMins);
                        ellapse = "" + (int)Math.Max(1, durationMins);
                        unit = "Minute".Pluralize((int)durationMins);
                    }
                    else
                    {
                        ellapse = "" + (int)Math.Max(1, durationSecs);
                        duration = (int)(durationSecs) + " second".Pluralize((int)durationSecs);
                        unit = "Second".Pluralize((int)durationSecs);
                    }
                }

                return Tuple.Create(todoCompletion.ToPercentage("N/A"), issuesSolved.ToString(), meetingRatingStr, ellapse, unit);
            }
            catch (Exception e) {
                log.Error(e);
            }

            return Tuple.Create("", "", "", "", "");
        }

        [Queue(HangfireQueues.Immediate.CONCLUSION_EMAIL)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
		[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
		public static async Task SendConclusionEmail_Unsafe(long meetingId, long? onlySendToUser, bool sendToExternal) {

			var unsent = new List<Mail>();
			long recurrenceId = 0;

			var error = "";

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					try {
						var meeting = s.Get<L10Meeting>(meetingId);
						recurrenceId = meeting.L10RecurrenceId;

						var recurrence = s.Get<L10Recurrence>(recurrenceId);
						var attendees = GetMeetingAttendees_Unsafe(meetingId, s);

						var orgTzOffset = recurrence.Organization.GetTimezoneOffset();

                        // get all headlines, todos, issues and attendees of the meeting
						var conclusionItems = ConclusionItems.Get_Unsafe(s, meetingId);
						var headlines = conclusionItems.MeetingHeadlines;
						var todoList = conclusionItems.OutstandingTodos;//s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId && x.CompleteTime == null).List().ToList();
						var issuesForTable = conclusionItems.ClosedIssues;//.Where(x => !x.AwaitingSolve);
						var sendEmailTo = conclusionItems.SendEmailsTo;

                        // context todos that is related to the issue 
                        var todoContext = todoList.Where(l => l.ForModel == "IssueModel"
                                                              && issuesForTable.Select(ft => ft.Issue.Id)
                                                                  .Contains(l.ForModelId));

						if (onlySendToUser != null) {
							sendEmailTo = sendEmailTo.Where(x => x.UserId == onlySendToUser.Value).ToList();
						}

						//All awaitables 
						//headline.CloseDuringMeetingId = meeting.Id;

                        // get Notes for Issues, Todo and Headlines
                        var pads = issuesForTable
                            .Select(x => x.Issue.PadId)
                            .Union(todoList.Select(x => x.PadId))
                            .Union(headlines.Select(x => x.HeadlinePadId))
                            .ToList();
						var padTexts = await PadAccessor.GetHtmls(pads);

						/////
						//var headlineTable = await HeadlineAccessor.BuildHeadlineTable(headlines.ToList(), "Headlines", recurrenceId, true, padTexts);
                        var headlineList = headlines.Select(hl => 		
                            new EmailConclusionData(
                                //(hl.About.NotNull(x => x.GetName()) ?? hl.AboutName),
                                hl.Owner.GetName(), 
                                hl.Message, 
                                string.Empty, 
                                padTexts[hl.HeadlinePadId].ToHtmlString()) // this should have been filled up above. No need to retrieve again
                        ).ToArray();

                        EmailConclusionData TodoModelToConclusionData(TodoModel tl)
                        {
                            var tzOffset = tl.AccountableUser.GetTimezoneOffset();
                            var format = tl.AccountableUser.GetTimeSettings().DateFormat ?? "MM-dd-yyyy";
                            var dueDate = TimeData.ConvertFromServerTime(tl.DueDate.Date, tzOffset);
                            return new EmailConclusionData(tl.AccountableUser.GetName(), tl.Message, dueDate.ToString(format), null);
                        }

                        // Issues
                        var issuesList = issuesForTable
                            .OrderBy(it => it.CloseTime)
                            .Select(it =>
                                {
                                    // get all todo that associated with the current issue if any
                                    var todoIssueContext = todoContext.Where(t => t.ForModelId == it.Issue.Id)
                                        .Select(TodoModelToConclusionData);

                                    return new EmailConclusionIssueData(it.CreatedBy.GetName(),
                                        it.Issue.Message, it.CloseTime.ToString(),
                                        padTexts[it.Issue.PadId].ToHtmlString(),
                                        todoIssueContext?.ToList());
                                }
                            ).ToList();


                        var allUserIds = todoList.Select(x => x.AccountableUserId)
                            .Union(attendees.Select(x => x.User.Id))
                            .Distinct().ToList();
                        
                        // all users in TodoList and Attendees
                        var auLu = s.QueryOver<UserOrganizationModel>()
                            .WhereRestrictionOn(x => x.Id)
                            .IsIn(allUserIds)
                            .List()
                            .ToDefaultDictionary(u => u.Id,
                                u => u,
                                u => null);

                        var todoListDisplay = todoList.Select(TodoModelToConclusionData).ToArray();


                        // attendees
                        var emailOffset = sendEmailTo.Select(u => new
                            { Name = u.User.GetName(), Email = auLu[u.User.Id].GetEmail(), Offset = auLu[u.User.Id].GetTimezoneOffset()})
                            .ToList();
                            
                        // external
						if (sendToExternal && onlySendToUser == null) {
							var alsoSendTo = s.QueryOver<MeetingSummaryWhoModel>()
								.Where(x => x.RecurrenceId == recurrenceId && x.DeleteTime == null)
								.List().ToList();

                            // add to list of emails
                            emailOffset.AddRange(alsoSendTo.Where(x => x.Type == MeetingSummaryWhoType.Email)
                                .Select(u => new { Name = "Hello", Email = u.Who, Offset = orgTzOffset}));

                        }

                        // iterate each email
                        var productName = Config.ProductName(recurrence.Organization);
                        emailOffset.ForEach(eo =>
                        {
                            var stats = BuildConcludeStatsTuple(eo.Offset, meeting.TodoCompletion,
                                                         meeting.AverageMeetingRating, meeting.StartTime, meeting.CompleteTime,
                                                         conclusionItems.ClosedIssues.Count);
                            var body = BuildEmailConclusion(recurrence.Name, productName, stats, todoListDisplay, headlineList, issuesList);


                            var mail = Mail.To(EmailTypes.L10Summary, eo.Email)
                                .Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
                                .Body(EmailStrings.MeetingSummary_Body_New, eo.Name, body);
                                
                            unsent.Add(mail);
                        });

					} catch (Exception e) {
						log.Error("Emailer issue(1):" + recurrenceId, e);
						error += "(1)" + e.Message;
					}

					tx.Commit();
					s.Flush();
				}
			}

			try {
				if (unsent.Any()) {
					await Emailer.SendEmails(unsent, wrapped: false);
				}
			} catch (Exception e) {
				log.Error("Emailer issue(2):" + recurrenceId, e);
				error += "(2)" + e.Message;
			}

			if (!string.IsNullOrWhiteSpace(error)) {
				throw new Exception(error);
			}

		}

        [Queue(HangfireQueues.Immediate.CONCLUSION_EMAIL)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
		[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
		public static async Task SendConclusionEmail_Unsafe2(long meetingId, long? onlySendToUser, bool sendToExternal) {

			var unsent = new List<Mail>();
			long recurrenceId = 0;

			var error = "";

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					try {
						var meeting = s.Get<L10Meeting>(meetingId);
						recurrenceId = meeting.L10RecurrenceId;

						var recurrence = s.Get<L10Recurrence>(recurrenceId);
						var attendees = GetMeetingAttendees_Unsafe(meetingId, s);

						var orgTzOffset = recurrence.Organization.GetTimezoneOffset();

						var conclusionItems = ConclusionItems.Get_Unsafe(s, meetingId);
						var headlines = conclusionItems.MeetingHeadlines;
						var todoList = conclusionItems.OutstandingTodos;//s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId && x.CompleteTime == null).List().ToList();
						var issuesForTable = conclusionItems.ClosedIssues;//.Where(x => !x.AwaitingSolve);
						var sendEmailTo = conclusionItems.SendEmailsTo;


						if (onlySendToUser != null) {
							sendEmailTo = sendEmailTo.Where(x => x.UserId == onlySendToUser.Value).ToList();
						}

						//All awaitables 
						//headline.CloseDuringMeetingId = meeting.Id;


						var pads = issuesForTable.Select(x => x.Issue.PadId).ToList();
						pads.AddRange(todoList.Select(x => x.PadId));
						pads.AddRange(headlines.Select(x => x.HeadlinePadId));
						var padTexts = await PadAccessor.GetHtmls(pads);

						/////
						var headlineTable = await HeadlineAccessor.BuildHeadlineTable(headlines.ToList(), "Headlines", recurrenceId, true, padTexts);

						var issueTable = await IssuesAccessor.BuildIssuesSolvedTable(issuesForTable.ToList(), "Issues Solved", recurrenceId, true, padTexts);
						var todosTable = new DefaultDictionary<long, string>(x => "");
						var hasTodos = new DefaultDictionary<long, bool>(x => false);

						var allUserIds = todoList.Select(x => x.AccountableUserId).ToList();
						allUserIds.AddRange(attendees.Select(x => x.User.Id));
						allUserIds = allUserIds.Distinct().ToList();
                        // all users in TodoList and Attendees
						var allUsers = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(allUserIds).List().ToList();

						var auLu = new DefaultDictionary<long, UserOrganizationModel>(x => null);
						foreach (var u in allUsers) {
							auLu[u.Id] = u;
						}

						foreach (var personTodos in todoList.GroupBy(x => x.AccountableUserId)) {
							var user = auLu[personTodos.First().AccountableUserId];
							//var email = user.GetEmail();

							if (personTodos.Any()) {
								hasTodos[personTodos.First().AccountableUserId] = true;
							}

							var tzOffset = personTodos.First().AccountableUser.GetTimezoneOffset();
							var timeFormat = personTodos.First().AccountableUser.GetTimeSettings().DateFormat;

							var todoTable = await TodoAccessor.BuildTodoTable(personTodos.ToList(), tzOffset, timeFormat, "Outstanding To-dos", true, padLookup: padTexts);


							var output = new StringBuilder();

							output.Append(todoTable.ToString());
							output.Append("<br/>");
							todosTable[user.Id] = output.ToString();
						}

						foreach (var userAttendee in sendEmailTo) {
							var output = new StringBuilder();
							var user = auLu[userAttendee.User.Id];
							var email = user.GetEmail();
							var toSend = false;

							var concludeStats = BuildConcludeStatsTable(user.GetTimezoneOffset(), meeting.TodoCompletion, meeting.AverageMeetingRating, meeting.StartTime, meeting.CompleteTime, conclusionItems.ClosedIssues.Count);

							output.Append(concludeStats);
							toSend = true;//Always send, we have stats now.

							output.Append("<br/>");

							if (hasTodos[userAttendee.User.Id]) {
								toSend = true;
							}

							output.Append(todosTable[user.Id]);
							if (issuesForTable.Any()) {
								output.Append(issueTable.ToString());
								toSend = true;
							}


							if (headlines.Any()) {
								output.Append(headlineTable.ToString());
								output.Append("<br/>");
								toSend = true;
							}


							var mail = Mail.To(EmailTypes.L10Summary, email)
								.Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
								.Body(EmailStrings.MeetingSummary_Body, user.GetName(), output.ToString(), Config.ProductName(recurrence.Organization));
							if (toSend) {
								unsent.Add(mail);
							}
						}

						if (sendToExternal && onlySendToUser == null) {
							var alsoSendTo = s.QueryOver<MeetingSummaryWhoModel>()
								.Where(x => x.RecurrenceId == recurrenceId && x.DeleteTime == null)
								.List().ToList();

							foreach (var a in alsoSendTo.Where(x => x.Type == MeetingSummaryWhoType.Email)) {
								var email = a.Who;
								var output = new StringBuilder();
								var toSend = false;

								var concludeStats = BuildConcludeStatsTable(orgTzOffset, meeting.TodoCompletion, meeting.AverageMeetingRating, meeting.StartTime, meeting.CompleteTime, conclusionItems.ClosedIssues.Count);

								output.Append(concludeStats);
								toSend = true;//Always send, we have stats now.

								output.Append("<br/>");

								if (issuesForTable.Any()) {
									output.Append(issueTable.ToString());
									toSend = true;
								}
								if (headlines.Any()) {
									output.Append(headlineTable.ToString());
									output.Append("<br/>");
									toSend = true;
								}

								var mail = Mail.To(EmailTypes.L10Summary, email)
								.Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
								.Body(EmailStrings.MeetingSummary_Body, "Hello", output.ToString(), Config.ProductName(recurrence.Organization));
								if (toSend) {
									unsent.Add(mail);
								}
							}

						}

					} catch (Exception e) {
						log.Error("Emailer issue(1):" + recurrenceId, e);
						error += "(1)" + e.Message;
					}

					tx.Commit();
					s.Flush();
				}
			}

			try {
				if (unsent.Any()) {
					await Emailer.SendEmails(unsent);
				}
			} catch (Exception e) {
				log.Error("Emailer issue(2):" + recurrenceId, e);
				error += "(2)" + e.Message;
			}

			if (!string.IsNullOrWhiteSpace(error)) {
				throw new Exception(error);
			}

		}

#endregion

		public static async Task UpdateRating(UserOrganizationModel caller, List<System.Tuple<long, decimal?>> ratingValues, long meetingId, string connectionId) {

			L10Meeting meeting = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;
					//Make sure we're unstarted
					var perms = PermissionsUtility.Create(s, caller);
					meeting = s.QueryOver<L10Meeting>().Where(t => t.Id == meetingId).SingleOrDefault();
					perms.ViewL10Meeting(meeting.Id);


					var ids = ratingValues.Select(x => x.Item1).ToArray();

					//Set rating for attendees
					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.List().ToList();
					var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));

					foreach (var a in raters) {
						a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
						s.Update(a);
					}

					Audit.L10Log(s, caller, meeting.L10RecurrenceId, "UpdateL10Rating", ForModel.Create(meeting));
					tx.Commit();
					s.Flush();
				}
			}
		}



		public static IEnumerable<L10Recurrence.L10Recurrence_Connection> GetConnected(UserOrganizationModel caller, long recurrenceId, bool load = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var connections = s.QueryOver<L10Recurrence.L10Recurrence_Connection>().Where(x => x.DeleteTime >= DateTime.UtcNow && x.RecurrenceId == recurrenceId).List().ToList();
					if (load) {
						var userIds = connections.Select(x => x.UserId).Distinct().ToArray();
						var tiny = TinyUserAccessor.GetUsers_Unsafe(s, userIds).ToDefaultDictionary(x => x.UserOrgId, x => x, null);
						foreach (var c in connections) {
							c._User = tiny[c.UserId];
						}
					}
					return connections;
				}
			}
		}

		public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//var perms = PermissionsUtility.
					if (recurrenceId == -3) {
						var recurs = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null)
							.WhereRestrictionOn(x => x.User.Id).IsIn(caller.UserIds)
							.Select(x => x.L10Recurrence.Id)
							.List<long>().ToList();
						//Hey.. this doesnt grab all visible meetings.. it should be adjusted when we know that GetVisibleL10Meetings_Tiny is optimized
						//GetVisibleL10Meetings_Tiny(s, perms, caller.Id);
						foreach (var r in recurs) {
							hub.Groups.Add(connectionId, RealTimeHub.Keys.GenerateMeetingGroupId(r));
						}
						hub.Groups.Add(connectionId, RealTimeHub.Keys.UserId(caller.Id));
					} else {
						PermissionsAccessor.Permitted(caller, x => x.ViewL10Recurrence(recurrenceId));
						hub.Groups.Add(connectionId, RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));
						Audit.L10Log(s, caller, recurrenceId, "JoinL10Meeting", ForModel.Create(caller));

#pragma warning disable CS0618 // Type or member is obsolete
						var connection = new L10Recurrence.L10Recurrence_Connection() { Id = connectionId, RecurrenceId = recurrenceId, UserId = caller.Id };
#pragma warning restore CS0618 // Type or member is obsolete

						s.SaveOrUpdate(connection);

						connection._User = TinyUser.FromUserOrganization(caller);

						var perms = PermissionsUtility.Create(s, caller);
						var currentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
						if (currentMeeting != null) {
							var isAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.L10Meeting.Id == currentMeeting.Id && x.User.Id == caller.Id && x.DeleteTime == null).RowCount() > 0;
							if (!isAttendee) {
								var potentialAttendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id && x.L10Recurrence.Id == recurrenceId).RowCount() > 0;
								if (potentialAttendee) {
									s.Save(new L10Meeting.L10Meeting_Attendee() {
										L10Meeting = currentMeeting,
										User = caller,
									});
								}
							}
						}

						tx.Commit();
						s.Flush();

						var meetingHub = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));
						meetingHub.userEnterMeeting(connection);
						//?meetingHub.userEnterMeeting(caller.Id, connectionId, caller.GetName(), caller.ImageUrl(true));
					}
				}
			}

			return null;
		}
#endregion




#region toDelete

		public static async Task __TestConclude(ISession s, ITransaction tx, UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, decimal?>> ratingValues, ConcludeSendEmail sendEmail, bool closeTodos, bool closeHeadlines, string connectionId) {
			var now = DateTime.UtcNow;
			//Make sure we're unstarted
			var perms = PermissionsUtility.Create(s, caller);
			var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId, false);
			perms.ViewL10Meeting(meeting.Id);

			var todoRatio = new Ratio();
			var todos = L10Accessor.GetTodosForRecurrence(s, perms, recurrenceId, meeting.Id);

			foreach (var todo in todos) {
				if (todo.CreateTime < meeting.StartTime) {
					if (todo.CompleteTime != null) {
						todo.CompleteDuringMeetingId = meeting.Id;
						if (closeTodos) {
							todo.CloseTime = now;
						}
						s.Update(todo);
					}
					todoRatio.Add(todo.CompleteTime != null ? 1 : 0, 1);
				}
			}

			var headlines = L10Accessor.GetHeadlinesForMeeting(s, perms, recurrenceId);
			if (closeHeadlines) {
				L10Accessor.CloseHeadlines_Unsafe(meeting.Id, s, now, headlines);
			}


			//Conclude the forum
			var recurrence = s.Get<L10Recurrence>(recurrenceId);
			await L10Accessor.SendConclusionTextMessages_Unsafe(recurrenceId, recurrence, s, now);

			L10Accessor.CloseIssuesOnConclusion_Unsafe(recurrenceId, meeting, s, now);

			meeting.TodoCompletion = todoRatio;
			meeting.CompleteTime = now;
			meeting.SendConcludeEmailTo = sendEmail;
			s.Update(meeting);

			var attendees = L10Accessor.GetMeetingAttendees_Unsafe(meeting.Id, s);
			var raters = L10Accessor.SetConclusionRatings_Unsafe(ratingValues, meeting, s, attendees);

			L10Accessor.CloseLogsOnConclusion_Unsafe(meeting, s, now);

			//Close all sub issues
			IssueModel issueAlias = null;
			var issue_recurParents = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.CloseTime >= meeting.StartTime && x.CloseTime <= meeting.CompleteTime && x.Recurrence.Id == recurrenceId)
				.List().ToList();
			L10Accessor._RecursiveCloseIssues(s, issue_recurParents.Select(x => x.Id).ToList(), now);


			recurrence.MeetingInProgress = null;
			recurrence.SelectedVideoProvider = null;
			s.Update(recurrence);

			var sendEmailTo = new List<L10Meeting.L10Meeting_Attendee>();

			//send emails
			if (sendEmail != ConcludeSendEmail.None) {
				switch (sendEmail) {
					case ConcludeSendEmail.AllAttendees:
						sendEmailTo = attendees;
						break;
					case ConcludeSendEmail.AllRaters:
						sendEmailTo = raters.ToList();
						break;
					default:
						break;
				}
			}

			ConclusionItems.Save_Unsafe(recurrenceId, meeting.Id, s, todos.Where(x => x.CloseTime == null).ToList(), headlines, issue_recurParents, sendEmailTo);

			await Trigger(x => x.Create(s, EventType.ConcludeMeeting, caller, recurrence, message: recurrence.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));

			Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting", ForModel.Create(meeting));

			tx.Rollback();
		}

#endregion
	}
}
