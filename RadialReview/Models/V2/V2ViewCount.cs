using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.V2 {
	public class V2ViewCount {
		public virtual long Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string Ip { get; set; }
		public virtual long UserId { get; set; }
		public virtual DateTime Time { get; set; }
		public V2ViewCount() {
			Time = DateTime.UtcNow;
		}

		public class Map : ClassMap<V2ViewCount> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.Ip);
				Map(x => x.UserId);				
				Map(x => x.Time);
			}
		}
	}
}