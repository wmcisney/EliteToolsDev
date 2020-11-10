using FluentNHibernate.Mapping;
using RadialReview;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Zapier {
	public enum ZapierEvents {
		invalid=0,
		new_todo=1,
		update_todo=2,
		all_todo=3,
		new_issue=4,
		update_issue=5,
		all_issue=6,
		new_rock=7,
		update_rock=8,
		all_rock=9,
		new_headline=10,
		update_headline=11,
		all_headline=12,
		new_measurable=13,
		update_measurable=14,
		all_measurable=15,
		update_score=16,
		attach_rock=17,
		attach_measurable=18,

		//Also update EnsureItemFilter
	}

	public class ZapierSubscription {
		public virtual long Id { get; set; }
		public virtual long SubscriberId { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long ZapierId { get; set; }
		public virtual string TargetUrl { get; set; }
		public virtual ZapierEvents Event { get; set; }

		public virtual long? FilterOnL10RecurrenceId { get; set; }
		public virtual long? FilterOnAccountableUserId { get; set; }
		public virtual long? FilterOnItemId { get; set; }


		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }




		public ZapierSubscription() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ZapierSubscription> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.OrgId);
				// Map(x => x.UserOrgId);

				Map(x => x.SubscriberId).Column("UserOrgId");

				Map(x => x.Event).CustomType<ZapierEvents>();
				Map(x => x.FilterOnAccountableUserId).Column("AccountableUserId");
				Map(x => x.FilterOnL10RecurrenceId).Column("L10RecurrenceId");
				Map(x => x.FilterOnItemId).Column("ItemId");
				Map(x => x.TargetUrl);
				Map(x => x.ZapierId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
			}

		}
	}
}
