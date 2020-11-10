using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {
	public class EditRockViewModel {
		public long? Id { get; set; }
		public string Title { get; set; }
		public long AccountableUser { get; set; }
		public bool? AddToVTO { get; set; }
		public List<SelectListItem> PotentialUsers { get; set; }
		public List<SelectListItem> PossibleRecurrences { get; set; }
		public RockState Completion { get; set; }
		public DateTime DueDate { get; set; }
		public string Notes { get; set; }
		public List<MilestoneVM> Milestones { get; set; }
		public long[] RecurrenceIds { get; set; }



		public bool HideMeetings { get; set; }
		public List<RockTypesVm> RockTypes { get; set; }
		public List<RockStatesVm> RockStates { get; set; }

		public bool CanEdit { get; set; }
		public bool CanCreate { get; set; }
		public bool IsCreate { get; set; }
		public bool CanArchive { get; set; }
		public bool AnyL10sWithMilestones { get; set; }

		public EditRockViewModel() {
			CanEdit = true;
			CanCreate = true;
			AnyL10sWithMilestones = true;
			RecurrenceIds = new long[] { };
		}



		public class MilestoneVM {
			public string Name { get; set; }
			public DateTime DueDate { get; set; }
			public bool Complete { get; set; }
			public long Id { get; set; }
			public bool Deleted { get; set; }
		}

		public class RockTypesVm {
			public string id { get; set; }

			public string name { get; set; }
			public bool disabled { get; set; }
		}

		public class RockStatesVm {
			public string id { get; set; }

			public string name { get; set; }
		}


	}
}