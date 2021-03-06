﻿using RadialReview.Accessors;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class IssuesController : BaseController {

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Pad(long id, bool showControls = true, bool readOnly = false) {
			try {
				var issue = IssuesAccessor.GetIssue(GetUser(), id);
				var padId = issue.PadId;
				if (readOnly || !PermissionsAccessor.IsPermitted(GetUser(), x => x.EditIssue(id))) {
					padId = await PadAccessor.GetReadonlyPad(issue.PadId);
				}
				return Redirect(Config.NotesUrl("p/" + padId + "?showControls=" + (showControls ? "true" : "false") + "&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(GetUser().GetName())));
			} catch (Exception) {
				return RedirectToAction("Index", "Error");
			}
		}

		#region From Todo
		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> IssueFromTodo(long recurrence, long todo, long meeting) {
			//var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);
			//copyto = copyto ?? i.Recurrence.Id;
			PermissionsAccessor.Permitted(GetUser(), x =>
				x.ViewL10Meeting(meeting)
				 .ViewL10Recurrence(recurrence)
			);

			var todoModel = TodoAccessor.GetTodo(GetUser(), todo);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, LoadMeeting.True());
			var possible = recur._DefaultAttendees
				.Select(x => x.User)
				.Select(x => new IssueVM.AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList();

            var ownerId = todoModel.AccountableUser != null ? todoModel.AccountableUser.Id : GetUser().Id;
            if (!possible.Any(x => x.id == ownerId))
                ownerId = GetUser().Id;

            var model = new IssueVM() {
				//IssueId = i.Issue.Id,
				RecurrenceId = recurrence,
				Message = await todoModel.NotNull(async x => await x.GetIssueMessage()),
				Details = await todoModel.NotNull(async x => await x.GetIssueDetails()),
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				ForId = todo,
				PossibleUsers = possible,
				OwnerId = ownerId,
			};
			return PartialView("CreateIssueModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> IssueFromTodo(IssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId, x => x.ForId);
			PermissionsAccessor.Permitted(GetUser(), x =>
				x.ViewL10Meeting(model.MeetingId)
				 .ViewL10Recurrence(model.RecurrenceId)
			);

			var creation = IssueCreation.CreateL10Issue(model.Message, model.Details, model.OwnerId, model.RecurrenceId, model.MeetingId, model.Priority, modelType: "TodoModel", modelId: model.ForId);

			await IssuesAccessor.CreateIssue(GetUser(), creation); /*model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message ?? "",
                Description = model.Details ?? "",
                ForModel = "TodoModel",
                ForModelId = model.ForId,
                Organization = GetUser().Organization,
                _Priority = model.Priority
            });*/
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}
		#endregion


		/// <summary>
		/// Copy an issue
		/// </summary>
		/// <param name="copyto"></param>
		/// <param name="recurrence_issue"></param>
		/// <returns></returns>
		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CopyModal(long recurrence_issue, long? copyto = null) {
			var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);

			copyto = copyto ?? i.Recurrence.Id;
			var details = "";
			try {
				details = await PadAccessor.GetText(i.Issue.PadId);
			} catch (Exception) {
			}

			var model = new CopyIssueVM() {
				IssueId = i.Issue.Id,
				Message = i.Issue.Message,
				Details = details,//i.Issue.Description,
				ParentIssue_RecurrenceId = i.Id,
				CopyIntoRecurrenceId = copyto.Value,
				PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), i.Recurrence.Id)
			};
			return PartialView("CopyIssueModal", model);
		}



		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> GetCopyModal(long recurrence_issue, long? copyto = null) {
			var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);

			copyto = copyto ?? i.Recurrence.Id;
			var details = "";

			var model = new CopyIssueVM() {
				IssueId = i.Issue.Id,
				Message = i.Issue.Message,
				Details = details,//i.Issue.Description,
				ParentIssue_RecurrenceId = i.Id,
				CopyIntoRecurrenceId = copyto.Value,
				PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), i.Recurrence.Id).Where(m => m.Id != i.Recurrence.Id).Select(s => new L10Recurrence() { Id = s.Id, Name = s.Name }).ToList()
			};

			return Json(ResultObject.SilentSuccess(model), JsonRequestBehavior.AllowGet);
		}

		//[Access(AccessLevel.UserOrganization)]
		//public PartialViewResult EditModal(long id) {
		//	var todo = TodoAccessor.GetTodo(GetUser(), id);

		//	ViewBag.CanEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditTodo(id));
		//	return PartialView(todo);
		//}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult EditModal(long id) {

			var issueRecurrence = IssuesAccessor.GetIssue_Recurrence(GetUser(), id);

			ViewBag.CanEdit = PermissionsAccessor.IsPermitted(GetUser(), x => x.EditIssueRecurrence(id));
			return PartialView(new IssueVM() {
				Priority = issueRecurrence.Priority,
				Message = issueRecurrence.Issue.Message,
				OwnerId = issueRecurrence.Owner.Id,
				IssueId = issueRecurrence.Issue.Id,
				IssueRecurrenceId = issueRecurrence.Id,
			});
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> EditModal(IssueVM model) {

			await IssuesAccessor.EditIssue(GetUser(), model.IssueRecurrenceId, message: model.Message, owner: model.OwnerId, priority: model.Priority);

			//var todo = IssuesAccessor.EditIssue(GetUser(), model.IssueRecurrenceId, model.Message, model.OwnerId,model.Priority);
			return Json(ResultObject.SilentSuccess());
		}



		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CopyModal(CopyIssueVM model) {
			//ValidateValues(model, x => x.ParentIssue_RecurrenceId, x => x.IssueId);
			var issue = IssuesAccessor.CopyIssue(GetUser(), model.ParentIssue_RecurrenceId, model.CopyIntoRecurrenceId);
			//model.PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), issue.Recurrence.Id);

			await IssuesAccessor.EditIssue(GetUser(), model.ParentIssue_RecurrenceId, awaitingSolve: true);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UnCopyModal(CopyIssueVM model) {
			var issue = IssuesAccessor.UnCopyIssue(GetUser(), model.ParentIssue_RecurrenceId, model.CopyIntoRecurrenceId);

			await IssuesAccessor.EditIssue(GetUser(), model.ParentIssue_RecurrenceId, awaitingSolve: false);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateIssue(long recurrence, long meeting = -1, string issue = null, long? modelId = null, string modelType = null, bool showUsers = true) {
			if (meeting != -1) {
				PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			}

			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, LoadMeeting.True());
			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var possible = people.Select(x => new IssueVM.AccountableUserVM() {
				id = x.Id,
				imageUrl = x.ImageUrl(true, ImageSize._32),
				name = x.GetName()
			}).ToList();


			var prior = recur.Prioritization;
			var showPriority = false;
			if (/*prior == Models.L10.PrioritizationType.Invalid ||*/ prior == Models.L10.PrioritizationType.Priority) {
				showPriority = true;
			}

			var model = new IssueVM() {
				Message = issue,
				ByUserId = GetUser().Id,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = possible,
				OwnerId = GetUser().Id,
				ForModelId = modelId,
				ForModelType = modelType,
				ShowPriority = showPriority,
				HideUsers = !showUsers
			};
			return PartialView("CreateIssueModal", model);
		}

		public class MeetingVm {
			public long id { get; set; }

			public string name { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateIssueRecurrence(long? recurrenceId = null, long? meetingId = null) {			
			ViewBag.PossibleMeetings = L10Accessor.GetVisibleL10RecurrencesVM(GetUser(), GetUser().Id, false)
				.Where(x => x.IsAttendee == true)
				.OrderBy(x => x.Recurrence.StarDate ?? DateTime.MaxValue)
				.Select(x => new MeetingVm {
					name = x.Recurrence.Name,
					id = x.Recurrence.Id
				}).ToList();

			var model = new IssueVM() {
				ByUserId = GetUser().Id,
				MeetingId = meetingId ?? -1,
				PossibleUsers = null,
				OwnerId = GetUser().Id,
				RecurrenceId = recurrenceId ?? 0,
			};
			return PartialView("CreateIssueRecurrenceModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateIssueRecurrence(IssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.OwnerId, x => x.ForId);
			var creation = IssueCreation.CreateL10Issue(model.Message ?? "", model.Details ?? "", model.OwnerId, model.RecurrenceId, model.MeetingId, model.Priority);
			await IssuesAccessor.CreateIssue(GetUser(), creation);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateIssue(IssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId, x => x.ForId);
			if (model.MeetingId != -1) {
				PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));
			}

			var creation = IssueCreation.CreateL10Issue(model.Message ?? "", model.Details ?? "", model.OwnerId, model.RecurrenceId, model.MeetingId, model.Priority, model.ForModelType ?? "IssueModel", model.ForModelId ?? -1);
			await IssuesAccessor.CreateIssue(GetUser(), creation);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}


		[Access(AccessLevel.UserOrganization)]
		//[Obsolete("Remove me please", true)]
		//[Untested("Find a work around to this")]
		public async Task<PartialViewResult> Modal(long meeting, long recurrence, long measurable, long score, long? userid = null) {
			PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			ScoreModel s = null;
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, LoadMeeting.True());

			try {
				if (score == 0 && userid.HasValue) {
					var shift = System.TimeSpan.FromDays((recur.CurrentWeekHighlightShift + 1) * 7 - .0001);

					var week = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrence, true, false, false).CreateTime.Add(shift).StartOfWeek(DayOfWeek.Sunday);
					if (measurable > 0) {
						var scores = (await L10Accessor.GetOrGenerateScoresForRecurrence(GetUser(), recurrence)).Where(x => x.MeasurableId == measurable && x.AccountableUserId == userid.Value && x.ForWeek == week);
						s = scores.FirstOrDefault();
					} else {
						var scores = (await L10Accessor.GetOrGenerateScoresForRecurrence(GetUser(), recurrence)).Where(x => x.Id == score && x.AccountableUserId == userid.Value && x.ForWeek == week);
						s = scores.FirstOrDefault();
					}
				} else {
					s = ScorecardAccessor.GetScore(GetUser(), score);
				}
			} catch (Exception e) {
				log.Error("Issues/Modal", e);
			}
			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var possible = people
				.Select(x => new IssueVM.AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList();

			bool useMessage = true;

			if (s != null && score == 0 && userid.HasValue) {
				s.AccountableUser = people.FirstOrDefault(x => x.Id == s.AccountableUserId);
				s.Measurable.AccountableUser = s.AccountableUser;
				s.Measurable.AdminUser = s.AccountableUser;
			}

			string message = null;
			try {
				if (s != null && useMessage) {
					message = await s.GetIssueMessage();
				}
			} catch (Exception) { }

			string details = null;
			try {
				if (s != null && useMessage) {
					details = await s.GetIssueDetails();
				}
			} catch (Exception) { }
            var ownerId = s.AccountableUser != null ? s.AccountableUser.Id : GetUser().Id;
            if (!people.Any(x => x.Id == ownerId))
                ownerId = GetUser().Id;
            var model = new ScoreCardIssueVM() {
				ByUserId = GetUser().Id,
				Message = message,
				Details = details,
				MeasurableId = measurable,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = possible,
				OwnerId = ownerId,
			};
			return PartialView("ScorecardIssueModal", model);
		}



		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(ScoreCardIssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.MeasurableId, x => x.RecurrenceId);
			PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));
			var creation = IssueCreation.CreateL10Issue(model.Message, model.Details, model.OwnerId, model.RecurrenceId, model.MeetingId, model.Priority, "MeasurableModel", model.MeasurableId);
			await IssuesAccessor.CreateIssue(GetUser(), creation);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CreateRockIssue(long meeting, long rock) {
			PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = RockAccessor.GetRockInMeeting(GetUser(), rock, meeting);
			var recur = L10Accessor.GetCurrentL10RecurrenceFromMeeting(GetUser(), meeting);

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var possible = people.Select(x => new IssueVM.AccountableUserVM() {
				id = x.Id,
				imageUrl = x.ImageUrl(true, ImageSize._32),
				name = x.GetName()
			}).ToList();
            var ownerId = s.ForRock.AccountableUser != null ? s.ForRock.AccountableUser.Id : GetUser().Id;
            if (!people.Any(x => x.Id == ownerId))
                ownerId = GetUser().Id;
            var model = new RockIssueVM() {
				ByUserId = GetUser().Id,
				Message = await s.NotNull(async x => await x.GetIssueMessage()),
				Details = await s.NotNull(async x => await x.GetIssueDetails()),
				MeetingId = meeting,
				RockId = rock,
				RecurrenceId = s.ForRecurrence.Id,
				PossibleUsers = possible,
				OwnerId = ownerId,
			};
			return PartialView("RockIssueModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateRockIssue(RockIssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RockId, x => x.RecurrenceId);
			PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));
			var creation = IssueCreation.CreateL10Issue(model.Message, model.Details, model.OwnerId, model.RecurrenceId, model.MeetingId, model.Priority, "RockModel", model.RockId);
			await IssuesAccessor.CreateIssue(GetUser(), creation);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}


		[Access(AccessLevel.UserOrganization)]

		public async Task<PartialViewResult> CreateHeadlineIssue(long meeting, long headline, long? recurrence = null) {

			//Sometimes this method is called with -1 for meetingId,
			if (meeting == -1 && recurrence != null) {
				meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrence.Value, true).NotNull(x => x.Id);
			}

			PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			var s = HeadlineAccessor.GetHeadline(GetUser(), headline);
			var recur = L10Accessor.GetCurrentL10RecurrenceFromMeeting(GetUser(), meeting);

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var possible = people.Select(x => new IssueVM.AccountableUserVM() {
				id = x.Id,
				imageUrl = x.ImageUrl(true, ImageSize._32),
				name = x.GetName()
			}).ToList();

			//get Notes
			s._Notes = await PadAccessor.GetText(s.HeadlinePadId);
            var ownerId = s.Owner != null ? s.Owner.Id : GetUser().Id;
            if (!people.Any(x => x.Id == ownerId))
                ownerId = GetUser().Id;
            var model = new HeadlineIssueVM() {
				ByUserId = GetUser().Id,
				Message = await s.NotNull(async x => await x.GetIssueMessage()),
				Details = await s.NotNull(async x => await x.GetIssueDetails()),
				MeetingId = meeting,
				HeadlineId = headline,
				RecurrenceId = s.RecurrenceId,
				PossibleUsers = possible,
				OwnerId = ownerId
            };
			return PartialView("HeadlineIssueModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateHeadlineIssue(HeadlineIssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.HeadlineId, x => x.RecurrenceId);
			PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));
			var creation = IssueCreation.CreateL10Issue(model.Message, model.Details, model.OwnerId, model.RecurrenceId, model.MeetingId, model.Priority, "PeopleHeadline", model.HeadlineId);
			await IssuesAccessor.CreateIssue(GetUser(), creation);
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}


		[Access(AccessLevel.UserOrganization)]
		public FileContentResult Listing() {
			var csv = IssuesAccessor.Listing(GetUser(), GetUser().Organization.Id);
			return File(csv.ToBytes(), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> IssuesAndTodos(bool details = false) {
			var file = await IssuesAccessor.GetIssuesAndTodosSpreadsheetAtOrganization(GetUser(), GetUser().Organization.Id, details);
			return Xls(file, "Issues_Todos_" + DateTime.UtcNow.ToJavascriptMilliseconds());
		}
	}
}

#region Deleted

#endregion
