using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application {

	public enum FeatureSwitchFlag {
		Production=0,
		Beta = 1,
		SuperAdmin = 2,
		WHITE_LABEL = 3,
	}

	public class FeatureSwitch {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string FeatureSelector { get; set; }
		public virtual FeatureSwitchFlag ForFlag { get; set; }
		public virtual bool Disabled { get; set; }

		public FeatureSwitch() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<FeatureSwitch> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.FeatureSelector);
				Map(x => x.ForFlag).CustomType<FeatureSwitchFlag>();
				Map(x => x.Disabled);				
			}
		}
	}
}