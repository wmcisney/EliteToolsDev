

using NHibernate;
using RadialReview.Api;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.AttachedPermission
{
    public class PermissionDto
    {
        public bool CanEdit { get; set; }        

        public PermissionDto()
        {
            CanEdit = true;          
        }
    }
    public interface IAttachedPermission
    {
        PermissionDto Permission { get; set; }       
    }

    public interface IAttachedPermissionHandler
    {
        Task<PermissionDto> GetPermissionsForObject(ISession s, PermissionsUtility perm, IAttachedPermission t);
        Task<PermissionObject> GetPermissionsForAdministration(ISession s, PermissionsUtility perm);

    }

    public interface IAttachedPermissionHandler<T> : IAttachedPermissionHandler  where T:IAttachedPermission
    {
    }
}
