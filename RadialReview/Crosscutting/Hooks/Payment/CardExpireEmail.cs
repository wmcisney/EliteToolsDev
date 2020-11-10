using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Accessors;
using RadialReview.Variables;

namespace RadialReview.Crosscutting.Hooks.Payment {
	public class CardExpireEmail : IPaymentHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			var org = s.Get<OrganizationModel>(token.OrganizationId);
			var admins = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.ManagingOrganization == true && x.Id == token.OrganizationId).List().ToList();
			var emails = new List<Mail>();


			//REPLACE_ME
			var subject = s.GetSettingOrDefault(Variable.Names.UPDATE_CARD_SUBJECT, "[Action Required] EliteTools - Update Payment Information");

			foreach (var a in admins) {
				var mail = Mail
					.To("CardExpire", a.GetEmail())
					.SubjectPlainText(subject)
					.Body(EmailStrings.UpdateCard_Body, Config.ProductName(), Config.BaseUrl(null));
				//REPLACE_ME
				mail.ReplyToAddress = s.GetSettingOrDefault("SupportEmail", "support@dlptools.com");
				//REPLACE_ME
				mail.ReplyToName = "EliteTools Support";
			}
			await Emailer.SendEmails(emails);
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public HookPriority GetHookPriority() {
			//noop
			return HookPriority.Low;
		}

		public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
			//noop
		}

		public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
			//noop
		}

		public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token,decimal amount) {
			//noop
		}

		public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
			//noop
		}
	}
}