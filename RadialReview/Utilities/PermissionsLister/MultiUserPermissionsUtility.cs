using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities {
	public partial class PermissionsUtility {

		public class MultiUserUser {
			public MultiUserUser(long id, long organizationId, bool managerAtOrganization, bool organization_Settings_ManagersCanEditSelf, bool organization_Settings_EmployeesCanEditSelf, bool managingOrganization,
				string firstName, string lastname, string email) {
				Id = id;
				OrganizationId = organizationId;
				ManagerAtOrganization = managerAtOrganization;
				Organization_Settings_ManagersCanEditSelf = organization_Settings_ManagersCanEditSelf;
				Organization_Settings_EmployeesCanEditSelf = organization_Settings_EmployeesCanEditSelf;
				ManagingOrganization = managingOrganization;
				FirstName = firstName;
				LastName = lastname;
				Email = email;
			}

			public long Id { get; set; }
			public long OrganizationId { get; set; }
			public bool ManagerAtOrganization { get; internal set; }
			public bool Organization_Settings_ManagersCanEditSelf { get; internal set; }
			public bool Organization_Settings_EmployeesCanEditSelf { get; internal set; }
			public bool ManagingOrganization { get; internal set; }
			public string FirstName { get; set; }

			public string LastName { get; set; }
			public string Email { get; set; }

			public string GetName() {
				return ((FirstName ?? "") + " " + (LastName ?? "")).Trim();
			}
			public string GetEmail() {
				return (Email ?? "").Trim();
			}
		}

		public class MultiUserContext {
			private MultiUserContext() {

			}

			public static MultiUserContext FromOrganization(ISession s, PermissionsUtility perms, long organizationId/*,bool includeTempUsers=false*/) {
				return new MultiUserContext() {
					_UserOrganizationIds = null,
					SelectedUsers = new Lazy<List<MultiUserUser>>(() => {
						return GenerateUsers(s, null, organizationId, false, true);
					}),
					SubordinateAndSelfIds = new Lazy<List<long>>(() => DeepAccessor.Users.GetSubordinatesAndSelf(s, perms.GetCaller(), perms.GetCaller().Id/*,includeTempUsers: includeTempUsers*/))
				};
			}
			public static MultiUserContext FromUsers(ISession s, PermissionsUtility perms, long[] userOrganizationIds/*, bool includeTempUsers = false*/) {
				return new MultiUserContext() {
					_UserOrganizationIds = userOrganizationIds.Distinct().ToArray(),
					SelectedUsers = new Lazy<List<MultiUserUser>>(() => {
						return GenerateUsers(s, userOrganizationIds.ToList(), -1, true, false);
					}),
					SubordinateAndSelfIds = new Lazy<List<long>>(() => DeepAccessor.Users.GetSubordinatesAndSelf(s, perms.GetCaller(), perms.GetCaller().Id/*, includeTempUsers:includeTempUsers*/)),
				};
			}

			private long[] _UserOrganizationIds { get; set; }
			public long[] GetUserOrganizationIds() {
				if (_UserOrganizationIds == null) {
					_UserOrganizationIds =SelectedUsers.Value.Select(x => x.Id).Distinct().ToArray();
				}
				return _UserOrganizationIds;
			}

			public Lazy<List<MultiUserUser>> SelectedUsers { get; set; }
			public Lazy<List<long>> SubordinateAndSelfIds { get; set; }

			public MultiUserUser GetUser(long id) {
				return SelectedUsers.Value.FirstOrDefault(x => x.Id == id);
			}

			public DefaultDictionary<long, bool> AllTrue() {
				return GetUserOrganizationIds().Distinct().ToDefaultDictionary(x => x, x => true, x => false);

			}

			public static List<MultiUserUser> GenerateUsers(ISession s, List<long> userIds, long orgId, bool filterOnUsers, bool filterOnOrg) {

				if (!filterOnOrg && !filterOnUsers) {
					throw new Exception("A filter is required");
				}

				OrganizationModel orgAlias = null;
				UserModel userAlias = null;
				TempUserModel tempUserAlias = null;
				var q = s.QueryOver<UserOrganizationModel>()
					.JoinAlias(x => x.Organization, () => orgAlias)
					.Left.JoinAlias(x => x.User, () => userAlias)
					.Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
					.Where(x => x.DeleteTime == null);

				if (filterOnUsers) {
					q = q.WhereRestrictionOn(x => x.Id).IsIn(userIds);
				}
				if (filterOnOrg) {
					q = q.Where(x => x.Organization.Id == orgId);
				}

				var list = q.Select(
					x => x.Id,
					x => x.Organization.Id,
					x => x.ManagerAtOrganization,
					x => orgAlias._Settings.ManagersCanEditSelf,
					x => orgAlias._Settings.EmployeesCanEditSelf,
					x => x.ManagingOrganization,
					x => userAlias.FirstName,
					x => userAlias.LastName,
					x => userAlias.UserName,
					x => tempUserAlias.FirstName,
					x => tempUserAlias.LastName,
					x => tempUserAlias.Email
				).List<object[]>();
				return list.Select(x => new MultiUserUser(
					(long)x[0],
					(long)x[1],
					(bool)x[2],
					(bool)x[3],
					(bool)x[4],
					(bool)x[5],
					(string)x[6] ?? (string)x[9],
					(string)x[7] ?? (string)x[10],
					(string)x[8] ?? (string)x[11]
				)).ToList();
			}

		}

		public DefaultDictionary<long, bool> MultiUserManagesUserOrganizations(MultiUserContext cache, bool disableIfSelf) {
			/*
			 * ALSO UPDATE : PermissionsUtility.ManagesUserOrganization
			 */

			if (IsRadialAdmin(caller)) {
				return cache.AllTrue();
			}

			var dict = new DefaultDictionary<long, bool>(x => false);
			foreach (var userOrganizationId in cache.GetUserOrganizationIds()) {
				var user = cache.GetUser(userOrganizationId);

				if (user == null) {
					dict[userOrganizationId] = false;
					continue;
				}

				if (IsManagingOrganization(user.OrganizationId, true)) {
					dict[userOrganizationId] = true;
					continue;
				}

				if (caller.ManagingOrganization) {
					var subordinate = session.Get<UserOrganizationModel>(userOrganizationId);
					if (user != null && user.OrganizationId == caller.Organization.Id) {
						dict[userOrganizationId] = true;
						continue;
					}
				}

				if (disableIfSelf && caller.Id == userOrganizationId) {
					dict[userOrganizationId] = false;
					continue;
				}

				if (caller.IsManager() && cache.SubordinateAndSelfIds.Value.Any(x => x == userOrganizationId)) {
					dict[userOrganizationId] = true;
					continue;
				}

				dict[userOrganizationId] = false;
			}
			return dict;
		}


		public DefaultDictionary<long, bool> MultiUserEditUserDetails(MultiUserContext cache) {
			/*
			 * ALSO UPDATE : PermissionsUtility.EditUserDetails
			 */

			var dict = new DefaultDictionary<long, bool>(x => false);
			var managesUsers = MultiUserManagesUserOrganizations(cache, true);
			foreach (var forUserId in cache.GetUserOrganizationIds()) {
				if (managesUsers[forUserId]) {
					dict[forUserId] = true;
					continue;
				} else {
					var foundUser = cache.GetUser(forUserId);
					if (foundUser.Id == caller.Id && ((foundUser.ManagerAtOrganization && foundUser.Organization_Settings_ManagersCanEditSelf) || foundUser.Organization_Settings_EmployeesCanEditSelf || foundUser.ManagingOrganization)) {
						dict[forUserId] = true;
						continue;
					}
				}
				dict[forUserId] = false;
			}
			return dict;
		}


		public DefaultDictionary<long, bool> MultiUserCanAdminMeetingWithUsers(MultiUserContext cache) {
			/*
			 * ALSO UPDATE : PermissionsUtility.CanAdminMeetingWithUser
			 */

			if (IsRadialAdmin(caller)) {
				return cache.AllTrue();
			}

			var dict = new DefaultDictionary<long, bool>(x => false);
			var editUserDetails = MultiUserEditUserDetails(cache);

			//Get caller's Adminable meetings
			var usersCallerSharesAdminableMeetingsWith = new Lazy<List<long>>(() => {
				var adminRecurrenceIds = GetAdminMeetingForUser(caller.Id).ToList();
				//Get Attendees in these meetings
				return session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(adminRecurrenceIds.ToList())
					.Select(x => x.User.Id)
					.List<long>().ToList();
			});

			foreach (var userId in cache.GetUserOrganizationIds()) {
				var owner = session.Get<UserOrganizationModel>(userId);
				if (owner.Organization.Settings.EmployeesCanEditSelf && IsSelf(userId)) {
					dict[userId] = true;
					continue;
				}

				if (editUserDetails[userId]) {
					dict[userId] = true;
					continue;
				}

				var inAnyAdminMeetings = usersCallerSharesAdminableMeetingsWith.Value.Any(x => x == userId);
				//is user id an attendee?
				if (inAnyAdminMeetings) {
					dict[userId] = true;
					continue;
				}
			}
			return dict;
		}

	}
}
