﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;

namespace RadialReview.Exceptions {
	public enum PaymentExceptionType {
		MissingToken = 1,
		ResponseError = 2,
		Fallthrough = 3,
		Uncaptured = 4,

	}
	public class PaymentException : Exception {

		public String OrganizationName { get; set; }
		public long OrganizationId { get; set; }
		public DateTime OccurredAt { get; set; }
		public decimal ChargeAmount { get; set; }
		public PaymentExceptionType Type { get; set; }
		public PaymentException(OrganizationModel organization, decimal chargeAmount, PaymentExceptionType type, String message = null) : base(message ?? "An error occurred in making a payment.") {
			OrganizationId = organization.NotNull(x => x.Id);
			OrganizationName = organization.NotNull(x => x.GetName());
			OccurredAt = DateTime.UtcNow;
			ChargeAmount = chargeAmount;
			Type = type;

		}
	}
}
