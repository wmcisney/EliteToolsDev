using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.PermissionsListers {
	public class NameIdPermissions {
		public NameIdPermissions(long id,string name, bool canView, bool canEdit, bool canAdmin) {
			Id = id;
			CanView = canView;
			CanEdit = canEdit;
			CanAdmin = canAdmin;
			Name = name;
		}

		public NameIdPermissions(PermItem permItem)
			: this(permItem.ResId,null, permItem.CanView, permItem.CanEdit, permItem.CanAdmin) {
		}

		public long Id { get; set; }
		public bool CanView { get; set; }
		public bool CanEdit { get; set; }
		public bool CanAdmin { get; set; }
		public string Name { get; set; }
	}

	public class L10PermissionsHelper {

		public static List<NameIdPermissions> GetL10RecurrencesAndPermissionsForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var recurrencePermIds = L10PermissionsHelper.GetL10RecurrencesAndPermissionsForUser(s, perms, userId);
					return recurrencePermIds.ToList();
				}
			}
		}

		public static List<NameIdPermissions> GetL10RecurrencesAndPermissionsForUser(ISession s, PermissionsUtility perms, long userId) {
			perms.ViewUserOrganization(userId, false);
			var user = s.Get<UserOrganizationModel>(userId);
			var recurrencePermIds = new List<NameIdPermissions>();

			//QUERIES:
			var attendingRecurrenceIdsF = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null && x.User.Id == userId)
				.Select(x => x.L10Recurrence.Id)
				.Future<long>();

			var aliveMeetingsF = s.QueryOver<L10Recurrence>()
									.Where(x => x.OrganizationId == user.Organization.Id && x.DeleteTime == null && !x.Pristine)
									.Select(x => x.Id, x => x.Name)
									.Future<object[]>()
									.Select(x => new {
										Id = (long)x[0],
										Name = (string)x[1]
									});

			var orgMeetingPermItemsF = s.QueryOver<PermItem>()
					.Where(x => x.DeleteTime == null &&
								x.ResType == PermItem.ResourceType.L10Recurrence &&
								x.OrganizationId == user.Organization.Id
					).Future();

			//RESOLVED:
			var myRgms = ResponsibilitiesAccessor.GetResponsibilityGroupIdsForUser(s, perms, userId);
			var attendingRecurrenceIds = attendingRecurrenceIdsF.ToList();
			var orgMeetingPermItems = orgMeetingPermItemsF.ToList();
			var aliveMeetings = aliveMeetingsF.ToDictionary(x=>x.Id,x=>x.Name);

			//Where OrgAdmin can admin
			if (user.IsManagingOrganization()) {
				var superAdminRecurIds = orgMeetingPermItems
											.Where(x => x.AccessorType == PermItem.AccessType.Admins)
											.Select(x => new NameIdPermissions(x))
											.ToList();
				recurrencePermIds.AddRange(superAdminRecurIds);
			}


			//Where Creator can admin
			var adminableCreatedRecurIds = orgMeetingPermItems
				.Where(x => x.AccessorType == PermItem.AccessType.Creator && x.AccessorId == userId)
				.Select(x => new NameIdPermissions(x))
				.ToList();

			recurrencePermIds.AddRange(adminableCreatedRecurIds);

			//Where Member can admin
			var adminableMemberRecurIds = orgMeetingPermItems
				.Where(x => x.AccessorType == PermItem.AccessType.Members) //Members can admin
				.Where(x => attendingRecurrenceIds.Contains(x.ResId))//We are a member
				.Select(x => new NameIdPermissions(x))
				.ToList();
			recurrencePermIds.AddRange(adminableMemberRecurIds);


			//Where RGM can admin
			//(User, Teams, Positions)
			var myUserAdminRecurIds = orgMeetingPermItems
				.Where(x => myRgms.Contains(x.AccessorId) && x.AccessorType == PermItem.AccessType.RGM)
				.Select(x => new NameIdPermissions(x))
				.ToList();
			recurrencePermIds.AddRange(myUserAdminRecurIds);

			//Where Email can admin
			var allEmailPerms = orgMeetingPermItems.Where(x => x.AccessorType == PermItem.AccessType.Email).ToList();
			if (user.User != null && user.User.UserName != null && allEmailPerms.Any()) {

				var myEmailPermsIds = s.QueryOver<EmailPermItem>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Id).IsIn(allEmailPerms.Select(x => x.AccessorId).ToArray())
					.List().ToList()
					.Where(x => x.Email != null) // Make sure it's valid
					.Where(x => x.Email.ToLower() == user.User.UserName.ToLower())//matches our email
					.Select(x => x.Id)
					.ToList();

				var myEmailAdminRecurIds = allEmailPerms
					.Where(x => myEmailPermsIds.Contains(x.AccessorId))
					.Select(x => new NameIdPermissions(x))
					.ToList();
				recurrencePermIds.AddRange(myEmailAdminRecurIds);
			}

			//Combine duplicates
			var groupped = recurrencePermIds
								.GroupBy(x => x.Id)
								.Where(x=>aliveMeetings.ContainsKey(x.Key))
								.Select(x => new NameIdPermissions(x.Key, aliveMeetings[x.Key], x.Any(y => y.CanView), x.Any(y => y.CanEdit), x.Any(y => y.CanAdmin)))
								.ToList();

			return groupped.Where(x=>x.CanView || x.CanEdit || x.CanAdmin).ToList();
		}
	}
}