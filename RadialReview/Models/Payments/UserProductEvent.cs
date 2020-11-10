using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments {

	public enum UserProductEventType {
		Activate,
		Deactivate,
	}

	public enum ProductType {
		L10,
		People
	}

	public class UserProductEvent {
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime EventTime { get; set; }
		public virtual UserProductEventType EventType { get; set; }
		public virtual ProductType ProductType { get; set; }
		public virtual bool Chargeable { get; set; }

		[Obsolete("Do not use")]
		public UserProductEvent() {}

		public UserProductEvent(long userId, long orgId, DateTime now, UserProductEventType eventType, ProductType productType) {
			UserId = userId;
			OrgId = orgId;
			EventTime = now;
			EventType = eventType;
			ProductType = productType;
			Chargeable = true;
		}

		public class Map : ClassMap<UserProductEvent> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.UserId);
				Map(x => x.OrgId);
				Map(x => x.EventTime);
				Map(x => x.EventType);
				Map(x => x.ProductType);
				Map(x => x.Chargeable);
			}
		}
	}
}