using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsvHelper;
using RadialReview.Utilities;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using System.Net;
using System.Globalization;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Issues;
using System.Text;
using SpreadsheetLight;
using ExcelDataReader;
using System.Data;
using Newtonsoft.Json;

namespace RadialReview.Controllers {
	public partial class UploadController : BaseController {

		public enum CsvSampleType {
			rocks,
			issues,
			todos,
			headlines,
			scorecard,
		}

		//private static string ZERO_WIDTH_SPACE = "\u200B";

		private static string ZeroEncoder(byte number) {
			var zero = "\u200B";
			var one = "\u200D";
			var pad = "\u200C";
			var encoded = Convert.ToString(number, 2);
			return pad + encoded.Replace("0", zero).Replace("1", one) + pad;
		}

		private static string FIRST_COLUMN = ZeroEncoder(1);
		private static string SECOND_COLUMN = ZeroEncoder(2);
		private static string THIRD_COLUMN = ZeroEncoder(3);
		private static string FOURTH_COLUMN = ZeroEncoder(4);
		private static string FIFTH_COLUMN = ZeroEncoder(5);
		private static string SIXTH_COLUMN = ZeroEncoder(6);

		private static string FIRST_ROW = ZeroEncoder(16);

		private static string IGNORE_ROW = ZeroEncoder(32);


		private static Csv GetSample(UserOrganizationModel caller, CsvSampleType type) {

			var c = new Csv("" + type);
			switch (type) {
				case CsvSampleType.rocks:
					c.Add("1", (FIRST_COLUMN) + "Rocks", (IGNORE_ROW) + $"Close the ABC Co. Deal");
					c.Add("1", (SECOND_COLUMN) + "Who", caller.GetName());
					c.Add("1", (THIRD_COLUMN) + "Due", DateTime.UtcNow.AddDays(90).ToString("MM/dd/yyyy"));
					c.Add("1", (FOURTH_COLUMN) + "Details", "Terms agreed on and contract has been signed.");
					break;
				case CsvSampleType.issues:
					c.Add("1", (FIRST_COLUMN) + "Issues", (IGNORE_ROW) + "Overhead is too high");
					c.Add("1", (SECOND_COLUMN) + "Details", "Costs are growing...");
					c.Add("1", (THIRD_COLUMN) + "Owner", caller.GetName());
					break;
				case CsvSampleType.todos:
					c.Add("1", (FIRST_COLUMN) + "To-dos", (IGNORE_ROW) + "Contact Allison for referral");
					c.Add("1", (SECOND_COLUMN) + "Due", DateTime.UtcNow.AddDays(7).ToString("MM/dd/yyyy"));
					c.Add("1", (THIRD_COLUMN) + "Who", caller.GetName());
					c.Add("1", (FOURTH_COLUMN) + "Details", "Get list of preferred providers.");
					break;
				case CsvSampleType.headlines:
					c.Add("1", (FIRST_COLUMN) + "Headlines", (IGNORE_ROW) + "Jamsison bought a house");
					c.Add("1", (SECOND_COLUMN) + "Details", "Upgraded to a 3 bedroom.");
					break;
				case CsvSampleType.scorecard:

					var startDates = new List<string>();
					var endDates = new List<string>();
					var revenues = new List<string>();
					var rand = new Random();
					for (var i = 13; i >= 1; i--) {
						startDates.Add(DateTime.UtcNow.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday).AddDays(-i * 7).ToString("MM/dd/yyyy"));
						endDates.Add(DateTime.UtcNow.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday).AddDays(-(i - 1) * 7 - 1).ToString("MM/dd/yyyy"));
						revenues.Add("$" + rand.Next(-10, 500) * 100);
					}
					var WHO_COL = " ";
					var MEAS_COL = "  ";
					var GOAL_COL = (FIRST_ROW);
					c.Add("1", WHO_COL, (FIRST_COLUMN) + "Who");
					c.Add("1", MEAS_COL, (SECOND_COLUMN) + "Measurable");
					c.Add("1", GOAL_COL, (THIRD_COLUMN) + "Goal");
					for (var i = 0; i < startDates.Count; i++) {
						c.Add("1", startDates[i], endDates[i]);
					}


					c.Add("2", WHO_COL,  caller.GetName());
					c.Add("2", MEAS_COL, "Revenue on W" + (IGNORE_ROW) +"idgets");
					c.Add("2", GOAL_COL, ">$14999");
					for (var i = 0; i < startDates.Count; i++) {
						c.Add("2", startDates[i], revenues[i]);
					}
					break;
				default:
					break;
			}
			return c;
		}

		private FileResult CsvToFile(CsvSampleType type) {
			var csv = UploadController.GetSample(GetUser(), type);
			var str = csv.ToCsv(false);
			var bytes = Encoding.UTF8.GetBytes(str);
			Response.AddHeader("Content-Disposition", "attachment;filename=sample_" + type + ".csv");

			return new FileContentResult(bytes, "text/csv");
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> SampleL10() {
			var export = new CsvSampleType[] {
				CsvSampleType.scorecard,
				CsvSampleType.rocks,
				//CsvSampleType.headlines,
				CsvSampleType.todos,
				CsvSampleType.issues
			};

			var instructions = new Csv("Instructions");
			instructions.Add("0", "INSTRUCTIONS", "");
			instructions.Add("1", "INSTRUCTIONS", "Move your data into each of the sheets");
			instructions.Add("2", "INSTRUCTIONS", "Do not rename the sheets. We use them in uploading.");
			instructions.Add("2.1", "INSTRUCTIONS", "");
			instructions.Add("3", "INSTRUCTIONS", "Rocks, measurables, to-dos, " +/*headlines,*/" and issues that are uploaded will be created. ");
			instructions.Add("4", "INSTRUCTIONS", "If you want to share rocks or measurables from another meeting, exclude it from the upload and add them manually in the software.");

			var pages = export.Select(x => UploadController.GetSample(GetUser(), x)).ToList();
			pages.Insert(0, instructions);

			var file = CsvUtility.ToXls(false, pages);
			var name = "UploadeL10_" + GetUser().GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow).ToString("yyyyMMdd");

			//Pretty up the Instructions page
			file.SelectWorksheet("Instructions");
			file.SetCellStyle(1, 1, new SLStyle() {
				Font = new SLFont() {
					Bold = true
				}
			});
			file.SetColumnWidth(1, 120);

			var bold = file.CreateStyle();
			bold.Font.Bold = true;

			try {
				file.SelectWorksheet("scorecard");
				file.SetColumnWidth(1, 13);
				file.SetColumnWidth(2, 13);
				file.SetColumnWidth(3, 13);
				for (var i = 0; i < 13; i++) {
					file.SetColumnWidth(4 + i, 10);
				}
			} catch (Exception) {
			}

			try {
				file.SelectWorksheet("rocks");
				file.SetColumnWidth(1, 33);
				file.SetColumnWidth(2, 13);
				file.SetColumnWidth(3, 10);
				file.SetColumnWidth(4, 66);
				file.SetCellStyle("A1", "D1", bold);

			} catch (Exception) {
			}

			try {
				file.SelectWorksheet("todos");
				file.SetColumnWidth(1, 33);
				file.SetColumnWidth(2, 10);
				file.SetColumnWidth(3, 13);
				file.SetColumnWidth(4, 66);
				file.SetCellStyle("A1", "D1", bold);

			} catch (Exception) {
			}
			try {
				file.SelectWorksheet("issues");
				file.SetColumnWidth(1, 33);
				file.SetColumnWidth(2, 66);
				file.SetColumnWidth(3, 13);
				file.SetCellStyle("A1", "C1", bold);

			} catch (Exception) {
			}
			file.SelectWorksheet("Instructions");


			return Xls(file, name);
		}

		private class HttpPostFileInternal : HttpPostedFileBase {
			public override Stream InputStream { get { return _internalStream; } }
			public override string FileName { get { return _fileName; } }
			public override string ContentType { get { return _contentType; } }
			public override int ContentLength { get { return (int)_internalStream.Length; } }

			private Stream _internalStream { get; set; }
			private string _fileName { get; set; }
			private string _contentType { get; set; }

			public HttpPostFileInternal(string fileName, string contentType, Stream stream) : base() {
				this._internalStream = stream;
				this._fileName = fileName;
				this._contentType = contentType;
			}
		}

		public class PathScript {
			public PathScript(ResultObject result, string script) {
				Result = result;
				Script = script;
				StepGuess = new List<int[]>();
			}

			public ResultObject Result { get; set; }
			public string Script { get; set; }
			public List<int[]> StepGuess { get; set; }
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> UploadL10(long recurrenceId, HttpPostedFileBase file) {
			PermissionsAccessor.Permitted(GetUser(), x => x.EditL10Recurrence(recurrenceId));

			var upload = await UploadAccessor.UploadAndParseExcel(GetUser(), UploadType.L10, file, ForModel.Create<L10Recurrence>(recurrenceId));
			if (file != null && file.ContentLength > 0) {
				using (var ms = new MemoryStream()) {
					file.InputStream.Seek(0, SeekOrigin.Begin);
					await file.InputStream.CopyToAsync(ms);

					var list = new List<PathScript>();
					ms.Seek(0, SeekOrigin.Begin);

					var sheetsAsCsv = new List<DataTableHelper>();

					using (var document = ExcelReaderFactory.CreateReader(ms)) {
						var tables = document.AsDataSet();
						for (var i = 0; i < tables.Tables.Count; i++) {
							var t = tables.Tables[i];
							sheetsAsCsv.Add(CsvUtility.FromTable(t));
						}
					}

					{
						var page = "" + CsvSampleType.scorecard;
						if (sheetsAsCsv.Any(x => x.Name == page)) {
							var sheet = sheetsAsCsv.First(x => x.Name == page).ExcludeRows(r => r.Find(IGNORE_ROW)!=null);
							var f = new HttpPostFileInternal("Scorecard", "text/csv", sheet.ToCsv().ToStream());
							JsonResult res = await UploadRecurrenceFile(recurrenceId, f, UploadType.Scorecard, true);

							var obj = res.Data as ResultObject;
							var output = new PathScript(obj, "UploadScorecard.js");
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((FIRST_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((SECOND_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((THIRD_COLUMN))));
							output.StepGuess.Add(sheet.GetRowStartingRight(sheet.Find((FIRST_ROW))));
							list.Add(output);
						}
					}
					{
						var page = "" + CsvSampleType.rocks;
						if (sheetsAsCsv.Any(x => x.Name == page)) {
							var sheet = sheetsAsCsv.First(x => x.Name == page).ExcludeRows(r => r.Find(IGNORE_ROW) != null);
							var f = new HttpPostFileInternal("Rocks", "text/csv", sheet.ToCsv().ToStream());
							JsonResult res = await UploadRecurrenceFile(recurrenceId, f, UploadType.Rocks, true);
							var obj = res.Data as ResultObject;
							var output = new PathScript(obj, "UploadRocks.js");
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((FIRST_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((SECOND_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((THIRD_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((FOURTH_COLUMN))));
							list.Add(output);
						}
					}
					{
						var page = "" + CsvSampleType.todos;
						if (sheetsAsCsv.Any(x => x.Name == page)) {
							var sheet = sheetsAsCsv.First(x => x.Name == page).ExcludeRows(r => r.Find(IGNORE_ROW) != null);
							var f = new HttpPostFileInternal("Todos", "text/csv", sheet.ToCsv().ToStream());
							JsonResult res = await UploadRecurrenceFile(recurrenceId, f, UploadType.Todos, true);
							var obj = res.Data as ResultObject;
							var output = new PathScript(obj, "UploadTodos.js");
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((FIRST_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((SECOND_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((THIRD_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((FOURTH_COLUMN))));
							list.Add(output);
						}
					}
					{
						var page = "" + CsvSampleType.issues;
						if (sheetsAsCsv.Any(x => x.Name == page)) {
							var sheet = sheetsAsCsv.First(x => x.Name == page).ExcludeRows(r => r.Find(IGNORE_ROW) != null);
							var f = new HttpPostFileInternal("Issues", "text/csv", sheet.ToCsv().ToStream());
							JsonResult res = await UploadRecurrenceFile(recurrenceId, f, UploadType.Issues, true);
							var obj = res.Data as ResultObject;
							var output = new PathScript(obj, "UploadIssues.js");
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((FIRST_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((SECOND_COLUMN))));
							output.StepGuess.Add(sheet.GetColumnStartingUnder(sheet.Find((THIRD_COLUMN))));
							list.Add(output);
						}
					}

					return Json(ResultObject.SilentSuccess(list));

				}
			}

			return Json(ResultObject.CreateError("Upload failed"));

		}

	}
}