using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FluentNHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Models.VTO;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.VTO;
using RadialReview.Exceptions;

namespace RadialReview.Controllers {
	public partial class
		VTOController : BaseController {
		public class VTOListingVM {
			public List<VtoModel> VTOs { get; set; }
		}

		public class VTOViewModel {
			public long Id { get; set; }

			public bool IsPartial { get; set; }

			public bool OnlyCompanyWideRocks { get; set; }
			public long? VisionId { get; set; }
		}


		// GET: VTO
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			var vtos = VtoAccessor.GetAllVTOForOrganization(GetUser(), GetUser().Organization.Id);
			var model = new VTOListingVM() {
				VTOs = vtos
			};
			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Edit(long id = 0, bool noheading = false, bool? vision = null, bool? traction = null, bool? includeCompanyVision = null) {
			if (id == 0) {
				var model2 = VtoAccessor.CreateVTO(GetUser(), GetUser().Organization.Id);
				model2.Name = "<no name>";
				return RedirectToAction("Edit", new { id = model2.Id, noheading = noheading, vision = vision, traction = traction });
			}

			AngularVTO model;
			var visionAllowed = PermissionsAccessor.IsPermitted(GetUser(), x => x.ViewVTOVision(id));
			var tractionAllowed = PermissionsAccessor.IsPermitted(GetUser(), x => x.ViewVTOTraction(id));
			var issuesAllowed = PermissionsAccessor.IsPermitted(GetUser(), x => x.ViewVTOTractionIssues(id));


			var getVision = ((vision ?? true) && visionAllowed);
			var getTraction = ((traction ?? true) && tractionAllowed);

			model = VtoAccessor.GetVTO(GetUser(), id, getVision, getTraction, getTraction);

			if (GetUser().Organization.Settings.HasImage()) {
				ViewBag.CompanyImageUrl = GetUser().Organization.Settings.GetImageUrl(ImageSize._img);
			}

			var defaultShowVision = false;
			var defaultShowTraction = true;
			var defaultCompanyVision = true;
			//var onlyCompanyWideRocks = false;

			if (model.L10Recurrence != null) {

				try {
					var isLeadership = L10Accessor.GetL10Recurrence(GetUser(), model.L10Recurrence.Value, LoadMeeting.False()).TeamType == L10TeamType.LeadershipTeam;
					defaultShowVision = isLeadership;
				} catch (PermissionsException) {
					//no access to L10
					defaultShowVision = true;
				}
				//onlyCompanyWideRocks = onlyCompanyWideRocks || isLeadership;
			} else {
				defaultShowVision = true;
			}

			ViewBag.HideVision = !(getVision && defaultShowVision);
			ViewBag.HideTraction = !(getTraction && defaultShowTraction);

			var lookupCompanyVision = (includeCompanyVision ?? defaultCompanyVision);
			
			AngularVTO visionPage = null;

			if (lookupCompanyVision) {
				visionPage = VtoAccessor.GetSharedVTO(GetUser(), model._OrganizationId);
				if (visionPage != null) {
					ViewBag.HideVision = false;
				}

			}

			ViewBag.CanEditCoreValues = PermissionsAccessor.IsPermitted(GetUser(), x => x.EditCompanyValues(model._OrganizationId));

			var editVision = false;
			var editTraction = false;

			if (PermissionsAccessor.IsPermitted(GetUser(), x => x.EditVTO(model.Id))) {
				editTraction = true;
				if (visionPage == null) {
					editVision = true;
				} else {
					editVision = model.Id == visionPage.Id;// _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditVTO(visionPage.Id));
				}
			}

			ViewBag.CanEditVTOTraction = editTraction;
			ViewBag.CanEditVTOVision = editVision;

			var vm = new VTOViewModel() {
				Id = model.Id,
				VisionId = visionPage.NotNull(x => (long?)x.Id),
				IsPartial = noheading
			};
			if (noheading)
				return PartialView(vm);
			return View(vm);
		}

		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0)]
		public JsonResult Data(long id) {
            //model.ThreeYearPicture.FutureDate = DateTime.SpecifyKind(model.ThreeYearPicture.FutureDate.Value.AddMinutes(offset), DateTimeKind.Unspecified);                
            var model = VtoAccessor.GetAngularVTO(GetUser(), id);
            var offset = GetUser().GetTimezoneOffset();
            //if (model.ThreeYearPicture.FutureDate.HasValue)
            //    model.ThreeYearPicture.FutureDate = model.ThreeYearPicture.FutureDate.Value.AddMinutes(offset).Date;
            //if (model.OneYearPlan.FutureDate.HasValue)
            //    model.OneYearPlan.FutureDate = model.OneYearPlan.FutureDate.Value.AddMinutes(offset).Date;
            //if (model.QuarterlyRocks.FutureDate.HasValue)
            //    model.QuarterlyRocks.FutureDate = model.QuarterlyRocks.FutureDate.Value.AddMinutes(offset).Date;

            if (model.ThreeYearPicture!=null && model.ThreeYearPicture.FutureDate.HasValue && model.ThreeYearPicture.FutureDate.Value != model.ThreeYearPicture.FutureDate.Value.Date)
                model.ThreeYearPicture.FutureDate = model.ThreeYearPicture.FutureDate.Value.AddMinutes(offset).Date;
            if (model.OneYearPlan != null && model.OneYearPlan.FutureDate.HasValue && model.OneYearPlan.FutureDate.Value != model.OneYearPlan.FutureDate.Value.Date)
                model.OneYearPlan.FutureDate = model.OneYearPlan.FutureDate.Value.AddMinutes(offset).Date;
            if (model.QuarterlyRocks!=null && model.QuarterlyRocks.FutureDate.HasValue && model.QuarterlyRocks.FutureDate.Value != model.QuarterlyRocks.FutureDate.Value.Date)
                model.QuarterlyRocks.FutureDate = model.QuarterlyRocks.FutureDate.Value.AddMinutes(offset).Date;


            return Json(model, JsonRequestBehavior.AllowGet);
		}

	}
}