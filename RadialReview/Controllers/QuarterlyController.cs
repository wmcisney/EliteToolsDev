using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.VTO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using RadialReview.Accessors.PDF;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using System.Threading.Tasks;
using static RadialReview.Accessors.PdfAccessor;
using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Areas.People.Accessors;
using RadialReview.Utilities.Pdf;
using PdfSharp.Drawing;
using RadialReview.Models.Json;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Accessors.PDF.Hangfire;
using RadialReview.Models.Downloads;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Accessors.PDF.Partial;
using System.IO;
using TractionTools.Utils.Pdf;
using RadialReview.Utilities;
using TractionTools.Utils.Pdf.Generators;

namespace RadialReview.Controllers {
	public class QuarterlyController : BaseController {
		// GET: Quarterly
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id) {
			ViewBag.IncludePeople = GetUser().Organization.Settings.EnablePeople;
			return PartialView(id);
		}



		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> UpdateQuarter() {
			var found = await QuarterlyAccessor.GetQuarterOrGenerate(GetUser(), GetUser().Organization.Id);
			return PartialView(new QuarterVM() {
				Name = found.Name,
				StartDate = found.StartDate,
				EndDate = found.EndDate,
				Year = found.Year,
				Quarter = found.Quarter,
				OrganizationId = found.OrganizationId,

			});
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> UpdateQuarter(QuarterVM model) {
			var res = await QuarterlyAccessor.UpdateQuarterModel(GetUser(), model.OrganizationId, model.Name, model.Year, model.Quarter, model.StartDate, model.EndDate);
			return Json(ResultObject.Create(new QuarterVM(res), "Updated " + res.Name + "."));
		}


		public class PrintoutPdfOptions {
			public bool coverPage { get; set; }

			public bool issues { get; set; }
			public bool todos { get; set; }
			public bool scorecard { get; set; }
			public bool rocks { get; set; }
			public bool headlines { get; set; }
			public bool vto { get; set; }
			public bool l10 { get; set; }
			public bool acc { get; set; }
			public bool pa { get; set; }
		}



		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> Printout(long id, FormCollection model/*, PdfAccessor.AccNodeJs root = null*/) {
			return await Printout(id,
				model["issues"].ToBooleanJS(),
				model["todos"].ToBooleanJS(),
				model["scorecard"].ToBooleanJS(),
				model["rocks"].ToBooleanJS(),
				model["headlines"].ToBooleanJS(),
				model["vto"].ToBooleanJS(),
				model["l10"].ToBooleanJS(),
				model["acc"].ToBooleanJS(),
				model["print"].ToBooleanJS(),
				model["quarterly"].ToBooleanJS(),
				model["pa"].ToBooleanJS()
			);
		}

		public class SendQuarterlyVM {

			public class ScheduledEmails {
				public long Id { get; set; }
				public string Email { get; set; }
				public string Date { get; set; }
				public bool Sent { get; set; }
			}

			public string ImplementerEmail { get; set; }
			public DateTime SendDate { get; set; }
			public bool Later { get; set; }
			public long RecurrenceId { get; set; }
			public List<ScheduledEmails> Scheduled { get; set; }
			public SendQuarterlyVM() {
				SendDate = DateTime.UtcNow;
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Send(long id) {
			PermissionsAccessor.EnsurePermitted(GetUser(), x => x.ViewL10Recurrence(id));

			var sched = QuarterlyAccessor.GetScheduledEmails(GetUser(), id).Where(x => x.SentTime == null || x.SentTime > DateTime.UtcNow.AddDays(-380));

			var model = new SendQuarterlyVM() {
				ImplementerEmail = GetUser().Organization.ImplementerEmail,
				RecurrenceId = id,
				Scheduled = sched.Select(x => new SendQuarterlyVM.ScheduledEmails() {
					Id = x.Id,
					Date = GetUser().GetTimeSettings().ConvertFromServerTime(x.ScheduledTime).ToString("MM/dd/yyyy"),
					Email = x.Email,
					Sent = x.SentTime != null
				}).ToList()
			};

			return PartialView(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UnscheduledQuarterly(long id) {
			await QuarterlyAccessor.DeleteScheduledEmail(GetUser(), id);
			return Json(ResultObject.Success("Email unscheduled."), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Send(SendQuarterlyVM model) {
			var sendTime = model.Later ? GetUser().GetTimeSettings().ConvertToServerTime(model.SendDate) : DateTime.UtcNow;
			await QuarterlyAccessor.ScheduleQuarterlyEmail(GetUser(), model.RecurrenceId, model.ImplementerEmail, sendTime);

			if ((model.ImplementerEmail??"").Trim()=="")
				return Json(ResultObject.SilentSuccess());

			return Json(ResultObject.Success((model.Later ? "Email scheduled!" : "Sending email.")));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public async Task<JsonResult> PrintVTO(long id, string fill = null, string border = null, string image = null, string filltext = null, string lighttext = null, string lightborder = null, string textColor = null) {
			var settings = new VtoPdfSettings(image, fill, lighttext, lightborder, filltext, textColor, border);
			Scheduler.Enqueue(() => GenerateVtoPdf.GenerateVTO(new HangfireCaller(GetUser()), id, FileOutputMethod.Print, settings));
			return Json(ResultObject.Success("Generating..."), JsonRequestBehavior.AllowGet);/*Pdf(merged, now + "_" + vto.Name + "_VTO.pdf", true);*/
		}
		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public async Task<ActionResult> PrintPages(long id, bool issues = false, bool todos = false, bool scorecard = false, bool rocks = false, bool vto = false, bool l10 = false, bool acc = false, bool print = false) {
			return await Printout(id, issues, todos, scorecard, rocks, false, vto, l10, acc, print);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public async Task<ActionResult> Printout(long id, bool issues = false, bool todos = false, bool scorecard = true, bool rocks = true, bool headlines = true, bool vto = true, bool l10 = true, bool acc = true, bool print = false, bool quarterly = true/*, PdfAccessor.AccNodeJs root = null*/, bool pa = false, int? maxSec = null) {
			var d = await PdfAccessor.QuarterlyPrintout(GetUser(), id, issues, todos, scorecard, rocks, headlines, vto, l10, acc, print, quarterly, pa, maxSec);
			return Pdf(d.Document, d.CreateTime.ToJsMs() + "_" + d.RecurrenceName + "_QuarterlyPrintout.pdf", true);
		}


		#region NewPrintout

		public class Modal2ViewModel {
			public long id { get; set; }
			public PrintoutPdfOptions options { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal2(long id) {
			ViewBag.IncludePeople = GetUser().Organization.Settings.EnablePeople;
			var vm = new Modal2ViewModel { id = id, options = QuarterlyPdf.GetDefaultPdfOptions() };
			return PartialView(vm);
			//return Modal(id);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> Printout2(long id, PrintoutPdfOptions options) {

			// default to puppeteer - should probably in the configuration
			var generator = PdfGeneratorType.puppeteer;

			var pdfPageSettings = new PdfPageSettings {
				Orientation = PdfPageOrientation.Landscape,
				HasFooterOnFirstPage = !options.coverPage
			};

			//var coverPageStream = await GenerateCoverPage(id, options, generator, pdfPageSettings);
			// turn it off
			//options.coverPage = false;

			var generatorObj = PdfEngine.GetGenerator(generator);
			var pdfStream = await QuarterlyPdf.GeneratePdfStreamForRecurrence(GetUser(), generatorObj, id, options, pdfPageSettings);


			return new FileStreamResult(pdfStream, "application/pdf");
		}



		/// <summary>
		/// Output Pdf
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		[HttpGet]
		[Access(AccessLevel.Any)]
		//[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> PdfHtml(long id, PrintoutPdfOptions options) {
			options.coverPage = true;
			options.rocks = true;
			options.acc = true;
			options.headlines = true;
			options.issues = true;
			options.pa = true;
			options.scorecard = true;
			options.todos = true;
			options.vto = true;

			var htmlSource = await QuarterlyPdf.GenerateHtml(GetUser(), id, options);

			//return View(string.Empty);
			return Content(htmlSource.FlattenPages(), "text/html");
		}


		private async Task<Stream> GenerateCoverPage(long id, PrintoutPdfOptions options, string generator, PdfPageSettings pdfPageSettings) {
			throw new NotImplementedException();
			//Stream coverPageStream = null;

			//// if generator is puppeteer, generate coverpage using selectPdf since it can't turn off the footer on first page
			//if (generator == "puppet" && options.coverPage) {
			//	var coverpageHtml = await QuarterlyPdf.GenerateCoverPageWithToC(GetUser(), id, options);

			//	coverPageStream = await PdfEngine.GenerateFromHtmlString(coverpageHtml, null, null, new PuppeteerGenerator(), pdfPageSettings);
			//}

			//return coverPageStream;
		}
		#endregion

	}
}
