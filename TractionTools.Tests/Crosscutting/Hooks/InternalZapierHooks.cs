using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHibernate;
using RadialReview.Crosscutting.Hooks.Internal;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.Crosscutting.Hooks {
	[TestClass]
	public class InternalZapierHooksTests {
		private UserOrganizationModel MockUOM() {
			return new UserOrganizationModel() {
				User = MockUser(),
				Organization = MockOrg(),
				Id = -101,
				ManagingOrganization = true,
				ManagerAtOrganization = true,
				AttachTime = new DateTime(2019, 01, 02),
				CreateTime = new DateTime(2019, 01, 01),
			};

		}

		private UserModel MockUser() {
			return  new UserModel() {
				Id = "guid",
				FirstName = "firstName",
				LastName = "lastName",
				UserName = "e@mail.com",
			};
		}

		private OrganizationModel MockOrg() {
			return new OrganizationModel() {
				Id = -100,
				Name = new LocalizedStringModel() {
					Standard = "orgName"
				}
			};
		}

		private PaymentSpringsToken MockToken() {
			return new PaymentSpringsToken() {
				Id = -102,
				Address_1 = "address_1",
				Address_2 = "address_2",
				Active = true,
				CardOwner ="card owner",
				City = "city",
				State="state",
				Zip	="zip",
				Country="country",
				CardType="cardType",
				OrganizationId = -100,
				MonthExpire = 6,
				YearExpire = 2019,
				ReceiptEmail = "em@il.com",
				TokenType =PaymentSpringTokenType.BankAccount,
				Website ="website.com",
				Phone="867-5309",
			};

		}

		[TestMethod]
		public async Task InternalZapierHooks_AttachUserOrg() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.OnUserOrganizationAttach(null,MockUOM() );
				}
			}
		}

		[TestMethod]
		public async Task InternalZapierHooks_CreateUserOrganization() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.CreateUserOrganization(null, MockUOM());
				}
			}
		}

		[TestMethod]
		public async Task InternalZapierHooks_UpdateUserModel() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.UpdateUserModel(null, MockUser());
				}
			}
		}

		[TestMethod]
		public async Task InternalZapierHooks_CreateOrganization() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.CreateOrganization(null,null, MockOrg());
				}
			}
		}

		[TestMethod]
		public async Task InternalZapierHooks_UpdateOrganization() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					var mock = new Mock<ISession>();
					var org = MockOrg();
					mock.Setup(framework => framework.Get<OrganizationModel>(org.Id)).Returns(org);

					await hook.UpdateOrganization(mock.Object, org.Id,null);
				}
			}
		}

		[TestMethod]
		public async Task InternalZapierHooks_UpdateCard() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.UpdateCard(null, MockToken());
				}
			}
		}
		[TestMethod]
		public async Task InternalZapierHooks_SuccessfulCharge() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.SuccessfulCharge(null, MockToken(), 100);
				}
			}
		}
		[TestMethod]
		public async Task InternalZapierHooks_PaymentFailedUncaptured() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.PaymentFailedUncaptured(null, MockOrg().Id, new DateTime(2019, 06, 06), "payment failed. uncaptured.", true);
				}
			}
		}
		[TestMethod]
		public async Task InternalZapierHooks_PaymentFailedCaptured() {
			var hook = new InternalZapierHooks();
			using (Config.Mock.MockLocal(false)) {
				using (Scheduler.MockAndExecute()) {
					await hook.PaymentFailedCaptured(null, MockOrg().Id, new DateTime(2019, 06, 06),new PaymentException(MockOrg(),1,PaymentExceptionType.MissingToken, "token missing"), true);
				}
			}
		}

	}
}
