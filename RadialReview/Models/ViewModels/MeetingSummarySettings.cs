using RadialReview.Models.Meeting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels {
	public class MeetingSummarySettings {
		public List<MeetingSummaryWhoModel> SendTo { get; set; }
		public long RecurrenceId { get; set; }

	}
}