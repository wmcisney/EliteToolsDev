using RadialReview.Crosscutting.AttachedPermission;
using RadialReview.Models.Angular.Issues;
using RadialReview.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Xml.Serialization;

namespace RadialReview.Api
{
    public class TTActionWebApiFilter : ActionFilterAttribute
    {
        //public override void OnActionExecuting(HttpActionContext actionContext)
        //{

        //}

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            bool includePermissionFlag = false;
            if (actionExecutedContext.Request.Headers.Contains("Include_Permission"))
            {
                string flag = actionExecutedContext.Request.Headers.GetValues("Include_Permission").FirstOrDefault();
                Boolean.TryParse(flag, out includePermissionFlag);
            }

            if (includePermissionFlag && actionExecutedContext.Response != null) //<-- includePermissionFlag
            {
                var objectContent = actionExecutedContext.Response.Content as System.Net.Http.ObjectContent;
                if (objectContent != null)
                {
                    var type = objectContent.ObjectType; //type of the returned object
                    var value = objectContent.Value; //holding the returned value


                    if (value is IEnumerable) {
                        type = value.GetType().GetGenericArguments()[0];
                    }else{
                        value = value.AsList();
                    }

                    var resultList = (value as IEnumerable<dynamic>).ToList();
                    List<PermissionObject> listPermission = new List<PermissionObject>();
                    using (var s = HibernateSession.GetCurrentSession())
                    {
                        using (var tx = s.BeginTransaction())
                        {
                            var caller = ((BaseApiController)actionExecutedContext.ActionContext.ControllerContext.Controller).CurrentUser;
                            var perms = PermissionsUtility.Create(s, caller);

                             if (!listPermission.Any(x => x.Name == type.Name))
                                listPermission.Add(PermissionRegistry.GetAdminstrationPermission(s,perms,type).GetAwaiter().GetResult());


                            foreach (var item in resultList)
                            {
                                if (item is IAttachedPermission)
                                {
                                    var attachedPermission = (IAttachedPermission)item;
                                    PermissionRegistry.AttachPermissions(s, perms, (IAttachedPermission)item).GetAwaiter().GetResult();

                                }
                            }
                            var response = new ResponseWithPermission { Result = resultList, Permissions = listPermission };
                            (actionExecutedContext.ActionContext.Response.Content as System.Net.Http.ObjectContent).Value = response;
                        }
                    }
                }
            }


            //base.OnActionExecuted(actionExecutedContext);
        }


    }

    public class ResponseWithPermission
    {
        public object Result { get; set; }
        public List<PermissionObject> Permissions { get; set; }
    }

    public class PermissionObject
    {
        public string Name { get; set; }
        public bool CanCreate { get; set; }

        public PermissionObject()
        {
            this.Name = "";
            this.CanCreate = true;
        }
    }
}