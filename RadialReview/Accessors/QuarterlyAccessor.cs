using Hangfire;
using NHibernate;
using RadialReview.Accessors.PDF.Partial;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Quarterly;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TractionTools.Utils.Pdf;

namespace RadialReview.Accessors {
	public class QuarterlyAccessor {

		public class YearQuarter {
			public int Year { get; set; }
			public int Quarter { get; set; }
			public DateTime CurrentYearStart { get; set; }
		}

		public static YearQuarter EstimateQuarter(DateTime now, DateTime? yearStart = null) {
			yearStart = yearStart ?? new DateTime(now.Year, 1, 1);
			var dir = yearStart.Value > now ? -1 : 1;
			//Adjust year start;
			while (yearStart.Value > now) {
				yearStart = yearStart.Value.AddYears(-1);
			}
			while (yearStart.Value < now.AddYears(-1)) {
				yearStart = yearStart.Value.AddYears(1);
			}
			//Find quarter
			var qtr = 0;
			var i = yearStart.Value;
			while (i < now) {
				i = i.AddMonths(3);
				if (i <= now) {
					qtr += 1;
				}
			}

			int quarter = (qtr % 4) + 1;
			int year = yearStart.Value.Year;
			if (yearStart.Value >= new DateTime(yearStart.Value.Year, 6, 1)) {
				year = year + 1;
			}

			return new YearQuarter() {
				Quarter = quarter,
				Year = year,
				CurrentYearStart = yearStart.Value,
			};
		}

		public static List<int> PossibleYears(DateTime now) {
			var start = now.AddDays(-1).Year - 1;
			var end = now.AddDays(180).Year;
			var list = new List<int>();
			for (var i = start; i <= end; i++) {
				list.Add(i);
			}
			return list;
		}

		public static async Task<QuarterModel> UpdateQuarterModel(UserOrganizationModel caller, long organizationId,
				string name = null, int? year = null, int? quarter = null,
				DateTime? startDate = null, DateTime? endDate = null, bool currentQuarterVerification = true) {
			if (currentQuarterVerification && year != null && !PossibleYears(DateTime.UtcNow).Any(x => x == year)) {
				throw new PermissionsException("Invalid Year");
			}
			if (currentQuarterVerification && quarter != null && (quarter.Value < 1 || quarter.Value > 4)) {
				throw new PermissionsException("Invalid Quater");
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanUpdateQuarter();
					var updates = new IQuarterHookUpdates();
					var generatedQuarterModel = GetQuarterOrGenerate_Unsaved(s, perms, organizationId, out bool genereated);
					var found = new QuarterModel {
						Name = name,
						Quarter = quarter.HasValue ? quarter.Value : generatedQuarterModel.Quarter,
						Year = year.HasValue ? year.Value : generatedQuarterModel.Year,
						StartDate = startDate.HasValue ? caller.GetTimeSettings().ConvertToServerTime(startDate.Value) : generatedQuarterModel.StartDate,
						EndDate = endDate.HasValue ? caller.GetTimeSettings().ConvertToServerTime(endDate.Value) : generatedQuarterModel.EndDate,
						NextReminder = DateTime.UtcNow,
						OrganizationId = organizationId
					};

					if (name != null && name != found.Name) {
						found.Name = name;
						updates.UpdatedName = true;
					}
					if (year != null && year != found.Year) {
						found.Year = year.Value;
						updates.UpdatedYear = true;
					}
					if (quarter != null && quarter != found.Quarter) {
						found.Quarter = quarter.Value;
						updates.UpdatedQuarter = true;
					}


					if (startDate != null && startDate != found.StartDate) {
						found.StartDate = startDate.Value;
						updates.UpdatedStartDate = true;
					}
					if (endDate != null && endDate != found.EndDate) {
						found.EndDate = endDate.Value;
						updates.UpdatedEndDate = true;
					}

					if (found.StartDate > found.EndDate)
						throw new PermissionsException("StartDate must be before the end date");
					if (currentQuarterVerification && found.StartDate.AddDays(-1) > DateTime.UtcNow.Date)
						throw new PermissionsException("StartDate must be before today");

					s.SaveOrUpdate(found);

					tx.Commit();
					s.Flush();
					await HooksRegistry.Each<IQuarterHook>((ses, x) => x.UpdateQuarter(ses, found, updates));
					return found;
				}
			}
		}

		public static async Task<QuarterModel> GetQuarterDoNotGenerate(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var generated = false;
					var qtr = GetQuarterOrGenerate_Unsaved(s, perms, organizationId, out generated);

					if (!generated) {
						return qtr;
					}
					return null;


				}
			}
		}
		/// <summary>
		/// onUndefined specifies the date to use when quarter is unset. Defaults to 90 days out.
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="organizationId"></param>
		/// <param name="onUndefined"></param>
		/// <returns></returns>
		public static async Task<DateTime> GetQuarterEndDate(UserOrganizationModel caller, long organizationId, DateTime? onUndefined = null) {
			return (await GetQuarterDoNotGenerate(caller, organizationId)).NotNull(x => x.EndDate) ?? onUndefined ?? DateTime.UtcNow.AddDays(90).Date;
		}

		public static async Task<QuarterModel> GetQuarterOrGenerate(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					bool generated;
					var found = GetQuarterOrGenerate_Unsaved(s, perms, organizationId, out generated);
					if (generated) {
						s.Save(found);
						tx.Commit();
						s.Flush();
						await HooksRegistry.Each<IQuarterHook>((ses, x) => x.GenerateQuarter(ses, found));
					}
					return found;
				}
			}
		}

		[Obsolete("Call Commit")]
		private static QuarterModel GetQuarterOrGenerate_Unsaved(ISession s, PermissionsUtility perms, long organizationId, out bool generated) {
			perms.ViewOrganization(organizationId);

			var quarters = s.QueryOver<QuarterModel>()
				.Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
				.List().ToList();
			var now = DateTime.UtcNow;
			return _GetCurrentQuarter(quarters, organizationId, now, out generated);
		}

		[Obsolete("Use get or generate instead. Public for testing only.")]
		public static QuarterModel _GetCurrentQuarter(List<QuarterModel> quarters, long organizationId, DateTime now, out bool generated) {
			//QuarterModel found = quarters.Where(x => x.StartDate <= now && now <= (x.EndDate ?? DateTime.MaxValue)).OrderBy(x => x.Id).FirstOrDefault();
			QuarterModel found = quarters.OrderByDescending(x => x.Id).FirstOrDefault();
			if (found == null) {
				var mostRecent = quarters.Where(x => x.EndDate != null).OrderBy(x => x.EndDate).LastOrDefault();

				if (mostRecent == null) {
					//estimate quarter start initial
					var estimate = EstimateQuarter(now);
					var qtr = estimate.Quarter;
					var year = estimate.Year;

					var date = new DateTime(now.Year, 1, 1).AddMonths((qtr - 1) * 3);
					found = new QuarterModel() {
						CreatedBy = null,
						StartDate = date,
						EndDate = null,
						Name = "Q" + qtr + " " + year,
						Quarter = qtr,
						Year = year,
						NextReminder = now,
						OrganizationId = organizationId,
					};
					generated = true;
				} else {
					if (now < mostRecent.EndDate.Value.AddMonths(3)) {
						//guess next quarter (which immediately follows this one
						var qtr = (((mostRecent.Quarter - 1) + 1) % 4) + 1;
						var year = mostRecent.Year;
						if (mostRecent.Quarter == 4) {
							year += 1;
						}

						found = new QuarterModel() {
							CreatedBy = null,
							StartDate = mostRecent.EndDate.Value,
							EndDate = null,
							Name = "Q" + qtr + " " + year,
							Quarter = qtr,
							Year = year,
							NextReminder = now,
							OrganizationId = organizationId,
						};
						generated = true;
					} else {
						//too many months have gone by.
						var yearStart = mostRecent.EndDate.Value.AddMonths((4 - mostRecent.Quarter) * 3);
						var estimate = EstimateQuarter(now, yearStart);
						var qtr = estimate.Quarter;
						var year = estimate.Year;
						var currentYearStart = estimate.CurrentYearStart;
						//var currentYearStart = new DateTime(now.Year, yearStart.Month, 1);
						//try {
						//	currentYearStart = new DateTime(now.Year, yearStart.Month, yearStart.Day);
						//} catch (Exception) {
						//	//eat it
						//}
						var date = currentYearStart.AddMonths((qtr - 1) * 3);
						found = new QuarterModel() {
							CreatedBy = null,
							StartDate = date,
							EndDate = null,
							Name = "Q" + qtr + " " + year,
							Quarter = qtr,
							Year = year,
							NextReminder = now,
							OrganizationId = organizationId,
						};
						generated = true;
					}
				}
			} else {
				generated = false;
			}

			return found;
		}

		public static async Task DeleteScheduledEmail(UserOrganizationModel caller, long emailId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var email = s.Get<QuarterlyEmail>(emailId);
					perms.ViewL10Recurrence(email.RecurrenceId);

					email.DeleteTime = DateTime.UtcNow;
					s.Update(email);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task ScheduleQuarterlyEmail(UserOrganizationModel caller, long recurrenceId, string email, DateTime sendTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(recurrenceId);
					email = (email??"").ToLower();
					if (email.Trim() != "") {
						var qe = new QuarterlyEmail() {
							Email = email,
							RecurrenceId = recurrenceId,
							ScheduledTime = sendTime,
							OrgId = caller.Organization.Id,
							SenderId = caller.Id,
						};

						var org = s.Get<OrganizationModel>(caller.Organization.Id);
						s.Update(org);

						s.Save(qe);

						org.ImplementerEmail = email;
						Scheduler.Schedule(() => ScheduledEmail_HangFire(qe.Id), Math2.Max(TimeSpan.FromMinutes(0), sendTime - DateTime.UtcNow));
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<QuarterlyEmail> GetScheduledEmails(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(recurrenceId);
					var scheduled = s.QueryOver<QuarterlyEmail>().Where(x => x.DeleteTime == null && x.ScheduledTime > DateTime.UtcNow.AddDays(-1) && x.RecurrenceId == recurrenceId).List().ToList();
					return scheduled;
				}
			}
		}

		[Queue(HangfireQueues.Immediate.SCHEDULED_QUARTERLY_EMAIL)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<string> ScheduledEmail_HangFire(long quarterlyEmailId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var qe = s.Get<QuarterlyEmail>(quarterlyEmailId);

					if (qe.SentTime != null) {
						throw new Exception("Already sent quarterly printout");
					}

					if (qe.DeleteTime != null) {
						throw new Exception("Request deleted");
					}

					var caller = s.Get<UserOrganizationModel>(qe.SenderId);
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(qe.RecurrenceId);

					var generated = caller.GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow);


					//generate
					var generatorObj = PdfEngine.GetGenerator(PdfGeneratorType.puppeteer);
					var options = QuarterlyPdf.GetDefaultPdfOptions();
					var pdfPageSettings = new PdfPageSettings {
						Orientation = PdfPageOrientation.Landscape,
						HasFooterOnFirstPage = !options.coverPage
					};
					var pdf = await QuarterlyPdf.GeneratePdfStreamForRecurrence(caller, generatorObj, qe.RecurrenceId, options, pdfPageSettings);


					//var pdf = await PdfAccessor.QuarterlyPrintout(caller, qe.RecurrenceId, false, false, true, true, true, true, true, true, false, true, false, null);

					var orgName = caller.Organization.GetName();
					var mail = Mail.To("QuarterlyPrintout", qe.Email).AddBcc(caller.GetEmail())
								   .SubjectPlainText("Quarterly Printout - " + orgName)
								   .Body(EmailStrings.QuarterlyPrintout_Body, orgName, ProductStrings.ProductName);
					mail.ReplyToAddress = caller.GetEmail();
					mail.ReplyToName = caller.GetName();
					using (var stream = new MemoryStream()) {
						pdf.CopyTo(stream);
						//pdf.Document.Save(stream, false);
						var base64 = Convert.ToBase64String(stream.ToArray());
						stream.Close();

						mail.AddAttachment(new Mandrill.Models.EmailAttachment() {
							Base64 = true,
							Content = base64,
							Type = "application/pdf",
							Name = generated.ToString("yyyyMMdd") + " - " + orgName + ".pdf",
						});

						await Emailer.SendEmail(mail);

						qe.SentTime = DateTime.UtcNow;
						s.Update(qe);
						tx.Commit();
						s.Flush();
						return qe.Email;
					}
				}
			}
		}
	}
}
