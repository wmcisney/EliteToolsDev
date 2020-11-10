using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Json;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class ScorecardController : BaseController {

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult Listing() {
			var csv = _ScorecardAccessor.Listing(GetUser(), GetUser().Organization.Id, DateRange.Full());
			return File(csv.ToBytes(), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> SetFormula(long id, string formula) {
			if (formula == "-notset-") {
				throw new PermissionsException("Formula was empty");
			}

			await ScorecardAccessor.SetFormula(GetUser(), id, formula);
			return Json(ResultObject.SilentSuccess());
		}
		public class FormulaParts {
			public MeasurableModel ForMeasurable { get; set; }
			public FormulaParts(List<object> tokens) {
				this.Values = tokens;
			}

			public List<MeasurableModel> VisibleMeasurables { get; set; }

			public List<dynamic> Values { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> FormulaPartial(long? id = null) {
			MeasurableModel m = null;
			string formula = "";
			if (id != null) {
				m = ScorecardAccessor.GetMeasurable(GetUser(), id.Value);
				formula = m.Formula;
			}

			var visible = ScorecardAccessor.GetVisibleMeasurables(GetUser(), GetUser().Organization.Id, false);

			var parsed = FormulaUtility.Parse(formula);
			var variables = parsed.GetVariables().Select(x => long.Parse(x.Split('(')[0])).ToList();

			var lookup = variables.Distinct().ToDictionary(x => x, x => {
				try {
					return ScorecardAccessor.GetMeasurable(GetUser(), x);
				} catch (Exception) {
					return null;
				}
			});
			var tokens = parsed.Tokenize(x => {
				var split = x.Split('(');
				var mid = long.Parse(split[0]);
				var offset = 0;
				if (split.Length > 1) {
					var args = split[1].Split(',', ')');
					offset = int.Parse(args[0]);
				}
				var lbl = lookup[mid].Title;
				var content = "<span class='offset'>";
				if (offset != 0) {
					if (offset >= 0) {
						content += "(+" + offset + ")";
					} else {
						content += "(" + offset + ")";
					}
				}
				content += "</span>";
				return new {
					label = lbl + content,
					attrs = new {
						id = mid,
						offset = offset,
						//content = content,
					}
				};
			});
			var fp = new FormulaParts(tokens) {
				ForMeasurable = m,
				VisibleMeasurables = visible,
			};

			return PartialView(fp);
		}

		private IEnumerable<TinyUser> SetupMeasurableViewBag(long? measId, bool create, bool showRecurrences, long? selectedRecurId) {
			//Owners
			var possibleOwners = TinyUserAccessor.GetAllPossibleVisibleOwners(GetUser());

			var selected = new List<long>();
			if (measId != null) {
				selected = ScorecardAccessor.GetMeasurablesRecurrenceIds(GetUser(), measId.Value);
			}

			if (selectedRecurId != null) {
				selected.Add(selectedRecurId.Value);
			}

			var meetings = SelectListAccessor.GetL10RecurrenceAdminable(GetUser(), GetUser().Id, x => selected.Contains(x.Id));

			ViewBag.PossibleRecurrences = meetings;
			ViewBag.PossibleOwners = possibleOwners.ToSelectList(x => x.Name, x => x.UserOrgId);

			ViewBag.IsCreate = create;
			ViewBag.ShowRecurrences = showRecurrences;
			ViewBag.CanArchive = !create && meetings.Where(x => x.Selected).All(x => !x.Disabled);
			return possibleOwners;
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CreateMeasurable(long? recurrenceId = null, bool showRecurrences = false, long? userId = null) {
			var model = new AngularMeasurable();
			var possibleOwners = SetupMeasurableViewBag(null, true, showRecurrences, recurrenceId);

			userId = userId ?? GetUser().Id;
			var found = possibleOwners.FirstOrDefault(x => x.UserOrgId == userId);
			if (found != null) {
				model.Owner = AngularUser.CreateUser(found);
			}
			return PartialView("MeasurablePartial", model);
		}




		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Archive(long id) {
			await ScorecardAccessor.DeleteMeasurable(GetUser(), id);
			return Json(ResultObject.Success("Measurable archived."), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> CreateMeasurable(AngularMeasurable model, decimal? lower = null, decimal? upper = null,
			bool? enableCumulative = null, DateTime? cumulativeStart = null,
			bool? enableAverage = null, DateTime? averageStart = null,
			string formula = null, long[] recurrenceIds = null) {
			await ScorecardAccessor.CreateMeasurableHelper(GetUser(), model, lower, upper, enableCumulative, cumulativeStart, enableAverage, averageStart, formula, recurrenceIds);

			return Json(ResultObject.SilentSuccess());
		}



		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> EditMeasurable(long id) {
			if (id <= 0) {
				throw new PermissionsException("This measurable is not editable");
			}

			var model = ScorecardAccessor.GetMeasurable(GetUser(), id);
			SetupMeasurableViewBag(id, false, false, null);
			return PartialView("MeasurablePartial", new AngularMeasurable(model));
		}


		public class ArchiveMeasurablesVM {
			public long MeasurableId { get; set; }
			public string MeasurableName { get; set; }
			public bool ArchiveAll { get; set; }
			public Dictionary<string, string> Selected { get; set; }
			public List<L10Accessor.MeasurableRecurrence> Items { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> ArchiveMeasurable(long id, long? recurrenceId = null) {
			var measurableId = id;
			var measurable = ScorecardAccessor.GetMeasurable(GetUser(), id);
			var items = await L10Accessor.GetMeasurableRecurrences(GetUser(), measurableId);
			var canArchive = items.All(x => x.CanAdmin);
			canArchive = canArchive && PermissionsAccessor.IsPermitted(GetUser(), x => x.EditMeasurable(id));
			ViewBag.CanArchive = canArchive;

			return PartialView(new ArchiveMeasurablesVM() {
				MeasurableId = measurableId,
				MeasurableName = measurable.Title,
				ArchiveAll = false,
				Items = items,
				Selected = items.ToDictionary(x => "" + x.RecurrenceId, x => (x.RecurrenceId == recurrenceId) ? "true" : "false"),
			});
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> ArchiveMeasurable(ArchiveMeasurablesVM model) {
			model.Selected = model.Selected ?? new Dictionary<string, string>();
			var selected = (await L10Accessor.GetMeasurableRecurrences(GetUser(), model.MeasurableId))
				.ToDefaultDictionary(
					x => x.RecurrenceId,
					x => model.Selected.ContainsKey("" + x.RecurrenceId) && model.Selected["" + x.RecurrenceId] == "on",
					x => false
				);

			if (!selected.All(x => x.Value) && model.ArchiveAll) {
				throw new PermissionsException("Cannot archive a measurable while still attached to meeting.");
			}

			var archived = false;
			var removedAll = selected.All(x => x.Value);
			foreach (var b in selected.Where(x => x.Value)) {
				await L10Accessor.DetachMeasurable(GetUser(), b.Key, model.MeasurableId, removedAll && model.ArchiveAll);
			}
			if (selected.All(x => x.Value) && model.ArchiveAll) {
				//await ScorecardAccessor.DeleteMeasurable(GetUser(), model.MeasurableId);
				archived = true;
			}
			return Json(ResultObject.SilentSuccess(new {
				archived,
				removedAll
			}));

		}

	}
}
