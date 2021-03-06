﻿//using System.Collections.Specialized;
//using FluentNHibernate.Utils;
//using NHibernate;
//using NHibernate.Criterion;
//using NHibernate.Linq;
//using RadialReview.Exceptions;
//using RadialReview.Models;
//using RadialReview.Models.Permissions;
//using RadialReview.Utilities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;

//namespace RadialReview.Accessors
//{
//    public class DeepSubordianteAccessor : BaseAccessor
//    {

//        public List<long> GetSubordinatesAndSelf(UserOrganizationModel caller, long userId)
//        {
//            using (var s = HibernateSession.GetCurrentSession())
//            {
//                using (var tx = s.BeginTransaction())
//                {
//                    return GetSubordinatesAndSelf(s, caller, userId);
//                }
//            }
//        }
//        public List<UserOrganizationModel> GetSubordinatesAndSelfModels(UserOrganizationModel caller, long userId)
//        {
//            using (var s = HibernateSession.GetCurrentSession())
//            {
//                using (var tx = s.BeginTransaction())
//                {
//                    return GetSubordinatesAndSelfModels(s, caller, userId);
//                }
//            }

//        }

//        public static List<UserOrganizationModel> GetSubordinatesAndSelfModels(ISession s, UserOrganizationModel caller, long userId)
//        {

//            if (caller.Id != userId && !PermissionsUtility.IsAdmin(caller))
//            {
//				var found = s.QueryOver<DeepSubordinateModel>().Where(x => x.DeleteTime == null && x.ManagerId == caller.Id && x.SubordinateId == userId).SingleOrDefault();
//                if (found == null)
//                    throw new PermissionsException("You don't have access to this user");
//            }
//			//var allPermissions = s.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.ForUser.Id == userId && x.Permissions == PermissionType.ManageEmployees).Select(x => x.AsUser.Id).List<long>().ToList();
//			//allPermissions.Add(userId);
//			var allPermissions = new List<long>(){userId};


//            UserOrganizationModel alias=null;

//            var subordinates = s.QueryOver(()=>alias)
//                                .WithSubquery.WhereExists(
//                                    QueryOver.Of<DeepSubordinateModel>()
//                                        .Where(e => e.DeleteTime==null)
//										.WhereRestrictionOn(x=>x.ManagerId).IsIn(allPermissions)
//                                        .Where(d => d.SubordinateId == alias.Id)
//                                        .Select(d=>d.Id))
//                                        .List().ToList();

//                /*.Where(x => x.DeleteTime == null && x.ManagerId == userId)
//                .Fetch(e => e.Subordinate).Eager
//                .Select(x=>x.Subordinate).List<UserOrganizationModel>()
//                .ToList();*/
//            return subordinates;
//        }

//        public static List<long> GetSubordinatesAndSelf(ISession s, UserOrganizationModel caller, long userId,PermissionType? type=null)
//        {

//            if (caller.Id != userId && !PermissionsUtility.IsAdmin(caller)){
//                var found = s.QueryOver<DeepSubordinateModel>().Where(x => x.DeleteTime == null && x.ManagerId == caller.Id && x.SubordinateId == userId).SingleOrDefault();
//                if (found == null)
//                    throw new PermissionsException("You don't have access to this user");
//            }
//			var allPermitted = new List<long>() { userId };
//			if (type!=null)
//				allPermitted.AddRange(s.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.ForUser.Id == userId && x.Permissions == type).Select(x => x.AsUser.Id).List<long>().ToList());
			
//			//allPermissions.Add(userId);

//            var subordinates = s.QueryOver<DeepSubordinateModel>()
//                                    .Where(x => x.DeleteTime == null)
//									.WhereRestrictionOn(x => x.ManagerId).IsIn(allPermitted)
//                                    .Select(x=>x.SubordinateId)
//                                    .List<long>()
//                                    .ToList();
//			subordinates.Add(userId);

//            return subordinates.Distinct().ToList();
//        }

//        public List<DeepSubordinateModel> GetOrganizationMap(UserOrganizationModel caller, long organizationId)
//        {
//            using (var s = HibernateSession.GetCurrentSession())
//            {
//                using (var tx = s.BeginTransaction())
//                {
//                    var perm = PermissionsUtility.Create(s, caller);
//                    return GetOrganizationMap(s, perm, organizationId);
//                }
//            }
//        }

//        public static List<DeepSubordinateModel> GetOrganizationMap(ISession s, PermissionsUtility perm, long organizationId)
//        {
//            perm.ViewOrganization(organizationId);
//            return s.QueryOver<DeepSubordinateModel>().Where(x => x.OrganizationId == organizationId).List().ToList();
//        }

//		[Obsolete("Use UserAccessor.RemoveManager",false)]
//        public static void Remove(ISession s, UserOrganizationModel manager, UserOrganizationModel subordinate, DateTime now,bool ignoreCircular=false)
//        {
//            //Grab all subordinates' deep subordinates
//            var allSuperiors = SubordinateUtility.GetSuperiors(s,manager, false);
//            allSuperiors.Add(manager);
//            var allSubordinates = SubordinateUtility.GetSubordinates(s,subordinate, false);
//            allSubordinates.Add(subordinate);

//			var selfRemove = false;

//            foreach (var SUP in allSuperiors)
//            {
//                var managerSubordinates = s.QueryOver<DeepSubordinateModel>().Where(x => x.ManagerId == SUP.Id).List().ToListAlive();

//                foreach (var sub in allSubordinates)
//                {
//                    var found = managerSubordinates.FirstOrDefault(x => x.SubordinateId == sub.Id && x.Links>0);
//                    if (found == null)
//                    {
//                        log.Error("Manager link doesn't exist for orgId=(" + manager.Organization.Id + "). Advise that you run deep subordinate creation.");
//                    }
//                    else
//                    {
//                        found.Links -= 1;
//                        if (found.Links == 0)
//                            found.DeleteTime = now;
//						if (found.Links < 0)
//						{
//							if (!ignoreCircular)
//								throw new Exception("This shouldn't happen.");
//						}
//                        s.Update(found);
//                    }
//                }
//            }
//        }

//		[Obsolete("Use UserAccessor.AddManager", false)]
//		public static void Add(ISession s, UserOrganizationModel manager, UserOrganizationModel subordinate, long organizationId, DateTime now, bool ignoreCircular =false)
//        {
//            //Get **users** subordinates, make them deep subordinates of manager
//            var allSubordinates = SubordinateUtility.GetSubordinates(s,subordinate, false);
//            allSubordinates.Add(subordinate);
//            var allSuperiors = SubordinateUtility.GetSuperiors(s,manager, false);
//            allSuperiors.Add(manager);

//            //for manager and each of his superiors
//            foreach (var SUP in allSuperiors)
//            {
//                var managerSubordinates = s.QueryOver<DeepSubordinateModel>().Where(x => x.ManagerId == SUP.Id).List().ToListAlive();

//                foreach (var sub in allSubordinates)
//                {
//                    if (sub.Id == SUP.Id && !ignoreCircular)
//                        throw new PermissionsException("A circular dependency was found. " + manager.GetName() + " cannot manage " + subordinate.GetName() + " because " + manager.GetName() + " is " + subordinate.GetName() + "'s subordinate.");

//                    var found = managerSubordinates.FirstOrDefault(x => x.SubordinateId == sub.Id);
//                    if (found == null)
//                    {
//                        found = new DeepSubordinateModel()
//                        {
//                            CreateTime = now,
//                            ManagerId = SUP.Id,
//                            SubordinateId = sub.Id,
//                            Links = 0,
//                            OrganizationId = organizationId
//                        };
//                    }
//                    found.Links += 1;
//                    s.SaveOrUpdate(found);
//                }
//            }
//        }


//        public static void RemoveAll(ISession s, UserOrganizationModel user, DateTime now)
//        {
//            var id = user.Id;
//            var all = s.QueryOver<DeepSubordinateModel>().Where(x => (x.ManagerId == id || x.SubordinateId == id) && x.DeleteTime==null).List().ToList();

//            foreach (var a in all)
//            {
//                a.DeleteTime = now;
//                s.Update(a);
//            }

//        }

//	    public static bool ManagesUser(ISession s,PermissionsUtility perms, long managerId, long subordinateId)
//	    {
//			perms.ViewUserOrganization(managerId, false).ViewUserOrganization(subordinateId, false);
//			var m = s.Get<UserOrganizationModel>(managerId);
//			var sub = s.Get<UserOrganizationModel>(subordinateId);
//			if (m.IsRadialAdmin)
//				return true;
//			if (m.ManagingOrganization && m.Organization.Id == sub.Organization.Id)
//				return true;

//			var found = s.QueryOver<DeepSubordinateModel>().Where(x => x.ManagerId == managerId && x.SubordinateId == subordinateId && x.DeleteTime == null).Take(1).SingleOrDefault();

//			return found != null;
//	    }

//	    public static bool ManagesUser(UserOrganizationModel caller, long managerId, long subordinateId)
//	    {
//			using (var s = HibernateSession.GetCurrentSession())
//			{
//				using (var tx = s.BeginTransaction()){
//					var perms = PermissionsUtility.Create(s, caller);

//					return ManagesUser(s, perms, managerId, subordinateId);
//				}
//			}
//	    }

//        public static void UndeleteAll(ISession s, UserOrganizationModel user, DateTime deleteTime)
//        {
//            var id = user.Id;
//            var all = s.QueryOver<DeepSubordinateModel>().Where(x => (x.ManagerId == id || x.SubordinateId == id) && x.DeleteTime == deleteTime).List().ToList();

//            foreach (var a in all)
//            {
//                a.DeleteTime = null;
//                s.Update(a);
//            }
//        }
//    }
//}