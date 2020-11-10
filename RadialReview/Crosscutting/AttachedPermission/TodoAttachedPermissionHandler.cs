using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using NHibernate;
using RadialReview.Api;
using RadialReview.Models.Angular.Todos;
using RadialReview.Utilities;

namespace RadialReview.Crosscutting.AttachedPermission
{
    public class TodoAttachedPermissionHandler : IAttachedPermissionHandler<AngularTodo>
    {
        public async Task<PermissionObject> GetPermissionsForAdministration(ISession s, PermissionsUtility perm)
        {
            return new PermissionObject()
            {
                CanCreate = true,
                Name = "AngularTodo"
            };
        }

        public async Task<PermissionDto> GetPermissionsForObject(ISession s, PermissionsUtility perm, IAttachedPermission t)
        {
            return new PermissionDto { CanEdit = false };
        }
    }
}