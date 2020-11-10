using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application {
	public class ReferralModel : IHistorical {
		public virtual string Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long FromUserId { get; set; }
		public virtual DateTime? SendTime { get; set; }
		public virtual string EmailTo { get; set; }
		public virtual string ToName { get; set; }
		public virtual string EmailFrom{ get; set; }
		public virtual string EmailSubject { get; set; }
		public virtual string EmailBody { get; set; }
		
		public ReferralModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ReferralModel> {
			public Map() {
				Id(x => x.Id).CustomType(typeof(string)).GeneratedBy.Custom(typeof(GuidStringGenerator)).Length(36);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.FromUserId);
				Map(x => x.SendTime);
				Map(x => x.ToName);
				Map(x => x.EmailTo);
				Map(x => x.EmailFrom);
				Map(x => x.EmailSubject);
				Map(x => x.EmailBody);
			}

		}

	}

	

}