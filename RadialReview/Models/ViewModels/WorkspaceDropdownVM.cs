using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Models.UserOrganizationModel;

namespace RadialReview.Models.ViewModels {
	public class WorkspaceDropdownVM {
		public PrimaryWorkspaceModel PrimaryWorkspace { get; set; }
		public long DefaultWorkspaceId { get; set; }
		public List<TinyRecurrence> AllMeetings { get; set; }
		public List<NameId> CustomWorkspaces { get; set; }
		public bool DisplayCreate { get; set; }

		public WorkspaceDropdownVM() {
			AllMeetings = new List<TinyRecurrence>();
			CustomWorkspaces = new List<NameId>();
		}

		public bool DisplayMeetings() {
			return AllMeetings != null && AllMeetings.Any();
		}

		public bool DisplayCustom() {
			return (CustomWorkspaces != null && CustomWorkspaces.Any());
		}

	}
}