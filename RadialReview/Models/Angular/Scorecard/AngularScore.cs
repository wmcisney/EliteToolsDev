using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using System.Runtime.Serialization;
using RadialReview.Models.Application;

namespace RadialReview.Models.Angular.Scorecard {
	public class AngularScore : BaseAngular {

		public AngularScore(long id,long measurableId) : base(id) {
			Measurable = new AngularMeasurable(measurableId);
		}

		public AngularScore(ScoreModel score, DateTime? absoluteUpdateTime, bool skipUser = true) : base(score.Id) {
			Week = DateTime.SpecifyKind(score.ForWeek, DateTimeKind.Utc);
			ForWeek = TimingUtility.GetWeekSinceEpoch(Week);
			if (Id == 0)
				Id = score.MeasurableId - ForWeek;


			Measurable = new AngularMeasurable(score.Measurable, skipUser);
			Measured = score.Measured;
			DateEntered = score.DateEntered;
			Target = score.OriginalGoal ?? Measurable.Target;
			AltTarget = score.AlternateOriginalGoal ?? Measurable.AltTarget;
			Direction = score.OriginalGoalDirection ?? Measurable.Direction;

			if (score._Editable == false)
				Disabled = true;

			UT = absoluteUpdateTime;
			Origins = new List<NameId>();
			OrganizationId = score.OrganizationId;
			AccountableUserId = score.AccountableUserId;
			MeasurableId = Measurable.Id;
			MeasurableName = Measurable.Name + "";
		}

		public AngularScore() {
		}
		public long ForWeek { get; set; }
		public DateTime Week { get; set; }

		[IgnoreDataMember]
		public AngularMeasurable Measurable { get; set; }
		public long MeasurableId { get; set; }
		public string MeasurableName { get; set; }
		public List<NameId> Origins { get; set; }
		public DateTime? DateEntered { get; set; }
		public decimal? Measured { get; set; }
		public bool? Disabled { get; set; }
		public LessGreater? Direction { get; set; }
		public decimal? Target { get; set; }
		public decimal? AltTarget { get; set; }

		public long OrganizationId { get; set; }
		public long AccountableUserId { get; set; }

	}
}