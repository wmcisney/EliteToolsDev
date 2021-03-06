﻿using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Payments;
using System;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public class IInvoiceUpdates {
		public bool ForgivenChanged { get; set; }
		public bool PaidStatusChanged { get; set; }
	}
	public interface IPaymentHook : IHook {
		Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token);

		Task CardExpiresSoon(ISession s, PaymentSpringsToken token);

		Task UpdateCard(ISession s, PaymentSpringsToken token);
		Task SuccessfulCharge(ISession s, PaymentSpringsToken token, decimal amount);

		Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt);
		Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt);

	}

	public interface IInvoiceHook : IHook {

		Task UpdateInvoice(ISession s, InvoiceModel invoice, IInvoiceUpdates updates);
		Task InvoiceCreated(ISession s, InvoiceModel invoice);
	}
}