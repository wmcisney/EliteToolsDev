using RadialReview.Accessors;
using RadialReview.Models.Quarterly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {

	public class QuarterVM {
		public long OrganizationId { get; set; }
		public string Name { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public int Year { get; set; }
		public int Quarter { get; set; }

		public List<SelectListItem> AvailableQuarters { get; set; }
		public List<SelectListItem> AvailableYears { get; set; }

		public QuarterVM() {
			AvailableQuarters = Enumerable.Range(1,4).Select(x => new SelectListItem { Text = "" + x, Value = "" + x }).ToList();
			AvailableYears = QuarterlyAccessor.PossibleYears(DateTime.UtcNow).Select(x => new SelectListItem { Text = "" + x, Value = "" + x }).ToList();
		}
		public QuarterVM(QuarterModel q) :base(){
			Name = q.Name;
			StartDate = q.StartDate;
			EndDate = q.EndDate;
			Quarter = q.Quarter;
			Year = q.Year;

		}
	}
}