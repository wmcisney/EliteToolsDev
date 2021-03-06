﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using NHibernate;
using RadialReview.Api;
using RadialReview.Models.Angular.Issues;
using RadialReview.Utilities;

namespace RadialReview.Crosscutting.AttachedPermission
{
    public class IssueAttachedPermissionHandler : IAttachedPermissionHandler<AngularIssue>
    {
        public async Task<PermissionObject> GetPermissionsForAdministration(ISession s, PermissionsUtility perm){
            return new PermissionObject(){
                CanCreate = true,
                Name = "AngularIssue"
            };
        }

        public async Task<PermissionDto> GetPermissionsForObject(ISession s, PermissionsUtility perm, IAttachedPermission t)
        {
            return new PermissionDto {
                CanEdit = perm.IsPermitted(x => x.EditIssue(((AngularIssue)t).Id))
            };
        }

        //public async Task<PermissionDto> GetPermissions(ISession s, PermissionsUtility perm, IAttachedPermission t)
        //{
        //    if ()
        //}
    }
}