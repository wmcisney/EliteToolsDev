using RadialReview.Accessors;
using RadialReview.Utilities.DataTypes;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class ReadonlyController : BaseController {
		// GET: Readonly
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			var meetings = L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id, false, false);
			return View(meetings);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Meeting(long id) {
			var range = new DateRange(DateTime.UtcNow.AddDays(-7 * 13), DateTime.UtcNow);
			var meeting = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id,true, true, true,range,false,range,false);
			return View(meeting);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Error() {			
			return View();
		}
	}
}