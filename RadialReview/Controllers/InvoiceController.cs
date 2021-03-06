﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System.Threading.Tasks;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using NHibernate;
using RadialReview.Models.Enums;

namespace RadialReview.Controllers {
	public class InvoiceController : BaseController {
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			return List();
		}

		[Access(AccessLevel.Radial)]
		public async Task<bool> ForceUpdateCard(long id) {
			var caller = GetUser();
			var t = PaymentAccessor.GetCards(GetUser(), id).LastOrDefault(x => x.Active).NotNull(x => x.GetToken());
			if (t != null) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var perms = PermissionsUtility.Create(s, caller);

						await HooksRegistry.Each<IPaymentHook>((ses, x) => x.UpdateCard(ses, t));
						tx.Commit();
						s.Flush();
						return true;
					}
				}
			}
			return false;

		}

		[Access(AccessLevel.UserOrganization)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
		public ActionResult List(long? id = null) {
			AllowAdminsWithoutAudit();
			var orgid = id ?? GetUser().Organization.Id;
			var list = InvoiceAccessor.GetInvoicesForOrganization(GetUser(), orgid);
			ViewBag.OrgId = id;
			return View("List", list);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Details(long id) {
            AllowAdminsWithoutAudit();
            var invoice = InvoiceAccessor.GetInvoice(GetUser(), id).AsList();
			return View(invoice);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult All(long? id = null) {
			var orgid = id ?? GetUser().Organization.Id;
			var list = InvoiceAccessor.GetInvoicesForOrganization(GetUser(), orgid)
							.Select(x=>InvoiceAccessor.GetInvoice(GetUser(),x.Id))
							.ToList();
			ViewBag.OrgId = id;
			
			return View("Details",list);
		}

		[Access(AccessLevel.Radial)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
		public ActionResult Outstanding(AccountType? type=null) {
			var o = InvoiceAccessor.AllOutstanding_Unsafe(GetUser());
			if (type != null) {
				o = o.Where(x => x.Organization.AccountType == type.Value).ToList();
			}

			ViewBag.ShowOrganization = true;
			return View("List", o);
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> Forgive(long id, bool forgive = true) {
			await InvoiceAccessor.Forgive(GetUser(), id, forgive);
			return Json(ResultObject.SilentSuccess(new { id, forgive }), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> MarkPaid(long id, bool paid = true, DateTime? time = null) {
			time = time ?? DateTime.UtcNow;
			await InvoiceAccessor.MarkPaid(GetUser(), id, time.Value, paid);
			return Json(ResultObject.SilentSuccess(new { id, paid, time = paid ? time : null }), JsonRequestBehavior.AllowGet);
		}

	}
}
