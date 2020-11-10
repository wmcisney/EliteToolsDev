using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Downloads;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static RadialReview.Accessors.PdfAccessor;

namespace RadialReview.Accessors.PDF.Hangfire {
	public class GenerateVtoPdf {

		public class VtoSettings {
			public string fill { get; set; }
			public string border { get; set; }
			public string image { get; set; }
			public string filltext { get; set; }
			public string lighttext { get; set; }
			public string lightborder { get; set; }
			public string textColor { get; set; }
		}

		public async static Task GenerateVTO(HangfireCaller hangfire, long vtoId,FileOutputMethod method, VtoPdfSettings settings) {
			UserOrganizationModel caller;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserOrganizationModel>(hangfire.UserOrganizationId);
				}
			}

			var vto = VtoAccessor.GetAngularVTO(caller, vtoId);
			var doc = PdfAccessor.CreateDoc(caller, vto.Name + " Vision/Traction Organizer");

			await PdfAccessor.AddVTO(doc, vto, caller.GetOrganizationSettings().GetDateFormat(), settings);
			var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";

			var merger = new DocumentMerger();
			merger.AddDoc(doc);
			var merged = merger.Flatten(vto.Name + " VTO.pdf", false, true, caller.Organization.Settings.GetDateFormat());

			var tags = new List<TagModel>();
			if (vto.L10Recurrence.HasValue) {
				tags.Add(TagModel.Create<L10Recurrence>(vto.L10Recurrence.Value, "V/TO"));
			}
			tags.Add(TagModel.Create("V/TO"));
			using (MemoryStream stream = new MemoryStream()) {
				merged.Save(stream, false);
				stream.Seek(0, SeekOrigin.Begin);

				await FileAccessor.Save_Unsafe(
					hangfire.UserOrganizationId,
					stream,
					vto.Name, "pdf",
					"Vision/Traction Organizer generated " + hangfire.GetCallerLocalTime().ToShortDateString(),
					FileOrigin.UserGenerate,
					method,
					FileNotification.NotifyCaller(hangfire.ConnectionId),
					null, tags.ToArray());
				stream.Close();
			}
				
		}
	}
}