using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ReferralController : BaseController
    {


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> Modal() {
			var model = await ReferralAccessor.GenerateReferral(GetUser());
			return PartialView(model);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> Modal(ReferralModel model) {

			await ReferralAccessor.SendReferral(GetUser(), model);

			return Json(ResultObject.Success("Thank you for your referral, it means a lot!"));
		}

	}
}