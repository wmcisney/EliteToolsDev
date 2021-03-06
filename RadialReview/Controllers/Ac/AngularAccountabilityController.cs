﻿using RadialReview.Accessors;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public partial class AccountabilityController : BaseController
    {
		// GET: AngularAccountability
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularAccountabilityNode(AngularAccountabilityNode model, string connectionId = null)
		{
			await AccountabilityAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularRole(AngularRole model, string connectionId = null) {
			await AccountabilityAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult RemoveAngularRole(long id) {
			AccountabilityAccessor.RemoveRole(GetUser(), id);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UnremoveAngularRole(long id) {
			AccountabilityAccessor.UnremoveRole(GetUser(), id);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddNode(long id)
		{
			AccountabilityAccessor.AppendNode(GetUser(), id);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Swap(long id,long parent,string connectionId=null)
		{

			//_UserAccessor.SwapManager(GetUser(), id, oldManagerId, newManagerId, DateTime.UtcNow);
			AccountabilityAccessor.SwapParents(GetUser(), id,parent, connectionId);
			return Json(ResultObject.SilentSuccess());
		}



		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddRole(long aid,AttachType atype,int? insert=null,string name=null) {
			await AccountabilityAccessor.AddRole(GetUser(), new Attach(atype,aid),name, insert:insert);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Append(long id){
			var nodeId = id;
			AccountabilityAccessor.AppendNode(GetUser(), nodeId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		//[Access(AccessLevel.UserOrganization)]
		//public JsonResult Swap(long id, long parent)
		//{
		//	var nodeId = id;
		//	AccountabilityAccessor.SwapParents(GetUser(), nodeId, parent);
		//	return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		//}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Remove(long id) {
			var nodeId = id;
			AccountabilityAccessor.RemoveNode(GetUser(), nodeId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}	


		[Access(AccessLevel.UserOrganization)]
		public JsonResult Data(long id, long? node = null, long? user = null, bool expandAll = false)
		{
			var tree = AccountabilityAccessor.GetTree(GetUser(), id,user, node, expandAll: expandAll);
			// var acTreeId = tree.Flatten().FirstOrDefault(x => x.user.NotNull(y => y.Id == (parent ?? GetUser().Id)));
			var c = new Chart<AngularAccountabilityChart>(tree.Id)
			{
				height = "100%",
				width = "100%",
				data = tree,				
			};
			return Json(c, JsonRequestBehavior.AllowGet);
		}
		



	}
}