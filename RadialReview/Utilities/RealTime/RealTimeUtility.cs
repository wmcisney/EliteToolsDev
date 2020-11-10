using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility : IDisposable {

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public const string DEFAULT_BATCH = "DEFAULT_BATCH";

		protected Dictionary<string, AngularUpdate> _updaters = new Dictionary<string, AngularUpdate>();
		protected Dictionary<string, dynamic> _groups = new Dictionary<string, dynamic>();

		protected List<Action> _actions = new List<Action>();
		protected bool Executed = false;
		protected bool SkipExecution = false;
		protected string SkipUser = null;
		private RealTimeUtility() { }

		private RealTimeUtility(string skipUser, bool shouldExecute) {
			// TODO: Complete member initialization
			SkipExecution = !shouldExecute;
			SkipUser = skipUser;
		}

		public static RealTimeUtility Create() {
			return new RealTimeUtility(null, true);
		}
		public static RealTimeUtility Create(bool shouldExecute = true) {
			return new RealTimeUtility(null, shouldExecute);
		}
		public static RealTimeUtility Create(string skipUser = null, bool shouldExecute = true) {
			return new RealTimeUtility(skipUser, shouldExecute);
		}

		public void DoNotExecute() {
			if (Executed) {
				throw new PermissionsException("Already executed.");
			}

			SkipExecution = true;
		}

		public bool ExecuteOnException { get; set; }

		protected bool Execute() {
			if (SkipExecution) {
				return false;
			}

			if (Executed) {
				throw new PermissionsException("Cannot execute again.");
			}

			Executed = true;
			_actions.ForEach(f => {
				try {
					f();
				} catch (Exception e) {
					log.Error("RealTime exception", e);
				}
			});
			foreach (var b in _updaters) {
				try {
					var group = _groups[b.Key];
					var angularUpdate = b.Value;
					group.update(angularUpdate);
				} catch (Exception e) {
					log.Error("SignalR exception", e);
				}
			}

			return true;
		}

		private string KeyNameGen(string name, bool skip, string group) {
			return name + "`" + skip + "`" + (group ?? DEFAULT_BATCH);
		}

		protected AngularUpdate GetUpdater<HUB>(string name, bool skip = true, string batch = null) where HUB : IHub {
			var key = KeyNameGen(name, skip, batch);

			if (_updaters.ContainsKey(key)) {
				return _updaters[key];
			}

			GetGroup<HUB>(name, skip, batch);
			var updater = new AngularUpdate();
			_updaters[key] = updater;
			return updater;
		}

		public RTUserUpdater UpdateUsers(params long[] userIds) {
			return new RTUserUpdater(userIds, this);
		}


		public RTRecurrenceUpdater UpdateRecurrences(IEnumerable<long> recurrences) {
			return UpdateRecurrences(recurrences.ToArray());
		}
		public RTVtoUpdater UpdateVtos(IEnumerable<long> vtos) {
			return UpdateVtos(vtos.ToArray());
		}

		public RTRecurrenceUpdater UpdateRecurrences(params long[] recurrences) {
			return new RTRecurrenceUpdater(recurrences, this);
		}
		public RTVtoUpdater UpdateVtos(params long[] vtos) {
			return new RTVtoUpdater(vtos, this);
		}

		protected void AddAction(Action a) {
			_actions.Add(a);
		}
		public RTOrganizationUpdater UpdateOrganization(long orgId) {
			return new RTOrganizationUpdater(orgId, this);
		}

		protected dynamic GetGroup<HUB>(string name, bool skip = true,string batch=null) where HUB : IHub {
			var key = KeyNameGen(name, skip, batch);
			if (_groups.ContainsKey(key)) {
				return _groups[key];
			}

			var hub = GlobalHost.ConnectionManager.GetHubContext<HUB>();
			var group = hub.Clients.Group(name, skip ? SkipUser : null);
			_groups[key] = group;
			return group;
		}

		protected dynamic GetUserGroup<HUB>(List<string> userNames, string batch = null) where HUB : IHub {
			var name = "u_" + string.Join("~", userNames);
			var key = KeyNameGen(name, false, batch);
			if (_groups.ContainsKey(key)) {
				return _groups[key];
			}

			var hub = GlobalHost.ConnectionManager.GetHubContext<HUB>();
			var group = hub.Clients.Users(userNames);
			_groups[key] = group;
			return group;
		}
		protected AngularUpdate GetUserUpdater<HUB>(List<string> usernames, string batch = null) where HUB : IHub {
			var name = "u_" + string.Join("~", usernames);
			var key = KeyNameGen(name, false, batch);

			if (_updaters.ContainsKey(key)) {
				return _updaters[key];
			}

			GetGroup<HUB>(name, false);
			var updater = new AngularUpdate();
			_updaters[key] = updater;
			return updater;
		}


		public void Dispose() {
			if (!Executed) {
				if (ExecuteOnException || !ExceptionUtility.IsInException()) {
					Execute();
				} else {
					log.Info("skipping real-time execute due to exception");
				}
			}

		}


	}
}