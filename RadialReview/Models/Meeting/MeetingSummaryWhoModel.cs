using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Meeting {
	public class MeetingSummaryWhoModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual MeetingSummaryWhoType Type { get; set; }
		public virtual string Who { get; set; }
		public virtual long RecurrenceId { get; set; }

		public MeetingSummaryWhoModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<MeetingSummaryWhoModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.RecurrenceId);
				Map(x => x.Who);
				Map(x => x.Type);
			}
		}
	}

	public enum MeetingSummaryWhoType {
		Invalid = 0,
		UserOrganization = 1,
		Email = 2,
	}
}