using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Calculators {
#if false
	public class UserCalculator {

		private long OrgId { get; set; }
		private ISession Session { get; set; }
		private List<UserProductEvent> Events { get; set; }

		private List<UQ> AllPeopleList { get; set; }

		private PaymentPlan_Monthly Plan { get; set; }

		public UserCalculator(ISession s, long orgId, PaymentPlanModel planModel, DateRange range) {
			if (NHibernateUtil.GetClass(planModel) != typeof(PaymentPlan_Monthly)) {
				throw new PermissionsException("Unhandled Payment Plan");
			}
			Plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(planModel);
			if (Plan.OrgId != orgId)
				throw new Exception("Org Id do not match");

			var rangeStart = range.StartTime;
			var rangeEnd = range.EndTime;

			Session = s;
			OrgId = orgId;
			UserModel u = null;
			UserOrganizationModel uo = null;
			var events = s.QueryOver<UserProductEvent>().Where(x => x.OrgId == orgId).Future();
			AllPeopleList = s.QueryOver<UserOrganizationModel>(() => uo)
									.Left.JoinAlias(() => uo.User, () => u)                 ///Existed any time during this range.
									.Where(() => uo.Organization.Id == orgId && uo.CreateTime <= rangeEnd && (uo.DeleteTime == null || uo.DeleteTime > rangeStart) && !uo.IsRadialAdmin)
									.Select(x => x.Id, x => u.IsRadialAdmin, x => x.IsClient, x => x.User.Id, x => x.EvalOnly)
									.List<object[]>()
									.Select(x => new UQ {
										UserOrgId = (long)x[0],
										IsRadialAdmin = (bool?)x[1],
										IsClient = (bool)x[2],
										UserId = (string)x[3],
										IsRegistered = x[3] != null,
										EvalOnly = (bool?)x[4] ?? false
									})
									.Where(x => x.IsRadialAdmin == null || (bool)x.IsRadialAdmin == false)
									.ToList();

			Events = events.ToList();

			if (Plan.NoChargeForClients) {
				AllPeopleList = AllPeopleList.Where(x => x.IsClient == false).ToList();
			}
			if (Plan.NoChargeForUnregisteredUsers) {
				AllPeopleList = AllPeopleList.Where(x => x.IsRegistered).ToList();
			}
		}


		public class UQ {
			public long UserOrgId { get; set; }
			public bool? IsRadialAdmin { get; set; }
			public bool IsClient { get; set; }
			public String UserId { get; set; }
			public bool IsRegistered { get; set; }
			public bool EvalOnly { get; set; }
		}

		private class ChargeEvent {
			public bool Chargeable { get; set; }
			public DateTime EventTime { get; set; }
			//public bool Active { get; set; }
		}


#pragma warning disable CS0618 // Type or member is obsolete
		public static double PercentageChargeable(List<UserProductEvent> allEvents, long userId, ProductType product, DateTime startDate, DateTime endDate) {

			if (startDate >= endDate)
				throw new Exception("Start date must be before end date");

			var evts = allEvents
				.Where(x => x.UserId == userId && x.EventTime <= endDate && x.ProductType == product)
				.OrderBy(x => x.EventTime)
				.Select(x => new ChargeEvent {
					EventTime = x.EventTime,
					Chargeable = x.Chargeable && x.EventType == UserProductEventType.Activate,
				}).ToList();

			var initialSnapshot = new ChargeEvent {
				Chargeable = false,
				EventTime = DateTime.MinValue,
			};


			foreach (var e in evts.Where(x => x.EventTime <= startDate)) {
				initialSnapshot.Chargeable = e.Chargeable;
			}

			var chargeDuration = TimeSpan.FromSeconds(0);

			var prev = initialSnapshot;
			prev.EventTime = startDate;

			foreach (var e in evts.Where(x => startDate < x.EventTime && x.EventTime <= endDate)) {
				if (prev.Chargeable) {
					chargeDuration += TimeSpan.FromDays(Math.Ceiling((e.EventTime - prev.EventTime).TotalDays));
				}
				prev = e;
			}

			if (prev.Chargeable) {
				chargeDuration += TimeSpan.FromDays(Math.Ceiling((endDate - prev.EventTime).TotalDays));
			}


			var percent = chargeDuration.TotalDays / (endDate - startDate).TotalDays;
			return Math.Max(0, Math.Min(1, percent));



		}
#pragma warning restore CS0618 // Type or member is obsolete


		public static List<Itemized> CalculateCharge(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime) {
			var itemized = new List<Itemized>();

			if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly)) {
				var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(paymentPlan);
				var rangeStart = executeTime.Subtract(plan.SchedulerPeriod());// TimespanExtensions.OneMonth());
				var rangeEnd = executeTime;
				var durationMult = plan.DurationMultiplier();
				var durationDesc = plan.MultiplierDesc();

				///HEAVY LIFTING
				var calc = new UserCalculator(s, org.Id, plan, new DateRange(rangeStart, rangeEnd));

				//s.Auditer().GetRevisionNumberForDate(<OrganizationModel>(org.Id,);

				var allRevisions = s.AuditReader().GetRevisionsBetween<OrganizationModel>(s, org.Id, rangeStart, rangeEnd).ToList();
				var qcEnabled = /*org.Settings.EnableReview;//*/allRevisions.Any(x => x.Object.Settings.EnableReview || x.Object.Settings.EnablePeople);
				var l10Enabled = /*org.Settings.EnableL10; //*/allRevisions.Any(x => x.Object.Settings.EnableL10);

				//In case clocks are off.
				var executionCalculationDate = executeTime.AddDays(1).Date;


				if (plan.BaselinePrice > 0) {
					var reviewItem = new Itemized() {
						Name = "Elite Tools" + durationDesc,
						Price = plan.BaselinePrice * durationMult,
						Quantity = 1,
					};
					itemized.Add(reviewItem);
				}


				if (l10Enabled) {
					var l10Item = new Itemized() {
						Name = "Level 10 Meeting Software" + durationDesc,
						Price = plan.L10PricePerPerson * durationMult,
						Quantity = calc.NumberL10UsersToChargeFor,
					};
					if (l10Item.Quantity != 0) {
						itemized.Add(l10Item);

						if (!(plan.L10FreeUntil == null || !(plan.L10FreeUntil.Value.Date > executionCalculationDate))) {
							//Discount it since it is free
							itemized.Add(l10Item.Discount());
						}
					}
				}

				if (qcEnabled) {
					var reviewItem = new Itemized() {
						Name = "People Tools™" + durationDesc,
						Price = plan.ReviewPricePerPerson * durationMult,
						Quantity = calc.NumberQCUsersToChargeFor//allPeopleList.Where(x => !x.IsClient).Count()
					};
					if (reviewItem.Quantity != 0) {
						itemized.Add(reviewItem);
						if (!(plan.ReviewFreeUntil == null || !(plan.ReviewFreeUntil.Value.Date > executionCalculationDate))) {
							//Discount it since it is free
							itemized.Add(reviewItem.Discount());
						}
					}
				}

				if ((plan.FreeUntil.Date > executionCalculationDate)) {
					//Discount it since it is free
					var total = itemized.Sum(x => x.Total());
					itemized.Add(new Itemized() {
						Name = "Discount",
						Price = -1 * total,
						Quantity = 1,
					});
				}

				var discountLookup = new Dictionary<AccountType, string>() {
                    //{AccountType.Cancelled,"Inactive Account" },
                    {AccountType.Implementer,"Discount (Implementer)" },
					{AccountType.Dormant,"Inactive Account" },
					{AccountType.SwanServices, "Demo Account (Swan Services)" },
					{AccountType.Other, "Discount (Special Account)" },
					{AccountType.UserGroup, "Discount (User Group)" },
					{AccountType.Coach, "Discount (Coach)" },
					{AccountType.FreeForever, "Discount (Free)" },
				};

				if (org != null && discountLookup.ContainsKey(org.AccountType) && itemized.Sum(x => x.Total()) != 0) {
					var total = itemized.Sum(x => x.Total());
					itemized.Add(new Itemized() {
						Name = discountLookup[org.AccountType],//"Discount (Implementer)",
						Price = -1 * total,
						Quantity = 1,
					});
				}

				if (org != null && org.AccountType == AccountType.Cancelled) {
					itemized = new List<Itemized>();
				}

			} else {
				throw new PermissionsException("Unhandled Payment Plan");
			}

			var credits = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id && x.AmountRemaining > 0).List().ToList();
			_ApplyCreditsToInvoice(s, org, itemized, credits);

			return itemized;
		}


	}
#endif
}