﻿using System.Web.UI.WebControls;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Criterion;
using RadialReview.Models.Reviews;
using RadialReview.SessionExtension;
using RadialReview;

namespace RadialReview.Accessors {
    public class ResponsibilitiesAccessor : BaseAccessor {
        public ResponsibilityModel GetResponsibility(UserOrganizationModel caller, long responsibilityId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var responsibility = s.Get<ResponsibilityModel>(responsibilityId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(responsibility.ForOrganizationId);
                    return responsibility;
                }
            }
        }
        public List<ResponsibilityModel> GetResponsibilitiesForUser(UserOrganizationModel caller, long forUserId, DateRange range) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
#pragma warning disable CS0618 // Type or member is obsolete
                    return GetResponsibilitiesForUser(s.ToQueryProvider(true), perms, forUserId, range);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        [Obsolete("Use AskableAccessor.GetAskablesForUser", false)]
        public static List<ResponsibilityModel> GetResponsibilitiesForUser(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId, DateRange range) {
            return GetResponsibilityGroupsForUser(queryProvider, perms, forUserId)
                    .SelectMany(x => x.Responsibilities)
                    .FilterRange(range)
                    .ToList();
        }

        public ResponsibilityGroupModel GetResponsibilityGroup(ISession s, PermissionsUtility perms, long responsibilityGroupId) {
            var resGroup = s.Get<ResponsibilityGroupModel>(responsibilityGroupId);
            long orgId;

            if (resGroup is OrganizationModel)
                orgId = resGroup.Id;
            else
                orgId = resGroup.Organization.Id;

            try {
                var a = resGroup.GetName();
                var b = resGroup.GetImageUrl();
            } catch {

            }

            perms.ViewOrganization(orgId);
            return resGroup;
        }

        public ResponsibilityGroupModel GetResponsibilityGroup(UserOrganizationModel caller, long responsibilityGroupId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetResponsibilityGroup(s, perms, responsibilityGroupId);
                }
            }
        }

        public static void ReorderSection(UserOrganizationModel caller, long sectionId, int oldOrder, int newOrder) {

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).EditOrganizationQuestions(caller.Organization.Id);
                    var found = s.Get<AskableSectionModel>(sectionId);
                    if (found.OrganizationId != caller.Organization.Id)
                        throw new PermissionsException();

                    var items = s.QueryOver<AskableSectionModel>().Where(x => x.DeleteTime == null && x.OrganizationId == caller.Organization.Id).List().ToList();

                    Reordering.Create(items, sectionId, null, oldOrder, newOrder, x => x._Ordering, x => x.Id)
                              .ApplyReorder(s);


                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static List<AskableSectionModel> GetAllSections(UserOrganizationModel caller, long organizationId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);

                    return s.QueryOver<AskableSectionModel>()
                                .Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
                                .List().OrderBy(x => x._Ordering).ToList();

                }
            }
        }

        public static AskableSectionModel GetSection(UserOrganizationModel caller, long sectionId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var section = s.Get<AskableSectionModel>(sectionId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(section.OrganizationId);
                    return section;

                }
            }
        }

        public static AskableSectionModel EditSection(UserOrganizationModel caller, AskableSectionModel section) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    PermissionsUtility.Create(s, caller).EditOrganizationQuestions(caller.Organization.Id);
                    if (section.Id == 0) {
                        section.OrganizationId = caller.Organization.Id;

                        var allOrders = s.QueryOver<AskableSectionModel>()
                            .Where(x => x.DeleteTime == null && x.OrganizationId == section.OrganizationId)
                            .Select(x => x._Ordering).List<int>();
                        if (allOrders.Any())
                            section._Ordering = allOrders.Max() + 1;
                        else
                            section._Ordering = 0;

                        s.Save(section);
                    } else {
                        var found = s.Get<AskableSectionModel>(section.Id);
                        if (found.OrganizationId != caller.Organization.Id)
                            throw new PermissionsException();
                        if (found.OrganizationId != section.OrganizationId)
                            throw new PermissionsException();
                        s.Merge(section);
                    }
                    tx.Commit();
                    s.Flush();
                    return section;
                }
            }


        }

        public List<ResponsibilityModel> GetResponsibilities(UserOrganizationModel caller, long responsibilityGroupId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var responsibilities = s.QueryOver<ResponsibilityModel>().Where(x => x.ForResponsibilityGroup == responsibilityGroupId).List().ToList();

                    var orgs = responsibilities.Select(x => x.ForOrganizationId).Distinct().ToList();
                    var permissions = PermissionsUtility.Create(s, caller);
                    foreach (var oId in orgs) {
                        permissions.ViewOrganization(oId);
                    }
                    return responsibilities;
                }
            }
        }

        private static TeamAccessor _TeamAccessor = new TeamAccessor();
        private static PositionAccessor _PositionAccessor = new PositionAccessor();

        public static List<UserOrganizationModel> GetResponsibilityGroupMembers(ISession s, PermissionsUtility perm, long rgmId) {
            var found = s.Get<ResponsibilityGroupModel>(rgmId);

            if (found is UserOrganizationModel) {
                perm.ViewUserOrganization(found.Id, false);
                return new List<UserOrganizationModel>() { (UserOrganizationModel)found };
            } else if (found is OrganizationModel) {
#pragma warning disable CS0618 // Type or member is obsolete
                return OrganizationAccessor.GetAllUserOrganizations(s, perm, found.Id);
#pragma warning restore CS0618 // Type or member is obsolete
            } else if (found is OrganizationTeamModel) {
                return TeamAccessor.GetTeamMembers(s.ToQueryProvider(true), perm, found.Id, true).Select(x => x.User).ToList();
            } else if (found is OrganizationPositionModel) {
                return OrganizationAccessor.GetUsersWithOrganizationPositions(s, perm, found.Organization.Id, rgmId);
            } else {
                throw new ArgumentOutOfRangeException();
            }
        }

        public static List<UserOrganizationModel> GetResponsibilityGroupMembers(UserOrganizationModel caller, long rgmId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetResponsibilityGroupMembers(s, perms, rgmId);
                }
            }
        }

        public static IEnumerable<long> GetMemberIds(ISession s, PermissionsUtility perm, long rgmId) {
            return GetMemberIds(s, perm, rgmId.AsList());
        }

        public static IEnumerable<long> GetMemberIds(ISession s, PermissionsUtility perm, IEnumerable<long> rgmId) {

            var rgms = s.QueryOver<ResponsibilityGroupModel>().WhereRestrictionOn(x => x.Id).IsIn(rgmId.Distinct().ToArray())
                //.Select(x=>x.Id,x=>x.accoun.,x=>x.Organization.Id).List<object[]>()
                //.Select(x=> new {Id = (long)x[0],Type =(Type)x[1],OrgId = (long)x[2] })
                .List()
                .ToList();

            var output = new List<IEnumerable<long>>();

            foreach (var _rgm in rgms) {
                var rgm = _rgm.Deproxy();
                if (rgm.GetType() == typeof(UserOrganizationModel)) {
                    perm.ViewUserOrganization(rgm.Id, false);
                    output.Add(rgm.Id.AsList());
                } else if (rgm.GetType() == typeof(OrganizationModel)) {
                    output.Add(OrganizationAccessor.GetAllUserOrganizationIds(s, perm, rgm.Id));
#pragma warning restore CS0618 // Type or member is obsolete
                } else if (rgm.GetType() == typeof(OrganizationTeamModel)) {
                    output.Add(TeamAccessor.GetTeamMemberIds(s, perm, rgm.Id));
                } else if (rgm.GetType() == typeof(OrganizationPositionModel)) {
                    output.Add(OrganizationAccessor.GetUserIdsWithOrganizationPositions(s, perm, rgm.Organization.Id, rgm.Id));
                } else {
                    throw new ArgumentOutOfRangeException();
                }
            }
            return output.SelectMany(x => x.ToList()).Distinct();
        }



        public List<ResponsibilityGroupModel> GetResponsibilityGroupsForUser(UserOrganizationModel caller, long userId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetResponsibilityGroupsForUser(s.ToQueryProvider(true), perms, userId);
                }
            }
        }

        public static IEnumerable<long> GetResponsibilityGroupIdsForUser(ISession s, PermissionsUtility perms, long userId) {
            perms.ViewUserOrganization(userId, false);

            var output = new List<IEnumerable<long>>();
            output.Add(userId.AsList());
            output.Add(TeamAccessor.GetUsersTeamIds(s, perms, userId));
            output.Add(PositionAccessor.GetPositionIdsForUser(s, perms, userId));
            //output.Add(TeamAccessor.GetUsersTeamIds(s, perms, userId));
            return output.SelectMany(x => x);
        }

        [Obsolete("Does not check if caller can view ResponsibilityGroups",false)]
        public static List<ResponsibilityGroupModel> GetResponsibilityGroupsForRgm(ISession s, PermissionsUtility permissions, long rgmId) {
            var rgm = s.Get<ResponsibilityGroupModel>(rgmId);
            permissions.ViewRGM(rgmId);

            if (rgm is UserOrganizationModel) {
                return GetResponsibilityGroupsForUser(s.ToQueryProvider(true), permissions, rgmId);
            } else {
                return new List<ResponsibilityGroupModel>() { rgm };
            }
        }

	/*	public static List<long> GetResponsibilityGroupIdsForUser(ISession s, PermissionsUtility permissions, long userId) {
			var teams = TeamAccessor.GetUsersTeamIds(s, permissions, userId).ToListAlive().Select(x => x.Team.Id).ToList();
			var positions = PositionAccessor.GetPositionIdsForUser(s, permissions, userId).ToList();

			permissions.ViewUserOrganization(userId, false);
			var user = s.Get<UserOrganizationModel>(userId);

			var responsibilityGroups = new List<long>();
			//User
			responsibilityGroups.Add(user.Id);
			//Positions
			responsibilityGroups.AddRange(positions);
			//Teams
			responsibilityGroups.AddRange(teams);
			return responsibilityGroups;
		}*/

		public static List<ResponsibilityGroupModel> GetResponsibilityGroupsForUser(AbstractQuery s, PermissionsUtility permissions, long userId) {
            var teams = TeamAccessor.GetUsersTeams(s, permissions, userId);

            permissions.ViewUserOrganization(userId, false);
            var user = s.Get<UserOrganizationModel>(userId);

            var responsibilityGroups = new List<ResponsibilityGroupModel>();
            //User
            responsibilityGroups.Add(user);
            //Positions
            responsibilityGroups.AddRange(user.Positions.ToListAlive().Select(x => x.Position));
            //Teams
            responsibilityGroups.AddRange(teams.ToListAlive().Select(x => x.Team));
            return responsibilityGroups;
        }

        public void EditResponsibility(UserOrganizationModel caller, long responsibilityId, String responsibility = null,
            long? categoryId = null, long? responsibilityGroupId = null, bool? active = null, WeightType? weight = null,
            bool? required = null, AboutType? onlyAsk = null, bool updateOutstandingReviews = false, long? sectionId = null,
            QuestionType? questionType = null, string arguments = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                var r = new ResponsibilityModel();
                using (var tx = s.BeginTransaction()) {
                    var permissions = PermissionsUtility.Create(s, caller);

                    if (responsibilityId == 0) {
                        if (responsibility == null || categoryId == null || responsibilityGroupId == null)
                            throw new PermissionsException();

                        var rg = s.Get<ResponsibilityGroupModel>(responsibilityGroupId.Value);
                        permissions.ViewOrganization(rg.Organization.Id);
                        r.ForResponsibilityGroup = responsibilityGroupId.Value;
                        r.Responsibility = responsibility;
                        r.Required = true;
                        r.ForOrganizationId = caller.Organization.Id;
                        s.Save(r);
                        rg.Responsibilities.Add(r);
                        s.Update(rg);
                    } else {
                        r = s.Get<ResponsibilityModel>(responsibilityId);

                        if (responsibilityGroupId != null && responsibilityGroupId != r.ForResponsibilityGroup)//Cant change responsibility Group
                            throw new PermissionsException();

                        responsibilityGroupId = r.ForResponsibilityGroup;
                    }


                    permissions.EditOrganization(r.ForOrganizationId);

                    if (responsibility != null)
                        r.Responsibility = responsibility;

                    var qtWasSet = false;
                    if (categoryId != null) {
                        permissions.ViewCategory(categoryId.Value);
                        var cat = s.Get<QuestionCategoryModel>(categoryId.Value);
                        r.Category = cat;

                        if (ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.THUMBS).Id == cat.Id) {
                            r.SetQuestionType(QuestionType.Thumbs);
                            qtWasSet = true;
                        } else if (ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK).Id == cat.Id) {
                            r.SetQuestionType(QuestionType.Feedback);
                            qtWasSet = true;
                        }
                    }

                    if (qtWasSet == false && questionType != null) {
                        r.SetQuestionType(questionType.Value);

                    }

                    if (sectionId != null) {
                        var section = s.Get<AskableSectionModel>(sectionId);
                        permissions.ViewOrganization(section.OrganizationId);
                    }
                    r.SectionId = sectionId;

                    r.Arguments = arguments;

                    if (active != null) {
                        if (active == true)
                            r.DeleteTime = null;
                        else
                            r.DeleteTime = DateTime.UtcNow;
                    }

                    if (required != null)
                        r.Required = required.Value;
                    if (weight != null)
                        r.Weight = weight.Value;
                    if (onlyAsk != null)
                        r.OnlyAsk = onlyAsk.Value;


                    // update outstanding reviews.
                    if (updateOutstandingReviews) {
                        var outstanding = ReviewAccessor.OutstandingReviewsForOrganization_Unsafe(s, r.ForOrganizationId);
                        if (outstanding.Any()) {
                            var members = GetResponsibilityGroupMembers(s, permissions, responsibilityGroupId.Value);
                            var reviewees = members.Select(x => new Reviewee(x));
                            foreach (var o in outstanding) {
                                ReviewAccessor.AddResponsibilityAboutUsersToReview(s, permissions, o.Id, reviewees, r.Id);
                            }
                        }
                    }
                    s.Update(r);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public bool SetActive(UserOrganizationModel caller, long responsibilityId, Boolean active) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var responsibility = s.Get<ResponsibilityModel>(responsibilityId);
                    PermissionsUtility.Create(s, caller).EditOrganization(responsibility.ForOrganizationId);
                    if (active == true) {
                        responsibility.DeleteTime = null;
                    } else {
                        if (responsibility.DeleteTime == null)
                            responsibility.DeleteTime = DateTime.UtcNow;
                    }
                    s.Update(responsibility);
                    tx.Commit();
                    s.Flush();
                    return active;
                }
            }
        }

        //public void SetActive(UserOrganizationModel caller,long respon
    }
}
