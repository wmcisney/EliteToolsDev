using LambdaSerializer;
using log4net;
using NHibernate;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RadialReview.Crosscutting.AttachedPermission;
using RadialReview.Api;

namespace RadialReview.Crosscutting.AttachedPermission
{
    public class PermissionRegistry
    {
        //protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static PermissionRegistry _Singleton { get; set; }
        private List<IAttachedPermissionHandler> _AttachedPermissionHandler { get; set; }
        private static object lck = new object();

        private PermissionRegistry()
        {
            lock (lck)
            {
                _AttachedPermissionHandler = new List<IAttachedPermissionHandler>();
            }
        }

        public static void RegisterPermission(IAttachedPermissionHandler permission)
        {
            var hooks = GetSingleton();
            lock (lck)
            {
                hooks._AttachedPermissionHandler.Add(permission);
            }
        }

        public static PermissionRegistry GetSingleton()
        {
            if (_Singleton == null)
                _Singleton = new PermissionRegistry();
            return _Singleton;
        }

        public static async Task<PermissionObject> GetAdminstrationPermission(ISession s, PermissionsUtility perm, Type type)
        {
            var handler = await GetHandler(s, perm, type);
            if (handler != null){
                return await handler.GetPermissionsForAdministration(s, perm);
            }
            return null;

        }

        public static async Task AttachPermissions(ISession s, PermissionsUtility perm, IAttachedPermission permission){
            var handler = await GetHandler(s, perm, permission.GetType());
            if (handler != null)
            {
                try{
                    permission.Permission = await handler.GetPermissionsForObject(s, perm, permission);
                }catch (Exception e){
                    permission.Permission = new PermissionDto();
                }
            }        
        }


        private static async Task<IAttachedPermissionHandler> GetHandler(ISession s, PermissionsUtility perm, Type type)
        {
            var list = GetSingleton()._AttachedPermissionHandler;
            foreach(var handler in list)
            {                     
                var handlerInterface = handler.GetType().GetInterfaces().SingleOrDefault(x => x.Name.StartsWith(nameof(IAttachedPermissionHandler)) && x.GenericTypeArguments.Length == 1);
                if (handlerInterface != null)
                {
                    if (handlerInterface.GenericTypeArguments[0] == type)
                        return handler;
                }            
            }
            return null;
        }
    }
}