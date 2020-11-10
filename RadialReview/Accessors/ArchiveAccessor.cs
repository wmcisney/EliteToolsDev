using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
	public class ArchiveAccessor {

		public class ArchiveVM {
			public class ArchiveItemVM {

				public long Id { get; set; }
				public DateTime? DeleteTime { get; set; }
				public string DetailsUrl { get; set; }
				public String Name { get; set; }
				public string Owner { get; internal set; }
			}

			public String Title { get; set; }
			public IEnumerable<ArchiveItemVM> Objects { get; set; }
			/// <summary>
			/// {0} is replaced by the Id
			/// </summary>
			public String UndeleteUrl { get; set; }
			public String AuditUrl { get; set; }
		}

		public static ArchiveVM ArchievedRocksForOrganization(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ManagingOrganization(orgId);

					var rocks = s.QueryOver<RockModel>()
						.Where(x => x.DeleteTime != null && x.OrganizationId == orgId)
						.List().ToList();

					var model = new ArchiveVM {
						Title = "Rocks",
						Objects = rocks.Select(x => new ArchiveVM.ArchiveItemVM {
							Name = x.Rock,
							Id = x.Id,
							DeleteTime = x.DeleteTime,
							Owner = x.AccountableUser.NotNull(y => y.GetName()),
							DetailsUrl = "/rocks/pad/" + x.Id + "?readonly=true"
						}).ToList(),
						UndeleteUrl = "/rocks/undelete/{0}",
						AuditUrl = "/audit/rocks/{0}",
					};

					return model;

				}
			}
		}

		public static ArchiveVM ArchievedMeasurablesForOrganization(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ManagingOrganization(orgId);

					var measurables = s.QueryOver<MeasurableModel>()
						.Where(x => x.DeleteTime != null && x.Organization.Id == orgId)
						.List().ToList();

					var model = new ArchiveVM {
						Title = "Measurables",
						Objects = measurables.Select(x => new ArchiveVM.ArchiveItemVM {
							Name = x.Title,
							Id = x.Id,
							DeleteTime = x.DeleteTime,
							Owner = x.AccountableUser.NotNull(y => y.GetName()),
							//DetailsUrl = "/measurable/pad/" + x.Id + "?readonly=true"

						}).ToList(),
						UndeleteUrl = "/measurable/undelete/{0}",
						AuditUrl = "/audit/measurables/{0}"
					};

					return model;

				}
			}
		}
	}
}