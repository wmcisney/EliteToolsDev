using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {

	public partial class RealTimeUtility {
		public class RTUserUpdater {

			protected List<long> _userIds = new List<long>();
			protected RealTimeUtility rt;
			public RTUserUpdater(IEnumerable<long> userIds, RealTimeUtility rt) {
				_userIds = userIds.Where(x=>x>0).Distinct().ToList();
				this.rt = rt;
			}


			public RTUserUpdater Update(IAngularId item) {
				return Update(rid => item);
			}
			public RTUserUpdater Update(Func<long, IAngularId> item) {
				rt.AddAction(() => {
					UpdateAll(item);
				});
				return this;
			}

			protected void UpdateAll(Func<long, IAngularId> itemGenerater, bool forceNoSkip = false) {
				foreach (var r in _userIds) {
					var updater = rt.GetUpdater<RealTimeHub>(RealTimeHub.Keys.UserId(r), !forceNoSkip);
					updater.Add(itemGenerater(r));
				}
			}


			public RTUserUpdater AddLowLevelAction(Action<dynamic> action) {
				rt.AddAction(() => {
					foreach (var r in _userIds) {
						var g = rt.GetGroup<RealTimeHub>(RealTimeHub.Keys.UserId(r));
						action(g);
					}
				});
				return this;
			}

		}
	}
}