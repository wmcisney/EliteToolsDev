using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Quarterly {
	public class QuarterModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Name { get; set; }
		public virtual DateTime StartDate { get; set; }
		public virtual DateTime? EndDate { get; set; }
		public virtual long? CreatedBy { get; set; }
		public virtual DateTime NextReminder { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual int Year { get; set; }
		public virtual int Quarter { get; set; }

		public QuarterModel() {
			CreateTime = DateTime.UtcNow;
			NextReminder = DateTime.MinValue;
		}

		public class Map : ClassMap<QuarterModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.StartDate);
				Map(x => x.EndDate);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.NextReminder);
				Map(x => x.OrganizationId);
				Map(x => x.Year);
				Map(x => x.Quarter);
			}
		}
	}
}