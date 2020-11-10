using RadialReview.Accessors;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace RadialReview.Controllers {
	public partial class AccountabilityController : BaseController {
		// GET: Accountablity
		public ActionResult Index() {
			return View();
		}

		public class AccountabilityChartVM {
			public string CompanyImageUrl { get; set; }

			public long UserId;
			public long OrganizationId;
			public long ChartId;

			public long? FocusNode { get; internal set; }
			public bool ExpandAll { get; set; }
			public string Json { get; set; }
			public string CompanyName { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult OrgChart(long id, long? user = null, long? node = null, bool noheading = false, bool expandAll = false, bool prefetch = true) {
			var orgId = id;
			ViewBag.NoTitleBar = noheading;
			var accChartId = AccountabilityAccessor.GetOrganizationChartId(GetUser(), orgId);
			return RedirectToAction("Chart",new { id = accChartId, user = user, node = node, noheading = noheading, expandAll = expandAll, prefetch = prefetch });
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Chart(long? id = null, long? user = null, long? node = null, bool noheading = false, bool expandAll = false, bool prefetch = true) {
			ViewBag.NoTitleBar = noheading;
			var u = GetUser();
			var idr = id ?? u.Organization.AccountabilityChartId;
			user = user ?? u.Id;

			ViewBag.CanManagePositions = PermissionsAccessor.IsPermitted(GetUser(), x => x.EditPositions(GetUser().Organization.Id));

			if (node == null && user != null) {
				node = AccountabilityAccessor.GetNodesForUser(GetUser(), user.Value).FirstOrDefault().NotNull(x => (long?)x.Id);
			}

			var json = "false";
			if (prefetch) {
				var jsonJ = Json(Data(idr, node, user, expandAll), JsonRequestBehavior.AllowGet);
				json = new JavaScriptSerializer().Serialize(jsonJ.Data);
			}

			return View(new AccountabilityChartVM() {
				CompanyName = u.Organization.NotNull(x => x.GetName()),
				CompanyImageUrl = u.Organization.NotNull(x => x.Settings.GetImageUrl(ImageSize._img)),
				UserId = u.Id,
				OrganizationId = u.Organization.Id,
				ChartId = idr,
				FocusNode = node,
				ExpandAll = expandAll,
				Json = json
			});
		}
	}
}
