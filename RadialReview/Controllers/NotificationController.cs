using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Models.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class NotificationController : BaseController {

		public class NotificationViewModel {
			public long Id { get; set; }
			public bool CanMarkSeen { get; set; }
			public string Message { get; set; }
			public string Details { get; set; }
			public string Image { get; set; }
			public DateTime? Seen { get; set; }
			public DateTime CreateTime { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Index(int page = 0,int count=10) {
			var notifications = await NotificationAccessor.GetNotificationsForUser(GetUser(), GetUser().Id, DateTime.MinValue, NotificationDevices.Computer,true);
			//await NotificationAccessor.MarkAllSeen(GetUser(), GetUser().Id, NotificationDevices.Computer);
			var notificationsVM = notifications
									.OrderByDescending(x=>x.Seen==null)
									.ThenByDescending(x => x.CreateTime)
									.Skip(count * page).Take(count)
									.Select(x => new NotificationViewModel() {
										Id = x.Id,
										Message = x.Name,
										Details = x.Details,
										Image = x.ImageUrl,
										Seen = x.Seen,
										CreateTime = x.CreateTime
									}).ToList();
			ViewBag.page = page;
			ViewBag.count = count;

			return View(notificationsVM);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> MarkRead(long id, bool force = false) {
			await NotificationAccessor.MarkSeen(GetUser(), id, force);
			return Json(true, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> MarkUnread(long id) {
			await NotificationAccessor.MarkUnseen(GetUser(), id);
			return Json(true, JsonRequestBehavior.AllowGet);
		}

		public class NotificationPostVM {
			public NotificationPostVM() {
				GroupKey = "key-"+(int)(((DateTime.UtcNow-new DateTime(2019,1,1)).TotalMilliseconds)/15000);
				CanMarkSeen = true;
				OnComputer = true;
				Expires = DateTime.MaxValue;
			}


			[Required]
			public long UserId { get;  set; }
			[Required]
			public string GroupKey { get;  set; }
			public bool OnComputer { get;  set; }
			public bool OnPhone { get;  set; }
			[Required]
			[AllowHtml]
			public string Message { get;  set; }
			[AllowHtml]
			public string Details { get;  set; }
			public DateTime? Expires { get;  set; }
			public string ActionUrl { get;  set; }
			public bool CanMarkSeen { get;  set; }
		}

		[Access(AccessLevel.RadialData)]
		public async Task<PartialViewResult> AdminCreate(long? userId = null) {			
			var n = new NotificationPostVM() {
				UserId = userId??0,
			};
			return PartialView(n);
		}

		[Access(AccessLevel.RadialData)]
		[HttpPost]
		public async Task<JsonResult> AdminCreate(NotificationPostVM model) {
			var devices = NotificationDevices.None;

			if (model.OnComputer) {
				devices = devices | NotificationDevices.Computer;
			}
			if (model.OnPhone) {
				devices = devices | NotificationDevices.Phone;
			}

			await NotificationAccessor.FireNotification_Unsafe(NotificationGroupKey.FromString(model.GroupKey),model.UserId,devices,model.Message,model.Details,model.Expires,model.ActionUrl,model.CanMarkSeen);
			return Json(ResultObject.SilentSuccess());
		}



		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Data(/*bool seen=false*/) {

			var notifications = await NotificationAccessor.GetNotificationsForUser(GetUser(), GetUser().Id, DateTime.UtcNow.AddDays(-1), NotificationDevices.Computer,false);
			//if (seen) {
			//	await NotificationAccessor.MarkAllSeen(GetUser(), GetUser().Id, NotificationDevices.Computer);
			//}
			return Json(notifications.Select(x => new NotificationViewModel() {
				Id = x.Id,
				CanMarkSeen = x.CanBeMarkedSeen,
				Message = x.Name,
				Details = x.Details,
				Image = x.ImageUrl,
				Seen = x.Seen
			}).ToList(),JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> Modal() {
			var notifications = await NotificationAccessor.GetNotificationsForUser(GetUser(), GetUser().Id, null, NotificationDevices.Computer);
			return PartialView(notifications.Select(x => new NotificationViewModel() {
				Id = x.Id,
				CanMarkSeen = x.CanBeMarkedSeen,
				Message = x.Name,
				Details = x.Details,
				Image = x.ImageUrl,
				Seen = x.Seen
			}).ToList());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> ClearNotifications() {
			await NotificationAccessor.MarkAllSeen(GetUser(), GetUser().Id, NotificationDevices.Computer);
			return Json(ResultObject.SilentSuccess());
		}
	}
}