using RadialReview.Accessors;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Application;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace RadialReview.Api.V1 {
	/// <summary>
	/// Create or update a Level 10 Meeting
	/// </summary>
	[RoutePrefix("api/v1")]
	public class L10Controller : BaseApiController {


		public class CreateMeeting {
			/// <summary>
			/// Meeting Name
			/// </summary>
			public string title { get; set; }
			/// <summary>
			/// Add yourself to the meeting (Default: false)
			/// </summary>
			public bool addSelf { get; set; }
		}

		public class CreatedMeeting {
			public long meetingId { get; set; }
		}
		// PUT: api/L10
		/// <summary>
		/// Create a new Level 10 meeting.
		/// </summary>
		/// <returns>The meeting ID</returns>
		[Route("L10/create")]
		[HttpPost]
		public async Task<CreatedMeeting> CreateL10([FromBody]CreateMeeting body) {
			var _recurrence = await L10Accessor.CreateBlankRecurrence(GetUser(), GetUser().Organization.Id, false);
			await L10Accessor.UpdateRecurrence(GetUser(), _recurrence.Id, body.title);
			if (body.addSelf) {
				await L10Accessor.AddAttendee(GetUser(), _recurrence.Id, GetUser().Id);
			}
			return new CreatedMeeting {
				meetingId = _recurrence.Id
			};
		}

		/// <summary>
		/// Update a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="name">Updated meeting name</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}")]
		[HttpPut]
		public async Task EditL10(long MEETING_ID, [FromBody]TitleModel body) {
			await L10Accessor.UpdateRecurrence(GetUser(), MEETING_ID, body.title);
		}
		/// <summary>
		/// Delete a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}")]
		[HttpDelete]
		public async Task RemoveL10(long MEETING_ID) {
			await L10Accessor.DeleteL10Recurrence(GetUser(), MEETING_ID);
		}

		/// <summary>
		/// Add an existing scorecard measurable to a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="MEASURABLE_ID">Scorecard measurable ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/measurables/{MEASURABLE_ID}")]
		[HttpPost]
		public async Task AttachMeasurableL10(long MEETING_ID, long MEASURABLE_ID) {
			await L10Accessor.AttachMeasurable(GetUser(), MEETING_ID, MEASURABLE_ID);
		}


		/// <summary>
		/// Remove a scorecard measurable from a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="MEASURABLE_ID">Scorecard measurable ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/measurables/{MEASURABLE_ID}")]
		[HttpDelete]
		public async Task RemoveMeasurableL10(long MEETING_ID, long MEASURABLE_ID) {
			await L10Accessor.Remove(GetUser(), new AngularMeasurable() { Id = MEASURABLE_ID }, MEETING_ID, null);
		}

		/// <summary>
		/// Add an existing rock to a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="ROCK_ID">Rock ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/rocks/{ROCK_ID}")]
		[HttpPost]
		public async Task AttachRockMeetingL10(long MEETING_ID, long ROCK_ID) {
			await L10Accessor.AttachRock(GetUser(), MEETING_ID, ROCK_ID, false, AttachRockType.Existing);
		}

		/// <summary>
		/// Remove a rock from a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="ROCK_ID">Rock ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/rocks/{ROCK_ID}")]
		[HttpDelete]
		public async Task RemoveRockL10(long MEETING_ID, long ROCK_ID) {
			await L10Accessor.Remove(GetUser(), new AngularRock() { Id = ROCK_ID }, MEETING_ID, null);
		}



		public class CreateTodo {
			/// <summary>
			/// To-do title
			/// </summary>
			[Required]
			public string title { get; set; }
			/// <summary>
			/// To-do notes (Default: none)
			/// </summary>
			public string details { get; set; }
			/// <summary>
			/// Accountable user (Default: you)
			/// </summary>
			public long? accountableUserId { get; set; }
			/// <summary>
			/// To-do due date (Default: 7 days)
			/// </summary>
			public DateTime? dueDate { get; set; }

		}


		/// <summary>
		/// Add a to-do to a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="title">To-do title</param>
		/// <param name="accountableUserId">Accountable user ID (Default: you)</param>
		/// <param name="dueDate">Due Date (Default: 7 days from now)</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/todos")]
		[HttpPost]
		public async Task<AngularTodo> CreateTodoL10(long MEETING_ID, [FromBody]CreateTodo body) {
			if (!body.dueDate.HasValue) {
				body.dueDate = DateTime.Now.AddDays(7);
			}
			//var model = new TodoModel() {
			//	Message = body.title,
			//	Details = body.details,
			//	DueDate = body.dueDate.Value,
			//	AccountableUserId = body.accountableUserId ?? GetUser().Id,
			//	ForRecurrenceId = MEETING_ID
			//};
			var model = TodoCreation.GenerateL10Todo(MEETING_ID, body.title, body.details, body.accountableUserId ?? GetUser().Id, body.dueDate.Value);

			var todo = await TodoAccessor.CreateTodo(GetUser(), model);
			return new AngularTodo(todo);
		}

		public class CreateIssue {
			/// <summary>
			/// Issue title
			/// </summary>
			[Required]
			public string title { get; set; }
			/// <summary>
			/// Owner Id (Default: you)
			/// </summary>
			public long? ownerId { get; set; }
			/// <summary>
			/// Issue details (Default: none)
			/// </summary>
			public string details { get; set; }
		}


		/// <summary>
		/// Add an issue to a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="title">Issue title</param>
		/// <param name="ownerId">Owner's user ID (Default: you)</param>
		/// <param name="details">Issue details (Default: none)</param>
		/// <returns>The created issue</returns>
		[Route("L10/{MEETING_ID}/issues")]
		[HttpPost]
		public async Task<AngularIssue> CreateIssueL10(long MEETING_ID, [FromBody]CreateIssue body) {
			body.ownerId = body.ownerId ?? GetUser().Id;
			//var issue = new IssueModel() { Message = body.title, Description = body.details };
			var creation = IssueCreation.CreateL10Issue(body.title, body.details, body.ownerId, MEETING_ID);
			var success = await IssuesAccessor.CreateIssue(GetUser(), creation);// MEETING_ID, body.ownerId.Value, issue);
			return new AngularIssue(success.IssueRecurrenceModel);

		}

		/// <summary>
		/// Remove an issue from a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="ISSUE_ID">Issue ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/issues/{ISSUE_ID}")]
		[HttpDelete]
		public async Task RemoveIssueL10(long MEETING_ID, long ISSUE_ID) {
			await L10Accessor.Remove(GetUser(), new AngularIssue() { Id = ISSUE_ID }, MEETING_ID, null);
		}

		public class CreateHeadline {
			/// <summary>
			/// Headline title
			/// </summary>
			[Required]
			public string title { get; set; }
			/// <summary>
			/// Owner Id (Default: you)
			/// </summary>
			public long? ownerId { get; set; }
			/// <summary>
			/// Optional headline notes (Default: none)
			/// </summary>
			public string notes { get; set; }
		}

		/// <summary>
		/// Create a people headline for a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="title">People headline title</param>
		/// <param name="ownerId">People headline owner ID</param>
		/// <param name="details">People headline details</param>
		/// <returns>The created people headline</returns>
		[Route("L10/{MEETING_ID}/headlines")]
		[HttpPost]
		public async Task<AngularHeadline> CreateHeadlineL10(long MEETING_ID, [FromBody]CreateHeadline body) {
			body.ownerId = body.ownerId ?? GetUser().Id;

			var headline = new PeopleHeadline() {
				Message = body.title,
				OwnerId = body.ownerId.Value,
				_Details = body.notes,
				RecurrenceId = MEETING_ID,
				OrganizationId = GetUser().Organization.Id
			};
			var success = await HeadlineAccessor.CreateHeadline(GetUser(), headline);
			if (!success) {
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			}

			return new AngularHeadline(headline);
		}

		public class CreateRockModel {
			/// <summary>
			/// Rock name
			/// </summary>
			[Required]
			public string title { get; set; }

			/// <summary>
			/// Rock owner (Default: you)
			/// </summary>
			public long? accountableUserId { get; set; }

		}

		/// <summary>
		/// Create a new rock and move it to L10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="title">Rock title</param>
		/// <param name="accountableUserId">Rock owner ID</param>
		/// <returns>The created rock</returns>
		[Route("L10/{MEETING_ID}/rocks")]
		[HttpPost]
		public async Task<AngularRock> CreateRockL10(long MEETING_ID, [FromBody]CreateRockModel body) {
			var ownerId = (body.accountableUserId == 0 || body.accountableUserId == null) ? GetUser().Id : body.accountableUserId.Value;
			var rock = await RockAccessor.CreateRock(GetUser(), ownerId, message: body.title);
			await L10Accessor.AttachRock(GetUser(), MEETING_ID, rock.Id, false, AttachRockType.Create);


			var response = new AngularRock(rock, false);
			var l10s = RockAccessor.GetRecurrencesContainingRock(GetUser(), rock.Id);
			foreach (var l10 in l10s) {
				response.Origins.Add(new NameId(l10.Name, l10.RecurrenceId));
			}

			return response;
		}

		/// <summary>
		/// Remove a headline from a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="HEADLINE_ID">People headline ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/headlines/{HEADLINE_ID}")]
		[HttpDelete]
		public async Task RemoveHeadlineL10(long MEETING_ID, long HEADLINE_ID) {
			await L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = HEADLINE_ID }, MEETING_ID, null);
		}


		/// <summary>
		/// Get information about a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <returns>The Level 10 meeting</returns>
		[Route("L10/{MEETING_ID}")]
		[HttpGet]
		public async Task<AngularRecurrence> GetL10(long MEETING_ID) {
			return await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), MEETING_ID, includeCreatedBy: true);
		}

		/// <summary>
		/// Get a list of attendees
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <returns>A list of attendee users</returns>
		[Route("L10/{MEETING_ID}/attendees")]
		[HttpGet]
		public IEnumerable<AngularUser> GetL10Attendees(long MEETING_ID) {
			return L10Accessor.GetAttendees(GetUser(), MEETING_ID).Select(x => AngularUser.CreateUser(x));
		}

		/// <summary>
		/// Add an existing user to a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
		[Route("L10/{MEETING_ID}/attendees/{USER_ID}")]
		[HttpPost]
		public async Task AddAttendee(long MEETING_ID, long USER_ID) {
			await L10Accessor.AddAttendee(GetUser(), MEETING_ID, USER_ID);
		}

		/// <summary>
		/// Get a list of Level 10 meetings
		/// </summary>
		/// <returns>A list of meetings</returns>
		[Route("L10/list")]
		[HttpGet]
		public IEnumerable<NameId> GetL10List() {
			return L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id);
		}

		/// <summary>
		/// Get list of L10 meetings attended by a particular user
		/// </summary>
		///  /// <param name="USER_ID">User Id</param>
		/// <returns>A list of meetings</returns>
		[Route("L10/{USER_ID}/list")]
		[HttpGet]
		public IEnumerable<NameId> GetUserL10List(long USER_ID) {
			return L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), USER_ID, true);
		}

		/// <summary>
		/// Get a list of issues for a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting Id</param>
		/// <param name="INCLUDE_RESOLVED">Issue Status whether solved or unresolve (Default: false)</param>
		/// <returns>List of issues</returns>
		[Route("l10/{MEETING_ID}/issues")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetRecurrenceIssues(long MEETING_ID, bool INCLUDE_RESOLVED = false) {
			return L10Accessor.GetIssuesForRecurrence(GetUser(), MEETING_ID, INCLUDE_RESOLVED).Select(x => new AngularIssue(x));
		}

		/// <summary>
		/// Get list of long term issues for a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting Id</param>
		/// <returns>List of long term issues</returns>
		[Route("l10/{MEETING_ID}/longtermissues")]
		[HttpGet]
		[ApiExplorerSettings(IgnoreApi = true)]
		public IEnumerable<AngularIssue> GetRecurrenceLongTermIssues(long MEETING_ID) {
			return L10Accessor.GetLongTermIssuesForRecurrence(GetUser(), MEETING_ID);
			//return list.Select(x => new AngularIssue(x));
		}
		/// <summary>
		/// Get a list of issues in a Level 10 meeting for a particular user
		/// </summary>
		/// <param name="MEETING_ID"></param>
		/// <param name="USER_ID"></param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/users/{USER_ID}/issues")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetUserIssues(long USER_ID, long MEETING_ID) {
			return IssuesAccessor.GetRecurrenceIssuesForUser(GetUser(), USER_ID, MEETING_ID).Select(x => new AngularIssue(x));
		}

		/// <summary>
		/// Get a list of to-dos in a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="INCLUDE_CLOSED">Todo Status whether closed or open (Default: false)</param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/todos")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetRecurrenceTodos(long MEETING_ID, bool INCLUDE_CLOSED = false) {
			//await L10Accessor.CreateBlankRecurrence()
			return L10Accessor.GetAllTodosForRecurrence(GetUser(), MEETING_ID, INCLUDE_CLOSED).Select(x => new AngularTodo(x));
		}

		/// <summary>
		/// Get a list of todos in a Level 10 meeting for a particular user
		/// </summary>
		/// <param name="MEETING_ID"></param>
		/// <param name="USER_ID"></param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/users/{USER_ID}/todos")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetUserTodos(long USER_ID, long MEETING_ID) {
			return L10Accessor.GetUserTodosForRecurrence(GetUser(), USER_ID, MEETING_ID).Select(x => new AngularTodo(x));
		}

		/// <summary>
		/// Get a list of people headlines in a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/headlines")]
		[HttpGet]
		public IEnumerable<AngularHeadline> GetRecurrenceHeadlines(long MEETING_ID, bool Include_Origin = false) {
			//await L10Accessor.CreateBlankRecurrence()
			var response = L10Accessor.GetHeadlinesForMeeting(GetUser(), MEETING_ID).Select(x => new AngularHeadline(x));
			if (Include_Origin) {
				var origin = L10Accessor.GetL10Recurrence(GetUser(), MEETING_ID, LoadMeeting.False());
				response = response.ToList();
				foreach (var headline in response) {
					headline.Origin = origin.Name;
					headline.OriginId = origin.Id;
				}
			}
			return response;
		}

		/// <summary>
		/// Get a list of headlines in a Level 10 meeting for a particular user
		/// </summary>
		/// <param name="MEETING_ID"></param>
		/// <param name="USER_ID"></param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/users/{USER_ID}/headlines")]
		[HttpGet]
		public IEnumerable<AngularHeadline> GetUserHeadlines(long USER_ID, long MEETING_ID, bool Include_Origin = false) {
			var response = HeadlineAccessor.GetRecurrenceHeadlinesForUser(GetUser(), USER_ID, MEETING_ID).Select(x => new AngularHeadline(x));
			if (Include_Origin) {
				var origin = L10Accessor.GetL10Recurrence(GetUser(), MEETING_ID, LoadMeeting.False());
				response = response.ToList();
				foreach (var headline in response) {
					headline.Origin = origin.Name;
					headline.OriginId = origin.Id;
				}
			}
			return response;
		}

		/// <summary>
		/// Get a list of rocks in a Level 10 meeting for a particular user
		/// </summary>
		/// <param name="MEETING_ID"></param>
		/// <param name="USER_ID"></param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/users/{USER_ID}/rocks")]
		[HttpGet]
		public IEnumerable<AngularRock> GetUserRocks(long USER_ID, long MEETING_ID, bool INCLUDE_ORIGIN = false) {
			var response = RockAccessor.GetRecurrenceRocksForUser(GetUser(), USER_ID, MEETING_ID).Select(x => new AngularRock(x));
			if (INCLUDE_ORIGIN) {
				response = response.ToList();
				foreach (var rock in response) {
					var l10s = RockAccessor.GetRecurrencesContainingRock(GetUser(), rock.Id);
					foreach (var l10 in l10s) {
						rock.Origins.Add(new NameId(l10.Name, l10.RecurrenceId));
					}
				}
			}
			return response;

		}

		//      /// <summary>
		//      /// Get a list of rocks in a Level 10 meeting
		//      /// </summary>
		//      /// <param name="MEETING_ID">Meeting ID</param>
		//      /// <returns></returns>
		//      ///
		//      [HttpGet]
		//[Route("l10/{MEETING_ID}/rocks")]
		//public IEnumerable<AngularRock> GetRecurrenceRocks(long MEETING_ID) {
		//	return L10Accessor.GetRocksForRecurrence(GetUser(), MEETING_ID).Select(x => new AngularRock(x));
		//}

		[HttpGet]
		[ApiExplorerSettings(IgnoreApi = true)]
		[Route("l10/{MEETING_ID}/rocks")]
		public IEnumerable<AngularRock> GetRecurrenceRocks(long MEETING_ID, DateTime archive) {
			return L10Accessor.GetRocksForRecurrence(GetUser(), MEETING_ID, true)
				.Where(x => x.DeleteTime == null || x.DeleteTime >= archive)
				.Select(x => new AngularRock(x));

		}

		/// <summary>
		/// Get list of L10 rocks
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <param name="INCLUDE_ARCHIVE">Include Archive  (Default: false)</param>
		/// <returns>A list of L10 rocks</returns>
		[Route("L10/{MEETING_ID}/rocks")]
		[HttpGet]
		public IEnumerable<AngularRock> GetL10Rocks(long MEETING_ID, bool INCLUDE_ARCHIVE = false, bool INCLUDE_ORIGIN = false) {
			var response = L10Accessor.GetRocksForRecurrence(GetUser(), MEETING_ID, INCLUDE_ARCHIVE).Select(x => new AngularRock(x)).ToList();
			if (INCLUDE_ORIGIN) {
				response = response.ToList();
				foreach (var rock in response) {
					var l10s = RockAccessor.GetRecurrencesContainingRock(GetUser(), rock.Id);
					foreach (var l10 in l10s) {
						rock.Origins.Add(new Models.Application.NameId(l10.Name, l10.RecurrenceId));
					}
				}
			}
			return response;
		}

		/// <summary>
		/// Get a list of measurables in a Level 10 meeting
		/// </summary>
		/// <param name="MEETING_ID">Meeting ID</param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/measurables")]
		[HttpGet]
		public IEnumerable<AngularMeasurable> GetRecurrenceMeasurables(long MEETING_ID) {
			var response = L10Accessor.GetMeasurablesForRecurrence(GetUser(), MEETING_ID).Select(x => new AngularMeasurable(x.Measurable));
			return response;
		}

		/// <summary>
		/// Get a list of measurables in a Level 10 meeting for a particular user
		/// </summary>
		/// <param name="MEETING_ID"></param>
		/// <param name="USER_ID"></param>
		/// <returns></returns>
		[Route("l10/{MEETING_ID}/users/{USER_ID}/measurables")]
		[HttpGet]
		public IEnumerable<AngularMeasurable> GetUserMeasurables(long USER_ID, long MEETING_ID) {
			return ScorecardAccessor.GetRecurrenceMeasurablesForUser(GetUser(), USER_ID, MEETING_ID).Select(x => new AngularMeasurable(x.Measurable));
		}

		///// <summary>
		///// Create a to-do for particular Level 10 meeting
		///// </summary>
		///// <param name="MEETING_ID">Meeting ID</param>
		///// <param name="title">To-do title</param>
		///// <param name="dueDate">To-do due date (Default: 7 days from now)</param>
		///// <returns></returns>
		//      [Route("l10/{MEETING_ID}/todo")]
		//      [HttpPost]
		//      public async Task CreateTodo(long MEETING_ID, [FromBody]string title, [FromBody]DateTime? dueDate=null) {
		//          dueDate = dueDate ?? DateTime.UtcNow.AddDays(7);
		//          await TodoAccessor.CreateTodo(GetUser(), MEETING_ID, new TodoModel() { Message = title, DueDate = dueDate.Value });
		//      }
	}
}
