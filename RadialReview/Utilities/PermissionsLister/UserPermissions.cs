using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.PermissionsListers {
	public class NameIdCreatablePermissions {
		public NameIdCreatablePermissions(long id, string name, bool canCreate) {
			Id = id;
			CanCreate = canCreate;
			Name = name;
		}
		public long Id { get; set; }
		public bool CanCreate { get; set; }
		public string Name { get; set; }
	}
	public class UserPermissionsHelper {


		public static List<NameIdCreatablePermissions> GetUsersWeCanCreateRocksFor(UserOrganizationModel caller, long forUserId, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUsersWeCanCreateMeetingItemsFor(s, perms, forUserId, orgId);
				}
			}
		}

		public static List<NameIdCreatablePermissions> GetUsersWeCanCreateMeasurablesFor(UserOrganizationModel caller, long forUserId, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUsersWeCanCreateMeetingItemsFor(s, perms, forUserId, orgId);
				}
			}
		}

		/// <summary>
		/// Returns a list of userOrgIds that we can create rocks for
		/// </summary>
		/// <returns></returns>
		public static List<NameIdCreatablePermissions> GetUsersWeCanCreateMeetingItemsFor(ISession s, PermissionsUtility perms, long forUserId, long orgId) {
			/*
			 * ALSO UPDATE : PermissionsUtility.CreateRocksForUser
			 */
			perms.Self(forUserId);
			perms.ViewOrganization(orgId);

			//var allUsers = s.QueryOver<UserOrganizationModel>()
			//				.Where(x => x.DeleteTime == null && x.Organization.Id == orgId)
			//				.List().ToList();


			var ctx = PermissionsUtility.MultiUserContext.FromOrganization(s, perms,orgId);



			var allowedIds = perms.MultiUserCanAdminMeetingWithUsers(ctx).Where(x => x.Value).Select(x => x.Key).ToList();
			var visibleIds = allowedIds.ToList();
			visibleIds.AddRange(ctx.SubordinateAndSelfIds.Value);
			var names = ctx.SelectedUsers.Value.ToDefaultDictionary(x => x.Id, x => x.GetName());

			return visibleIds
					.Distinct()
					.Select(x => new NameIdCreatablePermissions(x, names[x], allowedIds.Any(y => y == x)))
					.OrderBy(x => x.Name)
					.ToList();
		}
	}
}