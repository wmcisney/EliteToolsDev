﻿using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Enums;
using RadialReview;
using RadialReview.Models.Accountability;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Reviews;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

	public class PosDur {
		public long UserId { get; set; }
		public long PosId { get; set; }
		public string PosName { get; set; }
		public DateTime _StartTime { get; set; }
		public DateTime? _DeleteTime { get; set; }
	}
	public class TeamDur {
		public long UserId { get; set; }
		public long TeamId { get; set; }
		public string TeamName { get; set; }
		public DateTime _StartTime { get; set; }
		public DateTime? _DeleteTime { get; set; }
	}
	public class RoleAccessor {

		#region GetRoleLinks_Unsafe
		//Update Both GetRoleLinks_Unsafe Methods
		public static List<RoleLink> GetRoleLinks_Unsafe(AbstractQuery queryProvider, long forUserId, DateRange range = null) {

			var teams = queryProvider.Where<TeamDurationModel>(x => x.UserId == forUserId).FilterRange(range);
			var pos = queryProvider.Where<PositionDurationModel>(x => x.UserId == forUserId).FilterRange(range);

			var userRoleLinks = queryProvider.Where<RoleLink>(x => x.AttachType == AttachType.User && x.AttachId == forUserId).ToListAlive();
			var teamRoleLinks = queryProvider.WhereRestrictionOn<RoleLink>(
				x => x.AttachType == AttachType.Team,
				x => x.AttachId,
				teams.Select(x => (object)x.TeamId).ToArray()
			).FilterRange(range);
			var posRoleLinks = queryProvider.WhereRestrictionOn<RoleLink>(
				x => x.AttachType == AttachType.Position,
				x => x.AttachId,
				pos.Select(x => (object)x.Position.Id).ToArray()
			).FilterRange(range);

			var allLinks = new List<RoleLink>();
			allLinks.AddRange(userRoleLinks);
			allLinks.AddRange(teamRoleLinks);
			allLinks.AddRange(posRoleLinks);

			return allLinks;
		}

		public static void UndeleteRole(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditRole(id);
					var r = s.Get<RoleModel>(id);
					r.DeleteTime = null;
					s.Update(r);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<RoleModel> GetRolesForAttach_Unsafe(ISession s, Attach attach, DateRange range = null) {
			var roleIds = s.QueryOver<RoleLink>()
				.Where(range.Filter<RoleLink>())
				.Where(x => x.AttachId == attach.Id && x.AttachType == attach.Type)
				.Select(x => x.RoleId)
				.List<long>().Distinct().ToList();

			return s.QueryOver<RoleModel>()
				.Where(range.Filter<RoleModel>())
				.WhereRestrictionOn(x => x.Id).IsIn(roleIds)
				.List().ToList();
		}

		public static async Task EditRole(UserOrganizationModel caller, long id, string role, DateTime? deleteTime = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditRole(id);
					var r = s.Get<RoleModel>(id);

					var updateSent = false;
					r.Role = role;
					if (r.DeleteTime != deleteTime) {
						r.DeleteTime = deleteTime;
						if (deleteTime == null) {
							await HooksRegistry.Each<IRolesHook>((ses, x) => x.CreateRole(ses, r));
						} else {
							await HooksRegistry.Each<IRolesHook>((ses, x) => x.DeleteRole(ses, r));
						}
						updateSent = true;
					}
					s.Update(r);
					if (!updateSent)
						await HooksRegistry.Each<IRolesHook>((ses, x) => x.UpdateRole(ses, r));




					tx.Commit();
					s.Flush();
				}
			}
		}

		public class RoleLinksQuery {

			public RoleLinksQuery(IEnumerable<RoleModel> roles, IEnumerable<RoleLink> roleLinks, IEnumerable<TeamDurationModel> teams, IEnumerable<PositionDurationModel> positions, DateRange range = null) {
				Teams = teams;
				Positions = positions;
				RoleLinks = roleLinks;
				DateRange = range;
				Roles = roles;
			}

			public IEnumerable<TeamDurationModel> Teams { get; set; }
			public IEnumerable<PositionDurationModel> Positions { get; set; }
			public IEnumerable<RoleLink> RoleLinks { get; set; }
			public IEnumerable<RoleModel> Roles { get; set; }
			public DateRange DateRange { get; set; }

			private IEnumerable<RoleDetails> ConstructRoleDetails(RoleLink link) {
				var role = Roles.Where(x => x.Id == link.RoleId).Where(DateRange.Filter<RoleModel>().Compile()).SingleOrDefault();
				if (role != null)
					yield return new RoleDetails(role, link);
				yield break;
			}

			public IEnumerable<RoleDetails> GetRoleDetailsForUser(long forUserId) {

				var range = DateRange;
				var teams = Teams.Where(x => x.UserId == forUserId).Where(range.Filter<TeamDurationModel>().Compile());// .FilterRange(range);
				var pos = Positions.Where(x => x.UserId == forUserId).Where(range.Filter<PositionDurationModel>().Compile());

				var rangeLinks = RoleLinks.Where(range.Filter<RoleLink>().Compile());

				var userRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.User && x.AttachId == forUserId).SelectMany(ConstructRoleDetails);
				var teamRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.Team && (teams.Any(y => y.TeamId == x.AttachId))).SelectMany(ConstructRoleDetails);
				var posRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.Position && (pos.Any(y => y.Position.Id == x.AttachId))).SelectMany(ConstructRoleDetails);

				return userRoleLinks.Union(teamRoleLinks).Union(posRoleLinks);
			}

			public IEnumerable<RoleLink> GetRoleLinksForUser(long forUserId) {
				return GetRoleDetailsForUser(forUserId).Select(x => x.RoleLink);
			}
			public IEnumerable<RoleModel> GetRolesForUser(long forUserId) {
				return GetRoleDetailsForUser(forUserId).Select(x => x.Role);
			}

			public IEnumerable<RoleDetails> GetRoleDetailsForNode(AccountabilityNode node) {
				var nodeRoleLinks = new List<RoleDetails>();
				var posId = node.AccountabilityRolesGroup.NotNull(x => x.PositionId);
				if (posId != null) {
					nodeRoleLinks.AddRange(RoleLinks.Where(x => x.AttachType == AttachType.Position && x.AttachId == posId).SelectMany(ConstructRoleDetails));
				}

				if (node.User != null) {
					var userId = node.User.Id;
					nodeRoleLinks.AddRange(RoleLinks.Where(x => x.AttachType == AttachType.User && x.AttachId == userId).SelectMany(ConstructRoleDetails));
					var teamIds = Teams.Where(x => x.UserId == userId).Select(x => x.TeamId).ToArray();
					nodeRoleLinks.AddRange(RoleLinks.Where(x => x.AttachType == AttachType.Team && teamIds.Any(y => y == x.AttachId)).SelectMany(ConstructRoleDetails));
				}
				return nodeRoleLinks;
			}

			public class RoleDetails {
				public RoleDetails(RoleModel role, RoleLink roleLink) {
					Role = role;
					RoleLink = roleLink;
				}
				public RoleModel Role { get; set; }
				public RoleLink RoleLink { get; set; }
				public Attach RoleComesFrom { get { return RoleLink.GetAttach(); } }
			}


		}

		//Update Both GetRoleLinks_Unsafe Methods
		public static List<RoleLink> GetRoleLinksForUser_Unsafe(ISession s, long forUserId, DateRange range = null) {
			var teams = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == forUserId).Where(range.Filter<TeamDurationModel>()).Future();// .FilterRange(range);
			var pos = s.QueryOver<PositionDurationModel>().Where(x => x.UserId == forUserId).Where(range.Filter<PositionDurationModel>()).Future();
			var userRoleLinks = s.QueryOver<RoleLink>().Where(x => x.AttachType == AttachType.User && x.AttachId == forUserId).Where(range.Filter<RoleLink>()).Future();
			var teamRoleLinks = s.QueryOver<RoleLink>().Where(x => x.AttachType == AttachType.Team).Where(range.Filter<RoleLink>())
				.WhereRestrictionOn(x => x.AttachId).IsIn(teams.Select(x => x.TeamId).ToLazyCollection()).Future();
			var posRoleLinks = s.QueryOver<RoleLink>().Where(x => x.AttachType == AttachType.Position).Where(range.Filter<RoleLink>())
				.WhereRestrictionOn(x => x.AttachId).IsIn(pos.Select(x => x.Position.Id).ToLazyCollection()).Future();

			//return userRoleLinks.Union(teamRoleLinks).Union(posRoleLinks);

			var allLinks = new List<RoleLink>();
			allLinks.AddRange(userRoleLinks);
			allLinks.AddRange(teamRoleLinks);
			allLinks.AddRange(posRoleLinks);

			return allLinks;
		}
		public static RoleLinksQuery GetRolesForOrganization_Unsafe(ISession s, long organizationId, DateRange range = null) {

			//THIS SHOULD ALL BE LAZY..
			var teams = s.QueryOver<TeamDurationModel>().Where(x => x.OrganizationId == organizationId).Where(range.Filter<TeamDurationModel>()).Future();
			var pos = s.QueryOver<PositionDurationModel>().Where(x => x.OrganizationId == organizationId).Where(range.Filter<PositionDurationModel>()).Future();
			var roleLinks = s.QueryOver<RoleLink>().Where(x => x.OrganizationId == organizationId).Where(range.Filter<RoleLink>()).Future();
			var roles = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == organizationId).Where(range.Filter<RoleModel>()).Future();

			return new RoleLinksQuery(roles, roleLinks, teams, pos, range);

			//return userRoleLinks;

		}
		#endregion

		#region GetRoles
		public List<RoleModel> GetRoles(UserOrganizationModel caller, long userId, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetRoles(s, perms, userId, range);
				}
			}
		}
		//Update Both GetRoles Methods
		public static List<RoleModel> GetRoles(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId, DateRange range = null) {
			perms.ViewUserOrganization(forUserId, false);
			var allLinks = GetRoleLinks_Unsafe(queryProvider, forUserId, range);
			return queryProvider.WhereRestrictionOn<RoleModel>(null, x => x.Id, allLinks.Select(x => x.RoleId).Distinct().Cast<object>())
				.FilterRange(range)
				.ToList();
		}

		public static List<RoleModel> GetRolesForReviewee(AbstractQuery queryProvider, PermissionsUtility perms, Reviewee revieweeUser, DateRange range = null) {
			var forUserId = revieweeUser.RGMId;
			if (revieweeUser.ACNodeId == null)
				return GetRoles(queryProvider, perms, revieweeUser.RGMId, range);
			else {
				return GetRolesForAcNode(queryProvider, perms, revieweeUser.ACNodeId.Value, range)
							.OrderBy(x => x.Ordering)
							.Select(x => x.Role)
							.ToList();
			}
		}



		//Update Both GetRoles Methods
		public static List<RoleModel> GetRoles(ISession s, PermissionsUtility perms, long forUserId, DateRange range = null) {
			perms.ViewUserOrganization(forUserId, false);
			var allLinks = GetRoleLinksForUser_Unsafe(s, forUserId, range);
			var roles = s.QueryOver<RoleModel>().WhereRestrictionOn(x => x.Id).IsIn(allLinks.Select(x => x.RoleId).Distinct().ToArray())
				.Where(range.Filter<RoleModel>()).List().ToList();

			roles.ForEach(x => {
				var link = allLinks.FirstOrDefault(y => y.RoleId == x.Id);
				if (link != null)
					x._Attach = new Attach(link.AttachType, link.AttachId);
			});
			return roles;

		}

		public static RoleModel GetRoleById(UserOrganizationModel caller, long roleId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				var perms = PermissionsUtility.Create(s, caller);
				return GetRolesById(s, caller, perms, roleId);
			}
		}

		public static RoleModel GetRolesById(ISession s, UserOrganizationModel caller, PermissionsUtility perms, long roleId) {
			var roles = s.Get<RoleModel>(roleId);
			perms.ViewOrganization(roles.OrganizationId);

			if (roles.DeleteTime != null) {
				throw new PermissionsException("Cannot view role.");
			}

			return roles;
		}

		#endregion


		[Untested("Hooks")]
		public async Task EditRoles(UserOrganizationModel caller, long userId, List<RoleModel> roles, bool updateOutstanding) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
					var existingRoles = GetRoles(s, perms, userId);
					var existingLinks = GetRoleLinksForUser_Unsafe(s, userId);

					if (!roles.All(x => x.Id == 0 || existingRoles.Any(y => y.Id == x.Id)))
						throw new PermissionsException("Role cannot be edited.");


					//if (roles.Any(x => x.ForUserId != userId))
					//	throw new PermissionsException("Role UserId does not match UserId");


					//if (roles.Any(x => x.ForUserId != userId))
					// throw new PermissionsException("Role UserId does not match UserId");

					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					var outstanding = ReviewAccessor.OutstandingReviewsForOrganization_Unsafe(s, orgId);




					foreach (var r in roles) {
						r.Category = category;
						r.OrganizationId = orgId;

						if (r.Id == 0) {
							//Role is new
						} else {
							var old = existingRoles.FirstOrDefault(x => x.Id == r.Id);
							if (old == null)
								throw new PermissionsException("Could not find role: '" + r.Role + "'.");

							if (r.Role != old.Role) {
								var links = existingLinks.Where(x => x.RoleId == r.Id);
								if (!links.Any(x => x.AttachType == AttachType.User && x.AttachId == userId)) {
									if (!perms.IsPermitted(x => x.EditRole(r.Id))) {
										var err = "Role does not belong to user: '" + r.Role + "'.";
										err += links.FirstOrDefault().NotNull(x => "It belongs to a " + x.AttachType) ?? "";
										throw new PermissionsException(err);
									}
								}
							}

						}

						var added = r.Id == 0;
						if (added) {
							s.Save(r);
						} else {
							s.Merge(r);
						}

						if (added) {
							var link = new RoleLink() {
								AttachId = userId,
								AttachType = AttachType.User,
								OrganizationId = orgId,
								RoleId = r.Id,
								CreateTime = r.CreateTime
							};
							s.Save(link);
							await HooksRegistry.Each<IRolesHook>((ses, x) => x.CreateRole(ses, r));
						} else {
							await HooksRegistry.Each<IRolesHook>((ses, x) => x.UpdateRole(ses, r));
						}

						if (updateOutstanding && added) {
							foreach (var o in outstanding) {
								ReviewAccessor.AddResponsibilityAboutUserToReview(s, perms, o.Id, new Reviewee(userId, null), r.Id);
							}
						}
					}

					//user.NumRoles = roles.Count(x => x.DeleteTime == null);
					s.SaveOrUpdate(user);
					s.Flush();
					s.GetFresh<UserOrganizationModel>(user.Id).UpdateCache(s);


					tx.Commit();
					s.Flush();
				}
			}
		}

		public static int CountRoles(ISession s, long userId) {
			return GetRoleLinksForUser_Unsafe(s, userId).Distinct(x => x.RoleId).Count();
		}


		public static List<RoleGroupRole> GetRolesForAcNode(AbstractQuery queryProvider, PermissionsUtility perms, long acNodeId, DateRange range = null) {

			var node = queryProvider.Get<AccountabilityNode>(acNodeId);
			perms.ViewOrganization(node.OrganizationId);


			var teamDur = new List<TeamDurationModel>();
			var allRoleLinks = new List<RoleLink>();
			if (node.UserId != null) {
				perms.ViewUserOrganization(node.UserId.Value, false);
				teamDur = queryProvider.Where<TeamDurationModel>(x =>
					x.OrganizationId == node.OrganizationId && x.UserId == node.UserId
				).FilterRange(range).ToList();

				var userRoleLinks = queryProvider.Where<RoleLink>(x => x.OrganizationId == node.OrganizationId && x.AttachType == AttachType.User && x.AttachId == node.UserId).FilterRange(range).ToList();
				allRoleLinks.AddRange(userRoleLinks);
			}

			var posDur = new List<PositionDurationModel>();
			if (node.AccountabilityRolesGroup.PositionId != null) {
				posDur = queryProvider.Where<PositionDurationModel>(x =>
						x.OrganizationId == node.OrganizationId && x.Position.Id == node.AccountabilityRolesGroup.PositionId
					).FilterRange(range).ToList();
			}

			if (teamDur.Any()) {
				var teamsRoleLinks = queryProvider.WhereRestrictionOn<RoleLink>(x => x.OrganizationId == node.OrganizationId && x.AttachType == AttachType.Team, x => x.AttachId, teamDur.Select(x => x.TeamId).Cast<object>()).FilterRange(range).ToList();
				allRoleLinks.AddRange(teamsRoleLinks);
			}
			if (posDur.Any()) {
				var positionsRoleLinks = queryProvider.WhereRestrictionOn<RoleLink>(x => x.OrganizationId == node.OrganizationId && x.AttachType == AttachType.Position, x => x.AttachId, posDur.Select(x => x.Position.Id).Cast<object>()).FilterRange(range).ToList();
				allRoleLinks.AddRange(positionsRoleLinks);
			}


			var allRoles = queryProvider.WhereRestrictionOn<RoleModel>(x => x.OrganizationId == node.OrganizationId, x => x.Id, allRoleLinks.Select(x => x.RoleId).Cast<object>())
				.FilterRange(range).Distinct(x => x.Id).ToList();


			var roleLU = allRoles.ToDictionary(x => x.Id, x => x);

			var pd = posDur.Select(x => new PosDur() {
				PosName = x.Position.GetName(),
				PosId = x.Position.Id,
				UserId = x.UserId
			}).ToList();

			var td = teamDur.Select(x => new TeamDur() {
				TeamName = x.Team.GetName(),
				TeamId = x.TeamId,
				UserId = x.UserId
			}).ToList();


			return ConstructRolesForNode(node.UserId, node.AccountabilityRolesGroup.PositionId, roleLU, allRoleLinks, pd, td).SelectMany(x => x.Roles).ToList();


			//return queryProvider.WhereRestrictionOn<RoleModel>(null, x => x.Id, allLinks.Select(x => x.RoleId).Distinct().Cast<object>())
			//	.FilterRange(range)
			//	.ToList();
		}

		public static List<RoleGroup> ConstructRolesForNode(long? userId, long? positionId, Dictionary<long, RoleModel> rolesLU,
			List<RoleLink> links, List<PosDur> pd, List<TeamDur> td) {

			var relaventPD = pd.Where(x => x.PosId == positionId).ToList();
			//relaventPD.AddRange(pd.Where(x => x.PosId == positionId));


			var relaventTD = td.Where(x => x.UserId == userId).ToList();

			var relaventGroups = new List<RoleGroup>();
			if (userId != null) {
				var userRoleLinks = links.Where(x => x.AttachType == AttachType.User && x.AttachId == userId);
				var userRoles = userRoleLinks.Select(x => new RoleGroupRole(rolesLU.GetOrDefault(x.RoleId, null),x.Ordering)).Where(x => x != null && x.Role!=null).ToList();
				if (userRoles.Any())
					relaventGroups.Add(new RoleGroup(userRoles, userId.Value, AttachType.User, "User"));

				//relaventGroups.AddRange(userRoles.SelectMany(x => {
				//	if (!rolesLU.ContainsKey(x.RoleId))
				//		return new List<RoleGroup>();
				//	return new RoleGroup(, x.AttachId.Value, x.AttachType, "User").AsList();
				//}));
			}
			//if (positionId!=null)
			{
				var roles = new List<RoleModel>();

				var posGroup = new DefaultDictionary<long, RoleGroup>(x => new RoleGroup(new List<RoleGroupRole>(), x, AttachType.Position, "Function"));

				if (positionId != null) {
					var baseGroup = posGroup[positionId.Value];
					var myPosRolesLinks = links.Where(x => x.AttachType == AttachType.Position && x.AttachId == positionId);
					var posRoles = myPosRolesLinks.Select(x => new RoleGroupRole(rolesLU.GetOrDefault(x.RoleId, null),x.Ordering)).Where(x => x != null && x.Role != null).ToList();
					posGroup[positionId.Value].Roles.AddRange(posRoles);
				}

				var posRolesLinks = links.Where(x => x.AttachType == AttachType.Position && relaventPD.Any(y => y.PosId == x.AttachId) && x.AttachId != positionId);
				foreach (var pos in posRolesLinks.GroupBy(x => x.AttachId)) {
					var posRoles = pos.Select(x => new RoleGroupRole(rolesLU.GetOrDefault(x.RoleId, null),x.Ordering)).Where(x => x != null && x.Role!=null).ToList();
					posGroup[pos.Key].Roles.AddRange(posRoles);
				}

				foreach (var group in posGroup) {
					relaventGroups.Add(group.Value);
				}
			}
			{
				var teamRolesLinks = links.Where(x => x.AttachType == AttachType.Team && relaventTD.Any(y => y.TeamId == x.AttachId));

				foreach (var team in teamRolesLinks.GroupBy(x => x.AttachId)) {
					var teamRoles = team.Select(x => new RoleGroupRole(rolesLU.GetOrDefault(x.RoleId, null),x.Ordering)).Where(x => x != null && x.Role!=null).ToList();
					if (teamRoles.Any()) {
						var teamName = td.FirstOrDefault(y => y.TeamId == team.Key).NotNull(y => y.TeamName) ?? "Team";
						relaventGroups.Add(new RoleGroup(teamRoles, team.Key, AttachType.Team, teamName));
					}
				}
			}
			//relaventGroups.AddRange(posRoles.SelectMany(x => {
			//	if (!rolesLU.ContainsKey(x.RoleId))
			//		return new List<RoleGroup>();
			//	return new RoleGroup(rolesLU[x.RoleId], x.AttachId.Value, x.AttachType, "Function").AsList();
			//}));

			//var teamRoles = links.Where(x => x.AttachType == AttachType.Team && relaventTD.Any(y => y.TeamId == x.AttachId));
			//relaventGroups.AddRange(teamRoles.SelectMany(x => {
			//	if (!rolesLU.ContainsKey(x.RoleId))
			//		return new List<RoleGroup>();
			//	return new RoleGroup(rolesLU[x.RoleId], x.AttachId.Value, x.AttachType, td.FirstOrDefault(y => y.TeamId == x.AttachId).NotNull(y => y.TeamName) ?? "Team").AsList();
			//}));


			return relaventGroups.ToList();
		}
	}
}