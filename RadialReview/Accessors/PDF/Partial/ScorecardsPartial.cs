using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Accessors.PDF.Partial {

	public class ScorecardsPartialModel {
		public AngularScorecard Scorecard { get; set; }
		public string Owner { get; set; }
		public string DueDate { get; set; }
	}

	public class ScorecardsPartialViewModel {
		public string Company { get; set; }
		public string Image { get; set; }
		public ScorecardsPartialModel Scorecards { get; set; }
	}

	public class ScorecardsPartial : IPdfPartial {
		private readonly ScorecardsPartialViewModel _viewModel;
		public ScorecardsPartial(ScorecardsPartialViewModel viewModel) {
			_viewModel = viewModel;
		}

		private const string _partialView = "~/Views/Quarterly/ScorecardsPartial.cshtml";

		public string Title => "Scorecard";

		/// <summary>
		/// Generate Html String from the given template
		/// </summary>
		/// <returns></returns>
		public string Generate() {
			return ViewUtility.RenderPartial(_partialView, _viewModel).Execute();
		}
	}
}