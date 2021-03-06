﻿//using RadialReview.Utilities.Hooks;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using NHibernate;
//using System.Threading.Tasks;
//using Microsoft.AspNet.SignalR;
//using RadialReview.Hubs;
//using RadialReview.Areas.CoreProcess.CamundaComm;
//using RadialReview.Models.Angular.Base;
//using RadialReview.Models.Angular.Dashboard;
//using RadialReview.Models.Angular.CoreProcess;
//using static CamundaCSharpClient.Query.Task.TaskQuery;

//namespace RadialReview.Crosscutting.Hooks.Realtime {
//    public class RealTime_Tasks : ITaskHook {


//		public bool AbsorbErrors() {
//			return false;
//		}
//		public bool CanRunRemotely() {
//			return false;
//		}

//		public HookPriority GetHookPriority() {
//			return HookPriority.UI;
//		}

//		private void _UpdateGroup(IHubContext hub,IEnumerable<IdentityLink> links,string identityLinkType, AngularListType addOrRemove,  AngularTask angularTask) {
//            var groups = links.Where(x => identityLinkType == null || x.type == identityLinkType).SelectNoException(link => RealTimeHub.Keys.GenerateRgm(link)).ToList();
//            _UpdateGroup(hub, groups, addOrRemove, angularTask);
//        }

//        private static void _UpdateGroup(IHubContext hub, List<string> groups, AngularListType addOrRemove, AngularTask angularTask) {
//            var exe = hub.Clients.Groups(groups);
//            exe.update(new AngularUpdate() {
//                new AngularCoreProcessData() {
//                    Tasks = AngularList.CreateFrom(addOrRemove,angularTask)
//                }
//            });
//        }

//        public async Task ClaimTask(ISession s, string taskId, long userId) {
//            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
//            var links = await CommFactory.Get().GetIdentityLinks(taskId);
//            var task = await CommFactory.Get().GetTaskById(taskId);

//            var angularTask = AngularTask.Create(task);
//            _UpdateGroup(hub, links, "candidate", AngularListType.Remove, angularTask);
//            _UpdateGroup(hub, links, "assignee", AngularListType.ReplaceIfNewer, angularTask);
//        }

//        public async Task CompleteTask(ISession s, string taskId, long userId) {
//            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
//            var groups = RealTimeHub.Keys.GenerateRgm(userId).AsList();

//            var angularTask = new AngularTask(taskId);
//            _UpdateGroup(hub, groups, AngularListType.Remove, angularTask);
//        }

//        public async Task UnclaimTask(ISession s, string taskId) {

//            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
//            var links = await CommFactory.Get().GetIdentityLinks(taskId);
//            var task = await CommFactory.Get().GetTaskById(taskId);

//            var angularTask = AngularTask.Create(task);
//            _UpdateGroup(hub, links, "assignee", AngularListType.Remove, angularTask);
//            _UpdateGroup(hub, links, "candidate", AngularListType.ReplaceIfNewer, angularTask);
//        }
//    }
//}