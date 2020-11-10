using RadialReview.Accessors;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class DocumentsController : BaseController {
		// GET: Documents
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			return View(FileAccessor.GetVisibleFiles(GetUser(), GetUser().Id));
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<RedirectResult> Open(long id) {
			return Redirect(await FileAccessor.GetFileUrl(GetUser(), id));
		}

	}
}