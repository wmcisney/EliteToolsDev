using RadialReview.Accessors;
using RadialReview.Models.Downloads;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Files;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class ExportController : BaseController {
		//
		// GET: /Category/
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UserScorecard(long? start = null, long? end = null) {
			var range = new DateRange(start, end);
			await ScorecardAccessor.UserScorecardCsv(GetUser(), GetUser().Id, range, FileOrigin.UserGenerate);
			return Json(ResultObject.Success("Downloading..."), JsonRequestBehavior.AllowGet);
			//return File(csv.ToBytes(false), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() +"_"+GetUser().GetName().SanatizeForFiles(true) +"_Scorecard.csv");
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> LocalFile(string id) {
			if (!Config.IsLocal()) {
				throw new Exception("Endpoint only available locally");
			}
			var found = FileAccessor.GetLocalFile(id);

			return Download(found.Bytes, found.FileName, found.FileType);
		}
	}
}