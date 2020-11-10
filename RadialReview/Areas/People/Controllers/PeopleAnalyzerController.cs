using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Angular;
using RadialReview.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace RadialReview.Areas.People.Controllers
{
    public class PeopleAnalyzerController : BaseController
    {
        // GET: People/PeopleAnalyzer
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index(bool noheading = false, long? recurrenceId = null) {
			ViewBag.NoTitleBar = noheading;
			//var pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), GetUser().Id);
			ViewBag.RecurrenceId = recurrenceId;
			return View();// pa);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Data(long? userId = null, long? recurrenceId = null,bool flat = false) {
			AngularPeopleAnalyzer pa;

			if (flat) {
				pa = QuarterlyConversationAccessor.GetPeopleAnalyzerSuperAdminFlat(GetUser(), GetUser().Id);
			} else {
				if (recurrenceId != null)
					pa = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(GetUser(), userId ?? GetUser().Id, recurrenceId.Value);
				else
					pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), userId ?? GetUser().Id, flat: flat);
			}

			var serializer = new JavaScriptSerializer();

			// For simplicity just use Int32's max value.
			// You could always read the value from the config section mentioned above.
			serializer.MaxJsonLength = Int32.MaxValue;

			var result = new ContentResult {
				Content = serializer.Serialize(pa),
				ContentType = "application/json"
			};
			return result;
		}

	}
}