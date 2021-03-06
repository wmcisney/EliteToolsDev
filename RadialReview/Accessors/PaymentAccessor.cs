﻿using Hangfire;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.Tasks;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using static RadialReview.Utilities.PaymentSpringUtil;

namespace RadialReview.Accessors {
	public class PaymentAccessor : BaseAccessor {

		/// <summary>
		/// Returns true if delinquent by certain number of days
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="orgId"></param>
		/// <param name="daysOverdue"></param>
		/// <returns></returns>
		public static bool ShowDelinquent(UserOrganizationModel caller, long orgId, int daysOverdue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					try {
						perms.EditCompanyPayment(orgId);
						var cards = s.QueryOver<PaymentSpringsToken>()
							.Where(x => x.OrganizationId == orgId && x.DeleteTime == null && x.Active == true)
							.List().ToList();

						if (!cards.Any()) {
							var org = s.Get<OrganizationModel>(orgId);
							if (org.AccountType != AccountType.Demo) {
								return false;
							}

							if (org == null) {
								throw new NullReferenceException("Organization does not exist");
							}

							if (org.DeleteTime != null) {
								throw new FallthroughException("Organization was deleted.");
							}

							var plan = org.PaymentPlan;
							if (plan.FreeUntil.AddDays(daysOverdue) < DateTime.UtcNow) {
								return true;
							}
						}
						return false;
					} catch (Exception) {
						return false;
					}
				}
			}
		}

		#region Payment Information
		public static List<PaymentMethodVM> GetCards(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (!caller.User.IsRadialAdmin && organizationId != caller.Organization.Id) {
						throw new PermissionsException("Organization Ids do not match");
					}

					PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
					var cards = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
					return cards.Select(x => new PaymentMethodVM(x)).ToList();
				}
			}
		}

		public static async Task<PaymentMethodVM> SetCard(UserOrganizationModel caller, long orgId, PaymentTokenVM token) {
			return await SetCard(caller, orgId, token.id, token.@class, token.card_type, token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year, null, null, null, null, null, null, null, null, null, true);
		}

		public static async Task<PaymentMethodVM> SetACH(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
			string token_type, string account_type, string firstName, string lastName, string accountLast4, string routingNumber, String address_1, String address_2,
			String city, String state, string zip, string phone, string website, string country, string email, bool active) {
			if (token_type != "bank_account") {
				throw new PermissionsException("ACH requires token_type = 'bank_account'");
			}

			return await SetToken(caller, organizationId, tokenId, @class, null, null, null, 0, 0, address_1, address_2, city, state, zip, phone, website, country, email, active, accountLast4, routingNumber, firstName, lastName, account_type, PaymentSpringTokenType.BankAccount);
		}

		public static async Task<PaymentMethodVM> SetCard(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
			string cardType, string cardOwnerName, string last4, int expireMonth, int expireYear, String address_1, String address_2,
			String city, String state, string zip, string phone, string website, string country, string email, bool active) {

			return await SetToken(caller, organizationId, tokenId, @class, cardType, cardOwnerName, last4, expireMonth, expireYear, address_1, address_2, city, state, zip, phone, website, country, email, active, null, null, null, null, null, PaymentSpringTokenType.CreditCard);

		}

		private static async Task<PaymentMethodVM> SetToken(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
			string cardType, string cardOwnerName, string cardLast4, int cardExpireMonth, int cardExpireYear, String address_1, String address_2,
			String city, String state, string zip, string phone, string website, string country, string email, bool active,
			string bankLast4, string bankRouting, string bankFirstName, string bankLastName, string bankAccountType, PaymentSpringTokenType tokenType) {

			PaymentSpringsToken token;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (@class != "token") {
						throw new PermissionsException("Id must be a token");
					}

					if (String.IsNullOrWhiteSpace(tokenId)) {
						throw new PermissionsException("Token was empty");
					}

					if (organizationId != caller.Organization.Id) {
						throw new PermissionsException("Organization Ids do not match");
					}

					PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
					if (active) {
						var previous = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.Active == true && x.DeleteTime == null).List().ToList();
						foreach (var p in previous) {
							p.Active = false;
							s.Update(p);
						}
					}

					//CURL
					var client = new HttpClient();

					var keys = new List<KeyValuePair<string, string>>();
					keys.Add(new KeyValuePair<string, string>("token", tokenId));
					keys.Add(new KeyValuePair<string, string>("first_name", caller.GetFirstName()));
					keys.Add(new KeyValuePair<string, string>("last_name", caller.GetLastName()));
					keys.Add(new KeyValuePair<string, string>("company", caller.Organization.GetName()));
					if (address_1 != null) {
						keys.Add(new KeyValuePair<string, string>("address_1", address_1));
					}

					if (address_2 != null) {
						keys.Add(new KeyValuePair<string, string>("address_2", address_2));
					}

					if (city != null) {
						keys.Add(new KeyValuePair<string, string>("city", city));
					}

					if (state != null) {
						keys.Add(new KeyValuePair<string, string>("state", state));
					}

					if (zip != null) {
						keys.Add(new KeyValuePair<string, string>("zip", zip));
					}

					if (phone != null) {
						keys.Add(new KeyValuePair<string, string>("phone", phone));
					}
					//if (fax != null)
					//    keys.Add(new KeyValuePair<string, string>("fax", fax));
					if (website != null) {
						keys.Add(new KeyValuePair<string, string>("website", website));
					}

					if (country != null) {
						keys.Add(new KeyValuePair<string, string>("country", country));
					}

					if (email != null) {
						keys.Add(new KeyValuePair<string, string>("email", email));
					}


					// Create the HttpContent for the form to be posted.
					var requestContent = new FormUrlEncodedContent(keys.ToArray());
					try {

						//Do not supress
						var privateApi = Config.PaymentSpring_PrivateKey();

						var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

						//added
						ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

						HttpResponseMessage response = await client.PostAsync("https://api.paymentspring.com/api/v1/customers", requestContent);
						HttpContent responseContent = response.Content;
						using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
							var result = await reader.ReadToEndAsync();
							if (Json.Decode(result).errors != null) {
								var builder = new List<string>();
								for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
									builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
								}
								throw new PermissionsException(String.Join(" ", builder));
							}
							if (Json.Decode(result).@class != "customer") {
								throw new PermissionsException("Expected class: 'Customer'");
							}

							token = new PaymentSpringsToken() {
								CustomerToken = Json.Decode(result).id,
								CardLast4 = cardLast4,
								CardOwner = cardOwnerName,
								CardType = cardType,
								MonthExpire = cardExpireMonth,
								YearExpire = cardExpireYear,
								OrganizationId = organizationId,
								Active = active,
								ReceiptEmail = email,
								CreatedBy = caller.Id,

								TokenType = tokenType,
								BankAccountLast4 = bankLast4,
								BankRouting = bankRouting,
								BankFirstName = bankFirstName,
								BankLastName = bankLastName,
								BankAccountType = bankAccountType,

								Address_1 = address_1,
								Address_2 = address_2,
								City = city,
								State = state,
								Zip = zip,
								Phone = phone,
								Website = website,
								Country = country,

							};
							s.Save(token);
							tx.Commit();
							s.Flush();
						}
					} catch (Exception) {
						throw;
					}
				}
				using (var ss = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await EventUtil.Trigger(x => x.Create(ss, EventType.PaymentEntered, caller, token, "Added " + tokenType));
						tx.Commit();
						s.Flush();
					}
				}
				using (var ss = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await HooksRegistry.Each<IPaymentHook>((ses, x) => x.UpdateCard(ses, token));
						tx.Commit();
						s.Flush();
					}
				}

				return new PaymentMethodVM(token);
			}
		}

		public static IEnumerable<PaymentCredit> GetCredits(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCredits(s, perms, organizationId);
				}
			}
		}

		public static IEnumerable<PaymentCredit> GetCredits(ISession s, PermissionsUtility perms, long organizationId) {
			perms.CanView(PermItem.ResourceType.UpdatePaymentForOrganization, organizationId);
			return s.QueryOver<PaymentCredit>().Where(x => x.OrgId == organizationId && x.DeleteTime == null).List().ToList();
		}

		#endregion
		#region Charge
		public static async Task<string> EnqueueChargeOrganizationFromTask(long organizationId, long taskId, bool forceUseTest = false, bool sendReceipt = true, DateTime? executeTime = null) {
			string jobId = "not-started";
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(organizationId);
					if (org == null) {
						throw new NullReferenceException("Organization does not exist");
					}

					if (org.DeleteTime != null) {
						throw new FallthroughException("Organization was deleted.");
					}

					var plan = org.PaymentPlan;
					if (plan.Task == null) {
						throw new PermissionsException("Task was null.");
					}

					if (plan.Task.OriginalTaskId == 0) {
						throw new PermissionsException("PaymentPlan OriginalTaskId was 0.");
					}

					var task = s.Get<ScheduledTask>(taskId);
					if (task.Executed != null) {
						throw new PermissionsException("Task was already executed.");
					}

					if (task.DeleteTime != null) {
						throw new PermissionsException("Task was deleted.");
					}

					if (task.OriginalTaskId == 0) {
						throw new PermissionsException("ScheduledTask OriginalTaskId was 0.");
					}

					if (plan.Task.OriginalTaskId != task.OriginalTaskId) {
						throw new PermissionsException("ScheduledTask and PaymentPlan do not have the same task.");
					}

					if (task.Started == null) {
						throw new PermissionsException("Task was not started.");
					}

					executeTime = executeTime ?? DateTime.UtcNow.Date;
#pragma warning disable CS0618 // Type or member is obsolete
					jobId = Scheduler.Enqueue(() => new Unsafe().ChargeViaHangfire(organizationId, taskId, forceUseTest, sendReceipt, executeTime.Value));
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
			return jobId;
		}

		public class HangfireChargeResult {
			public PaymentResult PaymentResult { get; set; }
			public bool WasCharged { get; set; }
			public string Message { get; set; }
			public bool HasError { get; set; }
			public bool WasFallthrough { get; set; }
			public bool WasPaymentException { get; set; }

			public HangfireChargeResult(PaymentResult paymentResult, bool wasCharged, bool wasFallthrough, bool hasBreakingError, bool wasPaymentException, string message) {
				WasCharged = wasCharged;
				PaymentResult = paymentResult;
				Message = message;
				HasError = hasBreakingError;
				WasFallthrough = wasFallthrough;
				WasPaymentException = wasPaymentException;
			}
		}
		#endregion

		#region Invoicing/Calculators
		public static async Task<InvoiceModel> CreateInvoice(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime, ItemizedCharge items) {
			var invoice = new InvoiceModel() {
				Organization = org,
				InvoiceDueDate = executeTime.Add(TimespanExtensions.OneMonth()).Date
			};

			if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly)) {
				invoice.ServiceStart = executeTime;
				invoice.ServiceEnd = executeTime.Add(TimespanExtensions.OneMonth()).Date;
			} else {
				throw new PermissionsException("Unhandled Payment Plan");
			}

			s.Save(invoice);

			var invoiceItems = items.ItemizedList.Select(x => new InvoiceItemModel() {
				AmountDue = x.Total(),
				Currency = Currency.USD,
				PricePerItem = x.Price,
				Quantity = x.Quantity,
				Name = x.Name,
				Description = x.Description,
				ForInvoice = invoice,
			}).ToList();

			foreach (var i in invoiceItems) {
				s.Save(i);
			}

			foreach (var user in items.ChargedFor) {
				s.Save(new InvoiceUserItemModel() {
					Email = user.Email,
					InvoiceId = invoice.Id,
					Name = user.Name,
					OrgId = org.Id,
					UserAttachTime = user.AttachTime,
					UserOrganizationId = user.UserOrgId,
					Description = string.Join(",", user.ChargedFor),
					//Description = 
				});
			}


			invoice.InvoiceItems = invoiceItems;
			invoice.AmountDue = invoice.InvoiceItems.Sum(x => x.AmountDue);
			s.Update(invoice);

			await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.InvoiceCreated(s, invoice));

			return invoice;
		}

		[Obsolete("Public for testing only")]
		public static void _ApplyCreditsToInvoice(ISession s, OrganizationModel org, List<Itemized> itemized, List<PaymentCredit> credits) {
			//Apply credits

			var total = itemized.Sum(x => x.Total());

			if (credits.Any(x => x.AmountRemaining > 0) && total > 0) {
				var adjTotal = total;
				var totalCreditsApplied = 0m;
				foreach (var c in credits) {
					if (adjTotal > 0 && c.AmountRemaining > 0) {
						if (c.AmountRemaining >= adjTotal) {
							c.AmountRemaining -= adjTotal;
							totalCreditsApplied += adjTotal;
							s.Update(c);
							adjTotal = 0;
							break;
						} else {
							adjTotal -= c.AmountRemaining;
							totalCreditsApplied += c.AmountRemaining;
							c.AmountRemaining = 0;
							s.Update(c);
						}
					}
				}
				itemized.Add(new Itemized() {
					Name = "Credit",
					Price = -totalCreditsApplied,
					Quantity = 1
				});
			}
		}

		//[Obsolete("Do not use",false)]
		public class UserCalculator {
			public class UQ {
				public long UserOrgId { get; set; }
				public bool? IsRadialAdmin { get; set; }
				public bool IsClient { get; set; }
				public String UserId { get; set; }
				public bool IsRegistered { get; set; }
				public bool EvalOnly { get; set; }
				public string FirstName { get; set; }
				public string LastName { get; set; }
				public string Email { get; set; }
				public DateTime? AttachTime { get; set; }
				public HashSet<string> ChargedFor { get; set; }
				public UQ() {
					ChargedFor = new HashSet<string>();
				}
			}

			public IEnumerable<UQ> AllPeopleList { get; protected set; }
			public PaymentPlan_Monthly Plan { get; protected set; }

			public IEnumerable<UQ> QCUsers { get { return AllPeopleList.Where(x => !x.IsClient); } }
			public IEnumerable<UQ> L10Users { get { return AllPeopleList.Where(x => !x.EvalOnly); } }
			public IEnumerable<UQ> L10UsersToChargeFor { get { return AllPeopleList.Where(x => !x.EvalOnly).Skip(Plan.FirstN_Users_Free); } }

			public int NumberQCUsers { get { return QCUsers.Count(); } }
			public int NumberTotalUsers { get { return AllPeopleList.Count(); } }

			public int NumberQCUsersToChargeFor { get { return NumberQCUsers; } }
			public int NumberL10UsersToChargeFor { get { return Math.Max(0, NumberL10Users - Plan.FirstN_Users_Free); } }

			public int NumberL10Users { get { return L10Users.Count(); } }

			public UserCalculator(ISession s, long orgId, PaymentPlanModel planModel, DateRange range) {
				//throw new NotImplementedException("Use other UserCalculator");
				if (NHibernateUtil.GetClass(planModel) != typeof(PaymentPlan_Monthly)) {
					throw new PermissionsException("Unhandled Payment Plan");
				}

				Plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(planModel);

				if (Plan.OrgId != orgId) {
					throw new Exception("Org Id do not match");
				}

				var rangeStart = range.StartTime;
				var rangeEnd = range.EndTime;

				UserModel u = null;
				TempUserModel tu = null;
				UserOrganizationModel uo = null;
				AllPeopleList = s.QueryOver<UserOrganizationModel>(() => uo)
									.Left.JoinAlias(() => uo.User, () => u)                 ///Existed any time during this range.
									.Left.JoinAlias(() => uo.TempUser, () => tu)
									.Where(() => uo.Organization.Id == orgId && uo.CreateTime <= rangeEnd && (uo.DeleteTime == null || uo.DeleteTime > rangeStart) && !uo.IsRadialAdmin)
									.Select(x => x.Id, x => u.IsRadialAdmin, x => x.IsClient, x => x.User.Id, x => x.EvalOnly, x => u.FirstName, x => u.LastName, x => u.UserName, x => x.EmailAtOrganization, x => x.AttachTime, x => tu.FirstName, x => tu.LastName, x=>tu.Email)
									.List<object[]>()
									.Select(x => new UQ {
										UserOrgId = (long)x[0],
										IsRadialAdmin = (bool?)x[1],
										IsClient = (bool)x[2],
										UserId = (string)x[3],
										IsRegistered = x[3] != null,
										EvalOnly = (bool?)x[4] ?? false,
										FirstName = (string)x[5] ?? (string)x[10],
										LastName = (string)x[6] ?? (string)x[11],
										Email = ((string)x[7]) ?? (string)x[12] ?? ((string)x[8]),
										AttachTime = (x[3] == null) ? ((DateTime?)null) : ((DateTime?)x[9])
									})
									.Where(x => x.IsRadialAdmin == null || (bool)x.IsRadialAdmin == false)
									.ToList();
				if (Plan.NoChargeForClients) {
					AllPeopleList = AllPeopleList.Where(x => x.IsClient == false).ToList();
				}
				if (Plan.NoChargeForUnregisteredUsers) {
					AllPeopleList = AllPeopleList.Where(x => x.IsRegistered).ToList();
				}
			}
		}

		public class ItemizedCharge {
			private UserCalculator calculator;

			public ItemizedCharge() {
			}

			public List<Itemized> ItemizedList { get; set; }
			public List<UserCharge> ChargedFor { get; set; }

			public class UserCharge {
				public string Name { get; set; }
				public string Email { get; set; }
				public long UserOrgId { get; set; }
				public DateTime? AttachTime { get; set; }
				public HashSet<string> ChargedFor { get; set; }
			}

		}

		public class ChargeTypes {
			public static string L10 = "Level 10";
			public static string PeopleTools = "People Tools™";
		}

		public static ItemizedCharge CalculateCharge(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime) {
			var itemized = new List<Itemized>();
			var chargedFor = new List<ItemizedCharge.UserCharge>();

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
					calc.L10Users.ToList().ForEach(x => x.ChargedFor.Add(ChargeTypes.L10/*, plan.L10PricePerPerson * durationMult*/));

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
					calc.QCUsers.ToList().ForEach(x => x.ChargedFor.Add(ChargeTypes.PeopleTools/*, plan.L10PricePerPerson * durationMult*/));
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

				chargedFor.AddRange(calc.AllPeopleList.Select(x => new ItemizedCharge.UserCharge() {
					AttachTime = x.AttachTime,
					ChargedFor = new HashSet<string>(x.ChargedFor.ToList()),
					Email = x.Email,
					Name = string.Join(" ", new[] { x.FirstName, x.LastName }),
					UserOrgId = x.UserOrgId,
				}));

				if (org != null && org.AccountType == AccountType.Cancelled) {
					itemized = new List<Itemized>();
					chargedFor = new List<ItemizedCharge.UserCharge>();
				}

			} else {
				throw new PermissionsException("Unhandled Payment Plan");
			}

			var credits = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id && x.AmountRemaining > 0).List().ToList();
			_ApplyCreditsToInvoice(s, org, itemized, credits);

			return new ItemizedCharge() {
				ItemizedList = itemized,
				ChargedFor = chargedFor
			};
		}
		#endregion

		#region Plan
		public PaymentPlanModel BasicPaymentPlan() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PaymentPlanModel basicPlan = null;
					try {
						basicPlan = s.QueryOver<PaymentPlanModel>().Where(x => x.IsDefault).SingleOrDefault();
					} catch (Exception e) {
						log.Error(e);
					}
					if (basicPlan == null) {
						basicPlan = new PaymentPlanModel() {
							Description = "Employee count model",
							IsDefault = true,
							PlanCreated = DateTime.UtcNow
						};
						s.Save(basicPlan);
						tx.Commit();
						s.Flush();
					}
					return basicPlan;
				}
			}
		}

		[Obsolete("Dont forget to attach to send this through AttachPlan")]
		public static PaymentPlan_Monthly GeneratePlan(PaymentPlanType type, DateTime? now = null, DateTime? trialEnd = null) {
			var now1 = now ?? DateTime.UtcNow;
			var day30 = now1.AddDays(30);
			var day90 = now1.AddDays(90);
			var basePlan = new PaymentPlan_Monthly() {
				FreeUntil = trialEnd ?? day30,
				L10FreeUntil = trialEnd ?? day30,
				ReviewFreeUntil = Math2.Max(day90, trialEnd ?? DateTime.MinValue),
				PlanCreated = now1,
				NoChargeForUnregisteredUsers = true,
			};
			switch (type) {
				case PaymentPlanType.Enterprise_Monthly_March2016:
					basePlan.Description = "Traction® Tools for Enterprise";
					basePlan.BaselinePrice = 500;
					basePlan.L10PricePerPerson = 2;
					basePlan.ReviewPricePerPerson = 2;
					basePlan.FirstN_Users_Free = 45;
					break;
				case PaymentPlanType.Professional_Monthly_March2016:
					basePlan.Description = "Elite Tools Professional";
					basePlan.BaselinePrice = 149;
					basePlan.L10PricePerPerson = 10;
					basePlan.ReviewPricePerPerson = 2;
					basePlan.FirstN_Users_Free = 10;
					break;
				case PaymentPlanType.SelfImplementer_Monthly_March2016:
					basePlan.Description = "Elite Tools Self-Implementer";
					basePlan.BaselinePrice = 199;
					basePlan.L10PricePerPerson = 12;
					basePlan.ReviewPricePerPerson = 3;
					basePlan.FirstN_Users_Free = 10;
					break;
				default:
					throw new ArgumentOutOfRangeException("type", "PaymentPlanType not implemented " + type);
			}
			return basePlan;
		}

		public static PaymentPlanModel AttachPlan(ISession s, OrganizationModel organization, PaymentPlanModel plan) {
			var task = new ScheduledTask() {
				MaxException = 1,
				Url = "/Scheduler/ChargeAccount/" + organization.Id,
				NextSchedule = plan.SchedulerPeriod(),
				Fire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date.AddHours(3)),
				FirstFire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date.AddHours(3)),
				TaskName = plan.TaskName(),
				EmailOnException = true,
			};
			s.Save(task);
			task.OriginalTaskId = task.Id;
			s.Update(task);
			if (plan is PaymentPlan_Monthly) {
				var ppm = (PaymentPlan_Monthly)plan;
				ppm.OrgId = organization.Id;
			}

			plan.Task = task;
			s.Save(plan);
			return plan;
		}

		public static PaymentPlanModel GetPlan(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).Or(x => x.ManagingOrganization(organizationId), x => x.CanView(PermItem.ResourceType.UpdatePaymentForOrganization, organizationId));
					var org = s.Get<OrganizationModel>(organizationId);

					var plan = s.Get<PaymentPlanModel>(org.PaymentPlan.Id);

					if (plan != null && plan.Task != null) {
						plan._CurrentTask = s.QueryOver<ScheduledTask>()
							.Where(x => x.OriginalTaskId == plan.Task.OriginalTaskId && x.Executed == null)
							.List().FirstOrDefault();
					}


					return (PaymentPlanModel)s.GetSessionImplementation().PersistenceContext.Unproxy(plan);

				}
			}
		}

		public static PaymentPlanType GetPlanType(string planType) {
			switch (planType.Replace("-", "").ToLower()) {
				case "professional":
					return PaymentPlanType.Professional_Monthly_March2016;
				case "enterprise":
					return PaymentPlanType.Enterprise_Monthly_March2016;
				case "selfimplementer":
					return PaymentPlanType.SelfImplementer_Monthly_March2016;
				default:
					throw new ArgumentOutOfRangeException("Cannot create Payment Plan (" + planType + ")");
			}
		}

		#endregion

		#region Unsafe Methods
		[Obsolete("Unsafe")]
		public class Unsafe : IChargeViaHangfire {
			public class ChargeResult {
				public InvoiceModel Invoice { get; set; }
				public PaymentResult Result { get; set; }
			}

			//[Obsolete("Unsafe")]
			public static async Task<ChargeResult> ChargeOrganization_Unsafe(ISession s, OrganizationModel org, PaymentPlanModel plan, DateTime executeTime, bool forceUseTest, bool firstAttempt) {
				try {
					var o = new ChargeResult();
					var itemizedCharge = CalculateCharge(s, org, plan, executeTime);
					o.Invoice = await CreateInvoice(s, org, plan, executeTime, itemizedCharge);
#pragma warning disable CS0618 // Type or member is obsolete
					o.Result = await ExecuteInvoice(s, o.Invoice, forceUseTest);
#pragma warning restore CS0618 // Type or member is obsolete
					return o;
				} catch (PaymentException e) {
					await HooksRegistry.Each<IPaymentHook>((ses, x) => x.PaymentFailedCaptured(ses, org.Id, executeTime, e, firstAttempt));
					throw;
				} catch (Exception e) {
					if (!(e is FallthroughException)) {
						await HooksRegistry.Each<IPaymentHook>((ses, x) => x.PaymentFailedUncaptured(ses, org.Id, executeTime, e.Message, firstAttempt));
					}

					throw;
				}
			}

			//[Obsolete("Unsafe")]
			public static async Task<bool> EmailInvoice(string emailAddress, InvoiceModel invoice, DateTime chargeTime) {
				var ProductName = Config.ProductName(invoice.Organization);
				var SupportEmail = ProductStrings.SupportEmail;
				var OrgName = invoice.Organization.GetName();
				var Charged = invoice.AmountDue;
				//var CardLast4 = result.card_number ?? "NA";
				//var TransactionId = result.id ?? "NA";
				var ChargeTime = chargeTime;
				var ServiceThroughDate = invoice.ServiceEnd.ToString("yyyy-MM-dd");
				var Address = ProductStrings.Address;

				var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime);
				var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt") + " " + invoice.Organization.GetTimeZoneId(localChargeTime);

				var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
				if (emailAddress != null) {
					email = email.AddBcc(emailAddress);
				}
				var toSend = email.SubjectPlainText("[" + ProductName + "] Invoice for " + invoice.Organization.GetName())
					//[ProductName, SupportEmail, OrgName, Charged, CardLast4, TransactionId, ChargeTime, ServiceThroughDate, Address]
					.Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("{0:C}", Charged), "", "", lctStr, ServiceThroughDate, Address);
				await Emailer.SendEmail(toSend);
				return true;
			}

			public static async Task<PaymentResult> ExecuteInvoice(ISession s, InvoiceModel invoice, bool useTest = false) {

				if (invoice.PaidTime != null) {
					throw new FallthroughException("Invoice was already paid");
				}

				if (invoice.ForgivenBy != null) {
					throw new FallthroughException("Invoice was forgiven");
				}

				var result = await ChargeOrganizationAmount(s, invoice.Organization.Id, invoice.AmountDue, useTest);

				invoice.TransactionId = result.id;
				invoice.PaidTime = DateTime.UtcNow;
				s.Update(invoice);

				await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.UpdateInvoice(s, invoice, new IInvoiceUpdates() { PaidStatusChanged = true }));

				return result;
			}
			//[Obsolete("Unsafe")]
			public static async Task<bool> SendReceipt(PaymentResult result, InvoiceModel invoice) {
				if (invoice.PaidTime != null) {
					var ProductName = Config.ProductName(invoice.Organization);
					var SupportEmail = ProductStrings.SupportEmail;
					var OrgName = invoice.Organization.GetName();
					var Charged = invoice.AmountDue;
					var CardLast4 = result.card_number ?? "NA";
					var TransactionId = result.id ?? "NA";
					var ChargeTime = invoice.PaidTime;
					var ServiceThroughDate = invoice.ServiceEnd.ToString("yyyy-MM-dd");
					var Address = ProductStrings.Address;

					var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime.Value);
					var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt") + " " + invoice.Organization.GetTimeZoneId(localChargeTime);

					var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
					if (result.email != null) {
						email = email.AddBcc(result.email);
					}
					var toSend = email.SubjectPlainText("[" + ProductName + "] Payment Receipt for " + invoice.Organization.GetName())
						.Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("{0:C}", Charged), CardLast4, TransactionId, lctStr, ServiceThroughDate, Address);
					await Emailer.SendEmail(toSend);
					return true;
				}
				return false;
			}
			[Obsolete("Use ExecuteInvoice instead.")]
			public static async Task<PaymentResult> ChargeOrganizationAmount(ISession s, long organizationId, decimal amount, bool forceTest = false) {
				if (amount == 0) {
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFree, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "No Charge", arg1: 0m));
					return new PaymentResult() {
						amount_settled = 0,
					};
				}

				var token = PaymentSpringUtil.GetToken(s, organizationId);

				var org2 = s.Get<OrganizationModel>(organizationId);
				if (org2 != null && org2.AccountType == AccountType.Implementer) {
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFree, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Implementer", arg1: 0));
					throw new FallthroughException("Failed to charge implementer account (" + org2.Id + ") " + org2.GetName());
				}
				if (org2 != null && org2.AccountType == AccountType.Dormant) {
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Dormant", arg1: 0));
					throw new FallthroughException("Failed to charge dormant account (" + org2.Id + ") " + org2.GetName());
				}

				if (token == null) {
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "MissingToken", arg1: amount));
					throw new PaymentException(s.Get<OrganizationModel>(organizationId), amount, PaymentExceptionType.MissingToken, "Token missing for " + org2.GetName() + " (" + organizationId + ")");
				}
				PaymentResult pr = null;
				try {
					pr = await PaymentSpringUtil.ChargeToken(org2, token, amount, forceTest);
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentReceived, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Charged", arg1: amount));
				} catch (PaymentException e) {
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "" + e.Type, arg1: amount));
					throw e;
				} catch (Exception e) {
					await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Unhandled:" + e.Message, arg1: amount));
					throw e;
				}

				var org = s.Get<OrganizationModel>(organizationId);
				if (org.AccountType == AccountType.Demo) {
					org.AccountType = AccountType.Paying;
				}

				if (org.PaymentPlan != null) {
					if (org.PaymentPlan.LastExecuted == null) {
						await HooksRegistry.Each<IPaymentHook>((ses, x) => x.FirstSuccessfulCharge(s, token));
					}
					await HooksRegistry.Each<IPaymentHook>((ses, x) => x.SuccessfulCharge(s, token, amount));
					org.PaymentPlan.LastExecuted = DateTime.UtcNow;
				}

				return pr;
			}
			[Obsolete("Do not use. Use ExecuteInvoice instead.")]
			public static async Task<PaymentResult> ChargeOrganizationAmount(long organizationId, decimal amount, bool useTest = false) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var charged = await ChargeOrganizationAmount(s, organizationId, amount, useTest);
						tx.Commit();
						s.Flush();
						return charged;
					}
				}
			}

			//[Obsolete("Unsafe", false)]
			public static List<long> GetPayingOrganizations(ISession s) {
				var scheduledToPay = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ScheduledTask.MonthlyPaymentPlan && x.DeleteTime == null && x.Executed == null)
					.List().ToList().Select(x => x.Url.Split('/').Last().ToLong());
				var hasTokens = s.QueryOver<PaymentSpringsToken>().Where(x => x.Active && x.DeleteTime == null).List().ToList();

				var hasTokens_scheduledToPay = hasTokens.Select(x => x.OrganizationId).Intersect(scheduledToPay);
				return hasTokens_scheduledToPay.ToList();
			}
			//[Obsolete("Unsafe", false)]
			public static decimal CalculateTotalCharge(ISession s, List<long> orgIds) {
				var orgs = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Organization.Id).IsIn(orgIds.Distinct().ToList()).List().ToList();

				return orgs.Sum(o =>
						CalculateCharge(s, o, o.PaymentPlan, DateTime.UtcNow)
							.ItemizedList
							.Sum(x => x.Total())
					);
			}
			public static async Task RecordCapturedPaymentException(PaymentException capturedPaymentException, long taskId) {
				try {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							s.Save(PaymentErrorLog.Create(capturedPaymentException, taskId));
							tx.Commit();
							s.Flush();
						}
					}
				} catch (Exception e) {
					log.Error("FatalPaymentException [A]~(task:" + taskId + ")", e);
				}
				log.Error("PaymentException~(task:" + taskId + ")", capturedPaymentException);

				try {
					var orgName = capturedPaymentException.OrganizationName + "(" + capturedPaymentException.OrganizationId + ")";
					var trace = capturedPaymentException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
					var email = Mail.To(EmailTypes.PaymentException, ProductStrings.PaymentExceptionEmail)
						.Subject(EmailStrings.PaymentException_Subject, orgName)
						.Body(EmailStrings.PaymentException_Body, capturedPaymentException.Message, "<b>" + capturedPaymentException.Type + "</b> for '" + orgName + "'  ($" + capturedPaymentException.ChargeAmount + ") at " + capturedPaymentException.OccurredAt + " [TaskId=" + taskId + "]", trace);

					await Emailer.SendEmail(email, true);
				} catch (Exception e) {
					log.Error("FatalPaymentException [B]~(task:" + taskId + ")", e);
				}
			}
			public static async Task RecordUnknownPaymentException(Exception capturedException, long orgId, long taskId) {
				log.Error("Exception during Payment~(org:" + orgId + ", task:" + taskId + ")", capturedException);
				try {
					var trace = capturedException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
					var email = Mail.To(EmailTypes.PaymentException, ProductStrings.ErrorEmail)
						.Subject(EmailStrings.PaymentException_Subject, "{Non-payment exception}")
						.Body(EmailStrings.PaymentException_Body, capturedException.NotNull(x => x.Message), "{Non-payment}", trace, "[OrgId=" + orgId + "] --  [TaskId=" + taskId + "]");
					await Emailer.SendEmail(email, true);
				} catch (Exception e) {
					log.Error("FatalPaymentException [C]~(org:" + orgId + ", task:" + taskId + ")", e);
				}
			}

			/*
			 * There are no tests to ensure that this task is unstarted. Tests were performed in the EnqueueChargeOrganizationFromTask method above			  
			 */
			[Obsolete("Must call with Enqueue. Cannot be run again through ScheduledTask (task is marked complete), cannot be called inside a session. Calling a second time will charge a second time.")]
			[Queue(HangfireQueues.Immediate.CHARGE_ACCOUNT_VIA_HANGFIRE)]
			[AutomaticRetry(Attempts = 0)]
			public async Task<HangfireChargeResult> ChargeViaHangfire(long organizationId, long unverified_taskId, bool forceUseTest, bool sendReceipt, DateTime executeTime) {
				PaymentResult result = null;
				ChargeResult chargeResult = null;
				try {
					log.Info("ChargingOrganization(" + organizationId + ")");
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var org = s.Get<OrganizationModel>(organizationId);
							var plan = org.PaymentPlan;
							try {
								chargeResult = await ChargeOrganization_Unsafe(s, org, plan, executeTime, forceUseTest, true);
								result = chargeResult.Result;
							} finally {
								tx.Commit();
								s.Flush();
							}
						}
					}
					if (sendReceipt) {
						await SendReceipt(result, chargeResult.Invoice);
					}
					log.Info("ChargedOrganization(" + organizationId + ")");
					return new HangfireChargeResult(result, true, false, false, false, "charged");
				} catch (PaymentException capturedPaymentException) {
					await RecordCapturedPaymentException(capturedPaymentException, unverified_taskId);
					//Saved exception.. stop execution
					throw;
					//return new HangfireChargeResult(null, false, false, true, true, "" + capturedPaymentException.Type);
				} catch (FallthroughException e) {
					log.Error("FallthroughCaptured", e);
					//It's a fallthrough, stop execution
					return new HangfireChargeResult(null, false, true, false, true, e.NotNull(x => x.Message) ?? "Exception was null");
				} catch (Exception capturedException) {
					await RecordUnknownPaymentException(capturedException, organizationId, unverified_taskId);
					//Email send.. stop execution.
					throw;
					//return new HangfireChargeResult(null, false, false, true, false, capturedException.NotNull(x => x.Message) ?? "-no message-");
				}
			}



		}
		#endregion
		#region Test Methods
		public class Test {
			public enum TestCardType : long {
				Visa = 4111111111111111L,
				Amex = 345829002709133L,
				Discover = 6011010948700474L,
				Mastercard = 5499740000000057L,
			}
			public static async Task<PaymentTokenVM> GenerateFakeCard(string owner = "John Doe", TestCardType cardType = TestCardType.Visa) {

				var url = "https://api.paymentspring.com/api/v1/tokens";

				var csc = "999";
				if (cardType == TestCardType.Amex) {
					csc = csc + "7";
				}

				var client = new HttpClient();

				var keys = new List<KeyValuePair<string, string>>();
				keys.Add(new KeyValuePair<string, string>("card_number", "" + (long)cardType));
				keys.Add(new KeyValuePair<string, string>("card_exp_month", "08"));
				keys.Add(new KeyValuePair<string, string>("card_exp_year", "2022"));
				keys.Add(new KeyValuePair<string, string>("csc", csc));

				keys.Add(new KeyValuePair<string, string>("card_owner_name", owner));


				// Create the HttpContent for the form to be posted.
				var requestContent = new FormUrlEncodedContent(keys.ToArray());

				var publicApi = Config.PaymentSpring_PublicKey(true);
				var byteArray = new UTF8Encoding().GetBytes(publicApi + ":");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

				//added
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				HttpResponseMessage response = await client.PostAsync(url, requestContent);
				HttpContent responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
					var result = await reader.ReadToEndAsync();
					if (Json.Decode(result).errors != null) {
						var builder = new List<string>();
						for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
							builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
						}
						throw new PermissionsException(String.Join(" ", builder));
					}
					var r = JsonConvert.DeserializeObject<PaymentTokenVM>(result);
					if (r.@class != "token") {
						throw new PermissionsException("Id must be a token");
					}

					if (String.IsNullOrWhiteSpace(r.id)) {
						throw new PermissionsException("Token was empty");
					}

					if (r.card_owner_name != owner) {
						throw new PermissionsException("Owner incorrect");
					}

					return r;
				}
			}
		}

		public interface IChargeViaHangfire {
			//Change inteface with care. this will break outstanding jobs.
			Task<HangfireChargeResult> ChargeViaHangfire(long organizationId, long unchecked_taskId, bool forceUseTest, bool sendReceipt, DateTime executeTime);
		}
		#endregion

	}

}
