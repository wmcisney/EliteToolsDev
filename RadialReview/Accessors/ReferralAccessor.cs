using NHibernate;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class ReferralAccessor {

		/// <summary>
		/// Do not edit
		/// </summary>
		public class ReferralEmail {

			public ReferralEmail() { }

			public ReferralEmail(string subject, string body) {
				Subject = subject;
				Body = body;
			}

			public string Subject { get; set; }

			public string Body { get; set; }

		}




		public static async Task<ReferralModel> GenerateReferral(UserOrganizationModel caller) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					ReferralEmail email = GetDefaultReferralEmail(caller, s);

					var model = new ReferralModel() {
						FromUserId = caller.Id,
						EmailFrom = caller.GetEmail(),
						EmailBody = email.Body,
						EmailSubject = email.Subject
					};

					s.Save(model);

					tx.Commit();
					s.Flush();
					return model;
				}
			}
		}

		public static string REFERRAL_LINK = "https://www.mytractiontools.com/schedule-traction-tools-demo/";

		private static ReferralEmail GetDefaultReferralEmail(UserOrganizationModel caller, ISession s) {
			var lastUsed = s.QueryOver<ReferralModel>().Where(x => x.DeleteTime == null && x.SendTime != null && x.FromUserId == caller.Id).OrderBy(x => x.CreateTime).Desc.Take(1).SingleOrDefault();
			if (lastUsed != null) {
				return new ReferralEmail(lastUsed.EmailSubject, lastUsed.EmailBody);
			} else {
				var email = new ReferralEmail {
					Subject = "Elite Tools",
					Body =
(@"Hey!

I wanted to invite you to checkout Elite Tools, they'd be happy to provide you with a 30 minute demo.
I think you will love the time-saving features and the efficiency provided by Elite Tools. 
Please feel free to schedule a meeting directly on our calendar by clicking on the link below:

" + REFERRAL_LINK + @"

Thanks!

{0}")
				};
				if (caller.Organization.AccountType == Models.Enums.AccountType.Implementer || caller.Organization.AccountType == Models.Enums.AccountType.Coach) {
					email = s.GetSettingOrDefault<ReferralEmail>(Variable.Names.EOSI_REFERRAL_EMAIL, email);
				} else {
					email = s.GetSettingOrDefault<ReferralEmail>(Variable.Names.CLIENT_REFERRAL_EMAIL, email);
				}

				email.Body = string.Format(email.Body, caller.GetName());
				return email;
			}
		}


		public static async Task SendReferral(UserOrganizationModel caller, ReferralModel model) {
			ReferralModel found;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					found = s.Get<ReferralModel>(model.Id);
					perms.Self(found.FromUserId);

					if (found == null) {
						throw new PermissionsException("Referral not found");
					}
					if (found.SendTime != null) {
						throw new PermissionsException("Referral already sent");
					}

					found.EmailSubject = model.EmailSubject;
					found.EmailBody = model.EmailBody;
					found.EmailTo = model.EmailTo.Trim().ToLower();
					found.ToName = model.ToName.NotNull(x => x.Trim());
					found.SendTime = DateTime.UtcNow;

					s.Update(found);
					tx.Commit();
					s.Flush();
				}
			}

			string append = "";
			if (found.ToName != null) {
				try {
					var split = found.ToName.Split(new[] { ' ' },StringSplitOptions.RemoveEmptyEntries);
					if (split.Length > 0) {
						append = "&firstname="+Uri.EscapeUriString(split.First());
						if (split.Count() > 1) {
							append += "&lastname="+Uri.EscapeUriString(string.Join(" ", split.Skip(1).ToList()));
						}
					}
				} catch (Exception e) {
				}
			}



			var body = found.EmailBody.Replace(REFERRAL_LINK, REFERRAL_LINK + "?email=" + model.EmailTo + "&field:5042731=" + found.FromUserId + append);
			body = Emailer.ReplaceLink(body, true);
			body = body.Replace("\n", "<br/>");

			var email = Mail.To("Referral", found.EmailTo)
							.AddBcc(found.EmailFrom)
							.Subject(found.EmailSubject)
							.Body(body)
							.AddTracker(found.Id);

			email.ReplyToName = caller.GetName();
			email.ReplyToAddress = model.EmailFrom;

			await Emailer.EnqueueEmail(email, wrapped: false);

		}



	}
}