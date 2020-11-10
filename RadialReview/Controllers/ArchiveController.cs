using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static RadialReview.Accessors.ArchiveAccessor;

namespace RadialReview.Controllers {
	public class ArchiveController : BaseController {


		[Access(AccessLevel.Radial)]
		public ActionResult Index() {
			return View();
		}


		[Access(AccessLevel.Manager)]
		public ActionResult Users() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//tx.Commit();
					//s.Flush();

					var users = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.DeleteTime != null && x.Organization.Id == user.Organization.Id)
						.List().ToList();

					try {
						foreach (var u in users) {
							var a = u.Cache.LastLogin;
						}
					} catch (Exception e) {
						//opps
					}


					return View(users);

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult L10() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//tx.Commit();
					//s.Flush();

					var l10s = s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime != null && x.Organization.Id == user.Organization.Id)
						.List().ToList();

					return View(l10s.Select(x => new { Name = x.Name, Id = x.Id, DeleteTime = x.DeleteTime }).ToList());

				}
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Measurables() {
			var model = ArchiveAccessor.ArchievedMeasurablesForOrganization(GetUser(), GetUser().Organization.Id);
			return View("Table", model);
		}



		[Access(AccessLevel.UserOrganization)]
		public ActionResult Rocks() {
			var model = ArchiveAccessor.ArchievedRocksForOrganization(GetUser(), GetUser().Organization.Id);
			return View("Table", model);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Roles() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rocks = s.QueryOver<RoleModel>().Where(x => x.DeleteTime != null && x.OrganizationId == user.Organization.Id).List().ToList();
					var model = new ArchiveVM {
						Title = "Roles",
						Objects = rocks.Select(x => new ArchiveVM.ArchiveItemVM { Name = x.Role, Id = x.Id, DeleteTime = x.DeleteTime, Owner = "" }).ToList(),
						UndeleteUrl = "/roles/undelete/{0}",
						AuditUrl = "/audit/roles/{0}"
					};
					return View("Table", model);

				}
			}
		}
	}
}