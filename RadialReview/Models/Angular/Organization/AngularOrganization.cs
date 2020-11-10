using RadialReview.Models.Angular.Base;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Organization {
	public class AngularOrganization : BaseAngular {
		public AngularOrganization() {}
		public AngularOrganization(long id) : base(id) {}
		public AngularOrganization(OrganizationModel org) :this(org.Id){
			
			Name = org.GetName();
			ImageUrl = org.GetImageUrl();
			DateFormat = org.GetTimeSettings().DateFormat;
			HasLogo = org.GetImageUrl() != ResponsibilityGroupModel.DEFAULT_IMAGE;
		}

		public string Name { get; set; }		
		public string ImageUrl { get; set; }
		//public AngularQuarter Quarter { get; set; }
		public string DateFormat { get; set; }
		public bool HasLogo { get;  set; }
		public AngularTimezone Timezone { get; set; }
	}

	public class AngularTimezone {
		public int Offset { get; set; }
	}

	public class AngularQuarter {
		public string Name { get; set; }
		public int Quarter { get; set; }
		public int Year { get; set; }
	}
}