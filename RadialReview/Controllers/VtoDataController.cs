using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Json;
using System.Threading.Tasks;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.Base;

namespace RadialReview.Controllers {
	public partial class VTOController : BaseController {
		// GET: VtoData
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularVtoString(AngularVtoString model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularVtoKV(AngularVtoKV model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularVto(AngularVTO model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularVtoRock(AngularVtoRock model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularOneYearPlan(AngularOneYearPlan model, string connectionId = null) {
            model.FutureDate = model.FutureDate.HasValue ? model.FutureDate.Value.Date : (DateTime?)null;
            await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularQuarterlyRocks(AngularQuarterlyRocks model, string connectionId = null) {
            model.FutureDate = model.FutureDate.HasValue ? model.FutureDate.Value.Date : (DateTime?)null;
            await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularThreeYearPicture(AngularThreeYearPicture model, string connectionId = null) {

            model.FutureDate = model.FutureDate.HasValue ? model.FutureDate.Value.Date : (DateTime?)null;
            await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularStrategy(AngularStrategy model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateMarketStrategy(long vto, string connectionId = null) {
			VtoAccessor.CreateMarketingStrategy(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}

        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> RemoveMarketStrategy(long strategyId, string connectionId = null) {
            await VtoAccessor.RemoveMarketingStrategy(GetUser(), strategyId, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
		public JsonResult AddThreeYear(long vto, string connectionId = null) {
			VtoAccessor.AddThreeYear(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddYearGoal(long vto, string connectionId = null) {
			VtoAccessor.AddYearGoal(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddCompanyValue(long vto, string connectionId = null) {
			VtoAccessor.AddCompanyValue(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddRock(long vto, string connectionId = null) {
			await VtoAccessor.CreateNewRock(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> DeleteRock(long value, string connectionId = null) {
			await VtoAccessor.UpdateRock(GetUser(), value, null, null, true, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> DeleteCompanyValue(long value, string connectionId = null) {
            await VtoAccessor.UpdateCompanyValue(GetUser(), value, null, null, true, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularCompanyValue(AngularCompanyValue model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularCoreFocus(AngularCoreFocus model, string connectionId = null) {
			await VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddUniques(long vto, long marketingStrategyId, string connectionId = null) {
			VtoAccessor.AddUniques(GetUser(), vto, marketingStrategyId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddIssue(long vto, string connectionId = null) {
            var vtoModel = VtoAccessor.GetAngularVTO(GetUser(), vto);
            var creation = IssueCreation.CreateL10Issue("", "", GetUser().Id, vtoModel.L10Recurrence.Value); 
            var success = IssuesAccessor.CreateIssue(GetUser(), creation);
            L10Accessor.MoveIssueToVto(GetUser(), success.Result.IssueRecurrenceModel.Id, connectionId);

            //VtoAccessor.AddIssue(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> DeleteString(long value, string connectionId = null) {
			await VtoAccessor.UpdateVtoString(GetUser(), value, null, true, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> DeleteKV(long value, string connectionId = null) {
			await VtoAccessor.UpdateVtoKV(GetUser(), value, null, null, true, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddKV(long value,VtoItemType type, string connectionId = null) {
			Func<VtoModel, BaseAngularList<AngularVtoKV>, IAngularId>  action = null;
			switch (type) {
				case VtoItemType.Header_ThreeYearPicture:
					action = (vto, list) => new AngularThreeYearPicture(vto.ThreeYearPicture.Id) { Headers = list };
					break;
				case VtoItemType.Header_OneYearPlan:
					action = (vto, list) => new AngularOneYearPlan(vto.OneYearPlan.Id) { Headers = list };
					break;
				case VtoItemType.Header_QuarterlyRocks:
					action = (vto, list) => new AngularQuarterlyRocks(vto.QuarterlyRocks.Id) { Headers = list };
					break;
				default:
					break;
			}


			await VtoAccessor.AddKV(GetUser(), value, type, action, key:"Measurables:",value:"Edit here...");
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> XUpdateRock(string pk, string name, string value) {
			switch (name.ToLower()) {
			case "accountable":
			await VtoAccessor.UpdateRock(GetUser(), pk.ToLong(), null, value.ToLong(), null, null);//VtoAccessor.UpdateRockAccountable(GetUser(), pk.ToLong(), value.ToLong());
			break;
			default:
			throw new ArgumentOutOfRangeException(name.ToLower());
			}
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
	}
}