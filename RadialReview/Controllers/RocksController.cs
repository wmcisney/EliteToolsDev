using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Rocks;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using RadialReview.Models.Angular.Rocks;
using static RadialReview.Utilities.SelectExistingOrCreateUtility;
using RadialReview.Exceptions;
using RadialReview.Utilities.PermissionsListers;
using RadialReview.Models.Rocks;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers {
	public class RocksController : BaseController {
		public class RockVM {
			public long TemplateId { get; set; }
			public long UserId { get; set; }
			public List<RockModel> Rocks { get; set; }
			public List<Models.UserTemplate.UserTemplate.UT_Rock> TemplateRocks { get; set; }
			public DateTime CurrentTime { get; set; }
			public bool Locked { get; set; }
			public bool UpdateOutstandingReviews { get; set; }
			public bool UpdateAllL10s { get; set; }

			public RockVM() {
				CurrentTime = DateTime.UtcNow;
				UpdateAllL10s = false;//true;
			}
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Search(string q, int results = 4, string exclude = null) {
			long[] excludeLong = new long[] { };
			if (exclude != null) {
				try {
					excludeLong = exclude.Split(',').Select(x => x.ToLong()).ToArray();
				} catch (Exception) { }
			}

			var oo = await RockAccessor.Search(GetUser(), GetUser().Organization.Id, q, excludeLong);
			//var oo = _SearchUsers(q, results, exclude);
			var o = oo.Select(x => {
				var desc = "Owner: " + x.AccountableUser.GetName();
				return new BaseSelectExistingOrCreateItem {
					ItemValue = "" + x.Id,
					Name = x.Rock,
					ImageUrl = x.AccountableUser.GetImageUrl(),
					Description = desc
				};
			}).ToList();
			return Json(ResultObject.SilentSuccess(o), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Pad(long id, bool showControls = true, bool readOnly = false) {
			try {
				var rock = RockAccessor.GetRock(GetUser(), id);
				var padId = rock.PadId;
				if (readOnly || !PermissionsAccessor.IsPermitted(GetUser(), x => x.EditRock(id, false))) {
					padId = await PadAccessor.GetReadonlyPad(rock.PadId);
				}
				return Redirect(Config.NotesUrl("p/" + padId + "?showControls=" + (showControls ? "true" : "false") + "&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(GetUser().GetName())));
			} catch (Exception) {
				return RedirectToAction("Index", "Error");
			}
		}

		//public class RockEditVM {

		//	public long Id { get; set; }
		//	public string Name { get; set; }
		//	public long AccountableUserId { get; set; }
		//	public long[] RecurrenceIds { get; set; }
		//	public string Notes { get; set; }
		//	public DateTime DueDate { get; set; }
		//	public List<MilestoneVM> Milestones { get; set; }

		//	public List<SelectListItem> PossibleRecurrences { get; set; }
		//	public List<SelectListItem> PossibleOwners { get; set; }
		//	public RockState Completion { get; set; }
		//	public bool AddToVTO { get; set; }
		//	//public long SelectedRecurrenceId { get; set; }


		//	public class MilestoneVM {
		//		public string Name { get; set; }
		//		public DateTime DueDate { get; set; }
		//		public bool Complete { get; set; }
		//		public long Id { get; set; }
		//		public bool Deleted { get; set; }
		//	}
		//}


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> Create(long? userId = null, long? recurrenceId = null) {

			var vm = await RockAccessor.BuildRockVM(GetUser(), null, recurrenceId: recurrenceId, defaultUserId: userId);

			if (!vm.CanEdit || !vm.CanCreate) {
				throw new PermissionsException("You cannot create rocks.");
			}
			return PartialView("Edit", vm);

			//////
			//var rocksStates = new List<RockStatesVm>();
			//rocksStates.Add(new RockStatesVm { id = "AtRisk", name = "Off Track" });
			//rocksStates.Add(new RockStatesVm { id = "OnTrack", name = "On Track" });
			//rocksStates.Add(new RockStatesVm { id = "Complete", name = "Done" });
			//ViewBag.RockStates = rocksStates;
			//userId = userId ?? GetUser().Id;

			//var meetings = SelectListAccessor.GetL10RecurrenceAdminable(GetUser(), GetUser().Id, x => x.Id == recurrenceId);

			//var rockTypes = new List<RockTypesVm>();
			//rockTypes.Add(new RockTypesVm { id = "False", name = "Individual" });
			//rockTypes.Add(new RockTypesVm { id = "True", name = "Departmental/Company" });
			//ViewBag.RockTypes = rockTypes;


			//var editableUsers = SelectListAccessor.GetUsersWeCanCreateRocksFor(GetUser(), GetUser().Id, x => x.Id == userId);

			//var qtr = await QuarterlyAccessor.GetQuarterDoNotGenerate(GetUser(), GetUser().Organization.Id);
			//var endDate = DateTime.UtcNow.AddDays(90);
			//if (qtr != null) {
			//	endDate = qtr.EndDate ?? endDate;
			//}
			//if (!editableUsers.Any(x => !x.Disabled))
			//	throw new PermissionsException("You cannot create rocks.");

			//var recurs = new List<long>();
			//if (recurrenceId != null)
			//	recurs.Add(recurrenceId.Value);

			//ViewBag.CanEdit = true;
			//ViewBag.IsCreate = true;

			//return PartialView("Edit", new RockEditVM() {
			//	AccountableUserId = userId.Value,
			//	PossibleRecurrences = meetings,
			//	PossibleOwners = editableUsers,
			//	RecurrenceIds = recurs.ToArray(),
			//	DueDate = endDate,
			//	Milestones = new List<RockEditVM.MilestoneVM>(),
			//	Completion = RockState.OnTrack,
			//});
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> Edit(long id, /*long recurrenceid,*/ bool hideMeetings = false) {
			var vm = await RockAccessor.BuildRockVM(GetUser(), id);
			vm.HideMeetings = hideMeetings;
			return PartialView(vm);

			//			var rocksStates = new List<RockStatesVm>();
			//			rocksStates.Add(new RockStatesVm { id = "AtRisk", name = "Off Track" });
			//			rocksStates.Add(new RockStatesVm { id = "OnTrack", name = "On Track" });
			//			rocksStates.Add(new RockStatesVm { id = "Complete", name = "Done" });
			//			ViewBag.RockStates = rocksStates;

			//			var rock = RockAccessor.GetRock(GetUser(), id);
			//			var editableUsers = SelectListAccessor.GetUsersWeCanCreateRocksFor(GetUser(), GetUser().Id, x => x.Id == rock.ForUserId);

			//			var selected = RockAccessor.GetRecurrencesContainingRock(GetUser(), id);


			//			//PROBABLY WRONG
			//			var vtoRock = false;
			//			/*if (recurrenceid > 0) {
			//				vtoRock = selected.FirstOrDefault(x => x.RecurrenceId == recurrenceid).VtoRock;
			//			} else {
			//				vtoRock = selected.Any(x => x.VtoRock == true);
			//			}*/

			//			var meetings = SelectListAccessor.GetL10RecurrenceAdminable(GetUser(), GetUser().Id, x => selected.Any(y=>y.RecurrenceId==x.Id));
			//			//var milestones = RockAccessor.GetMilestonesForRock(GetUser(), id);
			//			var rockAndMs = RockAccessor.GetRockAndMilestones(GetUser(), id);

			//			ViewBag.CanEdit = PermissionsAccessor.IsPermitted(GetUser(), x => x.EditRock(id));
			//			ViewBag.AnyL10sWithMilestones = rockAndMs.AnyMilestoneMeetings || selected.Any(x=>x.MilestonesEnabled);
			//			ViewBag.HideMeetings = hideMeetings;

			//			var rockTypes = new List<RockTypesVm>();
			//			rockTypes.Add(new RockTypesVm { id = "False", name = "Individual" });
			//			//rockTypes.Add(new RockTypesVm { id = "True", name = "Departmental (Added to your team's V/TO)" });
			//			if (selected.Any(x => x.TeamType == Models.L10.L10TeamType.LeadershipTeam)){
			//				rockTypes.Add(new RockTypesVm { id = "True", name = "Company (Added to your team's V/TO)" });
			//			} else {
			//				rockTypes.Add(new RockTypesVm { id = "True", name = "Departmental (Added to your team's V/TO)" });
			//			}
			//			ViewBag.RockTypes = rockTypes;

			//			var dueDate = rock.DueDate ?? await QuarterlyAccessor.GetQuarterEndDate(GetUser(), GetUser().Organization.Id);

			//			return PartialView(new RockEditVM() {
			//				Id = id,
			//				PossibleRecurrences = meetings,
			//				PossibleOwners = editableUsers,
			//				AccountableUserId = rock.ForUserId,
			//				Name = rock.Name,
			//				RecurrenceIds = selected.Select(x=>x.RecurrenceId).ToArray(),
			//				DueDate = dueDate,
			//				Completion = rock.Completion,
			////				SelectedRecurrenceId = recurrenceid,
			//				Milestones = rockAndMs.Milestones.Select(x => new RockEditVM.MilestoneVM() {
			//					Complete = x.Status ==Models.Rocks.MilestoneStatus.Done,
			//					DueDate = x.DueDate,
			//					Name = x.Name,
			//					Id = x.Id,
			//				}).ToList(),
			//				AddToVTO = vtoRock,
			//			});
		}


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Edit(EditRockViewModel model) {
			model.RecurrenceIds = model.RecurrenceIds ?? new long[0];
			var modelMilestones = (model.Milestones ?? new List<EditRockViewModel.MilestoneVM>()).Where(x => !x.Deleted).ToList();
			var errors = new List<PermissionsException>();
			var dueDate = model.DueDate;// GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate);
			if (model.Id == 0 || model.Id == null) {

				bool toBeAddedToL10 = model.RecurrenceIds.Length > 0;
				var rock = await RockAccessor.CreateRock(GetUser(), model.AccountableUser, model.Title, notes: model.Notes, dueDate: dueDate, toBeAddedToL10: toBeAddedToL10);
				foreach (var recurId in model.RecurrenceIds) {
					try {
						await L10Accessor.AttachRock(GetUser(), recurId, rock.Id, model.AddToVTO ?? false, AttachRockType.Create);
					} catch (PermissionsException e) {
						errors.Add(e);
					}
				}
				model.Id = rock.Id;

				foreach (var m in modelMilestones ?? new List<EditRockViewModel.MilestoneVM>()) {
					await RockAccessor.AddMilestone(GetUser(), rock.Id, m.Name, m.DueDate, m.Complete);
				}

				if (errors.Any()) {
					return Json(ResultObject.CreateError((errors.Count == 1 ? errors.First().Message : null) ?? "An error occurred", model));
				}

				return Json(ResultObject.Create(model, "Rock created."));

			} else {

				var rockAndMs = RockAccessor.GetRockAndMilestones(GetUser(), model.Id.Value);
				var rock = rockAndMs.Rock;

				await RockAccessor.UpdateRock(GetUser(), model.Id.Value, model.Title, model.AccountableUser, model.Completion, dueDate: dueDate);
				//if (model.SelectedRecurrenceId > 0) {
				//	var rockRecurrenceId = L10Accessor.GetRocksForRecurrence(GetUser(), model.SelectedRecurrenceId).FirstOrDefault(x => x.ForRock.Id == model.Id).Id;
				//	await L10Accessor.SetVtoRock(GetUser(), rockRecurrenceId, model.AddToVTO);
				//}
				var oldSelected = RockAccessor.GetRecurrencesContainingRock(GetUser(), model.Id.Value).Select(x => x.RecurrenceId);



				var addRemove = SetUtility.AddRemove(oldSelected, model.RecurrenceIds);
				var addRemoveMilestone = SetUtility.AddRemove(rockAndMs.Milestones.Select(x => x.Id), (modelMilestones ?? new List<EditRockViewModel.MilestoneVM>()).Select(x => x.Id));

				foreach (var addRecurId in addRemove.AddedValues) {
					try {
						await L10Accessor.AttachRock(GetUser(), addRecurId, rock.Id, model.AddToVTO ?? false, AttachRockType.Existing);
					} catch (PermissionsException e) {
						errors.Add(e);
					}
				}
				foreach (var removeRecurId in addRemove.RemovedValues) {
					try {
						await L10Accessor.RemoveRock(GetUser(), removeRecurId, rock.Id, false);
					} catch (PermissionsException e) {
						errors.Add(e);
					}
				}

				if (model.AddToVTO != null) {
					foreach (var remainRecurId in addRemove.RemainingValues) {
						await L10Accessor.SetVtoRock(GetUser(), remainRecurId, rock.Id, model.AddToVTO.Value);
					}
				}

				foreach (var mid in addRemoveMilestone.AddedValues.Distinct()) {
					foreach (var m in modelMilestones.Where(x => x.Id == mid)) {
						await RockAccessor.AddMilestone(GetUser(), rock.Id, m.Name, m.DueDate, m.Complete);
					}
				}

				foreach (var mid in addRemoveMilestone.RemainingValues) {
					foreach (var m in modelMilestones.Where(x => x.Id == mid)) {
						await RockAccessor.EditMilestone(GetUser(), m.Id, m.Name, m.DueDate, status: m.Complete ? MilestoneStatus.Done : MilestoneStatus.NotDone);
					}
				}

				foreach (var m in addRemoveMilestone.RemovedValues) {
					await RockAccessor.DeleteMilestone(GetUser(), m);
				}
				if (errors.Any()) {
					return Json(ResultObject.CreateError((errors.Count == 1 ? errors.First().Message : null) ?? "An error occurred", model));
				}

				return Json(ResultObject.Create(model, "Rock updated."));
			}


		}





		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> SetDueDate(long rockId, long dueDate) {
			var dateR = dueDate.ToDateTime();
			await L10Accessor.UpdateRock(GetUser(), rockId, null, null, null, null, dueDate: dateR);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Archive(long id) {
			var userId = id;
			var archivedRocks = RockAccessor.GetArchivedRocks(GetUser(), userId);
			return View(archivedRocks);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult ModalSingle(long id, long userId, long? periodId) {
			PermissionsAccessor.Permitted(GetUser(), x => x.EditQuestionForUser(userId));
			RockModel rock;
			if (id == 0) {
				rock = new RockModel() {
					CreateTime = DateTime.UtcNow,
					OnlyAsk = AboutType.Self,
				};
			} else {
				rock = RockAccessor.GetRock(GetUser(), id);
			}

			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			//ViewBag.Periods = PeriodAccessor.GetPeriod(GetUser(), periodId.Value).AsList().ToSelectList(x => x.Name, x => x.Id);//PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);

			return PartialView(new RocksController.RockVM { Rocks = rock.AsList(), UserId = userId });
		}

		//[Access(AccessLevel.UserOrganization)]
		//public JsonResult Delete(long id) {
		//	RockAccessor.DeleteRock(GetUser(), id);
		//	return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		//}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Undelete(long id) {
			await RockAccessor.UndeleteRock(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult BlankEditorRow(bool includeUsers = false, bool unusedCompanyRock = false, long? periodId = null, bool hideperiod = false, bool showCompany = false, bool excludeDelete = false, long? recurrenceId = null) {
			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			if (includeUsers) {
				if (recurrenceId != null) {
					ViewBag.PossibleUsers = L10Accessor.GetAttendees(GetUser(), recurrenceId.Value);
				} else {
					ViewBag.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
				}
			}

			ViewBag.HidePeriod = true;// hideperiod;
			ViewBag.ShowCompany = showCompany;
			ViewBag.HideDelete = excludeDelete;
			ViewBag.HideArchive = excludeDelete;

			return PartialView("_RockRow", new RockModel() {
				CreateTime = DateTime.UtcNow,
				//CompanyRock = companyRock,
				OnlyAsk = AboutType.Self,
				PeriodId = periodId
			});
		}

		//[Access(AccessLevel.Manager)]
		//[Untested("Vto_Rocks","Remove from UI")]
		//[Obsolete("Do not use",true)]
		//public PartialViewResult CompanyRockModal(long id) {
		//	var orgId = id;
		//	var rocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();

		//	//var rocks = RockAccessor.GetAllRocksAtOrganization(GetUser(), orgId, true);
		//	var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
		//	ViewBag.Periods = periods;
		//	ViewBag.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
		//	return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		//}

		//[HttpPost]
		//[Access(AccessLevel.Manager)]
		//[Untested("Vto_Rocks", "Remove from UI")]
		//[Obsolete("Do not use", true)]
		//public JsonResult CompanyRockModal(RocksController.RockVM model) {
		//	//var rocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
		//	var oid = GetUser().Organization.Id;
		//	model.Rocks.ForEach(x => x.OrganizationId = oid);
		//	RockAccessor.EditCompanyRocks(GetUser(), GetUser().Organization.Id, model.Rocks);
		//	return Json(ResultObject.Create(model.Rocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.Success));
		//}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id) {
			PermissionsAccessor.Permitted(GetUser(), x => x.EditQuestionForUser(id));
			var userId = id;
			var rocks = RockAccessor.GetAllRocks(GetUser(), userId);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).OrderByDescending(x => x.EndTime).ToSelectList(x => x.Name, x => x.Id);
			ViewBag.Periods = periods;
			return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(RocksController.RockVM model) {
			foreach (var r in model.Rocks) {
				r.ForUserId = model.UserId;
			}
			await RockAccessor.EditRocks(GetUser(), model.UserId, model.Rocks, model.UpdateOutstandingReviews, model.UpdateAllL10s);
			return Json(ResultObject.Create(model.Rocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.Success));
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult Listing() {
			var csv = RockAccessor.Listing(GetUser(), GetUser().Organization.Id);
			return File(csv.ToBytes(false), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> All() {
			var rocksAndMilestones = (await RockAccessor.AllVisibleRocksAndMilestonesAtOrganization(GetUser(), GetUser().Organization.Id))
								.Select(x => new AngularRockAndMilestones(x.Rock, x.Milestones))
								.ToList();
			return View(rocksAndMilestones);
		}



		public class RockTable {
			public List<RockModel> Rocks { get; set; }
			public List<long> Editables { get; set; }
			public bool Editable { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Table(long id, bool editor = false, bool current = true) {
			var forUserId = id;
			var rocks = RockAccessor.GetAllRocks(GetUser(), forUserId);
			var editables = new List<long>();

			if (current) {
				rocks = rocks.Where(x => x.CompleteTime == null).ToList();
			}

			if (editor && PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagesUserOrganization(forUserId, false))) {
				editables = rocks.Select(x => x.Id).ToList();
			}

			var model = new RockTable() {
				Editables = editables,
				Rocks = rocks,
				Editable = editor,
			};

			return PartialView(model);


		}

		public class RockAndMilestonesVM {
			public List<AngularMilestone> Milestones { get; set; }
			public AngularRock Rock { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		[Untested("Vto_Rocks", "Make sure that Company rock is uneditable.")]
		public PartialViewResult EditModal(long id) {
			var rockAndMs = RockAccessor.GetRockAndMilestones(GetUser(), id);

			var model = new RockAndMilestonesVM() {
				Rock = new AngularRock(rockAndMs.Rock, false),
				Milestones = rockAndMs.Milestones.Select(x => new AngularMilestone(x)).ToList()
			};

			ViewBag.CanEdit = PermissionsAccessor.IsPermitted(GetUser(), x => x.EditRock(id, false));
			ViewBag.AnyL10sWithMilestones = rockAndMs.AnyMilestoneMeetings;

			return PartialView(model);

		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> EditModal(RocksController.RockAndMilestonesVM model) {
			var rock = model.Rock;
			await L10Accessor.Update(GetUser(), rock, null);
			return Json(ResultObject.SilentSuccess());
		}


		//public ActionResult Assessment()
		#region Template
		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id) {
			var templateId = id;
			var template = UserTemplateAccessor.GetUserTemplate(GetUser(), templateId, loadRocks: true);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			ViewBag.Periods = periods;
			return PartialView(new RocksController.RockVM { TemplateRocks = template._Rocks, TemplateId = templateId });
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public async Task<JsonResult> TemplateModal(RocksController.RockVM model) {
			foreach (var r in model.TemplateRocks) {
				if (r.Id == 0) {
					if (r.DeleteTime == null) {
						await UserTemplateAccessor.AddRockToTemplate(GetUser(), model.TemplateId, r.Rock, r.PeriodId);
					}
				} else {
					UserTemplateAccessor.UpdateRockTemplate(GetUser(), r.Id, r.Rock, r.PeriodId, r.DeleteTime);
				}
			}

			return Json(ResultObject.SilentSuccess()); //ResultObject.Create(model.TemplateRocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.SilentSuccess));
		}
		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankTemplateEditorRow(long id) {
			var templateId = id;
			PermissionsAccessor.Permitted(GetUser(), x => x.ViewTemplate(templateId));
			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			return PartialView("_TemplateRockRow", new UserTemplate.UT_Rock() {
				TemplateId = templateId
			});
		}
		#endregion
	}
}
